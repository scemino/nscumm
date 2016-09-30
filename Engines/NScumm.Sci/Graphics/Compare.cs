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
            if (SciEngine.LookupSelector(_segMan, @object, s => s.brLeft, null, out tmp) == SelectorType.Variable)
            {
                short x = (short)SciEngine.ReadSelectorValue(_segMan, @object, s => s.x);
                short y = (short)SciEngine.ReadSelectorValue(_segMan, @object, s => s.y);
                short z = (short)((SciEngine.Selector(s => s.z) > -1) ? SciEngine.ReadSelectorValue(_segMan, @object, s => s.z) : 0);
                var yStep = (short)SciEngine.ReadSelectorValue(_segMan, @object, s => s.yStep);
                int viewId = (short)SciEngine.ReadSelectorValue(_segMan, @object, s => s.view);
                var loopNo = (short)SciEngine.ReadSelectorValue(_segMan, @object, s => s.loop);
                var celNo = (short)SciEngine.ReadSelectorValue(_segMan, @object, s => s.cel);

                // HACK: Ignore invalid views for now (perhaps unimplemented text views?)
                if (viewId == 0xFFFF)   // invalid view
                    return;

                ViewScaleSignals scaleSignal = 0;
                if (ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                {
                    scaleSignal = (ViewScaleSignals)SciEngine.ReadSelectorValue(_segMan, @object, s => s.scaleSignal);
                }

                var celRect = new Rect();

                var tmpView = _cache.GetView(viewId);
                if (!tmpView.IsScaleable)
                    scaleSignal = 0;

                if ((scaleSignal & ViewScaleSignals.DoScaling) != 0)
                {
                    celRect = GetNSRect(@object);
                }
                else {
                    if (tmpView.IsSci2Hires)
						tmpView.AdjustToUpscaledCoordinates(ref y,ref  x);

					tmpView.GetCelRect(loopNo, celNo, x, y, z, out celRect);

                    if (tmpView.IsSci2Hires)
                    {
						tmpView.AdjustBackUpscaledCoordinates(ref celRect.Top, ref celRect.Left);
						tmpView.AdjustBackUpscaledCoordinates(ref celRect.Bottom, ref celRect.Right);
                    }
                }

                celRect.Bottom = (short) (y + 1);
                celRect.Top = (short) (celRect.Bottom - yStep);

                SciEngine.WriteSelectorValue(_segMan, @object, s => s.brLeft, (ushort)celRect.Left);
                SciEngine.WriteSelectorValue(_segMan, @object, s => s.brRight, (ushort)celRect.Right);
                SciEngine.WriteSelectorValue(_segMan, @object, s => s.brTop, (ushort)celRect.Top);
                SciEngine.WriteSelectorValue(_segMan, @object, s => s.brBottom, (ushort)celRect.Bottom);
            }
        }

        public Register KernelCanBeHere(Register curObject, Register listReference)
        {
            Rect checkRect;
            Rect adjustedRect;
            ushort controlMask;
            ushort result;

            checkRect.Left = (short) SciEngine.ReadSelectorValue(_segMan, curObject, s => s.brLeft);
            checkRect.Top = (short) SciEngine.ReadSelectorValue(_segMan, curObject, s => s.brTop);
            checkRect.Right = (short) SciEngine.ReadSelectorValue(_segMan, curObject, s => s.brRight);
            checkRect.Bottom = (short) SciEngine.ReadSelectorValue(_segMan, curObject, s => s.brBottom);

            if (!checkRect.IsValidRect)
            {   // can occur in Iceman and Mother Goose - HACK? TODO: is this really occuring in sierra sci? check this
                // TODO: warning("kCan(t)BeHere - invalid rect %d, %d . %d, %d", checkRect.left, checkRect.top, checkRect.right, checkRect.bottom);
                return Register.NULL_REG; // this means "can be here"
            }

            adjustedRect = _coordAdjuster.OnControl(checkRect);

            var signal = (ViewSignals)SciEngine.ReadSelectorValue(_segMan, curObject, s => s.signal);
            controlMask = (ushort)SciEngine.ReadSelectorValue(_segMan, curObject, s => s.illegalBits);
            result = (ushort)(IsOnControl(GfxScreenMasks.CONTROL, adjustedRect) & controlMask);
            if ((result == 0) && (signal & (ViewSignals.IgnoreActor | ViewSignals.RemoveView)) == 0)
            {
                var list = _segMan.LookupList(listReference);
                if (list == null)
                    throw new InvalidOperationException("kCanBeHere called with non-list as parameter");

                return CanBeHereCheckRectList(curObject, checkRect, list);
            }
            return Register.Make(0, result);
        }

        private Register CanBeHereCheckRectList(Register checkObject, Rect checkRect, List list)
        {
            var curAddress = list.first;
            var curNode = _segMan.LookupNode(curAddress);
            Register curObject;
            ViewSignals signal;
            Rect curRect;

            while (curNode != null)
            {
                curObject = curNode.value;
                if (curObject != checkObject)
                {
                    signal = (ViewSignals)SciEngine.ReadSelectorValue(_segMan, curObject, s => s.signal);
                    if ((signal & (ViewSignals.IgnoreActor | ViewSignals.RemoveView | ViewSignals.NoUpdate)) == 0)
                    {
                        curRect.Left = (short) SciEngine.ReadSelectorValue(_segMan, curObject, s => s.brLeft);
                        curRect.Top = (short) SciEngine.ReadSelectorValue(_segMan, curObject, s => s.brTop);
                        curRect.Right = (short) SciEngine.ReadSelectorValue(_segMan, curObject, s => s.brRight);
                        curRect.Bottom = (short) SciEngine.ReadSelectorValue(_segMan, curObject, s => s.brBottom);
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
            var nsRect = new Rect();
            nsRect.Top = (short)SciEngine.ReadSelectorValue(_segMan, @object, s => s.nsTop);
            nsRect.Left = (short)SciEngine.ReadSelectorValue(_segMan, @object, s => s.nsLeft);
            nsRect.Bottom = (short)SciEngine.ReadSelectorValue(_segMan, @object, s => s.nsBottom);
            nsRect.Right = (short)SciEngine.ReadSelectorValue(_segMan, @object, s => s.nsRight);

            return nsRect;
        }

        public void SetNSRect(Register @object, Rect nsRect)
        {
            SciEngine.WriteSelectorValue(_segMan, @object, s => s.nsLeft, (ushort)nsRect.Left);
            SciEngine.WriteSelectorValue(_segMan, @object, s => s.nsTop, (ushort)nsRect.Top);
            SciEngine.WriteSelectorValue(_segMan, @object, s => s.nsRight, (ushort)nsRect.Right);
            SciEngine.WriteSelectorValue(_segMan, @object, s => s.nsBottom, (ushort)nsRect.Bottom);
        }

        public void KernelSetNowSeen(Register objectReference)
        {
            var viewId = (int) SciEngine.ReadSelectorValue(_segMan, objectReference, o => o.view);
            var loopNo = (short) SciEngine.ReadSelectorValue(_segMan, objectReference, o => o.loop);
            var celNo = (short) SciEngine.ReadSelectorValue(_segMan, objectReference, o => o.cel);
            var x = (short) SciEngine.ReadSelectorValue(_segMan, objectReference, o => o.x);
            var y = (short) SciEngine.ReadSelectorValue(_segMan, objectReference, o => o.y);
            short z = 0;
            if (SciEngine.Selector(o => o.z) > -1)
                z = (short) SciEngine.ReadSelectorValue(_segMan, objectReference, o => o.z);

            var view = _cache.GetView(viewId);
            Rect celRect;
            view.GetCelRect(loopNo, celNo, x, y, z, out celRect);

            if (SciEngine.LookupSelector(_segMan, objectReference, o => o.nsTop, null) == SelectorType.Variable)
            {
                SetNSRect(objectReference, celRect);
            }
        }

        public ushort KernelOnControl(GfxScreenMasks screenMask, Rect rect)
        {
            var adjustedRect = _coordAdjuster.OnControl(rect);

            var result = IsOnControl(screenMask, adjustedRect);
            return result;
        }

        public bool KernelIsItSkip(int viewId, short loopNo, short celNo, Point position)
        {
            var tmpView = _cache.GetView(viewId);
            var celInfo = tmpView.GetCelInfo(loopNo, celNo);
            position.X = (short) ScummHelper.Clip(position.X, 0, celInfo.width - 1);
            position.Y = (short) ScummHelper.Clip(position.Y, 0, celInfo.height - 1);
            var celData = tmpView.GetBitmap(loopNo, celNo);
            var result = (celData[position.Y * celInfo.width + position.X] == celInfo.clearKey);
            return result;
        }
    }
}
