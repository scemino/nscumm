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

using System;
using NScumm.Core;
using NScumm.Core.Engines;
using NScumm.Core.IO;
using NScumm.Sci.Sound;
using NScumm.Sci.Engine;
using NScumm.Sci.Graphics;
using NScumm.Sci.Parser;

namespace NScumm.Sci
{
    /// <summary>
    /// Supported languages
    /// </summary>
    internal enum Language
    {
        NONE = 0,
        ENGLISH = 1,
        FRENCH = 33,
        SPANISH = 34,
        ITALIAN = 39,
        GERMAN = 49,
        JAPANESE = 81,
        PORTUGUESE = 351
    }

    internal static class ADGameFlags
    {
        public const int NO_FLAGS = 0;
        /// <summary>
        /// flag to designate not yet officially-supported games that are not fit for public testin
        /// </summary>
        public const int UNSTABLE = (1 << 21);
        /// <summary>
        /// flag to designate not yet officially-supported games that are fit for public testing
        /// </summary>
        public const int TESTING = (1 << 22);
        /// <summary>
        /// flag to designate well known pirated versions with cracks
        /// </summary>
        public const int PIRATED = (1 << 23);
        /// <summary>
        /// always add English as language option
        /// </summary>
        public const int ADDENGLISH = (1 << 24);
        /// <summary>
        /// the md5 for this entry will be calculated from the resource fork
        /// </summary>
        public const int MACRESFORK = (1 << 25);
        /// <summary>
        /// Extra field value will be used as main game title, not gameid
        /// </summary>
        public const int USEEXTRAASTITLE = (1 << 26);
        /// <summary>
        /// don't add language to gameid
        /// </summary>
        public const int DROPLANGUAGE = (1 << 27);
        /// <summary>
        /// don't add platform to gameid
        /// </summary>
        public const int DROPPLATFORM = (1 << 28);
        /// <summary>
        /// add "-cd" to gameid
        /// </summary>
        public const int CD = (1 << 29);
        /// <summary>
        /// add "-demo" to gameid
        /// </summary>
        public const int DEMO = (1 << 30);
    }

    internal class SciEngine : IEngine
    {
        private static SciEngine _instance;

        private ResourceManager _resMan;
        private string _directory;
        private ADGameDescription _gameDescription;
        private SciGameId _gameId;
        private SoundCommandParser _soundCmd;
        private Register _gameObjectAddress;
        private ScriptPatcher _scriptPatcher;
        private Kernel _kernel;
        private GameFeatures _features;
        private Vocabulary _vocabulary;
        private EngineState _gamestate;
        private EventManager _eventMan;
        public opcode_format[][] _opcode_formats;

        public GfxAnimate _gfxAnimate; // Animate for 16-bit gfx
        public GfxCache _gfxCache;
        public GfxCompare _gfxCompare;
        public GfxControls16 _gfxControls16; // Controls for 16-bit gfx
        public GfxControls32 _gfxControls32; // Controls for 32-bit gfx
        public GfxCoordAdjuster _gfxCoordAdjuster;
        public GfxCursor _gfxCursor;
        public GfxMenu _gfxMenu; // Menu for 16-bit gfx
        public GfxPalette _gfxPalette;
        public GfxPaint _gfxPaint;
        public GfxPaint16 _gfxPaint16; // Painting in 16-bit gfx
        public GfxPaint32 _gfxPaint32; // Painting in 32-bit gfx
        public GfxPorts _gfxPorts; // Port managment for 16-bit gfx
        public GfxScreen _gfxScreen;
        public GfxText16 _gfxText16;
        public GfxText32 _gfxText32;
        public GfxTransitions _gfxTransitions; // transitions between screens for 16-bit gfx
        public GfxMacIconBar _gfxMacIconBar; // Mac Icon Bar manager

        public DebugState _debugState;

        public SciEngine(ISystem system, SciGameDescriptor desc, SciGameId id)
        {
            _system = system;
            _instance = this;
            _gameId = id;
            _gameDescription = desc.GameDescription;
            _directory = ServiceLocator.FileStorage.GetDirectoryName(desc.Path);
            _debugState = new DebugState();
        }

