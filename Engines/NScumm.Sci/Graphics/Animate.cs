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


    struct AnimateEntry
    {
        public short givenOrderNo;
        public Register @object;
        public int viewId;
        public short loopNo;
        public short celNo;
        public short paletteNo;
        public short x, y, z;
        public short priority;
        public ushort signal;
        public ushort scaleSignal;
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
        private AnimateEntry[] _lastCastData;
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
            
            Init();
        }

        private void Init()
        {
            _lastCastData = new AnimateEntry[0];

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
            Fill(old_picNotValid);

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

        private void RestoreAndDelete(int argc, StackPtr? argv)
        {
            // This has to be done in a separate loop. At least in sq1 some .dispose
            // modifies FIXEDLOOP flag in signal for another object. In that case we
            // would overwrite the new signal with our version of the old signal.
            foreach (var it in _list)
            {
                // Finally update signal
                SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.signal), it.signal);
            }

            for (int i = _list.Count - 1; i > 0; i--)
            {
                var it = _list[i];
                // We read out signal here again, this is not by accident but to ensure
                // that we got an up-to-date signal
                it.signal = (ushort)SciEngine.ReadSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.signal));

                if ((it.signal & (ushort)(ViewSignals.NoUpdate | ViewSignals.RemoveView)) == 0)
                {
                    _paint16.BitsRestore(SciEngine.ReadSelector(_s._segMan, it.@object, SciEngine.Selector(s => s.underBits)));
                    SciEngine.WriteSelectorValue(_s._segMan, it.@object, SciEngine.Selector(s => s.underBits), 0);
                }

                if ((it.signal & (ushort)ViewSignals.DisposeMe)!=0)
                {
                    // Call .delete_ method of that object
                    SciEngine.InvokeSelector(_s, it.@object, SciEngine.Selector(s => s.delete_), argc, argv, 0);
                }
            }
        }

        private void UpdateScreen(byte old_picNotValid)
        {
            throw new NotImplementedException();
        }

        private void Update()
        {
            throw new NotImplementedException();
        }

        private void ThrottleSpeed()
        {
            throw new NotImplementedException();
        }

        private void DrawCels()
        {
            throw new NotImplementedException();
        }

        private void Fill(byte old_picNotValid)
        {
            throw new NotImplementedException();
        }

        private void MakeSortedList(List list)
        {
            throw new NotImplementedException();
        }

        private bool Invoke(List list, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
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
            Array.Clear(_lastCastData,0, _lastCastData.Length);
        }
    }
}
