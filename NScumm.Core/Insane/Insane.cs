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
using NScumm.Core.IO;
using System.IO;
using NScumm.Core.Smush;

namespace NScumm.Core.Insane
{
    partial class Insane
    {
        public Insane(ScummEngine7 scumm)
        {
            _vm = scumm;

            InitVars();

            // TODO: vs
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
//                _smush_bensgoggNut = new NutRenderer(_vm, "bensgogg.nut");
//                _smush_bencutNut = new NutRenderer(_vm, "bencut.nut");
            }


//
//            _smush_iconsNut = new NutRenderer(_vm, "icons.nut");
//            _smush_icons2Nut = new NutRenderer(_vm, "icons2.nut");
        }

        public void EscapeKeyHandler()
        {
            if(!_insaneIsRunning) {
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
            // TODO: vs
//                case 18:
//                    queueSceneSwitch(17, _smush_minedrivFlu, "minedriv.san", 64, 0, _continueFrame1, 1300);
//                    writeArray(9, 1);
//                    break;
//                case 2:
//                    flu = &_fluConf[14 + _iactSceneId2];
//                    if ((_vm->_game.features & GF_DEMO) && (_vm->_game.platform == Common::kPlatformDOS))
//                        queueSceneSwitch(4, 0, "tovista.san", 64, 0, 0, 0);
//                    else
//                        queueSceneSwitch(flu->sceneId, *flu->fluPtr, flu->filenamePtr, 64, 0,
//                            flu->startFrame, flu->numFrames);
//                    break;
//                case 3:
//                    queueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0, _continueFrame, 1300);
//                    break;
//                case 4:
//                    if (_needSceneSwitch)
//                        return;
//
//                    if (readArray(6)) {
//                        if (readArray(4)) {
//                            queueSceneSwitch(14, 0, "hitdust2.san", 64, 0, 0, 0);
//                        } else {
//                            queueSceneSwitch(14, 0, "hitdust4.san", 64, 0, 0, 0);
//                        }
//                    } else {
//                        if (readArray(4)) {
//                            queueSceneSwitch(14, 0, "hitdust1.san", 64, 0, 0, 0);
//                        } else {
//                            queueSceneSwitch(14, 0, "hitdust3.san", 64, 0, 0, 0);
//                        }
//                    }
//                    break;
//                case 5:
//                    if (readArray(4)) {
//                        if (_needSceneSwitch)
//                            return;
//                        queueSceneSwitch(15, 0, "vistthru.san", 64, 0, 0, 0);
//                    } else {
//                        writeArray(1, _posVista);
//                        smush_setToFinish();
//                    }
//                    break;
//                case 6:
//                    if (readArray(4)) {
//                        if (_needSceneSwitch)
//                            return;
//                        queueSceneSwitch(15, 0, "chasthru.san", 64, 0, 0, 0);
//                    } else {
//                        if (readArray(5)) {
//                            writeArray(1, _val57d);
//                            smush_setToFinish();
//                        } else {
//                            writeArray(4, 1);
//                            queueSceneSwitch(15, 0, "chasout.san", 64, 0, 0, 0);
//                        }
//                    }
//                    break;
//                case 8:
//                    flu = &_fluConf[7 + _iactSceneId2];
//                    if ((_vm->_game.features & GF_DEMO) && (_vm->_game.platform == Common::kPlatformDOS))
//                        queueSceneSwitch(1, 0, "minedriv.san", 64, 0, 0, 0);
//                    else
//                        queueSceneSwitch(flu->sceneId, *flu->fluPtr, flu->filenamePtr, 64, 0,
//                            flu->startFrame, flu->numFrames);
//                    break;
//                case 7:
//                    flu = &_fluConf[0 + _iactSceneId2];
//                    if ((_vm->_game.features & GF_DEMO) && (_vm->_game.platform == Common::kPlatformDOS))
//                        queueSceneSwitch(1, 0, "minedriv.san", 64, 0, 0, 0);
//                    else
//                        queueSceneSwitch(flu->sceneId, *flu->fluPtr, flu->filenamePtr, 64, 0,
//                            flu->startFrame, flu->numFrames);
//                    break;
//                case 23:
//                    _actor[0].damage = 0;
//                    queueSceneSwitch(21, 0, "rottfite.san", 64, 0, 0, 0);
//                    break;
//                case 9:
//                    _actor[0].damage = 0;
//                    queueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0, _continueFrame, 1300);
//                    break;
//                case 10:
//                    _actor[0].damage = 0;
//                    queueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0, _continueFrame1, 1300);
//                    break;
//                case 13:
//                    if ((_vm->_game.features & GF_DEMO) && (_vm->_game.platform == Common::kPlatformDOS))
//                        queueSceneSwitch(1, 0, "minedriv.san", 64, 0, 0, 0);
//                    else
//                        queueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0, _continueFrame, 1300);
//                    break;
//                case 24:
//                    queueSceneSwitch(21, 0, "rottfite.san", 64, 0, 0, 0);
//                    break;
//                case 16:
//                    writeArray(4, 0);
//                    writeArray(5, 1);
//                    writeArray(1, _posBrokenCar);
//                    writeArray(3, _posBrokenTruck);
//                    smush_setToFinish();
//                    break;
//                case 15:
//                    switch (_tempSceneId) {
//                        case 5:
//                            queueSceneSwitch(6, 0, "toranch.san", 64, 0, 0, 530);
//                            break;
//                        case 6:
//                            queueSceneSwitch(4, 0, "tovista1.san", 64, 0, 0, 230);
//                            break;
//                        default:
//                            break;
//                    }
//                    break;
                default:
                    break;
            }
        }

