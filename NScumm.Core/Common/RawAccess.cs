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

namespace NScumm.Core.Common
{
    public class ByteAccess
    {
        private readonly byte[] _data;
        private int _offset;

        public byte Value
        {
            get { return _data[_offset]; }
            set { _data[_offset] = value; }
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public int Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public byte this[int index]
        {
            get { return _data[_offset + index]; }
            set { _data[_offset + index] = value; }
        }

        public ByteAccess(ByteAccess data, int offset = 0)
            : this(data.Data, data.Offset + offset)
        {
        }

        public ByteAccess(byte[] data, int offset = 0)
        {
            _data = data;
            _offset = offset;
        }

        public uint ReadUInt32(int offset = 0)
        {
            return _data.ToUInt32(_offset + offset);
        }

        public uint ReadUInt32BigEndian(int offset = 0)
        {
            return _data.ToUInt32BigEndian(_offset + offset);
        }

        public int ReadInt32(int offset = 0)
        {
            return _data.ToInt32(_offset + offset);
        }

        public ushort ReadUInt16(int offset = 0)
        {
            return _data.ToUInt16(_offset + offset);
        }

        public ushort ReadUInt16BigEndian(int offset = 0)
        {
            return _data.ToUInt16BigEndian(_offset + offset);
        }

        public short ReadInt16(int offset = 0)
        {
            return _data.ToInt16(_offset + offset);
        }

        public void WriteUInt16(int offset, ushort value)
        {
            _data.WriteUInt16(_offset + offset, value);
        }

        public UShortAccess ToUInt16()
        {
            return new UShortAccess(_data, _offset);
        }

        public byte Increment()
        {
            var ret = Value;
            _offset++;
            return ret;
        }
    }

    public class UShortAccess
    {
        private readonly byte[] _data;
        private int _offset;

        public byte[] Data
        {
            get { return _data; }
        }

        public ushort Value
        {
            get { return _data.ToUInt16(Offset); }
            set { _data.WriteUInt16(Offset, value); }
        }

        public int Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public ushort this[int index]
        {
            get { return _data.ToUInt16(Offset + index * 2); }
            set { _data.WriteUInt16(Offset + index * 2, value); }
        }

        public UShortAccess(UShortAccess data, int offset = 0)
            : this(data.Data, data.Offset + offset)
        {
        }

        public UShortAccess(ByteAccess data, int offset = 0)
            : this(data.Data, data.Offset + offset)
        {
        }

        public UShortAccess(byte[] data, int offset = 0)
        {
            _data = data;
            Offset = offset;
        }

        public ByteAccess ToByte()
        {
            return new ByteAccess(_data, _offset);
        }
    }

    public class UIntAccess
    {
        private readonly byte[] _data;
        private int _offset;

        public byte[] Data
        {
            get { return _data; }
        }

        public uint Value
        {
            get { return _data.ToUInt32(Offset); }
            set { _data.WriteUInt32(Offset, value); }
        }

        public int Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public uint this[int index]
        {
            get { return _data.ToUInt32(Offset + index * 4); }
            set { _data.WriteUInt32(Offset + index * 4, value); }
        }

        public UIntAccess(byte[] data, int offset = 0)
        {
            _data = data;
            Offset = offset;
        }
    }

    public class StructAccess<T>
    {
        private readonly byte[] _data;
        private readonly int _offset;
        private readonly int _size;

        public T Value
        {
            get { return ServiceLocator.Platform.ToStructure<T>(_data, _offset); }
        }

        public T this[int index]
        {
            get { return ServiceLocator.Platform.ToStructure<T>(_data, _offset + index * _size); }
        }

        public StructAccess(byte[] data, int offset)
        {
            _size = ServiceLocator.Platform.SizeOf<T>();
            _data = data;
            _offset = offset;
        }
    }

    public class FieldAccess<T>
    {
        private readonly Func<T> _getField;
        private readonly Action<T> _setField;

        public T Field
        {
            get { return _getField(); }
            set { _setField(value); }
        }

        public FieldAccess(Func<T> get, Action<T> set)
        {
            _getField = get;
            _setField = set;
        }
    }
}
