//
//  Codec47Decoder.cs
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
using NScumm.Core;

namespace NScumm.Scumm.Smush
{
    public class Codec47Decoder
    {
        public Codec47Decoder(int width, int height)
        {
            _lastTableWidth = -1;
            _width = width;
            _height = height;
            _tableBig = new byte[256 * 388];
            _tableSmall = new byte[256 * 128];
            MakeTablesInterpolation(4);
            MakeTablesInterpolation(8);

            _frameSize = _width * _height;
            _deltaSize = _frameSize * 3;
            _deltaBuf = new byte[_deltaSize];
            _deltaBufs[0] = 0;
            _deltaBufs[1] = _frameSize;
            _curBuf = _frameSize * 2;
        }

        public bool Decode(byte[] dst, byte[] src)
        {
            if ((_tableBig == null) || (_tableSmall == null) || (_deltaBuf == null))
                return false;

            _offset1 = _deltaBufs[1] - _curBuf;
            _offset2 = _deltaBufs[0] - _curBuf;

            int seq_nb = BitConverter.ToUInt16(src, 0);

            int gfx_data = 26;

            if (seq_nb == 0)
            {
                MakeTables47(_width);
                ScummHelper.ArraySet(_deltaBuf, src[12], 0);
                ScummHelper.ArraySet(_deltaBuf, src[13], _frameSize);
                _prevSeqNb = -1;
            }

            if ((src[4] & 1) != 0)
            {
                gfx_data += 32896;
            }

            switch (src[2])
            {
                case 0:
                    Array.Copy(src, gfx_data, _deltaBuf, _curBuf, _frameSize);
                    break;
                case 1:
                // Used by Outlaws, but not by any SCUMM game.
                    throw new NotImplementedException("codec47: not implemented decode1 proc");
                case 2:
                    if (seq_nb == _prevSeqNb + 1)
                    {
                        Decode2(_deltaBuf, _curBuf, src, gfx_data, _width, _height, src, 8);
                    }
                    break;
                case 3:
                    Array.Copy(_deltaBuf, _deltaBufs[1], _deltaBuf, _curBuf, _frameSize);
                    break;
                case 4:
                    Array.Copy(_deltaBuf, _deltaBufs[0], _deltaBuf, _curBuf, _frameSize);
                    break;
                case 5:
                    Codec37Decoder.BompDecodeLine(src, gfx_data, _deltaBuf, _curBuf, BitConverter.ToInt32(src, 14));
                    break;
            }

            Array.Copy(_deltaBuf, _curBuf, dst, 0, _frameSize);

            if (seq_nb == _prevSeqNb + 1)
            {
                if (src[3] == 1)
                {
                    ScummHelper.Swap(ref _curBuf, ref _deltaBufs[1]);
                }
                else if (src[3] == 2)
                {
                    ScummHelper.Swap(ref _deltaBufs[0], ref _deltaBufs[1]);
                    ScummHelper.Swap(ref _deltaBufs[1], ref _curBuf);
                }
            }
            _prevSeqNb = seq_nb;

            return true;
        }

        void Decode2(byte[] dst, int dstPos, byte[] src, int srcPos, int width, int height, byte[] param_ptr, int param_ptrPos)
        {
            _d_src = src;
            _d_srcPos = srcPos;
            _paramPtr = param_ptr;
            _paramPtrPos = param_ptrPos - 0xf8;
            int bw = (width + 7) / 8;
            int bh = (height + 7) / 8;
            int next_line = width * 7;
            _d_pitch = width;

            do
            {
                int tmp_bw = bw;
                do
                {
                    Level1(dst, dstPos);
                    dstPos += 8;
                } while ((--tmp_bw) != 0);
                dstPos += next_line;
            } while ((--bh) != 0);
        }

        static void COPY_2X1_LINE(byte[] dst, int dstPos, byte[] src, int srcPos)
        {                   
            Array.Copy(src, srcPos, dst, dstPos, 2);
        }

        static void COPY_4X1_LINE(byte[] dst, int dstPos, byte[] src, int srcPos)
        {                   
            Array.Copy(src, srcPos, dst, dstPos, 4);
        }

