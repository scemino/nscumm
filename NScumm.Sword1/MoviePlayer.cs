using System;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.Video;
using System.Collections.Generic;

namespace NScumm.Sword1
{
    class MovieText
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

        public MoviePlayer(SwordEngine vm)
        {
            _vm = vm;
        }

        public void Load(int id)
        {
            string filename, path;

            var directory = ServiceLocator.FileStorage.GetDirectoryName(_vm.Settings.Game.Path);

            //if (SystemVars.ShowText != 0)
            //{
            //    filename = $"{SequenceList[id]}.txt";
            //    path = ScummHelper.LocatePath(directory, filename);
            //    if (path != null)
            //    {
            //        var f = new StreamReader(ServiceLocator.FileStorage.OpenFileRead(path));
            //        string line;
            //        int lineNo = 0;
            //        int lastEnd = -1;

            //        _movieTexts.Clear();
            //        while (f.BaseStream.Position < f.BaseStream.Length)
            //        {
            //            line = f.ReadLine();
            //            lineNo++;
            //            if (line == null || line[0] == '#')
            //            {
            //                continue;
            //            }

            //            const char* ptr = line.c_str();

            //            // TODO: Better error handling
            //            int startFrame = strtoul(ptr, const_cast<char**>(&ptr), 10);
            //            int endFrame = strtoul(ptr, const_cast<char**>(&ptr), 10);

            //            while (*ptr && Common::isSpace(*ptr))
            //                ptr++;

            //            if (startFrame > endFrame)
            //            {
            //                warning("%s:%d: startFrame (%d) > endFrame (%d)", filename.c_str(), lineNo, startFrame, endFrame);
            //                continue;
            //            }

            //            if (startFrame <= lastEnd)
            //            {
            //                warning("%s:%d startFrame (%d) <= lastEnd (%d)", filename.c_str(), lineNo, startFrame, lastEnd);
            //                continue;
            //            }

            //            int color = 0;
            //            if (*ptr == '@')
            //            {
            //                ++ptr;
            //                color = strtoul(ptr, const_cast<char**>(&ptr), 10);
            //                while (*ptr && Common::isSpace(*ptr))
            //                    ptr++;
            //            }

            //            _movieTexts.push_back(MovieText(startFrame, endFrame, ptr, color));
            //            lastEnd = endFrame;
            //        }
            //    }
            //}

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
                _decoder = new SmackerDecoder(_vm.Mixer);
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

            while (!SwordEngine.ShouldQuit && !_decoder.EndOfVideo && !skipped)
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
                    }

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


        // TODO:
        //void PerformPostProcessing(byte[] screen)
        //{
        //    // TODO: We don't support displaying these in true color yet,
        //    // nor using the PSX fonts to display subtitles.
        //    if (SystemVars.Platform == Core.IO.Platform.PSX /* || _decoderType == kVideoDecoderMP2*/)
        //        return;

        //    if (!_movieTexts.empty())
        //    {
        //        if (_decoder.getCurFrame() == _movieTexts.front()._startFrame)
        //        {
        //            _textMan.makeTextSprite(2, (const uint8*)_movieTexts.front()._text.c_str(), 600, LETTER_COL);

        //            FrameHeader* frame = _textMan.giveSpriteData(2);
        //            _textWidth = _resMan.toUint16(frame.width);
        //            _textHeight = _resMan.toUint16(frame.height);
        //            _textX = 320 - _textWidth / 2;
        //            _textY = 420 - _textHeight;
        //            _textColor = _movieTexts.front()._color;
        //        }
        //        if (_decoder.getCurFrame() == _movieTexts.front()._endFrame)
        //        {
        //            _textMan.releaseText(2, false);
        //            _movieTexts.pop_front();
        //        }
        //    }

        //    byte* src, *dst;
        //    int x, y;

        //    if (_textMan.giveSpriteData(2))
        //    {
        //        src = (byte*)_textMan.giveSpriteData(2) + sizeof(FrameHeader);
        //        dst = screen + _textY * SCREEN_WIDTH + _textX * 1;

        //        for (y = 0; y < _textHeight; y++)
        //        {
        //            for (x = 0; x < _textWidth; x++)
        //            {
        //                switch (src[x])
        //                {
        //                    case BORDER_COL:
        //                        dst[x] = getBlackColor();
        //                        break;
        //                    case LETTER_COL:
        //                        dst[x] = findTextColor();
        //                        break;
        //                }
        //            }
        //            src += _textWidth;
        //            dst += SCREEN_WIDTH;
        //        }
        //    }
        //    else if (_textX && _textY)
        //    {
        //        // If the frame doesn't cover the entire screen, we have to
        //        // erase the subtitles manually.

        //        int frameWidth = _decoder.getWidth();
        //        int frameHeight = _decoder.getHeight();
        //        int frameX = (_system.getWidth() - frameWidth) / 2;
        //        int frameY = (_system.getHeight() - frameHeight) / 2;

        //        dst = screen + _textY * _system.getWidth();

        //        for (y = 0; y < _textHeight; y++)
        //        {
        //            if (_textY + y < frameY || _textY + y >= frameY + frameHeight)
        //            {
        //                memset(dst + _textX, getBlackColor(), _textWidth);
        //            }
        //            else {
        //                if (frameX > _textX)
        //                    memset(dst + _textX, getBlackColor(), frameX - _textX);
        //                if (frameX + frameWidth < _textX + _textWidth)
        //                    memset(dst + frameX + frameWidth, getBlackColor(), _textX + _textWidth - (frameX + frameWidth));
        //            }

        //            dst += _system.getWidth();
        //        }

        //        _textX = 0;
        //        _textY = 0;
        //    }
        //}

        private void DrawFramePSX(Surface frame)
        {
            // The PSX videos have half resolution
            var scaledFrame = new Surface(frame.Width, frame.Height * 2, frame.PixelFormat, false);
            for (int y = 0; y < scaledFrame.Height; y++)
            {
                Array.Copy(frame.Pixels, (y / 2) * frame.Pitch, scaledFrame.Pixels, y * frame.Pitch, scaledFrame.Width * scaledFrame.BytesPerPixel);
            }

            {
                ushort x = (ushort)((_vm.Settings.Game.Width - scaledFrame.Width) / 2);
                ushort y = (ushort)((_vm.Settings.Game.Height - scaledFrame.Height) / 2);

                _vm.GraphicsManager.CopyRectToScreen(scaledFrame.Pixels, scaledFrame.Pitch, 0, 0, x, y, scaledFrame.Width, scaledFrame.Height);
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