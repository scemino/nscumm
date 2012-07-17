using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class Charset
    {
        public byte Height { get; set; }

        public byte Bpp { get; set; }

        public byte[] ColorMap { get; private set; }

        public Dictionary<byte, CharInfo> Characters { get; private set; }

        public Charset()
        {
            this.ColorMap = new byte[16];
            this.Characters = new Dictionary<byte, CharInfo>();
        }

    }
}
