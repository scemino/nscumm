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
using NScumm.Core.Audio.Midi;
using System.Collections.Generic;
using System.Linq;
using NScumm.Core.Audio.OPL;
using System.IO;

namespace NScumm.Core
{
    public class Player: IPlayer, IMidiPlayer
    {
        readonly IMidiPlayer midi;
        readonly ISoundRepository soundRepository;
        readonly HookDatas hook;
        readonly List<Part> parts;
        int detune;
        int priority;
        int pan;

        #region IMidiPlayer implementation

        void IMidiPlayer.LoadFrom(byte[] data)
        {
            midi.LoadFrom(data);
        }

        void IMidiPlayer.Stop()
        {
            midi.Stop();
        }

        public bool Update()
        {
            return midi.Update();
        }

        public float GetMusicTimer()
        {
            return midi.GetMusicTimer();
        }

        public MidiChannel[] Channels
        {
            get
            {
                return midi.Channels;
            }
        }

        #endregion

        class PlayerSysEx: ISysEx
        {
            IMidiPlayer player;
            ISysEx sysEx;

            public PlayerSysEx(IMidiPlayer player, ISysEx sysEx)
            {
                this.player = player;
                this.sysEx = sysEx;
            }

            public void Do(IMidiPlayer midi, Stream input)
            {
                sysEx.Do(player, input);
            }
        }

        public Player(ISoundRepository soundRepository, IOpl opl, ISysEx sysEx)
        {
            midi = new MidiPlayer(opl, new PlayerSysEx(this, sysEx));
            this.soundRepository = soundRepository;
            hook = new HookDatas();
            parts = new List<Part>();
        }

        public bool IsNativeMT32{ get; private set; }

        public bool IsMidi { get; private set; }

        public int Priority
        {
            get{ return priority; }
            set
            {
                priority = value;
                foreach (var part in parts)
                {
                    part.Priority = part.Priority;
                }
                // TODO:
                //_se->reallocateMidiChannels(_midi);
            }
        }

        public int EffectiveVolume { get; private set; }

        public int Transpose { get; private set; }

        public int Pan
        {
            get{ return pan; }
            set
            {
                pan = value;
                foreach (var part in parts)
                {
                    part.Pan = part.Pan;
                }
            }
        }

        public int Detune
        {
            get{ return detune; }
            set
            {
                detune = value;
                foreach (var part in parts)
                {
                    part.Detune = part.Detune;
                }
            }
        }

        public int OffsetNote { get; set; }

        public bool IsActive { get; private set; }

        public int Id { get; private set; }

        public bool IsFadingOut { get; private set; }

        public void Clear()
        {
            if (!IsActive)
                return;
            //debugC(DEBUG_IMUSE, "Stopping music %d", _id);

            midi.Stop();

//            uninit_parts();
//            _se->ImFireAllTriggers(_id);
            IsActive = false;
//            _midi = NULL;
            Id = 0;
            OffsetNote = 0;
        }

        public bool StartSound(int sound)
        {
            IsActive = true;
            Id = sound;

            var data = soundRepository.GetSound(sound);
            if (data == null)
                return false;
            midi.LoadFrom(data);
            return true;
        }

        public int GetParam(int param, int chan)
        {
            switch (param)
            {
                case 0:
                    return Priority;
//                case 1:
//                    return Volume;
                case 2:
                    return Pan;
                case 3:
                    return Transpose;
                case 4:
                    return Detune;
//                case 5:
//                    return _speed;
//                case 6:
//                    return _track_index;
//                case 7:
//                    return getBeatIndex();
//                case 8:
//                    return (_parser ? _parser->getTick() % TICKS_PER_BEAT : 0); // _tick_index;
//                case 9:
//                    return _loop_counter;
//                case 10:
//                    return _loop_to_beat;
//                case 11:
//                    return _loop_to_tick;
//                case 12:
//                    return _loop_from_beat;
//                case 13:
//                    return _loop_from_tick;
//                case 14:
//                case 15:
//                case 16:
//                case 17:
//                    return query_part_param(param, chan);
//                case 18:
//                case 19:
//                case 20:
//                case 21:
//                case 22:
                case 23:
                    return hook.QueryParam(param, chan);
                default:
                    return -1;
            }
        }

        public int SetHook(int cls, int value, int chan)
        { 
            return hook.Set(cls, value, chan);
        }

        Part GetActivePart(int chan)
        {
            return parts.FirstOrDefault(p => p.Channel == chan);
        }

        Part GetPart(int chan)
        {
            var part = GetActivePart(chan);
            if (part != null)
                return part;

            part = new Part(this, Priority, chan);

            // Insert part into front of parts list
            parts.Insert(0, part);


            return part;
        }
    }
}
