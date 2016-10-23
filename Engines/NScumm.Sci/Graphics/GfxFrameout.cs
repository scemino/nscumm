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
using System.Collections.Generic;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

#if ENABLE_SCI32

namespace NScumm.Sci.Graphics
{
    public enum ShakeDirection
    {
        Vertical = 1,
        Horizontal = 2
    }

// TODO: Verify display styles and adjust names appropriately for
    // types 1 through 12 & 15 (others are correct)
    // Names should be:
    // * VShutterIn, VShutterOut
    // * HShutterIn, HShutterOut
    // * WipeLeft, WipeRight, WipeDown, WipeUp
    // * PixelDissolve
    // * ShutDown and Kill? (and Plain and Fade?)
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
        kShowStyle11 = 11,
        kShowStyle12 = 12,
        kShowStyleFadeOut = 13,
        kShowStyleFadeIn = 14,
        // TODO: Only in SCI3
        kShowStyleUnknown = 15
    }

    /**
     * Show styles represent transitions applied to draw planes.
     * One show style per plane can be active at a time.
     */

    internal class ShowStyleEntry
    {
        /**
         * The ID of the plane this show style belongs to.
         * In SCI2.1mid (at least SQ6), per-plane transitions
         * were removed and a single plane ID is used.
         */
        public Register plane;

        /**
         * The type of the transition.
         */
        public ShowStyleType type;

        // TODO: This name is probably incorrect
        public bool fadeUp;

        /**
         * The number of steps for the show style.
         */
        public short divisions;

        // NOTE: This property exists from SCI2 through at least
        // SCI2.1mid but is never used in the actual processing
        // of the styles?
        public int unknownC;

        /**
         * The color used by transitions that draw CelObjColor
         * screen items. -1 for transitions that do not draw
         * screen items.
         */
        public short color;

        // TODO: Probably uint32
        // TODO: This field probably should be used in order to
        // provide time-accurate processing of show styles. In the
        // actual SCI engine (at least 2–2.1mid) it appears that
        // style transitions are drawn “as fast as possible”, one
        // step per loop, even though this delay field exists
        public int delay;

        // TODO: Probably bool, but never seems to be true?
        public int animate;

        /**
         * The wall time at which the next step of the animation
         * should execute.
         */
        public int nextTick;

        /**
         * During playback of the show style, the current step
         * (out of divisions).
         */
        public int currentStep;

        /**
         * The next show style.
         */
        public ShowStyleEntry next;

        /**
         * Whether or not this style has finished running and
         * is ready for disposal.
         */
        public bool processed;

        //
        // Engine specific properties for SCI2.1mid through SCI3
        //

        /**
         * The number of entries in the fadeColorRanges array.
         */
        public byte fadeColorRangesCount;

        /**
         * A pointer to an dynamically sized array of palette
         * indexes, in the order [ fromColor, toColor, ... ].
         * Only colors within this range are transitioned.
         */
        public Ptr<ushort> fadeColorRanges;
    }

    internal class GfxFrameout
    {
        /**
         * Whether palMorphFrameOut should be used instead of
         * frameOut for rendering. Used by kMorphOn to
         * explicitly enable palMorphFrameOut for one frame.
         */
        public bool _palMorphIsOn;
        public bool _isHiRes;
        private GfxCoordAdjuster32 _coordAdjuster;
        private GfxPalette32 _palette;
        private ResourceManager _resMan;
        private GfxScreen _screen;
        private SegManager _segMan;
        /**
         * Optimization to avoid the more expensive object name
         * comparision on every call to kAddScreenItem and
         * kRemoveScreenItem.
         */
        private bool _benchmarkingFinished;
        /**
         * Whether or not calls to kFrameOut should be framerate
         * limited to 60fps.
         */
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
         * TODO: Documentation
         */
        private sbyte[] _styleRanges = new sbyte[256];

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
        private RectList _showList;

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

        private Ptr<int> _dissolveSequenceSeeds;
        private Ptr<short> _defaultDivisions;
        private Ptr<short> _defaultUnknownC;

        /**
         * TODO: Documentation
         */
        private ShowStyleEntry _showStyles;

        public Buffer CurrentBuffer => _currentBuffer;
        public PlaneList VisiblePlanes => _visiblePlanes;

        public GfxFrameout(SegManager segMan, ResourceManager resMan, GfxCoordAdjuster coordAdjuster, GfxScreen screen, GfxPalette32 palette, GfxCursor32 gfxCursor32)
        {
            _palette = palette;
            _visiblePlanes = new PlaneList();
            _resMan = resMan;
            _screen = screen;
            _segMan = segMan;
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
            _currentBuffer = new Buffer(screen.DisplayWidth, screen.DisplayHeight, BytePtr.Null);
            _screenRect = new Rect((short) screen.DisplayWidth, (short) screen.DisplayHeight);
            _currentBuffer.SetPixels(new byte[screen.DisplayWidth * screen.DisplayHeight]);

            for (int i = 0; i < 236; i += 2)
            {
                _styleRanges[i] = 0;
                _styleRanges[i + 1] = -1;
            }
            for (int i = 236; i < _styleRanges.Length; ++i)
            {
                _styleRanges[i] = 0;
            }

            // TODO: Make hires detection work uniformly across all SCI engine
            // versions (this flag is normally passed by SCI::MakeGraphicsMgr
            // to the GraphicsMgr constructor depending upon video configuration,
            // so should be handled upstream based on game configuration instead
            // of here)
            if (ResourceManager.GetSciVersion() >= SciVersion.V2_1_EARLY && _resMan.DetectHires())
            {
                _isHiRes = true;
            }

            if (ResourceManager.GetSciVersion() < SciVersion.V2_1_MIDDLE)
            {
                _dissolveSequenceSeeds = dissolveSequences[0];
                _defaultDivisions = divisionsDefaults[0];
                _defaultUnknownC = unknownCDefaults[0];
            }
            else
            {
                _dissolveSequenceSeeds = dissolveSequences[1];
                _defaultDivisions = divisionsDefaults[1];
                _defaultUnknownC = unknownCDefaults[1];
            }

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

            // TODO: Nothing in the renderer really uses this. Currently,
            // the cursor renderer does, and kLocalToGlobal/kGlobalToLocal
            // do, but in the real engine (1) the cursor is handled in
            // frameOut, and (2) functions do a very simple lookup of the
            // plane and arithmetic with the plane's gameRect. In
            // principle, CoordAdjuster could be reused for
            // convertGameRectToPlaneRect, but it is not super clear yet
            // what the benefit would be to do that.
            _coordAdjuster = (GfxCoordAdjuster32) coordAdjuster;

            // TODO: Script resolution is hard-coded per game;
            // also this must be set or else the engine will crash
            _coordAdjuster.SetScriptsResolution(_currentBuffer.ScriptWidth, _currentBuffer.ScriptHeight);
        }


        public void DeleteScreenItem(ScreenItem screenItem)
        {
            throw new NotImplementedException();
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

        public void DeletePlane(Plane plane)
        {
            throw new NotImplementedException();
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
                new Plane(new Rect((short) _currentBuffer.ScriptWidth, (short) _currentBuffer.ScriptHeight));
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
            short viewId = (short) SciEngine.ReadSelectorValue(_segMan, @object, o => o.view);
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
                _planes.Remove(plane);
            }
            else
            {
                plane._created = 0;
                plane._deleted = SciEngine.Instance._gfxFrameout.GetScreenCount();
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

        public void KernelFrameOut(bool shouldShowBits)
        {
            if (_showStyles != null)
            {
                ProcessShowStyles();
            }
            else if (_palMorphIsOn)
            {
                PalMorphFrameOut(_styleRanges, null);
                _palMorphIsOn = false;
            }
            else
            {
// TODO: Window scroll
//		if (g_PlaneScroll) {
//			processScrolls();
//		}

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
                throttleTime = 17;
                _throttleState = 0;
            }
            else
            {
                throttleTime = 16;
                ++_throttleState;
            }

            SciEngine.Instance.EngineState.SpeedThrottler(throttleTime);
            SciEngine.Instance.EngineState._throttleTrigger = true;
        }


        // NOTE: Different version of SCI engine support different show styles
        // SCI2 implements 0, 1/3/5/7/9, 2/4/6/8/10, 11, 12, 13, 14
        // SCI2.1 implements 0, 1/2/3/4/5/6/7/8/9/10/11/12/15, 13, 14
        // SCI3 implements 0, 1/3/5/7/9, 2/4/6/8/10, 11, 12/15, 13, 14
        // TODO: Sierra code needs to be replaced with code that uses the
        // computed entry.delay property instead of just counting divisors,
        // as the latter is machine-speed-dependent and leads to wrong
        // transition speeds
        private void ProcessShowStyles()
        {
            uint now = SciEngine.Instance.TickCount;

            bool continueProcessing;

            // TODO: Change to bool? Engine uses inc to set the value to true,
            // but there does not seem to be any reason to actually count how
            // many times it was set
            int doFrameOut;
            do
            {
                continueProcessing = false;
                doFrameOut = 0;
                ShowStyleEntry showStyle = _showStyles;
                while (showStyle != null)
                {
                    bool retval = false;

                    if (showStyle.animate == 0)
                    {
                        ++doFrameOut;
                    }

                    if (showStyle.nextTick < now || showStyle.animate == 0)
                    {
                        // TODO: Different versions of SCI use different processors!
                        // This is the SQ6/KQ7/SCI2.1mid table.
                        switch (showStyle.type)
                        {
                            case ShowStyleType.kShowStyleNone:
                            {
                                retval = ProcessShowStyleNone(showStyle);
                                break;
                            }
                            case ShowStyleType.kShowStyleHShutterOut:
                            case ShowStyleType.kShowStyleVShutterOut:
                            case ShowStyleType.kShowStyleWipeLeft:
                            case ShowStyleType.kShowStyleWipeUp:
                            case ShowStyleType.kShowStyleIrisOut:
                            case ShowStyleType.kShowStyleHShutterIn:
                            case ShowStyleType.kShowStyleVShutterIn:
                            case ShowStyleType.kShowStyleWipeRight:
                            case ShowStyleType.kShowStyleWipeDown:
                            case ShowStyleType.kShowStyleIrisIn:
                            case ShowStyleType.kShowStyle11:
                            case ShowStyleType.kShowStyle12:
                            case ShowStyleType.kShowStyleUnknown:
                            {
                                retval = ProcessShowStyleMorph(showStyle);
                                break;
                            }
                            case ShowStyleType.kShowStyleFadeOut:
                            {
                                retval = ProcessShowStyleFade(-1, showStyle);
                                break;
                            }
                            case ShowStyleType.kShowStyleFadeIn:
                            {
                                retval = ProcessShowStyleFade(1, showStyle);
                                break;
                            }
                        }
                    }

                    if (!retval)
                    {
                        continueProcessing = true;
                    }

                    if (retval && showStyle.processed)
                    {
                        showStyle = DeleteShowStyleInternal(showStyle);
                    }
                    else
                    {
                        showStyle = showStyle.next;
                    }
                }

                if (doFrameOut != 0)
                {
                    FrameOut(true);

                    // TODO: Transitions without the “animate” flag are too
                    // fast, but the throttle value is arbitrary. Someone on
                    // real hardware probably needs to test what the actual
                    // speed of these transitions should be
                    EngineState state = SciEngine.Instance.EngineState;
                    state.SpeedThrottler(33);
                    state._throttleTrigger = true;
                }
            } while (continueProcessing && doFrameOut != 0);
        }

        private ShowStyleEntry DeleteShowStyleInternal(ShowStyleEntry showStyle)
        {
            ShowStyleEntry lastEntry = null;

            for (ShowStyleEntry testEntry = _showStyles; testEntry != null; testEntry = testEntry.next)
            {
                if (testEntry == showStyle)
                {
                    break;
                }
                lastEntry = testEntry;
            }

            if (lastEntry == null)
            {
                _showStyles = showStyle.next;
                lastEntry = _showStyles;
            }
            else
            {
                lastEntry.next = showStyle.next;
            }

            // TODO: Verify that this is the correct entry to return
            // for the loop in processShowStyles to work correctly
            return lastEntry;
        }


        private bool ProcessShowStyleNone(ShowStyleEntry showStyle)
        {
            if (showStyle.fadeUp)
            {
                _palette.SetFade(100, 0, 255);
            }
            else
            {
                _palette.SetFade(0, 0, 255);
            }

            showStyle.processed = true;
            return true;
        }

        private bool ProcessShowStyleMorph(ShowStyleEntry showStyle)
        {
            PalMorphFrameOut(_styleRanges, showStyle);
            showStyle.processed = true;
            return true;
        }

        // TODO: Normalise use of 'entry' vs 'showStyle'
        private bool ProcessShowStyleFade(int direction, ShowStyleEntry showStyle)
        {
            bool unchanged = true;
            if (showStyle.currentStep < showStyle.divisions)
            {
                int percent;
                if (direction <= 0)
                {
                    percent = showStyle.divisions - showStyle.currentStep - 1;
                }
                else
                {
                    percent = showStyle.currentStep;
                }

                percent *= 100;
                percent /= showStyle.divisions - 1;

                if (showStyle.fadeColorRangesCount > 0)
                {
                    for (int i = 0, len = showStyle.fadeColorRangesCount; i < len; i += 2)
                    {
                        _palette.SetFade((ushort) percent, (byte) showStyle.fadeColorRanges[i],
                            showStyle.fadeColorRanges[i + 1]);
                    }
                }
                else
                {
                    _palette.SetFade((ushort) percent, 0, 255);
                }

                ++showStyle.currentStep;
                showStyle.nextTick += showStyle.delay;
                unchanged = false;
            }

            if (showStyle.currentStep >= showStyle.divisions && unchanged)
            {
                if (direction > 0)
                {
                    showStyle.processed = true;
                }

                return true;
            }

            return false;
        }

        private void PalMorphFrameOut(sbyte[] styleRanges, ShowStyleEntry showStyle)
        {
            Palette sourcePalette = new Palette(_palette.NextPalette);
            AlterVmap(sourcePalette, sourcePalette, -1, styleRanges);

            short prevRoom = SciEngine.Instance.EngineState.variables[Vm.VAR_GLOBAL][12].ToInt16();

            Rect rect = new Rect((short) _screen.DisplayWidth, (short) _screen.DisplayHeight);
            _showList.Add(rect);
            ShowBits();

            // NOTE: The original engine allocated these as static arrays of 100
            // pointers to ScreenItemList / RectList
            var screenItemLists = new List<DrawList>(_planes.Count);
            var eraseLists = new List<RectList>(_planes.Count);

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

            for (var i = 0; i < _planes.Count; ++i)
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
                    // TODO: Limiting range 72 to 103 is NOT present in every game
                    if (styleRanges[i] == -1 || (styleRanges[i] == 0 && i > 71 && i < 104))
                    {
                        sourcePalette.colors[i] = nextPalette.colors[i];
                        sourcePalette.colors[i].used = 1;
                    }
                }
            }

            _palette.Submit(sourcePalette);
            _palette.UpdateFFrame();
            _palette.UpdateHardware();
            AlterVmap(nextPalette, sourcePalette, 1, _styleRanges);

            if (showStyle != null && showStyle.type != ShowStyleType.kShowStyleUnknown)
            {
// TODO: SCI2.1mid transition effects
//		processEffects();
                Warning("Transition {0} not implemented!", showStyle.type);
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

            for (var i = 0; i < _planes.Count; ++i)
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

                    clut[paletteIndex] = (byte) minDiffIndex;
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

                    clut[paletteIndex] = (byte) minDiffIndex;
                }
            }

            // NOTE: This is currBuffer.ptr in SCI engine
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

        void MergeToShowList(Rect drawRect, RectList showList, int overdrawThreshold)
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

        int SplitRectsForRender(Rect middleRect, Rect showRect, Rect[] outRects)
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

        // NOTE: The third rectangle parameter is only ever given a non-empty rect
        // by VMD code, via `frameOut`
        private void CalcLists(IList<DrawList> drawLists, IList<RectList> eraseLists)
        {
            CalcLists(drawLists, eraseLists, new Rect());
        }

        private void CalcLists(IList<DrawList> drawLists, IList<RectList> eraseLists, Rect eraseRect)
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

            var planeCount = _planes.Count;
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
            planeCount = _planes.Count;

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
                    visiblePlane = plane;
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
            foreach (var rect in _showList)
            {
                Rect rounded = new Rect(rect);
                // NOTE: SCI engine used BR-inclusive rects so used slightly
                // different masking here to ensure that the width of rects
                // was always even.
                rounded.Left &= ~1;
                rounded.Right = (short) ((rounded.Right + 1) & ~1);

                // TODO:
                // _cursor.GonnaPaint(rounded);
            }

            // TODO:
            // _cursor.PaintStarting();

            foreach (var rect in _showList)
            {
                Rect rounded = new Rect(rect);
                // NOTE: SCI engine used BR-inclusive rects so used slightly
                // different masking here to ensure that the width of rects
                // was always even.
                rounded.Left &= ~1;
                rounded.Right = (short) ((rounded.Right + 1) & ~1);

                BytePtr sourceBuffer = new BytePtr(_currentBuffer.Pixels,
                    rounded.Top * _currentBuffer.ScreenWidth + rounded.Left);

                SciEngine.Instance.System.GraphicsManager.CopyRectToScreen(sourceBuffer, _currentBuffer.ScreenWidth,
                    rounded.Left, rounded.Top,
                    rounded.Width, rounded.Height);
            }

            // TODO:
            // _cursor.DonePainting();

            _showList.Clear();
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
                    scaledPosition.X = (short) (scaledPosition.X * 128 / screenItem._scale.x);
                    scaledPosition.Y = (short) (scaledPosition.Y * 128 / screenItem._scale.y);
                }

                byte pixel = celObj.ReadPixel((ushort) scaledPosition.X, (ushort) scaledPosition.Y, mirrorX);
                return pixel != celObj._transparentColor;
            }

            return true;
        }

        private ShowStyleEntry FindShowStyleForPlane(Register planeObj)
        {
            ShowStyleEntry entry = _showStyles;
            while (entry != null)
            {
                if (entry.plane == planeObj)
                {
                    break;
                }
                entry = entry.next;
            }

            return entry;
        }

        public void FrameOut(bool shouldShowBits, Rect eraseRect = new Rect())
        {
            // TODO: Robot
            //	if (_robot != nullptr) {
            //		_robot.doRobot();
            //	}

            // NOTE: The original engine allocated these as static arrays of 100
            // pointers to ScreenItemList / RectList
            List<DrawList> screenItemLists = new List<DrawList>(_planes.Count);
            List<RectList> eraseLists = new List<RectList>(_planes.Count);

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

            for (var i = 0; i < _planes.Count; ++i)
            {
                DrawEraseList(eraseLists[i], _planes[i]);
                DrawScreenItemList(screenItemLists[i]);
            }

// TODO: Robot
//	if (_robot != nullptr) {
//		_robot.frameAlmostVisible();
//	}

            _palette.UpdateHardware(!shouldShowBits);

            if (shouldShowBits)
            {
                ShowBits();
            }

            _frameNowVisible = true;

// TODO: Robot
//	if (_robot != nullptr) {
//		robot.frameNowVisible();
//	}
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

        public void KernelAddPicAt(Register planeObject, int pictureId, short x, short y, bool mirrorX)
        {
            Plane plane = _planes.FindByObject(planeObject);
            if (plane == null)
            {
                Error("kAddPicAt: Plane {0} not found", planeObject);
            }
            plane.AddPic(pictureId, new Point(x, y), mirrorX);
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
                        (ushort) (SciEngine.ReadSelectorValue(_segMan, screenItem._object, o => o.x) + deltaX));
                }

                if (deltaY != 0)
                {
                    SciEngine.WriteSelectorValue(_segMan, screenItem._object, o => o.y,
                        (ushort) (SciEngine.ReadSelectorValue(_segMan, screenItem._object, o => o.y) + deltaY));
                }
            }
        }

        public void KernelSetPalStyleRange(byte fromColor, byte toColor)
        {
            if (toColor > fromColor)
            {
                return;
            }

            for (int i = fromColor; i < toColor; ++i)
            {
                _styleRanges[i] = 0;
            }
        }

        public bool KernelSetNowSeen(Register screenItemObject)
        {
            Register planeObject = SciEngine.ReadSelector(_segMan, screenItemObject, o=>o.plane);

            Plane plane = _planes.FindByObject(planeObject);
            if (plane == null) {
                Error("kSetNowSeen: Plane {0} not found for screen item {1}", planeObject, screenItemObject);
            }

            ScreenItem screenItem = plane._screenItemList.FindByObject(screenItemObject);
            if (screenItem == null) {
                return false;
            }

            Rect result = screenItem.GetNowSeenRect(plane);
            SciEngine.WriteSelectorValue(_segMan, screenItemObject, o=>o.nsLeft, (ushort) result.Left);
            SciEngine.WriteSelectorValue(_segMan, screenItemObject, o=>o.nsTop, (ushort) result.Top);
            SciEngine.WriteSelectorValue(_segMan, screenItemObject, o=>o.nsRight, (ushort) (result.Right - 1));
            SciEngine.WriteSelectorValue(_segMan, screenItemObject, o=>o.nsBottom, (ushort) (result.Bottom - 1));
            return true;
        }

        public void ShakeScreen(short toInt16, ShakeDirection shakeDirection)
        {
            throw new NotImplementedException();
        }
    }
}

#endif