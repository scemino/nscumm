//
//  ScummEngine6.cs
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

using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Audio;
using NScumm.Core.IO;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace NScumm.Core
{

    partial class ScummEngine6: ScummEngine5
    {
        Stack<int> _vmStack = new Stack<int>(150);

        public ScummEngine6(GameInfo game, IGraphicsManager graphicsManager, IInputManager inputManager, IAudioDriver audioDriver)
            : base(game, graphicsManager, inputManager, audioDriver)
        {
        }

        #region implemented abstract members of ScummEngine

        protected override void InitOpCodes()
        {
            _opCodes = (from method in GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                                 let attributes = (OpCodeAttribute[])method.GetCustomAttributes(typeof(OpCodeAttribute), false)
                                 where attributes.Length > 0
                                 from id in attributes[0].Ids
                                 select new {OpCode = id,Action = OpCode(method)}).ToDictionary(o => o.OpCode, o => o.Action);
        }

        Action OpCode(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentException("A method was expected.", "method");

            List<Func<object>> args = new List<Func<object>>();
            foreach (var param in method.GetParameters().Reverse())
            {
                var paramType = param.ParameterType;
                if (paramType.IsArray && paramType.GetElementType() == typeof(int))
                {
                    args.Add(() => GetStackList(int.MaxValue));
                }
                else if (paramType == typeof(byte))
                {
                    args.Add(() => (byte)Pop());
                }
                else if (paramType == typeof(short))
                {
                    args.Add(() => (short)Pop());
                }
                else if (paramType == typeof(ushort))
                {
                    args.Add(() => (ushort)Pop());
                }
                else if (paramType == typeof(int))
                {
                    args.Add(() => Pop());
                }
                else
                {
                    throw new ArgumentException("An array was expected as parameter.", "method");
                }
            }

            var action = new Action(() => method.Invoke(this, args.Select(arg => arg()).Reverse().ToArray()));
            return action;
        }

        void Push(int value)
        {
            _vmStack.Push(value);
        }

        void Push(bool value)
        {
            _vmStack.Push(value ? 1 : 0);
        }

        int Pop()
        {
            return _vmStack.Pop();
        }

        int[] GetStackList(int max)
        {
            var num = Pop();

            if (num > max)
                throw new InvalidOperationException(string.Format("Too many items {0} in stack list, max {1}", num, max));

            var args = new int[num];
            var i = num;
            while (i-- != 0)
            {
                args[i] = Pop();
            }
            return args;
        }

        [OpCode(0x00)]
        void PushByte()
        {
            Push(ReadByte());
        }

        [OpCode(0x01)]
        void PushWord()
        {
            Push(ReadWord());
        }

        [OpCode(0x02)]
        void PushByteVar()
        {
            Push(ReadVariable(ReadByte()));
        }

        [OpCode(0x03)]
        void PushWordVar()
        {
            Push(ReadVariable(ReadWord()));
        }

        [OpCode(0x4e)]
        void ByteVarInc()
        {
            var var = ReadByte();
            WriteVariable(var, ReadVariable(var) + 1);
        }

        [OpCode(0x4f)]
        void WordVarInc()
        {
            var var = ReadWord();
            WriteVariable(var, ReadVariable(var) + 1);
        }

        #endregion
    }
}

