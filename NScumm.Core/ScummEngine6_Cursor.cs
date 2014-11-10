//
//  ScummEngine6_Cursor.cs
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
        [OpCode(0x6b)]
        void CursorCommand()
        {
            var subOp = ReadByte();
            switch (subOp)
            {
                case 0x90:              // SO_CURSOR_ON Turn cursor on
                    _cursor.State = 1;
                    VerbMouseOver(0);
                    break;
                case 0x91:              // SO_CURSOR_OFF Turn cursor off
                    _cursor.State = 0;
                    VerbMouseOver(0);
                    break;
                case 0x92:              // SO_USERPUT_ON
                    _userPut = 1;
                    break;
                case 0x93:              // SO_USERPUT_OFF
                    _userPut = 0;
                    break;
                case 0x94:              // SO_CURSOR_SOFT_ON Turn soft cursor on
                    _cursor.State++;
                    if (_cursor.State > 1)
                        throw new NotSupportedException("Cursor state greater than 1 in script");
                    VerbMouseOver(0);
                    break;
                case 0x95:              // SO_CURSOR_SOFT_OFF Turn soft cursor off
                    _cursor.State--;
                    VerbMouseOver(0);
                    break;
                case 0x96:              // SO_USERPUT_SOFT_ON
                    _userPut++;
                    break;
                case 0x97:              // SO_USERPUT_SOFT_OFF
                    _userPut--;
                    break;
            //                case 0x99:              // SO_CURSOR_IMAGE Set cursor image
            //                    {
            //                        int room, obj;
            //                        obj = popRoomAndObj(&room);
            //                        SetCursorFromImg(obj, room, 1);
            //                        break;
            //                    }
            //                case 0x9A:              // SO_CURSOR_HOTSPOT Set cursor hotspot
            //                    a = pop();
            //                    SetCursorHotspot(pop(), a);
            //                    UpdateCursor();
            //                    break;
                case 0x9C:              // SO_CHARSET_SET
                    InitCharset(Pop());
                    break;
                case 0x9D:              // SO_CHARSET_COLOR
                    var args = GetStackList(16);
                    for (var i = 0; i < args.Length; i++)
                    /*_charsetColorMap[i] =*/ _charsetData[_string[1].Default.Charset][i] = (byte)args[i];
                    break;
            //                case 0xD6:              // SO_CURSOR_TRANSPARENT Set cursor transparent color
            //                    SetCursorTransparency(pop());
            //                    break;
                default:
                    throw new NotSupportedException(string.Format("CursorCommand: default case {0:X2}", subOp));
            }

            Variables[VariableCursorState.Value] = _cursor.State;
            Variables[VariableUserPut.Value] = _userPut;

        }
    }
}

