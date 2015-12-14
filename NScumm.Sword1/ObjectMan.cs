using System;
using NScumm.Core;

namespace NScumm.Sword1
{
    internal class ObjectMan
    {
        public const int TOTAL_SECTIONS = 150;  // number of sections, rooms + mega sections
        public const int TEXT_sect = 149;       // text compacts exist in section 149, probably after all the megas
        public const int ITM_PER_SEC = 0x10000; // 65536 items per section -> was originally called "SIZE"
        public const int ITM_ID = 0xFFFF;//& with this -> originally "NuSIZE"

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

        public void CloseSection(uint screen)
        {
            if (_liveList[screen] == 0)  // close the section that PLAYER has just left, if it's empty now
                _resMan.ResClose(_objectList[screen]);
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

	        SwordRes.COMP1,		// 1		PARIS 1
	        SwordRes.COMP2,		// 2
	        SwordRes.COMP3,		// 3
	        SwordRes.COMP4,		// 4
	        SwordRes.COMP5,		// 5
	        SwordRes.COMP6,		// 6
	        SwordRes.COMP7,		// 7
	        SwordRes.COMP8,		// 8

	        SwordRes.COMP9,		// 9		PARIS 2
	        SwordRes.COMP10,		// 10
	        SwordRes.COMP11,		// 11
	        SwordRes.COMP12,		// 12
	        SwordRes.COMP13,		// 13
	        SwordRes.COMP14,		// 14
	        SwordRes.COMP15,		// 15
	        SwordRes.COMP16,		// 16
	        SwordRes.COMP17,		// 17
	        SwordRes.COMP18,		// 18

	        SwordRes.COMP19,		// 19		IRELAND
	        SwordRes.COMP20,		// 20
	        SwordRes.COMP21,		// 21
	        SwordRes.COMP22,		// 22
	        SwordRes.COMP23,		// 23
	        SwordRes.COMP24,		// 24
	        SwordRes.COMP25,		// 25
	        SwordRes.COMP26,		// 26

	        SwordRes.COMP27,		// 27		PARIS 3
	        SwordRes.COMP28,		// 28
	        SwordRes.COMP29,		// 29
	        SwordRes.COMP30,		// 30 - Heart Monitor
	        SwordRes.COMP31,		// 31
	        SwordRes.COMP32,		// 32
	        SwordRes.COMP33,		// 33
	        SwordRes.COMP34,		// 34
	        SwordRes.COMP35,		// 35

	        SwordRes.COMP36,		// 36		PARIS 4
	        SwordRes.COMP37,		// 37
	        SwordRes.COMP38,		// 38
	        SwordRes.COMP39,		// 39
	        SwordRes.COMP40,		// 40
	        SwordRes.COMP41,		// 41
	        SwordRes.COMP42,		// 42
	        SwordRes.COMP43,		// 43
	        0,				// 44

	        SwordRes.COMP45,		// 45		SYRIA
	        SwordRes.COMP46,		// 46		PARIS 4
	        SwordRes.COMP47,		// 47
	        SwordRes.COMP48,		// 48		PARIS 4
	        SwordRes.COMP49,		// 49
	        SwordRes.COMP50,		// 50
	        0,				// 51
	        0,				// 52
	        SwordRes.COMP53,		// 53
	        SwordRes.COMP54,		// 54
	        SwordRes.COMP55,		// 55

	        SwordRes.COMP56,		// 56		SPAIN
	        SwordRes.COMP57,		// 57
	        SwordRes.COMP58,		// 58
	        SwordRes.COMP59,		// 59
	        SwordRes.COMP60,		// 60
	        SwordRes.COMP61,		// 61
	        SwordRes.COMP62,		// 62

	        SwordRes.COMP63,		// 63		NIGHT TRAIN
	        0,				// 64
	        SwordRes.COMP65,		// 65
	        SwordRes.COMP66,		// 66
	        SwordRes.COMP67,		// 67
	        0,				// 68
	        SwordRes.COMP69,		// 69
	        0,				// 70

	        SwordRes.COMP71,		// 71		SCOTLAND
	        SwordRes.COMP72,		// 72
	        SwordRes.COMP73,		// 73
	        SwordRes.COMP74,		// 74		END SEQUENCE IN SECRET_CRYPT
	        SwordRes.COMP75,		// 75
	        SwordRes.COMP76,		// 76
	        SwordRes.COMP77,		// 77
	        SwordRes.COMP78,		// 78
	        SwordRes.COMP79,		// 79

	        SwordRes.COMP80,		// 80		PARIS MAP

	        SwordRes.COMP81,		// 81	Full-screen for "Asstair" in Paris2

	        SwordRes.COMP55,		// 82	Full-screen BRITMAP in sc55 (Syrian Cave)
	        0,				// 83
	        0,				// 84
	        0,				// 85

	        SwordRes.COMP86,		// 86		EUROPE MAP
	        SwordRes.COMP48,		// 87		fudged in for normal window (sc48)
	        SwordRes.COMP48,		// 88		fudged in for filtered window (sc48)
	        0,				// 89

	        SwordRes.COMP90,		// 90		PHONE SCREEN
	        SwordRes.COMP91,		// 91		ENVELOPE SCREEN
	        SwordRes.COMP17,		// 92		fudged in for George close-up surprised in sc17 wardrobe
	        SwordRes.COMP17,		// 93		fudged in for George close-up inquisitive in sc17 wardrobe
	        SwordRes.COMP29,		// 94		fudged in for George close-up in sc29 sarcophagus
	        SwordRes.COMP38,		// 95		fudged in for George close-up in sc29 sarcophagus
	        SwordRes.COMP42,		// 96		fudged in for chalice close-up from sc42
	        0,				// 97
	        0,				// 98
	        SwordRes.COMP99,		// 99		MESSAGE SCREEN (BLANK)

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
	        SwordRes.MEGA_GEO,		// 128 mega_one the player
	        SwordRes.MEGA_NICO,		// 129 mega_two
	        SwordRes.MEGA_MUS,		// 130
	        SwordRes.MEGA_BENOIR,	// 131
	        0,				// 132
	        SwordRes.MEGA_ROSSO,		// 133
	        SwordRes.MEGA_DUANE,		// 134
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
	        SwordRes.MEGA_MOUE,			// 145 mega_moue
	        SwordRes.MEGA_ALBERT,		// 146 mega_albert
	        0,					// 147
	        0,					// 148
	        SwordRes.TEXT_OBS,			// 149
        };


