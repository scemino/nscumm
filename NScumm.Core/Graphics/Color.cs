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
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public struct Color
    {
        public int R { get; set; }

        public int G { get; set; }

        public int B { get; set; }

        public static Color FromRgb(int r, int g, int b)
        {
            return new Color { R = r, G = g, B = b };
        }

        public override int GetHashCode()
        {
            return R ^ G ^ B;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var color = (Color)obj;
            return R == color.R && G == color.G && B == color.B;
        }

        internal string DebuggerDisplay
        {
            get
            { 
                return string.Format("({0}, {1}, {2})", R, G, B);
            }    
        }
    }

    public static class ColorHelper
    {
        public static ushort RGBToColor(byte r, byte g, byte b)
        {
            return
                (ushort)(((0xFF >> 8) << 0) | ((r >> 3) << 10) | ((g >> 3) << 5) | ((b >> 3) << 0));
        }

        public static void ColorToRGB(ushort color, out byte r, out byte g, out byte b)
        {
            r = (byte)Expand5(color >> 10);
            g = (byte)Expand5(color >> 5);
            b = (byte)Expand5(color >> 0);
        }

        static int Expand5(int value)
        {
            value &= 31;
            return (value << 3) | (value >> 2);
        }
    }
}
