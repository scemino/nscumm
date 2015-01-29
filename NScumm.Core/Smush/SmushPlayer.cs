//
//  SmushPlayer.cs
//
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
using System.IO;
using NScumm.Core.Audio;
using System.Threading;
using NScumm.Core.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NScumm.Core.Graphics;

namespace NScumm.Core.Smush
{
    class SmushPlayer
    {
        public int Width
        {
            get{ return _width; }
        }

        public int Height
        {
            get{ return _height; }
        }

        public SmushPlayer(ScummEngine7 scumm)
        {
            _vm = scumm;
            _smixer = _vm.SmushMixer;
        }

        public void Play(string filename, int speed, int offset = 0, int startFrame = 0)
        {
            filename = ScummHelper.LocatePath(Path.GetDirectoryName(_vm.Game.Path), filename);
            // Verify the specified file exists

            _updateNeeded = false;
            _warpNeeded = false;
            _palDirtyMin = 256;
            _palDirtyMax = -1;

            bool oldMouseState = _vm._gfxManager.ShowCursor(false);

            // Load the video
            _seekFile = filename;
            _seekPos = offset;
            _seekFrame = startFrame;
            _base = null;

            SetupAnim(filename);
            Init(speed);

            _startTime = Environment.TickCount;
            _startFrame = startFrame;
            _frame = startFrame;

            _pauseTime = 0;

            int skipped = 0;

            for (;;)
            {
                int now, elapsed;
                bool skipFrame = false;

                if (_insanity)
                {
                    // Seeking makes a mess of trying to sync the audio to
                    // the sound. Synt to time instead.
                    now = Environment.TickCount - _pauseTime;
                    elapsed = now - _startTime;
                }
                else if (_vm.Mixer.IsSoundHandleActive(_compressedFileSoundHandle))
                {
                    // Compressed SMUSH files.
                    elapsed = _vm.Mixer.GetSoundElapsedTime(_compressedFileSoundHandle);
                }
                else if (_vm.Mixer.IsSoundHandleActive(_IACTchannel))
                {
                    // Curse of Monkey Island SMUSH files.
                    elapsed = _vm.Mixer.GetSoundElapsedTime(_IACTchannel);
                }
                else
                {
                    // For other SMUSH files, we don't necessarily have any
                    // one channel to sync against, so we have to use
                    // elapsed real time.
                    now = Environment.TickCount - _pauseTime;
                    elapsed = now - _startTime;
                }

                if (elapsed >= ((_frame - _startFrame) * 1000) / _speed)
                {
                    if (elapsed >= ((_frame + 1) * 1000) / _speed)
                        skipFrame = true;
                    else
                        skipFrame = false;
                    TimerCallback();
                }

                _vm.HandleSound();

                if (_warpNeeded)
                {
                    // TODO: vs
//                    _vm._gfxManager.WarpMouse(_warpX, _warpY);
                    _warpNeeded = false;
                }

                _vm._inputManager.UpdateStates();
                _vm.ProcessInput();
                if (_palDirtyMax >= _palDirtyMin)
                {
                    _vm._gfxManager.SetPalette(_pal.Colors, _palDirtyMin, _palDirtyMax - _palDirtyMin + 1);

                    _palDirtyMax = -1;
                    _palDirtyMin = 256;
                    skipFrame = false;
                }
                if (skipFrame)
                {
                    if (++skipped > 10)
                    {
                        skipFrame = false;
                        skipped = 0;
                    }
                }
                else
                    skipped = 0;
                if (_updateNeeded)
                {
                    if (!skipFrame)
                    {
                        // Workaround for bug #1386333: "FT DEMO: assertion triggered
                        // when playing movie". Some frames there are 384 x 224
                        int w = Math.Min(_width, _vm.ScreenWidth);
                        int h = Math.Min(_height, _vm.ScreenHeight);

                        _vm._gfxManager.CopyRectToScreen(_dst, _width, 0, 0, w, h);
                        _vm._gfxManager.UpdateScreen();
                        _updateNeeded = false;
                    }
                }
                if (_endOfFile)
                    break;
                if (_vm.HasToQuit || _vm._saveLoadFlag != 0 || _vm.SmushVideoShouldFinish)
                {
                    _smixer.Stop();
                    _vm.Mixer.StopHandle(_compressedFileSoundHandle);
                    _vm.Mixer.StopHandle(_IACTchannel);
                    _IACTpos = 0;
                    break;
                }
                Thread.Sleep(10);
            }

            Release();

            // Reset mouse state
            _vm._gfxManager.ShowCursor(oldMouseState);
        }

