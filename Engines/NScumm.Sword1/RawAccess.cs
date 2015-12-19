using NScumm.Core;
using System;

namespace NScumm.Sword1
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

        public ByteAccess(ByteAccess data)
            : this(data.Data, data.Offset)
        {
        }

        public ByteAccess(byte[] data, int offset = 0)
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

        public UShortAccess(byte[] data, int offset = 0)
        {
            _data = data;
            Offset = offset;
        }
    }

    class UIntAccess
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
