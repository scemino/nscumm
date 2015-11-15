//
//  ScummEngine6_Array.cs
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

using System;
using System.Diagnostics;
using System.IO;
using NScumm.Core;

namespace NScumm.Scumm
{
    class ArrayHeader
    {
        public ArrayType Type
        {
            get
            {
                stream.Seek(0, SeekOrigin.Begin);
                return (ArrayType)reader.ReadInt16();
            }
            set
            {
                stream.Seek(0, SeekOrigin.Begin);
                writer.WriteInt16((int)value);
            }
        }

        public int Dim1
        {
            get
            {
                stream.Seek(2, SeekOrigin.Begin);
                return reader.ReadInt16();
            }
            set
            {
                stream.Seek(2, SeekOrigin.Begin);
                writer.WriteInt16(value);
            }
        }

        public int Dim2
        {
            get
            {
                stream.Seek(4, SeekOrigin.Begin);
                return reader.ReadInt16();
            }
            set
            {
                stream.Seek(4, SeekOrigin.Begin);
                writer.WriteInt16(value);
            }
        }

        readonly MemoryStream stream;
        readonly BinaryReader reader;
        readonly BinaryWriter writer;

        public ArrayHeader(byte[] data)
        {
            stream = new MemoryStream(data);
            reader = new BinaryReader(stream);
            writer = new BinaryWriter(stream);
        }

        public void Write(int index, byte value)
        {
            stream.Seek(6 + index, SeekOrigin.Begin);
            writer.WriteByte(value);
        }

        public void Write(int index, ushort value)
        {
            stream.Seek(6 + index * 2, SeekOrigin.Begin);
            writer.WriteUInt16(value);
        }

        public void Write(int index, uint value)
        {
            stream.Seek(6 + index * 4, SeekOrigin.Begin);
            writer.WriteUInt32(value);
        }

        public void Write(int index, byte[] values)
        {
            stream.Seek(6 + index, SeekOrigin.Begin);
            writer.WriteBytes(values, values.Length);
        }

        public byte ReadByte(int index)
        {
            stream.Seek(6 + index, SeekOrigin.Begin);
            return reader.ReadByte();
        }

        public ushort ReadUInt16(int index)
        {
            stream.Seek(6 + index * 2, SeekOrigin.Begin);
            return reader.ReadUInt16();
        }

        public uint ReadUInt32(int index)
        {
            stream.Seek(6 + index * 4, SeekOrigin.Begin);
            return reader.ReadUInt32();
        }
    }

    enum ArrayType
    {
        BitArray = 1,
        NibbleArray = 2,
        ByteArray = 3,
        StringArray = 4,
        IntArray = 5,
        DwordArray = 6
    }


    partial class ScummEngine6
    {
        int FindFreeArrayId()
        {
            for (var i = 1; i < _strings.Length; i++)
            {
                if (_strings[i] == null)
                    return i;
            }
            return -1;
        }

        ArrayHeader GetArray(uint array)
        {
            var data = _strings[ReadVariable(array)];
            return data != null ? new ArrayHeader(data) : null;
        }

        public void WriteArray(uint array, int idx, int @base, int value)
        {
//            Debug.WriteLine("WriteArray: {0} {1} {2} {3}", array, idx, @base, value);
            var ah = GetArray(array);
            if (ah == null)
                return;

            var offset = @base + idx * ah.Dim1;

            if (offset < 0 || offset >= ah.Dim1 * ah.Dim2)
            {
                throw new InvalidOperationException(string.Format("writeArray: array {0} out of bounds: [{1},{2}] exceeds [{3},{4}]",
                        array, @base, idx, ah.Dim1, ah.Dim2));
            }

            if (ah.Type != ArrayType.IntArray)
            {
                ah.Write(offset, (byte)value);
            }
            else if (Game.Version == 8)
            {
                ah.Write(offset, (uint)value);
            }
            else
            {
                ah.Write(offset, (ushort)value);
            }
        }

        protected ArrayHeader DefineArray(uint array, ArrayType type, int dim2, int dim1)
        {
//            Debug.WriteLine("DefineArray: {0} {1} {2} {3}", array, type, dim2, dim1);
            Debug.Assert(0 <= type && (int)type <= 5);

            NukeArray(array);

            var id = FindFreeArrayId();

            int size;
            if (Game.Version == 8)
            {
                if ((array & 0x80000000) != 0)
                {
                    throw new InvalidOperationException("Can't define bit variable as array pointer");
                }
                size = (type == ArrayType.IntArray) ? 4 : 1;
            }
            else
            {
                if ((array & 0x8000) != 0)
                {
                    throw new InvalidOperationException("Can't define bit variable as array pointer");
                }

                size = (type == ArrayType.IntArray) ? 2 : 1;
            }

            WriteVariable(array, id);

            size *= dim2 + 1;
            size *= dim1 + 1;

            _strings[id] = new byte[size + 6 /*sizeof(ArrayHeader)*/];
            var ah = new ArrayHeader(_strings[id]);
            ah.Type = type;
            ah.Dim1 = dim1 + 1;
            ah.Dim2 = dim2 + 1;
            return ah;
        }

        protected void NukeArray(uint a)
        {
            var data = ReadVariable(a);

            if (data != 0)
                _strings[data] = null;

            WriteVariable(a, 0);
        }

        public int ReadArray(uint array, int idx, int @base)
        {
            var ah = GetArray(array);

            if (ah == null)
                throw new InvalidOperationException(string.Format("ReadArray: invalid array {0} ({1})", array, ReadVariable(array)));

            // WORKAROUND bug #645711. This is clearly a script bug, as this script
            // excerpt shows nicely:
            // ...
            // [03A7] (5D)         if (isAnyOf(array-447[localvar13][localvar14],[0,4])) {
            // [03BD] (5D)           if ((localvar13 != -1) && (localvar14 != -1)) {
            // [03CF] (B6)             printDebug.begin()
            // ...
            // So it checks for invalid array indices only *after* using them to access
            // the array. Ouch.
            if (Game.GameId == Scumm.IO.GameId.FullThrottle && array == 447 && CurrentRoom == 95 && Slots[CurrentScript].Number == 2010 && idx == -1 && @base == -1)
            {
                return 0;
            }

            int offset = @base + idx * ah.Dim1;

            if (offset < 0 || offset >= (ah.Dim1 * ah.Dim2))
            {
                throw new InvalidOperationException(string.Format("readArray: array {0} out of bounds: [{1},{2}] exceeds [{3},{4}]",
                        array, @base, idx, ah.Dim1, ah.Dim2));
            }

            int val;
            if (ah.Type != ArrayType.IntArray)
            {
                val = (sbyte)ah.ReadByte(offset);
            }
            else if (Game.Version == 8)
            {
                val = (int)ah.ReadUInt32(offset);
            }
            else
            {
                val = (short)ah.ReadUInt16(offset);
            }
            return val;
        }
    }
}