        public void SeekSan(string filename, int pos, int contFrame)
        {
            _seekFile = ScummHelper.LocatePath(Path.GetDirectoryName(_vm.Game.Path), Path.GetFileName(filename));
            _seekPos = pos;
            _seekFrame = contFrame;
            _pauseTime = 0;
        }

        public void Insanity(bool flag)
        {
            _insanity = flag;
        }

        public void SetPalette(byte[] palette)
        {
            for (int i = 0; i < 256; i++)
            {
                _pal.Colors[i] = Color.FromRgb(palette[i * 3], palette[i * 3 + 1], palette[i * 3 + 2]);
            }
            SetDirtyColors(0, 255);
        }

        public void SetPaletteValue(int n, byte r, byte g, byte b)
        {
            _pal.Colors[n] = Color.FromRgb(r, g, b);
            SetDirtyColors(n, n);
        }

        public SmushFont GetFont(int font)
        {
            if (_sf[font] != null)
                return _sf[font];

            if (_vm.Game.GameId == GameId.FullThrottle)
            {
                if (!(_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm._game.platform == Common::kPlatformDOS)*/))
                {
                    var ft_fonts = new []
                    {
                        "scummfnt.nut",
                        "techfnt.nut",
                        "titlfnt.nut",
                        "specfnt.nut"
                    };

                    Debug.Assert(font >= 0 && font < ft_fonts.Length);

                    _sf[font] = new SmushFont(_vm, ft_fonts[font], true, false);
                }
            }
            else if (_vm.Game.GameId == GameId.Dig)
            {
                if (!(_vm.Game.Features.HasFlag(GameFeatures.Demo)))
                {
                    Debug.Assert(font >= 0 && font < 4);

                    var file_font = string.Format("font{0}.nut", font);
                    _sf[font] = new SmushFont(_vm, file_font, font != 0, false);
                }
            }
            //            else if (_vm.Game.GameId == GameId.Comi)
            //            {
            //                int numFonts = (_vm._game.features & GF_DEMO) ? 4 : 5;
            //                assert(font >= 0 && font < numFonts);
            //
            //                sprintf(file_font, "font%d.nut", font);
            //                _sf[font] = new SmushFont(_vm, file_font, false, true);
            //            }
            else
            {
                throw new NotSupportedException("SmushPlayer::getFont() Unknown font setup for game");
            }

            Debug.Assert(_sf[font] != null);
            return _sf[font];
        }

        public string GetString(int id)
        {
            return _strings[id];
        }

        void Release()
        {
            _vm.SmushVideoShouldFinish = true;

            _vm.SmushActive = false;
            _vm._fullRedraw = true;

            // HACK HACK HACK: This is an *evil* trick, beware! See above for
            // some explanation.
            _vm.MainVirtScreen.Pitch = _origPitch;
            _vm.Gdi.NumStrips = _origNumStrips;
        }

        void TimerCallback()
        {
            ParseNextFrame();
        }

