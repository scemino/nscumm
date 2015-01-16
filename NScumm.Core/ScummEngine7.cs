//
//  ScummEngine7.cs
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

using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Audio;
using NScumm.Core.IO;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace NScumm.Core
{
    partial class ScummEngine7: ScummEngine6
    {
        int VariableCameraPosY;
        int VariableTimeDateSecond;
        int VariableLeftButtonDown;
        int VariableRightButtonDown;
        int VariableRandomNumber;
        int VariableNumGlobalObjs;
        int VariableCameraDestX;
        int VariableCameraDestY;
        int VariableCameraFollowedActor;
        int VariableExitScript2;
        int VariableRestartKey;
        int VariablePauseKey;
        int VariableVersionKey;
        int VariableTalkStopKey;
        int VariableKeypress;
        int VariableCameraMinY;
        int VariableCameraMaxY;
        int VariableCameraThresholdX;
        int VariableCameraThresholdY;
        int VariableCameraSpeedX;
        int VariableCameraSpeedY;
        int VariableCameraAccelX;
        int VariableCameraAccelY;
        int VariableDefaultTalkDelay;
        int VariableCharsetMask;
        int VariableVideoName;
        int VariableString2Draw;
        int VariableCustomScaleTable;
        int VariableBlastAboveText;
        int VariableMusicBundleLoaded;
        int VariableVoiceBundleLoaded;


        public bool SmushVideoShouldFinish { get; internal set;}
        public bool SmushActive { get; internal set; }
        internal SmushPlayer SmushPlayer { get; private set;}
        internal SmushMixer SmushMixer { get; private set;}

        public ScummEngine7(GameInfo game, IGraphicsManager graphicsManager, IInputManager inputManager, IMixer mixer)
            : base(game, graphicsManager, inputManager, mixer)
        {
            VariableMouseX = 1;
            VariableMouseY = 2;
            VariableVirtualMouseX = 3;
            VariableVirtualMouseY = 4;
            VariableRoomWidth = 5;
            VariableRoomHeight = 6;
            VariableCameraPosX = 7;
            VariableCameraPosY = 8;
            VariableOverride = 9;
            VariableRoom = 10;
            VariableRoomResource = 11;
            VariableTalkActor = 12;
            VariableHaveMessage = 13;
            VariableTimer = 14;
            VariableTimerTotal = 15;

            VariableTimeDateYear = 16;
            VariableTimeDateMonth = 17;
            VariableTimeDateDay = 18;
            VariableTimeDateHour = 19;
            VariableTimeDateMinute = 20;
            VariableTimeDateSecond = 21;

            VariableLeftButtonDown = 22;
            VariableRightButtonDown = 23;
            VariableLeftButtonHold = 24;
            VariableRightButtonHold = 25;

            VariableMemoryPerformance = 26;
            VariableVideoPerformance = 27;
            VariableGameLoaded = 29;
            VariableV6EMSSpace = 32;
            VariableVoiceMode = 33; // 0 is voice, 1 is voice+text, 2 is text only
            VariableRandomNumber = 34;
            VariableNewRoom = 35;
            VariableWalkToObject = 36;

            VariableNumGlobalObjs = 37;

            VariableCameraDestX = 38;
            VariableCameraDestY = 39;
            VariableCameraFollowedActor = 40;

            VariableScrollScript = 50;
            VariableEntryScript = 51;
            VariableEntryScript2 = 52;
            VariableExitScript = 53;
            VariableExitScript2 = 54;
            VariableVerbScript = 55;
            VariableSentenceScript = 56;
            VariableInventoryScript = 57;
            VariableCutSceneStartScript = 58;
            VariableCutSceneEndScript = 59;
            VariableSaveLoadScript = 60;
            VariableSaveLoadScript2 = 61;

            VariableCutSceneExitKey = 62;
            VariableRestartKey = 63;
            VariablePauseKey = 64;
            VariableMainMenu = 65;
            VariableVersionKey = 66;
            VariableTalkStopKey = 67;
            VariableKeypress = 118;

            VariableTimerNext = 97;
            VariableTimer1 = 98;
            VariableTimer2 = 99;
            VariableTimer3 = 100;

            VariableCameraMinX = 101;
            VariableCameraMaxX = 102;
            VariableCameraMinY = 103;
            VariableCameraMaxY = 104;
            VariableCameraThresholdX = 105;
            VariableCameraThresholdY = 106;
            VariableCameraSpeedX = 107;
            VariableCameraSpeedY = 108;
            VariableCameraAccelX = 109;
            VariableCameraAccelY = 110;

            VariableEgo = 111;

            VariableCursorState = 112;
            VariableUserPut = 113;
            VariableDefaultTalkDelay = 114;
            VariableCharIncrement = 115;
            VariableDebugMode = 116;
            VariableFadeDelay = 117;

            // Full Throttle specific
            if (game.GameId == GameId.FullThrottle)
            {
                VariableCharsetMask = 119;
            }

            VariableVideoName = 123;

            VariableString2Draw = 130;
            VariableCustomScaleTable = 131;

            VariableBlastAboveText = 133;

            VariableMusicBundleLoaded = 135;
            VariableVoiceBundleLoaded = 136;

            SmushMixer = new SmushMixer(Mixer);
            SmushPlayer = new SmushPlayer(this);
        }
    }
}