        public string GetSciLanguageString(string str, Language requestedLanguage, Language? secondaryLanguage = null, ushort? languageSplitter = null)
        {
            throw new NotImplementedException();
            //Language foundLanguage = Sci.Language.NONE;

            //for (int i = 0; i < str.Length; i++)
            //{
            //    var curChar = str[i];

            //    if ((curChar == '%') || (curChar == '#'))
            //    {
            //        var curChar2 = str[i + 1];
            //        foundLanguage = CharToLanguage(curChar2);

            //        if (foundLanguage != Sci.Language.NONE)
            //        {
            //            // Return language splitter
            //            if (languageSplitter)
            //                *languageSplitter = curChar | (curChar2 << 8);
            //            // Return the secondary language found in the string
            //            if (secondaryLanguage)
            //                *secondaryLanguage = foundLanguage;
            //            break;
            //        }
            //    }
            //}

            //if (foundLanguage == requestedLanguage)
            //{
            //    if (curChar2 == 'J')
            //    {
            //        // Japanese including Kanji, displayed with system font
            //        // Convert half-width characters to full-width equivalents
            //        Common::String fullWidth;
            //        uint16 mappedChar;

            //        textPtr += 2; // skip over language splitter

            //        while (1)
            //        {
            //            curChar = *textPtr;

            //            switch (curChar)
            //            {
            //                case 0: // Terminator NUL
            //                    return fullWidth;
            //                case '\\':
            //                    // "\n", "\N", "\r" and "\R" were overwritten with SPACE + 0x0D in PC-9801 SSCI
            //                    //  inside GetLongest() (text16). We do it here, because it's much cleaner and
            //                    //  we have to process the text here anyway.
            //                    //  Occurs for example in Police Quest 2 intro
            //                    curChar2 = *(textPtr + 1);
            //                    switch (curChar2)
            //                    {
            //                        case 'n':
            //                        case 'N':
            //                        case 'r':
            //                        case 'R':
            //                            fullWidth += ' ';
            //                            fullWidth += 0x0D; // CR
            //                            textPtr += 2;
            //                            continue;
            //                    }
            //            }

            //            textPtr++;

            //            mappedChar = s_halfWidthSJISMap[curChar];
            //            if (mappedChar)
            //            {
            //                fullWidth += mappedChar >> 8;
            //                fullWidth += mappedChar & 0xFF;
            //            }
            //            else {
            //                // Copy double-byte character
            //                curChar2 = *(textPtr++);
            //                if (!curChar)
            //                {
            //                    error("SJIS character %02X is missing second byte", curChar);
            //                    break;
            //                }
            //                fullWidth += curChar;
            //                fullWidth += curChar2;
            //            }
            //        }

            //    }
            //    else {
            //        return Common::String((const char*)(textPtr + 2));
            //    }
            //}

            //if (curChar)
            //    return Common::String(str.c_str(), (const char*)textPtr - str.c_str());

            //return str;
        }

        public static SciEngine Instance
        {
            get { return _instance; }
        }

        public bool HasToQuit
        {
            get;
            set;
        }

        public bool IsPaused
        {
            get;
            set;
        }

        public Core.Common.Language Language
        {
            get { return _gameDescription.language; }
        }

        public Platform Platform { get { return _gameDescription.platform; } }

        public SciGameId GameId { get { return _gameId; } }

        public EngineState EngineState { get { return _gamestate; } }

        public Register GameObject { get { return _gameObjectAddress; } }

        public bool HasMacIconBar
        {
            get
            {
                return _resMan.IsSci11Mac && ResourceManager.GetSciVersion() == SciVersion.V1_1 &&
                        (GameId == SciGameId.KQ6 || GameId == SciGameId.FREDDYPHARKAS);
            }
        }

        public bool IsDemo
        {
            get
            {
                return (_gameDescription.flags & ADGameFlags.DEMO) != 0;
            }
        }

        public bool IsCD
        {
            get
            {
                return (_gameDescription.flags & ADGameFlags.CD) != 0;
            }
        }

        public ResourceManager ResMan { get { return _resMan; } }

        public Kernel Kernel { get { return _kernel; } }

