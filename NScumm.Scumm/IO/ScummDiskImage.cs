//
//  ScummDiskImage.cs
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

using System.IO;
using System;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Scumm.IO
{
    static class ScummDiskImage
    {
        public static Stream CreateResource(ResourceIndex0 index, byte resourceNumber)
        {
            var roomDisk = index.GetRoomDisk(resourceNumber);
            var disk = string.Format(index.Game.Pattern, roomDisk);
            var directory = ServiceLocator.FileStorage.GetDirectoryName(index.Game.Path);
            var path = ServiceLocator.FileStorage.OpenFileRead(ScummHelper.LocatePath(directory, disk));
            var br = new BinaryReader(path);

            if (index.Game.Platform == Platform.Apple2GS)
            {
                br.BaseStream.Seek(roomDisk == 1 ? 142080 : 143104, SeekOrigin.Begin);
            }

            var signature = br.ReadUInt16();
            if (roomDisk == 1 && signature != 0x0A31)
                throw new NotSupportedException(string.Format("Invalid signature '{0:X}' in disk 1", signature));
            var signatureExpected = index.Game.Platform == Platform.Apple2GS ? 0x0032 : 0x0132;
            if (roomDisk == 2 && signature != signatureExpected)
                throw new NotSupportedException(string.Format("Invalid signature '{0:X}' in disk 2", signature));

            int numResources;
            if (index.Game.GameId == GameId.Maniac)
            {
                if (index.Game.Features.HasFlag(GameFeatures.Demo))
                {
                    numResources = maniacDemoResourcesPerFile[resourceNumber];
                }
                else
                {
                    numResources = maniacResourcesPerFile[resourceNumber];
                }
            }
            else
            {
                numResources  = zakResourcesPerFile[resourceNumber];
            }

            // read resources
            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);
            var resourceOffset = index.GetResourceOffset(resourceNumber);
            br.BaseStream.Seek(resourceOffset, SeekOrigin.Begin);
            for (var i = 0; i < numResources; i++)
            {
                var size = br.ReadUInt16();
                bw.Write(size);
                if (size > 0)
                {
                    bw.Write(br.ReadBytes(size - 2));
                }
            }
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        static readonly int[] maniacResourcesPerFile =
            {
                0, 11,  1,  3,  9, 12,  1, 13, 10,  6,
                4,  1,  7,  1,  1,  2,  7,  8, 19,  9,
                6,  9,  2,  6,  8,  4, 16,  8,  3,  3,
                12, 12,  2,  8,  1,  1,  2,  1,  9,  1,
                3,  7,  3,  3, 13,  5,  4,  3,  1,  1,
                3, 10,  1,  0,  0
            };

        static readonly int[] maniacDemoResourcesPerFile = 
            {
                 0, 12,  0,  2,  1, 12,  1, 13,  6,  0,
                 31, 0,  1,  0,  0,  0,  0,  1,  1,  1,
                 0,  1,  0,  0,  2,  0,  0,  1,  0,  0,
                 2,  7,  1, 11,  0,  0,  5,  1,  0,  0,
                 1,  0,  1,  3,  4,  3,  1,  0,  0,  1,
                 2,  2,  0,  0,  0
            };

        static readonly int[] zakResourcesPerFile =
            {
            0, 29, 12, 14, 13,  4,  4, 10,  7,  4,
            14, 19,  5,  4,  7,  6, 11,  9,  4,  4,
            1,  3,  3,  5,  1,  9,  4, 10, 13,  6,
            7, 10,  2,  6,  1, 11,  2,  5,  7,  1,
            7,  1,  4,  2,  8,  6,  6,  6,  4, 13,
            3,  1,  2,  1,  2,  1, 10,  1,  1
        };
    }

}
