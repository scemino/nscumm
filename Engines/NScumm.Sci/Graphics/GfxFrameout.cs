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

using System;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;
using NScumm.Sci.Video;
using NScumm.Core.Common;

#if ENABLE_SCI32

namespace NScumm.Sci.Graphics
{
    public enum ShakeDirection
    {
        Vertical = 1,
        Horizontal = 2
    }

    internal enum ShowStyleType /* : uint8 */
    {
        kShowStyleNone = 0,
        kShowStyleHShutterOut = 1,
        kShowStyleHShutterIn = 2,
        kShowStyleVShutterOut = 3,
        kShowStyleVShutterIn = 4,
        kShowStyleWipeLeft = 5,
        kShowStyleWipeRight = 6,
        kShowStyleWipeUp = 7,
        kShowStyleWipeDown = 8,
        kShowStyleIrisOut = 9,
        kShowStyleIrisIn = 10,
        kShowStyleDissolveNoMorph = 11,
        kShowStyleDissolve = 12,
        kShowStyleFadeOut = 13,
        kShowStyleFadeIn = 14,
        kShowStyleMorph = 15
    }

    /// <summary>
    /// Frameout class, kFrameout and relevant functions for SCI32 games.
    /// Roughly equivalent to GraphicsMgr in the actual SCI engine.
    /// </summary>
    internal class GfxFrameout
    {
        /// <summary>
        /// Whether palMorphFrameOut should be used instead of
        /// frameOut for rendering. Used by kMorphOn to
        /// explicitly enable palMorphFrameOut for one frame.
        /// </summary>
        public bool _palMorphIsOn;
        public bool _isHiRes;
        private GfxPalette32 _palette;
        private SegManager _segMan;

        /// <summary>
        /// Optimization to avoid the more expensive object name
        /// comparision on every call to kAddScreenItem and
        /// kRemoveScreenItem.
        /// </summary>
        private bool _benchmarkingFinished;

        /// <summary>
        /// Whether or not calls to kFrameOut should be framerate
        /// limited to 60fps.
        /// </summary>
        private bool _throttleFrameOut;

        private static readonly int[][] dissolveSequences =
        {
            /* SCI2.1early- */ new[] {3, 6, 12, 20, 48, 96, 184, 272, 576, 1280, 3232, 6912, 13568, 24576, 46080},
            /* SCI2.1mid+ */
            new[]
            {
                0, 0, 3, 6, 12, 20, 48, 96, 184, 272, 576, 1280, 3232, 6912, 13568, 24576, 46080, 73728, 132096, 466944
            }
        };

        private static readonly short[][] divisionsDefaults =
        {
            /* SCI2.1early- */ new short[] {1, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 40, 40, 101, 101},
            /* SCI2.1mid+ */   new short[] {1, 20, 20, 20, 20, 10, 10, 10, 10, 20, 20, 6, 10, 101, 101, 2}
        };

        private static readonly short[][] unknownCDefaults =
        {
            /* SCI2.1early- */ new short[] {0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 0, 0, 0, 0},
            /* SCI2.1mid+ */   new short[] {0, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 0, 0, 7, 7, 0}
        };

        /**
         * State tracker to provide more accurate 60fps
         * video throttling.
         */
        private byte _throttleState;

        /**
         * The internal display pixel buffer. During frameOut,
         * this buffer is drawn into according to the draw and
         * erase rects calculated by `calcLists`, then drawn out
         * to the hardware surface according to the `_showList`
         * rects (which are also calculated by `calcLists`).
         */
        private Buffer _currentBuffer;

        /**
         * When true, a change to the remap zone in the palette
         * has occurred and screen items with remap data need to
         * be redrawn.
         */
        private bool _remapOccurred;

        /**
         * Whether or not the data in the current buffer is what
         * is visible to the user. During rendering updates,
         * this flag is set to false.
         */
        public bool _frameNowVisible;

        /**
         * TODO: Document
         * TODO: Depending upon if the engine ever modifies this
         * rect, it may be stupid to store it separately instead
         * of just getting width/height from GfxScreen.
         *
         * @note This field is on `GraphicsMgr.screen` in SCI
         * engine.
         */
        private Rect _screenRect;

        /**
         * A list of rectangles, in display coordinates, that
         * represent portions of the internal screen buffer that
         * should be drawn to the hardware display surface.
         *
         * @note This field is on `GraphicsMgr.screen` in SCI
         * engine.
         */
        private RectList _showList = new RectList();

        /**
         * The amount of extra overdraw that is acceptable when
         * merging two show list rectangles together into a
         * single larger rectangle.
         *
         * @note This field is on `GraphicsMgr.screen` in SCI
         * engine.
         */
        private int _overdrawThreshold;

        /**
         * A list of planes that are currently drawn to the
         * hardware display surface. Used to calculate
         * differences in plane properties between the last
         * frame and current frame.
         *
         * @note This field is on `GraphicsMgr.visibleScreen` in
         * SCI engine.
         */
        private PlaneList _visiblePlanes;

        /**
         * The list of planes (i.e. layers) that have been added
         * to the screen.
         *
         * @note This field is on `GraphicsMgr.screen` in SCI
         * engine.
         */
        private readonly PlaneList _planes = new PlaneList();

        private GfxTransitions32 _transitions;
        private GfxCursor32 _cursor = new GfxCursor32();

        public Buffer CurrentBuffer => _currentBuffer;
        public PlaneList VisiblePlanes => _visiblePlanes;

        public GfxFrameout(SegManager segMan, GfxPalette32 palette, GfxTransitions32 transitions, GfxCursor32 cursor)
        {
            _isHiRes = GameIsHiRes();
            _palette = palette;
            _cursor = cursor;
            _segMan = segMan;
            _transitions = transitions;
            _throttleFrameOut = true;
            _visiblePlanes = new PlaneList();

            if (SciEngine.Instance.GameId == SciGameId.PHANTASMAGORIA)
            {
                _currentBuffer = new Buffer(630, 450, BytePtr.Null);
            }
            else if (_isHiRes)
            {
                _currentBuffer = new Buffer(640, 480, BytePtr.Null);
            }
            else
            {
                _currentBuffer = new Buffer(320, 200, BytePtr.Null);
            }
            _currentBuffer.SetPixels(new byte[_currentBuffer.ScreenWidth * _currentBuffer.ScreenHeight]);
            _screenRect = new Rect((short)_currentBuffer.ScreenWidth, (short)_currentBuffer.ScreenHeight);
            // TODO: vs
            // InitGraphics(_currentBuffer.ScreenWidth, _currentBuffer.ScreenHeight, _isHiRes);

            switch (SciEngine.Instance.GameId)
            {
                case SciGameId.HOYLE5:
                case SciGameId.GK2:
                case SciGameId.LIGHTHOUSE:
                case SciGameId.LSL7:
                case SciGameId.PHANTASMAGORIA2:
                case SciGameId.PQSWAT:
                case SciGameId.TORIN:
                case SciGameId.RAMA:
                    _currentBuffer.ScriptWidth = 640;
                    _currentBuffer.ScriptHeight = 480;
                    break;
                default:
                    // default script width for other games is 320x200
                    break;
            }
        }

