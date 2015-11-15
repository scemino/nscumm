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
    public class ScaleSlot
    {
        public int X1, Y1, Scale1;
        public int X2, Y2, Scale2;

        public void SaveOrLoad(Serializer serializer)
        {
            var scaleSlotsEntries = new[]
            {
                LoadAndSaveEntry.Create(reader => X1 = reader.ReadInt16(), writer => writer.WriteInt16(X1), 13),
                LoadAndSaveEntry.Create(reader => Y1 = reader.ReadInt16(), writer => writer.WriteInt16(Y1), 13),
                LoadAndSaveEntry.Create(reader => Scale1 = reader.ReadInt16(), writer => writer.WriteInt16(Scale1), 13),
                LoadAndSaveEntry.Create(reader => X2 = reader.ReadInt16(), writer => writer.WriteInt16(X2), 13),
                LoadAndSaveEntry.Create(reader => Y2 = reader.ReadInt16(), writer => writer.WriteInt16(Y2), 13),
                LoadAndSaveEntry.Create(reader => Scale2 = reader.ReadInt16(), writer => writer.WriteInt16(Scale2), 13),
            };
            scaleSlotsEntries.ForEach(e => e.Execute(serializer));
        }
    }
}
