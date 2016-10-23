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

using System.Text;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;

namespace NScumm.Sci.Graphics
{
    internal class TextEditor
    {
        /**
         * The bitmap where the editor is rendered.
         */
        public Register bitmap;

        /**
         * The width of the editor, in bitmap pixels.
         */
        public short width;

        /**
         * The text in the editor.
         */
        public StringBuilder text = new StringBuilder();

        /**
         * The rect where text should be drawn into the editor,
         * in bitmap pixels.
         */
        public Rect textRect;

        /**
         * The color of the border. -1 indicates no border.
         */
        public short borderColor;

        /**
         * The text color.
         */
        public byte foreColor;

        /**
         * The background color.
         */
        public byte backColor;

        /**
         * The transparent color.
         */
        public byte skipColor;

        /**
         * The font used to render the text in the editor.
         */
        public int fontId;

        /**
         * The current position of the cursor within the editor.
         */
        public ushort cursorCharPosition;

        /**
         * Whether or not the cursor is currently drawn to the
         * screen.
         */
        public bool cursorIsDrawn;

        /**
         * The rectangle for drawing the input cursor, in bitmap
         * pixels.
         */
        public Rect cursorRect;

        /**
         * The maximum allowed text length, in characters.
         */
        public ushort maxLength;
    }
}