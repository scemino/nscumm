//
//  ScriptParser6_Room.cs
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
        protected virtual Statement RoomOps()
        {
            var subOp = ReadByte();
            var exp = new MethodInvocation("RoomOps");
            switch (subOp)
            {
                case 172:               // SO_ROOM_SCROLL
                    {
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "RoomScroll")).AddArguments(a, b);
                    }
                    break;

                case 174:               // SO_ROOM_SCREEN
                    {
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "InitScreens")).AddArguments(a, b);
                    }
                    break;

                case 175:               // SO_ROOM_PALETTE
                    {
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "SetPalette")).AddArguments(d, a, b, c);
                    }
                    break;

                case 176:               // SO_ROOM_SHAKE_ON
                    exp = new MethodInvocation(new MemberAccess(exp, "Shake")).AddArgument(true);
                    break;

                case 177:               // SO_ROOM_SHAKE_OFF
                    exp = new MethodInvocation(new MemberAccess(exp, "Shake")).AddArgument(false);
                    break;

                case 179:               // SO_ROOM_INTENSITY
                    {
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "DarkenPalette")).AddArguments(a, a, a, b, c);
                    }
                    break;

                case 180:               // SO_ROOM_SAVEGAME
                    {
                        var a = Pop();
                        var b = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "SaveGame")).AddArguments(a, b);
                    }
                    break;

                case 181:               // SO_ROOM_FADE
                    {
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "Fade")).AddArgument(a);
                    }
                    break;
                case 182:               // SO_RGB_ROOM_INTENSITY
                    {
                        var e = Pop();
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "DarkenPalette")).AddArguments(a, b, c, d, e);
                    }
                    break;

                case 183:               // SO_ROOM_SHADOW
                    {
                        var e = Pop();
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "SetShadowPalette")).AddArguments(a, b, c, d, e);
                    }
                    break;

                case 184:               // SO_SAVE_STRING
                    throw new NotSupportedException("save string not implemented");

                case 185:               // SO_LOAD_STRING
                    throw new NotSupportedException("load string not implemented");

                case 186:               // SO_ROOM_TRANSFORM
                    {
                        var d = Pop();
                        var c = Pop();
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "PalManipulateInit")).AddArguments(a, b, c, d);
                    }
                    break;

                case 187:               // SO_CYCLE_SPEED
                    {
                        var b = Pop();
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "CycleSpeed")).AddArguments(a, b);
                    }

                    break;
                case 213:               // SO_ROOM_NEW_PALETTE
                    {
                        var a = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "NewPalette")).AddArgument(a);
                    }
                    break;
                default:
                    throw new NotSupportedException(string.Format("RoomOps: default case {0}", subOp));
            }
            return exp.ToStatement();
        }

        protected Statement LoadRoom()
        {
            var room = Pop();
            return new MethodInvocation("LoadRoom").AddArgument(room).ToStatement();
        }

        protected Statement LoadRoomWithEgo()
        {
            var y = Pop();
            var x = Pop();
            Expression room, obj;
            PopRoomAndObject(out room, out obj);
            return new MethodInvocation("LoadRoomWithEgo").AddArguments(room, obj, x, y).ToStatement();
        }

        protected Statement SetBoxFlags()
        {
            var value = Pop();
            var args = GetStackList(65);
            return new MethodInvocation("SetBoxFlags").AddArguments(value, args).ToStatement();
        }

        protected Statement CreateBoxMatrix()
        {
            return new MethodInvocation("CreateBoxMatrix").ToStatement();
        }

        protected Statement PseudoRoom()
        {
            var args = GetStackList(100);
            var value = Pop();
            return new MethodInvocation("PseudoRoom").AddArguments(value, args).ToStatement();
        }

        protected Statement SetBoxSet()
        {
            return new MethodInvocation("SetBoxSet").ToStatement();
        }
    }
}

