//
//  XnaInputManager.cs
//
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


using Microsoft.Xna.Framework.Input;
using NScumm.Core.Input;
using System.Collections.Generic;
using NScumm.Core;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
using NScumm.Core.Graphics;
#if WINDOWS_UWP
using Windows.UI.ViewManagement;
using Windows.UI.Core;
#endif

namespace NScumm
{
    internal sealed class XnaInputManager : IInputManager
    {
        private readonly object _gate = new object();
        private readonly List<Keys> _virtualKeysDown = new List<Keys>();
        private readonly List<Keys> _virtualKeysUp = new List<Keys>();
        private HashSet<KeyCode> _keysPressed = new HashSet<KeyCode>();
        private KeyboardState _keyboardState;
        private bool _backPressed;
        private bool _leftButtonPressed;
        private bool _isMenuPressed;
        private Core.Graphics.Point _mousePosition;
        private bool _rightButtonPressed;
        private Game _game;
        private int _width;
        private int _height;

#if WINDOWS_UWP
		private bool _showKeyboard;
        private InputPane _inputPane;
        private CoreWindow _currentWindow;
#endif
        public Vector2 RealPosition { get; private set; }

        public XnaInputManager(Game game, Core.IO.IGameDescriptor gameDesc)
        {
            _game = game;
            _width = gameDesc.Width;
            _height = gameDesc.Height;
            _mousePosition = new Core.Graphics.Point();

            TouchPanel.EnableMouseGestures = true;
            TouchPanel.EnabledGestures = GestureType.Hold | GestureType.Tap;
            TouchPanel.EnableMouseTouchPoint = true;

#if WINDOWS_UWP
            var view = SystemNavigationManager.GetForCurrentView();
            view.BackRequested += HardwareButtons_BackPressed;

            bool isHardwareButtonsApiPresent = Windows.Foundation.Metadata.ApiInformation.IsTypePresent
                (typeof(Windows.Phone.UI.Input.HardwareButtons).FullName);

            if (isHardwareButtonsApiPresent)
            {
                Windows.Phone.UI.Input.HardwareButtons.CameraPressed += HardwareButtons_CameraPressed;
            }

            _inputPane = InputPane.GetForCurrentView();
            _currentWindow = CoreWindow.GetForCurrentThread();
            _currentWindow.KeyDown += XnaInputManager_KeyDown;
            _currentWindow.KeyUp += XnaInputManager_KeyUp;
#endif
        }

#if WINDOWS_UWP
        private void XnaInputManager_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            if (_currentWindow.IsInputEnabled)
            {
                lock (_gate)
                {
                    _virtualKeysUp.Add((Keys)args.VirtualKey);
                }
            }
        }

        private void XnaInputManager_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (_currentWindow.IsInputEnabled)
            {
                lock (_gate)
                {
                    _virtualKeysDown.Add((Keys)args.VirtualKey);
                }
            }
        }

        private void HardwareButtons_BackPressed(object sender, BackRequestedEventArgs e)
        {
            _backPressed = true;
            _showKeyboard = false;
            e.Handled = true;
        }

        private void HardwareButtons_CameraPressed(object sender, Windows.Phone.UI.Input.CameraEventArgs e)
        {
            _isMenuPressed = true;
        }
