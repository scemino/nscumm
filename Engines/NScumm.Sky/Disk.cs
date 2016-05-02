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
using System.Collections.Generic;
using System.IO;
using NScumm.Core;

namespace NScumm.Sky
{
	[Flags]
	internal enum SkyGameType
	{
		PcGamer = 0x01,
		Floppy = 0x02,
		Cd = 0x04,
		Demo = 0x08,
		English = 0x10,
		German = 0x20
	}

	internal class SkyGameVersion
	{
		public SkyGameVersion (SkyGameType type, Version version)
		{
			Type = type;
			Version = version;
		}

		public SkyGameType Type { get; private set; }

		public Version Version { get; }
	}

	internal class DiskEntry
	{
		public bool HasHeader;
		public bool IsCompressed;
		public int Offset;
		public int Size;
	}

	internal class DataFileHeader
	{
		public const int Size = 22;

		public ushort flag {
			get { return Data.ToUInt16 (0); }
			set { Data.WriteUInt16 (0, value); }
		}

		// bit 0: set for color data, clear for not
		// bit 1: set for compressed, clear for uncompressed
		// bit 2: set for 32 colors, clear for 16 colors

		public ushort s_x {
			get { return Data.ToUInt16 (2); }
			set { Data.WriteUInt16 (2, value); }
		}

		public ushort s_y {
			get { return Data.ToUInt16 (4); }
			set { Data.WriteUInt16 (4, value); }
		}

		public ushort s_width {
			get { return Data.ToUInt16 (6); }
			set { Data.WriteUInt16 (6, value); }
		}

		public ushort s_height {
			get { return Data.ToUInt16 (8); }
			set { Data.WriteUInt16 (8, value); }
		}

		public ushort s_sp_size {
			get { return Data.ToUInt16 (10); }
			set { Data.WriteUInt16 (10, value); }
		}

		public ushort s_tot_size {
			get { return Data.ToUInt16 (12); }
			set { Data.WriteUInt16 (12, value); }
		}

		public ushort s_n_sprites {
			get { return Data.ToUInt16 (14); }
			set { Data.WriteUInt16 (14, value); }
		}

		public short s_offset_x {
			get { return Data.ToInt16 (16); }
			set { Data.WriteInt16 (16, value); }
		}

		public short s_offset_y {
			get { return Data.ToInt16 (18); }
			set { Data.WriteInt16 (18, value); }
		}

		public ushort s_compressed_size {
			get { return Data.ToUInt16 (20); }
			set { Data.WriteUInt16 (20, value); }
		}

		public byte[] Data { get; }

		public DataFileHeader (byte[] data)
		{
			Data = data;
		}
	}

	internal class Disk : IDisposable
	{
		private const string DataFilename = "sky.dsk";
		private const string DinnerFilename = "sky.dnr";
		private const int MaxFilesInList = 60;

		private readonly ushort[] _buildList = new ushort[MaxFilesInList];
		private readonly Stream _dataDiskFile;
		private readonly IFileStorage _fileStorage;
		private readonly uint[] _loadedFilesList = new uint[MaxFilesInList];
		private readonly IPlatform _platform;
		private readonly RncDecoder _rncDecoder;

		private Dictionary<int, DiskEntry> _entries;
		private int _dinnerTableEntries;

		public Disk (string directory)
		{
			_rncDecoder = new RncDecoder ();

			_fileStorage = ServiceLocator.FileStorage;
			_platform = ServiceLocator.Platform;
			var dataPath = _fileStorage.Combine (directory, DataFilename);
			_dataDiskFile = _fileStorage.OpenFileRead (dataPath);

			ReadEntries (directory);
		}

		public void Dispose ()
		{
			_dataDiskFile.Dispose ();
		}

