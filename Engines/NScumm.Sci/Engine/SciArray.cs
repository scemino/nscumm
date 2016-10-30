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
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    internal enum SciArrayType
    {
        Int16 = 0,
        ID = 1,
        Byte = 2,
        String = 3,
        // Type 4 was for 32-bit integers; never used
        Invalid = 5
    }

    internal class SciArray
    {
        public SciArray()
        {
            _type = SciArrayType.Invalid;
        }

        public SciArray(SciArray array)
        {
            _type = array._type;
            _size = array._size;
            _elementSize = array._elementSize;
            _data = new byte[_size * _elementSize];
            Array.Copy(array._data, _data, _size);
        }

        public SciArray Assign(SciArray array)
        {
            if (this == array)
                return this;

            _type = array._type;
            _size = array._size;
            _elementSize = array._elementSize;
            _data = new byte[_elementSize * _size];
            Array.Copy(array._data, _data, _elementSize * _size);

            return this;
        }

        /// <summary>
        /// Copies values from the source array. Both arrays will be grown if needed
        /// to prevent out-of-bounds reads/writes.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sourceIndex"></param>
        /// <param name="targetIndex"></param>
        /// <param name="count"></param>
        public void Copy(SciArray source, ushort sourceIndex, ushort targetIndex, ushort count)
        {
            if (count == 65535 /* -1 */)
            {
                count = (ushort)(source.Size - sourceIndex);
            }

            if (count == 0)
            {
                return;
            }

            Resize((ushort)(targetIndex + count));
            source.Resize((ushort)(sourceIndex + count));

            System.Diagnostics.Debug.Assert(source._elementSize == _elementSize);

            BytePtr sourceData = new BytePtr(source._data, sourceIndex * source._elementSize);
            BytePtr targetData = new BytePtr(_data, targetIndex * _elementSize);
            Array.Copy(sourceData.Data, sourceData.Offset, targetData.Data, targetData.Offset, count * _elementSize);
        }

        /// <summary>
        /// Fills the array with the given value. Existing values will be
        /// overwritten. The array will be grown if needed to store all values.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <param name="value"></param>
        public void Fill(ushort index, ushort count, Register value)
        {
            if (count == 65535 /* -1 */)
            {
                count = (ushort)(Size - index);
            }

            if (count == 0)
            {
                return;
            }

            Resize((ushort)(index + count));

            switch (_type)
            {
                case SciArrayType.Int16:
                    {
                        short fillValue = value.ToInt16();
                        var target = new UShortAccess(_data, index * 2);
                        while (count-- != 0)
                        {
                            target.Value = (ushort)fillValue;
                            target.Offset += 2;
                        }
                        break;
                    }
                case SciArrayType.ID:
                    {
                        var target = new RegisterBytePtr(new BytePtr(_data, index * Register.Size));
                        for (int i = 0; i < count; i++)
                        {
                            target[i] = value;
                        }
                        break;
                    }
                case SciArrayType.Byte:
                case SciArrayType.String:
                    {
                        BytePtr target = new BytePtr(_data, index);
                        byte fillValue = (byte)value.Offset;
                        while (count-- != 0)
                        {
                            target.Value = fillValue;
                            target.Offset++;
                        }
                        break;
                    }
                case SciArrayType.Invalid:
                    Error("Attempted write to uninitialized SciArray");
                    break;
            }
        }

        /// <summary>
        /// Copies the string from the given Common::String into this array.
        /// </summary>
        /// <param name="string"></param>
        public void FromString(string @string)
        {
            // At least LSL6hires uses a byte-type array to hold string data
            System.Diagnostics.Debug.Assert(_type == SciArrayType.String || _type == SciArrayType.Byte);
            Array.Resize(ref _data, @string.Length + 1);
            Array.Copy(@string.GetBytes(), _data, @string.Length);
            _data[@string.Length] = 0;
        }

        /// <summary>
        /// Sets the type of this array. The type of the array may only be set once.
        /// </summary>
        /// <param name="type"></param>
        public void SetType(SciArrayType type)
        {
            System.Diagnostics.Debug.Assert(_type == SciArrayType.Invalid);
            switch (type)
            {
                case SciArrayType.ID:
                    _elementSize = Register.Size;
                    break;
                case SciArrayType.Int16:
                    _elementSize = sizeof(short);
                    break;
                case SciArrayType.String:
                    _elementSize = sizeof(byte);
                    break;
                case SciArrayType.Byte:
                    _elementSize = sizeof(byte);
                    break;
                default:
                    Error("Invalid array type {0}", type);
                    break;
            }
            _type = type;
        }

        /// <summary>
        /// Ensures the array is large enough to store at least the given number of
        /// values given in `newSize`. If `force` is true, the array will be resized
        /// to store exactly `newSize` values. New values are initialized to zero.
        /// </summary>
        /// <param name="newSize"></param>
        /// <param name="force"></param>
        public void Resize(ushort newSize, bool force = false)
        {
            if (force || newSize > _size)
            {
                Array.Resize(ref _data, _elementSize * newSize);
                _size = newSize;
            }
        }

        /// <summary>
        /// Shrinks a string array to its optimal size.
        /// </summary>
        public void Snug()
        {
            System.Diagnostics.Debug.Assert(_type == SciArrayType.String || _type == SciArrayType.Byte);
            Resize((ushort)(_data.GetTextLength() + 1), true);
        }

        /// <summary>
        /// Gets the value at the given index as a Register.
        /// </summary>
        /// <returns></returns>
        public Register GetAsID(ushort index)
        {
            if (ResourceManager.GetSciVersion() >= SciVersion.V3)
            {
                Resize(index);
            }
            else
            {
                System.Diagnostics.Debug.Assert(index < _size);
            }

            switch (_type)
            {
                case SciArrayType.Int16:
                    return Register.Make(0, (ushort)_data.ToInt16(2 * index));
                case SciArrayType.Byte:
                case SciArrayType.String:
                    return Register.Make(0, _data[index]);
                case SciArrayType.ID:
                    return new RegisterBytePtr(_data)[index];
                default:
                    Error("Invalid array type {0}", _type);
                    return Register.NULL_REG;
            }
        }

        /// <summary>
        /// Sets the value at the given index from a Register.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void SetFromID(ushort index, Register value)
        {
            if (ResourceManager.GetSciVersion() >= SciVersion.V3)
            {
                Resize(index);
            }
            else
            {
                System.Diagnostics.Debug.Assert(index < _size);
            }

            switch (_type)
            {
                case SciArrayType.Int16:
                    _data.WriteInt16(index * 2, value.ToInt16());
                    break;
                case SciArrayType.Byte:
                case SciArrayType.String:
                    _data[index] = (byte)value.ToInt16();
                    break;
                case SciArrayType.ID:
                    new RegisterBytePtr(_data)[index] = value;
                    break;
                default:
                    Error("Invalid array type {0}", _type);
                    break;
            }
        }

        /// <summary>
        /// Returns a reference to the byte at the given index. Only valid for
        /// string and byte arrays.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public BytePtr ByteAt(ushort index)
        {
            System.Diagnostics.Debug.Assert(_type == SciArrayType.String || _type == SciArrayType.Byte);

            if (ResourceManager.GetSciVersion() >= SciVersion.V3)
            {
                Resize(index);
            }
            else
            {
                System.Diagnostics.Debug.Assert(index < _size);
            }

            return new BytePtr(_data, index);
        }

        public byte this[ushort index]
        {
            get { return ByteAt(index)[0]; }
            set
            {
                var ptr = ByteAt(index);
                ptr[0] = value;
            }
        }

        /// <summary>
        /// Gets the value at the given index as an int16.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public short GetAsInt16(ushort index)
        {
            System.Diagnostics.Debug.Assert(_type == SciArrayType.Int16);

            if (ResourceManager.GetSciVersion() >= SciVersion.V3)
            {
                Resize(index);
            }
            else
            {

                System.Diagnostics.Debug.Assert(index < _size);
            }

            Register value = new Register(new BytePtr(_data, index * Register.Size));
            System.Diagnostics.Debug.Assert(value.IsNumber);
            return value.ToInt16();
        }

        public virtual void Destroy()
        {
            _data = null;
            _type = SciArrayType.Invalid;
            _size = _elementSize = 0;
        }

        /// <summary>
        /// Reads values from the given reg_t pointer and sets them in the array,
        /// growing the array if needed to store all values.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <param name="values"></param>
        public void SetElements(ushort index, ushort count, StackPtr values)
        {
            Resize((ushort)(index + count));

            switch (_type)
            {
                case SciArrayType.Int16:
                    {
                        StackPtr source = values;
                        var target = new UShortAccess(_data, index);
                        while (count-- != 0)
                        {
                            if (!source[0].IsNumber)
                            {
                                Error("Non-number {0} sent to int16 array", source[0]);
                            }
                            target.Value = (ushort)source[0].ToInt16();
                            target.Offset++;
                            ++source;
                        }
                        break;
                    }
                case SciArrayType.ID:
                    {
                        StackPtr source = values;
                        RegisterBytePtr target = new RegisterBytePtr(new BytePtr(_data, index * Register.Size));
                        for (int i = 0; i < count; i++)
                        {
                            target[i] = source[0];
                            source++;
                        }
                        break;
                    }
                case SciArrayType.Byte:
                case SciArrayType.String:
                    {
                        StackPtr source = values;
                        var target = new BytePtr(_data, index);
                        while (count-- != 0)
                        {
                            if (!source[0].IsNumber)
                            {
                                Error("Non-number {0} sent to byte or string array", source[0]);
                            }
                            target.Value = (byte)source[0].Offset;
                            target.Offset++;
                            ++source;
                        }
                        break;
                    }
                default:
                    Error("Attempted write to SciArray with invalid type {0}", _type);
                    break;
            }
        }

        public SciArrayType Type => _type;

        /// <summary>
        /// Returns the size of the array, in elements.
        /// </summary>
        public int Size => _size;

        /// <summary>
        /// Returns the size of the array, in bytes.
        /// </summary>
        public int ByteSize => _size * _elementSize;

        /// <summary>
        /// Returns a pointer to the array's raw data storage.
        /// </summary>
        public byte[] RawData => _data;

        protected byte[] _data;
        protected SciArrayType _type;
        protected int _size; // _size holds the number of entries that the scripts have requested
        protected byte _elementSize;

        public void ByteCopy(SciArray source, ushort sourceOffset, ushort targetOffset, ushort count)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class ArrayTable : SegmentObjTable<SciArray>
    {
        public ArrayTable() : base(SegmentType.ARRAY)
        {
        }

        public override void FreeAtAddress(SegManager segMan, Register subAddr)
        {
            this[(int)subAddr.Offset].Destroy();
            FreeEntry((int)subAddr.Offset);
        }

        public override List<Register> ListAllOutgoingReferences(Register addr)
        {
            var refs = new List<Register>();
            if (!IsValidEntry((int)addr.Offset))
            {
                // explicitly freed; ignore these references
                Error("Invalid array referenced for outgoing references: {0}", addr);
            }

            var array = this[(int)addr.Offset];
            if (array.Type == SciArrayType.ID)
            {
                for (var i = 0; i < array.Size; i++)
                {
                    var value = array.GetAsID((ushort)i);
                    if (value.IsPointer)
                        refs.Add(value);
                }
            }

            return refs;
        }

        public override SegmentRef Dereference(Register pointer)
        {
            SegmentRef ret = new SegmentRef();

            SciArray array = this[(int)pointer.Offset];
            bool isRaw = array.Type != SciArrayType.ID;

            ret.isRaw = isRaw;
            ret.maxSize = isRaw ? array.ByteSize : array.Size;
            if (isRaw)
            {
                ret.raw = array.RawData;
            }
            else
            {
                ret.reg = new RegisterBytePtr(array.RawData);
            }
            return ret;
        }
    }
}

#endif