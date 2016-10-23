//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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

#if ENABLE_SCI32

using NScumm.Core;

namespace NScumm.Sci.Graphics
{
    internal class CelScalerTable
    {
        /**
         * A lookup table of indexes that should be used to find
         * the correct column to read from the source bitmap
         * when drawing a scaled version of the source bitmap.
         */
        public int[] valuesX = new int[1024];

        /**
         * The ratio used to generate the x-values.
         */
        public Rational scaleX;

        /**
         * A lookup table of indexes that should be used to find
         * the correct row to read from a source bitmap when
         * drawing a scaled version of the source bitmap.
         */
        public int[] valuesY = new int[1024];

        /**
         * The ratio used to generate the y-values.
         */
        public Rational scaleY;
    }


    internal class CelScaler
    {
        /// <summary>
        /// Cached scale tables
        /// </summary>
        private readonly CelScalerTable[] _scaleTables;

        /// <summary>
        /// The index of the most recently used scale table.
        /// </summary>
        private int _activeIndex;

        /**
         * Activates a scale table for the given X and Y ratios.
         * If there is no table that matches the given ratios,
         * the least most recently used table will be replaced
         * and activated.
         */

        private void ActivateScaleTables(ref Rational scaleX, ref Rational scaleY)
        {
            short screenWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenWidth;
            short screenHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScreenHeight;

            for (var i = 0; i < _scaleTables.Length; ++i)
            {
                if (_scaleTables[i].scaleX == scaleX && _scaleTables[i].scaleY == scaleY)
                {
                    _activeIndex = i;
                    return;
                }
            }

            int index = 1 - _activeIndex;
            _activeIndex = index;
            CelScalerTable table = _scaleTables[index];

            if (table.scaleX != scaleX)
            {
                System.Diagnostics.Debug.Assert(screenWidth <= table.valuesX.Length);
                BuildLookupTable(table.valuesX, ref scaleX, screenWidth);
                table.scaleX = scaleX;
            }

            if (table.scaleY != scaleY)
            {
                System.Diagnostics.Debug.Assert(screenHeight <= table.valuesY.Length);
                BuildLookupTable(table.valuesY, ref scaleY, screenHeight);
                table.scaleY = scaleY;
            }
        }

        /**
         * Builds a pixel lookup table in `table` for the given
         * ratio. The table will be filled up to the specified
         * size, which should be large enough to draw across the
         * entire target buffer.
         */

        private void BuildLookupTable(int[] table, ref Rational ratio, int size)
        {
            var t = new Ptr<int>(table);
            int value = 0;
            int remainder = 0;
            int num = ratio.Numerator;
            for (int i = 0; i < size; ++i)
            {
                t.Value = value;
                t.Offset++;
                remainder += ratio.Denominator;
                if (remainder >= num)
                {
                    value += remainder / num;
                    remainder %= num;
                }
            }
        }

        public CelScaler()
        {
            _scaleTables = new CelScalerTable[2];
            for (int i = 0; i < _scaleTables.Length; i++)
            {
                _scaleTables[i] = new CelScalerTable();
            }
            var table = _scaleTables[0];
            table.scaleX = new Rational();
            table.scaleY = new Rational();
            for (int i = 0; i < table.valuesX.Length; ++i)
            {
                table.valuesX[i] = i;
                table.valuesY[i] = i;
            }
            for (int i = 1; i < _scaleTables.Length; ++i)
            {
                _scaleTables[i] = _scaleTables[0];
            }
        }

        /// <summary>
        /// Retrieves scaler tables for the given X and Y ratios.
        /// </summary>
        /// <param name="scaleX"></param>
        /// <param name="scaleY"></param>
        /// <returns></returns>
        public CelScalerTable GetScalerTable(ref Rational scaleX, ref Rational scaleY)
        {
            ActivateScaleTables(ref scaleX, ref scaleY);
            return _scaleTables[_activeIndex];
        }
    }
}

#endif