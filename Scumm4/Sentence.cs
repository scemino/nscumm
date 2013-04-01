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

namespace Scumm4
{
    internal class Sentence
    {
        public byte verb;
        public byte preposition;
        public ushort objectA;
        public ushort objectB;
        public byte freezeCount;

        public void Load(System.IO.BinaryReader reader, uint version)
        {
             var sentenceEntries = new[]{
                    LoadAndSaveEntry.Create(()=> verb = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> preposition = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> objectA = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> objectB = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> freezeCount = reader.ReadByte(),8),
             };
             Array.ForEach(sentenceEntries, e => e.Execute(version));
        }
    }
}
