//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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

#if ENABLE_SCI32

using System;
using System.Collections.Generic;
using System.Text;
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    internal class SciArray<T> {
        public SciArray()
        {
            _type = -1;
        }

        public SciArray(SciArray<T> array) {
            _type = array._type;
            _size = array._size;
            _actualSize = array._actualSize;
            _data = new T[_actualSize];
            Array.Copy(array._data, _data, _size);
        }

        public SciArray<T> Assign(SciArray<T> array)
        {
            if (this == array)
                return this;

            _type = array._type;
            _size = array._size;
            _actualSize = array._actualSize;
            _data = new T[_actualSize];
            Array.Copy(array._data, _data, _size);

            return this;
        }

        public void SetType(sbyte type) {
            if (_type >= 0)
                Error("SciArray::setType(): Type already set");

            _type = type;
        }

        public void SetSize(int size) {
            if (_type < 0)
                Error("SciArray::setSize(): No type set");

            // Check if we don't have to do anything
            if (_size == size)
                return;

            // Check if we don't have to expand the array
            if (size <= _actualSize) {
                _size = size;
                return;
            }

            // So, we're going to have to create an array of some sort
            var newArray = new T[size];
            Array.Clear(newArray, 0, size);

            // Check if we never created an array before
            if (_data==null) {
                _size = _actualSize = size;
                _data = newArray;
                return;
            }

            // Copy data from the old array to the new
            Array.Copy(_data, newArray,_size);

            // Now set the new array to the old and set the sizes
            _data = newArray;
            _size = _actualSize = size;
        }

        public T GetValue(ushort index) {
            if (index >= _size)
                Error("SciArray::getValue(): {0} is out of bounds ({1})", index, _size);

            return _data[index];
        }

        public void SetValue(ushort index, T value) {
            if (index >= _size)
                Error("SciArray::setValue(): {0} is out of bounds ({1})", index, _size);

            _data[index] = value;
        }

        public sbyte Type =>_type;
        public int Size=>_size;
        public T[] RawData => _data;

        protected sbyte _type;
        protected  T[] _data;
        protected int _size; // _size holds the number of entries that the scripts have requested
        protected int _actualSize; // _actualSize is the actual numbers of entries allocated

        public virtual void Destroy()
        {
            _data = null;
            _type = -1;
            _size = _actualSize = 0;
        }
    }

    internal class SciString : SciArray<byte>
    {
        public SciString()
        {
            SetType(3);
        }

        // We overload destroy to ensure the string type is 3 after destroying
        public override void Destroy()
        {
            base.Destroy();
            _type = 3;
        }

        public override string ToString()
        {
            if (_type != 3)
                Error("SciString::toString(): Array is not a string");

            var @string=new StringBuilder();
            for (var i = 0; i < _size && _data[i] != 0; i++)
                @string.Append((char)_data[i]);

            return @string.ToString();
        }

        public void FromString(string @string)
        {
            if (_type != 3)
                Error("SciString::fromString(): Array is not a string");

            SetSize(@string.Length + 1);

            for (var i = 0; i < @string.Length; i++)
                _data[i] = (byte)@string[i];

            _data[@string.Length] = 0x0;
        }
    }

    internal sealed class ArrayTable : SegmentObjTable<SciArray<Register>>
    {
        public ArrayTable() : base(SegmentType.ARRAY)
        {
        }

        public override void FreeAtAddress(SegManager segMan, Register subAddr)
        {
            this[(int) subAddr.Offset].Destroy();
            FreeEntry((int) subAddr.Offset);
        }

        public override List<Register> ListAllOutgoingReferences(Register addr)
        {
            var tmp=new List<Register>();
            if (!IsValidEntry((int) addr.Offset))
            {
                Error("Invalid array referenced for outgoing references: {0}", addr);
            }

            var array = this[(int) addr.Offset];

            for (var i = 0; i < array.Size; i++)
            {
                var value = array.GetValue((ushort) i);
                if (value.Segment != 0)
                    tmp.Add(value);
            }

            return tmp;
        }

        public override SegmentRef Dereference(Register pointer)
        {
            var ret = new SegmentRef
            {
                isRaw = false,
                maxSize = this[(int) pointer.Offset].Size * 2,
                reg = new StackPtr(this[(int) pointer.Offset].RawData,0)
            };
            return ret;
        }
    }

    internal class StringTable: SegmentObjTable<SciString>
    {
        public StringTable() : base(SegmentType.STRING)
        {
        }

        public override void FreeAtAddress(SegManager segMan, Register subAddr)
        {
            this[(int) subAddr.Offset].Destroy();
            FreeEntry((int) subAddr.Offset);
        }

        public override SegmentRef Dereference(Register pointer)
        {
            var ret = new SegmentRef
            {
                isRaw = true,
                maxSize = this[(int) pointer.Offset].Size,
                raw = new BytePtr(this[(int) pointer.Offset].RawData)
            };
            return ret;
        }
    }
}

#endif
