//
//  V2A_Sound_SingleLooped.cs
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
    // plays a single looped waveform
    class V2A_Sound_SingleLooped : V2A_Sound_Base
    {
        public V2A_Sound_SingleLooped(ushort offset, ushort size, ushort freq, ushort vol, ushort loopoffset, ushort loopsize)
            : base(1, offset, size)
        {
            _loopoffset = loopoffset;
            _loopsize = loopsize;
            _freq = freq;
            _vol = vol;
        }

        public V2A_Sound_SingleLooped(ushort offset, ushort size, ushort freq, ushort vol)
            : base(1, offset, size)
        {
            _loopoffset = 0;
            _loopsize = size;
            _freq = freq;
            _vol = vol;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            var tmp_data = new byte[_size];
            Array.Copy(data, _offset, tmp_data, 0, _size);
            int vol = (_vol << 2) | (_vol >> 4);
            Player.StartChannel(Id, tmp_data, _size, BASE_FREQUENCY / _freq, vol, _loopoffset, _loopoffset + _loopsize);
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            return true;
        }

        readonly ushort _loopoffset;
        readonly ushort _loopsize;
        readonly ushort _freq;
        readonly ushort _vol;
    }
    
}
