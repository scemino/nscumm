//
//  ScummEngine3_Cursor.cs
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
        void CursorCommand()
        {
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:
				// Cursor On
                    _cursor.State = 1;
                    VerbMouseOver(0);
                    break;

                case 2:
				// Cursor Off
                    _cursor.State = 0;
                    VerbMouseOver(0);
                    break;

                case 3:
				// User Input on
                    _userPut = 1;
                    break;

                case 4:
				// User Input off
                    _userPut = 0;
                    break;

                case 5:
				// SO_CURSOR_SOFT_ON
                    _cursor.State++;
                    VerbMouseOver(0);
                    break;

                case 6:
				// SO_CURSOR_SOFT_OFF
                    _cursor.State--;
                    VerbMouseOver(0);
                    break;

                case 7:         // SO_USERPUT_SOFT_ON
                    _userPut++;
                    break;

                case 8:         // SO_USERPUT_SOFT_OFF
                    _userPut--;
                    break;

                case 10:
                    {
                        // SO_CURSOR_IMAGE
                        var i = GetVarOrDirectByte(OpCodeParameter.Param1); // Cursor number
                        var j = GetVarOrDirectByte(OpCodeParameter.Param2); // Charset letter to use
                        RedefineBuiltinCursorFromChar(i, j);
                    }
                    break;

                case 11:        // SO_CURSOR_HOTSPOT
                    {
                        var i = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var j = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var k = GetVarOrDirectByte(OpCodeParameter.Param3);
                        RedefineBuiltinCursorHotspot(i, j, k);
                    }
                    break;

                case 12:
                    {
                        // SO_CURSOR_SET
                        var i = GetVarOrDirectByte(OpCodeParameter.Param1);
                        if (i >= 0 && i <= 3)
                        {
                            _currentCursor = i;
                        }
//                        else
//                        {
//                            Console.Error.WriteLine("CURSOR_SET: unsupported cursor id {0}", i);
//                        }
                        break;
                    }
                case 13:
                    InitCharset(GetVarOrDirectByte(OpCodeParameter.Param1));
                    break;
                case 14:											/* unk */
                    if (Game.Version == 3)
                    {
                        GetVarOrDirectByte(OpCodeParameter.Param1);
                        GetVarOrDirectByte(OpCodeParameter.Param2);
                        // This is some kind of "init charset" opcode. However, we don't have to do anything
                        // in here, as our initCharset automatically calls loadCharset for GF_SMALL_HEADER,
                        // games if needed.
                    }
                    else
                    {
                        var table = GetWordVarArgs();
                        for (var i = 0; i < table.Length; i++)
                            CharsetColorMap[i] = _charsetData[String[1].Default.Charset][i] = (byte)table[i];
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (VariableCursorState.HasValue)
            {
                Variables[VariableCursorState.Value] = _cursor.State;
                Variables[VariableUserPut.Value] = _userPut;
            }
        }
    }
}

