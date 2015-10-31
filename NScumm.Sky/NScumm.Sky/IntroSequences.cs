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
    }
}
