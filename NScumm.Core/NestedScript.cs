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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NScumm.Core
{
    public class NestedScript
    {
        public ushort number;
        public WhereIsObject where;
        public byte slot;

        public void SaveOrLoad(Serializer serializer)
        {
            var nestedScriptEntries = new[]{
                LoadAndSaveEntry.Create(reader => number = reader.ReadUInt16(), writer => writer.Write(number),8),
                LoadAndSaveEntry.Create(reader => where = (WhereIsObject)reader.ReadByte(), writer => writer.Write((byte)where),8),
                LoadAndSaveEntry.Create(reader => slot = reader.ReadByte(), writer => writer.Write(slot),8),
            };
            Array.ForEach(nestedScriptEntries, e => e.Execute(serializer));
        }
    }
}
