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

namespace NScumm.Core
{
	partial class ScummEngine
	{
		public const int VariableEgo = 0x01;
		const int VariableCameraPosX = 0x02;
		const int VariableHaveMessage = 0x03;
		const int VariableRoom = 0x04;
		const int VariableOverride = 0x05;
		const int VariableCurrentLights = 0x09;
		public const int VariableTimer1 = 0x0B;
		public const int VariableTimer2 = 0x0C;
		public const int VariableTimer3 = 0x0D;
		public const int VariableMusicTimer = 0x0E;
		const int VariableCameraMinX = 0x11;
		const int VariableCameraMaxX = 0x12;
		public const int VariableTimerNext = 0x13;
		public const int VariableVirtualMouseX = 0x14;
		public const int VariableVirtualMouseY = 0x15;
		const int VariableRoomResource = 0x16;
		public const int VariableCutSceneExitKey = 0x18;
		const int VariableTalkActor = 0x19;
		const int VariableCameraFastX = 0x1A;
		const int VariableScrollScript = 0x1B;
		const int VariableEntryScript = 0x1C;
		const int VariableEntryScript2 = 0x1D;
		const int VariableExitScript = 0x1E;
		const int VariableVerbScript = 0x20;
		const int VariableSentenceScript = 0x21;
		const int VariableInventoryScript = 0x22;
		const int VariableCutSceneStartScript = 0x23;
		const int VariableCutSceneEndScript = 0x24;
		public const int VariableCharIncrement = 0x25;
		const int VariableWalkToObject = 0x26;
		const int VariableDebugMode = 0x27;
		const int VariableHeapSpace = 0x28;
		public const int VariableMouseX = 0x2C;
		public const int VariableMouseY = 0x2D;
		const int VariableTimer = 0x2E;
		const int VariableTimerTotal = 0x2F;
		const int VariableSoundcard = 0x30;
		const int VariableVideoMode = 0x31;
		const int VariableMainMenu = 0x32;
		const int VariableFixedDisk = 0x33;
		const int VariableCursorState = 0x34;
		const int VariableUserPut = 0x35;
		const int VariableTalkStringY = 0x36;

		int[] _variables;
		BitArray _bitVars = new BitArray (4096);
		Stack<int> _stack = new Stack<int> ();
		int _resultVarIndex;

		internal int[] Variables {
			get { return _variables; }
		}

		void InitVariables ()
		{
			_variables = new int[NumVariables];
			_variables [VariableVideoMode] = 19;
			_variables [VariableFixedDisk] = 1;
			_variables [VariableHeapSpace] = 1400;
			_variables [VariableCharIncrement] = 4;
			TalkingActor = 0;
			#if DEBUG
			//_variables[VariableDebugMode] = 1;
			#endif
			// MDT_ADLIB
			_variables [VariableSoundcard] = 3;

			_variables [VariableTalkStringY] = -0x50;

			// Setup light
			_variables [VariableCurrentLights] = (int)(LightModes.ActorUseBasePalette | LightModes.ActorUseColors | LightModes.RoomLightsOn);

			if (_game.Id == "monkey") {
				_variables [74] = 1225;
			}
		}

		byte ReadByte ()
		{
			return _currentScriptData [_currentPos++];
		}

		ushort ReadWord ()
		{
			ushort word = (ushort)(_currentScriptData [_currentPos++] | (_currentScriptData [_currentPos++] << 8));
			return word;
		}

		void GetResult ()
		{
			_resultVarIndex = ReadWord ();
			if ((_resultVarIndex & 0x2000) == 0x2000) {
				int a = (int)ReadWord ();
				if ((a & 0x2000) == 0x2000) {
					_resultVarIndex += ReadVariable (a & ~0x2000);
				} else {
					_resultVarIndex += (a & 0xFFF);
				}
				_resultVarIndex &= ~0x2000;
			}
		}

