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

using NScumm.Core.Audio;
using NScumm.Core.Common;
using NScumm.Core.Graphics;
using System;
using System.IO;

namespace NScumm.Core.Video
{
    partial class PsxStreamDecoder
    {
        class PsxVideoTrack : VideoTrack
        {
            private enum PlaneType
            {
                Y = 0,
                U = 1,
                V = 2
            }

            private const int AC_CODE_COUNT = 113;
            private const uint ESCAPE_CODE = uint.MaxValue;
            private const uint END_OF_BLOCK = uint.MaxValue - 1; // arbitrary, just so we can tell what code it is

            /// <summary>
            /// Here are the codes/lengths/symbols that are used for decoding
            /// DC coefficients (version 3 frames only)
            /// </summary>
            private const int DC_CODE_COUNT = 9;

            private Surface _surface;
            private uint _frameCount;
            private Timestamp _nextFrameStartTime;
            private bool _endOfTrack;
            private int _curFrame;

            private ushort _macroBlocksW, _macroBlocksH;
            private byte[] _yBuffer;
            private byte[] _cbBuffer;
            private byte[] _crBuffer;

            private Huffman _acHuffman;
            private Huffman _dcHuffmanChroma;
            private Huffman _dcHuffmanLuma;

            private int[] _lastDC = new int[3];

            private ushort _width, _height;

            public PsxVideoTrack(Stream firstSector, CDSpeed speed, uint frameCount, PixelFormat screenFormat)
            {
                var br = new BinaryReader(firstSector);
                _nextFrameStartTime = new Timestamp(0, (int)speed);
                _frameCount = frameCount;

                firstSector.Seek(40, SeekOrigin.Begin);
                _width = br.ReadUInt16();
                _height = br.ReadUInt16();
                _surface = new Surface(_width, _height, screenFormat, false);

                _macroBlocksW = (ushort)((_width + 15) / 16);
                _macroBlocksH = (ushort)((_height + 15) / 16);
                _yBuffer = new byte[_macroBlocksW * _macroBlocksH * 16 * 16];
                _cbBuffer = new byte[_macroBlocksW * _macroBlocksH * 8 * 8];
                _crBuffer = new byte[_macroBlocksW * _macroBlocksH * 8 * 8];

                _endOfTrack = false;
                _curFrame = -1;
                _acHuffman = new Huffman(0, AC_CODE_COUNT, s_huffmanACCodes, s_huffmanACLengths, s_huffmanACSymbols);
                _dcHuffmanChroma = new Huffman(0, DC_CODE_COUNT, s_huffmanDCChromaCodes, s_huffmanDCChromaLengths, s_huffmanDCSymbols);
                _dcHuffmanLuma = new Huffman(0, DC_CODE_COUNT, s_huffmanDCLumaCodes, s_huffmanDCLumaLengths, s_huffmanDCSymbols);
            }

            public override ushort Width
            {
                get { return _width; }
            }

            public override ushort Height
            {
                get { return _height; }
            }

            public override PixelFormat PixelFormat
            {
                get { return _surface.PixelFormat; }
            }

            public override int CurrentFrame
            {
                get { return _curFrame; }
            }

            public override int FrameCount
            {
                get { return (int)_frameCount; }
            }

            public override bool EndOfTrack
            {
                get { return _endOfTrack; }
            }


            public void SetEndOfTrack()
            {
                _endOfTrack = true;
            }

            public override uint GetNextFrameStartTime()
            {
                return (uint)_nextFrameStartTime.Milliseconds;
            }

