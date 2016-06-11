//
//  QueenEngine.cs
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
using NScumm.Core.IO;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    public class LogicGame : Logic
    {
        public LogicGame(QueenEngine vm)
            : base(vm)
        {
        }

        public override void UseJournal()
        {
            _vm.Input.ClearKeyVerb();
            _vm.Input.ClearMouseButton();

            _vm.Command.Clear(false);
            _journal.Use();
            _vm.Walk.StopJoe();

            _vm.Input.ClearKeyVerb();
            _vm.Input.ClearMouseButton();
        }

        protected override bool ChangeToSpecialRoom()
        {
            if (CurrentRoom == Defines.ROOM_JUNGLE_PINNACLE)
            {
                HandlePinnacleRoom();
                return true;
            }
            else if (CurrentRoom == Defines.FOTAQ_LOGO && GameState[Defines.VAR_INTRO_PLAYED] == 0)
            {
                DisplayRoom(CurrentRoom, RoomDisplayMode.RDM_FADE_NOJOE, 100, 2, true);
                PlayCutaway("COPY.CUT");
                if (_vm.HasToQuit)
                    return true;
                PlayCutaway("CLOGO.CUT");
                if (_vm.HasToQuit)
                    return true;
                if (_vm.Resource.Platform != Platform.Amiga)
                {
                    // TODO: conf
                    //					if (ConfMan.getBool("alt_intro") && _vm.Resource.IsCD) {
                    //						PlayCutaway("CINTR.CUT");
                    //					} else {
                    PlayCutaway("CDINT.CUT");
                    //					}
                }
                if (_vm.HasToQuit)
                    return true;
                PlayCutaway("CRED.CUT");
                if (_vm.HasToQuit)
                    return true;
                _vm.Display.PalSetPanel();
                SceneReset();
                CurrentRoom = Defines.ROOM_HOTEL_LOBBY;
                EntryObj = 584;
                DisplayRoom(CurrentRoom, RoomDisplayMode.RDM_FADE_JOE, 100, 2, true);
                PlayCutaway("C70D.CUT");
                GameState[Defines.VAR_INTRO_PLAYED] = 1;
                InventoryRefresh();
                return true;
            }
            return false;
        }

        protected override void SetupSpecialMoveTable()
        {
			_specialMoves[2] = AsmMakeJoeUseDress;
			_specialMoves[3] = AsmMakeJoeUseNormalClothes;
			_specialMoves[4] = AsmMakeJoeUseUnderwear;
			_specialMoves[7] = AsmStartCarAnimation;       // room 74
			_specialMoves[8] = AsmStopCarAnimation;        // room 74
    		_specialMoves[9] = AsmStartFightAnimation;     // room 69
    		_specialMoves[10] = AsmWaitForFrankPosition;   // c69e.cut
    		_specialMoves[11] = AsmMakeFrankGrowing;       // c69z.cut
    		_specialMoves[12] = AsmMakeRobotGrowing;       // c69z.cut
			_specialMoves[14] = AsmEndGame;
			_specialMoves[15] = AsmPutCameraOnDino;
			_specialMoves[16] = AsmPutCameraOnJoe;
			_specialMoves[19] = AsmSetAzuraInLove;
			_specialMoves[20] = AsmPanRightFromJoe;
			_specialMoves[21] = AsmSetLightsOff;
			_specialMoves[22] = AsmSetLightsOn;
			_specialMoves[23] = AsmSetManequinAreaOn;
			_specialMoves[24] = AsmPanToJoe;
			_specialMoves[25] = AsmTurnGuardOn;
			_specialMoves[26] = AsmPanLeft320To144;
			_specialMoves[27] = AsmSmoochNoScroll;
			_specialMoves[28] = AsmMakeLightningHitPlane;
			_specialMoves[29] = AsmScaleBlimp;
			_specialMoves[30] = AsmScaleEnding;
			_specialMoves[31] = AsmWaitForCarPosition;
			_specialMoves[33] = AsmAttemptPuzzle;
			_specialMoves[34] = AsmScrollTitle;
			if (_vm.Resource.Platform == Platform.DOS) {
				_specialMoves[5]  = AsmSwitchToDressPalette;
				_specialMoves[6]  = AsmSwitchToNormalPalette;
				_specialMoves[13] = AsmShrinkRobot;
				_specialMoves[17] = AsmAltIntroPanRight;      // cintr.cut
				_specialMoves[18] = AsmAltIntroPanLeft;       // cintr.cut
				_specialMoves[27] = AsmSmooch;
				_specialMoves[32] = AsmShakeScreen;
				_specialMoves[34] = AsmScaleTitle;
				_specialMoves[36] = AsmPanRightToHugh;
				_specialMoves[37] = AsmMakeWhiteFlash;
				_specialMoves[38] = AsmPanRightToJoeAndRita;
				_specialMoves[39] = AsmPanLeftToBomb;         // cdint.cut
			}
        }
    }
}

