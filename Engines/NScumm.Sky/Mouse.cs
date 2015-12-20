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

using System;
using NScumm.Core;
using NScumm.Core.Graphics;

namespace NScumm.Sky
{
    internal class Mouse
    {
        private const int MiceFile = 60300;
        private const int NoMainObjects = 24;
        private const int NoLincObjects = 21;

        private readonly uint[] _mouseLincObjects =
        {
            24625,
            24649,
            24827,
            24651,
            24583,
            24581,
            24582,
            24628,
            24650,
            24629,
            24732,
            24631,
            24584,
            24630,
            24626,
            24627,
            24632,
            24643,
            24828,
            24830,
            24829
        };

        private readonly uint[] _mouseMainObjects =
        {
            65,
            9,
            66,
            64,
            8,
            63,
            10,
            11,
            71,
            76,
            37,
            36,
            42,
            75,
            79,
            6,
            74,
            39,
            49,
            43,
            34,
            35,
            77,
            38
        };

        private readonly SkyCompact _skyCompact;

        private bool _logicClick;
        private readonly byte[] _miceData;
        private ushort _mouseB;
        private byte[] _objectMouseData;
        private readonly Disk _skyDisk;
        private readonly SkySystem _system;

        public Mouse(SkySystem system, Disk skyDisk, SkyCompact skyCompact)
        {
            _system = system;
            _skyDisk = skyDisk;
            _skyCompact = skyCompact;

            CurrentMouseType = 6;
            MouseX = Screen.GameScreenWidth / 2;
            MouseY = Screen.GameScreenHeight / 2;

            _miceData = _skyDisk.LoadFile(MiceFile);

            //load in the object mouse file
            _objectMouseData = _skyDisk.LoadFile(MiceFile + 1);
        }

        public Logic Logic { get; internal set; }

        public ushort MouseX
        {
            get; private set;
        }

        public ushort MouseY { get; private set; }

        public ushort CurrentMouseType { get; private set; }

        public bool WasClicked
        {
            get
            {
                if (_logicClick)
                {
                    _logicClick = false;
                    return true;
                }
                else
                    return false;
            }
        }


        public void LogicClick()
        {
            _logicClick = true;
        }

        public void SpriteMouse(ushort frameNum, byte mouseX, byte mouseY)
        {
            CurrentMouseType = frameNum;

            var newCursor = 0;
            var header = ServiceLocator.Platform.ToStructure<DataFileHeader>(_miceData, 0);
            newCursor += header.s_sp_size * frameNum;
            newCursor += ServiceLocator.Platform.SizeOf<DataFileHeader>();

            var mouseWidth = header.s_width;
            var mouseHeight = header.s_height;

            _system.GraphicsManager.SetCursor(_miceData, newCursor, mouseWidth, mouseHeight, new Point(mouseX, mouseY), 0);
            _system.GraphicsManager.IsCursorVisible = frameNum != Logic.MOUSE_BLANK;
        }

        public void ReplaceMouseCursors(ushort fileNo)
        {
            _objectMouseData = _skyDisk.LoadFile(fileNo);
        }

        public void MouseEngine()
        {
            _logicClick = _mouseB > 0; // click signal is available for Logic for one gamecycle

            if (Logic.ScriptVariables[Logic.MOUSE_STOP] == 0)
            {
                if ((Logic.ScriptVariables[Logic.MOUSE_STATUS] & (1 << 1)) != 0)
                {
                    PointerEngine((ushort)(MouseX + Logic.TOP_LEFT_X), (ushort)(MouseY + Logic.TOP_LEFT_Y));
                    if ((Logic.ScriptVariables[Logic.MOUSE_STATUS] & (1 << 2)) != 0) //buttons enabled?
                        ButtonEngine1();
                }
            }
            _mouseB = 0; //don't save up buttons
        }

        public void FnOpenCloseHand(bool open)
        {
            if (!open && Logic.ScriptVariables[Logic.OBJECT_HELD] == 0)
            {
                SpriteMouse(1, 0, 0);
                return;
            }
            var cursor = (ushort)(FindMouseCursor(Logic.ScriptVariables[Logic.OBJECT_HELD]) << 1);
            if (open)
                cursor++;

            var header = ServiceLocator.Platform.ToStructure<DataFileHeader>(_objectMouseData, 0);
            int size = header.s_sp_size;

            var srcData = size * cursor + ServiceLocator.Platform.SizeOf<DataFileHeader>();
            var destData = ServiceLocator.Platform.SizeOf<DataFileHeader>();
            Array.Copy(_objectMouseData, srcData, _miceData, destData, size);
            SpriteMouse(0, 5, 5);
        }

        public bool FnAddHuman()
        {
            //reintroduce the mouse so that the human can control the player
            //could still be switched out at high-level

            if (Logic.ScriptVariables[Logic.MOUSE_STOP] == 0)
            {
                Logic.ScriptVariables[Logic.MOUSE_STATUS] |= 6; //cursor & mouse

                if (MouseY < 2) //stop mouse activating top line
                    MouseY = 2;

                // TODO: _system.WarpMouse(_mouseX, _mouseY);

                //force the pointer engine into running a get-off
                //even if it's over nothing

                //KWIK-FIX
                //get off may contain script to remove mouse pointer text
                //surely this script should be run just in case
                //I am going to try it anyway
                if (Logic.ScriptVariables[Logic.GET_OFF] != 0)
                    Logic.Script((ushort)Logic.ScriptVariables[Logic.GET_OFF],
                        (ushort)(Logic.ScriptVariables[Logic.GET_OFF] >> 16));

                Logic.ScriptVariables[Logic.SPECIAL_ITEM] = 0xFFFFFFFF;
                Logic.ScriptVariables[Logic.GET_OFF] = Logic.RESET_MOUSE;
            }

            return true;
        }

