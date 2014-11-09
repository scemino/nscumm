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
using System;

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

        Statement StopObjectScript()
        {
            return new MethodInvocation("StopObjectScript").AddArgument(Pop()).ToStatement();
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

        Statement DelayFrames()
        {
            return new MethodInvocation("DelayFrames").AddArgument(Pop()).ToStatement();
        }

        Statement Delay()
        {
            return new MethodInvocation("Delay").AddArgument(Pop()).ToStatement();
        }

        Statement DelaySeconds()
        {
            return new MethodInvocation("DelaySeconds").AddArgument(Pop()).ToStatement();
        }

        Statement DelayMinutes()
        {
            return new MethodInvocation("DelayMinutes").AddArgument(Pop()).ToStatement();
        }

        Statement StopSentence()
        {
            return new MethodInvocation("StopSentence").ToStatement();
        }

        Statement Wait()
        {
            var subOp = ReadByte();
            switch (subOp)
            {
                case 168:               // SO_WAIT_FOR_ACTOR Wait for actor
                    {
                        var offset = ReadWordSigned();
                        var actor = Pop();
                        return new MethodInvocation("WaitForActor").AddArguments(actor, offset.ToLiteral()).ToStatement();
                    }
                case 169:               // SO_WAIT_FOR_MESSAGE Wait for message
                    return new MethodInvocation("WaitForMessage").ToStatement();
                case 170:               // SO_WAIT_FOR_CAMERA Wait for camera
                    return new MethodInvocation("WaitForCamera").ToStatement();
                case 171:               // SO_WAIT_FOR_SENTENCE
                    return new MethodInvocation("WaitForSentence").ToStatement();
                case 226:               // SO_WAIT_FOR_ANIMATION
                    {
                        var offset = ReadWordSigned();
                        var actor = Pop();
                        return new MethodInvocation("WaitForAnimation").AddArguments(actor, offset.ToLiteral()).ToStatement();
                    }
                case 232:               // SO_WAIT_FOR_TURN
                    {
                        var offset = ReadWordSigned();
                        var actor = Pop();
                        return new MethodInvocation("WaitForTurn").AddArguments(actor, offset.ToLiteral()).ToStatement();
                    }
                default:
                    throw new NotSupportedException(string.Format("Wait: default case 0x{0:X}", subOp));
            }
        }

        Statement StartObject()
        {
            var args = GetStackList(25);
            var entryp = Pop();
            var script = Pop();
            var flags = Pop();
            return new MethodInvocation("StartObject").AddArguments(script, entryp, flags, args).ToStatement();
        }
    }
}

