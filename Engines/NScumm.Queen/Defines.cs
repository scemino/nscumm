//
//  Defines.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core;
using System.IO;
using NScumm.Core.IO;
using System.Globalization;

namespace NScumm.Queen
{
    
	static class Defines
	{
		public const int GAME_SCREEN_WIDTH = 320;
		public const int GAME_SCREEN_HEIGHT = 200;
		public const int ROOM_ZONE_HEIGHT = 150;
		public const int PANEL_ZONE_HEIGHT = 50;

		public const int COMPRESSION_NONE = 0;
		public const int COMPRESSION_MP3 = 1;
		public const int COMPRESSION_OGG = 2;
		public const int COMPRESSION_FLAC = 3;

		public const int FRAMES_JOE = 38;
		public const int FRAMES_JOURNAL = 40;

		public const int ROOM_JUNGLE_INSIDE_PLANE = 1;
		public const int ROOM_JUNGLE_OUTSIDE_PLANE = 2;
		public const int ROOM_JUNGLE_BRIDGE = 4;
		public const int ROOM_JUNGLE_GORILLA_1 = 6;
		public const int ROOM_JUNGLE_PINNACLE = 7;
		public const int ROOM_JUNGLE_SLOTH = 8;
		public const int ROOM_JUNGLE_BUD_SKIP = 9;
		public const int ROOM_JUNGLE_BEETLE = 11;
		public const int ROOM_JUNGLE_MISSIONARY = 13;
		public const int ROOM_JUNGLE_GORILLA_2 = 14;

		public const int ROOM_AMAZON_ENTRANCE = 16;
		public const int ROOM_AMAZON_HIDEOUT = 17;
		public const int ROOM_AMAZON_THRONE = 18;
		public const int ROOM_AMAZON_JAIL = 19;

		public const int ROOM_VILLAGE = 20;
		public const int ROOM_TRADER_BOBS = 21;

		public const int ROOM_FLODA_OUTSIDE = 22;
		public const int ROOM_FLODA_KITCHEN = 26;
		public const int ROOM_FLODA_LOCKERROOM = 27;
		public const int ROOM_FLODA_KLUNK = 30;
		public const int ROOM_FLODA_HENRY = 32;
		public const int ROOM_FLODA_OFFICE = 35;
		public const int ROOM_FLODA_JAIL = 41;
		public const int ROOM_FLODA_FRONTDESK = 103;

		public const int ROOM_TEMPLE_OUTSIDE = 43;
		public const int ROOM_TEMPLE_MUMMIES = 46;
		public const int ROOM_TEMPLE_ZOMBIES = 50;
		public const int ROOM_TEMPLE_TREE = 51;
		public const int ROOM_TEMPLE_SNAKE = 53;
		public const int ROOM_TEMPLE_LIZARD_LASER = 55;
		public const int ROOM_TEMPLE_MAZE = 58;
		public const int ROOM_TEMPLE_MAZE_2 = 59;
		public const int ROOM_TEMPLE_MAZE_3 = 60;
		public const int ROOM_TEMPLE_MAZE_4 = 61;
		public const int ROOM_TEMPLE_MAZE_5 = 100;
		public const int ROOM_TEMPLE_MAZE_6 = 101;

		public const int ROOM_VALLEY_CARCASS = 67;

		public const int ROOM_HOTEL_UPSTAIRS = 70;
		public const int ROOM_HOTEL_DOWNSTAIRS = 71;
		public const int ROOM_HOTEL_LOLA = 72;
		public const int ROOM_HOTEL_LOBBY = 73;

		public const int ROOM_CAR_CHASE = 74;

		public const int ROOM_FINAL_FIGHT = 69;

		public const int ROOM_INTRO_RITA_JOE_HEADS = 116;
		public const int ROOM_INTRO_EXPLOSION = 123;

		//special
		public const int SPARKY_OUTSIDE_HOTEL = 77;
		public const int DEATH_MASK = 79;
		public const int IBI_LOGO = 82;
		public const int COMIC_1 = 87;
		public const int COMIC_2 = 88;
		public const int COMIC_3 = 89;
		public const int ROOM_UNUSED_INTRO_1 = 90;
		public const int ROOM_UNUSED_INTRO_2 = 91;
		public const int ROOM_UNUSED_INTRO_3 = 92;
		public const int ROOM_UNUSED_INTRO_4 = 93;
		public const int ROOM_UNUSED_INTRO_5 = 94;
		public const int FOTAQ_LOGO = 95;
		public const int WARNER_LOGO = 126;

		public const int FAYE_HEAD = 37;
		public const int AZURA_HEAD = 106;
		public const int FRANK_HEAD = 107;

		public const int ROOM_ENDING_CREDITS = 110;

