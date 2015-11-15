//
//  ScriptParser_Strings.cs
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
        Statement Print()
        {
            var actor = GetVarOrDirectByte(OpCodeParameter.Param1);
            return DecodeParseString(
                new MethodInvocation(
                    new MemberAccess(
                        new ElementAccess("Actors", actor), 
                        "Print"))).ToStatement();
        }

        Statement GetStringWidth()
        {
            var exp = GetResultIndexExpression();
            var str = GetVarOrDirectByte(OpCodeParameter.Param1);
            return SetResultExpression(exp, new MethodInvocation("GetStringWidth").AddArgument(str)).ToStatement();
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
                        exp = new MethodInvocation(new MemberAccess(exp, "At")).AddArguments(x, y);
                        break;

                    case 1:     // SO_COLOR
                        var color = GetVarOrDirectByte(OpCodeParameter.Param1);
                        exp = new MethodInvocation(new MemberAccess(exp, "Color")).AddArguments(color);
                        break;

                    case 2:     // SO_CLIPPED
                        var clipped = GetVarOrDirectWord(OpCodeParameter.Param1);
                        exp = new MethodInvocation(new MemberAccess(exp, "Clipped")).AddArguments(clipped);
                        break;

                    case 4:     // SO_CENTER
                        exp = new MethodInvocation(new MemberAccess(exp, "Center"));
                        break;

                    case 6:     // SO_LEFT
                        var args = new List<Expression>();
                        if (Game.Version == 3)
                        {
                            args.Add(GetVarOrDirectWord(OpCodeParameter.Param1));
                        }
                        exp = new MethodInvocation(new MemberAccess(exp, "Left")).AddArguments(args);
                        break;

                    case 7:     // SO_OVERHEAD
                        exp = new MethodInvocation(new MemberAccess(exp, "Overhead"));
                        break;

                    case 8:
                        {	// SO_SAY_VOICE
                            var offset = GetVarOrDirectWord(OpCodeParameter.Param1);
                            var delay = GetVarOrDirectWord(OpCodeParameter.Param2);
                            exp = new MethodInvocation(new MemberAccess(exp, "PlayCDTrack")).AddArguments(offset, delay);
                        }
                        break;

                    case 15:
                        {   // SO_TEXTSTRING
                            var text = ReadCharacters();
                            exp = new MethodInvocation(new MemberAccess(exp, "Print")).AddArguments(text);
                        }
                        return exp;

                    default:
                        throw new NotImplementedException(string.Format("DecodeParseString #{0:X2} is not implemented", _opCode & 0xF));
                }
            }
            return exp;
        }

        Statement StringOperations()
        {
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:
                    {
                        // loadstring
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var text = ReadCharacters();
                        return new BinaryExpression(
                            new ElementAccess("Strings", id),
                            Operator.Assignment,
                            text).ToStatement();
                    }
                case 2:
                    {
                        // copy string
                        var idA = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var idB = GetVarOrDirectByte(OpCodeParameter.Param2);
                        return new BinaryExpression(
                            idA,
                            Operator.Assignment,
                            idB).ToStatement();
                    }
                case 3:
                    {
                        // Write Character
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var index = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var character = GetVarOrDirectByte(OpCodeParameter.Param3);
                        return new BinaryExpression(
                            new ElementAccess(
                                new ElementAccess("Strings", id),
                                index),
                            Operator.Assignment,
                            character).ToStatement();
                    }
                case 4:
                    {
                        // Get string char
                        var index = GetResultIndexExpression();
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        return SetResultExpression(
                            index,
                            new ElementAccess(
                                new ElementAccess("Strings", id),
                                b)).ToStatement();
                    }
                case 5:
                    {
                        // New String
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var size = GetVarOrDirectByte(OpCodeParameter.Param2);
                        return new BinaryExpression(
                            new ElementAccess(
                                new SimpleName("Strings"),
                                id),
                            Operator.Assignment,
                            new MethodInvocation("CreateString").
						AddArgument(size)).ToStatement();
                    }
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

