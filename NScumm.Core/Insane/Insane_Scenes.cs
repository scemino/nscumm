//
//  Insane_Scenes.cs
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

namespace NScumm.Core.Insane
{
    partial class Insane
    {
        public void ProcPreRendering() 
        {
            _smush_isSanFileSetup = false; // FIXME: This shouldn't be here

            SwitchSceneIfNeeded();

            if (_sceneData1Loaded!=0) {
                _val115_ = true;
                if (_keyboardDisable==0) {
                    smush_changeState(1);
                    _keyboardDisable = 1;
                }
            } else {
                _val115_ = false;
                if (_keyboardDisable!=0) {
                    smush_changeState(0);
                    _keyboardDisable = 0;
                }
            }
        }

        public void SetSmushParams(int speed)
        {
            _speed = speed;
        }

        public void RunScene(int arraynum) {
            _insaneIsRunning = true;
            _player = _vm.SmushPlayer;
            _player.Insanity(true);

            _numberArray = arraynum;

            // zeroValues1()
            _objArray2Idx = 0;
            _objArray2Idx2 = 0;
            // zeroValues2()
            _objArray1Idx = 0;
            _objArray1Idx2 = 0;
            // zeroValues3()
            _currScenePropIdx = 0;
            _currScenePropSubIdx = 0;
            _currTrsMsg = null;

            smush_warpMouse(160, 100, -1);
            PutActors();
//            ReadState();

            Debug.WriteLine("INSANE Arg: {0}", ReadArray(0));

            switch (ReadArray(0)) {
//                case 1:
//                    InitScene(1);
//                    SetupValues();
//                    if (_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm.Game.Platform == Common::kPlatformDOS)*/)
//                        smlayer_setActorCostume(0, 2, readArray(9));
//                    else
//                        smlayer_setActorCostume(0, 2, readArray(10));
//                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1 + 190, _smlayer_room2);
//                    startVideo("minedriv.san", 1, 32, 12, 0);
//                    break;
//                case 2:
//                    setupValues();
//                    if ((_vm._game.features & GF_DEMO) && (_vm._game.platform == Common::kPlatformDOS))
//                        smlayer_setActorCostume(0, 2, readArray(10));
//                    else
//                        smlayer_setActorCostume(0, 2, readArray(11));
//                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1 + 190, _smlayer_room2);
//
//                    _mainRoadPos = readArray(2);
//                    if ((_vm._game.features & GF_DEMO) && (_vm._game.platform == Common::kPlatformDOS)) {
//                        initScene(5);
//                        startVideo("tovista.san", 1, 32, 12, 0);
//                    } else if (_mainRoadPos == _posBrokenTruck) {
//                        initScene(5);
//                        startVideo("tovista2.san", 1, 32, 12, 0);
//                    } else if (_mainRoadPos == _posBrokenCar) {
//                        initScene(5);
//                        startVideo("tovista2.san", 1, 32, 12, 0, _smush_tovista2Flu, 60);
//                    } else {
//                        initScene(4);
//                        startVideo("tovista1.san", 1, 32, 12, 0);
//                    }
//                    break;
//                case 3:
//                    setupValues();
//                    if ((_vm._game.features & GF_DEMO) && (_vm._game.platform == Common::kPlatformDOS))
//                        smlayer_setActorCostume(0, 2, readArray(10));
//                    else
//                        smlayer_setActorCostume(0, 2, readArray(11));
//                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1 + 190, _smlayer_room2);
//                    _mainRoadPos = readArray(2);
//                    if (_mainRoadPos == _posBrokenTruck) {
//                        initScene(6);
//                        startVideo("toranch.san", 1, 32, 12, 0, _smush_toranchFlu, 300);
//                    } else if (_mainRoadPos == _posBrokenCar) {
//                        initScene(6);
//                        startVideo("toranch.san", 1, 32, 12, 0, _smush_toranchFlu, 240);
//                    } else {
//                        initScene(6);
//                        startVideo("toranch.san", 1, 32, 12, 0);
//                    }
//                    break;
//                case 4:
//                    _firstBattle = true;
//                    _currEnemy = EN_ROTT1;
//                    initScene(13);
//                    startVideo("minefite.san", 1, 32, 12, 0);
//                    break;
//                case 5:
//                    writeArray(1, _val54d);
//                    initScene(24);
//                    startVideo("rottopen.san", 1, 32, 12, 0);
//                    break;
//                case 6:
//                    initScene(1);
//                    setupValues();
//                    smlayer_setFluPalette(_smush_roadrashRip, 1);
//                    smlayer_setActorCostume(0, 2, readArray(10));
//                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1 + 190, _smlayer_room2);
//                    startVideo("minedriv.san", 1, 32, 12, 0, _smush_minedrivFlu, 420);
//                    break;
//                case 7:
//                case 8:
//                case 9:
//                    break;
//                case 10:
//                    initScene(26);
//                    writeArray(1, _val54d);
//                    startVideo("credits.san", 1, 32, 12, 0);
//                    break;
                default:
                    throw new NotSupportedException(string.Format("Unknown FT_INSANE mode {0}", ReadArray(0)));
            }

            PutActors();
//            _enemy[EN_ROTT3].maxdamage = 120;

            _insaneIsRunning = false;
            _player.Insanity(false);

//            if (!(_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)) {
//                WriteArray(50, _actor[0].inventory[INV_CHAIN]);
//                WriteArray(51, _actor[0].inventory[INV_CHAINSAW]);
//                WriteArray(52, _actor[0].inventory[INV_MACE]);
//                WriteArray(53, _actor[0].inventory[INV_2X4]);
//                WriteArray(54, _actor[0].inventory[INV_WRENCH]);
//                WriteArray(55, _actor[0].inventory[INV_DUST]);
//                WriteArray(56, _enemy[EN_CAVEFISH].isEmpty);
//                WriteArray(337, _enemy[EN_TORQUE].occurences);
//                WriteArray(329, _enemy[EN_ROTT1].occurences);
//                WriteArray(330, _enemy[EN_ROTT2].occurences);
//                WriteArray(331, _enemy[EN_ROTT3].occurences);
//                WriteArray(332, _enemy[EN_VULTF1].occurences);
//                WriteArray(333, _enemy[EN_VULTM1].occurences);
//                WriteArray(334, _enemy[EN_VULTF2].occurences);
//                WriteArray(335, _enemy[EN_VULTM2].occurences);
//                WriteArray(336, _enemy[EN_CAVEFISH].occurences);
//                WriteArray(339, _enemy[EN_VULTF2].isEmpty);
//                WriteArray(340, _enemy[EN_VULTM2].isEmpty);
//            }

            _vm.Sound.StopAllSounds(); // IMUSE_StopAllSounds();
        }

