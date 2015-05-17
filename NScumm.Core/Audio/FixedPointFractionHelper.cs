//
//  FixedPointFractionHelper.cs
//
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

namespace NScumm.Core.Audio
{
    static class FixedPointFractionHelper
    {
        public const int FRAC_BITS = 16;
        public const long FRAC_LO_MASK = ((1L << FRAC_BITS) - 1);
        public const long FRAC_HI_MASK = ((1L << FRAC_BITS) - 1) << FRAC_BITS;

        public const long FRAC_ONE = (1L << FRAC_BITS);
        // 1.0
        public const long FRAC_HALF = (1L << (FRAC_BITS - 1));
        // 0.5

        public static int DoubleToFrac(double value)
        {
            return (int)(value * FRAC_ONE);
        }

        public static double FracToDouble(int value)
        {
            return ((double)value) / FRAC_ONE;
        }

        public static int IntToFrac(short value)
        {
            return value << FRAC_BITS;
        }

        public static short FracToInt(int value)
        {
            return (short)(value >> FRAC_BITS);
        }
    }
    
}
