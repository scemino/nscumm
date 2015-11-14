//
//  ScriptParser_Variable.cs
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
using NScumm.Scumm.IO;

namespace NScumm.Dump
{
    partial class ScriptParser3
    {
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

        protected Expression SetResult(int index, Expression value)
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

        Statement SetVarRange()
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
            return new MethodInvocation("SetVarRange").AddArgument(GetResultIndex(index)).AddArguments(args).ToStatement();
        }

        Statement Move()
        {
            var indexExp = GetResultIndexExpression();
            var value = GetVarOrDirectWord(OpCodeParameter.Param1);
            return SetResultExpression(indexExp, value).ToStatement();
        }
    }
}

