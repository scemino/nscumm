//
//  ScriptParser3.cs
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
using NScumm.Scumm.IO;
using System;
using NScumm.Core;
using System.Collections.Generic;
using NScumm.Scumm;

namespace NScumm.Dump
{
    partial class ScriptParser3: ScriptParser
    {
        public ScriptParser3(GameInfo game)
            : base(game)
        {
            KnownVariables = new Dictionary<int, string>
            {
                { 1,"VariableEgo" },
                { 2,"VariableCameraPosX" },
                { 3,"VariableHaveMessage" },
                { 4,"VariableRoom" },
                { 5,"VariableOverride" },
                { 9,"VariableCurrentLights" },
                { 11,"VariableTimer1" },
                { 12,"VariableTimer2" },
                { 13,"VariableTimer3" },
                { 14,"VariableMusicTimer" },
                { 17,"VariableCameraMinX" },
                { 18,"VariableCameraMaxX" },
                { 19,"VariableTimerNext" },
                { 20,"VariableVirtualMouseX" },
                { 21,"VariableVirtualMouseY" },
                { 22,"VariableRoomResource" },
                { 24,"VariableCutSceneExitKey" },
                { 25,"VariableTalkActor" },
                { 26,"VariableCameraFastX" },
                { 28,"VariableEntryScript" },
                { 29,"VariableEntryScript2" },
                { 30,"VariableExitScript" },
                { 32,"VariableVerbScript" },
                { 33,"VariableSentenceScript" },
                { 34,"VariableInventoryScript" },
                { 35,"VariableCutSceneStartScript" },
                { 36,"VariableCutSceneEndScript" },
                { 37,"VariableCharIncrement" },
                { 38,"VariableWalkToObject" },
                { 40,"VariableHeapSpace" },
                { 44,"VariableMouseX" },
                { 45,"VariableMouseY" },
                { 46,"VariableTimer" },
                { 47,"VariableTimerTotal" },
                { 48,"VariableSoundcard" },
                { 49,"VariableVideoMode" }
            };
        }

        Statement SaveLoadGame()
        {
            var index = ((IntegerLiteralExpression)GetResultIndexExpression()).Value;
            var arg = GetVarOrDirectByte(OpCodeParameter.Param1);
            var result = new MethodInvocation("SaveLoadGame").AddArgument(arg);
            return SetResult(index, result).ToStatement();
        }

        Statement SaveLoadVars()
        {
            return ReadByte() == 1 ? SaveVars() : LoadVars();
        }

        Statement SaveVars()
        {
            var exp = new MethodInvocation("Vars");
            while ((_opCode = ReadByte()) != 0)
            {
                switch (_opCode & 0x1F)
                {
                    case 0x01:
                        {
                            var a = GetResultIndexExpression();
                            var b = GetResultIndexExpression();
                            exp = new MethodInvocation(new MemberAccess(exp, "SaveVars")).AddArguments(a, b);
                        }
                        break;
                    case 0x02:
                        {
                            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                            exp = new MethodInvocation(new MemberAccess(exp, "SaveStringVars")).AddArguments(a, b);
                        }
                        break;
                    case 0x03: // open file
                        {
                            var file = ReadCharacters();
                            exp = new MethodInvocation(new MemberAccess(exp, "OpenWriteFile")).AddArgument(file);
                        }
                        break;
                    case 0x04: //??
                        return exp.ToStatement();
                    case 0x1F: // close file
                        exp = new MethodInvocation(new MemberAccess(exp, "CloseFile"));
                        return exp.ToStatement();
                }
            }
            return exp.ToStatement();
        }

