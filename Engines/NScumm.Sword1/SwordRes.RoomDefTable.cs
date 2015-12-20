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

namespace NScumm.Sword1
{
    partial class SwordRes
    {
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
		        layers=new uint[]{ room1_l0, room1_l1, room1_l2 },						//layers
		        grids=new uint[]{ room1_gd1, room1_gd2 },								//grids
		        palettes=new uint[]{ room1_PAL, PARIS1_PAL },								//palettes
		        parallax =new uint[]{ room1_plx,0},												//parallax layers
	        },
            new RoomDef {
                totalLayers=3,													//total_layers		//room 2
		        sizeX=640,												//size_x
		        sizeY=400,												//size_y
		        gridWidth=56,													//grid_width
		        layers=new uint[]{ room2_l0, room2_l1, room2_l2,0},						//layers
		        grids=new uint[]{ room2_gd1, room2_gd2,0},							//grids
		        palettes=new uint[]{ room2_PAL, PARIS1_PAL },								//palettes
		        parallax =new uint[]{0,0},												//parallax layers
	        },
            new RoomDef {
                totalLayers=3,													//total_layers		//room 3
		        sizeX=640,												//size_x
		        sizeY=400,												//size_y
		        gridWidth=56,													//grid_width
		        layers=new uint[]{ room3_l0, room3_l1, room3_l2,0},						//layers
		        grids=new uint[]{ room3_gd1, room3_gd2,0},							//grids
		        palettes=new uint[]{ room3_PAL, PARIS1_PAL },								//palettes
		        parallax =new uint[]{0,0},												//parallax layers
	        },
            new RoomDef {
                totalLayers=3,																			//total_layers		//room 4
		        sizeX=640,																		//size_x
		        sizeY=400,																		//size_y
		        gridWidth=56,																			//grid_width
		        layers=new uint[]{ room4_l0, room4_l1, room4_l2,0},					//layers
		        grids=new uint[]{ room4_gd1, room4_gd2,0},								//grids
		        palettes=new uint[]{ room4_PAL, PARIS1_PAL },									//palettes
		        parallax =new uint[]{0,0},																	//parallax layers
	        },
            new RoomDef {
                totalLayers=3,																			//total_layers		//room 5
		        sizeX=640,																		//size_x
		        sizeY=400,																		//size_y
		        gridWidth=56,																			//grid_width
		        layers=new uint[]{ room5_l0, room5_l1, room5_l2,0},					//layers
		        grids=new uint[]{ room5_gd1, room5_gd2,0},								//grids
		        palettes=new uint[]{ room5_PAL, PARIS1_PAL },									//palettes
		        parallax =new uint[]{0,0},																	//parallax layers
	        },
            new RoomDef {
                totalLayers=2,																			//total_layers		//room 6
		        sizeX=640,																		//size_x
		        sizeY=400,																		//size_y
		        gridWidth=56,																			//grid_width
		        layers=new uint[]{ room6_l0, room6_l1,0,0},								//layers
		        grids=new uint[]{ room6_gd1,0,0},												//grids
		        palettes=new uint[]{ room6_PAL, SEWER_PAL },									//palettes
		        parallax =new uint[]{0,0},																	//parallax layers
	        },
            new RoomDef {
                totalLayers=3,																			//total_layers		//room 7
		        sizeX=640,																		//size_x
		        sizeY=400,																		//size_y
		        gridWidth=56,																			//grid_width
		        layers=new uint[]{ room7_l0, room7_l1, room7_l2,0},					//layers
		        grids=new uint[]{ room7_gd1, room7_gd2,0},								//grids
		        palettes=new uint[]{ room7_PAL, SEWER_PAL },									//palettes
		        parallax =new uint[]{0,0},																	//parallax layers
	        },
            new RoomDef {
                totalLayers=3,																			//total_layers		//room 8
		        sizeX=784,																		//size_x
		        sizeY=400,																		//size_y
		        gridWidth=65,																			//grid_width
		        layers=new uint[]{ room8_l0, room8_l1, room8_l2,0},					//layers
		        grids=new uint[]{ room8_gd1, room8_gd2,0},								//grids
		        palettes=new uint[]{ room8_PAL, PARIS1_PAL },									//palettes
		        parallax =new uint[]{ room8_plx,0},													//parallax layers
	        },

