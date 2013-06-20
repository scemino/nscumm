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
    public class ColorCycle
    {
        public ushort delay;
        public ushort counter;
        public ushort flags;
        public byte start;
        public byte end;

        public void SaveOrLoad(Serializer serializer)
        {
            var colorCycleEntries = new[]{
                    LoadAndSaveEntry.Create(reader => delay = reader.ReadUInt16(), writer => writer.Write(delay), 8),
                    LoadAndSaveEntry.Create(reader => counter = reader.ReadUInt16(), writer => writer.Write(counter),8),
                    LoadAndSaveEntry.Create(reader => flags = reader.ReadUInt16(), writer => writer.Write(flags),8),
                    LoadAndSaveEntry.Create(reader => start = reader.ReadByte(), writer => writer.Write(start),8),
                    LoadAndSaveEntry.Create(reader => end = reader.ReadByte(), writer => writer.Write(end),8),
             };
            Array.ForEach(colorCycleEntries, e => e.Execute(serializer));
        }
    }
}