        void ParseNextFrame()
        {
            string subType;
            uint subSize = 0;
            long subOffset = 0;

            if (_seekPos >= 0)
            {
                if (_smixer != null)
                    _smixer.Stop();

                if (!string.IsNullOrEmpty(_seekFile))
                {
                    _base = new XorReader(File.OpenRead(_seekFile), 0);
                    _base.ReadUInt32BigEndian();
                    _baseSize = _base.ReadUInt32BigEndian();

                    if (_seekPos > 0)
                    {
                        Debug.Assert(_seekPos > 8);
                        // In this case we need to get palette and number of frames
                        subType = System.Text.Encoding.ASCII.GetString(_base.ReadBytes(4));
                        subSize = _base.ReadUInt32BigEndian();
                        subOffset = _base.BaseStream.Position;
                        Debug.Assert(subType == "AHDR");
                        HandleAnimHeader(subSize, _base);
                        _base.BaseStream.Seek(subOffset + subSize, SeekOrigin.Begin);

                        _middleAudio = true;
                        _seekPos -= 8;
                    }
                    else
                    {
                        // We need this in Full Throttle when entering/leaving
                        // the old mine road.
                        TryCmpFile(_seekFile);
                    }
                    _skipPalette = false;
                }
                else
                {
                    _skipPalette = true;
                }

                _base.BaseStream.Seek(_seekPos + 8, SeekOrigin.Begin);
                _frame = _seekFrame;
                _startFrame = _frame;
                _startTime = Environment.TickCount;

                _seekPos = -1;
            }

            Debug.Assert(_base != null);

            if (_base.BaseStream.Position >= _baseSize)
            {
                _vm.SmushVideoShouldFinish = true;
                _endOfFile = true;
                return;
            }

            subType = System.Text.Encoding.ASCII.GetString(_base.ReadBytes(4));
            subSize = _base.ReadUInt32BigEndian();
            subOffset = _base.BaseStream.Position;

//            Debug.WriteLine("Chunk: {0} at {1}", subType, subOffset);

            switch (subType)
            {
                case "AHDR": // FT INSANE may seek file to the beginning
                    HandleAnimHeader(subSize, _base);
                    break;
                case "FRME":
                    HandleFrame(subSize, _base);
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Unknown Chunk found at {0:X}: {1}, {2}", subOffset, subType, subSize));
            }

            _base.BaseStream.Seek(subOffset + subSize, SeekOrigin.Begin);

            if (_insanity)
                _vm.Sound.ProcessSound();

            // TODO: vs
//            _vm._imuseDigital.flushTracks();
        }

        void HandleFrame(uint frameSize, XorReader b)
        {
//            Debug.WriteLine("SmushPlayer.HandleFrame({0})", _frame);
            _skipNext = false;

            if (_insanity)
            {
                _vm.Insane.ProcPreRendering();
            }

            while (frameSize > 0)
            {
                var subType = System.Text.Encoding.ASCII.GetString(_base.ReadBytes(4));
                var subSize = b.ReadUInt32BigEndian();
                var subOffset = b.BaseStream.Position;
                switch (subType)
                {
                    case "NPAL":
                        HandleNewPalette(subSize, b);
                        break;
                    case "FOBJ":
                        HandleFrameObject(subSize, b);
                        break;
#if USE_ZLIB
                    case "ZFOB":
                        HandleZlibFrameObject(subSize, b);
                        break;
#endif
                    case "PSAD":
                        if (!_compressedFileMode)
                            HandleSoundFrame(subSize, b);
                        break;
                    case "TRES":
                        HandleTextResource(subType, subSize, b);
                        break;
                    case "XPAL":
                        HandleDeltaPalette(subSize, b);
                        break;
                    case "IACT":
                        HandleIACT(subSize, b);
                        break;
                    case "STOR":
                        HandleStore(subSize, b);
                        break;
                    case "FTCH":
                        HandleFetch(subSize, b);
                        break;
                    case "SKIP":
                        _vm.Insane.ProcSKIP((int)subSize, b);
                        break;
                    case "TEXT":
                        HandleTextResource(subType, subSize, b);
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Unknown frame subChunk found : {0}, {1}", subType, subSize));
                }

                frameSize -= subSize + 8;
                b.BaseStream.Seek(subOffset + subSize, SeekOrigin.Begin);
                if ((subSize & 1) != 0)
                {
                    b.BaseStream.Seek(1, SeekOrigin.Current);
                    frameSize--;
                }
            }

            if (_insanity)
            {
                _vm.Insane.ProcPostRendering(_dst, 0, 0, 0, _frame, _nbframes - 1);
            }

            if (_width != 0 && _height != 0)
            {
                UpdateScreen();
            }
            _smixer.HandleFrame();

            _frame++;
        }

        void HandleStore(uint subSize, XorReader b)
        {
            Debug.WriteLine("SmushPlayer::HandleStore()");
            Debug.Assert(subSize >= 4);
            _storeFrame = true;
        }

        void HandleFetch(uint subSize, XorReader b)
        {
            Debug.WriteLine("SmushPlayer::HandleFetch()");
            Debug.Assert(subSize >= 6);

            if (_frameBuffer != null)
            {
                Array.Copy(_frameBuffer, _dst, _width * _height);
            }
        }

