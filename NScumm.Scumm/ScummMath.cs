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
using NScumm.Core.Graphics;

namespace NScumm.Scumm
{
    static class ScummMath
    {
        public static bool CompareSlope(Point p1, Point p2, Point p3)
        {
            return (p2.Y - p1.Y) * (p3.X - p1.X) <= (p3.Y - p1.Y) * (p2.X - p1.X);
        }

        public static Point ClosestPtOnLine(Point lineStart, Point lineEnd, Point p)
        {
            Point result;

            int lxdiff = lineEnd.X - lineStart.X;
            int lydiff = lineEnd.Y - lineStart.Y;

            if (lineEnd.X == lineStart.X)
            {   // Vertical line?
                result.X = lineStart.X;
                result.Y = p.Y;
            }
            else if (lineEnd.Y == lineStart.Y)
            {   // Horizontal line?
                result.X = p.X;
                result.Y = lineStart.Y;
            }
            else
            {
                int dist = lxdiff * lxdiff + lydiff * lydiff;
                int a, b, c;
                if (Math.Abs(lxdiff) > Math.Abs(lydiff))
                {
                    a = lineStart.X * lydiff / lxdiff;
                    b = p.X * lxdiff / lydiff;

                    c = (a + b - lineStart.Y + p.Y) * lydiff * lxdiff / dist;

                    result.X = (short)c;
                    result.Y = (short)(c * lydiff / lxdiff - a + lineStart.Y);
                }
                else
                {
                    a = lineStart.Y * lxdiff / lydiff;
                    b = p.Y * lydiff / lxdiff;

                    c = (a + b - lineStart.X + p.X) * lydiff * lxdiff / dist;

                    result.X = (short)(c * lxdiff / lydiff - a + lineStart.X);
                    result.Y = (short)c;
                }
            }

            if (Math.Abs(lydiff) < Math.Abs(lxdiff))
            {
                if (lxdiff > 0)
                {
                    if (result.X < lineStart.X)
                        result = lineStart;
                    else if (result.X > lineEnd.X)
                        result = lineEnd;
                }
                else
                {
                    if (result.X > lineStart.X)
                        result = lineStart;
                    else if (result.X < lineEnd.X)
                        result = lineEnd;
                }
            }
            else
            {
                if (lydiff > 0)
                {
                    if (result.Y < lineStart.Y)
                        result = lineStart;
                    else if (result.Y > lineEnd.Y)
                        result = lineEnd;
                }
                else
                {
                    if (result.Y > lineStart.Y)
                        result = lineStart;
                    else if (result.Y < lineEnd.Y)
                        result = lineEnd;
                }
            }

            return result;
        }

        public static uint GetClosestPtOnBox(BoxCoords box, Point pIn, out Point pOut)
        {
            Point tmp;
            uint dist;
            uint bestdist = 0xFFFFFF;
            pOut = new Point();

            tmp = ScummMath.ClosestPtOnLine(box.UpperLeft, box.UpperRight, pIn);
            dist = pIn.SquareDistance(tmp);
            if (dist < bestdist)
            {
                bestdist = dist;
                pOut = tmp;
            }

            tmp = ScummMath.ClosestPtOnLine(box.UpperRight, box.LowerRight, pIn);
            dist = pIn.SquareDistance(tmp);
            if (dist < bestdist)
            {
                bestdist = dist;
                pOut = tmp;
            }

            tmp = ScummMath.ClosestPtOnLine(box.LowerRight, box.LowerLeft, pIn);
            dist = pIn.SquareDistance(tmp);
            if (dist < bestdist)
            {
                bestdist = dist;
                pOut = tmp;
            }

            tmp = ScummMath.ClosestPtOnLine(box.LowerLeft, box.UpperLeft, pIn);
            dist = pIn.SquareDistance(tmp);
            if (dist < bestdist)
            {
                bestdist = dist;
                pOut = tmp;
            }

            return bestdist;
        }

        public static int GetDistance(Point p1, Point p2)
        {
            int a = Math.Abs(p1.Y - p2.Y);
            int b = Math.Abs(p1.X - p2.X);
            return Math.Max(a, b);
        }

        public static int GetAngleFromPos(int x, int y, bool useATAN)
        {
            if (useATAN)
            {
                double temp = Math.Atan2(x, -y);
                return NormalizeAngle((int)(temp * 180 / Math.PI));
            }
            if (Math.Abs(y) * 2 < Math.Abs(x))
            {
                if (x > 0)
                    return 90;
                return 270;
            }
            if (y > 0)
                return 180;
            return 0;
        }

        public static int NormalizeAngle(int angle)
        {
            int temp = (angle + 360) % 360;
            return ToSimpleDir(true, temp) * 45;
        }

        public static int ToSimpleDir(bool dirType, int dir)
        {
            if (dirType)
            {
                var directions = new short[] { 22, 72, 107, 157, 202, 252, 287, 337 };
                for (int i = 0; i < 7; i++)
                    if (dir >= directions[i] && dir <= directions[i + 1])
                        return i + 1;
            }
            else
            {
                var directions = new short[] { 71, 109, 251, 289 };
                for (int i = 0; i < 3; i++)
                    if (dir >= directions[i] && dir <= directions[i + 1])
                        return i + 1;
            }
            return 0;
        }

        /// <summary>
        /// Convert a simple direction to an angle.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static int FromSimpleDirection(int dir)
        {
            return dir * 90;
        }
    }
}