        public void Run()
        {
            _resMan = new ResourceManager(_directory);
            _resMan.AddAppropriateSources();
            _resMan.Init();

            // TODO: Add error handling. Check return values of addAppropriateSources
            // and init. We first have to *add* sensible return values, though ;).
            /*
                if (!_resMan) {
                    warning("No resources found, aborting");
                    return Common::kNoGameDataFoundError;
                }
            */

            // Reset, so that error()s before SoundCommandParser is initialized wont cause a crash
            _soundCmd = null;

            // Add the after market GM patches for the specified game, if they exist
            _resMan.AddNewGMPatch(_gameId);
            _gameObjectAddress = _resMan.FindGameObject();

            _scriptPatcher = new ScriptPatcher();
            SegManager segMan = new SegManager(_resMan, _scriptPatcher);

            // Initialize the game screen
            _gfxScreen = new GfxScreen(_system, _resMan);
            // TODO: _gfxScreen.EnableUndithering(ConfMan.getBool("disable_dithering"));

            _kernel = new Kernel(_resMan, segMan);
            _kernel.Init();

            _features = new GameFeatures(segMan, _kernel);

            // Only SCI0, SCI01 and SCI1 EGA games used a parser
            _vocabulary = (ResourceManager.GetSciVersion() <= SciVersion.V1_EGA_ONLY) ? new Vocabulary(_resMan, false) : null;
            // Also, XMAS1990 apparently had a parser too. Refer to http://forums.scummvm.org/viewtopic.php?t=9135
            if (GameId == SciGameId.CHRISTMAS1990)
                _vocabulary = new Vocabulary(_resMan, false);
            _audio = new AudioPlayer(_resMan);
            _gamestate = new EngineState(segMan);
            _eventMan = new EventManager(_resMan.DetectFontExtended());

            // TODO: Create debugger console. It requires GFX and _gamestate to be initialized
            //_console = new Console(this);

            // The game needs to be initialized before the graphics system is initialized, as
            // the graphics code checks parts of the seg manager upon initialization (e.g. for
            // the presence of the fastCast object)
            if (!InitGame())
            { /* Initialize */
                // TODO: warning("Game initialization failed: Aborting...");
                // TODO: Add an "init failed" error?
                //return Common::kUnknownError;
                return;
            }

            // we try to find the super class address of the game object, we can't do that earlier
            SciObject gameObject = segMan.GetObject(_gameObjectAddress);
            if (gameObject == null)
            {
                // TODO: warning("Could not get game object, aborting...");
                return;
            }

            script_adjust_opcode_formats();

            // Must be called after game_init(), as they use _features
            _kernel.LoadKernelNames(_features);
            // TODO: _soundCmd = new SoundCommandParser(_resMan, segMan, _kernel, _audio, _features.detectDoSoundType());

            // TODO: SyncSoundSettings();
            // TODO: SyncIngameAudioOptions();

            // Load our Mac executable here for icon bar palettes and high-res fonts
            // TODO: LoadMacExecutable();

            // Initialize all graphics related subsystems
            InitGraphics();

            // Patch in our save/restore code, so that dialogs are replaced
            // TODO: PatchGameSaveRestore();
            SetLauncherLanguage();

            // TODO: Check whether loading a savestate was requested
            //int directSaveSlotLoading = ConfMan.getInt("save_slot");
            //if (directSaveSlotLoading >= 0)
            //{
            //    // call GameObject::play (like normally)
            //    initStackBaseWithSelector(SELECTOR(play));
            //    // We set this, so that the game automatically quit right after init
            //    _gamestate.variables[VAR_GLOBAL][4] = TRUE_REG;

            //    // Jones only initializes its menus when restarting/restoring, thus set
            //    // the gameIsRestarting flag here before initializing. Fixes bug #6536.
            //    if (g_sci.getGameId() == GID_JONES)
            //        _gamestate.gameIsRestarting = GAMEISRESTARTING_RESTORE;

            //    _gamestate._executionStackPosChanged = false;
            //    run_vm(_gamestate);

            //    // As soon as we get control again, actually restore the game
            //    reg_t restoreArgv[2] = { NULL_REG, make_reg(0, directSaveSlotLoading) };    // special call (argv[0] is NULL)
            //    kRestoreGame(_gamestate, 2, restoreArgv);

            //    // this indirectly calls GameObject::init, which will setup menu, text font/color codes etc.
            //    //  without this games would be pretty badly broken
            //}

            // Show any special warnings for buggy scripts with severe game bugs,
            // which have been patched by Sierra
            if (GameId == SciGameId.LONGBOW)
            {
                // Longbow 1.0 has a buggy script which prevents the game
                // from progressing during the Green Man riddle sequence.
                // A patch for this buggy script has been released by Sierra,
                // and is necessary to complete the game without issues.
                // The patched script is included in Longbow 1.1.
                // Refer to bug #3036609.
                var buggyScript = _resMan.FindResource(new ResourceId(ResourceType.Script, 180), false);

                if (buggyScript != null && (buggyScript.size == 12354 || buggyScript.size == 12362))
                {
                    // TODO: showScummVMDialog("A known buggy game script has been detected, which could "

                    //"prevent you from progressing later on in the game, during "

                    //"the sequence with the Green Man's riddles. Please, apply "

                    //"the latest patch for this game by Sierra to avoid possible "

                    //"problems");
                }
            }

            // TODO:
            // Show a warning if the user has selected a General MIDI device, no GM patch exists
            // (i.e. patch 4) and the game is one of the known 8 SCI1 games that Sierra has provided
            // after market patches for in their "General MIDI Utility".
            //if (_soundCmd.getMusicType() == MT_GM && !ConfMan.getBool("native_mt32"))
            //{
            //    if (!_resMan.findResource(ResourceId(kResourceTypePatch, 4), 0))
            //    {
            //        switch (getGameId())
            //        {
            //            case GID_ECOQUEST:
            //            case GID_HOYLE3:
            //            case GID_LSL1:
            //            case GID_LSL5:
            //            case GID_LONGBOW:
            //            case GID_SQ1:
            //            case GID_SQ4:
            //            case GID_FAIRYTALES:
            //                showScummVMDialog("You have selected General MIDI as a sound device. Sierra "

            //                                  "has provided after-market support for General MIDI for this "

            //                                  "game in their \"General MIDI Utility\". Please, apply this "

            //                                  "patch in order to enjoy MIDI music with this game. Once you "

            //                                  "have obtained it, you can unpack all of the included *.PAT "

            //                                  "files in your ScummVM extras folder and ScummVM will add the "

            //                                  "appropriate patch automatically. Alternatively, you can follow "

            //                                  "the instructions in the READ.ME file included in the patch and "

            //                                  "rename the associated *.PAT file to 4.PAT and place it in the "

            //                                  "game folder. Without this patch, General MIDI music for this "

            //                                  "game will sound badly distorted.");
            //                break;
            //            default:
            //                break;
            //        }
            //    }
            //}

            // TODO:
            //if (gameHasFanMadePatch())
            //{
            //    showScummVMDialog("Your game is patched with a fan made script patch. Such patches have "

            //                      "been reported to cause issues, as they modify game scripts extensively. "

            //                      "The issues that these patches fix do not occur in ScummVM, so you are "

            //                      "advised to remove this patch from your game folder in order to avoid "

            //                      "having unexpected errors and/or issues later on.");
            //}

            RunGame();

            // TODO: ConfMan.flushToDisk();
        }

