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
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class ObjectData: ICloneable
    {
        public ushort Number { get; set; }

        public ushort Width { get; set; }

        public ushort Height { get; set; }

        public int ActorDir { get; set; }

        public byte Parent { get; set; }

        public byte ParentState { get; set; }

        public byte State { get; set; }

        public DrawBitmaps Flags { get; set; }

        public int FloatingObjectIndex { get; set; }

        public Point Walk { get; set; }

        public Point Position { get; set; }

        public Dictionary<int, int> ScriptOffsets { get; private set; }

        public ScriptData Script { get; private set; }

        public byte[] Name { get; set; }

        public List<ImageData> Images { get; private set; }

        public List<Point> Hotspots { get; private set; }

        public ObjectData()
        {
            ScriptOffsets = new Dictionary<int, int>();
            Script = new ScriptData();
            Images = new List<ImageData>();
            Hotspots = new List<Point>();
        }

        #region ICloneable implementation

        object ICloneable.Clone()
        {
            return Clone();
        }

        public ObjectData Clone()
        {
            var obj = (ObjectData)this.MemberwiseClone();
            obj.Script = obj.Script.Clone();
            obj.ScriptOffsets = new Dictionary<int, int>();
            foreach (var item in ScriptOffsets)
            {
                obj.ScriptOffsets.Add(item.Key, item.Value);
            }
            obj.Images.Clear();
            foreach (var img in Images)
            {
                obj.Images.Add(img.Clone());
            }
            return obj;
        }

        #endregion

        public void SaveOrLoad(Serializer serializer)
        {
            if (serializer.IsLoading)
            {
                Images.Clear();
            }
            var objectEntries = new[]
            {
                LoadAndSaveEntry.Create(reader => reader.ReadUInt32(), writer => writer.WriteUInt32(0), 8),
                LoadAndSaveEntry.Create(reader => reader.ReadUInt32(), writer => writer.WriteUInt32(0), 8),
                LoadAndSaveEntry.Create(reader =>
                    {
                        Walk = new Point(reader.ReadInt16(), reader.ReadInt16());
                    }, writer =>
                    {
                        writer.WriteInt16(Walk.X);
                        writer.WriteInt16(Walk.Y);
                    }, 8),
                LoadAndSaveEntry.Create(reader => Number = reader.ReadUInt16(), writer => writer.Write(Number), 8),
                LoadAndSaveEntry.Create(reader =>
                    {
                        Position = new Point(reader.ReadInt16(), reader.ReadInt16());
                    }, writer =>
                    {
                        writer.WriteInt16(Position.X);
                        writer.WriteInt16(Position.Y);
                    }, 8),
                LoadAndSaveEntry.Create(reader => Width = reader.ReadUInt16(), writer => writer.WriteUInt16(Width), 8),
                LoadAndSaveEntry.Create(reader => Height = reader.ReadUInt16(), writer => writer.WriteUInt16(Height), 8),
                LoadAndSaveEntry.Create(reader => ActorDir = reader.ReadByte(), writer => writer.WriteByte(ActorDir), 8),
                LoadAndSaveEntry.Create(reader => ParentState = reader.ReadByte(), writer => writer.WriteByte(ParentState), 8),
                LoadAndSaveEntry.Create(reader => Parent = reader.ReadByte(), writer => writer.WriteByte(Parent), 8),
                LoadAndSaveEntry.Create(reader => State = reader.ReadByte(), writer => writer.WriteByte(State), 8),
                LoadAndSaveEntry.Create(reader => reader.ReadByte(), writer => writer.WriteByte(0), 8),
                LoadAndSaveEntry.Create(reader => Flags = (DrawBitmaps)reader.ReadByte(), writer => writer.WriteByte((byte)Flags), 46),
            };
            Array.ForEach(objectEntries, e => e.Execute(serializer));
        }

        internal string DebuggerDisplay
        {
            get
            { 
                return Number != 0 ? string.Format("(Number: {0}, Name = {1})", Number, System.Text.Encoding.ASCII.GetString(Name)) : "Number 0";
            }    
        }
    }
}
