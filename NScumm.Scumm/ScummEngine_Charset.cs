//
//  ScummEngine_Charset.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm
{
    partial class ScummEngine
    {
        const byte CharsetMaskTransparency = 0xFD;
        protected byte[] _charsetBuffer = new byte[512];
        public byte[] CharsetColorMap = new byte[16];
        public byte[] TownsCharsetColorMap = new byte[16];

        public int TownsPaletteFlags { get; protected set; }

        protected byte[][] _charsetData = CreateCharsetData();
        protected int _charsetBufPos;
        protected CharsetRenderer _charset;
        // Somewhat hackish stuff for 2 byte support (Chinese/Japanese/Korean)
        public byte NewLineCharacter;
        internal bool UseCjkMode;
        protected byte _charsetColor;
        protected int _nextLeft, _nextTop;

        static byte[][] CreateCharsetData()
        {
            var data = new byte[16][];
            for (int i = 0; i < 16; i++)
            {
                data[i] = new byte[23];
            }
            return data;
        }

        void CreateCharset()
        {
            switch (Game.Version)
            {
                case 0:
                case 1:
                    _charset = new CharsetRenderer2(this);
                    break;
                case 2:
                    _charset = new CharsetRenderer2(this);
                    break;
                case 3:
                    if (_game.Platform == Platform.FMTowns)
                        _charset = new CharsetRendererTowns3(this);
                    else
                        _charset = new CharsetRenderer3(this);
                    break;
                case 8:
                    _charset = new CharsetRendererNut(this);
                    break;
                default:
                    if (_game.Platform == Platform.FMTowns)
                    {
                        _charset = new CharsetRendererTownsClassic(this);
                    }
                    else
                    {
                        _charset = new CharsetRendererClassic(this);
                    }
                    break;
            }
        }

        protected void InitCharset(int charsetNum)
        {
            _string[0].Default.Charset = (byte)charsetNum;
            _string[1].Default.Charset = (byte)charsetNum;

            Array.Copy(_charsetData[charsetNum], CharsetColorMap, CharsetColorMap.Length);
        }

        protected void LoadCharset(int no)
        {
            if (Game.Version > 4)
            {
                /* FIXME - hack around crash in Indy4 (occurs if you try to load after dieing) */
                if (Game.GameId == Scumm.IO.GameId.Indy4 && no == 0)
                    no = 1;

                var ptr = ResourceManager.GetCharsetData((byte)no);

                for (var i = 0; i < 15; i++)
                {
                    _charsetData[no][i + 1] = ptr[i + 14];
                }
            }
        }

        protected virtual void Charset()
        {
            byte[] subtitleBuffer = new byte[200];
            var subtitleLine = 0;
            Point subtitlePos = new Point();

            if (Game.Version >= 7)
            {
                ((ScummEngine7)this).ProcessSubtitleQueue();
            }

            if (_haveMsg == 0)
                return;

            if (Game.Version >= 4 && Game.Version <= 6)
            {
                // Do nothing while the camera is moving
                if ((_camera.DestinationPosition.X / 8) != (_camera.CurrentPosition.X / 8) || _camera.CurrentPosition.X != _camera.LastPosition.X)
                    return;
            }

            Actor a = null;
            if (TalkingActor != 0xFF)
                a = Actors[TalkingActor];

            if (a != null && _string[0].Overhead)
            {
                int s;

                _string[0].Position = new Point(
                    (short)(a.Position.X - MainVirtScreen.XStart),
                    (short)(a.Position.Y - a.Elevation - ScreenTop));

                if (_game.Version <= 5)
                {
                    if (_variables[VariableTalkStringY.Value] < 0)
                    {
                        s = (a.ScaleY * _variables[VariableTalkStringY.Value]) / 0xFF;
                        _string[0].Position = _string[0].Position.Offset(0, (short)(((_variables[VariableTalkStringY.Value] - s) / 2) + s));
                    }
                    else
                    {
                        _string[0].Position = new Point(_string[0].Position.X, (short)_variables[VariableTalkStringY.Value]);
                    }
                }
                else
                {
                    s = a.ScaleX * a.TalkPosition.X / 0xFF;
                    var x = _string[0].Position.X + ((a.TalkPosition.X - s) / 2) + s;

                    s = a.ScaleY * a.TalkPosition.Y / 0xFF;
                    var y = _string[0].Position.Y + ((a.TalkPosition.Y - s) / 2) + s;

                    if (y > ScreenHeight - 40)
                        y = ScreenHeight - 40;
                    _string[0].Position = new Point((short)x, (short)y);
                }


                if (_string[0].Position.Y < 1)
                    _string[0].Position = new Point(_string[0].Position.X, 1);

                if (_string[0].Position.X < 80)
                    _string[0].Position = new Point(80, _string[0].Position.Y);
                if (_string[0].Position.X > ScreenWidth - 80)
                    _string[0].Position = new Point((short)(ScreenWidth - 80), _string[0].Position.Y);
            }

            _charset.Top = _string[0].Position.Y + ScreenTop;
            _charset.StartLeft = _charset.Left = _string[0].Position.X;
            _charset.Right = _string[0].Right;
            _charset.Center = _string[0].Center;
            _charset.SetColor(_charsetColor);

            if (a != null && a.Charset != 0)
                _charset.SetCurID(a.Charset);
            else
                _charset.SetCurID(_string[0].Charset);

            if (_game.Version >= 5)
                Array.Copy(_charsetData[_charset.GetCurId()], CharsetColorMap, 4);

            if (_keepText && Game.Platform == Platform.FMTowns)
                _charset.Str = _curStringRect;

            if (_talkDelay != 0)
                return;

            if ((Game.Version <= 6 && _haveMsg == 1) ||
                (Game.Version == 7 && _haveMsg != 1))
            {
                if ((Sound.SfxMode & 2) == 0)
                    StopTalk();
                return;
            }

            if (a != null && !_string[0].NoTalkAnim)
            {
                a.RunTalkScript(a.TalkStartFrame);
                _useTalkAnims = true;
            }

            _talkDelay = VariableDefaultTalkDelay.HasValue ? Variables[VariableDefaultTalkDelay.Value] : 60;

            if (!_keepText)
            {
                if (Game.Version >= 7)
                {
                    ((ScummEngine7)this).ClearSubtitleQueue();
                    _nextLeft = _string[0].Position.X;
                    _nextTop = _string[0].Position.Y + ScreenTop;
                }
                else
                {
                    if (_game.Platform == Platform.FMTowns)
                        TownsRestoreCharsetBg();
                    else
                        RestoreCharsetBg();
                }
            }

            if (Game.Version > 3)
            {
                int maxWidth = _charset.Right - _string[0].Position.X - 1;
                if (_charset.Center)
                {
                    if (maxWidth > _nextLeft)
                        maxWidth = _nextLeft;
                    maxWidth *= 2;
                }

                _charset.AddLinebreaks(0, _charsetBuffer, _charsetBufPos, maxWidth);
            }

            if (_charset.Center)
            {
                _nextLeft -= _charset.GetStringWidth(0, _charsetBuffer, _charsetBufPos) / 2;
                if (_nextLeft < 0)
                    _nextLeft = Game.Version >= 6 ? _string[0].Position.X : 0;
            }

            _charset.DisableOffsX = _charset.FirstChar = !_keepText;

            int c = 0;
            while (HandleNextCharsetCode(a, ref c))
            {
                if (c == 0)
                {
                    // End of text reached, set _haveMsg accordingly
                    _haveMsg = (_game.Version >= 7) ? 2 : 1;
                    _keepText = false;
                    break;
                }

                if (c == 13)
                {
                    if (Game.Version >= 7 && subtitleLine != 0)
                    {
                        ((ScummEngine7)this).AddSubtitleToQueue(subtitleBuffer, 0, subtitlePos, _charsetColor, (byte)_charset.GetCurId());
                        subtitleLine = 0;
                    }

                    if (!NewLine())
                        break;
                    continue;
                }

                // Handle line overflow for V3. See also bug #1306269.
                if (_game.Version == 3 && _nextLeft >= ScreenWidth)
                {
                    _nextLeft = ScreenWidth;
                }
                // Handle line breaks for V1-V2
                if (_game.Version <= 2 && _nextLeft >= ScreenWidth)
                {
                    if (!NewLine())
                        break;  // FIXME: Is this necessary? Only would be relevant for v0 games
                }

                _charset.Left = _nextLeft;
                _charset.Top = _nextTop;

                if (Game.Version >= 7)
                {
                    if (subtitleLine == 0)
                    {
                        subtitlePos.X = (short)_charset.Left;
                        // BlastText position is relative to the top of the screen, adjust y-coordinate
                        subtitlePos.Y = (short)(_charset.Top - ScreenTop);
                    }
                    subtitleBuffer[subtitleLine++] = (byte)c;
                    subtitleBuffer[subtitleLine] = 0;
                }
                else
                {
                    _charset.PrintChar(c, false);
                }
                _nextLeft = _charset.Left;
                _nextTop = _charset.Top;

                if (_game.Version <= 2)
                {
                    _talkDelay += _defaultTalkDelay;
                    Variables[VariableCharCount.Value]++;
                }
                else
                {
                    _talkDelay += _variables[VariableCharIncrement.Value];
                }
            }

            if (_game.Platform == Platform.FMTowns && (c == 0 || c == 2 || c == 3))
                _curStringRect = _charset.Str;

            if (Game.Version >= 7 && subtitleLine != 0)
            {
                ((ScummEngine7)this).AddSubtitleToQueue(subtitleBuffer, 0, subtitlePos, _charsetColor, (byte)_charset.GetCurId());
            }
        }

        bool NewLine()
        {
            _nextLeft = _string[0].Position.X;
            if (_charset.Center)
            {
                _nextLeft -= _charset.GetStringWidth(0, _charsetBuffer, _charsetBufPos) / 2;
                if (_nextLeft < 0)
                    _nextLeft = Game.Version >= 6 ? _string[0].Position.X : 0;
            }

            if (_game.Version == 0)
            {
                return false;
            }
            else if (_game.Platform != Platform.FMTowns && _string[0].Height != 0)
            {
                _nextTop += _charset.GetFontHeight();
            }
            else
            {
                bool useCJK = UseCjkMode;
                // SCUMM5 FM-Towns doesn't use the height of the ROM font here.
//                if (_game.Platform == Platform.FMTowns && Game.Version == 5)
//                    _useCJKMode = false;
                _nextTop += _charset.GetFontHeight();
                UseCjkMode = useCJK;
            }

            if (_game.Version > 3)
            {
                // FIXME: is this really needed?
                _charset.DisableOffsX = true;
            }

            return true;
        }

        internal bool HandleNextCharsetCode(Actor a, ref int code)
        {
            int color, frme, c = 0, oldy;
            bool endLoop = false;

            var bufferPos = _charsetBufPos;
            while (!endLoop)
            {
                c = _charsetBuffer[bufferPos++];
                if (!(c == 0xFF || (Game.Version <= 6 && c == 0xFE)))
                {
                    break;
                }
                c = _charsetBuffer[bufferPos++];

                if (NewLineCharacter != 0 && c == NewLineCharacter)
                {
                    c = 13;
                    break;
                }

                switch (c)
                {
                    case 1:
                        c = 13; // new line
                        endLoop = true;
                        break;

                    case 2:
                        _haveMsg = 0;
                        _keepText = true;
                        endLoop = true;
                        break;

                    case 3:
                        _haveMsg = (_game.Version >= 7) ? 1 : 0xFF;
                        _keepText = false;
                        endLoop = true;
                        break;

                    case 8:
					// Ignore this code here. Occurs e.g. in MI2 when you
					// talk to the carpenter on scabb island. It works like
					// code 1 (=newline) in verb texts, but is ignored in
					// spoken text (i.e. here). Used for very long verb
					// sentences.
                        break;

                    case 9:
                        frme = _charsetBuffer[bufferPos] | (_charsetBuffer[bufferPos + 1] << 8);
                        bufferPos += 2;
                        if (a != null)
                            a.StartAnimActor(frme);
                        break;

                    case 10:
					// Note the similarity to the code in debugMessage()
                        {
                            var talkSound_a = (_charsetBuffer[bufferPos] | (_charsetBuffer[bufferPos + 1] << 8) | (_charsetBuffer[bufferPos + 4] << 16) | (_charsetBuffer[bufferPos + 5] << 24));
                            var talkSound_b = (_charsetBuffer[bufferPos + 8] | (_charsetBuffer[bufferPos + 9] << 8) | (_charsetBuffer[bufferPos + 12] << 16) | (_charsetBuffer[bufferPos + 13] << 24));
                            bufferPos += 14;
                            Sound.TalkSound(talkSound_a, talkSound_b, 2);
                            _haveActorSpeechMsg = false;
                        }
                        break;

                    case 12:
                        color = _charsetBuffer[bufferPos] | (_charsetBuffer[bufferPos + 1] << 8);
                        bufferPos += 2;
                        if (color == 0xFF)
                            _charset.SetColor(_charsetColor);
                        else
                            _charset.SetColor((byte)color);
                        break;

                    case 13:
					//debug(0, "handleNextCharsetCode: Unknown opcode 13 %d", READ_LE_UINT16(buffer));
                        bufferPos += 2;
                        break;

                    case 14:
                        oldy = _charset.GetFontHeight();
                        _charset.SetCurID(_charsetBuffer[bufferPos++]);
                        bufferPos += 2;
                        Array.Copy(_charsetData[_charset.GetCurId()], CharsetColorMap, 4);
                        _nextTop -= _charset.GetFontHeight() - oldy;
                        break;

                    default:
                        throw new NotSupportedException(string.Format("handleNextCharsetCode: invalid code {0}", c));
                }
            }
            _charsetBufPos = bufferPos;
            code = c;
            return (c != 2 && c != 3);
        }

        void RestoreCharsetBg()
        {
            _nextLeft = _string[0].Position.X;
            _nextTop = _string[0].Position.Y + ScreenTop;

            if (_charset.HasMask)
            {
                _charset.HasMask = false;
                _charset.Str.Left = -1;
                _charset.Left = -1;

                // Restore background on the whole text area. This code is based on
                // restoreBackground(), but was changed to only restore those parts which are
                // currently covered by the charset mask.

                var vs = _charset.TextScreen;
                if (vs.Height == 0)
                    return;

                MarkRectAsDirty(vs, 0, vs.Width, 0, vs.Height, Gdi.UsageBitRestored);

                if (vs.HasTwoBuffers && _currentRoom != 0 && IsLightOn())
                {
                    if (vs != MainVirtScreen)
                    {
                        // Restore from back buffer
                        var screenBufNav = new PixelNavigator(vs.Surfaces[0]);
                        screenBufNav.OffsetX(vs.XStart);
                        var backNav = new PixelNavigator(vs.Surfaces[1]);
                        backNav.OffsetX(vs.XStart);
                        Gdi.Blit(screenBufNav, backNav, vs.Width, vs.Height);
                    }
                }
                else
                {
                    // Clear area
                    var screenBuf = vs.Surfaces[0].Pixels;
                    Array.Clear(screenBuf, 0, screenBuf.Length);
                }

                if (vs.HasTwoBuffers)
                {
                    // Clean out the charset mask
                    ClearTextSurface();
                }
            }
        }
    }
}

