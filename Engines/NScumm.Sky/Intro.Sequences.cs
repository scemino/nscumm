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

namespace NScumm.Sky
{
    partial class Intro
    {
        const ushort SHOWSCREEN = 0;
        const ushort COMMANDEND = 0;   // end of COMMANDFLIRT block
        const ushort FADEUP = 1;       // fade up palette
        const ushort FADEDOWN = 2;
        const ushort DELAY = 3;
        const ushort DOFLIRT = 4;      // start flirt sequence (and wait for it to finish)
        const ushort SCROLLFLIRT = 5;  // start special floppy intro flirt sequence (and wait for it)
        const ushort COMMANDFLIRT = 6; // start flirt sequence and wait for it, while processing command block
        const ushort BGFLIRT = 7;      // start flirt sequence without waiting for it
        const ushort WAITFLIRT = 8;    // wait for sequence started by BGFLIRT
        const ushort STOPFLIRT = 9;
        const ushort STARTMUSIC = 10;
        const ushort WAITMUSIC = 11;
        const ushort PLAYVOICE = 12;
        const ushort WAITVOICE = 13;
        const ushort LOADBG = 14;      // load new background sound
        const ushort PLAYBG = 15;      // play background sound
        const ushort LOOPBG = 16;      // loop background sound
        const ushort STOPBG = 17;      // stop background sound
        const ushort SEQEND = 65535;   // end of intro sequence

        const ushort IC_PREPARE_TEXT = 20; // commands used in COMMANDFLIRT block
        const ushort IC_SHOW_TEXT = 21;
        const ushort IC_REMOVE_TEXT = 22;
        const ushort IC_MAKE_SOUND = 23;
        const ushort IC_FX_VOLUME = 24;

