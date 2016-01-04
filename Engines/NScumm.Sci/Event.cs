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

using NScumm.Core.Graphics;
using System.Collections.Generic;
using System;

namespace NScumm.Sci
{
    internal class SciEvent
    {
        public const int SCI_KEYMOD_RSHIFT = (1 << 0);
        public const int SCI_KEYMOD_LSHIFT = (1 << 1);
        public const int SCI_KEYMOD_CTRL = (1 << 2);
        public const int SCI_KEYMOD_ALT = (1 << 3);
        public const int SCI_KEYMOD_SCRLOCK = (1 << 4);
        public const int SCI_KEYMOD_NUMLOCK = (1 << 5);
        public const int SCI_KEYMOD_CAPSLOCK = (1 << 6);
        public const int SCI_KEYMOD_INSERT = (1 << 7);

        public const int SCI_KEYMOD_NO_FOOLOCK = (~(SCI_KEYMOD_SCRLOCK | SCI_KEYMOD_NUMLOCK | SCI_KEYMOD_CAPSLOCK | SCI_KEYMOD_INSERT));
        public const int SCI_KEYMOD_ALL = 0xFF;

        public const int SCI_KEY_F1 = (59 << 8);
        public const int SCI_KEY_F2 = (60 << 8);
        public const int SCI_KEY_F3 = (61 << 8);
        public const int SCI_KEY_F4 = (62 << 8);
        public const int SCI_KEY_F5 = (63 << 8);
        public const int SCI_KEY_F6 = (64 << 8);
        public const int SCI_KEY_F7 = (65 << 8);
        public const int SCI_KEY_F8 = (66 << 8);
        public const int SCI_KEY_F9 = (67 << 8);
        public const int SCI_KEY_F10 = (68 << 8);

        /*Values for type*/
        public const int SCI_EVENT_NONE = 0;
        public const int SCI_EVENT_MOUSE_PRESS = (1 << 0);
        public const int SCI_EVENT_MOUSE_RELEASE = (1 << 1);
        public const int SCI_EVENT_KEYBOARD = (1 << 2);
        public const int SCI_EVENT_DIRECTION = (1 << 6);
        public const int SCI_EVENT_SAID = (1 << 7);
        /*Fake values for other events*/
        public const int SCI_EVENT_QUIT = (1 << 11);
        public const int SCI_EVENT_PEEK = (1 << 15);
        public const int SCI_EVENT_ANY = 0x7fff;

        public short type;
        public short data;
        public short modifiers;
        /**
         * For keyboard events: 'data' after applying
         * the effects of 'modifiers', e.g. if
         *   type == SCI_EVT_KEYBOARD
         *   data == 'a'
         *   buckybits == SCI_EVM_LSHIFT
         * then
         *   character == 'A'
         * For 'Alt', characters are interpreted by their
         * PC keyboard scancodes.
         */
        public short character;

        /**
         * The mouse position at the time the event was created.
         *
         * These are display coordinates!
         */
        public Point mousePos;
    }

    internal class EventManager
    {
        private readonly bool _fontIsExtended;
        private List<SciEvent> _events;

        public EventManager(bool fontIsExtended)
        {
            _fontIsExtended = fontIsExtended;
            _events = new List<SciEvent>();
        }

        internal void GetSciEvent(int sCI_EVENT_PEEK)
        {
            throw new NotImplementedException();
        }

        public void UpdateScreen()
        {
            // Update the screen here, since it's called very often.
            // Throttle the screen update rate to 60fps.
            var s = SciEngine.Instance.EngineState;
            if (Environment.TickCount - s._screenUpdateTime >= 1000 / 60)
            {
                SciEngine.Instance.System.GraphicsManager.UpdateScreen();
                s._screenUpdateTime = Environment.TickCount;
                // Throttle the checking of shouldQuit() to 60fps as well, since
                // Engine::shouldQuit() invokes 2 virtual functions
                // (EventManager::shouldQuit() and EventManager::shouldRTL()),
                // which is very expensive to invoke constantly without any
                // throttling at all.
                if (SciEngine.Instance.ShouldQuit)
                    s.abortScriptProcessing = Engine.AbortGameState.QuitGame;
            }
        }
    }
}
