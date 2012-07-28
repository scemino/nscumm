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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Scumm4
{
    public enum CompressionTypes
    {
        Uncompressed,
        Method1,
        Method2,
        Unknow
    }

    public enum RenderingDirections
    {
        Horizontal,
        Vertical,
        Unknow = 3
    }

    public class ImageDecoder
    {
        private Palette _palette;
        private IList<Strip> _strips;
        private int _currentLine;
        private int _currentColumn;
        private RenderingDirections _renderdingDirection;
        private int _numBitPerPaletteEntry;
        private int _substrationVariable;
        private BitStreamManager _bitStreamManager;
        private int _paletteIndex;
        private int _currentOffset;
        private int _start;
        private byte[] _pixels;
        private int _width;
        private int _height;
        private Point _pos;

        public ImageDecoder(byte[] pixels)
        {
            _pixels = pixels;
        }

        public void Decode(IList<Strip> strips, Palette palette, Point pos, int stripOffset, int screenWidth, int screenHeight, int imageHeight)
        {
            this._start = stripOffset;
            this._strips = strips;
            this._palette = palette;
            _pos = pos;
            _width = screenWidth;
            _height = screenHeight;
            _imageHeight = imageHeight;
            this.Decode();
        }

        private void Decode()
        {
            var count = _width / 8;
            for (int i = _start; i < _start + count && i < this._strips.Count; i++)
            {
                Strip stripData = this._strips[i];
                this._currentLine = 0;
                this._currentColumn = 0;
                var info = GetCompressionInformation(stripData);
                this._renderdingDirection = info.RenderdingDirection;
                this._currentOffset = i * 8;
                if (info.CompressionType == CompressionTypes.Method1 || info.CompressionType == CompressionTypes.Method2)
                {
                    this.DecodeCompressed(stripData);
                }
                else
                {
                    if (info.CompressionType == CompressionTypes.Uncompressed)
                    {
                        this.DecodeUncompressed(stripData);
                    }
                }
            }
        }

        private BitmapPalette ToPalette()
        {
            return new BitmapPalette(this._palette.Colors);
        }

        public class CompressionInformation
        {
            public CompressionTypes CompressionType { get; set; }
            public RenderingDirections RenderdingDirection { get; set; }
            public bool Transparent { get; set; }
            public int ParamSubtraction { get; set; }
        }

        private static CompressionInformation GetCompressionInformation(Strip strip)
        {
            CompressionInformation info = new CompressionInformation();
            if (strip.CodecId == 1)
            {
                info.CompressionType = CompressionTypes.Uncompressed;
                info.RenderdingDirection = RenderingDirections.Horizontal;
                info.Transparent = false;
                info.ParamSubtraction = -1;
            }
            else
            {
                if (strip.CodecId >= 14 && strip.CodecId <= 18)
                {
                    info.CompressionType = CompressionTypes.Method1;
                    info.RenderdingDirection = RenderingDirections.Vertical;
                    info.Transparent = false;
                    info.ParamSubtraction = 10;
                }
                else
                {
                    if (strip.CodecId >= 24 && strip.CodecId <= 28)
                    {
                        info.CompressionType = CompressionTypes.Method1;
                        info.RenderdingDirection = RenderingDirections.Horizontal;
                        info.Transparent = false;
                        info.ParamSubtraction = 20;
                    }
                    else
                    {
                        if (strip.CodecId >= 34 && strip.CodecId <= 38)
                        {
                            info.CompressionType = CompressionTypes.Method1;
                            info.RenderdingDirection = RenderingDirections.Vertical;
                            info.Transparent = true;
                            info.ParamSubtraction = 30;
                        }
                        else
                        {
                            if (strip.CodecId >= 44 && strip.CodecId <= 48)
                            {
                                info.CompressionType = CompressionTypes.Method1;
                                info.RenderdingDirection = RenderingDirections.Horizontal;
                                info.Transparent = true;
                                info.ParamSubtraction = 40;
                            }
                            else
                            {
                                if (strip.CodecId >= 64 && strip.CodecId <= 68)
                                {
                                    info.CompressionType = CompressionTypes.Method2;
                                    info.RenderdingDirection = RenderingDirections.Horizontal;
                                    info.Transparent = false;
                                    info.ParamSubtraction = 60;
                                }
                                else
                                {
                                    if (strip.CodecId >= 84 && strip.CodecId <= 88)
                                    {
                                        info.CompressionType = CompressionTypes.Method2;
                                        info.RenderdingDirection = RenderingDirections.Horizontal;
                                        info.Transparent = true;
                                        info.ParamSubtraction = 80;
                                    }
                                    else
                                    {
                                        if (strip.CodecId >= 104 && strip.CodecId <= 108)
                                        {
                                            info.CompressionType = CompressionTypes.Method2;
                                            info.RenderdingDirection = RenderingDirections.Horizontal;
                                            info.Transparent = false;
                                            info.ParamSubtraction = 100;
                                        }
                                        else
                                        {
                                            if (strip.CodecId >= 124 && strip.CodecId <= 128)
                                            {
                                                info.CompressionType = CompressionTypes.Method2;
                                                info.RenderdingDirection = RenderingDirections.Horizontal;
                                                info.Transparent = true;
                                                info.ParamSubtraction = 120;
                                            }
                                            else
                                            {
                                                info.CompressionType = CompressionTypes.Unknow;
                                                info.RenderdingDirection = RenderingDirections.Unknow;
                                                info.Transparent = false;
                                                info.ParamSubtraction = -2;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return info;
        }

        private void DecodeCompressed(Strip strip)
        {
            var info = GetCompressionInformation(strip);
            this._numBitPerPaletteEntry = (int)strip.CodecId - info.ParamSubtraction;
            this._substrationVariable = 1;
            this._bitStreamManager = new BitStreamManager(strip.Data);
            this._currentLine = 0;
            this._currentColumn = 0;
            bool flag = false;
            this._paletteIndex = (int)this._bitStreamManager.ReadByte();

            SetPixel(this._currentOffset + this._currentColumn, this._currentLine);

            while (!flag)
            {
                bool flag2 = this._bitStreamManager.ReadBit();
                if (flag2)
                {
                    if (info.CompressionType == CompressionTypes.Method1)
                    {
                        this.DecodeCode1Specifics();
                    }
                    else
                    {
                        if (info.CompressionType == CompressionTypes.Method2)
                        {
                            this.DecodeCode2Specifics();
                        }
                    }
                }
                else
                {
                    this.DrawNextPixel();
                }
                if ((this._currentColumn == 7 && this._currentLine == (int)(_imageHeight - 1)) || this._bitStreamManager.EndOfStream)
                {
                    flag = true;
                }
            }
        }

        private void SetPixel(int x, int y)
        {
            // Assign the color data to the pixel.
            var color = _palette.Colors[_paletteIndex];

            int l_x = _pos.x + x - (_start * 8);
            int l_y = _pos.y + y;
            if ((l_x >= 0) && (l_x < this._width) && (l_y < this._height) && (l_y >= 0))
            {
                var index = (l_x * 3) + (l_y * _width * 3);
                _pixels[index++] = color.R;
                _pixels[index++] = color.G;
                _pixels[index++] = color.B;
            }
        }

        private void DecodeCode1Specifics()
        {
            if (!this._bitStreamManager.ReadBit())
            {
                this._paletteIndex = (int)this._bitStreamManager.ReadValue(this._numBitPerPaletteEntry);
                this._substrationVariable = 1;
            }
            else
            {
                if (!this._bitStreamManager.ReadBit())
                {
                    this._paletteIndex -= this._substrationVariable;
                }
                else
                {
                    this._substrationVariable *= -1;
                    this._paletteIndex -= this._substrationVariable;
                }
            }
            this.DrawNextPixel();
        }
        private void DecodeCode2Specifics()
        {
            if (!this._bitStreamManager.ReadBit())
            {
                this._paletteIndex = (int)this._bitStreamManager.ReadValue(this._numBitPerPaletteEntry);
            }
            else
            {
                switch (this._bitStreamManager.ReadValue(3))
                {
                    case 0:
                        this._paletteIndex -= 4;
                        break;
                    case 1:
                        this._paletteIndex -= 3;
                        break;
                    case 2:
                        this._paletteIndex -= 2;
                        break;
                    case 3:
                        this._paletteIndex--;
                        break;
                    case 4:
                        {
                            byte b = this._bitStreamManager.ReadByte();
                            for (int i = 0; i < (int)b; i++)
                            {
                                this.DrawNextPixel();
                            }
                            return;
                        }
                    case 5:
                        this._paletteIndex++;
                        break;
                    case 6:
                        this._paletteIndex += 2;
                        break;
                    case 7:
                        this._paletteIndex += 3;
                        break;
                    default:
                        System.Diagnostics.Debugger.Break();
                        break;
                }
            }
            this.DrawNextPixel();
        }

        private void DecodeUncompressed(Strip strip)
        {
            this._bitStreamManager = new BitStreamManager(strip.Data);
            bool flag = false;
            this._paletteIndex = (int)this._bitStreamManager.ReadByte();

            SetPixel(this._currentOffset, 0);

            while (!flag)
            {
                this._paletteIndex = (int)this._bitStreamManager.ReadByte();
                this.DrawNextPixel();
                if ((this._currentColumn == 7 && this._currentLine == (int)(_imageHeight - 1)) || this._bitStreamManager.EndOfStream)
                {
                    flag = true;
                }
            }
        }

        private void DrawNextPixel()
        {
            if (this._renderdingDirection == RenderingDirections.Horizontal)
            {
                this._currentColumn++;
                if (this._currentColumn == 8)
                {
                    this._currentColumn = 0;
                    this._currentLine++;
                }
            }
            else
            {
                this._currentLine++;
                if (this._currentLine == _imageHeight)
                {
                    this._currentLine = 0;
                    this._currentColumn++;
                }
            }
            SetPixel(this._currentOffset + this._currentColumn, this._currentLine);
        }

        public int _imageHeight { get; set; }
    }
}