        public bool GameIsHiRes()
        {
            // QFG4 is always low resolution
            if (SciEngine.Instance.GameId == SciGameId.QFG4)
            {
                return false;
            }

            // GK1 DOS floppy is low resolution only, but GK1 Mac floppy is high
            // resolution only
            if (SciEngine.Instance.GameId == SciGameId.GK1 &&
                !SciEngine.Instance.IsCd &&
                SciEngine.Instance.Platform != Core.IO.Platform.Macintosh)
            {

                return false;
            }

            // All other games are either high resolution by default, or have a
            // user-defined toggle
            return ConfigManager.Instance.Get<bool>("enable_high_resolution_graphics");
        }

        public void DeleteScreenItem(ScreenItem screenItem)
        {
            Plane plane = _planes.FindByObject(screenItem._plane);
            if (plane == null)
            {
                Error("GfxFrameout::deleteScreenItem: Could not find plane {0} for screen item {1}", screenItem._plane, screenItem._object);
            }
            if (plane._screenItemList.FindByObject(screenItem._object) == null)
            {
                Error("GfxFrameout::deleteScreenItem: Screen item {0} not found in plane {1}", screenItem._object, screenItem._plane);
            }
            DeleteScreenItem(screenItem, plane);
        }

        public void DeleteScreenItem(ScreenItem screenItem, Plane plane)
        {
            if (screenItem._created == 0)
            {
                screenItem._created = 0;
                screenItem._updated = 0;
                screenItem._deleted = GetScreenCount();
            }
            else
            {
                plane._screenItemList.Remove(screenItem);
                plane._screenItemList.Pack();
            }
        }

        public void DeleteScreenItem(ScreenItem screenItem, Register planeObject)
        {
            Plane plane = _planes.FindByObject(planeObject);
            if (plane == null)
            {
                Error("GfxFrameout::deleteScreenItem: Could not find plane {0} for screen item {1}", planeObject,
                    screenItem._object);
            }
            DeleteScreenItem(screenItem, plane);
        }

        public void DeletePlane(Plane planeToFind)
        {
            Plane plane = _planes.FindByObject(planeToFind._object);
            if (plane == null)
            {
                Error("deletePlane: Plane {0} not found", planeToFind._object);
            }

            if (plane._created != 0)
            {
                _planes.Erase(plane);
            }
            else
            {
                plane._created = 0;
                plane._moved = 0;
                plane._deleted = GetScreenCount();
            }
        }

        public void Run()
        {
            CelObj.Init();
            Plane.Init();
            ScreenItem.Init();

            // NOTE: This happens in SCI::InitPlane in the actual engine,
            // and is a background fill plane to ensure hidden planes
            // (planes with a priority of -1) are never drawn
            Plane initPlane =
                new Plane(new Rect((short)_currentBuffer.ScriptWidth, (short)_currentBuffer.ScriptHeight));
            initPlane._priority = 0;
            _planes.Add(initPlane);
        }

        // NOTE: This function is used within ScreenItem subsystem and assigned
        // to various booleanish fields that seem to represent the state of the
        // screen item (created, updated, deleted). In GK1/DOS, Phant1/m68k,
        // SQ6/DOS, SQ6/Win, and Phant2/Win, this function simply returns 1. If
        // you know of any game/environment where this function returns some
        // value other than 1, or if you used to work at Sierra and can explain
        // why this is a thing (and if anyone needs to care about it), please
        // open a ticket!!
        public int GetScreenCount()
        {
            return 1;
        }

        public PlaneList GetPlanes()
        {
            return _planes;
        }

        public void KernelAddPlane(Register @object)
        {
            Plane plane = _planes.FindByObject(@object);
            if (plane != null)
            {
                plane.Update(@object);
                UpdatePlane(plane);
            }
            else
            {
                plane = new Plane(@object);
                AddPlane(plane);
            }
        }

        public void KernelAddScreenItem(Register @object)
        {
            // The "fred" object is used to test graphics performance;
            // it is impacted by framerate throttling, so disable the
            // throttling when this item is on the screen for the
            // performance check to pass.
            if (!_benchmarkingFinished && _throttleFrameOut && CheckForFred(@object))
            {
                _throttleFrameOut = false;
            }

            Register planeObject = SciEngine.ReadSelector(_segMan, @object, o => o.plane);

            _segMan.GetObject(@object).SetInfoSelectorFlag(SciObject.InfoFlagViewInserted);

            Plane plane = _planes.FindByObject(planeObject);
            if (plane == null)
            {
                Error("kAddScreenItem: Plane {0} not found for screen item {1}", planeObject, @object);
            }

            ScreenItem screenItem = plane._screenItemList.FindByObject(@object);
            if (screenItem != null)
            {
                screenItem.Update(@object);
            }
            else
            {
                screenItem = new ScreenItem(@object);
                plane._screenItemList.Add(screenItem);
            }
        }

        private void UpdatePlane(Plane plane)
        {
            // NOTE: This assertion comes from SCI engine code.
            System.Diagnostics.Debug.Assert(_planes.FindByObject(plane._object) == plane);

            Plane visiblePlane = _visiblePlanes.FindByObject(plane._object);
            plane.Sync(visiblePlane, _screenRect);
            // NOTE: updateScreenRect was originally called a second time here,
            // but it is already called at the end of the Plane::Update call
            // in the original engine anyway.

            _planes.Sort();
        }

        public void AddPlane(Plane plane)
        {
            if (_planes.FindByObject(plane._object) == null)
            {
                plane.ClipScreenRect(_screenRect);
                _planes.Add(plane);
            }
            else
            {
                plane._deleted = 0;
                if (plane._created == 0)
                {
                    plane._moved = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }
                _planes.Sort();
            }
        }

        private bool CheckForFred(Register @object)
        {
            short viewId = (short)SciEngine.ReadSelectorValue(_segMan, @object, o => o.view);
            SciGameId gameId = SciEngine.Instance.GameId;

            if (gameId == SciGameId.QFG4 && viewId == 9999)
            {
                return true;
            }

            if (gameId != SciGameId.QFG4 && viewId == -556)
            {
                return true;
            }

            return _segMan.GetObjectName(@object) == "fred";
        }

