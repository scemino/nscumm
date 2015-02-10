//
//  IMuseDigital_Tables.cs
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

namespace NScumm.Core.Audio.IMuse
{
    partial class IMuseDigital
    {
        struct ImuseDigTable
        {
            public byte transitionType;
            public short soundId;
            public string name;
            public byte attribPos;
            public byte hookId;
            public string filename;

            public ImuseDigTable(byte transitionType, short soundId, string name, byte attribPos, byte hookId, string filename)
            {
                this.transitionType = transitionType;
                this.soundId = soundId;
                this.name = name;
                this.attribPos = attribPos;
                this.hookId = hookId;
                this.filename = filename;
            }
        }

        struct ImuseRoomMap
        {
            public sbyte roomId;
            public byte stateIndex1;
            public byte offset;
            public byte stateIndex2;
            public byte attribPos;
            public byte stateIndex3;

            public ImuseRoomMap(sbyte roomId, byte stateIndex1, byte offset, byte stateIndex2, byte attribPos, byte stateIndex3)
            {
                this.roomId = roomId;
                this.stateIndex1 = stateIndex1;
                this.offset = offset;
                this.stateIndex2 = stateIndex2;
                this.attribPos = attribPos;
                this.stateIndex3 = stateIndex3;
            }
        }

        static readonly ImuseRoomMap[] _digStateMusicMap =
            {
            new ImuseRoomMap(0, 0, 0, 0, 0, 0),
            new ImuseRoomMap(1, 0, 0, 0, 0, 0),
            new ImuseRoomMap(2, 2, 0, 0, 0, 0),
            new ImuseRoomMap(4, 3, 0, 0, 0, 0),
            new ImuseRoomMap(5, 3, 0, 0, 0, 0),
            new ImuseRoomMap(6, 3, 0, 0, 0, 0),
            new ImuseRoomMap(7, 3, 0, 0, 0, 0),
            new ImuseRoomMap(8, 4, 0, 0, 0, 0),
            new ImuseRoomMap(9, 5, 0, 0, 0, 0),
            new ImuseRoomMap(10, 4, 0, 0, 0, 0),
            new ImuseRoomMap(12, 5, 0, 0, 0, 0),
            new ImuseRoomMap(14, 5, 0, 0, 0, 0),
            new ImuseRoomMap(15, 6, 29, 7, 0, 0),
            new ImuseRoomMap(16, 8, 0, 0, 0, 0),
            new ImuseRoomMap(17, 1, 0, 0, 0, 0),
            new ImuseRoomMap(18, 9, 0, 0, 0, 0),
            new ImuseRoomMap(19, 9, 0, 0, 0, 0),
            new ImuseRoomMap(20, 6, 0, 0, 0, 0),
            new ImuseRoomMap(21, 6, 0, 0, 0, 0),
            new ImuseRoomMap(22, 44, 0, 0, 0, 0),
            new ImuseRoomMap(23, 10, 7, 0, 0, 0),
            new ImuseRoomMap(24, 26, 0, 0, 0, 0),
            new ImuseRoomMap(25, 17, 0, 0, 0, 0),
            new ImuseRoomMap(26, 17, 0, 0, 0, 0),
            new ImuseRoomMap(27, 18, 0, 0, 0, 0),
            new ImuseRoomMap(28, 1, 0, 0, 0, 0),
            new ImuseRoomMap(29, 20, 0, 0, 0, 0),
            new ImuseRoomMap(30, 22, 0, 0, 0, 0),
            new ImuseRoomMap(31, 23, 0, 0, 0, 0),
            new ImuseRoomMap(32, 22, 0, 0, 0, 0),
            new ImuseRoomMap(33, 26, 0, 0, 0, 0),
            new ImuseRoomMap(34, 24, 0, 0, 0, 0),
            new ImuseRoomMap(35, 1, 0, 0, 0, 0),
            new ImuseRoomMap(36, 1, 0, 0, 0, 0),
            new ImuseRoomMap(37, 42, 0, 0, 0, 0),
            new ImuseRoomMap(38, 43, 0, 0, 0, 0),
            new ImuseRoomMap(39, 44, 0, 0, 0, 0),
            new ImuseRoomMap(40, 1, 0, 0, 0, 0),
            new ImuseRoomMap(41, 43, 0, 0, 0, 0),
            new ImuseRoomMap(42, 44, 0, 0, 0, 0),
            new ImuseRoomMap(43, 43, 0, 0, 0, 0),
            new ImuseRoomMap(44, 45, 117, 45, 114, 46),
            new ImuseRoomMap(47, 1, 0, 0, 0, 0),
            new ImuseRoomMap(48, 43, 0, 0, 0, 0),
            new ImuseRoomMap(49, 44, 0, 0, 0, 0),
            new ImuseRoomMap(51, 1, 0, 0, 0, 0),
            new ImuseRoomMap(53, 28, 0, 0, 0, 0),
            new ImuseRoomMap(54, 28, 0, 0, 0, 0),
            new ImuseRoomMap(55, 29, 0, 0, 0, 0),
            new ImuseRoomMap(56, 29, 0, 0, 0, 0),
            new ImuseRoomMap(57, 29, 0, 0, 0, 0),
            new ImuseRoomMap(58, 31, 0, 0, 0, 0),
            new ImuseRoomMap(59, 1, 0, 0, 0, 0),
            new ImuseRoomMap(60, 37, 0, 0, 0, 0),
            new ImuseRoomMap(61, 39, 0, 0, 0, 0),
            new ImuseRoomMap(62, 38, 0, 0, 0, 0),
            new ImuseRoomMap(63, 39, 0, 0, 0, 0),
            new ImuseRoomMap(64, 39, 0, 0, 0, 0),
            new ImuseRoomMap(65, 40, 0, 0, 0, 0),
            new ImuseRoomMap(67, 40, 0, 0, 0, 0),
            new ImuseRoomMap(68, 39, 0, 0, 0, 0),
            new ImuseRoomMap(69, 1, 0, 0, 0, 0),
            new ImuseRoomMap(70, 49, 0, 0, 0, 0),
            new ImuseRoomMap(73, 50, 0, 0, 0, 0),
            new ImuseRoomMap(75, 51, 0, 0, 0, 0),
            new ImuseRoomMap(76, 1, 0, 0, 0, 0),
            new ImuseRoomMap(77, 52, 7, 0, 0, 0),
            new ImuseRoomMap(78, 63, 0, 0, 0, 0),
            new ImuseRoomMap(79, 1, 0, 0, 0, 0),
            new ImuseRoomMap(82, 21, 0, 0, 0, 0),
            new ImuseRoomMap(85, 1, 0, 0, 0, 0),
            new ImuseRoomMap(86, 0, 0, 0, 0, 0),
            new ImuseRoomMap(89, 33, 6, 35, 5, 34),
            new ImuseRoomMap(90, 16, 0, 0, 0, 0),
            new ImuseRoomMap(91, 57, 0, 0, 0, 0),
            new ImuseRoomMap(88, 32, 0, 0, 0, 0),
            new ImuseRoomMap(92, 25, 0, 0, 0, 0),
            new ImuseRoomMap(93, 0, 0, 0, 0, 0),
            new ImuseRoomMap(95, 19, 0, 0, 0, 0),
            new ImuseRoomMap(80, 41, 0, 0, 0, 0),
            new ImuseRoomMap(81, 48, 0, 0, 0, 0),
            new ImuseRoomMap(83, 27, 0, 0, 0, 0),
            new ImuseRoomMap(94, 36, 0, 0, 0, 0),
            new ImuseRoomMap(40, 1, 0, 0, 0, 0),
            new ImuseRoomMap(96, 13, 0, 0, 0, 0),
            new ImuseRoomMap(97, 14, 0, 0, 0, 0),
            new ImuseRoomMap(98, 11, 0, 0, 0, 0),
            new ImuseRoomMap(99, 15, 0, 0, 0, 0),
            new ImuseRoomMap(100, 17, 0, 0, 0, 0),
            new ImuseRoomMap(101, 38, 0, 0, 0, 0),
            new ImuseRoomMap(103, 0, 0, 0, 0, 0),
            new ImuseRoomMap(104, 0, 0, 0, 0, 0),
            new ImuseRoomMap(11, 44, 0, 0, 0, 0),
            new ImuseRoomMap(3, 47, 0, 0, 0, 0),
            new ImuseRoomMap(105, 30, 128, 29, 0, 0),
            new ImuseRoomMap(106, 0, 0, 0, 0, 0),
            new ImuseRoomMap(107, 1, 0, 0, 0, 0),
            new ImuseRoomMap(108, 1, 0, 0, 0, 0),
            new ImuseRoomMap(47, 1, 0, 0, 0, 0),
            new ImuseRoomMap(50, 1, 0, 0, 0, 0),
            new ImuseRoomMap(52, 0, 0, 0, 0, 0),
            new ImuseRoomMap(71, 1, 0, 0, 0, 0),
            new ImuseRoomMap(13, 1, 0, 0, 0, 0),
            new ImuseRoomMap(72, 1, 0, 0, 0, 0),
            new ImuseRoomMap(46, 33, 6, 35, 5, 34),
            new ImuseRoomMap(74, 1, 0, 0, 0, 0),
            new ImuseRoomMap(84, 1, 0, 0, 0, 0),
            new ImuseRoomMap(66, 1, 0, 0, 0, 0),
            new ImuseRoomMap(102, 1, 0, 0, 0, 0),
            new ImuseRoomMap(109, 1, 0, 0, 0, 0),
            new ImuseRoomMap(110, 2, 0, 0, 0, 0),
            new ImuseRoomMap(45, 1, 0, 0, 0, 0),
            new ImuseRoomMap(87, 1, 0, 0, 0, 0),
            new ImuseRoomMap(111, 1, 0, 0, 0, 0),
            new ImuseRoomMap(-1, 1, 0, 0, 0, 0)
        };

