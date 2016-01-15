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
using System.Linq;
using NScumm.Core.Input;

namespace NScumm.Sci
{
    internal class SciEvent
    {
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

        /* Keycodes of special keys: */
        public const int SCI_KEY_ESC = 27;
        public const int SCI_KEY_BACKSPACE = 8;
        public const int SCI_KEY_ENTER = 13;
        public const int SCI_KEY_TAB = '\t';
        public const int SCI_KEY_SHIFT_TAB = (0xf << 8);

        public const int SCI_KEY_HOME = (71 << 8);  // 7
        public const int SCI_KEY_UP = (72 << 8);    // 8
        public const int SCI_KEY_PGUP = (73 << 8);// 9
                                                  //
        public const int SCI_KEY_LEFT = (75 << 8);  // 4
        public const int SCI_KEY_CENTER = (76 << 8);    // 5
        public const int SCI_KEY_RIGHT = (77 << 8); // 6
                                                    //
        public const int SCI_KEY_END = (79 << 8);   // 1
        public const int SCI_KEY_DOWN = (80 << 8);  // 2
        public const int SCI_KEY_PGDOWN = (81 << 8);    // 3
                                                        //
        public const int SCI_KEY_INSERT = (82 << 8);    // 0
        public const int SCI_KEY_DELETE = (83 << 8);// .

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

        /*Values for buckybits */
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

        public SciEvent(short type, short data, short modifiers, short character, Point mousePos)
        {
            this.type = type;
            this.data = data;
            this.modifiers = modifiers;
            this.character = character;
            this.mousePos = mousePos;
        }
    }

    internal class EventManager
    {
        private readonly bool _fontIsExtended;
        private List<SciEvent> _events;
        private ScummInputState _oldState;

        public EventManager(bool fontIsExtended)
        {
            _fontIsExtended = fontIsExtended;
            _events = new List<SciEvent>();
        }

        public SciEvent GetSciEvent(int mask)
        {
            SciEvent @event = new SciEvent(0, 0, 0, 0, new Point(0, 0));

            UpdateScreen();

            // Get all queued events from graphics driver
            do
            {
                @event = GetScummVMEvent();
                if (@event.type != SciEvent.SCI_EVENT_NONE)
                    _events.Add(@event);
            } while (@event.type != SciEvent.SCI_EVENT_NONE);

            // Search for matching event in queue
            //Common::List<SciEvent>::iterator iter = _events.begin();
            //while (iter != _events.end() && !((*iter).type & mask))
            //    ++iter;
            var e = _events.FirstOrDefault(o => (o.type & mask) != 0);

            if (e != null)
            {
                // Event found
                @event = e;

                // If not peeking at the queue, remove the event
                if ((mask & SciEvent.SCI_EVENT_PEEK) == 0)
                    _events.Remove(e);
            }
            else {
                // No event found: we must return a SCI_EVT_NONE event.

                // Because event.type is SCI_EVT_NONE already here,
                // there is no need to change it.
            }

            return @event;
        }

