//
//  Resource.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core;
using System.IO;
using NScumm.Core.IO;
using System.Collections.Generic;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
	[Flags]
	public enum GameFeatures
	{
		/// <summary>
		/// Demo.
		/// </summary>
		DEMO = 1 << 0,
		/// <summary>
		/// Equivalent to cdrom version check.
		/// </summary>
		TALKIE = 1 << 1,
		/// <summary>
		/// Floppy, ie. non-talkie version.
		/// </summary>
		FLOPPY = 1 << 2,
		/// <summary>
		/// Interview demo.
		/// </summary>
		INTERVIEW = 1 << 3,
		/// <summary>
		/// Version rebuilt with the 'compression_queen' tool.
		/// </summary>
		REBUILT = 1 << 4
	}

	public class DetectedGameVersion
	{
		public Platform platform;
		public Language language;
		public GameFeatures features;
		public byte compression;
		public string str;
		public byte queenTblVersion;
		public uint queenTblOffset;
	}

	class RetailGameVersion
	{
		public string str;
		public byte queenTblVersion;
		public uint queenTblOffset;
		public uint dataFileSize;

		public RetailGameVersion (string str, byte queenTblVersion, uint queenTblOffset, uint dataFileSize)
		{
			this.str = str;	
			this.queenTblVersion = queenTblVersion;	
			this.queenTblOffset = queenTblOffset;	
			this.dataFileSize = dataFileSize;	
		}
	}

	class ResourceEntry
	{
		public string filename;
		public byte bundle;
		public uint offset;
		public uint size;

		public ResourceEntry (string filename,byte bundle=0,uint offset=0,uint size=0)
		{
			this.filename = filename;
			this.bundle = bundle;
			this.offset = offset;
			this.size = size;
		}
	}

	class ResourceEntryComparer : Comparer<ResourceEntry>
	{
		public override int Compare (ResourceEntry x, ResourceEntry y)
		{
			return string.Compare(x.filename, y.filename, StringComparison.OrdinalIgnoreCase);
		}
	}

	public partial class Resource: IDisposable
	{
		const int VER_ENG_FLOPPY = 0;
		const int VER_ENG_TALKIE = 1;
		const int VER_FRE_FLOPPY = 2;
		const int VER_FRE_TALKIE = 3;
		const int VER_GER_FLOPPY = 4;
		const int VER_GER_TALKIE = 5;
		const int VER_ITA_FLOPPY = 6;
		const int VER_ITA_TALKIE = 7;
		const int VER_SPA_TALKIE = 8;
		const int VER_HEB_TALKIE = 9;
		const int VER_DEMO_PCGAMES = 10;
		const int VER_DEMO = 11;
		const int VER_INTERVIEW = 12;
		const int VER_AMI_ENG_FLOPPY = 13;
		const int VER_AMI_DEMO = 14;
		const int VER_AMI_INTERVIEW = 15;

		const int JAS_VERSION_OFFSET_DEMO = 0x119A8;
		const int JAS_VERSION_OFFSET_INTV	= 0xCF8;
		const int JAS_VERSION_OFFSET_PC	= 0x12484;

		const string TableFilename = "queen.tbl";

		public Language Language  { get {return _version.language; }}
		public Platform Platform  { get {return _version.platform; }}

		public bool IsDemo { get {return _version.features.HasFlag(GameFeatures.DEMO); }}
		public bool IsInterview { get {return _version.features.HasFlag(GameFeatures.INTERVIEW); }}
		public bool IsFloppy { get {return _version.features.HasFlag(GameFeatures.FLOPPY); }}
		public bool IsCD { get {return _version.features.HasFlag(GameFeatures.TALKIE); }}

        public byte Compression { get { return _version.compression;} }

        BinaryReader _resourceFile;
		int _currentResourceFileNum;
		DetectedGameVersion _version;
		ushort _resourceEntries;
		ResourceEntry[] _resourceTable;
		string _path;
		ResourceEntryComparer _resourceEntryComparer;

		public string JASVersion { get{ return _version.str; }}

		private static readonly RetailGameVersion[] _gameVersions = new [] {
			new RetailGameVersion ("PEM10", 1, 0x00000008, 22677657),
			new RetailGameVersion ("CEM10", 1, 0x0000584E, 190787021),
			new RetailGameVersion ("PFM10", 1, 0x0002CD93, 22157304),
			new RetailGameVersion ("CFM10", 1, 0x00032585, 186689095),
			new RetailGameVersion ("PGM10", 1, 0x00059ACA, 22240013),
			new RetailGameVersion ("CGM10", 1, 0x0005F2A7, 217648975),
			new RetailGameVersion ("PIM10", 1, 0x000866B1, 22461366),
			new RetailGameVersion ("CIM10", 1, 0x0008BEE2, 190795582),
			new RetailGameVersion ("CSM10", 1, 0x000B343C, 190730602),
			new RetailGameVersion ("CHM10", 1, 0x000DA981, 190705558),
			new RetailGameVersion ("PE100", 1, 0x00101EC6, 3724538),
			new RetailGameVersion ("PE100", 1, 0x00102B7F, 3732177),
			new RetailGameVersion ("PEint", 1, 0x00103838, 1915913),
			new RetailGameVersion ("aEM10", 2, 0x00103F1E, 351775),
			new RetailGameVersion ("CE101", 2, 0x00107D8D, 563335),
			new RetailGameVersion ("PE100", 2, 0x001086D4, 597032)
		};


		public Resource (string path)
		{
			_path = path;
			_resourceEntryComparer = new ResourceEntryComparer ();
			_currentResourceFileNum = 1;
			_resourceFile = new BinaryReader (ServiceLocator.FileStorage.OpenFileRead (path));
			_version = DetectVersion (_resourceFile.BaseStream);

			if (_version.features.HasFlag (GameFeatures.REBUILT)) {
				ReadTableEntries (_resourceFile);
			} else {
				ReadTableFile (_version.queenTblVersion, _version.queenTblOffset);
			}
			CheckJASVersion ();
			D.Debug (5, $"Detected game version: {_version.str}, which has {_resourceEntries} resource entries");
		}

		public void Dispose ()
		{
			_resourceFile.Dispose ();
		}

		public byte[] LoadFile(string filename, uint skipBytes = 0)
		{
			uint size;
			return LoadFile (filename, skipBytes, out size);
		}

		public byte[] LoadFile(string filename, uint skipBytes, out uint size)
		{
            D.Debug(7, $"Resource::loadFile('{filename}')");
			var re = ResourceEntry(filename);
			var sz = re.size - skipBytes;
			size = sz;
			SeekResourceFile(re.bundle, re.offset + skipBytes);
			var dstBuf = _resourceFile.ReadBytes ((int)sz);
			return dstBuf;
		}

		public List<string> LoadTextFile(string filename) 
		{
			var stringList=new List<string>();
            D.Debug(7, $"Resource::loadTextFile('{filename}')");
			ResourceEntry re = ResourceEntry(filename);
			SeekResourceFile(re.bundle, re.offset);
			var stream = new StreamReader (new SeekableSubReadStream (_resourceFile.BaseStream, re.offset, re.offset + re.size));

			while (true) 
			{
				string tmp = stream.ReadLine ();
				if (tmp==null)
					break;
				stringList.Add (tmp);
			}

			return stringList;
		}

		public bool FileExists(string filename) { return ResourceEntry(filename) != null; }

		public Stream FindSound(string filename, out uint size) 
		{
			//assert(strstr(filename, ".SB") != NULL || strstr(filename, ".AMR") != NULL || strstr(filename, ".INS") != NULL);
			var re = ResourceEntry(filename);
			if (re!=null) {
				size = re.size;
				SeekResourceFile(re.bundle, re.offset);
				return _resourceFile.BaseStream;
			}
			size = 0;
			return null;
		}

		private void CheckJASVersion() {
			if (_version.platform == Platform.Amiga) {
				// don't bother verifying the JAS version string with these versions,
				// it will be done at the end of Logic::readQueenJas, anyway
				return;
			}
			ResourceEntry re = ResourceEntry("QUEEN.JAS");
			uint offset = re.offset;
			if (IsDemo)
				offset += JAS_VERSION_OFFSET_DEMO;
			else if (IsInterview)
				offset += JAS_VERSION_OFFSET_INTV;
			else
				offset += JAS_VERSION_OFFSET_PC;
			SeekResourceFile(re.bundle, offset);

			string versionStr = _resourceFile.ReadBytes(6).ToText(0,6);
			if (_version.str != versionStr)
				throw new NotSupportedException($"Verifying game version failed! (expected: '{_version.str}', found: '{versionStr}')");
		}

		private void SeekResourceFile(int num, uint offset) {
			if (_currentResourceFileNum != num) {
				D.Debug(7, $"Opening resource file {num}, current {_currentResourceFileNum}");
				_resourceFile.Dispose();
				string name=$"queen.{num}";
				var filename = ScummHelper.LocatePath (ServiceLocator.FileStorage.GetDirectoryName (_path), name);
				_resourceFile = new BinaryReader(ServiceLocator.FileStorage.OpenFileRead (filename));
				_currentResourceFileNum = num;
			}
			_resourceFile.BaseStream.Seek(offset, SeekOrigin.Begin);
		}

		private ResourceEntry ResourceEntry(string filename)
		{
			var index = Array.BinarySearch (_resourceTable, new ResourceEntry(filename), _resourceEntryComparer);
			return index < 0 ? null : _resourceTable [index];
		}

		private void ReadTableEntries (BinaryReader file)
		{
			_resourceEntries = file.ReadUInt16BigEndian ();
			_resourceTable = new ResourceEntry[_resourceEntries];
			for (var i = 0; i < _resourceEntries; ++i) {
				_resourceTable [i] 
					= new ResourceEntry (
						filename: file.ReadBytes (12).GetText (0, 12),
						bundle: file.ReadByte (),
						offset: file.ReadUInt32BigEndian (),
						size: file.ReadUInt32BigEndian ());
			}
		}

		private void ReadTableFile(byte version, uint offset)
		{
			var tableFilename = ScummHelper.LocatePath(ServiceLocator.FileStorage.GetDirectoryName(_path),TableFilename);
			using(var tableFile = new BinaryReader(ServiceLocator.FileStorage.OpenFileRead(tableFilename)))
			{
				if (tableFile.ReadTag() == "QTBL") {
					uint tableVersion = tableFile.ReadUInt32BigEndian();
					if (version > tableVersion) {
						throw new NotSupportedException($"The game you are trying to play requires version {version} of queen.tbl, "+
							$"you have version {tableVersion} ; please update it");
					}
						tableFile.BaseStream.Seek(offset, SeekOrigin.Current);
					ReadTableEntries(tableFile);
				} else {
					// check if it is the english floppy version, for which we have a hardcoded version of the table
					if (_version.str==_gameVersions[VER_ENG_FLOPPY].str) {
						_resourceEntries = 1076;
						_resourceTable = _resourceTablePEM10;
					} else {
							throw new NotSupportedException($"Could not find tablefile '{TableFilename}'");
					}
				}
			}
		}

		public static DetectedGameVersion DetectVersion (Stream file)
		{
			var ver = new DetectedGameVersion ();
			var reader = new BinaryReader (file);
			if (reader.ReadTag () == "QTBL") {
				ver.str = reader.ReadBytes (6).ToText (0, 6);
				file.Seek (2, SeekOrigin.Current);
				ver.compression = reader.ReadByte ();
				ver.features = GameFeatures.REBUILT;
				ver.queenTblVersion = 0;
				ver.queenTblOffset = 0;
			} else {
				var gameVersion = DetectGameVersionFromSize (file.Length);
				if (Equals (gameVersion, default(RetailGameVersion))) {
					// TODO: warning("Unknown/unsupported FOTAQ version");
					return null;
				}
				ver.str = gameVersion.str;
				ver.compression = Defines.COMPRESSION_NONE;
				ver.features = 0;
				ver.queenTblVersion = gameVersion.queenTblVersion;
				ver.queenTblOffset = gameVersion.queenTblOffset;

				// Handle game versions for which versionStr information is irrevelant
				if (Equals (gameVersion, _gameVersions [VER_AMI_DEMO])) { // CE101
					ver.language = Language.EN_ANY;
					ver.features |= GameFeatures.FLOPPY | GameFeatures.DEMO;
					ver.platform = Platform.Amiga;
					return ver;
				}
				if (Equals (gameVersion, _gameVersions [VER_AMI_INTERVIEW])) { // PE100
					ver.language = Language.EN_ANY;
					ver.features |= GameFeatures.FLOPPY | GameFeatures.INTERVIEW;
					ver.platform = Platform.Amiga;
					return ver;
				}
			}

			switch (ver.str [1]) {
			case 'E':
				// TODO: vs
//				if (LanguageHelper.ParseLanguage(ConfMan.get("language")) == Language.RU_RUS) {
//					ver.language = Language.RU_RUS;
//				} else if (LanguageHelper.ParseLanguage(ConfMan.get("language")) == Language.GR_GRE) {
//					ver.language = Language.GR_GRE;
//				} else 
				{
					ver.language = Language.EN_ANY;
				}
				break;
			case 'F':
				ver.language = Language.FR_FRA;
				break;
			case 'G':
				ver.language = Language.DE_DEU;
				break;
			case 'H':
				ver.language = Language.HE_ISR;
				break;
			case 'I':
				ver.language = Language.IT_ITA;
				break;
			case 'S':
				ver.language = Language.ES_ESP;
				break;
			case 'g':
				ver.language = Language.GR_GRE;
				break;
			case 'R':
				ver.language = Language.RU_RUS;
				break;
			default:
				throw new InvalidOperationException ($"Invalid language id '{ver.str[1]}'");
			}

			switch (ver.str [0]) {
			case 'P':
				ver.features |= GameFeatures.FLOPPY;
				ver.platform = Platform.DOS;
				break;
			case 'C':
				ver.features |= GameFeatures.TALKIE;
				ver.platform = Platform.DOS;
				break;
			case 'a':
				ver.features |= GameFeatures.FLOPPY;
				ver.platform = Platform.Amiga;
				break;
			default:
				throw new InvalidOperationException ($"Invalid platform id '{ver.str[0]}'");
			}

			if ((ver.str.Substring (2, 3) == "100") || (ver.str.Substring (2, 3) == "101")) {
				ver.features |= GameFeatures.DEMO;
			} else if (ver.str.Substring (2, 3) == "int") {
				ver.features |= GameFeatures.INTERVIEW;
			}

			return ver;
		}

		private static RetailGameVersion DetectGameVersionFromSize (long size)
		{
			for (int i = 0; i < _gameVersions.Length; ++i) {
				if (_gameVersions [i].dataFileSize == size) {
					return _gameVersions [i];
				}
			}
			return default(RetailGameVersion);
		}

	}
}

