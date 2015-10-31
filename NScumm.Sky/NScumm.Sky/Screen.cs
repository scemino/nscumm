using NScumm.Core;
using NScumm.Core.Graphics;
using System;

namespace NScumm.Sky
{
    class Screen
    {
        const int VgaColors = 256;
        const int GameColors = 240;

        public const int GameScreenWidth = 320;
        public const int GameScreenHeight = 192;
        const int FullScreenWidth = 320;
        const int FullScreenHeight = 200;

        const int GridX = 20;
        const int GridY = 24;
        const int GridW = 16;
        const int GridH = 8;

        public byte[] Current
        {
            get { return _currentScreen; }
        }

        public Screen(ISystem system, Disk disk, SkyCompact skyCompact)
        {
            _system = system;
            _skyDisk = disk;

            _gameGrid = new byte[GridX * GridY * 2];
            ForceRefresh();

            //blank the first 240 colors of the palette
            var tmpPal = new Color[VgaColors];
            //set the remaining colors
            for (var i = 0; i < (VgaColors - GameColors); i++)
            {
                tmpPal[GameColors + i] = Color.FromRgb(
                      (_top16Colors[i].R << 2) + (_top16Colors[i].R >> 4)
                    , (_top16Colors[i].G << 2) + (_top16Colors[i].G >> 4)
                    , (_top16Colors[i].B << 2) + (_top16Colors[i].B >> 4));
            }
            //set the palette
            _system.GraphicsManager.SetPalette(tmpPal, 0, VgaColors);
        }

        /// <summary>
        /// Set a new palette.
        /// </summary>
        /// <param name="pal">pal is an array to dos vga rgb components 0..63</param>
        private void SetPalette(byte[] pal)
        {
            _palette = ConvertPalette(pal);
            _system.GraphicsManager.SetPalette(_palette, 0, GameColors);
            _system.GraphicsManager.UpdateScreen();
        }

        public void SetPalette(ushort fileNum)
        {
            var tmpPal = _skyDisk.LoadFile(fileNum);
            if (tmpPal != null)
            {
                SetPalette(tmpPal);
            }
            else
                throw new InvalidOperationException(string.Format("Screen::setPalette: can't load file nr. {0}", fileNum));
        }

        public void PaletteFadeUp(ushort fileNr)
        {
            var pal = _skyDisk.LoadFile(fileNr);
            if (pal != null)
            {
                PaletteFadeUp(pal);
            }
            //else
            //    warning("Screen::paletteFadeUp: Can't load palette #%d", fileNr);
        }

        private void PaletteFadeUp(byte[] pal)
        {
            var tmpPal = ConvertPalette(pal);

            var delayTime = Environment.TickCount;
            for (var cnt = 1; cnt <= 32; cnt++)
            {
                delayTime += 20;

                for (var colCnt = 0; colCnt < GameColors; colCnt++)
                {
                    _palette[colCnt] = Color.FromRgb((tmpPal[colCnt].R * cnt) >> 5, (tmpPal[colCnt].G * cnt) >> 5, (tmpPal[colCnt].B * cnt) >> 5);
                }

                _system.GraphicsManager.SetPalette(_palette, 0, GameColors);
                _system.GraphicsManager.UpdateScreen();

                var waitTime = delayTime - Environment.TickCount;
                if (waitTime < 0)
                    waitTime = 0;
                ServiceLocator.Platform.Sleep(waitTime);
            }
        }


        public void ShowScreen(int fileNum)
        {
            // This is only used for static images in the floppy and cd intro
            _currentScreen = _skyDisk.LoadFile(fileNum);
            // make sure the last 8 lines are forced to black.
            Array.Clear(_currentScreen, GameScreenHeight * GameScreenWidth, (FullScreenHeight - GameScreenHeight) * GameScreenWidth);

            ShowScreen(_currentScreen);
        }

        private void ShowScreen(byte[] screen)
        {
            _system.GraphicsManager.CopyRectToScreen(screen, 320, 0, 0, GameScreenWidth, GameScreenHeight);
            _system.GraphicsManager.UpdateScreen();
        }


