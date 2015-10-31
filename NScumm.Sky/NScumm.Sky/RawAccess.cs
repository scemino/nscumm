using NScumm.Core;

namespace NScumm.Sky
{
    class ByteAccess
    {
        private byte[] _data;
        private int _offset;

        public byte Value
        {
            get { return _data[_offset]; }
            set { _data[_offset] = value; }
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
        private byte[] _data;
        private int _offset;

        public ushort Value
        {
            get { return _data.ToUInt16(_offset); }
            set { _data.WriteUInt16(_offset, value); }
        }

        public ushort this[int index]
        {
            get { return _data.ToUInt16(_offset + index * 2); }
            set { _data.WriteUInt16(_offset + index * 2, value); }
        }

        public UShortAccess(byte[] data, int offset)
        {
            _data = data;
            _offset = offset;
        }
    }

    class StructAccess<T>
    {
        private byte[] _data;
        private int _offset;
        private int _size;

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
}
