//
//  AGOSEngine_Waxworks.cs
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
using System.Text;
using NScumm.Core;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    internal class AgosEngineWaxworks : AgosEngineElvira2
    {
        private readonly int[] _lineCounts = new int[6];
        private readonly BytePtr[] _linePtrs = new BytePtr[6];
        private readonly byte[] _boxBuffer = new byte[310];
        private Dictionary<int, Action> _opcodes;
        private BytePtr _boxBufferPtr;
        private int _boxLineCount;
        private bool _boxCr;

        public AgosEngineWaxworks(ISystem system, GameSettings settings, AgosGameDescription gd)
            : base(system, settings, gd)
        {
        }

        protected override void SetupOpcodes()
        {
            _opcodes = new Dictionary<int, Action>
            {
                {1, o_at},
                {2, o_notAt},
                {5, o_carried},
                {6, o_notCarried},
                {7, o_isAt},
                {8, oe1_isNotAt},
                {9, oe1_sibling},
                {10, oe1_notSibling},
                {11, o_zero},
                {12, o_notZero},
                {13, o_eq},
                {14, o_notEq},
                {15, o_gt},
                {16, o_lt},
                {17, o_eqf},
                {18, o_notEqf},
                {19, o_ltf},
                {20, o_gtf},
                {21, oe1_isIn},
                {22, oe1_isNotIn},
                {23, o_chance},
                {24, oe1_isPlayer},
                {25, o_isRoom},
                {26, o_isObject},
                {27, o_state},
                {28, o_oflag},
                {29, oe1_canPut},
                {31, o_destroy},
                {33, o_place},
                {34, oe1_copyof},
                {35, oe1_copyfo},
                {36, o_copyff},
                {37, oe1_whatO},
                {39, oe1_weigh},
                {41, o_clear},
                {42, o_let},
                {43, o_add},
                {44, o_sub},
                {45, o_addf},
                {46, o_subf},
                {47, o_mul},
                {48, o_div},
                {49, o_mulf},
                {50, o_divf},
                {51, o_mod},
                {52, o_modf},
                {53, o_random},
                {54, oe2_moveDirn},
                {55, oww_goto},
                {56, o_oset},
                {57, o_oclear},
                {58, o_putBy},
                {59, o_inc},
                {60, o_dec},
                {61, o_setState},
                {62, o_print},
                {63, o_message},
                {64, o_msg},
                {65, oww_addTextBox},
                {66, oww_setShortText},
                {67, oww_setLongText},
                {68, o_end},
                {69, o_done},
                {71, o_process},
                {76, o_when},
                {77, o_if1},
                {78, o_if2},
                {79, o_isCalled},
                {80, o_is},
                {82, o_debug},
                {83, oe1_rescan},
                {85, oww_whereTo},
                {87, o_comment},
                {89, oe1_loadGame},
                {90, o_getParent},
                {91, o_getNext},
                {92, o_getChildren},
                {94, oe1_findMaster},
                {95, oe1_nextMaster},
                {96, o_picture},
                {97, o_loadZone},
                {98, oe1_animate},
                {99, oe1_stopAnimate},
                {100, o_killAnimate},
                {101, o_defWindow},
                {102, o_window},
                {103, o_cls},
                {104, o_closeWindow},
                {105, oe2_menu},
                {106, oww_textMenu},
                {107, o_addBox},
                {108, o_delBox},
                {109, o_enableBox},
                {110, o_disableBox},
                {111, o_moveBox},
                {114, o_doIcons},
                {115, o_isClass},
                {116, o_setClass},
                {117, o_unsetClass},
                {119, o_waitSync},
                {120, o_sync},
                {121, o_defObj},
                {125, o_here},
                {126, o_doClassIcons},
                {127, o_playTune},
                {130, o_setAdjNoun},
                {132, o_saveUserGame},
                {133, o_loadUserGame},
                {135, oww_pauseGame},
                {136, o_copysf},
                {137, o_restoreIcons},
                {138, o_freezeZones},
                {139, o_placeNoIcons},
                {140, o_clearTimers},
                {141, o_setDollar},
                {142, o_isBox},
                {143, oe2_doTable},
                {144, oe2_setDoorOpen},
                {145, oe2_setDoorClosed},
                {146, oe2_setDoorLocked},
                {147, oe2_setDoorClosed},
                {148, oe2_ifDoorOpen},
                {149, oe2_ifDoorClosed},
                {150, oe2_ifDoorLocked},
                {151, oe2_storeItem},
                {152, oe2_getItem},
                {153, oe2_bSet},
                {154, oe2_bClear},
                {155, oe2_bZero},
                {156, oe2_bNotZero},
                {157, oe2_getOValue},
                {158, oe2_setOValue},
                {160, oe2_ink},
                {175, oe2_getDollar2},
                {179, oe2_isAdjNoun},
                {180, oe2_b2Set},
                {181, oe2_b2Clear},
                {182, oe2_b2Zero},
                {183, oe2_b2NotZero},
                {184, oww_boxMessage},
                {185, oww_boxMsg},
                {186, oww_boxLongText},
                {187, oww_printBox},
                {188, oww_boxPObj},
                {189, oww_lockZones},
                {190, oww_unlockZones}
            };
            _numOpcodes = 191;
        }

        protected override void ExecuteOpcode(int opcode)
        {
            var op = _opcodes[opcode];
            op();
        }

        protected override void SetupVideoOpcodes(Action[] op)
        {
            Debug("AGOSEngine_Waxworks::setupVideoOpcodes");
            base.SetupVideoOpcodes(op);

            op[58] = vc58_checkCodeWheel;
            op[60] = vc60_stopAnimation;
            op[61] = vc61;
            op[62] = vc62_fastFadeOut;
            op[63] = vc63_fastFadeIn;
        }

        protected override void SetupGame()
        {
            gss = Simon1Settings;
            _numVideoOpcodes = 64;
            _vgaMemSize = 1000000;
            _itemMemSize = 80000;
            _tableMemSize = 50000;
            _frameCount = 4;
            _vgaBaseDelay = 1;
            _vgaPeriod = 50;
            _numBitArray1 = 16;
            _numBitArray2 = 15;
            _numItemStore = 50;
            _numTextBoxes = 10;
            _numVars = 255;

            _numMusic = 26;
            _numZone = 155;

            SetupGameCore();
        }

        protected override void AddArrows(WindowBlock window, byte num)
        {
            var h = FindEmptyHitArea();
            var ha = h.Value;
            _scrollUpHitArea = (ushort) h.Offset;

            SetBitFlag(22, true);
            ha.x = 255;
            ha.y = 153;
            ha.width = 9;
            ha.height = 11;
            ha.flags = BoxFlags.kBFBoxInUse | BoxFlags.kBFNoTouchName;
            ha.id = 0x7FFB;
            ha.priority = 100;
            ha.window = window;
            ha.verb = 1;

            h = FindEmptyHitArea();
            ha = h.Value;
            _scrollDownHitArea = (ushort) h.Offset;

            ha.x = 255;
            ha.y = 170;
            ha.width = 9;
            ha.height = 11;
            ha.flags = BoxFlags.kBFBoxInUse | BoxFlags.kBFNoTouchName;
            ha.id = 0x7FFC;
            ha.priority = 100;
            ha.window = window;
            ha.verb = 1;
            SetWindowImageEx(6, 103);
        }

        protected override void RemoveArrows(WindowBlock window, int num)
        {
            SetBitFlag(22, false);
            SetWindowImageEx(6, 103);
        }

        protected override string GenSaveName(int slot)
        {
            if (GamePlatform == Platform.DOS)
                return $"waxworks-pc.{slot:D3}";
            return $"waxworks.{slot:D3}";
        }

        protected override bool ConfirmOverWrite(WindowBlock window)
        {
            var sub = GetSubroutineByID(80);
            if (sub != null)
                StartSubroutineEx(sub);

            if (_variableArray[253] == 0)
                return true;

            return false;
        }

        protected override void MoveDirn(Item i, uint x)
        {
            if (i.parent == 0)
                return;

            ushort n = GetExitOf(DerefItem(i.parent), (ushort) x);
            if (DerefItem(n) == null)
            {
                LoadRoomItems(n);
                n = GetExitOf(DerefItem(i.parent), (ushort) x);
            }

            var d = DerefItem(n);
            if (d == null) return;

            n = GetDoorState(DerefItem(i.parent), (ushort) x);
            if (n != 1) return;

            if (CanPlace(i, d) == 0)
                SetItemParent(i, d);
        }

        private void oww_goto()
        {
            // 55: set itemA parent
            uint item = (uint) GetNextItemID();
            if (DerefItem(item) == null)
            {
                SetItemParent(Me(), null);
                LoadRoomItems((ushort) item);
            }
            SetItemParent(Me(), DerefItem(item));
        }

        protected void oww_addTextBox()
        {
            // 65: add hit area
            int id = (int) GetVarOrWord();
            int x = (int) GetVarOrWord();
            int y = (int) GetVarOrWord();
            int w = (int) GetVarOrWord();
            int h = (int) GetVarOrWord();
            int number = (int) GetVarOrByte();
            if (number < _numTextBoxes)
                DefineBox(id, x, y, w, h, (number << 8) + 129, 208, DummyItem2);
        }

        protected void oww_setShortText()
        {
            // 66: set item name
            uint var = GetVarOrByte();
            uint stringId = GetNextStringID();
            if (var < _numTextBoxes)
            {
                _shortText[var] = (ushort) stringId;
            }
        }

        protected void oww_setLongText()
        {
            // 67: set item description
            int var = (int) GetVarOrByte();
            uint stringId = GetNextStringID();
            if (Features.HasFlag(GameFeatures.GF_TALKIE))
            {
                uint speechId = (uint) GetNextWord();
                if (var < _numTextBoxes)
                {
                    _longText[var] = (ushort) stringId;
                    _longSound[var] = (ushort) speechId;
                }
            }
            else
            {
                if (var < _numTextBoxes)
                {
                    _longText[var] = (ushort) stringId;
                }
            }
        }

        protected void oww_printLongText()
        {
            // 70: show string from array
            var str = GetStringPtrById(_longText[GetVarOrByte()]);
            ShowMessageFormat("{0}\n", str);
        }

        private void oww_whereTo()
        {
            // 85: where to
            Item i = GetNextItemPtr();
            short d = (short) GetVarOrByte();
            short f = (short) GetVarOrByte();

            if (f == 1)
                _subjectItem = DerefItem(GetExitOf(i, (ushort) d));
            else
                _objectItem = DerefItem(GetExitOf(i, (ushort) d));
        }

        protected void oww_lockZones()
        {
            // 189: lock zone
            _vgaMemBase = _vgaMemPtr;
        }

        protected void oww_unlockZones()
        {
            // 190: unlock zone
            _vgaMemPtr = _vgaFrozenBase;
            _vgaMemBase = _vgaFrozenBase;
        }

        private void oww_textMenu()
        {
            // 106: set text menu
            byte slot = (byte) GetVarOrByte();
            TextMenu[slot] = (byte) GetVarOrByte();
        }

        private void oww_boxMessage()
        {
            // 184: print message to box
            BoxTextMessage(GetStringPtrById((ushort) GetNextStringID()));
        }

        private void oww_boxMsg()
        {
            // 185: print msg to box
            BoxTextMsg(GetStringPtrById((ushort) GetNextStringID()));
        }

        private void oww_boxLongText()
        {
            // 186: print long text to box
            BoxTextMsg(GetStringPtrById(_longText[GetVarOrByte()]));
        }

        private void oww_printBox()
        {
            // 187: print box
            PrintBox();
        }

        private void oww_boxPObj()
        {
            // 188: print object name to box
            var subObject = (SubObject) FindChildOfType(GetNextItemPtr(), ChildType.kObjectType);

            if (subObject != null && subObject.objectFlags.HasFlag(SubObjectFlags.kOFText))
                BoxTextMsg(GetStringPtrById((ushort) subObject.objectFlagValue[0]));
        }

        private void oww_pauseGame()
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
                    if (_lastHitArea3 != null)
                        break;
                    Delay(1);
                }

                var ha = _lastHitArea;

                if (ha == null)
                {
                }
                else if (ha.id == 200)
                {
                    break;
                }
                else if (ha.id == 201)
                {
                    break;
                }
            }

            RestartAnimation();
            _gameStoppedClock = GetTime() - pauseTime + _gameStoppedClock;
        }

        protected override void DrawIcon(WindowBlock window, int icon, int x, int y)
        {
            _videoLockOut |= 0x8000;

            LockScreen(screen =>
            {
                var dst = screen.Pixels;

                dst += (x + window.x) * 8;
                dst += (y * 20 + window.y) * screen.Pitch;

                byte color = (byte) (dst[0] & 0xF0);
                if (GamePlatform == Platform.Amiga)
                {
                    BytePtr src = _iconFilePtr;
                    src.Offset += src.ToInt32BigEndian(icon * 4);
                    DecompressIconPlanar(dst, src, 24, 10, color, screen.Pitch);
                }
                else
                {
                    BytePtr src = _iconFilePtr;
                    src.Offset += src.ToUInt16(icon * 2);
                    DecompressIcon(dst, src, 24, 10, color, screen.Pitch);
                }

                _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
            });
        }

        protected override int SetupIconHitArea(WindowBlock window, uint num, int x, int y, Item itemPtr)
        {
            var h = FindEmptyHitArea();
            var ha = h.Value;
            ha.x = (ushort) ((x + window.x) * 8);
            ha.y = (ushort) (y * 20 + window.y);
            ha.itemPtr = itemPtr;
            ha.width = 24;
            ha.height = 20;
            ha.flags = BoxFlags.kBFDragBox | BoxFlags.kBFBoxInUse | BoxFlags.kBFBoxItem;
            ha.id = 0x7FFD;
            ha.priority = 100;
            ha.verb = 208;

            return h.Offset;
        }

        protected override void BoxController(uint x, uint y, uint mode)
        {
            Ptr<HitArea> ha = _hitAreas;
            int count = _hitAreas.Length;
            ushort priority = 0;
            ushort lx = (ushort) x;
            ushort ly = (ushort) y;

            if (GameType == SIMONGameType.GType_FF || GameType == SIMONGameType.GType_PP)
            {
                lx = (ushort) (lx + _scrollX);
                ly = (ushort) (ly + _scrollY);
            }
            else if (GameType == SIMONGameType.GType_SIMON2)
            {
                if (GetBitFlag(79) || y < 134)
                {
                    lx = (ushort) (lx + _scrollX * 8);
                }
            }

            HitArea bestHa = null;

            do
            {
                if (ha.Value.flags.HasFlag(BoxFlags.kBFBoxInUse))
                {
                    if (!ha.Value.flags.HasFlag(BoxFlags.kBFBoxDead))
                    {
                        if (lx >= ha.Value.x && ly >= ha.Value.y &&
                            lx - ha.Value.x < ha.Value.width && ly - ha.Value.y < ha.Value.height &&
                            priority <= ha.Value.priority)
                        {
                            priority = ha.Value.priority;
                            bestHa = ha.Value;
                        }
                        else
                        {
                            if (ha.Value.flags.HasFlag(BoxFlags.kBFBoxSelected))
                            {
                                HitareaLeave(ha.Value, true);
                                ha.Value.flags &= ~BoxFlags.kBFBoxSelected;
                            }
                        }
                    }
                    else
                    {
                        ha.Value.flags &= ~BoxFlags.kBFBoxSelected;
                    }
                }
                ha.Offset++;
            } while (--count != 0);

            _currentBoxNum = 0;
            _currentBox = bestHa;

            if (bestHa == null)
            {
                ClearName();
                if (GameType == SIMONGameType.GType_WW && _mouseCursor >= 4)
                {
                    _mouseCursor = 0;
                    _needHitAreaRecalc++;
                }
                return;
            }

            _currentBoxNum = bestHa.id;

            if (mode != 0)
            {
                if (mode == 3)
                {
                    if (bestHa.flags.HasFlag(BoxFlags.kBFDragBox))
                    {
                        _lastClickRem = bestHa;
                    }
                }
                else
                {
                    _lastHitArea = bestHa;
                    if (GameType == SIMONGameType.GType_PP)
                    {
                        _variableArray[400] = (short) x;
                        _variableArray[401] = (short) y;
                    }
                    else if (GameType == SIMONGameType.GType_SIMON1 || GameType == SIMONGameType.GType_SIMON2 ||
                             GameType == SIMONGameType.GType_FF)
                    {
                        _variableArray[1] = (short) x;
                        _variableArray[2] = (short) y;
                    }
                }
            }

            if ((GameType == SIMONGameType.GType_WW) && (_mouseCursor == 0 || _mouseCursor >= 4))
            {
                uint verb = (uint) (bestHa.verb & 0x3FFF);
                if (verb >= 239 && verb <= 242)
                {
                    uint cursor = verb - 235;
                    if (_mouseCursor != cursor)
                    {
                        _mouseCursor = (byte) cursor;
                        _needHitAreaRecalc++;
                    }
                }
            }

            if (GameType != SIMONGameType.GType_WW || !_nameLocked)
            {
                if (bestHa.flags.HasFlag(BoxFlags.kBFNoTouchName))
                {
                    ClearName();
                }
                else if (bestHa != _lastNameOn)
                {
                    DisplayName(bestHa);
                }
            }

            if (bestHa.flags.HasFlag(BoxFlags.kBFInvertTouch) && !bestHa.flags.HasFlag(BoxFlags.kBFBoxSelected))
            {
                HitareaLeave(bestHa, false);
                bestHa.flags |= BoxFlags.kBFBoxSelected;
            }
        }

        protected override bool LoadTablesIntoMem(ushort subrId)
        {
            var p = _tblList;
            if (p == BytePtr.Null)
                return false;

            while (p.Value != 0)
            {
                var filename = new StringBuilder();
                while (p.Value != 0)
                {
                    filename.Append((char) p.Value);
                    p.Offset++;
                }
                p.Offset++;

                if (_gd.Platform == Platform.Acorn)
                {
                    filename.Append(".DAT");
                }

                for (;;)
                {
                    uint minNum = p.ToUInt16BigEndian();
                    p.Offset += 2;
                    if (minNum == 0)
                        break;

                    uint maxNum = p.ToUInt16BigEndian();
                    p.Offset += 2;

                    if (subrId < minNum || subrId > maxNum) continue;

                    _subroutineList = _subroutineListOrg;
                    _tablesHeapPtr = _tablesHeapPtrOrg;
                    _tablesHeapCurPos = _tablesHeapCurPosOrg;
                    _stringIdLocalMin = 1;
                    _stringIdLocalMax = 0;

                    var @in = OpenTablesFile(filename.ToString());
                    ReadSubroutineBlock(@in);
                    CloseTablesFile(@in);
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                    {
                        // TODO: vs
//                        _sound.LoadSfxTable(GetFileName(GameFileTypes.GAME_GMEFILE),
//                            _gameOffsetsPtr[atoi(filename + 6) - 1 + _soundIndexBase]);
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                             _gd.Platform == Platform.Windows)
                    {
                        filename[0] = 'S';
                        filename[1] = 'F';
                        filename[2] = 'X';
                        filename[3] = 'X';
                        filename[4] = 'X';
                        filename[5] = 'X';
                        var tmp = filename.ToString().Substring(6);
                        if (int.Parse(tmp) != 1 && int.Parse(tmp) != 30)
                            _sound.ReadSfxFile(filename.ToString());
                    }

                    AlignTableMem();

                    _tablesheapPtrNew = _tablesHeapPtr;
                    _tablesHeapCurPosNew = _tablesHeapCurPos;

                    if (_tablesHeapCurPos > _tablesHeapSize)
                        Error("loadTablesIntoMem: Out of table memory");
                    return true;
                }
            }

            Debug(1, "loadTablesIntoMem: didn't find {0}", subrId);
            return false;
        }

        private void BoxTextMessage(string x)
        {
            _boxBufferPtr = $"{x}\n".GetBytes();
            _lineCounts[_boxLineCount] += x.Length;
            _boxBufferPtr += x.Length + 1;
            _boxLineCount++;
            _linePtrs[_boxLineCount] = _boxBufferPtr;
            _boxCr = true;
        }

        private void BoxTextMsg(string x)
        {
            _boxBufferPtr = x.GetBytes();
            _lineCounts[_boxLineCount] += x.Length;
            _boxBufferPtr += x.Length;
            _boxCr = false;
        }

        private void PrintBox()
        {
            _boxBufferPtr = string.Empty.GetBytes();
            _linePtrs[0] = _boxBuffer;
            if (_boxCr == false)
                _boxLineCount++;
            StopAnimate(105);
            var boxSize = GetBoxSize();
            _variableArray[53] = (short) boxSize;
            Animate(3, 1, 100, 0, 0, 0);
            ChangeWindow(5);

            switch (boxSize)
            {
                case 1:
                    _textWindow.x = 10;
                    _textWindow.y = 163;
                    _textWindow.width = 20;
                    _textWindow.height = 1;
                    _textWindow.textMaxLength = 26;
                    break;
                case 2:
                    _textWindow.x = 8;
                    _textWindow.y = 160;
                    _textWindow.width = 24;
                    _textWindow.height = 2;
                    _textWindow.textMaxLength = 32;
                    break;
                case 3:
                    _textWindow.x = 6;
                    _textWindow.y = 156;
                    _textWindow.width = 28;
                    _textWindow.height = 3;
                    _textWindow.textMaxLength = 37;
                    break;
                case 4:
                    _textWindow.x = 4;
                    _textWindow.y = 153;
                    _textWindow.width = 32;
                    _textWindow.height = 4;
                    _textWindow.textMaxLength = 42;
                    break;
                case 5:
                    _textWindow.x = 2;
                    _textWindow.y = 150;
                    _textWindow.width = 36;
                    _textWindow.height = 5;
                    _textWindow.textMaxLength = 48;
                    break;
                default:
                    _textWindow.x = 1;
                    _textWindow.y = 147;
                    _textWindow.width = 38;
                    _textWindow.height = 6;
                    _textWindow.textMaxLength = 50;
                    break;
            }
            _textWindow.textColumn = 0;
            _textWindow.textRow = 0;
            _textWindow.textColumnOffset = 0;
            _textWindow.textLength = 0;
            JustifyStart();
            WaitForSync(99);
            _boxBufferPtr = _boxBuffer;
            while (_boxBufferPtr.Value != 0)
            {
                JustifyOutPut(_boxBufferPtr.Value);
                _boxBufferPtr.Offset++;
            }
            _boxLineCount = 0;
            _boxBufferPtr = _boxBuffer;
            _lineCounts[0] = 0;
            _lineCounts[1] = 0;
            _lineCounts[2] = 0;
            _lineCounts[3] = 0;
            _lineCounts[4] = 0;
            _lineCounts[5] = 0;
            ChangeWindow(0);
        }

        // Waxworks specific
        private ushort GetBoxSize()
        {
            switch (_boxLineCount)
            {
                case 1:
                    var x = _lineCounts[0];
                    if (x <= 26)
                        return 1;
                    if (x <= 64)
                        if (CheckFit(_linePtrs[0], 32, 2))
                            return 2;
                    if (x <= 111)
                        if (CheckFit(_linePtrs[0], 37, 3))
                            return 3;
                    if (x <= 168)
                        if (CheckFit(_linePtrs[0], 42, 4))
                            return 4;
                    if (x <= 240)
                        if (CheckFit(_linePtrs[0], 48, 5))
                            return 5;
                    return 6;
                case 2:
                    if (_lineCounts[0] <= 32)
                    {
                        if (_lineCounts[1] <= 32)
                            return 2;
                        if (_lineCounts[1] <= 74)
                            if (CheckFit(_linePtrs[1], 37, 2))
                                return 3;
                        if (_lineCounts[1] <= 126)
                            if (CheckFit(_linePtrs[1], 42, 3))
                                return 4;
                        if (_lineCounts[1] <= 172)
                            if (CheckFit(_linePtrs[1], 48, 4))
                                return 5;
                        return 6;
                    }
                    if ((_lineCounts[0] <= 74) && (CheckFit(_linePtrs[0], 37, 2)))
                    {
                        if (_lineCounts[1] <= 37)
                            return 3;
                        if (_lineCounts[1] <= 84)
                            if (CheckFit(_linePtrs[1], 42, 2))
                                return 4;
                        if (_lineCounts[1] <= 144)
                            if (CheckFit(_linePtrs[1], 48, 3))
                                return 5;
                        return 6;
                    }
                    if ((_lineCounts[0] <= 126) && (CheckFit(_linePtrs[0], 42, 3)))
                    {
                        if (_lineCounts[1] <= 42)
                            return 4;
                        if (_lineCounts[1] <= 84)
                            if (CheckFit(_linePtrs[1], 48, 2))
                                return 5;
                        return 6;
                    }
                    if ((_lineCounts[0] <= 192) && (CheckFit(_linePtrs[0], 48, 4)))
                    {
                        if (_lineCounts[1] <= 48)
                            return 5;
                        return 6;
                    }
                    return 6;
                case 3:
                    if (_lineCounts[0] <= 37)
                    {
                        if (_lineCounts[1] <= 37)
                        {
                            if (_lineCounts[2] <= 37)
                                return 3;
                            if (_lineCounts[2] <= 84)
                                if (CheckFit(_linePtrs[2], 42, 2))
                                    return 4;
                            if (_lineCounts[2] <= 144)
                                if (CheckFit(_linePtrs[2], 48, 3))
                                    return 5;
                            return 6;
                        }
                        if ((_lineCounts[1] <= 84) && (CheckFit(_linePtrs[1], 42, 2)))
                        {
                            if (_lineCounts[2] <= 42)
                                return 4;
                            if (_lineCounts[2] <= 96)
                                if (CheckFit(_linePtrs[2], 48, 2))
                                    return 5;
                            return 6;
                        }
                        if ((_lineCounts[1] <= 144) && (CheckFit(_linePtrs[1], 48, 3)))
                        {
                            if (_lineCounts[2] <= 48)
                                return 5;
                            return 6;
                        }
                        return 6;
                    }
                    if ((_lineCounts[0] <= 84) && (CheckFit(_linePtrs[0], 42, 2)))
                    {
                        if (_lineCounts[1] <= 42)
                        {
                            if (_lineCounts[2] <= 42)
                                return 4;
                            if (_lineCounts[2] <= 96)
                                if (CheckFit(_linePtrs[2], 48, 2))
                                    return 5;
                            return 6;
                        }
                        if ((_lineCounts[1] <= 96) && (CheckFit(_linePtrs[1], 48, 2)))
                        {
                            if (_lineCounts[2] <= 48)
                                return 5;
                            return 6;
                        }
                        return 6;
                    }
                    if ((_lineCounts[0] <= 96) && (CheckFit(_linePtrs[0], 48, 3)))
                    {
                        if (_lineCounts[1] <= 48)
                        {
                            if (_lineCounts[2] <= 48)
                                return 5;
                        }
                        return 6;
                    }
                    return 6;
                case 4:
                    if (_lineCounts[0] <= 42)
                    {
                        if (_lineCounts[1] <= 42)
                        {
                            if (_lineCounts[2] <= 42)
                            {
                                if (_lineCounts[3] <= 42)
                                    return 4;
                                if (_lineCounts[3] <= 96)
                                    if (CheckFit(_linePtrs[3], 48, 2))
                                        return 5;
                                return 6;
                            }
                            if ((_lineCounts[2] <= 96) && (CheckFit(_linePtrs[2], 48, 2)))
                                if (_lineCounts[3] <= 48)
                                    return 5;
                            return 6;
                        }
                        if ((_lineCounts[1] <= 96) && (CheckFit(_linePtrs[1], 48, 2)))
                            if ((_lineCounts[2] <= 48) && (_lineCounts[3] <= 48))
                                return 5;
                        return 6;
                    }
                    if ((_lineCounts[0] <= 96) && (CheckFit(_linePtrs[0], 48, 2)))
                        if ((_lineCounts[1] <= 48) && (_lineCounts[2] <= 48) && (_lineCounts[3] <= 48))
                            return 5;
                    return 6;
                case 5:
                    if ((_lineCounts[0] > 48) || (_lineCounts[1] > 48) || (_lineCounts[2] > 48)
                        || (_lineCounts[3] > 48) || (_lineCounts[4] > 48))
                        return 6;
                    return 5;
                default:
                    return 6;
            }
        }

        private static bool CheckFit(BytePtr ptr, int width, int lines)
        {
            int countw = 0;
            int countl = 0;
            var x = BytePtr.Null;
            while (ptr.Value != 0)
            {
                if (ptr.Value == '\n')
                    return true;
                if (countw == width)
                {
                    countl++;
                    countw = 0;
                    ptr = x;
                }
                if (ptr.Value == ' ')
                {
                    x = ptr;
                    x.Offset++;
                }
                countw++;
                if (countl == lines)
                    return false;
                ptr.Offset++;
            }

            return true;
        }
    }
}