        public void ProcPostRendering(byte[] renderBitmap, int codecparam, int setupsan12,
            int setupsan13, int curFrame, int maxFrame) {
            int tmpSnd;
            bool needMore = false;

            Debug.WriteLine("procPostRendering");

            throw new NotImplementedException();
//            if (!_keyboardDisable) {
//                switch (_currSceneId) {
//                    case 12:
//                        PostCase11(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        break;
//                    case 1:
//                        PostCase0(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        if (!smlayer_isSoundRunning(88))
//                            smlayer_startSfx(88);
//                        smlayer_soundSetPan(88, ((_actor[0].x+160)>>2)+64);
//                        if (_tiresRustle) {
//                            if (!smlayer_isSoundRunning(87))
//                                smlayer_startSfx(87);
//                        } else {
//                            smlayer_stopSound(87);
//                        }
//                        break;
//                    case 18:
//                    case 19:
//                        postCase17(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        smlayer_stopSound(95);
//                        smlayer_stopSound(87);
//                        smlayer_stopSound(88);
//                        if (!smlayer_isSoundRunning(88))
//                            smlayer_startSfx(88);
//                        break;
//                    case 17:
//                        postCase16(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        if (!smlayer_isSoundRunning(88))
//                            smlayer_startSfx(88);
//                        break;
//                    case 2:
//                        postCase1(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        break;
//                    case 3:
//                        postCase2(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        needMore = true;
//                        if (!smlayer_isSoundRunning(89)) {
//                            smlayer_startSfx(89);
//                            smlayer_soundSetPriority(89, 100);
//                        }
//                        tmpSnd = _enemy[_currEnemy].sound;
//                        if (!smlayer_isSoundRunning(tmpSnd)) {
//                            smlayer_startSfx(tmpSnd);
//                            smlayer_soundSetPriority(tmpSnd, 100);
//                        }
//                        smlayer_soundSetPan(89, ((_actor[0].x+160)>>2)+64);
//                        smlayer_soundSetPan(tmpSnd, ((_actor[1].x+160)>>2)+64);
//                        if (!_tiresRustle) {
//                            smlayer_stopSound(87);
//                        } else {
//                            if (!smlayer_isSoundRunning(87))
//                                smlayer_startSfx(87);
//                        }
//                        break;
//                    case 21:
//                        postCase20(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        needMore = true;
//                        if (!smlayer_isSoundRunning(89)) {
//                            smlayer_startSfx(89);
//                            smlayer_soundSetPriority(89, 100);
//                        }
//                        tmpSnd = _enemy[_currEnemy].sound;
//                        if (!smlayer_isSoundRunning(tmpSnd)) {
//                            smlayer_startSfx(tmpSnd);
//                            smlayer_soundSetPriority(tmpSnd, 100);
//                        }
//                        smlayer_soundSetPan(89, ((_actor[0].x+160)>>2)+64);
//                        smlayer_soundSetPan(tmpSnd, ((_actor[1].x+160)>>2)+64);
//                        break;
//                    case 4:
//                    case 5:
//                        postCase3(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        if (!smlayer_isSoundRunning(88))
//                            smlayer_startSfx(88);
//                        smlayer_soundSetPan(88, ((_actor[0].x+160)>>2)+64);
//                        break;
//                    case 6:
//                        postCase5(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        if (!smlayer_isSoundRunning(88))
//                            smlayer_startSfx(88);
//                        smlayer_soundSetPan(88, ((_actor[0].x+160)>>2)+64);
//                        break;
//                    case 7:
//                    case 8:
//                        postCase6(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        break;
//                    case 9:
//                    case 23:
//                        postCase8(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        break;
//                    case 10:
//                        postCase9(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        break;
//                    case 11:
//                    case 20:
//                    case 22:
//                        postCase10(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        break;
//                    case 14:
//                        postCase23(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        break;
//                    case 13:
//                        postCase12(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        needMore = true;
//                        if (!smlayer_isSoundRunning(89)) {
//                            smlayer_startSfx(89);
//                            smlayer_soundSetPriority(89, 100);
//                        }
//                        tmpSnd = _enemy[_currEnemy].sound;
//                        if (!smlayer_isSoundRunning(tmpSnd)) {
//                            smlayer_startSfx(tmpSnd);
//                            smlayer_soundSetPriority(tmpSnd, 100);
//                        }
//                        smlayer_soundSetPan(89, ((_actor[0].x+160)>>2)+64);
//                        smlayer_soundSetPan(tmpSnd, ((_actor[1].x+160)>>2)+64);
//                        break;
//                    case 24:
//                        if (!smlayer_isSoundRunning(90)) {
//                            smlayer_startSfx(90);
//                            smlayer_soundSetPriority(90, 100);
//                        }
//                        postCase23(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        break;
//                    case 15:
//                    case 16:
//                        postCase14(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//                        break;
//                    case 25:
//                    case 26:
//                        break;
//                }
//
//                if (_currScenePropIdx)
//                    postCaseAll(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//
//                _actor[0].frame++;
//                _actor[0].act[3].frame++;
//                _actor[0].act[2].frame++;
//                _actor[0].act[1].frame++;
//                _actor[0].act[0].frame++;
//                _actor[1].act[3].frame++;
//                _actor[1].frame++;
//                _actor[1].act[2].frame++;
//                _actor[1].act[1].frame++;
//                _actor[1].act[0].frame++;
//            }
//
//            if (!_val115_) {
//                smlayer_overrideDrawActorAt(&renderBitmap[0], renderBitmap[2], renderBitmap[3]);
//                _isBenCut = 0;
//            }
//
//            if (_isBenCut)
//                smlayer_drawSomething(renderBitmap, codecparam, 89, 56, 1, _smush_bencutNut, 0, 0, 0);
//
//            if (!_keyboardDisable)
//                _vm->processActors();
//
//            if (needMore)
//                postCaseMore(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
//
//            _tiresRustle = false;
        }

