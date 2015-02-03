/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using Microsoft.Xna.Framework.Input;
using NScumm.Core.Input;
using System.Collections.Generic;
using NScumm.Core;
using System.Linq;

namespace NScumm.MonoGame
{
    sealed class XnaInputManager : NScumm.Core.Input.IInputManager
    {
        public XnaInputManager(OpenTK.NativeWindow window)
        {
            this.window = window;
        }

        public NScumm.Core.Graphics.Point GetMousePosition()
        {
            var state = Mouse.GetState();
            var x = state.X - window.Bounds.X;
            var y = state.Y - window.Bounds.Y;

            var scaleX = 320.0 / window.Bounds.Width;
            var scaleY = 200.0 / window.Bounds.Height;
            var pOut = new NScumm.Core.Graphics.Point((short)(x * scaleX), (short)(y * scaleY));
            return pOut;
        }

        public ScummInputState GetState()
        {
            lock (gate)
            {
                var mouseState = Mouse.GetState();
                var keys = Keyboard.GetState().GetPressedKeys().Where(key => keyToKeyCode.ContainsKey(key)).Select(key => keyToKeyCode[key]).ToList();
                var inputState = new ScummInputState(keys, mouseState.LeftButton == ButtonState.Pressed, mouseState.RightButton == ButtonState.Pressed);
                return inputState;
            }
        }

        OpenTK.NativeWindow window;
        object gate = new object();
        static readonly Dictionary<Keys, KeyCode> keyToKeyCode = new Dictionary<Keys,KeyCode>
        { 
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
        };
    }
}