        void HandleDeltaPalette(uint subSize, XorReader b)
        {
            Debug.WriteLine("SmushPlayer.HandleDeltaPalette()");

            if (subSize == 0x300 * 3 + 4)
            {

                b.ReadUInt16();
                b.ReadUInt16();

                for (int i = 0; i < 0x300; i++)
                {
                    _deltaPal[i] = b.ReadInt16();
                }
                _pal = ReadPalette(b);
                SetDirtyColors(0, 255);
            }
            else if (subSize == 6)
            {

                b.ReadUInt16();
                b.ReadUInt16();
                b.ReadUInt16();

                for (int i = 0; i < 0x100; i++)
                {
                    _pal.Colors[i] = Color.FromRgb(
                        DeltaColor(_pal.Colors[i].R, _deltaPal[i * 3]),
                        DeltaColor(_pal.Colors[i].G, _deltaPal[i * 3 + 1]),
                        DeltaColor(_pal.Colors[i].B, _deltaPal[i * 3 + 2]));
                }
                SetDirtyColors(0, 255);
            }
            else
            {
                throw new InvalidOperationException("SmushPlayer.HandleDeltaPalette() Wrong size for DeltaPalette");
            }
        }

        void HandleIACT(uint subSize, XorReader b)
        {
            Debug.WriteLine("SmushPlayer::IACT()");
            Debug.Assert(subSize >= 8);

            int code = b.ReadUInt16();
            int flags = b.ReadUInt16();
            int unknown = b.ReadInt16();
            int track_flags = b.ReadUInt16();

            if ((code != 8) && (flags != 46))
            {
                _vm.Insane.ProcIACT(_dst, 0, 0, 0, b, 0, 0, code, flags, unknown, track_flags);
                return;
            }

            if (_compressedFileMode)
            {
                return;
            }

            Debug.Assert(flags == 46 && unknown == 0);
            int track_id = b.ReadUInt16();
            int index = b.ReadUInt16();
            int nbframes = b.ReadUInt16();
            int size = b.ReadInt32();
            int bsize = (int)subSize - 18;

            if (_vm.Game.GameId != GameId.CurseOfMonkeyIsland)
            {
                int track = track_id;
                if (track_flags == 1)
                {
                    track = track_id + 100;
                }
                else if (track_flags == 2)
                {
                    track = track_id + 200;
                }
                else if (track_flags == 3)
                {
                    track = track_id + 300;
                }
                else if ((track_flags >= 100) && (track_flags <= 163))
                {
                    track = track_id + 400;
                }
                else if ((track_flags >= 200) && (track_flags <= 263))
                {
                    track = track_id + 500;
                }
                else if ((track_flags >= 300) && (track_flags <= 363))
                {
                    track = track_id + 600;
                }
                else
                {
                    Debug.Fail(string.Format("SmushPlayer::handleIACT(): bad track_flags: {0}", track_flags));
                }
                Debug.WriteLine("SmushPlayer::handleIACT(): {0}, {1}, {2}", track, index, track_flags);

                var c = _smixer.FindChannel(track);
                if (c == null)
                {
//                    c = new ImuseChannel(track);
//                    _smixer.AddChannel(c);
                    // TODO: vs and remove return
                    return;
                }
                if (index == 0)
                    c.SetParameters(nbframes, size, track_flags, unknown, 0);
                else
                    c.CheckParameters(index, nbframes, size, track_flags, unknown);
                c.AppendData(b, bsize);
            }
            else
            {
                // TODO: Move this code into another SmushChannel subclass?
                var src = b.ReadBytes(bsize);
                int d_src = 0;
                byte value;

                while (bsize > 0)
                {
                    if (_IACTpos >= 2)
                    {
                        int len = ScummHelper.SwapBytes(BitConverter.ToUInt16(_IACToutput, 0)) + 2;
                        len -= _IACTpos;
                        if (len > bsize)
                        {
                            Array.Copy(src, d_src, _IACToutput, _IACTpos, bsize);
                            _IACTpos += bsize;
                            bsize = 0;
                        }
                        else
                        {
                            var output_data = new byte[4096];

                            Array.Copy(src, d_src, _IACToutput, _IACTpos, len);
                            var dst = 0;
                            var d_src2 = 0;
                            d_src2 += 2;
                            int count = 1024;
                            byte variable1 = _IACToutput[d_src2++];
                            byte variable2 = (byte)(variable1 / 16);
                            variable1 &= 0x0f;
                            do
                            {
                                value = _IACToutput[d_src2++];
                                if (value == 0x80)
                                {
                                    output_data[dst++] = _IACToutput[d_src2++];
                                    output_data[dst++] = _IACToutput[d_src2++];
                                }
                                else
                                {
                                    short val = (sbyte)(value << variable2);
                                    output_data[dst++] = (byte)(val >> 8);
                                    output_data[dst++] = (byte)(val);
                                }
                                value = _IACToutput[d_src2++];
                                if (value == 0x80)
                                {
                                    output_data[dst++] = _IACToutput[d_src2++];
                                    output_data[dst++] = _IACToutput[d_src2++];
                                }
                                else
                                {
                                    short val = (sbyte)(value << variable1);
                                    output_data[dst++] = (byte)(val >> 8);
                                    output_data[dst++] = (byte)(val);
                                }
                            } while ((--count)!=0);

                            if (_IACTstream == null)
                            {
                                _IACTstream = new QueuingAudioStream(22050, true);
                                _IACTchannel = _vm.Mixer.PlayStream(SoundType.SFX, _IACTstream);
                            }
                            _IACTstream.QueueBuffer(output_data, 0x1000, true, AudioFlags.Stereo | AudioFlags.Is16Bits);

                            bsize -= len;
                            d_src += len;
                            _IACTpos = 0;
                        }
                    }
                    else
                    {
                        if (bsize > 1 && _IACTpos == 0)
                        {
                            _IACToutput[0] = src[d_src++];
                            _IACTpos = 1;
                            bsize--;
                        }
                        _IACToutput[_IACTpos] = src[d_src++];
                        _IACTpos++;
                        bsize--;
                    }
                }
            }
        }