        Statement LoadVars()
        {
            var exp = new MethodInvocation("Vars");
            while ((_opCode = ReadByte()) != 0)
            {
                switch (_opCode & 0x1F)
                {
                    case 0x01:
                        {
                            var a = GetResultIndexExpression();
                            var b = GetResultIndexExpression();
                            exp = new MethodInvocation(new MemberAccess(exp, "LoadVars")).AddArguments(a, b);
                        }
                        break;
                    case 0x02:
                        {
                            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                            exp = new MethodInvocation(new MemberAccess(exp, "LoadStringVars")).AddArguments(a, b);
                        }
                        break;
                    case 0x03: // open file
                        {
                            var file = ReadCharacters();
                            exp = new MethodInvocation(new MemberAccess(exp, "OpenReadFile")).AddArgument(file);
                        }
                        break;
                    case 0x04: //??
                        return exp.ToStatement();
                    case 0x1F: // close file
                        exp = new MethodInvocation(new MemberAccess(exp, "CloseFile"));
                        return exp.ToStatement();
                }
            }
            return exp.ToStatement();
        }

        Statement SetBoxFlags()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var b = ReadByte().ToLiteral();
            return new MethodInvocation("SetBoxFlags").AddArguments(a, b).ToStatement();
        }

        Statement DebugOp()
        {
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            return new MethodInvocation("Debug").AddArgument(a).ToStatement();
        }

        Statement SystemOps()
        {
            var subOp = ReadByte();
            return new MethodInvocation("System").AddArgument(subOp.ToLiteral()).ToStatement();
        }

        Statement DrawBox()
        {
            var x = GetVarOrDirectWord(OpCodeParameter.Param1);
            var y = GetVarOrDirectWord(OpCodeParameter.Param2);

            _opCode = ReadByte();
            var x2 = GetVarOrDirectWord(OpCodeParameter.Param1);
            var y2 = GetVarOrDirectWord(OpCodeParameter.Param2);
            var color = GetVarOrDirectByte(OpCodeParameter.Param3);

            return new MethodInvocation("DrawBox").AddArguments(x, y, x2, y2, color).ToStatement();
        }

        Statement DelayVariable()
        {
            return new MethodInvocation("Delay").AddArgument(ReadVariable(ReadWord())).ToStatement();
        }

        Statement GetRandomNumber()
        {
            var index = GetResultIndexExpression();
            var max = GetVarOrDirectByte(OpCodeParameter.Param1);

            return SetResultExpression(index, new MethodInvocation("GetRandomNumber").AddArgument(max)).ToStatement();
        }

