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
    internal interface IMapper
    {
        void Draw(BytePtr target, byte pixel, byte skipColor);
    }

    internal interface IScaler
    {
        void SetTarget(short x, short y);
        byte Read();
    }

    internal interface IReader
    {
        BytePtr GetRow(short y);
    }

    internal struct CelCacheEntry
    {
        /**
         * A monotonically increasing cache ID used to identify
         * the least recently used item in the cache for
         * replacement.
         */
        public int id;
        public CelObj celObj;
    }

    // SCI32 has four different coordinate systems:
    // 1. low resolution, 2. game/script resolution,
    // 3. text/bitmap resolution, 4. screen resolution
    //
    // In CelObj, these values are used when there is
    // no baked in resolution of cels.
    //
    // In ScreenItem, it is used when deciding which
    // path to take to calculate dimensions.
    internal static class LowRes
    {
        public const int X = 320;
        public const int Y = 200;
    }

    internal enum CelType
    {
        View = 0,
        Pic = 1,
        Mem = 2,
        Color = 3
    }

    internal enum CelCompressionType
    {
        None = 0,
        RLE = 138,
        Invalid = 1000
    }

    /// <summary>
    /// A cel object is the lowest-level rendering primitive in
    /// the SCI engine and draws itself directly to a target
    /// pixel buffer.
    /// </summary>
    internal abstract class CelObj
    {
        /**
         * When true, every second line of the cel will be
         * rendered as a black line.
         *
         * @see ScreenItem::_drawBlackLines
         * @note Using a static member because otherwise this
         * would otherwise need to be copied down through
         * several calls. (SSCI did similar, using a global
         * variable.)
         */
        protected static bool _drawBlackLines;

        /**
         * When true, this cel will be horizontally mirrored
         * when it is drawn. This is an internal flag that is
         * set by draw methods based on the combination of the
         * cel's `_mirrorX` property and the owner screen item's
         * `_mirrorX` property.
         */
        protected bool _drawMirrored;

        public static CelScaler _scaler;

        /**
         * The basic identifying information for this cel. This
         * information effectively acts as a composite key for
         * a cel object, and any cel object can be recreated
         * from this data alone.
         */
        public CelInfo32 _info;

        /**
         * The offset to the cel header for this cel within the
         * raw resource data.
         */
        public int _celHeaderOffset;

        /**
         * The offset to the embedded palette for this cel
         * within the raw resource data.
         */
        public int _hunkPaletteOffset;

        /**
         * The natural dimensions of the cel.
         */
        public ushort _width, _height;

        /**
         * TODO: Documentation
         */
        public Point _origin;

        /**
         * The dimensions of the original coordinate system for
         * the cel. Used to scale cels from their native size
         * to the correct size on screen.
         *
         * @note This is set to scriptWidth/Height for
         * CelObjColor. For other cel objects, the value comes
         * from the raw resource data. For text bitmaps, this is
         * the width/height of the coordinate system used to
         * generate the text, which also defaults to
         * scriptWidth/Height but seems to typically be changed
         * to more closely match the native screen resolution.
         */
        public ushort _xResolution, _yResolution;

        /**
         * The skip (transparent) color for the cel. When
         * compositing, any pixels matching this color will not
         * be copied to the buffer.
         */
        public byte _skipColor;

        /**
         * Whether or not this cel has any transparent regions.
         * This is used for optimised drawing of non-transparent
         * cels.
         */
        public bool _transparent; // TODO: probably "skip"?

        /**
         * The compression type for the pixel data for this cel.
         */
        public CelCompressionType _compressionType;

        /**
         * Whether or not this cel should be palette-remapped?
         */
        public bool _remap;

        /**
         * If true, the cel contains pre-mirrored picture data.
         * This value comes directly from the resource data and
         * is XORed with the `_mirrorX` property of the owner
         * screen item when rendering.
         */
        public bool _mirrorX;

        protected CelObj()
        {
        }

        protected CelObj(CelObj src)
        {
            _celHeaderOffset = src._celHeaderOffset;
            _compressionType = src._compressionType;
            _origin = src._origin;
            _height = src._height;
            _hunkPaletteOffset = src._hunkPaletteOffset;
            _info = src._info.Clone();
            _mirrorX = src._mirrorX;
            _remap = src._remap;
            _yResolution = src._yResolution;
            _xResolution = src._xResolution;
            _transparent = src._transparent;
            _skipColor = src._skipColor;
            _width = src._width;
        }

        /**
         * Initialises static CelObj members.
         */

        public static void Init()
        {
            Deinit();
            _drawBlackLines = false;
            _nextCacheId = 1;
            _scaler = new CelScaler();
            _cache = new CelCacheEntry[100];
        }

        /**
         * Frees static CelObj members.
         */

        public static void Deinit()
        {
            _scaler = null;
            _cache = null;
        }

        /**
         * Draws the cel to the target buffer using the priority
         * and positioning information from the given screen
         * item. The mirroring of the cel will be unchanged from
         * any previous call to draw.
         */

        public void Draw(Buffer target, ScreenItem screenItem, ref Rect targetRect)
        {
            var scaledPosition = screenItem._scaledPosition;
            var scaleX = screenItem._ratioX;
            var scaleY = screenItem._ratioY;
            _drawBlackLines = screenItem._drawBlackLines;

            if (_remap)
            {
                // NOTE: In the original code this check was `g_Remap_numActiveRemaps && _remap`,
                // but since we are already in a `_remap` branch, there is no reason to check it
                // again
                if (SciEngine.Instance._gfxRemap32.RemapCount != 0)
                {
                    if (scaleX.IsOne && scaleY.IsOne)
                    {
                        if (_compressionType == CelCompressionType.None)
                        {
                            if (_drawMirrored)
                            {
                                DrawUncompHzFlipMap(target, targetRect, scaledPosition);
                            }
                            else
                            {
                                DrawUncompNoFlipMap(target, targetRect, scaledPosition);
                            }
                        }
                        else
                        {
                            if (_drawMirrored)
                            {
                                DrawHzFlipMap(target, targetRect, scaledPosition);
                            }
                            else
                            {
                                DrawNoFlipMap(target, targetRect, scaledPosition);
                            }
                        }
                    }
                    else
                    {
                        if (_compressionType == CelCompressionType.None)
                        {
                            ScaleDrawUncompMap(target, scaleX, scaleY, targetRect, scaledPosition);
                        }
                        else
                        {
                            ScaleDrawMap(target, scaleX, scaleY, targetRect, scaledPosition);
                        }
                    }
                }
                else
                {
                    if (scaleX.IsOne && scaleY.IsOne)
                    {
                        if (_compressionType == CelCompressionType.None)
                        {
                            if (_drawMirrored)
                            {
                                DrawUncompHzFlip(target, targetRect, scaledPosition);
                            }
                            else
                            {
                                DrawUncompNoFlip(target, targetRect, scaledPosition);
                            }
                        }
                        else
                        {
                            if (_drawMirrored)
                            {
                                DrawHzFlip(target, targetRect, scaledPosition);
                            }
                            else
                            {
                                DrawNoFlip(target, targetRect, scaledPosition);
                            }
                        }
                    }
                    else
                    {
                        if (_compressionType == CelCompressionType.None)
                        {
                            ScaleDrawUncomp(target, scaleX, scaleY, targetRect, scaledPosition);
                        }
                        else
                        {
                            ScaleDraw(target, scaleX, scaleY, targetRect, scaledPosition);
                        }
                    }
                }
            }
            else
            {
                if (scaleX.IsOne && scaleY.IsOne)
                {
                    if (_compressionType == CelCompressionType.None)
                    {
                        if (_transparent)
                        {
                            if (_drawMirrored)
                            {
                                DrawUncompHzFlipNoMD(target, targetRect, scaledPosition);
                            }
                            else
                            {
                                DrawUncompNoFlipNoMD(target, targetRect, scaledPosition);
                            }
                        }
                        else
                        {
                            if (_drawMirrored)
                            {
                                DrawUncompHzFlipNoMDNoSkip(target, targetRect, scaledPosition);
                            }
                            else
                            {
                                DrawUncompNoFlipNoMDNoSkip(target, targetRect, scaledPosition);
                            }
                        }
                    }
                    else
                    {
                        if (_drawMirrored)
                        {
                            DrawHzFlipNoMD(target, targetRect, scaledPosition);
                        }
                        else
                        {
                            DrawNoFlipNoMD(target, targetRect, scaledPosition);
                        }
                    }
                }
                else
                {
                    if (_compressionType == CelCompressionType.None)
                    {
                        ScaleDrawUncompNoMD(target, scaleX, scaleY, targetRect, scaledPosition);
                    }
                    else
                    {
                        ScaleDrawNoMD(target, scaleX, scaleY, targetRect, scaledPosition);
                    }
                }
            }

            _drawBlackLines = false;
        }

        private void ScaleDrawUncompMap(Buffer target, Rational scaleX, Rational scaleY, Rect targetRect,
            Point scaledPosition)
        {
            Render(() => new MAPPER_Map(),
                (celObj, tr, sp) =>
                    new SCALER_Scale(new READER_Uncompressed(celObj, (short) celObj._width),
                        _drawMirrored, celObj, tr, sp, scaleX, scaleY),
                target, targetRect, scaledPosition);
        }

        private void DrawHzFlipMap(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_Map(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(new READER_Compressed(celObj, maxWidth),
                    true, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void DrawNoFlipMap(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_Map(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(new READER_Compressed(celObj, maxWidth),
                    false, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void DrawUncompHzFlipMap(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_Map(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(
                    new READER_Uncompressed(celObj, maxWidth), true, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void DrawUncompNoFlipMap(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_Map(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(new READER_Uncompressed(celObj, maxWidth),
                    false, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void ScaleDrawMap(Buffer target, Rational scaleX, Rational scaleY, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_Map(),
                (celObj, tr, sp) => new SCALER_Scale(new READER_Compressed(celObj, (short) celObj._width),
                    _drawMirrored, celObj, tr, sp, scaleX, scaleY),
                target, targetRect, scaledPosition);
        }

        private void DrawUncompHzFlip(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_NoMap(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(
                    new READER_Uncompressed(celObj, maxWidth), true, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void DrawUncompNoFlip(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_NoMap(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(
                    new READER_Uncompressed(celObj, maxWidth), false, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void DrawHzFlip(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_NoMap(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(new READER_Compressed(celObj,maxWidth),
                    true, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void DrawNoFlip(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_NoMap(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(new READER_Compressed(celObj,maxWidth),
                    false, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void ScaleDrawUncomp(Buffer target, Rational scaleX, Rational scaleY, Rect targetRect,
            Point scaledPosition)
        {
            Render(() => new MAPPER_NoMap(),
                (celObj, tr, sp) => new SCALER_Scale(
                    new READER_Uncompressed(celObj, (short) celObj._width),
                    _drawMirrored, celObj, tr, sp, scaleX, scaleY),
                target, targetRect, scaledPosition);
        }

        private void ScaleDraw(Buffer target, Rational scaleX, Rational scaleY, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_NoMap(),
                (celObj, tr, sp) => new SCALER_Scale(new READER_Compressed(this, (short) celObj._width),
                    _drawMirrored, celObj, tr, sp, scaleX, scaleY),
                target, targetRect, scaledPosition);
        }

        private void DrawUncompHzFlipNoMD(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_NoMD(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(
                    new READER_Uncompressed(celObj, maxWidth),
                    true, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void DrawUncompHzFlipNoMDNoSkip(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_NoMDNoSkip(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(new READER_Uncompressed(celObj, maxWidth),
                    true, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void ScaleDrawNoMD(Buffer target, Rational scaleX, Rational scaleY, Rect targetRect,
            Point scaledPosition)
        {
            Render(() => new MAPPER_NoMD(),
                (celObj, tr, sp) =>
                        new SCALER_Scale(new READER_Compressed(this, (short) celObj._width),
                            _drawMirrored, celObj, tr, sp, scaleX, scaleY)
                , target, targetRect, scaledPosition);
        }

        private void ScaleDrawUncompNoMD(Buffer target, Rational scaleX, Rational scaleY, Rect targetRect,
            Point scaledPosition)
        {
            Render(() => new MAPPER_NoMD(),
                (celObj, tr, sp) => new SCALER_Scale(
                    new READER_Uncompressed(this, (short) celObj._width),
                        _drawMirrored, celObj, tr, sp, scaleX, scaleY),
                target, targetRect, scaledPosition);
        }

        private void DrawUncompNoFlipNoMD(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_NoMD(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(
                    new READER_Uncompressed(this, maxWidth), false, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void DrawUncompNoFlipNoMDNoSkip(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_NoMDNoSkip(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(
                    new READER_Uncompressed(this, maxWidth),
                    false, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void DrawHzFlipNoMD(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(() => new MAPPER_NoMD(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(new READER_Compressed(this, maxWidth),
                    true, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void DrawNoFlipNoMD(Buffer target, Rect targetRect, Point scaledPosition)
        {
            Render(
                () => new MAPPER_NoMD(),
                (celObj, maxWidth, sp) => new SCALER_NoScale(new READER_Compressed(this, maxWidth),
                    false, celObj, sp),
                target, targetRect, scaledPosition);
        }

        private void Render(Func<IMapper> mapperFactory, Func<CelObj, short, Point, IScaler> scalerFactory,
            Buffer target, Rect targetRect, Point scaledPosition)
        {
            var mapper = mapperFactory();
            var scaler = scalerFactory(this, (short) (targetRect.Left - scaledPosition.X + targetRect.Width),
                scaledPosition);
            var renderer = new Renderer(mapper, scaler, _skipColor, false);
            renderer.Draw(target, targetRect);
        }

        private void Render(Func<IMapper> mapperFactory,
            Func<CelObj, Rect, Point, IScaler> scalerFactory, Buffer target, Rect targetRect, Point scaledPosition)
        {
            var mapper = mapperFactory();
            var scaler = scalerFactory(this, targetRect, scaledPosition);
            var renderer = new Renderer(mapper, scaler, _skipColor, _drawBlackLines);
            renderer.Draw(target, targetRect);
        }

        /**
         * Draws the cel to the target buffer using the priority
         * and positioning information from the given screen
         * item and the given mirror flag.
         *
         * @note In SCI engine, this function was a virtual
         * function, but CelObjView, CelObjPic, and CelObjMem
         * all used the same function and the compiler
         * deduplicated the copies; we deduplicate the source by
         * putting the implementation on CelObj instead of
         * copying it to 3/4 of the subclasses.
         */

        public virtual void Draw(Buffer target, ScreenItem screenItem, ref Rect targetRect, bool mirrorX)
        {
            _drawMirrored = mirrorX;
            Draw(target, screenItem, ref targetRect);
        }

        /**
         * Draws the cel to the target buffer using the
         * positioning and mirroring information from the
         * provided arguments.
         *
         * @note In SCI engine, this function was a virtual
         * function, but CelObjView, CelObjPic, and CelObjMem
         * all used the same function and the compiler
         * deduplicated the copies; we deduplicate the source by
         * putting the implementation on CelObj instead of
         * copying it to 3/4 of the subclasses.
         */

        public virtual void Draw(Buffer target, ref Rect targetRect, ref Point scaledPosition, bool mirrorX)
        {
            _drawMirrored = mirrorX;
            Rational square = new Rational();
            DrawTo(target, ref targetRect, ref scaledPosition, square, square);
        }

        /**
         * Draws the cel to the target buffer using the given
         * position and scaling parameters. The mirroring of the
         * cel will be unchanged from any previous call to draw.
         */

        public void DrawTo(Buffer target, ref Rect targetRect, ref Point scaledPosition, Rational scaleX,
            Rational scaleY)
        {
            if (_remap)
            {
                if (scaleX.IsOne && scaleY.IsOne)
                {
                    if (_compressionType == CelCompressionType.None)
                    {
                        if (_drawMirrored)
                        {
                            DrawUncompHzFlipMap(target, targetRect, scaledPosition);
                        }
                        else
                        {
                            DrawUncompNoFlipMap(target, targetRect, scaledPosition);
                        }
                    }
                    else
                    {
                        if (_drawMirrored)
                        {
                            DrawHzFlipMap(target, targetRect, scaledPosition);
                        }
                        else
                        {
                            DrawNoFlipMap(target, targetRect, scaledPosition);
                        }
                    }
                }
                else
                {
                    if (_compressionType == CelCompressionType.None)
                    {
                        ScaleDrawUncompMap(target, scaleX, scaleY, targetRect, scaledPosition);
                    }
                    else
                    {
                        ScaleDrawMap(target, scaleX, scaleY, targetRect, scaledPosition);
                    }
                }
            }
            else
            {
                if (scaleX.IsOne && scaleY.IsOne)
                {
                    if (_compressionType == CelCompressionType.None)
                    {
                        if (_drawMirrored)
                        {
                            DrawUncompHzFlipNoMD(target, targetRect, scaledPosition);
                        }
                        else
                        {
                            DrawUncompNoFlipNoMD(target, targetRect, scaledPosition);
                        }
                    }
                    else
                    {
                        if (_drawMirrored)
                        {
                            DrawHzFlipNoMD(target, targetRect, scaledPosition);
                        }
                        else
                        {
                            DrawNoFlipNoMD(target, targetRect, scaledPosition);
                        }
                    }
                }
                else
                {
                    if (_compressionType == CelCompressionType.None)
                    {
                        ScaleDrawUncompNoMD(target, scaleX, scaleY, targetRect, scaledPosition);
                    }
                    else
                    {
                        ScaleDrawNoMD(target, scaleX, scaleY, targetRect, scaledPosition);
                    }
                }
            }
        }

        /**
         * Creates a copy of this cel on the free store and
         * returns a pointer to the new object. The new cel will
         * point to a shared copy of bitmap/resource data.
         */
        public abstract CelObj Duplicate();

        /**
         * Retrieves a pointer to the raw resource data for this
         * cel. This method cannot be used with a CelObjColor.
         */

        public abstract BytePtr GetResPointer();

        /**
         * Reads the pixel at the given coordinates. This method
         * is valid only for CelObjView and CelObjPic.
         */

        public virtual byte ReadPixel(ushort x, ushort y, bool mirrorX)
        {
            if (mirrorX)
            {
                x = (ushort) (_width - x - 1);
            }

            if (_compressionType == CelCompressionType.None)
            {
                READER_Uncompressed reader = new READER_Uncompressed(this, (short) (x + 1));
                return reader.GetRow((short) y)[x];
            }
            else
            {
                READER_Compressed reader = new READER_Compressed(this, (short) (x + 1));
                return reader.GetRow((short) y)[x];
            }
        }

        /**
         * Submits the palette from this cel to the palette
         * manager for integration into the master screen
         * palette.
         */

        public void SubmitPalette()
        {
            if (_hunkPaletteOffset != 0)
            {
                HunkPalette palette = new HunkPalette(new BytePtr(GetResPointer(), _hunkPaletteOffset));
                SciEngine.Instance._gfxPalette32.Submit(palette);
            }
        }

        /// <summary>
        /// A monotonically increasing cache ID used to identify
        /// the least recently used item in the cache for
        /// replacement.
        /// </summary>
        protected static int _nextCacheId;

        /// <summary>
        /// A cache of cel objects used to avoid reinitialisation
        /// overhead for cels with the same CelInfo32.
        /// </summary>
        /// <remarks>At least SQ6 uses a fixed cache size of 100.</remarks>
        protected static CelCacheEntry[] _cache;

        /// <summary>
        /// Searches the cel cache for a CelObj matching the
        /// provided CelInfo32. If not found, -1 is returned.
        /// nextInsertIndex will receive the index of the oldest
        /// item in the cache, which can be used to replace
        /// the oldest item with a newer item.
        /// </summary>
        /// <param name="celInfo"></param>
        /// <param name="nextInsertIndex"></param>
        /// <returns></returns>
        protected static int SearchCache(ref CelInfo32 celInfo, out int nextInsertIndex)
        {
            nextInsertIndex = -1;
            int oldestId = _nextCacheId + 1;
            int oldestIndex = 0;

            for (int i = 0, len = _cache.Length; i < len; ++i)
            {
                CelCacheEntry entry = _cache[i];

                if (entry.celObj == null)
                {
                    if (nextInsertIndex == -1)
                    {
                        nextInsertIndex = i;
                    }
                }
                else if (entry.celObj._info == celInfo)
                {
                    entry.id = ++_nextCacheId;
                    return i;
                }
                else if (oldestId > entry.id)
                {
                    oldestId = entry.id;
                    oldestIndex = i;
                }
            }

            if (nextInsertIndex == -1)
            {
                nextInsertIndex = oldestIndex;
            }

            return -1;
        }

        /// <summary>
        /// Puts a copy of this CelObj into the cache at the
        /// given cache index.
        /// </summary>
        /// <param name="cacheIndex"></param>
        protected void PutCopyInCache(int cacheIndex)
        {
            if (cacheIndex == -1)
            {
                Error("Invalid cache index");
            }

            var entry = _cache[cacheIndex];
            entry.celObj = null;
            entry.celObj = Duplicate();
            entry.id = ++_nextCacheId;
        }
    }
}

#endif
