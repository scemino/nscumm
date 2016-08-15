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

using System;

namespace NScumm.Core.Graphics
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct Point: IEquatable<Point>
    {
        /// <summary>
        /// The horizontal part of the point.
        /// </summary>
        public int X;
        /// <summary>
        /// The vertical part of the point
        /// </summary>
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Point(Point pos)
        {
            X = pos.X;
            Y = pos.Y;
        }

        public static Point operator -(Point pos1, Point pos2)
        {
            return new Point(pos1.X - pos2.X, pos1.Y - pos2.Y);
        }

        public static Point operator +(Point pos1, Point pos2)
        {
            return new Point(pos1.X + pos2.X, pos1.Y + pos2.Y);
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
                return Equals((Point)obj);
            }
            return false;
        }

        public Point Offset(int x, int y)
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

        internal string DebuggerDisplay
        {
            get
            { 
                return string.Format("({0}, {1})", X, Y);
            }    
        }
    }
}
