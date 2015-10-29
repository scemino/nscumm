using NScumm.Core;
using System;

namespace NScumm.Sky
{
    class RncDecoder
    {
        const uint RNC_SIGNATURE = 0x524E4301; // "RNC\001"

        //other defines
        const int TABLE_SIZE = (16 * 8);
        const int MIN_LENGTH = 2;
        const int HEADER_LEN = 18;

        //return codes
        const int NOT_PACKED = 0;
        const int PACKED_CRC = -1;
        const int UNPACKED_CRC = -2;

        ushort[] _rawTable = new ushort[64];
        ushort[] _posTable = new ushort[64];
        ushort[] _lenTable = new ushort[64];
        ushort[] _crcTable = new ushort[256];

        ushort _bitBuffl;
        ushort _bitBuffh;
        byte _bitCount;
        int _srcPtr;
        byte[] _src;
        int _dstPtr;
        byte[] _dst;

        public RncDecoder()
        {
            InitCrc();
        }

        public int UnpackM1(byte[] input, int offset, byte[] output, int outputOffset, ushort key)
        {
            int inputptr = offset;
            int unpackLen = 0;
            int packLen = 0;
            ushort counts = 0;
            ushort crcUnpacked = 0;
            ushort crcPacked = 0;

            _bitBuffl = 0;
            _bitBuffh = 0;
            _bitCount = 0;

            //Check for "RNC "
            if (input.ToUInt32BigEndian(inputptr) != RNC_SIGNATURE)
                return NOT_PACKED;

            inputptr += 4;

            // read unpacked/packed file length
            unpackLen = input.ToInt32BigEndian(inputptr); inputptr += 4;
            packLen = input.ToInt32BigEndian(inputptr); inputptr += 4;

            var blocks = input[inputptr + 5];

            //read CRC's
            crcUnpacked = input.ToUInt16BigEndian(inputptr); inputptr += 2;
            crcPacked = input.ToUInt16BigEndian(inputptr); inputptr += 2;
            inputptr = (inputptr + HEADER_LEN - 16);

            if (CrcBlock(input, inputptr, packLen) != crcPacked)
                return PACKED_CRC;

            inputptr = offset + HEADER_LEN;
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

                counts = InputBits(16);

                do
                {
                    int inputLength = InputValue(_rawTable);
                    int inputOffset;

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
                        inputOffset = InputValue(_posTable) + 1;
                        inputLength = InputValue(_lenTable) + MIN_LENGTH;

                        // Don't use Array.Copy here! because input and output overlap.
                        var tmpPtr = (_dstPtr - inputOffset);
                        while ((inputLength--) != 0)
                            _dst[_dstPtr++] = _dst[tmpPtr++];
                    }
                } while ((--counts) != 0);
            } while ((--blocks) != 0);

            if (CrcBlock(output, outputOffset, unpackLen) != crcUnpacked)
                return UNPACKED_CRC;

            // all is done..return the amount of unpacked bytes
            return unpackLen;
        }

        void InitCrc()
        {
            ushort tmp1 = 0;
            ushort tmp2 = 0;

            for (tmp2 = 0; tmp2 < 0x100; tmp2++)
            {
                tmp1 = tmp2;
                for (var cnt = 8; cnt > 0; cnt--)
                {
                    if ((tmp1 % 2) != 0)
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
        ushort CrcBlock(byte[] block, int offset, int size)
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

        ushort InputBits(byte amount)
        {
            ushort newBitBuffh = _bitBuffh;
            ushort newBitBuffl = _bitBuffl;
            short newBitCount = _bitCount;
            ushort remBits;

            ushort returnVal = (ushort)(((1 << amount) - 1) & newBitBuffl);
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

        void MakeHufftable(ushort[] table)
        {
            int offset = 0;
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

                        ushort b = (ushort)(huffCode >> (16 - bitLength));
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

        ushort InputValue(ushort[] table)
        {
            int offset = 0;
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
