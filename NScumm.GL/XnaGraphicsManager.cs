using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NScumm.GL
{
    internal sealed class XnaGraphicsManager : NScumm.Core.Graphics.IGraphicsManager, IDisposable
    {
        #region Fields
        private Texture2D _texture;
        private Texture2D _textureCursor;
        private Microsoft.Xna.Framework.Color[] _pixels;
        private Microsoft.Xna.Framework.Color[] _colors;
        private bool _cursorVisible;
        private Microsoft.Xna.Framework.Vector2 _hotspot;
        #endregion

        public XnaGraphicsManager(GraphicsDevice device)
        {
            _pixels = new Microsoft.Xna.Framework.Color[320 * 200];
            _texture = new Texture2D(device, 320, 200);
            _textureCursor = new Texture2D(device, 16, 16);
            _colors = new Microsoft.Xna.Framework.Color[256];
        }

        public void UpdateScreen()
        {
            _texture.SetData(_pixels);
        }

        public void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int width, int height)
        {
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    var color = _colors[buffer[x + w + (y + h) * sourceStride]];
                    _pixels[x + w + (y + h) * 320] = color;
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
                _colors[i + first] = new Microsoft.Xna.Framework.Color(color.R, color.G, color.B);
            }
        }
        #endregion

        #region Cursor Methods
        public void SetCursor(byte[] pixels, int width, int height, int hotspotX, int hotspotY)
        {
            _hotspot = new Microsoft.Xna.Framework.Vector2(hotspotX, hotspotY);

            var pixelsCursor = new Microsoft.Xna.Framework.Color[16 * 16];
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    var palColor = pixels[w + h * width];
                    var color = palColor == 0 ? Microsoft.Xna.Framework.Color.Transparent : _colors[palColor];
                    pixelsCursor[w + h * width] = color;
                }
            }
            _textureCursor.SetData(pixelsCursor);
        }

        public void ShowCursor(bool show)
        {
            _cursorVisible = show;
        }
        #endregion

        #region Draw Methods
        public void DrawScreen(SpriteBatch spriteBatch)
        {
			var rect = spriteBatch.GraphicsDevice.PresentationParameters.Bounds;
            spriteBatch.Draw(_texture, rect, null, Microsoft.Xna.Framework.Color.White);
        }

        public void DrawCursor(SpriteBatch spriteBatch, Vector2 cursorPos)
        {
            if (_cursorVisible)
            {
                double scaleX = spriteBatch.GraphicsDevice.PresentationParameters.Bounds.Width / 320.0;
                double scaleY = spriteBatch.GraphicsDevice.PresentationParameters.Bounds.Height / 200.0;
                var rect = new Rectangle((int)(cursorPos.X - scaleX * _hotspot.X), (int)(cursorPos.Y - scaleY * _hotspot.Y), (int)(scaleX * 16), (int)(scaleY * 16));
                spriteBatch.Draw(_textureCursor, rect, null, Microsoft.Xna.Framework.Color.White);
            }
        } 
        #endregion

        #region Dispose
        ~XnaGraphicsManager()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            _texture.Dispose();
            _textureCursor.Dispose();
        } 
        #endregion
    }
}
