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
using NScumm.Core.Audio;
using NScumm.Core.Common;

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

        private RandomSource _rng;
        private ResourceManager _resMan;
        private string _directory;
        private ADGameDescription _gameDescription;
        private SciGameId _gameId;
        public SoundCommandParser _soundCmd;
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

        // Maps half-width single-byte SJIS to full-width double-byte SJIS
        // Note: SSCI maps 0x5C (the Yen symbol) to 0x005C, which terminates
        // the string with the leading 0x00 byte. We map Yen to 0x818F.
        private static readonly ushort[] s_halfWidthSJISMap = {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x81A8, 0x81A9, 0x81AA, 0x81AB,
            0x8140, 0x8149, 0x818D, 0x8194, 0x8190, 0x8193, 0x8195, 0x818C,
            0x8169, 0x816A, 0x8196, 0x817B, 0x8143, 0x817C, 0x8144, 0x815E,
            0x824F, 0x8250, 0x8251, 0x8252, 0x8253, 0x8254, 0x8255, 0x8256,
            0x8257, 0x8258, 0x8146, 0x8147, 0x8183, 0x8181, 0x8184, 0x8148,
            0x8197, 0x8260, 0x8261, 0x8262, 0x8263, 0x8264, 0x8265, 0x8266,
            0x8267, 0x8268, 0x8269, 0x826A, 0x826B, 0x826C, 0x826D, 0x826E,
            0x826F, 0x8270, 0x8271, 0x8272, 0x8273, 0x8274, 0x8275, 0x8276,
            0x8277, 0x8278, 0x8279, 0x816D, 0x818F /* 0x005C */, 0x816E, 0x814F, 0x8151,
            0x8280, 0x8281, 0x8282, 0x8283, 0x8284, 0x8285, 0x8286, 0x8287,
            0x8288, 0x8289, 0x828A, 0x828B, 0x828C, 0x828D, 0x828E, 0x828F,
            0x8290, 0x8291, 0x8292, 0x8293, 0x8294, 0x8295, 0x8296, 0x8297,
            0x8298, 0x8299, 0x829A, 0x816F, 0x8162, 0x8170, 0x8160, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0x8140, 0x8142, 0x8175, 0x8176, 0x8141, 0x8145, 0x8392, 0x8340,
            0x8342, 0x8344, 0x8346, 0x8348, 0x8383, 0x8385, 0x8387, 0x8362,
            0x815C, 0x8341, 0x8343, 0x8345, 0x8347, 0x8349, 0x834A, 0x834C,
            0x834E, 0x8350, 0x8352, 0x8354, 0x8356, 0x8358, 0x835A, 0x835C,
            0x835E, 0x8360, 0x8363, 0x8365, 0x8367, 0x8369, 0x836A, 0x836B,
            0x836C, 0x836D, 0x836E, 0x8371, 0x8374, 0x8377, 0x837A, 0x837D,
            0x837E, 0x8380, 0x8381, 0x8382, 0x8384, 0x8386, 0x8388, 0x8389,
            0x838A, 0x838B, 0x838C, 0x838D, 0x838F, 0x8393, 0x814A, 0x814B,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        public SciEngine(ISystem system, IAudioOutput output, SciGameDescriptor desc, SciGameId id)
        {
            _engineStartTime = Environment.TickCount;
            _system = system;
            _mixer = new Mixer(44100);
            // HACK:
            _mixer.Read(new byte[0], 0);
            output.SetSampleProvider(_mixer);
            _instance = this;
            _rng = new RandomSource("sci");
            _gameId = id;
            _gameDescription = desc.GameDescription;
            _directory = ServiceLocator.FileStorage.GetDirectoryName(desc.Path);
            _debugState = new DebugState();
        }

        public string GetSavegameName(int nr)
        {
            return $"{_gameDescription.gameid}.{nr:D3}";
        }

        /// <summary>
        /// Processes a multilanguage string based on the current language settings and
        /// returns a string that is ready to be displayed.
        /// </summary>
        /// <param name="str">the multilanguage string</param>
        /// <param name="languageSplitter"></param>
        /// <param name="sep">
        /// optional seperator between main language and subtitle language,
        /// if NULL is passed no subtitle will be added to the returned string
        /// </param>
        /// <returns>processed string</returns>
        public string StrSplitLanguage(string str, ushort languageSplitter, string sep = "\r----------\r")
        {
            Language activeLanguage = GetSciLanguage();
            Language subtitleLanguage = Sci.Language.NONE;

            if (Selector(s => s.subtitleLang) != -1)
                subtitleLanguage = (Language)ReadSelectorValue(_gamestate._segMan, _gameObjectAddress, Selector(s => s.subtitleLang));

            Language foundLanguage;
            string retval = GetSciLanguageString(str, activeLanguage, out foundLanguage, out languageSplitter);

            // Don't add subtitle when separator is not set, subtitle language is not set, or
            // string contains only one language
            if ((sep == null) || (subtitleLanguage == Sci.Language.NONE) || (foundLanguage == Sci.Language.NONE))
                return retval;

            // Add subtitle, unless the subtitle language doesn't match the languages in the string
            if ((subtitleLanguage == Sci.Language.ENGLISH) || (subtitleLanguage == foundLanguage))
            {
                retval += sep;
                retval += GetSciLanguageString(str, subtitleLanguage);
            }

            return retval;
        }

        public string GetSciLanguageString(string str, Language requestedLanguage)
        {
            Language secondaryLanguage;
            ushort languageSplitter;
            return GetSciLanguageString(str, requestedLanguage, out secondaryLanguage, out languageSplitter);
        }

        public string GetSciLanguageString(string str, Language requestedLanguage, out Language secondaryLanguage, out ushort languageSplitter)
        {
            secondaryLanguage = Sci.Language.NONE;
            languageSplitter = 0;

            var textPtr = 0;
            Language foundLanguage = Sci.Language.NONE;
            char curChar = '\0';
            char curChar2 = '\0';
            int i;
            for (i = 0; i < str.Length; i++)
            {
                curChar = str[i];

                if ((curChar == '%') || (curChar == '#'))
                {
                    curChar2 = str[i + 1];
                    foundLanguage = CharToLanguage(curChar2);

                    if (foundLanguage != Sci.Language.NONE)
                    {
                        // Return language splitter
                        languageSplitter = (ushort)(curChar | (curChar2 << 8));
                        // Return the secondary language found in the string
                        secondaryLanguage = foundLanguage;
                        break;
                    }
                }
            }

            if (i == str.Length)
            {
                curChar = '\0';
            }

            if (foundLanguage == requestedLanguage)
            {
                if (curChar2 == 'J')
                {
                    // Japanese including Kanji, displayed with system font
                    // Convert half-width characters to full-width equivalents
                    string fullWidth = string.Empty;
                    ushort mappedChar;

                    textPtr += 2; // skip over language splitter

                    while (true)
                    {
                        curChar = str[textPtr];

                        switch (curChar)
                        {
                            case '\0': // Terminator NUL
                                return fullWidth;
                            case '\\':
                                // "\n", "\N", "\r" and "\R" were overwritten with SPACE + 0x0D in PC-9801 SSCI
                                //  inside GetLongest() (text16). We do it here, because it's much cleaner and
                                //  we have to process the text here anyway.
                                //  Occurs for example in Police Quest 2 intro
                                curChar2 = str[textPtr + 1];
                                switch (curChar2)
                                {
                                    case 'n':
                                    case 'N':
                                    case 'r':
                                    case 'R':
                                        fullWidth += ' ';
                                        fullWidth += 0x0D; // CR
                                        textPtr += 2;
                                        continue;
                                }
                                break;
                        }

                        textPtr++;

                        mappedChar = s_halfWidthSJISMap[curChar];
                        if (mappedChar != 0)
                        {
                            fullWidth += mappedChar >> 8;
                            fullWidth += mappedChar & 0xFF;
                        }
                        else {
                            // Copy double-byte character
                            curChar2 = str[textPtr++];
                            if (curChar == 0)
                            {
                                throw new InvalidOperationException("SJIS character {curChar:X2} is missing second byte");
                            }
                            fullWidth += curChar;
                            fullWidth += curChar2;
                        }
                    }

                }
                else {
                    return str.Substring(textPtr + 2);
                }
            }

            if (curChar != 0)
                return str.Substring(0, textPtr);

            return str;
        }

        public void Sleep(int msecs)
        {
            int time;
            int wakeup_time = Environment.TickCount + msecs;

            while (true)
            {
                // let backend process events and update the screen
                _eventMan.GetSciEvent(SciEvent.SCI_EVENT_PEEK);
                time = Environment.TickCount;
                if (time + 10 < wakeup_time)
                {
                    ServiceLocator.Platform.Sleep(10);
                }
                else {
                    if (time < wakeup_time)
                        ServiceLocator.Platform.Sleep(wakeup_time - time);
                    break;
                }

            }
        }

        private static Language CharToLanguage(char c)
        {
            switch (c)
            {
                case 'F':
                    return Sci.Language.FRENCH;
                case 'S':
                    return Sci.Language.SPANISH;
                case 'I':
                    return Sci.Language.ITALIAN;
                case 'G':
                    return Sci.Language.GERMAN;
                case 'J':
                case 'j':
                    return Sci.Language.JAPANESE;
                case 'P':
                    return Sci.Language.PORTUGUESE;
                default:
                    return Sci.Language.NONE;
            }
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

        public string FilePrefix
        {
            get
            {
                return _gameDescription.gameid;
            }
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

        public GameFeatures Features { get { return _features; } }

        public int InQfGImportRoom
        {
            get
            {
                if (_gameId == SciGameId.QFG2 && _gamestate.CurrentRoomNumber == 805)
                {
                    // QFG2 character import screen
                    return 2;
                }
                else if (_gameId == SciGameId.QFG3 && _gamestate.CurrentRoomNumber == 54)
                {
                    // QFG3 character import screen
                    return 3;
                }
                else if (_gameId == SciGameId.QFG4 && _gamestate.CurrentRoomNumber == 54)
                {
                    return 4;
                }
                return 0;
            }
        }

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

        public bool IsBE
        {
            get
            {
                switch (_gameDescription.platform)
                {
                    case Platform.Amiga:
                    case Platform.Macintosh:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public RandomSource Rng { get { return _rng; } }

        public ResourceManager ResMan { get { return _resMan; } }

        public Kernel Kernel { get { return _kernel; } }

        public ISaveFileManager SaveFileManager { get { return _system.SaveFileManager; } }

        public string Directory { get { return _directory; } }

        public EventManager EventManager { get { return _eventMan; } }

        public IMixer Mixer { get { return _mixer; } }

        public ISystem System { get { return _system; } }

        public bool ShouldQuit { get; internal set; }

        public Vocabulary Vocabulary { get { return _vocabulary; } }

        /// <summary>
        /// Gets or sets the total play time.
        /// </summary>
        public int TotalPlaytime
        {
            get
            {
                if (_pauseLevel == 0)
                    return Environment.TickCount - _engineStartTime;
                else
                    return _pauseStartTime - _engineStartTime;
            }
            set
            {
                var currentTime = Environment.TickCount;

                // We need to reset the pause start time here in case the engine is already
                // paused to avoid any incorrect play time counting.
                if (_pauseLevel > 0)
                    _pauseStartTime = currentTime;

                _engineStartTime = currentTime - value;
            }
        }

        public string SavegamePattern { get { return _gameDescription.gameid + ".???"; } }

        public string WrapFilename(string name)
        {
            return FilePrefix + "-" + name;
        }

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
            _gfxScreen = new GfxScreen(_resMan);
            _gfxScreen.IsUnditheringEnabled = false;
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
            _soundCmd = new SoundCommandParser(_resMan, segMan, _kernel, _audio, _features.DetectDoSoundType());

            // TODO: SyncSoundSettings();
            _soundCmd.SetMasterVolume(11);
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
            //    if (g_sci.getGameId() == SciGameId.JONES)
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
            //            case SciGameId.ECOQUEST:
            //            case SciGameId.HOYLE3:
            //            case SciGameId.LSL1:
            //            case SciGameId.LSL5:
            //            case SciGameId.LONGBOW:
            //            case SciGameId.SQ1:
            //            case SciGameId.SQ4:
            //            case SciGameId.FAIRYTALES:
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

        public string StrSplit(string text, string sep = "\r----------\r")
        {
            ushort tmp;
            return StrSplitLanguage(text, out tmp, sep);
        }

        /// <summary>
        /// Processes a multilanguage string based on the current language settings and
        /// returns a string that is ready to be displayed.
        /// </summary>
        /// <param name="str">the multilanguage string</param>
        /// <param name="languageSplitter"></param>
        /// <param name="sep">optional seperator between main language and subtitle language,if NULL is passed no subtitle will be added to the returned string</param>
        /// <returns>processed string</returns>
        private string StrSplitLanguage(string str, out ushort languageSplitter, string sep = "\r----------\r")
        {
            Language activeLanguage = GetSciLanguage();
            Language subtitleLanguage = Sci.Language.NONE;

            if (Selector(s => s.subtitleLang) != -1)
                subtitleLanguage = (Language)ReadSelectorValue(_gamestate._segMan, _gameObjectAddress, Selector(s => s.subtitleLang));

            Language foundLanguage;
            string retval = GetSciLanguageString(str, activeLanguage, out foundLanguage, out languageSplitter);

            // Don't add subtitle when separator is not set, subtitle language is not set, or
            // string contains only one language
            if ((sep == null) || (subtitleLanguage == Sci.Language.NONE) || (foundLanguage == Sci.Language.NONE))
                return retval;

            // Add subtitle, unless the subtitle language doesn't match the languages in the string
            if ((subtitleLanguage == Sci.Language.ENGLISH) || (subtitleLanguage == foundLanguage))
            {
                retval += sep;
                retval += GetSciLanguageString(str, subtitleLanguage);
            }

            return retval;
        }

        private void RunGame()
        {
            TotalPlaytime = 0;

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
                    _gamestate.ShrinkStackToBase();
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
                _audio.StopAllAudio();
                _soundCmd.ClearPlayList();
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
            if (Vm.SendSelector(_gamestate, _gameObjectAddress, _gameObjectAddress, _gamestate.stack_base, 2, _gamestate.stack_base) == null)
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

            _gfxPalette = new GfxPalette(_resMan, _gfxScreen);
            _gfxCache = new GfxCache(_resMan, _gfxScreen, _gfxPalette);
            _gfxCursor = new GfxCursor(_resMan, _gfxPalette, _gfxScreen);

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

            _gamestate.r_acc = Register.Make(Register.NULL_REG);
            _gamestate.r_prev = Register.Make(Register.NULL_REG);

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

            _gamestate.lastWaitTime = _gamestate._screenUpdateTime = Environment.TickCount;

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

        public static uint ReadSelectorValue(SegManager segMan, Register obj, int selectorId)
        {
            return ReadSelector(segMan, obj, selectorId).Offset;
        }

        public static uint ReadSelectorValue(SegManager segMan, Register obj, Func<SelectorCache, int> func)
        {
            return ReadSelector(segMan, obj, Selector(func)).Offset;
        }

        public static Register ReadSelector(SegManager segMan, Register obj, Func<SelectorCache, int> func)
        {
            return ReadSelector(segMan, obj, Selector(func));
        }

        public static Register ReadSelector(SegManager segMan, Register obj, int selectorId)
        {
            ObjVarRef address = new ObjVarRef();
            Register fptr;
            if (LookupSelector(segMan, obj, selectorId, address, out fptr) != SelectorType.Variable)
                return Register.NULL_REG;
            else
                return address.GetPointer(segMan)[0];
        }

        public static SelectorType LookupSelector(SegManager segMan, Register obj_location, Func<SelectorCache, int> func, ObjVarRef varp, out Register fptr)
        {
            return LookupSelector(segMan, obj_location, Selector(func), varp, out fptr);
        }

        public static SelectorType LookupSelector(SegManager segMan, Register obj_location, int selectorId, ObjVarRef varp, out Register fptr)
        {
            fptr = Register.NULL_REG;
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
                    varp.obj = Register.Make(obj_location);
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

        public static void InvokeSelector(EngineState s, Register @object, Func<SelectorCache, int> func, int k_argc, StackPtr? k_argp, int argc = 0, StackPtr? argv = null)
        {
            InvokeSelector(s, @object, Selector(func), k_argc, k_argp, argc, argv);
        }

        public static void InvokeSelector(EngineState s, Register @object, int selectorId, int k_argc, StackPtr? k_argp, int argc = 0, StackPtr? argv = null)
        {
            int i;
            int framesize = 2 + 1 * argc;
            StackPtr stackframe = k_argp.Value + k_argc;

            stackframe[0] = Register.Make(0, (ushort)selectorId);  // The selector we want to call
            stackframe[1] = Register.Make(0, (ushort)argc); // Argument count

            Register tmp;
            var slc_type = LookupSelector(s._segMan, @object, selectorId, null, out tmp);

            if (slc_type == SelectorType.None)
            {
                throw new InvalidOperationException($"Selector '{Instance.Kernel.GetSelectorName(selectorId)}' of object at {@object} could not be invoked");
            }
            if (slc_type == SelectorType.Variable)
            {
                throw new InvalidOperationException($"Attempting to invoke variable selector {Instance.Kernel.GetSelectorName(selectorId)} of object {@object}");
            }

            for (i = 0; i < argc; i++)
                stackframe[2 + i] = argv.Value[i]; // Write each argument

            ExecStack xstack;

            // Now commit the actual function:
            xstack = Vm.SendSelector(s, @object, @object, stackframe, framesize, stackframe);

            xstack.sp += argc + 2;
            xstack.fp += argc + 2;


            Vm.Run(s); // Start a new vm
        }


        private void SetSciLanguage(Language lang)
        {
            if (Selector(s => s.printLang) != -1)
                WriteSelectorValue(_gamestate._segMan, _gameObjectAddress, Selector(s => s.printLang), (ushort)lang);
        }

        public static void WriteSelectorValue(SegManager segMan, Register obj, Func<SelectorCache, int> func, ushort value)
        {
            WriteSelectorValue(segMan, obj, Selector(func), value);
        }

        public static void WriteSelectorValue(SegManager segMan, Register obj, int selectorId, ushort value)
        {
            WriteSelector(segMan, obj, selectorId, Register.Make(0, value));
        }

        public static void WriteSelector(SegManager segMan, Register obj, Func<SelectorCache, int> func, Register value)
        {
            WriteSelector(segMan, obj, Selector(func), value);
        }

        public static void WriteSelector(SegManager segMan, Register obj, int selectorId, Register value)
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
            {
                var ptr = address.GetPointer(segMan);
                ptr[0] = value;
            }
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

        public void CheckVocabularySwitch()
        {
            ushort parserLanguage = 1;
            if (Selector(o => o.parseLang) != -1)
                parserLanguage = (ushort)ReadSelectorValue(_gamestate._segMan, _gameObjectAddress, o => o.parseLang);

            if (parserLanguage != _vocabularyLanguage)
            {
                _vocabulary = new Vocabulary(_resMan, parserLanguage > 1 ? true : false);
                _vocabulary.Reset();
                _vocabularyLanguage = parserLanguage;
            }
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
        public AudioPlayer _audio;
        private ISystem _system;
        private Mixer _mixer;

        /// <summary>
        /// The pause level, 0 means 'running', a positive value indicates
        /// how often the engine has been paused (and hence how often it has
        /// to be un-paused before it resumes running). This makes it possible
        /// to nest code which pauses the engine.
        /// </summary>
        private int _pauseLevel;

        /// <summary>
        /// The time when the engine was started. This value is used to calculate
        /// the current play time of the game running.
        /// </summary>
        private int _engineStartTime;

        /// <summary>
        /// The time when the pause was started.
        /// </summary>
        private int _pauseStartTime;
        private ushort _vocabularyLanguage;
    }
}
