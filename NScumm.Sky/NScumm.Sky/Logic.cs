using NScumm.Core;
using NScumm.Sky.Music;
using System;
using System.Diagnostics;
using System.IO;

namespace NScumm.Sky
{
    partial class Logic
    {
        public const int NumSkyScriptVars = 838;
        private SkyCompact _skyCompact;
        private Screen _skyScreen;
        private Disk _skyDisk;
        private Text _skyText;
        private MusicBase _skyMusic;
        private Sound _skySound;
        private Mouse _skyMouse;
        private uint[] _scriptVariables = new uint[NumSkyScriptVars];
        private uint _currentSection;
        private Action[] _logicTable;
        private Compact _compact;
        private Grid _skyGrid;
        private byte[][] _moduleList = new byte[16][];
        private uint[] _stack = new uint[20];
        private int _stackPtr;
        private Func<uint, uint, uint, bool>[] _mcodeTable;
        private uint[] _objectList = new uint[30];
        private Random _rnd = new Random(Environment.TickCount);

        public Control Control { get; internal set; }

        public uint[] ScriptVariables
        {
            get { return _scriptVariables; }
        }

        public Logic(SkyCompact skyCompact, Screen skyScreen, Disk skyDisk, Text skyText, MusicBase skyMusic, Mouse skyMouse, Sound skySound)
        {
            _skyCompact = skyCompact;
            _skyScreen = skyScreen;
            _skyDisk = skyDisk;
            _skyText = skyText;
            _skyMusic = skyMusic;
            _skySound = skySound;
            _skyMouse = skyMouse;

            _skyGrid = new Grid(this, _skyDisk, _skyCompact);
            // TODO:
            //_skyAutoRoute = new AutoRoute(_skyGrid, _skyCompact);

            SetupLogicTable();
            SetupMcodeTable();

            _currentSection = 0xFF; //force music & sound reload
            InitScriptVariables();
        }

        private void SetupMcodeTable()
        {
            _mcodeTable = new Func<uint, uint, uint, bool>[]
            {
                FnCacheChip,
                FnCacheFast,
                FnDrawScreen,
                FnAr,
                FnArAnimate,
                FnIdle,
                FnInteract,
                FnStartSub,
                FnTheyStartSub,
                FnAssignBase,
                FnDiskMouse,
                FnNormalMouse,
                FnBlankMouse,
                FnCrossMouse,
                FnCursorRight,
                FnCursorLeft,
                FnCursorDown,
                FnOpenHand,
                FnCloseHand,
                FnGetTo,
                FnSetToStand,
                FnTurnTo,
                FnArrived,
                FnLeaving,
                FnSetAlternate,
                FnAltSetAlternate,
                FnKillId,
                FnNoHuman,
                FnAddHuman,
                FnAddButtons,
                FnNoButtons,
                FnSetStop,
                FnClearStop,
                FnPointerText,
                FnQuit,
                FnSpeakMe,
                FnSpeakMeDir,
                FnSpeakWait,
                FnSpeakWaitDir,
                FnChooser,
                FnHighlight,
                FnTextKill,
                FnStopMode,
                FnWeWait,
                FnSendSync,
                FnSendFastSync,
                FnSendRequest,
                FnClearRequest,
                FnCheckRequest,
                FnStartMenu,
                FnUnhighlight,
                FnFaceId,
                FnForeground,
                FnBackground,
                FnNewBackground,
                FnSort,
                FnNoSpriteEngine,
                FnNoSpritesA6,
                FnResetId,
                FnToggleGrid,
                FnPause,
                FnRunAnimMod,
                FnSimpleMod,
                FnRunFrames,
                FnAwaitSync,
                FnIncMegaSet,
                FnDecMegaSet,
                FnSetMegaSet,
                FnMoveItems,
                FnNewList,
                FnAskThis,
                FnRandom,
                FnPersonHere,
                FnToggleMouse,
                FnMouseOn,
                FnMouseOff,
                FnFetchX,
                FnFetchY,
                FnTestList,
                FnFetchPlace,
                FnCustomJoey,
                FnSetPalette,
                FnTextModule,
                FnChangeName,
                FnMiniLoad,
                FnFlushBuffers,
                FnFlushChip,
                FnSaveCoods,
                FnPlotGrid,
                FnRemoveGrid,
                FnEyeball,
                FnCursorUp,
                FnLeaveSection,
                FnEnterSection,
                FnRestoreGame,
                FnRestartGame,
                FnNewSwingSeq,
                FnWaitSwingEnd,
                FnSkipIntroCode,
                FnBlankScreen,
                FnPrintCredit,
                FnLookAt,
                FnLincTextModule,
                FnTextKill2,
                FnSetFont,
                FnStartFx,
                FnStopFx,
                FnStartMusic,
                FnStopMusic,
                FnFadeDown,
                FnFadeUp,
                FnQuitToDos,
                FnPauseFx,
                FnUnPauseFx,
                FnPrintf
            };
        }

        public void ParseSaveData(byte[] data)
        {
            using (var reader = new BinaryReader(new MemoryStream(data)))
            {
                if (!SkyEngine.IsDemo)
                    FnLeaveSection(ScriptVariables[CUR_SECTION], 0, 0);
                for (ushort cnt = 0; cnt < NumSkyScriptVars; cnt++)
                    ScriptVariables[cnt] = reader.ReadUInt32();
                FnEnterSection(ScriptVariables[CUR_SECTION], 0, 0);
            }
        }

        private void InitScriptVariables()
        {
            ScriptVariables[LOGIC_LIST_NO] = 141;
            ScriptVariables[LAMB_GREET] = 62;
            ScriptVariables[JOEY_SECTION] = 1;
            ScriptVariables[LAMB_SECTION] = 2;
            ScriptVariables[S15_FLOOR] = 8371;
            ScriptVariables[GUARDIAN_THERE] = 1;
            ScriptVariables[DOOR_67_68_FLAG] = 1;
            ScriptVariables[SC70_IRIS_FLAG] = 3;
            ScriptVariables[DOOR_73_75_FLAG] = 1;
            ScriptVariables[SC76_CABINET1_FLAG] = 1;
            ScriptVariables[SC76_CABINET2_FLAG] = 1;
            ScriptVariables[SC76_CABINET3_FLAG] = 1;
            ScriptVariables[DOOR_77_78_FLAG] = 1;
            ScriptVariables[SC80_EXIT_FLAG] = 1;
            ScriptVariables[SC31_LIFT_FLAG] = 1;
            ScriptVariables[SC32_LIFT_FLAG] = 1;
            ScriptVariables[SC33_SHED_DOOR_FLAG] = 1;
            ScriptVariables[BAND_PLAYING] = 1;
            ScriptVariables[COLSTON_AT_TABLE] = 1;
            ScriptVariables[SC36_NEXT_DEALER] = 16731;
            ScriptVariables[SC36_DOOR_FLAG] = 1;
            ScriptVariables[SC37_DOOR_FLAG] = 2;
            ScriptVariables[SC40_LOCKER_1_FLAG] = 1;
            ScriptVariables[SC40_LOCKER_2_FLAG] = 1;
            ScriptVariables[SC40_LOCKER_3_FLAG] = 1;
            ScriptVariables[SC40_LOCKER_4_FLAG] = 1;
            ScriptVariables[SC40_LOCKER_5_FLAG] = 1;

            if (SystemVars.Instance.GameVersion.Version.Minor == 288)
            {
                Array.Copy(forwardList1b288, 0, ScriptVariables, 352, forwardList1b288.Length);
            }
            else
            {
                Array.Copy(forwardList1b, 0, ScriptVariables, 352, forwardList1b.Length);
            }

            Array.Copy(forwardList2b, 0, ScriptVariables, 656, forwardList2b.Length);
            Array.Copy(forwardList3b, 0, ScriptVariables, 721, forwardList3b.Length);
            Array.Copy(forwardList4b, 0, ScriptVariables, 663, forwardList4b.Length);
            Array.Copy(forwardList5b, 0, ScriptVariables, 505, forwardList5b.Length);
        }

        public void InitScreen0()
        {
            FnEnterSection(0, 0, 0);
            _skyMusic.StartMusic(2);
            SystemVars.Instance.CurrentMusic = 2;
        }

