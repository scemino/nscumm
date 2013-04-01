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
using System.Collections.Generic;
using System.IO;

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
        public DrawBitmapFlags flags;

        public Dictionary<byte, ushort> ScriptOffsets { get; private set; }
        public Dictionary<byte, ScriptData> Scripts { get; private set; }
        public byte[] Name { get; set; }

        public ObjectData()
        {
            this.ScriptOffsets = new Dictionary<byte, ushort>();
            this.Scripts = new Dictionary<byte, ScriptData>();
        }

        public byte[] Image { get; set; }

        public void Load(BinaryReader reader, uint version)
        {
            var objectEntries = new[]{
                LoadAndSaveEntry.Create(()=> OBIMoffset = reader.ReadUInt32(),8),
                LoadAndSaveEntry.Create(()=> OBCDoffset = reader.ReadUInt32(),8),
                LoadAndSaveEntry.Create(()=> walk_x = reader.ReadInt16(),8),
                LoadAndSaveEntry.Create(()=> walk_y = reader.ReadInt16(),8),
                LoadAndSaveEntry.Create(()=> obj_nr = reader.ReadUInt16(),8),
                LoadAndSaveEntry.Create(()=> x_pos = reader.ReadInt16(),8),
                LoadAndSaveEntry.Create(()=> y_pos = reader.ReadInt16(),8),
                LoadAndSaveEntry.Create(()=> width = reader.ReadUInt16(),8),
                LoadAndSaveEntry.Create(()=> height = reader.ReadUInt16(),8),
                LoadAndSaveEntry.Create(()=> actordir = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> parentstate = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> parent = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> state = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> fl_object_index = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> flags = (DrawBitmapFlags)reader.ReadByte(),46),
            };
            Array.ForEach(objectEntries, e => e.Execute(version));
        }
    }
}
