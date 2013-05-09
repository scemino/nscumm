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

namespace Scumm4
{
    internal class Sentence
    {
        public byte verb;
        public byte preposition;
        public ushort objectA;
        public ushort objectB;
        public byte freezeCount;

        public void SaveOrLoad(Serializer serializer)
        {
            var sentenceEntries = new[]{
                    LoadAndSaveEntry.Create(reader => verb = reader.ReadByte(), writer => writer.Write(verb), 8),
                    LoadAndSaveEntry.Create(reader => preposition = reader.ReadByte(), writer => writer.Write(preposition),8),
                    LoadAndSaveEntry.Create(reader => objectA = reader.ReadUInt16(), writer => writer.Write(objectA),8),
                    LoadAndSaveEntry.Create(reader => objectB = reader.ReadUInt16(), writer => writer.Write(objectB),8),
                    LoadAndSaveEntry.Create(reader => freezeCount = reader.ReadByte(), writer => writer.Write(freezeCount),8),
             };
            Array.ForEach(sentenceEntries, e => e.Execute(serializer));
        }
    }
}
