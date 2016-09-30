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
using NScumm.Core.IO;

namespace NScumm.Sword1
{
    class MousePtr
    {
        public ushort numFrames
        {
            get { return Data.ToUInt16(Offset); }
            set { Data.WriteUInt16(Offset, value); }
        }
        public ushort sizeX
        {
            get { return Data.ToUInt16(Offset + 2); }
            set { Data.WriteUInt16(Offset + 2, value); }
        }
        public ushort sizeY
        {
            get { return Data.ToUInt16(Offset + 4); }
            set { Data.WriteUInt16(Offset + 4, value); }
        }
        public ushort hotSpotX
        {
            get { return Data.ToUInt16(Offset + 6); }
            set { Data.WriteUInt16(Offset + 6, value); }
        }
        public ushort hotSpotY
        {
            get { return Data.ToUInt16(Offset + 8); }
            set { Data.WriteUInt16(Offset + 8, value); }
        }
        public ByteAccess dummyData { get; }

        public const int Size = 58;

        public MousePtr(byte[] data)
        {
            Data = data;
            Offset = 0;
            dummyData = new ByteAccess(data, Offset + 10);
        }

        public int Offset { get; }
        public byte[] Data { get; }
    }

    struct MouseObj
    {
        public int id;
        public SwordObject compact;
    }

    internal class Mouse
    {
        const int MAX_MOUSE = 30;

        public const int BS1L_BUTTON_DOWN = 2;
        public const int BS1L_BUTTON_UP = 4;
        public const int BS1R_BUTTON_DOWN = 8;
        public const int BS1R_BUTTON_UP = 16;
        public const int BS1_WHEEL_UP = 32;
        public const int BS1_WHEEL_DOWN = 64;
        public const int MOUSE_BOTH_BUTTONS = (BS1L_BUTTON_DOWN | BS1R_BUTTON_DOWN);
        public const int MOUSE_DOWN_MASK = (BS1L_BUTTON_DOWN | BS1R_BUTTON_DOWN);
        public const int MOUSE_UP_MASK = (BS1L_BUTTON_UP | BS1R_BUTTON_UP);

        private MouseObj[] _objList = new MouseObj[MAX_MOUSE];
        private ResMan _resMan;
        private ObjectMan _objMan;
        private ISystem _system;

        uint _currentPtrId, _currentLuggageId;
        MousePtr _currentPtr;
        int _frame, _activeFrame;

        ushort _numObjs;
        ushort _lastState, _state;
        uint _getOff;
        bool _inTopMenu, _mouseOverride;
        private Logic _logic;
        private Menu _menu;
        private Point _mouse;

        public Mouse(ISystem system, ResMan resMan, ObjectMan objectMan)
        {
            _resMan = resMan;
            _objMan = objectMan;
            _system = system;
        }

        public void Initialize()
        {
            _numObjs = 0;
            Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_STATUS] = 0; // mouse off and unlocked
            _getOff = 0;
            _inTopMenu = false;
            _lastState = 0;
            _mouseOverride = false;
            _currentPtrId = _currentLuggageId = 0;

            for (byte cnt = 0; cnt < 17; cnt++)     // force res manager to keep mouse
                _resMan.ResOpen((uint)(SwordRes.MSE_POINTER + cnt)); // cursors in memory all the time

            _system.GraphicsManager.IsCursorVisible = false;
            CreatePointer(0, 0);
        }

