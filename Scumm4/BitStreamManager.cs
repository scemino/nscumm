/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Scumm4
{
    public class BitStreamManager
    {
        private List<bool> _bitStream;
        private int _position;
        public int Position
        {
            get
            {
                return this._position;
            }
            set
            {
                if (value > this._bitStream.Count)
                {
                    throw new IndexOutOfRangeException("Position is after the end of stream");
                }
                this._position = value;
            }
        }
        public bool EndOfStream
        {
            get
            {
                return this._position == this._bitStream.Count;
            }
        }
        public int Lenght
        {
            get
            {
                return this._bitStream.Count;
            }
        }
        public BitStreamManager()
        {
            this._bitStream = new List<bool>();
        }
        public BitStreamManager(byte[] byteArray)
        {
            this._bitStream = new List<bool>(byteArray.Length * 8);
            for (int i = 0; i < byteArray.Length; i++)
            {
                byte newByte = byteArray[i];
                this.AddByte(newByte);
            }
        }
        private void CheckIsEnoughSpace(int numBits)
        {
            if (this._position + numBits > this._bitStream.Count)
            {
                throw new EndOfStreamException("Position is after the end of stream");
            }
        }
        public bool ReadBit()
        {
            if (this.EndOfStream)
            {
                throw new EndOfStreamException("Stream finished");
            }
            return this._bitStream[this._position++];
        }
        public byte ReadByte()
        {
            return this.ReadValue(8);
        }
        public byte ReadValue(int numBits)
        {
            this.CheckIsEnoughSpace(numBits);
            bool[] array = new bool[numBits];
            for (int i = 0; i < numBits; i++)
            {
                array[i] = this._bitStream[this.Position + i];
            }
            this.Position += numBits;
            return this.BitArrayToByte(array);
        }
        public void AddBit(bool bit)
        {
            this._bitStream.Add(bit);
        }
        public void AddByte(byte newByte)
        {
            this._bitStream.Add((newByte & 1) != 0);
            this._bitStream.Add((newByte & 2) != 0);
            this._bitStream.Add((newByte & 4) != 0);
            this._bitStream.Add((newByte & 8) != 0);
            this._bitStream.Add((newByte & 16) != 0);
            this._bitStream.Add((newByte & 32) != 0);
            this._bitStream.Add((newByte & 64) != 0);
            this._bitStream.Add((newByte & 128) != 0);
        }
        public void AddByte(byte newByte, int numBits)
        {
            for (int i = 0; i < numBits; i++)
            {
                this._bitStream.Add(((int)newByte & (int)Math.Pow(2.0, (double)i)) != 0);
            }
        }
        public byte[] ToByteArray()
        {
            return this.BitArrayToByteArray(this._bitStream.ToArray());
        }
        private byte[] BitArrayToByteArray(bool[] bitArray)
        {
            int num = bitArray.Length / 8;
            if (bitArray.Length % 8 != 0)
            {
                num++;
            }
            byte[] array = new byte[num];
            int num2 = 0;
            int num3 = 0;
            for (int i = 0; i < bitArray.Length; i++)
            {
                if (bitArray[i])
                {
                    byte[] expr_3F_cp_0 = array;
                    int expr_3F_cp_1 = num2;
                    expr_3F_cp_0[expr_3F_cp_1] |= (byte)(1 << num3);
                }
                num3++;
                if (num3 == 8)
                {
                    num3 = 0;
                    num2++;
                }
            }
            return array;
        }
        private byte BitArrayToByte(bool[] bitArray)
        {
            if (bitArray.Length > 8)
            {
                throw new ConversionException("Maximum allowed is 8 bits");
            }
            byte b = 0;
            for (int i = 0; i < bitArray.Length; i++)
            {
                if (bitArray[i])
                {
                    b |= (byte)(1 << i);
                }
            }
            return b;
        }

        public ushort ReadUInt16()
        {
            return (ushort)(this.ReadByte() | (((ushort)this.ReadByte()) << 8));
        }
    }
}
