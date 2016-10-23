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

using NScumm.Core;
using NScumm.Core.Graphics;

namespace NScumm.Sci.Graphics
{
    internal class Buffer : Surface
    {
        private readonly ushort _screenWidth;
        private readonly ushort _screenHeight;

        public bool IsNull => Pixels == BytePtr.Null;
        public ushort ScreenWidth => _screenWidth;
        public ushort ScreenHeight => _screenHeight;
        public ushort ScriptWidth { get; set; }
        public ushort ScriptHeight { get; set; }

        public Buffer(ushort width, ushort height, BytePtr pixels)
            : base(width, height, pixels, PixelFormat.Indexed8)
        {
            _screenWidth = width;
            _screenHeight = height;
            // TODO: These values are not correct for all games. Script
            // dimensions were hard-coded per game in the original
            // interpreter. Search all games for their internal script
            // dimensions and set appropriately. (This code does not
            // appear to exist at all in SCI3, which uses 640x480.)
            ScriptWidth = 320;
            ScriptHeight = 200;
        }

        public void Clear(byte value)
        {
            Pixels.Data.Set(0, value, Width * Height);
        }

        public BytePtr GetAddress(ushort x, ushort y)
        {
            return GetBasePtr(x, y);
        }

        public BytePtr GetAddressSimRes(ushort x, ushort y)
        {
            return new BytePtr(Pixels, y * Width * _screenHeight / ScriptHeight + x * _screenWidth / ScriptWidth);
        }
    }
}