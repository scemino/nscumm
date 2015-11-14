//
//  V2A_Sound_Special_Maniac46.cs
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
    // intermittently plays a single waveform for a specified duration
    // used when applying a wrench to a pipe
    class V2A_Sound_Special_Maniac46 : V2A_Sound_Base
    {
        public V2A_Sound_Special_Maniac46(ushort offset, ushort size, ushort freq, byte vol, byte loopwidth, byte numloops)
            : base(1, offset, size)
        {
            _freq = freq;
            _vol = vol;
            _loopwidth = loopwidth;
            _numloops = numloops;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            _data = new byte[BitConverter.ToUInt16(data, 0)];
            Array.Copy(data, _data, _data.Length);
            Soundon();
            _loop = 0;
            _loopctr = 0;
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            _loop++;
            if (_loop == _loopwidth)
            {
                _loop = 0;
                _loopctr++;
                if (_loopctr == _numloops)
                    return false;
                Player.StopChannel(Id);
                Soundon();
            }
            return true;
        }

        readonly ushort _freq;
        readonly byte _vol;
        readonly byte _loopwidth;
        readonly byte _numloops;

        int _loop;
        int _loopctr;

        void Soundon()
        {
            var tmp_data = new byte[_size];
            Array.Copy(_data, _offset, tmp_data, 0, _size);
            Player.StartChannel(Id, tmp_data, _size, BASE_FREQUENCY / _freq, (_vol << 2) | (_vol >> 4), 0, 0);
        }
    }
}
