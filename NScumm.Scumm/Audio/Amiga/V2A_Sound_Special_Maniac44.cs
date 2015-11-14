//
//  V2A_Sound_Special_Maniac44.cs
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
    // plays two looped waveforms pitch bending up at various predefined rates
    // used for some sort of siren-like noise in Maniac Mansion
    class V2A_Sound_Special_Maniac44 : V2A_Sound_Base
    {
        public V2A_Sound_Special_Maniac44(ushort offset1, ushort size1, ushort offset2, ushort size2, ushort freq1, ushort freq2, byte vol)
            : base(2)
        {
            _offset1 = offset1;
            _size1 = size1;
            _offset2 = offset2;
            _size2 = size2;
            _freq1 = freq1;
            _freq2 = freq2;
            _vol = vol;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;
            _data = new byte[BitConverter.ToUInt16(data, 0)];
            Array.Copy(data, _data, _data.Length);

            _loopnum = 1;
            _step = 2;
            _curfreq = _freq1;

            Soundon(_data, _offset1, _size1);
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            Player.SetChannelFreq(Id | 0x000, BASE_FREQUENCY / _curfreq);
            Player.SetChannelFreq(Id | 0x100, BASE_FREQUENCY / (_curfreq + 3));
            _curfreq -= _step;
            if (_loopnum == 7)
            {
                if ((BASE_FREQUENCY / _curfreq) >= 65536)
                    return false;
                else
                    return true;
            }
            if (_curfreq >= _freq2)
                return true;
            byte[] steps = { 0, 2, 2, 3, 4, 8, 15, 2 };
            _curfreq = _freq1;
            _step = steps[++_loopnum];
            if (_loopnum == 7)
            {
                Player.StopChannel(Id | 0x000);
                Player.StopChannel(Id | 0x100);
                Soundon(_data, _offset2, _size2);
            }
            return true;
        }

        readonly ushort _offset1;
        readonly ushort _size1;
        readonly ushort _offset2;
        readonly ushort _size2;
        readonly ushort _freq1;
        readonly ushort _freq2;
        readonly byte _vol;

        int _curfreq;
        ushort _loopnum;
        ushort _step;

        void Soundon(byte[] data, int offset, int size)
        {
            var tmp_data1 = new byte[size];
            var tmp_data2 = new byte[size];
            Array.Copy(data, offset, tmp_data1, 0, size);
            Array.Copy(data, offset, tmp_data2, 0, size);
            int vol = (_vol << 1) | (_vol >> 5);
            Player.StartChannel(Id | 0x000, tmp_data1, size, BASE_FREQUENCY / _curfreq, vol, 0, size, -127);
            Player.StartChannel(Id | 0x100, tmp_data2, size, BASE_FREQUENCY / (_curfreq + 3), vol, 0, size, 127);
        }
    }
    
}
