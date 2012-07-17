using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public struct Point
    {
        /// <summary>
        /// The horizontal part of the point.
        /// </summary>
        public short x;
        /// <summary>
        /// The vertical part of the point
        /// </summary>
        public short y;

        public Point(short x, short y)
        {
            this.x = x;
            this.y = y;
        }

        public Point(Point pos)
        {
            this.x = pos.x;
            this.y = pos.y;
        }

        public static bool operator ==(Point pos1, Point pos2)
        {
            return pos1.x == pos2.x && pos1.y == pos2.y;
        }

        public static bool operator !=(Point pos1, Point pos2)
        {
            return !(pos1 == pos2);
        }

        public override int GetHashCode()
        {
            return x ^ y;
        }

        public override bool Equals(object obj)
        {
            if (obj is Point)
            {
                var pos = ((Point)obj);
                return pos.x == this.x && pos.y == this.y;
            }
            return false;
        }

        /// <summary>
        /// Computes the square of the distance between this point and the point p.
        /// </summary>
        /// <param name="p">The other point.</param>
        /// <returns>The distance between this and p.</returns>
        public uint SquareDistance(Point p)
        {
            int diffx = Math.Abs(p.x - x);
            if (diffx >= 0x1000)
                return 0xFFFFFF;

            int diffy = Math.Abs(p.y - y);
            if (diffy >= 0x1000)
                return 0xFFFFFF;

            return (uint)(diffx * diffx + diffy * diffy);
        }
    }
}