			//------------------------------------------------------------------------
			// PARIS 2

			new RoomDef(
                3,																				//total_layers		//room 9
				640,																			//size_x
				400,																			//size_y
				56,																				//grid_width
				new uint[] {room9_l0, room9_l1, room9_l2,0},						//layers
				new uint[] {room9_gd1, room9_gd2,0},									//grids
				new uint[] { room9_PAL, PARIS2_PAL },										//palettes
				new uint[] {0,0}																		//parallax layers
			),
            new RoomDef(
                2,																				//total_layers		//room 10
				640,																			//size_x
				400,																			//size_y
				56,																				//grid_width
				new uint[] {room10_l0,room10_l1,0,0},								//layers
				new uint[] {room10_gd1,0,0},													//grids
				new uint[] {room10_PAL,R10SPRPAL},										//palettes
				new uint[] {0,0}																		//parallax layers
			),
            new RoomDef(
                3,																				//total_layers		//room 11
				640,																			//size_x
				400,																			//size_y
				56,																				//grid_width
				new uint[] {room11_l0,room11_l1,room11_l2,0},				//layers
				new uint[] {room11_gd1,room11_gd2,0},								//grids
				new uint[] {room11_PAL,PARIS2_PAL},									//palettes
				new uint[] {0,0}																		//parallax layers
			),
            new RoomDef(
                2,																				//total_layers		//room 12
				640,																			//size_x
				400,																			//size_y
				56,																				//grid_width
				new uint[] {room12_l0,room12_l1,0,0},								//layers
				new uint[] {room12_gd1,0,0},													//grids
				new uint[] {room12_PAL,PARIS2_PAL},									//palettes
				new uint[] {0,0}																		//parallax layers
			),
            new RoomDef(
                3,																				//total_layers		//room 13
				976,																			//size_x
				400,																			//size_y
				77,																				//grid_width
				new uint[] {room13_l0,room13_l1,room13_l2,0},				//layers
				new uint[] {room13_gd1,room13_gd2,0},								//grids
				new uint[] {room13_PAL,R13SPRPAL},										//palettes
				new uint[] {0,0}																		//parallax layers
			),
            new RoomDef(
                2,																				//total_layers		//room 14
				640,																			//size_x
				400,																			//size_y
				56,																				//grid_width
				new uint[] {room14_l0,room14_l1,0,0},								//layers
				new uint[] {room14_gd1,0,0},													//grids
				new uint[] {room14_PAL,PARIS2_PAL},									//palettes
				new uint[] {0,0}																		//parallax layers
			),
            new RoomDef(
                3,																				//total_layers		//room 15
				640,																			//size_x
				400,																			//size_y
				56,																				//grid_width
				new uint[]{room15_l0,room15_l1,room15_l2,0},				//layers
				new uint[]{room15_gd1,room15_gd2,0},								//grids
				new uint[]{room15_PAL,PARIS2_PAL},									//palettes
				new uint[]{0,0}																	//parallax layers
			),
            new RoomDef(
                2,																				//total_layers		//room 16
				640,																			//size_x
				400,																			//size_y
				56,																				//grid_width
				new uint[]{R16L0,R16L1,0,0},												//layers
				new uint[]{R16G1,0,0},															//grids
				new uint[]{room16_PAL,PARIS2_PAL},									//palettes
				new uint[]{0,0}																		//parallax layers
			),
            new RoomDef(
                3,																				//total_layers		//room 17
				640,																			//size_x
				400,																			//size_y
				56,																				//grid_width
				new uint[]{room17_l0,room17_l1,room17_l2,0},				//layers
				new uint[]{room17_gd1,room17_gd2,0},								//grids
				new uint[]{room17_PAL,PARIS2_PAL},									//palettes
				new uint[]{0,0}																		//parallax layers
			),
            new RoomDef(
                3,																				//total_layers		//room 18
				640,																			//size_x
				400,																			//size_y
				56,																				//grid_width
				new uint[]{room18_l0,room18_l1,room18_l2,0},				//layers
				new uint[]{room18_gd1,room18_gd2,0},								//grids
				new uint[]{room18_PAL,R18SPRPAL},										//palettes
				new uint[]{0,0}																		//parallax layers
			),
			//------------------------------------------------------------------------
			// IRELAND

