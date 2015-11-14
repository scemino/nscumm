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

using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    class Slot
    {
        public Point Position { get; set; }

        public short Right { get; set; }

        public byte Color { get; set; }

        public byte Charset { get; set; }

        public int Height;
        public bool Center;
        public bool Overhead;
        public bool NoTalkAnim;
        public bool Wrapping;

        public Slot()
        {
            Position = new Point();
            Right = 319;
            Color = 0xF;
            Charset = 4;
        }

        public void CopyFrom(Slot s)
        {
            Position = s.Position;
            Right = s.Right;
            Color = s.Color;
            Charset = s.Charset;
            Center = s.Center;
            Overhead = s.Overhead;
            NoTalkAnim = s.NoTalkAnim;
            Wrapping = s.Wrapping;
        }
    }

    class TextSlot : Slot
    {
        Slot _default = new Slot();

        public Slot Default
        {
            get { return _default; }
        }

        public void SaveDefault()
        {
            _default.CopyFrom(this);
        }

        public void LoadDefault()
        {
            CopyFrom(_default);
        }

        public void SaveOrLoad(Serializer serializer)
        {
            var stringTabEntries = new[]
            {
                LoadAndSaveEntry.Create(reader =>
                    {
                        var xPos = reader.ReadInt16(); 
                        var defaultXPos = reader.ReadInt16();
                        var yPos = reader.ReadInt16();
                        var defaultYPos = reader.ReadInt16();
                        Position = new Point(xPos, yPos);
                        _default.Position = new Point(defaultXPos, defaultYPos);
                    }, writer =>
                    {
                        writer.WriteInt16(Position.X);
                        writer.WriteInt16(_default.Position.X);
                        writer.WriteInt16(Position.Y);
                        writer.WriteInt16(_default.Position.Y);
                    }, 8),
                LoadAndSaveEntry.Create(reader => Right = reader.ReadInt16(), writer => writer.WriteInt16(Right), 8),
                LoadAndSaveEntry.Create(reader => _default.Right = reader.ReadInt16(), writer => writer.WriteInt16(_default.Right), 8),
                LoadAndSaveEntry.Create(reader => Color = reader.ReadByte(), writer => writer.WriteByte(Color), 8),
                LoadAndSaveEntry.Create(reader => _default.Color = reader.ReadByte(), writer => writer.WriteByte(_default.Color), 8),
                LoadAndSaveEntry.Create(reader => Charset = reader.ReadByte(), writer => writer.WriteByte(Charset), 8),
                LoadAndSaveEntry.Create(reader => _default.Charset = reader.ReadByte(), writer => writer.WriteByte(_default.Charset), 8),
                LoadAndSaveEntry.Create(reader => Center = reader.ReadBoolean(), writer => writer.WriteByte(Center), 8),
                LoadAndSaveEntry.Create(reader => _default.Center = reader.ReadBoolean(), writer => writer.WriteByte(_default.Center), 8),
                LoadAndSaveEntry.Create(reader => Overhead = reader.ReadBoolean(), writer => writer.WriteByte(Overhead), 8),
                LoadAndSaveEntry.Create(reader => _default.Overhead = reader.ReadBoolean(), writer => writer.WriteByte(_default.Overhead), 8),
                LoadAndSaveEntry.Create(reader => NoTalkAnim = reader.ReadBoolean(), writer => writer.WriteByte(NoTalkAnim), 8),
                LoadAndSaveEntry.Create(reader => _default.NoTalkAnim = reader.ReadBoolean(), writer => writer.WriteByte(_default.NoTalkAnim), 8),
                LoadAndSaveEntry.Create(reader => Wrapping = reader.ReadBoolean(), writer => writer.WriteByte(Wrapping), 71),
                LoadAndSaveEntry.Create(reader => _default.Wrapping = reader.ReadBoolean(), writer => writer.WriteByte(_default.Wrapping), 71)
            };
            stringTabEntries.ForEach(e => e.Execute(serializer));
        }
    }
}