        const ushort CDV_00 = 59500;
        const ushort CD_PAL = 59501;
        const ushort CD_1_LOG = 59502;
        const ushort CD_1 = 59503;
        const ushort CDV_01 = 59504;
        const ushort CDV_02 = 59505;
        const ushort CD_2 = 59506;
        const ushort CDV_03 = 59507;
        const ushort CDV_04 = 59508;
        const ushort CD_3 = 59509;
        const ushort CDV_05 = 59510;
        const ushort CDV_06 = 59511;
        const ushort CD_5 = 59512;
        const ushort CDV_07 = 59513;
        const ushort CDV_08 = 59514;
        const ushort CDV_09 = 59515;
        const ushort CD_7 = 59516;
        const ushort CDV_10 = 59518;
        const ushort CD_11 = 59519;
        const ushort CDV_11 = 59520;
        const ushort CD_11_PAL = 59521;
        const ushort CD_11_LOG = 59522;
        const ushort CDV_12 = 59523;
        const ushort CD_13 = 59524;
        const ushort CDV_13 = 59525;
        const ushort CDV_14 = 59527;
        const ushort CDV_15 = 59528;
        const ushort CD_15_PAL = 59529;
        const ushort CD_15_LOG = 59530;
        const ushort CDV_16 = 59531;
        const ushort CD_17_LOG = 59532;
        const ushort CD_17 = 59533;
        const ushort CDV_17 = 59534;
        const ushort CDV_18 = 59535;
        const ushort CDV_19 = 59536;
        const ushort CD_19_PAL = 59537;
        const ushort CD_19_LOG = 59538;
        const ushort CDV_20 = 59539;
        const ushort CD_20_LOG = 59540;
        const ushort CDV_21 = 59541;
        const ushort CD_21_LOG = 59542;
        const ushort CDV_22 = 59545;
        const ushort CDV_23 = 59546;
        const ushort CD_23_PAL = 59547;
        const ushort CD_24_LOG = 59550;
        const ushort CDV_24 = 59551;
        const ushort CDV_25 = 59554;
        const ushort CDV_26 = 59556;
        const ushort CD_27 = 59557;
        const ushort CDV_27 = 59558;
        const ushort CD_27_PAL = 59559;
        const ushort CD_27_LOG = 59560;
        const ushort CDV_28 = 59561;
        const ushort CDV_29 = 59562;
        const ushort CDV_30 = 59563;
        const ushort CDV_31 = 59565;
        const ushort CDV_32 = 59566;
        const ushort CDV_33 = 59567;
        const ushort CDV_34 = 59568;
        const ushort CD_35 = 59569;
        const ushort CDV_35 = 59570;
        const ushort CD_35_PAL = 59571;
        const ushort CD_35_LOG = 59572;
        const ushort CDV_36 = 59574;
        const ushort CD_37 = 59575;
        const ushort CDV_37 = 59576;
        const ushort CD_37_PAL = 59577;
        const ushort CD_37_LOG = 59578;
        const ushort CDV_38 = 59579;
        const ushort CDV_39 = 59581;
        const ushort CDV_40 = 59583;
        const ushort CD_40_PAL = 59584;
        const ushort CD_40_LOG = 59585;
        const ushort CDV_41 = 59587;
        const ushort CDV_42 = 59588;
        const ushort CD_43 = 59589;
        const ushort CDV_43 = 59590;
        const ushort CD_43_PAL = 59591;
        const ushort CD_43_LOG = 59592;
        const ushort CDV_44 = 59594;
        const ushort CD_45 = 59595;
        const ushort CDV_45 = 59596;
        const ushort CD_45_PAL = 59597;
        const ushort CD_45_LOG = 59598;
        const ushort CDV_46 = 59600;
        const ushort CDV_47 = 59602;
        const ushort CD_47_PAL = 59603;
        const ushort CD_47_LOG = 59604;
        const ushort CD_48 = 59605;
        const ushort CDV_48 = 59606;
        const ushort CD_48_PAL = 59607;
        const ushort CD_48_LOG = 59608;
        const ushort CD_49 = 59609;
        const ushort CDV_49 = 59610;
        const ushort CD_50 = 59611;
        const ushort CDV_50 = 59612;
        const ushort CDV_51 = 59613;
        const ushort CDV_52 = 59614;
        const ushort CDV_53 = 59615;
        const ushort CDV_54 = 59616;
        const ushort CDV_55 = 59618;
        const ushort CD_55_PAL = 59619;
        const ushort CD_55_LOG = 59620;
        const ushort CDV_56 = 59621;
        const ushort CDV_57 = 59622;
        const ushort CD_58 = 59623;
        const ushort CDV_58 = 59624;
        const ushort CD_58_PAL = 59625;
        const ushort CD_58_LOG = 59626;
        const ushort CDV_59 = 59627;
        const ushort CDV_60 = 59628;
        const ushort CDV_61 = 59629;
        const ushort CDV_62 = 59630;
        const ushort CDV_63 = 59631;
        const ushort CDV_64 = 59632;
        const ushort CDV_65 = 59633;
        const ushort CDV_66 = 59635;
        const ushort CD_66_PAL = 59636;
        const ushort CD_66_LOG = 59637;
        const ushort CDV_67 = 59639;
        const ushort CD_67_PAL = 59640;
        const ushort CD_67_LOG = 59641;
        const ushort CDV_68 = 59642;
        const ushort CD_69 = 59643;
        const ushort CDV_69 = 59644;
        const ushort CD_69_PAL = 59645;
        const ushort CD_69_LOG = 59646;
        const ushort CDV_70 = 59647;
        const ushort CDV_71 = 59648;
        const ushort CDV_72 = 59649;
        const ushort CD_72_PAL = 59650;
        const ushort CD_72_LOG = 59651;
        const ushort CD_73_PAL = 59652;
        const ushort CD_73_LOG = 59653;
        const ushort CDV_73 = 59654;
        const ushort CDV_74 = 59655;
        const ushort CDV_75 = 59656;
        const ushort CD_76_PAL = 59657;
        const ushort CD_76_LOG = 59658;
        const ushort CDV_76 = 59659;
        const ushort CDV_77 = 59660;
        const ushort CD_78_PAL = 59661;
        const ushort CD_78_LOG = 59662;
        const ushort CDV_78 = 59663;
        const ushort CDV_79 = 59664;
        const ushort CDV_80 = 59665;
        const ushort CDV_81 = 59666;
        const ushort CDV_82 = 59667;
        const ushort CDV_83 = 59668;
        const ushort CDV_84 = 59669;
        const ushort CDV_85 = 59670;
        const ushort CDV_86 = 59671;
        const ushort CDV_87 = 59672;
        const ushort CD_100 = 60087;
        const ushort CD_101_LOG = 60088;
        const ushort CD_101 = 60099;
        const ushort CD_102_LOG = 60090;
        const ushort CD_102 = 60091;
        const ushort CD_103_PAL = 60092;
        const ushort CD_103_LOG = 60093;
        const ushort CD_103 = 60094;
        const ushort CD_104_PAL = 60095;
        const ushort CD_104_LOG = 60096;
        const ushort CD_104 = 60097;
        const ushort CD_105 = 60098;


