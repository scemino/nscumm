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
    class Sentence
    {
        public byte Verb;
        public byte Preposition;
        public ushort ObjectA;
        public ushort ObjectB;
        public byte FreezeCount;

        public void SaveOrLoad(Serializer serializer)
        {
            var sentenceEntries = new[]{
                    LoadAndSaveEntry.Create(reader => Verb = reader.ReadByte(), writer => writer.Write(Verb), 8),
                    LoadAndSaveEntry.Create(reader => Preposition = reader.ReadByte(), writer => writer.Write(Preposition),8),
                    LoadAndSaveEntry.Create(reader => ObjectA = reader.ReadUInt16(), writer => writer.Write(ObjectA),8),
                    LoadAndSaveEntry.Create(reader => ObjectB = reader.ReadUInt16(), writer => writer.Write(ObjectB),8),
                    LoadAndSaveEntry.Create(reader => FreezeCount = reader.ReadByte(), writer => writer.Write(FreezeCount),8),
             };
            Array.ForEach(sentenceEntries, e => e.Execute(serializer));
        }
    }
}
