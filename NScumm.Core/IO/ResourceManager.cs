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
using System.Collections.Generic;

namespace NScumm.Core.IO
{
	public class Script
	{
		public int Id {
			get;
			private set;
		}

		public byte[] Data {
			get;
			private set;
		}

		public Script (int id, byte[] data)
		{
			this.Id = id;
			this.Data = data;
		}
	}

	public class ResourceManager
	{
		readonly ResourceIndex index;

		public byte[] ObjectOwnerTable { get { return index.ObjectOwnerTable; } }

		public byte[] ObjectStateTable { get { return index.ObjectStateTable; } }

		public uint[] ClassData { get { return index.ClassData; } }

		public string Directory { get; private set; }

		public IEnumerable<Room> Rooms {
			get {
				foreach (var rIndex in index.RoomNames) {
					yield return GetRoom (rIndex.Key);
				}
			}
		}

		public IEnumerable<Script> Scripts {
			get {
				for (byte i = 0; i < index.ScriptResources.Count; i++) {
					if (index.ScriptResources [i].RoomNum != 0) {
						yield return new Script (i, GetScript (i));
					}
				}
			}
		}

		public IEnumerable<byte[]> Sounds {
			get {
				for (byte i = 0; i < index.SoundResources.Count; i++) {
					if (index.SoundResources [i].RoomNum != 0) {
						yield return GetSound(i);
					}
				}
			}
		}

		ResourceManager (string path)
		{
			index = ResourceIndex.Load (path);
			Directory = Path.GetDirectoryName (path);
		}

		public static ResourceManager Load (string path)
		{
			return new ResourceManager (path); 
		}

		public Room GetRoom (byte roomNum)
		{
			Room room = null;
			var disk = OpenRoom (roomNum);
			if (disk != null) {
				var rOffsets = disk.ReadRoomOffsets ();
				room = disk.ReadRoom (rOffsets [roomNum]);
				room.Name = index.RoomNames [roomNum];
			}

			return room;
		}

		public XorReader GetCostumeReader (byte scriptNum)
		{
			XorReader reader = null;
			var res = index.CostumeResources [scriptNum];
			var disk = OpenRoom (res.RoomNum);
			if (disk != null) {
				var rOffsets = disk.ReadRoomOffsets ();
				var offset = res.Offset;
				reader = disk.ReadCostume (rOffsets [res.RoomNum] + offset);
			}
			return reader;
		}

		public byte[] GetCharsetData (byte id)
		{
			byte[] charset = null;
			var disk = OpenCharset (id);
			if (disk != null) {
				charset = disk.ReadCharsetData ();
			}
			return charset;
		}

		public byte[] GetScript (byte scriptNum)
		{
			byte[] data = null;
			var resource = index.ScriptResources [scriptNum];
			var disk = OpenRoom (resource.RoomNum);
			if (disk != null) {
				var rOffsets = disk.ReadRoomOffsets ();
				data = disk.ReadScript (rOffsets [resource.RoomNum] + resource.Offset);
			}
			return data;
		}

		public byte[] GetSound(int sound)
		{
			byte[] data = null;
			var resource = index.SoundResources [sound];
			var disk = OpenRoom(resource.RoomNum);
			if (disk != null)
			{
				var rOffsets = disk.ReadRoomOffsets();
				data = disk.ReadSound(rOffsets[resource.RoomNum] + resource.Offset);
			}
			return data;
		}

		ResourceFile OpenRoom (byte roomIndex)
		{
			var diskNum = index.RoomResources [roomIndex].RoomNum;
			var diskName = string.Format ("disk{0:00}.lec", diskNum);
			var game1Path = Path.Combine (Directory, diskName);

			var file = diskNum != 0 ? new ResourceFile (game1Path, 0x69) : null;
			return file;
		}

		ResourceFile OpenCharset (byte id)
		{
			var diskName = string.Format ("{0}.lfl", 900 + id);
			var game1Path = Path.Combine (Directory, diskName);
			var file = new ResourceFile (game1Path, 0x0);
			return file;
		}
	}
}