		public SkyGameVersion DetermineGameVersion ()
		{
			//determine game version based on number of entries in dinner table
			switch (_dinnerTableEntries) {
			case 232:
                    // German floppy demo (v0.0272)
				return new SkyGameVersion (SkyGameType.German | SkyGameType.Floppy | SkyGameType.Demo,
					new Version (0, 0272));
			case 243:
                    // pc gamer demo (v0.0109)
				return new SkyGameVersion (SkyGameType.PcGamer | SkyGameType.Demo, new Version (0, 0109));
			case 247:
                    // English floppy demo (v0.0267)
				return new SkyGameVersion (SkyGameType.English | SkyGameType.Floppy | SkyGameType.Demo,
					new Version (0, 0267));
			case 1404:
                    //floppy (v0.0288)
				return new SkyGameVersion (SkyGameType.Floppy, new Version (0, 0288));
			case 1413:
                    //floppy (v0.0303)
				return new SkyGameVersion (SkyGameType.Floppy, new Version (0, 0303));
			case 1445:
                    //floppy (v0.0331 or v0.0348)
				if (_dataDiskFile.Length == 8830435)
					return new SkyGameVersion (SkyGameType.Floppy, new Version (0, 0348));
				return new SkyGameVersion (SkyGameType.Floppy, new Version (0, 0331));
			case 1711:
                    //cd demo (v0.0365)
				return new SkyGameVersion (SkyGameType.Cd | SkyGameType.Demo, new Version (0, 0365));
			case 5099:
                    //cd (v0.0368)
				return new SkyGameVersion (SkyGameType.Cd | SkyGameType.Demo, new Version (0, 0368));
			case 5097:
                    //cd (v0.0372)
				return new SkyGameVersion (SkyGameType.Cd | SkyGameType.Demo, new Version (0, 0372));
			default:
                    //unknown version
				throw new NotSupportedException ($"Unknown game version! {_dinnerTableEntries} dinner table entries");
			}
		}

		public uint[] LoadedFilesList {
			get { return _loadedFilesList; }
		}

		public byte[] LoadFile (int id)
		{
			// goto entry offset
			var entry = _entries [id];
			_dataDiskFile.Position = entry.Offset;
			// read data
			var br = new BinaryReader (_dataDiskFile);
			var data = new byte[entry.Size + 4];
			var read = br.Read (data, 0, entry.Size);
			if (read != entry.Size) {
				throw new InvalidOperationException (
					string.Format ("Unable to read {0} bytes from datadisk ({1} bytes read)", entry.Size, read));
			}

			// check header if compressed or not
			var header = new DataFileHeader(data);
			var isCompressed = entry.IsCompressed && ((header.flag >> 7) & 1) == 1;
			if (!isCompressed)
				return data;

			// data is compressed
			var decompSize = (header.flag & ~0xFF) << 8;
			decompSize |= header.s_tot_size;
			var uncompDest = new byte[decompSize];
			int unpackLen;
			var sizeOfDataFileHeader = 22;
			if (entry.HasHeader) {
				unpackLen = _rncDecoder.UnpackM1 (data, sizeOfDataFileHeader, uncompDest, 0);
			} else {
				Array.Copy (data, uncompDest, sizeOfDataFileHeader);
				unpackLen = _rncDecoder.UnpackM1 (data, sizeOfDataFileHeader, uncompDest, sizeOfDataFileHeader);
			}
			return uncompDest;
		}

		public byte[] LoadScriptFile (ushort fileNr)
		{
			// TODO: SCUMM_BIG_ENDIAN
			return LoadFile (fileNr);
		}

		public void RefreshFilesList (uint[] list)
		{
			byte cnt = 0;
			while (_loadedFilesList [cnt] != 0) {
				SkyEngine.ItemList [_loadedFilesList [cnt] & 2047] = null;
				cnt++;
			}
			cnt = 0;
			while (list [cnt] != 0) {
				_loadedFilesList [cnt] = list [cnt];
				SkyEngine.ItemList [_loadedFilesList [cnt] & 2047] = LoadFile ((ushort)(_loadedFilesList [cnt] & 0x7FFF));
				cnt++;
			}
			_loadedFilesList [cnt] = 0;
		}

		public void FnCacheChip (byte[] data)
		{
			// fnCacheChip is called after fnCacheFast
			ushort cnt = 0;
			while (_buildList [cnt] != 0)
				cnt++;
			ushort fCnt = 0;
			do {
				_buildList [cnt + fCnt] = (ushort)(data.ToUInt16 (fCnt * 2) & 0x7FFFU);
				fCnt++;
			} while (data.ToUInt16 ((fCnt - 1) * 2) != 0);
			FnCacheFiles ();
		}

		public void FnCacheFast (byte[] data)
		{
			if (data != null) {
				byte cnt = 0;
				do {
					_buildList [cnt] = (ushort)(data.ToUInt16 (cnt * 2) & 0x7FFFU);
					cnt++;
				} while (data.ToUInt16 ((cnt - 1) * 2) != 0);
			}
		}