        public bool SectionAlive(ushort section)
        {
            return _liveList[section] > 0;
        }

        public uint FetchNoObjects(int section)
        {
            if (_cptData[section] == null)
                throw new InvalidOperationException($"fetchNoObjects: section {section} is not open");
            return _cptData[section].Data.ToUInt32(_cptData[section].Offset);
        }

        public ByteAccess LockText(uint textId)
        {
            var lang = SystemVars.Language;
            var text = LockText(textId, lang);
            if (text == null)
            {
                if (lang != Language.BS1_ENGLISH)
                {
                    text = LockText(textId, Language.BS1_ENGLISH);
                    // TODO:
                    //if (text != 0)
                    //    warning("Missing translation for textId %u (\"%s\")", textId, text);
                    UnlockText(textId, Language.BS1_ENGLISH);
                }

                return new ByteAccess(new[] { (byte)' ' }, 0);
            }
            return text;
        }

        private ByteAccess LockText(uint textId, Language lang)
        {
            var addr = _resMan.OpenFetchRes(_textList[(int)(textId / ObjectMan.ITM_PER_SEC), (int)lang]);
            if (addr == null)
                return null;
            var addrOff = Screen.Header.Size;
            // TODO:
            //if ((textId & ITM_ID) >= _resMan->readUint32(addr))
            //{
            //    // Workaround for missing sentences in some langages in the demo.
            //    switch (textId)
            //    {
            //        case 8455194:
            //            return const_cast<char*>(_translationId8455194[lang]);
            //        case 8455195:
            //            return const_cast<char*>(_translationId8455195[lang]);
            //        case 8455196:
            //            return const_cast<char*>(_translationId8455196[lang]);
            //        case 8455197:
            //            return const_cast<char*>(_translationId8455197[lang]);
            //        case 8455198:
            //            return const_cast<char*>(_translationId8455198[lang]);
            //        case 8455199:
            //            return const_cast<char*>(_translationId8455199[lang]);
            //        case 8455200:
            //            return const_cast<char*>(_translationId8455200[lang]);
            //        case 8455201:
            //            return const_cast<char*>(_translationId8455201[lang]);
            //        case 8455202:
            //            return const_cast<char*>(_translationId8455202[lang]);
            //        case 8455203:
            //            return const_cast<char*>(_translationId8455203[lang]);
            //        case 8455204:
            //            return const_cast<char*>(_translationId8455204[lang]);
            //        case 8455205:
            //            return const_cast<char*>(_translationId8455205[lang]);
            //        case 6488080:
            //            return const_cast<char*>(_translationId6488080[lang]);
            //        case 6488081:
            //            return const_cast<char*>(_translationId6488081[lang]);
            //        case 6488082:
            //            return const_cast<char*>(_translationId6488082[lang]);
            //        case 6488083:
            //            return const_cast<char*>(_translationId6488083[lang]);
            //    }

            //    warning("ObjectMan::lockText(%d): only %d texts in file", textId & ITM_ID, _resMan->readUint32(addr));
            //    return NULL;
            //}
            uint offset = _resMan.ReadUInt32(addr.ToUInt32((int)(addrOff + ((textId & ITM_ID) + 1) * 4)));
            if (offset == 0)
            {
                // TODO: Workaround bug for missing sentence in some langages in Syria (see bug #1977094).
                // We use the hardcoded text in this case.
                //if (textId == 2950145)
                //    return const_cast<char*>(_translationId2950145[lang]);

                // TODO: warning("ObjectMan::lockText(%d): text number has no text lines", textId);
                return null;
            }
            return new ByteAccess(addr, (int)(addrOff + offset));
        }

