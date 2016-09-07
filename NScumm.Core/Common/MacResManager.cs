//
//  MacResManager.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static NScumm.Core.DebugHelper;

namespace NScumm.Core
{
    /// <summary>
    /// Class for handling Mac data and resource forks.
    /// It can read from raw, MacBinary, and AppleDouble formats.
    /// </summary>
    public class MacResManager
    {
        const int MBI_INFOHDR = 128;
        const int MBI_ZERO1 = 0;
        const int MBI_NAMELEN = 1;
        const int MBI_ZERO2 = 74;
        const int MBI_ZERO3 = 82;
        const int MBI_DFLEN = 83;
        const int MBI_RFLEN = 87;
        const int MAXNAMELEN = 63;

        enum ResForkMode
        {
            None = 0,
            Raw,
            MacBinary,
            AppleDouble
        }

        struct ResMap
        {
            public ushort resAttr;
            public ushort typeOffset;
            public ushort nameOffset;
            public ushort numTypes;
        }

        class ResType
        {
            public uint id;
            public ushort items;
            public ushort offset;
        }

        class Resource
        {
            public ushort id;
            public short nameOffset;
            public byte attr;
            public uint dataOffset;
            public string name;
        }

        Stream _stream;
        string _baseFileName;

        ResForkMode _mode;

        int _resForkOffset;
        int _resForkSize;

        int _dataOffset;

        int _dataLength;
        int _mapOffset;
        int _mapLength;
        ResMap _resMap;
        ResType[] _resTypes;
        Ptr<Resource>[] _resLists;

        public bool HasResFork
        {
            get
            {
                return !string.IsNullOrEmpty(_baseFileName) && _mode != ResForkMode.None;
            }
        }

        public MacResManager()
        {
            Close();
        }

        /// <summary>
        /// Open a Mac data/resource fork pair.
        /// This uses SearchMan to find the data/resource forks. This should only be used
        /// from inside an engine.
        /// </summary>
        /// <param name="fileName">The base file name of the file.</param>
        /// <remarks>This will check for the raw resource fork, MacBinary, and AppleDouble formats.</remarks>
        /// <returns>True on success</returns>
        public bool Open(string fileName)
        {
            Close();

# if MACOSX
        // Check the actual fork on a Mac computer
        String fullPath = ConfMan.get("path") + "/" + fileName + "/..namedfork/rsrc";
        FSNode resFsNode = FSNode(fullPath);
    if (resFsNode.exists()) {
        SeekableReadStream* macResForkRawStream = resFsNode.createReadStream();

        if (macResForkRawStream && loadFromRawFork(*macResForkRawStream)) {
            _baseFileName = fileName;
            return true;
        }

    delete macResForkRawStream;
}
#endif

            var file = Engine.OpenFileRead(fileName + ".rsrc");

            // Prefer standalone files first, starting with raw forks
            if (file != null && LoadFromRawFork(file))
            {
                _baseFileName = fileName;
                return true;
            }
            file.Dispose();

            file = Engine.OpenFileRead(ConstructAppleDoubleName(fileName));
            // Then try for AppleDouble using Apple's naming
            if (file != null && LoadFromAppleDouble(file))
            {
                _baseFileName = fileName;
                return true;
            }
            file.Dispose();

            // Check .bin for MacBinary next
            file = Engine.OpenFileRead(fileName + ".bin");
            if (file != null && LoadFromMacBinary(file))
            {
                _baseFileName = fileName;
                return true;
            }
            file.Dispose();

            // As a last resort, see if just the data fork exists
            file = Engine.OpenFileRead(fileName);
            if (file != null)
            {
                _baseFileName = fileName;

                // FIXME: Is this really needed?
                if (IsMacBinary(file))
                {
                    file.Seek(0, SeekOrigin.Begin);
                    if (LoadFromMacBinary(file))
                        return true;
                }

                file.Seek(0, SeekOrigin.Begin);
                _stream = file;
                return true;
            }

            file.Dispose();

            // The file doesn't exist
            return false;
        }

