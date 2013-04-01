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

namespace Scumm4.Graphics
{
    public interface IGraphicsManager
    {
        double Width { get; }
        double Height { get; }

        Point GetMousePosition();

        void UpdateScreen();

        void CopyRectToScreen(Array buf, int sourceStride, int x, int y, int width, int height);

        void SetPalette(System.Windows.Media.Color[] color);

        void SetCursor(byte[] pixels, int width, int height, int hotspotX, int hotspotY);
    }
}
