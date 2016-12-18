//
//  AgosEngine.Window.cs
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
using System.Linq;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        private int GetWindowNum(WindowBlock window)
        {
            int i;

            for (i = 0; i != _windowArray.Length; i++)
                if (_windowArray[i] == window)
                    return i;

            Error("getWindowNum: not found");
            return 0; // for compilers that don't support NORETURN
        }

        private WindowBlock OpenWindow(uint x, uint y, uint w, uint h, uint flags, uint fillColor, uint textColor)
        {
            var window = _windowList.FirstOrDefault(o => o.mode == 0);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 && y >= 133)
                textColor += 16;

            window.mode = 2;
            window.x = (short) x;
            window.y = (short) y;
            window.width = (short) w;
            window.height = (short) h;
            window.flags = (byte) flags;
            window.fillColor = (byte) fillColor;
            window.textColor = (byte) textColor;
            window.textColumn = 0;
            window.textColumnOffset = 0;
            window.textRow = 0;
            window.scrollY = 0;

            // Characters are 6 pixels
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                window.textMaxLength = (ushort) ((window.width * 8 - 4) / 6);
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                window.textMaxLength = (ushort) (window.width * 8 / 6 + 1);
            else
                window.textMaxLength = (ushort) (window.width * 8 / 6);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                ClearWindow(window);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                _gd.Platform == Platform.Amiga && window.fillColor == 225)
                window.fillColor = (byte) (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR) ? 17 : 241);

            return window;
        }

        protected void ChangeWindow(uint a)
        {
            a &= 7;

            if (_windowArray[a] == null || _curWindow == a)
                return;

            _curWindow = (ushort) a;
            JustifyOutPut(0);
            _textWindow = _windowArray[a];
            JustifyStart();
        }

        private void CloseWindow(uint a)
        {
            if (_windowArray[a] == null)
                return;
            RemoveIconArray((int) a);
            ResetWindow(_windowArray[a]);
            _windowArray[a] = null;
            if (_curWindow == a)
            {
                _textWindow = null;
                ChangeWindow(0);
            }
        }

        private void ClearWindow(WindowBlock window)
        {
            if ((window.flags & 0x10) != 0)
                RestoreWindow(window);
            else
                ColorWindow(window);

            window.textColumn = 0;
            window.textRow = 0;
            window.textColumnOffset = (ushort) ((GameType == SIMONGameType.GType_ELVIRA2) ? 4 : 0);
            window.textLength = 0;
            window.scrollY = 0;
        }

        private void ColorWindow(WindowBlock window)
        {
            ushort y = (ushort) window.y;
            ushort h = (ushort) (window.height * 8);

            if (GameType == SIMONGameType.GType_ELVIRA2 && window.y == 146)
            {
                if (window.fillColor == 1)
                {
                    _displayPalette[33] = Color.FromRgb(48 * 4, 40 * 4, 32 * 4);
                }
                else
                {
                    _displayPalette[33] = Color.FromRgb(56 * 4, 56 * 4, 40 * 4);
                }

                y--;
                h += 2;

                _paletteFlag = 1;
            }

            ColorBlock(window, (ushort) (window.x * 8), y, (ushort) (window.width * 8), h);
        }

        private void ColorBlock(WindowBlock window, ushort x, ushort y, ushort w, ushort h)
        {
            _videoLockOut |= 0x8000;

            LockScreen(screen =>
            {
                var dst = screen.GetBasePtr(x, y);

                byte color = window.fillColor;
                if (GameType == SIMONGameType.GType_ELVIRA2 || GameType == SIMONGameType.GType_WW)
                    color = (byte) (color + dst[0] & 0xF0);

                do
                {
                    dst.Data.Set(dst.Offset, color, w);
                    dst += screen.Pitch;
                } while (--h != 0);
            });

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        private void ResetWindow(WindowBlock window)
        {
            if ((window.flags & 8) != 0)
                RestoreWindow(window);
            window.mode = 0;
        }

        private void RestoreWindow(WindowBlock window)
        {
            _videoLockOut |= 0x8000;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                RestoreBlock((ushort) (window.y + window.height), (ushort) (window.x + window.width),
                    (ushort) window.y, (ushort) window.x);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                if (_restoreWindow6 && _windowArray[2] == window)
                {
                    window = _windowArray[6];
                    _restoreWindow6 = false;
                }

                RestoreBlock((ushort) (window.x * 8), (ushort) window.y,
                    (ushort) ((window.x + window.width) * 8),
                    (ushort) (window.y + window.height * 8));
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
            {
                RestoreBlock((ushort) (window.x * 8), (ushort) window.y,
                    (ushort) ((window.x + window.width) * 8),
                    (ushort) (window.y + window.height * 8 + (window == _windowArray[2] ? 1 : 0)));
            }
            else
            {
                ushort x = (ushort) window.x;
                ushort w = (ushort) window.width;

                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                {
                    // Adjustments to remove inventory arrows
                    if ((x & 1) != 0)
                    {
                        x--;
                        w++;
                    }
                    if ((w & 1) != 0)
                    {
                        w++;
                    }
                }

                RestoreBlock((ushort) (x * 8), (ushort) window.y,
                    (ushort) ((x + w) * 8), (ushort) (window.y + window.height * 8));
            }

            _videoLockOut = (ushort) (_videoLockOut & ~0x8000);
        }

        protected void RestoreBlock(ushort x, ushort y, ushort w, ushort h)
        {
            LockScreen(screen =>
            {
                var dst = screen.Pixels;
                var src = BackGround;

                dst += y * screen.Pitch;
                src += y * _backGroundBuf.Pitch;

                byte paletteMod = 0;
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                    !_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO) && y >= 133)
                    paletteMod = 16;

                while (y < h)
                {
                    for (var i = x; i < w; i++)
                        dst[i] = (byte) (src[i] + paletteMod);
                    y++;
                    dst += screen.Pitch;
                    src += _backGroundBuf.Pitch;
                }
            });
        }

        protected void SetTextColor(uint color)
        {
            WindowBlock window = _windowArray[_curWindow];

            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_32COLOR) && color != 0)
            {
                if (window.fillColor == 17)
                    color = 25;
                else
                    color = 220;
            }

            window.textColor = (byte) color;
        }

        private void SendWindow(uint a)
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN ||
                _textWindow != _windowArray[0])
            {
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                {
                    if ((_textWindow.flags & 1) == 0)
                    {
                        HaltAnimation();
                    }
                }

                WindowPutChar(_textWindow, (byte) a);
            }
        }

        private void WaitWindow(WindowBlock window)
        {
            window.textColumn = (short) ((window.width / 2) - 3);
            window.textRow = (short) (window.height - 1);
            window.textLength = 0;

            var message = "[ OK ]";
            foreach (var c in message)
                WindowPutChar(window, (byte) c);

            var ha = FindEmptyHitArea().Value;
            ha.x = (ushort) ((window.width / 2 + window.x - 3) * 8);
            ha.y = (ushort) (window.height * 8 + window.y - 8);
            ha.width = 48;
            ha.height = 8;
            ha.flags = BoxFlags.kBFBoxInUse;
            ha.id = 0x7FFF;
            ha.priority = 999;

            while (!HasToQuit)
            {
                _lastHitArea = null;
                _lastHitArea3 = null;

                while (!HasToQuit)
                {
                    if (_lastHitArea3 != null)
                        break;
                    Delay(1);
                }

                ha = _lastHitArea;
                if (ha == null)
                {
                }
                else if (ha.id == 0x7FFF)
                {
                    break;
                }
            }

            UndefineBox(0x7FFF);
        }

        private void WriteChar(WindowBlock window, int x, int y, int offs, int val)
        {
            int chr;

            // Clear background of first digit
            window.textColumnOffset = (ushort) offs;
            window.textColor = 0;
            WindowDrawChar(window, x * 8, y, 129);

            if (val != -1)
            {
                // Print first digit
                chr = val / 10 + 48;
                window.textColor = 15;
                WindowDrawChar(window, x * 8, y, (byte) chr);
            }

            offs += 6;
            if (offs >= 7)
            {
                offs -= 8;
                x++;
            }

            // Clear background of second digit
            window.textColumnOffset = (ushort) offs;
            window.textColor = 0;
            WindowDrawChar(window, x * 8, y, 129);

            if (val != -1)
            {
                // Print second digit
                chr = val % 10 + 48;
                window.textColor = 15;
                WindowDrawChar(window, x * 8, y, (byte) chr);
            }
        }
    }
}