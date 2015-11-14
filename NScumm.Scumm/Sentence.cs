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
    class Sentence
    {
        byte freezeCount;

        public byte Verb { get; private set; }

        public bool Preposition { get; private set; }

        public ushort ObjectA { get; private set; }

        public ushort ObjectB { get; private set; }

        public bool IsFrozen
        {
            get{ return freezeCount > 0; }
        }

        public Sentence()
        {
        }

        public Sentence(byte verb, ushort objectA, ushort objectB)
        {
            Verb = verb;
            ObjectA = objectA;
            ObjectB = objectB;
            Preposition = objectB != 0;
        }

        public void Freeze()
        {
            freezeCount++;
        }

        public void Unfreeze()
        {
            if (IsFrozen)
                freezeCount--;
        }

        public void SaveOrLoad(Serializer serializer)
        {
            var sentenceEntries = new[]
            {
                LoadAndSaveEntry.Create(reader => Verb = reader.ReadByte(), writer => writer.WriteByte(Verb), 8),
                LoadAndSaveEntry.Create(reader => Preposition = reader.ReadBoolean(), writer => writer.WriteByte(Preposition), 8),
                LoadAndSaveEntry.Create(reader => ObjectA = reader.ReadUInt16(), writer => writer.WriteUInt16(ObjectA), 8),
                LoadAndSaveEntry.Create(reader => ObjectB = reader.ReadUInt16(), writer => writer.WriteUInt16(ObjectB), 8),
                LoadAndSaveEntry.Create(reader => freezeCount = reader.ReadByte(), writer => writer.WriteByte(freezeCount), 8),
            };
            sentenceEntries.ForEach(e => e.Execute(serializer));
        }
    }
}
