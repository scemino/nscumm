//
//  IMuseInternal.cs
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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Audio.IMuse
{
    /// <summary>
    /// SCUMM implementation of IMuse.
    /// This class implements the IMuse mixin interface for the SCUMM environment.
    /// </summary>
    class IMuseInternal : IIMuse
    {
        const int TriggerId = 0;
        const int CommandId = 1;

        protected bool _native_mt32;
        protected bool _enable_gs;
        protected MidiDriver _midi_adlib;
        protected MidiDriver _midi_native;
        protected TimerCallbackInfo _timer_info_adlib;
        protected TimerCallbackInfo _timer_info_native;
    
        protected GameId _game_id;

        public GameId GameId { get { return _game_id; } }
    
        // Plug-in SysEx handling. Right now this only supports one
        // custom SysEx handler for the hardcoded IMUSE_SYSEX_ID
        // manufacturer code. TODO: Expand this to support multiple
        // SysEx handlers for client-specified manufacturer codes.
        internal protected SysExFunc Sysex { get; private set; }

        protected object _mutex = new object();
    
        protected bool _paused;
        protected bool _initialized;

        public int TempoFactor { get; protected set; }

        protected int _player_limit;
        // Limits how many simultaneous music tracks are played
        protected bool _recycle_players;
        // Can we stop a player in order to start another one?
    
        protected uint _queue_end, _queue_pos, _queueSound;
        protected bool _queue_adding;
    
        protected byte _queue_marker;
        protected bool _queue_cleared;
        protected byte _master_volume;
        // Master volume. 0-255
        protected byte _music_volume;
        // Global music volume. 0-255
    
        protected ushort _trigger_count;
        /// <summary>
        // Sam & Max triggers
        /// </summary>
        internal protected ImTrigger[] _snm_triggers;
        private ushort _snm_trigger_index;
    
        protected ushort[] _channel_volume;
        protected ushort[] _channel_volume_eff;
        protected ushort[] _volchan_table;
        internal protected Player[] _players;
        internal protected Part[] _parts;

        public bool PcSpeaker { get; private set; }

        protected Instrument[] _global_instruments;
        protected CommandQueue[] _cmd_queue;
        protected DeferredCommand[] _deferredCommands;

        public IMuseInternal()
        {
            _snm_triggers = new ImTrigger[16];
            for (int i = 0; i < _snm_triggers.Length; i++)
            {
                _snm_triggers[i] = new ImTrigger();
            }
            _channel_volume = new ushort[8];
            _channel_volume_eff = new ushort[8];
            _volchan_table = new ushort[8];
            _players = new Player[8];
            for (int i = 0; i < _players.Length; i++)
            {
                _players[i] = new Player();
            }
            _player_limit = _players.Length;
            _parts = new Part[32];
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i] = new Part();
            }
            _global_instruments = new Instrument[32];
            for (int i = 0; i < _global_instruments.Length; i++)
            {
                _global_instruments[i] = new Instrument();
            }
            _cmd_queue = new CommandQueue[64];
            for (int i = 0; i < _cmd_queue.Length; i++)
            {
                _cmd_queue[i] = new CommandQueue();
            }
            _deferredCommands = new DeferredCommand[4];
            for (int i = 0; i < _deferredCommands.Length; i++)
            {
                _deferredCommands[i] = new DeferredCommand();
            }
            _timer_info_adlib = new TimerCallbackInfo();
            _timer_info_native = new TimerCallbackInfo();
        }

        internal protected int Initialize(MidiDriver nativeMidiDriver, MidiDriver adlibMidiDriver)
        {
            _midi_native = nativeMidiDriver;
            _midi_adlib = adlibMidiDriver;
            if (nativeMidiDriver != null)
            {
                _timer_info_native.IMuse = this;
                _timer_info_native.Driver = nativeMidiDriver;
                InitMidiDriver(_timer_info_native);
            }
            if (adlibMidiDriver != null)
            {
                _timer_info_adlib.IMuse = this;
                _timer_info_adlib.Driver = adlibMidiDriver;
                InitMidiDriver(_timer_info_adlib);
            }

            if (TempoFactor == 0)
                TempoFactor = 100;
            _master_volume = 255;

            for (var i = 0; i != 8; i++)
                _channel_volume[i] = _channel_volume_eff[i] = _volchan_table[i] = 127;

            InitPlayers();
            InitQueue();
            InitParts();

            _initialized = true;

            return 0;
        }

        protected void InitMidiDriver(TimerCallbackInfo info)
        {
            // Open MIDI driver
            var result = info.Driver.Open();
//            if (result != MidiDriverError.None)
//                Console.Error.WriteLine("IMuse initialization - {0}", MidiDriver.GetErrorName(result));

            // Connect to the driver's timer
            info.Driver.SetTimerCallback(info, MidiTimerCallback);
        }

        protected static void MidiTimerCallback(object data)
        {
            TimerCallbackInfo info = (TimerCallbackInfo)data;
            info.IMuse.OnTimer(info.Driver);
        }

        void IIMuse.OnTimer(MidiDriver midi)
        {
            OnTimer(midi);
        }

        protected void OnTimer(MidiDriver midi)
        {
            lock (_mutex)
            {
                if (_paused || !_initialized)
                    return;

                if (midi == _midi_native || _midi_native == null)
                    HandleDeferredCommands(midi);
                SequencerTimers(midi);
            }
        }

        [Flags]
        internal protected enum ChunkType
        {
            MThd = 1,
            FORM = 2,
            MDhd = 4,
            // Used in MI2 and INDY4. Contain certain start parameters (priority, volume, etc. ) for the player.
            MDpg = 8
            // These chunks exist in DOTT and SAMNMAX. They don't get processed, however.}
        }


        internal protected byte[] FindStartOfSound(int sound, ChunkType ct = ChunkType.MThd | ChunkType.FORM)
        {
            int size, pos;

            var ptr = ScummEngine.Instance.ResourceManager.GetSound(ScummEngine.Instance.Sound.MusicType, sound);

            if (ptr == null)
            {
                Debug.WriteLine("IMuseInternal::findStartOfSound(): Sound {0} doesn't exist", sound);
                return null;
            }

            // Check for old-style headers first, like 'RO'
            const ChunkType trFlag = ChunkType.MThd | ChunkType.FORM;
            if (System.Text.Encoding.UTF8.GetString(ptr, 0, 3) == "ROL")
                return ct == trFlag ? ptr : null;
            if (System.Text.Encoding.UTF8.GetString(ptr, 4, 2) == "SO")
            {
                if (ct == trFlag)
                {
                    var tmp = new byte[ptr.Length - 4];
                    Array.Copy(ptr, 4, tmp, 0, tmp.Length);
                    return tmp;
                }
                return null;
            }

            var ids = new string[]
            {
                "MThd",
                "FORM",
                "MDhd",
                "MDpg"
            };

            using (var ms = new MemoryStream(ptr))
            {
                var br = new BinaryReader(ms);
                ms.Seek(4, SeekOrigin.Current);
                size = (int)br.ReadUInt32BigEndian();

                // Okay, we're looking for one of those things: either
                // an 'MThd' tag (for SMF), or a 'FORM' tag (for XMIDI).
                size = 48; // Arbitrary; we should find our tag within the first 48 bytes of the resource
                pos = 0;
                while (pos < size)
                {
                    for (int i = 0; i < ids.Length; ++i)
                    {
                        var sig = System.Text.Encoding.UTF8.GetString(br.ReadBytes(4));
                        ms.Seek(-4, SeekOrigin.Current);
                        if ((((int)ct) & (1 << i)) != 0 && (sig == ids[i]))
                        {
                            var tmp = new byte[ptr.Length - ms.Position];
                            Array.Copy(ptr, (int)ms.Position, tmp, 0, tmp.Length);
                            return tmp;
                        }
                    }
                    ++pos; // We could probably iterate more intelligently
                    ms.Seek(1, SeekOrigin.Current);
                }

                if (ct == (ChunkType.MThd | ChunkType.FORM))
                    Debug.WriteLine("IMuseInternal.FindStartOfSound(): Failed to align on sound {0}", sound);
            }

            return null;
        }

        internal protected bool IsMT32(int sound)
        {
            var ptr = ScummEngine.Instance.ResourceManager.GetSound(ScummEngine.Instance.Sound.MusicType, sound);
            if (ptr == null)
                return false;

            var tag = System.Text.Encoding.UTF8.GetString(ptr, 0, 4);
            switch (tag)
            {
                case "ADL ":
                case "ASFX": // Special AD class for old AdLib sound effects
                case "SPK ":
                    return false;

                case "AMI ":
                case "ROL ":
                    return true;

                case "MAC ": // Occurs in the Mac version of FOA and MI2
                    return false;

                case "GMD ":
                    return false;

                case "MIDI": // Occurs in Sam & Max
                    // HE games use Roland music
                    if (ptr[8] == 'H' && ptr[9] == 'S')
                        return true;
                    else
                        return false;
            }

            // Old style 'RO' has equivalent properties to 'ROL'
            if (System.Text.Encoding.UTF8.GetString(ptr, 0, 2) == "RO")
                return true;
            // Euphony tracks show as 'SO' and have equivalent properties to 'ADL'
            if (System.Text.Encoding.UTF8.GetString(ptr, 4, 2) == "SO")
                return false;

//            Console.Error.WriteLine("Unknown music type: '{0}'", tag);

            return false;
        }

        internal protected bool IsMIDI(int sound)
        {
            var ptr = ScummEngine.Instance.ResourceManager.GetSound(ScummEngine.Instance.Sound.MusicType, sound);
            if (ptr == null)
                return false;

            var tag = System.Text.Encoding.UTF8.GetString(ptr, 0, 4);
            switch (tag)
            {
                case "ADL ":
                case "ASFX": // Special AD class for old AdLib sound effects
                case "SPK ":
                    return false;

                case "AMI ":
                case "ROL ":
                    return true;

                case "MAC ": // Occurs in the Mac version of FOA and MI2
                    return true;

                case "GMD ":
                case "MIDI": // Occurs in Sam & Max
                    return true;
            }

            // Old style 'RO' has equivalent properties to 'ROL'
            if (System.Text.Encoding.UTF8.GetString(ptr, 0, 2) == "RO")
                return true;
            // Euphony tracks show as 'SO' and have equivalent properties to 'ADL'
            // FIXME: Right now we're pretending it's GM.
            if (System.Text.Encoding.UTF8.GetString(ptr, 4, 2) == "SO")
                return true;

//            Console.Error.WriteLine("Unknown music type: '{0}'", tag);

            return false;
        }

        internal protected bool SupportsPercussion(int sound)
        {
            var ptr = ScummEngine.Instance.ResourceManager.GetSound(ScummEngine.Instance.Sound.MusicType, sound);
            if (ptr == null)
                return false;

            var tag = System.Text.Encoding.UTF8.GetString(ptr, 0, 4);
            switch (tag)
            {
                case "ADL ":
                case "ASFX": // Special AD class for old AdLib sound effects
                case "SPK ":
                    return false;

                case "AMI ":
                case "ROL ":
                    return true;

                case "MAC ": // Occurs in the Mac version of FOA and MI2
                    // This is MIDI, i.e. uses MIDI style program changes, but without a
                    // special percussion channel.
                    return false;

                case "GMD ":
                case "MIDI": // Occurs in Sam & Max
                    return true;
            }

            // Old style 'RO' has equivalent properties to 'ROL'
            if (System.Text.Encoding.UTF8.GetString(ptr, 0, 2) == "RO")
                return true;
            // Euphony tracks show as 'SO' and have equivalent properties to 'ADL'
            // FIXME: Right now we're pretending it's GM.
            if (System.Text.Encoding.UTF8.GetString(ptr, 4, 2) == "SO")
                return true;

//            Console.Error.WriteLine("Unknown music type: '{0}'", tag);

            return false;
        }

        protected int GetQueueSoundStatus(int sound)
        {
            var j = _queue_pos;
            var i = _queue_end;

            while (i != j)
            {
                var a = _cmd_queue[i].array;
                if (a[0] == CommandId && a[1] == 8 && a[2] == sound)
                    return 2;
                i = (uint)((i + 1) % _cmd_queue.Length);
            }

            for (i = 0; i < _deferredCommands.Length; ++i)
            {
                if (_deferredCommands[i].TimeLeft != 0 && _deferredCommands[i].A == 8 &&
                    _deferredCommands[i].B == sound)
                {
                    return 2;
                }
            }

            return 0;
        }

        void IIMuse.HandleMarker(int id, int data)
        {
            HandleMarker(id, data);
        }

        public void HandleMarker(int id, int data)
        {
            if ((_queue_end == _queue_pos) || (_queue_adding && _queueSound == id && data == _queue_marker))
                return;

            var p = _cmd_queue[_queue_end].array;
            if (p[0] != TriggerId || id != p[1] || data != p[2])
                return;

            _trigger_count--;
            _queue_cleared = false;
            _queue_end = (uint)((_queue_end + 1) % _cmd_queue.Length);

            while (_queue_end != _queue_pos && _cmd_queue[_queue_end].array[0] == CommandId && !_queue_cleared)
            {
                p = _cmd_queue[_queue_end].array;
                DoCommandInternal(p[1], p[2], p[3], p[4], p[5], p[6], p[7], 0);
                _queue_end = (uint)((_queue_end + 1) % _cmd_queue.Length);
            }
        }

        internal protected int GetChannelVolume(uint a)
        {
            if (a < 8)
                return _channel_volume_eff[a];
            return (_master_volume * _music_volume / 255) / 2;
        }

        protected void InitGM(MidiDriver midi)
        {
            throw new NotImplementedException();
        }

        protected void InitMT32(MidiDriver midi)
        {
            throw new NotImplementedException();
        }

        protected void InitPlayers()
        {
            for (var i = 0; i < _players.Length; i++)
            {
                var player = _players[i];
                player._se = this;
                player.Clear();
            }
        }

        protected void InitParts()
        {
            for (var i = 0; i < _parts.Length; i++)
            {
                var part = _parts[i];
                part.Init();
                part.Se = this;
                part.Slot = i;
            }
        }

        protected void InitQueue()
        {
            _queue_adding = false;
            _queue_pos = 0;
            _queue_end = 0;
            _trigger_count = 0;
        }

        protected void SequencerTimers(MidiDriver midi)
        {
            for (var i = _players.Length; i != 0; i--)
            {
                var player = _players[i - 1];
                if (player.IsActive && player.MidiDriver == midi)
                {
                    player.OnTimer();
                }
            }
        }

        internal protected MidiDriver GetBestMidiDriver(int sound)
        {
            MidiDriver driver;

            if (IsMIDI(sound))
            {
                if (_midi_native != null)
                {
                    driver = _midi_native;
                }
                else
                {
                    // Route it through AdLib anyway.
                    driver = _midi_adlib;
                }
            }
            else
            {
                driver = _midi_adlib;
            }
            return driver;
        }

        protected Player AllocatePlayer(byte priority)
        {
            Player best = null;
            byte bestpri = 255;

            for (var i = 0; i < _player_limit; i++)
            {
                var player = _players[i];
                if (!player.IsActive)
                    return player;
                if (player.Priority < bestpri)
                {
                    best = player;
                    bestpri = player.Priority;
                }
            }

            if (bestpri < priority || _recycle_players)
                return best;

            Debug.WriteLine("Denying player request");
            return null;
        }

        internal protected Part AllocatePart(byte pri, MidiDriver midi)
        {
            Part best = null;

            for (var i = 0; i < _parts.Length; i++)
            {
                var part = _parts[i];
                if (part.Player == null)
                {
                    return part;
                }
                if (pri >= part.PriorityEffective)
                {
                    pri = (byte)part.PriorityEffective;
                    best = part;
                }
            }

            if (best != null)
            {
                best.Uninit();
                ReallocateMidiChannels(midi);
            }
            else
            {
                Debug.WriteLine("Denying part request");
            }
            return best;
        }

        protected int ImSetTrigger(int sound, int id, int a, int b, int c, int d, int e, int f, int g, int h)
        {
            // Sam & Max: ImSetTrigger.
            // Sets a trigger for a particular player and
            // marker ID, along with doCommand parameters
            // to invoke at the marker. The marker is
            // represented by MIDI SysEx block 00 xx(F7)
            // where "xx" is the marker ID.
            ushort oldest_trigger = 0;
            ImTrigger oldest_ptr = null;
            ImTrigger trig = null;
            int i;

            for (i = 0; i < _snm_triggers.Length; i++)
            {
                trig = _snm_triggers[i];
                if (trig.Id == 0)
                    break;
                // We used to only compare 'id' and 'sound' here, but at least
                // at the Dino Bungie Memorial that causes the music to stop
                // after getting the T-Rex tooth. See bug #888161.
                if (trig.Id == id && trig.Sound == sound && trig.Command[0] == a)
                    break;

                ushort diff;
                if (trig.Expire <= _snm_trigger_index)
                    diff = (ushort)(_snm_trigger_index - trig.Expire);
                else
                    diff = (ushort)(0x10000 - trig.Expire + _snm_trigger_index);

                if (oldest_ptr == null || oldest_trigger < diff)
                {
                    oldest_ptr = trig;
                    oldest_trigger = diff;
                }
            }

            // If we didn't find a trigger, see if we can expire one.
            if (i == _snm_triggers.Length)
            {
                if (oldest_ptr == null)
                    return -1;
                trig = oldest_ptr;
            }

            trig.Id = (byte)id;
            trig.Sound = sound;
            trig.Expire = (ushort)(++_snm_trigger_index & 0xFFFF);
            trig.Command[0] = a;
            trig.Command[1] = b;
            trig.Command[2] = c;
            trig.Command[3] = d;
            trig.Command[4] = e;
            trig.Command[5] = f;
            trig.Command[6] = g;
            trig.Command[7] = h;

            // If the command is to start a sound, stop that sound if it's already playing.
            // This fixes some carnival music problems.
            // NOTE: We ONLY do this if the sound that will trigger the command is actually
            // playing. Otherwise, there's a problem when exiting and re-entering the
            // Bumpusville mansion. Ref Bug #780918.
            if (trig.Command[0] == 8 && GetSoundStatusInternal(trig.Command[1], true) != 0 && GetSoundStatusInternal(sound, true) != 0)
                StopSoundInternal(trig.Command[1]);
            return 0;
        }

        protected int ImClearTrigger(int sound, int id)
        {
            int count = 0;
            foreach (var trig in _snm_triggers)
            {
                if ((sound == -1 || trig.Sound == sound) && trig.Id != 0 && (id == -1 || trig.Id == id))
                {
                    trig.Sound = trig.Id = 0;
                    ++count;
                }
            }
            return (count > 0) ? 0 : -1;
        }

        internal protected int ImFireAllTriggers(int sound)
        {
            if (sound == 0)
                return 0;
            int count = 0;
            foreach (var trig in _snm_triggers)
            {
                if (trig.Sound == sound)
                {
                    trig.Sound = trig.Id = 0;
                    DoCommandInternal(8, trig.Command);
                    ++count;
                }
            }
            return (count > 0) ? 0 : -1;
        }

        protected void AddDeferredCommand(int time, int a, int b, int c, int d, int e, int f)
        {
            var cmd = _deferredCommands.FirstOrDefault(o => o.TimeLeft == 0);

            if (cmd != null)
            {
                cmd.TimeLeft = (uint)time * 10000;
                cmd.A = a;
                cmd.B = b;
                cmd.C = c;
                cmd.D = d;
                cmd.E = e;
                cmd.F = f;
            }
        }

        protected void HandleDeferredCommands(MidiDriver midi)
        {
            uint advance = midi.BaseTempo;

            foreach (var cmd in _deferredCommands)
            {
                if (cmd.TimeLeft == 0)
                    continue;
                if (cmd.TimeLeft <= advance)
                {
                    DoCommandInternal(cmd.A, cmd.B, cmd.C, cmd.D, cmd.E, cmd.F, 0, 0);
                    cmd.TimeLeft = advance;
                }
                cmd.TimeLeft -= advance;
            }
        }

        protected int EnqueueCommand(int a, int b, int c, int d, int e, int f, int g)
        {
            var i = _queue_pos;

            if (i == _queue_end)
                return -1;

            if (a == -1)
            {
                _queue_adding = false;
                _trigger_count++;
                return 0;
            }

            var p = _cmd_queue[_queue_pos].array;
            p[0] = CommandId;
            p[1] = a;
            p[2] = b;
            p[3] = c;
            p[4] = d;
            p[5] = e;
            p[6] = f;
            p[7] = g;

            i = (uint)((i + 1) % _cmd_queue.Length);

            if (_queue_end != i)
            {
                _queue_pos = i;
                return 0;
            }
            else
            {
                _queue_pos = (uint)((i - 1) % _cmd_queue.Length);
                return -1;
            }
        }

        protected int EnqueueTrigger(int sound, int marker)
        {
            var pos = _queue_pos;

            var p = _cmd_queue[pos].array;
            p[0] = TriggerId;
            p[1] = sound;
            p[2] = marker;

            pos = (uint)((pos + 1) % _cmd_queue.Length);
            if (_queue_end == pos)
            {
                _queue_pos = (uint)((pos - 1) % _cmd_queue.Length);
                return -1;
            }

            _queue_pos = pos;
            _queue_adding = true;
            _queueSound = (uint)sound;
            _queue_marker = (byte)marker;
            return 0;
        }

        int IIMuse.ClearQueue()
        {
            return ClearQueue();
        }

        protected int ClearQueue()
        {
            _queue_adding = false;
            _queue_cleared = true;
            _queue_pos = 0;
            _queue_end = 0;
            _trigger_count = 0;
            return 0;
        }

        protected int QueryQueue(int param)
        {
            switch (param)
            {
                case 0: // Get trigger count
                    return _trigger_count;
                case 1: // Get trigger type
                    if (_queue_end == _queue_pos)
                        return -1;
                    return _cmd_queue[_queue_end].array[1];
                case 2: // Get trigger sound
                    if (_queue_end == _queue_pos)
                        return 0xFF;
                    return _cmd_queue[_queue_end].array[2];
                default:
                    return -1;
            }
        }

        protected Player FindActivePlayer(int id)
        {
            foreach (var player in _players)
            {
                if (player.IsActive && player.Id == id)
                    return player;
            }
            return null;
        }

        protected int GetVolchanEntry(uint a)
        {
            if (a < 8)
                return _volchan_table[a];
            return -1;
        }

        protected int SetVolchanEntry(int sound, uint volchan)
        {
            var r = GetVolchanEntry(volchan);
            if (r == -1)
                return -1;

            if (r >= 8)
            {
                var player = FindActivePlayer(sound);
                if (player != null && player.VolChan != volchan)
                {
                    player.VolChan = volchan;
                    player.SetVolume(player.Volume);
                    return 0;
                }
                return -1;
            }
            else
            {
                Player best = null;
                var num = 0;
                Player sameid = null;
                foreach (var player in _players)
                {
                    if (player.IsActive)
                    {
                        if (player.VolChan == volchan)
                        {
                            num++;
                            if (best == null || player.Priority <= best.Priority)
                                best = player;
                        }
                        else if (player.Id == sound)
                        {
                            sameid = player;
                        }
                    }
                }
                if (sameid == null)
                    return -1;
                var p = _players.LastOrDefault();
                if (num >= r)
                    best.Clear();
                p.VolChan = volchan;
                p.SetVolume(p.Volume);
                return 0;
            }
        }

        protected int SetChannelVolume(uint chan, uint vol)
        {
            if (chan >= 8 || vol > 127)
                return -1;

            _channel_volume[chan] = (ushort)vol;
            _channel_volume_eff[chan] = (ushort)(_master_volume * _music_volume * vol / 255 / 255);
            UpdateVolumes();
            return 0;
        }

        protected void UpdateVolumes()
        {
            foreach (var player in _players)
            {
                if (player.IsActive)
                    player.SetVolume(player.Volume);
            }
        }

        protected int SetVolchan(int a, int b)
        {
            if (a >= 8)
                return -1;
            _volchan_table[a] = (ushort)b;
            return 0;
        }

        protected void FixPartsAfterLoad()
        {
            foreach (var part in _parts)
                if (part.Player != null)
                    part.FixAfterLoad();
        }

        protected void FixPlayersAfterLoad(ScummEngine scumm)
        {
            foreach (var player in _players)
            {
                if (player.IsActive)
                {
//                        scumm..getResourceAddress(rtSound, player.getID());
                    player.FixAfterLoad();
                }
            }
        }

        protected int SetImuseMasterVolume(uint vol)
        {
            if (vol > 255)
                vol = 255;
            if (_master_volume == vol)
                return 0;
            _master_volume = (byte)vol;
            vol = (uint)_master_volume * _music_volume / 255;
            for (var i = 0; i < _channel_volume.Length; i++)
            {
                _channel_volume_eff[i] = (ushort)(_channel_volume[i] * vol / 255);
            }
            if (!_paused)
                UpdateVolumes();
            return 0;
        }

        internal protected void ReallocateMidiChannels(MidiDriver midi)
        {
            Part part, hipart;
            byte hipri, lopri;

            while (true)
            {
                hipri = 0;
                hipart = null;
                for (var i = 0; i < 32; i++)
                {
                    part = _parts[i];
                    if (part.Player != null && part.Player.MidiDriver == midi &&
                        !part.Percussion && part.On &&
                        part.MidiChannel == null && part.PriorityEffective >= hipri)
                    {
                        hipri = (byte)part.PriorityEffective;
                        hipart = part;
                    }
                }

                if (hipart == null)
                    return;

                if ((hipart.MidiChannel = midi.AllocateChannel()) == null)
                {
                    lopri = 255;
                    Part lopart = null;
                    for (var i = 0; i < 32; i++)
                    {
                        part = _parts[i];
                        if (part.MidiChannel != null && part.MidiChannel.Device == midi && part.PriorityEffective <= lopri)
                        {
                            lopri = (byte)part.PriorityEffective;
                            lopart = part;
                        }
                    }

                    if (lopart == null || lopri >= hipri)
                        return;
                    lopart.Off();

                    if ((hipart.MidiChannel = midi.AllocateChannel()) == null)
                        return;
                }
                hipart.SendAll();
            }
        }

        public void SetGlobalInstrument(byte slot, byte[] data)
        {
            if (slot < 32)
            {
                if (PcSpeaker)
                {
                    _global_instruments[slot].PcSpk(data);
                }
                else
                    _global_instruments[slot].Adlib(data);
            }
        }

        internal protected void CopyGlobalInstrument(byte slot, Instrument dest)
        {
            if (slot >= 32)
                return;

            // Both the AdLib code and the PC Speaker code use an all zero instrument
            // as default in the original, thus we do the same.
            // PC Speaker instrument size is 23, while AdLib instrument size is 30.
            // Thus we just use a 30 byte instrument data array as default.
            var defaultInstr = new byte[PcSpeaker ? 23 : 30];

            if (_global_instruments[slot].IsValid)
            {
                // In case we have an valid instrument set up, copy it to the part.
                _global_instruments[slot].CopyTo(dest);
            }
            else if (PcSpeaker)
            {
                Debug.WriteLine("Trying to use non-existent global PC Speaker instrument {0}", slot);
                dest.PcSpk(defaultInstr);
            }
            else
            {
                Debug.WriteLine("Trying to use non-existent global AdLib instrument {0}", slot);
                dest.Adlib(defaultInstr);
            }
        }

        internal protected bool IsNativeMT32{ get { return _native_mt32; } }
    
        // Internal mutex-free versions of the IMuse and MusicEngine methods.
        protected bool StartSoundInternal(int sound, int offset = 0)
        {
            // Do not start a sound if it is already set to start on an ImTrigger
            // event. This fixes carnival music problems where a sound has been set
            // to trigger at the right time, but then is started up immediately
            // anyway, only to be restarted later when the trigger occurs.
            //
            // However, we have to make sure the sound with the trigger is actually
            // playing, otherwise the music may stop when Sam and Max are thrown
            // out of Bumpusville, because entering the mansion sets up a trigger
            // for a sound that isn't necessarily playing. This is somewhat related
            // to bug #780918.

            foreach (var trigger in _snm_triggers)
            {
                if (trigger.Sound != 0 && trigger.Id != 0 && trigger.Command[0] == 8 && trigger.Command[1] == sound && GetSoundStatusInternal(trigger.Sound, true) != 0)
                    return false;
            }

            var ptr = FindStartOfSound(sound);
            if (ptr == null)
            {
                Debug.WriteLine("IMuseInternal::startSound(): Couldn't find sound {0}", sound);
                return false;
            }

            // Check which MIDI driver this track should use.
            // If it's NULL, it ain't something we can play.
            var driver = GetBestMidiDriver(sound);
            if (driver == null)
                return false;

            // If the requested sound is already playing, start it over
            // from scratch. This was originally a hack to prevent Sam & Max
            // iMuse messiness while upgrading the iMuse engine, but it
            // is apparently necessary to deal with fade-and-restart
            // race conditions that were observed in MI2. Reference
            // Bug #590511 and Patch #607175 (which was reversed to fix
            // an FOA regression: Bug #622606).
            var player = FindActivePlayer(sound);
            if (player == null)
            {
                ptr = FindStartOfSound(sound, ChunkType.MDhd);
                int size = 128;
                if (ptr != null)
                {
                    using (var br = new BinaryReader(new MemoryStream(ptr)))
                    {
                        br.BaseStream.Seek(4, SeekOrigin.Begin);
                        var tmp = br.ReadUInt32BigEndian();
                        size = tmp != 0 && ptr[10] != 0 ? ptr[10] : 128;
                    }
                }
                player = AllocatePlayer((byte)size);
            }

            if (player == null)
                return false;

            // WORKAROUND: This is to work around a problem at the Dino Bungie
            // Memorial.
            //
            // There are three pieces of music involved here:
            //
            // 80 - Main theme (looping)
            // 81 - Music when entering Rex's and Wally's room (not looping)
            // 82 - Music when listening to Rex or Wally
            //
            // When entering, tune 81 starts, tune 80 is faded down (not out) and
            // a trigger is set in tune 81 to fade tune 80 back up.
            //
            // When listening to Rex or Wally, tune 82 is started, tune 81 is faded
            // out and tune 80 is faded down even further.
            //
            // However, when tune 81 is faded out its trigger will cause tune 80 to
            // fade back up, resulting in two tunes being played simultaneously at
            // full blast. It's no use trying to keep tune 81 playing at volume 0.
            // It doesn't loop, so eventually it will terminate on its own.
            //
            // I don't know how the original interpreter handled this - or even if
            // it handled it at all - but it looks like sloppy scripting to me. Our
            // workaround is to clear the trigger if the player listens to Rex or
            // Wally before tune 81 has finished on its own.

            if (_game_id == GameId.SamNMax && sound == 82 && GetSoundStatusInternal(81, false) != 0)
                ImClearTrigger(81, 1);

            player.Clear();
            player.SetOffsetNote(offset);
            return player.StartSound(sound, driver);
        }

        protected int StopSoundInternal(int sound)
        {
            int r = -1;
            var player = FindActivePlayer(sound);
            if (player != null)
            {
                player.Clear();
                r = 0;
            }
            return r;
        }

        protected int StopAllSoundsInternal()
        {
            ClearQueue();
            foreach (var player in _players)
            {
                if (player.IsActive)
                    player.Clear();
            }
            return 0;
        }

        protected int GetSoundStatusInternal(int sound, bool ignoreFadeouts)
        {
            foreach (var player in _players)
            {
                if (player.IsActive && (!ignoreFadeouts || !player.IsFadingOut))
                {
                    if (sound == -1)
                        return player.Id;
                    else if (player.Id == sound)
                        return 1;
                }
            }
            return (sound == -1) ? 0 : GetQueueSoundStatus(sound);
        }

        protected int DoCommandInternal(int a, int b, int c, int d, int e, int f, int g, int h)
        {
            var args = new int[8]{ a, b, c, d, e, f, g, h };
            return DoCommandInternal(8, args);
        }

        protected int DoCommandInternal(int numargs, int[] a)
        {
            if (numargs < 1)
                return -1;

            int i;
            byte cmd = (byte)(a[0] & 0xFF);
            byte param = (byte)(a[0] >> 8);
            Player player = null;

            if (!_initialized && (cmd != 0 || param != 0))
                return -1;

//            {
//                var str = string.Format("DoCommand - {0} ({1}/{2})", a[0], (int)param, (int)cmd);
//                for (i = 1; i < numargs; ++i)
//                    str += string.Format(", {0}", a[i]);
//                Debug.WriteLine(str);
//            }

            if (param == 0)
            {
                switch (cmd)
                {
                    case 6:
                        if (a[1] > 127)
                            return -1;
                        else
                        {
                            Debug.WriteLine("IMuse DoCommand(6) - SetImuseMasterVolume ({0})", a[1]);
                            return SetImuseMasterVolume((uint)((a[1] << 1) | (a[1] != 0 ? 0 : 1))); // Convert from 0-127 to 0-255
                        }
                    case 7:
                        Debug.WriteLine("IMuse DoCommand(7) - GetMasterVolume ({0})", a[1]);
                        return _master_volume / 2; // Convert from 0-255 to 0-127
                    case 8:
                        return StartSoundInternal(a[1]) ? 0 : -1;
                    case 9:
                        return StopSoundInternal(a[1]);
                    case 10: // FIXME: Sam and Max - Not sure if this is correct
                        return StopAllSoundsInternal();
                    case 11:
                        return StopAllSoundsInternal();
                    case 12:
                            // Sam & Max: Player-scope commands
                        player = FindActivePlayer(a[1]);
                        if (player == null)
                            return -1;

                        switch (a[3])
                        {
                            case 6:
                                    // Set player volume.
                                return player.SetVolume((byte)a[4]);
                            default:
//                                Console.Error.WriteLine("IMuseInternal::DoCommand(12) unsupported sub-command {0}", a[3]);
                                break;
                        }
                        return -1;
                    case 13:
                        return GetSoundStatusInternal(a[1], true);
                    case 14:
                            // Sam and Max: Parameter fade
                        player = FindActivePlayer(a[1]);
                        if (player != null)
                            return player.AddParameterFader((ParameterFaderType)a[3], a[4], a[5]);
                        return -1;

                    case 15:
                            // Sam & Max: Set hook for a "maybe" jump
                        player = FindActivePlayer(a[1]);
                        if (player != null)
                        {
                            player.SetHook(0, (byte)a[3], 0);
                            return 0;
                        }
                        return -1;
                    case 16:
                        Debug.WriteLine("IMuse DoCommand(16) - SetVolChan ({0}, {1})", a[1], a[2]);
                        return SetVolchan(a[1], a[2]);
                    case 17:
                        if (_game_id != GameId.SamNMax)
                        {
                            Debug.WriteLine("IMuse DoCommand(17) - setChannelVolume ({0}, {1})", a[1], a[2]);
                            return SetChannelVolume((uint)a[1], (uint)a[2]);
                        }
                        else
                        {
                            if (a[4] != 0)
                            {
                                int[] b = new int[16];
                                for (i = 0; i < numargs; ++i)
                                    b[i] = a[i];
                                return ImSetTrigger(b[1], b[3], b[4], b[5], b[6], b[7], b[8], b[9], b[10], b[11]);
                            }
                            else
                            {
                                return ImClearTrigger(a[1], a[3]);
                            }
                        }
                    case 18:
                        if (_game_id != GameId.SamNMax)
                        {
                            return SetVolchanEntry(a[1], (uint)a[2]);
                        }
                        else
                        {
                            // Sam & Max: ImCheckTrigger.
                            // According to Mike's notes to Ender,
                            // this function returns the number of triggers
                            // associated with a particular player ID and
                            // trigger ID.
                            a[0] = 0;
                            for (i = 0; i < _snm_triggers.Length; ++i)
                            {
                                if (_snm_triggers[i].Sound == a[1] && _snm_triggers[i].Id != 0 &&
                                    (a[3] == -1 || _snm_triggers[i].Id == a[3]))
                                {
                                    ++a[0];
                                }
                            }
                            return a[0];
                        }
                    case 19:
                            // Sam & Max: ImClearTrigger
                            // This should clear a trigger that's been set up
                            // with ImSetTrigger(cmd == 17). Seems to work....
                        return ImClearTrigger(a[1], a[3]);
                    case 20:
                            // Sam & Max: Deferred Command
                        AddDeferredCommand(a[1], a[2], a[3], a[4], a[5], a[6], a[7]);
                        return 0;
                    case 2:
                    case 3:
                        return 0;
                    default:
//                        Console.Error.WriteLine("DoCommand({0} [{1}/{2}], {3}, {4}, {5}, {6}, {7}, {8}, {9}) unsupported", a[0], param, cmd, a[1], a[2], a[3], a[4], a[5], a[6], a[7]);
                        break;
                }
            }
            else if (param == 1)
            {
                if (((1 << cmd) & 0x783FFF) != 0)
                {
                    player = FindActivePlayer(a[1]);
                    if (player == null)
                        return -1;
                    if (((1 << cmd) & (1 << 11 | 1 << 22)) != 0)
                    {
                        Contract.Assert(a[2] >= 0 && a[2] <= 15);
                        // TODO: vs: check if it's correct...
                        player = player.GetPart((byte)a[2]).Player;
                        if (player == null)
                            return -1;
                    }
                }

                switch (cmd)
                {
                    case 0:
                        if (_game_id == GameId.SamNMax)
                        {
                            if (a[3] == 1) // Measure number
                                        return (int)(((player.GetBeatIndex() - 1) >> 2) + 1);
                            else if (a[3] == 2) // Beat number
                                        return (int)player.GetBeatIndex();
                            return -1;
                        }
                        else
                        {
                            return player.GetParam(a[2], (byte)a[3]);
                        }
                    case 1:
                        if (_game_id == GameId.SamNMax)
                        {
                            // FIXME: Could someone verify this?
                            //
                            // This jump instruction is known to be used in
                            // the following cases:
                            //
                            // 1) Going anywhere on the USA map
                            // 2) Winning the Wak-A-Rat game
                            // 3) Losing or quitting the Wak-A-Rat game
                            // 4) Conroy hitting Max with a golf club
                            //
                            // For all these cases the position parameters
                            // are always the same: 2, 1, 0, 0.
                            //
                            // 5) When leaving the bigfoot party. The
                            //    position parameters are: 3, 4, 300, 0
                            // 6) At Frog Rock, when the UFO appears. The
                            //    position parameters are: 10, 4, 400, 1
                            //
                            // The last two cases used to be buggy, so I
                            // have made a change to how the last two
                            // position parameters are handled. I still do
                            // not know if it's correct, but it sounds
                            // good to me at least.

                            Debug.WriteLine("DoCommand({0} [{1}/{2}], {3}, {4}, {5}, {6}, {7}, {8}, {9})", a[0], param, cmd, a[1], a[2], a[3], a[4], a[5], a[6], a[7]);
                            player.Jump((uint)(a[3] - 1), (uint)((a[4] - 1) * 4 + a[5]), (uint)(a[6] + ((a[7] * player.GetTicksPerBeat()) >> 2)));
                        }
                        else
                            player.SetPriority(a[2]);
                        return 0;
                    case 2:
                        return player.SetVolume((byte)a[2]);
                    case 3:
                        player.SetPan(a[2]);
                        return 0;
                    case 4:
                        return player.SetTranspose((byte)a[2], a[3]);
                    case 5:
                        player.SetDetune(a[2]);
                        return 0;
                    case 6:
                            // WORKAROUND for bug #1324106. When playing the
                            // "flourishes" as Rapp's body appears from his ashes,
                            // MI2 sets up triggers to pause the music, in case the
                            // animation plays too slowly, and then the music is
                            // manually unpaused for the next part of the music.
                            //
                            // In ScummVM, the animation finishes slightly too
                            // quickly, and the pause command is run *after* the
                            // unpause command. So we work around it by ignoring
                            // all attempts at pausing this particular sound.
                            //
                            // I could have sworn this wasn't needed after the
                            // recent timer change, but now it looks like it's
                            // still needed after all.
                        if (_game_id != GameId.Monkey2 || player.Id != 183 || a[2] != 0)
                        {
                            player.SetSpeed((byte)a[2]);
                        }
                        return 0;
                    case 7:
                        return player.Jump((uint)a[2], (uint)a[3], (uint)a[4]) ? 0 : -1;
                    case 8:
                        return player.Scan((uint)a[2], (uint)a[3], (uint)a[4]);
                    case 9:
                        return player.SetLoop((uint)a[2], (uint)a[3], (uint)a[4], (uint)a[5], (uint)a[6]) ? 0 : -1;
                    case 10:
                        player.ClearLoop();
                        return 0;
                    case 11:
                            // TODO: vs: check if it's correct...
                        player.Part.SetOnOff(a[3] != 0);
                        return 0;
                    case 12:
                        return player.SetHook((byte)a[2], (byte)a[3], (byte)a[4]);
                    case 13:
                        return player.AddParameterFader(ParameterFaderType.Volume, a[2], a[3]);
                    case 14:
                        return EnqueueTrigger(a[1], a[2]);
                    case 15:
                        return EnqueueCommand(a[1], a[2], a[3], a[4], a[5], a[6], a[7]);
                    case 16:
                        return ClearQueue();
                    case 19:
                        return player.GetParam(a[2], (byte)a[3]);
                    case 20:
                        return player.SetHook((byte)a[2], (byte)a[3], (byte)a[4]);
                    case 21:
                        return -1;
                    case 22:
                            // TODO: vs: check if it's correct
                        player.Part.Volume = a[3];
                        return 0;
                    case 23:
                        return QueryQueue(a[1]);
                    case 24:
                        return 0;
                    default:
//                        Console.Error.WriteLine("DoCommand({0} [{1}/{2}], {3}, {4}, {5}, {6}, {7}, {8}, {9}) unsupported", a[0], param, cmd, a[1], a[2], a[3], a[4], a[5], a[6], a[7]);
                        return -1;
                }
            }

            return -1;
        }
    
        // IMuse interface
        public void Pause(bool paused)
        {
            lock (_mutex)
            {
                if (_paused == paused)
                    return;
                int vol = _music_volume;
                if (paused)
                    _music_volume = 0;
                UpdateVolumes();
                _music_volume = (byte)vol;

                // Fix for Bug #817871. The MT-32 apparently fails
                // sometimes to respond to a channel volume message
                // (or only uses it for subsequent note events).
                // The result is hanging notes on pause. Reportedly
                // happens in the original distro, too. To fix that,
                // just send AllNotesOff to the channels.
                if (_midi_native != null && _native_mt32)
                {
                    for (int i = 0; i < 16; ++i)
                        _midi_native.Send((byte)(123 << 8 | 0xB0 | i));
                }

                _paused = paused;
            }
        }

        public void SaveOrLoad(Serializer ser)
        {
            if (ser.IsLoading && ser.Reader.BaseStream.Position >= ser.Reader.BaseStream.Length)
                return;

            lock (_mutex)
            {
                var mainEntries = new []
                {
                    LoadAndSaveEntry.Create(r => _queue_end = r.ReadByte(), w => w.WriteByte((byte)_queue_end), 8),
                    LoadAndSaveEntry.Create(r => _queue_pos = r.ReadByte(), w => w.WriteByte((byte)_queue_pos), 8),
                    LoadAndSaveEntry.Create(r => _queueSound = r.ReadUInt16(), w => w.WriteUInt16(_queueSound), 8),
                    LoadAndSaveEntry.Create(r => _queue_adding = r.ReadBoolean(), w => w.Write(_queue_adding), 8),
                    LoadAndSaveEntry.Create(r => _queue_marker = r.ReadByte(), w => w.WriteByte(_queue_marker), 8),
                    LoadAndSaveEntry.Create(r => _queue_cleared = r.ReadBoolean(), w => w.Write(_queue_cleared), 8),
                    LoadAndSaveEntry.Create(r => _master_volume = r.ReadByte(), w => w.WriteByte(_master_volume), 8),
                    LoadAndSaveEntry.Create(r => _trigger_count = r.ReadUInt16(), w => w.WriteUInt16(_trigger_count), 8),
                    LoadAndSaveEntry.Create(r => _snm_trigger_index = r.ReadUInt16(), w => w.WriteUInt16(_snm_trigger_index), 54),
                    LoadAndSaveEntry.Create(r => _channel_volume = r.ReadUInt16s(8), w => w.WriteUInt16s(_channel_volume, 8), 8),
                    LoadAndSaveEntry.Create(r => _volchan_table = r.ReadUInt16s(8), w => w.WriteUInt16s(_channel_volume, 8), 8)
                };

                mainEntries.ForEach(e => e.Execute(ser));
                _cmd_queue.ForEach(e => e.SaveOrLoad(ser));
                _snm_triggers.ForEach(e => e.SaveOrLoad(ser));

                // The players
                _players.ForEach(p => p.SaveOrLoad(ser));

                // The parts
                _parts.ForEach(p => p.SaveOrLoad(ser));

                {
                    // Load/save the instrument definitions, which were revamped with V11.
                    if (ser.Version >= 11)
                    {
                        foreach (var part in _parts)
                        {
                            part.Instrument.SaveOrLoad(ser);
                        }
                    }
                    else
                    {
                        foreach (var part in _parts)
                            part.Instrument.Clear();
                    }
                }

                // VolumeFader has been replaced with the more generic ParameterFader.
                // FIXME: replace this loop by something like
                LoadAndSaveEntry.Create(r => r.ReadBytes(13 * 8), w => w.WriteBytes(new byte[13 * 8], 13 * 8), 8, 16).Execute(ser);
//
//            // Normally, we have to fix up the data structures after loading a
//            // saved game. But there are cases where we don't. For instance, The
//            // Macintosh version of Monkey Island 1 used to convert the Mac0 music
//            // resources to General MIDI and play it through iMUSE as a rough
//            // approximation. Now it has its own player, but old savegame still
//            // have the iMUSE data in them. We have to skip that data, using a
//            // dummy iMUSE object, but since the resource is no longer recognizable
//            // to iMUSE, the fixup fails hard. So yes, this is a bit of a hack.
//
//            if (ser->isLoading() && fixAfterLoad) {
//                // Load all sounds that we need
//                fix_players_after_load(scumm);
//                fix_parts_after_load();
//                setImuseMasterVolume(_master_volume);
//
//                if (_midi_native)
//                    reallocateMidiChannels(_midi_native);
//                if (_midi_adlib)
//                    reallocateMidiChannels(_midi_adlib);
//            }
            }
        }

        public bool GetSoundActive(int sound)
        {
            lock (_mutex)
            {
                return GetSoundStatusInternal(sound, false) != 0;
            }
        }

        public int DoCommand(int numargs, int[] args)
        {
            lock (_mutex)
            {
                return DoCommandInternal(numargs, args);
            }
        }

        public uint Property(ImuseProperty prop, uint value)
        {
            lock (_mutex)
            {
                switch (prop)
                {
                    case ImuseProperty.TempoBase:
                    // This is a specified as a percentage of normal
                    // music speed. The number must be an integer
                    // ranging from 50 to 200(for 50% to 200% normal speed).
                        if (value >= 50 && value <= 200)
                            TempoFactor = (int)value;
                        break;

                    case ImuseProperty.NativeMt32:
                        _native_mt32 = (value > 0);
                        Instrument.NativeMT32(_native_mt32);
                        if (_midi_native != null && _native_mt32)
                            InitMT32(_midi_native);
                        break;

                    case ImuseProperty.Gs:
                        _enable_gs = (value > 0);

                    // GS Mode emulates MT-32 on a GS device, so _native_mt32 should always be true
                        if (_midi_native != null && _enable_gs)
                        {
                            _native_mt32 = true;
                            InitGM(_midi_native);
                        }
                        break;

                    case ImuseProperty.LimitPlayers:
                        if (value > 0 && value <= _players.Length)
                            _player_limit = (int)value;
                        break;

                    case ImuseProperty.RecyclePlayers:
                        _recycle_players = (value != 0);
                        break;

                    case ImuseProperty.GameId:
                        _game_id = (GameId)value;
                        break;

                    case ImuseProperty.PcSpeaker:
                        PcSpeaker = (value != 0);
                        break;
                }

                return 0;
            }
        }

        public virtual void AddSysexHandler(byte mfgID, SysExFunc handler)
        {
            // TODO: Eventually support multiple sysEx handlers and pay
            // attention to the client-supplied manufacturer ID.
            lock (_mutex)
            {
                Sysex = handler;
            }
        }

        public void StartSoundWithNoteOffset(int sound, int offset)
        {
            lock (_mutex)
            {
                StartSoundInternal(sound, offset);
            }
        }
    
        // MusicEngine interface
        public void SetMusicVolume(int vol)
        {
            lock (_mutex)
            {
                if (vol > 255)
                    vol = 255;
                if (_music_volume == vol)
                    return;
                _music_volume = (byte)vol;
                vol = _master_volume * _music_volume / 255;
                for (var i = 0; i < _channel_volume.Length; i++)
                {
                    _channel_volume_eff[i] = (ushort)(_channel_volume[i] * vol / 255);
                }
                if (!_paused)
                    UpdateVolumes();
            }
        }

        public void StartSound(int sound)
        {
            lock (_mutex)
            {
                StartSoundInternal(sound);
            }
        }

        public void StopSound(int sound)
        {
            lock (_mutex)
            {
                StopSoundInternal(sound);
            }
        }

        public void StopAllSounds()
        {
            lock (_mutex)
            {
                StopAllSoundsInternal();
            }
        }

        public int GetSoundStatus(int sound)
        {
            lock (_mutex)
            {
                return GetSoundStatusInternal(sound, true);
            }
        }

        public int GetMusicTimer()
        {
            lock (_mutex)
            {
                int best_time = 0;
                foreach (var player in _players)
                {
                    if (player.IsActive)
                    {
                        int timer = player.GetMusicTimer();
                        if (timer > best_time)
                            best_time = timer;
                    }
                }
                return best_time;
            }
        }
    
        // Factory function
        public static IMuseInternal Create(MidiDriver nativeMidiDriver, MidiDriver adlibMidiDriver)
        {
            var i = new IMuseInternal();
            i.Initialize(nativeMidiDriver, adlibMidiDriver);
            return i;
        }
    }
}