        public void Engine()
        {
            do
            {
                var raw = _skyCompact.FetchCptRaw((ushort)ScriptVariables[LOGIC_LIST_NO]);
                var logicList = new UShortAccess(raw, 0);
                var i = 0;
                ushort id;
                while ((id = logicList[i++]) != 0)
                { // 0 means end of list
                    if (id == 0xffff)
                    {
                        // Change logic data address
                        raw = _skyCompact.FetchCptRaw(logicList[i]);
                        logicList = new UShortAccess(raw, 0);
                        continue;
                    }

                    ScriptVariables[CUR_ID] = id;
                    _compact = _skyCompact.FetchCpt(id);

                    // check the id actually wishes to be processed
                    if ((_compact.Core.status & (1 << 6)) == 0)
                        continue;

                    // ok, here we process the logic bit system

                    if ((_compact.Core.status & (1 << 7)) != 0)
                        _skyGrid.RemoveObjectFromWalk(_compact);

                    //TODO: Debug::logic(_compact.Core.logic);
                    _logicTable[_compact.Core.logic]();

                    if ((_compact.Core.status & (1 << 7)) != 0)
                        _skyGrid.ObjectToWalk(_compact);

                    // a sync sent to the compact is available for one cycle
                    // only. that cycle has just ended so remove the sync.
                    // presumably the mega has just reacted to it.
                    _compact.Core.sync = 0;
                }
                // usually this loop is run only once, it'll only be run a second time if the game
                // script just asked the user to enter a copy protection code.
                // this is done to prevent the copy protection screen from flashing up.
                // (otherwise it would be visible for 1/50 second)
            } while (CheckProtection());
        }

        private bool CheckProtection()
        {
            if (ScriptVariables[ENTER_DIGITS] != 0)
            {
                if (ScriptVariables[CONSOLE_TYPE] == 5) // reactor code
                    ScriptVariables[FS_COMMAND] = 240;
                else                                     // copy protection
                    ScriptVariables[FS_COMMAND] = 337;
                ScriptVariables[ENTER_DIGITS] = 0;
                return true;
            }
            else
                return false;
        }

        private bool FnEnterSection(uint sectionNo, uint b, uint c)
        {
            if (SkyEngine.IsDemo && (sectionNo > 2))
                Control.ShowGameQuitMsg();

            ScriptVariables[CUR_SECTION] = sectionNo;
            SystemVars.Instance.CurrentMusic = 0;

            if (sectionNo == 5) //linc section - has different mouse icons
                _skyMouse.ReplaceMouseCursors(60302);

            if ((sectionNo != _currentSection) || (SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.GAME_RESTORED)))
            {
                _currentSection = sectionNo;

                sectionNo++;
                _skyMusic.LoadSection((byte)sectionNo);
                _skySound.LoadSection((byte)sectionNo);
                _skyGrid.LoadGrids();
                SystemVars.Instance.SystemFlags &= ~SystemFlags.GAME_RESTORED;
            }

            return true;
        }

        private bool FnLeaveSection(uint sectionNo, uint b, uint c)
        {
            if (SkyEngine.IsDemo)
            {
                // TODO: Engine.QuitGame();
            }

            if (sectionNo == 5) //linc section - has different mouse icons
                _skyMouse.ReplaceMouseCursors(60301);

            return true;
        }

        private void SetupLogicTable()
        {
            _logicTable = new Action[] {
                Nop,
                LogicScript,	 // 1  script processor
		        AutoRoute,	 // 2  Make a route
		        ArAnim,	 // 3  Follow a route
		        ArTurn,	 // 4  Mega turns araound
		        Alt,		 // 5  Set up new get-to script
		        Anim,	 // 6  Follow a sequence
		        Turn,	 // 7  Mega turning
		        Cursor,	 // 8  id tracks the pointer
		        Talk,	 // 9  count down and animate
		        Listen,	 // 10 player waits for talking id
		        Stopped,	 // 11 wait for id to move
		        Choose,	 // 12 wait for player to click
		        Frames,	 // 13 animate just frames
		        Pause,	 // 14 Count down to 0 and go
		        WaitSync,	 // 15 Set to l_script when sync!=0
		        SimpleAnim,	 // 16 Module anim without x,y's
	        };
        }

        private void SimpleAnim()
        {
            /// follow an animation sequence module whilst ignoring the coordinate data

            var grafixProg = _skyCompact.GetGrafixPtr(_compact);

            // *grafix_prog: command
            while (grafixProg.Value != 0)
            {
                _compact.Core.grafixProgPos += 3;
                if (grafixProg.Value != SEND_SYNC)
                {
                    grafixProg.Offset += 2;
                    grafixProg.Offset += 2; // skip coordinates

                    // *grafix_prog: frame
                    if (grafixProg.Value >= 64)
                        _compact.Core.frame = grafixProg.Value;
                    else
                        _compact.Core.frame = (ushort)(grafixProg.Value + _compact.Core.offset);

                    return;
                }

                grafixProg.Offset += 2;
                // *grafix_prog: id to sync
                Compact compact2 = _skyCompact.FetchCpt(grafixProg.Value);
                grafixProg.Offset += 2;

                // *grafix_prog: sync
                compact2.Core.sync = grafixProg.Value;
                grafixProg.Offset += 2;
            }

            _compact.Core.downFlag = 0; // return 'ok' to script
            _compact.Core.logic = L_SCRIPT;
            LogicScript();
        }

        private void WaitSync()
        {
            /// checks c_sync, when its non 0
            /// the id is put back into script mode
            // use this instead of loops in the script

            if (_compact.Core.sync == 0)
                return;

            _compact.Core.logic = L_SCRIPT;
            LogicScript();
        }

        private void Pause()
        {
            if (--_compact.Core.flag != 0)
                return;

            _compact.Core.logic = L_SCRIPT;
            LogicScript();
            return;
        }

        private void Frames()
        {
            throw new NotImplementedException();
        }

        private void Choose()
        {
            throw new NotImplementedException();
        }

        private void Stopped()
        {
            throw new NotImplementedException();
        }

        private void Listen()
        {
            throw new NotImplementedException();
        }

        private void Talk()
        {
            throw new NotImplementedException();
        }

        private void Turn()
        {
            throw new NotImplementedException();
        }

        private void Anim()
        {
            /// Follow an animation sequence
            var grafixProg = _skyCompact.GetGrafixPtr(_compact);

            while (grafixProg.Value != 0)
            {
                _compact.Core.grafixProgPos += 3; // all types are 3 words.
                if (grafixProg.Value == LF_START_FX)
                { // do fx
                    grafixProg.Offset += 2;
                    ushort sound = grafixProg.Value; grafixProg.Offset += 2;
                    ushort volume = grafixProg.Value; grafixProg.Offset += 2;

                    // channel 0
                    FnStartFx(sound, 0, volume);
                }
                else if (grafixProg.Value >= LF_START_FX)
                { // do sync
                    grafixProg.Offset += 2;

                    Compact cpt = _skyCompact.FetchCpt(grafixProg.Value); grafixProg.Offset += 2;

                    cpt.Core.sync = grafixProg.Value; grafixProg.Offset += 2;
                }
                else
                { // put coordinates and frame in
                    _compact.Core.xcood = grafixProg.Value; grafixProg.Offset += 2;
                    _compact.Core.ycood = grafixProg.Value; grafixProg.Offset += 2;

                    _compact.Core.frame = (ushort)(grafixProg.Value | _compact.Core.offset); grafixProg.Offset += 2;
                    return;
                }
            }

            _compact.Core.downFlag = 0;
            _compact.Core.logic = L_SCRIPT;
            LogicScript();
        }

        private void Alt()
        {
            throw new NotImplementedException();
        }

        private void ArTurn()
        {
            throw new NotImplementedException();
        }

        private void ArAnim()
        {
            throw new NotImplementedException();
        }

        private void AutoRoute()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This function is basicly a wrapper around the real script engine. It runs
        /// the script engine until a script has finished.
        /// </summary>
        private void LogicScript()
        {
            /// Process the current mega's script
            /// If the script finishes then drop back a level

            for (;;)
            {
                ushort mode = _compact.Core.mode; // get pointer to current script
                var scriptNo = SkyCompact.GetSub(_compact, mode);
                var offset = SkyCompact.GetSub(_compact, (ushort)(mode + 2));

                offset.Field = Script((ushort)scriptNo.Field, (ushort)offset.Field);

                if ((ushort)offset.Field == 0) // script finished
                    _compact.Core.mode -= 4;
                else if (_compact.Core.mode == mode)
                    return;
            }
        }

