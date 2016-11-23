//
//  Charset.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core;

namespace NScumm.Agos
{
    partial class AGOSEngine
    {
        private void DoOutput(BytePtr src, int len)
        {
            if (_textWindow == null)
                return;

            while (len-- != 0)
            {
                int idx;
                if (src.Value != 12 && _textWindow.iconPtr != null &&
                    _fcsData1[idx = GetWindowNum(_textWindow)] != 2)
                {
                    _fcsData1[idx] = 2;
                    _fcsData2[idx] = true;
                }

                SendWindow(src.Value);
                src.Offset++;
            }
        }

        private void ClsCheck(WindowBlock window)
        {
            int index = GetWindowNum(window);
            TidyIconArray(index);
            _fcsData1[index] = 0;
        }

        private void TidyIconArray(int i)
        {
            if (_fcsData2[i])
            {
                MouseOff();
                var window = _windowArray[i];
                DrawIconArray(i, window.iconPtr.itemRef, window.iconPtr.line, window.iconPtr.classMask);
                _fcsData2[i] = false;
                MouseOn();
            }
        }

        protected void ShowMessageFormat(string s, params object[] args)
        {
            var buf = string.Format(s, args);

            if (_fcsData1[_curWindow] == 0)
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                {
                    if (_showMessageFlag)
                    {
                        if ((_windowArray[_curWindow].flags & 128) != 0)
                        {
                            HaltAnimation();
                        }
                    }
                }
                OpenTextWindow();
                if (!_showMessageFlag)
                {
                    _windowArray[0] = _textWindow;
                    JustifyStart();
                }
                _showMessageFlag = true;
                _fcsData1[_curWindow] = 1;
            }

            for (var i = 0; i < buf.Length && buf[i] != 0; i++)
                JustifyOutPut((byte) buf[i]);
        }

