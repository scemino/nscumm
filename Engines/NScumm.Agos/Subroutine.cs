//
//  Subroutine.cs
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

namespace NScumm.Agos
{
    class Subroutine
    {
        public const int Size = 8;

        /// <summary>
        /// Subroutine ID.
        /// </summary>
        public ushort id
        {
            get { return Pointer.ToUInt16(); }
            set { Pointer.WriteUInt16(0, value); }
        }

        /// <summary>
        /// Offset from subroutine start to first subroutine line.
        /// </summary>
        public ushort first
        {
            get { return Pointer.ToUInt16(2); }
            set { Pointer.WriteUInt16(2, value); }
        }

        /// <summary>
        /// Next subroutine in linked list.
        /// </summary>
        public int nextOffset
        {
            get { return Pointer.ToInt32(4); }
            set { Pointer.WriteInt32(4, value); }
        }

        public Subroutine next
        {
            get { return nextOffset == 0 ? null : new Subroutine(Pointer + nextOffset); }
            set
            {
                if (value == null)
                    nextOffset = 0;
                else
                    nextOffset = value.Pointer.Offset - Pointer.Offset;
            }
        }

        public BytePtr Pointer;

        public Subroutine(BytePtr ptr)
        {
            Pointer = ptr;
        }
    }
}