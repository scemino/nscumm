//
//  V2A_Sound_Special_ManiacTentacle.cs
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
    // plays a single looped waveform, starting at one frequency and at full volume, bending down to another frequency, and then fading volume to zero
    // used in Maniac Mansion for the tentacle sounds
    class V2A_Sound_Special_ManiacTentacle : V2A_Sound_Base
    {
        public V2A_Sound_Special_ManiacTentacle(ushort offset, ushort size, ushort freq1, ushort freq2, ushort step)
            : base(1, offset, size)
        {
            _freq1 = freq1;
            _freq2 = freq2;
            _step = step;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            var tmp_data = new byte[_size];
            Array.Copy(data, _offset, tmp_data, 0, _size);
            _curfreq = _freq1;
            _curvol = 0x3F;
            Player.StartChannel(Id, tmp_data, _size, BASE_FREQUENCY / _curfreq, (_curvol << 2) | (_curvol >> 4), 0, _size);
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            if (_curfreq > _freq2)
                _curvol = 0x3F + _freq2 - _curfreq;
            if (_curvol < 1)
                return false;
            _curfreq += _step;
            Player.SetChannelFreq(Id, BASE_FREQUENCY / _curfreq);
            Player.SetChannelVol(Id, (_curvol << 2) | (_curvol >> 4));
            return true;
        }

        readonly ushort _freq1;
        readonly ushort _freq2;
        readonly ushort _step;

        ushort _curfreq;
        int _curvol;
    }
}
