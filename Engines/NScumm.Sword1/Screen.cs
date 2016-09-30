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
using System.Linq;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using System.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sword1
{
    class RoomDef
    {
        public int totalLayers;
        public int sizeX;
        public int sizeY;
        public int gridWidth;  //number of 16*16 grid blocks across - including off screen edges.
        public uint[] layers;
        public uint[] grids;
        public uint[] palettes;
        public uint[] parallax;

        public RoomDef()
        {
        }

        public RoomDef(int totalLayers, int sizeX, int sizeY, int gridWidth, uint[] layers, uint[] grids, uint[] palettes, uint[] parallax)
        {
            this.totalLayers = totalLayers;
            this.sizeX = sizeX;
            this.sizeY = sizeY;
            this.gridWidth = sizeX;
            this.layers = layers;
            this.grids = grids;
            this.palettes = palettes;
            this.parallax = parallax;
        }
    }

    class FrameHeader
    {
        public const int Size = 16;

        public ByteAccess runTimeComp
        {
            get { return Data; }
        }

        public uint compSize
        {
            get { return Data.Data.ToUInt32(Data.Offset + 4); }
            set { Data.Data.WriteUInt32(Data.Offset + 4, value); }
        }

        public ushort width
        {
            get { return Data.Data.ToUInt16(Data.Offset + 8); }
            set { Data.Data.WriteUInt16(Data.Offset + 8, value); }
        }

        public ushort height
        {
            get { return Data.Data.ToUInt16(Data.Offset + 10); }
            set { Data.Data.WriteUInt16(Data.Offset + 10, value); }
        }

        public short offsetX
        {
            get { return Data.Data.ToInt16(Data.Offset + 12); }
            set { Data.Data.WriteUInt16(Data.Offset + 12, (ushort)value); }
        }

        public short offsetY
        {
            get { return Data.Data.ToInt16(Data.Offset + 14); }
            set { Data.Data.WriteUInt16(Data.Offset + 14, (ushort)value); }
        }

        public ByteAccess Data { get; }

        public FrameHeader(byte[] data)
            : this(new ByteAccess(data))
        {
        }

        public FrameHeader(ByteAccess data)
        {
            Data = data;
        }
    }

    class ParallaxHeader
    {
        public const int Size = 20;

        public byte[] type
        {
            get { return Data; }
        }

        public ushort sizeX
        {
            get { return Data.ToUInt16(16); }
        }

        public ushort sizeY
        {
            get { return Data.ToUInt16(18); }
        }

        public byte[] Data { get; }

        public ParallaxHeader(byte[] data)
        {
            Data = data;
        }
    }

    class SortSpr
    {
        public int id, y;
    }

    class PSXDataCache
    {
        // Cache for PSX screen, to avoid decompressing background at every screen update
        public byte[] decodedBackground;
        public byte[] extPlxCache; // If this screen requires an external parallax, save it here
    }

    class Header
    {
        public string type
        {
            get { return new string(Data.Take(6).Select(b => (char)b).ToArray()); }
        }

        public ushort version
        {
            get { return Data.ToUInt16(6); }
            set { Data.WriteUInt16(6, value); }
        }

        public uint comp_length
        {
            get { return Data.ToUInt32(8); }
            set { Data.WriteUInt32(8, value); }
        }

        public string compression
        {
            get { return new string(Data.Skip(12).Take(4).Select(b => (char)b).ToArray()); }
        }

        public uint decomp_length
        {
            get { return Data.ToUInt32(16); }
            set { Data.WriteUInt32(16, value); }
        }

        public const int Size = 20;

        public Header(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; }
    }

    internal partial class Screen
    {
        private const int SCRNGRID_X = 16;
        private const int SCRNGRID_Y = 8;
        private const int SHRINK_BUFFER_SIZE = 50000;
        private const int RLE_BUFFER_SIZE = 50000;

        public const int FLASH_RED = 0;
        public const int FLASH_BLUE = 1;
        public const int BORDER_YELLOW = 2;
        public const int BORDER_GREEN = 3;
        public const int BORDER_PURPLE = 4;
        public const int BORDER_BLACK = 5;

        public const int SCREEN_WIDTH = 640;
        private const int SCREEN_DEPTH = 400;
        public const int SCREEN_LEFT_EDGE = 128;
        public const int SCREEN_RIGHT_EDGE = 128 + SCREEN_WIDTH - 1;
        public const int SCREEN_TOP_EDGE = 128;
        public const int SCREEN_BOTTOM_EDGE = 128 + SCREEN_DEPTH - 1;

        private const int SCROLL_FRACTION = 16;
        private const int MAX_SCROLL_DISTANCE = 8;
        private const int FADE_UP = 1;
        private const int FADE_DOWN = -1;

        private const int MAX_FORE = 20;
        private const int MAX_BACK = 20;
        private const int MAX_SORT = 20;

        private const int TYPE_FLOOR = 1;
        private const int TYPE_MOUSE = 2;
        private const int TYPE_SPRITE = 3;
        private const int TYPE_NON_MEGA = 4;
        public const int TYPE_MEGA = 5;
        public const int TYPE_PLAYER = 6;
        private const int TYPE_TEXT = 7;

        private const int STAT_MOUSE = 1;
        private const int STAT_LOGIC = 2;
        private const int STAT_EVENTS = 4;
        private const int STAT_FORE = 8;
        private const int STAT_BACK = 16;
        private const int STAT_SORT = 32;
        private const int STAT_SHRINK = 64;
        private const int STAT_BOOKMARK = 128;
        private const int STAT_TALK_WAIT = 256;
        private const int STAT_OVERRIDE = 512;


        private ISystem _system;
        private ResMan _resMan;
        private ObjectMan _objMan;
        private ushort _currentScreen;
        private byte[] _screenBuf;
        private bool _fullRefresh;
        private byte[] _screenGrid;
        private ByteAccess[] _layerBlocks = new ByteAccess[4];
        private byte[][] _parallax = new byte[2][];
        private byte[] _rleBuffer = new byte[RLE_BUFFER_SIZE];
        private byte[] _shrinkBuffer = new byte[SHRINK_BUFFER_SIZE];
        private bool _updatePalette;

        private uint[] _foreList = new uint[MAX_FORE];
        private uint[] _backList = new uint[MAX_BACK];
        private SortSpr[] _sortList = new SortSpr[MAX_SORT];
        private byte _foreLength, _backLength, _sortLength;
        private ushort _scrnSizeX, _scrnSizeY, _gridSizeX, _gridSizeY;
        private readonly PSXDataCache _psxCache; // Cache used for PSX backgrounds
        private string _directory;

        private UShortAccess[] _layerGrid = new UShortAccess[4];
        private ushort _oldScrollX;
        private ushort _oldScrollY;
        private bool _isBlack;
        private int _fadingStep;
        private sbyte _fadingDirection;
        private Color[] _currentPalette = new Color[256];
        private Color[] _targetPalette = new Color[256];
        private Text _textMan;


        public Text TextManager
        {
            get { return _textMan; }
            set { _textMan = value; }
        }

        public Screen(string directory, ISystem system, ResMan resMan, ObjectMan objMan)
        {
            _directory = directory;
            _system = system;
            _resMan = resMan;
            _objMan = objMan;
            _currentScreen = 0xFFFF;
            for (int i = 0; i < _sortList.Length; i++)
            {
                _sortList[i] = new SortSpr();
            }
            _psxCache = new PSXDataCache();
        }

        public void SetScrolling(short offsetX, short offsetY)
        {
            offsetX = (short)ScummHelper.Clip(offsetX, 0, (int)Logic.ScriptVars[(int)ScriptVariableNames.MAX_SCROLL_OFFSET_X]);
            offsetY = (short)ScummHelper.Clip(offsetY, 0, (int)Logic.ScriptVars[(int)ScriptVariableNames.MAX_SCROLL_OFFSET_Y]);

            if (Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_FLAG] == 2)
            { // first time on this screen - need absolute scroll immediately!
                _oldScrollX = (ushort)(Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X] = (uint)offsetX);
                _oldScrollY = (ushort)(Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y] = (uint)offsetY);
                Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_FLAG] = 1;
                _fullRefresh = true;
            }
            else if (Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_FLAG] == 1)
            {
                // Because parallax layers may be drawn on the old scroll offset, we
                // want a full refresh not only when the scroll offset changes, but
                // also on the frame where they become the same.
                if (_oldScrollX != Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X] || _oldScrollY != Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y])
                    _fullRefresh = true;
                _oldScrollX = (ushort)Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X];
                _oldScrollY = (ushort)Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y];
                int dx = (int)(offsetX - Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X]);
                int dy = (int)(offsetY - Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y]);
                int scrlDistX = ScummHelper.Clip((SCROLL_FRACTION - 1 + Math.Abs(dx)) / SCROLL_FRACTION * (dx > 0 ? 1 : -1), -MAX_SCROLL_DISTANCE, MAX_SCROLL_DISTANCE);
                int scrlDistY = ScummHelper.Clip((SCROLL_FRACTION - 1 + Math.Abs(dy)) / SCROLL_FRACTION * (dy > 0 ? 1 : -1), -MAX_SCROLL_DISTANCE, MAX_SCROLL_DISTANCE);
                if ((scrlDistX != 0) || (scrlDistY != 0))
                    _fullRefresh = true;
                Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X] = (uint)ScummHelper.Clip((int)(Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X] + scrlDistX), 0, (int)Logic.ScriptVars[(int)ScriptVariableNames.MAX_SCROLL_OFFSET_X]);
                Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y] = (uint)ScummHelper.Clip((int)(Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y] + scrlDistY), 0, (int)Logic.ScriptVars[(int)ScriptVariableNames.MAX_SCROLL_OFFSET_Y]);
            }
            else
            {
                // SCROLL_FLAG == 0, this usually means that the screen is smaller than 640x400 and doesn't need scrolling at all
                // however, it can also mean that the gamescript overwrote the scrolling flag to take care of scrolling directly,
                // (see bug report #1345130) so we ignore the offset arguments in this case
                Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X] = (uint)ScummHelper.Clip((int)Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X], 0, (int)Logic.ScriptVars[(int)ScriptVariableNames.MAX_SCROLL_OFFSET_X]);
                Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y] = (uint)ScummHelper.Clip((int)Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y], 0, (int)Logic.ScriptVars[(int)ScriptVariableNames.MAX_SCROLL_OFFSET_Y]);
                if ((Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X] != _oldScrollX) || (Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y] != _oldScrollY))
                {
                    _fullRefresh = true;
                    _oldScrollX = (ushort)Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X];
                    _oldScrollY = (ushort)Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y];
                }
            }
        }

        public void FadeDownPalette()
        {
            if (!_isBlack)
            { // don't fade down twice
                _fadingStep = 15;
                _fadingDirection = FADE_DOWN;
            }
        }

        public void FadeUpPalette()
        {
            _fadingStep = 1;
            _fadingDirection = FADE_UP;
        }

        public void FnSetPalette(byte start, ushort length, uint id, bool fadeUp)
        {
            var palData = _resMan.OpenFetchRes(id);
            if (start == 0) // force color 0 to black
                palData[0] = palData[1] = palData[2] = 0;

            if (SystemVars.Platform == Platform.Macintosh)
            {  // see bug #1701058
                if (start != 0 && start + length == 256) // and force color 255 to black as well
                    palData[(length - 1) * 3 + 0] = palData[(length - 1) * 3 + 1] = palData[(length - 1) * 3 + 2] = 0;
            }

            for (uint cnt = 0; cnt < length; cnt++)
            {
                _targetPalette[start + cnt] = Color.FromRgb(palData[cnt * 3 + 0] << 2, palData[cnt * 3 + 1] << 2, palData[cnt * 3 + 2] << 2);
            }
            _resMan.ResClose(id);
            _isBlack = false;
            if (fadeUp)
            {
                _fadingStep = 1;
                _fadingDirection = FADE_UP;
                Array.Clear(_currentPalette, 0, 256);
                _system.GraphicsManager.SetPalette(_currentPalette, 0, 256);
            }
            else
            {
                // TODO: check this
                _system.GraphicsManager.SetPalette(_targetPalette, start, length);
            }
        }

        public void FullRefresh()
        {
            _fullRefresh = true;
            _system.GraphicsManager.SetPalette(_targetPalette, 0, 256);
        }

        public bool StillFading()
        {
            return _fadingStep != 0;
        }

        public bool ShowScrollFrame()
        {
            if (!_fullRefresh || Logic.ScriptVars[(int)ScriptVariableNames.NEW_PALETTE] != 0 || _updatePalette)
                return false; // don't draw an additional frame if we aren't scrolling or have to change the palette
            if ((_oldScrollX == Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X]) &&
                    (_oldScrollY == Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y]))
                return false; // check again if we *really* are scrolling.

            ushort avgScrlX = (ushort)((_oldScrollX + Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X]) / 2);
            ushort avgScrlY = (ushort)((_oldScrollY + Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y]) / 2);

            _system.GraphicsManager.CopyRectToScreen(_screenBuf, avgScrlY * _scrnSizeX + avgScrlX, _scrnSizeX, 0, 40, SCREEN_WIDTH, SCREEN_DEPTH);
            _system.GraphicsManager.UpdateScreen();
            return true;
        }

        public void UpdateScreen()
        {
            if (Logic.ScriptVars[(int)ScriptVariableNames.NEW_PALETTE] != 0)
            {
                _fadingStep = 1;
                _fadingDirection = FADE_UP;
                _updatePalette = true;
                Logic.ScriptVars[(int)ScriptVariableNames.NEW_PALETTE] = 0;
            }
            if (_updatePalette)
            {
                FnSetPalette(0, 184, SwordRes.RoomDefTable[_currentScreen].palettes[0], false);
                FnSetPalette(184, 72, SwordRes.RoomDefTable[_currentScreen].palettes[1], false);
                _updatePalette = false;
            }
            if (_fadingStep != 0)
            {
                FadePalette();
                _system.GraphicsManager.SetPalette(_currentPalette, 0, 256);
            }

            ushort scrlX = (ushort)Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X];
            ushort scrlY = (ushort)Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y];
            if (_fullRefresh)
            {
                _fullRefresh = false;
                ushort copyWidth = SCREEN_WIDTH;
                ushort copyHeight = SCREEN_DEPTH;
                if (scrlX + copyWidth > _scrnSizeX)
                    copyWidth = (ushort)(_scrnSizeX - scrlX);
                if (scrlY + copyHeight > _scrnSizeY)
                    copyHeight = (ushort)(_scrnSizeY - scrlY);
                _system.GraphicsManager.CopyRectToScreen(_screenBuf, scrlY * _scrnSizeX + scrlX, _scrnSizeX, 0, 40, copyWidth, copyHeight);
            }
            else
            {
                // partial screen update only. The screen coordinates probably won't fit to the
                // grid holding the informations on which blocks have to be updated.
                // as the grid will be X pixel higher and Y pixel more to the left, this can be cured
                // by first checking the top border, then the left column and then the remaining (aligned) part.
                var gridPos = scrlX / SCRNGRID_X + scrlY / SCRNGRID_Y * _gridSizeX;
                var scrnBuf = scrlY * _scrnSizeX + scrlX;
                byte diffX = (byte)(scrlX % SCRNGRID_X);
                byte diffY = (byte)(scrlY % SCRNGRID_Y);
                ushort gridW = SCREEN_WIDTH / SCRNGRID_X;
                ushort gridH = SCREEN_DEPTH / SCRNGRID_Y;
                if (diffY != 0)
                {
                    diffY = (byte)(SCRNGRID_Y - diffY);
                    ushort cpWidth = 0;
                    for (ushort cntx = 0; cntx < gridW; cntx++)
                        if (_screenGrid[gridPos + cntx] != 0)
                        {
                            _screenGrid[gridPos + cntx] >>= 1;
                            cpWidth++;
                        }
                        else if (cpWidth != 0)
                        {
                            short xPos = (short)((cntx - cpWidth) * SCRNGRID_X - diffX);
                            if (xPos < 0)
                                xPos = 0;
                            _system.GraphicsManager.CopyRectToScreen(_screenBuf, scrnBuf + xPos, _scrnSizeX, xPos, 40, cpWidth * SCRNGRID_X, diffY);
                            cpWidth = 0;
                        }
                    if (cpWidth != 0)
                    {
                        short xPos = (short)((gridW - cpWidth) * SCRNGRID_X - diffX);
                        if (xPos < 0)
                            xPos = 0;
                        _system.GraphicsManager.CopyRectToScreen(_screenBuf, scrnBuf + xPos, _scrnSizeX, xPos, 40, SCREEN_WIDTH - xPos, diffY);
                    }
                    scrlY += diffY;
                }
                // okay, y scrolling is compensated. check x now.
                gridPos = scrlX / SCRNGRID_X + scrlY / SCRNGRID_Y * _gridSizeX;
                scrnBuf = scrlY * _scrnSizeX + scrlX;
                if (diffX != 0)
                {
                    diffX = (byte)(SCRNGRID_X - diffX);
                    ushort cpHeight = 0;
                    for (ushort cnty = 0; cnty < gridH; cnty++)
                    {
                        if (_screenGrid[gridPos] != 0)
                        {
                            _screenGrid[gridPos] >>= 1;
                            cpHeight++;
                        }
                        else if (cpHeight != 0)
                        {
                            ushort yPos = (ushort)((cnty - cpHeight) * SCRNGRID_Y);
                            _system.GraphicsManager.CopyRectToScreen(_screenBuf, scrnBuf + yPos * _scrnSizeX, _scrnSizeX, 0, yPos + diffY + 40, diffX, cpHeight * SCRNGRID_Y);
                            cpHeight = 0;
                        }
                        gridPos += _gridSizeX;
                    }
                    if (cpHeight != 0)
                    {
                        ushort yPos = (ushort)((gridH - cpHeight) * SCRNGRID_Y);
                        _system.GraphicsManager.CopyRectToScreen(_screenBuf, scrnBuf + yPos * _scrnSizeX, _scrnSizeX, 0, yPos + diffY + 40, diffX, SCREEN_DEPTH - (yPos + diffY));
                    }
                    scrlX += diffX;
                }
                // x scroll is compensated, too. check the rest of the screen, now.
                scrnBuf = scrlY * _scrnSizeX + scrlX;
                gridPos = scrlX / SCRNGRID_X + scrlY / SCRNGRID_Y * _gridSizeX;
                for (ushort cnty = 0; cnty < gridH; cnty++)
                {
                    ushort cpWidth = 0;
                    ushort cpHeight = SCRNGRID_Y;
                    if (cnty == gridH - 1)
                        cpHeight = (ushort)(SCRNGRID_Y - diffY);
                    for (ushort cntx = 0; cntx < gridW; cntx++)
                        if (_screenGrid[gridPos + cntx] != 0)
                        {
                            _screenGrid[gridPos + cntx] >>= 1;
                            cpWidth++;
                        }
                        else if (cpWidth != 0)
                        {
                            _system.GraphicsManager.CopyRectToScreen(_screenBuf, scrnBuf + (cntx - cpWidth) * SCRNGRID_X, _scrnSizeX, (cntx - cpWidth) * SCRNGRID_X + diffX, cnty * SCRNGRID_Y + diffY + 40, cpWidth * SCRNGRID_X, cpHeight);
                            cpWidth = 0;
                        }
                    if (cpWidth != 0)
                    {
                        ushort xPos = (ushort)((gridW - cpWidth) * SCRNGRID_X);
                        _system.GraphicsManager.CopyRectToScreen(_screenBuf, scrnBuf + xPos, _scrnSizeX, xPos + diffX, cnty * SCRNGRID_Y + diffY + 40, SCREEN_WIDTH - (xPos + diffX), cpHeight);
                    }
                    gridPos += _gridSizeX;
                    scrnBuf += _scrnSizeX * SCRNGRID_Y;
                }
            }
            _system.GraphicsManager.UpdateScreen();
        }

        public void NewScreen(uint screen)
        {
            // set sizes and scrolling, initialize/load screengrid, force screen refresh
            _currentScreen = (ushort)screen;
            _scrnSizeX = (ushort)SwordRes.RoomDefTable[screen].sizeX;
            _scrnSizeY = (ushort)SwordRes.RoomDefTable[screen].sizeY;
            _gridSizeX = (ushort)(_scrnSizeX / SCRNGRID_X);
            _gridSizeY = (ushort)(_scrnSizeY / SCRNGRID_Y);
            if (_scrnSizeX % SCRNGRID_X != 0 || _scrnSizeY % SCRNGRID_Y != 0)
                throw new InvalidOperationException($"Illegal screensize: {screen}: {_scrnSizeX}/{_scrnSizeY}");
            if ((_scrnSizeX > SCREEN_WIDTH) || (_scrnSizeY > SCREEN_DEPTH))
            {
                Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_FLAG] = 2;
                Logic.ScriptVars[(int)ScriptVariableNames.MAX_SCROLL_OFFSET_X] = (uint)(_scrnSizeX - SCREEN_WIDTH);
                Logic.ScriptVars[(int)ScriptVariableNames.MAX_SCROLL_OFFSET_Y] = (uint)(_scrnSizeY - SCREEN_DEPTH);
            }
            else
            {
                Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_FLAG] = 0;
                Logic.ScriptVars[(int)ScriptVariableNames.MAX_SCROLL_OFFSET_X] = 0;
                Logic.ScriptVars[(int)ScriptVariableNames.MAX_SCROLL_OFFSET_Y] = 0;
            }
            Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X] = 0;
            Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y] = 0;

            if (SystemVars.Platform == Platform.PSX)
                FlushPsxCache();

            _screenBuf = new byte[_scrnSizeX * _scrnSizeY];
            _screenGrid = new byte[_gridSizeX * _gridSizeY];

            for (var cnt = 0; cnt < SwordRes.RoomDefTable[_currentScreen].totalLayers; cnt++)
            {
                // open and lock all resources, will be closed in quitScreen()
                _layerBlocks[cnt] = new ByteAccess(_resMan.OpenFetchRes(SwordRes.RoomDefTable[_currentScreen].layers[cnt]));
                if (cnt > 0)
                    _layerBlocks[cnt].Offset += Header.Size;
            }
            for (var cnt = 0; cnt < SwordRes.RoomDefTable[_currentScreen].totalLayers - 1; cnt++)
            {
                // there's no grid for the background layer, so it's totalLayers - 1
                _layerGrid[cnt] = new UShortAccess(_resMan.OpenFetchRes(SwordRes.RoomDefTable[_currentScreen].grids[cnt]));
                _layerGrid[cnt].Data.Offset += 14 * 2;
            }
            _parallax[0] = _parallax[1] = null;
            if (SwordRes.RoomDefTable[_currentScreen].parallax[0] != 0)
                _parallax[0] = _resMan.OpenFetchRes(SwordRes.RoomDefTable[_currentScreen].parallax[0]);
            if (SwordRes.RoomDefTable[_currentScreen].parallax[1] != 0)
                _parallax[1] = _resMan.OpenFetchRes(SwordRes.RoomDefTable[_currentScreen].parallax[1]);

            _updatePalette = true;
            _fullRefresh = true;
        }

        public void QuitScreen()
        {
            byte cnt;
            if (SystemVars.Platform == Platform.PSX)
                FlushPsxCache();
            for (cnt = 0; cnt < SwordRes.RoomDefTable[_currentScreen].totalLayers; cnt++)
                _resMan.ResClose(SwordRes.RoomDefTable[_currentScreen].layers[cnt]);
            for (cnt = 0; cnt < SwordRes.RoomDefTable[_currentScreen].totalLayers - 1; cnt++)
                _resMan.ResClose(SwordRes.RoomDefTable[_currentScreen].grids[cnt]);
            if (SwordRes.RoomDefTable[_currentScreen].parallax[0] != 0)
                _resMan.ResClose(SwordRes.RoomDefTable[_currentScreen].parallax[0]);
            if (SwordRes.RoomDefTable[_currentScreen].parallax[1] != 0)
                _resMan.ResClose(SwordRes.RoomDefTable[_currentScreen].parallax[1]);
            _currentScreen = 0xFFFF;
        }

        public void Draw()
        {
            byte cnt;

            Debug(8, "Screen::draw() . _currentScreen {_currentScreen}");

            if (_currentScreen == 54)
            {
                // rm54 has a BACKGROUND parallax layer in parallax[0]
                if (_parallax[0] != null && SystemVars.Platform != Platform.PSX) //Avoid drawing this parallax on PSX edition, it gets occluded by background
                    RenderParallax(_parallax[0]);
                var src = _layerBlocks[0];
                var dest = 0;

                if (SystemVars.Platform == Platform.PSX)
                {
                    if (_psxCache.decodedBackground == null)
                        _psxCache.decodedBackground = PsxShrinkedBackgroundToIndexed(_layerBlocks[0].Data, _layerBlocks[0].Offset, _scrnSizeX, _scrnSizeY);
                    Array.Copy(_psxCache.decodedBackground, _screenBuf, _scrnSizeX * _scrnSizeY);
                }
                else
                {
                    ushort scrnScrlY = (ushort)Math.Min((uint)_oldScrollY, Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y]);
                    ushort scrnHeight = (ushort)(SCREEN_DEPTH + Math.Abs((int)_oldScrollY - (int)Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y]));

                    src.Offset += scrnScrlY * _scrnSizeX;
                    dest += scrnScrlY * _scrnSizeX;

                    // In this background to create transparency we have to iterate through all pixels, avoid checking those out of screen
                    for (ushort cnty = scrnScrlY; (cnty < _scrnSizeY) && (cnty < scrnHeight + scrnScrlY); cnty++)
                        for (ushort cntx = 0; cntx < _scrnSizeX; cntx++)
                        {
                            if (src[0] != 0)
                                if (SystemVars.Platform != Platform.Macintosh || src[0] != 255) // see bug #1701058
                                    _screenBuf[dest] = src[0];
                            src.Offset++;
                            dest++;
                        }
                }

            }
            else if (SystemVars.Platform != Platform.PSX)
            {
                Array.Copy(_layerBlocks[0].Data, _layerBlocks[0].Offset, _screenBuf, 0, _scrnSizeX * _scrnSizeY);
            }
            else
            {
                // We are using PSX version
                if (_currentScreen == 45 || _currentScreen == 55 ||
                        _currentScreen == 57 || _currentScreen == 63 || _currentScreen == 71)
                {
                    // Width shrinked backgrounds
                    if (_psxCache.decodedBackground == null)
                        _psxCache.decodedBackground = PsxShrinkedBackgroundToIndexed(_layerBlocks[0].Data, _layerBlocks[0].Offset, _scrnSizeX, _scrnSizeY);
                }
                else
                {
                    if (_psxCache.decodedBackground == null)
                        _psxCache.decodedBackground = PsxBackgroundToIndexed(_layerBlocks[0].Data, _layerBlocks[0].Offset, _scrnSizeX, _scrnSizeY);
                }
                Array.Copy(_psxCache.decodedBackground, _screenBuf, _scrnSizeX * _scrnSizeY);
            }

            for (cnt = 0; cnt < _backLength; cnt++)
                ProcessImage(_backList[cnt]);

            for (cnt = 0; cnt < _sortLength - 1; cnt++)
                for (byte sCnt = 0; sCnt < _sortLength - 1; sCnt++)
                    if (_sortList[sCnt].y > _sortList[sCnt + 1].y)
                    {
                        ScummHelper.Swap(ref _sortList[sCnt], ref _sortList[sCnt + 1]);
                    }

            for (cnt = 0; cnt < _sortLength; cnt++)
                ProcessImage((uint)_sortList[cnt].id);

            if ((_currentScreen != 54) && _parallax[0] != null)
                RenderParallax(_parallax[0]); // screens other than 54 have FOREGROUND parallax layer in parallax[0]
            if (_parallax[1] != null)
                RenderParallax(_parallax[1]);

            // PSX version has parallax layer for this room in an external file (TRAIN.PLX)
            if (SystemVars.Platform == Platform.PSX && _currentScreen == 63)
            {
                // FIXME: this should be handled in a cleaner way...
                if (_psxCache.extPlxCache == null)
                {
                    var path = ScummHelper.LocatePath(_directory, "TRAIN.PLX");
                    using (var parallax = new BinaryReader(ServiceLocator.FileStorage.OpenFileRead(path)))
                    {
                        _psxCache.extPlxCache = parallax.ReadBytes((int)parallax.BaseStream.Length);
                    }
                }
                RenderParallax(_psxCache.extPlxCache);
            }

            for (cnt = 0; cnt < _foreLength; cnt++)
                ProcessImage(_foreList[cnt]);

            _backLength = _sortLength = _foreLength = 0;
        }

        public void ClearScreen()
        {
            if (_screenBuf != null)
            {
                _fullRefresh = true;
                Array.Clear(_screenBuf, 0, _scrnSizeX * _scrnSizeY);
                _system.GraphicsManager.FillScreen(0);
            }
        }

        public void AddToGraphicList(byte listId, uint objId)
        {
            if (listId == 0)
            {
                System.Diagnostics.Debug.Assert(_foreLength < MAX_FORE);
                _foreList[_foreLength++] = objId;
            }
            if (listId == 1)
            {
                System.Diagnostics.Debug.Assert(_sortLength < MAX_SORT);
                var cpt = _objMan.FetchObject(objId);
                _sortList[_sortLength].id = (int)objId;
                _sortList[_sortLength].y = cpt.anim_y; // gives feet coords if boxed mega, otherwise top of sprite box
                if ((cpt.status & STAT_SHRINK) == 0)
                {     // not a boxed mega using shrinking
                    Header frameRaw = new Header(_resMan.OpenFetchRes((uint)cpt.resource));
                    FrameHeader frameHead = new FrameHeader(_resMan.FetchFrame(frameRaw.Data, (uint)cpt.frame));
                    _sortList[_sortLength].y += _resMan.ReadUInt16(frameHead.height) - 1; // now pointing to base of sprite
                    _resMan.ResClose((uint)cpt.resource);
                }
                _sortLength++;
            }
            if (listId == 2)
            {
                System.Diagnostics.Debug.Assert(_backLength < MAX_BACK);
                _backList[_backLength++] = objId;
            }
        }

        public void FnSetParallax(uint screen, uint resId)
        {
            SwordRes.RoomDefTable[screen].parallax[0] = resId;
        }

        public void FnFlash(byte color)
        {
            Warning($"stub: Screen::fnFlash({color})");
        }

        public void ShowFrame(ushort x, ushort y, uint resId, uint frameNo, byte[] fadeMask = null, sbyte fadeStatus = 0)
        {
            byte[] frame = new byte[40 * 40];
            int i, j;

            // PSX top menu is black
            if (SystemVars.Platform != Platform.PSX)
            {
                // Dark gray background
                frame.Set(0, 199, frame.Length);
            }

            if (resId != 0xffffffff)
            {
                FrameHeader frameHead = new FrameHeader(_resMan.FetchFrame(_resMan.OpenFetchRes(resId), frameNo));
                var frameData = new ByteAccess(frameHead.Data.Data, frameHead.Data.Offset + FrameHeader.Size);

                if (SystemVars.Platform == Platform.PSX)
                {
                    //We need to decompress PSX frames
                    var frameBufferPSX = new byte[_resMan.ReadUInt16(frameHead.width) * _resMan.ReadUInt16(frameHead.height) / 2];
                    DecompressHIF(frameData.Data, frameData.Offset, frameBufferPSX);

                    for (i = 0; i < _resMan.ReadUInt16(frameHead.height) / 2; i++)
                    {
                        for (j = 0; j < _resMan.ReadUInt16(frameHead.width); j++)
                        {
                            var data = frameBufferPSX[i * _resMan.ReadUInt16(frameHead.width) + j];
                            frame[(i * 2 + 4) * 40 + j + 2] = data;
                            frame[(i * 2 + 1 + 4) * 40 + j + 2] = data; //Linedoubling the sprite
                        }
                    }
                }
                else
                {
                    for (i = 0; i < _resMan.ReadUInt16(frameHead.height); i++)
                        for (j = 0; j < _resMan.ReadUInt16(frameHead.height); j++)
                            frame[(i + 4) * 40 + j + 2] = frameData[i * _resMan.ReadUInt16(frameHead.width) + j];
                }

                _resMan.ResClose(resId);
            }

            if (fadeMask != null)
            {
                for (i = 0; i < 40; i++)
                {
                    for (j = 0; j < 40; j++)
                    {
                        if (fadeMask[((i % 8) * 8) + (j % 8)] >= fadeStatus)
                            frame[i * 40 + j] = 0;
                    }
                }
            }

            _system.GraphicsManager.CopyRectToScreen(frame, 40, x, y, 40, 40);
        }

        public static void DecompressHIF(byte[] src, int srcOff, byte[] dest)
        {
            var dstOff = 0;
            for (;;)
            { //Main loop
                byte controlByte = src[srcOff++];
                uint byteCount = 0;
                while (byteCount < 8)
                {
                    if ((controlByte & 0x80) != 0)
                    {
                        ushort infoWord = src.ToUInt16BigEndian(srcOff); //Read the info word
                        srcOff += 2;
                        if (infoWord == 0xFFFF) return; //Got 0xFFFF code, finished.

                        int repeatCount = (infoWord >> 12) + 2; //How many time data needs to be refetched
                        while (repeatCount >= 0)
                        {
                            var oldDataSrc = dstOff - ((infoWord & 0xFFF) + 1);
                            dest[dstOff++] = dest[oldDataSrc];
                            repeatCount--;
                        }
                    }
                    else
                        dest[dstOff++] = src[srcOff++];
                    byteCount++;
                    controlByte <<= 1; //Shifting left the control code one bit
                }
            }
        }


        private void ProcessImage(uint id)
        {
            FrameHeader frameHead;
            int scale;

            var compact = _objMan.FetchObject(id);

            if (compact.type == TYPE_TEXT)
                frameHead = new FrameHeader(_textMan.GiveSpriteData((byte)compact.target));
            else
                frameHead = new FrameHeader(_resMan.FetchFrame(_resMan.OpenFetchRes((uint)compact.resource), (uint)compact.frame));

            ushort spriteX = (ushort)compact.anim_x;
            ushort spriteY = (ushort)compact.anim_y;


            if ((compact.status & STAT_SHRINK) != 0)
            {
                scale = (compact.scale_a * compact.ycoord + compact.scale_b) / 256;
                spriteX = (ushort)(spriteX + (_resMan.ReadInt16(frameHead.offsetX) * scale) / 256);
                spriteY = (ushort)(spriteY + (_resMan.ReadInt16(frameHead.offsetY) * scale) / 256);
            }
            else
            {
                scale = 256;
                spriteX = (ushort)(spriteX + _resMan.ReadInt16(frameHead.offsetX));
                spriteY = (ushort)(spriteY + _resMan.ReadInt16(frameHead.offsetY));
            }

            var sprData = new ByteAccess(frameHead.Data.Data, frameHead.Data.Offset + FrameHeader.Size);
            if (SystemVars.Platform == Platform.PSX && compact.type != TYPE_TEXT)
            {
                // PSX sprites are compressed with HIF
                var hifBuf = new byte[_resMan.ReadUInt16(frameHead.width) * _resMan.ReadUInt16(frameHead.height) / 2];
                DecompressHIF(sprData.Data, sprData.Offset, hifBuf);
                sprData = new ByteAccess(hifBuf);
            }
            else if (frameHead.runTimeComp[3] == '7')
            { // RLE7 encoded?
                DecompressRLE7(sprData.Data, sprData.Offset, _resMan.ReadUInt32(frameHead.compSize), _rleBuffer);
                sprData = new ByteAccess(_rleBuffer);
            }
            else if (frameHead.runTimeComp[3] == '0')
            { // RLE0 encoded?
                DecompressRLE0(sprData.Data, sprData.Offset, _resMan.ReadUInt32(frameHead.compSize), _rleBuffer);
                sprData = new ByteAccess(_rleBuffer);
            }
            else if (frameHead.runTimeComp[1] == 'I')
            { // new type
                var tonyBuf = new byte[_resMan.ReadUInt32(frameHead.width) * _resMan.ReadUInt16(frameHead.height)];
                DecompressTony(sprData.Data, sprData.Offset, _resMan.ReadUInt32(frameHead.compSize), tonyBuf);
                sprData = new ByteAccess(tonyBuf);
            }

            ushort sprSizeX, sprSizeY;
            if ((compact.status & STAT_SHRINK) != 0)
            {
                //Clean shrink buffer to avoid corruption
                Array.Clear(_shrinkBuffer, 0, SHRINK_BUFFER_SIZE);
                if (SystemVars.Platform == Platform.PSX && (compact.resource != SwordRes.GEORGE_MEGA))
                {
                    // PSX Height shrinked sprites
                    sprSizeX = (ushort)((scale * _resMan.ReadUInt16(frameHead.width)) / 256);
                    sprSizeY = (ushort)((scale * (_resMan.ReadUInt16(frameHead.height))) / 256 / 2);
                    FastShrink(sprData, _resMan.ReadUInt16(frameHead.width), (ushort)((_resMan.ReadUInt16(frameHead.height)) / 2), (uint)scale, _shrinkBuffer);
                }
                else if (SystemVars.Platform == Platform.PSX)
                {
                    // PSX width/height shrinked sprites
                    sprSizeX = (ushort)((scale * _resMan.ReadUInt16(frameHead.width)) / 256 / 2);
                    sprSizeY = (ushort)((scale * _resMan.ReadUInt16(frameHead.height)) / 256 / 2);
                    FastShrink(sprData, (ushort)(_resMan.ReadUInt16(frameHead.width) / 2), (ushort)(_resMan.ReadUInt16(frameHead.height) / 2), (uint)scale, _shrinkBuffer);
                }
                else
                {
                    sprSizeX = (ushort)(scale * _resMan.ReadUInt16(frameHead.width) / 256);
                    sprSizeY = (ushort)(scale * _resMan.ReadUInt16(frameHead.height) / 256);
                    FastShrink(sprData, _resMan.ReadUInt16(frameHead.width), _resMan.ReadUInt16(frameHead.height), (uint)scale, _shrinkBuffer);
                }
                sprData = new ByteAccess(_shrinkBuffer);
            }
            else
            {
                sprSizeX = _resMan.ReadUInt16(frameHead.width);
                if (SystemVars.Platform == Platform.PSX)
                {
                    // PSX sprites are half height
                    sprSizeY = (ushort)(_resMan.ReadUInt16(frameHead.height) / 2);
                }
                else
                    sprSizeY = _resMan.ReadUInt16(frameHead.height);
            }

            if ((compact.status & STAT_OVERRIDE) == 0)
            {
                //mouse size linked to exact size & coordinates of sprite box - shrink friendly
                if (_resMan.ReadUInt16((ushort)frameHead.offsetX) != 0 || _resMan.ReadUInt16((ushort)frameHead.offsetY) != 0)
                {
                    //for megas the mouse area is reduced to account for sprite not
                    //filling the box size is reduced to 1/2 width, 4/5 height
                    compact.mouse_x1 = spriteX + sprSizeX / 4;
                    compact.mouse_x2 = spriteX + 3 * sprSizeX / 4;
                    compact.mouse_y1 = spriteY + sprSizeY / 10;
                    compact.mouse_y2 = spriteY + 9 * sprSizeY / 10;
                }
                else
                {
                    compact.mouse_x1 = spriteX;
                    compact.mouse_x2 = spriteX + sprSizeX;
                    compact.mouse_y1 = spriteY;
                    compact.mouse_y2 = spriteY + sprSizeY;
                }
            }

            ushort sprPitch = sprSizeX;
            ushort incr;
            SpriteClipAndSet(ref spriteX, ref spriteY, ref sprSizeX, ref sprSizeY, out incr);

            if ((sprSizeX > 0) && (sprSizeY > 0))
            {
                if (SystemVars.Platform != Platform.PSX || (compact.type == TYPE_TEXT)
                    || (compact.resource == SwordRes.LVSFLY) || (compact.resource != SwordRes.GEORGE_MEGA && (sprSizeX < 260)))
                {
                    sprData.Offset += incr;
                    DrawSprite(sprData, spriteX, spriteY, sprSizeX, sprSizeY, sprPitch);
                }
                else if (((sprSizeX >= 260) && (sprSizeX < 450)) || ((compact.resource == SwordRes.GMWRITH) && (sprSizeX < 515))
                         // a psx shrinked sprite (1/2 width)
                         || ((compact.resource == SwordRes.GMPOWER) && (sprSizeX < 515)))
                {
                    // some needs to be hardcoded, headers don't give useful infos
                    DrawPsxHalfShrinkedSprite(sprData.Data, sprData.Offset + incr, spriteX, spriteY, (ushort)(sprSizeX / 2), sprSizeY, (ushort)(sprPitch / 2));
                }
                else if (sprSizeX >= 450) // A PSX double shrinked sprite (1/3 width)
                {
                    DrawPsxFullShrinkedSprite(sprData.Data, sprData.Offset + incr, spriteX, spriteY, (ushort)(sprSizeX / 3), sprSizeY, (ushort)(sprPitch / 3));
                }
                else // This is for psx half shrinked, walking george and remaining sprites
                {
                    DrawPsxHalfShrinkedSprite(sprData.Data, sprData.Offset + incr, spriteX, spriteY, sprSizeX, sprSizeY, sprPitch);
                }
                if ((compact.status & STAT_FORE) == 0 &&
                    !(SystemVars.Platform == Platform.PSX && (compact.resource == SwordRes.MOUBUSY)))
                // Check fixes moue sprite being masked by layer, happens only on psx
                {
                    VerticalMask(spriteX, spriteY, sprSizeX, sprSizeY);
                }
            }

            if (compact.type != TYPE_TEXT)
                _resMan.ResClose((uint)compact.resource);
        }

        private void DrawSprite(ByteAccess sprData, ushort sprX, ushort sprY, ushort sprWidth, ushort sprHeight, ushort sprPitch)
        {
            var dest = new ByteAccess(_screenBuf, sprY * _scrnSizeX + sprX);

            for (ushort cnty = 0; cnty < sprHeight; cnty++)
            {
                for (ushort cntx = 0; cntx < sprWidth; cntx++)
                    if (sprData[cntx] != 0)
                        dest[cntx] = sprData[cntx];

                if (SystemVars.Platform == Platform.PSX)
                { //On PSX version we need to double horizontal lines
                    dest.Offset += _scrnSizeX;
                    for (ushort cntx = 0; cntx < sprWidth; cntx++)
                        if (sprData[cntx] != 0)
                            dest[cntx] = sprData[cntx];
                }

                sprData.Offset += sprPitch;
                dest.Offset += _scrnSizeX;
            }
        }

        private void VerticalMask(ushort x, ushort y, ushort bWidth, ushort bHeight)
        {
            if (SwordRes.RoomDefTable[_currentScreen].totalLayers <= 1)
                return;

            if (SystemVars.Platform == Platform.PSX)
            { // PSX sprites are vertical shrinked, and some width shrinked
                bHeight *= 2;
                bWidth *= 2;
            }

            bWidth = (ushort)((bWidth + (x & (SCRNGRID_X - 1)) + (SCRNGRID_X - 1)) / SCRNGRID_X);
            bHeight = (ushort)((bHeight + (y & (SCRNGRID_Y - 1)) + (SCRNGRID_Y - 1)) / SCRNGRID_Y);

            x /= SCRNGRID_X;
            y /= SCRNGRID_Y;
            if (x + bWidth > _gridSizeX)
                bWidth = (ushort)(_gridSizeX - x);
            if (y + bHeight > _gridSizeY)
                bHeight = (ushort)(_gridSizeY - y);

            ushort gridY = (ushort)(y + SCREEN_TOP_EDGE / SCRNGRID_Y); // imaginary screen on top
            gridY = (ushort)(gridY + bHeight - 1); // we start from the bottom edge
            ushort gridX = (ushort)(x + SCREEN_LEFT_EDGE / SCRNGRID_X); // imaginary screen left
            ushort lGridSizeX = (ushort)(_gridSizeX + 2 * (SCREEN_LEFT_EDGE / SCRNGRID_X)); // width of the grid for the imaginary screen

            for (ushort blkx = 0; blkx < bWidth; blkx++)
            {
                // A sprite can be masked by several layers at the same time,
                // so we have to check them all. See bug #917427.
                for (short level = (short)(SwordRes.RoomDefTable[_currentScreen].totalLayers - 2); level >= 0; level--)
                {
                    if (_layerGrid[level][gridX + blkx + gridY * lGridSizeX] != 0)
                    {
                        var grid = new UShortAccess(_layerGrid[level].Data, _layerGrid[level].Data.Offset + (gridX + blkx + gridY * lGridSizeX) * 2);
                        for (short blky = (short)(bHeight - 1); blky >= 0; blky--)
                        {
                            if (grid[0] != 0)
                            {
                                ByteAccess blkData;
                                if (SystemVars.Platform == Platform.PSX)
                                    blkData = new ByteAccess(_layerBlocks[level + 1].Data, _layerBlocks[level + 1].Offset + (_resMan.ReadUInt16(grid[0]) - 1) * 64); //PSX layers are half height too...
                                else
                                    blkData = new ByteAccess(_layerBlocks[level + 1].Data, _layerBlocks[level + 1].Offset + (_resMan.ReadUInt16(grid[0]) - 1) * 128);
                                BlitBlockClear((ushort)(x + blkx), (ushort)(y + blky), blkData);
                            }
                            else
                                break;
                            grid.Data.Offset -= lGridSizeX * 2;
                        }
                    }
                }
            }
        }

        private void BlitBlockClear(ushort x, ushort y, ByteAccess data)
        {
            var dest = y * SCRNGRID_Y * _scrnSizeX + x * SCRNGRID_X;

            for (byte cnty = 0; cnty < (SystemVars.Platform == Platform.PSX ? SCRNGRID_Y / 2 : SCRNGRID_Y); cnty++)
            {
                for (byte cntx = 0; cntx < SCRNGRID_X; cntx++)
                    if (data[cntx] != 0)
                        _screenBuf[dest + cntx] = data[cntx];

                if (SystemVars.Platform == Platform.PSX)
                {
                    dest += _scrnSizeX;
                    for (byte cntx = 0; cntx < SCRNGRID_X; cntx++)
                        if (data[cntx] != 0)
                            _screenBuf[dest + cntx] = data[cntx];
                }

                data.Offset += SCRNGRID_X;
                dest += _scrnSizeX;
            }
        }

        private void SpriteClipAndSet(ref ushort pSprX, ref ushort pSprY, ref ushort pSprWidth, ref ushort pSprHeight, out ushort incr)
        {
            short sprX = (short)(pSprX - SCREEN_LEFT_EDGE);
            short sprY = (short)(pSprY - SCREEN_TOP_EDGE);
            short sprW = (short)pSprWidth;
            short sprH = (short)pSprHeight;

            if (sprY < 0)
            {
                incr = (ushort)((-sprY) * sprW);
                sprH += sprY;
                sprY = 0;
            }
            else
                incr = 0;
            if (sprX < 0)
            {
                incr = (ushort)(incr - sprX);
                sprW += sprX;
                sprX = 0;
            }

            if (sprY + sprH > _scrnSizeY)
                sprH = (short)(_scrnSizeY - sprY);
            if (sprX + sprW > _scrnSizeX)
                sprW = (short)(_scrnSizeX - sprX);

            if (sprH < 0)
                pSprHeight = 0;
            else
                pSprHeight = (ushort)sprH;
            if (sprW < 0)
                pSprWidth = 0;
            else
                pSprWidth = (ushort)sprW;

            pSprX = (ushort)sprX;
            pSprY = (ushort)sprY;

            if (pSprWidth != 0 && pSprHeight != 0)
            {
                // sprite will be drawn, so mark it in the grid buffer
                ushort gridH = (ushort)((pSprHeight + (sprY & (SCRNGRID_Y - 1)) + (SCRNGRID_Y - 1)) / SCRNGRID_Y);
                ushort gridW = (ushort)((pSprWidth + (sprX & (SCRNGRID_X - 1)) + (SCRNGRID_X - 1)) / SCRNGRID_X);

                if (SystemVars.Platform == Platform.PSX)
                {
                    gridH *= 2; // This will correct the PSX sprite being cut at half height
                    gridW *= 2; // and masking problems when sprites are stretched in width

                    ushort bottomSprPos = (ushort)(pSprY + (pSprHeight) * 2); //Position of bottom line of sprite
                    if (bottomSprPos > _scrnSizeY)
                    { //Check that resized psx sprite isn't drawn outside of screen boundaries
                        ushort outScreen = (ushort)(bottomSprPos - _scrnSizeY);
                        pSprHeight = (ushort)(outScreen % 2 != 0 ? pSprHeight - (outScreen + 1) / 2 : pSprHeight - outScreen / 2);
                    }

                }

                ushort gridX = (ushort)(sprX / SCRNGRID_X);
                ushort gridY = (ushort)(sprY / SCRNGRID_Y);
                var gridBuf = gridX + gridY * _gridSizeX;
                if (gridX + gridW > _gridSizeX)
                    gridW = (ushort)(_gridSizeX - gridX);
                if (gridY + gridH > _gridSizeY)
                    gridH = (ushort)(_gridSizeY - gridY);

                for (ushort cnty = 0; cnty < gridH; cnty++)
                {
                    for (ushort cntx = 0; cntx < gridW; cntx++)
                        _screenGrid[gridBuf + cntx] = 2;
                    gridBuf += _gridSizeX;
                }
            }
        }

        private void FastShrink(ByteAccess src, ushort width, ushort height, uint scale, byte[] dest)
        {
            uint resHeight = (height * scale) >> 8;
            uint resWidth = (width * scale) >> 8;
            uint step = 0x10000 / scale;
            byte[] columnTab = new byte[160];
            uint res = step >> 1;

            for (ushort cnt = 0; cnt < resWidth; cnt++)
            {
                columnTab[cnt] = (byte)(res >> 8);
                res += step;
            }

            uint newRow = step >> 1;
            uint oldRow = 0;

            var destPos = 0;
            var srcPos = 0;
            ushort lnCnt;
            for (lnCnt = 0; lnCnt < resHeight; lnCnt++)
            {
                while (oldRow < (newRow >> 8))
                {
                    oldRow++;
                    srcPos += width;
                }
                for (ushort colCnt = 0; colCnt < resWidth; colCnt++)
                {
                    dest[destPos++] = src[srcPos + columnTab[colCnt]];
                }
                newRow += step;
            }
            // scaled, now stipple shadows if there are any
            for (lnCnt = 0; lnCnt < resHeight; lnCnt++)
            {
                ushort xCnt = (ushort)(lnCnt & 1);
                destPos = (int)(lnCnt * resWidth + (lnCnt & 1));
                while (xCnt < resWidth)
                {
                    if (dest[destPos] == 200)
                        dest[destPos] = 0;
                    destPos += 2;
                    xCnt += 2;
                }
            }
        }

        private void DecompressTony(byte[] src, int srcOffset, uint compSize, byte[] dest)
        {
            var endOfData = compSize;
            var srcPos = 0;
            var dstPos = 0;
            while (srcPos < endOfData)
            {
                byte numFlat = src[srcOffset + srcPos++];
                if (numFlat != 0)
                {
                    dest.Set(dstPos, src[srcOffset + srcPos], numFlat);
                    srcPos++;
                    dstPos += numFlat;
                }
                if (srcPos < endOfData)
                {
                    byte numNoFlat = src[srcOffset + srcPos++];
                    Array.Copy(src, srcOffset + srcPos, dest, dstPos, numNoFlat);
                    srcPos += numNoFlat;
                    dstPos += numNoFlat;
                }
            }
        }

        private void DecompressRLE0(byte[] src, int srcOffset, uint compSize, byte[] dest)
        {
            var srcBufEnd = compSize;
            var srcPos = 0;
            var dstPos = 0;
            while (srcPos < srcBufEnd)
            {
                byte color = src[srcOffset + srcPos++];
                if (color != 0)
                {
                    dest[dstPos++] = color;
                }
                else
                {
                    byte skip = src[srcOffset + srcPos++];
                    Array.Clear(dest, dstPos, skip);
                    dstPos += skip;
                }
            }
        }

        private void DecompressRLE7(byte[] src, int srcOffset, uint compSize, byte[] dest)
        {
            var compBufEnd = compSize;
            var srcPos = 0;
            var dstPos = 0;
            while (srcPos < compBufEnd)
            {
                byte code = src[srcOffset + srcPos++];
                if ((code > 127) || (code == 0))
                    dest[dstPos++] = code;
                else
                {
                    code++;
                    dest.Set(dstPos, src[srcOffset + srcPos++], code);
                    dstPos += code;
                }
            }
        }

        private void RenderParallax(byte[] data)
        {
            ushort paraScrlX, paraScrlY;
            ushort scrnScrlX, scrnScrlY;
            ushort scrnWidth, scrnHeight;
            ushort paraSizeX, paraSizeY;
            ParallaxHeader header = null;
            UIntAccess lineIndexes = null;

            if (SystemVars.Platform == Platform.PSX) //Parallax headers are different in PSX version
                FetchPsxParallaxSize(data, out paraSizeX, out paraSizeY);
            else
            {
                header = new ParallaxHeader(data);
                lineIndexes = new UIntAccess(data, ParallaxHeader.Size);
                paraSizeX = _resMan.ReadUInt16(header.sizeX);
                paraSizeY = _resMan.ReadUInt16(header.sizeY);
            }

            System.Diagnostics.Debug.Assert((paraSizeX >= SCREEN_WIDTH) && (paraSizeY >= SCREEN_DEPTH));

            // we have to render more than the visible screen part for displaying scroll frames
            scrnScrlX = (ushort)Math.Min((uint)_oldScrollX, Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X]);
            scrnWidth = (ushort)(SCREEN_WIDTH + Math.Abs((int)_oldScrollX - (int)Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_X]));
            scrnScrlY = (ushort)Math.Min((uint)_oldScrollY, Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y]);
            scrnHeight = (ushort)(SCREEN_DEPTH + Math.Abs((int)_oldScrollY - (int)Logic.ScriptVars[(int)ScriptVariableNames.SCROLL_OFFSET_Y]));


            if (_scrnSizeX != SCREEN_WIDTH)
            {
                double scrlfx = (paraSizeX - SCREEN_WIDTH) / ((double)(_scrnSizeX - SCREEN_WIDTH));
                paraScrlX = (ushort)(scrnScrlX * scrlfx);
            }
            else
                paraScrlX = 0;

            if (_scrnSizeY != SCREEN_DEPTH)
            {
                double scrlfy = (paraSizeY - SCREEN_DEPTH) / ((double)(_scrnSizeY - SCREEN_DEPTH));
                paraScrlY = (ushort)(scrnScrlY * scrlfy);
            }
            else
                paraScrlY = 0;

            if (SystemVars.Platform == Platform.PSX)
            {
                DrawPsxParallax(data, paraScrlX, scrnScrlX, scrnWidth);
            }
            else
            {
                for (ushort cnty = 0; cnty < scrnHeight; cnty++)
                {
                    var src = _resMan.ReadUInt32(lineIndexes[cnty + paraScrlY]);
                    var dest = scrnScrlX + (cnty + scrnScrlY) * _scrnSizeX;
                    ushort remain = paraScrlX;
                    ushort xPos = 0;
                    while (remain != 0)
                    {
                        // skip past the first part of the parallax to get to the right scrolling position
                        byte doSkip = data[src++];
                        if (doSkip <= remain)
                            remain -= doSkip;
                        else
                        {
                            xPos = (ushort)(doSkip - remain);
                            dest += xPos;
                            remain = 0;
                        }
                        byte doCopy = data[src++];
                        if (doCopy <= remain)
                        {
                            remain -= doCopy;
                            src += doCopy;
                        }
                        else
                        {
                            ushort remCopy = (ushort)(doCopy - remain);
                            Array.Copy(data, (int)(src + remain), _screenBuf, dest, remCopy);
                            dest += remCopy;
                            src += doCopy;
                            xPos = remCopy;
                            remain = 0;
                        }
                    }
                    while (xPos < scrnWidth)
                    {
                        byte skip;
                        if ((skip = data[src++]) != 0)
                        {
                            dest += skip;
                            xPos += skip;
                        }
                        if (xPos < scrnWidth)
                        {
                            byte doCopy;
                            if ((doCopy = data[src++]) != 0)
                            {
                                if (xPos + doCopy > scrnWidth)
                                    doCopy = (byte)(scrnWidth - xPos);
                                Array.Copy(data, (int)src, _screenBuf, dest, doCopy);
                                dest += doCopy;
                                xPos += doCopy;
                                src += doCopy;
                            }
                        }
                    }
                }
            }
        }

        private void FadePalette()
        {
            if (_fadingStep == 16)
            {
                Array.Copy(_targetPalette, _currentPalette, 256);
            }
            else if ((_fadingStep == 1) && (_fadingDirection == FADE_DOWN))
            {
                Array.Clear(_currentPalette, 0, 256);
            }
            else
            {
                for (ushort cnt = 0; cnt < 256; cnt++)
                    _currentPalette[cnt] = Color.FromRgb(
                        (_targetPalette[cnt].R * _fadingStep) >> 4,
                        (_targetPalette[cnt].G * _fadingStep) >> 4,
                        (_targetPalette[cnt].B * _fadingStep) >> 4);
            }

            _fadingStep += _fadingDirection;
            if (_fadingStep == 17)
            {
                _fadingStep = 0;
                _isBlack = false;
            }
            else if (_fadingStep == 0)
                _isBlack = true;
        }


        // Used to draw psx sprites which are 1/2 of original width
        private void DrawPsxHalfShrinkedSprite(byte[] sprData, int offset, ushort sprX, ushort sprY, ushort sprWidth, ushort sprHeight, ushort sprPitch)
        {
            var dest = new ByteAccess(_screenBuf, (sprY * _scrnSizeX) + sprX);
            var data = new ByteAccess(sprData, offset);

            for (ushort cnty = 0; cnty < sprHeight; cnty++)
            {
                for (ushort cntx = 0; cntx < sprWidth; cntx++)
                    if (data[cntx] != 0)
                    {
                        dest[cntx * 2] = data[cntx]; //In these sprites we need to double vetical lines too...
                        dest[cntx * 2 + 1] = data[cntx];
                    }

                dest.Offset += _scrnSizeX;
                for (ushort cntx = 0; cntx < sprWidth; cntx++)
                    if (data[cntx] != 0)
                    {
                        dest[cntx * 2] = data[cntx];
                        dest[cntx * 2 + 1] = data[cntx];
                    }

                data.Offset += sprPitch;
                dest.Offset += _scrnSizeX;
            }
        }

        // Used to draw psx sprites which are 1/3 of original width
        private void DrawPsxFullShrinkedSprite(byte[] sprData, int offset, ushort sprX, ushort sprY, ushort sprWidth, ushort sprHeight, ushort sprPitch)
        {
            var dest = new ByteAccess(_screenBuf, (sprY * _scrnSizeX) + sprX);
            var data = new ByteAccess(sprData, offset);

            for (ushort cnty = 0; cnty < sprHeight; cnty++)
            {
                for (ushort cntx = 0; cntx < sprWidth; cntx++)
                    if (data[cntx] != 0)
                    {
                        dest[cntx * 3] = data[cntx]; //In these sprites we need to double vertical lines too...
                        dest[cntx * 3 + 1] = data[cntx];
                        dest[cntx * 3 + 2] = data[cntx];
                    }

                dest.Offset += _scrnSizeX;
                for (ushort cntx = 0; cntx < sprWidth; cntx++)
                    if (data[cntx] != 0)
                    {
                        dest[cntx * 3] = data[cntx];
                        dest[cntx * 3 + 1] = data[cntx];
                        dest[cntx * 3 + 2] = data[cntx];
                    }

                data.Offset += sprPitch;
                dest.Offset += _scrnSizeX;
            }
        }

        private void DrawPsxParallax(byte[] psxParallax, ushort paraScrlX, ushort scrnScrlX, ushort scrnWidth)
        {
            ushort totTiles = psxParallax.ToUInt16(14); // Total tiles

            ushort skipRow = (ushort)(paraScrlX / 16); // Rows of tiles we have to skip
            byte leftPixelSkip = (byte)(paraScrlX % 16); // Pixel columns we have to skip while drawing the first row

            var plxPos = new ByteAccess(psxParallax, 16); // Pointer to tile position header section
            var plxOff = new ByteAccess(psxParallax, 16 + totTiles * 2); // Pointer to tile relative offsets section
            var plxData = new ByteAccess(psxParallax, 16 + totTiles * 2 + totTiles * 4); //Pointer to beginning of tiles data section

            var tile_buffer = new byte[16 * 16]; // Buffer for 16x16 pix tile

            /* For parallax rendering we should check both horizontal and vertical scrolling,
             * but in PSX edition of the game, the only vertical scrolling parallax is disabled.
             * So, in this function i'll only check for horizontal scrolling.
             */

            for (ushort currentTile = 0; currentTile < totTiles - 1; currentTile++)
            {
                byte tileXpos = plxPos[2 * currentTile]; // Fetch tile X and Y position in the grid
                byte tileYpos = (byte)(plxPos[2 * currentTile + 1] * 2);
                int tileBegin = (tileXpos * 16) - paraScrlX;
                tileBegin = (tileBegin < 0) ? 0 : tileBegin;
                ushort currentLine = (ushort)(tileYpos * 16); //Current line of the image we are drawing upon, used to avoid going out of screen

                if (tileXpos >= skipRow)
                { // Tiles not needed in the screen buffer are not uncompressed
                    int tileOffset = plxOff.Data.ToInt32(plxOff.Offset + 4 * currentTile);
                    ushort rightScreenLimit = (ushort)(_scrnSizeX - scrnScrlX); // Do not write over and beyond this limit, lest we get memory corruption
                    var dest = new ByteAccess(_screenBuf, (tileYpos * 16 * _scrnSizeX) + tileBegin + scrnScrlX);
                    var src = new ByteAccess(tile_buffer);

                    DecompressHIF(plxData.Data, plxData.Offset + tileOffset, tile_buffer); // Decompress the tile

                    if (tileXpos != skipRow)
                    { // This tile will surely be drawn fully in the buffer
                        for (byte tileLine = 0; (tileLine < 16) && (currentLine < SCREEN_DEPTH); tileLine++)
                        { // Check that we are not going outside the bottom screen part
                            for (byte tileColumn = 0; (tileColumn < 16) && (tileBegin + tileColumn) < rightScreenLimit; tileColumn++)
                                if (src[tileColumn] != 0)
                                    dest[tileColumn] = src[tileColumn];
                            dest.Offset += _scrnSizeX;
                            currentLine++;

                            if (currentLine < SCREEN_DEPTH)
                            {
                                for (byte tileColumn = 0; (tileColumn < 16) && (tileBegin + tileColumn) < rightScreenLimit; tileColumn++)
                                    if (src[tileColumn] != 0)
                                        dest[tileColumn] = src[tileColumn];
                                dest.Offset += _scrnSizeX;
                                currentLine++;
                            }
                            src.Offset += 16; // get to next line of decoded tile
                        }
                    }
                    else { // This tile may be drawn only partially
                        src.Offset += leftPixelSkip; //Skip hidden pixels
                        for (byte tileLine = 0; (tileLine < 16) && (currentLine < SCREEN_DEPTH); tileLine++)
                        {
                            for (byte tileColumn = 0; tileColumn < (16 - leftPixelSkip); tileColumn++)
                                if (src[tileColumn] != 0)
                                    dest[tileColumn] = src[tileColumn];
                            dest.Offset += _scrnSizeX;
                            currentLine++;

                            if (currentLine < SCREEN_DEPTH)
                            {
                                for (byte tileColumn = 0; tileColumn < (16 - leftPixelSkip); tileColumn++)
                                    if (src[tileColumn] != 0)
                                        dest[tileColumn] = src[tileColumn];
                                dest.Offset += _scrnSizeX;
                                currentLine++;
                            }
                            src.Offset += 16;
                        }
                    }
                }
            }
        }

        // needed because some psx backgrounds are half width and half height
        private byte[] PsxShrinkedBackgroundToIndexed(byte[] psxBackground, int offset, int bakXres, int bakYres)
        {
            int xresInTiles = ((bakXres / 2) % 16) != 0 ? (bakXres / 32) + 1 : (bakXres / 32);
            int yresInTiles = ((bakYres / 2) % 16) != 0 ? (bakYres / 32) + 1 : (bakYres / 32);
            int totTiles = xresInTiles * yresInTiles;
            int tileYpos = 0; //tile position in a virtual xresInTiles * yresInTiles grid
            int tileXpos = 0;
            uint dataBegin = psxBackground.ToUInt32(offset + 4);

            var decomp_tile = new byte[16 * 16]; //Tiles are always 16 * 16
            var fullres_buffer = new byte[bakXres * (yresInTiles + 1) * 32];

            bool isCompressed = (psxBackground.ToUInt32(offset) == ScummHelper.MakeTag('C', 'O', 'M', 'P'));

            totTiles -= xresInTiles;
            var b = offset + 4; //We skip the id tag

            int currentTile;
            for (currentTile = 0; currentTile < totTiles; currentTile++)
            {
                int tileOffset = psxBackground.ToInt32(b + 4 * currentTile);

                if (isCompressed)
                {
                    DecompressHIF(psxBackground, b + tileOffset - 4, decomp_tile); //Decompress the tile into decomp_tile
                }
                else {
                    Array.Copy(psxBackground, b + tileOffset - 4, decomp_tile, 0, 16 * 16);
                }

                if (currentTile > 0 && (currentTile % xresInTiles) == 0)
                { //Finished a line of tiles, going down
                    tileYpos++;
                    tileXpos = 0;
                }

                for (byte tileLine = 0; tileLine < 16; tileLine++)
                {
                    var dest = new ByteAccess(fullres_buffer, tileLine * bakXres * 2 + tileXpos * 32 + tileYpos * bakXres * 16 * 2);
                    for (byte tileColumn = 0; tileColumn < 16; tileColumn++)
                    {
                        byte pixData = decomp_tile[tileColumn + tileLine * 16];
                        dest[tileColumn * 2] = pixData;
                        dest[tileColumn * 2 + 1] = pixData;
                    }
                    dest.Offset += bakXres;
                    for (byte tileColumn = 0; tileColumn < 16; tileColumn++)
                    {
                        var pixData = decomp_tile[tileColumn + tileLine * 16];
                        dest[tileColumn * 2] = pixData;
                        dest[tileColumn * 2 + 1] = pixData;
                    }

                }
                tileXpos++;
            }

            //Calculate number of remaining tiles
            uint remainingTiles = (uint)((dataBegin - (currentTile * 4 + 4)) / 4);

            // Last line of tiles is full width!
            uint tileHeight = (remainingTiles == xresInTiles * 2) ? 16U : 8;

            tileXpos = 0;
            for (; currentTile < totTiles + remainingTiles; currentTile++)
            {
                int tileOffset = psxBackground.ToInt32(b + 4 * currentTile);

                if (isCompressed)
                {
                    DecompressHIF(psxBackground, b + tileOffset - 4, decomp_tile); //Decompress the tile into decomp_tile
                }
                else {
                    Array.Copy(psxBackground, b + tileOffset - 4, decomp_tile, 0, 256);
                }

                for (byte tileLine = 0; tileLine < tileHeight; tileLine++)
                { // Write the decoded tiles into last lines of background
                    Array.Copy(decomp_tile, tileLine * 16, fullres_buffer, tileXpos * 16 + (tileLine + (yresInTiles - 1) * 16) * bakXres * 2, 16);
                    Array.Copy(decomp_tile, tileLine * 16, fullres_buffer, tileXpos * 16 + (tileLine + (yresInTiles - 1) * 16) * bakXres * 2 + bakXres, 16);
                }
                tileXpos++;
            }

            return fullres_buffer;
        }

        private byte[] PsxBackgroundToIndexed(byte[] psxBackground, int offset, int bakXres, int bakYres)
        {
            int xresInTiles = bakXres / 16;
            int yresInTiles = ((bakYres / 2) % 16) != 0 ? (bakYres / 32) + 1 : (bakYres / 32);
            int totTiles = xresInTiles * yresInTiles;
            int tileYpos = 0; //tile position in a virtual xresInTiles * yresInTiles grid
            int tileXpos = 0;
            uint tag = psxBackground.ToUInt32(offset);

            var decomp_tile = new byte[16 * 16]; //Tiles are always 16 * 16
            var fullres_buffer = new byte[bakXres * yresInTiles * 32];

            bool isCompressed = (tag == 0x434F4D50);

            offset += 4; //We skip the id tag

            for (var currentTile = 0; currentTile < totTiles; currentTile++)
            {
                int tileOffset = psxBackground.ToInt32(offset + 4 * currentTile);

                if (isCompressed)
                    DecompressHIF(psxBackground, offset + tileOffset - 4, decomp_tile); //Decompress the tile into decomp_tile
                else
                    Array.ConstrainedCopy(psxBackground, offset + tileOffset - 4, decomp_tile, 0, 16 * 16);

                if (currentTile > 0 && (currentTile % xresInTiles) == 0)
                { //Finished a line of tiles, going down
                    tileYpos++;
                    tileXpos = 0;
                }

                for (byte tileLine = 0; tileLine < 16; tileLine++)
                {
                    // Copy data to destination buffer
                    Array.Copy(decomp_tile, tileLine * 16, fullres_buffer, tileLine * bakXres * 2 + tileXpos * 16 + tileYpos * bakXres * 16 * 2, 16);
                    Array.Copy(decomp_tile, tileLine * 16, fullres_buffer, tileLine * bakXres * 2 + bakXres + tileXpos * 16 + tileYpos * bakXres * 16 * 2, 16);
                }
                tileXpos++;
            }

            return fullres_buffer;
        }

        private void FetchPsxParallaxSize(byte[] psxParallax, out ushort paraSizeX, out ushort paraSizeY)
        {
            ushort xresInTiles = psxParallax.ToUInt16(10);
            ushort yresInTiles = psxParallax.ToUInt16(12);

            paraSizeX = (ushort)(xresInTiles * 16);
            paraSizeY = (ushort)(yresInTiles * 32); // Vertical resolution needs to be doubled
        }

        private void FlushPsxCache()
        {
            _psxCache.decodedBackground = null;
            _psxCache.extPlxCache = null;
        }
    }
}