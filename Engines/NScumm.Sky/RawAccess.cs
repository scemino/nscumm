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

using NScumm.Core;
using System;

namespace NScumm.Sky
{
    class ByteAccess
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

        public ByteAccess(byte[] data, int offset)
        {
            _data = data;
            _offset = offset;
        }
    }

    class UShortAccess
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

        public UShortAccess(byte[] data, int offset)
        {
            _data = data;
            Offset = offset;
        }
    }

    class StructAccess<T>
    {
        private readonly byte[] _data;
        private readonly int _offset;
        private readonly int _size;
		private readonly Func<byte[], int, T> _getObject;

        public T Value
        {
			get { return _getObject(_data, _offset); }
        }

        public T this[int index]
        {
			get { return _getObject(_data, _offset + index * _size); }
        }

		public StructAccess(byte[] data, int offset, int size, Func<byte[], int, T> getObject)
        {
            _size = size;
			_getObject = getObject;
            _data = data;
            _offset = offset;
        }
    }

    class FieldAccess<T>
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
