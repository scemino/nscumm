//
//  SmfMidiParser.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NScumm.Core.Common;
using System;
using System.IO;
using System.Text;

namespace NScumm.Core.Audio.Midi
{
    public class SmfMidiParser : MidiParser
    {
        protected bool MalformedPitchBends { get; set; }

        #region implemented abstract members of MidiParser

        public override void LoadMusic(byte[] data)
        {
            UnloadMusic();
            var pos = new ByteAccess(data);

            int midiType;
            var isGmf = false;

            if (ScummHelper.ToText(data) == "RIFF")
            {
                // Skip the outer RIFF header.
                pos.Offset += 8;
            }

            if (ScummHelper.ToText(pos.Data, pos.Offset) == "MThd")
            {
                // SMF with MTHd information.
                pos.Offset += 4;
                var len = (int)Read4high(pos);
                if (len != 6)
                {
                    throw new InvalidOperationException($"MThd length 6 expected but found {len}");
                }

                // Verify that this MIDI either is a Type 2
                // or has only 1 track. We do not support
                // multitrack Type 1 files.
                NumTracks = pos[2] << 8 | pos[3];
                midiType = pos[1];
                if (midiType > 2 /*|| (midiType < 2 && _numTracks > 1)*/)
                {
                    throw new InvalidOperationException($"No support for a Type {midiType} MIDI with {NumTracks} tracks");
                }
                PulsesPerQuarterNote = pos[4] << 8 | pos[5];
                pos.Offset += len;
            }
            else if (ScummHelper.ToText(pos.Data, pos.Offset) == "GMF\x1")
            {
                // Older GMD/MUS file with no header info.
                // Assume 1 track, 192 PPQN, and no MTrk headers.
                isGmf = true;
                midiType = 0;
                NumTracks = 1;
                PulsesPerQuarterNote = 192;
                pos.Offset += 7; // 'GMD\x1' + 3 bytes of useless (translate: unknown) information
            }
            else
            {
                throw new InvalidOperationException(string.Format("Expected MThd or GMD header but found '{0}{1}{2}{3}' instead", pos[0], pos[1], pos[2], pos[3]));
            }

            // Now we identify and store the location for each track.
            if (NumTracks > Tracks.Length)
            {
                throw new InvalidOperationException(string.Format("Can only handle {0} tracks but was handed {1}", Tracks.Length, NumTracks));
            }

            int totalSize = 0;
            var tracksRead = 0;
            while (tracksRead < NumTracks)
            {
                if (ScummHelper.ToText(pos.Data, pos.Offset) != "MTrk" && !isGmf)
                {
                    var msg = new StringBuilder();
                    msg.AppendFormat("Position: {0} ('{1}')", pos.Offset - 4, (char)pos[0]).AppendLine();
                    msg.AppendFormat("Hit invalid block '{0}{1}{2}{3}' while scanning for track locations", pos[0], pos[1], pos[2], pos[3]);
                    throw new InvalidOperationException(msg.ToString());
                }

                // If needed, skip the MTrk and length bytes
                Tracks[tracksRead] = new ByteAccess(pos, (isGmf ? 0 : 8));
                if (!isGmf)
                {
                    pos.Offset += 4;
                    var len = (int)Read4high(pos);
                    totalSize += len;
                    pos.Offset += len;
                }
                else
                {
                    // TODO: vs An SMF End of Track meta event must be placed
                    // at the end of the stream.
                    //                    data[size++] = 0xFF;
                    //                    data[size++] = 0x2F;
                    //                    data[size++] = 0x00;
                    //                    data[size++] = 0x00;
                    throw new NotImplementedException("Gmf not implemented");
                }
                ++tracksRead;
            }

            // If this is a Type 1 MIDI, we need to now compress
            // our tracks down into a single Type 0 track.
            //_buffer = 0;

            if (midiType == 1)
            {
                // FIXME: Doubled the buffer size to prevent crashes with the
                // Inherit the Earth MIDIs. Jamieson630 said something about a
                // better fix, but this will have to do in the meantime.
                //                _buffer = (byte*)malloc(size * 2);
                //                compressToType0();
                //                _numTracks = 1;
                //                _tracks[0] = _buffer;
                throw new NotImplementedException("MidiType 1 not yet implemented.");
            }

            // Note that we assume the original data passed in
            // will persist beyond this call, i.e. we do NOT
            // copy the data to our own buffer. Take warning....
            ResetTracking();
            Tempo = 500000;
            ActiveTrack = 0;
        }

