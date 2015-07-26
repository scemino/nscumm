//
//  V2A_Sound_Special_Maniac32.cs
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
    // plays 4 looped waveforms, each at modulating frequencies
    // used for the siren noise in Maniac Mansion
    class V2A_Sound_Special_Maniac32 : V2A_Sound_Base
    {
        public V2A_Sound_Special_Maniac32(ushort offset1, ushort size1, ushort offset2, ushort size2, byte vol)
            : base(4)
        {
            _offset1 = offset1;
            _size1 = size1;
            _offset2 = offset2;
            _size2 = size2;
            _vol = vol;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;

            _freq1 = 0x02D0;
            _step1 = -0x000A;
            _freq2 = 0x0122;
            _step2 = 0x000A;
            _freq3 = 0x02BC;
            _step3 = -0x0005;
            _freq4 = 0x010E;
            _step4 = 0x0007;

            var tmp_data1 = new byte[_size1];
            var tmp_data2 = new byte[_size2];
            var tmp_data3 = new byte[_size1];
            var tmp_data4 = new byte[_size2];
            Array.Copy(data, _offset1, tmp_data1, 0, _size1);
            Array.Copy(data, _offset2, tmp_data2, 0, _size2);
            Array.Copy(data, _offset1, tmp_data3, 0, _size1);
            Array.Copy(data, _offset2, tmp_data4, 0, _size2);
            Player.StartChannel(Id | 0x000, tmp_data1, _size1, BASE_FREQUENCY / _freq1, _vol, 0, _size1, -127);
            Player.StartChannel(Id | 0x100, tmp_data2, _size2, BASE_FREQUENCY / _freq2, _vol, 0, _size2, 127);
            Player.StartChannel(Id | 0x200, tmp_data3, _size1, BASE_FREQUENCY / _freq3, _vol, 0, _size1, 127);
            Player.StartChannel(Id | 0x300, tmp_data4, _size2, BASE_FREQUENCY / _freq4, _vol, 0, _size2, -127);
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            Updatefreq(ref _freq1, ref _step1, 0x00AA, 0x00FA);
            Updatefreq(ref _freq2, ref _step2, 0x019A, 0x03B6);
            Updatefreq(ref _freq3, ref _step3, 0x00AA, 0x00FA);
            Updatefreq(ref _freq4, ref _step4, 0x019A, 0x03B6);
            Player.SetChannelFreq(Id | 0x000, BASE_FREQUENCY / _freq1);
            Player.SetChannelFreq(Id | 0x100, BASE_FREQUENCY / _freq2);
            Player.SetChannelFreq(Id | 0x200, BASE_FREQUENCY / _freq3);
            Player.SetChannelFreq(Id | 0x300, BASE_FREQUENCY / _freq4);
            return true;
        }

        readonly ushort _offset1;
        readonly ushort _size1;
        readonly ushort _offset2;
        readonly ushort _size2;
        readonly byte _vol;

        ushort _freq1;
        short _step1;
        ushort _freq2;
        short _step2;
        ushort _freq3;
        short _step3;
        ushort _freq4;
        short _step4;

        void Updatefreq(ref ushort freq, ref short step, ushort min, ushort max)
        {
            freq = (ushort)(freq + step);
            if (freq <= min)
            {
                freq = min;
                step = (short)-step;
            }
            if (freq >= max)
            {
                freq = max;
                step = (short)-step;
            }
        }
    }    
}
