using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class ResourceManager
    {
        public Resource[] Rooms { get; private set; }
        public Resource[] Sounds { get; private set; }
        public Resource[] Scripts { get; private set; }
        public Resource[] Charsets { get; private set; }
        public Resource[] Costumes { get; private set; }
        public Resource[] Strings { get; private set; }

        public ResourceManager()
        {
            this.Rooms = new Resource[99];
            this.Sounds = new Resource[0xC7];
            this.Scripts = new Resource[0xC7];
            this.Charsets = new Resource[9];
            this.Costumes = new Resource[0xC7];
            this.Strings = new Resource[0x32];
        }
    }
}
