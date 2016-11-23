//
//  SubroutineLine.cs
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
    internal class SubroutineLine
    {
        public const int Size = 8;

        public ushort next
        {
            get { return Pointer.ToUInt16(); }
            set { Pointer.WriteUInt16(0, value); }
        }

        public short verb
        {
            get { return Pointer.ToInt16(2); }
            set { Pointer.WriteInt16(2, value); }
        }

        public short noun1
        {
            get { return Pointer.ToInt16(4); }
            set { Pointer.WriteInt16(4, value); }
        }

        public short noun2
        {
            get { return Pointer.ToInt16(6); }
            set { Pointer.WriteInt16(6, value); }
        }

        public BytePtr Pointer;

        public SubroutineLine(BytePtr ptr)
        {
            Pointer = ptr;
        }
    }
}