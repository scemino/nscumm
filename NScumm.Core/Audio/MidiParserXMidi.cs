//
//  MidiParserXMidi.cs
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

using static NScumm.Core.DebugHelper;

namespace NScumm.Core.Audio
{
    public delegate void XMidiCallbackProc(byte eventData, object refCon);

    public delegate void XMidiNewTimbreListProc(MidiDriver driver, BytePtr timbreListPtr, int timbreListSize);

    /// <summary>
    /// The XMIDI version of MidiParser.
    /// Much of this code is adapted from the XMIDI implementation from the exult
    /// project.
    /// </summary>
    public class MidiParserXMidi : MidiParser
    {
        private struct Loop
        {
            public BytePtr pos;
            public byte repeat;
        }

        private readonly Loop[] _loop = new Loop[4];
        private int _loopCount;

        private readonly XMidiCallbackProc _callbackProc;
        private readonly object _callbackData;

        // TODO:
        // This should possibly get cleaned up at some point, but it's very tricks.
        // We need to support XMIDI TIMB for 7th guest, which uses
        // Miles Audio drivers. The MT32 driver needs to get the TIMB chunk, so that it
        // can install all required timbres before the song starts playing.
        // But we can't easily implement this directly like for example creating
        // a special Miles Audio class for usage in this XMIDI-class, because other engines use this
        // XMIDI-parser but w/o using Miles Audio drivers.
        private readonly XMidiNewTimbreListProc _newTimbreListProc;
        private readonly MidiDriver _newTimbreListDriver;

        private readonly BytePtr[] _tracksTimbreList = new BytePtr[120];

        // Timbre-List for each track.
        private readonly int[] _tracksTimbreListSize = new int[120];

        // Size of the Timbre-List for each track.
        private BytePtr _activeTrackTimbreList;

        private int _activeTrackTimbreListSize;

        public MidiParserXMidi(XMidiCallbackProc proc, object data, XMidiNewTimbreListProc newTimbreListProc,
            MidiDriver newTimbreListDriver)
        {
            _callbackProc = proc;
            _callbackData = data;
            _loopCount = -1;
            _newTimbreListProc = newTimbreListProc;
            _newTimbreListDriver = newTimbreListDriver;
        }

