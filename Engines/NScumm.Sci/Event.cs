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
using System.Linq;
using NScumm.Core.Input;
using NScumm.Core;
using NScumm.Sci.Graphics;

namespace NScumm.Sci
{
    internal class SciEvent
    {
        /*Values for type*/
        public const int SCI_EVENT_NONE = 0;
        public const int SCI_EVENT_MOUSE_PRESS = 1 << 0;
        public const int SCI_EVENT_MOUSE_RELEASE = 1 << 1;
        public const int SCI_EVENT_KEYBOARD = 1 << 2;
        public const int SCI_EVENT_DIRECTION = (1 << 6);
        public const int SCI_EVENT_SAID = (1 << 7);
#if ENABLE_SCI32
        public const int SCI_EVENT_HOT_RECTANGLE = (1 << 8);
#endif

        /*Fake values for other events*/
        public const int SCI_EVENT_QUIT = (1 << 11);
        public const int SCI_EVENT_PEEK = (1 << 15);
        public const int SCI_EVENT_ANY = 0x7fff;

        /* Keycodes of special keys: */
#if ENABLE_SCI32
        public const int SCI_KEY_ETX = 3;
#endif

        /* Keycodes of special keys: */
        public const int SCI_KEY_ESC = 27;
        public const int SCI_KEY_BACKSPACE = 8;
        public const int SCI_KEY_ENTER = 13;
        public const int SCI_KEY_TAB = '\t';
        public const int SCI_KEY_SHIFT_TAB = (0xf << 8);

        public const int SCI_KEY_HOME = (71 << 8); // 7
        public const int SCI_KEY_UP = (72 << 8); // 8
        public const int SCI_KEY_PGUP = (73 << 8); // 9
        //
        public const int SCI_KEY_LEFT = (75 << 8); // 4
        public const int SCI_KEY_CENTER = (76 << 8); // 5
        public const int SCI_KEY_RIGHT = (77 << 8); // 6
        //
        public const int SCI_KEY_END = (79 << 8); // 1
        public const int SCI_KEY_DOWN = (80 << 8); // 2
        public const int SCI_KEY_PGDOWN = (81 << 8); // 3
        //
        public const int SCI_KEY_INSERT = (82 << 8); // 0
        public const int SCI_KEY_DELETE = (83 << 8); // .

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

        public const int SCI_KEYMOD_NO_FOOLOCK =
            (~(SCI_KEYMOD_SCRLOCK | SCI_KEYMOD_NUMLOCK | SCI_KEYMOD_CAPSLOCK | SCI_KEYMOD_INSERT));

        public const int SCI_KEYMOD_ALL = 0xFF;

        public ushort type;
        public ushort modifiers;
        /// <summary>
        /// For keyboard events: the actual character of the key that was pressed
        /// For 'Alt', characters are interpreted by their
        /// PC keyboard scancodes.
        /// </summary>
        public ushort character;

        /// <summary>
        /// The mouse position at the time the event was created,
        /// in display coordinates.
        /// </summary>
        public Point mousePos;

#if ENABLE_SCI32
        /// <summary>
        /// The mouse position at the time the event was created,
        /// in script coordinates.
        /// </summary>
        public Point mousePosSci;

        public short hotRectangleIndex;

        public SciEvent(ushort type, ushort modifiers, ushort character, Point mousePos, Point mousePosSci, short hotRectangleIndex)
        {
            this.type = type;
            this.modifiers = modifiers;
            this.character = character;
            this.mousePos = mousePos;
            this.mousePosSci = mousePosSci;
            this.hotRectangleIndex = hotRectangleIndex;
        }
#endif

