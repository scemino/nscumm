using System;
using System.Collections.Generic;
using NScumm.Core;
using System.Linq;

namespace NScumm.Tmp
{
    partial class ScriptParser
    {
        IEnumerable<Statement> Add()
        {
            var indexExp = GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable(indexExp);
            yield return SetResultExpression(indexExp, new BinaryExpression(a, Operator.Add, b));
        }

        IEnumerable<Statement> Subtract()
        {
            var indexExp = GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(indexExp), Operator.Minus, a));
        }

        IEnumerable<Statement> Multiply()
        {
            var indexExp = (LiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(Convert.ToInt32(indexExp.Value)), Operator.Multiply, a));
        }

        IEnumerable<Statement> Divide()
        {
            var indexExp = (LiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(Convert.ToInt32(indexExp.Value)), Operator.Divide, a));
        }

        IEnumerable<Statement> Increment()
        {
            var index = GetResultIndexExpression();
            yield return SetResultExpression(index, 
                new BinaryExpression(
                    ReadVariable(index),
                    Operator.Add,
                    1.ToLiteral()));
        }

        IEnumerable<Statement> Decrement()
        {
            var index = GetResultIndexExpression();
            yield return SetResultExpression(index, new UnaryExpression(ReadVariable(index), Operator.PostDecrement));
        }

        IEnumerable<Statement> And()
        {
            var indexExp = (LiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(Convert.ToInt32(indexExp.Value)), Operator.And, a));
        }

        IEnumerable<Statement> Or()
        {
            var indexExp = (LiteralExpression)GetResultIndexExpression();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, new BinaryExpression(ReadVariable(Convert.ToInt32(indexExp.Value)), Operator.Or, a));
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
                    new LiteralExpression(0)));
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
            yield return JumpRelative(new LiteralExpression(false));
        }

        Statement JumpRelative(Expression condition)
        {
            var offset = (short)ReadWord();
            var binExp = condition as BinaryExpression;
            if (binExp != null)
            {
                condition = new BinaryExpression(binExp.Left, Not(binExp.Operator), binExp.Right);
            }
            else
            {
                condition = true.ToLiteral();
            }
            return new IfStatement(condition, (int)_br.BaseStream.Position + offset);
        }

        static Operator Not(Operator op)
        {
            Operator notOp;
            switch (op)
            {
                case Operator.Equals:
                    notOp = Operator.Inequals;
                    break;
                case Operator.Inequals:
                    notOp = Operator.Equals;
                    break;
                case Operator.LowerOrEquals:
                    notOp = Operator.Greater;
                    break;
                case Operator.Greater:
                    notOp = Operator.LowerOrEquals;
                    break;
                case Operator.GreaterOrEquals:
                    notOp = Operator.Lower;
                    break;
                case Operator.Lower:
                    notOp = Operator.GreaterOrEquals;
                    break;
                default:
                    throw new NotSupportedException(string.Format("Invalid operator {0}", op));
            }
            return notOp;
        }

        IEnumerable<Statement> ExpressionFunc()
        {
            var statements = new List<Statement>();
            var stack = new Stack<Expression>();
            var indexExp = GetResultIndexExpression();
            var dst = indexExp;
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
                            statements.AddRange(ExecuteOpCode());
                            stack.Push(new ElementAccess("Variables", new LiteralExpression(0)));
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            statements.Add(SetResultExpression(dst, stack.Pop()));
            return statements;
        }
    }
}

