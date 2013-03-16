using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    internal class ScummHelper
    {
        public static int NewDirToOldDir(int dir)
        {
            if (dir >= 71 && dir <= 109)
                return 1;
            if (dir >= 109 && dir <= 251)
                return 2;
            if (dir >= 251 && dir <= 289)
                return 0;
            return 3;
        }

        public static int RevBitMask(int x)
        {
            return (0x80 >> (x));
        }

        public static void AssertRange(int min, int value, int max, string desc)
        {
            if (value < min || value > max)
            {
                throw new ArgumentOutOfRangeException("value", string.Format("{0} {1} is out of bounds ({2},{3})", desc, value, min, max));
            }
        }
    }
}