        public void UnlockText(uint textId)
        {
            UnlockText(textId, SystemVars.Language);
        }

        void UnlockText(uint textId, Language lang)
        {
            _resMan.ResClose(_textList[textId / ITM_PER_SEC, (int)lang]);
        }

        public uint FnCheckForTextLine(uint textId)
        {
            byte retVal = 0;
            if (_textList[textId / ITM_PER_SEC, 0] == 0)
                return 0; // section does not exist

            byte lang = (byte)SystemVars.Language;
            var textData = new UIntAccess(_resMan.OpenFetchRes(_textList[textId / ITM_PER_SEC, lang]), Screen.Header.Size);
            if ((textId & ITM_ID) < _resMan.ReadUInt32(textData[0]))
            {
                textData.Offset += 4;
                if (textData[(int)(textId & ITM_ID)] != 0)
                    retVal = 1;
            }
            _resMan.ResClose(_textList[textId / ITM_PER_SEC, lang]);
            return retVal;
        }

        public void MegaLeaving(ushort section, int id)
        {
            if (_liveList[section] == 0)
                throw new InvalidOperationException($"mega {id} is leaving empty section {section}");
            _liveList[section]--;
            if ((_liveList[section] == 0) && (id != Logic.PLAYER))
            {
                _resMan.ResClose(_objectList[section]);
                _cptData[section] = null;
            }
            /* if the player is leaving the section then we have to close the resources after
               mainloop ends, because the screen will still need the resources*/
        }

