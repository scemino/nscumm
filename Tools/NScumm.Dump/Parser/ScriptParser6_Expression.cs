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

namespace NScumm.Dump
{
    partial class ScriptParser6
    {
        protected Statement Jump()
        {
            return new JumpStatement(true.ToLiteral(), ReadWordSigned() + (int)_br.BaseStream.Position);
        }

        protected Statement If()
        {
            return new JumpStatement(Pop(), ReadWordSigned() + (int)_br.BaseStream.Position);
        }

        protected Statement IfNot()
        {
            return new JumpStatement(new UnaryExpression(Pop(), Operator.Not), ReadWordSigned() + (int)_br.BaseStream.Position);
        }

        protected Statement Dup()
        {
            var exp = Pop();
            return new MethodInvocation("Dup").AddArgument(exp).ToStatement();
        }

        protected Statement Not()
        {
            var exp = Pop();
            return Push(Not(exp));
        }

        Expression Not(Expression exp)
        {
            return new UnaryExpression(exp, Operator.Not);
        }

        protected Statement Eq()
        {
            var a = Pop();
            var b = Pop();
            return Push(new BinaryExpression(a, Operator.Equals, b));
        }

        protected Statement NEq()
        {
            var a = Pop();
            var b = Pop();
            return Push(new BinaryExpression(a, Operator.Inequals, b));
        }

        protected Statement Add()
        {
            return BinaryExpression(Operator.Add);
        }

        protected Statement Sub()
        {
            return BinaryExpression(Operator.Subtract);
        }

        protected Statement Mul()
        {
            return BinaryExpression(Operator.Multiply);
        }

        protected Statement Div()
        {
            return BinaryExpression(Operator.Divide);
        }

        protected Statement Gt()
        {
            return BinaryExpression(Operator.Greater);
        }

        protected Statement Ge()
        {
            return BinaryExpression(Operator.GreaterOrEquals);
        }

        protected Statement Lt()
        {
            return BinaryExpression(Operator.Lower);
        }

        protected Statement Le()
        {
            return BinaryExpression(Operator.LowerOrEquals);
        }

        protected Statement Land()
        {
            return BinaryExpression(Operator.And);
        }

        protected Statement Lor()
        {
            return BinaryExpression(Operator.Or);
        }

        protected Statement Band()
        {
            return BinaryExpression(Operator.BitwiseAnd);
        }

        protected Statement Bor()
        {
            return BinaryExpression(Operator.BitwiseOr);
        }

        protected Statement Mod()
        {
            return BinaryExpression(Operator.Modulus);
        }

        protected Statement PopStatement()
        {
            return Pop().ToStatement();
        }

        protected Statement BinaryExpression(Operator op)
        {
            var a = Pop();
            return Push(new BinaryExpression(Pop(), op, a));
        }

        protected Statement IsAnyOf()
        {
            var list = GetStackList(100);
            var val = Pop();
            return new MethodInvocation("IsAnyOf").AddArguments(list, val).ToStatement();
        }

        protected Statement IfClassOfIs()
        {
            var list = GetStackList(16);
            var @class = Pop();
            return Push(new MethodInvocation("IfClassOfIs").AddArguments(list, @class));
        }

        protected Statement Abs()
        {
            return Push(new MethodInvocation("Abs").AddArgument(Pop()));
        }
    }
}

