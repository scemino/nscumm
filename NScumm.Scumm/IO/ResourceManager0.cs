//
//  ResourceManager0.cs
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

using NScumm.Core;

namespace NScumm.Scumm.IO
{
    class ResourceManager0: ResourceManager
    {
        public ResourceManager0(GameInfo game)
            : base(game)
        {
        }

        protected override ResourceFile OpenRoom(byte roomIndex)
        {
            var stream = ScummDiskImage.CreateResource((ResourceIndex0)Index, roomIndex);
            var file = new ResourceFile0(stream);
            return file;
        }

        protected override byte[] ReadCharset(byte id)
        {
            // Stub, V0 font resources are hardcoded into the engine.
            return null;
        }
    }
}

