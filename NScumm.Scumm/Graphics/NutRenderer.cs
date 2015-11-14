//
//  NutRenderer.cs
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

using System;
using System.Diagnostics;
using System.IO;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Scumm.Smush;

namespace NScumm.Scumm.Graphics
{
    public class NutRenderer
    {
        public NutRenderer(ScummEngine vm, string filename)
        {
            _vm = vm;
            var directory = ServiceLocator.FileStorage.GetDirectoryName(_vm.Game.Path);
            var path = ScummHelper.LocatePath(directory, filename);
            LoadFont(path);
        }

        public int NumChars { get { return _numChars; } }

        public void DrawFrame(byte[] dst, int c, int x, int y)
        {
            var width = Math.Min((int)_chars[c].Width, _vm.ScreenWidth - x);
            var height = Math.Min((int)_chars[c].Height, _vm.ScreenHeight - y);
            var src = UnpackChar((char)c);
            var srcPitch = _chars[c].Width;

            var minX = x < 0 ? -x : 0;
            var minY = y < 0 ? -y : 0;

            if (height <= 0 || width <= 0)
            {
                return;
            }

            var srcPos = 0;
            var dstPos = y * _vm.ScreenWidth + x;

            if (minY != 0)
            {
                srcPos += minY * srcPitch;
                dstPos += minY * _vm.ScreenWidth;
            }

            byte bits = 0;
            for (int ty = minY; ty < height; ty++)
            {
                for (int tx = minX; tx < width; tx++)
                {
                    bits = src[tx + srcPos];
                    if (bits != 231 && bits != 0)
                    {
                        dst[tx + dstPos] = bits;
                    }
                }
                srcPos += srcPitch;
                dstPos += _vm.ScreenWidth;
            }
        }

        public void DrawChar(PixelNavigator dst, char c, int x, int y, byte color)
        {
            var width = Math.Min((int)_chars[c].Width, dst.Width - x);
            var height = Math.Min((int)_chars[c].Height, dst.Height - y);
            var src = UnpackChar(c);
            var srcPitch = _chars[c].Width;
            var srcPos = 0;

            var minX = x < 0 ? -x : 0;
            var minY = y < 0 ? -y : 0;

            if (height <= 0 || width <= 0)
            {
                return;
            }

            if (minY != 0)
            {
                srcPos += minY * srcPitch;
                dst.OffsetY(minY);
            }

            for (int ty = minY; ty < height; ty++)
            {
                for (int tx = minX; tx < width; tx++)
                {
                    if (src[srcPos + tx] != _chars[c].Transparency)
                    {
                        if (src[srcPos + tx] == 1)
                        {
                            dst.Write(tx, color);
                        }
                        else
                        {
                            dst.Write(tx, src[srcPos + tx]);
                        }
                    }
                }
                srcPos += srcPitch;
                dst.OffsetY(1);
            }
        }

        public void Draw2byte(Surface s, int c, int x, int y, byte color)
        {
            throw new NotImplementedException();
            // FIXME: This gets passed a const destination Surface. Intuitively this
            // should never get written to. But sadly it does... For now we simply
            // cast the const qualifier away.
//            var dstNav = new PixelNavigator(s);
//            dstNav.Offset(x, y);
//            var width = _vm._2byteWidth;
//            var height = Math.Min(_vm._2byteHeight, s.h - y);
//            var src = _vm.Get2byteCharPtr(c);
//            byte bits = 0;
//
//            if (height <= 0 || width <= 0)
//            {
//                return;
//            }
//
//            for (int ty = 0; ty < height; ty++)
//            {
//                for (int tx = 0; tx < width; tx++)
//                {
//                    if ((tx & 7) == 0)
//                        bits = *src++;
//                    if (x + tx < 0 || x + tx >= s.w || y + ty < 0)
//                        continue;
//                    if (bits & ScummHelper.RevBitMask(tx % 8))
//                    {
//                        dst[tx] = color;
//                    }
//                }
//                dst += s.Pitch;
//            }
        }

