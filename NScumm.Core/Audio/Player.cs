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

namespace NScumm.Core
{
    public class Player: IPlayer
    {
        readonly IMidiPlayer midi;
        readonly ISoundRepository soundRepository;
        readonly HookDatas hook;

        public Player(IMidiPlayer midi, ISoundRepository soundRepository)
        {
            this.midi = midi;
            this.soundRepository = soundRepository;
            hook = new HookDatas();
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
//                case 0:
//                    return _priority;
//                case 1:
//                    return _volume;
//                case 2:
//                    return _pan;
//                case 3:
//                    return _transpose;
//                case 4:
//                    return _detune;
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
    }
}
