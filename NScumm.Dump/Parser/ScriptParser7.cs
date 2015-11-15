//
//  ScriptParser7.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

using NScumm.Scumm.IO;
using System.Collections.Generic;

namespace NScumm.Dump
{
    class ScriptParser7: ScriptParser6
    {
        public ScriptParser7(GameInfo game)
            : base(game)
        {
            KnownVariables = new Dictionary<int, string>
            {
                { 1, "VariableMouseX" },
                { 2, "VariableMouseY" },
                { 3, "VariableVirtualMouseX" },
                { 4, "VariableVirtualMouseY" },
                { 5, "VariableRoomWidth" },
                { 6, "VariableRoomHeight" },
                { 7, "VariableCameraPosX" },
                { 8, "VariableCameraPosY" },
                { 9, "VariableOverride" },
                { 10, "VariableRoom" },
                { 11, "VariableRoomResource" },
                { 12, "VariableTalkActor" },
                { 13, "VariableHaveMessage" },
                { 14, "VariableTimer" },
                { 15, "VariableTimerTotal" },

                { 16, "VariableTimeDateYear" },
                { 17, "VariableTimeDateMonth" },
                { 18, "VariableTimeDateDay" },
                { 19, "VariableTimeDateHour" },
                { 20, "VariableTimeDateMinute" },
                { 21, "VariableTimeDateSecond" },

                { 22, "VariableLeftButtonDown" },
                { 23, "VariableRightButtonDown" },
                { 24, "VariableLeftButtonHold" },
                { 25, "VariableRightButtonHold" }
                ,
                { 26, "VariableMemoryPerformance" },
                { 27, "VariableVideoPerformance" },
                { 29, "VariableGameLoaded" },
                { 32, "VariableV6EMSSpace" },
                { 33, "VariableVoiceMode" },
                { 34, "VariableRandomNumber" },
                { 35, "VariableNewRoom" },
                { 36, "VariableWalkToObject" },

                { 37, "VariableNumGlobalObjs" },

                { 38, "VariableCameraDestX" },
                { 39, "VariableCameraDestY" },
                { 40, "VariableCameraFollowedActor" },

                { 50, "VariableScrollScript" },
                { 51, "VariableEntryScript" },
                { 52, "VariableEntryScript2" },
                { 53, "VariableExitScript" },
                { 54, "VariableExitScript2" },
                { 55, "VariableVerbScript" },
                { 56, "VariableSentenceScript" },
                { 57, "VariableInventoryScript" },
                { 58, "VariableCutSceneStartScript" },
                { 59, "VariableCutSceneEndScript" },
                { 60, "VariableSaveLoadScript" },
                { 61, "VariableSaveLoadScript2" },

                { 62, "VariableCutSceneExitKey" },
                { 63, "VariableRestartKey" },
                { 64, "VariablePauseKey" },
                { 65, "VariableMainMenu" },
                { 66, "VariableVersionKey" },
                { 118, "VariableKeyPress" },

                { 97, "VariableTimerNext" },
                { 98, "VariableTimer1" },
                { 99, "VariableTimer2" },
                { 100, "VariableTimer3" },
               
                { 101, "VariableCameraMinX" },
                { 102, "VariableCameraMaxX" },
                { 103, "VariableCameraMinY" },
                { 104, "VariableCameraMaxY" },
                { 105, "VariableCameraThresholdX" },
                { 106, "VariableCameraThresholdY" },
                { 107, "VariableCameraSpeedX" },
                { 108, "VariableCameraSpeedY" },
                { 109, "VariableCameraAccelX" },
                { 110, "VariableCameraAccelY" },

                { 111, "VariableEgo" },

                { 112, "VariableCursorState" },
                { 113, "VariableUserPut" },
                { 114, "VariableDefaultTalkDelay" },
                { 115, "VariableCharIncrement" },
                { 116, "VariableDebugMode" },
                { 117, "VariableFadeDelay" },

                { 123, "VariableVideoName" },

                { 130, "VariableString2Draw" },
                { 131, "VariableCustomScaleTable" },

                { 133, "VariableBlastAboveText" },

                { 135, "VariableMusicBundleLoaded" },
                { 136, "VariableVoiceBundleLoaded" },
            };

            if (game.GameId == GameId.FullThrottle)
            {
                KnownVariables.Add(119, "VariableCharsetMask");
            }
        }


    }
}


