//
//  ResourceIndex0.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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
using NScumm.Core.IO;

namespace NScumm.Scumm.IO
{
    public class ResourceIndex0: ResourceIndex
    {
        byte[] roomDisks = new byte[59];
        byte[] roomTracks = new byte[59];
        byte[] roomSectors = new byte[59];

        protected override void LoadIndex(GameInfo game)
        {
            var disk1 = string.Format(game.Pattern, 1);
            var directory = ServiceLocator.FileStorage.GetDirectoryName(game.Path);
            var path = ScummHelper.LocatePath(directory, disk1);
            sectorOffset = game.Platform == Platform.Apple2GS ? AppleSectorOffset : C64SectorOffset;

            int numGlobalObjects;
            int numRooms;
            int numCostumes;
            int numScripts;
            int numSounds;

            if (game.GameId == GameId.Maniac)
            {
                numGlobalObjects = 256;
                numRooms = 55;
                numCostumes = 25;

                if (game.Features.HasFlag(GameFeatures.Demo))
                {
                    numScripts = 55;
                    numSounds = 40;
                }
                else
                {
                    numScripts = 160;
                    numSounds = 70;
                }
            }
            else
            {
                numGlobalObjects = 775;
                numRooms = 59;
                numCostumes = 38;
                numScripts = 155;
                numSounds = 127;
            }

            using (var disk = ServiceLocator.FileStorage.OpenFileRead(path))
            {
                var br = new BinaryReader(disk);
                if (Game.Platform == Platform.Apple2GS)
                {
                    br.BaseStream.Seek(142080, SeekOrigin.Begin);
                }

                var signature = br.ReadUInt16();
                if (signature != 0x0A31)
                {
                    throw new NotSupportedException(string.Format("Invalid signature '{0:X}' in disk 1", signature));
                }

                // object flags
                ObjectOwnerTable = new byte[numGlobalObjects];
                ObjectStateTable = new byte[numGlobalObjects];
                ClassData = new uint[numGlobalObjects];
                for (int i = 0; i < numGlobalObjects; i++)
                {
                    var tmp = br.ReadByte();
                    ObjectOwnerTable[i] = (byte)(tmp & 0x0F);
                    ObjectStateTable[i] = (byte)(tmp >> 4);
                }

                // room offsets
                for (var i = 0; i < numRooms; i++)
                {
                    roomDisks[i] = (byte)(br.ReadByte() - '0');
                }
                for (var i = 0; i < numRooms; i++)
                {
                    roomSectors[i] = br.ReadByte();
                    roomTracks[i] = br.ReadByte();
                }
                CostumeResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br, numCostumes));
                ScriptResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br, numScripts));
                SoundResources = new ReadOnlyCollection<Resource>(ReadResTypeList(br, numSounds));
            }
        }

        public int GetRoomDisk(byte res)
        {
            return roomDisks[res];
        }

        public int GetResourceOffset(byte res)
        {
            return (sectorOffset[roomTracks[res]] + roomSectors[res]) * 256;
        }

        protected virtual Resource[] ReadResTypeList(BinaryReader br, int? numEntries = null)
        {
            var num = numEntries.HasValue ? numEntries.Value : br.ReadByte();
            var res = new Resource[num];
            var rooms = br.ReadBytes(num);
            for (int i = 0; i < num; i++)
            {
                var offset = ToOffset(br.ReadUInt16());
                res[i] = new Resource{ RoomNum = rooms[i], Offset = offset };
            }
            return res;
        }

        static uint ToOffset(ushort offset)
        {
            return offset == 0xFFFF ? 0xFFFFFFFF : offset;
        }

        int[] sectorOffset;

        static readonly int[] C64SectorOffset =
        {
            0,
            0, 21, 42, 63, 84, 105, 126, 147, 168, 189, 210, 231, 252, 273, 294, 315, 336,
            357, 376, 395, 414, 433, 452, 471,
            490, 508, 526, 544, 562, 580,
            598, 615, 632, 649, 666
        };

        static readonly int[] AppleSectorOffset =
        {
            0, 16, 32, 48, 64, 80, 96, 112, 128, 144, 160, 176, 192, 208, 224, 240, 256,
            272, 288, 304, 320, 336, 352, 368,
            384, 400, 416, 432, 448, 464,
            480, 496, 512, 528, 544, 560
        };
    }
}

