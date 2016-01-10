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
using System.Linq;
using NScumm.Sci.Engine;
using NScumm.Core.Graphics;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Sci.Graphics
{
    // Flags for the signal selector
    enum ViewSignals
    {
        StopUpdate = 0x0001,
        ViewUpdated = 0x0002,
        NoUpdate = 0x0004,
        Hidden = 0x0008,
        FixedPriority = 0x0010,
        AlwaysUpdate = 0x0020,
        ForceUpdate = 0x0040,
        RemoveView = 0x0080,
        Frozen = 0x0100, // I got frozen today!!
                         //kSignalExtraActor	 = 0x0200, // unused by us, defines all actors that may be included into the background if speed is too slow
        HitObstacle = 0x0400, // used in the actor movement code by kDoBresen()
        DoesntTurn = 0x0800, // used by _k_dirloop() to determine if an actor can turn or not
                             //kSignalNoCycler		 = 0x1000, // unused by us
                             //kSignalIgnoreHorizon = 0x2000, // unused by us, defines actor that can ignore horizon
        IgnoreActor = 0x4000,
        DisposeMe = 0x8000
    }

    enum ViewScaleSignals
    {
        DoScaling = 0x0001, // enables scaling when drawing that cel (involves scaleX and scaleY)
        GlobalScaling = 0x0002, // means that global scaling shall get applied on that cel (sets scaleX/scaleY)
        Hoyle4SpecialHandling = 0x0004  // HOYLE4-exclusive: special handling inside kAnimate, is used when giving out cards
    }

    internal struct AnimateEntry
    {
        public short givenOrderNo;
        public Register @object;
        public int viewId;
        public short loopNo;
        public short celNo;
        public short paletteNo;
        public short x, y, z;
        public short priority;
        public ViewSignals signal;
        public ViewScaleSignals scaleSignal;
        public short scaleX;
        public short scaleY;
        public Rect celRect;
        public bool showBitsFlag;
        public Register castHandle;
    }

    /// <summary>
    /// Animate class, kAnimate and relevant functions for SCI16 (SCI0-SCI1.1) games
    /// </summary>
    internal class GfxAnimate
    {
        private EngineState _s;
        private GfxCache _cache;
        private GfxCursor _cursor;
        private GfxPaint16 _paint16;
        private GfxPalette _palette;
        private GfxPorts _ports;
        private GfxScreen _screen;
        private GfxTransitions _transitions;
        private List<AnimateEntry> _lastCastData;
        private bool _ignoreFastCast;
        private List<AnimateEntry> _list;

        public GfxAnimate(EngineState state, GfxCache cache, GfxPorts ports, GfxPaint16 paint16, GfxScreen screen, GfxPalette palette, GfxCursor cursor, GfxTransitions transitions)
        {
            _s = state;
            _cache = cache;
            _ports = ports;
            _paint16 = paint16;
            _screen = screen;
            _palette = palette;
            _cursor = cursor;
            _transitions = transitions;

            _list = new List<AnimateEntry>();
            _lastCastData = new List<AnimateEntry>();

            Init();
        }

        private void Init()
        {
            _lastCastData.Clear();

            _ignoreFastCast = false;
            // fastCast object is not found in any SCI games prior SCI1
            if (ResourceManager.GetSciVersion() <= SciVersion.V01)
                _ignoreFastCast = true;
            // Also if fastCast object exists at gamestartup, we can assume that the interpreter doesnt do kAnimate aborts
            //  (found in Larry 1)
            if (ResourceManager.GetSciVersion() > SciVersion.V0_EARLY)
            {
                if (!_s._segMan.FindObjectByName("fastCast").IsNull)
                    _ignoreFastCast = true;
            }
        }

        public void KernelAnimate(Register listReference, bool cycle, int argc, StackPtr? argv)
        {
            byte old_picNotValid = (byte)_screen._picNotValid;

            if (ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                _palette.PalVaryUpdate();

            if (listReference.IsNull)
            {
                DisposeLastCast();
                if (_screen._picNotValid != 0)
                    AnimateShowPic();
                return;
            }

            List list = _s._segMan.LookupList(listReference);
            if (list == null)
                throw new InvalidOperationException("kAnimate called with non-list as parameter");

            if (cycle)
            {
                if (!Invoke(list, argc, argv))
                    return;

                // Look up the list again, as it may have been modified
                list = _s._segMan.LookupList(listReference);
            }

            Port oldPort = _ports.SetPort(_ports._picWind);
            DisposeLastCast();

            MakeSortedList(list);
            Fill(ref old_picNotValid);

            if (old_picNotValid != 0)
            {
                // beginUpdate()/endUpdate() were introduced SCI1.
                // Calling those for SCI0 will work most of the time but breaks minor
                // stuff like percentage bar of qfg1ega at the character skill screen.
                if (ResourceManager.GetSciVersion() >= SciVersion.V1_EGA_ONLY)
                    _ports.BeginUpdate(_ports._picWind);
                Update();
                if (ResourceManager.GetSciVersion() >= SciVersion.V1_EGA_ONLY)
                    _ports.EndUpdate(_ports._picWind);
            }

            DrawCels();

            if (_screen._picNotValid != 0)
                AnimateShowPic();

            UpdateScreen(old_picNotValid);
            RestoreAndDelete(argc, argv);

            // We update the screen here as well, some scenes like EQ1 credits run w/o calling kGetEvent thus we wouldn't update
            //  screen at all
            SciEngine.Instance.EventManager.UpdateScreen();

            _ports.SetPort(oldPort);

            // Now trigger speed throttler
            ThrottleSpeed();
        }

        private void DrawCels()
        {
            Register bitsHandle;
            _lastCastData.Clear();

            for (int i = 0; i < _list.Count; i++)
            {
                var it = _list[i];
                if (!it.signal.HasFlag(ViewSignals.NoUpdate | ViewSignals.Hidden | ViewSignals.AlwaysUpdate))
                {
                    // Save background
                    bitsHandle = _paint16.BitsSave(it.celRect, GfxScreenMasks.ALL);
                    SciEngine.WriteSelector(_s._segMan, it.@object, SciEngine.Selector(s => s.underBits), bitsHandle);

                    // draw corresponding cel
                    _paint16.DrawCel(it.viewId, it.loopNo, it.celNo, it.celRect, (byte)it.priority, (ushort)it.paletteNo, (ushort)it.scaleX, (ushort)it.scaleY);
                    it.showBitsFlag = true;

                    if (it.signal.HasFlag(ViewSignals.RemoveView))
                        it.signal &= ~ViewSignals.RemoveView;

                    // Remember that entry in lastCast
                    _lastCastData.Add(it);
                }
            }
        }

        private void RestoreAndDelete(int argc, StackPtr? argv)
        {
            // This has to be done in a separate loop. At least in sq1 some .dispose
            // modifies FIXEDLOOP flag in signal for another object. In that case we
            // would overwrite the new signal with our version of the old signal.
            foreach (var it in _list)
            {
                // Finally update signal
                SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.signal), (ushort)it.signal);
            }

            for (int i = _list.Count - 1; i > 0; i--)
            {
                var it = _list[i];
                // We read out signal here again, this is not by accident but to ensure
                // that we got an up-to-date signal
                it.signal = (ViewSignals)SciEngine.ReadSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.signal));

                if ((it.signal & (ViewSignals.NoUpdate | ViewSignals.RemoveView)) == 0)
                {
                    _paint16.BitsRestore(SciEngine.ReadSelector(_s._segMan, it.@object, SciEngine.Selector(s => s.underBits)));
                    SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.underBits), 0);
                }

                if ((it.signal & ViewSignals.DisposeMe) != 0)
                {
                    // Call .delete_ method of that object
                    SciEngine.InvokeSelector(_s, it.@object, SciEngine.Selector(s => s.delete_), argc, argv, 0);
                }
            }
        }

        private void UpdateScreen(byte oldPicNotValid)
        {
            Rect lsRect;
            Rect workerRect;

            for (int i = 0; i < _list.Count; i++)
            {
                var it = _list[i];
                if (it.showBitsFlag || !(it.signal.HasFlag((ViewSignals.RemoveView | ViewSignals.NoUpdate)) ||
                                                (!(it.signal.HasFlag(ViewSignals.RemoveView)) && (it.signal.HasFlag(ViewSignals.NoUpdate)) && oldPicNotValid != 0)))
                {
                    lsRect.Left = (int)SciEngine.ReadSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.lsLeft));
                    lsRect.Top = (int)SciEngine.ReadSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.lsTop));
                    lsRect.Right = (int)SciEngine.ReadSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.lsRight));
                    lsRect.Bottom = (int)SciEngine.ReadSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.lsBottom));

                    workerRect = lsRect;
                    workerRect.Clip(it.celRect);

                    if (!workerRect.IsEmpty)
                    {
                        workerRect = lsRect;
                        workerRect.Extend(it.celRect);
                    }
                    else {
                        _paint16.BitsShow(lsRect);
                        workerRect = it.celRect;
                    }
                    SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.lsLeft), (ushort)it.celRect.Left);
                    SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.lsTop), (ushort)it.celRect.Top);
                    SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.lsRight), (ushort)it.celRect.Right);
                    SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.lsBottom), (ushort)it.celRect.Bottom);
                    // may get used for debugging
                    //_paint16.frameRect(workerRect);
                    _paint16.BitsShow(workerRect);

                    if (it.signal.HasFlag(ViewSignals.Hidden))
                        it.signal |= ViewSignals.RemoveView;
                }
            }
            // use this for debug purposes
            // _screen.copyToScreen();
        }

        private void ThrottleSpeed()
        {
            switch (_lastCastData.Count)
            {
                case 0:
                    // No entries drawn . no speed throttler triggering
                    break;
                case 1:
                    {

                        // One entry drawn . check if that entry was a speed benchmark view, if not enable speed throttler
                        AnimateEntry onlyCast = _lastCastData[0];
                        if ((onlyCast.viewId == 0) && (onlyCast.loopNo == 13) && (onlyCast.celNo == 0))
                        {
                            // this one is used by jones talkie
                            if ((onlyCast.celRect.Height == 8) && (onlyCast.celRect.Width == 8))
                            {
                                _s._gameIsBenchmarking = true;
                                return;
                            }
                        }
                        // first loop and first cel used?
                        if ((onlyCast.loopNo == 0) && (onlyCast.celNo == 0))
                        {
                            // and that cel has a known speed benchmark resolution
                            short onlyHeight = (short)onlyCast.celRect.Height;
                            short onlyWidth = (short)onlyCast.celRect.Width;
                            if (((onlyWidth == 12) && (onlyHeight == 35)) || // regular benchmark view ("fred", "Speedy", "ego")
                                ((onlyWidth == 29) && (onlyHeight == 45)) || // King's Quest 5 french "fred"
                                ((onlyWidth == 1) && (onlyHeight == 5)) || // Freddy Pharkas "fred"
                                ((onlyWidth == 1) && (onlyHeight == 1)))
                            { // Laura Bow 2 Talkie
                              // check further that there is only one cel in that view
                                GfxView onlyView = _cache.GetView(onlyCast.viewId);
                                if ((onlyView.LoopCount == 1) && (onlyView.GetCelCount(0) != 0))
                                {
                                    _s._gameIsBenchmarking = true;
                                    return;
                                }
                            }
                        }
                        _s._gameIsBenchmarking = false;
                        _s._throttleTrigger = true;
                        break;
                    }
                default:
                    // More than 1 entry drawn . time for speed throttling
                    _s._gameIsBenchmarking = false;
                    _s._throttleTrigger = true;
                    break;
            }
        }

        private void Update()
        {
            Register bitsHandle;
            Rect rect;

            // Remove all no-update cels, if requested
            for (int i = _list.Count - 1; i >= 0; i--)
            {
                var it = _list[i];
                if (it.signal.HasFlag(ViewSignals.NoUpdate))
                {
                    if (!it.signal.HasFlag(ViewSignals.RemoveView))
                    {
                        bitsHandle = SciEngine.ReadSelector(_s._segMan, it.@object, SciEngine.Selector(s => s.underBits));
                        if (_screen._picNotValid != 1)
                        {
                            _paint16.BitsRestore(bitsHandle);
                            it.showBitsFlag = true;
                        }
                        else {
                            _paint16.BitsFree(bitsHandle);
                        }
                        SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.underBits), 0);
                    }
                    it.signal &= ~ViewSignals.ForceUpdate;
                    if (it.signal.HasFlag(ViewSignals.ViewUpdated))
                        it.signal &= ~(ViewSignals.ViewUpdated | ViewSignals.NoUpdate);
                }
                else if (it.signal.HasFlag(ViewSignals.StopUpdate))
                {
                    it.signal &= ~ViewSignals.StopUpdate;
                    it.signal |= ViewSignals.NoUpdate;
                }
            }

            // Draw always-update cels
            for (int i = 0; i < _list.Count; i++)
            {
                var it = _list[i];
                if (it.signal.HasFlag(ViewSignals.AlwaysUpdate))
                {
                    // draw corresponding cel
                    _paint16.DrawCel(it.viewId, it.loopNo, it.celNo, it.celRect, (byte)it.priority, (ushort)it.paletteNo, (ushort)it.scaleX, (ushort)it.scaleY);
                    it.showBitsFlag = true;

                    it.signal &= ~(ViewSignals.StopUpdate | ViewSignals.ViewUpdated | ViewSignals.NoUpdate | ViewSignals.ForceUpdate);
                    if (!(it.signal.HasFlag(ViewSignals.IgnoreActor)))
                    {
                        rect = it.celRect;
                        rect.Top = ScummHelper.Clip(_ports.KernelPriorityToCoordinate((byte)it.priority) - 1, rect.Top, rect.Bottom - 1);
                        _paint16.FillRect(rect, GfxScreenMasks.CONTROL, 0, 0, 15);
                    }
                }
            }

            // Saving background for all NoUpdate-cels
            for (int i = 0; i < _list.Count; i++)
            {
                var it = _list[i];
                if (it.signal.HasFlag(ViewSignals.NoUpdate))
                {
                    if (it.signal.HasFlag(ViewSignals.Hidden))
                    {
                        it.signal |= ViewSignals.RemoveView;
                    }
                    else {
                        it.signal &= ~ViewSignals.RemoveView;
                        if (it.signal.HasFlag(ViewSignals.IgnoreActor))
                            bitsHandle = _paint16.BitsSave(it.celRect, GfxScreenMasks.VISUAL | GfxScreenMasks.PRIORITY);
                        else
                            bitsHandle = _paint16.BitsSave(it.celRect, GfxScreenMasks.ALL);
                        SciEngine.WriteSelector(_s._segMan, it.@object, SciEngine.Selector(s => s.underBits), bitsHandle);
                    }
                }
            }

            // Draw NoUpdate cels
            for (int i = 0; i < _list.Count; i++)
            {
                var it = _list[i];
                if (it.signal.HasFlag(ViewSignals.NoUpdate) && !(it.signal.HasFlag(ViewSignals.Hidden)))
                {
                    // draw corresponding cel
                    _paint16.DrawCel(it.viewId, it.loopNo, it.celNo, it.celRect, (byte)it.priority, (ushort)it.paletteNo, (ushort)it.scaleX, (ushort)it.scaleY);
                    it.showBitsFlag = true;

                    if (!(it.signal.HasFlag(ViewSignals.IgnoreActor)))
                    {
                        rect = it.celRect;
                        rect.Top = ScummHelper.Clip(_ports.KernelPriorityToCoordinate((byte)it.priority) - 1, rect.Top, rect.Bottom - 1);
                        _paint16.FillRect(rect, GfxScreenMasks.CONTROL, 0, 0, 15);
                    }
                }
            }
        }

        public void ReAnimate(Rect rect)
        {
            if (_lastCastData.Count != 0)
            {
                for (int i = 0; i < _lastCastData.Count; i++)
                {
                    var it = _lastCastData[i];
                    it.castHandle = Register.Make(_paint16.BitsSave(it.celRect, GfxScreenMasks.VISUAL | GfxScreenMasks.PRIORITY));
                    _paint16.DrawCel(it.viewId, it.loopNo, it.celNo, it.celRect, (byte)it.priority, (ushort)it.paletteNo, (ushort)it.scaleX, (ushort)it.scaleY);
                }
                _paint16.BitsShow(rect);
                // restoring
                for (int i = _lastCastData.Count - 1; i >= 0; i--)
                {
                    var it = _lastCastData[i];
                    _paint16.BitsRestore(Register.Make(it.castHandle));
                }
            }
            else
            {
                _paint16.BitsShow(rect);
            }
        }

        private void Fill(ref byte old_picNotValid)
        {
            GfxView view = null;

            for (int i = 0; i < _list.Count; i++)
            {
                var it = _list[i];
                // Get the corresponding view
                view = _cache.GetView(it.viewId);

                AdjustInvalidCels(view, it);
                ProcessViewScaling(view, it);
                SetNsRect(view, it);

                //warning("%s view %d, loop %d, cel %d, signal %x", _s._segMan.getObjectName(curObject), it.viewId, it.loopNo, it.celNo, it.signal);

                // Calculate current priority according to y-coordinate
                if (!it.signal.HasFlag(ViewSignals.FixedPriority))
                {
                    it.priority = _ports.KernelCoordinateToPriority(it.y);
                    SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.priority), (ushort)it.priority);
                }

                if (it.signal.HasFlag(ViewSignals.NoUpdate))
                {
                    if ((it.signal & (ViewSignals.ForceUpdate | ViewSignals.ViewUpdated)) != 0
                        || (it.signal & ViewSignals.Hidden) != 0 && (it.signal & ViewSignals.RemoveView) == 0
                        || 0 == (it.signal & ViewSignals.Hidden) && (it.signal & ViewSignals.RemoveView) != 0
                        || (it.signal & ViewSignals.AlwaysUpdate) != 0)
                        old_picNotValid++;
                    it.signal &= ~ViewSignals.StopUpdate;
                }
                else {
                    if ((it.signal & ViewSignals.StopUpdate) != 0 || (it.signal & ViewSignals.AlwaysUpdate) != 0)
                        old_picNotValid++;
                    it.signal &= ~ViewSignals.ForceUpdate;
                }
            }
        }

        private void SetNsRect(GfxView view, AnimateEntry it)
        {
            bool shouldSetNsRect = true;

            // Create rect according to coordinates and given cel
            if (it.scaleSignal.HasFlag(ViewScaleSignals.DoScaling))
            {
                view.GetCelScaledRect(it.loopNo, it.celNo, it.x, it.y, it.z, it.scaleX, it.scaleY, it.celRect);
                // when being scaled, only set nsRect, if object will get drawn
                if (it.signal.HasFlag(ViewSignals.Hidden) && !it.signal.HasFlag(ViewSignals.AlwaysUpdate))
                    shouldSetNsRect = false;
            }
            else {
                //  This special handling is not included in the other SCI1.1 interpreters and MUST NOT be
                //  checked in those cases, otherwise we will break games (e.g. EcoQuest 2, room 200)
                if ((SciEngine.Instance.GameId == SciGameId.HOYLE4) && it.scaleSignal.HasFlag(ViewScaleSignals.Hoyle4SpecialHandling))
                {
                    it.celRect = SciEngine.Instance._gfxCompare.GetNSRect(it.@object);
                    view.GetCelSpecialHoyle4Rect(it.loopNo, it.celNo, it.x, it.y, it.z, it.celRect);
                    shouldSetNsRect = false;
                }
                else {
                    it.celRect = view.GetCelRect(it.loopNo, it.celNo, it.x, it.y, it.z);
                }
            }

            if (shouldSetNsRect)
            {
                SciEngine.Instance._gfxCompare.SetNSRect(it.@object, it.celRect);
            }
        }

        private void ProcessViewScaling(GfxView view, AnimateEntry it)
        {
            if (!view.IsScaleable)
            {
                // Laura Bow 2 (especially floppy) depends on this, some views are not supposed to be scaleable
                //  this "feature" was removed in later versions of SCI1.1
                it.scaleSignal = 0;
                it.scaleY = it.scaleX = 128;
            }
            else {
                // Process global scaling, if needed
                if (it.scaleSignal.HasFlag(ViewScaleSignals.DoScaling))
                {
                    if (it.scaleSignal.HasFlag(ViewScaleSignals.GlobalScaling))
                    {
                        ApplyGlobalScaling(it, view);
                    }
                }
            }
        }

        private void ApplyGlobalScaling(AnimateEntry entry, GfxView view)
        {
            // Global scaling uses global var 2 and some other stuff to calculate scaleX/scaleY
            short maxScale = (short)SciEngine.ReadSelectorValue(_s._segMan, entry.@object, SciEngine.Selector(s => s.maxScale));
            short celHeight = view.GetHeight(entry.loopNo, entry.celNo);
            short maxCelHeight = (short)((maxScale * celHeight) >> 7);
            Register globalVar2 = _s.variables[Vm.VAR_GLOBAL][2]; // current room object
            short vanishingY = (short)SciEngine.ReadSelectorValue(_s._segMan, globalVar2, SciEngine.Selector(s => s.vanishingY));

            short fixedPortY = (short)(_ports.Port.rect.Bottom - vanishingY);
            short fixedEntryY = (short)(entry.y - vanishingY);
            if (fixedEntryY == 0)
                fixedEntryY = 1;

            if ((celHeight == 0) || (fixedPortY == 0))
                throw new InvalidOperationException("global scaling panic");

            entry.scaleY = (short)((maxCelHeight * fixedEntryY) / fixedPortY);
            entry.scaleY = (short)((entry.scaleY * 128) / celHeight);

            entry.scaleX = entry.scaleY;

            // and set objects scale selectors
            SciEngine.WriteSelectorValue(_s._segMan, entry.@object, SciEngine.Selector(s => s.scaleX), (ushort)entry.scaleX);
            SciEngine.WriteSelectorValue(_s._segMan, entry.@object, SciEngine.Selector(s => s.scaleY), (ushort)entry.scaleY);
        }

        private void AdjustInvalidCels(GfxView view, AnimateEntry it)
        {
            // adjust loop and cel, if any of those is invalid
            //  this seems to be completely crazy code
            //  sierra sci checked signed int16 to be above or equal the counts and reseted to 0 in those cases
            //  later during view processing those are compared unsigned again and then set to maximum count - 1
            //  Games rely on this behavior. For example laura bow 1 has a knight standing around in room 37
            //   which has cel set to 3. This cel does not exist and the actual knight is 0
            //   In kq5 on the other hand during the intro, when the trunk is opened, cel is set to some real
            //   high number, which is negative when considered signed. This actually requires to get fixed to
            //   maximum cel, otherwise the trunk would be closed.
            short viewLoopCount = (short)view.LoopCount;
            if (it.loopNo >= viewLoopCount)
            {
                it.loopNo = 0;
                SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.loop), (ushort)it.loopNo);
            }
            else if (it.loopNo < 0)
            {
                it.loopNo = (short)(viewLoopCount - 1);
                // not setting selector is right, sierra sci didn't do it during view processing as well
            }
            short viewCelCount = (short)view.GetCelCount(it.loopNo);
            if (it.celNo >= viewCelCount)
            {
                it.celNo = 0;
                SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.cel), (ushort)it.celNo);
            }
            else if (it.celNo < 0)
            {
                it.celNo = (short)(viewCelCount - 1);
            }
        }

        private void MakeSortedList(List list)
        {
            Register curAddress = list.first;
            Node curNode = _s._segMan.LookupNode(curAddress);
            short listNr;

            // Clear lists
            _list.Clear();
            _lastCastData.Clear();

            // Fill the list
            for (listNr = 0; curNode != null; listNr++)
            {
                AnimateEntry listEntry = new AnimateEntry();
                Register curObject = curNode.value;
                listEntry.@object = Register.Make(curObject);
                listEntry.castHandle = Register.NULL_REG;

                // Get data from current object
                listEntry.givenOrderNo = listNr;
                listEntry.viewId = (int)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.view));
                listEntry.loopNo = (short)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.loop));
                listEntry.celNo = (short)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.cel));
                listEntry.paletteNo = (short)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.palette));
                listEntry.x = (short)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.x));
                listEntry.y = (short)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.y));
                listEntry.z = (short)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.z));
                listEntry.priority = (short)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.priority));
                listEntry.signal = (ViewSignals)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.signal));
                if (ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                {
                    // Cel scaling
                    listEntry.scaleSignal = (ViewScaleSignals)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.scaleSignal));
                    if (listEntry.scaleSignal.HasFlag(ViewScaleSignals.DoScaling))
                    {
                        listEntry.scaleX = (short)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.scaleX));
                        listEntry.scaleY = (short)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.scaleY));
                    }
                    else {
                        listEntry.scaleX = 128;
                        listEntry.scaleY = 128;
                    }
                }
                else {
                    listEntry.scaleSignal = 0;
                    listEntry.scaleX = 128;
                    listEntry.scaleY = 128;
                }
                // listEntry.celRect is filled in AnimateFill()
                listEntry.showBitsFlag = false;

                _list.Add(listEntry);

                curAddress = curNode.succ;
                curNode = _s._segMan.LookupNode(curAddress);
            }

            // Possible TODO: As noted in the comment in sortHelper we actually
            // require a stable sorting algorithm here. Since Common::sort is not stable
            // at the time of writing this comment, we work around that in our ordering
            // comparator. If that changes in the future or we want to use some
            // stable sorting algorithm here, we should change that.
            // In that case we should test such changes intensively. A good place to test stable sort
            // is iceman, cupboard within the submarine. If sort isn't stable, the cupboard will be
            // half-open, half-closed. Of course that's just one of many special cases.

            // Now sort the list according y and z (descending)
            _list.Sort(SortHelper);
        }

        private int SortHelper(AnimateEntry entry1, AnimateEntry entry2)
        {
            if (entry1.y == entry2.y)
            {
                // if both y and z are the same, use the order we were given originally
                //  this is needed for special cases like iceman room 35
                if (entry1.z == entry2.z)
                    return entry2.givenOrderNo.CompareTo(entry1.givenOrderNo);
                else
                    return entry2.z.CompareTo(entry1.z);
            }
            return entry2.y.CompareTo(entry1.y);
        }

        private bool Invoke(List list, int argc, StackPtr? argv)
        {
            Register curAddress = list.first;
            Node curNode = _s._segMan.LookupNode(curAddress);
            Register curObject;
            ViewSignals signal;

            while (curNode != null)
            {
                curObject = curNode.value;

                if (!_ignoreFastCast)
                {
                    // Check if the game has a fastCast object set
                    //  if we don't abort kAnimate processing, at least in kq5 there will be animation cels drawn into speech boxes.
                    if (!_s.variables[Vm.VAR_GLOBAL][84].IsNull)
                    {
                        if (_s._segMan.GetObjectName(_s.variables[Vm.VAR_GLOBAL][84]) == "fastCast")
                            return false;
                    }
                }

                signal = (ViewSignals)SciEngine.ReadSelectorValue(_s._segMan, curObject, SciEngine.Selector(s => s.signal));
                if ((signal & ViewSignals.Frozen) == 0)
                {
                    // Call .doit method of that object
                    SciEngine.InvokeSelector(_s, curObject, SciEngine.Selector(s => s.doit), argc, argv, 0);

                    // If a game is being loaded, stop processing
                    if (_s.abortScriptProcessing != AbortGameState.None)
                        return true; // Stop processing

                    // Lookup node again, since the nodetable it was in may have been reallocated.
                    // The node might have been deallocated at this point (e.g. LSL2, room 42),
                    // in which case the node reference will be null and the loop will stop below.
                    // If the node is deleted from kDeleteKey, it won't have a successor node, thus
                    // list processing will stop here (which is what SSCI does).
                    curNode = _s._segMan.LookupNode(curAddress, false);
                }

                if (curNode != null)
                {
                    curAddress = curNode.succ;
                    curNode = _s._segMan.LookupNode(curAddress);
                }
            }
            return true;
        }

        private void AnimateShowPic()
        {
            Port picPort = _ports._picWind;
            Rect picRect = picPort.rect;
            bool previousCursorState = _cursor.IsVisible;

            if (previousCursorState)
                _cursor.KernelHide();
            // Adjust picRect to become relative to screen
            picRect.Translate(picPort.left, picPort.top);
            _transitions.DoIt(picRect);
            if (previousCursorState)
                _cursor.KernelShow();
        }

        private void DisposeLastCast()
        {
            _lastCastData.Clear();
        }

        public void KernelAddToPicList(Register listReference, int argc, StackPtr? argv)
        {
            List list;

            _ports.SetPort(_ports._picWind);

            list = _s._segMan.LookupList(listReference);
            if (list == null)
                throw new InvalidOperationException("kAddToPic called with non-list as parameter");

            MakeSortedList(list);
            AddToPicDrawCels();

            AddToPicSetPicNotValid();
        }

        private void AddToPicSetPicNotValid()
        {
            if (ResourceManager.GetSciVersion() <= SciVersion.V1_EARLY)
                _screen._picNotValid = 1;
            else
                _screen._picNotValid = 2;
        }


        private void AddToPicDrawCels()
        {
            Register curObject;
            GfxView view = null;

            for (int i = 0; i < _list.Count; i++)
            {
                var it = _list[i];
                curObject = Register.Make(it.@object);

                // Get the corresponding view
                view = _cache.GetView(it.viewId);

                // kAddToPic does not do loop/cel-number fixups

                if (it.priority == -1)
                    it.priority = _ports.KernelCoordinateToPriority(it.y);

                if (!view.IsScaleable)
                {
                    // Laura Bow 2 specific - Check fill() below
                    it.scaleSignal = 0;
                    it.scaleY = it.scaleX = 128;
                }

                // Create rect according to coordinates and given cel
                if (it.scaleSignal.HasFlag(ViewScaleSignals.DoScaling))
                {
                    if (it.scaleSignal.HasFlag(ViewScaleSignals.GlobalScaling))
                    {
                        ApplyGlobalScaling(it, view);
                    }
                    view.GetCelScaledRect(it.loopNo, it.celNo, it.x, it.y, it.z, it.scaleX, it.scaleY, it.celRect);
                    SciEngine.Instance._gfxCompare.SetNSRect(curObject, it.celRect);
                }
                else {
                    it.celRect = view.GetCelRect(it.loopNo, it.celNo, it.x, it.y, it.z);
                }

                // draw corresponding cel
                _paint16.DrawCel(view, it.loopNo, it.celNo, it.celRect, (byte)it.priority, (ushort)it.paletteNo, (ushort)it.scaleX, (ushort)it.scaleY);
                if (!it.signal.HasFlag(ViewSignals.IgnoreActor))
                {
                    it.celRect.Top = ScummHelper.Clip(_ports.KernelPriorityToCoordinate((byte)it.priority) - 1, it.celRect.Top, it.celRect.Bottom - 1);
                    _paint16.FillRect(it.celRect, GfxScreenMasks.CONTROL, 0, 0, 15);
                }
            }
        }

        public void KernelAddToPicView(int viewId, short loopNo, short celNo, short x, short y, short priority, short control)
        {
            _ports.SetPort(_ports._picWind);
            AddToPicDrawView(viewId, loopNo, celNo, x, y, priority, control);
            AddToPicSetPicNotValid();
        }

        private void AddToPicDrawView(int viewId, short loopNo, short celNo, short x, short y, short priority, short control)
        {
            GfxView view = _cache.GetView(viewId);
            Rect celRect;

            if (priority == -1)
                priority = _ports.KernelCoordinateToPriority(y);

            // Create rect according to coordinates and given cel
            celRect = view.GetCelRect(loopNo, celNo, x, y, 0);
            _paint16.DrawCel(view, loopNo, celNo, celRect, (byte)priority, 0);

            if (control != -1)
            {
                celRect.Top = ScummHelper.Clip(_ports.KernelPriorityToCoordinate((byte)priority) - 1, celRect.Top, celRect.Bottom - 1);
                _paint16.FillRect(celRect, GfxScreenMasks.CONTROL, 0, 0, (byte)control);
            }
        }
    }
}