        private void CreatePointer(uint ptrId, uint luggageId)
        {
            _currentPtr = null;

            if (ptrId != 0)
            {
                MousePtr lugg = null;
                MousePtr ptr = new MousePtr(_resMan.OpenFetchRes(ptrId));
                ushort noFrames = _resMan.ReadUInt16(ptr.numFrames);
                ushort ptrSizeX = _resMan.ReadUInt16(ptr.sizeX);
                ushort ptrSizeY = _resMan.ReadUInt16(ptr.sizeY);
                ushort luggSizeX = 0;
                ushort luggSizeY = 0;
                ushort resSizeX;
                ushort resSizeY;

                if (Sword1.SystemVars.Platform == Platform.PSX) //PSX pointers are half height
                    ptrSizeY *= 2;

                if (luggageId != 0)
                {
                    lugg = new MousePtr(_resMan.OpenFetchRes(luggageId));
                    luggSizeX = _resMan.ReadUInt16(lugg.sizeX);
                    luggSizeY = _resMan.ReadUInt16(lugg.sizeY);

                    if (SystemVars.Platform == Platform.PSX)
                        luggSizeY *= 2;

                    resSizeX = Math.Max(ptrSizeX, (ushort)((ptrSizeX / 2) + luggSizeX));
                    resSizeY = Math.Max(ptrSizeY, (ushort)((ptrSizeY / 2) + luggSizeY));
                }
                else
                {
                    resSizeX = ptrSizeX;
                    resSizeY = ptrSizeY;
                }
                _currentPtr = new MousePtr(new byte[MousePtr.Size + resSizeX * resSizeY * noFrames]);
                _currentPtr.hotSpotX = _resMan.ReadUInt16(ptr.hotSpotX);
                _currentPtr.hotSpotY = _resMan.ReadUInt16(ptr.hotSpotY);
                _currentPtr.numFrames = noFrames;
                _currentPtr.sizeX = resSizeX;
                _currentPtr.sizeY = resSizeY;
                var ptrData = _currentPtr;
                var ptrDataOff = MousePtr.Size;
                int dstDataOff = 0;
                ptrData.Data.Set(ptrData.Offset + ptrDataOff, 255, resSizeX * resSizeY * noFrames);
                if (luggageId != 0)
                {
                    dstDataOff = ptrDataOff + resSizeX - luggSizeX;
                    for (uint frameCnt = 0; frameCnt < noFrames; frameCnt++)
                    {
                        var luggSrc = MousePtr.Size;
                        dstDataOff += (resSizeY - luggSizeY) * resSizeX;
                        for (uint cnty = 0; cnty < (uint)(SystemVars.Platform == Platform.PSX ? luggSizeY / 2 : luggSizeY); cnty++)
                        {
                            for (uint cntx = 0; cntx < luggSizeX; cntx++)
                                if (lugg.Data[lugg.Offset + luggSrc + cntx] != 0)
                                    ptrData.Data[ptrData.Offset + dstDataOff + cntx] = lugg.Data[lugg.Offset + luggSrc + cntx];

                            if (SystemVars.Platform == Platform.PSX)
                            {
                                dstDataOff += resSizeX;
                                for (uint cntx = 0; cntx < luggSizeX; cntx++)
                                    if (lugg.Data[lugg.Offset + luggSrc + cntx] != 0)
                                        ptrData.Data[ptrData.Offset + dstDataOff + cntx] = lugg.Data[lugg.Offset + luggSrc + cntx];
                            }

                            dstDataOff += resSizeX;
                            luggSrc += luggSizeX;
                        }
                    }
                    _resMan.ResClose(luggageId);
                }

                dstDataOff = ptrDataOff;
                var srcDataOff = MousePtr.Size;
                for (uint frameCnt = 0; frameCnt < noFrames; frameCnt++)
                {
                    for (uint cnty = 0; cnty < (uint)(SystemVars.Platform == Platform.PSX ? ptrSizeY / 2 : ptrSizeY); cnty++)
                    {
                        for (uint cntx = 0; cntx < ptrSizeX; cntx++)
                            if (ptr.Data[ptr.Offset + srcDataOff + cntx] != 0)
                                ptrData.Data[ptrData.Offset + dstDataOff + cntx] = ptr.Data[ptr.Offset + srcDataOff + cntx];

                        if (SystemVars.Platform == Platform.PSX)
                        {
                            dstDataOff += resSizeX;
                            for (uint cntx = 0; cntx < ptrSizeX; cntx++)
                                if (ptr.Data[ptr.Offset + srcDataOff + cntx] != 0)
                                    ptrData.Data[ptrData.Offset + dstDataOff + cntx] = ptr.Data[ptr.Offset + srcDataOff + cntx];
                        }

                        srcDataOff += ptrSizeX;
                        dstDataOff += resSizeX;
                    }
                    dstDataOff += (resSizeY - ptrSizeY) * resSizeX;
                }
                _resMan.ResClose(ptrId);
            }
        }

