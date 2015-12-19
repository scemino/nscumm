//
//  V2A_Sound_Special_Maniac69.cs
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
    // plays a single looped waveform starting at a specific frequency/volume, dropping in frequency and fading volume to zero
    // used when Maniac Mansion explodes
    class V2A_Sound_Special_Maniac69 : V2A_Sound_Base
    {
        public V2A_Sound_Special_Maniac69(ushort offset, ushort size, ushort freq, byte vol)
            : base(1, offset, size)
        {
            _freq = freq;
            _vol = vol;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            var tmp_data = new byte[_size];
            Array.Copy(data, _offset, tmp_data, 0, _size);
            _curvol = (ushort)((_vol << 3) | (_vol >> 3));
            _curfreq = _freq;
            Player.StartChannel(Id, tmp_data, _size, BASE_FREQUENCY / _curfreq, _curvol >> 1, 0, _size);
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            _curfreq += 2;
            Player.SetChannelFreq(Id, BASE_FREQUENCY / _curfreq);
            _curvol--;
            if (_curvol == 0)
                return false;
            Player.SetChannelVol(Id, _curvol >> 1);
            return true;
        }

        readonly ushort _freq;
        readonly byte _vol;

        ushort _curfreq;
        ushort _curvol;
    }
}
