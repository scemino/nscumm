//
//  ScriptParser6_Graphics.cs
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
        protected virtual Statement CursorCommand()
        {
            var exp = new MethodInvocation("CursorCommand");

            var subOp = ReadByte();

            switch (subOp)
            {
                case 0x90:              // SO_CURSOR_ON Turn cursor on
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursor")).AddArgument(true);
                    break;
                case 0x91:              // SO_CURSOR_OFF Turn cursor off
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursor")).AddArgument(false);
                    break;
                case 0x92:              // SO_USERPUT_ON
                    exp = new MethodInvocation(new MemberAccess(exp, "SetUserInput")).AddArgument(true);
                    break;
                case 0x93:              // SO_USERPUT_OFF
                    exp = new MethodInvocation(new MemberAccess(exp, "SetUserInput")).AddArgument(false);
                    break;
                case 0x94:              // SO_CURSOR_SOFT_ON Turn soft cursor on
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursorSoft")).AddArgument(true);
                    break;
                case 0x95:              // SO_CURSOR_SOFT_OFF Turn soft cursor off
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursorSoft")).AddArgument(false);
                    break;
                case 0x96:              // SO_USERPUT_SOFT_ON
                    exp = new MethodInvocation(new MemberAccess(exp, "SetUserInputSoft")).AddArgument(true);
                    break;
                case 0x97:              // SO_USERPUT_SOFT_OFF
                    exp = new MethodInvocation(new MemberAccess(exp, "SetUserInputSoft")).AddArgument(false);
                    break;
                case 0x99:              // SO_CURSOR_IMAGE Set cursor image
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursorImage")).AddArguments(Pop(), Pop());
                    break;
                case 0x9A:              // SO_CURSOR_HOTSPOT Set cursor hotspot
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursorHotspot")).AddArguments(Pop(), Pop());
                    break;
                case 0x9C:              // SO_CHARSET_SET
                    exp = new MethodInvocation(new MemberAccess(exp, "InitCharset")).AddArgument(Pop());
                    break;
                case 0x9D:              // SO_CHARSET_COLOR
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCharsetColor")).AddArgument(GetStackList(16));
                    break;
                case 0xD6:              // SO_CURSOR_TRANSPARENT Set cursor transparent color
                    exp = new MethodInvocation(new MemberAccess(exp, "SetCursorTransparentColor")).AddArgument(Pop());
                    break;
                default:
                    throw new NotSupportedException(string.Format("CursorCommand: default case {0:X2}", subOp));
            }
            return exp.ToStatement();
        }

        protected Statement DrawObject()
        {
            var state = Pop();
            var obj = Pop();
            return new MethodInvocation("SetObjectState").AddArguments(obj, state).ToStatement();
        }

        protected Statement DrawObjectAt()
        {
            var y = Pop();
            var x = Pop();
            var obj = Pop();
            return new MethodInvocation("SetObjectState").AddArguments(obj, x, y).ToStatement();
        }

        protected Statement DrawBlastObject()
        {
            var args = GetStackList(19);
            var e = Pop();
            var d = Pop();
            var c = Pop();
            var b = Pop();
            var a = Pop();
            return new MethodInvocation("DrawBlastObject").AddArguments(a, b, c, d, e, args).ToStatement();
        }

        protected Statement DrawBox()
        {
            var color = Pop();
            var y2 = Pop();
            var x2 = Pop();
            var y = Pop();
            var x = Pop();
            return new MethodInvocation("DrawBox").AddArguments(x, y, x2, y2, color).ToStatement();
        }
    }
}

