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
    {Sword1Res.ENGLISH0,        Sword1Res.FRENCH0,      Sword1Res.GERMAN0,      Sword1Res.ITALIAN0,     Sword1Res.SPANISH0,     Sword1Res.CZECH0,       Sword1Res.PORT0},		// 0		INVENTORY		BOTH CD'S - used in almost all locations
	{Sword1Res.ENGLISH1,        Sword1Res.FRENCH1,      Sword1Res.GERMAN1,      Sword1Res.ITALIAN1,     Sword1Res.SPANISH1,     Sword1Res.CZECH1,       Sword1Res.PORT1},		// 1		PARIS 1			CD1
	{Sword1Res.ENGLISH2,        Sword1Res.FRENCH2,      Sword1Res.GERMAN2,      Sword1Res.ITALIAN2,     Sword1Res.SPANISH2,     Sword1Res.CZECH2,       Sword1Res.PORT2},		// 2								CD1
	{Sword1Res.ENGLISH3,        Sword1Res.FRENCH3,      Sword1Res.GERMAN3,      Sword1Res.ITALIAN3,     Sword1Res.SPANISH3,     Sword1Res.CZECH3,       Sword1Res.PORT3},		// 3								CD1
	{Sword1Res.ENGLISH4,        Sword1Res.FRENCH4,      Sword1Res.GERMAN4,      Sword1Res.ITALIAN4,     Sword1Res.SPANISH4,     Sword1Res.CZECH4,       Sword1Res.PORT4},		// 4								CD1
	{Sword1Res.ENGLISH5,        Sword1Res.FRENCH5,      Sword1Res.GERMAN5,      Sword1Res.ITALIAN5,     Sword1Res.SPANISH5,     Sword1Res.CZECH5,       Sword1Res.PORT5},		// 5								CD1
	{Sword1Res.ENGLISH6,        Sword1Res.FRENCH6,      Sword1Res.GERMAN6,      Sword1Res.ITALIAN6,     Sword1Res.SPANISH6,     Sword1Res.CZECH6,       Sword1Res.PORT6},		// 6								CD1
	{Sword1Res.ENGLISH7,        Sword1Res.FRENCH7,      Sword1Res.GERMAN7,      Sword1Res.ITALIAN7,     Sword1Res.SPANISH7,     Sword1Res.CZECH7,       Sword1Res.PORT7},		// 7								CD1
	{0,                         0,                      0,                      0,                      0,                      0,                      0              },    	// 8								-
	{Sword1Res.ENGLISH9,        Sword1Res.FRENCH9,      Sword1Res.GERMAN9,      Sword1Res.ITALIAN9,     Sword1Res.SPANISH9,     Sword1Res.CZECH9,       Sword1Res.PORT9},		// 9		PARIS 2			CD1
	{0,0,0,0,0,0,0},																																							// 10								-
	{Sword1Res.ENGLISH11,       Sword1Res.FRENCH11,     Sword1Res.GERMAN11,     Sword1Res.ITALIAN11,    Sword1Res.SPANISH11,    Sword1Res.CZECH11,  Sword1Res.PORT11},	// 11								CD1
	{Sword1Res.ENGLISH12,       Sword1Res.FRENCH12,     Sword1Res.GERMAN12,     Sword1Res.ITALIAN12,    Sword1Res.SPANISH12,    Sword1Res.CZECH12,  Sword1Res.PORT12},	// 12								CD1
	{Sword1Res.ENGLISH13,       Sword1Res.FRENCH13,     Sword1Res.GERMAN13,     Sword1Res.ITALIAN13,    Sword1Res.SPANISH13,    Sword1Res.CZECH13,  Sword1Res.PORT13},	// 13								CD1
	{Sword1Res.ENGLISH14,       Sword1Res.FRENCH14,     Sword1Res.GERMAN14,     Sword1Res.ITALIAN14,    Sword1Res.SPANISH14,    Sword1Res.CZECH14,  Sword1Res.PORT14},	// 14								CD1
	{Sword1Res.ENGLISH15,       Sword1Res.FRENCH15,     Sword1Res.GERMAN15,     Sword1Res.ITALIAN15,    Sword1Res.SPANISH15,    Sword1Res.CZECH15,  Sword1Res.PORT15},	// 15								CD1
	{Sword1Res.ENGLISH16,       Sword1Res.FRENCH16,     Sword1Res.GERMAN16,     Sword1Res.ITALIAN16,    Sword1Res.SPANISH16,    Sword1Res.CZECH16,  Sword1Res.PORT16},	// 16								CD1
	{Sword1Res.ENGLISH17,       Sword1Res.FRENCH17,     Sword1Res.GERMAN17,     Sword1Res.ITALIAN17,    Sword1Res.SPANISH17,    Sword1Res.CZECH17,  Sword1Res.PORT17},	// 17								CD1
	{Sword1Res.ENGLISH18,       Sword1Res.FRENCH18,     Sword1Res.GERMAN18,     Sword1Res.ITALIAN18,    Sword1Res.SPANISH18,    Sword1Res.CZECH18,  Sword1Res.PORT18},	// 18								CD1
	{Sword1Res.ENGLISH19,       Sword1Res.FRENCH19,     Sword1Res.GERMAN19,     Sword1Res.ITALIAN19,    Sword1Res.SPANISH19,    Sword1Res.CZECH19,  Sword1Res.PORT19},	// 19		IRELAND			CD2
	{Sword1Res.ENGLISH20,       Sword1Res.FRENCH20,     Sword1Res.GERMAN20,     Sword1Res.ITALIAN20,    Sword1Res.SPANISH20,    Sword1Res.CZECH20,  Sword1Res.PORT20},	// 20								CD2
	{Sword1Res.ENGLISH21,       Sword1Res.FRENCH21,     Sword1Res.GERMAN21,     Sword1Res.ITALIAN21,    Sword1Res.SPANISH21,    Sword1Res.CZECH21,  Sword1Res.PORT21},	// 21								CD2
	{Sword1Res.ENGLISH22,       Sword1Res.FRENCH22,     Sword1Res.GERMAN22,     Sword1Res.ITALIAN22,    Sword1Res.SPANISH22,    Sword1Res.CZECH22,  Sword1Res.PORT22},	// 22								CD2
	{Sword1Res.ENGLISH23,       Sword1Res.FRENCH23,     Sword1Res.GERMAN23,     Sword1Res.ITALIAN23,    Sword1Res.SPANISH23,    Sword1Res.CZECH23,  Sword1Res.PORT23},	// 23								CD2
	{Sword1Res.ENGLISH24,       Sword1Res.FRENCH24,     Sword1Res.GERMAN24,     Sword1Res.ITALIAN24,    Sword1Res.SPANISH24,    Sword1Res.CZECH24,  Sword1Res.PORT24},	// 24								CD2
	{Sword1Res.ENGLISH25,       Sword1Res.FRENCH25,     Sword1Res.GERMAN25,     Sword1Res.ITALIAN25,    Sword1Res.SPANISH25,    Sword1Res.CZECH25,  Sword1Res.PORT25},	// 25								CD2
	{0,0,0,0,0,0,0},																																							// 26								-
	{Sword1Res.ENGLISH27,       Sword1Res.FRENCH27,     Sword1Res.GERMAN27,     Sword1Res.ITALIAN27,    Sword1Res.SPANISH27,    Sword1Res.CZECH27,  Sword1Res.PORT27},	// 27		PARIS 3			CD1
	{Sword1Res.ENGLISH28,       Sword1Res.FRENCH28,     Sword1Res.GERMAN28,     Sword1Res.ITALIAN28,    Sword1Res.SPANISH28,    Sword1Res.CZECH28,  Sword1Res.PORT28},	// 28								CD1
	{Sword1Res.ENGLISH29,       Sword1Res.FRENCH29,     Sword1Res.GERMAN29,     Sword1Res.ITALIAN29,    Sword1Res.SPANISH29,    Sword1Res.CZECH29,  Sword1Res.PORT29},	// 29								CD1
	{0,0,0,0,0,0,0},																																							// 30								-
	{Sword1Res.ENGLISH31,       Sword1Res.FRENCH31,     Sword1Res.GERMAN31,     Sword1Res.ITALIAN31,    Sword1Res.SPANISH31,    Sword1Res.CZECH31,  Sword1Res.PORT31},	// 31								CD1
	{Sword1Res.ENGLISH32,       Sword1Res.FRENCH32,     Sword1Res.GERMAN32,     Sword1Res.ITALIAN32,    Sword1Res.SPANISH32,    Sword1Res.CZECH32,  Sword1Res.PORT32},	// 32								CD1
	{Sword1Res.ENGLISH33,       Sword1Res.FRENCH33,     Sword1Res.GERMAN33,     Sword1Res.ITALIAN33,    Sword1Res.SPANISH33,    Sword1Res.CZECH33,  Sword1Res.PORT33},	// 33								CD1
	{Sword1Res.ENGLISH34,       Sword1Res.FRENCH34,     Sword1Res.GERMAN34,     Sword1Res.ITALIAN34,    Sword1Res.SPANISH34,    Sword1Res.CZECH34,  Sword1Res.PORT34},	// 34								CD1
	{Sword1Res.ENGLISH35,       Sword1Res.FRENCH35,     Sword1Res.GERMAN35,     Sword1Res.ITALIAN35,    Sword1Res.SPANISH35,    Sword1Res.CZECH35,  Sword1Res.PORT35},	// 35								CD1
	{Sword1Res.ENGLISH36,       Sword1Res.FRENCH36,     Sword1Res.GERMAN36,     Sword1Res.ITALIAN36,    Sword1Res.SPANISH36,    Sword1Res.CZECH36,  Sword1Res.PORT36},	// 36		PARIS 4			CD1
	{Sword1Res.ENGLISH37,       Sword1Res.FRENCH37,     Sword1Res.GERMAN37,     Sword1Res.ITALIAN37,    Sword1Res.SPANISH37,    Sword1Res.CZECH37,  Sword1Res.PORT37},	// 37								CD1
	{Sword1Res.ENGLISH38,       Sword1Res.FRENCH38,     Sword1Res.GERMAN38,     Sword1Res.ITALIAN38,    Sword1Res.SPANISH38,    Sword1Res.CZECH38,  Sword1Res.PORT38},	// 38								CD1
	{Sword1Res.ENGLISH39,       Sword1Res.FRENCH39,     Sword1Res.GERMAN39,     Sword1Res.ITALIAN39,    Sword1Res.SPANISH39,    Sword1Res.CZECH39,  Sword1Res.PORT39},	// 39								CD1
	{Sword1Res.ENGLISH40,       Sword1Res.FRENCH40,     Sword1Res.GERMAN40,     Sword1Res.ITALIAN40,    Sword1Res.SPANISH40,    Sword1Res.CZECH40,  Sword1Res.PORT40},	// 40								CD1
	{Sword1Res.ENGLISH41,       Sword1Res.FRENCH41,     Sword1Res.GERMAN41,     Sword1Res.ITALIAN41,    Sword1Res.SPANISH41,    Sword1Res.CZECH41,  Sword1Res.PORT41},	// 41								CD1
	{Sword1Res.ENGLISH42,       Sword1Res.FRENCH42,     Sword1Res.GERMAN42,     Sword1Res.ITALIAN42,    Sword1Res.SPANISH42,    Sword1Res.CZECH42,  Sword1Res.PORT42},	// 42								CD1
	{Sword1Res.ENGLISH43,       Sword1Res.FRENCH43,     Sword1Res.GERMAN43,     Sword1Res.ITALIAN43,    Sword1Res.SPANISH43,    Sword1Res.CZECH43,  Sword1Res.PORT43},	// 43								CD1
	{0,0,0,0,0,0,0},																																							// 44								-
	{Sword1Res.ENGLISH45,       Sword1Res.FRENCH45,     Sword1Res.GERMAN45,     Sword1Res.ITALIAN45,    Sword1Res.SPANISH45,    Sword1Res.CZECH45,  Sword1Res.PORT45},	// 45		SYRIA				CD2
	{0,0,0,0,0,0,0},																																							// 46		(PARIS 4)		-
	{Sword1Res.ENGLISH47,       Sword1Res.FRENCH47,     Sword1Res.GERMAN47,     Sword1Res.ITALIAN47,    Sword1Res.SPANISH47,    Sword1Res.CZECH47,  Sword1Res.PORT47},	// 47								CD2
	{Sword1Res.ENGLISH48,       Sword1Res.FRENCH48,     Sword1Res.GERMAN48,     Sword1Res.ITALIAN48,    Sword1Res.SPANISH48,    Sword1Res.CZECH48,  Sword1Res.PORT48},	// 48		(PARIS 4)		CD1
	{Sword1Res.ENGLISH49,       Sword1Res.FRENCH49,     Sword1Res.GERMAN49,     Sword1Res.ITALIAN49,    Sword1Res.SPANISH49,    Sword1Res.CZECH49,  Sword1Res.PORT49},	// 49								CD2
	{Sword1Res.ENGLISH50,       Sword1Res.FRENCH50,     Sword1Res.GERMAN50,     Sword1Res.ITALIAN50,    Sword1Res.SPANISH50,    Sword1Res.CZECH50,  Sword1Res.PORT50},	// 50								CD2
	{0,0,0,0,0,0,0},																																							// 51								-
	{0,0,0,0,0,0,0},																																							// 52								-
	{0,0,0,0,0,0,0},																																							// 53								-
	{Sword1Res.ENGLISH54,       Sword1Res.FRENCH54,     Sword1Res.GERMAN54,     Sword1Res.ITALIAN54,    Sword1Res.SPANISH54,    Sword1Res.CZECH54,  Sword1Res.PORT54},	// 54								CD2
	{Sword1Res.ENGLISH55,       Sword1Res.FRENCH55,     Sword1Res.GERMAN55,     Sword1Res.ITALIAN55,    Sword1Res.SPANISH55,    Sword1Res.CZECH55,  Sword1Res.PORT55},	// 55								CD2
	{Sword1Res.ENGLISH56,       Sword1Res.FRENCH56,     Sword1Res.GERMAN56,     Sword1Res.ITALIAN56,    Sword1Res.SPANISH56,    Sword1Res.CZECH56,  Sword1Res.PORT56},	// 56		SPAIN				CD2
	{Sword1Res.ENGLISH57,       Sword1Res.FRENCH57,     Sword1Res.GERMAN57,     Sword1Res.ITALIAN57,    Sword1Res.SPANISH57,    Sword1Res.CZECH57,  Sword1Res.PORT57},	// 57								CD2
	{Sword1Res.ENGLISH58,       Sword1Res.FRENCH58,     Sword1Res.GERMAN58,     Sword1Res.ITALIAN58,    Sword1Res.SPANISH58,    Sword1Res.CZECH58,  Sword1Res.PORT58},	// 58								CD2
	{Sword1Res.ENGLISH59,       Sword1Res.FRENCH59,     Sword1Res.GERMAN59,     Sword1Res.ITALIAN59,    Sword1Res.SPANISH59,    Sword1Res.CZECH59,  Sword1Res.PORT59},	// 59								CD2
	{Sword1Res.ENGLISH60,       Sword1Res.FRENCH60,     Sword1Res.GERMAN60,     Sword1Res.ITALIAN60,    Sword1Res.SPANISH60,    Sword1Res.CZECH60,  Sword1Res.PORT60},	// 60								CD2
	{Sword1Res.ENGLISH61,       Sword1Res.FRENCH61,     Sword1Res.GERMAN61,     Sword1Res.ITALIAN61,    Sword1Res.SPANISH61,    Sword1Res.CZECH61,  Sword1Res.PORT61},	// 61								CD2
	{0,0,0,0,0,0,0},																																							// 62								-
	{Sword1Res.ENGLISH63,       Sword1Res.FRENCH63,     Sword1Res.GERMAN63,     Sword1Res.ITALIAN63,    Sword1Res.SPANISH63,    Sword1Res.CZECH63,  Sword1Res.PORT63},	// 63		TRAIN				CD2
	{0,0,0,0,0,0,0},																																							// 64								-
	{Sword1Res.ENGLISH65,       Sword1Res.FRENCH65,     Sword1Res.GERMAN65,     Sword1Res.ITALIAN65,    Sword1Res.SPANISH65,    Sword1Res.CZECH65,  Sword1Res.PORT65},	// 65								CD2
	{Sword1Res.ENGLISH66,       Sword1Res.FRENCH66,     Sword1Res.GERMAN66,     Sword1Res.ITALIAN66,    Sword1Res.SPANISH66,    Sword1Res.CZECH66,  Sword1Res.PORT66},	// 66								CD2
	{0,0,0,0,0,0,0},																																							// 67								-
	{0,0,0,0,0,0,0},																																							// 68								-
	{Sword1Res.ENGLISH69,       Sword1Res.FRENCH69,      Sword1Res.GERMAN69,      Sword1Res.ITALIAN69, Sword1Res.SPANISH69, Sword1Res.CZECH69,   Sword1Res.PORT69},	// 69								CD2
	{0,0,0,0,0,0,0},																																							// 70								-
	{Sword1Res.ENGLISH71,       Sword1Res.FRENCH71,     Sword1Res.GERMAN71,     Sword1Res.ITALIAN71,    Sword1Res.SPANISH71,    Sword1Res.CZECH71,  Sword1Res.PORT71},	// 71		SCOTLAND		CD2
	{Sword1Res.ENGLISH72,       Sword1Res.FRENCH72,     Sword1Res.GERMAN72,     Sword1Res.ITALIAN72,    Sword1Res.SPANISH72,    Sword1Res.CZECH72,  Sword1Res.PORT72},	// 72								CD2
	{Sword1Res.ENGLISH73,       Sword1Res.FRENCH73,     Sword1Res.GERMAN73,     Sword1Res.ITALIAN73,    Sword1Res.SPANISH73,    Sword1Res.CZECH73,  Sword1Res.PORT73},	// 73								CD2
	{Sword1Res.ENGLISH74,       Sword1Res.FRENCH74,     Sword1Res.GERMAN74,     Sword1Res.ITALIAN74,    Sword1Res.SPANISH74,    Sword1Res.CZECH74,  Sword1Res.PORT74},	// 74								CD2
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
	{Sword1Res.ENGLISH90,     Sword1Res.FRENCH90,       Sword1Res.GERMAN90,       Sword1Res.ITALIAN90,  Sword1Res.SPANISH90,  Sword1Res.CZECH90,    Sword1Res.PORT90},	// 90		PHONE				BOTH CD'S (NICO & TODRYK PHONE TEXT - can phone nico from a number of sections
	{0,0,0,0,0,0,0},																																							// 91								-
	{0,0,0,0,0,0,0},																																							// 92								-
	{0,0,0,0,0,0,0},																																							// 93								-
	{0,0,0,0,0,0,0},																																							// 94								-
	{0,0,0,0,0,0,0},																																							// 95								-
	{0,0,0,0,0,0,0},																																							// 96								-
	{0,0,0,0,0,0,0},																																							// 97								-
	{0,0,0,0,0,0,0},																																							// 98								-
	{Sword1Res.ENGLISH99,     Sword1Res.FRENCH99,       Sword1Res.GERMAN99,       Sword1Res.ITALIAN99,  Sword1Res.SPANISH99,  Sword1Res.CZECH99,    Sword1Res.PORT99},	// 99		MESSAGES		BOTH CD'S - contains general text, most not requiring samples, but includes demo samples
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
	{Sword1Res.ENGLISH129,    Sword1Res.FRENCH129,  Sword1Res.GERMAN129,  Sword1Res.ITALIAN129, Sword1Res.SPANISH129, Sword1Res.CZECH129,   Sword1Res.PORT129},	// 129	NICO				BOTH CD'S	- used in screens 1,10,71,72,73
	{0,0,0,0,0,0,0},																																							// 130							-
	{Sword1Res.ENGLISH131,    Sword1Res.FRENCH131,  Sword1Res.GERMAN131,  Sword1Res.ITALIAN131, Sword1Res.SPANISH131, Sword1Res.CZECH131,   Sword1Res.PORT131},	// 131	BENOIR			CD1				- used in screens 31..35
	{0,0,0,0,0,0,0},																																							// 132							-
	{Sword1Res.ENGLISH133,    Sword1Res.FRENCH133,  Sword1Res.GERMAN133,  Sword1Res.ITALIAN133, Sword1Res.SPANISH133, Sword1Res.CZECH133,   Sword1Res.PORT133},	// 133	ROSSO				CD1				- used in screen 18
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
	{Sword1Res.ENGLISH145,    Sword1Res.FRENCH145,  Sword1Res.GERMAN145,  Sword1Res.ITALIAN145, Sword1Res.SPANISH145, Sword1Res.CZECH145,   Sword1Res.PORT145},	// 145	MOUE				CD1				- used in screens 1 & 18
	{Sword1Res.ENGLISH146,    Sword1Res.FRENCH146,  Sword1Res.GERMAN146,  Sword1Res.ITALIAN146, Sword1Res.SPANISH146, Sword1Res.CZECH146,   Sword1Res.PORT146},	// 146	ALBERT			CD1				- used in screens 4 & 5
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