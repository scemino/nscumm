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

namespace NScumm.Scumm.Audio.IMuse.IMuseDigital
{
    partial class IMuseDigital
    {
        struct ImuseComiTable
        {
            public byte TransitionType;
            public short SoundId;
            public string Name;
            public byte AttribPos;
            public byte HookId;
            public short FadeOutDelay;
            public string Filename;

            public ImuseComiTable(byte transitionType, short soundId, string name, byte attribPos, byte hookId, short fadeOutDelay, string filename)
            {
                TransitionType = transitionType;
                SoundId = soundId;
                Name = name;
                AttribPos = attribPos;
                HookId = hookId;
                FadeOutDelay = fadeOutDelay;
                Filename = filename;
            }
        }

        static readonly ImuseComiTable[] _comiSeqMusicTable = new []
        {
            new ImuseComiTable(0, 2000, "SEQ_NULL", 0, 0, 0, ""),
            new ImuseComiTable(0, 2100, "seqINTRO", 0, 0, 0, ""),
            new ImuseComiTable(3, 2105, "seqInterlude1", 0, 0, 60, "2105-I~1.IMX"),
            new ImuseComiTable(8, 2110, "seqLastBoat", 0, 1, 0, ""),
            new ImuseComiTable(0, 2115, "seqSINK_SHIP", 0, 0, 0, ""),
            new ImuseComiTable(0, 2120, "seqCURSED_RING", 0, 0, 60, ""),
            new ImuseComiTable(3, 2200, "seqInterlude2", 0, 0, 60, "2200-I~1.IMX"),
            new ImuseComiTable(3, 2210, "seqKidnapped", 0, 0, 60, "2210-K~1.IMX"),
            new ImuseComiTable(8, 2220, "seqSnakeVomits", 0, 1, 0, ""),
            new ImuseComiTable(8, 2222, "seqPopBalloon", 0, 1, 0, ""),
            new ImuseComiTable(3, 2225, "seqDropBalls", 0, 0, 60, "2225-D~1.IMX"),
            new ImuseComiTable(4, 2232, "seqArriveBarber", 0, 0, 60, "2232-A~1.IMX"),
            new ImuseComiTable(3, 2233, "seqAtonal", 0, 0, 60, "2233-A~1.IMX"),
            new ImuseComiTable(3, 2235, "seqShaveHead1", 0, 0, 60, "2235-S~1.IMX"),
            new ImuseComiTable(2, 2236, "seqShaveHead2", 0, 2, 60, "2235-S~1.IMX"),
            new ImuseComiTable(3, 2245, "seqCaberLose", 0, 0, 60, "2245-C~1.IMX"),
            new ImuseComiTable(3, 2250, "seqCaberWin", 0, 0, 60, "2250-C~1.IMX"),
            new ImuseComiTable(3, 2255, "seqDuel1", 0, 0, 60, "2255-D~1.IMX"),
            new ImuseComiTable(2, 2256, "seqDuel2", 0, 2, 60, "2255-D~1.IMX"),
            new ImuseComiTable(2, 2257, "seqDuel3", 0, 3, 60, "2255-D~1.IMX"),
            new ImuseComiTable(3, 2260, "seqBlowUpTree1", 0, 0, 60, "2260-B~1.IMX"),
            new ImuseComiTable(2, 2261, "seqBlowUpTree2", 0, 2, 60, "2260-B~1.IMX"),
            new ImuseComiTable(3, 2275, "seqMonkeys", 0, 0, 60, "2275-M~1.IMX"),
            new ImuseComiTable(9, 2277, "seqAttack", 0, 1, 0, ""),
            new ImuseComiTable(3, 2285, "seqSharks", 0, 0, 60, "2285-S~1.IMX"),
            new ImuseComiTable(3, 2287, "seqTowelWalk", 0, 0, 60, "2287-T~1.IMX"),
            new ImuseComiTable(0, 2293, "seqNICE_BOOTS", 0, 0, 0, ""),
            new ImuseComiTable(0, 2295, "seqBIG_BONED", 0, 0, 0, ""),
            new ImuseComiTable(3, 2300, "seqToBlood", 0, 0, 60, "2300-T~1.IMX"),
            new ImuseComiTable(3, 2301, "seqInterlude3", 0, 0, 60, "2301-I~1.IMX"),
            new ImuseComiTable(3, 2302, "seqRott1", 0, 0, 60, "2302-R~1.IMX"),
            new ImuseComiTable(2, 2304, "seqRott2", 0, 2, 60, "2302-R~1.IMX"),
            new ImuseComiTable(2, 2305, "seqRott2b", 0, 21, 60, "2302-R~1.IMX"),
            new ImuseComiTable(2, 2306, "seqRott3", 0, 3, 60, "2302-R~1.IMX"),
            new ImuseComiTable(2, 2308, "seqRott4", 0, 4, 60, "2302-R~1.IMX"),
            new ImuseComiTable(2, 2309, "seqRott5", 0, 5, 60, "2302-R~1.IMX"),
            new ImuseComiTable(3, 2311, "seqVerse1", 0, 0, 60, "2311-S~1.IMX"),
            new ImuseComiTable(2, 2312, "seqVerse2", 0, 2, 60, "2311-S~1.IMX"),
            new ImuseComiTable(2, 2313, "seqVerse3", 0, 3, 60, "2311-S~1.IMX"),
            new ImuseComiTable(2, 2314, "seqVerse4", 0, 4, 60, "2311-S~1.IMX"),
            new ImuseComiTable(2, 2315, "seqVerse5", 0, 5, 60, "2311-S~1.IMX"),
            new ImuseComiTable(2, 2316, "seqVerse6", 0, 6, 60, "2311-S~1.IMX"),
            new ImuseComiTable(2, 2317, "seqVerse7", 0, 7, 60, "2311-S~1.IMX"),
            new ImuseComiTable(2, 2318, "seqVerse8", 0, 8, 60, "2311-S~1.IMX"),
            new ImuseComiTable(2, 2319, "seqSongEnd", 0, 9, 60, "2311-S~1.IMX"),
            new ImuseComiTable(2, 2336, "seqRiposteLose", 0, 0, 60, "2336-R~1.IMX"),
            new ImuseComiTable(2, 2337, "seqRiposteWin", 0, 0, 60, "2337-R~1.IMX"),
            new ImuseComiTable(2, 2338, "seqInsultLose", 0, 0, 60, "2338-I~1.IMX"),
            new ImuseComiTable(2, 2339, "seqInsultWin", 0, 0, 60, "2339-I~1.IMX"),
            new ImuseComiTable(3, 2340, "seqSwordLose", 0, 0, 60, "1335-S~1.IMX"),
            new ImuseComiTable(3, 2345, "seqSwordWin", 0, 0, 60, "1340-S~1.IMX"),
            new ImuseComiTable(3, 2347, "seqGetMap", 0, 0, 60, "1345-G~1.IMX"),
            new ImuseComiTable(3, 2400, "seqInterlude4", 0, 0, 60, "2400-I~1.IMX"),
            new ImuseComiTable(0, 2405, "seqSHIPWRECK", 0, 0, 0, ""),
            new ImuseComiTable(3, 2408, "seqFakeCredits", 0, 0, 60, "2408-F~1.IMX"),
            new ImuseComiTable(3, 2410, "seqPassOut", 0, 0, 60, "2410-P~1.IMX"),
            new ImuseComiTable(3, 2414, "seqGhostTalk", 0, 0, 60, "2414-G~1.IMX"),
            new ImuseComiTable(2, 2415, "seqGhostWedding", 0, 1, 60, "2414-G~1.IMX"),
            new ImuseComiTable(3, 2420, "seqEruption", 0, 0, 60, "2420-E~1.IMX"),
            new ImuseComiTable(3, 2425, "seqSacrifice", 0, 0, 60, "2425-S~1.IMX"),
            new ImuseComiTable(2, 2426, "seqSacrificeEnd", 0, 1, 60, "2425-S~1.IMX"),
            new ImuseComiTable(3, 2430, "seqScareDigger", 0, 0, 60, "2430-S~1.IMX"),
            new ImuseComiTable(3, 2445, "seqSkullArrive", 0, 0, 60, "2445-S~1.IMX"),
            new ImuseComiTable(3, 2450, "seqFloat", 0, 0, 60, "2450-C~1.IMX"),
            new ImuseComiTable(2, 2451, "seqFall", 0, 1, 60, "2450-C~1.IMX"),
            new ImuseComiTable(2, 2452, "seqUmbrella", 0, 2, 60, "2450-C~1.IMX"),
            new ImuseComiTable(3, 2460, "seqFight", 0, 0, 60, "2460-F~1.IMX"),
            new ImuseComiTable(0, 2465, "seqLAVE_RIDE", 0, 0, 0, ""),
            new ImuseComiTable(0, 2470, "seqMORE_SLAW", 0, 0, 0, ""),
            new ImuseComiTable(0, 2475, "seqLIFT_CURSE", 0, 0, 0, ""),
            new ImuseComiTable(3, 2500, "seqInterlude5", 0, 0, 60, "2500-I~1.IMX"),
            new ImuseComiTable(3, 2502, "seqExitSkycar", 0, 0, 60, "2502-E~1.IMX"),
            new ImuseComiTable(3, 2504, "seqGrow1", 0, 0, 60, "2504-G~1.IMX"),
            new ImuseComiTable(2, 2505, "seqGrow2", 0, 1, 60, "2504-G~1.IMX"),
            new ImuseComiTable(3, 2508, "seqInterlude6", 0, 0, 60, "2508-I~1.IMX"),
            new ImuseComiTable(0, 2515, "seqFINALE", 0, 0, 0, ""),
            new ImuseComiTable(3, 2520, "seqOut", 0, 0, 60, "2520-OUT.IMX"),
            new ImuseComiTable(3, 2530, "seqZap1a", 0, 0, 60, "2530-Z~1.IMX"),
            new ImuseComiTable(2, 2531, "seqZap1b", 0, 1, 60, "2530-Z~1.IMX"),
            new ImuseComiTable(2, 2532, "seqZap1c", 0, 2, 60, "2530-Z~1.IMX"),
            new ImuseComiTable(2, 2540, "seqZap2a", 0, 0, 60, "2540-Z~1.IMX"),
            new ImuseComiTable(2, 2541, "seqZap2b", 0, 1, 60, "2540-Z~1.IMX"),
            new ImuseComiTable(2, 2542, "seqZap2c", 0, 2, 60, "2540-Z~1.IMX"),
            new ImuseComiTable(3, 2550, "seqZap3a", 0, 0, 60, "2550-Z~1.IMX"),
            new ImuseComiTable(2, 2551, "seqZap3b", 0, 1, 60, "2550-Z~1.IMX"),
            new ImuseComiTable(2, 2552, "seqZap3c", 0, 2, 60, "2550-Z~1.IMX"),
            new ImuseComiTable(3, 2560, "seqZap4a", 0, 0, 60, "2560-Z~1.IMX"),
            new ImuseComiTable(2, 2561, "seqZap4b", 0, 1, 60, "2560-Z~1.IMX"),
            new ImuseComiTable(2, 2562, "seqZap4c", 0, 2, 60, "2560-Z~1.IMX"),
            new ImuseComiTable(0, -1, "", 0, 0, 0, "")
        };


