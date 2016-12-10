//
//  AGOSEngine.Input.cs
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

using NScumm.Core;
using NScumm.Core.Audio;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        private bool _ambientPaused;

        private uint SetVerbText(HitArea ha)
        {
            uint id = 0xFFFF;

            if (GameType == SIMONGameType.GType_ELVIRA1 || GameType == SIMONGameType.GType_ELVIRA2)
                return id;

            if (ha.flags.HasFlag(BoxFlags.kBFTextBox))
            {
                if (GameType == SIMONGameType.GType_PP)
                    id = ha.id;
                else if (GameType == SIMONGameType.GType_FF && ha.flags.HasFlag(BoxFlags.kBFHyperBox))
                    id = ha.data;
                else
                    id = (uint) ((int) ha.flags / 256);
            }
            if (GameType == SIMONGameType.GType_PP)
                _variableArray[199] = (short) id;
            else if (GameType == SIMONGameType.GType_WW)
                _variableArray[10] = (short) id;
            else
                _variableArray[60] = (short) id;

            return id;
        }

        protected void SetupCondCHelper()
        {
            _noRightClick = true;

            if (GameType == SIMONGameType.GType_WW)
                ClearMenuStrip();

            if (GameType == SIMONGameType.GType_FF)
            {
                int cursor = 5;
                int animMax = 16;

                if (GetBitFlag(200))
                {
                    cursor = 11;
                    animMax = 5;
                }
                else if (GetBitFlag(201))
                {
                    cursor = 12;
                    animMax = 5;
                }
                else if (GetBitFlag(202))
                {
                    cursor = 13;
                    animMax = 5;
                }
                else if (GetBitFlag(203))
                {
                    cursor = 14;
                    animMax = 9;
                }
                else if (GetBitFlag(205))
                {
                    cursor = 17;
                    animMax = 11;
                }
                else if (GetBitFlag(206))
                {
                    cursor = 16;
                    animMax = 2;
                }
                else if (GetBitFlag(208))
                {
                    cursor = 26;
                    animMax = 2;
                }
                else if (GetBitFlag(209))
                {
                    cursor = 27;
                    animMax = 9;
                }
                else if (GetBitFlag(210))
                {
                    cursor = 28;
                    animMax = 9;
                }

                _animatePointer = false;
                _mouseCursor = (byte) cursor;
                _mouseAnimMax = (byte) animMax;
                _mouseAnim = 1;
                _needHitAreaRecalc++;
            }

            if (GameType == SIMONGameType.GType_SIMON2)
            {
                _mouseCursor = 0;
                if (_defaultVerb != 999)
                {
                    _mouseCursor = 9;
                    _needHitAreaRecalc++;
                    _defaultVerb = 0;
                }
            }

            _lastHitArea = null;
            _hitAreaObjectItem = null;
            _nameLocked = false;

            var last = _lastNameOn;
            ClearName();
            _lastNameOn = last;

            while (!HasToQuit)
            {
                _lastHitArea = null;
                _lastHitArea3 = null;
                _leftButtonDown = false;

                do
                {
                    if (_exitCutscene && GetBitFlag(9))
                    {
                        EndCutscene();
                        goto out_of_here;
                    }

                    if (GameType == SIMONGameType.GType_FF)
                    {
                        if (_variableArray[254] == 63)
                        {
                            HitareaStuffHelper2();
                        }
                        else if (_variableArray[254] == 75)
                        {
                            HitareaStuffHelper2();
                            _variableArray[60] = 9999;
                            goto out_of_here;
                        }
                    }

                    Delay(100);
                } while ((_lastHitArea3 == HitArea.None || _lastHitArea3 == null) && !HasToQuit);

                if (_lastHitArea == null)
                {
                }
                else if (_lastHitArea.id == 0x7FFB)
                {
                    InventoryUp(_lastHitArea.window);
                }
                else if (_lastHitArea.id == 0x7FFC)
                {
                    InventoryDown(_lastHitArea.window);
                }
                else if (_lastHitArea.itemPtr != null)
                {
                    _hitAreaObjectItem = _lastHitArea.itemPtr;
                    SetVerbText(_lastHitArea);
                    break;
                }
            }

            out_of_here:
            _lastHitArea3 = null;
            _lastHitArea = null;
            _lastNameOn = null;

            _mouseCursor = 0;
            _noRightClick = false;
        }

        private void WaitForInput()
        {
            _leftButtonDown = false;
            _lastHitArea = null;
            //_lastClickRem = 0;
            _verbHitArea = 0;
            _hitAreaSubjectItem = null;
            _hitAreaObjectItem = null;
            _clickOnly = false;
            _nameLocked = false;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
            {
                _mouseCursor = 0;
                _needHitAreaRecalc++;
                ClearMenuStrip();
            }
            else
            {
                ResetVerbs();
            }

            while (!HasToQuit)
            {
                _lastHitArea = null;
                _lastHitArea3 = null;
                _dragAccept = true;

                while (!HasToQuit)
                {
                    if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                         _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) &&
                        _keyPressed.IsKeyDown(KeyCode.F10))
                        DisplayBoxStars();
                    if (ProcessSpecialKeys())
                    {
                        if (_gd.ADGameDescription.gameId != GameIds.GID_DIMP)
                            goto out_of_here;
                    }

                    if (_lastHitArea3 == HitArea.None)
                    {
                        _lastHitArea = null;
                        _lastHitArea3 = null;
                        _dragAccept = true;
                    }
                    else
                    {
                        if (_lastHitArea3 != null || _dragMode)
                            break;
                        HitareaStuffHelper();
                        Delay(100);
                    }
                }

                HitArea ha;
                if (_lastHitArea3 == null && _dragMode)
                {
                    ha = _lastClickRem;

                    if (ha == null || ha.itemPtr == null || ha.flags.HasFlag(BoxFlags.kBFDragBox))
                    {
                        _dragFlag = false;
                        _dragMode = false;
                        _dragCount = 0;
                        _dragEnd = false;
                        continue;
                    }

                    _hitAreaSubjectItem = ha.itemPtr;
                    _verbHitArea = 500;

                    do
                    {
                        ProcessSpecialKeys();
                        HitareaStuffHelper();
                        Delay(100);

                        if (!_dragFlag)
                        {
                            _dragFlag = false;
                            _dragMode = false;
                            _dragCount = 0;
                            _dragEnd = false;
                        }
                    } while (!_dragEnd);

                    _dragFlag = false;
                    _dragMode = false;
                    _dragCount = 0;
                    _dragEnd = false;

                    BoxController((uint) _mouse.X, (uint) _mouse.Y, 1);

                    if (_currentBox != null)
                    {
                        _hitAreaObjectItem = _currentBox.itemPtr;
                        SetVerbText(_currentBox);
                    }

                    break;
                }

                ha = _lastHitArea;
                if (ha == null)
                {
                }
                else if (ha.id == 0x7FFB)
                {
                    InventoryUp(ha.window);
                }
                else if (ha.id == 0x7FFC)
                {
                    InventoryDown(ha.window);
                }
                else if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                          _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) &&
                         (ha.id >= 101 && ha.id < 113))
                {
                    _verbHitArea = ha.verb;
                    SetVerb(ha);
                    _defaultVerb = 0;
                }
                else
                {
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                    {
                        if (_mouseCursor == 3)
                            _verbHitArea = 236;

                        if (ha.id == 98)
                        {
                            Animate(2, 1, 110, 0, 0, 0);
                            WaitForSync(34);
                        }
                        else if (ha.id == 108)
                        {
                            Animate(2, 1, 106, 0, 0, 0);
                            WaitForSync(34);
                        }
                        else if (ha.id == 109)
                        {
                            Animate(2, 1, 107, 0, 0, 0);
                            WaitForSync(34);
                        }
                        else if (ha.id == 115)
                        {
                            Animate(2, 1, 109, 0, 0, 0);
                            WaitForSync(34);
                        }
                        else if (ha.id == 116)
                        {
                            Animate(2, 1, 113, 0, 0, 0);
                            WaitForSync(34);
                        }
                        else if (ha.id == 117)
                        {
                            Animate(2, 1, 112, 0, 0, 0);
                            WaitForSync(34);
                        }
                        else if (ha.id == 118)
                        {
                            Animate(2, 1, 108, 0, 0, 0);
                            WaitForSync(34);
                        }
                        else if (ha.id == 119)
                        {
                            Animate(2, 1, 111, 0, 0, 0);
                            WaitForSync(34);
                        }
                    }
                    if (ha.itemPtr != null && (ha.verb == 0 || _verbHitArea != 0 ||
                                               _hitAreaSubjectItem != ha.itemPtr &&
                                               ha.flags.HasFlag(BoxFlags.kBFBoxItem)))
                    {
                        _hitAreaSubjectItem = ha.itemPtr;
                        var id = SetVerbText(ha);
                        _nameLocked = false;
                        DisplayName(ha);
                        _nameLocked = true;

                        if (_verbHitArea != 0)
                        {
                            break;
                        }

                        if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                            DoMenuStrip(menuFor_ww(ha.itemPtr, id));
                        else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                            DoMenuStrip(menuFor_e2(ha.itemPtr));
                        else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                            LightMenuStrip(GetUserFlag1(ha.itemPtr, 6));
                    }
                    else
                    {
                        if (ha.verb != 0)
                        {
                            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW && _mouseCursor != 0 &&
                                _mouseCursor < 4)
                            {
                                _hitAreaSubjectItem = ha.itemPtr;
                                break;
                            }

                            _verbHitArea = (ushort) (ha.verb & 0xBFFF);
                            if ((ha.verb & 0x4000) != 0)
                            {
                                _hitAreaSubjectItem = ha.itemPtr;
                                break;
                            }
                            if (_hitAreaSubjectItem != null)
                                break;

                            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                            {
                                if (ha.id == 109)
                                {
                                    _mouseCursor = 2;
                                    _needHitAreaRecalc++;
                                }
                                else if (ha.id == 117)
                                {
                                    _mouseCursor = 3;
                                    _needHitAreaRecalc++;
                                }
                            }
                        }
                    }
                }
            }

            out_of_here:
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                ClearMenuStrip();
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                UnlightMenuStrip();

            _nameLocked = false;
            _needHitAreaRecalc++;
            _dragAccept = false;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW && _mouseCursor < 3)
                _mouseCursor = 0;
        }

        private void HitareaStuffHelper()
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                if (_variableArray[254] != 0 || _variableArray[249] != 0)
                {
                    HitareaStuffHelper2();
                }
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_WW ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
            {
                uint subr_id = (ushort) _variableArray[254];
                if (subr_id != 0)
                {
                    Subroutine sub = GetSubroutineByID(subr_id);
                    if (sub != null)
                    {
                        StartSubroutineEx(sub);
                        PermitInput();
                    }
                    _variableArray[254] = 0;
                    _runScriptReturn1 = false;
                }
            }

            uint cur_time = GetTime();
            if (cur_time != _lastTime)
            {
                _lastTime = cur_time;
                if (KickoffTimeEvents())
                    PermitInput();
            }

            if (_gd.ADGameDescription.gameId == GameIds.GID_DIMP)
                Delay(200);
        }

        private void HitareaStuffHelper2()
        {
            uint subr_id;
            Subroutine sub;

            subr_id = (ushort) _variableArray[249];
            if (subr_id != 0)
            {
                sub = GetSubroutineByID(subr_id);
                if (sub != null)
                {
                    _variableArray[249] = 0;
                    StartSubroutineEx(sub);
                    PermitInput();
                }
                _variableArray[249] = 0;
            }

            subr_id = (ushort) _variableArray[254];
            if (subr_id != 0)
            {
                sub = GetSubroutineByID(subr_id);
                if (sub != null)
                {
                    _variableArray[254] = 0;
                    StartSubroutineEx(sub);
                    PermitInput();
                }
                _variableArray[254] = 0;
            }

            _runScriptReturn1 = false;
        }

        private void PermitInput()
        {
            if (_mortalFlag)
                return;

            _mortalFlag = true;
            JustifyOutPut(0);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
            {
                int n = 0;
                while (n < 8)
                {
                    if ((_fcsData1[n] != 0) && (_windowArray[n] != null) && (_windowArray[n].flags & 128) != 0)
                    {
                        _textWindow = _windowArray[n];
                        WaitWindow(_textWindow);
                        ClsCheck(_textWindow);
                    }
                    _fcsData1[n] = 0;
                    n++;
                }

                RestartAnimation();
            }

            _curWindow = 0;
            if (_windowArray[0] != null)
            {
                _textWindow = _windowArray[0];
                JustifyStart();
            }
            _mortalFlag = false;
        }

        private bool ProcessSpecialKeys()
        {
            bool verbCode = false;

            if (_gd.ADGameDescription.gameId == GameIds.GID_DIMP)
            {
                uint t1 = GetTime() / 30;
                if (_lastMinute == 0)
                    _lastMinute = t1;
                if ((t1 - _lastMinute) != 0)
                {
                    _variableArray[120] = (short) (_variableArray[120] + (t1 - _lastMinute));
                    _lastMinute = t1;
                }
            }

            if (HasToQuit)
                _exitCutscene = true;

            var keys = OSystem.InputManager.GetState();
            if (keys.IsKeyDown(KeyCode.Up))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                    _verbHitArea = 302;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                    _verbHitArea = 239;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 && IsBoxDead(101))
                    _verbHitArea = 200;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 && IsBoxDead(101))
                    _verbHitArea = 214;
                verbCode = true;
            }
            if (keys.IsKeyDown(KeyCode.Down))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                    _verbHitArea = 304;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                    _verbHitArea = 241;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 && IsBoxDead(107))
                    _verbHitArea = 202;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 && IsBoxDead(105))
                    _verbHitArea = 215;
                verbCode = true;
            }
            if (keys.IsKeyDown(KeyCode.Right))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                    _verbHitArea = 303;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                    _verbHitArea = 240;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 && IsBoxDead(102))
                    _verbHitArea = 201;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 && IsBoxDead(103))
                    _verbHitArea = 216;
                verbCode = true;
            }
            if (keys.IsKeyDown(KeyCode.Left))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                    _verbHitArea = 301;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                    _verbHitArea = 242;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 && IsBoxDead(104))
                    _verbHitArea = 203;
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 && IsBoxDead(107))
                    _verbHitArea = 217;
                verbCode = true;
            }
            if (keys.IsKeyDown(KeyCode.Escape))
            {
                _exitCutscene = true;
            }
            if (keys.IsKeyDown(KeyCode.F1))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                {
                    VcWriteVar(5, 50);
                    VcWriteVar(86, 0);
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
                {
                    VcWriteVar(5, 40);
                    VcWriteVar(86, 0);
                }
            }
            if (keys.IsKeyDown(KeyCode.F2))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                {
                    VcWriteVar(5, 75);
                    VcWriteVar(86, 1);
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
                {
                    VcWriteVar(5, 60);
                    VcWriteVar(86, 1);
                }
            }
            if (keys.IsKeyDown(KeyCode.F3))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                {
                    VcWriteVar(5, 125);
                    VcWriteVar(86, 2);
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
                {
                    VcWriteVar(5, 100);
                    VcWriteVar(86, 2);
                }
            }
            if (keys.IsKeyDown(KeyCode.F5))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                    _exitCutscene = true;
            }
            if (keys.IsKeyDown(KeyCode.F7))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF && GetBitFlag(76))
                    _variableArray[254] = 70;
            }
            if (keys.IsKeyDown(KeyCode.F9))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                    SetBitFlag(73, !GetBitFlag(73));
            }
            if (keys.IsKeyDown(KeyCode.F12))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP &&
                    _gd.ADGameDescription.gameId != GameIds.GID_DIMP)
                {
                    if (!GetBitFlag(110))
                    {
                        SetBitFlag(107, !GetBitFlag(107));
                        _vgaPeriod = (byte) ((GetBitFlag(107) != false) ? 15 : 30);
                    }
                }
            }
            if (keys.IsKeyDown(KeyCode.Pause))
            {
                Pause();
            }

            if (keys.IsKeyDown(KeyCode.T))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 &&
                    _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE) ||
                    (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE) &&
                     _language != Language.EN_ANY && _language != Language.DE_DEU))
                {
                    if (_speech)
                        _subtitles = !_subtitles;
                }
            }
            if (keys.IsKeyDown(KeyCode.V))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                    (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 &&
                     (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))))
                {
                    if (_subtitles)
                        _speech = !_speech;
                }
            }
            if (keys.IsKeyDown(KeyCode.Plus))
            {
                if (_midiEnabled)
                {
                    _midi.SetVolume(_midi.MusicVolume + 16, _midi.SfxVolume + 16);
                }
                ConfigManager.Instance.Set<int>("music_volume", Mixer.GetVolumeForSoundType(SoundType.Music) + 16);
                SyncSoundSettings();
            }
            if (keys.IsKeyDown(KeyCode.Minus))
            {
                if (_midiEnabled)
                {
                    _midi.SetVolume(_midi.MusicVolume - 16, _midi.SfxVolume - 16);
                }
                ConfigManager.Instance.Set<int>("music_volume", Mixer.GetVolumeForSoundType(SoundType.Music) - 16);
                SyncSoundSettings();
            }
            if (keys.IsKeyDown(KeyCode.M))
            {
                _musicPaused = !_musicPaused;
                if (_midiEnabled)
                {
                    _midi.Pause(_musicPaused);
                }
                Mixer.PauseHandle(_modHandle, _musicPaused);
                SyncSoundSettings();
            }
            if (keys.IsKeyDown(KeyCode.S))
            {
                if (GameId == GameIds.GID_SIMON1DOS)
                {
                    _midi._enable_sfx = !_midi._enable_sfx;
                }
                else
                {
                    _effectsPaused = !_effectsPaused;
                    _sound.EffectsPause(_effectsPaused);
                }
            }
            if (keys.IsKeyDown(KeyCode.B))
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                {
                    _ambientPaused = !_ambientPaused;
                    _sound.AmbientPause(_ambientPaused);
                }
            }

            return verbCode;
        }

        protected void WaitForSync(uint a)
        {
            int maxCount = _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ? 1000 : 2500;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
            {
                if (a != 200)
                {
                    ushort tmp = _lastVgaWaitFor;
                    _lastVgaWaitFor = 0;
                    if (tmp == a)
                        return;
                }
            }

            _vgaWaitFor = (ushort) a;
            _syncCount = 0;
            _exitCutscene = false;
            _rightButtonDown = false;

            while (_vgaWaitFor != 0 && !HasToQuit)
            {
                if (_rightButtonDown)
                {
                    if (_vgaWaitFor == 200 && (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                                               !GetBitFlag(14)))
                    {
                        SkipSpeech();
                        break;
                    }
                }
                if (_exitCutscene)
                {
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                    {
                        if (_variableArray[105] == 0)
                        {
                            _variableArray[105] = 255;
                            break;
                        }
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                             _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                    {
                        if (_vgaWaitFor == 51)
                        {
                            SetBitFlag(244, true);
                            break;
                        }
                    }
                    else
                    {
                        if (GetBitFlag(9))
                        {
                            EndCutscene();
                            break;
                        }
                    }
                }
                ProcessSpecialKeys();

                if (_syncCount >= maxCount)
                {
                    Warning("waitForSync: wait timed out");
                    break;
                }

                Delay(1);
            }
        }
    }
}