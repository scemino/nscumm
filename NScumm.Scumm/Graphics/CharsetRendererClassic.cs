/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;

namespace NScumm.Scumm.Graphics
{
    class CharsetRendererClassic : CharsetRendererCommon
    {
        protected int _offsX, _offsY;
        protected int _width, _height, _origWidth, _origHeight;
        protected int _charPos;

        public CharsetRendererClassic(ScummEngine vm)
            : base(vm)
        {
        }

        public override void PrintChar(int chr, bool ignoreCharsetMask)
        {
            VirtScreen vs;

            //ScummHelper.AssertRange(1, _curId, _vm._numCharsets - 1, "charset");

            if ((vs = Vm.FindVirtScreen(Top)) == null && (vs = Vm.FindVirtScreen(Top + GetFontHeight())) == null)
                return;

            if (chr == '@')
                return;

            Vm.CharsetColorMap[1] = Color;

            if (!PrepareDraw(chr))
                return;

            if (FirstChar)
            {
                Str.Left = 0;
                Str.Top = 0;
                Str.Right = 0;
                Str.Bottom = 0;
            }

            Top += _offsY;
            Left += _offsX;

            if (Left + _origWidth > Right + 1 || Left < 0)
            {
                Left += _origWidth;
                Top -= _offsY;
                return;
            }

            DisableOffsX = false;

            if (FirstChar)
            {
                Str.Left = Left;
                Str.Top = Top;
                Str.Right = Left;
                Str.Bottom = Top;
                FirstChar = false;
            }

            if (Left < Str.Left)
                Str.Left = Left;

            if (Top < Str.Top)
                Str.Top = Top;

            int drawTop = Top - vs.TopLine;

            Vm.MarkRectAsDirty(vs, Left, Left + _width, drawTop, drawTop + _height);

            // This check for kPlatformFMTowns and kMainVirtScreen is at least required for the chat with
            // the navigator's head in front of the ghost ship in Monkey Island 1
            if (!ignoreCharsetMask || (Vm.Game.Platform == Platform.FMTowns && vs == Vm.MainVirtScreen))
            {
                HasMask = true;
                TextScreen = vs;
            }

            PrintCharIntern(_fontPtr, _charPos, _origWidth, _origHeight, _width, _height, vs, ignoreCharsetMask);

            Left += _origWidth;

            if (Str.Right < Left)
            {
                Str.Right = Left;
                if (Vm.Game.Platform != Platform.FMTowns && _enableShadow)
                    Str.Right++;
            }

            if (Str.Bottom < Top + _origHeight)
                Str.Bottom = Top + _origHeight;

            Top -= _offsY;
        }

        public override void DrawChar(int chr, Surface s, int x, int y)
        {
            if (!PrepareDraw(chr))
                return;
            var pn = new PixelNavigator(s);
            pn.GoTo(x, y);
            DrawBitsN(s, pn, _fontPtr, _charPos, _fontPtr[_fontPos], y, _width, _height);
        }

        public override int GetCharWidth(int chr)
        {
            int spacing = 0;

//            if (_vm->_useCJKMode && chr >= 0x80)
//                return _vm->_2byteWidth / 2;

            int offs = _fontPtr.ToInt32(_fontPos + chr * 4 + 4);
            if (offs != 0)
                spacing = _fontPtr[_fontPos + offs] + (sbyte)_fontPtr[_fontPos + offs + 2];

            return spacing;
        }

        protected virtual void DrawBitsN(Surface s, PixelNavigator dst, System.Collections.Generic.IList<byte> src, int srcPos, byte bpp, int drawTop, int width, int height)
        {
            if (bpp != 1 && bpp != 2 && bpp != 4 && bpp != 8)
                throw new ArgumentException("Invalid bpp", "bpp");

            byte bits = src[srcPos++];
            byte numbits = 8;
            var cmap = Vm.CharsetColorMap;

            for (int y = 0; y < height && y + drawTop < s.Height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int color = (bits >> (8 - bpp)) & 0xFF;

                    if (color != 0 && (y + drawTop >= 0))
                    {
                        dst.Write(cmap[color]);
                    }
                    dst.OffsetX(1);
                    bits <<= bpp;
                    numbits -= bpp;
                    if (numbits == 0)
                    {
                        bits = src[srcPos++];
                        numbits = 8;
                    }
                }
                dst.Offset(-width, 1);
            }
        }

