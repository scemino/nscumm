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

using System;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;

namespace NScumm.Sci.Graphics
{
    [Flags]
    internal enum ControlStateFlags
    {
        ENABLED = 0x0001,  // 0001 - enabled buttons
        DISABLED = 0x0004,  // 0010 - grayed out buttons
        SELECTED = 0x0008   // 1000 - widgets surrounded by a frame
    }

    // Control types and flags
    internal enum ControlType
    {
        BUTTON = 1,
        TEXT = 2,
        TEXTEDIT = 3,
        ICON = 4,
        LIST = 6,
        LIST_ALIAS = 7,
        DUMMY = 10
    }

    /// <summary>
    /// Controls class, handles drawing of controls in SCI16 (SCI0-SCI1.1) games
    /// </summary>
    internal class GfxControls16
    {
        private GfxPaint16 _paint16;
        private GfxPorts _ports;
        private GfxScreen _screen;
        private GfxText16 _text16;
        private SegManager _segMan;

        // Textedit-Control related
        private Rect _texteditCursorRect;
        private bool _texteditCursorVisible;
        private int _texteditBlinkTime;

        private static readonly string controlListUpArrow = "\x18";
        private static readonly string controlListDownArrow = "\x19";

        public int PicNotValid
        {
            get
            {
                if (ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                    return _screen._picNotValidSci11;
                return _screen._picNotValid;
            }
        }

        public GfxControls16(SegManager segMan, GfxPorts ports, GfxPaint16 paint16, GfxText16 text16, GfxScreen screen)
        {
            _segMan = segMan;
            _ports = ports;
            _paint16 = paint16;
            _text16 = text16;
            _screen = screen;

            _texteditBlinkTime = 0;
            _texteditCursorVisible = false;
        }

        public void KernelDrawButton(Rect rect, Register obj, string text, ushort languageSplitter, int fontId, ControlStateFlags style, bool hilite)
        {
            short sci0EarlyPen = 0, sci0EarlyBack = 0;
            if (!hilite)
            {
                if (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY)
                {
                    // SCI0early actually used hardcoded green/black buttons instead of using the port colors
                    sci0EarlyPen = _ports._curPort.penClr;
                    sci0EarlyBack = _ports._curPort.backClr;
                    _ports.PenColor(0);
                    _ports.BackColor(2);
                }
                rect.Grow(1);
                _paint16.EraseRect(rect);
                _paint16.FrameRect(rect);
                rect.Grow(-2);
                _ports.TextGreyedOutput((style & ControlStateFlags.ENABLED) == 0);
                _text16.Box(text, languageSplitter, false, rect, GfxText16.SCI_TEXT16_ALIGNMENT_CENTER, fontId);
                _ports.TextGreyedOutput(false);
                rect.Grow(1);
                if ((style & ControlStateFlags.SELECTED) != 0)
                    _paint16.FrameRect(rect);
                if (PicNotValid == 0)
                {
                    rect.Grow(1);
                    _paint16.BitsShow(rect);
                }
                if (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY)
                {
                    _ports.PenColor(sci0EarlyPen);
                    _ports.BackColor(sci0EarlyBack);
                }
            }
            else {
                // SCI0early used xor to invert button rectangles resulting in pink/white buttons
                if (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY)
                    _paint16.InvertRectViaXOR(rect);
                else
                    _paint16.InvertRect(rect);
                _paint16.BitsShow(rect);
            }
        }

        public void KernelDrawText(Rect rect, Register controlObject, string text, ushort languageSplitter, int fontId, short alignment, ControlStateFlags style, bool hilite)
        {
            if (!hilite)
            {
                rect.Grow(1);
                _paint16.EraseRect(rect);
                rect.Grow(-1);
                _text16.Box(text, languageSplitter, false, rect, alignment, fontId);
                if ((style & ControlStateFlags.SELECTED) != 0)
                {
                    _paint16.FrameRect(rect);
                }
                if (PicNotValid == 0)
                    _paint16.BitsShow(rect);
            }
            else {
                _paint16.InvertRect(rect);
                _paint16.BitsShow(rect);
            }
        }

        public void KernelDrawTextEdit(Rect rect, Register controlObject, string text, ushort languageSplitter, int fontId, short mode, ControlStateFlags style, short cursorPos, short maxChars, bool hilite)
        {
            Rect textRect = rect;
            ushort oldFontId = (ushort)_text16.FontId;

            rect.Grow(1);
            _texteditCursorVisible = false;
            TexteditCursorErase();
            _paint16.EraseRect(rect);
            _text16.Box(text, languageSplitter, false, textRect, GfxText16.SCI_TEXT16_ALIGNMENT_LEFT, fontId);
            _paint16.FrameRect(rect);
            if ((style & ControlStateFlags.SELECTED) != 0)
            {
                _text16.SetFont(fontId);
                rect.Grow(-1);
                TexteditCursorDraw(rect, text, (ushort)cursorPos);
                _text16.SetFont(oldFontId);
                rect.Grow(1);
            }
            if (PicNotValid == 0)
                _paint16.BitsShow(rect);
        }

        private void TexteditCursorErase()
        {
            if (_texteditCursorVisible)
            {
                _paint16.InvertRect(_texteditCursorRect);
                _paint16.BitsShow(_texteditCursorRect);
                _texteditCursorVisible = false;
            }
            TexteditSetBlinkTime();
        }

        private void TexteditCursorDraw(Rect rect, string text, ushort curPos)
        {
            short textWidth, i;
            if (!_texteditCursorVisible)
            {
                textWidth = 0;
                for (i = 0; i < curPos; i++)
                {
                    textWidth += _text16._font.GetCharWidth(text[i]);
                }
                _texteditCursorRect.Left = (short) (rect.Left + textWidth);
                _texteditCursorRect.Top = rect.Top;
                _texteditCursorRect.Bottom = (short) (_texteditCursorRect.Top + _text16._font.Height);
                _texteditCursorRect.Right = (short) (_texteditCursorRect.Left + (text.Length == curPos ? 1 : _text16._font.GetCharWidth(text[curPos])));
                _paint16.InvertRect(_texteditCursorRect);
                _paint16.BitsShow(_texteditCursorRect);
                _texteditCursorVisible = true;

                TexteditSetBlinkTime();
            }
        }

        private void TexteditSetBlinkTime()
        {
            _texteditBlinkTime = NScumm.Core.ServiceLocator.Platform.GetMilliseconds() + (30 * 1000 / 60);
        }

        public void KernelDrawIcon(Rect rect, Register controlObject, int viewId, short loopNo, short celNo, short priority, short style, bool hilite)
        {
            if (!hilite)
            {
                _paint16.DrawCelAndShow(viewId, loopNo, celNo, (ushort)rect.Left, (ushort)rect.Top, priority, 0);
                if ((style & 0x20) != 0)
                {
                    _paint16.FrameRect(rect);
                }
                if (PicNotValid == 0)
                    _paint16.BitsShow(rect);
            }
            else {
                _paint16.InvertRect(rect);
                _paint16.BitsShow(rect);
            }
        }

        public void KernelDrawList(Rect rect, Register obj, short maxChars, short count, string[] entries, int fontId, ControlStateFlags style, short upperPos, short cursorPos, bool isAlias, bool hilite)
        {
            if (!hilite)
            {
                DrawListControl(rect, obj, maxChars, count, entries, fontId, upperPos, cursorPos, isAlias);
                rect.Grow(1);
                if (isAlias && (style & ControlStateFlags.SELECTED) != 0)
                {
                    _paint16.FrameRect(rect);
                }
                if (PicNotValid == 0)
                    _paint16.BitsShow(rect);
            }
        }

        private void DrawListControl(Rect rect, Register obj, short maxChars, short count, string[] entries, int fontId, short upperPos, short cursorPos, bool isAlias)
        {
            Rect workerRect = rect;
            int oldFontId = _text16.FontId;
            short oldPenColor = _ports._curPort.penClr;
            ushort fontSize = 0;
            short i;
            string listEntry;
            short listEntryLen;
            short lastYpos;

            // draw basic window
            _paint16.EraseRect(workerRect);
            workerRect.Grow(1);
            _paint16.FrameRect(workerRect);

            // draw UP/DOWN arrows
            //  we draw UP arrow one pixel lower than sierra did, because it looks nicer. Also the DOWN arrow has one pixel
            //  line inbetween as well
            // They "fixed" this in SQ4 by having the arrow character start one pixel line later, we don't adjust there
            if (SciEngine.Instance.GameId != SciGameId.SQ4)
                workerRect.Top++;
            _text16.Box(controlListUpArrow, false, workerRect, GfxText16.SCI_TEXT16_ALIGNMENT_CENTER, 0);
            workerRect.Top = (short) (workerRect.Bottom - 10);
            _text16.Box(controlListDownArrow, false, workerRect, GfxText16.SCI_TEXT16_ALIGNMENT_CENTER, 0);

            // Draw inner lines
            workerRect.Top = (short) (rect.Top + 9);
            workerRect.Bottom -= 10;
            _paint16.FrameRect(workerRect);
            workerRect.Grow(-1);

            _text16.SetFont(fontId);
            fontSize = (ushort)_ports._curPort.fontHeight;
            _ports.PenColor(_ports._curPort.penClr); _ports.BackColor(_ports._curPort.backClr);
            workerRect.Bottom = (short) (workerRect.Top + fontSize);
            lastYpos = (short)(rect.Bottom - fontSize);

            // Write actual text
            for (i = upperPos; i < count; i++)
            {
                _paint16.EraseRect(workerRect);
                listEntry = entries[i];
                if (listEntry[0] != 0)
                {
                    _ports.MoveTo((short)workerRect.Left, (short)workerRect.Top);
                    listEntryLen = (short)listEntry.Length;
                    _text16.Draw(listEntry, 0, Math.Min(maxChars, listEntryLen), oldFontId, oldPenColor);
                    if ((!isAlias) && (i == cursorPos))
                    {
                        _paint16.InvertRect(workerRect);
                    }
                }
                workerRect.Translate(0, (short) fontSize);
                if (workerRect.Bottom > lastYpos)
                    break;
            }

            _text16.SetFont(oldFontId);
        }

        public void KernelTexteditChange(Register controlObject, Register eventObject)
        {
            ushort cursorPos = (ushort)SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.cursor);
            ushort maxChars = (ushort)SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.max);
            Register textReference = SciEngine.ReadSelector(_segMan, controlObject, o => o.text);
            string text;
            ushort textSize, eventType, eventKey = 0, modifiers = 0;
            bool textChanged = false;
            bool textAddChar = false;
            Rect rect;

