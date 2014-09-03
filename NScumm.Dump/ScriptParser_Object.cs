using System;
using System.Collections.Generic;
using NScumm.Core;

namespace NScumm.Tmp
{
    partial class ScriptParser
    {
        IEnumerable<Statement> SetState()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var state = GetVarOrDirectByte(OpCodeParameter.Param2);
            yield return new BinaryExpression(new MemberAccess(
                    new ElementAccess("Objects", obj),
                    "State"),
                Operator.Assignment,
                state).ToStatement();
        }

        IEnumerable<Statement> SetOwnerOf()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var owner = GetVarOrDirectByte(OpCodeParameter.Param2);

            yield return new BinaryExpression(new MemberAccess(
                    new ElementAccess("Objects", obj),
                    "Owner"),
                Operator.Assignment,
                owner).ToStatement();
        }

        IEnumerable<Statement> IfState()
        {
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = GetVarOrDirectByte(OpCodeParameter.Param2);
            yield return JumpRelative(
                new BinaryExpression(
                    new MemberAccess(
                        new ElementAccess("Objects", a),
                        "State"),
                    Operator.Equals,
                    b));
        }

        IEnumerable<Statement> IfNotState()
        {
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = GetVarOrDirectByte(OpCodeParameter.Param2);
            yield return JumpRelative(new BinaryExpression(
                    new MemberAccess(
                        new ElementAccess("Objects", a),
                        "State"),
                    Operator.Inequals,
                    b));
        }

        IEnumerable<Statement> IfClassOfIs()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var args = new List<Expression>();
            while ((_opCode = ReadByte()) != 0xFF)
            {
                var cls = GetVarOrDirectWord(OpCodeParameter.Param1);
                args.Add(cls);
            }
            yield return JumpRelative(
                new MemberAccess(
                    new ElementAccess("Objects", obj),
                    new MethodInvocation("IsOfClass").AddArguments(args)));
        }

        IEnumerable<Statement> GetObjectOwner()
        {
            var indexExp = GetResultIndexExpression();
            yield return SetResultExpression(indexExp, new MethodInvocation("GetOwner").AddArgument(GetVarOrDirectWord(OpCodeParameter.Param1)));
        }

        IEnumerable<Statement> SetObjectName()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return new BinaryExpression(
                new MemberAccess(
                    new ElementAccess("Objects", obj),
                    "Name"),
                Operator.Assignment,
                ReadCharacters()).ToStatement();
        }

        IEnumerable<Statement> SetClass()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var args = new List<Expression>();
            while ((_opCode = ReadByte()) != 0xFF)
            {
                var cls = GetVarOrDirectWord(OpCodeParameter.Param1);
                args.Add(cls);
            }
            yield return new MethodInvocation("SetClass").AddArgument(obj).AddArguments(args).ToStatement();
        }

        IEnumerable<Statement> StartObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var script = GetVarOrDirectByte(OpCodeParameter.Param2);
            var data = GetWordVarArgs();
            yield return new MethodInvocation("StartObject").AddArguments(obj, script).AddArguments(data).ToStatement();
        }

        IEnumerable<Statement> PickupObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return new MethodInvocation("PickupObject").AddArgument(obj).ToStatement();
        }

        IEnumerable<Statement> FindObject()
        {
            var resultIndexExp = GetResultIndexExpression();
            var x = GetVarOrDirectByte(OpCodeParameter.Param1);
            var y = GetVarOrDirectByte(OpCodeParameter.Param2);
            yield return SetResultExpression(
                resultIndexExp,
                new MethodInvocation("FindObject").
				AddArguments(x, y));
        }

        IEnumerable<Statement> StopObjectCode()
        {
            yield return new MethodInvocation("StopObjectCode").ToStatement();
        }

        IEnumerable<Statement> DrawObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var xpos = GetVarOrDirectWord(OpCodeParameter.Param2);
            var ypos = GetVarOrDirectWord(OpCodeParameter.Param3);
            yield return new MethodInvocation("DrawObject").AddArguments(obj, xpos, ypos).ToStatement();
        }
    }
}