        public void KernelDeletePlane(Register @object)
        {
            Plane plane = _planes.FindByObject(@object);
            if (plane == null)
            {
                Error("kDeletePlane: Plane {0} not found", @object);
            }

            if (plane._created != 0)
            {
                // NOTE: The original engine calls some `AbortPlane` function that
                // just ends up doing this anyway so we skip the extra indirection
                _planes.Erase(plane);
            }
            else
            {
                plane._created = 0;
                plane._deleted = SciEngine.Instance._gfxFrameout.GetScreenCount();
            }
        }

        public void PalMorphFrameOut(sbyte[] styleRanges, PlaneShowStyle showStyle)
        {
            Palette sourcePalette = new Palette(_palette.NextPalette);
            AlterVmap(sourcePalette, sourcePalette, -1, styleRanges);

            short prevRoom = SciEngine.Instance.EngineState.variables[Vm.VAR_GLOBAL][Vm.GlobalVarPreviousRoomNo].ToInt16();

            Rect rect = new Rect((short)_currentBuffer.ScreenWidth, (short)_currentBuffer.ScreenHeight);
            _showList.Add(rect);
            ShowBits();

            // NOTE: The original engine allocated these as static arrays of 100
            // pointers to ScreenItemList / RectList
            var screenItemLists = new Array<DrawList>(() => new DrawList());
            var eraseLists = new Array<RectList>(() => new RectList());

            screenItemLists.Resize(_planes.Size);
            eraseLists.Resize(_planes.Size);

            if (SciEngine.Instance._gfxRemap32.RemapCount > 0 && _remapOccurred)
            {
                RemapMarkRedraw();
            }

            CalcLists(screenItemLists, eraseLists);
            foreach (var list in screenItemLists)
            {
                list.Sort();
            }

            foreach (var list in screenItemLists)
            {
                foreach (var drawItem in list)
                {
                    drawItem.screenItem.GetCelObj().SubmitPalette();
                }
            }

            _remapOccurred = _palette.UpdateForFrame();
            _frameNowVisible = false;

            for (int i = 0; i < _planes.Size; ++i)
            {
                DrawEraseList(eraseLists[i], _planes[i]);
                DrawScreenItemList(screenItemLists[i]);
            }

            Palette nextPalette = new Palette(_palette.NextPalette);

            if (prevRoom < 1000)
            {
                for (int i = 0; i < sourcePalette.colors.Length; ++i)
                {
                    if (styleRanges[i] == -1 || styleRanges[i] == 0)
                    {
                        sourcePalette.colors[i] = nextPalette.colors[i];
                        sourcePalette.colors[i].used = 1;
                    }
                }
            }
            else
            {
                for (int i = 0; i < sourcePalette.colors.Length; ++i)
                {
                    if (styleRanges[i] == -1 || ValidZeroStyle((byte)styleRanges[i], i))
                    {
                        sourcePalette.colors[i] = nextPalette.colors[i];
                        sourcePalette.colors[i].used = 1;
                    }
                }
            }

            _palette.Submit(sourcePalette);
            _palette.UpdateFFrame();
            _palette.UpdateHardware();
            AlterVmap(nextPalette, sourcePalette, 1, _transitions._styleRanges);

            if (showStyle != null && showStyle.type != ShowStyleType.kShowStyleMorph)
            {
                _transitions.ProcessEffects(showStyle);
            }
            else
            {
                ShowBits();
            }

            _frameNowVisible = true;

            foreach (var plane in _planes)
            {
                plane._redrawAllCount = GetScreenCount();
            }

            if (SciEngine.Instance._gfxRemap32.RemapCount > 0 && _remapOccurred)
            {
                RemapMarkRedraw();
            }

            CalcLists(screenItemLists, eraseLists);
            foreach (var list in screenItemLists)
            {
                list.Sort();
            }

            foreach (var list in screenItemLists)
            {
                foreach (var drawItem in list)
                {
                    drawItem.screenItem.GetCelObj().SubmitPalette();
                }
            }

            _remapOccurred = _palette.UpdateForFrame();
            // NOTE: During this second loop, `_frameNowVisible = false` is
            // inside the next loop in SCI2.1mid
            _frameNowVisible = false;

            for (int i = 0; i < _planes.Size; ++i)
            {
                DrawEraseList(eraseLists[i], _planes[i]);
                DrawScreenItemList(screenItemLists[i]);
            }

            _palette.Submit(nextPalette);
            _palette.UpdateFFrame();
            _palette.UpdateHardware(false);
            ShowBits();

            _frameNowVisible = true;
        }

        /**
	     * Validates whether the given palette index in the
	     * style range should copy a color from the next
	     * palette to the source palette during a palette
	     * morph operation.
	     */
        private bool ValidZeroStyle(byte style, int i)
        {
            if (style != 0)
            {
                return false;
            }

            // TODO: Cannot check Shivers or MGDX until those executables can be
            // unwrapped
            switch (SciEngine.Instance.GameId)
            {
                case SciGameId.KQ7:
                case SciGameId.PHANTASMAGORIA:
                case SciGameId.SQ6:
                    return (i > 71 && i < 104);
                default:
                    return true;
            }
        }

        public void KernelDeleteScreenItem(Register @object)
        {
            // The "fred" object is used to test graphics performance;
            // it is impacted by framerate throttling, so disable the
            // throttling when this item is on the screen for the
            // performance check to pass.
            if (!_benchmarkingFinished && CheckForFred(@object))
            {
                _benchmarkingFinished = true;
                _throttleFrameOut = true;
            }

            _segMan.GetObject(@object).ClearInfoSelectorFlag(SciObject.InfoFlagViewInserted);

            Register planeObject = SciEngine.ReadSelector(_segMan, @object, o => o.plane);
            Plane plane = _planes.FindByObject(planeObject);

            ScreenItem screenItem = plane?._screenItemList.FindByObject(@object);
            if (screenItem == null)
            {
                return;
            }

            DeleteScreenItem(screenItem, plane);
        }

        public void ShowRect(Rect rect)
        {
            if (!rect.IsEmpty)
            {
                _showList.Clear();
                _showList.Add(rect);
                ShowBits();
            }
        }

