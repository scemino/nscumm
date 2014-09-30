//
//  HookDatas.cs
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


namespace NScumm.Core
{
    class HookDatas
    {
        byte[] jump;
        byte transpose;
        byte[] partOnOff;
        byte[] partVolume;
        byte[] partProgram;
        byte[] partTranspose;

        public int QueryParam(int param, int chan)
        {
            switch (param)
            {
                case 18:
                    return jump[0];
                case 19:
                    return transpose;
                case 20:
                    return partOnOff[chan];
                case 21:
                    return partVolume[chan];
                case 22:
                    return partProgram[chan];
                case 23:
                    return partTranspose[chan];
                default:
                    return -1;
            }
        }

        public int Set(int cls, int value, int chan)
        {
            switch (cls)
            {
                case 0:
                    if (value != jump[0])
                    {
                        jump[1] = jump[0];
                        jump[0] = (byte)value;
                    }
                    break;
                case 1:
                    transpose = (byte)value;
                    break;
                case 2:
                    if (chan < 16)
                        partOnOff[chan] = (byte)value;
                    else if (chan == 16)
                        Set(partOnOff, (byte)value);
                    break;
                case 3:
                    if (chan < 16)
                        partVolume[chan] = (byte)value;
                    else if (chan == 16)
                        Set(partVolume, (byte)value);
                    break;
                case 4:
                    if (chan < 16)
                        partProgram[chan] = (byte)value;
                    else if (chan == 16)
                        Set(partProgram, (byte)value);
                    break;
                case 5:
                    if (chan < 16)
                        partTranspose[chan] = (byte)value;
                    else if (chan == 16)
                        Set(partTranspose, (byte)value);
                    break;
                default:
                    return -1;
            }
            return 0;
        }

        public HookDatas()
        { 
            jump = new byte[2];
            partOnOff = new byte[16];
            partVolume = new byte[16];
            partProgram = new byte[16];
            partTranspose = new byte[16];
        }

        static void Set<T>(T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = value;
            }
        }
    };
}
