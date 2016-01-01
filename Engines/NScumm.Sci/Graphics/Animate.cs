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

namespace NScumm.Sci.Graphics
{
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

            Init();
        }

        private void Init()
        {
            _lastCastData = null;

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
    }
}
