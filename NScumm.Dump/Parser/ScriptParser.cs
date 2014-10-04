using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Dump
{
    public abstract partial class ScriptParser
    {
        BinaryReader _br;

        public GameInfo Game
        {
            get;
            private set;
        }

        protected ScriptParser(GameInfo info)
        {
            Game = info;
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

        protected void AddKnownVariables(IDictionary<int, string> knownVariables)
        {
            foreach (var item in knownVariables)
            {
                KnownVariables.Add(item.Key, item.Value);
            }
        }

        public static ScriptParser Create(GameInfo info)
        {
            ScriptParser parser;
            switch (info.Version)
            {
                case 3:
                    parser = new ScriptParser3(info);
                    break;
                case 4:
                    parser = new ScriptParser4(info);
                    break;
                case 5:
                    parser = new ScriptParser5(info);
                    break;
                default:
                    throw new NotSupportedException(string.Format("SCUMM version {0} not supported.", info.Version));
            }
            parser.InitOpCodes();
            return parser;
        }

        public CompilationUnit Parse(byte[] data)
        {
            var compilationUnit = new CompilationUnit();
            _br = new BinaryReader(new MemoryStream(data));
            while (_br.BaseStream.Position < _br.BaseStream.Length)
            {
                _opCode = _br.ReadByte();
                try
                {
                    var statements = ExecuteOpCode().ToList();
                    compilationUnit.AddStatements(statements);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ResetColor();
                    return compilationUnit;
                }
            }
			
            return compilationUnit;
        }

        IEnumerable<Statement> SaveLoadGame()
        {
            var index = ((IntegerLiteralExpression)GetResultIndexExpression()).Value;
            var arg = GetVarOrDirectByte(OpCodeParameter.Param1);
            var result = new MethodInvocation("SaveLoadGame").AddArgument(arg);
            yield return SetResult(index, result);
        }

        IEnumerable<Statement> SaveLoadVars()
        {
            return ReadByte() == 1 ? SaveVars() : LoadVars();
        }

        IEnumerable<Statement> SaveVars()
        {
            while ((_opCode = ReadByte()) != 0)
            {
                switch (_opCode & 0x1F)
                {
                    case 0x01:
                        {
                            var a = GetResultIndexExpression();
                            var b = GetResultIndexExpression();
                            yield return new MethodInvocation("SaveVars").AddArguments(a, b).ToStatement();
                        }
                        break;
                    case 0x02:
                        {
                            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                            yield return new MethodInvocation("SaveStringVars").AddArguments(a, b).ToStatement();
                        }
                        break;
                    case 0x03: // open file
                        {
                            var file = ReadCharacters();
                            yield return new MethodInvocation("OpenWriteFile").AddArgument(file).ToStatement();
                        }
                        break;
                    case 0x04: //??
                        yield  break;
                    case 0x1F: // close file
                        yield return new MethodInvocation("CloseFile").ToStatement();
                        yield  break;
                }
            }
        }

        IEnumerable<Statement> LoadVars()
        {
            while ((_opCode = ReadByte()) != 0)
            {
                switch (_opCode & 0x1F)
                {
                    case 0x01:
                        {
                            var a = GetResultIndexExpression();
                            var b = GetResultIndexExpression();
                            yield return new MethodInvocation("LoadVars").AddArguments(a, b).ToStatement();
                        }
                        break;
                    case 0x02:
                        {
                            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
                            var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                            yield return new MethodInvocation("LoadStringVars").AddArguments(a, b).ToStatement();
                        }
                        break;
                    case 0x03: // open file
                        {
                            var file = ReadCharacters();
                            yield return new MethodInvocation("OpenReadFile").AddArgument(file).ToStatement();
                        }
                        break;
                    case 0x04: //??
                        yield  break;
                    case 0x1F: // close file
                        yield return new MethodInvocation("CloseFile").ToStatement();
                        yield  break;
                }
            }
        }

        IEnumerable<Statement> SetBoxFlags()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var b = ReadByte().ToLiteral();
            yield return new MethodInvocation("SetBoxFlags").AddArguments(a, b).ToStatement();
        }

        IEnumerable<Statement> DebugOp()
        {
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return new MethodInvocation("Debug").AddArgument(a).ToStatement();
        }

        IEnumerable<Statement> SystemOps()
        {
            var subOp = ReadByte();
            yield return new MethodInvocation("System").AddArgument(subOp.ToLiteral()).ToStatement();
        }

        IEnumerable<Statement> DrawBox()
        {
            var x = GetVarOrDirectWord(OpCodeParameter.Param1);
            var y = GetVarOrDirectWord(OpCodeParameter.Param2);

            _opCode = ReadByte();
            var x2 = GetVarOrDirectWord(OpCodeParameter.Param1);
            var y2 = GetVarOrDirectWord(OpCodeParameter.Param2);
            var color = GetVarOrDirectByte(OpCodeParameter.Param3);

            yield return new MethodInvocation("DrawBox").AddArguments(x, y, x2, y2, color).ToStatement();
        }

        IEnumerable<Statement> DelayVariable()
        {
            yield return new MethodInvocation("Delay").AddArgument(ReadVariable(ReadWord())).ToStatement();
        }

        IEnumerable<Statement> GetRandomNumber()
        {
            var index = GetResultIndexExpression();
            var max = GetVarOrDirectByte(OpCodeParameter.Param1);

            yield return SetResultExpression(index, new MethodInvocation("GetRandomNumber").AddArgument(max));
        }

        IEnumerable<Statement> Lights()
        {
            var a = GetVarOrDirectByte(OpCodeParameter.Param1);
            var b = ReadByte().ToLiteral();
            var c = ReadByte().ToLiteral();
            yield return new MethodInvocation("Lights").AddArguments(a, b, c).ToStatement();
        }

        IEnumerable<Statement> PrintEgo()
        {
            yield return DecodeParseString(new ElementAccess("Variables", 1)).ToStatement();
        }

        IEnumerable<Statement> GetDistance()
        {
            var index = GetResultIndexExpression();
            var o1 = GetVarOrDirectWord(OpCodeParameter.Param1);
            var o2 = GetVarOrDirectWord(OpCodeParameter.Param2);
            var r = new MethodInvocation("GetDistance").AddArguments(o1, o2);

            yield return SetResultExpression(index, r);
        }

        IEnumerable<Statement> Wait()
        {
            if (Game.Id == "indy3")
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
                        yield return new MethodInvocation("WaitForActor").AddArgument(
                            new ElementAccess("Actors", actor)).ToStatement();
                    }
                    break;
                case 2:     // SO_WAIT_FOR_MESSAGE
                    yield return new MethodInvocation("WaitForMessage").ToStatement();
                    break;
                case 3:     // SO_WAIT_FOR_CAMERA
                    yield return new MethodInvocation("WaitForCamera").ToStatement();
                    break;
                case 4:     // SO_WAIT_FOR_SENTENCE
                    yield return new MethodInvocation("WaitForSentence").ToStatement();
                    break;
                default:
                    throw new NotImplementedException("Wait: unknown subopcode" + (_opCode & 0x1F));
            }
        }

        IEnumerable<Statement> WaitForActor()
        {
            if (Game.Id == "indy3")
            {
                var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
                yield return new MethodInvocation("WaitForActor").AddArgument(actor).ToStatement();
            }
        }

        IEnumerable<Statement> WaitForSentence()
        {
            yield return new MethodInvocation("WaitForSentence").ToStatement();
        }

        IEnumerable<Statement> DoSentence()
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
            yield return new MethodInvocation("DoSentence").AddArguments(verbExp).AddArguments(args).ToStatement();
        }

        IEnumerable<Statement> Delay()
        {
            int delay = ReadByte();
            delay |= (ReadByte() << 8);
            delay |= (ReadByte() << 16);
            yield return new MethodInvocation("Delay").AddArgument(delay.ToLiteral()).ToStatement();
        }

        IEnumerable<Statement> ResourceRoutines()
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
                    yield return new ExpressionStatement(
                        new MethodInvocation("LoadScript").
					AddArgument(resId));
                    break;
                case 2: // load sound
                    yield return new ExpressionStatement(
                        new MethodInvocation("LoadSound").
					AddArgument(resId));
                    break;
                case 3: // load costume
                    yield return new ExpressionStatement(
                        new MethodInvocation("LoadCostume").
					AddArgument(resId));
                    break;

                case 4: // load room
                    yield return new ExpressionStatement(
                        new MethodInvocation("LoadRoom").
					AddArgument(resId));
                    break;

                case 5:         // SO_NUKE_SCRIPT
                    yield return new ExpressionStatement(
                        new MethodInvocation("UnloadScript").
					AddArgument(resId));
                    break;
                case 6:         // SO_NUKE_SOUND
                    yield return new ExpressionStatement(
                        new MethodInvocation("UnloadSound").
					AddArgument(resId));
                    break;
                case 7:         // SO_NUKE_COSTUME
                    yield return new ExpressionStatement(
                        new MethodInvocation("UnloadCostume").
					AddArgument(resId));
                    break;
                case 8:         // SO_NUKE_ROOM
                    yield return new ExpressionStatement(
                        new MethodInvocation("UnloadRoom").
					AddArgument(resId));
                    break;
                case 9:         // SO_LOCK_SCRIPT
                    yield return new ExpressionStatement(
                        new MethodInvocation("LockScript").
					AddArgument(resId));
                    break;

                case 10:
                    yield return new ExpressionStatement(
                        new MethodInvocation("LockSound").
					AddArgument(resId));
                    break;

                case 11:        // SO_LOCK_COSTUME
                    yield return new ExpressionStatement(
                        new MethodInvocation("LockCostume").
					AddArgument(resId));
                    break;

                case 12:        // SO_LOCK_ROOM
                    yield return new ExpressionStatement(
                        new MethodInvocation("LockRoom").
					AddArgument(resId));
                    break;

                case 13:        // SO_UNLOCK_SCRIPT
                    yield return new ExpressionStatement(
                        new MethodInvocation("UnlockScript").
					AddArgument(resId));
                    break;

                case 14:        // SO_UNLOCK_SOUND
                    yield return new ExpressionStatement(
                        new MethodInvocation("UnlockSound").
					AddArgument(resId));
                    break;

                case 15:        // SO_UNLOCK_COSTUME
                    yield return new ExpressionStatement(
                        new MethodInvocation("UnlockCostume").
					AddArgument(resId));
                    break;

                case 16:        // SO_UNLOCK_ROOM
                    yield return new MethodInvocation("UnlockRoom").AddArgument(resId).ToStatement();
                    break;

                case 17:
                    yield return 
                        new MethodInvocation("ClearHeap").ToStatement();
                    break;

                case 18:
                    yield return new MethodInvocation("LoadCharset").AddArgument(resId).ToStatement();
                    break;
                case 19:
                    yield return new MethodInvocation("UnloadCharset").AddArgument(resId).ToStatement();
                    break;
                case 20:        // SO_LOAD_OBJECT
                    var a = GetVarOrDirectWord(OpCodeParameter.Param2);
                    yield return new MethodInvocation("LoadObject").AddArguments(resId, a).ToStatement();
                    break;
                default:
                    throw new NotImplementedException(string.Format("ResourceRoutines #{0} is not yet implemented, sorry :(", op));
            }
        }

        IEnumerable<Statement> CursorCommand()
        {
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:
                    // Cursor On
                    yield return new BinaryExpression(new MemberAccess(new SimpleName("Cursor"), "State"),
                        Operator.Assignment, 1.ToLiteral()).ToStatement();
                    break;

                case 2:
                    // Cursor Off
                    yield return new BinaryExpression(new MemberAccess(new SimpleName("Cursor"), "State"),
                        Operator.Assignment, 0.ToLiteral()).ToStatement();
                    break;

                case 3:
                    // User Input on
                    yield return new BinaryExpression(new SimpleName("UserPut"),
                        Operator.Assignment, 1.ToLiteral()).ToStatement();
                    break;

                case 4:
                    // User Input off
                    yield return new BinaryExpression(new SimpleName("UserPut"),
                        Operator.Assignment, 0.ToLiteral()).ToStatement();
                    break;

                case 5:
                    // SO_CURSOR_SOFT_ON
                    yield return new UnaryExpression(new MemberAccess(new SimpleName("Cursor"), "State"),
                        Operator.PostIncrement).ToStatement();
                    break;

                case 6:
                    // SO_CURSOR_SOFT_OFF
                    yield return new UnaryExpression(new MemberAccess(new SimpleName("Cursor"), "State"),
                        Operator.PostDecrement).ToStatement();
                    break;

                case 7:         // SO_USERPUT_SOFT_ON
                    yield return new UnaryExpression(new SimpleName("UserPut"),
                        Operator.PostIncrement).ToStatement();
                    break;

                case 8:         // SO_USERPUT_SOFT_OFF
                    yield return new UnaryExpression(new SimpleName("UserPut"),
                        Operator.PostDecrement).ToStatement();
                    break;

                case 10:
                    {
                        // SO_CURSOR_IMAGE
                        var a = GetVarOrDirectByte(OpCodeParameter.Param1); // Cursor number
                        var b = GetVarOrDirectByte(OpCodeParameter.Param2); // Charset letter to use
                        yield return new MethodInvocation("CursorImage").AddArguments(a, b).ToStatement();
                    }
                    break;

                case 11:        // SO_CURSOR_HOTSPOT
                    {
                        var a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var c = GetVarOrDirectByte(OpCodeParameter.Param3);
                        yield return new MethodInvocation("CursorHotspot").AddArguments(a, b, c).ToStatement();
                    }
                    break;

                case 12:
                    {
                        // SO_CURSOR_SET
                        var i = GetVarOrDirectByte(OpCodeParameter.Param1);

                        yield return new BinaryExpression(new MemberAccess(new SimpleName("Cursor"), "Type"),
                            Operator.Assignment, i.ToLiteral()).ToStatement();
                        break;
                    }
                case 13:
                    {
                        yield return new MethodInvocation("InitCharset").AddArgument(GetVarOrDirectByte(OpCodeParameter.Param1)).ToStatement();
                    }
                    break;
                case 14:											/* unk */
                    if (Game.Version == 3)
                    {
                        var a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        yield return new MethodInvocation("InitCharset").AddArguments(a, b).ToStatement();
                    }
                    else
                    {
                        var args = GetWordVarArgs();
                        yield return new MethodInvocation("InitCharsetColormap").AddArguments(args).ToStatement();
                    }
                    break;

                default:
                    throw new NotImplementedException(string.Format("CursorCommand sub opcode #{0} not implemented", _opCode & 0x1F));
            }
        }
    }
}

