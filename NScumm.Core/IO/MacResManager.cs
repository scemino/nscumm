//
//  MacResManager.cs
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
using System;
using System.IO;
using System.Diagnostics;

namespace NScumm.Core
{
    /// <summary>
    /// Class for handling Mac data and resource forks.
    /// It can read from raw, MacBinary, and AppleDouble formats.
    /// </summary>
    public class MacResManager
    {
        public MacResManager(string gamePath)
        {
            _gamePath = gamePath;
        }

        public void Close()
        {
            _baseFileName = null;
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
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

        public bool Exists(string fileName)
        {
            // Try the file name by itself
            var path = ScummHelper.LocatePath(_gamePath, fileName);
            if (path != null)
                return true;

            // Try the .rsrc extension
//            if (File::exists(fileName + ".rsrc"))
//                return true;

            // Check if we have a MacBinary file
            path = ScummHelper.LocatePath(_gamePath, fileName + ".bin");
            if (path != null /*&& isMacBinary(tempFile)*/)
                return true;

            // Check if we have an AppleDouble file
//            if (tempFile.open(constructAppleDoubleName(fileName)) && tempFile.readUint32BE() == 0x00051607)
//                return true;

            return false;
        }

        public bool Open(string fileName)
        {
            Close();

#if MACOSX
    // Check the actual fork on a Mac computer
//    String fullPath = ConfMan.get("path") + "/" + fileName + "/..namedfork/rsrc";
//    FSNode resFsNode = FSNode(fullPath);
//    if (resFsNode.exists()) {
//        SeekableReadStream *macResForkRawStream = resFsNode.createReadStream();
//
//        if (macResForkRawStream && loadFromRawFork(*macResForkRawStream)) {
//            _baseFileName = fileName;
//            return true;
//        }
//
//        delete macResForkRawStream;
//    }
#endif

//            var file = ServiceLocator.FileStorage.OpenFileRead(fileName + ".rsrc");

            // Prefer standalone files first, starting with raw forks
//    if (file.open(fileName + ".rsrc") && loadFromRawFork(*file)) {
//        _baseFileName = fileName;
//        return true;
//    }
//    file.close();

            // Then try for AppleDouble using Apple's naming
//    if (file.open(constructAppleDoubleName(fileName)) && loadFromAppleDouble(*file)) {
//        _baseFileName = fileName;
//        return true;
//    }
//    file.close();

            // Check .bin for MacBinary next
            var path = ScummHelper.LocatePath(_gamePath, fileName + ".bin");
            if (path != null)
            {
                var file = ServiceLocator.FileStorage.OpenFileRead(path);
                if (LoadFromMacBinary(file))
                {
                    _baseFileName = fileName;
                    return true;
                }
            }

            // As a last resort, see if just the data fork exists
            path = ScummHelper.LocatePath(_gamePath, fileName);
            if (ServiceLocator.FileStorage.FileExists(path))
            {
                var file = ServiceLocator.FileStorage.OpenFileRead(path);

                _baseFileName = fileName;

                // FIXME: Is this really needed?
//                if (IsMacBinary(file))
//                {
//                    file.Seek(0, System.IO.SeekOrigin.Begin);
//                    if (LoadFromMacBinary(file))
//                        return true;
//                }

                file.Seek(0, SeekOrigin.Begin);
                _stream = file;
                return true;
            }

            // The file doesn't exist
            return false;
        }

        /**
     * Read resource from the MacBinary file
     * @param typeID FourCC of the type
     * @param resID Resource ID to fetch
     * @return Pointer to a SeekableReadStream with loaded resource
     */
        public Stream GetResource(uint typeID, ushort resID)
        {
            var br = new BinaryReader(_stream);
            int typeNum = -1;
            int resNum = -1;

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
            var len = (int)br.ReadUInt32BigEndian();

            // Ignore resources with 0 length
            if (len == 0)
                return null;

            return new MemoryStream(br.ReadBytes(len));
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

        bool LoadFromMacBinary(Stream stream)
        {
            var br = new BinaryReader(stream);
            byte[] infoHeader = br.ReadBytes(MBI_INFOHDR);

            // Maybe we have MacBinary?
            if (infoHeader[MBI_ZERO1] == 0 && infoHeader[MBI_ZERO2] == 0 &&
                infoHeader[MBI_ZERO3] == 0 && infoHeader[MBI_NAMELEN] <= MAXNAMELEN)
            {

                // Pull out fork lengths
                var dataSize = infoHeader.ToInt32BigEndian(MBI_DFLEN);
                var rsrcSize = infoHeader.ToInt32BigEndian(MBI_RFLEN);

                var dataSizePad = (((dataSize + 127) >> 7) << 7);
                var rsrcSizePad = (((rsrcSize + 127) >> 7) << 7);

                // Length check
                if (MBI_INFOHDR + dataSizePad + rsrcSizePad == (uint)stream.Length)
                {
                    _resForkOffset = MBI_INFOHDR + dataSizePad;
                    _resForkSize = rsrcSize;
                }
            }

            if (_resForkOffset < 0)
                return false;

            _mode = Mode.MacBinary;
            return Load(stream);
        }

        bool Load(Stream stream)
        {
            var br = new BinaryReader(stream);
            if (_mode == Mode.None)
                return false;

            stream.Seek(_resForkOffset, SeekOrigin.Begin);

            _dataOffset = (uint)(br.ReadUInt32BigEndian() + _resForkOffset);
            _mapOffset = (uint)(br.ReadUInt32BigEndian() + _resForkOffset);
            _dataLength = br.ReadUInt32BigEndian();
            _mapLength = br.ReadUInt32BigEndian();

            // do sanity check
            if (stream.Position == stream.Length || _dataOffset >= stream.Length || _mapOffset >= stream.Length ||
                _dataLength + _mapLength > stream.Length)
            {
                _resForkOffset = -1;
                _mode = Mode.None;
                return false;
            }

            Debug.WriteLine("got header: data {0} [{1}] map {2} [{3}]",
                _dataOffset, _dataLength, _mapOffset, _mapLength);

            _stream = stream;

            ReadMap();
            return true;
        }

        void ReadMap()
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

                Debug.WriteLine("resType: <{0}> items: {1} offset: {2} (0x{3:X2})", _resTypes[i].id, _resTypes[i].items, _resTypes[i].offset, _resTypes[i].offset);
            }

            _resLists = new Resource[_resMap.numTypes][];

            for (int i = 0; i < _resMap.numTypes; i++)
            {
                _resLists[i] = new Resource[_resTypes[i].items];
                _stream.Seek(_resTypes[i].offset + _mapOffset + _resMap.typeOffset, SeekOrigin.Begin);

                for (int j = 0; j < _resTypes[i].items; j++)
                {
                    var resPtr = _resLists[i][j] = new Resource();

                    resPtr.id = br.ReadUInt16BigEndian();
                    resPtr.nameOffset = (short)br.ReadUInt16BigEndian();
                    resPtr.dataOffset = br.ReadUInt32BigEndian();
                    br.ReadUInt32BigEndian();

                    resPtr.attr = (byte)(resPtr.dataOffset >> 24);
                    resPtr.dataOffset &= 0xFFFFFF;
                }

                for (int j = 0; j < _resTypes[i].items; j++)
                {
                    if (_resLists[i][j].nameOffset != -1)
                    {
                        _stream.Seek(_resLists[i][j].nameOffset + _mapOffset + _resMap.nameOffset, SeekOrigin.Begin);

                        byte len = br.ReadByte();
                        _resLists[i][j].name = System.Text.Encoding.UTF8.GetString(br.ReadBytes(len));
                    }
                }
            }
        }

        enum Mode
        {
            None = 0,
            Raw,
            MacBinary,
            AppleDouble
        }

        Mode _mode;

        const int MBI_INFOHDR = 128;
        const int MBI_ZERO1 = 0;
        const int MBI_NAMELEN = 1;
        const int MBI_ZERO2 = 74;
        const int MBI_ZERO3 = 82;
        const int MBI_DFLEN = 83;
        const int MBI_RFLEN = 87;
        const int MAXNAMELEN = 63;

        struct ResMap
        {
            public ushort resAttr;
            public ushort typeOffset;
            public ushort nameOffset;
            public ushort numTypes;
        }

        struct ResType
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

        ResType[] _resTypes;
        Resource[][] _resLists;

        ResMap _resMap;
        Stream _stream;
        string _baseFileName;

        int _resForkOffset;
        int _resForkSize;

        uint _dataOffset;
        uint _dataLength;
        uint _mapOffset;
        uint _mapLength;

        string _gamePath;
    }


}

