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

namespace NScumm.Core
{
    public class ByteAccess
    {
        private readonly byte[] _data;

        public byte Value
        {
            get { return _data[Offset]; }
            set { _data[Offset] = value; }
        }

        public byte[] Data
        {
            get { return _data; }
        }

        public int Offset
        {
            get;
            set;
        }

        public byte this[int index]
        {
            get { return _data[Offset + index]; }
            set { _data[Offset + index] = value; }
        }

        public ByteAccess(ByteAccess data, int offset = 0)
            : this(data.Data, data.Offset + offset)
        {
        }

        public ByteAccess(byte[] data, int offset = 0)
        {
            _data = data;
            Offset = offset;
        }
    }

    public static class ByteAccessExtension
    {
        public static short ToInt16BigEndian(this ByteAccess value, int startIndex = 0)
        {
            return (short)value.Data.ToUInt16BigEndian(value.Offset + startIndex);
        }

        public static ushort ToUInt16BigEndian(this ByteAccess value, int startIndex = 0)
        {
            return value.Data.ToUInt16BigEndian(value.Offset + startIndex);
        }

        public static int ToInt32BigEndian(this ByteAccess value, int startIndex = 0)
        {
            return value.Data.ToInt32BigEndian(value.Offset + startIndex);
        }

        public static uint ToUInt32BigEndian(this ByteAccess value, int startIndex = 0)
        {
            return value.Data.ToUInt32BigEndian(value.Offset + startIndex);
        }
    }

    public class UShortAccess
    {
        private readonly byte[] _data;

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
            get;
            set;
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

    public class UIntAccess
    {
        private readonly byte[] _data;

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
            get;
            set;
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
}
