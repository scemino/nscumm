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

using Scumm4.Graphics;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NScumm
{
    internal sealed class WpfInputManager : DispatcherObject, Scumm4.Input.IInputManager
    {
        private FrameworkElement _elt;

        public WpfInputManager(FrameworkElement elt)
        {
            _elt = elt;
        }

        public Scumm4.Graphics.Point GetMousePosition()
        {
            if (this.Dispatcher.HasShutdownStarted) return new Scumm4.Graphics.Point();
            return (Scumm4.Graphics.Point)this.Dispatcher.Invoke(new Func<Scumm4.Graphics.Point>(() =>
            {
                var pos = Mouse.GetPosition(_elt);
                var scaleX = _elt.ActualWidth / 320.0;
                var scaleY = _elt.ActualHeight / 200;
                return new Scumm4.Graphics.Point((short)(pos.X / scaleX), (short)(pos.Y / scaleY));
            }));
        }

        public bool IsKeyDown(Scumm4.KeyCode code)
        {
            return (bool)this.Dispatcher.Invoke(new Func<bool>(() =>
            {
                if (code >= Scumm4.KeyCode.A && code <= Scumm4.KeyCode.Z)
                {
                    return Keyboard.IsKeyDown(code - Scumm4.KeyCode.A + Key.A);
                }
                else if (code == Scumm4.KeyCode.Escape)
                {
                    return Keyboard.IsKeyDown(Key.Escape);
                }
                else if (code >= Scumm4.KeyCode.F1 && code <= Scumm4.KeyCode.F9)
                {
                    return Keyboard.IsKeyDown(code - Scumm4.KeyCode.F1 + Key.F1);
                }
                return false;
            }));
        }

        public bool IsMouseLeftPressed()
        {
            return (bool)this.Dispatcher.Invoke(new Func<bool>(() =>
            {
                return Mouse.LeftButton == MouseButtonState.Pressed;
            }));
        }

        public bool IsMouseRightPressed()
        {
            return (bool)this.Dispatcher.Invoke(new Func<bool>(() =>
            {
                return Mouse.RightButton == MouseButtonState.Pressed;
            }));
        }
    }
}