        private void RunGame()
        {
            // TODO: SetTotalPlayTime(0);

            InitStackBaseWithSelector(Selector(s => s.play)); // Call the play selector

            // TODO: Attach the debug console on game startup, if requested
            //if (DebugMan.isDebugChannelEnabled(kDebugLevelOnStartup))
            //    _console.attach();

            _gamestate._syncedAudioOptions = false;

            do
            {
                _gamestate._executionStackPosChanged = false;
                Vm.Run(_gamestate);
                ExitGame();

                _gamestate._syncedAudioOptions = true;

                if (_gamestate.abortScriptProcessing == AbortGameState.RestartGame)
                {
                    _gamestate._segMan.ResetSegMan();
                    InitGame();
                    InitStackBaseWithSelector(Selector(s => s.play));
                    // TODO: PatchGameSaveRestore();
                    SetLauncherLanguage();
                    _gamestate.gameIsRestarting = GameIsRestarting.RESTART;
                    _gamestate._throttleLastTime = 0;
                    if (_gfxMenu != null)
                        _gfxMenu.Reset();
                    _gamestate.abortScriptProcessing = AbortGameState.None;
                    _gamestate._syncedAudioOptions = false;
                }
                else if (_gamestate.abortScriptProcessing == AbortGameState.LoadGame)
                {
                    _gamestate.abortScriptProcessing = AbortGameState.None;
                    _gamestate._executionStack.Clear();
                    InitStackBaseWithSelector(Selector(s => s.replay));
                    // TODO: PatchGameSaveRestore();
                    SetLauncherLanguage();
                    // TODO: _gamestate.ShrinkStackToBase();
                    _gamestate.abortScriptProcessing = AbortGameState.None;

                    // TODO: SyncSoundSettings();
                    // TODO: SyncIngameAudioOptions();
                    // Games do not set their audio settings when loading
                }
                else {
                    break;  // exit loop
                }
            } while (true);
        }

        private void ExitGame()
        {
            if (_gamestate.abortScriptProcessing != AbortGameState.LoadGame)
            {
                _gamestate._executionStack.Clear();
                // TODO: _audio.StopAllAudio();
                // TODO: _soundCmd.ClearPlayList();
            }

            // TODO Free parser segment here

            // TODO Free scripts here

            // Close all opened file handles
            Array.Resize(ref _gamestate._fileHandles, 5);
        }

        private void InitStackBaseWithSelector(int selector)
        {
            _gamestate.stack_base[0] = Register.Make(0, (ushort)selector);
            _gamestate.stack_base[1] = Register.NULL_REG;

            // Register the first element on the execution stack
            if (Vm.SendSelector(_gamestate, _gameObjectAddress, _gameObjectAddress, _gamestate.stack_base, 2, _gamestate.stack_base) == 0)
            {
                // TODO: _console.printObject(_gameObjectAddress);
                // error("initStackBaseWithSelector: error while registering the first selector in the call stack");
            }
        }

