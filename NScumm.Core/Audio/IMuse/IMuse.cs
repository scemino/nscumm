//
//  IMuse.cs
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
using System.Collections.Generic;
using System.Linq;
using NScumm.Core.Audio.OPL;

namespace NScumm.Core.Audio.IMuse
{
    public class IMuse: IIMuse, IMusicEngine
    {
        const int TriggerId = 0;
        const int CommandId = 1;

        List<IPlayer> players;
        object gate = new object();
        CommandQueue[] _cmd_queue;
        bool _queue_adding;
        int _queue_end;
        int _queue_pos;
        int _trigger_count;
        int _queue_sound;
        int _queue_marker;
        bool _queue_cleared;
        ISoundRepository soundRepository;
        IOpl opl;
        ISysEx sysEx;

        public IMuse(ISoundRepository soundRepository, IOpl opl)
        {
            sysEx = new IMuseSysEx(this);
            this.opl = opl;
            this.soundRepository = soundRepository;
            players = new List<IPlayer>();

            _cmd_queue = new CommandQueue[64];
            for (int i = 0; i < _cmd_queue.Length; i++)
            {
                _cmd_queue[i] = new CommandQueue();
            }
        }

        #region IMusicEngine implementation

        public bool Update()
        {
            bool updated = false;
            lock (gate)
            {
                foreach (var player in players.ToList())
                {
                    if (player.IsActive)
                    {
                        var isActive = player.Update();
                        if (!isActive)
                        {
                            players.Remove(player);
                        }
                        updated |= isActive;
                    }
                }
            }
            return updated;
        }

        public float GetMusicTimer()
        {
            lock (gate)
            {
                var bestTime = 0f;
                foreach (var player in players)
                {
                    if (player.IsActive)
                    {
                        var timer = player.GetMusicTimer();
                        if (timer > bestTime)
                            bestTime = timer;
                    }
                }
                return bestTime;
            }
        }

        public void SetMusicVolume(int vol)
        {
            throw new NotImplementedException();
        }

        public void StartSound(int sound)
        {
            lock (gate)
            {
                StartSoundCore(sound);
            }
        }

        bool StartSoundCore(int sound, int offset = 0)
        {
            var player = new Player(soundRepository, opl, sysEx);
            player.OffsetNote = offset;
            players.Add(player);
            return player.StartSound(sound);
        }

        IPlayer FindActivePlayer(int sound)
        {
            return players.FirstOrDefault(o => o.IsActive && o.Id == sound);
        }

        public void StopSound(int sound)
        {
            lock (gate)
            {
                StopSoundCore(sound);
            }
        }

        public void StopAllSounds()
        {
            lock (gate)
            {
                StopAllSoundsCore();
            }
        }

        public int GetSoundStatus(int sound)
        {
            lock (gate)
            {
                return GetSoundStatusCore(sound, true);
            }
        }