            public void DecodeFrame(Stream frame, int sectorCount)
            {
                // A frame is essentially an MPEG-1 intra frame

                var bits = new BitStream(frame, BitStreamType.SixteenBits, true, true);

                bits.Skip(16); // unknown
                bits.Skip(16); // 0x3800
                ushort scale = (ushort)bits.GetBits(16);
                ushort version = (ushort)bits.GetBits(16);

                if (version != 2 && version != 3)
                    throw new InvalidOperationException("Unknown PSX stream frame version");

                // Initalize default v3 DC here
                _lastDC[0] = _lastDC[1] = _lastDC[2] = 0;

                for (int mbX = 0; mbX < _macroBlocksW; mbX++)
                    for (int mbY = 0; mbY < _macroBlocksH; mbY++)
                        DecodeMacroBlock(bits, mbX, mbY, scale, version);

                // Output data onto the frame
                YUVToRGBManager.Convert420(_surface, LuminanceScale.ScaleFull, _yBuffer, _cbBuffer, _crBuffer, _surface.Width, _surface.Height, _macroBlocksW * 16, _macroBlocksW * 8);

                _curFrame++;

                // Increase the time by the amount of sectors we read
                // One may notice that this is still not the most precise
                // method since a frame takes up the time its sectors took
                // up instead of the amount of time it takes the next frame
                // to be read from the sectors. The actual frame rate should
                // be constant instead of variable, so the slight difference
                // in a frame's showing time is negligible (1/150 of a second).
                _nextFrameStartTime = _nextFrameStartTime.AddFrames(sectorCount);
            }

            public override Surface DecodeNextFrame()
            {
                return _surface;
            }


            private void DecodeMacroBlock(BitStream bits, int mbX, int mbY, ushort scale, ushort version)
            {
                int pitchY = _macroBlocksW * 16;
                int pitchC = _macroBlocksW * 8;

                // Note the strange order of red before blue
                DecodeBlock(bits, _crBuffer, (mbY * pitchC + mbX) * 8, pitchC, scale, version, PlaneType.V);
                DecodeBlock(bits, _cbBuffer, (mbY * pitchC + mbX) * 8, pitchC, scale, version, PlaneType.U);
                DecodeBlock(bits, _yBuffer, (mbY * pitchY + mbX) * 16, pitchY, scale, version, PlaneType.Y);
                DecodeBlock(bits, _yBuffer, (mbY * pitchY + mbX) * 16 + 8, pitchY, scale, version, PlaneType.Y);
                DecodeBlock(bits, _yBuffer, (mbY * pitchY + mbX) * 16 + 8 * pitchY, pitchY, scale, version, PlaneType.Y);
                DecodeBlock(bits, _yBuffer, (mbY * pitchY + mbX) * 16 + 8 * pitchY + 8, pitchY, scale, version, PlaneType.Y);
            }

            private void DecodeBlock(BitStream bits, byte[] block, int offset, int pitch, ushort scale, ushort version, PlaneType plane)
            {
                // Version 2 just has signed 10 bits for DC
                // Version 3 has them huffman coded
                int[] coefficients = new int[8 * 8];
                coefficients[0] = ReadDC(bits, version, plane);
                ReadAC(bits, coefficients, 1); // Read in the AC

                // Dequantize
                float[] dequantData = new float[8 * 8];
                DequantizeBlock(coefficients, dequantData, scale);

                // Perform IDCT
                float[] idctData = new float[8 * 8];
                Idct(dequantData, idctData);

                // Now output the data
                for (int y = 0; y < 8; y++)
                {
                    var dst = offset + pitch * y;

                    // Convert the result to be in the range [0, 255]
                    for (int x = 0; x < 8; x++)
                        block[dst++] = (byte)(ScummHelper.Clip(idctData[y * 8 + x], -128.0f, 127.0f) + 128);
                }
            }

            private void Idct(float[] dequantData, float[] result)
            {
                // IDCT code based on JPEG's IDCT code
                // TODO: Switch to the integer-based one mentioned in the docs
                // This is by far the costliest operation here

                float[] tmp = new float[8 * 8];
                var d = 0;

                // Apply 1D IDCT to rows
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        tmp[y + x * 8] = (float)(dequantData[d + 0] * s_idct8x8[x][0]
                                        + dequantData[d + 1] * s_idct8x8[x][1]
                                        + dequantData[d + 2] * s_idct8x8[x][2]
                                        + dequantData[d + 3] * s_idct8x8[x][3]
                                        + dequantData[d + 4] * s_idct8x8[x][4]
                                        + dequantData[d + 5] * s_idct8x8[x][5]
                                        + dequantData[d + 6] * s_idct8x8[x][6]
                                        + dequantData[d + 7] * s_idct8x8[x][7]);
                    }

