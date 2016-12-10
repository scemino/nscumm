//
//  AGOSEngine.Event.cs
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
using System.Linq;
using NScumm.Core;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        private bool _fastMode;
        private byte _opcode177Var1, _opcode177Var2;
        private byte _opcode178Var1, _opcode178Var2;

        protected void AddTimeEvent(ushort timeout, ushort subroutine_id)
        {
            TimeEvent te = new TimeEvent(), last = null;
            uint curTime = GetTime();

            if (_gd.ADGameDescription.gameId == GameIds.GID_DIMP)
            {
                timeout /= 2;
            }

            te.time = curTime + timeout - _gameStoppedClock;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF && _clockStopped != 0)
                te.time -= GetTime() - _clockStopped;
            te.subroutine_id = subroutine_id;

            var first = _firstTimeStruct;
            while (first != null)
            {
                if (te.time <= first.time)
                {
                    if (last != null)
                    {
                        last.next = te;
                        te.next = first;
                        return;
                    }
                    te.next = _firstTimeStruct;
                    _firstTimeStruct = te;
                    return;
                }

                last = first;
                first = first.next;
            }

            if (last != null)
            {
                last.next = te;
                te.next = null;
            }
            else
            {
                _firstTimeStruct = te;
                te.next = null;
            }
        }

        private void DelTimeEvent(TimeEvent te)
        {
            if (te == _pendingDeleteTimeEvent)
                _pendingDeleteTimeEvent = null;

            if (te == _firstTimeStruct)
            {
                _firstTimeStruct = te.next;
                return;
            }

            var cur = _firstTimeStruct;
            if (cur == null)
                Error("delTimeEvent: none available");

            for (;;)
            {
                if (cur.next == null)
                    Error("delTimeEvent: no such te");
                if (te == cur.next)
                {
                    cur.next = te.next;
                    return;
                }
                cur = cur.next;
            }
        }

        private void InvokeTimeEvent(TimeEvent te)
        {
            _scriptVerb = 0;

            if (_runScriptReturn1)
                return;

            var sub = GetSubroutineByID(te.subroutine_id);
            if (sub != null)
                StartSubroutineEx(sub);

            _runScriptReturn1 = false;
        }

        protected void KillAllTimers()
        {
            TimeEvent cur, next;

            for (cur = _firstTimeStruct; cur != null; cur = next)
            {
                next = cur.next;
                DelTimeEvent(cur);
            }
            _clickOnly = false;
        }

        private bool KickoffTimeEvents()
        {
            uint cur_time;
            TimeEvent te;
            bool result = false;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF && _clockStopped != 0)
                return result;

            cur_time = GetTime() - _gameStoppedClock;

            while ((te = _firstTimeStruct) != null && te.time <= cur_time && !HasToQuit)
            {
                result = true;
                _pendingDeleteTimeEvent = te;
                InvokeTimeEvent(te);
                if (_pendingDeleteTimeEvent != null)
                {
                    _pendingDeleteTimeEvent = null;
                    DelTimeEvent(te);
                }
            }

            return result;
        }

        private void HaltAnimation()
        {
            if ((_videoLockOut & 0x10) != 0)
                return;

            _videoLockOut |= 0x10;

            if (_displayFlag != 0)
            {
                DisplayScreen();
                _displayFlag = 0;
            }
        }

        private void RestartAnimation()
        {
            if ((_videoLockOut & 0x10) == 0)
                return;

            if (GameType != SIMONGameType.GType_PN)
            {
                _window4Flag = 2;
                SetMoveRect(0, 0, 224, 127);
                DisplayScreen();
            }

            _videoLockOut = (ushort) (_videoLockOut & ~0x10);
        }

        private void AddVgaEvent(ushort num, EventType type, BytePtr codePtr, ushort curSprite, ushort curZoneNum)
        {
            _videoLockOut |= 1;

            var vte = _vgaTimerList.FirstOrDefault(o => o.delay == 0);
            vte.delay = (short) num;
            vte.codePtr = codePtr;
            vte.id = curSprite;
            vte.zoneNum = curZoneNum;
            vte.type = type;

            _videoLockOut = (ushort) (_videoLockOut & ~1);
        }

        protected void DeleteVgaEvent(Ptr<VgaTimerEntry> vte)
        {
            _videoLockOut |= 1;

            if (vte.Offset + 1 <= _nextVgaTimerToProcess.Offset)
            {
                _nextVgaTimerToProcess.Offset--;
            }

            do
            {
                vte[0] = new VgaTimerEntry(vte[1]);
                vte.Offset++;
            } while (vte.Value.delay != 0);

            _videoLockOut = (ushort) (_videoLockOut & ~1);
        }

        private void ProcessVgaEvents()
        {
            var vte = new Ptr<VgaTimerEntry>(_vgaTimerList);

            _vgaTickCounter++;

            while (vte.Value.delay != 0)
            {
                vte.Value.delay -= _vgaBaseDelay;
                if (vte.Value.delay <= 0)
                {
                    ushort curZoneNum = vte.Value.zoneNum;
                    ushort curSprite = vte.Value.id;
                    var scriptPtr = vte.Value.codePtr;

                    switch (vte.Value.type)
                    {
                        case EventType.ANIMATE_INT:
                            vte.Value.delay = (short) (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2
                                ? 5
                                : _frameCount);
                            AnimateSprites();
                            vte.Offset++;
                            break;
                        case EventType.ANIMATE_EVENT:
                            _nextVgaTimerToProcess = new Ptr<VgaTimerEntry>(vte, 1);
                            DeleteVgaEvent(vte);
                            AnimateEvent(scriptPtr, curZoneNum, curSprite);
                            vte = _nextVgaTimerToProcess;
                            break;
                        case EventType.SCROLL_EVENT:
                            _nextVgaTimerToProcess = new Ptr<VgaTimerEntry>(vte, 1);
                            DeleteVgaEvent(vte);
                            ScrollEvent();
                            vte = _nextVgaTimerToProcess;
                            break;
                        case EventType.PLAYER_DAMAGE_EVENT:
                            PlayerDamageEvent(vte, curZoneNum);
                            vte = _nextVgaTimerToProcess;
                            break;
                        case EventType.MONSTER_DAMAGE_EVENT:
                            MonsterDamageEvent(vte, curZoneNum);
                            vte = _nextVgaTimerToProcess;
                            break;
                        default:
                            Error("processVgaEvents: Unknown event type {0}", vte.Value.type);
                            return;
                    }
                }
                else
                {
                    vte.Offset++;
                }
            }
        }

        private void AnimateEvent(BytePtr codePtr, ushort curZoneNum, ushort curSprite)
        {
            _vgaCurSpriteId = curSprite;

            _vgaCurZoneNum = curZoneNum;
            _zoneNumber = curZoneNum;
            var vpe = _vgaBufferPointers[curZoneNum];

            _curVgaFile1 = vpe.vgaFile1;
            _curVgaFile2 = vpe.vgaFile2;
            _curSfxFile = vpe.sfxFile;
            _curSfxFileSize = vpe.sfxFileEnd.Offset - vpe.sfxFile.Offset;

            _vcPtr = codePtr;

            RunVgaScript();
        }

        private void ScrollEvent()
        {
            if (_scrollCount == 0)
                return;

            if (GameType == SIMONGameType.GType_FF)
            {
                if (_scrollCount < 0)
                {
                    if (_scrollFlag != -8)
                    {
                        _scrollFlag = -8;
                        _scrollCount += 8;
                    }
                }
                else
                {
                    if (_scrollFlag != 8)
                    {
                        _scrollFlag = 8;
                        _scrollCount -= 8;
                    }
                }
            }
            else
            {
                if (_scrollCount < 0)
                {
                    if (_scrollFlag != -1)
                    {
                        _scrollFlag = -1;
                        if (++_scrollCount == 0)
                            return;
                    }
                }
                else
                {
                    if (_scrollFlag != 1)
                    {
                        _scrollFlag = 1;
                        if (--_scrollCount == 0)
                            return;
                    }
                }

                AddVgaEvent(6, EventType.SCROLL_EVENT, BytePtr.Null, 0, 0);
            }
        }

        private void PlayerDamageEvent(Ptr<VgaTimerEntry> vte, ushort dx)
        {
            // Draws damage indicator gauge when player hit
            _nextVgaTimerToProcess = new Ptr<VgaTimerEntry>(vte, 1);

            if (_opcode177Var1 == 0)
            {
                DrawStuff(_image1, 4 + _opcode177Var2 * 4);
                _opcode177Var2++;
                if (_opcode177Var2 == dx)
                {
                    _opcode177Var1 = 1;
                    vte.Value.delay = (short) (16 - dx);
                }
                else
                {
                    vte.Value.delay = 1;
                }
            }
            else if (_opcode177Var2 != 0)
            {
                _opcode177Var2--;
                DrawStuff(_image2, 4 + _opcode177Var2 * 4);
                vte.Value.delay = 3;
            }
            else
            {
                DeleteVgaEvent(vte);
            }
        }

        private void DrawStuff(BytePtr src, int xoffs)
        {
            LocksScreen(screen =>
            {
                var y = GamePlatform == Platform.AtariST ? 132 : 135;
                var dst = screen.GetBasePtr(xoffs, y);

                for (var h = 0; h < 6; h++)
                {
                    src.Copy(dst, 4);
                    src += 4;
                    dst += screen.Pitch;
                }
            });
        }

        private void MonsterDamageEvent(Ptr<VgaTimerEntry> vte, ushort dx)
        {
            // Draws damage indicator gauge when monster hit
            _nextVgaTimerToProcess = new Ptr<VgaTimerEntry>(vte, 1);

            if (_opcode178Var1 == 0)
            {
                DrawStuff(_image3, 275 + _opcode178Var2 * 4);
                _opcode178Var2++;
                if (_opcode178Var2 >= 10 || _opcode178Var2 == dx)
                {
                    _opcode178Var1 = 1;
                    vte.Value.delay = (short) (16 - dx);
                }
                else
                {
                    vte.Value.delay = 1;
                }
            }
            else if (_opcode178Var2 != 0)
            {
                _opcode178Var2--;
                DrawStuff(_image4, 275 + _opcode178Var2 * 4);
                vte.Value.delay = 3;
            }
            else
            {
                DeleteVgaEvent(vte);
            }
        }

        protected void Delay(int amount)
        {
            //Event @event;
            var start = DateTime.Now;
            var cur = start;

            // TODO: vs: _system.getAudioCDManager().update();

            // TODO: vs: _debugger.onFrame();

            var vgaPeriod = _fastMode ? 10 : _vgaPeriod;
            //var vgaPeriod = _vgaPeriod;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP &&
                _gd.ADGameDescription.gameId != GameIds.GID_DIMP)
            {
                if (vgaPeriod == 15 && _variableArray[999] == 0)
                    vgaPeriod = 30;
            }

            _rnd.GetRandomNumber(2);

            do
            {
                while (!_inCallBack && cur >= _lastVgaTick + TimeSpan.FromMilliseconds(vgaPeriod) && !IsPaused)
                {
                    _lastVgaTick += TimeSpan.FromMilliseconds(vgaPeriod);

                    // don't get too many frames behind
                    if (cur >= _lastVgaTick + TimeSpan.FromMilliseconds(vgaPeriod * 2))
                        _lastVgaTick = cur;

                    _inCallBack = true;
                    TimerProc();
                    _inCallBack = false;
                }

                var inputState = OSystem.InputManager.GetState();
                if (inputState.IsKeyDown(KeyCode.D0) && inputState.IsKeyDown(KeyCode.D9)
                    && (inputState.IsKeyDown(KeyCode.LeftAlt) ||
                        inputState.IsKeyDown(KeyCode.LeftControl)))
                {
                    var key = inputState.GetKeys().FirstOrDefault(k => k >= KeyCode.D0 && k <= KeyCode.D9);
                    _saveLoadSlot = (byte) (key - KeyCode.D0);

                    // There is no save slot 0
                    if (_saveLoadSlot == 0)
                        _saveLoadSlot = 10;

                    _saveLoadName=$"Quick {_saveLoadSlot}";
                    _saveLoadType = (byte) (inputState.IsKeyDown(KeyCode.LeftAlt) ? 1 : 2);
                    QuickLoadOrSave();
                }
                if (inputState.IsKeyDown(KeyCode.LeftControl))
                {
                    if (inputState.IsKeyDown(KeyCode.A))
                    {
                        // TODO; GUI::Dialog* _aboutDialog;
                        //_aboutDialog = new GUI::AboutDialog();
                        //_aboutDialog.runModal();
                    }
                    else if (inputState.IsKeyDown(KeyCode.F))
                    {
                        _fastMode = !_fastMode;
                    }
                    else if (inputState.IsKeyDown(KeyCode.D))
                    {
                        // TODO: _debugger.attach();
                    }
                }
                /*while (_eventMan.pollEvent(@event))
                {
                    switch (@event.type)
                    {
                        case Common::EVENT_KEYDOWN:
                            if (@event.kbd.hasFlags(Common::KBD_ALT))
                            {
                                if (@event.kbd.keycode == Common::KEYCODE_u)
                                {
                                    DumpAllSubroutines();
                                }
                                else if (@event.kbd.keycode == Common::KEYCODE_i)
                                {
                                    DumpAllVgaImageFiles();
                                }
                                else if (@event.kbd.keycode == Common::KEYCODE_v)
                                {
                                    DumpAllVgaScriptFiles();
                                }
                            }

                            if (_gd.ADGameDescription.gameType == GType_PP)
                            {
                                if (@event.kbd.hasFlags(Common::KBD_SHIFT))
                                    _variableArray[41] = 0;
                                else
                                    _variableArray[41] = 1;
                            }

                            _keyPressed = @event.kbd;
                            break;
                        case Common::EVENT_RTL:
                        case Common::EVENT_QUIT:
                            return;
                        case Common::EVENT_WHEELUP:
                            handleMouseWheelUp();
                            break;
                        case Common::EVENT_WHEELDOWN:
                            handleMouseWheelDown();
                            break;
                    }
                }*/

                _keyPressed = inputState;

                if (inputState.IsLeftButtonDown)
                {
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                        SetBitFlag(89, true);
                    _leftButtonDown = true;
                    _leftButton = 1;
                }
                else
                {
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                        SetBitFlag(89, false);

                    _leftButton = 0;
                    _leftButtonCount = 0;
                    _leftClick = true;
                }

                if (inputState.IsRightButtonDown)
                {
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                        SetBitFlag(92, false);
                    _rightButtonDown = true;
                }
                else
                {
                    _rightClick = true;
                }

                if (_leftButton == 1)
                    _leftButtonCount++;

                // TODO: vs: _system.getAudioCDManager().update();

                OSystem.GraphicsManager.UpdateScreen();

                if (amount == 0)
                    break;

                var thisDelay = _fastMode ? 1 : 20;
                if (thisDelay > amount)
                    thisDelay = amount;
                ServiceLocator.Platform.Sleep(thisDelay);

                cur = DateTime.Now;
            } while (cur < start + TimeSpan.FromMilliseconds(amount) && !HasToQuit);
        }

        // TODO: share this
        public static char ToChar(KeyCode key)
        {
            if (key >= KeyCode.A && key <= KeyCode.Z)
            {
                return (char) ('a' + (key - KeyCode.A));
            }
            if (key >= KeyCode.D0 && key <= KeyCode.D9)
            {
                return (char) ('0' + (key - KeyCode.D0));
            }
            if (key >= KeyCode.NumPad0 && key <= KeyCode.NumPad9)
            {
                return (char) ('0' + (key - KeyCode.NumPad0));
            }
            switch (key)
            {
                case KeyCode.Tab:
                    return '\t';
                case KeyCode.Return:
                    return '\n';
                case KeyCode.Space:
                    return ' ';
                case KeyCode.Comma:
                    return ',';
                case KeyCode.OemPeriod:
                    return '.';
                default:
                    return '\0';
            }
        }

        private void TimerProc()
        {
            if ((_videoLockOut & 0x80E9) != 0 || (_videoLockOut & 2) != 0)
                return;

            _syncCount++;

            _videoLockOut |= 2;

            HandleMouseMoved();

            if ((_videoLockOut & 0x10) == 0)
            {
                ProcessVgaEvents();
                ProcessVgaEvents();
                _cepeFlag = !_cepeFlag;
                if (!_cepeFlag)
                    ProcessVgaEvents();
            }

            if (_displayFlag != 0)
            {
                DisplayScreen();
                _displayFlag = 0;
            }

            _videoLockOut = (ushort) (_videoLockOut & ~2);
        }

        private static readonly byte[] _image1 =
        {
            0x3A, 0x37, 0x3B, 0x37,
            0x3A, 0x3E, 0x3F, 0x3E,
            0x37, 0x3F, 0x31, 0x3F,
            0x37, 0x3F, 0x31, 0x3F,
            0x3A, 0x3E, 0x3F, 0x3E,
            0x3A, 0x37, 0x3B, 0x37,
        };

        private static readonly byte[] _image2 =
        {
            0x3A, 0x3A, 0x3B, 0x3A,
            0x3A, 0x37, 0x3E, 0x37,
            0x3A, 0x37, 0x3E, 0x37,
            0x3A, 0x37, 0x3E, 0x37,
            0x3A, 0x37, 0x3E, 0x37,
            0x3A, 0x3A, 0x3B, 0x3A,
        };

        private static readonly byte[] _image3 =
        {
            0x3A, 0x32, 0x3B, 0x32,
            0x3A, 0x39, 0x3F, 0x39,
            0x32, 0x3F, 0x31, 0x3F,
            0x32, 0x3F, 0x31, 0x3F,
            0x3A, 0x39, 0x3F, 0x39,
            0x3A, 0x32, 0x3B, 0x32,
        };

        private static readonly byte[] _image4 =
        {
            0x3A, 0x3A, 0x3B, 0x3A,
            0x3A, 0x32, 0x39, 0x32,
            0x3A, 0x32, 0x38, 0x32,
            0x3A, 0x32, 0x38, 0x32,
            0x3A, 0x32, 0x39, 0x32,
            0x3A, 0x3A, 0x3B, 0x3A,
        };

        private byte _saveLoadSlot;
        private string _saveLoadName;
        private byte _saveLoadType;
    }
}