        protected override void ParseNextEvent(EventInfo info)
        {
            info.Start = new ByteAccess(Position.PlayPos);
            info.Delta = ReadVLQ(Position.PlayPos);

            // Process the next info. If mpMalformedPitchBends
            // was set, we must skip over any pitch bend events
            // because they are from Simon games and are not
            // real pitch bend events, they're just two-byte
            // prefixes before the real info.
            do
            {
                if ((Position.PlayPos[0] & 0xF0) >= 0x80)
                    info.Event = Position.PlayPos.Increment();
                else
                    info.Event = Position.RunningStatus;
            } while (MalformedPitchBends && (info.Event & 0xF0) == 0xE0 && Position.PlayPos.Increment() != 0);
            if (info.Event < 0x80)
                return;

            Position.RunningStatus = info.Event;
            switch (info.Command)
            {
                case 0x9: // Note On
                    info.Param1 = Position.PlayPos.Increment();
                    info.Param2 = Position.PlayPos.Increment();
                    if (info.Param2 == 0)
                        info.Event = info.Channel | 0x80;
                    info.Length = 0;
                    //                    Debug.WriteLine("MidiParser_SMF::ParseNextEvent NoteOn({0},{1},{2})", info.Event, info.Param1, info.Param2);
                    break;

                case 0xC:
                case 0xD:
                    info.Param1 = Position.PlayPos.Increment();
                    info.Param2 = 0;
                    //                    Debug.WriteLine("MidiParser_SMF::ParseNextEvent Param1 = {0}", info.Param1);
                    break;

                case 0x8:
                case 0xA:
                case 0xB:
                case 0xE:
                    info.Param1 = Position.PlayPos.Increment();
                    info.Param2 = Position.PlayPos.Increment();
                    info.Length = 0;
                    //                    Debug.WriteLine("MidiParser_SMF::ParseNextEvent Param1 = {0}, Param2 = {1}", info.Param1, info.Param2);
                    break;

                case 0xF: // System Common, Meta or SysEx event
                    switch (info.Event & 0x0F)
                    {
                        case 0x2: // Song Position Pointer
                            info.Param1 = Position.PlayPos.Increment();
                            info.Param2 = Position.PlayPos.Increment();
                            break;

                        case 0x3: // Song Select
                            info.Param1 = Position.PlayPos.Increment();
                            info.Param2 = 0;
                            break;

                        case 0x6:
                        case 0x8:
                        case 0xA:
                        case 0xB:
                        case 0xC:
                        case 0xE:
                            info.Param1 = info.Param2 = 0;
                            break;

                        case 0x0: // SysEx
                            {
                                var len = ReadVLQ(Position.PlayPos);
                                info.Data = new ByteAccess(Position.PlayPos);
                                Position.PlayPos.Offset += info.Length;
                            }
                            break;

                        case 0xF: // META event
                            {
                                info.MetaType = Position.PlayPos.Increment();
                                info.Length = ReadVLQ(Position.PlayPos);
                                info.Data = new ByteAccess(Position.PlayPos);
                                Position.PlayPos.Offset += info.Length;
                            }
                            break;
                        default:
                            //                            Console.Error.WriteLine("MidiParser_SMF::parseNextEvent: Unsupported event code {0:X}", info.Event);
                            break;
                    }
                    break;
            }
        }

        #endregion


        /// <summary>
        /// Platform independent BE uint32 read-and-advance.
        /// This helper function reads Big Endian 32-bit numbers
        /// from a memory pointer, at the same time advancing
        /// the pointer.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected uint Read4high(ByteAccess data)
        {
            uint val = data.ReadUInt32BigEndian();
            data.Offset += 4;
            return val;
        }
    }
}