        public override void LoadMusic(byte[] data, int offset, int length)
        {
            byte[] buf = new byte[32];

            _loopCount = -1;

            UnloadMusic();
            BytePtr pos = data;

            if (pos.GetRawText(0, 4) != "FORM") return;
            pos += 4;

            // Read length of
            int len = Read4High(ref pos);
            var start = pos;

            // XDIRless XMIDI, we can handle them here.
            switch (pos.GetRawText(0, 4))
            {
                case "XMID":
                    Warning("XMIDI doesn't have XDIR");
                    pos += 4;
                    NumTracks = 1;
                    break;
                case "XDIR":
                    // Not an XMIDI that we recognize
                    Warning("Expected 'XDIR' but found '{0}{1}{2}{3}'", pos[0], pos[1], pos[2], pos[3]);
                    return;
                default:
                    // Seems Valid
                    pos += 4;
                    NumTracks = 0;

                    var i = 0;
                    for (i = 4; i < len; i++)
                    {
                        // Read 4 bytes of type
                        pos.Copy(buf, 4);
                        pos += 4;

                        // Read length of chunk
                        int chunkLen = Read4High(ref pos);

                        // Add eight bytes
                        i += 8;

                        if (buf.GetRawText(0, 4) == "INFO")
                        {
                            // Must be at least 2 bytes long
                            if (chunkLen < 2)
                            {
                                Warning("Invalid chunk length {0} for 'INFO' block", chunkLen);
                                return;
                            }

                            NumTracks = (byte) Read2Low(ref pos);

                            if (chunkLen > 2)
                            {
                                Warning("Chunk length {0} is greater than 2", chunkLen);
                                //pos += chunkLen - 2;
                            }
                            break;
                        }

                        // Must align
                        pos += (chunkLen + 1) & ~1;
                        i += (chunkLen + 1) & ~1;
                    }

                    // Didn't get to fill the header
                    if (NumTracks == 0)
                    {
                        Warning("Didn't find a valid track count");
                        return;
                    }

                    // Ok now to start part 2
                    // Goto the right place
                    pos = start + ((len + 1) & ~1);

                    if (pos.GetRawText(0, 4) == "CAT ")
                    {
                        // Not an XMID
                        Warning("Expected 'CAT ' but found '{0}{1}{2}{3}'", pos[0], pos[1], pos[2], pos[3]);
                        return;
                    }
                    pos += 4;

                    // Now read length of this track
                    len = Read4High(ref pos);

                    if (pos.GetRawText(0, 4) == "XMID")
                    {
                        // Not an XMID
                        Warning("Expected 'XMID' but found '{0}{1}{2}{3}'", pos[0], pos[1], pos[2], pos[3]);
                        return;
                    }
                    pos += 4;
                    break;
            }

            // Ok it's an XMIDI.
            // We're going to identify and store the location for each track.
            if (NumTracks > Tracks.Length)
            {
                Warning("Can only handle {0} tracks but was handed {1}", Tracks.Length, NumTracks);
                return;
            }

            int tracksRead = 0;
            while (tracksRead < NumTracks)
            {
                switch (pos.GetRawText(0, 4))
                {
                    case "FORM":
                        // Skip this plus the 4 bytes after it.
                        pos += 8;
                        break;
                    case "XMID":
                        // Skip this.
                        pos += 4;
                        break;
                    case "TIMB":
                        // Custom timbres
                        // chunk data is as follows:
                        // UINT16LE timbre count (amount of custom timbres used by this track)
                        //   BYTE     patchId
                        //   BYTE     bankId
                        //    * timbre count
                        pos += 4;
                        len = Read4High(ref pos);
                        _tracksTimbreList[tracksRead] = pos; // Skip the length bytes
                        _tracksTimbreListSize[tracksRead] = len;
                        pos += (len + 1) & ~1;
                        break;
                    case "EVNT":
                        // Ahh! What we're looking for at last.
                        Tracks[tracksRead] = pos + 8; // Skip the EVNT and length bytes
                        pos += 4;
                        len = Read4High(ref pos);
                        pos += (len + 1) & ~1;
                        ++tracksRead;
                        break;
                    default:
                        Warning("Hit invalid block '{0}{1}{2}{3}' while scanning for track locations", pos[0], pos[1],
                            pos[2], pos[3]);
                        return;
                }
            }

            // If we got this far, we successfully established
            // the locations for each of our tracks.
            // Note that we assume the original data passed in
            // will persist beyond this call, i.e. we do NOT
            // copy the data to our own buffer. Take warning....
            _ppqn = 60;
            ResetTracking();
            Tempo = 500000;
            ActiveTrack = 0;
            _activeTrackTimbreList = _tracksTimbreList[0];
            _activeTrackTimbreListSize = _tracksTimbreListSize[0];

            _newTimbreListProc?.Invoke(_newTimbreListDriver, _activeTrackTimbreList, _activeTrackTimbreListSize);
        }

