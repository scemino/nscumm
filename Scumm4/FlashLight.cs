using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public struct FlashLight
    {
        public int x, y, w, h;
        //public byte* buffer;
        public ushort xStrips, yStrips;
        public bool isDrawn;
    }
}
