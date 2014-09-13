using System;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Tmp
{
    partial class ScriptParser
    {
        IEnumerable<Statement> LoadRoom()
        {
            var room = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return new MethodInvocation("StartScene").AddArgument(room).ToStatement();
        }

        IEnumerable<Statement> LoadRoomWithEgo()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var room = GetVarOrDirectByte(OpCodeParameter.Param2);
            var x = ReadWordSigned().ToLiteral();
            var y = ReadWordSigned().ToLiteral();
            yield return new MethodInvocation("LoadRoomWithEgo").AddArguments(obj, room, x, y).ToStatement();
        }

        IEnumerable<Statement> PseudoRoom()
        {
            int i = ReadByte(), j;
            while ((j = ReadByte()) != 0)
            {
                if (j >= 0x80)
                {
                    //_resourceMapper [j & 0x7F] = (byte)i;
                    yield return new ExpressionStatement(
                        new BinaryExpression(
                            new ElementAccess(
                                "ResourceMapper",
                                new LiteralExpression(j & 0x7f)),
                            Operator.Assignment,
                            new LiteralExpression(i)));
                }
            }
        }

        IEnumerable<Statement> RoomEffect()
        {
            _opCode = ReadByte();
            if ((_opCode & 0x1F) == 3)
            {
                var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                yield return new ExpressionStatement(
                    new MethodInvocation("RoomEffect").
					AddArgument(a));
            }
        }

        IEnumerable<Statement> RoomOps()
        {
            var paramsBeforeOpcode = (Game.Version == 3);
            Expression a = null;
            Expression b = null;
            if (paramsBeforeOpcode)
            {
                a = GetVarOrDirectWord(OpCodeParameter.Param1);
                b = GetVarOrDirectWord(OpCodeParameter.Param2);
            }
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:     // SO_ROOM_SCROLL
                    {
                        if (!paramsBeforeOpcode)
                        {
                            a = GetVarOrDirectWord(OpCodeParameter.Param1);
                            b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        }
                        yield return new MethodInvocation("Scroll").
						AddArguments(a, b).ToStatement();
                    }
                    break;

                case 2:     // SO_ROOM_COLOR
                    {
                        if (!paramsBeforeOpcode)
                        {
                            a = GetVarOrDirectWord(OpCodeParameter.Param1);
                            b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        }
                        yield return new BinaryExpression(new ElementAccess(new SimpleName("RoomPalette"), b),
                            Operator.Assignment,
                            a).ToStatement();
                    }
                    break;

                case 3:     // SO_ROOM_SCREEN
                    {
                        if (!paramsBeforeOpcode)
                        {
                            a = GetVarOrDirectWord(OpCodeParameter.Param1);
                            b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        }
                        yield return new MethodInvocation("InitRoomScreen").
						AddArguments(a, b).ToStatement();
                    }
                    break;

                case 4:     // SO_ROOM_PALETTE
                    {
                        if (Game.Version < 5)
                        {
                            if (!paramsBeforeOpcode)
                            {
                                a = GetVarOrDirectWord(OpCodeParameter.Param1);
                                b = GetVarOrDirectWord(OpCodeParameter.Param2);
                            }
                            yield return new BinaryExpression(new ElementAccess(new SimpleName("RoomShadowPalette"), b),
                                Operator.Assignment,
                                a).ToStatement();
                        }
                        else
                        {
                            var index = GetVarOrDirectWord(OpCodeParameter.Param1);
                            var r = GetVarOrDirectWord(OpCodeParameter.Param2);
                            var g = GetVarOrDirectWord(OpCodeParameter.Param3);
                            _opCode = ReadByte();
                            b = GetVarOrDirectByte(OpCodeParameter.Param1);
                            yield return new MethodInvocation("SetPaletteColor").AddArguments(index, r, g, b).ToStatement();
                        }
                    }
                    break;

                case 5:     // SO_ROOM_SHAKE_ON
                    yield return new MethodInvocation("Shake").AddArgument(true.ToLiteral()).ToStatement();
                    break;

                case 6:     // SO_ROOM_SHAKE_OFF
                    yield return new MethodInvocation("Shake").AddArgument(false.ToLiteral()).ToStatement();
                    break;

                case 7:     // SO_ROOM_SCALE
                    {
                        a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        _opCode = ReadByte();
                        var c = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var d = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _opCode = ReadByte();
                        var e = GetVarOrDirectByte(OpCodeParameter.Param2);
                        yield return new MethodInvocation("RoomScale").AddArguments(a, b, c, d, e).ToStatement();
                    }
                    break;
                case 8:     // SO_ROOM_INTENSITY
                    {
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var c = GetVarOrDirectByte(OpCodeParameter.Param3);
                        yield return new MethodInvocation("RoomIntensity").AddArguments(a, b, c).ToStatement();
                    }
                    break;
                case 9:     // SO_ROOM_SAVEGAME
                    {
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        yield return new MethodInvocation("RoomSavegame").AddArguments(a, b).ToStatement();
                    }
                    break;
                case 10:    // SO_ROOM_FADE
                    {
                        a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        yield return new MethodInvocation("RoomEffect").AddArgument(a).ToStatement();
                    }
                    break;
                case 11:    // SO_RGB_ROOM_INTENSITY
                    {
                        a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        var c = GetVarOrDirectWord(OpCodeParameter.Param3);
                        _opCode = ReadByte();
                        var d = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var e = GetVarOrDirectByte(OpCodeParameter.Param2);
                        yield return new MethodInvocation("RoomIntensity").AddArguments(a, b, c, d, e).ToStatement();
                    }
                    break;
                case 12:        // SO_ROOM_SHADOW
                    {
                        a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        var c = GetVarOrDirectWord(OpCodeParameter.Param3);
                        _opCode = ReadByte();
                        var d = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var e = GetVarOrDirectByte(OpCodeParameter.Param2);
                        yield return new MethodInvocation("RoomShadow").AddArguments(a, b, c, d, e).ToStatement();
                    }
                    break;

                case 16:	// SO_CYCLE_SPEED
                    {
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        yield return new MethodInvocation("ColorCycleSpeed").AddArguments(a, b).ToStatement();
                    }
                    break;
                default:
                    throw new NotImplementedException(string.Format("RoomOps #{0} not implemented", _opCode & 0x1F));
            }
        }
    }
}

