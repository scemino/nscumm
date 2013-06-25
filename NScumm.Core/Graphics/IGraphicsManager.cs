﻿/*
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
    public interface IGraphicsManager
    {
        void UpdateScreen();

        void SetShakePos(int i);
        void CopyRectToScreen(byte[] buffer, int sourceStride, int x, int y, int width, int height);

        void SetPalette(Color[] color);
        void SetPalette(Color[] color, int first, int num);

        void SetCursor(byte[] pixels, int width, int height, int hotspotX, int hotspotY);

        void ShowCursor(bool show);
    }
}