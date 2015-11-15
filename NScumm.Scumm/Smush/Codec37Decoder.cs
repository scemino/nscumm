//
//  Codec37Decoder.cs
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

namespace NScumm.Scumm.Smush
{
    public class Codec37Decoder
    {
        public Codec37Decoder(int width, int height)
        {
            _width = width;
            _height = height;
            _frameSize = _width * _height;
            _deltaSize = _frameSize * 3 + 0x13600;
            _deltaBuf = new byte[_deltaSize];
            _deltaBufs[0] = 0x4D80;
            _deltaBufs[1] = 0xE880 + _frameSize;
            _offsetTable = new short[255];
            _curtable = 0;
            _prevSeqNb = 0;
            _tableLastPitch = -1;
            _tableLastIndex = -1;
        }

        public void Decode(byte[] dst, byte[] src)
        {
            int bw = (_width + 3) / 4, bh = (_height + 3) / 4;
            int pitch = bw * 4;

            var seq_nb = BitConverter.ToUInt16(src, 2);
            var decoded_size = (int)BitConverter.ToUInt32(src, 4);
            byte mask_flags = src[12];
            Maketable(pitch, src[1]);
            int tmp;

            switch (src[0])
            {
//                case 0:
//                    if ((_deltaBufs[_curtable] - _deltaBuf) > 0) {
//                        memset(_deltaBuf, 0, _deltaBufs[_curtable] - _deltaBuf);
//                    }
//                    tmp = (_deltaBuf + _deltaSize) - _deltaBufs[_curtable] - decoded_size;
//                    if (tmp > 0) {
//                        memset(_deltaBufs[_curtable] + decoded_size, 0, tmp);
//                    }
//                    memcpy(_deltaBufs[_curtable], src + 16, decoded_size);
//                    break;
//                case 1:
//                    if ((seq_nb & 1) || !(mask_flags & 1)) {
//                        _curtable ^= 1;
//                    }
//                    proc1(_deltaBufs[_curtable], src + 16, _deltaBufs[_curtable ^ 1] - _deltaBufs[_curtable],
//                        bw, bh, pitch, _offsetTable);
//                    break;
                case 2:
                    BompDecodeLine(src, 16, _deltaBuf, _deltaBufs[_curtable], decoded_size);
                    if (_deltaBufs[_curtable] > 0)
                    {
                        Array.Clear(_deltaBuf, 0, _deltaBufs[_curtable]);
                    }
                    tmp = _deltaSize - _deltaBufs[_curtable] - decoded_size;
                    if (tmp > 0)
                    {
                        Array.Clear(_deltaBuf, _deltaBufs[_curtable] + decoded_size, tmp);
                    }
                    break;
                case 3:
                    if (((seq_nb & 1) != 0) || ((mask_flags & 1) == 0))
                    {
                        _curtable ^= 1;
                    }

                    if ((mask_flags & 4) != 0)
                    {
                        Proc3WithFDFE(_deltaBuf, _deltaBufs[_curtable], src, 16,
                            _deltaBufs[_curtable ^ 1] - _deltaBufs[_curtable], bw, bh,
                            pitch, _offsetTable);
                    }
                    else
                    {
                        Proc3WithoutFDFE(_deltaBuf, _deltaBufs[_curtable], src, 16,
                            _deltaBufs[_curtable ^ 1] - _deltaBufs[_curtable], bw, bh,
                            pitch, _offsetTable);
                    }
                    break;
//                case 4:
//                    if ((seq_nb & 1) || !(mask_flags & 1)) {
//                        _curtable ^= 1;
//                    }
//
//                    if ((mask_flags & 4) != 0) {
//                        proc4WithFDFE(_deltaBufs[_curtable], src + 16,
//                            _deltaBufs[_curtable ^ 1] - _deltaBufs[_curtable], bw, bh,
//                            pitch, _offsetTable);
//                    } else {
//                        proc4WithoutFDFE(_deltaBufs[_curtable], src + 16,
//                            _deltaBufs[_curtable ^ 1] - _deltaBufs[_curtable], bw, bh,
//                            pitch, _offsetTable);
//                    }
//                    break;
                default:
                    throw new NotImplementedException();
                    //break;
            }
            _prevSeqNb = seq_nb;

            Array.Copy(_deltaBuf, _deltaBufs[_curtable], dst, 0, _frameSize);
        }

