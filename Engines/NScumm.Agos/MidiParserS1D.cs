//
//  MidiParserS1D.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core;

namespace NScumm.Agos
{
    /// <summary>
    /// Simon 1 Demo version of MidiParser.
    /// </summary>
    internal class MidiParserS1D : MidiParser
    {
        struct Loop
        {
            public ushort timer;
            public BytePtr start, end;
        }

        private BytePtr _data;
        private bool _noDelta;
        private readonly Loop[] _loops = new Loop[16];

        public override void LoadMusic(byte[] data, int offset, int size)
        {
            UnloadMusic();

            if (size == 0)
                return;

            // The original actually just ignores the first two bytes.
            BytePtr pos = data;
            if (pos.Value == 0xFC)
            {
                // SysEx found right at the start
                // this seems to happen since Elvira 2, we ignore it
                // 3rd byte after the SysEx seems to be saved into a global

                // We expect at least 4 bytes in total
                if (size < 4)
                    return;

                byte skipOffset = pos[2]; // get second byte after the SysEx
                // pos[1] seems to have been ignored
                // pos[3] is saved into a global inside the original interpreters

                // Waxworks + Simon 1 demo typical header is:
                //  0xFC 0x29 0x07 0x01 [0x00/0x01]
                // Elvira 2 typical header is:
                //  0xFC 0x04 0x06 0x06

                if (skipOffset >= 6)
                {
                    // should be at least 6, so that we skip over the 2 size bytes and the
                    // smallest SysEx possible
                    skipOffset -= 2; // 2 size bytes were already read by previous code outside of this method

                    if (size <= skipOffset) // Skip to the end of file? -> something is not correct
                        return;

                    // Do skip over the bytes
                    pos += skipOffset;
                }
                else
                {
                    DebugHelper.Warning("MidiParser_S1D: unexpected skip offset in music file");
                }
            }

            // And now we're at the actual data. Only one track.
            NumTracks = 1;
            _data = pos;
            Tracks[0] = pos;

            // Note that we assume the original data passed in
            // will persist beyond this call, i.e. we do NOT
            // copy the data to our own buffer. Take warning....
            ResetTracking();
            Tempo = 666667;
            ActiveTrack = 0;
        }

        protected override void ParseNextEvent(EventInfo info)
        {
            info.Start = Position.PlayPos;
            info.Length = 0;
            info.Delta = _noDelta ? 0 : ReadVLQ2(ref Position.PlayPos);
            _noDelta = false;

            info.Event = Position.PlayPos.Value;
            Position.PlayPos.Offset++;
            if ((info.Event & 0x80) == 0)
            {
                _noDelta = true;
                info.Event |= 0x80;
            }

            if (info.Event == 0xFC)
            {
                // This means End of Track.
                // Rewrite in SMF (MIDI transmission) form.
                info.Event = 0xFF;
                info.MetaType = 0x2F;
            }
            else
            {
                switch (info.Command)
                {
                    case 0x8: // note off
                        info.Param1 = Position.PlayPos.Value;
                        Position.PlayPos.Offset++;
                        info.Param2 = 0;
                        break;

                    case 0x9: // note on
                        info.Param1 = Position.PlayPos.Value;
                        Position.PlayPos.Offset++;
                        info.Param2 = Position.PlayPos.Value;
                        Position.PlayPos.Offset++;
                        // Rewrite note on events with velocity 0 as note off events.
                        // This is the actual meaning of this, but theoretically this
                        // should not need to be rewritten, since all MIDI devices should
                        // interpret it like that. On the other hand all our MidiParser
                        // implementations do it and there seems to be code in MidiParser
                        // which relies on this for tracking active notes.
                        if (info.Param2 == 0)
                        {
                            info.Event = (byte) (info.Channel | 0x80);
                        }
                        break;

                    case 0xA:
                    {
                        // loop control
                        // In case the stop mode(?) is set to 0x80 this will stop the
                        // track over here.

                        short loopIterations = (sbyte) Position.PlayPos.Value;
                        Position.PlayPos.Offset++;
                        if (loopIterations == 0)
                        {
                            _loops[info.Channel].start = Position.PlayPos;
                        }
                        else
                        {
                            if (_loops[info.Channel].timer == 0)
                            {
                                if (_loops[info.Channel].start != new BytePtr())
                                {
                                    _loops[info.Channel].timer = (ushort) loopIterations;
                                    _loops[info.Channel].end = Position.PlayPos;

                                    // Go to the start of the loop
                                    Position.PlayPos = _loops[info.Channel].start;
                                }
                            }
                            else
                            {
                                if (_loops[info.Channel].timer != 0)
                                    Position.PlayPos = _loops[info.Channel].start;
                                --_loops[info.Channel].timer;
                            }
                        }

                        // We need to read the next midi event here. Since we can not
                        // safely pass this event to the MIDI event processing.
                        ChainEvent(ref info);
                    }
                        break;

                    case 0xB: // auto stop marker(?)
                        // In case the stop mode(?) is set to 0x80 this will stop the
                        // track.

                        // We need to read the next midi event here. Since we can not
                        // safely pass this event to the MIDI event processing.
                        ChainEvent(ref info);
                        break;

                    case 0xC: // program change
                        info.Param1 = Position.PlayPos.Value;
                        Position.PlayPos.Offset++;
                        info.Param2 = 0;
                        break;

                    case 0xD: // jump to loop end
                        if (_loops[info.Channel].end != BytePtr.Null)
                            Position.PlayPos = _loops[info.Channel].end;

                        // We need to read the next midi event here. Since we can not
                        // safely pass this event to the MIDI event processing.
                        ChainEvent(ref info);
                        break;

                    default:
                        // The original called some other function from here, which seems
                        // not to be MIDI related.
                        DebugHelper.Warning("MidiParser_S1D: default case {0}", info.Channel);

                        // We need to read the next midi event here. Since we can not
                        // safely pass this event to the MIDI event processing.
                        ChainEvent(ref info);
                        break;
                }
            }
        }

        private int ReadVLQ2(ref BytePtr data)
        {
            // LE format VLQ, which is 2 bytes long at max.
            int delta = data.Value;
            data.Offset++;
            if ((delta & 0x80) != 0)
            {
                delta &= 0x7F;
                delta = delta | data.Value << 7;
                data.Offset++;
            }

            return delta;
        }

        private void ChainEvent(ref EventInfo info)
        {
            // When we chain an event, we add up the old delta.
            int delta = info.Delta;
            ParseNextEvent(info);
            info.Delta += delta;
        }

        private void ResetTracking()
        {
            base.ResetTracking();
            // The first event never contains any delta.
            _noDelta = true;
            Array.Clear(_loops, 0, _loops.Length);
        }
    }
}