        public void StartSequence(ushort fileNum)
        {
            _seqInfo.seqData = _skyDisk.LoadFile(fileNum);
            _seqInfo.nextFrame = Environment.TickCount + 60;
            _seqInfo.framesLeft = _seqInfo.seqData[0];
            _seqInfo.seqDataPos = 1;
            _seqInfo.running = true;
            _seqInfo.runningItem = false;
        }

        public bool SequenceRunning()
        {
            return _seqInfo.running;
        }

        public void StopSequence()
        {
            _seqInfo.running = false;
            WaitForTick();
            WaitForTick();
            _seqInfo.nextFrame = 0;
            _seqInfo.framesLeft = 0;
            _seqInfo.seqData = null;
            _seqInfo.seqDataPos = 0;
        }

        public uint SeqFramesLeft()
        {
            return _seqInfo.framesLeft;
        }

        public void ProcessSequence()
        {
            if (!_seqInfo.running)
                return;

            if (Environment.TickCount < _seqInfo.nextFrame)
                return;

            _seqInfo.nextFrame += 60;

            Array.Clear(_seqGrid, 0, 12 * 20);

            var screenPos = 0;

            byte nrToSkip, nrToDo, cnt;
            do
            {
                do
                {
                    nrToSkip = _seqInfo.seqData[_seqInfo.seqDataPos];
                    _seqInfo.seqDataPos++;
                    screenPos += nrToSkip;
                } while (nrToSkip == 0xFF);

                do
                {
                    nrToDo = _seqInfo.seqData[_seqInfo.seqDataPos];
                    _seqInfo.seqDataPos++;

                    byte gridSta = (byte)((screenPos / (GameScreenWidth * 16)) * 20 + ((screenPos % GameScreenWidth) >> 4));
                    byte gridEnd = (byte)(((screenPos + nrToDo) / (GameScreenWidth * 16)) * 20 + (((screenPos + nrToDo) % GameScreenWidth) >> 4));
                    gridSta = Math.Min(gridSta, (byte)(12 * 20 - 1));
                    gridEnd = Math.Min(gridEnd, (byte)(12 * 20 - 1));
                    if (gridEnd >= gridSta)
                        for (cnt = gridSta; cnt <= gridEnd; cnt++)
                            _seqGrid[cnt] = 1;
                    else
                    {
                        for (cnt = gridSta; cnt < (gridSta / 20 + 1) * 20; cnt++)
                            _seqGrid[cnt] = 1;
                        for (cnt = (byte)((gridEnd / 20) * 20); cnt <= gridEnd; cnt++)
                            _seqGrid[cnt] = 1;
                    }

                    for (cnt = 0; cnt < nrToDo; cnt++)
                    {
                        _currentScreen[screenPos] = _seqInfo.seqData[_seqInfo.seqDataPos];
                        _seqInfo.seqDataPos++;
                        screenPos++;
                    }
                } while (nrToDo == 0xFF);
            } while (screenPos < (GameScreenWidth * GameScreenHeight));
            var gridPtr = 0;
            var scrPtr = 0;
            var rectPtr = 0;
            byte rectWid = 0, rectX = 0, rectY = 0;
            for (byte cnty = 0; cnty < 12; cnty++)
            {
                for (byte cntx = 0; cntx < 20; cntx++)
                {
                    if (_seqGrid[gridPtr] != 0)
                    {
                        if (rectWid == 0)
                        {
                            rectX = cntx;
                            rectY = cnty;
                            rectPtr = scrPtr;
                        }
                        rectWid++;
                    }
                    else if (rectWid != 0)
                    {
                        _system.GraphicsManager.CopyRectToScreen(_currentScreen, rectPtr, GameScreenWidth, rectX << 4, rectY << 4, rectWid << 4, 16);
                        rectWid = 0;
                    }
                    scrPtr += 16;
                    gridPtr++;
                }
                if (rectWid != 0)
                {
                    _system.GraphicsManager.CopyRectToScreen(_currentScreen, rectPtr, GameScreenWidth, rectX << 4, rectY << 4, rectWid << 4, 16);
                    rectWid = 0;
                }
                scrPtr += 15 * GameScreenWidth;
            }
            _system.GraphicsManager.UpdateScreen();
            _seqInfo.framesLeft--;

            if (_seqInfo.framesLeft == 0)
            {
                _seqInfo.running = false;
                if (!_seqInfo.runningItem)
                    _seqInfo.seqData = null;
                _seqInfo.seqData = null;
                _seqInfo.seqDataPos = 0;
            }
        }

