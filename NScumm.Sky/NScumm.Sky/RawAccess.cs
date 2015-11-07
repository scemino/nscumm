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
