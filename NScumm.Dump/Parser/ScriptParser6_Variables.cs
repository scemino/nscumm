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
        const string VariablesName = "Variables";

        protected Statement ByteArrayRead()
        {
            return Push(ReadArray(ReadByte().ToLiteral(), 0.ToLiteral(), Pop()));
        }

        protected Statement WordArrayRead()
        {
            return Push(ReadArray(ReadWord().ToLiteral(), 0.ToLiteral(), Pop()));
        }

        protected Statement ByteArrayWrite()
        {
            var a = Pop();
            return WriteArray(ReadByte().ToLiteral(), 0.ToLiteral(), Pop(), a);
        }

        protected Statement WordArrayWrite()
        {
            var a = Pop();
            return WriteArray(ReadWord().ToLiteral(), 0.ToLiteral(), Pop(), a);
        }

        protected Statement ByteArrayIndexedRead()
        {
            var @base = Pop();
            var index = Pop();
            return Push(ReadArray(ReadByte().ToLiteral(), index, @base));
        }

        protected Statement WordArrayIndexedRead()
        {
            var @base = Pop();
            var index = Pop();
            return Push(ReadArray(ReadWord().ToLiteral(), index, @base));
        }

        protected Statement ByteArrayIndexedWrite()
        {
            var val = Pop();
            var @base = Pop();
            return WriteArray(ReadByte().ToLiteral(), Pop(), @base, val);
        }

        protected Statement WordArrayIndexedWrite()
        {
            var val = Pop();
            var @base = Pop();
            return WriteArray(ReadWord().ToLiteral(), Pop(), @base, val);
        }

        protected Statement WriteArray(params Expression[] args)
        {
            return new MethodInvocation("WriteArray").AddArguments(args).ToStatement();
        }

        protected Statement ByteVarDec()
        {
            var @var = ReadByte();
            return new UnaryExpression(ReadVariable(var), Operator.PostDecrement).ToStatement();
        }

        protected Statement WordVarDec()
        {
            var @var = ReadWord();
            return new UnaryExpression(ReadVariable(var), Operator.PostDecrement).ToStatement();
        }

        protected Statement ByteArrayDec()
        {
            var @var = ReadByte();
            var @base = Pop();
            return new UnaryExpression(ReadArray(@var.ToLiteral(), 0.ToLiteral(), @base), Operator.PostDecrement).ToStatement();
        }

        protected Statement WordArrayDec()
        {
            var @var = ReadWord();
            var @base = Pop();
            return new UnaryExpression(ReadArray(@var.ToLiteral(), 0.ToLiteral(), @base), Operator.PostDecrement).ToStatement();
        }

        protected Statement ByteArrayInc()
        {
            var @var = ReadByte();
            var @base = Pop();
            return new UnaryExpression(ReadArray(@var.ToLiteral(), 0.ToLiteral(), @base), Operator.PostIncrement).ToStatement();
        }

        protected Statement WordArrayInc()
        {
            var @var = ReadWord();
            var @base = Pop();
            return new UnaryExpression(ReadArray(@var.ToLiteral(), 0.ToLiteral(), @base), Operator.PostIncrement).ToStatement();
        }

        protected Statement WriteByteVar()
        {
            return WriteVar(ReadByte(), Pop()).ToStatement();
        }

        protected Statement WriteWordVar()
        {
            return WriteVar(ReadWord(), Pop()).ToStatement();
        }

        Expression WriteVar(int index, Expression value)
        {
            return new BinaryExpression(new ElementAccess(VariablesName, index), Operator.Assignment, value);
        }

        protected Statement PushByte()
        {
            return Push(ReadByte().ToLiteral());
        }

        protected Statement PushByteVar()
        {
            return Push(ReadVariable(ReadByte()));
        }

        protected Statement PushWord()
        {
            return Push(ReadWordSigned().ToLiteral());
        }

        protected Statement PushWordVar()
        {
            return Push(ReadVariable(ReadWordSigned()));
        }

        protected Statement Push(params Expression[] values)
        {
            return new MethodInvocation("Push").AddArguments(values).ToStatement();
        }

        protected Expression Pop()
        {
            return new MethodInvocation("Pop");
        }

        protected Expression GetStackList(int maxNum)
        {
            return new MethodInvocation("Pop").AddArgument(maxNum);
        }

        protected virtual Statement ArrayOps()
        {
            var subOp = ReadByte();
            var array = ReadWord();

            var exp = new MethodInvocation("ArrayOps");

            switch (subOp)
            {
                case 205:               // SO_ASSIGN_STRING
                    {
                        var index = Pop();
                        var text = ReadCharacters();
                        exp = new MethodInvocation(new MemberAccess(exp, "SetString")).AddArguments(array.ToLiteral(), index, text);
                    }
                    break;
                case 208:               // SO_ASSIGN_INT_LIST
                    {
                        var b = Pop();
                        var c = Pop();
                        var d = ReadVariable(array);
                        exp = new MethodInvocation(new MemberAccess(exp, "SetInt")).AddArguments(array.ToLiteral(), d, b, c);
                    }
                    break;
                case 212:               // SO_ASSIGN_2DIM_LIST
                    {
                        var b = Pop();
                        var len = GetStackList(128);
                        var d = ReadVariable(array);
                        var c = Pop();
                        exp = new MethodInvocation(new MemberAccess(exp, "SetInt2D")).AddArguments(array.ToLiteral(), d, b, c, len);
                    }
                    break;
                default:
                    throw new NotSupportedException(string.Format("o6_arrayOps: default case {0} (array {1})", subOp, array));
            }
            return exp.ToStatement();
        }

        protected virtual Statement DimArray()
        {
            var subOp = ReadByte();
            string data;
            switch (subOp)
            {
                case 199:               // SO_INT_ARRAY
                    data = "int";
                    break;
                case 200:               // SO_BIT_ARRAY
                    data = "bit";
                    break;
                case 201:               // SO_NIBBLE_ARRAY
                    data = "nibble";
                    break;
                case 202:               // SO_BYTE_ARRAY
                    data = "byte";
                    break;
                case 203:               // SO_STRING_ARRAY
                    data = "string";
                    break;
                case 204:               // SO_UNDIM_ARRAY
                    return new MethodInvocation("NukeArray").AddArgument(ReadWord()).ToStatement();
                default:
                    throw new NotSupportedException(string.Format("DimArray: default case {0}", subOp));
            }

            return new MethodInvocation("DefineArray").AddArguments(ReadWord().ToLiteral(), data.ToLiteral(), 0.ToLiteral(), Pop()).ToStatement();
        }

        protected virtual Statement Dim2DimArray()
        {
            var subOp = ReadByte();
            string data;
            switch (subOp)
            {
                case 199:               // SO_INT_ARRAY
                    data = "int";
                    break;
                case 200:               // SO_BIT_ARRAY
                    data = "bit";
                    break;
                case 201:               // SO_NIBBLE_ARRAY
                    data = "nibble";
                    break;
                case 202:               // SO_BYTE_ARRAY
                    data = "byte";
                    break;
                case 203:               // SO_STRING_ARRAY
                    data = "string";
                    break;
                default:
                    throw new NotSupportedException(string.Format("DimArray: default case {0}", subOp));
            }

            var b = Pop();
            var a = Pop();
            return new MethodInvocation("DefineArray").AddArguments(ReadWord().ToLiteral(), data.ToLiteral(), a, b).ToStatement();
        }

        protected Statement ByteVarInc()
        {
            var var = ReadByte();
            var @base = Pop();
            return WriteArray(var.ToLiteral(), 0.ToLiteral(), @base, new BinaryExpression(ReadArray(var.ToLiteral(), 0.ToLiteral(), @base), Operator.Add, 1.ToLiteral()));
        }

        protected Statement WordVarInc()
        {
            var var = ReadWord();
            var @base = Pop();
            return WriteArray(var.ToLiteral(), 0.ToLiteral(), @base, new BinaryExpression(ReadArray(var.ToLiteral(), 0.ToLiteral(), @base), Operator.Add, 1.ToLiteral()));
        }

        Expression ReadArray(params Expression[] args)
        {
            return new MethodInvocation("ReadArray").AddArguments(args);
        }

        protected Statement GetRandomNumber()
        {
            return Push(new MethodInvocation("GetRandomNumber").AddArgument(Pop()));
        }

        protected Statement GetRandomNumberRange()
        {
            var max = Pop();
            var min = Pop();
            return Push(new MethodInvocation("GetRandom").AddArguments(min, max));
        }

        protected Statement Shuffle()
        {
            var b = Pop();
            var a = Pop();
            var num = ReadWord();
            return new MethodInvocation("ShuffleArray")
                .AddArguments(num.ToLiteral(), b, a).ToStatement();
        }

        protected Statement PickVarRandom()
        {
            var args = GetStackList(100);
            var value = ReadWord();
            return Push(new MethodInvocation("PickVarRandom")
                .AddArguments(args, value.ToLiteral()));
        }
    }
}

