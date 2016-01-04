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

using NScumm.Core.Common;
using System;

namespace NScumm.Core.Graphics
{
    public enum PixelFormat
    {
        Indexed8,
        Rgb16,
        Rgb24
    }

    public class Surface
    {
        #region Fields

        /// <summary>
        /// The surface's pixel data.
        /// </summary>
        byte[] _buffer;

        #endregion

        #region Properties

        /// <summary>
        /// The width of the surface.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// The height of the surface.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// The number of bytes a pixel line has.
        /// </summary>
        /// <remarks>
        /// This might not equal w * bytesPerPixel.
        /// </remarks>
        public int Pitch { get; set; }

        /// <summary>
        /// Number of bytes used in the pixel format
        /// </summary>
        public int BytesPerPixel { get; private set; }

        /// <summary>
        /// The pixel format of the surface.
        /// </summary>
        public PixelFormat PixelFormat { get; private set; }

        public byte[] Pixels { get { return _buffer; } }

        #endregion

        #region Constructor

        /// <summary>
        /// Allocate memory for the pixel data of the surface.
        /// </summary>
        /// <param name="width">Width of the surface object.</param>
        /// <param name="height">Height of the surface objec.t</param>
        /// <param name="format">The pixel format the surface should use.</param>
        public Surface(int width, int height, PixelFormat format, bool trick)
        {
            if (width < 0)
                throw new ArgumentOutOfRangeException("width", "Width should be positive");
            if (height < 0)
                throw new ArgumentOutOfRangeException("height", "Height should be positive");

            Width = width;
            Height = height;
            PixelFormat = format;

            BytesPerPixel = GetBytesPerPixel(format);

            Pitch = width * BytesPerPixel;
            if (trick)
            {
                _buffer = new byte[Pitch * height + 8 * Pitch];
            }
            else
            {
                _buffer = new byte[Pitch * height];
            }
        }

        public static int GetBytesPerPixel(PixelFormat pixelFormat)
        {
            int bytesPerPixel;
            switch (pixelFormat)
            {
                case PixelFormat.Indexed8:
                    bytesPerPixel = 1;
                    break;
                case PixelFormat.Rgb16:
                    bytesPerPixel = 2;
                    break;
                case PixelFormat.Rgb24:
                    bytesPerPixel = 3;
                    break;
                default:
                    throw new ArgumentException(string.Format("Pixel format {0} is not supported", pixelFormat));
            }
            return bytesPerPixel;
        }

        #endregion

        public void Move(int dx, int dy, int height)
        {
            // Short circuit check - do we have to do anything anyway?
            if ((dx == 0 && dy == 0) || height <= 0)
                return;

            if (BytesPerPixel != 1 && BytesPerPixel != 2 && BytesPerPixel != 4)
                throw new NotSupportedException("Surface.Move: bytesPerPixel must be 1, 2, or 4");

            // vertical movement
            if (dy > 0)
            {
                // move down - copy from bottom to top
                var dstPos = (height - 1) * Pitch;
                var srcPos = dstPos - dy * Pitch;
                for (var y = dy; y < height; y++)
                {
                    Buffer.BlockCopy(Pixels, srcPos, Pixels, dstPos, Pitch);
                    srcPos -= Pitch;
                    dstPos -= Pitch;
                }
            }
            else if (dy < 0)
            {
                // move up - copy from top to bottom
                var dstPos = 0;
                var srcPos = dstPos - dy * Pitch;
                for (var y = -dy; y < height; y++)
                {
                    Buffer.BlockCopy(Pixels, srcPos, Pixels, dstPos, Pitch);
                    srcPos += Pitch;
                    dstPos += Pitch;
                }
            }

            // horizontal movement
            if (dx > 0)
            {
                // move right - copy from right to left
                var dstPos = (Pitch - BytesPerPixel);
                var srcPos = dstPos - (dx * BytesPerPixel);
                for (var y = 0; y < height; y++)
                {
                    for (var x = dx; x < Width; x++)
                    {
                        if (BytesPerPixel == 1)
                        {
                            Pixels[dstPos--] = Pixels[srcPos--];
                        }
                        else if (BytesPerPixel == 2)
                        {
                            Array.Copy(Pixels, srcPos, Pixels, dstPos, 2);
                            srcPos -= 2;
                            dstPos -= 2;
                        }
                        else if (BytesPerPixel == 4)
                        {
                            Array.Copy(Pixels, srcPos, Pixels, dstPos, 4);
                            srcPos -= 4;
                            dstPos -= 4;
                        }
                    }
                    srcPos += Pitch + (Pitch - dx * BytesPerPixel);
                    dstPos += Pitch + (Pitch - dx * BytesPerPixel);
                }
            }
            else if (dx < 0)
            {
                // move left - copy from left to right
                var dstPos = 0;
                var srcPos = dstPos - (dx * BytesPerPixel);
                for (var y = 0; y < height; y++)
                {
                    for (var x = -dx; x < Width; x++)
                    {
                        if (BytesPerPixel == 1)
                        {
                            Pixels[dstPos++] = Pixels[srcPos++];
                        }
                        else if (BytesPerPixel == 2)
                        {
                            Array.Copy(Pixels, srcPos, Pixels, dstPos, 2);
                            srcPos += 2;
                            dstPos += 2;
                        }
                        else if (BytesPerPixel == 4)
                        {
                            Array.Copy(Pixels, srcPos, Pixels, dstPos, 4);
                            srcPos += 4;
                            dstPos += 4;
                        }
                    }
                    srcPos += Pitch - (Pitch + dx * BytesPerPixel);
                    dstPos += Pitch - (Pitch + dx * BytesPerPixel);
                }
            }
        }

        public void FillRect(Rect r, uint color)
        {
            r.Clip(Width, Height);

            if (!r.IsValidRect)
                return;

            int width = r.Width;
            int lineLen = width;
            int height = r.Height;
            bool useMemset = true;

            var bpp = GetBytesPerPixel(PixelFormat);
            if (bpp == 2)
            {
                lineLen *= 2;
                if ((ushort)color != ((color & 0xff) | (color & 0xff) << 8))
                    useMemset = false;
            }
            else if (bpp == 4)
            {
                useMemset = false;
            }
            else if (bpp != 1)
            {
                throw new InvalidOperationException("Surface::fillRect: bytesPerPixel must be 1, 2, or 4");
            }

            if (useMemset)
            {
                var ptr = GetBasePtr(r.Left, r.Top);
                while ((height--) != 0)
                {
                    ptr.Data.Set(ptr.Offset, (byte)color, lineLen);
                    ptr.Offset += Pitch;
                }
            }
            else {
                if (bpp == 2)
                {
                    throw new NotImplementedException();
                    //var ptr = GetBasePtr(r.Left, r.Top).ToUInt16();
                    //while ((height--)!=0)
                    //{
                    //    Fill(ptr, ptr + width, (ushort)color);
                    //    ptr.Offset += Pitch;
                    //}
                }
                else {
                    throw new NotImplementedException();
                    //var ptr = GetBasePtr(r.Left, r.Top).ToUInt32();
                    //while (height--)
                    //{
                    //    Common::fill(ptr, ptr + width, color);
                    //    ptr += pitch / 4;
                    //}
                }
            }
        }

        private ByteAccess GetBasePtr(int x, int y)
        {
            return new ByteAccess(Pixels, y * Pitch + x * GetBytesPerPixel(PixelFormat));
        }
    }
}