                    d += 8;
                }

                // Apply 1D IDCT to columns
                for (int x = 0; x < 8; x++)
                {
                    var u = x * 8;
                    for (int y = 0; y < 8; y++)
                    {
                        result[y * 8 + x] = (float)(tmp[u + 0] * s_idct8x8[y][0]
                                                    + tmp[u + 1] * s_idct8x8[y][1]
                                                    + tmp[u + 2] * s_idct8x8[y][2]
                                                    + tmp[u + 3] * s_idct8x8[y][3]
                                                    + tmp[u + 4] * s_idct8x8[y][4]
                                                    + tmp[u + 5] * s_idct8x8[y][5]
                                                    + tmp[u + 6] * s_idct8x8[y][6]
                                                    + tmp[u + 7] * s_idct8x8[y][7]);
                    }
                }
            }

            private void DequantizeBlock(int[] coefficients, float[] block, ushort scale)
            {
                // Dequantize the data, un-zig-zagging as we go along
                for (int i = 0; i < 8 * 8; i++)
                {
                    if (i == 0) // Special case for the DC coefficient
                        block[i] = coefficients[i] * s_quantizationTable[i];
                    else
                        block[i] = (float)coefficients[s_zigZagTable[i]] * s_quantizationTable[i] * scale / 8;
                }
            }

            private void ReadAC(BitStream bits, int[] block, int offset)
            {
                // Clear the block first
                for (int i = 0; i < 63; i++)
                    block[offset + i] = 0;

                int count = 0;
                int b = offset;
                while (!bits.IsEndOfStream)
                {
                    uint symbol = _acHuffman.GetSymbol(bits);

                    if (symbol == ESCAPE_CODE)
                    {
                        // The escape code!
                        int zeroes = (int)bits.GetBits(6);
                        count += zeroes + 1;
                        BLOCK_OVERFLOW_CHECK(count);
                        b += zeroes;
                        block[b++] = ReadSignedCoefficient(bits);
                    }
                    else if (symbol == END_OF_BLOCK)
                    {
                        // We're done
                        break;
                    }
                    else {
                        // Normal huffman code
                        int zeroes = GET_AC_ZERO_RUN(symbol);
                        count += zeroes + 1;
                        BLOCK_OVERFLOW_CHECK(count);
                        b += zeroes;

                        if (bits.GetBit() != 0)
                            block[b++] = -GET_AC_COEFFICIENT(symbol);
                        else
                            block[b++] = GET_AC_COEFFICIENT(symbol);
                    }
                }
            }

            private int ReadDC(BitStream bits, ushort version, PlaneType plane)
            {
                // Version 2 just has its coefficient as 10-bits
                if (version == 2)
                    return ReadSignedCoefficient(bits);

                // Version 3 has it stored as huffman codes as a difference from the previous DC value

                var huffman = (plane == PlaneType.Y) ? _dcHuffmanLuma : _dcHuffmanChroma;

                uint symbol = huffman.GetSymbol(bits);
                int dc = 0;

                if (GET_DC_BITS(symbol) != 0)
                {
                    bool negative = (bits.GetBit() == 0);
                    dc = (int)bits.GetBits(GET_DC_BITS(symbol) - 1);

                    if (negative)
                        dc -= GET_DC_NEG(symbol);
                    else
                        dc += GET_DC_POS(symbol);
                }

                _lastDC[(int)plane] += dc * 4; // convert from 8-bit to 10-bit
                return _lastDC[(int)plane];
            }

            private int ReadSignedCoefficient(BitStream bits)
            {
                uint val = bits.GetBits(10);

                // extend the sign
                int shift = 8 * sizeof(int) - 10;
                return (int)(val << shift) >> shift;
            }

