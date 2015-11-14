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

namespace NScumm.Scumm
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
        public int Ulx, Uly;
        public int Urx, Ury;
        public int Lrx, Lry;
        public int Llx, Lly;
        public int Mask;
        public BoxFlags Flags;
        public int ScaleSlot;
        public int Scale;
    }
}
