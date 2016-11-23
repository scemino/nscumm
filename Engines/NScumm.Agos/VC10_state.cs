//
//  VC10_state.cs
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

namespace NScumm.Agos
{
    [Flags]
    enum DrawFlags
    {
        kDFFlip = 0x1,
        kDFNonTrans = 0x2,
        kDFSkipStoreBG = 0x4,
        kDFCompressed = 0x8,
        kDFCompressedFlip = 0x10,
        kDFMasked = 0x20,

        // Feeble specific
        kDFOverlayed = 0x10,
        kDFScaled = 0x40,
        kDFShaded = 0x80
    }

    class VC10_state
    {
        public short image;
        public DrawFlags flags;
        public byte palette;
        public byte paletteMod;

        public short x, y;
        public ushort width, height;
        public ushort draw_width, draw_height;
        public ushort x_skip, y_skip;

        public BytePtr surf2_addr;
        public uint surf2_pitch;

        public BytePtr surf_addr;
        public uint surf_pitch;

        public ushort dl, dh;

        public BytePtr srcPtr;
        public sbyte depack_cont;

        public byte[] depack_dest = new byte[480];
    }
}