        static readonly ImuseDigTable[] _digSeqMusicTable = {
            new ImuseDigTable(0, 2000, "SEQ_NULL",            0, 0, ""),
            new ImuseDigTable(0, 2005, "seqLogo",             0, 0, ""),
            new ImuseDigTable(0, 2010, "seqIntro",            0, 0, ""),
            new ImuseDigTable(6, 2020, "seqExplosion1b",      0, 0, ""),
            new ImuseDigTable(3, 2030, "seqAstTunnel1a",      0, 0, "SEQ(AS~1.IMU"),
            new ImuseDigTable(6, 2031, "seqAstTunnel2b",      0, 0, ""),
            new ImuseDigTable(4, 2032, "seqAstTunnel3a",      0, 0, "SEQ(AS~2.IMU"),
            new ImuseDigTable(5, 2040, "seqToPlanet1b",       0, 0, ""),
            new ImuseDigTable(4, 2045, "seqArgBegin",         0, 0, "SEQ(AR~1.IMU"),
            new ImuseDigTable(4, 2046, "seqArgEnd",           0, 0, "SEQ(AR~2.IMU"),
            new ImuseDigTable(4, 2050, "seqWreckGhost",       0, 0, "SEQ(GH~1.IMU"),
            new ImuseDigTable(4, 2060, "seqCanyonGhost",      0, 0, "SEQ(GH~2.IMU"),
            new ImuseDigTable(0, 2070, "seqBrinkFall",        0, 0, ""),
            new ImuseDigTable(4, 2080, "seqPanUpCanyon",      0, 0, "SEQ(PA~1.IMU"),
            new ImuseDigTable(6, 2091, "seqAirlockTunnel1b",  0, 0, ""),
            new ImuseDigTable(6, 2100, "seqTramToMu",         0, 0, ""),
            new ImuseDigTable(6, 2101, "seqTramFromMu",       0, 0, ""),
            new ImuseDigTable(6, 2102, "seqTramToTomb",       0, 0, ""),
            new ImuseDigTable(6, 2103, "seqTramFromTomb",     0, 0, ""),
            new ImuseDigTable(6, 2104, "seqTramToPlan",       0, 0, ""),
            new ImuseDigTable(6, 2105, "seqTramFromPlan",     0, 0, ""),
            new ImuseDigTable(6, 2106, "seqTramToMap",        0, 0, ""),
            new ImuseDigTable(6, 2107, "seqTramFromMap",      0, 0, ""),
            new ImuseDigTable(6, 2108, "seqTramToCath",       0, 0, ""),
            new ImuseDigTable(6, 2109, "seqTramFromCath",     0, 0, ""),
            new ImuseDigTable(0, 2110, "seqMuseumGhost",      0, 0, ""),
            new ImuseDigTable(0, 2120, "seqSerpentAppears",   0, 0, ""),
            new ImuseDigTable(0, 2130, "seqSerpentEats",      0, 0, ""),
            new ImuseDigTable(6, 2140, "seqBrinkRes1b",       0, 0, ""),
            new ImuseDigTable(4, 2141, "seqBrinkRes2a",       0, 0, "SEQ(BR~1.IMU"),
            new ImuseDigTable(3, 2150, "seqLockupEntry",      0, 0, "SEQ(BR~1.IMU"),
            new ImuseDigTable(0, 2160, "seqSerpentExplodes",  0, 0, ""),
            new ImuseDigTable(4, 2170, "seqSwimUnderwater",   0, 0, "SEQ(DE~1.IMU"),
            new ImuseDigTable(4, 2175, "seqWavesPlunge",      0, 0, "SEQ(PL~1.IMU"),
            new ImuseDigTable(0, 2180, "seqCryptOpens",       0, 0, ""),
            new ImuseDigTable(0, 2190, "seqGuardsFight",      0, 0, ""),
            new ImuseDigTable(3, 2200, "seqCreatorRes1.1a",   0, 0, "SEQ(CR~1.IMU"),
            new ImuseDigTable(6, 2201, "seqCreatorRes1.2b",   0, 0, ""),
            new ImuseDigTable(6, 2210, "seqMaggieCapture1b",  0, 0, ""),
            new ImuseDigTable(3, 2220, "seqStealCrystals",    0, 0, "SEQ(BR~1.IMU"),
            new ImuseDigTable(0, 2230, "seqGetByMonster",     0, 0, ""),
            new ImuseDigTable(6, 2240, "seqKillMonster1b",    0, 0, ""),
            new ImuseDigTable(3, 2250, "seqCreatorRes2.1a",   0, 0, "SEQ(CR~2.IMU"),
            new ImuseDigTable(6, 2251, "seqCreatorRes2.2b",   0, 0, ""),
            new ImuseDigTable(4, 2252, "seqCreatorRes2.3a",   0, 0, "SEQ(CR~3.IMU"),
            new ImuseDigTable(0, 2260, "seqMaggieInsists",    0, 0, ""),
            new ImuseDigTable(0, 2270, "seqBrinkHelpCall",    0, 0, ""),
            new ImuseDigTable(3, 2280, "seqBrinkCrevice1a",   0, 0, "SEQ(BR~2.IMU"),
            new ImuseDigTable(3, 2281, "seqBrinkCrevice2a",   0, 0, "SEQ(BR~3.IMU"),
            new ImuseDigTable(6, 2290, "seqCathAccess1b",     0, 0, ""),
            new ImuseDigTable(4, 2291, "seqCathAccess2a",     0, 0, "SEQ(CA~1.IMU"),
            new ImuseDigTable(3, 2300, "seqBrinkAtGenerator", 0, 0, "SEQ(BR~1.IMU"),
            new ImuseDigTable(6, 2320, "seqFightBrink1b",     0, 0, ""),
            new ImuseDigTable(6, 2340, "seqMaggieDies1b",     0, 0, ""),
            new ImuseDigTable(6, 2346, "seqMaggieRes1b",      0, 0, ""),
            new ImuseDigTable(4, 2347, "seqMaggieRes2a",      0, 0, "SEQ(MA~1.IMU"),
            new ImuseDigTable(0, 2350, "seqCreatureFalls",    0, 0, ""),
            new ImuseDigTable(5, 2360, "seqFinale1b",         0, 0, ""),
            new ImuseDigTable(3, 2370, "seqFinale2a",         0, 0, "SEQ(FI~1.IMU"),
            new ImuseDigTable(6, 2380, "seqFinale3b1",        0, 0, ""),
            new ImuseDigTable(6, 2390, "seqFinale3b2",        0, 0, ""),
            new ImuseDigTable(3, 2400, "seqFinale4a",         0, 0, "SEQ(FI~2.IMU"),
            new ImuseDigTable(3, 2410, "seqFinale5a",         0, 0, "SEQ(FI~3.IMU"),
            new ImuseDigTable(3, 2420, "seqFinale6a",         0, 0, "SEQ(FI~4.IMU"),
            new ImuseDigTable(3, 2430, "seqFinale7a",         0, 0, "SE3D2B~5.IMU"),
            new ImuseDigTable(6, 2440, "seqFinale8b",         0, 0, ""),
            new ImuseDigTable(4, 2450, "seqFinale9a",         0, 0, "SE313B~5.IMU"),
            new ImuseDigTable(0,   -1, "",                    0, 0, "")
        };