        public ushort[] GetResIDArray(uint typeID)
        {
            int typeNum = -1;

            for (int i = 0; i < _resMap.numTypes; i++)
                if (_resTypes[i].id == typeID)
                {
                    typeNum = i;
                    break;
                }

            if (typeNum == -1)
                return new ushort[0];

            ushort[] res = new ushort[_resTypes[typeNum].items];

            for (int i = 0; i < _resTypes[typeNum].items; i++)
                res[i] = _resLists[typeNum][i].id;

            return res;
        }

        public string GetResName(uint typeID, ushort resID)
        {
            int typeNum = -1;

            for (int i = 0; i < _resMap.numTypes; i++)
                if (_resTypes[i].id == typeID)
                {
                    typeNum = i;
                    break;
                }

            if (typeNum == -1)
                return "";

            for (int i = 0; i < _resTypes[typeNum].items; i++)
                if (_resLists[typeNum][i].id == resID)
                    return _resLists[typeNum][i].name;

            return "";
        }

        public void Close()
        {
            _resForkOffset = -1;
            _mode = ResForkMode.None;

            for (int i = 0; i < _resMap.numTypes; i++)
            {
                //for (int j = 0; j < _resTypes[i].items; j++)
                //    if (_resLists[i][j].nameOffset != -1)
                //        delete[] _resLists[i][j].name;

                //delete[] _resLists[i];
            }

            //delete[] _resLists; _resLists = 0;
            //delete[] _resTypes; _resTypes = 0;
            //delete _stream; _stream = 0;
            _resMap.numTypes = 0;
        }

        public uint[] GetResTagArray()
        {
            uint[] tagArray;

            if (!HasResFork)
                return new uint[0];

            tagArray = new uint[_resMap.numTypes];

            for (var i = 0; i < _resMap.numTypes; i++)
                tagArray[i] = _resTypes[i].id;

            return tagArray;
        }

        public Stream GetResource(uint typeID, ushort resID)
        {
            int typeNum = -1;
            int resNum = -1;

            var br = new BinaryReader(_stream);
            for (int i = 0; i < _resMap.numTypes; i++)
                if (_resTypes[i].id == typeID)
                {
                    typeNum = i;
                    break;
                }

            if (typeNum == -1)
                return null;

            for (int i = 0; i < _resTypes[typeNum].items; i++)
                if (_resLists[typeNum][i].id == resID)
                {
                    resNum = i;
                    break;
                }

            if (resNum == -1)
                return null;

            _stream.Seek(_dataOffset + _resLists[typeNum][resNum].dataOffset, SeekOrigin.Begin);
            int len = br.ReadInt32BigEndian();

            // Ignore resources with 0 length
            if (len == 0)
                return null;

            return new SeekableSubReadStream(_stream, _stream.Position, _stream.Position + len);
        }

        public Stream GetResource(string fileName)
        {
            var br = new BinaryReader(_stream);
            for (var i = 0; i < _resMap.numTypes; i++)
            {
                for (var j = 0; j < _resTypes[i].items; j++)
                {
                    if (_resLists[i][j].nameOffset != -1 && string.Equals(fileName, _resLists[i][j].name, StringComparison.OrdinalIgnoreCase))
                    {
                        _stream.Seek(_dataOffset + _resLists[i][j].dataOffset, SeekOrigin.Begin);
                        var len = br.ReadUInt32BigEndian();

                        // Ignore resources with 0 length
                        if (len == 0)
                            return null;

                        return new SeekableSubReadStream(_stream, _stream.Position, _stream.Position + len);
                    }
                }
            }

            return null;
        }