        static void FILL_2X1_LINE(byte[] dst, int dstPos, byte value)
        {
            for (int i = dstPos; i < dstPos + 2; i++)
            {
                dst[i] = value;
            }
        }

        static void FILL_4X1_LINE(byte[] dst, int dstPos, byte value)
        {
            for (int i = dstPos; i < dstPos + 4; i++)
            {
                dst[i] = value;
            }
        }

        void Level3(byte[] d_dst, int dstPos)
        {
            var code = _d_src[_d_srcPos++];

            if (code < 0xF8)
            {
                var tmp = _table[code] + _offset1;
                COPY_2X1_LINE(d_dst, dstPos, d_dst, dstPos + tmp);
                COPY_2X1_LINE(d_dst, dstPos + _d_pitch, d_dst, dstPos + _d_pitch + tmp);
            }
            else if (code == 0xFF)
            {
                COPY_2X1_LINE(d_dst, dstPos, _d_src, _d_srcPos + 0);
                COPY_2X1_LINE(d_dst, dstPos + _d_pitch, _d_src, _d_srcPos + 2);
                _d_srcPos += 4;
            }
            else if (code == 0xFE)
            {
                byte t = _d_src[_d_srcPos++];
                FILL_2X1_LINE(d_dst, dstPos, t);
                FILL_2X1_LINE(d_dst, dstPos + _d_pitch, t);
            }
            else if (code == 0xFC)
            {
                var tmp = _offset2;
                COPY_2X1_LINE(d_dst, dstPos, d_dst, dstPos + tmp);
                COPY_2X1_LINE(d_dst, dstPos + _d_pitch, d_dst, dstPos + _d_pitch + tmp);
            }
            else
            {
                byte t = _paramPtr[_paramPtrPos + code];
                FILL_2X1_LINE(d_dst, dstPos, t);
                FILL_2X1_LINE(d_dst, dstPos + _d_pitch, t);
            }
        }

        void Level2(byte[] d_dst, int dstPos)
        {
            var code = _d_src[_d_srcPos++];

            if (code < 0xF8)
            {
                var tmp = _table[code] + _offset1;
                for (var i = 0; i < 4; i++)
                {
                    COPY_4X1_LINE(d_dst, dstPos, d_dst, dstPos + tmp);
                    dstPos += _d_pitch;
                }
            }
            else if (code == 0xFF)
            {
                Level3(d_dst, dstPos);
                dstPos += 2;
                Level3(d_dst, dstPos);
                dstPos += _d_pitch * 2 - 2;
                Level3(d_dst, dstPos);
                dstPos += 2;
                Level3(d_dst, dstPos);
            }
            else if (code == 0xFE)
            {
                var t = _d_src[_d_srcPos++];
                for (var i = 0; i < 4; i++)
                {
                    FILL_4X1_LINE(d_dst, dstPos, t);
                    dstPos += _d_pitch;
                }
            }
            else if (code == 0xFD)
            {
                int tmp_ptr = _d_src[_d_srcPos++] * 128;
                int l = _tableSmall[tmp_ptr + 96];
                byte val = _d_src[_d_srcPos++];
                int tmp_ptr2 = tmp_ptr;
                while ((l--) != 0)
                {
                    d_dst[dstPos + BitConverter.ToUInt16(_tableSmall, tmp_ptr2)] = val;
                    tmp_ptr2 += 2;
                }
                l = _tableSmall[tmp_ptr + 97];
                val = _d_src[_d_srcPos++];
                tmp_ptr2 = tmp_ptr + 32;
                while ((l--) != 0)
                {
                    d_dst[dstPos + BitConverter.ToUInt16(_tableSmall, tmp_ptr2)] = val;
                    tmp_ptr2 += 2;
                }
            }
            else if (code == 0xFC)
            {
                var tmp = _offset2;
                for (var i = 0; i < 4; i++)
                {
                    COPY_4X1_LINE(d_dst, dstPos, d_dst, dstPos + tmp);
                    dstPos += _d_pitch;
                }
            }
            else
            {
                byte t = _paramPtr[_paramPtrPos + code];
                for (var i = 0; i < 4; i++)
                {
                    FILL_4X1_LINE(d_dst, dstPos, t);
                    dstPos += _d_pitch;
                }
            }
        }