        static byte DeltaColor(int org_color, int delta_color)
        {
            int t = (org_color * 129 + delta_color) / 128;
            return (byte)ScummHelper.Clip(t, 0, 255);
        }

        void HandleTextResource(string subType, uint subSize, XorReader b)
        {
            int pos_x = b.ReadInt16();
            int pos_y = b.ReadInt16();
            int flags = b.ReadInt16();
            int left = b.ReadInt16();
            int top = b.ReadInt16();
            int right = b.ReadInt16();
            /*int height =*/
            b.ReadInt16();
            /*int unk2 =*/
            b.ReadUInt16();

            string str;
            if (subType == "TEXT")
            {
                str = System.Text.Encoding.ASCII.GetString(b.ReadBytes((int)(subSize - 16)));
            }
            else
            {
                int string_id = b.ReadUInt16();
                if (_strings == null)
                    return;
                str = _strings[string_id];
            }

            // if subtitles disabled and bit 3 is set, then do not draw
            //
            // Query ConfMan here. However it may be slower, but
            // player may want to switch the subtitles on or off during the
            // playback. This fixes bug #1550974
//            if ((!ConfMan.getBool("subtitles")) && ((flags & 8) == 8))
//                return;

            // TODO: vs
            var sf = GetFont(0);
            int color = 15;
//            while (*str == '/') {
//                str++; // For Full Throttle text resources
//            }
//
//            byte transBuf[512];
//            if (_vm._game.id == GID_CMI) {
//                _vm.translateText((const byte *)str - 1, transBuf);
//                while (*str++ != '/')
//                    ;
//                string2 = (char *)transBuf;
//
//                // If string2 contains formatting information there probably
//                // wasn't any translation for it in the language.tab file. In
//                // that case, pretend there is no string2.
//                if (string2[0] == '^')
//                    string2[0] = 0;
//            }
//
            while (str[0] == '^')
            {
                switch (str[1])
                {
                    case 'f':
                        {
                            int id = str[3] - '0';
                            str = str.Substring(4);
                            sf = GetFont(id);
                        }
                        break;
                    case 'c':
                        {
                            color = str[4] - '0' + 10 * (str[3] - '0');
                            str = str.Substring(5);
                        }
                        break;
                    default:
                        throw new InvalidOperationException("invalid escape code in text string");
                }
            }

            // HACK. This is to prevent bug #1310846. In updated Win95 dig
            // there is such line:
            //
            // ^f01^c001LEAD TESTER
            // Chris Purvis
            // ^f01
            // ^f01^c001WINDOWS COMPATIBILITY
            // Chip Hinnenberg
            // ^f01^c001WINDOWS TESTING
            // Jim Davison
            // Lynn Selk
            //
            // i.e. formatting exists not in the first line only
            // We just strip that off and assume that neither font
            // nor font color was altered. Proper fix would be to feed
            // drawString() with each line sequentally
//            char *string3 = NULL, *sptr2;
//            const char *sptr;
//
//            if (strchr(str, '^')) {
//                string3 = (char *)malloc(strlen(str) + 1);
//
//                for (sptr = str, sptr2 = string3; *sptr;) {
//                    if (*sptr == '^') {
//                        switch (sptr[1]) {
//                            case 'f':
//                                sptr += 4;
//                                break;
//                            case 'c':
//                                sptr += 5;
//                                break;
//                            default:
//                                error("invalid escape code in text string");
//                        }
//                    } else {
//                        *sptr2++ = *sptr++;
//                    }
//                }
//                *sptr2++ = *sptr++; // copy zero character
//                str = string3;
//            }
//
            Debug.Assert(sf != null);
            sf.Color = (byte)color;
//
//            if (_vm._game.id == GID_CMI && string2[0] != 0) {
//                str = string2;
//            }
//
            // flags:
            // bit 0 - center       1
            // bit 1 - not used     2
            // bit 2 - ???          4
            // bit 3 - wrap around  8
            switch (flags & 9)
            {
                case 0:
                    sf.DrawString(str, _dst, _width, _height, pos_x, pos_y, false);
                    break;
                case 1:
                    sf.DrawString(str, _dst, _width, _height, pos_x, Math.Max(pos_y, top), true);
                    break;
                case 8:
                    // FIXME: Is 'right' the maximum line width here, just
                    // as it is in the next case? It's used several times
                    // in The Dig's intro, where 'left' and 'right' are
                    // always 0 and 321 respectively, and apparently we
                    // handle that correctly.
                    sf.DrawStringWrap(str, _dst, _width, _height, pos_x, Math.Max(pos_y, top), left, right, false);
                    break;
                case 9:
                    // In this case, the 'right' parameter is actually the
                    // maximum line width. This explains why it's sometimes
                    // smaller than 'left'.
                    //
                    // Note that in The Dig's "Spacetime Six" movie it's
                    // 621. I have no idea what that means.
                    sf.DrawStringWrap(str, _dst, _width, _height, pos_x, Math.Max(pos_y, top), left, Math.Min(left + right, _width), true);
                    break;
                default:
                    throw new InvalidOperationException(string.Format("SmushPlayer::handleTextResource. Not handled flags: {0}", flags));
            }
        }

