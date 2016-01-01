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
using NScumm.Sci.Engine;

namespace NScumm.Sci
{
    enum ResSourceType
    {
        Directory = 0,   ///< Directories containing game resources/patches
        Patch,           ///< External resource patches
        Volume,          ///< Game resources (resource.* or ressci.*)
        ExtMap,          ///< Non-audio resource maps
        IntMap,          ///< SCI1.1 and later audio resource maps
        AudioVolume,     ///< Audio resources - resource.sfx / resource.aud
        ExtAudioMap,     ///< SCI1 audio resource maps
        Wave,            ///< External WAVE files, patched in as sound resources
        MacResourceFork, ///< Mac SCI1.1 and later resource forks
        Chunk            ///< Script chunk resources (*.chk)
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

                public ResourceType ResourceType { get { return _id.Type; } }
                public ushort Number { get { return _id.Number; } }

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
                            szPacked = (int)br.ReadUInt16() - 4;
                            szUnpacked = br.ReadUInt16();
                            wCompression = br.ReadUInt16();
                            break;
                        case ResVersion.Sci1Late:
                            type = _resMan.ConvertResType(file.ReadByte());
                            number = br.ReadUInt16();
                            szPacked = (int)br.ReadUInt16() - 4;
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
                            type = _resMan.ConvertResType(file->readByte());
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
                            throw new NotImplementedException();
                            //dec = new DecompressorHuffman();
                            break;
                        case ResourceCompression.LZW:
                        case ResourceCompression.LZW1:
                        case ResourceCompression.LZW1View:
                        case ResourceCompression.LZW1Pic:
                            dec = new DecompressorLZW(compression);
                            break;
                        case ResourceCompression.DCL:
                            throw new NotImplementedException();
                            //dec = new DecompressorDCL();
                            break;
#if ENABLE_SCI32
                        case ResourceCompression.STACpack:
                            dec = new DecompressorLZS;
                            break;
#endif
                        default:
                            // TODO: error("Resource %s: Compression method %d not supported", _id.toString().c_str(), compression);
                            return ResourceErrorCodes.UNKNOWN_COMPRESSION;
                    }

                    data = new byte[size];
                    _status = ResourceStatus.Allocated;
                    errorNum = data != null ? dec.Unpack(file, data, szPacked, size) : ResourceErrorCodes.RESOURCE_TOO_BIG;
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
                    // TODO/ warning("Failed to open %s", getLocationName().c_str());
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
                    // TODO: warning("Error %d occurred while reading %s from resource file %s: %s",
                    //error, res->_id.toString().c_str(), res->getResourceLocation().c_str(),
                    //s_errorDescriptions[error]);
                    res.Unalloc();
                }

                if (_resourceFile != null)
                    fileStream.Dispose();
            }

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

            public virtual uint AudioCompressionType { get { return _audioCompressionType; } }

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

                throw new NotImplementedException();
                //var fileStream = getVolumeFile(resMan, 0);
                //if (!fileStream)
                //    return;

                //fileStream->seek(0, SEEK_SET);
                //uint32 compressionType = fileStream->readUint32BE();
                //switch (compressionType)
                //{
                //    case MKTAG('M', 'P', '3', ' '):
                //    case MKTAG('O', 'G', 'G', ' '):
                //    case MKTAG('F', 'L', 'A', 'C'):
                //        // Detected a compressed audio volume
                //        _audioCompressionType = compressionType;
                //        // Now read the whole offset mapping table for later usage
                //        int32 recordCount = fileStream->readUint32LE();
                //        if (!recordCount)
                //            error("compressed audio volume doesn't contain any entries");
                //        int32* offsetMapping = new int32[(recordCount + 1) * 2];
                //        _audioCompressionOffsetMapping = offsetMapping;
                //        for (int recordNo = 0; recordNo < recordCount; recordNo++)
                //        {
                //            *offsetMapping++ = fileStream->readUint32LE();
                //            *offsetMapping++ = fileStream->readUint32LE();
                //        }
                //        // Put ending zero
                //        *offsetMapping++ = 0;
                //        *offsetMapping++ = fileStream->size();
                //}

                //if (_resourceFile)
                //    delete fileStream;
            }

            public virtual void LoadResource(ResourceManager resMan, Resource res)
            {
                throw new NotImplementedException();
            }
        }
    }
}