#endif
        public Core.Graphics.Point GetMousePosition()
        {
            return _mousePosition;
        }

        private void UpdateMousePosition(Vector2 pos)
        {
            var rect = _game.Services.GetService<IGraphicsManager>().Bounds;
            if (rect.Width == 0 || rect.Height == 0) return;

            var scaleX = (float)_width / rect.Width;
            var scaleY = (float)_height / rect.Height;
            RealPosition = pos;
            _mousePosition = new Core.Graphics.Point((int)((pos.X - rect.Left) * scaleX), (int)((pos.Y - rect.Top) * scaleY));
        }

        public void UpdateInput(KeyboardState keyboard)
        {
            lock (_gate)
            {
#if WINDOWS_UWP
                if (_showKeyboard)
                {
                    _inputPane.TryShow();
                }
                else
                {
                    _inputPane.TryHide();
                }
#endif

                _leftButtonPressed = false;
                _rightButtonPressed = false;

                if (TouchPanel.IsGestureAvailable)
                {
                    var gesture = TouchPanel.ReadGesture();
                    if (gesture.GestureType == GestureType.Tap)
                    {
                        _leftButtonPressed = true;
                    }
                    else if (gesture.GestureType == GestureType.Hold)
                    {
                        _rightButtonPressed = true;
                    }
                }

                var locations = TouchPanel.GetState();
                foreach (var touch in locations)
                {
                    if (touch.State == TouchLocationState.Moved || touch.State == TouchLocationState.Pressed)
                    {
                        var pos = touch.Position;
                        UpdateMousePosition(pos);
                        Mouse.SetPosition((int)pos.X, (int)pos.Y);
                        _leftButtonPressed = true;
                    }
                }

                var state = Mouse.GetState();
                _leftButtonPressed |= state.LeftButton == ButtonState.Pressed;
                _rightButtonPressed |= state.RightButton == ButtonState.Pressed;
                UpdateMousePosition(state.Position.ToVector2());

                _keyboardState = keyboard;

                _keysPressed = new HashSet<KeyCode>(_keyboardState.GetPressedKeys().Where(KeyToKeyCode.ContainsKey).Select(key => KeyToKeyCode[key]));
                if (_virtualKeysDown.Count > 0)
                {
                    _virtualKeysDown.ForEach(k =>
                    {
                        if (KeyToKeyCode.ContainsKey(k))
                        {
                            _keysPressed.Add(KeyToKeyCode[k]);
                        }
                    });

                    _virtualKeysUp.ForEach(k =>
                    {
                        _virtualKeysDown.Remove(k);
                    });
                    _virtualKeysUp.Clear();
                }
                if (_backPressed)
                {
                    _keysPressed.Add(KeyCode.Escape);
                    _backPressed = false;
                }
                if (_isMenuPressed)
                {
                    _keysPressed.Add(KeyCode.F5);
                    _isMenuPressed = false;
                }
            }
        }

        public ScummInputState GetState()
        {
            lock (_gate)
            {
                var inputState = new ScummInputState(_keysPressed.ToList(), _leftButtonPressed, _rightButtonPressed);
                return inputState;
            }
        }

        public void ResetKeys()
        {
            lock (_gate)
            {
                _keyboardState = new KeyboardState();
                _keysPressed.Clear();
                _virtualKeysDown.Clear();
                _virtualKeysUp.Clear();
            }
        }

        public void ShowVirtualKeyboard()
        {
			#if WINDOWS_UWP
			_showKeyboard = true;
			#endif
        }

        public void HideVirtualKeyboard()
        {
			#if WINDOWS_UWP
			_showKeyboard = false;
			#endif
        }

        private static readonly Dictionary<Keys, KeyCode> KeyToKeyCode = new Dictionary<Keys, KeyCode>
        {
            { Keys.LeftControl,   KeyCode.LeftControl },
            { Keys.OemPeriod,   KeyCode.OemPeriod },
            { Keys.Back,   KeyCode.Backspace },
            { Keys.Tab,    KeyCode.Tab       },
            { Keys.Enter,  KeyCode.Return    },
            { Keys.Escape, KeyCode.Escape    },
            { Keys.Space,  KeyCode.Space     },
            { Keys.F1 , KeyCode.F1 },
            { Keys.F2 , KeyCode.F2 },
            { Keys.F3 , KeyCode.F3 },
            { Keys.F4 , KeyCode.F4 },
            { Keys.F5 , KeyCode.F5 },
            { Keys.F6 , KeyCode.F6 },
            { Keys.F7 , KeyCode.F7 },
            { Keys.F8 , KeyCode.F8 },
            { Keys.F9 , KeyCode.F9 },
            { Keys.F10, KeyCode.F10 },
            { Keys.F11, KeyCode.F11 },
            { Keys.F12, KeyCode.F12 },
            { Keys.A, KeyCode.A },
            { Keys.B, KeyCode.B },
            { Keys.C, KeyCode.C },
            { Keys.D, KeyCode.D },
            { Keys.E, KeyCode.E },
            { Keys.F, KeyCode.F },
            { Keys.G, KeyCode.G },
            { Keys.H, KeyCode.H },
            { Keys.I, KeyCode.I },
            { Keys.J, KeyCode.J },
            { Keys.K, KeyCode.K },
            { Keys.L, KeyCode.L },
            { Keys.M, KeyCode.M },
            { Keys.N, KeyCode.N },
            { Keys.O, KeyCode.O },
            { Keys.P, KeyCode.P },
            { Keys.Q, KeyCode.Q },
            { Keys.R, KeyCode.R },
            { Keys.S, KeyCode.S },
            { Keys.T, KeyCode.T },
            { Keys.U, KeyCode.U },
            { Keys.V, KeyCode.V },
            { Keys.W, KeyCode.W },
            { Keys.X, KeyCode.X },
            { Keys.Y, KeyCode.Y },
            { Keys.Z, KeyCode.Z },
            { Keys.D0, KeyCode.D0 },
            { Keys.D1, KeyCode.D1 },
            { Keys.D2, KeyCode.D2 },
            { Keys.D3, KeyCode.D3 },
            { Keys.D4, KeyCode.D4 },
            { Keys.D5, KeyCode.D5 },
            { Keys.D6, KeyCode.D6 },
            { Keys.D7, KeyCode.D7 },
            { Keys.D8, KeyCode.D8 },
            { Keys.D9, KeyCode.D9 },
            { Keys.NumPad0, KeyCode.NumPad0 },
            { Keys.NumPad1, KeyCode.NumPad1 },
            { Keys.NumPad2, KeyCode.NumPad2 },
            { Keys.NumPad3, KeyCode.NumPad3 },
            { Keys.NumPad4, KeyCode.NumPad4 },
            { Keys.NumPad5, KeyCode.NumPad5 },
            { Keys.NumPad6, KeyCode.NumPad6 },
            { Keys.NumPad7, KeyCode.NumPad7 },
            { Keys.NumPad8, KeyCode.NumPad8 },
            { Keys.NumPad9, KeyCode.NumPad9 },
            { Keys.Up, KeyCode.Up },
            { Keys.Down, KeyCode.Down },
            { Keys.Right, KeyCode.Right },
            { Keys.Left, KeyCode.Left },
            { Keys.Insert, KeyCode.Insert },
            { Keys.Home, KeyCode.Home },
            { Keys.End, KeyCode.End },
            { Keys.PageUp, KeyCode.PageUp },
            { Keys.PageDown, KeyCode.PageDown },
            { Keys.LeftShift, KeyCode.LeftShift },
        };

    }
}