            private static void BLOCK_OVERFLOW_CHECK(int count)
            {
                if (count > 63)
                    throw new InvalidOperationException("PSXStreamDecoder::readAC(): Too many coefficients");
            }

            private static int GET_AC_ZERO_RUN(uint code)
            {
                return (int)(code >> 8);
            }

            private static int GET_AC_COEFFICIENT(uint code)
            {
                return ((int)(code & 0xff));
            }

            private static int GET_DC_BITS(uint x)
            {
                return (int)(x >> 16);
            }

            private static int GET_DC_NEG(uint x)
            {
                return (int)((x >> 8) & 0xff);
            }

            private static int GET_DC_POS(uint x)
            {
                return (int)(x & 0xff);
            }

            private static uint AC_HUFF_VAL(int z, int a)
            {
                return (uint)((z << 8) | a);
            }

            private static uint DC_HUFF_VAL(int b, int n, int p)
            {
                return (uint)(((b) << 16) | ((n) << 8) | (p));
            }

            // IDCT table built with :
            // _idct8x8[x][y] = cos(((2 * x + 1) * y) * (M_PI / 16.0)) * 0.5;
            // _idct8x8[x][y] /= sqrt(2.0) if y == 0
            static readonly double[][] s_idct8x8 = {
                new double[]{ 0.353553390593274,  0.490392640201615,  0.461939766255643,  0.415734806151273,  0.353553390593274,  0.277785116509801,  0.191341716182545,  0.097545161008064 },
                new double[]{ 0.353553390593274,  0.415734806151273,  0.191341716182545, -0.097545161008064, -0.353553390593274, -0.490392640201615, -0.461939766255643, -0.277785116509801 },
                new double[]{ 0.353553390593274,  0.277785116509801, -0.191341716182545, -0.490392640201615, -0.353553390593274,  0.097545161008064,  0.461939766255643,  0.415734806151273 },
                new double[]{ 0.353553390593274,  0.097545161008064, -0.461939766255643, -0.277785116509801,  0.353553390593274,  0.415734806151273, -0.191341716182545, -0.490392640201615 },
                new double[]{ 0.353553390593274, -0.097545161008064, -0.461939766255643,  0.277785116509801,  0.353553390593274, -0.415734806151273, -0.191341716182545,  0.490392640201615 },
                new double[]{ 0.353553390593274, -0.277785116509801, -0.191341716182545,  0.490392640201615, -0.353553390593273, -0.097545161008064,  0.461939766255643, -0.415734806151273 },
                new double[]{ 0.353553390593274, -0.415734806151273,  0.191341716182545,  0.097545161008064, -0.353553390593274,  0.490392640201615, -0.461939766255643,  0.277785116509801 },
                new double[]{ 0.353553390593274, -0.490392640201615,  0.461939766255643, -0.415734806151273,  0.353553390593273, -0.277785116509801,  0.191341716182545, -0.097545161008064 }
            };

            // Standard JPEG/MPEG zig zag table
            static readonly byte[] s_zigZagTable = {
                 0,  1,  5,  6, 14, 15, 27, 28,
                 2,  4,  7, 13, 16, 26, 29, 42,
                 3,  8, 12, 17, 25, 30, 41, 43,
                 9, 11, 18, 24, 31, 40, 44, 53,
                10, 19, 23, 32, 39, 45, 52, 54,
                20, 22, 33, 38, 46, 51, 55, 60,
                21, 34, 37, 47, 50, 56, 59, 61,
                35, 36, 48, 49, 57, 58, 62, 63
            };

            // One byte different from the standard MPEG-1 table
            static readonly byte[] s_quantizationTable = {
                 2, 16, 19, 22, 26, 27, 29, 34,
                16, 16, 22, 24, 27, 29, 34, 37,
                19, 22, 26, 27, 29, 34, 34, 38,
                22, 22, 26, 27, 29, 34, 37, 40,
                22, 26, 27, 29, 32, 35, 40, 48,
                26, 27, 29, 32, 35, 40, 48, 58,
                26, 27, 29, 34, 38, 46, 56, 69,
                27, 29, 35, 38, 46, 56, 69, 83
            };

