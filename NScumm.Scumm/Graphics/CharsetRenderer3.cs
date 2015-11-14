//
//  CharsetRenderer3.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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
using System.IO;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Graphics
{
    public class CharsetRenderer3: CharsetRendererCommon
    {
        uint _charOffset;

        public CharsetRenderer3(ScummEngine vm)
            : base(vm)
        {
        }

        public override void SetCurID(int id)
        {
            if (id == -1)
                return;

            //ScummHelper.AssertRange(0, id, _vm.NumCharsets - 1, "charset");

            CurId = id;

            _fontPtr = Vm.ResourceManager.GetCharsetData((byte)id);
            if (_fontPtr == null)
                throw new NotSupportedException(string.Format("CharsetRendererCommon::setCurID: charset {0} not found", id));

            _bytesPerPixel = 1;
            NumChars = _fontPtr[4];
            _fontHeight = _fontPtr[5];
            _charOffset = NumChars + 6;
        }

        public override int GetFontHeight()
        {
//            if (Vm.UseCjkMode)
//                return Math.Max(Vm._2byteHeight + 1, _fontHeight);
//            else
            return _fontHeight;
        }

        public override int GetCharWidth(int chr)
        {
            int spacing = 0;

            //if (Vm.UseCjkMode && (chr & 0x80) != 0)
            //    spacing = Vm._2byteWidth / 2;

            if (spacing == 0)
                spacing = _fontPtr[6 + chr];

            return spacing;
        }

        protected virtual void EnableShadow(bool enable)
        {
            _shadowColor = 0;
            _shadowMode = enable;
        }

        protected virtual int GetDrawWidthIntern(int chr)
        {
            return GetCharWidth(chr);
        }

        protected virtual int GetDrawHeightIntern(int chr)
        {
            return 8;
        }

        public override void SetColor(byte color)
        {
            bool useShadow;
            Color = color;

            if (Vm.Game.Features.HasFlag(GameFeatures.Old256))
            {
                useShadow = ((Color & 0x80) != 0);
                Color &= 0x7f;
            }
            else
                useShadow = false;
               
            if (Vm.Game.Platform == Platform.FMTowns)
            {
                Color = (byte)((Color & 0x0f) | ((Color & 0x0f) << 4));
                if (Color == 0)
                    Color = 0x88;
            }

            EnableShadow(useShadow);

            //TranslateColor();
        }

        public override void PrintChar(int chr, bool ignoreCharsetMask)
        {
            // Indy3 / Zak256 / Loom
            int width, height, origWidth = 0, origHeight;
            VirtScreen vs;
            //const byte *charPtr;
            int is2byte = (chr >= 256 && Vm.UseCjkMode) ? 1 : 0;

            //assertRange(0, _curId, _vm->_numCharsets - 1, "charset");
            if ((vs = Vm.FindVirtScreen(Top)) == null)
                return;

            if (chr == '@')
                return;

            var charPtr = new BinaryReader(new MemoryStream(_fontPtr));
            charPtr.BaseStream.Seek(_charOffset + chr * 8, SeekOrigin.Begin);
            width = GetDrawWidthIntern(chr);
            height = GetDrawHeightIntern(chr);
            SetDrawCharIntern(chr);

            origWidth = width;
            origHeight = height;

            // Clip at the right side (to avoid drawing "outside" the screen bounds).
            if (Left + origWidth > Right + 1)
                return;

            if (_shadowMode)
            {
                width++;
                height++;
            }

            if (FirstChar)
            {
                Str.Left = Left;
                Str.Top = Top;
                Str.Right = Left;
                Str.Bottom = Top;
                FirstChar = false;
            }

            int drawTop = Top - vs.TopLine;

            Vm.MarkRectAsDirty(vs, Left, Left + width, drawTop, drawTop + height);

            if (!ignoreCharsetMask)
            {
                HasMask = true;
                TextScreen = vs;
            }

            if ((ignoreCharsetMask || !vs.HasTwoBuffers) && (Vm.Game.Platform != Platform.FMTowns))
                DrawBits1(vs.Surfaces[0], Left + vs.XStart, drawTop, charPtr, drawTop, origWidth, origHeight);
            else
                DrawBits1(Vm.TextSurface, Left, Top, charPtr, drawTop, origWidth, origHeight);

//    if (is2byte) {
//                origWidth /= Vm.TextSurfaceMultiplier;
//        height /= _vm->_textSurfaceMultiplier;
//    }

            if (Str.Left > Left)
                Str.Left = Left;

            Left += origWidth;

            if (Str.Right < Left)
            {
                Str.Right = Left;
                if (_shadowMode)
                    Str.Right++;
            }

            if (Str.Bottom < Top + height)
                Str.Bottom = Top + height;
        }

        public override void DrawChar(int chr, Surface s, int x, int y)
        {
            //const byte *charPtr = (Vm.UseCjkMode && chr > 127) ? Vm.Get2byteCharPtr(chr) : _fontPtr + chr * 8;
            var charPtr = new BinaryReader(new MemoryStream(_fontPtr));
            charPtr.BaseStream.Seek(_charOffset + chr * 8, SeekOrigin.Begin);
            var width = GetDrawWidthIntern(chr);
            var height = GetDrawHeightIntern(chr);
            SetDrawCharIntern(chr);
            DrawBits1(s, x, y, charPtr, y, width, height);
        }

        protected virtual void SetDrawCharIntern(int chr)
        {
        }

        protected virtual void DrawBits1(Surface surface, int x, int y, BinaryReader src, int drawTop, int width, int height)
        {
            var dst = new PixelNavigator(surface);
            dst.GoTo(x, y);
            
            byte bits = 0;
            byte col = Color;
            //int pitch = surface.Pitch - width * surface.BytesPerPixel;
            var dst2 = new PixelNavigator(dst);
            dst2.OffsetY(1);
            
            for (y = 0; y < height && y + drawTop < surface.Height; y++)
            {
                for (x = 0; x < width; x++)
                {
                    if ((x % 8) == 0)
                        bits = src.ReadByte();
                    if ((bits & ScummHelper.RevBitMask(x % 8)) != 0 && y + drawTop >= 0)
                    {
                        if (_shadowMode)
                        {
                            dst.OffsetX(1);
                            dst.Write(_shadowColor);
                            dst2.Write(_shadowColor);
                            dst2.OffsetX(1);
                            dst2.Write(_shadowColor);
                        }
                        dst.Write(col);
                    }
                    dst.OffsetX(1);
                    dst2.OffsetX(1);
                }
            
                dst.OffsetX(surface.Width - width);
                dst2.OffsetX(surface.Width - width);
            }
        }
    }
}

