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
using System.Diagnostics;

namespace NScumm.Core.Graphics
{
    /// <summary>
    /// The scale of the luminance values.
    /// </summary>
    public enum LuminanceScale
    {
        /// <summary>
        /// Luminance values range from [0, 255]
        /// </summary>
        ScaleFull,
        /// <summary>
        /// Luminance values range from [16, 235], the range from ITU-R BT.601
        /// </summary>
        ScaleITU
    }

    internal class YUVToRGBLookup
    {
        public YUVToRGBLookup(PixelFormat format, LuminanceScale scale)
        {
            _format = format;
            _scale = scale;

            var r_2_pix_alloc = 0 * 768;
            var g_2_pix_alloc = 1 * 768;
            var b_2_pix_alloc = 2 * 768;

            if (scale == LuminanceScale.ScaleFull)
            {
                // Set up entries 0-255 in rgb-to-pixel value tables.
                for (int i = 0; i < 256; i++)
                {
                    _rgbToPix[r_2_pix_alloc + i + 256] = ColorHelper.RGBToColor((byte)i, 0, 0);
                    _rgbToPix[g_2_pix_alloc + i + 256] = ColorHelper.RGBToColor(0, (byte)i, 0);
                    _rgbToPix[b_2_pix_alloc + i + 256] = ColorHelper.RGBToColor(0, 0, (byte)i);
                }

                // Spread out the values we have to the rest of the array so that we do
                // not need to check for overflow.
                for (int i = 0; i < 256; i++)
                {
                    _rgbToPix[r_2_pix_alloc + i] = _rgbToPix[r_2_pix_alloc + 256];
                    _rgbToPix[r_2_pix_alloc + i + 512] = _rgbToPix[r_2_pix_alloc + 511];
                    _rgbToPix[g_2_pix_alloc + i] = _rgbToPix[g_2_pix_alloc + 256];
                    _rgbToPix[g_2_pix_alloc + i + 512] = _rgbToPix[g_2_pix_alloc + 511];
                    _rgbToPix[b_2_pix_alloc + i] = _rgbToPix[b_2_pix_alloc + 256];
                    _rgbToPix[b_2_pix_alloc + i + 512] = _rgbToPix[b_2_pix_alloc + 511];
                }
            }
            else {
                // Set up entries 16-235 in rgb-to-pixel value tables
                for (int i = 16; i < 236; i++)
                {
                    byte scaledValue = (byte)((i - 16) * 255 / 219);
                    _rgbToPix[r_2_pix_alloc + i + 256] = ColorHelper.RGBToColor(scaledValue, 0, 0);
                    _rgbToPix[g_2_pix_alloc + i + 256] = ColorHelper.RGBToColor(0, scaledValue, 0);
                    _rgbToPix[b_2_pix_alloc + i + 256] = ColorHelper.RGBToColor(0, 0, scaledValue);
                }

                // Spread out the values we have to the rest of the array so that we do
                // not need to check for overflow. We have to do it here in two steps.
                for (int i = 0; i < 256 + 16; i++)
                {
                    _rgbToPix[r_2_pix_alloc + i] = _rgbToPix[r_2_pix_alloc + 256 + 16];
                    _rgbToPix[g_2_pix_alloc + i] = _rgbToPix[g_2_pix_alloc + 256 + 16];
                    _rgbToPix[b_2_pix_alloc + i] = _rgbToPix[b_2_pix_alloc + 256 + 16];
                }

                for (int i = 256 + 236; i < 768; i++)
                {
                    _rgbToPix[r_2_pix_alloc + i] = _rgbToPix[r_2_pix_alloc + 256 + 236 - 1];
                    _rgbToPix[g_2_pix_alloc + i] = _rgbToPix[g_2_pix_alloc + 256 + 236 - 1];
                    _rgbToPix[b_2_pix_alloc + i] = _rgbToPix[b_2_pix_alloc + 256 + 236 - 1];
                }
            }
        }

        public PixelFormat PixelFormat { get { return _format; } }
        public LuminanceScale Scale { get { return _scale; } }
        public uint[] GetRGBToPix() { return _rgbToPix; }

        private PixelFormat _format;
        private LuminanceScale _scale;
        private uint[] _rgbToPix = new uint[3 * 768]; // 9216 bytes
    }

    public static class YUVToRGBManager
    {
        private static YUVToRGBLookup _lookup;