        void StopSceneSounds(int sceneId) {
            int flag = 0;

            Debug.WriteLine("StopSceneSounds({0})", sceneId);
            // TODO: vs
            switch (sceneId) {
                case 1:
//                    smlayer_stopSound(88);
//                    smlayer_stopSound(86);
//                    smlayer_stopSound(87);
                    flag = 1;
                    break;
                case 18:
                case 19:
//                    smlayer_stopSound(88);
                    flag = 1;
                    break;
                case 17:
//                    smlayer_stopSound(88);
//                    smlayer_stopSound(94);
                    flag = 1;
                    break;
                case 2:
                case 7:
                case 8:
                    flag = 1;
                    break;
                case 3:
                    flag = 1;
                    StopSceneSounds13();
                    break;
                case 21:
                    flag = 1;
                    StopSceneSounds13();
                    break;
                case 13:
                    StopSceneSounds13();
                    break;
                case 4:
                case 5:
                case 6:
//                    smlayer_stopSound(88);
//                    smlayer_stopSound(86);
                    flag = 1;
                    break;
                case 24:
//                    smlayer_stopSound(90);
                    break;
                case 9:
                case 10:
                case 11:
                case 12:
                case 14:
                case 15:
                case 16:
                case 20:
                case 22:
                case 23:
                    break;
            }
            if (flag==0)
                return;

//            smlayer_setActorCostume(0, 2, 0);
//            smlayer_setActorCostume(0, 0, 0);
//            smlayer_setActorCostume(0, 1, 0);
//            smlayer_setActorCostume(1, 2, 0);
//            smlayer_setActorCostume(1, 0, 0);
//            smlayer_setActorCostume(1, 1, 0);

            return;
        }

