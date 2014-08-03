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

namespace NScumm.Core.Graphics
{
	public struct Point: IEquatable<Point>
    {
        /// <summary>
        /// The horizontal part of the point.
        /// </summary>
        public short X;
        /// <summary>
        /// The vertical part of the point
        /// </summary>
        public short Y;

        public Point(short x, short y)
        {
            X = x;
            Y = y;
        }

        public Point(Point pos)
        {
            X = pos.X;
            Y = pos.Y;
        }

        public static bool operator ==(Point pos1, Point pos2)
        {
            return pos1.X == pos2.X && pos1.Y == pos2.Y;
        }

        public static bool operator !=(Point pos1, Point pos2)
        {
            return !(pos1 == pos2);
        }

        public override int GetHashCode()
        {
            return X ^ Y;
        }

		public bool Equals(Point point)
		{
			return point.X == X && point.Y == Y;
		}

        public override bool Equals(object obj)
        {
            if (obj is Point)
            {
				return Equals ((Point)obj);
            }
            return false;
        }

		public Point Offset(short x, short y)
        {
            X += x;
            Y += y;
			return this;
        }

        /// <summary>
        /// Computes the square of the distance between this point and the point p.
        /// </summary>
        /// <param name="p">The other point.</param>
        /// <returns>The distance between this and p.</returns>
        public uint SquareDistance(Point p)
        {
            int diffx = Math.Abs(p.X - X);
            if (diffx >= 0x1000)
                return 0xFFFFFF;

            int diffy = Math.Abs(p.Y - Y);
            if (diffy >= 0x1000)
                return 0xFFFFFF;

            return (uint)(diffx * diffx + diffy * diffy);
        }
    }
}
