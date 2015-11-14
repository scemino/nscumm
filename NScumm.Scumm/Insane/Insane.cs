//
//  Insane.cs
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
using System.IO;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;
using NScumm.Scumm.IO;
using NScumm.Scumm.Smush;

namespace NScumm.Scumm.Insane
{
    partial class Insane
    {
        public Insane(ScummEngine7 scumm)
        {
            _vm = scumm;

            InitVars();

            if (!(_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm.Game.Platform == Platform.Dos)*/))
            {
                _smush_roadrashRip = ReadFileToMem("roadrash.rip");
                _smush_roadrsh2Rip = ReadFileToMem("roadrsh2.rip");
                _smush_roadrsh3Rip = ReadFileToMem("roadrsh3.rip");
                _smush_goglpaltRip = ReadFileToMem("goglpalt.rip");
                _smush_tovista1Flu = ReadFileToMem("tovista1.flu");
                _smush_tovista2Flu = ReadFileToMem("tovista2.flu");
                _smush_toranchFlu = ReadFileToMem("toranch.flu");
                _smush_minedrivFlu = ReadFileToMem("minedriv.flu");
                _smush_minefiteFlu = ReadFileToMem("minefite.flu");

                _smush_bensgoggNut = new NutRenderer(_vm, "bensgogg.nut");
                _smush_bencutNut = new NutRenderer(_vm, "bencut.nut");
            }
            _smush_iconsNut = new NutRenderer(_vm, "icons.nut");
            _smush_icons2Nut = new NutRenderer(_vm, "icons2.nut");
        }

