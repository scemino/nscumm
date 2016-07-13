//
//  GfxManager.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core;
using NScumm.Core.Graphics;
using OpenTK;

namespace NScumm.Droid.Services
{
    internal class GfxManager : IGraphicsManager
    {
        private EditableSurfaceView _view;
        private Color[] _palColor;
        private Vector2 _hotspot;

        public GfxManager(EditableSurfaceView view, Rect size)
        {
            _view = view;
            Bounds = size;
            Pixels = new byte[size.Width * size.Height];
            _view._color = new byte[size.Width * size.Height * 3];
            _view._width = size.Width;
            _view._height = size.Height;
            _palColor = new Color[256];
        }

        public Rect Bounds
        {
            get;
        }

        public bool IsCursorVisible
        {
            get; set;
        }

        public PixelFormat PixelFormat
        {
            get; set;
        }

        public byte[] Pixels
        {
            get; private set;
        }

        public int ShakePosition
        {
            get;
            set;
        }

        public Surface Capture()
        {
            throw new NotImplementedException();
        }

        public void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int width, int height)
        {
            CopyRectToScreen(buffer, 0, sourceStride, x, y, width, height);
        }

        public void CopyRectToScreen(byte[] buffer, int startOffset, int sourceStride, int x, int y, int width, int height)
        {
            var w = Bounds.Width;
            for (int h = 0; h < height; h++)
            {
                Array.Copy(buffer, startOffset + h * sourceStride, Pixels, x + (y + h) * w, width);
            }
        }

        public void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int dstX, int dstY, int width, int height)
        {
            var w = Bounds.Width;
            for (int h = 0; h < height; h++)
            {
                Array.Copy(buffer, x + (h + y) * sourceStride, Pixels, dstX + (dstY + h) * w, width);
            }
        }

        public void FillScreen(int color)
        {
            Pixels.Set(0, (byte)color, Bounds.Width * Bounds.Height);
        }

        public Color[] GetPalette()
        {
            return _palColor;
        }

        public void SetCursor(byte[] pixels, int width, int height, Point hotspot)
        {
            SetCursor(pixels, 0, width, height, hotspot, 0xFF);

        }

        public void SetCursor(byte[] pixels, int offset, int width, int height, Point hotspot, int keyColor)
        {
            _hotspot = new Vector2(hotspot.X, hotspot.Y);
            _view._pixelsCursor = new byte[width * height * 4];

            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    var palColor = pixels[offset + w + h * width];
                    if (palColor == keyColor)
                    {
                        _view._pixelsCursor[w * 4 + h * width * 4 + 3] = 0xFF;
                    }
                    else
                    {
                        var c = _palColor[palColor];
                        _view._pixelsCursor[w * 4 + h * width * 4] = (byte)c.R;
                        _view._pixelsCursor[w * 4 + h * width * 4 + 1] = (byte)c.G;
                        _view._pixelsCursor[w * 4 + h * width * 4 + 2] = (byte)c.B;
                        _view._pixelsCursor[w * 4 + h * width * 4 + 3] = 0xFF;
                    }
                }
            }
        }

        public void SetPalette(Color[] color)
        {
            _palColor = color;
        }

        public void SetPalette(Color[] color, int first, int num)
        {
            Array.Copy(color, 0, _palColor, first, num);
        }

        public void UpdateScreen()
        {
            var length = Bounds.Width * Bounds.Height;
            for (int i = 0; i < length; i++)
            {
                var c = _palColor[Pixels[i]];
                _view._color[i * 3] = (byte)c.R;
                _view._color[i * 3 + 1] = (byte)c.G;
                _view._color[i * 3 + 2] = (byte)c.B;
            }
        }
    }

}