        void Level1(byte[] d_dst, int dstPos)
        {
            var code = _d_src[_d_srcPos++];

            if (code < 0xF8)
            {
                var tmp2 = _table[code] + _offset1;
                for (var i = 0; i < 8; i++)
                {
                    COPY_4X1_LINE(d_dst, dstPos + 0, d_dst, dstPos + tmp2);
                    COPY_4X1_LINE(d_dst, dstPos + 4, d_dst, dstPos + tmp2 + 4);
                    dstPos += _d_pitch;
                }
            }
            else if (code == 0xFF)
            {
                Level2(d_dst, dstPos);
                dstPos += 4;
                Level2(d_dst, dstPos);
                dstPos += _d_pitch * 4 - 4;
                Level2(d_dst, dstPos);
                dstPos += 4;
                Level2(d_dst, dstPos);
            }
            else if (code == 0xFE)
            {
                var t = _d_src[_d_srcPos++];
                for (var i = 0; i < 8; i++)
                {
                    FILL_4X1_LINE(d_dst, dstPos, t);
                    FILL_4X1_LINE(d_dst, dstPos + 4, t);
                    dstPos += _d_pitch;
                }
            }
            else if (code == 0xFD)
            {
                var tmp = _d_src[_d_srcPos++];
                int tmp_ptr = tmp * 388;
                byte l = _tableBig[tmp_ptr + 384];
                byte val = _d_src[_d_srcPos++];
                int tmp_ptr2 = tmp_ptr;
                while ((l--) != 0)
                {
                    d_dst[dstPos + BitConverter.ToUInt16(_tableBig, tmp_ptr2)] = val;
                    tmp_ptr2 += 2;
                }
                l = _tableBig[tmp_ptr + 385];
                val = _d_src[_d_srcPos++];
                tmp_ptr2 = tmp_ptr + 128;
                while ((l--) != 0)
                {
                    d_dst[dstPos + BitConverter.ToUInt16(_tableBig, tmp_ptr2)] = val;
                    tmp_ptr2 += 2;
                }
            }
            else if (code == 0xFC)
            {
                var tmp2 = _offset2;
                for (var i = 0; i < 8; i++)
                {
                    COPY_4X1_LINE(d_dst, dstPos + 0, d_dst, dstPos + tmp2);
                    COPY_4X1_LINE(d_dst, dstPos + 4, d_dst, dstPos + tmp2 + 4);
                    dstPos += _d_pitch;
                }
            }
            else
            {
                byte t = _paramPtr[_paramPtrPos + code];
                for (var i = 0; i < 8; i++)
                {
                    FILL_4X1_LINE(d_dst, dstPos, t);
                    FILL_4X1_LINE(d_dst, dstPos + 4, t);
                    dstPos += _d_pitch;
                }
            }
        }

