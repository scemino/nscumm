//
//  Player.cs
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
using System.Diagnostics;
using System.IO;
using System.Text;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Audio.IMuse
{
    class Player : MidiDriverBase
    {
        const int TicksPerBeat = 480;

        // Moved from IMuseInternal.
        // This is only used by one player at a time.
        protected static ushort[] _active_notes;

        protected MidiDriver _midi;
        protected MidiParser _parser;

        protected Part _parts;
        protected bool _active;
        protected bool _scanning;

        public bool Scanning { get { return _scanning; } }

        protected int _id;
        protected byte _priority;
        protected byte _volume;
        protected sbyte _pan;
        protected sbyte _transpose;
        protected sbyte _detune;
        protected int _note_offset;
        protected byte _vol_eff;

        protected int _track_index;
        protected uint _loop_to_beat;
        protected uint _loop_from_beat;
        protected uint _loop_counter;
        protected uint _loop_to_tick;
        protected uint _loop_from_tick;
        protected byte _speed;

        // This does not get used by us! It is only
        // here for save/load purposes, and gets
        // passed on to the MidiParser during
        // fixAfterLoad().
        protected uint _music_tick;

        protected HookDatas _hook;
        protected ParameterFader[] _parameterFaders;

        protected bool _isMT32;
        protected bool _isMIDI;
        protected bool _supportsPercussion;

        public bool SupportsPercussion { get { return _supportsPercussion; } }

        // Player part
        protected void HookClear()
        {
            _hook = new HookDatas();
        }

        protected void UninitParts()
        {
            Debug.Assert(_parts == null || _parts.Player == this);

            while (_parts != null)
                _parts.Uninit();

            // In case another player is waiting to allocate parts
            if (_midi != null)
                _se.ReallocateMidiChannels(_midi);
        }

        protected void PartSetTranspose(byte chan, byte relative, sbyte b)
        {
            if (b > 24 || b < -24)
                return;

            var part = GetPart(chan);
            if (part == null)
                return;
            if (relative != 0)
                b = (sbyte)TransposeClamp(b + part.Transpose, -7, 7);
            part.SetTranspose(b);
        }

        public static int TransposeClamp(int a, int b, int c)
        {
            if (b > a)
                a += (b - a + 11) / 12 * 12;
            if (c < a)
                a -= (a - c + 11) / 12 * 12;
            return a;
        }

        public void MaybeJump(byte cmd, uint track, uint beat, uint tick)
        {
            // Is this the hook I'm waiting for?
            if (cmd != 0 && _hook.Jump[0] != cmd)
                return;

            // Reset hook?
            if (cmd != 0 && cmd < 0x80)
            {
                _hook.Jump[0] = _hook.Jump[1];
                _hook.Jump[1] = 0;
            }

            Jump(track, beat, tick);
        }

        public void MaybeSetTranspose(byte[] data)
        {
            var cmd = data[0];

            // Is this the hook I'm waiting for?
            if (cmd != 0 && _hook.Transpose != cmd)
                return;

            // Reset hook?
            if (cmd != 0 && cmd < 0x80)
                _hook.Transpose = 0;

            SetTranspose(data[1], (sbyte)data[2]);
        }

        public void MaybePartOnOff(byte[] data)
        {
            var cmd = data[1];
            var chan = data[0];

            var p = _hook.PartOnOff[chan];

            // Is this the hook I'm waiting for?
            if (cmd != 0 && p != cmd)
                return;

            if (cmd != 0 && cmd < 0x80)
                _hook.PartOnOff[chan] = 0;

            var part = GetPart(chan);
            if (part != null)
                part.SetOnOff(data[2] != 0);
        }

        public void MaybeSetVolume(byte[] data)
        {
            var cmd = data[1];
            var chan = data[0];

            var p = _hook.PartVolume[chan];

            // Is this the hook I'm waiting for?
            if (cmd != 0 && p != cmd)
                return;

            // Reset hook?
            if (cmd != 0 && cmd < 0x80)
                _hook.PartVolume[chan] = 0;

            var part = GetPart(chan);
            if (part != null)
                part.Volume = data[2];
        }

        public void MaybeSetProgram(byte[] data)
        {
            var cmd = data[1];
            var chan = data[0];

            // Is this the hook I'm waiting for?
            var p = _hook.PartProgram[chan];

            if (cmd != 0 && p != cmd)
                return;

            if (cmd != 0 && cmd < 0x80)
                _hook.PartProgram[chan] = 0;

            var part = GetPart(chan);
            if (part != null)
                part.ProgramChange(data[2]);
        }

        public void MaybeSetTransposePart(byte[] data)
        {
            var cmd = data[1];
            var chan = data[0];

            // Is this the hook I'm waiting for?
            var p = _hook.PartTranspose[chan];

            if (cmd != 0 && p != cmd)
                return;

            // Reset hook?
            if (cmd != 0 && cmd < 0x80)
                _hook.PartTranspose[chan] = 0;

            PartSetTranspose(chan, data[2], (sbyte)data[3]);
        }

        protected void TurnOffPedals()
        {
            for (var part = _parts; part != null; part = part.Next)
            {
                if (part.Pedal)
                    part.Sustain(false);
            }
        }

        protected int QueryPartParam(int param, byte chan)
        {
            var part = _parts;
            while (part != null)
            {
                if (part.Channel == chan)
                {
                    switch (param)
                    {
                        case 14:
                            return part.On ? 1 : 0;
                        case 15:
                            return part.Volume;
                        case 16:
                                    // FIXME: Need to know where this occurs...
//                            Console.Error.WriteLine("Trying to cast instrument ({0}, {1}) -- please tell Fingolfin", param, chan);
                    // In old versions of the code, this used to return part->_program.
                    // This was changed in revision 2.29 of imuse.cpp (where this code used
                    // to reside).
                    //              return (int)part->_instrument;
                            return part.Transpose;
                        case 17:
                            return part.Transpose;
                        default:
                            return -1;
                    }
                }
                part = part.Next;
            }
            return 129;
        }

        protected void TurnOffParts()
        {
            for (var part = _parts; part != null; part = part.Next)
            {
                if (part.Pedal)
                    part.Sustain(false);
            }
        }

        protected void PlayActiveNotes()
        {
            for (var i = 0; i < 16; ++i)
            {
                var part = GetPart((byte)i);
                if (part != null)
                {
                    var mask = 1 << i;
                    for (var j = 0; j < 128; ++j)
                    {
                        if ((_active_notes[j] & mask) != 0)
                            part.NoteOn((byte)j, 80);
                    }
                }
            }
        }

        protected void TransitionParameters()
        {
            var advance = _midi.BaseTempo;
            int value;

            foreach (var ptr in _parameterFaders)
            {
                if (ptr.Param == 0)
                    continue;

                ptr.CurrentTime += advance;
                if (ptr.CurrentTime > ptr.TotalTime)
                    ptr.CurrentTime = ptr.TotalTime;
                value = (int)ptr.Start + (int)(ptr.End - ptr.Start) * (int)ptr.CurrentTime / (int)ptr.TotalTime;

                switch (ptr.Param)
                {
                    case ParameterFaderType.Volume:
                            // Volume.
                        if (value == 0 && ptr.End == 0)
                        {
                            Clear();
                            return;
                        }
                        SetVolume((byte)value);
                        break;

                    case ParameterFaderType.Transpose:
                            // FIXME: Is this really transpose?
                        SetTranspose(0, value / 100);
                        SetDetune(value % 100);
                        break;

                    case ParameterFaderType.Speed: // impSpeed:
                            // Speed.
                        SetSpeed((byte)value);
                        break;

                    default:
                        ptr.Param = 0;
                        break;
                }

                if (ptr.CurrentTime >= ptr.TotalTime)
                    ptr.Param = 0;
            }
        }

        // Sequencer part
        protected int StartSeqSound(int sound, bool resetVars = true)
        {
            if (resetVars)
            {
                _loop_to_beat = 1;
                _loop_from_beat = 1;
                _track_index = 0;
                _loop_counter = 0;
                _loop_to_tick = 0;
                _loop_from_tick = 0;
            }

            var ptr = _se.FindStartOfSound(sound);
            if (ptr == null)
                return -1;

            if (Encoding.UTF8.GetString(ptr, 0, 2) == "RO")
            {
                // Old style 'RO' resource
                _parser = MidiParser.CreateRO();
            }
            else if (Encoding.UTF8.GetString(ptr, 0, 4) == "FORM")
            {
                // Humongous Games XMIDI resource
                _parser = MidiParser.CreateXMidiParser();
            }
            else
            {
                // SCUMM SMF resource
                _parser = MidiParser.CreateSmfParser();
            }

            _parser.MidiDriver = this;
            _parser.Property(MidiParserProperty.SmartJump, 1);
            _parser.LoadMusic(ptr);
            _parser.ActiveTrack = _track_index;

            ptr = _se.FindStartOfSound(sound, IMuseInternal.ChunkType.MDhd);
            var speed = 128;
            if (resetVars)
            {
                if (ptr != null)
                {
                    using (var br = new BinaryReader(new MemoryStream(ptr)))
                    {
                        br.BaseStream.Seek(4, SeekOrigin.Begin);
                        speed = br.ReadUInt32BigEndian() != 0 && ptr[15] != 0 ? ptr[15] : 128;
                    }
                }
            }
            else
            {
                speed = _speed;
            }
            SetSpeed((byte)speed);

            return 0;
        }

        protected void LoadStartParameters(int sound)
        {
            _priority = 0x80;
            _volume = 0x7F;
            VolChan = 0xFFFF;
            _vol_eff = (byte)((_se.GetChannelVolume(0xFFFF) << 7) >> 7);
            _pan = 0;
            _transpose = 0;
            _detune = 0;

            var ptr = _se.FindStartOfSound(sound, IMuseInternal.ChunkType.MDhd);
            uint size;

            if (ptr != null)
            {
                using (var br = new BinaryReader(new MemoryStream(ptr)))
                {
                    br.BaseStream.Seek(4, SeekOrigin.Current);
                    size = br.ReadUInt32BigEndian();
                    br.BaseStream.Seek(4, SeekOrigin.Current);

                    // MDhd chunks don't get used in MI1 and contain only zeroes.
                    // We check for volume, priority and speed settings of zero here.
                    if (size != 0 && (ptr[2] | ptr[3] | ptr[7]) != 0)
                    {
                        _priority = ptr[2];
                        _volume = ptr[3];
                        _pan = (sbyte)ptr[4];
                        _transpose = (sbyte)ptr[5];
                        _detune = (sbyte)ptr[6];
                        SetSpeed(ptr[7]);
                    }
                }
            }
        }

        public IMuseInternal _se;

        public uint VolChan { get; set; }

        public Player()
        {
            _speed = 128;
            _parameterFaders = new ParameterFader[4];
            for (int i = 0; i < _parameterFaders.Length; i++)
            {
                _parameterFaders[i] = new ParameterFader();
            }
            _active_notes = new ushort[128];
            _hook = new HookDatas();
        }

        public int AddParameterFader(ParameterFaderType param, int target, int time)
        {
            int start;

            switch (param)
            {
                case ParameterFaderType.Volume:
                    // HACK: If volume is set to 0 with 0 time,
                    // set it so immediately but DON'T clear
                    // the player. This fixes a problem with
                    // music being cleared inappropriately
                    // in S&M when playing with the Dinosaur.
                    if (target == 0 && time == 0)
                    {
                        SetVolume(0);
                        return 0;
                    }

                    // Volume fades are handled differently.
                    start = _volume;
                    break;

                case ParameterFaderType.Transpose:
                    // FIXME: Is this transpose? And what's the scale?
                    // It's set to fade to -2400 in the tunnel of love.
                    //      debug(0, "parameterTransition(3) outside Tunnel of Love?");
                    start = _transpose;
                    //      target /= 200;
                    break;

                case ParameterFaderType.Speed: // impSpeed
                    // FIXME: Is the speed from 0-100?
                    // Right now I convert it to 0-128.
                    start = _speed;
                    //      target = target * 128 / 100;
                    break;

                case ParameterFaderType.ClearAll:
                    {
                        // FIXME? I *think* this clears all parameter faders.
                        foreach (var pf in _parameterFaders)
                        {
                            pf.Param = ParameterFaderType.None;
                        }
                        return 0;
                    }

                default:
                    Debug.WriteLine("Player.AddParameterFader({0}, {1}, {2}): Unknown parameter", param, target, time);
                    return 0; // Should be -1, but we'll let the script think it worked.
            }

            ParameterFader best = null;
            for (var i = _parameterFaders.Length - 1; i != 0; --i)
            {
                var ptr = _parameterFaders[i];
                if (ptr.Param == param)
                {
                    best = ptr;
                    start = ptr.End;
                    break;
                }
                else if (ptr.Param == ParameterFaderType.None)
                {
                    best = ptr;
                }
            }

            if (best != null)
            {
                best.Param = param;
                best.Start = start;
                best.End = target;
                if (time == 0)
                    best.TotalTime = 1;
                else
                    best.TotalTime = (uint)time * 10000;
                best.CurrentTime = 0;
            }
            else
            {
                Debug.WriteLine("IMuse Player {0}: Out of parameter faders", _id);
                return -1;
            }

            return 0;
        }

        public void Clear()
        {
            if (!_active)
                return;
            Debug.WriteLine("Stopping music {0}", _id);

            if (_parser != null)
            {
                _parser.UnloadMusic();
                _parser = null;
            }
            UninitParts();
            _se.ImFireAllTriggers(_id);
            _active = false;
            _midi = null;
            _id = 0;
            _note_offset = 0;
        }

        public void ClearLoop()
        {
            _loop_counter = 0;
        }

        public void FixAfterLoad()
        {
            _midi = _se.GetBestMidiDriver(_id);
            if (_midi == null)
            {
                Clear();
            }
            else
            {
                StartSeqSound(_id, false);
                SetSpeed(_speed);
                if (_parser != null)
                    _parser.JumpToTick(_music_tick); // start_seqSound already switched tracks
                _isMT32 = _se.IsMT32(_id);
                _isMIDI = _se.IsMIDI(_id);
                _supportsPercussion = _se.SupportsPercussion(_id);
            }
        }

        public Part GetActivePart(byte chan)
        {
            var part = _parts;
            while (part != null)
            {
                if (part.Channel == chan)
                    return part;
                part = part.Next;
            }
            return null;
        }

        public uint GetBeatIndex()
        {
            return (_parser != null ? (uint)(_parser.Tick / TicksPerBeat + 1) : 0);
        }

        public sbyte Detune{ get { return _detune; } }

        public byte GetEffectiveVolume()
        {
            return _vol_eff;
        }

        public int Id
        {
            get
            {
                return _id;
            }
        }

        public MidiDriver MidiDriver
        {
            get
            {
                return _midi;
            }
        }

        public int GetParam(int param, byte chan)
        {
            switch (param)
            {
                case 0:
                    return _priority;
                case 1:
                    return _volume;
                case 2:
                    return _pan;
                case 3:
                    return _transpose;
                case 4:
                    return _detune;
                case 5:
                    return _speed;
                case 6:
                    return _track_index;
                case 7:
                    return (int)GetBeatIndex();
                case 8:
                    return (_parser != null ? _parser.Tick % TicksPerBeat : 0); // _tick_index;
                case 9:
                    return (int)_loop_counter;
                case 10:
                    return (int)_loop_to_beat;
                case 11:
                    return (int)_loop_to_tick;
                case 12:
                    return (int)_loop_from_beat;
                case 13:
                    return (int)_loop_from_tick;
                case 14:
                case 15:
                case 16:
                case 17:
                    return QueryPartParam(param, chan);
                case 18:
                case 19:
                case 20:
                case 21:
                case 22:
                case 23:
                    return _hook.QueryParam(param, chan);
                default:
                    return -1;
            }
        }

        public sbyte Pan { get { return _pan; } }

        public Part Part { get { return _parts; } }

        public Part GetPart(byte chan)
        {
            var part = GetActivePart(chan);
            if (part != null)
                return part;

            part = _se.AllocatePart(_priority, _midi);
            if (part == null)
            {
                Debug.WriteLine("No parts available");
                return null;
            }

            // Insert part into front of parts list
            part.Previous = null;
            part.Next = _parts;
            if (_parts != null)
                _parts.Previous = part;
            _parts = part;

            part.Channel = chan;
            part.Setup(this);

            return part;
        }

        public byte Priority
        {
            get
            {
                return _priority;
            }
        }

        public uint GetTicksPerBeat()
        {
            return TicksPerBeat;
        }

        public sbyte GetTranspose()
        {
            return _transpose;
        }

        public byte Volume
        {
            get
            {
                return _volume;
            }
        }

        public bool IsActive
        {
            get
            {
                return _active;
            }
        }

        public bool IsFadingOut
        {
            get
            {
                for (var i = 0; i < _parameterFaders.Length; ++i)
                {
                    if (_parameterFaders[i].Param == ParameterFaderType.Volume &&
                        _parameterFaders[i].End == 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IsMIDI
        {
            get{ return _isMIDI; }
        }

        public bool IsMT32
        {
            get{ return _isMT32; }
        }

        public bool Jump(uint track, uint beat, uint tick)
        {
            if (_parser == null)
                return false;
            if ((_parser.ActiveTrack = (int)track) != 0)
                _track_index = (int)track;
            if (!_parser.JumpToTick((beat - 1) * TicksPerBeat + tick))
                return false;
            TurnOffPedals();
            return true;
        }

        public void OnTimer()
        {
            // First handle any parameter transitions
            // that are occuring.
            TransitionParameters();

            // Since the volume parameter can cause
            // the player to be deactivated, check
            // to make sure we're still active.
            if (!_active || _parser == null)
                return;

            var target_tick = _parser.Tick;
            var beat_index = target_tick / TicksPerBeat + 1;
            var tick_index = target_tick % TicksPerBeat;

            if (_loop_counter != 0 && (beat_index > _loop_from_beat ||
                (beat_index == _loop_from_beat && tick_index >= _loop_from_tick)))
            {
                _loop_counter--;
                Jump((uint)_track_index, _loop_to_beat, _loop_to_tick);
            }
            _parser.OnTimer();
        }

        public void RemovePart(Part part)
        {
            // Unlink
            if (part.Next != null)
                part.Next.Previous = part.Previous;
            if (part.Previous != null)
                part.Previous.Next = part.Next;
            else
                _parts = part.Next;
            part.Next = part.Previous = null;
        }

        public int Scan(uint totrack, uint tobeat, uint totick)
        {
            if (!_active || _parser == null)
                return -1;

            if (tobeat == 0)
                tobeat++;

            TurnOffParts();
            Array.Clear(_active_notes, 0, _active_notes.Length);
            _scanning = true;

            // If the scan involves a track switch, scan to the end of
            // the current track so that our state when starting the
            // new track is fully up to date.
            if (totrack != _track_index)
                _parser.JumpToTick(uint.MaxValue, true);
            _parser.ActiveTrack = (int)totrack;
            if (!_parser.JumpToTick((tobeat - 1) * TicksPerBeat + totick, true))
            {
                _scanning = false;
                return -1;
            }

            _scanning = false;
            _se.ReallocateMidiChannels(_midi);
            PlayActiveNotes();

            if (_track_index != totrack)
            {
                _track_index = (int)totrack;
                _loop_counter = 0;
            }
            return 0;
        }

        public void SaveOrLoad(Serializer ser)
        {
            var playerEntries = new []
            {
                LoadAndSaveEntry.Create(r => _active = r.ReadBoolean(), w => w.Write(_active), 8),
                LoadAndSaveEntry.Create(r => _id = r.ReadUInt16(), w => w.WriteUInt16(_id), 8),
                LoadAndSaveEntry.Create(r => _priority = r.ReadByte(), w => w.WriteByte(_priority), 8),
                LoadAndSaveEntry.Create(r => _volume = r.ReadByte(), w => w.WriteByte(_volume), 8),
                LoadAndSaveEntry.Create(r => _pan = r.ReadSByte(), w => w.Write(_pan), 8),
                LoadAndSaveEntry.Create(r => _transpose = r.ReadSByte(), w => w.Write(_transpose), 8),
                LoadAndSaveEntry.Create(r => _detune = r.ReadSByte(), w => w.Write(_detune), 8),
                LoadAndSaveEntry.Create(r => VolChan = r.ReadUInt16(), w => w.WriteUInt16(VolChan), 8),
                LoadAndSaveEntry.Create(r => _vol_eff = r.ReadByte(), w => w.WriteByte(_vol_eff), 8),
                LoadAndSaveEntry.Create(r => _speed = r.ReadByte(), w => w.WriteByte(_speed), 8),
                LoadAndSaveEntry.Create(r => r.ReadUInt16(), w => w.WriteUInt16(0), 8, 19), //_song_index
                LoadAndSaveEntry.Create(r => _track_index = r.ReadUInt16(), w => w.WriteUInt16(_track_index), 8),
                LoadAndSaveEntry.Create(r => r.ReadUInt16(), w => w.WriteUInt16(0), 8, 17), //_timer_counter
                LoadAndSaveEntry.Create(r => _loop_to_beat = r.ReadUInt16(), w => w.WriteUInt16(_loop_to_beat), 8),
                LoadAndSaveEntry.Create(r => _loop_from_beat = r.ReadUInt16(), w => w.WriteUInt16(_loop_from_beat), 8),
                LoadAndSaveEntry.Create(r => _loop_counter = r.ReadUInt16(), w => w.WriteUInt16(_loop_counter), 8),
                LoadAndSaveEntry.Create(r => _loop_to_tick = r.ReadUInt16(), w => w.WriteUInt16(_loop_to_tick), 8),
                LoadAndSaveEntry.Create(r => _loop_from_tick = r.ReadUInt16(), w => w.WriteUInt16(_loop_from_tick), 8),
                LoadAndSaveEntry.Create(r => r.ReadUInt32(), w => w.WriteUInt32(0), 8, 19), //_tempo
                LoadAndSaveEntry.Create(r => r.ReadUInt32(), w => w.WriteUInt32(0), 8, 17), //_cur_pos
                LoadAndSaveEntry.Create(r => r.ReadUInt32(), w => w.WriteUInt32(0), 8, 17), //_next_pos
                LoadAndSaveEntry.Create(r => r.ReadUInt32(), w => w.WriteUInt32(0), 8, 17), //_song_offset
                LoadAndSaveEntry.Create(r => r.ReadUInt16(), w => w.WriteUInt16(0), 8, 17), //_tick_index
                LoadAndSaveEntry.Create(r => r.ReadUInt16(), w => w.WriteUInt16(0), 8, 17), //_beat_index
                LoadAndSaveEntry.Create(r => r.ReadUInt16(), w => w.WriteUInt16(0), 8, 17), // _ticks_per_beat
                LoadAndSaveEntry.Create(r => _music_tick = r.ReadUInt32(), w => w.WriteUInt32(_music_tick), 19),
            };

            if (ser.IsLoading && _parser != null)
            {
                _parser = null;
            }
            _music_tick = _parser != null ? (uint)_parser.Tick : 0;

            int num;
            if (!ser.IsLoading)
            {
                num = _parts != null ? Array.IndexOf(_se._parts, _parts) + 1 : 0;
                ser.Writer.WriteUInt16(num);
            }
            else
            {
                num = ser.Reader.ReadUInt16();
                _parts = num != 0 ? _se._parts[num - 1] : null;
            }

            playerEntries.ForEach(e => e.Execute(ser));
            _hook.SaveOrLoad(ser);
            _parameterFaders.ForEach(pf => pf.SaveOrLoad(ser));
        }

        public int SetHook(byte cls, byte value, byte chan)
        {
            return _hook.Set(cls, value, chan);
        }

        public void SetDetune(int detune)
        {
            _detune = (sbyte)detune;
            for (var part = _parts; part != null; part = part.Next)
            {
                part.Detune = part.Detune;
            }
        }

        public void SetOffsetNote(int offset)
        {
            _note_offset = offset;
        }

        public bool SetLoop(uint count, uint tobeat, uint totick, uint frombeat, uint fromtick)
        {
            if (tobeat + 1 >= frombeat)
                return false;

            if (tobeat == 0)
                tobeat = 1;

            // FIXME: Thread safety?
            _loop_counter = 0; // Because of possible interrupts
            _loop_to_beat = tobeat;
            _loop_to_tick = totick;
            _loop_from_beat = frombeat;
            _loop_from_tick = fromtick;
            _loop_counter = count;

            return true;
        }

        public void SetPan(int pan)
        {
            _pan = (sbyte)pan;
            for (var part = _parts; part != null; part = part.Next)
            {
                part.Pan = part.Pan;
            }
        }

        public void SetPriority(int pri)
        {
            _priority = (byte)pri;
            for (var part = _parts; part != null; part = part.Next)
            {
                part.Priority = part.Priority;
            }
            _se.ReallocateMidiChannels(_midi);
        }

        public void SetSpeed(byte speed)
        {
            _speed = speed;
            if (_parser != null)
                _parser.TimerRate = (uint)(((_midi.BaseTempo * speed) >> 7) * _se.TempoFactor / 100);
        }

        public int SetTranspose(byte relative, int b)
        {
            if (b > 24 || b < -24 || relative > 1)
                return -1;
            if (relative != 0)
                b = TransposeClamp(_transpose + b, -24, 24);

            _transpose = (sbyte)b;

            for (var part = _parts; part != null; part = part.Next)
            {
                part.SetTranspose((sbyte)part.Transpose);
            }

            return 0;
        }

        public int SetVolume(byte vol)
        {
            if (vol > 127)
                return -1;

            _volume = vol;
            _vol_eff = (byte)(_se.GetChannelVolume(VolChan) * (vol + 1) >> 7);

            for (var part = _parts; part != null; part = part.Next)
            {
                part.Volume = part.Volume;
            }

            return 0;
        }

        public bool StartSound(int sound, MidiDriver midi)
        {

            // Not sure what the old code was doing,
            // but we'll go ahead and do a similar check.
            var ptr = _se.FindStartOfSound(sound);
            if (ptr == null)
            {
//                Console.Error.WriteLine("Player::startSound(): Couldn't find start of sound {0}", sound);
                return false;
            }

            _isMT32 = _se.IsMT32(sound);
            _isMIDI = _se.IsMIDI(sound);
            _supportsPercussion = _se.SupportsPercussion(sound);

            _parts = null;
            _active = true;
            _midi = midi;
            _id = sound;

            LoadStartParameters(sound);

            for (var i = 0; i < _parameterFaders.Length; ++i)
            {
                _parameterFaders[i].Init();
            }
            HookClear();

            if (StartSeqSound(sound) != 0)
            {
                _active = false;
                _midi = null;
                return false;
            }

            Debug.WriteLine("Starting music {0}", sound);
            return true;
        }

        public int GetMusicTimer()
        {
            return _parser != null ? (int)(_parser.Tick * 2 / _parser.PPQN) : 0;
        }

        // MidiDriver interface
        public override void Send(int b)
        {
            byte cmd = (byte)(b & 0xF0);
            byte chan = (byte)(b & 0x0F);
            byte param1 = (byte)((b >> 8) & 0xFF);
            byte param2 = (byte)((b >> 16) & 0xFF);
            Part part;

            switch (cmd >> 4)
            {
                case 0x8: // Key Off
                    if (!_scanning)
                    {
                        if ((part = GetPart(chan)) != null)
                            part.NoteOff(param1);
                    }
                    else
                    {
                        _active_notes[param1] &= (ushort)~(1 << chan);
                    }
                    break;

                case 0x9: // Key On
                    param1 += (byte)_note_offset;
                    if (!_scanning)
                    {
                        if (_isMT32 && !_se.IsNativeMT32)
                            param2 = (byte)((((param2 * 3) >> 2) + 32) & 0x7F);
                        if ((part = GetPart(chan)) != null)
                            part.NoteOn(param1, param2);
                    }
                    else
                    {
                        _active_notes[param1] |= (ushort)(1 << chan);
                    }
                    break;

                case 0xB: // Control Change
                    part = (param1 == 123 ? GetActivePart(chan) : GetPart(chan));
                    if (part == null)
                        break;

                    switch (param1)
                    {
                        case 0: // Bank select. Not supported
                            break;
                        case 1: // Modulation Wheel
                            part.ModulationWheel(param2);
                            break;
                        case 7: // Volume
                            part.Volume = param2;
                            break;
                        case 10: // Pan Position
                            part.Pan = param2 - 0x40;
                            break;
                        case 16: // Pitchbend Factor(non-standard)
                            part.PitchBendFactor(param2);
                            break;
                        case 17: // GP Slider 2
                            part.Detune = param2 - 0x40;
                            break;
                        case 18: // GP Slider 3
                            part.Priority = (param2 - 0x40);
                            _se.ReallocateMidiChannels(_midi);
                            break;
                        case 64: // Sustain Pedal
                            part.Sustain(param2 != 0);
                            break;
                        case 91: // Effects Level
                            part.EffectLevel(param2);
                            break;
                        case 93: // Chorus Level
                            part.ChorusLevel(param2);
                            break;
                        case 116: // XMIDI For Loop. Not supported
                            // Used in the ending sequence of puttputt
                            break;
                        case 117: // XMIDI Next/Break. Not supported
                            // Used in the ending sequence of puttputt
                            break;
                        case 123: // All Notes Off
                            part.AllNotesOff();
                            break;
                        default:
//                            Console.Error.WriteLine("Player::send(): Invalid control change {0}", param1);
                            break;
                    }
                    break;

                case 0xC: // Program Change
                    part = GetPart(chan);
                    if (part != null)
                    {
                        if (_isMIDI)
                        {
                            if (param1 < 128)
                                part.ProgramChange(param1);
                        }
                        else
                        {
                            if (param1 < 32)
                                part.LoadGlobalInstrument(param1);
                        }
                    }
                    break;

                case 0xE: // Pitch Bend
                    part = GetPart(chan);
                    if (part != null)
                        part.PitchBend((short)(((param2 << 7) | param1) - 0x2000));
                    break;

                case 0xA: // Aftertouch
                case 0xD: // Channel Pressure
                case 0xF: // Sequence Controls
                    break;

                default:
                    if (!_scanning)
                    {
//                        Console.Error.WriteLine("Player::send(): Invalid command {0}", cmd);
                        Clear();
                    }
                    break;
            }
            return;
        }

        const int YM2612SysExId = 0x7C;
        const int IMuseSysExId = 0x7D;
        const int RolandSysExId = 0x41;

        static byte[] Extract(byte[] msg, int offset)
        {
            var tmp = new byte[msg.Length - offset];
            Array.Copy(msg, offset, tmp, 0, tmp.Length);
            return tmp;
        }

        public override void SysEx(byte[] msg, ushort length)
        {
            int p = 0;
            byte a;
            byte[] buf = new byte[128];
            Part part;

            // Check SysEx manufacturer.
            a = msg[p++];
            --length;
            if (a != IMuseSysExId)
            {
                if (a == RolandSysExId)
                {
                    // Roland custom instrument definition.
                    if (_isMIDI || _isMT32)
                    {
                        part = GetPart((byte)(msg[p] & 0x0F));
                        if (part != null)
                        {
                            part.Instrument.Roland(Extract(msg, p - 1));
                            if (part.ClearToTransmit())
                                part.Instrument.Send(part.MidiChannel);
                        }
                    }
                }
                else if (a == YM2612SysExId)
                {
                    // FM-TOWNS custom instrument definition
                    _midi.SysExCustomInstrument(msg[p], NScumm.Core.Audio.SoftSynth.AdlibMidiDriver.ToType("EUP "), Extract(msg, p + 1));
                }
                else
                {
                    // SysEx manufacturer 0x97 has been spotted in the
                    // Monkey Island 2 AdLib music, so don't make this a
                    // fatal error. See bug #1481383.
                    // The Macintosh version of Monkey Island 2 simply
                    // ignores these SysEx events too.
//                    if (a == 0)
//                        Console.Error.WriteLine("Unknown SysEx manufacturer 0x00 0x{0:X2} 0x{1:X2}", msg[p], msg[p + 1]);
//                    else
//                        Console.Error.WriteLine("Unknown SysEx manufacturer 0x{0:X2}", (int)a);
                }
                return;
            }
            --length;

            // Too big?
            if (length >= buf.Length * 2)
                return;

            if (_se.Sysex != null)
                _se.Sysex(this, Extract(msg, p), length);
        }

        public override void MetaEvent(byte type, byte[] data, ushort length)
        {
            if (type == 0x2F)
                Clear();
        }
    }
    
}
