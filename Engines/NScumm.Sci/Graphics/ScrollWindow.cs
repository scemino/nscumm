//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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
using System.Collections.Generic;
using System.Text;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// A scrollable text window.
    /// </summary>
    internal class ScrollWindow
    {
        /**
	 * The text renderer.
	 */
        private GfxText32 _gfxText32;

        /**
         * The individual text entries added to the
         * ScrollWindow.
         */
        private List<ScrollWindowEntry> _entries;

        /**
         * The maximum number of entries allowed. Once this
         * limit is reached, the oldest entry will be removed
         * when a new entry is added.
         */
        private uint _maxNumEntries;

        /**
         * A mapping from a line index to the line's character
         * offset in `_text`.
         */
        private List<int> _startsOfLines;

        /**
         * All text added to the window.
         */
        private string _text;

        /**
         * Text that is within the viewport of the ScrollWindow.
         */
        private string _visibleText;

        /**
         * The offset of the first visible character in `_text`.
         */
        private int _firstVisibleChar;

        /**
         * The index of the line that is at the top of the
         * viewport.
         */
        private int _topVisibleLine;

        /**
         * The index of the last visible character in `_text`,
         * or -1 if there is no text.
         */
        private int _lastVisibleChar;

        /**
         * The index of the line that is at the bottom of the
         * viewport, or -1 if there is no text.
         */
        private int _bottomVisibleLine;

        /**
         * The total number of lines in the backbuffer. This
         * number may be higher than the total number of entries
         * if an entry contains newlines.
         */
        private int _numLines;

        /**
         * The number of lines that are currently visible in the
         * text area of the window.
         */
        private int _numVisibleLines;

        /**
         * The plane in which the ScrollWindow should be
         * rendered.
         */
        private Register _plane;

        /**
         * The default text color.
         */
        private byte _foreColor;

        /**
         * The default background color of the text bitmap.
         */
        private byte _backColor;

        /**
         * The default border color of the text bitmap. If -1,
         * the viewport will have no border.
         */
        private short _borderColor;

        /**
         * The default font used for rendering text into the
         * ScrollWindow.
         */
        private int _fontId;

        /**
         * The default text alignment used for rendering text
         * into the ScrollWindow.
         */
        private TextAlign _alignment;

        /**
         * The visibility of the ScrollWindow.
         */
        private bool _visible;

        /**
         * The dimensions of the text box inside the font
         * bitmap, in text-system coordinates.
         */
        private Rect _textRect;

        /**
         * The top-left corner of the ScrollWindow's screen
         * item, in game script coordinates, relative to the
         * parent plane.
         */
        private Point _position;

        /**
         * The height of the default font in screen pixels. All
         * fonts rendered into the ScrollWindow must have this
         * same height.
         */
        private byte _pointSize;

        /**
         * The bitmap used to render text.
         */
        private Register _bitmap;

        /**
         * A monotonically increasing ID used to identify
         * text entries added to the ScrollWindow.
         */
        private ushort _nextEntryId;

        /**
         * The ScrollWindow's screen item.
         */
        private ScreenItem _screenItem;

        public Rational Where => new Rational(_topVisibleLine, Math.Max(_numLines, 1));

        public ScrollWindow(SegManager segMan, ref Rect gameRect, ref Point position, Register plane,
            byte defaultForeColor,
            byte defaultBackColor, int defaultFontId, TextAlign defaultAlignment, short defaultBorderColor,
            ushort maxNumEntries)
        {
            _startsOfLines = new List<int>();
            _gfxText32 = new GfxText32(segMan, SciEngine.Instance._gfxCache);
            _maxNumEntries = maxNumEntries;
            _plane = plane;
            _foreColor = defaultForeColor;
            _backColor = defaultBackColor;
            _borderColor = defaultBorderColor;
            _fontId = defaultFontId;
            _alignment = defaultAlignment;
            _position = position;
            _nextEntryId = 1;

            _entries = new List<ScrollWindowEntry>(maxNumEntries);

            _gfxText32.SetFont(_fontId);
            _pointSize = _gfxText32._font.Height;

            ushort scriptWidth = SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            ushort scriptHeight = SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

            Rect bitmapRect = new Rect(gameRect);
            Helpers.Mulinc(ref bitmapRect, new Rational(GfxText32._scaledWidth, scriptWidth),
                new Rational(GfxText32._scaledHeight, scriptHeight));

            _textRect.Left = 2;
            _textRect.Top = 2;
            _textRect.Right = (short) (bitmapRect.Width - 2);
            _textRect.Bottom = (short) (bitmapRect.Height - 2);

            byte skipColor = 0;
            while (skipColor == _foreColor || skipColor == _backColor)
            {
                skipColor++;
            }

            System.Diagnostics.Debug.Assert(bitmapRect.Width > 0 && bitmapRect.Height > 0);
            _bitmap = _gfxText32.CreateFontBitmap(bitmapRect.Width, bitmapRect.Height, _textRect, string.Empty,
                _foreColor, _backColor, skipColor, _fontId, _alignment, _borderColor, false, false, false);

            DebugC(1, DebugLevels.Graphics, "New ScrollWindow: textRect size: {0} x {1}, bitmap: {2}",
                _textRect.Width,
                _textRect.Height, _bitmap);
        }

        public void Show()
        {
            if (_visible)
            {
                return;
            }

            if (_screenItem == null)
            {
                var celInfo = new CelInfo32
                {
                    type = CelType.Mem,
                    bitmap = _bitmap
                };
                _screenItem = new ScreenItem(_plane, celInfo, _position, new ScaleInfo());
            }

            Plane plane = SciEngine.Instance._gfxFrameout.GetPlanes().FindByObject(_plane);
            plane._screenItemList.Add(_screenItem);

            _visible = true;
        }

        public void Hide()
        {
            if (!_visible)
            {
                return;
            }

            SciEngine.Instance._gfxFrameout.DeleteScreenItem(_screenItem, _plane);
            _screenItem = null;
            SciEngine.Instance._gfxFrameout.FrameOut(true);

            _visible = false;
        }

        public void Go(Rational location)
        {
            int line = location * _numLines;
            if (line < 0 || line > _numLines)
            {
                DebugHelper.Error("Index is Out of Range in ScrollWindow");
            }

            _firstVisibleChar = _startsOfLines[line];
            Update(true);

            // HACK:
            // It usually isn't possible to set _topVisibleLine >= _numLines, and so
            // update() doesn't. However, in this case we should set _topVisibleLine
            // past the end. This is clearly visible in Phantasmagoria when dragging
            // the slider in the About dialog to the very end. The slider ends up lower
            // than where it can be moved by scrolling down with the arrows.
            if (location.IsOne)
            {
                _topVisibleLine = _numLines;
            }
        }

        public Register Modify(Register id, string text, int fontId, short foreColor, TextAlign alignment,
            bool scrollTo)
        {
            ScrollWindowEntry entry = null;
            int firstCharLocation = 0;
            foreach (var e in _entries)
            {
                if (e.id == id)
                {
                    entry = e;
                    break;
                }
                firstCharLocation += e.text.Length;
            }

            if (entry == null)
            {
                return Register.Make(0, 0);
            }

            var oldTextLength = entry.text.Length;

            FillEntry(entry, text, fontId, foreColor, alignment);
            _text = _text.Substring(firstCharLocation, oldTextLength);
            _text = _text.Insert(firstCharLocation, entry.text);

            if (scrollTo)
            {
                _firstVisibleChar = firstCharLocation;
            }

            ComputeLineIndices();
            Update(true);

            return entry.id;
        }

        public void PageUp()
        {
            if (_topVisibleLine == 0)
            {
                return;
            }

            _topVisibleLine -= _numVisibleLines;
            if (_topVisibleLine < 0)
            {
                _topVisibleLine = 0;
            }

            _firstVisibleChar = _startsOfLines[_topVisibleLine];
            Update(true);
        }

        public void PageDown()
        {
            if (_topVisibleLine + 1 >= _numLines)
            {
                return;
            }

            _topVisibleLine += _numVisibleLines;
            if (_topVisibleLine + 1 >= _numLines)
            {
                _topVisibleLine = _numLines - 1;
            }

            _firstVisibleChar = _startsOfLines[_topVisibleLine];
            Update(true);
        }

        public void UpArrow()
        {
            if (_topVisibleLine == 0)
            {
                return;
            }

            _topVisibleLine--;
            _bottomVisibleLine--;

            if (_bottomVisibleLine - _topVisibleLine + 1 < _numVisibleLines)
            {
                _bottomVisibleLine = _numLines - 1;
            }

            _firstVisibleChar = _startsOfLines[_topVisibleLine];
            _lastVisibleChar = _startsOfLines[_bottomVisibleLine + 1] - 1;

            _visibleText = _text.Substring(_firstVisibleChar, _lastVisibleChar + 1);

            string lineText = _text.Substring(_startsOfLines[_topVisibleLine], _startsOfLines[_topVisibleLine + 1] - 1);

            DebugC(3, DebugLevels.Graphics,
                "ScrollWindow::upArrow: top: {0}, bottom: {1}, num: {2}, numvis: {3}, lineText: {4}", _topVisibleLine,
                _bottomVisibleLine, _numLines, _numVisibleLines, lineText);

            _gfxText32.ScrollLine(lineText, _numVisibleLines, _foreColor, _alignment, _fontId, ScrollDirection.Up);

            if (_visible)
            {
                System.Diagnostics.Debug.Assert(_screenItem != null);

                _screenItem.Update();
                SciEngine.Instance._gfxFrameout.FrameOut(true);
            }
        }

        public void DownArrow()
        {
            if (_topVisibleLine + 1 >= _numLines)
            {
                return;
            }

            _topVisibleLine++;
            _bottomVisibleLine++;

            if (_bottomVisibleLine + 1 >= _numLines)
            {
                _bottomVisibleLine = _numLines - 1;
            }

            _firstVisibleChar = _startsOfLines[_topVisibleLine];
            _lastVisibleChar = _startsOfLines[_bottomVisibleLine + 1] - 1;

            _visibleText = _text.Substring(_firstVisibleChar, _lastVisibleChar + 1);

            string lineText;
            if (_bottomVisibleLine - _topVisibleLine + 1 == _numVisibleLines)
            {
                lineText = _text.Substring(_startsOfLines[_bottomVisibleLine],
                    _startsOfLines[_bottomVisibleLine + 1] - 1);
            }
            else
            {
                lineText = string.Empty;
                // scroll in empty string
            }

            DebugC(3, DebugLevels.Graphics,
                "ScrollWindow::downArrow: top: {0}, bottom: {1}, num: {2}, numvis: {3}, lineText: {4}",
                _topVisibleLine, _bottomVisibleLine, _numLines, _numVisibleLines, lineText);


            _gfxText32.ScrollLine(lineText, _numVisibleLines, _foreColor, _alignment, _fontId, ScrollDirection.Down);

            if (_visible)
            {
                System.Diagnostics.Debug.Assert(_screenItem != null);

                _screenItem.Update();
                SciEngine.Instance._gfxFrameout.FrameOut(true);
            }
        }

        public void Home()
        {
            if (_firstVisibleChar == 0)
            {
                return;
            }

            _firstVisibleChar = 0;
            Update(true);
        }

        public void End()
        {
            if (_bottomVisibleLine + 1 >= _numLines)
            {
                return;
            }

            int line = _numLines - _numVisibleLines;
            if (line < 0)
            {
                line = 0;
            }
            _firstVisibleChar = _startsOfLines[line];
            Update(true);
        }

        public Register Add(string text, int fontId, short foreColor, TextAlign alignment, bool scrollTo)
        {
            if (_entries.Count == _maxNumEntries)
            {
                ScrollWindowEntry removedEntry = _entries[0];
                _entries.Remove(removedEntry);
                _text = _text.Substring(removedEntry.text.Length);
                // `_firstVisibleChar` will be reset shortly if
                // `scrollTo` is true, so there is no reason to
                // update it
                if (!scrollTo)
                {
                    _firstVisibleChar -= removedEntry.text.Length;
                }
            }

            var entry = new ScrollWindowEntry();
            _entries.Add(entry);

            // NOTE: In SSCI the line ID was a memory handle for the
            // string of this line. We use a numeric ID instead.
            entry.id = Register.Make(0, _nextEntryId++);

            if (_nextEntryId > _maxNumEntries)
            {
                _nextEntryId = 1;
            }

            // NOTE: In SSCI this was updated after _text was
            // updated, which meant there was an extra unnecessary
            // subtraction operation (subtracting `entry.text` size)
            if (scrollTo)
            {
                _firstVisibleChar = _text.Length;
            }

            FillEntry(entry, text, fontId, foreColor, alignment);
            _text += entry.text;

            ComputeLineIndices();
            Update(true);

            return entry.id;
        }

        private void Update(bool doFrameOut)
        {
            _topVisibleLine = 0;
            while (
                _topVisibleLine < _numLines - 1 &&
                _firstVisibleChar >= _startsOfLines[_topVisibleLine + 1]
            )
            {
                ++_topVisibleLine;
            }

            _bottomVisibleLine = _topVisibleLine + _numVisibleLines - 1;
            if (_bottomVisibleLine >= _numLines)
            {
                _bottomVisibleLine = _numLines - 1;
            }

            _firstVisibleChar = _startsOfLines[_topVisibleLine];

            if (_bottomVisibleLine >= 0)
            {
                _lastVisibleChar = _startsOfLines[_bottomVisibleLine + 1] - 1;
            }
            else
            {
                _lastVisibleChar = -1;
            }

            _visibleText = _text.Substring(_firstVisibleChar, _lastVisibleChar + 1);

            _gfxText32.Erase(_textRect, false);
            _gfxText32.DrawTextBox(_visibleText);

            if (_visible)
            {
                System.Diagnostics.Debug.Assert(_screenItem != null);

                _screenItem.Update();
                if (doFrameOut)
                {
                    SciEngine.Instance._gfxFrameout.FrameOut(true);
                }
            }
        }

        private void ComputeLineIndices()
        {
            _gfxText32.SetFont(_fontId);
            // NOTE: Unlike SSCI, foreColor and alignment are not
            // set since these properties do not affect the width of
            // lines

            if (_gfxText32._font.Height != _pointSize)
            {
                DebugHelper.Error("Illegal font size font = {0} pointSize = {1}, should be {2}.", _fontId,
                    _gfxText32._font.Height, _pointSize);
            }

            Rect lineRect = new Rect(0, 0, _textRect.Width, (short) (_pointSize + 3));

            _startsOfLines.Clear();

            // NOTE: The original engine had a 1000-line limit; we
            // do not enforce any limit
            for (var charIndex = 0; charIndex < _text.Length;)
            {
                _startsOfLines.Add(charIndex);
                charIndex += _gfxText32.GetTextCount(_text, charIndex, lineRect, false);
            }

            _numLines = _startsOfLines.Count;

            _startsOfLines.Add(_text.Length);

            _lastVisibleChar = _gfxText32.GetTextCount(_text, 0, _fontId, _textRect, false) - 1;

            _bottomVisibleLine = 0;
            while (
                _bottomVisibleLine < _numLines - 1 &&
                _startsOfLines[_bottomVisibleLine + 1] < _lastVisibleChar
            )
            {
                ++_bottomVisibleLine;
            }

            _numVisibleLines = _bottomVisibleLine + 1;
        }


        private static void FillEntry(ScrollWindowEntry entry, string text, int fontId, short foreColor,
            TextAlign alignment)
        {
            entry.alignment = alignment;
            entry.foreColor = foreColor;
            entry.fontId = fontId;

            var formattedText = new StringBuilder();

            // NB: There are inconsistencies here.
            // If there is a multi-line entry with non-default properties, and it
            // is only partially displayed, it may not be displayed right, since the
            // property directives are only added to the first line.
            // (Verified by trying this in SSCI SQ6 with a custom ScrollWindowAdd call.)
            //
            // The converse is also a potential issue (but unverified), where lines
            // with properties -1 can inherit properties from the previously rendered
            // line instead of the defaults.

            // NOTE: SSCI added "|s<lineIndex>|" here, but |s| is
            // not a valid control code, so it just always ended up
            // getting skipped
            if (entry.fontId != -1)
            {
                formattedText.Append($"|f{entry.fontId}|");
            }
            if (entry.foreColor != -1)
            {
                formattedText.Append($"|c{entry.foreColor}|");
            }
            if (entry.alignment != TextAlign.Default)
            {
                formattedText.Append($"|a{entry.alignment}|");
            }
            formattedText.Append(text);
            entry.text = formattedText.ToString();
        }
    }
}

#endif
