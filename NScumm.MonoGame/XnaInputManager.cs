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

namespace NScumm.MonoGame
{
    sealed class XnaInputManager : NScumm.Core.Input.IInputManager
    {
        Microsoft.Xna.Framework.GameWindow window;

        KeyboardState keyboardState;
        MouseState mouseState;

        public XnaInputManager(Microsoft.Xna.Framework.GameWindow window)
        {
            this.window = window;
        }

        public NScumm.Core.Graphics.Point GetMousePosition()
        {
            var state = Mouse.GetState();
            var x = state.X;
            var y = state.Y;

            var scaleX = 320.0 / window.ClientBounds.Width;
            var scaleY = 200.0 / window.ClientBounds.Height;
            var pOut = new NScumm.Core.Graphics.Point((short)(x * scaleX), (short)(y * scaleY));
            return pOut;
        }

        public void UpdateStates()
        {
            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();
        }

        public bool IsKeyDown(NScumm.Core.KeyCode code)
        {
            if (code >= NScumm.Core.KeyCode.A && code <= NScumm.Core.KeyCode.Z)
            {
                return //state.IsButtonDown(code - Scumm4.KeyCode.A + Microsoft.Xna.Framework.Input.Buttons.A) ||
					keyboardState.IsKeyDown(code - NScumm.Core.KeyCode.A + Keys.A);
            }
            if (code == NScumm.Core.KeyCode.Escape)
            {
                return //state.IsButtonDown(Microsoft.Xna.Framework.Input.Buttons.Back) ||
					keyboardState.IsKeyDown(Keys.Escape);
            }
            if (code >= NScumm.Core.KeyCode.F1 && code <= NScumm.Core.KeyCode.F9)
            {
                return keyboardState.IsKeyDown(code - NScumm.Core.KeyCode.F1 + Keys.F1);
            }
            if (code >= NScumm.Core.KeyCode.D0 && code <= NScumm.Core.KeyCode.D9)
            {
                return keyboardState.IsKeyDown(code - NScumm.Core.KeyCode.D0 + Keys.NumPad0);
            }
            return false;

        }

        public bool IsMouseLeftPressed()
        {
            return mouseState.LeftButton == ButtonState.Pressed;
        }

        public bool IsMouseRightPressed()
        {
            return mouseState.RightButton == ButtonState.Pressed;
        }

    }
}