        public void EscapeKeyHandler()
        {
            if (!_insaneIsRunning)
            {
                smush_setToFinish();
                return;
            }

            if (_needSceneSwitch || _keyboardDisable != 0)
                return;

            Debug.WriteLine("scene: {0}", _currSceneId);
            switch (_currSceneId)
            {
                case 1:
                    if (_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm.Game.Platform == Platform.Dos)*/)
                    {
                        QueueSceneSwitch(1, null, "minedriv.san", 64, 0, 0, 0);
                    }
                    else
                    {
                        QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0, _continueFrame1, 1300);
                        WriteArray(9, 0);
                    }
                    break;
                case 18:
                    QueueSceneSwitch(17, _smush_minedrivFlu, "minedriv.san", 64, 0, _continueFrame1, 1300);
                    WriteArray(9, 1);
                    break;
                case 2:
                    {
                        var flu = _fluConf[14 + _iactSceneId2];
                        if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
                            QueueSceneSwitch(4, null, "tovista.san", 64, 0, 0, 0);
                        else
                            QueueSceneSwitch(flu.sceneId, flu.fluPtr, flu.filename, 64, 0, flu.startFrame, flu.numFrames);
                    }
                    break;
                case 3:
                    QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0, _continueFrame, 1300);
                    break;
                case 4:
                    if (_needSceneSwitch)
                        return;

                    if (ReadArray(6) != 0)
                    {
                        if (ReadArray(4) != 0)
                        {
                            QueueSceneSwitch(14, null, "hitdust2.san", 64, 0, 0, 0);
                        }
                        else
                        {
                            QueueSceneSwitch(14, null, "hitdust4.san", 64, 0, 0, 0);
                        }
                    }
                    else
                    {
                        if (ReadArray(4) != 0)
                        {
                            QueueSceneSwitch(14, null, "hitdust1.san", 64, 0, 0, 0);
                        }
                        else
                        {
                            QueueSceneSwitch(14, null, "hitdust3.san", 64, 0, 0, 0);
                        }
                    }
                    break;
                case 5:
                    if (ReadArray(4) != 0)
                    {
                        if (_needSceneSwitch)
                            return;
                        QueueSceneSwitch(15, null, "vistthru.san", 64, 0, 0, 0);
                    }
                    else
                    {
                        WriteArray(1, _posVista);
                        smush_setToFinish();
                    }
                    break;
                case 6:
                    if (ReadArray(4) != 0)
                    {
                        if (_needSceneSwitch)
                            return;
                        QueueSceneSwitch(15, null, "chasthru.san", 64, 0, 0, 0);
                    }
                    else
                    {
                        if (ReadArray(5) != 0)
                        {
                            WriteArray(1, _val57d);
                            smush_setToFinish();
                        }
                        else
                        {
                            WriteArray(4, 1);
                            QueueSceneSwitch(15, null, "chasout.san", 64, 0, 0, 0);
                        }
                    }
                    break;
                case 8:
                    {
                        var flu = _fluConf[7 + _iactSceneId2];
                        if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
                            QueueSceneSwitch(1, null, "minedriv.san", 64, 0, 0, 0);
                        else
                            QueueSceneSwitch(flu.sceneId, flu.fluPtr, flu.filename, 64, 0,
                                flu.startFrame, flu.numFrames);
                    }
                    break;
                case 7:
                    {
                        var flu = _fluConf[0 + _iactSceneId2];
                        if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
                            QueueSceneSwitch(1, null, "minedriv.san", 64, 0, 0, 0);
                        else
                            QueueSceneSwitch(flu.sceneId, flu.fluPtr, flu.filename, 64, 0,
                                flu.startFrame, flu.numFrames);
                    }
                    break;
                case 23:
                    _actor[0].damage = 0;
                    QueueSceneSwitch(21, null, "rottfite.san", 64, 0, 0, 0);
                    break;
                case 9:
                    _actor[0].damage = 0;
                    QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0, _continueFrame, 1300);
                    break;
                case 10:
                    _actor[0].damage = 0;
                    QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0, _continueFrame1, 1300);
                    break;
                case 13:
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
                        QueueSceneSwitch(1, null, "minedriv.san", 64, 0, 0, 0);
                    else
                        QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0, _continueFrame, 1300);
                    break;
                case 24:
                    QueueSceneSwitch(21, null, "rottfite.san", 64, 0, 0, 0);
                    break;
                case 16:
                    WriteArray(4, 0);
                    WriteArray(5, 1);
                    WriteArray(1, _posBrokenCar);
                    WriteArray(3, _posBrokenTruck);
                    smush_setToFinish();
                    break;
                case 15:
                    switch (_tempSceneId)
                    {
                        case 5:
                            QueueSceneSwitch(6, null, "toranch.san", 64, 0, 0, 530);
                            break;
                        case 6:
                            QueueSceneSwitch(4, null, "tovista1.san", 64, 0, 0, 230);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        public void ProcSKIP(int subSize, BinaryReader b)
        {
            short par1, par2;
            _player._skipNext = false;

            if (_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm.Game.Platform == Common::kPlatformDOS)*/)
            {
                Debug.Assert(subSize >= 2);
                par1 = b.ReadInt16();
                par2 = 0;
            }
            else
            {
                Debug.Assert(subSize >= 4);
                par1 = b.ReadInt16();
                par2 = b.ReadInt16();
            }

            if (par2 == 0)
            {
                if (IsBitSet(par1))
                    _player._skipNext = true;
            }
            else if (IsBitSet(par1) != IsBitSet(par2))
            {
                _player._skipNext = true;
            }
        }

        bool IsBitSet(int n)
        {
            Debug.Assert(n < 0x80);
            return (_iactBits[n] != 0);
        }

        void InitVars()
        {
            _speed = 12;
            _currSceneId = 1;
            _currEnemy = -1;
            _battleScene = true;
            _approachAnim = -1;

            if (_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
            {
                init_enemyStruct(EN_ROTT1, EN_ROTT1, 0, 0, 60, 0, INV_MACE, 63, "endcrshr.san",
                    25, 15, 16, 26, 13, 3);
            }
            else
            {
                init_enemyStruct(EN_ROTT1, EN_ROTT1, 0, 0, 160, 0, INV_MACE, 90, "wr2_rott.san",
                    26, 16, 17, 27, 11, 3);
            }

            init_enemyStruct(EN_ROTT2, EN_ROTT2, 1, 0, 250, 0, INV_2X4, 90, "wr2_rott.san",
                28, 16, 17, 42, 11, 3);
            init_enemyStruct(EN_ROTT3, EN_ROTT3, 2, 0, 120, 0, INV_HAND, 90, "wr2_rott.san",
                15, 16, 17, 43, 11, 3);
            init_enemyStruct(EN_VULTF1, EN_VULTF1, 3, 0, 60, 0, INV_HAND, 91, "wr2_vltp.san",
                29, 33, 32, 37, 12, 4);
            init_enemyStruct(EN_VULTM1, EN_VULTM1, 4, 0, 100, 0, INV_CHAIN, 91, "wr2_vltc.san",
                30, 33, 32, 36, 12, 4);
            init_enemyStruct(EN_VULTF2, EN_VULTF2, 5, 0, 250, 0, INV_CHAINSAW, 91, "wr2_vlts.san",
                31, 33, 32, 35, 12, 4);
            init_enemyStruct(EN_VULTM2, EN_VULTM2, 6, 0, 900, 0, INV_BOOT, 91, "wr2_rott.san",
                34, 33, 32, 45, 16, 4);
            init_enemyStruct(EN_CAVEFISH, EN_CAVEFISH, 7, 0, 60, 0, INV_DUST, 92, "wr2_cave.san",
                39, 0, 0, 41, 13, 2);
            init_enemyStruct(EN_TORQUE, EN_TORQUE, 8, 0, 900, 0, INV_HAND, 93, "wr2_vltp.san",
                57, 0, 0, 37, 12, 1);

            init_fluConfStruct(1, 1, _smush_minedrivFlu, "minedriv.san", 235, 1300);
            init_fluConfStruct(2, 1, _smush_minedrivFlu, "minedriv.san", 355, 1300);
            init_fluConfStruct(3, 1, _smush_minedrivFlu, "minedriv.san", 1255, 1300);
            init_fluConfStruct(4, 1, _smush_minedrivFlu, "minedriv.san", 565, 1300);
            init_fluConfStruct(5, 1, _smush_minedrivFlu, "minedriv.san", 1040, 1300);
            init_fluConfStruct(8, 1, _smush_minedrivFlu, "minedriv.san", 1040, 1300);
            init_fluConfStruct(9, 1, _smush_minedrivFlu, "minedriv.san", 655, 1300);
            init_fluConfStruct(10, 1, _smush_minedrivFlu, "minedriv.san", 115, 1300);
            init_fluConfStruct(11, 1, _smush_minedrivFlu, "minedriv.san", 315, 1300);
            init_fluConfStruct(12, 1, _smush_minedrivFlu, "minedriv.san", 235, 1300);
            init_fluConfStruct(15, 6, _smush_toranchFlu, "toranch.san", 115, 530);
            init_fluConfStruct(16, 5, _smush_tovista2Flu, "tovista2.san", 235, 290);
            init_fluConfStruct(17, 4, _smush_tovista1Flu, "tovista1.san", 175, 230);
            init_fluConfStruct(18, 4, _smush_tovista1Flu, "tovista1.san", 175, 230);
            init_fluConfStruct(19, 6, _smush_toranchFlu, "toranch.san", 115, 530);
            init_fluConfStruct(20, 6, _smush_toranchFlu, "toranch.san", 115, 530);

            init_scenePropStruct(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
            init_scenePropStruct(1, 0, 1, 128, 2001, 0, 0, 0, 0, 56, 2);
            init_scenePropStruct(2, 0, 0, 125, 1002, 0, 0, 0, 0, 35, 3);
            init_scenePropStruct(3, 0, 1, 129, 2002, 0, 0, 0, 0, 23, 4);
            init_scenePropStruct(4, 0, 1, 130, 2003, 0, 0, 0, 0, 40, 5);
            init_scenePropStruct(5, 0, 0, 126, 1005, 0, 0, 0, 0, 46, 6);
            init_scenePropStruct(6, 0, 1, 131, 2004, 0, 0, 0, 0, 39, 7);
            init_scenePropStruct(7, 0, 1, 132, 2005, 0, 0, 0, 0, 45, 8);
            init_scenePropStruct(8, 0, 1, 133, 2006, 0, 0, 0, 0, 14, 9);
            init_scenePropStruct(9, 0, 0, 127, 1009, 0, 0, 0, 0, 15, 10);
            init_scenePropStruct(10, 0, 1, 134, 501, 0, 0, 0, 0, 25, 11);
            init_scenePropStruct(11, 0, 1, 135, 502, 0, 0, 0, 0, 15, 0);
            init_scenePropStruct(12, 1, -1, 0, 0, 0xFF, 0xFF, 0xFF, 0, 0, 1);
            init_scenePropStruct(13, 1, 0, 291, 135, 0xFF, 0xFF, 0xFF, 0, 25, 0);
            init_scenePropStruct(14, 2, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
            init_scenePropStruct(15, 2, 1, 277, 17, 0xFC, 0, 0xFC, 0, 56, 2);
            init_scenePropStruct(16, 2, 0, 288, 18, 0xFF, 0xFF, 0xFF, 0, 56, 3);
            init_scenePropStruct(17, 2, 1, 278, 19, 0xFC, 0, 0xFC, 0, 56, 0);
            init_scenePropStruct(18, 3, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
            init_scenePropStruct(19, 3, 1, 282, 23, 0xFC, 0, 0xFC, 0, 56, 0);
            init_scenePropStruct(20, 4, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
            init_scenePropStruct(21, 4, 1, 283, 24, 0xFC, 0, 0xFC, 0, 56, 0);
            init_scenePropStruct(22, 5, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
            init_scenePropStruct(23, 5, 1, 284, 25, 0xFC, 0, 0xFC, 0, 56, 0);
            init_scenePropStruct(24, 6, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
            init_scenePropStruct(25, 6, 1, 285, 26, 0xFC, 0, 0xFC, 0, 56, 0);
            init_scenePropStruct(26, 7, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
            init_scenePropStruct(27, 7, 1, 286, 27, 0xFC, 0, 0xFC, 0, 56, 0);
            init_scenePropStruct(28, 8, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
            init_scenePropStruct(29, 8, 1, 287, 28, 0xFC, 0, 0xFC, 0, 56, 0);
            init_scenePropStruct(30, 9, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
            init_scenePropStruct(31, 9, 1, 261, 1, 0xFC, 0, 0, 0, 40, 2);
            init_scenePropStruct(32, 9, 1, 262, 2, 0xFC, 0, 0, 0, 40, 3);
            init_scenePropStruct(33, 9, 1, 263, 3, 0xFC, 0, 0, 0, 40, 0);
            init_scenePropStruct(34, 10, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
            init_scenePropStruct(35, 10, 1, 263, 3, 0xFC, 0, 0, 0, 30, 0);
            init_scenePropStruct(36, 11, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
            init_scenePropStruct(37, 11, 1, 264, 4, 0xFC, 0, 0, 0, 30, 0);
            init_scenePropStruct(38, 12, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
            init_scenePropStruct(39, 12, 1, 265, 5, 0xFC, 0, 0, 0, 30, 0);
            init_scenePropStruct(40, 13, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
            init_scenePropStruct(41, 13, 1, 266, 6, 0xFC, 0, 0, 0, 30, 0);
            init_scenePropStruct(42, 14, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
            init_scenePropStruct(43, 14, 1, 267, 7, 0xFC, 0, 0, 0, 30, 0);
            init_scenePropStruct(44, 15, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
            init_scenePropStruct(45, 15, 1, 268, 8, 0xFC, 0, 0, 0, 30, 0);
            init_scenePropStruct(46, 16, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
            init_scenePropStruct(47, 16, 1, 274, 14, 0xFC, 0, 0, 0, 30, 0);
            init_scenePropStruct(48, 17, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
            init_scenePropStruct(49, 17, 1, 270, 10, 0xFC, 0, 0, 0, 30, 0);
            init_scenePropStruct(50, 18, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
            init_scenePropStruct(51, 18, 0, 289, 45, 0xFF, 0xFF, 0xFF, 0, 40, 2);
            init_scenePropStruct(52, 18, 1, 177, 49, 0xFC, 0xFC, 0x54, 0, 40, 3);
            init_scenePropStruct(53, 18, 1, 178, 50, 0xFC, 0xFC, 0x54, 0, 40, 4);
            init_scenePropStruct(54, 18, 0, 290, 47, 0xFF, 0xFF, 0xFF, 0, 40, 0);
            init_scenePropStruct(55, 19, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
            init_scenePropStruct(56, 19, 1, 179, 51, 0xFC, 0xFC, 0x54, 0, 40, 0);
            init_scenePropStruct(57, 20, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
            init_scenePropStruct(58, 20, 1, 183, 55, 0xFC, 0xFC, 0x54, 0, 40, 0);
            init_scenePropStruct(59, 21, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
            init_scenePropStruct(60, 21, 1, 184, 56, 0xFC, 0xFC, 0x54, 0, 40, 0);
            init_scenePropStruct(61, 22, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
            init_scenePropStruct(62, 22, 1, 186, 58, 0xFC, 0xFC, 0x54, 0, 40, 0);
            init_scenePropStruct(63, 23, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
            init_scenePropStruct(64, 23, 1, 191, 63, 0xFC, 0xFC, 0x54, 0, 40, 0);
            init_scenePropStruct(65, 24, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
            init_scenePropStruct(66, 24, 1, 192, 64, 0xFC, 0xFC, 0x54, 0, 40, 0);
            init_scenePropStruct(67, 25, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
            init_scenePropStruct(68, 25, 1, 220, 93, 0xBC, 0x78, 0x48, 0, 40, 2);
            init_scenePropStruct(69, 25, 1, 221, 94, 0xBC, 0x78, 0x48, 0, 40, 3);
            init_scenePropStruct(70, 25, 1, 222, 95, 0xBC, 0x78, 0x48, 0, 40, 0);
            init_scenePropStruct(71, 26, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
            init_scenePropStruct(72, 26, 1, 223, 96, 0xBC, 0x78, 0x48, 0, 40, 0);
            init_scenePropStruct(73, 27, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
            init_scenePropStruct(74, 27, 1, 224, 97, 0xBC, 0x78, 0x48, 0, 40, 0);
            init_scenePropStruct(75, 28, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
            init_scenePropStruct(76, 28, 1, 225, 98, 0xBC, 0x78, 0x48, 0, 40, 0);
            init_scenePropStruct(77, 29, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
            init_scenePropStruct(78, 29, 1, 226, 99, 0xBC, 0x78, 0x48, 0, 40, 0);
            init_scenePropStruct(79, 30, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
            init_scenePropStruct(80, 30, 1, 228, 101, 0xBC, 0x78, 0x48, 0, 40, 0);
            init_scenePropStruct(81, 31, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
            init_scenePropStruct(82, 31, 1, 229, 102, 0xBC, 0x78, 0x48, 0, 40, 0);
            init_scenePropStruct(83, 32, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
            init_scenePropStruct(84, 32, 1, 233, 106, 0xA8, 0xA8, 0xA8, 0, 40, 2);
            init_scenePropStruct(85, 32, 1, 234, 107, 0xA8, 0xA8, 0xA8, 0, 40, 0);
            init_scenePropStruct(86, 33, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
            init_scenePropStruct(87, 33, 1, 241, 114, 0xA8, 0xA8, 0xA8, 0, 40, 2);
            init_scenePropStruct(88, 33, 1, 242, 115, 0xA8, 0xA8, 0xA8, 0, 40, 0);
            init_scenePropStruct(89, 34, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
            init_scenePropStruct(90, 34, 1, 237, 110, 0xA8, 0xA8, 0xA8, 0, 40, 2);
            init_scenePropStruct(91, 34, 1, 238, 111, 0xA8, 0xA8, 0xA8, 0, 40, 3);
            init_scenePropStruct(92, 34, 1, 239, 112, 0xA8, 0xA8, 0xA8, 0, 40, 0);
            init_scenePropStruct(93, 35, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
            init_scenePropStruct(94, 35, 1, 258, 131, 0xA8, 0xA8, 0xA8, 0, 40, 0);
            init_scenePropStruct(95, 36, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
            init_scenePropStruct(96, 36, 1, 260, 133, 0xA8, 0xA8, 0xA8, 0, 40, 0);
            init_scenePropStruct(97, 37, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
            init_scenePropStruct(98, 37, 1, 252, 125, 0xA8, 0xA8, 0xA8, 0, 40, 0);
            init_scenePropStruct(99, 38, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
            init_scenePropStruct(100, 38, 1, 254, 127, 0xA8, 0xA8, 0xA8, 0, 40, 0);
            init_scenePropStruct(101, 39, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
            init_scenePropStruct(102, 39, 1, 236, 109, 0xA8, 0xA8, 0xA8, 0, 40, 0);
            init_scenePropStruct(103, 40, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
            init_scenePropStruct(104, 40, 1, 174, 42, 4, 0xBC, 0, 0, 40, 0);
            init_scenePropStruct(105, 41, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
            init_scenePropStruct(106, 41, 1, 167, 36, 4, 0xBC, 0, 0, 40, 0);
            init_scenePropStruct(107, 42, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
            init_scenePropStruct(108, 42, 1, 160, 29, 4, 0xBC, 0, 0, 40, 0);
            init_scenePropStruct(109, 43, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
            init_scenePropStruct(110, 43, 1, 161, 30, 4, 0xBC, 0, 0, 40, 0);
            init_scenePropStruct(111, 44, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
            init_scenePropStruct(112, 44, 1, 163, 32, 4, 0xBC, 0, 0, 40, 0);
            init_scenePropStruct(113, 45, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
            init_scenePropStruct(114, 45, 1, 164, 33, 4, 0xBC, 0, 0, 40, 0);
            init_scenePropStruct(115, 46, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
            init_scenePropStruct(116, 46, 1, 170, 39, 4, 0xBC, 0, 0, 40, 0);
            init_scenePropStruct(117, 47, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
            init_scenePropStruct(118, 47, 1, 166, 35, 4, 0xBC, 0, 0, 40, 0);
            init_scenePropStruct(119, 48, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
            init_scenePropStruct(120, 48, 1, 175, 43, 4, 0xBC, 0, 0, 40, 0);
            init_scenePropStruct(121, 49, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
            init_scenePropStruct(122, 49, 1, 203, 75, 0x40, 0x40, 0xFC, 0, 40, 0);
            init_scenePropStruct(123, 50, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
            init_scenePropStruct(124, 50, 1, 194, 66, 0x40, 0x40, 0xFC, 0, 40, 0);
            init_scenePropStruct(125, 51, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
            init_scenePropStruct(126, 51, 1, 195, 67, 0x40, 0x40, 0xFC, 0, 40, 0);
            init_scenePropStruct(127, 52, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
            init_scenePropStruct(128, 52, 1, 199, 71, 0x40, 0x40, 0xFC, 0, 40, 0);
            init_scenePropStruct(129, 53, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
            init_scenePropStruct(130, 53, 1, 205, 77, 0x40, 0x40, 0xFC, 0, 40, 0);
            init_scenePropStruct(131, 54, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
            init_scenePropStruct(132, 54, 1, 212, 85, 0x40, 0x40, 0xFC, 0, 40, 0);
            init_scenePropStruct(133, 55, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
            init_scenePropStruct(134, 55, 1, 201, 73, 0x40, 0x40, 0xFC, 0, 40, 0);
            init_scenePropStruct(135, 56, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
            init_scenePropStruct(136, 56, 1, 198, 70, 0x40, 0x40, 0xFC, 0, 40, 0);
            init_scenePropStruct(137, 57, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
            init_scenePropStruct(138, 57, 0, 59, 134, 0xFF, 0xFF, 0xFF, 0, 30, 0);

            for (int i = 0; i < _actor.Length; i++)
            {
                _actor[i] = new Actor();
            }
            _actor[0].damage = 0;
            if (_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm.Game.Platform == Common::kPlatformDOS)*/)
                _actor[0].maxdamage = 60;
            else
                _actor[0].maxdamage = 80;
            _actor[0].field_8 = 1;
            _actor[0].frame = 0;
            _actor[0].tilt = 0;
            _actor[0].cursorX = 0;
            _actor[0].speed = 0;
            _actor[0].x = 160;
            _actor[0].y = 0;
            _actor[0].y1 = -1;
            _actor[0].x1 = -1;
            _actor[0].weaponClass = 2;
            _actor[0].animWeaponClass = 0;
            _actor[0].newFacingFlag = 2;
            _actor[0].curFacingFlag = 0;
            _actor[0].lost = false;
            _actor[0].kicking = false;
            _actor[0].field_44 = false;
            _actor[0].field_48 = false;
            _actor[0].defunct = false;
            _actor[0].scenePropSubIdx = 0;
            _actor[0].field_54 = 0;
            _actor[0].runningSound = 0;
            _actor[0].weapon = INV_HAND;
            _actor[0].inventory[INV_CHAIN] = false;
            _actor[0].inventory[INV_CHAINSAW] = false;
            _actor[0].inventory[INV_MACE] = false;
            _actor[0].inventory[INV_2X4] = false;
            _actor[0].inventory[INV_WRENCH] = true;
            _actor[0].inventory[INV_BOOT] = true;
            _actor[0].inventory[INV_HAND] = true;
            _actor[0].inventory[INV_DUST] = false;
            _actor[0].probability = 5;
            _actor[0].enemyHandler = EN_BEN;
            init_actStruct(0, 0, 11, 1, 1, 0, 0, 0);
            init_actStruct(0, 1, 12, 1, 1, 0, 0, 0);
            init_actStruct(0, 2, 1, 1, 1, 0, 0, 0);
            init_actStruct(0, 3, 1, 1, 1, 0, 0, 0);

            _actor[1].damage = 0;
            _actor[1].maxdamage = -1;
            _actor[1].field_8 = 1;
            _actor[1].frame = 0;
            _actor[1].tilt = 0;
            _actor[1].cursorX = 0;
            _actor[1].speed = 0;
            _actor[1].x = 160;
            _actor[1].y = 0;
            _actor[1].y1 = -1;
            _actor[1].x1 = -1;
            _actor[1].weaponClass = 2;
            _actor[1].animWeaponClass = 0;
            _actor[1].newFacingFlag = 0;
            _actor[1].curFacingFlag = 0;
            _actor[1].lost = false;
            _actor[1].kicking = false;
            _actor[1].field_44 = false;
            _actor[1].field_48 = false;
            _actor[1].defunct = false;
            _actor[1].scenePropSubIdx = 0;
            _actor[1].field_54 = 0;
            _actor[1].runningSound = 0;
            _actor[1].weapon = INV_HAND;
            _actor[1].inventory[INV_CHAIN] = false;
            _actor[1].inventory[INV_CHAINSAW] = false;
            _actor[1].inventory[INV_MACE] = true;
            _actor[1].inventory[INV_2X4] = false;
            _actor[1].inventory[INV_WRENCH] = false;
            _actor[1].inventory[INV_BOOT] = false;
            _actor[1].inventory[INV_HAND] = false;
            _actor[1].inventory[INV_DUST] = false;
            _actor[1].probability = 5;
            _actor[1].enemyHandler = -1;

            init_actStruct(1, 0, 14, 1, 1, 0, 0, 0);
            init_actStruct(1, 1, 15, 1, 1, 0, 0, 0);
            init_actStruct(1, 2, 13, 1, 1, 0, 0, 0);
            init_actStruct(1, 3, 13, 1, 1, 0, 0, 0);
        }

        byte[] ReadFileToMem(string name)
        {
            return ServiceLocator.FileStorage.ReadAllBytes(ScummHelper.LocatePath(ServiceLocator.FileStorage.GetDirectoryName(_vm.Game.Path), name));
        }

        void SwitchSceneIfNeeded()
        {
            if (_needSceneSwitch && !_smush_isSanFileSetup)
            {
                PutActors();
                StopSceneSounds(_currSceneId);
                _tempSceneId = _currSceneId;
                _currSceneId = _temp2SceneId;
                _needSceneSwitch = false;
                LoadSceneData(_temp2SceneId, 0, 1);

                if (LoadSceneData(_temp2SceneId, 0, 2))
                {
                    SetSceneCostumes(_temp2SceneId);
                    _sceneData2Loaded = 0;
                    _sceneData1Loaded = 0;
                    return;
                }

                _sceneData2Loaded = 1;
                if (_temp2SceneId == 13 || _temp2SceneId == 3)
                    _isBenCut = true;
            }

            if (_sceneData2Loaded != 0 && _sceneData1Loaded == 0)
            {
                SetSceneCostumes(_currSceneId);
                _sceneData2Loaded = 0;
            }
        }

        void PutActors()
        {
            smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1, (byte)_smlayer_room);
            smlayer_putActor(0, 0, _actor[0].x, _actor[0].y1, (byte)_smlayer_room);
            smlayer_putActor(0, 1, _actor[0].x, _actor[0].y1, (byte)_smlayer_room);
            smlayer_putActor(1, 2, _actor[0].x, _actor[0].y1, (byte)_smlayer_room);
            smlayer_putActor(1, 0, _actor[0].x, _actor[0].y1, (byte)_smlayer_room);
            smlayer_putActor(1, 1, _actor[0].x, _actor[0].y1, (byte)_smlayer_room);
        }

        void QueueSceneSwitch(int sceneId, byte[] fluPtr, string filename,
                              int arg_C, int arg_10, int startFrame, int numFrames)
        {
            int tmp;

            Debug.WriteLine("QueueSceneSwitch({0}, *, {1}, {2}, {3}, {4}, {5})", sceneId, filename, arg_C, arg_10,
                startFrame, numFrames);
            if (_needSceneSwitch || _sceneData1Loaded != 0)
                return;

            if (fluPtr != null)
            {
                tmp = ((int)startFrame / 30 + 1) * 30;
                if (tmp >= numFrames)
                    tmp = 0;

                smush_setupSanWithFlu(filename, arg_C | 32, -1, -1, 0, fluPtr, tmp);
            }
            else
            {
                smush_setupSanFromStart(filename, arg_C | 32, -1, -1, 0);
            }
            _needSceneSwitch = true;
            _temp2SceneId = (byte)sceneId;
        }

        int ReadArray(int item)
        {
            return _vm.ReadArray(_numberArray, 0, item);
        }

        void WriteArray(int item, int value)
        {
            _vm.WriteArray(_numberArray, 0, item, value);
        }

        void ReadState()
        { // PATCH

            if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm.Game.platform == Common::kPlatformDOS)*/)
            {
                _actor[0].inventory[INV_CHAIN] = false;
                _actor[0].inventory[INV_CHAINSAW] = false;
                _actor[0].inventory[INV_MACE] = false;
                _actor[0].inventory[INV_2X4] = false;
                _actor[0].inventory[INV_WRENCH] = true;
                _actor[0].inventory[INV_DUST] = false;
                _actor[0].inventory[INV_HAND] = true;
                _actor[0].inventory[INV_BOOT] = false;
                _smlayer_room2 = 13;
            }
            else
            {
                _actor[0].inventory[INV_CHAIN] = ReadArray(50) != 0;
                _actor[0].inventory[INV_CHAINSAW] = ReadArray(51) != 0;
                _actor[0].inventory[INV_MACE] = ReadArray(52) != 0;
                _actor[0].inventory[INV_2X4] = ReadArray(53) != 0;
                _actor[0].inventory[INV_WRENCH] = ReadArray(54) != 0;
                _actor[0].inventory[INV_DUST] = ReadArray(55) != 0;
                _actor[0].inventory[INV_HAND] = true;
                _actor[0].inventory[INV_BOOT] = true;
                _smlayer_room = ReadArray(320);
                _smlayer_room2 = ReadArray(321);
                _posBrokenTruck = (short)ReadArray(322);
                _posVista = (short)ReadArray(323);
                _val57d = ReadArray(324);
                _posCave = (short)ReadArray(325);
                _posBrokenCar = (short)ReadArray(326);
                _val54d = ReadArray(327);
                _posFatherTorque = (short)ReadArray(328);
                _enemy[EN_TORQUE].occurences = (short)ReadArray(337);
                _enemy[EN_ROTT1].occurences = (short)ReadArray(329);
                _enemy[EN_ROTT2].occurences = (short)ReadArray(330);
                _enemy[EN_ROTT3].occurences = (short)ReadArray(331);
                _enemy[EN_VULTF1].occurences = (short)ReadArray(332);
                _enemy[EN_VULTM1].occurences = (short)ReadArray(333);
                _enemy[EN_VULTF2].occurences = (short)ReadArray(334);
                _enemy[EN_VULTM2].occurences = (short)ReadArray(335);
                _enemy[EN_CAVEFISH].occurences = (short)ReadArray(336);
                _enemy[EN_VULTM2].isEmpty = ReadArray(340);
                _enemy[EN_VULTF2].isEmpty = ReadArray(339);
                _enemy[EN_CAVEFISH].isEmpty = ReadArray(56);

                // FIXME
                // Some sanity checks. There were submitted savefiles where these values were wrong
                // Still it is unknown what leads to this state. Most probably it is memory
                // overwrite
                if (_enemy[EN_VULTM2].isEmpty != ReadArray(7))
                {
                    //                    Console.Error.WriteLine("Wrong INSANE parameters for EN_VULTM2 ({0} {1})",
                    //                        _enemy[EN_VULTM2].isEmpty, ReadArray(7));
                    _enemy[EN_VULTM2].isEmpty = ReadArray(7);
                }

                if ((_enemy[EN_VULTF2].isEmpty != 0) != (_actor[0].inventory[INV_CHAINSAW]))
                {
                    //                    Console.Error.WriteLine("Wrong INSANE parameters for EN_VULTF2 ({0} {1})",
                    //                        _enemy[EN_VULTF2].isEmpty, _actor[0].inventory[INV_CHAINSAW]);
                    _enemy[EN_VULTF2].isEmpty = (_actor[0].inventory[INV_CHAINSAW]) ? 1 : 0;
                }

                // FIXME
                // This used to be here but.
                //  - bootparam 551 gives googles without cavefish met
                //  - when you get the ramp, googles disappear, but you already won the cavefish
                // Incorrect situation would be
                //  you won cavefish, don't have googles, don't have ramp
                //
                // So if you find out what how to check ramp presense, feel free to add check here
                // (beware of FT ver a and ver b. In version b var311 is inserted and all vars >311
                // are shifted),
                //
                //if (_enemy[EN_CAVEFISH].isEmpty != ReadArray(8))
                //  error("Wrong INSANE parameters for EN_CAVEFISH (%d %d). Please, report this",
                //        _enemy[EN_CAVEFISH].isEmpty, ReadArray(8));
            }
        }

        void StartVideo(string filename, int num, int argC, int frameRate, int doMainLoop, byte[] fluPtr = null, int startFrame = 0)
        {
            int offset = 0;
            _smush_curFrame = 0;
            _smush_isSanFileSetup = false;
            _smush_setupsan4 = 0;
            _smush_smushState = 0;
            _smush_setupsan1 = 0;
            _smush_setupsan17 = 0;

            if (fluPtr != null)
            {
                offset = smush_setupSanWithFlu(filename, 0, -1, -1, 0, fluPtr, startFrame);
            }
            else
            {
                smush_setupSanFromStart(filename, 0, -1, -1, 0);
            }

            _player.Play(filename, _speed, offset, startFrame);
        }

        void SetupValues()
        {
            _actor[0].x = 160;
            _actor[0].y = 200;
            _actor[0].tilt = 0;
            _actor[0].field_8 = 1;
            _actor[0].frame = 0;
            _actor[0].act[2].state = 1;
            _actor[0].act[0].state = 1;
            _actor[0].act[1].state = 0;
            _actor[0].act[2].room = 1;
            _actor[0].act[1].room = 0;
            _actor[0].act[0].room = 0;
            _actor[0].cursorX = 0;
            _actor[0].lost = false;
            _currEnemy = -1;
            _approachAnim = -1;
            smush_warpMouse(160, 100, -1);
        }

        void SetEnemyCostumes()
        {
            int i;

            Debug.WriteLine("setEnemyCostumes(%d)", _currEnemy);

            if ((_vm.Game.Features.HasFlag(GameFeatures.Demo))/* && (_vm->_game.platform == Common::kPlatformDOS)*/)
            {
                smlayer_setActorCostume(0, 2, ReadArray(11));
                smlayer_setActorCostume(0, 0, ReadArray(13));
                smlayer_setActorCostume(0, 1, ReadArray(12));
            }
            else
            {
                smlayer_setActorCostume(0, 2, ReadArray(12));
                smlayer_setActorCostume(0, 0, ReadArray(14));
                smlayer_setActorCostume(0, 1, ReadArray(13));
            }
            smlayer_setActorLayer(0, 1, 1);
            smlayer_setActorLayer(0, 2, 5);
            smlayer_setActorLayer(0, 0, 10);
            smlayer_putActor(0, 2, _actor[0].x + 11, _actor[0].y1 + 102, (byte)_smlayer_room2);
            smlayer_putActor(0, 1, _actor[0].x, _actor[0].y1 + 200, (byte)_smlayer_room2);
            smlayer_putActor(0, 0, _actor[0].x, _actor[0].y1 + 200, (byte)_smlayer_room2);

            if (_currEnemy == EN_CAVEFISH)
            {
                smlayer_setActorCostume(1, 2, ReadArray(_enemy[_currEnemy].costume4));
                _actor[1].act[2].room = 1;
                _actor[1].act[1].room = 0;
                _actor[1].act[0].room = 0;
                _actor[1].act[2].animTilt = 1;
                _actor[1].field_8 = 98;
                _actor[1].act[2].state = 98;
                _actor[1].act[0].state = 98;
                _actor[1].act[1].state = 98;

                smlayer_putActor(1, 2, _actor[1].x + _actor[1].act[2].tilt - 17,
                    _actor[1].y + _actor[1].y1 - 98, (byte)_smlayer_room2);
            }
            else if (_currEnemy == EN_TORQUE)
            {
                smlayer_setActorCostume(1, 2, ReadArray(_enemy[_currEnemy].costume4));
                _actor[1].act[2].room = 1;
                _actor[1].act[1].room = 0;
                _actor[1].act[0].room = 0;
                _actor[1].field_8 = 1;
                _actor[1].act[2].state = 1;
                _actor[1].act[0].state = 1;
                _actor[1].act[1].state = 1;
                smlayer_putActor(1, 2, _actor[1].x + _actor[1].act[2].tilt - 17,
                    _actor[1].y + _actor[1].y1 - 98, (byte)_smlayer_room2);
            }
            else
            {
                _actor[1].act[2].room = 1;
                _actor[1].act[1].room = 1;
                _actor[1].act[0].room = 1;

                if (_enemy[_currEnemy].costume4 != 0)
                    smlayer_setActorCostume(1, 2, ReadArray(_enemy[_currEnemy].costume4));

                if (_enemy[_currEnemy].costume5 != 0)
                    smlayer_setActorCostume(1, 0, ReadArray(_enemy[_currEnemy].costume5));

                if (_enemy[_currEnemy].costume6 != 0)
                    smlayer_setActorCostume(1, 1, ReadArray(_enemy[_currEnemy].costume6));

                _actor[1].field_8 = 1;
                _actor[1].act[2].state = 1;
                _actor[1].act[0].state = 1;
                _actor[1].act[1].state = 1;

                if (_actor[1].act[2].room != 0)
                    smlayer_putActor(1, 2, _actor[1].x + _actor[1].act[2].tilt - 17,
                        _actor[1].y + _actor[1].y1 - 98,
                        (byte)_smlayer_room2);
            }

            if (_actor[1].act[1].room != 0)
                smlayer_putActor(1, 1, _actor[1].x, _actor[1].y + _actor[1].y1,
                    (byte)_smlayer_room2);

            if (_actor[1].act[0].room != 0)
                smlayer_putActor(1, 0, _actor[1].x, _actor[1].y + _actor[1].y1,
                    (byte)_smlayer_room2);

            smlayer_setActorLayer(1, 1, 1);
            smlayer_setActorLayer(1, 2, 5);
            smlayer_setActorLayer(1, 0, 10);

            _actor[1].damage = 0;
            _actor[1].x = 250;
            _actor[1].y = 300;
            _actor[1].cursorX = 0;
            _actor[1].tilt = 0;
            _actor[1].weapon = -1;
            _actor[1].weaponClass = 2;
            _enemy[_currEnemy].occurences++;
            _actor[1].maxdamage = _enemy[_currEnemy].maxdamage;
            _actor[1].enemyHandler = _enemy[_currEnemy].handler;
            _actor[1].animWeaponClass = 0;
            for (i = 0; i < 8; i++)
                _actor[1].inventory[i] = false;
            _actor[0].damage = 0;
            _actor[0].x = 100;
            _actor[0].y = 200;
            _actor[0].weapon = INV_HAND;
            _actor[0].weaponClass = 2;
            _actor[0].animWeaponClass = 0;
            _actor[0].newFacingFlag = 2;
            _actor[0].curFacingFlag = 0;
            _actor[0].tilt = 0;
            _actor[0].field_8 = 1;
            _actor[0].act[2].state = 1;
            _actor[0].act[2].animTilt = 1;
            _actor[0].act[0].state = 0;
            _actor[0].act[1].state = 1;
            _actor[0].act[2].room = 1;
            _actor[0].act[1].room = 1;
            _actor[0].act[0].room = 1;
            _actor[0].cursorX = 0;
            _actor[0].defunct = false;
            _actor[0].scenePropSubIdx = 0;
            _actor[0].field_54 = 0;
            _actor[0].runningSound = 0;
            _actor[0].lost = false;
            _actor[0].kicking = false;
            _actor[0].field_44 = false;
            _actor[1].inventory[_enemy[_currEnemy].weapon] = true;
            _actor[0].field_48 = false;
            _actor[1].defunct = false;
            _actor[1].scenePropSubIdx = 0;
            _actor[1].field_54 = 0;
            _actor[1].runningSound = 0;
            _actor[1].lost = false;
            _actor[1].kicking = false;
            _actor[1].field_44 = false;
            _actor[1].field_48 = false;
            if (_enemy[_currEnemy].initializer != -1)
                EnemyInitializer(_enemy[_currEnemy].initializer, _actor[1].damage,
                    _actor[0].damage, _actor[1].probability);

            smush_warpMouse(160, 100, -1);
        }

        static readonly int[] scenePropIdx =
            {
                0,  12,  14,  18,  20,  22,  24,  26,  28,  30,  34,
                36,  38,  40,  42,  44,  46,  48,  50,  55,  57,  59,  61,  63,  65,  67,  71,
                73,  75,  77,  79,  81,  83,  85,  89,  93,  95,  97,  99, 101, 103, 105, 107,
                109, 111, 113, 115, 117, 119, 121, 123, 125, 127, 129, 131, 133, 135, 137
            };

        void PrepareScenePropScene(int scenePropNum, bool arg_4, bool arg_8)
        {
            int tmp, idx = scenePropIdx[scenePropNum];

            Debug.WriteLine("Insane::prepareScenePropScene({0}, {1}, {2})", scenePropNum, arg_4, arg_8);

            if (((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/) || !LoadScenePropSounds(idx))
                return;

            _actor[0].defunct = arg_4;
            _actor[1].defunct = arg_8;
            _currScenePropIdx = idx;
            _sceneProp[idx + 1].counter = 0;
            _currScenePropSubIdx = 1;
            if (_sceneProp[idx + 1].trsId != 0)
                _currTrsMsg = HandleTrsTag(_sceneProp[idx + 1].trsId);
            else
                _currTrsMsg = null;

            tmp = _sceneProp[idx + 1].actor;
            if (tmp != -1)
            {
                _actor[tmp].field_54 = 1;
                _actor[tmp].act[3].state = 117;
                _actor[tmp].scenePropSubIdx = _currScenePropSubIdx;
            }
        }

        bool LoadScenePropSounds(int scenePropNum)
        {
            int num = 0;
            int res = 1;

            if (_sceneProp[scenePropNum + num].index != 1)
            {
                while (num < 12)
                {
                    res &= smlayer_loadSound(_sceneProp[scenePropNum + num].sound, 0, 2);
                    num = _sceneProp[scenePropNum + num].index;

                    if (num == 0)
                        break;
                }
            }

            return res != 0;
        }

        void init_actStruct(int actornum, int actnum, int actorval, byte state, int room, int animTilt, int tilt, int frame)
        {
            _actor[actornum].act[actnum].actor = actorval;
            _actor[actornum].act[actnum].state = state;
            _actor[actornum].act[actnum].room = room;
            _actor[actornum].act[actnum].animTilt = animTilt;
            _actor[actornum].act[actnum].tilt = tilt;
            _actor[actornum].act[actnum].frame = frame;
        }

        void init_enemyStruct(int n, int handler, int initializer,
                              short occurences, int maxdamage, int isEmpty,
                              int weapon, int sound, string filename,
                              int costume4, int costume6, int costume5,
                              short costumevar, int maxframe, int apprAnim)
        {
            Debug.Assert(filename.Length < 20);

            _enemy[n] = new Enemy();
            _enemy[n].handler = handler;
            _enemy[n].initializer = initializer;
            _enemy[n].occurences = occurences;
            _enemy[n].maxdamage = maxdamage;
            _enemy[n].isEmpty = isEmpty;
            _enemy[n].weapon = weapon;
            _enemy[n].sound = sound;
            _enemy[n].filename = filename;
            _enemy[n].costume4 = costume4;
            _enemy[n].costume6 = costume6;
            _enemy[n].costume5 = costume5;
            _enemy[n].costumevar = costumevar;
            _enemy[n].maxframe = maxframe;
            _enemy[n].apprAnim = apprAnim;
        }

        void init_fluConfStruct(int n, int sceneId, byte[] fluPtr,
                                string filename, int startFrame, int numFrames)
        {
            _fluConf[n] = new FluConf();
            _fluConf[n].sceneId = sceneId;
            _fluConf[n].fluPtr = fluPtr;
            _fluConf[n].filename = filename;
            _fluConf[n].startFrame = startFrame;
            _fluConf[n].numFrames = numFrames;
        }

        void init_scenePropStruct(int n, int n1, int actornum, int sound, int trsId,
                                  byte r, byte g, byte b, int counter, int maxCounter,
                                  int index)
        {
            _sceneProp[n] = new SceneProp();
            _sceneProp[n].actor = actornum; // main actor number, -1 if not applicable
            _sceneProp[n].sound = sound;
            _sceneProp[n].trsId = trsId;
            _sceneProp[n].r = r;
            _sceneProp[n].g = g;
            _sceneProp[n].b = b;
            _sceneProp[n].counter = counter;
            _sceneProp[n].maxCounter = maxCounter;
            _sceneProp[n].index = index;
        }

        int smush_setupSanWithFlu(string filename, int setupsan2, int step1, int step2, int setupsan1, byte[] fluPtr, int numFrames)
        {
            var tmp = 0;
            int offset;

            Debug.WriteLine("smush_setupSanWithFlu({0}, {1}, {2}, {3}, {4}, {5}, {6})", filename, setupsan2,
                step1, step2, setupsan1, fluPtr, numFrames);

            _smush_setupsan1 = setupsan1;

            /* skip FLUP marker */
            if (System.Text.Encoding.UTF8.GetString(fluPtr, 0, 4) == "FLUP")
                tmp += 8;

            _smush_setupsan2 = (short)setupsan2;

            if (fluPtr[tmp + 2] <= 1)
            {
                /* 0x300 -- palette, 0x8 -- header */
                offset = (int)BitConverter.ToUInt32(fluPtr, tmp + 0x308 + numFrames * 4);
                smush_setupSanFile(filename, offset, numFrames);
                Array.Copy(fluPtr, tmp + 2, _smush_earlyFluContents, 0, 0x306);
                _smush_earlyFluContents[0x30e] = 0;
                _smush_earlyFluContents[0x30f] = 0;
                _smush_earlyFluContents[0x310] = 0;
                _smush_earlyFluContents[0x311] = 0;
                _smush_earlyFluContents[0x306] = 0;
                _smush_earlyFluContents[0x307] = 0;
            }
            else
            {
                offset = (int)BitConverter.ToUInt32(fluPtr, tmp + 0x31c + numFrames * 4);
                smush_setupSanFile(filename, offset, numFrames);
                Array.Copy(fluPtr, tmp + 2, _smush_earlyFluContents, 0, 0x31a);
            }
            _smush_isSanFileSetup = true;
            _smush_setupsan4 = 1;
            _smush_curFrame = (short)numFrames;
            smush_setFrameSteps(step1, step2);
            smush_warpMouse(160, 100, -1);

            return offset;
        }

        void smush_setFrameSteps(int step1, int step2)
        {
            _smush_frameNum2 = _smush_curFrame;
            _smush_frameNum1 = (short)step2;
            _smush_frameStep = (short)step1;
        }

        void smush_warpMouse(int x, int y, int buttons)
        {
            // TODO: vs
            //            _player.WarpMouse(x, y, buttons);
        }

        void smush_setupSanFromStart(string filename, int setupsan2, int step1, int step2, int setupsan1)
        {
            Debug.WriteLine("Insane::smush_setupFromStart({0})", filename);
            _smush_setupsan1 = setupsan1;
            _smush_setupsan2 = (short)setupsan2;
            smush_setupSanFile(filename, 0, 0);
            _smush_isSanFileSetup = true;
            smush_setFrameSteps(step1, step2);
            smush_warpMouse(160, 100, -1);
        }

        void smush_setupSanFile(string filename, int offset, int contFrame)
        {
            Debug.WriteLine("Insane::smush_setupSanFile({0}, {1:X}, {2})", filename, offset, contFrame);

            _player.SeekSan(filename, offset, contFrame);
        }

        int smush_changeState(int state)
        {
            if (state == 2)
            {
                if (_smush_smushState == 0)
                    _smush_smushState = 1;
                else
                    _smush_smushState = 0;
            }
            else if (state == 4)
            {
                if (_smush_smushState != 0)
                    _smush_smushState = 3;
            }
            else if (state != 5)
            {
                _smush_smushState = state;
            }
            return _smush_smushState;
        }

        void smush_setToFinish()
        {
            Debug.WriteLine("Video is set to finish");
            _vm.SmushVideoShouldFinish = true;
        }

        void smush_rewindCurrentSan(int arg_0, int arg_4, int arg_8)
        {
            Debug.WriteLine("smush_rewindCurrentSan({0}, {1}, {2})", arg_0, arg_4, arg_8);
            _smush_setupsan2 = (short)arg_0;

            smush_setupSanFile(null, 0, 0);
            _smush_isSanFileSetup = true;
            smush_setFrameSteps(arg_4, arg_8);

            _smush_curFrame = 0; // HACK
        }

        void smlayer_setActorCostume(int actornum, int actnum, int costume)
        {
            var a = _vm.Actors[_actor[actornum].act[actnum].actor];
            a.SetActorCostume((ushort)costume);
            a.SetDirection(180);
            a.StartAnimActor(1);
            _actor[actornum].act[actnum].frame = 0;
        }

        void smlayer_setActorLayer(int actornum, int actnum, int layer)
        {
            var a = _vm.Actors[_actor[actornum].act[actnum].actor];
            a.Layer = layer;
        }

        int smlayer_loadSound(int id, int flag, int phase)
        {
            int resid;

            if (phase == 1)
            {
                if (_idx2Exceeded != 0)
                    if (_objArray2Idx >= _objArray2Idx2)
                        return 0;
            }
            resid = ReadArray(id);

            if (resid == 0 && phase == 2)
                return 0;

            if (phase == 2)
                _vm.ResourceManager.LoadSound(_vm.Sound.MusicType, resid);

            _vm.ResourceManager.SetSoundCounter(resid, 1);

            if (phase == 1)
            {
                _objArray2Idx2++;
                _objArray2[_objArray2Idx2] = id;
                if (_objArray2Idx2 >= 100)
                {
                    _idx2Exceeded = 1;
                    _objArray2Idx2 = 0;
                }
            }
            return resid;
        }

        int smlayer_loadCostume(int id, int phase)
        {
            var resid = ReadArray(id);

            if (resid == 0)
                return 0;

            _vm.ResourceManager.LoadCostume(resid);
            _vm.ResourceManager.SetCostumeCounter(resid, 1);

            if (phase == 1)
            {
                _objArray1Idx2++;
                _objArray1[_objArray1Idx2] = id;
                if (_objArray1Idx2 == 100)
                    _objArray1Idx2 = 0;
            }

            return resid;
        }

        void smlayer_putActor(int actornum, int actnum, int x, int y, byte room)
        {
            var a = _vm.Actors[_actor[actornum].act[actnum].actor];
            a.PutActor(new Point((short)x, (short)y), room);
        }

        void smlayer_overrideDrawActorAt(byte[] arg_0, byte arg_4, byte arg_8)
        {
            // FIXME: doublecheck

            // noop in current implementation
        }

        void smlayer_drawSomething(byte[] renderBitmap, int codecparam, int x, int y, int arg_10, NutRenderer nutfile,
                                   int c, int arg_1C, int arg_20)
        {
            nutfile.DrawFrame(renderBitmap, c, x, y);
        }

        void smlayer_showStatusMsg(int arg_0, byte[] renderBitmap, int codecparam,
                                   int pos_x, int pos_y, int arg_14, int arg_18,
                                   int flags, string formatString, string strng)
        {
            var sf = _player.GetFont(0);
            int color = 1;
            int top = 0;

            var str = string.Format(formatString, strng);

            while (str[0] == '^')
            {
                switch (str[1])
                {
                    case 'f':
                        {
                            int id = str[3] - '0';
                            str = str.Substring(4);
                            sf = _player.GetFont(id);
                        }
                        break;
                    case 'c':
                        {
                            color = str[4] - '0' + 10 * (str[3] - '0');
                            str = str.Substring(5);
                        }
                        break;
                    default:
                        throw new InvalidOperationException("invalid escape code in text string");
                }
            }

            sf.Color = (byte)color;

            // flags:
            // bit 0 - center       1
            // bit 1 - not used     2
            // bit 2 - ???          4
            // bit 3 - wrap around  8
            switch (flags)
            {
                case 0:
                    sf.DrawString(str, renderBitmap, _player.Width, _player.Height, pos_x, pos_y, false);
                    break;
                case 1:
                    sf.DrawString(str, renderBitmap, _player.Width, _player.Height, pos_x, Math.Max(pos_y, top), true);
                    break;
                case 5:
                    sf.DrawStringWrap(str, renderBitmap, _player.Width, _player.Height, pos_x, pos_y, 10, 300, true);
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Insane::smlayer_showStatusMsg. Not handled flags: {0}", flags));
            }
        }

        void smlayer_setFluPalette(byte[] pal, int shut_flag)
        {
            if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                return;

            //    if (shut_flag)
            //      // FIXME: shut colors and make picture appear smoothly
            //      SmushPlayer::setPalette(pal);
            //    else
            _player.SetPalette(pal);
        }

        void smlayer_setActorFacing(int actornum, int actnum, int frame, int direction)
        {
            if (_actor[actornum].act[actnum].room != 0)
            {
                var a = _vm.Actors[_actor[actornum].act[actnum].actor];
                a.SetDirection(direction);
                a.StartAnimActor(frame);
                _actor[actornum].act[actnum].frame = 0;
            }
        }

        bool smlayer_actorNeedRedraw(int actornum, int actnum)
        {
            var a = _vm.Actors[_actor[actornum].act[actnum].actor];
            return a.NeedRedraw;
        }

        bool smlayer_isSoundRunning(int sound)
        {
            return _vm.IMuseDigital.GetSoundStatus(ReadArray(sound)) != 0;
        }

        bool smlayer_startSfx(int sound)
        {
            if (smlayer_loadSound(sound, 0, 2) != 0)
            {
                _vm.IMuseDigital.StartSfx(ReadArray(sound), 40);
                return true;
            }
            else
                return false;
        }

        void smlayer_soundSetPriority(int soundId, int priority)
        {
            _vm.IMuseDigital.SetPriority(soundId, priority);
        }

        void smlayer_stopSound(int idx)
        {
            _vm.IMuseDigital.StopSound(ReadArray(idx));
        }

        void smlayer_soundSetPan(int soundId, int pan)
        {
            _vm.IMuseDigital.SetPan(soundId, pan);
        }

        bool smlayer_startVoice(int sound)
        {
            if (smlayer_loadSound(sound, 0, 2) != 0)
            {
                _vm.IMuseDigital.StartSfx(ReadArray(sound), 126);
                return true;
            }
            else
                return false;
        }


        string HandleTrsTag(int trsId)
        {
            Debug.WriteLine("Insane::handleTrsTag({0})", trsId);
            return _player.GetString(trsId);
        }

        int CalcTilt(int speed)
        {
            if (speed + 3 > 6)
                return 0;

            return tilt[speed + 3];
        }

        int WeaponMaxRange(int actornum)
        {

            if (_actor[actornum].weapon == -1)
                return 104;

            return MaxRangeMap[_actor[actornum].weapon];
        }

        int WeaponMinRange(int actornum)
        {

            if (_actor[actornum].weapon == -1)
                return 40;

            return MinRangeMap[_actor[actornum].weapon];
        }

        int GetRandomNumber(int max)
        {
            return new Random().Next(max + 1);
        }

        int ProcessMouse()
        {
            int buttons = 0;

            // TODO: vs check this
            _enemyState[EN_BEN, 0] = (short)_vm.Variables[_vm.VariableMouseX.Value];
            _enemyState[EN_BEN, 1] = (short)_vm.Variables[_vm.VariableMouseY.Value];

            buttons = _vm.Variables[_vm.VariableLeftButtonHold.Value] != 0 ? 1 : 0;
            buttons |= _vm.Variables[_vm.VariableRightButtonHold.Value] != 0 ? 2 : 0;

            return buttons;
        }

        bool GetKeyState(KeyCode code)
        {
            return GetKeyState((int)code);
        }

        bool GetKeyState(int code)
        {
            return _vm.GetKeyState(code);
        }

        int ProcessKeyboard()
        {
            int retval = 0;
            int dx = 0, dy = 0;
            int tmpx, tmpy;

            if (GetKeyState(0x14f) || GetKeyState(0x14b) || GetKeyState(0x147))
                dx--;

            if (GetKeyState(0x151) || GetKeyState(0x14d) || GetKeyState(0x149))
                dx++;

            if (GetKeyState(0x147) || GetKeyState(0x148) || GetKeyState(0x149))
                dy--;

            if (GetKeyState(0x14f) || GetKeyState(0x150) || GetKeyState(0x151))
                dy++;

            if (dx == _keybOldDx)
                _velocityX += 4;
            else
                _velocityX = 3;

            if (dy == _keybOldDy)
                _velocityY += 4;
            else
                _velocityY = 2;

            _keybOldDx = dx;
            _keybOldDy = dy;

            if (_velocityX > 48)
                _velocityX = 48;

            if (_velocityY > 32)
                _velocityY = 32;

            _keybX += dx * _velocityX;
            _keybY += dy * _velocityY;

            tmpx = _keybX / 4;
            tmpy = _keybY / 4;

            _keybX -= tmpx * 4;
            _keybY -= tmpy * 4;

            if (tmpx != 0 || tmpy != 0)
            {
                _enemyState[EN_BEN, 0] += (short)tmpx;
                _enemyState[EN_BEN, 1] += (short)tmpy;
            }

            if (GetKeyState((int)KeyCode.Return))
                retval |= 1;

            if (GetKeyState((int)KeyCode.Tab))
                retval |= 2;

            return retval;
        }

        bool Actor1StateFlags(int state)
        {
            // This is compressed table. It contains indexes where state
            // changes. I.e. 0-33: true, 34-38: false 39-72: true, etc.

            bool retvalue = false;

            for (var i = 0; i < Actor1StateFlagsSpans.Length; i++)
            {
                retvalue = !retvalue;
                if (Actor1StateFlagsSpans[i] <= state)
                    break;
            }
            return retvalue;
        }

        bool Actor0StateFlags2(int state)
        {
            bool retvalue = true;
            for (var i = 0; i < Actor0StateFlags2Spans.Length; i++)
            {
                retvalue = !retvalue;
                if (Actor0StateFlags2Spans[i] >= state)
                    break;
            }
            return retvalue;
        }

        int WeaponDamage(int actornum)
        {
            if (_actor[actornum].weapon == -1)
                return 10;

            return WeaponDamageMap[_actor[actornum].weapon];
        }


        bool Actor0StateFlags1(int state)
        {
            bool retvalue = true;

            for (var i = 0; i < Actor0StateFlags1Spans.Length; i++)
            {
                retvalue = !retvalue;
                if (Actor0StateFlags1Spans[i] >= state)
                    break;
            }
            return retvalue;
        }

        void ReinitActors()
        {
            if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
            {
                smlayer_setActorCostume(0, 2, ReadArray(11));
                smlayer_setActorCostume(0, 0, ReadArray(13));
                smlayer_setActorCostume(0, 1, ReadArray(12));
            }
            else
            {
                smlayer_setActorCostume(0, 2, ReadArray(12));
                smlayer_setActorCostume(0, 0, ReadArray(14));
                smlayer_setActorCostume(0, 1, ReadArray(13));
            }
            smlayer_setActorLayer(0, 1, 1);
            smlayer_setActorLayer(0, 2, 5);
            smlayer_setActorLayer(0, 0, 10);
            _actor[0].weapon = INV_HAND;
            _actor[0].weaponClass = 2;
            _actor[0].animWeaponClass = 0;
            _actor[0].newFacingFlag = 2;
            _actor[0].curFacingFlag = 0;
            _actor[0].tilt = 0;
            _actor[0].field_8 = 1;
            _actor[0].act[2].state = 1;
            _actor[0].act[2].animTilt = 1;
            _actor[0].act[0].state = 0;
            _actor[0].act[1].state = 1;
            _actor[0].act[2].room = 1;
            _actor[0].act[1].room = 1;
            _actor[0].act[0].room = 1;
            _actor[0].cursorX = 0;
        }

        void SetBit(int n)
        {
            Debug.Assert(n < 0x80);
            _iactBits[n] = 1;
        }

        void ClearBit(int n)
        {
            Debug.Assert(n < 0x80);
            _iactBits[n] = 0;
        }

        const int INV_CHAIN = 0;
        const int INV_CHAINSAW = 1;
        const int INV_MACE = 2;
        const int INV_2X4 = 3;
        const int INV_WRENCH = 4;
        const int INV_BOOT = 5;
        const int INV_HAND = 6;
        const int INV_DUST = 7;

        const int EN_ROTT1 = 0;
        // rottwheeler
        const int EN_ROTT2 = 1;
        // rottwheeler
        const int EN_ROTT3 = 2;
        // rottwheeler
        const int EN_VULTF1 = 3;
        // vulture (redhead female1)
        const int EN_VULTM1 = 4;
        // vulture (male with glasses)
        const int EN_VULTF2 = 5;
        // vulture (redhead female2)
        const int EN_VULTM2 = 6;
        // vulture (initialized as rottwheeler) (male)
        const int EN_CAVEFISH = 7;
        // Cavefish Maximum Fish
        const int EN_TORQUE = 8;
        // Father Torque
        const int EN_BEN = 9;
        // used only with handler

        static readonly int[] tilt = { -5, -4, -2, 0, 2, 4, 5 };

        static readonly int[] MaxRangeMap = { 135, 125, 130, 125, 120, 104, 104, 104 };
        static readonly int[] MinRangeMap = { 80, 40, 80, 40, 80, 80, 40, 50 };

        static readonly int[] Actor1StateFlagsSpans = { 0, 34, 39, 73, 89, 90, 92, 93, 99, 100, 117 };
        static readonly int[] Actor0StateFlags2Spans =
            {0, 10, 14, 34, 39, 73, 75, 79, 81, 90, 93, 94,
                98, 100, 117, 133, 136, 153, 158, 200, 202, 209, 212, 213, 217,
                219, 236, 256, 259, 272, 277, 311, 312, 315, 317, 328, 331, 332,
                336, 338, 355, 379, 382, 391, 396, 440, 441, 447, 450, 451, 455,
                457, 474, 502, 505, 510, 515, 549, 553, 566, 569, 570, 574, 576,
                593, 601, 604, 629, 634, 680, 682, 685, 688, 689, 693, 695, 712,
                716, 718, 748, 753, 787, 788, 804, 807, 808, 812, 814, 831, 863,
                866, 867, 872, 920, 922, 923, 926, 927, 931, 933, 950
            };
        static readonly int[] Actor0StateFlags1Spans = { 0, 2, 34, 35, 39, 69, 98, 100, 117 };

        static readonly int[] ActorAnimationData =
            {
                20, 21, 22, 23, 24, 25, 26, 13, 14, 15, 16, 17,
                18, 19, 6, 7, 8, 9, 10, 11, 12
            };

        static readonly int[] WeaponDamageMap = { 20, 300, 25, 40, 15, 10, 10, 5 };

        ScummEngine7 _vm;
        SmushPlayer _player;
        int _speed;
        bool _insaneIsRunning;

        uint _numberArray;
        int _objArray1Idx2;
        int[] _objArray1 = new int[101];
        int _objArray2Idx;
        int _objArray2Idx2;
        int[] _objArray2 = new int[101];
        byte _currSceneId;
        byte _temp2SceneId;
        byte _tempSceneId;
        int _currEnemy;
        int _currScenePropIdx;
        int _currScenePropSubIdx;
        string _currTrsMsg;
        short _sceneData2Loaded;
        short _sceneData1Loaded;
        short _keyboardDisable;
        bool _needSceneSwitch;
        int _idx2Exceeded;
        bool _beenCheated;
        bool _tiresRustle;
        int _keybOldDx;
        int _keybOldDy;
        int _velocityX;
        int _velocityY;
        int _keybX;
        int _keybY;
        bool _firstBattle;
        bool _weaponBenJustSwitched;
        bool _kickBenProgress;
        bool _battleScene;
        bool _kickEnemyProgress;
        bool _weaponEnemyJustSwitched;
        int[,] _enHdlVar = new int[9, 9];
        int _smlayer_room;
        int _smlayer_room2;

        byte[] _smush_roadrashRip;
        // FIXME: combine them in array
        byte[] _smush_roadrsh2Rip;
        byte[] _smush_roadrsh3Rip;
        byte[] _smush_goglpaltRip;
        byte[] _smush_tovista1Flu;
        byte[] _smush_tovista2Flu;
        byte[] _smush_toranchFlu;
        byte[] _smush_minedrivFlu;
        byte[] _smush_minefiteFlu;
        NutRenderer _smush_bencutNut;
        NutRenderer _smush_bensgoggNut;
        NutRenderer _smush_iconsNut;
        NutRenderer _smush_icons2Nut;
        bool _smush_isSanFileSetup;
        bool _isBenCut;
        int _smush_smushState;
        int _continueFrame;
        int _continueFrame1;
        int _counter1;
        int _iactSceneId;
        int _iactSceneId2;
        int _smush_setupsan17;
        int _smush_setupsan1;
        short _smush_setupsan2;
        int _smush_setupsan4;
        short _smush_frameStep;
        short _smush_curFrame;
        short _smush_frameNum1;
        short _smush_frameNum2;
        byte[] _smush_earlyFluContents = new byte[0x31a];
        short[,] _enemyState = new short[10, 10];
        byte[] _iactBits = new byte[0x80];
        short _mainRoadPos;
        short _posBrokenCar;
        short _posBrokenTruck;
        short _posFatherTorque;
        short _posCave;
        short _posVista;
        bool _roadBranch;
        bool _roadStop;
        bool _carIsBroken;
        bool _benHasGoggles;
        bool _mineCaveIsNear;
        bool _objectDetected;
        bool _roadBumps;
        int _approachAnim;
        int _val54d;
        int _val57d;
        bool _val115_;
        int _val211d;
        int _val213d;
        int _metEnemiesListTail;
        int[] _metEnemiesList = new int[12];

        class Enemy
        {
            public int handler;
            public int initializer;
            public short occurences;
            public int maxdamage;
            public int isEmpty;
            public int weapon;
            public int sound;
            public string filename;
            public int costume4;
            public int costume6;
            public int costume5;
            public short costumevar;
            public int maxframe;
            public int apprAnim;
        }

        Enemy[] _enemy = new Enemy[9];

        class FluConf
        {
            public int sceneId;
            public byte[] fluPtr;
            public string filename;
            public int startFrame;
            public int numFrames;
        }

        FluConf[] _fluConf = new FluConf[21];

        class SceneProp
        {
            public int actor;
            // main actor number, -1 if not applicable
            public int sound;
            public int trsId;
            public byte r;
            public byte g;
            public byte b;
            public int counter;
            public int maxCounter;
            public int index;
        }

        SceneProp[] _sceneProp = new SceneProp[139];

        class Act
        {
            public int actor;
            public byte state;
            public int room;
            public int animTilt;
            public int tilt;
            public int frame;
        }

        class Actor
        {
            public int damage;
            public int maxdamage;
            public int field_8;
            public int frame;
            public int tilt;
            public int cursorX;
            public int speed;
            public int x;
            public int y;
            public int y1;
            public int x1;
            public short weaponClass;
            public short animWeaponClass;
            public short newFacingFlag;
            public short curFacingFlag;
            public bool lost;
            public bool kicking;
            public bool field_44;
            public bool field_48;
            // unused
            public bool defunct;
            public int scenePropSubIdx;
            public int field_54;
            public int runningSound;
            public int weapon;
            public bool[] inventory = new bool[8];
            public int probability;
            public int enemyHandler;
            public Act[] act = new Act[4];

            public Actor()
            {
                for (int i = 0; i < act.Length; i++)
                {
                    act[i] = new Act();
                }
            }
        }

        Actor[] _actor = new Actor[2];
    }
}

