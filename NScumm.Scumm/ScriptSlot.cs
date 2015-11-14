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
using NScumm.Core;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    [Flags]
    public enum ScriptStatus
    {
        Dead = 0,
        Paused = 1,
        Running = 2,
        Frozen = 0x80
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

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    class ScriptSlot
    {
        byte freezeCount;

        public uint Offset;
        public int Delay;
        public ushort Number;
        public bool FreezeResistant;
        public bool Recursive;
        public bool IsExecuted;

        public ScriptStatus Status{ get; set; }

        public WhereIsObject Where;
        public byte CutSceneOverride;

        public int InventoryEntry { get; set; }

        public bool Frozen { get { return freezeCount > 0; } }

        public ushort DelayFrameCount { get; set; }

        public int[] LocalVariables = new int[26];

        public void InitializeLocals(Array locals)
        {
            Array.Copy(locals, LocalVariables, locals.Length);
            Array.Clear(LocalVariables, locals.Length, LocalVariables.Length - locals.Length);
        }

        public void Freeze()
        {
            freezeCount++;
            Status |= ScriptStatus.Frozen;
        }

        public void Unfreeze()
        {
            if (Frozen)
                freezeCount--;
            if (!Frozen)
            {
                Status &= ~ScriptStatus.Frozen;
            }
        }

        public void UnfreezeAll()
        {
            freezeCount = 0;
            Status &= ~ScriptStatus.Frozen;
        }

        public void SaveOrLoad(Serializer serializer, System.Collections.Generic.IList<ScriptData> localScripts, int numGlobalScripts)
        {
            var scriptSlotEntries = new[]
            {
                LoadAndSaveEntry.Create(
                    reader => Offset = reader.ReadUInt32(),
                    writer =>
                    {
                        var offsetToSave = Offset;
                        if (Where == WhereIsObject.Global)
                        {
                            offsetToSave += 6;
                        }
                        else if (Where == WhereIsObject.Local && Number >= numGlobalScripts && localScripts[Number - numGlobalScripts] != null)
                        {
                            offsetToSave = (uint)(Offset + localScripts[Number - numGlobalScripts].Offset);
                        }
                        writer.WriteUInt32(offsetToSave);
                    }, 8),

                LoadAndSaveEntry.Create(reader => Delay = reader.ReadInt32(), writer => writer.WriteInt32(Delay), 8),
                LoadAndSaveEntry.Create(reader => Number = reader.ReadUInt16(), writer => writer.WriteUInt16(Number), 8),
                LoadAndSaveEntry.Create(reader => DelayFrameCount = reader.ReadUInt16(), writer => writer.WriteUInt16(DelayFrameCount), 8),
                LoadAndSaveEntry.Create(reader => Status = (ScriptStatus)reader.ReadByte(), writer => writer.WriteByte((byte)Status), 8),
                LoadAndSaveEntry.Create(reader => Where = (WhereIsObject)reader.ReadByte(), writer => writer.WriteByte((byte)Where), 8),
                LoadAndSaveEntry.Create(reader => FreezeResistant = reader.ReadBoolean(), writer => writer.WriteByte(FreezeResistant), 8),
                LoadAndSaveEntry.Create(reader => Recursive = reader.ReadBoolean(), writer => writer.WriteByte(Recursive), 8),
                LoadAndSaveEntry.Create(reader => freezeCount = reader.ReadByte(), writer => writer.WriteByte(freezeCount), 8),
                LoadAndSaveEntry.Create(reader => IsExecuted = reader.ReadBoolean(), writer => writer.WriteByte(IsExecuted), 8),
                LoadAndSaveEntry.Create(reader => CutSceneOverride = reader.ReadByte(), writer => writer.WriteByte(CutSceneOverride), 8),
                LoadAndSaveEntry.Create(reader => reader.ReadByte(), writer => writer.WriteByte(0), 46),
                LoadAndSaveEntry.Create(reader => reader.ReadByte(), writer => writer.WriteByte(0), 8, 10),
            };

            scriptSlotEntries.ForEach(e => e.Execute(serializer));
        }

        internal string DebuggerDisplay
        {
            get
            { 
                return Number != 0 ? string.Format("(Number: {0}, {1}, {2}, {3})", Number, Status, Where, Frozen ? "Frozen" : string.Empty) : "Number 0";
            }    
        }
    }
}
