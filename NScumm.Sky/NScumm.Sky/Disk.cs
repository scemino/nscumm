using NScumm.Core;
using NScumm.Core.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace NScumm.Sky
{
    [Flags]
    enum SkyGameType
    {
        PcGamer = 0x01,
        Floppy = 0x02,
        Cd = 0x04,
        Demo = 0x08,
        English = 0x10,
        German = 0x20
    }

    class SkyGameVersion
    {
        public SkyGameType Type { get; private set; }
        public Version Version { get; private set; }

        public SkyGameVersion(SkyGameType type, Version version)
        {
            Type = type;
            Version = version;
        }
    }

    class DiskEntry
    {
        public int Offset;
        public bool HasHeader;
        public bool IsCompressed;
        public int Size;
    }

    [StructLayout(LayoutKind.Explicit)]
    class DataFileHeader
    {
        [FieldOffset(0)]
        public ushort flag; // bit 0: set for color data, clear for not
                            // bit 1: set for compressed, clear for uncompressed
                            // bit 2: set for 32 colors, clear for 16 colors
        [FieldOffset(2)]
        public ushort s_x;
        [FieldOffset(4)]
        public ushort s_y;
        [FieldOffset(6)]
        public ushort s_width;
        [FieldOffset(8)]
        public ushort s_height;
        [FieldOffset(10)]
        public ushort s_sp_size;
        [FieldOffset(12)]
        public ushort s_tot_size;
        [FieldOffset(14)]
        public ushort s_n_sprites;
        [FieldOffset(16)]
        public short s_offset_x;
        [FieldOffset(18)]
        public short s_offset_y;
        [FieldOffset(20)]
        public ushort s_compressed_size;
    }

    class Disk : IDisposable
    {
        const string dataFilename = "sky.dsk";
        const string dinnerFilename = "sky.dnr";

        private Stream _dataDiskFile;
        private int _dinnerTableEntries;
        private Dictionary<int, DiskEntry> _entries;
        private RncDecoder _rncDecoder;
        private IFileStorage _fileStorage;
        private IPlatform _platform;

        public Disk(string directory)
        {
            _rncDecoder = new RncDecoder();

            _fileStorage = ServiceLocator.FileStorage;
            _platform = ServiceLocator.Platform;
            var dataPath = _fileStorage.Combine(directory, dataFilename);
            _dataDiskFile = _fileStorage.OpenFileRead(dataPath);

            ReadEntries(directory);
        }

        public SkyGameVersion DetermineGameVersion()
        {
            //determine game version based on number of entries in dinner table
            switch (_dinnerTableEntries)
            {
                case 232:
                    // German floppy demo (v0.0272)
                    return new SkyGameVersion(SkyGameType.German | SkyGameType.Floppy | SkyGameType.Demo, new Version(0, 0272));
                case 243:
                    // pc gamer demo (v0.0109)
                    return new SkyGameVersion(SkyGameType.PcGamer | SkyGameType.Demo, new Version(0, 0109));
                case 247:
                    // English floppy demo (v0.0267)
                    return new SkyGameVersion(SkyGameType.English | SkyGameType.Floppy | SkyGameType.Demo, new Version(0, 0267));
                case 1404:
                    //floppy (v0.0288)
                    return new SkyGameVersion(SkyGameType.Floppy, new Version(0, 0288));
                case 1413:
                    //floppy (v0.0303)
                    return new SkyGameVersion(SkyGameType.Floppy, new Version(0, 0303));
                case 1445:
                    //floppy (v0.0331 or v0.0348)
                    if (_dataDiskFile.Length == 8830435)
                        return new SkyGameVersion(SkyGameType.Floppy, new Version(0, 0348));
                    else
                        return new SkyGameVersion(SkyGameType.Floppy, new Version(0, 0331));
                case 1711:
                    //cd demo (v0.0365)
                    return new SkyGameVersion(SkyGameType.Cd | SkyGameType.Demo, new Version(0, 0365));
                case 5099:
                    //cd (v0.0368)
                    return new SkyGameVersion(SkyGameType.Cd | SkyGameType.Demo, new Version(0, 0368));
                case 5097:
                    //cd (v0.0372)
                    return new SkyGameVersion(SkyGameType.Cd | SkyGameType.Demo, new Version(0, 0372));
                default:
                    //unknown version
                    throw new NotSupportedException($"Unknown game version! {_dinnerTableEntries} dinner table entries");
            }
        }

        public byte[] LoadFile(int id)
        {
            // goto entry offset
            var entry = _entries[id];
            _dataDiskFile.Position = entry.Offset;
            // read data
            var br = new BinaryReader(_dataDiskFile);
            var data = new byte[entry.Size + 4];
            br.Read(data, 0, entry.Size);

            // check header if compressed or not
            var header = _platform.ToStructure<DataFileHeader>(data, 0);
            var isCompressed = entry.IsCompressed && ((header.flag >> 7) & 1) == 1;
            if (!isCompressed)
                return data;

            // data is compressed
            var decompSize = (header.flag & ~0xFF) << 8;
            decompSize |= header.s_tot_size;
            var uncompDest = new byte[decompSize];
            int unpackLen;
            var sizeOfDataFileHeader = 22;
            if (entry.HasHeader)
            {
                unpackLen = _rncDecoder.UnpackM1(data, sizeOfDataFileHeader, uncompDest, 0, 0);
            }
            else
            {
                Array.Copy(data, uncompDest, sizeOfDataFileHeader);
                unpackLen = _rncDecoder.UnpackM1(data, sizeOfDataFileHeader, uncompDest, sizeOfDataFileHeader, 0);
            }
            return uncompDest;
        }

        public void Dispose()
        {
            _dataDiskFile.Dispose();
        }

        private void ReadEntries(string directory)
        {
            var dinnerPath = _fileStorage.Combine(directory, dinnerFilename);
            using (var dinnerFile = _fileStorage.OpenFileRead(dinnerPath))
            {
                var dinnerReader = new BinaryReader(dinnerFile);
                _dinnerTableEntries = dinnerReader.ReadInt32();
                _entries = new Dictionary<int, DiskEntry>();
                for (int i = 0; i < _dinnerTableEntries; i++)
                {
                    var id = (int)dinnerReader.ReadUInt16();
                    var tmp = dinnerReader.ReadUInt32();
                    var tmp2 = (uint)dinnerReader.ReadUInt16();
                    var offset = (int)(tmp & 0X0FFFFFF);
                    var cflag = (byte)((offset >> 23) & 0x1) == 1;
                    offset &= 0x7FFFFF;
                    if (cflag)
                    {
                        var version = DetermineGameVersion();
                        if (version.Version.Minor == 331)
                            offset <<= 3;
                        else
                            offset <<= 4;
                    }
                    var flags = (tmp2 << 8) | (tmp & 0xFF000000) >> 24;
                    var hasHeader = ((flags >> 22) & 0x1) == 1;
                    var isCompressed = ((flags >> 23) & 0x1) == 0;
                    var size = (int)(flags & 0x03fffff);
                    _entries.Add(id, new DiskEntry { Offset = offset, HasHeader = hasHeader, IsCompressed = isCompressed, Size = size });
                }
            }
        }
    }
}