        void InitVars()
        {
            _speed = 12;
            _currSceneId = 1;
            _currEnemy = -1;
            _battleScene = true;
            _approachAnim = -1;

            // TODO: vs
//            if ((_vm->_game.features & GF_DEMO) && (_vm->_game.platform == Common::kPlatformDOS)) {
//                init_enemyStruct(EN_ROTT1, EN_ROTT1, 0, 0, 60, 0, INV_MACE, 63, "endcrshr.san",
//                    25, 15, 16, 26, 13, 3);
//            } else {
//                init_enemyStruct(EN_ROTT1, EN_ROTT1, 0, 0, 160, 0, INV_MACE, 90, "wr2_rott.san",
//                    26, 16, 17, 27, 11, 3);
//            }
//
//            init_enemyStruct(EN_ROTT2, EN_ROTT2, 1, 0, 250, 0, INV_2X4, 90, "wr2_rott.san",
//                28, 16, 17, 42, 11, 3);
//            init_enemyStruct(EN_ROTT3, EN_ROTT3, 2, 0, 120, 0, INV_HAND, 90, "wr2_rott.san",
//                15, 16, 17, 43, 11, 3);
//            init_enemyStruct(EN_VULTF1, EN_VULTF1, 3, 0, 60, 0, INV_HAND, 91, "wr2_vltp.san",
//                29, 33, 32, 37, 12, 4);
//            init_enemyStruct(EN_VULTM1, EN_VULTM1, 4, 0, 100, 0, INV_CHAIN, 91, "wr2_vltc.san",
//                30, 33, 32, 36, 12, 4);
//            init_enemyStruct(EN_VULTF2, EN_VULTF2, 5, 0, 250, 0, INV_CHAINSAW, 91, "wr2_vlts.san",
//                31, 33, 32, 35, 12, 4);
//            init_enemyStruct(EN_VULTM2, EN_VULTM2, 6, 0, 900, 0, INV_BOOT, 91, "wr2_rott.san",
//                34, 33, 32, 45, 16, 4);
//            init_enemyStruct(EN_CAVEFISH, EN_CAVEFISH, 7, 0, 60, 0, INV_DUST, 92, "wr2_cave.san",
//                39, 0, 0, 41, 13, 2);
//            init_enemyStruct(EN_TORQUE, EN_TORQUE, 8, 0, 900, 0, INV_HAND, 93, "wr2_vltp.san",
//                57, 0, 0, 37, 12, 1);
//
//            init_fluConfStruct(1, 1, &_smush_minedrivFlu, "minedriv.san", 235, 1300);
//            init_fluConfStruct(2, 1, &_smush_minedrivFlu, "minedriv.san", 355, 1300);
//            init_fluConfStruct(3, 1, &_smush_minedrivFlu, "minedriv.san", 1255, 1300);
//            init_fluConfStruct(4, 1, &_smush_minedrivFlu, "minedriv.san", 565, 1300);
//            init_fluConfStruct(5, 1, &_smush_minedrivFlu, "minedriv.san", 1040, 1300);
//            init_fluConfStruct(8, 1, &_smush_minedrivFlu, "minedriv.san", 1040, 1300);
//            init_fluConfStruct(9, 1, &_smush_minedrivFlu, "minedriv.san", 655, 1300);
//            init_fluConfStruct(10, 1, &_smush_minedrivFlu, "minedriv.san", 115, 1300);
//            init_fluConfStruct(11, 1, &_smush_minedrivFlu, "minedriv.san", 315, 1300);
//            init_fluConfStruct(12, 1, &_smush_minedrivFlu, "minedriv.san", 235, 1300);
//            init_fluConfStruct(15, 6, &_smush_toranchFlu, "toranch.san", 115, 530);
//            init_fluConfStruct(16, 5, &_smush_tovista2Flu, "tovista2.san", 235, 290);
//            init_fluConfStruct(17, 4, &_smush_tovista1Flu, "tovista1.san", 175, 230);
//            init_fluConfStruct(18, 4, &_smush_tovista1Flu, "tovista1.san", 175, 230);
//            init_fluConfStruct(19, 6, &_smush_toranchFlu, "toranch.san", 115, 530);
//            init_fluConfStruct(20, 6, &_smush_toranchFlu, "toranch.san", 115, 530);
//
//            init_scenePropStruct(  0,  0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
//            init_scenePropStruct(  1,  0, 1, 128, 2001, 0, 0, 0, 0, 56, 2);
//            init_scenePropStruct(  2,  0, 0, 125, 1002, 0, 0, 0, 0, 35, 3);
//            init_scenePropStruct(  3,  0, 1, 129, 2002, 0, 0, 0, 0, 23, 4);
//            init_scenePropStruct(  4,  0, 1, 130, 2003, 0, 0, 0, 0, 40, 5);
//            init_scenePropStruct(  5,  0, 0, 126, 1005, 0, 0, 0, 0, 46, 6);
//            init_scenePropStruct(  6,  0, 1, 131, 2004, 0, 0, 0, 0, 39, 7);
//            init_scenePropStruct(  7,  0, 1, 132, 2005, 0, 0, 0, 0, 45, 8);
//            init_scenePropStruct(  8,  0, 1, 133, 2006, 0, 0, 0, 0, 14, 9);
//            init_scenePropStruct(  9,  0, 0, 127, 1009, 0, 0, 0, 0, 15, 10);
//            init_scenePropStruct( 10,  0, 1, 134, 501, 0, 0, 0, 0, 25, 11);
//            init_scenePropStruct( 11,  0, 1, 135, 502, 0, 0, 0, 0, 15, 0);
//            init_scenePropStruct( 12,  1, -1, 0, 0, 0xFF, 0xFF, 0xFF, 0, 0, 1);
//            init_scenePropStruct( 13,  1, 0, 291, 135, 0xFF, 0xFF, 0xFF, 0, 25, 0);
//            init_scenePropStruct( 14,  2, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
//            init_scenePropStruct( 15,  2, 1, 277, 17, 0xFC, 0, 0xFC, 0, 56, 2);
//            init_scenePropStruct( 16,  2, 0, 288, 18, 0xFF, 0xFF, 0xFF, 0, 56, 3);
//            init_scenePropStruct( 17,  2, 1, 278, 19, 0xFC, 0, 0xFC, 0, 56, 0);
//            init_scenePropStruct( 18,  3, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
//            init_scenePropStruct( 19,  3, 1, 282, 23, 0xFC, 0, 0xFC, 0, 56, 0);
//            init_scenePropStruct( 20,  4, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
//            init_scenePropStruct( 21,  4, 1, 283, 24, 0xFC, 0, 0xFC, 0, 56, 0);
//            init_scenePropStruct( 22,  5, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
//            init_scenePropStruct( 23,  5, 1, 284, 25, 0xFC, 0, 0xFC, 0, 56, 0);
//            init_scenePropStruct( 24,  6, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
//            init_scenePropStruct( 25,  6, 1, 285, 26, 0xFC, 0, 0xFC, 0, 56, 0);
//            init_scenePropStruct( 26,  7, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
//            init_scenePropStruct( 27,  7, 1, 286, 27, 0xFC, 0, 0xFC, 0, 56, 0);
//            init_scenePropStruct( 28,  8, -1, 0, 0, 0xFC, 0, 0xFC, 0, 0, 1);
//            init_scenePropStruct( 29,  8, 1, 287, 28, 0xFC, 0, 0xFC, 0, 56, 0);
//            init_scenePropStruct( 30,  9, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
//            init_scenePropStruct( 31,  9, 1, 261, 1, 0xFC, 0, 0, 0, 40, 2);
//            init_scenePropStruct( 32,  9, 1, 262, 2, 0xFC, 0, 0, 0, 40, 3);
//            init_scenePropStruct( 33,  9, 1, 263, 3, 0xFC, 0, 0, 0, 40, 0);
//            init_scenePropStruct( 34, 10, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
//            init_scenePropStruct( 35, 10, 1, 263, 3, 0xFC, 0, 0, 0, 30, 0);
//            init_scenePropStruct( 36, 11, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
//            init_scenePropStruct( 37, 11, 1, 264, 4, 0xFC, 0, 0, 0, 30, 0);
//            init_scenePropStruct( 38, 12, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
//            init_scenePropStruct( 39, 12, 1, 265, 5, 0xFC, 0, 0, 0, 30, 0);
//            init_scenePropStruct( 40, 13, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
//            init_scenePropStruct( 41, 13, 1, 266, 6, 0xFC, 0, 0, 0, 30, 0);
//            init_scenePropStruct( 42, 14, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
//            init_scenePropStruct( 43, 14, 1, 267, 7, 0xFC, 0, 0, 0, 30, 0);
//            init_scenePropStruct( 44, 15, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
//            init_scenePropStruct( 45, 15, 1, 268, 8, 0xFC, 0, 0, 0, 30, 0);
//            init_scenePropStruct( 46, 16, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
//            init_scenePropStruct( 47, 16, 1, 274, 14, 0xFC, 0, 0, 0, 30, 0);
//            init_scenePropStruct( 48, 17, -1, 0, 0, 0xFC, 0, 0, 0, 0, 1);
//            init_scenePropStruct( 49, 17, 1, 270, 10, 0xFC, 0, 0, 0, 30, 0);
//            init_scenePropStruct( 50, 18, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
//            init_scenePropStruct( 51, 18, 0, 289, 45, 0xFF, 0xFF, 0xFF, 0, 40, 2);
//            init_scenePropStruct( 52, 18, 1, 177, 49, 0xFC, 0xFC, 0x54, 0, 40, 3);
//            init_scenePropStruct( 53, 18, 1, 178, 50, 0xFC, 0xFC, 0x54, 0, 40, 4);
//            init_scenePropStruct( 54, 18, 0, 290, 47, 0xFF, 0xFF, 0xFF, 0, 40, 0);
//            init_scenePropStruct( 55, 19, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
//            init_scenePropStruct( 56, 19, 1, 179, 51, 0xFC, 0xFC, 0x54, 0, 40, 0);
//            init_scenePropStruct( 57, 20, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
//            init_scenePropStruct( 58, 20, 1, 183, 55, 0xFC, 0xFC, 0x54, 0, 40, 0);
//            init_scenePropStruct( 59, 21, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
//            init_scenePropStruct( 60, 21, 1, 184, 56, 0xFC, 0xFC, 0x54, 0, 40, 0);
//            init_scenePropStruct( 61, 22, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
//            init_scenePropStruct( 62, 22, 1, 186, 58, 0xFC, 0xFC, 0x54, 0, 40, 0);
//            init_scenePropStruct( 63, 23, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
//            init_scenePropStruct( 64, 23, 1, 191, 63, 0xFC, 0xFC, 0x54, 0, 40, 0);
//            init_scenePropStruct( 65, 24, -1, 0, 0, 0xFC, 0xFC, 0x54, 0, 0, 1);
//            init_scenePropStruct( 66, 24, 1, 192, 64, 0xFC, 0xFC, 0x54, 0, 40, 0);
//            init_scenePropStruct( 67, 25, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
//            init_scenePropStruct( 68, 25, 1, 220, 93, 0xBC, 0x78, 0x48, 0, 40, 2);
//            init_scenePropStruct( 69, 25, 1, 221, 94, 0xBC, 0x78, 0x48, 0, 40, 3);
//            init_scenePropStruct( 70, 25, 1, 222, 95, 0xBC, 0x78, 0x48, 0, 40, 0);
//            init_scenePropStruct( 71, 26, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
//            init_scenePropStruct( 72, 26, 1, 223, 96, 0xBC, 0x78, 0x48, 0, 40, 0);
//            init_scenePropStruct( 73, 27, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
//            init_scenePropStruct( 74, 27, 1, 224, 97, 0xBC, 0x78, 0x48, 0, 40, 0);
//            init_scenePropStruct( 75, 28, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
//            init_scenePropStruct( 76, 28, 1, 225, 98, 0xBC, 0x78, 0x48, 0, 40, 0);
//            init_scenePropStruct( 77, 29, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
//            init_scenePropStruct( 78, 29, 1, 226, 99, 0xBC, 0x78, 0x48, 0, 40, 0);
//            init_scenePropStruct( 79, 30, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
//            init_scenePropStruct( 80, 30, 1, 228, 101, 0xBC, 0x78, 0x48, 0, 40, 0);
//            init_scenePropStruct( 81, 31, -1, 0, 0, 0xBC, 0x78, 0x48, 0, 0, 1);
//            init_scenePropStruct( 82, 31, 1, 229, 102, 0xBC, 0x78, 0x48, 0, 40, 0);
//            init_scenePropStruct( 83, 32, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
//            init_scenePropStruct( 84, 32, 1, 233, 106, 0xA8, 0xA8, 0xA8, 0, 40, 2);
//            init_scenePropStruct( 85, 32, 1, 234, 107, 0xA8, 0xA8, 0xA8, 0, 40, 0);
//            init_scenePropStruct( 86, 33, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
//            init_scenePropStruct( 87, 33, 1, 241, 114, 0xA8, 0xA8, 0xA8, 0, 40, 2);
//            init_scenePropStruct( 88, 33, 1, 242, 115, 0xA8, 0xA8, 0xA8, 0, 40, 0);
//            init_scenePropStruct( 89, 34, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
//            init_scenePropStruct( 90, 34, 1, 237, 110, 0xA8, 0xA8, 0xA8, 0, 40, 2);
//            init_scenePropStruct( 91, 34, 1, 238, 111, 0xA8, 0xA8, 0xA8, 0, 40, 3);
//            init_scenePropStruct( 92, 34, 1, 239, 112, 0xA8, 0xA8, 0xA8, 0, 40, 0);
//            init_scenePropStruct( 93, 35, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
//            init_scenePropStruct( 94, 35, 1, 258, 131, 0xA8, 0xA8, 0xA8, 0, 40, 0);
//            init_scenePropStruct( 95, 36, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
//            init_scenePropStruct( 96, 36, 1, 260, 133, 0xA8, 0xA8, 0xA8, 0, 40, 0);
//            init_scenePropStruct( 97, 37, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
//            init_scenePropStruct( 98, 37, 1, 252, 125, 0xA8, 0xA8, 0xA8, 0, 40, 0);
//            init_scenePropStruct( 99, 38, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
//            init_scenePropStruct(100, 38, 1, 254, 127, 0xA8, 0xA8, 0xA8, 0, 40, 0);
//            init_scenePropStruct(101, 39, -1, 0, 0, 0xA8, 0xA8, 0xA8, 0, 0, 1);
//            init_scenePropStruct(102, 39, 1, 236, 109, 0xA8, 0xA8, 0xA8, 0, 40, 0);
//            init_scenePropStruct(103, 40, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
//            init_scenePropStruct(104, 40, 1, 174, 42, 4, 0xBC, 0, 0, 40, 0);
//            init_scenePropStruct(105, 41, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
//            init_scenePropStruct(106, 41, 1, 167, 36, 4, 0xBC, 0, 0, 40, 0);
//            init_scenePropStruct(107, 42, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
//            init_scenePropStruct(108, 42, 1, 160, 29, 4, 0xBC, 0, 0, 40, 0);
//            init_scenePropStruct(109, 43, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
//            init_scenePropStruct(110, 43, 1, 161, 30, 4, 0xBC, 0, 0, 40, 0);
//            init_scenePropStruct(111, 44, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
//            init_scenePropStruct(112, 44, 1, 163, 32, 4, 0xBC, 0, 0, 40, 0);
//            init_scenePropStruct(113, 45, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
//            init_scenePropStruct(114, 45, 1, 164, 33, 4, 0xBC, 0, 0, 40, 0);
//            init_scenePropStruct(115, 46, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
//            init_scenePropStruct(116, 46, 1, 170, 39, 4, 0xBC, 0, 0, 40, 0);
//            init_scenePropStruct(117, 47, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
//            init_scenePropStruct(118, 47, 1, 166, 35, 4, 0xBC, 0, 0, 40, 0);
//            init_scenePropStruct(119, 48, -1, 0, 0, 4, 0xBC, 0, 0, 0, 1);
//            init_scenePropStruct(120, 48, 1, 175, 43, 4, 0xBC, 0, 0, 40, 0);
//            init_scenePropStruct(121, 49, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
//            init_scenePropStruct(122, 49, 1, 203, 75, 0x40, 0x40, 0xFC, 0, 40, 0);
//            init_scenePropStruct(123, 50, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
//            init_scenePropStruct(124, 50, 1, 194, 66, 0x40, 0x40, 0xFC, 0, 40, 0);
//            init_scenePropStruct(125, 51, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
//            init_scenePropStruct(126, 51, 1, 195, 67, 0x40, 0x40, 0xFC, 0, 40, 0);
//            init_scenePropStruct(127, 52, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
//            init_scenePropStruct(128, 52, 1, 199, 71, 0x40, 0x40, 0xFC, 0, 40, 0);
//            init_scenePropStruct(129, 53, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
//            init_scenePropStruct(130, 53, 1, 205, 77, 0x40, 0x40, 0xFC, 0, 40, 0);
//            init_scenePropStruct(131, 54, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
//            init_scenePropStruct(132, 54, 1, 212, 85, 0x40, 0x40, 0xFC, 0, 40, 0);
//            init_scenePropStruct(133, 55, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
//            init_scenePropStruct(134, 55, 1, 201, 73, 0x40, 0x40, 0xFC, 0, 40, 0);
//            init_scenePropStruct(135, 56, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
//            init_scenePropStruct(136, 56, 1, 198, 70, 0x40, 0x40, 0xFC, 0, 40, 0);
//            init_scenePropStruct(137, 57, -1, 0, 0, 0x40, 0x40, 0xFC, 0, 0, 1);
//            init_scenePropStruct(138, 57, 0, 59, 134, 0xFF, 0xFF, 0xFF, 0, 30, 0);
//
//            _actor[0].damage = 0;
//            if ((_vm->_game.features & GF_DEMO) && (_vm->_game.platform == Common::kPlatformDOS))
//                _actor[0].maxdamage = 60;
//            else
//                _actor[0].maxdamage = 80;
//            _actor[0].field_8 = 1;
//            _actor[0].frame = 0;
//            _actor[0].tilt = 0;
//            _actor[0].cursorX = 0;
//            _actor[0].speed = 0;
//            _actor[0].x = 160;
//            _actor[0].y = 0;
//            _actor[0].y1 = -1;
//            _actor[0].x1 = -1;
//            _actor[0].weaponClass = 2;
//            _actor[0].animWeaponClass = 0;
//            _actor[0].newFacingFlag = 2;
//            _actor[0].curFacingFlag = 0;
//            _actor[0].lost = false;
//            _actor[0].kicking = false;
//            _actor[0].field_44 = false;
//            _actor[0].field_48 = false;
//            _actor[0].defunct = 0;
//            _actor[0].scenePropSubIdx = 0;
//            _actor[0].field_54 = 0;
//            _actor[0].runningSound = 0;
//            _actor[0].weapon = INV_HAND;
//            _actor[0].inventory[INV_CHAIN] = 0;
//            _actor[0].inventory[INV_CHAINSAW] = 0;
//            _actor[0].inventory[INV_MACE] = 0;
//            _actor[0].inventory[INV_2X4] = 0;
//            _actor[0].inventory[INV_WRENCH] = 1;
//            _actor[0].inventory[INV_BOOT] = 1;
//            _actor[0].inventory[INV_HAND] = 1;
//            _actor[0].inventory[INV_DUST] = 0;
//            _actor[0].probability = 5;
//            _actor[0].enemyHandler = EN_BEN;
//            init_actStruct(0, 0, 11, 1, 1, 0, 0, 0);
//            init_actStruct(0, 1, 12, 1, 1, 0, 0, 0);
//            init_actStruct(0, 2, 1,  1, 1, 0, 0, 0);
//            init_actStruct(0, 3, 1,  1, 1, 0, 0, 0);
//
//            _actor[1].damage = 0;
//            _actor[1].maxdamage = -1;
//            _actor[1].field_8 = 1;
//            _actor[1].frame = 0;
//            _actor[1].tilt = 0;
//            _actor[1].cursorX = 0;
//            _actor[1].speed = 0;
//            _actor[1].x = 160;
//            _actor[1].y = 0;
//            _actor[1].y1 = -1;
//            _actor[1].x1 = -1;
//            _actor[1].weaponClass = 2;
//            _actor[1].animWeaponClass = 0;
//            _actor[1].newFacingFlag = 0;
//            _actor[1].curFacingFlag = 0;
//            _actor[1].lost = false;
//            _actor[1].kicking = false;
//            _actor[1].field_44 = false;
//            _actor[1].field_48 = false;
//            _actor[1].defunct = 0;
//            _actor[1].scenePropSubIdx = 0;
//            _actor[1].field_54 = 0;
//            _actor[1].runningSound = 0;
//            _actor[1].weapon = INV_HAND;
//            _actor[1].inventory[INV_CHAIN] = 0;
//            _actor[1].inventory[INV_CHAINSAW] = 0;
//            _actor[1].inventory[INV_MACE] = 1;
//            _actor[1].inventory[INV_2X4] = 0;
//            _actor[1].inventory[INV_WRENCH] = 0;
//            _actor[1].inventory[INV_BOOT] = 0;
//            _actor[1].inventory[INV_HAND] = 0;
//            _actor[1].inventory[INV_DUST] = 0;
//            _actor[1].probability = 5;
//            _actor[1].enemyHandler = -1;
//
//            init_actStruct(1, 0, 14, 1, 1, 0, 0, 0);
//            init_actStruct(1, 1, 15, 1, 1, 0, 0, 0);
//            init_actStruct(1, 2, 13, 1, 1, 0, 0, 0);
//            init_actStruct(1, 3, 13, 1, 1, 0, 0, 0);
        }