        private void SetLauncherLanguage()
        {
            // TODO:
            //if ((_gameDescription.flags & ADGF_ADDENGLISH)!=0)
            //{
            //    // If game is multilingual
            //    if (Common::parseLanguage(ConfMan.get("language")) == Common::EN_ANY)
            //    {
            //        // and English was selected as language
            //        if (SELECTOR(printLang) != -1) // set text language to English
            //            writeSelectorValue(_gamestate._segMan, _gameObjectAddress, SELECTOR(printLang), K_LANG_ENGLISH);
            //        if (SELECTOR(parseLang) != -1) // and set parser language to English as well
            //            writeSelectorValue(_gamestate._segMan, _gameObjectAddress, SELECTOR(parseLang), K_LANG_ENGLISH);
            //    }
            //}
        }

        private void InitGraphics()
        {
            // Reset all graphics objects
            _gfxAnimate = null;
            _gfxCache = null;
            _gfxCompare = null;
            _gfxControls16 = null;
            _gfxCoordAdjuster = null;
            _gfxCursor = null;
            _gfxMacIconBar = null;
            _gfxMenu = null;
            _gfxPaint = null;
            _gfxPaint16 = null;
            _gfxPalette = null;
            _gfxPorts = null;
            _gfxText16 = null;
            _gfxTransitions = null;
# if ENABLE_SCI32
            _gfxControls32 = null;
            _gfxText32 = null;
            _robotDecoder = null;
            _gfxFrameout = null;
            _gfxPaint32 = null;
#endif
            if (HasMacIconBar)
                _gfxMacIconBar = new GfxMacIconBar();

            _gfxPalette = new GfxPalette(_system, _resMan, _gfxScreen);
            _gfxCache = new GfxCache(_resMan, _gfxScreen, _gfxPalette);
            _gfxCursor = new GfxCursor(_system, _resMan, _gfxPalette, _gfxScreen);

#if ENABLE_SCI32
            if (getSciVersion() >= SCI_VERSION_2)
            {
                // SCI32 graphic objects creation
                _gfxCoordAdjuster = new GfxCoordAdjuster32(_gamestate._segMan);
                _gfxCursor.init(_gfxCoordAdjuster, _eventMan);
                _gfxCompare = new GfxCompare(_gamestate._segMan, _gfxCache, _gfxScreen, _gfxCoordAdjuster);
                _gfxPaint32 = new GfxPaint32(_resMan, _gfxCoordAdjuster, _gfxScreen, _gfxPalette);
                _gfxPaint = _gfxPaint32;
                _gfxText32 = new GfxText32(_gamestate._segMan, _gfxCache, _gfxScreen);
                _gfxControls32 = new GfxControls32(_gamestate._segMan, _gfxCache, _gfxText32);
                _robotDecoder = new RobotDecoder(getPlatform() == Common::kPlatformMacintosh);
                _gfxFrameout = new GfxFrameout(_gamestate._segMan, _resMan, _gfxCoordAdjuster, _gfxCache, _gfxScreen, _gfxPalette, _gfxPaint32);
            }
            else {
#endif
            // SCI0-SCI1.1 graphic objects creation
            _gfxPorts = new GfxPorts(_gamestate._segMan, _gfxScreen);
            _gfxCoordAdjuster = new GfxCoordAdjuster16(_gfxPorts);
            _gfxCursor.Init(_gfxCoordAdjuster, _eventMan);
            _gfxCompare = new GfxCompare(_gamestate._segMan, _gfxCache, _gfxScreen, _gfxCoordAdjuster);
            _gfxTransitions = new GfxTransitions(_gfxScreen, _gfxPalette);
            _gfxPaint16 = new GfxPaint16(_resMan, _gamestate._segMan, _gfxCache, _gfxPorts, _gfxCoordAdjuster, _gfxScreen, _gfxPalette, _gfxTransitions, _audio);
            _gfxPaint = _gfxPaint16;
            _gfxAnimate = new GfxAnimate(_gamestate, _gfxCache, _gfxPorts, _gfxPaint16, _gfxScreen, _gfxPalette, _gfxCursor, _gfxTransitions);
            _gfxText16 = new GfxText16(_gfxCache, _gfxPorts, _gfxPaint16, _gfxScreen);
            _gfxControls16 = new GfxControls16(_gamestate._segMan, _gfxPorts, _gfxPaint16, _gfxText16, _gfxScreen);
            _gfxMenu = new GfxMenu(_eventMan, _gamestate._segMan, _gfxPorts, _gfxPaint16, _gfxText16, _gfxScreen, _gfxCursor);

            _gfxMenu.Reset();

            _gfxPorts.Init(_features.UsesOldGfxFunctions(), _gfxPaint16, _gfxText16);
            _gfxPaint16.Init(_gfxAnimate, _gfxText16);

# if ENABLE_SCI32
            }
#endif

            // Set default (EGA, amiga or resource 999) palette
            _gfxPalette.SetDefault();
        }

