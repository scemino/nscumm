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

namespace NScumm.Core
{
    partial class ScummEngine
    {
        IInputManager _inputManager;
        KeyCode mouseAndKeyboardStat;

        void CheckExecVerbs()
        {
            if (_userPut <= 0 || mouseAndKeyboardStat == 0)
                return;

            if ((ScummMouseButtonState)mouseAndKeyboardStat < ScummMouseButtonState.MaxKey)
            {
                // Check keypresses
                var vs = (from verb in _verbs.Skip(1)
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
                    RunInputScript(ClickArea.Verb, (KeyCode)_verbs[over].VerbId, code);
                }
                else
                {
                    // Scene was clicked
                    var area = zone == MainVirtScreen ? ClickArea.Scene : ClickArea.Verb;
                    RunInputScript(area, 0, code);
                }
            }
        }

        void ProcessInput()
        {
            mouseAndKeyboardStat = 0;

            bool mainmenuKeyEnabled = VariableMainMenu.HasValue && _variables[VariableMainMenu.Value] != 0;

            if (_inputManager.IsKeyDown(KeyCode.Escape))
            {
                mouseAndKeyboardStat = (KeyCode)Variables[VariableCutSceneExitKey.Value];
                AbortCutscene();
            }

            if (mainmenuKeyEnabled && _inputManager.IsKeyDown(KeyCode.F5))
            {
                var eh = ShowMenuDialogRequested;
                if (eh != null)
                {
                    eh(this, EventArgs.Empty);
                }
            }

            for (var i = KeyCode.A; i <= KeyCode.Z; i++)
            {
                if (_inputManager.IsKeyDown(i))
                {
                    mouseAndKeyboardStat = i;
                }
            }
            for (var i = KeyCode.F1; i <= KeyCode.F9; i++)
            {
                if (_inputManager.IsKeyDown(i))
                {
                    mouseAndKeyboardStat = i;
                }
            }

            for (var i = KeyCode.F1; i <= KeyCode.F9; i++)
            {
                if (_inputManager.IsKeyDown(i))
                {
                    mouseAndKeyboardStat = i;
                }
            }

            if (_inputManager.IsKeyDown(KeyCode.Return))
            {
                mouseAndKeyboardStat = KeyCode.Return;
            }
            if (_inputManager.IsKeyDown(KeyCode.Backspace))
            {
                mouseAndKeyboardStat = KeyCode.Backspace;
            }
            if (_inputManager.IsKeyDown(KeyCode.Tab))
            {
                mouseAndKeyboardStat = KeyCode.Tab;
            }
            if (_inputManager.IsKeyDown(KeyCode.Space))
            {
                mouseAndKeyboardStat = KeyCode.Space;
            }

            if (_inputManager.IsMouseLeftPressed())
            {
                mouseAndKeyboardStat = (KeyCode)ScummMouseButtonState.LeftClick;
            }

            if (_inputManager.IsMouseRightPressed())
            {
                mouseAndKeyboardStat = (KeyCode)ScummMouseButtonState.RightClick;
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
                    if (_inputManager.IsKeyDown(i))
                    {
                        mouseAndKeyboardStat = (KeyCode)numpad[i - KeyCode.D0];
                    }
                }
            }
            else
            {
                for (var i = KeyCode.D0; i <= KeyCode.D9; i++)
                {
                    if (_inputManager.IsKeyDown(i))
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
            Variables[VariableVirtualMouseY.Value] = (int)_mousePos.Y - MainVirtScreen.TopLine;
        }
    }
}

