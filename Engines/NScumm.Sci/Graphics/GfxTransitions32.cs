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

using NScumm.Core.Graphics;
using NScumm.Sci.Engine;
using System.Collections.Generic;
using static NScumm.Core.DebugHelper;
using System;
using System.Linq;

namespace NScumm.Sci.Graphics
{
    /**
 * Show styles represent transitions applied to draw planes.
 * One show style per plane can be active at a time.
 */
    class PlaneShowStyle
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

        /**
         * When true, the show style is an entry transition
         * to a new room. When false, it is an exit
         * transition away from an old room.
         */
        public bool fadeUp;

        /**
         * The number of steps for the show style.
         */
        public short divisions;

        /**
         * The color used by transitions that draw CelObjColor
         * screen items. -1 for transitions that do not draw
         * screen items.
         */
        public short color;

        // TODO: Probably uint
        // TODO: This field probably should be used in order to
        // provide time-accurate processing of show styles. In the
        // actual SCI engine (at least 2–2.1mid) it appears that
        // style transitions are drawn “as fast as possible”, one
        // step per loop, even though this delay field exists
        public int delay;

        // TODO: Probably bool, but never seems to be true?
        public bool animate;

        /**
         * The wall time at which the next step of the animation
         * should execute.
         */
        public uint nextTick;

        /**
         * During playback of the show style, the current step
         * (out of divisions).
         */
        public int currentStep;

        /**
         * Whether or not this style has finished running and
         * is ready for disposal.
         */
        public bool processed;

        //
        // Engine specific properties for SCI2.1early
        //

        /**
         * A list of screen items, each representing one
         * block of a wipe transition.
         */
        public ScreenItem[] screenItems;

        /**
         * For wipe transitions, the number of edges with a
         * moving wipe (1, 2, or 4).
         */
        public byte numEdges;

        /**
         * The dimensions of the plane, in game script
         * coordinates.
         */
        public short width, height;

        /**
         * For pixel dissolve transitions, the screen item
         * used to render the transition.
         */
        public ScreenItem bitmapScreenItem;

        /**
         * For pixel dissolve transitions, the bitmap used
         * to render the transition.
         */
        public Register bitmap;

        /**
         * The bit mask used by pixel dissolve transitions.
         */
        public uint dissolveMask;

        /**
         * The first pixel that was dissolved in a pixel
         * dissolve transition.
         */
        public uint firstPixel;

        /**
         * The last pixel that was dissolved. Once all
         * pixels have been dissolved, `pixel` will once
         * again equal `firstPixel`.
         */
        public uint pixel;

        //
        // Engine specific properties for SCI2.1mid through SCI3
        //

        /// <summary>
        /// The number of entries in the fadeColorRanges array.
        /// </summary>
        public byte fadeColorRangesCount;

