//
//  CharsetRendererTowns3.cs
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

using System.IO;
using NScumm.Core;
using NScumm.Core.Graphics;

namespace NScumm.Scumm.Graphics
{
    public class CharsetRendererTowns3: CharsetRenderer3
    {
        public CharsetRendererTowns3(ScummEngine vm)
            : base(vm)
        {
        }

        public override int GetCharWidth(int chr)
        {
            int spacing = 0;

//            if (Vm._useCJKMode)
//            {
//                if (chr >= 256)
//                    spacing = 8;
//                else if (chr >= 128)
//                    spacing = 4;
//            }

            if (spacing == 0)
                spacing = _fontPtr[6 + chr];

            return spacing;
        }

        public override int GetFontHeight()
        {
            return /*Vm._useCJKMode ? 8 :*/ _fontHeight;
        }

        protected override void EnableShadow(bool enable)
        {
            _shadowColor = 8;
            _enableShadow = enable;

            _shadowColor = 0x88;
//    if (Vm._cjkFont)
//        Vm._cjkFont.SetDrawingMode(enable ? Graphics::FontSJIS::kFMTownsShadowMode : Graphics::FontSJIS::kDefaultMode);
        }

        protected override void DrawBits1(Surface dest, int x, int y, BinaryReader src, int drawTop, int width, int height)
        {
            if (_sjisCurChar != 0)
            {
//                Debug.Assert(Vm._cjkFont);
//                Vm._cjkFont.drawChar(dest, _sjisCurChar, x, y, _color, _shadowColor);
                return;
            }
            bool scale2x = ((dest == Vm.TextSurface) && (Vm.TextSurfaceMultiplier == 2) /*&& !(_sjisCurChar >= 256 && Vm.UseCJKMode)*/);

            byte bits = 0;
            var col = Color;
            var bpp = Surface.GetBytesPerPixel(dest.PixelFormat);
            int pitch = dest.Pitch - width * bpp;
            var dst = new PixelNavigator(dest);
            dst.GoTo(x, y);
            var dst2 = new PixelNavigator(dst);
            dst2.OffsetY(1);

            var dst3 = new PixelNavigator(dst2);
            var dst4 = new PixelNavigator(dst2);
            if (scale2x)
            {
                dst3.OffsetY(1);
                dst4.OffsetY(1);
                pitch <<= 1;
            }

            for (y = 0; y < height && y + drawTop < dest.Height; y++)
            {
                for (x = 0; x < width; x++)
                {
                    if ((x % 8) == 0)
                        bits = src.ReadByte();
                    if (((bits & ScummHelper.RevBitMask(x % 8)) != 0) && y + drawTop >= 0)
                    {
                        if (bpp == 2)
                        {
                            if (_enableShadow)
                            {
                                dst.WriteUInt16(2, Vm._16BitPalette[_shadowColor]);
                                dst.WriteUInt16(2 + dest.Pitch, Vm._16BitPalette[_shadowColor]);
                            }
                            dst.WriteUInt16(Vm._16BitPalette[Color]);
                        }
                        else
                        {
                            if (_enableShadow)
                            {
                                if (scale2x)
                                {
                                    dst.Write(2, _shadowColor);
                                    dst.Write(3, _shadowColor);
                                    dst2.Write(2, _shadowColor);
                                    dst2.Write(3, _shadowColor);

                                    dst3.Write(0, _shadowColor);
                                    dst3.Write(1, _shadowColor);
                                    dst4.Write(0, _shadowColor);
                                    dst4.Write(0, _shadowColor);
                                }
                                else
                                {
                                    dst2.Write(_shadowColor);
                                    dst.Write(1, _shadowColor);
                                }
                            }
                            dst.Write(col);

                            if (scale2x)
                            {
                                dst.Write(1, col);
                                dst2.Write(0, col);
                                dst2.Write(1, col);
                            }
                        }
                    }
                    dst.OffsetX(1);
                    dst2.OffsetX(1);
                    if (scale2x)
                    {
                        dst.OffsetX(1);
                        dst2.OffsetX(1);
                        dst3.OffsetX(1);
                        dst4.OffsetX(1);
                    }
                }

                dst.OffsetX(pitch / dst.BytesByPixel);
                dst2.OffsetX(pitch / dst2.BytesByPixel);
                dst3.OffsetX(pitch / dst3.BytesByPixel);
                dst4.OffsetX(pitch / dst4.BytesByPixel);
            }
        }

        protected override int GetDrawWidthIntern(int chr)
        {
//            if (Vm._useCJKMode && chr > 127)
//            {
//                assert(Vm._cjkFont);
//                return Vm._cjkFont.getCharWidth(chr);
//            }
            return base.GetDrawWidthIntern(chr);
        }

        protected override int GetDrawHeightIntern(int chr)
        {
//            if (Vm._useCJKMode && chr > 127)
//            {
//                assert(Vm._cjkFont);
//                return Vm._cjkFont.getFontHeight();
//            }
            return base.GetDrawHeightIntern(chr);
        }

        protected override void SetDrawCharIntern(int chr)
        {
            _sjisCurChar = /*(Vm.UseCJKMode && chr > 127) ? chr : */0;
        }

        ushort _sjisCurChar;
    }
}