        void HandleSoundFrame(uint subSize, XorReader b)
        {
//            Debug.WriteLine("SmushPlayer.HandleSoundFrame()");

            int track_id = b.ReadUInt16();
            int index = b.ReadUInt16();
            int max_frames = b.ReadUInt16();
            int flags = b.ReadUInt16();
            int vol = b.ReadByte();
            int pan = (sbyte)b.ReadByte();
            if (index == 0)
            {
                Debug.WriteLine("track_id:{0}, max_frames:{1}, flags:{2}, vol:{3}, pan:{4}", track_id, max_frames, flags, vol, pan);
            }
            int size = (int)(subSize - 10);
            HandleSoundBuffer(track_id, index, max_frames, flags, vol, pan, b, size);
        }

        void HandleSoundBuffer(int track_id, int index, int max_frames, int flags, int vol, int pan, XorReader b, int size)
        {
            Debug.WriteLine("SmushPlayer::handleSoundBuffer({0}, {1})", track_id, index);
            //  if ((flags & 128) == 128) {
            //      return;
            //  }
            //  if ((flags & 64) == 64) {
            //      return;
            //  }
            var c = _smixer.FindChannel(track_id);
            if (c == null)
            {
                c = new SaudChannel(track_id);
                _smixer.AddChannel(c);
            }

            if (_middleAudio || index == 0)
            {
                c.SetParameters(max_frames, flags, vol, pan, index);
            }
            else
            {
                c.CheckParameters(index, max_frames, flags, vol, pan);
            }
            _middleAudio = false;
            c.AppendData(b, size);
        }

