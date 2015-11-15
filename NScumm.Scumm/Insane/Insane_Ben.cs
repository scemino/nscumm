//
//  Insane_Ben.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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

using System.Diagnostics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Insane
{
    partial class Insane
    {
        void TurnBen(bool controllable)
        {
            int buttons;

            switch (_currSceneId)
            {
                case 21:
                case 25:
                case 3:
                case 13:
                    if (_actor[0].damage < _actor[0].maxdamage)
                    {
                        _actor[0].lost = false;
                    }
                    else
                    {
                        if (!_actor[0].lost && !_actor[1].lost)
                        {
                            _actor[0].lost = true;
                            _actor[0].act[2].state = 36;
                            _actor[0].act[1].state = 36;
                            _actor[0].act[1].room = 0;
                            _actor[0].act[0].state = 36;
                            _actor[0].act[0].room = 0;

                            if (smlayer_isSoundRunning(95))
                                smlayer_stopSound(95);
                        }
                    }
                    buttons = 0;
                    if (!_actor[0].lost && controllable)
                    {
                        buttons = ActionBen();
                        if (_currSceneId == 13)
                            buttons &= 2;
                        if (_currEnemy == EN_TORQUE)
                            buttons = 0;
                    }
                    Debug.WriteLine("00:{0} 01:{1} 02:{2} 03:{3}", _actor[0].act[0].state,
                        _actor[0].act[1].state, _actor[0].act[2].state, _actor[0].act[3].state);
                    Actor01Reaction(buttons);
                    Actor02Reaction(buttons);
                    Actor03Reaction(buttons);
                    Actor00Reaction(buttons);
                    break;
                case 17:
                    MineChooseRoad(ProcessBenOnRoad(false));
                    break;
                default:
                    if (_actor[0].damage < _actor[0].maxdamage)
                    {
                        _actor[0].lost = false;
                    }
                    else
                    {
                        if (!_actor[0].lost && !_actor[1].lost)
                        {
                            QueueSceneSwitch(10, null, "wr2_ben.san", 64, 0, 0, 0);
                            _actor[0].lost = true;
                            _actor[0].act[2].state = 36;
                            _actor[0].act[2].room = 0;
                            _actor[0].act[0].state = 36;
                            _actor[0].act[0].room = 0;
                            _actor[0].act[1].state = 36;
                            _actor[0].act[1].room = 0;
                            MineChooseRoad(0);
                            return;
                        }
                    }

                    if (!_actor[0].lost && controllable)
                        MineChooseRoad(ProcessBenOnRoad(true));
                    else
                        MineChooseRoad(0);
                    break;
            }
        }

        int EnemyBenHandler(int actor1, int actor2, int probability)
        {
            int retval;
            int tmp;

            retval = ProcessMouse();

            // Joystick support is skipped

            retval |= ProcessKeyboard();

            tmp = _enemyState[EN_BEN, 0] - 160;
            if (tmp < -160)
                tmp = -160;

            if (tmp > 160)
                tmp = 160;

            _actor[actor1].cursorX = tmp;

            smush_warpMouse(_enemyState[EN_BEN, 0], _enemyState[EN_BEN, 1], -1);

            return (retval & 3);
        }

        int CalcBenDamage(bool arg_0, bool arg_4)
        {
            if ((_actor[1].x - _actor[0].x > WeaponMaxRange(1)) ||
                (_actor[1].x - _actor[0].x < WeaponMinRange(1)))
                return 0;

            if (_actor[0].field_44 && arg_4)
                return 1000;

            if (!Actor1StateFlags(_actor[0].act[2].state))
                return 0;

            if (arg_0)
            {
                OuchSoundBen();
                _actor[0].damage += WeaponDamage(1); // PATCH
            }

            return 1;
        }

        int ActionBen()
        {
            int buttons, tmp;
            bool doDamage = false;
            int sound;

            if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                sound = 59;
            else
                sound = 95;

            if (_actor[0].enemyHandler != -1)
                buttons = EnemyHandler(_actor[0].enemyHandler, 0, 1, _actor[0].probability);
            else
                buttons = EnemyHandler(EN_TORQUE, 0, 1, _actor[0].probability);

            if (_actor[0].tilt != 0)
            {
                _actor[0].speed += _actor[0].cursorX / 40;
            }
            else
            {
                if (_actor[0].speed < 0)
                    _actor[0].speed++;
                else
                    _actor[0].speed--;
            }

            if (_actor[0].speed > 8)
                _actor[0].speed = 8;

            if (_actor[0].speed < -8)
                _actor[0].speed = -8;

            _actor[0].x += _actor[0].speed;

            if (_actor[0].x > 100)
                _actor[0].x--;
            else if (_actor[0].x < 100)
                _actor[0].x++;

            if (_actor[0].x >= 0)
            {
                if (_actor[1].x - 90 <= _actor[0].x && !_actor[0].lost && !_actor[1].lost)
                {
                    _val213d++;
                    _actor[0].x = _actor[1].x - 90;

                    tmp = _actor[1].speed;
                    _actor[1].speed = _actor[0].speed;
                    _actor[0].speed = tmp;

                    if (_val213d > 50)
                    {
                        _actor[0].cursorX = -320;
                        _val213d = 0;
                    }

                    if (!smlayer_isSoundRunning(sound))
                        smlayer_startSfx(sound);
                }
                else
                {
                    if (smlayer_isSoundRunning(sound))
                        smlayer_stopSound(sound);

                    _val213d = 0;
                }
            }
            else
            {
                _actor[0].x = 0;
                _actor[0].damage++; // FIXME: apparently it is a bug in original
                // and damage is doubled
                doDamage = true;
            }

            if (_actor[0].x > 320)
            {
                _actor[0].x = 320;
                doDamage = true;
            }

            if (_actor[0].x < 10 || _actor[0].x > 310 || doDamage)
            {
                _tiresRustle = true;
                _actor[0].x1 = -_actor[0].x1;
                _actor[0].damage++; // PATCH
            }

            return buttons;
        }

        // Bike
        void Actor00Reaction(int buttons)
        {
            int tmpx, tmpy;

            switch (_actor[0].tilt)
            {
                case -3:
                    if (_actor[0].act[0].state != 41)
                    {
                        smlayer_setActorFacing(0, 0, 6, 180);
                        _actor[0].act[0].state = 41;
                    }
                    break;
                case -2:
                    if (_actor[0].act[0].state != 40)
                    {
                        smlayer_setActorFacing(0, 0, 7, 180);
                        _actor[0].act[0].state = 40;
                    }
                    break;
                case -1:
                    if (_actor[0].act[0].state != 39)
                    {
                        smlayer_setActorFacing(0, 0, 8, 180);
                        _actor[0].act[0].state = 39;
                    }
                    break;
                case 0:
                    if (_actor[0].act[0].state != 1)
                    {
                        smlayer_setActorFacing(0, 0, 9, 180);
                        _actor[0].act[0].state = 1;
                    }
                    break;
                case 1:
                    if (_actor[0].act[0].state != 55)
                    {
                        smlayer_setActorFacing(0, 0, 10, 180);
                        _actor[0].act[0].state = 55;
                    }
                    break;
                case 2:
                    if (_actor[0].act[0].state != 56)
                    {
                        smlayer_setActorFacing(0, 0, 11, 180);
                        _actor[0].act[0].state = 56;
                    }
                    break;
                case 3:
                    if (_actor[0].act[0].state != 57)
                    {
                        smlayer_setActorFacing(0, 0, 12, 180);
                        _actor[0].act[0].state = 57;
                    }
                    break;
                default:
                    break;
            }
            tmpx = _actor[0].x + _actor[0].x1;
            tmpy = _actor[0].y + _actor[0].y1;

            if (_actor[0].act[0].room != 0)
                smlayer_putActor(0, 0, tmpx, tmpy, (byte)_smlayer_room2);
            else
                smlayer_putActor(0, 0, tmpx, tmpy, (byte)_smlayer_room);
        }

        // Bike top
        void Actor01Reaction(int buttons)
        {
            int tmpx, tmpy;

            ChooseBenWeaponAnim(buttons);

            switch (_actor[0].tilt)
            {
                case -3:
                    if (_actor[0].act[1].state != 41 || _actor[0].weaponClass != _actor[0].animWeaponClass)
                    {
                        SetBenAnimation(0, 6);
                        _actor[0].act[1].state = 41;
                    }

                    if (_actor[0].cursorX >= -100)
                    {
                        SetBenAnimation(0, 7);
                        _actor[0].act[1].state = 40;
                        _actor[0].field_8 = 48;
                        _actor[0].tilt = -2;
                    }
                    break;
                case -2:
                    if (_actor[0].act[1].state != 40 || _actor[0].weaponClass != _actor[0].animWeaponClass)
                    {
                        SetBenAnimation(0, 7);
                        _actor[0].act[1].state = 40;
                    }
                    if (_actor[0].field_8 == 48)
                        _actor[0].tilt = -1;
                    else
                        _actor[0].tilt = -3;
                    break;
                case -1:
                    if (_actor[0].act[1].state != 39 || _actor[0].weaponClass != _actor[0].animWeaponClass)
                    {
                        SetBenAnimation(0, 8);
                        _actor[0].act[1].state = 39;
                    }

                    if (_actor[0].field_8 == 48)
                        _actor[0].tilt = 0;
                    else
                        _actor[0].tilt = -2;
                    break;
                case 0:
                    if (_actor[0].act[1].state != 1 || _actor[0].weaponClass != _actor[0].animWeaponClass)
                    {
                        SetBenAnimation(0, 9);
                        _actor[0].act[1].state = 1;
                    }
                    _actor[0].field_8 = 1;
                    if (_actor[0].cursorX < -100)
                    {
                        SetBenAnimation(0, 8);
                        _actor[0].act[1].state = 39;
                        _actor[0].field_8 = 46;
                        _actor[0].tilt = -1;
                    }
                    else
                    {
                        if (_actor[0].cursorX > 100)
                        {
                            SetBenAnimation(0, 10);
                            _actor[0].act[1].state = 55;
                            _actor[0].field_8 = 49;
                            _actor[0].tilt = 1;
                        }
                    }
                    break;
                case 1:
                    if (_actor[0].act[1].state != 55 || _actor[0].weaponClass != _actor[0].animWeaponClass)
                    {
                        SetBenAnimation(0, 10);
                        _actor[0].act[1].state = 55;
                    }
                    if (_actor[0].field_8 == 51)
                        _actor[0].tilt = 0;
                    else
                        _actor[0].tilt = 2;
                    break;
                case 2:
                    if (_actor[0].act[1].state != 56 || _actor[0].weaponClass != _actor[0].animWeaponClass)
                    {
                        SetBenAnimation(0, 11);
                        _actor[0].act[1].state = 56;
                    }
                    if (_actor[0].field_8 == 51)
                        _actor[0].tilt = 1;
                    else
                        _actor[0].tilt = 3;
                    break;
                case 3:
                    if (_actor[0].act[1].state != 57 || _actor[0].weaponClass != _actor[0].animWeaponClass)
                    {
                        SetBenAnimation(0, 12);
                        _actor[0].act[1].state = 57;
                    }

                    if (_actor[0].cursorX <= 100)
                    {
                        SetBenAnimation(0, 11);
                        _actor[0].act[1].state = 56;
                        _actor[0].field_8 = 51;
                        _actor[0].tilt = 2;
                    }
                    break;
            }

            if (_actor[0].curFacingFlag != _actor[0].newFacingFlag)
            {
                if (_actor[0].newFacingFlag == 2)
                    smlayer_setActorFacing(0, 1, 28, 180);
                else
                    smlayer_setActorFacing(0, 1, 27, 180);
            }

            tmpx = _actor[0].x + _actor[0].x1;
            tmpy = _actor[0].y + _actor[0].y1;

            if (_actor[0].act[1].room != 0)
                smlayer_putActor(0, 1, tmpx, tmpy, (byte)_smlayer_room2);
            else
                smlayer_putActor(0, 1, tmpx, tmpy, (byte)_smlayer_room);

            _actor[0].animWeaponClass = _actor[0].weaponClass;
            _actor[0].curFacingFlag = _actor[0].newFacingFlag;
        }

        // Ben
        void Actor02Reaction(int buttons)
        {
            int tmp, tmp2;

            switch (_actor[0].act[2].state)
            {
                case 1:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 2;
                    _actor[0].kicking = false;

                    switch (_actor[0].tilt)
                    {
                        case -3:
                            if (_actor[0].act[2].animTilt != -3)
                            {
                                smlayer_setActorFacing(0, 2, 6, 180);
                                _actor[0].act[2].animTilt = -3;
                            }
                            break;
                        case -2:
                            if (_actor[0].field_8 == 48)
                                smlayer_setActorFacing(0, 2, 7, 180);
                            _actor[0].act[2].animTilt = -2;
                            break;
                        case -1:
                            if (_actor[0].field_8 == 46)
                                smlayer_setActorFacing(0, 2, 8, 180);
                            _actor[0].act[2].animTilt = -1;
                            break;
                        case 0:
                            if (_actor[0].act[2].animTilt != 0)
                            {
                                smlayer_setActorFacing(0, 2, 9, 180);
                                _actor[0].act[2].animTilt = 0;
                            }
                            break;
                        case 1:
                            if (_actor[0].field_8 == 49)
                                smlayer_setActorFacing(0, 2, 10, 180);
                            _actor[0].act[2].animTilt = 1;
                            break;
                        case 2:
                            if (_actor[0].field_8 == 51)
                                smlayer_setActorFacing(0, 2, 11, 180);
                            _actor[0].act[2].animTilt = 2;
                            break;
                        case 3:
                            if (_actor[0].act[2].animTilt != 3)
                            {
                                smlayer_setActorFacing(0, 2, 12, 180);
                                _actor[0].act[2].animTilt = 3;
                            }
                            break;
                        default:
                            break;
                    }
                    _actor[0].act[2].tilt = 0;
                    break;
                case 2:
                    smlayer_setActorLayer(0, 2, 4);
                    smlayer_setActorFacing(0, 2, 17, 180);
                    _actor[0].kicking = true;
                    _actor[0].weaponClass = 1;
                    _actor[0].act[2].state = 3;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    if (!((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/))
                        smlayer_startSfx(63);
                    break;
                case 3:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    if (_actor[0].act[2].frame == 2)
                    {
                        if (_currEnemy != EN_CAVEFISH)
                        {
                            tmp = CalcEnemyDamage(true, true);
                            if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                            {
                                if (tmp == 1)
                                    smlayer_startSfx(50);
                            }
                            else
                            {
                                if (tmp == 1)
                                    smlayer_startSfx(60);
                                if (tmp == 1000)
                                    smlayer_startSfx(62);
                            }
                        }
                        else
                        {
                            if ((_actor[1].x - _actor[0].x <= WeaponMaxRange(0)) &&
                                (_actor[1].x - _actor[0].x >= WeaponMinRange(0)) &&
                                _actor[0].field_54 == 0)
                                PrepareScenePropScene(1, false, false);
                        }
                    }
                    if (_actor[0].act[2].frame >= 4)
                    {
                        smlayer_setActorFacing(0, 2, 20, 180);
                        _actor[0].act[2].state = 4;
                    }

                    _actor[0].kicking = true;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 4:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 2)
                    {
                        smlayer_setActorFacing(0, 2, 9, 180);
                        _actor[0].act[2].state = 1;
                        _actor[0].act[2].animTilt = -1000;
                        _actor[0].weaponClass = 2;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 5:
                    smlayer_setActorLayer(0, 2, 5);
                    break;
                case 6:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 2;
                    _actor[0].newFacingFlag = 1;
                    _actor[0].kicking = false;
                    smlayer_setActorCostume(0, 2, ReadArray(22));
                    smlayer_setActorFacing(0, 2, 19, 180);
                    _actor[0].act[2].state = 7;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    smlayer_startSfx(66);
                    break;
                case 7:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 2;
                    _actor[0].newFacingFlag = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 1)
                    {
                        smlayer_setActorFacing(0, 2, 20, 180);
                        _actor[0].act[2].state = 8;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 8:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 2;
                    _actor[0].newFacingFlag = 1;
                    _actor[0].kicking = false;
                    if ((_actor[0].act[2].frame == 3) && (CalcEnemyDamage(false, false) == 1))
                    {
                        _actor[1].damage += WeaponDamage(0);
                        smlayer_startSfx(64);
                        _actor[1].cursorX = 320;
                    }
                    if (_actor[0].act[2].frame >= 5)
                    {
                        smlayer_setActorFacing(0, 2, 21, 180);
                        _actor[0].act[2].state = 9;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 9:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 2;
                    _actor[0].newFacingFlag = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 3)
                    {
                        smlayer_setActorCostume(0, 2, ReadArray(12));
                        _actor[0].newFacingFlag = 2;
                        _actor[0].act[2].state = 1;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 10:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    smlayer_setActorFacing(0, 2, 19, 180);
                    _actor[0].act[2].state = 11;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    smlayer_startSfx(75);
                    break;
                case 11:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    if (_actor[0].act[2].frame >= 2)
                    {
                        if (_currEnemy == EN_VULTM2)
                        {
                            if ((_actor[1].x - _actor[0].x <= WeaponMaxRange(0)) &&
                                (_actor[1].x - _actor[0].x >= WeaponMinRange(0)) &&
                                CalcEnemyDamage(false, false) != 0)
                            {
                                smlayer_setActorFacing(0, 2, 20, 180);
                                _actor[0].act[2].state = 97;
                                _actor[0].act[2].room = 0;
                                _actor[0].act[1].room = 0;
                                _actor[0].act[0].room = 0;
                                smlayer_setActorLayer(0, 2, 25);
                                smlayer_setActorCostume(1, 2, ReadArray(45));
                                smlayer_setActorFacing(1, 2, 6, 180);
                                smlayer_startSfx(101);
                                _actor[1].act[2].state = 97;
                                _actor[1].lost = true;
                                _actor[1].act[2].room = 1;
                                _actor[1].act[1].room = 0;
                                _actor[1].act[0].room = 0;
                            }
                            else
                            {
                                smlayer_setActorFacing(0, 2, 20, 180);
                                _actor[0].act[2].state = 12;
                            }
                        }
                        else
                        {
                            smlayer_setActorFacing(0, 2, 20, 180);
                            _actor[0].act[2].state = 12;
                        }
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 12:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    if (_actor[0].act[2].frame >= 1)
                    {
                        if (_currEnemy != EN_CAVEFISH)
                        {
                            switch (_actor[1].weapon)
                            {
                                case INV_CHAIN:
                                case INV_CHAINSAW:
                                case INV_MACE:
                                case INV_2X4:
                                case INV_DUST:
                                    tmp = CalcEnemyDamage(true, true);
                                    if (tmp == 1)
                                        smlayer_startSfx(73);
                                    if (tmp == 1000)
                                        smlayer_startSfx(74);
                                    break;
                                default:
                                    if (CalcEnemyDamage(true, false) == 1)
                                        smlayer_startSfx(73);
                                    break;
                            }
                        }
                        else
                        {
                            if ((_actor[1].x - _actor[0].x <= WeaponMaxRange(0)) &&
                                (_actor[1].x - _actor[0].x >= WeaponMinRange(0)) &&
                                _actor[0].field_54 == 0)
                                PrepareScenePropScene(1, false, false);

                        }
                        smlayer_setActorFacing(0, 2, 21, 180);
                        _actor[0].act[2].state = 13;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 13:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 3)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].state = 63;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 14:
                    smlayer_setActorLayer(0, 2, 8);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    smlayer_setActorFacing(0, 2, 19, 180);
                    _actor[0].act[2].state = 15;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    smlayer_startSfx(78);
                    break;
                case 15:
                    smlayer_setActorLayer(0, 2, 8);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    if (_actor[0].act[2].frame >= 2)
                    {
                        switch (_actor[1].weapon)
                        {
                            case INV_CHAIN:
                            case INV_CHAINSAW:
                                if (WeaponBenIsEffective())
                                {
                                    smlayer_setActorFacing(0, 2, 22, 180);
                                    _actor[0].act[2].state = 81;
                                }
                                else
                                {
                                    smlayer_setActorFacing(0, 2, 20, 180);
                                    _actor[0].act[2].state = 16;
                                }
                                break;
                            case INV_MACE:
                                if (!_actor[1].kicking || _actor[1].field_44)
                                if (Actor1StateFlags(_actor[1].act[2].state))
                                {
                                    smlayer_setActorFacing(0, 2, 20, 180);
                                    _actor[0].act[2].state = 106;
                                    break;
                                }
                                smlayer_setActorFacing(0, 2, 20, 180);
                                _actor[0].act[2].state = 16;
                                break;
                            default:
                                smlayer_setActorFacing(0, 2, 20, 180);
                                _actor[0].act[2].state = 16;
                                break;
                        }
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 16:
                    smlayer_setActorLayer(0, 2, 8);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    if (_actor[0].act[2].frame >= 1)
                    {
                        switch (_actor[1].weapon)
                        {
                            case INV_CHAIN:
                            case INV_CHAINSAW:
                                tmp = CalcEnemyDamage(true, true);
                                if (tmp == 1)
                                    smlayer_startSfx(76);
                                if (tmp == 1000)
                                    smlayer_startSfx(77);
                                break;
                            case INV_BOOT:
                                CalcEnemyDamage(false, true);
                                break;
                            case INV_DUST:
                                if ((_actor[1].x - _actor[0].x <= WeaponMaxRange(0)) &&
                                    (_actor[1].x - _actor[0].x >= WeaponMinRange(0)))
                                {
                                    smlayer_startSfx(76);
                                    _actor[1].damage += WeaponDamage(0);
                                }
                                break;
                            default:
                                if (CalcEnemyDamage(true, false) != 0)
                                    smlayer_startSfx(76);
                                break;
                        }
                        smlayer_setActorFacing(0, 2, 21, 180);
                        _actor[0].act[2].state = 17;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 17:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 2)
                    {
                        smlayer_setActorFacing(0, 2, 26, 180);
                        _actor[0].act[2].state = 64;
                        smlayer_stopSound(76);
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 18:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    smlayer_setActorFacing(0, 2, 19, 180);
                    _actor[0].act[2].state = 19;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    smlayer_startSfx(69);
                    break;
                case 19:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    if (_actor[0].act[2].frame >= 1)
                    {
                        switch (_actor[1].weapon)
                        {
                            case INV_CHAIN:
                                if (_actor[1].kicking)
                                {
                                    _actor[1].act[2].state = 108;
                                    _actor[0].act[2].state = 110;
                                }
                                else
                                {
                                    smlayer_setActorFacing(0, 2, 20, 180);
                                    _actor[0].act[2].state = 20;
                                }
                                break;
                            case INV_CHAINSAW:
                                if (_actor[1].kicking || _actor[1].field_44)
                                    _actor[0].act[2].state = 106;
                                else
                                {
                                    smlayer_setActorFacing(0, 2, 20, 180);
                                    _actor[0].act[2].state = 20;
                                }
                                break;
                            case INV_MACE:
                            case INV_2X4:
                                if (WeaponBenIsEffective())
                                {
                                    smlayer_setActorFacing(0, 2, 22, 180);
                                    _actor[0].act[2].state = 77;
                                    break;
                                }
                                smlayer_setActorFacing(0, 2, 20, 180);
                                _actor[0].act[2].state = 20;
                                break;
                            default:
                                smlayer_setActorFacing(0, 2, 20, 180);
                                _actor[0].act[2].state = 20;
                                break;
                        }
                    }

                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 20:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    if (_actor[0].act[2].frame >= 1)
                    {
                        if (_currEnemy != EN_CAVEFISH)
                        {
                            switch (_actor[1].weapon)
                            {
                                case INV_CHAINSAW:
                                case INV_MACE:
                                case INV_2X4:
                                case INV_BOOT:
                                    tmp = CalcEnemyDamage(true, true);
                                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                                    {
                                        if (tmp == 1)
                                            smlayer_startSfx(52);
                                        if (tmp == 1000)
                                            smlayer_startSfx(56);
                                    }
                                    else
                                    {
                                        if (tmp == 1)
                                            smlayer_startSfx(67);
                                        if (tmp == 1000)
                                            smlayer_startSfx(68);
                                    }
                                    break;
                                default:
                                    if (CalcEnemyDamage(true, false) != 0)
                                        smlayer_startSfx(67);
                                    break;
                            }
                        }
                        else
                        {
                            if ((_actor[1].x - _actor[0].x <= WeaponMaxRange(0)) &&
                                (_actor[1].x - _actor[0].x >= WeaponMinRange(0)) &&
                                _actor[0].field_54 == 0)
                                PrepareScenePropScene(1, false, false);
                        }
                        smlayer_setActorFacing(0, 2, 21, 180);
                        _actor[0].act[2].state = 21;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 21:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 6)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].state = 65;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 22:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 0;
                    _actor[0].kicking = true;
                    smlayer_setActorFacing(0, 2, 19, 180);
                    _actor[0].act[2].state = 23;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    smlayer_startSfx(81);
                    break;
                case 23:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 0;
                    _actor[0].kicking = true;
                    if (_actor[0].act[2].frame >= 4)
                    {
                        switch (_actor[1].weapon)
                        {
                            case INV_CHAIN:
                            case INV_CHAINSAW:
                            case INV_MACE:
                            case INV_2X4:
                            case INV_BOOT:
                            case INV_DUST:
                                if (WeaponBenIsEffective())
                                {
                                    smlayer_setActorFacing(0, 2, 22, 180);
                                    _actor[0].act[2].state = 83;
                                    break;
                                }
                                smlayer_setActorFacing(0, 2, 20, 180);
                                _actor[0].act[2].state = 24;
                                break;
                            default:
                                smlayer_setActorFacing(0, 2, 20, 180);
                                _actor[0].act[2].state = 24;
                                break;
                        }
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 24:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 0;
                    _actor[0].kicking = true;
                    if (_actor[0].act[2].frame >= 1)
                    {
                        switch (_actor[1].weapon)
                        {
                            case INV_CHAIN:
                            case INV_CHAINSAW:
                            case INV_MACE:
                            case INV_2X4:
                            case INV_BOOT:
                            case INV_DUST:
                                tmp = CalcEnemyDamage(true, true);
                                if (tmp == 1)
                                {
                                    if (_currEnemy == EN_CAVEFISH)
                                    {
                                        _actor[1].lost = true;
                                        _actor[1].act[2].state = 102;
                                        _actor[1].damage = _actor[1].maxdamage + 10;
                                    }
                                    smlayer_startSfx(79);
                                }
                                if (tmp == 1000)
                                    smlayer_startSfx(80);
                                break;
                            default:
                                if (CalcEnemyDamage(true, false) == 0)
                                    smlayer_startSfx(79);
                                break;
                        }
                        smlayer_setActorFacing(0, 2, 21, 180);
                        _actor[0].act[2].state = 25;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 25:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 0;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 6)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].state = 66;
                        _actor[0].weaponClass = 1;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 26:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    smlayer_setActorFacing(0, 2, 19, 180);
                    _actor[0].act[2].state = 27;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    if (!((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/))
                        smlayer_startSfx(72);
                    break;
                case 27:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    if (_actor[0].act[2].frame >= 1)
                    {
                        switch (_actor[1].weapon)
                        {
                            case INV_CHAIN:
                            case INV_CHAINSAW:
                            case INV_MACE:
                            case INV_2X4:
                            case INV_BOOT:
                            case INV_DUST:
                                if (WeaponBenIsEffective())
                                {
                                    smlayer_setActorFacing(0, 2, 22, 180);
                                    _actor[0].act[2].state = 75;
                                    break;
                                }
                                smlayer_setActorFacing(0, 2, 20, 180);
                                _actor[0].act[2].state = 28;
                                break;
                            default:
                                smlayer_setActorFacing(0, 2, 20, 180);
                                _actor[0].act[2].state = 28;
                                break;
                        }
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 28:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    if (_actor[0].act[2].frame >= 3)
                    {
                        if (_currEnemy != EN_CAVEFISH)
                        {
                            switch (_actor[1].weapon)
                            {
                                case INV_CHAIN:
                                case INV_CHAINSAW:
                                case INV_MACE:
                                case INV_2X4:
                                case INV_BOOT:
                                case INV_DUST:
                                    tmp = CalcEnemyDamage(true, true);
                                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                                    {
                                        if (tmp == 1)
                                            smlayer_startSfx(58);
                                        if (tmp == 1000)
                                            smlayer_startSfx(56);
                                    }
                                    else
                                    {
                                        if (tmp == 1)
                                            smlayer_startSfx(70);
                                        if (tmp == 1000)
                                            smlayer_startSfx(71);
                                    }
                                    break;
                                case INV_HAND:
                                    if (CalcEnemyDamage(true, false) == 0)
                                        smlayer_startSfx(70);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            if ((_actor[1].x - _actor[0].x <= WeaponMaxRange(0)) &&
                                (_actor[1].x - _actor[0].x >= WeaponMinRange(0)) &&
                                _actor[0].field_54 == 0)
                                PrepareScenePropScene(1, false, false);
                        }
                        smlayer_setActorFacing(0, 2, 21, 180);
                        _actor[0].act[2].state = 29;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 29:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 6)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].state = 62;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 30:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    smlayer_setActorCostume(0, 2, ReadArray(21));
                    smlayer_setActorFacing(0, 2, 18, 180);
                    _actor[0].act[2].state = 31;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    smlayer_startSfx(84);
                    break;
                case 31:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    if (_actor[0].act[2].frame >= 6)
                    {
                        smlayer_setActorFacing(0, 2, 20, 180);
                        _actor[0].act[2].state = 32;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 32:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    if (_actor[0].act[2].frame >= 5)
                    {
                        switch (_currEnemy)
                        {
                            case EN_ROTT3:
                                if (CalcEnemyDamage(false, false) != 0)
                                    _actor[1].act[2].state = 115;
                                break;
                            case EN_VULTF2:
                                if (CalcEnemyDamage(false, false) != 0)
                                    _actor[1].act[2].state = 113;
                                break;
                            default:
                                tmp = CalcEnemyDamage(true, true);
                                if (tmp == 1)
                                    smlayer_startSfx(82);
                                if (tmp == 1000)
                                    smlayer_startSfx(83);
                                break;
                        }
                        smlayer_setActorFacing(0, 2, 21, 180);
                        _actor[0].act[2].state = 33;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 33:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 5)
                    {
                        smlayer_setActorCostume(0, 2, ReadArray(12));
                        _actor[0].act[2].state = 1;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 34:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].kicking = false;

                    if (!smlayer_actorNeedRedraw(0, 2))
                    {
                        SetBenState();
                        _actor[0].act[2].tilt = 0;
                        // for some reason there is no break at this
                        // place, so tilt gets overridden on next line
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 35:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].kicking = false;

                    if (!smlayer_actorNeedRedraw(0, 2))
                    {
                        SwitchBenWeapon();
                        _actor[0].act[2].state = 1;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 36:
                    _actor[0].lost = true;
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].kicking = false;
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                        smlayer_setActorCostume(0, 2, ReadArray(17));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(18));
                    smlayer_setActorFacing(0, 2, 6, 180);
                    smlayer_startSfx(96);
                    switch (_currEnemy)
                    {
                        case EN_ROTT1:
                            PrepareScenePropScene(33, false, false);
                            break;
                        case EN_ROTT2:
                            tmp = GetRandomNumber(4);
                            if (tmp != 0)
                                PrepareScenePropScene(35, false, false);
                            if (tmp == 3)
                                PrepareScenePropScene(36, false, false);
                            break;
                        case EN_VULTF1:
                            PrepareScenePropScene(6, false, false);
                            break;
                        case EN_VULTM1:
                            tmp = GetRandomNumber(4);
                            if (tmp == 0)
                                PrepareScenePropScene(40, false, false);
                            if (tmp == 3)
                                PrepareScenePropScene(41, false, false);
                            break;
                        default:
                            break;
                    }
                    _actor[0].act[2].state = 37;
                    break;
                case 37:
                    smlayer_setActorLayer(0, 2, 25);
                    _actor[0].cursorX = 0;
                    _actor[0].kicking = false;
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                    {
                        if (_actor[0].act[2].frame >= 28)
                        {
                            QueueSceneSwitch(9, null, "bencrshe.san", 64, 0, 0, 0);
                            _actor[0].act[2].state = 38;
                        }
                    }
                    else if (_actor[0].act[2].frame >= 18 ||
                             (_actor[0].x < 50 && _actor[0].act[2].frame >= 10) ||
                             (_actor[0].x > 270 && _actor[0].act[2].frame >= 10))
                    {
                        if (_currSceneId == 21)
                        {
                            QueueSceneSwitch(23, null, "benflip.san", 64, 0, 0, 0);
                        }
                        else
                        {
                            switch (_currEnemy)
                            {
                                case EN_ROTT1:
                                case EN_ROTT2:
                                case EN_ROTT3:
                                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                                        QueueSceneSwitch(9, null, "bencrshe.san", 64, 0, 0, 0);
                                    else
                                        QueueSceneSwitch(9, null, "wr2_benr.san", 64, 0, 0, 0);
                                    break;
                                case EN_VULTF1:
                                case EN_VULTM1:
                                case EN_VULTF2:
                                case EN_VULTM2:
                                    QueueSceneSwitch(9, null, "wr2_benv.san", 64, 0, 0, 0);
                                    break;
                                case EN_CAVEFISH:
                                    QueueSceneSwitch(9, null, "wr2_benc.san", 64, 0, 0, 0);
                                    break;
                                default:
                                    QueueSceneSwitch(9, null, "wr2_ben.san", 64, 0, 0, 0);
                                    break;
                            }
                        }
                        _actor[0].act[2].state = 38;
                    }
                    break;
                case 38:
                    if (_actor[0].act[2].frame >= 36)
                    {
                        _actor[0].act[2].frame = 0;
                        if (_currSceneId == 21)
                        {
                            QueueSceneSwitch(23, null, "benflip.san", 64, 0, 0, 0);
                        }
                        else
                        {
                            switch (_currEnemy)
                            {
                                case EN_ROTT1:
                                case EN_ROTT2:
                                case EN_ROTT3:
                                    QueueSceneSwitch(9, null, "wr2_benr.san", 64, 0, 0, 0);
                                    break;
                                case EN_VULTF1:
                                case EN_VULTM1:
                                case EN_VULTF2:
                                case EN_VULTM2:
                                    QueueSceneSwitch(9, null, "wr2_benv.san", 64, 0, 0, 0);
                                    break;
                                case EN_CAVEFISH:
                                    QueueSceneSwitch(9, null, "wr2_benc.san", 64, 0, 0, 0);
                                    break;
                                default:
                                    QueueSceneSwitch(9, null, "wr2_ben.san", 64, 0, 0, 0);
                                    break;
                            }
                        }
                        _actor[0].act[2].state = 38;
                    }
                    break;
                case 63:
                    smlayer_setActorLayer(0, 2, 5);
                    if (_actor[0].act[2].animTilt != 0)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].animTilt = 0;
                    }
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 64:
                    smlayer_setActorLayer(0, 2, 5);
                    if (_actor[0].act[2].animTilt != 0)
                    {
                        smlayer_setActorFacing(0, 2, 26, 180);
                        _actor[0].act[2].animTilt = 0;
                    }
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 65:
                    smlayer_setActorLayer(0, 2, 5);
                    if (_actor[0].act[2].animTilt != 0)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].animTilt = 0;
                    }
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 66:
                    smlayer_setActorLayer(0, 2, 5);
                    if (_actor[0].act[2].animTilt != 0)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].animTilt = 0;
                    }
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 62:
                    smlayer_setActorLayer(0, 2, 5);
                    if (_actor[0].act[2].animTilt != 0)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].animTilt = 0;
                    }
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 73:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = true;
                    if (_actor[0].act[2].frame >= 2 && !_kickBenProgress)
                    {
                        smlayer_setActorFacing(0, 2, 19, 180);
                        _actor[0].act[2].state = 74;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 74:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = false;
                    if (_actor[0].act[2].frame >= 2)
                    {
                        smlayer_setActorFacing(0, 2, 9, 180);
                        _actor[0].act[2].state = 1;
                        _actor[0].weaponClass = 2;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 75:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = true;
                    if (_actor[0].act[2].frame >= 4 && !_kickBenProgress)
                    {
                        smlayer_setActorFacing(0, 2, 23, 180);
                        _actor[0].act[2].state = 76;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 76:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = false;
                    if (_actor[0].act[2].frame >= 4)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].state = 62;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 77:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = true;
                    if (_actor[0].act[2].frame >= 2)
                    {
                        smlayer_setActorFacing(0, 2, 23, 180);
                        _actor[0].act[2].state = 78;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 78:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = false;
                    if (_actor[0].act[2].frame >= 5)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].state = 65;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 79:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = true;
                    if (_actor[0].act[2].frame >= 2)
                    {
                        smlayer_setActorFacing(0, 2, 23, 180);
                        _actor[0].act[2].state = 80;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 80:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = false;
                    if (_actor[0].act[2].frame >= 6)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].state = 63;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 81:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = true;
                    if (_actor[0].act[2].frame >= 2 && !_kickBenProgress)
                    {
                        smlayer_setActorFacing(0, 2, 23, 180);
                        _actor[0].act[2].state = 82;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 82:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = false;
                    if (_actor[0].act[2].frame >= 3)
                    {
                        smlayer_setActorFacing(0, 2, 26, 180);
                        _actor[0].act[2].state = 64;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 83:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 0;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = true;
                    if (_actor[0].act[2].frame >= 2 && !_kickBenProgress)
                    {
                        smlayer_setActorFacing(0, 2, 23, 180);
                        _actor[0].act[2].state = 84;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 84:
                    smlayer_setActorLayer(0, 2, 6);
                    _actor[0].weaponClass = 0;
                    _actor[0].kicking = false;
                    _actor[0].field_44 = false;
                    if (_actor[0].act[2].frame >= 5)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].state = 66;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 97:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = true;
                    if (_actor[0].act[2].frame >= 5)
                    {
                        _actor[0].act[2].room = 1;
                        _actor[0].act[1].room = 1;
                        _actor[0].act[0].room = 1;
                        smlayer_setActorFacing(0, 2, 21, 180);
                        _actor[0].act[2].state = 13;
                        _actor[0].x = _actor[1].x - 116;
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 104:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    smlayer_setActorFacing(0, 2, 28, 180);
                    _actor[0].act[2].state = 105;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 105:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 5)
                    {
                        _actor[0].act[2].state = 1;
                        _actor[0].inventory[INV_MACE] = false;
                        smlayer_startVoice(318);
                        SwitchBenWeapon();
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 106:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    smlayer_setActorFacing(0, 2, 29, 180);
                    _actor[0].act[2].state = 107;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 107:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 9)
                    {
                        _actor[0].act[2].state = 1;
                        _actor[0].inventory[INV_MACE] = false;
                        smlayer_startVoice(318);
                        SwitchBenWeapon();
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 108:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    smlayer_setActorFacing(0, 2, 28, 180);
                    _actor[0].act[2].state = 109;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 109:
                    smlayer_setActorLayer(0, 2, 5);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 5)
                    {
                        _actor[0].act[2].state = 1;
                        _actor[0].inventory[INV_CHAIN] = false; // Chain
                        smlayer_startVoice(318);
                        SwitchBenWeapon();
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 110:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    smlayer_setActorFacing(0, 2, 30, 180);
                    _actor[0].act[2].state = 111;
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                case 111:
                    smlayer_setActorLayer(0, 2, 4);
                    _actor[0].weaponClass = 1;
                    _actor[0].kicking = false;
                    if (_actor[0].act[2].frame >= 7)
                    {
                        smlayer_setActorFacing(0, 2, 25, 180);
                        _actor[0].act[2].state = 65;
                        _actor[0].inventory[INV_CHAIN] = true; // Chain
                    }
                    _actor[0].act[2].tilt = CalcTilt(_actor[0].tilt);
                    break;
                default:
                    break;
            }
            tmp = _actor[0].x + _actor[0].act[2].tilt + 17 + _actor[0].x1;
            tmp2 = _actor[0].y + _actor[0].y1 - 98;

            if (_actor[0].act[2].room != 0)
                smlayer_putActor(0, 2, tmp, tmp2, (byte)_smlayer_room2);
            else
                smlayer_putActor(0, 2, tmp, tmp2, (byte)_smlayer_room);
        }

        void Actor03Reaction(int buttons)
        {
            int tmp;

            switch (_actor[0].act[3].state)
            {
                case 1:
                    _actor[0].field_54 = 0;
                    break;
                case 52:
                    if (_actor[0].runningSound != 0)
                        smlayer_stopSound(_actor[0].runningSound);

                    if (_currScenePropIdx != 0)
                        ShutCurrentScene();

                    _actor[0].runningSound = 0;
                    _actor[0].defunct = false;
                    _actor[0].field_54 = 0;
                    smlayer_setActorFacing(0, 3, 15, 180);
                    _actor[0].act[3].state = 53;
                    break;
                case 53:
                    if (_actor[0].act[3].frame >= 2)
                    {
                        smlayer_setActorFacing(0, 3, 16, 180);
                        _actor[0].act[3].state = 54;
                    }
                    break;
                case 54:
                    break;
                case 69:
                    if (_actor[0].act[3].frame >= 2)
                        _actor[0].act[3].state = 70;
                    break;
                case 70:
                    if (_actor[0].scenePropSubIdx != 0)
                    {
                        smlayer_setActorFacing(0, 3, 4, 180);
                        tmp = _currScenePropIdx + _actor[0].scenePropSubIdx;
                        if (!smlayer_startVoice(_sceneProp[tmp].sound))
                            _actor[0].runningSound = 0;
                        else
                            _actor[0].runningSound = _sceneProp[tmp].sound;
                        _actor[0].act[3].state = 72;
                    }
                    else
                    {
                        _actor[0].act[3].state = 118;
                    }
                    break;
                case 71:
                    _actor[0].field_54 = 0;
                    if (_actor[0].act[3].frame >= 2)
                        _actor[0].act[3].state = 1;
                    break;
                case 72:
                    if (_actor[0].runningSound != 0)
                    {
                        if (!smlayer_isSoundRunning(_actor[0].runningSound))
                        {
                            smlayer_setActorFacing(0, 3, 5, 180);
                            _actor[0].act[3].state = 70;
                            _actor[0].scenePropSubIdx = 0;
                        }
                    }
                    else
                    {
                        tmp = _currScenePropIdx + _actor[0].scenePropSubIdx;
                        if (_sceneProp[tmp].counter >= _sceneProp[tmp].maxCounter)
                        {
                            smlayer_setActorFacing(0, 3, 5, 180);
                            _actor[0].act[3].state = 70;
                            _actor[0].scenePropSubIdx = 0;
                            _actor[0].runningSound = 0;
                        }
                    }
                    break;
                case 117:
                    ReinitActors();
                    smlayer_setActorFacing(0, 3, 13, 180);
                    _actor[0].act[3].state = 69;
                    break;
                case 118:
                    smlayer_setActorFacing(0, 3, 14, 180);
                    _actor[0].act[3].state = 71;
                    break;
                default:
                    break;
            }
        }

        void MineChooseRoad(int buttons)
        {
            short tmp;

            if (_actor[0].field_8 < 1)
                return;

            if (_actor[0].field_8 == 112)
            {
                if (_actor[0].frame < 18 || _needSceneSwitch)
                    return;

                QueueSceneSwitch(18, null, "fishgogg.san", 64, 0, 0, 0);
            }
            else if (_actor[0].field_8 == 1)
            {
                tmp = (short)(_actor[0].cursorX / 22);

                switch (_currSceneId)
                {
                    case 17:
                        if ((buttons & 1) != 0)
                        {
                            if (_mineCaveIsNear)
                            {
                                WriteArray(1, _posCave);
                                smush_setToFinish();
                            }

                            if (_roadBranch && !_needSceneSwitch)
                            {
                                _iactSceneId2 = _iactSceneId;
                                QueueSceneSwitch(2, null, "mineexit.san", 64, 0, 0, 0);
                            }
                        }

                        if ((buttons & 2) == 0 || _needSceneSwitch)
                            return;

                        QueueSceneSwitch(19, null, "fishgog2.san", 64, 0, 0, 0);
                        break;
                    case 1:
                        _actor[0].tilt = tmp;

                        if (tmp < -7)
                            _actor[0].tilt = -7;
                        if (tmp > 7)
                            _actor[0].tilt = 7;

                        DrawSpeedyActor(buttons);

                        if (((buttons & 1) != 0) && _currSceneId == 1 && _roadBranch && !_needSceneSwitch)
                        {
                            _iactSceneId2 = _iactSceneId;
                            QueueSceneSwitch(2, null, "mineexit.san", 64, 0, 0, 0);
                        }

                        if ((buttons & 2) == 0 || !_benHasGoggles)
                            return;

                        _actor[0].frame = 0;
                        _actor[0].field_8 = 112;
                        smlayer_setActorFacing(0, 2, 26, 180);
                        break;
                    case 4:
                    case 5:
                        _actor[0].tilt = tmp;

                        if (tmp < -7)
                            _actor[0].tilt = -7;
                        if (tmp > 7)
                            _actor[0].tilt = 7;

                        DrawSpeedyActor(buttons);

                        if ((buttons & 1) == 0)
                            return;

                        if (_roadBranch && !_needSceneSwitch)
                        {
                            _iactSceneId2 = _iactSceneId;

                            if (ReadArray(4) != 0 && _val211d < 3)
                            {
                                _val211d++;
                                QueueSceneSwitch(8, null, "fishfear.san", 64, 0, 0, 0);
                            }
                            else
                            {
                                QueueSceneSwitch(8, null, "tomine.san", 64, 0, 0, 0);
                            }
                        }

                        if (_roadStop)
                        {
                            WriteArray(1, _posBrokenTruck);
                            WriteArray(3, _val57d);
                            smush_setToFinish();
                        }

                        if (!_carIsBroken)
                            return;

                        WriteArray(1, _posBrokenCar);
                        WriteArray(3, _val57d);
                        smush_setToFinish();
                        break;
                    case 6:
                        _actor[0].tilt = tmp;

                        if (tmp < -7)
                            _actor[0].tilt = -7;
                        if (tmp > 7)
                            _actor[0].tilt = 7;

                        DrawSpeedyActor(buttons);

                        if ((buttons & 1) == 0)
                            return;

                        if (_roadBranch && !_needSceneSwitch)
                        {
                            _iactSceneId2 = _iactSceneId;

                            if (ReadArray(4) != 0 && _val211d < 3)
                            {
                                _val211d++;
                                QueueSceneSwitch(7, null, "fishfear.san", 64, 0, 0, 0);
                            }
                            else
                            {
                                QueueSceneSwitch(7, null, "tomine.san", 64, 0, 0, 0);
                            }
                        }

                        if (_roadStop)
                        {
                            WriteArray(1, _posBrokenTruck);
                            WriteArray(3, _posVista);
                            smush_setToFinish();
                        }

                        if (!_carIsBroken)
                            return;

                        WriteArray(1, _posBrokenCar);
                        WriteArray(3, _posVista);
                        smush_setToFinish();
                        break;
                    default:
                        break;
                }
            }
        }

        int ProcessBenOnRoad(bool flag)
        {
            int buttons;

            if (_actor[0].enemyHandler != -1)
                buttons = EnemyHandler(_actor[0].enemyHandler, 0, 1, _actor[0].probability);
            else
                buttons = EnemyHandler(EN_TORQUE, 0, 1, _actor[0].probability);

            if (flag)
            {
                _actor[0].speed = _actor[0].tilt;

                if (_actor[0].speed > 8)
                    _actor[0].speed = 8;

                if (_actor[0].speed < -8)
                    _actor[0].speed = -8;

                _actor[0].x += _actor[0].speed / 2 + _actor[0].speed;

                if (_actor[0].x < 0)
                    _actor[0].x = 0;

                if (_actor[0].x > 320)
                    _actor[0].x = 320;
            }

            return buttons;
        }

        void OuchSoundBen()
        {
            _actor[0].act[3].state = 52;

            if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
            {
                smlayer_startVoice(54);
                return;
            }

            switch (GetRandomNumber(3))
            {
                case 0:
                    smlayer_startVoice(315);
                    break;
                case 1:
                    smlayer_startVoice(316);
                    break;
                case 2:
                    smlayer_startVoice(317);
                    break;
                case 3:
                    smlayer_startVoice(98);
                    break;
            }
        }

        void SetBenAnimation(int actornum, int anim)
        {
            if (anim <= 12)
                smlayer_setActorFacing(actornum, 1,
                    ActorAnimationData[_actor[actornum].weaponClass * 7 + anim - 6], 180);
        }

        void ChooseBenWeaponAnim(int buttons)
        {
            // kick
            if (((buttons & 1) != 0) && (_currEnemy != EN_TORQUE))
            {
                if (!_kickBenProgress && Actor0StateFlags2(_actor[0].act[2].state + _actor[0].weapon * 119))
                {
                    switch (_actor[0].weapon)
                    {
                        case INV_CHAIN:
                            _actor[0].act[2].state = 10;
                            break;
                        case INV_CHAINSAW:
                            _actor[0].act[2].state = 14;
                            break;
                        case INV_MACE:
                            _actor[0].act[2].state = 18;
                            break;
                        case INV_2X4:
                            _actor[0].act[2].state = 22;
                            break;
                        case INV_WRENCH:
                            _actor[0].act[2].state = 26;
                            break;
                        case INV_BOOT:
                            _actor[0].act[2].state = 6;
                            break;
                        case INV_HAND:
                            _actor[0].act[2].state = 2;
                            break;
                        case INV_DUST:
                            _actor[0].act[2].state = 30;
                            break;
                        default:
                            break;
                    }
                    _actor[0].kicking = true;
                    _kickBenProgress = true;
                }
            }
            else
            {
                _kickBenProgress = false;
            }

            // switch weapon
            if (((buttons & 2) != 0) && (_currEnemy != EN_TORQUE))
            {
                if (_weaponBenJustSwitched)
                    return;

                if (!Actor0StateFlags1(_actor[0].act[2].state))
                    return;

                switch (_actor[0].weapon)
                {
                    case INV_CHAIN:
                    case INV_CHAINSAW:
                    case INV_MACE:
                    case INV_2X4:
                    case INV_WRENCH:
                        _actor[0].act[2].state = 35;
                        smlayer_setActorFacing(0, 2, 24, 180);
                        break;
                    case INV_BOOT:
                    case INV_HAND:
                    case INV_DUST:
                        _actor[0].act[2].state = 0;
                        SwitchBenWeapon();
                        break;
                }

                _weaponBenJustSwitched = true;
            }
            else
            {
                _weaponBenJustSwitched = false;
            }
        }

        bool WeaponBenIsEffective()
        {
            if ((_actor[1].x - _actor[0].x > WeaponMaxRange(0)) ||
                (_actor[1].x - _actor[0].x < WeaponMinRange(0)) ||
                !_actor[1].kicking)
                return false;

            return true;
        }

        int SetBenState()
        {
            _actor[0].act[2].animTilt = -1000;

            switch (_actor[0].weapon)
            {
                case INV_CHAIN:
                    _actor[0].weaponClass = 1;
                    _actor[0].act[2].state = 63;
                    break;
                case INV_CHAINSAW:
                    _actor[0].weaponClass = 1;
                    _actor[0].act[2].state = 64;
                    break;
                case INV_MACE:
                    _actor[0].weaponClass = 1;
                    _actor[0].act[2].state = 65;
                    break;
                case INV_2X4:
                    _actor[0].weaponClass = 1;
                    _actor[0].act[2].state = 66;
                    break;
                case INV_WRENCH:
                    _actor[0].weaponClass = 1;
                    _actor[0].act[2].state = 62;
                    break;
                case INV_BOOT:
                case INV_HAND:
                case INV_DUST:
                    _actor[0].weaponClass = 2;
                    _actor[0].act[2].state = 1;
                    break;
                default:
                    break;
            }
            return _actor[0].act[2].state;
        }

        void SwitchBenWeapon()
        {
            do
            {
                _actor[0].weapon++;
                if (_actor[0].weapon > 7)
                    _actor[0].weapon = INV_CHAIN;

            } while (!_actor[0].inventory[_actor[0].weapon]);

            switch (_actor[0].weapon)
            {
                case INV_CHAIN:
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                        smlayer_setActorCostume(0, 2, ReadArray(19));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(20));
                    smlayer_setActorFacing(0, 2, 18, 180);
                    _actor[0].weaponClass = 1;
                    _actor[0].act[2].state = 34;
                    break;
                case INV_CHAINSAW:
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                        smlayer_setActorCostume(0, 2, ReadArray(23));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(24));
                    smlayer_setActorFacing(0, 2, 18, 180);
                    _actor[0].weaponClass = 1;
                    _actor[0].act[2].state = 34;
                    break;
                case INV_MACE:
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                        smlayer_setActorCostume(0, 2, ReadArray(22));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(23));
                    smlayer_setActorFacing(0, 2, 18, 180);
                    _actor[0].weaponClass = 1;
                    _actor[0].act[2].state = 34;
                    break;
                case INV_2X4:
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                    {
                        smlayer_setActorCostume(0, 2, ReadArray(18));
                    }
                    else
                    {
                        if (_currEnemy == EN_CAVEFISH)
                            smlayer_setActorCostume(0, 2, ReadArray(38));
                        else
                            smlayer_setActorCostume(0, 2, ReadArray(19));
                    }
                    smlayer_setActorFacing(0, 2, 18, 180);
                    _actor[0].weaponClass = 1;
                    _actor[0].act[2].state = 34;
                    break;
                case INV_WRENCH:
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                        smlayer_setActorCostume(0, 2, ReadArray(24));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(25));
                    smlayer_setActorFacing(0, 2, 18, 180);
                    _actor[0].weaponClass = 1;
                    _actor[0].act[2].state = 34;
                    break;
                case INV_BOOT:
                case INV_HAND:
                case INV_DUST:
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                        smlayer_setActorCostume(0, 2, ReadArray(11));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(12));
                    _actor[0].weaponClass = 2;
                    _actor[0].act[2].state = 1;
                    break;
                default:
                    break;
            }
        }

        void DrawSpeedyActor(int buttons)
        {
            switch (_actor[0].tilt)
            {
                case -7:
                    if (_actor[0].act[2].state != 47)
                    {
                        smlayer_setActorFacing(0, 2, 13, 180);
                        _actor[0].act[2].state = 47;
                    }
                    break;
                case -6:
                    if (_actor[0].act[2].state != 44)
                    {
                        smlayer_setActorFacing(0, 2, 11, 180);
                        _actor[0].act[2].state = 44;
                    }
                    break;
                case -5:
                    if (_actor[0].act[2].state != 43)
                    {
                        smlayer_setActorFacing(0, 2, 10, 180);
                        _actor[0].act[2].state = 43;
                    }
                    break;
                case -4:
                    if (_actor[0].act[2].state != 42)
                    {
                        smlayer_setActorFacing(0, 2, 9, 180);
                        _actor[0].act[2].state = 42;
                    }
                    break;
                case -3:
                    if (_actor[0].act[2].state != 41)
                    {
                        smlayer_setActorFacing(0, 2, 8, 180);
                        _actor[0].act[2].state = 41;
                    }
                    break;
                case -2:
                    if (_actor[0].act[2].state != 40)
                    {
                        smlayer_setActorFacing(0, 2, 7, 180);
                        _actor[0].act[2].state = 40;
                    }
                    break;
                case -1:
                    if (_actor[0].act[2].state != 39)
                    {
                        smlayer_setActorFacing(0, 2, 6, 180);
                        _actor[0].act[2].state = 39;
                    }
                    break;
                case 0:
                    if (_actor[0].act[2].state != 1)
                    {
                        smlayer_setActorFacing(0, 2, 22, 180);
                        _actor[0].act[2].state = 1;
                    }
                    break;
                case 1:
                    if (_actor[0].act[2].state != 55)
                    {
                        smlayer_setActorFacing(0, 2, 14, 180);
                        _actor[0].act[2].state = 55;
                    }
                    break;
                case 2:
                    if (_actor[0].act[2].state != 56)
                    {
                        smlayer_setActorFacing(0, 2, 15, 180);
                        _actor[0].act[2].state = 56;
                    }
                    break;
                case 3:
                    if (_actor[0].act[2].state != 57)
                    {
                        smlayer_setActorFacing(0, 2, 16, 180);
                        _actor[0].act[2].state = 57;
                    }
                    break;
                case 4:
                    if (_actor[0].act[2].state != 58)
                    {
                        smlayer_setActorFacing(0, 2, 17, 180);
                        _actor[0].act[2].state = 58;
                    }
                    break;
                case 5:
                    if (_actor[0].act[2].state != 59)
                    {
                        smlayer_setActorFacing(0, 2, 18, 180);
                        _actor[0].act[2].state = 59;
                    }
                    break;
                case 6:
                    if (_actor[0].act[2].state != 60)
                    {
                        smlayer_setActorFacing(0, 2, 19, 180);
                        _actor[0].act[2].state = 60;
                    }
                    break;
                case 7:
                    if (_actor[0].act[2].state != 50)
                    {
                        smlayer_setActorFacing(0, 2, 21, 180);
                        _actor[0].act[2].state = 50;
                    }
                    break;
                default:
                    break;
            }

            if (_actor[0].act[2].room == 0)
                return;

            smlayer_putActor(0, 2, _actor[0].x + _actor[0].x1, _actor[0].y + _actor[0].y1,
                (byte)_smlayer_room2);
        }
    }
}

