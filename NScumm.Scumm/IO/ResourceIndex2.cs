//
//  ResourceIndex2.cs
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
    class ResourceIndex2 : ResourceIndex
    {
        protected override void LoadIndex(GameInfo game)
        {
            using (var file = ServiceLocator.FileStorage.OpenFileRead(game.Path))
            {
                var br = new BinaryReader(new XorStream(file, 0xFF));
                var magic = br.ReadUInt16();
                switch (magic)
                {
                    case 0x0A31:
                        // Classic V1 game detected
                        ReadClassicIndexFile(br);
                        break;
                    case 0x0100:
                        // Enhanced V2 game detected
                        ReadEnhancedIndexFile(br);
                        break;
                    default:
                        throw new NotSupportedException(
                            string.Format("The magic id doesn't match ({0:X2})", magic));
                }
            }
        }

        void ReadClassicIndexFile(BinaryReader br)
        {
            int numGlobalObjects, numRooms, numCostumes, numScripts, numSounds;
            if (Game.GameId == GameId.Maniac)
            {
                numGlobalObjects = 800;
                numRooms = 55;
                numCostumes = 35;
                numScripts = 200;
                numSounds = 100;
            }
            else if (Game.GameId == GameId.Zak)
            {
                numGlobalObjects = 775;
                numRooms = 61;
                numCostumes = 37;
                numScripts = 155;
                numSounds = 120;
            }
            else
            {
                throw new InvalidOperationException();
            }

            ReadDirectoryOfObjects(br, numGlobalObjects);
            RoomResources = new ReadOnlyCollection<Resource>(ReadRoomResTypeList(br, numRooms));
            CostumeResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br, numCostumes));
            ScriptResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br, numScripts));
            SoundResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br, numSounds));
        }

        void ReadEnhancedIndexFile(BinaryReader br)
        {
            var numGlobalObjects = br.ReadUInt16();
            ReadDirectoryOfObjects(br, numGlobalObjects);
            RoomResources = new ReadOnlyCollection<Resource>(ReadRoomResTypeList(br));
            CostumeResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br));
            ScriptResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br));
            SoundResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br));
        }

        static Resource[] ReadRoomResTypeList(BinaryReader br, int? numEntries = null)
        {
            var num = numEntries.HasValue ? numEntries.Value : br.ReadByte();
            var rooms = new Resource[num];
            br.ReadBytes(num); // disk file numbers
            for (int i = 0; i < num; i++)
            {
                var offset = ToOffset(br.ReadUInt16());
                rooms[i] = new Resource { RoomNum = (byte)i, Offset = offset };
            }
            return rooms;
        }

        protected virtual Resource[] ReadResTypeList(BinaryReader br, int? numEntries = null)
        {
            var num = numEntries.HasValue ? numEntries.Value : br.ReadByte();
            var res = new Resource[num];
            var rooms = br.ReadBytes(num);
            for (int i = 0; i < num; i++)
            {
                var offset = ToOffset(br.ReadUInt16());
                res[i] = new Resource { RoomNum = rooms[i], Offset = offset };
            }
            return res;
        }

        protected virtual void ReadDirectoryOfObjects(BinaryReader br, int num)
        {
            ObjectOwnerTable = new byte[num];
            ObjectStateTable = new byte[num];
            ClassData = new uint[num];
            for (int i = 0; i < num; i++)
            {
                var tmp = br.ReadByte();
                ObjectOwnerTable[i] = (byte)(tmp & 0x0F);
                ObjectStateTable[i] = (byte)(tmp >> 4);
            }
        }

        static uint ToOffset(ushort offset)
        {
            return offset == 0xFFFF ? 0xFFFFFFFF : offset;
        }
    }
}

