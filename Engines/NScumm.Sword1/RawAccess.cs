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

namespace NScumm.Sword1
{
    class ByteAccess
    {
        public byte Value
        {
            get { return Data[Offset]; }
            set { Data[Offset] = value; }
        }

        public byte[] Data { get; }

        public int Offset { get; set; }

        public byte this[int index]
        {
            get { return Data[Offset + index]; }
            set { Data[Offset + index] = value; }
        }

        public ByteAccess(ByteAccess data)
            : this(data.Data, data.Offset)
        {
        }

        public ByteAccess(byte[] data, int offset = 0)
        {
            Data = data;
            Offset = offset;
        }
    }

    class UShortAccess
    {
        public BytePtr Data;

        public ushort Value
        {
            get { return Data.ToUInt16(); }
            set { Data.WriteUInt16(0, value); }
        }

        public ushort this[int index]
        {
            get { return Data.ToUInt16(index * 2); }
            set { Data.WriteUInt16(index * 2, value); }
        }

        public UShortAccess(BytePtr data, int offset = 0)
        {
            Data = new BytePtr(data,offset);
        }
    }

    class UIntAccess
    {
        public readonly BytePtr Data;

        public uint Value
        {
            get { return Data.ToUInt32(Offset); }
            set { Data.WriteUInt32(Offset, value); }
        }

        public int Offset { get; set; }

        public uint this[int index]
        {
            get { return Data.ToUInt32(Offset + index * 4); }
            set { Data.WriteUInt32(Offset + index * 4, value); }
        }

        public UIntAccess(BytePtr data, int offset = 0)
        {
            Data = data;
            Offset = offset;
        }
    }
}
