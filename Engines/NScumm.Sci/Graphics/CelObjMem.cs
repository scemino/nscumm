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

using NScumm.Core;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// A CelObjMem is the drawing primitive for arbitrary
    /// bitmaps generated in memory. Generated bitmaps in SCI32
    /// include text & vector drawings and per-pixel screen
    /// transitions like dissolves.
    /// </summary>
    internal class CelObjMem : CelObj
    {
        public CelObjMem(Register bitmapObject)
        {
            _info = new CelInfo32();
            _info.type = CelType.Mem;
            _info.bitmap = bitmapObject;
            _mirrorX = false;
            _compressionType = CelCompressionType.None;
            _celHeaderOffset = 0;
            _transparent = true;

            SciBitmap bitmap = SciEngine.Instance.EngineState._segMan.LookupBitmap(bitmapObject);
            // NOTE: SSCI did no error checking here at all.
            if (bitmap == null)
            {
                Error("Bitmap {0} not found", bitmapObject);
            }
            _width = bitmap.Width;
            _height = bitmap.Height;
            _origin = bitmap.Origin;
            _skipColor = bitmap.SkipColor;
            _xResolution = bitmap.XResolution;
            _yResolution = bitmap.YResolution;
            _hunkPaletteOffset = bitmap.HunkPaletteOffset;
            _remap = bitmap.Remap;
        }

        public override CelObj Duplicate()
        {
            return new CelObjMem(_info.bitmap);
        }

        public override BytePtr GetResPointer()
        {
            return SciEngine.Instance.EngineState._segMan.LookupBitmap(_info.bitmap).RawData;
        }
    }
}

#endif