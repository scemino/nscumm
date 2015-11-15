//
//  ResourceManager5.cs
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

using NScumm.Core;

namespace NScumm.Scumm.IO
{
    class ResourceManager5: ResourceManager4
    {
        public ResourceManager5(GameInfo game)
            : base(game)
        {           
        }

        protected override ResourceFile OpenRoom(byte roomIndex)
        {
            var diskNum = Index.RoomResources[roomIndex].RoomNum;
            var diskName = string.Format(Game.Pattern, diskNum);
            var game1Path = ScummHelper.NormalizePath(ServiceLocator.FileStorage.Combine(Directory, diskName));

            var file = new ResourceFile5(new XorStream(ServiceLocator.FileStorage.OpenFileRead(game1Path), 0x69));
            return file;
        }

        protected override byte[] ReadCharset(byte id)
        {
            var res = ((ResourceIndex5)Index).CharsetResources[id];
            var diskNum = res.RoomNum;
            var file = (ResourceFile5)OpenRoom(diskNum);
            var rOffset = file.GetRoomOffset(diskNum);
            return file.ReadCharset(rOffset + res.Offset);
        }
    }
}