        /**
         * A pointer to an dynamically sized array of palette
         * indexes, in the order [ fromColor, toColor, ... ].
         * Only colors within this range are transitioned.
         */
        public ushort[] fadeColorRanges;
    }

    /// <summary>
    /// PlaneScroll describes a transition between two different
    /// pictures within a single plane.
    /// </summary>
    class PlaneScroll
    {
        /**
         * The ID of the plane to be scrolled.
         */
        public Register plane;

        /**
         * The current position of the scroll.
         */
        public short x, y;

        /**
         * The distance that should be scrolled. Only one of
         * `deltaX` or `deltaY` may be set.
         */
        public short deltaX, deltaY;

        /**
         * The pic that should be created and scrolled into
         * view inside the plane.
         */
        public int newPictureId;

        /**
         * The picture that should be scrolled out of view
         * and deleted from the plane.
         */
        public int oldPictureId;

        /**
         * If true, the scroll animation is interleaved
         * with other updates to the graphics. If false,
         * the scroll will be exclusively animated until
         * it is finished.
         */
        public bool animate;

        /**
         * The tick after which the animation will start.
         */
        public uint startTick;
    }

    class GfxTransitions32
    {
        private SegManager _segMan;
        private sbyte _throttleState;
        /// <summary>
        /// A map of palette entries that can be morphed
        /// by the Morph show style.
        /// </summary>
        public sbyte[] _styleRanges = new sbyte[256];
        /// <summary>
        /// Default sequence values for pixel dissolve
        /// transition bit masks.
        /// </summary>
        private int[] _dissolveSequenceSeeds;

        /// <summary>
        /// The list of PlaneShowStyles that are
        /// currently active.
        /// </summary>
        private List<PlaneShowStyle> _showStyles = new List<PlaneShowStyle>();

        /// <summary>
        /// A list of active plane scrolls.
        /// </summary>
        private List<PlaneScroll> _scrolls = new List<PlaneScroll>();

        private static readonly int[][] dissolveSequences = {
            /* SCI2.1early- */ new int[]{ 3, 6, 12, 20, 48, 96, 184, 272, 576, 1280, 3232, 6912, 13568, 24576, 46080 },
            /* SCI2.1mid+ */ new int[]{ 0, 0, 3, 6, 12, 20, 48, 96, 184, 272, 576, 1280, 3232, 6912, 13568, 24576, 46080, 73728, 132096, 466944 }
        };
        private static readonly short[][] divisionsDefaults = {
	        /* SCI2.1early- */ new short[]{ 1, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 40, 40, 101, 101 },
	        /* SCI2.1mid+ */   new short[]{ 1, 20, 20, 20, 20, 10, 10, 10, 10, 20, 20,  6, 10, 101, 101, 2 }
        };

        /// <summary>
        /// Default values for `PlaneShowStyle::divisions`
	    /// for the current SCI version.
        /// </summary>
        private short[] _defaultDivisions;

        public bool HasShowStyles { get { return _showStyles.Count > 0; } }

        public bool HasScrolls { get { return _scrolls.Count > 0; } }

        public GfxTransitions32(SegManager segMan)
        {
            _segMan = segMan;
            for (int i = 0; i < 236; i += 2)
            {
                _styleRanges[i] = 0;
                _styleRanges[i + 1] = -1;
            }
            for (int i = 236; i < _styleRanges.Length; ++i)
            {
                _styleRanges[i] = 0;
            }

            if (ResourceManager.GetSciVersion() < SciVersion.V2_1_MIDDLE)
            {
                _dissolveSequenceSeeds = dissolveSequences[0];
                _defaultDivisions = divisionsDefaults[0];
            }
            else
            {
                _dissolveSequenceSeeds = dissolveSequences[1];
                _defaultDivisions = divisionsDefaults[1];
            }
        }

        public void ProcessScrolls()
        {
            throw new NotImplementedException();
            //foreach (var it in _scrolls)
            //{
            //    bool finished = ProcessScroll(it);
            //    if (finished)
            //    {
            //        it = _scrolls.Remove(it);
            //    }
            //    else
            //    {
            //        ++it;
            //    }
            //}

            //Throttle();
        }

        // TODO: 10-argument version is only in SCI3; argc checks are currently wrong for this version
        // and need to be fixed in future
        public void KernelSetShowStyle(ushort argc, Register planeObj, ShowStyleType type, short seconds, short back, short priority, short animate, short frameOutNow, Register pFadeArray, short divisions, short blackScreen)
        {
            bool hasDivisions = false;
            bool hasFadeArray = false;

            // KQ7 2.0b uses a mismatched version of the Styler script (SCI2.1early script
            // for SCI2.1mid engine), so the calls it makes to kSetShowStyle are wrong and
            // put `divisions` where `pFadeArray` is supposed to be
            if (ResourceManager.GetSciVersion() == SciVersion.V2_1_MIDDLE && SciEngine.Instance.GameId == SciGameId.KQ7)
            {
                hasDivisions = argc > 7;
                hasFadeArray = false;
                divisions = argc > 7 ? pFadeArray.ToInt16() : (short)-1;
                pFadeArray = Register.NULL_REG;
            }
            else if (ResourceManager.GetSciVersion() < SciVersion.V2_1_MIDDLE)
            {
                hasDivisions = argc > 7;
                hasFadeArray = false;
            }
            else if (ResourceManager.GetSciVersion() < SciVersion.V3)
            {
                hasDivisions = argc > 8;
                hasFadeArray = argc > 7;
            }
            else
            {
                hasDivisions = argc > 9;
                hasFadeArray = argc > 8;
            }

            bool isFadeUp;
            short color;
            if (back != -1)
            {
                isFadeUp = false;
                color = back;
            }
            else
            {
                isFadeUp = true;
                color = 0;
            }

            Plane plane = SciEngine.Instance._gfxFrameout.GetPlanes().FindByObject(planeObj);
            if (plane == null)
            {
                Error("Plane {0} is not present in active planes list", planeObj);
            }

            bool createNewEntry = true;
            PlaneShowStyle entry = FindShowStyleForPlane(planeObj);
            if (entry != null)
            {
                bool useExisting = true;

                if (ResourceManager.GetSciVersion() < SciVersion.V2_1_MIDDLE)
                {
                    useExisting = plane._gameRect.Width == entry.width && plane._gameRect.Height == entry.height;
                }

                if (useExisting)
                {
                    useExisting = entry.divisions == (hasDivisions ? divisions : _defaultDivisions[(int)type]);
                }

                if (useExisting)
                {
                    createNewEntry = false;
                    isFadeUp = true;
                    entry.currentStep = 0;
                }
                else
                {
                    isFadeUp = true;
                    color = entry.color;
                    DeleteShowStyle(_showStyles, FindIteratorForPlane(planeObj));
                    entry = null;
                }
            }

            if (type == ShowStyleType.kShowStyleNone)
            {
                if (createNewEntry == false)
                {
                    DeleteShowStyle(_showStyles, FindIteratorForPlane(planeObj));
                }

                return;
            }

            if (createNewEntry)
            {
                entry = new PlaneShowStyle();
                // NOTE: SCI2.1 engine tests if allocation returned a null pointer
                // but then only avoids setting currentStep if this is so. Since
                // this is a nonsensical approach, we do not do that here
                entry.currentStep = 0;
                entry.processed = false;
                entry.divisions = hasDivisions ? divisions : _defaultDivisions[(int)type];
                entry.plane = planeObj;
                entry.fadeColorRangesCount = 0;

                if (ResourceManager.GetSciVersion() < SciVersion.V2_1_MIDDLE)
                {
                    // for pixel dissolve
                    entry.bitmap = Register.NULL_REG;
                    entry.bitmapScreenItem = null;

                    // for wipe
                    Array.Clear(entry.screenItems, 0, entry.screenItems.Length);
                    entry.width = plane._gameRect.Width;
                    entry.height = plane._gameRect.Height;
                }
                else
                {
                    entry.fadeColorRanges = null;
                    if (hasFadeArray)
                    {
                        // NOTE: SCI2.1mid engine does no check to verify that an array is
                        // successfully retrieved, and SegMan will cause a fatal error
                        // if we try to use a memory segment that is not an array
                        SciArray table = _segMan.LookupArray(pFadeArray);

                        int rangeCount = table.Size;
                        entry.fadeColorRangesCount = (byte)rangeCount;

                        // NOTE: SCI engine code always allocates memory even if the range
                        // table has no entries, but this does not really make sense, so
                        // we avoid the allocation call in this case
                        if (rangeCount > 0)
                        {
                            entry.fadeColorRanges = new ushort[rangeCount];
                            for (var i = 0; i < rangeCount; ++i)
                            {
                                entry.fadeColorRanges[i] = (ushort)table.GetAsInt16((ushort)i);
                            }
                        }
                    }
                }
            }

            // NOTE: The original engine had no nullptr check and would just crash
            // if it got to here
            if (entry == null)
            {
                Error("Cannot edit non-existing ShowStyle entry");
            }

            entry.fadeUp = isFadeUp;
            entry.color = color;
            entry.nextTick = SciEngine.Instance.TickCount;
            entry.type = type;
            entry.animate = animate != 0;
            entry.delay = (seconds * 60 + entry.divisions - 1) / entry.divisions;

            if (entry.delay == 0)
            {
                Error("ShowStyle has no duration");
            }

            if (frameOutNow != 0)
            {
                // Creates a reference frame for the pixel dissolves to use
                SciEngine.Instance._gfxFrameout.FrameOut(false);
            }

            if (createNewEntry)
            {
                if (ResourceManager.GetSciVersion() <= SciVersion.V2_1_EARLY)
                {
                    switch (entry.type)
                    {
                        case ShowStyleType.kShowStyleIrisOut:
                        case ShowStyleType.kShowStyleIrisIn:
                            Configure21EarlyIris(entry, priority);
                            break;
                        case ShowStyleType.kShowStyleDissolve:
                            Configure21EarlyDissolve(entry, priority, plane._gameRect);
                            break;
                        default:
                            // do nothing
                            break;
                    }
                }

                _showStyles.Add(entry);
            }
        }

        /// <summary>
        /// Sets the range that will be used by
        /// `GfxFrameout::palMorphFrameOut` to alter
        /// palette entries.
        /// </summary>
        /// <param name="fromColor"></param>
        /// <param name="toColor"></param>
        public void KernelSetPalStyleRange(byte fromColor, byte toColor)
        {
            if (toColor > fromColor)
            {
                return;
            }

            for (int i = fromColor; i <= toColor; ++i)
            {
                _styleRanges[i] = 0;
            }
        }

        /// <summary>
        /// Processes all active show styles in a loop
        /// until they are finished.
        /// </summary>
        public void ProcessShowStyles()
        {
            uint now = SciEngine.Instance.TickCount;

            bool continueProcessing;
            bool doFrameOut;
            do
            {
                continueProcessing = false;
                doFrameOut = false;
                for (int i = 0; i < _showStyles.Count; i++)
                {
                    var showStyle = _showStyles[i];
                    bool finished = false;

                    if (!showStyle.animate)
                    {
                        doFrameOut = true;
                    }

                    finished = ProcessShowStyle(showStyle, now);

                    if (!finished)
                    {
                        continueProcessing = true;
                    }

                    if (finished && showStyle.processed)
                    {
                        i = DeleteShowStyle(_showStyles, i);
                    }
                    else
                    {
                        i = i + 1;
                    }
                }

                if (SciEngine.Instance.ShouldQuit)
                {
                    return;
                }

                if (doFrameOut)
                {
                    SciEngine.Instance._gfxFrameout.FrameOut(true);
                    //TODO: SciEngine.Instance.SciDebugger.OnFrame();
                    Throttle();
                }
            } while (continueProcessing && doFrameOut);
        }

        /// <summary>
        /// Processes show styles that are applied
        /// through `GfxFrameout::palMorphFrameOut`.
        /// </summary>
        /// <param name="showStyle"></param>
        public void ProcessEffects(PlaneShowStyle showStyle)
        {
            throw new NotImplementedException();
        }

        private void Throttle()
        {
            byte throttleTime;
            if (_throttleState == 2)
            {
                throttleTime = 34;
                _throttleState = 0;
            }
            else
            {
                throttleTime = 33;
                ++_throttleState;
            }

            SciEngine.Instance.EngineState.SpeedThrottler(throttleTime);
            SciEngine.Instance.EngineState._throttleTrigger = true;
        }

        private int DeleteShowStyle(List<PlaneShowStyle> showStyles, int index)
        {
            var showStyle = _showStyles[index];
            switch (showStyle.type)
            {
                case ShowStyleType.kShowStyleDissolveNoMorph:
                case ShowStyleType.kShowStyleDissolve:
                    if (ResourceManager.GetSciVersion() <= SciVersion.V2_1_EARLY)
                    {
                        _segMan.FreeBitmap(showStyle.bitmap);
                        SciEngine.Instance._gfxFrameout.DeleteScreenItem(showStyle.bitmapScreenItem);
                    }
                    break;
                case ShowStyleType.kShowStyleIrisOut:
                case ShowStyleType.kShowStyleIrisIn:
                    if (ResourceManager.GetSciVersion() <= SciVersion.V2_1_EARLY)
                    {
                        for (int i = 0; i < showStyle.screenItems.Length; ++i)
                        {
                            ScreenItem screenItem = showStyle.screenItems[i];
                            if (screenItem != null)
                            {
                                SciEngine.Instance._gfxFrameout.DeleteScreenItem(screenItem);
                            }
                        }
                    }
                    break;
                case ShowStyleType.kShowStyleFadeIn:
                case ShowStyleType.kShowStyleFadeOut:
                    if (ResourceManager.GetSciVersion() > SciVersion.V2_1_EARLY && showStyle.fadeColorRangesCount > 0)
                    {
                        showStyle.fadeColorRanges = null;
                    }
                    break;
                case ShowStyleType.kShowStyleNone:
                case ShowStyleType.kShowStyleMorph:
                case ShowStyleType.kShowStyleHShutterIn:
                    // do nothing
                    break;
                default:

                    Error("Unknown delete transition type {0}", showStyle.type);
                    break;
            }

            _showStyles.Remove(showStyle);
            return index;
        }

        private bool ProcessShowStyle(PlaneShowStyle showStyle, uint now)
        {
            if (showStyle.nextTick >= now && showStyle.animate)
            {
                return false;
            }

            switch (showStyle.type)
            {
                default:
                case ShowStyleType.kShowStyleNone:
                    return ProcessNone(showStyle);
                case ShowStyleType.kShowStyleHShutterOut:
                case ShowStyleType.kShowStyleHShutterIn:
                case ShowStyleType.kShowStyleVShutterOut:
                case ShowStyleType.kShowStyleVShutterIn:
                case ShowStyleType.kShowStyleWipeLeft:
                case ShowStyleType.kShowStyleWipeRight:
                case ShowStyleType.kShowStyleWipeUp:
                case ShowStyleType.kShowStyleWipeDown:
                case ShowStyleType.kShowStyleDissolveNoMorph:
                case ShowStyleType.kShowStyleMorph:
                    return ProcessMorph(showStyle);
                case ShowStyleType.kShowStyleDissolve:
                    if (ResourceManager.GetSciVersion() > SciVersion.V2_1_EARLY)
                    {
                        return ProcessMorph(showStyle);
                    }
                    else
                    {
                        return ProcessPixelDissolve(showStyle);
                    }
                case ShowStyleType.kShowStyleIrisOut:
                    if (ResourceManager.GetSciVersion() > SciVersion.V2_1_EARLY)
                    {
                        return ProcessMorph(showStyle);
                    }
                    else
                    {
                        return ProcessIrisOut(showStyle);
                    }
                case ShowStyleType.kShowStyleIrisIn:
                    if (ResourceManager.GetSciVersion() > SciVersion.V2_1_EARLY)
                    {
                        return ProcessMorph(showStyle);
                    }
                    else
                    {
                        return ProcessIrisIn(showStyle);
                    }
                case ShowStyleType.kShowStyleFadeOut:
                    return ProcessFade(-1, showStyle);
                case ShowStyleType.kShowStyleFadeIn:
                    return ProcessFade(1, showStyle);
            }
        }

        private bool ProcessNone(PlaneShowStyle showStyle)
        {
            if (showStyle.fadeUp)
            {
                SciEngine.Instance._gfxPalette32.SetFade(100, 0, 255);
            }
            else
            {
                SciEngine.Instance._gfxPalette32.SetFade(0, 0, 255);
            }

            showStyle.processed = true;
            return true;
        }

        private bool ProcessFade(sbyte direction, PlaneShowStyle showStyle)
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
                        SciEngine.Instance._gfxPalette32.SetFade((ushort)percent, (byte)showStyle.fadeColorRanges[i], showStyle.fadeColorRanges[i + 1]);
                    }
                }
                else
                {
                    SciEngine.Instance._gfxPalette32.SetFade((ushort)percent, 0, 255);
                }

                ++showStyle.currentStep;
                showStyle.nextTick = (uint)(showStyle.nextTick + showStyle.delay);
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

        private bool ProcessIrisIn(PlaneShowStyle showStyle)
        {
            if (ResourceManager.GetSciVersion() > SciVersion.V2_1_EARLY)
            {
                Error("IrisIn is not known to be used by any SCI2.1mid+ game. Please submit a bug report with details about the game you were playing and what you were doing that triggered this error. Thanks!");
            }

            return ProcessWipe(1, showStyle);
        }

        private bool ProcessWipe(sbyte direction, PlaneShowStyle showStyle)
        {
            bool unchanged = true;
            if (showStyle.currentStep < showStyle.divisions)
            {
                int index;
                if (direction > 0)
                {
                    index = showStyle.currentStep;
                }
                else
                {
                    index = showStyle.divisions - showStyle.currentStep - 1;
                }

                index *= showStyle.numEdges;
                for (int i = 0; i < showStyle.numEdges; ++i)
                {
                    ScreenItem screenItem = showStyle.screenItems[index + i];
                    if (showStyle.fadeUp)
                    {
                        SciEngine.Instance._gfxFrameout.DeleteScreenItem(screenItem);
                        showStyle.screenItems[index + i] = null;
                    }
                    else
                    {
                        SciEngine.Instance._gfxFrameout.AddScreenItem(screenItem);
                    }
                }

                ++showStyle.currentStep;
                showStyle.nextTick = (uint)(showStyle.nextTick + showStyle.delay);
                unchanged = false;
            }

            if (showStyle.currentStep >= showStyle.divisions && unchanged)
            {
                if (showStyle.fadeUp)
                {
                    showStyle.processed = true;
                }

                return true;
            }

            return false;
        }

        private bool ProcessIrisOut(PlaneShowStyle showStyle)
        {
            if (ResourceManager.GetSciVersion() > SciVersion.V2_1_EARLY)
            {
                Error("IrisOut is not known to be used by any SCI2.1mid+ game. Please submit a bug report with details about the game you were playing and what you were doing that triggered this error. Thanks!");
            }

            return ProcessWipe(-1, showStyle);
        }

        private bool ProcessMorph(PlaneShowStyle showStyle)
        {
            SciEngine.Instance._gfxFrameout.PalMorphFrameOut(_styleRanges, showStyle);
            showStyle.processed = true;
            return true;
        }

        private bool ProcessPixelDissolve(PlaneShowStyle showStyle)
        {
            if (ResourceManager.GetSciVersion() > SciVersion.V2_1_EARLY)
            {
                return ProcessPixelDissolve21Mid(showStyle);
            }
            else
            {
                return ProcessPixelDissolve21Early(showStyle);
            }
        }

        private int BitWidth(int number)
        {
            int width = 0;
            while (number != 0)
            {
                number >>= 1;
                width += 1;
            }
            return width;
        }

        private bool ProcessPixelDissolve21Mid(PlaneShowStyle showStyle)
        {
            // SQ6 room 530

            Plane plane = SciEngine.Instance._gfxFrameout.VisiblePlanes.FindByObject(showStyle.plane);
            Rect screenRect = plane._screenRect;
            Rect rect;

            int planeWidth = screenRect.Width;
            int planeHeight = screenRect.Height;
            int divisions = showStyle.divisions;
            int width = planeWidth / divisions + ((planeWidth % divisions) != 0 ? 1 : 0);
            int height = planeHeight / divisions + ((planeHeight % divisions) != 0 ? 1 : 0);

            uint mask = (uint)_dissolveSequenceSeeds[BitWidth(width * height - 1)];
            int seq = 1;

            uint iteration = 0;
            uint numIterationsPerTick = (uint)((width * height + divisions) / divisions);

            do
            {
                int row = seq / width;
                int col = seq % width;

                if (row < height)
                {
                    if (row == height && (planeHeight % divisions) != 0)
                    {
                        if (col == width && (planeWidth % divisions) != 0)
                        {
                            rect.Left = (short)(col * divisions);
                            rect.Top = (short)(row * divisions);
                            rect.Right = (short)(col * divisions + (planeWidth % divisions));
                            rect.Bottom = (short)(row * divisions + (planeHeight % divisions));
                            rect.Clip(screenRect);
                            SciEngine.Instance._gfxFrameout.ShowRect(rect);
                        }
                        else
                        {
                            rect.Left = (short)(col * divisions);
                            rect.Top = (short)(row * divisions);
                            rect.Right = (short)(col * divisions * 2);
                            rect.Bottom = (short)(row * divisions + (planeHeight % divisions));
                            rect.Clip(screenRect);
                            SciEngine.Instance._gfxFrameout.ShowRect(rect);
                        }
                    }
                    else
                    {
                        if (col == width && (planeWidth % divisions) != 0)
                        {
                            rect.Left = (short)(col * divisions);
                            rect.Top = (short)(row * divisions);
                            rect.Right = (short)(col * divisions + (planeWidth % divisions) + 1);
                            rect.Bottom = (short)(row * divisions * 2 + 1);
                            rect.Clip(screenRect);
                            SciEngine.Instance._gfxFrameout.ShowRect(rect);
                        }
                        else
                        {
                            rect.Left = (short)(col * divisions);
                            rect.Top = (short)(row * divisions);
                            rect.Right = (short)(col * divisions * 2 + 1);
                            rect.Bottom = (short)(row * divisions * 2 + 1);
                            rect.Clip(screenRect);
                            SciEngine.Instance._gfxFrameout.ShowRect(rect);
                        }
                    }
                }

                if ((seq & 1) != 0)
                {
                    seq = (int)((seq >> 1) ^ mask);
                }
                else
                {
                    seq >>= 1;
                }

                if (++iteration == numIterationsPerTick)
                {
                    Throttle();
                    iteration = 0;
                }
            } while (seq != 1 && !SciEngine.Instance.ShouldQuit);

            rect.Left = screenRect.Left;
            rect.Top = screenRect.Top;
            rect.Right = (short)(divisions + screenRect.Left);
            rect.Bottom = (short)(divisions + screenRect.Bottom);
            rect.Clip(screenRect);
            SciEngine.Instance._gfxFrameout.ShowRect(rect);
            Throttle();

            SciEngine.Instance._gfxFrameout.ShowRect(screenRect);
            return true;
        }

        private bool ProcessPixelDissolve21Early(PlaneShowStyle showStyle)
        {
            bool unchanged = true;

            SciBitmap bitmap = _segMan.LookupBitmap(showStyle.bitmap);
            Buffer buffer = new Buffer((ushort)showStyle.width, (ushort)showStyle.height, bitmap.Pixels);

            uint numPixels = (uint)(showStyle.width * showStyle.height);
            uint numPixelsPerDivision = (uint)((numPixels + showStyle.divisions) / showStyle.divisions);

            uint index;
            if (showStyle.currentStep == 0)
            {
                int i = 0;
                index = numPixels;
                if (index != 1)
                {
                    for (;;)
                    {
                        index >>= 1;
                        if (index == 1)
                        {
                            break;
                        }
                        i++;
                    }
                }

                showStyle.dissolveMask = (uint)_dissolveSequenceSeeds[i];
                index = 53427;

                showStyle.firstPixel = index;
                showStyle.pixel = index;
            }
            else
            {
                index = showStyle.pixel;
                for (;;)
                {
                    if ((index & 1) != 0)
                    {
                        index >>= 1;
                        index ^= showStyle.dissolveMask;
                    }
                    else
                    {
                        index >>= 1;
                    }

                    if (index < numPixels)
                    {
                        break;
                    }
                }

                if (index == showStyle.firstPixel)
                {
                    index = 0;
                }
            }

            if (showStyle.currentStep < showStyle.divisions)
            {
                for (int i = 0; i < numPixelsPerDivision; ++i)
                {
                    var ptr = buffer.GetBasePtr((int)(index % showStyle.width), (int)(index / showStyle.width));
                    ptr.Value = (byte)showStyle.color;

                    for (;;)
                    {
                        if ((index & 1) != 0)
                        {
                            index >>= 1;
                            index ^= showStyle.dissolveMask;
                        }
                        else
                        {
                            index >>= 1;
                        }

                        if (index < numPixels)
                        {
                            break;
                        }
                    }

                    if (index == showStyle.firstPixel)
                    {
                        buffer.FillRect(new Rect(0, 0, showStyle.width, showStyle.height), (uint)showStyle.color);
                        break;
                    }
                }

                showStyle.pixel = index;
                showStyle.nextTick = (uint)(showStyle.nextTick + showStyle.delay);
                ++showStyle.currentStep;
                unchanged = false;
                if (showStyle.bitmapScreenItem._created == 0)
                {
                    showStyle.bitmapScreenItem._updated = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }
            }

            if ((showStyle.currentStep >= showStyle.divisions) && unchanged)
            {
                if (showStyle.fadeUp)
                {
                    showStyle.processed = true;
                }

                return true;
            }

            return false;
        }

        private void Configure21EarlyDissolve(PlaneShowStyle entry, short priority, Rect _gameRect)
        {
            throw new NotImplementedException();
        }

        private void Configure21EarlyIris(PlaneShowStyle entry, short priority)
        {
            throw new NotImplementedException();
        }

        private int FindIteratorForPlane(Register planeObj)
        {
            for (int i = 0; i < _showStyles.Count; i++)
            {
                if (_showStyles[i].plane == planeObj)
                    return i;
            }
            return -1;
        }

        private PlaneShowStyle FindShowStyleForPlane(Register planeObj)
        {
            return _showStyles.FirstOrDefault(o => o.plane == planeObj);
        }
    }
}
