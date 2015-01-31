//
//  BundleCodecs.cs
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

namespace NScumm.Core
{
    static class BundleCodecs
    {
        /*
 * The "IMC" codec below (see cases 13 & 15 in decompressCodec) is actually a
 * variant of the IMA codec, see also
 *   <http://www.multimedia.cx/simpleaudio.html>
 *
 * It is somewhat different, though: the standard ADPCM codecs use a fixed
 * size for their data packets (4 bits), while the codec implemented here
 * varies the size of each "packet" between 2 and 7 bits.
 */

        static byte[] _destImcTable;
        static uint[] _destImcTable2;

        public static void InitializeImcTables() {
            int pos;

            if (_destImcTable==null)
                _destImcTable = new byte[89];
            if (_destImcTable2 == null)
                _destImcTable2 = new uint[89 * 64];

            for (pos = 0; pos <= 88; ++pos) {
                byte put = 1;
                int tableValue = ((Ima_ADPCMStream._imaTable[pos] * 4) / 7) / 2;
                while (tableValue != 0) {
                    tableValue /= 2;
                    put++;
                }
                if (put < 3) {
                    put = 3;
                }
                if (put > 8) {
                    put = 8;
                }
                _destImcTable[pos] = (byte)(put - 1);
            }

            for (int n = 0; n < 64; n++) {
                for (pos = 0; pos <= 88; ++pos) {
                    int count = 32;
                    int put = 0;
                    int tableValue = Ima_ADPCMStream._imaTable[pos];
                    do {
                        if ((count & n) != 0) {
                            put += tableValue;
                        }
                        count /= 2;
                        tableValue /= 2;
                    } while (count != 0);
                    _destImcTable2[n + pos * 64] = (uint)put;
                }
            }
        }

        public static int Decode12BitsSample(byte[] src, out byte[] dst, int size)
        {
            int loop_size = size / 3;
            int s_size = loop_size * 4;
            dst = new byte[s_size];
            var i = 0;
            var ptr = 0;
            int tmp;
            while ((loop_size--) != 0)
            {
                byte v1 = src[i++];
                byte v2 = src[i++];
                byte v3 = src[i++];
                tmp = ((((v2 & 0x0f) << 8) | v1) << 4) - 0x8000;
                Array.Copy(BitConverter.GetBytes(tmp), dst, ptr);
                ptr += 2;
                tmp = ((((v2 & 0xf0) << 4) | v3) << 4) - 0x8000;
                Array.Copy(BitConverter.GetBytes(tmp), dst, ptr);
                ptr += 2;
            }
            return s_size;
        }

    }
}