        public SciEvent(ushort type, ushort modifiers, ushort character, Point mousePos)
        {
            this.type = type;
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
#if ENABLE_SCI32
            SciEvent @event = new SciEvent(SciEvent.SCI_EVENT_NONE, 0, 0, new Point(), new Point(), -1);
#else

            SciEvent @event = new SciEvent(SciEvent.SCI_EVENT_NONE, 0, 0, new Point());
#endif
            UpdateScreen();

            // Get all queued events from graphics driver
            do
            {
                @event = GetScummVmEvent();
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
            else
            {
                // No event found: we must return a SCI_EVT_NONE event.

                // Because event.type is SCI_EVT_NONE already here,
                // there is no need to change it.
            }

            return @event;
        }

        private SciEvent GetScummVmEvent()
        {
#if ENABLE_SCI32
            SciEvent input = new SciEvent(SciEvent.SCI_EVENT_NONE, 0, 0, new Point(), new Point(), -1);
            SciEvent noEvent = new SciEvent(SciEvent.SCI_EVENT_NONE, 0, 0, new Point(), new Point(), -1);
#else
            SciEvent input = new SciEvent(SciEvent.SCI_EVENT_NONE, 0, 0, 0, new Point(0, 0));
            SciEvent noEvent = new SciEvent(SciEvent.SCI_EVENT_NONE, 0, 0, 0, new Point(0, 0));
#endif

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

#if ENABLE_SCI32
            if (ResourceManager.GetSciVersion() >= SciVersion.V2)
            {
                var screen = SciEngine.Instance._gfxFrameout.CurrentBuffer;

                // This will clamp `mousePos` according to the restricted zone,
                // so any cursor or screen item associated with the mouse position
                // does not bounce when it hits the edge (or ignore the edge)
                SciEngine.Instance._gfxCursor32.DeviceMoved(mousePos);

                Point mousePosSci = mousePos;
                var rx = new Rational(screen.ScriptWidth, screen.ScreenWidth);
                var ry = new Rational(screen.ScriptHeight, screen.ScreenHeight);
                Helpers.Mulru(ref mousePosSci, ref rx, ref ry);
                noEvent.mousePosSci = input.mousePosSci = mousePosSci;

                if (_hotRectanglesActive)
                {
                    CheckHotRectangles(mousePosSci);
                }
            }
            else
            {
#endif
                SciEngine.Instance._gfxScreen.AdjustBackUpscaledCoordinates(ref mousePos.Y, ref mousePos.X);
#if ENABLE_SCI32
            }
#endif
            noEvent.mousePos = input.mousePos = mousePos;

            // TODO:
            var state = im.GetState();

            if (_oldState.IsLeftButtonDown)
            {
                if (!state.IsLeftButtonDown)
                {
                    input.type = SciEvent.SCI_EVENT_MOUSE_RELEASE;
                    _oldState = state;
                    return input;
                }
            }
            else
            {
                if (state.IsLeftButtonDown)
                {
                    input.type = SciEvent.SCI_EVENT_MOUSE_PRESS;
                    _oldState = state;
                    return input;
                }
            }
            if (_oldState.IsRightButtonDown)
            {
                if (!state.IsRightButtonDown)
                {
                    input.type = SciEvent.SCI_EVENT_MOUSE_RELEASE;
                    input.modifiers |= (SciEvent.SCI_KEYMOD_RSHIFT | SciEvent.SCI_KEYMOD_LSHIFT); // this value was hardcoded in the mouse interrupt handler
                    _oldState = state;
                    return input;
                }
            }
            else
            {
                if (state.IsRightButtonDown)
                {
                    input.type = SciEvent.SCI_EVENT_MOUSE_PRESS;
                    input.modifiers |= (SciEvent.SCI_KEYMOD_RSHIFT | SciEvent.SCI_KEYMOD_LSHIFT); // this value was hardcoded in the mouse interrupt handler
                    _oldState = state;
                    return input;
                }
            }
            _oldState = state;



            var keys = state.GetKeys().ToList();
            if (keys.Count != 0)
            {
                if (keys[0] >= KeyCode.D0 && keys[0] <= KeyCode.D9)
                {
                    input.character = (ushort)(keys[0] - KeyCode.D0 + '0');
                }
                else
                {
                    input.character = (ushort)keys[0];
                }
            }
            im.ResetKeys();
            input.type = SciEvent.SCI_EVENT_KEYBOARD;



            // If no actual key was pressed (e.g. if only a modifier key was pressed),
            // ignore the event
            if (input.character == 0)
                return noEvent;

            return input;
        }

        private void CheckHotRectangles(Point mousePosition)
        {
            int lastActiveRectIndex = _activeRectIndex;
            _activeRectIndex = -1;

            for (short i = 0; i < (short)_hotRects.Length; ++i)
            {
                if (_hotRects[i].Contains(mousePosition))
                {
                    _activeRectIndex = i;
                    if (i != lastActiveRectIndex)
                    {
                        var hotRectEvent = new SciEvent(SciEvent.SCI_EVENT_HOT_RECTANGLE, 0, 0, new Point(), new Point(), i);
                        _events.Insert(0, hotRectEvent);
                        break;
                    }

                    lastActiveRectIndex = _activeRectIndex;
                }
            }

            if (lastActiveRectIndex != _activeRectIndex && lastActiveRectIndex != -1)
            {
                _activeRectIndex = -1;
                var hotRectEvent = new SciEvent(SciEvent.SCI_EVENT_HOT_RECTANGLE, 0, 0, new Point(), new Point(), -1);
                _events.Insert(0, hotRectEvent);
            }
        }

        public void UpdateScreen()
        {
            // Update the screen here, since it's called very often.
            // Throttle the screen update rate to 60fps.
            var s = SciEngine.Instance.EngineState;
            if (ServiceLocator.Platform.GetMilliseconds() - s._screenUpdateTime >= 1000 / 60)
            {
                SciEngine.Instance.System.GraphicsManager.UpdateScreen();
                s._screenUpdateTime = ServiceLocator.Platform.GetMilliseconds();
                // Throttle the checking of shouldQuit() to 60fps as well, since
                // Engine::shouldQuit() invokes 2 virtual functions
                // (EventManager::shouldQuit() and EventManager::shouldRTL()),
                // which is very expensive to invoke constantly without any
                // throttling at all.
                if (SciEngine.Instance.ShouldQuit)
                    s.abortScriptProcessing = Engine.AbortGameState.QuitGame;
            }
        }

        private bool _hotRectanglesActive;
        private Rect[] _hotRects;
        private short _activeRectIndex;

        public void SetHotRectanglesActive(bool active)
        {
            _hotRectanglesActive = active;
        }

        public void SetHotRectangles(Rect[] rects)
        {
            _hotRects = rects;
            _activeRectIndex = -1;
        }
    }
}