        void HandleNewPalette(uint subSize, XorReader b)
        {
            Debug.Write("SmushPlayer.HandleNewPalette()");
            Debug.Assert(subSize >= 0x300);

            if (_skipPalette)
                return;

            _pal = ReadPalette(b);
            SetDirtyColors(0, 255);
        }

        void HandleFrameObject(uint subSize, XorReader b)
        {
            Debug.Assert(subSize >= 14);
            if (_skipNext)
            {
                _skipNext = false;
                return;
            }

            int codec = b.ReadUInt16();
            int left = b.ReadUInt16();
            int top = b.ReadUInt16();
            int width = b.ReadUInt16();
            int height = b.ReadUInt16();

            b.ReadUInt16();
            b.ReadUInt16();

            int chunk_size = (int)(subSize - 14);
            var chunk_buffer = b.ReadBytes(chunk_size);
            DecodeFrameObject(codec, chunk_buffer, left, top, width, height);
        }

        void DecodeFrameObject(int codec, byte[] src, int left, int top, int width, int height)
        {
            if ((height == 242) && (width == 384))
            {
                if (_specialBuffer == null)
                    _specialBuffer = new byte[242 * 384];
                _dst = _specialBuffer;
            }
            else if ((height > _vm.ScreenHeight) || (width > _vm.ScreenWidth))
                return;
            // FT Insane uses smaller frames to draw overlays with moving objects
            // Other .san files do have them as well but their purpose in unknown
            // and often it causes memory overdraw. So just skip those frames
            else if (!_insanity && ((height != _vm.ScreenHeight) || (width != _vm.ScreenWidth)))
                return;

            if ((height == 242) && (width == 384))
            {
                _width = width;
                _height = height;
            }
            else
            {
                _width = _vm.ScreenWidth;
                _height = _vm.ScreenHeight;
            }

            switch (codec)
            {
                case 1:
                case 3:
//                    SmushDecodeCodec1(_dst, src, left, top, width, height, _vm.ScreenWidth);
                    break;
                case 37:
                    if (_codec37 == null)
                        _codec37 = new Codec37Decoder(width, height);
                    _codec37.Decode(_dst, src);
                    break;
//                case 47:
//                    if (!_codec47)
//                        _codec47 = new Codec47Decoder(width, height);
//                    if (_codec47)
//                        _codec47.decode(_dst, src);
//                    break;
                default:
                    throw new InvalidOperationException(string.Format("Invalid codec for frame object : {0}", codec));
            }

            if (_storeFrame)
            {
                if (_frameBuffer == null)
                {
                    _frameBuffer = new byte[_width * _height];
                }
                Array.Copy(_dst, _frameBuffer, width * height);
                _storeFrame = false;
            }
        }

        internal static void SmushDecodeCodec1(byte[] dst, int dstPos, byte[] src, int srcfOffset, int left, int top, int width, int height, int pitch)
        {
            byte val, code;
            int length;
            int size_line;

            dstPos += top * pitch;
            int srcPos = srcfOffset;
            for (var h = 0; h < height; h++)
            {
                size_line = BitConverter.ToUInt16(src, srcPos);
                srcPos += 2;
                dstPos += left;
                while (size_line > 0)
                {
                    code = src[srcPos++];
                    size_line--;
                    length = (code >> 1) + 1;
                    if ((code & 1) != 0)
                    {
                        val = src[srcPos++];
                        size_line--;
                        if (val != 0)
                        {
                            for (int i = 0; i < length; i++)
                            {
                                dst[dstPos + i] = val;
                            }
                        }
                        dstPos += length;
                    }
                    else
                    {
                        size_line -= length;
                        while ((length--) != 0)
                        {
                            val = src[srcPos++];
                            if (val != 0)
                                dst[dstPos] = val;
                            dstPos++;
                        }
                    }
                }
                dstPos += pitch - left - width;
            }
        }

        void UpdateScreen()
        {
            int end_time, start_time = Environment.TickCount;
            _updateNeeded = true;
            end_time = Environment.TickCount;
//            Debug.WriteLine("Smush stats: updateScreen( {0} )", end_time - start_time);
        }

        void HandleAnimHeader(uint subSize, XorReader b)
        {
            Debug.WriteLine("SmushPlayer::HandleAnimHeader()");
            Debug.Assert(subSize >= 0x300 + 6);

            /* _version = */
            b.ReadUInt16();
            _nbframes = b.ReadUInt16();
            b.ReadUInt16();

            if (_skipPalette)
                return;

            _pal = ReadPalette(b);
            SetDirtyColors(0, 255);
        }

