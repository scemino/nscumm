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
using System.Collections.Generic;
using System.IO;

namespace NScumm.Core
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
        public DrawBitmaps flags;

        public Dictionary<byte, ushort> ScriptOffsets { get; private set; }
        public ScriptData Script { get; private set; }
        public byte[] Name { get; set; }

        public ObjectData()
        {
            this.ScriptOffsets = new Dictionary<byte, ushort>();
            this.Script = new ScriptData();
        }

        public byte[] Image { get; set; }

        public void SaveOrLoad(Serializer serializer)
        {
            var objectEntries = new[]{
                LoadAndSaveEntry.Create(reader => OBIMoffset = reader.ReadUInt32(), writer => writer.Write(OBIMoffset), 8),
                LoadAndSaveEntry.Create(reader => OBCDoffset = reader.ReadUInt32(), writer => writer.Write(OBCDoffset),8),
                LoadAndSaveEntry.Create(reader => walk_x = reader.ReadInt16(), writer => writer.Write(walk_x),8),
                LoadAndSaveEntry.Create(reader => walk_y = reader.ReadInt16(), writer => writer.Write(walk_y),8),
                LoadAndSaveEntry.Create(reader => obj_nr = reader.ReadUInt16(), writer => writer.Write(obj_nr),8),
                LoadAndSaveEntry.Create(reader => x_pos = reader.ReadInt16(), writer => writer.Write(x_pos),8),
                LoadAndSaveEntry.Create(reader => y_pos = reader.ReadInt16(), writer => writer.Write(y_pos),8),
                LoadAndSaveEntry.Create(reader => width = reader.ReadUInt16(), writer => writer.Write(width),8),
                LoadAndSaveEntry.Create(reader => height = reader.ReadUInt16(), writer => writer.Write(height),8),
                LoadAndSaveEntry.Create(reader => actordir = reader.ReadByte(), writer => writer.Write(actordir),8),
                LoadAndSaveEntry.Create(reader => parentstate = reader.ReadByte(), writer => writer.Write(parentstate),8),
                LoadAndSaveEntry.Create(reader => parent = reader.ReadByte(), writer => writer.Write(parent),8),
                LoadAndSaveEntry.Create(reader => state = reader.ReadByte(), writer => writer.Write(state),8),
                LoadAndSaveEntry.Create(reader => fl_object_index = reader.ReadByte(), writer => writer.Write(fl_object_index),8),
                LoadAndSaveEntry.Create(reader => flags = (DrawBitmaps)reader.ReadByte(), writer => writer.Write((byte)flags),46),
            };
            Array.ForEach(objectEntries, e => e.Execute(serializer));
        }
    }
}