        static readonly ImuseComiTable[] _comiStateMusicTable = new []
        {
            new ImuseComiTable(0, 1000, "STATE_NULL", 0, 0, 0, ""),             /* 00 */
            new ImuseComiTable(0, 1001, "stateNoChange", 0, 0, 0, ""),             /* 01 */
            new ImuseComiTable(3, 1098, "stateCredits1", 0, 0, 60, "1098-C~1.IMX"), /* 02 */
            new ImuseComiTable(3, 1099, "stateMenu", 0, 0, 60, "1099-M~1.IMX"), /* 03 */
            new ImuseComiTable(3, 1100, "stateHold1", 4, 0, 60, "1100-H~1.IMX"), /* 04 */
            new ImuseComiTable(3, 1101, "stateWaterline1", 4, 0, 60, "1101-W~1.IMX"), /* 05 */
            new ImuseComiTable(3, 1102, "stateHold2", 6, 1, 60, "1102-H~1.IMX"), /* 06 */
            new ImuseComiTable(3, 1103, "stateWaterline2", 6, 0, 60, "1103-W~1.IMX"), /* 07 */
            new ImuseComiTable(3, 1104, "stateCannon", 0, 0, 60, "1104-C~1.IMX"), /* 08 */
            new ImuseComiTable(3, 1105, "stateTreasure", 0, 0, 60, "1105-T~1.IMX"), /* 09 */
            new ImuseComiTable(3, 1200, "stateFortBase", 10, 1, 60, "1200-F~1.IMX"), /* 10 */
            new ImuseComiTable(3, 1201, "statePreFort", 10, 1, 60, "1201-P~1.IMX"), /* 11 */
            new ImuseComiTable(3, 1202, "statePreVooOut", 12, 0, 60, "1202-P~1.IMX"), /* 12 */
            new ImuseComiTable(3, 1203, "statePreVooIn", 12, 0, 60, "1203-P~1.IMX"), /* 13 */
            new ImuseComiTable(3, 1204, "statePreVooLady", 12, 0, 60, "1204-P~1.IMX"), /* 14 */
            new ImuseComiTable(3, 1205, "stateVoodooOut", 0, 0, 60, "1205-V~1.IMX"), /* 15 */
            new ImuseComiTable(3, 1210, "stateVoodooIn", 0, 0, 60, "1210-V~1.IMX"), /* 16 */
            new ImuseComiTable(12, 1212, "stateVoodooInAlt", 0, 1, 42, "1210-V~1.IMX"), /* 17 */
            new ImuseComiTable(3, 1215, "stateVoodooLady", 0, 0, 60, "1215-V~1.IMX"), /* 18 */
            new ImuseComiTable(3, 1219, "statePrePlundermap", 0, 0, 60, "1219-P~1.IMX"), /* 19 */
            new ImuseComiTable(3, 1220, "statePlundermap", 0, 0, 60, "1220-P~1.IMX"), /* 20 */
            new ImuseComiTable(3, 1222, "statePreCabana", 0, 0, 60, "1222-P~1.IMX"), /* 21 */
            new ImuseComiTable(3, 1223, "stateCabana", 0, 0, 60, "1223-C~1.IMX"), /* 22 */
            new ImuseComiTable(3, 1224, "statePostCabana", 23, 0, 60, "1224-P~1.IMX"), /* 23 */
            new ImuseComiTable(3, 1225, "stateBeachClub", 23, 0, 60, "1225-B~1.IMX"), /* 24 */
            new ImuseComiTable(3, 1230, "stateCliff", 0, 0, 60, "1230-C~1.IMX"), /* 25 */
            new ImuseComiTable(3, 1232, "stateBelly", 0, 0, 48, "1232-B~1.IMX"), /* 26 */
            new ImuseComiTable(3, 1235, "stateQuicksand", 0, 0, 60, "1235-Q~1.IMX"), /* 27 */
            new ImuseComiTable(3, 1240, "stateDangerBeach", 0, 0, 48, "1240-D~1.IMX"), /* 28 */
            new ImuseComiTable(12, 1241, "stateDangerBeachAlt", 0, 2, 48, "1240-D~1.IMX"), /* 29 */
            new ImuseComiTable(3, 1245, "stateRowBoat", 0, 0, 60, "1245-R~1.IMX"), /* 30 */
            new ImuseComiTable(3, 1247, "stateAlongside", 0, 0, 48, "1247-A~1.IMX"), /* 31 */
            new ImuseComiTable(12, 1248, "stateAlongsideAlt", 0, 1, 48, "1247-A~1.IMX"), /* 32 */
            new ImuseComiTable(3, 1250, "stateChimpBoat", 0, 0, 30, "1250-C~1.IMX"), /* 33 */
            new ImuseComiTable(3, 1255, "stateMrFossey", 0, 0, 48, "1255-M~1.IMX"), /* 34 */
            new ImuseComiTable(3, 1259, "statePreTown", 0, 0, 60, "1259-P~1.IMX"), /* 35 */
            new ImuseComiTable(3, 1260, "stateTown", 0, 0, 60, "1260-T~1.IMX"), /* 36 */
            new ImuseComiTable(3, 1264, "statePreMeadow", 0, 0, 60, "1264-P~1.IMX"), /* 37 */
            new ImuseComiTable(3, 1265, "stateMeadow", 0, 0, 60, "1265-M~1.IMX"), /* 38 */
            new ImuseComiTable(3, 1266, "stateMeadowAmb", 0, 0, 60, "1266-M~1.IMX"), /* 39 */
            new ImuseComiTable(3, 1270, "stateWardrobePre", 40, 0, 60, "1270-W~1.IMX"), /* 40 */
            new ImuseComiTable(3, 1272, "statePreShow", 40, 0, 60, "1272-P~1.IMX"), /* 41 */
            new ImuseComiTable(3, 1274, "stateWardrobeShow", 42, 0, 60, "1274-W~1.IMX"), /* 42 */
            new ImuseComiTable(3, 1276, "stateShow", 42, 0, 60, "1276-S~1.IMX"), /* 43 */
            new ImuseComiTable(3, 1277, "stateWardrobeJug", 44, 0, 60, "1277-W~1.IMX"), /* 44 */
            new ImuseComiTable(3, 1278, "stateJuggling", 44, 0, 60, "1278-J~1.IMX"), /* 45 */
            new ImuseComiTable(3, 1279, "statePostShow", 0, 0, 60, "1279-P~1.IMX"), /* 46 */
            new ImuseComiTable(3, 1280, "stateChickenShop", 0, 0, 60, "1280-C~1.IMX"), /* 47 */
            new ImuseComiTable(3, 1285, "stateBarberShop", 48, 0, 60, "1285-B~1.IMX"), /* 48 */
            new ImuseComiTable(3, 1286, "stateVanHelgen", 48, 0, 60, "1286-V~1.IMX"), /* 49 */
            new ImuseComiTable(3, 1287, "stateBill", 48, 0, 60, "1287-B~1.IMX"), /* 50 */
            new ImuseComiTable(3, 1288, "stateHaggis", 48, 0, 60, "1288-H~1.IMX"), /* 51 */
            new ImuseComiTable(3, 1289, "stateRottingham", 48, 0, 60, "1289-R~1.IMX"), /* 52 */
            new ImuseComiTable(3, 1305, "stateDeck", 0, 0, 60, "1305-D~1.IMX"), /* 53 */
            new ImuseComiTable(3, 1310, "stateCombatMap", 0, 0, 60, "1310-C~1.IMX"), /* 54 */
            new ImuseComiTable(3, 1320, "stateShipCombat", 0, 0, 60, "1320-S~1.IMX"), /* 55 */
            new ImuseComiTable(3, 1325, "stateSwordfight", 0, 0, 60, "1325-S~1.IMX"), /* 56 */
            new ImuseComiTable(3, 1327, "stateSwordRott", 0, 0, 60, "1327-S~1.IMX"), /* 57 */
            new ImuseComiTable(3, 1330, "stateTownEdge", 0, 0, 60, "1330-T~1.IMX"), /* 58 */
            new ImuseComiTable(3, 1335, "stateSwordLose", 0, 0, 60, "1335-S~1.IMX"), /* 59 */
            new ImuseComiTable(3, 1340, "stateSwordWin", 0, 0, 60, "1340-S~1.IMX"), /* 60 */
            new ImuseComiTable(3, 1345, "stateGetMap", 0, 0, 60, "1345-G~1.IMX"), /* 61 */
            new ImuseComiTable(3, 1400, "stateWreckBeach", 0, 0, 60, "1400-W~1.IMX"), /* 62 */
            new ImuseComiTable(3, 1405, "stateBloodMap", 63, 0, 60, "1405-B~1.IMX"), /* 63 */
            new ImuseComiTable(3, 1410, "stateClearing", 0, 0, 60, "1410-C~1.IMX"), /* 64 */
            new ImuseComiTable(3, 1415, "stateLighthouse", 63, 0, 60, "1415-L~1.IMX"), /* 65 */
            new ImuseComiTable(3, 1420, "stateVillage", 66, 0, 60, "1420-V~1.IMX"), /* 66 */
            new ImuseComiTable(3, 1423, "stateVolcano", 66, 0, 60, "1423-V~1.IMX"), /* 67 */
            new ImuseComiTable(3, 1425, "stateAltar", 66, 0, 60, "1425-A~1.IMX"), /* 68 */
            new ImuseComiTable(3, 1430, "stateHotelOut", 0, 0, 60, "1430-H~1.IMX"), /* 69 */
            new ImuseComiTable(3, 1435, "stateHotelBar", 70, 0, 60, "1435-H~1.IMX"), /* 70 */
            new ImuseComiTable(3, 1440, "stateHotelIn", 70, 0, 60, "1440-H~1.IMX"), /* 71 */
            new ImuseComiTable(3, 1445, "stateTarotLady", 70, 0, 60, "1445-T~1.IMX"), /* 72 */
            new ImuseComiTable(3, 1447, "stateGoodsoup", 70, 0, 60, "1447-G~1.IMX"), /* 73 */
            new ImuseComiTable(3, 1448, "stateGuestRoom", 0, 0, 60, "1448-G~1.IMX"), /* 74 */
            new ImuseComiTable(3, 1450, "stateWindmill", 63, 0, 60, "1450-W~1.IMX"), /* 75 */
            new ImuseComiTable(3, 1455, "stateCemetary", 0, 0, 60, "1455-C~1.IMX"), /* 76 */
            new ImuseComiTable(3, 1460, "stateCrypt", 77, 0, 60, "1460-C~1.IMX"), /* 77 */
            new ImuseComiTable(3, 1463, "stateGraveDigger", 77, 0, 60, "1463-G~1.IMX"), /* 78 */
            new ImuseComiTable(3, 1465, "stateMonkey1", 0, 0, 60, "1465-M~1.IMX"), /* 79 */
            new ImuseComiTable(3, 1475, "stateStanDark", 0, 0, 60, "1475-S~1.IMX"), /* 80 */
            new ImuseComiTable(3, 1477, "stateStanLight", 0, 0, 60, "1477-S~1.IMX"), /* 81 */
            new ImuseComiTable(3, 1480, "stateEggBeach", 63, 0, 60, "1480-E~1.IMX"), /* 82 */
            new ImuseComiTable(3, 1485, "stateSkullIsland", 0, 0, 60, "1485-S~1.IMX"), /* 83 */
            new ImuseComiTable(3, 1490, "stateSmugglersCave", 0, 0, 60, "1490-S~1.IMX"), /* 84 */
            new ImuseComiTable(3, 1500, "stateLeChuckTalk", 0, 0, 60, "1500-L~1.IMX"), /* 85 */
            new ImuseComiTable(3, 1505, "stateCarnival", 0, 0, 60, "1505-C~1.IMX"), /* 86 */
            new ImuseComiTable(3, 1511, "stateHang", 87, 0, 60, "1511-H~1.IMX"), /* 87 */
            new ImuseComiTable(3, 1512, "stateRum", 87, 0, 60, "1512-RUM.IMX"), /* 88 */
            new ImuseComiTable(3, 1513, "stateTorture", 87, 0, 60, "1513-T~1.IMX"), /* 89 */
            new ImuseComiTable(3, 1514, "stateSnow", 87, 0, 60, "1514-S~1.IMX"), /* 90 */
            new ImuseComiTable(3, 1515, "stateCredits", 0, 0, 60, "1515-C~1.IMX"), /* 91 */
            new ImuseComiTable(3, 1520, "stateCarnAmb", 0, 0, 60, "1520-C~1.IMX"), /* 92 */
            new ImuseComiTable(0, -1, "", 0, 0, 0, "")
        };

