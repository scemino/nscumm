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
using NScumm.Scumm.IO;

namespace NScumm.Scumm
{
    public class ColorCycle
    {
        public ushort Delay;
        public ushort Counter;
        public ushort Flags;
        public byte Start;
        public byte End;

        public void SaveOrLoad(Serializer serializer)
        {
            var colorCycleEntries = new[]
            {
                LoadAndSaveEntry.Create(reader => Delay = reader.ReadUInt16(), writer => writer.WriteUInt16(Delay), 8),
                LoadAndSaveEntry.Create(reader => Counter = reader.ReadUInt16(), writer => writer.WriteUInt16(Counter), 8),
                LoadAndSaveEntry.Create(reader => Flags = reader.ReadUInt16(), writer => writer.WriteUInt16(Flags), 8),
                LoadAndSaveEntry.Create(reader => Start = reader.ReadByte(), writer => writer.WriteByte(Start), 8),
                LoadAndSaveEntry.Create(reader => End = reader.ReadByte(), writer => writer.WriteByte(End), 8),
            };
            colorCycleEntries.ForEach(e => e.Execute(serializer));
        }
    }
}
