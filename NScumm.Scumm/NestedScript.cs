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
    public class NestedScript
    {
        public ushort Number;
        public WhereIsObject Where;
        public byte Slot;

        public void SaveOrLoad(Serializer serializer)
        {
            var nestedScriptEntries = new[]
            {
                LoadAndSaveEntry.Create(reader => Number = reader.ReadUInt16(), writer => writer.WriteUInt16(Number), 8),
                LoadAndSaveEntry.Create(reader => Where = (WhereIsObject)reader.ReadByte(), writer => writer.WriteByte((byte)Where), 8),
                LoadAndSaveEntry.Create(reader => Slot = reader.ReadByte(), writer => writer.WriteByte(Slot), 8),
            };
            nestedScriptEntries.ForEach(e => e.Execute(serializer));
        }
    }
}