            static readonly uint[] s_huffmanACCodes = {
	            // Regular codes
	            3, 3, 4, 5, 5, 6, 7, 4, 5, 6, 7, 4, 5, 6, 7,
                32, 33, 34, 35, 36, 37, 38, 39, 8, 9, 10, 11,
                12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22,
                23, 24, 25, 26, 27, 28, 29, 30, 31, 16, 17,
                18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28,
                29, 30, 31, 16, 17, 18, 19, 20, 21, 22, 23,
                24, 25, 26, 27, 28, 29, 30, 31, 16, 17, 18,
                19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
                30, 31, 16, 17, 18, 19, 20, 21, 22, 23, 24,
                25, 26, 27, 28, 29, 30, 31,

	            // Escape code
	            1,
	            // End of block code
	            2
            };

            static readonly byte[] s_huffmanACLengths = {
	            // Regular codes
	            2, 3, 4, 4, 5, 5, 5, 6, 6, 6, 6, 7, 7, 7, 7,
                8, 8, 8, 8, 8, 8, 8, 8, 10, 10, 10, 10, 10,
                10, 10, 10, 12, 12, 12, 12, 12, 12, 12, 12,
                12, 12, 12, 12, 12, 12, 12, 12, 13, 13, 13,
                13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
                13, 13, 14, 14, 14, 14, 14, 14, 14, 14, 14,
                14, 14, 14, 14, 14, 14, 14, 15, 15, 15, 15,
                15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
                15, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16,
                16, 16, 16, 16, 16, 16,

	            // Escape code
	            6,
	            // End of block code
	            2
            };

