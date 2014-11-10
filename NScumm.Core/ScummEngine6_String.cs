//
//  ScummEngine6_String.cs
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

namespace NScumm.Core
{
    partial class ScummEngine6
    {
        [OpCode(0xb4)]
        void PrintLine()
        {
            _actorToPrintStrFor = 0xFF;
            DecodeParseString(0, 0);
        }

        [OpCode(0xb5)]
        void PrintText()
        {
            DecodeParseString(1, 0);
        }

        [OpCode(0xb6)]
        void PrintDebug()
        {
            DecodeParseString(2, 0);
        }

        [OpCode(0xb7)]
        void PrintSystem()
        {
            DecodeParseString(3, 0);
        }

        [OpCode(0xb8)]
        void PrintActor()
        {
            DecodeParseString(0, 1);
        }

        [OpCode(0xb9)]
        void PrintEgo()
        {
            Push(Variables[VariableEgo.Value]);
            DecodeParseString(0, 1);
        }

        void DecodeParseString(int m, int n)
        {
            byte b = ReadByte();

            switch (b)
            {
                case 65:                // SO_AT
                    var y = (short)Pop();
                    var x = (short)Pop();
                    _string[m].Position = new NScumm.Core.Graphics.Point(x, y);
                    _string[m].Overhead = false;
                    break;
                case 66:                // SO_COLOR
                    _string[m].Color = (byte)Pop();
                    break;
                case 67:                // SO_CLIPPED
                    _string[m].Right = (byte)Pop();
                    break;
                case 69:                // SO_CENTER
                    _string[m].Center = true;
                    _string[m].Overhead = false;
                    break;
                case 71:                // SO_LEFT
                    _string[m].Center = false;
                    _string[m].Overhead = false;
                    break;
                case 72:                // SO_OVERHEAD
                    _string[m].Overhead = true;
                    _string[m].NoTalkAnim = false;
                    break;
                case 74:                // SO_MUMBLE
                    _string[m].NoTalkAnim = true;
                    break;
                case 75:                // SO_TEXTSTRING
                    var text = ReadCharacters();
                    PrintString(m, text);
                    break;
                case 0xFE:
                    _string[m].LoadDefault();
                    if (n != 0)
                        _actorToPrintStrFor = Pop();
                    break;
                case 0xFF:
                    _string[m].SaveDefault();
                    break;
                default:
                    throw new NotSupportedException(string.Format("DecodeParseString: default case 0x{0:X}", b));
            }
        }
    }
}

