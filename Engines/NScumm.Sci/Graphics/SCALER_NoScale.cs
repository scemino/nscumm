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
    internal class SCALER_NoScale : IScaler
    {
#if !NDEBUG
        private BytePtr _rowEdge;
#endif
        private BytePtr _row;
        private readonly IReader _reader;
        private readonly short _lastIndex;
        private readonly short _sourceX;
        private readonly short _sourceY;
        private readonly bool _flip;

        public SCALER_NoScale(IReader reader, bool flip, CelObj celObj, Point scaledPosition)
        {
            _flip = flip;
            _reader = reader;
            _lastIndex = (short) (celObj._width - 1);
            _sourceX = (short) scaledPosition.X;
            _sourceY = (short) scaledPosition.Y;
        }

        public void SetTarget(short x, short y)
        {
            _row = _reader.GetRow((short) (y - _sourceY));

            if (_flip)
            {
#if !NDEBUG
                _rowEdge = new BytePtr(_row, -1);
#endif
                _row.Offset += _lastIndex - (x - _sourceX);
                Debug.Assert(_row.Offset > _rowEdge.Offset);
            }
            else
            {
#if !NDEBUG
                _rowEdge = new BytePtr(_row, _lastIndex + 1);
#endif
                _row.Offset += x - _sourceX;
                Debug.Assert(_row.Offset < _rowEdge.Offset);
            }
        }

        public byte Read()
        {
            Debug.Assert(_row != _rowEdge);

            byte r;
            if (_flip)
            {
                r = _row.Value;
                _row.Offset--;
                return r;
            }
            r = _row.Value;
            _row.Offset++;
            return r;
        }
    }
}
#endif
