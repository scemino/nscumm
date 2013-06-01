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

namespace NScumm.Core.Graphics
{
    public struct PixelNavigator
    {
        private readonly int _startOffset;
        private int _offset;
        private byte[] _pixels;
        private int _pitch;
        private int _bytesByPixel;

        public PixelNavigator(Surface surface)
            : this(surface.Pixels, surface.Pitch, surface.BytesPerPixel)
        {
        }

        public PixelNavigator(byte[] pixels, int pitch, int bytesByPixel)
        {
            _startOffset = 0;
            _offset = 0;
            _pixels = pixels;
            _pitch = pitch;
            _bytesByPixel = bytesByPixel;
        }

        public PixelNavigator(PixelNavigator navigator)
        {
            _startOffset = navigator._offset;
            _offset = _startOffset;
            _pixels = navigator._pixels;
            _pitch = navigator._pitch;
            _bytesByPixel = navigator._bytesByPixel;
        }

        public void GoTo(int x, int y)
        {
            _offset = _startOffset + y * _pitch + x * _bytesByPixel;
        }

        public void GoToIgnoreBytesByPixel(int x, int y)
        {
            _offset = _startOffset + y * _pitch + x;
        }

        public void OffsetX(int x)
        {
            _offset += x * _bytesByPixel;
        }

        public void OffsetY(int y)
        {
            _offset += y * _pitch;
        }

        public void Offset(int x, int y)
        {
            _offset += y * _pitch + x * _bytesByPixel;
        }

        public void Write(byte data)
        {
            _pixels[_offset] = data;
        }

        public void WriteUInt16(ushort data)
        {
            _pixels[_offset] = (byte)(data >> 8);
            _pixels[_offset + 1] = (byte)(data & 0x00FF);
        }

        public byte Read()
        {
            return _pixels[_offset];
        }

        public ushort ReadUInt16()
        {
            ushort data = _pixels[_offset];
            data = (ushort)(data << 8 | _pixels[_offset + 1]);
            return data;
        }
    }
}
