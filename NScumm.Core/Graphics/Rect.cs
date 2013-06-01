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
        public int top, left;
        public int bottom, right;

        public int Height
        {
            get { return bottom - top; }
        }

        public int Width
        {
            get { return right - left; }
        }

        public Rect(int x1, int y1, int x2, int y2)
        {
            top = y1;
            left = x1;
            bottom = y2;
            right = x2;
        }

        public void Clip(int maxw, int maxh)
        {
            Clip(new Rect(0, 0, maxw, maxh));
        }

        public void Clip(Rect r)
        {
            if (top < r.top) top = r.top;
            else if (top > r.bottom) top = r.bottom;

            if (left < r.left) left = r.left;
            else if (left > r.right) left = r.right;

            if (bottom > r.bottom) bottom = r.bottom;
            else if (bottom < r.top) bottom = r.top;

            if (right > r.right) right = r.right;
            else if (right < r.left) right = r.left;
        }
    }
}
