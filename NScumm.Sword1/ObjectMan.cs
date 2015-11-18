using System;
using NScumm.Core;

namespace NScumm.Sword1
{
    internal class ObjectMan
    {
        public const int TOTAL_SECTIONS = 150;  // number of sections, rooms + mega sections
        const int TEXT_sect = 149;       // text compacts exist in section 149, probably after all the megas
        const int ITM_PER_SEC = 0x10000; // 65536 items per section -> was originally called "SIZE"
        const int ITM_ID = 0xFFFF;//& with this -> originally "NuSIZE"

        private ResMan _resMan;
        ushort[] _liveList = new ushort[TOTAL_SECTIONS];                  //which sections are active
        ByteAccess[] _cptData = new ByteAccess[TOTAL_SECTIONS];

        public ObjectMan(ResMan pResourceMan)
        {
            _resMan = pResourceMan;
        }

        public void Initialize()
        {
            ushort cnt;
            for (cnt = 0; cnt < TOTAL_SECTIONS; cnt++)
                _liveList[cnt] = 0; // we don't need to close the files here. When this routine is
                                    // called, the memory was flushed() anyways, so these resources
                                    // already *are* closed.

            _liveList[128] = _liveList[129] = _liveList[130] = _liveList[131] = _liveList[133] =
                                                  _liveList[134] = _liveList[145] = _liveList[146] = _liveList[TEXT_sect] = 1;

            for (cnt = 0; cnt < TOTAL_SECTIONS; cnt++)
            {
                if (_liveList[cnt] != 0)
                    _cptData[cnt] = new ByteAccess(_resMan.CptResOpen(_objectList[cnt]), Screen.Header.Size);
                else
                    _cptData[cnt] = null;
            }
        }

        public void CloseSection(uint scriptVar)
        {
            throw new System.NotImplementedException();
        }

        public SwordObject FetchObject(uint id)
        {
            var addr = _cptData[id / ITM_PER_SEC];
            if (addr == null)
                throw new InvalidOperationException($"fetchObject: section {id / ITM_PER_SEC} is not open");
            id &= ITM_ID;
            // DON'T do endian conversion here. it's already done.
            //return (Object*)(addr + *(uint32*)(addr + (id + 1) * 4));
            var offset = addr.Data.ToUInt32((int)(addr.Offset + (id + 1) * 4));
            return new SwordObject(addr.Data, (int)(addr.Offset + offset));
        }

        public void MegaEntering(ushort section)
        {
            _liveList[section]++;
            if (_liveList[section] == 1)
                _cptData[section] = new ByteAccess(_resMan.CptResOpen(_objectList[section]), Screen.Header.Size);
        }

