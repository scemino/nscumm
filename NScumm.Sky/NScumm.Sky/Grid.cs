using System;
using System.Diagnostics;

namespace NScumm.Sky
{
    internal class Grid
    {
        private const int TopLeftX = 128;
        private const int TopLeftY = 136;

        private const int TotNoGrids = 70; //total no. of grids supported

        private const int GridFileStart = 60000;
        private readonly byte[][] _gameGrids = new byte[TotNoGrids][];

        private readonly sbyte[] _gridConvertTable =
        {
            0, //0
            1, //1
            2, //2
            3, //3
            4, //4
            5, //5
            6, //6
            7, //7
            8, //8
            9, //9
            10, //10
            11, //11
            12, //12
            13, //13
            14, //14
            15, //15
            16, //16
            17, //17
            18, //18
            19, //19
            20, //20
            21, //21
            22, //22
            23, //23
            24, //24
            25, //25
            26, //26
            27, //27
            28, //28
            29, //29
            30, //30
            31, //31
            32, //32
            33, //33
            34, //34
            -1, //35
            35, //36
            36, //37
            37, //38
            38, //39
            39, //40
            40, //41
            41, //42
            -1, //43
            42, //44
            43, //45
            44, //46
            45, //47
            46, //48
            -1, //49
            -1, //50
            -1, //51
            -1, //52
            -1, //53
            -1, //54
            -1, //55
            -1, //56
            -1, //57
            -1, //58
            -1, //59
            -1, //60
            -1, //61
            -1, //62
            -1, //63
            -1, //64
            47, //65
            TotNoGrids, //66
            48, //67
            49, //68
            50, //69
            51, //70
            52, //71
            53, //72
            54, //73
            55, //74
            56, //75
            57, //76
            58, //77
            59, //78
            60, //79
            -1, //80
            61, //81
            62, //82
            -1, //83
            -1, //84
            -1, //85
            -1, //86
            -1, //87
            -1, //88
            TotNoGrids, //89
            63, //90
            64, //91
            65, //92
            66, //93
            67, //94
            68, //95
            69 //96
        };

        private readonly SkyCompact _skyCompact;
        private readonly Disk _skyDisk;
        private readonly Logic _skyLogic;

        public Grid(Logic logic, Disk disk, SkyCompact skyCompact)
        {
            _skyLogic = logic;
            _skyDisk = disk;
            _skyCompact = skyCompact;
        }

        public void RemoveObjectFromWalk(Compact cpt)
        {
            int bitNum;
            uint width;
            byte gridIdx;
            if (GetGridValues(cpt, out gridIdx, out bitNum, out width))
                RemoveObjectFromWalk(gridIdx, bitNum, width);
        }

        public void ObjectToWalk(Compact cpt)
        {
            int bitNum;
            uint width;
            byte gridIdx;
            if (GetGridValues(cpt, out gridIdx, out bitNum, out width))
                ObjectToWalk(gridIdx, bitNum, width);
        }

        public void LoadGrids()
        {
            // no endian conversion necessary as I'm using uint8* instead of uint32*
            for (byte cnt = 0; cnt < TotNoGrids; cnt++)
            {
                _gameGrids[cnt] = _skyDisk.LoadFile(GridFileStart + cnt);
            }
            if (!SkyEngine.IsDemo)
            {
                // single disk demos never get that far
                // Reloading the grids can sometimes cause problems eg when reichs door is
                // open the door grid bit gets replaced so you can't get back in (or out)
                if (_skyLogic.ScriptVariables[Logic.REICH_DOOR_FLAG] != 0)
                    RemoveGrid(256, 280, 1, _skyCompact.FetchCpt((ushort) CptIds.ReichDoor20));
                //removeGrid(256, 280, 1, &SkyCompact::reich_door_20);
            }
        }

        public void RemoveGrid(uint x, uint y, uint width, Compact cpt)
        {
            int resBitPos;
            uint resWidth;
            byte resGridIdx;
            if (GetGridValues(x, y, width, cpt, out resGridIdx, out resBitPos, out resWidth))
                RemoveObjectFromWalk(resGridIdx, resBitPos, resWidth);
        }

        public void PlotGrid(uint x, uint y, uint width, Compact cpt)
        {
            int resBitPos;
            uint resWidth;
            byte resGridIdx;
            if (GetGridValues(x, y, width - 1, cpt, out resGridIdx, out resBitPos, out resWidth))
                ObjectToWalk(resGridIdx, resBitPos, resWidth);
        }

        private void ObjectToWalk(byte gridIdx, int bitNum, uint width)
        {
            for (uint cnt = 0; cnt < width; cnt++)
            {
                _gameGrids[gridIdx][bitNum >> 3] |= (byte) (1 << (bitNum & 0x7));
                if ((bitNum & 0x1F) == 0)
                    bitNum += 0x3F;
                else
                    bitNum--;
            }
        }

        private void RemoveObjectFromWalk(byte gridIdx, int bitNum, uint width)
        {
            for (uint cnt = 0; cnt < width; cnt++)
            {
                _gameGrids[gridIdx][bitNum >> 3] &= (byte) ~(1 << (bitNum & 0x7));
                if ((bitNum & 0x1F) == 0)
                    bitNum += 0x3F;
                else
                    bitNum--;
            }
        }

        private bool GetGridValues(Compact cpt, out byte resGrid, out int resBitNum, out uint resWidth)
        {
            var width = SkyCompact.GetMegaSet(cpt).gridWidth;
            return GetGridValues(cpt.Core.xcood, cpt.Core.ycood, width, cpt, out resGrid, out resBitNum, out resWidth);
        }

        private bool GetGridValues(uint x, uint y, uint width, Compact cpt, out byte resGrid, out int resBitNum, out uint resWidth)
        {
            int bitPos;
            resGrid = 0;
            resBitNum = 0;
            resWidth = 0;
            if (y < TopLeftY)
                return false; // off screen
            y -= TopLeftY;
            y >>= 3; // convert to blocks
            if (y >= Screen.GameScreenHeight >> 3)
                return false; // off screen
            bitPos = (int) (y*40);
            width++;
            x >>= 3; // convert to blocks

            if (x < TopLeftX >> 3)
            {
                // at least partially off screen
                if (x + width < TopLeftX >> 3)
                    return false; // completely off screen
                width -= (TopLeftX >> 3) - x;
                x = 0;
            }
            else
                x -= TopLeftX >> 3;

            if (Screen.GameScreenWidth >> 3 <= x)
                return false; // off screen
            if (Screen.GameScreenWidth >> 3 < x + width) // partially off screen
                width = (Screen.GameScreenWidth >> 3) - x;

            bitPos += (int) x;
            Debug.Assert((_gridConvertTable[cpt.Core.screen] >= 0) && (_gridConvertTable[cpt.Core.screen] < TotNoGrids));
            resGrid = (byte) _gridConvertTable[cpt.Core.screen];

            var tmpBits = 0x1F - (bitPos & 0x1F);
            bitPos &= ~0x1F; // divide into dword address and bit number
            bitPos += tmpBits;
            resBitNum = bitPos;
            resWidth = width;
            return true;
        }
    }
}