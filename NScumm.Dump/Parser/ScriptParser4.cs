//
//  ScriptParser4.cs
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
using NScumm.Core;
using System.Collections.Generic;
using NScumm.Scumm.IO;
using System;
using NScumm.Scumm;

namespace NScumm.Dump
{
    class ScriptParser4: ScriptParser3
    {
        public ScriptParser4(GameInfo game)
            : base(game)
        {
            var knownVariables = new Dictionary<int,string>
            {
                { 27, "VariableScrollScript" },
                { 39, "VariableDebugMode" },
                { 50, "VariableMainMenu" },
                { 51, "VariableFixedDisk" },
                { 52, "VariableCursorState" },
                { 53, "VariableUserPut" },
                { 54, "VariableTalkStringY" }
            };

            AddKnownVariables(knownVariables);
        }

        protected override void InitOpCodes()
        {
            base.InitOpCodes();

            opCodes[0x30] = MatrixOperations;
            opCodes[0xB0] = MatrixOperations;
            opCodes[0x3B] = GetActorScale;
            opCodes[0xBB] = GetActorScale;
            opCodes[0x4C] = SoundKludge;
        }

        protected Statement GetActorScale()
        {
            var indexExp = GetResultIndexExpression();
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            return SetResultExpression(indexExp, 
                new MemberAccess(
                    new ElementAccess("Actors", actor),
                    "Scale")).ToStatement();
        }

        protected Statement SoundKludge()
        {
            var items = GetWordVarArgs();
            return new MethodInvocation("SoundKludge").AddArguments(items).ToStatement();
        }

        Statement MatrixOperations()
        {
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:
                    var a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    return new MethodInvocation("SetBoxFlags").AddArguments(a, b).ToStatement();
                case 2:
                    a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    return new MethodInvocation("SetBoxScale").AddArguments(a, b).ToStatement();
                case 3:
                    a = GetVarOrDirectByte(OpCodeParameter.Param1);
                    b = GetVarOrDirectByte(OpCodeParameter.Param2);
                    return new MethodInvocation("SetBoxScaleSlot").AddArguments(a, b).ToStatement();
                case 4:
                    return new MethodInvocation("CreateBoxMatrix").ToStatement();
                default:
                    throw new NotImplementedException(string.Format("MatrixOperations subopcode {0} not implemented", _opCode & 0x1F));
            }
        }
    }
}

