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

namespace NScumm.Core
{
    public class Slot
    {
        public short xpos = 2;
        public short ypos = 5;
        public short right = 319;
        public short height = 0;
        public byte color = 0xF;
        public byte charset = 4;
        public bool center = false;
        public bool overhead;
        public bool no_talk_anim;
        public bool wrapping;

        public void CopyFrom(Slot s)
        {
            this.xpos = s.xpos;
            this.ypos = s.ypos;
            this.right = s.right;
            this.color = s.color;
            this.charset = s.charset;
            this.center = s.center;
            this.overhead = s.overhead;
            this.no_talk_anim = s.no_talk_anim;
            this.wrapping = s.wrapping;
        }
    }

    public class TextSlot : Slot
    {
        private Slot _default = new Slot();

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
            var stringTabEntries = new[]{
                    LoadAndSaveEntry.Create(reader => xpos = reader.ReadInt16(), writer => writer.Write(xpos), 8),
                    LoadAndSaveEntry.Create(reader => _default.xpos = reader.ReadInt16(), writer => writer.Write(_default.xpos),8),
                    LoadAndSaveEntry.Create(reader => ypos = reader.ReadInt16(), writer => writer.Write(ypos),8),
                    LoadAndSaveEntry.Create(reader => _default.ypos = reader.ReadInt16(), writer => writer.Write(_default.ypos),8),
                    LoadAndSaveEntry.Create(reader => right= reader.ReadInt16(), writer => writer.Write(right),8),
                    LoadAndSaveEntry.Create(reader => _default.right= reader.ReadInt16(), writer => writer.Write(_default.right),8),
                    LoadAndSaveEntry.Create(reader => color= reader.ReadByte(), writer => writer.Write(color),8),
                    LoadAndSaveEntry.Create(reader => _default.color= reader.ReadByte(), writer => writer.Write(_default.color),8),
                    LoadAndSaveEntry.Create(reader => charset= reader.ReadByte(), writer => writer.Write(charset),8),
                    LoadAndSaveEntry.Create(reader => _default.charset= reader.ReadByte(), writer => writer.Write(_default.charset),8),
                    LoadAndSaveEntry.Create(reader => center = reader.ReadBoolean(), writer => writer.Write(center),8),
                    LoadAndSaveEntry.Create(reader => _default.center = reader.ReadBoolean(), writer => writer.Write(_default.center),8),
                    LoadAndSaveEntry.Create(reader => overhead = reader.ReadBoolean(), writer => writer.Write(overhead),8),
                    LoadAndSaveEntry.Create(reader => _default.overhead = reader.ReadBoolean(), writer => writer.Write(_default.overhead),8),
                    LoadAndSaveEntry.Create(reader => no_talk_anim = reader.ReadBoolean(), writer => writer.Write(no_talk_anim),8),
                    LoadAndSaveEntry.Create(reader => _default.no_talk_anim = reader.ReadBoolean(), writer => writer.Write(_default.no_talk_anim),8),
                    LoadAndSaveEntry.Create(reader => wrapping = reader.ReadBoolean(), writer => writer.Write(wrapping),71),
                    LoadAndSaveEntry.Create(reader => _default.wrapping = reader.ReadBoolean(), writer => writer.Write(_default.wrapping),71)
             };
            Array.ForEach(stringTabEntries, e => e.Execute(serializer));
        }
    }
}
