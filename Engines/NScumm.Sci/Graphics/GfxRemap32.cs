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

using System;
using NScumm.Core;

namespace NScumm.Sci.Graphics
{
    internal class GfxRemap32
    {
        /**
         * The number of currently active remaps.
         */
        private byte _numActiveRemaps;
        /**
         * The first index of the remap area in the system
         * palette.
         */
        private byte _remapStartColor;
        /**
         * The last index of the remap area in the system
         * palette.
         */
        private byte _remapEndColor;

        public byte RemapCount => _numActiveRemaps;
        public byte StartColor => _remapStartColor;

        /**
         * The list of SingleRemaps.
         */
//        List<SingleRemap> _remaps;

        /**
         * Determines whether or not the given color has an
         * active remapper. If it does not, it is treated as a
         * skip color and the pixel is not drawn.
         *
         * @note SSCI uses a boolean array to decide whether a
         * a pixel is remapped, but it is possible to get the
         * same information from `_remaps`, as this function
         * does.
         * Presumably, the separate array was created for
         * performance reasons, since this is called a lot in
         * the most critical section of the renderer.
         */

        public bool RemapEnabled(byte color)
        {
            throw new NotImplementedException();
//            byte index = (byte) (_remapEndColor - color);
//            Debug.Assert(index < _remaps.Count);
//            return (_remaps[index]._type != Remap.None);
        }

        public byte RemapColor(byte pixel, BytePtr target)
        {
            throw new NotImplementedException();
        }
    }
}