        public void FnSaveCoods()
        {
            Logic.ScriptVariables[Logic.SAFEX] = (uint)(MouseX + Logic.TOP_LEFT_X);
            Logic.ScriptVariables[Logic.SAFEY] = (uint)(MouseY + Logic.TOP_LEFT_Y);
        }

        public void LockMouse()
        {
            SystemVars.Instance.SystemFlags |= SystemFlags.MouseLocked;
        }

        public void UnlockMouse()
        {
            SystemVars.Instance.SystemFlags &= ~SystemFlags.MouseLocked;
        }

        public void MouseMoved(ushort x, ushort y)
        {
            MouseX = x;
            MouseY = y;
        }

        public void ButtonPressed(ushort button)
        {   
            _mouseB = button;
        }

        public void WaitMouseNotPressed(int minDelay)
        {
            var mousePressed = true;
            var now = Environment.TickCount;
            var inputMan = _system.InputManager;
            while (mousePressed || Environment.TickCount < now + minDelay)
            {
                if (SkyEngine.ShouldQuit)
                {
                    minDelay = 0;
                    mousePressed = false;
                }

                var state = inputMan.GetState();
                if (!state.IsLeftButtonDown && !state.IsRightButtonDown)
                    mousePressed = false;

                if (state.IsKeyDown(KeyCode.Escape))
                {
                    minDelay = 0;
                    mousePressed = false;
                }
            }
            _system.GraphicsManager.UpdateScreen();
            ServiceLocator.Platform.Sleep(20);
        }

        private ushort FindMouseCursor(uint itemNum)
        {
            for (ushort cnt = 0; cnt < NoMainObjects; cnt++)
            {
                if (itemNum == _mouseMainObjects[cnt])
                {
                    return cnt;
                }
            }
            for (ushort cnt = 0; cnt < NoLincObjects; cnt++)
            {
                if (itemNum == _mouseLincObjects[cnt])
                {
                    return cnt;
                }
            }
            return 0;
        }

        private void ButtonEngine1()
        {
            //checks for clicking on special item
            //"compare the size of this routine to S1 mouse_button"

            if (_mouseB != 0)
            {
                //anything pressed?
                Logic.ScriptVariables[Logic.BUTTON] = _mouseB;
                if (Logic.ScriptVariables[Logic.SPECIAL_ITEM] != 0)
                {
                    //over anything?
                    var item = _skyCompact.FetchCpt((ushort)Logic.ScriptVariables[Logic.SPECIAL_ITEM]);
                    if (item.Core.mouseClick != 0)
                        Logic.MouseScript(item.Core.mouseClick, item);
                }
            }
        }

        private void PointerEngine(ushort xPos, ushort yPos)
        {
            UShortAccess currentList;
            var currentListNum = Logic.ScriptVariables[Logic.MOUSE_LIST_NO];
            do
            {
                currentList = new UShortAccess(_skyCompact.FetchCptRaw((ushort)currentListNum), 0);
                while ((currentList[0] != 0) && (currentList[0] != 0xFFFF))
                {
                    var itemNum = currentList[0];
                    var itemData = _skyCompact.FetchCpt(itemNum);
                    currentList.Offset += 2;
                    if ((itemData.Core.screen == Logic.ScriptVariables[Logic.SCREEN]) &&
                        ((itemData.Core.status & 16) != 0))
                    {
                        if (itemData.Core.xcood + itemData.Core.mouseRelX > xPos) continue;
                        if (itemData.Core.xcood + itemData.Core.mouseRelX + itemData.Core.mouseSizeX < xPos) continue;
                        if (itemData.Core.ycood + itemData.Core.mouseRelY > yPos) continue;
                        if (itemData.Core.ycood + itemData.Core.mouseRelY + itemData.Core.mouseSizeY < yPos) continue;
                        // we've hit the item
                        if (Logic.ScriptVariables[Logic.SPECIAL_ITEM] == itemNum)
                            return;
                        Logic.ScriptVariables[Logic.SPECIAL_ITEM] = itemNum;
                        if (Logic.ScriptVariables[Logic.GET_OFF] != 0)
                            Logic.MouseScript(Logic.ScriptVariables[Logic.GET_OFF], itemData);
                        Logic.ScriptVariables[Logic.GET_OFF] = itemData.Core.mouseOff;
                        if (itemData.Core.mouseOn != 0)
                            Logic.MouseScript(itemData.Core.mouseOn, itemData);
                        return;
                    }
                }
                if (currentList[0] == 0xFFFF)
                    currentListNum = currentList[1];
            } while (currentList[0] != 0);

            if (Logic.ScriptVariables[Logic.SPECIAL_ITEM] != 0)
            {
                Logic.ScriptVariables[Logic.SPECIAL_ITEM] = 0;

                if (Logic.ScriptVariables[Logic.GET_OFF] != 0)
                    Logic.Script((ushort)Logic.ScriptVariables[Logic.GET_OFF], (ushort)(Logic.ScriptVariables[Logic.GET_OFF] >> 16));
                Logic.ScriptVariables[Logic.GET_OFF] = 0;
            }
        }
    }
}