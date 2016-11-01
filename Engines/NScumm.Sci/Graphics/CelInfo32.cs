//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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

#if ENABLE_SCI32

using NScumm.Sci.Engine;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// A CelInfo32 object describes the basic properties of a
    /// cel object.
    /// </summary>
    internal class CelInfo32
    {
        /// <summary>
        /// The type of the cel object.
        /// </summary>
        public CelType type;

        /// <summary>
        /// For cel objects that draw from resources, the ID of
        /// the resource to load.
        /// </summary>
        public int resourceId;

        /// <summary>
        /// For CelObjView, the loop number to draw from the
        /// view resource.
        /// </summary>
        public short loopNo;

        /// <summary>
        /// For CelObjView and CelObjPic, the cel number to draw
        /// from the view or pic resource.
        /// </summary>
        public short celNo;

        /// <summary>
        /// For CelObjMem, a segment register pointing to a heap
        /// resource containing headered bitmap data.
        /// </summary>
        public Register bitmap;

        /// <summary>
        /// For CelObjColor, the fill color.
        /// </summary>
        public byte color;

        // NOTE: In at least SCI2.1/SQ6, color is left
        // uninitialised.
        public CelInfo32()
        {
            type = CelType.Mem;
        }

        // NOTE: This is the equivalence criteria used by
        // CelObj::searchCache in at least SCI2.1/SQ6. Notably,
        // it does not check the color field.
        public static bool operator ==(CelInfo32 info1, CelInfo32 info2)
        {
            return info1.type == info2.type &&
                   info1.resourceId == info2.resourceId &&
                   info1.loopNo == info2.loopNo &&
                   info1.celNo == info2.celNo &&
                   info1.bitmap == info2.bitmap;
        }

        public static bool operator !=(CelInfo32 info1, CelInfo32 info2)
        {
            return !(info1 == info2);
        }

        public override int GetHashCode()
        {
            return type.GetHashCode() ^
                   resourceId.GetHashCode() ^
                   loopNo.GetHashCode() ^
                   celNo.GetHashCode() ^
                   bitmap.GetHashCode();
        }

        public CelInfo32 Clone()
        {
            var info = new CelInfo32
            {
                type = type,
                resourceId = resourceId,
                loopNo = loopNo,
                celNo = celNo,
                bitmap = bitmap,
                color = color
            };
            return info;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is CelInfo32)) return false;
            return this == (CelInfo32)obj;
        }
    }
}

#endif
