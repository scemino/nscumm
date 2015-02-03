//
//  ScummEngine_Input.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using NScumm.Core.Input;
using NScumm.Core.IO;

namespace NScumm.Core
{
    partial class ScummEngine
    {
        protected internal IInputManager _inputManager;
        protected ScummInputState _inputState;
        protected KeyCode mouseAndKeyboardStat;
        MouseButtonStatus _leftBtnPressed, _rightBtnPressed;

        internal void ParseEvents()
        {
            var newInput = _inputManager.GetState();
            if (newInput != _inputState)
            {
                _inputState = newInput;
                if (_inputState.IsLeftButtonDown)
                {
                    _leftBtnPressed = MouseButtonStatus.Clicked | MouseButtonStatus.Down;
                }
                else
                {
                    _leftBtnPressed &= ~MouseButtonStatus.Down;
                }
                if (_inputState.IsRightButtonDown)
                {
                    _rightBtnPressed = MouseButtonStatus.Clicked | MouseButtonStatus.Down;
                }
                else
                {
                    _rightBtnPressed &= ~MouseButtonStatus.Down;
                }
            }
        }

        void CheckExecVerbs()
        {
            if (_userPut <= 0 || mouseAndKeyboardStat == 0)
                return;

            if ((ScummMouseButtonState)mouseAndKeyboardStat < ScummMouseButtonState.MaxKey)
            {
                // Check keypresses
                var vs = (from verb in Verbs.Skip(1)
                                      where verb.VerbId != 0 && verb.SaveId == 0 && verb.CurMode == 1
                                      where verb.Key == (byte)mouseAndKeyboardStat
                                      select verb).FirstOrDefault();
                if (vs != null)
                {
                    // Trigger verb as if the user clicked it
                    RunInputScript(ClickArea.Verb, (KeyCode)vs.VerbId, 1);
                    return;
                }

                // Generic keyboard input
                RunInputScript(ClickArea.Key, mouseAndKeyboardStat, 1);
            }
            else if ((((ScummMouseButtonState)mouseAndKeyboardStat) & ScummMouseButtonState.MouseMask) != 0)
            {
                var code = ((ScummMouseButtonState)mouseAndKeyboardStat).HasFlag(ScummMouseButtonState.LeftClick) ? (byte)1 : (byte)2;
                var zone = FindVirtScreen(_mousePos.Y);

                if (zone == null)
                    return;

                var over = FindVerbAtPos(_mousePos.X, _mousePos.Y);

                if (over != 0)
                {
                    // Verb was clicked
                    RunInputScript(ClickArea.Verb, (KeyCode)Verbs[over].VerbId, code);
                }
                else
                {
                    // Scene was clicked
                    var area = zone == MainVirtScreen ? ClickArea.Scene : ClickArea.Verb;
                    RunInputScript(area, 0, code);
                }
            }
        }

