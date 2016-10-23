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
    /// A single block of text written to a ScrollWindow.
    /// </summary>
    internal class ScrollWindowEntry
    {
        /**
         * ID of the line. In SSCI this was actually a memory
         * handle for the string of this line. We use a simple
         * numeric ID instead.
         */
        public Register id;

        /**
         * The alignment to use when rendering this line of
         * text. If -1, the default alignment from the
         * corresponding ScrollWindow will be used.
         */
        public TextAlign alignment;

        /**
         * The color to use to render this line of text. If -1,
         * the default foreground color from the corresponding
         * ScrollWindow will be used.
         */
        public short foreColor;

        /**
         * The font to use to render this line of text. If -1,
         * the default font from the corresponding ScrollWindow
         * will be used.
         */
        public int fontId;

        /**
         * The text.
         */
        public string text;
    }
}

#endif
