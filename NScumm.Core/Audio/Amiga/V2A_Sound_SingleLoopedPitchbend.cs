//
//  V2A_Sound_SingleLoopedPitchbend.cs
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
    // plays a single looped waveform which starts at one frequency and bends to another frequency, where it remains until stopped
    class V2A_Sound_SingleLoopedPitchbend : V2A_Sound_Base
    {
        public V2A_Sound_SingleLoopedPitchbend(ushort offset, ushort size, ushort freq1, ushort freq2, byte vol, byte step)
            : base(1, offset, size)
        {
            _freq1 = freq1;
            _freq2 = freq2;
            _vol = vol;
            _step = step;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            var tmp_data = new byte[_size];
            Array.Copy(data, _offset, tmp_data, 0, _size);
            int vol = (_vol << 2) | (_vol >> 4);
            _curfreq = _freq1;
            Player.StartChannel(Id, tmp_data, _size, BASE_FREQUENCY / _curfreq, vol, 0, _size);
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            if (_freq1 < _freq2)
            {
                _curfreq += _step;
                if (_curfreq > _freq2)
                    _curfreq = _freq2;
                else
                    Player.SetChannelFreq(Id, BASE_FREQUENCY / _curfreq);
            }
            else
            {
                _curfreq -= _step;
                if (_curfreq < _freq2)
                    _curfreq = _freq2;
                else
                    Player.SetChannelFreq(Id, BASE_FREQUENCY / _curfreq);
            }
            return true;
        }

        readonly ushort _freq1;
        readonly ushort _freq2;
        readonly byte _vol;
        readonly ushort _step;
        ushort _curfreq;
    }
    
}
