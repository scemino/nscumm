//
//  ResourceManager3.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.


using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Scumm.IO
{
    class ResourceManager2: ResourceManager
    {
        public ResourceManager2(GameInfo game)
            : base(game)
        {
        }

        protected override ResourceFile OpenRoom(byte roomIndex)
        {
            var diskName = string.Format("{0:00}.lfl", roomIndex);
            var game1Path = ServiceLocator.FileStorage.Combine(Directory, diskName);

            if (Game.Version == 1)
            {
                if (Game.Platform == Platform.C64)
                {
                    var res = ScummDiskImage.CreateResource((ResourceIndex0)Index, roomIndex);
                    return new ResourceFile1(res);
                }
                return new ResourceFile1(new XorStream(ServiceLocator.FileStorage.OpenFileRead(game1Path), 0xFF));
            }
            return new ResourceFile2(new XorStream(ServiceLocator.FileStorage.OpenFileRead(game1Path), 0xFF));
        }

        protected override byte[] ReadCharset(byte id)
        {
            // Stub, V2 font resources are hardcoded into the engine.
            return null;
        }
    }
	
}
