//
//  ScummEngine_Cursor.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm
{
    partial class ScummEngine
    {
        protected readonly Cursor _cursor = new Cursor();
        protected byte[] _cursorData;
        protected Point _mousePos;
        protected byte cursorColor;
        protected int _currentCursor;
        protected sbyte _userPut;
        		
        protected static byte[] defaultCursorColors = { 15, 15, 7, 8 };
        protected ushort[][] _cursorImages = new ushort[4][];
        protected readonly byte[] _cursorHotspots = new byte[2 * 4];
        static readonly ushort[][] default_cursor_images =
            {
                /* cross-hair */
                new ushort[]
                {
                    0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0000, 0x7e3f,
                    0x0000, 0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0080, 0x0000
                },
                /* hourglass */
                new ushort[]
                {
                    0x0000, 0x7ffe, 0x6006, 0x300c, 0x1818, 0x0c30, 0x0660, 0x03c0,
                    0x0660, 0x0c30, 0x1998, 0x33cc, 0x67e6, 0x7ffe, 0x0000, 0x0000
                },
                /* arrow */
                new ushort[]
                {
                    0x0000, 0x4000, 0x6000, 0x7000, 0x7800, 0x7c00, 0x7e00, 0x7f00,
                    0x7f80, 0x78c0, 0x7c00, 0x4600, 0x0600, 0x0300, 0x0300, 0x0180
                },
                /* hand */
                new ushort[]
                {
                    0x1e00, 0x1200, 0x1200, 0x1200, 0x1200, 0x13ff, 0x1249, 0x1249,
                    0xf249, 0x9001, 0x9001, 0x9001, 0x8001, 0x8001, 0x8001, 0xffff
                }
            };
        static readonly byte[] default_cursor_hotspots =
            {
                8, 7,
                8, 7,
                1, 1,
                5, 0,
                8, 7, //zak256
            };

        protected virtual void SetDefaultCursor()
        {

        }

        void AnimateCursor()
        {
            if (_cursor.Animate)
            {
                if ((_cursor.AnimateIndex & 0x1) == 0)
                {
                    SetBuiltinCursor((_cursor.AnimateIndex >> 1) & 3);
                }
                _cursor.AnimateIndex++;
            }
        }

        protected abstract void SetBuiltinCursor(int idx);

        void ResetCursors()
        {
            for (int i = 0; i < 4; i++)
            {
                _cursorImages[i] = new ushort[16];
                Array.Copy(default_cursor_images[i], _cursorImages[i], 16);
            }
            Array.Copy(default_cursor_hotspots, _cursorHotspots, 8);
        }

        protected void RedefineBuiltinCursorFromChar(int index, int chr)
        {
            // Cursor image in both Loom versions are based on images from charset.
            // This function is *only* supported for Loom!
            if (_game.GameId != Scumm.IO.GameId.Loom)
                throw new NotSupportedException("RedefineBuiltinCursorFromChar is *only* supported for Loom!");
            if (index < 0 || index >= 4)
                throw new ArgumentException("index");

            //	const int oldID = _charset->getCurID();

            var ptr = _cursorImages[index];

            if (_game.Version == 3)
            {
                _charset.SetCurID(0);
            }
            else if (_game.Version >= 4)
            {
                _charset.SetCurID(1);
            }

            var s = new Surface(_charset.GetCharWidth(chr), _charset.GetFontHeight(), PixelFormat.Indexed8, false);
            var p = new PixelNavigator(s);
            Gdi.Fill(new PixelNavigator(s), 123, s.Width, s.Height);

            _charset.DrawChar(chr, s, 0, 0);

            Array.Clear(ptr, 0, ptr.Length);
            for (int h = 0; h < s.Height; h++)
            {
                for (int w = 0; w < s.Width; w++)
                {
                    p.GoTo(w, h);
                    if (p.Read() != 123)
                    {
                        ptr[h] |= (ushort)(1 << (15 - w));
                    }
                }
            }

            //	_charset->setCurID(oldID);
        }

        protected void RedefineBuiltinCursorHotspot(int index, int x, int y)
        {
            // Cursor image in both Looms are based on images from charset.
            // This function is *only* supported for Loom!
            if (_game.GameId != Scumm.IO.GameId.Loom)
                throw new NotSupportedException("RedefineBuiltinCursorHotspot is *only* supported for Loom!");
            if (index < 0 || index >= 4)
                throw new ArgumentException("index");

            _cursorHotspots[index * 2] = (byte)x;
            _cursorHotspots[index * 2 + 1] = (byte)y;
        }
    }
}

