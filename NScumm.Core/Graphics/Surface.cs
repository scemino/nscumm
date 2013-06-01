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
        private byte[] _buffer;
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
        public int Pitch { get; private set; }

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
            if (width < 0) throw new ArgumentOutOfRangeException("width", width, "Width should be positive");
            if (height < 0) throw new ArgumentOutOfRangeException("height", height, "Height should be positive");

            this.Width = width;
            this.Height = height;
            this.PixelFormat = format;

            switch (this.PixelFormat)
            {
                case PixelFormat.Indexed8:
                    this.BytesPerPixel = 1;
                    break;
                case PixelFormat.Rgb16:
                    this.BytesPerPixel = 2;
                    break;
                case PixelFormat.Rgb24:
                    this.BytesPerPixel = 3;
                    break;
                default:
                    break;
            }

            this.Pitch = width * BytesPerPixel;
            if (trick)
            {
                this._buffer = new byte[this.Pitch * height + 4 * this.Pitch];
            }
            else
            {
                this._buffer = new byte[this.Pitch * height];
            }
        }
        #endregion
    }
}
