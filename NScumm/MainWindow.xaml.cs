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

using Scumm4;
using Scumm4.Graphics;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace NScumm
{
    public class WpfGraphicsManager : DispatcherObject, IGraphicsManager
    {
        private Image _elt;
        private byte[] _pixels;
        private WriteableBitmap _bmp;

        public WpfGraphicsManager(Image elt, WriteableBitmap bmp, byte[] pixels, Color[] colors)
        {
            _colors = colors;
            _bmp = bmp;
            _elt = elt;
            _pixels = pixels;
            this.Width = _elt.ActualWidth;
            this.Height = _elt.ActualHeight;
            _elt.SizeChanged += OnSizeChanged;
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Width = _elt.ActualWidth;
            this.Height = _elt.ActualHeight;
        }

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

        public void SetPalette(Color[] colors)
        {
            Array.Copy(colors, _colors, colors.Length);
            this.Dispatcher.Invoke(new Action(() =>
            {
                _bmp = new WriteableBitmap(320, 200, 96, 96, PixelFormats.Indexed8, new BitmapPalette(_colors));
                _elt.Source = _bmp;
            }));
        }

        public Scumm4.Point GetMousePosition()
        {
            var pos = Mouse.GetPosition(_elt);
            return new Scumm4.Point((short)pos.X, (short)pos.Y);
        }

        public void UpdateScreen()
        {
            //this.Dispatcher.Invoke(new Action(() =>
            //{
            //_bmp.WritePixels(new Int32Rect(0, 0, 320, 200), _pixels, 320 * 3, 0);
            //if (m_ptr != IntPtr.Zero)
            //{
            //    _bmp.WritePixels(new Int32Rect(0, 0, 320, 200), m_ptr, 200 * 320, 320);
            //}
            //}));
        }

        private Color[] _colors;
        public void CopyRectToScreen(Array buf, int sourceStride, int x, int y, int width, int height)
        {
            if (height == 0) return;
            if (this.Dispatcher.CheckAccess())
            {
                CopyRectToScreenCore(buf, sourceStride, x, y, width, height);
            }
            else
            {
                this.Dispatcher.Invoke(new Action<Array, int, int, int, int, int>(CopyRectToScreenCore),
                    buf, sourceStride, x, y, width, height);
            }
        }

        private void CopyRectToScreenCore(Array buf, int sourceStride, int x, int y, int width, int height)
        {
            _bmp.WritePixels(new Int32Rect(x, y, width, height), buf, sourceStride, x, y);
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private ScummIndex _index;
        private ScummInterpreter _interpreter;
        private Thread _thread;
        public byte[] _pixels;
        private WriteableBitmap bmp;
        private Color[] _colors = new Color[256];
        private BitmapPalette _palette;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            _palette = new BitmapPalette(_colors);
            _pixels = new byte[320 * 200];
            bmp = new WriteableBitmap(320, 200, 96, 96, PixelFormats.Indexed8, _palette);
            _screen.Source = bmp;

            var info = ((NScumm.App)App.Current).Info;
            this.Title = string.Format("{0} - {1}", info.Description, info.Culture.NativeName);

            _index = new ScummIndex();
            _index.LoadIndex(info.Path);
            _index.GetCharset(4);

            var gfx = new WpfGraphicsManager(_screen, bmp, _pixels, _colors);
            _interpreter = new ScummInterpreter(_index, _pixels, gfx);

            _thread = new Thread(new ThreadStart(() =>
            {
                _interpreter.Go();
            }));
            _thread.IsBackground = true;
            _thread.Start();
        }


    }

    public class MiddleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (double)value / 2.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (double)value * 2.0;
        }
    }

}