        void StopSceneSounds13(){
            //                    if (_actor[0].runningSound != 0)
            //                        smlayer_stopSound(_actor[0].runningSound);
            //                    _actor[0].runningSound = 0;
            //
            //                    if (_actor[1].runningSound != 0)
            //                        smlayer_stopSound(_actor[1].runningSound);
            //                    _actor[1].runningSound = 0;
            //
            //                    if (_currScenePropIdx != 0)
            //                        shutCurrentScene();
            //
            //                    _currScenePropSubIdx = 0;
            //                    _currTrsMsg = 0;
            //                    _actor[0].defunct = 0;
            //                    _actor[0].scenePropSubIdx = 0;
            //                    _actor[0].field_54 = 0;
            //                    _actor[1].defunct = 0;
            //                    _actor[1].scenePropSubIdx = 0;
            //                    _actor[1].field_54 = 0;
            //                    if ((_vm._game.features & GF_DEMO) && (_vm._game.platform == Common::kPlatformDOS)) {
            //                        smlayer_stopSound(59);
            //                        smlayer_stopSound(63);
            //                    } else {
            //                        smlayer_stopSound(89);
            //                        smlayer_stopSound(90);
            //                        smlayer_stopSound(91);
            //                        smlayer_stopSound(92);
            //                        smlayer_stopSound(93);
            //                        smlayer_stopSound(95);
            //                        smlayer_stopSound(87);
            //                    }
        }

