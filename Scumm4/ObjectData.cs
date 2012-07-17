using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class ObjectData
    {
        public uint OBIMoffset;
        public uint OBCDoffset;
        public short walk_x, walk_y;
        public ushort obj_nr;
        public short x_pos;
        public short y_pos;
        public ushort width;
        public ushort height;
        public byte actordir;
        public byte parent;
        public byte parentstate;
        public byte state;
        public byte fl_object_index;
        public byte flags;

        public Dictionary<byte, ushort> ScriptOffsets { get; private set; }
        public Dictionary<byte, ScriptData> Scripts { get; private set; }
        public string Name { get; set; }
        public List<Strip> Strips { get; private set; }

        public ObjectData()
        {
            this.ScriptOffsets = new Dictionary<byte, ushort>();
            this.Scripts = new Dictionary<byte, ScriptData>();
            this.Strips = new List<Strip>();
        }
    }
}
