//
//  BytePtr.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
    public struct BytePtr
    {
        public int Offset;
        public readonly byte[] Data;

        public static readonly BytePtr Null = new BytePtr(null);

        public byte Value
        {
            get { return Data[Offset]; }
            set { Data[Offset] = value; }
        }

        public byte this[int index]
        {
            get { return Data[Offset + index]; }
            set { Data[Offset + index] = value; }
        }

        public BytePtr(BytePtr ptr, int offset = 0)
        {
            Data = ptr.Data;
            Offset = ptr.Offset + offset;
        }

        public BytePtr(byte[] data, int offset = 0)
        {
            Data = data;
            Offset = offset;
        }

        public static implicit operator BytePtr(ByteAccess ba)
        {
            return new BytePtr(ba.Data, ba.Offset);
        }

        public static implicit operator BytePtr(byte[] ba)
        {
            return new BytePtr(ba);
        }

        public static implicit operator ByteAccess(BytePtr p)
        {
            return new ByteAccess(p.Data, p.Offset);
        }

        public static bool operator ==(BytePtr p1, BytePtr p2)
        {
            return p1.Data == p2.Data &&
                     p1.Offset == p2.Offset;
        }

        public static bool operator !=(BytePtr p1, BytePtr p2)
        {
            return !(p1 == p2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BytePtr)) return false;
            return this == (BytePtr)obj;
        }

        public override int GetHashCode()
        {
            return Data == null ? 0 : Data.GetHashCode() ^ Offset ;
        }
    }

    public struct Int32Ptr
    {
        public int Offset;
        public readonly int[] Data;

        public static readonly Int32Ptr Null = new Int32Ptr(null);

        public int Value
        {
            get { return Data[Offset]; }
            set { Data[Offset] = value; }
        }

        public int this[int index]
        {
            get { return Data[Offset + index]; }
            set { Data[Offset + index] = value; }
        }

        public Int32Ptr(Int32Ptr ptr, int offset = 0)
        {
            Data = ptr.Data;
            Offset = ptr.Offset + offset;
        }

        public Int32Ptr(int[] data, int offset = 0)
        {
            Data = data;
            Offset = offset;
        }

        public static bool operator ==(Int32Ptr p1, Int32Ptr p2)
        {
            return p1.Data == p2.Data &&
                     p1.Offset == p2.Offset;
        }

        public static bool operator !=(Int32Ptr p1, Int32Ptr p2)
        {
            return !(p1 == p2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Int32Ptr)) return false;
            return this == (Int32Ptr)obj;
        }

        public override int GetHashCode()
        {
            return Data == null ? 0 : Data.GetHashCode() ^ Offset;
        }
    }

    public struct Ptr<T>
    {
        public int Offset;
        public readonly T[] Data;

        public static readonly Ptr<T> Null = new Ptr<T>(null);

        public T Value
        {
            get { return Data[Offset]; }
            set { Data[Offset] = value; }
        }

        public T this[int index]
        {
            get { return Data[Offset + index]; }
            set { Data[Offset + index] = value; }
        }

        public Ptr(Ptr<T> ptr, int offset = 0)
        {
            Data = ptr.Data;
            Offset = ptr.Offset + offset;
        }

        public Ptr(T[] data, int offset = 0)
        {
            Data = data;
            Offset = offset;
        }

        public static implicit operator Ptr<T>(T[] ba)
        {
            return new Ptr<T>(ba);
        }

        public static bool operator ==(Ptr<T> p1, Ptr<T> p2)
        {
            return p1.Data == p2.Data &&
                     p1.Offset == p2.Offset;
        }

        public static bool operator !=(Ptr<T> p1, Ptr<T> p2)
        {
            return !(p1 == p2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Ptr<T>)) return false;
            return this == (Ptr<T>)obj;
        }

        public override int GetHashCode()
        {
            return Data == null ? 0 : Data.GetHashCode() ^ Offset;
        }
    }

    public static class BytePtrExtension
    {
        public static void WriteInt16(this BytePtr data, int startIndex, short value)
        {
            data.Data.WriteInt16(data.Offset + startIndex, value);
        }

        public static void WriteInt16BigEndian(this BytePtr data, int startIndex, short value)
        {
            data.Data.WriteInt16BigEndian(data.Offset + startIndex, value);
        }

        public static void WriteUInt16(this BytePtr data, int startIndex, ushort value)
        {
            data.Data.WriteUInt16(data.Offset + startIndex, value);
        }

        public static void WriteUInt16BigEndian(this BytePtr data, int startIndex, ushort value)
        {
            data.Data.WriteUInt16BigEndian(data.Offset + startIndex, value);
        }

        public static void WriteUInt32(this BytePtr data, int startIndex, uint value)
        {
            data.Data.WriteUInt32(data.Offset + startIndex, value);
        }

        public static short ToInt16BigEndian(this BytePtr value, int startIndex = 0)
        {
            return (short)value.Data.ToUInt16BigEndian(value.Offset + startIndex);
        }

        public static short ToInt16(this BytePtr value, int startIndex = 0)
        {
            return (short)value.Data.ToUInt16(value.Offset + startIndex);
        }

        public static ushort ToUInt16BigEndian(this BytePtr value, int startIndex = 0)
        {
            return value.Data.ToUInt16BigEndian(value.Offset + startIndex);
        }

        public static ushort ToUInt16(this BytePtr value, int startIndex = 0)
        {
            return value.Data.ToUInt16(value.Offset + startIndex);
        }

        public static int ToInt32BigEndian(this BytePtr value, int startIndex = 0)
        {
            return value.Data.ToInt32BigEndian(value.Offset + startIndex);
        }

        public static int ToInt32(this BytePtr value, int startIndex = 0)
        {
            return value.Data.ToInt32(value.Offset + startIndex);
        }

        public static uint ToUInt32BigEndian(this BytePtr value, int startIndex = 0)
        {
            return value.Data.ToUInt32BigEndian(value.Offset + startIndex);
        }

        public static uint ToUInt24(this BytePtr value, int startIndex = 0)
        {
            return value.Data.ToUInt24(value.Offset + startIndex);
        }

        public static uint ToUInt32(this BytePtr value, int startIndex = 0)
        {
            return value.Data.ToUInt32(value.Offset + startIndex);
        }
    }
}