        public ushort Script(ushort scriptNo, ushort offset)
        {
            do
            {
                bool restartScript = false;

                /// process a script
                /// low level interface to interpreter

                ushort moduleNo = (ushort)(scriptNo >> 12);
                var data = _moduleList[moduleNo]; // get module address

                if (data == null)
                { // We need to load the script module
                    _moduleList[moduleNo] = _skyDisk.LoadScriptFile((ushort)(moduleNo + F_MODULE_0));
                    data = _moduleList[moduleNo]; // module has been loaded
                }

                var scriptData = new UShortAccess(data, 0);

                // TODO: debug
                //debug(3, "Doing Script: %d:%d:%x", moduleNo, scriptNo & 0xFFF, offset ? (offset - moduleStart[scriptNo & 0xFFF]) : 0);

                // WORKAROUND for bug #3149412: "Invalid Mode when giving shades to travel agent"
                // Using the dark glasses on Trevor (travel agent) multiple times in succession would
                // wreck the trevor compact's mode, as the script in question doesn't account for using
                // this item at this point in the game (you will only have it here if you play the game
                // in an unusual way) and thus would loop indefinitely / never drop out.
                // To prevent this, we trigger the generic response by pretending we're using an item
                // which the script /does/ handle.
                if (scriptNo == TREVOR_SPEECH && _scriptVariables[OBJECT_HELD] == IDO_SHADES)
                    _scriptVariables[OBJECT_HELD] = IDO_GLASS;


                // Check whether we have an offset or what
                if (offset != 0)
                    scriptData = new UShortAccess(data, offset * 2);
                else
                    scriptData.Offset += (scriptData[scriptNo & 0x0FFF] * 2);

                uint a = 0, b = 0, c = 0;
                ushort command, s;

                while (!restartScript)
                {
                    command = scriptData.Value; scriptData.Offset += 2; // get a command
                    // TODO: debug
                    //Debug::script(command, scriptData);

                    switch (command)
                    {
                        case 0: // push_variable
                            Push(_scriptVariables[scriptData.Value / 4]); scriptData.Offset += 2;
                            break;
                        case 1: // less_than
                            a = Pop();
                            b = Pop();
                            if (a > b)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 2: // push_number
                            Push(scriptData.Value); scriptData.Offset += 2;
                            break;
                        case 3: // not_equal
                            a = Pop();
                            b = Pop();
                            if (a != b)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 4: // if_and
                            a = Pop();
                            b = Pop();
                            if (a != 0 && b != 0)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 5: // skip_zero
                            s = scriptData.Value; scriptData.Offset += 2;

                            a = Pop();
                            if (a == 0)
                                scriptData.Offset += s;
                            break;
                        case 6: // pop_var
                            b = _scriptVariables[scriptData.Value / 4] = Pop(); scriptData.Offset += 2;
                            break;
                        case 7: // minus
                            a = Pop();
                            b = Pop();
                            Push(b - a);
                            break;
                        case 8: // plus
                            a = Pop();
                            b = Pop();
                            Push(b + a);
                            break;
                        case 9: // skip_always
                            s = scriptData.Value; scriptData.Offset += 2;
                            scriptData.Offset += s;
                            break;
                        case 10: // if_or
                            a = Pop();
                            b = Pop();
                            if (a != 0 || b != 0)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 11: // call_mcode
                            {
                                a = scriptData.Value; scriptData.Offset += 2;
                                Debug.Assert(a <= 3);
                                switch (a)
                                {
                                    case 3:
                                        c = Pop();
                                        b = Pop();
                                        a = Pop();
                                        break;
                                    case 2:
                                        b = Pop();
                                        a = Pop();
                                        break;
                                    case 1:
                                        a = Pop();
                                        break;
                                }

                                ushort mcode = (ushort)(scriptData.Value / 4); scriptData.Offset += 2; // get mcode number 

                                // TODO: debug
                                //Debug::mcode(mcode, a, b, c);

                                var saveCpt = _compact;
                                bool ret = _mcodeTable[mcode](a, b, c);
                                _compact = saveCpt;

                                if (!ret)
                                    return (ushort)(scriptData.Offset / 2);
                            }
                            break;
                        case 12: // more_than
                            a = Pop();
                            b = Pop();
                            if (a < b)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 14: // switch
                            c = s = scriptData.Value; scriptData.Offset += 2; // get number of cases

                            a = Pop(); // and value to switch on

                            do
                            {
                                if (a == scriptData.Value)
                                {
                                    scriptData.Offset += scriptData[1];
                                    scriptData.Offset += 2;
                                    break;
                                }
                                scriptData.Offset += 2;
                            } while ((--s) != 0);

                            if (s == 0)
                                scriptData.Offset += scriptData.Value; // use the default
                            break;
                        case 15: // push_offset
                            {
                                var elem = _skyCompact.GetCompactElem(_compact, scriptData.Value);
                                Push(elem.Field); scriptData.Offset += 2;
                            }
                            break;
                        case 16: // pop_offset
                            {
                                // pop a value into a compact
                                var elem = _skyCompact.GetCompactElem(_compact, scriptData.Value); scriptData.Offset += 2;
                                elem.Field = (ushort)Pop();
                            }
                            break;
                        case 17: // is_equal
                            a = Pop();
                            b = Pop();
                            if (a == b)
                                Push(1);
                            else
                                Push(0);
                            break;
                        case 18:
                            { // skip_nz
                                short t = (short)scriptData.Value; scriptData.Offset += 2;
                                a = Pop();
                                if (a != 0)
                                    scriptData.Offset += t;
                                break;
                            }
                        case 13:
                        case 19: // script_exit
                            return 0;
                        case 20: // restart_script
                            offset = 0;
                            restartScript = true;
                            break;
                        default:
                            throw new InvalidOperationException(string.Format("Unknown script command: {0}", command));
                    }
                }
            } while (true);
        }

        private uint Pop()
        {
            if (_stackPtr < 1 || _stackPtr > _stack.Length - 1)
                throw new InvalidOperationException("No items on Stack to pop");
            return _stack[--_stackPtr];
        }

        private void Push(uint a)
        {
            if (_stackPtr > _stack.Length - 2)
                throw new InvalidOperationException("Stack overflow");
            _stack[_stackPtr++] = a;
        }

        private void Cursor()
        {
            _skyText.LogicCursor(_compact, _skyMouse.MouseX, _skyMouse.MouseY);
        }

        private void Nop()
        {
        }

        private bool FnCacheChip(uint a, uint b, uint c)
        {
            _skySound.FnStopFx();
            _skyDisk.FnCacheChip(_skyCompact.FetchCptRaw((ushort)a));
            return true;
        }

        private bool FnCacheFast(uint a, uint b, uint c)
        {
            _skyDisk.FnCacheFast(_skyCompact.FetchCptRaw((ushort)a));
            return true;
        }

        private bool FnDrawScreen(uint a, uint b, uint c)
        {
            //TODO:debug(5, "Call: fnDrawScreen(%X, %X)", a, b);
            SystemVars.Instance.CurrentPalette = a;
            _skyScreen.FnDrawScreen(a, b);

            if (_scriptVariables[SCREEN] == 32)
            {
                /* workaround for script bug #786482
                    Under certain circumstances, which never got completely cleared,
                    the gardener can get stuck in an animation, waiting for a sync
                    signal from foster.
                    This is most probably caused by foster leaving the screen before
                    sending the sync.
                    To work around that, we simply send a sync to the gardener every time
                    we enter the screen. If he isn't stuck (and thus not waiting for sync)
                    it will be ignored anyways */

                // TODO: debug(1, "sending gardener sync");
                FnSendSync(ID_SC32_GARDENER, 1, 0);
            }
            return true;
        }

        private bool FnAr(uint x, uint y, uint c)
        {
            _compact.Core.downFlag = 1; // assume failure in-case logic is interupted by speech (esp Joey)

            _compact.Core.arTargetX = (ushort)x;
            _compact.Core.arTargetY = (ushort)y;
            _compact.Core.logic = L_AR; // Set to AR mode

            _compact.Core.xcood &= 0xfff8;
            _compact.Core.ycood &= 0xfff8;

            return false; // drop out of script
        }

        private bool FnArAnimate(uint a, uint b, uint c)
        {
            _compact.Core.mood = 0; // high level 'not stood still'
            _compact.Core.logic = L_AR_ANIM;
            return false; // drop out of script
        }

        private bool FnIdle(uint a, uint b, uint c)
        {
            // set the player idling
            _compact.Core.logic = 0;
            return true;
        }

        private bool FnInteract(uint targetId, uint b, uint c)
        {
            _compact.Core.mode += 4; // next level up
            _compact.Core.logic = L_SCRIPT;
            var cpt = _skyCompact.FetchCpt((ushort)targetId);

            SkyCompact.GetSub(_compact, _compact.Core.mode).Field = cpt.Core.actionScript;
            SkyCompact.GetSub(_compact, (ushort)(_compact.Core.mode + 2)).Field = 0;

            return false;
        }

        private bool FnStartSub(uint scr, uint b, uint c)
        {
            _compact.Core.mode += 4;
            SkyCompact.GetSub(_compact, _compact.Core.mode).Field = (ushort)(scr & 0xffff);
            SkyCompact.GetSub(_compact, (ushort)(_compact.Core.mode + 2)).Field = (ushort)(scr >> 16);
            return false;
        }

