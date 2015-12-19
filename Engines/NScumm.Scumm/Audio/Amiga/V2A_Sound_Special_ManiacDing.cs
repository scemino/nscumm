//
//  V2A_Sound_Special_ManiacDing.cs
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

namespace NScumm.Scumm.Audio.Amiga
{
    // plays a single looped waveform, fading the volume from zero to maximum at one rate, then back to zero at another rate
    // used when a microwave oven goes 'Ding'
    class V2A_Sound_Special_ManiacDing :  V2A_Sound_Base
    {
        public V2A_Sound_Special_ManiacDing(ushort offset, ushort size, ushort freq, byte fadeinrate, byte fadeoutrate)
            : base(1, offset, size)
        { 
            _freq = freq;
            _fade1 = fadeinrate;
            _fade2 = fadeoutrate;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            var tmp_data = new byte[_size];
            Array.Copy(data, _offset, tmp_data, 0, _size);
            _curvol = 1;
            _dir = 0;
            Player.StartChannel(Id, tmp_data, _size, BASE_FREQUENCY / _freq, _curvol, 0, _size);
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            if (_dir == 0)
            {
                _curvol += _fade1;
                if (_curvol > 0x3F)
                {
                    _curvol = 0x3F;
                    _dir = 1;
                }
            }
            else
            {
                _curvol -= _fade2;
                if (_curvol < 1)
                    return false;
            }
            Player.SetChannelVol(Id, (_curvol << 2) | (_curvol >> 4));
            return true;
        }

        readonly ushort _freq;
        readonly ushort _fade1;
        readonly ushort _fade2;
        int _curvol;
        int _dir;
    }
    
}
