using System;
using System.Collections.Generic;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Dump
{
    partial class ScriptParser
    {
        protected byte ReadByte()
        {
            return _br.ReadByte();
        }

        protected ushort ReadWord()
        {
            var word = _br.ReadUInt16();
            return word;
        }

        protected short ReadWordSigned()
        {
            return (short)ReadWord();
        }

        protected Expression GetResultIndexExpression()
        {
            var resultVarIndex = (int)ReadWord();
            Expression resultVarIndexExp = resultVarIndex.ToLiteral();
            if ((resultVarIndex & 0x2000) == 0x2000)
            {
                int a = (int)ReadWord();
                if ((a & 0x2000) == 0x2000)
                {
                    var variableExp = ReadVariable(a & ~0x2000);
                    var literalExp = variableExp as LiteralExpression;
                    if (literalExp != null)
                    {
                        resultVarIndex += Convert.ToInt32(literalExp.Value);
                        resultVarIndex &= ~0x2000;
                        resultVarIndexExp = resultVarIndex.ToLiteral();
                    }
                    else
                    {
                        resultVarIndexExp = new BinaryExpression(
                            new BinaryExpression(resultVarIndexExp, Operator.Add, variableExp), 
                            Operator.And,
                            new UnaryExpression(0x2000.ToLiteral(), Operator.Not));
                    }
                }
                else
                {
                    resultVarIndex = resultVarIndex + (a & 0xFFF);	
                    resultVarIndex &= ~0x2000;
                    resultVarIndexExp = resultVarIndex.ToLiteral();
                }
            }
            return resultVarIndexExp;
        }

        Statement SetResult(int index, Expression value)
        {
            if ((index & 0xF000) == 0)
            {
                //Variables [index] = value;
                return new BinaryExpression(
                    new ElementAccess("Variables", index),
                    Operator.Assignment,
                    value).ToStatement();
            }

            if ((index & 0x8000) != 0)
            {
                if (Game.Version <= 3)
                {
                    return new BinaryExpression(
                        new ElementAccess("BitVariables", index),
                        Operator.Assignment,
                        value).ToStatement();
                }
                index &= 0x7FFF;
                //BitVariables [index] = value != 0;
                return new BinaryExpression(
                    new ElementAccess("BitVariables", index),
                    Operator.Assignment,
                    value).ToStatement();
            }

            if ((index & 0x4000) != 0)
            {
                if (Game.Features.HasFlag(GameFeatures.FewLocals))
                {
                    index &= 0xF;
                }
                else
                {
                    index &= 0xFFF;
                }
                //LocalVariables [index] = value;
                return new BinaryExpression(
                    new ElementAccess("LocalVariables", index),
                    Operator.Assignment,
                    value).ToStatement();
            }
            //throw new NotSupportedException ();
            return new MethodInvocation("SetResult").AddArguments(index.ToLiteral(), value).ToStatement();
        }

        protected Statement SetResultExpression(Expression index, Expression value)
        {
            var literalExp = index as LiteralExpression;
            if (literalExp != null)
            {
                return SetResult(Convert.ToInt32(literalExp.Value), value);
            }
            else
            {
                return new MethodInvocation("WriteResult").AddArguments(index, value).ToStatement();
            }
        }

        Expression ReadVariable(Expression index)
        {
            var literal = index as LiteralExpression;
            if (literal != null)
            {
                return ReadVariable(Convert.ToInt32(literal.Value));
            }
            else
            {
                return ReadVariable2(index);
            }
        }

        Expression ReadVariable(int var)
        {
            if ((var & 0x2000) == 0x2000)
            {
                var a = ReadWord();
                if ((a & 0x2000) == 0x2000)
                {
                    var exp = ReadVariable(a & ~0x2000);
                    var literalExp = exp as LiteralExpression;
                    if (literalExp != null)
                    {
                        var += Convert.ToInt32(literalExp.Value);
                    }
                    else
                    {
                        return ReadVariable2(exp);
                    }
                }
                else
                {
                    var += a & 0xFFF;
                }
                var &= ~0x2000;
            }

            return ReadVariable2(var);
        }

        Expression ReadVariable2(Expression var)
        {
            return new MethodInvocation("ReadVariable").AddArgument(var);
        }

        Expression ReadVariable2(int var)
        {
            if ((var & 0xF000) == 0)
            {
                return new ElementAccess(
                    new SimpleName("Variables"),
                    new LiteralExpression(var));
            }

            if ((var & 0x8000) == 0x8000)
            {
                var &= 0x7FFF;

                return new ElementAccess(
                    new SimpleName("BitVariables"),
                    new LiteralExpression(var));
            }

            if ((var & 0x4000) == 0x4000)
            {
                var &= 0xFFF;

                return new ElementAccess("LocalVariables", var);
            }
            throw new NotSupportedException("Illegal varbits (r)");
        }

        Expression GetVar()
        {
            return ReadVariable(ReadWord());
        }

        protected Expression GetVarOrDirectByte(OpCodeParameter param)
        {
            if (((OpCodeParameter)_opCode).HasFlag(param))
                return GetVar();
            return new LiteralExpression(ReadByte());
        }

        protected Expression GetVarOrDirectWord(OpCodeParameter param)
        {
            if (((OpCodeParameter)_opCode).HasFlag(param))
                return GetVar();
            return new LiteralExpression(ReadWordSigned());
        }

        protected IList<Expression> GetWordVarArgs()
        {
            var args = new List<Expression>();
            while ((_opCode = _br.ReadByte()) != 0xFF)
            {
                args.Add(GetVarOrDirectWord(OpCodeParameter.Param1));
            }
            return args.ToArray();
        }

        IEnumerable<Statement> SetVarRange()
        {
            var index = Convert.ToInt32(((LiteralExpression)GetResultIndexExpression()).Value);
            var a = ReadByte();
            int b;
            do
            {
                if ((_opCode & 0x80) == 0x80)
                    b = ReadWordSigned();
                else
                    b = ReadByte();
                var statement = SetResult(index, new LiteralExpression(b));
                yield return statement;
                index++;

            } while ((--a) > 0);
        }

        LiteralExpression ReadCharacters()
        {
            var sb = new List<byte>();
            var character = ReadByte();
            while (character != 0)
            {
                sb.Add(character);
                if (character == 0xFF)
                {
                    character = ReadByte();
                    sb.Add(character);
                    if (character != 1 && character != 2 && character != 3 && character != 8)
                    {
                        character = ReadByte();
                        sb.Add(character);
                        character = ReadByte();
                        sb.Add(character);
                    }
                }
                character = ReadByte();
            }
            return new LiteralExpression(sb.ToArray());
        }

        IEnumerable<Statement> Move()
        {
            var indexExp = GetResultIndexExpression();
            var value = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, value);
        }
    }
}

