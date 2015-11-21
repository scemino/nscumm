using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.IO;

namespace NScumm.Sword1
{
    internal enum HelperScripts
    {
        HELP_IRELAND = 0,
        HELP_SYRIA,
        HELP_SPAIN,
        HELP_NIGHTTRAIN,
        HELP_SCOTLAND,
        HELP_WHITECOAT,
        HELP_SPAIN2
    }

    internal partial class Logic
    {
        private const int MAX_STACK_SIZE = 10;
        private const int SCRIPT_VERSION = 13;

        private const int NON_ZERO_SCRIPT_VARS = 95;
        private const int NUM_SCRIPT_VARS = 1179;

        private const int SCRIPT_CONT = 1;
        private const int SCRIPT_STOP = 0;

        public const int SAM = 2162689;
        public const int PLAYER = 8388608;
        public const int GEORGE = 8388608;
        public const int NICO = 8454144;
        public const int BENOIR = 8585216;
        public const int ROSSO = 8716288;
        public const int DUANE = 8781824;
        public const int MOUE = 9502720;
        public const int ALBERT = 9568256;

        public const int SAND_25 = 1638407;
        public const int HOLDING_REPLICA_25 = 1638408;
        public const int GMASTER_79 = 5177345;
        public const int SCR_std_off = 0 * 0x10000 + 6;
        public const int SCR_exit0 = 0 * 0x10000 + 7;
        public const int SCR_exit1 = 0 * 0x10000 + 8;
        public const int SCR_exit2 = 0 * 0x10000 + 9;
        public const int SCR_exit3 = 0 * 0x10000 + 10;
        public const int SCR_exit4 = 0 * 0x10000 + 11;
        public const int SCR_exit5 = 0 * 0x10000 + 12;
        public const int SCR_exit6 = 0 * 0x10000 + 13;
        public const int SCR_exit7 = 0 * 0x10000 + 14;
        public const int SCR_exit8 = 0 * 0x10000 + 15;
        public const int SCR_exit9 = 0 * 0x10000 + 16;
        public const int LEFT_SCROLL_POINTER = 8388610;
        public const int RIGHT_SCROLL_POINTER = 8388611;
        public const int FLOOR_63 = 4128768;
        public const int ROOF_63 = 4128779;
        public const int GUARD_ROOF_63 = 4128781;
        public const int LEFT_TREE_POINTER_71 = 4653058;
        public const int RIGHT_TREE_POINTER_71 = 4653059;
        public const int SCR_menu_look = 0 * 0x10000 + 24;
        public const int SCR_icon_combine_script = 0 * 0x10000 + 25;

        public const int STAT_MOUSE = 1;
        public const int STAT_LOGIC = 2;
        public const int STAT_EVENTS = 4;
        public const int STAT_FORE = 8;
        public const int STAT_BACK = 16;
        public const int STAT_SORT = 32;
        public const int STAT_SHRINK = 64;
        public const int STAT_BOOKMARK = 128;
        public const int STAT_TALK_WAIT = 256;
        public const int STAT_OVERRIDE = 512;

        private const int LOGIC_idle = 0;
        private const int LOGIC_script = 1;
        private const int LOGIC_AR_animate = 2;
        private const int LOGIC_interaction = 3;
        private const int LOGIC_speech = 4;
        private const int LOGIC_full_anim = 5;
        private const int LOGIC_anim = 6;
        private const int LOGIC_pause = 7;
        private const int LOGIC_wait_for_sync = 8;
        private const int LOGIC_quit = 9;
        private const int LOGIC_restart = 10;
        private const int LOGIC_bookmark = 11;
        private const int LOGIC_wait_for_talk = 12;
        private const int LOGIC_start_talk = 13;
        private const int LOGIC_choose = 14;
        private const int LOGIC_new_script = 15;
        private const int LOGIC_pause_for_event = 16;

        private const int IT_MCODE = 1;               // Call an mcode routine
        private const int IT_PUSHNUMBER = 2;               // push a number on the stack
        private const int IT_PUSHVARIABLE = 3;               // push a variable on the stack

        private const int IT_NOTEQUAL = 4;
        private const int IT_ISEQUAL = 5;
        private const int IT_PLUS = 6;
        private const int IT_TIMES = 7;
        private const int IT_ANDAND = 8;
        private const int IT_OROR = 9;
        private const int IT_LESSTHAN = 10;
        private const int IT_NOT = 11;
        private const int IT_MINUS = 12;
        private const int IT_AND = 13;
        private const int IT_OR = 14;
        private const int IT_GTE = 15;      // >=
        private const int IT_LTE = 16;      // <=
        private const int IT_DEVIDE = 17;      // <=
        private const int IT_GT = 18;      // >

        private const int IT_SCRIPTEND = 20;
        private const int IT_POPVAR = 21;
        private const int IT_POPLONGOFFSET = 22;
        private const int IT_PUSHLONGOFFSET = 23;
        private const int IT_SKIPONFALSE = 24;
        private const int IT_SKIP = 25;
        private const int IT_SWITCH = 26;
        private const int IT_SKIPONTRUE = 27;
        private const int IT_PRINTF = 28;
        private const int IT_RESTARTSCRIPT = 30;
        private const int IT_POPWORDOFFSET = 31;
        private const int IT_PUSHWORDOFFSET = 32;

        private SwordEngine _vm;
        private IMixer _mixer;
        public static uint[] ScriptVars = new uint[NUM_SCRIPT_VARS];
        private uint _newScript; // <= ugly, but I can't avoid it.

