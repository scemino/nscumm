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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using NScumm.Core;
using Point = NScumm.Core.Graphics.Point;

namespace NScumm.MonoGame
{
    sealed class XnaGraphicsManager : Core.Graphics.IGraphicsManager, IDisposable
    {
        Core.Graphics.PixelFormat _pixelFormat;

        public Core.Graphics.PixelFormat PixelFormat
        {
            get { return _pixelFormat; }
            set
            {
                _pixelFormat = value;
                var pixelSize = _pixelFormat == NScumm.Core.Graphics.PixelFormat.Rgb16 ? 2 : 1;
                _colorGraphicsManager = _pixelFormat == NScumm.Core.Graphics.PixelFormat.Rgb16 ? (IColorGraphicsManager)new Rgb16GraphicsManager(this) : new RgbIndexed8GraphicsManager(this);
                _pixels = new byte[_width * _height * pixelSize];
            }
        }

        public int ShakePosition { get; set; }
        public bool IsCursorVisible { get; set; }

        public XnaGraphicsManager(int width, int height, NScumm.Core.Graphics.PixelFormat format, GameWindow window, GraphicsDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            _window = window;
            _device = device;
            _width = width;
            _height = height;
            PixelFormat = format;
            
            _texture = new Texture2D(device, _width, _height);
            _textureCursor = new Texture2D(device, 16, 16);
            _palColors = new Color[256];
            _colors = new Color[_width * _height];
        }

        public void UpdateScreen()
        {
            lock (_gate)
            {
                _colorGraphicsManager.UpdateScreen();
            }
        }

        public void CopyRectToScreen(byte[] buffer, int startOffset, int sourceStride, int x, int y, int width, int height)
        {
            _colorGraphicsManager.CopyRectToScreen(buffer, startOffset, sourceStride, x, y, width, height);
        }

        public void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int width, int height)
        {
            _colorGraphicsManager.CopyRectToScreen(buffer, sourceStride, x, y, width, height);
        }

