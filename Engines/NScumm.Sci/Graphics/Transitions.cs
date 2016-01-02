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
using NScumm.Core.Graphics;

namespace NScumm.Sci.Graphics
{
    enum SciTransition
    {
        VERTICALROLL_FROMCENTER = 0,
        HORIZONTALROLL_FROMCENTER = 1,
        STRAIGHT_FROM_RIGHT = 2,
        STRAIGHT_FROM_LEFT = 3,
        STRAIGHT_FROM_BOTTOM = 4,
        STRAIGHT_FROM_TOP = 5,
        DIAGONALROLL_FROMCENTER = 6,
        DIAGONALROLL_TOCENTER = 7,
        BLOCKS = 8,
        PIXELATION = 9,
        FADEPALETTE = 10,
        SCROLL_RIGHT = 11,
        SCROLL_LEFT = 12,
        SCROLL_UP = 13,
        SCROLL_DOWN = 14,
        NONE_LONGBOW = 15,
        NONE = 100,
        // here are transitions that are used by the old tableset, but are not included anymore in the new tableset
        VERTICALROLL_TOCENTER = 300,
        HORIZONTALROLL_TOCENTER = 301
    }

    struct GfxTransitionTranslateEntry
    {
        public short orgId;
        public SciTransition newId;
        public bool blackoutFlag;
    }

    /// <summary>
    /// Transitions class, handles doing transitions for SCI0.SCI1.1 games like fade out/fade in, mosaic effect, etc.
    /// </summary>
    internal class GfxTransitions
    {
        private GfxPalette _palette;
        private GfxScreen _screen;
        private byte[] _oldScreen; // buffer for saving current active screen data to, has dimenions of _screen->_displayScreen
        private GfxTransitionTranslateEntry[] _translationTable;
        private SciTransition _number;
        private bool _blackoutFlag;

        // This table contains a mapping between oldIDs (prior SCI1LATE) and newIDs
        private static readonly GfxTransitionTranslateEntry[] oldTransitionIDs = {
            new GfxTransitionTranslateEntry { orgId =   0, newId = SciTransition.VERTICALROLL_FROMCENTER,   blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =   1, newId = SciTransition.HORIZONTALROLL_FROMCENTER, blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =   2, newId = SciTransition.STRAIGHT_FROM_RIGHT,       blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =   3, newId = SciTransition.STRAIGHT_FROM_LEFT,        blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =   4, newId = SciTransition.STRAIGHT_FROM_BOTTOM,      blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =   5, newId = SciTransition.STRAIGHT_FROM_TOP,         blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =   6, newId = SciTransition.DIAGONALROLL_TOCENTER,     blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =   7, newId = SciTransition.DIAGONALROLL_FROMCENTER,   blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =   8, newId = SciTransition.BLOCKS,                    blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =   9, newId = SciTransition.VERTICALROLL_TOCENTER,     blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =  10, newId = SciTransition.HORIZONTALROLL_TOCENTER,   blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =  11, newId = SciTransition.STRAIGHT_FROM_RIGHT,       blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId =  12, newId = SciTransition.STRAIGHT_FROM_LEFT,        blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId =  13, newId = SciTransition.STRAIGHT_FROM_BOTTOM,      blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId =  14, newId = SciTransition.STRAIGHT_FROM_TOP,         blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId =  15, newId = SciTransition.DIAGONALROLL_FROMCENTER,   blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId =  16, newId = SciTransition.DIAGONALROLL_TOCENTER,     blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId =  17, newId = SciTransition.BLOCKS,                    blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId =  18, newId = SciTransition.PIXELATION,                blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =  27, newId = SciTransition.PIXELATION   ,             blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId =  30, newId = SciTransition.FADEPALETTE,               blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =  40, newId = SciTransition.SCROLL_RIGHT,              blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =  41, newId = SciTransition.SCROLL_LEFT,               blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =  42, newId = SciTransition.SCROLL_UP,                 blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId =  43, newId = SciTransition.SCROLL_DOWN,               blackoutFlag = false },
            new GfxTransitionTranslateEntry { orgId = 100, newId = SciTransition.NONE,                      blackoutFlag = false },
        };

        public GfxTransitions(GfxScreen screen, GfxPalette palette)
        {
            _screen = screen;
            _palette = palette;

            Init();
        }

        private void Init()
        {
            _oldScreen = new byte[_screen.DisplayHeight * _screen.DisplayWidth];

            if (ResourceManager.GetSciVersion() >= SciVersion.V1_LATE)
                _translationTable = null;
            else
                _translationTable = oldTransitionIDs;

            // setup default transition
            _number = SciTransition.HORIZONTALROLL_FROMCENTER;
            _blackoutFlag = false;
        }

        public void DoIt(Rect picRect)
        {
            throw new NotImplementedException();
        }
    }
}