        static readonly ImuseDigTable[] _digStateMusicTable =
        {
            new ImuseDigTable(0, 1000, "STATE_NULL", 0, 0, ""),             /* 00 */
            new ImuseDigTable(0, 1001, "stateNoChange", 0, 0, ""),             /* 01 */
            new ImuseDigTable(3, 1100, "stateAstShip", 2, 0, "ASTERO~1.IMU"), /* 02 */
            new ImuseDigTable(3, 1120, "stateAstClose", 2, 0, "ASTERO~2.IMU"), /* 03 */
            new ImuseDigTable(3, 1140, "stateAstInside", 0, 0, "ASTERO~3.IMU"), /* 04 */
            new ImuseDigTable(3, 1150, "stateAstCore", 0, 2, "ASTERO~4.IMU"), /* 05 */
            new ImuseDigTable(3, 1200, "stateCanyonClose", 0, 1, "CANYON~1.IMU"), /* 06 */
            new ImuseDigTable(3, 1205, "stateCanyonClose_m", 0, 0, "CANYON~2.IMU"), /* 07 */
            new ImuseDigTable(3, 1210, "stateCanyonOver", 0, 1, "CANYON~3.IMU"), /* 08 */
            new ImuseDigTable(3, 1220, "stateCanyonWreck", 0, 1, "CANYON~4.IMU"), /* 09 */
            new ImuseDigTable(3, 1300, "stateNexusCanyon", 10, 0, "NEXUS(~1.IMU"), /* 10 */
            new ImuseDigTable(3, 1310, "stateNexusPlan", 10, 0, "NEXUS(~1.IMU"), /* 11 */
            new ImuseDigTable(3, 1320, "stateNexusRamp", 10, 0, "NEXUS(~2.IMU"), /* 12 */
            new ImuseDigTable(3, 1330, "stateNexusMuseum", 10, 0, "NEXUS(~3.IMU"), /* 13 */
            new ImuseDigTable(3, 1340, "stateNexusMap", 10, 0, "NEXUS(~4.IMU"), /* 14 */
            new ImuseDigTable(3, 1350, "stateNexusTomb", 10, 0, "NE3706~5.IMU"), /* 15 */
            new ImuseDigTable(3, 1360, "stateNexusCath", 10, 0, "NE3305~5.IMU"), /* 16 */
            new ImuseDigTable(3, 1370, "stateNexusAirlock", 0, 0, "NE2D3A~5.IMU"), /* 17 */
            new ImuseDigTable(3, 1380, "stateNexusPowerOff", 0, 1, "NE8522~5.IMU"), /* 18 */
            new ImuseDigTable(3, 1400, "stateMuseumTramNear", 0, 1, "TRAM(M~1.IMU"), /* 19 */
            new ImuseDigTable(3, 1410, "stateMuseumTramFar", 0, 0, "TRAM(M~2.IMU"), /* 20 */
            new ImuseDigTable(3, 1420, "stateMuseumLockup", 0, 0, "MUSEUM~1.IMU"), /* 21 */
            new ImuseDigTable(3, 1433, "stateMuseumPool", 22, 1, "MUSEUM~2.IMU"), /* 22 */
            new ImuseDigTable(3, 1436, "stateMuseumSpire", 22, 2, "MUSEUM~3.IMU"), /* 23 */
            new ImuseDigTable(3, 1440, "stateMuseumMuseum", 22, 2, "MUSEUM~4.IMU"), /* 24 */
            new ImuseDigTable(3, 1450, "stateMuseumLibrary", 0, 0, "MUB575~5.IMU"), /* 25 */
            new ImuseDigTable(3, 1460, "stateMuseumCavern", 0, 0, "MUF9BE~5.IMU"), /* 26 */
            new ImuseDigTable(3, 1500, "stateTombTramNear", 0, 1, "TRAM(T~1.IMU"), /* 27 */
            new ImuseDigTable(3, 1510, "stateTombBase", 28, 2, "TOMB(A~1.IMU"), /* 28 */
            new ImuseDigTable(3, 1520, "stateTombSpire", 28, 2, "TOMB(A~2.IMU"), /* 29 */
            new ImuseDigTable(3, 1530, "stateTombCave", 28, 2, "TOMB(A~3.IMU"), /* 30 */
            new ImuseDigTable(3, 1540, "stateTombCrypt", 31, 1, "TOMB(C~1.IMU"), /* 31 */
            new ImuseDigTable(3, 1550, "stateTombGuards", 31, 1, "TOMB(C~2.IMU"), /* 32 */
            new ImuseDigTable(3, 1560, "stateTombInner", 0, 1, "TOMB(I~1.IMU"), /* 33 */
            new ImuseDigTable(3, 1570, "stateTombCreator1", 0, 0, "TOMB(C~3.IMU"), /* 34 */
            new ImuseDigTable(3, 1580, "stateTombCreator2", 0, 0, "TOMB(C~4.IMU"), /* 35 */
            new ImuseDigTable(3, 1600, "statePlanTramNear", 0, 1, "TRAM(P~1.IMU"), /* 36 */
            new ImuseDigTable(3, 1610, "statePlanTramFar", 0, 0, "TRAM(P~2.IMU"), /* 37 */
            new ImuseDigTable(3, 1620, "statePlanBase", 38, 2, "PLAN(A~1.IMU"), /* 38 */
            new ImuseDigTable(3, 1630, "statePlanSpire", 38, 2, "PLAN(A~2.IMU"), /* 39 */
            new ImuseDigTable(3, 1650, "statePlanDome", 0, 0, "PLAN(D~1.IMU"), /* 40 */
            new ImuseDigTable(3, 1700, "stateMapTramNear", 0, 1, "TRAM(M~3.IMU"), /* 41 */
            new ImuseDigTable(3, 1710, "stateMapTramFar", 0, 0, "TRAM(M~4.IMU"), /* 42 */
            new ImuseDigTable(3, 1720, "stateMapCanyon", 43, 2, "MAP(AM~1.IMU"), /* 43 */
            new ImuseDigTable(3, 1730, "stateMapExposed", 43, 2, "MAP(AM~2.IMU"), /* 44 */
            new ImuseDigTable(3, 1750, "stateMapNestEmpty", 43, 2, "MAP(AM~4.IMU"), /* 45 */
            new ImuseDigTable(3, 1760, "stateMapNestMonster", 0, 0, "MAP(MO~1.IMU"), /* 46 */
            new ImuseDigTable(3, 1770, "stateMapKlein", 0, 0, "MAP(KL~1.IMU"), /* 47 */
            new ImuseDigTable(3, 1800, "stateCathTramNear", 0, 1, "TRAM(C~1.IMU"), /* 48 */
            new ImuseDigTable(3, 1810, "stateCathTramFar", 0, 0, "TRAM(C~2.IMU"), /* 49 */
            new ImuseDigTable(3, 1820, "stateCathLab", 50, 1, "CATH(A~1.IMU"), /* 50 */
            new ImuseDigTable(3, 1830, "stateCathOutside", 50, 1, "CATH(A~2.IMU"), /* 51 */
            new ImuseDigTable(3, 1900, "stateWorldMuseum", 52, 0, "WORLD(~1.IMU"), /* 52 */
            new ImuseDigTable(3, 1901, "stateWorldPlan", 52, 0, "WORLD(~2.IMU"), /* 53 */
            new ImuseDigTable(3, 1902, "stateWorldTomb", 52, 0, "WORLD(~3.IMU"), /* 54 */
            new ImuseDigTable(3, 1903, "stateWorldMap", 52, 0, "WORLD(~4.IMU"), /* 55 */
            new ImuseDigTable(3, 1904, "stateWorldCath", 52, 0, "WO3227~5.IMU"), /* 56 */
            new ImuseDigTable(3, 1910, "stateEye1", 0, 0, "EYE1~1.IMU"),   /* 57 */
            new ImuseDigTable(3, 1911, "stateEye2", 0, 0, "EYE2~1.IMU"),   /* 58 */
            new ImuseDigTable(3, 1912, "stateEye3", 0, 0, "EYE3~1.IMU"),   /* 59 */
            new ImuseDigTable(3, 1913, "stateEye4", 0, 0, "EYE4~1.IMU"),   /* 60 */
            new ImuseDigTable(3, 1914, "stateEye5", 0, 0, "EYE5~1.IMU"),   /* 61 */
            new ImuseDigTable(3, 1915, "stateEye6", 0, 0, "EYE6~1.IMU"),   /* 62 */
            new ImuseDigTable(3, 1916, "stateEye7", 0, 0, "EYE7~1.IMU"),   /* 63 */
            new ImuseDigTable(0, -1, "", 0, 0, "")
        };

