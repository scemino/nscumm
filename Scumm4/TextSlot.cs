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
using System.Linq;
using System.Text;

namespace Scumm4
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

        public void Load(System.IO.BinaryReader reader, uint version)
        {
            var stringTabEntries = new[]{
                    LoadAndSaveEntry.Create(()=> xpos = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _default.xpos = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> ypos = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _default.ypos = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> right= reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _default.right= reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> color= reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _default.color= reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> charset= reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _default.charset= reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> center = reader.ReadBoolean(),8),
                    LoadAndSaveEntry.Create(()=> _default.center = reader.ReadBoolean(),8),
                    LoadAndSaveEntry.Create(()=> overhead = reader.ReadBoolean(),8),
                    LoadAndSaveEntry.Create(()=> _default.overhead = reader.ReadBoolean(),8),
                    LoadAndSaveEntry.Create(()=> no_talk_anim = reader.ReadBoolean(),8),
                    LoadAndSaveEntry.Create(()=> _default.no_talk_anim = reader.ReadBoolean(),8),
                    LoadAndSaveEntry.Create(()=> wrapping = reader.ReadBoolean(),71),
                    LoadAndSaveEntry.Create(()=> _default.wrapping = reader.ReadBoolean(),71)
             };
            Array.ForEach(stringTabEntries, e => e.Execute(version));
        }
    }
}