        public void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int dstX, int dstY, int width, int height)
        {
            _colorGraphicsManager.CopyRectToScreen(buffer, sourceStride, x, y, dstX, dstY, width, height);
        }

        public Core.Graphics.Surface Capture()
        {
            var surface = new Core.Graphics.Surface(_width, _height, NScumm.Core.Graphics.PixelFormat.Indexed8, false);
            Array.Copy(_pixels, surface.Pixels, _pixels.Length);
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
            for (int i = 0; i < 256; i++)
            {
                var c = _palColors[i];
                colors[i] = Core.Graphics.Color.FromRgb(c.R, c.G, c.B);
            }
            return colors;
        }

        public void SetPalette(Core.Graphics.Color[] colors, int first, int num)
        {
            for (int i = 0; i < num; i++)
            {
                var color = colors[i + first];
                _palColors[i + first] = new Color(color.R, color.G, color.B);
            }
        }

        #endregion

        #region Cursor Methods

        public Vector2 Hotspot { get; private set; }

        public void SetCursor(byte[] pixels, int width, int height, Core.Graphics.Point hotspot)
        {
            _colorGraphicsManager.SetCursor(pixels, width, height, hotspot);
        }

        public void SetCursor(byte[] pixels, int offset, int width, int height, Point hotspot, int keyColor)
        {
            _colorGraphicsManager.SetCursor(pixels, offset, width, height, hotspot, keyColor);
        }

        public void FillScreen(int color)
        {
            _colorGraphicsManager.FillScreen(color);
        }

        #endregion

        #region Draw Methods

        public void DrawScreen(SpriteBatch spriteBatch)
        {
            lock (_gate)
            {
                var rect = spriteBatch.GraphicsDevice.Viewport.Bounds;
                rect.Offset(0, ShakePosition);
                _texture.SetData(_colors);
                spriteBatch.Draw(_texture, rect, null, Color.White);
            }
        }

        public void DrawCursor(SpriteBatch spriteBatch, Vector2 cursorPos)
        {
            if (IsCursorVisible)
            {
                double scaleX = _window.ClientBounds.Width / _width;
                double scaleY = _window.ClientBounds.Height / _height;
                var rect = new Rectangle((int)(cursorPos.X - scaleX * Hotspot.X), (int)(cursorPos.Y - scaleY * Hotspot.Y), (int)(scaleX * _textureCursor.Width), (int)(scaleY * _textureCursor.Height));
                spriteBatch.Draw(_textureCursor, rect, null, Color.White);
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
            void CopyRectToScreen(byte[] buffer, int startOffset, int sourceStride, int x, int y, int width, int height);
            void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int width, int height);
            void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int dstX, int dstY, int width, int height);
            void SetCursor(byte[] pixels, int width, int height, Core.Graphics.Point hotspot);
            void SetCursor(byte[] pixels, int offset, int width, int height, Core.Graphics.Point hotspot, int keyColor);
            void FillScreen(int color);
        }

        class Rgb16GraphicsManager : IColorGraphicsManager
        {
            XnaGraphicsManager _gfxManager;

            public Rgb16GraphicsManager(XnaGraphicsManager gfxManager)
            {
                _gfxManager = gfxManager;
            }

            public void UpdateScreen()
            {
                byte r, g, b;
                for (int h = 0; h < _gfxManager._height; h++)
                {
                    for (int w = 0; w < _gfxManager._width; w++)
                    {
                        var c = _gfxManager._pixels.ToUInt16(w * 2 + h * _gfxManager._width * 2);
                        NScumm.Core.Graphics.ColorHelper.ColorToRGB(c, out r, out g, out b);
                        _gfxManager._colors[w + h * _gfxManager._width] = new Color(r, g, b);
                    }
                }
            }

            public void CopyRectToScreen(byte[] buffer, int startOffset, int sourceStride, int x, int y, int width, int height)
            {
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        _gfxManager._pixels.WriteUInt16(w * 2 + h * _gfxManager._width * 2, buffer.ToUInt16(startOffset + (x + w) * 2 + (h + y) * sourceStride));
                    }
                }
            }

            public void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int width, int height)
            {
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        _gfxManager._pixels.WriteUInt16(w * 2 + h * _gfxManager._width * 2, buffer.ToUInt16((x + w) * 2 + (h + y) * sourceStride));
                    }
                }
            }

            public void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int dstX, int dstY, int width, int height)
            {
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        _gfxManager._pixels.WriteUInt16((dstX + w) * 2 + (dstY + h) * _gfxManager._width * 2, buffer.ToUInt16((x + w) * 2 + (h + y) * sourceStride));
                    }
                }
            }

            public void SetCursor(byte[] pixels, int width, int height, Core.Graphics.Point hotspot)
            {
                SetCursor(pixels, 0, width, height, hotspot, 0xFF);
            }

            public void SetCursor(byte[] pixels, int offset, int width, int height, Point hotspot, int keyColor)
            {
                if (_gfxManager._textureCursor.Width != width || _gfxManager._textureCursor.Height != height)
                {
                    _gfxManager._textureCursor.Dispose();
                    _gfxManager._textureCursor = new Texture2D(_gfxManager._device, width, height);
                }

                _gfxManager.Hotspot = new Vector2(hotspot.X, hotspot.Y);
                var pixelsCursor = new Color[width * height];

                byte r, g, b;
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        var palColor = pixels.ToUInt16(offset + w * 2 + h * width * 2);
                        NScumm.Core.Graphics.ColorHelper.ColorToRGB(palColor, out r, out g, out b);
                        var color = palColor == keyColor ? Color.Transparent : new Color(r, g, b);
                        pixelsCursor[w + h * width] = color;
                    }
                }

                _gfxManager._textureCursor.SetData(pixelsCursor);
            }

            public void FillScreen(int color)
            {
                for (int h = 0; h < _gfxManager._height; h++)
                {
                    for (int w = 0; w < _gfxManager._width; w++)
                    {
                        _gfxManager._pixels.WriteUInt16(w * 2 + h * _gfxManager._width * 2, (ushort)color);
                    }
                }
            }
        }

        class RgbIndexed8GraphicsManager : IColorGraphicsManager
        {
            XnaGraphicsManager _gfxManager;

            public RgbIndexed8GraphicsManager(XnaGraphicsManager gfxManager)
            {
                _gfxManager = gfxManager;
            }

            public void UpdateScreen()
            {
                for (int h = 0; h < _gfxManager._height; h++)
                {
                    for (int w = 0; w < _gfxManager._width; w++)
                    {
                        var color = _gfxManager._palColors[_gfxManager._pixels[w + h * _gfxManager._width]];
                        _gfxManager._colors[w + h * _gfxManager._width] = color;
                    }
                }
            }

            public void CopyRectToScreen(byte[] buffer, int startOffset, int sourceStride, int x, int y, int width, int height)
            {
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        _gfxManager._pixels[x + w + (y + h) * _gfxManager._width] = buffer[startOffset + w + h * sourceStride];
                    }
                }
            }

            public void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int width, int height)
            {
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        _gfxManager._pixels[x + w + (y + h) * _gfxManager._width] = buffer[w + h * sourceStride];
                    }
                }
            }

            public void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int dstX, int dstY, int width, int height)
            {
                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        _gfxManager._pixels[dstX + w + (dstY + h) * _gfxManager._width] = buffer[x + w + (h + y) * sourceStride];
                    }
                }
            }

            public void SetCursor(byte[] pixels, int width, int height, Core.Graphics.Point hotspot)
            {
                SetCursor(pixels, 0, width, height, hotspot, 0xFF);
            }

            public void SetCursor(byte[] pixels, int offset, int width, int height, Point hotspot, int keyColor)
            {
                if (_gfxManager._textureCursor.Width != width || _gfxManager._textureCursor.Height != height)
                {
                    _gfxManager._textureCursor.Dispose();
                    _gfxManager._textureCursor = new Texture2D(_gfxManager._device, width, height);
                }

                _gfxManager.Hotspot = new Vector2(hotspot.X, hotspot.Y);
                var pixelsCursor = new Color[width * height];

                for (int h = 0; h < height; h++)
                {
                    for (int w = 0; w < width; w++)
                    {
                        var palColor = pixels[offset + w + h * width];
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
        readonly Texture2D _texture;
        Texture2D _textureCursor;
        byte[] _pixels;
        Color[] _palColors;
        GraphicsDevice _device;
        int _width, _height;
        object _gate = new object();
        Color[] _colors;
        GameWindow _window;
        IColorGraphicsManager _colorGraphicsManager;
        #endregion
    }
}