        Statement Lights()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var b = ReadByte().ToLiteral();
            var c = ReadByte().ToLiteral();
            return new MethodInvocation("Lights").AddArguments(a, b, c).ToStatement();
        }

        Statement PrintEgo()
        {
            return DecodeParseString(new ElementAccess("Variables", 1)).ToStatement();
        }

        Statement GetDistance()
        {
            var index = GetResultIndexExpression();
            var o1 = GetVarOrDirectWord(OpCodeParameter.Param1);
            var o2 = GetVarOrDirectWord(OpCodeParameter.Param2);
            var r = new MethodInvocation("GetDistance").AddArguments(o1, o2);

            return SetResultExpression(index, r).ToStatement();
        }

        Statement Wait()
        {
            if (Game.GameId == GameId.Indy3)
            {
                _opCode = 2;
            }
            else
            {
                _opCode = ReadByte();
            }

            switch (_opCode & 0x1F)
            {
                case 1:     // SO_WAIT_FOR_ACTOR
                    {
                        var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
                        return new MethodInvocation("WaitForActor").AddArgument(
                            new ElementAccess("Actors", actor)).ToStatement();
                    }
                case 2:     // SO_WAIT_FOR_MESSAGE
                    return new MethodInvocation("WaitForMessage").ToStatement();
                case 3:     // SO_WAIT_FOR_CAMERA
                    return new MethodInvocation("WaitForCamera").ToStatement();
                case 4:     // SO_WAIT_FOR_SENTENCE
                    return new MethodInvocation("WaitForSentence").ToStatement();
                default:
                    throw new NotImplementedException("Wait: unknown subopcode" + (_opCode & 0x1F));
            }
        }

        Statement WaitForActor()
        {
            if (Game.GameId == GameId.Indy3)
            {
                var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
                return new MethodInvocation("WaitForActor").AddArgument(actor).ToStatement();
            }
            return new MethodInvocation("WaitForActor").ToStatement();
        }

        Statement WaitForSentence()
        {
            return new MethodInvocation("WaitForSentence").ToStatement();
        }

        Statement DoSentence()
        {
            var verbExp = GetVarOrDirectByte(OpCodeParameter.Param1);
            var verbLiteralExp = verbExp as IntegerLiteralExpression;
            var args = new List<Expression>();
            if (verbLiteralExp != null)
            {
                if (Convert.ToByte(verbLiteralExp.Value) != 0xFE)
                {
                    args.Add(GetVarOrDirectWord(OpCodeParameter.Param2));
                    args.Add(GetVarOrDirectWord(OpCodeParameter.Param3));
                }
            }
            return new MethodInvocation("DoSentence").AddArguments(verbExp).AddArguments(args).ToStatement();
        }

        Statement Delay()
        {
            int delay = ReadByte();
            delay |= (ReadByte() << 8);
            delay |= (ReadByte() << 16);
            return new MethodInvocation("Delay").AddArgument(delay.ToLiteral()).ToStatement();
        }

        Statement ResourceRoutines()
        {
            Expression resId = 0.ToLiteral();

            _opCode = ReadByte();
            if (_opCode != 17)
            {
                resId = GetVarOrDirectByte(OpCodeParameter.Param1);
            }

            int op = _opCode & 0x3F;
            switch (op)
            {
                case 1: // load script
                    return new ExpressionStatement(
                        new MethodInvocation("LoadScript").
                        AddArgument(resId));
                case 2: // load sound
                    return new ExpressionStatement(
                        new MethodInvocation("LoadSound").
                        AddArgument(resId));
                case 3: // load costume
                    return new ExpressionStatement(
                        new MethodInvocation("LoadCostume").
                        AddArgument(resId));
                case 4: // load room
                    return new ExpressionStatement(
                        new MethodInvocation("LoadRoom").
                        AddArgument(resId));
                case 5:         // SO_NUKE_SCRIPT
                    return new ExpressionStatement(
                        new MethodInvocation("UnloadScript").
                        AddArgument(resId));
                case 6:         // SO_NUKESound
                    return new ExpressionStatement(
                        new MethodInvocation("UnloadSound").
                        AddArgument(resId));
                case 7:         // SO_NUKE_COSTUME
                    return new ExpressionStatement(
                        new MethodInvocation("UnloadCostume").
                        AddArgument(resId));
                case 8:         // SO_NUKE_ROOM
                    return new ExpressionStatement(
                        new MethodInvocation("UnloadRoom").
                        AddArgument(resId));
                case 9:         // SO_LOCK_SCRIPT
                    return new ExpressionStatement(
                        new MethodInvocation("LockScript").
                        AddArgument(resId));
                case 10:
                    return new ExpressionStatement(
                        new MethodInvocation("LockSound").
                        AddArgument(resId));
                case 11:        // SO_LOCK_COSTUME
                    return new ExpressionStatement(
                        new MethodInvocation("LockCostume").
                        AddArgument(resId));
                case 12:        // SO_LOCK_ROOM
                    return new ExpressionStatement(
                        new MethodInvocation("LockRoom").
                        AddArgument(resId));
                case 13:        // SO_UNLOCK_SCRIPT
                    return new ExpressionStatement(
                        new MethodInvocation("UnlockScript").
                        AddArgument(resId));
                case 14:        // SO_UNLOCKSound
                    return new ExpressionStatement(
                        new MethodInvocation("UnlockSound").
                        AddArgument(resId));
                case 15:        // SO_UNLOCK_COSTUME
                    return new ExpressionStatement(
                        new MethodInvocation("UnlockCostume").
                        AddArgument(resId));
                case 16:        // SO_UNLOCK_ROOM
                    return new MethodInvocation("UnlockRoom").AddArgument(resId).ToStatement();
                case 17:
                    return new MethodInvocation("ClearHeap").ToStatement();
                case 18:
                    return new MethodInvocation("LoadCharset").AddArgument(resId).ToStatement();
                case 19:
                    return new MethodInvocation("UnloadCharset").AddArgument(resId).ToStatement();
                case 20:        // SO_LOAD_OBJECT
                    var a = GetVarOrDirectWord(OpCodeParameter.Param2);
                    return new MethodInvocation("LoadObject").AddArguments(resId, a).ToStatement();
                default:
                    throw new NotImplementedException(string.Format("ResourceRoutines #{0} is not yet implemented, sorry :(", op));
            }
        }

        Statement CursorCommand()
        {
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:
                    // Cursor On
                    return new BinaryExpression(new MemberAccess(new SimpleName("Cursor"), "State"),
                        Operator.Assignment, 1.ToLiteral()).ToStatement();
                case 2:
                    // Cursor Off
                    return new BinaryExpression(new MemberAccess(new SimpleName("Cursor"), "State"),
                        Operator.Assignment, 0.ToLiteral()).ToStatement();
                case 3:
                    // User Input on
                    return new BinaryExpression(new SimpleName("UserPut"),
                        Operator.Assignment, 1.ToLiteral()).ToStatement();
                case 4:
                    // User Input off
                    return new BinaryExpression(new SimpleName("UserPut"),
                        Operator.Assignment, 0.ToLiteral()).ToStatement();
                case 5:
                    // SO_CURSOR_SOFT_ON
                    return new UnaryExpression(new MemberAccess(new SimpleName("Cursor"), "State"),
                        Operator.PostIncrement).ToStatement();
                case 6:
                    // SO_CURSOR_SOFT_OFF
                    return new UnaryExpression(new MemberAccess(new SimpleName("Cursor"), "State"),
                        Operator.PostDecrement).ToStatement();
                case 7:         // SO_USERPUT_SOFT_ON
                    return new UnaryExpression(new SimpleName("UserPut"),
                        Operator.PostIncrement).ToStatement();
                case 8:         // SO_USERPUT_SOFT_OFF
                    return new UnaryExpression(new SimpleName("UserPut"),
                        Operator.PostDecrement).ToStatement();
                case 10:
                    {
                        // SO_CURSOR_IMAGE
                        var a = GetVarOrDirectByte(OpCodeParameter.Param1); // Cursor number
                        var b = GetVarOrDirectByte(OpCodeParameter.Param2); // Charset letter to use
                        return new MethodInvocation("CursorImage").AddArguments(a, b).ToStatement();
                    }

                case 11:        // SO_CURSOR_HOTSPOT
                    {
                        var a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var c = GetVarOrDirectByte(OpCodeParameter.Param3);
                        return new MethodInvocation("CursorHotspot").AddArguments(a, b, c).ToStatement();
                    }

                case 12:
                    {
                        // SO_CURSOR_SET
                        var i = GetVarOrDirectByte(OpCodeParameter.Param1);

                        return new BinaryExpression(new MemberAccess(new SimpleName("Cursor"), "Type"),
                            Operator.Assignment, i.ToLiteral()).ToStatement();
                    }
                case 13:
                    return new MethodInvocation("InitCharset").AddArgument(GetVarOrDirectByte(OpCodeParameter.Param1)).ToStatement();
                case 14:                                            /* unk */
                    if (Game.Version == 3)
                    {
                        var a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        return new MethodInvocation("InitCharset").AddArguments(a, b).ToStatement();
                    }
                    else
                    {
                        var args = GetWordVarArgs();
                        return new MethodInvocation("InitCharsetColormap").AddArguments(args).ToStatement();
                    }
                default:
                    throw new NotImplementedException(string.Format("CursorCommand sub opcode #{0} not implemented", _opCode & 0x1F));
            }
        }
    }
}

