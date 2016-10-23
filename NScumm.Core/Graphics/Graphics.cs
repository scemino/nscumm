//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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
    public static class Graphics
    {
        public static void DrawLine(int x0, int y0, int x1, int y1, int color, Action<int, int, int, object> plotProc,
            object data)
        {
            // Bresenham's line algorithm, as described by Wikipedia
            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);

            if (steep)
            {
                ScummHelper.Swap(ref x0, ref y0);
                ScummHelper.Swap(ref x1, ref y1);
            }

            int deltaX = Math.Abs(x1 - x0);
            int deltaY = Math.Abs(y1 - y0);
            int deltaErr = deltaY;
            int x = x0;
            int y = y0;
            int err = 0;

            int xStep = x0 < x1 ? 1 : -1;
            int yStep = y0 < y1 ? 1 : -1;

            if (steep)
                plotProc(y, x, color, data);
            else
                plotProc(x, y, color, data);

            while (x != x1)
            {
                x += xStep;
                err += deltaErr;
                if (2 * err > deltaX)
                {
                    y += yStep;
                    err -= deltaX;
                }
                if (steep)
                    plotProc(y, x, color, data);
                else
                    plotProc(x, y, color, data);
            }
        }

        /// <summary>
        /// Bresenham as presented in Foley & Van Dam
        /// Code is based on GD lib http://libgd.github.io/
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <param name="thick"></param>
        /// <param name="color"></param>
        /// <param name="???"></param>
        /// <param name="plotProc"></param>
        /// <param name="data"></param>
        public static void DrawThickLine2(int x1, int y1, int x2, int y2, int thick, int color,
            Action<int, int, int, object> plotProc, object data)
        {
            int incr1, incr2, d, x, y;
            int wid;
            int w, wstart;

            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);

            if (dx == 0)
            {
                int xn = x1 - thick / 2;
                Rect r = new Rect((short) xn, (short) Math.Min(y1, y2), (short) (xn + thick - 1),
                    (short) Math.Max(y1, y2));
                DrawFilledRect(r, color, plotProc, data);
                return;
            }
            if (dy == 0)
            {
                int yn = y1 - thick / 2;
                Rect r = new Rect((short) Math.Min(x1, x2), (short) yn, (short) Math.Max(x1, x2),
                    (short) (yn + thick - 1));
                DrawFilledRect(r, color, plotProc, data);
                return;
            }

            if (dy <= dx)
            {
                /* More-or-less horizontal. use wid for vertical stroke */
                /* Doug Claar: watch out for NaN in atan2 (2.0.5) */

                /* 2.0.12: Michael Schwartz: divide rather than multiply;
                      TBB: but watch out for /0! */
                double ac = Math.Cos(Math.Atan2(dy, dx));
                if (ac != 0)
                {
                    wid = (int) (thick / ac);
                }
                else
                {
                    wid = 1;
                }
                if (wid == 0)
                {
                    wid = 1;
                }
                d = 2 * dy - dx;
                incr1 = 2 * dy;
                incr2 = 2 * (dy - dx);
                int xend;
                int ydirflag;
                if (x1 > x2)
                {
                    x = x2;
                    y = y2;
                    ydirflag = (-1);
                    xend = x1;
                }
                else
                {
                    x = x1;
                    y = y1;
                    ydirflag = 1;
                    xend = x2;
                }

                /* Set up line thickness */
                wstart = y - wid / 2;
                for (w = wstart; w < wstart + wid; w++)
                    plotProc(x, y, color, data);

                if (((y2 - y1) * ydirflag) > 0)
                {
                    while (x < xend)
                    {
                        x++;
                        if (d < 0)
                        {
                            d += incr1;
                        }
                        else
                        {
                            y++;
                            d += incr2;
                        }
                        wstart = y - wid / 2;
                        for (w = wstart; w < wstart + wid; w++)
                            plotProc(x, w, color, data);
                    }
                }
                else
                {
                    while (x < xend)
                    {
                        x++;
                        if (d < 0)
                        {
                            d += incr1;
                        }
                        else
                        {
                            y--;
                            d += incr2;
                        }
                        wstart = y - wid / 2;
                        for (w = wstart; w < wstart + wid; w++)
                            plotProc(x, w, color, data);
                    }
                }
            }
            else
            {
                /* More-or-less vertical. use wid for horizontal stroke */
                /* 2.0.12: Michael Schwartz: divide rather than multiply;
                   TBB: but watch out for /0! */
                double @as = Math.Sin(Math.Atan2(dy, dx));
                if (@as != 0)
                {
                    wid = (int) (thick / @as);
                }
                else
                {
                    wid = 1;
                }
                if (wid == 0)
                    wid = 1;

                d = 2 * dx - dy;
                incr1 = 2 * dx;
                incr2 = 2 * (dx - dy);
                int yend;
                int xdirflag;
                if (y1 > y2)
                {
                    y = y2;
                    x = x2;
                    yend = y1;
                    xdirflag = (-1);
                }
                else
                {
                    y = y1;
                    x = x1;
                    yend = y2;
                    xdirflag = 1;
                }

                /* Set up line thickness */
                wstart = x - wid / 2;
                for (w = wstart; w < wstart + wid; w++)
                    plotProc(w, y, color, data);

                if (((x2 - x1) * xdirflag) > 0)
                {
                    while (y < yend)
                    {
                        y++;
                        if (d < 0)
                        {
                            d += incr1;
                        }
                        else
                        {
                            x++;
                            d += incr2;
                        }
                        wstart = x - wid / 2;
                        for (w = wstart; w < wstart + wid; w++)
                            plotProc(w, y, color, data);
                    }
                }
                else
                {
                    while (y < yend)
                    {
                        y++;
                        if (d < 0)
                        {
                            d += incr1;
                        }
                        else
                        {
                            x--;
                            d += incr2;
                        }
                        wstart = x - wid / 2;
                        for (w = wstart; w < wstart + wid; w++)
                            plotProc(w, y, color, data);
                    }
                }
            }
        }

        public static void DrawFilledRect(Rect rect, int color, Action<int, int, int, object> plotProc, object data)
        {
            for (int y = rect.Top; y <= rect.Bottom; y++)
                DrawHLine(rect.Left, rect.Right, y, color, plotProc, data);
        }

        public static void DrawHLine(int x1, int x2, int y, int color, Action<int, int, int, object> plotProc,
            object data)
        {
            if (x1 > x2)
                ScummHelper.Swap(ref x1, ref x2);

            for (int x = x1; x <= x2; x++)
                plotProc(x, y, color, data);
        }
    }
}