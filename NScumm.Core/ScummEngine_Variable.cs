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
using System;
using System.Collections.Generic;
using System.Collections;
using NScumm.Core.IO;

namespace NScumm.Core
{
    partial class ScummEngine
    {
        public int? VariableEgo = 0x01;
        public int? VariableCameraPosX = 0x02;
        public int? VariableHaveMessage = 0x03;
        public int? VariableRoom = 0x04;
        public int? VariableOverride = 0x05;
        public int? VariableCurrentLights = 0x09;
        public int? VariableTimer1 = 0x0B;
        public int? VariableTimer2 = 0x0C;
        public int? VariableTimer3 = 0x0D;
        public int? VariableMusicTimer = 0x0E;
        public int? VariableCameraMinX = 0x11;
        public int? VariableCameraMaxX = 0x12;
        public int? VariableTimerNext = 0x13;
        public int? VariableVirtualMouseX = 0x14;
        public int? VariableVirtualMouseY = 0x15;
        public int? VariableRoomResource = 0x16;
        public int? VariableCutSceneExitKey = 0x18;
        public int? VariableTalkActor = 0x19;
        public int? VariableCameraFastX = 0x1A;
        public int? VariableScrollScript;
        public int? VariableEntryScript = 0x1C;
        public int? VariableEntryScript2 = 0x1D;
        public int? VariableExitScript = 0x1E;
        public int? VariableVerbScript = 0x20;
        public int? VariableSentenceScript = 0x21;
        public int? VariableInventoryScript = 0x22;
        public int? VariableCutSceneStartScript = 0x23;
        public int? VariableCutSceneEndScript = 0x24;
        public int? VariableCharIncrement = 0x25;
        public int? VariableWalkToObject = 0x26;
        public int? VariableDebugMode;
        public int? VariableHeapSpace = 0x28;
        public int? VariableMouseX = 0x2C;
        public int? VariableMouseY = 0x2D;
        public int? VariableTimer = 0x2E;
        public int? VariableTimerTotal = 0x2F;
        public int? VariableSoundcard = 0x30;
        public int? VariableVideoMode = 0x31;
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

        int[] _variables = new int[NumVariables];
        BitArray _bitVars = new BitArray(4096);
        Stack<int> _stack = new Stack<int>();
        int _resultVarIndex;

        public int[] Variables
        {
            get { return _variables; }
        }

        void InitVariables()
        {
            Variables[VariableVideoMode.Value] = 19;
            Variables[VariableHeapSpace.Value] = 1400;
            Variables[VariableCharIncrement.Value] = 4;
            TalkingActor = 0;

            // 0 PC Speaker
            // 1 Tandy
            // 2 CMS
            // 3 AdLib
            // 4 Roland
            Variables[VariableSoundcard.Value] = 3;
        }

        protected byte ReadByte()
        {
            return _currentScriptData[_currentPos++];
        }

        ushort ReadWord()
        {
            ushort word = (ushort)(_currentScriptData[_currentPos++] | (_currentScriptData[_currentPos++] << 8));
            return word;
        }

        protected void GetResult()
        {
            _resultVarIndex = ReadWord();
            if ((_resultVarIndex & 0x2000) == 0x2000)
            {
                int a = (int)ReadWord();
                if ((a & 0x2000) == 0x2000)
                {
                    _resultVarIndex += ReadVariable(a & ~0x2000);
                }
                else
                {
                    _resultVarIndex += (a & 0xFFF);
                }
                _resultVarIndex &= ~0x2000;
            }
        }

        int ReadVariable(int var)
        {
            if ((var & 0x2000) == 0x2000)
            {
                var a = ReadWord();
                if ((a & 0x2000) == 0x2000)
                    var += ReadVariable(a & ~0x2000);
                else
                    var += a & 0xFFF;
                var &= ~0x2000;
            }

            if ((var & 0xF000) == 0)
            {
                //Console.WriteLine("ReadVariable({0}) => {1}", var, _variables[var]);
                ScummHelper.AssertRange(0, var, NumVariables - 1, "variable (reading)");
                if (var == 490 && _game.Id == "monkey2")
                {
                    var = 518;
                }
                return _variables[var];
            }

            if ((var & 0x8000) == 0x8000)
            {
                if (_game.Version <= 3)
                {
                    int bit = var & 0xF;
                    var = (var >> 4) & 0xFF;

                    ScummHelper.AssertRange(0, var, NumVariables - 1, "variable (reading)");
                    return (_variables[var] & (1 << bit)) > 0 ? 1 : 0;
                }
                var &= 0x7FFF;

                ScummHelper.AssertRange(0, var, _bitVars.Length - 1, "variable (reading)");
                return _bitVars[var] ? 1 : 0;
            }

            if ((var & 0x4000) == 0x4000)
            {
                if (Game.Features.HasFlag(GameFeatures.FewLocals))
                {
                    var &= 0xF;
                }
                else
                {
                    var &= 0xFFF;
                }

                ScummHelper.AssertRange(0, var, 20, "local variable (reading)");
                return _slots[_currentScript].LocalVariables[var];
            }

            throw new NotSupportedException("Illegal varbits (r)");
        }

        protected int GetVarOrDirectWord(OpCodeParameter param)
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

        int GetVar()
        {
            return ReadVariable(ReadWord());
        }

        short ReadWordSigned()
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
            int index = _resultVarIndex;
            Console.WriteLine("SetResult({0},{1})", index, value);
            if ((index & 0xF000) == 0)
            {
                ScummHelper.AssertRange(0, index, NumVariables - 1, "variable (writing)");
                _variables[index] = value;
                return;
            }

            if ((index & 0x8000) != 0)
            {
                if (_game.Version <= 3)
                {
                    int bit = index & 0xF;
                    index = (index >> 4) & 0xFF;
                    ScummHelper.AssertRange(0, index, NumVariables - 1, "variable (writing)");
                    if (value > 0)
                        _variables[index] |= (1 << bit);
                    else
                        _variables[index] &= ~(1 << bit);
                }
                else
                {
                    index &= 0x7FFF;

                    ScummHelper.AssertRange(0, index, _bitVars.Length - 1, "bit variable (writing)");
                    _bitVars[index] = value != 0;
                }
                return;
            }

            if ((index & 0x4000) != 0)
            {
                if (Game.Features.HasFlag(GameFeatures.FewLocals))
                {
                    index &= 0xF;
                }
                else
                {
                    index &= 0xFFF;
                }

                ScummHelper.AssertRange(0, index, 20, "local variable (writing)");
                //Console.WriteLine ("SetLocalVariables(script={0},var={1},value={2})", _currentScript, index, value);
                _slots[_currentScript].LocalVariables[index] = value;
                return;
            }
        }

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
            _variables[VariableCameraPosX.Value] = _camera.CurrentPosition.X;
            _variables[VariableHaveMessage.Value] = _haveMsg;
        }
    }
}