            if (textReference.IsNull)
                throw new InvalidOperationException("kEditControl called on object that doesnt have a text reference");
            text = _segMan.GetString(textReference);

            ushort oldCursorPos = cursorPos;

            if (!eventObject.IsNull)
            {
                textSize = (ushort)text.Length;
                eventType = (ushort)SciEngine.ReadSelectorValue(_segMan, eventObject, o => o.type);

                switch (eventType)
                {
                    case SciEvent.SCI_EVENT_MOUSE_PRESS:
                        // TODO: Implement mouse support for cursor change
                        break;
                    case SciEvent.SCI_EVENT_KEYBOARD:
                        eventKey = (ushort)SciEngine.ReadSelectorValue(_segMan, eventObject, o => o.message);
                        modifiers = (ushort)SciEngine.ReadSelectorValue(_segMan, eventObject, o => o.modifiers);
                        switch (eventKey)
                        {
                            case SciEvent.SCI_KEY_BACKSPACE:
                                if (cursorPos > 0)
                                {
                                    cursorPos--; text = text.Remove(cursorPos, 1);
                                    textChanged = true;
                                }
                                break;
                            case SciEvent.SCI_KEY_DELETE:
                                if (cursorPos < textSize)
                                {
                                    text = text.Remove(cursorPos, 1);
                                    textChanged = true;
                                }
                                break;
                            case SciEvent.SCI_KEY_HOME: // HOME
                                cursorPos = 0; textChanged = true;
                                break;
                            case SciEvent.SCI_KEY_END: // END
                                cursorPos = textSize; textChanged = true;
                                break;
                            case SciEvent.SCI_KEY_LEFT: // LEFT
                                if (cursorPos > 0)
                                {
                                    cursorPos--; textChanged = true;
                                }
                                break;
                            case SciEvent.SCI_KEY_RIGHT: // RIGHT
                                if (cursorPos + 1 <= textSize)
                                {
                                    cursorPos++; textChanged = true;
                                }
                                break;
                            case 3: // returned in SCI1 late and newer when Control - C is pressed
                                if ((modifiers & SciEvent.SCI_KEYMOD_CTRL) != 0)
                                {
                                    // Control-C erases the whole line
                                    cursorPos = 0; text = string.Empty;
                                    textChanged = true;
                                }
                                break;
                            default:
                                if (((modifiers & SciEvent.SCI_KEYMOD_CTRL) != 0) && eventKey == 99)
                                {
                                    // Control-C in earlier SCI games (SCI0 - SCI1 middle)
                                    // Control-C erases the whole line
                                    cursorPos = 0; text = string.Empty;
                                    textChanged = true;
                                }
                                else if (eventKey > 31 && eventKey < 256 && textSize < maxChars)
                                {
                                    // insert pressed character
                                    textAddChar = true;
                                    textChanged = true;
                                }
                                break;
                        }
                        break;
                }
            }

