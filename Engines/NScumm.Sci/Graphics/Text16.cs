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

        public GfxFont GetFont()
        {
            if ((_font == null) || (_font.ResourceId != _ports._curPort.fontId))
                _font = _cache.GetFont(_ports._curPort.fontId);

            return _font;
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

        public void StringWidth(string str, int orgFontId, out short textWidth, out short textHeight)
        {
            Width(str, 0, (short)str.Length, orgFontId, out textWidth, out textHeight, true);
        }

        private void Width(string text, int from, int length, int orgFontId, out short textWidth, out short textHeight, bool restoreFont)
        {
            throw new NotImplementedException();
            //    ushort curChar;
            //    int previousFontId = FontId;
            //    short previousPenColor = _ports._curPort.penClr;

            //    textWidth = 0; textHeight = 0;

            //    GetFont();
            //    if (_font)
            //    {
            //        text += from;
            //        while (len--)
            //        {
            //            curChar = (*(const byte*)text++);
            //            if (_font.isDoubleByte(curChar))
            //            {
            //                curChar |= (*(const byte*)text++) << 8;
            //                len--;
            //            }
            //            switch (curChar)
            //            {
            //                case 0x0A:
            //                case 0x0D:
            //                case 0x9781: // this one is used by SQ4/japanese as line break as well
            //                    textHeight = MAX<int16>(textHeight, _ports._curPort.fontHeight);
            //                    break;
            //                case 0x7C:
            //                    if (getSciVersion() >= SCI_VERSION_1_1)
            //                    {
            //                        len -= CodeProcessing(text, orgFontId, 0, false);
            //                        break;
            //                    }
            //                default:
            //                    textHeight = MAX<int16>(textHeight, _ports._curPort.fontHeight);
            //                    textWidth += _font.getCharWidth(curChar);
            //            }
            //        }
            //    }
            //    // When calculating size, we do not restore font because we need the current (code modified) font active
            //    //  If we are drawing this is called inbetween, so font needs to get restored
            //    //  If we are calculating size of just one fixed string (::StringWidth), then we need to restore
            //    if (restoreFont)
            //    {
            //        SetFont(previousFontId);
            //        _ports.penColor(previousPenColor);
            //    }
            //    return;
            //}
        }

        internal void DrawString(string textSplit)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// we need to have a separate status drawing code
        /// In KQ4 the IV char is actually 0xA, which would otherwise get considered as linebreak and not printed
        /// </summary>
        /// <param name="text"></param>
        public void DrawStatus(string text)
        {
            short charWidth;
            var textLen = text.Length;
            Rect rect;

            GetFont();
            if (_font == null)
                return;

            rect.Top = _ports._curPort.curTop;
            rect.Bottom = rect.Top + _ports._curPort.fontHeight;
            foreach (var curChar in text)
            {
                charWidth = _font.GetCharWidth(curChar);
                _font.Draw(curChar, (short)(_ports._curPort.top + _ports._curPort.curTop), (short)(_ports._curPort.left + _ports._curPort.curLeft), (byte)_ports._curPort.penClr, _ports._curPort.greyedOutput);
                _ports._curPort.curLeft += charWidth;
            }
        }
    }
}
