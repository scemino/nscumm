//
//  ScriptParser_Object.cs
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

using System.Collections.Generic;
using NScumm.Core;
using NScumm.Scumm;

namespace NScumm.Dump
{
    partial class ScriptParser3
    {
        Statement SetState()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var state = GetVarOrDirectByte(OpCodeParameter.Param2);
            return new BinaryExpression(new MemberAccess(
                    new ElementAccess("Objects", obj),
                    "State"),
                Operator.Assignment,
                state).ToStatement();
        }

        Statement SetOwnerOf()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var owner = GetVarOrDirectByte(OpCodeParameter.Param2);

            return new BinaryExpression(new MemberAccess(
                    new ElementAccess("Objects", obj),
                    "Owner"),
                Operator.Assignment,
                owner).ToStatement();
        }

        Statement IfState()
        {
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = GetVarOrDirectByte(OpCodeParameter.Param2);
            return JumpRelative(
                new BinaryExpression(
                    new MemberAccess(
                        new ElementAccess("Objects", a),
                        "State"),
                    Operator.Equals,
                    b));
        }

        Statement IfNotState()
        {
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = GetVarOrDirectByte(OpCodeParameter.Param2);
            return JumpRelative(new BinaryExpression(
                    new MemberAccess(
                        new ElementAccess("Objects", a),
                        "State"),
                    Operator.Inequals,
                    b));
        }

        Statement IfClassOfIs()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var args = new List<Expression>();
            while ((_opCode = ReadByte()) != 0xFF)
            {
                var cls = GetVarOrDirectWord(OpCodeParameter.Param1);
                args.Add(cls);
            }
            return JumpRelative(
                new MethodInvocation(
                    new MemberAccess(
                        new ElementAccess("Objects", obj),
                        "IsOfClass")).AddArguments(args));
        }

        Statement GetObjectOwner()
        {
            var indexExp = GetResultIndexExpression();
            return SetResultExpression(indexExp, new MethodInvocation("GetOwner").AddArgument(GetVarOrDirectWord(OpCodeParameter.Param1))).ToStatement();
        }

        Statement SetObjectName()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            return new BinaryExpression(
                new MemberAccess(
                    new ElementAccess("Objects", obj),
                    "Name"),
                Operator.Assignment,
                ReadCharacters()).ToStatement();
        }

        Statement SetClass()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var args = new List<Expression>();
            while ((_opCode = ReadByte()) != 0xFF)
            {
                var cls = GetVarOrDirectWord(OpCodeParameter.Param1);
                args.Add(cls);
            }
            return new MethodInvocation("SetClass").AddArgument(obj).AddArguments(args).ToStatement();
        }

        Statement StartObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var script = GetVarOrDirectByte(OpCodeParameter.Param2);
            var data = GetWordVarArgs();
            return new MethodInvocation("StartObject").AddArguments(obj, script).AddArguments(data).ToStatement();
        }

        protected virtual Statement PickupObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            return new MethodInvocation("PickupObject").AddArgument(obj).ToStatement();
        }

        Statement FindObject()
        {
            var resultIndexExp = GetResultIndexExpression();
            var x = GetVarOrDirectByte(OpCodeParameter.Param1);
            var y = GetVarOrDirectByte(OpCodeParameter.Param2);
            return SetResultExpression(
                resultIndexExp,
                new MethodInvocation("FindObject").
                AddArguments(x, y)).ToStatement();
        }

        Statement StopObjectCode()
        {
            return new MethodInvocation("StopObjectCode").ToStatement();
        }

        protected virtual Statement DrawObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var xpos = GetVarOrDirectWord(OpCodeParameter.Param2);
            var ypos = GetVarOrDirectWord(OpCodeParameter.Param3);
            return new MethodInvocation("DrawObject").AddArguments(obj, xpos, ypos).ToStatement();
        }
    }
}