        void Proc3WithFDFE(byte[] dst, int dstPos, byte[] src, int srcPos, int next_offs, int bw, int bh, int pitch, short[] _offsetTable)
        {
            do
            {
                int i = bw;
                do
                {
                    var code = src[srcPos++];
                    if (code == 0xFD)
                    {
                        Literal4x4(src, ref srcPos, dst, ref dstPos, pitch);
                    }
                    else if (code == 0xFE)
                    {
                        Literal4X1(src, ref srcPos, dst, ref dstPos, pitch);
                    }
                    else if (code == 0xFF)
                    {
                        Literal1X1(src, ref srcPos, dst, ref dstPos, pitch);
                    }
                    else
                    {
                        var dstPos2 = dstPos + _offsetTable[code] + next_offs;
                        COPY_4X4(dst, dstPos2, dst, ref dstPos, pitch);
                    }
                } while ((--i) != 0);
                dstPos += pitch * 3;
            } while ((--bh) != 0);
        }

        void Proc3WithoutFDFE(byte[] dst, int dstPos, byte[] src, int srcPos, int next_offs, int bw, int bh, int pitch, short[] _offsetTable)
        {
            do
            {
                int i = bw;
                do
                {
                    var code = src[srcPos++];
                    if (code == 0xFF)
                    {
                        Literal1X1(src, ref srcPos, dst, ref dstPos, pitch);
                    }
                    else
                    {
                        var dstPos2 = dstPos + _offsetTable[code] + next_offs;
                        COPY_4X4(dst, dstPos2, dst, ref dstPos, pitch);
                    }
                } while ((--i) != 0);
                dstPos += pitch * 3;
            } while ((--bh) != 0);
        }

        /* Copy a 4x4 pixel block from a different place in the framebuffer */
        void COPY_4X4(byte[] dst2, int dstPos2, byte[] dst, ref int dstPos, int pitch)
        {
            for (var x = 0; x < 4; x++)
            {
                COPY_4X1_LINE(dst, dstPos + pitch * x, dst2, dstPos2 + pitch * x);
            }
            dstPos += 4;
        }

        /* Fill a 4x4 pixel block with a literal pixel value */
        void Literal4x4(byte[] src, ref int srcPos, byte[] dst, ref int dstPos, int pitch)
        {
            int t = READ_LITERAL_PIXEL(src, ref srcPos);         
            for (var x = 0; x < 4; x++)
            {               
                WRITE_4X1_LINE(dst, dstPos + pitch * x, t); 
            }                       
            dstPos += 4;
        }

        /* Fill four 4x1 pixel blocks with literal pixel values */
        void Literal4X1(byte[] src, ref int srcPos, byte[] dst, ref int dstPos, int pitch)
        {
            for (var x = 0; x < 4; x++)
            {               
                int t = READ_LITERAL_PIXEL(src, ref srcPos);
                WRITE_4X1_LINE(dst, dstPos + pitch * x, t); 
            }                       
            dstPos += 4;   
        }

        /* Fill sixteen 1x1 pixel blocks with literal pixel values */
        void Literal1X1(byte[] src, ref int srcPos, byte[] dst, ref int dstPos, int pitch)
        {
            for (var x = 0; x < 4; x++)
            {               
                COPY_4X1_LINE(dst, dstPos + pitch * x, src, srcPos);
                srcPos += 4;
            }            
            dstPos += 4;
        }

        void COPY_4X1_LINE(byte[] dst, int dstPos, byte[] src, int srcPos)
        {
            Array.Copy(src, srcPos, dst, dstPos, 4);
        }

        void WRITE_4X1_LINE(byte[] dst, int dstPos, int value)
        {
            Array.Copy(BitConverter.GetBytes(value), 0, dst, dstPos, 4);
        }

        int READ_LITERAL_PIXEL(byte[] src, ref int srcPos)
        {
            int v = src[srcPos++];
            v += (v << 8) + (v << 16) + (v << 24);
            return v;
        }

        public static void BompDecodeLine(byte[] src, int srcPos, byte[] dst, int dstPos, int len)
        {
            while (len > 0)
            {
                var code = src[srcPos++];
                var num = (code >> 1) + 1;
                if (num > len)
                    num = len;
                len -= num;
                if ((code & 1) != 0)
                {
                    var color = src[srcPos++];
                    for (int i = 0; i < num; i++)
                    {
                        dst[dstPos + i] = color;
                    }
                }
                else
                {
                    for (int i = 0; i < num; i++)
                    {
                        dst[dstPos + i] = src[srcPos++];
                    }
                }
                dstPos += num;
            }
        }

        void Maketable(int pitch, int index)
        {
            if (_tableLastPitch == pitch && _tableLastIndex == index)
                return;

            _tableLastPitch = pitch;
            _tableLastIndex = index;
            index *= 255;
            Debug.Assert((index + 254) < (maketable_bytes.Length / 2));

            for (var i = 0; i < 255; i++)
            {
                var j = (i + index) * 2;
                _offsetTable[i] = (short)(maketable_bytes[j + 1] * pitch + maketable_bytes[j]);
            }
        }

