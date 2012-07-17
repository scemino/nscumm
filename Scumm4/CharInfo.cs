using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class CharInfo
    {
        public CharInfo(byte widht, byte height)
        {
            this.Width = widht;
            this.Height = height;
            this.Stride = ((Width + 7) / 8) * 8;
            this.Pixels = new byte[this.Stride * this.Height];
        }

        public byte[] Pixels { get; private set; }

        public byte Width { get; private set; }

        public byte Height { get; private set; }

        public sbyte X { get; set; }

        public sbyte Y { get; set; }

        public int Stride { get; private set; }
    }
}
