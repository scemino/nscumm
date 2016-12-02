//
//  AGOSEngine_Simon1.cs
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
using System.IO;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    internal class AgosEngineSimon1 : AGOSEngine_Waxworks
    {
        private Dictionary<int, Action> _opcodes;

        private static readonly GameSpecificSettings Simon1Settings =
            new GameSpecificSettings
            {
                base_filename = string.Empty, // base_filename
                restore_filename = string.Empty, // restore_filename
                tbl_filename = string.Empty, // tbl_filename
                effects_filename = "EFFECTS", // effects_filename
                speech_filename = "SIMON" // speech_filename
            };


        public AgosEngineSimon1(ISystem system, GameSettings settings, AGOSGameDescription gd)
            : base(system, settings, gd)
        {
        }

        protected override void ExecuteOpcode(int opcode)
        {
            Debug($"ExecuteOpcode({opcode})");
            if (_opcodes.ContainsKey(opcode))
            {
                _opcodes[opcode]();
                return;
            }
            o_invalid();
        }

        protected override void UserGame(bool load)
        {
            Array.Clear(_saveBuf, 0, _saveBuf.Length);
            var maxChar = (_language == Language.HE_ISR) ? 155 : 128;

            _saveOrLoad = load;

            var saveTime = GetTime();

            var numSaveGames = CountSaveGames();
            if (!load)
                numSaveGames++;
            numSaveGames -= 6;
            if (numSaveGames < 0)
                numSaveGames = 0;
            numSaveGames++;
            _numSaveGameRows = (ushort) numSaveGames;

            _saveLoadRowCurPos = 1;
            if (!load)
                _saveLoadRowCurPos = (ushort) numSaveGames;

            _saveLoadEdit = false;

            restart:
            bool b;
            var i = UserGameGetKey(out b, maxChar);

            if (i == 205)
                goto get_out;
            if (!load)
            {
                // if_1
                if_1:
                var result = i;

                DisableBox(208 + i);
                LeaveHitAreaById(208 + i);

                var window = _windowArray[5];

                window.textRow = (short) result;

                // init x offset with a 2 character savegame number + a period (18 pix)
                if (_language == Language.HE_ISR)
                {
                    window.textColumn = 3;
                    window.textColumnOffset = 6;
                }
                else
                {
                    window.textColumn = 2;
                    window.textColumnOffset = 2;
                }
                window.textLength = 3;

                var name = new BytePtr(_saveBuf, i * 18);

                // now process entire savegame name to get correct x offset for cursor
                _saveGameNameLen = 0;
                while (name[_saveGameNameLen] != 0)
                {
                    if (_language == Language.HE_ISR)
                    {
                        byte width = 6;
                        if (name[_saveGameNameLen] >= 64 && name[_saveGameNameLen] < 91)
                            width = _hebrewCharWidths[name[_saveGameNameLen] - 64];
                        window.textLength++;
                        window.textColumnOffset -= width;
                        if (window.textColumnOffset < width)
                        {
                            window.textColumnOffset += 8;
                            window.textColumn++;
                        }
                    }
                    else
                    {
                        window.textLength++;
                        window.textColumnOffset += 6;
                        if (name[_saveGameNameLen] == 'i' || name[_saveGameNameLen] == 'l')
                            window.textColumnOffset -= 2;
                        if (window.textColumnOffset >= 8)
                        {
                            window.textColumnOffset -= 8;
                            window.textColumn++;
                        }
                    }
                    _saveGameNameLen++;
                }

                while (!HasToQuit)
                {
                    WindowPutChar(window, 127);

                    _saveLoadEdit = true;

                    i = UserGameGetKey(out b, maxChar);

                    if (b)
                    {
                        if (i == 205)
                            goto get_out;
                        EnableBox(208 + result);
                        if (_saveLoadEdit)
                        {
                            UserGameBackSpace(_windowArray[5], 8);
                        }
                        goto if_1;
                    }

                    if (!_saveLoadEdit)
                    {
                        EnableBox(208 + result);
                        goto restart;
                    }

                    if (_language == Language.HE_ISR)
                    {
                        if (i >= 128)
                            i -= 64;
                        else if (i >= 32)
                            i = hebrewKeyTable[i - 32];
                    }

                    UserGameBackSpace(_windowArray[5], 8);
                    if (i == 10 || i == 13)
                    {
                        break;
                    }
                    else if (i == 8)
                    {
                        // do_backspace
                        if (_saveGameNameLen != 0)
                        {
                            byte m, x;

                            _saveGameNameLen--;
                            m = name[_saveGameNameLen];

                            if (_language == Language.HE_ISR)
                                x = 8;
                            else
                                x = (byte) ((name[_saveGameNameLen] == 'i' || name[_saveGameNameLen] == 'l') ? 1 : 8);

                            name[_saveGameNameLen] = 0;

                            UserGameBackSpace(_windowArray[5], x, m);
                        }
                    }
                    else if (i >= 32 && _saveGameNameLen != 17)
                    {
                        name[_saveGameNameLen++] = (byte) i;

                        WindowPutChar(_windowArray[5], (byte) i);
                    }
                }

                if (!SaveGame(_saveLoadRowCurPos + result, _saveBuf.GetRawText(result * 18)))
                    FileError(_windowArray[5], true);
            }
            else
            {
                if (!LoadGame(GenSaveName(_saveLoadRowCurPos + i)))
                    FileError(_windowArray[5], false);
            }

            get_out:
            DisableFileBoxes();

            _gameStoppedClock = GetTime() - saveTime + _gameStoppedClock;
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
                {23, o_chance},
                {25, o_isRoom},
                {26, o_isObject},
                {27, o_state},
                {28, o_oflag},
                {31, o_destroy},
                {33, o_place},
                {36, o_copyff},
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
                {55, o_goto},
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
                {70, oww_printLongText},
                {71, o_process},
                {76, o_when},
                {77, o_if1},
                {78, o_if2},
                {79, o_isCalled},
                {80, o_is},
                {82, o_debug},
                {83, oe1_rescan},
                {87, o_comment},
                {88, o_haltAnimation},
                {89, o_restartAnimation},
                {90, o_getParent},
                {91, o_getNext},
                {92, o_getChildren},
                {96, o_picture},
                {97, o_loadZone},
                {98, os1_animate},
                {99, oe1_stopAnimate},
                {100, o_killAnimate},
                {101, o_defWindow},
                {102, o_window},
                {103, o_cls},
                {104, o_closeWindow},
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
                {135, os1_pauseGame},
                {136, o_copysf},
                {137, o_restoreIcons},
                {138, o_freezeZones},
                {139, o_placeNoIcons},
                {140, o_clearTimers},
                {141, o_setDollar},
                {142, o_isBox},
                {143, oe2_doTable},
                {151, oe2_storeItem},
                {152, oe2_getItem},
                {153, oe2_bSet},
                {154, oe2_bClear},
                {155, oe2_bZero},
                {156, oe2_bNotZero},
                {157, oe2_getOValue},
                {158, oe2_setOValue},
                {160, oe2_ink},
                {161, os1_screenTextBox},
                {162, os1_screenTextMsg},
                {163, os1_playEffect},
                {164, oe2_getDollar2},
                {165, oe2_isAdjNoun},
                {166, oe2_b2Set},
                {167, oe2_b2Clear},
                {168, oe2_b2Zero},
                {169, oe2_b2NotZero},
                {175, oww_lockZones},
                {176, oww_unlockZones},
                {177, os1_screenTextPObj},
                {178, os1_getPathPosn},
                {179, os1_scnTxtLongText},
                {180, os1_mouseOn},
                {181, os1_mouseOff},
                {182, os1_loadBeard},
                {183, os1_unloadBeard},
                {184, os1_unloadZone},
                {185, os1_loadStrings},
                {186, os1_unfreezeZones},
                {187, os1_specialFade}
            };
            _numOpcodes = 188;
        }

        protected override void SetupVideoOpcodes(Action[] op)
        {
            base.SetupVideoOpcodes(op);

            op[11] = vc11_clearPathFinder;
            op[17] = vc17_setPathfinderItem;
            op[22] = vc22_setPalette;
            op[32] = vc32_copyVar;
            op[37] = vc37_addToSpriteY;
            op[48] = vc48_setPathFinder;
            op[59] = vc59_ifSpeech;
            op[60] = vc60_stopAnimation;
            op[61] = vc61_setMaskImage;
            op[62] = vc62_fastFadeOut;
            op[63] = vc63_fastFadeIn;
        }

        protected override string GenSaveName(int slot)
        {
            return $"simon1.{slot:D3}";
        }

        private int UserGameGetKey(out bool b, int maxChar)
        {
            b = true;

            if (!_saveLoadEdit)
            {
                ListSaveGames();
            }

            _keyPressed = new ScummInputState();

            while (!HasToQuit)
            {
                _lastHitArea = null;
                _lastHitArea3 = null;

                do
                {
                    var keyCode = _keyPressed.GetKeys().FirstOrDefault();
                    var ascii = keyCode == 0 ? '\0' : ToChar(keyCode);
                    if (_saveLoadEdit && ascii != 0 && ascii < maxChar)
                    {
                        b = false;
                        return ascii;
                    }
                    Delay(10);
                } while (_lastHitArea3 == null && !HasToQuit);

                var ha = _lastHitArea;
                if (ha == null || ha.id < 205)
                {
                }
                else if (ha.id == 205)
                {
                    return ha.id;
                }
                else if (ha.id == 206)
                {
                    if (_saveLoadRowCurPos != 1)
                    {
                        if (_saveLoadRowCurPos < 7)
                            _saveLoadRowCurPos = 1;
                        else
                            _saveLoadRowCurPos -= 6;

                        _saveLoadEdit = false;
                        ListSaveGames();
                    }
                }
                else if (ha.id == 207)
                {
                    if (_saveDialogFlag)
                    {
                        _saveLoadRowCurPos += 6;
                        if (_saveLoadRowCurPos >= _numSaveGameRows)
                            _saveLoadRowCurPos = _numSaveGameRows;

                        _saveLoadEdit = false;
                        ListSaveGames();
                    }
                }
                else if (ha.id < 214)
                {
                    return ha.id - 208;
                }
            }

            return 205;
        }

        protected override void InitMouse()
        {
            InitMouseCore();

            var src = new Ptr<ushort>(_common_mouseInfo);
            for (var i = 0; i < 16; i++)
            {
                for (var j = 0; j < 16; j++)
                {
                    if ((src[0] & (1 << (15 - j % 16))) == 0) continue;
                    if ((src[1] & (1 << (15 - j % 16))) != 0)
                    {
                        _mouseData[16 * i + j] = 1;
                    }
                    else
                    {
                        _mouseData[16 * i + j] = 0;
                    }
                }
                src.Offset += 2;
            }

            OSystem.GraphicsManager.SetCursor(_mouseData, 16, 16, new Point(), 0xFF);
        }

        protected override void HandleMouseMoved()
        {
            uint x;

            if (_mouseHideCount != 0)
            {
                OSystem.GraphicsManager.IsCursorVisible = false;
                return;
            }

            OSystem.GraphicsManager.IsCursorVisible = true;
            _mouse = OSystem.InputManager.GetMousePosition();

            if (_defaultVerb != 0)
            {
                uint id = 101;
                if (_mouse.Y >= 136)
                    id = 102;
                if (_defaultVerb != id)
                    ResetVerbs();
            }

            if (GameType == SIMONGameType.GType_FF)
            {
                if (GetBitFlag(99))
                {
                    // Oracle
                    if (_mouse.X >= 10 && _mouse.X <= 635 && _mouse.Y >= 5 && _mouse.Y <= 475)
                    {
                        SetBitFlag(98, true);
                    }
                    else
                    {
                        if (GetBitFlag(98))
                        {
                            _variableArray[254] = 63;
                        }
                    }
                }
                else if (GetBitFlag(88))
                {
                    // Close Up
                    if (_mouse.X >= 10 && _mouse.X <= 635 && _mouse.Y >= 5 && _mouse.Y <= 475)
                    {
                        SetBitFlag(87, true);
                    }
                    else
                    {
                        if (GetBitFlag(87))
                        {
                            _variableArray[254] = 75;
                        }
                    }
                }

                if (_rightButtonDown)
                {
                    _rightButtonDown = false;
                    SetVerb(null);
                }
            }
            else if (GameType == SIMONGameType.GType_SIMON2)
            {
                if (GetBitFlag(79))
                {
                    if (!_vgaVar9)
                    {
                        if (_mouse.X >= 315 || _mouse.X < 9)
                        {
                            _vgaVar9 = false;
                            goto get_out2;
                        }
                        _vgaVar9 = true;
                    }
                    if (_scrollCount == 0)
                    {
                        if (_mouse.X >= 315)
                        {
                            if (_scrollX != _scrollXMax)
                                _scrollFlag = 1;
                        }
                        else if (_mouse.X < 8)
                        {
                            if (_scrollX != 0)
                                _scrollFlag = -1;
                        }
                    }
                }
                else
                {
                    _vgaVar9 = false;
                }
                get_out2:
                ;
            }

            if (_mouse != _mouseOld)
                _needHitAreaRecalc++;

            if (_leftButtonOld == 0 && _leftButtonCount != 0)
            {
                BoxController((uint) _mouse.X, (uint) _mouse.Y, 3);
            }
            _leftButtonOld = _leftButton;

            x = 0;
            if (_lastHitArea3 == null && _leftButtonDown)
            {
                _leftButtonDown = false;
                x = 1;
            }
            else
            {
                if (!_litBoxFlag && _needHitAreaRecalc == 0)
                    goto get_out;
            }

            BoxController((uint) _mouse.X, (uint) _mouse.Y, x);
            _lastHitArea3 = _lastHitArea;
            if (x == 1 && _lastHitArea == null)
                _lastHitArea3 = HitArea.None;

            get_out:
            _mouseOld = _mouse;
            DrawMousePointer();

            _needHitAreaRecalc = 0;
            _litBoxFlag = false;
        }

        protected override void DrawIcon(WindowBlock window, int icon, int x, int y)
        {
            BytePtr dst;
            BytePtr src;

            _videoLockOut |= 0x8000;

            LocksScreen(screen =>
            {
                dst = screen.Pixels;

                dst.Offset += (x + window.x) * 8;
                dst.Offset += (y * 25 + window.y) * screen.Pitch;

                if (_gd.Platform == Platform.Amiga)
                {
                    src = _iconFilePtr;
                    src += src.ToInt32BigEndian(icon * 4);
                    var color = (byte) (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR) ? 224 : 240);
                    DecompressIconPlanar(dst, src, 24, 12, color, (uint) screen.Pitch);
                }
                else
                {
                    src = _iconFilePtr;
                    src += src.ToUInt16(icon * 2);
                    DecompressIcon(dst, src, 24, 12, 224, screen.Pitch);
                }
            });

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        protected override void VcStopAnimation(ushort zone, ushort sprite)
        {
            var oldCurSpriteId = _vgaCurSpriteId;
            var oldCurZoneNum = _vgaCurZoneNum;
            var vcPtrOrg = _vcPtr;

            _vgaCurZoneNum = zone;
            _vgaCurSpriteId = sprite;

            Ptr<VgaSleepStruct> vfs = _waitSyncTable;
            while (vfs.Value.ident != 0)
            {
                if (vfs.Value.id == _vgaCurSpriteId && vfs.Value.zoneNum == _vgaCurZoneNum)
                {
                    while (vfs.Value.ident != 0)
                    {
                        vfs[0] = new VgaSleepStruct(vfs[1]);
                        vfs.Offset++;
                    }
                    break;
                }
                vfs.Offset++;
            }

            var vsp = FindCurSprite();
            if (vsp.Value.id != 0)
            {
                vc25_halt_sprite();

                Ptr<VgaTimerEntry> vte = _vgaTimerList;
                while (vte.Value.delay != 0)
                {
                    if (vte.Value.id == _vgaCurSpriteId && vte.Value.zoneNum == _vgaCurZoneNum)
                    {
                        DeleteVgaEvent(vte);
                        break;
                    }
                    vte.Offset++;
                }
            }

            _vgaCurZoneNum = oldCurZoneNum;
            _vgaCurSpriteId = oldCurSpriteId;
            _vcPtr = vcPtrOrg;
        }

        protected override void SetupGame()
        {
            gss = Simon1Settings;
            _tableIndexBase = 1576 / 4;
            _textIndexBase = 1460 / 4;
            _numVideoOpcodes = 64;
            _vgaMemSize = 1000000;
            _itemMemSize = 20000;
            _tableMemSize = 50000;
            _musicIndexBase = 1316 / 4;
            _soundIndexBase = 0;
            _frameCount = 1;
            _vgaBaseDelay = 1;
            _vgaPeriod = 50;
            _numBitArray1 = 16;
            _numBitArray2 = 16;
            _numItemStore = 10;
            _numTextBoxes = 20;
            _numVars = 255;

            _numMusic = 34;
            _numSFX = 127;
            _numSpeech = 3623;
            _numZone = 164;

            base.SetupGame();
        }

        protected override void DrawImage(Vc10State state)
        {
            var vlut = new Ptr<ushort>(_videoWindows, _windowNum * 4);

            if (!DrawImageClip(state))
                return;

            LocksScreen(screen =>
            {
                if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR))
                    state.palette = 0xC0;

                ushort xoffs, yoffs;
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                {
                    state.surf2_addr = BackGround;
                    state.surf2_pitch = (uint) _backGroundBuf.Pitch;

                    state.surf_addr = _window4BackScn.Pixels;
                    state.surf_pitch = (uint) _window4BackScn.Pitch;

                    xoffs = (ushort) (((vlut[0] - _videoWindows[16]) * 2 + state.x) * 8);
                    yoffs = (ushort) (vlut[1] - _videoWindows[17] + state.y);

                    var xmax = xoffs + state.draw_width * 2;
                    var ymax = yoffs + state.draw_height;
                    SetMoveRect(xoffs, yoffs, (ushort) xmax, (ushort) ymax);

                    _window4Flag = 1;
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                         _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
                {
                    // The DOS Floppy demo was based off Waxworks engine
                    if (_windowNum == 4 || (_windowNum >= 10 && _windowNum <= 27))
                    {
                        state.surf2_addr = BackGround;
                        state.surf2_pitch = (uint) _backGroundBuf.Pitch;

                        state.surf_addr = _window4BackScn.Pixels;
                        state.surf_pitch = (uint) (_videoWindows[18] * 16);

                        xoffs = (ushort) (((vlut[0] - _videoWindows[16]) * 2 + state.x) * 8);
                        yoffs = (ushort) (vlut[1] - _videoWindows[17] + state.y);

                        var xmax = xoffs + state.draw_width * 2;
                        var ymax = yoffs + state.draw_height;
                        SetMoveRect(xoffs, yoffs, (ushort) xmax, (ushort) ymax);

                        _window4Flag = 1;
                    }
                    else
                    {
                        state.surf_addr = screen.Pixels;
                        state.surf_pitch = (uint) screen.Pitch;

                        xoffs = (ushort) ((vlut[0] * 2 + state.x) * 8);
                        yoffs = (ushort) (vlut[1] + state.y);
                    }
                }
                else
                {
                    if (_windowNum == 3 || _windowNum == 4 || _windowNum >= 10)
                    {
                        if (_window3Flag == 1)
                        {
                            state.surf2_addr = BackGround;
                            state.surf2_pitch = (uint) _backGroundBuf.Pitch;

                            state.surf_addr = BackGround;
                            state.surf_pitch = (uint) _backGroundBuf.Pitch;
                        }
                        else
                        {
                            state.surf2_addr = BackGround;
                            state.surf2_pitch = (uint) _backGroundBuf.Pitch;

                            state.surf_addr = _window4BackScn.Pixels;
                            state.surf_pitch = (uint) _window4BackScn.Pitch;
                        }

                        xoffs = (ushort) (((vlut[0] - _videoWindows[16]) * 2 + state.x) * 8);
                        yoffs = (ushort) (vlut[1] - _videoWindows[17] + state.y);

                        var xmax = xoffs + state.draw_width * 2;
                        var ymax = yoffs + state.draw_height;
                        SetMoveRect(xoffs, yoffs, (ushort) xmax, (ushort) ymax);

                        _window4Flag = 1;
                    }
                    else
                    {
                        state.surf2_addr = BackGround;
                        state.surf2_pitch = (uint) _backGroundBuf.Pitch;

                        state.surf_addr = screen.Pixels;
                        state.surf_pitch = (uint) screen.Pitch;

                        xoffs = (ushort) ((vlut[0] * 2 + state.x) * 8);
                        yoffs = (ushort) (vlut[1] + state.y);
                    }
                }

                state.surf_addr += (int) (xoffs + yoffs * state.surf_pitch);
                state.surf2_addr += (int) (xoffs + yoffs * state.surf2_pitch);

                if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR) && _window3Flag == 0 && yoffs > 133)
                {
                    state.paletteMod = 208;
                }

                if (_backFlag)
                {
                    DrawBackGroundImage(state);
                }
                else if (state.flags.HasFlag(DrawFlags.kDFMasked))
                {
                    DrawMaskedImage(state);
                }
                else if ((((_videoLockOut & 0x20) != 0) && state.palette == 0) || state.palette == 0xC0)
                {
                    Draw32ColorImage(state);
                }
                else
                {
                    DrawVertImage(state);
                }
            });
        }

        protected override void ClearName()
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                return;

            if (_nameLocked || _lastNameOn == null)
                return;

            ResetNameWindow();
        }

        protected override void AddArrows(WindowBlock window, uint num)
        {
            var ha = FindEmptyHitArea();
            _scrollUpHitArea = (ushort) ha.Offset;

            ha.Value.x = 308;
            ha.Value.y = 149;
            ha.Value.width = 12;
            ha.Value.height = 17;
            ha.Value.flags = BoxFlags.kBFBoxInUse | BoxFlags.kBFNoTouchName;
            ha.Value.id = 0x7FFB;
            ha.Value.priority = 100;
            ha.Value.window = window;
            ha.Value.verb = 1;

            ha = FindEmptyHitArea();
            _scrollDownHitArea = (ushort) ha.Offset;

            ha.Value.x = 308;
            ha.Value.y = 176;
            ha.Value.width = 12;
            ha.Value.height = 17;
            ha.Value.flags = BoxFlags.kBFBoxInUse | BoxFlags.kBFNoTouchName;
            ha.Value.id = 0x7FFC;
            ha.Value.priority = 100;
            ha.Value.window = window;
            ha.Value.verb = 1;

            _videoLockOut |= 0x8;

            var vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, 1);
            var curVgaFile2Orig = _curVgaFile2;
            var windowNumOrig = _windowNum;
            var palette = (byte) (_gd.Platform == Platform.Amiga ? 15 : 14);

            _windowNum = 0;
            _curVgaFile2 = vpe.Value.vgaFile2;
            DrawImageInit(1, palette, 38, 150, DrawFlags.kDFSkipStoreBG);

            _curVgaFile2 = curVgaFile2Orig;
            _windowNum = windowNumOrig;

            _videoLockOut = (ushort) (_videoLockOut & ~0x8);
        }

        protected override void RemoveArrows(WindowBlock window, int num)
        {
            if (GameType == SIMONGameType.GType_SIMON1)
            {
                RestoreBlock(304, 146, 320, 200);
            }
        }

        protected override int SetupIconHitArea(WindowBlock window, uint num, int x, int y, Item itemPtr)
        {
            var ha = FindEmptyHitArea();
            ha.Value.x = (ushort) ((x + window.x) * 8);
            ha.Value.y = (ushort) (y * 25 + window.y);
            ha.Value.itemPtr = itemPtr;
            ha.Value.width = 24;
            ha.Value.height = 24;
            ha.Value.flags = BoxFlags.kBFDragBox | BoxFlags.kBFBoxInUse | BoxFlags.kBFBoxItem;
            ha.Value.id = 0x7FFD;
            ha.Value.priority = 100;
            ha.Value.verb = 208;

            return ha.Offset;
        }

        private void ListSaveGames()
        {
            Stream @in;
            ushort i, slot;
            BytePtr dst = _saveBuf;

            DisableFileBoxes();

            ShowMessageFormat("\xC");

            dst.Data.Set(dst.Offset, 0, 108);

            slot = _saveLoadRowCurPos;
            while (_saveLoadRowCurPos + 6 > slot)
            {
                if ((@in = OSystem.SaveFileManager.OpenForLoading(GenSaveName(slot))) == null)
                    break;

                @in.Read(dst.Data, dst.Offset, 18);
                @in.Dispose();

                var lastSlot = slot;
                if (slot < 10)
                {
                    ShowMessageFormat(" ");
                }
                else if (_language == Language.HE_ISR)
                {
                    lastSlot = (ushort) ((slot % 10) * 10);
                    lastSlot = (ushort) (lastSlot + slot / 10);
                }

                ShowMessageFormat("{0}", lastSlot);
                if (_language == Language.HE_ISR && (slot % 10) == 0)
                    ShowMessageFormat("0");
                ShowMessageFormat(".{0}\n", dst.GetRawText());
                dst += 18;
                slot++;
            }

            if (!_saveOrLoad)
            {
                if (_saveLoadRowCurPos + 6 == slot)
                {
                    slot++;
                }
                else
                {
                    if (slot < 10)
                        ShowMessageFormat(" ");
                    ShowMessageFormat("{0}.\n", slot);
                }
            }
            else
            {
                if (_saveLoadRowCurPos + 6 == slot)
                {
                    if ((@in = OSystem.SaveFileManager.OpenForLoading(GenSaveName(slot))) != null)
                    {
                        slot++;
                        @in.Dispose();
                    }
                }
            }

            _saveDialogFlag = true;

            i = (ushort) (slot - _saveLoadRowCurPos);
            if (i != 7)
            {
                i++;
                if (!_saveOrLoad)
                    i++;
                _saveDialogFlag = false;
            }

            if (--i == 0)
                return;

            do
            {
                EnableBox(208 + i - 1);
            } while (--i != 0);
        }

        private void Draw32ColorImage(Vc10State state)
        {
            BytePtr src;
            BytePtr dst;

            if (state.flags.HasFlag(DrawFlags.kDFCompressed))
            {
                var dstPtr = state.surf_addr;
                src = state.srcPtr;
                /* AAAAAAAA BBBBBBBB CCCCCCCC DDDDDDDD EEEEEEEE
                 * aaaaabbb bbcccccd ddddeeee efffffgg ggghhhhh
                 */

                do
                {
                    var count = state.draw_width / 4;

                    dst = dstPtr;
                    do
                    {
                        var bits = (src[0] << 24) | (src[1] << 16) | (src[2] << 8) | src[3];
                        byte color;

                        color = (byte) ((bits >> (32 - 5)) & 31);
                        if ((state.flags.HasFlag(DrawFlags.kDFNonTrans)) || color != 0)
                            dst[0] = color;
                        color = (byte) ((bits >> (32 - 10)) & 31);
                        if ((state.flags.HasFlag(DrawFlags.kDFNonTrans)) || color != 0)
                            dst[1] = color;
                        color = (byte) ((bits >> (32 - 15)) & 31);
                        if ((state.flags.HasFlag(DrawFlags.kDFNonTrans)) || color != 0)
                            dst[2] = color;
                        color = (byte) ((bits >> (32 - 20)) & 31);
                        if ((state.flags.HasFlag(DrawFlags.kDFNonTrans)) || color != 0)
                            dst[3] = color;
                        color = (byte) ((bits >> (32 - 25)) & 31);
                        if ((state.flags.HasFlag(DrawFlags.kDFNonTrans)) || color != 0)
                            dst[4] = color;
                        color = (byte) ((bits >> (32 - 30)) & 31);
                        if ((state.flags.HasFlag(DrawFlags.kDFNonTrans)) || color != 0)
                            dst[5] = color;

                        bits = (bits << 8) | src[4];

                        color = (byte) ((bits >> (40 - 35)) & 31);
                        if ((state.flags.HasFlag(DrawFlags.kDFNonTrans)) || color != 0)
                            dst[6] = color;
                        color = (byte) ((bits) & 31);
                        if ((state.flags.HasFlag(DrawFlags.kDFNonTrans)) || color != 0)
                            dst[7] = color;

                        dst += 8;
                        src += 5;
                    } while (--count != 0);
                    dstPtr.Offset = (int) (dstPtr.Offset + state.surf_pitch);
                } while (--state.draw_height != 0);
            }
            else
            {
                src = state.srcPtr + (state.width * state.y_skip * 16) + (state.x_skip * 8);
                dst = state.surf_addr;

                state.draw_width *= 2;

                int h = state.draw_height;
                do
                {
                    int i;
                    for (i = 0; i != state.draw_width; i++)
                        if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || src[i] != 0)
                            dst[i] = (byte) (src[i] + state.paletteMod);
                    dst.Offset = (int) (dst.Offset + state.surf_pitch);
                    src += state.width * 16;
                } while (--h != 0);
            }
        }

        private void DrawMaskedImage(Vc10State state)
        {
            if (GameType == SIMONGameType.GType_SIMON1 && (_windowNum == 3 || _windowNum == 4 || _windowNum >= 10))
            {
                state.surf2_addr += _videoWindows[17] * 320;
            }

            if (Features.HasFlag(GameFeatures.GF_32COLOR))
            {
                var mask = state.srcPtr + (state.width * state.y_skip * 16) + (state.x_skip * 8);
                var src = state.surf2_addr;
                var dst = state.surf_addr;

                state.draw_width *= 2;

                int h = state.draw_height;
                do
                {
                    for (var i = 0; i != state.draw_width; i++)
                    {
                        if (GameType == SIMONGameType.GType_SIMON1 && GetBitFlag(88))
                        {
                            /* transparency */
                            if (mask[i] != 0 && ((dst[i] & 16) != 0))
                                dst[i] = src[i];
                        }
                        else
                        {
                            /* no transparency */
                            if (mask[i] != 0)
                                dst[i] = src[i];
                        }
                    }
                    dst.Offset += (int) state.surf_pitch;
                    src.Offset += (int) state.surf2_pitch;
                    mask += state.width * 16;
                } while (--h != 0);
            }
            else if (state.flags.HasFlag(DrawFlags.kDFCompressed))
            {
                state.x_skip *= 4;
                state.dl = state.width;
                state.dh = state.height;

                vc10_skip_cols(state);

                var w = 0;
                do
                {
                    var mask = vc10_depackColumn(state);
                    var src = state.surf2_addr + w * 2;
                    var dst = state.surf_addr + w * 2;

                    var h = (byte) state.draw_height;
                    do
                    {
                        if (GameType == SIMONGameType.GType_SIMON1 && GetBitFlag(88))
                        {
                            /* transparency */
                            if (((mask[0] & 0xF0) != 0) && ((dst[0] & 0x0F0) == 0x20))
                                dst[0] = src[0];
                            if (((mask[0] & 0x0F) != 0) && ((dst[1] & 0x0F0) == 0x20))
                                dst[1] = src[1];
                        }
                        else
                        {
                            /* no transparency */
                            if ((mask[0] & 0xF0) != 0)
                                dst[0] = src[0];
                            if ((mask[0] & 0x0F) != 0)
                                dst[1] = src[1];
                        }
                        mask.Offset++;
                        dst.Offset += (int) state.surf_pitch;
                        src.Offset += (int) state.surf2_pitch;
                    } while (--h != 0);
                } while (++w != state.draw_width);
            }
            else
            {
                var mask = state.srcPtr + (state.width * state.y_skip) * 8;
                var src = state.surf2_addr;
                var dst = state.surf_addr;

                state.x_skip *= 4;

                do
                {
                    for (var count = 0; count != state.draw_width; count++)
                    {
                        if (GameType == SIMONGameType.GType_SIMON1 && GetBitFlag(88))
                        {
                            /* transparency */
                            if ((mask[count + state.x_skip] & 0xF0) != 0)
                                if ((dst[count * 2] & 0xF0) == 0x20)
                                    dst[count * 2] = src[count * 2];
                            if ((mask[count + state.x_skip] & 0x0F) != 0)
                                if ((dst[count * 2 + 1] & 0xF0) == 0x20)
                                    dst[count * 2 + 1] = src[count * 2 + 1];
                        }
                        else
                        {
                            /* no transparency */
                            if ((mask[count + state.x_skip] & 0xF0) != 0)
                                dst[count * 2] = src[count * 2];
                            if ((mask[count + state.x_skip] & 0x0F) != 0)
                                dst[count * 2 + 1] = src[count * 2 + 1];
                        }
                    }
                    src.Offset += (int) state.surf2_pitch;
                    dst.Offset += (int) state.surf_pitch;
                    mask += state.width * 8;
                } while (--state.draw_height != 0);
            }
        }

        private void PlaySpeech(ushort speechId, ushort vgaSpriteId)
        {
            if (speechId == 9999)
            {
                if (_subtitles)
                    return;
                if (!GetBitFlag(14) && !GetBitFlag(28))
                {
                    SetBitFlag(14, true);
                    _variableArray[100] = 15;
                    Animate(4, 1, 130, 0, 0, 0);
                    WaitForSync(130);
                }
                _skipVgaWait = true;
            }
            else
            {
                if (_subtitles && _scriptVar2)
                {
                    Animate(4, 2, 204, 0, 0, 0);
                    WaitForSync(204);
                    StopAnimate(204);
                }
                if (vgaSpriteId < 100)
                    StopAnimate((ushort) (201 + vgaSpriteId));

                LoadVoice(speechId);

                if (vgaSpriteId < 100)
                    Animate(4, 2, (ushort) (201 + vgaSpriteId), 0, 0, 0);
            }
        }

        protected override void PlayMusic(ushort music, ushort track)
        {
            StopMusic();

            // Support for compressed music from the ScummVM Music Enhancement Project
            // TODO: _system.AudioCDManager().Stop();
            //_system.getAudioCDManager().play(music + 1, -1, 0, 0, true);
            //if (_system.getAudioCDManager().isPlaying())
            //    return;

            if (GamePlatform == Platform.Amiga)
            {
                PlayModule(music);
            }
            else if (Features.HasFlag(GameFeatures.GF_TALKIE))
            {
                var buf = new byte[4];

                // WORKAROUND: For a script bug in the CD versions
                // We skip this music resource, as it was replaced by
                // a sound effect, and the script was never updated.
                if (music == 35)
                    return;

                _midi.SetLoop(true); // Must do this BEFORE loading music. (GMF may have its own override.)

                _gameFile.Seek(_gameOffsetsPtr[_musicIndexBase + music], SeekOrigin.Begin);
                _gameFile.Read(buf, 0, 4);
                if (ScummHelper.ArrayEquals(buf, 0, "GMF\x1".GetBytes(), 0, 4))
                {
                    _gameFile.Seek(_gameOffsetsPtr[_musicIndexBase + music], SeekOrigin.Begin);
                    _midi.LoadSMF(_gameFile, music);
                }
                else
                {
                    _gameFile.Seek(_gameOffsetsPtr[_musicIndexBase + music], SeekOrigin.Begin);
                    _midi.LoadMultipleSMF(_gameFile);
                }

                _midi.StartTrack(0);
                _midi.StartTrack(track);
            }
            else if (GamePlatform == Platform.Acorn)
            {
                // TODO: Add support for Desktop Tracker format in Acorn disk version
            }
            else
            {
                var filename = $"MOD{music}.MUS";
                var f = OpenFileRead(filename);
                if (f == null)
                    Error("playMusic: Can't load music from '{0}'", filename);

                _midi.SetLoop(true); // Must do this BEFORE loading music. (GMF may have its own override.)

                if (Features.HasFlag(GameFeatures.GF_DEMO))
                    _midi.LoadS1D(f);
                else
                    _midi.LoadSMF(f, music);

                _midi.StartTrack(0);
                _midi.StartTrack(track);
            }
        }

        private void os1_animate()
        {
            // 98: animate
            var vgaSpriteId = (ushort) GetVarOrWord();
            var windowNum = (ushort) GetVarOrByte();
            var x = (short) GetVarOrWord();
            var y = (short) GetVarOrWord();
            var palette = (ushort) (GetVarOrWord() & 15);

            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE) && vgaSpriteId >= 400)
            {
                _lastVgaWaitFor = 0;
            }

            _videoLockOut |= 0x40;
            Animate(windowNum, (ushort) (vgaSpriteId / 100), vgaSpriteId, x, y, palette);
            _videoLockOut = (ushort) (_videoLockOut & ~0x40);
        }

        private void os1_pauseGame()
        {
            // 135: pause game
            OSystem.InputManager.ShowVirtualKeyboard();

            // TODO: os1_pauseGame
            // KeyCode keyYes, keyNo;

//            GetLanguageYesNo(_language, keyYes, keyNo);
//
//            while (!HasToQuit) {
//                Delay(1);
//                if (_keyPressed.keycode == keyYes)
//                    QuitGame();
//                else if (_keyPressed.keycode == keyNo)
//                    break;
//            }

            OSystem.InputManager.HideVirtualKeyboard();
        }

        private void os1_screenTextBox()
        {
            // 161: setup text
            var tl = GetTextLocation(GetVarOrByte());

            tl.x = (short) GetVarOrWord();
            tl.y = (short) GetVarOrByte();
            tl.width = (short) GetVarOrWord();
        }

        private void os1_screenTextMsg()
        {
            // 162: print string
            var vgaSpriteId = GetVarOrByte();
            var color = GetVarOrByte();
            var stringId = GetNextStringID();
            var stringPtr = string.Empty;
            uint speechId = 0;

            if (stringId != 0xFFFF)
                stringPtr = GetStringPtrById((ushort) stringId);

            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                    speechId = (ushort) GetVarOrWord();
                else
                    speechId = (ushort) GetNextWord();
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                vgaSpriteId = 1;

            var tl = GetTextLocation(vgaSpriteId);
            if (_speech && speechId != 0)
                PlaySpeech((ushort) speechId, (ushort) vgaSpriteId);
            if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 &&
                 _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE) ||
                 _gd.ADGameDescription.gameType == SIMONGameType.GType_FF) &&
                speechId == 0)
            {
                StopAnimateSimon2(2, (ushort) (vgaSpriteId + 2));
            }

            // WORKAROUND: Several strings in the French version of Simon the Sorcerer 1 set the incorrect width,
            // causing crashes, or glitches in subtitles. See bug #3512776 for example.
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                _language == Language.FR_FRA)
            {
                if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE) && stringId == 33219)
                    tl.width = 96;
                if (!_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE) && stringId == 33245)
                    tl.width = 96;
            }

            if (!string.IsNullOrEmpty(stringPtr) && (speechId == 0 || _subtitles))
                PrintScreenText(vgaSpriteId, color, stringPtr, tl.x, tl.y, tl.width);
        }

        private void os1_playEffect()
        {
            // 163: play sound
            var soundId = (ushort) GetVarOrWord();

            if (GameId == GameIds.GID_SIMON1DOS)
                PlaySting(soundId);
            else
                _sound.PlayEffects(soundId);
        }

        private void os1_screenTextPObj()
        {
            // 177: inventory descriptions
            var vgaSpriteId = GetVarOrByte();
            var color = GetVarOrByte();

            var subObject = (SubObject) FindChildOfType(GetNextItemPtr(), ChildType.kObjectType);
            if (Features.HasFlag(GameFeatures.GF_TALKIE))
            {
                if (subObject != null && subObject.objectFlags.HasFlag(SubObjectFlags.kOFVoice))
                {
                    var offs = GetOffsetOfChild2Param(subObject, (int) SubObjectFlags.kOFVoice);
                    PlaySpeech((ushort) subObject.objectFlagValue[offs], (ushort) vgaSpriteId);
                }
                else if (subObject != null && subObject.objectFlags.HasFlag(SubObjectFlags.kOFNumber))
                {
                    var offs = GetOffsetOfChild2Param(subObject, (int) SubObjectFlags.kOFNumber);
                    PlaySpeech((ushort) (subObject.objectFlagValue[offs] + 3550), (ushort) vgaSpriteId);
                }
            }

            if (subObject != null && subObject.objectFlags.HasFlag(SubObjectFlags.kOFText) && _subtitles)
            {
                var stringPtr = GetStringPtrById((ushort) subObject.objectFlagValue[0]);
                var tl = GetTextLocation(vgaSpriteId);

                if (subObject.objectFlags.HasFlag(SubObjectFlags.kOFNumber))
                {
                    string buf;
                    if (_language == Language.HE_ISR)
                    {
                        int j =
                            subObject.objectFlagValue[GetOffsetOfChild2Param(subObject, (int) SubObjectFlags.kOFNumber)];
                        var k = j % 10 * 10;
                        k += j / 10;
                        if (j % 10 == 0)
                            buf = $"0{k}{stringPtr}";
                        else
                            buf = $"{k}{stringPtr}";
                    }
                    else
                    {
                        buf = string.Format("{0}{1}",
                            subObject.objectFlagValue[GetOffsetOfChild2Param(subObject, (int) SubObjectFlags.kOFNumber)],
                            stringPtr);
                    }
                    stringPtr = buf;
                }
                if (!string.IsNullOrEmpty(stringPtr))
                    PrintScreenText(vgaSpriteId, color, stringPtr, tl.x, tl.y, tl.width);
            }
        }

        private void os1_getPathPosn()
        {
            // 178: path find
            var x = GetVarOrWord();
            var y = GetVarOrWord();
            var var1 = GetVarOrByte();
            var var2 = GetVarOrByte();

            uint bestI = 0, bestJ = 0, bestDist = 0xFFFFFFFF;
            var maxPath = GameType == SIMONGameType.GType_FF || GameType == SIMONGameType.GType_PP ? 100 : 20;

            if (GameType == SIMONGameType.GType_FF || GameType == SIMONGameType.GType_PP)
            {
                x = (uint) (x + _scrollX);
                y = (uint) (y + _scrollY);
            }
            else if (GameType == SIMONGameType.GType_SIMON2)
            {
                x = (uint) (x + _scrollX * 8);
            }

            var end = (GameType == SIMONGameType.GType_FF) ? 9999 : 999;
            var prevI = (uint) (maxPath + 1 - ReadVariable(12));
            for (var i = maxPath; i != 0; --i)
            {
                var p = _pathFindArray[maxPath - i];
                if (p == BytePtr.Null)
                    continue;

                for (var j = 0; ReadUint16Wrapper(p) != end; j++, p += 4)
                {
                    var xDiff = (uint) Math.Abs((short) (ReadUint16Wrapper(p) - x));
                    var yDiff = (uint) Math.Abs((short) (ReadUint16Wrapper(p + 2) - 12 - y));

                    if (xDiff < yDiff)
                    {
                        xDiff /= 4;
                        yDiff *= 4;
                    }
                    xDiff += yDiff / 4;

                    if ((xDiff < bestDist) || ((xDiff == bestDist) && (prevI == i)))
                    {
                        bestDist = xDiff;
                        bestI = (uint) (maxPath + 1 - i);
                        bestJ = (uint) j;
                    }
                }
            }

            WriteVariable((ushort) var1, (ushort) bestI);
            WriteVariable((ushort) var2, (ushort) bestJ);
        }

        private void os1_scnTxtLongText()
        {
            // 179: conversation responses and room descriptions
            var vgaSpriteId = GetVarOrByte();
            var color = GetVarOrByte();
            var stringId = GetVarOrByte();
            uint speechId = 0;

            var stringPtr = GetStringPtrById(_longText[stringId]);
            if (Features.HasFlag(GameFeatures.GF_TALKIE))
                speechId = _longSound[stringId];

            if (GameType == SIMONGameType.GType_FF || GameType == SIMONGameType.GType_PP)
                vgaSpriteId = 1;
            var tl = GetTextLocation(vgaSpriteId);

            if (_speech && speechId != 0)
                PlaySpeech((ushort) speechId, (ushort) vgaSpriteId);
            if (!string.IsNullOrEmpty(stringPtr) && _subtitles)
                PrintScreenText(vgaSpriteId, color, stringPtr, tl.x, tl.y, tl.width);
        }

        private void os1_mouseOn()
        {
            // 180: force mouseOn
            _mouseHideCount = 0;
        }

        private void os1_mouseOff()
        {
            // 181: force mouseOff
            ScriptMouseOff();
        }

        private void os1_loadBeard()
        {
            // 182: load beard
            if (_beardLoaded == false)
            {
                _beardLoaded = true;
                _videoLockOut |= 0x8000;
                LoadVGABeardFile(328);
                _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
            }
        }

        private void os1_unloadBeard()
        {
            // 183: unload beard
            if (_beardLoaded)
            {
                _beardLoaded = false;
                _videoLockOut |= 0x8000;
                LoadVGABeardFile(23);
                _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
            }
        }

        private void os1_unloadZone()
        {
            // 184: unload zone
            var a = GetVarOrWord();
            var vpe = _vgaBufferPointers[a];
            vpe.sfxFile = BytePtr.Null;
            vpe.vgaFile1 = BytePtr.Null;
            vpe.vgaFile2 = BytePtr.Null;
        }

        private void os1_loadStrings()
        {
            // 185: load sound files
            _soundFileId = (ushort) GetVarOrWord();
            if (_gd.Platform == Platform.Amiga && _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
            {
                string buf = $"{_soundFileId}Effects";
                _sound.ReadSfxFile(buf);
                buf = $"{_soundFileId}simon";
                _sound.ReadVoiceFile(buf);
            }
        }

        private void os1_unfreezeZones()
        {
            // 186: freeze zone
            UnfreezeBottom();
        }

        private void os1_specialFade()
        {
            // 187: fade to black

            for (var i = 32; i != 0; --i)
            {
                PaletteFadeOut(_currentPalette, 32, 8);
                PaletteFadeOut(new Ptr<Color>(_currentPalette, 48), 144, 8);
                PaletteFadeOut(new Ptr<Color>(_currentPalette, 208), 48, 8);
                OSystem.GraphicsManager.SetPalette(_currentPalette, 0, 256);
                Delay(5);
            }

            Array.Copy(_currentPalette, _displayPalette, _currentPalette.Length);
        }

        private void ScriptMouseOff()
        {
            _videoLockOut |= 0x8000;
            vc34_setMouseOff();
            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        private void vc22_setPalette()
        {
            Ptr<Color> palptr;
            ushort num, palSize;

            var a = (ushort) VcReadNextWord();
            var b = (ushort) VcReadNextWord();

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                num = 256;
                palSize = 768;
                palptr = _displayPalette;
            }
            else
            {
                num = (ushort) (a == 0 ? 32 : 16);
                palSize = 96;
                palptr = new Ptr<Color>(_displayPalette, a * 16);
            }

            var offs = _curVgaFile1 + 6;
            var src = offs + b * palSize;

            do
            {
                palptr[0] = Color.FromRgb(src[0] * 4, src[1] * 4, src[2] * 4);
                palptr.Offset++;
                src += 3;
            } while (--num != 0);

            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR))
            {
                // Custom palette used for verb area
                palptr = new Ptr<Color>(_displayPalette, 13 * 16);
                for (var c = 0; c < 32; c++)
                {
                    palptr[0] = Color.FromRgb(CustomPalette[c * 3 + 0], CustomPalette[c * 3 + 1],
                        CustomPalette[c * 3 + 2]);
                    palptr.Offset++;
                }
            }

            _paletteFlag = 2;
            _vgaSpriteChanged++;
        }

        private static readonly byte[] CustomPalette =
        {
            0x00, 0x00, 0x00,
            0x99, 0x22, 0xFF,
            0x66, 0xCC, 0xFF,
            0xFF, 0x99, 0xFF,
            0xFF, 0xFF, 0xFF,
            0x66, 0x44, 0xBB,
            0x77, 0x55, 0xCC,
            0x88, 0x77, 0xCC,
            0xCC, 0xAA, 0xDD,
            0x33, 0x00, 0x09,
            0x66, 0x44, 0xCC,
            0x88, 0x55, 0xCC,
            0xAA, 0x77, 0xEE,
            0x00, 0x00, 0x00,
            0x00, 0x00, 0x00,
            0x00, 0x00, 0x00,
            0x00, 0x00, 0x00,
            0xFF, 0xFF, 0xFF,
            0x33, 0x00, 0x00,
            0xCC, 0xCC, 0xDD,
            0x88, 0x99, 0xBB,
            0x44, 0x77, 0xAA,
            0x44, 0x44, 0x66,
            0x44, 0x44, 0x00,
            0x44, 0x66, 0x00,
            0x88, 0x99, 0x00,
            0x99, 0x44, 0x00,
            0xBB, 0x44, 0x22,
            0xFF, 0x55, 0x33,
            0xFF, 0x88, 0x88,
            0xFF, 0xBB, 0x33,
            0xFF, 0xFF, 0x77,
        };
    }
}