        public void KernelFrameOut(bool shouldShowBits)
        {
            if (_transitions.HasShowStyles)
            {
                _transitions.ProcessShowStyles();
            }
            else if (_palMorphIsOn)
            {
                PalMorphFrameOut(_transitions._styleRanges, null);
                _palMorphIsOn = false;
            }
            else
            {
                if (_transitions.HasScrolls)
                {
                    _transitions.ProcessScrolls();
                }

                FrameOut(shouldShowBits);
            }

            Throttle();
        }

        public void Throttle()
        {
            if (!_throttleFrameOut) return;

            byte throttleTime;
            if (_throttleState == 2)
            {
                throttleTime = 16;
                _throttleState = 0;
            }
            else
            {
                throttleTime = 17;
                ++_throttleState;
            }

            SciEngine.Instance.EngineState.SpeedThrottler(throttleTime);
            SciEngine.Instance.EngineState._throttleTrigger = true;
        }

        private void AlterVmap(Palette palette1, Palette palette2, sbyte style, sbyte[] styleRanges)
        {
            byte[] clut = new byte[256];

            for (int paletteIndex = 0; paletteIndex < palette1.colors.Length; ++paletteIndex)
            {
                int outerR = palette1.colors[paletteIndex].R;
                int outerG = palette1.colors[paletteIndex].G;
                int outerB = palette1.colors[paletteIndex].B;

                if (styleRanges[paletteIndex] == style)
                {
                    int minDiff = 262140;
                    int minDiffIndex = paletteIndex;

                    for (int i = 0; i < 236; ++i)
                    {
                        if (styleRanges[i] != style)
                        {
                            int r = palette1.colors[i].R;
                            int g = palette1.colors[i].G;
                            int b = palette1.colors[i].B;
                            int diffSquared = (outerR - r) * (outerR - r) + (outerG - g) * (outerG - g) +
                                              (outerB - b) * (outerB - b);
                            if (diffSquared < minDiff)
                            {
                                minDiff = diffSquared;
                                minDiffIndex = i;
                            }
                        }
                    }

                    clut[paletteIndex] = (byte)minDiffIndex;
                }

                if (style == 1 && styleRanges[paletteIndex] == 0)
                {
                    int minDiff = 262140;
                    int minDiffIndex = paletteIndex;

                    for (int i = 0; i < 236; ++i)
                    {
                        int r = palette2.colors[i].R;
                        int g = palette2.colors[i].G;
                        int b = palette2.colors[i].B;

                        int diffSquared = (outerR - r) * (outerR - r) + (outerG - g) * (outerG - g) +
                                          (outerB - b) * (outerB - b);
                        if (diffSquared < minDiff)
                        {
                            minDiff = diffSquared;
                            minDiffIndex = i;
                        }
                    }

                    clut[paletteIndex] = (byte)minDiffIndex;
                }
            }

            BytePtr pixels = _currentBuffer.Pixels;

            for (int pixelIndex = 0, numPixels = _currentBuffer.ScreenWidth * _currentBuffer.ScreenHeight;
                pixelIndex < numPixels;
                ++pixelIndex)
            {
                byte currentValue = pixels[pixelIndex];
                sbyte styleRangeValue = styleRanges[currentValue];
                if (styleRangeValue == -1 && styleRangeValue == style)
                {
                    currentValue = pixels[pixelIndex] = clut[currentValue];
                    // NOTE: In original engine this assignment happens outside of the
                    // condition, but if the branch is not followed the value is just
                    // going to be the same as it was before
                    styleRangeValue = styleRanges[currentValue];
                }

                if (
                    (styleRangeValue == 1 && styleRangeValue == style) ||
                    (styleRangeValue == 0 && style == 1)
                )
                {
                    pixels[pixelIndex] = clut[currentValue];
                }
            }
        }


        private void DrawScreenItemList(DrawList screenItemList)
        {
            var drawListSize = screenItemList.Count;
            for (var i = 0; i < drawListSize; ++i)
            {
                DrawItem drawItem = screenItemList[i];
                MergeToShowList(drawItem.rect, _showList, _overdrawThreshold);
                ScreenItem screenItem = drawItem.screenItem;
                // TODO: Remove
                //		debug("Drawing item %04x:%04x to %d %d %d %d", PRINT_REG(screenItem._object), PRINT_RECT(drawItem.rect));
                CelObj celObj = screenItem._celObj;
                celObj.Draw(_currentBuffer, screenItem, ref drawItem.rect, screenItem._mirrorX ^ celObj._mirrorX);
            }
        }

        private void DrawEraseList(RectList eraseList, Plane plane)
        {
            if (plane._type != PlaneType.Colored)
            {
                return;
            }

            var eraseListSize = eraseList.Count;
            for (var i = 0; i < eraseListSize; ++i)
            {
                MergeToShowList(eraseList[i], _showList, _overdrawThreshold);
                _currentBuffer.FillRect(eraseList[i], plane._back);
            }
        }

        private void MergeToShowList(Rect drawRect, RectList showList, int overdrawThreshold)
        {
            RectList mergeList = new RectList();
            mergeList.Add(drawRect);

            for (var i = 0; i < mergeList.Count; ++i)
            {
                bool didMerge = false;
                Rect r1 = mergeList[i];
                if (r1.IsEmpty) continue;

                for (var j = 0; j < showList.Count; ++j)
                {
                    Rect r2 = showList[j];
                    if (r2.IsEmpty) continue;

                    var merged = r1;
                    merged.Extend(r2);

                    int difference = merged.Width * merged.Height;
                    difference -= r1.Width * r1.Height;
                    difference -= r2.Width * r2.Height;
                    if (r1.Intersects(r2))
                    {
                        Rect overlap = r1.FindIntersectingRect(r2);
                        difference += overlap.Width * overlap.Height;
                    }

                    if (difference <= overdrawThreshold)
                    {
                        mergeList.RemoveAt(i);
                        showList.RemoveAt(j);
                        mergeList.Add(merged);
                        didMerge = true;
                        break;
                    }

                    Rect[] outRects = new Rect[2];
                    int splitCount = SplitRectsForRender(mergeList[i], showList[j], outRects);
                    if (splitCount != -1)
                    {
                        mergeList.Add(mergeList[i]);
                        mergeList.RemoveAt(i);
                        showList.RemoveAt(j);
                        didMerge = true;
                        while ((splitCount--) != 0)
                        {
                            mergeList.Add(outRects[splitCount]);
                        }
                        break;
                    }
                }

                if (didMerge)
                {
                    showList.Pack();
                }
            }

            mergeList.Pack();
            for (var i = 0; i < mergeList.Count; ++i)
            {
                showList.Add(mergeList[i]);
            }
        }