        int GetSoundStatusCore(int sound, bool ignoreFadeouts)
        {
            foreach (var player in players)
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

        int GetQueueSoundStatus(int sound)
        {
            // TODO
            return 0;
        }

        #endregion

        #region IIMuse implementation

        public int DoCommand(int num, int[] a)
        {
            var cmd = a[0] & 0xFF;
            var param = a[0] >> 8;

            if (param == 0)
            {
                switch (cmd)
                {
                    case 2:
                    case 3:
                        return 0;
                    case 8:
                        return StartSoundCore(a[1]) ? 0 : -1;
                    case 9:
                        StopSoundCore(a[1]);
                        return 0;
                    case 10: // FIXME: Sam and Max - Not sure if this is correct
                        StopAllSoundsCore();
                        return 0;
                    case 11:
                        StopAllSoundsCore();
                        return 0;
                    case 13:
                        return GetSoundStatusCore(a[1], true);
                    default:
                        Console.Error.WriteLine("DoCommand({0} [{1}/{2}], {3}, {4}, {5}, {6}, {7}, {8}, {9}) unsupported", a[0], param, cmd, a[1], a[2], a[3], a[4], a[5], a[6], a[7]);
                        return -1;
                }
            }
            else if (param == 1)
            {
                IPlayer player = null;
                if (((1 << cmd) & 0x783FFF) != 0)
                {
                    player = FindActivePlayer(a[1]);
                    if (player == null)
                        return -1;
                    if (((1 << cmd) & (1 << 11 | 1 << 22)) != 0)
                    {
                        System.Diagnostics.Debug.Assert(a[2] >= 0 && a[2] <= 15);
                        // TODO:
//                        player = player.GetPart(a[2]);
//                        if (!player)
//                            return -1;
                    }
                }

                switch (cmd)
                {
                    case 0:
                        return player.GetParam(a[2], a[3]);
                    case 1:
                        player.Priority = a[2];
                        return 0;
                    case 2:
//                        return player.SetVolume(a[2]);
                        return 0;
                    case 3:
                        player.Pan = a[2];
                        return 0;
                    case 4:
//                        return player.SetTranspose(a[2], a[3]);
                        return 0;
                    case 5:
                        player.Detune = a[2];
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
//                        if (_game.Id != "monkey2" || player.Id != 183 || a[2] != 0)
//                        {
//                            player.SetSpeed(a[2]);
//                        }
                        return 0;
                    case 7:
                            //return player.Jump(a[2], a[3], a[4]) ? 0 : -1;
                        return 0;
                    case 8:
                            //return player.Scan(a[2], a[3], a[4]);
                        return 0;
                    case 9:
                            //return player.SetLoop(a[2], a[3], a[4], a[5], a[6]) ? 0 : -1;
                        return 0;
                    case 10:
                            //player.ClearLoop();
                        return 0;
                    case 11:
                            //((Part)player).SetOnOff(a[3] != 0);
                        return 0;
                    case 12:
                        return player.SetHook(a[2], a[3], a[4]);
                    case 13:
                            //return player.AddParameterFader(ParameterFader.Volume, a[2], a[3]);
                        return -1;
                    case 14:
                        return EnqueueTrigger(a[1], a[2]);
                    case 15:
                        return EnqueueCommand(new []{ a[1], a[2], a[3], a[4], a[5], a[6], a[7] });
                    case 16:
                        return ClearQueue();
                    case 19:
                        return player.GetParam(a[2], a[3]);
                    case 20:
                        return player.SetHook(a[2], a[3], a[4]);
                    case 21:
                        return -1;
                    case 22:
                            //((Part)player).Volume(a[3]);
                        return 0;
                    case 23:
                        return QueryQueue(a[1]);
                    case 24:
                        return 0;

                    default:
                        Console.Error.WriteLine("DoCommand({0} [{1}/{2}], {3}, {4}, {5}, {6}, {7}, {8}, {9}) unsupported", a[0], param, cmd, a[1], a[2], a[3], a[4], a[5], a[6], a[7]);
                        return -1;
                }
            }

            return -1;
        }

        void StopSoundCore(int sound)
        {
            var player = FindActivePlayer(sound);
            if (player != null)
            {
                player.Clear();
            }
        }

        void StopAllSoundsCore()
        {
            ClearQueue();
            foreach (var player in players)
            {
                if (player.IsActive)
                    player.Clear();
            }
            players.Clear();
        }

        int QueryQueue(int param)
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

        int EnqueueCommand(int[] arr)
        {
            var i = _queue_pos;

            if (i == _queue_end)
                return -1;

            if (arr[0] == -1)
            {
                _queue_adding = false;
                _trigger_count++;
                return 0;
            }

            var p = _cmd_queue[_queue_pos].array;
            p[0] = CommandId;
            Array.Copy(arr, 0, p, 1, arr.Length);

            i = (i + 1) % _cmd_queue.Length;

            if (_queue_end != i)
            {
                _queue_pos = i;
                return 0;
            }
            else
            {
                _queue_pos = (i - 1) % _cmd_queue.Length;
                if (_queue_pos == -1)
                {
                    _queue_pos = _cmd_queue.Length - 1;
                    if (_queue_pos == -1)
                    {
                        _queue_pos = _cmd_queue.Length - 1;
                    }
                }
                return -1;
            }
        }

        int EnqueueTrigger(int sound, int marker)
        {
            var pos = _queue_pos;

            var p = _cmd_queue[pos].array;
            p[0] = TriggerId;
            p[1] = sound;
            p[2] = marker;

            pos = (pos + 1) % _cmd_queue.Length;
            if (_queue_end == pos)
            {
                _queue_pos = (pos - 1) % _cmd_queue.Length;
                if (_queue_pos == -1)
                {
                    _queue_pos = _cmd_queue.Length - 1;
                }
                return -1;
            }

            _queue_pos = pos;
            _queue_adding = true;
            _queue_sound = sound;
            _queue_marker = marker;
            return 0;
        }

        int ClearQueue()
        {
            _queue_adding = false;
            _queue_cleared = true;
            _queue_pos = 0;
            _queue_end = 0;
            _trigger_count = 0;
            return 0;
        }

        public void HandleMarker(int id, int data)
        {
            if ((_queue_end == _queue_pos) || (_queue_adding && _queue_sound == id && data == _queue_marker))
                return;

            var p = _cmd_queue[_queue_end].array;
            if (p[0] != TriggerId || id != p[1] || data != p[2])
                return;

            _trigger_count--;
            _queue_cleared = false;
            _queue_end = (_queue_end + 1) % _cmd_queue.Length;

            while (_queue_end != _queue_pos && _cmd_queue[_queue_end].array[0] == CommandId && !_queue_cleared)
            {
                p = _cmd_queue[_queue_end].array;
                DoCommand(8, new []{ p[1], p[2], p[3], p[4], p[5], p[6], p[7], 0 });
                _queue_end = (_queue_end + 1) % _cmd_queue.Length;
            }
        }

        #endregion

    }
}
