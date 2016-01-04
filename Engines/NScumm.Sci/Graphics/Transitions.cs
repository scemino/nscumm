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
using NScumm.Core;

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

    class GfxTransitionTranslateEntry
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
        private byte[] _oldScreen; // buffer for saving current active screen data to, has dimenions of _screen._displayScreen
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

        // this table defines the blackout-transition that needs to be done prior doing the actual transition
        static GfxTransitionTranslateEntry[] blackoutTransitionIDs = {
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.VERTICALROLL_FROMCENTER,          newId = SciTransition.VERTICALROLL_TOCENTER,      blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.HORIZONTALROLL_FROMCENTER,        newId = SciTransition.HORIZONTALROLL_TOCENTER,    blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.STRAIGHT_FROM_RIGHT,              newId = SciTransition.STRAIGHT_FROM_LEFT,         blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.STRAIGHT_FROM_LEFT,               newId = SciTransition.STRAIGHT_FROM_RIGHT,        blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.STRAIGHT_FROM_BOTTOM,             newId = SciTransition.STRAIGHT_FROM_TOP,          blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.STRAIGHT_FROM_TOP,                newId = SciTransition.STRAIGHT_FROM_BOTTOM,       blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.DIAGONALROLL_FROMCENTER,          newId = SciTransition.DIAGONALROLL_TOCENTER,      blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.DIAGONALROLL_TOCENTER,            newId = SciTransition.DIAGONALROLL_FROMCENTER,    blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.BLOCKS,                           newId = SciTransition.BLOCKS,                     blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.PIXELATION,                       newId = SciTransition.PIXELATION,                 blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.FADEPALETTE,                      newId = SciTransition.NONE,                       blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.SCROLL_RIGHT,                     newId = SciTransition.NONE,                       blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.SCROLL_LEFT,                      newId = SciTransition.NONE,                       blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.SCROLL_UP,                        newId = SciTransition.NONE,                       blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.SCROLL_DOWN,                      newId = SciTransition.NONE,                       blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.NONE_LONGBOW,                     newId = SciTransition.NONE,                       blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.NONE,                             newId = SciTransition.NONE,                       blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.VERTICALROLL_TOCENTER,            newId = SciTransition.NONE,                       blackoutFlag = true },
            new GfxTransitionTranslateEntry { orgId = (short)SciTransition.HORIZONTALROLL_TOCENTER,          newId = SciTransition.NONE,                       blackoutFlag = true },
        };

        private Rect _picRect;
        private int _transitionStartTime;

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
            GfxTransitionTranslateEntry translationEntry;

            _picRect = picRect;

            if (_translationTable != null)
            {
                // We need to translate the ID
                translationEntry = TranslateNumber((short)_number, _translationTable);
                if (translationEntry != null)
                {
                    _number = translationEntry.newId;
                    _blackoutFlag = translationEntry.blackoutFlag;
                }
                else {
                    // TODO: warning("Transitions: old ID %d not supported", _number);
                    _number = SciTransition.NONE;
                    _blackoutFlag = false;
                }
            }

            if (_blackoutFlag)
            {
                // We need to find out what transition we are supposed to use for
                // blackout
                translationEntry = TranslateNumber((short)_number, blackoutTransitionIDs);
                if (translationEntry != null)
                {
                    DoTransition((short)translationEntry.newId, true);
                }
                else {
                    // TODO: warning("Transitions: ID %d not listed in blackoutTransitionIDs", _number);
                }
            }

            _palette.PalVaryPrepareForTransition();

            // Now we do the actual transition to the new screen
            DoTransition((short)_number, false);

            _screen._picNotValid = 0;
        }

        // This may get called twice, if blackoutFlag is set. It will get once called
        // with blackoutFlag set and another time with no blackoutFlag.
        private void DoTransition(short number, bool blackoutFlag)
        {
            if (number != (short)SciTransition.FADEPALETTE)
            {
                SetNewPalette(blackoutFlag);
            }

            _transitionStartTime = Environment.TickCount;
            switch ((SciTransition)number)
            {
                case SciTransition.VERTICALROLL_FROMCENTER:
                    VerticalRollFromCenter(blackoutFlag);
                    break;
                case SciTransition.VERTICALROLL_TOCENTER:
                    VerticalRollToCenter(blackoutFlag);
                    break;
                case SciTransition.HORIZONTALROLL_FROMCENTER:
                    HorizontalRollFromCenter(blackoutFlag);
                    break;
                case SciTransition.HORIZONTALROLL_TOCENTER:
                    HorizontalRollToCenter(blackoutFlag);
                    break;
                case SciTransition.DIAGONALROLL_TOCENTER:
                    DiagonalRollToCenter(blackoutFlag);
                    break;
                case SciTransition.DIAGONALROLL_FROMCENTER:
                    DiagonalRollFromCenter(blackoutFlag);
                    break;

                case SciTransition.STRAIGHT_FROM_RIGHT:
                case SciTransition.STRAIGHT_FROM_LEFT:
                case SciTransition.STRAIGHT_FROM_BOTTOM:
                case SciTransition.STRAIGHT_FROM_TOP:
                    Straight(number, blackoutFlag);
                    break;

                case SciTransition.PIXELATION:
                    Pixelation(blackoutFlag);
                    break;

                case SciTransition.BLOCKS:
                    Blocks(blackoutFlag);
                    break;

                case SciTransition.FADEPALETTE:
                    if (!blackoutFlag)
                    {
                        FadeOut(); SetNewScreen(blackoutFlag); FadeIn();
                    }
                    break;

                case SciTransition.SCROLL_RIGHT:
                case SciTransition.SCROLL_LEFT:
                case SciTransition.SCROLL_UP:
                case SciTransition.SCROLL_DOWN:
                    Scroll(number);
                    break;

                case SciTransition.NONE_LONGBOW:
                case SciTransition.NONE:
                    SetNewScreen(blackoutFlag);
                    break;

                default:
                    // TODO: warning("Transitions: ID %d not implemented", number);
                    SetNewScreen(blackoutFlag);
                    break;
            }
        }

        private void Scroll(short number)
        {
            throw new NotImplementedException();
        }

        private void FadeIn()
        {
            throw new NotImplementedException();
        }

        private void SetNewScreen(bool blackoutFlag)
        {
            throw new NotImplementedException();
        }

        private void FadeOut()
        {
            throw new NotImplementedException();
        }

        private void Blocks(bool blackoutFlag)
        {
            throw new NotImplementedException();
        }

        private void Pixelation(bool blackoutFlag)
        {
            throw new NotImplementedException();
        }

        private void Straight(short number, bool blackoutFlag)
        {
            throw new NotImplementedException();
        }

        private void DiagonalRollFromCenter(bool blackoutFlag)
        {
            throw new NotImplementedException();
        }

        // Diagonally displays new screen starting from edges - works on _picRect area
        // only. Assumes that height of rect is larger than width.
        private void DiagonalRollToCenter(bool blackoutFlag)
        {
            Rect upperRect = new Rect(_picRect.Left, _picRect.Top, _picRect.Right, _picRect.Top + 1);
            Rect lowerRect = new Rect(_picRect.Left, _picRect.Bottom - 1, _picRect.Right, _picRect.Bottom);
            Rect leftRect = new Rect(_picRect.Left, _picRect.Top, _picRect.Left + 1, _picRect.Bottom);
            Rect rightRect = new Rect(_picRect.Right - 1, _picRect.Top, _picRect.Right, _picRect.Bottom);
            int msecCount = 0;

            while (upperRect.Top < lowerRect.Bottom)
            {
                CopyRectToScreen(upperRect, blackoutFlag); upperRect.Translate(0, 1); upperRect.Left++; upperRect.Right--;
                CopyRectToScreen(lowerRect, blackoutFlag); lowerRect.Translate(0, -1); lowerRect.Left++; lowerRect.Right--;
                CopyRectToScreen(leftRect, blackoutFlag); leftRect.Translate(1, 0);
                CopyRectToScreen(rightRect, blackoutFlag); rightRect.Translate(-1, 0);
                msecCount += 4;
                UpdateScreenAndWait(msecCount);
            }
        }

        private void UpdateScreenAndWait(int shouldBeAtMsec)
        {
            // Common::Event ev;

            // TODO: while (g_system->getEventManager()->pollEvent(ev)) { }  // discard all events

            SciEngine.Instance.System.GraphicsManager.UpdateScreen();
            // if we have still some time left, delay accordingly
            int msecPos = Environment.TickCount - _transitionStartTime;
            if (shouldBeAtMsec > msecPos)
                ServiceLocator.Platform.Sleep(shouldBeAtMsec - msecPos);
        }

        private void CopyRectToScreen(Rect rect, bool blackoutFlag)
        {
            if (!blackoutFlag)
            {
                _screen.CopyRectToScreen(rect);
            }
            else {
                Surface surface = SciEngine.Instance.System.GraphicsManager.Capture();
                if (_screen.UpscaledHires == 0)
                {
                    surface.FillRect(rect, 0);
                }
                else {
                    Rect upscaledRect = rect;
                    _screen.AdjustToUpscaledCoordinates(ref upscaledRect.Top, ref upscaledRect.Left);
                    _screen.AdjustToUpscaledCoordinates(ref upscaledRect.Bottom, ref upscaledRect.Right);
                    surface.FillRect(upscaledRect, 0);
                }
                SciEngine.Instance.System.GraphicsManager.CopyRectToScreen(surface.Pixels, surface.Pitch, 0, 0, surface.Width, surface.Height);
            }
        }

        private void HorizontalRollToCenter(bool blackoutFlag)
        {
            throw new NotImplementedException();
        }

        private void HorizontalRollFromCenter(bool blackoutFlag)
        {
            throw new NotImplementedException();
        }

        private void VerticalRollToCenter(bool blackoutFlag)
        {
            throw new NotImplementedException();
        }

        private void VerticalRollFromCenter(bool blackoutFlag)
        {
            throw new NotImplementedException();
        }

        private void SetNewPalette(bool blackoutFlag)
        {
            if (!blackoutFlag)
                _palette.SetOnScreen();
        }

        // will translate a number and return corresponding translationEntry
        private GfxTransitionTranslateEntry TranslateNumber(short number, GfxTransitionTranslateEntry[] table)
        {
            for (int i = 0; i < table.Length; i++)
            {
                if (table[i].orgId == number)
                    return table[i];
            }
            return null;
        }

        public void Setup(short number, bool blackoutFlag)
        {
            if (number != -1)
            {
# if !DISABLE_TRANSITIONS
                _number = (SciTransition)number;
#else
                _number = SCI_TRANSITIONS_NONE;
#endif
                _blackoutFlag = blackoutFlag;
                // TODO: debugC(kDebugLevelGraphics, "Transition %d, blackout %d", number, blackoutFlag);
            }
        }
    }
}