        /**
         * Determines the parts of `middleRect` that aren't overlapped
         * by `showRect`, optimised for contiguous memory writes.
         * Returns -1 if `middleRect` and `showRect` have no intersection.
         * Returns number of returned parts (in `outRects`) otherwise.
         * (In particular, this returns 0 if `middleRect` is contained
         * in `other`.)
         *
         * `middleRect` is modified directly to extend into the upper
         * and lower rects.
         */

        private int SplitRectsForRender(Rect middleRect, Rect showRect, Rect[] outRects)
        {
            if (!middleRect.Intersects(showRect))
            {
                return -1;
            }

            short minLeft = Math.Min(middleRect.Left, showRect.Left);
            short maxRight = Math.Max(middleRect.Right, showRect.Right);

            short upperLeft, upperTop, upperRight, upperMaxTop;
            if (middleRect.Top < showRect.Top)
            {
                upperLeft = middleRect.Left;
                upperTop = middleRect.Top;
                upperRight = middleRect.Right;
                upperMaxTop = showRect.Top;
            }
            else
            {
                upperLeft = showRect.Left;
                upperTop = showRect.Top;
                upperRight = showRect.Right;
                upperMaxTop = middleRect.Top;
            }

            short lowerLeft, lowerRight, lowerBottom, lowerMinBottom;
            if (middleRect.Bottom > showRect.Bottom)
            {
                lowerLeft = middleRect.Left;
                lowerRight = middleRect.Right;
                lowerBottom = middleRect.Bottom;
                lowerMinBottom = showRect.Bottom;
            }
            else
            {
                lowerLeft = showRect.Left;
                lowerRight = showRect.Right;
                lowerBottom = showRect.Bottom;
                lowerMinBottom = middleRect.Bottom;
            }

            int splitCount = 0;
            middleRect.Left = minLeft;
            middleRect.Top = upperMaxTop;
            middleRect.Right = maxRight;
            middleRect.Bottom = lowerMinBottom;

            if (upperTop != upperMaxTop)
            {
                outRects[0].Left = upperLeft;
                outRects[0].Top = upperTop;
                outRects[0].Right = upperRight;
                outRects[0].Bottom = upperMaxTop;

                // Merge upper rect into middle rect if possible
                if (outRects[0].Left == middleRect.Left && outRects[0].Right == middleRect.Right)
                {
                    middleRect.Top = outRects[0].Top;
                }
                else
                {
                    ++splitCount;
                }
            }

            if (lowerBottom != lowerMinBottom)
            {
                outRects[splitCount].Left = lowerLeft;
                outRects[splitCount].Top = lowerMinBottom;
                outRects[splitCount].Right = lowerRight;
                outRects[splitCount].Bottom = lowerBottom;

                // Merge lower rect into middle rect if possible
                if (outRects[splitCount].Left == middleRect.Left && outRects[splitCount].Right == middleRect.Right)
                {
                    middleRect.Bottom = outRects[splitCount].Bottom;
                }
                else
                {
                    ++splitCount;
                }
            }

            System.Diagnostics.Debug.Assert(splitCount <= 2);
            return splitCount;
        }

        private void CalcLists(Array<DrawList> drawLists, Array<RectList> eraseLists)
        {
            CalcLists(drawLists, eraseLists, new Rect());
        }

