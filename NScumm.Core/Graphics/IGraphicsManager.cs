//
//  IGraphicsManager.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace NScumm.Core.Graphics
{
    public interface IGraphicsManager
    {
        PixelFormat PixelFormat { get; }
        int ShakePosition { get; set; }
        bool IsCursorVisible { get; set; }

        Surface Capture();
        void CopyRectToScreen(byte[] buffer, int startOffset, int sourceStride, int x, int y, int width, int height);
        void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int width, int height);
        void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int dstX, int dstY, int width, int height);
        void UpdateScreen();

        void SetPalette(Color[] color);
        void SetPalette(Color[] color, int first, int num);

        void SetCursor(byte[] pixels, int width, int height, Point hotspot);
        void SetCursor(byte[] pixels, int offset, int width, int height, Point hotspot, int keyColor);
    }
}
