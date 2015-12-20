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

using NScumm.Core;
using System;

namespace NScumm.Sky
{
    internal class RncDecoder
    {
        private const uint RncSignature = 0x524E4301; // "RNC\001"

        //other defines
        private const int MinLength = 2;
        private const int HeaderLen = 18;

        //return codes
        private const int NotPacked = 0;
        private const int PackedCrc = -1;
        private const int UnpackedCrc = -2;

        private readonly ushort[] _rawTable = new ushort[64];
        private readonly ushort[] _posTable = new ushort[64];
        private readonly ushort[] _lenTable = new ushort[64];
        private readonly ushort[] _crcTable = new ushort[256];

        private ushort _bitBuffl;
        private ushort _bitBuffh;
        private byte _bitCount;
        private int _srcPtr;
        private byte[] _src;
        private int _dstPtr;
        private byte[] _dst;

        public RncDecoder()
        {
            InitCrc();
        }

        public int UnpackM1(byte[] input, int offset, byte[] output, int outputOffset)
        {
            var inputptr = offset;
            int unpackLen;
            int packLen;
            ushort crcUnpacked;
            ushort crcPacked;

            _bitBuffl = 0;
            _bitBuffh = 0;
            _bitCount = 0;

            //Check for "RNC "
            if (input.ToUInt32BigEndian(inputptr) != RncSignature)
                return NotPacked;

            inputptr += 4;

            // read unpacked/packed file length
            unpackLen = input.ToInt32BigEndian(inputptr); inputptr += 4;
            packLen = input.ToInt32BigEndian(inputptr); inputptr += 4;

            var blocks = input[inputptr + 5];

            //read CRC's
            crcUnpacked = input.ToUInt16BigEndian(inputptr); inputptr += 2;
            crcPacked = input.ToUInt16BigEndian(inputptr); inputptr += 2;
            inputptr = inputptr + HeaderLen - 16;

            if (CrcBlock(input, inputptr, packLen) != crcPacked)
                return PackedCrc;

            inputptr = offset + HeaderLen;
            _src = input;
            _srcPtr = inputptr;
            _dst = output;

            //var inputHigh = packLen + HEADER_LEN;
            //var outputLow = 0;
            //var outputHigh = input[inputptr + 16] + unpackLen + outputLow;

            //if (!((inputHigh <= outputLow) || (outputHigh <= inputHigh)))
            //{
            //    _srcPtr = inputHigh;
            //    _dstPtr = outputHigh;
            //    Array.Copy(_src, _srcPtr - packLen, _dst, _dstPtr - packLen, packLen);
            //    _srcPtr = (_dstPtr - packLen);
            //}

            _dstPtr = outputOffset;
            _dst = output;
            _bitCount = 0;

            _bitBuffl = _src.ToUInt16(_srcPtr);
            InputBits(2);

            do
            {
                MakeHufftable(_rawTable);
                MakeHufftable(_posTable);
                MakeHufftable(_lenTable);

                var counts = InputBits(16);

                do
                {
                    int inputLength = InputValue(_rawTable);

                    if (inputLength != 0)
                    {
                        Array.Copy(_src, _srcPtr, _dst, _dstPtr, inputLength); //Array.Copy is allowed here
                        _dstPtr += inputLength;
                        _srcPtr += inputLength;
                        var a = _src.ToUInt16(_srcPtr);
                        var b = _src.ToUInt16(_srcPtr + 2);

                        _bitBuffl &= (ushort)((1 << _bitCount) - 1);
                        _bitBuffl |= (ushort)(a << _bitCount);
                        _bitBuffh = (ushort)((a >> (16 - _bitCount)) | (b << _bitCount));
                    }

                    if (counts > 1)
                    {
                        var inputOffset = InputValue(_posTable) + 1;
                        inputLength = InputValue(_lenTable) + MinLength;

                        // Don't use Array.Copy here! because input and output overlap.
                        var tmpPtr = _dstPtr - inputOffset;
                        while (inputLength-- != 0)
                            _dst[_dstPtr++] = _dst[tmpPtr++];
                    }
                } while (--counts != 0);
            } while (--blocks != 0);

            if (CrcBlock(output, outputOffset, unpackLen) != crcUnpacked)
                return UnpackedCrc;

            // all is done..return the amount of unpacked bytes
            return unpackLen;
        }

