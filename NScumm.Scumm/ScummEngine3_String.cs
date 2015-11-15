//
//  ScummEngine3_String.cs
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
using NScumm.Core.Graphics;

namespace NScumm.Scumm
{
    partial class ScummEngine3
    {
        void StringOperations()
        {
            _opCode = ReadByte();
            switch (_opCode & 0x1F)
            {
                case 1:
                    {
                        // loadstring
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        _strings[id] = ReadCharacters();
                    }
                    break;

                case 2:
                    {
                        // copy string
                        var idA = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var idB = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _strings[idA] = new byte[_strings[idB].Length];
                        Array.Copy(_strings[idB], _strings[idA], _strings[idB].Length);
                    }
                    break;

                case 3:
                    {
                        // Write Character
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var index = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var character = GetVarOrDirectByte(OpCodeParameter.Param3);
                        _strings[id][index] = (byte)character;
                    }
                    break;

                case 4:
                    {
                        // Get string char
                        GetResult();
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var b = GetVarOrDirectByte(OpCodeParameter.Param2);
                        var result = b < _strings[id].Length && b >= 0 ? _strings[id][b] : 0;
                        SetResult(result);
                    }
                    break;

                case 5:
                    {
                        // New String
                        var id = GetVarOrDirectByte(OpCodeParameter.Param1);
                        var size = GetVarOrDirectByte(OpCodeParameter.Param2);
                        _strings[id] = new byte[size];
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        void GetStringWidth()
        {
            GetResult();
            var str = GetVarOrDirectByte(OpCodeParameter.Param1);
            var ptr = _strings[str];

            var width = _charset.GetStringWidth(0, ptr, 0);
            SetResult(width);
        }

        protected override void Print()
        {
            _actorToPrintStrFor = GetVarOrDirectByte(OpCodeParameter.Param1);
            DecodeParseString();
        }

        protected override void DecodeParseString()
        {
            int textSlot;
            switch (_actorToPrintStrFor)
            {
                case 252:
                    textSlot = 3;
                    break;

                case 253:
                    textSlot = 2;
                    break;

                case 254:
                    textSlot = 1;
                    break;

                default:
                    textSlot = 0;
                    break;
            }

            String[textSlot].LoadDefault();
            while ((_opCode = ReadByte()) != 0xFF)
            {
                switch (_opCode & 0xF)
                {
                    case 0:     // SO_AT
                        String[textSlot].Position = new Point(
                            (short)GetVarOrDirectWord(OpCodeParameter.Param1),
                            (short)GetVarOrDirectWord(OpCodeParameter.Param2));
                        String[textSlot].Overhead = false;
                        break;

                    case 1:     // SO_COLOR
                        String[textSlot].Color = (byte)GetVarOrDirectByte(OpCodeParameter.Param1);
                        break;

                    case 2:     // SO_CLIPPED
                        String[textSlot].Right = (short)GetVarOrDirectWord(OpCodeParameter.Param1);
                        break;

                    case 4:     // SO_CENTER
                        String[textSlot].Center = true;
                        String[textSlot].Overhead = false;
                        break;

                    case 6:     // SO_LEFT
                        {
                            if (Game.Version == 3)
                            {
                                String[textSlot].Height = GetVarOrDirectWord(OpCodeParameter.Param1);
                            }
                            else
                            {
                                String[textSlot].Center = false;
                                String[textSlot].Overhead = false;
                            }
                        }
                        break;

                    case 7:     // SO_OVERHEAD
                        String[textSlot].Overhead = true;
                        break;

                    case 8:
                        {   // SO_SAY_VOICE
                            var offset = (ushort)GetVarOrDirectWord(OpCodeParameter.Param1);
                            var delay = (ushort)GetVarOrDirectWord(OpCodeParameter.Param2);

                            if (Game.GameId == Scumm.IO.GameId.Loom && Game.Version == 4)
                            {
                                if (offset == 0 && delay == 0)
                                {
                                    Variables[VariableMusicTimer.Value] = 0;
                                    Sound.StopCD();
                                }
                                else
                                {
                                    // Loom specified the offset from the start of the CD;
                                    // thus we have to subtract the length of the first track
                                    // (22500 frames) plus the 2 second = 150 frame leadin.
                                    // I.e. in total 22650 frames.
                                    offset = (ushort)(offset * 7.5 - 22500 - 2 * 75);

                                    // Slightly increase the delay (5 frames = 1/25 of a second).
                                    // This noticably improves the experience in Loom CD.
                                    delay = (ushort)(delay * 7.5 + 5);

                                    Sound.PlayCDTrack(1, 0, offset, delay);
                                }
                            }
//                            else
//                            {
//                                Console.Error.WriteLine("ScummEngine: decodeParseString: Unhandled case 8");
//                            }
                        }
                        break;

                    case 15:
                        {   // SO_TEXTSTRING
                            var tmp = ReadCharacters();
                            PrintString(textSlot, tmp);
                            // In SCUMM V1-V3, there were no 'default' values for the text slot
                            // values. Hence to achieve correct behavior, we have to keep the
                            // 'default' values in sync with the active values.
                            //
                            // Note: This is needed for Indy3 (Grail Diary). It's also needed
                            // for Loom, or the lines Bobbin speaks during the intro are put
                            // at position 0,0.
                            //
                            // Note: We can't use saveDefault() here because we only want to
                            // save the position and color. In particular, we do not want to
                            // save the 'center' flag. See bug #933168.
                            if (Game.Version <= 3)
                            {
                                String[textSlot].Default.Position = String[textSlot].Position;
                                String[textSlot].Default.Height = String[textSlot].Height;
                                String[textSlot].Default.Color = String[textSlot].Color;
                            }
                        }
                        return;

                    default:
                        throw new NotImplementedException();
                }
            }

            String[textSlot].SaveDefault();
        }
    }
}