        void MakeTablesInterpolation(int param)
        {
            int variable1, variable2;
            int b1, b2;
            int value_table47_1_2, value_table47_1_1, value_table47_2_2, value_table47_2_1;
            int[] tableSmallBig = new int[64];
            int tmp, s;
            sbyte[] table47_1;
            sbyte[] table47_2;
            int ptr_small_big;
            int ptr;
            int i, x, y;

            if (param == 8)
            {
                table47_1 = codec47_table_big1;
                table47_2 = codec47_table_big2;
                ptr = 0;
                for (i = 0; i < 256; i++)
                {
                    _tableBig[ptr + 384] = 0;
                    _tableBig[ptr + 385] = 0;
                    ptr += 388;
                }
            }
            else if (param == 4)
            {
                table47_1 = codec47_table_small1;
                table47_2 = codec47_table_small2;
                ptr = 0;
                for (i = 0; i < 256; i++)
                {
                    _tableSmall[ptr + 96] = 0;
                    _tableSmall[ptr + 97] = 0;
                    ptr += 128;
                }
            }
            else
            {
                throw new InvalidOperationException(string.Format("Codec47Decoder::makeTablesInterpolation: unknown param {0}", param));
            }

            s = 0;
            for (x = 0; x < 16; x++)
            {
                value_table47_1_1 = table47_1[x];
                value_table47_2_1 = table47_2[x];
                for (y = 0; y < 16; y++)
                {
                    value_table47_1_2 = table47_1[y];
                    value_table47_2_2 = table47_2[y];

                    if (value_table47_2_1 == 0)
                    {
                        b1 = 0;
                    }
                    else if (value_table47_2_1 == param - 1)
                    {
                        b1 = 1;
                    }
                    else if (value_table47_1_1 == 0)
                    {
                        b1 = 2;
                    }
                    else if (value_table47_1_1 == param - 1)
                    {
                        b1 = 3;
                    }
                    else
                    {
                        b1 = 4;
                    }

                    if (value_table47_2_2 == 0)
                    {
                        b2 = 0;
                    }
                    else if (value_table47_2_2 == param - 1)
                    {
                        b2 = 1;
                    }
                    else if (value_table47_1_2 == 0)
                    {
                        b2 = 2;
                    }
                    else if (value_table47_1_2 == param - 1)
                    {
                        b2 = 3;
                    }
                    else
                    {
                        b2 = 4;
                    }

                    Array.Clear(tableSmallBig, 0, tableSmallBig.Length);

                    variable2 = Math.Abs(value_table47_2_2 - value_table47_2_1);
                    tmp = Math.Abs(value_table47_1_2 - value_table47_1_1);
                    if (variable2 <= tmp)
                    {
                        variable2 = tmp;
                    }

                    for (variable1 = 0; variable1 <= variable2; variable1++)
                    {
                        int variable3, variable4;

                        if (variable2 > 0)
                        {
                            // Linearly interpolate between value_table47_1_1 and value_table47_1_2
                            // respectively value_table47_2_1 and value_table47_2_2.
                            variable4 = (value_table47_1_1 * variable1 + value_table47_1_2 * (variable2 - variable1) + variable2 / 2) / variable2;
                            variable3 = (value_table47_2_1 * variable1 + value_table47_2_2 * (variable2 - variable1) + variable2 / 2) / variable2;
                        }
                        else
                        {
                            variable4 = value_table47_1_1;
                            variable3 = value_table47_2_1;
                        }
                        ptr_small_big = param * variable3 + variable4;
                        tableSmallBig[ptr_small_big] = 1;
                        

                        if ((b1 == 2 && b2 == 3) || (b2 == 2 && b1 == 3) ||
                            (b1 == 0 && b2 != 1) || (b2 == 0 && b1 != 1))
                        {
                            if (variable3 >= 0)
                            {
                                i = variable3 + 1;
                                while ((i--) != 0)
                                {
                                    tableSmallBig[ptr_small_big] = 1;
                                    ptr_small_big -= param;
                                }
                            }
                        }
                        else if ((b2 != 0 && b1 == 1) || (b1 != 0 && b2 == 1))
                        {
                            if (param > variable3)
                            {
                                i = param - variable3;
                                while ((i--) != 0)
                                {
                                    tableSmallBig[ptr_small_big] = 1;
                                    ptr_small_big += param;
                                }
                            }
                        }
                        else if ((b1 == 2 && b2 != 3) || (b2 == 2 && b1 != 3))
                        {
                            if (variable4 >= 0)
                            {
                                i = variable4 + 1;
                                while ((i--) != 0)
                                {
                                    tableSmallBig[ptr_small_big--] = 1;
                                }
                            }
                        }
                        else if ((b1 == 0 && b2 == 1) || (b2 == 0 && b1 == 1) ||
                                 (b1 == 3 && b2 != 2) || (b2 == 3 && b1 != 2))
                        {
                            if (param > variable4)
                            {
                                i = param - variable4;
                                while ((i--) != 0)
                                {
                                    tableSmallBig[ptr_small_big++] = 1;
                                }
                            }
                        }
                    }

                    if (param == 8)
                    {
                        for (i = 64 - 1; i >= 0; i--)
                        {
                            if (tableSmallBig[i] != 0)
                            {
                                _tableBig[256 + s + _tableBig[384 + s]] = (byte)i;
                                _tableBig[384 + s]++;
                            }
                            else
                            {
                                _tableBig[320 + s + _tableBig[385 + s]] = (byte)i;
                                _tableBig[385 + s]++;
                            }
                        }
                        s += 388;
                    }
                    if (param == 4)
                    {
                        for (i = 16 - 1; i >= 0; i--)
                        {
                            if (tableSmallBig[i] != 0)
                            {
                                _tableSmall[64 + s + _tableSmall[96 + s]] = (byte)i;
                                _tableSmall[96 + s]++;
                            }
                            else
                            {
                                _tableSmall[80 + s + _tableSmall[97 + s]] = (byte)i;
                                _tableSmall[97 + s]++;
                            }
                        }
                        s += 128;
                    }
                }
            }
        }

