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
using System.IO;

namespace Scumm4
{
    public class ScaleSlot
    {
        public int x1, y1, scale1;
        public int x2, y2, scale2;

        public void Load(BinaryReader reader, uint version)
        {
            var scaleSlotsEntries = new[]{
                    LoadAndSaveEntry.Create(()=> x1 = reader.ReadInt16(),13),
                    LoadAndSaveEntry.Create(()=> y1 = reader.ReadInt16(),13),
                    LoadAndSaveEntry.Create(()=> scale1 = reader.ReadInt16(),13),
                    LoadAndSaveEntry.Create(()=> x2 = reader.ReadInt16(),13),
                    LoadAndSaveEntry.Create(()=> y2 = reader.ReadInt16(),13),
                    LoadAndSaveEntry.Create(()=> scale2 = reader.ReadInt16(),13),
             };
            Array.ForEach(scaleSlotsEntries, e => e.Execute(version));
        }
    }
}
