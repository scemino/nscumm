//
//  ScriptParser6_Resources.cs
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
        protected virtual Statement ResourceRoutines()
        {
            var subOp = ReadByte();
            var exp = new MethodInvocation("ResourceRoutines");
            switch (subOp)
            {
                case 100:               // SO_LOAD_SCRIPT
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadScript")).AddArgument(resId);
                    }
                    break;
                case 101:               // SO_LOADSound
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadSound")).AddArgument(resId);
                    }
                    break;
                case 102:               // SO_LOAD_COSTUME
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadCostume")).AddArgument(resId);
                    }
                    break;
                case 103:               // SO_LOAD_ROOM
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadRoom")).AddArgument(resId);
                    }
                    break;
                case 104:               // SO_NUKE_SCRIPT
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "NukeScript")).AddArgument(resId);
                    }
                    break;
                case 105:               // SO_NUKESound
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "NukeSound")).AddArgument(resId);
                    }
                    break;
                case 106:               // SO_NUKE_COSTUME
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "NukeCostume")).AddArgument(resId);
                    }
                    break;
                case 107:               // SO_NUKE_ROOM
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "NukeRoom")).AddArgument(resId);
                    }
                    break;
                case 108:               // SO_LOCK_SCRIPT
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "LockScript")).AddArgument(resId);
                    }
                    break;
                case 109:               // SO_LOCKSound
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "LockSound")).AddArgument(resId);
                    }
                    break;
                case 110:               // SO_LOCK_COSTUME
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "LockCostume")).AddArgument(resId);
                    }
                    break;
                case 111:               // SO_LOCK_ROOM
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "LockRoom")).AddArgument(resId);
                    }
                    break;
                case 112:               // SO_UNLOCK_SCRIPT
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "UnlockScript")).AddArgument(resId);
                    }
                    break;
                case 113:               // SO_UNLOCKSound
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "UnlockSound")).AddArgument(resId);
                    }
                    break;
                case 114:               // SO_UNLOCK_COSTUME
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "UnlockCostume")).AddArgument(resId);
                    }
                    break;
                case 115:               // SO_UNLOCK_ROOM
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "UnlockRoom")).AddArgument(resId);
                    }
                    break;
                case 116:               // SO_CLEAR_HEAP
                    /* this is actually a scumm message */
                    throw new NotSupportedException("Clear heap not working yet");
                case 117:               // SO_LOAD_CHARSET
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadCharset")).AddArgument(resId);
                    }
                    break;
                case 118:               // SO_NUKE_CHARSET
                    {
                        var resId = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "NukeCharset")).AddArgument(resId);
                    }
                    break;
                case 119:               // SO_LOAD_OBJECT
                    {
                        exp = new MethodInvocation(new MemberAccess(exp, "LoadFlyingObject")).AddArguments(Pop(), Pop());
                        break;
                    }
                default:
                    throw new NotSupportedException(string.Format("ResourceRoutines: default case {0}", subOp));
            }
            return exp.ToStatement();
        }
    }
}

