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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using OpenTK;

namespace NScumm.MonoGame
{
    sealed class XnaGraphicsManager : NScumm.Core.Graphics.IGraphicsManager, IDisposable
    {
        #region Fields

        readonly Texture2D _texture;
        Texture2D _textureCursor;
        byte[] _pixels;
        Color[] _palColors;
        bool _cursorVisible;
        int _shakePos;
        GraphicsDevice _device;
        int _width, _height;

        #endregion

        #region Constructor

        NativeWindow _window;

        public XnaGraphicsManager(int width, int height, NativeWindow window, GraphicsDevice device)
        {
            if (device == null)
                throw new ArgumentNullException("device");

            _window = window;
            _device = device;
            _width = width;
            _height = height;
            _pixels = new byte[_width * _height];
            _texture = new Texture2D(device, _width, _height);
            _textureCursor = new Texture2D(device, 16, 16);
            _palColors = new Color[256];
            for (int i = 0; i < _palColors.Length; i++)
            {
                _palColors[i] = Color.White;               
            }
            _colors = new Color[_width * _height];
        }

        #endregion

        object _gate = new object();
        Color[] _colors;

        public void UpdateScreen()
        {
            lock (_gate)
            {
                for (int h = 0; h < _height; h++)
                {
                    for (int w = 0; w < _width; w++)
                    {
                        var color = _palColors[_pixels[w + h * _width]];
                        _colors[w + h * _width] = color;
                    }
                }
            }
        }

        int snapshot = 0;

        public void Snapshot()
        {
            using (var bmp = new System.Drawing.Bitmap(_width, _height))
            {
                for (int h = 0; h < _height; h++)
                {
                    for (int w = 0; w < _width; w++)
                    {
                        var color = _palColors[_pixels[w + h * _width]];
                        bmp.SetPixel(w, h, System.Drawing.Color.FromArgb(color.R, color.G, color.B));
                    }
                }
                bmp.Save(string.Format("/tmp/frame_{0}.png", ++snapshot));
            }
        }

        public void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int width, int height)
        {
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    _pixels[x + w + (y + h) * _width] = buffer[w + h * sourceStride];
                }
            }
        }

        #region Palette Methods

        public void SetPalette(NScumm.Core.Graphics.Color[] colors)
        {
            if (colors.Length > 0)
            {
                SetPalette(colors, 0, colors.Length);
            }
        }

        public void SetPalette(NScumm.Core.Graphics.Color[] colors, int first, int num)
        {
            for (int i = 0; i < num; i++)
            {
                var color = colors[i + first];
                _palColors[i + first] = new Color(color.R, color.G, color.B);
            }
        }

        #endregion

        #region Cursor Methods

        public Microsoft.Xna.Framework.Vector2 Hotspot { get; private set; }

        public void SetCursor(byte[] pixels, int width, int height, NScumm.Core.Graphics.Point hotspot)
        {
            if (_textureCursor.Width != width || _textureCursor.Height != height)
            {
                _textureCursor.Dispose();
                _textureCursor = new Texture2D(_device, width, height);
            }

            Hotspot = new Microsoft.Xna.Framework.Vector2(hotspot.X, hotspot.Y);
            var pixelsCursor = new Color[width * height];
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    var palColor = pixels[w + h * width];
                    var color = palColor == 0xFF ? Color.Transparent : _palColors[palColor];
                    pixelsCursor[w + h * width] = color;
                }
            }
            _textureCursor.SetData(pixelsCursor);
        }

        public bool ShowCursor(bool show)
        {
            var lastState = _cursorVisible;
            _cursorVisible = show;
            return lastState;
        }

        #endregion

        #region Draw Methods

        public void DrawScreen(SpriteBatch spriteBatch)
        {
            lock (_gate)
            {
                var rect = spriteBatch.GraphicsDevice.Viewport.Bounds;
                rect.Offset(0, _shakePos);
                _texture.SetData(_colors);
                spriteBatch.Draw(_texture, rect, null, Color.White);
            }
        }

        public void DrawCursor(SpriteBatch spriteBatch, Microsoft.Xna.Framework.Vector2 cursorPos)
        {
            if (_cursorVisible)
            {
                double scaleX = _window.Bounds.Width / _width;
                double scaleY = _window.Bounds.Height / _height;
                var rect = new Rectangle((int)(cursorPos.X - _window.Bounds.X - scaleX * Hotspot.X), (int)(cursorPos.Y - _window.Bounds.Y - scaleY * Hotspot.Y), (int)(scaleX * _textureCursor.Width), (int)(scaleY * _textureCursor.Height));
                spriteBatch.Draw(_textureCursor, rect, null, Color.White);
            }
        }

        #endregion

        #region Misc

        public void SetShakePos(int pos)
        {
            _shakePos = pos;
        }

        #endregion

        #region Dispose

        ~XnaGraphicsManager ()
        {
            Dispose();
        }

        public void Dispose()
        {
            _texture.Dispose();
            _textureCursor.Dispose();
        }

        #endregion
    }
}
