//
//  Channel.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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
using ChannelData = NScumm.Core.Audio.OPL.OPL3.ChannelData;

namespace NScumm.Core.Audio.OPL
{
    abstract class Channel
    {
        internal int channelBaseAddress;
        protected double[] feedback;
        protected int fnuml, fnumh, kon, block, cha, chb, chc, chd, fb, cnt;
        // Factor to convert between normalized amplitude to normalized
        // radians. The amplitude maximum is equivalent to 8*Pi radians.
        internal const double toPhase = 4;

        internal Channel(int baseAddress)
        {
            channelBaseAddress = baseAddress;
            fnuml = fnumh = kon = block = cha = chb = chc = chd = fb = cnt = 0;
            feedback = new double[2];
            feedback[0] = feedback[1] = 0;
        }

        internal void update_2_KON1_BLOCK3_FNUMH2()
        {

            int _2_kon1_block3_fnumh2 = OPL3.registers[channelBaseAddress + ChannelData._2_KON1_BLOCK3_FNUMH2_Offset];

            // Frequency Number (hi-register) and Block. These two registers, together with fnuml, 
            // sets the Channel´s base frequency;
            block = (_2_kon1_block3_fnumh2 & 0x1C) >> 2;
            fnumh = _2_kon1_block3_fnumh2 & 0x03;
            updateOperators();

            // Key On. If changed, calls Channel.keyOn() / keyOff().
            int newKon = (_2_kon1_block3_fnumh2 & 0x20) >> 5;
            if (newKon != kon)
            {
                if (newKon == 1)
                    keyOn();
                else
                    keyOff();
                kon = newKon;
            }
        }

        internal void update_FNUML8()
        {
            int fnuml8 = OPL3.registers[channelBaseAddress + ChannelData.FNUML8_Offset];
            // Frequency Number, low register.
            fnuml = fnuml8 & 0xFF;
            updateOperators();
        }

        internal void update_CHD1_CHC1_CHB1_CHA1_FB3_CNT1()
        {
            int chd1_chc1_chb1_cha1_fb3_cnt1 = OPL3.registers[channelBaseAddress + ChannelData.CHD1_CHC1_CHB1_CHA1_FB3_CNT1_Offset];
            chd = (chd1_chc1_chb1_cha1_fb3_cnt1 & 0x80) >> 7;
            chc = (chd1_chc1_chb1_cha1_fb3_cnt1 & 0x40) >> 6;
            chb = (chd1_chc1_chb1_cha1_fb3_cnt1 & 0x20) >> 5;
            cha = (chd1_chc1_chb1_cha1_fb3_cnt1 & 0x10) >> 4;
            fb = (chd1_chc1_chb1_cha1_fb3_cnt1 & 0x0E) >> 1;
            cnt = chd1_chc1_chb1_cha1_fb3_cnt1 & 0x01;
            updateOperators();
        }

        internal void updateChannel()
        {
            update_2_KON1_BLOCK3_FNUMH2();
            update_FNUML8();
            update_CHD1_CHC1_CHB1_CHA1_FB3_CNT1();
        }

        protected double[] getInFourChannels(double channelOutput)
        {
            double[] output = new double[4];

            if (OPL3._new == 0)
                output[0] = output[1] = output[2] = output[3] = channelOutput;
            else
            {
                output[0] = (cha == 1) ? channelOutput : 0;
                output[1] = (chb == 1) ? channelOutput : 0;
                output[2] = (chc == 1) ? channelOutput : 0;
                output[3] = (chd == 1) ? channelOutput : 0;
            }

            return output;
        }

        public abstract double[] getChannelOutput();

        protected abstract void keyOn();

        protected abstract void keyOff();

        protected abstract void updateOperators();
    }
}

