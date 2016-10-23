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

using NScumm.Core.Graphics;
using NScumm.Sci.Engine;
using System;
using NScumm.Core;

namespace NScumm.Sci.Graphics
{
    public struct Color : IEquatable<Color>
    {
        public byte used;
        public byte R, G, B;

        public override int GetHashCode()
        {
            return R ^ G ^ B;
        }

        public override bool Equals(object obj)
        {
            return obj is Color && Equals((Color)obj);
        }

        public bool Equals(Color other)
        {
            return other.R == R && other.G == G && other.B == B;
        }

        public static bool operator ==(Color c1, Color c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(Color c1, Color c2)
        {
            return !(c1 == c2);
        }
    }

    internal class Palette
    {
        public readonly byte[] mapping = new byte[256];
        public int timestamp;
        public readonly Color[] colors = new Color[256];
        public readonly byte[] intensity = new byte[256];

        public Palette()
        {
        }

        public Palette(Palette copy)
        {
            Array.Copy(copy.mapping, mapping, mapping.Length);
            timestamp = copy.timestamp;
            Array.Copy(copy.colors, colors, colors.Length);
            Array.Copy(copy.intensity, intensity, intensity.Length);
        }
    }

    internal class Port
    {
        private const int PORTS_FIRSTWINDOWID = 2;

        public ushort id;
        public short top, left;
        public Rect rect;
        public short curTop, curLeft;
        public short fontHeight;
        public int fontId;
        public bool greyedOutput;
        public short penClr, backClr;
        public short penMode;
        public ushort counterTillFree;

        public Port(ushort theId)
        {
            id = theId;
        }

        public bool IsWindow { get { return id >= PORTS_FIRSTWINDOWID && id != 0xFFFF; } }
    }

    internal class Window : Port
    {
        public Rect dims; // client area of window
        public Rect restoreRect; // total area of window including borders
        public WindowManagerStyle wndStyle;
        public GfxScreenMasks saveScreenMask;
        public Register hSaved1;
        public Register hSaved2;
        public string title;
        public bool bDrawn;

        public Window(ushort theId) : base(theId)
        {
        }
    }

    internal static class Helpers
    {
        /// <summary>
        /// Multiplies a number by a rational number, rounding up to
        /// the nearest whole number.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="ratio">Ratio.</param>
        /// <param name="extra">Extra.</param>
        public static int Mulru(int value, ref Rational ratio, int extra = 0)
        {
            int num = (value + extra) * ratio.Numerator;
            int result = num / ratio.Denominator;
            if (num > ratio.Denominator && (num % ratio.Denominator)!=0)
            {
                ++result;
            }
            return result - extra;
        }

        /// <summary>
        /// Multiplies a point by two rational numbers for X and Y,
        /// rounding up to the nearest whole number.Modifies the
        /// point directly.
        /// </summary>
        /// <param name="point">Point.</param>
        /// <param name="ratioX">Ratio x.</param>
        /// <param name="ratioY">Ratio y.</param>
        public static void Mulru(ref Point point, ref Rational ratioX, ref Rational ratioY)
        {
            point.X = (short) Mulru(point.X, ref ratioX);
            point.Y = (short) Mulru(point.Y, ref ratioY);
        }

        /// <summary>
        /// Multiplies a point by two rational numbers for X and Y,
        /// rounding up to the nearest whole number. Modifies the
        /// rect directly.
        /// </summary>
        /// <param name="rect">Rect.</param>
        /// <param name="ratioX">Ratio x.</param>
        /// <param name="ratioY">Ratio y.</param>
        /// <param name="extra">Extra.</param>
        public static void Mulru(ref Rect rect, ref Rational ratioX, ref Rational ratioY, int extra)
        {
            rect.Left = (short) Mulru(rect.Left, ref ratioX);
            rect.Top = (short) Mulru(rect.Top, ref ratioY);
            rect.Right = (short) (Mulru(rect.Right - 1, ref ratioX, extra) + 1);
            rect.Bottom = (short) (Mulru(rect.Bottom - 1, ref ratioY, extra) + 1);
        }

        /**
         * Multiplies a rectangle by two ratios with default
         * rounding. Modifies the rect directly. Uses inclusive
         * rectangle rounding.
         */
        public static void Mulinc(ref Rect rect, Rational ratioX, Rational ratioY)
        {
            rect.Left = (short) (rect.Left * ratioX);
            rect.Top = (short) (rect.Top * ratioY);
            rect.Right = (short) (((rect.Right - 1) * ratioX) + 1);
            rect.Bottom = (short) (((rect.Bottom - 1) * ratioY) + 1);
        }
    }
}