        public static IEnumerable<string> ListFiles(string pattern)
        {
            var directory = ServiceLocator.FileStorage.GetDirectoryName(Engine.Instance.Settings.Game.Path);
            var baseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // List files itself.
            var memberList = new [] { pattern, pattern + ".rsrc", pattern + ".bin", ConstructAppleDoubleName(pattern) }
                .SelectMany(p => ServiceLocator.FileStorage.EnumerateFiles(directory, p));

            foreach (var i in memberList)
            {
                var filename = ServiceLocator.FileStorage.GetFileName(i);

                // For raw resource forks and MacBinary files we strip the extension
                // here to obtain a valid base name.
                var extension = ServiceLocator.FileStorage.GetExtension(filename);

                if (!string.IsNullOrEmpty(extension))
                {
                    bool removeExtension = false;

                    // TODO: Should we really keep filenames suggesting raw resource
                    // forks or MacBinary files but not being such around? This might
                    // depend on the pattern the client requests...
                    if (extension== "rsrc")
                    {
                        using (var stream = Engine.OpenFileRead(i))
                        {
                            removeExtension = IsRawFork(stream);
                        }
                    }
                    else if (extension=="bin")
                    {
                        using (var stream = Engine.OpenFileRead(i))
                        {
                            removeExtension = IsMacBinary(stream);
                        }
                    }

                    if (removeExtension)
                    {
                        filename = ServiceLocator.FileStorage.GetFileNameWithoutExtension(i);
                    }
                }

                // Strip AppleDouble '._' prefix if applicable.
                bool isAppleDoubleName;
                string filenameAppleDoubleStripped = DisassembleAppleDoubleName(filename, out isAppleDoubleName);

                if (isAppleDoubleName)
                {
                    using (var stream = Engine.OpenFileRead(i))
                    {
                        var br = new BinaryReader(stream);
                        if (br.ReadUInt32BigEndian() == 0x00051607)
                        {
                            filename = filenameAppleDoubleStripped;
                        }
                        // TODO: Should we really keep filenames suggesting AppleDouble
                        // but not being AppleDouble around? This might depend on the
                        // pattern the client requests...
                    }
                }

                baseNames.Add(filename);
            }

            // Append resulting base names to list to indicate found files.
            return baseNames;
        }

        private static bool IsRawFork(Stream stream)
        {
            var br = new BinaryReader(stream);
            // TODO: Is there a better way to detect whether this is a raw fork?
            int dataOffset = br.ReadInt32BigEndian();
            int mapOffset = br.ReadInt32BigEndian();
            int dataLength = br.ReadInt32BigEndian();
            int mapLength = br.ReadInt32BigEndian();

            return dataOffset < stream.Length && dataOffset + dataLength <= stream.Length
                && mapOffset < stream.Length && mapOffset + mapLength <= stream.Length;
        }

        private static string DisassembleAppleDoubleName(string name, out bool isAppleDouble)
        {
            isAppleDouble = false;
            var n = new StringBuilder(name);
            // Remove "._" before the last portion of a path name.
            for (int i = n.Length - 1; i >= 0; --i)
            {
                if (i == 0)
                {
                    if (n.Length > 2 && n[0] == '.' && n[1] == '_')
                    {
                        n.Remove(0,2);
                        isAppleDouble = true;
                    }
                }
                else if (n[i] == '/')
                {
                    if ((i + 2) < n.Length && n[i + 1] == '.' && n[i + 2] == '_')
                    {
                        n.Remove(i + 1, 2);
                        isAppleDouble = true;
                    }
                    break;
                }
            }

            return n.ToString();
        }

        public static bool Exists(string fileName)
        {
            // Try the file name by itself
            if (ServiceLocator.FileStorage.FileExists(fileName))
                return true;

            // Try the .rsrc extension
            if (ServiceLocator.FileStorage.FileExists(fileName + ".rsrc"))
                return true;

            // Check if we have a MacBinary file
            var tempFile = Engine.OpenFileRead(fileName + ".bin");
            if (tempFile != null)
            {
                using (tempFile)
                {
                    if (IsMacBinary(tempFile))
                        return true;
                }
            }

            // Check if we have an AppleDouble file
            tempFile = Engine.OpenFileRead(ConstructAppleDoubleName(fileName));
            if (tempFile != null)
            {
                using (tempFile)
                {
                    var br = new BinaryReader(tempFile);
                    if (br.ReadUInt32BigEndian() == 0x00051607)
                        return true;
                }
            }

            return false;
        }