        int _deltaSize;
        int[] _deltaBufs = new int[2];
        byte[] _deltaBuf;
        short[] _offsetTable;
        int _curtable;
        ushort _prevSeqNb;
        int _tableLastPitch;
        int _tableLastIndex;
        int _frameSize;
        int _width, _height;

        static int[] maketable_bytes =
            {
                0,   0,   1,   0,   2,   0,   3,   0,   5,   0,
                8,   0,  13,   0,  21,   0,  -1,   0,  -2,   0,
                -3,   0,  -5,   0,  -8,   0, -13,   0, -17,   0,
                -21,   0,   0,   1,   1,   1,   2,   1,   3,   1,
                5,   1,   8,   1,  13,   1,  21,   1,  -1,   1,
                -2,   1,  -3,   1,  -5,   1,  -8,   1, -13,   1,
                -17,   1, -21,   1,   0,   2,   1,   2,   2,   2,
                3,   2,   5,   2,   8,   2,  13,   2,  21,   2,
                -1,   2,  -2,   2,  -3,   2,  -5,   2,  -8,   2,
                -13,   2, -17,   2, -21,   2,   0,   3,   1,   3,
                2,   3,   3,   3,   5,   3,   8,   3,  13,   3,
                21,   3,  -1,   3,  -2,   3,  -3,   3,  -5,   3,
                -8,   3, -13,   3, -17,   3, -21,   3,   0,   5,
                1,   5,   2,   5,   3,   5,   5,   5,   8,   5,
                13,   5,  21,   5,  -1,   5,  -2,   5,  -3,   5,
                -5,   5,  -8,   5, -13,   5, -17,   5, -21,   5,
                0,   8,   1,   8,   2,   8,   3,   8,   5,   8,
                8,   8,  13,   8,  21,   8,  -1,   8,  -2,   8,
                -3,   8,  -5,   8,  -8,   8, -13,   8, -17,   8,
                -21,   8,   0,  13,   1,  13,   2,  13,   3,  13,
                5,  13,   8,  13,  13,  13,  21,  13,  -1,  13,
                -2,  13,  -3,  13,  -5,  13,  -8,  13, -13,  13,
                -17,  13, -21,  13,   0,  21,   1,  21,   2,  21,
                3,  21,   5,  21,   8,  21,  13,  21,  21,  21,
                -1,  21,  -2,  21,  -3,  21,  -5,  21,  -8,  21,
                -13,  21, -17,  21, -21,  21,   0,  -1,   1,  -1,
                2,  -1,   3,  -1,   5,  -1,   8,  -1,  13,  -1,
                21,  -1,  -1,  -1,  -2,  -1,  -3,  -1,  -5,  -1,
                -8,  -1, -13,  -1, -17,  -1, -21,  -1,   0,  -2,
                1,  -2,   2,  -2,   3,  -2,   5,  -2,   8,  -2,
                13,  -2,  21,  -2,  -1,  -2,  -2,  -2,  -3,  -2,
                -5,  -2,  -8,  -2, -13,  -2, -17,  -2, -21,  -2,
                0,  -3,   1,  -3,   2,  -3,   3,  -3,   5,  -3,
                8,  -3,  13,  -3,  21,  -3,  -1,  -3,  -2,  -3,
                -3,  -3,  -5,  -3,  -8,  -3, -13,  -3, -17,  -3,
                -21,  -3,   0,  -5,   1,  -5,   2,  -5,   3,  -5,
                5,  -5,   8,  -5,  13,  -5,  21,  -5,  -1,  -5,
                -2,  -5,  -3,  -5,  -5,  -5,  -8,  -5, -13,  -5,
                -17,  -5, -21,  -5,   0,  -8,   1,  -8,   2,  -8,
                3,  -8,   5,  -8,   8,  -8,  13,  -8,  21,  -8,
                -1,  -8,  -2,  -8,  -3,  -8,  -5,  -8,  -8,  -8,
                -13,  -8, -17,  -8, -21,  -8,   0, -13,   1, -13,
                2, -13,   3, -13,   5, -13,   8, -13,  13, -13,
                21, -13,  -1, -13,  -2, -13,  -3, -13,  -5, -13,
                -8, -13, -13, -13, -17, -13, -21, -13,   0, -17,
                1, -17,   2, -17,   3, -17,   5, -17,   8, -17,
                13, -17,  21, -17,  -1, -17,  -2, -17,  -3, -17,
                -5, -17,  -8, -17, -13, -17, -17, -17, -21, -17,
                0, -21,   1, -21,   2, -21,   3, -21,   5, -21,
                8, -21,  13, -21,  21, -21,  -1, -21,  -2, -21,
                -3, -21,  -5, -21,  -8, -21, -13, -21, -17, -21,
                0,   0,  -8, -29,   8, -29, -18, -25,  17, -25,
                0, -23,  -6, -22,   6, -22, -13, -19,  12, -19,
                0, -18,  25, -18, -25, -17,  -5, -17,   5, -17,
                -10, -15,  10, -15,   0, -14,  -4, -13,   4, -13,
                19, -13, -19, -12,  -8, -11,  -2, -11,   0, -11,
                2, -11,   8, -11, -15, -10,  -4, -10,   4, -10,
                15, -10,  -6,  -9,  -1,  -9,   1,  -9,   6,  -9,
                -29,  -8, -11,  -8,  -8,  -8,  -3,  -8,   3,  -8,
                8,  -8,  11,  -8,  29,  -8,  -5,  -7,  -2,  -7,
                0,  -7,   2,  -7,   5,  -7, -22,  -6,  -9,  -6,
                -6,  -6,  -3,  -6,  -1,  -6,   1,  -6,   3,  -6,
                6,  -6,   9,  -6,  22,  -6, -17,  -5,  -7,  -5,
                -4,  -5,  -2,  -5,   0,  -5,   2,  -5,   4,  -5,
                7,  -5,  17,  -5, -13,  -4, -10,  -4,  -5,  -4,
                -3,  -4,  -1,  -4,   0,  -4,   1,  -4,   3,  -4,
                5,  -4,  10,  -4,  13,  -4,  -8,  -3,  -6,  -3,
                -4,  -3,  -3,  -3,  -2,  -3,  -1,  -3,   0,  -3,
                1,  -3,   2,  -3,   4,  -3,   6,  -3,   8,  -3,
                -11,  -2,  -7,  -2,  -5,  -2,  -3,  -2,  -2,  -2,
                -1,  -2,   0,  -2,   1,  -2,   2,  -2,   3,  -2,
                5,  -2,   7,  -2,  11,  -2,  -9,  -1,  -6,  -1,
                -4,  -1,  -3,  -1,  -2,  -1,  -1,  -1,   0,  -1,
                1,  -1,   2,  -1,   3,  -1,   4,  -1,   6,  -1,
                9,  -1, -31,   0, -23,   0, -18,   0, -14,   0,
                -11,   0,  -7,   0,  -5,   0,  -4,   0,  -3,   0,
                -2,   0,  -1,   0,   0, -31,   1,   0,   2,   0,
                3,   0,   4,   0,   5,   0,   7,   0,  11,   0,
                14,   0,  18,   0,  23,   0,  31,   0,  -9,   1,
                -6,   1,  -4,   1,  -3,   1,  -2,   1,  -1,   1,
                0,   1,   1,   1,   2,   1,   3,   1,   4,   1,
                6,   1,   9,   1, -11,   2,  -7,   2,  -5,   2,
                -3,   2,  -2,   2,  -1,   2,   0,   2,   1,   2,
                2,   2,   3,   2,   5,   2,   7,   2,  11,   2,
                -8,   3,  -6,   3,  -4,   3,  -2,   3,  -1,   3,
                0,   3,   1,   3,   2,   3,   3,   3,   4,   3,
                6,   3,   8,   3, -13,   4, -10,   4,  -5,   4,
                -3,   4,  -1,   4,   0,   4,   1,   4,   3,   4,
                5,   4,  10,   4,  13,   4, -17,   5,  -7,   5,
                -4,   5,  -2,   5,   0,   5,   2,   5,   4,   5,
                7,   5,  17,   5, -22,   6,  -9,   6,  -6,   6,
                -3,   6,  -1,   6,   1,   6,   3,   6,   6,   6,
                9,   6,  22,   6,  -5,   7,  -2,   7,   0,   7,
                2,   7,   5,   7, -29,   8, -11,   8,  -8,   8,
                -3,   8,   3,   8,   8,   8,  11,   8,  29,   8,
                -6,   9,  -1,   9,   1,   9,   6,   9, -15,  10,
                -4,  10,   4,  10,  15,  10,  -8,  11,  -2,  11,
                0,  11,   2,  11,   8,  11,  19,  12, -19,  13,
                -4,  13,   4,  13,   0,  14, -10,  15,  10,  15,
                -5,  17,   5,  17,  25,  17, -25,  18,   0,  18,
                -12,  19,  13,  19,  -6,  22,   6,  22,   0,  23,
                -17,  25,  18,  25,  -8,  29,   8,  29,   0,  31,
                0,   0,  -6, -22,   6, -22, -13, -19,  12, -19,
                0, -18,  -5, -17,   5, -17, -10, -15,  10, -15,
                0, -14,  -4, -13,   4, -13,  19, -13, -19, -12,
                -8, -11,  -2, -11,   0, -11,   2, -11,   8, -11,
                -15, -10,  -4, -10,   4, -10,  15, -10,  -6,  -9,
                -1,  -9,   1,  -9,   6,  -9, -11,  -8,  -8,  -8,
                -3,  -8,   0,  -8,   3,  -8,   8,  -8,  11,  -8,
                -5,  -7,  -2,  -7,   0,  -7,   2,  -7,   5,  -7,
                -22,  -6,  -9,  -6,  -6,  -6,  -3,  -6,  -1,  -6,
                1,  -6,   3,  -6,   6,  -6,   9,  -6,  22,  -6,
                -17,  -5,  -7,  -5,  -4,  -5,  -2,  -5,  -1,  -5,
                0,  -5,   1,  -5,   2,  -5,   4,  -5,   7,  -5,
                17,  -5, -13,  -4, -10,  -4,  -5,  -4,  -3,  -4,
                -2,  -4,  -1,  -4,   0,  -4,   1,  -4,   2,  -4,
                3,  -4,   5,  -4,  10,  -4,  13,  -4,  -8,  -3,
                -6,  -3,  -4,  -3,  -3,  -3,  -2,  -3,  -1,  -3,
                0,  -3,   1,  -3,   2,  -3,   3,  -3,   4,  -3,
                6,  -3,   8,  -3, -11,  -2,  -7,  -2,  -5,  -2,
                -4,  -2,  -3,  -2,  -2,  -2,  -1,  -2,   0,  -2,
                1,  -2,   2,  -2,   3,  -2,   4,  -2,   5,  -2,
                7,  -2,  11,  -2,  -9,  -1,  -6,  -1,  -5,  -1,
                -4,  -1,  -3,  -1,  -2,  -1,  -1,  -1,   0,  -1,
                1,  -1,   2,  -1,   3,  -1,   4,  -1,   5,  -1,
                6,  -1,   9,  -1, -23,   0, -18,   0, -14,   0,
                -11,   0,  -7,   0,  -5,   0,  -4,   0,  -3,   0,
                -2,   0,  -1,   0,   0, -23,   1,   0,   2,   0,
                3,   0,   4,   0,   5,   0,   7,   0,  11,   0,
                14,   0,  18,   0,  23,   0,  -9,   1,  -6,   1,
                -5,   1,  -4,   1,  -3,   1,  -2,   1,  -1,   1,
                0,   1,   1,   1,   2,   1,   3,   1,   4,   1,
                5,   1,   6,   1,   9,   1, -11,   2,  -7,   2,
                -5,   2,  -4,   2,  -3,   2,  -2,   2,  -1,   2,
                0,   2,   1,   2,   2,   2,   3,   2,   4,   2,
                5,   2,   7,   2,  11,   2,  -8,   3,  -6,   3,
                -4,   3,  -3,   3,  -2,   3,  -1,   3,   0,   3,
                1,   3,   2,   3,   3,   3,   4,   3,   6,   3,
                8,   3, -13,   4, -10,   4,  -5,   4,  -3,   4,
                -2,   4,  -1,   4,   0,   4,   1,   4,   2,   4,
                3,   4,   5,   4,  10,   4,  13,   4, -17,   5,
                -7,   5,  -4,   5,  -2,   5,  -1,   5,   0,   5,
                1,   5,   2,   5,   4,   5,   7,   5,  17,   5,
                -22,   6,  -9,   6,  -6,   6,  -3,   6,  -1,   6,
                1,   6,   3,   6,   6,   6,   9,   6,  22,   6,
                -5,   7,  -2,   7,   0,   7,   2,   7,   5,   7,
                -11,   8,  -8,   8,  -3,   8,   0,   8,   3,   8,
                8,   8,  11,   8,  -6,   9,  -1,   9,   1,   9,
                6,   9, -15,  10,  -4,  10,   4,  10,  15,  10,
                -8,  11,  -2,  11,   0,  11,   2,  11,   8,  11,
                19,  12, -19,  13,  -4,  13,   4,  13,   0,  14,
                -10,  15,  10,  15,  -5,  17,   5,  17,   0,  18,
                -12,  19,  13,  19,  -6,  22,   6,  22,   0,  23,
            };
    }
}

