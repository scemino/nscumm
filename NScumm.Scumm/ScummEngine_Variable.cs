//
//  ScummEngine_Variables.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections;
using System.Collections.Generic;

namespace NScumm.Scumm
{
    abstract partial class ScummEngine
    {
        public int? VariableEgo;
        public int? VariableCameraPosX;
        public int? VariableCameraPosY;
        public int? VariableHaveMessage;
        public int? VariableRoom;
        public int? VariableOverride;
        public int? VariableCurrentLights;
        public int? VariableTimer1;
        public int? VariableTimer2;
        public int? VariableTimer3;
        public int? VariableMusicTimer;
        public int? VariableCameraMinY;
        public int? VariableCameraMaxY;
        public int? VariableCameraMinX;
        public int? VariableCameraMaxX;
        public int? VariableTimerNext;
        public int? VariableVirtualMouseX;
        public int? VariableVirtualMouseY;
        public int? VariableRoomResource;
        public int? VariableLastSound;
        public int? VariableCutSceneExitKey;
        public int? VariableTalkActor;
        public int? VariableCameraFastX;
        public int? VariableScrollScript;
        public int? VariableEntryScript;
        public int? VariableEntryScript2;
        public int? VariableExitScript;
        public int? VariableExitScript2;
        public int? VariableVerbScript;
        public int? VariableSentenceScript;
        public int? VariableInventoryScript;
        public int? VariableCutSceneStartScript;
        public int? VariableCutSceneEndScript;
        public int? VariableCharIncrement;
        public int? VariableWalkToObject;
        public int? VariableDebugMode;
        public int? VariableHeapSpace;
        public int? VariableMouseX;
        public int? VariableMouseY;
        public int? VariableTimer;
        public int? VariableTimerTotal;
        public int? VariableSoundcard;
        public int? VariableVideoMode;
        public int? VariableMainMenu;
        public int? VariableFixedDisk;
        public int? VariableCursorState;
        public int? VariableUserPut;
        public int? VariableTalkStringY;
        public int? VariableNoSubtitles;
        public int? VariableSoundResult;
        public int? VariableTalkStopKey;
        public int? VariableFadeDelay;
        public int? VariableSoundParam;
        public int? VariableSoundParam2;
        public int? VariableSoundParam3;
        public int? VariableInputMode;
        public int? VariableMemoryPerformance;
        public int? VariableVideoPerformance;
        public int? VariableRoomFlag;
        public int? VariableGameLoaded;
        public int? VariableNewRoom;
        public int? VariableRoomWidth;
        public int? VariableRoomHeight;
        public int? VariableVoiceMode;
        public int? VariableSaveLoadScript;
        public int? VariableSaveLoadScript2;
        public int? VariableLeftButtonHold;
        public int? VariableRightButtonHold;
        public int? VariableLeftButtonDown;
        public int? VariableRightButtonDown;
        public int? VariableV6SoundMode;
        public int? VariableV6EMSSpace;
        public int? VariableCameraThresholdX;
        public int? VariableCameraThresholdY;
        public int? VariableCameraAccelX;
        public int? VariableCameraAccelY;
        public int? VariableVoiceBundleLoaded;
        public int? VariableDefaultTalkDelay;
        public int? VariableMusicBundleLoaded;
        public int? VariableCurrentDisk;
        public int? VariableActiveVerb;
        public int? VariableActiveObject1;
        public int? VariableActiveObject2;
        public int? VariableVerbAllowed;
        public int? VariableCharCount;

        int[] _variables;
        protected BitArray _bitVars;
        protected Stack<int> _stack = new Stack<int>();
        protected int _resultVarIndex;

        public int[] Variables
        {
            get { return _variables; }
        }

        protected byte ReadByte()
        {
            return _currentScriptData[CurrentPos++];
        }

        protected virtual uint ReadWord()
        {
            return FetchScriptWord();
        }

        protected uint FetchScriptWord()
        {
            ushort word = (ushort)(_currentScriptData[CurrentPos++] | (_currentScriptData[CurrentPos++] << 8));
            return word;
        }

        protected abstract void GetResult();

        protected abstract int ReadVariable(uint var);

        protected virtual int GetVarOrDirectWord(OpCodeParameter param)
        {
            if (((OpCodeParameter)_opCode).HasFlag(param))
                return GetVar();
            return ReadWordSigned();
        }

        protected int GetVarOrDirectByte(OpCodeParameter param)
        {
            if (((OpCodeParameter)_opCode).HasFlag(param))
                return GetVar();
            return ReadByte();
        }

        protected abstract int GetVar();

        protected virtual int ReadWordSigned()
        {
            return (short)ReadWord();
        }

        protected int[] GetWordVarArgs()
        {
            var args = new List<int>();
            while ((_opCode = ReadByte()) != 0xFF)
            {
                args.Add(GetVarOrDirectWord(OpCodeParameter.Param1));
            }
            return args.ToArray();
        }

        protected void SetResult(int value)
        {
            WriteVariable((uint)_resultVarIndex, value);
        }

        protected abstract void WriteVariable(uint index, int value);

        void SetVarRange()
        {
            GetResult();
            var a = ReadByte();
            int b;
            do
            {
                if ((_opCode & 0x80) == 0x80)
                    b = ReadWordSigned();
                else
                    b = ReadByte();
                SetResult(b);
                _resultVarIndex++;
            } while ((--a) > 0);
        }

        void UpdateVariables()
        {
            if (Game.Version >= 7)
            {
                Variables[VariableCameraPosX.Value] = Camera.CurrentPosition.X;
                Variables[VariableCameraPosY.Value] = Camera.CurrentPosition.Y;
            }
            else if (Game.Version <= 2)
            {
                Variables[VariableCameraPosX.Value] = Camera.CurrentPosition.X >> V12_X_SHIFT;
            }
            else
            {
                _variables[VariableCameraPosX.Value] = _camera.CurrentPosition.X;
            }
            if (Game.Version <= 7)
                Variables[VariableHaveMessage.Value] = _haveMsg;
        }
    }
}