        int LoadSceneData(int scene, int flag, int phase) {
            // TODO: vs
//            if ((_vm._game.features & GF_DEMO) && (_vm._game.platform == Common::kPlatformDOS))
//                return 1;

            int retvalue = 1;

            Debug.WriteLine("Insane::loadSceneData({0}, {1}, {2})", scene, flag, phase);

            switch (scene) {
                case 1:
//                    smlayer_loadSound(88, flag, phase);
//                    smlayer_loadSound(86, flag, phase);
//                    smlayer_loadSound(87, flag, phase);
//                    smlayer_loadCostume(10, phase);
                    break;
                case 4:
                case 5:
                case 6:
//                    smlayer_loadSound(88, flag, phase);
//                    smlayer_loadCostume(11, phase);
                    break;
                case 3:
                case 13:
//                    switch (_currEnemy) {
//                        case EN_TORQUE:
//                            smlayer_loadSound(59, flag, phase);
//                            smlayer_loadSound(93, flag, phase);
//                            smlayer_loadCostume(57, phase);
//                            smlayer_loadCostume(37, phase);
//                            break;
//                        case EN_ROTT1:
//                            smlayer_loadSound(201, flag, phase);
//                            smlayer_loadSound(194, flag, phase);
//                            smlayer_loadSound(195, flag, phase);
//                            smlayer_loadSound(199, flag, phase);
//                            smlayer_loadSound(205, flag, phase);
//                            smlayer_loadSound(212, flag, phase);
//                            smlayer_loadSound(198, flag, phase);
//                            smlayer_loadSound(203, flag, phase);
//                            smlayer_loadSound(213, flag, phase);
//                            smlayer_loadSound(215, flag, phase);
//                            smlayer_loadSound(216, flag, phase);
//                            smlayer_loadSound(217, flag, phase);
//                            smlayer_loadSound(218, flag, phase);
//                            smlayer_loadSound(90, flag, phase);
//                            smlayer_loadCostume(26, phase);
//                            smlayer_loadCostume(16, phase);
//                            smlayer_loadCostume(17, phase);
//                            smlayer_loadCostume(27, phase);
//                            break;
//                        case EN_ROTT2:
//                            smlayer_loadSound(242, flag, phase);
//                            smlayer_loadSound(244, flag, phase);
//                            smlayer_loadSound(236, flag, phase);
//                            smlayer_loadSound(238, flag, phase);
//                            smlayer_loadSound(239, flag, phase);
//                            smlayer_loadSound(240, flag, phase);
//                            smlayer_loadSound(258, flag, phase);
//                            smlayer_loadSound(259, flag, phase);
//                            smlayer_loadSound(260, flag, phase);
//                            smlayer_loadSound(243, flag, phase);
//                            smlayer_loadSound(244, flag, phase);
//                            smlayer_loadSound(245, flag, phase);
//                            smlayer_loadSound(246, flag, phase);
//                            smlayer_loadSound(233, flag, phase);
//                            smlayer_loadSound(234, flag, phase);
//                            smlayer_loadSound(241, flag, phase);
//                            smlayer_loadSound(242, flag, phase);
//                            smlayer_loadSound(90, flag, phase);
//                            smlayer_loadCostume(28, phase);
//                            smlayer_loadCostume(16, phase);
//                            smlayer_loadCostume(17, phase);
//                            smlayer_loadCostume(42, phase);
//                            break;
//                        case EN_ROTT3:
//                            smlayer_loadSound(223, flag, phase);
//                            smlayer_loadSound(224, flag, phase);
//                            smlayer_loadSound(225, flag, phase);
//                            smlayer_loadSound(226, flag, phase);
//                            smlayer_loadSound(228, flag, phase);
//                            smlayer_loadSound(229, flag, phase);
//                            smlayer_loadSound(230, flag, phase);
//                            smlayer_loadSound(232, flag, phase);
//                            smlayer_loadSound(220, flag, phase);
//                            smlayer_loadSound(221, flag, phase);
//                            smlayer_loadSound(222, flag, phase);
//                            smlayer_loadSound(90, flag, phase);
//                            smlayer_loadCostume(15, phase);
//                            smlayer_loadCostume(16, phase);
//                            smlayer_loadCostume(17, phase);
//                            smlayer_loadCostume(43, phase);
//                            smlayer_loadCostume(47, phase);
//                            break;
//                        case EN_VULTF1:
//                            smlayer_loadSound(282, flag, phase);
//                            smlayer_loadSound(283, flag, phase);
//                            smlayer_loadSound(284, flag, phase);
//                            smlayer_loadSound(285, flag, phase);
//                            smlayer_loadSound(286, flag, phase);
//                            smlayer_loadSound(287, flag, phase);
//                            smlayer_loadSound(279, flag, phase);
//                            smlayer_loadSound(280, flag, phase);
//                            smlayer_loadSound(281, flag, phase);
//                            smlayer_loadSound(277, flag, phase);
//                            smlayer_loadSound(288, flag, phase);
//                            smlayer_loadSound(278, flag, phase);
//                            smlayer_loadSound(91, flag, phase);
//                            smlayer_loadCostume(29, phase);
//                            smlayer_loadCostume(33, phase);
//                            smlayer_loadCostume(32, phase);
//                            smlayer_loadCostume(37, phase);
//                            break;
//                        case EN_VULTM1:
//                            smlayer_loadSound(160, flag, phase);
//                            smlayer_loadSound(161, flag, phase);
//                            smlayer_loadSound(174, flag, phase);
//                            smlayer_loadSound(167, flag, phase);
//                            smlayer_loadSound(163, flag, phase);
//                            smlayer_loadSound(164, flag, phase);
//                            smlayer_loadSound(170, flag, phase);
//                            smlayer_loadSound(166, flag, phase);
//                            smlayer_loadSound(175, flag, phase);
//                            smlayer_loadSound(162, flag, phase);
//                            smlayer_loadSound(91, flag, phase);
//                            smlayer_loadCostume(30, phase);
//                            smlayer_loadCostume(33, phase);
//                            smlayer_loadCostume(32, phase);
//                            smlayer_loadCostume(36, phase);
//                            break;
//                        case EN_VULTF2:
//                            smlayer_loadSound(263, flag, phase);
//                            smlayer_loadSound(264, flag, phase);
//                            smlayer_loadSound(265, flag, phase);
//                            smlayer_loadSound(266, flag, phase);
//                            smlayer_loadSound(267, flag, phase);
//                            smlayer_loadSound(268, flag, phase);
//                            smlayer_loadSound(270, flag, phase);
//                            smlayer_loadSound(271, flag, phase);
//                            smlayer_loadSound(275, flag, phase);
//                            smlayer_loadSound(276, flag, phase);
//                            smlayer_loadSound(261, flag, phase);
//                            smlayer_loadSound(262, flag, phase);
//                            smlayer_loadSound(263, flag, phase);
//                            smlayer_loadSound(274, flag, phase);
//                            smlayer_loadSound(91, flag, phase);
//                            smlayer_loadCostume(31, phase);
//                            smlayer_loadCostume(33, phase);
//                            smlayer_loadCostume(32, phase);
//                            smlayer_loadCostume(35, phase);
//                            smlayer_loadCostume(46, phase);
//                            break;
//                        case EN_VULTM2:
//                            smlayer_loadSound(179, flag, phase);
//                            smlayer_loadSound(183, flag, phase);
//                            smlayer_loadSound(184, flag, phase);
//                            smlayer_loadSound(186, flag, phase);
//                            smlayer_loadSound(191, flag, phase);
//                            smlayer_loadSound(192, flag, phase);
//                            smlayer_loadSound(180, flag, phase);
//                            smlayer_loadSound(101, flag, phase);
//                            smlayer_loadSound(289, flag, phase);
//                            smlayer_loadSound(177, flag, phase);
//                            smlayer_loadSound(178, flag, phase);
//                            smlayer_loadSound(290, flag, phase);
//                            smlayer_loadSound(102, flag, phase);
//                            smlayer_loadSound(91, flag, phase);
//                            smlayer_loadCostume(34, phase);
//                            smlayer_loadCostume(33, phase);
//                            smlayer_loadCostume(32, phase);
//                            smlayer_loadCostume(44, phase);
//                            smlayer_loadCostume(45, phase);
//                            break;
//                        case EN_CAVEFISH:
//                            smlayer_loadSound(291, flag, phase);
//                            smlayer_loadSound(100, flag, phase);
//                            smlayer_loadSound(92, flag, phase);
//                            smlayer_loadCostume(39, phase);
//                            smlayer_loadCostume(40, phase);
//                            smlayer_loadCostume(41, phase);
//                            break;
//                        default:
//                            retvalue = 0;
//                            break;
//                    }
//                    smlayer_loadSound(64, flag, phase);
//                    smlayer_loadSound(65, flag, phase);
//                    smlayer_loadSound(66, flag, phase);
//                    smlayer_loadSound(67, flag, phase);
//                    smlayer_loadSound(68, flag, phase);
//                    smlayer_loadSound(69, flag, phase);
//                    smlayer_loadSound(70, flag, phase);
//                    smlayer_loadSound(71, flag, phase);
//                    smlayer_loadSound(72, flag, phase);
//                    smlayer_loadSound(73, flag, phase);
//                    smlayer_loadSound(74, flag, phase);
//                    smlayer_loadSound(75, flag, phase);
//                    smlayer_loadSound(76, flag, phase);
//                    smlayer_loadSound(77, flag, phase);
//                    smlayer_loadSound(78, flag, phase);
//                    smlayer_loadSound(79, flag, phase);
//                    smlayer_loadSound(80, flag, phase);
//                    smlayer_loadSound(81, flag, phase);
//                    smlayer_loadSound(82, flag, phase);
//                    smlayer_loadSound(83, flag, phase);
//                    smlayer_loadSound(84, flag, phase);
//                    smlayer_loadSound(85, flag, phase);
//                    smlayer_loadSound(86, flag, phase);
//                    smlayer_loadSound(87, flag, phase);
//                    smlayer_loadSound(62, flag, phase);
//                    smlayer_loadSound(63, flag, phase);
//                    smlayer_loadSound(60, flag, phase);
//                    smlayer_loadSound(61, flag, phase);
//                    smlayer_loadSound(315, flag, phase);
//                    smlayer_loadSound(316, flag, phase);
//                    smlayer_loadSound(317, flag, phase);
//                    smlayer_loadSound(98, flag, phase);
//                    smlayer_loadSound(318, flag, phase);
//                    smlayer_loadSound(96, flag, phase);
//                    smlayer_loadSound(97, flag, phase);
//                    smlayer_loadSound(95, flag, phase);
//                    smlayer_loadSound(89, flag, phase);
//                    smlayer_loadCostume(12, phase);
//                    smlayer_loadCostume(13, phase);
//                    smlayer_loadCostume(14, phase);
//                    smlayer_loadCostume(18, phase);
//                    smlayer_loadCostume(22, phase);
//                    smlayer_loadCostume(19, phase);
//                    smlayer_loadCostume(38, phase);
//                    smlayer_loadCostume(20, phase);
//                    smlayer_loadCostume(21, phase);
//                    smlayer_loadCostume(23, phase);
//                    smlayer_loadCostume(24, phase);
//                    smlayer_loadCostume(25, phase);
                    break;
                case 21:
                case 24:
                case 25:
//                    smlayer_loadSound(223, flag, phase);
//                    smlayer_loadSound(224, flag, phase);
//                    smlayer_loadSound(225, flag, phase);
//                    smlayer_loadSound(226, flag, phase);
//                    smlayer_loadSound(228, flag, phase);
//                    smlayer_loadSound(229, flag, phase);
//                    smlayer_loadSound(230, flag, phase);
//                    smlayer_loadSound(232, flag, phase);
//                    smlayer_loadSound(90, flag, phase);
//                    smlayer_loadCostume(15, phase);
//                    smlayer_loadCostume(16, phase);
//                    smlayer_loadCostume(17, phase);
//                    smlayer_loadCostume(43, phase);
//                    smlayer_loadSound(62, flag, phase);
//                    smlayer_loadSound(63, flag, phase);
//                    smlayer_loadSound(60, flag, phase);
//                    smlayer_loadSound(61, flag, phase);
//                    smlayer_loadSound(315, flag, phase);
//                    smlayer_loadSound(316, flag, phase);
//                    smlayer_loadSound(317, flag, phase);
//                    smlayer_loadSound(98, flag, phase);
//                    smlayer_loadSound(318, flag, phase);
//                    smlayer_loadSound(96, flag, phase);
//                    smlayer_loadSound(97, flag, phase);
//                    smlayer_loadSound(95, flag, phase);
//                    smlayer_loadSound(89, flag, phase);
//                    smlayer_loadCostume(12, phase);
//                    smlayer_loadCostume(13, phase);
//                    smlayer_loadCostume(14, phase);
//                    smlayer_loadCostume(18, phase);
//                    smlayer_loadCostume(22, phase);
                    break;
                case 17:
//                    smlayer_loadSound(88, flag, phase);
//                    smlayer_loadSound(94, flag, phase);
                    break;
                case 2:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 14:
                case 15:
                case 16:
                case 18:
                case 19:
                case 20:
                case 22:
                case 23:
                    break;
                default:
                    retvalue = 0;
                    break;
            }
            if (phase == 1) {
                _sceneData1Loaded = 1;
            }
            return retvalue;
        }

