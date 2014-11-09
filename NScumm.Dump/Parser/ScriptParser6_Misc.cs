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
        Statement PrintSystem()
        {
            return DecodeParseString(new MethodInvocation("PrintSystem"), 0).ToStatement();
        }

        Expression DecodeParseString(Expression target, int n)
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
                    if (n != 0)
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

        Statement SystemOps()
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
                    throw new NotSupportedException(string.Format("SystemOps invalid case {0}", subOp));
            }
        }

        Statement SetBlastObjectWindow()
        {
            var d = Pop();
            var c = Pop();
            var b = Pop();
            var a = Pop();
            return new MethodInvocation("SetBlastObjectWindow").AddArguments(a, b, c, d).ToStatement();
        }
    }
}

