using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class Limb
    {
        public ushort Offset;
        public ushort Size;

        public List<ushort> ImageOffsets { get; private set; }

        public Limb()
        {
            this.ImageOffsets = new List<ushort>();
        }
    }
}
