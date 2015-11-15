//
//  ScummEngine3_Expression.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;

namespace NScumm.Scumm
{
    partial class ScummEngine3
    {
        protected override void IsLess()
        {
            var varNum = ReadWord();
            var a = (short)ReadVariable(varNum);
            var b = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b < a);
        }

        protected override void IsLessEqual()
        {
            var varNum = ReadWord();
            var a = (short)ReadVariable(varNum);
            var b = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b <= a);
        }

        protected override void IsGreater()
        {
            var a = (short)ReadVariable(ReadWord());
            var b = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b > a);
        }

        protected override void IsGreaterEqual()
        {
            var a = (short)ReadVariable(ReadWord());
            var b = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
            JumpRelative(b >= a);
        }

        void Multiply()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable((uint)_resultVarIndex);
            SetResult(a * b);
        }

        void Or()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable((uint)_resultVarIndex);
            SetResult(a | b);
        }

        void And()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            var b = ReadVariable((uint)_resultVarIndex);
            SetResult(a & b);
        }

        protected override void Add()
        {
            GetResult();
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            int b = ReadVariable((uint)_resultVarIndex);
            SetResult(a + b);
        }

        void Divide()
        {
            GetResult();
            var a = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(ReadVariable((uint)_resultVarIndex) / a);
        }

        protected override void Subtract()
        {
            GetResult();
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            SetResult(ReadVariable((uint)_resultVarIndex) - a);
        }

        void Expression()
        {
            _stack.Clear();
            GetResult();
            int dst = _resultVarIndex;
            while ((_opCode = ReadByte()) != 0xFF)
            {
                switch (_opCode & 0x1F)
                {
                    case 1:
					// var
                        _stack.Push(GetVarOrDirectWord(OpCodeParameter.Param1));
                        break;

                    case 2:
					// add
                        {
                            var i = _stack.Pop();
                            _stack.Push(i + _stack.Pop());
                        }
                        break;

                    case 3:
					// sub
                        {
                            var i = _stack.Pop();
                            _stack.Push(_stack.Pop() - i);
                        }
                        break;

                    case 4:
					// mul
                        {
                            var i = _stack.Pop();
                            _stack.Push(i * _stack.Pop());
                        }
                        break;

                    case 5:
					// div
                        {
                            var i = _stack.Pop();
                            _stack.Push(_stack.Pop() / i);
                        }
                        break;

                    case 6:
					// normal opcode
                        {
                            _opCode = ReadByte();
                            ExecuteOpCode(_opCode);
                            _stack.Push(Variables[0]);
                        }
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            _resultVarIndex = dst;
            SetResult(_stack.Pop());
        }
    }
}

