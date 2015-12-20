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

using NScumm.Core;
using NScumm.Sky.Music;
using System;
using NScumm.Core.Audio;
using NScumm.Core.Audio.Decoders;
using System.IO;

namespace NScumm.Sky
{
    partial class Intro : IDisposable
    {
        const int FrameSize = Screen.GameScreenWidth * Screen.GameScreenHeight;
        const int IntroTextWidth = 128;

        public Intro(Disk disk, Screen screen, MusicBase music, Sound sound, Text skyText, Mixer mixer, SkySystem system)
        {
            _skyDisk = disk;
            _skyScreen = screen;
            _skyMusic = music;
            _skySound = sound;
            _skyText = skyText;
            _mixer = mixer;
            _system = system;
            _textBuf = new byte[10000];
            _saveBuf = new byte[10000];
        }

        public void Dispose()
        {
            if (_skyScreen.SequenceRunning())
                _skyScreen.StopSequence();
            _mixer.StopID(Sound.SoundBg);
        }

        public bool DoIntro(bool floppyIntro)
        {
            if (!SkyEngine.IsCDVersion)
            {
                floppyIntro = true;
            }

            _skyMusic.LoadSection(0);
            _skySound.LoadSection(0);

            if (!EscDelay(3000))
                return false;

            if (floppyIntro)
                _skyMusic.StartMusic(1);

            var seqData = _mainIntroSeq;
            int i = 0;
            while (seqData[i] != SEQEND)
            {
                if (!NextPart(seqData, ref i))
                    return false;
            }

            seqData = floppyIntro ? _floppyIntroSeq : _cdIntroSeq;

            i = 0;
            while (seqData[i] != SEQEND)
            {
                if (!NextPart(seqData, ref i))
                    return false;
            }
            return true;
        }

        private bool NextPart(ushort[] data, ref int i)
        {
            // return false means cancel intro
            var command = data[i++];
            switch (command)
            {
                case SHOWSCREEN:
                    _skyScreen.ShowScreen(data[i++]);
                    return true;
                case FADEUP:
                    _skyScreen.PaletteFadeUp(data[i++]);
                    _relDelay += 32 * 20; // hack: the screen uses a seperate delay function for the
                                          // blocking fadeups. So add 32*20 msecs to out delay counter.
                    return true;
                case FADEDOWN:
                    _skyScreen.FnFadeDown(0);
                    _relDelay += 32 * 20; // hack: see above.
                    return true;
                case DELAY:
                    if (!EscDelay(data[i++]))
                        return false;
                    return true;
                case DOFLIRT:
                    _skyScreen.StartSequence(data[i++]);
                    while (_skyScreen.SequenceRunning())
                        if (!EscDelay(50))
                            return false;
                    return true;
                case SCROLLFLIRT:
                    return FloppyScrollFlirt();
                case COMMANDFLIRT:
                    return CommandFlirt(data, ref i);
                case STOPFLIRT:
                    _skyScreen.StopSequence();
                    return true;
                case STARTMUSIC:
                    _skyMusic.StartMusic(data[i++]);
                    return true;
                case WAITMUSIC:
                    while (_skyMusic.IsPlaying)
                        if (!EscDelay(50))
                            return false;
                    return true;
                case BGFLIRT:
                    _skyScreen.StartSequence(data[i++]);
                    return true;
                case WAITFLIRT:
                    while (_skyScreen.SequenceRunning())
                        if (!EscDelay(50))
                            return false;
                    return true;
                case PLAYVOICE:
                    {
                        if (!EscDelay(200))
                            return false;
                        var vData = _skyDisk.LoadFile(data[i++]);
                        // HACK: Fill the header with silence. We should
                        // probably use _skySound instead of calling playStream()
                        // directly, but this will have to do for now.
                        vData.Set(0, 127, ServiceLocator.Platform.SizeOf<DataFileHeader>());

                        var stream = new RawStream(AudioFlags.Unsigned, 11025, true, new MemoryStream(vData));
                        _voice = _mixer.PlayStream(SoundType.Speech, stream, Sound.SoundVoice);
                    }
                    return true;
                case WAITVOICE:
                    while (_mixer.IsSoundHandleActive(_voice))
                        if (!EscDelay(50))
                            return false;
                    return true;
                case LOADBG:
                    _mixer.StopID(Sound.SoundBg);
                    _bgBuf = _skyDisk.LoadFile(data[i++]);
                    return true;
                case LOOPBG:
                    {
                        _mixer.StopID(Sound.SoundBg);
                        var stream = new RawStream(AudioFlags.Unsigned, 11025, false, new MemoryStream(_bgBuf, 256, _bgBuf.Length - 768));
                        _bgSfx = _mixer.PlayStream(SoundType.SFX, new LoopingAudioStream(stream, 0), Sound.SoundBg);
                    }
                    return true;
                case PLAYBG:
                    {
                        _mixer.StopID(Sound.SoundBg);
                        var stream = new RawStream(AudioFlags.Unsigned, 11025, false, new MemoryStream(_bgBuf, 256, _bgBuf.Length - 768));
                        _bgSfx = _mixer.PlayStream(SoundType.SFX, stream, Sound.SoundBg);
                    }
                    return true;
                case STOPBG:
                    _mixer.StopID(Sound.SoundBg);
                    return true;
                default:
                    throw new NotSupportedException(string.Format("Unknown intro command {0:X2}", command));
            }
        }

