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

namespace NScumm.Scumm
{
    partial class ScummEngine6
    {
        [OpCode(0xb4)]
        protected virtual void PrintLine()
        {
            _actorToPrintStrFor = 0xFF;
            DecodeParseString(0, 0);
        }

        [OpCode(0xb5)]
        protected virtual void PrintText()
        {
            DecodeParseString(1, 0);
        }

        [OpCode(0xb6)]
        protected virtual void PrintDebug()
        {
            DecodeParseString(2, 0);
        }

        [OpCode(0xb7)]
        protected virtual void PrintSystem()
        {
            DecodeParseString(3, 0);
        }

        [OpCode(0xb8)]
        protected virtual void PrintActor()
        {
            DecodeParseString(0, 1);
        }

        [OpCode(0xb9)]
        protected override void PrintEgo()
        {
            Push(Variables[VariableEgo.Value]);
            DecodeParseString(0, 1);
        }

        protected override byte[] GetStringAt(int index)
        {
            var str = _strings[index];
            if (str == null)
                return null;
            var dest = new byte[str.Length - 6];
            Array.Copy(str, 6, dest, 0, str.Length - 6);
            return dest;
        }

        protected virtual void DecodeParseString(int m, int n)
        {
            byte b = ReadByte();

            switch (b)
            {
                case 65:                // SO_AT
                    var y = (short)Pop();
                    var x = (short)Pop();
                    String[m].Position = new Core.Graphics.Point(x, y);
                    String[m].Overhead = false;
                    break;
                case 66:                // SO_COLOR
                    String[m].Color = (byte)Pop();
                    break;
                case 67:                // SO_CLIPPED
                    var r = Pop();
                    String[m].Right = (short)r;
                    break;
                case 69:                // SO_CENTER
                    String[m].Center = true;
                    String[m].Overhead = false;
                    break;
                case 71:                // SO_LEFT
                    String[m].Center = false;
                    String[m].Overhead = false;
                    break;
                case 72:                // SO_OVERHEAD
                    String[m].Overhead = true;
                    String[m].NoTalkAnim = false;
                    break;
                case 74:                // SO_MUMBLE
                    String[m].NoTalkAnim = true;
                    break;
                case 75:                // SO_TEXTSTRING
                    var text = ReadCharacters();
                    PrintString(m, text);
                    break;
                case 0xFE:
                    String[m].LoadDefault();
                    if (n != 0)
                        _actorToPrintStrFor = Pop();
                    break;
                case 0xFF:
                    String[m].SaveDefault();
                    break;
                default:
                    throw new NotSupportedException(string.Format("DecodeParseString: default case 0x{0:X}", b));
            }
        }
    }
}