        private static readonly uint[,] _scriptVarInit = new uint[NON_ZERO_SCRIPT_VARS, 2]
        {
            {42, 448}, {43, 378}, {51, 1}, {92, 1}, {147, 71}, {201, 1},
            {209, 1}, {215, 1}, {242, 2}, {244, 1}, {246, 3}, {247, 1},
            {253, 1}, {297, 1}, {398, 1}, {508, 1}, {605, 1}, {606, 1},
            {701, 1}, {709, 1}, {773, 1}, {843, 1}, {907, 1}, {923, 1},
            {966, 1}, {988, 2}, {1058, 1}, {1059, 2}, {1060, 3}, {1061, 4},
            {1062, 5}, {1063, 6}, {1064, 7}, {1065, 8}, {1066, 9}, {1067, 10},
            {1068, 11}, {1069, 12}, {1070, 13}, {1071, 14}, {1072, 15}, {1073, 16},
            {1074, 17}, {1075, 18}, {1076, 19}, {1077, 20}, {1078, 21}, {1079, 22},
            {1080, 23}, {1081, 24}, {1082, 25}, {1083, 26}, {1084, 27}, {1085, 28},
            {1086, 29}, {1087, 30}, {1088, 31}, {1089, 32}, {1090, 33}, {1091, 34},
            {1092, 35}, {1093, 36}, {1094, 37}, {1095, 38}, {1096, 39}, {1097, 40},
            {1098, 41}, {1099, 42}, {1100, 43}, {1101, 44}, {1102, 48}, {1103, 45},
            {1104, 47}, {1105, 49}, {1106, 50}, {1107, 52}, {1108, 54}, {1109, 56},
            {1110, 57}, {1111, 58}, {1112, 59}, {1113, 60}, {1114, 61}, {1115, 62},
            {1116, 63}, {1117, 64}, {1118, 65}, {1119, 66}, {1120, 67}, {1121, 68},
            {1122, 69}, {1123, 71}, {1124, 72}, {1125, 73}, {1126, 74}
        };

        private bool _textRunning;
        private bool _speechRunning;
        private bool _speechFinished;

        private readonly byte[][] _startData =
        {
            StaticRes.g_startPos0.ToArray(),
            StaticRes.g_startPos1.ToArray()
        };

        private ObjectMan _objMan;
        private Music _music;

        public Logic(SwordEngine vm, ObjectMan objectMan, ResMan resMan, Screen screen, Mouse mouse, Music music, IMixer mixer, Sound sound)
        {
            _vm = vm;
            _objMan = objectMan;
            _resMan = resMan;
            _screen = screen;
            _mouse = mouse;
            _music = music;
            _sound = sound;
            _mixer = mixer;

            SetupMcodeTable();
        }

        private void SetupMcodeTable()
        {
            _mcodeTable = new Func<SwordObject, int, int, int, int, int, int, int, int>[]
            {
                fnBackground,
                fnForeground,
                fnSort,
                fnNoSprite,
                fnMegaSet,
                fnAnim,
                fnSetFrame,
                fnFullAnim,
                fnFullSetFrame,
                fnFadeDown,
                fnFadeUp,
                fnCheckFade,
                fnSetSpritePalette,
                fnSetWholePalette,
                fnSetFadeTargetPalette,
                fnSetPaletteToFade,
                fnSetPaletteToCut,
                fnPlaySequence,
                fnIdle,
                fnPause,
                fnPauseSeconds,
                fnQuit,
                fnKillId,
                fnSuicide,
                fnNewScript,
                fnSubScript,
                fnRestartScript,
                fnSetBookmark,
                fnGotoBookmark,
                fnSendSync,
                fnWaitSync,
                cfnClickInteract,
                cfnSetScript,
                cfnPresetScript,
                fnInteract,
                fnIssueEvent,
                fnCheckForEvent,
                fnWipeHands,
                fnISpeak,
                fnTheyDo,
                fnTheyDoWeWait,
                fnWeWait,
                fnChangeSpeechText,
                fnTalkError,
                fnStartTalk,
                fnCheckForTextLine,
                fnAddTalkWaitStatusBit,
                fnRemoveTalkWaitStatusBit,
                fnNoHuman,
                fnAddHuman,
                fnBlankMouse,
                fnNormalMouse,
                fnLockMouse,
                fnUnlockMouse,
                fnSetMousePointer,
                fnSetMouseLuggage,
                fnMouseOn,
                fnMouseOff,
                fnChooser,
                fnEndChooser,
                fnStartMenu,
                fnEndMenu,
                cfnReleaseMenu,
                fnAddSubject,
                fnAddObject,
                fnRemoveObject,
                fnEnterSection,
                fnLeaveSection,
                fnChangeFloor,
                fnWalk,
                fnTurn,
                fnStand,
                fnStandAt,
                fnFace,
                fnFaceXy,
                fnIsFacing,
                fnGetTo,
                fnGetToError,
                fnGetPos,
                fnGetGamepadXy,
                fnPlayFx,
                fnStopFx,
                fnPlayMusic,
                fnStopMusic,
                fnInnerSpace,
                fnRandom,
                fnSetScreen,
                fnPreload,
                fnCheckCD,
                fnRestartGame,
                fnQuitGame,
                fnDeathScreen,
                fnSetParallax,
                fnTdebug,
                fnRedFlash,
                fnBlueFlash,
                fnYellow,
                fnGreen,
                fnPurple,
                fnBlack
            };
        }

        public void Initialize()
        {
            Array.Clear(ScriptVars, 0, NUM_SCRIPT_VARS);
            for (byte cnt = 0; cnt < NON_ZERO_SCRIPT_VARS; cnt++)
                ScriptVars[_scriptVarInit[cnt, 0]] = _scriptVarInit[cnt, 1];
            if (SystemVars.IsDemo)
                ScriptVars[(int)ScriptVariableNames.PLAYINGDEMO] = 1;

            // TODO:
            _eventMan = new EventManager();
            _textMan = new Text(_objMan, _resMan, SystemVars.Language == Language.BS1_CZECH);
            _screen.TextManager = _textMan;
            _textRunning = _speechRunning = false;
            _speechFinished = true;
        }

        public void StartPositions(int pos)
        {
            bool spainVisit2 = false;
            if ((pos >= 956) && (pos <= 962))
            {
                spainVisit2 = true;
                pos -= 900;
            }
            if ((pos > 80) || (_startData[pos] == null))
                throw new InvalidOperationException($"Starting in Section {pos} is not supported");

            ScriptVars[(int)ScriptVariableNames.CHANGE_STANCE] = StaticRes.STAND;
            ScriptVars[(int)ScriptVariableNames.GEORGE_CDT_FLAG] = Sword1Res.GEO_TLK_TABLE;

            RunStartScript(_startData[pos]);
            if (spainVisit2)
                RunStartScript(_helperData[(int)HelperScripts.HELP_SPAIN2]);

            if (pos == 0)
                pos = 1;
            var compact = _objMan.FetchObject(PLAYER);
            FnEnterSection(compact, PLAYER, pos, 0, 0, 0, 0, 0);    // (automatically opens the compact resource for that section)
            SystemVars.ControlPanelMode = ControlPanelMode.CP_NORMAL;
            SystemVars.WantFade = true;
        }

