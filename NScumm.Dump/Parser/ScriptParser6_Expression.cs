//
//  ScriptParser6_Expression.cs
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
        Statement Jump()
        {
            return new JumpStatement(true.ToLiteral(), ReadWordSigned() + (int)_br.BaseStream.Position);
        }

        Statement If()
        {
            return new JumpStatement(Pop(), ReadWordSigned() + (int)_br.BaseStream.Position);
        }

        Statement IfNot()
        {
            return new JumpStatement(new UnaryExpression(Pop(), Operator.Not), ReadWordSigned() + (int)_br.BaseStream.Position);
        }

        Statement Dup()
        {
            var exp = Pop();
            return Push(exp, exp);
        }

        Statement Not()
        {
            var exp = Pop();
            return Push(Not(exp));
        }

        Expression Not(Expression exp)
        {
            return new UnaryExpression(exp, Operator.Not);
        }

        Statement Eq()
        {
            var a = Pop();
            var b = Pop();
            return Push(new BinaryExpression(a, Operator.Equals, b));
        }

        Statement NEq()
        {
            var a = Pop();
            var b = Pop();
            return Push(new BinaryExpression(a, Operator.Inequals, b));
        }

        Statement Add()
        {
            return BinaryExpression(Operator.Add);
        }

        Statement Sub()
        {
            return BinaryExpression(Operator.Subtract);
        }

        Statement Mul()
        {
            return BinaryExpression(Operator.Multiply);
        }

        Statement Div()
        {
            return BinaryExpression(Operator.Divide);
        }

        Statement Gt()
        {
            return BinaryExpression(Operator.Greater);
        }

        Statement Ge()
        {
            return BinaryExpression(Operator.GreaterOrEquals);
        }

        Statement Lt()
        {
            return BinaryExpression(Operator.Lower);
        }

        Statement Le()
        {
            return BinaryExpression(Operator.LowerOrEquals);
        }

        Statement Land()
        {
            return BinaryExpression(Operator.And);
        }

        Statement Lor()
        {
            return BinaryExpression(Operator.Or);
        }

        Statement PopStatement()
        {
            return Pop().ToStatement();
        }

        Statement BinaryExpression(Operator op)
        {
            var a = Pop();
            return Push(new BinaryExpression(Pop(), op, a));
        }

        Statement IsAnyOf()
        {
            var list = GetStackList(100);
            var val = Pop();
            return new MethodInvocation("IsAnyOf").AddArguments(list, val).ToStatement();
        }

        Statement IfClassOfIs()
        {
            var list = GetStackList(16);
            var @class = Pop();
            return Push(new MethodInvocation("IfClassOfIs").AddArguments(list, @class));
        }

        Statement Abs()
        {
            return Push(new MethodInvocation("Abs").AddArgument(Pop()));
        }
    }
}

