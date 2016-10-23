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
    public struct Rect : IEquatable<Rect>
    {
        public short Top, Left;
        public short Bottom, Right;

        public short Height
        {
            get { return (short) (Bottom - Top); }
            set { Bottom = (short) (Top + value); }
        }

        public short Width
        {
            get { return (short) (Right - Left); }
            set { Right = (short) (Left + value); }
        }

        internal string DebuggerDisplay => $"[{Left},{Top},{Right},{Bottom}]";

        public bool IsValid => Left <= Right && Top <= Bottom;

        public bool IsEmpty => Left >= Right || Top >= Bottom;

        public bool IsValidRect => Left <= Right && Top <= Bottom;

        public Rect(Rect r)
        {
            Top = r.Top;
            Left = r.Left;
            Bottom = r.Bottom;
            Right = r.Right;
        }

        public Rect(short w, short h)
        {
            Top = 0;
            Left = 0;
            Bottom = h;
            Right = w;
        }

        public Rect(short x1, short y1, short x2, short y2)
        {
            Top = y1;
            Left = x1;
            Bottom = y2;
            Right = x2;
        }

        public void Extend(ref Rect r)
        {
            Left = Math.Min(Left, r.Left);
            Right = Math.Max(Right, r.Right);
            Top = Math.Min(Top, r.Top);
            Bottom = Math.Max(Bottom, r.Bottom);
        }

        public void Clip(short maxw, short maxh)
        {
            Clip(new Rect(0, 0, maxw, maxh));
        }

        public void Clip(Rect r)
        {
            if (Top < r.Top)
                Top = r.Top;
            else if (Top > r.Bottom)
                Top = r.Bottom;

            if (Left < r.Left)
                Left = r.Left;
            else if (Left > r.Right)
                Left = r.Right;

            if (Bottom > r.Bottom)
                Bottom = r.Bottom;
            else if (Bottom < r.Top)
                Bottom = r.Top;

            if (Right > r.Right)
                Right = r.Right;
            else if (Right < r.Left)
                Right = r.Left;
        }

        /// <summary>
        /// Check if given position is inside this rectangle.
        /// </summary>
        /// <param name="x">The horizontal position to check.</param>
        /// <param name="y">The vertical position to check.</param>
        /// <returns>true if the given position is inside this rectangle, false otherwise</returns>
        public bool Contains(int x, int y)
        {
            return (Left <= x) && (x < Right) && (Top <= y) && (y < Bottom);
        }

        public bool Contains(Point p)
        {
            return Contains(p.X, p.Y);
        }

        /// <summary>
        /// Check if the given rect is contained inside this rectangle.
        /// </summary>
        /// <param name="r">The rectangle to check</param>
        /// <returns>true if the given rect is inside, false otherwise</returns>
        public bool Contains(Rect r)
        {
            return (Left <= r.Left) && (r.Right <= Right) &&
                   (Top <= r.Top) && (r.Bottom <= Bottom);
        }

        public override string ToString()
        {
            return DebuggerDisplay;
        }

        public void MoveTo(short x, short y)
        {
            Bottom = (short) (Bottom + y - Top);
            Right = (short) (Right + x - Left);
            Top = y;
            Left = x;
        }

        /// <summary>
        /// Extend this rectangle in all four directions by the given number of pixels
        /// </summary>
        /// <param name="offset">the size to grow by</param>
        public void Grow(short offset)
        {
            Top -= offset;
            Left -= offset;
            Bottom += offset;
            Right += offset;
        }

        public void Translate(short dx, short dy)
        {
            Left += dx; Right += dx;
            Top += dy; Bottom += dy;
        }

        /// <summary>
        /// Extend this rectangle so that it contains r
        /// </summary>
        /// <param name="r">the rectangle to extend by</param>
        public void Extend(Rect r)
        {
            Left = Math.Min(Left, r.Left);
            Right = Math.Max(Right, r.Right);
            Top = Math.Min(Top, r.Top);
            Bottom = Math.Max(Bottom, r.Bottom);
        }

        /**
         * Check if given rectangle intersects with this rectangle
         *
         * @param r the rectangle to check
         *
         * @return true if the given rectangle has a non-empty intersection with
         *         this rectangle, false otherwise
         */
        public bool Intersects(Rect r)
        {
            return (Left < r.Right) && (r.Left < Right) && (Top < r.Bottom) && (r.Top < Bottom);
        }

        /**
         * Find the intersecting rectangle between this rectangle and the given rectangle
         *
         * @param r the intersecting rectangle
         *
         * @return the intersection of the rectangles or an empty rectangle if not intersecting
         */
        public Rect FindIntersectingRect(Rect r)
        {
            if (!Intersects(r))
                return new Rect();

            return new Rect(Math.Max(r.Left, Left), Math.Max(r.Top, Top), Math.Min(r.Right, Right),
                Math.Min(r.Bottom, Bottom));
        }

        public bool Equals(Rect other)
        {
            return Top == other.Top && Bottom == other.Bottom && Left == other.Left && Right == other.Right;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Rect && Equals((Rect) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Top.GetHashCode();
                hashCode = (hashCode * 397) ^ Bottom.GetHashCode();
                hashCode = (hashCode * 397) ^ Left.GetHashCode();
                hashCode = (hashCode * 397) ^ Right.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(Rect left, Rect right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rect left, Rect right)
        {
            return !left.Equals(right);
        }
    }
}
