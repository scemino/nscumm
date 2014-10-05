using System;
using System.Collections.Generic;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Dump
{
    partial class ScriptParser
    {
        public Dictionary<int, string> KnownVariables { get; private set; }

        protected int ReadByte()
        {
            return _br.ReadByte();
        }

        protected int ReadWord()
        {
            var word = _br.ReadUInt16();
            return word;
        }

        protected int ReadWordSigned()
        {
            return ReadWord();
        }

        protected Expression GetResultIndexExpression()
        {
            var resultVarIndex = ReadWord();
            Expression resultVarIndexExp = resultVarIndex.ToLiteral();
            if ((resultVarIndex & 0x2000) == 0x2000)
            {
                int a = ReadWord();
                if ((a & 0x2000) == 0x2000)
                {
                    var variableExp = ReadVariable(a & ~0x2000);
                    var literalExp = variableExp as IntegerLiteralExpression;
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

        Expression GetResultIndex(int index)
        {
            if ((index & 0xF000) == 0)
            {
                return new ElementAccess("Variables", index);
            }

            if ((index & 0x8000) != 0)
            {
                if (Game.Version <= 3)
                {
                    return new ElementAccess("BitVariables", index);
                }
                index &= 0x7FFF;
                //BitVariables [index] = value != 0;
                return new ElementAccess("BitVariables", index);
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
                return new ElementAccess("LocalVariables", index);
            }
            //throw new NotSupportedException ();
            return new ElementAccess("Variables", index);
        }

        Expression SetResult(int index, Expression value)
        {
            return new BinaryExpression(GetResultIndex(index), Operator.Assignment, value);
        }

        protected Expression SetResultExpression(Expression index, Expression value)
        {
            var literalExp = index as IntegerLiteralExpression;
            if (literalExp != null)
            {
                return SetResult(literalExp.Value, value);
            }
            else
            {
                return new BinaryExpression(new ElementAccess("Variables", index), Operator.Assignment, value);
            }
        }

        Expression ReadVariable(Expression index)
        {
            var literal = index as IntegerLiteralExpression;
            if (literal != null)
            {
                return ReadVariable(literal.Value);
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
                    var literalExp = exp as IntegerLiteralExpression;
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
                    var.ToLiteral());
            }

            if ((var & 0x8000) == 0x8000)
            {
                var &= 0x7FFF;

                return new ElementAccess(
                    new SimpleName("BitVariables"),
                    var.ToLiteral());
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
            return new IntegerLiteralExpression(ReadByte());
        }

        protected Expression GetVarOrDirectWord(OpCodeParameter param)
        {
            if (((OpCodeParameter)_opCode).HasFlag(param))
                return GetVar();
            return new IntegerLiteralExpression(ReadWordSigned());
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
            var index = ((IntegerLiteralExpression)GetResultIndexExpression()).Value;
            var len = ReadByte();
            var args = new List<Expression>();
            do
            {
                if ((_opCode & 0x80) == 0x80)
                    args.Add(ReadWordSigned().ToLiteral());
                else
                    args.Add(ReadByte().ToLiteral());
            } while ((--len) > 0);
            yield return new MethodInvocation("SetVarRange").AddArgument(GetResultIndex(index)).AddArguments(args).ToStatement();
        }

        Expression ReadCharacters()
        {
            var sb = new List<byte>();
            var character = (byte)ReadByte();
            while (character != 0)
            {
                sb.Add(character);
                if (character == 0xFF)
                {
                    character = (byte)ReadByte();
                    sb.Add(character);
                    if (character != 1 && character != 2 && character != 3 && character != 8)
                    {
                        character = (byte)ReadByte();
                        sb.Add(character);
                        character = (byte)ReadByte();
                        sb.Add(character);
                    }
                }
                character = (byte)ReadByte();
            }
            return new StringLiteralExpression(sb.ToArray());
        }

        IEnumerable<Statement> Move()
        {
            var indexExp = GetResultIndexExpression();
            var value = GetVarOrDirectWord(OpCodeParameter.Param1);
            yield return SetResultExpression(indexExp, value).ToStatement();
        }
    }
}