        byte[] ReadFileToMem(string name)
        {
            return File.ReadAllBytes(ScummHelper.LocatePath(Path.GetDirectoryName(_vm.Game.Path), name));
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

                if (LoadSceneData(_temp2SceneId, 0, 2) != 0)
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
            // TODO: vs
//            smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1, _smlayer_room);
//            smlayer_putActor(0, 0, _actor[0].x, _actor[0].y1, _smlayer_room);
//            smlayer_putActor(0, 1, _actor[0].x, _actor[0].y1, _smlayer_room);
//            smlayer_putActor(1, 2, _actor[0].x, _actor[0].y1, _smlayer_room);
//            smlayer_putActor(1, 0, _actor[0].x, _actor[0].y1, _smlayer_room);
//            smlayer_putActor(1, 1, _actor[0].x, _actor[0].y1, _smlayer_room);
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

        int smush_setupSanWithFlu(string filename, int setupsan2, int step1, int step2, int setupsan1, byte[] fluPtr, int numFrames)
        {
            var tmp = 0;
            int offset;

            Debug.WriteLine("smush_setupSanWithFlu({0}, {1}, {2}, {3}, {4}, {5}, {6})", filename, setupsan2,
                step1, step2, setupsan1, fluPtr, numFrames);

            _smush_setupsan1 = setupsan1;

            /* skip FLUP marker */
            if (System.Text.Encoding.ASCII.GetString(fluPtr, 0, 4) == "FLUP")
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

        void smush_setFrameSteps(int step1, int step2) {
            _smush_frameNum2 = _smush_curFrame;
            _smush_frameNum1 = (short)step2;
            _smush_frameStep = (short)step1;
        }

        void smush_warpMouse(int x, int y, int buttons) {
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

        ScummEngine7 _vm;
        SmushPlayer _player;
        int _speed;
        bool _insaneIsRunning;

        int _numberArray;
        int _emulTimerId;
        int _emulateInterrupt;
        int _flag1d;
        int _mainTimerId;
        int _objArray1Idx;
        int _objArray1Idx2;
        int[] _objArray1 = new int[101];
        int _objArray2Idx;
        int _objArray2Idx2;
        int[] _objArray2 = new int[101];
        byte _currSceneId;
        int _timer1Flag;
        int _timer3Id;
        int _timer4Id;
        int _timer6Id;
        int _timer7Id;
        int _timerSpriteId;
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
        int _firstBattle;
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
    }
}

