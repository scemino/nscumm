using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class RoomHeader
    {
        public ushort Width { get; set; }
        public ushort Height { get; set; }
        public ushort NumObjects { get; set; }
    }

    public class Room
    {
        public RoomHeader Header { get; set; }
        public List<Strip> Strips { get; private set; }
        public Palette Palette { get; private set; }
        public ScriptData[] LocalScripts { get; private set; }
        public ScriptData EntryScript { get; private set; }
        public ScriptData ExitScript { get; private set; }
        public List<byte[]> ZPlanes { get; private set; }
        public List<ObjectData> Objects { get; private set; }
        public List<Box> Boxes { get; private set; }
        public List<byte> BoxMatrix { get; private set; }
        public string Name { get; set; }

        public Room()
        {
            this.Strips = new List<Strip>();
            this.ZPlanes = new List<byte[]>();
            this.Boxes = new List<Box>();
            this.Objects = new List<ObjectData>();
            this.BoxMatrix = new List<byte>();
            this.Palette = new Palette();
            this.EntryScript = new ScriptData();
            this.ExitScript = new ScriptData();
            this.LocalScripts = new ScriptData[1024];
        }
    }
}
