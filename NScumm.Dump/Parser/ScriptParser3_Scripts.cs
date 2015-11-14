//
//  ScriptParser_Scripts.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
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

using System;
using System.Collections.Generic;
using NScumm.Core;
using NScumm.Scumm;

namespace NScumm.Dump
{
    partial class ScriptParser3
    {
        Statement StartScript()
        {
            var op = _opCode;
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            var args = GetWordVarArgs();
            return new MethodInvocation("RunScript").
				AddArguments(
                script,
                new BooleanLiteralExpression((op & 0x20) != 0),
                new BooleanLiteralExpression((op & 0x40) != 0)).
				AddArguments(args).ToStatement();
        }

        Statement StopScript()
        {
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation(
                new MemberAccess(
                    new ElementAccess("Scripts", script),
                    "Stop")).ToStatement();
        }

        Statement StopObjectScript()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            return new MethodInvocation("StopObjectScript").AddArgument(obj).ToStatement();
        }

        Statement FreezeScripts()
        {
            var scr = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("FreezeScripts").AddArgument(scr).ToStatement();
        }

        Statement ChainScript()
        {
            var script = GetVarOrDirectByte(OpCodeParameter.Param1);
            var args = GetWordVarArgs();
            return new MethodInvocation("ChainScript").AddArgument(script).AddArguments(args).ToStatement();
        }

        Statement CutScene()
        {
            var args = GetWordVarArgs();
            return new MethodInvocation("CutScene").AddArguments(args).ToStatement();
        }

        Statement BeginOverride()
        {
            return new MethodInvocation((ReadByte() != 0) ? "BeginOverride" : "EndOverride").ToStatement();
        }

        Statement EndCutscene()
        {
            return new MethodInvocation("EndCutScene").ToStatement();
        }

        Statement IsScriptRunning()
        {
            var indexExp = GetResultIndexExpression();
            return SetResultExpression(indexExp, new MethodInvocation("IsScriptRunning").
                AddArgument(GetVarOrDirectByte(OpCodeParameter.Param1))).ToStatement();
        }

        Statement BreakHere()
        {
            return new MethodInvocation("BreakHere").ToStatement();
        }
    }
}

