//
//  ImTrigger.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Linq;
using NScumm.Core;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Audio.IMuse
{
    class ImTrigger
    {
        public int Sound { get; set; }

        public byte Id { get; set; }

        public ushort Expire { get; set; }

        public int[] Command { get; private set; }

        public ImTrigger()
        { 
            Command = new int[8];
        }

        public void SaveOrLoad(Serializer ser)
        {
            var snmTriggerEntries = new []
            {
                LoadAndSaveEntry.Create(r => Sound = r.ReadInt16(), w => w.WriteInt16(Sound), 54),
                LoadAndSaveEntry.Create(r => Id = r.ReadByte(), w => w.WriteByte(Id), 54),
                LoadAndSaveEntry.Create(r => Expire = r.ReadUInt16(), w => w.WriteUInt16(Expire), 54),
                    LoadAndSaveEntry.Create(r => Command = r.ReadUInt16s(8).Select(i => (int)i).ToArray(), w => w.WriteUInt16s(Command.Select(i => (ushort)i).ToArray(), 8), 54),
            };
            snmTriggerEntries.ForEach(e => e.Execute(ser));
        }
    }
    
}
