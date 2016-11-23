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
    partial class AGOSEngine
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

        private void ShowActionString(string stringPtr)
        {
            throw new NotImplementedException();
        }

        private void HandleVerbClicked(uint verb)
        {
            throw new NotImplementedException();
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

        private void FreeBox(uint boxCode)
        {
            throw new NotImplementedException();
        }

        private void EnableBox(int hitarea)
        {
            HitArea ha = FindBox(hitarea);
            if (ha != null)
                ha.flags = (ushort) (ha.flags & ~(ushort) BoxFlags.kBFBoxDead);
        }

        private void DisableBox(int hitarea)
        {
            var ha = FindBox(hitarea);
            if (ha == null) return;

            ha.flags = (ushort) (ha.flags | (ushort) BoxFlags.kBFBoxDead);
            ha.flags = (ushort) (ha.flags & ~(ushort) BoxFlags.kBFBoxSelected);
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
            return (ha.flags & (ushort) BoxFlags.kBFBoxDead) == 0;
        }

        protected void DefineBox(int id, int x, int y, int width, int height, int flags, int verb, Item itemPtr)
        {
            UndefineBox(id);

            var ha = FindEmptyHitArea();
            ha.Value.x = (ushort) x;
            ha.Value.y = (ushort) y;
            ha.Value.width = (ushort) width;
            ha.Value.height = (ushort) height;
            ha.Value.flags = (ushort) (flags | (ushort) BoxFlags.kBFBoxInUse);
            ha.Value.id = ha.Value.priority = (ushort) id;
            ha.Value.verb = (ushort) verb;
            ha.Value.itemPtr = itemPtr;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF &&
                ((ha.Value.flags & (ushort) BoxFlags.kBFHyperBox) != 0))
            {
                ha.Value.data = _hyperLink;
                ha.Value.priority = 50;
            }

            _needHitAreaRecalc++;
        }

        private void ResetVerbs()
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

            if ((ha.flags & (ushort) BoxFlags.kBFBoxDead) != 0)
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

        private void SetVerb(HitArea ha)
        {
            HitArea tmp = _currentVerbBox;

            if (ha == tmp)
                return;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
            {
                if (tmp != null)
                {
                    tmp.flags |= (ushort) BoxFlags.kBFInvertTouch;
                    if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR))
                        InvertBox(tmp, 212, 208, 212, 8);
                    else
                        InvertBox(tmp, 213, 208, 213, 10);
                }

                if ((ha.flags & (ushort) BoxFlags.kBFBoxSelected) != 0)
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

                ha.flags = (ushort) (ha.flags & ~(ushort) (BoxFlags.kBFBoxSelected | BoxFlags.kBFInvertTouch));
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

        private void InventoryUp(WindowBlock haWindow)
        {
            throw new NotImplementedException();
        }

        private void InventoryDown(WindowBlock haWindow)
        {
            throw new NotImplementedException();
        }

        private void BoxController(short mouseX, short mouseY, int i)
        {
            throw new NotImplementedException();
        }

        private void DisplayName(HitArea ha)
        {
            throw new NotImplementedException();
        }

        private void InvertBox(HitArea ha, byte a, byte b, byte c, byte d)
        {
            byte color;
            int w, h, i;

            _videoLockOut |= 0x8000;

            LocksScreen(screen =>
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


    }
}