        private SciEvent GetScummVMEvent()
        {
            SciEvent input = new SciEvent(SciEvent.SCI_EVENT_NONE, 0, 0, 0, new Point(0, 0));
            SciEvent noEvent = new SciEvent(SciEvent.SCI_EVENT_NONE, 0, 0, 0, new Point(0, 0));

            var im = SciEngine.Instance.System.InputManager;

            // Save the mouse position
            //
            // We call getMousePos of the event manager here, since we also want to
            // store the mouse position in case of keyboard events, which do not feature
            // any mouse position information itself.
            // This should be safe, since the mouse position in the event manager should
            // only be updated when a mouse related event has been taken from the queue
            // via pollEvent.
            // We also adjust the position based on the scaling of the screen.
            Point mousePos = im.GetMousePosition();
            SciEngine.Instance._gfxScreen.AdjustBackUpscaledCoordinates(ref mousePos.Y, ref mousePos.X);

            noEvent.mousePos = input.mousePos = mousePos;

            // TODO:
            var state = im.GetState();
            //if (state.GetKeys().Count==0)
            //{
            //    int modifiers = em->getModifierState();
            //    noEvent.modifiers =
            //        ((modifiers & Common::KBD_ALT) ? SCI_KEYMOD_ALT : 0) |
            //        ((modifiers & Common::KBD_CTRL) ? SCI_KEYMOD_CTRL : 0) |
            //        ((modifiers & Common::KBD_SHIFT) ? SCI_KEYMOD_LSHIFT | SCI_KEYMOD_RSHIFT : 0);

            //    return noEvent;
            //}
            //if (ev.type == Common::EVENT_QUIT)
            //{
            //    input.type = SCI_EVENT_QUIT;
            //    return input;
            //}

            // Handle mouse events
            //for (int i = 0; i < mouseEventMappings.Length; i++)
            //{
            //    if (mouseEventMappings[i].commonType == ev.type)
            //    {
            //        input.type = mouseEventMappings[i].sciType;
            //        input.data = mouseEventMappings[i].data;
            //        return input;
            //    }
            //}
            if (_oldState.IsLeftButtonDown)
            {
                if (!state.IsLeftButtonDown)
                {
                    input.type = SciEvent.SCI_EVENT_MOUSE_RELEASE;
                    input.data = 1;
                    _oldState = state;
                    return input;
                }
            }
            else
            {
                if (state.IsLeftButtonDown)
                {
                    input.type = SciEvent.SCI_EVENT_MOUSE_PRESS;
                    input.data = 1;
                    _oldState = state;
                    return input;
                }
            }
            if (_oldState.IsRightButtonDown)
            {
                if (!state.IsRightButtonDown)
                {
                    input.type = SciEvent.SCI_EVENT_MOUSE_RELEASE;
                    input.data = 2;
                    _oldState = state;
                    return input;
                }
            }
            else
            {
                if (state.IsRightButtonDown)
                {
                    input.type = SciEvent.SCI_EVENT_MOUSE_PRESS;
                    input.data = 2;
                    _oldState = state;
                    return input;
                }
            }
            _oldState = state;

            // If we reached here, make sure that it's a keydown event
            //if (ev.type != Common::EVENT_KEYDOWN)
            //    return noEvent;

            //// Check for Control-D (debug console)
            //if (ev.kbd.hasFlags(Common::KBD_CTRL | Common::KBD_SHIFT) && ev.kbd.keycode == Common::KEYCODE_d)
            //{
            //    // Open debug console
            //    Console* con = g_sci->getSciDebugger();
            //    con->attach();
            //    return noEvent;
            //}

            //// Process keyboard events

            //int modifiers = em->getModifierState();
            //bool numlockOn = (ev.kbd.flags & Common::KBD_NUM);

            //input.data = ev.kbd.keycode;
            //input.character = ev.kbd.ascii;
            //input.type = SCI_EVENT_KEYBOARD;

            //input.modifiers =
            //    ((modifiers & Common::KBD_ALT) ? SCI_KEYMOD_ALT : 0) |
            //    ((modifiers & Common::KBD_CTRL) ? SCI_KEYMOD_CTRL : 0) |
            //    ((modifiers & Common::KBD_SHIFT) ? SCI_KEYMOD_LSHIFT | SCI_KEYMOD_RSHIFT : 0);

            //// Caps lock and Scroll lock have been removed, cause we already handle upper
            //// case keys ad Scroll lock doesn't seem to be used anywhere
            ////((ev.kbd.flags & Common::KBD_CAPS) ? SCI_KEYMOD_CAPSLOCK : 0) |
            ////((ev.kbd.flags & Common::KBD_SCRL) ? SCI_KEYMOD_SCRLOCK : 0) |

            //if (!(input.data & 0xFF00))
            //{
            //    // Directly accept most common keys without conversion
            //    if ((input.character >= 0x80) && (input.character <= 0xFF))
            //    {
            //        // If there is no extended font, we will just clear the
            //        // current event.
            //        // Sierra SCI actually accepted those characters, but
            //        // didn't display them inside text edit controls because
            //        // the characters were missing inside the font(s).
            //        // We filter them out for non-multilingual games because
            //        // of that.
            //        if (!_fontIsExtended)
            //            return noEvent;
            //        // Convert 8859-1 characters to DOS (cp850/437) for
            //        // multilingual SCI01 games
            //        input.character = codepagemap_88591toDOS[input.character & 0x7f];
            //    }
            //    if (input.data == Common::KEYCODE_TAB)
            //    {
            //        input.character = input.data = SCI_KEY_TAB;
            //        if (modifiers & Common::KBD_SHIFT)
            //            input.character = SCI_KEY_SHIFT_TAB;
            //    }
            //    if (input.data == Common::KEYCODE_DELETE)
            //        input.data = input.character = SCI_KEY_DELETE;
            //}
            //else if ((input.data >= Common::KEYCODE_F1) && input.data <= Common::KEYCODE_F10)
            //{
            //    // SCI_K_F1 == 59 << 8
            //    // SCI_K_SHIFT_F1 == 84 << 8
            //    input.character = input.data = SCI_KEY_F1 + ((input.data - Common::KEYCODE_F1) << 8);
            //    if (modifiers & Common::KBD_SHIFT)
            //        input.character = input.data + 0x1900;
            //}
            //else {
            //    // Special keys that need conversion
            //    for (int i = 0; i < ARRAYSIZE(keyMappings); i++)
            //    {
            //        if (keyMappings[i].scummVMKey == ev.kbd.keycode)
            //        {
            //            input.character = input.data = numlockOn ? keyMappings[i].sciKeyNumlockOn : keyMappings[i].sciKeyNumlockOff;
            //            break;
            //        }
            //    }
            //}

            //// When Ctrl AND Alt are pressed together with a regular key, Linux will give us control-key, Windows will give
            ////  us the actual key. My opinion is that windows is right, because under DOS the keys worked the same, anyway
            ////  we support the other case as well
            //if ((modifiers & Common::KBD_ALT) && input.character > 0 && input.character < 27)
            //    input.character += 96; // 0x01 -> 'a'

            //// Scancodify if appropriate
            //if (modifiers & Common::KBD_ALT)
            //    input.character = altify(input.character);
            //if (getSciVersion() <= SCI_VERSION_1_MIDDLE && (modifiers & Common::KBD_CTRL) && input.character > 0 && input.character < 27)
            //    input.character += 96; // 0x01 -> 'a'

            // If no actual key was pressed (e.g. if only a modifier key was pressed),
            // ignore the event
            if (input.character == 0)
                return noEvent;

            return input;
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
