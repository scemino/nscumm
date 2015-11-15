//
//  ScriptParser_Expression.cs
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

using System;
using System.Collections.Generic;
using NScumm.Core;
using NScumm.Scumm;

namespace NScumm.Dump
{
    partial class ScriptParser3
    {
        Statement Add()
        {
            var indexExp = GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable(indexExp);
            return SetResultExpression(indexExp, new BinaryExpression(a, Operator.Add, b)).ToStatement();
        }

        Statement Subtract()
        {
            var indexExp = GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(indexExp), Operator.Subtract, a)).ToStatement();
        }

        Statement Multiply()
        {
            var indexExp = (IntegerLiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(indexExp.Value), Operator.Multiply, a)).ToStatement();
        }

        Statement Divide()
        {
            var indexExp = (IntegerLiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(indexExp.Value), Operator.Divide, a)).ToStatement();
        }

        Statement Increment()
        {
            var index = GetResultIndexExpression();
            return SetResultExpression(index, 
                new BinaryExpression(
                    ReadVariable(index),
                    Operator.Add,
                    1.ToLiteral())).ToStatement();
        }

        Statement Decrement()
        {
            var index = GetResultIndexExpression();
            return SetResultExpression(index, new UnaryExpression(ReadVariable(index), Operator.PostDecrement)).ToStatement();
        }

        Statement And()
        {
            var indexExp = (IntegerLiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(indexExp.Value), Operator.And, a)).ToStatement();
        }

        Statement Or()
        {
            var indexExp = (IntegerLiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(indexExp.Value), Operator.Or, a)).ToStatement();
        }

        Statement NotEqualZero()
        {
            var var = ReadWord();
            var a = ReadVariable(var);

            return JumpRelative(
                new BinaryExpression(
                    a,
                    Operator.Inequals,
                    0.ToLiteral()));
        }

        Statement EqualZero()
        {
            var var = ReadWord();
            var a = ReadVariable(var);
            //JumpRelative (a == 0);
            return JumpRelative(
                new BinaryExpression(
                    a,
                    Operator.Equals,
                    0.ToLiteral()));
        }

        Statement IsNotEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return JumpRelative(new BinaryExpression(a, Operator.Inequals, b));
        }

        Statement IsGreater()
        {
            var a = ReadVariable(ReadWord());
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return JumpRelative(new BinaryExpression(b, Operator.Greater, a));
        }

        Statement IsGreaterEqual()
        {
            var a = ReadVariable(ReadWord());
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return JumpRelative(new BinaryExpression(b, Operator.GreaterOrEquals, a));
        }

        Statement IsLess()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return JumpRelative(new BinaryExpression(b, Operator.Lower, a));
        }

        Statement IsLessEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return JumpRelative(new BinaryExpression(b, Operator.LowerOrEquals, a));
        }

        Statement IsEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            return 
                JumpRelative(
                new BinaryExpression(a, Operator.Equals, b));
        }

        Statement JumpRelative()
        {
            return JumpRelative(false.ToLiteral());
        }

        Statement JumpRelative(Expression condition)
        {
            var offset = (short)ReadWord();
            var binExp = condition as BinaryExpression;
            if (binExp != null)
            {
                condition = binExp.Not();
            }
            else
            {
                condition = true.ToLiteral();
            }
            return new JumpStatement(condition, (int)_br.BaseStream.Position + offset);
        }

        Statement ExpressionFunc()
        {
            var stack = new Stack<Expression>();
            var resultExp = GetResultIndexExpression();
            if (!(resultExp is IntegerLiteralExpression))
                throw new InvalidOperationException(string.Format("ResultExpression was expected to be an integer but was a {0} instead", resultExp.GetType()));
            var dst = ((IntegerLiteralExpression)resultExp).Value;
            while ((_opCode = ReadByte()) != 0xFF)
            {
                switch (_opCode & 0x1F)
                {
                    case 1:
					// var
                        stack.Push(GetVarOrDirectWord(OpCodeParameter.Param1));
                        break;

                    case 2:
					// add
                        {
                            var i = stack.Pop();
                            stack.Push(new BinaryExpression(i, Operator.Add, stack.Pop()));
                        }
                        break;

                    case 3:
					// sub
                        {
                            var i = stack.Pop();
                            stack.Push(new BinaryExpression(stack.Pop(), Operator.Subtract, i));
                        }
                        break;

                    case 4:
					// mul
                        {
                            var i = stack.Pop();
                            stack.Push(new BinaryExpression(i, Operator.Multiply, stack.Pop()));
                        }
                        break;

                    case 5:
					// div
                        {
                            var i = stack.Pop();
                            stack.Push(new BinaryExpression(stack.Pop(), Operator.Divide, i));
                        }
                        break;

                    case 6:
					// normal opcode
                        {
                            _opCode = ReadByte();
                            var statement = ExecuteOpCode();
                            var expStatement = statement as ExpressionStatement;
                            if (expStatement == null)
                                throw new InvalidOperationException("ExpressionStatement expected");
                            var binExp = expStatement.Expression as BinaryExpression;
                            if (binExp == null)
                                throw new InvalidOperationException("BinaryExpression expected");
                            if (!((binExp.Left is ElementAccess) && ((ElementAccess)binExp.Left).Target is SimpleName))
                                throw new InvalidOperationException("Variables[0] was expected expected");
                            stack.Push(binExp.Right);
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            var exp = new BinaryExpression(GetResultIndex(dst), Operator.Assignment, stack.Pop());
            return exp.ToStatement();
        }
    }
}

