using System;
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.Graphics;

namespace NScumm.Sky
{
    internal class Screen
    {
        private const int VgaColors = 256;
        private const int GameColors = 240;
        private const int Fore = 1;
        private const int Back = 0;
        private const int ScrollJump = 16;

        public const int GameScreenWidth = 320;
        public const int GameScreenHeight = 192;
        public const int FullScreenWidth = 320;
        public const int FullScreenHeight = 200;

        private const int GridX = 20;
        private const int GridY = 24;
        private const int GridW = 16;
        private const int GridH = 8;
        private const int GridWShift = 4;
        private const int GridHShift = 3;
        private const int TopLeftX = 128;
        private const int TopLeftY = 136;

        private static readonly Color[] Top16Colors =
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

        private readonly byte[] _gameGrid;
        private int _maskX1;
        private int _maskX2;
        private Color[] _palette;
        private byte[] _scrollScreen;
        private readonly byte[] _seqGrid = new byte[20 * 12];

        private SequenceIntro _seqInfo;
        private readonly SkyCompact _skyCompact;
        private readonly Disk _skyDisk;
        private ushort _sprHeight;
        private ushort _sprWidth;
        private uint _sprX;
        private uint _sprY;

        private readonly ISystem _system;

        public Screen(ISystem system, Disk disk, SkyCompact skyCompact)
        {
            _system = system;
            _skyDisk = disk;
            _skyCompact = skyCompact;

            _gameGrid = new byte[GridX * GridY * 2];
            ForceRefresh();

            //blank the first 240 colors of the palette
            var tmpPal = new Color[VgaColors];
            //set the remaining colors
            for (var i = 0; i < VgaColors - GameColors; i++)
            {
                tmpPal[GameColors + i] = Color.FromRgb(
                    (Top16Colors[i].R << 2) + (Top16Colors[i].R >> 4)
                    , (Top16Colors[i].G << 2) + (Top16Colors[i].G >> 4)
                    , (Top16Colors[i].B << 2) + (Top16Colors[i].B >> 4));
            }
            //set the palette
            _system.GraphicsManager.SetPalette(tmpPal, 0, VgaColors);
        }

        public byte[] Current { get; private set; }

        public Logic Logic { get; internal set; }

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