        public int GetCharWidth(char c)
        {
//            if (c >= 0x80 && _vm._useCJKMode)
//                return _vm._2byteWidth / 2;

            if (c >= _numChars)
                throw new InvalidOperationException(string.Format("invalid character in NutRenderer::getCharWidth : {0} ({1})", c, _numChars));

            return _chars[c].Width;
        }

        public int GetCharHeight(char c)
        {
//            if (c >= 0x80 && _vm->_useCJKMode)
//                return _vm->_2byteHeight;

            if (c >= _numChars)
                throw new InvalidOperationException(string.Format("invalid character in NutRenderer::getCharHeight : {0} ({1})", c, _numChars));

            return _chars[c].Height;
        }

        void LoadFont(string filename)
        {
            byte[] dataSrc;
            using (var file = ServiceLocator.FileStorage.OpenFileRead(filename))
            {
                var reader = new BinaryReader(file);
                var tag = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(4));
                if (tag != "ANIM")
                {
                    throw new InvalidOperationException("NutRenderer.LoadFont() there is no ANIM chunk in font header");
                }

                var length = (int)reader.ReadUInt32BigEndian();
                dataSrc = reader.ReadBytes(length);
            }

            if (System.Text.Encoding.UTF8.GetString(dataSrc, 0, 4) != "AHDR")
            {
                throw new InvalidOperationException("NutRenderer::loadFont() there is no AHDR chunk in font header");
            }

            // We pre-decode the font, which may seem wasteful at first. Actually,
            // the memory needed for just the decoded glyphs is smaller than the
            // whole of the undecoded font file.


            _numChars = BitConverter.ToUInt16(dataSrc, 10);
            Debug.Assert(_numChars <= _chars.Length);

            _paletteMap = new byte[256];

            var offset = 0;
            var decodedLength = 0;
            for (var l = 0; l < _numChars; l++)
            {
                offset += (int)ScummHelper.SwapBytes(BitConverter.ToUInt32(dataSrc, offset + 4)) + 16;
                int width = BitConverter.ToUInt16(dataSrc, offset + 14);
                int height = BitConverter.ToUInt16(dataSrc, offset + 16);
                int size = width * height;
                decodedLength += size;
                if (size > _maxCharSize)
                    _maxCharSize = size;
            }

            Debug.WriteLine("NutRenderer::loadFont('{0}') - decodedLength = {1}", filename, decodedLength);

            _decodedData = new byte[decodedLength];
            var decodedPos = 0;