        protected virtual bool PrepareDraw(int chr)
        {
            var reader = new BinaryReader(new MemoryStream(_fontPtr));
            reader.BaseStream.Seek(_fontPos + chr * 4 + 4, SeekOrigin.Begin);
            int charOffs = reader.ReadInt32();
            //assert(charOffs < 0x14000);
            if (charOffs == 0)
                return false;
            _charPos = _fontPos + charOffs;

            reader.BaseStream.Seek(_charPos, SeekOrigin.Begin);
            _width = _origWidth = reader.ReadByte();
            _height = _origHeight = reader.ReadByte();

            if (DisableOffsX)
            {
                _offsX = 0;
                reader.ReadSByte();
            }
            else
            {
                _offsX = reader.ReadSByte();
            }

            _offsY = reader.ReadSByte();

            _charPos += 4;	// Skip over char header
            return true;
        }

        void PrintCharIntern(byte[] fontPtr, int charPos, int origWidth, int origHeight, int width, int height, VirtScreen vs, bool ignoreCharsetMask)
        {
            int drawTop = Top - vs.TopLine;

            PixelNavigator? back = null;
            PixelNavigator dstPtr;
            Surface dstSurface;
            if ((ignoreCharsetMask || !vs.HasTwoBuffers))
            {
                dstSurface = vs.Surfaces[0];
                dstPtr = new PixelNavigator(vs.Surfaces[0]);
                dstPtr.GoTo(vs.XStart + Left, drawTop);
            }
            else
            {
                dstSurface = Vm.TextSurface;
                dstPtr = new PixelNavigator(dstSurface);
                dstPtr.GoTo(Left, Top - Vm.ScreenTop);
            }

            if (BlitAlso && vs.HasTwoBuffers)
            {
                back = dstPtr;
                dstSurface = vs.Surfaces[0];
                dstPtr = new PixelNavigator(dstSurface);
                dstPtr.GoTo(vs.XStart + Left, drawTop);
            }

            if (!ignoreCharsetMask && vs.HasTwoBuffers)
            {
                drawTop = Top - Vm.ScreenTop;
            }

            DrawBitsN(dstSurface, dstPtr, fontPtr, charPos, fontPtr[_fontPos], drawTop, origWidth, origHeight);

            if (BlitAlso && vs.HasTwoBuffers)
            {
                // FIXME: Revisiting this code, I think the _blitAlso mode is likely broken
                // right now -- we are copying stuff from "dstPtr" to "back", but "dstPtr" really
                // only conatains charset data...
                // One way to fix this: don't copy etc.; rather simply render the char twice,
                // once to each of the two buffers. That should hypothetically yield
                // identical results, though I didn't try it and right now I don't know
                // any spots where I can test this...
                if (!ignoreCharsetMask)
                    throw new NotSupportedException("This might be broken -- please report where you encountered this to Fingolfin");

                // Perform some clipping
                int w = Math.Min(width, dstSurface.Width - Left);
                int h = Math.Min(height, dstSurface.Height - drawTop);
                if (Left < 0)
                {
                    w += Left;
                    back.Value.OffsetX(-Left);
                    dstPtr.OffsetX(-Left);
                }
                if (drawTop < 0)
                {
                    h += drawTop;
                    back.Value.OffsetY(-drawTop);
                    dstPtr.OffsetY(-drawTop);
                }

                // Blit the image data
                if (w > 0)
                {
                    while (h-- > 0)
                    {
                        for (int i = 0; i < w; i++)
                        {
                            back.Value.Write(dstPtr.Read());
                            back.Value.OffsetX(1);
                            dstPtr.OffsetX(1);
                        }
                        back.Value.Offset(-w, 1);
                        dstPtr.Offset(-w, 1);
                    }
                }

            }
        }
    }
}