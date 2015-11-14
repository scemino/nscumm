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
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.Audio.Decoders;

namespace NScumm.Scumm.Audio.IMuse.IMuseDigital
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

        public static void InitializeImcTables()
        {
            int pos;

            if (_destImcTable == null)
                _destImcTable = new byte[89];
            if (_destImcTable2 == null)
                _destImcTable2 = new uint[89 * 64];

            for (pos = 0; pos <= 88; ++pos)
            {
                byte put = 1;
                int tableValue = ((Ima_ADPCMStream._imaTable[pos] * 4) / 7) / 2;
                while (tableValue != 0)
                {
                    tableValue /= 2;
                    put++;
                }
                if (put < 3)
                {
                    put = 3;
                }
                if (put > 8)
                {
                    put = 8;
                }
                _destImcTable[pos] = (byte)(put - 1);
            }

            for (int n = 0; n < 64; n++)
            {
                for (pos = 0; pos <= 88; ++pos)
                {
                    int count = 32;
                    int put = 0;
                    int tableValue = Ima_ADPCMStream._imaTable[pos];
                    do
                    {
                        if ((count & n) != 0)
                        {
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
                Array.Copy(BitConverter.GetBytes(ScummHelper.SwapBytes((ushort)tmp)), 0, dst, ptr, 2);
                ptr += 2;
                tmp = ((((v2 & 0xf0) << 4) | v3) << 4) - 0x8000;
                Array.Copy(BitConverter.GetBytes(ScummHelper.SwapBytes((ushort)tmp)), 0, dst, ptr, 2);
                ptr += 2;
            }
            return s_size;
        }

        public static int DecompressCodec(int codec, byte[] compInput, byte[] compOutput, int inputSize)
        {
            int outputSize;
            int offset1, offset2, offset3, length, k, c, s, j, r, t, z;
            int ptr;
            byte t_tmp1, t_tmp2;

            switch (codec)
            {
                case 0:
                    Array.Copy(compOutput, compOutput, inputSize);
                    outputSize = inputSize;
                    break;

                case 1:
                    outputSize = CompDecode(compInput, compOutput);
                    break;

                case 2:
                    outputSize = CompDecode(compInput, compOutput);
                    for (z = 1; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];
                    break;

                case 3:
                    outputSize = CompDecode(compInput, compOutput);
                    for (z = 2; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];
                    for (z = 1; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];
                    break;

                case 4:
                    outputSize = CompDecode(compInput, compOutput);
                    for (z = 2; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];
                    for (z = 1; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];

                    var t_table = new byte[outputSize];

                    var src = compOutput;
                    length = (outputSize << 3) / 12;
                    k = 0;
                    if (length > 0)
                    {
                        c = -12;
                        s = 0;
                        j = 0;
                        do
                        {
                            ptr = length + (k >> 1);
                            t_tmp2 = src[j];
                            if ((k & 1) != 0)
                            {
                                r = c >> 3;
                                t_table[r + 2] = (byte)(((t_tmp2 & 0x0f) << 4) | (src[ptr + 1] >> 4));
                                t_table[r + 1] = (byte)((t_tmp2 & 0xf0) | (t_table[r + 1]));
                            }
                            else
                            {
                                r = s >> 3;
                                t_table[r + 0] = (byte)(((t_tmp2 & 0x0f) << 4) | (src[ptr] & 0x0f));
                                t_table[r + 1] = (byte)(t_tmp2 >> 4);
                            }
                            s += 12;
                            c += 12;
                            k++;
                            j++;
                        } while (k < length);
                    }
                    offset1 = ((length - 1) * 3) >> 1;
                    t_table[offset1 + 1] = (byte)((t_table[offset1 + 1]) | (src[length - 1] & 0xf0));
                    Array.Copy(t_table, src, outputSize);
                    break;

                case 5:
                    outputSize = CompDecode(compInput, compOutput);
                    for (z = 2; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];
                    for (z = 1; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];

                    t_table = new byte[outputSize];

                    src = compOutput;
                    length = (outputSize << 3) / 12;
                    k = 1;
                    c = 0;
                    s = 12;
                    t_table[0] = (byte)(src[length] >> 4);
                    t = length + k;
                    j = 1;
                    if (t > k)
                    {
                        do
                        {
                            t_tmp1 = src[length + (k >> 1)];
                            t_tmp2 = src[j - 1];
                            if ((k & 1) != 0)
                            {
                                r = c >> 3;
                                t_table[r + 0] = (byte)((t_tmp2 & 0xf0) | t_table[r]);
                                t_table[r + 1] = (byte)(((t_tmp2 & 0x0f) << 4) | (t_tmp1 & 0x0f));
                            }
                            else
                            {
                                r = s >> 3;
                                t_table[r + 0] = (byte)(t_tmp2 >> 4);
                                t_table[r - 1] = (byte)(((t_tmp2 & 0x0f) << 4) | (t_tmp1 >> 4));
                            }
                            s += 12;
                            c += 12;
                            k++;
                            j++;
                        } while (k < t);
                    }
                    Array.Copy(t_table, src, outputSize);
                    break;

                case 6:
                    outputSize = CompDecode(compInput, compOutput);
                    for (z = 2; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];
                    for (z = 1; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];

                    t_table = new byte[outputSize];

                    src = compOutput;
                    length = (outputSize << 3) / 12;
                    k = 0;
                    c = 0;
                    j = 0;
                    s = -12;
                    t_table[0] = src[outputSize - 1];
                    t_table[outputSize - 1] = src[length - 1];
                    t = length - 1;
                    if (t > 0)
                    {
                        do
                        {
                            t_tmp1 = src[length + (k >> 1)];
                            t_tmp2 = src[j];
                            if ((k & 1) != 0)
                            {
                                r = s >> 3;
                                t_table[r + 2] = (byte)((t_tmp2 & 0xf0) | t_table[r + 2]);
                                t_table[r + 3] = (byte)(((t_tmp2 & 0x0f) << 4) | (t_tmp1 >> 4));
                            }
                            else
                            {
                                r = c >> 3;
                                t_table[r + 2] = (byte)(t_tmp2 >> 4);
                                t_table[r + 1] = (byte)(((t_tmp2 & 0x0f) << 4) | (t_tmp1 & 0x0f));
                            }
                            s += 12;
                            c += 12;
                            k++;
                            j++;
                        } while (k < t);
                    }
                    Array.Copy(t_table, src, outputSize);
                    break;

                case 10:
                    outputSize = CompDecode(compInput, compOutput);
                    for (z = 2; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];
                    for (z = 1; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];

                    t_table = new byte[outputSize];
                    Array.Copy(compOutput, t_table, outputSize);

                    offset1 = outputSize / 3;
                    offset2 = offset1 << 1;
                    offset3 = offset2;
                    src = compOutput;

                    while ((offset1--) != 0)
                    {
                        offset2 -= 2;
                        offset3--;
                        t_table[offset2 + 0] = src[offset1];
                        t_table[offset2 + 1] = src[offset3];
                    }

                    src = compOutput;
                    length = (outputSize << 3) / 12;
                    k = 0;
                    if (length > 0)
                    {
                        c = -12;
                        s = 0;
                        do
                        {
                            j = length + (k >> 1);
                            t_tmp1 = t_table[k];
                            if ((k & 1) != 0)
                            {
                                r = c >> 3;
                                t_tmp2 = t_table[j + 1];
                                src[r + 2] = (byte)(((t_tmp1 & 0x0f) << 4) | (t_tmp2 >> 4));
                                src[r + 1] = (byte)((src[r + 1]) | (t_tmp1 & 0xf0));
                            }
                            else
                            {
                                r = s >> 3;
                                t_tmp2 = t_table[j];
                                src[r + 0] = (byte)(((t_tmp1 & 0x0f) << 4) | (t_tmp2 & 0x0f));
                                src[r + 1] = (byte)(t_tmp1 >> 4);
                            }
                            s += 12;
                            c += 12;
                            k++;
                        } while (k < length);
                    }
                    offset1 = ((length - 1) * 3) >> 1;
                    src[offset1 + 1] = (byte)((t_table[length] & 0xf0) | src[offset1 + 1]);
                    break;

                case 11:
                    outputSize = CompDecode(compInput, compOutput);
                    for (z = 2; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];
                    for (z = 1; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];

                    t_table = new byte[outputSize];
                    Array.Copy(compOutput, t_table, outputSize);

                    offset1 = outputSize / 3;
                    offset2 = offset1 << 1;
                    offset3 = offset2;
                    src = compOutput;

                    while ((offset1--) != 0)
                    {
                        offset2 -= 2;
                        offset3--;
                        t_table[offset2 + 0] = src[offset1];
                        t_table[offset2 + 1] = src[offset3];
                    }

                    src = compOutput;
                    length = (outputSize << 3) / 12;
                    k = 1;
                    c = 0;
                    s = 12;
                    t_tmp1 = (byte)(t_table[length] >> 4);
                    src[0] = t_tmp1;
                    t = length + k;
                    if (t > k)
                    {
                        do
                        {
                            j = length + (k >> 1);
                            t_tmp1 = t_table[k - 1];
                            t_tmp2 = t_table[j];
                            if ((k & 1) != 0)
                            {
                                r = c >> 3;
                                src[r + 0] = (byte)((src[r]) | (t_tmp1 & 0xf0));
                                src[r + 1] = (byte)(((t_tmp1 & 0x0f) << 4) | (t_tmp2 & 0x0f));
                            }
                            else
                            {
                                r = s >> 3;
                                src[r + 0] = (byte)(t_tmp1 >> 4);
                                src[r - 1] = (byte)(((t_tmp1 & 0x0f) << 4) | (t_tmp2 >> 4));
                            }
                            s += 12;
                            c += 12;
                            k++;
                        } while (k < t);
                    }
                    break;

                case 12:
                    outputSize = CompDecode(compInput, compOutput);
                    for (z = 2; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];
                    for (z = 1; z < outputSize; z++)
                        compOutput[z] += compOutput[z - 1];

                    t_table = new byte[outputSize];
                    Array.Copy(compOutput, t_table, outputSize);

                    offset1 = outputSize / 3;
                    offset2 = offset1 << 1;
                    offset3 = offset2;
                    src = compOutput;

                    while ((offset1--) != 0)
                    {
                        offset2 -= 2;
                        offset3--;
                        t_table[offset2 + 0] = src[offset1];
                        t_table[offset2 + 1] = src[offset3];
                    }

                    src = compOutput;
                    length = (outputSize << 3) / 12;
                    k = 0;
                    c = 0;
                    s = -12;
                    src[0] = t_table[outputSize - 1];
                    src[outputSize - 1] = t_table[length - 1];
                    t = length - 1;
                    if (t > 0)
                    {
                        do
                        {
                            j = length + (k >> 1);
                            t_tmp1 = t_table[k];
                            t_tmp2 = t_table[j];
                            if ((k & 1) != 0)
                            {
                                r = s >> 3;
                                src[r + 2] = (byte)((src[r + 2]) | (t_tmp1 & 0xf0));
                                src[r + 3] = (byte)(((t_tmp1 & 0x0f) << 4) | (t_tmp2 >> 4));
                            }
                            else
                            {
                                r = c >> 3;
                                src[r + 2] = (byte)(t_tmp1 >> 4);
                                src[r + 1] = (byte)(((t_tmp1 & 0x0f) << 4) | (t_tmp2 & 0x0f));
                            }
                            s += 12;
                            c += 12;
                            k++;
                        } while (k < t);
                    }
                    break;

                case 13:
                case 15:
                    outputSize = DecompressADPCM(compInput, compOutput, (codec == 13) ? 1 : 2);
                    break;

                default:
//                    Console.Error.WriteLine("BundleCodecs::decompressCodec() Unknown codec {0}", codec);
                    outputSize = 0;
                    break;
            }

            return outputSize;
        }

        static int DecompressADPCM(byte[] compInput, byte[] compOutput, int channels)
        {
            // Decoder for the the IMA ADPCM variants used in COMI.
            // Contrary to regular IMA ADPCM, this codec uses a variable
            // bitsize for the encoded data.

            const int MAX_CHANNELS = 2;
            int outputSamplesLeft;
            int destPos;
            short firstWord;
            var initialTablePos = new byte[MAX_CHANNELS];
            //int32 initialimcTableEntry[MAX_CHANNELS] = {7, 7};
            var initialOutputWord = new int[MAX_CHANNELS];
            int totalBitOffset, curTablePos, outputWord;

            // We only support mono and stereo
            Debug.Assert(channels == 1 || channels == 2);

            var src = compInput;
            var dst = compOutput;
            int srcPos = 0;
            int dstPos = 0;
            outputSamplesLeft = 0x1000;

            // Every data packet contains 0x2000 bytes of audio data
            // when extracted. In order to encode bigger data sets,
            // one has to split the data into multiple blocks.
            //
            // Every block starts with a 2 byte word. If that word is
            // non-zero, it indicates the size of a block of raw audio
            // data (not encoded) following it. That data we simply copy
            // to the output buffer and then proceed by decoding the
            // remaining data.
            //
            // If on the other hand the word is zero, then what follows
            // are 7*channels bytes containing seed data for the decoder.
            firstWord = src.ToInt16BigEndian();
            srcPos += 2;
            if (firstWord != 0)
            {
                // Copy raw data
                Array.Copy(src, srcPos, dst, dstPos, firstWord);
                dstPos += firstWord;
                srcPos += firstWord;
                Debug.Assert((firstWord & 1) == 0);
                outputSamplesLeft -= firstWord / 2;
            }
            else
            {
                // Read the seed values for the decoder.
                for (var i = 0; i < channels; i++)
                {
                    initialTablePos[i] = src[srcPos];
                    srcPos += 1;
                    //initialimcTableEntry[i] = READ_BE_UINT32(src);
                    srcPos += 4;
                    initialOutputWord[i] = src.ToInt32BigEndian(srcPos);
                    srcPos += 4;
                }
            }

            totalBitOffset = 0;
            // The channels are encoded separately.
            for (int chan = 0; chan < channels; chan++)
            {
                // Read initial state (this makes it possible for the data stream
                // to be split & spread across multiple data chunks.
                curTablePos = initialTablePos[chan];
                //imcTableEntry = initialimcTableEntry[chan];
                outputWord = initialOutputWord[chan];

                // We need to interleave the channels in the output; we achieve
                // that by using a variables dest offset:
                destPos = chan * 2;

                var bound = (channels == 1)
                    ? outputSamplesLeft
                    : ((chan == 0)
                        ? (outputSamplesLeft + 1) / 2
                        : outputSamplesLeft / 2);
                for (var i = 0; i < bound; ++i)
                {
                    // Determine the size (in bits) of the next data packet
                    var curTableEntryBitCount = _destImcTable[curTablePos];
                    Debug.Assert(2 <= curTableEntryBitCount && curTableEntryBitCount <= 7);

                    // Read the next data packet
                    int readPos = srcPos + (totalBitOffset >> 3);
                    var readWord = (ushort)(src.ToInt16BigEndian(readPos) << (totalBitOffset & 7));
                    var packet = (byte)(readWord >> (16 - curTableEntryBitCount));

                    // Advance read position to the next data packet
                    totalBitOffset += curTableEntryBitCount;

                    // Decode the data packet into a delta value for the output signal.
                    byte signBitMask = (byte)(1 << (curTableEntryBitCount - 1));
                    byte dataBitMask = (byte)(signBitMask - 1);
                    byte data = (byte)(packet & dataBitMask);

                    var tmpA = (data << (7 - curTableEntryBitCount));
                    int imcTableEntry = Ima_ADPCMStream._imaTable[curTablePos] >> (curTableEntryBitCount - 1);
                    int delta = (int)(imcTableEntry + _destImcTable2[tmpA + (curTablePos * 64)]);

                    // The topmost bit in the data packet tells is a sign bit
                    if ((packet & signBitMask) != 0)
                    {
                        delta = -delta;
                    }

                    // Accumulate the delta onto the output data
                    outputWord += delta;

                    // Clip outputWord to 16 bit signed, and write it into the destination stream
                    outputWord = ScummHelper.Clip(outputWord, -0x8000, 0x7fff);
                    ScummHelper.WriteUInt16BigEndian(dst, dstPos + destPos, (ushort)outputWord);
                    destPos += channels << 1;

                    // Adjust the curTablePos
                    curTablePos += (sbyte)imxOtherTable[curTableEntryBitCount - 2][data];
                    curTablePos = ScummHelper.Clip(curTablePos, 0, Ima_ADPCMStream._imaTable.Length - 1);
                }
            }

            return 0x2000;
        }

        static int NextBit(byte[] src, ref int srcptr, ref int mask, ref int bitsleft)
        {
            var bit = mask & 1;                    
            mask >>= 1;                        
            if ((--bitsleft) == 0)
            {                 
                mask = src.ToUInt16(srcptr); 
                srcptr += 2;                   
                bitsleft = 16;                 
            } 
            return bit;
        }

        static int CompDecode(byte[] src, byte[] dst)
        {
            int result, srcptr;
            int dstptr = 0;
            int data, size, bit, bitsleft = 16;
            int mask = src.ToUInt16();
            srcptr = 2;

            for (;;)
            {
                bit = NextBit(src, ref srcptr, ref mask, ref bitsleft);
                if (bit != 0)
                {
                    dst[dstptr++] = src[srcptr++];
                }
                else
                {
                    bit = NextBit(src, ref srcptr, ref mask, ref bitsleft);
                    if (bit == 0)
                    {
                        bit = NextBit(src, ref srcptr, ref mask, ref bitsleft);
                        size = bit << 1;
                        bit = NextBit(src, ref srcptr, ref mask, ref bitsleft);
                        size = (size | bit) + 3;
                        data = (int)(src[srcptr++] | 0xffffff00);
                    }
                    else
                    {
                        data = src[srcptr++];
                        size = src[srcptr++];

                        data |= (int)(0xfffff000 + ((size & 0xf0) << 4));
                        size = (size & 0x0f) + 3;

                        if (size == 3)
                        if ((src[srcptr++] + 1) == 1)
                            return dstptr;
                    }
                    result = dstptr + data;
                    while ((size--) != 0)
                        dst[dstptr++] = dst[result++];
                }
            }
        }
    
    
        // This table is the "big brother" of Audio::ADPCMStream::_stepAdjustTable.
        static readonly byte[][] imxOtherTable = {
            new byte[]
            {
                0xFF,
                4
            },
            new byte[]
            {
                0xFF, 0xFF,
                2,    8
            },
            new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF,
                1,    2,    4,    6
            },
            new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                1,    2,    4,    6,    8,   12,   16,   32
            },
            new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                1,    2,    4,    6,    8,   10,   12,   14,
                16,   18,   20,   22,   24,   26,   28,   32
            },
            new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                1,    2,    3,    4,    5,    6,    7,    8,
                9,   10,   11,   12,   13,   14,   15,   16,
                17,   18,   19,   20,   21,   22,   23,   24,
                25,   26,   27,   28,   29,   30,   31,   32
            }
        };
    }

}