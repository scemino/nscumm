//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOßR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#if ENABLE_SCI32

using System;
using NScumm.Core;
using NScumm.Core.Graphics;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    internal class CelObjPic : CelObj
    {
        /// <summary>
        /// The number of cels in the original picture resource.
        /// </summary>
        public readonly byte _celCount;

        /// <summary>
        /// The position of this cel relative to the top-left
        /// corner of the picture.
        /// </summary>
        public Point _relativePosition;

        /// <summary>
        /// The z-buffer priority for this cel. Higher prorities
        /// are drawn on top of lower priorities.
        /// </summary>
        public readonly short _priority;

        private CelObjPic(CelObjPic src)
            : base(src)
        {
            _celCount = src._celCount;
            _priority = src._priority;
            _relativePosition = src._relativePosition;
        }

        public static CelObjPic Create(int picId, short celNo)
        {
            var info = new CelInfo32
            {
                type = CelType.Pic,
                resourceId = picId,
                loopNo = 0,
                celNo = celNo
            };

            int cacheInsertIndex;
            var cacheIndex = SearchCache(ref info, out cacheInsertIndex);
            if (cacheIndex == -1) return new CelObjPic(info);

            var entry = _cache[cacheIndex];
            var cachedCelObj = (CelObjPic) entry.celObj;
            if (cachedCelObj == null)
            {
                Error("Expected a CelObjPic in cache slot {0}", cacheIndex);
            }
            var celObjCopy = new CelObjPic(cachedCelObj);
            entry.id = ++_nextCacheId;
            return celObjCopy;
        }

        private CelObjPic(CelInfo32 info)
        {
            _info = info;
            _mirrorX = false;
            _compressionType = CelCompressionType.Invalid;
            _transparent = true;
            _remap = false;

            int cacheInsertIndex;
            var cacheIndex = SearchCache(ref _info, out cacheInsertIndex);
            if (cacheIndex != -1)
            {
                throw new InvalidOperationException("This item is not expected to be in cache.");
            }

            var resource =
                SciEngine.Instance.ResMan.FindResource(new ResourceId(ResourceType.Pic, (ushort) info.resourceId), false);

            // NOTE: SCI2.1/SQ6 just silently returns here.
            if (resource == null)
            {
                Warning("Pic resource {0} not loaded", info.resourceId);
                return;
            }

            var data = resource.data;

            _celCount = data[2];

            if (_info.celNo >= _celCount)
            {
                Error("Cel number {0} greater than cel count {1}", _info.celNo, _celCount);
            }

            _celHeaderOffset = data.ReadSci11EndianUInt16() + (data.ReadSci11EndianUInt16(4) * _info.celNo);
            _hunkPaletteOffset = (int) data.ReadSci11EndianUInt32(6);

            var celHeader = new BytePtr(data, _celHeaderOffset);

            _width = celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset);
            _height = celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset + 2);
            _origin.X = (short) celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset + 4);
            _origin.Y = (short) celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset + 6);
            _skipColor = celHeader[8];
            _compressionType = (CelCompressionType) celHeader[9];
            _priority = (short) celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset + 36);
            _relativePosition.X = (short) celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset + 38);
            _relativePosition.Y = (short) celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset + 40);

            var sizeFlag1 = data.ReadSci11EndianUInt16(10);
            var sizeFlag2 = data.ReadSci11EndianUInt16(12);

            if (sizeFlag2 != 0)
            {
                _xResolution = sizeFlag1;
                _yResolution = sizeFlag2;
            }
            else if (sizeFlag1 == 0)
            {
                _xResolution = LowRes.X;
                _yResolution = LowRes.Y;
            }
            else if (sizeFlag1 == 1)
            {
                _xResolution = 640;
                _yResolution = 480;
            }
            else if (sizeFlag1 == 2)
            {
                _xResolution = 640;
                _yResolution = 400;
            }

            if ((celHeader[10] & 128) != 0)
            {
                // NOTE: This is correct according to SCI2.1/SQ6/DOS;
                // the engine re-reads the byte value as a word value
                var flags = celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset + 10);
                _transparent = (flags & 1) != 0;
                _remap = (flags & 2) != 0;
            }
            else
            {
                _transparent = _compressionType != CelCompressionType.None ? true : AnalyzeUncompressedForSkip();

                if (_compressionType != CelCompressionType.None && _compressionType != CelCompressionType.RLE)
                {
                    Error("Compression type not supported - P: {0}  C: {1}", info.resourceId, info.celNo);
                }
            }

            PutCopyInCache(cacheInsertIndex);
        }

        private bool AnalyzeUncompressedForSkip()
        {
            var resource = GetResPointer();
            var pixels = new BytePtr(resource,
                (int) resource.Data.ReadSci11EndianUInt32((int) (resource.Offset + _celHeaderOffset + 24)));
            for (var i = 0; i < _width * _height; ++i)
            {
                var pixel = pixels[i];
                if (pixel == _skipColor)
                {
                    return true;
                }
            }
            return false;
        }

        public override CelObj Duplicate()
        {
            return new CelObjPic(this);
        }

        public override BytePtr GetResPointer()
        {
            var resource =
                SciEngine.Instance.ResMan.FindResource(new ResourceId(ResourceType.Pic, (ushort) _info.resourceId),
                    false);
            if (resource == null)
            {
                Error("Failed to load pic {0} from resource manager", _info.resourceId);
            }
            return resource.data;
        }
    }
}

#endif