        public void Engine()
        {
            // TODO: debug(8, "\n\nNext logic cycle");
            _eventMan.ServiceGlobalEventList();

            for (ushort sectCnt = 0; sectCnt < ObjectMan.TOTAL_SECTIONS; sectCnt++)
            {
                if (_objMan.SectionAlive(sectCnt))
                {
                    uint numCpts = _objMan.FetchNoObjects(sectCnt);
                    for (uint cptCnt = 0; cptCnt < numCpts; cptCnt++)
                    {
                        uint currentId = (uint)(sectCnt * ObjectMan.ITM_PER_SEC + cptCnt);
                        var compact = _objMan.FetchObject(currentId);

                        if ((compact.status & STAT_LOGIC) != 0)
                        { // does the object want to be processed?
                            if ((compact.status & STAT_EVENTS) != 0)
                            {
                                //subscribed to the global-event-switcher? and in logic mode
                                switch (compact.logic)
                                {
                                    case LOGIC_pause_for_event:
                                    case LOGIC_idle:
                                    case LOGIC_AR_animate:
                                        _eventMan.CheckForEvent(compact);
                                        break;
                                }
                            }
                            // TODO: debug(7, "Logic::engine: handling compact %d (%X)", currentId, currentId);
                            ProcessLogic(compact, currentId);
                            compact.sync = 0; // syncs are only available for 1 cycle.
                        }

                        if ((uint)compact.screen == ScriptVars[(int)ScriptVariableNames.SCREEN])
                        {
                            if ((compact.status & STAT_FORE) != 0)
                                _screen.AddToGraphicList(0, currentId);
                            if ((compact.status & STAT_SORT) != 0)
                                _screen.AddToGraphicList(1, currentId);
                            if ((compact.status & STAT_BACK) != 0)
                                _screen.AddToGraphicList(2, currentId);

                            if ((compact.status & STAT_MOUSE) != 0)
                                _mouse.AddToList((int) currentId, compact);
                        }
                    }
                }
            }
        }

        public void UpdateScreenParams()
        {
            var compact = _objMan.FetchObject(PLAYER);
            _screen.SetScrolling((short)(compact.xcoord - ScriptVars[(int)ScriptVariableNames.FEET_X]),
                                 (short)(compact.ycoord - ScriptVars[(int)ScriptVariableNames.FEET_Y]));
        }

