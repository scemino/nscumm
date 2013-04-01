/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Scumm4
{
    [Flags]
    public enum BoxFlags
    {
        XFlip = 0x08,
        YFlip = 0x10,
        IgnoreScale = 0x20,
        PlayerOnly = 0x20,
        Locked = 0x40,
        Invisible = 0x80
    }

    public class Box
    {
        public short ulx, uly;
        public short urx, ury;
        public short lrx, lry;
        public short llx, lly;
        public byte mask;
        public BoxFlags flags;
        public ushort scale;
    }
}