            offset = 0;
            for (var l = 0; l < _numChars; l++)
            {
                offset += (int)ScummHelper.SwapBytes(BitConverter.ToUInt32(dataSrc, offset + 4)) + 8;
                if (System.Text.Encoding.UTF8.GetString(dataSrc, offset, 4) != "FRME")
                {
                    throw new InvalidOperationException(string.Format("NutRenderer::loadFont({0}) there is no FRME chunk {1} (offset {2:X})", filename, l, offset));
                }
                offset += 8;
                if (System.Text.Encoding.UTF8.GetString(dataSrc, offset, 4) != "FOBJ")
                {
                    throw new InvalidOperationException(string.Format("NutRenderer::loadFont({0}) there is no FOBJ chunk in FRME chunk {1} (offset {2:X})", filename, l, offset));
                }
                int codec = BitConverter.ToUInt16(dataSrc, offset + 8);
                // _chars[l].xoffs = READ_LE_UINT16(dataSrc + offset + 10);
                // _chars[l].yoffs = READ_LE_UINT16(dataSrc + offset + 12);
                _chars[l].Width = BitConverter.ToUInt16(dataSrc, offset + 14);
                _chars[l].Height = BitConverter.ToUInt16(dataSrc, offset + 16);
                _chars[l].Src = _decodedData;
                _chars[l].SrcOffset = decodedPos;

                decodedPos += (_chars[l].Width * _chars[l].Height);

                // If characters have transparency, then bytes just get skipped and
                // so there may appear some garbage. That's why we have to fill it
                // with a default color first.
                if (codec == 44)
                {
                    for (int i = 0; i < _chars[l].Width * _chars[l].Height; i++)
                    {
                        _chars[l].Src[_chars[l].SrcOffset + i] = Smush44TransparentColor;
                    }
                    _paletteMap[Smush44TransparentColor] = 1;
                    _chars[l].Transparency = Smush44TransparentColor;
                }
                else
                {
                    for (int i = 0; i < _chars[l].Width * _chars[l].Height; i++)
                    {
                        _chars[l].Src[_chars[l].SrcOffset + i] = DefaultTransparentColor;
                    }
                    _paletteMap[DefaultTransparentColor] = 1;
                    _chars[l].Transparency = DefaultTransparentColor;
                }

                switch (codec)
                {
                    case 1:
                        Codec1(_chars[l].Src, _chars[l].SrcOffset, dataSrc, offset + 22, _chars[l].Width, _chars[l].Height, _chars[l].Width);
                        break;
                    case 21:
                    case 44:
                        Codec21(_chars[l].Src, _chars[l].SrcOffset, dataSrc, offset + 22, _chars[l].Width, _chars[l].Height, _chars[l].Width);
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("NutRenderer::loadFont: unknown codec: {0}", codec));
                }
            }

            // We have decoded the font. Now let's see if we can re-compress it to
            // a more compact format. Start by counting the number of colors.

            int numColors = 0;
            for (var l = 0; l < 256; l++)
            {
                if (_paletteMap[l] != 0)
                {
                    if (numColors < _palette.Length)
                    {
                        _paletteMap[l] = (byte)numColors;
                        _palette[numColors] = (byte)l;
                    }
                    numColors++;
                }
            }

            // Now _palette contains all the used colors, and _paletteMap maps the
            // real color to the palette index.

            if (numColors <= 2)
                _bpp = 1;
            else if (numColors <= 4)
                _bpp = 2;
            else if (numColors <= 16)
                _bpp = 4;
            else
                _bpp = 8;

            if (_bpp < 8)
            {
                int compressedLength = 0;
                for (var l = 0; l < 256; l++)
                {
                    compressedLength += (((_bpp * _chars[l].Width + 7) / 8) * _chars[l].Height);
                }

                Debug.WriteLine("NutRenderer::loadFont('{0}') - compressedLength = {1} ({2} bpp)", filename, compressedLength, _bpp);

                var compressedData = new byte[compressedLength];

                offset = 0;

                for (var l = 0; l < 256; l++)
                {
                    var src = _chars[l].Src;
                    var srcPos = _chars[l].SrcOffset;
                    var dstPos = offset;
                    int srcPitch = _chars[l].Width;
                    int dstPitch = (_bpp * _chars[l].Width + 7) / 8;

                    for (var h = 0; h < _chars[l].Height; h++)
                    {
                        byte bit = 0x80;
                        var nextDst = dstPos + dstPitch;
                        for (int w = 0; w < srcPitch; w++)
                        {
                            byte color = _paletteMap[src[srcPos + w]];
                            for (int i = 0; i < _bpp; i++)
                            {
                                if ((color & (1 << i)) != 0)
                                    compressedData[dstPos] |= bit;
                                bit >>= 1;
                            }
                            if (bit == 0)
                            {
                                bit = 0x80;
                                dstPos++;
                            }
                        }
                        srcPos += srcPitch;
                        dstPos = nextDst;
                    }
                    _chars[l].Src = compressedData;
                    _chars[l].SrcOffset = offset;
                    offset += (dstPitch * _chars[l].Height);
                }

                _decodedData = compressedData;

                _charBuffer = new byte[_maxCharSize];
            }

