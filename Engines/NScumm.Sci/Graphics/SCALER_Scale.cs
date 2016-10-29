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

using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.Graphics;

namespace NScumm.Sci.Graphics
{
    internal class SCALER_Scale : IScaler
    {
#if !NDEBUG
        private readonly short _maxX;
#endif
        private BytePtr _row;
        private readonly IReader _reader;
        private short _x;
        private static readonly short[] _valuesX = new short[1024];
        private static readonly short[] _valuesY = new short[1024];

        public SCALER_Scale(IReader reader, bool flip, CelObj celObj, Rect targetRect, Point scaledPosition, Rational scaleX,
            Rational scaleY)
        {
#if !NDEBUG
            _maxX = (short) (targetRect.Right - 1);
#endif
            // The maximum width of the scaled object may not be as
            // wide as the source data it requires if downscaling,
            // so just always make the reader decompress an entire
            // line of source data when scaling
            _reader = reader;
            // In order for scaling ratios to apply equally across objects that
            // start at different positions on the screen (like the cels of a
            // picture), the pixels that are read from the source bitmap must all
            // use the same pattern of division. In other words, cels must follow
            // a global scaling pattern as if they were always drawn starting at an
            // even multiple of the scaling ratio, even if they are not.
            //
            // To get the correct source pixel when reading out through the scaler,
            // the engine creates a lookup table for each axis that translates
            // directly from target positions to the indexes of source pixels using
            // the global cadence for the given scaling ratio.
            //
            // Note, however, that not all games use the global scaling mode.
            //
            // SQ6 definitely uses the global scaling mode (an easy visual
            // comparison is to leave Implants N' Stuff and then look at Roger);
            // Torin definitely does not (scaling subtitle backgrounds will cause it
            // to attempt a read out of bounds and crash). They are both SCI
            // "2.1mid" games, so currently the common denominator looks to be that
            // games which use global scaling are the ones that use low-resolution
            // script coordinates too.

            var table = CelObj._scaler.GetScalerTable(ref scaleX, ref scaleY);

            if (SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth == LowRes.X)
            {
                var unscaledX = (short) (new Rational(scaledPosition.X) / scaleX);
                if (flip)
                {
                    var lastIndex = celObj._width - 1;
                    for (var x = targetRect.Left; x < targetRect.Right; ++x)
                    {
                        _valuesX[x] = (short) (lastIndex - (table.valuesX[x] - unscaledX));
                    }
                }
                else
                {
                    for (var x = targetRect.Left; x < targetRect.Right; ++x)
                    {
                        _valuesX[x] = (short) (table.valuesX[x] - unscaledX);
                    }
                }

                var unscaledY = (short) (scaledPosition.Y / scaleY);
                for (var y = targetRect.Top; y < targetRect.Bottom; ++y)
                {
                    _valuesY[y] = (short) (table.valuesY[y] - unscaledY);
                }
            }
            else
            {
                if (flip)
                {
                    var lastIndex = celObj._width - 1;
                    for (var x = 0; x < targetRect.Width; ++x)
                    {
                        _valuesX[targetRect.Left + x] = (short) (lastIndex - table.valuesX[x]);
                    }
                }
                else
                {
                    for (var x = 0; x < targetRect.Width; ++x)
                    {
                        _valuesX[targetRect.Left + x] = (short) table.valuesX[x];
                    }
                }

                for (var y = 0; y < targetRect.Height; ++y)
                {
                    _valuesY[targetRect.Top + y] = (short) table.valuesY[y];
                }
            }
        }

        public void SetTarget(short x, short y)
        {
            _row = _reader.GetRow(_valuesY[y]);
            _x = x;
            Debug.Assert(_x >= 0 && _x <= _maxX);
        }

        public byte Read()
        {
            Debug.Assert(_x >= 0 && _x <= _maxX);
            return _row[_valuesX[_x++]];
        }
    }
}

#endif
