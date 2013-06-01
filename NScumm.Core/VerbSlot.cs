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
using System.IO;

namespace NScumm.Core
{
    public enum VerbType
    {
        Text = 0,
        Image = 1
    }

    public class VerbSlot
    {
        public Rect curRect;
        public Rect oldRect;
        public ushort verbid;
        public byte color, hicolor, dimcolor, bkcolor;
        public VerbType type;
        public byte charset_nr, curmode;
        public ushort saveid;
        public byte key;
        public bool center;
        public byte prep;
        public ushort imgindex;

        public byte[] Text { get; set; }

        public void SaveOrLoad(Serializer serializer)
        {
            var verbEntries = new[]{
                LoadAndSaveEntry.Create(reader => curRect.left = reader.ReadInt16(), writer => writer.WriteInt16(curRect.left),8),
                LoadAndSaveEntry.Create(reader => curRect.top = reader.ReadInt16(), writer => writer.WriteInt16(curRect.top),8),
                LoadAndSaveEntry.Create(reader => curRect.right = reader.ReadInt16(), writer => writer.WriteInt16(curRect.right),8),
                LoadAndSaveEntry.Create(reader => curRect.bottom = reader.ReadInt16(), writer => writer.WriteInt16(curRect.bottom),8),
                LoadAndSaveEntry.Create(reader => oldRect.left = reader.ReadInt16(), writer => writer.WriteInt16(oldRect.left),8),
                LoadAndSaveEntry.Create(reader => oldRect.top = reader.ReadInt16(), writer => writer.WriteInt16(oldRect.top),8),
                LoadAndSaveEntry.Create(reader => oldRect.right = reader.ReadInt16(), writer => writer.WriteInt16(oldRect.right),8),
                LoadAndSaveEntry.Create(reader => oldRect.bottom = reader.ReadInt16(), writer => writer.WriteInt16(oldRect.bottom),8),
                                               
                LoadAndSaveEntry.Create(reader => verbid = reader.ReadByte(), writer => writer.Write((byte)verbid),8,11),
                LoadAndSaveEntry.Create(reader => verbid = reader.ReadUInt16(), writer => writer.Write(verbid),12),
                                               
                LoadAndSaveEntry.Create(reader => color = reader.ReadByte(), writer => writer.Write(color),8),
                LoadAndSaveEntry.Create(reader => hicolor = reader.ReadByte(), writer => writer.Write(hicolor),8),
                LoadAndSaveEntry.Create(reader => dimcolor = reader.ReadByte(), writer => writer.Write(dimcolor),8),
                LoadAndSaveEntry.Create(reader => bkcolor = reader.ReadByte(), writer => writer.Write(bkcolor),8),
                LoadAndSaveEntry.Create(reader => type = (VerbType)reader.ReadByte(), writer => writer.Write((byte)type),8),
                LoadAndSaveEntry.Create(reader => charset_nr = reader.ReadByte(), writer => writer.Write(charset_nr),8),
                LoadAndSaveEntry.Create(reader => curmode = reader.ReadByte(), writer => writer.Write(curmode),8),
                LoadAndSaveEntry.Create(reader => saveid = reader.ReadByte(), writer => writer.WriteByte(saveid),8),
                LoadAndSaveEntry.Create(reader => key = reader.ReadByte(), writer => writer.Write(key),8),
                LoadAndSaveEntry.Create(reader => center = reader.ReadBoolean(), writer => writer.Write(center),8),
                LoadAndSaveEntry.Create(reader => prep = reader.ReadByte(), writer => writer.Write(prep),8),
                LoadAndSaveEntry.Create(reader => imgindex = reader.ReadUInt16(), writer => writer.Write(imgindex),8),
            };
            Array.ForEach(verbEntries, e => e.Execute(serializer));
        }
    }
}
