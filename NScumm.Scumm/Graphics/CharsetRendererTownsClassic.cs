//
//  CharsetRendererTownsClassic.cs
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

using System.Collections.Generic;
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Graphics
{
    class CharsetRendererTownsClassic: CharsetRendererClassic
    {
        public CharsetRendererTownsClassic(ScummEngine engine)
            : base(engine)
        {
        }

        public override int GetCharWidth(int chr)
        {
            int spacing = 0;

            // TODO: useCJKMode
//            if (Vm._useCJKMode) {
//                if ((chr & 0xff00) == 0xfd00) {
//                    chr &= 0xff;
//                } else if (chr >= 256) {
//                    spacing = 8;
//                } else if (useFontRomCharacter(chr)) {
//                    spacing = 4;
//                }
//
//                if (spacing) {
//                    if (Vm._game.id == GID_MONKEY) {
//                        spacing++;
//                        if (_curId == 2)
//                            spacing++;
//                    } else if (Vm._game.id != GID_INDY4 && _curId == 1) {
//                        spacing++;
//                    }
//                }
//            }

            if (spacing == 0)
            {
                int offs = _fontPtr.ToInt32(_fontPos + chr * 4 + 4);
                if (offs != 0)
                    spacing = _fontPtr[_fontPos + offs] + (sbyte)_fontPtr[_fontPos + offs + 2];
            }

            return spacing;
        }

        public override int GetFontHeight()
        {
            var htbl = (Vm.Game.GameId == GameId.Monkey1) ? sjisFontHeightM1 : ((Vm.Game.GameId == GameId.Indy4) ? sjisFontHeightI4 : sjisFontHeightM2);
            return /*Vm._useCJKMode ? htbl[_curId] :*/ _fontHeight;
        }

        protected override void DrawBitsN(Surface s, PixelNavigator dst, IList<byte> src, int srcPos, byte bpp, int drawTop, int width, int height)
        {
//            if (_sjisCurChar)
//            {
//                assert(Vm._cjkFont);
//                Vm._cjkFont.drawChar(Vm._textSurface, _sjisCurChar, _left * Vm._textSurfaceMultiplier, (_top - Vm._screenTop) * Vm._textSurfaceMultiplier, Vm._townsCharsetColorMap[1], _shadowColor);
//                return;
//            }

            bool scale2x = (Vm.TextSurfaceMultiplier == 2);
            dst = new PixelNavigator(Vm.TextSurface);
            dst.GoTo(Left * Vm.TextSurfaceMultiplier, (Top - Vm.ScreenTop) * Vm.TextSurfaceMultiplier);

            int y, x;
            int color;
            byte numbits, bits;

            int pitch = Vm.TextSurface.Pitch - width;

            Debug.Assert(bpp == 1 || bpp == 2 || bpp == 4 || bpp == 8);
            bits = src[srcPos++];
            numbits = 8;
            var cmap = Vm.CharsetColorMap;
            var dst2 = new PixelNavigator(dst);

            if (Vm.Game.Platform == Platform.FMTowns)
                cmap = Vm.TownsCharsetColorMap;
            if (scale2x)
            {
                dst2.OffsetY(1);
                pitch <<= 1;
            }

            for (y = 0; y < height && y + drawTop < Vm.TextSurface.Height; y++)
            {
                for (x = 0; x < width; x++)
                {
                    color = (bits >> (8 - bpp)) & 0xFF;
                    if (color != 0 && y + drawTop >= 0)
                    {
                        dst.Write(cmap[color]);
                        if (scale2x)
                        {
                            dst.Write(1, dst.Read());
                            dst2.Write(dst.Read());
                            dst2.Write(1, dst.Read());
                        }
                    }
                    dst.OffsetX(1);

                    if (scale2x)
                    {
                        dst.OffsetX(1);
                        dst2.OffsetX(2);
                    }

                    bits <<= bpp;
                    numbits -= bpp;
                    if (numbits == 0)
                    {
                        bits = src[srcPos++];
                        numbits = 8;
                    }
                }
                dst.OffsetX(pitch / dst.BytesByPixel);
                dst2.OffsetX(pitch / dst2.BytesByPixel);
            }
        }

        protected override bool PrepareDraw(int chr)
        {
            ProcessCharsetColors();
            bool noSjis = false;

            // TODO: useCJKMode
//            if (Vm.Game.Platform == Platform.FMTowns && Vm._useCJKMode)
//            {
//                if ((chr & 0x00ff) == 0x00fd)
//                {
//                    chr >>= 8;
//                    noSjis = true;
//                }
//            }

            if (UseFontRomCharacter(chr) && !noSjis)
            {
                SetupShadowMode();
                _charPos = 0;
                _sjisCurChar = (ushort)chr;

                _width = GetCharWidth(chr);
                // For whatever reason MI1 uses a different font width
                // for alignment calculation and for drawing when
                // charset 2 is active. This fixes some subtle glitches.
                if (Vm.Game.GameId == GameId.Monkey1 && CurId == 2)
                    _width--;
                _origWidth = _width;

                _origHeight = _height = GetFontHeight();
                _offsX = _offsY = 0;
            }
//            else if (Vm._useCJKMode && (chr >= 128) && !noSjis)
//            {
//                setupShadowMode();
//                _origWidth = _width = _vm._2byteWidth;
//                _origHeight = _height = _vm._2byteHeight;
//                _charPtr = _vm.get2byteCharPtr(chr);
//                _offsX = _offsY = 0;
//                if (_enableShadow)
//                {
//                    _width++;
//                    _height++;
//                }
//            }
            else
            {
                _sjisCurChar = 0;
                return base.PrepareDraw(chr);
            }
            return true;
        }

        void ProcessCharsetColors()
        {
            for (var i = 0; i < (1 << _bytesPerPixel); i++)
            {
                var c = Vm.CharsetColorMap[i];

                if (c > 16)
                {
                    var t = (Vm.CurrentPalette.Colors[c].R < 32) ? 4 : 12;
                    t |= ((Vm.CurrentPalette.Colors[c].G < 32) ? 2 : 10);
                    t |= ((Vm.CurrentPalette.Colors[c].B < 32) ? 1 : 9);
                    c = (byte)t;
                }

                if (c == 0)
                    c = Vm.TownsOverrideShadowColor;

                c = (byte)(((c & 0x0f) << 4) | (c & 0x0f));
                Vm.TownsCharsetColorMap[i] = c;
            }
        }

        bool UseFontRomCharacter(int chr)
        {
            // TODO: _useCJKMode
//            if (!Vm._useCJKMode)
            return false;

            // Some SCUMM 5 games contain hard coded logic to determine whether to use
            // the SCUMM fonts or the FM-Towns font rom to draw a character. For the other
            // games we will simply check for a character greater 127.
            //if (chr < 128)
            //{
            //    if (((Vm.Game.GameId == GameId.Monkey2 && CurId != 0) || (Vm.Game.GameId == GameId.Indy4 && CurId != 3)) && (chr > 31 && chr != 94 && chr != 95 && chr != 126 && chr != 127))
            //        return true;
            //    return false;
            //}
            //return true;
        }

        void SetupShadowMode()
        {
//            _enableShadow = true;
//            _shadowColor = Vm.TownsCharsetColorMap[0];
//            Debug.Assert(Vm._cjkFont);
//
//            if (((Vm.Game.GameId == GameId.Monkey1) && (CurId == 2 || CurId == 4 || CurId == 6)) ||
//                ((Vm.Game.GameId == GameId.Monkey2) && (CurId != 1 && CurId != 5 && CurId != 9)) ||
//                ((Vm.Game.GameId == GameId.Indy4) && (CurId == 2 || CurId == 3 || CurId == 4))) {
//                Vm._cjkFont.SetDrawingMode(Graphics::FontSJIS::kOutlineMode);
//            } else {
//                Vm._cjkFont.SetDrawingMode(Graphics::FontSJIS::kDefaultMode);
//            }
//
//            Vm._cjkFont.ToggleFlippedMode((Vm.Game.GameId == GameId.Monkey1 || Vm.Game.GameId == GameId.Monkey2) && CurId == 3);
        }

        ushort _sjisCurChar;

        static readonly byte[] sjisFontHeightM1 = { 0, 8, 9, 8, 9, 8, 9, 0, 0, 0 };
        static readonly byte[] sjisFontHeightM2 = { 0, 8, 9, 9, 9, 8, 9, 9, 9, 8 };
        static readonly byte[] sjisFontHeightI4 = { 0, 8, 9, 9, 9, 8, 8, 8, 8, 8 };
    }
}