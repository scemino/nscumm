using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public enum ScriptStatus
    {
        Dead = 0,
        Paused = 1,
        Running = 2
    }

    public enum WhereIsObject
    {
        NotFound = -1,
        Inventory = 0,
        Room = 1,
        Global = 2,
        Local = 3,
        FLObject = 4
    }

    public struct ScriptSlot
    {
        public uint offs;
        public int delay;
        public ushort number;
        public ushort delayFrameCount;
        public bool freezeResistant, recursive;
        public bool didexec;
        public ScriptStatus status;
        public WhereIsObject where;
        public byte freezeCount;
        public byte cutsceneOverride;
        public int InventoryEntry { get; set; }
        public bool Frozen { get; set; }
    }
}