        private static Lazy<short[]> _colorTab = new Lazy<short[]>(() =>
        {
            var tmp = new short[4 * 256]; // 2048 bytes

            var Cr_r_tab = 0;
            var Cr_g_tab = 1 * 256;
            var Cb_g_tab = 2 * 256;
            var Cb_b_tab = 3 * 256;

            // Generate the tables for the display surface

            for (int i = 0; i < 256; i++)
            {
                // Gamma correction (luminescence table) and chroma correction
                // would be done here. See the Berkeley mpeg_play sources.

                short CR = (short)(i - 128), CB = CR;
                tmp[Cr_r_tab + i] = (short)(((0.419 / 0.299) * CR) + 0 * 768 + 256);
                tmp[Cr_g_tab + i] = (short)((-(0.299 / 0.419) * CR) + 1 * 768 + 256);
                tmp[Cb_g_tab + i] = (short)((-(0.114 / 0.331) * CB));
                tmp[Cb_b_tab + i] = (short)(((0.587 / 0.331) * CB) + 2 * 768 + 256);
            }

            return tmp;
        });

        public static void Convert420(Surface dst, LuminanceScale scale, byte[] ySrc, byte[] uSrc, byte[] vSrc, int yWidth, int yHeight, int yPitch, int uvPitch)
        {
            // Sanity checks
            Debug.Assert(dst != null && dst.Pixels != BytePtr.Null);
            Debug.Assert(dst.BytesPerPixel == 2 || dst.BytesPerPixel == 4);

            Debug.Assert(ySrc != null && uSrc != null && vSrc != null);

            Debug.Assert((yWidth & 1) == 0);
            Debug.Assert((yHeight & 1) == 0);

            var lookup = GetLookup(dst.PixelFormat, scale);

            // Use a templated function to avoid an if check on every pixel
            if (dst.BytesPerPixel == 2)
                ConvertYUV420ToRGB(dst.Pixels, dst.Pitch, lookup, _colorTab.Value, ySrc, uSrc, vSrc, yWidth, yHeight, yPitch, uvPitch, sizeof(ushort));
            else
                ConvertYUV420ToRGB(dst.Pixels, dst.Pitch, lookup, _colorTab.Value, ySrc, uSrc, vSrc, yWidth, yHeight, yPitch, uvPitch, sizeof(uint));
        }

        private static YUVToRGBLookup GetLookup(PixelFormat format, LuminanceScale scale)
        {
            if (_lookup != null && _lookup.PixelFormat == format && _lookup.Scale == scale)
                return _lookup;

            _lookup = new YUVToRGBLookup(format, scale);
            return _lookup;
        }

        private static void ConvertYUV420ToRGB(BytePtr dstPtr, int dstPitch, YUVToRGBLookup lookup, short[] colorTab,
            byte[] ySrc, byte[] uSrc, byte[] vSrc, int yWidth, int yHeight, int yPitch, int uvPitch, int size)
        {
            var putPixel = size == sizeof(uint) ? ScummHelper.WriteUInt32 : new Action<byte[], int, uint>((dst, offset, value) => dst.WriteUInt16(offset, (ushort)value));

            int halfHeight = yHeight >> 1;
            int halfWidth = yWidth >> 1;

            // Keep the tables in pointers here to avoid a dereference on each pixel
            var Cr_r_tab = 0;
            var Cr_g_tab = 256;
            var Cb_g_tab = Cr_g_tab + 256;
            var Cb_b_tab = Cb_g_tab + 256;

            var y = 0;
            var u = 0;
            var v = 0;
            var d = 0;

            for (int h = 0; h < halfHeight; h++)
            {
                for (int w = 0; w < halfWidth; w++)
                {
                    short cr_r = colorTab[Cr_r_tab + vSrc[v]];
                    short crb_g = (short)(colorTab[Cr_g_tab + vSrc[v]] + colorTab[Cb_g_tab + uSrc[u]]);
                    short cb_b = colorTab[Cb_b_tab + uSrc[u]];
                    ++u;
                    ++v;

                    var value = new Func<int, uint>(s =>
                    {
                        var rgbToPix = lookup.GetRGBToPix();
                        var val = (rgbToPix[s + cr_r] | rgbToPix[s + crb_g] | rgbToPix[s + cb_b]);
                        return val;
                    });

                    putPixel(dstPtr.Data, dstPtr.Offset+ d, value(ySrc[y]));
                    putPixel(dstPtr.Data, dstPtr.Offset + d + dstPitch, value(ySrc[y + yPitch]));
                    y++;
                    d += size;

                    putPixel(dstPtr.Data, dstPtr.Offset + d, value(ySrc[y]));
                    putPixel(dstPtr.Data, dstPtr.Offset + d + dstPitch, value(ySrc[y + yPitch]));
                    y++;
                    d += size;
                }

                d += dstPitch;
                y += (yPitch << 1) - yWidth;
                u += uvPitch - halfWidth;
                v += uvPitch - halfWidth;
            }
        }
    }
}
