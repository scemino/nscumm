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
        
            return new SwitchStatement(GetIndex(args, 0))
                .Add(new CaseStatement(113,
                    Push(new MethodInvocation("GetPixels").AddArguments(GetIndex(args, 1), GetIndex(args, 2)))))
                .Add(new CaseStatement(115,
                    Push(new MethodInvocation("GetSpecialBox").AddArguments(GetIndex(args, 1), GetIndex(args, 2)))))
                .Add(new CaseStatement(116,
                    Push(new MethodInvocation("CheckXYInBoxBounds").AddArguments(GetIndex(args, 3), GetIndex(args, 1), GetIndex(args, 2)))))
                .Add(new CaseStatement(206,
                    Push(new MethodInvocation("RemapPaletteColor").AddArguments(GetIndex(args, 1), GetIndex(args, 2), GetIndex(args, 3)))))
                .Add(new CaseStatement(207,
                    Push(new MemberAccess(Object(new MethodInvocation("GetObjectIndex").AddArgument(GetIndex(args, 1))), "X"))))
                .Add(new CaseStatement(208,
                    Push(new MemberAccess(Object(new MethodInvocation("GetObjectIndex").AddArgument(GetIndex(args, 1))), "Y"))))
                .Add(new CaseStatement(209,
                    Push(new MemberAccess(Object(new MethodInvocation("GetObjectIndex").AddArgument(GetIndex(args, 1))), "Width"))))
                .Add(new CaseStatement(210,
                    Push(new MemberAccess(Object(new MethodInvocation("GetObjectIndex").AddArgument(GetIndex(args, 1))), "Height"))))
                .Add(new CaseStatement(211,
                    Push(new MethodInvocation("GetKeyState").AddArgument(GetIndex(args, 1)))))
                .Add(new CaseStatement(212,
                    Push(new MemberAccess(GetActor(GetIndex(args, 1)), "Frame"))))
                .Add(new CaseStatement(213,
                    Push(new MemberAccess(Verb(GetIndex(args, 1)), "Left"))))
                .Add(new CaseStatement(214,
                    Push(new MemberAccess(Verb(GetIndex(args, 1)), "Top"))))
                .Add(new CaseStatement(215,
                    Push(new MethodInvocation("GetBoxFlag").AddArgument(GetIndex(args, 1)))));
        
        }

        protected virtual Statement KernelSetFunctions()
        {
            var args = GetStackList(30);

            return new SwitchStatement(GetIndex(args, 0))
                .Add(new CaseStatement(3))
                .Add(new CaseStatement(4,
                    new MethodInvocation("GrabCursor").AddArguments(GetIndex(args, 1), GetIndex(args, 2), GetIndex(args, 3), GetIndex(args, 4)).ToStatement()))
                .Add(new CaseStatement(5,
                    new MethodInvocation("FadeOut").AddArguments(GetIndex(args, 1)).ToStatement()))
                .Add(new CaseStatement(6,
                    new MethodInvocation("FadeIn").AddArguments(GetIndex(args, 1)).ToStatement()))
                .Add(new CaseStatement(8,
                    new MethodInvocation("StartManiac").ToStatement()))
                .Add(new CaseStatement(9,
                    new MethodInvocation("killAllScriptsExceptCurrent").ToStatement()))
                .Add(new CaseStatement(104,
                    new MethodInvocation("nukeFlObjects").AddArguments(GetIndex(args, 2), GetIndex(args, 3)).ToStatement()))
                .Add(new CaseStatement(107,
                    new MethodInvocation(new MemberAccess(GetActor(GetIndex(args, 1)), "SetScale")).AddArgument(GetIndex(args, 2)).ToStatement()))
                .Add(new CaseStatement(108))
                .Add(new CaseStatement(109,
                    new MethodInvocation("SetShadowPalette").AddArguments(GetIndex(args, 3), GetIndex(args, 4), GetIndex(args, 5), GetIndex(args, 1), GetIndex(args, 2)).ToStatement()))
                .Add(new CaseStatement(110, new MethodInvocation("ClearCharsetMask").ToStatement()))
                .Add(new CaseStatement(111, new MethodInvocation("ShadowMode").AddArguments(GetIndex(args, 2), GetIndex(args, 3)).ToStatement()))
                .Add(new CaseStatement(112,
                    new MethodInvocation("SetShadowPalette").AddArguments(GetIndex(args, 3), GetIndex(args, 4), GetIndex(args, 5), GetIndex(args, 1), GetIndex(args, 2), GetIndex(args, 6), GetIndex(args, 7)).ToStatement()))
                .Add(new CaseStatement(114, new MethodInvocation("SetDirtyColors").ToStatement()))
                .Add(new CaseStatement(117, new MethodInvocation("FreezeScripts").AddArgument(0x80).ToStatement()))
                .Add(new CaseStatement(119, new MethodInvocation("EnqueueObject").AddArguments(GetIndex(args, 1), GetIndex(args, 2), GetIndex(args, 3), GetIndex(args, 4), GetIndex(args, 5), GetIndex(args, 6), GetIndex(args, 7), GetIndex(args, 8)).ToStatement()))
                .Add(new CaseStatement(120, new MethodInvocation("SwapPalColors").AddArguments(GetIndex(args, 1), GetIndex(args, 2)).ToStatement()))
                .Add(new CaseStatement(122, new MethodInvocation("IMUSEDoCommand").AddArguments(args).ToStatement()))
                .Add(new CaseStatement(123, new MethodInvocation("CopyPalColor").AddArguments(GetIndex(args, 2), GetIndex(args, 1)).ToStatement()))
                .Add(new CaseStatement(124, new MethodInvocation("SaveSound").AddArguments(GetIndex(args, 1)).ToStatement()));
        }

        protected Statement GetDateTime()
        {
            return new MethodInvocation("GetDateTime").ToStatement();
        }
    }
}