        static readonly ushort[] _mainIntroSeq = {
            DELAY,       3000, // keep virgin screen up
	        FADEDOWN,
            SHOWSCREEN, 60112, // revo screen + palette
	        FADEUP,     60113,
            DELAY,       8000,
            FADEDOWN,
            SHOWSCREEN, 60114, // gibbo screen + palette
	        FADEUP,     60115,
            DELAY,       2000,
            FADEDOWN,
            SEQEND
        };

        static readonly ushort[] _floppyIntroSeq = {
            SHOWSCREEN,   60081,
            FADEUP,       60080,
            DOFLIRT,      60082,
            DOFLIRT,      60083,
            DOFLIRT,      60084, // Beneath a Steel Sky
	        DOFLIRT,      60085,
            DOFLIRT,      60086,
            SCROLLFLIRT,
            COMMANDFLIRT, 60087, // => command list 4a
		        136, IC_MAKE_SOUND,  1, 70,
                 90, IC_FX_VOLUME,  80,
                 50, IC_FX_VOLUME,  90,
                  5, IC_FX_VOLUME, 100,
            COMMANDEND,
            SHOWSCREEN,   60088,
            COMMANDFLIRT, 60089, // => command list 4b (cockpit)
		        1000, IC_PREPARE_TEXT,  77,
                 220, IC_SHOW_TEXT,     20, 160, // radar detects jamming signal
		         105, IC_REMOVE_TEXT,
                 105, IC_PREPARE_TEXT,  81,
                 105, IC_SHOW_TEXT,    170,  86, // well switch to override you fool
		          35, IC_REMOVE_TEXT,
                  35, IC_PREPARE_TEXT, 477,
                  35, IC_SHOW_TEXT,     30, 160,
                   3, IC_REMOVE_TEXT,
            COMMANDEND,
            SHOWSCREEN,   60090,
            COMMANDFLIRT, 60091, // => command list 4c
		        1000, IC_FX_VOLUME, 100,
                  25, IC_FX_VOLUME, 110,
                  15, IC_FX_VOLUME, 120,
                   4, IC_FX_VOLUME, 127,
            COMMANDEND,
            FADEDOWN,
            SHOWSCREEN,  60093,
            FADEUP,       60092,
            COMMANDFLIRT, 60094, // => command list 5
		        31, IC_MAKE_SOUND, 2, 127,
            COMMANDEND,
            WAITMUSIC,
            FADEDOWN,
            SHOWSCREEN,   60096,
            STARTMUSIC,       2,
            FADEUP,       60095,
            COMMANDFLIRT, 60097, // => command list 6a
		        1000, IC_PREPARE_TEXT, 478,
                  13, IC_SHOW_TEXT,    175, 155,
            COMMANDEND,
            COMMANDFLIRT, 60098, // => command list 6b
		        131, IC_REMOVE_TEXT,
                131, IC_PREPARE_TEXT, 479,
                 74, IC_SHOW_TEXT,    175, 155,
                 45, IC_REMOVE_TEXT,
                 45, IC_PREPARE_TEXT, 162,
                 44, IC_SHOW_TEXT,    175, 155,
                  4, IC_REMOVE_TEXT,
            COMMANDEND,
            SEQEND
        };