        void MakeTables47(int width)
        {
            if (_lastTableWidth == width)
                return;

            _lastTableWidth = width;

            int a, c, d;
            short tmp;

            for (int l = 0; l < codec47_table.Length; l += 2)
            {
                _table[l / 2] = (short)(codec47_table[l + 1] * width + codec47_table[l]);
            }
            // Note: _table[255] is never inited; but since only the first 0xF8
            // entries of it are used anyway, this doesn't matter.

            a = 0;
            c = 0;
            do
            {
                for (d = 0; d < _tableSmall[96 + c]; d++)
                {
                    tmp = _tableSmall[64 + c + d];
                    tmp = (short)((byte)(tmp >> 2) * width + (tmp & 3));
                    _tableSmall[c + d * 2] = (byte)tmp;
                    _tableSmall[c + d * 2 + 1] = (byte)(tmp >> 8);
                }
                for (d = 0; d < _tableSmall[97 + c]; d++)
                {
                    tmp = _tableSmall[80 + c + d];
                    tmp = (short)((byte)(tmp >> 2) * width + (tmp & 3));
                    _tableSmall[32 + c + d * 2] = (byte)tmp;
                    _tableSmall[32 + c + d * 2 + 1] = (byte)(tmp >> 8);
                }
                for (d = 0; d < _tableBig[384 + a]; d++)
                {
                    tmp = _tableBig[256 + a + d];
                    tmp = (short)((byte)(tmp >> 3) * width + (tmp & 7));
                    _tableBig[a + d * 2] = (byte)tmp;
                    _tableBig[a + d * 2 + 1] = (byte)(tmp >> 8);
                }
                for (d = 0; d < _tableBig[385 + a]; d++)
                {
                    tmp = _tableBig[320 + a + d];
                    tmp = (short)((byte)(tmp >> 3) * width + (tmp & 7));
                    _tableBig[128 + a + d * 2] = (byte)tmp;
                    _tableBig[128 + a + d * 2 + 1] = (byte)(tmp >> 8);
                }

                a += 388;
                c += 128;
            } while (c < 32768);
        }

        int _deltaSize;
        int[] _deltaBufs = new int[2];
        byte[] _deltaBuf;
        int _curBuf;
        int _prevSeqNb;
        int _lastTableWidth;
        byte[] _d_src, _paramPtr;
        int _d_srcPos, _paramPtrPos;
        int _d_pitch;
        int _offset1, _offset2;
        byte[] _tableBig;
        byte[] _tableSmall;
        short[] _table = new short[256];
        int _frameSize;
        int _width, _height;

        static readonly sbyte[] codec47_table_small1 =
            {
                0, 1, 2, 3, 3, 3, 3, 2, 1, 0, 0, 0, 1, 2, 2, 1,
            };

        static readonly sbyte[] codec47_table_small2 =
            {
                0, 0, 0, 0, 1, 2, 3, 3, 3, 3, 2, 1, 1, 1, 2, 2,
            };

        static readonly sbyte[] codec47_table_big1 =
            {
                0, 2, 5, 7, 7, 7, 7, 7, 7, 5, 2, 0, 0, 0, 0, 0,
            };

        static readonly sbyte[] codec47_table_big2 =
            {
                0, 0, 0, 0, 1, 3, 4, 6, 7, 7, 7, 7, 6, 4, 3, 1,
            };

