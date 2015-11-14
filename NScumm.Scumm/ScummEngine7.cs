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

using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using NScumm.Scumm.Audio.IMuse.IMuseDigital;
using NScumm.Scumm.IO;
using NScumm.Scumm.Smush;

namespace NScumm.Scumm
{
    partial class ScummEngine7: ScummEngine6
    {
        protected int VariableNumGlobalObjs;
        protected int VariableCameraDestX;
        protected int VariableCameraDestY;
        protected int VariableRestartKey;
        protected int VariablePauseKey;
        protected int VariableVersionKey;
        protected int VariableCameraSpeedX;
        protected int VariableCameraSpeedY;
        protected int VariableVideoName;
        protected int VariableString2Draw;
        public int VariableCustomScaleTable;

        public bool SmushVideoShouldFinish { get; internal set; }

        public bool SmushActive { get; internal set; }

        internal SmushPlayer SmushPlayer { get; private set; }

        internal SmushMixer SmushMixer { get; private set; }

        internal Insane.Insane Insane { get; private set; }

        internal IMuseDigital IMuseDigital { get; private set; }

        public ScummEngine7(GameSettings game, IGraphicsManager graphicsManager, IInputManager inputManager, IMixer mixer)
            : base(game, graphicsManager, inputManager, mixer)
        {
            if (Game.GameId == GameId.Dig && (Game.Features.HasFlag(GameFeatures.Demo)))
                _smushFrameRate = 15;
            else
                _smushFrameRate = (Game.GameId == GameId.FullThrottle) ? 10 : 12;

            for (int i = 0; i < _subtitleQueue.Length; i++)
            {
                _subtitleQueue[i] = new SubtitleText();
            }

            int dimuseTempo = 10;
            MusicEngine = IMuseDigital = new IMuseDigital(this, mixer, dimuseTempo);
            IMuseDigital.SetAudioNames(ResourceManager.AudioNames);

            // Create FT INSANE object
            if (Game.GameId == GameId.FullThrottle)
                Insane = new Insane.Insane(this);

            SmushMixer = new SmushMixer(Mixer);
            SmushPlayer = new SmushPlayer(this);
        }

        protected override void SetupVars()
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
            VariableKeyPress = 118;

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
            if (Game.GameId == GameId.FullThrottle)
            {
                VariableCharsetMask = 119;
            }

            VariableVideoName = 123;

            VariableString2Draw = 130;
            VariableCustomScaleTable = 131;

            VariableBlastAboveText = 133;

            VariableMusicBundleLoaded = 135;
            VariableVoiceBundleLoaded = 136;
        }

        protected override void ResetScummVars()
        {
            base.ResetScummVars();

            Variables[VariableCameraThresholdX.Value] = 100;
            Variables[VariableCameraThresholdY.Value] = 70;
            Variables[VariableCameraAccelX.Value] = 100;
            Variables[VariableCameraAccelY.Value] = 100;

            if (Game.Version != 8)
            {
                Variables[VariableV6EMSSpace.Value] = 10000;
                // TODO: vs change this: 1401
                Variables[VariableNumGlobalObjs] = 1401 - 1;
            }

            Variables[VariableDefaultTalkDelay.Value] = 60;
        }
    }
}