        public void SetPaletteEndian(byte[] pal)
        {
            // TODO: BigEndian
            _palette = ConvertPalette(pal);
            _system.GraphicsManager.SetPalette(_palette, 0, GameColors);
            _system.GraphicsManager.UpdateScreen();
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

        public void HalvePalette()
        {
            var halfPalette = new Color[VgaColors];

            for (var cnt = 0; cnt < GameColors; cnt++)
            {
                var c = _palette[cnt];
                halfPalette[cnt] = Color.FromRgb(c.R >> 1, c.G >> 1, c.B >> 1);
            }
            _system.GraphicsManager.SetPalette(halfPalette, 0, GameColors);
        }

        public void ShowScreen(int fileNum)
        {
            // This is only used for static images in the floppy and cd intro
            Current = _skyDisk.LoadFile(fileNum);
            // make sure the last 8 lines are forced to black.
            Array.Clear(Current, GameScreenHeight * GameScreenWidth, (FullScreenHeight - GameScreenHeight) * GameScreenWidth);

            ShowScreen(Current);
        }

        public void ShowScreen(byte[] screen)
        {
            _system.GraphicsManager.CopyRectToScreen(screen, 320, 0, 0, GameScreenWidth, GameScreenHeight);
            _system.GraphicsManager.UpdateScreen();
        }


        public void StartSequence(ushort fileNum)
        {
            _seqInfo.SeqData = _skyDisk.LoadFile(fileNum);
            _seqInfo.NextFrame = Environment.TickCount + 60;
            _seqInfo.FramesLeft = _seqInfo.SeqData[0];
            _seqInfo.SeqDataPos = 1;
            _seqInfo.Running = true;
            _seqInfo.RunningItem = false;
        }

        public bool SequenceRunning()
        {
            return _seqInfo.Running;
        }

        public void StopSequence()
        {
            _seqInfo.Running = false;
            WaitForTick();
            WaitForTick();
            _seqInfo.NextFrame = 0;
            _seqInfo.FramesLeft = 0;
            _seqInfo.SeqData = null;
            _seqInfo.SeqDataPos = 0;
        }

        public uint SeqFramesLeft()
        {
            return _seqInfo.FramesLeft;
        }

        public void ProcessSequence()
        {
            if (!_seqInfo.Running)
                return;

            if (Environment.TickCount < _seqInfo.NextFrame)
                return;

            _seqInfo.NextFrame += 60;

            Array.Clear(_seqGrid, 0, 12 * 20);

            var screenPos = 0;

            do
            {
                byte nrToSkip;
                do
                {
                    nrToSkip = _seqInfo.SeqData[_seqInfo.SeqDataPos];
                    _seqInfo.SeqDataPos++;
                    screenPos += nrToSkip;
                } while (nrToSkip == 0xFF);

                byte nrToDo;
                do
                {
                    nrToDo = _seqInfo.SeqData[_seqInfo.SeqDataPos];
                    _seqInfo.SeqDataPos++;

                    var gridSta = (byte)(screenPos / (GameScreenWidth * 16) * 20 + ((screenPos % GameScreenWidth) >> 4));
                    var gridEnd =
                        (byte)
                            ((screenPos + nrToDo) / (GameScreenWidth * 16) * 20 +
                             (((screenPos + nrToDo) % GameScreenWidth) >> 4));
                    gridSta = Math.Min(gridSta, (byte)(12 * 20 - 1));
                    gridEnd = Math.Min(gridEnd, (byte)(12 * 20 - 1));
                    byte cnt;
                    if (gridEnd >= gridSta)
                        for (cnt = gridSta; cnt <= gridEnd; cnt++)
                            _seqGrid[cnt] = 1;
                    else
                    {
                        for (cnt = gridSta; cnt < (gridSta / 20 + 1) * 20; cnt++)
                            _seqGrid[cnt] = 1;
                        for (cnt = (byte)(gridEnd / 20 * 20); cnt <= gridEnd; cnt++)
                            _seqGrid[cnt] = 1;
                    }

                    for (cnt = 0; cnt < nrToDo; cnt++)
                    {
                        Current[screenPos] = _seqInfo.SeqData[_seqInfo.SeqDataPos];
                        _seqInfo.SeqDataPos++;
                        screenPos++;
                    }
                } while (nrToDo == 0xFF);
            } while (screenPos < GameScreenWidth * GameScreenHeight);
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
                        _system.GraphicsManager.CopyRectToScreen(Current, rectPtr, GameScreenWidth, rectX << 4,
                            rectY << 4, rectWid << 4, 16);
                        rectWid = 0;
                    }
                    scrPtr += 16;
                    gridPtr++;
                }
                if (rectWid != 0)
                {
                    _system.GraphicsManager.CopyRectToScreen(Current, rectPtr, GameScreenWidth, rectX << 4, rectY << 4,
                        rectWid << 4, 16);
                    rectWid = 0;
                }
                scrPtr += 15 * GameScreenWidth;
            }
            _system.GraphicsManager.UpdateScreen();
            _seqInfo.FramesLeft--;

