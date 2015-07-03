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

using System;
using System.IO;
using System.Text;

namespace NScumm.Core.Audio.Midi
{
    public class SmfMidiParser: MidiParser
    {
        protected bool MalformedPitchBends { get; set; }

        #region implemented abstract members of MidiParser

        MemoryStream input;

        public override void LoadMusic(byte[] data)
        {
            input = new MemoryStream(data);
            UnloadMusic();

            int midiType;
            var isGmf = false;
            var br = new BinaryReader(input);
            var sig = br.ReadBytes(4);
            input.Seek(0, SeekOrigin.Begin);

            if (AreEquals(sig, "RIFF"))
            {
                // Skip the outer RIFF header.
                input.Seek(8, SeekOrigin.Current);
            }

            if (AreEquals(sig, "MThd"))
            {
                // SMF with MTHd information.
                input.Seek(4, SeekOrigin.Current);
                var len = br.ReadUInt32BigEndian();
                if (len != 6)
                {
                    throw new InvalidOperationException(string.Format("MThd length 6 expected but found {0}", len));
                }
                br.ReadByte(); //?
                midiType = br.ReadByte();
                // Verify that this MIDI either is a Type 2
                // or has only 1 track. We do not support
                // multitrack Type 1 files.
                NumTracks = br.ReadUInt16BigEndian();
                
                if (midiType > 2 /*|| (midiType < 2 && _numTracks > 1)*/)
                {
                    throw new InvalidOperationException(string.Format("No support for a Type {0} MIDI with {1} tracks", midiType, NumTracks));
                }
                PulsesPerQuarterNote = br.ReadUInt16BigEndian();
            }
            else if (AreEquals(sig, "GMF\x1"))
            {
                // Older GMD/MUS file with no header info.
                // Assume 1 track, 192 PPQN, and no MTrk headers.
                isGmf = true;
                midiType = 0;
                NumTracks = 1;
                PulsesPerQuarterNote = 192;
                // 'GMD\x1' + 3 bytes of useless (translate: unknown) information
                input.Seek(7, SeekOrigin.Current);
            }
            else
            {
                throw new InvalidOperationException(string.Format("Expected MThd or GMD header but found '{0}{1}{2}{3}' instead", sig[0], sig[1], sig[2], sig[3]));
            }

            // Now we identify and store the location for each track.
            if (NumTracks > Tracks.Length)
            {
                throw new InvalidOperationException(string.Format("Can only handle {0} tracks but was handed {1}", Tracks.Length, NumTracks));
            }

            uint totalSize = 0;
            var tracksRead = 0;
            while (tracksRead < NumTracks)
            {
                sig = br.ReadBytes(4);
                if (!AreEquals(sig, "MTrk") && !isGmf)
                {
                    var msg = new StringBuilder();
                    msg.AppendFormat("Position: {0} ('{1}')", input.Position - 4, (char)sig[0]).AppendLine();
                    msg.AppendFormat("Hit invalid block '{0}{1}{2}{3}' while scanning for track locations", sig[0], sig[1], sig[2], sig[3]);
                    throw new InvalidOperationException(msg.ToString());
                }

                // If needed, skip the MTrk and length bytes
                Tracks[tracksRead] = new Track{ Position = input.Position + (isGmf ? -4 : 4) };
                if (!isGmf)
                {
                    var len = br.ReadUInt32BigEndian();
                    totalSize += len;
                    input.Seek(len, SeekOrigin.Current);
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
            info.Start = Position.PlayPos;
            input.Seek(Position.PlayPos, SeekOrigin.Begin);
            info.Delta = ReadVLQ(input);

            // Process the next info. If mpMalformedPitchBends
            // was set, we must skip over any pitch bend events
            // because they are from Simon games and are not
            // real pitch bend events, they're just two-byte
            // prefixes before the real info.
            do
            {
                var data = input.ReadByte();
                if ((data & 0xF0) >= 0x80)
                {                    
                    info.Event = data;
                }
                else
                {
                    info.Event = Position.RunningStatus;
                    input.Position--;
                }
            } while (MalformedPitchBends && (info.Event & 0xF0) == 0xE0 && input.Position++ < input.Length);
            if (info.Event < 0x80)
                return;

            Position.RunningStatus = info.Event;
            switch (info.Command)
            {
                case 0x9: // Note On
                    info.Param1 = input.ReadByte();
                    info.Param2 = input.ReadByte();
                    if (info.Param2 == 0)
                        info.Event = info.Channel | 0x80;
                    info.Data = new byte[0];
//                    Debug.WriteLine("MidiParser_SMF::ParseNextEvent NoteOn({0},{1},{2})", info.Event, info.Param1, info.Param2);
                    break;

                case 0xC:
                case 0xD:
                    info.Param1 = input.ReadByte();
                    info.Param2 = 0;
//                    Debug.WriteLine("MidiParser_SMF::ParseNextEvent Param1 = {0}", info.Param1);
                    break;

                case 0x8:
                case 0xA:
                case 0xB:
                case 0xE:
                    info.Param1 = input.ReadByte();
                    info.Param2 = input.ReadByte();
                    info.Data = new byte[0];
//                    Debug.WriteLine("MidiParser_SMF::ParseNextEvent Param1 = {0}, Param2 = {1}", info.Param1, info.Param2);
                    break;

                case 0xF: // System Common, Meta or SysEx event
                    switch (info.Event & 0x0F)
                    {
                        case 0x2: // Song Position Pointer
                            info.Param1 = input.ReadByte();
                            info.Param2 = input.ReadByte();
                            break;

                        case 0x3: // Song Select
                            info.Param1 = input.ReadByte();
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
                                var len = ReadVLQ(input);
                                var br = new BinaryReader(input);
                                info.Data = br.ReadBytes(len);
                            }
                            break;

                        case 0xF: // META event
                            {

                                var br = new BinaryReader(input);
                                info.MetaType = input.ReadByte();
                                var len = ReadVLQ(input);
                                info.Data = br.ReadBytes(len);
                            }
                            break;
                        default:
//                            Console.Error.WriteLine("MidiParser_SMF::parseNextEvent: Unsupported event code {0:X}", info.Event);
                            break;
                    }
                    break;
            }
            Position.PlayPos = input.Position;
        }

        #endregion

        static bool AreEquals(byte[] data1, string data2)
        {
            return AreEquals(data1, Encoding.UTF8.GetBytes(data2));
        }

        static bool AreEquals(byte[] data1, byte[] data2)
        {
            if (data1.Length == data2.Length)
            {
                for (int i = 0; i < data1.Length; i++)
                {
                    if (data1[i] != data2[i])
                        return false;
                }
                return true;
            }
            return false;
        }
    }
}
