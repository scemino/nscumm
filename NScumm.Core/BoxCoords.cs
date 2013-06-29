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


using NScumm.Core.Graphics;

namespace NScumm.Core
{
    /// <summary>
    /// Box coordinates.
    /// </summary>
    public class BoxCoords
    {
        public Point Ul = new Point();
        public Point Ur = new Point();
        public Point Ll = new Point();
        public Point Lr = new Point();

        public bool InBoxQuickReject(Point p, int threshold)
        {
            int t = p.X - threshold;
            if (t > Ul.X && t > Ur.X && t > Lr.X && t > Ll.X)
                return true;

            t = p.X + threshold;
            if (t < Ul.X && t < Ur.X && t < Lr.X && t < Ll.X)
                return true;

            t = p.Y - threshold;
            if (t > Ul.Y && t > Ur.Y && t > Lr.Y && t > Ll.Y)
                return true;

            t = p.Y + threshold;
            if (t < Ul.Y && t < Ur.Y && t < Lr.Y && t < Ll.Y)
                return true;

            return false;
        }
    }
}
