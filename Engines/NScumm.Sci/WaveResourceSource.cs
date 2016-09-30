//
//  WaveResourceSource.cs
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci
{
    internal class WaveResourceSource : ResourceManager.ResourceSource
    {
        public WaveResourceSource(string name) : base(ResSourceType.Wave, name)
        {
        }

        public override void LoadResource(ResourceManager resMan, Resource res)
        {
            var fileStream = GetVolumeFile(resMan, res);
            if (fileStream == null)
                return;

            System.Diagnostics.Debug.Assert(fileStream.Length == -1 || res._fileOffset < fileStream.Length);
            fileStream.Seek(res._fileOffset, SeekOrigin.Begin);
            res.LoadFromWaveFile(fileStream);
            if (_resourceFile != null)
                fileStream.Dispose();
        }
    }

    /// <summary>
    /// Reads SCI1.1+ resources from a Mac resource fork.
    /// </summary>
    internal class MacResourceForkResourceSource : ResourceManager.ResourceSource
    {
        private static readonly Dictionary<uint, ResourceType> MacResTagMap = new Dictionary<uint, ResourceType>{
            { ScummHelper.MakeTag('V','5','6',' '), ResourceType.View },
            { ScummHelper.MakeTag('P','5','6',' '), ResourceType.Pic },
            { ScummHelper.MakeTag('S','C','R',' '), ResourceType.Script },
            { ScummHelper.MakeTag('T','E','X',' '), ResourceType.Text },
            { ScummHelper.MakeTag('S','N','D',' '), ResourceType.Sound },
            { ScummHelper.MakeTag('V','O','C',' '), ResourceType.Vocab },
            { ScummHelper.MakeTag('F','O','N',' '), ResourceType.Font },
            { ScummHelper.MakeTag('C','U','R','S'), ResourceType.Cursor },
            { ScummHelper.MakeTag('c','r','s','r'), ResourceType.Cursor },
            { ScummHelper.MakeTag('P','a','t',' '), ResourceType.Patch },
            { ScummHelper.MakeTag('P','A','L',' '), ResourceType.Palette },
            { ScummHelper.MakeTag('s','n','d',' '), ResourceType.Audio },
            { ScummHelper.MakeTag('M','S','G',' '), ResourceType.Message },
            { ScummHelper.MakeTag('H','E','P',' '), ResourceType.Heap },
            { ScummHelper.MakeTag('I','B','I','N'), ResourceType.MacIconBarPictN },
            { ScummHelper.MakeTag('I','B','I','S'), ResourceType.MacIconBarPictS },
            { ScummHelper.MakeTag('P','I','C','T'), ResourceType.MacPict },
            { ScummHelper.MakeTag('S','Y','N',' '), ResourceType.Sync },
            { ScummHelper.MakeTag('S','Y','N','C'), ResourceType.Sync }
        };

        private readonly MacResManager _macResMan;

        public MacResourceForkResourceSource(string name, int volNum)
            : base(ResSourceType.MacResourceFork, name, volNum)
        {
            _macResMan = new MacResManager();
        }

        public override void ScanSource(ResourceManager resMan)
        {
            if (!_macResMan.Open(LocationName))
                Error("{0} is not a valid Mac resource fork", LocationName);

            var tagArray = _macResMan.GetResTagArray();

            foreach (var tag in tagArray)
            {
                var type = ResourceType.Invalid;

                // Map the Mac tags to our ResourceType
                for (var j = 0; j < MacResTagMap.Count; j++)
                {
                    if (!MacResTagMap.ContainsKey(tag)) continue;

                    type = MacResTagMap[tagArray[j]];
                    break;
                }

                if (type == ResourceType.Invalid)
                    continue;

                var idArray = _macResMan.GetResIDArray(tag);

                foreach (var id in idArray)
                {
                    ResourceId resId;

                    // Check to see if we've got a base36 encoded resource name
                    if (type == ResourceType.Audio)
                    {
                        var resourceName = _macResMan.GetResName(tag, id);

                        // If we have a file name on an audio resource, we've got an audio36
                        // resource. Parse the file name to get the id.
                        if (!string.IsNullOrEmpty(resourceName) && resourceName[0] == '@')
                            resId = ConvertPatchNameBase36(ResourceType.Audio36, resourceName);
                        else
                            resId = new ResourceId(type, id);
                    }
                    else if (type == ResourceType.Sync)
                    {
                        var resourceName = _macResMan.GetResName(tag, id);

                        // Same as with audio36 above
                        if (!string.IsNullOrEmpty(resourceName) && resourceName[0] == '#')
                            resId = ConvertPatchNameBase36(ResourceType.Sync36, resourceName);
                        else
                            resId = new ResourceId(type, id);
                    }
                    else {
                        // Otherwise, we're just going with the id that was given
                        resId = new ResourceId(type, id);
                    }

                    // Overwrite Resource instance. Resource forks may contain patches.
                    // The size will be filled in later by decompressResource()
                    resMan.UpdateResource(resId, this, 0);
                }
            }
        }

        private static ResourceId ConvertPatchNameBase36(ResourceType type, string filename)
        {
            // The base36 encoded resource contains the following:
            // uint16 resourceId, byte noun, byte verb, byte cond, byte seq

            // Skip patch type character

            var resourceNr = System.Convert.ToUInt16(filename.Substring(1, 3), 36); // 3 characters
            var noun = System.Convert.ToUInt16(filename.Substring(4, 2), 36);       // 2 characters
            var verb = System.Convert.ToUInt16(filename.Substring(6, 2), 36);       // 2 characters
                                                                                       // Skip '.'
            var cond = System.Convert.ToUInt16(filename.Substring(9, 2), 36);       // 2 characters
            var seq = System.Convert.ToUInt16(filename.Substring(11, 1), 36);       // 1 character

            return new ResourceId(type, resourceNr, (byte)noun, (byte)verb, (byte)cond, (byte)seq);
        }


        public override void LoadResource(ResourceManager resMan, Resource res)
        {
            var type = res.ResourceType;
            Stream stream = null;

            if (type == ResourceType.Audio36 || type == ResourceType.Sync36)
            {
                // Handle audio36/sync36, convert back to audio/sync
                stream = _macResMan.GetResource(res._id.ToPatchNameBase36());
            }
            else {
                // Plain resource handling
                var tagArray = ResTypeToMacTags(type).ToArray();

                for (var i = 0; i < tagArray.Length && stream == null; i++)
                    stream = _macResMan.GetResource(tagArray[i], res.Number);
            }

            if (stream != null)
                DecompressResource(stream, res);
        }



        private static IEnumerable<uint> ResTypeToMacTags(ResourceType type)
        {
            foreach (var entry in MacResTagMap)
                if (entry.Value == type)
                    yield return entry.Key;
        }

        private bool IsCompressableResource(ResourceType type)
        {
            // Any types that were not originally an SCI format are not compressed, it seems.
            // (Audio/36 being Mac snd resources here)
            return type != ResourceType.MacPict && type != ResourceType.Audio &&
                    type != ResourceType.MacIconBarPictN && type != ResourceType.MacIconBarPictS &&
                    type != ResourceType.Audio36 && type != ResourceType.Sync &&
                    type != ResourceType.Sync36 && type != ResourceType.Cursor;
        }

        private void DecompressResource(Stream stream, Resource resource)
        {
            var br = new BinaryReader(stream);

            // KQ6 Mac is the only game not compressed. It's not worth writing a
            // heuristic just for that game. Also, skip over any resource that cannot
            // be compressed.
            var canBeCompressed = !(SciEngine.Instance != null && SciEngine.Instance.GameId == SciGameId.KQ6) && IsCompressableResource(resource._id.Type);
            uint uncompressedSize = 0;

            // GK2 Mac is crazy. In its Patches resource fork, picture 2315 is not
            // compressed and it is hardcoded in the executable to say that it's
            // not compressed. Why didn't they just add four zeroes to the end of
            // the resource? (Checked with PPC disasm)
            if (SciEngine.Instance != null && SciEngine.Instance.GameId == SciGameId.GK2 && resource._id.Type == ResourceType.Pic && resource._id.Number == 2315)
                canBeCompressed = false;

            // Get the uncompressed size from the end of the resource
            if (canBeCompressed && stream.Length > 4)
            {
                stream.Seek(stream.Length - 4, SeekOrigin.Begin);
                uncompressedSize = br.ReadUInt32BigEndian();
                stream.Seek(0, SeekOrigin.Begin);
            }

            if (uncompressedSize == 0)
            {
                // Not compressed
                resource.size = (int)stream.Length;

                // Cut out the 'non-compressed marker' (four zeroes) at the end
                if (canBeCompressed)
                    resource.size -= 4;

                resource.data = new byte[resource.size];
                stream.Read(resource.data, 0, resource.size);
            }
            else {
                // Decompress
                resource.size = (int)uncompressedSize;
                resource.data = new byte[uncompressedSize];

                var ptr = new BytePtr(resource.data);

                while (stream.Position < stream.Length)
                {
                    var code = br.ReadByte();

                    int literalLength, offset, copyLength;
                    byte extraByte1;

                    if (code == 0xFF)
                    {
                        // End of stream marker
                        break;
                    }

                    switch (code & 0xC0)
                    {
                        case 0x80:
                            // Copy chunk expanded
                            extraByte1 = br.ReadByte();
                            var extraByte2 = br.ReadByte();

                            literalLength = extraByte2 & 3;

                            OUTPUT_LITERAL(br, ref ptr, ref literalLength);

                            offset = ((code & 0x3f) | ((extraByte1 & 0xe0) << 1) | ((extraByte2 & 0xfc) << 7)) + 1;
                            copyLength = (extraByte1 & 0x1f) + 3;

                            OUTPUT_COPY(ref ptr, offset, ref copyLength);
                            break;
                        case 0xC0:
                            // Literal chunk
                            if (code >= 0xD0)
                            {
                                // These codes cannot be used
                                if (code == 0xD0 || code > 0xD3)
                                    Error("Bad Mac compression code %02x", code);

                                literalLength = code & 3;
                            }
                            else
                                literalLength = (code & 0xf) * 4 + 4;

                            OUTPUT_LITERAL(br, ref ptr, ref literalLength);
                            break;
                        default:
                            // Copy chunk
                            extraByte1 = br.ReadByte();

                            literalLength = (extraByte1 >> 3) & 0x3;

                            OUTPUT_LITERAL(br, ref ptr, ref literalLength);

                            offset = (code + ((extraByte1 & 0xE0) << 2)) + 1;
                            copyLength = (extraByte1 & 0x7) + 3;

                            OUTPUT_COPY(ref ptr, offset, ref copyLength);
                            break;
                    }
                }
            }

            resource._status = ResourceStatus.Allocated;
        }

        private static void OUTPUT_LITERAL(BinaryReader br, ref BytePtr ptr, ref int length)
        {
            while (length-- != 0)
            {
                ptr.Value=br.ReadByte();
                ptr.Offset++;
            }
        }

        private static void OUTPUT_COPY(ref BytePtr ptr, int offset, ref int length)
        {
            while (length--!=0)
            {
                var value = ptr[-offset];
                ptr[0] = value;
                ptr.Offset++;
            }
        }
    }
}