        struct ImuseFtStateTable
        {
            public string audioName;
            public byte transitionType;
            public byte volume;
            public string name;
        }

        struct ImuseFtSeqTable
        {
            public string audioName;
            public byte transitionType;
            public byte volume;
        }

        static readonly ImuseFtStateTable[] _ftStateMusicTable =
            {
                new ImuseFtStateTable{ audioName = "",         transitionType = 0,  volume = 0,    name = "STATE_NULL"          },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 127,  name = "stateKstandOutside"  },
                new ImuseFtStateTable{ audioName = "kinside",  transitionType = 2,  volume = 127,  name = "stateKstandInside"   },
                new ImuseFtStateTable{ audioName = "moshop",   transitionType = 3,  volume = 64,   name = "stateMoesInside"     },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateMoesOutside"    },
                new ImuseFtStateTable{ audioName = "mellover", transitionType = 2,  volume = 127,  name = "stateMellonAbove"    },
                new ImuseFtStateTable{ audioName = "radloop",  transitionType = 3,  volume = 28,   name = "stateTrailerOutside" },
                new ImuseFtStateTable{ audioName = "radloop",  transitionType = 3,  volume = 58,   name = "stateTrailerInside"  },
                new ImuseFtStateTable{ audioName = "radloop",  transitionType = 3,  volume = 127,  name = "stateTodShop"        },
                new ImuseFtStateTable{ audioName = "junkgate", transitionType = 2,  volume = 127,  name = "stateJunkGate"       },
                new ImuseFtStateTable{ audioName = "junkover", transitionType = 3,  volume = 127,  name = "stateJunkAbove"      },
                new ImuseFtStateTable{ audioName = "gastower", transitionType = 2,  volume = 127,  name = "stateGasTower"       },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateTowerAlarm"     },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateCopsOnGround"   },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateCopsAround"     },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateMoesRuins"      },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateKstandNight"    },
                new ImuseFtStateTable{ audioName = "trukblu2", transitionType = 2,  volume = 127,  name = "stateTruckerTalk"    },
                new ImuseFtStateTable{ audioName = "stretch",  transitionType = 2,  volume = 127,  name = "stateMumblyPeg"      },
                new ImuseFtStateTable{ audioName = "kstand",   transitionType = 2,  volume = 100,  name = "stateRanchOutside"   },
                new ImuseFtStateTable{ audioName = "kinside",  transitionType = 2,  volume = 127,  name = "stateRanchInside"    },
                new ImuseFtStateTable{ audioName = "desert",   transitionType = 2,  volume = 127,  name = "stateWreckedTruck"   },
                new ImuseFtStateTable{ audioName = "opening",  transitionType = 2,  volume = 100,  name = "stateGorgeVista"     },
                new ImuseFtStateTable{ audioName = "caveopen", transitionType = 2,  volume = 127,  name = "stateCaveOpen"       },
                new ImuseFtStateTable{ audioName = "cavecut1", transitionType = 2,  volume = 127,  name = "stateCaveOuter"      },
                new ImuseFtStateTable{ audioName = "cavecut1", transitionType = 1,  volume = 127,  name = "stateCaveMiddle"     },
                new ImuseFtStateTable{ audioName = "cave",     transitionType = 2,  volume = 127,  name = "stateCaveInner"      },
                new ImuseFtStateTable{ audioName = "corville", transitionType = 2,  volume = 127,  name = "stateCorvilleFront"  },
                new ImuseFtStateTable{ audioName = "mines",    transitionType = 2,  volume = 127,  name = "stateMineField"      },
                new ImuseFtStateTable{ audioName = "bunyman3", transitionType = 2,  volume = 127,  name = "stateBunnyStore"     },
                new ImuseFtStateTable{ audioName = "stretch",  transitionType = 2,  volume = 127,  name = "stateStretchBen"     },
                new ImuseFtStateTable{ audioName = "saveme",   transitionType = 2,  volume = 127,  name = "stateBenPleas"       },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateBenConvinces"   },
                new ImuseFtStateTable{ audioName = "derby",    transitionType = 3,  volume = 127,  name = "stateDemoDerby"      },
                new ImuseFtStateTable{ audioName = "fire",     transitionType = 3,  volume = 127,  name = "stateLightMyFire"    },
                new ImuseFtStateTable{ audioName = "derby",    transitionType = 3,  volume = 127,  name = "stateDerbyChase"     },
                new ImuseFtStateTable{ audioName = "carparts", transitionType = 2,  volume = 127,  name = "stateVultureCarParts" },
                new ImuseFtStateTable{ audioName = "cavecut1", transitionType = 2,  volume = 127,  name = "stateVulturesInside" },
                new ImuseFtStateTable{ audioName = "mines",    transitionType = 2,  volume = 127,  name = "stateFactoryRear"    },
                new ImuseFtStateTable{ audioName = "croffice", transitionType = 2,  volume = 127,  name = "stateCorleyOffice"   },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateCorleyHall"     },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateProjRoom"       },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateMMRoom"         },
                new ImuseFtStateTable{ audioName = "bumper",   transitionType = 2,  volume = 127,  name = "stateBenOnBumper"    },
                new ImuseFtStateTable{ audioName = "benump",   transitionType = 2,  volume = 127,  name = "stateBenOnBack"      },
                new ImuseFtStateTable{ audioName = "plane",    transitionType = 2,  volume = 127,  name = "stateInCargoPlane"   },
                new ImuseFtStateTable{ audioName = "saveme",   transitionType = 2,  volume = 127,  name = "statePlaneControls"  },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateCliffHanger1"   },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateCliffHanger2"   },
            };

        static readonly string[] _ftSeqNames =
            {

                "SEQ_NULL",
                "seqLogo",
                "seqOpenFlick",
                "seqBartender",
                "seqBenWakes",
                "seqPhotoScram",
                "seqClimbChain",
                "seqDogChase",
                "seqDogSquish",
                "seqDogHoist",
                "seqCopsArrive",
                "seqCopsLand",
                "seqCopsLeave",
                "seqCopterFlyby",
                "seqCopterCrash",
                "seqMoGetsParts",
                "seqMoFixesBike",
                "seqFirstGoodbye",
                "seqCopRoadblock",
                "seqDivertCops",
                "seqMurder",
                "seqCorleyDies",
                "seqTooLateAtMoes",
                "seqPicture",
                "seqNewsReel",
                "seqCopsInspect",
                "seqHijack",
                "seqNestolusAtRanch",
                "seqRipLimo",
                "seqGorgeTurn",
                "seqCavefishTalk",
                "seqArriveCorville",
                "seqSingleBunny",
                "seqBunnyArmy",
                "seqArriveAtMines",
                "seqArriveAtVultures",
                "seqMakePlan",
                "seqShowPlan",
                "seqDerbyStart",
                "seqLightBales",
                "seqNestolusBBQ",
                "seqCallSecurity",
                "seqFilmFail",
                "seqFilmBurn",
                "seqRipSpeech",
                "seqExposeRip",
                "seqRipEscape",
                "seqRareMoment",
                "seqFanBunnies",
                "seqRipDead",
                "seqFuneral",
                "seqCredits"         
            };

        static readonly ImuseFtSeqTable[] _ftSeqMusicTable =
            {
                new ImuseFtSeqTable { audioName = "",         transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "opening",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "barbeat",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "barwarn",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0, },

                new ImuseFtSeqTable { audioName = "benwakes", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "barwarn",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "swatben",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "dogattak", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 4,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 4,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "cops2",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "cops2",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "cops2",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "bunymrch", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 4,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "melcut",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "tada",     transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 4,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "trucker",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "cops2",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "barwarn",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "murder",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "murder2",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "corldie",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "barwarn",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "picture",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "ripintro", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "trucker",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "hosed",    transitionType = 2,  volume = 127 },

                new ImuseFtSeqTable { audioName = "ripdead",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "nesranch", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "scolding", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "desert",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "cavecut1", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "caveamb",  transitionType = 2,  volume = 80 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "castle",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "bunymrch", transitionType = 2,  volume = 105 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "valkyrs",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "melcut",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "veltures", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "sorry",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "makeplan", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "castle",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "derby",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "fire",     transitionType = 3,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "saveme",   transitionType = 3,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "scolding", transitionType = 2,  volume = 127 },

                new ImuseFtSeqTable { audioName = "cops2",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "sorry",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "sorry",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "caveamb",  transitionType = 2,  volume = 85 },
                new ImuseFtSeqTable { audioName = "tada",     transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "expose",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 4,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "mocoup",   transitionType = 2,  volume = 127 },

                new ImuseFtSeqTable { audioName = "ripscram", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "valkyrs",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "ripdead",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "funeral",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "moshop",   transitionType = 3,  volume = 64 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "bornbad",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "hammvox",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "legavox",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "chances",  transitionType = 2,  volume = 90 },
            };

    }
}

