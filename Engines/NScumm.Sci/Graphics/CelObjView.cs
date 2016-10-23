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
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
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
    internal class CelObjView : CelObj
    {
        public static CelObjView Create(int viewId, short loopNo, short celNo)
        {
            var info = new CelInfo32
            {
                type = CelType.View,
                resourceId = viewId,
                loopNo = loopNo,
                celNo = celNo
            };

            int cacheInsertIndex;
            var cacheIndex = SearchCache(ref info, out cacheInsertIndex);
            if (cacheIndex == -1) return new CelObjView(info);

            var entry = _cache[cacheIndex];
            var cachedCelObj = (CelObjView) entry.celObj;
            if (cachedCelObj == null)
            {
                Error("Expected a CelObjView in cache slot {0}", cacheIndex);
            }
            var celObjCopy = new CelObjView(cachedCelObj);
            entry.id = ++_nextCacheId;
            return celObjCopy;
        }

        public void Draw(Buffer target, ref Rect targetRect, ref Point scaledPosition, bool mirrorX, Rational scaleX,
            Rational scaleY)
        {
            _drawMirrored = mirrorX;
            DrawTo(target, ref targetRect, ref scaledPosition, scaleX, scaleY);
        }

        private CelObjView(CelObjView src)
            : base(src)
        {
        }

        private CelObjView(CelInfo32 info)
        {
            _info = info;

            _mirrorX = false;
            _compressionType = CelCompressionType.Invalid;
            _transparent = true;

            // TODO: The next code should be moved to a common file that
            // generates view resource metadata for both SCI16 and SCI32
            // implementations

            var resource = SciEngine.Instance.ResMan.FindResource(
                new ResourceId(ResourceType.View, (ushort) _info.resourceId), false);

            // NOTE: SCI2.1/SQ6 just silently returns here.
            if (resource == null)
            {
                Error("View resource {0} not found", info.resourceId);
                return;
            }

            var data = resource.data;

            _xResolution = data.ReadSci11EndianUInt16(14);
            _yResolution = data.ReadSci11EndianUInt16(16);

            if (_xResolution == 0 || _yResolution == 0)
            {
                byte sizeFlag = data[5];
                if (sizeFlag == 0)
                {
                    _xResolution = LowRes.X;
                    _yResolution = LowRes.Y;
                }
                else if (sizeFlag == 1)
                {
                    _xResolution = 640;
                    _yResolution = 480;
                }
                else if (sizeFlag == 2)
                {
                    _xResolution = 640;
                    _yResolution = 400;
                }
            }

            ushort loopCount = data[2];
            if (_info.loopNo >= loopCount)
            {
                _info.loopNo = (short) (loopCount - 1);
            }

            // NOTE: This is the actual check, in the actual location,
            // from SCI engine.
            if (_info.loopNo < 0)
            {
                Error("Loop is less than 0!");
            }

            ushort viewHeaderSize = data.ReadSci11EndianUInt16();
            byte loopHeaderSize = data[12];
            byte viewHeaderFieldSize = 2;

            BytePtr loopHeader = new BytePtr(data, viewHeaderFieldSize + viewHeaderSize + loopHeaderSize * _info.loopNo);

            if ((sbyte) loopHeader[0] != -1)
            {
                if (loopHeader[1] == 1)
                {
                    _mirrorX = true;
                }

                loopHeader = new BytePtr(data,
                    viewHeaderFieldSize + viewHeaderSize + loopHeaderSize * (sbyte) loopHeader[0]);
            }

            byte celCount = loopHeader[2];
            if (_info.celNo >= celCount)
            {
                _info.celNo = (short) (celCount - 1);
            }

            _hunkPaletteOffset = (int) data.ReadSci11EndianUInt32(8);
            _celHeaderOffset =
                (int) (loopHeader.Data.ReadSci11EndianUInt32(loopHeader.Offset + 12) + (data[13] * _info.celNo));

            BytePtr celHeader = new BytePtr(data, _celHeaderOffset);

            _width = celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset);
            _height = celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset + 2);
            _displace.X = (short) (_width / 2 - (short) celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset + 4));
            _displace.Y = (short) (_height - (short) celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset + 6) - 1);
            _transparentColor = celHeader[8];
            _compressionType = (CelCompressionType) celHeader[9];

            if (_compressionType != CelCompressionType.None && _compressionType != CelCompressionType.RLE)
            {
                Error("Compression type not supported - V: {0}  L: {1}  C: {2}", _info.resourceId, _info.loopNo,
                    _info.celNo);
            }

            if ((celHeader[10] & 128) != 0)
            {
                // NOTE: This is correct according to SCI2.1/SQ6/DOS;
                // the engine re-reads the byte value as a word value
                ushort flags = celHeader.Data.ReadSci11EndianUInt16(celHeader.Offset + 10);
                _transparent = (flags & 1) != 0;
                _remap = (flags & 2) != 0;
            }
            else if (_compressionType == CelCompressionType.None)
            {
                _remap = AnalyzeUncompressedForRemap();
            }
            else
            {
                _remap = AnalyzeForRemap();
            }

            int cacheInsertIndex;
            SearchCache(ref _info, out cacheInsertIndex);
            PutCopyInCache(cacheInsertIndex);
        }

        private bool AnalyzeUncompressedForRemap()
        {
            BytePtr pixels = new BytePtr(GetResPointer(),
                (int) GetResPointer().ReadSci11EndianUInt32(_celHeaderOffset + 24));
            for (int i = 0; i < _width * _height; ++i)
            {
                byte pixel = pixels[i];
                if (
                    pixel >= SciEngine.Instance._gfxRemap32.StartColor &&
                    pixel <= SciEngine.Instance._gfxRemap32.EndColor &&
                    pixel != _transparentColor
                )
                {
                    return true;
                }
            }
            return false;
        }

        private bool AnalyzeForRemap()
        {
            var reader = new READER_Compressed(this, (short) _width);
            for (int y = 0; y < _height; y++)
            {
                var curRow = reader.GetRow((short) y);
                for (int x = 0; x < _width; x++)
                {
                    byte pixel = curRow[x];
                    if (
                        pixel >= SciEngine.Instance._gfxRemap32.StartColor &&
                        pixel <= SciEngine.Instance._gfxRemap32.EndColor &&
                        pixel != _transparentColor
                    )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override CelObj Duplicate()
        {
            return new CelObjView(this);
        }

        public override BytePtr GetResPointer()
        {
            ResourceManager.ResourceSource.Resource resource = SciEngine.Instance.ResMan.FindResource(
                new ResourceId(ResourceType.View, (ushort) _info.resourceId), false);
            if (resource == null)
            {
                Error("Failed to load view {0} from resource manager", _info.resourceId);
            }
            return resource.data;
        }

        public static short GetNumLoops(int viewId)
        {
            var resource = SciEngine.Instance.ResMan.FindResource(new ResourceId(ResourceType.View, (ushort) viewId),
                false);
            if (resource == null)
            {
                return 0;
            }

            System.Diagnostics.Debug.Assert(resource.size >= 3);
            return resource.data[2];
        }

        public static short GetNumCels(int viewId, short loopNo)
        {
            var resource = SciEngine.Instance.ResMan.FindResource(new ResourceId(ResourceType.View, (ushort) viewId), false);

            if (resource==null) {
                return 0;
            }

            var data = resource.data;

            var loopCount = data[2];

            // Every version of SCI32 has a logic error in this function that causes
            // random memory to be read if a script requests the cel count for one
            // past the maximum loop index. At least GK1 room 800 does this, and gets
            // stuck in an infinite loop because the game script expects this method
            // to return a non-zero value.
            // The scope of this bug means it is likely to pop up in other games, so we
            // explicitly trap the bad condition here and report it so that any other
            // game scripts relying on this broken behavior can be fixed as well

            throw new NotImplementedException();

//            if (loopNo == loopCount) {
//                SciCallOrigin origin;
//                SciWorkaroundSolution solution = TrackOriginAndFindWorkaround(0, kNumCels_workarounds, &origin);
//                switch (solution.type) {
//                    case WORKAROUND_NONE:
//                        error("[CelObjView::getNumCels]: loop number %d is equal to loop count in view %u, %s", loopNo, viewId, origin.toString().c_str());
//                    case WORKAROUND_FAKE:
//                        return (int16)solution.value;
//                    case WORKAROUND_IGNORE:
//                        return 0;
//                    case WORKAROUND_STILLCALL:
//                        break;
//                }
//            }

            if (loopNo > loopCount || loopNo < 0) {
                return 0;
            }

            ushort viewHeaderSize = data.ReadSci11EndianUInt16();
            byte loopHeaderSize = data[12];
            byte viewHeaderFieldSize = 2;

#if !NDEBUG
            BytePtr dataMax = new BytePtr(data, resource.size);
#endif
            BytePtr loopHeader = new BytePtr(data, viewHeaderFieldSize + viewHeaderSize + (loopHeaderSize * loopNo));
            System.Diagnostics.Debug.Assert(loopHeader + 3 <= dataMax);

            if ((sbyte)loopHeader[0] != -1) {
                loopHeader = new BytePtr(data, viewHeaderFieldSize + viewHeaderSize + (loopHeaderSize * (sbyte)loopHeader[0]));
                System.Diagnostics.Debug.Assert(loopHeader >= data && loopHeader + 3 <= dataMax);
            }

            return loopHeader[2];
        }
    }
}

#endif