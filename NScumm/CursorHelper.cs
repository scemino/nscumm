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

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NScumm
{
    internal static class CursorHelper
    {
        private struct IconInfo
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

        public static System.Windows.Input.Cursor CreateCursor(BitmapSource bmp, int xHotSpot, int yHotSpot)
        {
            using (var bmp2 = Convert(bmp))
            {
                return InternalCreateCursor(bmp2, xHotSpot, yHotSpot);
            }
        }

        private static System.Drawing.Bitmap Convert(BitmapSource bmp)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));

            System.Drawing.Bitmap bmp2;
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                bmp2 = new System.Drawing.Bitmap(ms);
            }
            return bmp2;
        }

        private static System.Windows.Input.Cursor InternalCreateCursor(System.Drawing.Bitmap bmp, int xHotSpot, int yHotSpot)
        {
            var tmp = new IconInfo();
            GetIconInfo(bmp.GetHicon(), ref tmp);
            tmp.xHotspot = xHotSpot;
            tmp.yHotspot = yHotSpot;
            tmp.fIcon = false;

            var ptr = CreateIconIndirect(ref tmp);
            var handle = new SafeIconHandle(ptr);
            return System.Windows.Interop.CursorInteropHelper.Create(handle);
        }
    }    
}
