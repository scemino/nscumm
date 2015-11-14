//
//  V2A_Sound_MultiLooped.cs
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
    // plays two looped waveforms
    class V2A_Sound_MultiLooped : V2A_Sound_Base
    {
        public V2A_Sound_MultiLooped(ushort offset, ushort size, ushort freq1, byte vol1, ushort freq2, byte vol2)
            : base(2, offset, size)
        {
            _freq1 = freq1;
            _vol1 = vol1;
            _freq2 = freq2;
            _vol2 = vol2;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            var tmp_data1 = new byte[_size];
            var tmp_data2 = new byte[_size];
            Array.Copy(data, _offset, tmp_data1, 0, _size);
            Array.Copy(data, _offset, tmp_data2, 0, _size);
            int vol1 = (_vol1 << 1) | (_vol1 >> 5);
            int vol2 = (_vol2 << 1) | (_vol2 >> 5);
            Player.StartChannel(Id | 0x000, tmp_data1, _size, BASE_FREQUENCY / _freq1, vol1, 0, _size, -127);
            Player.StartChannel(Id | 0x100, tmp_data2, _size, BASE_FREQUENCY / _freq2, vol2, 0, _size, 127);
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            return true;
        }

        readonly ushort _freq1;
        readonly byte _vol1;
        readonly ushort _freq2;
        readonly byte _vol2;
    }
    
}
