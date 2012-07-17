using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class AnimPict
    {
        public AnimPict(ushort width, ushort height)
        {
            this.Width = width;
            this.Height = height;
            this.Data = new byte[width * height];
        }

        public byte[] Data { get; private set; }

        public ushort Width { get; private set; }

        public ushort Height { get; private set; }

        public short RelX { get; set; }

        public short RelY { get; set; }

        public short MoveX { get; set; }

        public short MoveY { get; set; }

        public ushort Limb { get; set; }

        public bool Mirror { get; set; }
    }
}