        //- regular screen.asm routines
        public void ForceRefresh()
        {
            _gameGrid.Set(0, 0x80, GridX * GridY);
        }

        public void FnFadeDown(uint scroll)
        {
            if (((scroll != 123) && (scroll != 321)) || (SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.NO_SCROLL)))
            {
                var delayTime = Environment.TickCount;
                for (var cnt = 0; cnt < 32; cnt++)
                {
                    delayTime += 20;
                    PaletteFadeDownHelper(_palette, GameColors);
                    _system.GraphicsManager.SetPalette(_palette, 0, GameColors);
                    _system.GraphicsManager.UpdateScreen();
                    var waitTime = delayTime - Environment.TickCount;
                    if (waitTime < 0)
                        waitTime = 0;
                    ServiceLocator.Platform.Sleep(waitTime);
                }
            }
            else
            {
                // scrolling is performed by fnFadeUp. It's just prepared here
                _scrollScreen = _currentScreen;
                _currentScreen = new byte[FullScreenWidth * FullScreenHeight];
                // the game will draw the new room into _currentScreen which
                // will be scrolled into the visible screen by fnFadeUp
                // fnFadeUp also frees the _scrollScreen
            }
        }


        private Color[] ConvertPalette(byte[] pal)
        {
            Color[] colors = new Color[VgaColors];
            for (var i = 0; i < VgaColors; i++)
            {
                colors[i] = Color.FromRgb(
                    (pal[3 * i + 0] << 2) + (pal[3 * i + 0] >> 4),
                    (pal[3 * i + 1] << 2) + (pal[3 * i + 1] >> 4),
                    (pal[3 * i + 2] << 2) + (pal[3 * i + 2] >> 4));
            }
            return colors;
        }

        private void PaletteFadeDownHelper(Color[] pal, uint num)
        {
            for (int i = 0; i < num; i++)
            {
                if (pal[i].R >= 8)
                    pal[i].R -= 8;
                else
                    pal[i].R = 0;

                if (pal[i].G >= 8)
                    pal[i].G -= 8;
                else
                    pal[i].G = 0;

                if (pal[i].B >= 8)
                    pal[i].B -= 8;
                else
                    pal[i].B = 0;
            }
        }

        private void WaitForTick()
        {
            var start = Environment.TickCount;
            var end = start + 20 - (start % 20);
            int remain;

            //           Common::EventManager* eventMan = _system->getEventManager();
            //           Common::Event event;

            //while (true) {
            //           while (eventMan->pollEvent(event))
            //           ;

            start = Environment.TickCount;

            if (start >= end)
                return;

            remain = end - start;
            if (remain < 10)
            {
                ServiceLocator.Platform.Sleep(remain);
                return;
            }

            ServiceLocator.Platform.Sleep(10);
        }

        struct SequenceIntro
        {
            public int nextFrame;
            public uint framesLeft;
            public byte[] seqData;
            public int seqDataPos;
            public volatile bool running;
            public bool runningItem; // when playing an item, don't free it afterwards.
        }

        private ISystem _system;
        private Disk _skyDisk;
        private Color[] _palette;
        private byte[] _seqGrid = new byte[20 * 12];

        private byte[] _currentScreen;
        private byte[] _scrollScreen;

        private SequenceIntro _seqInfo;
        private byte[] _gameGrid;

        private static readonly Color[] _top16Colors = new Color[]
        {
            Color.FromRgb(0, 0, 0),
            Color.FromRgb(38, 38, 38),
            Color.FromRgb(63, 63, 63),
            Color.FromRgb(0, 0, 0),
            Color.FromRgb(0, 0, 0),
            Color.FromRgb(0, 0, 0),
            Color.FromRgb(0, 0, 0),
            Color.FromRgb(54, 54, 54),
            Color.FromRgb(45, 47, 49),
            Color.FromRgb(32, 31, 41),
            Color.FromRgb(29, 23, 37),
            Color.FromRgb(23, 18, 30),
            Color.FromRgb(49, 11, 11),
            Color.FromRgb(39, 5, 5),
            Color.FromRgb(29, 1, 1),
            Color.FromRgb(63, 63, 63)
        };
    }
}
