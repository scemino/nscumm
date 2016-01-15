//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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

using NScumm.Core.Graphics;
using System;
using System.Collections.Generic;
using NScumm.Sci.Engine;
using NScumm.Core.Common;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// Text16 class, handles text calculation and displaying of text for SCI0.SCI1.1 games
    /// </summary>
    internal class GfxText16
    {
        public const int SCI_TEXT16_ALIGNMENT_RIGHT = -1;
        public const int SCI_TEXT16_ALIGNMENT_CENTER = 1;
        public const int SCI_TEXT16_ALIGNMENT_LEFT = 0;

        private GfxCache _cache;
        private GfxPaint16 _paint16;
        private GfxPorts _ports;
        private GfxScreen _screen;
        public GfxFont _font;
        private int _codeFontsCount;
        private int[] _codeFonts;
        private int _codeColorsCount;
        private ushort[] _codeColors;

        private Rect _codeRefTempRect;
        private List<Rect> _codeRefRects;

        // Has actually punctuation and characters in it, that may not be the first in a line
        static readonly ushort[] text16_shiftJIS_punctuation = {
            0x9F82, 0xA182, 0xA382, 0xA582, 0xA782, 0xC182, 0xE182, 0xE382, 0xE582, 0xEC82, 0x4083, 0x4283,
            0x4483, 0x4683, 0x4883, 0x6283, 0x8383, 0x8583, 0x8783, 0x8E83, 0x9583, 0x9683, 0x5B81, 0x4181,
            0x4281, 0x7681, 0x7881, 0x4981, 0x4881, 0
        };

        public int FontId
        {
            get
            {
                return _ports._curPort.fontId;
            }
        }

        public GfxText16(GfxCache cache, GfxPorts ports, GfxPaint16 paint16, GfxScreen screen)
        {
            _cache = cache;
            _ports = ports;
            _paint16 = paint16;
            _screen = screen;
            _codeRefRects = new List<Rect>();

            Init();
        }

        private void Init()
        {
            _font = null;
            _codeFonts = null;
            _codeFontsCount = 0;
            _codeColors = null;
            _codeColorsCount = 0;
        }

        public GfxFont GetFont()
        {
            if ((_font == null) || (_font.ResourceId != _ports._curPort.fontId))
                _font = _cache.GetFont(_ports._curPort.fontId);

            return _font;
        }

        public void SetFont(int fontId)
        {
            if ((_font == null) || (_font.ResourceId != fontId))
                _font = _cache.GetFont(fontId);

            _ports._curPort.fontId = _font.ResourceId;
            _ports._curPort.fontHeight = _font.Height;
        }

        public void StringWidth(string str, int orgFontId, out short textWidth, out short textHeight)
        {
            Width(str, 0, (short)str.Length, orgFontId, out textWidth, out textHeight, true);
        }

        private void Width(string text, int from, int len, int orgFontId, out short textWidth, out short textHeight, bool restoreFont)
        {
            ushort curChar;
            int previousFontId = FontId;
            short previousPenColor = _ports._curPort.penClr;

            var i = from;
            textWidth = 0; textHeight = 0;

            GetFont();
            if (_font != null)
            {
                while ((len--) != 0)
                {
                    curChar = text[i++];
                    if (_font.IsDoubleByte(curChar))
                    {
                        curChar |= (ushort)(text[i++] << 8);
                        len--;
                    }
                    switch (curChar)
                    {
                        case 0x0A:
                        case 0x0D:
                        case 0x9781: // this one is used by SQ4/japanese as line break as well
                            textHeight = Math.Max(textHeight, _ports._curPort.fontHeight);
                            break;
                        case 0x7C:
                        default:
                            if (curChar == 0x7C && ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                            {
                                len -= CodeProcessing(text, ref i, orgFontId, 0, false);
                                break;
                            }
                            textHeight = Math.Max(textHeight, _ports._curPort.fontHeight);
                            textWidth += _font.GetCharWidth(curChar);
                            break;
                    }
                }
            }
            // When calculating size, we do not restore font because we need the current (code modified) font active
            //  If we are drawing this is called inbetween, so font needs to get restored
            //  If we are calculating size of just one fixed string (::StringWidth), then we need to restore
            if (restoreFont)
            {
                SetFont(previousFontId);
                _ports.PenColor(previousPenColor);
            }
            return;
        }

        public void DrawString(string text)
        {
            int previousFontId = FontId;
            short previousPenColor = _ports._curPort.penClr;

            Draw(text, 0, (short)text.Length, previousFontId, previousPenColor);
            SetFont(previousFontId);
            _ports.PenColor(previousPenColor);
        }

        /// <summary>
        /// we need to have a separate status drawing code
        /// In KQ4 the IV char is actually 0xA, which would otherwise get considered as linebreak and not printed
        /// </summary>
        /// <param name="text"></param>
        public void DrawStatus(string text)
        {
            short charWidth;
            var textLen = text.Length;
            Rect rect;

            GetFont();
            if (_font == null)
                return;

            rect.Top = _ports._curPort.curTop;
            rect.Bottom = rect.Top + _ports._curPort.fontHeight;
            foreach (var curChar in text)
            {
                charWidth = _font.GetCharWidth(curChar);
                _font.Draw(curChar, (short)(_ports._curPort.top + _ports._curPort.curTop), (short)(_ports._curPort.left + _ports._curPort.curLeft), (byte)_ports._curPort.penClr, _ports._curPort.greyedOutput);
                _ports._curPort.curLeft += charWidth;
            }
        }

        public short Size(out Rect rect, string text, ushort languageSplitter, int fontId, short maxWidth)
        {
            int previousFontId = FontId;
            short previousPenColor = _ports._curPort.penClr;
            short charCount;
            short maxTextWidth = 0, textWidth;
            short totalHeight = 0, textHeight;

            rect = new Rect();
            if (fontId != -1)
                SetFont(fontId);
            else
                fontId = previousFontId;

            rect.Top = rect.Left = 0;

            if (maxWidth < 0)
            { // force output as single line
                if (SciEngine.Instance.Language == Core.Common.Language.JA_JPN)
                    SwitchToFont900OnSjis(text, 0, languageSplitter);

                StringWidth(text, fontId, out textWidth, out textHeight);
                rect.Bottom = textHeight;
                rect.Right = textWidth;
            }
            else {
                // rect.right=found widest line with RTextWidth and GetLongest
                // rect.bottom=num. lines * GetPointSize
                rect.Right = (maxWidth != 0 ? maxWidth : 192);
                var curTextPos = 0; // in work position for GetLongest()
                var curTextLine = 0; // starting point of current line
                while (curTextPos < text.Length)
                {
                    // We need to check for Shift-JIS every line
                    if (SciEngine.Instance.Language == Core.Common.Language.JA_JPN)
                        SwitchToFont900OnSjis(text, curTextPos, languageSplitter);

                    charCount = GetLongest(text, ref curTextPos, rect.Right, fontId);
                    if (charCount == 0)
                        break;
                    Width(text, curTextLine, charCount, fontId, out textWidth, out textHeight, false);
                    maxTextWidth = Math.Max(textWidth, maxTextWidth);
                    totalHeight += textHeight;
                    curTextLine = curTextPos;
                }
                rect.Bottom = totalHeight;
                rect.Right = maxWidth != 0 ? maxWidth : Math.Min(rect.Right, maxTextWidth);
            }
            SetFont(previousFontId);
            _ports.PenColor(previousPenColor);
            return (short)rect.Right;
        }

        // return max # of chars to fit maxwidth with full words, does not include
        // breaking space
        //  Also adjusts text pointer to the new position for the caller
        // 
        // Special cases in games:
        //  Laura Bow 2 - Credits in the game menu - all the text lines start with spaces (bug #5159)
        //                Act 6 Coroner questionaire - the text of all control buttons has trailing spaces
        //                                              "Detective Ryan Hanrahan O'Riley" contains even more spaces (bug #5334)
        //  Conquests of Camelot - talking with Cobb - one text box of the dialogue contains a longer word,
        //                                              that will be broken into 2 lines (bug #5159)

        private short GetLongest(string text, ref int pos, int maxWidth, int orgFontId)
        {
            ushort curChar = 0;
            var textStartPtr = pos;
            var lastSpacePtr = -1;
            short lastSpaceCharCount = 0;
            short curCharCount = 0, resultCharCount = 0;
            ushort curWidth = 0, tempWidth = 0;
            int previousFontId = FontId;
            short previousPenColor = _ports._curPort.penClr;

            GetFont();
            if (_font == null)
                return 0;

            while (true)
            {
                curChar = pos == text.Length ? (ushort)0 : text[pos];
                if (_font.IsDoubleByte(curChar))
                {
                    curChar |= (ushort)(text[pos + 1] << 8);
                }
                switch (curChar)
                {
                    case 0x7C:
                        if (ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                        {
                            curCharCount++; pos++;
                            curCharCount += CodeProcessing(text, ref pos, orgFontId, previousPenColor, false);
                            continue;
                        }
                        break;

                    // We need to add 0xD, 0xA and 0xD 0xA to curCharCount and then exit
                    //  which means, we split text like for example
                    //  - 'Mature, experienced software analyst available.' 0xD 0xA
                    //    'Bug installation a proven speciality. "No version too clean."' (normal game text, this is from lsl2)
                    //  - 0xA '-------' 0xA (which is the official sierra subtitle separator) (found in multilingual versions)
                    //  Sierra did it the same way.
                    case 0xD:
                    // it's meant to pass through here
                    case 0xA:
                    case 0x9781: // this one is used by SQ4/japanese as line break as well
                    // and it's also meant to pass through here
                    case 0:
                        // Check, if 0xA is following, if so include it as well
                        if (curChar == 0xD && text[pos + 1] == 0xA)
                        {
                            curCharCount++; pos++;
                        }
                        if (curChar == 0xD || curChar == 0xA || curChar == 0x9781)
                        {
                            curCharCount++; pos++;
                            if (curChar > 0xFF)
                            {
                                curCharCount++; pos++;
                            }
                        }
                        SetFont(previousFontId);
                        _ports.PenColor(previousPenColor);
                        return curCharCount;

                    case ' ':
                        lastSpaceCharCount = curCharCount; // return count up to (but not including) breaking space
                        lastSpacePtr = pos + 1; // remember position right after the current space
                        break;
                }
                tempWidth += _font.GetCharWidth(curChar);

                // Width is too large? . break out
                if (tempWidth > maxWidth)
                    break;

                // still fits, remember width
                curWidth = tempWidth;

                // go to next character
                curCharCount++; pos++;
                if (curChar > 0xFF)
                {
                    // Double-Byte
                    curCharCount++; pos++;
                }
            }

            if (lastSpaceCharCount != 0)
            {
                // Break and at least one space was found before that
                resultCharCount = lastSpaceCharCount;

                // additionally skip over all spaces, that are following that space, but don't count them for displaying purposes
                pos = lastSpacePtr;
                while (text[pos] == ' ')
                    pos++;

            }
            else {
                // Break without spaces found, we split the very first word - may also be Kanji/Japanese
                if (curChar > 0xFF)
                {
                    // current charracter is Japanese

                    // PC-9801 SCI actually added the last character, which shouldn't fit anymore, still onto the
                    //  screen in case maxWidth wasn't fully reached with the last character
                    if ((maxWidth - 1) > curWidth)
                    {
                        curCharCount += 2; pos += 2;

                        curChar = text[pos];
                        if (_font.IsDoubleByte(curChar))
                        {
                            curChar |= (ushort)(text[pos + 1] << 8);
                        }
                    }

                    // But it also checked, if the current character is not inside a punctuation table and it even
                    //  went backwards in case it found multiple ones inside that table.
                    uint nonBreakingPos = 0;

                    while (true)
                    {
                        // Look up if character shouldn't be the first on a new line
                        nonBreakingPos = 0;
                        while (text16_shiftJIS_punctuation[nonBreakingPos] != 0)
                        {
                            if (text16_shiftJIS_punctuation[nonBreakingPos] == curChar)
                                break;
                            nonBreakingPos++;
                        }
                        if (text16_shiftJIS_punctuation[nonBreakingPos] == 0)
                        {
                            // character is fine
                            break;
                        }
                        // Character is not acceptable, seek backward in the text
                        curCharCount -= 2; pos -= 2;
                        if (pos < textStartPtr)
                            throw new InvalidOperationException("Seeking back went too far, data corruption?");

                        curChar = text[pos];
                        if (!_font.IsDoubleByte(curChar))
                            throw new InvalidOperationException("Non double byte while seeking back");
                        curChar |= (ushort)(text[pos + 1] << 8);
                    }
                }

                // We split the word in that case
                resultCharCount = curCharCount;
            }
            SetFont(previousFontId);
            _ports.PenColor(previousPenColor);
            return resultCharCount;
        }

        public void KernelTextSize(string text, ushort languageSplitter, short font, short maxWidth, out short textWidth, out short textHeight)
        {
            Rect rect;
            Size(out rect, text, languageSplitter, font, maxWidth);
            textWidth = (short)rect.Width;
            textHeight = (short)rect.Height;
        }

        private bool SwitchToFont900OnSjis(string text, int offset, ushort languageSplitter)
        {
            throw new NotImplementedException();
        }

        public void Box(string text, bool show, Rect rect, short alignment, int fontId)
        {
            Box(text, 0, show, rect, alignment, fontId);
        }

        // Draws a text in rect.
        public void Box(string text, ushort languageSplitter, bool show, Rect rect, short alignment, int fontId)
        {
            short textWidth, maxTextWidth, textHeight, charCount;
            short offset = 0;
            short hline = 0;
            int previousFontId = FontId;
            short previousPenColor = _ports._curPort.penClr;
            bool doubleByteMode = false;
            var curTextPos = 0;
            var curTextLine = 0;

            if (fontId != -1)
                SetFont(fontId);
            else
                fontId = previousFontId;

            // Reset reference code rects
            _codeRefRects.Clear();
            _codeRefTempRect.Left = _codeRefTempRect.Top = -1;

            maxTextWidth = 0;
            while (curTextPos < text.Length)
            {
                // We need to check for Shift-JIS every line
                //  Police Quest 2 PC-9801 often draws English + Japanese text during the same call
                if (SciEngine.Instance.Language == Core.Common.Language.JA_JPN)
                {
                    if (SwitchToFont900OnSjis(text, curTextPos, languageSplitter))
                        doubleByteMode = true;
                }

                charCount = GetLongest(text, ref curTextPos, rect.Width, fontId);
                if (charCount == 0)
                    break;
                Width(text, curTextLine, charCount, fontId, out textWidth, out textHeight, true);
                maxTextWidth = Math.Max(maxTextWidth, textWidth);
                switch (alignment)
                {
                    case SCI_TEXT16_ALIGNMENT_RIGHT:
                        offset = (short)(rect.Width - textWidth);
                        break;
                    case SCI_TEXT16_ALIGNMENT_CENTER:
                        offset = (short)((rect.Width - textWidth) / 2);
                        break;
                    case SCI_TEXT16_ALIGNMENT_LEFT:
                        offset = 0;
                        break;

                    default:
                        // TODO: warning("Invalid alignment %d used in TextBox()", alignment);
                        break;
                }
                _ports.MoveTo((short)(rect.Left + offset), (short)(rect.Top + hline));

                if (show)
                {
                    Show(text, curTextLine, charCount, fontId, previousPenColor);
                }
                else {
                    Draw(text, curTextLine, charCount, fontId, previousPenColor);
                }

                hline += textHeight;
                curTextLine = curTextPos;
            }
            SetFont(previousFontId);
            _ports.PenColor(previousPenColor);

            if (doubleByteMode)
            {
                // Kanji is written by pc98 rom to screen directly. Because of
                // GetLongest() behavior (not cutting off the last char, that causes a
                // new line), results in the script thinking that the text would need
                // less space. The coordinate adjustment in fontsjis.cpp handles the
                // incorrect centering because of that and this code actually shows all
                // of the chars - if we don't do this, the scripts will only show most
                // of the chars, but the last few pixels won't get shown most of the
                // time.
                Rect kanjiRect = rect;
                _ports.OffsetRect(ref kanjiRect);
                kanjiRect.Left &= 0xFFC;
                kanjiRect.Right = kanjiRect.Left + maxTextWidth;
                kanjiRect.Bottom = kanjiRect.Top + hline;
                kanjiRect.Left *= 2; kanjiRect.Right *= 2;
                kanjiRect.Top *= 2; kanjiRect.Bottom *= 2;
                _screen.CopyDisplayRectToScreen(kanjiRect);
            }
        }

        private void Show(string text, int from, short len, int orgFontId, short orgPenColor)
        {
            Rect rect;

            rect.Top = _ports._curPort.curTop;
            rect.Bottom = rect.Top + _ports.PointSize;
            rect.Left = _ports._curPort.curLeft;
            Draw(text, from, len, orgFontId, orgPenColor);
            rect.Right = _ports._curPort.curLeft;
            _paint16.BitsShow(rect);
        }

        public void Draw(string text, int from, short len, int orgFontId, short orgPenColor)
        {
            ushort curChar, charWidth;
            Rect rect;

            GetFont();
            if (_font == null)
                return;

            rect.Top = _ports._curPort.curTop;
            rect.Bottom = rect.Top + _ports._curPort.fontHeight;
            var i = from;
            while ((len--) != 0)
            {
                curChar = text[i++];
                if (_font.IsDoubleByte(curChar))
                {
                    curChar |= (ushort)(text[i++] << 8);
                    len--;
                }
                switch (curChar)
                {
                    case 0x0A:
                    case 0x0D:
                    case 0:
                    case 0x9781: // this one is used by SQ4/japanese as line break as well
                        break;
                    case 0x7C:
                    default:
                        if (curChar == 0x7C && ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                        {
                            len -= CodeProcessing(text, ref i, orgFontId, orgPenColor, true);
                            break;
                        }
                        charWidth = _font.GetCharWidth(curChar);
                        // clear char
                        if (_ports._curPort.penMode == 1)
                        {
                            rect.Left = _ports._curPort.curLeft;
                            rect.Right = rect.Left + charWidth;
                            _paint16.EraseRect(rect);
                        }
                        // CharStd
                        _font.Draw(curChar, (short)(_ports._curPort.top + _ports._curPort.curTop), (short)(_ports._curPort.left + _ports._curPort.curLeft), (byte)_ports._curPort.penClr, _ports._curPort.greyedOutput);
                        _ports._curPort.curLeft += (short)charWidth;
                        break;
                }
            }
        }

        // This internal function gets called as soon as a '|' is found in a text. It
        // will process the encountered code and set new font/set color. We only support
        // one-digit codes currently, don't know if multi-digit codes are possible.
        // Returns textcode character count.
        private short CodeProcessing(string text, ref int from, int orgFontId, short orgPenColor, bool doingDrawing)
        {
            var textCode = from;
            short textCodeSize = 0;
            char curCode;
            sbyte curCodeParm;

            // Find the end of the textcode
            while ((++textCodeSize) != 0 && (from < text.Length) && (text[from++] != 0x7C)) { }

            // possible TextCodes:
            //  c . sets textColor to current port pen color
            //  cX . sets textColor to _textColors[X-1]
            curCode = text[textCode];
            curCodeParm = (sbyte)text[textCode + 1];
            if (char.IsDigit((char)curCodeParm))
            {
                curCodeParm -= (sbyte)'0';
            }
            else {
                curCodeParm = -1;
            }
            switch (curCode)
            {
                case 'c': // set text color
                    if (curCodeParm == -1)
                    {
                        _ports._curPort.penClr = orgPenColor;
                    }
                    else {
                        if (curCodeParm < _codeColorsCount)
                        {
                            _ports._curPort.penClr = (short)_codeColors[curCodeParm];
                        }
                    }
                    break;
                case 'f': // set text font
                    if (curCodeParm == -1)
                    {

                        SetFont(orgFontId);
                    }
                    else {
                        if (curCodeParm < _codeFontsCount)
                        {
                            SetFont(_codeFonts[curCodeParm]);
                        }
                    }
                    break;
                case 'r': // reference (used in pepper)
                    if (doingDrawing)
                    {
                        if (_codeRefTempRect.Top == -1)
                        {
                            // Starting point
                            _codeRefTempRect.Top = _ports._curPort.curTop;
                            _codeRefTempRect.Left = _ports._curPort.curLeft;
                        }
                        else {
                            // End point reached
                            _codeRefTempRect.Bottom = _ports._curPort.curTop + _ports._curPort.fontHeight;
                            _codeRefTempRect.Right = _ports._curPort.curLeft;
                            _codeRefRects.Add(_codeRefTempRect);
                            _codeRefTempRect.Left = _codeRefTempRect.Top = -1;
                        }
                    }
                    break;
            }
            return textCodeSize;
        }

        public Register AllocAndFillReferenceRectArray()
        {
            var rectCount = _codeRefRects.Count;
            if (rectCount != 0)
            {
                Register rectArray;
                var rectArrayPtr = new ByteAccess(SciEngine.Instance.EngineState._segMan.AllocDynmem(4 * 2 * (rectCount + 1), "text code reference rects", out rectArray));
                GfxCoordAdjuster coordAdjuster = SciEngine.Instance._gfxCoordAdjuster;
                for (var curRect = 0; curRect < rectCount; curRect++)
                {
                    var left = _codeRefRects[curRect].Left;
                    var top = _codeRefRects[curRect].Top;
                    var right = _codeRefRects[curRect].Right;
                    var bottom = _codeRefRects[curRect].Bottom;
                    coordAdjuster.KernelLocalToGlobal(ref left, ref top);
                    coordAdjuster.KernelLocalToGlobal(ref right, ref bottom);
                    _codeRefRects[curRect] = new Rect(left, top, right, bottom);
                    rectArrayPtr.WriteUInt16(0, (ushort)_codeRefRects[curRect].Left);
                    rectArrayPtr.WriteUInt16(2, (ushort)_codeRefRects[curRect].Top);
                    rectArrayPtr.WriteUInt16(4, (ushort)_codeRefRects[curRect].Right);
                    rectArrayPtr.WriteUInt16(6, (ushort)_codeRefRects[curRect].Bottom);
                    rectArrayPtr.Offset += 8;
                }
                rectArrayPtr.WriteUInt16(0, 0x7777);
                rectArrayPtr.WriteUInt16(2, 0x7777);
                rectArrayPtr.WriteUInt16(4, 0x7777);
                rectArrayPtr.WriteUInt16(6, 0x7777);
                return rectArray;
            }
            return Register.NULL_REG;
        }

        // Used SCI1+ for text codes
        public void KernelTextFonts(int argc, StackPtr? argv)
        {
            _codeFontsCount = argc;
            _codeFonts = new int[argc];
            for (var i = 0; i < argc; i++)
            {
                _codeFonts[i] = argv.Value[i].ToUInt16();
            }
        }

        // Used SCI1+ for text codes
        public void KernelTextColors(int argc, StackPtr? argv)
        {
            _codeColorsCount = argc;
            _codeColors = new ushort[argc];
            for (var i = 0; i < argc; i++)
            {
                _codeColors[i] = argv.Value[i].ToUInt16();
            }
        }
    }
}