        // NOTE: The third rectangle parameter is only ever given a non-empty rect
        // by VMD code, via `frameOut`
        private void CalcLists(Array<DrawList> drawLists, Array<RectList> eraseLists, Rect eraseRect)
        {
            RectList eraseList = new RectList();
            Rect[] outRects = new Rect[4];
            int deletedPlaneCount = 0;
            bool addedToEraseList = false;
            bool foundTransparentPlane = false;

            if (!eraseRect.IsEmpty)
            {
                addedToEraseList = true;
                eraseList.Add(eraseRect);
            }

            var planeCount = _planes.Size;
            for (var outerPlaneIndex = 0; outerPlaneIndex < planeCount; ++outerPlaneIndex)
            {
                Plane outerPlane = _planes[outerPlaneIndex];
                Plane visiblePlane = _visiblePlanes.FindByObject(outerPlane._object);

                // NOTE: SSCI only ever checks for kPlaneTypeTransparent here, even
                // though kPlaneTypeTransparentPicture is also a transparent plane
                if (outerPlane._type == PlaneType.Transparent)
                {
                    foundTransparentPlane = true;
                }

                if (outerPlane._deleted != 0)
                {
                    if (visiblePlane != null && !visiblePlane._screenRect.IsEmpty)
                    {
                        eraseList.Add(visiblePlane._screenRect);
                        addedToEraseList = true;
                    }
                    ++deletedPlaneCount;
                }
                else if (visiblePlane != null && outerPlane._moved != 0)
                {
                    // _moved will be decremented in the final loop through the planes,
                    // at the end of this function

                    {
                        int splitCount = SplitRects(visiblePlane._screenRect, outerPlane._screenRect, outRects);
                        if (splitCount != 0)
                        {
                            if (splitCount == -1 && !visiblePlane._screenRect.IsEmpty)
                            {
                                eraseList.Add(visiblePlane._screenRect);
                            }
                            else
                            {
                                for (int i = 0; i < splitCount; ++i)
                                {
                                    eraseList.Add(outRects[i]);
                                }
                            }
                            addedToEraseList = true;
                        }
                    }

                    if (outerPlane._redrawAllCount == 0)
                    {
                        int splitCount = SplitRects(outerPlane._screenRect, visiblePlane._screenRect, outRects);
                        if (splitCount != 0)
                        {
                            for (int i = 0; i < splitCount; ++i)
                            {
                                eraseList.Add(outRects[i]);
                            }
                            addedToEraseList = true;
                        }
                    }
                }

                if (addedToEraseList)
                {
                    for (var rectIndex = 0; rectIndex < eraseList.Count; ++rectIndex)
                    {
                        Rect rect = eraseList[rectIndex];
                        for (int innerPlaneIndex = planeCount - 1; innerPlaneIndex >= 0; --innerPlaneIndex)
                        {
                            Plane innerPlane = _planes[innerPlaneIndex];

                            if (
                                innerPlane._deleted == 0 &&
                                innerPlane._type != PlaneType.Transparent &&
                                innerPlane._screenRect.Intersects(rect)
                            )
                            {
                                if (innerPlane._redrawAllCount == 0)
                                {
                                    eraseLists[innerPlaneIndex].Add(innerPlane._screenRect.FindIntersectingRect(rect));
                                }

                                int splitCount = SplitRects(rect, innerPlane._screenRect, outRects);
                                for (int i = 0; i < splitCount; ++i)
                                {
                                    eraseList.Add(outRects[i]);
                                }

                                eraseList.RemoveAt(rectIndex);
                                break;
                            }
                        }
                    }

                    eraseList.Pack();
                }
            }

            // clean up deleted planes
            if (deletedPlaneCount != 0)
            {
                for (int planeIndex = planeCount - 1; planeIndex >= 0; --planeIndex)
                {
                    Plane plane = _planes[planeIndex];

                    if (plane._deleted != 0)
                    {
                        --plane._deleted;
                        if (plane._deleted <= 0)
                        {
                            int visiblePlaneIndex = _visiblePlanes.FindIndexByObject(plane._object);
                            if (visiblePlaneIndex != -1)
                            {
                                _visiblePlanes.RemoveAt(visiblePlaneIndex);
                            }

                            _planes.RemoveAt(planeIndex);
                            eraseLists.RemoveAt(planeIndex);
                            drawLists.RemoveAt(planeIndex);
                        }

                        if (--deletedPlaneCount <= 0)
                        {
                            break;
                        }
                    }
                }
            }

            // Some planes may have been deleted, so re-retrieve count
            planeCount = _planes.Size;

            for (var outerIndex = 0; outerIndex < planeCount; ++outerIndex)
            {
                // "outer" just refers to the outer loop
                Plane outerPlane = _planes[outerIndex];
                if (outerPlane._priorityChanged != 0)
                {
                    --outerPlane._priorityChanged;

                    Plane visibleOuterPlane = _visiblePlanes.FindByObject(outerPlane._object);
                    if (visibleOuterPlane == null)
                    {
                        Warning("calcLists could not find visible plane for {0}", outerPlane._object);
                        continue;
                    }

                    eraseList.Add(outerPlane._screenRect.FindIntersectingRect(visibleOuterPlane._screenRect));

                    for (int innerIndex = planeCount - 1; innerIndex >= 0; --innerIndex)
                    {
                        // "inner" just refers to the inner loop
                        Plane innerPlane = _planes[innerIndex];
                        Plane visibleInnerPlane = _visiblePlanes.FindByObject(innerPlane._object);

                        var rectCount = eraseList.Count;
                        for (var rectIndex = 0; rectIndex < rectCount; ++rectIndex)
                        {
                            int splitCount = SplitRects(eraseList[rectIndex], innerPlane._screenRect, outRects);
                            if (splitCount == 0)
                            {
                                // same priority, or relative priority between inner/outer changed
                                if ((visibleOuterPlane._priority - visibleInnerPlane?._priority) *
                                    (outerPlane._priority - innerPlane._priority) <= 0)
                                {
                                    if (outerPlane._priority <= innerPlane._priority)
                                    {
                                        eraseLists[innerIndex].Add(eraseList[rectIndex]);
                                    }
                                    else
                                    {
                                        eraseLists[outerIndex].Add(eraseList[rectIndex]);
                                    }
                                }

                                eraseList.RemoveAt(rectIndex);
                            }
                            else if (splitCount != -1)
                            {
                                for (int i = 0; i < splitCount; ++i)
                                {
                                    eraseList.Add(outRects[i]);
                                }

                                // same priority, or relative priority between inner/outer changed
                                if ((visibleOuterPlane._priority - visibleInnerPlane?._priority) *
                                    (outerPlane._priority - innerPlane._priority) <= 0)
                                {
                                    eraseList[rectIndex] =
                                        outerPlane._screenRect.FindIntersectingRect(innerPlane._screenRect);

                                    if (outerPlane._priority <= innerPlane._priority)
                                    {
                                        eraseLists[innerIndex].Add(eraseList[rectIndex]);
                                    }
                                    else
                                    {
                                        eraseLists[outerIndex].Add(eraseList[rectIndex]);
                                    }
                                }
                                eraseList.RemoveAt(rectIndex);
                            }
                        }
                        eraseList.Pack();
                    }
                }
            }

            for (var planeIndex = 0; planeIndex < planeCount; ++planeIndex)
            {
                Plane plane = _planes[planeIndex];
                Plane visiblePlane = _visiblePlanes.FindByObject(plane._object);

                if (!plane._screenRect.IsEmpty)
                {
                    if (plane._redrawAllCount != 0)
                    {
                        plane.RedrawAll(visiblePlane, _planes, drawLists[planeIndex], eraseLists[planeIndex]);
                    }
                    else
                    {
                        if (visiblePlane == null)
                        {
                            Error("Missing visible plane for source plane {0}", plane._object);
                        }

                        plane.CalcLists(visiblePlane, _planes, drawLists[planeIndex], eraseLists[planeIndex]);
                    }
                }
                else
                {
                    plane.DecrementScreenItemArrayCounts(visiblePlane, false);
                }

                if (plane._moved != 0)
                {
                    // the work for handling moved/resized planes was already done
                    // earlier in the function, we are just cleaning up now
                    --plane._moved;
                }

                if (plane._created != 0)
                {
                    _visiblePlanes.Add(new Plane(plane));
                    --plane._created;
                }
                else if (plane._updated != 0)
                {
                    visiblePlane.Assign(plane);
                    --plane._updated;
                }
            }

            // NOTE: SSCI only looks for kPlaneTypeTransparent, not
            // kPlaneTypeTransparentPicture
            if (foundTransparentPlane)
            {
                for (var planeIndex = 0; planeIndex < planeCount; ++planeIndex)
                {
                    for (var i = planeIndex + 1; i < planeCount; ++i)
                    {
                        if (_planes[i]._type == PlaneType.Transparent)
                        {
                            _planes[i].FilterUpEraseRects(drawLists[i], eraseLists[planeIndex]);
                        }
                    }

                    if (_planes[planeIndex]._type == PlaneType.Transparent)
                    {
                        for (int i = planeIndex - 1; i >= 0; --i)
                        {
                            _planes[i].FilterDownEraseRects(drawLists[i], eraseLists[i], eraseLists[planeIndex]);
                        }

                        if (eraseLists[planeIndex].Count > 0)
                        {
                            Error("Transparent plane's erase list not absorbed");
                        }
                    }

                    for (var i = planeIndex + 1; i < planeCount; ++i)
                    {
                        if (_planes[i]._type == PlaneType.Transparent)
                        {
                            _planes[i].FilterUpDrawRects(drawLists[i], drawLists[planeIndex]);
                        }
                    }
                }
            }
        }

        /**
         * Determines the parts of `r` that aren't overlapped by `other`.
         * Returns -1 if `r` and `other` have no intersection.
         * Returns number of returned parts (in `outRects`) otherwise.
         * (In particular, this returns 0 if `r` is contained in `other`.)
         */

