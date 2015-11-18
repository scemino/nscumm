using System;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Sword1
{
    struct RoomDef
    {
        public int totalLayers;
        public int sizeX;
        public int sizeY;
        public int gridWidth;  //number of 16*16 grid blocks across - including off screen edges.
        public uint[] layers;
        public uint[] grids;
        public uint[] palettes;
        public uint[] parallax;
    }

    internal class Screen
    {
        const int SCRNGRID_X = 16;
        const int SCRNGRID_Y = 8;
        const int SHRINK_BUFFER_SIZE = 50000;
        const int RLE_BUFFER_SIZE = 50000;

        const int FLASH_RED = 0;
        const int FLASH_BLUE = 1;
        const int BORDER_YELLOW = 2;
        const int BORDER_GREEN = 3;
        const int BORDER_PURPLE = 4;
        const int BORDER_BLACK = 5;

        const int SCREEN_WIDTH = 640;
        const int SCREEN_DEPTH = 400;

        private ISystem _system;
        private ResMan _resMan;
        private ObjectMan _objectMan;
        private ushort _currentScreen;
        private byte[] _screenBuf;
        private bool _fullRefresh;
        private ushort _scrnSizeX;
        private ushort _scrnSizeY;
        private ushort _gridSizeX;
        private ushort _gridSizeY;
        private byte[] _screenGrid;
        private ByteAccess[] _layerBlocks = new ByteAccess[4];
        private byte[][] _parallax = new byte[2][];
        private bool _updatePalette;

        public Screen(ISystem system, ResMan resMan, ObjectMan objectMan)
        {
            _system = system;
            _resMan = resMan;
            _objectMan = objectMan;
            _currentScreen = 0xFFFF;
        }

        public void NewScreen(uint screen)
        {
            // set sizes and scrolling, initialize/load screengrid, force screen refresh
            _currentScreen = (ushort)screen;
            _scrnSizeX = (ushort)RoomDefTable[screen].sizeX;
            _scrnSizeY = (ushort)RoomDefTable[screen].sizeY;
            _gridSizeX = (ushort)(_scrnSizeX / SCRNGRID_X);
            _gridSizeY = (ushort)(_scrnSizeY / SCRNGRID_Y);
            if ((_scrnSizeX % SCRNGRID_X) != 0 || (_scrnSizeY % SCRNGRID_Y) != 0)
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

            for (var cnt = 0; cnt < RoomDefTable[_currentScreen].totalLayers; cnt++)
            {
                // open and lock all resources, will be closed in quitScreen()
                _layerBlocks[cnt] = new ByteAccess(_resMan.OpenFetchRes(RoomDefTable[_currentScreen].layers[cnt]), 0);
                if (cnt > 0)
                    _layerBlocks[cnt].Offset += Header.Size;
            }
            for (var cnt = 0; cnt < RoomDefTable[_currentScreen].totalLayers - 1; cnt++)
            {
                // there's no grid for the background layer, so it's totalLayers - 1
                _layerGrid[cnt] = new UShortAccess(_resMan.OpenFetchRes(RoomDefTable[_currentScreen].grids[cnt]), 0);
                _layerGrid[cnt].Offset += 14 * 2;
            }
            _parallax[0] = _parallax[1] = null;
            if (RoomDefTable[_currentScreen].parallax[0] != 0)
                _parallax[0] = _resMan.OpenFetchRes(RoomDefTable[_currentScreen].parallax[0]);
            if (RoomDefTable[_currentScreen].parallax[1] != 0)
                _parallax[1] = _resMan.OpenFetchRes(RoomDefTable[_currentScreen].parallax[1]);

            _updatePalette = true;
            _fullRefresh = true;
        }

        private void FlushPsxCache()
        {
            throw new NotImplementedException();
        }

        public void Draw()
        {
            throw new System.NotImplementedException();
        }

        public bool ShowScrollFrame()
        {
            throw new System.NotImplementedException();
        }

        public void UpdateScreen()
        {
            throw new System.NotImplementedException();
        }

        public void FullRefresh()
        {
            throw new System.NotImplementedException();
        }

        public void FadeDownPalette()
        {
            throw new System.NotImplementedException();
        }

        public bool StillFading()
        {
            throw new System.NotImplementedException();
        }

        public void QuitScreen()
        {
            throw new System.NotImplementedException();
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

        public static RoomDef[] RoomDefTable =
        {
            // these are NOT const
            new RoomDef {
                totalLayers = 0, //total_layers  --- room 0 NOT USED
                sizeX = 0, //size_x			= width
                sizeY = 0, //size_y			= height
                gridWidth = 0, //grid_width	= width/16 + 16
                layers = new uint[]{0, 0, 0, 0}, //layers
                grids = new uint[] {0, 0, 0}, //grids
                palettes = new uint[] {0, 0}, //palettes		{ background palette [0..183], sprite palette [184..255] }
                parallax = new uint[] {0, 0}, //parallax layers
            },
            //------------------------------------------------------------------------
	        // PARIS 1

	        new RoomDef {
                totalLayers =3,													//total_layers		//room 1
		        sizeX= 784,												//size_x
		        sizeY= 400,												//size_y
		        gridWidth=65,													//grid_width
		        layers=new uint[]{ Sword1Res.room1_l0, Sword1Res.room1_l1, Sword1Res.room1_l2 },						//layers
		        grids=new uint[]{ Sword1Res.room1_gd1, Sword1Res.room1_gd2 },								//grids
		        palettes=new uint[]{ Sword1Res.room1_PAL, Sword1Res.PARIS1_PAL },								//palettes
		        parallax =new uint[]{ Sword1Res.room1_plx,0},												//parallax layers
	        },
        };

        private UShortAccess[] _layerGrid = new UShortAccess[4];

        public class Header
        {
            string type;
            ushort version;
            uint comp_length;
            string compression;
            uint decomp_length;

            public const int Size = 20;
        }
    }
}