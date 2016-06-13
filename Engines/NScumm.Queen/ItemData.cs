//
//  ItemData.cs
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

using System;
using NScumm.Core;

namespace NScumm.Queen
{
    public class ItemData
    {
        //! entry in OBJECT_NAME
        public short name;
        //! entry in OBJECT_DESCR
        public ushort description;
        //! state of the object
        public ushort state;
        //! bank bobframe
        public ushort frame;
        //! entry in OBJECT_DESCR (>0 if available)
        public short sfxDescription;

        public void ReadFromBE(byte[] data, ref int ptr)
        {
            name = data.ToInt16BigEndian(ptr); ptr += 2;
            description = data.ToUInt16BigEndian(ptr); ptr += 2;
            state = data.ToUInt16BigEndian(ptr); ptr += 2;
            frame = data.ToUInt16BigEndian(ptr); ptr += 2;
            sfxDescription = data.ToInt16BigEndian(ptr); ptr += 2;
        }

        public void WriteToBE(byte[] data, ref int ptr)
        {
            data.WriteInt16BigEndian(ptr, name); ptr += 2;
            data.WriteUInt16BigEndian(ptr, description); ptr += 2;
            data.WriteUInt16BigEndian(ptr, state); ptr += 2;
            data.WriteUInt16BigEndian(ptr, frame); ptr += 2;
            data.WriteInt16BigEndian(ptr, sfxDescription); ptr += 2;
        }
    }

}