			new RoomDef(
                3,													//total_layers		//room 19 - Ireland Street
				848,												//size_x
				864,												//size_y
				69,													//grid_width
				new uint[]{R19L0,R19L1,R19L2,0},			//layers
				new uint[]{R19G1,R19G2,0},						//grids
				new uint[]{R19PAL,R19SPRPAL},					//palettes
				new uint[]{0,0}											//parallax layers
			),
            new RoomDef(
                4,													//total_layers		//room 20 - Macdevitts
				640,												//size_x
				400,												//size_y
				56,													//grid_width
				new uint[]{R20L0,R20L1,R20L2,R20L3},	//layers
				new uint[]{R20G1,R20G2,R20G3},				//grids
				new uint[]{R20PAL,R20SPRPAL},					//palettes
				new uint[]{0,0}											//parallax layers
			),
            new RoomDef(
                3,													//total_layers		//room 21 - Pub Cellar
				640,												//size_x
				400,												//size_y
				56,													//grid_width
				new uint[]{R21L0,R21L1,R21L2,0},			//layers
				new uint[]{R21G1,R21G2,0},						//grids
				new uint[]{R21PAL,SPRITE_PAL},				//palettes
				new uint[]{0,0}											//parallax layers
			),
            new RoomDef(
                2,													//total_layers		//room 22 - Castle Gate
				784,												//size_x
				400,												//size_y
				65,													//grid_width
				new uint[]{R22L0,R22L1,0,0},					//layers
				new uint[]{R22G1,0,0},								//grids
				new uint[]{R22PAL,R22SPRPAL},					//palettes
				new uint[]{0,0}											//parallax layers
			),
            new RoomDef(
                1,													//total_layers		//room 23 - Castle Hay Top
				640,												//size_x
				400,												//size_y
				56,													//grid_width
				new uint[]{R23L0,0,0,0},							//layers
				new uint[]{0,0,0},										//grids
				new uint[]{R23PAL,SPRITE_PAL},				//palettes
				new uint[]{0,0}											//parallax layers
			),
            new RoomDef(
                2,													//total_layers		//room 24 - Castle Yard
				880,												//size_x
				400,												//size_y
				71,													//grid_width
				new uint[]{R24L0,R24L1,0,0},					//layers
				new uint[]{R24G1,0,0},								//grids
				new uint[]{R24PAL,SPRITE_PAL},				//palettes
				new uint[]{R24PLX,0}									//parallax layers
			),
            new RoomDef(
                2,													//total_layers		//room 25 - Castle Dig
				640,												//size_x
				400,												//size_y
				56,													//grid_width
				new uint[]{R25L0,R25L1,0,0},					//layers
				new uint[]{R25G1,0,0},								//grids
				new uint[]{R25PAL,R25SPRPAL},					//palettes
				new uint[]{0,0}											//parallax layers
			),
            new RoomDef(
                3,													//total_layers		//room 26 - Cellar Dark
				640,												//size_x
				400,												//size_y
				56,													//grid_width
				new uint[]{R26L0,R26L1,R26L2,0},			//layers
				new uint[]{R26G1,R26G2,0},						//grids
				new uint[]{R26PAL,R26SPRPAL},					//palettes
				new uint[]{0,0}											//parallax layers
			),

			//------------------------------------------------------------------------
	// PARIS 3