        private bool LoadFromRawFork(Stream stream)
        {
            _mode = ResForkMode.Raw;
            _resForkOffset = 0;
            _resForkSize = (int)stream.Length;
            return Load(stream);
        }

        private bool Load(Stream stream)
        {
            if (_mode == ResForkMode.None)
                return false;

            stream.Seek(_resForkOffset, SeekOrigin.Begin);
            var br = new BinaryReader(stream);

            _dataOffset = br.ReadInt32BigEndian() + _resForkOffset;
            _mapOffset = br.ReadInt32BigEndian() + _resForkOffset;
            _dataLength = br.ReadInt32BigEndian();
            _mapLength = br.ReadInt32BigEndian();

            // do sanity check
            if (stream.Position != stream.Length || _dataOffset >= stream.Length || _mapOffset >= stream.Length ||
                _dataLength + _mapLength > stream.Length)
            {
                _resForkOffset = -1;
                _mode = ResForkMode.None;
                return false;
            }

            Debug(7, "got header: data %d [%d] map %d [%d]",
                _dataOffset, _dataLength, _mapOffset, _mapLength);

            _stream = stream;

            ReadMap();
            return true;
        }

        private static string ConstructAppleDoubleName(string name)
        {
            var sb = new StringBuilder(name);
            // Insert "._" before the last portion of a path name
            for (int i = sb.Length - 1; i >= 0; i--)
            {
                if (i == 0)
                {
                    sb.Insert(0, "._");
                }
                else if (name[i] == '/')
                {
                    sb.Insert(i + 1, "._");
                    break;
                }
            }

            return name;
        }

        private bool LoadFromAppleDouble(Stream stream)
        {
            var br = new BinaryReader(stream);
            if (br.ReadUInt32BigEndian() != 0x00051607) // tag
                return false;

            stream.Seek(20, SeekOrigin.Current); // version + home file system

            ushort entryCount = br.ReadUInt16BigEndian();

            for (var i = 0; i < entryCount; i++)
            {
                uint id = br.ReadUInt32BigEndian();
                int offset = br.ReadInt32BigEndian();
                int length = br.ReadInt32BigEndian(); // length

                if (id == 2)
                {
                    // Found the resource fork!
                    _resForkOffset = offset;
                    _mode = ResForkMode.AppleDouble;
                    _resForkSize = length;
                    return Load(stream);
                }
            }

            return false;
        }

        private void ReadMap()
        {
            var br = new BinaryReader(_stream);
            _stream.Seek(_mapOffset + 22, SeekOrigin.Begin);

            _resMap.resAttr = br.ReadUInt16BigEndian();
            _resMap.typeOffset = br.ReadUInt16BigEndian();
            _resMap.nameOffset = br.ReadUInt16BigEndian();
            _resMap.numTypes = br.ReadUInt16BigEndian();
            _resMap.numTypes++;

            _stream.Seek(_mapOffset + _resMap.typeOffset + 2, SeekOrigin.Begin);
            _resTypes = new ResType[_resMap.numTypes];

            for (int i = 0; i < _resMap.numTypes; i++)
            {
                _resTypes[i].id = br.ReadUInt32BigEndian();
                _resTypes[i].items = br.ReadUInt16BigEndian();
                _resTypes[i].offset = br.ReadUInt16BigEndian();
                _resTypes[i].items++;

                Debug(8, "resType: <{0}> items: {1} offset: {2} (0x{3})", Tag2String(_resTypes[i].id), _resTypes[i].items, _resTypes[i].offset, _resTypes[i].offset);
            }

            _resLists = new Ptr<Resource>[_resMap.numTypes];

            for (int i = 0; i < _resMap.numTypes; i++)
            {
                _resLists[i] = new Resource[_resTypes[i].items];
                _stream.Seek(_resTypes[i].offset + _mapOffset + _resMap.typeOffset, SeekOrigin.Begin);

                for (int j = 0; j < _resTypes[i].items; j++)
                {
                    Ptr<Resource> resPtr = new Ptr<Resource>(_resLists[i], j);

                    resPtr.Value.id = br.ReadUInt16BigEndian();
                    resPtr.Value.nameOffset = br.ReadInt16BigEndian();
                    resPtr.Value.dataOffset = br.ReadUInt32BigEndian();
                    br.ReadUInt32BigEndian();
                    resPtr.Value.name = string.Empty;

                    resPtr.Value.attr = (byte)(resPtr.Value.dataOffset >> 24);
                    resPtr.Value.dataOffset &= 0xFFFFFF;
                }

                for (int j = 0; j < _resTypes[i].items; j++)
                {
                    if (_resLists[i][j].nameOffset != -1)
                    {
                        _stream.Seek(_resLists[i][j].nameOffset + _mapOffset + _resMap.nameOffset, SeekOrigin.Begin);

                        byte len = br.ReadByte();
                        _resLists[i][j].name = br.ReadBytes(len).GetRawText();
                    }
                }
            }
        }

