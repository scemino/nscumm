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

using System.Diagnostics;
using NScumm.Core;

namespace NScumm.Sci.Graphics
{
    internal class READER_Uncompressed : IReader
    {

#if !NDEBUG
        private readonly short _sourceHeight;
#endif
        private readonly BytePtr _pixels;
        private readonly short _sourceWidth;

        public READER_Uncompressed(CelObj celObj, short tmp)
        {
#if !NDEBUG
            _sourceHeight = (short) celObj._height;
#endif
            _sourceWidth = (short) celObj._width;
            var resource = celObj.GetResPointer();
            _pixels = new BytePtr(resource,
                (int) resource.Data.ReadSci11EndianUInt32((int) (resource.Offset + celObj._celHeaderOffset + 24)));
        }

        public BytePtr GetRow(short y)
        {
            Debug.Assert(y >= 0 && y < _sourceHeight);
            return new BytePtr(_pixels, y * _sourceWidth);
        }
    }
}
#endif