        private void script_adjust_opcode_formats()
        {
            Instance._opcode_formats = new opcode_format[128][];
            //memcpy(Instance._opcode_formats, g_base_opcode_formats, 128 * 4 * sizeof(opcode_format));
            for (int i = 0; i < 128; i++)
            {
                Instance._opcode_formats[i] = new opcode_format[4];
                Array.Copy(g_base_opcode_formats[i], Instance._opcode_formats[i], g_base_opcode_formats[i].Length);
            }

            if (Instance._features.DetectLofsType() != SciVersion.V0_EARLY)
            {
                Instance._opcode_formats[Vm.op_lofsa][0] = opcode_format.Script_Offset;
                Instance._opcode_formats[Vm.op_lofss][0] = opcode_format.Script_Offset;
            }

# if ENABLE_SCI32
            // In SCI32, some arguments are now words instead of bytes
            if (getSciVersion() >= SCI_VERSION_2)
            {
                Instance._opcode_formats[op_calle][2] = Script_Word;
                Instance._opcode_formats[op_callk][1] = Script_Word;
                Instance._opcode_formats[op_super][1] = Script_Word;
                Instance._opcode_formats[op_send][0] = Script_Word;
                Instance._opcode_formats[op_self][0] = Script_Word;
                Instance._opcode_formats[op_call][1] = Script_Word;
                Instance._opcode_formats[op_callb][1] = Script_Word;
            }

            if (getSciVersion() >= SCI_VERSION_3)
            {
                // TODO: There are also opcodes in
                // here to get the superclass, and possibly the species too.
                Instance._opcode_formats[0x4d / 2][0] = Script_None;
                Instance._opcode_formats[0x4e / 2][0] = Script_None;
            }
#endif
        }

        private bool InitGame()
        {
            // Script 0 needs to be allocated here before anything else!
            int script0Segment = _gamestate._segMan.GetScriptSegment(0, ScriptLoadType.LOCK);
            ushort segid = 0;
            DataStack stack = _gamestate._segMan.AllocateStack(Vm.STACK_SIZE, ref segid);

            _gamestate._msgState = new MessageState(_gamestate._segMan);
            _gamestate.gcCountDown = Vm.GC_INTERVAL - 1;

            // Script 0 should always be at segment 1
            if (script0Segment != 1)
            {
                // TODO: debug(2, "Failed to instantiate script 0");
                return false;
            }

            _gamestate.InitGlobals();
            _gamestate._segMan.InitSysStrings();

            _gamestate.r_acc = _gamestate.r_prev = Register.NULL_REG;

            _gamestate._executionStack.Clear();    // Start without any execution stack
            _gamestate.executionStackBase = -1; // No vm is running yet
            _gamestate._executionStackPosChanged = false;

            _gamestate.abortScriptProcessing = AbortGameState.None;
            _gamestate.gameIsRestarting = GameIsRestarting.NONE;

            _gamestate.stack_base = new StackPtr(stack._entries, 0);
            _gamestate.stack_top = new StackPtr(stack._entries, stack._capacity);

            if (_gamestate._segMan.InstantiateScript(0) == 0)
            {
                throw new InvalidOperationException("initGame(): Could not instantiate script 0");
            }

            // Reset parser
            if (_vocabulary != null)
                _vocabulary.Reset();

            _gamestate.lastWaitTime = _gamestate._screenUpdateTime = (uint)Environment.TickCount;

            // Load game language into printLang property of game object
            SetSciLanguage();

            return true;
        }

        private void SetSciLanguage()
        {
            SetSciLanguage(GetSciLanguage());
        }

        public static int Selector(Func<SelectorCache, int> func)
        {
            return func(Instance._kernel._selectorCache);
        }

