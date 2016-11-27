//
//  XnaGraphicsManager.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NScumm.Core;
using Point = NScumm.Core.Graphics.Point;
using Rect = NScumm.Core.Graphics.Rect;

namespace NScumm.Desktop
{
    sealed class XnaGraphicsManager : Core.Graphics.IGraphicsManager, IDisposable
    {
        public Core.Graphics.PixelFormat PixelFormat
        {
            get { return _pixelFormat; }
            set
            {
                _pixelFormat = value;
                var pixelSize = _pixelFormat == Core.Graphics.PixelFormat.Rgb16 ? 2 : 1;
                _colorGraphicsManager = _pixelFormat == Core.Graphics.PixelFormat.Rgb16 ? (IColorGraphicsManager)new Rgb16GraphicsManager(this) : new RgbIndexed8GraphicsManager(this);
                _pixels = new byte[_width * _height * pixelSize];
            }
        }

        public byte[] Pixels => _pixels;

        public int ShakePosition { get; set; }

        public bool IsCursorVisible { get; set; }

        public Rect Bounds => new Rect((short) _rect.Left, (short) _rect.Top, (short) _rect.Right, (short) _rect.Bottom);

        public XnaGraphicsManager(int width, int height, Core.Graphics.PixelFormat format, GraphicsDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            _device = device;
            _width = width;
            _height = height;
            PixelFormat = format;

            _texture = new Texture2D(device, _width, _height);
            _textureCursor = new Texture2D(device, 16, 16);
            _palColors = new Color[256];
            _colors = new Color[_width * _height];
            _preferredAspect = (float)_width / _height;
        }

        public void UpdateScreen()
        {
            lock (_gate)
            {
                _colorGraphicsManager.UpdateScreen();
            }
        }

        public void CopyRectToScreen(BytePtr buffer, int startOffset, int sourceStride, int x, int y, int width, int height)
        {
            _colorGraphicsManager.CopyRectToScreen(buffer, startOffset, sourceStride, x, y, width, height);
        }

        public void CopyRectToScreen(BytePtr buffer, int sourceStride, int x, int y, int width, int height)
        {
            _colorGraphicsManager.CopyRectToScreen(buffer, sourceStride, x, y, width, height);
        }

        public void CopyRectToScreen(BytePtr buffer, int sourceStride, int x, int y, int dstX, int dstY, int width, int height)
        {
            _colorGraphicsManager.CopyRectToScreen(buffer, sourceStride, x, y, dstX, dstY, width, height);
        }

        public Core.Graphics.Surface Capture()
        {
            var surface = new Core.Graphics.Surface((ushort) _width, (ushort) _height, Core.Graphics.PixelFormat.Indexed8, false);
            Array.Copy(_pixels,0, surface.Pixels.Data, surface.Pixels.Offset, _pixels.Length);
            return surface;
        }

        #region Palette Methods

        public void SetPalette(Core.Graphics.Color[] colors)
        {
            if (colors.Length > 0)
            {
                SetPalette(colors, 0, colors.Length);
            }
        }

        public Core.Graphics.Color[] GetPalette()
        {
            var colors = new Core.Graphics.Color[256];
            for (var i = 0; i < 256; i++)
            {
                var c = _palColors[i];
                colors[i] = Core.Graphics.Color.FromRgb(c.R, c.G, c.B);
            }
            return colors;
        }

        public void SetPalette(Core.Graphics.Color[] colors, int first, int num)
        {
            for (var i = 0; i < num; i++)
            {
                var color = colors[i + first];
                _palColors[i + first] = new Color(color.R, color.G, color.B);
            }

            _colorGraphicsManager.UpdateCursor();
        }

        #endregion

        #region Cursor Methods

        public Vector2 Hotspot { get; private set; }

        public void SetCursor(BytePtr pixels, int width, int height, Point hotspot, int keyColor = 0xFF)
        {
            _colorGraphicsManager.SetCursor(pixels, width, height, hotspot, keyColor);
        }

        public void FillScreen(int color)
        {
            _colorGraphicsManager.FillScreen(color);
        }

        #endregion

        #region Draw Methods

