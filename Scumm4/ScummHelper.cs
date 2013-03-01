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
    }
}