		public void FnMiniLoad (ushort fileNum)
		{
			ushort cnt = 0;
			while (_loadedFilesList [cnt] != 0) {
				if (_loadedFilesList [cnt] == fileNum)
					return;
				cnt++;
			}
			_loadedFilesList [cnt] = fileNum & 0x7FFFU;
			_loadedFilesList [cnt + 1] = 0;
			SkyEngine.ItemList [fileNum & 2047] = LoadFile (fileNum);
		}

		public void FnFlushBuffers ()
		{
			// dump all loaded sprites
			byte lCnt = 0;
			while (_loadedFilesList [lCnt] != 0) {
				SkyEngine.ItemList [_loadedFilesList [lCnt] & 2047] = null;
				lCnt++;
			}
			_loadedFilesList [0] = 0;
		}

		private void FnCacheFiles ()
		{
			ushort lCnt, bCnt, targCnt;
			targCnt = lCnt = 0;
			bool found;
			while (_loadedFilesList [lCnt] != 0) {
				bCnt = 0;
				found = false;
				while (_buildList [bCnt] != 0 && !found) {
					if ((_buildList [bCnt] & 0x7FFFU) == _loadedFilesList [lCnt])
						found = true;
					else
						bCnt++;
				}
				if (found) {
					_loadedFilesList [targCnt] = _loadedFilesList [lCnt];
					targCnt++;
				} else {
					SkyEngine.ItemList [_loadedFilesList [lCnt] & 2047] = null;
				}
				lCnt++;
			}
			_loadedFilesList [targCnt] = 0; // mark end of list
			bCnt = 0;
			while (_buildList [bCnt] != 0) {
				if ((_buildList [bCnt] & 0x7FF) == 0x7FF) {
					// amiga dummy files
					bCnt++;
					continue;
				}
				lCnt = 0;
				found = false;
				while (_loadedFilesList [lCnt] != 0 && !found) {
					if (_loadedFilesList [lCnt] == (_buildList [bCnt] & 0x7FFFU))
						found = true;
					lCnt++;
				}
				if (found) {
					bCnt++;
					continue;
				}
				// ok, we really have to load the file.
				_loadedFilesList [targCnt] = _buildList [bCnt] & 0x7FFFU;
				targCnt++;
				_loadedFilesList [targCnt] = 0;
				SkyEngine.ItemList [_buildList [bCnt] & 2047] = LoadFile (_buildList [bCnt] & 0x7FFF);
				if (SkyEngine.ItemList [_buildList [bCnt] & 2047] == null) {
					// TODO: warning("fnCacheFiles: Disk::loadFile() returned null for file {0}", _buildList[bCnt] & 0x7FFF);
				}
				bCnt++;
			}
			_buildList [0] = 0;
		}

		private void ReadEntries (string directory)
		{
			var dinnerPath = _fileStorage.Combine (directory, DinnerFilename);
			using (var dinnerFile = _fileStorage.OpenFileRead (dinnerPath)) {
				var dinnerReader = new BinaryReader (dinnerFile);
				_dinnerTableEntries = dinnerReader.ReadInt32 ();
				_entries = new Dictionary<int, DiskEntry> ();
				for (var i = 0; i < _dinnerTableEntries; i++) {
					var id = (int)dinnerReader.ReadUInt16 ();
					var tmp = dinnerReader.ReadUInt32 ();
					var tmp2 = (uint)dinnerReader.ReadUInt16 ();
					var offset = (int)(tmp & 0X0FFFFFF);
					var cflag = (byte)((offset >> 23) & 0x1) == 1;
					offset &= 0x7FFFFF;
					if (cflag) {
						var version = DetermineGameVersion ();
						if (version.Version.Minor == 331)
							offset <<= 3;
						else
							offset <<= 4;
					}
					var flags = (tmp2 << 8) | (tmp & 0xFF000000) >> 24;
					var hasHeader = ((flags >> 22) & 0x1) == 1;
					var isCompressed = ((flags >> 23) & 0x1) == 0;
					var size = (int)(flags & 0x03fffff);
					_entries.Add (id,
						new DiskEntry {
							Offset = offset,
							HasHeader = hasHeader,
							IsCompressed = isCompressed,
							Size = size
						});
				}
			}
		}
	}
}