        private bool CommandFlirt(ushort[] data, ref int i)
        {
            _skyScreen.StartSequence(data[i++]);

            while ((data[i] != COMMANDEND) || _skyScreen.SequenceRunning())
            {
                while ((_skyScreen.SeqFramesLeft() < data[i]))
                {
                    i++;
                    var command = data[i++];
                    switch (command)
                    {
                        case IC_PREPARE_TEXT:
                            _skyText.DisplayText(data[i++], _textBuf, true, IntroTextWidth, 255);
                            break;
                        case IC_SHOW_TEXT:
                            using (var header = ServiceLocator.Platform.WriteStructure<DataFileHeader>(_textBuf, 0))
                            {
                                header.Object.s_x = data[i++];
                                header.Object.s_y = data[i++];
                            }
                            ShowTextBuf();
                            break;
                        case IC_REMOVE_TEXT:
                            RestoreScreen();
                            break;
                        case IC_MAKE_SOUND:
                            _skySound.PlaySound(data[0], data[1], 0);
                            i += 2;
                            break;
                        case IC_FX_VOLUME:
                            _skySound.PlaySound(1, data[i], 0);
                            i++;
                            break;
                        default:
                            throw new NotSupportedException(string.Format("Unknown FLIRT command {0:X2}", command));
                    }
                }

                if (!EscDelay(50))
                {
                    _skyScreen.StopSequence();
                    return false;
                }
            }

            i++; // move pointer over "COMMANDEND"
            return true;
        }

        private void ShowTextBuf()
        {
            var header = ServiceLocator.Platform.ToStructure<DataFileHeader>(_textBuf, 0);
            ushort x = header.s_x;
            ushort y = header.s_y;
            ushort width = header.s_width;
            ushort height = header.s_height;
            var screenBuf = y * Screen.GameScreenWidth + x;
            var sizeofDataFileHeader = 22;
            Array.Copy(_textBuf, _saveBuf, sizeofDataFileHeader);
            var saveBuf = sizeofDataFileHeader;
            var textBuf = sizeofDataFileHeader;
            for (ushort cnty = 0; cnty < height; cnty++)
            {
                Array.Copy(_skyScreen.Current, screenBuf, _saveBuf, saveBuf, width);
                for (var cntx = 0; cntx < width; cntx++)
                    if (_textBuf[textBuf + cntx] != 0)
                        _skyScreen.Current[screenBuf + cntx] = _textBuf[textBuf + cntx];
                screenBuf += Screen.GameScreenWidth;
                textBuf += width;
                saveBuf += width;
            }
            screenBuf = y * Screen.GameScreenWidth + x;
            _system.GraphicsManager.CopyRectToScreen(_skyScreen.Current, screenBuf, Screen.GameScreenWidth, x, y, width, height);
        }

