//
//  ObjectData.cs
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

using NScumm.Core;

namespace NScumm.Queen
{
    public class ObjectData
    {
        //! entry in OBJECT_NAME (<0: object is hidden, 0: object has been deleted)
        public short name;
        //! coordinates of object
        public ushort x, y;
        //! entry in OBJECT_DESCR
        public ushort description;
        //! associated object
        public short entryObj;
        //! room in which this object is available
        public ushort room;
        //! state of the object (grab direction, on/off, default command...)
        public ushort state;
        public short image;

        public void ReadFromBE(byte[] data, ref int ptr)
        {
            name = data.ToInt16BigEndian(ptr); ptr += 2;
            x = data.ToUInt16BigEndian(ptr); ptr += 2;
            y = data.ToUInt16BigEndian(ptr); ptr += 2;
            description = data.ToUInt16BigEndian(ptr); ptr += 2;
            entryObj = data.ToInt16BigEndian(ptr); ptr += 2;
            room = data.ToUInt16BigEndian(ptr); ptr += 2;
            state = data.ToUInt16BigEndian(ptr); ptr += 2;
            image = data.ToInt16BigEndian(ptr); ptr += 2;
        }

        public void WriteToBE(byte[] data, ref int ptr)
        {
            data.WriteInt16BigEndian(ptr, name); ptr += 2;
            data.WriteUInt16BigEndian(ptr, x); ptr += 2;
            data.WriteUInt16BigEndian(ptr, y); ptr += 2;
            data.WriteUInt16BigEndian(ptr, description); ptr += 2;
            data.WriteInt16BigEndian(ptr, entryObj); ptr += 2;
            data.WriteUInt16BigEndian(ptr, room); ptr += 2;
            data.WriteUInt16BigEndian(ptr, state); ptr += 2;
            data.WriteInt16BigEndian(ptr, image); ptr += 2;
        }
    }

}