	new RoomDef(
        3,																					//total_layers		//room 27
		640,																				//size_x
		400,																				//size_y
		56,																					//grid_width
		new uint[]{R27L0,R27L1,R27L2,0},											//layers
		new uint[]{R27G1,R27G2,0},														//grids
		new uint[]{room27_PAL,SPRITE_PAL},										//palettes
		new uint[]{0,0}																			//parallax layers
	),
    new RoomDef(
        3,																					//total_layers		//room 28
		640,																				//size_x
		400,																				//size_y
		56,																					//grid_width
		new uint[]{R28L0,R28L1,R28L2,0},											//layers
		new uint[]{R28G1,R28G2,0},														//grids
		new uint[]{R28PAL,R28SPRPAL},													//palettes
		new uint[]{0,0}																			//parallax layers
	),
    new RoomDef(
        2,																					//total_layers		//room 29
		640,																				//size_x
		400,																				//size_y
		56,																					//grid_width
		new uint[]{R29L0,R29L1,0,0},													//layers
		new uint[]{R29G1,0,0},																//grids
		new uint[]{R29PAL,R29SPRPAL},													//palettes
		new uint[]{0,0}																			//parallax layers
	),
    new RoomDef(
        1,																					//total_layers		//room 30 - for MONITOR seen while player in rm34
		640,																				//size_x
		400,																				//size_y
		56,																					//grid_width
		new uint[]{MONITOR,0,0,0},														//layers
		new uint[]{0,0,0},																		//grids
		new uint[]{MONITOR_PAL,PARIS3_PAL},										//palettes
		new uint[]{0,0}																			//parallax layers
	),
    new RoomDef(
        1,																					//total_layers		//room 31
		640,																				//size_x
		400,																				//size_y
		56,																					//grid_width
		new uint[]{room31_l0,0,0,0},													//layers
		new uint[]{0,0,0},																		//grids
		new uint[]{room31_PAL,PARIS3_PAL},										//palettes
		new uint[]{0,0}																			//parallax layers
	),
    new RoomDef(
        3,																					//total_layers		//room 32
		640,																				//size_x
		400,																				//size_y
		56,																					//grid_width
		new uint[]{room32_l0,room32_l1,room32_l2,0},					//layers
		new uint[]{room32_gd1,room32_gd2,0},									//grids
		new uint[]{room32_PAL,PARIS3_PAL},										//palettes
		new uint[]{0,0}																		//parallax layers
	),
    new RoomDef(
        3,																					//total_layers		//room 33
		640,																				//size_x
		400,																				//size_y
		56,																					//grid_width
		new uint[]{room33_l0,room33_l1,room33_l2,0},					//layers
		new uint[]{room33_gd1,room33_gd2,0},									//grids
		new uint[]{room33_PAL,PARIS3_PAL},										//palettes
		new uint[]{0,0}																			//parallax layers
	),
    new RoomDef(
        4,																					//total_layers		//room 34
		1120,																				//size_x
		400,																				//size_y
		86,																					//grid_width
		new uint[]{room34_l0,room34_l1,room34_l2,room34_l3},	//layers
		new uint[]{room34_gd1,room34_gd2,room34_gd3},					//grids
		new uint[]{room34_PAL,PARIS3_PAL},										//palettes
		new uint[]{R34PLX,0}																	//parallax layers
	),
    new RoomDef(
        2,																					//total_layers		//room 35
		640,																				//size_x
		400,																				//size_y
		56,																					//grid_width
		new uint[]{room35_l0,room35_l1,0},										//layers
		new uint[]{room35_gd1,0},															//grids
		new uint[]{room35_PAL,PARIS3_PAL},										//palettes
		new uint[]{0,0}																			//parallax layers
	),
	//------------------------------------------------------------------------
	// PARIS 4
	new RoomDef(
        2,													//total_layers		//room 36
		960,												//size_x
		400,												//size_y
		76,													//grid_width
		new uint[]{R36L0,R36L1,0,0},					//layers
		new uint[]{R36G1,0,0},								//grids
		new uint[]{room36_PAL,R36SPRPAL},			//palettes
		new uint[]{R36PLX,0}									//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 37
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R37L0,R37L1,0,0},					//layers
		new uint[]{R37G1,0,0},								//grids
		new uint[]{room37_PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 38
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R38L0,R38L1,0,0},					//layers
		new uint[]{R38G1,0,0},								//grids
		new uint[]{room38_PAL,R38SPRPAL},			//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 39
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R39L0,R39L1,0,0},					//layers
		new uint[]{R39G1,0,0},								//grids
		new uint[]{room39_PAL,R39SPRPAL},			//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 40
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R40L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{room40_PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 41
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R41L0,R41L1,0,0},					//layers
		new uint[]{R41G1,0,0},								//grids
		new uint[]{room41_PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        3,													//total_layers		//room 42
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R42L0,R42L1,R42L2,0},			//layers
		new uint[]{R42G1,R42G2,0},						//grids
		new uint[]{room42_PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 43
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R43L0,R43L1,0,0},					//layers
		new uint[]{R43G1,0,0},								//grids
		new uint[]{room43_PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 44
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
	//------------------------------------------------------------------------
	// SYRIA
	new RoomDef(
        2,													//total_layers		//room 45 - Syria Stall
		1152,												//size_x
		400,												//size_y
		88,													//grid_width
		new uint[]{R45L0,R45L1,0,0},					//layers
		new uint[]{R45G1,0,0},								//grids
		new uint[]{R45PAL,R45SPRPAL},					//palettes
		new uint[]{R45PLX,0}									//parallax layers
	),
    new RoomDef(
        3,																	//total_layers		//room 46 (Hotel Alley, Paris 2)
		640,																//size_x
		400,																//size_y
		56,																	//grid_width
		new uint[]{room46_l0,room46_l1,room46_l2,0},	//layers
		new uint[]{room46_gd1,room46_gd2,0},					//grids
		new uint[]{room46_PAL,PARIS2_PAL},						//palettes
		new uint[]{0,0}															//parallax layers
	),
    new RoomDef(
        3,													//total_layers		//room 47 - Syria Carpet
		640,												//size_x
		800,												//size_y
		56,													//grid_width
		new uint[]{R47L0,R47L1,R47L2,0},			//layers
		new uint[]{R47G1,R47G2,0},						//grids
		new uint[]{R47PAL,SYRIA_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        3,													//total_layers		//room 48 (Templar Church, Paris 4)
		1184,												//size_x
		400,												//size_y
		90,													//grid_width
		new uint[]{R48L0,R48L1,R48L2,0},			//layers
		new uint[]{R48G1,R48G2,0},						//grids
		new uint[]{R48PAL,R48SPRPAL},					//palettes
		new uint[]{R48PLX,0}									//parallax layers
	),
    new RoomDef(
        3,													//total_layers		//room 49 - Syria Club
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R49L0,R49L1,R49L2,0},			//layers
		new uint[]{R49G1,R49G2,0},						//grids
		new uint[]{R49PAL,SYRIA_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        4,													//total_layers		//room 50 - Syria Toilet
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R50L0,R50L1,R50L2,R50L3},	//layers
		new uint[]{R50G1,R50G2,R50G3},				//grids
		new uint[]{R50PAL,SYRIA_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 51 - NOT USED
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 52 - NOT USED
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 53 - Bull Head Pan
		880,												//size_x
		1736,												//size_y
		71,													//grid_width
		new uint[]{R53L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{R53PAL,R53SPRPAL},					//palettes
		new uint[]{FRONT53PLX,BACK53PLX}			//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 54 - Bull Head
		896,												//size_x
		1112,												//size_y
		72,													//grid_width
		new uint[]{R54L0,R54L1,0,0},					//layers
		new uint[]{R54G1,0,0},								//grids
		new uint[]{R54PAL,SYRIA_PAL},					//palettes
		new uint[]{R54PLX,0}									//parallax layers - SPECIAL BACKGROUND PARALLAX - MUST GO IN FIRST SLOT
	),
    new RoomDef(
        1,													//total_layers		//room 55 - Bull Secret
		1040,												//size_x
		400,												//size_y
		81,													//grid_width
		new uint[]{R55L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{R55PAL,R55SPRPAL},					//palettes
		new uint[]{R55PLX,0}									//parallax layers
	),
	//------------------------------------------------------------------------
	// SPAIN

	new RoomDef(
        3,													//total_layers		//room 56 - Countess' room
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R56L0,R56L1,R56L2,0},			//layers
		new uint[]{R56G1,R56G2,0},						//grids
		new uint[]{R56PAL,SPAIN_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 57 - Spain Drive
		1760,												//size_x
		400,												//size_y
		126,												//grid_width
		new uint[]{R57L0,R57L1,0,0},					//layers
		new uint[]{R57G1,0,0},								//grids
		new uint[]{R57PAL,SPAIN_PAL},					//palettes
		new uint[]{R57PLX,0}									//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 58 - Mausoleum Exterior
		864,												//size_x
		400,												//size_y
		70,													//grid_width
		new uint[]{R58L0,R58L1,0,0},					//layers
		new uint[]{R58G1,0,0},								//grids
		new uint[]{R58PAL,SPAIN_PAL},					//palettes
		new uint[]{R58PLX,0}									//parallax layers
	),
    new RoomDef(
        3,													//total_layers		//room 59 - Mausoleum Interior
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R59L0,R59L1,R59L2,0},			//layers
		new uint[]{R59G1,R59G2,0},						//grids
		new uint[]{R59PAL,SPAIN_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        3,													//total_layers		//room 60 - Spain Reception
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R60L0,R60L1,R60L2,0},			//layers
		new uint[]{R60G1,R60G2,0},						//grids
		new uint[]{R60PAL,SPAIN_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 61 - Spain Well
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R61L0,R61L1,0,0},					//layers
		new uint[]{R61G1,0,0},								//grids
		new uint[]{R61PAL,SPAIN_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 62 - CHESS PUZZLE
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R62L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{R62PAL,SPRITE_PAL},				//palettes
		new uint[]{0,0}											//parallax layers
	),
	//------------------------------------------------------------------------
	// NIGHT TRAIN

	new RoomDef(
        2,													//total_layers		//room 63 - train_one
		2160,												//size_x
		400,												//size_y
		151,												//grid_width
		new uint[]{R63L0,R63L1,0,0},					//layers
		new uint[]{R63G1,0,0},								//grids
		new uint[]{R63PAL,TRAIN_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 64 - NOT USED
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 65 - compt_one
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R65L0,R65L1,0,0},					//layers
		new uint[]{R65G1,0,0},								//grids
		new uint[]{R65PAL,TRAIN_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 66 - compt_two
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R66L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{R66PAL,TRAIN_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 67 - compt_three
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R67L0,R67L1,0,0},					//layers
		new uint[]{R67G1,0,0},								//grids
		new uint[]{R67PAL,TRAIN_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 68 - NOT USED
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 69 - train_guard
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R69L0,R69L1,0,0},					//layers
		new uint[]{R69G1,0,0},								//grids
		new uint[]{R69PAL,R69SPRPAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 70 - NOT USED
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
	//------------------------------------------------------------------------
	// SCOTLAND

	new RoomDef(
        2,													//total_layers		//room 71 - churchyard
		1760,												//size_x
		400,												//size_y
		126,												//grid_width
		new uint[]{R71L0,R71L1,0,0},					//layers
		new uint[]{R71G1,0,0},								//grids
		new uint[]{R71PAL,SPRITE_PAL},				//palettes
		new uint[]{R71PLX,0}									//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 72 - church_tower
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R72L0,R72L1,0,0},					//layers
		new uint[]{R72G1,0,0},								//grids
		new uint[]{R72PAL,SPRITE_PAL},				//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        3,													//total_layers		//room 73 - crypt
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R73L0,R73L1,R73L2,0},			//layers
		new uint[]{R73G1,R73G2,0},						//grids
		new uint[]{R73PAL,R73SPRPAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        2,													//total_layers		//room 74 - secret_crypt
		1136,												//size_x
		400,												//size_y
		87,													//grid_width
		new uint[]{R74L0,R74L1,0,0},					//layers
		new uint[]{R74G1,0,0},								//grids
		new uint[]{R74PAL,ENDSPRPAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 75 - secret_crypt
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R75L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{R75PAL,ENDSPRPAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 76 - secret_crypt
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R76L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{R76PAL,ENDSPRPAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 77 - secret_crypt
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R77L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{R77PAL,ENDSPRPAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 78 - secret_crypt
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R78L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{R78PAL,ENDSPRPAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 79 - secret_crypt
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R79L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{R79PAL,ENDSPRPAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
	//------------------------------------------------------------------------
	// MAPS

	new RoomDef(
        1,													//total_layers		//room 80 - paris map
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{room80_l0,0,0,0},					//layers
		new uint[]{0,0,0},										//grids
		new uint[]{room80_PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 81 - for sequence of Assassin coming up stairs to rm17
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{ASSTAIR2,0,0,0},						//layers
		new uint[]{0,0,0},										//grids
		new uint[]{ASSTAIR2_PAL,SPRITE_PAL},	//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 82 - Map of Britain, viewed frrom sc55 (Syria Cave)
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{BRITMAP,0,0,0},						//layers
		new uint[]{0,0,0},										//grids
		new uint[]{BRITMAP_PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 83 - NOT USED
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 84 - NOT USED
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 85 - NOT USED
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 86 - europe map
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{room86_l0,0,0,0},					//layers
		new uint[]{0,0,0},										//grids
		new uint[]{room86_PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 87 - normal window in sc48
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{WINDOW1,0,0,0},						//layers
		new uint[]{0,0,0},										//grids
		new uint[]{WINDOW1_PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 88 - filtered window in sc48
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{WINDOW2,0,0,0},						//layers
		new uint[]{0,0,0},										//grids
		new uint[]{WINDOW2_PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 89 - NOT USED
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 90 - phone screen
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R90L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{R90PAL,PHONE_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 91 - envelope screen
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{R91L0,0,0,0},							//layers
		new uint[]{0,0,0},										//grids
		new uint[]{R91PAL,SPRITE_PAL},					//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 92 - for close up of George surprised in wardrobe in sc17
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{SBACK17,0,0,0},						//layers
		new uint[]{0,0,0},										//grids
		new uint[]{SBACK17PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 93 - for close up of George inquisitive in wardrobe in sc17
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{IBACK17,0,0,0},						//layers
		new uint[]{0,0,0},										//grids
		new uint[]{IBACK17PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 94 - for close up of George in sarcophagus in sc29
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{BBACK29,0,0,0},						//layers
		new uint[]{0,0,0},										//grids
		new uint[]{BBACK29PAL,BBACK29SPRPAL},	//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 95 - for close up of George during templar meeting, in sc38
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{BBACK38,0,0,0},						//layers
		new uint[]{0,0,0},										//grids
		new uint[]{BBACK38PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 96 - close up of chalice projection
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{CHALICE42,0,0,0},					//layers
		new uint[]{0,0,0},										//grids
		new uint[]{CHALICE42_PAL,SPRITE_PAL},	//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 97 - NOT USED
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        0,													//total_layers		//room 98 - NOT USED
		0,													//size_x
		0,													//size_y
		0,													//grid_width
		new uint[]{0,0,0,0},									//layers
		new uint[]{0,0,0},										//grids
		new uint[]{0,0},											//palettes
		new uint[]{0,0}											//parallax layers
	),
    new RoomDef(
        1,													//total_layers		//room 99 - blank screen
		640,												//size_x
		400,												//size_y
		56,													//grid_width
		new uint[]{room99_l0,0,0,0},					//layers
		new uint[]{0,0,0},										//grids
		new uint[]{room99_PAL,SPRITE_PAL},		//palettes
		new uint[]{0,0}											//parallax layers
	)
        };
    }
}
