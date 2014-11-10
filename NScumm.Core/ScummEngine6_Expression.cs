//
//  ScummEngine6_Expression.cs
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

namespace NScumm.Core
{
    partial class ScummEngine6
    {
        [OpCode(0x0c)]
        void Dup(int value)
        {
            Push(value);
            Push(value);
        }

        [OpCode(0x0d)]
        void Not(int value)
        {
            Push(value == 0 ? 1 : 0);
        }

        [OpCode(0x0e)]
        void Eq(int a, int b)
        {
            Push(a == b ? 1 : 0);
        }

        [OpCode(0x0f)]
        void NEq(int a, int b)
        {
            Push(a != b ? 1 : 0);
        }

        [OpCode(0x14)]
        void Add(int a, int b)
        {
            Push(a + b);
        }

        [OpCode(0x15)]
        void Sub(int a, int b)
        {
            Push(a - b);
        }

        [OpCode(0x16)]
        void Mul(int a, int b)
        {
            Push(a * b);
        }

        [OpCode(0x17)]
        void Div(int a, int b)
        {
            Push(a / b);
        }

        [OpCode(0x18)]
        void Land(int a, int b)
        {
            Push((a != 0 && b != 0) ? 1 : 0);
        }

        [OpCode(0x19)]
        void Lor(int a, int b)
        {
            Push((a != 0 || b != 0) ? 1 : 0);
        }

        [OpCode(0x1a)]
        void Pop(int a)
        {
        }

        [OpCode(0x5c)]
        void If(int condition)
        {
            if (condition != 0)
                Jump();
            else
                ReadWordSigned();
        }

        [OpCode(0x5d)]
        void IfNot(int condition)
        {
            if (condition == 0)
                Jump();
            else
                ReadWordSigned();
        }

        [OpCode(0x10)]
        void Gt(int a, int b)
        {
            Push(a > b ? 1 : 0);
        }

        [OpCode(0x11)]
        void Lt(int a, int b)
        {
            Push(a < b ? 1 : 0);
        }

        [OpCode(0x12)]
        void Le(int a, int b)
        {
            Push(a <= b ? 1 : 0);
        }

        [OpCode(0x13)]
        void Ge(int a, int b)
        {
            Push(a >= b ? 1 : 0);
        }

        [OpCode(0x73)]
        void Jump()
        {
            var offset = ReadWordSigned();

            // WORKAROUND bug #2826144: Talking to the guard at the bigfoot party, after
            // he's let you inside, will cause the game to hang, if you end the conversation.
            // This is a script bug, due to a missing jump in one segment of the script.
            if (Game.Id == "samnmax" && Slots[CurrentScript].Number == 101 && ReadVariable(0x8000 + 97) == 1 && offset == 1)
            {
                offset = -18;
            }

            CurrentPos += offset;
        }
    }
}

