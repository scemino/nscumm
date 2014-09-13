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
using System.Collections.ObjectModel;

namespace NScumm.Core.Graphics
{
    public class VirtScreen
    {
        #region Fields

        Surface[] _surfaces;
        ReadOnlyCollection<Surface> _roSurfaces;

        /// <summary>
        /// Array containing for each visible strip of this virtual screen the
        /// coordinate at which the dirty region of that strip starts.
        /// 't' stands for 'top' - the top coordinate of the dirty region.
        /// This together with bdirty is used to do efficient redrawing of
        /// the screen.
        /// </summary>
        int[] tdirty = new int[80 + 1];

        /// <summary>
        /// Array containing for each visible strip of this virtual screen the
        /// coordinate at which the dirty region of that strip end.
        /// 'b' stands for 'bottom' - the bottom coordinate of the dirty region.
        /// This together with tdirty is used to do efficient redrawing of
        /// the screen.
        /// </summary>
        int[] bdirty = new int[80 + 1];

        #endregion

        #region Properties

        public ReadOnlyCollection<Surface> Surfaces
        {
            get { return _roSurfaces; }
        }

        public int[] TDirty
        {
            get { return tdirty; }
        }

        public int[] BDirty
        {
            get { return bdirty; }
        }

        public ushort XStart { get; set; }

        /// <summary>
        /// Vertical position of the virtual screen. Tells how much the virtual
        /// screen is shifted along the y axis relative to the real screen.
        /// </summary>
        public int TopLine { get; set; }

        public bool HasTwoBuffers { get; set; }

        public int Pitch { get { return Surfaces[0].Pitch; } }

        public PixelFormat PixelFormat { get { return Surfaces[0].PixelFormat; } }

        public int BytesPerPixel { get { return Surfaces[0].BytesPerPixel; } }

        public int Width { get { return Surfaces[0].Width; } }

        public int Height { get { return Surfaces[0].Height; } }

        #endregion

        #region Constructor

        public VirtScreen(int top, int width, int height, PixelFormat format, int numBuffers, bool trick = false)
        {
            if (numBuffers <= 0)
                throw new ArgumentOutOfRangeException("numBuffers", numBuffers, "The number of buffers should be positive.");

            TopLine = top;
            _surfaces = new Surface[numBuffers];
            _roSurfaces = new ReadOnlyCollection<Surface>(_surfaces);
            for (int i = 0; i < numBuffers; i++)
            {
                _surfaces[i] = new Surface(width, height, format, trick);
            }

            SetDirtyRange(0, height);
            HasTwoBuffers = Surfaces.Count == 2;
        }

        #endregion

        #region Methods

        public void SetDirtyRange(int top, int bottom)
        {
            for (int i = 0; i < 80 + 1; i++)
            {
                tdirty[i] = top;
                bdirty[i] = bottom;
            }
        }

        #endregion
    }
}
