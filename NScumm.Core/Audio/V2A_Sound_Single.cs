//
//  V2A_Sound_Single.cs
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
    // plays a single waveform
    class V2A_Sound_Single: V2A_Sound_Base
    {
        public V2A_Sound_Single(ushort offset, ushort size, ushort freq, byte vol)
            : base(1, offset, size)
        {
            _freq = freq;
            _vol = vol;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            var size = Math.Min(_size, data.Length - _offset);
            var tmp_data = new byte[size];
            Array.Copy(data, _offset, tmp_data, 0, size);
            int vol = (_vol << 2) | (_vol >> 4);
            Player.StartChannel(Id, tmp_data, _size, BASE_FREQUENCY / _freq, vol);
            _ticks = 1 + (60 * _size * _freq) / BASE_FREQUENCY;
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            _ticks--;
            if (_ticks == 0)
            {
                return false;
            }
            return true;
        }

        readonly ushort _freq;
        readonly byte _vol;
        int _ticks;
    }
    
}
