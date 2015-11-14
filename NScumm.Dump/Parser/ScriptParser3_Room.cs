//
//  ScriptParser_Room.cs
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
        Statement LoadRoom()
        {
            var room = GetVarOrDirectByte(OpCodeParameter.Param1);
            return new MethodInvocation("StartScene").AddArgument(room).ToStatement();
        }

        Statement LoadRoomWithEgo()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var room = GetVarOrDirectByte(OpCodeParameter.Param2);
            var x = ReadWordSigned().ToLiteral();
            var y = ReadWordSigned().ToLiteral();
            return new MethodInvocation("LoadRoomWithEgo").AddArguments(obj, room, x, y).ToStatement();
        }

        Statement PseudoRoom()
        {
            var i = ReadByte();
            var args = new List<Expression>();
            int j;
            while ((j = ReadByte()) != 0)
            {
                if (j >= 0x80)
                {
                    //_resourceMapper [j & 0x7F] = (byte)i;
                    args.Add((j & 0x7f).ToLiteral());
                }
            }
            return new MethodInvocation(
                new MemberAccess(new MethodInvocation("PseudoRoom").AddArguments(args), "SetValue"))
                    .AddArgument(i).ToStatement();
        }

        Statement RoomEffect()
        {
            var exp = new MethodInvocation("RoomEffect");
            _opCode = ReadByte();
            if ((_opCode & 0x1F) == 3)
            {
                var a = GetVarOrDirectWord(OpCodeParameter.Param1);
                exp.AddArgument(a);
            }
            return exp.ToStatement();
        }

        Statement RoomOps()
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
                        return new MethodInvocation("Scroll").
						AddArguments(a, b).ToStatement();
                    }
                case 2:     // SO_ROOM_COLOR
                    {
                        if (!paramsBeforeOpcode)
                        {
                            a = GetVarOrDirectWord(OpCodeParameter.Param1);
                            b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        }
                        return new BinaryExpression(new ElementAccess(new SimpleName("RoomPalette"), b),
                            Operator.Assignment,
                            a).ToStatement();
                    }
                case 3:     // SO_ROOM_SCREEN
                    {
                        if (!paramsBeforeOpcode)
                        {
                            a = GetVarOrDirectWord(OpCodeParameter.Param1);
                            b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        }
                        return new MethodInvocation("InitRoomScreen").
						AddArguments(a, b).ToStatement();
                    }
                case 4:     // SO_ROOM_PALETTE
                    {
                        if (Game.Version < 5)
                        {
                            if (!paramsBeforeOpcode)
                            {
                                a = GetVarOrDirectWord(OpCodeParameter.Param1);
                                b = GetVarOrDirectWord(OpCodeParameter.Param2);
                            }
                            return new BinaryExpression(new ElementAccess(new SimpleName("RoomShadowPalette"), b),
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
                            return new MethodInvocation("SetPaletteColor").AddArguments(index, r, g, b).ToStatement();
                        }
                    }
                case 5:     // SO_ROOM_SHAKE_ON
                    return new MethodInvocation("Shake").AddArgument(true.ToLiteral()).ToStatement();
                case 6:     // SO_ROOM_SHAKE_OFF
                    return new MethodInvocation("Shake").AddArgument(false.ToLiteral()).ToStatement();
                case 7:     // SO_ROOM_SCALE
                    {
                        a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        _opCode = ReadByte();
                        var c = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var d = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _opCode = ReadByte();
                        var e = GetVarOrDirectByte(OpCodeParameter.Param2);
                        return new MethodInvocation("RoomScale").AddArguments(a, b, c, d, e).ToStatement();
                    }
                case 8:     // SO_ROOM_INTENSITY
                    {
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var c = GetVarOrDirectByte(OpCodeParameter.Param3);
                        return new MethodInvocation("RoomIntensity").AddArguments(a, b, c).ToStatement();
                    }
                case 9:     // SO_ROOM_SAVEGAME
                    {
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        return new MethodInvocation("RoomSavegame").AddArguments(a, b).ToStatement();
                    }
                case 10:    // SO_ROOM_FADE
                    {
                        a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        return new MethodInvocation("RoomEffect").AddArgument(a).ToStatement();
                    }
                case 11:    // SO_RGB_ROOM_INTENSITY
                    {
                        a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        var c = GetVarOrDirectWord(OpCodeParameter.Param3);
                        _opCode = ReadByte();
                        var d = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var e = GetVarOrDirectByte(OpCodeParameter.Param2);
                        return new MethodInvocation("RoomIntensity").AddArguments(a, b, c, d, e).ToStatement();
                    }
                case 12:        // SO_ROOM_SHADOW
                    {
                        a = GetVarOrDirectWord(OpCodeParameter.Param1);
                        b = GetVarOrDirectWord(OpCodeParameter.Param2);
                        var c = GetVarOrDirectWord(OpCodeParameter.Param3);
                        _opCode = ReadByte();
                        var d = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var e = GetVarOrDirectByte(OpCodeParameter.Param2);
                        return new MethodInvocation("RoomShadow").AddArguments(a, b, c, d, e).ToStatement();
                    }
                case 14:    // SO_LOAD_STRING
                    {
                        // This subopcode is used in Indy 4 to load the IQ points data.
                        // See SO_SAVE_STRING for details
                        var index = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var filename = ReadCharacters();
                        return new MethodInvocation("LoadString").AddArguments(index, filename).ToStatement();
                    }
                case 16:	// SO_CYCLE_SPEED
                    {
                        a = GetVarOrDirectByte(OpCodeParameter.Param1);
                        b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        return new MethodInvocation("ColorCycleSpeed").AddArguments(a, b).ToStatement();
                    }
                default:
                    throw new NotImplementedException(string.Format("RoomOps #{0} not implemented", _opCode & 0x1F));
            }
        }
    }
}

