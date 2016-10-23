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
using System.Collections.Generic;
using System.Text;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    [Flags]
    internal enum ScaleSignals32
    {
        kScaleSignalNone = 0,
        kScaleSignalManual = 1,
        kScaleSignalVanishingPoint = 2,
    }

    internal class ScaleInfo
    {
        public int x, y, max;
        public ScaleSignals32 signal;

        public ScaleInfo()
        {
            x = 128;
            y = 128;
            max = 100;
            signal = ScaleSignals32.kScaleSignalNone;
        }
    }

    /// <summary>
    /// Controls class, handles drawing of controls in SCI32 (SCI2, SCI2.1, SCI3) games
    /// </summary>
    internal class GfxControls32
    {
        private const int kMessageBoxOK = 0x0;
        private const int kMessageBoxYesNo = 0x4;

        private SegManager _segMan;
        private GfxCache _gfxCache;
        private GfxText32 _gfxText32;
        private int _nextScrollWindowId;

        /// <summary>
        /// If true, typing will overwrite text that already
        /// exists at the text cursor's current position.
        /// </summary>
        private bool _overwriteMode;

        /// <summary>
        /// The tick at which the text cursor should be toggled
        /// by `flashCursor`.
        /// </summary>
        private uint _nextCursorFlashTick;

        /// <summary>
        /// A lookup table for registered ScrollWindow instances.
        /// </summary>
        Dictionary<ushort, ScrollWindow> _scrollWindows;

        public GfxControls32(SegManager segMan, GfxCache cache, GfxText32 text)
        {
            _segMan = segMan;
            _gfxCache = cache;
            _gfxText32 = text;
            // SSCI used a memory handle for a ScrollWindow object
            // as ID. We use a simple numeric handle instead.
            _nextScrollWindowId = 10000;
            _scrollWindows = new Dictionary<ushort, ScrollWindow>();
        }

        public Register KernelMessageBox(string message, string title, ushort style)
        {
            if (SciEngine.Instance != null)
            {
                SciEngine.Instance.IsPaused = true;
            }

            short result = 0;
            switch (style & 0xF)
            {
                case kMessageBoxOK:
                    result = ShowMessageBox(message, "OK", null, 1, 1);
                    break;
                case kMessageBoxYesNo:
                    result = ShowMessageBox(message, "Yes", "No", 6, 7);
                    break;
                default:
                    Error("Unsupported MessageBox style 0x{0:x}", style & 0xF);
                    break;
            }

            if (SciEngine.Instance != null)
            {
                SciEngine.Instance.IsPaused = false;
            }

            return Register.Make(0, (ushort) result);
        }

        private short ShowMessageBox(string message, string okLabel, string altLabel, short okValue, short altValue)
        {
            throw new NotImplementedException();
        }

        public Register MakeScrollWindow(Rect gameRect, Point position, Register planeObj, byte defaultForeColor,
            byte defaultBackColor, int defaultFontId, TextAlign defaultAlignment, short defaultBorderColor,
            ushort maxNumEntries)
        {
            ScrollWindow scrollWindow = new ScrollWindow(_segMan, ref gameRect, ref position, planeObj,
                defaultForeColor, defaultBackColor, defaultFontId, defaultAlignment, defaultBorderColor, maxNumEntries);

            ushort id = (ushort) _nextScrollWindowId++;
            _scrollWindows[id] = scrollWindow;
            return Register.Make(0, id);
        }

        public ScrollWindow GetScrollWindow(Register id)
        {
            if (!_scrollWindows.ContainsKey(id.ToUInt16()))
            {
                Error("Invalid ScrollWindow ID");
                return null;
            }
            return _scrollWindows[id.ToUInt16()];
        }

        private void EraseCursor(TextEditor editor)
        {
            if (editor.cursorIsDrawn)
            {
                _gfxText32.InvertRect(editor.bitmap, editor.width, editor.cursorRect, editor.foreColor, editor.backColor,
                    true);
                editor.cursorIsDrawn = false;
            }

            _nextCursorFlashTick = SciEngine.Instance.TickCount + 30;
        }

        public Register KernelEditText(Register controlObject)
        {
            SegManager segMan = _segMan;

            var textObject = SciEngine.ReadSelector(_segMan, controlObject, o => o.text);
            var editor = new TextEditor
            {
                text = new StringBuilder(_segMan.GetString(textObject)),
                foreColor = (byte) SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.fore),
                backColor = (byte) SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.back),
                skipColor = (byte) SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.skip),
                fontId = (int) SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.font),
                maxLength = (ushort) SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.width),
                bitmap = SciEngine.ReadSelector(_segMan, controlObject, o => o.bitmap),
                cursorCharPosition = 0,
                cursorIsDrawn = false,
                borderColor = (short) SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.borderColor)
            };

            Register titleObject = SciEngine.ReadSelector(_segMan, controlObject, o => o.title);

            short titleHeight = 0;
            int titleFontId = (int) SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.titleFont);
            if (!titleObject.IsNull)
            {
                GfxFont titleFont = _gfxCache.GetFont(titleFontId);
                titleHeight = (short) (titleHeight + _gfxText32.ScaleUpHeight(titleFont.Height) + 1);
                if (editor.borderColor != -1)
                {
                    titleHeight += 2;
                }
            }

            short width = 0;
            short height = titleHeight;

            GfxFont editorFont = _gfxCache.GetFont(editor.fontId);
            height = (short) (height + _gfxText32.ScaleUpHeight(editorFont.Height) + 1);
            _gfxText32.SetFont(editor.fontId);
            short emSize = (short) _gfxText32.GetCharWidth('M', true);
            width = (short) (width + editor.maxLength * emSize + 1);
            if (editor.borderColor != -1)
            {
                width += 4;
                height += 2;
            }

            Rect editorPlaneRect = new Rect(width, height);
            editorPlaneRect.Translate((short) SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.x),
                (short) SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.y));

            Register planeObj = SciEngine.ReadSelector(_segMan, controlObject, o => o.plane);
            Plane sourcePlane = SciEngine.Instance._gfxFrameout.VisiblePlanes.FindByObject(planeObj);
            if (sourcePlane == null)
            {
                Error("Could not find plane {0}", planeObj);
            }
            editorPlaneRect.Translate(sourcePlane._gameRect.Left, sourcePlane._gameRect.Top);

            editor.textRect = new Rect(2, (short) (titleHeight + 2), (short) (width - 1), (short) (height - 1));
            editor.width = width;

            if (editor.bitmap.IsNull)
            {
                TextAlign alignment = (TextAlign) SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.mode);

                if (titleObject.IsNull)
                {
                    bool dimmed = SciEngine.ReadSelectorValue(_segMan, controlObject, o => o.dimmed) != 0;
                    editor.bitmap = _gfxText32.CreateFontBitmap(width, height, editor.textRect, editor.text.ToString(),
                        editor.foreColor, editor.backColor, editor.skipColor, editor.fontId, alignment,
                        editor.borderColor, dimmed, true, false);
                }
                else
                {
                    Error(
                        "Titled bitmaps are not known to be used by any game. Please submit a bug report with details about the game you were playing and what you were doing that triggered this error. Thanks!");
                }
            }

            DrawCursor(editor);

            Plane plane = new Plane(editorPlaneRect, PlanePictureCodes.kPlanePicTransparent);
            plane.ChangePic();
            SciEngine.Instance._gfxFrameout.AddPlane(plane);

            CelInfo32 celInfo = new CelInfo32
            {
                type = CelType.Mem,
                bitmap = editor.bitmap
            };

            ScreenItem screenItem = new ScreenItem(plane._object, celInfo, new Point(), new ScaleInfo());
            plane._screenItemList.Add(screenItem);

            // frameOut must be called after the screen item is
            // created, and before it is updated at the end of the
            // event loop, otherwise it has both created and updated
            // flags set which crashes the engine (it runs updates
            // before creations)
            SciEngine.Instance._gfxFrameout.FrameOut(true);

            EventManager eventManager = SciEngine.Instance.EventManager;
            bool clearTextOnInput = true;
            bool textChanged = false;
            for (;;)
            {
                // We peek here because the last event needs to be allowed to
                // dispatch a second time to the normal event handling system.
                // In the actual engine, the event is always consumed and then
                // the last event just gets posted back to the event manager for
                // reprocessing, but instead, we only remove the event from the
                // queue *after* we have determined it is not a defocusing event
                SciEvent @event = eventManager.GetSciEvent(SciEvent.SCI_EVENT_ANY | SciEvent.SCI_EVENT_PEEK);

                bool focused = true;
                // Original engine did not have a QUIT event but we have to handle it
                if (@event.type == SciEvent.SCI_EVENT_QUIT)
                {
                    focused = false;
                    break;
                }
                else if (@event.type == SciEvent.SCI_EVENT_MOUSE_PRESS && !editorPlaneRect.Contains(@event.mousePosSci))
                {
                    focused = false;
                }
                else if (@event.type == SciEvent.SCI_EVENT_KEYBOARD)
                {
                    switch (@event.character)
                    {
                        case SciEvent.SCI_KEY_ESC:
                        case SciEvent.SCI_KEY_UP:
                        case SciEvent.SCI_KEY_DOWN:
                        case SciEvent.SCI_KEY_TAB:
                        case SciEvent.SCI_KEY_SHIFT_TAB:
                        case SciEvent.SCI_KEY_ENTER:
                            focused = false;
                            break;
                    }
                }

                if (!focused)
                {
                    break;
                }

                // Consume the event now that we know it is not one of the
                // defocusing events above
                if (@event.type != SciEvent.SCI_EVENT_NONE)
                    eventManager.GetSciEvent(SciEvent.SCI_EVENT_ANY);

                // NOTE: In the original engine, the font and bitmap were
                // reset here on each iteration through the loop, but it
                // doesn't seem like this should be necessary since
                // control is not yielded back to the VM until input is
                // received, which means there is nothing that could modify
                // the GfxText32's state with a different font in the
                // meantime

                bool shouldDeleteChar = false;
                bool shouldRedrawText = false;
                ushort lastCursorPosition = editor.cursorCharPosition;
                if (@event.type == SciEvent.SCI_EVENT_KEYBOARD)
                {
                    switch (@event.character)
                    {
                        case SciEvent.SCI_KEY_LEFT:
                            clearTextOnInput = false;
                            if (editor.cursorCharPosition > 0)
                            {
                                --editor.cursorCharPosition;
                            }
                            break;

                        case SciEvent.SCI_KEY_RIGHT:
                            clearTextOnInput = false;
                            if (editor.cursorCharPosition < editor.text.Length)
                            {
                                ++editor.cursorCharPosition;
                            }
                            break;

                        case SciEvent.SCI_KEY_HOME:
                            clearTextOnInput = false;
                            editor.cursorCharPosition = 0;
                            break;

                        case SciEvent.SCI_KEY_END:
                            clearTextOnInput = false;
                            editor.cursorCharPosition = (ushort) editor.text.Length;
                            break;

                        case SciEvent.SCI_KEY_INSERT:
                            clearTextOnInput = false;
                            // Redrawing also changes the cursor rect to
                            // reflect the new insertion mode
                            shouldRedrawText = true;
                            _overwriteMode = !_overwriteMode;
                            break;

                        case SciEvent.SCI_KEY_DELETE:
                            clearTextOnInput = false;
                            if (editor.cursorCharPosition < editor.text.Length)
                            {
                                shouldDeleteChar = true;
                            }
                            break;

                        case SciEvent.SCI_KEY_BACKSPACE:
                            clearTextOnInput = false;
                            shouldDeleteChar = true;
                            if (editor.cursorCharPosition > 0)
                            {
                                --editor.cursorCharPosition;
                            }
                            break;

                        case SciEvent.SCI_KEY_ETX:
                            editor.text.Clear();
                            editor.cursorCharPosition = 0;
                            shouldRedrawText = true;
                            break;

                        default:
                        {
                            if (@event.character >= 20 && @event.character < 257)
                            {
                                if (clearTextOnInput)
                                {
                                    clearTextOnInput = false;
                                    editor.text.Clear();
                                }

                                if (
                                    (_overwriteMode && editor.cursorCharPosition < editor.maxLength) ||
                                    (editor.text.Length < editor.maxLength &&
                                     _gfxText32.GetCharWidth((char) @event.character, true) +
                                     _gfxText32.GetStringWidth(editor.text.ToString()) < editor.textRect.Width)
                                )
                                {
                                    if (_overwriteMode && editor.cursorCharPosition < editor.text.Length)
                                    {
                                        editor.text[editor.cursorCharPosition] = (char) @event.character;
                                    }
                                    else
                                    {
                                        editor.text.Insert(editor.cursorCharPosition, new[] {(char) @event.character});
                                    }

                                    ++editor.cursorCharPosition;
                                    shouldRedrawText = true;
                                }
                            }
                            break;
                        }
                    }
                }

                if (shouldDeleteChar)
                {
                    shouldRedrawText = true;
                    if (editor.cursorCharPosition < editor.text.Length)
                    {
                        editor.text.Remove(editor.cursorCharPosition, 1);
                    }
                }

                if (shouldRedrawText)
                {
                    EraseCursor(editor);
                    _gfxText32.Erase(editor.textRect, true);
                    _gfxText32.DrawTextBox(editor.text.ToString());
                    DrawCursor(editor);
                    textChanged = true;
                    screenItem._updated = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }
                else if (editor.cursorCharPosition != lastCursorPosition)
                {
                    EraseCursor(editor);
                    DrawCursor(editor);
                    screenItem._updated = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }
                else
                {
                    FlashCursor(editor);
                    screenItem._updated = SciEngine.Instance._gfxFrameout.GetScreenCount();
                }

                SciEngine.Instance._gfxFrameout.FrameOut(true);
                // TODO: SciEngine.Instance.SciDebugger.OnFrame();
                SciEngine.Instance._gfxFrameout.Throttle();
            }

            SciEngine.Instance._gfxFrameout.DeletePlane(plane);
            if (SciEngine.ReadSelectorValue(segMan, controlObject, o => o.frameOut) != 0)
            {
                SciEngine.Instance._gfxFrameout.FrameOut(true);
            }

            _segMan.FreeHunkEntry(editor.bitmap);

            if (textChanged)
            {
                editor.text = new StringBuilder(editor.text.ToString().Trim());
                var @string = _segMan.LookupArray(textObject);
                @string.FromString(editor.text.ToString());
            }

            return Register.Make(0, textChanged);
        }

        public void DestroyScrollWindow(Register id)
        {
            ScrollWindow scrollWindow = GetScrollWindow(id);
            scrollWindow.Hide();
            _scrollWindows.Remove((ushort) id.Offset);
        }

        private void FlashCursor(TextEditor editor)
        {
            if (SciEngine.Instance.TickCount <= _nextCursorFlashTick) return;
            _gfxText32.InvertRect(editor.bitmap, editor.width, editor.cursorRect, editor.foreColor, editor.backColor,
                true);

            editor.cursorIsDrawn = !editor.cursorIsDrawn;
            _nextCursorFlashTick = SciEngine.Instance.TickCount + 30;
        }

        private void DrawCursor(TextEditor editor)
        {
            if (!editor.cursorIsDrawn)
            {
                editor.cursorRect.Left = (short) (editor.textRect.Left +
                                                  _gfxText32.GetTextWidth(editor.text.ToString(), 0,
                                                      editor.cursorCharPosition));

                short scaledFontHeight = (short) _gfxText32.ScaleUpHeight(_gfxText32._font.Height);

                // NOTE: The original code branched on borderColor here but
                // the two branches appeared to be identical, differing only
                // because the compiler decided to be differently clever
                // when optimising multiplication in each branch
                if (_overwriteMode)
                {
                    editor.cursorRect.Top = editor.textRect.Top;
                    editor.cursorRect.Height = scaledFontHeight;
                }
                else
                {
                    editor.cursorRect.Top = (short) (editor.textRect.Top + scaledFontHeight - 1);
                    editor.cursorRect.Height = 1;
                }

                char currentChar = editor.cursorCharPosition < editor.text.Length
                    ? editor.text[editor.cursorCharPosition]
                    : ' ';
                editor.cursorRect.Width = (short) _gfxText32.GetCharWidth(currentChar, true);

                _gfxText32.InvertRect(editor.bitmap, editor.width, editor.cursorRect, editor.foreColor, editor.backColor,
                    true);

                editor.cursorIsDrawn = true;
            }

            _nextCursorFlashTick = SciEngine.Instance.TickCount + 30;
        }
    }
}

#endif