        void SetSceneCostumes(int sceneId) 
        {
            Debug.WriteLine( "Insane::SetSceneCostumes({0})", sceneId);

            switch (sceneId) {
                case 1:
//                    if ((_vm._game.features & GF_DEMO) && (_vm._game.platform == Common::kPlatformDOS))
//                        smlayer_setActorCostume(0, 2, readArray(9));
//                    else
//                        smlayer_setActorCostume(0, 2, readArray(10));
//                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1 + 190, _smlayer_room2);
//                    smlayer_setFluPalette(_smush_roadrashRip, 0);
//                    setupValues();
                    return;
                case 17:
//                    smlayer_setFluPalette(_smush_goglpaltRip, 0);
//                    setupValues();
                    return;
                case 2:
//                    if ((_vm._game.features & GF_DEMO) && (_vm._game.platform == Common::kPlatformDOS))
//                        smlayer_setActorCostume(0, 2, readArray(9));
//                    else
//                        smlayer_setActorCostume(0, 2, readArray(10));
//                    setupValues();
                    return;
                case 13:
//                    setEnemyCostumes();
//                    smlayer_setFluPalette(_smush_roadrashRip, 0);
                    return;
                case 21:
//                    _currEnemy = EN_ROTT3; //PATCH
//                    setEnemyCostumes();
//                    _actor[1].y = 200;
//                    smlayer_setFluPalette(_smush_roadrashRip, 0);
                    return;
                case 4:
                case 5:
                case 6:
//                    if ((_vm._game.features & GF_DEMO) && (_vm._game.platform == Common::kPlatformDOS))
//                        smlayer_setActorCostume(0, 2, readArray(10));
//                    else
//                        smlayer_setActorCostume(0, 2, readArray(11));
//                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1+190, _smlayer_room2);
//                    setupValues();
                    return;
                case 7:
                case 8:
                    WriteArray(4, 0);
                    return;
            }
        }
    }
}

