//
//  V2A_Sound_Special_ManiacPhone.cs
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
using NScumm.Core.IO;

namespace NScumm.Core.Audio
{

    // intermittently plays two looped waveforms for a specific duration
    // used for ringing telephones
    class V2A_Sound_Special_ManiacPhone : V2A_Sound_Base
    {
        public V2A_Sound_Special_ManiacPhone(ushort offset, ushort size, ushort freq1, byte vol1, ushort freq2, byte vol2, ushort numframes, byte playwidth, byte loopwidth)
            : base(2, offset, size)
        {
            _freq1 = freq1;
            _vol1 = vol1;
            _freq2 = freq2;
            _vol2 = vol2;
            _duration = numframes;
            _playwidth = playwidth;
            _loopwidth = loopwidth;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            _data = new byte[BitConverter.ToUInt16(data, 0)];
            Array.Copy(data, _data, _data.Length);
            Soundon();
            _ticks = 0;
            _loop = 0;
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            if (_loop == _playwidth)
            {
                Player.StopChannel(Id | 0x000);
                Player.StopChannel(Id | 0x100);
            }
            if (_loop == _loopwidth)
            {
                _loop = 0;
                Soundon();
            }
            _loop++;
            _ticks++;
            if (_ticks >= _duration)
                return false;
            return true;
        }

        readonly ushort _freq1;
        readonly byte _vol1;
        readonly ushort _freq2;
        readonly byte _vol2;
        readonly ushort _duration;
        readonly byte _playwidth;
        readonly byte _loopwidth;

        int _ticks;
        int _loop;

        void Soundon()
        {
            var tmp_data1 = new byte[_size];
            var tmp_data2 = new byte[_size];
            Array.Copy(_data, _offset, tmp_data1, 0, _size);
            Array.Copy(_data, _offset, tmp_data2, 0, _size);
            int vol1 = (_vol1 << 1) | (_vol1 >> 5);
            int vol2 = (_vol2 << 1) | (_vol2 >> 5);
            Player.StartChannel(Id | 0x000, tmp_data1, _size, BASE_FREQUENCY / _freq1, vol1, 0, _size, -127);
            Player.StartChannel(Id | 0x100, tmp_data2, _size, BASE_FREQUENCY / _freq2, vol2, 0, _size, 127);
        }
    }

    // intermittently plays a single waveform for a specified duration
    // used when applying a wrench to a pipe

    // plays a single waveform at irregular intervals for a specified number of frames, possibly looped
    // used for typewriter noises, as well as tapping on the bus in Zak McKracken

    // plays two looped waveforms pitch bending up at various predefined rates
    // used for some sort of siren-like noise in Maniac Mansion

    // plays 4 looped waveforms, each at modulating frequencies
    // used for the siren noise in Maniac Mansion

    // plays a music track
    
}