        private Language GetSciLanguage()
        {
            Language lang = (Language)_resMan.GetAudioLanguage();
            if (lang != Sci.Language.NONE)
                return lang;

            lang = Sci.Language.ENGLISH;

            if (Selector(s => s.printLang) != -1)
            {
                lang = (Language)ReadSelectorValue(_gamestate._segMan, _gameObjectAddress, Selector(s => s.printLang));

                if ((ResourceManager.GetSciVersion() >= SciVersion.V1_1) || (lang == Sci.Language.NONE))
                {
                    // If language is set to none, we use the language from the game detector.
                    // SSCI reads this from resource.cfg (early games do not have a language
                    // setting in resource.cfg, but instead have the secondary language number
                    // hardcoded in the game script).
                    // SCI1.1 games always use the language setting from the config file
                    // (essentially disabling runtime language switching).
                    // Note: only a limited number of multilanguage games have been tested
                    // so far, so this information may not be 100% accurate.
                    switch (Language)
                    {
                        case Core.Common.Language.FR_FRA:
                            lang = Sci.Language.FRENCH;
                            break;
                        case Core.Common.Language.ES_ESP:
                            lang = Sci.Language.SPANISH;
                            break;
                        case Core.Common.Language.IT_ITA:
                            lang = Sci.Language.ITALIAN;
                            break;
                        case Core.Common.Language.DE_DEU:
                            lang = Sci.Language.GERMAN;
                            break;
                        case Core.Common.Language.JA_JPN:
                            lang = Sci.Language.JAPANESE;
                            break;
                        case Core.Common.Language.PT_BRA:
                            lang = Sci.Language.PORTUGUESE;
                            break;
                        default:
                            lang = Sci.Language.ENGLISH;
                            break;
                    }
                }
            }

            return lang;
        }

        private static uint ReadSelectorValue(SegManager segMan, Register obj, int selectorId)
        {
            return ReadSelector(segMan, obj, selectorId).Offset;
        }

        private static Register ReadSelector(SegManager segMan, Register obj, int selectorId)
        {
            ObjVarRef address = new ObjVarRef();
            Register fptr;
            if (LookupSelector(segMan, obj, selectorId, address, out fptr) != SelectorType.Variable)
                return Register.NULL_REG;
            else
                return address.GetPointer(segMan);
        }

        public static SelectorType LookupSelector(SegManager segMan, Register obj_location, int selectorId, ObjVarRef varp, out Register fptr)
        {
            fptr = null;
            SciObject obj = segMan.GetObject(obj_location);
            int index;
            bool oldScriptHeader = (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY);

            // Early SCI versions used the LSB in the selector ID as a read/write
            // toggle, meaning that we must remove it for selector lookup.
            if (oldScriptHeader)
                selectorId &= ~1;

            if (obj == null)
            {
                throw new InvalidOperationException($"lookupSelector(): Attempt to send to non-object or invalid script. Address was {obj_location}");
            }

            index = obj.LocateVarSelector(segMan, selectorId);

            if (index >= 0)
            {
                // Found it as a variable
                if (varp != null)
                {
                    varp.obj = obj_location;
                    varp.varindex = index;
                }
                return SelectorType.Variable;
            }
            else {
                // Check if it's a method, with recursive lookup in superclasses
                while (obj != null)
                {
                    index = obj.FuncSelectorPosition(selectorId);
                    if (index >= 0)
                    {
                        fptr = obj.GetFunction(index);
                        return SelectorType.Method;
                    }
                    else {
                        obj = segMan.GetObject(obj.SuperClassSelector);
                    }
                }

                return SelectorType.None;
            }


            //	return _lookupSelector_function(segMan, obj, selectorId, fptr);
        }

        private void SetSciLanguage(Language lang)
        {
            if (Selector(s => s.printLang) != -1)
                WriteSelectorValue(_gamestate._segMan, _gameObjectAddress, Selector(s => s.printLang), (ushort)lang);
        }

        private static void WriteSelectorValue(SegManager segMan, Register obj, int selectorId, ushort value)
        {
            WriteSelector(segMan, obj, selectorId, Register.Make(0, value));
        }

        private static void WriteSelector(SegManager segMan, Register obj, int selectorId, Register value)
        {
            ObjVarRef address = new ObjVarRef();

            if ((selectorId < 0) || (selectorId > Instance._kernel.SelectorNamesSize))
            {
                throw new InvalidOperationException($"Attempt to write to invalid selector {selectorId} of object at {obj}.");
            }

            Register tmp;
            if (LookupSelector(segMan, obj, selectorId, address, out tmp) != SelectorType.Variable)
                throw new InvalidOperationException($"Selector '{Instance._kernel.GetSelectorName(selectorId)}' of object at {obj} could not be written to");
            else
                address.GetPointer(segMan).Set(value);
        }

        event EventHandler IEngine.ShowMenuDialogRequested
        {
            add { }
            remove { }
        }

        void IEngine.Load(string filename)
        {
            throw new NotImplementedException();
        }

        void IEngine.Save(string filename)
        {
            throw new NotImplementedException();
        }

