//
//  InputManager.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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

using NScumm.Core;
using NScumm.Core.Input;
using NScumm.Core.Graphics;
using System.Linq;
using System.Collections.Generic;
using Android.Views;
using NScumm.Core.IO;
using Android.Content;
using Android.Views.InputMethods;

namespace NScumm.Droid.Services
{
    internal class DroidInputManager : IInputManager
    {
        private HashSet<KeyCode> _keys;
        private float _x, _y;
        private bool _isMouseLeftDown;
        private bool _isMouseRightDown;
        private Context _context;
        private int _fingersDown;

        public int width, height;

        public View View { get; internal set; }
        public IGameDescriptor Game { get; internal set; }

        public DroidInputManager(Context context)
        {
            _context = context;
            _keys = new HashSet<KeyCode>();
        }

        public Point GetMousePosition()
        {
            return new Point((short)_x, (short)_y);
        }

        public ScummInputState GetState()
        {
            return new ScummInputState(_keys.ToList(), _isMouseLeftDown, _isMouseRightDown);
        }

        public void ResetKeys()
        {
            _keys.Clear();
        }

        public void ShowVirtualKeyboard()
        {
            MainActivity.Instance.RunOnUiThread(() =>
            {
                var imm = (InputMethodManager)_context.GetSystemService(Context.InputMethodService);
                imm.ShowSoftInput(View, ShowFlags.Implicit);
            });
        }

        public void HideVirtualKeyboard()
        {
            MainActivity.Instance.RunOnUiThread(() =>
            {
                var imm = (InputMethodManager)_context.GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(View.WindowToken, HideSoftInputFlags.ImplicitOnly);
            });
        }

        public bool OnTouchEvent(MotionEvent e)
        {
            if (!IsMouseEvent(e))
            {
                var action = (int)e.Action;
                var pointer = (action & (int)MotionEventActions.PointerIndexMask) >> (int)MotionEventActions.PointerIndexShift;
                var pointerAction = action & (int)MotionEventActions.Mask;
                if (pointer > 0)
                {
                    switch (pointerAction)
                    {
                        case (int)MotionEventActions.PointerDown:
                            if (pointer > _fingersDown)
                                _fingersDown = pointer;
                            return true;
                        case (int)MotionEventActions.PointerUp:
                            if (pointer != _fingersDown)
                                return true;
                            if (_fingersDown == 1)
                            {
                                _isMouseRightDown = true;
                            }
                            return true;
                    }
                }
                return false;
            }

            var buttonState = e.ButtonState;
            if (e.Action != MotionEventActions.Up && buttonState == 0)
            {
                // On some device types, ButtonState is 0 even when tapping on the touchpad or using the stylus on the screen etc.
                _isMouseLeftDown = true;
            }
            else
            {
                _isMouseLeftDown = buttonState.HasFlag(MotionEventButtonState.Primary);
            }

            _isMouseRightDown = buttonState.HasFlag(MotionEventButtonState.Secondary);
            UpdatePosition(e);

            return true;
        }

        public bool OnDown(MotionEvent e)
        {
            UpdatePosition(e);
            return true;
        }

        private void UpdatePosition(MotionEvent e)
        {
            var ratioW = ((float)Game.Width) / width;
            var ratioH = ((float)Game.Height) / height;
            _x = e.GetX() * ratioW;
            _y = e.GetY() * ratioH;
        }

        public bool OnSingleTapUp(MotionEvent e)
        {
            if (_fingersDown > 0)
            {
                _fingersDown = 0;
                return true;
            }

            var time = e.EventTime - e.DownTime;
            UpdatePosition(e);

            // TODO put these values in some option dlg?
            if (time > 500)
            {
                _isMouseRightDown = true;
            }
            else if (!_isMouseLeftDown)
            {
                _isMouseLeftDown = true;
                _isMouseRightDown = false;
            }
            else
            {
                _isMouseLeftDown = false;
            }
            return true;
        }

        public bool OnKeyDown(Keycode keyCode)
        {
            if (keyCode == Keycode.Menu)
            {
                _keys.Add(KeyToKeyCode[keyCode]);
                return true;
            }

            if (!KeyToKeyCode.ContainsKey(keyCode))
                return false;
            _keys.Add(KeyToKeyCode[keyCode]);
            return true;
        }

        private static bool IsMouseEvent(MotionEvent e)
        {
            if (e == null) return false;

            InputDevice device = e.Device;
            if (device == null) return false;

            var sources = device.Sources;
            return (sources.HasFlag(InputSourceType.Mouse) ||
                    sources.HasFlag(InputSourceType.Stylus) ||
                    sources.HasFlag(InputSourceType.Touchpad));
        }