        public void NewScreen(uint screen)
        {
            var compact = (SwordObject)_objMan.FetchObject(PLAYER);

            // work around script bug #911508
            if (((screen == 25) || (ScriptVars[(int)ScriptVariableNames.SCREEN] == 25)) && (ScriptVars[(int)ScriptVariableNames.SAND_FLAG] == 4))
            {
                var cpt = _objMan.FetchObject(Logic.SAND_25);
                var george = _objMan.FetchObject(PLAYER);
                if (george.place == HOLDING_REPLICA_25) // is george holding the replica in his hands?
                    FnFullSetFrame(cpt, SAND_25, Sword1Res.IMPFLRCDT, Sword1Res.IMPFLR, 0, 0, 0, 0); // empty impression in floor
                else
                    FnFullSetFrame(cpt, SAND_25, Sword1Res.IMPPLSCDT, Sword1Res.IMPPLS, 0, 0, 0, 0); // impression filled with plaster
            }

            // work around, at screen 69 in psx version TOP menu gets stuck at disabled, fix it at next screen (71)
            if ((screen == 71) && (SystemVars.Platform == Platform.PSX))
                ScriptVars[(int)ScriptVariableNames.TOP_MENU_DISABLED] = 0;

            if (SystemVars.JustRestoredGame != 0)
            { // if we've just restored a game - we want George to be exactly as saved
                FnAddHuman(null, 0, 0, 0, 0, 0, 0, 0);
                if (ScriptVars[(int)ScriptVariableNames.GEORGE_WALKING] != 0)
                { // except that if George was walking when we saveed the game
                    FnStandAt(compact, PLAYER, (int)ScriptVars[(int)ScriptVariableNames.CHANGE_X], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_Y], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_DIR], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_STANCE], 0, 0);
                    FnIdle(compact, PLAYER, 0, 0, 0, 0, 0, 0);
                    ScriptVars[(int)ScriptVariableNames.GEORGE_WALKING] = 0;
                }
                SystemVars.JustRestoredGame = 0;
                _music.StartMusic(ScriptVars[(int)ScriptVariableNames.CURRENT_MUSIC], 1);
            }
            else
            { // if we haven't just restored a game, set George to stand, etc
                compact.screen = (int)ScriptVars[(int)ScriptVariableNames.NEW_SCREEN]; //move the mega/player at this point between screens
                FnStandAt(compact, PLAYER, (int)ScriptVars[(int)ScriptVariableNames.CHANGE_X], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_Y], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_DIR], (int)ScriptVars[(int)ScriptVariableNames.CHANGE_STANCE], 0, 0);
                FnChangeFloor(compact, PLAYER, ScriptVars[(int)ScriptVariableNames.CHANGE_PLACE], 0, 0, 0, 0, 0);
            }
        }

        private int FnEnterSection(SwordObject cpt, int id, int screen, int d, int e, int f, int z, int x)
        {
            if (screen >= ObjectMan.TOTAL_SECTIONS)
                throw new InvalidOperationException($"mega {id} tried entering section {screen}");

            /* if (cpt.o_type == TYPE_PLAYER)
               ^= this was the original condition from the game sourcecode.
               not sure why it doesn't work*/
            if (id == PLAYER)
                ScriptVars[(int)ScriptVariableNames.NEW_SCREEN] = (uint)screen;
            else
                cpt.screen = screen; // move the mega
            _objMan.MegaEntering((ushort)screen);
            return SCRIPT_CONT;
        }

        private void RunStartScript(byte[] data)
        {
            // Here data is a static resource defined in staticres.cpp
            // It is always in little endian
            ushort varId = 0;
            byte fnId = 0;
            uint param1 = 0;
            int i = 0;
            while (data[i] != (int)StartPosOpcodes.opcSeqEnd)
            {
                switch ((StartPosOpcodes)data[i++])
                {
                    case StartPosOpcodes.opcCallFn:
                        fnId = data[i++];
                        param1 = data[i++];
                        StartPosCallFn(fnId, param1, 0, 0);
                        break;
                    case StartPosOpcodes.opcCallFnLong:
                        fnId = data[i++];
                        StartPosCallFn(fnId, data.ToUInt32(i), data.ToUInt32(i + 4), data.ToUInt32(i + 8));
                        i += 12;
                        break;
                    case StartPosOpcodes.opcSetVar8:
                        varId = data.ToUInt16(i);
                        ScriptVars[varId] = data[2];
                        i += 3;
                        break;
                    case StartPosOpcodes.opcSetVar16:
                        varId = data.ToUInt16(i);
                        ScriptVars[varId] = data.ToUInt32(i + 2);
                        i += 4;
                        break;
                    case StartPosOpcodes.opcSetVar32:
                        varId = data.ToUInt16(i);
                        ScriptVars[varId] = data.ToUInt32(i + 2);
                        i += 6;
                        break;
                    case StartPosOpcodes.opcGeorge:
                        ScriptVars[(int)ScriptVariableNames.CHANGE_X] = data.ToUInt16(i + 0);
                        ScriptVars[(int)ScriptVariableNames.CHANGE_Y] = data.ToUInt16(i + 2);
                        ScriptVars[(int)ScriptVariableNames.CHANGE_DIR] = data[i + 4];
                        ScriptVars[(int)ScriptVariableNames.CHANGE_PLACE] = data.ToUInt24(i + 5);
                        i += 8;
                        break;
                    case StartPosOpcodes.opcRunStart:
                        data = _startData[data[i]];
                        i = 0;
                        break;
                    case StartPosOpcodes.opcRunHelper:
                        data = _helperData[data[i]];
                        i = 0;
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected opcode in StartScript");
                }
            }
        }

        private void StartPosCallFn(byte fnId, uint param1, uint param2, uint param3)
        {
            {
                //Object obj = NULL;
                switch ((StartPosOpcodes)fnId)
                {
                    case StartPosOpcodes.opcPlaySequence:
                        FnPlaySequence(null, 0, (int)param1, 0, 0, 0, 0, 0);
                        break;
                    //case StartPosOpcodes.opcAddObject:
                    //    FnAddObject(null, 0, param1, 0, 0, 0, 0, 0);
                    //    break;
                    //case StartPosOpcodes.opcRemoveObject:
                    //    FnRemoveObject(null, 0, param1, 0, 0, 0, 0, 0);
                    //    break;
                    //case StartPosOpcodes.opcMegaSet:
                    //    obj = _objMan.fetchObject(param1);
                    //    FnMegaSet(obj, param1, param2, param3, 0, 0, 0, 0);
                    //    break;
                    //case StartPosOpcodes.opcNoSprite:
                    //    obj = _objMan.fetchObject(param1);
                    //    FnNoSprite(obj, param1, param2, param3, 0, 0, 0, 0);
                    //    break;
                    default:
                        throw new InvalidOperationException($"Illegal fnCallfn argument {fnId}");
                }
            }
        }

        private int FnPlaySequence(object cpt, int id, int sequenceId, int d, int e, int f, int z, int x)
        {
            // A cutscene usually (always?) means the room will change. In the
            // meantime, we don't want any looping sound effects still playing.
            _sound.QuitScreen();

            var player = new MoviePlayer(_vm);
            _screen.ClearScreen();
            player.Load(sequenceId);
            player.Play();
            return SCRIPT_CONT;
        }

        private void ProcessLogic(SwordObject compact, uint id)
        {
            int logicRet;
            do
            {
                switch (compact.logic)
                {
                    case LOGIC_idle:
                        logicRet = 0;
                        break;
                    case LOGIC_pause:
                    case LOGIC_pause_for_event:
                        if (compact.pause != 0)
                        {
                            compact.pause--;
                            logicRet = 0;
                        }
                        else
                        {
                            compact.logic = LOGIC_script;
                            logicRet = 1;
                        }
                        break;
                    case LOGIC_quit:
                        compact.logic = LOGIC_script;
                        logicRet = 0;
                        break;
                    case LOGIC_wait_for_sync:
                        if (compact.sync != 0)
                        {
                            logicRet = 1;
                            compact.logic = LOGIC_script;
                        }
                        else
                            logicRet = 0;
                        break;
                    //case LOGIC_choose:
                    //    ScriptVars[(int)ScriptVariableNames.CUR_ID] = id;
                    //    logicRet = _menu.LogicChooser(compact);
                    //    break;
                    //case LOGIC_wait_for_talk:
                    //    logicRet = LogicWaitTalk(compact);
                    //    break;
                    //case LOGIC_start_talk:
                    //    logicRet = LogicStartTalk(compact);
                    //    break;
                    case LOGIC_script:
                        ScriptVars[(int)ScriptVariableNames.CUR_ID] = id;
                        logicRet = ScriptManager(compact, id);
                        break;
                    case LOGIC_new_script:
                        compact.tree.script_pc[compact.tree.script_level] = (int)_newScript;
                        compact.tree.script_id[compact.tree.script_level] = (int)_newScript;
                        compact.logic = LOGIC_script;
                        logicRet = 1;
                        break;
                    //case LOGIC_AR_animate:
                    //    logicRet = LogicArAnimate(compact, id);
                    //    break;
                    case LOGIC_restart:
                        compact.tree.script_pc[compact.tree.script_level] = compact.tree.script_id[compact.tree.script_level];
                        compact.logic = LOGIC_script;
                        logicRet = 1;
                        break;
                    //case LOGIC_bookmark:
                    //    memcpy(&(compact.tree.o_script_level), &(compact.bookmark.o_script_level), sizeof(ScriptTree));
                    //    if (id == GMASTER_79)
                    //    {
                    //        // workaround for ending script.
                    //        // GMASTER_79 is not prepared for mega_interact receiving INS_quit
                    //        fnSuicide(compact, id, 0, 0, 0, 0, 0, 0);
                    //        logicRet = 0;
                    //    }
                    //    else
                    //    {
                    //        compact.logic = LOGIC_script;
                    //        logicRet = 1;
                    //    }
                    //    break;
                    case LOGIC_speech:
                        logicRet = SpeechDriver(compact);
                        break;
                    //case LOGIC_full_anim:
                    //    logicRet = fullAnimDriver(compact);
                    //    break;
                    case LOGIC_anim:
                        logicRet = AnimDriver(compact);
                        break;
                    default:
                        throw new NotImplementedException();
                }

            } while (logicRet != 0);
        }

        int SpeechDriver(SwordObject compact)
        {
            if ((_speechClickDelay == 0) && (_mouse.TestEvent() & Mouse.BS1L_BUTTON_DOWN) != 0)
                _speechFinished = true;
            if (_speechClickDelay != 0)
                _speechClickDelay--;

            if (_speechRunning)
            {
                if (_sound.SpeechFinished())
                    _speechFinished = true;
            }
            else
            {
                if (compact.speech_time == 0)
                    _speechFinished = true;
                else
                    compact.speech_time--;
            }
            if (_speechFinished)
            {
                if (_speechRunning)
                    _sound.StopSpeech();
                compact.logic = LOGIC_script;
                if (_textRunning)
                {
                    _textMan.ReleaseText(compact.text_id);
                    _objMan.FetchObject((uint)compact.text_id).status = 0; // kill compact linking text sprite
                }
                _speechRunning = _textRunning = false;
                _speechFinished = true;
            }
            if (compact.anim_resource != 0)
            {
                var animData = _resMan.OpenFetchRes((uint)compact.anim_resource);
                var animOff = Screen.Header.Size;
                int numFrames = (int)_resMan.ReadUInt32(animData.ToUInt32(animOff));
                animOff += 4;
                compact.anim_pc++; // go to next frame of anim

                if (_speechFinished || (compact.anim_pc >= numFrames) ||
                        (_speechRunning && (_sound.AmISpeaking() == 0)))
                    compact.anim_pc = 0; //set to frame 0, closed mouth

                AnimUnit animPtr = new AnimUnit(animData, animOff + AnimUnit.Size * compact.anim_pc);
                if ((compact.status & STAT_SHRINK)==0)
                {
                    compact.anim_x = (int) _resMan.ReadUInt32(animPtr.animX);
                    compact.anim_y = (int) _resMan.ReadUInt32(animPtr.animY);
                }
                compact.frame = (int) _resMan.ReadUInt32(animPtr.animFrame);
                _resMan.ResClose((uint) compact.anim_resource);
            }
            return 0;
        }

        private int AnimDriver(SwordObject compact)
        {
            if (compact.sync != 0)
            {
                compact.logic = LOGIC_script;
                return 1;
            }
            var data = _resMan.OpenFetchRes((uint)compact.anim_resource);
            var dataOff = Screen.Header.Size;
            var numFrames = _resMan.ReadUInt32(data.ToUInt32(dataOff));
            AnimUnit animPtr = new AnimUnit(data, dataOff + 4 + compact.anim_pc * AnimUnit.Size);

            if ((compact.status & STAT_SHRINK) == 0)
            {
                compact.anim_x = (int)_resMan.ReadUInt32(animPtr.animX);
                compact.anim_y = (int)_resMan.ReadUInt32(animPtr.animY);
            }

            compact.frame = (int)_resMan.ReadUInt32(animPtr.animFrame);
            compact.anim_pc++;
            if (compact.anim_pc == (int)numFrames)
                compact.logic = LOGIC_script;

            _resMan.ResClose((uint)compact.anim_resource);
            return 0;
        }

        private int ScriptManager(SwordObject compact, uint id)
        {
            int ret;
            do
            {
                int level = compact.tree.script_level;
                uint script = (uint)compact.tree.script_id[level];
                // TODO: Debug::interpretScript(id, level, script, compact.o_tree.o_script_pc[level] & ITM_ID);
                ret = InterpretScript(compact, id, _resMan.LockScript(script), script, compact.tree.script_pc[level] & ObjectMan.ITM_ID);
                _resMan.UnlockScript(script);
                if (ret == 0)
                {
                    if (compact.tree.script_level != 0)
                        compact.tree.script_level--;
                    else
                        throw new InvalidOperationException($"ScriptManager: basescript {script} for cpt {id} ended");
                }
                else
                    compact.tree.script_pc[level] = ret;
            } while (ret == 0);
            return 1;
            //Logic continues - but the script must have changed logic mode
            //this is a radical change from S2.0 where once a script finished there
            //was no more processing for that object on that cycle - the Logic_engine terminated.
            //This meant that new logics that needed immediate action got a first call from the
            //setup function. This was a bit tweeky. This technique ensures that the script is a
            //totally seamless concept that takes up zero cycle time. The only downside is that
            //an FN_quit becomes slightly more convoluted, but so what you might ask.
        }

        private int InterpretScript(SwordObject compact, uint id, Screen.Header scriptModule, uint scriptBase,
            int scriptNum)
        {
            var scriptCode = new UIntAccess(scriptModule.Data, Screen.Header.Size);
            int[] stack = new int[MAX_STACK_SIZE];
            int stackIdx = 0;
            int offset;
            int pc;
            if (scriptModule.type != "Script")
                throw new InvalidOperationException("Invalid script module");
            if (scriptModule.version != SCRIPT_VERSION)
                throw new InvalidOperationException("Illegal script version");
            if (scriptNum < 0)
                throw new InvalidOperationException("negative script number");
            if ((uint)scriptNum >= scriptModule.decomp_length)
                throw new InvalidOperationException("Script number out of bounds");

            if (scriptNum < scriptCode[0])
                pc = (int)scriptCode[scriptNum + 1];
            else
                pc = scriptNum;
            int startOfScript = (int)scriptCode[(int)((scriptBase & ObjectMan.ITM_ID) + 1)];

            int a, b, c, d, e, f;
            int mCodeReturn = 0;
            int mCodeNumber = 0, mCodeArguments = 0;
            uint varNum = 0;
            while (true)
            {
                Debug.Assert((stackIdx >= 0) && (stackIdx <= MAX_STACK_SIZE));
                switch (scriptCode[pc++])
                {
                    case IT_MCODE:
                        a = b = c = d = e = f = 0;
                        mCodeNumber = (int)scriptCode[pc++];
                        mCodeArguments = (int)scriptCode[pc++];
                        switch (mCodeArguments)
                        {
                            case 6:
                                f = stack[--stackIdx];
                                e = stack[--stackIdx];
                                d = stack[--stackIdx];
                                c = stack[--stackIdx];
                                b = stack[--stackIdx];
                                a = stack[--stackIdx];
                                break;
                            case 5:
                                e = stack[--stackIdx];
                                d = stack[--stackIdx];
                                c = stack[--stackIdx];
                                b = stack[--stackIdx];
                                a = stack[--stackIdx];
                                break;
                            case 4:
                                d = stack[--stackIdx];
                                c = stack[--stackIdx];
                                b = stack[--stackIdx];
                                a = stack[--stackIdx];
                                break;
                            case 3:
                                c = stack[--stackIdx];
                                b = stack[--stackIdx];
                                a = stack[--stackIdx];
                                break;
                            case 2:
                                b = stack[--stackIdx];
                                a = stack[--stackIdx];
                                break;
                            case 1:
                                a = stack[--stackIdx];
                                break;
                            case 0:

                                break;
                            default:
                                // TODO: warning("mcode[%d]: too many arguments(%d)", mCodeNumber, mCodeArguments);
                                break;
                        }
                        // TODO: System.Diagnostics.Debug::callMCode(mCodeNumber, mCodeArguments, a, b, c, d, e, f);
                        mCodeReturn = _mcodeTable[mCodeNumber](compact, (int)id, a, b, c, d, e, f);
                        if (mCodeReturn == 0)
                            return pc;
                        break;
                    case IT_PUSHNUMBER:
                        // TODO: debug(9, "IT_PUSH: %d", scriptCode[pc]);
                        stack[stackIdx++] = (int)scriptCode[pc++];
                        break;
                    case IT_PUSHVARIABLE:
                        // TODO: debug(9, "IT_PUSHVARIABLE: ScriptVar[%d] => %d", scriptCode[pc], _scriptVars[scriptCode[pc]]);
                        varNum = scriptCode[pc++];
                        if (Sword1.SystemVars.IsDemo && SystemVars.Platform == Platform.Windows)
                        {
                            if (varNum >= 397) // BS1 Demo has different number of script variables
                                varNum++;
                            if (varNum >= 699)
                                varNum++;
                        }
                        stack[stackIdx++] = (int)ScriptVars[varNum];
                        break;
                    case IT_NOTEQUAL:
                        stackIdx--;
                        // TODO:debug(9, "IT_NOTEQUAL: RESULT = %d", stack[stackIdx - 1] != stack[stackIdx]);
                        stack[stackIdx - 1] = stack[stackIdx - 1] != stack[stackIdx] ? 1 : 0;
                        break;
                    case IT_ISEQUAL:
                        stackIdx--;
                        // TODO:debug(9, "IT_ISEQUAL: RESULT = %d", stack[stackIdx - 1] == stack[stackIdx]);
                        stack[stackIdx - 1] = stack[stackIdx - 1] == stack[stackIdx] ? 1 : 0;
                        break;
                    case IT_PLUS:
                        stackIdx--;
                        // TODO:debug(9, "IT_PLUS: RESULT = %d", stack[stackIdx - 1] + stack[stackIdx]);
                        stack[stackIdx - 1] = stack[stackIdx - 1] + stack[stackIdx];
                        break;
                    case IT_TIMES:
                        stackIdx--;
                        // TODO: debug(9, "IT_TIMES: RESULT = %d", stack[stackIdx - 1] * stack[stackIdx]);
                        stack[stackIdx - 1] = (stack[stackIdx - 1] * stack[stackIdx]);
                        break;
                    case IT_ANDAND:
                        stackIdx--;
                        // TODO: debug(9, "IT_ANDAND: RESULT = %d", stack[stackIdx - 1] && stack[stackIdx]);
                        stack[stackIdx - 1] = (stack[stackIdx - 1] != 0 && stack[stackIdx] != 0) ? 1 : 0;
                        break;
                    case IT_OROR:           // ||
                        stackIdx--;
                        // TODO: debug(9, "IT_OROR: RESULT = %d", stack[stackIdx - 1] || stack[stackIdx]);
                        stack[stackIdx - 1] = (stack[stackIdx - 1] != 0 || stack[stackIdx] != 0) ? 1 : 0;
                        break;
                    case IT_LESSTHAN:
                        stackIdx--;
                        // TODO: debug(9, "IT_LESSTHAN: RESULT = %d", stack[stackIdx - 1] < stack[stackIdx]);
                        stack[stackIdx - 1] = (stack[stackIdx - 1] < stack[stackIdx]) ? 1 : 0;
                        break;
                    case IT_NOT:
                        // TODO: debug(9, "IT_NOT: RESULT = %d", stack[stackIdx - 1] ? 0 : 1);
                        if (stack[stackIdx - 1] != 0)
                            stack[stackIdx - 1] = 0;
                        else
                            stack[stackIdx - 1] = 1;
                        break;
                    case IT_MINUS:
                        stackIdx--;
                        // TODO: debug(9, "IT_MINUS: RESULT = %d", stack[stackIdx - 1] - stack[stackIdx]);
                        stack[stackIdx - 1] = (stack[stackIdx - 1] - stack[stackIdx]);
                        break;
                    case IT_AND:
                        stackIdx--;
                        // TODO: debug(9, "IT_AND: RESULT = %d", stack[stackIdx - 1] & stack[stackIdx]);
                        stack[stackIdx - 1] = (stack[stackIdx - 1] & stack[stackIdx]);
                        break;
                    case IT_OR:
                        stackIdx--;
                        // TODO: debug(9, "IT_OR: RESULT = %d", stack[stackIdx - 1] | stack[stackIdx]);
                        stack[stackIdx - 1] = (stack[stackIdx - 1] | stack[stackIdx]);
                        break;
                    case IT_GTE:
                        stackIdx--;
                        // TODO: debug(9, "IT_GTE: RESULT = %d", stack[stackIdx - 1] >= stack[stackIdx]);
                        stack[stackIdx - 1] = (stack[stackIdx - 1] >= stack[stackIdx]) ? 1 : 0;
                        break;
                    case IT_LTE:
                        stackIdx--;
                        // TODO: debug(9, "IT_LTE: RESULT = %d", stack[stackIdx - 1] <= stack[stackIdx]);
                        stack[stackIdx - 1] = (stack[stackIdx - 1] <= stack[stackIdx]) ? 1 : 0;
                        break;
                    case IT_DEVIDE:
                        stackIdx--;
                        // TODO: debug(9, "IT_DEVIDE: RESULT = %d", stack[stackIdx - 1] / stack[stackIdx]);
                        stack[stackIdx - 1] = (stack[stackIdx - 1] / stack[stackIdx]);
                        break;
                    case IT_GT:
                        stackIdx--;
                        // TODO: debug(9, "IT_GT: RESULT = %d", stack[stackIdx - 1] > stack[stackIdx]);
                        stack[stackIdx - 1] = (stack[stackIdx - 1] > stack[stackIdx]) ? 1 : 0;
                        break;
                    case IT_SCRIPTEND:
                        // TODO: debug(9, "IT_SCRIPTEND");
                        return 0;
                    case IT_POPVAR:         // pop a variable
                        // TODO: debug(9, "IT_POPVAR: ScriptVars[%d] = %d", scriptCode[pc], stack[stackIdx - 1]);
                        varNum = scriptCode[pc++];
                        if (SystemVars.IsDemo && SystemVars.Platform == Platform.Windows)
                        {
                            if (varNum >= 397) // BS1 Demo has different number of script variables
                                varNum++;
                            if (varNum >= 699)
                                varNum++;
                        }
                        ScriptVars[varNum] = (uint)stack[--stackIdx];
                        break;
                    case IT_POPLONGOFFSET:
                        offset = (int)scriptCode[pc++];
                        // TODO: debug(9, "IT_POPLONGOFFSET: Cpt[%d] = %d", offset, stack[stackIdx - 1]);
                        compact.Data.WriteUInt32(compact.Offset + offset, (uint)stack[--stackIdx]);
                        break;
                    case IT_PUSHLONGOFFSET:
                        offset = (int)scriptCode[pc++];
                        // TODO: debug(9, "IT_PUSHLONGOFFSET: PUSH Cpt[%d] (==%d)", offset, *((int32*)((uint8*)compact + offset)));
                        stack[stackIdx++] = (int)compact.Data.ToUInt32(compact.Offset + offset);
                        break;
                    case IT_SKIPONFALSE:
                        // TODO: debug(9, "IT_SKIPONFALSE: %d (%s)", scriptCode[pc], (stack[stackIdx - 1] ? "IS TRUE (NOT SKIPPED)" : "IS FALSE (SKIPPED)"));
                        if (stack[--stackIdx] != 0)
                            pc++;
                        else
                            pc = (int)(pc + scriptCode[pc]);
                        break;
                    case IT_SKIP:
                        // TODO: debug(9, "IT_SKIP: %d", scriptCode[pc]);
                        pc = (int)(pc + scriptCode[pc]);
                        break;
                    case IT_SWITCH:         // The mega switch statement
                        // TODO: debug(9, "IT_SWITCH: [SORRY, NO DEBUG INFO]");
                        {
                            int switchValue = stack[--stackIdx];
                            int switchCount = (int)scriptCode[pc++];
                            int doneSwitch = 0;

                            for (int cnt = 0; (cnt < switchCount) && (doneSwitch == 0); cnt++)
                            {
                                if (switchValue == scriptCode[pc])
                                {
                                    pc = (int)(pc + scriptCode[pc + 1]);
                                    doneSwitch = 1;
                                }
                                else
                                    pc += 2;
                            }
                            if (doneSwitch == 0)
                                pc = (int)(pc + scriptCode[pc]);
                        }
                        break;
                    case IT_SKIPONTRUE:     // skip if expression true
                        // TODO: debug(9, "IT_SKIPONTRUE: %d (%s)", scriptCode[pc], (stack[stackIdx - 1] ? "IS TRUE (SKIPPED)" : "IS FALSE (NOT SKIPPED)"));
                        stackIdx--;
                        if (stack[stackIdx] != 0)
                            pc = (int)(pc + scriptCode[pc]);
                        else
                            pc++;
                        break;
                    case IT_PRINTF:
                        // TODO: debug(0, "IT_PRINTF(%d)", stack[stackIdx]);
                        break;
                    case IT_RESTARTSCRIPT:
                        // TODO: debug(9, "IT_RESTARTSCRIPT");
                        pc = startOfScript;
                        break;
                    case IT_POPWORDOFFSET:
                        offset = (int)scriptCode[pc++];
                        // TODO: debug(9, "IT_POPWORDOFFSET: Cpt[%d] = %d", offset, stack[stackIdx - 1] & 0xFFFF);
                        compact.Data.WriteUInt32(compact.Offset + offset, (uint)(stack[--stackIdx] & 0xffff));
                        break;
                    case IT_PUSHWORDOFFSET:
                        offset = (int)scriptCode[pc++];
                        // TODO: debug(9, "IT_PUSHWORDOFFSET: PUSH Cpt[%d] == %d", offset, (*((int32*)((uint8*)compact + offset))) & 0xffff);
                        stack[stackIdx++] = compact.Data.ToInt32(compact.Offset + offset) & 0xffff;
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid operator {scriptCode[pc - 1]}");
                }
            }
        }

        private int FnChangeFloor(SwordObject cpt, int id, uint floor, int i, int i1, int i2, int i3, int i4)
        {
            cpt.place = (int)floor;
            var floorCpt = _objMan.FetchObject(floor);
            cpt.scale_a = floorCpt.scale_a;
            cpt.scale_b = floorCpt.scale_b;
            return SCRIPT_CONT;
        }

        private int FnIdle(SwordObject cpt, int id, int c, int d, int e, int f, int z, int x)
        {
            cpt.tree.script_level = 0; // force to level 0
            cpt.logic = LOGIC_idle;
            return SCRIPT_STOP;
        }

        private int FnStandAt(SwordObject cpt, int id, int x, int y, int dir, int stance, int a, int b)
        {
            if ((dir < 0) || (dir > 8))
            {
                // TODO: warning("fnStandAt:: invalid direction %d", dir);
                return SCRIPT_CONT;
            }
            if (dir == 8)
                dir = cpt.dir;
            cpt.xcoord = x;
            cpt.ycoord = y;
            return FnStand(cpt, id, dir, stance, 0, 0, 0, 0);
        }

        private int FnStand(SwordObject cpt, int id, int dir, int stance, int c, int d, int a, int b)
        {
            if ((dir < 0) || (dir > 8))
            {
                // TODO: warning("fnStand:: invalid direction %d", dir);
                return SCRIPT_CONT;
            }
            if (dir == 8)
                dir = cpt.dir;
            cpt.resource = cpt.walk_resource;
            cpt.status |= STAT_SHRINK;
            cpt.anim_x = cpt.xcoord;
            cpt.anim_y = cpt.ycoord;
            cpt.frame = 96 + dir;
            cpt.dir = dir;
            return SCRIPT_STOP;
        }

        private int FnAddHuman(object o, int i, int i1, int i2, int i3, int i4, int i5, int i6)
        {
            _mouse.FnAddHuman();
            return SCRIPT_CONT;
        }

        private int FnFullSetFrame(SwordObject cpt, int id, int cdt, int spr, int frameNo, int f, int z, int x)
        {
            var data = _resMan.OpenFetchRes((uint) cdt);
            var dataOff = Screen.Header.Size;

            if (frameNo == LAST_FRAME)
                frameNo = (int) (_resMan.ReadUInt32(data.ToUInt32(dataOff)) - 1);
            dataOff += 4;

            AnimUnit animPtr = new AnimUnit(data, dataOff + AnimUnit.Size * frameNo);
            cpt.anim_x = cpt.xcoord = (int) _resMan.ReadUInt32(animPtr.animX);
            cpt.anim_y = cpt.ycoord = (int) _resMan.ReadUInt32(animPtr.animY);
            cpt.frame = (int) _resMan.ReadUInt32(animPtr.animFrame);

            cpt.resource = spr;
            cpt.status &= ~STAT_SHRINK;

            _resMan.ResClose((uint) cdt);
            return SCRIPT_CONT;
        }

        private static readonly byte[][] _helperData = {
            StaticRes.g_genIreland.ToArray(),
            StaticRes.g_genSyria.ToArray(),
            StaticRes.g_genSpain.ToArray(),
            StaticRes.g_genNightTrain.ToArray(),
            StaticRes.g_genScotland.ToArray(),
            StaticRes.g_genWhiteCoat.ToArray(),
            StaticRes.g_genSpain.ToArray()
        };

        private EventManager _eventMan;
        private Screen _screen;
        private ResMan _resMan;
        private Text _textMan;
        private Mouse _mouse;
        private Func<SwordObject, int, int, int, int, int, int, int, int>[] _mcodeTable;

        public void RunMouseScript(SwordObject cpt, int scriptId)
        {
            Screen.Header script = _resMan.LockScript((uint) scriptId);
            // TODO: debug(9, "running mouse script %d", scriptId);
            InterpretScript(cpt, ScriptVars[(int) ScriptVariableNames.SPECIAL_ITEM], script, (uint) scriptId, scriptId);
            _resMan.UnlockScript((uint) scriptId);
        }

        public int CfnPresetScript(SwordObject cpt, int id, int target, int script, int e, int f, int z, int x)
        {
            var tar = _objMan.FetchObject((uint) target);
            tar.tree.script_level = 0;
            tar.tree.script_pc[0] = script;
            tar.tree.script_id[0] = script;
            if (tar.logic == LOGIC_idle)
                tar.logic = LOGIC_script;
            return SCRIPT_CONT;
        }

        private int GetTextLength(ByteAccess text)
        {
            int count = 0;
            int i = text.Offset;
            while(text.Data[i]!=0)
            {
                count++;
            }
            return count;
        }
    }

    internal class ArrayInt
    {
        private byte[] _data;
        private int _offset;
        private int _length;

        public int this[int index]
        {
            get { return _data.ToInt32(_offset + (index << 2)); }
            set { _data.WriteUInt32(_offset + (index << 2), (uint)value); }
        }

        public ArrayInt(byte[] data, int offset, int length)
        {
            _data = data;
            _offset = offset;
            _length = length;
        }
    }

    internal class ScriptTree
    {         //this is a logic tree, used by OBJECTs
        public const int TOTAL_script_levels = 5;
        private const int Size = 44;

        //logic level
        public int script_level
        {
            get { return _data.ToInt32(_offset); }
            set { _data.WriteUInt32(_offset, (uint)value); }
        }
        public ArrayInt script_id { get; private set; }   //script id's (are unique to each level)
        public ArrayInt script_pc { get; private set; }   //pc of script for each (if script_manager)

        private byte[] _data;
        private int _offset;

        public ScriptTree(byte[] data, int offset)
        {
            _data = data;
            _offset = offset;
            script_id = new ArrayInt(data, offset + 4, TOTAL_script_levels);
            script_pc = new ArrayInt(data, offset + 4 + 4 * TOTAL_script_levels, TOTAL_script_levels);
        }

        public void CopyFrom(ScriptTree scriptTree)
        {
            CopyFrom(scriptTree._data, scriptTree._offset);
        }

        public void CopyFrom(byte[] data, int offset)
        {
            Array.Copy(data, offset, _data, _offset, Size);
        }
    }

    internal class TalkOffset
    {
        public int x
        {
            get { return (int)Data.ToUInt32(Offset); }
            set { Data.WriteUInt32(Offset, (uint)value); }
        }

        public int y
        {
            get { return (int)Data.ToUInt32(Offset + 4); }
            set { Data.WriteUInt32(Offset + 4, (uint)value); }
        }

        public TalkOffset(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;
        }

        public int Offset { get; }

        public byte[] Data { get; }
    }

    internal struct OEventSlot
    {
        public int o_event
        {
            get { return (int)Data.ToUInt32(Offset); }
            set { Data.WriteUInt32(Offset, (uint)value); }
        }

        public int o_event_script
        {
            get { return (int)Data.ToUInt32(Offset + 4); }
            set { Data.WriteUInt32(Offset + 4, (uint)value); }
        }

        public byte[] Data { get; }
        public int Offset { get; }

        public OEventSlot(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;
        }
    }

    internal class WalkData
    {
        public int frame
        {
            get { return (int)Data.ToUInt32(Offset); }
            set { Data.WriteUInt32(Offset, (uint)value); }
        }
        public int x
        {
            get { return (int)Data.ToUInt32(Offset + 4); }
            set { Data.WriteUInt32(Offset + 4, (uint)value); }
        }
        public int y
        {
            get { return (int)Data.ToUInt32(Offset + 8); }
            set { Data.WriteUInt32(Offset + 8, (uint)value); }
        }
        public int step
        {
            get { return (int)Data.ToUInt32(Offset + 12); }
            set { Data.WriteUInt32(Offset + 12, (uint)value); }
        }
        public int dir
        {
            get { return (int)Data.ToUInt32(Offset + 16); }
            set { Data.WriteUInt32(Offset + 16, (uint)value); }
        }

        public byte[] Data { get; }
        public int Offset { get; }

        public WalkData(byte[] data, int offset)
        {
            Data = data;
            Offset = offset;
        }
    }
}