        private void JustifyStart()
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                _printCharCurPos = _textWindow.textColumn;
                _printCharMaxPos = _textWindow.width;
            }
            else
            {
                _printCharCurPos = (short) _textWindow.textLength;
                _printCharMaxPos = (short) _textWindow.textMaxLength;
            }
            _printCharPixelCount = 0;
            _numLettersToPrint = 0;
            _newLines = 0;
        }

        private void JustifyOutPut(byte chr)
        {
            if (chr == 12)
            {
                _numLettersToPrint = 0;
                _printCharCurPos = 0;
                _printCharPixelCount = 0;
                DoOutput(new byte[] {chr}, 1);
                ClsCheck(_textWindow);
            }
            else if (chr == 0 || chr == ' ' || chr == 10)
            {
                bool fit;

                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                {
                    fit = _printCharMaxPos - _printCharCurPos > _printCharPixelCount;
                }
                else
                {
                    fit = _printCharMaxPos - _printCharCurPos >= _printCharPixelCount;
                }

                if (fit)
                {
                    _printCharCurPos += _printCharPixelCount;
                    DoOutput(_lettersToPrintBuf, _numLettersToPrint);
                    if (_printCharCurPos == _printCharMaxPos)
                    {
                        _printCharCurPos = 0;
                    }
                    else
                    {
                        if (chr != 0)
                            DoOutput(new byte[] {chr}, 1);
                        if (chr == 10)
                            _printCharCurPos = 0;
                        else if (chr != 0)
                            _printCharCurPos += (short) ((_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                                                          _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                                ? GetFeebleFontSize(chr)
                                : 1);
                    }
                }
                else
                {
                    byte newline_character = 10;
                    _printCharCurPos = _printCharPixelCount;
                    DoOutput(new[] {newline_character}, 1);
                    DoOutput(_lettersToPrintBuf, _numLettersToPrint);
                    if (chr == ' ')
                    {
                        DoOutput(new byte[] {chr}, 1);
                        _printCharCurPos += (short) ((_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                                                      _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                            ? GetFeebleFontSize(chr)
                            : 1);
                    }
                    else
                    {
                        DoOutput(new byte[] {chr}, 1);
                        _printCharCurPos = 0;
                    }
                }
                _numLettersToPrint = 0;
                _printCharPixelCount = 0;
            }
            else
            {
                _lettersToPrintBuf[_numLettersToPrint++] = chr;
                _printCharPixelCount += (short) ((_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                                                  _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                    ? GetFeebleFontSize(chr)
                    : 1);
            }
        }

        private void OpenTextWindow()
        {
            if (_textWindow != null)
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                {
                    if ((_textWindow.flags & 0x80) != 0)
                        ClearWindow(_textWindow);
                }
                return;
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                _textWindow = OpenWindow(64, 96, 384, 172, 1, 0, 15);
            else
                _textWindow = OpenWindow(8, 144, 24, 6, 1, 0, 15);
        }

        private void WindowPutChar(WindowBlock window, byte c, byte b = 0)
        {
            byte width = 6;

            if (c == 12)
            {
                ClearWindow(window);
            }
            else if (c == 13 || c == 10)
            {
                WindowNewLine(window);
            }
            else if ((c == 1 && _language != Language.HE_ISR) || (c == 8))
            {
                if (_language == Language.HE_ISR)
                {
                    if (b >= 64 && b < 91)
                        width = _hebrewCharWidths[b - 64];

                    if (window.textLength != 0)
                    {
                        window.textLength--;
                        window.textColumnOffset += width;
                        if (window.textColumnOffset >= 8)
                        {
                            window.textColumnOffset -= 8;
                            window.textColumn--;
                        }
                    }
                }
                else
                {
                    sbyte val = (sbyte) ((c == 8) ? 6 : 4);

                    if (window.textLength != 0)
                    {
                        window.textLength--;
                        window.textColumnOffset = (ushort) (window.textColumnOffset - val);
                        if ((sbyte) window.textColumnOffset < val)
                        {
                            window.textColumnOffset += 8;
                            window.textColumn--;
                        }
                    }
                }
            }
            else if (c >= 32)
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                {
                    // Ignore invalid characters
                    if (c - 32 > 195)
                        return;

                    WindowDrawChar(window, window.textColumn + window.x, window.textRow + window.y, c);
                    window.textColumn = (short) (window.textColumn + GetFeebleFontSize(c));
                    return;
                }

                // Ignore invalid characters
                if (c - 32 > 98)
                    return;

                if (window.textLength == window.textMaxLength)
                {
                    WindowNewLine(window);
                }
                else if (window.textRow == window.height)
                {
                    WindowNewLine(window);
                    window.textRow--;
                }

                if (_language == Language.HE_ISR)
                {
                    if (c >= 64 && c < 91)
                        width = _hebrewCharWidths[c - 64];
                    window.textColumnOffset -= width;
                    if (window.textColumnOffset >= width)
                    {
                        window.textColumnOffset += 8;
                        window.textColumn++;
                    }
                    WindowDrawChar(window, (window.width + window.x - window.textColumn) * 8,
                        window.textRow * 8 + window.y, c);
                    window.textLength++;
                }
                else
                {
                    WindowDrawChar(window, (window.textColumn + window.x) * 8, window.textRow * 8 + window.y, c);

                    window.textLength++;
                    window.textColumnOffset += 6;
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                        _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                    {
                        if (c == 'i' || c == 'l')
                            window.textColumnOffset -= 2;
                    }
                    if (window.textColumnOffset >= 8)
                    {
                        window.textColumnOffset -= 8;
                        window.textColumn++;
                    }
                }
            }
        }

        private void WindowNewLine(WindowBlock window)
        {
            window.textColumn = 0;
            window.textColumnOffset = (ushort) (GameType == SIMONGameType.GType_ELVIRA2 ? 4 : 0);
            window.textLength = 0;

            if (GameType == SIMONGameType.GType_PN)
            {
                window.textRow++;
                if (window.textRow == window.height)
                {
                    WindowScroll(window);
                    window.textRow--;
                }
            }
            else
            {
                if (window.textRow == window.height)
                {
                    if (GameType == SIMONGameType.GType_ELVIRA1 || GameType == SIMONGameType.GType_ELVIRA2 ||
                        GameType == SIMONGameType.GType_WW)
                    {
                        WindowScroll(window);
                    }
                }
                else
                {
                    window.textRow++;
                }
            }
        }

        private void WindowScroll(WindowBlock window)
        {
            _videoLockOut |= 0x8000;

            if (window.height != 1)
            {
                LocksScreen(screen =>
                {
                    ushort w = (ushort)(window.width * 8);
                    ushort h = (ushort)((window.height - 1) * 8);

                    var dst = screen.GetBasePtr(window.x * 8, window.y);
                    var src = dst + 8 * screen.Pitch;

                    do
                    {
                        Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, w);
                        src += screen.Pitch;
                        dst += screen.Pitch;
                    } while (--h != 0);
                });
            }

            ColorBlock(window, (ushort)(window.x * 8), (ushort) ((window.height - 1) * 8 + window.y), (ushort) (window.width * 8), 8);

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }
    }
}