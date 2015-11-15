//
//  Insane_Enemy.cs
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

using System;
using System.Diagnostics;
using NScumm.Core;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Insane
{
    partial class Insane
    {
        void TurnEnemy(bool battle)
        {
            int buttons;

            if (_actor[1].damage < _actor[1].maxdamage)
            {
                _actor[1].lost = false;
            }
            else
            {
                if (!_actor[1].lost && !_actor[0].lost)
                {
                    _actor[1].lost = true;
                    _actor[1].act[2].state = 36;
                    _actor[1].act[1].state = 36;
                    _actor[1].act[0].state = 36;
                    _actor[1].act[1].room = 0;
                    _actor[1].act[0].room = 0;
                }
            }

            buttons = 0;

            if (!_actor[1].lost)
            if (battle)
                buttons = ActionEnemy();

            Debug.WriteLine("11:{0} 12:{1} 13:{2} 10:{3}", _actor[1].act[1].state,
                _actor[1].act[2].state, _actor[1].act[3].state, _actor[1].act[0].state);
            Actor11Reaction(buttons);
            Actor12Reaction(buttons);
            Actor13Reaction(buttons);
            Actor10Reaction(buttons);
        }

        int ActionEnemy()
        {
            int buttons;

            if (_actor[1].enemyHandler != -1)
                buttons = EnemyHandler(_actor[1].enemyHandler, 1, 0, _actor[1].probability);
            else
                buttons = EnemyHandler(EN_TORQUE, 1, 0, _actor[1].probability);

            if (_actor[1].tilt != 0)
            {
                _actor[1].speed += _actor[1].cursorX / 40;
            }
            else
            {
                if (_actor[1].speed < 0)
                    _actor[1].speed++;
                else
                    _actor[1].speed--;
            }

            if (_actor[1].speed > 8)
                _actor[1].speed = 8;

            if (_actor[1].speed < -8)
                _actor[1].speed = -8;

            _actor[1].x += _actor[0].speed;

            if (_actor[1].x > 250)
                _actor[1].x--;
            else if (_actor[1].x < 250)
                _actor[1].x++;

            if (_actor[1].x > 320)
            {
                _actor[1].x = 320;
                _actor[1].damage++;
                _actor[1].x1 = -_actor[1].x1;
                _actor[1].damage++;

                return buttons;
            }

            if (!_actor[1].lost)
            {
                if (_actor[0].x + 90 > _actor[1].x)
                    _actor[1].x = _actor[0].x + 90;
            }

            if (_actor[1].x < 0)
            {
                _actor[1].x = 0;
                _actor[1].x1 = -_actor[1].x1;
                _actor[1].damage++;
            }
            else if (_actor[1].x > 310)
            {
                _actor[1].x1 = -_actor[1].x1;
                _actor[1].damage++;
            }

            return buttons;
        }

        int EnemyHandler(int num, int actor1, int actor2, int probability)
        {
            switch (num)
            {
                case EN_ROTT1:
                    return Enemy0handler(actor1, actor2, probability);
                case EN_ROTT2:
                    return Enemy1handler(actor1, actor2, probability);
                case EN_ROTT3:
                    return Enemy2handler(actor1, actor2, probability);
                case EN_VULTF1:
                    return Enemy3handler(actor1, actor2, probability);
                case EN_VULTM1:
                    return Enemy4handler(actor1, actor2, probability);
                case EN_VULTF2:
                    return Enemy5handler(actor1, actor2, probability);
                case EN_VULTM2:
                    return Enemy6handler(actor1, actor2, probability);
                case EN_CAVEFISH:
                    return Enemy7handler(actor1, actor2, probability);
                case EN_TORQUE:
                    return Enemy8handler(actor1, actor2, probability);
                case EN_BEN:
                    return EnemyBenHandler(actor1, actor2, probability);
                case -1:
                    // nothing
                    break;
            }
            return 0;
        }

        // FIXME: this is exact actor00Reaction. Combine
        void Actor10Reaction(int buttons)
        {
            int tmpx, tmpy;

            switch (_actor[1].tilt)
            {
                case -3:
                    if (_actor[1].act[0].state != 41)
                    {
                        smlayer_setActorFacing(1, 0, 6, 180);
                        _actor[1].act[0].state = 41;
                    }
                    break;
                case -2:
                    if (_actor[1].act[0].state != 40)
                    {
                        smlayer_setActorFacing(1, 0, 7, 180);
                        _actor[1].act[0].state = 40;
                    }
                    break;
                case -1:
                    if (_actor[1].act[0].state != 39)
                    {
                        smlayer_setActorFacing(1, 0, 8, 180);
                        _actor[1].act[0].state = 39;
                    }
                    break;
                case 0:
                    if (_actor[1].act[0].state != 1)
                    {
                        smlayer_setActorFacing(1, 0, 9, 180);
                        _actor[1].act[0].state = 1;
                    }
                    break;
                case 1:
                    if (_actor[1].act[0].state != 55)
                    {
                        smlayer_setActorFacing(1, 0, 10, 180);
                        _actor[1].act[0].state = 55;
                    }
                    break;
                case 2:
                    if (_actor[1].act[0].state != 56)
                    {
                        smlayer_setActorFacing(1, 0, 11, 180);
                        _actor[1].act[0].state = 56;
                    }
                    break;
                case 3:
                    if (_actor[1].act[0].state != 57)
                    {
                        smlayer_setActorFacing(1, 0, 12, 180);
                        _actor[1].act[0].state = 57;
                    }
                    break;
                default:
                    break;
            }
            tmpx = _actor[1].x + _actor[1].x1;
            tmpy = _actor[1].y + _actor[1].y1;

            if (_actor[1].act[0].room != 0)
                smlayer_putActor(1, 0, tmpx, tmpy, (byte)_smlayer_room2);
            else
                smlayer_putActor(1, 0, tmpx, tmpy, (byte)_smlayer_room);
        }

        void Actor11Reaction(int buttons)
        {
            int tmpx, tmpy;

            ChooseEnemyWeaponAnim(buttons);

            switch (_actor[1].tilt)
            {
                case -3:
                    if (_actor[1].act[1].state != 41 || _actor[1].weaponClass != _actor[1].animWeaponClass)
                    {
                        SetEnemyAnimation(1, 6);
                        _actor[1].act[1].state = 41;
                    }

                    if (_actor[1].cursorX >= -100)
                    {
                        SetEnemyAnimation(1, 7);
                        _actor[1].act[1].state = 40;
                        _actor[1].field_8 = 48;
                        _actor[1].tilt = -2;
                    }

                    _actor[1].x += _actor[1].cursorX / 32;
                    break;
                case -2:
                    if (_actor[1].act[1].state != 40 || _actor[1].weaponClass != _actor[1].animWeaponClass)
                    {
                        SetEnemyAnimation(1, 7);
                        _actor[1].act[1].state = 40;
                    }
                    if (_actor[1].field_8 == 48)
                        _actor[1].tilt = -1;
                    else
                        _actor[1].tilt = -3;

                    _actor[1].x += _actor[1].cursorX / 32;
                    break;
                case -1:
                    if (_actor[1].act[1].state != 39 || _actor[1].weaponClass != _actor[1].animWeaponClass)
                    {
                        SetEnemyAnimation(1, 8);
                        _actor[1].act[1].state = 39;
                    }

                    if (_actor[1].field_8 == 48)
                        _actor[1].tilt = 0;
                    else
                        _actor[1].tilt = -2;

                    _actor[1].x += _actor[1].cursorX / 32;
                    break;
                case 0:
                    if (_actor[1].act[1].state != 1 || _actor[1].weaponClass != _actor[1].animWeaponClass)
                    {
                        SetEnemyAnimation(1, 9);
                        _actor[1].act[1].state = 1;
                    }
                    _actor[1].field_8 = 1;
                    if (_actor[1].cursorX < -100)
                    {
                        SetEnemyAnimation(1, 8);
                        _actor[1].act[1].state = 39;
                        _actor[1].field_8 = 46;
                        _actor[1].tilt = -1;
                    }
                    else
                    {
                        if (_actor[1].cursorX > 100)
                        {
                            SetEnemyAnimation(1, 10);
                            _actor[1].act[1].state = 55;
                            _actor[1].field_8 = 49;
                            _actor[1].tilt = 1;
                        }
                    }
                    break;
                case 1:
                    if (_actor[1].act[1].state != 55 || _actor[1].weaponClass != _actor[1].animWeaponClass)
                    {
                        SetEnemyAnimation(1, 10);
                        _actor[1].act[1].state = 55;
                    }
                    if (_actor[1].field_8 == 51)
                        _actor[1].tilt = 0;
                    else
                        _actor[1].tilt = 2;

                    _actor[1].x += _actor[1].cursorX / 32;
                    break;
                case 2:
                    if (_actor[1].act[1].state != 56 || _actor[1].weaponClass != _actor[1].animWeaponClass)
                    {
                        SetEnemyAnimation(1, 11);
                        _actor[1].act[1].state = 56;
                    }
                    if (_actor[1].field_8 == 51)
                        _actor[1].tilt = 1;
                    else
                        _actor[1].tilt = 3;

                    _actor[1].x += _actor[1].cursorX / 32;
                    break;
                case 3:
                    if (_actor[1].act[1].state != 57 || _actor[1].weaponClass != _actor[1].animWeaponClass)
                    {
                        SetEnemyAnimation(1, 12);
                        _actor[1].act[1].state = 57;
                    }

                    if (_actor[1].cursorX <= 100)
                    {
                        SetEnemyAnimation(1, 11);
                        _actor[1].act[1].state = 56;
                        _actor[1].field_8 = 51;
                        _actor[1].tilt = 2;
                    }

                    _actor[1].x += _actor[1].cursorX / 32;
                    break;
            }

            tmpx = _actor[1].x;
            tmpy = _actor[1].y + _actor[1].y1;

            if (_actor[1].act[1].room != 0)
                smlayer_putActor(1, 1, tmpx, tmpy, (byte)_smlayer_room2);
            else
                smlayer_putActor(1, 1, tmpx, tmpy, (byte)_smlayer_room);

            _actor[1].animWeaponClass = _actor[1].weaponClass;
        }

        void Actor12Reaction(int buttons)
        {
            int tmp, tmp2;

            switch (_actor[1].act[2].state)
            {
                case 1:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].weaponClass = 2;
                    _actor[1].kicking = false;

                    switch (_actor[1].tilt)
                    {
                        case -3:
                            if (_actor[1].act[2].animTilt != -3)
                            {
                                smlayer_setActorFacing(1, 2, 6, 180);
                                _actor[1].act[2].animTilt = -3;
                            }
                            break;
                        case -2:
                            if (_actor[1].field_8 == 48)
                                smlayer_setActorFacing(1, 2, 7, 180);
                            _actor[1].act[2].animTilt = -2;
                            break;
                        case -1:
                            if (_actor[1].field_8 == 46)
                                smlayer_setActorFacing(1, 2, 8, 180);
                            _actor[1].act[2].animTilt = -1;
                            break;
                        case 0:
                            if (_actor[1].act[2].animTilt != 0)
                            {
                                smlayer_setActorFacing(1, 2, 9, 180);
                                _actor[1].act[2].animTilt = 0;
                            }
                            break;
                        case 1:
                            if (_actor[1].field_8 == 49)
                                smlayer_setActorFacing(1, 2, 10, 180);
                            _actor[1].act[2].animTilt = 1;
                            break;
                        case 2:
                            if (_actor[1].field_8 == 51)
                                smlayer_setActorFacing(1, 2, 11, 180);
                            _actor[1].act[2].animTilt = 2;
                            break;
                        case 3:
                            if (_actor[1].act[2].animTilt != 3)
                            {
                                smlayer_setActorFacing(1, 2, 12, 180);
                                _actor[1].act[2].animTilt = 3;
                            }
                            break;
                        default:
                            break;
                    }
                    _actor[1].act[2].tilt = 0;
                    break;
                case 2:
                    smlayer_setActorLayer(1, 2, 4);
                    smlayer_setActorFacing(1, 2, 17, 180);
                    _actor[1].kicking = true;
                    _actor[1].weaponClass = 1;
                    _actor[1].act[2].state = 3;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    if (!((_vm.Game.Features.HasFlag(Scumm.IO.GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/))
                        smlayer_startSfx(63);
                    break;
                case 3:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    if (_actor[1].act[2].frame >= 6)
                    {
                        tmp = CalcBenDamage(true, true);
                        if ((_vm.Game.Features.HasFlag(Scumm.IO.GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
                        {
                            if (tmp == 1)
                                smlayer_startSfx(50);
                        }
                        else if (tmp == 1)
                            smlayer_startSfx(60);
                        else if (tmp == 1000)
                            smlayer_startSfx(62);
                        smlayer_setActorFacing(1, 2, 20, 180);
                        _actor[1].act[2].state = 4;
                    }
                    _actor[1].kicking = true;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 4:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    if (_actor[1].act[2].frame >= 2)
                    {
                        smlayer_setActorFacing(1, 2, 9, 180);
                        _actor[1].act[2].state = 1;
                        _actor[1].act[2].animTilt = -1000;
                        _actor[1].weaponClass = 2;
                        _actor[1].kicking = false;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 5:
                    smlayer_setActorLayer(1, 2, 5);
                    break;
                case 10:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    smlayer_setActorFacing(1, 2, 19, 180);
                    _actor[1].act[2].state = 11;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    smlayer_startSfx(75);
                    break;
                case 11:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    if (_actor[1].act[2].frame >= 2)
                    {
                        if (WeaponEnemyIsEffective())
                        {
                            smlayer_setActorFacing(1, 2, 22, 180);
                            _actor[1].act[2].state = 79;
                        }
                        else
                        {
                            smlayer_setActorFacing(1, 2, 20, 180);
                            _actor[1].act[2].state = 12;
                        }
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 12:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    if (_actor[1].act[2].frame >= 1)
                    {
                        switch (_actor[0].weapon)
                        {
                            case INV_CHAIN:
                            case INV_CHAINSAW:
                            case INV_MACE:
                            case INV_2X4:
                            case INV_DUST:
                            case INV_WRENCH:
                            case INV_BOOT:
                                tmp = CalcBenDamage(true, true);
                                if (tmp == 1)
                                    smlayer_startSfx(73);
                                if (tmp == 1000)
                                    smlayer_startSfx(74);
                                break;
                            case INV_HAND:
                                if (CalcBenDamage(true, false) != 0)
                                    smlayer_startSfx(73);
                                break;
                        }
                        smlayer_setActorFacing(1, 2, 21, 180);
                        _actor[1].act[2].state = 13;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 13:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    if (_actor[1].act[2].frame >= 3)
                    {
                        smlayer_setActorFacing(1, 2, 25, 180);
                        _actor[1].act[2].state = 63;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 14:
                    smlayer_setActorLayer(1, 2, 8);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    smlayer_setActorFacing(1, 2, 19, 180);
                    _actor[1].act[2].state = 15;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    smlayer_startSfx(78);
                    break;
                case 15:
                    smlayer_setActorLayer(1, 2, 8);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    if (_actor[1].act[2].frame >= 5)
                    {
                        switch (_actor[0].weapon)
                        {
                            case INV_CHAIN:
                            case INV_CHAINSAW:
                            case INV_MACE:
                            case INV_2X4:
                            case INV_WRENCH:
                                if (WeaponEnemyIsEffective())
                                {
                                    smlayer_setActorFacing(1, 2, 22, 180);
                                    _actor[1].act[2].state = 81;
                                }
                                else
                                {
                                    smlayer_setActorFacing(1, 2, 20, 180);
                                    _actor[1].act[2].state = 16;
                                }
                                break;
                            default:
                                smlayer_setActorFacing(1, 2, 20, 180);
                                _actor[1].act[2].state = 16;
                                break;
                        }
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 16:
                    smlayer_setActorLayer(1, 2, 8);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    if (_actor[1].act[2].frame >= 3)
                    {
                        switch (_actor[0].weapon)
                        {
                            case INV_CHAIN:
                            case INV_CHAINSAW:
                            case INV_MACE:
                            case INV_2X4:
                            case INV_WRENCH:
                                tmp = CalcBenDamage(true, true);
                                if (tmp == 1)
                                    smlayer_startSfx(76);
                                if (tmp == 1000)
                                    smlayer_startSfx(77);
                                break;
                            default:
                                CalcBenDamage(true, false);
                                break;
                        }
                        smlayer_setActorFacing(1, 2, 21, 180);
                        _actor[1].act[2].state = 17;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 17:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    if (_actor[1].act[2].frame >= 1)
                    {
                        smlayer_setActorFacing(1, 2, 26, 180);
                        _actor[1].act[2].state = 64;
                        smlayer_stopSound(76);
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 18:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    smlayer_setActorFacing(1, 2, 19, 180);
                    _actor[1].act[2].state = 19;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    if (!((_vm.Game.Features.HasFlag(Scumm.IO.GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/))
                    {
                        smlayer_startSfx(69);
                        if (_actor[1].field_54 == 0)
                        {
                            tmp = GetRandomNumber(4);
                            if (tmp == 1)
                                smlayer_startSfx(213);
                            else if (tmp == 3)
                                smlayer_startSfx(215);
                        }
                    }
                    else
                    {
                        smlayer_startSfx(53);
                    }
                    break;
                case 19:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    if (_actor[1].act[2].frame >= 3)
                    {
                        switch (_actor[0].weapon)
                        {
                            case INV_CHAIN:
                                if (_actor[0].kicking)
                                {
                                    _actor[0].act[2].state = 108;
                                    _actor[1].act[2].state = 110;
                                }
                                else
                                {
                                    smlayer_setActorFacing(1, 2, 20, 180);
                                    _actor[1].act[2].state = 20;
                                }
                                break;
                            case INV_CHAINSAW:
                                if (_actor[0].kicking || _actor[0].field_44)
                                    _actor[1].act[2].state = 106;
                                else
                                {
                                    smlayer_setActorFacing(1, 2, 20, 180);
                                    _actor[1].act[2].state = 20;
                                }
                                break;
                            case INV_BOOT:
                            case INV_DUST:
                                smlayer_setActorFacing(1, 2, 20, 180);
                                _actor[1].act[2].state = 20;
                                break;
                            default:
                                if (WeaponEnemyIsEffective())
                                {
                                    smlayer_setActorFacing(1, 2, 22, 180);
                                    _actor[1].act[2].state = 77;
                                }
                                else
                                {
                                    smlayer_setActorFacing(1, 2, 20, 180);
                                    _actor[1].act[2].state = 20;
                                }
                                break;
                        }
                    }
                    _actor[1].kicking = true;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 20:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    if (_actor[1].act[2].frame >= 1)
                    {
                        switch (_actor[1].weapon)
                        {
                            case INV_CHAINSAW:
                            case INV_MACE:
                            case INV_2X4:
                            case INV_BOOT:
                                tmp = CalcBenDamage(true, true);
                                if ((_vm.Game.Features.HasFlag(Scumm.IO.GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
                                {
                                    if (tmp == 1)
                                        smlayer_startSfx(52);
                                    else if (tmp == 1000)
                                        smlayer_startSfx(56);
                                }
                                else
                                {
                                    if (tmp == 1)
                                        smlayer_startSfx(67);
                                    else if (tmp == 1000)
                                        smlayer_startSfx(68);
                                }
                                break;
                            default:
                                CalcBenDamage(true, false);
                                break;
                        }
                        smlayer_setActorFacing(1, 2, 21, 180);
                        _actor[1].act[2].state = 21;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 21:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    if (_actor[1].act[2].frame >= 5)
                    {
                        smlayer_setActorFacing(1, 2, 25, 180);
                        smlayer_setActorLayer(1, 2, 5);
                        _actor[1].act[2].state = 65;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 22:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 0;
                    _actor[1].kicking = true;
                    smlayer_setActorFacing(1, 2, 19, 180);
                    _actor[1].act[2].state = 23;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    smlayer_startSfx(81);
                    break;
                case 23:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 0;
                    _actor[1].kicking = true;
                    if (_actor[1].act[2].frame >= 3)
                    {
                        if (WeaponEnemyIsEffective())
                        {
                            smlayer_setActorFacing(1, 2, 22, 180);
                            _actor[1].act[2].state = 83;
                        }
                        else
                        {
                            smlayer_setActorFacing(1, 2, 20, 180);
                            _actor[1].act[2].state = 24;

                            if (_actor[1].field_54 == 0)
                                smlayer_startSfx(246);
                        }
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 24:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 0;
                    _actor[1].kicking = true;
                    if (_actor[1].act[2].frame >= 1)
                    {
                        tmp = CalcBenDamage(true, true);

                        if (tmp == 1)
                            smlayer_startSfx(79);

                        if (tmp == 1000)
                            smlayer_startSfx(80);

                        smlayer_setActorFacing(1, 2, 21, 180);
                        _actor[1].act[2].state = 25;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 25:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].weaponClass = 0;
                    _actor[1].kicking = false;
                    if (_actor[1].act[2].frame >= 3)
                    {
                        smlayer_setActorFacing(1, 2, 25, 180);
                        _actor[1].act[2].state = 66;
                        _actor[1].weaponClass = 1;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 26:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    smlayer_setActorFacing(1, 2, 19, 180);
                    _actor[1].act[2].state = 27;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    smlayer_startSfx(72);
                    break;
                case 27:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    if (_actor[1].act[2].frame >= 1)
                    {
                        if (WeaponEnemyIsEffective())
                        {
                            smlayer_setActorFacing(1, 2, 22, 180);
                            _actor[1].act[2].state = 75;
                        }
                        else
                        {
                            smlayer_setActorFacing(1, 2, 20, 180);
                            _actor[1].act[2].state = 28;
                        }
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 28:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = true;
                    if (_actor[1].act[2].frame >= 3)
                    {
                        tmp = CalcBenDamage(true, true);
                        if ((_vm.Game.Features.HasFlag(Scumm.IO.GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
                        {
                            if (tmp == 1)
                                smlayer_startSfx(57);
                        }
                        else if (tmp == 1)
                            smlayer_startSfx(70);
                        else if (tmp == 1000)
                            smlayer_startSfx(71);

                        smlayer_setActorFacing(1, 2, 21, 180);
                        _actor[1].act[2].state = 29;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 29:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    if (_actor[1].act[2].frame >= 6)
                    {
                        smlayer_setActorFacing(1, 2, 25, 180);
                        _actor[1].act[2].state = 62;
                        _actor[1].kicking = false;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 34:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].kicking = false;

                    if (!smlayer_actorNeedRedraw(1, 2))
                    {
                        SetEnemyState();
                        _actor[1].act[2].tilt = 0;
                        // for some reason there is no break at this
                        // place, so tilt gets overridden on next line
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 35:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].kicking = false;

                    if (!smlayer_actorNeedRedraw(1, 2))
                        SwitchEnemyWeapon();
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 36:
                    _actor[1].lost = true;
                    _actor[1].field_54 = 1;
                    _actor[1].cursorX = 0;
                    _actor[1].kicking = false;
                    smlayer_setActorCostume(1, 2, ReadArray(_enemy[_currEnemy].costumevar));
                    smlayer_setActorFacing(1, 2, 6, 180);
                    smlayer_setActorLayer(1, 2, 25);
                    _actor[1].act[2].state = 37;

                    if (!((_vm.Game.Features.HasFlag(Scumm.IO.GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/))
                    {
                        smlayer_startSfx(96);
                        switch (_currEnemy)
                        {
                            case EN_ROTT1:
                                smlayer_startVoice(212);
                                break;
                            case EN_ROTT2:
                                smlayer_startVoice(259);
                                break;
                            case EN_ROTT3:
                                smlayer_startVoice(232);
                                break;
                            case EN_VULTF1:
                                smlayer_startVoice(281);
                                break;
                            case EN_VULTF2:
                                smlayer_startVoice(276);
                                break;
                        }
                    }
                    break;
                case 37:
                    _actor[1].cursorX = 0;
                    _actor[1].kicking = false;

                    if (_actor[1].act[2].frame < _enemy[_currEnemy].maxframe)
                    {
                        if (_actor[1].x >= 50 && _actor[1].x <= 270)
                            break;

                        if (_actor[1].act[2].frame < _enemy[_currEnemy].maxframe / 2)
                            break;
                    }
                    if (_currSceneId == 21)
                    {
                        QueueSceneSwitch(22, null, "rottflip.san", 64, 0, 0, 0);
                        _actor[1].act[2].state = 38;
                    }
                    else
                    {
                        QueueSceneSwitch(11, null, _enemy[_currEnemy].filename, 64, 0, 0, 0);
                        _actor[1].act[2].state = 38;
                    }
                    break;
                case 38:
                    smlayer_setActorLayer(1, 2, 25);
                    _actor[1].kicking = false;

                    if (_actor[1].act[2].frame < _enemy[_currEnemy].maxframe + 20)
                        break;

                    _actor[1].act[2].frame = 0;

                    if (_currSceneId == 21)
                    {
                        QueueSceneSwitch(22, null, "rottflip.san", 64, 0, 0, 0);
                        _actor[1].act[2].state = 38;
                    }
                    else
                    {
                        QueueSceneSwitch(11, null, _enemy[_currEnemy].filename, 64, 0, 0, 0);
                        _actor[1].act[2].state = 38;
                    }
                    break;
                case 63:
                    smlayer_setActorLayer(1, 2, 5);
                    if (_actor[1].act[2].animTilt != 0)
                    {
                        smlayer_setActorFacing(1, 2, 25, 180);
                        _actor[1].act[2].animTilt = 0;
                    }
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 64:
                    smlayer_setActorLayer(1, 2, 5);
                    if (_actor[1].act[2].animTilt != 0)
                    {
                        smlayer_setActorFacing(1, 2, 26, 180);
                        _actor[1].act[2].animTilt = 0;
                    }
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 62:
                case 65:
                case 66:
                    smlayer_setActorLayer(1, 2, 5);
                    if (_actor[1].act[2].animTilt != 0)
                    {
                        smlayer_setActorFacing(1, 2, 25, 180);
                        _actor[1].act[2].animTilt = 0;
                    }
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 73:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = true;
                    if (_actor[1].act[2].frame >= 2 && !_kickEnemyProgress)
                    {
                        smlayer_setActorFacing(1, 2, 19, 180);
                        _actor[1].act[2].state = 74;
                    }

                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 74:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = false;
                    if (_actor[1].act[2].frame >= 2)
                    {
                        smlayer_setActorFacing(1, 2, 9, 180);
                        _actor[1].act[2].state = 1;
                        _actor[1].weaponClass = 2;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 75:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = true;
                    if (_actor[1].act[2].frame >= 4 && !_kickEnemyProgress)
                    {
                        smlayer_setActorFacing(1, 2, 23, 180);
                        _actor[1].act[2].state = 76;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 76:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = false;
                    if (_actor[1].act[2].frame >= 4)
                    {
                        smlayer_setActorFacing(1, 2, 25, 180);
                        _actor[1].act[2].state = 62;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 77:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = true;
                    if (_actor[1].act[2].frame >= 1 && !_kickEnemyProgress)
                    {
                        smlayer_setActorFacing(1, 2, 23, 180);
                        _actor[1].act[2].state = 78;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 78:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = false;
                    if (_actor[1].act[2].frame >= 5)
                    {
                        smlayer_setActorFacing(1, 2, 25, 180);
                        _actor[1].act[2].state = 65;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 79:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = true;
                    if (_actor[1].act[2].frame >= 1 && !_kickEnemyProgress)
                    {
                        smlayer_setActorFacing(1, 2, 23, 180);
                        _actor[1].act[2].state = 80;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 80:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = false;
                    if (_actor[1].act[2].frame >= 6)
                    {
                        smlayer_setActorFacing(1, 2, 25, 180);
                        _actor[1].act[2].state = 63;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 81:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = true;
                    if (_actor[1].act[2].frame >= 2 && !_kickEnemyProgress)
                    {
                        smlayer_setActorFacing(1, 2, 23, 180);
                        _actor[1].act[2].state = 82;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 82:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = false;
                    if (_actor[1].act[2].frame >= 3)
                    {
                        smlayer_setActorFacing(1, 2, 26, 180);
                        _actor[1].act[2].state = 64;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 83:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 0;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = true;
                    if (_actor[1].act[2].frame >= 2 && !_kickEnemyProgress)
                    {
                        smlayer_setActorFacing(1, 2, 23, 180);
                        _actor[1].act[2].state = 84;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 84:
                    smlayer_setActorLayer(1, 2, 6);
                    _actor[1].weaponClass = 0;
                    _actor[1].kicking = false;
                    _actor[1].field_44 = false;
                    if (_actor[1].act[2].frame >= 5)
                    {
                        smlayer_setActorFacing(1, 2, 25, 180);
                        _actor[1].act[2].state = 66;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 89:
                    smlayer_setActorLayer(1, 2, 26);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    if (_roadBumps)
                        smlayer_setActorFacing(1, 2, 13, 180);
                    else
                        smlayer_setActorFacing(1, 2, 12, 180);

                    smlayer_startSfx(100);
                    _actor[1].act[2].state = 90;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    smlayer_setActorLayer(1, 2, 26);
                    break;
                case 90:
                    smlayer_setActorLayer(1, 2, 26);
                    _actor[1].weaponClass = 2;
                    _actor[1].kicking = false;
                    if (_actor[1].act[2].frame >= 5)
                    if (_actor[1].x - _actor[0].x <= 125)
                        _actor[0].damage += 90;

                    if (_actor[1].act[2].frame >= 12)
                    {
                        _actor[1].kicking = false;
                        SetEnemyState();
                        smlayer_setActorLayer(1, 2, 5);
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 91:
                    smlayer_setActorLayer(1, 2, 26);
                    _actor[1].kicking = false;
                    break;
                case 92:
                case 96:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].kicking = false;
                    break;
                case 93:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    smlayer_setActorFacing(1, 2, 18, 180);
                    _actor[1].act[2].state = 94;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    smlayer_startSfx(102);
                    break;
                case 94:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    if (_actor[1].act[2].frame >= 15)
                    {
                        smlayer_setActorCostume(1, 2, ReadArray(44));
                        smlayer_setActorFacing(1, 2, 6, 180);
                        _actor[1].act[2].state = 95;
                        _actor[1].act[0].room = 0;
                        _actor[1].act[1].room = 0;
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 95:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].kicking = false;
                    if (_actor[1].act[2].frame >= 19)
                    {
                        QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0,
                            _continueFrame1, 1300);
                        _actor[1].act[2].state = 96;
                    }
                    break;
                case 97:
                    smlayer_setActorLayer(1, 2, 25);
                    _actor[1].lost = true;
                    if (_actor[1].act[2].frame >= 18)
                    {
                        WriteArray(7, 1);
                        _enemy[EN_VULTM2].isEmpty = 1;
                        QueueSceneSwitch(12, null, "getnitro.san", 0, 0, 0, 0);
                    }
                    break;
                case 98:
                    smlayer_setActorLayer(1, 2, 5);
                    if (_actor[1].act[2].animTilt != 0)
                    {
                        smlayer_setActorFacing(1, 2, 6, 180);
                        _actor[1].act[2].animTilt = 0;
                    }
                    if (_roadBumps)
                    {
                        smlayer_setActorFacing(1, 2, 7, 180);
                        _actor[1].act[2].state = 100;
                    }
                    _actor[1].kicking = false;
                    break;
                case 99:
                    smlayer_setActorLayer(1, 2, 5);
                    if (_actor[1].act[2].animTilt != 0)
                    {
                        smlayer_setActorFacing(1, 2, 9, 180);
                        _actor[1].act[2].animTilt = 0;
                    }
                    if (!_roadBumps)
                    {
                        smlayer_setActorFacing(1, 2, 8, 180);
                        _actor[1].act[2].state = 101;
                    }
                    _actor[1].kicking = false;
                    break;
                case 100:
                    smlayer_setActorLayer(1, 2, 5);
                    if (_actor[1].act[2].frame >= 4)
                    {
                        smlayer_setActorFacing(1, 2, 9, 180);
                        _actor[1].act[2].state = 99;
                    }
                    _actor[1].kicking = false;
                    break;
                case 101:
                    smlayer_setActorLayer(1, 2, 5);
                    if (_actor[1].act[2].frame >= 4)
                    {
                        smlayer_setActorFacing(1, 2, 6, 180);
                        _actor[1].act[2].state = 98;
                    }
                    _actor[1].kicking = false;
                    break;
                case 102:
                    _actor[1].lost = true;
                    _actor[1].cursorX = 0;
                    _actor[1].kicking = false;
                    smlayer_setActorCostume(1, 2, ReadArray(40));
                    smlayer_setActorFacing(1, 2, 6, 180);
                    smlayer_setActorLayer(1, 2, 25);
                    _actor[1].act[2].state = 103;
                    break;
                case 103:
                    _actor[1].kicking = false;

                    if (_actor[1].act[2].frame >= 18 || ((_actor[1].x < 50 || _actor[1].x > 270) &&
                        _actor[1].act[2].frame >= 9))
                    {
                        _enemy[EN_CAVEFISH].isEmpty = 1;
                        QueueSceneSwitch(20, null, "wr2_cvko.san", 64, 0, 0, 0);
                        _actor[1].act[2].state = 38;
                    }
                    break;
                case 106:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    smlayer_setActorFacing(1, 2, 29, 180);
                    _actor[1].act[2].state = 107;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 107:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    if (_actor[1].act[2].frame >= 8)
                        _actor[1].damage = _actor[1].maxdamage + 10;

                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 108:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    smlayer_setActorFacing(1, 2, 28, 180);
                    _actor[1].act[2].state = 109;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 109:
                    smlayer_setActorLayer(1, 2, 5);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    if (_actor[1].act[2].frame >= 6)
                        _actor[1].damage = _actor[1].maxdamage + 10;

                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 110:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    smlayer_setActorFacing(1, 2, 30, 180);
                    _actor[1].act[2].state = 111;
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 111:
                    smlayer_setActorLayer(1, 2, 4);
                    _actor[1].weaponClass = 1;
                    _actor[1].kicking = false;
                    if (_actor[1].act[2].frame >= 7)
                    {
                        smlayer_setActorFacing(1, 2, 25, 180);
                        _actor[1].act[2].state = 65;
                        smlayer_setActorLayer(1, 2, 5);
                    }
                    _actor[1].act[2].tilt = CalcTilt(_actor[1].tilt);
                    break;
                case 113:
                    _actor[1].lost = true;
                    _actor[1].kicking = false;
                    smlayer_setActorCostume(1, 2, ReadArray(46));
                    smlayer_setActorFacing(1, 2, 6, 180);
                    smlayer_setActorLayer(1, 2, 25);
                    _actor[1].act[1].room = 0;
                    _actor[1].act[0].room = 0;
                    _actor[1].cursorX = 0;
                    _actor[1].act[2].state = 114;
                    _enemy[EN_VULTF2].isEmpty = 1;
                    smlayer_startVoice(275);
                    break;
                case 114:
                    smlayer_setActorLayer(1, 2, 25);
                    _actor[1].kicking = false;

                    if (_actor[1].act[2].frame >= 16 || ((_actor[1].x < 50 || _actor[1].x > 270)
                        && (_actor[1].act[2].frame >= 8)))
                    {
                        QueueSceneSwitch(11, null, _enemy[_currEnemy].filename, 64, 0, 0, 0);
                        _actor[1].act[2].state = 38;
                    }
                    break;
                case 115:
                    _actor[1].lost = true;
                    _actor[1].kicking = false;
                    smlayer_setActorCostume(1, 2, ReadArray(47));
                    smlayer_setActorFacing(1, 2, 6, 180);
                    smlayer_setActorLayer(1, 2, 25);
                    _actor[1].act[1].room = 0;
                    _actor[1].act[0].room = 0;
                    _actor[1].cursorX = 0;
                    _actor[1].act[2].state = 116;
                    break;
                case 116:
                    smlayer_setActorLayer(1, 2, 25);
                    _actor[1].kicking = false;

                    if (_actor[1].act[2].frame >= 17 || ((_actor[1].x < 50 || _actor[1].x > 270)
                        && _actor[1].act[2].frame >= 8))
                    {
                        QueueSceneSwitch(11, null, _enemy[_currEnemy].filename, 64, 0, 0, 0);
                        _actor[1].act[2].state = 38;
                    }
                    break;
                default:
                    break;
            }
            tmp = _actor[1].x + _actor[1].act[2].tilt - 17;
            tmp2 = _actor[1].y + _actor[1].y1 - 98;

            if (_actor[1].act[2].room != 0)
                smlayer_putActor(1, 2, tmp, tmp2, (byte)_smlayer_room2);
            else
                smlayer_putActor(1, 2, tmp, tmp2, (byte)_smlayer_room);
        }

        void Actor13Reaction(int buttons)
        {
            int tmp;

            switch (_actor[1].act[3].state)
            {
                case 1:
                case 54:
                    _actor[1].field_54 = 0;
                    break;
                case 52:
                    if (_actor[1].runningSound != 0)
                        smlayer_stopSound(_actor[1].runningSound);

                    if (_currScenePropIdx != 0)
                        ShutCurrentScene();

                    _actor[1].runningSound = 0;
                    _actor[1].defunct = false;
                    _actor[1].field_54 = 0;
                    smlayer_setActorFacing(1, 3, 15, 180);
                    _actor[1].act[3].state = 53;
                    break;
                case 53:
                    _actor[1].field_54 = 0;
                    if (_actor[1].act[3].frame >= 2)
                    {
                        smlayer_setActorFacing(1, 3, 16, 180);
                        _actor[1].act[3].state = 54;
                    }
                    break;
                case 69:
                    if (_actor[1].act[3].frame >= 2)
                        _actor[1].act[3].state = 70;
                    break;
                case 70:
                    if (_actor[1].scenePropSubIdx != 0)
                    {
                        smlayer_setActorFacing(1, 3, 4, 180);
                        tmp = _currScenePropIdx + _actor[1].scenePropSubIdx;
                        if (!smlayer_startVoice(_sceneProp[tmp].sound))
                            _actor[1].runningSound = 0;
                        else
                            _actor[1].runningSound = _sceneProp[tmp].sound;
                        _actor[1].act[3].state = 72;
                    }
                    else
                    {
                        _actor[1].act[3].state = 118;
                    }
                    break;
                case 71:
                    _actor[1].field_54 = 0;
                    if (_actor[1].act[3].frame >= 2)
                        _actor[1].act[3].state = 1;
                    break;
                case 72:
                    if (_actor[1].runningSound != 0)
                    {
                        if (!smlayer_isSoundRunning(_actor[1].runningSound))
                        {
                            smlayer_setActorFacing(1, 3, 5, 180);
                            _actor[1].act[3].state = 70;
                            _actor[1].scenePropSubIdx = 0;
                        }
                    }
                    else
                    {
                        tmp = _currScenePropIdx + _actor[1].scenePropSubIdx;
                        if (_sceneProp[tmp].counter >= _sceneProp[tmp].maxCounter)
                        {
                            smlayer_setActorFacing(1, 3, 5, 180);
                            _actor[1].act[3].state = 70;
                            _actor[1].scenePropSubIdx = 0;
                            _actor[1].runningSound = 0;
                        }
                    }
                    break;
                case 117:
                    smlayer_setActorFacing(1, 3, 13, 180);
                    _actor[1].field_54 = 1;
                    _actor[1].act[3].state = 69;
                    break;
                case 118:
                    smlayer_setActorFacing(1, 3, 14, 180);
                    _actor[1].act[3].state = 71;
                    break;
                default:
                    break;
            }
        }

        int EnemyInitializer(int num, int actor1, int actor2, int probability)
        {
            switch (num)
            {
                case EN_ROTT1:
                    return Enemy0initializer(actor1, actor2, probability);
                case EN_ROTT2:
                    return Enemy1initializer(actor1, actor2, probability);
                case EN_ROTT3:
                    return Enemy2initializer(actor1, actor2, probability);
                case EN_VULTF1:
                    return Enemy3initializer(actor1, actor2, probability);
                case EN_VULTM1:
                    return Enemy4initializer(actor1, actor2, probability);
                case EN_VULTF2:
                    return Enemy5initializer(actor1, actor2, probability);
                case EN_VULTM2:
                    return Enemy6initializer(actor1, actor2, probability);
                case EN_CAVEFISH:
                    return Enemy7initializer(actor1, actor2, probability);
                case EN_TORQUE:
                    return Enemy8initializer(actor1, actor2, probability);
                case -1:
                    // nothing
                    break;
            }

            return 0;
        }

        int Enemy0initializer(int actor1, int actor2, int probability)
        {
            int i;

            for (i = 0; i < 9; i++)
                _enemyState[EN_ROTT1, i] = 0;

            for (i = 0; i < 9; i++)
                _enHdlVar[EN_ROTT1, i] = 0;

            _beenCheated = false;

            return 1;
        }

        int Enemy0handler(int actor1, int actor2, int probability)
        {
            int dist;

            int retval = 0;
            int act1damage = _actor[actor1].damage; // ebx
            int act2damage = _actor[actor2].damage; // ebp
            int act1x = _actor[actor1].x; // esi
            int act2x = _actor[actor2].x; // edi

            if (!_actor[actor1].defunct)
            {
                if (_enHdlVar[EN_ROTT1, 1] > _enHdlVar[EN_ROTT1, 2])
                {
                    if (act1damage - act2damage >= 30)
                    {
                        if (GetRandomNumber(probability - 1) != 1)
                            _enHdlVar[EN_ROTT1, 0] = 0;
                        else
                            _enHdlVar[EN_ROTT1, 0] = 1;
                    }
                    _enHdlVar[EN_ROTT1, 1] = 0;
                    _enHdlVar[EN_ROTT1, 2] = GetRandomNumber(probability * 2 - 1);
                }

                dist = Math.Abs(act1x - act2x);

                if (_enHdlVar[EN_ROTT1, 3] > _enHdlVar[EN_ROTT1, 4])
                {
                    if (_enHdlVar[EN_ROTT1, 0] == 1)
                    {
                        if (WeaponMaxRange(actor1) < dist)
                        {
                            if (act2x < act1x)
                                _actor[actor1].cursorX = -101;
                            else
                                _actor[actor1].cursorX = 101;
                        }
                        else
                        {
                            if (WeaponMinRange(actor1) > dist)
                            {
                                if (act2x < act1x)
                                    _actor[actor1].cursorX = 101;
                                else
                                    _actor[actor1].cursorX = -101;
                            }
                            else
                            {
                                _actor[actor1].cursorX = 0;
                            }
                        }
                    }
                    else
                    {
                        if (WeaponMaxRange(actor2) >= dist)
                        {
                            if (act2x < act1x)
                                _actor[actor1].cursorX = 101;
                            else
                                _actor[actor1].cursorX = -101;
                        }
                        else
                        {
                            _actor[actor1].cursorX = 0;
                        }
                    }
                    _enHdlVar[EN_ROTT1, 3] = 0;
                    _enHdlVar[EN_ROTT1, 4] = GetRandomNumber(probability - 1);
                }

                if (_enHdlVar[EN_ROTT1, 5] > _enHdlVar[EN_ROTT1, 6])
                {
                    if (WeaponMaxRange(actor2) + 40 >= dist)
                    {
                        if (GetRandomNumber(probability - 1) == 1)
                            retval = 1;
                    }
                    if (_actor[actor2].kicking)
                    {
                        if (WeaponMaxRange(actor2) >= dist)
                        if (GetRandomNumber(probability * 2 - 1) <= 1)
                            retval = 1;
                    }
                    _enHdlVar[EN_ROTT1, 5] = 0;
                    _enHdlVar[EN_ROTT1, 6] = GetRandomNumber(probability - 1) / 2;
                }

                if (_actor[actor1].weapon == -1)
                    retval = 2;

                if ((_actor[actor1].field_54 == 0) &&
                    (!_actor[actor2].lost) &&
                    (!_actor[actor1].lost))
                {
                    if (_actor[actor1].act[3].state == 54)
                    {
                        switch (GetRandomNumber(9))
                        {
                            case 3:
                                if (_enemyState[EN_ROTT1, 6] == 0)
                                {
                                    _enemyState[EN_ROTT1, 6] = 1;
                                    PrepareScenePropScene(54, false, false);
                                }
                                break;
                            case 8:
                                if (_enemyState[EN_ROTT1, 4] == 0)
                                {
                                    _enemyState[EN_ROTT1, 4] = 1;
                                    PrepareScenePropScene(52, false, false);
                                }
                                break;
                        }
                    }
                    else
                    {
                        switch (GetRandomNumber(14))
                        {
                            case 2:
                                if (_enemyState[EN_ROTT1, 2] == 0)
                                {
                                    _enemyState[EN_ROTT1, 2] = 1;
                                    PrepareScenePropScene(50, false, false);
                                }
                                break;
                            case 4:
                                if (_enemyState[EN_ROTT1, 3] == 0)
                                {
                                    _enemyState[EN_ROTT1, 3] = 1;
                                    PrepareScenePropScene(51, false, false);
                                }
                                break;
                            case 6:
                                if (_enemyState[EN_ROTT1, 7] == 0)
                                {
                                    _enemyState[EN_ROTT1, 7] = 1;
                                    if (_enemy[EN_ROTT1].occurences != 0)
                                        PrepareScenePropScene(55, false, false);
                                }
                                break;
                            case 9:
                                if (_enemyState[EN_ROTT1, 5] == 0)
                                {
                                    _enemyState[EN_ROTT1, 5] = 1;
                                    PrepareScenePropScene(53, false, false);
                                }
                                break;
                            case 11:
                                if (_enemyState[EN_ROTT1, 8] == 0)
                                {
                                    _enemyState[EN_ROTT1, 8] = 1;
                                    PrepareScenePropScene(56, false, false);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                _enHdlVar[EN_ROTT1, 1]++;
                _enHdlVar[EN_ROTT1, 3]++;
                _enHdlVar[EN_ROTT1, 5]++;
            }

            if (act1x > 310)
                _actor[actor1].cursorX = -320;
            else if (act1x < 10)
                _actor[actor1].cursorX = 320;
            else if (act1x > 280)
                _actor[actor1].cursorX = -160;

            // Shift+V cheat to win the battle
            if (GetKeyState(KeyCode.LeftShift) && GetKeyState(KeyCode.V) && !_beenCheated &&
                !_actor[0].lost && !_actor[1].lost)
            {
                _beenCheated = true;
                _actor[1].damage = _actor[1].maxdamage + 10;
            }

            return retval;
        }

        int Enemy1initializer(int actor1, int actor2, int probability)
        {
            int i;

            for (i = 0; i < 9; i++)
                _enemyState[EN_ROTT2, i] = 0;

            for (i = 0; i < 9; i++)
                _enHdlVar[EN_ROTT2, i] = 0;

            _beenCheated = false;

            return 1;
        }

        int Enemy1handler(int actor1, int actor2, int probability)
        {
            int dist;

            int retval = 0;
            int act1damage = _actor[actor1].damage; // ebx
            int act2damage = _actor[actor2].damage; // ebp
            int act1x = _actor[actor1].x; // esi
            int act2x = _actor[actor2].x; // edi

            if (!_actor[actor1].defunct)
            {
                if (_enHdlVar[EN_ROTT2, 1] > _enHdlVar[EN_ROTT2, 2])
                {
                    if (act1damage - act2damage >= 30)
                    {
                        if (GetRandomNumber(probability - 1) != 1)
                            _enHdlVar[EN_ROTT2, 0] = 0;
                        else
                            _enHdlVar[EN_ROTT2, 0] = 1;
                    }
                    else
                    {
                        _enHdlVar[EN_ROTT2, 0] = 1;
                    }
                    _enHdlVar[EN_ROTT2, 1] = 0;
                    _enHdlVar[EN_ROTT2, 2] = GetRandomNumber(probability * 2 - 1);
                }

                dist = Math.Abs(act1x - act2x);

                if (_enHdlVar[EN_ROTT2, 3] > _enHdlVar[EN_ROTT2, 4])
                {
                    if (_enHdlVar[EN_ROTT2, 0] == 1)
                    {
                        if (WeaponMaxRange(actor1) < dist)
                        {
                            if (act2x < act1x)
                                _actor[actor1].cursorX = -101;
                            else
                                _actor[actor1].cursorX = 101;
                        }
                        else
                        {
                            if (WeaponMinRange(actor1) > dist)
                            {
                                if (act2x < act1x)
                                    _actor[actor1].cursorX = 101;
                                else
                                    _actor[actor1].cursorX = -101;
                            }
                            else
                            {
                                _actor[actor1].cursorX = 0;
                            }
                        }
                    }
                    else
                    {
                        if (WeaponMaxRange(actor2) >= dist)
                        {
                            if (act2x < act1x)
                                _actor[actor1].cursorX = 101;
                            else
                                _actor[actor1].cursorX = -101;
                        }
                        else
                        {
                            _actor[actor1].cursorX = 0;
                        }
                    }
                    _enHdlVar[EN_ROTT2, 3] = 0;
                    _enHdlVar[EN_ROTT2, 4] = GetRandomNumber(probability - 1);
                }

                if (_enHdlVar[EN_ROTT2, 5] > _enHdlVar[EN_ROTT2, 6])
                {
                    if (WeaponMaxRange(actor2) + 40 >= dist)
                    {
                        if (GetRandomNumber(probability - 1) == 1)
                            retval = 1;
                    }
                    if (_actor[actor2].kicking)
                    {
                        if (WeaponMaxRange(actor2) >= dist)
                        if (GetRandomNumber(probability * 2 - 1) <= 1)
                            retval = 1;
                    }
                    _enHdlVar[EN_ROTT1, 5] = 0;
                    _enHdlVar[EN_ROTT1, 6] = GetRandomNumber(probability - 1) / 2;
                }

                if (_actor[actor1].weapon == -1)
                    retval = 2;

                if ((_actor[actor1].field_54 == 0) &&
                    (!_actor[actor2].lost) &&
                    (!_actor[actor1].lost))
                {
                    if (_actor[actor1].act[3].state == 54)
                    {
                        switch (GetRandomNumber(9))
                        {
                            case 3:
                                if (_enemyState[EN_ROTT2, 6] == 0)
                                {
                                    _enemyState[EN_ROTT2, 6] = 1;
                                    PrepareScenePropScene(38, false, false);
                                }
                                break;
                            case 8:
                                if (_enemyState[EN_ROTT2, 5] == 0)
                                {
                                    _enemyState[EN_ROTT2, 5] = 1;
                                    PrepareScenePropScene(37, false, false);
                                }
                                break;
                        }
                    }
                    else
                    {
                        switch (GetRandomNumber(14))
                        {
                            case 2:
                                if (_enemyState[EN_ROTT2, 2] == 0)
                                {
                                    _enemyState[EN_ROTT2, 2] = 1;
                                    PrepareScenePropScene(34, false, false);
                                }
                                break;
                            case 11:
                                if (_enemyState[EN_ROTT1, 7] == 0)
                                {
                                    _enemyState[EN_ROTT1, 7] = 1;
                                    PrepareScenePropScene(39, false, false);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                _enHdlVar[EN_ROTT2, 1]++;
                _enHdlVar[EN_ROTT2, 3]++;
                _enHdlVar[EN_ROTT2, 5]++;
            }

            if (act1x > 310)
                _actor[actor1].cursorX = -320;
            else if (act1x < 10)
                _actor[actor1].cursorX = 320;
            else if (act1x > 280)
                _actor[actor1].cursorX = -160;
            else if (_actor[actor1].defunct)
                _actor[actor1].cursorX = 0;

            // Shift+V cheat to win the battle
            if (GetKeyState(KeyCode.LeftShift) && GetKeyState(KeyCode.V) && !_beenCheated &&
                !_actor[0].lost && !_actor[1].lost)
            {
                _beenCheated = true;
                _actor[1].damage = _actor[1].maxdamage + 10;
            }

            return retval;
        }

        int Enemy2handler(int actor1, int actor2, int probability)
        {
            int dist;

            int retval = 0;
            int act1damage = _actor[actor1].damage; // ebx
            int act2damage = _actor[actor2].damage; // ebp
            int act1x = _actor[actor1].x; // esi
            int act2x = _actor[actor2].x; // edi

            if (!_actor[actor1].defunct)
            {
                if (_enHdlVar[EN_ROTT3, 1] > _enHdlVar[EN_ROTT3, 2])
                {
                    if (act1damage - act2damage >= 30)
                    {
                        if (GetRandomNumber(probability - 1) != 1)
                            _enHdlVar[EN_ROTT3, 0] = 0;
                        else
                            _enHdlVar[EN_ROTT3, 0] = 1;
                    }
                    else
                        _enHdlVar[EN_ROTT3, 0] = 1;

                    _enHdlVar[EN_ROTT3, 1] = 0;
                    _enHdlVar[EN_ROTT3, 2] = GetRandomNumber(probability * 2 - 1);
                }

                dist = Math.Abs(act1x - act2x);

                if (_enHdlVar[EN_ROTT3, 3] > _enHdlVar[EN_ROTT3, 4])
                {
                    if (_enHdlVar[EN_ROTT3, 0] == 1)
                    {
                        if (WeaponMaxRange(actor1) < dist)
                        {
                            if (act2x < act1x)
                                _actor[actor1].cursorX = -101;
                            else
                                _actor[actor1].cursorX = 101;
                        }
                        else
                        {
                            if (WeaponMinRange(actor1) > dist)
                            {
                                if (act2x < act1x)
                                    _actor[actor1].cursorX = 101;
                                else
                                    _actor[actor1].cursorX = -101;
                            }
                            else
                            {
                                _actor[actor1].cursorX = 0;
                            }
                        }
                    }
                    else
                    {
                        if (WeaponMaxRange(actor2) >= dist)
                        {
                            if (act2x < act1x)
                                _actor[actor1].cursorX = 101;
                            else
                                _actor[actor1].cursorX = -101;
                        }
                        else
                        {
                            _actor[actor1].cursorX = 0;
                        }
                    }
                    _enHdlVar[EN_ROTT3, 3] = 0;
                    _enHdlVar[EN_ROTT3, 4] = GetRandomNumber(probability - 1);
                }
                if (_enHdlVar[EN_ROTT3, 5] > _enHdlVar[EN_ROTT3, 6])
                {
                    if (WeaponMaxRange(actor2) + 40 >= dist)
                    {
                        if (GetRandomNumber(probability - 1) == 1)
                            retval = 1;
                    }
                    if (_actor[actor2].kicking)
                    {
                        if (WeaponMaxRange(actor2) <= dist)
                        if (GetRandomNumber(probability * 2 - 1) <= 1)
                            retval = 1;
                    }
                    _enHdlVar[EN_ROTT3, 5] = 0;
                    _enHdlVar[EN_ROTT3, 6] = GetRandomNumber(probability - 1) / 2;
                }

                if (_actor[actor1].weapon == -1)
                    retval = 2;

                if ((_actor[actor1].field_54 == 0) &&
                    (!_actor[actor2].lost) &&
                    (!_actor[actor1].lost))
                {
                    if (_actor[actor1].act[3].state == 54)
                    {
                        switch (GetRandomNumber(9))
                        {
                            case 3:
                                if (_enemyState[EN_ROTT3, 1] == 0)
                                {
                                    _enemyState[EN_ROTT3, 1] = 1;
                                    PrepareScenePropScene(26, false, false);
                                }
                                break;
                            case 5:
                                if (_enemyState[EN_ROTT3, 3] == 0)
                                {
                                    _enemyState[EN_ROTT3, 3] = 1;
                                    PrepareScenePropScene(28, false, false);
                                }
                                break;
                            case 8:
                                if (_enemyState[EN_ROTT3, 2] == 0)
                                {
                                    _enemyState[EN_ROTT3, 2] = 1;
                                    PrepareScenePropScene(27, false, false);
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (_actor[actor1].kicking)
                        {
                            if (GetRandomNumber(9) == 9)
                            {
                                if (_enemyState[EN_ROTT3, 6] == 0)
                                {
                                    _enemyState[EN_ROTT3, 6] = 1;
                                    PrepareScenePropScene(31, false, false);
                                }
                            }
                        }
                        else
                        {
                            if (GetRandomNumber(14) == 7)
                            {
                                if (_enemyState[EN_ROTT3, 5] == 0)
                                {
                                    _enemyState[EN_ROTT3, 5] = 1;
                                    PrepareScenePropScene(30, false, false);
                                }
                            }
                        }
                    }
                }
                _enHdlVar[EN_ROTT3, 1]++;
                _enHdlVar[EN_ROTT3, 3]++;
                _enHdlVar[EN_ROTT3, 5]++;
            }

            if (act1x > 310)
                _actor[actor1].cursorX = -320;
            else if (act1x < 10)
                _actor[actor1].cursorX = 320;
            else if (act1x > 280)
                _actor[actor1].cursorX = -160;

            // Shift+V cheat to win the battle
            if (GetKeyState(KeyCode.LeftShift) && GetKeyState(KeyCode.V) && !_beenCheated &&
                !_actor[0].lost && !_actor[1].lost)
            {
                _beenCheated = true;
                _actor[1].damage = _actor[1].maxdamage + 10;
            }

            return retval;
        }

        int Enemy2initializer(int actor1, int actor2, int probability)
        {
            int i;

            for (i = 0; i < 7; i++)
                _enemyState[EN_ROTT3, i] = 0;

            for (i = 0; i < 9; i++)
                _enHdlVar[EN_ROTT3, i] = 0;

            _beenCheated = false;

            return 1;
        }

        int Enemy3handler(int actor1, int actor2, int probability)
        {
            int dist;

            int retval = 0;
            int act1damage = _actor[actor1].damage; // ebx
            int act2damage = _actor[actor2].damage; // ebp
            int act1x = _actor[actor1].x; // esi
            int act2x = _actor[actor2].x; // edi

            if (!_actor[actor1].defunct)
            {
                if (_enHdlVar[EN_VULTF1, 1] > _enHdlVar[EN_VULTF1, 2])
                {
                    if (act1damage - act2damage >= 30)
                    {
                        if (GetRandomNumber(probability - 1) != 1)
                            _enHdlVar[EN_VULTF1, 0] = 0;
                        else
                            _enHdlVar[EN_VULTF1, 0] = 1;
                    }
                    else
                        _enHdlVar[EN_VULTF1, 0] = 1;

                    _enHdlVar[EN_VULTF1, 1] = 0;
                    _enHdlVar[EN_VULTF1, 2] = GetRandomNumber(probability * 2 - 1);
                }

                dist = Math.Abs(act1x - act2x);

                if (_enHdlVar[EN_VULTF1, 3] > _enHdlVar[EN_VULTF1, 4])
                {
                    if (_enHdlVar[EN_VULTF1, 0] == 1)
                    {
                        if (WeaponMaxRange(actor1) < dist)
                        {
                            if (act2x < act1x)
                                _actor[actor1].cursorX = -101;
                            else
                                _actor[actor1].cursorX = 101;
                        }
                        else
                        {
                            if (WeaponMinRange(actor1) > dist)
                            {
                                if (act2x < act1x)
                                    _actor[actor1].cursorX = 101;
                                else
                                    _actor[actor1].cursorX = -101;
                            }
                            else
                            {
                                _actor[actor1].cursorX = 0;
                            }
                        }
                    }
                    else
                    {
                        if (WeaponMaxRange(actor2) >= dist)
                        {
                            if (act2x < act1x)
                                _actor[actor1].cursorX = 101;
                            else
                                _actor[actor1].cursorX = -101;
                        }
                        else
                        {
                            _actor[actor1].cursorX = 0;
                        }
                    }
                    _enHdlVar[EN_VULTF1, 3] = 0;
                    _enHdlVar[EN_VULTF1, 4] = GetRandomNumber(probability - 1);
                }

                if (_enHdlVar[EN_VULTF1, 5] > _enHdlVar[EN_VULTF1, 6])
                {
                    if (_enHdlVar[EN_VULTF1, 0] == 1)
                    {
                        if (WeaponMaxRange(actor2) + 40 >= dist)
                        if (GetRandomNumber(probability - 1) == 1)
                            retval = 1;
                    }
                    else
                    {
                        if (_actor[actor1].kicking)
                        if (WeaponMaxRange(actor2) >= dist)
                        if (GetRandomNumber(probability - 1) <= 1)
                            retval = 1;
                    }
                    _enHdlVar[EN_VULTF1, 5] = 0;
                    _enHdlVar[EN_VULTF1, 6] = GetRandomNumber(probability - 1) / 2;
                }

                if (_actor[actor1].weapon == -1)
                    retval = 2;

                if ((_actor[actor1].field_54 == 0) &&
                    (!_actor[actor2].lost) &&
                    (!_actor[actor1].lost))
                {
                    _enHdlVar[EN_VULTF1, 8] = GetRandomNumber(25);
                    if (_enHdlVar[EN_VULTF1, 8] != _enHdlVar[EN_VULTF1, 7])
                    {
                        switch (_enHdlVar[EN_VULTF1, 8])
                        {
                            case 0:
                                if (_enemyState[EN_VULTF1, 4] == 0)
                                {
                                    _enemyState[EN_VULTF1, 4] = 1;
                                    PrepareScenePropScene(3, false, false);
                                }
                                break;
                            case 1:
                                if (_enemyState[EN_VULTF1, 1] == 0)
                                {
                                    _enemyState[EN_VULTF1, 1] = 1;
                                    PrepareScenePropScene(4, false, false);
                                }
                                break;
                            case 2:
                                if (_enemyState[EN_VULTF1, 2] == 0)
                                {
                                    _enemyState[EN_VULTF1, 2] = 1;
                                    PrepareScenePropScene(5, false, false);
                                }
                                break;
                            case 3:
                                if (_enemyState[EN_VULTF1, 3] == 0)
                                {
                                    _enemyState[EN_VULTF1, 3] = 1;
                                    PrepareScenePropScene(6, false, false);
                                }
                                break;
                            case 4:
                                if (_enemyState[EN_VULTF1, 4] == 0)
                                {
                                    _enemyState[EN_VULTF1, 4] = 1;
                                    PrepareScenePropScene(7, false, false);
                                }
                                break;
                            case 5:
                                if (_enemyState[EN_VULTF1, 5] == 0)
                                {
                                    _enemyState[EN_VULTF1, 5] = 1;
                                    PrepareScenePropScene(8, false, false);
                                }
                                break;
                        }
                        _enHdlVar[EN_VULTF1, 7] = _enHdlVar[EN_VULTF1, 8];
                    }

                }
                _enHdlVar[EN_VULTF1, 1]++;
                _enHdlVar[EN_VULTF1, 3]++;
                _enHdlVar[EN_VULTF1, 5]++;
            }
            else
            {
                _actor[actor1].cursorX = 0;
            }

            if (act1x > 310)
                _actor[actor1].cursorX = -320;
            else if (act1x < 10)
                _actor[actor1].cursorX = 320;
            else if (act1x > 280)
                _actor[actor1].cursorX = -160;

            // Shift+V cheat to win the battle
            if (GetKeyState(KeyCode.LeftShift) && GetKeyState(KeyCode.V) && !_beenCheated &&
                !_actor[0].lost && !_actor[1].lost)
            {
                _beenCheated = true;
                _actor[1].damage = _actor[1].maxdamage + 10;
            }

            return retval;
        }

        int Enemy3initializer(int actor1, int actor2, int probability)
        {
            int i;

            for (i = 0; i < 6; i++)
                _enemyState[EN_VULTF1, i] = 0;

            for (i = 0; i < 9; i++)
                _enHdlVar[EN_VULTF1, i] = 0;

            _beenCheated = false;

            return 1;
        }

        int Enemy4handler(int actor1, int actor2, int probability)
        {
            int dist;

            int retval = 0;
            int act1damage = _actor[actor1].damage; // ebx
            int act2damage = _actor[actor2].damage; // ebp
            int act1x = _actor[actor1].x; // esi
            int act2x = _actor[actor2].x; // edi

            if (!_actor[actor1].defunct)
            {
                if (_enHdlVar[EN_VULTM1, 1] > _enHdlVar[EN_VULTM1, 2])
                {
                    if (act1damage - act2damage >= 30)
                    {
                        if (GetRandomNumber(probability - 1) != 1)
                            _enHdlVar[EN_VULTM1, 0] = 0;
                        else
                            _enHdlVar[EN_VULTM1, 0] = 1;
                    }
                    else
                        _enHdlVar[EN_VULTM1, 0] = 1;

                    _enHdlVar[EN_VULTM1, 1] = 0;
                    _enHdlVar[EN_VULTM1, 2] = GetRandomNumber(probability * 2 - 1);
                }

                dist = Math.Abs(act1x - act2x);

                if (_enHdlVar[EN_VULTM1, 3] > _enHdlVar[EN_VULTM1, 4])
                {
                    if (_enHdlVar[EN_VULTM1, 0] == 1)
                    {
                        if (WeaponMaxRange(actor1) < dist)
                        {
                            if (act2x < act1x)
                                _actor[actor1].cursorX = -101;
                            else
                                _actor[actor1].cursorX = 101;
                        }
                        else
                        {
                            if (WeaponMinRange(actor1) > dist)
                            {
                                if (act2x < act1x)
                                    _actor[actor1].cursorX = 101;
                                else
                                    _actor[actor1].cursorX = -101;
                            }
                            else
                            {
                                _actor[actor1].cursorX = 0;
                            }
                        }
                    }
                    else
                    {
                        if (WeaponMaxRange(actor2) >= dist)
                        {
                            if (act2x < act1x)
                                _actor[actor1].cursorX = 101;
                            else
                                _actor[actor1].cursorX = -101;
                        }
                        else
                        {
                            _actor[actor1].cursorX = 0;
                        }
                    }
                    _enHdlVar[EN_VULTM1, 3] = 0;
                    _enHdlVar[EN_VULTM1, 4] = GetRandomNumber(probability - 1);
                }
                if (_enHdlVar[EN_VULTM1, 5] > _enHdlVar[EN_VULTM1, 6])
                {
                    if (WeaponMaxRange(actor2) + 40 >= dist)
                    {
                        if (GetRandomNumber(probability - 1) == 1)
                            retval = 1;
                    }
                    if (_actor[actor2].kicking)
                    {
                        if (WeaponMaxRange(actor2) >= dist)
                        if (GetRandomNumber(probability * 2 - 1) <= 1)
                            retval = 1;
                    }
                    _enHdlVar[EN_VULTM1, 5] = 0;
                    _enHdlVar[EN_VULTM1, 6] = GetRandomNumber(probability - 1) / 2;
                }

                if (_actor[actor1].weapon == -1)
                    retval = 2;

                if ((_actor[actor1].field_54 == 0) &&
                    (!_actor[actor2].lost) &&
                    (!_actor[actor1].lost))
                {
                    if (_actor[actor1].act[3].state == 54)
                    {
                        switch (GetRandomNumber(9))
                        {
                            case 4:
                                if (_enemyState[EN_VULTM1, 7] == 0)
                                {
                                    _enemyState[EN_VULTM1, 7] = 1;
                                    PrepareScenePropScene(46, false, false);
                                }
                                break;
                            case 8:
                                if (_enemyState[EN_VULTM1, 8] == 0)
                                {
                                    _enemyState[EN_VULTM1, 8] = 1;
                                    PrepareScenePropScene(47, false, false);
                                }
                                break;
                        }
                    }
                    else
                    {
                        if (_actor[actor1].kicking)
                        {
                            switch (GetRandomNumber(9))
                            {
                                case 3:
                                    PrepareScenePropScene(44, false, false);
                                    break;
                                case 9: // Original is 10 here which never happens
                                    PrepareScenePropScene(45, false, false);
                                    break;
                            }
                        }
                        else
                        {
                            if (WeaponMaxRange(actor2) >= dist)
                            {
                                switch (GetRandomNumber(9))
                                {
                                    case 3:
                                        if (_enemyState[EN_VULTM1, 3] == 0)
                                        {
                                            _enemyState[EN_VULTM1, 3] = 1;
                                            PrepareScenePropScene(42, false, false);
                                        }
                                        break;
                                    case 9: // Original is 10 here which never happens
                                        if (_enemyState[EN_VULTM1, 4] == 0)
                                        {
                                            _enemyState[EN_VULTM1, 4] = 1;
                                            PrepareScenePropScene(43, false, false);
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                switch (GetRandomNumber(14))
                                {
                                    case 7:
                                        if (_enemyState[EN_VULTM1, 9] == 0)
                                        {
                                            _enemyState[EN_VULTM1, 9] = 1;
                                            PrepareScenePropScene(48, false, false);
                                        }
                                        break;
                                    case 11:
                                        if (_enemyState[EN_VULTM1, 1] == 0)
                                        {
                                            _enemyState[EN_VULTM1, 1] = 1;
                                            PrepareScenePropScene(40, false, false);
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
                _enHdlVar[EN_VULTM1, 1]++;
                _enHdlVar[EN_VULTM1, 3]++;
                _enHdlVar[EN_VULTM1, 5]++;
            }

            if (act1x > 310)
                _actor[actor1].cursorX = -320;
            else if (act1x < 10)
                _actor[actor1].cursorX = 320;
            else if (act1x > 280)
                _actor[actor1].cursorX = -160;

            // Shift+V cheat to win the battle
            if (GetKeyState(KeyCode.LeftShift) && GetKeyState(KeyCode.V) && !_beenCheated &&
                !_actor[0].lost && !_actor[1].lost)
            {
                _beenCheated = true;
                _actor[1].damage = _actor[1].maxdamage + 10;
            }

            return retval;
        }

        int Enemy4initializer(int actor1, int actor2, int probability)
        {
            int i;

            for (i = 0; i < 10; i++)
                _enemyState[EN_VULTM1, i] = 0;

            for (i = 0; i < 9; i++)
                _enHdlVar[EN_VULTM1, i] = 0;

            _beenCheated = false;

            return 1;
        }

        int Enemy5handler(int actor1, int actor2, int probability)
        {
            int retval = 0;
            int act1damage = _actor[actor1].damage; // ebx
            int act1x = _actor[actor1].x; // esi
            int act2x = _actor[actor2].x; // ebp

            int dist = Math.Abs(act1x - act2x);

            if (WeaponMaxRange(actor1) >= dist)
            {
                if (_enHdlVar[EN_VULTF2, 2] == 0)
                    _enHdlVar[EN_VULTF2, 3]++;
                _enHdlVar[EN_VULTF2, 1] = 1;
            }
            else
            {
                _enHdlVar[EN_VULTF2, 1] = 0;
            }

            if (!_actor[actor1].defunct)
            {
                if (_enHdlVar[EN_VULTF2, 3] >= 2 || act1damage != 0)
                {
                    _actor[actor1].damage = 10;

                    if (WeaponMaxRange(actor1) <= dist)
                    {
                        if (act2x < act1x)
                            _actor[actor1].cursorX = -101;
                        else
                            _actor[actor1].cursorX = 101;
                    }
                    else
                    {
                        _actor[actor1].cursorX = 0;
                    }

                    if (WeaponMaxRange(actor1) + 20 >= dist)
                    if (GetRandomNumber(probability - 1) == 1)
                        retval = 1;
                }
                else
                {
                    if (WeaponMaxRange(actor2) >= dist && _actor[actor2].weapon == INV_CHAINSAW)
                    {
                        if (_actor[actor2].kicking || (GetRandomNumber(probability - 1) == 1))
                            retval = 1;
                    }
                    _actor[actor1].cursorX = 0;
                    if (_enHdlVar[EN_VULTF2, 0] >= 100)
                        _enHdlVar[EN_VULTF2, 3] = 3;
                }

                if ((_actor[actor1].field_54 == 0) &&
                    (!_actor[actor2].lost) &&
                    (!_actor[actor1].lost))
                {
                    if (_actor[actor1].act[3].state == 54)
                        switch (GetRandomNumber(9))
                        {
                            case 4:
                                if (_enemyState[EN_VULTF2, 6] == 0)
                                {
                                    _enemyState[EN_VULTF2, 6] = 1;
                                    PrepareScenePropScene(15, false, false);
                                }
                                break;
                            case 8:
                                if (_enemyState[EN_VULTF2, 3] == 0)
                                {
                                    _enemyState[EN_VULTF2, 3] = 1;
                                    PrepareScenePropScene(12, false, false);
                                }
                                break;
                        }
                    else
                    {
                        if (_actor[actor1].kicking)
                        {
                            switch (GetRandomNumber(9))
                            {
                                case 2:
                                    if (_enemyState[EN_VULTF2, 8] == 0)
                                    {
                                        _enemyState[EN_VULTF2, 8] = 1;
                                        PrepareScenePropScene(17, false, false);
                                    }
                                    break;
                                case 5:
                                    PrepareScenePropScene(11, false, false);
                                    _enemyState[EN_VULTF2, 2] = 1;
                                    break;
                                case 9: // Original is 10
                                    _enemyState[EN_VULTF2, 1] = 1;
                                    PrepareScenePropScene(10, false, false);
                                    break;
                            }
                        }
                        else
                        {
                            switch (GetRandomNumber(14))
                            {
                                case 3:
                                    if (_enemyState[EN_VULTF2, 4] == 0)
                                    {
                                        _enemyState[EN_VULTF2, 4] = 1;
                                        PrepareScenePropScene(13, false, false);
                                    }
                                    break;
                                case 11:
                                    if (_enemyState[EN_VULTF2, 5] == 0)
                                    {
                                        _enemyState[EN_VULTF2, 5] = 1;
                                        PrepareScenePropScene(14, false, false);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            if (_actor[actor1].weapon == -1)
                retval = 2;

            if (act1x > 310)
                _actor[actor1].cursorX = -320;
            else if (act1x < 10)
                _actor[actor1].cursorX = 320;
            else if (act1x > 280)
                _actor[actor1].cursorX = -160;
            else if (_actor[actor1].defunct)
                _actor[actor1].cursorX = 0;

            _enHdlVar[EN_VULTF2, 2] = _enHdlVar[EN_VULTF2, 1];
            _enHdlVar[EN_VULTF2, 0]++;

            // Shift+V cheat to win the battle
            if (GetKeyState(KeyCode.LeftShift) && GetKeyState(KeyCode.V) && !_beenCheated &&
                !_actor[0].lost && !_actor[1].lost)
            {
                _beenCheated = true;
                _actor[1].damage = _actor[1].maxdamage + 10;
                _actor[1].act[2].state = 113;
            }

            return retval;
        }

        int Enemy5initializer(int actor1, int actor2, int probability)
        {
            int i;

            for (i = 0; i < 9; i++)
                _enemyState[EN_VULTF2, i] = 0;

            for (i = 0; i < 9; i++)
                _enHdlVar[EN_VULTF2, i] = 0;

            _beenCheated = false;

            return 1;
        }

        int Enemy6handler(int actor1, int actor2, int probability)
        {
            int retval = 0;
            int act1damage = _actor[actor1].damage; // ebx //ebx
            //int act2damage = _actor[actor2].damage; // ebp // edi
            int act1x = _actor[actor1].x; // esi
            int act2x = _actor[actor2].x; // edi

            if (_actor[actor2].weapon == INV_CHAINSAW)
                retval = 1;

            int dist = Math.Abs(act1x - act2x);

            if (_actor[actor1].defunct)
            {
                /*                       scenePropIdx[18] */
                if (_currScenePropIdx == 50 && _currScenePropSubIdx == 3)
                    retval = 1;
            }
            else
            {
                if (act1damage > 0 || _enHdlVar[EN_VULTM2, 0] > 20)
                {
                    _actor[actor1].damage = 10;
                    if (_enHdlVar[EN_VULTM2, 1] == 0 && !_actor[actor1].lost)
                    {
                        if (_actor[actor1].field_54 == 0)
                        {
                            switch (GetRandomNumber(3))
                            {
                                case 0:
                                    if (_enemyState[EN_VULTM2, 1] == 0)
                                    {
                                        _enemyState[EN_VULTM2, 1] = 1;
                                        PrepareScenePropScene(19, false, false);
                                    }
                                    break;
                                case 1:
                                    if (_enemyState[EN_VULTM2, 2] == 0)
                                    {
                                        _enemyState[EN_VULTM2, 2] = 1;
                                        PrepareScenePropScene(20, false, false);
                                    }
                                    break;
                                case 2:
                                    if (_enemyState[EN_VULTM2, 3] == 0)
                                    {
                                        _enemyState[EN_VULTM2, 3] = 1;
                                        PrepareScenePropScene(21, false, false);
                                    }
                                    break;
                                case 3:
                                    if (_enemyState[EN_VULTM2, 4] == 0)
                                    {
                                        _enemyState[EN_VULTM2, 4] = 1;
                                        PrepareScenePropScene(22, false, false);
                                    }
                                    break;
                            }
                            _enHdlVar[EN_VULTM2, 1] = 1;
                            goto _label1;
                        }
                    }
                    else
                    {
                        if (_actor[actor1].field_54 == 0 &&
                            !_actor[actor1].lost)
                        {
                            retval = 1;
                            _enHdlVar[EN_VULTM2, 0] = 0;
                        }
                    }
                }
                else
                {
                    if (WeaponMaxRange(actor2) >= dist)
                    {
                        if (act2x < act1x)
                            _actor[actor1].cursorX = 101;
                        else
                            _actor[actor1].cursorX = -101;
                    }
                    else
                    {
                        _actor[actor1].cursorX = 0;
                    }
                }

                if ((_enHdlVar[EN_VULTM2, 1] == 0) &&
                    (_actor[actor1].field_54 == 0) &&
                    (!_actor[actor2].lost) &&
                    (!_actor[actor1].lost))
                {
                    switch (GetRandomNumber(14))
                    {
                        case 2:
                            if (_enemyState[EN_VULTM2, 5] == 0)
                            {
                                _enemyState[EN_VULTM2, 5] = 1;
                                PrepareScenePropScene(23, false, false);
                            }
                            break;
                        case 7:
                            if (_enemyState[EN_VULTF1, 6] == 0)
                            {
                                _enemyState[EN_VULTF1, 6] = 1;
                                PrepareScenePropScene(24, false, false);
                            }
                            break;
                    }
                }
            }

            _label1:
            if (act1x > 310)
                _actor[actor1].cursorX = -320;
            else if (act1x < 219)
                _actor[actor1].cursorX = 320;
            else if (act1x > 280)
                _actor[actor1].cursorX = -160;
            else
                _actor[actor1].cursorX = 0;

            if (_actor[actor1].weapon == -1)
                retval = 2;

            _enHdlVar[EN_VULTM2, 0]++;

            // Shift+V cheat to win the battle
            if (GetKeyState(KeyCode.LeftShift) && GetKeyState(KeyCode.V) && !_beenCheated &&
                !_actor[0].lost && !_actor[1].lost)
            {
                _beenCheated = true;
                _actor[0].act[2].state = 97;
                smlayer_setActorFacing(0, 2, 20, 180);
                _actor[0].act[2].room = 0;
                _actor[0].act[1].room = 0;
                _actor[0].act[0].room = 0;
                smlayer_setActorLayer(1, 2, 25);
                smlayer_setActorCostume(1, 2, ReadArray(45));
                smlayer_setActorFacing(1, 2, 6, 180);
                _actor[1].act[2].state = 97;
                _actor[1].act[2].room = 1;
                _actor[1].act[1].room = 0;
                _actor[1].act[0].room = 0;
            }

            if (_actor[actor1].lost)
                retval = 0;

            return retval;
        }

        int Enemy6initializer(int actor1, int actor2, int probability)
        {
            int i;

            for (i = 0; i < 7; i++)
                _enemyState[EN_VULTM2, i] = 0;

            for (i = 0; i < 9; i++)
                _enHdlVar[EN_VULTM2, i] = 0;

            _beenCheated = false;

            return 1;
        }

        int Enemy7handler(int actor1, int actor2, int probability)
        {
            int retval = 0;
            int act1damage = _actor[actor1].damage; // ebx
            int act1x = _actor[actor1].x; // ebp, esi
            int act2x = _actor[actor2].x; // edi

            int dist = Math.Abs(act1x - act2x);

            if (_enHdlVar[EN_CAVEFISH, 1] >= 600)
            {
                _enHdlVar[EN_CAVEFISH, 2] = 1;
                _enHdlVar[EN_CAVEFISH, 1] = 0;
            }
            else
            {
                if (_enHdlVar[EN_CAVEFISH, 2] == 0)
                {
                    if (WeaponMaxRange(actor2) + 30 >= dist)
                    {
                        if (act2x < act1x)
                            _actor[actor1].cursorX = 101;
                        else
                            _actor[actor1].cursorX = -101;
                    }
                    else
                    {
                        _actor[actor1].cursorX = 0;
                    }
                    goto _labelA;
                }
            }

            if (WeaponMaxRange(actor1) <= dist)
            {
                if (act2x < act1x)
                    _actor[actor1].cursorX = -101;
                else
                    _actor[actor1].cursorX = 101;
            }
            else
            {
                _actor[actor1].cursorX = 0;
            }

            _labelA:
            if (act1x > 310)
                _actor[actor1].cursorX = -320;
            else if (act1x < 10)
                _actor[actor1].cursorX = 320;

            if (dist <= 95)
                retval = 1;

            if (_actor[actor1].weapon == -1)
                retval = 2;

            _enHdlVar[EN_CAVEFISH, 1]++;
            _enHdlVar[EN_CAVEFISH, 0] = act1damage;

            // Shift+V cheat to win the battle
            if (GetKeyState(KeyCode.LeftShift) && GetKeyState(KeyCode.V) && !_beenCheated &&
                !_actor[0].lost && !_actor[1].lost)
            {
                _beenCheated = true;
                _actor[1].damage = _actor[1].maxdamage + 10;
                _actor[1].act[2].state = 102;
            }

            return retval;
        }

        int Enemy7initializer(int actor1, int actor2, int probability)
        {
            int i;

            for (i = 0; i < 9; i++)
                _enHdlVar[EN_CAVEFISH, i] = 0;

            _beenCheated = false;

            return 1;
        }

        int Enemy8handler(int actor1, int actor2, int probability)
        {
            _actor[actor1].cursorX = 0;
            return 0;
        }

        int Enemy8initializer(int actor1, int actor2, int probability)
        {
            return 1;
        }

        void ChooseEnemyWeaponAnim(int buttons)
        {
            // kick
            if (((buttons & 1) != 0) && (!_actor[0].lost))
            {
                if (!_kickEnemyProgress && Actor0StateFlags2(_actor[1].act[2].state + _actor[1].weapon * 119))
                {
                    switch (_actor[1].weapon)
                    {
                        case INV_CHAIN:
                            _actor[1].act[2].state = 10;
                            break;
                        case INV_CHAINSAW:
                            _actor[1].act[2].state = 14;
                            break;
                        case INV_MACE:
                            _actor[1].act[2].state = 18;
                            break;
                        case INV_2X4:
                            _actor[1].act[2].state = 22;
                            break;
                        case INV_WRENCH:
                            _actor[1].act[2].state = 26;
                            break;
                        case INV_BOOT:
                            _actor[1].act[2].state = 93;
                            break;
                        case INV_HAND:
                            _actor[1].act[2].state = 2;
                            break;
                        case INV_DUST:
                            _actor[1].act[2].state = 89;
                            break;
                        default:
                            break;
                    }
                    _kickEnemyProgress = true;
                }
            }
            else
            {
                _kickEnemyProgress = false;
            }

            // switch weapon
            if (((buttons & 2) != 0) && (_currEnemy != EN_TORQUE))
            {
                if (_weaponEnemyJustSwitched || _actor[1].act[2].state == 35 ||
                    _actor[1].act[2].state == 34)
                    return;

                switch (_actor[1].weapon)
                {
                    case INV_CHAIN:
                    case INV_CHAINSAW:
                    case INV_MACE:
                    case INV_2X4:
                    case INV_WRENCH:
                        _actor[1].act[2].state = 35;
                        smlayer_setActorFacing(1, 2, 24, 180);
                        break;
                    case INV_BOOT:
                    case INV_HAND:
                    case INV_DUST:
                        // fallthrough
                    default:
                        SwitchEnemyWeapon();
                        break;
                }

                _weaponEnemyJustSwitched = true;
            }
            else
            {
                _weaponEnemyJustSwitched = false;
            }
        }

        void SetEnemyAnimation(int actornum, int anim)
        {
            int d = 0;

            if (_currEnemy == EN_VULTM1)
                d = 14;

            if (anim <= 12)
                smlayer_setActorFacing(actornum, 1,
                    ActorAnimationData[_actor[actornum].weaponClass * 7 + anim - 6] + d, 180);
        }

        bool WeaponEnemyIsEffective()
        {
            if ((_actor[1].x - _actor[0].x > WeaponMaxRange(1)) ||
                (_actor[1].x - _actor[0].x < WeaponMinRange(1)) ||
                !_actor[0].kicking)
                return false;

            return true;
        }

        void SetEnemyState()
        {
            if (_actor[1].lost)
                return;

            _actor[1].act[2].animTilt = -1000;

            if (_currEnemy == EN_CAVEFISH)
            {
                _actor[1].weaponClass = 2;
                if (!_roadBumps)
                    _actor[1].act[2].state = 98;
                else
                    _actor[1].act[2].state = 99;

                return;
            }

            switch (_actor[1].weapon)
            {
                case INV_CHAIN:
                    _actor[1].weaponClass = 1;
                    _actor[1].act[2].state = 63;
                    break;
                case INV_CHAINSAW:
                    _actor[1].weaponClass = 1;
                    _actor[1].act[2].state = 64;
                    break;
                case INV_MACE:
                    _actor[1].weaponClass = 1;
                    _actor[1].act[2].state = 65;
                    break;
                case INV_2X4:
                    _actor[1].weaponClass = 1;
                    _actor[1].act[2].state = 66;
                    break;
                case INV_WRENCH:
                    _actor[1].weaponClass = 1;
                    _actor[1].act[2].state = 62;
                    break;
                case INV_BOOT:
                case INV_HAND:
                case INV_DUST:
                    _actor[1].weaponClass = 2;
                    _actor[1].act[2].state = 1;
                    break;
            }
        }

        void SwitchEnemyWeapon()
        {
            do
            {
                _actor[1].weapon++;
                if (_actor[1].weapon > 7)
                    _actor[1].weapon = INV_CHAIN;
            } while (!_actor[1].inventory[_actor[1].weapon]);

            switch (_actor[1].weapon)
            {
                case INV_CHAIN:
                case INV_CHAINSAW:
                case INV_MACE:
                case INV_2X4:
                case INV_WRENCH:
                    smlayer_setActorCostume(1, 2, ReadArray(_enemy[_currEnemy].costume4));
                    smlayer_setActorFacing(1, 2, 18, 180);
                    _actor[1].weaponClass = 1;
                    _actor[1].act[2].state = 34;
                    break;
                case INV_BOOT:
                    _actor[1].weaponClass = 2;
                    _actor[1].act[2].state = 1;
                    break;
                case INV_HAND:
                    smlayer_setActorCostume(1, 2, ReadArray(_enemy[_currEnemy].costume4));
                    _actor[1].weaponClass = 2;
                    _actor[1].act[2].state = 1;
                    break;
                case INV_DUST:
                    SetEnemyState();
                    break;
                default:
                    break;
            }
        }

        int CalcEnemyDamage(bool arg_0, bool arg_4)
        {
            if ((_actor[1].x - _actor[0].x > WeaponMaxRange(0)) ||
                (_actor[1].x - _actor[0].x < WeaponMinRange(0)))
                return 0;

            if (_actor[1].field_44 && arg_4)
                return 1000;

            if (!Actor1StateFlags(_actor[1].act[2].state))
                return 0;

            if (arg_0)
            {
                OuchSoundEnemy();
                _actor[1].damage += WeaponDamage(0);
            }

            return 1;
        }

        void OuchSoundEnemy()
        {
            int tmp;

            _actor[1].act[3].state = 52;

            if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
            {
                smlayer_startVoice(55);
                return;
            }

            switch (_currEnemy)
            {
                case EN_VULTF1:
                    if (_actor[0].weapon == INV_DUST)
                    {
                        smlayer_startVoice(287);
                    }
                    else
                    {
                        if (GetRandomNumber(1)!=0)
                        {
                            smlayer_startVoice(279);
                        }
                        else
                        {
                            smlayer_startVoice(280);
                        }
                    }
                    break;
                case EN_VULTF2:
                    smlayer_startVoice(271);
                    break;
                case EN_VULTM1:
                    smlayer_startVoice(162);
                    break;
                case EN_ROTT1:
                    tmp = GetRandomNumber(2);

                    if (tmp==0)
                    {
                        smlayer_startVoice(216);
                    }
                    else if (tmp == 1)
                    {
                        smlayer_startVoice(217);
                    }
                    else
                    {
                        smlayer_startVoice(218);
                    }
                    break;
                case EN_ROTT2:
                    tmp = GetRandomNumber(2);

                    if (tmp==0)
                    {
                        smlayer_startVoice(243);
                    }
                    else if (tmp == 1)
                    {
                        smlayer_startVoice(244);
                    }
                    else
                    {
                        smlayer_startVoice(245);
                    }
                    break;
                case EN_VULTM2:
                    smlayer_startVoice(180);
                    break;
                default:
                    smlayer_startVoice(99);
                    break;
            }
        }
    }
}

