//
//  ScummEngine_String.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NScumm.Core;
using NScumm.Core.Graphics;

namespace NScumm.Scumm
{
    partial class ScummEngine
    {
        protected byte[][] _strings;
        TextSlot[] _string = new TextSlot[6];
        Rect _curStringRect=new Rect(-1,-1,-1,-1);
        internal TextSlot[] String { get { return _string; } }

        protected byte[] ReadCharacters()
        {
            var sb = new List<byte>();
            var character = ReadByte();
            while (character != 0)
            {
                sb.Add(character);
                if (character == 0xFF)
                {
                    character = ReadByte();
                    sb.Add(character);
                    if (character != 1 && character != 2 && character != 3 && character != 8)
                    {
                        var count = _game.Version == 8 ? 4 : 2;
                        sb.AddRange(from i in Enumerable.Range(0, count)
                                                         select ReadByte());
                    }
                }
                character = ReadByte();
            }
            return sb.ToArray();
        }

        protected virtual void PrintString(int textSlot, byte[] msg)
        {
            switch (textSlot)
            {
                case 0:
                    ActorTalk(msg);
                    break;

                case 1:
                    DrawString(1, msg);
                    break;

                case 2:
                    DebugMessage(msg);
                    break;

                case 3:
				// TODO:
				//    showMessageDialog(msg);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        void DebugMessage(byte[] msg)
        {
            var buffer = new byte[500];

            ConvertMessageToString(msg, buffer, 0);

            if ((buffer[0] != 0xFF))
            {
				Debug.WriteLine("DEBUG: {0}", buffer.GetText());
                return;
            }

            if (buffer[0] == 0xFF && buffer[1] == 10)
            {
                int channel = 0;

                var a = buffer[2] | (buffer[3] << 8) | (buffer[6] << 16) | (buffer[7] << 24);
                var b = buffer[10] | (buffer[11] << 8) | (buffer[14] << 16) | (buffer[15] << 24);

                // Sam and Max uses a caching system, printing empty messages
                // and setting VAR_V6SoundMODE beforehand. See patch 609791.
                if (_game.GameId == Scumm.IO.GameId.SamNMax)
                    channel = Variables[VariableV6SoundMode.Value];

                if (channel != 2)
                    Sound.TalkSound(a, b, 1, channel);
            }
        }

        protected int ConvertMessageToString(byte[] src, byte[] dst, int dstPos)
        {
            uint num = 0;
            uint val;
            byte chr;
            int dstPosBegin = dstPos;

            if (_game.Version >= 7)
            {
                src = TranslateText(src);
            }

            while (num < src.Length)
            {
                chr = src[num++];
                if (chr == 0)
                    break;

                if (chr == 0xFF)
                {
                    chr = src[num++];

                    if (chr == 1 || chr == 2 || chr == 3 || chr == 8)
                    {
                        // Simply copy these special codes
                        dst[dstPos++] = 0xFF;
                        dst[dstPos++] = chr;
                    }
                    else
                    {
                        val = (Game.Version == 8) ? BitConverter.ToUInt32(src, (int)num) : BitConverter.ToUInt16(src, (int)num);
                        switch (chr)
                        {
                            case 4:
                                dstPos += ConvertIntMessage(dst, dstPos, val);
                                break;

                            case 5:
                                dstPos += ConvertVerbMessage(dst, dstPos, val);
                                break;

                            case 6:
                                dstPos += ConvertNameMessage(dst, dstPos, val);
                                break;

                            case 7:
                                dstPos += ConvertStringMessage(dst, dstPos, val);
                                break;

                            case 9:
                            case 10:
                            case 12:
                            case 13:
                            case 14:
							// Simply copy these special codes
                                dst[dstPos++] = 0xFF;
                                dst[dstPos++] = chr;
                                dst[dstPos++] = src[num + 0];
                                dst[dstPos++] = src[num + 1];
                                break;

                            default:
                                throw new NotSupportedException(string.Format("convertMessageToString(): string escape sequence {0} unknown", chr));
                        }
                        num += (Game.Version == 8) ? (uint)4 : 2;
                    }
                }
                else
                {
                    if (chr != '@')
                    {
                        dst[dstPos++] = chr;
                    }
                }
            }

            dst[dstPos] = 0;

            return dstPos - dstPosBegin;
        }

        public virtual byte[] TranslateText(byte[] src)
        {
            return src;
        }

        int ConvertNameMessage(byte[] dst, int dstPos, uint var)
        {
            var num = ReadVariable(var);
            if (num != 0)
            {
                var ptr = GetObjectOrActorName(num);
                if (ptr != null)
                {
                    return ConvertMessageToString(ptr, dst, dstPos);
                }
            }
            return 0;
        }

        int ConvertVerbMessage(byte[] dst, int dstPos, uint var)
        {
            var num = ReadVariable(var);
            if (num != 0)
            {
                for (int k = 1; k < Verbs.Length; k++)
                {
                    if (num == Verbs[k].VerbId && Verbs[k].Type == VerbType.Text && (Verbs[k].SaveId == 0))
                    {
                        return ConvertMessageToString(Verbs[k].Text, dst, dstPos);
                    }
                }
            }
            return 0;
        }

        int ConvertIntMessage(Array dst, int dstPos, uint var)
        {
            var num = ReadVariable(var);
            var src = Encoding.UTF8.GetBytes(num.ToString());
            Array.Copy(src, 0, dst, dstPos, src.Length);
            return src.Length;
        }

        int ConvertStringMessage(byte[] dst, int dstPos, uint var)
        {
            if (Game.Version <= 2)
            {
                byte chr;
                int i = 0;
                while ((chr = (byte)Variables[var++]) != 0)
                {
                    if (chr != '@')
                    {
                        dst[dstPos++] = chr;
                        i++;
                    }
                }

                return i;
            }

            if ((Game.Version == 3) || (_game.Version >= 6))
            {
                var = (uint)ReadVariable(var);
            }

            if (var != 0)
            {
                var ptr = GetStringAt((int)var);
                if (ptr != null)
                {
                    return ConvertMessageToString(ptr, dst, dstPos);
                }
            }
            return 0;
        }

        protected virtual byte[] GetStringAt(int index)
        {
            return _strings[index];
        }
    }
}

