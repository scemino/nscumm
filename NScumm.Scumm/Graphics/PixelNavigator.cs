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

using NScumm.Core;
using NScumm.Core.Graphics;

namespace NScumm.Scumm.Graphics
{
    public struct PixelNavigator
    {
        readonly int _startOffset;
        readonly byte[] _pixels;
        int _offset;

        public int Pitch
        {
            get;
            private set;
        }

        public int BytesByPixel
        {
            get;
            private set;
        }

        public int Width
        {
            get;
            private set;
        }

        public int Height
        {
            get;
            private set;
        }

        public PixelNavigator(Surface surface)
            : this(surface.Pixels, surface.Width, surface.BytesPerPixel)
        {
        }

        public PixelNavigator(byte[] pixels, int width, int bytesByPixel)
            : this()
        {
            _startOffset = 0;
            _offset = 0;
            _pixels = pixels;
            Pitch = width * bytesByPixel;
            BytesByPixel = bytesByPixel;
            Width = width;
            Height = pixels.Length / Pitch;
        }

        public PixelNavigator(PixelNavigator navigator)
            : this()
        {
            _startOffset = navigator._offset;
            _offset = _startOffset;
            _pixels = navigator._pixels;
            Pitch = navigator.Pitch;
            BytesByPixel = navigator.BytesByPixel;
            Width = navigator.Width;
            Height = navigator.Height;
        }

        public void GoTo(int x, int y)
        {
            _offset = _startOffset + y * Pitch + x * BytesByPixel;
        }

        public void GoToIgnoreBytesByPixel(int x, int y)
        {
            _offset = _startOffset + y * Pitch + x;
        }

        public void OffsetX(int x)
        {
            _offset += x * BytesByPixel;
        }

        public void OffsetY(int y)
        {
            _offset += y * Pitch;
        }

        public void Offset(int x, int y)
        {
            _offset += y * Pitch + x * BytesByPixel;
        }

        public void Write(byte data)
        {
            _pixels[_offset] = data;
        }

        public void Set(byte data, int length)
        {
            _pixels.Set(_offset, data, length);
        }

        public void Set(ushort data, int length)
        {
            for (int i = 0; i < length; i++)
            {
                _pixels.WriteUInt16(_offset + i * 2, data);
            }
        }

        public void Set(uint data, int length)
        {
            for (int i = 0; i < length; i++)
            {
                _pixels.WriteUInt32(_offset + i * 4, data);
            }
        }

        public void Write(int offset, byte data)
        {
            _pixels[_offset + offset] = data;
        }

        public void WriteUInt16(ushort data)
        {
            _pixels.WriteUInt16(_offset, data);
        }

        public void WriteUInt16(int offset, ushort data)
        {
            _pixels.WriteUInt16(_offset + offset, data);
        }

        public byte Read()
        {
            return _pixels[_offset];
        }

        public byte Read(int offset)
        {
            return _pixels[_offset + offset];
        }

        public ushort ReadUInt16()
        {
            ushort data = _pixels[_offset];
            data = (ushort)(data << 8 | _pixels[_offset + 1]);
            return data;
        }
    }
}