        public void Animate()
        {
            if ((Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_STATUS] == 1) || (_mouseOverride && _currentPtr != null))
            {
                _frame = (_frame + 1) % _currentPtr.numFrames;

                if (_activeFrame == _frame)
                    return;

                var ptrData = MousePtr.Size;
                ptrData += _frame * _currentPtr.sizeX * _currentPtr.sizeY;

                _system.GraphicsManager.SetCursor(_currentPtr.Data, _currentPtr.Offset + ptrData, _currentPtr.sizeX, _currentPtr.sizeY, new Point((short) _currentPtr.hotSpotX, (short) _currentPtr.hotSpotY), 255);

                _activeFrame = _frame;
            }
        }

        public void Engine(ushort x, ushort y, ushort eventFlags)
        {
            _state = 0; // all mouse events are flushed after one cycle.
            if (_lastState != 0)
            { // delay all events by one cycle to notice L_button + R_button clicks correctly.
                _state = (ushort)(_lastState | eventFlags);
                _lastState = 0;
            }
            else if (eventFlags != 0)
                _lastState = eventFlags;

            // if we received both, mouse down and mouse up event in this cycle, resort them so that
            // we'll receive the up event in the next one.
            if ((_state & MOUSE_DOWN_MASK) != 0 && (_state & MOUSE_UP_MASK) != 0)
            {
                _lastState = (ushort)(_state & MOUSE_UP_MASK);
                _state &= MOUSE_DOWN_MASK;
            }

            _mouse.X = (short) x;
            _mouse.Y = (short) y;
            if ((Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_STATUS] & 1) == 0)
            {  // no human?
                _numObjs = 0;
                return; // no human, so we don't want the mouse engine
            }

            if (Logic.ScriptVars[(int)ScriptVariableNames.TOP_MENU_DISABLED] == 0)
            {
                if (y < 40)
                { // okay, we are in the top menu.
                    if (!_inTopMenu)
                    { // are we just entering it?
                        if (Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] == 0)
                            _menu.FnStartMenu();
                        SetPointer(SwordRes.MSE_POINTER, 0);
                    }
                    _menu.CheckTopMenu();
                    _inTopMenu = true;
                }
                else if (_inTopMenu)
                { // we're not in the menu. did we just leave it?
                    if (Logic.ScriptVars[(int)ScriptVariableNames.OBJECT_HELD] == 0)
                        _menu.FnEndMenu();
                    _inTopMenu = false;
                }
            }
            else if (_inTopMenu)
            {
                _menu.FnEndMenu();
                _inTopMenu = false;
            }

            Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_X] = Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X] + x + 128;
            Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_Y] = Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y] + y + 128 - 40;

            //-
            int touchedId = 0;
            ushort clicked = 0;
            if (y > 40)
            {
                for (ushort priority = 0; (priority < 10) && (touchedId == 0); priority++)
                {
                    for (ushort cnt = 0; (cnt < _numObjs) && (touchedId == 0); cnt++)
                    {
                        if ((_objList[cnt].compact.priority == priority) &&
                                (Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_X] >= (uint)_objList[cnt].compact.mouse_x1) &&
                                (Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_X] <= (uint)_objList[cnt].compact.mouse_x2) &&
                                (Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_Y] >= (uint)_objList[cnt].compact.mouse_y1) &&
                                (Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_Y] <= (uint)_objList[cnt].compact.mouse_y2))
                        {
                            touchedId = _objList[cnt].id;
                            clicked = cnt;
                        }
                    }
                }
                if (touchedId != (int)Logic.ScriptVars[(int)ScriptVariableNames.SPECIAL_ITEM])
                { //the mouse collision situation has changed in one way or another
                    Logic.ScriptVars[(int)ScriptVariableNames.SPECIAL_ITEM] = (uint)touchedId;
                    if (_getOff != 0)
                    { // there was something else selected before, run its get-off script
                        _logic.RunMouseScript(null, (int)_getOff);
                        _getOff = 0;
                    }
                    if (touchedId != 0)
                    { // there's something new selected, now.
                        if (_objList[clicked].compact.mouse_on != 0)  //run its get on
                            _logic.RunMouseScript(_objList[clicked].compact, _objList[clicked].compact.mouse_on);

                        _getOff = (uint)_objList[clicked].compact.mouse_off; //setup get-off for later
                    }
                }
            }
            else
                Logic.ScriptVars[(int)ScriptVariableNames.SPECIAL_ITEM] = 0;
            if ((_state & MOUSE_DOWN_MASK) != 0)
            {
                if (_inTopMenu)
                {
                    if (Logic.ScriptVars[(int)ScriptVariableNames.SECOND_ITEM] != 0)
                        _logic.RunMouseScript(null, (int)Menu._objectDefs[Logic.ScriptVars[(int)ScriptVariableNames.SECOND_ITEM], MenuObject.useScript]);
                    if (Logic.ScriptVars[(int)ScriptVariableNames.MENU_LOOKING] != 0)
                        _logic.CfnPresetScript(null, -1, Logic.PLAYER, Logic.SCR_menu_look, 0, 0, 0, 0);
                }

                Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_BUTTON] = (uint)(_state & MOUSE_DOWN_MASK);
                if (Logic.ScriptVars[(int)ScriptVariableNames.SPECIAL_ITEM] != 0)
                {
                    SwordObject compact = _objMan.FetchObject(Logic.ScriptVars[(int)ScriptVariableNames.SPECIAL_ITEM]);
                    _logic.RunMouseScript(compact, compact.mouse_click);
                }
            }
            _numObjs = 0;
        }

        public void AddToList(int id, SwordObject compact)
        {
            _objList[_numObjs].id = id;
            _objList[_numObjs].compact = compact;
            _numObjs++;
        }

        public void SetLuggage(uint resId, uint rate)
        {
            _currentLuggageId = resId;
            _frame = 0;
            _activeFrame = -1;

            CreatePointer(_currentPtrId, resId);
        }

        public void FnNoHuman()
        {
            if ((Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_STATUS] & 2) != 0) // locked, can't do anything
                return;
            Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_STATUS] = 0; // off & unlocked
            SetLuggage(0, 0);
            SetPointer(0, 0);
        }

        public void FnAddHuman()
        {
            if ((Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_STATUS] & 2) != 0) // locked, can't do anything
                return;
            Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_STATUS] = 1;
            Logic.ScriptVars[(int)ScriptVariableNames.SPECIAL_ITEM] = 0;
            _getOff = Logic.SCR_std_off;
            SetPointer(SwordRes.MSE_POINTER, 0);
        }

        public void FnBlankMouse()
        {
            SetPointer(0, 0);
        }

        public void FnNormalMouse()
        {
            SetPointer(SwordRes.MSE_POINTER, 0);
        }

        public void FnLockMouse()
        {
            Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_STATUS] |= 2;
        }

        public void FnUnlockMouse()
        {
            Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_STATUS] &= 1;
        }

        public void SetPointer(uint resId, uint rate)
        {
            _currentPtrId = resId;
            _frame = 0;
            _activeFrame = -1;

            CreatePointer(resId, _currentLuggageId);

            if ((resId == 0) || ((Logic.ScriptVars[(int)ScriptVariableNames.MOUSE_STATUS] & 1) == 0 && (!_mouseOverride)))
            {
                _system.GraphicsManager.IsCursorVisible = false;
            }
            else
            {
                Animate();
                _system.GraphicsManager.IsCursorVisible = true;
            }
        }

        public ushort TestEvent()
        {
            return _state;
        }

        public void UseLogicAndMenu(Logic logic, Menu menu)
        {
            _logic = logic;
            _menu = menu;
        }

        uint savedPtrId = 0;

        public void ControlPanel(bool on)
        {
            if (on)
            {
                savedPtrId = _currentPtrId;
                _mouseOverride = true;
                SetLuggage(0, 0);
                SetPointer(SwordRes.MSE_POINTER, 0);
            }
            else
            {
                _currentPtrId = savedPtrId;
                _mouseOverride = false;
                SetLuggage(_currentLuggageId, 0);
                SetPointer(_currentPtrId, 0);
            }
        }

        public void GiveCoords(out ushort x, out ushort y)
        {
            x = (ushort) _mouse.X;
            y = (ushort) _mouse.Y;
        }
    }
}