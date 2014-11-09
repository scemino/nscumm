//
//  ScriptParser6_Script.cs
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

namespace NScumm.Dump
{
    partial class ScriptParser6
    {
        Statement Cutscene()
        {
            var args = GetStackList(25);
            return new MethodInvocation("CutScene").AddArgument(args).ToStatement();
        }

        Statement EndCutscene()
        {
            return new MethodInvocation("EndCutScene").ToStatement();
        }

        Statement FreezeUnfreeze()
        {
            var args = Pop();
            return new MethodInvocation("FreezeUnfreeze").AddArgument(args).ToStatement();
        }

        Statement StartScript()
        {
            var args = GetStackList(25);
            var script = Pop();
            var flags = Pop();
            return new MethodInvocation("StartScript").AddArguments(script, flags, args).ToStatement();
        }

        Statement StartScriptQuick()
        {
            var args = GetStackList(25);
            var script = Pop();
            return new MethodInvocation("RunScript").AddArgument(script).AddArguments(args).ToStatement();
        }

        Statement StopScript()
        {
            return new MethodInvocation("StopScript").AddArgument(Pop()).ToStatement();
        }

        Statement DoSentence()
        {
            var b = Pop();
            var a = Pop();
            if (Game.Version < 8)
            {
                Pop(); // dummy pop 
            }
            var verb = Pop();
            return new MethodInvocation("DoSentence").AddArguments(verb, a, b).ToStatement();
        }

        Statement IsScriptRunning()
        {
            return Push(new MethodInvocation("IsScriptRunning").AddArgument(Pop()));
        }

        Statement BeginOverride()
        {
            return new MethodInvocation("BeginOverride").ToStatement();
        }

        Statement EndOverride()
        {
            return new MethodInvocation("EndOverride").ToStatement();
        }

        Statement BreakHere()
        {
            return new MethodInvocation("BreakHere").ToStatement();
        }

        Statement StopObjectCode()
        {
            return new MethodInvocation("StopObjectCode").ToStatement();
        }
    }
}

