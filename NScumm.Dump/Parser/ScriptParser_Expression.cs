using System;
using System.Collections.Generic;
using NScumm.Core;
using System.Linq;

namespace NScumm.Dump
{
    partial class ScriptParser
    {
        IEnumerable<Statement> Add()
        {
            var indexExp = GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable(indexExp);
            yield return SetResultExpression(indexExp, new BinaryExpression(a, Operator.Add, b)).ToStatement();
        }

        IEnumerable<Statement> Subtract()
        {
            var indexExp = GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(indexExp), Operator.Minus, a)).ToStatement();
        }

        IEnumerable<Statement> Multiply()
        {
            var indexExp = (IntegerLiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(indexExp.Value), Operator.Multiply, a)).ToStatement();
        }

        IEnumerable<Statement> Divide()
        {
            var indexExp = (IntegerLiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(indexExp.Value), Operator.Divide, a)).ToStatement();
        }

        IEnumerable<Statement> Increment()
        {
            var index = GetResultIndexExpression();
            yield return SetResultExpression(index, 
                new BinaryExpression(
                    ReadVariable(index),
                    Operator.Add,
                    1.ToLiteral())).ToStatement();
        }

        IEnumerable<Statement> Decrement()
        {
            var index = GetResultIndexExpression();
            yield return SetResultExpression(index, new UnaryExpression(ReadVariable(index), Operator.PostDecrement)).ToStatement();
        }

        IEnumerable<Statement> And()
        {
            var indexExp = (IntegerLiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(indexExp.Value), Operator.And, a)).ToStatement();
        }

        IEnumerable<Statement> Or()
        {
            var indexExp = (IntegerLiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(indexExp.Value), Operator.Or, a)).ToStatement();
        }

        IEnumerable<Statement> NotEqualZero()
        {
            var var = ReadWord();
            var a = ReadVariable(var);

            yield return JumpRelative(
                new BinaryExpression(
                    a,
                    Operator.Inequals,
                    0.ToLiteral()));
        }

        IEnumerable<Statement> EqualZero()
        {
            var var = ReadWord();
            var a = ReadVariable(var);
            //JumpRelative (a == 0);
            yield return JumpRelative(
                new BinaryExpression(
                    a,
                    Operator.Equals,
                    0.ToLiteral()));
        }

        IEnumerable<Statement> IsNotEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return JumpRelative(new BinaryExpression(a, Operator.Inequals, b));
        }

        IEnumerable<Statement> IsGreater()
        {
            var a = ReadVariable(ReadWord());
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return JumpRelative(new BinaryExpression(b, Operator.Greater, a));
        }

        IEnumerable<Statement> IsGreaterEqual()
        {
            var a = ReadVariable(ReadWord());
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return JumpRelative(new BinaryExpression(b, Operator.GreaterOrEquals, a));
        }

        IEnumerable<Statement> IsLess()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return JumpRelative(new BinaryExpression(b, Operator.Lower, a));
        }

        IEnumerable<Statement> IsLessEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return JumpRelative(new BinaryExpression(b, Operator.LowerOrEquals, a));
        }

        IEnumerable<Statement> IsEqual()
        {
            var varNum = ReadWord();
            var a = ReadVariable(varNum);
            var b = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return 
                JumpRelative(
                new BinaryExpression(a, Operator.Equals, b));
        }

        IEnumerable<Statement> JumpRelative()
        {
            yield return JumpRelative(false.ToLiteral());
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

        IEnumerable<Statement> ExpressionFunc()
        {
            var stack = new Stack<Expression>();
            var dst = ((IntegerLiteralExpression)GetResultIndexExpression()).Value;
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
                            stack.Push(new BinaryExpression(stack.Pop(), Operator.Minus, i));
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
                            var statements = ExecuteOpCode().ToList();
                            if (statements.Count != 1)
                                throw new InvalidOperationException("Only 1 ExpressionStatement expected");
                            var expStatement = statements[0] as ExpressionStatement;
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
            yield return exp.ToStatement();
        }
    }
}