        private static string Tag2String(uint tag)
        {
            char[] str = new char[5];
            str[0] = (char)(tag >> 24);
            str[1] = (char)(tag >> 16);
            str[2] = (char)(tag >> 8);
            str[3] = (char)tag;
            str[4] = '\0';
            // Replace non-printable chars by dot
            for (int i = 0; i < 4; ++i)
            {
                if (!IsPrint(str[i]))
                    str[i] = '.';
            }
            return new string(str);
        }

        private static bool IsAsciiChar(int c)
        {
            if (c < 0 || c > 127)
                return false;
            return true;
        }

        private static bool IsPrint(int c)
        {
            if (!IsAsciiChar(c)) return false;
            // TODO: check this
            return c > 0x1f && c != 0x7f;
        }

        private bool LoadFromMacBinary(Stream stream)
        {
            byte[] infoHeader = new byte[MBI_INFOHDR];
            stream.Read(infoHeader, 0, MBI_INFOHDR);

            // Maybe we have MacBinary?
            if (infoHeader[MBI_ZERO1] == 0 && infoHeader[MBI_ZERO2] == 0 &&
                infoHeader[MBI_ZERO3] == 0 && infoHeader[MBI_NAMELEN] <= MAXNAMELEN)
            {

                // Pull out fork lengths
                int dataSize = infoHeader.ToInt32BigEndian(MBI_DFLEN);
                int rsrcSize = infoHeader.ToInt32BigEndian(MBI_RFLEN);

                int dataSizePad = (((dataSize + 127) >> 7) << 7);
                int rsrcSizePad = (((rsrcSize + 127) >> 7) << 7);

                // Length check
                if (MBI_INFOHDR + dataSizePad + rsrcSizePad == stream.Length)
                {
                    _resForkOffset = MBI_INFOHDR + dataSizePad;
                    _resForkSize = rsrcSize;
                }
            }

            if (_resForkOffset < 0)
                return false;

            _mode = ResForkMode.MacBinary;
            return Load(stream);
        }

        private static bool IsMacBinary(Stream stream)
        {
            byte[] infoHeader = new byte[MBI_INFOHDR];
            int resForkOffset = -1;

            stream.Read(infoHeader, 0, MBI_INFOHDR);

            if (infoHeader[MBI_ZERO1] == 0 && infoHeader[MBI_ZERO2] == 0 &&
                infoHeader[MBI_ZERO3] == 0 && infoHeader[MBI_NAMELEN] <= MAXNAMELEN)
            {

                // Pull out fork lengths
                int dataSize = infoHeader.ToInt32BigEndian(MBI_DFLEN);
                int rsrcSize = infoHeader.ToInt32BigEndian(MBI_RFLEN);

                int dataSizePad = (((dataSize + 127) >> 7) << 7);
                int rsrcSizePad = (((rsrcSize + 127) >> 7) << 7);

                // Length check
                if (MBI_INFOHDR + dataSizePad + rsrcSizePad == stream.Length)
                {
                    resForkOffset = MBI_INFOHDR + dataSizePad;
                }
            }

            if (resForkOffset < 0)
                return false;

            return true;
        }

    }
}
