//
//  ScriptParser6_Misc.cs
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
        protected Statement PrintLine()
        {
            return DecodeParseString(new MethodInvocation("PrintLine"), false).ToStatement();
        }

        protected Statement PrintText()
        {
            return DecodeParseString(new MethodInvocation("PrintText"), false).ToStatement();
        }

        protected Statement PrintDebug()
        {
            return DecodeParseString(new MethodInvocation("PrintDebug"), false).ToStatement();
        }

        protected Statement PrintSystem()
        {
            return DecodeParseString(new MethodInvocation("PrintSystem"), false).ToStatement();
        }

        protected Statement PrintActor()
        {
            return DecodeParseString(new MethodInvocation("PrintActor"), true).ToStatement();
        }

        protected Statement PrintEgo()
        {
            return DecodeParseString(new MethodInvocation("PrintActor"), true).ToStatement();
        }

        protected virtual Expression DecodeParseString(Expression target, bool withActor)
        {
            var b = ReadByte();

            switch (b)
            {
                case 65:                // SO_AT
                    {
                        var y = Pop();
                        var x = Pop();
                        target = new MethodInvocation(new MemberAccess(target, "At")).AddArguments(x, y);
                    }
                    break;
                case 66:                // SO_COLOR
                    {
                        var color = Pop();
                        target = new MethodInvocation(new MemberAccess(target, "Color")).AddArguments(color);
                    }
                    break;
                case 67:                // SO_CLIPPED
                    {
                        var right = Pop();
                        target = new MethodInvocation(new MemberAccess(target, "Clipped")).AddArguments(right);
                    }
                    break;
                case 69:                // SO_CENTER
                    target = new MethodInvocation(new MemberAccess(target, "Center"));
                    break;
                case 71:                // SO_LEFT
                    target = new MethodInvocation(new MemberAccess(target, "Left"));
                    break;
                case 72:                // SO_OVERHEAD
                    target = new MethodInvocation(new MemberAccess(target, "OverHead"));
                    break;
                case 74:                // SO_MUMBLE
                    target = new MethodInvocation(new MemberAccess(target, "Mumble"));
                    break;
                case 75:                // SO_TEXTSTRING
                    target = new MethodInvocation(new MemberAccess(target, "Text")).AddArgument(ReadCharacters());
                    break;
                case 0xFE:
                    target = new MethodInvocation(new MemberAccess(target, "LoadDefault"));
                    if (withActor)
                    {
                        var actor = Pop();
                        target = new MethodInvocation(new MemberAccess(target, "Actor")).AddArgument(actor);
                    }
                    break;
                case 0xFF:
                    target = new MethodInvocation(new MemberAccess(target, "SaveDefault"));
                    break;
                default:
                    throw new NotSupportedException(string.Format("DecodeParseString: default case 0x{0:X}", b));
            }
            return target;
        }

        protected Statement SystemOps()
        {
            var subOp = ReadByte();

            switch (subOp)
            {
                case 158:               // SO_RESTART
                    return new MethodInvocation("Restart").ToStatement();
                case 159:               // SO_PAUSE
                    return new MethodInvocation("PauseGame").ToStatement();
                case 160:               // SO_QUIT
                    return new MethodInvocation("Quit").ToStatement();
                default:
                    return new MethodInvocation(string.Format("SystemOps invalid case {0}", subOp)).ToStatement();
            }
        }

        Expression GetIndex(Expression exp, int index)
        {
            return new ElementAccess(exp, index);
        }

        protected Statement SetBlastObjectWindow()
        {
            var d = Pop();
            var c = Pop();
            var b = Pop();
            var a = Pop();
            return new MethodInvocation("SetBlastObjectWindow").AddArguments(a, b, c, d).ToStatement();
        }

        protected virtual Statement KernelGetFunctions()
        {
            var args = GetStackList(30);
            return new MethodInvocation("KernelGetFunctions").AddArgument(args).ToStatement();
        }

        protected virtual Statement KernelSetFunctions()
        {
            var args = GetStackList(30);
            return new MethodInvocation("KernelSetFunctions").AddArgument(args).ToStatement();
        }

        protected Statement GetDateTime()
        {
            return new MethodInvocation("GetDateTime").ToStatement();
        }

        protected Statement Dummy()
        {
            return new MethodInvocation("Dummy").ToStatement();
        }

        protected Statement GetPixel()
        {
            var y = Pop();
            var x = Pop();
            return Push(new MethodInvocation("GetPixel").AddArguments(x, y));
        }
    }
}

