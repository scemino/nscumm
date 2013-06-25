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

using NScumm.Core.Graphics;
using NScumm.Core.IO;
using System;

namespace NScumm.Core
{
    public enum VerbType
    {
        Text = 0,
        Image = 1
    }

    public class VerbSlot
    {
        public Rect CurRect;
        public Rect OldRect;
        public ushort VerbId;
        public byte Color, HiColor, DimColor, BkColor;
        public VerbType Type;
        public byte CharsetNr, CurMode;
        public ushort SaveId;
        public byte Key;
        public bool Center;
        public byte Prep;
        public ushort ImgIndex;

        public byte[] Text { get; set; }

        public byte[] Image { get; set; }

        public ushort ImageWidth { get; set; }
        public ushort ImageHeight { get; set; }

        public void SaveOrLoad(Serializer serializer)
        {
            var verbEntries = new[]{
                LoadAndSaveEntry.Create(reader => CurRect.Left = reader.ReadInt16(), writer => writer.WriteInt16(CurRect.Left),8),
                LoadAndSaveEntry.Create(reader => CurRect.Top = reader.ReadInt16(), writer => writer.WriteInt16(CurRect.Top),8),
                LoadAndSaveEntry.Create(reader => CurRect.Right = reader.ReadInt16(), writer => writer.WriteInt16(CurRect.Right),8),
                LoadAndSaveEntry.Create(reader => CurRect.Bottom = reader.ReadInt16(), writer => writer.WriteInt16(CurRect.Bottom),8),
                LoadAndSaveEntry.Create(reader => OldRect.Left = reader.ReadInt16(), writer => writer.WriteInt16(OldRect.Left),8),
                LoadAndSaveEntry.Create(reader => OldRect.Top = reader.ReadInt16(), writer => writer.WriteInt16(OldRect.Top),8),
                LoadAndSaveEntry.Create(reader => OldRect.Right = reader.ReadInt16(), writer => writer.WriteInt16(OldRect.Right),8),
                LoadAndSaveEntry.Create(reader => OldRect.Bottom = reader.ReadInt16(), writer => writer.WriteInt16(OldRect.Bottom),8),
                                               
                LoadAndSaveEntry.Create(reader => VerbId = reader.ReadByte(), writer => writer.Write((byte)VerbId),8,11),
                LoadAndSaveEntry.Create(reader => VerbId = reader.ReadUInt16(), writer => writer.Write(VerbId),12),
                                               
                LoadAndSaveEntry.Create(reader => Color = reader.ReadByte(), writer => writer.Write(Color),8),
                LoadAndSaveEntry.Create(reader => HiColor = reader.ReadByte(), writer => writer.Write(HiColor),8),
                LoadAndSaveEntry.Create(reader => DimColor = reader.ReadByte(), writer => writer.Write(DimColor),8),
                LoadAndSaveEntry.Create(reader => BkColor = reader.ReadByte(), writer => writer.Write(BkColor),8),
                LoadAndSaveEntry.Create(reader => Type = (VerbType)reader.ReadByte(), writer => writer.Write((byte)Type),8),
                LoadAndSaveEntry.Create(reader => CharsetNr = reader.ReadByte(), writer => writer.Write(CharsetNr),8),
                LoadAndSaveEntry.Create(reader => CurMode = reader.ReadByte(), writer => writer.Write(CurMode),8),
                LoadAndSaveEntry.Create(reader => SaveId = reader.ReadByte(), writer => writer.WriteByte(SaveId),8),
                LoadAndSaveEntry.Create(reader => Key = reader.ReadByte(), writer => writer.Write(Key),8),
                LoadAndSaveEntry.Create(reader => Center = reader.ReadBoolean(), writer => writer.Write(Center),8),
                LoadAndSaveEntry.Create(reader => Prep = reader.ReadByte(), writer => writer.Write(Prep),8),
                LoadAndSaveEntry.Create(reader => ImgIndex = reader.ReadUInt16(), writer => writer.Write(ImgIndex),8),
            };
            Array.ForEach(verbEntries, e => e.Execute(serializer));
        }
    }
}
