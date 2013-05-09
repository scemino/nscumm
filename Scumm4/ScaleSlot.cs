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

using Scumm4.IO;
using System;
using System.IO;

namespace Scumm4
{
    public class ScaleSlot
    {
        public int x1, y1, scale1;
        public int x2, y2, scale2;

        public void SaveOrLoad(Serializer serializer)
        {
            var scaleSlotsEntries = new[]{
                    LoadAndSaveEntry.Create(reader => x1 = reader.ReadInt16(), writer => writer.WriteInt16(x1),13),
                    LoadAndSaveEntry.Create(reader => y1 = reader.ReadInt16(), writer => writer.WriteInt16(y1),13),
                    LoadAndSaveEntry.Create(reader => scale1 = reader.ReadInt16(), writer => writer.WriteInt16(scale1),13),
                    LoadAndSaveEntry.Create(reader => x2 = reader.ReadInt16(), writer => writer.WriteInt16(x2),13),
                    LoadAndSaveEntry.Create(reader => y2 = reader.ReadInt16(), writer => writer.WriteInt16(y2),13),
                    LoadAndSaveEntry.Create(reader => scale2 = reader.ReadInt16(), writer => writer.WriteInt16(scale2),13),
             };
            Array.ForEach(scaleSlotsEntries, e => e.Execute(serializer));
        }
    }
}
