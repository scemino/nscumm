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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Scumm4;

namespace CostumeViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Costume _cost;
        private CostumeAnimation _anim;
        private WriteableBitmap _bmp;
        private ScummIndex _index;

        public MainWindow()
        {
            InitializeComponent();

            var info = ((CostumeViewer.App)App.Current).Info;
            this.Title = string.Format("{0} - {1}", info.Description, info.Culture.NativeName);

            _index = new ScummIndex();
            _index.LoadIndex(info.Path);
            sliderCost.Minimum = 0;
            sliderCost.Maximum = 0x7D;
            sliderCost.Value = 0x23;
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _anim = _cost.Animations[(int)slider1.Value];
            CostumeAnimationLimb frame = null;
            if (_anim != null)
            {
                frame = _anim.Limbs.FirstOrDefault(f => f != null);
            }

            if (frame != null)
            {
                slider2.IsEnabled = true;
                slider2.Minimum = 0;
                slider2.Maximum = _anim.Limbs.Max(f => (f != null) ? f.End - f.Start : 0);
                slider2.Value = slider2.Minimum;
            }
            else
            {
                slider2.IsEnabled = false;
                slider2.Value = 0;
                slider2.Minimum = 0;
                slider2.Maximum = 0;
            }
            UpdatePicture();
        }

        private void slider2_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePicture();
        }

        private void UpdatePicture()
        {
            var data = new byte[320 * 200];
            _bmp.WritePixels(new Int32Rect(0, 0, 320, 200), data, 320, 0);
            for (int i = 0; i < 16; i++)
            {
                var limb = _anim != null ? _anim.Limbs[i] : null;
                var num = (int)slider2.Value;
                if (limb != null && limb.Start != 0xFFFF && (limb.Start + num) <= limb.End && limb.Pictures.Count > num)
                {
                    var pict = limb.Pictures[num];
                    _bmp.WritePixels(new Int32Rect(160 + pict.RelX, 100 + pict.RelY, pict.Width, pict.Height), pict.Data, pict.Width, 0);
                    image1.Source = _bmp;
                }
            }
        }

        private void sliderCost_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                _cost = _index.GetCostume((byte)sliderCost.Value);
                if (_cost != null)
                {
                    var room = _index.GetRoom(_cost.Room);
                    _bmp = new WriteableBitmap(320, 200, 96, 96, PixelFormats.Indexed8, new BitmapPalette(room.Palette.Colors));
                    slider1.IsEnabled = _cost.Animations.Length > 0;
                    slider1.Minimum = 0;
                    slider1.Maximum = _cost.Animations.Length - 1;
                    slider1.Value = 0;
                    _anim = (from anim in _cost.Animations
                            where anim != null
                            select anim).FirstOrDefault();
                    slider2.IsEnabled = _anim != null && _anim.Limbs.Count > 0;
                    slider2.Minimum = 0;
                    slider2.Maximum = _anim != null ? _anim.Limbs.Count - 1 : 0;
                    slider2.Value = 0;
                }
            }
            catch (Exception) { }

            if (_cost == null)
            {
                slider1.IsEnabled = slider2.IsEnabled = false;
            }
        }
    }
}