        public void DrawScreen(SpriteBatch spriteBatch)
        {
            var width = spriteBatch.GraphicsDevice.PresentationParameters.Bounds.Width;
            var height = spriteBatch.GraphicsDevice.PresentationParameters.Bounds.Height;
            var outputAspect = (float)width / height;
            if (outputAspect <= _preferredAspect)
            {
                // output is taller than it is wider, bars on top/bottom
                var presentHeight = (int)((width / _preferredAspect) + 0.5f);
                var barHeight = (height - presentHeight) / 2;
                _rect = new Rectangle(0, barHeight, width, presentHeight);
            }
            else
            {
                // output is wider than it is tall, bars left/right
                var presentWidth = (int)((height * _preferredAspect) + 0.5f);
                var barWidth = (width - presentWidth) / 2;
                _rect = new Rectangle(barWidth, 0, presentWidth, height);
            }
            _rect.Offset(0, ShakePosition);

            lock (_gate)
            {
                _texture.SetData(_colors);
                spriteBatch.Draw(_texture, _rect, Color.White);
            }
        }

        public void DrawCursor(SpriteBatch spriteBatch)
        {
            if (IsCursorVisible)
            {
                var cursorPos = Mouse.GetState().Position.ToVector2() * 2;
                float width = _rect.Width;
                float height = _rect.Height;
                var scale = new Vector2(width / _width, height / _height);
                spriteBatch.Draw(_textureCursor,
                    cursorPos - (scale * Hotspot),
                    color: Color.White,
                    scale: scale);
            }
        }

        #endregion

        #region Dispose

        ~XnaGraphicsManager()
        {
            Dispose();
        }

        public void Dispose()
        {
            _texture.Dispose();
            _textureCursor.Dispose();
        }

        #endregion

        #region IColorGraphicsManager classes

        interface IColorGraphicsManager
        {
            void UpdateScreen();

            void CopyRectToScreen(BytePtr buffer, int startOffset, int sourceStride, int x, int y, int width, int height);

            void CopyRectToScreen(BytePtr buffer, int sourceStride, int x, int y, int width, int height);

            void CopyRectToScreen(BytePtr buffer, int sourceStride, int x, int y, int dstX, int dstY, int width, int height);

            void SetCursor(BytePtr pixels, int width, int height, Point hotspot, int keyColor = 0xFF);

            void UpdateCursor();

            void FillScreen(int color);
       }

        class Rgb16GraphicsManager : IColorGraphicsManager
        {
            readonly XnaGraphicsManager _gfxManager;

            public Rgb16GraphicsManager(XnaGraphicsManager gfxManager)
            {
                _gfxManager = gfxManager;
            }

            public void UpdateCursor()
            {
            }

            public void UpdateScreen()
            {
                byte r, g, b;
                for (var h = 0; h < _gfxManager._height; h++)
                {
                    for (var w = 0; w < _gfxManager._width; w++)
                    {
                        var c = _gfxManager._pixels.ToUInt16(w * 2 + h * _gfxManager._width * 2);
                        Core.Graphics.ColorHelper.ColorToRGB(c, out r, out g, out b);
                        _gfxManager._colors[w + h * _gfxManager._width] = new Color(r, g, b);
                    }
                }
            }

            public void CopyRectToScreen(BytePtr buffer, int startOffset, int sourceStride, int x, int y, int width, int height)
            {
                for (var h = 0; h < height; h++)
                {
                    for (var w = 0; w < width; w++)
                    {
                        _gfxManager._pixels.WriteUInt16(w * 2 + h * _gfxManager._width * 2, buffer.ToUInt16(startOffset + (x + w) * 2 + (h + y) * sourceStride));
                    }
                }
            }

            public void CopyRectToScreen(BytePtr buffer, int sourceStride, int x, int y, int width, int height)
            {
                for (var h = 0; h < height; h++)
                {
                    for (var w = 0; w < width; w++)
                    {
                        _gfxManager._pixels.WriteUInt16(w * 2 + h * _gfxManager._width * 2, buffer.ToUInt16((x + w) * 2 + (h + y) * sourceStride));
                    }
                }
            }

            public void CopyRectToScreen(BytePtr buffer, int sourceStride, int x, int y, int dstX, int dstY, int width, int height)
            {
                for (var h = 0; h < height; h++)
                {
                    for (var w = 0; w < width; w++)
                    {
                        _gfxManager._pixels.WriteUInt16((dstX + w) * 2 + (dstY + h) * _gfxManager._width * 2, buffer.ToUInt16((x + w) * 2 + (h + y) * sourceStride));
                    }
                }
            }

