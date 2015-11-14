//
//  CharsetRendererNut.cs
//
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

using System.Diagnostics;
using NScumm.Core.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Graphics
{
    public class CharsetRendererNut: CharsetRenderer
    {
        public CharsetRendererNut(ScummEngine vm)
            : base(vm)
        {
        }


        #region implemented abstract members of CharsetRenderer

        public override void DrawChar(int chr, Surface s, int x, int y)
        {
        }

        public override void PrintChar(int chr, bool ignoreCharsetMask)
        {
            Rect shadow;

            Debug.Assert(_current != null);
            if (chr == '@')
                return;

            shadow.Left = Left;
            shadow.Top = Top;

            if (FirstChar)
            {
                Str.Left = (shadow.Left >= 0) ? shadow.Left : 0;
                Str.Top = (shadow.Top >= 0) ? shadow.Top : 0;
                Str.Right = Str.Left;
                Str.Bottom = Str.Top;
                FirstChar = false;
            }

            int width = _current.GetCharWidth((char)chr);
            int height = _current.GetCharHeight((char)chr);

//            bool is2byte = chr >= 256 && _vm._useCJKMode;
//            if (is2byte)
//                width = _vm._2byteWidth;

            shadow.Right = Left + width;
            shadow.Bottom = Top + height;

            Surface s;
            if (!ignoreCharsetMask)
            {
                HasMask = true;
                TextScreen = Vm.MainVirtScreen;
            }

            int offsetX = 0;
            int drawTop = Top;
            if (ignoreCharsetMask)
            {
                VirtScreen vs = Vm.MainVirtScreen;
                s = vs.Surfaces[0];
                offsetX = vs.XStart;
            }
            else
            {
                s = Vm.TextSurface;
                drawTop -= Vm.ScreenTop;
            }

            var dst = new PixelNavigator(s);
            dst.GoTo(Left + offsetX, drawTop);

//            if (chr >= 256 && _vm._useCJKMode)
//                _current.draw2byte(s, chr, _left, drawTop, _color);
//            else
            _current.DrawChar(dst, (char)chr, Left, drawTop, Color);
            Vm.MarkRectAsDirty(Vm.MainVirtScreen, shadow);

            if (Str.Left > Left)
                Str.Left = Left;

            // Original keeps glyph width and character dimensions separately
//            if ((_vm._language == Common::ZH_TWN || _vm._language == Common::KO_KOR) && is2byte)
//                width++;

            Left += width;

            if (Str.Right < shadow.Right)
                Str.Right = shadow.Right;

            if (Str.Bottom < shadow.Bottom)
                Str.Bottom = shadow.Bottom;
        }

        public override void SetCurID(int id)
        {
            if (id == -1)
                return;

            int numFonts = ((Vm.Game.GameId == GameId.CurseOfMonkeyIsland) && (Vm.Game.Features.HasFlag(GameFeatures.Demo))) ? 4 : 5;
            Debug.Assert(id < numFonts);
            CurId = id;
            if (_fr[id] == null)
            {
                var fontname = string.Format("font{0}.nut", id);
                _fr[id] = new NutRenderer(Vm, fontname);
            }
            _current = _fr[id];
        }

        public override int GetFontHeight()
        {
            // FIXME / TODO: how to implement this properly???
            Debug.Assert(_current != null);
            return _current.GetCharHeight('|');
        }

        public override int GetCharWidth(int chr)
        {
            Debug.Assert(_current != null);
            return _current.GetCharWidth((char)chr);
        }

        #endregion

        NutRenderer _current;
        NutRenderer[] _fr = new NutRenderer[5];
    }
}