        private bool FnTheyStartSub(uint mega, uint scr, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)mega);
            cpt.Core.mode += 4;
            SkyCompact.GetSub(cpt, cpt.Core.mode).Field = (ushort)(scr & 0xffff);
            SkyCompact.GetSub(cpt, (ushort)(cpt.Core.mode + 2)).Field = (ushort)(scr >> 16);
            return true;
        }

        private bool FnAssignBase(uint id, uint scr, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            cpt.Core.mode = C_BASE_MODE;
            cpt.Core.logic = L_SCRIPT;
            cpt.Core.baseSub = (ushort)(scr & 0xffff);
            cpt.Core.baseSub_off = (ushort)(scr >> 16);
            return true;
        }

        private bool FnDiskMouse(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_DISK, 11, 11);
            return true;
        }

        private bool FnNormalMouse(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_NORMAL, 0, 0);
            return true;
        }

        private bool FnBlankMouse(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_BLANK, 0, 0);
            return true;
        }

        private bool FnCrossMouse(uint a, uint b, uint c)
        {
            if (_scriptVariables[OBJECT_HELD] != 0)
                _skyMouse.FnOpenCloseHand(false);
            else
                _skyMouse.SpriteMouse(MOUSE_CROSS, 4, 4);
            return true;
        }

        private bool FnCursorRight(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_RIGHT, 9, 4);
            return true;
        }

        private bool FnCursorLeft(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_LEFT, 0, 5);
            return true;
        }

        private bool FnCursorDown(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_DOWN, 9, 4);
            return true;
        }

        private bool FnCursorUp(uint a, uint b, uint c)
        {
            _skyMouse.SpriteMouse(MOUSE_UP, 9, 4);
            return true;
        }

        private bool FnOpenHand(uint a, uint b, uint c)
        {
            _skyMouse.FnOpenCloseHand(true);
            return true;
        }

        private bool FnCloseHand(uint a, uint b, uint c)
        {
            _skyMouse.FnOpenCloseHand(false);
            return true;
        }

        private bool FnGetTo(uint targetPlaceId, uint mode, uint c)
        {
            _compact.Core.upFlag = (ushort)mode; // save mode for action script
            _compact.Core.mode += 4; // next level up
            Compact cpt = _skyCompact.FetchCpt(_compact.Core.place);
            if (cpt == null)
            {
                // TODO: warning("can't find _compact's getToTable. Place compact is NULL");
                return false;
            }
            var raw = _skyCompact.FetchCptRaw(cpt.Core.getToTableId);
            if (raw == null)
            {
                //TODO:  warning("Place compact's getToTable is NULL");
                return false;
            }

            var getToTable = new UShortAccess(raw, 0);
            while (getToTable.Value != targetPlaceId)
                getToTable.Offset += 4;

            // get new script
            SkyCompact.GetSub(_compact, _compact.Core.mode).Field = getToTable[1];
            SkyCompact.GetSub(_compact, (ushort)(_compact.Core.mode + 2)).Field = 0;

            return false; // drop out of script
        }

        private bool FnSetToStand(uint a, uint b, uint c)
        {
            _compact.Core.mood = 1; // high level stood still

            _compact.Core.grafixProgId = _skyCompact.GetCompactElem(_compact, (ushort)(C_STAND_UP + _compact.Core.megaSet + _compact.Core.dir * 4)).Field;
            _compact.Core.grafixProgPos = 0;

            UShortAccess standList = _skyCompact.GetGrafixPtr(_compact);

            _compact.Core.offset = standList.Value; // get frames offset
            _compact.Core.logic = L_SIMPLE_MOD;
            _compact.Core.grafixProgPos++;
            SimpleAnim();
            return false; // drop out of script
        }

        private bool FnTurnTo(uint dir, uint b, uint c)
        {
            /// turn compact to direction dir

            ushort curDir = _compact.Core.dir; // get current direction
            _compact.Core.dir = (ushort)(dir & 0xffff); // set new direction

            UShortAccess tt = _skyCompact.GetTurnTable(_compact, curDir);

            if (tt[(int)dir] == 0)
                return true; // keep going

            _compact.Core.turnProgId = tt[(int)dir]; // put turn program in
            _compact.Core.turnProgPos = 0;
            _compact.Core.logic = L_TURNING;

            Turn();

            return false; // drop out of script
        }

        private bool FnArrived(uint scriptVar, uint b, uint c)
        {
            _compact.Core.leaving = (ushort)(scriptVar & 0xffff);
            _scriptVariables[scriptVar / 4]++;
            return true;
        }

        private bool FnLeaving(uint a, uint b, uint c)
        {
            _compact.Core.atWatch = 0;

            if (_compact.Core.leaving != 0)
            {
                _scriptVariables[_compact.Core.leaving / 4]--;
                _compact.Core.leaving = 0; // I shall do this only once
            }

            return true; // keep going
        }

        private bool FnSetAlternate(uint scr, uint b, uint c)
        {
            _compact.Core.alt = (ushort)(scr & 0xffff);
            _compact.Core.logic = L_ALT;
            return false;
        }

        private bool FnAltSetAlternate(uint target, uint scr, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)target);
            cpt.Core.alt = (ushort)(scr & 0xffff);
            cpt.Core.logic = L_ALT;
            return false;
        }

        private bool FnKillId(uint id, uint b, uint c)
        {
            if (id != 0)
            {
                Compact cpt = _skyCompact.FetchCpt((ushort)id);
                if ((cpt.Core.status & (1 << 7)) != 0)
                    _skyGrid.RemoveObjectFromWalk(cpt);
                cpt.Core.status = 0;
            }
            return true;
        }

        private bool FnNoHuman(uint a, uint b, uint c)
        {
            if (_scriptVariables[MOUSE_STOP] == 0)
            {
                _scriptVariables[MOUSE_STATUS] &= 1;
                RunGetOff();
                FnBlankMouse(0, 0, 0);
            }
            return true;
        }

        private void RunGetOff()
        {
            throw new NotImplementedException();
        }

        private bool FnAddHuman(uint a, uint b, uint c)
        {
            return _skyMouse.FnAddHuman();
        }

        private bool FnAddButtons(uint a, uint b, uint c)
        {
            _scriptVariables[MOUSE_STATUS] |= 4;
            return true;
        }

        private bool FnNoButtons(uint a, uint b, uint c)
        {
            //remove the mouse buttons
            _scriptVariables[MOUSE_STATUS] &= 0xFFFFFFFB;
            return true;
        }

        private bool FnSetStop(uint a, uint b, uint c)
        {
            _scriptVariables[MOUSE_STOP] |= 1;
            return true;
        }

        private bool FnClearStop(uint a, uint b, uint c)
        {
            _scriptVariables[MOUSE_STOP] = 0;
            return true;
        }

        private bool FnPointerText(uint a, uint b, uint c)
        {
            _skyText.FnPointerText(a, _skyMouse.MouseX, _skyMouse.MouseY);
            return true;
        }

        private bool FnQuit(uint a, uint b, uint c)
        {
            return false;
        }

        private bool FnSpeakMe(uint targetId, uint mesgNum, uint animNum)
        {
            /* WORKAROUND for #2687172: When Mrs. Piermont is talking
               on the phone in her apartment, ignore her fnSpeakMe calls
               on other screens, as the lack of speech files for these lines
               will cause Foster's speech to be aborted if the timing is bad.
            */
            if (targetId == 0x4039 && animNum == 0x9B && _scriptVariables[SCREEN] != 38)
            {
                return false;
            }

            StdSpeak(_skyCompact.FetchCpt((ushort)targetId), mesgNum, animNum, 0);
            return false;   //drop out of script
        }

        private bool FnSpeakMeDir(uint targetId, uint mesgNum, uint animNum)
        {
            //must be player so don't cause script to drop out
            //this function sets the directional option whereby
            //the anim chosen is linked to c_dir
            animNum += (uint)(_compact.Core.dir << 1);  //2 sizes (large and small)
            return FnSpeakMe(targetId, mesgNum, animNum);
        }

        private bool FnSpeakWait(uint id, uint message, uint animation)
        {
            // non player mega char speaks
            // player will wait for it to finish before continuing script processing
            _compact.Core.flag = (ushort)(id & 0xffff);
            _compact.Core.logic = L_LISTEN;
            return FnSpeakMe(id, message, animation);
        }

        private bool FnSpeakWaitDir(uint a, uint b, uint c)
        {
            /* non player mega chr$ speaks	S2(20Jan93tw)
            the player will wait for it to finish
            before continuing script processing
            this function sets the directional option whereby
            the anim chosen is linked to c_dir -

            _compact is player
            a is ID to speak (not us)
            b is text message number
            c is base of mini table within anim_talk_table */

            //# ifdef __DC__
            //            __builtin_alloca(4); // Works around a gcc bug (wrong-code/11736)
            //#endif

            _compact.Core.flag = (ushort)a;
            _compact.Core.logic = L_LISTEN;

            Compact speaker = _skyCompact.FetchCpt((ushort)a);
            if (c != 0)
            {
                c += (uint)(speaker.Core.dir << 1);
                StdSpeak(speaker, b, c, speaker.Core.dir << 1);
            }
            else
                StdSpeak(speaker, b, c, 0);

            return false;
        }

        private bool FnChooser(uint a, uint b, uint c)
        {
            throw new NotImplementedException();

            // setup the text questions to be clicked on
            // read from TEXT1 until 0

            //SystemVars.Instance.SystemFlags |= SystemFlags.CHOOSING; // can't save/restore while choosing

            //_scriptVariables[THE_CHOSEN_ONE] = 0; // clear result

            //int p = TEXT1;
            //ushort ycood = TOP_LEFT_Y; // rolling coordinate

            //while (_scriptVariables[p] != 0)
            //{
            //    uint textNum = _scriptVariables[p++];

            //    DisplayedText lowText = _skyText.LowTextManager(textNum, GAME_SCREEN_WIDTH, 0, 241, 0);

            //    byte[] data = lowText.textData;

            //    // stipple the text

            //    uint size = ((DataFileHeader*)data).s_height * ((DataFileHeader*)data).s_width;
            //    uint index = 0;
            //    uint width = ((DataFileHeader*)data).s_width;
            //    uint height = ((DataFileHeader*)data).s_height;

            //    data += sizeof(DataFileHeader);

            //    while (index < size)
            //    {
            //        if (index % width <= 1)
            //            index ^= 1; //index++;
            //        if (!data[index])
            //            data[index] = 1;
            //        index += 2;
            //    }

            //    Compact textCompact = _skyCompact.FetchCpt(lowText.compactNum);

            //    textCompact.getToFlag = (ushort)textNum;
            //    textCompact.downFlag = (ushort)_scriptVariables[p++]; // get animation number

            //    textCompact.status |= ST_MOUSE; // mouse detects

            //    textCompact.xcood = TOP_LEFT_X; // set coordinates
            //    textCompact.ycood = ycood;
            //    ycood += (ushort)height;
            //}

            //if (p == TEXT1)
            //    return true;

            //_compact.Core.logic = L_CHOOSE; // player frozen until choice made
            //FnAddHuman(0, 0, 0); // bring back mouse

            //return false;
        }

        private bool FnHighlight(uint itemNo, uint pen, uint c)
        {
            pen -= 11;
            pen ^= 1;
            pen += 241;
            Compact textCompact = _skyCompact.FetchCpt((ushort)itemNo);
            var sprData = SkyEngine.ItemList[textCompact.Core.flag];
            _skyText.ChangeTextSpriteColor(sprData, (byte)pen);
            return true;
        }

        private bool FnTextKill(uint a, uint b, uint c)
        {
            /// Kill of text items that are mouse detectable

            uint id = FIRST_TEXT_COMPACT;

            for (int i = 10; i > 0; i--)
            {
                Compact cpt = _skyCompact.FetchCpt((ushort)id);
                if ((cpt.Core.status & (1 << 4)) != 0)
                    cpt.Core.status = 0;
                id++;
            }
            return true;
        }

        private bool FnStopMode(uint a, uint b, uint c)
        {
            _compact.Core.logic = L_STOPPED;
            return false;
        }

        private bool FnWeWait(uint id, uint b, uint c)
        {
            /// We have hit another mega
            /// we are going to wait for it to move

            _compact.Core.waitingFor = (ushort)id;
            StopAndWait();
            return true; // not sure about this
        }

        private bool FnSendSync(uint mega, uint sync, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)mega);
            cpt.Core.sync = (ushort)(sync & 0xffff);
            return false;
        }

        private bool FnSendFastSync(uint mega, uint sync, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)mega);
            cpt.Core.sync = (ushort)(sync & 0xffff);
            return true;
        }

        private bool FnSendRequest(uint target, uint scr, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)target);
            cpt.Core.request = (ushort)(scr & 0xffff);
            return false;
        }

        private bool FnClearRequest(uint target, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)target);
            cpt.Core.request = 0;
            return true;
        }

        private bool FnCheckRequest(uint a, uint b, uint c)
        {
            /// check for interaction request

            if (_compact.Core.request == 0)
                return true;

            _compact.Core.mode = C_ACTION_MODE; // into action mode

            _compact.Core.actionSub = _compact.Core.request;
            _compact.Core.actionSub_off = 0;

            _compact.Core.request = 0; // trash request
            return false; // drop from script
        }

        private bool FnStartMenu(uint firstObject, uint b, uint c)
        {
            /// initialize the top menu bar
            // firstObject is o0 for game menu, k0 for linc

            uint i;
            firstObject /= 4;

            // (1) FIRST, SET UP THE 2 ARROWS SO THEY APPEAR ON SCREEN

            Compact cpt = _skyCompact.FetchCpt(47);
            cpt.Core.status = ST_MOUSE + ST_FOREGROUND + ST_LOGIC + ST_RECREATE;
            cpt.Core.screen = (ushort)(_scriptVariables[SCREEN] & 0xffff);

            cpt = _skyCompact.FetchCpt(48);
            cpt.Core.status = ST_MOUSE + ST_FOREGROUND + ST_LOGIC + ST_RECREATE;
            cpt.Core.screen = (ushort)(_scriptVariables[SCREEN] & 0xffff);

            // (2) COPY OBJECTS FROM NON-ZERO INVENTORY VARIABLES INTO OBJECT DISPLAY LIST (& COUNT THEM)

            // sort the objects and pad with blanks

            uint menuLength = 0;
            for (i = firstObject; i < firstObject + _objectList.Length; i++)
            {
                if (_scriptVariables[i] != 0)
                    _objectList[menuLength++] = _scriptVariables[i];
            }
            _scriptVariables[MENU_LENGTH] = menuLength;

            // (3) OK, NOW TOP UP THE LIST WITH THE REQUIRED NO. OF BLANK OBJECTS (for min display length 11)

            uint blankID = 51;
            for (i = menuLength; i < 11; i++)
                _objectList[i] = blankID++;

            // (4) KILL ID's OF ALL 20 OBJECTS SO UNWANTED ICONS (SCROLLED OFF) DON'T REMAIN ON SCREEN
            // (There should be a better way of doing this - only kill id of 12th item when menu has scrolled right)

            for (i = 0; i < _objectList.Length; i++)
            {
                if (_objectList[i] != 0)
                    (_skyCompact.FetchCpt((ushort)_objectList[i])).Core.status = ST_LOGIC;
                else break;
            }

            // (5) NOW FIND OUT WHICH OBJECT TO START THE DISPLAY FROM (depending on scroll offset)

            if (menuLength < 11) // check we can scroll
                _scriptVariables[SCROLL_OFFSET] = 0;
            else if (menuLength < _scriptVariables[SCROLL_OFFSET] + 11)
                _scriptVariables[SCROLL_OFFSET] = menuLength - 11;

            // (6) AND FINALLY, INITIALIZE THE 11 OBJECTS SO THEY APPEAR ON SCREEEN

            ushort rollingX = TOP_LEFT_X + 28;
            for (i = 0; i < 11; i++)
            {
                cpt = _skyCompact.FetchCpt((ushort)(_objectList[_scriptVariables[SCROLL_OFFSET] + i]));

                cpt.Core.status = ST_MOUSE + ST_FOREGROUND + ST_LOGIC + ST_RECREATE;
                cpt.Core.screen = (ushort)(_scriptVariables[SCREEN] & 0xffff);

                cpt.Core.xcood = rollingX;
                rollingX += 24;

                if (_scriptVariables[MENU] == 2)
                    cpt.Core.ycood = 136;
                else
                    cpt.Core.ycood = 112;
            }

            return true;
        }

        private bool FnUnhighlight(uint item, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)item);
            cpt.Core.frame--;
            cpt.Core.getToFlag = 0;
            return true;
        }

        private bool FnFaceId(uint otherId, uint b, uint c)
        {
            /// return the direction to turn to face another id
            /// pass back result in c_just_flag

            Compact cpt = _skyCompact.FetchCpt((ushort)otherId);

            short x = (short)(_compact.Core.xcood - cpt.Core.xcood);

            if (x < 0)
            { // we're to the left
                x = (short)-x;
                _compact.Core.getToFlag = 3;
            }
            else
            { // it's to the left
                _compact.Core.getToFlag = 2;
            }

            // now check y

            // we must find the true bottom of the sprite
            // it is not enough to use y coord because changing
            // sprite offsets can ruin the formula - instead we
            // will use the bottom of the mouse collision area

            short y = (short)(_compact.Core.ycood - (cpt.Core.ycood + cpt.Core.mouseRelY + cpt.Core.mouseSizeY));

            if (y < 0)
            { // it's below
                y = (short)-y;
                if (y >= x)
                    _compact.Core.getToFlag = 1;
            }
            else
            { // it's above
                if (y >= x)
                    _compact.Core.getToFlag = 0;
            }
            return true;
        }

        private bool FnForeground(uint sprite, uint b, uint c)
        {
            /// Make sprite a foreground sprite
            Compact cpt = _skyCompact.FetchCpt((ushort)sprite);
            cpt.Core.status &= 0xfff8;
            cpt.Core.status |= ST_FOREGROUND;
            return true;
        }

        private bool FnBackground(uint a, uint b, uint c)
        {
            /// Make us a background sprite
            _compact.Core.status &= 0xfff8;
            _compact.Core.status |= ST_BACKGROUND;
            return true;
        }

        private bool FnNewBackground(uint sprite, uint b, uint c)
        {
            /// Make sprite a background sprite
            Compact cpt = _skyCompact.FetchCpt((ushort)sprite);
            cpt.Core.status &= 0xfff8;
            cpt.Core.status |= ST_BACKGROUND;
            return true;
        }

        private bool FnSort(uint mega, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)mega);
            cpt.Core.status &= 0xfff8;
            cpt.Core.status |= ST_SORT;
            return true;
        }

        private bool FnNoSpriteEngine(uint a, uint b, uint c)
        {
            /// stop the compact printing
            /// remove foreground, background & sort
            _compact.Core.status &= 0xfff8;
            return true;
        }

        private bool FnNoSpritesA6(uint us, uint b, uint c)
        {
            /// stop the compact printing
            /// remove foreground, background & sort
            Compact cpt = _skyCompact.FetchCpt((ushort)us);
            cpt.Core.status &= 0xfff8;
            return true;
        }

        private bool FnResetId(uint id, uint resetBlock, uint c)
        {
            throw new NotImplementedException();
            /// used when a mega is to be restarted
            /// eg - when a smaller mega turn to larger
            /// - a mega changes rooms...

            //Compact cpt = _skyCompact.FetchCpt((ushort)id);
            //Compact rst = _skyCompact.FetchCpt((ushort)resetBlock);

            //if (cpt == null)
            //{
            //    // TODO: warning("fnResetId(): Compact %d (id) == NULL", id);
            //    return true;
            //}
            //if (rst == null)
            //{
            //    // TODO: warning("fnResetId(): Compact %d (resetBlock) == NULL", resetBlock);
            //    return true;
            //}

            //ushort off;
            //while ((off = *rst++) != 0xffff)
            //    *(uint16*)_skyCompact.GetCompactElem(cpt, off) = *rst++;
            //return true;
        }

        private bool FnToggleGrid(uint a, uint b, uint c)
        {
            // Toggle a mega's grid plotting
            _compact.Core.status ^= ST_GRID_PLOT;
            return true;
        }

        private bool FnPause(uint cycles, uint b, uint c)
        {
            /// Set mega to L_PAUSE
            _compact.Core.flag = (ushort)(cycles & 0xffff);
            _compact.Core.logic = L_PAUSE;
            return false; // drop out of script
        }

        private bool FnRunAnimMod(uint animNo, uint b, uint c)
        {
            _compact.Core.grafixProgId = (ushort)animNo;
            _compact.Core.grafixProgPos = 0;

            _compact.Core.offset = _skyCompact.GetGrafixPtr(_compact).Value;
            _compact.Core.grafixProgPos++;
            _compact.Core.logic = L_MOD_ANIMATE;
            Anim();
            return false; // drop from script
        }

        private bool FnSimpleMod(uint animSeqNo, uint b, uint c)
        {
            _compact.Core.grafixProgId = (ushort)animSeqNo;
            _compact.Core.grafixProgPos = 0;

            _compact.Core.logic = L_SIMPLE_MOD;
            _compact.Core.offset = _skyCompact.GetGrafixPtr(_compact).Value;
            _compact.Core.grafixProgPos++;
            SimpleAnim();
            return false;
        }

        private bool FnRunFrames(uint sequenceNo, uint b, uint c)
        {
            _compact.Core.grafixProgId = (ushort)sequenceNo;
            _compact.Core.grafixProgPos = 0;

            _compact.Core.logic = L_FRAMES;
            _compact.Core.offset = _skyCompact.GetGrafixPtr(_compact).Value;
            _compact.Core.grafixProgPos++;
            SimpleAnim();
            return false;
        }

        private bool FnAwaitSync(uint a, uint b, uint c)
        {
            if (_compact.Core.sync != 0)
                return true;

            _compact.Core.logic = L_WAIT_SYNC;
            return false;
        }

        private bool FnIncMegaSet(uint a, uint b, uint c)
        {
            _compact.Core.megaSet += NEXT_MEGA_SET;
            return true;
        }

        private bool FnDecMegaSet(uint a, uint b, uint c)
        {
            _compact.Core.megaSet -= NEXT_MEGA_SET;
            return true;
        }

        private bool FnSetMegaSet(uint mega, uint setNo, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)mega);
            cpt.Core.megaSet = (ushort)(setNo * NEXT_MEGA_SET);
            return true;
        }

        private bool FnMoveItems(uint listNo, uint screenNo, uint c)
        {
            // Move a list of id's to another screen
            var p = new UShortAccess(_skyCompact.FetchCptRaw((ushort)CptIds.MoveList), 0);
            p = new UShortAccess(_skyCompact.FetchCptRaw(p[(int)listNo]), 0);
            for (int i = 0; i < 2; i++)
            {
                if (p.Value == 0)
                    return true;
                Compact cpt = _skyCompact.FetchCpt(p.Value); p.Offset += 2;
                cpt.Core.screen = (ushort)(screenNo & 0xffff);
            }
            return true;
        }

        private bool FnNewList(uint a, uint b, uint c)
        {
            /// Reset the chooser list
            for (int i = 0; i < 16; i++)
                _scriptVariables[TEXT1 + i] = 0;
            return true;
        }

        private bool FnAskThis(uint textNo, uint animNo, uint c)
        {
            // find first free position
            var p = TEXT1;
            while (_scriptVariables[p] != 0)
                p += 2;
            _scriptVariables[p++] = textNo;
            _scriptVariables[p] = animNo;
            return true;
        }

        private bool FnRandom(uint a, uint b, uint c)
        {
            _scriptVariables[RND] = (ushort)(_rnd.Next(65536) & a);
            return true;
        }

        private bool FnPersonHere(uint id, uint room, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            _scriptVariables[RESULT] = cpt.Core.screen == room ? 1U : 0;
            return true;
        }

        private bool FnToggleMouse(uint a, uint b, uint c)
        {
            _skyCompact.FetchCpt((ushort)a).Core.status ^= ST_MOUSE;
            return true;
        }

        private bool FnMouseOn(uint a, uint b, uint c)
        {
            //switch on the mouse highlight
            Compact cpt = _skyCompact.FetchCpt((ushort)a);
            cpt.Core.status |= ST_MOUSE;
            return true;
        }

        private bool FnMouseOff(uint a, uint b, uint c)
        {
            //switch off the mouse highlight
            Compact cpt = _skyCompact.FetchCpt((ushort)a);
            unchecked
            {
                cpt.Core.status &= (ushort)~ST_MOUSE;
            }
            return true;
        }

        private bool FnFetchX(uint id, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            _scriptVariables[RESULT] = cpt.Core.xcood;
            return true;
        }

        private bool FnFetchY(uint id, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            _scriptVariables[RESULT] = cpt.Core.ycood;
            return true;
        }

        private bool FnTestList(uint id, uint x, uint y)
        {
            _scriptVariables[RESULT] = 0; // assume fail
            var list = new UShortAccess(_skyCompact.FetchCptRaw((ushort)id), 0);

            while (list.Value != 0)
            {
                if ((x >= list[0]) && (x < list[1]) && (y >= list[2]) && (y < list[3]))
                    _scriptVariables[RESULT] = list[4];
                list.Offset += (5 * 2);
            }
            return true;
        }

        private bool FnFetchPlace(uint id, uint b, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            _scriptVariables[RESULT] = cpt.Core.place;
            return true;
        }

        private bool FnCustomJoey(uint id, uint b, uint c)
        {
            /// return id's x & y coordinate & c_mood (i.e. stood still yes/no)
            /// used by Joey-Logic - done in code like this because scripts can't
            /// get access to another megas compact as easily

            Compact cpt = _skyCompact.FetchCpt((ushort)id);

            _scriptVariables[PLAYER_X] = cpt.Core.xcood;
            _scriptVariables[PLAYER_Y] = cpt.Core.ycood;
            _scriptVariables[PLAYER_MOOD] = cpt.Core.mood;
            _scriptVariables[PLAYER_SCREEN] = cpt.Core.screen;
            return true;
        }

        private bool FnSetPalette(uint a, uint b, uint c)
        {
            _skyScreen.SetPaletteEndian(_skyCompact.FetchCptRaw((ushort)a));
            SystemVars.Instance.CurrentPalette = a;
            return true;
        }

        private bool FnTextModule(uint a, uint b, uint c)
        {
            _skyText.FnTextModule(a, b);
            return true;
        }

        private bool FnChangeName(uint id, uint textNo, uint c)
        {
            Compact cpt = _skyCompact.FetchCpt((ushort)id);
            cpt.Core.cursorText = (ushort)textNo;
            return true;
        }

        private bool FnMiniLoad(uint a, uint b, uint c)
        {
            _skyDisk.FnMiniLoad((ushort)a);
            return true;
        }

        private bool FnFlushBuffers(uint a, uint b, uint c)
        {
            _skyDisk.FnFlushBuffers();
            return true;
        }

        private bool FnFlushChip(uint a, uint b, uint c)
        {
            // this should be done automatically
            return true;
        }

        private bool FnSaveCoods(uint a, uint b, uint c)
        {
            _skyMouse.FnSaveCoods();
            return true;
        }

        private bool FnPlotGrid(uint x, uint y, uint width)
        {
            _skyGrid.PlotGrid(x, y, width, _compact);
            return true;
        }

        private bool FnRemoveGrid(uint x, uint y, uint width)
        {
            _skyGrid.RemoveGrid(x, y, width, _compact);
            return true;
        }

        private bool FnEyeball(uint id, uint b, uint c)
        {
            // set 'result' to frame no. pointing to foster, according to table used
            // eg. FN_eyeball (id_eye_90_table);

            var eyeTable = new UShortAccess(_skyCompact.FetchCptRaw((ushort)id), 0);
            Compact cpt = _skyCompact.FetchCpt(ID_BLUE_FOSTER);

            int x = cpt.Core.xcood; // 168 < x < 416
            x -= 168;
            x >>= 3;

            int y = cpt.Core.ycood; // 256 < y < 296
            y -= 256;
            y <<= 2;

            _scriptVariables[RESULT] = (uint)(eyeTable[x + y] + S91);
            return true;
        }

        private bool FnRestoreGame(uint a, uint b, uint c)
        {
            Control.DoLoadSavePanel();
            return false;
        }

        private bool FnRestartGame(uint a, uint b, uint c)
        {
            Control.RestartGame();
            return false;
        }

        private bool FnNewSwingSeq(uint a, uint b, uint c)
        {
            // only certain files work on pc. (huh?! something we should take care of?)
            if ((a == 85) || (a == 106) || (a == 75) || (a == 15))
            {
                _skyScreen.StartSequenceItem((ushort)a);
            }
            else
            {
                // TODO: Debug(1, "fnNewSwingSeq: ignored seq %d", a);
            }
            return true;
        }

        private bool FnWaitSwingEnd(uint a, uint b, uint c)
        {
            _skyScreen.WaitForSequence();
            return true;
        }

        private bool FnSkipIntroCode(uint a, uint b, uint c)
        {
            SystemVars.Instance.PastIntro = true;
            return true;
        }

        private bool FnBlankScreen(uint a, uint b, uint c)
        {
            _skyScreen.ClearScreen();
            return true;
        }

        private bool FnPrintCredit(uint a, uint b, uint c)
        {
            DisplayedText creditText = _skyText.LowTextManager(a, 240, 0, 248, true);
            Compact credCompact = _skyCompact.FetchCpt(creditText.compactNum);
            credCompact.Core.xcood = 168;
            if ((a == 558) && (c == 215))
                credCompact.Core.ycood = 211;
            else
                credCompact.Core.ycood = (ushort)c;
            _scriptVariables[RESULT] = creditText.compactNum;
            return true;
        }

        private bool FnLookAt(uint a, uint b, uint c)
        {
            DisplayedText textInfo = _skyText.LowTextManager(a, 240, 0, 248, true);
            Compact textCpt = _skyCompact.FetchCpt(textInfo.compactNum);
            textCpt.Core.xcood = 168;
            textCpt.Core.ycood = (ushort)c;

            _skyScreen.Recreate();
            _skyScreen.SpriteEngine();
            _skyScreen.Flip();

            FnNoHuman(0, 0, 0);
            _skyMouse.LockMouse();

            _skyMouse.WaitMouseNotPressed(800);

            _skyMouse.UnlockMouse();
            FnAddHuman(0, 0, 0);
            textCpt.Core.status = 0;

            return true;
        }

        private bool FnLincTextModule(uint textPos, uint textNo, uint buttonAction)
        {
            ushort cnt;
            if ((buttonAction & 0x8000) != 0)
                for (cnt = LINC_DIGIT_0; cnt <= LINC_DIGIT_9; cnt++)
                    _scriptVariables[cnt] = 0;
            buttonAction &= 0x7FFF;
            if (buttonAction < 10)
                _scriptVariables[LINC_DIGIT_0 + buttonAction] = textNo;

            DisplayedText text = _skyText.LowTextManager(textNo, 220, 0, 215, false);

            Compact textCpt = _skyCompact.FetchCpt(text.compactNum);

            if (textPos < 20)
            { // line number (for text)
                textCpt.Core.xcood = 152;
                textCpt.Core.ycood = (ushort)(textPos * 13 + 170);
            }
            else if (textPos > 20)
            { // x coordinate (for numbers)
                textCpt.Core.xcood = (ushort)textPos;
                textCpt.Core.ycood = 214;
            }
            else
            {
                //TODO:  warning("::fnLincTextModule: textPos == 20");
            }
            textCpt.Core.getToFlag = (ushort)textNo;
            return true;
        }

        private bool FnTextKill2(uint a, uint b, uint c)
        {
            /// Kill all text items

            uint id = FIRST_TEXT_COMPACT;

            for (int i = 10; i > 0; i--)
            {
                Compact cpt = _skyCompact.FetchCpt((ushort)id);
                cpt.Core.status = 0;
                id++;
            }
            return true;
        }

        private bool FnSetFont(uint font, uint b, uint c)
        {
            _skyText.FnSetFont(font);
            return true;
        }

        private bool FnStartFx(uint sound, uint b, uint c)
        {
            _skySound.FnStartFx(sound, (byte)(b & 1));
            return true;
        }

        private bool FnStopFx(uint a, uint b, uint c)
        {
            _skySound.FnStopFx();
            return true;
        }

        private bool FnStartMusic(uint a, uint b, uint c)
        {
            if (!(SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.MUS_OFF)))
                _skyMusic.StartMusic((ushort)a);
            SystemVars.Instance.CurrentMusic = (ushort)a;
            return true;
        }

        private bool FnStopMusic(uint a, uint b, uint c)
        {
            _skyMusic.StartMusic(0);
            SystemVars.Instance.CurrentMusic = 0;
            return true;
        }

        private bool FnFadeDown(uint a, uint b, uint c)
        {
            _skyScreen.FnFadeDown(a);
            return true;
        }

        private bool FnFadeUp(uint a, uint b, uint c)
        {
            SystemVars.Instance.CurrentPalette = a;
            _skyScreen.FnFadeUp(a, b);
            return true;
        }

        private bool FnQuitToDos(uint a, uint b, uint c)
        {
            QuitGame();
            return false;
        }

        private bool FnPauseFx(uint a, uint b, uint c)
        {
            _skySound.FnPauseFx();
            return true;
        }

        private bool FnUnPauseFx(uint a, uint b, uint c)
        {
            _skySound.FnUnPauseFx();
            return true;
        }

        private bool FnPrintf(uint a, uint b, uint c)
        {
            // TODO: debug("fnPrintf(%d, %d, %d)", a, b, c);
            return true;
        }


        private void StdSpeak(Compact compact, uint mesgNum, uint animNum, int v)
        {
            throw new NotImplementedException();
        }

        private void QuitGame()
        {
            throw new NotImplementedException();
        }

        private void StopAndWait()
        {
            throw new NotImplementedException();
        }

        static readonly uint[] forwardList1b = {
            JOBS_SPEECH,
            JOBS_S4,
            JOBS_ALARMED,
            JOEY_RECYCLE,
            SHOUT_SSS,
            JOEY_MISSION,
            TRANS_MISSION,
            SLOT_MISSION,
            CORNER_MISSION,
            JOEY_LOGIC,
            GORDON_SPEECH,
            JOEY_BUTTON_MISSION,
            LOB_DAD_SPEECH,
            LOB_SON_SPEECH,
            GUARD_SPEECH,
            MANTRACH_SPEECH,
            WRECK_SPEECH,
            ANITA_SPEECH,
            LAMB_FACTORY,
            FORE_SPEECH,
            JOEY_42_MISS,
            JOEY_JUNCTION_MISS,
            WELDER_MISSION,
            JOEY_WELD_MISSION,
            RADMAN_SPEECH,
            LINK_7_29,
            LINK_29_7,
            LAMB_TO_3,
            LAMB_TO_2,
            BURKE_SPEECH,
            BURKE_1,
            BURKE_2,
            DR_BURKE_1,
            JASON_SPEECH,
            JOEY_BELLEVUE,
            ANCHOR_SPEECH,
            ANCHOR_MISSION,
            JOEY_PC_MISSION,
            HOOK_MISSION,
            TREVOR_SPEECH,
            JOEY_FACTORY,
            HELGA_SPEECH,
            JOEY_HELGA_MISSION,
            GALL_BELLEVUE,
            GLASS_MISSION,
            LAMB_FACT_RETURN,
            LAMB_LEAVE_GARDEN,
            LAMB_START_29,
            LAMB_BELLEVUE,
            CABLE_MISSION,
            FOSTER_TOUR,
            LAMB_TOUR,
            FOREMAN_LOGIC,
            LAMB_LEAVE_FACTORY,
            LAMB_BELL_LOGIC,
            LAMB_FACT_2,
            START90,
            0,
            0,
            LINK_28_31,
            LINK_31_28,
            EXIT_LINC,
            DEATH_SCRIPT
        };

        public const int RESULT = 0;
        public const int SCREEN = 1;
        public const int LOGIC_LIST_NO = 2;
        public const int MOUSE_LIST_NO = 6;
        public const int DRAW_LIST_NO = 8;
        public const int CUR_ID = 12;
        public const int MOUSE_STATUS = 13;
        public const int MOUSE_STOP = 14;
        public const int BUTTON = 15;
        public const int SPECIAL_ITEM = 17;
        public const int GET_OFF = 18;
        public const int CURSOR_ID = 22;
        public const int SAFEX = 25;
        public const int SAFEY = 26;
        public const int PLAYER_X = 27;
        public const int PLAYER_Y = 28;
        public const int PLAYER_MOOD = 29;
        public const int PLAYER_SCREEN = 30;
        public const int HIT_ID = 37;
        public const int LAYER_0_ID = 41;
        public const int LAYER_1_ID = 42;
        public const int LAYER_2_ID = 43;
        public const int LAYER_3_ID = 44;
        public const int GRID_1_ID = 45;
        public const int GRID_2_ID = 46;
        public const int GRID_3_ID = 47;
        public const int THE_CHOSEN_ONE = 51;
        public const int TEXT1 = 53;
        public const int MENU_LENGTH = 100;
        public const int SCROLL_OFFSET = 101;
        public const int MENU = 102;
        public const int OBJECT_HELD = 103;
        public const int LAMB_GREET = 109;
        public const int RND = 115;
        public const int CUR_SECTION = 143;
        public const int JOEY_SECTION = 145;
        public const int LAMB_SECTION = 146;
        public const int KNOWS_PORT = 190;
        public const int GOT_SPONSOR = 240;
        public const int GOT_JAMMER = 258;
        public const int CONSOLE_TYPE = 345;
        public const int S15_FLOOR = 450;
        public const int FOREMAN_FRIEND = 451;
        public const int REICH_DOOR_FLAG = 470;
        public const int CARD_STATUS = 479;
        public const int CARD_FIX = 480;
        public const int GUARDIAN_THERE = 640;
        public const int FS_COMMAND = 643;
        public const int ENTER_DIGITS = 644;
        public const int LINC_DIGIT_0 = 646;
        public const int LINC_DIGIT_1 = 647;
        public const int LINC_DIGIT_2 = 648;
        public const int LINC_DIGIT_3 = 649;
        public const int LINC_DIGIT_4 = 650;
        public const int LINC_DIGIT_5 = 651;
        public const int LINC_DIGIT_6 = 651;
        public const int LINC_DIGIT_7 = 653;
        public const int LINC_DIGIT_8 = 654;
        public const int LINC_DIGIT_9 = 655;
        public const int DOOR_67_68_FLAG = 678;
        public const int SC70_IRIS_FLAG = 693;
        public const int DOOR_73_75_FLAG = 704;
        public const int SC76_CABINET1_FLAG = 709;
        public const int SC76_CABINET2_FLAG = 710;
        public const int SC76_CABINET3_FLAG = 711;
        public const int DOOR_77_78_FLAG = 719;
        public const int SC80_EXIT_FLAG = 720;
        public const int SC31_LIFT_FLAG = 793;
        public const int SC32_LIFT_FLAG = 797;
        public const int SC33_SHED_DOOR_FLAG = 798;
        public const int BAND_PLAYING = 804;
        public const int COLSTON_AT_TABLE = 805;
        public const int SC36_NEXT_DEALER = 806;
        public const int SC36_DOOR_FLAG = 807;
        public const int SC37_DOOR_FLAG = 808;
        public const int SC40_LOCKER_1_FLAG = 817;
        public const int SC40_LOCKER_2_FLAG = 818;
        public const int SC40_LOCKER_3_FLAG = 819;
        public const int SC40_LOCKER_4_FLAG = 820;
        public const int SC40_LOCKER_5_FLAG = 821;

        static readonly uint[] forwardList1b288 = {
            JOBS_SPEECH,
            JOBS_S4,
            JOBS_ALARMED,
            JOEY_RECYCLE,
            SHOUT_SSS,
            JOEY_MISSION,
            TRANS_MISSION,
            SLOT_MISSION,
            CORNER_MISSION,
            JOEY_LOGIC,
            GORDON_SPEECH,
            JOEY_BUTTON_MISSION,
            LOB_DAD_SPEECH,
            LOB_SON_SPEECH,
            GUARD_SPEECH,
            0x68,
            WRECK_SPEECH,
            ANITA_SPEECH,
            LAMB_FACTORY,
            FORE_SPEECH,
            JOEY_42_MISS,
            JOEY_JUNCTION_MISS,
            WELDER_MISSION,
            JOEY_WELD_MISSION,
            RADMAN_SPEECH,
            LINK_7_29,
            LINK_29_7,
            LAMB_TO_3,
            LAMB_TO_2,
            0x3147,
            0x3100,
            0x3101,
            0x3102,
            0x3148,
            0x3149,
            0x314A,
            0x30C5,
            0x30C6,
            0x30CB,
            0x314B,
            JOEY_FACTORY,
            0x314C,
            0x30E2,
            0x314D,
            0x310C,
            LAMB_FACT_RETURN,
            0x3139,
            0x313A,
            0x004F,
            CABLE_MISSION,
            FOSTER_TOUR,
            LAMB_TOUR,
            FOREMAN_LOGIC,
            LAMB_LEAVE_FACTORY,
            0x3138,
            LAMB_FACT_2,
            0x004D,
            0,
            0,
            LINK_28_31,
            LINK_31_28,
            0x004E,
            DEATH_SCRIPT
        };

        static readonly uint[] forwardList2b = {
            STD_ON,
            STD_EXIT_LEFT_ON,
            STD_EXIT_RIGHT_ON,
            ADVISOR_188,
            SHOUT_ACTION,
            MEGA_CLICK,
            MEGA_ACTION
        };

        static readonly uint[] forwardList3b = {
            DANI_SPEECH,
            DANIELLE_GO_HOME,
            SPUNKY_GO_HOME,
            HENRI_SPEECH,
            BUZZER_SPEECH,
            FOSTER_VISIT_DANI,
            DANIELLE_LOGIC,
            JUKEBOX_SPEECH,
            VINCENT_SPEECH,
            EDDIE_SPEECH,
            BLUNT_SPEECH,
            DANI_ANSWER_PHONE,
            SPUNKY_SEE_VIDEO,
            SPUNKY_BARK_AT_FOSTER,
            SPUNKY_SMELLS_FOOD,
            BARRY_SPEECH,
            COLSTON_SPEECH,
            GALL_SPEECH,
            BABS_SPEECH,
            CHUTNEY_SPEECH,
            FOSTER_ENTER_COURT
        };

        static readonly uint[] forwardList4b = {
            WALTER_SPEECH,
            JOEY_MEDIC,
            JOEY_MED_LOGIC,
            JOEY_MED_MISSION72,
            KEN_LOGIC,
            KEN_SPEECH,
            KEN_MISSION_HAND,
            SC70_IRIS_OPENED,
            SC70_IRIS_CLOSED,
            FOSTER_ENTER_BOARDROOM,
            BORED_ROOM,
            FOSTER_ENTER_NEW_BOARDROOM,
            HOBS_END,
            SC82_JOBS_SSS
        };

        static readonly uint[] forwardList5b = {
            SET_UP_INFO_WINDOW,
            SLAB_ON,
            UP_MOUSE,
            DOWN_MOUSE,
            LEFT_MOUSE,
            RIGHT_MOUSE,
            DISCONNECT_FOSTER
        };
    }
}