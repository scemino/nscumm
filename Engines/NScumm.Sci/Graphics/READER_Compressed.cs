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

using System;
using System.Diagnostics;
using NScumm.Core;

namespace NScumm.Sci.Graphics
{
    internal class READER_Compressed: IReader
    {
        private BytePtr _resource;
        private readonly byte[] _buffer = new byte[1024];
        private readonly uint _controlOffset;
        private readonly uint _dataOffset;
        private readonly uint _uncompressedDataOffset;
        private short _y;
        private readonly short _sourceHeight;
        private readonly byte _transparentColor;
        private readonly short _maxWidth;

        public READER_Compressed(CelObj celObj, short maxWidth)
        {
            _resource = celObj.GetResPointer();
            _y = -1;
            _sourceHeight = (short) celObj._height;
            _transparentColor = celObj._transparentColor;
            _maxWidth = maxWidth;
            Debug.Assert(maxWidth <= celObj._width);

            var celHeader = new BytePtr(_resource, (int) celObj._celHeaderOffset);
            _dataOffset = celHeader.Data.ReadSci11EndianUInt32(celHeader.Offset + 24);
            _uncompressedDataOffset = celHeader.Data.ReadSci11EndianUInt32(celHeader.Offset + 28);
            _controlOffset = celHeader.Data.ReadSci11EndianUInt32(celHeader.Offset + 32);
        }

        public BytePtr GetRow(short y)
        {
            Debug.Assert(y >= 0 && y < _sourceHeight);
            if (y == _y) return _buffer;

            // compressed data segment for row
            var row = new BytePtr(_resource,
                (int)
                (_dataOffset +
                 _resource.Data.ReadSci11EndianUInt32((int) (_resource.Offset + _controlOffset + y * 4))));

            // uncompressed data segment for row
            var literal = new BytePtr(_resource, (int) (
                _uncompressedDataOffset +
                _resource.Data.ReadSci11EndianUInt32(
                    (int) (_resource.Offset + _controlOffset + _sourceHeight * 4 + y * 4))));

            byte length;
            for (var i = 0; i < _maxWidth; i += length)
            {
                var controlByte = row.Value;
                row.Offset++;
                length = controlByte;

                // Run-length encoded
                if ((controlByte & 0x80) != 0)
                {
                    length &= 0x3F;
                    Debug.Assert(i + length < _buffer.Length);

                    // Fill with skip color
                    if ((controlByte & 0x40) != 0)
                    {
                        _buffer.Set(i, _transparentColor, length);
                        // Next value is fill color
                    }
                    else
                    {
                        _buffer.Set(i, literal.Value, length);
                        ++literal.Offset;
                    }
                    // Uncompressed
                }
                else
                {
                    Debug.Assert(i + length < _buffer.Length);
                    Array.Copy(literal.Data, literal.Offset, _buffer, i, length);
                    literal.Offset += length;
                }
            }
            _y = y;

            return _buffer;
        }
    }
}

#endif
