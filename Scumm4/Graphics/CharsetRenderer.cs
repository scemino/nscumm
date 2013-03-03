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
namespace Scumm4.Graphics
{
    public abstract class CharsetRenderer
    {
        internal Rect _str;
        public int _top;
        public int _left, _startLeft;
        public int _right;

        public bool _center;

        /// <summary>
        /// <c>true</c> if "removable" text is visible somewhere (should be called _hasText or so).
        /// </summary>
        public bool _hasMask;
        /// <summary>
        /// The virtual screen on which the text is visible.
        /// </summary>
        public VirtScreen _textScreen;

        public bool _blitAlso;

        public bool _firstChar;
        public bool _disableOffsX;

        protected byte _color;
        protected ScummInterpreter _vm;

        protected int _curId;

        protected CharsetRenderer(ScummInterpreter vm)
        {
            _top = 0;
            _left = 0;
            _startLeft = 0;
            _right = 0;

            _color = 0;

            _center = false;
            _hasMask = false;
            _textScreen = vm.MainVirtScreen;
            _blitAlso = false;
            _firstChar = false;
            _disableOffsX = false;

            _vm = vm;
            _curId = -1;
        }

        public virtual void SetColor(byte color)
        {
            _color = color;
            TranslateColor();
        }

        protected void TranslateColor()
        {
            // TODO ?
        }

        public abstract void PrintChar(int chr, bool ignoreCharsetMask);

        public abstract void SetCurID(int id);
        public int getCurID() { return _curId; }

        public abstract int GetFontHeight();
        public abstract int GetCharWidth(int chr);

        public int GetStringWidth(int arg, byte[] text, int pos)
        {
            int width = 1;
            int chr;
            int oldID = getCurID();

            while ((chr = text[pos++]) != 0)
            {
                if (chr == '\n' || chr == '\r' || chr == _vm._newLineCharacter)
                    break;
                {
                    // TODO: ?
                    //if (chr == '@' && !(_vm->_game.id == GID_CMI && _vm->_language == Common::ZH_TWN))
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
                                ;
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

                if (_vm._useCJKMode)
                {
                    if ((chr & 0x80) != 0)
                    {
                        pos++;
                        width += _vm._2byteWidth;
                        continue;
                    }
                }
                width += GetCharWidth(chr);
            }

            SetCurID(oldID);

            return width;
        }

        public void AddLinebreaks(int a, byte[] str, int pos, int maxwidth)
        {
            int lastspace = -1;
            int curw = 1;
            int chr;
            int oldID = getCurID();

            while ((chr = str[pos++]) != 0)
            {
                {
                    if (chr == '@')
                        continue;
                    if ((chr == 255) || (chr == 254))
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

                if (chr == _vm._newLineCharacter)
                    lastspace = pos - 1;

                if (_vm._useCJKMode)
                {
                    if ((chr & 0x80) != 0)
                    {
                        pos++;
                        curw += _vm._2byteWidth;
                    }
                }
                else
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
