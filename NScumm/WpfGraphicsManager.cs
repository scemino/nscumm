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

using Scumm4.Graphics;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NScumm
{
    internal sealed class WpfGraphicsManager : DispatcherObject, IGraphicsManager
    {
        #region Fields
        private Image _elt;
        private WriteableBitmap _bmp;
        private byte[] _pixels;
        private System.Windows.Media.Color[] _colors;
        private bool _showCursor;
        private bool _updatePalette;
        #endregion

        #region Constructor
        public WpfGraphicsManager(Image elt)
        {
            _colors = new System.Windows.Media.Color[256];
            _pixels = new byte[320 * 200];
            _updatePalette = true;
            _elt = elt;
            this.Width = _elt.ActualWidth;
            this.Height = _elt.ActualHeight;

            _elt.SizeChanged += OnSizeChanged;
        }
        #endregion

        #region Properties
        public double Width
        {
            get;
            private set;
        }

        public double Height
        {
            get;
            private set;
        }
        #endregion

        #region Palette Methods
        public void SetPalette(Scumm4.Graphics.Color[] colors)
        {
            if (colors.Length > 0)
            {
                SetPalette(colors, 0, colors.Length);
            }
        }

        public void SetPalette(Scumm4.Graphics.Color[] colors, int first, int num)
        {
            for (int i = 0; i < num; i++)
            {
                var color = colors[i + first];
                _colors[i + first] = System.Windows.Media.Color.FromRgb(color.R, color.G, color.B);
            }
            _updatePalette = true;
        }
        #endregion

        #region Screen Methods
        public void UpdateScreen()
        {
            if (this.Dispatcher.CheckAccess())
            {
                UpdateScreenCore();
            }
            else
            {
                this.Dispatcher.Invoke(new Action(() => UpdateScreenCore()));
            }
        }

        public void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int width, int height)
        {
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    _pixels[x + w + (y + h) * 320] = buffer[x + w + (y + h) * sourceStride];
                }
            }
        }
        #endregion

        #region Cursor Methods
        public void SetCursor(byte[] pixels, int width, int height, int hotspotX, int hotspotY)
        {
            Action setCursor = new Action(() =>
            {
                if (_showCursor)
                {
                    // create real-size cursor
                    _colors[0] = Colors.Transparent;
                    var cursorBmp = new WriteableBitmap(16, 16, 96, 96, PixelFormats.Indexed8, new BitmapPalette(_colors));
                    cursorBmp.WritePixels(new Int32Rect(0, 0, width, height), pixels, width, 0);

                    // scale it
                    double scaleX = this.Width / 320.0;
                    double scaleY = this.Height / 200.0;
                    var cursorBmpScaled = new TransformedBitmap(cursorBmp, new ScaleTransform(scaleX, scaleY, 0, 0));

                    _elt.Cursor = CursorHelper.CreateCursor(cursorBmpScaled, hotspotX, hotspotY);
                }
                else
                {
                    _elt.Cursor = Cursors.None;
                }
            });

            if (this.Dispatcher.CheckAccess())
            {
                setCursor();
            }
            else
            {
                this.Dispatcher.Invoke(setCursor);
            }
        }

        public void ShowCursor(bool show)
        {
            _showCursor = show;
        }
        #endregion

        #region Private Methods
        private void UpdateScreenCore()
        {
            CreateBitmap();
            _bmp.WritePixels(new Int32Rect(0, 0, 320, 200), _pixels, 320, 0, 0);

            _elt.Source = _bmp;
        }

        private void CreateBitmap()
        {
            if (_updatePalette)
            {
                _bmp = new WriteableBitmap(320, 200, 96, 96, PixelFormats.Indexed8, new BitmapPalette(_colors));
            }
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Width = _elt.ActualWidth;
            this.Height = _elt.ActualHeight;
        }
        #endregion
    }

}
