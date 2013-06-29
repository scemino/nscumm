using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace NScumm.GL
{
    sealed class XnaGraphicsManager : NScumm.Core.Graphics.IGraphicsManager, IDisposable
    {
        #region Fields
        Texture2D _texture;
        Texture2D _textureCursor;
        byte[] _pixels;
        Color[] _colors;
        bool _cursorVisible;
        Vector2 _hotspot;
        int _shakePos;
        #endregion

        #region Constructor
        public XnaGraphicsManager (GraphicsDevice device)
        {
            if (device == null)
                new ArgumentNullException ("device");

            _pixels = new byte[320 * 200];
            _texture = new Texture2D (device, 320, 200);
            _textureCursor = new Texture2D (device, 16, 16);
            _colors = new Color[256];
        }
        #endregion

        public void UpdateScreen ()
        {
            var colors = new Color[320 * 200];
            for (int h = 0; h < 200; h++) {
                for (int w = 0; w < 320; w++) {
                    var color = _colors [_pixels [w + h * 320]];
                    colors [w + h * 320] = color;
                }
            }
            _texture.SetData (colors);
        }

        public void CopyRectToScreen (byte[] buffer, int sourceStride, int x, int y, int width, int height)
        {
            for (int h = 0; h < height; h++) {
                for (int w = 0; w < width; w++) {
                    _pixels [x + w + (y + h) * 320] = buffer [w + h * sourceStride];
                }
            }
        }
        #region Palette Methods
        public void SetPalette (NScumm.Core.Graphics.Color[] colors)
        {
            if (colors.Length > 0) {
                SetPalette (colors, 0, colors.Length);
            }
        }

        public void SetPalette (NScumm.Core.Graphics.Color[] colors, int first, int num)
        {
            for (int i = 0; i < num; i++) {
                var color = colors [i + first];
                _colors [i + first] = new Color (color.R, color.G, color.B);
            }
        }
        #endregion
        #region Cursor Methods
        public void SetCursor (byte[] pixels, int width, int height, int hotspotX, int hotspotY)
        {
            _hotspot = new Vector2 (hotspotX, hotspotY);

            var pixelsCursor = new Color[16 * 16];
            for (int h = 0; h < height; h++) {
                for (int w = 0; w < width; w++) {
                    var palColor = pixels [w + h * width];
                    var color = palColor == 0 ? Color.Transparent : _colors [palColor];
                    pixelsCursor [w + h * width] = color;
                }
            }
            _textureCursor.SetData (pixelsCursor);
        }

        public void ShowCursor (bool show)
        {
            _cursorVisible = show;
        }
        #endregion
        #region Draw Methods
        public void DrawScreen (SpriteBatch spriteBatch)
        {
            var rect = spriteBatch.GraphicsDevice.Viewport.Bounds;
            rect.Offset (0, _shakePos);
            spriteBatch.Draw (_texture, rect, null, Color.White);
        }

        public void DrawCursor (SpriteBatch spriteBatch, Vector2 cursorPos)
        {
            if (_cursorVisible) {
                double scaleX = spriteBatch.GraphicsDevice.Viewport.Bounds.Width / 320.0;
                double scaleY = spriteBatch.GraphicsDevice.Viewport.Bounds.Height / 200.0;
                var rect = new Rectangle ((int)(cursorPos.X - scaleX * _hotspot.X), (int)(cursorPos.Y - scaleY * _hotspot.Y), (int)(scaleX * 16), (int)(scaleY * 16));
                spriteBatch.Draw (_textureCursor, rect, null, Color.White);
            }
        }
        #endregion
        #region Misc
        public void SetShakePos (int pos)
        {
            _shakePos = pos;
        }
        #endregion
        #region Dispose
        ~XnaGraphicsManager ()
        {
            Dispose ();
        }

        public void Dispose ()
        {
            _texture.Dispose ();
            _textureCursor.Dispose ();
        }
        #endregion
    }
}
