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

using NScumm.Core.IO;
using System;
using System.IO;

namespace NScumm.Core
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

        public void SaveOrLoad(Serializer serializer, ScriptData[] localScripts)
        {
            var scriptSlotEntries = new[]{
                LoadAndSaveEntry.Create(
                    reader => offs = reader.ReadUInt32(),
                    writer=>
                    {
                        var offsetToSave = offs;
                        if (where == WhereIsObject.Global)
                        {
                            offsetToSave += 6;
                        }
                        else if (where == WhereIsObject.Local && number >= 0xC8 && localScripts[number - 0xC8]!=null)
                        {
                            offsetToSave = (uint)(offs + localScripts[number - 0xC8].Offset);
                        }
                        writer.WriteUInt32(offsetToSave);
                    },8),

                LoadAndSaveEntry.Create(reader => delay = reader.ReadInt32(),writer=> writer.WriteInt32(delay),8),
                LoadAndSaveEntry.Create(reader => number = reader.ReadUInt16(),writer=> writer.WriteUInt16(number),8),
                LoadAndSaveEntry.Create(reader => delayFrameCount = reader.ReadUInt16(),writer=> writer.WriteUInt16(delayFrameCount),8),
                LoadAndSaveEntry.Create(reader => status = (ScriptStatus)reader.ReadByte(),writer=> writer.WriteByte((byte)status),8),
                LoadAndSaveEntry.Create(reader => where = (WhereIsObject)reader.ReadByte(),writer=> writer.WriteByte((byte)where),8),
                LoadAndSaveEntry.Create(reader => freezeResistant = reader.ReadBoolean(),writer=> writer.WriteByte(freezeResistant),8),
                LoadAndSaveEntry.Create(reader => recursive = reader.ReadBoolean(),writer=> writer.WriteByte(recursive),8),
                LoadAndSaveEntry.Create(reader => freezeCount = reader.ReadByte(),writer=> writer.WriteByte(freezeCount),8),
                LoadAndSaveEntry.Create(reader => didexec = reader.ReadBoolean(),writer=> writer.WriteByte(didexec),8),
                LoadAndSaveEntry.Create(reader => cutsceneOverride = reader.ReadByte(),writer=> writer.WriteByte(cutsceneOverride),8),
                LoadAndSaveEntry.Create(reader => reader.ReadByte(),writer=> writer.WriteByte((byte)0),46),
                LoadAndSaveEntry.Create(reader => reader.ReadByte(),writer=> writer.WriteByte((byte)0),8,10),
            };

            //if (serializer.IsLoading)
            //{
            //    if (where == WhereIsObject.Global)
            //    {
            //        offs -= 6;
            //    }
            //    else if (where == WhereIsObject.Local && number >= 0xC8)
            //    {
            //        offs = (uint)(offs - localScripts[number - 0xC8].Offset);
            //    }
            //}

            Array.ForEach(scriptSlotEntries, e => e.Execute(serializer));
        }
    }
}
