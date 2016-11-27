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
        PixelFormat PixelFormat { get; set; }
        int ShakePosition { get; set; }
        bool IsCursorVisible { get; set; }
        Rect Bounds { get; }
        byte[] Pixels { get; }

        Surface Capture();
        void CopyRectToScreen(BytePtr buffer, int sourceStride, int x, int y, int width, int height);
        void CopyRectToScreen(BytePtr buffer, int sourceStride, int x, int y, int dstX, int dstY, int width, int height);
        void UpdateScreen();

        void SetPalette(Color[] color);
        Color[] GetPalette();
        void SetPalette(Color[] color, int first, int num);

        void ReplaceCursorPalette(Ptr<Color> colors, int start, int num);
        void SetCursor(BytePtr pixels, int width, int height, Point hotspot, int keyColor = 0xFF);
        void FillScreen(int color);
    }
}
