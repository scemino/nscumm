//
//  AGOSEngineElvira2.cs
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
    internal class AgosEngineElvira2 : AgosEngineElvira1
    {
        private Dictionary<int, Action> _opcodes;

        public AgosEngineElvira2(ISystem system, GameSettings settings, AgosGameDescription gd)
            : base(system, settings, gd)
        {
        }

        protected override void SetupGame()
        {
            gss = Simon1Settings;
            _numVideoOpcodes = 60;
            _vgaMemSize = 1000000;
            _itemMemSize = 64000;
            _tableMemSize = 100000;
            _frameCount = 4;
            _vgaBaseDelay = 1;
            _vgaPeriod = 50;
            _numBitArray1 = 16;
            _numBitArray2 = 15;
            _numItemStore = 50;
            _numVars = 255;

            _numMusic = 9;
            _numZone = 99;

            SetupGameCore();
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
                {68, o_end},
                {69, o_done},
                {71, o_process},
                {72, oe2_doClass},
                {73, oe2_pObj},
                {74, oe1_pName},
                {75, oe1_pcName},
                {76, o_when},
                {77, o_if1},
                {78, o_if2},
                {79, o_isCalled},
                {80, o_is},
                {82, o_debug},
                {83, oe1_rescan},
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
                {107, o_addBox},
                {108, o_delBox},
                {109, o_enableBox},
                {110, o_disableBox},
                {111, o_moveBox},
                {113, oe2_drawItem},
                {114, o_doIcons},
                {115, o_isClass},
                {116, o_setClass},
                {117, o_unsetClass},
                {119, o_waitSync},
                {120, o_sync},
                {121, o_defObj},
                {123, oe1_setTime},
                {124, oe1_ifTime},
                {125, o_here},
                {126, o_doClassIcons},
                {127, o_playTune},
                {130, o_setAdjNoun},
                {132, o_saveUserGame},
                {133, o_loadUserGame},
                {135, oe2_pauseGame},
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
                {161, oe2_printStats},
                {165, oe2_setSuperRoom},
                {166, oe2_getSuperRoom},
                {167, oe2_setExitOpen},
                {168, oe2_setExitClosed},
                {169, oe2_setExitLocked},
                {170, oe2_setExitClosed},
                {171, oe2_ifExitOpen},
                {172, oe2_ifExitClosed},
                {173, oe2_ifExitLocked},
                {174, oe2_playEffect},
                {175, oe2_getDollar2},
                {176, oe2_setSRExit},
                {177, oe2_printPlayerDamage},
                {178, oe2_printMonsterDamage},
                {179, oe2_isAdjNoun},
                {180, oe2_b2Set},
                {181, oe2_b2Clear},
                {182, oe2_b2Zero},
                {183, oe2_b2NotZero},
            };
            _numOpcodes = 184;
        }

        protected override void SetupVideoOpcodes(Action[] op)
        {
            Debug("setupVideoOpcodes");
            SetupVideoOpcodesCore(op);

            op[17] = vc17_waitEnd;
            op[19] = vc19_loop;
            op[22] = vc22_setPalette;
            op[28] = vc28_playSFX;
            op[32] = vc32_saveScreen;
            op[37] = vc37_pokePalette;
            op[45] = vc45_setWindowPalette;
            op[46] = vc46_setPaletteSlot1;
            op[47] = vc47_setPaletteSlot2;
            op[48] = vc48_setPaletteSlot3;
            op[53] = vc53_dissolveIn;
            op[54] = vc54_dissolveOut;
            op[57] = vc57_blackPalette;
            op[56] = vc56_fullScreen;
            op[58] = vc58_checkCodeWheel;
            op[59] = vc59_ifEGA;
        }

        protected override int CanPlace(Item x, Item y)
        {
            var z = DerefItem(x.parent);
            var o = (SubObject) FindChildOfType(y, ChildType.kObjectType);
            if (o == null)
                return 0; /* Fits Fine */

            XPlace(x, null); /* Avoid disturbing figures */
            var cap = SizeContents(y);

            XPlace(x, z);
            if (o.objectFlags.HasFlag(SubObjectFlags.kOFVolume))
            {
                var ct = GetOffsetOfChild2Param(o, (int) SubObjectFlags.kOFVolume);
                cap = o.objectFlagValue[ct] - cap;
                cap -= SizeOfRec(x, 0); /* - size of item going in */
                if (cap < 0)
                    return -1; /* Too big to fit */
            }

            return 0;
        }

        private void vc45_setWindowPalette()
        {
            var num = (ushort) VcReadNextWord();
            var color = (ushort) VcReadNextWord();

            var vlut = new Ptr<ushort>(VideoWindows, num * 4);
            var width = (byte) (vlut[2] * 8);
            var height = (byte) vlut[3];

            if (num == 4)
            {
                var dst = _window4BackScn.Pixels;

                for (byte h = 0; h < height; h++)
                {
                    for (byte w = 0; w < width; w++)
                    {
                        var val = dst.ToUInt16(w * 2);
                        val &= 0xF0F;
                        val = (ushort) (val | color * 16);
                        dst.WriteUInt16(w * 2, val);
                    }
                    dst += width * 2;
                }
            }
            else
            {
                LockScreen(screen =>
                {
                    var dst = screen.GetBasePtr(vlut[0] * 16, vlut[1]);

                    if (GameType == SIMONGameType.GType_ELVIRA2 && num == 7)
                    {
                        dst.Offset -= 8;
                        width += 4;
                    }

                    for (byte h = 0; h < height; h++)
                    {
                        for (byte w = 0; w < width; w++)
                        {
                            var val = dst.ToUInt16(w * 2);
                            val &= 0xF0F;
                            val = (ushort) (val | color * 16);
                            dst.WriteUInt16(w * 2, val);
                        }
                        dst += screen.Pitch;
                    }
                });
            }
        }

        private void vc46_setPaletteSlot1()
        {
            var srcOffs = (ushort) VcReadNextWord();
            SetPaletteSlot(srcOffs, 1);
        }

        private void vc47_setPaletteSlot2()
        {
            var srcOffs = (ushort) VcReadNextWord();
            SetPaletteSlot(srcOffs, 2);
        }

        private void vc48_setPaletteSlot3()
        {
            var srcOffs = (ushort) VcReadNextWord();
            SetPaletteSlot(srcOffs, 3);
        }

        private void SetPaletteSlot(ushort srcOffs, byte dstOffs)
        {
            var palptr = new Ptr<Color>(DisplayPalette, dstOffs * 3 * 16);
            var offs = _curVgaFile1 + _curVgaFile1.ToUInt16BigEndian(6);
            var src = offs + srcOffs * 32;
            ushort num = 16;

            do
            {
                var color = src.ToUInt16BigEndian();
                palptr[0] = Color.FromRgb(
                    ((color & 0xf00) >> 8) * 32,
                    ((color & 0x0f0) >> 4) * 32,
                    ((color & 0x00f) >> 0) * 32);

                palptr.Offset++;
                src += 2;
            } while (--num != 0);

            _paletteFlag = 2;
        }

        private void vc53_dissolveIn()
        {
            var num = (ushort) VcReadNextWord();
            var speed = (ushort) (VcReadNextWord() + 1);

            BytePtr src, dst, srcOffs, srcOffs2, dstOffs, dstOffs2;
            short xoffs, yoffs;
            byte color = 0;

            // Only uses Video Window 4
            num = 4;

            var dissolveX = (ushort) (VideoWindows[num * 4 + 2] * 8);
            var dissolveY = (ushort) ((VideoWindows[num * 4 + 3] + 1) / 2);
            var dissolveCheck = (ushort) (dissolveY * dissolveX * 4);
            var dissolveDelay = (ushort) (dissolveCheck * 2 / speed);
            var dissolveCount = (ushort) (dissolveCheck * 2 / speed);

            var x = (short) (VideoWindows[num * 4 + 0] * 16);
            var y = (short) VideoWindows[num * 4 + 1];

            var count = (ushort) (dissolveCheck * 2);
            while (count-- != 0)
            {
                LockScreen(screen =>
                {
                    var dstPtr = screen.GetBasePtr(x, y);

                    yoffs = (short) _rnd.GetRandomNumber(dissolveY);
                    dst = dstPtr + yoffs * screen.Pitch;
                    src = _window4BackScn.GetBasePtr(0, yoffs);

                    xoffs = (short) _rnd.GetRandomNumber(dissolveX);
                    dst += xoffs;
                    src += xoffs;

                    dst.Value = (byte) (dst.Value & color);
                    dst.Value = (byte) (dst.Value | src.Value & 0xF);

                    dstOffs = dst;
                    srcOffs = src;

                    xoffs = (short) (dissolveX * 2 - 1 - (xoffs * 2));
                    dst += xoffs;
                    src += xoffs;

                    dst.Value = (byte) (dst.Value & color);
                    dst.Value = (byte) (dst.Value | src.Value & 0xF);

                    srcOffs2 = src;
                    dstOffs2 = dst;

                    yoffs = (short) ((dissolveY - 1) * 2 - (yoffs * 2));
                    src = srcOffs + yoffs * _window4BackScn.Pitch;
                    dst = dstOffs + yoffs * screen.Pitch;

                    color = 0xF0;
                    dst.Value = (byte) (dst.Value & color);
                    dst.Value = (byte) (dst.Value | src.Value & 0xF);

                    dst = dstOffs2 + yoffs * screen.Pitch;
                    src = srcOffs2 + yoffs * _window4BackScn.Pitch;

                    dst.Value = (byte) (dst.Value & color);
                    dst.Value = (byte) (dst.Value | src.Value & 0xF);
                });

                dissolveCount--;
                if (dissolveCount == 0)
                {
                    if (count >= dissolveCheck)
                        dissolveDelay++;

                    dissolveCount = dissolveDelay;
                    Delay(1);
                }
            }
        }

        private void vc54_dissolveOut()
        {
            ushort num = (ushort) VcReadNextWord();
            ushort color = (ushort) VcReadNextWord();
            ushort speed = (ushort) (VcReadNextWord() + 1);

            BytePtr dst, dstOffs;
            short xoffs, yoffs;

            ushort dissolveX = (ushort) (VideoWindows[num * 4 + 2] * 8);
            ushort dissolveY = (ushort) ((VideoWindows[num * 4 + 3] + 1) / 2);
            ushort dissolveCheck = (ushort) (dissolveY * dissolveX * 4);
            ushort dissolveDelay = (ushort) (dissolveCheck * 2 / speed);
            ushort dissolveCount = (ushort) (dissolveCheck * 2 / speed);

            short x = (short) (VideoWindows[num * 4 + 0] * 16);
            short y = (short) VideoWindows[num * 4 + 1];

            ushort count = (ushort) (dissolveCheck * 2);
            while (count-- != 0)
            {
                LockScreen(screen =>
                {
                    var dstPtr = screen.GetBasePtr(x, y);
                    color = (ushort) (color | dstPtr[0] & 0xF0);

                    yoffs = (short) _rnd.GetRandomNumber(dissolveY);
                    xoffs = (short) _rnd.GetRandomNumber(dissolveX);
                    dst = dstPtr + xoffs + yoffs * screen.Pitch;
                    dst.Value = (byte) color;

                    dstOffs = dst;

                    xoffs = (short) (dissolveX * 2 - 1 - (xoffs * 2));
                    dst += xoffs;
                    dst.Value = (byte) color;

                    yoffs = (short) ((dissolveY - 1) * 2 - (yoffs * 2));
                    dst = dstOffs + yoffs * screen.Pitch;
                    dst.Value = (byte) color;

                    dst += xoffs;
                    dst.Value = (byte) color;
                });

                dissolveCount--;
                if (dissolveCount == 0)
                {
                    if (count >= dissolveCheck)
                        dissolveDelay++;

                    dissolveCount = dissolveDelay;
                    Delay(1);
                }
            }
        }

        private void vc57_blackPalette()
        {
            Array.Clear(_currentPalette, 0, _currentPalette.Length);
            OSystem.GraphicsManager.SetPalette(_currentPalette, 0, 256);
        }

        private void vc56_fullScreen()
        {
            LockScreen(screen =>
            {
                var dst = screen.Pixels;
                var src = _curVgaFile2 + 800;

                for (var i = 0; i < _screenHeight; i++)
                {
                    src.Copy(dst, _screenWidth);
                    src.Offset += 320;
                    dst.Offset += screen.Pitch;
                }
            });

            FullFade();
        }

        private void vc59_ifEGA()
        {
            // Skip if not EGA
            VcSkipNextInstruction();
        }

        protected override void UserGame(bool load)
        {
            uint saveTime;
            int i, numSaveGames;
            bool b;
            Array.Clear(SaveBuf, 0, SaveBuf.Length);

            _saveOrLoad = load;

            saveTime = GetTime();

            if (GameType == SIMONGameType.GType_ELVIRA2)
                HaltAnimation();

            numSaveGames = CountSaveGames();
            _numSaveGameRows = (ushort) numSaveGames;
            _saveLoadRowCurPos = 1;
            _saveLoadEdit = false;

            byte num = (byte) ((GameType == SIMONGameType.GType_WW) ? 3 : 4);

            ListSaveGames();

            if (!load)
            {
                var window = _windowArray[num];
                short slot = -1;

                var name = new BytePtr(SaveBuf, 192);

                while (!HasToQuit)
                {
                    WindowPutChar(window, 128);

                    _saveLoadEdit = true;

                    i = UserGameGetKey(out b, 128);
                    if (b)
                    {
                        if (i <= 23)
                        {
                            if (!ConfirmOverWrite(window))
                            {
                                ListSaveGames();
                                continue;
                            }

                            if (!SaveGame(_saveLoadRowCurPos + i, SaveBuf.GetRawText(i * 8)))
                                FileError(_windowArray[num], true);
                        }

                        goto get_out;
                    }

                    UserGameBackSpace(_windowArray[num], 8);
                    if (i == 10 || i == 13)
                    {
                        slot = MatchSaveGame(name.GetRawText(), (ushort) numSaveGames);
                        if (slot >= 0)
                        {
                            if (!ConfirmOverWrite(window))
                            {
                                ListSaveGames();
                                continue;
                            }
                        }
                        break;
                    }
                    if (i == 8)
                    {
                        // do_backspace
                        if (_saveGameNameLen == 0) continue;

                        _saveGameNameLen--;
                        name[_saveGameNameLen] = 0;
                        UserGameBackSpace(_windowArray[num], 8);
                    }
                    else if (i >= 32 && _saveGameNameLen != 8)
                    {
                        name[_saveGameNameLen++] = (byte) i;
                        WindowPutChar(_windowArray[num], (byte) i);
                    }
                }

                if (_saveGameNameLen != 0)
                {
                    if (slot < 0)
                        slot = (short) numSaveGames;

                    if (!SaveGame(slot, SaveBuf.GetRawText(192)))
                        FileError(_windowArray[num], true);
                }
            }
            else
            {
                i = UserGameGetKey(out b, 128);
                if (i != 225)
                {
                    if (!LoadGame(GenSaveName(_saveLoadRowCurPos + i)))
                        FileError(_windowArray[num], false);
                }
            }

            get_out:
            DisableFileBoxes();

            _gameStoppedClock = GetTime() - saveTime + _gameStoppedClock;

            if (GameType == SIMONGameType.GType_ELVIRA2)
                RestartAnimation();
        }

        protected override int WeightOf(Item x)
        {
            var o = (SubObject) FindChildOfType(x, ChildType.kObjectType);

            if ((o != null) && o.objectFlags.HasFlag(SubObjectFlags.kOFWeight))
            {
                var ct = GetOffsetOfChild2Param(o, (int) SubObjectFlags.kOFWeight);
                return o.objectFlagValue[ct];
            }

            return 0;
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

                uint color = (uint) (dst[0] & 0xF0);
                if (Features.HasFlag(GameFeatures.GF_PLANAR))
                {
                    src = _iconFilePtr;
                    src += src.ToInt32BigEndian(icon * 4);
                    DecompressIconPlanar(dst, src, 24, 12, (byte) color, (uint) screen.Pitch);
                }
                else
                {
                    src = _iconFilePtr;
                    src += src.ToUInt16(icon * 2);
                    DecompressIcon(dst, src, 24, 12, (byte) color, screen.Pitch);
                }
            });

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        protected override void AddArrows(WindowBlock window, byte num)
        {
            var h = FindEmptyHitArea();
            var ha = h.Value;
            _scrollUpHitArea = (ushort) h.Offset;

            SetBitFlag(21, true);
            ha.x = 54;
            ha.y = 154;
            ha.width = 12;
            ha.height = 10;
            ha.flags = BoxFlags.kBFBoxInUse;
            ha.id = 0x7FFB;
            ha.priority = 100;
            ha.window = window;
            ha.verb = 1;

            h = FindEmptyHitArea();
            ha = h.Value;
            _scrollDownHitArea = (ushort) h.Offset;

            ha.x = 54;
            ha.y = 178;
            ha.width = 12;
            ha.height = 10;
            ha.flags = BoxFlags.kBFBoxInUse;
            ha.id = 0x7FFC;
            ha.priority = 100;
            ha.window = window;
            ha.verb = 1;
            SetWindowImageEx(6, 106);
        }

        protected override int SizeOfRec(Item i, int d)
        {
            var o = (SubObject) FindChildOfType(i, ChildType.kObjectType);

            int ct;
            if ((o != null) && o.objectFlags.HasFlag(SubObjectFlags.kOFSoft))
            {
                if (o.objectFlags.HasFlag(SubObjectFlags.kOFSize))
                {
                    ct = GetOffsetOfChild2Param(o, (int) SubObjectFlags.kOFSize);
                    return o.objectFlagValue[ct] + SizeRec(i, d + 1);
                }
                return SizeRec(i, d + 1);
            }
            if ((o != null) && o.objectFlags.HasFlag(SubObjectFlags.kOFSize))
            {
                ct = GetOffsetOfChild2Param(o, (int) SubObjectFlags.kOFSize);
                return o.objectFlagValue[ct];
            }

            return 0;
        }

        private Item FindInByClass(Item i, short m)
        {
            i = DerefItem(i.child);
            while (i != null)
            {
                if ((i.classFlags & m) != 0)
                {
                    _findNextPtr = DerefItem(i.next);
                    return i;
                }
                if (m == 0)
                {
                    _findNextPtr = DerefItem(i.next);
                    return i;
                }
                i = DerefItem(i.next);
            }
            return null;
        }

        private void SetExitState(Item i, ushort n, ushort d, ushort s)
        {
            var sr = (SubSuperRoom) FindChildOfType(i, ChildType.kSuperRoomType);
            if (sr != null)
                ChangeExitStates(sr, n, d, s);
        }

        // Elvira 2 specific
        private int ChangeExitStates(SubSuperRoom sr, int n, int d, ushort s)
        {
            int b, bd;
            ushort mask;

            switch (d)
            {
                case 0:
                    b = -(sr.roomX);
                    bd = 2;
                    if (((n % (sr.roomX * sr.roomY)) / sr.roomX) == 0)
                        return 0;
                    break;
                case 1:
                    b = 1;
                    bd = 3;
                    if (((n % (sr.roomX * sr.roomY)) % sr.roomX) == 0)
                        return 0;
                    break;
                case 2:
                    b = sr.roomX;
                    bd = 0;
                    if (((n % (sr.roomX * sr.roomY)) / sr.roomX) == (sr.roomY - 1))
                        return 0;
                    break;
                case 3:
                    b = -1;
                    bd = 1;
                    if (((n % (sr.roomX * sr.roomY)) % sr.roomX) == 1)
                        return 0;
                    break;
                case 4:
                    b = -(sr.roomX * sr.roomY);
                    bd = 5;
                    if (n < (sr.roomX * sr.roomY))
                        return 0;
                    break;
                case 5:
                    b = sr.roomX * sr.roomY;
                    bd = 4;
                    if (n > (sr.roomX * sr.roomY * (sr.roomZ - 1)))
                        return 0;
                    else
                        break;
                default:
                    return 0;
            }

            n--;
            d <<= 1;
            mask = (ushort) (3 << d);
            sr.roomExitStates[n] = (ushort) (sr.roomExitStates[n] & ~mask);
            sr.roomExitStates[n] = (ushort) (sr.roomExitStates[n] | (s << d));

            bd <<= 1;
            mask = (ushort) (3 << bd);
            sr.roomExitStates[n + b] = (ushort) (sr.roomExitStates[n + b] & ~mask);
            sr.roomExitStates[n + b] = (ushort) (sr.roomExitStates[n + b] | (s << bd));
            return 1;
        }

        private void SetSrExit(Item i, int n, int d, ushort s)
        {
            ushort mask = 3;

            var sr = (SubSuperRoom) FindChildOfType(i, ChildType.kSuperRoomType);
            if (sr != null)
            {
                n--;
                d <<= 1;
                mask <<= d;
                s <<= d;
                sr.roomExitStates[n] = (ushort) (sr.roomExitStates[n] & ~mask);
                sr.roomExitStates[n] |= s;
            }
        }

        protected void oe2_moveDirn()
        {
            // 54: move direction
            var d = (short) GetVarOrByte();
            MoveDirn(Me(), (uint) d);
        }

        private void oe2_doClass()
        {
            // 72: do class
            Item i = GetNextItemPtr();
            byte cm = GetByte();
            short num = (short) GetVarOrWord();

            _classMask = (short) ((cm != 0xFF) ? 1 << cm : 0);
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

        private void oe2_pObj()
        {
            // 73: print object
            var subObject = (SubObject) FindChildOfType(GetNextItemPtr(), ChildType.kObjectType);

            if (subObject != null && subObject.objectFlags.HasFlag(SubObjectFlags.kOFText))
                ShowMessageFormat("{0}", GetStringPtrById((ushort) subObject.objectFlagValue[0]));
        }

        private void oe2_drawItem()
        {
            // 113: draw item
            Item i = GetNextItemPtr();
            int a = (int) GetVarOrByte();
            int x = (int) GetVarOrWord();
            int y = (int) GetVarOrWord();
            MouseOff();
            DrawIcon(_windowArray[a % 8], ItemGetIconNumber(i), x, y);
            MouseOn();
        }

        protected void oe2_ink()
        {
            // 160
            SetTextColor(GetVarOrByte());
        }

        private void oe2_printStats()
        {
            // 161: print stats
            PrintStats();
        }

        private void oe2_setSuperRoom()
        {
            // 165: set super room
            _superRoomNumber = (ushort) GetVarOrWord();
        }

        private void oe2_getSuperRoom()
        {
            // 166: get super room
            WriteNextVarContents(_superRoomNumber);
        }

        private void oe2_setExitOpen()
        {
            // 167: set exit open
            Item i = GetNextItemPtr();
            ushort n = (ushort) GetVarOrWord();
            ushort d = (ushort) GetVarOrByte();
            SetExitState(i, n, d, 1);
        }

        private void oe2_setExitClosed()
        {
            // 168: set exit closed
            Item i = GetNextItemPtr();
            ushort n = (ushort) GetVarOrWord();
            ushort d = (ushort) GetVarOrByte();
            SetExitState(i, n, d, 2);
        }

        private void oe2_setExitLocked()
        {
            // 169: set exit locked
            Item i = GetNextItemPtr();
            ushort n = (ushort) GetVarOrWord();
            ushort d = (ushort) GetVarOrByte();
            SetExitState(i, n, d, 3);
        }

        private void oe2_ifExitOpen()
        {
            // 171: if exit open
            Item i = GetNextItemPtr();
            ushort n = (ushort) GetVarOrWord();
            ushort d = (ushort) GetVarOrByte();
            SetScriptCondition(GetExitState(i, n, d) == 1);
        }

        private void oe2_ifExitClosed()
        {
            // 172: if exit closed
            Item i = GetNextItemPtr();
            ushort n = (ushort) GetVarOrWord();
            ushort d = (ushort) GetVarOrByte();
            SetScriptCondition(GetExitState(i, n, d) == 2);
        }

        private void oe2_ifExitLocked()
        {
            // 173: if exit locked
            Item i = GetNextItemPtr();
            ushort n = (ushort) GetVarOrWord();
            ushort d = (ushort) GetVarOrByte();
            SetScriptCondition(GetExitState(i, n, d) == 3);
        }

        private void oe2_playEffect()
        {
            // 174: play sound
            uint soundId = GetVarOrWord();
            LoadSound((ushort) soundId, 0, 0);
        }

        protected void oe2_doTable()
        {
            // 143: start item sub
            var i = GetNextItemPtr();

            var r = (SubRoom) FindChildOfType(i, ChildType.kRoomType);
            if (r != null)
            {
                var sub = GetSubroutineByID(r.subroutine_id);
                if (sub != null)
                {
                    StartSubroutine(sub);
                    return;
                }
            }

            if (GameType == SIMONGameType.GType_ELVIRA2)
            {
                var sr = (SubSuperRoom) FindChildOfType(i, ChildType.kSuperRoomType);
                if (sr != null)
                {
                    var sub = GetSubroutineByID(sr.subroutine_id);
                    if (sub != null)
                    {
                        StartSubroutine(sub);
                    }
                }
            }
        }

        protected void oe2_storeItem()
        {
            // 151: set array6 to item
            var var = GetVarOrByte();
            var item = GetNextItemPtr();
            _itemStore[var] = item;
        }

        protected void oe2_getItem()
        {
            // 152: set m1 to m3 to array 6
            var item = _itemStore[GetVarOrByte()];
            var var = GetVarOrByte();
            if (var == 1)
            {
                _subjectItem = item;
            }
            else
            {
                _objectItem = item;
            }
        }

        protected void oe2_bSet()
        {
            // 153: set bit
            SetBitFlag((int) GetVarWrapper(), true);
        }

        protected void oe2_bClear()
        {
            // 154: clear bit
            SetBitFlag((int) GetVarWrapper(), false);
        }

        protected void oe2_bZero()
        {
            // 155: is bit clear
            SetScriptCondition(!GetBitFlag((int) GetVarWrapper()));
        }

        protected void oe2_bNotZero()
        {
            // 156: is bit set
            var bit = (int) GetVarWrapper();

            // WORKAROUND: Enable copy protection again, in cracked version.
            if (GameType == SIMONGameType.GType_SIMON1 && _currentTable != null &&
                _currentTable.id == 2962 && bit == 63)
            {
                bit = 50;
            }

            SetScriptCondition(GetBitFlag(bit));
        }

        protected void oe2_getOValue()
        {
            // 157: get item int prop
            var item = GetNextItemPtr();
            var subObject = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            var prop = (int) GetVarOrByte();

            if (subObject != null && subObject.objectFlags.HasFlag((SubObjectFlags) (1 << prop)) && prop < 16)
            {
                var offs = GetOffsetOfChild2Param(subObject, 1 << prop);
                WriteNextVarContents((ushort) subObject.objectFlagValue[offs]);
            }
            else
            {
                WriteNextVarContents(0);
            }
        }

        protected void oe2_setOValue()
        {
            // 158: set item prop
            var item = GetNextItemPtr();
            var subObject = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            var prop = (int) GetVarOrByte();
            var value = (int) GetVarOrWord();

            if (subObject != null && subObject.objectFlags.HasFlag((SubObjectFlags) (1 << prop)) && prop < 16)
            {
                var offs = GetOffsetOfChild2Param(subObject, 1 << prop);
                subObject.objectFlagValue[offs] = (short) value;
            }
        }

        protected void oe2_getDollar2()
        {
            // 175
            _showPreposition = true;

            SetupCondCHelper();

            _objectItem = _hitAreaObjectItem;

            if (_objectItem == DummyItem2)
                _objectItem = Me();

            if (_objectItem == DummyItem3)
                _objectItem = DerefItem(Me().parent);

            if (_objectItem != null)
            {
                _scriptNoun2 = _objectItem.noun;
                _scriptAdj2 = _objectItem.adjective;
            }
            else
            {
                _scriptNoun2 = -1;
                _scriptAdj2 = -1;
            }

            _showPreposition = false;
        }

        private void oe2_setSRExit()
        {
            // 176: set super room exit
            Item i = GetNextItemPtr();
            uint n = GetVarOrWord();
            uint d = GetVarOrByte();
            uint s = GetVarOrByte();
            SetSrExit(i, (int) n, (int) d, (ushort) s);
        }

        private void oe2_printPlayerDamage()
        {
            // 177: set player damage event
            uint a = GetVarOrByte();
            if (_opcode177Var1 != 0 && _opcode177Var2 == 0 && a != 0 && a <= 10)
            {
                AddVgaEvent(_vgaBaseDelay, EventType.PLAYER_DAMAGE_EVENT, BytePtr.Null, 0, (ushort) a);
                _opcode177Var2 = 0;
                _opcode177Var1 = 0;
            }
        }

        private void oe2_printMonsterDamage()
        {
            // 178: set monster damage event
            uint a = GetVarOrByte();
            if (_opcode178Var1 != 0 && _opcode178Var2 == 0 && a != 0 && a <= 10)
            {
                AddVgaEvent(_vgaBaseDelay, EventType.MONSTER_DAMAGE_EVENT, BytePtr.Null, 0, (ushort) a);
                _opcode178Var2 = 0;
                _opcode178Var1 = 0;
            }
        }

        protected void oe2_isAdjNoun()
        {
            // 179: item unk1 unk2 is
            var item = GetNextItemPtr();
            var a = (short) GetNextWord();
            var n = (short) GetNextWord();

            if (GameType == SIMONGameType.GType_ELVIRA2 && item == null)
            {
                // WORKAROUND bug #1745996: A null item can occur when
                // interacting with items in the dinning room
                SetScriptCondition(false);
                return;
            }

            System.Diagnostics.Debug.Assert(item != null);
            SetScriptCondition(item.adjective == a && item.noun == n);
        }

        protected void oe2_b2Set()
        {
            // 180: set bit2
            var bit = (int) GetVarOrByte();
            _bitArrayTwo[bit / 16] = (ushort) (_bitArrayTwo[bit / 16] | (1 << (bit & 15)));
        }

        protected void oe2_b2Clear()
        {
            // 181: clear bit2
            var bit = (int) GetVarOrByte();
            _bitArrayTwo[bit / 16] = (ushort) (_bitArrayTwo[bit / 16] & ~(1 << (bit & 15)));
        }

        protected void oe2_b2Zero()
        {
            // 182: is bit2 clear
            var bit = (int) GetVarOrByte();
            SetScriptCondition((_bitArrayTwo[bit / 16] & (1 << (bit & 15))) == 0);
        }

        protected void oe2_b2NotZero()
        {
            // 183: is bit2 set
            var bit = (int) GetVarOrByte();
            SetScriptCondition((_bitArrayTwo[bit / 16] & (1 << (bit & 15))) != 0);
        }

        protected void oe2_menu()
        {
            // 105: set agos menu
            _agosMenu = (byte) GetVarOrByte();
        }

        protected void oe2_setDoorOpen()
        {
            // 144: set door open
            var i = GetNextItemPtr();
            SetDoorState(i, (ushort) GetVarOrByte(), 1);
        }

        protected void oe2_setDoorClosed()
        {
            // 145: set door closed
            var i = GetNextItemPtr();
            SetDoorState(i, (ushort) GetVarOrByte(), 2);
        }

        protected void oe2_setDoorLocked()
        {
            // 146: set door locked
            var i = GetNextItemPtr();
            SetDoorState(i, (ushort) GetVarOrByte(), 3);
        }

        protected void oe2_ifDoorOpen()
        {
            // 148: if door open
            var i = GetNextItemPtr();
            var d = (ushort) GetVarOrByte();

            if (GameType == SIMONGameType.GType_WW)
            {
                // WORKAROUND bug #2686883: A null item can occur when
                // walking through Jack the Ripper scene
                if (i == null)
                {
                    SetScriptCondition(false);
                    return;
                }
            }

            SetScriptCondition(GetDoorState(i, d) == 1);
        }

        protected void oe2_ifDoorClosed()
        {
            // 149: if door closed
            var i = GetNextItemPtr();
            var d = (ushort) GetVarOrByte();
            SetScriptCondition(GetDoorState(i, d) == 2);
        }

        protected void oe2_ifDoorLocked()
        {
            // 150: if door locked
            var i = GetNextItemPtr();
            var d = (ushort) GetVarOrByte();
            SetScriptCondition(GetDoorState(i, d) == 3);
        }

        protected override bool SaveGame(int slot, string caption)
        {
            int itemIndex, numItem, i;
            TimeEvent te;
            var curTime = GetTime();
            var gsc = _gameStoppedClock;

            _videoLockOut |= 0x100;

            var stream = OSystem.SaveFileManager.OpenForSaving(GenSaveName(slot));
            if (stream == null)
            {
                _videoLockOut = (ushort) (_videoLockOut & ~0x100);
                return false;
            }
            var f = new BinaryWriter(stream);
            if (GameType == SIMONGameType.GType_PP)
            {
                // No caption
            }
            else if (GameType == SIMONGameType.GType_FF)
            {
                f.WriteBytes(caption.GetBytes(), 100);
            }
            else if (GameType == SIMONGameType.GType_SIMON1 || GameType == SIMONGameType.GType_SIMON2)
            {
                f.WriteString(caption, 18);
            }
            else
            {
                f.WriteString(caption, 8);
            }

            f.WriteUInt32BigEndian((uint) (_itemArrayInited - 1));
            f.WriteUInt32BigEndian(0xFFFFFFFF);
            f.WriteUInt32BigEndian(curTime);
            f.WriteUInt32BigEndian(0);

            i = 0;
            for (te = _firstTimeStruct; te != null; te = te.next)
                i++;
            f.WriteUInt32BigEndian((uint) i);

            if (GameType == SIMONGameType.GType_FF && _clockStopped != 0)
                gsc += (GetTime() - _clockStopped);
            for (te = _firstTimeStruct; te != null; te = te.next)
            {
                f.WriteUInt32BigEndian(te.time - curTime + gsc);
                f.WriteUInt16BigEndian(te.subroutine_id);
            }

            if (GameType == SIMONGameType.GType_WW && GamePlatform == Platform.DOS)
            {
                if (_roomsListPtr != BytePtr.Null)
                {
                    var p = _roomsListPtr;
                    while (true)
                    {
                        var minNum = p.ToUInt16BigEndian();
                        p += 2;
                        if (minNum == 0)
                            break;

                        var maxNum = p.ToUInt16BigEndian();
                        p += 2;

                        for (var z = minNum; z <= maxNum; z++)
                        {
                            var itemNum = (ushort) (z + 2);
                            var item = DerefItem(itemNum);

                            var num = (ushort) (itemNum - _itemArrayInited);
                            _roomStates[num].state = (ushort) item.state;
                            _roomStates[num].classFlags = item.classFlags;
                            var subRoom = (SubRoom) FindChildOfType(item, ChildType.kRoomType);
                            _roomStates[num].roomExitStates = subRoom.roomExitStates;
                        }
                    }
                }

                for (var s = 0; s < _numRoomStates; s++)
                {
                    f.WriteUInt16BigEndian(_roomStates[s].state);
                    f.WriteUInt16BigEndian(_roomStates[s].classFlags);
                    f.WriteUInt16BigEndian(_roomStates[s].roomExitStates);
                }
                f.WriteUInt16BigEndian(0);
                f.WriteUInt16BigEndian(_currentRoom);
            }

            itemIndex = 1;
            for (numItem = _itemArrayInited - 1; numItem != 0; numItem--)
            {
                var item = _itemArrayPtr[itemIndex++];

                if ((GameType == SIMONGameType.GType_WW && GamePlatform == Platform.Amiga) ||
                    GameType == SIMONGameType.GType_ELVIRA2)
                {
                    WriteItemID(f, item.parent);
                }
                else
                {
                    f.WriteUInt16BigEndian(item.parent);
                    f.WriteUInt16BigEndian(item.next);
                }

                f.WriteUInt16BigEndian((ushort) item.state);
                f.WriteUInt16BigEndian(item.classFlags);

                var r = (SubRoom) FindChildOfType(item, ChildType.kRoomType);
                if (r != null)
                {
                    f.WriteUInt16BigEndian(r.roomExitStates);
                }

                var sr = (SubSuperRoom) FindChildOfType(item, ChildType.kSuperRoomType);
                int j;
                if (sr != null)
                {
                    var n = (ushort) (sr.roomX * sr.roomY * sr.roomZ);
                    for (i = j = 0; i != n; i++)
                        f.WriteUInt16BigEndian(sr.roomExitStates[j++]);
                }

                var o = (SubObject) FindChildOfType(item, ChildType.kObjectType);
                if (o != null)
                {
                    f.WriteUInt32BigEndian((uint) o.objectFlags);
                    i = (int) (o.objectFlags & (SubObjectFlags) 1);

                    for (j = 1; j < 16; j++)
                    {
                        if (((int) o.objectFlags & (1 << j)) != 0)
                        {
                            f.WriteUInt16BigEndian((ushort) o.objectFlagValue[i++]);
                        }
                    }
                }

                var u = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
                if (u != null)
                {
                    for (i = 0; i != 4; i++)
                    {
                        f.WriteUInt16BigEndian(u.userFlags[i]);
                    }
                }
            }

            // write the variables
            for (i = 0; i != _numVars; i++)
            {
                f.WriteUInt16BigEndian((ushort) ReadVariable((ushort) i));
            }

            // write the items in item store
            for (i = 0; i != _numItemStore; i++)
            {
                if (GameType == SIMONGameType.GType_WW && GamePlatform == Platform.Amiga)
                {
                    f.WriteUInt16BigEndian((ushort) (ItemPtrToID(_itemStore[i]) * 16));
                }
                else if (GameType == SIMONGameType.GType_ELVIRA2)
                {
                    if (GamePlatform == Platform.DOS)
                    {
                        WriteItemID(f, (ushort) ItemPtrToID(_itemStore[i]));
                    }
                    else
                    {
                        f.WriteUInt16BigEndian((ushort) (ItemPtrToID(_itemStore[i]) * 18));
                    }
                }
                else
                {
                    f.WriteUInt16BigEndian((ushort) ItemPtrToID(_itemStore[i]));
                }
            }

            // Write the bits in array 1
            for (i = 0; i != _numBitArray1; i++)
                f.WriteUInt16BigEndian(_bitArray[i]);

            // Write the bits in array 2
            for (i = 0; i != _numBitArray2; i++)
                f.WriteUInt16BigEndian(_bitArrayTwo[i]);

            // Write the bits in array 3
            for (i = 0; i != _numBitArray3; i++)
                f.WriteUInt16BigEndian(BitArrayThree[i]);

            if (GameType == SIMONGameType.GType_ELVIRA2 || GameType == SIMONGameType.GType_WW)
            {
                f.WriteUInt16BigEndian(_superRoomNumber);
            }

            f.Dispose();
            _videoLockOut = (ushort) (_videoLockOut & ~0x100);

            return true;
        }

        protected override bool LoadGame(string filename, bool restartMode = false)
        {
            int num, itemIndex, i;

            _videoLockOut |= 0x100;

            var file = restartMode ? OpenFileRead(filename) : OSystem.SaveFileManager.OpenForLoading(filename);
            if (file == null)
            {
                _videoLockOut = (ushort) (_videoLockOut & ~0x100);
                return false;
            }

            var f = new BinaryReader(file);
            if (GameType == SIMONGameType.GType_PP)
            {
                // No caption
            }
            else if (GameType == SIMONGameType.GType_FF)
            {
                f.ReadBytes(100);
            }
            else if (GameType == SIMONGameType.GType_SIMON1 || GameType == SIMONGameType.GType_SIMON2)
            {
                f.ReadBytes(18);
            }
            else if (!restartMode)
            {
                f.ReadBytes(8);
            }

            num = f.ReadInt32BigEndian();

            if (f.ReadUInt32BigEndian() != 0xFFFFFFFF || num != _itemArrayInited - 1)
            {
                f.Dispose();
                _videoLockOut = (ushort) (_videoLockOut & ~0x100);
                return false;
            }

            f.ReadUInt32BigEndian();
            f.ReadUInt32BigEndian();
            _noParentNotify = true;

            // add all timers
            KillAllTimers();
            for (num = (int) f.ReadUInt32BigEndian(); num != 0; num--)
            {
                var timeout = f.ReadUInt32BigEndian();
                var subroutineId = f.ReadUInt16BigEndian();
                AddTimeEvent((ushort) timeout, subroutineId);
            }

            if (GameType == SIMONGameType.GType_WW && GamePlatform == Platform.DOS)
            {
                for (uint s = 0; s < _numRoomStates; s++)
                {
                    _roomStates[s].state = f.ReadUInt16BigEndian();
                    _roomStates[s].classFlags = f.ReadUInt16BigEndian();
                    _roomStates[s].roomExitStates = f.ReadUInt16BigEndian();
                }
                f.ReadUInt16BigEndian();

                var room = _currentRoom;
                _currentRoom = f.ReadUInt16BigEndian();
                if (_roomsListPtr != BytePtr.Null)
                {
                    var p = _roomsListPtr;
                    if (room == _currentRoom)
                    {
                        for (;;)
                        {
                            var minNum = p.ToUInt16BigEndian();
                            p += 2;
                            if (minNum == 0)
                                break;

                            var maxNum = p.ToUInt16BigEndian();
                            p += 2;

                            for (var z = minNum; z <= maxNum; z++)
                            {
                                var itemNum = (ushort) (z + 2);
                                var item = DerefItem(itemNum);

                                num = itemNum - _itemArrayInited;
                                item.state = (short) _roomStates[num].state;
                                item.classFlags = _roomStates[num].classFlags;
                                var subRoom = (SubRoom) FindChildOfType(item, ChildType.kRoomType);
                                subRoom.roomExitStates = _roomStates[num].roomExitStates;
                            }
                        }
                    }
                    else
                    {
                        for (;;)
                        {
                            var minNum = p.ToUInt16BigEndian();
                            p += 2;
                            if (minNum == 0)
                                break;

                            var maxNum = p.ToUInt16BigEndian();
                            p += 2;

                            for (var z = minNum; z <= maxNum; z++)
                            {
                                var itemNum = (ushort) (z + 2);
                                _itemArrayPtr[itemNum] = null;
                            }
                        }
                    }
                }

                if (room != _currentRoom)
                {
                    _roomsListPtr = BytePtr.Null;
                    LoadRoomItems(_currentRoom);
                }
            }

            itemIndex = 1;
            for (num = _itemArrayInited - 1; num != 0; num--)
            {
                Item item = _itemArrayPtr[itemIndex++], parentItem;

                if ((GameType == SIMONGameType.GType_WW && GamePlatform == Platform.Amiga) ||
                    GameType == SIMONGameType.GType_ELVIRA2)
                {
                    parentItem = DerefItem(ReadItemID(f));
                    SetItemParent(item, parentItem);
                }
                else
                {
                    uint parent = f.ReadUInt16BigEndian();
                    uint next = f.ReadUInt16BigEndian();

                    if (GameType == SIMONGameType.GType_WW && GamePlatform == Platform.DOS &&
                        DerefItem(item.parent) == null)
                        item.parent = 0;

                    parentItem = DerefItem(parent);
                    SetItemParent(item, parentItem);

                    if (parentItem == null)
                    {
                        item.parent = (ushort) parent;
                        item.next = (ushort) next;
                    }
                }

                item.state = (short) f.ReadUInt16BigEndian();
                item.classFlags = f.ReadUInt16BigEndian();

                var r = (SubRoom) FindChildOfType(item, ChildType.kRoomType);
                if (r != null)
                {
                    r.roomExitStates = f.ReadUInt16BigEndian();
                }

                var sr = (SubSuperRoom) FindChildOfType(item, ChildType.kSuperRoomType);
                int j;
                if (sr != null)
                {
                    var n = (ushort) (sr.roomX * sr.roomY * sr.roomZ);
                    for (i = j = 0; i != n; i++)
                        sr.roomExitStates[j++] = f.ReadUInt16BigEndian();
                }

                var o = (SubObject) FindChildOfType(item, ChildType.kObjectType);
                if (o != null)
                {
                    o.objectFlags = (SubObjectFlags) f.ReadUInt32BigEndian();
                    i = (int) (o.objectFlags & (SubObjectFlags) 1);

                    for (j = 1; j < 16; j++)
                    {
                        if ((o.objectFlags & (SubObjectFlags) (1 << j)) != 0)
                        {
                            o.objectFlagValue[i++] = (short) f.ReadUInt16BigEndian();
                        }
                    }
                }

                var u = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
                if (u != null)
                {
                    for (i = 0; i != 4; i++)
                    {
                        u.userFlags[i] = f.ReadUInt16BigEndian();
                    }
                }
            }

            // read the variables
            for (i = 0; i != _numVars; i++)
            {
                WriteVariable((ushort) i, f.ReadUInt16BigEndian());
            }

            // read the items in item store
            for (i = 0; i != _numItemStore; i++)
            {
                if (GameType == SIMONGameType.GType_WW && GamePlatform == Platform.Amiga)
                {
                    _itemStore[i] = DerefItem((uint) (f.ReadUInt16BigEndian() / 16));
                }
                else if (GameType == SIMONGameType.GType_ELVIRA2)
                {
                    if (GamePlatform == Platform.DOS)
                    {
                        _itemStore[i] = DerefItem(ReadItemID(f));
                    }
                    else
                    {
                        _itemStore[i] = DerefItem((uint) (f.ReadUInt16BigEndian() / 18));
                    }
                }
                else
                {
                    _itemStore[i] = DerefItem(f.ReadUInt16BigEndian());
                }
            }

            // Read the bits in array 1
            for (i = 0; i != _numBitArray1; i++)
                _bitArray[i] = f.ReadUInt16BigEndian();

            // Read the bits in array 2
            for (i = 0; i != _numBitArray2; i++)
                _bitArrayTwo[i] = f.ReadUInt16BigEndian();

            // Read the bits in array 3
            for (i = 0; i != _numBitArray3; i++)
                BitArrayThree[i] = f.ReadUInt16BigEndian();

            if (GameType == SIMONGameType.GType_ELVIRA2 || GameType == SIMONGameType.GType_WW)
            {
                _superRoomNumber = f.ReadUInt16BigEndian();
            }

            f.Dispose();

            _noParentNotify = false;

            _videoLockOut = (ushort) (_videoLockOut & ~0x100);

            // The floppy disk versions of Simon the Sorcerer 2 block changing
            // to scrolling rooms, if the copy protection fails. But the copy
            // protection flags are never set in the CD version.
            // Setting this copy protection flag, allows saved games to be shared
            // between all versions of Simon the Sorcerer 2.
            if (GameType == SIMONGameType.GType_SIMON2)
            {
                SetBitFlag(135, true);
            }

            return true;
        }

        protected override bool HasIcon(Item item)
        {
            var child = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            return child != null && child.objectFlags.HasFlag(SubObjectFlags.kOFIcon);
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

            return Array.IndexOf(_hitAreas, ha.Value);
        }

        protected override int ItemGetIconNumber(Item item)
        {
            var child = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            if (child == null || !child.objectFlags.HasFlag(SubObjectFlags.kOFIcon))
                return 0;

            var offs = GetOffsetOfChild2Param(child, 0x10);
            return child.objectFlagValue[offs];
        }

        protected override void ReadItemChildren(BinaryReader br, Item item, ChildType type)
        {
            if (type == ChildType.kRoomType)
            {
                int fr1 = br.ReadUInt16BigEndian();
                int fr2 = br.ReadUInt16BigEndian();
                int i;
                int j, k;

                var numRoomExit = 0;
                for (i = 0, j = fr2; i != 6; i++, j >>= 2)
                    if ((j & 3) != 0)
                        numRoomExit++;

                var subRoom = AllocateChildBlock<SubRoom>(item, ChildType.kRoomType);
                subRoom.subroutine_id = (ushort) fr1;
                subRoom.roomExitStates = (ushort) fr2;
                subRoom.roomExit = new ushort[numRoomExit];

                for (i = k = 0, j = fr2; i != 6; i++, j >>= 2)
                    if ((j & 3) != 0)
                        subRoom.roomExit[k++] = (ushort) FileReadItemID(br);
            }
            else if (type == ChildType.kObjectType)
            {
                var fr = br.ReadInt32BigEndian();
                int i;

                var subObject = AllocateChildBlock<SubObject>(item, ChildType.kObjectType);
                subObject.objectFlags = (SubObjectFlags) fr;

                if ((fr & 1) != 0)
                {
                    subObject.objectFlagValue.Add((short) br.ReadUInt32BigEndian());
                }
                for (i = 1; i != 16; i++)
                    if ((fr & (1 << i)) != 0)
                        subObject.objectFlagValue.Add(br.ReadInt16BigEndian());

                if (_gd.ADGameDescription.gameType != SIMONGameType.GType_ELVIRA2)
                    subObject.objectName = (ushort) br.ReadUInt32BigEndian();
            }
            else if (type == ChildType.kSuperRoomType)
            {
                System.Diagnostics.Debug.Assert(_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2);

                int i, j, k;
                int id, x, y, z;

                id = br.ReadUInt16BigEndian();
                x = br.ReadUInt16BigEndian();
                y = br.ReadUInt16BigEndian();
                z = br.ReadUInt16BigEndian();

                j = x * y * z;

                var subSuperRoom = AllocateChildBlock<SubSuperRoom>(item, ChildType.kSuperRoomType);
                subSuperRoom.subroutine_id = (ushort) id;
                subSuperRoom.roomX = (ushort) x;
                subSuperRoom.roomY = (ushort) y;
                subSuperRoom.roomZ = (ushort) z;

                subSuperRoom.roomExitStates = new ushort[j];
                for (i = k = 0; i != j; i++)
                    subSuperRoom.roomExitStates[k++] = br.ReadUInt16BigEndian();
            }
            else if (type == ChildType.kContainerType)
            {
                var container = AllocateChildBlock<SubContainer>(item, ChildType.kContainerType);
                container.volume = br.ReadUInt16BigEndian();
                container.flags = br.ReadUInt16BigEndian();
            }
            else if (type == ChildType.kChainType)
            {
                var chain = AllocateChildBlock<SubChain>(item, ChildType.kChainType);
                chain.chChained = (ushort) FileReadItemID(br);
            }
            else if (type == ChildType.kUserFlagType)
            {
                SetUserFlag(item, 0, br.ReadUInt16BigEndian());
                SetUserFlag(item, 1, br.ReadUInt16BigEndian());
                SetUserFlag(item, 2, br.ReadUInt16BigEndian());
                SetUserFlag(item, 3, br.ReadUInt16BigEndian());
            }
            else if (type == ChildType.kInheritType)
            {
                var inherit = AllocateChildBlock<SubInherit>(item, ChildType.kInheritType);
                inherit.inMaster = (ushort) FileReadItemID(br);
            }
            else
            {
                Error("readItemChildren: invalid type {0}", type);
            }
        }

        protected override void ExecuteOpcode(int opcode)
        {
            _opcodes[opcode]();
        }

        private int UserGameGetKey(out bool b, uint maxChar)
        {
            b = true;

            _keyPressed = new ScummInputState();

            while (!HasToQuit)
            {
                _lastHitArea = null;
                _lastHitArea3 = null;

                do
                {
                    var c = ToChar(_keyPressed.GetKeys().FirstOrDefault());
                    if (_saveLoadEdit && c != 0 && c < maxChar)
                    {
                        b = false;
                        return c;
                    }
                    Delay(10);
                } while (_lastHitArea3 == null && !HasToQuit);

                var ha = _lastHitArea;
                if (ha == null || ha.id < 200)
                {
                }
                else if (ha.id == 225)
                {
                    return ha.id;
                }
                else if (ha.id == 224)
                {
                    _saveGameNameLen = 0;
                    _saveLoadRowCurPos += 24;
                    if (_saveLoadRowCurPos >= _numSaveGameRows)
                        _saveLoadRowCurPos = 1;

                    ListSaveGames();
                }
                else if (ha.id < 224)
                {
                    return ha.id - 200;
                }
            }

            return 225;
        }

        private void ListSaveGames()
        {
            uint y, slot;
            BytePtr dst = SaveBuf;

            byte num = (byte) (GameType == SIMONGameType.GType_WW ? 3 : 4);

            DisableFileBoxes();

            WindowBlock window = _windowArray[num];
            window.textRow = 0;
            window.textColumn = 0;
            window.textColumnOffset = 4;

            WindowPutChar(window, 12);

            Array.Clear(dst.Data, 0, 200);

            slot = _saveLoadRowCurPos;
            for (y = 0; y < 8; y++)
            {
                window.textColumn = 0;
                window.textColumnOffset = (ushort) ((GameType == SIMONGameType.GType_ELVIRA2) ? 4 : 0);
                window.textLength = 0;
                Stream @in = OSystem.SaveFileManager.OpenForLoading(GenSaveName((int) slot++));
                if (@in != null)
                {
                    @in.Read(dst.Data, dst.Offset, 8);
                    @in.Dispose();

                    var name = dst;
                    foreach (var c in name)
                    {
                        if (c == 0) break;
                        WindowPutChar(window, c);
                    }

                    EnableBox((int) (200 + y * 3 + 0));
                }
                dst.Offset += 8;

                if (GameType == SIMONGameType.GType_WW)
                {
                    window.textColumn = 7;
                    window.textColumnOffset = 4;
                }
                else if (GameType == SIMONGameType.GType_ELVIRA2)
                {
                    window.textColumn = 8;
                    window.textColumnOffset = 0;
                }
                window.textLength = 0;
                @in = OSystem.SaveFileManager.OpenForLoading(GenSaveName((int) slot++));
                if (@in != null)
                {
                    @in.Read(dst.Data, dst.Offset, 8);
                    @in.Dispose();

                    var name = dst;
                    foreach (var c in name)
                    {
                        if (c == 0) break;
                        WindowPutChar(window, c);
                    }

                    EnableBox((int) (200 + y * 3 + 1));
                }
                dst.Offset += 8;

                window.textColumn = 15;
                window.textColumnOffset = (ushort) ((GameType == SIMONGameType.GType_ELVIRA2) ? 4 : 0);
                window.textLength = 0;
                @in = OSystem.SaveFileManager.OpenForLoading(GenSaveName((int) slot++));
                if (@in != null)
                {
                    @in.Read(dst.Data, dst.Offset, 8);
                    @in.Dispose();

                    var name = dst;
                    foreach (var c in name)
                    {
                        if (c == 0) break;
                        WindowPutChar(window, c);
                    }

                    EnableBox((int) (200 + y * 3 + 2));
                }
                dst += 8;

                WindowPutChar(window, 13);
            }

            window.textRow = 9;
            window.textColumn = 0;
            window.textColumnOffset = 4;
            window.textLength = 0;

            _saveGameNameLen = 0;
        }

        protected override void RemoveArrows(WindowBlock window, int num)
        {
            SetBitFlag(21, false);
            SetWindowImageEx(6, 106);
        }

        protected virtual void MoveDirn(Item i, uint x)
        {
            ushort n;

            if (i.parent == 0)
                return;

            var p = DerefItem(i.parent);
            if (FindChildOfType(p, ChildType.kSuperRoomType) != null)
            {
                n = GetExitState(p, _superRoomNumber, (ushort) x);
                if (n == 1)
                {
                    var sr = (SubSuperRoom) FindChildOfType(p, ChildType.kSuperRoomType);
                    ushort a;
                    switch (x)
                    {
                        case 0:
                            a = (ushort) -sr.roomX;
                            break;
                        case 1:
                            a = 1;
                            break;
                        case 2:
                            a = sr.roomX;
                            break;
                        case 3:
                            a = 0xFFFF;
                            break;
                        case 4:
                            a = (ushort) -(sr.roomX * sr.roomY);
                            break;
                        case 5:
                            a = (ushort) (sr.roomX * sr.roomY);
                            break;
                        default:
                            return;
                    }
                    _superRoomNumber += a;
                }
                return;
            }

            n = GetExitOf(DerefItem(i.parent), (ushort) x);

            var d = DerefItem(n);
            if (d != null)
            {
                n = GetDoorState(DerefItem(i.parent), (ushort) x);
                if (n == 1)
                {
                    if (CanPlace(i, d) == 0)
                        SetItemParent(i, d);
                }
            }
        }

        protected override bool ConfirmOverWrite(WindowBlock window)
        {
            // Original verison never confirmed
            return true;
        }

        protected override string GenSaveName(int slot)
        {
            if (GamePlatform == Platform.DOS)
                return $"elvira2-pc.{slot:D3}";
            return $"elvira2.{slot:D3}";
        }

        protected override void PrintStats()
        {
            var window = DummyWindow;
            int val;
            byte y = (byte) (GamePlatform == Platform.AtariST ? 132 : 134);

            window.flags = 1;

            MouseOff();

            // Level
            val = _variableArray[20];
            if (val < -99)
                val = -99;
            if (val > 99)
                val = 99;
            WriteChar(window, 10, y, 0, val);

            // PP
            val = _variableArray[22];
            if (val < -99)
                val = -99;
            if (val > 99)
                val = 99;
            WriteChar(window, 16, y, 6, val);

            // HP
            val = _variableArray[23];
            if (val < -99)
                val = -99;
            if (val > 99)
                val = 99;
            WriteChar(window, 23, y, 4, val);

            // Experience
            val = _variableArray[21];
            if (val < -99)
                val = -99;
            if (val > 9999)
                val = 9999;
            WriteChar(window, 30, y, 6, val / 100);
            WriteChar(window, 32, y, 2, val % 100);

            MouseOn();
        }

        private ushort GetExitState(Item i, ushort x, ushort d)
        {
            ushort mask = 3;
            ushort n;

            var sr = (SubSuperRoom) FindChildOfType(i, ChildType.kSuperRoomType);
            if (sr == null)
                return 0;

            d <<= 1;
            mask <<= d;
            n = (ushort) (sr.roomExitStates[x - 1] & mask);
            n >>= d;
            return n;
        }

        protected void vc58_checkCodeWheel()
        {
            _variableArray[0] = 0;
        }

        protected void vc61()
        {
            LockScreen(screen =>
            {
                var a = (ushort) VcReadNextWord();

                BytePtr src, dst;
                int tmp;

                var dstPtr = screen.Pixels;

                if (a == 6)
                {
                    src = _curVgaFile2 + 800;
                    dst = dstPtr;

                    for (var i = 0; i < _screenHeight; i++)
                    {
                        src.Copy(dst, _screenWidth);
                        src += 320;
                        dst += screen.Pitch;
                    }

                    tmp = 4 - 1;
                }
                else
                {
                    tmp = a - 1;
                }

                src = _curVgaFile2 + 3840 * 16 + 3360;
                while (tmp-- != 0)
                    src += 1536 * 16 + 1712;

                src += 800;

                if (a != 5)
                {
                    dst = dstPtr + 23 * screen.Pitch + 88;
                    for (var h = 0; h < 177; h++)
                    {
                        src.Copy(dst, 144);
                        src += 144;
                        dst += screen.Pitch;
                    }

                    if (a != 6)
                    {
                        return;
                    }

                    src = _curVgaFile2 + 9984 * 16 + 15344;
                }

                dst = dstPtr + 157 * screen.Pitch + 56;
                for (var h = 0; h < 17; h++)
                {
                    src.Copy(dst, 208);
                    src += 208;
                    dst += screen.Pitch;
                }

                if (a == 6)
                    FullFade();
            });
        }

        private void FullFade()
        {
            for (var c = 64; c != 0; c--)
            {
                var srcPal = _curVgaFile2 + 32;
                Ptr<Color> dstPal = _currentPalette;
                for (var p = 768; p != 0; p -= 3)
                {
                    var r = (byte) (srcPal[0] * 4);
                    var g = (byte) (srcPal[1] * 4);
                    var b = (byte) (srcPal[2] * 4);
                    dstPal[0] = Color.FromRgb(
                        dstPal.Value.R + (dstPal.Value.R != r ? 4 : 0),
                        dstPal.Value.G + (dstPal.Value.G != g ? 4 : 0),
                        dstPal.Value.B + (dstPal.Value.B != b ? 4 : 0));
                    srcPal += 3;
                    dstPal.Offset++;
                }
                OSystem.GraphicsManager.SetPalette(_currentPalette, 0, 256);
                Delay(5);
            }
        }
    }
}