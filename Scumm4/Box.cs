using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class Box
    {
        public short ulx, uly;
        public short urx, ury;
        public short lrx, lry;
        public short llx, lly;
        public byte mask;
        public BoxFlags flags;
        public ushort scale;
    }
}
