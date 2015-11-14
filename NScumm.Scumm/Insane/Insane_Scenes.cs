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
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Insane
{
    partial class Insane
    {
        public void ProcPreRendering()
        {
            _smush_isSanFileSetup = false; // FIXME: This shouldn't be here

            SwitchSceneIfNeeded();

            if (_sceneData1Loaded != 0)
            {
                _val115_ = true;
                if (_keyboardDisable == 0)
                {
                    smush_changeState(1);
                    _keyboardDisable = 1;
                }
            }
            else
            {
                _val115_ = false;
                if (_keyboardDisable != 0)
                {
                    smush_changeState(0);
                    _keyboardDisable = 0;
                }
            }
        }

        public void SetSmushParams(int speed)
        {
            _speed = speed;
        }

        int InitScene(int sceneId)
        {
            Debug.WriteLine("InitScene({0})", sceneId);

            if (_needSceneSwitch)
                return 1;

            StopSceneSounds(_currSceneId); // do it for previous scene
            LoadSceneData(sceneId, 0, 1);
            if (LoadSceneData(sceneId, 0, 2))
            {
                SetSceneCostumes(sceneId);
                _sceneData2Loaded = 0;
                _sceneData1Loaded = 0;
            }
            else
                _sceneData2Loaded = 1;

            _currSceneId = (byte)sceneId;

            return 1;
        }

        public void RunScene(uint arraynum)
        {
            _insaneIsRunning = true;
            _player = _vm.SmushPlayer;
            _player.Insanity(true);

            _numberArray = arraynum;

            // zeroValues1()
            _objArray2Idx = 0;
            _objArray2Idx2 = 0;
            // zeroValues2()
            _objArray1Idx2 = 0;
            // zeroValues3()
            _currScenePropIdx = 0;
            _currScenePropSubIdx = 0;
            _currTrsMsg = null;

            smush_warpMouse(160, 100, -1);
            PutActors();
            ReadState();

            Debug.WriteLine("INSANE Arg: {0}", ReadArray(0));

            switch (ReadArray(0))
            {
                case 1:
                    InitScene(1);
                    SetupValues();
                    if (_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm.Game.Platform == Common::kPlatformDOS)*/)
                        smlayer_setActorCostume(0, 2, ReadArray(9));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(10));
                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1 + 190, (byte)_smlayer_room2);
                    StartVideo("minedriv.san", 1, 32, 12, 0);
                    break;
                case 2:
                    SetupValues();
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
                        smlayer_setActorCostume(0, 2, ReadArray(10));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(11));
                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1 + 190, (byte)_smlayer_room2);

                    _mainRoadPos = (short)ReadArray(2);
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/) {
                        InitScene(5);
                        StartVideo("tovista.san", 1, 32, 12, 0);
                    } else if (_mainRoadPos == _posBrokenTruck) {
                        InitScene(5);
                        StartVideo("tovista2.san", 1, 32, 12, 0);
                    } else if (_mainRoadPos == _posBrokenCar) {
                        InitScene(5);
                        StartVideo("tovista2.san", 1, 32, 12, 0, _smush_tovista2Flu, 60);
                    } else {
                        InitScene(4);
                        StartVideo("tovista1.san", 1, 32, 12, 0);
                    }
                    break;
                case 3:
                    SetupValues();
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
                        smlayer_setActorCostume(0, 2, ReadArray(10));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(11));
                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1 + 190, (byte)_smlayer_room2);
                    _mainRoadPos = (short)ReadArray(2);
                    if (_mainRoadPos == _posBrokenTruck) {
                        InitScene(6);
                        StartVideo("toranch.san", 1, 32, 12, 0, _smush_toranchFlu, 300);
                    } else if (_mainRoadPos == _posBrokenCar) {
                        InitScene(6);
                        StartVideo("toranch.san", 1, 32, 12, 0, _smush_toranchFlu, 240);
                    } else {
                        InitScene(6);
                        StartVideo("toranch.san", 1, 32, 12, 0);
                    }
                    break;
                case 4:
                    _firstBattle = true;
                    _currEnemy = EN_ROTT1;
                    InitScene(13);
                    StartVideo("minefite.san", 1, 32, 12, 0);
                    break;
                case 5:
                    WriteArray(1, _val54d);
                    InitScene(24);
                    StartVideo("rottopen.san", 1, 32, 12, 0);
                    break;
                case 6:
                    InitScene(1);
                    SetupValues();
                    smlayer_setFluPalette(_smush_roadrashRip, 1);
                    smlayer_setActorCostume(0, 2, ReadArray(10));
                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1 + 190, (byte)_smlayer_room2);
                    StartVideo("minedriv.san", 1, 32, 12, 0, _smush_minedrivFlu, 420);
                    break;
                case 7:
                case 8:
                case 9:
                    break;
                case 10:
                    InitScene(26);
                    WriteArray(1, _val54d);
                    StartVideo("credits.san", 1, 32, 12, 0);
                    break;
                default:
                    throw new NotSupportedException(string.Format("Unknown FT_INSANE mode {0}", ReadArray(0)));
            }

            PutActors();
            _enemy[EN_ROTT3].maxdamage = 120;

            _insaneIsRunning = false;
            _player.Insanity(false);

            if (!(_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)) {
                WriteArray(50, _actor[0].inventory[INV_CHAIN]?1:0);
                WriteArray(51, _actor[0].inventory[INV_CHAINSAW]?1:0);
                WriteArray(52, _actor[0].inventory[INV_MACE]?1:0);
                WriteArray(53, _actor[0].inventory[INV_2X4]?1:0);
                WriteArray(54, _actor[0].inventory[INV_WRENCH]?1:0);
                WriteArray(55, _actor[0].inventory[INV_DUST]?1:0);
                WriteArray(56, _enemy[EN_CAVEFISH].isEmpty);
                WriteArray(337, _enemy[EN_TORQUE].occurences);
                WriteArray(329, _enemy[EN_ROTT1].occurences);
                WriteArray(330, _enemy[EN_ROTT2].occurences);
                WriteArray(331, _enemy[EN_ROTT3].occurences);
                WriteArray(332, _enemy[EN_VULTF1].occurences);
                WriteArray(333, _enemy[EN_VULTM1].occurences);
                WriteArray(334, _enemy[EN_VULTF2].occurences);
                WriteArray(335, _enemy[EN_VULTM2].occurences);
                WriteArray(336, _enemy[EN_CAVEFISH].occurences);
                WriteArray(339, _enemy[EN_VULTF2].isEmpty);
                WriteArray(340, _enemy[EN_VULTM2].isEmpty);
            }

            _vm.Sound.StopAllSounds(); // IMUSE_StopAllSounds();
        }

        public void ProcPostRendering(byte[] renderBitmap, int codecparam, int setupsan12,
                                      int setupsan13, int curFrame, int maxFrame)
        {
            int tmpSnd;
            bool needMore = false;

            Debug.WriteLine("procPostRendering");

            if (_keyboardDisable == 0)
            {
                switch (_currSceneId)
                {
                    case 12:
                        PostCase11(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        break;
                    case 1:
                        PostCase0(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        if (!smlayer_isSoundRunning(88))
                            smlayer_startSfx(88);
                        smlayer_soundSetPan(88, ((_actor[0].x + 160) >> 2) + 64);
                        if (_tiresRustle)
                        {
                            if (!smlayer_isSoundRunning(87))
                                smlayer_startSfx(87);
                        }
                        else
                        {
                            smlayer_stopSound(87);
                        }
                        break;
                    case 18:
                    case 19:
                        PostCase17(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        smlayer_stopSound(95);
                        smlayer_stopSound(87);
                        smlayer_stopSound(88);
                        if (!smlayer_isSoundRunning(88))
                            smlayer_startSfx(88);
                        break;
                    case 17:
                        PostCase16(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        if (!smlayer_isSoundRunning(88))
                            smlayer_startSfx(88);
                        break;
                    case 2:
                        PostCase1(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        break;
                    case 3:
                        PostCase2(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        needMore = true;
                        if (!smlayer_isSoundRunning(89))
                        {
                            smlayer_startSfx(89);
                            smlayer_soundSetPriority(89, 100);
                        }
                        tmpSnd = _enemy[_currEnemy].sound;
                        if (!smlayer_isSoundRunning(tmpSnd))
                        {
                            smlayer_startSfx(tmpSnd);
                            smlayer_soundSetPriority(tmpSnd, 100);
                        }
                        smlayer_soundSetPan(89, ((_actor[0].x + 160) >> 2) + 64);
                        smlayer_soundSetPan(tmpSnd, ((_actor[1].x + 160) >> 2) + 64);
                        if (!_tiresRustle)
                        {
                            smlayer_stopSound(87);
                        }
                        else
                        {
                            if (!smlayer_isSoundRunning(87))
                                smlayer_startSfx(87);
                        }
                        break;
                    case 21:
                        PostCase20(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        needMore = true;
                        if (!smlayer_isSoundRunning(89))
                        {
                            smlayer_startSfx(89);
                            smlayer_soundSetPriority(89, 100);
                        }
                        tmpSnd = _enemy[_currEnemy].sound;
                        if (!smlayer_isSoundRunning(tmpSnd))
                        {
                            smlayer_startSfx(tmpSnd);
                            smlayer_soundSetPriority(tmpSnd, 100);
                        }
                        smlayer_soundSetPan(89, ((_actor[0].x + 160) >> 2) + 64);
                        smlayer_soundSetPan(tmpSnd, ((_actor[1].x + 160) >> 2) + 64);
                        break;
                    case 4:
                    case 5:
                        PostCase3(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        if (!smlayer_isSoundRunning(88))
                            smlayer_startSfx(88);
                        smlayer_soundSetPan(88, ((_actor[0].x + 160) >> 2) + 64);
                        break;
                    case 6:
                        PostCase5(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        if (!smlayer_isSoundRunning(88))
                            smlayer_startSfx(88);
                        smlayer_soundSetPan(88, ((_actor[0].x + 160) >> 2) + 64);
                        break;
                    case 7:
                    case 8:
                        PostCase6(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        break;
                    case 9:
                    case 23:
                        PostCase8(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        break;
                    case 10:
                        PostCase9(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        break;
                    case 11:
                    case 20:
                    case 22:
                        PostCase10(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        break;
                    case 14:
                        PostCase23(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        break;
                    case 13:
                        PostCase12(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        needMore = true;
                        if (!smlayer_isSoundRunning(89))
                        {
                            smlayer_startSfx(89);
                            smlayer_soundSetPriority(89, 100);
                        }
                        tmpSnd = _enemy[_currEnemy].sound;
                        if (!smlayer_isSoundRunning(tmpSnd))
                        {
                            smlayer_startSfx(tmpSnd);
                            smlayer_soundSetPriority(tmpSnd, 100);
                        }
                        smlayer_soundSetPan(89, ((_actor[0].x + 160) >> 2) + 64);
                        smlayer_soundSetPan(tmpSnd, ((_actor[1].x + 160) >> 2) + 64);
                        break;
                    case 24:
                        if (!smlayer_isSoundRunning(90))
                        {
                            smlayer_startSfx(90);
                            smlayer_soundSetPriority(90, 100);
                        }
                        PostCase23(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        break;
                    case 15:
                    case 16:
                        PostCase14(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);
                        break;
                    case 25:
                    case 26:
                        break;
                }

                if (_currScenePropIdx != 0)
                    PostCaseAll(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);

                _actor[0].frame++;
                _actor[0].act[3].frame++;
                _actor[0].act[2].frame++;
                _actor[0].act[1].frame++;
                _actor[0].act[0].frame++;
                _actor[1].act[3].frame++;
                _actor[1].frame++;
                _actor[1].act[2].frame++;
                _actor[1].act[1].frame++;
                _actor[1].act[0].frame++;
            }

            if (!_val115_)
            {
                smlayer_overrideDrawActorAt(renderBitmap, renderBitmap[2], renderBitmap[3]);
                _isBenCut = false;
            }

            if (_isBenCut)
                smlayer_drawSomething(renderBitmap, codecparam, 89, 56, 1, _smush_bencutNut, 0, 0, 0);

            if (_keyboardDisable == 0)
                _vm.ProcessActors();

            if (needMore)
                PostCaseMore(renderBitmap, codecparam, setupsan12, setupsan13, curFrame, maxFrame);

            _tiresRustle = false;
        }

        void PostCaseMore(byte[] renderBitmap, int codecparam, int setupsan12, int setupsan13, int curFrame, int maxFrame)
        {
            if (_actor[0].weapon <= 7)
            {
                smlayer_drawSomething(renderBitmap, codecparam, 5, 160, 1, _smush_iconsNut,
                    _actor[0].weapon + 11, 0, 0);
            }
        }

        void PostCase0(byte[] renderBitmap, int codecparam, int setupsan12,
                       int setupsan13, int curFrame, int maxFrame)
        {
            TurnBen(true);

            if (curFrame == 0 || curFrame == 420)
                smlayer_setFluPalette(_smush_roadrashRip, 0);

            if (curFrame >= maxFrame)
                smush_rewindCurrentSan(1088, -1, -1);

            _roadBumps = false;
            _roadBranch = false;
            _roadStop = false;
            _benHasGoggles = false;
            _mineCaveIsNear = false;
            _continueFrame1 = curFrame;
        }

        void PostCase1(byte[] renderBitmap, int codecparam, int setupsan12,
                       int setupsan13, int curFrame, int maxFrame)
        {

            if ((curFrame >= maxFrame) && !_needSceneSwitch)
            {
                var flu = _fluConf[14 + _iactSceneId2];
                if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                    QueueSceneSwitch(4, null, "tovista.san", 64, 0, 0, 0);
                else
                    QueueSceneSwitch(flu.sceneId, flu.fluPtr, flu.filename, 64, 0,
                        flu.startFrame, flu.numFrames);
            }
            _roadBranch = false;
            _roadStop = false;
        }

        void PostCase2(byte[] renderBitmap, int codecparam, int setupsan12,
                       int setupsan13, int curFrame, int maxFrame)
        {
            TurnBen(_battleScene);
            TurnEnemy(true);

            if (curFrame == 0)
                smlayer_setFluPalette(_smush_roadrashRip, 0);

            if (curFrame >= maxFrame)
                smush_rewindCurrentSan(1088, -1, -1);

            _roadBumps = false;
            _roadBranch = false;
            _roadStop = false;
            _continueFrame = curFrame;
        }

        void PostCase3(byte[] renderBitmap, int codecparam, int setupsan12,
                       int setupsan13, int curFrame, int maxFrame)
        {
            TurnBen(true);

            if (_actor[0].x >= 158 && _actor[0].x <= 168)
            {
                if (!smlayer_isSoundRunning(86))
                    smlayer_startSfx(86);
            }
            else
            {
                if (smlayer_isSoundRunning(86))
                    smlayer_stopSound(86);
            }

            if (curFrame >= maxFrame)
            {
                if (_currSceneId == 4)
                {
                    if (!_needSceneSwitch)
                    {
                        if (ReadArray(6) != 0)
                        {
                            if (ReadArray(4) != 0)
                                QueueSceneSwitch(14, null, "hitdust2.san", 64, 0, 0, 0);
                            else
                                QueueSceneSwitch(14, null, "hitdust4.san", 64, 0, 0, 0);
                        }
                        else
                        {
                            if (ReadArray(4) != 0)
                                QueueSceneSwitch(14, null, "hitdust1.san", 64, 0, 0, 0);
                            else
                                QueueSceneSwitch(14, null, "hitdust3.san", 64, 0, 0, 0);
                        }
                    }
                }
                else
                {
                    if (ReadArray(4) != 0)
                    {
                        if (!_needSceneSwitch)
                            QueueSceneSwitch(15, null, "vistthru.san", 64, 0, 0, 0);
                    }
                    else
                    {
                        WriteArray(1, _posVista);
                        smush_setToFinish();
                    }
                }
            }

            _carIsBroken = false;
            _roadStop = false;
            _roadBranch = false;
            _iactSceneId = 0;
        }

        void PostCase5(byte[] renderBitmap, int codecparam, int setupsan12,
                       int setupsan13, int curFrame, int maxFrame)
        {
            TurnBen(true);

            if (_actor[0].x >= 158 && _actor[0].x <= 168)
            {
                if (!smlayer_isSoundRunning(86))
                    smlayer_startSfx(86);
            }
            else
            {
                if (smlayer_isSoundRunning(86))
                    smlayer_stopSound(86);
            }

            if (curFrame >= maxFrame)
            {
                if (ReadArray(4) != 0)
                {
                    if (!_needSceneSwitch)
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
            }

            _carIsBroken = false;
            _roadStop = false;
            _roadBranch = false;
            _iactSceneId = 0;
        }

        void PostCase6(byte[] renderBitmap, int codecparam, int setupsan12,
                       int setupsan13, int curFrame, int maxFrame)
        {


            if ((curFrame >= maxFrame) && !_needSceneSwitch)
            {
                FluConf flu;
                if (_currSceneId == 8)
                    flu = _fluConf[7 + _iactSceneId2];
                else
                    flu = _fluConf[0 + _iactSceneId2];

                if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                    QueueSceneSwitch(1, null, "minedriv.san", 64, 0, 0, 0);
                else
                    QueueSceneSwitch(flu.sceneId, flu.fluPtr, flu.filename, 64, 0,
                        flu.startFrame, flu.numFrames);
            }
            _roadBranch = false;
            _roadStop = false;
        }

        void PostCase8(byte[] renderBitmap, int codecparam, int setupsan12,
                       int setupsan13, int curFrame, int maxFrame)
        {
            if (curFrame >= maxFrame && !_needSceneSwitch)
            {
                _actor[0].damage = 0;

                if (_firstBattle)
                {
                    QueueSceneSwitch(13, _smush_minefiteFlu, "minefite.san", 64, 0,
                        _continueFrame, 1300);
                }
                else
                {
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm->_game.platform == Common::kPlatformDOS)*/)
                    {
                        QueueSceneSwitch(1, null, "minedriv.san", 64, 0, 0, 0);
                    }
                    else
                    {
                        if (_currSceneId == 23)
                        {
                            QueueSceneSwitch(21, null, "rottfite.san", 64, 0, 0, 0);
                        }
                        else
                        {
                            QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0,
                                _continueFrame, 1300);
                        }
                    }
                }
            }

            _roadBranch = false;
            _roadStop = false;
        }

        void PostCase9(byte[] renderBitmap, int codecparam, int setupsan12,
                       int setupsan13, int curFrame, int maxFrame)
        {
            if (curFrame >= maxFrame && !_needSceneSwitch)
            {
                _actor[0].damage = 0;
                QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0,
                    _continueFrame1, 1300);
            }
            _roadBranch = false;
            _roadStop = false;
        }

        void PostCase10(byte[] renderBitmap, int codecparam, int setupsan12,
                        int setupsan13, int curFrame, int maxFrame)
        {
            if (curFrame >= maxFrame && !_needSceneSwitch)
            {
                _actor[0].damage = 0;

                switch (_currSceneId)
                {
                    case 20:
                        WriteArray(8, 1);
                        QueueSceneSwitch(12, null, "liftgog.san", 0, 0, 0, 0);
                        break;
                    case 22:
                        WriteArray(1, _val54d);
                        smush_setToFinish();
                        break;
                    default:
                        if (_actor[0].inventory[_enemy[_currEnemy].weapon])
                        {
                            QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0,
                                _continueFrame, 1300);
                            break;
                        }

                        switch (_enemy[_currEnemy].weapon)
                        {
                            case INV_CHAIN:
                                _actor[0].inventory[INV_CHAIN] = true;
                                QueueSceneSwitch(12, null, "liftchay.san", 0, 0, 0, 0);
                                break;
                            case INV_CHAINSAW:
                                _actor[0].inventory[INV_CHAINSAW] = true;
                                QueueSceneSwitch(12, null, "liftsaw.san", 0, 0, 0, 0);
                                break;
                            case INV_MACE:
                                _actor[0].inventory[INV_MACE] = true;
                                QueueSceneSwitch(12, null, "liftmace.san", 0, 0, 0, 0);
                                break;
                            case INV_2X4:
                                _actor[0].inventory[INV_2X4] = true;
                                QueueSceneSwitch(12, null, "liftbord.san", 0, 0, 0, 0);
                                break;
                            default:
                                QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0,
                                    _continueFrame, 1300);
                                break;
                        }
                        break;
                }
            }

            _roadBranch = false;
            _roadStop = false;
        }

        void PostCase11(byte[] renderBitmap, int codecparam, int setupsan12,
                        int setupsan13, int curFrame, int maxFrame)
        {
            if (curFrame >= maxFrame && !_needSceneSwitch)
            {
                if (_firstBattle)
                {
                    smush_setToFinish();
                }
                else
                {
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
                        QueueSceneSwitch(1, null, "minedriv.san", 64, 0, 0, 0);
                    else
                        QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0,
                            _continueFrame, 1300);
                }
            }
            _roadBranch = false;
            _roadStop = false;
        }

        void PostCase12(byte[] renderBitmap, int codecparam, int setupsan12,
                        int setupsan13, int curFrame, int maxFrame)
        {
            if (_actor[1].y <= 200)
            {
                InitScene(3);
                _actor[1].y = 200;

                switch (_currEnemy)
                {
                    case EN_ROTT2:
                        TurnBen(true);

                        if (_enemy[EN_ROTT2].occurences <= 1)
                            PrepareScenePropScene(32, false, true);
                        else
                            PrepareScenePropScene(33, false, true);
                        break;
                    case EN_ROTT3:
                        TurnBen(true);

                        if (_enemy[EN_ROTT3].occurences <= 1)
                            PrepareScenePropScene(25, false, true);
                        break;
                    case EN_VULTF1:
                        TurnBen(true);

                        if (_enemy[EN_VULTF1].occurences <= 1)
                            PrepareScenePropScene(2, false, true);
                        break;
                    case EN_VULTF2:
                        TurnBen(true);

                        if (_enemy[EN_VULTF2].occurences <= 1)
                            PrepareScenePropScene(9, false, true);
                        else
                            PrepareScenePropScene(16, false, true);
                        break;
                    case EN_VULTM2:
                        if (_enemy[EN_VULTM2].occurences <= 1)
                        {
                            TurnBen(false);
                            PrepareScenePropScene(18, false, true);
                            _battleScene = false;
                        }
                        else
                            TurnBen(true);
                        break;
                    case EN_TORQUE:
                        TurnBen(false);
                        WriteArray(1, _posFatherTorque);
                        smush_setToFinish();
                        break;
                    case EN_ROTT1:
                    case EN_VULTM1:
                    case EN_CAVEFISH:
                    default:
                        TurnBen(true);
                        break;
                }
            }
            else
            {
                switch (_currEnemy)
                {
                    case EN_VULTM2:
                        if (_enemy[EN_VULTM2].occurences <= 1)
                            TurnBen(false);
                        else
                            TurnBen(true);
                        break;
                    case EN_TORQUE:
                        TurnBen(false);
                        if (_actor[1].y == 300)
                            PrepareScenePropScene(57, true, false);
                        break;
                    default:
                        TurnBen(true);
                        break;
                }
                _actor[1].y -= (_actor[1].y - 200) / 20 + 1;
            }

            TurnEnemy(false);

            if (curFrame == 0)
                smlayer_setFluPalette(_smush_roadrashRip, 0);

            if (curFrame >= maxFrame)
                smush_rewindCurrentSan(1088, -1, -1);

            _roadBranch = false;
            _roadStop = false;
            _continueFrame = curFrame;
        }

        void PostCase14(byte[] renderBitmap, int codecparam, int setupsan12,
                        int setupsan13, int curFrame, int maxFrame)
        {
            if (curFrame >= maxFrame)
            {
                if (_currSceneId == 16)
                {
                    WriteArray(4, 0);
                    WriteArray(5, 1);
                    WriteArray(1, _posBrokenCar);
                    WriteArray(3, _posBrokenTruck);
                    smush_setToFinish();
                }
                else
                {
                    switch (_tempSceneId)
                    {
                        case 5:
                            QueueSceneSwitch(6, null, "toranch.san", 64, 0, 0, 530);
                            break;
                        case 6:
                            QueueSceneSwitch(4, null, "tovista1.san", 64, 0, 0, 230);
                            break;
                    }
                }
            }

            _roadBranch = false;
            _roadStop = false;
        }

        void PostCase16(byte[] renderBitmap, int codecparam, int setupsan12,
                        int setupsan13, int curFrame, int maxFrame)
        {
            int tmp;

            TurnBen(true);
            var buf = string.Format("^f01{0:2O}", curFrame & 0x3f);
            smlayer_showStatusMsg(-1, renderBitmap, codecparam, 180, 168, 1, 2, 0, "{0}", buf);
            tmp = 400 - curFrame;

            if (tmp < 0)
                tmp += 1300;

            buf = string.Format("^f01{0:4D}", tmp);
            smlayer_showStatusMsg(-1, renderBitmap, codecparam, 202, 168, 1, 2, 0, "{0}", buf);

            buf = string.Format("^f01{0:2O}", curFrame & 0xff);
            smlayer_showStatusMsg(-1, renderBitmap, codecparam, 240, 168, 1, 2, 0, "{0}", buf);
            smlayer_showStatusMsg(-1, renderBitmap, codecparam, 170, 43, 1, 2, 0, "{0}", buf);

            smlayer_drawSomething(renderBitmap, codecparam, 0, 0, 1, _smush_bensgoggNut, 0, 0, 0);

            if (!_objectDetected)
                smlayer_drawSomething(renderBitmap, codecparam, 24, 170, 1,
                    _smush_iconsNut, 23, 0, 0);

            if (curFrame==0)
                smlayer_setFluPalette(_smush_goglpaltRip, 0);

            if (curFrame >= maxFrame)
            {
                smush_rewindCurrentSan(1088, -1, -1);
                smlayer_setFluPalette(_smush_goglpaltRip, 0);
            }
            _roadBumps = false;
            _mineCaveIsNear = false;
            _roadBranch = false;
            _roadStop = false;
            _objectDetected = false;
            _counter1++;
            _continueFrame1 = curFrame;
            if (_counter1 >= 10)
                _counter1 = 0;
        }

        void PostCase17(byte[] renderBitmap, int codecparam, int setupsan12,
                        int setupsan13, int curFrame, int maxFrame)
        {
            if (curFrame >= maxFrame && !_needSceneSwitch)
            {
                if (_currSceneId == 18)
                {
                    QueueSceneSwitch(17, _smush_minedrivFlu, "minedriv.san", 64, 0,
                        _continueFrame1, 1300);
                    WriteArray(9, 1);
                }
                else
                {
                    QueueSceneSwitch(1, _smush_minedrivFlu, "minedriv.san", 64, 0,
                        _continueFrame1, 1300);
                    WriteArray(9, 0);
                }
            }
            _roadBranch = false;
            _roadStop = false;
        }

        void PostCase20(byte[] renderBitmap, int codecparam, int setupsan12,
                        int setupsan13, int curFrame, int maxFrame)
        {
            TurnBen(true);
            TurnEnemy(true);

            if (curFrame >= maxFrame)
                smush_rewindCurrentSan(1088, -1, -1);

            _roadBumps = false;
            _roadBranch = false;
            _roadStop = false;
            _continueFrame = curFrame;
        }

        void PostCase23(byte[] renderBitmap, int codecparam, int setupsan12,
                        int setupsan13, int curFrame, int maxFrame)
        {
            if (curFrame >= maxFrame)
            {
                if (_currSceneId == 24)
                {
                    QueueSceneSwitch(21, null, "rottfite.san", 64, 0, 0, 0);
                }
                else
                {
                    if (ReadArray(6) != 0 && ReadArray(4) != 0)
                        QueueSceneSwitch(16, null, "limocrsh.san", 64, 0, 0, 0);
                    else
                        QueueSceneSwitch(5, null, "tovista2.san", 64, 0, 0, 290);
                }
            }
            _roadBranch = false;
            _roadStop = false;
        }

        void PostCaseAll(byte[] renderBitmap, int codecparam, int setupsan12, int setupsan13, int curFrame, int maxFrame)
        {
            var tsceneProp = _sceneProp[_currScenePropIdx + _currScenePropSubIdx];
            if (tsceneProp.actor != -1)
            {
                if (_actor[tsceneProp.actor].field_54 != 0)
                {
                    tsceneProp.counter++;
                    /*if (_actor[tsceneProp.actor].runningSound == 0 /*|| ConfMan.getBool("subtitles"))*/
                    {
                        if (_actor[tsceneProp.actor].act[3].state == 72 && _currTrsMsg != null)
                        {
                            _player.SetPaletteValue(0, tsceneProp.r, tsceneProp.g, tsceneProp.b);
                            _player.SetPaletteValue(1, tsceneProp.r, tsceneProp.g, tsceneProp.b);
                            _player.SetPaletteValue(0, 0, 0, 0);
                            smlayer_showStatusMsg(-1, renderBitmap, codecparam, 160, 20, 1, 2, 5,
                                "^f00{0}", _currTrsMsg);
                        }
                    }
                }
                else
                {
                    _currScenePropSubIdx = tsceneProp.index;
                    if (_currScenePropSubIdx != 0 && _currScenePropIdx != 0)
                    {
                        tsceneProp = _sceneProp[_currScenePropIdx + _currScenePropSubIdx];
                        tsceneProp.counter = 0;
                        if (tsceneProp.trsId != 0)
                            _currTrsMsg = HandleTrsTag(tsceneProp.trsId);
                        else
                            _currTrsMsg = string.Empty;

                        if (tsceneProp.actor != -1)
                        {
                            _actor[tsceneProp.actor].field_54 = 1;
                            _actor[tsceneProp.actor].act[3].state = 117;
                            _actor[tsceneProp.actor].scenePropSubIdx = _currScenePropSubIdx;
                        }
                    }
                    else
                    {
                        _currScenePropIdx = 0;
                        _currTrsMsg = string.Empty;
                        _currScenePropSubIdx = 0;
                        _actor[0].defunct = false;
                        _actor[1].defunct = false;
                        _battleScene = true;
                    }
                }
            }
            _roadBranch = false;
            _roadStop = false;
            _continueFrame = curFrame;
        }

        void StopSceneSounds(int sceneId)
        {
            int flag = 0;

            Debug.WriteLine("StopSceneSounds({0})", sceneId);
            switch (sceneId)
            {
                case 1:
                    smlayer_stopSound(88);
                    smlayer_stopSound(86);
                    smlayer_stopSound(87);
                    flag = 1;
                    break;
                case 18:
                case 19:
                    smlayer_stopSound(88);
                    flag = 1;
                    break;
                case 17:
                    smlayer_stopSound(88);
                    smlayer_stopSound(94);
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
                    smlayer_stopSound(88);
                    smlayer_stopSound(86);
                    flag = 1;
                    break;
                case 24:
                    smlayer_stopSound(90);
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
            if (flag == 0)
                return;

            smlayer_setActorCostume(0, 2, 0);
            smlayer_setActorCostume(0, 0, 0);
            smlayer_setActorCostume(0, 1, 0);
            smlayer_setActorCostume(1, 2, 0);
            smlayer_setActorCostume(1, 0, 0);
            smlayer_setActorCostume(1, 1, 0);

            return;
        }

        void StopSceneSounds13()
        {
            if (_actor[0].runningSound != 0)
                smlayer_stopSound(_actor[0].runningSound);
            _actor[0].runningSound = 0;
            
            if (_actor[1].runningSound != 0)
                smlayer_stopSound(_actor[1].runningSound);
            _actor[1].runningSound = 0;
            
            if (_currScenePropIdx != 0)
                ShutCurrentScene();
            
            _currScenePropSubIdx = 0;
            _currTrsMsg = string.Empty;
            _actor[0].defunct = false;
            _actor[0].scenePropSubIdx = 0;
            _actor[0].field_54 = 0;
            _actor[1].defunct = false;
            _actor[1].scenePropSubIdx = 0;
            _actor[1].field_54 = 0;
            if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) /*&& (_vm._game.platform == Common::kPlatformDOS)*/)
            {
                smlayer_stopSound(59);
                smlayer_stopSound(63);
            }
            else
            {
                smlayer_stopSound(89);
                smlayer_stopSound(90);
                smlayer_stopSound(91);
                smlayer_stopSound(92);
                smlayer_stopSound(93);
                smlayer_stopSound(95);
                smlayer_stopSound(87);
            }
        }

        void ShutCurrentScene()
        {
            Debug.WriteLine("shutCurrentScene()");

            _currScenePropIdx = 0;
            _currTrsMsg = string.Empty;
            _currScenePropSubIdx = 0;
            _actor[1].scenePropSubIdx = 0;
            _actor[1].defunct = false;

            if (_actor[1].runningSound != 0)
            {
                smlayer_stopSound(_actor[1].runningSound);
                _actor[1].runningSound = 0;
            }

            _actor[0].scenePropSubIdx = 0;
            _actor[0].defunct = false;

            if (_actor[0].runningSound != 0)
            {
                smlayer_stopSound(_actor[0].runningSound);
                _actor[0].runningSound = 0;
            }

            _battleScene = true;
        }

        bool LoadSceneData(int scene, int flag, int phase)
        {
            // TODO: vs
//            if ((_vm.Game.Features.HasFlag(GameFeatures.Demo)) && (_vm._game.platform == Common::kPlatformDOS))
//                return 1;

            bool retvalue = true;

            Debug.WriteLine("Insane::loadSceneData({0}, {1}, {2})", scene, flag, phase);

            switch (scene)
            {
                case 1:
                    smlayer_loadSound(88, flag, phase);
                    smlayer_loadSound(86, flag, phase);
                    smlayer_loadSound(87, flag, phase);
                    smlayer_loadCostume(10, phase);
                    break;
                case 4:
                case 5:
                case 6:
                    smlayer_loadSound(88, flag, phase);
                    smlayer_loadCostume(11, phase);
                    break;
                case 3:
                case 13:
                    switch (_currEnemy)
                    {
                        case EN_TORQUE:
                            smlayer_loadSound(59, flag, phase);
                            smlayer_loadSound(93, flag, phase);
                            smlayer_loadCostume(57, phase);
                            smlayer_loadCostume(37, phase);
                            break;
                        case EN_ROTT1:
                            smlayer_loadSound(201, flag, phase);
                            smlayer_loadSound(194, flag, phase);
                            smlayer_loadSound(195, flag, phase);
                            smlayer_loadSound(199, flag, phase);
                            smlayer_loadSound(205, flag, phase);
                            smlayer_loadSound(212, flag, phase);
                            smlayer_loadSound(198, flag, phase);
                            smlayer_loadSound(203, flag, phase);
                            smlayer_loadSound(213, flag, phase);
                            smlayer_loadSound(215, flag, phase);
                            smlayer_loadSound(216, flag, phase);
                            smlayer_loadSound(217, flag, phase);
                            smlayer_loadSound(218, flag, phase);
                            smlayer_loadSound(90, flag, phase);
                            smlayer_loadCostume(26, phase);
                            smlayer_loadCostume(16, phase);
                            smlayer_loadCostume(17, phase);
                            smlayer_loadCostume(27, phase);
                            break;
                        case EN_ROTT2:
                            smlayer_loadSound(242, flag, phase);
                            smlayer_loadSound(244, flag, phase);
                            smlayer_loadSound(236, flag, phase);
                            smlayer_loadSound(238, flag, phase);
                            smlayer_loadSound(239, flag, phase);
                            smlayer_loadSound(240, flag, phase);
                            smlayer_loadSound(258, flag, phase);
                            smlayer_loadSound(259, flag, phase);
                            smlayer_loadSound(260, flag, phase);
                            smlayer_loadSound(243, flag, phase);
                            smlayer_loadSound(244, flag, phase);
                            smlayer_loadSound(245, flag, phase);
                            smlayer_loadSound(246, flag, phase);
                            smlayer_loadSound(233, flag, phase);
                            smlayer_loadSound(234, flag, phase);
                            smlayer_loadSound(241, flag, phase);
                            smlayer_loadSound(242, flag, phase);
                            smlayer_loadSound(90, flag, phase);
                            smlayer_loadCostume(28, phase);
                            smlayer_loadCostume(16, phase);
                            smlayer_loadCostume(17, phase);
                            smlayer_loadCostume(42, phase);
                            break;
                        case EN_ROTT3:
                            smlayer_loadSound(223, flag, phase);
                            smlayer_loadSound(224, flag, phase);
                            smlayer_loadSound(225, flag, phase);
                            smlayer_loadSound(226, flag, phase);
                            smlayer_loadSound(228, flag, phase);
                            smlayer_loadSound(229, flag, phase);
                            smlayer_loadSound(230, flag, phase);
                            smlayer_loadSound(232, flag, phase);
                            smlayer_loadSound(220, flag, phase);
                            smlayer_loadSound(221, flag, phase);
                            smlayer_loadSound(222, flag, phase);
                            smlayer_loadSound(90, flag, phase);
                            smlayer_loadCostume(15, phase);
                            smlayer_loadCostume(16, phase);
                            smlayer_loadCostume(17, phase);
                            smlayer_loadCostume(43, phase);
                            smlayer_loadCostume(47, phase);
                            break;
                        case EN_VULTF1:
                            smlayer_loadSound(282, flag, phase);
                            smlayer_loadSound(283, flag, phase);
                            smlayer_loadSound(284, flag, phase);
                            smlayer_loadSound(285, flag, phase);
                            smlayer_loadSound(286, flag, phase);
                            smlayer_loadSound(287, flag, phase);
                            smlayer_loadSound(279, flag, phase);
                            smlayer_loadSound(280, flag, phase);
                            smlayer_loadSound(281, flag, phase);
                            smlayer_loadSound(277, flag, phase);
                            smlayer_loadSound(288, flag, phase);
                            smlayer_loadSound(278, flag, phase);
                            smlayer_loadSound(91, flag, phase);
                            smlayer_loadCostume(29, phase);
                            smlayer_loadCostume(33, phase);
                            smlayer_loadCostume(32, phase);
                            smlayer_loadCostume(37, phase);
                            break;
                        case EN_VULTM1:
                            smlayer_loadSound(160, flag, phase);
                            smlayer_loadSound(161, flag, phase);
                            smlayer_loadSound(174, flag, phase);
                            smlayer_loadSound(167, flag, phase);
                            smlayer_loadSound(163, flag, phase);
                            smlayer_loadSound(164, flag, phase);
                            smlayer_loadSound(170, flag, phase);
                            smlayer_loadSound(166, flag, phase);
                            smlayer_loadSound(175, flag, phase);
                            smlayer_loadSound(162, flag, phase);
                            smlayer_loadSound(91, flag, phase);
                            smlayer_loadCostume(30, phase);
                            smlayer_loadCostume(33, phase);
                            smlayer_loadCostume(32, phase);
                            smlayer_loadCostume(36, phase);
                            break;
                        case EN_VULTF2:
                            smlayer_loadSound(263, flag, phase);
                            smlayer_loadSound(264, flag, phase);
                            smlayer_loadSound(265, flag, phase);
                            smlayer_loadSound(266, flag, phase);
                            smlayer_loadSound(267, flag, phase);
                            smlayer_loadSound(268, flag, phase);
                            smlayer_loadSound(270, flag, phase);
                            smlayer_loadSound(271, flag, phase);
                            smlayer_loadSound(275, flag, phase);
                            smlayer_loadSound(276, flag, phase);
                            smlayer_loadSound(261, flag, phase);
                            smlayer_loadSound(262, flag, phase);
                            smlayer_loadSound(263, flag, phase);
                            smlayer_loadSound(274, flag, phase);
                            smlayer_loadSound(91, flag, phase);
                            smlayer_loadCostume(31, phase);
                            smlayer_loadCostume(33, phase);
                            smlayer_loadCostume(32, phase);
                            smlayer_loadCostume(35, phase);
                            smlayer_loadCostume(46, phase);
                            break;
                        case EN_VULTM2:
                            smlayer_loadSound(179, flag, phase);
                            smlayer_loadSound(183, flag, phase);
                            smlayer_loadSound(184, flag, phase);
                            smlayer_loadSound(186, flag, phase);
                            smlayer_loadSound(191, flag, phase);
                            smlayer_loadSound(192, flag, phase);
                            smlayer_loadSound(180, flag, phase);
                            smlayer_loadSound(101, flag, phase);
                            smlayer_loadSound(289, flag, phase);
                            smlayer_loadSound(177, flag, phase);
                            smlayer_loadSound(178, flag, phase);
                            smlayer_loadSound(290, flag, phase);
                            smlayer_loadSound(102, flag, phase);
                            smlayer_loadSound(91, flag, phase);
                            smlayer_loadCostume(34, phase);
                            smlayer_loadCostume(33, phase);
                            smlayer_loadCostume(32, phase);
                            smlayer_loadCostume(44, phase);
                            smlayer_loadCostume(45, phase);
                            break;
                        case EN_CAVEFISH:
                            smlayer_loadSound(291, flag, phase);
                            smlayer_loadSound(100, flag, phase);
                            smlayer_loadSound(92, flag, phase);
                            smlayer_loadCostume(39, phase);
                            smlayer_loadCostume(40, phase);
                            smlayer_loadCostume(41, phase);
                            break;
                        default:
                            retvalue = false;
                            break;
                    }
                    smlayer_loadSound(64, flag, phase);
                    smlayer_loadSound(65, flag, phase);
                    smlayer_loadSound(66, flag, phase);
                    smlayer_loadSound(67, flag, phase);
                    smlayer_loadSound(68, flag, phase);
                    smlayer_loadSound(69, flag, phase);
                    smlayer_loadSound(70, flag, phase);
                    smlayer_loadSound(71, flag, phase);
                    smlayer_loadSound(72, flag, phase);
                    smlayer_loadSound(73, flag, phase);
                    smlayer_loadSound(74, flag, phase);
                    smlayer_loadSound(75, flag, phase);
                    smlayer_loadSound(76, flag, phase);
                    smlayer_loadSound(77, flag, phase);
                    smlayer_loadSound(78, flag, phase);
                    smlayer_loadSound(79, flag, phase);
                    smlayer_loadSound(80, flag, phase);
                    smlayer_loadSound(81, flag, phase);
                    smlayer_loadSound(82, flag, phase);
                    smlayer_loadSound(83, flag, phase);
                    smlayer_loadSound(84, flag, phase);
                    smlayer_loadSound(85, flag, phase);
                    smlayer_loadSound(86, flag, phase);
                    smlayer_loadSound(87, flag, phase);
                    smlayer_loadSound(62, flag, phase);
                    smlayer_loadSound(63, flag, phase);
                    smlayer_loadSound(60, flag, phase);
                    smlayer_loadSound(61, flag, phase);
                    smlayer_loadSound(315, flag, phase);
                    smlayer_loadSound(316, flag, phase);
                    smlayer_loadSound(317, flag, phase);
                    smlayer_loadSound(98, flag, phase);
                    smlayer_loadSound(318, flag, phase);
                    smlayer_loadSound(96, flag, phase);
                    smlayer_loadSound(97, flag, phase);
                    smlayer_loadSound(95, flag, phase);
                    smlayer_loadSound(89, flag, phase);
                    smlayer_loadCostume(12, phase);
                    smlayer_loadCostume(13, phase);
                    smlayer_loadCostume(14, phase);
                    smlayer_loadCostume(18, phase);
                    smlayer_loadCostume(22, phase);
                    smlayer_loadCostume(19, phase);
                    smlayer_loadCostume(38, phase);
                    smlayer_loadCostume(20, phase);
                    smlayer_loadCostume(21, phase);
                    smlayer_loadCostume(23, phase);
                    smlayer_loadCostume(24, phase);
                    smlayer_loadCostume(25, phase);
                    break;
                case 21:
                case 24:
                case 25:
                    smlayer_loadSound(223, flag, phase);
                    smlayer_loadSound(224, flag, phase);
                    smlayer_loadSound(225, flag, phase);
                    smlayer_loadSound(226, flag, phase);
                    smlayer_loadSound(228, flag, phase);
                    smlayer_loadSound(229, flag, phase);
                    smlayer_loadSound(230, flag, phase);
                    smlayer_loadSound(232, flag, phase);
                    smlayer_loadSound(90, flag, phase);
                    smlayer_loadCostume(15, phase);
                    smlayer_loadCostume(16, phase);
                    smlayer_loadCostume(17, phase);
                    smlayer_loadCostume(43, phase);
                    smlayer_loadSound(62, flag, phase);
                    smlayer_loadSound(63, flag, phase);
                    smlayer_loadSound(60, flag, phase);
                    smlayer_loadSound(61, flag, phase);
                    smlayer_loadSound(315, flag, phase);
                    smlayer_loadSound(316, flag, phase);
                    smlayer_loadSound(317, flag, phase);
                    smlayer_loadSound(98, flag, phase);
                    smlayer_loadSound(318, flag, phase);
                    smlayer_loadSound(96, flag, phase);
                    smlayer_loadSound(97, flag, phase);
                    smlayer_loadSound(95, flag, phase);
                    smlayer_loadSound(89, flag, phase);
                    smlayer_loadCostume(12, phase);
                    smlayer_loadCostume(13, phase);
                    smlayer_loadCostume(14, phase);
                    smlayer_loadCostume(18, phase);
                    smlayer_loadCostume(22, phase);
                    break;
                case 17:
                    smlayer_loadSound(88, flag, phase);
                    smlayer_loadSound(94, flag, phase);
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
                    retvalue = false;
                    break;
            }
            if (phase == 1)
            {
                _sceneData1Loaded = 1;
            }
            return retvalue;
        }

        void SetSceneCostumes(int sceneId)
        {
            Debug.WriteLine("Insane::SetSceneCostumes({0})", sceneId);

            switch (sceneId)
            {
                case 1:
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm.Game.Platform == Platform.Dos)*/))
                        smlayer_setActorCostume(0, 2, ReadArray(9));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(10));
                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1 + 190, (byte)_smlayer_room2);
                    smlayer_setFluPalette(_smush_roadrashRip, 0);
                    SetupValues();
                    return;
                case 17:
                    smlayer_setFluPalette(_smush_goglpaltRip, 0);
                    SetupValues();
                    return;
                case 2:
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm.Game.Platform == Platform.Dos)*/))
                        smlayer_setActorCostume(0, 2, ReadArray(9));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(10));
                    SetupValues();
                    return;
                case 13:
                    SetEnemyCostumes();
                    smlayer_setFluPalette(_smush_roadrashRip, 0);
                    return;
                case 21:
                    _currEnemy = EN_ROTT3; //PATCH
                    SetEnemyCostumes();
                    _actor[1].y = 200;
                    smlayer_setFluPalette(_smush_roadrashRip, 0);
                    return;
                case 4:
                case 5:
                case 6:
                    if ((_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm.Game.Platform == Platform.Dos)*/))
                        smlayer_setActorCostume(0, 2, ReadArray(10));
                    else
                        smlayer_setActorCostume(0, 2, ReadArray(11));
                    smlayer_putActor(0, 2, _actor[0].x, _actor[0].y1 + 190, (byte)_smlayer_room2);
                    SetupValues();
                    return;
                case 7:
                case 8:
                    WriteArray(4, 0);
                    return;
            }
        }
    }
}

