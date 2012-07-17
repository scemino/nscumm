using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class Costume
    {
        private CostumeAnimation[] cAnims;
        private byte room;
        private byte[] palette;

        public byte[] Palette
        {
            get { return palette; }
        }

        public byte Room
        {
            get { return room; }
        }

        public CostumeAnimation[] Animations
        {
            get { return cAnims; }
        }

        public Costume(byte room, byte[] palette, CostumeAnimation[] cAnims)
        {
            this.room = room;
            this.palette = palette;
            this.cAnims = cAnims;
        }
    }
}
