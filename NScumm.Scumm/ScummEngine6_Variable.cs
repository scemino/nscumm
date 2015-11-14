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

namespace NScumm.Scumm
{
    partial class ScummEngine6
    {
        Stack<int> _vmStack = new Stack<int>(150);

        [OpCode(0x00)]
        protected void PushByte()
        {
            Push(ReadByte());
        }

        [OpCode(0x01)]
        protected void PushWord()
        {
            Push(ReadWordSigned());
//            Console.WriteLine("Push({0})", _vmStack.Peek());
        }

        [OpCode(0x02)]
        protected void PushByteVar()
        {
            Push(ReadVariable(ReadByte()));
        }

        [OpCode(0x03)]
        protected virtual void PushWordVar()
        {
            var v = ReadWord();
            Push(ReadVariable(v));
//            Console.WriteLine("Push({0}:{1})", v, _vmStack.Peek());
        }

        [OpCode(0x06)]
        protected void ByteArrayRead(int @base)
        {
            Push(ReadArray(ReadByte(), 0, @base));
        }

        [OpCode(0x07)]
        protected virtual void WordArrayRead(int @base)
        {
            Push(ReadArray(ReadWord(), 0, @base));
        }

        [OpCode(0x0a)]
        protected void ByteArrayIndexedRead(int index, int @base)
        {
            Push(ReadArray(ReadByte(), index, @base));
        }

        [OpCode(0x0b)]
        protected virtual void WordArrayIndexedRead(int index, int @base)
        {
            Push(ReadArray(ReadWord(), index, @base));
        }

        [OpCode(0x42)]
        protected void WriteByteVar(int value)
        {
            var index = ReadByte();
//            Debug.WriteLine("WriteByteVar {0} {1}", index, value);
            WriteVariable(index, value);
        }

        [OpCode(0x43)]
        protected virtual void WriteWordVar(int value)
        {
            var index = ReadWord();
//            Debug.WriteLine("WriteWordVar {0} {1}", index, value);
            WriteVariable(index, value);
        }

        [OpCode(0x46)]
        protected void ByteArrayWrite(int @base, int value)
        {
            WriteArray(ReadByte(), 0, @base, value);
        }

        [OpCode(0x47)]
        protected virtual void WordArrayWrite(int @base, int value)
        {
            WriteArray(ReadWord(), 0, @base, value);
        }

        [OpCode(0x4a)]
        protected void ByteArrayIndexedWrite(int index, int @base, int value)
        {
            WriteArray(ReadByte(), index, @base, value);
        }

        [OpCode(0x4b)]
        protected virtual void WordArrayIndexedWrite(int index, int @base, int value)
        {
            WriteArray(ReadWord(), index, @base, value);
        }

        [OpCode(0x4e)]
        protected void ByteVarInc()
        {
            var var = ReadByte();
            WriteVariable(var, ReadVariable(var) + 1);
        }

        [OpCode(0x4f)]
        protected virtual void WordVarInc()
        {
            var var = ReadWord();
            WriteVariable(var, ReadVariable(var) + 1);
        }

        [OpCode(0x52)]
        protected void ByteArrayInc(int @base)
        {
            var var = ReadByte();
            WriteArray(var, 0, @base, ReadArray(var, 0, @base) + 1);
        }

        [OpCode(0x53)]
        protected virtual void WordArrayInc(int @base)
        {
            var var = ReadWord();
            WriteArray(var, 0, @base, ReadArray(var, 0, @base) + 1);
        }

        [OpCode(0x56)]
        protected void ByteVarDec()
        {
            var var = ReadByte();
            WriteVariable(var, ReadVariable(var) - 1);
        }

        [OpCode(0x57)]
        protected virtual void WordVarDec()
        {
            var var = ReadWord();
            WriteVariable(var, ReadVariable(var) - 1);
        }

        [OpCode(0x5a)]
        protected void ByteArrayDec(int @base)
        {
            var var = ReadByte();
            WriteArray(var, 0, @base, ReadArray(var, 0, @base) - 1);
        }

        [OpCode(0x5b)]
        protected virtual void WordArrayDec(int @base)
        {
            var var = ReadWord();
            WriteArray(var, 0, @base, ReadArray(var, 0, @base) - 1);
        }

        [OpCode(0xA4)]
        protected virtual void ArrayOps()
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
                case 212:               // SO_ASSIGN_2DIM_LIST
                    {
                        var b = Pop();
                        var args = GetStackList(128);
                        var d = ReadVariable(array);
                        if (d == 0)
                            throw new InvalidOperationException("Must DIM a two dimensional array before assigning");
                        var c = Pop();
                        for (int i = 0; i < args.Length; i++)
                        {
                            WriteArray(array, c, b + i, args[i]);
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException(string.Format("ArrayOps: default case {0} (array {1})", subOp, array));
            }

        }

        [OpCode(0xbc)]
        protected virtual void DimArray()
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
                    NukeArray(array);
                    return;
                default:
                    throw new NotSupportedException(string.Format("DimArray: default case {0}", subOp));
            }

            var dim1 = Pop();
            DefineArray(array, type, 0, dim1);
        }

        [OpCode(0xc0)]
        protected void Dim2DimArray(int dim2, int dim1)
        {
            var subOp = ReadByte();
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
                default:
                    throw new NotSupportedException(string.Format("Dim2DimArray: default case {0}", subOp));
            }

            DefineArray(ReadWord(), type, dim2, dim1);
        }

        [OpCode(0xd4)]
        protected void Shuffle(int a, int b)
        {
            ShuffleArray(ReadWord(), a, b);
        }

        void ShuffleArray(uint num, int minIdx, int maxIdx)
        {
            int range = maxIdx - minIdx;
            int count = range * 2;

            // Shuffle the array 'num'
            while (count-- != 0)
            {
                var rnd = new Random();
                // Determine two random elements...
                var rand1 = rnd.Next(minIdx, maxIdx);
                var rand2 = rnd.Next(minIdx, maxIdx);

                // ...and swap them
                var val1 = ReadArray(num, 0, rand1);
                var val2 = ReadArray(num, 0, rand2);
                WriteArray(num, 0, rand1, val2);
                WriteArray(num, 0, rand2, val1);
            }
        }

        protected void Push(int value)
        {
            _vmStack.Push(value);
        }

        protected void Push(bool value)
        {
            _vmStack.Push(value ? 1 : 0);
        }

        protected int Pop()
        {
            return _vmStack.Pop();
        }

        protected int[] GetStackList(int max)
        {
            var num = Pop();

            if (num > max)
                throw new InvalidOperationException(string.Format("Too many items {0} in stack list, max {1}", num, max));

            var args = new int[num];
            var i = num;
            while (i-- != 0)
            {
                args[i] = Pop();
            }
            return args;
        }
    }
}

