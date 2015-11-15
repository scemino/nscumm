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

using NScumm.Core.Graphics;

namespace NScumm.Scumm.Graphics
{
    public abstract class CharsetRenderer
    {
        internal Rect Str;
        public int Top;
        public int Left, StartLeft;
        public int Right;

        public bool Center;

        /// <summary>
        /// <c>true</c> if "removable" text is visible somewhere (should be called _hasText or so).
        /// </summary>
        public bool HasMask;
        /// <summary>
        /// The virtual screen on which the text is visible.
        /// </summary>
        public VirtScreen TextScreen;

        public bool BlitAlso;

        public bool FirstChar;
        public bool DisableOffsX;

        protected byte Color;
        protected ScummEngine Vm;

        protected int CurId;

        protected CharsetRenderer(ScummEngine vm)
        {
            Top = 0;
            Left = 0;
            StartLeft = 0;
            Right = 0;

            Color = 0;

            Center = false;
            HasMask = false;
            TextScreen = vm.MainVirtScreen;
            BlitAlso = false;
            FirstChar = false;
            DisableOffsX = false;

            Vm = vm;
            CurId = -1;
        }

        public virtual void SetColor(byte color)
        {
            Color = color;
        }

        public abstract void DrawChar(int chr, Surface s, int x, int y);

        public abstract void PrintChar(int chr, bool ignoreCharsetMask);

        public abstract void SetCurID(int id);

        public int GetCurId()
        {
            return CurId;
        }

        public abstract int GetFontHeight();

        public abstract int GetCharWidth(int chr);

        public virtual int GetCharHeight(int chr) { return GetFontHeight(); }

        public int GetStringWidth(int arg, System.Collections.Generic.IList<byte> text, int pos)
        {
            int width = 1;
            int chr;
            int oldID = GetCurId();

            while (((chr = text[pos++]) != 0) && (pos < text.Count))
            {
                if (chr == '\n' || chr == '\r' || chr == Vm.NewLineCharacter)
                    break;
                {
                    // TODO: ?
                    //if (chr == '@' && !(_vm.Game.GameId == GameId.CurseOfMonkeyIsland && _vm->_language == Common::ZH_TWN))
                    //    continue;
                    if (chr == 255 || chr == 254)
                    {
                        chr = text[pos++];
                        if (chr == 3)	// 'WAIT'
							break;
                        if (chr == 8)
                        { // 'Verb on next line'
                            if (arg == 1)
                                break;
                            while (text[pos++] == ' ')
                            {
                            }
                            continue;
                        }
                        if (chr == 10 || chr == 21 || chr == 12 || chr == 13)
                        {
                            pos += 2;
                            continue;
                        }
                        if (chr == 9 || chr == 1 || chr == 2) // 'Newline'
							break;
                        if (chr == 14)
                        {
                            int set = text[pos] | (text[pos + 1] << 8);
                            pos += 2;
                            SetCurID(set);
                            continue;
                        }
                    }
                }

                //if (Vm.UseCjkMode)
                //{
                //    if ((chr & 0x80) != 0)
                //    {
                //        pos++;
                //        width += Vm._2byteWidth;
                //        continue;
                //    }
                //}
                width += GetCharWidth(chr);
            }

            SetCurID(oldID);

            return width;
        }

        public void AddLinebreaks(int a, System.Collections.Generic.IList<byte> str, int pos, int maxwidth)
        {
            int lastspace = -1;
            int curw = 1;
            int chr;
            int oldID = GetCurId();

            while ((chr = str[pos++]) != 0)
            {
                {
                    if (chr == '@')
                        continue;
                    if ((chr == 255) || (Vm.Game.Version <= 6 && chr == 254))
                    {
                        chr = str[pos++];
                        if (chr == 3) // 'Wait'
							break;
                        if (chr == 8)
                        { // 'Verb on next line'
                            if (a == 1)
                            {
                                curw = 1;
                            }
                            else
                            {
                                while (str[pos] == ' ')
                                    str[pos++] = (byte)'@';
                            }
                            continue;
                        }
                        if (chr == 10 || chr == 21 || chr == 12 || chr == 13)
                        {
                            pos += 2;
                            continue;
                        }
                        if (chr == 1)
                        { // 'Newline'
                            curw = 1;
                            continue;
                        }
                        if (chr == 2) // 'Don't terminate with \n'
							break;
                        if (chr == 14)
                        {
                            int set = str[pos] | (str[pos + 1] << 8);
                            pos += 2;
                            SetCurID(set);
                            continue;
                        }
                    }
                }
                if (chr == ' ')
                    lastspace = pos - 1;

                if (chr == Vm.NewLineCharacter)
                    lastspace = pos - 1;

                //if (Vm.UseCjkMode)
                //{
                //    if ((chr & 0x80) != 0)
                //    {
                //        pos++;
                //        curw += Vm._2byteWidth;
                //    }
                //}
                //else
                {
                    curw += GetCharWidth(chr);
                }
                if (lastspace == -1)
                    continue;
                if (curw > maxwidth)
                {
                    str[lastspace] = 0xD;
                    curw = 1;
                    pos = lastspace + 1;
                    lastspace = -1;
                }
            }

            SetCurID(oldID);
        }
    }
}