		public const int ROOM_JOURNAL = 200;
		// dummy value to keep Display methods happy

		public const int VAR_HOTEL_ITEMS_REMOVED = 3;
		public const int VAR_JOE_DRESSING_MODE = 19;
		public const int VAR_BYPASS_ZOMBIES = 21;
		public const int VAR_BYPASS_FLODA_RECEPTIONIST = 35;
		public const int VAR_GUARDS_TURNED_ON = 85;
		public const int VAR_HOTEL_ESCAPE_STATE = 93;
		public const int VAR_INTRO_PLAYED = 117;
		public const int VAR_AZURA_IN_LOVE = 167;
	}

	enum Item
	{
		ITEM_NONE = 0,
		ITEM_BAT,
		ITEM_JOURNAL,
		ITEM_KNIFE,
		ITEM_COCONUT_HALVES,
		ITEM_BEEF_JERKY,
		ITEM_PROPELLER,
		ITEM_BANANA,
		ITEM_VINE,
		ITEM_SLOTH_HAIR,
		ITEM_COMIC_BOOK,
		ITEM_FLOWER,
		ITEM_BEETLE,
		ITEM_ORCHID,
		ITEM_DICTIONARY,
		ITEM_DEATH_MASH,
		ITEM_PERFUME,
		ITEM_TYRANNO_HORN,
		ITEM_LOTION,
		ITEM_RECORD,
		ITEM_VACUUM_CLEANER,
		ITEM_NET,
		ITEM_ALCOHOL,
		ITEM_ROCKET_PACK,
		ITEM_SOME_MONEY,
		ITEM_CHEESE_BITZ,
		ITEM_DOG_FOOD,
		ITEM_CAN_OPENER,
		ITEM_LETTER,
		ITEM_SQUEAKY_TOY,
		ITEM_KEY,
		ITEM_BOOK,
		ITEM_PIECE_OF_PAPER,
		ITEM_ROCKET_PLAN,
		ITEM_PADLOCK_KEY,
		ITEM_RIB_CAGE,
		ITEM_SKULL,
		ITEM_LEG_BONE,
		ITEM_BAT2,
		ITEM_MAKESHIFT_TOCH,
		ITEM_LIGHTER,
		ITEM_GREEN_JEWEL,
		ITEM_PICK,
		ITEM_STONE_KEY,
		ITEM_BLUE_JEWEL,
		ITEM_CRYSTAL_SKULL,
		ITEM_TREE_SAP,
		ITEM_DINO_RAY_GUN,
		ITEM_BRANCHES,
		ITEM_WIG,
		ITEM_TOWEL,
		ITEM_OTHER_SHEET,
		ITEM_SHEET,
		ITEM_SHEET_ROPE,
		ITEM_CROWBAR,
		ITEM_COMEDY_BREASTS,
		ITEM_DRESS,
		ITEM_KEY2,
		ITEM_CLOTHES,
		ITEM_HAY,
		ITEM_OIL,
		ITEM_CHICKEN,
		ITEM_LIT_TORCH,
		ITEM_OPENED_DOG_FOOD,
		ITEM_SOME_MONEY2,
		ITEM_SOME_MORE_MONEY,
		ITEM_PEELED_BANANA,
		ITEM_STONE_DISC,
		ITEM_GNARLED_VINE,
		ITEM_FLINT,
		ITEM_LIGHTER2,
		ITEM_REST_OF_BEEF_JERKY,
		ITEM_LOTS_OF_MONEY,
		ITEM_HEAPS_OF_MONEY,
		ITEM_OPEN_BOOK,
		ITEM_REST_OF_THE_CHEESE_BITZ,
		ITEM_SCISSORS,
		ITEM_PENCIL,
		ITEM_SUPER_WEENIE_SERUM,
		ITEM_MUMMY_WRAPPINGS,
		ITEM_COCONUT,
		ITEM_ID_CARD,
		ITEM_BIT_OF_STONE,
		ITEM_CHUNK_OF_ROCK,
		ITEM_BIG_STICK,
		ITEM_STICKY_BIT_OF_STONE,
		ITEM_STICKY_CHUNK_OF_ROCK,
		ITEM_DEATH_MASK2,
		ITEM_CHEFS_SURPRISE,
		ITEM_STICKY_BAT,
		ITEM_REST_OF_WRAPPINGS,
		ITEM_BANANA2,
		ITEM_MUG,
		ITEM_FILE,
		ITEM_POCKET_ROCKET_BLUEPRINTS,
		ITEM_HAND_PUPPET,
		ITEM_ARM_BONE,
		ITEM_CROWN,
		ITEM_COMIC_COUPON,
		ITEM_TORN_PAGE}

	;

}
