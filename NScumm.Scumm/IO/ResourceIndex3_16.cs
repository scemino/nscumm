//
//  ResourceIndex3_16.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
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

using System;
using System.Collections.ObjectModel;
using System.IO;
using NScumm.Core;

namespace NScumm.Scumm.IO
{
    class ResourceIndex3_16: ResourceIndex
    {
        protected override void LoadIndex(GameInfo game)
        {
            using (var file = ServiceLocator.FileStorage.OpenFileRead(game.Path))
            {
                var br = new BinaryReader(new XorStream(file,0xFF));
                var magic = br.ReadUInt16();
                if (magic != 0x0100)
                    throw new NotSupportedException(
                        string.Format("The magic id doesn't match ({0:X2})", magic));

                ReadDirectoryOfObjects(br);
                RoomResources = new ReadOnlyCollection<Resource>(ReadRoomResTypeList(br));
                CostumeResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br));
                ScriptResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br));
                SoundResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br));
            }
        }

        protected virtual Resource[] ReadResTypeList(BinaryReader br)
        {
            var num = br.ReadByte();
            var res = new Resource[num];
            var rooms = br.ReadBytes(num);
            for (int i = 0; i < num; i++)
            {
                var offset = ToOffset(br.ReadUInt16());
                res[i] = new Resource{ RoomNum = rooms[i], Offset = offset };
            }
            return res;
        }

        protected virtual void ReadDirectoryOfObjects(BinaryReader br)
        {
            var numEntries = br.ReadUInt16();
            ObjectOwnerTable = new byte[numEntries];
            ObjectStateTable = new byte[numEntries];
            ClassData = new uint[numEntries];
            uint bits;
            for (int i = 0; i < numEntries; i++)
            {
                bits = br.ReadByte();
                bits |= (uint)(br.ReadByte() << 8);
                bits |= (uint)(br.ReadByte() << 16);
                ClassData[i] = bits;
                var tmp = br.ReadByte();
                ObjectStateTable[i] = (byte)(tmp >> 4);
                ObjectOwnerTable[i] = (byte)(tmp & 0x0F);
            }
        }

        static Resource[] ReadRoomResTypeList(BinaryReader br)
        {
            var num = br.ReadByte();
            var rooms = new Resource[num];
            br.ReadBytes(num); // disk file numbers
            for (int i = 0; i < num; i++)
            {
                var offset = ToOffset(br.ReadUInt16());
                rooms[i] = new Resource{ RoomNum = (byte)i, Offset = offset };
            }
            return rooms;
        }

        static uint ToOffset(ushort offset)
        {
            return offset == 0xFFFF ? 0xFFFFFFFF : offset;
        }
    }
}

