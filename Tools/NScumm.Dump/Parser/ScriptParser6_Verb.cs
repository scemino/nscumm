//
//  ScriptParser6_Verb.cs
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
        protected readonly SimpleName CurrentVerb = new SimpleName("CurrentVerb");
        readonly SimpleName Verbs = new SimpleName("Verbs");

        Expression Verb(Expression index)
        {
            return new ElementAccess(Verbs, index);
        }

        protected Statement GetVerbFromXY()
        {
            var y = Pop();
            var x = Pop();
            return Push(new MethodInvocation("FindVerbAt").AddArguments(x, y));
        }

        protected Statement GetVerbEntrypoint()
        {
            var entry = Pop();
            var verb = Pop();
            return Push(new MethodInvocation("GetVerbEntrypoint").AddArguments(verb, entry));
        }

        protected virtual Statement VerbOps()
        {
            var subOp = ReadByte();
            if (subOp == 196)
            {
                return new BinaryExpression(CurrentVerb, Operator.Assignment, Pop()).ToStatement();
            }
            var verb = (Expression)CurrentVerb;
            switch (subOp)
            {
                case 124:               // SO_VERB_IMAGE
                    {
                        verb = new MethodInvocation(new MemberAccess(verb, "SetImage")).AddArgument(Pop());
                    }
                    break;
                case 125:               // SO_VERB_NAME
                    verb = new MethodInvocation(new MemberAccess(verb, "Name")).AddArgument(ReadCharacters());
                    break;
                case 126:               // SO_VERB_COLOR
                    verb = new MethodInvocation(new MemberAccess(verb, "Color")).AddArgument(Pop());
                    break;
                case 127:               // SO_VERB_HICOLOR
                    verb = new MethodInvocation(new MemberAccess(verb, "HiColor")).AddArgument(Pop());
                    break;
                case 128:               // SO_VERB_AT
                    {
                        var top = Pop();
                        var left = Pop();
                        verb = new MethodInvocation(new MemberAccess(verb, "At")).AddArguments(left, top);
                    }
                    break;
                case 129:               // SO_VERB_ON
                    verb = new MethodInvocation(new MemberAccess(verb, "On"));
                    break;
                case 130:               // SO_VERB_OFF
                    verb = new MethodInvocation(new MemberAccess(verb, "Off"));
                    break;
                case 131:               // SO_VERB_DELETE
                    verb = new MethodInvocation(new MemberAccess(verb, "Delete"));
                    break;
                case 132:               // SO_VERB_NEW
                    verb = new MethodInvocation(new MemberAccess(verb, "New"));
                    break;
                case 133:               // SO_VERB_DIMCOLOR
                    verb = new MethodInvocation(new MemberAccess(verb, "DimColor")).AddArgument(Pop());
                    break;
                case 134:               // SO_VERB_DIM
                    verb = new MethodInvocation(new MemberAccess(verb, "Dim"));
                    break;
                case 135:               // SO_VERB_KEY
                    verb = new MethodInvocation(new MemberAccess(verb, "Key")).AddArgument(Pop());
                    break;
                case 136:               // SO_VERB_CENTER
                    verb = new MethodInvocation(new MemberAccess(verb, "Center"));
                    break;
                case 137:               // SO_VERB_NAME_STR
                    {
                        var a = Pop();
                        verb = new MethodInvocation(new MemberAccess(verb, "Name")).AddArguments(a, ReadCharacters());
                    }
                    break;
                case 139:               // SO_VERB_IMAGE_IN_ROOM
                    {
                        var b = Pop();
                        var a = Pop();
                        verb = new MethodInvocation(new MemberAccess(verb, "ImageInRoom")).AddArguments(a, b);
                    }
                    break;
                case 140:               // SO_VERB_BAKCOLOR
                    verb = new MethodInvocation(new MemberAccess(verb, "BackColor")).AddArgument(Pop());
                    break;
                case 255:
                    verb = new MethodInvocation(new MemberAccess(verb, "Draw"));
                    break;
                default:
                    throw new NotSupportedException(string.Format("VerbOps: default case {0}", subOp));
            }
            return verb.ToStatement();
        }

        protected Statement SaveRestoreVerbs()
        {
            var c = Pop();
            var b = Pop();
            var a = Pop();

            var subOp = ReadByte();
            if (Game.Version == 8)
            {
                subOp = (subOp - 141) + 0xB4;
            }

            switch (subOp)
            {
                case 141:               // SO_SAVE_VERBS
                    return new MethodInvocation("SaveVerbs").AddArguments(a, b, c).ToStatement();
                case 142:               // SO_RESTORE_VERBS
                    return new MethodInvocation("ResoreVerbs").AddArguments(a, b, c).ToStatement();
                case 143:               // SO_DELETE_VERBS
                    return new MethodInvocation("DeleteVerbs").AddArguments(a, b, c).ToStatement();
                default:
                    throw new NotSupportedException(string.Format("SaveRestoreVerbs: default case: {0}", subOp));
            }
        }
    }
}

