//
//  AGOSEngine.Verb.cs
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
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        protected virtual void ClearName()
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                return;

            if (_nameLocked || _lastNameOn == null)
                return;

            ResetNameWindow();
        }

        protected void ShowActionString(string @string)
        {
            int len = GameType == SIMONGameType.GType_WW ? 29 : 53;

            var window = _windowArray[1];
            if (window == null || window.textColor == 0)
                return;

            // Arisme : hack for long strings in the French version
            uint x;
            if (@string.Length - 1 <= len)
                x = (uint) ((len - (@string.Length - 1)) * 3);
            else
                x = 0;

            window.textColumn = (short) (x / 8);
            window.textColumnOffset = (ushort) (x & 7);
            if (_language == Language.HE_ISR && window.textColumnOffset != 0)
            {
                window.textColumnOffset = (ushort) (8 - window.textColumnOffset);
                window.textColumn++;
            }

            foreach (var c in @string)
                WindowPutChar(window, (byte) c);
        }

        private void HandleVerbClicked(uint verb)
        {
            int result;

            if (HasToQuit)
                return;

            _objectItem = _hitAreaObjectItem;
            if (_objectItem == _dummyItem2)
            {
                _objectItem = Me();
            }
            if (_objectItem == _dummyItem3)
            {
                _objectItem = DerefItem(Me().parent);
            }

            _subjectItem = _hitAreaSubjectItem;
            if (_subjectItem == _dummyItem2)
            {
                _subjectItem = Me();
            }
            if (_subjectItem == _dummyItem3)
            {
                _subjectItem = DerefItem(Me().parent);
            }

            if (_subjectItem != null)
            {
                _scriptNoun1 = _subjectItem.noun;
                _scriptAdj1 = _subjectItem.adjective;
            }
            else
            {
                _scriptNoun1 = -1;
                _scriptAdj1 = -1;
            }

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

            _scriptVerb = (short) _verbHitArea;

            var sub = GetSubroutineByID(0);
            if (sub == null)
                return;

            result = StartSubroutine(sub);
            if (result == -1)
                ShowMessageFormat("I don't understand");

            _runScriptReturn1 = false;

            sub = GetSubroutineByID(100);
            if (sub != null)
                StartSubroutine(sub);

            if (GameType == SIMONGameType.GType_SIMON2 || GameType == SIMONGameType.GType_FF ||
                GameType == SIMONGameType.GType_PP)
                _runScriptReturn1 = false;

            PermitInput();
        }

        protected void DisplayName(HitArea ha)
        {
            if (GameType == SIMONGameType.GType_ELVIRA1 || GameType == SIMONGameType.GType_ELVIRA2 ||
                GameType == SIMONGameType.GType_PP)
                return;

            bool result;
            int x = 0, y = 0;

            if (GameType == SIMONGameType.GType_FF)
            {
                if (ha.flags.HasFlag(BoxFlags.kBFHyperBox))
                {
                    _lastNameOn = ha;
                    return;
                }
                if (FindBox(50) != null)
                    return;

                if (GetBitFlag(99))
                    _animatePointer = !ha.flags.HasFlag(BoxFlags.kBFTextBox);
                else
                    _animatePointer = true;

                if (!GetBitFlag(73))
                    return;

                y = ha.y;
                if (GetBitFlag(99) && y > 288)
                    y = 288;
                y -= 17;
                if (y < 0)
                    y = 0;
                y += 2;
                x = ha.width / 2 + ha.x;
            }
            else
            {
                ResetNameWindow();
            }

            if (ha.flags.HasFlag(BoxFlags.kBFTextBox))
            {
                result = PrintTextOf((uint) ha.flags / 256, (uint) x, (uint) y);
            }
            else
            {
                result = PrintNameOf(ha.itemPtr, (uint) x, (uint) y);
            }

            if (result)
                _lastNameOn = ha;
        }

        protected void ResetNameWindow()
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 &&
                GetBitFlag(79))
                return;

            var window = _windowArray[1];
            if (window != null && window.textColor != 0)
                ClearWindow(window);

            _lastNameOn = null;
            _lastVerbOn = null;
        }

        private HitArea FindBox(int hitareaId)
        {
            var ha = 0;
            int count = _hitAreas.Length;

            do
            {
                var h = _hitAreas[ha];
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                {
                    if (h.id == hitareaId && h.flags != 0)
                        return h;
                }
                else
                {
                    if (h.id == hitareaId)
                        return h;
                }
                ha++;
            } while (--count != 0);
            return null;
        }

        protected Ptr<HitArea> FindEmptyHitArea()
        {
            var ha = 0;
            int count = _hitAreas.Length - 1;

            do
            {
                if (_hitAreas[ha].flags == 0)
                    return new Ptr<HitArea>(_hitAreas, ha);
                ha++;
            } while (--count != 0);

            // The last box is overwritten, if too many boxes are allocated.
            return new Ptr<HitArea>(_hitAreas, ha);
        }

        private void FreeBox(int index)
        {
            CHECK_BOUNDS(index, _hitAreas);
            _hitAreas[index].flags = 0;
        }

        protected void EnableBox(int hitarea)
        {
            HitArea ha = FindBox(hitarea);
            if (ha != null)
                ha.flags = (ha.flags & ~BoxFlags.kBFBoxDead);
        }

        protected void DisableBox(int hitarea)
        {
            var ha = FindBox(hitarea);
            if (ha == null) return;

            ha.flags = ha.flags | BoxFlags.kBFBoxDead;
            ha.flags = ha.flags & ~BoxFlags.kBFBoxSelected;
            if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                 _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) &&
                hitarea == 102)
            {
                ResetVerbs();
            }
        }

        private void MoveBox(int hitarea, int x, int y)
        {
            var ha = FindBox(hitarea);
            if (ha != null)
            {
                if (GameType == SIMONGameType.GType_FF || GameType == SIMONGameType.GType_PP)
                {
                    ha.x = (ushort) (ha.x + x);
                    ha.y = (ushort) (ha.y + y);
                }
                else
                {
                    ha.x = (ushort) x;
                    ha.y = (ushort) y;
                }
            }
        }

        private void UndefineBox(int hitarea)
        {
            HitArea ha = FindBox(hitarea);
            if (ha != null)
            {
                ha.flags = 0;
                if (ha == _lastNameOn)
                    ClearName();
                _needHitAreaRecalc++;
            }
        }

        private bool IsBoxDead(int hitarea)
        {
            HitArea ha = FindBox(hitarea);
            if (ha == null)
                return false;
            return (ha.flags & BoxFlags.kBFBoxDead) == 0;
        }

        protected void DefineBox(int id, int x, int y, int width, int height, int flags, int verb, Item itemPtr)
        {
            UndefineBox(id);

            var ha = FindEmptyHitArea();
            ha.Value.x = (ushort) x;
            ha.Value.y = (ushort) y;
            ha.Value.width = (ushort) width;
            ha.Value.height = (ushort) height;
            ha.Value.flags = (BoxFlags) flags | BoxFlags.kBFBoxInUse;
            ha.Value.id = ha.Value.priority = (ushort) id;
            ha.Value.verb = (ushort) verb;
            ha.Value.itemPtr = itemPtr;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF &&
                ((ha.Value.flags & BoxFlags.kBFHyperBox) != 0))
            {
                ha.Value.data = _hyperLink;
                ha.Value.priority = 50;
            }

            _needHitAreaRecalc++;
        }

        protected void ResetVerbs()
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                return;

            int id;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                id = 2;
                if (!GetBitFlag(79))
                    id = _mouse.Y >= 136 ? 102 : 101;
            }
            else
            {
                id = (int) (_mouse.Y >= 136 ? 102U : 101U);
            }

            _defaultVerb = (ushort) id;

            var ha = FindBox(id);
            if (ha == null)
                return;

            if ((ha.flags & BoxFlags.kBFBoxDead) != 0)
            {
                _defaultVerb = 999;
                _currentVerbBox = null;
            }
            else
            {
                _verbHitArea = ha.verb;
                SetVerb(ha);
            }
        }

        protected void SetVerb(HitArea ha)
        {
            HitArea tmp = _currentVerbBox;

            if (ha == tmp)
                return;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
            {
                if (tmp != null)
                {
                    tmp.flags |= BoxFlags.kBFInvertTouch;
                    if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR))
                        InvertBox(tmp, 212, 208, 212, 8);
                    else
                        InvertBox(tmp, 213, 208, 213, 10);
                }

                if ((ha.flags & BoxFlags.kBFBoxSelected) != 0)
                {
                    if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR))
                        InvertBox(ha, 216, 212, 212, 4);
                    else
                        InvertBox(ha, 218, 213, 213, 5);
                }
                else
                {
                    if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR))
                        InvertBox(ha, 220, 216, 216, 8);
                    else
                        InvertBox(ha, 223, 218, 218, 10);
                }

                ha.flags = ha.flags & ~(BoxFlags.kBFBoxSelected | BoxFlags.kBFInvertTouch);
            }
            else
            {
                if (ha.id < 101)
                    return;
                _mouseCursor = (byte) (ha.id - 101);
                _needHitAreaRecalc++;
            }
            _currentVerbBox = ha;
        }

        private void InventoryUp(WindowBlock window)
        {
            if (window.iconPtr.line == 0)
                return;

            MouseOff();
            int index = GetWindowNum(window);
            DrawIconArray(index, window.iconPtr.itemRef, window.iconPtr.line - 1, window.iconPtr.classMask);
            MouseOn();
        }

        private void InventoryDown(WindowBlock window)
        {
            MouseOff();
            int index = GetWindowNum(window);
            DrawIconArray(index, window.iconPtr.itemRef, window.iconPtr.line + 1, window.iconPtr.classMask);
            MouseOn();
        }

        protected virtual void BoxController(uint x, uint y, uint mode)
        {
            Ptr<HitArea> ha = _hitAreas;
            int count = _hitAreas.Length;
            ushort priority = 0;

            HitArea best_ha = null;

            do
            {
                if ((ha.Value.flags & BoxFlags.kBFBoxInUse) != 0)
                {
                    if ((ha.Value.flags & BoxFlags.kBFBoxDead) == 0)
                    {
                        if (x >= ha.Value.x && y >= ha.Value.y &&
                            x - ha.Value.x < ha.Value.width && y - ha.Value.y < ha.Value.height &&
                            priority <= ha.Value.priority)
                        {
                            priority = ha.Value.priority;
                            best_ha = ha.Value;
                        }
                        else
                        {
                            if ((ha.Value.flags & BoxFlags.kBFBoxSelected) != 0)
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
            _currentBox = best_ha;

            if (best_ha == null)
                return;

            _currentBoxNum = best_ha.id;

            if (mode != 0)
            {
                if (mode == 3)
                {
                    if ((best_ha.verb & 0x4000) != 0)
                    {
                        if (GameType == SIMONGameType.GType_ELVIRA1 && _variableArray[500] == 0)
                        {
                            _variableArray[500] = (short) (best_ha.verb & 0xBFFF);
                        }

                        if (_clickOnly && best_ha.id < 8)
                        {
                            uint id = best_ha.id;
                            if (id >= 4)
                                id -= 4;

                            InvertBox(FindBox((int) id), 0, 0, 0, 0);
                            _clickOnly = false;
                            return;
                        }
                    }

                    if ((best_ha.flags & BoxFlags.kBFDragBox) != 0)
                        _lastClickRem = best_ha;
                }
                else
                {
                    _lastHitArea = best_ha;
                }
            }

            if (_clickOnly)
                return;

            if ((best_ha.flags & BoxFlags.kBFInvertTouch) != 0)
            {
                if ((best_ha.flags & BoxFlags.kBFBoxSelected) == 0)
                {
                    HitareaLeave(best_ha, false);
                    best_ha.flags |= BoxFlags.kBFBoxSelected;
                }
            }
            else
            {
                if (mode == 0)
                    return;

                if ((best_ha.flags & BoxFlags.kBFInvertSelect) == 0)
                    return;

                if ((best_ha.flags & BoxFlags.kBFToggleBox) != 0)
                {
                    HitareaLeave(best_ha, false);
                    best_ha.flags ^= BoxFlags.kBFInvertSelect;
                }
                else if ((best_ha.flags & BoxFlags.kBFBoxSelected) == 0)
                {
                    HitareaLeave(best_ha, false);
                    best_ha.flags |= BoxFlags.kBFBoxSelected;
                }
            }
        }

        private void InvertBox(HitArea ha, byte a, byte b, byte c, byte d)
        {
            byte color;
            int w, h, i;

            _videoLockOut |= 0x8000;

            LockScreen(screen =>
            {
                var src = screen.GetBasePtr(ha.x, ha.y);

                // WORKAROUND: Hitareas for saved game names aren't adjusted for scrolling locations
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 && ha.id >= 208 && ha.id <= 213)
                {
                    src -= _scrollX * 8;
                }

                _litBoxFlag = true;

                w = ha.width;
                h = ha.height;

                do
                {
                    for (i = 0; i != w; ++i)
                    {
                        color = src[i];
                        if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                        {
                            if ((color & 0xF) == 0 || (color & 0xF) == 10)
                            {
                                color ^= 10;
                                src[i] = color;
                            }
                        }
                        else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                        {
                            if ((color & 1) == 0)
                            {
                                color ^= 2;
                                src[i] = color;
                            }
                        }
                        else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                        {
                            if ((color & 1) != 0)
                            {
                                color ^= 2;
                                src[i] = color;
                            }
                        }
                        else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                        {
                            if (_gd.Platform == Platform.DOS)
                            {
                                if (color != 15)
                                {
                                    color ^= 7;
                                    src[i] = color;
                                }
                            }
                            else
                            {
                                if (color != 14)
                                {
                                    color ^= 15;
                                    src[i] = color;
                                }
                            }
                        }
                        else
                        {
                            if (a >= color && b < color)
                            {
                                if (c >= color)
                                    color += d;
                                else
                                    color -= d;
                                src[i] = color;
                            }
                        }
                    }
                    src += screen.Pitch;
                } while (--h != 0);
            });

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        protected void LeaveHitAreaById(int hitarea_id)
        {
            var ha = FindBox(hitarea_id);
            if (ha != null)
                HitareaLeave(ha);
        }

        protected void HitareaLeave(HitArea ha, bool state = false)
        {
            if (GameType == SIMONGameType.GType_SIMON2)
            {
                InvertBox(ha, 231, 229, 230, 1);
            }
            else
            {
                if (Features.HasFlag(GameFeatures.GF_32COLOR))
                    InvertBox(ha, 220, 212, 216, 4);
                else
                    InvertBox(ha, 223, 213, 218, 5);
            }
        }
    }
}