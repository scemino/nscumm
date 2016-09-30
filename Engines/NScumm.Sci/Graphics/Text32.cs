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

using NScumm.Core.Graphics;
using NScumm.Sci.Engine;

namespace NScumm.Sci.Graphics
{
    internal enum TextAlign
    {
        Default = -1,
        Left = 0,
        Center = 1,
        Right = 2
    }

    /// <summary>
    /// Text32 class, handles text calculation and displaying of text for SCI2, SCI21 and SCI3 games
    /// </summary>
    internal class GfxText32
    {
        /**
         * The memory handle of the currently active bitmap.
         */
        public Register _bitmap;

        /**
         * The size of the x-dimension of the coordinate system
         * used by the text renderer. Static since it was global in SSCI.
         */
        public static short _scaledWidth;

        /**
         * The size of the y-dimension of the coordinate system
         * used by the text renderer. Static since it was global in SSCI.
         */
        public static short _scaledHeight;

        /**
         * The currently active font resource used to write text
         * into the bitmap.
         *
         * @note SCI engine builds the font table directly
         * inside of FontMgr; we use GfxFont instead.
         */
        public GfxFont _font;

        private SegManager _segMan;
        private GfxCache _cache;

        /**
         * The resource ID of the default font used by the game.
         *
         * @todo Check all SCI32 games to learn what their
         * default font is.
         */
        private static short _defaultFontId;

        /**
         * The width and height of the currently active text
         * bitmap, in text-system coordinates.
         *
         * @note These are unsigned in the actual engine.
         */
        private short _width, _height;

        /**
         * The color used to draw text.
         */
        private byte _foreColor;

        /**
         * The background color of the text box.
         */
        private byte _backColor;

        /**
         * The transparent color of the text box. Used when
         * compositing the bitmap onto the screen.
         */
        private byte _skipColor;

        /**
         * The rect where the text is drawn within the bitmap.
         * This rect is clipped to the dimensions of the bitmap.
         */
        private Rect _textRect;

        /**
         * The text being drawn to the currently active text
         * bitmap.
         */
        private string _text;

        /**
         * The font being used to draw the text.
         */
        private int _fontId;

        /**
         * The color of the text box border.
         */
        private short _borderColor;

        /**
         * TODO: Document
         */
        private bool _dimmed;

        /**
         * The text alignment for the drawn text.
         */
        private TextAlign _alignment;

        /**
         * The position of the text draw cursor.
         */
        private Point _drawPosition;

        public GfxText32(SegManager segMan, GfxCache fonts)
        {
            _segMan = segMan;
            _cache = fonts;
        }
    }
}