        public static int SplitRects(Rect r, Rect other, Rect[] outRects)
        {
            if (!r.Intersects(other))
            {
                return -1;
            }

            int splitCount = 0;
            if (r.Top < other.Top)
            {
                outRects[splitCount] = r;
                outRects[splitCount].Bottom = other.Top;
                r.Top = other.Top;
                splitCount++;
            }

            if (r.Bottom > other.Bottom)
            {
                outRects[splitCount] = r;
                outRects[splitCount].Top = other.Bottom;
                r.Bottom = other.Bottom;
                splitCount++;
            }

            if (r.Left < other.Left)
            {
                outRects[splitCount] = r;
                outRects[splitCount].Right = other.Left;
                r.Left = other.Left;
                splitCount++;
            }

            if (r.Right > other.Right)
            {
                outRects[splitCount] = r;
                outRects[splitCount].Left = other.Right;
                splitCount++;
            }

            return splitCount;
        }

        private void RemapMarkRedraw()
        {
            foreach (var p in _planes)
            {
                p.RemapMarkRedraw();
            }
        }

        private void ShowBits()
        {
            if (_showList.Count == 0)
            {
                SciEngine.Instance.System.GraphicsManager.UpdateScreen();
                return;
            }

            foreach (var rect in _showList)
            {
                Rect rounded = new Rect(rect);
                // NOTE: SCI engine used BR-inclusive rects so used slightly
                // different masking here to ensure that the width of rects
                // was always even.
                rounded.Left &= ~1;
                rounded.Right = (short)((rounded.Right + 1) & ~1);
                _cursor.GonnaPaint(rounded);
            }

            _cursor.PaintStarting();

            foreach (var rect in _showList)
            {
                Rect rounded = new Rect(rect);
                // NOTE: SCI engine used BR-inclusive rects so used slightly
                // different masking here to ensure that the width of rects
                // was always even.
                rounded.Left &= ~1;
                rounded.Right = (short)((rounded.Right + 1) & ~1);

                BytePtr sourceBuffer = _currentBuffer.Pixels + rounded.Top * _currentBuffer.ScreenWidth + rounded.Left;

                // TODO: Sometimes transition screen items generate zero-dimension
                // show rectangles. Is this a bug?
                if (rounded.Width == 0 || rounded.Height == 0)
                {
                    Warning("Zero-dimension show rectangle ignored");
                    continue;
                }

                SciEngine.Instance.System.GraphicsManager.CopyRectToScreen(sourceBuffer, _currentBuffer.ScreenWidth, rounded.Left, rounded.Top, rounded.Width, rounded.Height);
            }

            _cursor.DonePainting();

            _showList.Clear();
            SciEngine.Instance.System.GraphicsManager.UpdateScreen();
        }

        public short KernelGetHighPlanePri()
        {
            return _planes.GetTopSciPlanePriority();
        }

        public Register KernelIsOnMe(Register @object, Point position, bool checkPixel)
        {
            Register planeObject = SciEngine.ReadSelector(_segMan, @object, o => o.plane);
            Plane plane = _visiblePlanes.FindByObject(planeObject);
            if (plane == null)
            {
                return Register.Make(0, 0);
            }

            ScreenItem screenItem = plane._screenItemList.FindByObject(@object);
            if (screenItem == null)
            {
                return Register.Make(0, 0);
            }

            // NOTE: The original engine passed a copy of the ScreenItem into isOnMe
            // as a hack around the fact that the screen items in `_visiblePlanes`
            // did not have their `_celObj` pointers cleared when their CelInfo was
            // updated by `Plane::decrementScreenItemArrayCounts`. We handle this
            // this more intelligently by clearing `_celObj` in the copy assignment
            // operator, which is only ever called by `decrementScreenItemArrayCounts`
            // anyway.
            return Register.Make(0, IsOnMe(screenItem, plane, position, checkPixel));
        }

        private bool IsOnMe(ScreenItem screenItem, Plane plane, Point position, bool checkPixel)
        {
            Point scaledPosition = new Point(position);
            var r1 = new Rational(_currentBuffer.ScreenWidth, _currentBuffer.ScriptWidth);
            var r2 = new Rational(_currentBuffer.ScreenHeight, _currentBuffer.ScriptHeight);
            Helpers.Mulru(ref scaledPosition, ref r1, ref r2);
            scaledPosition.X += plane._planeRect.Left;
            scaledPosition.Y += plane._planeRect.Top;

            if (!screenItem._screenRect.Contains(scaledPosition))
            {
                return false;
            }

            if (checkPixel)
            {
                CelObj celObj = screenItem.GetCelObj();

                bool mirrorX = screenItem._mirrorX ^ celObj._mirrorX;

                scaledPosition.X -= screenItem._scaledPosition.X;
                scaledPosition.Y -= screenItem._scaledPosition.Y;

                r1 = new Rational(celObj._xResolution, _currentBuffer.ScreenWidth);
                r2 = new Rational(celObj._yResolution, _currentBuffer.ScreenHeight);
                Helpers.Mulru(ref scaledPosition, ref r1, ref r2);

                if (screenItem._scale.signal != ScaleSignals32.kScaleSignalNone && screenItem._scale.x != 0 &&
                    screenItem._scale.y != 0)
                {
                    scaledPosition.X = (short)(scaledPosition.X * 128 / screenItem._scale.x);
                    scaledPosition.Y = (short)(scaledPosition.Y * 128 / screenItem._scale.y);
                }

                byte pixel = celObj.ReadPixel((ushort)scaledPosition.X, (ushort)scaledPosition.Y, mirrorX);
                return pixel != celObj._skipColor;
            }

            return true;
        }

