//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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
using System.Linq.Expressions;
using System.Reflection;
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Another
{
    internal enum Mode
    {
        SmSave,
        SmLoad
    }

    internal abstract class Entry
    {
        public int MinVer { get; }
        public int MaxVer { get; }

        protected Entry(int minVer = Serializer.CurVer, int maxVer = Serializer.CurVer)
        {
            MinVer = minVer;
            MaxVer = maxVer;
        }

        public abstract int Save(Serializer serializer);

        public abstract void Load(Serializer serializer);

        public static Entry Create(BytePtr array, int length, int minVer = Serializer.CurVer)
        {
            return new ArrayEntry1(array, length, minVer);
        }

        public static Entry Create(byte[][] array, int l1, int l2, int minVer = Serializer.CurVer)
        {
            return new ArrayEntry1U2(array, l1, l2, minVer);
        }

        public static Entry Create(short[] array, int length, int minVer = Serializer.CurVer)
        {
            return new ArrayEntry2S(array, length, minVer);
        }

        public static Entry Create(ushort[] array, int length, int minVer = Serializer.CurVer)
        {
            return new ArrayEntry2U(array, length, minVer);
        }

        public static Entry Create(ushort[][] array, int l1, int l2, int minVer = Serializer.CurVer)
        {
            return new ArrayEntry2U2(array, l1, l2, minVer);
        }

        public static Entry Create<T>(T obj, Expression<Func<T, bool>> expression, int minVer = Serializer.CurVer)
        {
            return new BoolEntry<T>(obj, expression, minVer);
        }

        public static Entry Create<T>(T obj, Expression<Func<T, byte>> expression, int minVer = Serializer.CurVer)
        {
            return new ByteEntry<T>(obj, expression, minVer);
        }

        public static Entry Create<T>(T obj, Expression<Func<T, ushort>> expression, int minVer = Serializer.CurVer)
        {
            return new UShortEntry<T>(obj, expression, minVer);
        }

        public static Entry Create<T>(T obj, Expression<Func<T, int>> expression, int minVer = Serializer.CurVer)
        {
            return new IntEntry<T>(obj, expression, minVer);
        }

        public static Entry Create<T>(T obj, Expression<Func<T, BytePtr>> expression, int minVer = Serializer.CurVer)
        {
            return new BytePtrEntry<T>(obj, expression, minVer);
        }

        protected static Func<TValue> GetGetLambda<T, TValue>(T obj, Expression<Func<T, TValue>> expression)
        {
            var exp = (MemberExpression) expression.Body;
            var field = (FieldInfo) exp.Member;
            var paramObj = Expression.Constant(obj);
            var lambdaExp = Expression.Lambda<Func<TValue>>(
                    Expression.Field(paramObj, field))
                .Compile();

            return lambdaExp;
        }

        protected static Action<TValue> GetSetLambda<T, TValue>(T obj, Expression<Func<T, TValue>> expression)
        {
            var exp = (MemberExpression) expression.Body;
            var field = (FieldInfo) exp.Member;
            var paramObj = Expression.Constant(obj);
            var paramValue = Expression.Parameter(typeof(TValue));
            var lambdaExp = Expression.Lambda<Action<TValue>>(
                    Expression.Assign(
                        Expression.Field(paramObj, field),
                        paramValue),
                    paramValue)
                .Compile();

            return lambdaExp;
        }
    }

    internal class ByteEntry<T> : Entry
    {
        private readonly byte _value;
        private readonly Action<byte> _set;

        public ByteEntry(T obj, Expression<Func<T, byte>> expression, int minVer = Serializer.CurVer)
            : base(minVer)
        {
            _set = GetSetLambda(obj, expression);
            _value = GetGetLambda(obj, expression)();
        }

        public override int Save(Serializer serializer)
        {
            serializer.SaveByte(_value);
            return 1;
        }

        public override void Load(Serializer serializer)
        {
            _set(serializer.LoadByte());
        }
    }

    internal class BoolEntry<T> : Entry
    {
        private readonly bool _value;
        private readonly Action<bool> _set;

        public BoolEntry(T obj, Expression<Func<T, bool>> expression, int minVer = Serializer.CurVer)
            : base(minVer)
        {
            _set = GetSetLambda(obj, expression);
            _value = GetGetLambda(obj, expression)();
        }

        public override int Save(Serializer serializer)
        {
            serializer.SaveByte((byte) (_value ? 1 : 0));
            return 1;
        }

        public override void Load(Serializer serializer)
        {
            _set(serializer.LoadByte() != 0);
        }
    }

    internal class UShortEntry<T> : Entry
    {
        private readonly ushort _value;
        private readonly Action<ushort> _set;

        public UShortEntry(T obj, Expression<Func<T, ushort>> expression, int minVer)
            : base(minVer)
        {
            _set = GetSetLambda(obj, expression);
            _value = GetGetLambda(obj, expression)();
        }

        public override int Save(Serializer serializer)
        {
            serializer.WriteShort((short) _value);
            return 2;
        }

        public override void Load(Serializer serializer)
        {
            _set((ushort) serializer.LoadShort());
        }
    }

    internal class IntEntry<T> : Entry
    {
        private readonly int _value;
        private readonly Action<int> _set;

        public IntEntry(T obj, Expression<Func<T, int>> expression, int minVer = Serializer.CurVer)
            : base(minVer)
        {
            _set = GetSetLambda(obj, expression);
            _value = GetGetLambda(obj, expression)();
        }

        public override int Save(Serializer serializer)
        {
            serializer.SaveInt(_value);
            return 4;
        }

        public override void Load(Serializer serializer)
        {
            _set(serializer.LoadInt());
        }
    }

    internal class BytePtrEntry<T> : Entry
    {
        private readonly BytePtr _value;
        private readonly Action<BytePtr> _set;

        public BytePtrEntry(T obj, Expression<Func<T, BytePtr>> expression, int minVer = Serializer.CurVer)
            : base(minVer)
        {
            _set = GetSetLambda(obj, expression);
            _value = GetGetLambda(obj, expression)();
        }

        public override int Save(Serializer serializer)
        {
            serializer.SavePointer(_value);
            return 4;
        }

        public override void Load(Serializer serializer)
        {
            _set(serializer.LoadPointer());
        }
    }

    internal class ArrayEntry1U2 : Entry
    {
        private readonly byte[][] _array;
        private readonly int _length1;
        private readonly int _length2;

        public ArrayEntry1U2(byte[][] array, int length1, int length2, int minVer = Serializer.CurVer)
            : base(minVer)
        {
            _array = array;
            _length1 = length1;
            _length2 = length2;
        }

        public override int Save(Serializer serializer)
        {
            for (var i = 0; i < _length1; i++)
            {
                serializer.WriteBytes(_array[i].Take(_length2));
            }
            return _length1 * _length2;
        }

        public override void Load(Serializer serializer)
        {
            for (var i = 0; i < _length1; i++)
            {
                serializer.LoadBytes(_array[i], _length2);
            }
        }
    }

    internal class ArrayEntry1 : Entry
    {
        private readonly BytePtr _array;
        private readonly int _length;

        public ArrayEntry1(BytePtr array, int length, int minVer = Serializer.CurVer)
            : base(minVer)
        {
            _array = array;
            _length = length;
        }

        public override int Save(Serializer serializer)
        {
            serializer.WriteBytes(_array.Take(_length));
            return _length;
        }

        public override void Load(Serializer serializer)
        {
            serializer.LoadBytes(_array, _length);
        }
    }

    internal class ArrayEntry2S : Entry
    {
        private readonly Ptr<short> _array;
        private readonly int _length;

        public ArrayEntry2S(Ptr<short> array, int length, int minVer = Serializer.CurVer)
            : base(minVer)
        {
            _array = array;
            _length = length;
        }

        public override int Save(Serializer serializer)
        {
            serializer.WriteShorts(_array.Take(_length));
            return _length * 2;
        }

        public override void Load(Serializer serializer)
        {
            serializer.LoadShorts(_array, _length);
        }
    }

    internal class ArrayEntry2U : Entry
    {
        private readonly Ptr<ushort> _array;
        private readonly int _length;

        public ArrayEntry2U(Ptr<ushort> array, int length, int minVer = Serializer.CurVer)
            : base(minVer)
        {
            _array = array;
            _length = length;
        }

        public override int Save(Serializer serializer)
        {
            serializer.WriteShorts(_array.Select(o => (short) o).Take(_length));
            return _length * 2;
        }

        public override void Load(Serializer serializer)
        {
            serializer.LoadUShorts(_array, _length);
        }
    }

    internal class ArrayEntry2U2 : Entry
    {
        private readonly ushort[][] _array;
        private readonly int _length1;
        private readonly int _length2;

        public ArrayEntry2U2(ushort[][] array, int length1, int length2, int minVer = Serializer.CurVer)
            : base(minVer)
        {
            _array = array;
            _length1 = length1;
            _length2 = length2;
        }

        public override int Save(Serializer serializer)
        {
            for (var i = 0; i < _length1; i++)
            {
                serializer.WriteShorts(_array[i].Select(o => (short) o).Take(_length2));
            }
            return _length1 * _length2 * 2;
        }

        public override void Load(Serializer serializer)
        {
            for (var i = 0; i < _length1; i++)
            {
                serializer.LoadUShorts(_array[i], _length2);
            }
        }
    }

    internal class Serializer
    {
        public const int CurVer = 2;

        public readonly Mode Mode;

        private BytePtr _ptrBlock;
        private int _bytesCount;
        private readonly BinaryWriter _writer;
        private readonly BinaryReader _reader;
        private readonly ushort _saveVer;

        public Serializer(Stream stream, Mode mode, BytePtr ptrBlock, ushort saveVer = CurVer)
        {
            Mode = mode;
            _ptrBlock = ptrBlock;
            _saveVer = saveVer;
            if (mode == Mode.SmSave)
            {
                _writer = new BinaryWriter(stream);
            }
            else
            {
                _reader = new BinaryReader(stream);
            }
        }

        public void LoadBytes(BytePtr bytes, int length)
        {
            _reader.Read(bytes.Data, bytes.Offset, length);
        }

        public void WriteBytes(IEnumerable<byte> array)
        {
            foreach (var item in array)
            {
                _writer.Write(item);
            }
        }

        public void SaveByte(byte value)
        {
            _writer.WriteByte(value);
        }

        public byte LoadByte()
        {
            return _reader.ReadByte();
        }

        public void WriteShort(short value)
        {
            _writer.Write(ScummHelper.SwapBytes(value));
        }

        public short LoadShort()
        {
            return ScummHelper.SwapBytes(_reader.ReadInt16());
        }

        public void SaveInt(int value)
        {
            _writer.Write(ScummHelper.SwapBytes((uint) value));
        }

        public int LoadInt()
        {
            return (int) ScummHelper.SwapBytes((uint) _reader.ReadInt32());
        }

        public void LoadShorts(Ptr<short> data, int length)
        {
            for (var i = 0; i < length; i++)
            {
                data[i] = LoadShort();
            }
        }

        public void LoadUShorts(Ptr<ushort> data, int length)
        {
            for (var i = 0; i < length; i++)
            {
                data[i] = (ushort) LoadShort();
            }
        }

        public void WriteShorts(IEnumerable<short> array)
        {
            foreach (var item in array)
            {
                _writer.Write(ScummHelper.SwapBytes(item));
            }
        }

        public BytePtr LoadPointer()
        {
            return _ptrBlock + _reader.ReadInt32BigEndian();
        }

        public void SavePointer(BytePtr ptr)
        {
            var off = ptr.Offset - _ptrBlock.Offset;
            _writer.Write(ScummHelper.SwapBytes((uint) off));
        }

        public void SaveOrLoadEntries(Entry[] entries)
        {
            Debug(DebugLevels.DbgSer, "Serializer::saveOrLoadEntries() _mode={0}", Mode);
            _bytesCount = 0;
            switch (Mode)
            {
                case Mode.SmSave:
                    SaveEntries(entries);
                    break;
                case Mode.SmLoad:
                    LoadEntries(entries);
                    break;
            }
            Debug(DebugLevels.DbgSer, "Serializer::saveOrLoadEntries() _bytesCount={0}", _bytesCount);
        }

        private void LoadEntries(Entry[] entries)
        {
            Debug(DebugLevels.DbgSer, "Serializer::loadEntries()");
            foreach (var entry in entries)
            {
                if (_saveVer >= entry.MinVer && _saveVer <= entry.MaxVer)
                {
                    entry.Load(this);
                }
            }
        }

        private void SaveEntries(Entry[] entries)
        {
            Debug(DebugLevels.DbgSer, "Serializer::saveEntries()");
            foreach (var entry in entries)
            {
                if (entry.MaxVer == CurVer)
                {
                    _bytesCount += entry.Save(this);
                }
            }
        }
    }
}