        struct ImuseDigTable
        {
            public byte TransitionType;
            public short SoundId;
            public string Name;
            public byte AttribPos;
            public byte HookId;
            public string Filename;

            public ImuseDigTable(byte transitionType, short soundId, string name, byte attribPos, byte hookId, string filename)
            {
                TransitionType = transitionType;
                SoundId = soundId;
                Name = name;
                AttribPos = attribPos;
                HookId = hookId;
                Filename = filename;
            }
        }

        struct ImuseRoomMap
        {
            public sbyte RoomId;
            public byte StateIndex1;
            public byte Offset;
            public byte StateIndex2;
            public byte AttribPos;
            public byte StateIndex3;

            public ImuseRoomMap(sbyte roomId, byte stateIndex1, byte offset, byte stateIndex2, byte attribPos, byte stateIndex3)
            {
                RoomId = roomId;
                StateIndex1 = stateIndex1;
                Offset = offset;
                StateIndex2 = stateIndex2;
                AttribPos = attribPos;
                StateIndex3 = stateIndex3;
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

        static readonly ImuseDigTable[] _digSeqMusicTable =
            {
                new ImuseDigTable(0, 2000, "SEQ_NULL", 0, 0, ""),
                new ImuseDigTable(0, 2005, "seqLogo", 0, 0, ""),
                new ImuseDigTable(0, 2010, "seqIntro", 0, 0, ""),
                new ImuseDigTable(6, 2020, "seqExplosion1b", 0, 0, ""),
                new ImuseDigTable(3, 2030, "seqAstTunnel1a", 0, 0, "SEQ(AS~1.IMU"),
                new ImuseDigTable(6, 2031, "seqAstTunnel2b", 0, 0, ""),
                new ImuseDigTable(4, 2032, "seqAstTunnel3a", 0, 0, "SEQ(AS~2.IMU"),
                new ImuseDigTable(5, 2040, "seqToPlanet1b", 0, 0, ""),
                new ImuseDigTable(4, 2045, "seqArgBegin", 0, 0, "SEQ(AR~1.IMU"),
                new ImuseDigTable(4, 2046, "seqArgEnd", 0, 0, "SEQ(AR~2.IMU"),
                new ImuseDigTable(4, 2050, "seqWreckGhost", 0, 0, "SEQ(GH~1.IMU"),
                new ImuseDigTable(4, 2060, "seqCanyonGhost", 0, 0, "SEQ(GH~2.IMU"),
                new ImuseDigTable(0, 2070, "seqBrinkFall", 0, 0, ""),
                new ImuseDigTable(4, 2080, "seqPanUpCanyon", 0, 0, "SEQ(PA~1.IMU"),
                new ImuseDigTable(6, 2091, "seqAirlockTunnel1b", 0, 0, ""),
                new ImuseDigTable(6, 2100, "seqTramToMu", 0, 0, ""),
                new ImuseDigTable(6, 2101, "seqTramFromMu", 0, 0, ""),
                new ImuseDigTable(6, 2102, "seqTramToTomb", 0, 0, ""),
                new ImuseDigTable(6, 2103, "seqTramFromTomb", 0, 0, ""),
                new ImuseDigTable(6, 2104, "seqTramToPlan", 0, 0, ""),
                new ImuseDigTable(6, 2105, "seqTramFromPlan", 0, 0, ""),
                new ImuseDigTable(6, 2106, "seqTramToMap", 0, 0, ""),
                new ImuseDigTable(6, 2107, "seqTramFromMap", 0, 0, ""),
                new ImuseDigTable(6, 2108, "seqTramToCath", 0, 0, ""),
                new ImuseDigTable(6, 2109, "seqTramFromCath", 0, 0, ""),
                new ImuseDigTable(0, 2110, "seqMuseumGhost", 0, 0, ""),
                new ImuseDigTable(0, 2120, "seqSerpentAppears", 0, 0, ""),
                new ImuseDigTable(0, 2130, "seqSerpentEats", 0, 0, ""),
                new ImuseDigTable(6, 2140, "seqBrinkRes1b", 0, 0, ""),
                new ImuseDigTable(4, 2141, "seqBrinkRes2a", 0, 0, "SEQ(BR~1.IMU"),
                new ImuseDigTable(3, 2150, "seqLockupEntry", 0, 0, "SEQ(BR~1.IMU"),
                new ImuseDigTable(0, 2160, "seqSerpentExplodes", 0, 0, ""),
                new ImuseDigTable(4, 2170, "seqSwimUnderwater", 0, 0, "SEQ(DE~1.IMU"),
                new ImuseDigTable(4, 2175, "seqWavesPlunge", 0, 0, "SEQ(PL~1.IMU"),
                new ImuseDigTable(0, 2180, "seqCryptOpens", 0, 0, ""),
                new ImuseDigTable(0, 2190, "seqGuardsFight", 0, 0, ""),
                new ImuseDigTable(3, 2200, "seqCreatorRes1.1a", 0, 0, "SEQ(CR~1.IMU"),
                new ImuseDigTable(6, 2201, "seqCreatorRes1.2b", 0, 0, ""),
                new ImuseDigTable(6, 2210, "seqMaggieCapture1b", 0, 0, ""),
                new ImuseDigTable(3, 2220, "seqStealCrystals", 0, 0, "SEQ(BR~1.IMU"),
                new ImuseDigTable(0, 2230, "seqGetByMonster", 0, 0, ""),
                new ImuseDigTable(6, 2240, "seqKillMonster1b", 0, 0, ""),
                new ImuseDigTable(3, 2250, "seqCreatorRes2.1a", 0, 0, "SEQ(CR~2.IMU"),
                new ImuseDigTable(6, 2251, "seqCreatorRes2.2b", 0, 0, ""),
                new ImuseDigTable(4, 2252, "seqCreatorRes2.3a", 0, 0, "SEQ(CR~3.IMU"),
                new ImuseDigTable(0, 2260, "seqMaggieInsists", 0, 0, ""),
                new ImuseDigTable(0, 2270, "seqBrinkHelpCall", 0, 0, ""),
                new ImuseDigTable(3, 2280, "seqBrinkCrevice1a", 0, 0, "SEQ(BR~2.IMU"),
                new ImuseDigTable(3, 2281, "seqBrinkCrevice2a", 0, 0, "SEQ(BR~3.IMU"),
                new ImuseDigTable(6, 2290, "seqCathAccess1b", 0, 0, ""),
                new ImuseDigTable(4, 2291, "seqCathAccess2a", 0, 0, "SEQ(CA~1.IMU"),
                new ImuseDigTable(3, 2300, "seqBrinkAtGenerator", 0, 0, "SEQ(BR~1.IMU"),
                new ImuseDigTable(6, 2320, "seqFightBrink1b", 0, 0, ""),
                new ImuseDigTable(6, 2340, "seqMaggieDies1b", 0, 0, ""),
                new ImuseDigTable(6, 2346, "seqMaggieRes1b", 0, 0, ""),
                new ImuseDigTable(4, 2347, "seqMaggieRes2a", 0, 0, "SEQ(MA~1.IMU"),
                new ImuseDigTable(0, 2350, "seqCreatureFalls", 0, 0, ""),
                new ImuseDigTable(5, 2360, "seqFinale1b", 0, 0, ""),
                new ImuseDigTable(3, 2370, "seqFinale2a", 0, 0, "SEQ(FI~1.IMU"),
                new ImuseDigTable(6, 2380, "seqFinale3b1", 0, 0, ""),
                new ImuseDigTable(6, 2390, "seqFinale3b2", 0, 0, ""),
                new ImuseDigTable(3, 2400, "seqFinale4a", 0, 0, "SEQ(FI~2.IMU"),
                new ImuseDigTable(3, 2410, "seqFinale5a", 0, 0, "SEQ(FI~3.IMU"),
                new ImuseDigTable(3, 2420, "seqFinale6a", 0, 0, "SEQ(FI~4.IMU"),
                new ImuseDigTable(3, 2430, "seqFinale7a", 0, 0, "SE3D2B~5.IMU"),
                new ImuseDigTable(6, 2440, "seqFinale8b", 0, 0, ""),
                new ImuseDigTable(4, 2450, "seqFinale9a", 0, 0, "SE313B~5.IMU"),
                new ImuseDigTable(0, -1, "", 0, 0, "")
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
            public string AudioName;
            public byte TransitionType;
            public byte Volume;
            public string Name;
        }

        struct ImuseFtSeqTable
        {
            public string AudioName;
            public byte TransitionType;
            public byte Volume;
        }

        static readonly ImuseFtStateTable[] _ftStateMusicTable =
            {
                new ImuseFtStateTable{ AudioName = "",         TransitionType = 0,  Volume = 0,    Name = "STATE_NULL"          },
                new ImuseFtStateTable{ AudioName = "",         TransitionType = 4,  Volume = 127,  Name = "stateKstandOutside"  },
                new ImuseFtStateTable{ AudioName = "kinside",  TransitionType = 2,  Volume = 127,  Name = "stateKstandInside"   },
                new ImuseFtStateTable{ AudioName = "moshop",   TransitionType = 3,  Volume = 64,   Name = "stateMoesInside"     },
                new ImuseFtStateTable{ AudioName = "melcut",   TransitionType = 2,  Volume = 127,  Name = "stateMoesOutside"    },
                new ImuseFtStateTable{ AudioName = "mellover", TransitionType = 2,  Volume = 127,  Name = "stateMellonAbove"    },
                new ImuseFtStateTable{ AudioName = "radloop",  TransitionType = 3,  Volume = 28,   Name = "stateTrailerOutside" },
                new ImuseFtStateTable{ AudioName = "radloop",  TransitionType = 3,  Volume = 58,   Name = "stateTrailerInside"  },
                new ImuseFtStateTable{ AudioName = "radloop",  TransitionType = 3,  Volume = 127,  Name = "stateTodShop"        },
                new ImuseFtStateTable{ AudioName = "junkgate", TransitionType = 2,  Volume = 127,  Name = "stateJunkGate"       },
                new ImuseFtStateTable{ AudioName = "junkover", TransitionType = 3,  Volume = 127,  Name = "stateJunkAbove"      },
                new ImuseFtStateTable{ AudioName = "gastower", TransitionType = 2,  Volume = 127,  Name = "stateGasTower"       },
                new ImuseFtStateTable{ AudioName = "",         TransitionType = 4,  Volume = 0,    Name = "stateTowerAlarm"     },
                new ImuseFtStateTable{ AudioName = "melcut",   TransitionType = 2,  Volume = 127,  Name = "stateCopsOnGround"   },
                new ImuseFtStateTable{ AudioName = "melcut",   TransitionType = 2,  Volume = 127,  Name = "stateCopsAround"     },
                new ImuseFtStateTable{ AudioName = "melcut",   TransitionType = 2,  Volume = 127,  Name = "stateMoesRuins"      },
                new ImuseFtStateTable{ AudioName = "melcut",   TransitionType = 2,  Volume = 127,  Name = "stateKstandNight"    },
                new ImuseFtStateTable{ AudioName = "trukblu2", TransitionType = 2,  Volume = 127,  Name = "stateTruckerTalk"    },
                new ImuseFtStateTable{ AudioName = "stretch",  TransitionType = 2,  Volume = 127,  Name = "stateMumblyPeg"      },
                new ImuseFtStateTable{ AudioName = "kstand",   TransitionType = 2,  Volume = 100,  Name = "stateRanchOutside"   },
                new ImuseFtStateTable{ AudioName = "kinside",  TransitionType = 2,  Volume = 127,  Name = "stateRanchInside"    },
                new ImuseFtStateTable{ AudioName = "desert",   TransitionType = 2,  Volume = 127,  Name = "stateWreckedTruck"   },
                new ImuseFtStateTable{ AudioName = "opening",  TransitionType = 2,  Volume = 100,  Name = "stateGorgeVista"     },
                new ImuseFtStateTable{ AudioName = "caveopen", TransitionType = 2,  Volume = 127,  Name = "stateCaveOpen"       },
                new ImuseFtStateTable{ AudioName = "cavecut1", TransitionType = 2,  Volume = 127,  Name = "stateCaveOuter"      },
                new ImuseFtStateTable{ AudioName = "cavecut1", TransitionType = 1,  Volume = 127,  Name = "stateCaveMiddle"     },
                new ImuseFtStateTable{ AudioName = "cave",     TransitionType = 2,  Volume = 127,  Name = "stateCaveInner"      },
                new ImuseFtStateTable{ AudioName = "corville", TransitionType = 2,  Volume = 127,  Name = "stateCorvilleFront"  },
                new ImuseFtStateTable{ AudioName = "mines",    TransitionType = 2,  Volume = 127,  Name = "stateMineField"      },
                new ImuseFtStateTable{ AudioName = "bunyman3", TransitionType = 2,  Volume = 127,  Name = "stateBunnyStore"     },
                new ImuseFtStateTable{ AudioName = "stretch",  TransitionType = 2,  Volume = 127,  Name = "stateStretchBen"     },
                new ImuseFtStateTable{ AudioName = "saveme",   TransitionType = 2,  Volume = 127,  Name = "stateBenPleas"       },
                new ImuseFtStateTable{ AudioName = "",         TransitionType = 4,  Volume = 0,    Name = "stateBenConvinces"   },
                new ImuseFtStateTable{ AudioName = "derby",    TransitionType = 3,  Volume = 127,  Name = "stateDemoDerby"      },
                new ImuseFtStateTable{ AudioName = "fire",     TransitionType = 3,  Volume = 127,  Name = "stateLightMyFire"    },
                new ImuseFtStateTable{ AudioName = "derby",    TransitionType = 3,  Volume = 127,  Name = "stateDerbyChase"     },
                new ImuseFtStateTable{ AudioName = "carparts", TransitionType = 2,  Volume = 127,  Name = "stateVultureCarParts" },
                new ImuseFtStateTable{ AudioName = "cavecut1", TransitionType = 2,  Volume = 127,  Name = "stateVulturesInside" },
                new ImuseFtStateTable{ AudioName = "mines",    TransitionType = 2,  Volume = 127,  Name = "stateFactoryRear"    },
                new ImuseFtStateTable{ AudioName = "croffice", TransitionType = 2,  Volume = 127,  Name = "stateCorleyOffice"   },
                new ImuseFtStateTable{ AudioName = "melcut",   TransitionType = 2,  Volume = 127,  Name = "stateCorleyHall"     },
                new ImuseFtStateTable{ AudioName = "",         TransitionType = 4,  Volume = 0,    Name = "stateProjRoom"       },
                new ImuseFtStateTable{ AudioName = "",         TransitionType = 4,  Volume = 0,    Name = "stateMMRoom"         },
                new ImuseFtStateTable{ AudioName = "bumper",   TransitionType = 2,  Volume = 127,  Name = "stateBenOnBumper"    },
                new ImuseFtStateTable{ AudioName = "benump",   TransitionType = 2,  Volume = 127,  Name = "stateBenOnBack"      },
                new ImuseFtStateTable{ AudioName = "plane",    TransitionType = 2,  Volume = 127,  Name = "stateInCargoPlane"   },
                new ImuseFtStateTable{ AudioName = "saveme",   TransitionType = 2,  Volume = 127,  Name = "statePlaneControls"  },
                new ImuseFtStateTable{ AudioName = "",         TransitionType = 4,  Volume = 0,    Name = "stateCliffHanger1"   },
                new ImuseFtStateTable{ AudioName = "",         TransitionType = 4,  Volume = 0,    Name = "stateCliffHanger2"   },
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
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "opening",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "barbeat",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "barwarn",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "benwakes", TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "barwarn",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "swatben",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "dogattak", TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "",         TransitionType = 4,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "",         TransitionType = 4,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "cops2",    TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "cops2",    TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "cops2",    TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "bunymrch", TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "",         TransitionType = 4,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "melcut",   TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "tada",     TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "",         TransitionType = 4,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "trucker",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "cops2",    TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "barwarn",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "murder",   TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "murder2",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "corldie",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "barwarn",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "picture",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "ripintro", TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "trucker",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "hosed",    TransitionType = 2,  Volume = 127 },

                new ImuseFtSeqTable { AudioName = "ripdead",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "nesranch", TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "scolding", TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "desert",   TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "cavecut1", TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "caveamb",  TransitionType = 2,  Volume = 80 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "castle",   TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "bunymrch", TransitionType = 2,  Volume = 105 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "valkyrs",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "melcut",   TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "veltures", TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "sorry",    TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "makeplan", TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "castle",   TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "derby",    TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "fire",     TransitionType = 3,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "saveme",   TransitionType = 3,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "scolding", TransitionType = 2,  Volume = 127 },

                new ImuseFtSeqTable { AudioName = "cops2",    TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "sorry",    TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "sorry",    TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "caveamb",  TransitionType = 2,  Volume = 85 },
                new ImuseFtSeqTable { AudioName = "tada",     TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "expose",   TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 4,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "mocoup",   TransitionType = 2,  Volume = 127 },

                new ImuseFtSeqTable { AudioName = "ripscram", TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "",         TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "valkyrs",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "ripdead",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "funeral",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "moshop",   TransitionType = 3,  Volume = 64 },
                new ImuseFtSeqTable { AudioName = "",         TransitionType = 0,  Volume = 0  },

                new ImuseFtSeqTable { AudioName = "bornbad",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "hammvox",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "legavox",  TransitionType = 2,  Volume = 127 },
                new ImuseFtSeqTable { AudioName = "chances",  TransitionType = 2,  Volume = 90 },
            };

    }
}