            if (_seqInfo.FramesLeft == 0)
            {
                _seqInfo.Running = false;
                if (!_seqInfo.RunningItem)
                    _seqInfo.SeqData = null;
                _seqInfo.SeqData = null;
                _seqInfo.SeqDataPos = 0;
            }
        }

        //- regular screen.asm routines
        public void ForceRefresh()
        {
            _gameGrid.Set(0, 0x80, GridX * GridY);
        }

        public void FnFadeDown(uint scroll)
        {
            if (((scroll != 123) && (scroll != 321)) || SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.NO_SCROLL))
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
                _scrollScreen = Current;
                Current = new byte[FullScreenWidth * FullScreenHeight];
                // the game will draw the new room into _currentScreen which
                // will be scrolled into the visible screen by fnFadeUp
                // fnFadeUp also frees the _scrollScreen
            }
        }

        public void FnDrawScreen(uint palette, uint scroll)
        {
            // set up the new screen
            FnFadeDown(scroll);
            ForceRefresh();
            Recreate();
            SpriteEngine();
            Flip(false);
            FnFadeUp(palette, scroll);
        }

        public void FnFadeUp(uint palNum, uint scroll)
        {
            //_currentScreen points to new screen,
            //_scrollScreen points to graphic showing old room
            if ((scroll != 123) && (scroll != 321))
                scroll = 0;

            if ((scroll == 0) || SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.NO_SCROLL))
            {
                var palette = _skyCompact.FetchCptRaw((ushort)palNum);
                if (palette == null)
                    throw new NotSupportedException(string.Format("Screen::fnFadeUp: can't fetch compact {0:X2}", palNum));
                //# ifdef SCUMM_BIG_ENDIAN
                //                byte tmpPal[VGA_COLORS * 3];
                //                for (uint16 cnt = 0; cnt < VGA_COLORS * 3; cnt++)
                //                    tmpPal[cnt] = palette[cnt ^ 1];
                //                paletteFadeUp(tmpPal);
                //#else
                PaletteFadeUp(palette);
                //#endif
            }
            else if (scroll == 123)
            {
                // scroll left (going right)
                Debug.Assert(Current != null && _scrollScreen != null);
                for (var scrollCnt = 0; scrollCnt < GameScreenWidth / ScrollJump - 1; scrollCnt++)
                {
                    var scrNewPtr = scrollCnt * ScrollJump;
                    var scrOldPtr = 0;
                    for (var lineCnt = 0; lineCnt < GameScreenHeight; lineCnt++)
                    {
                        Buffer.BlockCopy(_scrollScreen, scrOldPtr + ScrollJump, _scrollScreen, scrOldPtr,
                            GameScreenWidth - ScrollJump);
                        Buffer.BlockCopy(Current, scrNewPtr, _scrollScreen, scrOldPtr + GameScreenWidth - ScrollJump,
                            GameScreenWidth - ScrollJump);
                        scrNewPtr += GameScreenWidth;
                        scrOldPtr += GameScreenWidth;
                    }
                    ShowScreen(_scrollScreen);
                    WaitForTick();
                }
                ShowScreen(Current);
            }
            else if (scroll == 321)
            {
                // scroll right (going left)
                Debug.Assert(Current != null && _scrollScreen != null);
                for (var scrollCnt = 0; scrollCnt < GameScreenWidth / ScrollJump - 1; scrollCnt++)
                {
                    var scrNewPtr = GameScreenWidth - (scrollCnt + 1) * ScrollJump;
                    var scrOldPtr = 0;
                    for (var lineCnt = 0; lineCnt < GameScreenHeight; lineCnt++)
                    {
                        Buffer.BlockCopy(_scrollScreen, scrOldPtr + ScrollJump, _scrollScreen, scrOldPtr,
                            GameScreenWidth - ScrollJump);
                        Buffer.BlockCopy(Current, scrNewPtr, _scrollScreen, scrOldPtr, ScrollJump);
                        scrNewPtr += GameScreenWidth;
                        scrOldPtr += GameScreenWidth;
                    }
                    ShowScreen(_scrollScreen);
                    WaitForTick();
                }
                ShowScreen(Current);
            }

            _scrollScreen = null;
        }

        public void Flip(bool doUpdate = true)
        {
            int copyX, copyWidth;
            copyX = copyWidth = 0;
            for (byte cnty = 0; cnty < GridY; cnty++)
            {
                for (byte cntx = 0; cntx < GridX; cntx++)
                {
                    if ((_gameGrid[cnty * GridX + cntx] & 1) != 0)
                    {
                        unchecked
                        {
                            _gameGrid[cnty * GridX + cntx] &= (byte)~1;
                        }
                        if (copyWidth == 0)
                            copyX = cntx * GridW;
                        copyWidth += GridW;
                    }
                    else if (copyWidth != 0)
                    {
                        _system.GraphicsManager.CopyRectToScreen(Current, cnty * GridH * GameScreenWidth + copyX,
                            GameScreenWidth, copyX, cnty * GridH, copyWidth, GridH);
                        copyWidth = 0;
                    }
                }
                if (copyWidth != 0)
                {
                    _system.GraphicsManager.CopyRectToScreen(Current, cnty * GridH * GameScreenWidth + copyX,
                        GameScreenWidth, copyX, cnty * GridH, copyWidth, GridH);
                    copyWidth = 0;
                }
            }
            if (doUpdate)
                _system.GraphicsManager.UpdateScreen();
        }

        public void SpriteEngine()
        {
            DoSprites(Back);
            SortSprites();
            DoSprites(Fore);
        }

        public void Recreate()
        {
            // check the game grid for changed blocks
            if (Logic.ScriptVariables[Logic.LAYER_0_ID] == 0)
                return;

            var gridPos = 0;
            var screenData = SkyEngine.ItemList[Logic.ScriptVariables[Logic.LAYER_0_ID]];
            if (screenData == null)
            {
                throw new InvalidOperationException(string.Format("Screen::recreate():\nSkyEngine::fetchItem(Logic::_scriptVariables[LAYER_0_ID]({0:X})) returned null", Logic.ScriptVariables[Logic.LAYER_0_ID]));
            }
            var screenDataPos = 0;
            var screenPos = 0;

            for (byte cnty = 0; cnty < GridY; cnty++)
            {
                for (byte cntx = 0; cntx < GridX; cntx++)
                {
                    if ((_gameGrid[gridPos] & 0x80) != 0)
                    {
                        _gameGrid[gridPos] &= 0x7F; // reset recreate flag
                        _gameGrid[gridPos] |= 1; // set bit for flip routine
                        var savedScreenY = screenPos;
                        for (byte gridCntY = 0; gridCntY < GridH; gridCntY++)
                        {
                            Array.Copy(screenData, screenDataPos, Current, screenPos, GridW);
                            screenPos += GameScreenWidth;
                            screenDataPos += GridW;
                        }
                        screenPos = savedScreenY + GridW;
                    }
                    else
                    {
                        screenPos += GridW;
                        screenDataPos += GridW * GridH;
                    }
                    gridPos++;
                }
                screenPos += (GridH - 1) * GameScreenWidth;
            }
        }

        public void WaitForSequence()
        {
            while (_seqInfo.Running)
            {
                ProcessSequence();
                ServiceLocator.Platform.Sleep(20);
            }
        }

        public void ClearScreen()
        {
            Array.Clear(Current, 0, FullScreenWidth * FullScreenHeight);
            _system.GraphicsManager.CopyRectToScreen(Current, GameScreenWidth, 0, 0, GameScreenWidth, GameScreenHeight);
            _system.GraphicsManager.UpdateScreen();
        }

        public void StartSequenceItem(ushort itemNum)
        {
            _seqInfo.SeqData = SkyEngine.ItemList[itemNum];
            _seqInfo.NextFrame = Environment.TickCount + 60;
            _seqInfo.FramesLeft = (uint)(_seqInfo.SeqData[0] - 1);
            _seqInfo.SeqDataPos = 1;
            _seqInfo.Running = true;
            _seqInfo.RunningItem = true;
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
                    _palette[colCnt] = Color.FromRgb((tmpPal[colCnt].R * cnt) >> 5, (tmpPal[colCnt].G * cnt) >> 5,
                        (tmpPal[colCnt].B * cnt) >> 5);
                }

                _system.GraphicsManager.SetPalette(_palette, 0, GameColors);
                _system.GraphicsManager.UpdateScreen();

                var waitTime = delayTime - Environment.TickCount;
                if (waitTime < 0)
                    waitTime = 0;
                ServiceLocator.Platform.Sleep(waitTime);
            }
        }

        /// <summary>
        ///     Set a new palette.
        /// </summary>
        /// <param name="pal">pal is an array to dos vga rgb components 0..63</param>
        private void SetPalette(byte[] pal)
        {
            _palette = ConvertPalette(pal);
            _system.GraphicsManager.SetPalette(_palette, 0, GameColors);
            _system.GraphicsManager.UpdateScreen();
        }

        private Color[] ConvertPalette(byte[] pal)
        {
            var colors = new Color[VgaColors];
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
            for (var i = 0; i < num; i++)
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
            var end = start + 20 - start % 20;
            int remain;

            //           Common::EventManager* eventMan = _system.getEventManager();
            //           Common::Event event;

            //while (true) {
            //           while (eventMan.pollEvent(event))
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

        private void SortSprites()
        {
            var sortList = new StSortList[30];
            uint currDrawList = Logic.DRAW_LIST_NO;

            while (Logic.ScriptVariables[currDrawList] != 0)
            {
                // big_sort_loop
                uint spriteCnt = 0;
                var loadDrawList = Logic.ScriptVariables[currDrawList];
                currDrawList++;

                bool nextDrawList;
                do
                {
                    // a_new_draw_list:
                    var drawListData = new UShortAccess(_skyCompact.FetchCptRaw((ushort)loadDrawList), 0);
                    nextDrawList = false;
                    while (!nextDrawList && (drawListData[0] != 0))
                    {
                        if (drawListData[0] == 0xFFFF)
                        {
                            loadDrawList = drawListData[1];
                            nextDrawList = true;
                        }
                        else
                        {
                            // process_this_id:
                            var spriteComp = _skyCompact.FetchCpt(drawListData[0]);
                            if (((spriteComp.Core.status & 4) != 0) && // is it sortable playfield?(!?!)
                                (spriteComp.Core.screen == Logic.ScriptVariables[Logic.SCREEN]))
                            {
                                // on current screen
                                var spriteData = SkyEngine.ItemList[spriteComp.Core.frame >> 6];
                                if (spriteData == null)
                                {
                                    // TODO: debug(9, "Missing file %d", spriteComp.frame >> 6);
                                    spriteComp.Core.status = 0;
                                }
                                else
                                {
                                    sortList[spriteCnt].YCood =
                                        (uint)
                                            (spriteComp.Core.ycood + spriteData.ToUInt16(18) + spriteData.ToUInt16(8));
                                    sortList[spriteCnt].Compact = spriteComp;
                                    sortList[spriteCnt].Sprite = spriteData;
                                    spriteCnt++;
                                }
                            }
                            drawListData.Offset += 2;
                        }
                    }
                } while (nextDrawList);
                // made_list:
                if (spriteCnt > 1)
                {
                    // bubble sort
                    for (var cnt1 = 0; cnt1 < spriteCnt - 1; cnt1++)
                        for (var cnt2 = cnt1 + 1; cnt2 < spriteCnt; cnt2++)
                            if (sortList[cnt1].YCood > sortList[cnt2].YCood)
                            {
                                StSortList tmp;
                                tmp.YCood = sortList[cnt1].YCood;
                                tmp.Sprite = sortList[cnt1].Sprite;
                                tmp.Compact = sortList[cnt1].Compact;
                                sortList[cnt1].YCood = sortList[cnt2].YCood;
                                sortList[cnt1].Sprite = sortList[cnt2].Sprite;
                                sortList[cnt1].Compact = sortList[cnt2].Compact;
                                sortList[cnt2].YCood = tmp.YCood;
                                sortList[cnt2].Sprite = tmp.Sprite;
                                sortList[cnt2].Compact = tmp.Compact;
                            }
                }
                for (var cnt = 0; cnt < spriteCnt; cnt++)
                {
                    DrawSprite(sortList[cnt].Sprite, sortList[cnt].Compact);
                    if ((sortList[cnt].Compact.Core.status & 8) != 0)
                        VectorToGame(0x81);
                    else
                        VectorToGame(1);
                    if ((sortList[cnt].Compact.Core.status & 0x200) == 0)
                        VerticalMask();
                }
            }
        }

        private void DoSprites(byte layer)
        {
            ushort drawListNum = Logic.DRAW_LIST_NO;
            while (Logic.ScriptVariables[drawListNum] != 0)
            {
                // std sp loop
                var idNum = Logic.ScriptVariables[drawListNum];
                drawListNum++;

                var drawList = new UShortAccess(_skyCompact.FetchCptRaw((ushort)idNum), 0);
                while (drawList[0] != 0)
                {
                    // new_draw_list:
                    while ((drawList[0] != 0) && (drawList[0] != 0xFFFF))
                    {
                        // back_loop:
                        // not_new_list
                        var spriteData = _skyCompact.FetchCpt(drawList[0]);
                        drawList.Offset += 2;
                        if (((spriteData.Core.status & (1 << layer)) != 0) &&
                            (spriteData.Core.screen == Logic.ScriptVariables[Logic.SCREEN]))
                        {
                            var toBeDrawn = SkyEngine.ItemList[spriteData.Core.frame >> 6];
                            if (toBeDrawn == null)
                            {
                                // TODO: debug(9, "Spritedata %d not loaded", spriteData.frame >> 6);
                                spriteData.Core.status = 0;
                            }
                            else
                            {
                                DrawSprite(toBeDrawn, spriteData);
                                if (layer == Back)
                                    VerticalMask();
                                if ((spriteData.Core.status & 8) != 0)
                                    VectorToGame(0x81);
                                else
                                    VectorToGame(1);
                            }
                        }
                    }
                    while (drawList[0] == 0xFFFF)
                        drawList = new UShortAccess(_skyCompact.FetchCptRaw(drawList[1]), 0);
                }
            }
        }

        private void VectorToGame(byte gridVal)
        {
            if (_sprWidth == 0)
                return;
            var trgGrid = _sprY * GridX + _sprX;
            for (var cnty = 0; cnty < _sprHeight; cnty++)
            {
                for (var cntx = 0; cntx < _sprWidth; cntx++)
                    _gameGrid[trgGrid + cntx] |= gridVal;
                trgGrid += GridX;
            }
        }

        private void DrawSprite(byte[] spriteInfo, Compact sprCompact)
        {
            if (spriteInfo == null)
            {
                // TODO: warning("Screen::drawSprite Can't draw sprite. Data %d was not loaded", sprCompact.frame >> 6);
                sprCompact.Core.status = 0;
                return;
            }
            var sprDataFile = ServiceLocator.Platform.ToStructure<DataFileHeader>(spriteInfo, 0);
            _sprWidth = sprDataFile.s_width;
            _sprHeight = sprDataFile.s_height;
            _maskX1 = _maskX2 = 0;
            var spriteData = (sprCompact.Core.frame & 0x3F) * sprDataFile.s_sp_size;
            spriteData += ServiceLocator.Platform.SizeOf<DataFileHeader>();
            var spriteY = sprCompact.Core.ycood + sprDataFile.s_offset_y - TopLeftY;
            if (spriteY < 0)
            {
                spriteY = -spriteY;
                if (_sprHeight <= (uint)spriteY)
                {
                    _sprWidth = 0;
                    return;
                }
                _sprHeight -= (ushort)spriteY;
                spriteData += sprDataFile.s_width * spriteY;
                spriteY = 0;
            }
            else
            {
                var botClip = GameScreenHeight - sprDataFile.s_height - spriteY;
                if (botClip < 0)
                {
                    botClip = -botClip;
                    if (_sprHeight <= (uint)botClip)
                    {
                        _sprWidth = 0;
                        return;
                    }
                    _sprHeight -= (ushort)botClip;
                }
            }
            _sprY = (uint)spriteY;
            var spriteX = sprCompact.Core.xcood + sprDataFile.s_offset_x - TopLeftX;
            if (spriteX < 0)
            {
                spriteX = -spriteX;
                if (_sprWidth <= (uint)spriteX)
                {
                    _sprWidth = 0;
                    return;
                }
                _sprWidth -= (ushort)spriteX;
                _maskX1 = spriteX;
                spriteX = 0;
            }
            else
            {
                var rightClip = GameScreenWidth - (sprDataFile.s_width + spriteX);
                if (rightClip < 0)
                {
                    rightClip = -rightClip + 1;
                    if (_sprWidth <= (uint)rightClip)
                    {
                        _sprWidth = 0;
                        return;
                    }
                    _sprWidth -= (ushort)rightClip;
                    _maskX2 = rightClip;
                }
            }
            _sprX = (uint)spriteX;
            var screenPtr = _sprY * GameScreenWidth + _sprX;
            if ((_sprHeight > 192) || (_sprY > 192))
            {
                _sprWidth = 0;
                return;
            }
            if ((_sprX + _sprWidth > 320) || (_sprY + _sprHeight > 192))
            {
                // TODO: warning("Screen::drawSprite fatal error: got x = %d, y = %d, w = %d, h = %d", _sprX, _sprY, _sprWidth, _sprHeight);
                _sprWidth = 0;
                return;
            }

            for (ushort cnty = 0; cnty < _sprHeight; cnty++)
            {
                for (ushort cntx = 0; cntx < _sprWidth; cntx++)
                    if (spriteInfo[spriteData + cntx + _maskX1] != 0)
                        Current[screenPtr + cntx] = spriteInfo[spriteData + cntx + _maskX1];
                spriteData += _sprWidth + _maskX2 + _maskX1;
                screenPtr += GameScreenWidth;
            }
            // Convert the sprite coordinate/size values to blocks for vertical mask and/or vector to game
            _sprWidth += (ushort)(_sprX + GridW - 1);
            _sprHeight += (ushort)(_sprY + GridH - 1);

            _sprX >>= GridWShift;
            _sprWidth >>= GridWShift;
            _sprY >>= GridHShift;
            _sprHeight >>= GridHShift;

            _sprWidth -= (ushort)_sprX;
            _sprHeight -= (ushort)_sprY;
        }

        private void VerticalMask()
        {
            if (_sprWidth == 0)
                return;
            var startGridOfs = (int)((_sprY + _sprHeight - 1) * GridX + _sprX);
            var startScreenPtr = (int)((_sprY + _sprHeight - 1) * GridH * GameScreenWidth + _sprX * GridW);

            for (uint layerCnt = Logic.LAYER_1_ID; layerCnt <= Logic.LAYER_3_ID; layerCnt++)
            {
                var gridOfs = startGridOfs;
                var screenPtr = startScreenPtr;
                for (uint widCnt = 0; widCnt < _sprWidth; widCnt++)
                {
                    // x_loop
                    var nLayerCnt = layerCnt;
                    while (Logic.ScriptVariables[nLayerCnt + 3] != 0)
                    {
                        var scrGrid = new UShortAccess(SkyEngine.ItemList[Logic.ScriptVariables[layerCnt + 3]], 0);
                        if (scrGrid[gridOfs] != 0)
                        {
                            VertMaskSub(scrGrid, gridOfs, screenPtr, layerCnt);
                            break;
                        }
                        nLayerCnt++;
                    }
                    // next_x:
                    screenPtr += GridW;
                    gridOfs++;
                }
            }
        }

        private void VertMaskSub(UShortAccess grid, int gridOfs, int screenPtr, uint layerId)
        {
            for (var cntx = 0; cntx < _sprHeight; cntx++)
            {
                // start_x | block_loop
                if (grid[gridOfs] != 0)
                {
                    if ((grid[gridOfs] & 0x8000) == 0)
                    {
                        var gridVal = grid[gridOfs] - 1;
                        gridVal *= GridW * GridH;
                        var dataSrc = new ByteAccess(SkyEngine.ItemList[Logic.ScriptVariables[layerId]], gridVal);
                        var dataTrg = screenPtr;
                        for (var grdCntY = 0; grdCntY < GridH; grdCntY++)
                        {
                            for (var grdCntX = 0; grdCntX < GridW; grdCntX++)
                                if (dataSrc[grdCntX] != 0)
                                    Current[dataTrg + grdCntX] = dataSrc[grdCntX];
                            dataSrc.Offset += GridW;
                            dataTrg += GameScreenWidth;
                        }
                    } // dummy_end:
                    screenPtr -= GridH * GameScreenWidth;
                    gridOfs -= GridX;
                }
                else
                    return;
            } // next_x
        }

        public void SetFocusRectangle(Rect rect)
        {
            // TODO: _system.SetFocusRectangle(rect);
            //_system.SetFocusRectangle(rect);
        }

        private struct SequenceIntro
        {
            public int NextFrame;
            public uint FramesLeft;
            public byte[] SeqData;
            public int SeqDataPos;
            public volatile bool Running;
            public bool RunningItem; // when playing an item, don't free it afterwards.
        }

        private struct StSortList
        {
            public uint YCood;
            public Compact Compact;
            public byte[] Sprite;
        }
    }
}