//
//  ScummEngine6_Variables.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NScumm.Core
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
                return (int)reader.ReadInt16();
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
                return (int)reader.ReadInt16();
            }
            set
            {
                stream.Seek(4, SeekOrigin.Begin);
                writer.WriteInt16(value);
            }
        }

        public byte[] Data
        {
            get
            {
                stream.Seek(6, SeekOrigin.Begin);
                return reader.ReadBytes((int)(stream.Length - 6));
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
        List<Array> _arrays = new List<Array>();

        [OpCode(0x43)]
        void WriteWordVar(int value)
        {
            var index = ReadWord();
            WriteVariable(index, value);
        }

        [OpCode(0xA4)]
        void ArrayOps()
        {
            var subOp = ReadByte();
            var array = ReadWord();

            switch (subOp)
            {
                case 205:               // SO_ASSIGN_STRING
                    {
                        var b = Pop();
                        var text = ReadCharacters();
                        var data = DefineArray(array, ArrayType.StringArray, 0, text.Length + 1);
                        data.Write(b, text);
                    }
                    break;
                case 208:               // SO_ASSIGN_INT_LIST
                    {
                        var b = Pop();
                        var c = Pop();
                        var d = ReadVariable(array);
                        if (d == 0)
                        {
                            DefineArray(array, ArrayType.IntArray, 0, b + c);
                        }
                        while (c-- != 0)
                        {
                            WriteArray(array, 0, b + c, Pop());
                        }
                    }
                    break;
//                case 212:               // SO_ASSIGN_2DIM_LIST
//                    b = pop();
//                    len = getStackList(list, ARRAYSIZE(list));
//                    d = readVar(array);
//                    if (d == 0)
//                        error("Must DIM a two dimensional array before assigning");
//                    c = pop();
//                    while (--len >= 0)
//                    {
//                        writeArray(array, c, b + len, list[len]);
//                    }
//                    break;
                default:
                    throw new NotSupportedException(string.Format("ArrayOps: default case {0} (array {1})", subOp, array));
            }

        }

        [OpCode(0x47)]
        void WordArrayWrite(int @base, int value)
        {
            WriteArray(ReadWord(), 0, @base, value);
        }

        [OpCode(0xbc)]
        void DimArray(int dim1)
        {
            var subOp = ReadByte();
            var array = ReadWord();
            ArrayType type;
            switch (subOp)
            {
                case 199:               // SO_INT_ARRAY
                    type = ArrayType.IntArray;
                    break;
                case 200:               // SO_BIT_ARRAY
                    type = ArrayType.BitArray;
                    break;
                case 201:               // SO_NIBBLE_ARRAY
                    type = ArrayType.NibbleArray;
                    break;
                case 202:               // SO_BYTE_ARRAY
                    type = ArrayType.ByteArray;
                    break;
                case 203:               // SO_STRING_ARRAY
                    type = ArrayType.StringArray;
                    break;
                case 204:               // SO_UNDIM_ARRAY
                    // TODO: SCUMM6: nukeArray(array);
                    return;
                default:
                    throw new NotSupportedException(string.Format("DimArray: default case {0}", subOp));
            }

            DefineArray(array, type, 0, dim1);
        }

        int FindFreeArrayId()
        {
            for (var i = 1; i < _strings.Length; i++)
            {
                if (_strings[i] == null)
                    return i;
            }
            return -1;
        }

        ArrayHeader GetArray(int array)
        {
            var data = _strings[ReadVariable(array)];
            return data != null ? new ArrayHeader(data) : null;
        }

        void WriteArray(int array, int idx, int @base, int value)
        {
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

        ArrayHeader DefineArray(int array, ArrayType type, int dim2, int dim1)
        {
            int size;

            Debug.Assert(0 <= (int)type && (int)type <= 5);

            //nukeArray(array);

            var id = FindFreeArrayId();

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
    }
}

