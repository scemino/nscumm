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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NScumm.Core
{
    public enum BitStreamType
    {
        EightBits,
        SixteenBits,
        ThirtyTwoBits
    }

    public class BitStream
    {
        private BinaryReader _br;
        private bool _isLittleEdian;
        private BitStreamType _type;
        private bool _isMsb2Lsb;

        /// <summary>
        /// Current value.
        /// </summary>
        private uint _value;
        /// <summary>
        /// Position within the current value.
        /// </summary>
        private byte _inValue;

        private readonly int _valueBits;

        public uint Size
        {
            get { return (uint)((_br.BaseStream.Length & ~((_valueBits >> 3) - 1)) * 8); }
        }

        public uint Position
        {
            get
            {
                if (_br.BaseStream.Position == 0)
                    return 0;

                uint p = (uint)(_inValue == 0
                    ? _br.BaseStream.Position
                    : (_br.BaseStream.Position - 1) & ~((_valueBits >> 3) - 1));
                return p * 8 + _inValue;
            }
        }

        public bool IsEndOfStream
        {
            get
            {
                return _br.BaseStream.Position >= _br.BaseStream.Length || (Position >= Size);
            }
        }

        public BitStream(Stream stream, BitStreamType type, bool isLittleEdian, bool isMsb2Lsb)
        {
            _br = new BinaryReader(stream);
            _type = type;
            _isLittleEdian = isLittleEdian;
            _isMsb2Lsb = isMsb2Lsb;
            switch (type)
            {
                case BitStreamType.EightBits:
                    _valueBits = 8;
                    break;
                case BitStreamType.SixteenBits:
                    _valueBits = 16;
                    break;
                case BitStreamType.ThirtyTwoBits:
                    _valueBits = 32;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        public uint ReadData()
        {
            if (_isLittleEdian)
            {
                switch (_type)
                {
                    case BitStreamType.EightBits:
                        return _br.ReadByte();
                    case BitStreamType.SixteenBits:
                        return _br.ReadUInt16();
                    case BitStreamType.ThirtyTwoBits:
                        return _br.ReadUInt32();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            switch (_type)
            {
                case BitStreamType.EightBits:
                    return _br.ReadByte();
                case BitStreamType.SixteenBits:
                    return _br.ReadUInt16BigEndian();
                case BitStreamType.ThirtyTwoBits:
                    return _br.ReadUInt32BigEndian();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static BitStream Create8Lsb(Stream stream)
        {
            return new BitStream(stream, BitStreamType.EightBits, false, false);
        }

        /// <summary>
        /// Add a bit to the value x, making it an n+1-bit value.
        /// </summary>
        /// <remarks>
        /// The current value is shifted and the bit is added to the
        /// appropriate place, dependant on the stream's bitorder.
        /// </remarks>
        /// <example>
        /// A bit y is added to the value 00001100 with size 4.
        /// If the stream's bitorder is MSB2LSB, the resulting value is 0001100y.
        /// If the stream's bitorder is LSB2MSB, the resulting value is 000y1100.
        /// </example>
        /// <param name="x"></param>
        /// <param name="n"></param>
        public void AddBit(ref uint x, int n)
        {
            if (n >= 32)
                throw new InvalidOperationException("BitStreamImpl.AddBit(): Too many bits requested to be read");

            if (_isMsb2Lsb)
                x = (x << 1) | GetBit();
            else
                x = (uint)((x & ~(1 << n)) | (GetBit() << n));
        }

        public uint GetBit()
        {
            // Check if we need the next value
            if (_inValue == 0)
                ReadValue();

            // Get the current bit
            int b = 0;
            if (_isMsb2Lsb)
                b = ((_value & 0x80000000) == 0) ? 0 : 1;
            else
                b = ((_value & 1) == 0) ? 0 : 1;

            // Shift to the next bit
            if (_isMsb2Lsb)
                _value <<= 1;
            else
                _value >>= 1;

            // Increase the position within the current value
            _inValue = (byte)((_inValue + 1) % _valueBits);

            return (uint)b;
        }

        public uint GetBits(int n)
        {
            if (n == 0)
                return 0;

            if (n > 32)
                throw new InvalidOperationException("BitStream::GetBits(): Too many bits requested to be read");

            // Read the number of bits
            uint v = 0;

            if (_isMsb2Lsb)
            {
                while (n-- > 0)
                    v = (v << 1) | GetBit();
            }
            else
            {
                for (uint i = 0; i < n; i++)
                    v = (v >> 1) | (GetBit() << 31);

                v >>= 32 - n;
            }

            return v;
        }

        public uint PeekBits(uint n)
        {
            uint value = _value;
            byte inValue = _inValue;
            uint curPos = (uint)_br.BaseStream.Position;

            uint v = GetBits((int)n);

            _br.BaseStream.Seek(curPos, SeekOrigin.Begin);
            _inValue = inValue;
            _value = value;

            return v;
        }

        public void Skip(uint n)
        {
            while (n-- > 0)
                GetBit();
        }


        private void ReadValue()
        {
            if (Size - Position < _valueBits)
                throw new InvalidOperationException("BitStreamImpl::readValue(): End of bit stream reached");

            _value = ReadData();

            // If we're reading the bits MSB first, we need to shift the value to that position
            if (_isMsb2Lsb)
                _value <<= 32 - _valueBits;
        }
    }
}
