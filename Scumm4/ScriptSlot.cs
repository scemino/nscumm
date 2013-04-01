/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;

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

    public class ScriptSlot
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

        public void Load(BinaryReader reader, uint version)
        {
            byte cycle;
            byte unk5;
            var scriptSlotEntries = new[]{
                LoadAndSaveEntry.Create(()=> offs = reader.ReadUInt32(),8),
                LoadAndSaveEntry.Create(()=> delay = reader.ReadInt32(),8),
                LoadAndSaveEntry.Create(()=> number = reader.ReadUInt16(),8),
                LoadAndSaveEntry.Create(()=> delayFrameCount = reader.ReadUInt16(),8),
                LoadAndSaveEntry.Create(()=> status = (ScriptStatus)reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> where = (WhereIsObject)reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> freezeResistant = reader.ReadBoolean(),8),
                LoadAndSaveEntry.Create(()=> recursive = reader.ReadBoolean(),8),
                LoadAndSaveEntry.Create(()=> freezeCount = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> didexec = reader.ReadBoolean(),8),
                LoadAndSaveEntry.Create(()=> cutsceneOverride = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> cycle = reader.ReadByte(),46),
                LoadAndSaveEntry.Create(()=> unk5 = reader.ReadByte(),8,10),
            };
            Array.ForEach(scriptSlotEntries, e => e.Execute(version));
        }
    }
}
