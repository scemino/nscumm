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
    public class Slot
    {
        public short XPos = 2;
        public short YPos = 5;
        public short Right = 319;
        public short Height = 0;
        public byte Color = 0xF;
        public byte Charset = 4;
        public bool Center;
        public bool Overhead;
        public bool NoTalkAnim;
        public bool Wrapping;

        public void CopyFrom(Slot s)
        {
            XPos = s.XPos;
            YPos = s.YPos;
            Right = s.Right;
            Color = s.Color;
            Charset = s.Charset;
            Center = s.Center;
            Overhead = s.Overhead;
            NoTalkAnim = s.NoTalkAnim;
            Wrapping = s.Wrapping;
        }
    }

    public class TextSlot : Slot
    {
        Slot _default = new Slot();

        public Slot Default
        {
            get { return _default; }
        }

        public void SaveDefault()
        {
            _default.CopyFrom(this);
        }

        public void LoadDefault()
        {
            CopyFrom(_default);
        }

        public void SaveOrLoad(Serializer serializer)
        {
            var stringTabEntries = new[]{
                    LoadAndSaveEntry.Create(reader => XPos = reader.ReadInt16(), writer => writer.Write(XPos), 8),
                    LoadAndSaveEntry.Create(reader => _default.XPos = reader.ReadInt16(), writer => writer.Write(_default.XPos),8),
                    LoadAndSaveEntry.Create(reader => YPos = reader.ReadInt16(), writer => writer.Write(YPos),8),
                    LoadAndSaveEntry.Create(reader => _default.YPos = reader.ReadInt16(), writer => writer.Write(_default.YPos),8),
                    LoadAndSaveEntry.Create(reader => Right= reader.ReadInt16(), writer => writer.Write(Right),8),
                    LoadAndSaveEntry.Create(reader => _default.Right= reader.ReadInt16(), writer => writer.Write(_default.Right),8),
                    LoadAndSaveEntry.Create(reader => Color= reader.ReadByte(), writer => writer.Write(Color),8),
                    LoadAndSaveEntry.Create(reader => _default.Color= reader.ReadByte(), writer => writer.Write(_default.Color),8),
                    LoadAndSaveEntry.Create(reader => Charset= reader.ReadByte(), writer => writer.Write(Charset),8),
                    LoadAndSaveEntry.Create(reader => _default.Charset= reader.ReadByte(), writer => writer.Write(_default.Charset),8),
                    LoadAndSaveEntry.Create(reader => Center = reader.ReadBoolean(), writer => writer.Write(Center),8),
                    LoadAndSaveEntry.Create(reader => _default.Center = reader.ReadBoolean(), writer => writer.Write(_default.Center),8),
                    LoadAndSaveEntry.Create(reader => Overhead = reader.ReadBoolean(), writer => writer.Write(Overhead),8),
                    LoadAndSaveEntry.Create(reader => _default.Overhead = reader.ReadBoolean(), writer => writer.Write(_default.Overhead),8),
                    LoadAndSaveEntry.Create(reader => NoTalkAnim = reader.ReadBoolean(), writer => writer.Write(NoTalkAnim),8),
                    LoadAndSaveEntry.Create(reader => _default.NoTalkAnim = reader.ReadBoolean(), writer => writer.Write(_default.NoTalkAnim),8),
                    LoadAndSaveEntry.Create(reader => Wrapping = reader.ReadBoolean(), writer => writer.Write(Wrapping),71),
                    LoadAndSaveEntry.Create(reader => _default.Wrapping = reader.ReadBoolean(), writer => writer.Write(_default.Wrapping),71)
             };
            Array.ForEach(stringTabEntries, e => e.Execute(serializer));
        }
    }
}