        protected override void ParseNextEvent(EventInfo info)
        {
            info.Start = Position.PlayPos;
            info.Delta = ReadVLQ2(ref Position.PlayPos);

            // Process the next event.
            Position.PlayPos.Offset++;
            info.Event = Position.PlayPos.Value;
            switch (info.Event >> 4)
            {
                case 0x9: // Note On
                    Position.PlayPos.Offset++;
                    info.Param1 = Position.PlayPos.Value;
                    Position.PlayPos.Offset++;
                    info.Param2 = Position.PlayPos.Value;
                    info.Length = ReadVLQ(ref Position.PlayPos);
                    if (info.Param2 == 0)
                    {
                        info.Event = (byte) (info.Channel | 0x80);
                        info.Length = 0;
                    }
                    break;

                case 0xC:
                case 0xD:
                    Position.PlayPos.Offset++;
                    info.Param1 = Position.PlayPos.Value;
                    info.Param2 = 0;
                    break;

                case 0x8:
                case 0xA:
                case 0xE:
                    Position.PlayPos.Offset++;
                    info.Param1 = Position.PlayPos.Value;
                    Position.PlayPos.Offset++;
                    info.Param2 = Position.PlayPos.Value;
                    break;

                case 0xB:
                    Position.PlayPos.Offset++;
                    info.Param1 = Position.PlayPos.Value;
                    Position.PlayPos.Offset++;
                    info.Param2 = Position.PlayPos.Value;

                    // This isn't a full XMIDI implementation, but it should
                    // hopefully be "good enough" for most things.

                    switch (info.Param1)
                    {
                        // Simplified XMIDI looping.
                        case 0x74:
                        {
                            // XMIDI_CONTROLLER_FOR_LOOP
                            BytePtr pos = Position.PlayPos;
                            if (_loopCount < _loop.Length - 1)
                                _loopCount++;
                            else
                                Warning("XMIDI: Exceeding maximum loop count {0}", _loop.Length);

                            _loop[_loopCount].pos = pos;
                            _loop[_loopCount].repeat = info.Param2;
                            break;
                        }

                        case 0x75: // XMIDI_CONTROLLER_NEXT_BREAK
                            if (_loopCount >= 0)
                            {
                                if (info.Param2 < 64)
                                {
                                    // End the current loop.
                                    _loopCount--;
                                }
                                else
                                {
                                    // Repeat 0 means "loop forever".
                                    if (_loop[_loopCount].repeat != 0)
                                    {
                                        if (--_loop[_loopCount].repeat == 0)
                                            _loopCount--;
                                        else
                                            Position.PlayPos = _loop[_loopCount].pos;
                                    }
                                    else
                                    {
                                        Position.PlayPos = _loop[_loopCount].pos;
                                    }
                                }
                            }
                            break;

                        case 0x77: // XMIDI_CONTROLLER_CALLBACK_TRIG
                            _callbackProc?.Invoke(info.Param2, _callbackData);
                            break;

                        case 0x6e: // XMIDI_CONTROLLER_CHAN_LOCK
                        case 0x6f: // XMIDI_CONTROLLER_CHAN_LOCK_PROT
                        case 0x70: // XMIDI_CONTROLLER_VOICE_PROT
                        case 0x71: // XMIDI_CONTROLLER_TIMBRE_PROT
                        case 0x72: // XMIDI_CONTROLLER_BANK_CHANGE
                        case 0x73: // XMIDI_CONTROLLER_IND_CTRL_PREFIX
                        case 0x76: // XMIDI_CONTROLLER_CLEAR_BB_COUNT
                        case 0x78: // XMIDI_CONTROLLER_SEQ_BRANCH_INDEX
                        default:
                            if (info.Param1 >= 0x6e && info.Param1 <= 0x78)
                            {
                                Warning("Unsupported XMIDI controller %d (0x%2x)",
                                    info.Param1, info.Param1);
                            }
                            break;
                    }

                    // Should we really keep passing the XMIDI controller events to
                    // the MIDI driver, or should we turn them into some kind of
                    // NOP events? (Dummy meta events, perhaps?) Ah well, it has
                    // worked so far, so it shouldn't cause any damage...

                    break;

                case 0xF: // Meta or SysEx event
                    switch (info.Event & 0x0F)
                    {
                        case 0x2: // Song Position Pointer
                            Position.PlayPos.Offset++;
                            info.Param1 = Position.PlayPos.Value;
                            Position.PlayPos.Offset++;
                            info.Param2 = Position.PlayPos.Value;
                            break;

                        case 0x3: // Song Select
                            Position.PlayPos.Offset++;
                            info.Param1 = Position.PlayPos.Value;
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
                            info.Length = ReadVLQ(ref Position.PlayPos);
                            info.Data = Position.PlayPos;
                            Position.PlayPos += info.Length;
                            break;

                        case 0xF: // META event
                            Position.PlayPos.Offset++;
                            info.MetaType = Position.PlayPos.Value;
                            info.Length = ReadVLQ(ref Position.PlayPos);
                            info.Data = Position.PlayPos;
                            Position.PlayPos += info.Length;
                            if (info.MetaType == 0x51 && info.Length == 3)
                            {
                                // Tempo event. We want to make these constant 500,000.
                                info.Data[0] = 0x07;
                                info.Data[1] = 0xA1;
                                info.Data[2] = 0x20;
                            }
                            break;

                        default:
                            Warning("MidiParser_XMIDI::parseNextEvent: Unsupported event code {0:X}", info.Event);
                            break;
                    }
                    break;
            }
        }

        // This is a special XMIDI variable length quantity
        private int ReadVLQ2(ref BytePtr pos)
        {
            int value = 0;
            while ((pos[0] & 0x80) == 0)
            {
                value += pos.Value;
                pos.Offset++;
            }
            return value;
        }
    }
}