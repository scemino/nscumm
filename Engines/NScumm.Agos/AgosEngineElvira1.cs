//
//  AGOSEngineElvira1.cs
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
using System.Collections.Generic;
using NScumm.Core;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    internal class AgosEngineElvira1 : AgosEngine
    {
        protected static readonly GameSpecificSettings Simon1Settings =
            new GameSpecificSettings
            {
                base_filename = string.Empty, // base_filename
                restore_filename = string.Empty, // restore_filename
                tbl_filename = string.Empty, // tbl_filename
                effects_filename = "EFFECTS", // effects_filename
                speech_filename = "SIMON" // speech_filename
            };

        private Dictionary<int, Action> _opcodes;

        public AgosEngineElvira1(ISystem system, GameSettings settings, AgosGameDescription gd)
            : base(system, settings, gd)
        {
        }

        protected override void SetupGame()
        {
            gss = Simon1Settings;
            _numVideoOpcodes = 57;
            _vgaMemSize = 1000000;
            _itemMemSize = 64000;
            _tableMemSize = 256000;
            _frameCount = 4;
            _vgaBaseDelay = 1;
            _vgaPeriod = 50;
            _numVars = 512;

            _numMusic = 14;
            _numZone = 74;

            SetupGameCore();
        }

        protected override void SetupOpcodes()
        {
            _opcodes = new Dictionary<int, Action>
            {
                {0, o_at},
                {1, o_notAt},
                {2, oe1_present},
                {3, oe1_notPresent},
                {4, oe1_worn},
                {5, oe1_notWorn},
                {6, o_carried},
                {7, o_notCarried},
                {8, o_isAt},
                {9, oe1_isNotAt},
                {10, oe1_sibling},
                {11, oe1_notSibling},
                {12, o_zero},
                {13, o_notZero},
                {14, o_eq},
                {15, o_notEq},
                {16, o_gt},
                {17, o_lt},
                {18, o_eqf},
                {19, o_notEqf},
                {20, o_ltf},
                {21, o_gtf},
                {22, oe1_isIn},
                {23, oe1_isNotIn},
                {29, o_chance},
                {30, oe1_isPlayer},
                {32, o_isRoom},
                {33, o_isObject},
                {34, o_state},
                {36, o_oflag},
                {37, oe1_canPut},
                {47, oe1_create},
                {48, o_destroy},
                {51, o_place},
                {54, oe1_copyof},
                {55, oe1_copyfo},
                {56, o_copyff},
                {57, oe1_whatO},
                {59, oe1_weigh},
                {60, oe1_setFF},
                {61, o_clear},
                {64, o_let},
                {65, o_add},
                {66, o_sub},
                {67, o_addf},
                {68, o_subf},
                {69, o_mul},
                {70, o_div},
                {71, o_mulf},
                {72, o_divf},
                {73, o_mod},
                {74, o_modf},
                {75, o_random},
                {76, oe1_moveDirn},
                {77, o_goto},
                {80, o_oset},
                {81, o_oclear},
                {84, o_putBy},
                {85, o_inc},
                {86, o_dec},
                {87, o_setState},
                {89, o_print},
                {90, oe1_score},
                {91, o_message},
                {92, o_msg},
                {96, oe1_look},
                {97, o_end},
                {98, o_done},
                {105, o_process},
                {106, oe1_doClass},
                {112, oe1_pObj},
                {114, oe1_pName},
                {115, oe1_pcName},
                {119, o_when},
                {128, o_if1},
                {129, o_if2},
                {135, oe1_isCalled},
                {136, o_is},
                {137, o_restoreIcons},
                {152, o_debug},
                {162, oe1_cFlag},
                {164, oe1_rescan},
                {176, oe1_setUserItem},
                {177, oe1_getUserItem},
                {178, oe1_clearUserItem},
                {180, oe1_whereTo},
                {181, oe1_doorExit},
                {198, o_comment},
                {202, oe1_loadGame},
                {206, o_getParent},
                {207, o_getNext},
                {208, o_getChildren},
                {219, oe1_findMaster},
                {220, oe1_nextMaster},
                {224, o_picture},
                {225, o_loadZone},
                {226, oe1_animate},
                {227, oe1_stopAnimate},
                {228, o_killAnimate},
                {229, o_defWindow},
                {230, o_window},
                {231, o_cls},
                {232, o_closeWindow},
                {233, oe1_menu},
                {235, oe1_addBox},
                {236, o_delBox},
                {237, o_enableBox},
                {238, o_disableBox},
                {239, o_moveBox},
                {242, o_doIcons},
                {243, o_isClass},
                {249, o_setClass},
                {250, o_unsetClass},
                {251, oe1_bitClear},
                {252, oe1_bitSet},
                {253, oe1_bitTest},
                {255, o_waitSync},
                {256, o_sync},
                {257, o_defObj},
                {258, oe1_enableInput},
                {259, oe1_setTime},
                {260, oe1_ifTime},
                {261, o_here},
                {262, o_doClassIcons},
                {263, oe1_playTune},
                {266, o_setAdjNoun},
                {267, oe1_zoneDisk},
                {268, o_saveUserGame},
                {269, o_loadUserGame},
                {270, oe1_printStats},
                {271, oe1_stopTune},
                {272, oe1_printPlayerDamage},
                {273, oe1_printMonsterDamage},
                {274, oe1_pauseGame},
                {275, o_copysf},
                {276, o_restoreIcons},
                {277, oe1_printPlayerHit},
                {278, oe1_printMonsterHit},
                {279, o_freezeZones},
                {280, o_placeNoIcons},
                {281, o_clearTimers},
                {282, o_setDollar},
                {283, o_isBox},
            };
            _numOpcodes = 284;
        }

        protected override void SetupVideoOpcodes(Action[] op)
        {
            Debug("AGOSEngine_Elvira1::setupVideoOpcodes");
            op[1] = vc1_fadeOut;
            op[2] = vc2_call;
            op[3] = vc3_loadSprite;
            op[4] = vc4_fadeIn;
            op[5] = vc5_ifEqual;
            op[6] = vc6_ifObjectHere;
            op[7] = vc7_ifObjectNotHere;
            op[8] = vc8_ifObjectIsAt;
            op[9] = vc9_ifObjectStateIs;
            op[10] = vc10_draw;
            op[13] = vc12_delay;
            op[14] = vc13_addToSpriteX;
            op[15] = vc14_addToSpriteY;
            op[16] = vc15_sync;
            op[17] = vc16_waitSync;
            op[18] = vc17_waitEnd;
            op[19] = vc18_jump;
            op[20] = vc19_loop;
            op[21] = vc20_setRepeat;
            op[22] = vc21_endRepeat;
            op[23] = vc22_setPalette;
            op[24] = vc23_setPriority;
            op[25] = vc24_setSpriteXY;
            op[26] = vc25_halt_sprite;
            op[27] = vc26_setSubWindow;
            op[28] = vc27_resetSprite;
            op[29] = vc28_playSFX;
            op[30] = vc29_stopAllSounds;
            op[31] = vc30_setFrameRate;
            op[32] = vc31_setWindow;
            op[33] = vc32_saveScreen;
            op[34] = vc33_setMouseOn;
            op[35] = vc34_setMouseOff;
            op[38] = vc35_clearWindow;
            op[40] = vc36_setWindowImage;
            op[41] = vc37_pokePalette;
            op[51] = vc38_ifVarNotZero;
            op[52] = vc39_setVar;
            op[53] = vc40_scrollRight;
            op[54] = vc41_scrollLeft;
            op[56] = vc42_delayIfNotEQ;
        }

        protected override void DrawIcon(WindowBlock window, int icon, int x, int y)
        {
            _videoLockOut |= 0x8000;

            LockScreen(screen =>
            {
                BytePtr src;
                var dst = screen.Pixels;

                dst += (x + window.x) * 8;
                dst += (y * 8 + window.y) * screen.Pitch;

                if (Features.HasFlag(GameFeatures.GF_PLANAR))
                {
                    src = _iconFilePtr;
                    src += src.ToUInt16BigEndian(icon * 2);
                    DecompressIconPlanar(dst, src, 24, 12, 16, screen.Pitch);
                }
                else
                {
                    src = _iconFilePtr;
                    src += icon * 288;
                    DecompressIconPlanar(dst, src, 24, 12, 16, screen.Pitch, false);
                }
            });

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        protected override string GenSaveName(int slot)
        {
            return $"elvira1.{slot:D3}";
        }

        private void oe1_printMonsterHit()
        {
            // 278: print monster hit
            WindowBlock window = DummyWindow;
            window.flags = 1;

            MouseOff();
            WriteChar(window, 35, 166, 4, _variableArray[415]);
            MouseOn();
        }

        private void oe1_printPlayerHit()
        {
            // 277: print player hit
            WindowBlock window = DummyWindow;
            window.flags = 1;

            MouseOff();
            WriteChar(window, 3, 166, 0, _variableArray[414]);
            MouseOn();
        }

        private void oe1_pauseGame()
        {
            throw new NotImplementedException();
        }

        private void oe1_printMonsterDamage()
        {
            // 273: print monster damage
            WindowBlock window = DummyWindow;
            window.flags = 1;

            MouseOff();
            WriteChar(window, 36, 88, 2, _variableArray[242]);
            MouseOn();
        }

        private void oe1_printPlayerDamage()
        {
            // 272: print player damage
            WindowBlock window = DummyWindow;
            window.flags = 1;

            MouseOff();
            WriteChar(window, 36, 38, 2, _variableArray[241]);
            MouseOn();
        }

        private void oe1_stopTune()
        {
            // 271: stop tune
        }

        private void oe1_printStats()
        {
            // 270: print stats
            PrintStats();
        }

        private void oe1_zoneDisk()
        {
            // 267: set disk number of each zone
            GetVarOrWord();
            GetVarOrWord();
        }

        private void oe1_playTune()
        {
            // 264: play tune
            ushort music = (ushort) GetVarOrWord();
            ushort track = (ushort) GetVarOrWord();

            if (music != _lastMusicPlayed)
            {
                _lastMusicPlayed = (short) music;
                // No tune under water
                if (music == 4)
                {
                    StopMusic();
                }
                else
                {
                    PlayMusic(music, track);
                }
            }
        }

        private void oe1_enableInput()
        {
            // 258: enable input
            _variableArray[500] = 0;

            for (int i = 120; i != 130; i++)
                DisableBox(i);

            _verbHitArea = 0;
            _hitAreaSubjectItem = null;
            _hitAreaObjectItem = null;

            _dragFlag = false;
            _dragAccept = false;
            _dragCount = 0;
            _dragMode = false;

            _lastHitArea3 = null;
            _lastHitArea = null;

            _clickOnly = true;
        }

        private void oe1_bitTest()
        {
            // 253: bit test
            int var = (int) GetVarOrWord();
            int bit = (int) GetVarOrWord();

            SetScriptCondition((_variableArray[var] & (1 << bit)) != 0);
        }

        private void oe1_bitSet()
        {
            // 252: set bit on
            int var = (int) GetVarOrWord();
            int bit = (int) GetVarOrWord();

            WriteVariable((ushort) var, (ushort) (_variableArray[var] | (1 << bit)));
        }

        private void oe1_bitClear()
        {
            // 251: set bit off
            int var = (int) GetVarOrWord();
            int bit = (int) GetVarOrWord();

            WriteVariable((ushort) var, (ushort) (_variableArray[var] & ~(1 << bit)));
        }

        private void oe1_present()
        {
            // 2: present (here or carried)
            Item item = GetNextItemPtr();
            SetScriptCondition(item.parent == GetItem1ID() || item.parent == Me().parent);
        }

        private void oe1_notPresent()
        {
            // 3: not present (neither here nor carried)
            Item item = GetNextItemPtr();
            SetScriptCondition(item.parent != GetItem1ID() && item.parent != Me().parent);
        }

        private void oe1_worn()
        {
            // 4: worn
            Item item = GetNextItemPtr();
            SubObject subObject = (SubObject) FindChildOfType(item, ChildType.kObjectType);

            if (item.parent != GetItem1ID() || subObject == null)
                SetScriptCondition(false);
            else
                SetScriptCondition(subObject.objectFlags.HasFlag(SubObjectFlags.kOFWorn));
        }

        private void oe1_notWorn()
        {
            // 5: not worn
            Item item = GetNextItemPtr();
            SubObject subObject = (SubObject) FindChildOfType(item, ChildType.kObjectType);

            if (item.parent != GetItem1ID() || subObject == null)
                SetScriptCondition(false);
            else
                SetScriptCondition(!subObject.objectFlags.HasFlag(SubObjectFlags.kOFWorn));
        }

        protected void oe1_isNotAt()
        {
            // 9: parent is not
            Item item = GetNextItemPtr();
            SetScriptCondition(item.parent != GetNextItemID());
        }

        protected void oe1_sibling()
        {
            // 10: sibling
            Item item1 = GetNextItemPtr();
            Item item2 = GetNextItemPtr();
            SetScriptCondition(item1.parent == item2.parent);
        }

        protected void oe1_notSibling()
        {
            // 11: not sibling
            Item item1 = GetNextItemPtr();
            Item item2 = GetNextItemPtr();
            SetScriptCondition(item1.parent != item2.parent);
        }

        protected void oe1_isIn()
        {
            // 22: is in
            Item item1 = GetNextItemPtr();
            Item item2 = GetNextItemPtr();
            SetScriptCondition(Contains(item1, item2) != 0);
        }

        protected void oe1_isNotIn()
        {
            // 23: is not in
            Item item1 = GetNextItemPtr();
            Item item2 = GetNextItemPtr();
            SetScriptCondition(Contains(item1, item2) == 0);
        }

        protected void oe1_isPlayer()
        {
            // 30: is player
            SetScriptCondition(IsPlayer(GetNextItemPtr()));
        }

        protected void oe1_canPut()
        {
            // 37: can put
            Item item1 = GetNextItemPtr();
            Item item2 = GetNextItemPtr();
            SetScriptCondition(CanPlace(item1, item2) == 0);
        }

        private void oe1_create()
        {
            // 47: create
            SetItemParent(GetNextItemPtr(), DerefItem(Me().parent));
        }

        protected void oe1_copyof()
        {
            // 54: copy of
            Item item = GetNextItemPtr();
            uint tmp = GetVarOrByte();
            WriteNextVarContents((ushort) GetUserFlag(item, (int) tmp));
        }

        private void oe1_moveDirn()
        {
            // 76: move direction
            short d = (short) ReadVariable((ushort) GetVarOrWord());
            MoveDirn(Me(), (uint) d);
        }

        protected void oe1_copyfo()
        {
            // 55: copy fo
            uint tmp = GetNextVarContents();
            Item item = GetNextItemPtr();
            SetUserFlag(item, (int) GetVarOrByte(), (int) tmp);
        }

        protected void oe1_whatO()
        {
            // 57: what o
            int a = (int) GetVarOrWord();

            if (a == 1)
                _subjectItem = FindMaster(_scriptAdj1, _scriptNoun1);
            else
                _objectItem = FindMaster(_scriptAdj2, _scriptNoun2);
        }

        protected void oe1_weigh()
        {
            // 59: weight
            Item item = GetNextItemPtr();
            WriteNextVarContents((ushort) WeighUp(item));
        }

        private void oe1_setFF()
        {
            // 60: set FF
            WriteNextVarContents(255);
        }

        private void oe1_score()
        {
            // 90: score
            var p = (SubPlayer) FindChildOfType(Me(), ChildType.kPlayerType);
            ShowMessageFormat("Your score is %d.\n", p.score);
        }

        private void oe1_look()
        {
            // 96: look
            Item i = DerefItem(Me().parent);
            if (i == null)
                return;

            var r = (SubRoom) FindChildOfType(i, ChildType.kRoomType);
            var o = (SubObject) FindChildOfType(i, ChildType.kObjectType);
            var p = (SubPlayer) FindChildOfType(i, ChildType.kPlayerType);
            if (p == null)
                return;

            if ((o != null) && (r == null))
            {
                ShowMessageFormat("In the {0}\n", GetStringPtrById(i.itemName));
            }
            else if (p != null)
            {
                ShowMessageFormat("Carried by {0}\n", GetStringPtrById(i.itemName));
            }

            if (r != null)
            {
                ShowMessageFormat("{0}", GetStringPtrById(r.roomLong));
            }

            ShowMessageFormat("\n");

            Item l = DerefItem(i.child);
            if (l != null)
            {
                lobjFunc(l, "You can see "); /* Show objects */
            }
        }

        void oe1_doClass()
        {
            // 106: do class
            Item i = GetNextItemPtr();
            short cm = (short) GetVarOrWord();
            short num = (short) GetVarOrWord();

            _classMask = (short) ((cm != -1) ? 1 << cm : 0);
            _classLine = new SubroutineLine(_currentTable.Pointer + _currentLine.next);
            if (num == 1)
            {
                _subjectItem = FindInByClass(i, (short) (1 << cm));
                _classMode1 = (short) (_subjectItem != null ? 1 : 0);
            }
            else
            {
                _objectItem = FindInByClass(i, (short) (1 << cm));
                _classMode2 = (short) (_objectItem != null ? 1 : 0);
            }
        }

        private void oe1_pObj()
        {
            // 112: print object name
            SubObject subObject = (SubObject) FindChildOfType(GetNextItemPtr(), ChildType.kObjectType);
            GetVarOrWord();

            if (subObject != null)
                ShowMessageFormat("{0}", GetStringPtrById(subObject.objectName));
        }

        protected void oe1_pName()
        {
            // 114: print item name
            Item i = GetNextItemPtr();
            ShowMessageFormat("{0}", GetStringPtrById(i.itemName));
        }

        protected void oe1_pcName()
        {
            // 115: print item case (and change first letter to upper case)
            Item i = GetNextItemPtr();
            ShowMessageFormat("{0}", GetStringPtrById(i.itemName, true));
        }

        private void oe1_isCalled()
        {
            // 135: childstruct fr2 is
            Item item = GetNextItemPtr();
            uint stringId = GetNextStringID();
            SetScriptCondition(
                string.Equals(GetStringPtrById(item.itemName), GetStringPtrById((ushort) stringId),
                    StringComparison.OrdinalIgnoreCase));
        }

        private void oe1_cFlag()
        {
            // 162: check container flag
            SubContainer c = (SubContainer) FindChildOfType(GetNextItemPtr(), ChildType.kContainerType);
            uint bit = GetVarOrWord();

            if (c == null)
                SetScriptCondition(false);
            else
                SetScriptCondition((c.flags & (1 << (int) bit)) != 0);
        }

        protected void oe1_rescan()
        {
            // 164: restart subroutine
            SetScriptReturn(-10);
        }

        void oe1_setUserItem()
        {
            // 176: set user item
            Item i = GetNextItemPtr();
            uint tmp = GetVarOrWord();
            SetUserItem(i, (int) tmp, GetNextItemID());
        }

        void oe1_getUserItem()
        {
            // 177: get user item
            Item i = GetNextItemPtr();
            int n = (int) GetVarOrWord();

            if (GetVarOrWord() == 1)
                _subjectItem = DerefItem((uint) GetUserItem(i, n));
            else
                _objectItem = DerefItem((uint) GetUserItem(i, n));
        }

        void oe1_clearUserItem()
        {
            // 178: clear user item
            Item i = GetNextItemPtr();
            uint tmp = GetVarOrWord();
            SetUserItem(i, (int) tmp, 0);
        }

        void oe1_whereTo()
        {
            // 180: where to
            Item i = GetNextItemPtr();
            short d = (short) GetVarOrWord();
            short f = (short) GetVarOrWord();

            if (f == 1)
                _subjectItem = GetExitOf_e1(i, (ushort) d);
            else
                _objectItem = GetExitOf_e1(i, (ushort) d);
        }

        private void oe1_doorExit()
        {
            // 181: door exit
            Item a = null;
            Item i = GetNextItemPtr();
            Item d = GetNextItemPtr();
            short f = (short) GetVarOrWord();
            short ct = 0;

            var c = (SubChain) FindChildOfType(d, ChildType.kChainType);
            if (c != null)
                a = DerefItem(c.chChained);
            while (ct < 6)
            {
                var x = GetDoorOf(i, (ushort) ct);
                if ((x == d) | (x == a))
                {
                    WriteVariable((ushort) f, (ushort) ct);
                    return;
                }
                ct++;
            }
            WriteVariable((ushort) f, 255);
        }

        protected void oe1_stopAnimate()
        {
            // 227: stop animate
            StopAnimate((ushort) GetVarOrWord());
        }

        private void oe1_menu()
        {
            // 233: agos menu
            uint b = GetVarOrWord();
            uint a = GetVarOrWord();
            DrawMenuStrip(a, b);
        }

        private void oe1_addBox()
        {
            // 235: add item box
            BoxFlags flags = 0;
            uint id = GetVarOrWord();
            uint @params = id / 1000;
            uint x, y, w, h, verb;

            id = id % 1000;

            if ((@params & 1) != 0)
                flags |= BoxFlags.kBFInvertTouch;
            if ((@params & 2) != 0)
                flags |= BoxFlags.kBFInvertSelect;
            if ((@params & 4) != 0)
                flags |= BoxFlags.kBFBoxItem;
            if ((@params & 8) != 0)
                flags |= BoxFlags.kBFToggleBox;
            if ((@params & 16) != 0)
                flags |= BoxFlags.kBFDragBox;

            x = GetVarOrWord();
            y = GetVarOrWord();
            w = GetVarOrWord();
            h = GetVarOrWord();
            var item = GetNextItemPtrStrange();
            verb = GetVarOrWord();
            if (x >= 1000)
            {
                verb += 0x4000;
                x -= 1000;
            }
            DefineBox((int) id, (int) x, (int) y, (int) w, (int) h, (int) flags, (int) verb, item);
        }

        protected void oe1_loadGame()
        {
            // 202: load restart state
            ushort stringId = (ushort) GetNextStringID();
            LoadGame(GetStringPtrById(stringId), true);
        }

        protected void oe1_findMaster()
        {
            // 219: find master
            short ad, no;
            short d = (short) GetVarOrByte();

            ad = (d == 1) ? _scriptAdj1 : _scriptAdj2;
            no = (d == 1) ? _scriptNoun1 : _scriptNoun2;

            d = (short) GetVarOrByte();
            if (d == 1)
                _subjectItem = FindMaster(ad, no);
            else
                _objectItem = FindMaster(ad, no);
        }

        protected void oe1_nextMaster()
        {
            // 220: next master
            short ad, no;
            Item item = GetNextItemPtr();
            short d = (short) GetVarOrByte();

            ad = (d == 1) ? _scriptAdj1 : _scriptAdj2;
            no = (d == 1) ? _scriptNoun1 : _scriptNoun2;

            d = (short) GetVarOrByte();
            if (d == 1)
                _subjectItem = NextMaster(item, ad, no);
            else
                _objectItem = NextMaster(item, ad, no);
        }

        protected void oe1_animate()
        {
            // 226: animate
            ushort vgaSpriteId = (ushort) GetVarOrWord();
            ushort windowNum = (ushort) GetVarOrByte();
            short x = (short) GetVarOrWord();
            short y = (short) GetVarOrWord();
            ushort palette = (ushort) GetVarOrWord();

            _videoLockOut |= 0x40;
            Animate(windowNum, (ushort) (vgaSpriteId / 100), vgaSpriteId, x, y, palette);
            _videoLockOut = (ushort) (_videoLockOut & ~0x40);
        }

        protected void oe1_setTime()
        {
            // 259: set time
            _timeStore = GetTime();
        }

        protected void oe1_ifTime()
        {
            // 260: if time
            uint a = GetVarOrWord();
            uint t = GetTime() - a;
            SetScriptCondition(t >= _timeStore);
        }

        protected void oe2_pauseGame()
        {
            // 135: pause game
            uint pauseTime = GetTime();
            HaltAnimation();

            while (!HasToQuit)
            {
                _lastHitArea = null;
                _lastHitArea3 = null;

                while (!HasToQuit)
                {
                    if (ProcessSpecialKeys() || _lastHitArea3 != null)
                        break;
                    Delay(1);
                }

                var ha = _lastHitArea;

                if (ha == null)
                {
                }
                else if (ha.id == 201)
                {
                    break;
                }
            }

            RestartAnimation();
            _gameStoppedClock = GetTime() - pauseTime + _gameStoppedClock;
        }

        private bool IsPlayer(Item item)
        {
            return FindChildOfType(item, ChildType.kPlayerType) != null;
        }

        private int Contains(Item a, Item b)
        {
            while (DerefItem(b.parent) != null)
            {
                if (DerefItem(b.parent) == a)
                    return 1;
                b = DerefItem(b.parent);
            }

            return 0;
        }

        protected override void ExecuteOpcode(int opcode)
        {
            _opcodes[opcode]();
        }

        private void lobjFunc(Item i, string f)
        {
            var n = 0;

            while (i != null)
            {
                var o = (SubObject) FindChildOfType(i, ChildType.kObjectType);
                if ((o != null) && o.objectFlags.HasFlag((SubObjectFlags) 1))
                    goto l1;
                if (i == Me())
                    goto l1;
                if (n == 0)
                {
                    if (!string.IsNullOrEmpty(f))
                        ShowMessageFormat("{0}", f);
                    n = 1;
                }
                else
                {
                    if (MoreText(i) != 0)
                        ShowMessageFormat(", ");
                    else
                        ShowMessageFormat(" and ");
                }
                ShowMessageFormat("{0}", GetStringPtrById(i.itemName));
                l1:
                i = DerefItem(i.next);
            }
            if (!string.IsNullOrEmpty(f))
            {
                if (n == 1)
                    ShowMessageFormat(".\n");
            }
            else
            {
                if (n == 0)
                    ShowMessageFormat("nothing");
            }
        }

        private short MoreText(Item i)
        {
            i = DerefItem(i.next);

            while (i != null)
            {
                var o = (SubObject) FindChildOfType(i, ChildType.kObjectType);
                if ((o != null) && o.objectFlags.HasFlag((SubObjectFlags) 1))
                    goto l1;
                if (i != Me())
                    return 1;
                l1:
                i = DerefItem(i.next);
            }

            return 0;
        }
    }
}