        static readonly uint[,] _textList = new uint[TOTAL_SECTIONS, 7] {
    {SwordRes.ENGLISH0,        SwordRes.FRENCH0,      SwordRes.GERMAN0,      SwordRes.ITALIAN0,     SwordRes.SPANISH0,     SwordRes.CZECH0,       SwordRes.PORT0},		// 0		INVENTORY		BOTH CD'S - used in almost all locations
	{SwordRes.ENGLISH1,        SwordRes.FRENCH1,      SwordRes.GERMAN1,      SwordRes.ITALIAN1,     SwordRes.SPANISH1,     SwordRes.CZECH1,       SwordRes.PORT1},		// 1		PARIS 1			CD1
	{SwordRes.ENGLISH2,        SwordRes.FRENCH2,      SwordRes.GERMAN2,      SwordRes.ITALIAN2,     SwordRes.SPANISH2,     SwordRes.CZECH2,       SwordRes.PORT2},		// 2								CD1
	{SwordRes.ENGLISH3,        SwordRes.FRENCH3,      SwordRes.GERMAN3,      SwordRes.ITALIAN3,     SwordRes.SPANISH3,     SwordRes.CZECH3,       SwordRes.PORT3},		// 3								CD1
	{SwordRes.ENGLISH4,        SwordRes.FRENCH4,      SwordRes.GERMAN4,      SwordRes.ITALIAN4,     SwordRes.SPANISH4,     SwordRes.CZECH4,       SwordRes.PORT4},		// 4								CD1
	{SwordRes.ENGLISH5,        SwordRes.FRENCH5,      SwordRes.GERMAN5,      SwordRes.ITALIAN5,     SwordRes.SPANISH5,     SwordRes.CZECH5,       SwordRes.PORT5},		// 5								CD1
	{SwordRes.ENGLISH6,        SwordRes.FRENCH6,      SwordRes.GERMAN6,      SwordRes.ITALIAN6,     SwordRes.SPANISH6,     SwordRes.CZECH6,       SwordRes.PORT6},		// 6								CD1
	{SwordRes.ENGLISH7,        SwordRes.FRENCH7,      SwordRes.GERMAN7,      SwordRes.ITALIAN7,     SwordRes.SPANISH7,     SwordRes.CZECH7,       SwordRes.PORT7},		// 7								CD1
	{0,                         0,                      0,                      0,                      0,                      0,                      0              },    	// 8								-
	{SwordRes.ENGLISH9,        SwordRes.FRENCH9,      SwordRes.GERMAN9,      SwordRes.ITALIAN9,     SwordRes.SPANISH9,     SwordRes.CZECH9,       SwordRes.PORT9},		// 9		PARIS 2			CD1
	{0,0,0,0,0,0,0},																																							// 10								-
	{SwordRes.ENGLISH11,       SwordRes.FRENCH11,     SwordRes.GERMAN11,     SwordRes.ITALIAN11,    SwordRes.SPANISH11,    SwordRes.CZECH11,  SwordRes.PORT11},	// 11								CD1
	{SwordRes.ENGLISH12,       SwordRes.FRENCH12,     SwordRes.GERMAN12,     SwordRes.ITALIAN12,    SwordRes.SPANISH12,    SwordRes.CZECH12,  SwordRes.PORT12},	// 12								CD1
	{SwordRes.ENGLISH13,       SwordRes.FRENCH13,     SwordRes.GERMAN13,     SwordRes.ITALIAN13,    SwordRes.SPANISH13,    SwordRes.CZECH13,  SwordRes.PORT13},	// 13								CD1
	{SwordRes.ENGLISH14,       SwordRes.FRENCH14,     SwordRes.GERMAN14,     SwordRes.ITALIAN14,    SwordRes.SPANISH14,    SwordRes.CZECH14,  SwordRes.PORT14},	// 14								CD1
	{SwordRes.ENGLISH15,       SwordRes.FRENCH15,     SwordRes.GERMAN15,     SwordRes.ITALIAN15,    SwordRes.SPANISH15,    SwordRes.CZECH15,  SwordRes.PORT15},	// 15								CD1
	{SwordRes.ENGLISH16,       SwordRes.FRENCH16,     SwordRes.GERMAN16,     SwordRes.ITALIAN16,    SwordRes.SPANISH16,    SwordRes.CZECH16,  SwordRes.PORT16},	// 16								CD1
	{SwordRes.ENGLISH17,       SwordRes.FRENCH17,     SwordRes.GERMAN17,     SwordRes.ITALIAN17,    SwordRes.SPANISH17,    SwordRes.CZECH17,  SwordRes.PORT17},	// 17								CD1
	{SwordRes.ENGLISH18,       SwordRes.FRENCH18,     SwordRes.GERMAN18,     SwordRes.ITALIAN18,    SwordRes.SPANISH18,    SwordRes.CZECH18,  SwordRes.PORT18},	// 18								CD1
	{SwordRes.ENGLISH19,       SwordRes.FRENCH19,     SwordRes.GERMAN19,     SwordRes.ITALIAN19,    SwordRes.SPANISH19,    SwordRes.CZECH19,  SwordRes.PORT19},	// 19		IRELAND			CD2
	{SwordRes.ENGLISH20,       SwordRes.FRENCH20,     SwordRes.GERMAN20,     SwordRes.ITALIAN20,    SwordRes.SPANISH20,    SwordRes.CZECH20,  SwordRes.PORT20},	// 20								CD2
	{SwordRes.ENGLISH21,       SwordRes.FRENCH21,     SwordRes.GERMAN21,     SwordRes.ITALIAN21,    SwordRes.SPANISH21,    SwordRes.CZECH21,  SwordRes.PORT21},	// 21								CD2
	{SwordRes.ENGLISH22,       SwordRes.FRENCH22,     SwordRes.GERMAN22,     SwordRes.ITALIAN22,    SwordRes.SPANISH22,    SwordRes.CZECH22,  SwordRes.PORT22},	// 22								CD2
	{SwordRes.ENGLISH23,       SwordRes.FRENCH23,     SwordRes.GERMAN23,     SwordRes.ITALIAN23,    SwordRes.SPANISH23,    SwordRes.CZECH23,  SwordRes.PORT23},	// 23								CD2
	{SwordRes.ENGLISH24,       SwordRes.FRENCH24,     SwordRes.GERMAN24,     SwordRes.ITALIAN24,    SwordRes.SPANISH24,    SwordRes.CZECH24,  SwordRes.PORT24},	// 24								CD2
	{SwordRes.ENGLISH25,       SwordRes.FRENCH25,     SwordRes.GERMAN25,     SwordRes.ITALIAN25,    SwordRes.SPANISH25,    SwordRes.CZECH25,  SwordRes.PORT25},	// 25								CD2
	{0,0,0,0,0,0,0},																																							// 26								-
	{SwordRes.ENGLISH27,       SwordRes.FRENCH27,     SwordRes.GERMAN27,     SwordRes.ITALIAN27,    SwordRes.SPANISH27,    SwordRes.CZECH27,  SwordRes.PORT27},	// 27		PARIS 3			CD1
	{SwordRes.ENGLISH28,       SwordRes.FRENCH28,     SwordRes.GERMAN28,     SwordRes.ITALIAN28,    SwordRes.SPANISH28,    SwordRes.CZECH28,  SwordRes.PORT28},	// 28								CD1
	{SwordRes.ENGLISH29,       SwordRes.FRENCH29,     SwordRes.GERMAN29,     SwordRes.ITALIAN29,    SwordRes.SPANISH29,    SwordRes.CZECH29,  SwordRes.PORT29},	// 29								CD1
	{0,0,0,0,0,0,0},																																							// 30								-
	{SwordRes.ENGLISH31,       SwordRes.FRENCH31,     SwordRes.GERMAN31,     SwordRes.ITALIAN31,    SwordRes.SPANISH31,    SwordRes.CZECH31,  SwordRes.PORT31},	// 31								CD1
	{SwordRes.ENGLISH32,       SwordRes.FRENCH32,     SwordRes.GERMAN32,     SwordRes.ITALIAN32,    SwordRes.SPANISH32,    SwordRes.CZECH32,  SwordRes.PORT32},	// 32								CD1
	{SwordRes.ENGLISH33,       SwordRes.FRENCH33,     SwordRes.GERMAN33,     SwordRes.ITALIAN33,    SwordRes.SPANISH33,    SwordRes.CZECH33,  SwordRes.PORT33},	// 33								CD1
	{SwordRes.ENGLISH34,       SwordRes.FRENCH34,     SwordRes.GERMAN34,     SwordRes.ITALIAN34,    SwordRes.SPANISH34,    SwordRes.CZECH34,  SwordRes.PORT34},	// 34								CD1
	{SwordRes.ENGLISH35,       SwordRes.FRENCH35,     SwordRes.GERMAN35,     SwordRes.ITALIAN35,    SwordRes.SPANISH35,    SwordRes.CZECH35,  SwordRes.PORT35},	// 35								CD1
	{SwordRes.ENGLISH36,       SwordRes.FRENCH36,     SwordRes.GERMAN36,     SwordRes.ITALIAN36,    SwordRes.SPANISH36,    SwordRes.CZECH36,  SwordRes.PORT36},	// 36		PARIS 4			CD1
	{SwordRes.ENGLISH37,       SwordRes.FRENCH37,     SwordRes.GERMAN37,     SwordRes.ITALIAN37,    SwordRes.SPANISH37,    SwordRes.CZECH37,  SwordRes.PORT37},	// 37								CD1
	{SwordRes.ENGLISH38,       SwordRes.FRENCH38,     SwordRes.GERMAN38,     SwordRes.ITALIAN38,    SwordRes.SPANISH38,    SwordRes.CZECH38,  SwordRes.PORT38},	// 38								CD1
	{SwordRes.ENGLISH39,       SwordRes.FRENCH39,     SwordRes.GERMAN39,     SwordRes.ITALIAN39,    SwordRes.SPANISH39,    SwordRes.CZECH39,  SwordRes.PORT39},	// 39								CD1
	{SwordRes.ENGLISH40,       SwordRes.FRENCH40,     SwordRes.GERMAN40,     SwordRes.ITALIAN40,    SwordRes.SPANISH40,    SwordRes.CZECH40,  SwordRes.PORT40},	// 40								CD1
	{SwordRes.ENGLISH41,       SwordRes.FRENCH41,     SwordRes.GERMAN41,     SwordRes.ITALIAN41,    SwordRes.SPANISH41,    SwordRes.CZECH41,  SwordRes.PORT41},	// 41								CD1
	{SwordRes.ENGLISH42,       SwordRes.FRENCH42,     SwordRes.GERMAN42,     SwordRes.ITALIAN42,    SwordRes.SPANISH42,    SwordRes.CZECH42,  SwordRes.PORT42},	// 42								CD1
	{SwordRes.ENGLISH43,       SwordRes.FRENCH43,     SwordRes.GERMAN43,     SwordRes.ITALIAN43,    SwordRes.SPANISH43,    SwordRes.CZECH43,  SwordRes.PORT43},	// 43								CD1
	{0,0,0,0,0,0,0},																																							// 44								-
	{SwordRes.ENGLISH45,       SwordRes.FRENCH45,     SwordRes.GERMAN45,     SwordRes.ITALIAN45,    SwordRes.SPANISH45,    SwordRes.CZECH45,  SwordRes.PORT45},	// 45		SYRIA				CD2
	{0,0,0,0,0,0,0},																																							// 46		(PARIS 4)		-
	{SwordRes.ENGLISH47,       SwordRes.FRENCH47,     SwordRes.GERMAN47,     SwordRes.ITALIAN47,    SwordRes.SPANISH47,    SwordRes.CZECH47,  SwordRes.PORT47},	// 47								CD2
	{SwordRes.ENGLISH48,       SwordRes.FRENCH48,     SwordRes.GERMAN48,     SwordRes.ITALIAN48,    SwordRes.SPANISH48,    SwordRes.CZECH48,  SwordRes.PORT48},	// 48		(PARIS 4)		CD1
	{SwordRes.ENGLISH49,       SwordRes.FRENCH49,     SwordRes.GERMAN49,     SwordRes.ITALIAN49,    SwordRes.SPANISH49,    SwordRes.CZECH49,  SwordRes.PORT49},	// 49								CD2
	{SwordRes.ENGLISH50,       SwordRes.FRENCH50,     SwordRes.GERMAN50,     SwordRes.ITALIAN50,    SwordRes.SPANISH50,    SwordRes.CZECH50,  SwordRes.PORT50},	// 50								CD2
	{0,0,0,0,0,0,0},																																							// 51								-
	{0,0,0,0,0,0,0},																																							// 52								-
	{0,0,0,0,0,0,0},																																							// 53								-
	{SwordRes.ENGLISH54,       SwordRes.FRENCH54,     SwordRes.GERMAN54,     SwordRes.ITALIAN54,    SwordRes.SPANISH54,    SwordRes.CZECH54,  SwordRes.PORT54},	// 54								CD2
	{SwordRes.ENGLISH55,       SwordRes.FRENCH55,     SwordRes.GERMAN55,     SwordRes.ITALIAN55,    SwordRes.SPANISH55,    SwordRes.CZECH55,  SwordRes.PORT55},	// 55								CD2
	{SwordRes.ENGLISH56,       SwordRes.FRENCH56,     SwordRes.GERMAN56,     SwordRes.ITALIAN56,    SwordRes.SPANISH56,    SwordRes.CZECH56,  SwordRes.PORT56},	// 56		SPAIN				CD2
	{SwordRes.ENGLISH57,       SwordRes.FRENCH57,     SwordRes.GERMAN57,     SwordRes.ITALIAN57,    SwordRes.SPANISH57,    SwordRes.CZECH57,  SwordRes.PORT57},	// 57								CD2
	{SwordRes.ENGLISH58,       SwordRes.FRENCH58,     SwordRes.GERMAN58,     SwordRes.ITALIAN58,    SwordRes.SPANISH58,    SwordRes.CZECH58,  SwordRes.PORT58},	// 58								CD2
	{SwordRes.ENGLISH59,       SwordRes.FRENCH59,     SwordRes.GERMAN59,     SwordRes.ITALIAN59,    SwordRes.SPANISH59,    SwordRes.CZECH59,  SwordRes.PORT59},	// 59								CD2
	{SwordRes.ENGLISH60,       SwordRes.FRENCH60,     SwordRes.GERMAN60,     SwordRes.ITALIAN60,    SwordRes.SPANISH60,    SwordRes.CZECH60,  SwordRes.PORT60},	// 60								CD2
	{SwordRes.ENGLISH61,       SwordRes.FRENCH61,     SwordRes.GERMAN61,     SwordRes.ITALIAN61,    SwordRes.SPANISH61,    SwordRes.CZECH61,  SwordRes.PORT61},	// 61								CD2
	{0,0,0,0,0,0,0},																																							// 62								-
	{SwordRes.ENGLISH63,       SwordRes.FRENCH63,     SwordRes.GERMAN63,     SwordRes.ITALIAN63,    SwordRes.SPANISH63,    SwordRes.CZECH63,  SwordRes.PORT63},	// 63		TRAIN				CD2
	{0,0,0,0,0,0,0},																																							// 64								-
	{SwordRes.ENGLISH65,       SwordRes.FRENCH65,     SwordRes.GERMAN65,     SwordRes.ITALIAN65,    SwordRes.SPANISH65,    SwordRes.CZECH65,  SwordRes.PORT65},	// 65								CD2
	{SwordRes.ENGLISH66,       SwordRes.FRENCH66,     SwordRes.GERMAN66,     SwordRes.ITALIAN66,    SwordRes.SPANISH66,    SwordRes.CZECH66,  SwordRes.PORT66},	// 66								CD2
	{0,0,0,0,0,0,0},																																							// 67								-
	{0,0,0,0,0,0,0},																																							// 68								-
	{SwordRes.ENGLISH69,       SwordRes.FRENCH69,      SwordRes.GERMAN69,      SwordRes.ITALIAN69, SwordRes.SPANISH69, SwordRes.CZECH69,   SwordRes.PORT69},	// 69								CD2
	{0,0,0,0,0,0,0},																																							// 70								-
	{SwordRes.ENGLISH71,       SwordRes.FRENCH71,     SwordRes.GERMAN71,     SwordRes.ITALIAN71,    SwordRes.SPANISH71,    SwordRes.CZECH71,  SwordRes.PORT71},	// 71		SCOTLAND		CD2
	{SwordRes.ENGLISH72,       SwordRes.FRENCH72,     SwordRes.GERMAN72,     SwordRes.ITALIAN72,    SwordRes.SPANISH72,    SwordRes.CZECH72,  SwordRes.PORT72},	// 72								CD2
	{SwordRes.ENGLISH73,       SwordRes.FRENCH73,     SwordRes.GERMAN73,     SwordRes.ITALIAN73,    SwordRes.SPANISH73,    SwordRes.CZECH73,  SwordRes.PORT73},	// 73								CD2
	{SwordRes.ENGLISH74,       SwordRes.FRENCH74,     SwordRes.GERMAN74,     SwordRes.ITALIAN74,    SwordRes.SPANISH74,    SwordRes.CZECH74,  SwordRes.PORT74},	// 74								CD2
	{0,0,0,0,0,0,0},																																							// 75								-
	{0,0,0,0,0,0,0},																																							// 76								-
	{0,0,0,0,0,0,0},																																							// 77								-
	{0,0,0,0,0,0,0},																																							// 78								-
	{0,0,0,0,0,0,0},																																							// 79								-
	{0,0,0,0,0,0,0},																																							// 80								-
	{0,0,0,0,0,0,0},																																							// 81								-
	{0,0,0,0,0,0,0},																																							// 82								-
	{0,0,0,0,0,0,0},																																							// 83								-
	{0,0,0,0,0,0,0},																																							// 84								-
	{0,0,0,0,0,0,0},																																							// 85								-
	{0,0,0,0,0,0,0},																																							// 86								-
	{0,0,0,0,0,0,0},																																							// 87								-
	{0,0,0,0,0,0,0},																																							// 88								-
	{0,0,0,0,0,0,0},																																							// 89								-
	{SwordRes.ENGLISH90,     SwordRes.FRENCH90,       SwordRes.GERMAN90,       SwordRes.ITALIAN90,  SwordRes.SPANISH90,  SwordRes.CZECH90,    SwordRes.PORT90},	// 90		PHONE				BOTH CD'S (NICO & TODRYK PHONE TEXT - can phone nico from a number of sections
	{0,0,0,0,0,0,0},																																							// 91								-
	{0,0,0,0,0,0,0},																																							// 92								-
	{0,0,0,0,0,0,0},																																							// 93								-
	{0,0,0,0,0,0,0},																																							// 94								-
	{0,0,0,0,0,0,0},																																							// 95								-
	{0,0,0,0,0,0,0},																																							// 96								-
	{0,0,0,0,0,0,0},																																							// 97								-
	{0,0,0,0,0,0,0},																																							// 98								-
	{SwordRes.ENGLISH99,     SwordRes.FRENCH99,       SwordRes.GERMAN99,       SwordRes.ITALIAN99,  SwordRes.SPANISH99,  SwordRes.CZECH99,    SwordRes.PORT99},	// 99		MESSAGES		BOTH CD'S - contains general text, most not requiring samples, but includes demo samples
	{0,0,0,0,0,0,0},																																							// 100							-
	{0,0,0,0,0,0,0},																																							// 101							-
	{0,0,0,0,0,0,0},																																							// 102							-
	{0,0,0,0,0,0,0},																																							// 103							-
	{0,0,0,0,0,0,0},																																							// 104							-
	{0,0,0,0,0,0,0},																																							// 105							-
	{0,0,0,0,0,0,0},																																							// 106							-
	{0,0,0,0,0,0,0},																																							// 107							-
	{0,0,0,0,0,0,0},																																							// 108							-
	{0,0,0,0,0,0,0},																																							// 109							-
	{0,0,0,0,0,0,0},																																							// 110							-
	{0,0,0,0,0,0,0},																																							// 111							-
	{0,0,0,0,0,0,0},																																							// 112							-
	{0,0,0,0,0,0,0},																																							// 113							-
	{0,0,0,0,0,0,0},																																							// 114							-
	{0,0,0,0,0,0,0},																																							// 115							-
	{0,0,0,0,0,0,0},																																							// 116							-
	{0,0,0,0,0,0,0},																																							// 117							-
	{0,0,0,0,0,0,0},																																							// 118							-
	{0,0,0,0,0,0,0},																																							// 119							-
	{0,0,0,0,0,0,0},																																							// 120							-
	{0,0,0,0,0,0,0},																																							// 121							-
	{0,0,0,0,0,0,0},																																							// 122							-
	{0,0,0,0,0,0,0},																																							// 123							-
	{0,0,0,0,0,0,0},																																							// 124							-
	{0,0,0,0,0,0,0},																																							// 125							-
	{0,0,0,0,0,0,0},																																							// 126							-
	{0,0,0,0,0,0,0},																																							// 127							-
	{0,0,0,0,0,0,0},																																							// 128							-
	{SwordRes.ENGLISH129,    SwordRes.FRENCH129,  SwordRes.GERMAN129,  SwordRes.ITALIAN129, SwordRes.SPANISH129, SwordRes.CZECH129,   SwordRes.PORT129},	// 129	NICO				BOTH CD'S	- used in screens 1,10,71,72,73
	{0,0,0,0,0,0,0},																																							// 130							-
	{SwordRes.ENGLISH131,    SwordRes.FRENCH131,  SwordRes.GERMAN131,  SwordRes.ITALIAN131, SwordRes.SPANISH131, SwordRes.CZECH131,   SwordRes.PORT131},	// 131	BENOIR			CD1				- used in screens 31..35
	{0,0,0,0,0,0,0},																																							// 132							-
	{SwordRes.ENGLISH133,    SwordRes.FRENCH133,  SwordRes.GERMAN133,  SwordRes.ITALIAN133, SwordRes.SPANISH133, SwordRes.CZECH133,   SwordRes.PORT133},	// 133	ROSSO				CD1				- used in screen 18
	{0,0,0,0,0,0,0},																																							// 134							-
	{0,0,0,0,0,0,0},																																							// 135							-
	{0,0,0,0,0,0,0},																																							// 136							-
	{0,0,0,0,0,0,0},																																							// 137							-
	{0,0,0,0,0,0,0},																																							// 138							-
	{0,0,0,0,0,0,0},																																							// 139							-
	{0,0,0,0,0,0,0},																																							// 140							-
	{0,0,0,0,0,0,0},																																							// 141							-
	{0,0,0,0,0,0,0},																																							// 142							-
	{0,0,0,0,0,0,0},																																							// 143							-
	{0,0,0,0,0,0,0},																																							// 144							-
	{SwordRes.ENGLISH145,    SwordRes.FRENCH145,  SwordRes.GERMAN145,  SwordRes.ITALIAN145, SwordRes.SPANISH145, SwordRes.CZECH145,   SwordRes.PORT145},	// 145	MOUE				CD1				- used in screens 1 & 18
	{SwordRes.ENGLISH146,    SwordRes.FRENCH146,  SwordRes.GERMAN146,  SwordRes.ITALIAN146, SwordRes.SPANISH146, SwordRes.CZECH146,   SwordRes.PORT146},	// 146	ALBERT			CD1				- used in screens 4 & 5
	{0,0,0,0,0,0,0},																																							// 147							-
	{0,0,0,0,0,0,0},																																							// 148							-
	{0,0,0,0,0,0,0},																																							// 149							-
};

        public void SaveLiveList(ushort[] liveBuf)
        {
            Array.Copy(_liveList, liveBuf, TOTAL_SECTIONS);
        }

        public void LoadLiveList(UShortAccess src)
        {
            for (var cnt = 0; cnt < TOTAL_SECTIONS; cnt++)
            {
                if (_liveList[cnt] != 0)
                {
                    _resMan.ResClose(_objectList[cnt]);
                    _cptData[cnt] = null;
                }
                _liveList[cnt] = src[cnt];
                if (_liveList[cnt] != 0)
                    _cptData[cnt] = new ByteAccess(_resMan.CptResOpen(_objectList[cnt]), Screen.Header.Size);
            }
        }
    }
}