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
using System.Linq;
using System.Text;

namespace Scumm4
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

        public void Load(BinaryReader reader, uint version)
        {
            var verbEntries = new[]{
                LoadAndSaveEntry.Create(()=> curRect.left = reader.ReadInt16(),8),
                LoadAndSaveEntry.Create(()=> curRect.top = reader.ReadInt16(),8),
                LoadAndSaveEntry.Create(()=> curRect.right = reader.ReadInt16(),8),
                LoadAndSaveEntry.Create(()=> curRect.bottom = reader.ReadInt16(),8),
                LoadAndSaveEntry.Create(()=> oldRect.left = reader.ReadInt16(),8),
                LoadAndSaveEntry.Create(()=> oldRect.top = reader.ReadInt16(),8),
                LoadAndSaveEntry.Create(()=> oldRect.right = reader.ReadInt16(),8),
                LoadAndSaveEntry.Create(()=> oldRect.bottom = reader.ReadInt16(),8),

                LoadAndSaveEntry.Create(()=> verbid = reader.ReadByte(),8,11),
                LoadAndSaveEntry.Create(()=> verbid = reader.ReadUInt16(),12),

                LoadAndSaveEntry.Create(()=> color = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> hicolor = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> dimcolor = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> bkcolor = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> type = (VerbType)reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> charset_nr = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> curmode = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> saveid = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> key = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> center = reader.ReadBoolean(),8),
                LoadAndSaveEntry.Create(()=> prep = reader.ReadByte(),8),
                LoadAndSaveEntry.Create(()=> imgindex = reader.ReadUInt16(),8),
            };
            Array.ForEach(verbEntries, e => e.Execute(version));
        }
    }


}