        private void InitCrc()
        {
            ushort tmp2;

            for (tmp2 = 0; tmp2 < 0x100; tmp2++)
            {
                var tmp1 = tmp2;
                for (var cnt = 8; cnt > 0; cnt--)
                {
                    if (tmp1 % 2 != 0)
                    {
                        tmp1 >>= 1;
                        tmp1 ^= 0x0a001;
                    }
                    else
                        tmp1 >>= 1;
                }
                _crcTable[tmp2] = tmp1;
            }
        }

        /// <summary>
        /// Calculates 16 bit crc of a block of memory.
        /// </summary>
        private ushort CrcBlock(byte[] block, int offset, int size)
        {
            ushort crc = 0;

            for (var i = 0; i < size; i++)
            {
                var tmp = block[offset++];
                crc ^= tmp;
                tmp = (byte)((crc >> 8) & 0x00FF);
                crc &= 0x00FF;
                crc = _crcTable[crc];
                crc ^= tmp;
            }

            return crc;
        }

        private ushort InputBits(byte amount)
        {
            var newBitBuffh = _bitBuffh;
            var newBitBuffl = _bitBuffl;
            short newBitCount = _bitCount;
            ushort remBits;

            var returnVal = (ushort)(((1 << amount) - 1) & newBitBuffl);
            newBitCount -= amount;

            if (newBitCount < 0)
            {
                newBitCount += amount;
                remBits = (ushort)(newBitBuffh << (16 - newBitCount));
                newBitBuffh >>= newBitCount;
                newBitBuffl >>= newBitCount;
                newBitBuffl |= remBits;
                _srcPtr += 2;
                newBitBuffh = _src.ToUInt16(_srcPtr);
                amount -= (byte)newBitCount;
                newBitCount = (short)(16 - amount);
            }
            remBits = (ushort)(newBitBuffh << (16 - amount));
            _bitBuffh = (ushort)(newBitBuffh >> amount);
            _bitBuffl = (ushort)((newBitBuffl >> amount) | remBits);
            _bitCount = (byte)newBitCount;

            return returnVal;
        }

        private void MakeHufftable(ushort[] table)
        {
            var offset = 0;
            var numCodes = InputBits(5);

            if (numCodes == 0)
                return;

            var huffLength = new byte[16];
            for (var i = 0; i < numCodes; i++)
                huffLength[i] = (byte)(InputBits(4) & 0x00FF);

            ushort huffCode = 0;

            for (var bitLength = 1; bitLength < 17; bitLength++)
            {
                for (var i = 0; i < numCodes; i++)
                {
                    if (huffLength[i] == bitLength)
                    {
                        table[offset++] = (ushort)((1 << bitLength) - 1);

                        var b = (ushort)(huffCode >> (16 - bitLength));
                        ushort a = 0;

                        for (var j = 0; j < bitLength; j++)
                            a |= (ushort)(((b >> j) & 1) << (bitLength - j - 1));
                        table[offset++] = a;

                        table[offset + 0x1e] = (ushort)((huffLength[i] << 8) | (i & 0x00FF));
                        huffCode += (ushort)(1 << (16 - bitLength));
                    }
                }
            }
        }

        private ushort InputValue(ushort[] table)
        {
            var offset = 0;
            ushort valOne, valTwo, value = _bitBuffl;

            do
            {
                valTwo = (ushort)(table[offset++] & value);
                valOne = table[offset++];

            } while (valOne != valTwo);

            value = table[offset + 0x1e];
            InputBits((byte)((value >> 8) & 0x00FF));
            value &= 0x00FF;

            if (value >= 2)
            {
                value--;
                valOne = InputBits((byte)(value & 0x00FF));
                valOne |= (ushort)(1 << value);
                value = valOne;
            }

            return value;
        }
    }
}
