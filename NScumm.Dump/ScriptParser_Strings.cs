using System;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Dump
{
    partial class ScriptParser
    {
        IEnumerable<Statement> Print()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return DecodeParseString(
                new MemberAccess(
                    new ElementAccess("Actors", actor), 
                    new MethodInvocation("Print"))).ToStatement();
        }

        IEnumerable<Statement> GetStringWidth()
        {
            var exp = GetResultIndexExpression();
            var str = GetVarOrDirectByte(OpCodeParameter.Param1);
            yield return SetResultExpression(exp, new MethodInvocation("GetStringWidth").AddArgument(str));
        }

        Expression DecodeParseString(Expression exp)
        {
            while ((_opCode = ReadByte()) != 0xFF)
            {
                switch (_opCode & 0xF)
                {
                    case 0:     // SO_AT
                        var x = GetVarOrDirectWord(OpCodeParameter.Param1);
                        var y = GetVarOrDirectWord(OpCodeParameter.Param2);
                        exp = new MemberAccess(exp, new MethodInvocation("At").AddArguments(x, y));
                        break;

                    case 1:     // SO_COLOR
                        var color = GetVarOrDirectByte(OpCodeParameter.Param1);
                        exp = new MemberAccess(exp, new MethodInvocation("Color").AddArguments(color));
                        break;

                    case 2:     // SO_CLIPPED
                        var clipped = GetVarOrDirectWord(OpCodeParameter.Param1);
                        exp = new MemberAccess(exp, new MethodInvocation("Clipped").AddArguments(clipped));
                        break;

                    case 4:     // SO_CENTER
                        exp = new MemberAccess(exp, new MethodInvocation("Center"));
                        break;

                    case 6:     // SO_LEFT
                        var args = new List<Expression>();
                        if (Game.Version == 3)
                        {
                            args.Add(GetVarOrDirectWord(OpCodeParameter.Param1));
                        }
                        exp = new MemberAccess(exp, new MethodInvocation("Left").AddArguments(args));
                        break;

                    case 7:     // SO_OVERHEAD
                        exp = new MemberAccess(exp, new MethodInvocation("Overhead"));
                        break;

                    case 8:
                        {	// SO_SAY_VOICE
                            var offset = GetVarOrDirectWord(OpCodeParameter.Param1);
                            var delay = GetVarOrDirectWord(OpCodeParameter.Param2);
                            exp = new MemberAccess(exp, new MethodInvocation("PlayCDTrack").AddArguments(offset, delay));
                        }
                        break;

                    case 15:
                        {   // SO_TEXTSTRING
                            var text = ReadCharacters();
                            exp = new MemberAccess(exp, new MethodInvocation("Print").AddArguments(text));
                        }
                        return exp;

                    default:
                        throw new NotImplementedException(string.Format("DecodeParseString #{0:X2} is not implemented", _opCode & 0xF));
                }
            }
            return exp;
        }

        IEnumerable<Statement> StringOperations()
        {
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:
                    {
                        // loadstring
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var text = ReadCharacters();
                        yield return new BinaryExpression(
                            new ElementAccess("Strings", id),
                            Operator.Assignment,
                            text).ToStatement();
                    }
                    break;

                case 2:
                    {
                        // copy string
                        var idA = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var idB = GetVarOrDirectByte(OpCodeParameter.Param2);
                        yield return new BinaryExpression(
                            idA,
                            Operator.Assignment,
                            idB).ToStatement();
                    }
                    break;

                case 3:
                    {
                        // Write Character
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var index = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var character = GetVarOrDirectByte(OpCodeParameter.Param3);
                        yield return new BinaryExpression(
                            new ElementAccess(
                                new ElementAccess("Strings", id),
                                index),
                            Operator.Assignment,
                            character).ToStatement();
                    }
                    break;

                case 4:
                    {
                        // Get string char
                        var index = GetResultIndexExpression();
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        yield return SetResultExpression(
                            index,
                            new ElementAccess(
                                new ElementAccess("Strings", id),
                                b));
                    }
                    break;

                case 5:
                    {
                        // New String
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var size = GetVarOrDirectByte(OpCodeParameter.Param2);
                        yield return new BinaryExpression(
                            new ElementAccess(
                                new SimpleName("Strings"),
                                id),
                            Operator.Assignment,
                            new MethodInvocation("CreateString").
						AddArgument(size)).ToStatement();
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}

