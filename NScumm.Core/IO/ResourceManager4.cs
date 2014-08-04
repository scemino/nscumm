//
//  ResourceManager4.cs
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
using System;
using System.IO;

namespace NScumm.Core.IO
{

	class ResourceManager4: ResourceManager
	{
		public ResourceManager4 (string path)
			: base (path)
		{			
		}

		protected override ResourceFile OpenRoom (byte roomIndex)
		{
			var diskNum = Index.RoomResources [roomIndex].RoomNum;
			var diskName = string.Format ("disk{0:00}.lec", diskNum);
			var game1Path = Path.Combine (Directory, diskName);

			var file = diskNum != 0 ? (ResourceFile)new ResourceFile4 (game1Path, 0x69) : null;
			return file;
		}

		protected override byte[] ReadCharset (byte id)
		{
			var diskName = string.Format ("{0}.lfl", 900 + id);
			var path = ScummHelper.NormalizePath (Path.Combine (Directory, diskName));
			using (var file = File.OpenRead (path)) {
				var reader = new BinaryReader (file);
				var size = reader.ReadUInt32 () + 11;
				return reader.ReadBytes ((int)size);
			}
		}
	}
	
}