            _paletteMap = null;
        }

        protected byte[] UnpackChar(char c)
        {
            if (_bpp == 8)
            {
                var tmp = new byte[_chars[c].Src.Length - _chars[c].SrcOffset];
                Array.Copy(_chars[c].Src, _chars[c].SrcOffset, tmp, 0, tmp.Length);
                return tmp;
            }

            var src = _chars[c].Src;
            var srcPos = _chars[c].SrcOffset;
            int pitch = (_bpp * _chars[c].Width + 7) / 8;

            for (int ty = 0; ty < _chars[c].Height; ty++)
            {
                for (int tx = 0; tx < _chars[c].Width; tx++)
                {
                    byte val;
                    int offset;
                    byte bit;

                    switch (_bpp)
                    {
                        case 1:
                            offset = tx / 8;
                            bit = (byte)(0x80 >> (tx % 8));
                            break;
                        case 2:
                            offset = tx / 4;
                            bit = (byte)(0x80 >> (2 * (tx % 4)));
                            break;
                        default:
                            offset = tx / 2;
                            bit = (byte)(0x80 >> (4 * (tx % 2)));
                            break;
                    }

                    val = 0;

                    for (int i = 0; i < _bpp; i++)
                    {
                        if ((src[srcPos + offset] & (bit >> i)) != 0)
                            val |= (byte)(1 << i);
                    }

                    _charBuffer[ty * _chars[c].Width + tx] = _palette[val];
                }
                srcPos += pitch;
            }

            return _charBuffer;
        }

        void Codec1(byte[] dst, int dstPos, byte[] src, int srcfOffset, int width, int height, int pitch)
        {
            SmushPlayer.SmushDecodeCodec1(dst, dstPos, src, srcfOffset, 0, 0, width, height, pitch);
            for (var i = 0; i < width * height; i++)
                _paletteMap[dst[dstPos + i]] = 1;
        }

        void Codec21(byte[] dst, int dstPos, byte[] src, int srcPos, int width, int height, int pitch)
        {
            while ((height--) != 0)
            {
                var dstPtrNext = dstPos + pitch;
                var srcPtrNext = srcPos + 2 + BitConverter.ToUInt16(src, srcPos);
                srcPos += 2;
                int len = width;
                do
                {
                    int offs = BitConverter.ToUInt16(src, srcPos);
                    srcPos += 2;
                    dstPos += offs;
                    len -= offs;
                    if (len <= 0)
                    {
                        break;
                    }
                    int w = BitConverter.ToUInt16(src, srcPos) + 1;
                    srcPos += 2;
                    len -= w;
                    if (len < 0)
                    {
                        w += len;
                    }
                    // the original codec44 handles this part slightly differently (this is the only difference with codec21) :
                    //  src bytes equal to 255 are replaced by 0 in dst
                    //  src bytes equal to 1 are replaced by a color passed as an argument in the original function
                    //  other src bytes values are copied as-is
                    for (int i = 0; i < w; i++)
                    {
                        _paletteMap[src[srcPos + i]] = 1;
                    }
                    Array.Copy(src, srcPos, dst, dstPos, w);
                    dstPos += w;
                    srcPos += w;
                } while (len > 0);
                dstPos = dstPtrNext;
                srcPos = srcPtrNext;
            }
        }

        protected ScummEngine _vm;
        protected int _numChars;
        protected int _maxCharSize;
        protected byte[] _charBuffer;
        protected byte[] _decodedData;
        protected byte[] _paletteMap;
        protected byte _bpp;
        protected byte[] _palette = new byte[16];

        const int DefaultTransparentColor = 0;
        const int Smush44TransparentColor = 2;

        protected struct NutChar
        {
            public ushort Width;
            public ushort Height;
            public byte[] Src;
            public int SrcOffset;
            public byte Transparency;
        }

        protected NutChar[] _chars = new NutChar[256];
    }
}

