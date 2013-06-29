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
using NScumm.Core.Graphics;
using NScumm.Core.IO;

namespace NScumm.Core
{
    class ObjectData
    {
        public uint OBIMoffset;
        public uint OBCDoffset;
        public short WalkX, WalkY;
        public ushort Number;
        public ushort Width;
        public ushort Height;
        public byte ActorDir;
        public byte Parent;
        public byte ParentState;
        public byte State;
        public byte FlObjectIndex;
        public DrawBitmaps Flags;

        public Point Position { get; set; }
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
                LoadAndSaveEntry.Create(reader => WalkX = reader.ReadInt16(), writer => writer.Write(WalkX),8),
                LoadAndSaveEntry.Create(reader => WalkY = reader.ReadInt16(), writer => writer.Write(WalkY),8),
                LoadAndSaveEntry.Create(reader => Number = reader.ReadUInt16(), writer => writer.Write(Number),8),
                LoadAndSaveEntry.Create(reader => {
                    Position = new Point(reader.ReadInt16(),reader.ReadInt16());
                }, writer => {
                    writer.Write(Position.X);
                    writer.Write(Position.Y);
                },8),
                LoadAndSaveEntry.Create(reader => Width = reader.ReadUInt16(), writer => writer.Write(Width),8),
                LoadAndSaveEntry.Create(reader => Height = reader.ReadUInt16(), writer => writer.Write(Height),8),
                LoadAndSaveEntry.Create(reader => ActorDir = reader.ReadByte(), writer => writer.Write(ActorDir),8),
                LoadAndSaveEntry.Create(reader => ParentState = reader.ReadByte(), writer => writer.Write(ParentState),8),
                LoadAndSaveEntry.Create(reader => Parent = reader.ReadByte(), writer => writer.Write(Parent),8),
                LoadAndSaveEntry.Create(reader => State = reader.ReadByte(), writer => writer.Write(State),8),
                LoadAndSaveEntry.Create(reader => FlObjectIndex = reader.ReadByte(), writer => writer.Write(FlObjectIndex),8),
                LoadAndSaveEntry.Create(reader => Flags = (DrawBitmaps)reader.ReadByte(), writer => writer.Write((byte)Flags),46),
            };
            Array.ForEach(objectEntries, e => e.Execute(serializer));
        }
    }
}