            if (SciEngine.Instance.Vocabulary != null && !textChanged && oldCursorPos != cursorPos)
            {
                //assert(!textAddChar);
                textChanged = SciEngine.Instance.Vocabulary.CheckAltInput(text, cursorPos);
            }

            if (textChanged)
            {
                int oldFontId = _text16.FontId;
                int fontId = (int)SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.font);
                rect = SciEngine.Instance._gfxCompare.GetNSRect(controlObject);

                _text16.SetFont(fontId);
                if (textAddChar)
                {

                    var textPtr = 0;

                    // We check if we are really able to add the new char
                    ushort textWidth = 0;
                    while (textPtr < text.Length)
                        textWidth += _text16._font.GetCharWidth((byte)text[textPtr++]);
                    textWidth += _text16._font.GetCharWidth(eventKey);

                    // Does it fit?
                    if (textWidth >= rect.Width)
                    {
                        _text16.SetFont(oldFontId);
                        return;
                    }

                    text = text.Insert(cursorPos++, new string((char)eventKey, 1));

                    // Note: the following checkAltInput call might make the text
                    // too wide to fit, but SSCI fails to check that too.
                }
                if (SciEngine.Instance.Vocabulary != null)
                    SciEngine.Instance.Vocabulary.CheckAltInput(text, cursorPos);
                TexteditCursorErase();
                _paint16.EraseRect(rect);
                _text16.Box(text, false, rect, GfxText16.SCI_TEXT16_ALIGNMENT_LEFT, -1);
                _paint16.BitsShow(rect);
                TexteditCursorDraw(rect, text, cursorPos);
                _text16.SetFont(oldFontId);
                // Write back string
                _segMan.Strcpy(textReference, text);
            }
            else {
                if (NScumm.Core.ServiceLocator.Platform.GetMilliseconds() >= _texteditBlinkTime)
                {
                    _paint16.InvertRect(_texteditCursorRect);
                    _paint16.BitsShow(_texteditCursorRect);
                    _texteditCursorVisible = !_texteditCursorVisible;
                    TexteditSetBlinkTime();
                }
            }

            SciEngine.WriteSelectorValue(_segMan, controlObject, o => o.cursor, cursorPos);
        }
    }
}
