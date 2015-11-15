//
//  AkosHeader.cs
//
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
using System.Runtime.InteropServices;

namespace NScumm.Scumm.IO
{
    struct AkosHeader
    {
        public ushort unk_1;
        public byte flags;
        public byte unk_2;
        public ushort num_anims;
        public ushort unk_3;
        public ushort codec;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct AkosOffset
    {
        [FieldOffset(0)]
        public uint akcd;
        // offset into the akcd data
        [FieldOffset(4)]
        public ushort akci;
        // offset into the akci data
    }
}

