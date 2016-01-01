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
using System;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// Text16 class, handles text calculation and displaying of text for SCI0.SCI1.1 games
    /// </summary>
    internal class GfxText16
    {
        public const int SCI_TEXT16_ALIGNMENT_RIGHT = -1;
        public const int SCI_TEXT16_ALIGNMENT_CENTER = 1;
        public const int SCI_TEXT16_ALIGNMENT_LEFT = 0;

        private GfxCache _cache;
        private GfxPaint16 _paint16;
        private GfxPorts _ports;
        private GfxScreen _screen;
        private GfxFont _font;
        private int _codeFontsCount;
        private int[] _codeFonts;
        private int _codeColorsCount;
        private ushort[] _codeColors;

        private Rect _codeRefTempRect;
        //private CodeRefRectArray _codeRefRects;

        public GfxText16(GfxCache cache, GfxPorts ports, GfxPaint16 paint16, GfxScreen screen)
        {
            _cache = cache;
            _ports = ports;
            _paint16 = paint16;
            _screen = screen;

            Init();
        }

        private void Init()
        {
            _font = null;
            _codeFonts = null;
            _codeFontsCount = 0;
            _codeColors = null;
            _codeColorsCount = 0;
        }

        public void SetFont(int fontId)
        {
            if ((_font == null) || (_font.ResourceId != fontId))
                _font = _cache.GetFont(fontId);

            _ports._curPort.fontId = _font.ResourceId;
            _ports._curPort.fontHeight = _font.Height;
        }

        internal void Box(string title, bool v1, Rect r, object sCI_TEXT16_ALIGNMENT_CENTER, int v2)
        {
            throw new NotImplementedException();
        }
    }
}
