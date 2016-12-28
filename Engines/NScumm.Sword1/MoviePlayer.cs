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
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Video;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NScumm.Sword1
{
    internal class MovieText
    {
        public ushort _startFrame;
        public ushort _endFrame;
        public ushort _color;
        public string _text;

        public MovieText(ushort startFrame, ushort endFrame, string text, ushort color)
        {

            _startFrame = startFrame;
            _endFrame = endFrame;
            _text = text;
            _color = color;
        }
    }

    internal class MoviePlayer
    {
        private readonly SwordEngine _vm;
        private VideoDecoder _decoder;
        private List<MovieText> _movieTexts = new List<MovieText>();
        private string _directory;
        private Text _textMan;
        private ResMan _resMan;
        private uint _black;
        private uint _c1Color, _c2Color, _c3Color, _c4Color;
        private ushort _textWidth;
        private ushort _textHeight;
        private int _textX;
        private int _textY;
        private ushort _textColor;
        private static readonly Regex _regex = new Regex(@"(\d+)\s+(\d+)\s+@(\d+)\s+(.*)", RegexOptions.Singleline);

        public MoviePlayer(SwordEngine vm, Text textMan, ResMan resMan)
        {
            _vm = vm;
            _textMan = textMan;
            _resMan = resMan;
            _directory = ServiceLocator.FileStorage.GetDirectoryName(_vm.Settings.Game.Path);
        }

        public void Load(int id)
        {
            string filename, path;

            var directory = ServiceLocator.FileStorage.GetDirectoryName(_vm.Settings.Game.Path);

            LoadSubtitles(id);

            // For the PSX version, we'll try the PlayStation stream files
            if (SystemVars.Platform == Core.IO.Platform.PSX)
            {
                _vm.GraphicsManager.PixelFormat = PixelFormat.Rgb16;

                // The demo uses the normal file names
                filename = (SystemVars.IsDemo ? SequenceList[id] : SequenceListPsx[id]) + ".str";
                _decoder = new PsxStreamDecoder(_vm.Mixer, CDSpeed.CD2x, _vm.GraphicsManager.PixelFormat);
            }
            else
            {

                filename = $"{SequenceList[id]}.smk";
                _decoder = new SmackerDecoder();
            }

            path = ScummHelper.LocatePath(directory, filename);
            var stream = ServiceLocator.FileStorage.OpenFileRead(path);
            _decoder.LoadStream(stream);
            _decoder.Start();
        }

        public void Play()
        {
            bool skipped = false;
            ushort x = (ushort)((_vm.Settings.Game.Width - _decoder.GetWidth()) / 2);
            ushort y = (ushort)((_vm.Settings.Game.Height - _decoder.GetHeight()) / 2);

            while (!Engine.Instance.HasToQuit && !_decoder.EndOfVideo && !skipped)
            {
                if (_decoder.NeedsUpdate)
                {
                    var frame = _decoder.DecodeNextFrame();
                    if (frame != null)
                    {
                        if (SystemVars.Platform == Core.IO.Platform.PSX)
                            DrawFramePSX(frame);
                        else
                            _vm.GraphicsManager.CopyRectToScreen(frame.Pixels, frame.Pitch, x, y, frame.Width, frame.Height);
                    }

                    if (_decoder.HasDirtyPalette)
                    {
                        var palette = ToPalette(_decoder.Palette);
                        _vm.GraphicsManager.SetPalette(palette, 0, 256);

                        UpdateColors();
                    }

                    var pixels = _vm.GraphicsManager.Pixels;
                    PerformPostProcessing(pixels);
                    _vm.GraphicsManager.CopyRectToScreen(pixels, _vm.Settings.Game.Width * Surface.GetBytesPerPixel(_vm.GraphicsManager.PixelFormat), 0, 0, _vm.Settings.Game.Width, _vm.Settings.Game.Height);
                    _vm.GraphicsManager.UpdateScreen();

                }

                int count;
                var lastState = new ScummInputState();
                do
                {
                    var state = _vm.System.InputManager.GetState();
                    count = state.GetKeys().Count;
                    if (state.IsKeyDown(KeyCode.Escape) || (lastState.IsLeftButtonDown && !state.IsLeftButtonDown))
                        skipped = true;
                    _vm.System.InputManager.ResetKeys();
                    lastState = state;
                } while (!skipped && count != 0);

                ServiceLocator.Platform.Sleep(10);
            }

            // Need to jump back to paletted color
            if (SystemVars.Platform == Core.IO.Platform.PSX)
                _vm.GraphicsManager.PixelFormat = PixelFormat.Indexed8;
        }

        private void LoadSubtitles(int id)
        {
            if (SystemVars.ShowText != 0)
            {
                var filename = $"{SequenceList[id]}.txt";
                var path = ScummHelper.LocatePath(_directory, filename);
                if (path != null)
                {
                    using (var f = new StreamReader(ServiceLocator.FileStorage.OpenFileRead(path)))
                    {
                        string line;
                        while ((line = f.ReadLine()) != null)
                        {
                            var m = _regex.Match(line);
                            if (m.Success)
                            {
                                var start = ushort.Parse(m.Groups[1].Value);
                                var end = ushort.Parse(m.Groups[2].Value);
                                var color = ushort.Parse(m.Groups[3].Value);
                                var text = m.Groups[4].Value;
                                _movieTexts.Add(new MovieText(start, end, text, color));
                            }
                        }
                    }
                }
            }
        }

        private void UpdateColors()
        {
            if (_movieTexts.Count > 0)
            {
                // Look for the best color indexes to use to display the subtitles
                uint minWeight = 0xFFFFFFFF;
                uint weight;
                float c1Weight = 1e+30f;
                float c2Weight = 1e+30f;
                float c3Weight = 1e+30f;
                float c4Weight = 1e+30f;
                byte r, g, b;
                float h, s, v, hd, hsvWeight;

                var palette = _decoder.Palette;
                var p = 0;

                // Color comparaison for the subtitles colors is done in HSL
                // C1 color is used for George and is almost white (R = 248, G = 252, B = 248)
                const float h1 = 0.333333f, s1 = 0.02f, v1 = 0.99f;

                // C2 color is used for George as a narrator and is grey (R = 184, G = 188, B = 184)
                const float h2 = 0.333333f, s2 = 0.02f, v2 = 0.74f;

                // C3 color is used for Nicole and is rose (R = 200, G = 120, B = 184)
                const float h3 = 0.866667f, s3 = 0.4f, v3 = 0.78f;

                // C4 color is used for Maguire and is blue (R = 80, G = 152, B = 184)
                const float h4 = 0.55f, s4 = 0.57f, v4 = 0.72f;

                for (uint i = 0; i < 256; i++)
                {
                    r = palette[p++];
                    g = palette[p++];
                    b = palette[p++];

                    weight = (uint)(3 * r * r + 6 * g * g + 2 * b * b);

                    if (weight <= minWeight)
                    {
                        minWeight = weight;
                        _black = i;
                    }

                    ConvertColor(r, g, b, out h, out s, out v);

                    // C1 color
                    // It is almost achromatic (very low saturation) so the hue as litle impact on the color.
                    // Therefore use a low weight on hue and high weight on saturation.
                    hd = h - h1;
                    hd += hd < -0.5f ? 1.0f : hd > 0.5f ? -1.0f : 0.0f;
                    hsvWeight = 1.0f * hd * hd + 4.0f * (s - s1) * (s - s1) + 3.0f * (v - v1) * (v - v1);
                    if (hsvWeight <= c1Weight)
                    {
                        c1Weight = hsvWeight;
                        _c1Color = i;
                    }

                    // C2 color
                    // Also an almost achromatic color so use the same weights as for C1 color.
                    hd = h - h2;
                    hd += hd < -0.5f ? 1.0f : hd > 0.5f ? -1.0f : 0.0f;
                    hsvWeight = 1.0f * hd * hd + 4.0f * (s - s2) * (s - s2) + 3.0f * (v - v2) * (v - v2);
                    if (hsvWeight <= c2Weight)
                    {
                        c2Weight = hsvWeight;
                        _c2Color = i;
                    }

                    // C3 color
                    // A light rose. Use a high weight on the hue to get a rose.
                    // The color is a bit gray and the saturation has not much impact so use a low weight.
                    hd = h - h3;
                    hd += hd < -0.5f ? 1.0f : hd > 0.5f ? -1.0f : 0.0f;
                    hsvWeight = 4.0f * hd * hd + 1.0f * (s - s3) * (s - s3) + 2.0f * (v - v3) * (v - v3);
                    if (hsvWeight <= c3Weight)
                    {
                        c3Weight = hsvWeight;
                        _c3Color = i;
                    }

                    // C4 color
                    // Blue. Use a hight weight on the hue to get a blue.
                    // The color is darker and more saturated than C3 and the saturation has more impact.
                    hd = h - h4;
                    hd += hd < -0.5f ? 1.0f : hd > 0.5f ? -1.0f : 0.0f;
                    hsvWeight = 5.0f * hd * hd + 3.0f * (s - s4) * (s - s4) + 2.0f * (v - v4) * (v - v4);
                    if (hsvWeight <= c4Weight)
                    {
                        c4Weight = hsvWeight;
                        _c4Color = i;
                    }
                }
            }
        }

        private void PerformPostProcessing(byte[] screen)
        {
            // TODO: We don't support displaying these in true color yet,
            // nor using the PSX fonts to display subtitles.
            if (SystemVars.Platform == Core.IO.Platform.PSX /* || _decoderType == kVideoDecoderMP2*/)
                return;

            if (_movieTexts.Count > 0)
            {
                if (_decoder.CurrentFrame == _movieTexts[0]._startFrame)
                {
                    var text = new ByteAccess(System.Text.Encoding.UTF8.GetBytes(_movieTexts[0]._text));
                    _textMan.MakeTextSprite(2, text, 600, Text.LetterCol);

                    var frame = new FrameHeader(_textMan.GiveSpriteData(2));
                    _textWidth = _resMan.ReadUInt16(frame.width);
                    _textHeight = _resMan.ReadUInt16(frame.height);
                    _textX = 320 - _textWidth / 2;
                    _textY = 420 - _textHeight;
                    _textColor = _movieTexts[0]._color;
                }
                if (_decoder.CurrentFrame == _movieTexts[0]._endFrame)
                {
                    _textMan.ReleaseText(2, false);
                    _movieTexts.RemoveAt(0);
                }
            }

            ByteAccess src, dst;
            int x, y;

            if (_textMan.GiveSpriteData(2) != null)
            {
                src = new ByteAccess(_textMan.GiveSpriteData(2));
                src.Offset += FrameHeader.Size;
                dst = new ByteAccess(screen, _textY * Screen.SCREEN_WIDTH + _textX * 1);

                for (y = 0; y < _textHeight; y++)
                {
                    for (x = 0; x < _textWidth; x++)
                    {
                        switch (src[x])
                        {
                            case Text.BorderCol:
                                dst[x] = (byte)GetBlackColor();
                                break;
                            case Text.LetterCol:
                                dst[x] = (byte)FindTextColor();
                                break;
                        }
                    }
                    src.Offset += _textWidth;
                    dst.Offset += Screen.SCREEN_WIDTH;
                }
            }
            else if (_textX != 0 && _textY != 0)
            {
                // If the frame doesn't cover the entire screen, we have to
                // erase the subtitles manually.

                int frameWidth = _decoder.GetWidth();
                int frameHeight = _decoder.GetHeight();
                int frameX = (_vm.Settings.Game.Width - frameWidth) / 2;
                int frameY = (_vm.Settings.Game.Height - frameHeight) / 2;

                dst = new ByteAccess(screen, _textY * _vm.Settings.Game.Width);

                for (y = 0; y < _textHeight; y++)
                {
                    if (_textY + y < frameY || _textY + y >= frameY + frameHeight)
                    {
                        dst.Data.Set(dst.Offset + _textX, (byte)GetBlackColor(), _textWidth);
                    }
                    else {
                        if (frameX > _textX)
                        {
                            dst.Data.Set(dst.Offset + _textX, (byte)GetBlackColor(), frameX - _textX);
                        }
                        if (frameX + frameWidth < _textX + _textWidth)
                        {
                            dst.Data.Set(dst.Offset + frameX + frameWidth, (byte)GetBlackColor(), _textX + _textWidth - (frameX + frameWidth));
                        }
                    }

                    dst.Offset += _vm.Settings.Game.Width;
                }

                _textX = 0;
                _textY = 0;
            }
        }

        private uint GetBlackColor()
        {
            return SystemVars.Platform == Core.IO.Platform.PSX ? ColorHelper.RGBToColor(0x00, 0x00, 0x00) : _black;
        }

        private uint FindTextColor()
        {
            if (SystemVars.Platform == Core.IO.Platform.PSX /*|| _decoderType == kVideoDecoderMP2*/)
            {
                // We're in true color mode, so return the actual colors
                switch (_textColor)
                {
                    case 1:
                        return ColorHelper.RGBToColor(248, 252, 248);
                    case 2:
                        return ColorHelper.RGBToColor(184, 188, 184);
                    case 3:
                        return ColorHelper.RGBToColor(200, 120, 184);
                    case 4:
                        return ColorHelper.RGBToColor(80, 152, 184);
                }

                return ColorHelper.RGBToColor(0xFF, 0xFF, 0xFF);
            }

            switch (_textColor)
            {
                case 1:
                    return _c1Color;
                case 2:
                    return _c2Color;
                case 3:
                    return _c3Color;
                case 4:
                    return _c4Color;
            }
            return _c1Color;
        }

        private void DrawFramePSX(Surface frame)
        {
            // The PSX videos have half resolution
            var scaledFrame = new Surface(frame.Width, (ushort) (frame.Height * 2), frame.PixelFormat, false);
            for (int y = 0; y < scaledFrame.Height; y++)
            {
                Array.Copy(frame.Pixels.Data, frame.Pixels.Offset + y / 2 * frame.Pitch, scaledFrame.Pixels.Data, scaledFrame.Pixels.Offset+y * frame.Pitch, scaledFrame.Width * scaledFrame.BytesPerPixel);
            }

            {
                ushort x = (ushort)((_vm.Settings.Game.Width - scaledFrame.Width) / 2);
                ushort y = (ushort)((_vm.Settings.Game.Height - scaledFrame.Height) / 2);

                _vm.GraphicsManager.CopyRectToScreen(scaledFrame.Pixels, scaledFrame.Pitch, 0, 0, x, y, scaledFrame.Width, scaledFrame.Height);
            }
        }

        private static void ConvertColor(byte r, byte g, byte b, out float h, out float s, out float v)
        {
            float varR = r / 255.0f;
            float varG = g / 255.0f;
            float varB = b / 255.0f;

            float min = Math.Min(varR, Math.Min(varG, varB));
            float max = Math.Max(varR, Math.Max(varG, varB));

            v = max;
            float d = max - min;
            s = max == 0.0f ? 0.0f : d / max;

            if (min == max)
            {
                h = 0.0f; // achromatic
            }
            else {
                if (max == varR)
                    h = (varG - varB) / d + (varG < varB ? 6.0f : 0.0f);
                else if (max == varG)
                    h = (varB - varR) / d + 2.0f;
                else
                    h = (varR - varG) / d + 4.0f;
                h /= 6.0f;
            }
        }

        private static Color[] ToPalette(byte[] palette)
        {
            var colors = new Color[256];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.FromRgb(palette[i * 3], palette[i * 3 + 1], palette[i * 3 + 2]);
            }
            return colors;
        }

        private static readonly string[] SequenceList = {
            "ferrari",  // 0  CD2   ferrari running down fitz in sc19
            "ladder",   // 1  CD2   george walking down ladder to dig sc24.sc$
            "steps",    // 2  CD2   george walking down steps sc23.sc24
            "sewer",    // 3  CD1   george entering sewer sc2.sc6
            "intro",    // 4  CD1   intro sequence .sc1
            "river",    // 5  CD1   george being thrown into river by flap & g$
            "truck",    // 6  CD2   truck arriving at bull's head sc45.sc53/4
            "grave",    // 7  BOTH  george's grave in scotland, from sc73 + from sc38 $
            "montfcon", // 8  CD2   monfaucon clue in ireland dig, sc25
            "tapestry", // 9  CD2   tapestry room beyond spain well, sc61
            "ireland",  // 10 CD2   ireland establishing shot europe_map.sc19
            "finale",   // 11 CD2   grand finale at very end, from sc73
            "history",  // 12 CD1   George's history lesson from Nico, in sc10
            "spanish",  // 13 CD2   establishing shot for 1st visit to Spain, europe_m$
            "well",     // 14 CD2   first time being lowered down well in Spai$
            "candle",   // 15 CD2   Candle burning down in Spain mausoleum sc59
            "geodrop",  // 16 CD2   from sc54, George jumping down onto truck
            "vulture",  // 17 CD2   from sc54, vultures circling George's dead body
            "enddemo",  // 18 ---   for end of single CD demo
            "credits",  // 19 CD2   credits, to follow "finale" sequence
        };

        // This is the list of the names of the PlayStation videos
        // TODO: fight.str, flashy.str,
        private static readonly string[] SequenceListPsx = {
            "e_ferr1",
            "ladder1",
            "steps1",
            "sewer1",
            "e_intro1",
            "river1",
            "truck1",
            "grave1",
            "montfcn1",
            "tapesty1",
            "ireland1",
            "e_fin1",
            "e_hist1",
            "spanish1",
            "well1",
            "candle1",
            "geodrop1",
            "vulture1",
            "", // demo video not present
	        ""  // credits are not a video
        };        
    }
}