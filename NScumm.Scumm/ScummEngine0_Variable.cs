//
//  ScummEngine0_Variable.cs
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

using System.Diagnostics;

namespace NScumm.Scumm
{
    partial class ScummEngine0
    {
        protected override void SetupVars()
        {
            VariableEgo = 0;
            VariableCameraPosX = 2;
            VariableHaveMessage = 3;
            VariableRoom = 4;
            VariableActiveObject2 = 5;
            VariableOverride = 6;
            VariableIsSoundRunning = 8;
            VariableActiveVerb = 9;
            VariableCharCount = 10;
        }

        protected override void ResetScummVars()
        {
            Variables[VariableEgo.Value] = 3;

            // Setup light
            _currentLights = (byte)(LightModes.ActorUseBasePalette | LightModes.ActorUseColors | LightModes.RoomLightsOn);
        }

        protected override int GetVarOrDirectWord(OpCodeParameter param)
        {
            return GetVarOrDirectByte(param);
        }

        void SetBitVar()
        {
            var flag = GetVarOrDirectByte(OpCodeParameter.Param1);
            var mask = GetVarOrDirectByte(OpCodeParameter.Param2);
            var mod = GetVarOrDirectByte(OpCodeParameter.Param3);

            _bitVars[flag * 8 + mask] = mod != 0;

            Debug.WriteLine("SetBitVar ({0}, {1} {2})", flag, mask, mod);
        }

        void GetBitVar()
        {
            GetResult();
            var flag = GetVarOrDirectByte(OpCodeParameter.Param1);
            var mask = GetVarOrDirectByte(OpCodeParameter.Param2);

            SetResult(_bitVars[flag * 8 + mask] ? 1 : 0);

            Debug.WriteLine("getBitVar ({0}, {1} {2})", flag, mask, _bitVars[flag * 8 + mask]);
        }
    }
}