        public void FrameOut(bool shouldShowBits, Rect eraseRect = new Rect())
        {
            RobotDecoder robotPlayer = SciEngine.Instance._video32.RobotPlayer;
            bool robotIsActive = robotPlayer.Status != RobotStatus.kRobotStatusUninitialized;

            if (robotIsActive)
            {
                robotPlayer.DoRobot();
            }

            // NOTE: The original engine allocated these as static arrays of 100
            // pointers to ScreenItemList / RectList
            var screenItemLists = new Array<DrawList>(() => new DrawList());
            var eraseLists = new Array<RectList>(() => new RectList());

            screenItemLists.Resize(_planes.Size);
            eraseLists.Resize(_planes.Size);

            if (SciEngine.Instance._gfxRemap32.RemapCount > 0 && _remapOccurred)
            {
                RemapMarkRedraw();
            }

            CalcLists(screenItemLists, eraseLists, eraseRect);

            foreach (var list in screenItemLists)
            {
                list.Sort();
            }

            foreach (var list in screenItemLists)
            {
                foreach (var drawItem in list)
                {
                    drawItem.screenItem.GetCelObj().SubmitPalette();
                }
            }

            _remapOccurred = _palette.UpdateForFrame();

            // NOTE: SCI engine set this to false on each loop through the
            // planelist iterator below. Since that is a waste, we only set
            // it once.
            _frameNowVisible = false;

            for (var i = 0; i < _planes.Size; ++i)
            {
                DrawEraseList(eraseLists[i], _planes[i]);
                DrawScreenItemList(screenItemLists[i]);
            }

            if (robotIsActive)
            {
                robotPlayer.FrameAlmostVisible();
            }

            _palette.UpdateHardware(!shouldShowBits);

            if (shouldShowBits)
            {
                ShowBits();
            }

            _frameNowVisible = true;

            if (robotIsActive)
            {
                robotPlayer.FrameNowVisible();
            }
        }

        public void KernelUpdatePlane(Register @object)
        {
            Plane plane = _planes.FindByObject(@object);
            if (plane == null)
            {
                Error("kUpdatePlane: Plane {0} not found", @object);
            }

            plane.Update(@object);
            UpdatePlane(plane);
        }

        public void KernelUpdateScreenItem(Register @object)
        {
            Register magnifierObject = SciEngine.ReadSelector(_segMan, @object, o => o.magnifier);
            if (magnifierObject.IsNull)
            {
                Register planeObject = SciEngine.ReadSelector(_segMan, @object, o => o.plane);
                Plane plane = _planes.FindByObject(planeObject);
                if (plane == null)
                {
                    Error("kUpdateScreenItem: Plane {0} not found for screen item {1}", planeObject, @object);
                }

                var screenItem = plane._screenItemList.FindByObject(@object);
                if (screenItem == null)
                {
                    Error("kUpdateScreenItem: Screen item {0} not found in plane {1}", @object, planeObject);
                }

                screenItem.Update(@object);
            }
            else
            {
                Error(
                    "Magnifier view is not known to be used by any game. Please submit a bug report with details about the game you were playing and what you were doing that triggered this error. Thanks!");
            }
        }

        public void AddScreenItem(ScreenItem screenItem)
        {
            Plane plane = _planes.FindByObject(screenItem._plane);
            if (plane == null)
            {
                Error("GfxFrameout::AddScreenItem: Could not find plane {0} for screen item {1}", screenItem._plane,
                    screenItem._object);
            }
            plane._screenItemList.Add(screenItem);
        }

        public void UpdateScreenItem(ScreenItem screenItem)
        {
            // TODO: In SCI3+ this will need to go through Plane
            //	Plane *plane = _planes.findByObject(screenItem._plane);
            //	if (plane == nullptr) {
            //		error("GfxFrameout::updateScreenItem: Could not find plane %04x:%04x for screen item %04x:%04x", PRINT_REG(screenItem._plane), PRINT_REG(screenItem._object));
            //	}
            screenItem.Update();
        }

        public void KernelAddPicAt(Register planeObject, int pictureId, short x, short y, bool mirrorX, bool deleteDuplicate)
        {
            Plane plane = _planes.FindByObject(planeObject);
            if (plane == null)
            {
                Error("kAddPicAt: Plane {0} not found", planeObject);
            }
            plane.AddPic(pictureId, new Point(x, y), mirrorX, deleteDuplicate);
        }

        public void KernelMovePlaneItems(Register @object, short deltaX, short deltaY, bool scrollPics)
        {
            Plane plane = _planes.FindByObject(@object);
            if (plane == null)
            {
                Error("kMovePlaneItems: Plane {0} not found", @object);
            }

            plane.ScrollScreenItems(deltaX, deltaY, scrollPics);

            foreach (var screenItem in plane._screenItemList)
            {
                // If object is a number, the screen item from the
                // engine, not a script, and should be ignored
                if (screenItem._object.IsNumber)
                {
                    continue;
                }

                if (deltaX != 0)
                {
                    SciEngine.WriteSelectorValue(_segMan, screenItem._object, o => o.x,
                        (ushort)(SciEngine.ReadSelectorValue(_segMan, screenItem._object, o => o.x) + deltaX));
                }

                if (deltaY != 0)
                {
                    SciEngine.WriteSelectorValue(_segMan, screenItem._object, o => o.y,
                        (ushort)(SciEngine.ReadSelectorValue(_segMan, screenItem._object, o => o.y) + deltaY));
                }
            }
        }

        public bool KernelSetNowSeen(Register screenItemObject)
        {
            Register planeObject = SciEngine.ReadSelector(_segMan, screenItemObject, o => o.plane);

            Plane plane = _planes.FindByObject(planeObject);
            if (plane == null)
            {
                Error("kSetNowSeen: Plane {0} not found for screen item {1}", planeObject, screenItemObject);
            }

            ScreenItem screenItem = plane._screenItemList.FindByObject(screenItemObject);
            if (screenItem == null)
            {
                return false;
            }

            Rect result = screenItem.GetNowSeenRect(plane);
            SciEngine.WriteSelectorValue(_segMan, screenItemObject, o => o.nsLeft, (ushort)result.Left);
            SciEngine.WriteSelectorValue(_segMan, screenItemObject, o => o.nsTop, (ushort)result.Top);
            SciEngine.WriteSelectorValue(_segMan, screenItemObject, o => o.nsRight, (ushort)(result.Right - 1));
            SciEngine.WriteSelectorValue(_segMan, screenItemObject, o => o.nsBottom, (ushort)(result.Bottom - 1));
            return true;
        }

        public void ShakeScreen(short numShakes, ShakeDirection direction)
        {
            if (direction.HasFlag(ShakeDirection.Horizontal))
            {
                // Used by QFG4 room 750
                Warning("TODO: Horizontal shake not implemented");
                return;
            }

            while ((numShakes--) != 0)
            {
                if (direction.HasFlag(ShakeDirection.Vertical))
                {
                    SciEngine.Instance.System.GraphicsManager.ShakePosition = _isHiRes ? 8 : 4;
                }

                SciEngine.Instance.System.GraphicsManager.UpdateScreen();
                SciEngine.Instance.EngineState.Wait(3);

                if (direction.HasFlag(ShakeDirection.Vertical))
                {
                    SciEngine.Instance.System.GraphicsManager.ShakePosition = 0;
                }

                SciEngine.Instance.System.GraphicsManager.UpdateScreen();
                SciEngine.Instance.EngineState.Wait(3);
            }
        }
    }
}

#endif