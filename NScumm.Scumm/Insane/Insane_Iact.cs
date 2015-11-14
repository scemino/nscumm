//
//  Insane_IACT.cs
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
using System.IO;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Insane
{
    partial class Insane
    {
        public void ProcIACT(byte[] renderBitmap, int codecparam, int setupsan12,
                             int setupsan13, BinaryReader b, int size, int flags,
                             int par1, int par2, int par3, int par4)
        {
            if (_keyboardDisable != 0)
                return;

            switch (_currSceneId)
            {
                case 1:
                    IactScene1(renderBitmap, codecparam, setupsan12, setupsan13, b, size, flags, par1, par2, par3, par4);
                    break;
                case 3:
                case 13:
                    IactScene3(renderBitmap, codecparam, setupsan12, setupsan13, b, size, flags, par1, par2, par3, par4);
                    break;
                case 4:
                case 5:
                    IactScene4(renderBitmap, codecparam, setupsan12, setupsan13, b, size, flags, par1, par2, par3, par4);
                    break;
                case 6:
                    IactScene6(renderBitmap, codecparam, setupsan12, setupsan13, b, size, flags, par1, par2, par3, par4);
                    break;
                case 17:
                    IactScene17(renderBitmap, codecparam, setupsan12, setupsan13, b, size, flags, par1, par2, par3, par4);
                    break;
                case 21:
                    IactScene21(renderBitmap, codecparam, setupsan12, setupsan13, b, size, flags, par1, par2, par3, par4);
                    break;
//                default:
//                    Debug.Fail(string.Format("Iasct scene {0} not supported", _currSceneId));
//                    break;
            }
        }

        void IactScene1(byte[] renderBitmap, int codecparam, int setupsan12,
                        int setupsan13, BinaryReader b, int size, int flags,
                        int par1, int par2, int par3, int par4)
        {
            int par5, par6, par7, par9, par11, par13;

            switch (par1)
            {
                case 2: // PATCH
                    if (par3 != 1)
                        break;

                    par5 = b.ReadUInt16(); // si
                    if (_actor[0].field_8 == 112)
                    {
                        SetBit(par5);
                        break;
                    }

                    if (_approachAnim == -1)
                    {
                        ChooseEnemy(); //PATCH
                        _approachAnim = _enemy[_currEnemy].apprAnim;
                    }

                    if (_approachAnim == par4)
                        ClearBit(par5);
                    else
                        SetBit(par5);
                    break;
                case 3:
                    if (par3 == 1)
                    {
                        SetBit(b.ReadUInt16());
                        _approachAnim = -1;
                    }
                    break;
                case 4:
                    if (par3 == 1 && (_approachAnim < 0 || _approachAnim > 4))
                        SetBit(b.ReadUInt16());
                    break;
                case 5:
                    if (par2 != 13)
                        break;

                    b.ReadUInt16();           // +8
                    b.ReadUInt16();           // +10
                    par7 = b.ReadUInt16();    // +12 dx
                    b.ReadUInt16();           // +14
                    par9 = b.ReadUInt16();    // +16 bx
                    b.ReadUInt16();           // +18
                    par11 = b.ReadUInt16();   // +20 cx
                    b.ReadUInt16();           // +22
                    par13 = b.ReadUInt16();   // +24 ax

                    if (par13 > _actor[0].x || par11 < _actor[0].x)
                    {
                        _tiresRustle = true;
                        _actor[0].x1 = -_actor[0].x1;
                        _actor[0].damage++; // PATCH
                    }

                    if (par9 < _actor[0].x || par7 > _actor[0].x)
                    {
                        _tiresRustle = true;
                        _actor[0].damage += 4; // PATCH
                    }
                    break;
                case 6:
                    switch (par2)
                    {
                        case 38:
                            smlayer_drawSomething(renderBitmap, codecparam, 50 - 19, 20 - 13, 3,
                                _smush_iconsNut, 7, 0, 0);
                            _roadBranch = true;
                            _iactSceneId = par4;
                            break;
                        case 25:
                            _roadBumps = true;
                            _actor[0].y1 = -_actor[0].y1;
                            break;
                        case 11:
                            if (_approachAnim >= 1 && _approachAnim <= 4 && !_needSceneSwitch)
                                QueueSceneSwitch(13, _smush_minefiteFlu, "minefite.san", 64, 0,
                                    _continueFrame1, 1300);
                            break;
                        case 9:
                            par5 = b.ReadUInt16(); // si
                            par6 = b.ReadUInt16(); // bx
                            smlayer_setFluPalette(_smush_roadrsh3Rip, 0);
                            if (par5 == par6 - 1)
                                smlayer_setFluPalette(_smush_roadrashRip, 0);
                            break;
                    }
                    break;
                case 7:
                    switch (par4)
                    {
                        case 1:
                            _actor[0].x -= (b.ReadUInt16() - 160) / 10;
                            break;
                        case 2:
                            par5 = b.ReadUInt16();

                            if (par5 - 8 > _actor[0].x || par5 + 8 < _actor[0].x)
                            {
                                if (smlayer_isSoundRunning(86))
                                    smlayer_stopSound(86);
                            }
                            else
                            {
                                if (!smlayer_isSoundRunning(86))
                                    smlayer_startSfx(86);
                            }
                            break;
                    }
                    break;
            }

            if (_approachAnim < 0 || _approachAnim > 4)
            if (ReadArray(8) != 0)
            {
                smlayer_drawSomething(renderBitmap, codecparam, 270 - 19, 20 - 18, 3,
                    _smush_iconsNut, 20, 0, 0);
                _benHasGoggles = true;
            }
        }

        void IactScene3(byte[] renderBitmap, int codecparam, int setupsan12,
                        int setupsan13, BinaryReader b, int size, int flags,
                        int command, int par1, int tmp1, int tmp2)
        {
            if (command == 6)
            {
                if (par1 == 9)
                {
                    var par2 = b.ReadUInt16(); // ptr + 8
                    var par3 = b.ReadUInt16(); // ptr + 10

                    if (par2 == 0)
                        smlayer_setFluPalette(_smush_roadrsh3Rip, 0);
                    else
                    {
                        if (par2 == par3 - 1)
                            smlayer_setFluPalette(_smush_roadrashRip, 0);
                    }
                }
                else if (par1 == 25)
                {
                    _roadBumps = true;
                    _actor[0].y1 = -_actor[0].y1;
                    _actor[1].y1 = -_actor[1].y1;
                }
            }
        }

        void IactScene4(byte[] renderBitmap, int codecparam, int setupsan12,
                        int setupsan13, BinaryReader b, int size, int flags,
                        int par1, int par2, int par3, int par4)
        {
            int par5;

            switch (par1)
            {
                case 2:
                case 4:
                    par5 = b.ReadUInt16(); // si
                    switch (par3)
                    {
                        case 1:
                            if (par4 == 1)
                            {
                                if (ReadArray(6) != 0)
                                    SetBit(par5);
                                else
                                    ClearBit(par5);
                            }
                            else
                            {
                                if (ReadArray(6) != 0)
                                    ClearBit(par5);
                                else
                                    SetBit(par5);
                            }
                            break;
                        case 2:
                            if (ReadArray(5) != 0)
                                ClearBit(par5);
                            else
                                SetBit(par5);
                            break;
                    }
                    break;
                case 6:
                    switch (par2)
                    {
                        case 38:

                            smlayer_drawSomething(renderBitmap, codecparam, 270 - 19, 20 - 13, 3,
                                _smush_icons2Nut, 10, 0, 0);
                            _roadBranch = true;
                            _iactSceneId = par4;
                            break;
                        case 7:
                            if (ReadArray(4) != 0)
                                return;

                            smlayer_drawSomething(renderBitmap, codecparam, 160 - 13, 20 - 10, 3, // QW
                                _smush_icons2Nut, 8, 0, 0);
                            _roadStop = true;
                            break;
                        case 8:
                            if (ReadArray(4) == 0 || ReadArray(6) == 0)
                                return;

                            WriteArray(1, _posBrokenTruck);
                            WriteArray(3, _val57d);
                            smush_setToFinish();

                            break;
                        case 25:
                            if (ReadArray(5) == 0)
                                return;

                            _carIsBroken = true;
                            smlayer_drawSomething(renderBitmap, codecparam, 160 - 13, 20 - 10, 3, // QW
                                _smush_icons2Nut, 8, 0, 0);
                            break;
                        case 11:
                            smlayer_drawSomething(renderBitmap, codecparam, 50 - 19, 20 - 13, 3,
                                _smush_icons2Nut, 9, 0, 0);
                            _roadBranch = true;
                            _iactSceneId = par4;
                            break;
                    }
                    break;
            }
        }

        void IactScene6(byte[] renderBitmap, int codecparam, int setupsan12,
            int setupsan13, BinaryReader b, int size, int flags,
            int par1, int par2, int par3, int par4) {
            int par5;

            switch (par1) {
                case 7:
                    par5 = b.ReadUInt16();
                    if (par4 != 3)
                        break;

                    if (par5 >= _actor[0].x)
                        break;

                    _actor[0].x = par5;
                    break;
                case 2:
                case 4:
                    par5 = b.ReadUInt16();
                    switch (par3) {
                        case 1:
                            if (par4 == 1) {
                                if (ReadArray(6)!=0)
                                    SetBit(par5);
                                else
                                    ClearBit(par5);
                            } else {
                                if (ReadArray(6)!=0)
                                    ClearBit(par5);
                                else
                                    SetBit(par5);
                            }
                            break;
                        case 2:
                            if (ReadArray(5)!=0)
                                ClearBit(par5);
                            else
                                SetBit(par5);
                            break;
                    }
                    break;
                case 6:
                    switch (par2) {
                        case 38:
                            smlayer_drawSomething(renderBitmap, codecparam, 270-19, 20-13, 3,
                                _smush_icons2Nut, 10, 0, 0);
                            _roadBranch = true;
                            _iactSceneId = par4;
                            break;
                        case 7:
                            if (ReadArray(4) != 0)
                                return;

                            _roadStop = true;
                            smlayer_drawSomething(renderBitmap, codecparam, 160-13, 20-10, 3, //QW
                                _smush_icons2Nut, 8, 0, 0);
                            break;
                        case 8:
                            if (ReadArray(4) == 0 || ReadArray(6) == 0)
                                return;

                            WriteArray(1, _posBrokenTruck);
                            WriteArray(3, _posVista);
                            smush_setToFinish();

                            break;
                        case 25:
                            if (ReadArray(5) == 0)
                                return;

                            _carIsBroken = true;
                            smlayer_drawSomething(renderBitmap, codecparam, 160-13, 20-10, 3, //QW
                                _smush_icons2Nut, 8, 0, 0);
                            break;
                        case 11:
                            smlayer_drawSomething(renderBitmap, codecparam, 50-19, 20-13, 3,
                                _smush_icons2Nut, 9, 0, 0);
                            _roadBranch = true;
                            _iactSceneId = par4;
                            break;
                    }
                    break;
            }
        }

        void IactScene17(byte[] renderBitmap, int codecparam, int setupsan12,
            int setupsan13, BinaryReader b, int size, int flags,
            int par1, int par2, int par3, int par4) {
            switch (par1) {
                case 2:
                case 3:
                case 4:
                    if (par3 == 1) {
                        SetBit(b.ReadUInt16());
                        _approachAnim = -1;
                    }
                    break;
                case 6:
                    switch (par2) {
                        case 38:
                            smlayer_drawSomething(renderBitmap, codecparam, 28, 48, 1,
                                _smush_iconsNut, 6, 0, 0);
                            _roadBranch = true;
                            _iactSceneId = par4;
                            if (_counter1 <= 4) {
                                if (_counter1 == 4)
                                    smlayer_startSfx(94);

                                smlayer_showStatusMsg(-1, renderBitmap, codecparam, 24, 167, 1,
                                    2, 0, "{0}", HandleTrsTag(5000));
                            }
                            _objectDetected = true;
                            break;
                        case 10:
                            smlayer_drawSomething(renderBitmap, codecparam, 28, 48, 1,
                                _smush_iconsNut, 6, 0, 0);
                            if (_counter1 <= 4) {
                                if (_counter1 == 4)
                                    smlayer_startSfx(94);

                                smlayer_showStatusMsg(-1, renderBitmap, codecparam, 24, 167, 1,
                                    2, 0, "{0}", HandleTrsTag(5001));
                            }
                            _objectDetected = true;
                            _mineCaveIsNear = true;
                            break;
                    }
                    break;
            }
        }

        void IactScene21(byte[] renderBitmap, int codecparam, int setupsan12,
            int setupsan13, BinaryReader b, int size, int flags,
            int par1, int par2, int par3, int par4) {
            // void implementation
        }

        void ChooseEnemy()
        {
            if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
            {
                _currEnemy = EN_ROTT1;
                return;
            }

            if (ReadArray(58) != 0)
                _enemy[EN_TORQUE].isEmpty = 1;

            if (_enemy[EN_TORQUE].occurences == 0)
            {
                _currEnemy = EN_TORQUE;
                _metEnemiesListTail++;
                _metEnemiesList[_metEnemiesListTail] = EN_TORQUE;
                return;
            }

            RemoveEmptyEnemies();

            int count, i, j, en, en2;
            bool notfound;

            en = 0;
            for (i = 0; i < 9; i++)
                if (_enemy[i].isEmpty == 0)
                    ++en;

            en -= 4;
            Debug.Assert(en >= 0);

            count = 0;
            while (true)
            {
                count++;
                if (count < 14)
                {
                    en2 = GetRandomNumber(10);
                    if (en2 == 9)
                        en2 = 6;
                    else if (en2 > 9)
                        en2 = 7;

                    notfound = true;

                    if (_enemy[en2].isEmpty != 0)
                        continue;

                    if (0 < _metEnemiesListTail)
                    {
                        i = 0;
                        do
                        {
                            if (en2 == _metEnemiesList[i + 1])
                                notfound = false;
                            i++;
                        } while (i < _metEnemiesListTail && notfound);
                    }
                    if (!notfound)
                    {
                        continue;
                    }
                }
                else
                {
                    j = 0;
                    do
                    {
                        notfound = true;
                        en2 = j;
                        if (0 < _metEnemiesListTail)
                        {
                            i = 0;
                            do
                            {
                                if (en2 == _metEnemiesList[i + 1])
                                    notfound = false;
                                i++;
                            } while (i < _metEnemiesListTail && notfound);
                        }
                        j++;
                    } while (j < 9 && !notfound);
                    if (!notfound)
                    {
                        _metEnemiesListTail = 0;
                        count = 0;
                        continue;
                    }
                }

                ++_metEnemiesListTail;
                Debug.Assert(_metEnemiesListTail < _metEnemiesList.Length);
                _metEnemiesList[_metEnemiesListTail] = en2;

                if (_metEnemiesListTail >= en)
                {
                    RemoveEnemyFromMetList(0);
                }

                if (notfound)
                    break;
            }

            _currEnemy = en2;
        }

        void RemoveEmptyEnemies()
        {
            if (_metEnemiesListTail > 0)
            {
                for (int i = 0; i < _metEnemiesListTail; i++)
                    if (_enemy[i].isEmpty == 1)
                        RemoveEnemyFromMetList(i);
            }
        }

        void RemoveEnemyFromMetList(int enemy1)
        {
            if (enemy1 >= _metEnemiesListTail)
                return;

            int en = enemy1;
            do
            {
                ++en;
                Debug.Assert(en + 1 < _metEnemiesList.Length);
                _metEnemiesList[en] = _metEnemiesList[en + 1];
            } while (en < _metEnemiesListTail);
            _metEnemiesListTail--;
        }
    }
}