            public void SetCursor(BytePtr pixels, int width, int height, Point hotspot, int keyColor)
            {
                if (_gfxManager._textureCursor.Width != width || _gfxManager._textureCursor.Height != height)
                {
                    _gfxManager._textureCursor.Dispose();
                    _gfxManager._textureCursor = new Texture2D(_gfxManager._device, width, height);
                }

                _gfxManager.Hotspot = new Vector2(hotspot.X, hotspot.Y);
                var pixelsCursor = new Color[width * height];

                byte r, g, b;
                for (var h = 0; h < height; h++)
                {
                    for (var w = 0; w < width; w++)
                    {
                        var palColor = pixels.ToUInt16(w * 2 + h * width * 2);
                        Core.Graphics.ColorHelper.ColorToRGB(palColor, out r, out g, out b);
                        var color = palColor == keyColor ? Color.Transparent : new Color(r, g, b);
                        pixelsCursor[w + h * width] = color;
                    }
                }

                _gfxManager._textureCursor.SetData(pixelsCursor);
            }

            public void FillScreen(int color)
            {
                for (var h = 0; h < _gfxManager._height; h++)
                {
                    for (var w = 0; w < _gfxManager._width; w++)
                    {
                        _gfxManager._pixels.WriteUInt16(w * 2 + h * _gfxManager._width * 2, (ushort)color);
                    }
                }
            }
        }

        class RgbIndexed8GraphicsManager : IColorGraphicsManager
        {
            readonly XnaGraphicsManager _gfxManager;

            public RgbIndexed8GraphicsManager(XnaGraphicsManager gfxManager)
            {
                _gfxManager = gfxManager;
            }

            public void UpdateScreen()
            {
                var length = _gfxManager._height * _gfxManager._width;
                for (var i = 0; i < length; i++)
                {
                    _gfxManager._colors[i] = _gfxManager._palColors[_gfxManager._pixels[i]];
                }
            }

            public void CopyRectToScreen(BytePtr buffer, int startOffset, int sourceStride, int x, int y, int width, int height)
            {
                for (var h = 0; h < height; h++)
                {
                    Array.Copy(buffer.Data, buffer.Offset+ startOffset + h * sourceStride, _gfxManager._pixels, x + (y + h) * _gfxManager._width, width);
                }
            }

            public void CopyRectToScreen(BytePtr buffer, int sourceStride, int x, int y, int width, int height)
            {
                for (var h = 0; h < height; h++)
                {
                    Array.Copy(buffer.Data, buffer.Offset + h * sourceStride, _gfxManager._pixels, x + (y + h) * _gfxManager._width, width);
                }
            }

            public void CopyRectToScreen(BytePtr buffer, int sourceStride, int x, int y, int dstX, int dstY, int width, int height)
            {
                for (var h = 0; h < height; h++)
                {
                    Array.Copy(buffer.Data, buffer.Offset + x + (h + y) * sourceStride, _gfxManager._pixels, dstX + (dstY + h) * _gfxManager._width, width);
                }
            }

            BytePtr _cursorPixels;
            int _cursorWidth;
            int _cursorHeight;
            Point _cursorHotspot;
            int _cursorKeyColor;

            public void UpdateCursor()
            {
                SetCursor(_cursorPixels, _cursorWidth, _cursorHeight, _cursorHotspot, _cursorKeyColor);
            }

            public void SetCursor(BytePtr pixels, int width, int height, Point hotspot, int keyColor)
            {
                _cursorPixels = pixels;
                _cursorWidth = width;
                _cursorHeight = height;
                _cursorHotspot = hotspot;
                _cursorKeyColor = keyColor;

                if (_gfxManager._textureCursor.Width != width || _gfxManager._textureCursor.Height != height)
                {
                    _gfxManager._textureCursor.Dispose();
                    _gfxManager._textureCursor = new Texture2D(_gfxManager._device, width, height);
                }

                _gfxManager.Hotspot = new Vector2(hotspot.X, hotspot.Y);
                var pixelsCursor = new Color[width * height];

                for (var h = 0; h < height; h++)
                {
                    for (var w = 0; w < width; w++)
                    {
                        var palColor = pixels[w + h * width];
                        var color = palColor == keyColor ? Color.Transparent : _gfxManager._palColors[palColor];
                        pixelsCursor[w + h * width] = color;
                    }
                }

                _gfxManager._textureCursor.SetData(pixelsCursor);
            }

            public void FillScreen(int color)
            {
                _gfxManager._pixels.Set(0, (byte)color, _gfxManager._width * _gfxManager._height);
            }
        }

        #endregion

        #region Fields

        private Core.Graphics.PixelFormat _pixelFormat;
        private readonly Texture2D _texture;
        private Texture2D _textureCursor;
        private byte[] _pixels;
        private readonly Color[] _palColors;
        private readonly GraphicsDevice _device;
        private readonly int _width;
        private readonly int _height;
        private readonly object _gate = new object();
        private readonly Color[] _colors;
        private IColorGraphicsManager _colorGraphicsManager;
        private readonly float _preferredAspect;
        private Rectangle _rect;

        #endregion
    }
}