        static readonly ushort[] _cdIntroSeq = {
            /* black screen */
            PLAYVOICE,  CDV_00,	// Foster: "The old man was trying to tell the future. Looking for pictures in the campfire..."
	        LOADBG,     59499,	// Fire crackle
	        LOOPBG,
            WAITVOICE,
            PLAYVOICE,  CDV_01,	// Shaman: "ohhh, I see evil..."
	        /* Fade up shaman image while he says his line... */
	        SHOWSCREEN, CD_1_LOG,
            FADEUP,     CD_PAL,
	        /* And then play the animation showing the shadows of the fire on his face */
	        BGFLIRT,    CD_1,
                WAITVOICE,
                PLAYVOICE,  CDV_02,	// Shaman: "Evil born deep beneath the city... far from the light of day."
		        WAITVOICE,
            STOPFLIRT,
            BGFLIRT,    CD_2,
                PLAYVOICE,  CDV_03, // Shaman: "I see it growing, safe beneath a sky of steel..."
		        WAITVOICE,
                PLAYVOICE,  CDV_04, // Shaman: "Scheming in the dark... gathering strength."
	        WAITFLIRT,
            WAITVOICE,
            PLAYVOICE,  CDV_05,		// Shaman: "And now... ohhh.... now the evil spreads..."
	        DELAY,      2000,
            BGFLIRT,    CD_3,
                WAITVOICE,
                PLAYVOICE,  CDV_06,	// Shaman: "It sends deadly feelers over the land above..."
	        WAITFLIRT,
            WAITVOICE,
            PLAYVOICE,  CDV_07,		// Shaman: "Across the gap... reaching towards this very place!"
	        BGFLIRT,    CD_5,
                WAITVOICE,
                PLAYVOICE,  CDV_08,	// Foster: "I'd seen him do this a hundred times, but I humoured him."
		        WAITVOICE,
                PLAYVOICE,  CDV_09,	// Foster: "After all, he'd been like a father to me."
	        WAITFLIRT,
            WAITVOICE,
            PLAYVOICE,  CDV_10,		// Foster: "And what does this evil want here?"
	        BGFLIRT,    CD_7,
                WAITVOICE,
                PLAYVOICE,  CDV_11, // Shaman: "Oh, my son. I fear..."
	        WAITFLIRT,
            FADEDOWN,
            SHOWSCREEN, CD_11_LOG,
            FADEUP,     CD_11_PAL,
            WAITVOICE,
            PLAYVOICE,  CDV_12,		// Shaman: "I fear the evil wants you!"
	        DELAY,      1600,
            BGFLIRT,    CD_11,
                WAITVOICE,
                PLAYVOICE,  CDV_13,	// Foster: "That was when Joey piped up..."
		        WAITVOICE,
            WAITFLIRT,
            WAITVOICE,
            PLAYVOICE,  CDV_14,		// Joey: "Foster! Sensors detect incoming audio source!"
	        LOADBG,     59498, // fire crackle to heli start
	        PLAYBG,
            DOFLIRT,    CD_13,
            WAITVOICE,
            PLAYVOICE,  CDV_15,		// Shaman: "The evil! The evil is nearly here...!"
	        FADEDOWN,
            SHOWSCREEN, CD_15_LOG,
            FADEUP,     CD_15_PAL,
            WAITVOICE,
            LOADBG,     59496, // quiet heli
	        LOOPBG,
            PLAYVOICE,  CDV_16,		// Foster: "It sounded more like a 'copter than a demon."
	        WAITVOICE,
            PLAYVOICE,  CDV_17,		// Foster: "But next thing, all hell let loose anyway..."
	        DELAY,      2000,
            SHOWSCREEN, CD_17_LOG,
            WAITVOICE,
            BGFLIRT,    CD_17,
                PLAYVOICE,  CDV_18,	// Shaman: "Run, Foster! Run! Hide from the evil!"
	        LOADBG,     59497, // loud heli
	        LOOPBG,
            WAITFLIRT,
            WAITVOICE,
            FADEDOWN,
            SHOWSCREEN, CD_19_LOG,
            FADEUP,     CD_19_PAL,
            PLAYVOICE,  CDV_19,		// Joey: "Foster! (zzzt) H-Help!"
	        WAITVOICE,
            PLAYVOICE,  CDV_20,		// Joey: "Better make my next body move faster, Foster..."
	        FADEDOWN,
            SHOWSCREEN, CD_20_LOG,
            FADEUP,     CD_19_PAL,
            WAITVOICE,
            LOADBG,     59496, // quiet heli
	        LOOPBG,
            PLAYVOICE,  CDV_21,		// Foster: "He was only a robot, but, well, I loved the little guy."
	        FADEDOWN,
            SHOWSCREEN, CD_21_LOG,
            FADEUP,     CD_19_PAL,
            WAITVOICE,
            PLAYVOICE,  CDV_22,		// Foster: "Then, as suddenly as it started, the shooting stopped."
	        LOADBG,     59494, // heli whine
	        PLAYBG,
            WAITVOICE,
            PLAYVOICE,  CDV_23,		// Foster: "There was a moment's silence as the copter cut its rotors, then..."
	        /* fade down while Foster's saying his line */
	        FADEDOWN,
            WAITVOICE,
            SHOWSCREEN, CD_24_LOG,
            FADEUP,     CD_23_PAL,
            PLAYVOICE,  CDV_24,		// Reich: "Whoever is in charge here, come forward..."
	        WAITVOICE,
            PLAYVOICE,  CDV_25,		// Reich: "Now!!"
	        WAITVOICE,
            PLAYVOICE,  CDV_26,		// Foster: "Only a fool would have argued with that firepower."
	        WAITVOICE,
            FADEDOWN,
            SHOWSCREEN, CD_27_LOG,
            FADEUP,     CD_27_PAL,
            PLAYVOICE,  CDV_27,		// Shaman: "... I am the leader of these people... We are peaceful..."
	        WAITVOICE,
            PLAYVOICE,  CDV_29,		// Reich: "Bring him here."
	        WAITVOICE,
            PLAYVOICE,  CDV_30,		// Guard: "At once, Commander Reich."
	        WAITVOICE,
            BGFLIRT,    CD_27,
                PLAYVOICE,  CDV_31,	// Reich: "We're looking for someone..."
		        WAITVOICE,
                PLAYVOICE,  CDV_32,	// Reich: "Someone who doesn't belong here..."
		        WAITVOICE,
                PLAYVOICE,  CDV_33,	// Reich: "Who wasn't born in this garbage dump..."
		        WAITVOICE,
                PLAYVOICE,  CDV_34,	// Reich: "Who came from the city as a child..."
	        WAITFLIRT,
            WAITVOICE,
            PLAYVOICE,  CDV_35,		// Reich: "We want to take him home again."
	        WAITVOICE,
            PLAYVOICE,  CDV_36,		// Foster: "My mind racing, I remembered where I'd seen that symbol before..."
		        FADEDOWN,
                SHOWSCREEN, CD_35_LOG,
                FADEUP,     CD_35_PAL,
            WAITVOICE,
            PLAYVOICE,  CDV_37,		// Foster: "It was the day the tribe found me..."
		        DOFLIRT,    CD_35,
            WAITVOICE,
            PLAYVOICE,  CDV_38,		// Foster: "The day of the crash..."
		        DOFLIRT,    CD_37,
            WAITVOICE,
            PLAYVOICE,  CDV_39,		// Foster: "The day my mother died."
	        WAITVOICE,
            FADEDOWN,
            SHOWSCREEN, CD_40_LOG,
            FADEUP,     CD_40_PAL,
            PLAYVOICE,  CDV_40,		// Shaman: "You alright, city boy?"
	        WAITVOICE,
            PLAYVOICE,  CDV_41,		// Shaman: "Got a name, son?"
	        WAITVOICE,
            PLAYVOICE,  CDV_42,		// Foster: "R-Robert."
	        WAITVOICE,
            FADEDOWN,
            SHOWSCREEN, CD_43_LOG,
            FADEUP,     CD_43_PAL,
            PLAYVOICE,  CDV_43,		// Shaman: "Hah! Welcome to the Gap, Robert!"
	        WAITVOICE,
            DOFLIRT,    CD_43,
            PLAYVOICE,  CDV_45,		// Foster: "As he patched me up, the old man had gently explained that there was no way back into the City..."
	        FADEDOWN,
            SHOWSCREEN, CD_45_LOG,
            FADEUP,     CD_45_PAL,
            WAITVOICE,
            PLAYVOICE,  CDV_46,		// Foster: "And I already knew there was nothing he could do for mother."
	        DOFLIRT,    CD_45,
            WAITVOICE,
            FADEDOWN,
            SHOWSCREEN, CD_47_LOG,
            FADEUP,     CD_47_PAL,
            PLAYVOICE,  CDV_47,		// Foster: "His tribe was poor, but they treated me like one of their own..."
	        WAITVOICE,
            PLAYVOICE,  CDV_48,		// Foster: "I learned how to survive in the wasteland they called the Gap..."
	        FADEDOWN,
            SHOWSCREEN, CD_48_LOG,
            FADEUP,     CD_48_PAL,
            WAITVOICE,
            BGFLIRT,    CD_48,
                PLAYVOICE,  CDV_49,	// Foster: "And scavenging from the City dumps."
		        WAITVOICE,
                PLAYVOICE,  CDV_50,	// Foster: "As the years passed, I forgot my life in the City."
	        WAITFLIRT,
            WAITVOICE,
            PLAYVOICE,  CDV_51,		// Foster: "Discovered new talents..."
	        BGFLIRT,    CD_49,
                WAITVOICE,
                PLAYVOICE,  CDV_52,	// Foster: "Hah!"
		        WAITVOICE,
                PLAYVOICE,  CDV_53,	// Joey: "I'm your (zzzt) friend... call me (zzzt) Joey."
		        WAITVOICE,
            WAITFLIRT,
            PLAYVOICE,  CDV_54,		// Foster: "And got a second name."
	        DOFLIRT,    CD_50,
            WAITVOICE,
            PLAYVOICE,  CDV_55,		// Shaman: "This is what we'll call you now you've come of age, son."
	        WAITVOICE,
            PLAYVOICE,  CDV_56,		// Shaman: "We found you... we fostered you..."
		        FADEDOWN,
                SHOWSCREEN, CD_55_LOG,
                FADEUP,     CD_55_PAL,
            WAITVOICE,
            PLAYVOICE,  CDV_57,		// Shaman: "So that makes you Robert Foster."
	        WAITVOICE,
            FADEDOWN,
            SHOWSCREEN, CD_58_LOG,
            FADEUP,     CD_58_PAL,
            PLAYVOICE,  CDV_58,		// Reich: "...Wasted enough time!"
	        WAITVOICE,
            PLAYVOICE,  CDV_59,		// Reich: "Give us the runaway or we'll shoot everyone..."
	        WAITVOICE,
            PLAYVOICE,  CDV_60,		// Reich: "Starting with you, grandad!"
	        WAITVOICE,
            PLAYVOICE,  CDV_61,		// Foster: "The old man had been right, for once..."
	        WAITVOICE,
            PLAYVOICE,  CDV_62,		// Foster: "It was me they wanted."
	        BGFLIRT,    CD_58,
                WAITVOICE,
                PLAYVOICE,  CDV_63,	// Shaman: "No, my son! Don't let the evil take you! Run!"
		        WAITVOICE,
                PLAYVOICE,  CDV_64,	// Guard: "DNA scan confirms it's him, sir."
	        WAITFLIRT,
            WAITVOICE,
            PLAYVOICE,  CDV_65,		// Foster: "Evil had come to the Gap, just as he said."
	        FADEDOWN,
            WAITVOICE,
            SHOWSCREEN, CD_66_LOG,
            FADEUP,     CD_66_PAL,
            PLAYVOICE,  CDV_66,		// Reich: "Take him."
	        WAITVOICE,
            PLAYVOICE,  CDV_67,		// Foster: "But had the old man seen why it wanted me?"
		        FADEDOWN,
                SHOWSCREEN, CD_67_LOG,
                FADEUP,     CD_67_PAL,
            WAITVOICE,
            PLAYVOICE,  CDV_68,		// Foster: "Or what it would do next?"
	        WAITVOICE,
            PLAYVOICE,  CDV_69,		// Foster: "It was too late to ask him now."
		        FADEDOWN,
                SHOWSCREEN, CD_69_LOG,
                FADEUP,     CD_69_PAL,
            WAITVOICE,
            PLAYVOICE,  CDV_70,		// Guard: "Leaving destruction zone, Commander Reich."
	        DOFLIRT,    CD_69,
            WAITVOICE,
            FADEDOWN,
            PLAYVOICE,  CDV_71,		// Reich: "Good. Detonate."
	        WAITVOICE,
            SHOWSCREEN, CD_72_LOG,
            FADEUP,     CD_72_PAL,
            PLAYVOICE,  CDV_72,		// Foster: "Much too late."
	        WAITVOICE,
            FADEDOWN,
            SHOWSCREEN, CD_73_LOG,
            FADEUP,     CD_73_PAL,
            PLAYVOICE,  CDV_73,		// Foster: "Why, you murdering..."
	        WAITVOICE,
            PLAYVOICE,  CDV_74,		// Reich: "Keep him quiet."
	        WAITVOICE,
            PLAYVOICE,  CDV_75,		// Foster: "All I could do was wait."
	        FADEDOWN,
            SHOWSCREEN, CD_76_LOG,
            FADEUP,     CD_76_PAL,
            WAITVOICE,
            PLAYVOICE,  CDV_76,		// Foster: "Just like on a hunt. Just like the old man taught me."
	        WAITVOICE,
            PLAYVOICE,  CDV_77,		// Foster: "Wait... and be ready."
	        WAITVOICE,
            FADEDOWN,
            SHOWSCREEN, CD_78_LOG,
            FADEUP,     CD_78_PAL,
            PLAYVOICE,  CDV_78,		// Foster: "It was dawn when we reached the City."
	        WAITVOICE,
            PLAYVOICE,  CDV_79,		// Reich: "Land in the central Security compound."
	        WAITVOICE,
            PLAYVOICE,  CDV_80,		// Foster: "A dawn my tribe would never see."
	        BGFLIRT,    CD_100,
                WAITVOICE,
                PLAYVOICE,  CDV_81,	// Foster: "They were no more than a note in Reich's book now."
		        WAITVOICE,
                PLAYVOICE,  CDV_82,	// Guard: "Yes, sir. Locking on automatic landing beacon."
		        WAITVOICE,
            WAITFLIRT,
            SHOWSCREEN, CD_101_LOG,
            BGFLIRT,    CD_101,
                PLAYVOICE,  CDV_83,	// Foster: "But what was I? Why did..."
		        WAITVOICE,
                PLAYVOICE,  CDV_84,	// Guard: "Sir! The guidance system! It's gone crazy!"
		        WAITVOICE,
                PLAYVOICE,  CDV_85,	// Guard: "We're going to HIT!"
		        WAITVOICE,
            WAITFLIRT,
            SHOWSCREEN, CD_102_LOG,
            PLAYVOICE,  CDV_86,		// Foster: "Maybe I'd get some answers now."
	        DOFLIRT,    CD_102,
            FADEDOWN,
            SHOWSCREEN, CD_103_LOG,
            FADEUP,     CD_103_PAL,
            BGFLIRT,    CD_103,
            WAITVOICE,
            PLAYVOICE,  CDV_87,		// Foster: "If I survived another 'copter crash."
	        WAITFLIRT,
            WAITVOICE,
            STARTMUSIC, 2,
            FADEDOWN,
            SHOWSCREEN, CD_104_LOG,
            FADEUP,     CD_104_PAL,
            DOFLIRT,    CD_104,
            DOFLIRT,    CD_105,
            SEQEND
        };
    }
}