        private static readonly Dictionary<Keycode, KeyCode> KeyToKeyCode = new Dictionary<Keycode, KeyCode>
        {
            { Keycode.CtrlLeft,   KeyCode.LeftControl },
            { Keycode.Menu,   KeyCode.F5 },
            { Keycode.Period,   KeyCode.OemPeriod },
            { Keycode.Del,   KeyCode.Backspace },
            { Keycode.Tab,    KeyCode.Tab  },
            { Keycode.Enter,  KeyCode.Return },
            { Keycode.Back, KeyCode.Escape },
            { Keycode.Escape, KeyCode.Escape },
            { Keycode.Space,  KeyCode.Space },
            { Keycode.F1 , KeyCode.F1 },
            { Keycode.F2 , KeyCode.F2 },
            { Keycode.F3 , KeyCode.F3 },
            { Keycode.F4 , KeyCode.F4 },
            { Keycode.F5 , KeyCode.F5 },
            { Keycode.F6 , KeyCode.F6 },
            { Keycode.F7 , KeyCode.F7 },
            { Keycode.F8 , KeyCode.F8 },
            { Keycode.F9 , KeyCode.F9 },
            { Keycode.F10, KeyCode.F10 },
            { Keycode.F11, KeyCode.F11 },
            { Keycode.F12, KeyCode.F12 },
            { Keycode.A, KeyCode.A },
            { Keycode.B, KeyCode.B },
            { Keycode.C, KeyCode.C },
            { Keycode.D, KeyCode.D },
            { Keycode.E, KeyCode.E },
            { Keycode.F, KeyCode.F },
            { Keycode.G, KeyCode.G },
            { Keycode.H, KeyCode.H },
            { Keycode.I, KeyCode.I },
            { Keycode.J, KeyCode.J },
            { Keycode.K, KeyCode.K },
            { Keycode.L, KeyCode.L },
            { Keycode.M, KeyCode.M },
            { Keycode.N, KeyCode.N },
            { Keycode.O, KeyCode.O },
            { Keycode.P, KeyCode.P },
            { Keycode.Q, KeyCode.Q },
            { Keycode.R, KeyCode.R },
            { Keycode.S, KeyCode.S },
            { Keycode.T, KeyCode.T },
            { Keycode.U, KeyCode.U },
            { Keycode.V, KeyCode.V },
            { Keycode.W, KeyCode.W },
            { Keycode.X, KeyCode.X },
            { Keycode.Y, KeyCode.Y },
            { Keycode.Z, KeyCode.Z },
            { Keycode.Num0, KeyCode.D0 },
            { Keycode.Num1, KeyCode.D1 },
            { Keycode.Num2, KeyCode.D2 },
            { Keycode.Num3, KeyCode.D3 },
            { Keycode.Num4, KeyCode.D4 },
            { Keycode.Num5, KeyCode.D5 },
            { Keycode.Num6, KeyCode.D6 },
            { Keycode.Num7, KeyCode.D7 },
            { Keycode.Num8, KeyCode.D8 },
            { Keycode.Num9, KeyCode.D9 },
            { Keycode.Numpad0, KeyCode.NumPad0 },
            { Keycode.Numpad1, KeyCode.NumPad1 },
            { Keycode.Numpad2, KeyCode.NumPad2 },
            { Keycode.Numpad3, KeyCode.NumPad3 },
            { Keycode.Numpad4, KeyCode.NumPad4 },
            { Keycode.Numpad5, KeyCode.NumPad5 },
            { Keycode.Numpad6, KeyCode.NumPad6 },
            { Keycode.Numpad7, KeyCode.NumPad7 },
            { Keycode.Numpad8, KeyCode.NumPad8 },
            { Keycode.Numpad9, KeyCode.NumPad9 },
            { Keycode.DpadUp, KeyCode.Up },
            { Keycode.DpadDown, KeyCode.Down },
            { Keycode.DpadRight, KeyCode.Right },
            { Keycode.DpadLeft, KeyCode.Left },
            { Keycode.Insert, KeyCode.Insert },
            { Keycode.Home, KeyCode.Home },
            { Keycode.MoveEnd, KeyCode.End },
            { Keycode.PageUp, KeyCode.PageUp },
            { Keycode.PageDown, KeyCode.PageDown },
            { Keycode.ShiftLeft, KeyCode.LeftShift }
        };
    }
}