        static readonly uint[] _objectList = { //a table of pointers to object files
	        0,			// 0

	        Sword1Res.COMP1,		// 1		PARIS 1
	        Sword1Res.COMP2,		// 2
	        Sword1Res.COMP3,		// 3
	        Sword1Res.COMP4,		// 4
	        Sword1Res.COMP5,		// 5
	        Sword1Res.COMP6,		// 6
	        Sword1Res.COMP7,		// 7
	        Sword1Res.COMP8,		// 8

	        Sword1Res.COMP9,		// 9		PARIS 2
	        Sword1Res.COMP10,		// 10
	        Sword1Res.COMP11,		// 11
	        Sword1Res.COMP12,		// 12
	        Sword1Res.COMP13,		// 13
	        Sword1Res.COMP14,		// 14
	        Sword1Res.COMP15,		// 15
	        Sword1Res.COMP16,		// 16
	        Sword1Res.COMP17,		// 17
	        Sword1Res.COMP18,		// 18

	        Sword1Res.COMP19,		// 19		IRELAND
	        Sword1Res.COMP20,		// 20
	        Sword1Res.COMP21,		// 21
	        Sword1Res.COMP22,		// 22
	        Sword1Res.COMP23,		// 23
	        Sword1Res.COMP24,		// 24
	        Sword1Res.COMP25,		// 25
	        Sword1Res.COMP26,		// 26

	        Sword1Res.COMP27,		// 27		PARIS 3
	        Sword1Res.COMP28,		// 28
	        Sword1Res.COMP29,		// 29
	        Sword1Res.COMP30,		// 30 - Heart Monitor
	        Sword1Res.COMP31,		// 31
	        Sword1Res.COMP32,		// 32
	        Sword1Res.COMP33,		// 33
	        Sword1Res.COMP34,		// 34
	        Sword1Res.COMP35,		// 35

	        Sword1Res.COMP36,		// 36		PARIS 4
	        Sword1Res.COMP37,		// 37
	        Sword1Res.COMP38,		// 38
	        Sword1Res.COMP39,		// 39
	        Sword1Res.COMP40,		// 40
	        Sword1Res.COMP41,		// 41
	        Sword1Res.COMP42,		// 42
	        Sword1Res.COMP43,		// 43
	        0,				// 44

	        Sword1Res.COMP45,		// 45		SYRIA
	        Sword1Res.COMP46,		// 46		PARIS 4
	        Sword1Res.COMP47,		// 47
	        Sword1Res.COMP48,		// 48		PARIS 4
	        Sword1Res.COMP49,		// 49
	        Sword1Res.COMP50,		// 50
	        0,				// 51
	        0,				// 52
	        Sword1Res.COMP53,		// 53
	        Sword1Res.COMP54,		// 54
	        Sword1Res.COMP55,		// 55

	        Sword1Res.COMP56,		// 56		SPAIN
	        Sword1Res.COMP57,		// 57
	        Sword1Res.COMP58,		// 58
	        Sword1Res.COMP59,		// 59
	        Sword1Res.COMP60,		// 60
	        Sword1Res.COMP61,		// 61
	        Sword1Res.COMP62,		// 62

	        Sword1Res.COMP63,		// 63		NIGHT TRAIN
	        0,				// 64
	        Sword1Res.COMP65,		// 65
	        Sword1Res.COMP66,		// 66
	        Sword1Res.COMP67,		// 67
	        0,				// 68
	        Sword1Res.COMP69,		// 69
	        0,				// 70

	        Sword1Res.COMP71,		// 71		SCOTLAND
	        Sword1Res.COMP72,		// 72
	        Sword1Res.COMP73,		// 73
	        Sword1Res.COMP74,		// 74		END SEQUENCE IN SECRET_CRYPT
	        Sword1Res.COMP75,		// 75
	        Sword1Res.COMP76,		// 76
	        Sword1Res.COMP77,		// 77
	        Sword1Res.COMP78,		// 78
	        Sword1Res.COMP79,		// 79

	        Sword1Res.COMP80,		// 80		PARIS MAP

	        Sword1Res.COMP81,		// 81	Full-screen for "Asstair" in Paris2

	        Sword1Res.COMP55,		// 82	Full-screen BRITMAP in sc55 (Syrian Cave)
	        0,				// 83
	        0,				// 84
	        0,				// 85

	        Sword1Res.COMP86,		// 86		EUROPE MAP
	        Sword1Res.COMP48,		// 87		fudged in for normal window (sc48)
	        Sword1Res.COMP48,		// 88		fudged in for filtered window (sc48)
	        0,				// 89

	        Sword1Res.COMP90,		// 90		PHONE SCREEN
	        Sword1Res.COMP91,		// 91		ENVELOPE SCREEN
	        Sword1Res.COMP17,		// 92		fudged in for George close-up surprised in sc17 wardrobe
	        Sword1Res.COMP17,		// 93		fudged in for George close-up inquisitive in sc17 wardrobe
	        Sword1Res.COMP29,		// 94		fudged in for George close-up in sc29 sarcophagus
	        Sword1Res.COMP38,		// 95		fudged in for George close-up in sc29 sarcophagus
	        Sword1Res.COMP42,		// 96		fudged in for chalice close-up from sc42
	        0,				// 97
	        0,				// 98
	        Sword1Res.COMP99,		// 99		MESSAGE SCREEN (BLANK)

	        0,				// 100
	        0,				// 101
	        0,				// 102
	        0,				// 103
	        0,				// 104
	        0,				// 105
	        0,				// 106
	        0,				// 107
	        0,				// 108
	        0,				// 109

	        0,				// 110
	        0,				// 111
	        0,				// 112
	        0,				// 113
	        0,				// 114
	        0,				// 115
	        0,				// 116
	        0,				// 117
	        0,				// 118
	        0,				// 119

	        0,				// 120
	        0,				// 121
	        0,				// 122
	        0,				// 123
	        0,				// 124
	        0,				// 125
	        0,				// 126
	        0,				// 127

        //mega sections
	        Sword1Res.MEGA_GEO,		// 128 mega_one the player
	        Sword1Res.MEGA_NICO,		// 129 mega_two
	        Sword1Res.MEGA_MUS,		// 130
	        Sword1Res.MEGA_BENOIR,	// 131
	        0,				// 132
	        Sword1Res.MEGA_ROSSO,		// 133
	        Sword1Res.MEGA_DUANE,		// 134
        // james megas
	        0,					// 135
	        0,					// 136
	        0,					// 137
	        0,					// 138
	        0,					// 139
	        0,					// 140
	        0,					// 141
	        0,					// 142
	        0,					// 143

        // jeremy megas
	        0,					// 144 mega_phone
	        Sword1Res.MEGA_MOUE,			// 145 mega_moue
	        Sword1Res.MEGA_ALBERT,		// 146 mega_albert
	        0,					// 147
	        0,					// 148
	        Sword1Res.TEXT_OBS,			// 149
        };


    }
}