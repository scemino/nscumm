//
//  V2A_Sound_Special_Maniac59.cs
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
    // plays a single looped waveform, starting at one frequency, bending down to another frequency, and then back up to the original frequency
    // used for electronic noises
    class V2A_Sound_Special_Maniac59 : V2A_Sound_Base
    {
        public V2A_Sound_Special_Maniac59(ushort offset, ushort size, ushort freq1, ushort freq2, ushort step, byte vol)
            : base(1, offset, size)
        {
            _freq1 = freq1;
            _freq2 = freq2;
            _step = step;
            _vol = vol;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            var tmp_data = new byte[_size];
            Array.Copy(data, _offset, tmp_data, 0, _size);
            int vol = (_vol << 2) | (_vol >> 4);
            _curfreq = _freq1;
            _dir = 2;
            Player.StartChannel(Id, tmp_data, _size, BASE_FREQUENCY / _curfreq, vol, 0, _size);
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            if (_dir == 2)
            {
                _curfreq += _step;
                if (_curfreq > _freq2)
                {
                    _curfreq = _freq2;
                    _dir = 1;
                }
                Player.SetChannelFreq(Id, BASE_FREQUENCY / _curfreq);
            }
            else if (_dir == 1)
            {
                _curfreq -= _step;
                if (_curfreq < _freq1)
                {
                    _curfreq = _freq1;
                    _dir = 0;
                }
                Player.SetChannelFreq(Id, BASE_FREQUENCY / _curfreq);
            }
            return true;
        }

        readonly ushort _freq1;
        readonly ushort _freq2;
        readonly ushort _step;
        readonly byte _vol;

        ushort _curfreq;
        int _dir;
    }    
}
