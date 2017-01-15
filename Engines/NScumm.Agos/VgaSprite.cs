﻿//
//  VgaSprite.cs
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

namespace NScumm.Agos
{
    internal class VgaSprite
    {
        public ushort id;
        public short image;
        public ushort palette;
        public short x, y;
        public DrawFlags flags;
        public ushort priority;
        public ushort windowNum;
        public ushort zoneNum;

        public VgaSprite()
        {
        }

        public VgaSprite(VgaSprite sprite)
        {
            id = sprite.id;
            image = sprite.image;
            palette = sprite.palette;
            x = sprite.x;
            y = sprite.y;
            flags = sprite.flags;
            priority = sprite.priority;
            windowNum = sprite.windowNum;
            zoneNum = sprite.zoneNum;
        }
    }
}