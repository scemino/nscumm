/*
 * This file is part of NScumm.
 *
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace NScumm.Scumm
{
    public class ScummText
    {
        readonly byte[] data;

        public ScummText(byte[] data)
        {
            this.data = data;
        }

        public void Decode(IScummTextDecoder decoder)
        {
            uint num = 0;

            while (num < data.Length)
            {
                byte chr = data[num++];
                if (chr == 0)
                    break;

                if (chr == 0xFF)
                {
                    chr = data[num++];
                    int val = 0;
                    if (chr != 1 && chr != 2 && chr != 3 && chr != 8)
                    {
                        val = data[num++] | data[num++] << 8;
                    }
                    switch (chr)
                    {
                        case 1:
                            decoder.WriteNewLine();
                            break;
                        case 2:
                            decoder.WriteKeep();
                            break;
                        case 3:
                            decoder.WriteWait();
                            break;
                        case 4:
                            decoder.WriteVariable(val);
                            break;

                        case 5:
                            decoder.WriteVerbMessage(val);
                            break;

                        case 6:
                            if (IsActor(val))
                            {
                                decoder.WriteActorName(val);
                            }
                            else
                            {
                                decoder.WriteObjectName(val);
                            }
                            break;

                        case 7:
                            decoder.WriteString(val);
                            break;
                        case 8:
                            // new line ?
                            decoder.WriteNewLine();
                            break;
                        case 9:
                            decoder.StartActorAnim(val);
                            break;
                        case 10:
                            decoder.PlaySound(val);
                            break;
                        case 12:
                            decoder.SetColor(val);
                            break;
                        case 13:
                            // ???
                            break;
                        case 14:
                            decoder.UseCharset(val);
                            break;

                        default:
                            throw new NotSupportedException(string.Format("convertMessageToString(): string escape sequence {0} unknown", chr));
                    }
                }
                else
                {
                    if (chr != '@')
                    {
                        decoder.Write(chr);
                    }
                }
            }
        }

        static bool IsActor(int val)
        {
            return val < 13;
        }
    }
}