            static readonly uint[] s_huffmanACSymbols = {
	            // Regular codes
	            AC_HUFF_VAL(0, 1), AC_HUFF_VAL(1, 1), AC_HUFF_VAL(0, 2), AC_HUFF_VAL(2, 1), AC_HUFF_VAL(0, 3),
                AC_HUFF_VAL(4, 1), AC_HUFF_VAL(3, 1), AC_HUFF_VAL(7, 1), AC_HUFF_VAL(6, 1), AC_HUFF_VAL(1, 2),
                AC_HUFF_VAL(5, 1), AC_HUFF_VAL(2, 2), AC_HUFF_VAL(9, 1), AC_HUFF_VAL(0, 4), AC_HUFF_VAL(8, 1),
                AC_HUFF_VAL(13, 1), AC_HUFF_VAL(0, 6), AC_HUFF_VAL(12, 1), AC_HUFF_VAL(11, 1), AC_HUFF_VAL(3, 2),
                AC_HUFF_VAL(1, 3), AC_HUFF_VAL(0, 5), AC_HUFF_VAL(10, 1), AC_HUFF_VAL(16, 1), AC_HUFF_VAL(5, 2),
                AC_HUFF_VAL(0, 7), AC_HUFF_VAL(2, 3), AC_HUFF_VAL(1, 4), AC_HUFF_VAL(15, 1), AC_HUFF_VAL(14, 1),
                AC_HUFF_VAL(4, 2), AC_HUFF_VAL(0, 11), AC_HUFF_VAL(8, 2), AC_HUFF_VAL(4, 3), AC_HUFF_VAL(0, 10),
                AC_HUFF_VAL(2, 4), AC_HUFF_VAL(7, 2), AC_HUFF_VAL(21, 1), AC_HUFF_VAL(20, 1), AC_HUFF_VAL(0, 9),
                AC_HUFF_VAL(19, 1), AC_HUFF_VAL(18, 1), AC_HUFF_VAL(1, 5), AC_HUFF_VAL(3, 3), AC_HUFF_VAL(0, 8),
                AC_HUFF_VAL(6, 2), AC_HUFF_VAL(17, 1), AC_HUFF_VAL(10, 2), AC_HUFF_VAL(9, 2), AC_HUFF_VAL(5, 3),
                AC_HUFF_VAL(3, 4), AC_HUFF_VAL(2, 5), AC_HUFF_VAL(1, 7), AC_HUFF_VAL(1, 6), AC_HUFF_VAL(0, 15),
                AC_HUFF_VAL(0, 14), AC_HUFF_VAL(0, 13), AC_HUFF_VAL(0, 12), AC_HUFF_VAL(26, 1), AC_HUFF_VAL(25, 1),
                AC_HUFF_VAL(24, 1), AC_HUFF_VAL(23, 1), AC_HUFF_VAL(22, 1), AC_HUFF_VAL(0, 31), AC_HUFF_VAL(0, 30),
                AC_HUFF_VAL(0, 29), AC_HUFF_VAL(0, 28), AC_HUFF_VAL(0, 27), AC_HUFF_VAL(0, 26), AC_HUFF_VAL(0, 25),
                AC_HUFF_VAL(0, 24), AC_HUFF_VAL(0, 23), AC_HUFF_VAL(0, 22), AC_HUFF_VAL(0, 21), AC_HUFF_VAL(0, 20),
                AC_HUFF_VAL(0, 19), AC_HUFF_VAL(0, 18), AC_HUFF_VAL(0, 17), AC_HUFF_VAL(0, 16), AC_HUFF_VAL(0, 40),
                AC_HUFF_VAL(0, 39), AC_HUFF_VAL(0, 38), AC_HUFF_VAL(0, 37), AC_HUFF_VAL(0, 36), AC_HUFF_VAL(0, 35),
                AC_HUFF_VAL(0, 34), AC_HUFF_VAL(0, 33), AC_HUFF_VAL(0, 32), AC_HUFF_VAL(1, 14), AC_HUFF_VAL(1, 13),
                AC_HUFF_VAL(1, 12), AC_HUFF_VAL(1, 11), AC_HUFF_VAL(1, 10), AC_HUFF_VAL(1, 9), AC_HUFF_VAL(1, 8),
                AC_HUFF_VAL(1, 18), AC_HUFF_VAL(1, 17), AC_HUFF_VAL(1, 16), AC_HUFF_VAL(1, 15), AC_HUFF_VAL(6, 3),
                AC_HUFF_VAL(16, 2), AC_HUFF_VAL(15, 2), AC_HUFF_VAL(14, 2), AC_HUFF_VAL(13, 2), AC_HUFF_VAL(12, 2),
                AC_HUFF_VAL(11, 2), AC_HUFF_VAL(31, 1), AC_HUFF_VAL(30, 1), AC_HUFF_VAL(29, 1), AC_HUFF_VAL(28, 1),
                AC_HUFF_VAL(27, 1),

	            // Escape code
	            ESCAPE_CODE,
	            // End of block code
	            END_OF_BLOCK
            };

            static readonly uint[] s_huffmanDCChromaCodes = {
                254, 126, 62, 30, 14, 6, 2, 1, 0
            };

            static readonly byte[] s_huffmanDCChromaLengths = {
                8, 7, 6, 5, 4, 3, 2, 2, 2
            };

            static readonly uint[] s_huffmanDCLumaCodes = {
                126, 62, 30, 14, 6, 5, 1, 0, 4
            };

            static readonly byte[] s_huffmanDCLumaLengths = {
                7, 6, 5, 4, 3, 3, 2, 2, 3
            };

            static readonly uint[] s_huffmanDCSymbols = {
                DC_HUFF_VAL(8, 255, 128), DC_HUFF_VAL(7, 127, 64), DC_HUFF_VAL(6, 63, 32),
                DC_HUFF_VAL(5, 31, 16), DC_HUFF_VAL(4, 15, 8), DC_HUFF_VAL(3, 7, 4),
                DC_HUFF_VAL(2, 3, 2), DC_HUFF_VAL(1, 1, 1), DC_HUFF_VAL(0, 0, 0)
            };
        }
    }
}