        static readonly sbyte[] codec47_table =
            {
                0,   0,  -1, -43,   6, -43,  -9, -42,  13, -41,
                -16, -40,  19, -39, -23, -36,  26, -34,  -2, -33,
                4, -33, -29, -32,  -9, -32,  11, -31, -16, -29,
                32, -29,  18, -28, -34, -26, -22, -25,  -1, -25,
                3, -25,  -7, -24,   8, -24,  24, -23,  36, -23,
                -12, -22,  13, -21, -38, -20,   0, -20, -27, -19,
                -4, -19,   4, -19, -17, -18,  -8, -17,   8, -17,
                18, -17,  28, -17,  39, -17, -12, -15,  12, -15,
                -21, -14,  -1, -14,   1, -14, -41, -13,  -5, -13,
                5, -13,  21, -13, -31, -12, -15, -11,  -8, -11,
                8, -11,  15, -11,  -2, -10,   1, -10,  31, -10,
                -23,  -9, -11,  -9,  -5,  -9,   4,  -9,  11,  -9,
                42,  -9,   6,  -8,  24,  -8, -18,  -7,  -7,  -7,
                -3,  -7,  -1,  -7,   2,  -7,  18,  -7, -43,  -6,
                -13,  -6,  -4,  -6,   4,  -6,   8,  -6, -33,  -5,
                -9,  -5,  -2,  -5,   0,  -5,   2,  -5,   5,  -5,
                13,  -5, -25,  -4,  -6,  -4,  -3,  -4,   3,  -4,
                9,  -4, -19,  -3,  -7,  -3,  -4,  -3,  -2,  -3,
                -1,  -3,   0,  -3,   1,  -3,   2,  -3,   4,  -3,
                6,  -3,  33,  -3, -14,  -2, -10,  -2,  -5,  -2,
                -3,  -2,  -2,  -2,  -1,  -2,   0,  -2,   1,  -2,
                2,  -2,   3,  -2,   5,  -2,   7,  -2,  14,  -2,
                19,  -2,  25,  -2,  43,  -2,  -7,  -1,  -3,  -1,
                -2,  -1,  -1,  -1,   0,  -1,   1,  -1,   2,  -1,
                3,  -1,  10,  -1,  -5,   0,  -3,   0,  -2,   0,
                -1,   0,   1,   0,   2,   0,   3,   0,   5,   0,
                7,   0, -10,   1,  -7,   1,  -3,   1,  -2,   1,
                -1,   1,   0,   1,   1,   1,   2,   1,   3,   1,
                -43,   2, -25,   2, -19,   2, -14,   2,  -5,   2,
                -3,   2,  -2,   2,  -1,   2,   0,   2,   1,   2,
                2,   2,   3,   2,   5,   2,   7,   2,  10,   2,
                14,   2, -33,   3,  -6,   3,  -4,   3,  -2,   3,
                -1,   3,   0,   3,   1,   3,   2,   3,   4,   3,
                19,   3,  -9,   4,  -3,   4,   3,   4,   7,   4,
                25,   4, -13,   5,  -5,   5,  -2,   5,   0,   5,
                2,   5,   5,   5,   9,   5,  33,   5,  -8,   6,
                -4,   6,   4,   6,  13,   6,  43,   6, -18,   7,
                -2,   7,   0,   7,   2,   7,   7,   7,  18,   7,
                -24,   8,  -6,   8, -42,   9, -11,   9,  -4,   9,
                5,   9,  11,   9,  23,   9, -31,  10,  -1,  10,
                2,  10, -15,  11,  -8,  11,   8,  11,  15,  11,
                31,  12, -21,  13,  -5,  13,   5,  13,  41,  13,
                -1,  14,   1,  14,  21,  14, -12,  15,  12,  15,
                -39,  17, -28,  17, -18,  17,  -8,  17,   8,  17,
                17,  18,  -4,  19,   0,  19,   4,  19,  27,  19,
                38,  20, -13,  21,  12,  22, -36,  23, -24,  23,
                -8,  24,   7,  24,  -3,  25,   1,  25,  22,  25,
                34,  26, -18,  28, -32,  29,  16,  29, -11,  31,
                9,  32,  29,  32,  -4,  33,   2,  33, -26,  34,
                23,  36, -19,  39,  16,  40, -13,  41,   9,  42,
                -6,  43,   1,  43,   0,   0,   0,   0,   0,   0
            };

    }
}

