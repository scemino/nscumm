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
using NScumm.Sci.Engine;
using NScumm.Core.Graphics;
using NScumm.Core;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// Compare class, handles compare operations graphic-wise (like when checking control screen for a pattern etc.)
    /// </summary>
    internal class GfxCompare
    {
        private GfxCache _cache;
        private GfxCoordAdjuster _coordAdjuster;
        private GfxScreen _screen;
        private SegManager _segMan;

        public GfxCompare(SegManager segMan, GfxCache cache, GfxScreen screen, GfxCoordAdjuster coordAdjuster)
        {
            _segMan = segMan;
            _cache = cache;
            _screen = screen;
            _coordAdjuster = coordAdjuster;
        }

        public void KernelBaseSetter(Register @object)
        {
            Register tmp;
            if (SciEngine.LookupSelector(_segMan, @object, SciEngine.Selector(s => s.brLeft), null, out tmp) == SelectorType.Variable)
            {
                short x = (short)SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.x));
                short y = (short)SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.y));
                short z = (short)((SciEngine.Selector(s => s.z) > -1) ? SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.z)) : 0);
                short yStep = (short)SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.yStep));
                int viewId = (short)SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.view));
                short loopNo = (short)SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.loop));
                short celNo = (short)SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.cel));

                // HACK: Ignore invalid views for now (perhaps unimplemented text views?)
                if (viewId == 0xFFFF)   // invalid view
                    return;

                ViewScaleSignals scaleSignal = 0;
                if (ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                {
                    scaleSignal = (ViewScaleSignals)SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.scaleSignal));
                }

                Rect celRect = new Rect();

                GfxView tmpView = _cache.GetView(viewId);
                if (!tmpView.IsScaleable)
                    scaleSignal = 0;

                if ((scaleSignal & ViewScaleSignals.DoScaling) != 0)
                {
                    celRect = GetNSRect(@object);
                }
                else {
                    if (tmpView.IsSci2Hires)
                        tmpView.AdjustToUpscaledCoordinates(y, x);

                    celRect = tmpView.GetCelRect(loopNo, celNo, x, y, z);

                    if (tmpView.IsSci2Hires)
                    {
                        tmpView.AdjustBackUpscaledCoordinates(celRect.Top, celRect.Left);
                        tmpView.AdjustBackUpscaledCoordinates(celRect.Bottom, celRect.Right);
                    }
                }

                celRect.Bottom = y + 1;
                celRect.Top = celRect.Bottom - yStep;

                SciEngine.WriteSelectorValue(_segMan, @object, SciEngine.Selector(s => s.brLeft), (ushort)celRect.Left);
                SciEngine.WriteSelectorValue(_segMan, @object, SciEngine.Selector(s => s.brRight), (ushort)celRect.Right);
                SciEngine.WriteSelectorValue(_segMan, @object, SciEngine.Selector(s => s.brTop), (ushort)celRect.Top);
                SciEngine.WriteSelectorValue(_segMan, @object, SciEngine.Selector(s => s.brBottom), (ushort)celRect.Bottom);
            }
        }

        public Register KernelCanBeHere(Register curObject, Register listReference)
        {
            Rect checkRect;
            Rect adjustedRect;
            ushort controlMask;
            ushort result;

            checkRect.Left = (int)SciEngine.ReadSelectorValue(_segMan, curObject, SciEngine.Selector(s => s.brLeft));
            checkRect.Top = (int)SciEngine.ReadSelectorValue(_segMan, curObject, SciEngine.Selector(s => s.brTop));
            checkRect.Right = (int)SciEngine.ReadSelectorValue(_segMan, curObject, SciEngine.Selector(s => s.brRight));
            checkRect.Bottom = (int)SciEngine.ReadSelectorValue(_segMan, curObject, SciEngine.Selector(s => s.brBottom));

            if (!checkRect.IsValidRect)
            {   // can occur in Iceman and Mother Goose - HACK? TODO: is this really occuring in sierra sci? check this
                // TODO: warning("kCan(t)BeHere - invalid rect %d, %d . %d, %d", checkRect.left, checkRect.top, checkRect.right, checkRect.bottom);
                return Register.NULL_REG; // this means "can be here"
            }

            adjustedRect = _coordAdjuster.OnControl(checkRect);

            var signal = (ViewSignals)SciEngine.ReadSelectorValue(_segMan, curObject, SciEngine.Selector(s => s.signal));
            controlMask = (ushort)SciEngine.ReadSelectorValue(_segMan, curObject, SciEngine.Selector(s => s.illegalBits));
            result = (ushort)(IsOnControl(GfxScreenMasks.CONTROL, adjustedRect) & controlMask);
            if ((result == 0) && (signal & (ViewSignals.IgnoreActor | ViewSignals.RemoveView)) == 0)
            {
                List list = _segMan.LookupList(listReference);
                if (list == null)
                    throw new InvalidOperationException("kCanBeHere called with non-list as parameter");

                return CanBeHereCheckRectList(curObject, checkRect, list);
            }
            return Register.Make(0, result);
        }

        private Register CanBeHereCheckRectList(Register checkObject, Rect checkRect, List list)
        {
            Register curAddress = list.first;
            Node curNode = _segMan.LookupNode(curAddress);
            Register curObject;
            ViewSignals signal;
            Rect curRect;

            while (curNode != null)
            {
                curObject = curNode.value;
                if (curObject != checkObject)
                {
                    signal = (ViewSignals)SciEngine.ReadSelectorValue(_segMan, curObject, SciEngine.Selector(s => s.signal));
                    if ((signal & (ViewSignals.IgnoreActor | ViewSignals.RemoveView | ViewSignals.NoUpdate)) == 0)
                    {
                        curRect.Left = (int)SciEngine.ReadSelectorValue(_segMan, curObject, SciEngine.Selector(s => s.brLeft));
                        curRect.Top = (int)SciEngine.ReadSelectorValue(_segMan, curObject, SciEngine.Selector(s => s.brTop));
                        curRect.Right = (int)SciEngine.ReadSelectorValue(_segMan, curObject, SciEngine.Selector(s => s.brRight));
                        curRect.Bottom = (int)SciEngine.ReadSelectorValue(_segMan, curObject, SciEngine.Selector(s => s.brBottom));
                        // Check if curRect is within checkRect
                        // This behavior is slightly odd, but it's how the original SCI
                        // engine did it: a rect cannot be contained within itself
                        // (there is no equality). Do NOT change this to contains(), as
                        // it breaks KQ4 early (bug #3315639).
                        if (curRect.Right > checkRect.Left &&
                            curRect.Left < checkRect.Right &&
                            curRect.Bottom > checkRect.Top &&
                            curRect.Top < checkRect.Bottom)
                            return curObject;
                    }
                }
                curAddress = curNode.succ;
                curNode = _segMan.LookupNode(curAddress);
            }
            return Register.NULL_REG;
        }

        private ushort IsOnControl(GfxScreenMasks screenMask, Rect rect)
        {
            short x, y;
            ushort result = 0;

            if (rect.IsEmpty)
                return 0;

            if (screenMask.HasFlag(GfxScreenMasks.PRIORITY))
            {
                for (y = (short)rect.Top; y < rect.Bottom; y++)
                {
                    for (x = (short)rect.Left; x < rect.Right; x++)
                    {
                        result |= (ushort)(1 << _screen.GetPriority(x, y));
                    }
                }
            }
            else {
                for (y = (short)rect.Top; y < rect.Bottom; y++)
                {
                    for (x = (short)rect.Left; x < rect.Right; x++)
                    {
                        result |= (ushort)(1 << _screen.GetControl(x, y));
                    }
                }
            }
            return result;
        }

        public Rect GetNSRect(Register @object)
        {
            Rect nsRect = new Rect();
            nsRect.Top = (short)SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.nsTop));
            nsRect.Left = (short)SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.nsLeft));
            nsRect.Bottom = (short)SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.nsBottom));
            nsRect.Right = (short)SciEngine.ReadSelectorValue(_segMan, @object, SciEngine.Selector(s => s.nsRight));

            return nsRect;
        }

        public void SetNSRect(Register @object, Rect nsRect)
        {
            SciEngine.WriteSelectorValue(_segMan, @object, SciEngine.Selector(s => s.nsLeft), (ushort)nsRect.Left);
            SciEngine.WriteSelectorValue(_segMan, @object, SciEngine.Selector(s => s.nsTop), (ushort)nsRect.Top);
            SciEngine.WriteSelectorValue(_segMan, @object, SciEngine.Selector(s => s.nsRight), (ushort)nsRect.Right);
            SciEngine.WriteSelectorValue(_segMan, @object, SciEngine.Selector(s => s.nsBottom), (ushort)nsRect.Bottom);
        }

        public void KernelSetNowSeen(Register objectReference)
        {
            GfxView view = null;
            Rect celRect = new Rect(0, 0);
            int viewId = (int)SciEngine.ReadSelectorValue(_segMan, objectReference, o => o.view);

            // HACK: Ignore invalid views for now (perhaps unimplemented text views?)
            if (viewId == 0xFFFF)   // invalid view
                return;

            short loopNo = (short)SciEngine.ReadSelectorValue(_segMan, objectReference, o => o.loop);
            short celNo = (short)SciEngine.ReadSelectorValue(_segMan, objectReference, o => o.cel);
            short x = (short)SciEngine.ReadSelectorValue(_segMan, objectReference, o => o.x);
            short y = (short)SciEngine.ReadSelectorValue(_segMan, objectReference, o => o.y);
            short z = 0;
            if (SciEngine.Selector(o => o.z) > -1)
                z = (short)SciEngine.ReadSelectorValue(_segMan, objectReference, SciEngine.Selector(o => o.z));

            view = _cache.GetView(viewId);

# if ENABLE_SCI32
            if (view.isSci2Hires())
                view.adjustToUpscaledCoordinates(y, x);
            else if (getSciVersion() == SCI_VERSION_2_1)
                _coordAdjuster.fromScriptToDisplay(y, x);
#endif

            celRect = view.GetCelRect(loopNo, celNo, x, y, z);

# if ENABLE_SCI32
            if (view.isSci2Hires())
            {
                view.adjustBackUpscaledCoordinates(celRect.top, celRect.left);
                view.adjustBackUpscaledCoordinates(celRect.bottom, celRect.right);
            }
            else if (getSciVersion() == SCI_VERSION_2_1)
            {
                _coordAdjuster.fromDisplayToScript(celRect.top, celRect.left);
                _coordAdjuster.fromDisplayToScript(celRect.bottom, celRect.right);
            }
#endif

            Register tmp;
            if (SciEngine.LookupSelector(_segMan, objectReference, o => o.nsTop, null, out tmp) == SelectorType.Variable)
            {
                SetNSRect(objectReference, celRect);
            }
        }

        public ushort KernelOnControl(GfxScreenMasks screenMask, Rect rect)
        {
            Rect adjustedRect = _coordAdjuster.OnControl(rect);

            ushort result = IsOnControl(screenMask, adjustedRect);
            return result;
        }

        public bool KernelIsItSkip(int viewId, short loopNo, short celNo, Point position)
        {
            GfxView tmpView = _cache.GetView(viewId);
            CelInfo celInfo = tmpView.GetCelInfo(loopNo, celNo);
            position.X = ScummHelper.Clip(position.X, 0, celInfo.width - 1);
            position.Y = ScummHelper.Clip(position.Y, 0, celInfo.height - 1);
            var celData = tmpView.GetBitmap(loopNo, celNo);
            bool result = (celData[position.Y * celInfo.width + position.X] == celInfo.clearKey);
            return result;
        }
    }
}
