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
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci
{
    enum ResSourceType
    {
        /// <summary>
        /// Directories containing game resources/patches.
        /// </summary>
        Directory = 0,
        /// <summary>
        /// External resource patches.
        /// </summary>
        Patch,
        /// <summary>
        /// Game resources (resource.* or ressci.*).
        /// </summary>
        Volume,
        /// <summary>
        /// Non-audio resource maps.
        /// </summary>
        ExtMap,
        /// <summary>
        /// SCI1.1 and later audio resource maps.
        /// </summary>
        IntMap,
        /// <summary>
        /// Audio resources - resource.sfx / resource.aud.
        /// </summary>
        AudioVolume,
        /// <summary>
        /// SCI1 audio resource maps.
        /// </summary>
        ExtAudioMap,
        /// <summary>
        /// External WAVE files, patched in as sound resources.
        /// </summary>
        Wave,
        /// <summary>
        /// Mac SCI1.1 and later resource forks.
        /// </summary>
        MacResourceFork,
        /// <summary>
        /// Script chunk resources (*.chk).
        /// </summary>
        Chunk
    }

    /// <summary>
    /// Resource status types
    /// </summary>
    enum ResourceStatus
    {
        NoMalloc = 0,
        Allocated,
        /// <summary>
        /// In the LRU queue.
        /// </summary>
        Enqueued,
        /// <summary>
        /// Allocated and in use 
        /// </summary>
        Locked
    }

    partial class ResourceManager
    {
        public class ResourceMap : Dictionary<ResourceId, ResourceSource.Resource>
        {
        }

        public class ResourceSource
        {
            /// <summary>
            /// Class for storing resources in memory.
            /// </summary>
            public class Resource
            {
                internal const int MAX_MEMORY = 256 * 1024;	// 256KB

                internal ResourceId _id; // TODO: _id could almost be made const, only readResourceInfo() modifies it...

                /// <summary>
                /// Offset in file
                /// </summary>
                internal int _fileOffset;
                internal ResourceSource _source;
                protected ResourceManager _resMan;
                internal ResourceStatus _status;
                /// <summary>
                /// Number of places where this resource was locked
                /// </summary>
                internal ushort _lockers;

                // NOTE : Currently most member variables lack the underscore prefix and have
                // public visibility to let the rest of the engine compile without changes.
                public int size;
                public byte[] data;
                public int _headerSize;
                public byte[] _header;

                public uint AudioCompressionType
                {
                    get
                    {
                        return _source.AudioCompressionType;
                    }
                }

                public ResourceType ResourceType { get { return _id.Type; } }

                public ushort Number { get { return _id.Number; } }

                public bool IsLocked { get { return _status == ResourceStatus.Locked; } }

                public Resource(ResourceManager resMan, ResourceId id)
                {
                    _resMan = resMan;
                    _id = id;
                }

                public ResourceErrorCodes ReadResourceInfo(ResVersion volVersion, Stream file,
                                      out int szPacked, out ResourceCompression compression)
                {
                    szPacked = 0;
                    compression = ResourceCompression.None;

                    // SCI0 volume format:  {wResId wPacked+4 wUnpacked wCompression} = 8 bytes
                    // SCI1 volume format:  {bResType wResNumber wPacked+4 wUnpacked wCompression} = 9 bytes
                    // SCI1.1 volume format:  {bResType wResNumber wPacked wUnpacked wCompression} = 9 bytes
                    // SCI32 volume format :  {bResType wResNumber dwPacked dwUnpacked wCompression} = 13 bytes
                    ushort w, number;
                    int wCompression, szUnpacked;
                    ResourceType type;

                    if (file.Length == 0)
                        return ResourceErrorCodes.EMPTY_RESOURCE;

                    var br = new BinaryReader(file);
                    switch (volVersion)
                    {
                        case ResVersion.Sci0Sci1Early:
                        case ResVersion.Sci1Middle:
                            w = br.ReadUInt16();
                            type = _resMan.ConvertResType(w >> 11);
                            number = (ushort)(w & 0x7FF);
                            szPacked = br.ReadUInt16() - 4;
                            szUnpacked = br.ReadUInt16();
                            wCompression = br.ReadUInt16();
                            break;
                        case ResVersion.Sci1Late:
                            type = _resMan.ConvertResType(file.ReadByte());
                            number = br.ReadUInt16();
                            szPacked = br.ReadUInt16() - 4;
                            szUnpacked = br.ReadUInt16();
                            wCompression = br.ReadUInt16();
                            break;
                        case ResVersion.Sci11:
                            type = _resMan.ConvertResType(br.ReadByte());
                            number = br.ReadUInt16();
                            szPacked = br.ReadUInt16();
                            szUnpacked = br.ReadUInt16();
                            wCompression = br.ReadUInt16();
                            break;
#if ENABLE_SCI32
                        case ResVersion.Sci2:
                        case ResVersion.Sci3:
                            type = _resMan.ConvertResType(file.readByte());
                            number = br.ReadUInt16();
                            szPacked = br.ReadUInt32();
                            szUnpacked = br.ReadUInt32();

                            // The same comment applies here as in
                            // detectVolVersion regarding SCI3. We ignore the
                            // compression field for SCI3 games, but must presume
                            // it exists in the file.
                            wCompression = br.ReadUInt16();

                            if (volVersion == kResVersionSci3)
                                wCompression = szPacked != szUnpacked ? 32 : 0;

                            break;
#endif
                        default:
                            return ResourceErrorCodes.RESMAP_INVALID_ENTRY;
                    }

                    // check if there were errors while reading
                    if (file.Position >= file.Length)
                        return ResourceErrorCodes.IO_ERROR;

                    _id = new ResourceId(type, number);
                    size = szUnpacked;

                    // checking compression method
                    switch (wCompression)
                    {
                        case 0:
                            compression = ResourceCompression.None;
                            break;
                        case 1:
                            compression = (GetSciVersion() <= SciVersion.V01) ? ResourceCompression.LZW : ResourceCompression.Huffman;
                            break;
                        case 2:
                            compression = (GetSciVersion() <= SciVersion.V01) ? ResourceCompression.Huffman : ResourceCompression.LZW1;
                            break;
                        case 3:
                            compression = ResourceCompression.LZW1View;
                            break;
                        case 4:
                            compression = ResourceCompression.LZW1Pic;
                            break;
                        case 18:
                        case 19:
                        case 20:
                            compression = ResourceCompression.DCL;
                            break;
#if ENABLE_SCI32
                        case 32:
                            compression = ResourceCompression.STACpack;
                            break;
#endif
                        default:
                            compression = ResourceCompression.Unknown;
                            break;
                    }

                    return (compression == ResourceCompression.Unknown) ? ResourceErrorCodes.UNKNOWN_COMPRESSION : ResourceErrorCodes.NONE;
                }

                public void Unalloc()
                {
                    data = null;
                    _status = ResourceStatus.NoMalloc;
                }

                public bool LoadFromAudioVolumeSCI1(Stream stream)
                {
                    data = new byte[size];

                    int really_read = stream.Read(data, 0, size);
                    if (really_read != size)
                        Warning("Read {0} bytes from {1} but expected {2}", really_read, _id, size);

                    _status = ResourceStatus.Allocated;
                    return true;
                }

                public bool LoadFromAudioVolumeSCI11(Stream stream)
                {
                    // Check for WAVE files here
                    var br = new BinaryReader(stream);
                    uint riffTag = br.ReadUInt32BigEndian();
                    if (riffTag == ScummHelper.MakeTag('R', 'I', 'F', 'F'))
                    {
                        _headerSize = 0;
                        size = br.ReadInt32() + 8;
                        stream.Seek(-8, SeekOrigin.Current);
                        return LoadFromWaveFile(stream);
                    }
                    stream.Seek(-4, SeekOrigin.Current);

                    // Rave-resources (King's Quest 6) don't have any header at all
                    if (ResourceType != ResourceType.Rave)
                    {
                        var type = _resMan.ConvertResType(br.ReadByte());
                        if (((ResourceType == ResourceType.Audio || ResourceType == ResourceType.Audio36) && (type != ResourceType.Audio))
                            || ((ResourceType == ResourceType.Sync || ResourceType == ResourceType.Sync36) && (type != ResourceType.Sync)))
                        {
                            Warning("Resource type mismatch loading {0}", _id);
                            Unalloc();
                            return false;
                        }

                        _headerSize = br.ReadByte();

                        if (type == ResourceType.Audio)
                        {
                            if (_headerSize != 7 && _headerSize != 11 && _headerSize != 12)
                            {
                                Warning("Unsupported audio header");
                                Unalloc();
                                return false;
                            }

                            if (_headerSize != 7)
                            { // Size is defined already from the map
                              // Load sample size
                                stream.Seek(7, SeekOrigin.Current);
                                size = br.ReadInt32();
                                // Adjust offset to point at the header data again
                                stream.Seek(-11, SeekOrigin.Current);
                            }
                        }
                    }
                    return LoadPatch(stream);
                }

                private bool LoadFromWaveFile(Stream stream)
                {
                    data = new byte[size];

                    int really_read = stream.Read(data, 0, size);
                    if (really_read != size)
                        Error("Read {0} bytes from {1} but expected {2}", really_read, _id, size);

                    _status = ResourceStatus.Allocated;
                    return true;
                }

                public bool LoadFromPatchFile()
                {
                    string filename = _source.LocationName;
                    var file = Core.Engine.OpenFileRead(filename);
                    if (file == null)
                    {
                        Warning($"Failed to open patch file {filename}");
                        Unalloc();
                        return false;
                    }
                    // Skip resourceid and header size byte
                    file.Seek(2, SeekOrigin.Begin);
                    return LoadPatch(file);
                }

                // Resource manager constructors and operations

                private bool LoadPatch(Stream file)
                {
                    Resource res = this;

                    // We assume that the resource type matches res.type
                    //  We also assume that the current file position is right at the actual data (behind resourceid/headersize byte)

                    res.data = new byte[res.size];

                    if (res._headerSize > 0)
                        res._header = new byte[res._headerSize];

                    if ((res.data == null) || ((res._headerSize > 0) && (res._header == null)))
                    {
                        Error($"Can't allocate {res.size + res._headerSize} bytes needed for loading {res._id}");
                    }

                    int really_read;
                    if (res._headerSize > 0)
                    {
                        really_read = file.Read(res._header, 0, res._headerSize);
                        if (really_read != res._headerSize)
                            Error($"Read {really_read} bytes from {res._id} but expected {res._headerSize}");
                    }

                    really_read = file.Read(res.data, 0, res.size);
                    if (really_read != res.size)
                        Error($"Read {really_read} bytes from {res._id} but expected {res.size}");

                    res._status = ResourceStatus.Allocated;
                    return true;
                }


                internal ResourceErrorCodes Decompress(ResVersion volVersion, Stream file)
                {
                    int szPacked = 0;
                    ResourceCompression compression;

                    // fill resource info
                    var errorNum = ReadResourceInfo(volVersion, file, out szPacked, out compression);
                    if (errorNum != ResourceErrorCodes.NONE)
                        return errorNum;

                    // getting a decompressor
                    Decompressor dec = null;
                    switch (compression)
                    {
                        case ResourceCompression.None:
                            dec = new Decompressor();
                            break;
                        case ResourceCompression.Huffman:
                            dec = new DecompressorHuffman();
                            break;
                        case ResourceCompression.LZW:
                        case ResourceCompression.LZW1:
                        case ResourceCompression.LZW1View:
                        case ResourceCompression.LZW1Pic:
                            dec = new DecompressorLZW(compression);
                            break;
                        case ResourceCompression.DCL:
                            dec = new DecompressorDCL();
                            break;
#if ENABLE_SCI32
                        case ResourceCompression.STACpack:
                            dec = new DecompressorLZS;
                            break;
#endif
                        default:
                            Error($"Resource {_id}: Compression method {compression} not supported");
                            return ResourceErrorCodes.UNKNOWN_COMPRESSION;
                    }

                    data = new byte[size];
                    _status = ResourceStatus.Allocated;
                    errorNum = dec.Unpack(file, data, szPacked, size);
                    if (errorNum != ResourceErrorCodes.NONE)
                        Unalloc();

                    return errorNum;
                }

            }

            protected readonly ResSourceType _sourceType;
            protected readonly string _name;
            public readonly object _resourceFile;
            public readonly int _volumeNumber;
            internal bool _scanned;

            public ResSourceType SourceType { get { return _sourceType; } }
            public string LocationName { get { return _name; } }

            // FIXME: This audio specific method is a hack. After all, why should a
            // ResourceSource or a Resource (which uses this method) have audio
            // specific methods? But for now we keep this, as it eases transition.
            public virtual uint AudioCompressionType { get { return 0; } }

            public ResourceSource(ResSourceType type, string name, int volNum = 0, object resFile = null)
            {
                _sourceType = type;
                _name = name;
                _volumeNumber = volNum;
                _resourceFile = resFile;
            }

            public virtual void ScanSource(ResourceManager resMan)
            {
            }

            public virtual ResourceSource FindVolume(ResourceSource map, int volNum)
            {
                return null;
            }

            /// <summary>
            /// Auxiliary method, used by loadResource implementations.
            /// </summary>
            /// <param name="resMan"></param>
            /// <param name="res"></param>
            /// <returns></returns>
            public Stream GetVolumeFile(ResourceManager resMan, Resource res)
            {
                var fileStream = resMan.GetVolumeFile(this);
                if (fileStream == null)
                {
                    Warning($"Failed to open {LocationName}");
                    if (res != null)
                        res.Unalloc();
                }

                return fileStream;
            }

            /// <summary>
            /// Load a resource.
            /// </summary>
            /// <param name="resMan"></param>
            /// <param name="res"></param>
            public virtual void LoadResource(ResourceManager resMan, Resource res)
            {
                var fileStream = GetVolumeFile(resMan, res);
                if (fileStream == null)
                    return;

                fileStream.Seek(res._fileOffset, SeekOrigin.Begin);

                var error = res.Decompress(resMan._volVersion, fileStream);
                if (error != ResourceErrorCodes.NONE)
                {
                    //Warning($"Error {error} occurred while reading {res._id} from resource file %s: %s",
                    //res.ResourceLocation,
                    //s_errorDescriptions[error]);
                    res.Unalloc();
                }

                if (_resourceFile != null)
                    fileStream.Dispose();
            }

        }

        internal void SetAudioLanguage(short language)
        {
            throw new NotImplementedException();
        }

        class DirectoryResourceSource : ResourceSource
        {
            public DirectoryResourceSource(string name)
                    : base(ResSourceType.Directory, name)
            {
            }

            public override void ScanSource(ResourceManager resMan)
            {
                resMan.ReadResourcePatches();

                // We can't use getSciVersion() at this point, thus using _volVersion
                if (resMan._volVersion >= ResVersion.Sci11)    // SCI1.1+
                    resMan.ReadResourcePatchesBase36();

                resMan.ReadWaveAudioPatches();
            }
        }

        class VolumeResourceSource : ResourceSource
        {
            private ResourceSource _associatedMap;

            public VolumeResourceSource(string name, ResourceSource map, int volNum, ResSourceType type = ResSourceType.Volume)
                : base(type, name, volNum)
            {
                _associatedMap = map;
            }

            public VolumeResourceSource(string name, ResourceSource map, int volNum, object resFile)
                : base(ResSourceType.Volume, name, volNum, resFile)
            {
                _associatedMap = map;
            }

            public override ResourceSource FindVolume(ResourceSource map, int volNum)
            {
                if (_associatedMap == map && _volumeNumber == volNum)
                    return this;
                return null;
            }
        }

        class PatchResourceSource : ResourceSource
        {
            public PatchResourceSource(string name) : base(ResSourceType.Patch, name) { }

            public override void LoadResource(ResourceManager resMan, Resource res)
            {
                bool result = res.LoadFromPatchFile();
                if (!result)
                {
                    // TODO: We used to fallback to the "default" code here if loadFromPatchFile
                    // failed, but I am not sure whether that is really appropriate.
                    // In fact it looks like a bug to me, so I commented this out for now.
                    //ResourceSource::loadResource(res);
                }
            }
        }

        class ExtMapResourceSource : ResourceSource
        {
            public ExtMapResourceSource(string name, int volNum, object resFile = null)
                : base(ResSourceType.ExtMap, name, volNum, resFile)
            {
            }

            public override void ScanSource(ResourceManager resMan)
            {
                if (resMan._mapVersion < ResVersion.Sci1Late)
                    resMan.ReadResourceMapSCI0(this);
                else
                    resMan.ReadResourceMapSCI1(this);
            }
        }

        class IntMapResourceSource : ResourceSource
        {
            public IntMapResourceSource(string name, int volNum)
            : base(ResSourceType.IntMap, name, volNum)
            {
            }

            public override void ScanSource(ResourceManager resMan)
            {
                resMan.ReadAudioMapSCI11(this);
            }
        }

        class AudioVolumeResourceSource : VolumeResourceSource
        {
            protected uint _audioCompressionType;
            protected int[] _audioCompressionOffsetMapping;

            public override uint AudioCompressionType { get { return _audioCompressionType; } }

            public AudioVolumeResourceSource(ResourceManager resMan, string name, ResourceSource map, int volNum)
                : base(name, map, volNum, ResSourceType.AudioVolume)
            {
                _audioCompressionType = 0;
                _audioCompressionOffsetMapping = null;

                /*
                 * Check if this audio volume got compressed by our tool. If that is the
                 * case, set _audioCompressionType and read in the offset translation
                 * table for later usage.
                 */

                var fileStream = GetVolumeFile(resMan, null);
                var br = new BinaryReader(fileStream);
                if (fileStream == null)
                    return;

                fileStream.Seek(0, SeekOrigin.Begin);
                uint compressionType = br.ReadUInt32BigEndian();
                if (compressionType == ScummHelper.MakeTag('M', 'P', '3', ' ') ||
                    compressionType == ScummHelper.MakeTag('O', 'G', 'G', ' ') ||
                    compressionType == ScummHelper.MakeTag('F', 'L', 'A', 'C'))
                {
                    // Detected a compressed audio volume
                    _audioCompressionType = compressionType;
                    // Now read the whole offset mapping table for later usage
                    int recordCount = br.ReadInt32();
                    if (recordCount == 0)
                        Error("compressed audio volume doesn't contain any entries");
                    var offsetMapping = new int[(recordCount + 1) * 2];
                    var i = 0;
                    _audioCompressionOffsetMapping = offsetMapping;
                    for (int recordNo = 0; recordNo < recordCount; recordNo++)
                    {
                        offsetMapping[i++] = br.ReadInt32();
                        offsetMapping[i++] = br.ReadInt32();
                    }
                    // Put ending zero
                    offsetMapping[i++] = 0;
                    offsetMapping[i++] = (int)fileStream.Length;
                }

                if (_resourceFile != null)
                    fileStream.Dispose();
            }

            public override void LoadResource(ResourceManager resMan, Resource res)
            {
                var fileStream = GetVolumeFile(resMan, res);
                if (fileStream == null)
                    return;

                if (_audioCompressionType != 0)
                {
                    // this file is compressed, so lookup our offset in the offset-translation table and get the new offset
                    //  also calculate the compressed size by using the next offset
                    var mappingTable = new Int32Ptr(_audioCompressionOffsetMapping);
                    var compressedOffset = 0;

                    do
                    {
                        if (mappingTable.Value == res._fileOffset)
                        {
                            mappingTable.Offset++;
                            compressedOffset = mappingTable.Value;
                            // Go to next compressed offset and use that to calculate size of compressed sample
                            switch (res.ResourceType)
                            {
                                case ResourceType.Sync:
                                case ResourceType.Sync36:
                                case ResourceType.Rave:
                                    // we should already have a (valid) size
                                    break;
                                default:
                                    mappingTable.Offset += 2;
                                    res.size = mappingTable.Value - compressedOffset;
                                    break;
                            }
                            break;
                        }
                        mappingTable.Offset += 2;
                    } while (mappingTable.Value != 0);

                    if (compressedOffset == 0)
                        Error("could not translate offset to compressed offset in audio volume");
                    fileStream.Seek(compressedOffset, SeekOrigin.Begin);

                    switch (res.ResourceType)
                    {
                        case ResourceType.Audio:
                        case ResourceType.Audio36:
                            // Directly read the stream, compressed audio wont have resource type id and header size for SCI1.1
                            res.LoadFromAudioVolumeSCI1(fileStream);
                            if (_resourceFile != null)
                                fileStream.Dispose();
                            return;
                    }
                }
                else {
                    System.Diagnostics.Debug.Assert(fileStream.Length == -1 || res._fileOffset < fileStream.Length);
                    // original file, directly seek to given offset and get SCI1/SCI1.1 audio resource
                    fileStream.Seek(res._fileOffset, SeekOrigin.Begin);
                }
                if (GetSciVersion() < SciVersion.V1_1)
                    res.LoadFromAudioVolumeSCI1(fileStream);
                else
                    res.LoadFromAudioVolumeSCI11(fileStream);

                if (_resourceFile != null)
                    fileStream.Dispose();
            }
        }

        public bool DetectEarlySound()
        {
            var res = FindResource(new ResourceId(ResourceType.Sound, 1), false);
            if (res != null)
            {
                if (res.size >= 0x22)
                {
                    if (res.data.ToUInt16(0x1f) == 0) // channel 15 voice count + play mask is 0 in SCI0LATE
                        if (res.data[0x21] == 0) // last byte right before actual data is 0 as well
                            return false;
                }
            }
            return true;
        }
    }
}