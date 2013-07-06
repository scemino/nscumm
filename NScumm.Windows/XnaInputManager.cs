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

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NScumm.Windows
{
    internal sealed class XnaInputManager : NScumm.Core.Input.IInputManager
    {
        private Microsoft.Xna.Framework.GameWindow _window;
        private KeyboardState m_keyboardState;
        private MouseState m_mouseState;

        public XnaInputManager(Microsoft.Xna.Framework.GameWindow window)
        {
            _window = window;
        }

        public NScumm.Core.Graphics.Point GetMousePosition()
        {
            NScumm.Core.Graphics.Point pOut = new NScumm.Core.Graphics.Point();
            var state = Microsoft.Xna.Framework.Input.Mouse.GetState();
                int x = state.X;
                int y = state.Y;

                double scaleX = 320.0 / _window.ClientBounds.Width;
                double scaleY = 200.0 / _window.ClientBounds.Height;
                pOut = new NScumm.Core.Graphics.Point((short)(x * scaleX), (short)(y * scaleY));
            return pOut;
        }

        public void UpdateStates()
        {
            m_keyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
            m_mouseState = Microsoft.Xna.Framework.Input.Mouse.GetState();
        }

        public bool IsKeyDown(NScumm.Core.KeyCode code)
        {
            if (code >= NScumm.Core.KeyCode.A && code <= NScumm.Core.KeyCode.Z)
            {
                return /*state.IsButtonDown(code - Scumm4.KeyCode.A + Microsoft.Xna.Framework.Input.Buttons.A) ||*/
                    m_keyboardState.IsKeyDown(code - NScumm.Core.KeyCode.A + Microsoft.Xna.Framework.Input.Keys.A);
            }
            else if (code == NScumm.Core.KeyCode.Escape)
            {
                return /*state.IsButtonDown(Microsoft.Xna.Framework.Input.Buttons.Back) ||*/
                    m_keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);
            }
            else if (code >= NScumm.Core.KeyCode.F1 && code <= NScumm.Core.KeyCode.F9)
            {
                return m_keyboardState.IsKeyDown(code - NScumm.Core.KeyCode.F1 + Microsoft.Xna.Framework.Input.Keys.F1);
            }
            else if (code >= NScumm.Core.KeyCode.D0 && code <= NScumm.Core.KeyCode.D9)
            {
                return m_keyboardState.IsKeyDown(code - NScumm.Core.KeyCode.D0 + Microsoft.Xna.Framework.Input.Keys.NumPad0);
            }
            return false;

        }

        public bool IsMouseLeftPressed()
        {
            return m_mouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
        }

        public bool IsMouseRightPressed()
        {
            return m_mouseState.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
        }

    }
}
