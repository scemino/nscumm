//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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
using NScumm.Core;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Another
{
    internal enum ResType : byte
    {
        RtSound = 0,
        RtMusic = 1,
        RtPolyAnim = 2, // full screen Video buffer, size=0x7D00

        // FCS: 0x7D00=32000...but 320x200 = 64000 ??
        // Since the game is 16 colors, two pixels palette indices can be stored in one byte
        // that's why we can store two pixels palette indice in one byte and we only need 320*200/2 bytes for
        // an entire screen.

        RtPalette = 3, // palette (1024=vga + 1024=ega), size=2048
        RtBytecode = 4,
        RtPolyCinematic = 5
    }

    internal class Resource
    {
        /// <summary>
        /// 600kb total memory consumed (not taking into account stack and static heap)
        /// </summary>
        private const int MemBlockSize = 600 * 1024;

        private const int MemEntryStateEndOfMemlist = 0xFF;
        private const int MemEntryStateNotNeeded = 0;
        private const int MemEntryStateLoaded = 1;
        private const int MemEntryStateLoadMe = 2;

        private const int ResSize = 0;
        private const int ResCompressed = 1;
        private const int StatsTotalSize = 6;

        //The game is divided in 10 parts.
        private const int GameNumParts = 10;

        public const int GamePartFirst = 0x3E80;
        public const int GamePart1 = 0x3E80;
        public const int GamePart2 = 0x3E81; //Introductino
        public const int GamePart3 = 0x3E82;
        public const int GamePart4 = 0x3E83; //Wake up in the suspended jail
        public const int GamePart5 = 0x3E84;
        public const int GamePart6 = 0x3E85; //BattleChar sequence
        public const int GamePart7 = 0x3E86;
        public const int GamePart8 = 0x3E87;
        public const int GamePart9 = 0x3E88;
        public const int GamePart10 = 0x3E89;
        private const int GamePartLast = 0x3E89;

        //For each part of the game, four resources are referenced.
        private const int MemlistPartPalette = 0;

        private const int MemlistPartCode = 1;
        private const int MemlistPartPolyCinematic = 2;
        private const int MemlistPartVideo2 = 3;

        private const int MemlistPartNone = 0x00;

        private readonly int[,] _resourceSizeStats = new int[7, 2];
        private readonly int[,] _resourceUnitStats = new int[7, 2];
        public readonly MemEntry[] MemList = ScummHelper.CreateArray<MemEntry>(150);

        private static readonly string[] ResTypes =
        {
            "RT_SOUND",
            "RT_MUSIC",
            "RT_POLY_ANIM",
            "RT_PALETTE",
            "RT_BYTECODE",
            "RT_POLY_CINEMATIC"
        };

        /*
	MEMLIST_PART_VIDEO1 and MEMLIST_PART_VIDEO2 are used to store polygons.

	It seems that:
	- MEMLIST_PART_VIDEO1 contains the cinematic polygons.
	- MEMLIST_PART_VIDEO2 contains the polygons for player and enemies animations.

	That would make sense since protection screen and cinematic game parts do not load MEMLIST_PART_VIDEO2.

*/
        private static readonly ushort[,] MemListParts = new ushort[,]
        {
            //MEMLIST_PART_PALETTE   MEMLIST_PART_CODE   MEMLIST_PART_VIDEO1   MEMLIST_PART_VIDEO2
            {0x14, 0x15, 0x16, 0x00}, // protection screens
            {0x17, 0x18, 0x19, 0x00}, // introduction cinematic
            {0x1A, 0x1B, 0x1C, 0x11},
            {0x1D, 0x1E, 0x1F, 0x11},
            {0x20, 0x21, 0x22, 0x11},
            {0x23, 0x24, 0x25, 0x00}, // battlechar cinematic
            {0x26, 0x27, 0x28, 0x11},
            {0x29, 0x2A, 0x2B, 0x11},
            {0x7D, 0x7E, 0x7F, 0x00},
            {0x7D, 0x7E, 0x7F, 0x00} // password screen
        };

        private static readonly AmigaMemEntry[] MemListAmigaFR =
        {
            new AmigaMemEntry(0, 0x1, 0x000000, 0x0000, 0x0000),
            new AmigaMemEntry(0, 0x1, 0x000000, 0x1A3C, 0x1A3C),
            new AmigaMemEntry(0, 0x1, 0x001A3C, 0x2E34, 0x2E34),
            new AmigaMemEntry(0, 0x1, 0x004870, 0x69F8, 0x69F8),
            new AmigaMemEntry(0, 0x1, 0x00B268, 0x45CE, 0x45CE),
            new AmigaMemEntry(0, 0x1, 0x00F836, 0x0EFA, 0x0EFA),
            new AmigaMemEntry(0, 0x1, 0x010730, 0x0D26, 0x0D26),
            new AmigaMemEntry(1, 0x1, 0x011456, 0x0494, 0x3CC0),
            new AmigaMemEntry(0, 0x2, 0x000000, 0x2674, 0x2674),
            new AmigaMemEntry(0, 0x1, 0x0118EA, 0x2BB6, 0x2BB6),
            new AmigaMemEntry(0, 0x1, 0x0144A0, 0x2BB4, 0x2BB4),
            new AmigaMemEntry(0, 0x1, 0x017054, 0x0426, 0x0426),
            new AmigaMemEntry(0, 0x1, 0x01747A, 0x1852, 0x1852),
            new AmigaMemEntry(0, 0x1, 0x018CCC, 0x0594, 0x0594),
            new AmigaMemEntry(0, 0x1, 0x019260, 0x13F0, 0x13F0),
            new AmigaMemEntry(0, 0x1, 0x01A650, 0x079E, 0x079E),
            new AmigaMemEntry(0, 0x2, 0x002674, 0x56A2, 0x56A2),
            new AmigaMemEntry(6, 0xC, 0x000000, 0x6214, 0x6214),
            new AmigaMemEntry(2, 0x5, 0x000000, 0x2410, 0x7D00),
            new AmigaMemEntry(2, 0x5, 0x002410, 0x7D00, 0x7D00),
            new AmigaMemEntry(3, 0x1, 0x01ADEE, 0x0800, 0x0800),
            new AmigaMemEntry(4, 0x1, 0x01B5EE, 0x0D2A, 0x0D2A),
            new AmigaMemEntry(5, 0x1, 0x01C318, 0x107C, 0x107C),
            new AmigaMemEntry(3, 0x1, 0x01D394, 0x0800, 0x0800),
            new AmigaMemEntry(4, 0x1, 0x01DB94, 0x2530, 0x2530),
            new AmigaMemEntry(5, 0x1, 0x0200C4, 0xFE7A, 0xFE7A),
            new AmigaMemEntry(3, 0x2, 0x007D16, 0x0800, 0x0800),
            new AmigaMemEntry(4, 0x2, 0x008516, 0x4BD0, 0x4BD0),
            new AmigaMemEntry(5, 0x2, 0x00D0E6, 0xFDBA, 0xFDBA),
            new AmigaMemEntry(3, 0xD, 0x000000, 0x0800, 0x0800),
            new AmigaMemEntry(4, 0xD, 0x000800, 0x974A, 0x974A),
            new AmigaMemEntry(5, 0xD, 0x009F4A, 0xD1D8, 0xD1D8),
            new AmigaMemEntry(3, 0x3, 0x000000, 0x0800, 0x0800),
            new AmigaMemEntry(4, 0x3, 0x000800, 0xED30, 0xED30),
            new AmigaMemEntry(5, 0x3, 0x00F530, 0xFEF6, 0xFEF6),
            new AmigaMemEntry(3, 0xA, 0x000000, 0x0800, 0x0800),
            new AmigaMemEntry(4, 0xA, 0x000800, 0x1B00, 0x1B00),
            new AmigaMemEntry(5, 0xA, 0x002300, 0x5E58, 0x5E58),
            new AmigaMemEntry(3, 0xA, 0x008158, 0x0800, 0x0800),
            new AmigaMemEntry(4, 0xA, 0x008958, 0x99D8, 0x99D8),
            new AmigaMemEntry(5, 0xA, 0x012330, 0xFF9A, 0xFF9A),
            new AmigaMemEntry(3, 0xB, 0x000000, 0x0800, 0x0800),
            new AmigaMemEntry(4, 0xB, 0x000800, 0x09F4, 0x09F4),
            new AmigaMemEntry(5, 0xB, 0x0011F4, 0x4E36, 0x4E36),
            new AmigaMemEntry(0, 0x1, 0x02FF3E, 0x0372, 0x0372),
            new AmigaMemEntry(0, 0x2, 0x01CEA0, 0x1E04, 0x1E04),
            new AmigaMemEntry(0, 0x1, 0x0302B0, 0x08EA, 0x08EA),
            new AmigaMemEntry(0, 0x1, 0x030B9A, 0x1A46, 0x1A46),
            new AmigaMemEntry(0, 0x2, 0x01ECA4, 0x343E, 0x343E),
            new AmigaMemEntry(0, 0x2, 0x0220E2, 0x149E, 0x149E),
            new AmigaMemEntry(0, 0x2, 0x023580, 0x1866, 0x1866),
            new AmigaMemEntry(0, 0x1, 0x0325E0, 0x0266, 0x0266),
            new AmigaMemEntry(0, 0x1, 0x000000, 0x0000, 0x0000),
            new AmigaMemEntry(0, 0x2, 0x024DE6, 0x01A8, 0x01A8),
            new AmigaMemEntry(0, 0x1, 0x032846, 0x1FEC, 0x1FEC),
            new AmigaMemEntry(0, 0x2, 0x024F8E, 0x13A4, 0x13A4),
            new AmigaMemEntry(0, 0x2, 0x026332, 0x15C4, 0x15C4),
            new AmigaMemEntry(0, 0x2, 0x0278F6, 0x0E2A, 0x0E2A),
            new AmigaMemEntry(0, 0x2, 0x028720, 0x0366, 0x0366),
            new AmigaMemEntry(0, 0x2, 0x028A86, 0x0078, 0x0078),
            new AmigaMemEntry(0, 0x2, 0x028AFE, 0x1392, 0x1392),
            new AmigaMemEntry(0, 0x2, 0x029E90, 0x06E0, 0x06E0),
            new AmigaMemEntry(0, 0x2, 0x02A570, 0x21AE, 0x21AE),
            new AmigaMemEntry(0, 0x1, 0x034832, 0x04FA, 0x04FA),
            new AmigaMemEntry(0, 0x1, 0x034D2C, 0x129E, 0x129E),
            new AmigaMemEntry(0, 0x1, 0x035FCA, 0x09B4, 0x09B4),
            new AmigaMemEntry(0, 0x2, 0x02C71E, 0x04EC, 0x04EC),
            new AmigaMemEntry(2, 0x4, 0x000000, 0x28FC, 0x7D00),
            new AmigaMemEntry(2, 0x4, 0x0028FC, 0x1C2C, 0x7D00),
            new AmigaMemEntry(2, 0x4, 0x004528, 0x1F20, 0x7D00),
            new AmigaMemEntry(2, 0x4, 0x006448, 0x22A8, 0x7D00),
            new AmigaMemEntry(2, 0x1, 0x03697E, 0x033C, 0x7D00),
            new AmigaMemEntry(2, 0x4, 0x0086F0, 0x2DA4, 0x7D00),
            new AmigaMemEntry(2, 0x4, 0x00B494, 0x3008, 0x7D00),
            new AmigaMemEntry(0, 0x2, 0x02CC0A, 0x03C0, 0x03C0),
            new AmigaMemEntry(0, 0x2, 0x02CFCA, 0x13E6, 0x13E6),
            new AmigaMemEntry(0, 0x2, 0x02E3B0, 0x04DE, 0x04DE),
            new AmigaMemEntry(0, 0x2, 0x02E88E, 0x05FA, 0x05FA),
            new AmigaMemEntry(0, 0x2, 0x02EE88, 0x025E, 0x025E),
            new AmigaMemEntry(0, 0x2, 0x02F0E6, 0x0642, 0x0642),
            new AmigaMemEntry(0, 0x2, 0x02F728, 0x19D0, 0x19D0),
            new AmigaMemEntry(0, 0x2, 0x0310F8, 0x00E8, 0x00E8),
            new AmigaMemEntry(0, 0x6, 0x000000, 0x1022, 0x1022),
            new AmigaMemEntry(2, 0x1, 0x036CBA, 0x1A8C, 0x7D00),
            new AmigaMemEntry(0, 0x2, 0x0311E0, 0x58AA, 0x58AA),
            new AmigaMemEntry(0, 0x6, 0x001022, 0x0990, 0x0990),
            new AmigaMemEntry(0, 0x6, 0x0019B2, 0x2C42, 0x2C42),
            new AmigaMemEntry(0, 0x6, 0x0045F4, 0x152C, 0x152C),
            new AmigaMemEntry(0, 0x6, 0x005B20, 0x05B4, 0x05B4),
            new AmigaMemEntry(0, 0x6, 0x0060D4, 0x23B4, 0x23B4),
            new AmigaMemEntry(0, 0x6, 0x008488, 0x1FA4, 0x1FA4),
            new AmigaMemEntry(0, 0x6, 0x00A42C, 0x0D20, 0x0D20),
            new AmigaMemEntry(0, 0x6, 0x00B14C, 0x0528, 0x0528),
            new AmigaMemEntry(0, 0x6, 0x00B674, 0x1608, 0x1608),
            new AmigaMemEntry(0, 0x6, 0x00CC7C, 0x01EA, 0x01EA),
            new AmigaMemEntry(0, 0x6, 0x00CE66, 0x07EA, 0x07EA),
            new AmigaMemEntry(0, 0x6, 0x00D650, 0x00E8, 0x00E8),
            new AmigaMemEntry(0, 0x7, 0x000000, 0x3978, 0x3978),
            new AmigaMemEntry(0, 0x7, 0x003978, 0x1178, 0x1178),
            new AmigaMemEntry(0, 0x7, 0x004AF0, 0x14B0, 0x14B0),
            new AmigaMemEntry(0, 0x7, 0x005FA0, 0x0AA4, 0x0AA4),
            new AmigaMemEntry(0, 0x7, 0x006A44, 0x02DA, 0x02DA),
            new AmigaMemEntry(0, 0x7, 0x006D1E, 0x2674, 0x2674),
            new AmigaMemEntry(0, 0x7, 0x009392, 0x12F0, 0x12F0),
            new AmigaMemEntry(0, 0x7, 0x00A682, 0x5D58, 0x5D58),
            new AmigaMemEntry(0, 0x7, 0x0103DA, 0xA222, 0xA222),
            new AmigaMemEntry(0, 0x8, 0x000000, 0x2E68, 0x2E68),
            new AmigaMemEntry(0, 0x8, 0x002E68, 0x51C6, 0x51C6),
            new AmigaMemEntry(0, 0x8, 0x00802E, 0x13E6, 0x13E6),
            new AmigaMemEntry(0, 0x8, 0x009414, 0x149E, 0x149E),
            new AmigaMemEntry(0, 0x8, 0x00A8B2, 0x58AA, 0x58AA),
            new AmigaMemEntry(0, 0x8, 0x01015C, 0x445C, 0x445C),
            new AmigaMemEntry(0, 0x7, 0x01A5FC, 0x0D90, 0x0D90),
            new AmigaMemEntry(0, 0x7, 0x01B38C, 0x09E4, 0x09E4),
            new AmigaMemEntry(0, 0x7, 0x01BD70, 0x198A, 0x198A),
            new AmigaMemEntry(0, 0x7, 0x01D6FA, 0x25D2, 0x25D2),
            new AmigaMemEntry(0, 0x8, 0x0145B8, 0x2430, 0x2430),
            new AmigaMemEntry(0, 0x8, 0x0169E8, 0x1316, 0x1316),
            new AmigaMemEntry(0, 0x8, 0x017CFE, 0x0220, 0x0220),
            new AmigaMemEntry(0, 0x8, 0x017F1E, 0x05EA, 0x05EA),
            new AmigaMemEntry(0, 0x8, 0x018508, 0x043C, 0x043C),
            new AmigaMemEntry(0, 0x8, 0x018944, 0x08EA, 0x08EA),
            new AmigaMemEntry(0, 0x8, 0x01922E, 0x1478, 0x1478),
            new AmigaMemEntry(0, 0x8, 0x01A6A6, 0x432E, 0x432E),
            new AmigaMemEntry(0, 0x8, 0x01E9D4, 0x06CE, 0x06CE),
            new AmigaMemEntry(3, 0x9, 0x000000, 0x0800, 0x0800),
            new AmigaMemEntry(4, 0x9, 0x000800, 0x0CC6, 0x0CC6),
            new AmigaMemEntry(5, 0x9, 0x0014C6, 0x13B8, 0x13B8),
            new AmigaMemEntry(0, 0x1, 0x038746, 0x189A, 0x189A),
            new AmigaMemEntry(0, 0x1, 0x039FE0, 0x07D8, 0x07D8),
            new AmigaMemEntry(0, 0x1, 0x03A7B8, 0x0462, 0x0462),
            new AmigaMemEntry(0, 0x1, 0x03AC1A, 0x0FA8, 0x0FA8),
            new AmigaMemEntry(0, 0xA, 0x0222CA, 0x672E, 0x672E),
            new AmigaMemEntry(0, 0x8, 0x000000, 0x0000, 0x0000),
            new AmigaMemEntry(0, 0x8, 0x000000, 0x0000, 0x0000),
            new AmigaMemEntry(0, 0x8, 0x000000, 0x0000, 0x0000),
            new AmigaMemEntry(0, 0x8, 0x01F0A2, 0x247C, 0x247C),
            new AmigaMemEntry(1, 0x2, 0x036A8A, 0x08C0, 0x08C0),
            new AmigaMemEntry(1, 0xB, 0x00602A, 0x08C4, 0x3CC0),
            new AmigaMemEntry(0, 0xA, 0x0289F8, 0x4F5A, 0x4F5A),
            new AmigaMemEntry(0, 0xA, 0x02D952, 0x4418, 0x4418),
            new AmigaMemEntry(0, 0xA, 0x031D6A, 0x293C, 0x293C),
            new AmigaMemEntry(0, 0xA, 0x0346A6, 0x3FC8, 0x3FC8),
            new AmigaMemEntry(0, 0x8, 0x000000, 0x0000, 0x0000),
            new AmigaMemEntry(2, 0xB, 0x0068EE, 0x2F94, 0x7D00),
            new AmigaMemEntry(2, 0xB, 0x009882, 0x33C0, 0x7D00),
        };

        private static readonly AmigaMemEntry[] MemListAmigaEN =
        {
            new AmigaMemEntry( 0, 0x1, 0x000000, 0x0000, 0x0000 ),
            new AmigaMemEntry( 0, 0x1, 0x000000, 0x1A3C, 0x1A3C ),
            new AmigaMemEntry( 0, 0x1, 0x001A3C, 0x2E34, 0x2E34 ),
            new AmigaMemEntry( 0, 0x1, 0x004870, 0x69F8, 0x69F8 ),
            new AmigaMemEntry( 0, 0x1, 0x00B268, 0x45CE, 0x45CE ),
            new AmigaMemEntry( 0, 0x1, 0x00F836, 0x0EFA, 0x0EFA ),
            new AmigaMemEntry( 0, 0x1, 0x010730, 0x0D26, 0x0D26 ),
            new AmigaMemEntry( 1, 0x1, 0x011456, 0x0494, 0x3CC0 ),
            new AmigaMemEntry( 0, 0x2, 0x000000, 0x2674, 0x2674 ),
            new AmigaMemEntry( 0, 0x1, 0x0118EA, 0x2BB6, 0x2BB6 ),
            new AmigaMemEntry( 0, 0x1, 0x0144A0, 0x2BB4, 0x2BB4 ),
            new AmigaMemEntry( 0, 0x1, 0x017054, 0x0426, 0x0426 ),
            new AmigaMemEntry( 0, 0x1, 0x01747A, 0x1852, 0x1852 ),
            new AmigaMemEntry( 0, 0x1, 0x018CCC, 0x0594, 0x0594 ),
            new AmigaMemEntry( 0, 0x1, 0x019260, 0x13F0, 0x13F0 ),
            new AmigaMemEntry( 0, 0x1, 0x01A650, 0x079E, 0x079E ),
            new AmigaMemEntry( 0, 0x2, 0x002674, 0x56A2, 0x56A2 ),
            new AmigaMemEntry( 6, 0xC, 0x000000, 0x6214, 0x6214 ),
            new AmigaMemEntry( 2, 0x5, 0x000000, 0x2410, 0x7D00 ),
            new AmigaMemEntry( 2, 0x5, 0x002410, 0x7D00, 0x7D00 ),
            new AmigaMemEntry( 3, 0x1, 0x01ADEE, 0x0800, 0x0800 ),
            new AmigaMemEntry( 4, 0x1, 0x01B5EE, 0x0DD8, 0x0DD8 ),
            new AmigaMemEntry( 5, 0x1, 0x01C3C6, 0x1090, 0x1090 ),
            new AmigaMemEntry( 3, 0x1, 0x01D456, 0x0800, 0x0800 ),
            new AmigaMemEntry( 4, 0x1, 0x01DC56, 0x2530, 0x2530 ),
            new AmigaMemEntry( 5, 0x1, 0x020186, 0xFE7A, 0xFE7A ),
            new AmigaMemEntry( 3, 0x2, 0x007D16, 0x0800, 0x0800 ),
            new AmigaMemEntry( 4, 0x2, 0x008516, 0x4C02, 0x4C02 ),
            new AmigaMemEntry( 5, 0x2, 0x00D118, 0xFDBA, 0xFDBA ),
            new AmigaMemEntry( 3, 0xD, 0x000000, 0x0800, 0x0800 ),
            new AmigaMemEntry( 4, 0xD, 0x000800, 0x98B6, 0x98B6 ),
            new AmigaMemEntry( 5, 0xD, 0x00A0B6, 0xD1D8, 0xD1D8 ),
            new AmigaMemEntry( 3, 0x3, 0x000000, 0x0800, 0x0800 ),
            new AmigaMemEntry( 4, 0x3, 0x000800, 0xEE5E, 0xEE5E ),
            new AmigaMemEntry( 5, 0x3, 0x00F65E, 0xFD08, 0xFD08 ),
            new AmigaMemEntry( 3, 0xA, 0x000000, 0x0800, 0x0800 ),
            new AmigaMemEntry( 4, 0xA, 0x000800, 0x1B00, 0x1B00 ),
            new AmigaMemEntry( 5, 0xA, 0x002300, 0x5E58, 0x5E58 ),
            new AmigaMemEntry( 3, 0xA, 0x008158, 0x0800, 0x0800 ),
            new AmigaMemEntry( 4, 0xA, 0x008958, 0x99DC, 0x99DC ),
            new AmigaMemEntry( 5, 0xA, 0x012334, 0xFF9A, 0xFF9A ),
            new AmigaMemEntry( 3, 0xB, 0x000000, 0x0800, 0x0800 ),
            new AmigaMemEntry( 4, 0xB, 0x000800, 0x09F4, 0x09F4 ),
            new AmigaMemEntry( 5, 0xB, 0x0011F4, 0x4E3A, 0x4E3A ),
            new AmigaMemEntry( 0, 0x1, 0x030000, 0x0372, 0x0372 ),
            new AmigaMemEntry( 0, 0x2, 0x01CED2, 0x1E04, 0x1E04 ),
            new AmigaMemEntry( 0, 0x1, 0x030372, 0x08EA, 0x08EA ),
            new AmigaMemEntry( 0, 0x1, 0x030C5C, 0x1A46, 0x1A46 ),
            new AmigaMemEntry( 0, 0x2, 0x01ECD6, 0x343E, 0x343E ),
            new AmigaMemEntry( 0, 0x2, 0x022114, 0x149E, 0x149E ),
            new AmigaMemEntry( 0, 0x2, 0x0235B2, 0x1866, 0x1866 ),
            new AmigaMemEntry( 0, 0x1, 0x0326A2, 0x0266, 0x0266 ),
            new AmigaMemEntry( 0, 0x1, 0x000000, 0x0000, 0x0000 ),
            new AmigaMemEntry( 0, 0x2, 0x024E18, 0x01A8, 0x01A8 ),
            new AmigaMemEntry( 0, 0x1, 0x032908, 0x1FEC, 0x1FEC ),
            new AmigaMemEntry( 0, 0x2, 0x024FC0, 0x13A4, 0x13A4 ),
            new AmigaMemEntry( 0, 0x2, 0x026364, 0x15C4, 0x15C4 ),
            new AmigaMemEntry( 0, 0x2, 0x027928, 0x0E2A, 0x0E2A ),
            new AmigaMemEntry( 0, 0x2, 0x028752, 0x0366, 0x0366 ),
            new AmigaMemEntry( 0, 0x2, 0x028AB8, 0x0078, 0x0078 ),
            new AmigaMemEntry( 0, 0x2, 0x028B30, 0x1392, 0x1392 ),
            new AmigaMemEntry( 0, 0x2, 0x029EC2, 0x06E0, 0x06E0 ),
            new AmigaMemEntry( 0, 0x2, 0x02A5A2, 0x21AE, 0x21AE ),
            new AmigaMemEntry( 0, 0x1, 0x0348F4, 0x04FA, 0x04FA ),
            new AmigaMemEntry( 0, 0x1, 0x034DEE, 0x129E, 0x129E ),
            new AmigaMemEntry( 0, 0x1, 0x03608C, 0x09B4, 0x09B4 ),
            new AmigaMemEntry( 0, 0x2, 0x02C750, 0x04EC, 0x04EC ),
            new AmigaMemEntry( 2, 0x4, 0x000000, 0x28FC, 0x7D00 ),
            new AmigaMemEntry( 2, 0x4, 0x0028FC, 0x1C2C, 0x7D00 ),
            new AmigaMemEntry( 2, 0x4, 0x004528, 0x1F20, 0x7D00 ),
            new AmigaMemEntry( 2, 0x4, 0x006448, 0x22A8, 0x7D00 ),
            new AmigaMemEntry( 2, 0x1, 0x036A40, 0x033C, 0x7D00 ),
            new AmigaMemEntry( 2, 0x4, 0x0086F0, 0x2DA4, 0x7D00 ),
            new AmigaMemEntry( 2, 0x4, 0x00B494, 0x3008, 0x7D00 ),
            new AmigaMemEntry( 0, 0x2, 0x02CC3C, 0x03C0, 0x03C0 ),
            new AmigaMemEntry( 0, 0x2, 0x02CFFC, 0x13E6, 0x13E6 ),
            new AmigaMemEntry( 0, 0x2, 0x02E3E2, 0x04DE, 0x04DE ),
            new AmigaMemEntry( 0, 0x2, 0x02E8C0, 0x05FA, 0x05FA ),
            new AmigaMemEntry( 0, 0x2, 0x02EEBA, 0x025E, 0x025E ),
            new AmigaMemEntry( 0, 0x2, 0x02F118, 0x0642, 0x0642 ),
            new AmigaMemEntry( 0, 0x2, 0x02F75A, 0x19D0, 0x19D0 ),
            new AmigaMemEntry( 0, 0x2, 0x03112A, 0x00E8, 0x00E8 ),
            new AmigaMemEntry( 0, 0x6, 0x000000, 0x1022, 0x1022 ),
            new AmigaMemEntry( 2, 0x1, 0x036D7C, 0x1A8C, 0x7D00 ),
            new AmigaMemEntry( 0, 0x2, 0x031212, 0x58AA, 0x58AA ),
            new AmigaMemEntry( 0, 0x6, 0x001022, 0x0990, 0x0990 ),
            new AmigaMemEntry( 0, 0x6, 0x0019B2, 0x2C42, 0x2C42 ),
            new AmigaMemEntry( 0, 0x6, 0x0045F4, 0x152C, 0x152C ),
            new AmigaMemEntry( 0, 0x6, 0x005B20, 0x05B4, 0x05B4 ),
            new AmigaMemEntry( 0, 0x6, 0x0060D4, 0x23B4, 0x23B4 ),
            new AmigaMemEntry( 0, 0x6, 0x008488, 0x1FA4, 0x1FA4 ),
            new AmigaMemEntry( 0, 0x6, 0x00A42C, 0x0D20, 0x0D20 ),
            new AmigaMemEntry( 0, 0x6, 0x00B14C, 0x0528, 0x0528 ),
            new AmigaMemEntry( 0, 0x6, 0x00B674, 0x1608, 0x1608 ),
            new AmigaMemEntry( 0, 0x6, 0x00CC7C, 0x01EA, 0x01EA ),
            new AmigaMemEntry( 0, 0x6, 0x00CE66, 0x07EA, 0x07EA ),
            new AmigaMemEntry( 0, 0x6, 0x00D650, 0x00E8, 0x00E8 ),
            new AmigaMemEntry( 0, 0x7, 0x000000, 0x3978, 0x3978 ),
            new AmigaMemEntry( 0, 0x7, 0x003978, 0x1178, 0x1178 ),
            new AmigaMemEntry( 0, 0x7, 0x004AF0, 0x14B0, 0x14B0 ),
            new AmigaMemEntry( 0, 0x7, 0x005FA0, 0x0AA4, 0x0AA4 ),
            new AmigaMemEntry( 0, 0x7, 0x006A44, 0x02DA, 0x02DA ),
            new AmigaMemEntry( 0, 0x7, 0x006D1E, 0x2674, 0x2674 ),
            new AmigaMemEntry( 0, 0x7, 0x009392, 0x12F0, 0x12F0 ),
            new AmigaMemEntry( 0, 0x7, 0x00A682, 0x5D58, 0x5D58 ),
            new AmigaMemEntry( 0, 0x7, 0x0103DA, 0xA222, 0xA222 ),
            new AmigaMemEntry( 0, 0x8, 0x000000, 0x2E68, 0x2E68 ),
            new AmigaMemEntry( 0, 0x8, 0x002E68, 0x51C6, 0x51C6 ),
            new AmigaMemEntry( 0, 0x8, 0x00802E, 0x13E6, 0x13E6 ),
            new AmigaMemEntry( 0, 0x8, 0x009414, 0x149E, 0x149E ),
            new AmigaMemEntry( 0, 0x8, 0x00A8B2, 0x58AA, 0x58AA ),
            new AmigaMemEntry( 0, 0x8, 0x01015C, 0x445C, 0x445C ),
            new AmigaMemEntry( 0, 0x7, 0x01A5FC, 0x0D90, 0x0D90 ),
            new AmigaMemEntry( 0, 0x7, 0x01B38C, 0x09E4, 0x09E4 ),
            new AmigaMemEntry( 0, 0x7, 0x01BD70, 0x198A, 0x198A ),
            new AmigaMemEntry( 0, 0x7, 0x01D6FA, 0x25D2, 0x25D2 ),
            new AmigaMemEntry( 0, 0x8, 0x0145B8, 0x2430, 0x2430 ),
            new AmigaMemEntry( 0, 0x8, 0x0169E8, 0x1316, 0x1316 ),
            new AmigaMemEntry( 0, 0x8, 0x017CFE, 0x0220, 0x0220 ),
            new AmigaMemEntry( 0, 0x8, 0x017F1E, 0x05EA, 0x05EA ),
            new AmigaMemEntry( 0, 0x8, 0x018508, 0x043C, 0x043C ),
            new AmigaMemEntry( 0, 0x8, 0x018944, 0x08EA, 0x08EA ),
            new AmigaMemEntry( 0, 0x8, 0x01922E, 0x1478, 0x1478 ),
            new AmigaMemEntry( 0, 0x8, 0x01A6A6, 0x432E, 0x432E ),
            new AmigaMemEntry( 0, 0x8, 0x01E9D4, 0x06CE, 0x06CE ),
            new AmigaMemEntry( 3, 0x9, 0x000000, 0x0800, 0x0800 ),
            new AmigaMemEntry( 4, 0x9, 0x000800, 0x0CC6, 0x0CC6 ),
            new AmigaMemEntry( 5, 0x9, 0x0014C6, 0x13B8, 0x13B8 ),
            new AmigaMemEntry( 0, 0x1, 0x038808, 0x189A, 0x189A ),
            new AmigaMemEntry( 0, 0x1, 0x03A0A2, 0x07D8, 0x07D8 ),
            new AmigaMemEntry( 0, 0x1, 0x03A87A, 0x0462, 0x0462 ),
            new AmigaMemEntry( 0, 0x1, 0x03ACDC, 0x0FA8, 0x0FA8 ),
            new AmigaMemEntry( 0, 0xA, 0x0222CE, 0x672E, 0x672E ),
            new AmigaMemEntry( 0, 0x8, 0x000000, 0x0000, 0x0000 ),
            new AmigaMemEntry( 0, 0x8, 0x000000, 0x0000, 0x0000 ),
            new AmigaMemEntry( 0, 0x8, 0x000000, 0x0000, 0x0000 ),
            new AmigaMemEntry( 0, 0x8, 0x01F0A2, 0x247C, 0x247C ),
            new AmigaMemEntry( 1, 0x2, 0x036ABC, 0x08C0, 0x08C0 ),
            new AmigaMemEntry( 1, 0xB, 0x00602E, 0x08C4, 0x3CC0 ),
            new AmigaMemEntry( 0, 0xA, 0x0289FC, 0x4F5A, 0x4F5A ),
            new AmigaMemEntry( 0, 0xA, 0x02D956, 0x4418, 0x4418 ),
            new AmigaMemEntry( 0, 0xA, 0x031D6E, 0x293C, 0x293C ),
            new AmigaMemEntry( 0, 0xA, 0x0346AA, 0x3FC8, 0x3FC8 ),
            new AmigaMemEntry( 0, 0x8, 0x000000, 0x0000, 0x0000 ),
            new AmigaMemEntry( 2, 0xB, 0x0068F2, 0x2F94, 0x7D00 ),
            new AmigaMemEntry( 2, 0xB, 0x009886, 0x33C0, 0X7D00 )
        };

        public Video Video { get; set; }

        public ushort CurrentPartId;
        public ushort RequestedNextPart;
        public bool UseSegVideo2;
        public BytePtr SegPalettes;
        public BytePtr SegBytecode;
        public BytePtr SegCinematic;
        public BytePtr SegVideo2;
        public BytePtr MemPtrStart;

        private ushort _numMemList;
        private BytePtr _scriptBakPtr, _scriptCurPtr, _vidBakPtr, _vidCurPtr;

        /// <summary>
        /// Read all entries from memlist.bin. Do not load anything in memory,
        /// this is just a fast way to access the data later based on their id.
        /// </summary>
        public void ReadEntries()
        {
            Stream stream;
            if (Engine.Instance.Settings.Game.Platform == Platform.Amiga)
            {
                stream = Engine.OpenFileRead("bank01");
                if (stream == null)
                {
                    Error("Resource::readEntries() unable to open 'memlist.bin' file");
                    //Error will exit() no need to return or do anything else.
                }
                var entries = Engine.Instance.Settings.Game.Language == Language.FR_FRA
                    ? MemListAmigaFR
                    : MemListAmigaEN;
                ReadEntriesAmiga(entries, 146);
                return;
            }

            var resourceCounter = 0;

            stream = Engine.OpenFileRead("memlist.bin");
            if (stream == null)
            {
                Error("Resource::readEntries() unable to open 'memlist.bin' file");
                //Error will exit() no need to return or do anything else.
            }
            using (var f = new BinaryReader(stream))
            {
                //Prepare stats array
                //memset(resourceSizeStats,0,sizeof(resourceSizeStats));
                //memset(resourceUnitStats,0,sizeof(resourceUnitStats));

                _numMemList = 0;
                var memEntry = new Ptr<MemEntry>(MemList);
                while (true)
                {
                    //System.Diagnostics.Debug.Assert(_numMemList < _memList.Length);
                    memEntry.Value.State = f.ReadByte();
                    memEntry.Value.Type = (ResType) f.ReadByte();
                    memEntry.Value.BufPtr = BytePtr.Null;
                    f.ReadUInt16BigEndian();
                    memEntry.Value.Unk4 = f.ReadUInt16BigEndian();
                    memEntry.Value.RankNum = f.ReadByte();
                    memEntry.Value.BankId = f.ReadByte();
                    memEntry.Value.BankOffset = f.ReadUInt32BigEndian();
                    memEntry.Value.UnkC = f.ReadUInt16BigEndian();
                    memEntry.Value.PackedSize = f.ReadUInt16BigEndian();
                    memEntry.Value.Unk10 = f.ReadUInt16BigEndian();
                    memEntry.Value.Size = f.ReadUInt16BigEndian();

                    if (memEntry.Value.State == MemEntryStateEndOfMemlist)
                    {
                        break;
                    }

                    //Memory tracking
                    if (memEntry.Value.PackedSize == memEntry.Value.Size)
                    {
                        _resourceUnitStats[(int) memEntry.Value.Type, ResSize]++;
                        _resourceUnitStats[StatsTotalSize, ResSize]++;
                    }
                    else
                    {
                        _resourceUnitStats[(int) memEntry.Value.Type, ResCompressed]++;
                        _resourceUnitStats[StatsTotalSize, ResCompressed]++;
                    }

                    _resourceSizeStats[(int) memEntry.Value.Type, ResSize] += memEntry.Value.Size;
                    _resourceSizeStats[StatsTotalSize, ResSize] += memEntry.Value.Size;
                    _resourceSizeStats[(int) memEntry.Value.Type, ResCompressed] += memEntry.Value.PackedSize;
                    _resourceSizeStats[StatsTotalSize, ResCompressed] += memEntry.Value.PackedSize;


                    if (memEntry.Value.State == MemEntryStateEndOfMemlist)
                    {
                        break;
                    }

                    Debug(DebugLevels.DbgRes, "R:0x{0:X2), {1,-17} size={2:D5} (compacted gain={3:F0}%)",
                        resourceCounter,
                        ResTypeToString(memEntry.Value.Type),
                        memEntry.Value.Size,
                        memEntry.Value.Size != 0
                            ? (memEntry.Value.Size - memEntry.Value.PackedSize) / (float) memEntry.Value.Size * 100.0f
                            : 0.0f);

                    resourceCounter++;

                    _numMemList++;
                    memEntry.Offset++;
                }

                Debug(DebugLevels.DbgRes, "\n");
                Debug(DebugLevels.DbgRes, "Total # resources: {0}", resourceCounter);
                Debug(DebugLevels.DbgRes, "Compressed       : {0}",
                    _resourceUnitStats[StatsTotalSize, ResCompressed]);
                Debug(DebugLevels.DbgRes, "Uncompressed     : {0}", _resourceUnitStats[StatsTotalSize, ResSize]);
                Debug(DebugLevels.DbgRes, "Note: {0:F0} % of resources are compressed.",
                    100 * _resourceUnitStats[StatsTotalSize, ResCompressed] / (float) resourceCounter);

                Debug(DebugLevels.DbgRes, "\n");
                Debug(DebugLevels.DbgRes, "Total size (uncompressed) : {0:D7} bytes.",
                    _resourceSizeStats[StatsTotalSize, ResSize]);
                Debug(DebugLevels.DbgRes, "Total size (compressed)   : {0:D7} bytes.",
                    _resourceSizeStats[StatsTotalSize, ResCompressed]);
                Debug(DebugLevels.DbgRes, "Note: Overall compression gain is : {0:F0} %.",
                    (_resourceSizeStats[StatsTotalSize, ResSize] -
                     _resourceSizeStats[StatsTotalSize, ResCompressed]) /
                    (float) _resourceSizeStats[StatsTotalSize, ResSize] * 100);

                Debug(DebugLevels.DbgRes, "\n");
                for (var i = 0; i < 6; i++)
                    Debug(DebugLevels.DbgRes,
                        "Total {0,-17} unpacked size: {1:D7} ({2:F0} % of total unpacked size) packedSize {3:D7} ({4:F0} % of floppy space) gain:({5:F0} %)",
                        ResTypeToString((ResType) i),
                        _resourceSizeStats[i, ResSize],
                        _resourceSizeStats[i, ResSize] / (float) _resourceSizeStats[StatsTotalSize, ResSize] * 100.0f,
                        _resourceSizeStats[i, ResCompressed],
                        _resourceSizeStats[i, ResCompressed] /
                        (float) _resourceSizeStats[StatsTotalSize, ResCompressed] * 100.0f,
                        (_resourceSizeStats[i, ResSize] - _resourceSizeStats[i, ResCompressed]) /
                        (float) _resourceSizeStats[i, ResSize] * 100.0f);

                Debug(DebugLevels.DbgRes, "Note: Damn you sound compression rate!");

                Debug(DebugLevels.DbgRes, "\nTotal bank files:              {0}",
                    _resourceUnitStats[StatsTotalSize, ResSize] +
                    _resourceUnitStats[StatsTotalSize, ResCompressed]);
                for (var i = 0; i < 6; i++)
                    Debug(DebugLevels.DbgRes, "Total {0,-17} files: {1:D3}", ResTypeToString((ResType) i),
                        _resourceUnitStats[i, ResSize] + _resourceUnitStats[i, ResCompressed]);
            }
        }

        private void ReadEntriesAmiga(AmigaMemEntry[] entries, int count)
        {
            _numMemList = (ushort) count;
            for (int i = 0; i < count; ++i)
            {
                MemList[i].Type = (ResType) entries[i].type;
                MemList[i].BankId = entries[i].bank;
                MemList[i].BankOffset = (uint) entries[i].offset;
                MemList[i].PackedSize = (ushort) entries[i].packedSize;
                MemList[i].Size = (ushort) entries[i].unpackedSize;
            }
            MemList[count].State = 0xFF;
        }

        public void AllocMemBlock()
        {
            MemPtrStart = new byte[MemBlockSize];
            _scriptBakPtr = _scriptCurPtr = MemPtrStart;
            _vidBakPtr =
                _vidCurPtr =
                    MemPtrStart + MemBlockSize -
                    0x800 * 16; //0x800 = 2048, so we have 32KB free for vidBack and vidCur
            UseSegVideo2 = false;
        }

        // Protection screen and cinematic don't need the player and enemies polygon data
        // so _memList[video2Index] is never loaded for those parts of the game. When
        // needed (for action phrases) _memList[video2Index] is always loaded with 0x11
        // (as seen in memListParts).
        public void SetupPart(ushort partId)
        {
            if (partId == CurrentPartId)
                return;

            if (partId < GamePartFirst || partId > GamePartLast)
                Error("Resource::setupPart() ec=0x{0:X} invalid partId", partId);

            var memListPartIndex = (ushort) (partId - GamePartFirst);

            var paletteIndex = (byte) MemListParts[memListPartIndex, MemlistPartPalette];
            var codeIndex = (byte) MemListParts[memListPartIndex, MemlistPartCode];
            var videoCinematicIndex = (byte) MemListParts[memListPartIndex, MemlistPartPolyCinematic];
            var video2Index = (byte) MemListParts[memListPartIndex, MemlistPartVideo2];

            // Mark all resources as located on harddrive.
            InvalidateAll();

            MemList[paletteIndex].State = MemEntryStateLoadMe;
            MemList[codeIndex].State = MemEntryStateLoadMe;
            MemList[videoCinematicIndex].State = MemEntryStateLoadMe;

            // This is probably a cinematic or a non interactive part of the game.
            // Player and enemy polygons are not needed.
            if (video2Index != MemlistPartNone)
                MemList[video2Index].State = MemEntryStateLoadMe;


            LoadMarkedAsNeeded();

            SegPalettes = MemList[paletteIndex].BufPtr;
            SegBytecode = MemList[codeIndex].BufPtr;
            SegCinematic = MemList[videoCinematicIndex].BufPtr;

            Debug($"data: {SegBytecode[0]},{SegBytecode.ToUInt16BigEndian(1):X2}");

            // This is probably a cinematic or a non interactive part of the game.
            // Player and enemy polygons are not needed.
            if (video2Index != MemlistPartNone)
                SegVideo2 = MemList[video2Index].BufPtr;

            Debug(DebugLevels.DbgRes, "");
            Debug(DebugLevels.DbgRes, "setupPart({0})", partId - GamePartFirst);
            Debug(DebugLevels.DbgRes, "Loaded resource {0} ({1}) in segPalettes.", paletteIndex,
                ResTypeToString(MemList[paletteIndex].Type));
            Debug(DebugLevels.DbgRes, "Loaded resource {0} ({1}) in segBytecode.", codeIndex,
                ResTypeToString(MemList[codeIndex].Type));
            Debug(DebugLevels.DbgRes, "Loaded resource {0} ({1}) in segCinematic.", videoCinematicIndex,
                ResTypeToString(MemList[videoCinematicIndex].Type));

            if (video2Index != MemlistPartNone)
                Debug(DebugLevels.DbgRes, "Loaded resource {0} ({1}) in _segVideo2.", video2Index,
                    ResTypeToString(MemList[video2Index].Type));

            CurrentPartId = partId;

            // _scriptCurPtr is changed in this.load();
            _scriptBakPtr = _scriptCurPtr;
        }

        public void InvalidateRes()
        {
            Ptr<MemEntry> me = MemList;
            var i = _numMemList;
            while (i-- != 0)
            {
                if (me.Value.Type <= ResType.RtPolyAnim || me.Value.Type > (ResType) 6)
                {
                    // 6 WTF ?!?! ResType goes up to 5 !!
                    me.Value.State = MemEntryStateNotNeeded;
                }
                ++me.Offset;
            }
            _scriptCurPtr = _scriptBakPtr;
        }

        /// <summary>
        /// This method serves two purpose:
        /// - Load parts in memory segments (palette,code,video1,video2)
        ///          or
        /// - Load a resource in memory
        /// This is decided based on the resourceId. If it does not match a mementry id it is supposed to
        /// be a part id.
        /// </summary>
        /// <param name="resourceId"></param>
        public void LoadPartsOrMemoryEntry(ushort resourceId)
        {
            if (resourceId > _numMemList)
            {
                RequestedNextPart = resourceId;
            }
            else
            {
                var me = MemList[resourceId];
                if (me.State == MemEntryStateNotNeeded)
                {
                    me.State = MemEntryStateLoadMe;
                    LoadMarkedAsNeeded();
                }
            }
        }

        public void SaveOrLoad(Serializer ser)
        {
            var loadedList = new byte[64];
            if (ser.Mode == Mode.SmSave)
            {
                BytePtr p = loadedList;
                var q = MemPtrStart;
                while (true)
                {
                    Ptr<MemEntry> it = MemList;
                    var me = Ptr<MemEntry>.Null;
                    var num = _numMemList;
                    while (num-- != 0)
                    {
                        if (it.Value.State == MemEntry.Loaded && it.Value.BufPtr == q)
                        {
                            me = it;
                        }
                        ++it.Offset;
                    }
                    if (me == Ptr<MemEntry>.Null)
                    {
                        break;
                    }
                    //assert(p < loadedList + 64);
                    p.Value = (byte) me.Offset;
                    p.Offset++;
                    q += me.Value.Size;
                }
            }

            Entry[] entries =
            {
                Entry.Create(loadedList, 64, 1),
                Entry.Create(this, o => o.CurrentPartId, 1),
                Entry.Create(this, o => o._scriptBakPtr, 1),
                Entry.Create(this, o => o._scriptCurPtr, 1),
                Entry.Create(this, o => o._vidBakPtr, 1),
                Entry.Create(this, o => o._vidCurPtr, 1),
                Entry.Create(this, o => o.UseSegVideo2, 1),
                Entry.Create(this, o => o.SegPalettes, 1),
                Entry.Create(this, o => o.SegBytecode, 1),
                Entry.Create(this, o => o.SegCinematic, 1),
                Entry.Create(this, o => o.SegVideo2, 1),
            };

            ser.SaveOrLoadEntries(entries);
            if (ser.Mode == Mode.SmLoad)
            {
                BytePtr p = loadedList;
                var q = MemPtrStart;
                while (p.Value != 0)
                {
                    var me = MemList[p.Value];
                    p.Offset++;
                    ReadBank(me, q);
                    me.BufPtr = q;
                    me.State = MemEntry.Loaded;
                    q += me.Size;
                }
            }
        }

        private static string ResTypeToString(ResType type)
        {
            if ((int) type >= ResTypes.Length)
                return "RT_UNKNOWN";
            return ResTypes[(int) type];
        }

        private void InvalidateAll()
        {
            for (var i = 0; i < _numMemList; i++)
            {
                MemList[i].State = MemEntryStateNotNeeded;
            }

            _scriptCurPtr = MemPtrStart;
        }

        /// <summary>
        /// Go over every resource and check if they are marked at "MEMENTRY_STATE_LOAD_ME".
        /// Load them in memory and mark them are MEMENTRY_STATE_LOADED
        /// </summary>
        private void LoadMarkedAsNeeded()
        {
            while (true)
            {
                MemEntry me = null;

                // get resource with max rankNum
                byte maxNum = 0;
                var i = _numMemList;
                Ptr<MemEntry> it = MemList;
                while (i-- != 0)
                {
                    if (it.Value.State == MemEntryStateLoadMe && maxNum <= it.Value.RankNum)
                    {
                        maxNum = it.Value.RankNum;
                        me = it.Value;
                    }
                    it.Offset++;
                }

                if (me == null)
                {
                    break; // no entry found
                }

                // At this point the resource descriptor should be pointed to "me"
                // "That's what she said"

                BytePtr loadDestination;
                if (me.Type == ResType.RtPolyAnim)
                {
                    loadDestination = _vidCurPtr;
                }
                else
                {
                    loadDestination = _scriptCurPtr;
                    if (me.Size > _vidBakPtr.Offset - _scriptCurPtr.Offset)
                    {
                        Warning("Resource::load() not enough memory");
                        me.State = MemEntryStateNotNeeded;
                        continue;
                    }
                }

                if (me.BankId == 0)
                {
                    Warning("Resource::load() ec=0x{0:X} (me.bankId == 0)", 0xF00);
                    me.State = MemEntryStateNotNeeded;
                }
                else
                {
                    Debug(DebugLevels.DbgBank,
                        "Resource::load() bufPos={0:X} size={1:X} type={2:X} pos={3:X} bankId={4:X}",
                        loadDestination.Offset - MemPtrStart.Offset, me.PackedSize, me.Type, me.BankOffset, me.BankId);
                    ReadBank(me, loadDestination);
                    if (me.Type == ResType.RtPolyAnim)
                    {
                        Video.CopyPagePtr(_vidCurPtr);
                        me.State = MemEntryStateNotNeeded;
                    }
                    else
                    {
                        me.BufPtr = loadDestination;
                        me.State = MemEntryStateLoaded;
                        _scriptCurPtr += me.Size;
                    }
                }
            }
        }

        private void ReadBank(MemEntry me, BytePtr dstBuf)
        {
            var n = Array.IndexOf(MemList, me);
            Debug(DebugLevels.DbgBank, "Resource::readBank({0})", n);

            var bk = new Bank();
            if (!bk.Read(me, dstBuf))
            {
                Error("Resource::readBank() unable to unpack entry {0}\n", n);
            }
        }
    }
}