		int ReadVariable (int var)
		{
			if ((var & 0x2000) == 0x2000) {
				var a = ReadWord ();
				if ((a & 0x2000) == 0x2000)
					var += ReadVariable (a & ~0x2000);
				else
					var += a & 0xFFF;
				var &= ~0x2000;
			}

			if ((var & 0xF000) == 0) {
				//Console.WriteLine("ReadVariable({0}) => {1}", var, _variables[var]);
				ScummHelper.AssertRange (0, var, NumVariables - 1, "variable (reading)");
				return _variables [var];
			}

			if ((var & 0x8000) == 0x8000) {
				if (_game.Version <= 3) {
					int bit = var & 0xF;
					var = (var >> 4) & 0xFF;

					ScummHelper.AssertRange (0, var, NumVariables - 1, "variable (reading)");
					return (_variables [var] & (1 << bit)) > 0 ? 1 : 0;
				}
				var &= 0x7FFF;

				ScummHelper.AssertRange (0, _resultVarIndex, _bitVars.Length - 1, "variable (reading)");
				return _bitVars [var] ? 1 : 0;
			}

			if ((var & 0x4000) == 0x4000) {
				var &= 0xFFF;

				ScummHelper.AssertRange (0, var, 20, "local variable (reading)");
				return _slots [_currentScript].LocalVariables [var];
			}

			throw new NotSupportedException ("Illegal varbits (r)");
		}

		int GetVarOrDirectWord (OpCodeParameter param)
		{
			if (((OpCodeParameter)_opCode).HasFlag (param))
				return GetVar ();
			return ReadWordSigned ();
		}

		int GetVarOrDirectByte (OpCodeParameter param)
		{
			if (((OpCodeParameter)_opCode).HasFlag (param))
				return GetVar ();
			return ReadByte ();
		}

		int GetVar ()
		{
			return ReadVariable (ReadWord ());
		}

		short ReadWordSigned ()
		{
			return (short)ReadWord ();
		}

		int[] GetWordVarArgs ()
		{
			var args = new List<int> ();
			while ((_opCode = ReadByte ()) != 0xFF) {
				args.Add (GetVarOrDirectWord (OpCodeParameter.Param1));
			}
			return args.ToArray ();
		}

		void SetResult (int value)
		{
			Console.WriteLine ("SetResult({0},{1})", _resultVarIndex, value);
			if ((_resultVarIndex & 0xF000) == 0) {
				ScummHelper.AssertRange (0, _resultVarIndex, NumVariables - 1, "variable (writing)");
				_variables [_resultVarIndex] = value;
				return;
			}

			if ((_resultVarIndex & 0x8000) != 0) {
				if (_game.Version <= 3) {
					int bit = value & 0xF;
					value = (value >> 4) & 0xFF;
					ScummHelper.AssertRange (0, value, NumVariables - 1, "variable (writing)");
					if (value > 0)
						_variables [value] |= (1 << bit);
					else
						_variables [value] &= ~(1 << bit);
				} else {
					_resultVarIndex &= 0x7FFF;

					ScummHelper.AssertRange (0, _resultVarIndex, _bitVars.Length - 1, "bit variable (writing)");
					_bitVars [_resultVarIndex] = value != 0;
				}
			}

			if ((_resultVarIndex & 0x4000) != 0) {
				_resultVarIndex &= 0xFFF;

				ScummHelper.AssertRange (0, _resultVarIndex, 20, "local variable (writing)");
				//Console.WriteLine ("SetLocalVariables(script={0},var={1},value={2})", _currentScript, _resultVarIndex, value);
				_slots [_currentScript].LocalVariables [_resultVarIndex] = value;
				return;
			}
		}

		void SetVarRange ()
		{
			GetResult ();
			var a = ReadByte ();
			int b;
			do {
				if ((_opCode & 0x80) == 0x80)
					b = ReadWordSigned ();
				else
					b = ReadByte ();
				SetResult (b);
				_resultVarIndex++;
			} while ((--a) > 0);
		}

		void UpdateVariables ()
		{
			_variables [VariableCameraPosX] = _camera.CurrentPosition.X;
			_variables [VariableHaveMessage] = _haveMsg;
		}
	}
}