        internal protected virtual void ProcessInput()
        {
            //
            // Determine the mouse button state.
            //
            mouseAndKeyboardStat = 0;

            if (_leftBtnPressed.HasFlag(MouseButtonStatus.Clicked) && _rightBtnPressed.HasFlag(MouseButtonStatus.Clicked))
            {
                // Pressing both mouse buttons is treated as if you pressed
                // the cutscene exit key (ESC) in V4+ games. That mimicks
                // the behavior of the original engine where pressing both
                // mouse buttons also skips the current cutscene.
                mouseAndKeyboardStat = 0;
//                lastKeyHit = Common::KeyState(Common::KEYCODE_ESCAPE);
            }
            else if (_rightBtnPressed.HasFlag(MouseButtonStatus.Clicked) && (Game.Version <= 3 && Game.GameId != GameId.Loom))
            {
                // Pressing right mouse button is treated as if you pressed
                // the cutscene exit key (ESC) in V0-V3 games. That mimicks
                // the behavior of the original engine where pressing right
                // mouse button also skips the current cutscene.
                mouseAndKeyboardStat = 0;
//                lastKeyHit = Common::KeyState(Common::KEYCODE_ESCAPE);
            }
            else if (_leftBtnPressed.HasFlag(MouseButtonStatus.Clicked))
            {
                mouseAndKeyboardStat = (KeyCode)ScummMouseButtonState.LeftClick;
            }
            else if (_rightBtnPressed.HasFlag(MouseButtonStatus.Clicked))
            {
                mouseAndKeyboardStat = (KeyCode)ScummMouseButtonState.RightClick;
            }

            if (Game.Version >= 6)
            {
                Variables[VariableLeftButtonHold.Value] = _leftBtnPressed.HasFlag(MouseButtonStatus.Down) ? 1 : 0;
                Variables[VariableRightButtonHold.Value] = _rightBtnPressed.HasFlag(MouseButtonStatus.Down) ? 1 : 0;

                // scumm7: left/right button down
                if (Game.Version >= 7)
                {
                    Variables[VariableLeftButtonDown.Value] = _leftBtnPressed.HasFlag(MouseButtonStatus.Clicked) ? 1 : 0;
                    Variables[VariableRightButtonDown.Value] = _rightBtnPressed.HasFlag(MouseButtonStatus.Clicked) ? 1 : 0;
                }
            }

            _leftBtnPressed &= ~MouseButtonStatus.Clicked;
            _rightBtnPressed &= ~MouseButtonStatus.Clicked;

            var cutsceneExitKeyEnabled = (!VariableCutSceneExitKey.HasValue || Variables[VariableCutSceneExitKey.Value] != 0);
            var mainmenuKeyEnabled = VariableMainMenu.HasValue && _variables[VariableMainMenu.Value] != 0;

            if (cutsceneExitKeyEnabled && _inputState.IsKeyDown(KeyCode.Escape))
            {
                mouseAndKeyboardStat = (KeyCode)Variables[VariableCutSceneExitKey.Value];
                AbortCutscene();
            }

            if (mainmenuKeyEnabled && _inputState.IsKeyDown(KeyCode.F5))
            {
                if (VariableSaveLoadScript.HasValue && _currentRoom != 0)
                {
                    RunScript(Variables[VariableSaveLoadScript.Value], false, false, new int[0]);
                }

                ShowMenu();

                if (VariableSaveLoadScript2.HasValue && _currentRoom != 0)
                {
                    RunScript(Variables[VariableSaveLoadScript2.Value], false, false, new int[0]);
                }
            }

            for (var i = KeyCode.A; i <= KeyCode.Z; i++)
            {
                if (_inputState.IsKeyDown(i))
                {
                    mouseAndKeyboardStat = i;
                }
            }
            for (var i = KeyCode.F1; i <= KeyCode.F9; i++)
            {
                if (_inputState.IsKeyDown(i))
                {
                    mouseAndKeyboardStat = i;
                }
            }

            if (_inputState.IsKeyDown(KeyCode.Return))
            {
                mouseAndKeyboardStat = KeyCode.Return;
            }
            if (_inputState.IsKeyDown(KeyCode.Backspace))
            {
                mouseAndKeyboardStat = KeyCode.Backspace;
            }
            if (_inputState.IsKeyDown(KeyCode.Tab))
            {
                mouseAndKeyboardStat = KeyCode.Tab;
            }
            if (_inputState.IsKeyDown(KeyCode.Space))
            {
                mouseAndKeyboardStat = KeyCode.Space;
            }

            if ((Game.Id == "indy4" || Game.Id == "pass"))
            {
                var numpad = new int[]
                {
                    '0',
                    335, 336, 337,
                    331, 332, 333,
                    327, 328, 329
                };

                for (var i = KeyCode.D0; i <= KeyCode.D9; i++)
                {
                    if (_inputState.IsKeyDown(i))
                    {
                        mouseAndKeyboardStat = (KeyCode)numpad[i - KeyCode.D0];
                    }
                }
            }
            else
            {
                for (var i = KeyCode.D0; i <= KeyCode.D9; i++)
                {
                    if (_inputState.IsKeyDown(i))
                    {
                        mouseAndKeyboardStat = i - (int)KeyCode.D0 + '0';
                    }
                }
            }

            _mousePos = _inputManager.GetMousePosition();
            if (_mousePos.X < 0)
                _mousePos.X = 0;
            if (_mousePos.X > ScreenWidth - 1)
                _mousePos.X = (short)(ScreenWidth - 1);
            if (_mousePos.Y < 0)
                _mousePos.Y = 0;
            if (_mousePos.Y > ScreenHeight - 1)
                _mousePos.Y = (short)(ScreenHeight - 1);

            var mouseX = (ScreenStartStrip * 8) + _mousePos.X;
            Variables[VariableMouseX.Value] = (int)_mousePos.X;
            Variables[VariableMouseY.Value] = (int)_mousePos.Y;
            Variables[VariableVirtualMouseX.Value] = (int)mouseX;
            Variables[VariableVirtualMouseY.Value] = (int)_mousePos.Y - MainVirtScreen.TopLine + ((_game.Version >= 7) ? ScreenTop : 0);
        }

        protected void ClearClickedStatus()
        {
            mouseAndKeyboardStat = 0;
            _leftBtnPressed &= ~MouseButtonStatus.Clicked;
            _rightBtnPressed &= ~MouseButtonStatus.Clicked;
        }

        protected void ShowMenu()
        {
            var eh = ShowMenuDialogRequested;
            if (eh != null)
            {
                eh(this, EventArgs.Empty);
            }
        }
    }
}