        // Base set of opcode formats. They're copied and adjusted slightly in
        // script_adjust_opcode_format depending on SCI version.
        static readonly opcode_format[][] g_base_opcode_formats = new opcode_format[128][] {
	        // 00 - 03 / bnot, add, sub, mul
	        new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None},
	        // 04 - 07 / div, mod, shr, shl
	        new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None},
	        // 08 - 0B / xor, and, or, neg
	        new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None},
	        // 0C - 0F / not, eq, ne, gt
	        new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None},
	        // 10 - 13 / ge, lt, le, ugt
	        new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None},
	        // 14 - 17 / uge, ult, ule, bt
	        new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_SRelative},
	        // 18 - 1B / bnt, jmp, ldi, push
	        new opcode_format[]{opcode_format.Script_SRelative}, new opcode_format[]{opcode_format.Script_SRelative}, new opcode_format[]{opcode_format.Script_SVariable}, new opcode_format[]{opcode_format.Script_None},
	        // 1C - 1F / pushi, toss, dup, link
	        new opcode_format[]{opcode_format.Script_SVariable}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_Variable},
	        // 20 - 23 / call, callk, callb, calle
	        new opcode_format[]{opcode_format.Script_SRelative, opcode_format.Script_Byte }, new opcode_format[]{opcode_format.Script_Variable, opcode_format.Script_Byte}, new opcode_format[]{opcode_format.Script_Variable, opcode_format.Script_Byte }, new opcode_format[]{opcode_format.Script_Variable, opcode_format.Script_SVariable, opcode_format.Script_Byte },
	        // 24 - 27 / ret, send, dummy, dummy
	        new opcode_format[]{opcode_format.Script_End}, new opcode_format[]{opcode_format.Script_Byte}, new opcode_format[]{opcode_format.Script_Invalid}, new opcode_format[]{opcode_format.Script_Invalid},
	        // 28 - 2B / class, dummy, self, super
	        new opcode_format[]{opcode_format.Script_Variable}, new opcode_format[]{opcode_format.Script_Invalid}, new opcode_format[]{opcode_format.Script_Byte}, new opcode_format[]{opcode_format.Script_Variable, opcode_format.Script_Byte },
	        // 2C - 2F / rest, lea, selfID, dummy
	        new opcode_format[]{opcode_format.Script_SVariable}, new opcode_format[]{opcode_format.Script_SVariable, opcode_format.Script_Variable }, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_Invalid},
	        // 30 - 33 / pprev, pToa, aTop, pTos
	        new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_Property}, new opcode_format[]{opcode_format.Script_Property}, new opcode_format[]{opcode_format.Script_Property},
	        // 34 - 37 / sTop, ipToa, dpToa, ipTos
	        new opcode_format[]{opcode_format.Script_Property}, new opcode_format[]{opcode_format.Script_Property}, new opcode_format[]{opcode_format.Script_Property}, new opcode_format[]{opcode_format.Script_Property},
	        // 38 - 3B / dpTos, lofsa, lofss, push0
	        new opcode_format[]{opcode_format.Script_Property}, new opcode_format[]{opcode_format.Script_SRelative}, new opcode_format[]{opcode_format.Script_SRelative}, new opcode_format[]{opcode_format.Script_None},
	        // 3C - 3F / push1, push2, pushSelf, line
	        new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_None}, new opcode_format[]{opcode_format.Script_Word},
	        // ------------------------------------------------------------------------
	        // 40 - 43 / lag, lal, lat, lap
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 44 - 47 / lsg, lsl, lst, lsp
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 48 - 4B / lagi, lali, lati, lapi
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 4C - 4F / lsgi, lsli, lsti, lspi
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // ------------------------------------------------------------------------
	        // 50 - 53 / sag, sal, sat, sap
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 54 - 57 / ssg, ssl, sst, ssp
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 58 - 5B / sagi, sali, sati, sapi
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 5C - 5F / ssgi, ssli, ssti, sspi
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // ------------------------------------------------------------------------
	        // 60 - 63 / plusag, plusal, plusat, plusap
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 64 - 67 / plussg, plussl, plusst, plussp
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 68 - 6B / plusagi, plusali, plusati, plusapi
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 6C - 6F / plussgi, plussli, plussti, plusspi
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // ------------------------------------------------------------------------
	        // 70 - 73 / minusag, minusal, minusat, minusap
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 74 - 77 / minussg, minussl, minusst, minussp
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 78 - 7B / minusagi, minusali, minusati, minusapi
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param},
	        // 7C - 7F / minussgi, minussli, minussti, minusspi
	        new opcode_format[]{opcode_format.Script_Global}, new opcode_format[]{opcode_format.Script_Local}, new opcode_format[]{opcode_format.Script_Temp}, new opcode_format[]{opcode_format.Script_Param}
        };
        private AudioPlayer _audio;
        private ISystem _system;
    }
}
