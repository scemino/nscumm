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

#if ENABLE_SCI32

namespace NScumm.Sci.Graphics
{
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
        private bool _isHiRes;
        private GfxCoordAdjuster32 _coordAdjuster;
        private GfxPalette32 _palette;
        private ResourceManager _resMan;
        private GfxScreen _screen;
        private SegManager _segMan;

        private static readonly int[][] dissolveSequences = {
            /* SCI2.1early- */ new []{ 3, 6, 12, 20, 48, 96, 184, 272, 576, 1280, 3232, 6912, 13568, 24576, 46080 },
            /* SCI2.1mid+ */   new []{ 0, 0, 3, 6, 12, 20, 48, 96, 184, 272, 576, 1280, 3232, 6912, 13568, 24576, 46080, 73728, 132096, 466944 }
        };

        private static readonly short[][] divisionsDefaults = {
            /* SCI2.1early- */ new short[]{ 1, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 40, 40, 101, 101 },
            /* SCI2.1mid+ */   new short[]{ 1, 20, 20, 20, 20, 10, 10, 10, 10, 20, 20,  6, 10, 101, 101, 2 }
        };

        private static readonly short[][] unknownCDefaults = {
            /* SCI2.1early- */ new short[]{ 0,  1,  1,  2,  2,  3,  3,  4,  4,  5,  5,  0,  0,   0,   0 },
            /* SCI2.1mid+ */   new short[]{ 0,  2,  2,  3,  3,  4,  4,  5,  5,  6,  6,  0,  0,   7,   7, 0 }
        };

        /**
         * State tracker to provide more accurate 60fps
         * video throttling.
         */
        private byte _throttleState;

        /**
         * TODO: Documentation
         */
        private sbyte[] _styleRanges =new sbyte[256];

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
        private bool _frameNowVisible;

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
        private List<Rect> _showList;

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
        private List<Plane> _visiblePlanes;

        /**
         * The list of planes (i.e. layers) that have been added
         * to the screen.
         *
         * @note This field is on `GraphicsMgr.screen` in SCI
         * engine.
         */
        private readonly PlaneList _planes=new PlaneList();

        private Ptr<int> _dissolveSequenceSeeds;
        private Ptr<short> _defaultDivisions;
        private Ptr<short> _defaultUnknownC;

        public Buffer CurrentBuffer => _currentBuffer;

        public GfxFrameout(SegManager segMan, ResourceManager resMan, GfxCoordAdjuster coordAdjuster, GfxScreen screen, GfxPalette32 palette)
        {
            _palette = palette;
            _resMan=resMan;
            _screen=screen;
            _segMan=segMan;
            _currentBuffer = new Buffer(screen.DisplayWidth, screen.DisplayHeight, null);
            _screenRect = new Rect((short)screen.DisplayWidth, (short)screen.DisplayHeight);
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

        public void DeletePlane(Plane plane)
        {
            throw new NotImplementedException();
        }

        public void FrameOut(bool p0)
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
            Plane initPlane = new Plane(new Rect((short) _currentBuffer.ScriptWidth, (short) _currentBuffer.ScriptHeight));
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
    }
}

#endif
