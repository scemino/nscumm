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
    public struct Rect
    {
        public int Top, Left;
        public int Bottom, Right;

        public int Height
        {
            get { return Bottom - Top; }
        }

        public int Width
        {
            get { return Right - Left; }
        }

        public Rect(int x1, int y1, int x2, int y2)
        {
            Top = y1;
            Left = x1;
            Bottom = y2;
            Right = x2;
        }

        public void Clip(int maxw, int maxh)
        {
            Clip(new Rect(0, 0, maxw, maxh));
        }

        public void Clip(Rect r)
        {
            if (Top < r.Top) Top = r.Top;
            else if (Top > r.Bottom) Top = r.Bottom;

            if (Left < r.Left) Left = r.Left;
            else if (Left > r.Right) Left = r.Right;

            if (Bottom > r.Bottom) Bottom = r.Bottom;
            else if (Bottom < r.Top) Bottom = r.Top;

            if (Right > r.Right) Right = r.Right;
            else if (Right < r.Left) Right = r.Left;
        }
    }
}