        Palette ReadPalette(XorReader b)
        {
            var colors = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                colors[i] = Color.FromRgb(b.ReadByte(), b.ReadByte(), b.ReadByte());
            }
            return new Palette(colors);
        }

        void SetDirtyColors(int min, int max)
        {
            if (_palDirtyMin > min)
                _palDirtyMin = min;
            if (_palDirtyMax < max)
                _palDirtyMax = max;
        }

        void TryCmpFile(string filename)
        {
            _vm.Mixer.StopHandle(_compressedFileSoundHandle);

            _compressedFileMode = false;
        }

        void SetupAnim(string file)
        {
            if (_insanity)
            {
                // TODO: vs
                if (!(_vm.Game.Features.HasFlag(GameFeatures.Demo) /*&& (_vm._game.platform == Common::kPlatformDOS)*/))
                    ReadString("mineroad.trs");
            }
            else
                ReadString(file);
        }

        void Init(int speed)
        {
            var vs = _vm.MainVirtScreen;

            _frame = 0;
            _speed = speed;
            _endOfFile = false;

            _vm.SmushVideoShouldFinish = false;
            _vm.SmushActive = true;

            _vm.SetDirtyColors(0, 255);
            _dst = vs.Surfaces[0].Pixels;


            // HACK HACK HACK: This is an *evil* trick, beware!
            // We do this to fix bug #1037052. A proper solution would change all the
            // drawing code to use the pitch value specified by the virtual screen.
            // However, since a lot of the SMUSH code currently assumes the screen
            // width and pitch to be equal, this will require lots of changes. So
            // we resort to this hackish solution for now.
            _origPitch = vs.Pitch;
            _origNumStrips = _vm.Gdi.NumStrips;
            vs.Pitch = vs.Width;
            _vm.Gdi.NumStrips = vs.Width / 8;

            _vm.Mixer.StopHandle(_compressedFileSoundHandle);
            _vm.Mixer.StopHandle(_IACTchannel);
            _IACTpos = 0;
            _vm.SmushMixer.Stop();
        }

        bool ReadString(string file)
        {
            var fname = Path.ChangeExtension(file, ".trs");
            if ((_strings = GetStrings(_vm, fname, false)) != null)
            {
                return true;
            }

            if (_vm.Game.GameId == GameId.Dig && (_strings = GetStrings(_vm, "digtxt.trs", true)) != null)
            {
                return true;
            }
            return false;
        }

        TrsFile GetStrings(ScummEngine vm, string file, bool is_encoded)
        {
            Debug.WriteLine("trying to read text resources from {0}", file);
            var filename = ScummHelper.LocatePath(Path.GetDirectoryName(_vm.Game.Path), Path.GetFileName(file));
            return filename != null ? TrsFile.Load(filename) : null;
        }

        ScummEngine7 _vm;
        SmushMixer _smixer;

        int _nbframes;
        short[] _deltaPal = new short[0x300];
        Palette _pal;
        SmushFont[] _sf = new SmushFont[5];
        TrsFile _strings;
        Codec37Decoder _codec37;
        //        Codec47Decoder _codec47;
        XorReader _base;
        uint _baseSize;
        byte[] _frameBuffer;
        byte[] _specialBuffer;

        string _seekFile;
        int _startFrame;
        int _startTime;
        int _seekPos;
        int _seekFrame;

        internal bool _skipNext;
        int _frame;

        SoundHandle _IACTchannel = new SoundHandle();
        QueuingAudioStream _IACTstream;

        SoundHandle _compressedFileSoundHandle = new SoundHandle();
        bool _compressedFileMode;
        byte[] _IACToutput = new byte[4096];
        int _IACTpos;
        bool _storeFrame;
        int _speed;
        bool _endOfFile;

        byte[] _dst;
        bool _updateNeeded;
        bool _warpNeeded;
        int _palDirtyMin, _palDirtyMax;
        int _warpX, _warpY;
        int _warpButtons;
        bool _insanity;
        bool _middleAudio;
        bool _skipPalette;

        int _width, _height;

        int _origPitch, _origNumStrips;
        bool _paused;
        int _pauseStartTime;
        int _pauseTime;

    }

}

