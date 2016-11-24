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
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AGOSEngine
    {
        private bool _fastMode = true;

        private void AddTimeEvent(ushort timeout, ushort subroutine_id)
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
            throw new NotImplementedException();
        }

        private void AddVgaEvent(ushort num, EventType type, BytePtr codePtr, ushort curSprite, ushort curZoneNum)
        {
            Debug($"AddVgaEvent({num})");
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
                            PlayerDamageEvent(vte.Value, curZoneNum);
                            vte = _nextVgaTimerToProcess;
                            break;
                        case EventType.MONSTER_DAMAGE_EVENT:
                            MonsterDamageEvent(vte.Value, curZoneNum);
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
            throw new NotImplementedException();
        }

        private void PlayerDamageEvent(VgaTimerEntry vte, ushort curZoneNum)
        {
            throw new NotImplementedException();
        }

        private void MonsterDamageEvent(VgaTimerEntry vte, ushort curZoneNum)
        {
            throw new NotImplementedException();
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

                // TODO: vs
                var inputState = OSystem.InputManager.GetState();
                /*while (_eventMan.pollEvent(@event))
                {
                    switch (@event.type)
                    {
                        case Common::EVENT_KEYDOWN:
                            if (@event.kbd.keycode >= Common::KEYCODE_0 && @event.kbd.keycode <= Common::KEYCODE_9
                                && (@event.kbd.hasFlags(Common::KBD_ALT) ||
                                    @event.kbd.hasFlags(Common::KBD_CTRL)))
                            {
                                _saveLoadSlot = @event.kbd.keycode - Common::KEYCODE_0;

                                // There is no save slot 0
                                if (_saveLoadSlot == 0)
                                    _saveLoadSlot = 10;

                                memset(_saveLoadName, 0, sizeof(_saveLoadName));
                                sprintf(_saveLoadName, "Quick %d", _saveLoadSlot);
                                _saveLoadType = (@event.kbd.hasFlags(Common::KBD_ALT)) ? 1 : 2;
                                quickLoadOrSave();
                            }
                            else if (@event.kbd.hasFlags(Common::KBD_ALT))
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
                            else if (@event.kbd.hasFlags(Common::KBD_CTRL))
                            {
                                if (@event.kbd.keycode == Common::KEYCODE_a)
                                {
                                    GUI::Dialog* _aboutDialog;
                                    _aboutDialog = new GUI::AboutDialog();
                                    _aboutDialog.runModal();
                                }
                                else if (@event.kbd.keycode == Common::KEYCODE_f)
                                {
                                    _fastMode = !_fastMode;
                                }
                                else if (@event.kbd.keycode == Common::KEYCODE_d)
                                {
                                    _debugger.attach();
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
                        case Common::EVENT_MOUSEMOVE:
                            break;
                        case Common::EVENT_LBUTTONDOWN:
                            if (_gd.ADGameDescription.gameType == GType_FF)
                                setBitFlag(89, true);
                            _leftButtonDown = true;
                            _leftButton = 1;
                            break;
                        case Common::EVENT_LBUTTONUP:
                            if (_gd.ADGameDescription.gameType == GType_FF)
                                setBitFlag(89, false);

                            _leftButton = 0;
                            _leftButtonCount = 0;
                            _leftClick = true;
                            break;
                        case Common::EVENT_RBUTTONDOWN:
                            if (_gd.ADGameDescription.gameType == GType_FF)
                                setBitFlag(92, false);
                            _rightButtonDown = true;
                            break;
                        case Common::EVENT_RBUTTONUP:
                            _rightClick = true;
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


    }
}