        private void RestoreScreen()
        {
            var header = ServiceLocator.Platform.ToStructure<DataFileHeader>(_saveBuf, 0);
            ushort x = header.s_x;
            ushort y = header.s_y;
            ushort width = header.s_width;
            ushort height = header.s_height;
            var screenBuf = y * Screen.GameScreenWidth + x;
            var sizeofDataFileHeader = 22;
            var saveBuf = sizeofDataFileHeader;
            for (var cnt = 0; cnt < height; cnt++)
            {
                Array.Copy(_saveBuf, saveBuf, _skyScreen.Current, screenBuf, width);
                screenBuf += Screen.GameScreenWidth;
                saveBuf += width;
            }
            _system.GraphicsManager.CopyRectToScreen(_saveBuf, sizeofDataFileHeader, width, x, y, width, height);
        }

        private bool FloppyScrollFlirt()
        {
            var scrollScreen = new byte[FrameSize * 2];
            Array.Copy(_skyScreen.Current, 0, scrollScreen, FrameSize, FrameSize);
            var scrollPos = FrameSize;
            var vgaData = _skyDisk.LoadFile(60100);
            var diffData = _skyDisk.LoadFile(60101);
            var frameNum = diffData.ToUInt16();
            var diffPtr = 2;
            var vgaPtr = 0;
            bool doContinue = true;

            for (ushort frameCnt = 1; (frameCnt < frameNum) && doContinue; frameCnt++)
            {
                var scrollVal = diffData[diffPtr++];
                if (scrollVal != 0)
                    scrollPos -= scrollVal * Screen.GameScreenWidth;

                ushort scrPos = 0;
                while (scrPos < FrameSize)
                {
                    byte nrToDo, nrToSkip;
                    do
                    {
                        nrToSkip = diffData[diffPtr++];
                        scrPos += nrToSkip;
                    } while (nrToSkip == 255);
                    do
                    {
                        nrToDo = diffData[diffPtr++];
                        Array.Copy(vgaData, vgaPtr, scrollScreen, scrollPos + scrPos, nrToDo);
                        scrPos += nrToDo;
                        vgaPtr += nrToDo;
                    } while (nrToDo == 255);
                }
                _system.GraphicsManager.CopyRectToScreen(scrollScreen, scrollPos, Screen.GameScreenWidth, 0, 0, Screen.GameScreenWidth, Screen.GameScreenHeight);
                _system.GraphicsManager.UpdateScreen();
                if (!EscDelay(60))
                    doContinue = false;
            }
            Array.Copy(scrollScreen, scrollPos, _skyScreen.Current, 0, FrameSize);
            return doContinue;
        }

        private bool EscDelay(int msecs)
        {
            var im = _system.InputManager;

            if (_relDelay == 0) // first call, init with system time
                _relDelay = Environment.TickCount;

            _relDelay += msecs; // now wait until Environment.TickCount >= _relDelay

            int nDelay;
            do
            {
                if (im.GetState().IsKeyDown(KeyCode.Escape))
                    return false;

                nDelay = _relDelay - Environment.TickCount;
                if (nDelay < 0)
                    nDelay = 0;
                else if (nDelay > 20)
                    nDelay = 20;

                ServiceLocator.Platform.Sleep(nDelay);

                _skyScreen.ProcessSequence();
                _system.GraphicsManager.UpdateScreen();
            } while (nDelay == 20);

            return true;
        }

        private readonly byte[] _saveBuf;
        private readonly Disk _skyDisk;
        private readonly Screen _skyScreen;
        private readonly SkySystem _system;
        private readonly byte[] _textBuf;
        private int _relDelay;
        private readonly Text _skyText;
        private readonly MusicBase _skyMusic;
        private readonly Sound _skySound;
        private readonly Mixer _mixer;
        private SoundHandle _voice;
        private byte[] _bgBuf;
        private SoundHandle _bgSfx;
    }
}