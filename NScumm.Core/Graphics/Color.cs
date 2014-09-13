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
}
