//
//  AGOSEngine.Script.cs
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

using System;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AGOSEngine
    {
        protected void WriteNextVarContents(ushort contents)
        {
            WriteVariable((ushort) GetVarWrapper(), contents);
        }

        private void SetWindowImageEx(ushort mode, ushort vgaSpriteId)
        {
            _window3Flag = 0;

            if (mode == 4)
            {
                vc29_stopAllSounds();

                if (GameType == SIMONGameType.GType_ELVIRA1)
                {
                    if (_variableArray[299] == 0)
                    {
                        _variableArray[293] = 0;
                        _wallOn = 0;
                    }
                }
                else if (GameType == SIMONGameType.GType_ELVIRA2)
                {
                    if (_variableArray[70] == 0)
                    {
                        _variableArray[71] = 0;
                        _wallOn = 0;
                    }
                }
            }

            if ((_videoLockOut & 0x10) != 0)
                Error("setWindowImageEx: _videoLockOut & 0x10");

            if (GameType != SIMONGameType.GType_PP && GameType != SIMONGameType.GType_FF)
            {
                if (GameType == SIMONGameType.GType_WW && (mode == 6 || mode == 8 || mode == 9))
                {
                    SetWindowImage(mode, vgaSpriteId);
                }
                else
                {
                    while (_copyScnFlag != 0 && !HasToQuit)
                        Delay(1);

                    SetWindowImage(mode, vgaSpriteId);
                }
            }
            else
            {
                SetWindowImage(mode, vgaSpriteId);
            }

            // Amiga versions wait for verb area to be displayed.
            if (GameType == SIMONGameType.GType_SIMON1 && GamePlatform == Platform.Amiga && vgaSpriteId == 1)
            {
                _copyScnFlag = 5;
                while (_copyScnFlag != 0 && !HasToQuit)
                    Delay(1);
            }
        }

        private void SendSync(uint a)
        {
            ushort id = To16Wrapper(a);
            _videoLockOut |= 0x8000;
            _vcPtr = BitConverter.GetBytes(id);
            vc15_sync();
            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        protected void o_picture()
        {
            // 96
            uint vgaRes = GetVarOrWord();
            uint mode = GetVarOrByte();

            // WORKAROUND: For a script bug in the Amiga AGA/CD32 versions
            // When selecting locations on the magical map, the script looks
            // for vga_res 12701, but only vga_res 12700 exists.
            if (GameType == SIMONGameType.GType_SIMON1 && GamePlatform == Platform.Amiga &&
                vgaRes == 12701)
            {
                return;
            }

            if (GameType == SIMONGameType.GType_PP && GameId != GameIds.GID_DIMP)
            {
                if (vgaRes == 8700 && GetBitFlag(107))
                {
                    _vgaPeriod = 30;
                }

                _picture8600 = vgaRes == 8600;
            }

            SetWindowImageEx((ushort) mode, (ushort) vgaRes);
        }

        protected void o_killAnimate()
        {
            // 100: kill animations
            _videoLockOut |= 0x8000;
            vc27_resetSprite();
            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        protected void o_cls()
        {
            // 103
            MouseOff();
            RemoveIconArray(_curWindow);
            ShowMessageFormat("\x0C");
            _oracleMaxScrollY = 0;
            _noOracleScroll = 0;
            MouseOn();
        }

        protected void o_closeWindow()
        {
            // 104
            CloseWindow(GetVarOrByte() & 7);
        }

        protected void o_delBox()
        {
            // 108: delete box
            UndefineBox((int) GetVarOrWord());
        }

        protected void o_doIcons()
        {
            // 114
            Item item = GetNextItemPtr();
            int num = (int) GetVarOrByte();
            MouseOff();
            DrawIconArray(num, item, 0, 0);
            MouseOn();
        }

        protected void o_notAt()
        {
            // 2: ptrA parent is not
            SetScriptCondition(Me().parent != GetNextItemID());
        }

        protected void o_carried()
        {
            // 5: parent is 1
            SetScriptCondition(GetNextItemPtr().parent == GetItem1ID());
        }

        protected void o_notCarried()
        {
            // 6: parent isnot 1
            SetScriptCondition(GetNextItemPtr().parent != GetItem1ID());
        }

        protected void o_isAt()
        {
            // 7: parent is
            Item item = GetNextItemPtr();
            SetScriptCondition(item.parent == GetNextItemID());
        }

        protected void o_zero()
        {
            // 11: is zero
            SetScriptCondition(GetNextVarContents() == 0);
        }

        protected void o_notZero()
        {
            // 12: isnot zero
            SetScriptCondition(GetNextVarContents() != 0);
        }

        protected void o_eq()
        {
            // 13: equal
            uint tmp = GetNextVarContents();
            uint tmp2 = GetVarOrWord();

#if __DS__
// HACK: Skip attempt to read Calypso's letter manually,
// due to speech segment been too large to fit into memory
            if (GameType == SIMONGameType.GType_SIMON1 && (Features.HasFlag(GameFeatures.GF_TALKIE) &&
                GamePlatform == Platform.Windows && _currentTable!=null) {
                if (_currentTable.id == 71 && tmp == 1 && tmp2 == 1) {
                    SetScriptCondition(false);
                    return;
                }
            }
#endif
            SetScriptCondition(tmp == tmp2);
        }

        protected void o_notEq()
        {
            // 14: not equal
            uint tmp = GetNextVarContents();
            SetScriptCondition(tmp != GetVarOrWord());
        }

        protected void o_gt()
        {
            // 15: is greater
            short tmp1 = (short) GetNextVarContents();
            short tmp2 = (short) GetVarOrWord();
            SetScriptCondition(tmp1 > tmp2);
        }

        protected void o_lt()
        {
            // 16: is less
            short tmp1 = (short) GetNextVarContents();
            short tmp2 = (short) GetVarOrWord();
            SetScriptCondition(tmp1 < tmp2);
        }

        protected void o_eqf()
        {
            // 17: is eq f
            uint tmp = GetNextVarContents();
            SetScriptCondition(tmp == GetNextVarContents());
        }

        protected void o_notEqf()
        {
            // 18: is not equal f
            uint tmp = GetNextVarContents();
            SetScriptCondition(tmp != GetNextVarContents());
        }

        protected void o_ltf()
        {
            // 19: is greater f
            short tmp1 = (short) GetNextVarContents();
            short tmp2 = (short) GetNextVarContents();
            SetScriptCondition(tmp1 < tmp2);
        }

        protected void o_gtf()
        {
            // 20: is less f
            short tmp1 = (short) GetNextVarContents();
            short tmp2 = (short) GetNextVarContents();
            SetScriptCondition(tmp1 > tmp2);
        }

        protected void o_isRoom()
        {
            // 25: is room
            SetScriptCondition(IsRoom(GetNextItemPtr()));
        }

        protected void o_isObject()
        {
            // 26: is object
            SetScriptCondition(IsObject(GetNextItemPtr()));
        }

        protected void o_state()
        {
            // 27: item state is
            Item item = GetNextItemPtr();
            SetScriptCondition((uint) item.state == GetVarOrWord());
        }

        protected void o_oflag()
        {
            // 28: item has prop
            var subObject = (SubObject) FindChildOfType(GetNextItemPtr(), ChildType.kObjectType);
            int num = (int) GetVarOrByte();
            SetScriptCondition(subObject != null && (subObject.objectFlags & (SubObjectFlags) (1 << num)) != 0);
        }

        protected void o_destroy()
        {
            // 31: set no parent
            SetItemParent(GetNextItemPtr(), null);
        }

        protected void o_copyff()
        {
            // 36: copy var
            uint value = GetNextVarContents();
            WriteNextVarContents((ushort) value);
        }

        protected void o_clear()
        {
            // 41: zero var
            WriteNextVarContents(0);
        }

        protected void o_let()
        {
            // 42: set var
            uint var = GetVarWrapper();
            uint value = GetVarOrWord();

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF && _currentTable != null)
            {
                // WORKAROUND: When the repair man comes to fix the car, the game doesn't
                // wait long enough for the screen to completely scroll to the left side.
                if (_currentTable.id == 20438 && var == 103 && value == 60)
                {
                    value = 71;
                }
            }

            WriteVariable((ushort) var, (ushort) value);
        }

        protected void o_add()
        {
            // 43: add
            uint var = GetVarWrapper();
            WriteVariable((ushort) var, (ushort) ReadVariable((ushort) (var + GetVarOrWord())));

            // WORKAROUND: The conversation of the male in Vid-Phone Booth at Dave's Space Bar
            // is based on variable 116, but stops due to a missing option (37).
            if (GameType == SIMONGameType.GType_FF && _currentTable.id == 10538 && ReadVariable(116) == 37)
                WriteVariable(116, 38);
        }

        protected void o_sub()
        {
            // 44: sub
            uint var = GetVarWrapper();
            WriteVariable((ushort) var, (ushort) (ReadVariable((ushort) var) - GetVarOrWord()));
        }

        protected void o_addf()
        {
            // 45: add f
            uint var = GetVarWrapper();
            WriteVariable((ushort) var, (ushort) (ReadVariable((ushort) var) + GetNextVarContents()));
        }

        protected void o_subf()
        {
            // 46: sub f
            uint var = GetVarWrapper();
            WriteVariable((ushort) var, (ushort) (ReadVariable((ushort) var) - GetNextVarContents()));
        }

        protected void o_mul()
        {
            // 47: mul
            uint var = GetVarWrapper();
            WriteVariable((ushort) var, (ushort) (ReadVariable((ushort) var) * GetVarOrWord()));
        }

        protected void o_div()
        {
            // 48: div
            uint var = GetVarWrapper();
            int value = (int) GetVarOrWord();
            if (value == 0)
                Error("o_div: Division by zero");
            WriteVariable((ushort) var, (ushort) (ReadVariable((ushort) var) / value));
        }

        protected void o_mulf()
        {
            // 49: mul f
            uint var = GetVarWrapper();
            WriteVariable((ushort) var, (ushort) (ReadVariable((ushort) var) * GetNextVarContents()));
        }

        protected void o_divf()
        {
            // 50: div f
            uint var = GetVarWrapper();
            int value = (int) GetNextVarContents();
            if (value == 0)
                Error("o_divf: Division by zero");
            WriteVariable((ushort) var, (ushort) (ReadVariable((ushort) var) / value));
        }

        protected void o_mod()
        {
            // 51: mod
            uint var = GetVarWrapper();
            int value = (int) GetVarOrWord();
            if (value == 0)
                Error("o_mod: Division by zero");
            WriteVariable((ushort) var, (ushort) (ReadVariable((ushort) var) % value));
        }

        protected void o_modf()
        {
            // 52: mod f
            uint var = GetVarWrapper();
            int value = (int) GetNextVarContents();
            if (value == 0)
                Error("o_modf: Division by zero");
            WriteVariable((ushort) var, (ushort) (ReadVariable((ushort) var) % value));
        }

        protected void o_random()
        {
            // 53: random
            uint var = GetVarWrapper();
            uint value = (ushort) GetVarOrWord();
            WriteVariable((ushort) var, (ushort) _rnd.GetRandomNumber(value - 1));
        }

        protected void o_goto()
        {
            // 55: set itemA parent
            var item = GetNextItemID();
            SetItemParent(Me(), _itemArrayPtr[item]);
        }

        protected void o_oset()
        {
            // 56: set child2 fr bit
            var subObject = (SubObject) FindChildOfType(GetNextItemPtr(), ChildType.kObjectType);
            int value = (int) GetVarOrByte();
            if (subObject != null && value >= 16)
                subObject.objectFlags |= (SubObjectFlags) (1 << value);
        }

        protected void o_oclear()
        {
            // 57: clear child2 fr bit
            var subObject = (SubObject) FindChildOfType(GetNextItemPtr(), ChildType.kObjectType);
            int value = (int) GetVarOrByte();
            if (subObject != null && value >= 16)
                subObject.objectFlags &= (SubObjectFlags) ~(1 << value);
        }

        protected void o_putBy()
        {
            // 58: make siblings
            Item item = GetNextItemPtr();
            SetItemParent(item, DerefItem(GetNextItemPtr().parent));
        }

        protected void o_inc()
        {
            // 59: item inc state
            Item item = GetNextItemPtr();
            if (item.state <= 30000)
            {
                SetItemState(item, item.state + 1);
                SynchChain(item);
            }
        }

        protected void o_dec()
        {
            // 60: item dec state
            Item item = GetNextItemPtr();
            if (item.state >= 0)
            {
                SetItemState(item, item.state - 1);
                SynchChain(item);
            }
        }

        protected void o_setState()
        {
            // 61: item set state
            Item item = GetNextItemPtr();
            int value = (int) GetVarOrWord();
            if (value < 0)
                value = 0;
            if (value > 30000)
                value = 30000;
            SetItemState(item, value);
            SynchChain(item);
        }

        protected void o_message()
        {
            // 63: show string nl
            ShowMessageFormat("{0}\n", GetStringPtrById((ushort) GetNextStringID()));
        }

        protected void o_msg()
        {
            // 64: show string
            ShowMessageFormat("{0}", GetStringPtrById((ushort) GetNextStringID()));
        }

        protected void o_when()
        {
            // 76: add timeout
            ushort timeout = (ushort) GetVarOrWord();
            AddTimeEvent(timeout, (ushort) GetVarOrWord());
        }

        protected void o_waitSync()
        {
            // 119: wait vga
            int var = (int) GetVarOrWord();
            _scriptVar2 = var == 200;

            if (var != 200 || !_skipVgaWait)
                WaitForSync((uint) var);
            _skipVgaWait = false;
        }

        protected void o_sync()
        {
            // 120: sync
            SendSync(GetVarOrWord());
        }

        protected void o_defObj()
        {
            // 121: set vga item
            uint slot = GetVarOrByte();
            _objectArray[slot] = GetNextItemPtr();
        }

        protected void o_here()
        {
            // 125: item is sibling with item 1
            Item item = GetNextItemPtr();
            SetScriptCondition(Me().parent == item.parent);
        }

        protected void o_doClassIcons()
        {
            // 126: do class icons
            Item item = GetNextItemPtr();
            int num = (int) GetVarOrByte();
            int a = (int) GetVarOrByte();

            MouseOff();
            if (GameType == SIMONGameType.GType_ELVIRA1)
                DrawIconArray(num, item, 0, a);
            else
                DrawIconArray(num, item, 0, 1 << a);
            MouseOn();
        }

        protected void o_playTune()
        {
            // 127: play tune
            ushort music = (ushort) GetVarOrWord();
            ushort track = (ushort) GetVarOrWord();

            if (music != _lastMusicPlayed)
            {
                _lastMusicPlayed = (short) music;
                PlayMusic(music, track);
            }
        }

        protected void o_freezeZones()
        {
            // 138: freeze zones
            FreezeBottom();

            if (!_copyProtection && !(_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE)) &&
                _currentTable != null)
            {
                if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 && _currentTable.id == 2924) ||
                    (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 && _currentTable.id == 1322))
                {
                    _variableArray[134] = 3;
                    _variableArray[135] = 3;
                    SetBitFlag(135, true);
                    SetScriptCondition(false);
                }
            }
        }


        private bool IsRoom(Item item)
        {
            return FindChildOfType(item, ChildType.kRoomType) != null;
        }

        private bool IsObject(Item item)
        {
            return FindChildOfType(item, ChildType.kObjectType) != null;
        }
    }
}