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
using System.Linq;
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci
{
    internal enum ResSourceType
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
    internal enum ResourceStatus
    {
        NoMalloc = 0,
        Allocated = 1,

        /// <summary>
        /// In the LRU queue.
        /// </summary>
        Enqueued = 2,

        /// <summary>
        /// Allocated and in use 
        /// </summary>
        Locked = 3
    }

    internal partial class ResourceManager
    {
        protected class ResourceMap : Dictionary<ResourceId, ResourceSource.Resource>
        {
        }

        public void SetAudioLanguage(short language)
        {
            if (_audioMapSCI1 != null)
            {
                if (_audioMapSCI1._volumeNumber == language)
                {
                    // This language is already loaded
                    return;
                }

                // We already have a map loaded, so we unload it first
                ReadAudioMapSCI1(_audioMapSCI1, true);

                // Remove all volumes that use this map from the source list
                foreach (var it in _sources.ToList())
                {
                    var src = it;
                    if (src.FindVolume(_audioMapSCI1, src._volumeNumber) != null)
                    {
                        _sources.Remove(it);
                    }
                }

                // Remove the map itself from the source list
                _sources.Remove(_audioMapSCI1);

                _audioMapSCI1 = null;
            }

            string filename = $"AUDIO{language:D3}";

            var fullname = filename + ".MAP";
            var f = Core.Engine.OpenFileRead(fullname);
            if (f == null)
            {
                Warning("No audio map found for language {0}", language);
                return;
            }

            f.Dispose();
            _audioMapSCI1 = AddSource(new ExtAudioMapResourceSource(fullname, language));

            // Search for audio volumes for this language and add them to the source list
            var files = ServiceLocator.FileStorage.EnumerateFiles(SciEngine.Instance.Directory, filename + ".0??");
            foreach (var name in files)
            {
                var number = int.Parse(name.Substring(name.Length - 3, 3));

                AddSource(new AudioVolumeResourceSource(this, name, _audioMapSCI1, number));
            }

            ScanNewSources();
        }

        public ResourceErrorCodes ReadAudioMapSCI1(ResourceSource map, bool unload = false)
        {
            var file = Core.Engine.OpenFileRead(map.LocationName);

            if (file == null)
                return ResourceErrorCodes.RESMAP_NOT_FOUND;

            using (var br = new BinaryReader(file))
            {
                var oldFormat = br.ReadUInt16() >> 11 == (int) ResourceType.Audio;
                file.Seek(0, SeekOrigin.Begin);

                while (true)
                {
                    var n = br.ReadUInt16();
                    var offset = br.ReadUInt32();
                    var size = br.ReadInt32();

                    if (file.Position == file.Length)
                    {
                        Warning("Error while reading {0}", map.LocationName);
                        return ResourceErrorCodes.RESMAP_NOT_FOUND;
                    }

                    if (n == 0xffff)
                        break;

                    byte volumeNr;

                    if (oldFormat)
                    {
                        n &= 0x07ff; // Mask out resource type
                        volumeNr = (byte) (offset >> 25); // most significant 7 bits
                        offset &= 0x01ffffff; // least significant 25 bits
                    }
                    else
                    {
                        volumeNr = (byte) (offset >> 28); // most significant 4 bits
                        offset &= 0x0fffffff; // least significant 28 bits
                    }

                    var src = FindVolume(map, volumeNr);

                    if (src != null)
                    {
                        if (unload)
                            RemoveAudioResource(new ResourceId(ResourceType.Audio, n));
                        else
                            AddResource(new ResourceId(ResourceType.Audio, n), src, offset, size);
                    }
                    else
                    {
                        Warning("Failed to find audio volume {0}", volumeNr);
                    }
                }
            }

            return 0;
        }

        private void RemoveAudioResource(ResourceId resId)
        {
            // Remove resource, unless it was loaded from a patch
            if (_resMap.ContainsKey(resId))
            {
                var res = _resMap[resId];

                if (res._source.SourceType == ResSourceType.AudioVolume)
                {
                    if (res._status == ResourceStatus.Locked)
                    {
                        Warning("Failed to remove resource {0} (still in use)", resId);
                    }
                    else
                    {
                        if (res._status == ResourceStatus.Enqueued)
                            RemoveFromLRU(res);

                        _resMap.Remove(resId);
                    }
                }
            }
        }

        private class DirectoryResourceSource : ResourceSource
        {
            public DirectoryResourceSource(string name)
                : base(ResSourceType.Directory, name)
            {
            }

            public override void ScanSource(ResourceManager resMan)
            {
                resMan.ReadResourcePatches();

                // We can't use getSciVersion() at this point, thus using _volVersion
                if (resMan._volVersion >= ResVersion.Sci11) // SCI1.1+
                    resMan.ReadResourcePatchesBase36();

                resMan.ReadWaveAudioPatches();
            }
        }

        private class PatchResourceSource : ResourceSource
        {
            public PatchResourceSource(string name) : base(ResSourceType.Patch, name)
            {
            }

            public override void LoadResource(ResourceManager resMan, Resource res)
            {
                var result = res.LoadFromPatchFile();
                if (!result)
                {
                    // TODO: We used to fallback to the "default" code here if loadFromPatchFile
                    // failed, but I am not sure whether that is really appropriate.
                    // In fact it looks like a bug to me, so I commented this out for now.
                    //ResourceSource::loadResource(res);
                }
            }
        }

        private class ExtMapResourceSource : ResourceSource
        {
            public ExtMapResourceSource(string name, int volNum, string resFile = null)
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

        private class IntMapResourceSource : ResourceSource
        {
            public readonly ushort _mapNumber;

            public IntMapResourceSource(string name, int volNum, int mapNum)
                : base(ResSourceType.IntMap, name, volNum)
            {
                _mapNumber = (ushort) mapNum;
            }

            public override void ScanSource(ResourceManager resMan)
            {
                resMan.ReadAudioMapSCI11(this);
            }
        }

        private class AudioVolumeResourceSource : VolumeResourceSource
        {
            protected readonly uint _audioCompressionType;
            protected readonly int[] _audioCompressionOffsetMapping;

            public override uint AudioCompressionType => _audioCompressionType;

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

                fileStream.Seek(0, SeekOrigin.Begin);
                var compressionType = br.ReadUInt32BigEndian();
                if (compressionType == ScummHelper.MakeTag('M', 'P', '3', ' ') ||
                    compressionType == ScummHelper.MakeTag('O', 'G', 'G', ' ') ||
                    compressionType == ScummHelper.MakeTag('F', 'L', 'A', 'C'))
                {
                    // Detected a compressed audio volume
                    _audioCompressionType = compressionType;
                    // Now read the whole offset mapping table for later usage
                    var recordCount = br.ReadInt32();
                    if (recordCount == 0)
                        Error("compressed audio volume doesn't contain any entries");
                    var offsetMapping = new int[(recordCount + 1) * 2];
                    var i = 0;
                    _audioCompressionOffsetMapping = offsetMapping;
                    for (var recordNo = 0; recordNo < recordCount; recordNo++)
                    {
                        offsetMapping[i++] = br.ReadInt32();
                        offsetMapping[i++] = br.ReadInt32();
                    }
                    // Put ending zero
                    offsetMapping[i++] = 0;
                    offsetMapping[i++] = (int) fileStream.Length;
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
                else
                {
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

        /// <summary>
        /// Reads resources from SCI2.1+ chunk resources
        /// </summary>
        private class ChunkResourceSource : ResourceSource
        {
            public ChunkResourceSource(string name, ushort number)
                : base(ResSourceType.Chunk, name)
            {
                // Chunk resources are resources that hold other resources. They are normally called
                // when using the kLoadChunk SCI2.1 kernel function. However, for example, the Lighthouse
                // SCI2.1 demo has a chunk but no scripts outside of the chunk.

                // A chunk resource is pretty straightforward in terms of layout
                // It begins with 11-byte entries in the header:
                // =========
                // b resType
                // w nEntry
                // dw offset
                // dw length
            }

            public override void ScanSource(ResourceManager resMan)
            {
                var chunk = resMan.FindResource(new ResourceId(ResourceType.Chunk, _number), false);

                if (chunk == null)
                    Error("Trying to load non-existent chunk");

                var ptr = new BytePtr(chunk.data);
                var firstOffset = 0;

                for (;;)
                {
                    var type = resMan.ConvertResType(ptr.Value);
                    var number = ptr.ToUInt16(1);
                    var id = new ResourceId(type, number);

                    var entry = new ResourceEntry
                    {
                        offset = ptr.ToInt32(3),
                        length = ptr.ToInt32(7)
                    };

                    _resMap[id] = entry;
                    ptr.Offset += 11;

                    DebugC(DebugLevels.ResMan, 2, "Found {0} in chunk {1}", id, _number);

                    resMan.UpdateResource(id, this, entry.length);

                    // There's no end marker to the data table, but the first resource
                    // begins directly after the entry table. So, when we hit the first
                    // resource, we're at the end of the entry table.

                    if (firstOffset == 0)
                        firstOffset = entry.offset;

                    if (ptr.Offset >= firstOffset)
                        break;
                }
            }

            public override void LoadResource(ResourceManager resMan, Resource res)
            {
                var chunk = resMan.FindResource(new ResourceId(ResourceType.Chunk, _number), false);

                if (!_resMap.ContainsKey(res._id))
                    Error("Trying to load non-existent resource from chunk {0}: {1} {2}", _number,
                        GetResourceTypeName(res._id.Type), res._id.Number);

                var entry = _resMap[res._id];
                res.data = new byte[entry.length];
                res.size = entry.length;
                res._header = null;
                res._headerSize = 0;
                res._status = ResourceStatus.Allocated;

                // Copy the resource data over
                Array.Copy(chunk.data, entry.offset, res.data, 0, entry.length);
            }

            protected ushort _number;

            private class ResourceEntry
            {
                public int offset;
                public int length;
            }

            private Dictionary<ResourceId, ResourceEntry> _resMap;
        }

        public bool DetectEarlySound()
        {
            var res = FindResource(new ResourceId(ResourceType.Sound, 1), false);
            if (!(res?.size >= 0x22)) return true;

            if (res.data.ToUInt16(0x1f) != 0) return true;
            return res.data[0x21] != 0;
        }
    }
}