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

#if ENABLE_SCI32
using System;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    internal enum TextAlign
    {
        Default = -1,
        Left = 0,
        Center = 1,
        Right = 2
    }

    internal enum ScrollDirection {
        Up,
        Down
    }

    /// <summary>
    /// Text32 class, handles text calculation and displaying of text for SCI2, SCI21 and SCI3 games
    /// </summary>
    internal class GfxText32
    {
        /**
	 * The size of the x-dimension of the coordinate system
	 * used by the text renderer. Static since it was global in SSCI.
	 */
        public static short _xResolution;

        /**
         * The size of the y-dimension of the coordinate system
         * used by the text renderer. Static since it was global in SSCI.
         */
        public static short _yResolution;

        /**
         * The memory handle of the currently active bitmap.
         */
        public Register _bitmap;

        /**
         * The size of the x-dimension of the coordinate system
         * used by the text renderer. Static since it was global in SSCI.
         */
        public static short _scaledWidth;

        /**
         * The size of the y-dimension of the coordinate system
         * used by the text renderer. Static since it was global in SSCI.
         */
        public static short _scaledHeight;

        /**
         * The currently active font resource used to write text
         * into the bitmap.
         *
         * @note SCI engine builds the font table directly
         * inside of FontMgr; we use GfxFont instead.
         */
        public GfxFont _font;

        private SegManager _segMan;
        private GfxCache _cache;

        /**
         * The resource ID of the default font used by the game.
         *
         * @todo Check all SCI32 games to learn what their
         * default font is.
         */
        private static short _defaultFontId;

        /**
         * The width and height of the currently active text
         * bitmap, in text-system coordinates.
         *
         * @note These are unsigned in the actual engine.
         */
        private short _width, _height;

        /**
         * The color used to draw text.
         */
        private byte _foreColor;

        /**
         * The background color of the text box.
         */
        private byte _backColor;

        /**
         * The transparent color of the text box. Used when
         * compositing the bitmap onto the screen.
         */
        private byte _skipColor;

        /**
         * The rect where the text is drawn within the bitmap.
         * This rect is clipped to the dimensions of the bitmap.
         */
        private Rect _textRect;

        /**
         * The text being drawn to the currently active text
         * bitmap.
         */
        private string _text;

        /**
         * The font being used to draw the text.
         */
        private int _fontId;

        /**
         * The color of the text box border.
         */
        private short _borderColor;

        /**
         * TODO: Document
         */
        private bool _dimmed;

        /**
         * The text alignment for the drawn text.
         */
        private TextAlign _alignment;

        /**
         * The position of the text draw cursor.
         */
        private Point _drawPosition;

        public GfxText32(SegManager segMan, GfxCache fonts)
        {
            _segMan = segMan;
            _cache = fonts;
        }

        public void SetFont(int fontId)
        {
            // NOTE: In SCI engine this calls FontMgr::BuildFontTable and then a font
            // table is built on the FontMgr directly; instead, because we already have
            // font resources, this code just grabs a font out of GfxCache.
            if (fontId != _fontId)
            {
                _fontId = fontId == -1 ? _defaultFontId : fontId;
                _font = _cache.GetFont(_fontId);
            }
        }

        public Register CreateFontBitmap(short width, short height, Rect rect, string text, byte foreColor,
            byte backColor, byte skipColor, int fontId, TextAlign alignment, short borderColor, bool dimmed,
            bool doScaling,bool gc)
        {
            _borderColor = borderColor;
            _text = text;
            _textRect = rect;
            _width = width;
            _height = height;
            _foreColor = foreColor;
            _backColor = backColor;
            _skipColor = skipColor;
            _alignment = alignment;
            _dimmed = dimmed;

            SetFont(fontId);

            if (doScaling)
            {
                short scriptWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
                short scriptHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

                var scaleX = new Rational(_scaledWidth, scriptWidth);
                var scaleY = new Rational(_scaledHeight, scriptHeight);

                _width = (short) (_width * scaleX);
                _height = (short) (_height * scaleY);
                Helpers.Mulinc(ref _textRect, scaleX, scaleY);
            }

            // _textRect represents where text is drawn inside the
            // sciBitmap; clipRect is the entire sciBitmap
            Rect bitmapRect = new Rect(_width, _height);

            if (_textRect.Intersects(bitmapRect))
            {
                _textRect.Clip(bitmapRect);
            }
            else
            {
                _textRect = new Rect();
            }

            _segMan.AllocateBitmap(_bitmap, _width, _height, _skipColor, 0, 0, _scaledWidth, _scaledHeight, 0, false,
                gc);

            Erase(bitmapRect, false);

            if (_borderColor > -1)
            {
                DrawFrame(bitmapRect, 1, (byte) _borderColor, false);
            }

            DrawTextBox();
            return _bitmap;
        }

        public void ScrollLine(string lineText, int numLines, byte color, TextAlign align, int fontId, ScrollDirection dir)
        {
            SciBitmap bmr = _segMan.LookupBitmap(_bitmap);
            var pixels = bmr.Pixels;

            int h = _font.Height;

            if (dir == ScrollDirection.Up)
            {
                // Scroll existing text down
                for (int i = 0; i < (numLines - 1) * h; ++i)
                {
                    int y = _textRect.Top + numLines * h - i - 1;
                    Array.Copy(pixels.Data, pixels.Offset + (y - h) * _width + _textRect.Left,
                        pixels.Data, pixels.Offset + y * _width + _textRect.Left, _textRect.Width);
                }
            }
            else
            {
                // Scroll existing text up
                for (int i = 0; i < (numLines - 1) * h; ++i)
                {
                    int y = _textRect.Top + i;
                    Array.Copy(pixels.Data, pixels.Offset + (y + h) * _width + _textRect.Left,
                        pixels.Data, pixels.Offset + y * _width + _textRect.Left, _textRect.Width);
                }
            }

            Rect lineRect = _textRect;

            if (dir == ScrollDirection.Up)
            {
                lineRect.Bottom = (short) (lineRect.Top + h);
            }
            else
            {
                // It is unclear to me what the purpose of this bottom++ is.
                // It does not seem to be the usual inc/exc issue.
                lineRect.Top = (short) (lineRect.Top + (numLines - 1) * h);
                lineRect.Bottom++;
            }

            Erase(lineRect, false);

            _drawPosition.X = _textRect.Left;
            _drawPosition.Y = _textRect.Top;
            if (dir == ScrollDirection.Down)
            {
                _drawPosition.Y = (short) (_drawPosition.Y + (numLines - 1) * h);
            }

            _foreColor = color;
            _alignment = align;
            //int fc = _foreColor;

            SetFont(fontId);

            _text = lineText;
            short textWidth = GetTextWidth(0, lineText.Length);

            if (_alignment == TextAlign.Center)
            {
                _drawPosition.X = (short) (_drawPosition.X + (_textRect.Width - textWidth) / 2);
            }
            else if (_alignment == TextAlign.Right)
            {
                _drawPosition.X = (short) (_drawPosition.X + _textRect.Width - textWidth);
            }

            //_foreColor = fc;
            //setFont(fontId);

            DrawText(0, lineText.Length);
        }


        public Register CreateFontBitmap(CelInfo32 celInfo, Rect rect, string text, short foreColor, short backColor,
            int fontId, short skipColor, short borderColor, bool dimmed, bool gc)
        {
            _borderColor = borderColor;
            _text = text;
            _textRect = rect;
            _foreColor = (byte) foreColor;
            _dimmed = dimmed;

            SetFont(fontId);

            short scriptWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            short scriptHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

            Helpers.Mulinc(ref _textRect, new Rational(_scaledWidth, scriptWidth),
                new Rational(_scaledHeight, scriptHeight));

            CelObjView view = CelObjView.Create(celInfo.resourceId, celInfo.loopNo, celInfo.celNo);
            _skipColor = view._transparentColor;
            _width = (short) (view._width * _scaledWidth / view._xResolution);
            _height = (short) (view._height * _scaledHeight / view._yResolution);

            Rect bitmapRect = new Rect(_width, _height);
            if (_textRect.Intersects(bitmapRect))
            {
                _textRect.Clip(bitmapRect);
            }
            else
            {
                _textRect = new Rect();
            }

            SciBitmap bitmap = _segMan.AllocateBitmap(_bitmap, _width, _height, _skipColor, 0, 0, _scaledWidth,
                _scaledHeight, 0, false, gc);

            // NOTE: The engine filled the sciBitmap pixels with 11 here, which is silly
            // because then it just erased the sciBitmap using the skip color. So we don't
            // fill the sciBitmap redundantly here.

            _backColor = _skipColor;
            Erase(bitmapRect, false);
            _backColor = (byte) backColor;

            var scaledPos = new Point(0, 0);
            view.Draw(bitmap.Buffer, ref bitmapRect, ref scaledPos, false,
                new Rational(_scaledWidth, view._xResolution),
                new Rational(_scaledHeight, view._yResolution));

            if (_backColor != skipColor && _foreColor != skipColor)
            {
                Erase(_textRect, false);
            }

            if (text.Length > 0)
            {
                if (_foreColor == skipColor)
                {
                    Error("TODO: Implement transparent text");
                }
                else
                {
                    if (borderColor != -1)
                    {
                        DrawFrame(bitmapRect, 1, (byte) _borderColor, false);
                    }

                    DrawTextBox();
                }
            }

            return _bitmap;
        }


        public short GetTextCount(string text, int index, Rect textRect, bool doScaling)
        {
            short scriptWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            short scriptHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

            Rect scaledRect = new Rect(textRect);
            if (doScaling)
            {
                Helpers.Mulinc(ref scaledRect, new Rational(_scaledWidth, scriptWidth),
                    new Rational(_scaledHeight, scriptHeight));
            }

            string oldText = _text;
            _text = text;

            int charIndex = index;
            short maxWidth = scaledRect.Width;
            short lineCount = (short) ((scaledRect.Height - 2) / _font.Height);
            while (lineCount-- != 0)
            {
                GetLongest(ref charIndex, maxWidth);
            }

            _text = oldText;
            return (short) (charIndex - index);
        }

        public short GetTextCount(string text, int index, int fontId, Rect textRect, bool doScaling)
        {
            SetFont(fontId);
            return GetTextCount(text, index, textRect, doScaling);
        }

        public void Erase(Rect rect, bool doScaling)
        {
            throw new System.NotImplementedException();
//            Rect targetRect = doScaling ? scaleRect(rect) : rect;
//
//            SciBitmap bitmap=new SciBitmap(_bitmap);
//            bitmap.getBuffer().fillRect(targetRect, _backColor);
        }

        public void DrawTextBox()
        {
            if (_text.Length == 0)
            {
                return;
            }

            var t = 0;
            var s = 0;
            short textRectWidth = _textRect.Width;
            _drawPosition.Y = _textRect.Top;
            int charIndex = 0;

            if (SciEngine.Instance.GameId == SciGameId.SQ6 || SciEngine.Instance.GameId == SciGameId.MOTHERGOOSEHIRES)
            {
                if (GetLongest(ref charIndex, textRectWidth) == 0)
                {
                    Error("DrawTextBox GetLongest=0");
                }
            }

            charIndex = 0;
            int nextCharIndex = 0;
            while (t < _text.Length)
            {
                _drawPosition.X = _textRect.Left;

                int length = GetLongest(ref nextCharIndex, textRectWidth);
                short textWidth = GetTextWidth(charIndex, length);

                if (_alignment == TextAlign.Center)
                {
                    _drawPosition.X = (short) (_drawPosition.X + (textRectWidth - textWidth) / 2);
                }
                else if (_alignment == TextAlign.Right)
                {
                    _drawPosition.X = (short) (_drawPosition.X + textRectWidth - textWidth);
                }

                DrawText(charIndex, length);
                charIndex = nextCharIndex;
                t = s + charIndex;
                _drawPosition.Y += _font.Height;
            }
        }

        public void DrawTextBox(string text)
        {
            _text = text;
            DrawTextBox();
        }

        private void DrawText(int index, int length)
        {
            System.Diagnostics.Debug.Assert(index + length <= _text.Length);

            // NOTE: This draw loop implementation is somewhat different than the
            // implementation in the actual engine, but should be accurate. Primarily
            // the changes revolve around eliminating some extra temporaries and
            // fixing the logic to match.
            var t = index;
            while (length-- > 0)
            {
                char currentChar = _text[t++];

                if (currentChar == '|')
                {
                    char controlChar = _text[t++];
                    --length;

                    if (length == 0)
                    {
                        return;
                    }

                    if (controlChar == 'a' || controlChar == 'c' || controlChar == 'f')
                    {
                        ushort value = 0;

                        while (length > 0)
                        {
                            char valueChar = _text[t];
                            if (valueChar < '0' || valueChar > '9')
                            {
                                break;
                            }

                            ++t;
                            --length;
                            value = (ushort) (10 * value + (valueChar - '0'));
                        }

                        if (length == 0)
                        {
                            return;
                        }

                        if (controlChar == 'a')
                        {
                            _alignment = (TextAlign) value;
                        }
                        else if (controlChar == 'c')
                        {
                            _foreColor = (byte) value;
                        }
                        else if (controlChar == 'f')
                        {
                            SetFont(value);
                        }
                    }

                    while (length > 0 && _text[t] != '|')
                    {
                        ++t;
                        --length;
                    }
                    if (length > 0)
                    {
                        ++t;
                        --length;
                    }
                }
                else
                {
                    DrawChar(currentChar);
                }
            }
        }

        private void DrawChar(char charIndex)
        {
            var bitmap = _segMan.GetHunkPointer(_bitmap);
            var pixels = new BytePtr(bitmap, (int) bitmap.ReadSci11EndianUInt32(28));

            _font.DrawToBuffer(charIndex, _drawPosition.Y, _drawPosition.X, _foreColor, _dimmed, pixels, _width, _height);
            _drawPosition.X += _font.GetCharWidth(charIndex);
        }

        private int GetLongest(ref int charIndex, short width)
        {
            System.Diagnostics.Debug.Assert(width > 0);

            int testLength = 0;
            int length = 0;

            int initialCharIndex = charIndex;

            // The index of the next word after the last word break
            int lastWordBreakIndex = charIndex;

            var t = charIndex;

            while (t++ < _text.Length)
            {
                var currentChar = _text[t];
                // NOTE: In the original engine, the font, color, and alignment were
                // reset here to their initial values

                // The text to render contains a line break; stop at the line break
                if (currentChar == '\r' || currentChar == '\n')
                {
                    // Skip the rest of the line break if it is a Windows-style
                    // \r\n or non-standard \n\r
                    // NOTE: In the original engine, the `text` pointer had not been
                    // advanced yet so the indexes used to access characters were
                    // one higher
                    if (
                        (currentChar == '\r' && _text[t] == '\n') ||
                        (currentChar == '\n' && _text[t] == '\r' && _text[t + 1] != '\n')
                    )
                    {
                        ++charIndex;
                    }

                    // We are at the end of a line but the last word in the line made
                    // it too wide to fit in the text area; return up to the previous
                    // word
                    if (length != 0 && GetTextWidth(initialCharIndex, testLength) > width)
                    {
                        charIndex = lastWordBreakIndex;
                        return length;
                    }

                    // Skip the line break and return all text seen up to now
                    // NOTE: In original engine, the font, color, and alignment were
                    // reset, then getTextWidth was called to use its side-effects to
                    // set font, color, and alignment according to the text from
                    // `initialCharIndex` to `testLength`
                    ++charIndex;
                    return testLength;
                }
                if (currentChar == ' ')
                {
                    // The last word in the line made it too wide to fit in the text area;
                    // return up to the previous word, then collapse the whitespace
                    // between that word and its next sibling word into the line break
                    if (GetTextWidth(initialCharIndex, testLength) > width)
                    {
                        charIndex = lastWordBreakIndex;
                        var n = lastWordBreakIndex;
                        while (_text[n++] == ' ')
                        {
                            ++charIndex;
                        }

                        // NOTE: In original engine, the font, color, and alignment were
                        // set here to the values that were seen at the last space character
                        return length;
                    }

                    // NOTE: In the original engine, the values of _fontId, _foreColor,
                    // and _alignment were stored for use in the return path mentioned
                    // just above here

                    // We found a word break that was within the text area, memorise it
                    // and continue processing. +1 on the character index because it has
                    // not been incremented yet so currently points to the word break
                    // and not the word after the break
                    length = testLength;
                    lastWordBreakIndex = charIndex + 1;
                }

                // In the middle of a line, keep processing
                ++charIndex;
                ++testLength;

                // NOTE: In the original engine, the font, color, and alignment were
                // reset here to their initial values

                // The text to render contained no word breaks yet but is already too
                // wide for the text area; just split the word in half at the point
                // where it overflows
                if (length != 0 || GetTextWidth(initialCharIndex, testLength) <= width) continue;

                charIndex = --testLength + lastWordBreakIndex;
                return testLength;
            }

            // The complete text to render was a single word, or was narrower than
            // the text area, so return the entire line
            if (length == 0 || GetTextWidth(initialCharIndex, testLength) <= width)
            {
                // NOTE: In original engine, the font, color, and alignment were
                // reset, then getTextWidth was called to use its side-effects to
                // set font, color, and alignment according to the text from
                // `initialCharIndex` to `testLength`
                return testLength;
            }

            // The last word in the line made it wider than the text area, so return
            // up to the penultimate word
            charIndex = lastWordBreakIndex;
            return length;
        }

        public short GetTextWidth(string text, int index, int length)
        {
            _text = text;
            return (short) ScaleUpWidth(GetTextWidth(index, length));
        }

        public short GetTextWidth(int index, int length)
        {
            short width = 0;

            var t = index;

            GfxFont font = _font;

            char currentChar = _text[t++];
            while (length > 0 && currentChar != '\0')
            {
                // Control codes are in the format `|<code><value>|`
                if (currentChar == '|')
                {
                    // NOTE: Original engine code changed the global state of the
                    // FontMgr here upon encountering any color, alignment, or
                    // font control code.
                    // To avoid requiring all callers to manually restore these
                    // values on every call, we ignore control codes other than
                    // font change (since alignment and color do not change the
                    // width of characters), and simply update the font pointer
                    // on stack instead of the member property font.
                    currentChar = _text[t++];
                    --length;

                    if (length > 0 && currentChar == 'f')
                    {
                        int fontId = 0;
                        do
                        {
                            currentChar = _text[t++];
                            --length;

                            fontId = fontId * 10 + currentChar - '0';
                        } while (length > 0 && _text[t] >= '0' && _text[t] <= '9');

                        if (length > 0)
                        {
                            font = _cache.GetFont(fontId);
                        }
                    }

                    // Forward through any more unknown control character data
                    while (length > 0 && _text[t] != '|')
                    {
                        ++t;
                        --length;
                    }
                    if (length > 0)
                    {
                        ++t;
                        --length;
                    }
                }
                else
                {
                    width += font.GetCharWidth(currentChar);
                }

                if (length <= 0) continue;

                currentChar = _text[t++];
                --length;
            }

            return width;
        }

        private Rect ScaleRect(Rect rect)
        {
            Rect scaledRect = new Rect(rect);
            short scriptWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            short scriptHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;
            var scaleX = new Rational(_scaledWidth, scriptWidth);
            var scaleY = new Rational(_scaledHeight, scriptHeight);
            Helpers.Mulinc(ref scaledRect, scaleX, scaleY);
            return scaledRect;
        }

        void DrawFrame(Rect rect, short size, byte color, bool doScaling)
        {
            Rect targetRect = doScaling ? ScaleRect(rect) : rect;

            var bitmap = _segMan.GetHunkPointer(_bitmap);
            var pixels = new BytePtr(bitmap, (int) bitmap.ReadSci11EndianUInt32(28) + rect.Top * _width + rect.Left);

            // NOTE: Not fully disassembled, but this should be right
            short rectWidth = targetRect.Width;
            short sidesHeight = (short) (targetRect.Height - size * 2);
            short centerWidth = (short) (rectWidth - size * 2);
            short stride = (short) (_width - rectWidth);

            for (short y = 0; y < size; ++y)
            {
                pixels.Data.Set(pixels.Offset, color, rectWidth);
                pixels.Offset += _width;
            }
            for (short y = 0; y < sidesHeight; ++y)
            {
                for (short x = 0; x < size; ++x)
                {
                    pixels[0] = color;
                    pixels.Offset++;
                }
                pixels.Offset += centerWidth;
                for (short x = 0; x < size; ++x)
                {
                    pixels[0] = color;
                    pixels.Offset++;
                }
                pixels.Offset += stride;
            }
            for (short y = 0; y < size; ++y)
            {
                pixels.Data.Set(pixels.Offset, color, rectWidth);
                pixels.Offset += _width;
            }
        }

        public Rect GetTextSize(string text, short maxWidth, bool doScaling)
        {
            // NOTE: Like most of the text rendering code, this function was pretty
            // weird in the original engine. The initial result rectangle was actually
            // a 1x1 rectangle (0, 0, 0, 0), which was then "fixed" after the main
            // text size loop finished running by subtracting 1 from the right and
            // bottom edges. Like other functions in SCI32, this has been converted
            // to use exclusive rects with inclusive rounding.

            Rect result = new Rect();

            short scriptWidth = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            short scriptHeight = (short) SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

            maxWidth = (short) (maxWidth * _scaledWidth / scriptWidth);

            _text = text;

            if (maxWidth >= 0)
            {
                if (maxWidth == 0)
                {
                    maxWidth = (short) (_scaledWidth * 3 / 5);
                }

                result.Right = maxWidth;

                short textWidth = 0;
                if (_text.Length > 0)
                {
                    var rawText = 0;
                    var sourceText = 0;
                    int charIndex = 0;
                    int nextCharIndex = 0;
                    while (rawText < _text.Length)
                    {
                        int length = GetLongest(ref nextCharIndex, result.Width);
                        textWidth = Math.Max(textWidth, GetTextWidth(charIndex, length));
                        charIndex = nextCharIndex;
                        rawText = sourceText + charIndex;
                        // TODO: Due to getLongest and getTextWidth not having side
                        // effects, it is possible that the currently loaded font's
                        // height is wrong for this line if it was changed inline
                        result.Bottom += _font.Height;
                    }
                }

                if (textWidth < maxWidth)
                {
                    result.Right = textWidth;
                }
            }
            else
            {
                result.Right = GetTextWidth(0, 10000);

                if (ResourceManager.GetSciVersion() < SciVersion.V2_1_MIDDLE)
                {
                    result.Bottom = 0;
                }
                else
                {
                    // NOTE: In the original engine code, the bottom was not decremented
                    // by 1, which means that the rect was actually a pixel taller than
                    // the height of the font. This was not the case in the other branch,
                    // which decremented the bottom by 1 at the end of the loop.
                    result.Bottom = (short) (_font.Height + 1);
                }
            }

            if (doScaling)
            {
                // NOTE: The original engine code also scaled top/left but these are
                // always zero so there is no reason to do that.
                result.Right = (short) (((result.Right - 1) * scriptWidth + _scaledWidth - 1) / _scaledWidth + 1);
                result.Bottom = (short) (((result.Bottom - 1) * scriptHeight + _scaledHeight - 1) / _scaledHeight + 1);
            }

            return result;
        }

        public int ScaleUpWidth(int value)
        {
            int scriptWidth = SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            return (value * scriptWidth + _scaledWidth - 1) / _scaledWidth;
        }

        public int ScaleUpHeight(byte value)
        {
            int scriptHeight = SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;
            return (value * scriptHeight + _scaledHeight - 1) / _scaledHeight;
        }

        public ushort GetCharWidth(char charIndex, bool doScaling)
        {
            ushort width = _font.GetCharWidth(charIndex);
            if (doScaling)
            {
                width = (ushort) ScaleUpWidth(width);
            }
            return width;
        }

        public short GetStringWidth(string text)
        {
            return GetTextWidth(text, 0, 10000);
        }

        public void InvertRect(Register bitmap, short bitmapStride, Rect rect, byte foreColor, byte backColor,
            bool doScaling)
        {
            Rect targetRect = rect;
            if (doScaling)
            {
                bitmapStride =
                    (short) (bitmapStride * _scaledWidth / SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth);
                targetRect = ScaleRect(rect);
            }

            var bitmapData = _segMan.GetHunkPointer(bitmap);

            // NOTE: SCI code is super weird here; it seems to be trying to look at the
            // entire size of the bitmap including the header, instead of just the pixel
            // data size. We just look at the pixel size. This function generally is an
            // odd duck since the stride dimension for a bitmap is built in to the bitmap
            // header, so perhaps it was once an unheadered bitmap format and this
            // function was never updated to match? Or maybe they exploit the
            // configurable stride length somewhere else to do stair stepping inverts...
            int invertSize = targetRect.Height * bitmapStride + targetRect.Width;
            int bitmapSize = (int) bitmapData.ReadSci11EndianUInt32(12);

            if (invertSize >= bitmapSize)
            {
                Error("InvertRect too big: {0} >= {1}", invertSize, bitmapSize);
            }

            // NOTE: Actual engine just added the bitmap header size hardcoded here
            var pixel = new BytePtr(bitmapData, (int) bitmapData.ReadSci11EndianUInt32(28) +
                                                bitmapStride * targetRect.Top + targetRect.Left);

            short stride = (short) (bitmapStride - targetRect.Width);
            short targetHeight = targetRect.Height;
            short targetWidth = targetRect.Width;

            for (var y = 0; y < targetHeight; ++y)
            {
                for (var x = 0; x < targetWidth; ++x)
                {
                    if (pixel.Value == foreColor)
                    {
                        pixel.Value = backColor;
                    }
                    else if (pixel.Value == backColor)
                    {
                        pixel.Value = foreColor;
                    }

                    pixel.Offset++;
                }

                pixel.Offset += stride;
            }
        }
    }
}

#endif
