//
//  V2A_Sound_Special_ManiacTypewriter.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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

namespace NScumm.Core.Audio
{
    // plays a single waveform at irregular intervals for a specified number of frames, possibly looped
    // used for typewriter noises, as well as tapping on the bus in Zak McKracken
    class V2A_Sound_Special_ManiacTypewriter : V2A_Sound_Base
    {
        public V2A_Sound_Special_ManiacTypewriter(ushort offset, ushort size, ushort freq, byte vol, byte numdurs, byte[] durations, bool looped)
            : base(1, offset, size)
        {
            _freq = freq;
            _vol = vol;
            _numdurs = numdurs;
            _durations = durations;
            _looped = looped;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            _data = new byte[BitConverter.ToUInt16(data, 0)];
            Array.Copy(data, _data, _data.Length);
            Soundon();
            _curdur = 0;
            _ticks = _durations[_curdur++];
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            _ticks--;
            if (_ticks == 0)
            {
                if (_curdur == _numdurs)
                {
                    if (_looped)
                        _curdur = 0;
                    else
                        return false;
                }
                Player.StopChannel(Id);
                Soundon();
                _ticks = _durations[_curdur++];
            }
            return true;
        }

        readonly ushort _freq;
        readonly byte _vol;
        readonly byte _numdurs;
        readonly byte[] _durations;
        readonly bool _looped;

        int _ticks;
        int _curdur;

        void Soundon()
        {
            var tmp_data = new byte[_size];
            Array.Copy(_data, _offset, tmp_data, 0, _size);
            Player.StartChannel(Id, tmp_data, _size, BASE_FREQUENCY / _freq, (_vol << 2) | (_vol >> 4), 0, 0);
        }
    }
}
