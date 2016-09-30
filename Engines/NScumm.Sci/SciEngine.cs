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
using NScumm.Core.IO;
using NScumm.Sci.Sound;
using NScumm.Sci.Engine;
using NScumm.Sci.Graphics;
using NScumm.Sci.Parser;
using NScumm.Core.Common;
using static NScumm.Core.DebugHelper;
using NScumm.Sci.Video;

namespace NScumm.Sci
{
    internal class FanMadePatchInfo
    {
        public readonly SciGameId GameId;
        public readonly ushort TargetScript;
        public readonly ushort TargetSize;
        public readonly ushort PatchedByteOffset;
        public readonly byte PatchedByte;

        public FanMadePatchInfo(SciGameId gameId, ushort targetScript, ushort targetSize, ushort patchedByteOffset, byte patchedByte)
        {
            GameId = gameId;
            TargetScript = targetScript;
            TargetSize = targetSize;
            PatchedByteOffset = patchedByteOffset;
            PatchedByte = patchedByte;
        }
    }

    /// <summary>
    /// Supported languages
    /// </summary>
    internal enum Language
    {
        None = 0,
        English = 1,
        French = 33,
        Spanish = 34,
        Italian = 39,
        German = 49,
        Japanese = 81,
        Portuguese = 351
    }

    internal class SciEngine : Core.Engine
    {
        private readonly ADGameDescription _gameDescription;
        public SoundCommandParser _soundCmd;
        public GameFeatures _features;
        public opcode_format[][] _opcode_formats;

        public GfxAnimate _gfxAnimate; // Animate for 16-bit gfx
        public GfxCache _gfxCache;
        public GfxCompare _gfxCompare;
        public GfxControls16 _gfxControls16; // Controls for 16-bit gfx
        public GfxControls32 _gfxControls32; // Controls for 32-bit gfx
        public GfxCoordAdjuster _gfxCoordAdjuster;
        public GfxCursor _gfxCursor;
        public GfxMenu _gfxMenu; // Menu for 16-bit gfx
        public GfxPaint16 _gfxPaint16; // Painting in 16-bit gfx
        public GfxPaint32 _gfxPaint32; // Painting in 32-bit gfx
        public GfxPorts _gfxPorts; // Port managment for 16-bit gfx
        public GfxScreen _gfxScreen;
        public GfxText16 _gfxText16;
        public GfxText32 _gfxText32;
        private GfxTransitions _gfxTransitions; // transitions between screens for 16-bit gfx
        public GfxMacIconBar _gfxMacIconBar; // Mac Icon Bar manager

#if ENABLE_SCI32
        private Audio32 _audio32;
        private Video32 _video32;
        private RobotDecoder _robotDecoder;
        public GfxFrameout _gfxFrameout; // kFrameout and the like for 32-bit gfx
#endif

        public readonly DebugState _debugState;
        private readonly MacResManager _macExecutable = new MacResManager();

        // Maps half-width single-byte SJIS to full-width double-byte SJIS
        // Note: SSCI maps 0x5C (the Yen symbol) to 0x005C, which terminates
        // the string with the leading 0x00 byte. We map Yen to 0x818F.
        private static readonly ushort[] HalfWidthSjisMap =
        {
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

        private static readonly byte[] PatchGameRestoreSave =
        {
            0x39, 0x03, // pushi 03
            0x76, // push0
            0x38, 0xff, 0xff, // pushi -1
            0x76, // push0
            0x43, 0xff, 0x06, // callk kRestoreGame/kSaveGame (will get changed afterwards)
            0x48, // ret
        };

        // SCI2 version: Same as above, but the second parameter to callk is a word
        private static readonly byte[] PatchGameRestoreSaveSci2 =
        {
            0x39, 0x03, // pushi 03
            0x76, // push0
            0x38, 0xff, 0xff, // pushi -1
            0x76, // push0
            0x43, 0xff, 0x06, 0x00, // callk kRestoreGame/kSaveGame (will get changed afterwards)
            0x48, // ret
        };

        // SCI21 version: Same as above, but the second parameter to callk is a word
        private static readonly byte[] PatchGameRestoreSaveSci21 =
        {
            0x39, 0x04, // pushi 04
            0x76, // push0	// 0: save, 1: restore (will get changed afterwards)
            0x76, // push0
            0x38, 0xff, 0xff, // pushi -1
            0x76, // push0
            0x43, 0xff, 0x08, 0x00, // callk kSave (will get changed afterwards)
            0x48, // ret
        };

        private readonly FanMadePatchInfo[] _patchInfo = {
        // game        script    size  offset   byte
        // ** NRS Patches **************************
            new FanMadePatchInfo( SciGameId.HOYLE3,     994,   2580,    656,  0x78 ),
            new FanMadePatchInfo( SciGameId.KQ1,         85,   5156,    631,  0x02 ),
            new FanMadePatchInfo( SciGameId.LAURABOW2,  994,   4382,      0,  0x00 ),
            new FanMadePatchInfo( SciGameId.LONGBOW,    994,   4950,   1455,  0x78 ), // English
            new FanMadePatchInfo( SciGameId.LONGBOW,    994,   5020,   1469,  0x78 ), // German
            new FanMadePatchInfo( SciGameId.LSL1,       803,    592,    342,  0x01 ),
            new FanMadePatchInfo( SciGameId.LSL3,       380,   6148,    195,  0x35 ),
            new FanMadePatchInfo( SciGameId.LSL5,       994,   4810,   1342,  0x78 ), // English
            new FanMadePatchInfo( SciGameId.LSL5,       994,   4942,   1392,  0x76 ), // German
            new FanMadePatchInfo( SciGameId.PQ1,        994,   4332,   1473,  0x78 ),
            new FanMadePatchInfo( SciGameId.PQ2,        200,  10614,      0,  0x00 ),
            new FanMadePatchInfo( SciGameId.PQ3,        994,   4686,   1291,  0x78 ), // English
            new FanMadePatchInfo( SciGameId.PQ3,        994,   4734,   1283,  0x78 ), // German
            new FanMadePatchInfo( SciGameId.QFG1VGA,    994,   4388,      0,  0x00 ),
            new FanMadePatchInfo( SciGameId.QFG3,       994,   4714,      2,  0x48 ),
            // TODO: Disabled, as it fixes a whole lot of bugs which can't be tested till SCI2.1 support is finished
            //new FanMadePatchInfo( SciGameId.QFG4,       710,  11477,      0,  0x00 ),
            new FanMadePatchInfo( SciGameId.SQ1,        994,   4740,      0,  0x00 ),
            new FanMadePatchInfo( SciGameId.SQ5,        994,   4142,   1496,  0x78 ), // English/German/French
            // TODO: Disabled, till we can test the Italian version
            //new FanMadePatchInfo( SciGameId.SQ5,        994,   4148,      0,  0x00 ),   // Italian - patched file is the same size as the original
            // TODO: The bugs in SQ6 can't be tested till SCI2.1 support is finished
            //new FanMadePatchInfo( SciGameId.SQ6,        380,  16308,  15042,  0x0C ),   // English
            //new FanMadePatchInfo( SciGameId.SQ6,        380,  11652,      0,  0x00 ),   // German - patched file is the same size as the original
            // ** End marker ***************************
            new FanMadePatchInfo( SciGameId.FANMADE,      0,      0,      0,  0x00 )
        };

        public uint TickCount => (uint)(TotalPlayTime * 60 / 1000);

        public MacResManager MacExecutable => _macExecutable;

        public SciEngine(ISystem system, GameSettings settings, SciGameDescriptor desc, SciGameId id)
            : base(system, settings)
        {
            System = system;
            Instance = this;
            Rng = new RandomSource("sci");
            GameId = id;
            _gameDescription = desc.GameDescription;
            Directory = ServiceLocator.FileStorage.GetDirectoryName(desc.Path);
            _debugState = new DebugState();

            // Set up the engine specific debug levels
            DebugManager.Instance.AddDebugChannel(DebugLevels.Error, "Error", "Script error debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Nodes, "Lists", "Lists and nodes debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Graphics, "Graphics", "Graphics debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Strings, "Strings", "Strings debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Memory, "Memory", "Memory debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.FuncCheck, "Func", "Function parameter debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Bresen, "Bresenham", "Bresenham algorithms debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Sound, "Sound", "Sound debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.BaseSetter, "Base", "Base Setter debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Parser, "Parser", "Parser debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Said, "Said", "Said specs debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.File, "File", "File I/O debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Time, "Time", "Time debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Room, "Room", "Room number debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.AvoidPath, "Pathfinding", "Pathfinding debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.DclInflate, "DCL", "DCL inflate debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.VM, "VM", "VM debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Scripts, "Scripts", "Notifies when scripts are unloaded");
            DebugManager.Instance.AddDebugChannel(DebugLevels.ScriptPatcher, "ScriptPatcher", "Notifies when scripts are patched");
            DebugManager.Instance.AddDebugChannel(DebugLevels.Workarounds, "Workarounds", "Notifies when workarounds are triggered");
            DebugManager.Instance.AddDebugChannel(DebugLevels.GC, "GC", "Garbage Collector debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.ResMan, "ResMan", "Resource manager debugging");
            DebugManager.Instance.AddDebugChannel(DebugLevels.OnStartup, "OnStartup", "Enter debugger at start of game");
            DebugManager.Instance.AddDebugChannel(DebugLevels.DebugMode, "DebugMode", "Enable game debug mode at start of game");
        }

        public string GetSavegameName(int nr)
        {
            return $"{_gameDescription.gameid}.{nr:D3}";
        }

        public string UnwrapFilename(string name)
        {
            string prefix = FilePrefix + "-";
            if (name.StartsWith(prefix))
                return name.Substring(0, prefix.Length);
            return name;
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
            Language subtitleLanguage = Sci.Language.None;

            if (Selector(s => s.subtitleLang) != -1)
                subtitleLanguage =
                    (Language)ReadSelectorValue(EngineState._segMan, GameObject, s => s.subtitleLang);

            Language foundLanguage;
            string retval = GetSciLanguageString(str, activeLanguage, out foundLanguage, out languageSplitter);

            // Don't add subtitle when separator is not set, subtitle language is not set, or
            // string contains only one language
            if ((sep == null) || (subtitleLanguage == Sci.Language.None) || (foundLanguage == Sci.Language.None))
                return retval;

            // Add subtitle, unless the subtitle language doesn't match the languages in the string
            if ((subtitleLanguage == Sci.Language.English) || (subtitleLanguage == foundLanguage))
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

        private static string GetSciLanguageString(string str, Language requestedLanguage,
            out Language secondaryLanguage,
            out ushort languageSplitter)
        {
            secondaryLanguage = Sci.Language.None;
            languageSplitter = 0;

            var textPtr = 0;
            Language foundLanguage = Sci.Language.None;
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

                    if (foundLanguage != Sci.Language.None)
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

                        mappedChar = HalfWidthSjisMap[curChar];
                        if (mappedChar != 0)
                        {
                            fullWidth += mappedChar >> 8;
                            fullWidth += mappedChar & 0xFF;
                        }
                        else
                        {
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
                return str.Substring(textPtr + 2);
            }

            if (curChar != 0)
                return str.Substring(0, textPtr);

            return str;
        }

        public void Sleep(int msecs)
        {
            int wakeupTime = ServiceLocator.Platform.GetMilliseconds() + msecs;

            while (true)
            {
                // let backend process events and update the screen
                EventManager.GetSciEvent(SciEvent.SCI_EVENT_PEEK);
                var time = ServiceLocator.Platform.GetMilliseconds();
                if (time + 10 < wakeupTime)
                {
                    ServiceLocator.Platform.Sleep(10);
                }
                else
                {
                    if (time < wakeupTime)
                        ServiceLocator.Platform.Sleep(wakeupTime - time);
                    break;
                }
            }
        }

        private static Language CharToLanguage(char c)
        {
            switch (c)
            {
                case 'F':
                    return Sci.Language.French;
                case 'S':
                    return Sci.Language.Spanish;
                case 'I':
                    return Sci.Language.Italian;
                case 'G':
                    return Sci.Language.German;
                case 'J':
                case 'j':
                    return Sci.Language.Japanese;
                case 'P':
                    return Sci.Language.Portuguese;
                default:
                    return Sci.Language.None;
            }
        }

        public new static SciEngine Instance { get; private set; }

        private string FilePrefix => _gameDescription.gameid;

        public Core.Language Language => _gameDescription.language;

        public Platform Platform => _gameDescription.platform;

        public SciGameId GameId { get; }

        public EngineState EngineState { get; private set; }

        public Register GameObject { get; private set; }

        public GameFeatures Features => _features;

        public int InQfGImportRoom
        {
            get
            {
                if (GameId == SciGameId.QFG2 && EngineState.CurrentRoomNumber == 805)
                {
                    // QFG2 character import screen
                    return 2;
                }
                if (GameId == SciGameId.QFG3 && EngineState.CurrentRoomNumber == 54)
                {
                    // QFG3 character import screen
                    return 3;
                }
                if (GameId == SciGameId.QFG4 && EngineState.CurrentRoomNumber == 54)
                {
                    return 4;
                }
                return 0;
            }
        }

        public bool HasMacIconBar => ResMan.IsSci11Mac && ResourceManager.GetSciVersion() == SciVersion.V1_1 &&
                                     (GameId == SciGameId.KQ6 || GameId == SciGameId.FREDDYPHARKAS);

        public bool IsDemo => _gameDescription.flags.HasFlag(ADGameFlags.DEMO);

        public bool IsCd => _gameDescription.flags.HasFlag(ADGameFlags.CD);

        public bool IsBe
            => _gameDescription.platform == Platform.Amiga || _gameDescription.platform == Platform.Macintosh;

        public RandomSource Rng { get; }

        public ResourceManager ResMan { get; private set; }

        public Kernel Kernel { get; private set; }

        public ISaveFileManager SaveFileManager => System.SaveFileManager;

        public string Directory { get; }

        public EventManager EventManager { get; private set; }

        public ISystem System { get; }

        public bool ShouldQuit { get; internal set; }

        public Vocabulary Vocabulary { get; private set; }

        public string SavegamePattern => _gameDescription.gameid + ".???";

        public ScriptPatcher ScriptPatcher { get; private set; }

        public Language GetSciLanguage()
        {
            Language lang = (Language)ResMan.GetAudioLanguage();
            if (lang != Sci.Language.None)
                return lang;

            if (Selector(o => o.printLang) == -1) return Sci.Language.English;

            lang = (Language)ReadSelectorValue(EngineState._segMan, GameObject, o => o.printLang);

            if ((ResourceManager.GetSciVersion() < SciVersion.V1_1) && (lang != Sci.Language.None)) return lang;

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
                case Core.Language.FR_FRA:
                    lang = Sci.Language.French;
                    break;
                case Core.Language.ES_ESP:
                    lang = Sci.Language.Spanish;
                    break;
                case Core.Language.IT_ITA:
                    lang = Sci.Language.Italian;
                    break;
                case Core.Language.DE_DEU:
                    lang = Sci.Language.German;
                    break;
                case Core.Language.JA_JPN:
                    lang = Sci.Language.Japanese;
                    break;
                case Core.Language.PT_BRA:
                    lang = Sci.Language.Portuguese;
                    break;
                default:
                    lang = Sci.Language.English;
                    break;
            }

            return lang;
        }

        public void SetSciLanguage(Language lang)
        {
            if (Selector(o => o.printLang) != -1)
                WriteSelectorValue(EngineState._segMan, GameObject, o => o.printLang, (ushort)lang);
        }

        public string WrapFilename(string name)
        {
            return FilePrefix + "-" + name;
        }

        public override void Run()
        {
            ResMan = new ResourceManager(Directory);
            ResMan.AddAppropriateSources();
            ResMan.Init();

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
            ResMan.AddNewGMPatch(GameId);
            GameObject = ResMan.FindGameObject();

            ScriptPatcher = new ScriptPatcher();
            SegManager segMan = new SegManager(ResMan, ScriptPatcher);

            // Initialize the game screen
            _gfxScreen = new GfxScreen(ResMan);
            _gfxScreen.IsUnditheringEnabled = ConfigManager.Instance.Get<bool>("disable_dithering");

            Kernel = new Kernel(ResMan, segMan);
            Kernel.Init();

            _features = new GameFeatures(segMan, Kernel);

            // Only SCI0, SCI01 and SCI1 EGA games used a parser
            Vocabulary = ResourceManager.GetSciVersion() <= SciVersion.V1_EGA_ONLY
                ? new Vocabulary(ResMan, false)
                : null;
            // Also, XMAS1990 apparently had a parser too. Refer to http://forums.scummvm.org/viewtopic.php?t=9135
            if (GameId == SciGameId.CHRISTMAS1990)
                Vocabulary = new Vocabulary(ResMan, false);
            _audio = new AudioPlayer(ResMan);
            EngineState = new EngineState(segMan);
            EventManager = new EventManager(ResMan.DetectFontExtended());

            // TODO: Create debugger console. It requires GFX and _gamestate to be initialized
            //_console = new Console(this);

            // The game needs to be initialized before the graphics system is initialized, as
            // the graphics code checks parts of the seg manager upon initialization (e.g. for
            // the presence of the fastCast object)
            if (!InitGame())
            {
                /* Initialize */
                Warning("Game initialization failed: Aborting...");
                // TODO: Add an "init failed" error?
                //return Common::kUnknownError;
                return;
            }

            // we try to find the super class address of the game object, we can't do that earlier
            SciObject gameObject = segMan.GetObject(GameObject);
            if (gameObject == null)
            {
                Warning("Could not get game object, aborting...");
                return;
            }

            script_adjust_opcode_formats();

            // Must be called after game_init(), as they use _features
            Kernel.LoadKernelNames(_features);
            _soundCmd = new SoundCommandParser(ResMan, segMan, _audio, _features.DetectDoSoundType());

            SyncSoundSettings();
            _soundCmd.SetMasterVolume(11);
            SyncIngameAudioOptions();

            // Load our Mac executable here for icon bar palettes and high-res fonts
            // TODO: LoadMacExecutable();

            // Initialize all graphics related subsystems
            InitGraphics();

            // Patch in our save/restore code, so that dialogs are replaced
            PatchGameSaveRestore();
            SetLauncherLanguage();

            // Check whether loading a savestate was requested
            int directSaveSlotLoading = ConfigManager.Instance.Get<int>("save_slot");
            if (directSaveSlotLoading >= 0)
            {
                // call GameObject::play (like normally)
                InitStackBaseWithSelector(Selector(o => o.play));
                // We set this, so that the game automatically quit right after init
                EngineState.variables[Vm.VAR_GLOBAL][4] = Register.TRUE_REG;

                // Jones only initializes its menus when restarting/restoring, thus set
                // the gameIsRestarting flag here before initializing. Fixes bug #6536.
                if (Instance.GameId == SciGameId.JONES)
                    EngineState.gameIsRestarting = GameIsRestarting.RESTORE;

                EngineState._executionStackPosChanged = false;
                Run();

                // As soon as we get control again, actually restore the game
                Register[] restoreArgv = { Register.NULL_REG, Register.Make(0, (ushort)directSaveSlotLoading) };    // special call (argv[0] is NULL)
                Kernel.kRestoreGame(EngineState, 2, new StackPtr(restoreArgv, 0));

                // this indirectly calls GameObject::init, which will setup menu, text font/color codes etc.
                //  without this games would be pretty badly broken
            }

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
                var buggyScript = ResMan.FindResource(new ResourceId(ResourceType.Script, 180), false);

                if (buggyScript != null && (buggyScript.size == 12354 || buggyScript.size == 12362))
                {
                    ShowScummVMDialog("A known buggy game script has been detected, which could " +
                    "prevent you from progressing later on in the game, during " +
                    "the sequence with the Green Man's riddles. Please, apply " +
                    "the latest patch for this game by Sierra to avoid possible " +
                    "problems");
                }
            }

            // Show a warning if the user has selected a General MIDI device, no GM patch exists
            // (i.e. patch 4) and the game is one of the known 8 SCI1 games that Sierra has provided
            // after market patches for in their "General MIDI Utility".
            if (_soundCmd.MusicType == Core.Audio.MusicType.GeneralMidi && !ConfigManager.Instance.Get<bool>("native_mt32"))
            {
                if (ResMan.FindResource(new ResourceId(ResourceType.Patch, 4), false) == null)
                {
                    switch (GameId)
                    {
                        case SciGameId.ECOQUEST:
                        case SciGameId.HOYLE3:
                        case SciGameId.LSL1:
                        case SciGameId.LSL5:
                        case SciGameId.LONGBOW:
                        case SciGameId.SQ1:
                        case SciGameId.SQ4:
                        case SciGameId.FAIRYTALES:
                            ShowScummVMDialog("You have selected General MIDI as a sound device. Sierra " +
                                              "has provided after-market support for General MIDI for this " +
                                              "game in their \"General MIDI Utility\". Please, apply this " +
                                              "patch in order to enjoy MIDI music with this game. Once you " +
                                              "have obtained it, you can unpack all of the included *.PAT " +
                                              "files in your ScummVM extras folder and ScummVM will add the " +
                                              "appropriate patch automatically. Alternatively, you can follow " +
                                              "the instructions in the READ.ME file included in the patch and " +
                                              "rename the associated *.PAT file to 4.PAT and place it in the " +
                                              "game folder. Without this patch, General MIDI music for this " +
                                              "game will sound badly distorted.");
                            break;
                    }
                }
            }

            if (GameHasFanMadePatch())
            {
                ShowScummVMDialog("Your game is patched with a fan made script patch. Such patches have " +
                                  "been reported to cause issues, as they modify game scripts extensively. " +
                                  "The issues that these patches fix do not occur in ScummVM, so you are " +
                                  "advised to remove this patch from your game folder in order to avoid " +
                                  "having unexpected errors and/or issues later on.");
            }

            RunGame();

            // TODO: ConfMan.flushToDisk();
        }

        private bool GameHasFanMadePatch()
        {
            int curEntry = 0;

            while (true)
            {
                if (_patchInfo[curEntry].TargetSize == 0)
                    break;

                if (_patchInfo[curEntry].GameId == GameId)
                {
                    var targetScript = ResMan.FindResource(new ResourceId(ResourceType.Script, _patchInfo[curEntry].TargetScript), false);

                    if (targetScript != null && targetScript.size + 2 == _patchInfo[curEntry].TargetSize)
                    {
                        if (_patchInfo[curEntry].PatchedByteOffset == 0)
                            return true;
                        if (targetScript.data[_patchInfo[curEntry].PatchedByteOffset - 2] == _patchInfo[curEntry].PatchedByte)
                            return true;
                    }
                }

                curEntry++;
            }

            return false;
        }

        private void ShowScummVMDialog(string message)
        {
            throw new NotImplementedException();
        }

        private void PatchGameSaveRestore()
        {
            SegManager segMan = EngineState._segMan;
            var gameObject = segMan.GetObject(GameObject);
            var gameSuperObject = segMan.GetObject(gameObject.SuperClassSelector);
            if (gameSuperObject == null)
                gameSuperObject = gameObject; // happens in KQ5CD, when loading saved games before r54510
            byte kernelIdRestore = 0;
            byte kernelIdSave = 0;

            switch (GameId)
            {
                case SciGameId.HOYLE1: // gets confused, although the game doesnt support saving/restoring at all
                case SciGameId.HOYLE2: // gets confused, see hoyle1
                case SciGameId.JONES:
                // gets confused, when we patch us in, the game is only able to save to 1 slot, so hooking is not required
                case SciGameId.MOTHERGOOSE: // mother goose EGA saves/restores directly and has no save/restore dialogs
                case SciGameId.MOTHERGOOSE256: // mother goose saves/restores directly and has no save/restore dialogs
                case SciGameId.PHANTASMAGORIA: // has custom save/load code
                case SciGameId.SHIVERS: // has custom save/load code
                    return;
            }

            if (ConfigManager.Instance.Get<bool>("originalsaveload"))
                return;

            ushort kernelNamesSize = (ushort)Kernel.KernelNamesSize;
            for (ushort kernelNr = 0; kernelNr < kernelNamesSize; kernelNr++)
            {
                string kernelName = Kernel.GetKernelName(kernelNr);
                if (kernelName == "RestoreGame")
                    kernelIdRestore = (byte)kernelNr;
                if (kernelName == "SaveGame")
                    kernelIdSave = (byte)kernelNr;
                if (kernelName == "Save")
                    kernelIdSave = kernelIdRestore = (byte)kernelNr;
            }

            // Search for gameobject superclass ::restore
            ushort gameSuperObjectMethodCount = (ushort)gameSuperObject.MethodCount;
            for (ushort methodNr = 0; methodNr < gameSuperObjectMethodCount; methodNr++)
            {
                ushort selectorId = (ushort)gameSuperObject.GetFuncSelector(methodNr);
                string methodName = Kernel.GetSelectorName(selectorId);
                if (methodName == "restore")
                {
                    if (kernelIdSave != kernelIdRestore)
                        PatchGameSaveRestoreCode(segMan, gameSuperObject.GetFunction(methodNr), kernelIdRestore);
                    else
                        PatchGameSaveRestoreCodeSci21(segMan, gameSuperObject.GetFunction(methodNr), kernelIdRestore,
                            true);
                }
                else if (methodName == "save")
                {
                    if (GameId != SciGameId.FAIRYTALES)
                    {
                        // Fairy Tales saves automatically without a dialog
                        if (kernelIdSave != kernelIdRestore)
                            PatchGameSaveRestoreCode(segMan, gameSuperObject.GetFunction(methodNr), kernelIdSave);
                        else
                            PatchGameSaveRestoreCodeSci21(segMan, gameSuperObject.GetFunction(methodNr), kernelIdSave,
                                false);
                    }
                }
            }

            // Search for gameobject ::save, if there is one patch that one too
            ushort gameObjectMethodCount = (ushort)gameObject.MethodCount;
            for (ushort methodNr = 0; methodNr < gameObjectMethodCount; methodNr++)
            {
                ushort selectorId = (ushort)gameObject.GetFuncSelector(methodNr);
                string methodName = Kernel.GetSelectorName(selectorId);
                if (methodName == "save")
                {
                    if (GameId != SciGameId.FAIRYTALES)
                    {
                        // Fairy Tales saves automatically without a dialog
                        if (kernelIdSave != kernelIdRestore)
                            PatchGameSaveRestoreCode(segMan, gameObject.GetFunction(methodNr), kernelIdSave);
                        else
                            PatchGameSaveRestoreCodeSci21(segMan, gameObject.GetFunction(methodNr), kernelIdSave, false);
                    }
                    break;
                }
            }
        }

        private static void PatchGameSaveRestoreCode(SegManager segMan, Register methodAddress, byte id)
        {
            Script script = segMan.GetScript(methodAddress.Segment);
            var patchPtr = script.GetBuf((int)methodAddress.Offset);

            if (ResourceManager.GetSciVersion() <= SciVersion.V1_1)
            {
                Array.Copy(PatchGameRestoreSave, 0, patchPtr.Data, patchPtr.Offset, PatchGameRestoreSave.Length);
            }
            else
            {
                // SCI2+
                Array.Copy(PatchGameRestoreSaveSci2, 0, patchPtr.Data, patchPtr.Offset, PatchGameRestoreSaveSci2.Length);

                if (Instance.IsBe)
                {
                    // LE . BE
                    patchPtr[9] = 0x00;
                    patchPtr[10] = 0x06;
                }
            }

            patchPtr[8] = id;
        }

        private static void PatchGameSaveRestoreCodeSci21(SegManager segMan, Register methodAddress, byte id,
            bool doRestore)
        {
            Script script = segMan.GetScript(methodAddress.Segment);
            var patchPtr = script.GetBuf((int)methodAddress.Offset);
            Array.Copy(PatchGameRestoreSaveSci21, 0, patchPtr.Data, patchPtr.Offset, PatchGameRestoreSaveSci21.Length);

            if (doRestore)
                patchPtr[2] = 0x78; // push1

            if (Instance.IsBe)
            {
                // LE . BE
                patchPtr[10] = 0x00;
                patchPtr[11] = 0x08;
            }

            patchPtr[9] = id;
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
            Language subtitleLanguage = Sci.Language.None;

            if (Selector(s => s.subtitleLang) != -1)
                subtitleLanguage =
                    (Language)ReadSelectorValue(EngineState._segMan, GameObject, s => s.subtitleLang);

            Language foundLanguage;
            string retval = GetSciLanguageString(str, activeLanguage, out foundLanguage, out languageSplitter);

            // Don't add subtitle when separator is not set, subtitle language is not set, or
            // string contains only one language
            if ((sep == null) || (subtitleLanguage == Sci.Language.None) || (foundLanguage == Sci.Language.None))
                return retval;

            // Add subtitle, unless the subtitle language doesn't match the languages in the string
            if ((subtitleLanguage == Sci.Language.English) || (subtitleLanguage == foundLanguage))
            {
                retval += sep;
                retval += GetSciLanguageString(str, subtitleLanguage);
            }

            return retval;
        }

        private void RunGame()
        {
            TotalPlayTime = 0;

            InitStackBaseWithSelector(Selector(s => s.play)); // Call the play selector

            // TODO: Attach the debug console on game startup, if requested
            //if (DebugMan.isDebugChannelEnabled(kDebugLevelOnStartup))
            //    _console.attach();

            EngineState._syncedAudioOptions = false;

            do
            {
                EngineState._executionStackPosChanged = false;
                Vm.Run(EngineState);
                ExitGame();

                EngineState._syncedAudioOptions = true;

                if (EngineState.abortScriptProcessing == AbortGameState.RestartGame)
                {
                    EngineState._segMan.ResetSegMan();
                    InitGame();
                    InitStackBaseWithSelector(Selector(s => s.play));
                    // TODO: PatchGameSaveRestore();
                    SetLauncherLanguage();
                    EngineState.gameIsRestarting = GameIsRestarting.RESTART;
                    EngineState._throttleLastTime = 0;
                    if (_gfxMenu != null)
                        _gfxMenu.Reset();
                    EngineState.abortScriptProcessing = AbortGameState.None;
                    EngineState._syncedAudioOptions = false;
                }
                else if (EngineState.abortScriptProcessing == AbortGameState.LoadGame)
                {
                    EngineState.abortScriptProcessing = AbortGameState.None;
                    EngineState._executionStack.Clear();
                    InitStackBaseWithSelector(Selector(s => s.replay));
                    // TODO: PatchGameSaveRestore();
                    SetLauncherLanguage();
                    EngineState.ShrinkStackToBase();
                    EngineState.abortScriptProcessing = AbortGameState.None;

                    SyncSoundSettings();
                    SyncIngameAudioOptions();
                    // Games do not set their audio settings when loading
                }
                else
                {
                    break; // exit loop
                }
            } while (true);
        }

        private void ExitGame()
        {
            if (EngineState.abortScriptProcessing != AbortGameState.LoadGame)
            {
                EngineState._executionStack.Clear();
                _audio.StopAllAudio();
                _soundCmd.ClearPlayList();
            }

            // TODO Free parser segment here

            // TODO Free scripts here

            // Close all opened file handles
            Array.Resize(ref EngineState._fileHandles, 5);
        }

        private void InitStackBaseWithSelector(int selector)
        {
            EngineState.stack_base[0] = Register.Make(0, (ushort)selector);
            EngineState.stack_base[1] = Register.NULL_REG;

            // Register the first element on the execution stack
            if (
                Vm.SendSelector(EngineState, GameObject, GameObject, EngineState.stack_base, 2,
                    EngineState.stack_base) == null)
            {
                // TODO: _console.printObject(_gameObjectAddress);
                // error("initStackBaseWithSelector: error while registering the first selector in the call stack");
            }
        }

        private void SetLauncherLanguage()
        {
            if (!_gameDescription.flags.HasFlag(ADGameFlags.ADDENGLISH)) return;

            if (LanguageHelper.ParseLanguage(ConfigManager.Instance.Get<string>("language")) != Core.Language.EN_ANY)
                return;

            // If game is multilingual and English was selected as language
            if (Selector(o => o.printLang) != -1) // set text language to English
                WriteSelectorValue(EngineState._segMan, GameObject, o => o.printLang,
                    (ushort)Sci.Language.English);
            if (Selector(o => o.parseLang) != -1) // and set parser language to English as well
                WriteSelectorValue(EngineState._segMan, GameObject, o => o.parseLang,
                    (ushort)Sci.Language.English);
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
            _gfxPaint16 = null;
            _gfxPorts = null;
            _gfxText16 = null;
            _gfxTransitions = null;
# if ENABLE_SCI32
            _gfxControls32 = null;
            _gfxText32 = null;
            _robotDecoder = null;
            _gfxFrameout = null;
            _gfxPaint32 = null;
            _gfxPalette32 = null;
            _gfxRemap32 = null;
#endif
            if (HasMacIconBar)
                _gfxMacIconBar = new GfxMacIconBar();

#if ENABLE_SCI32
            if (ResourceManager.GetSciVersion() >= SciVersion.V2)
            {
                _gfxPalette32 = new GfxPalette32(ResMan);
                _gfxRemap32 = new GfxRemap32();
            }
            else
            {
#endif
                _gfxPalette16 = new GfxPalette(ResMan, _gfxScreen);
                if (GameId == SciGameId.QFG4DEMO)
                    _gfxRemap16 = new GfxRemap(_gfxPalette16);
#if ENABLE_SCI32
            }
#endif

            _gfxCache = new GfxCache(ResMan, _gfxScreen, _gfxPalette16);
            _gfxCursor = new GfxCursor(ResMan, _gfxPalette16, _gfxScreen);

#if ENABLE_SCI32
            if (ResourceManager.GetSciVersion() >= SciVersion.V2)
            {
                // SCI32 graphic objects creation
                _gfxCoordAdjuster = new GfxCoordAdjuster32(EngineState._segMan);
                _gfxCursor.Init(_gfxCoordAdjuster, EventManager);
                _gfxCompare = new GfxCompare(EngineState._segMan, _gfxCache, _gfxScreen, _gfxCoordAdjuster);
                _gfxPaint32 = new GfxPaint32(EngineState._segMan);
                _robotDecoder = new RobotDecoder(Platform == Platform.Macintosh);
                _gfxFrameout = new GfxFrameout(EngineState._segMan, ResMan, _gfxCoordAdjuster, _gfxScreen,_gfxPalette32);
                _gfxText32 = new GfxText32(EngineState._segMan, _gfxCache);
                _gfxControls32 = new GfxControls32(EngineState._segMan, _gfxCache, _gfxText32);
                _gfxFrameout.Run();
            }
            else {
#endif
            // SCI0-SCI1.1 graphic objects creation
            _gfxPorts = new GfxPorts(EngineState._segMan, _gfxScreen);
            _gfxCoordAdjuster = new GfxCoordAdjuster16(_gfxPorts);
            _gfxCursor.Init(_gfxCoordAdjuster, EventManager);
            _gfxCompare = new GfxCompare(EngineState._segMan, _gfxCache, _gfxScreen, _gfxCoordAdjuster);
            _gfxTransitions = new GfxTransitions(_gfxScreen, _gfxPalette16);
            _gfxPaint16 = new GfxPaint16(ResMan, EngineState._segMan, _gfxCache, _gfxPorts, _gfxCoordAdjuster,
                _gfxScreen, _gfxPalette16, _gfxTransitions, _audio);
            _gfxAnimate = new GfxAnimate(EngineState, _gfxCache, _gfxPorts, _gfxPaint16, _gfxScreen, _gfxPalette16,
                _gfxCursor, _gfxTransitions);
            _gfxText16 = new GfxText16(_gfxCache, _gfxPorts, _gfxPaint16, _gfxScreen);
            _gfxControls16 = new GfxControls16(EngineState._segMan, _gfxPorts, _gfxPaint16, _gfxText16, _gfxScreen);
            _gfxMenu = new GfxMenu(EventManager, EngineState._segMan, _gfxPorts, _gfxPaint16, _gfxText16, _gfxScreen,
                _gfxCursor);

            _gfxMenu.Reset();

            _gfxPorts.Init(_features.UsesOldGfxFunctions(), _gfxPaint16, _gfxText16);
            _gfxPaint16.Init(_gfxAnimate, _gfxText16);

# if ENABLE_SCI32
            }
#endif

            // Set default (EGA, amiga or resource 999) palette
            _gfxPalette16.SetDefault();
        }

        private void script_adjust_opcode_formats()
        {
            Instance._opcode_formats = new opcode_format[128][];
            //memcpy(Instance._opcode_formats, g_base_opcode_formats, 128 * 4 * sizeof(opcode_format));
            for (int i = 0; i < 128; i++)
            {
                Instance._opcode_formats[i] = new opcode_format[4];
                Array.Copy(BaseOpcodeFormats[i], Instance._opcode_formats[i], BaseOpcodeFormats[i].Length);
            }

            if (Instance._features.DetectLofsType() != SciVersion.V0_EARLY)
            {
                Instance._opcode_formats[Vm.op_lofsa][0] = opcode_format.Script_Offset;
                Instance._opcode_formats[Vm.op_lofss][0] = opcode_format.Script_Offset;
            }

# if ENABLE_SCI32
// In SCI32, some arguments are now words instead of bytes
            if (ResourceManager.GetSciVersion() >= SciVersion.V2)
            {
                Instance._opcode_formats[Vm.op_calle][2] = opcode_format.Script_Word;
                Instance._opcode_formats[Vm.op_callk][1] = opcode_format.Script_Word;
                Instance._opcode_formats[Vm.op_super][1] = opcode_format.Script_Word;
                Instance._opcode_formats[Vm.op_send][0] = opcode_format.Script_Word;
                Instance._opcode_formats[Vm.op_self][0] = opcode_format.Script_Word;
                Instance._opcode_formats[Vm.op_call][1] = opcode_format.Script_Word;
                Instance._opcode_formats[Vm.op_callb][1] = opcode_format.Script_Word;
            }

            if (ResourceManager.GetSciVersion() >= SciVersion.V3)
            {
                // TODO: There are also opcodes in
                // here to get the superclass, and possibly the species too.
                Instance._opcode_formats[0x4d / 2][0] = opcode_format.Script_None;
                Instance._opcode_formats[0x4e / 2][0] = opcode_format.Script_None;
            }
#endif
        }

        private bool InitGame()
        {
            // Script 0 needs to be allocated here before anything else!
            int script0Segment = EngineState._segMan.GetScriptSegment(0, ScriptLoadType.LOCK);
            ushort segid;
            DataStack stack = EngineState._segMan.AllocateStack(Vm.STACK_SIZE, out segid);

            EngineState._msgState = new MessageState(EngineState._segMan);
            EngineState.gcCountDown = Vm.GC_INTERVAL - 1;

            // Script 0 should always be at segment 1
            if (script0Segment != 1)
            {
                Debug(2, "Failed to instantiate script 0");
                return false;
            }

            EngineState.InitGlobals();
            EngineState._segMan.InitSysStrings();

            EngineState.r_acc = Register.NULL_REG;
            EngineState.r_prev = Register.NULL_REG;

            EngineState._executionStack.Clear(); // Start without any execution stack
            EngineState.executionStackBase = -1; // No vm is running yet
            EngineState._executionStackPosChanged = false;

            EngineState.abortScriptProcessing = AbortGameState.None;
            EngineState.gameIsRestarting = GameIsRestarting.NONE;

            EngineState.stack_base = new StackPtr(stack._entries, 0);
            EngineState.stack_top = new StackPtr(stack._entries, stack._capacity);

            if (EngineState._segMan.InstantiateScript(0) == 0)
            {
                throw new InvalidOperationException("initGame(): Could not instantiate script 0");
            }

            // Reset parser
            Vocabulary?.Reset();

            EngineState.lastWaitTime = EngineState._screenUpdateTime = ServiceLocator.Platform.GetMilliseconds();

            // Load game language into printLang property of game object
            SetSciLanguage();

            return true;
        }

        private void SetSciLanguage()
        {
            SetSciLanguage(GetSciLanguage());
        }

        public void SyncIngameAudioOptions()
        {
            bool useGlobal90 = false;

            // Sync the in-game speech/subtitles settings for SCI1.1 CD games
            if (IsCd)
            {
                switch (ResourceManager.GetSciVersion())
                {
                    case SciVersion.V1_1:
                        // All SCI1.1 CD games use global 90
                        useGlobal90 = true;
                        break;
#if ENABLE_SCI32
                    case SciVersion.V2:
                    case SciVersion.V2_1_EARLY:
                    case SciVersion.V2_1_MIDDLE:
                    case SciVersion.V2_1_LATE:
                        // Only use global 90 for some specific games, not all SCI32 games used this method
                        switch (GameId)
                        {
                            case SciGameId.KQ7: // SCI2.1
                            case SciGameId.GK1: // SCI2
                            case SciGameId.GK2: // SCI2.1
                            case SciGameId.SQ6: // SCI2.1
                            case SciGameId.TORIN: // SCI2.1
                            case SciGameId.QFG4: // SCI2.1
                                useGlobal90 = true;
                                break;
                            case SciGameId.LSL6: // SCI2.1
                                // TODO: Uses gameFlags array
                                break;
                            // TODO: Unknown at the moment:
                            // Shivers - seems not to use global 90
                            // Police Quest: SWAT - unable to check
                            // Police Quest 4 - unable to check
                            // Mixed Up Mother Goose - unable to check
                            // Phantasmagoria - seems to use global 90, unable to check for subtitles atm
                            default:
                                return;
                        }
                        break;
#endif // ENABLE_SCI32
                    default:
                        return;
                }

                bool subtitlesOn = ConfigManager.Instance.Get<bool>("subtitles");
                bool speechOn = !ConfigManager.Instance.Get<bool>("speech_mute");

                if (!useGlobal90) return;

                if (subtitlesOn && !speechOn)
                {
                    EngineState.variables[Vm.VAR_GLOBAL][90] = Register.Make(0, 1); // subtitles
                }
                else if (!subtitlesOn && speechOn)
                {
                    EngineState.variables[Vm.VAR_GLOBAL][90] = Register.Make(0, 2); // speech
                }
                else if (subtitlesOn && speechOn)
                {
                    // Is it a game that supports simultaneous speech and subtitles?
                    switch (GameId)
                    {
                        case SciGameId.SQ4:
                        case SciGameId.FREDDYPHARKAS:
                        case SciGameId.ECOQUEST:
                        case SciGameId.LSL6:
                        case SciGameId.LAURABOW2:
                        case SciGameId.KQ6:
#if ENABLE_SCI32
// Unsure about Gabriel Knight 2
                            case SciGameId.KQ7: // SCI2.1
                            case SciGameId.GK1: // SCI2
                            case SciGameId.SQ6: // SCI2.1, SQ6 seems to always use subtitles anyway
                            case SciGameId.TORIN: // SCI2.1
                            case SciGameId.QFG4: // SCI2.1
#endif // ENABLE_SCI32
                            EngineState.variables[Vm.VAR_GLOBAL][90] = Register.Make(0, 3); // speech + subtitles
                            break;
                        default:
                            // Game does not support speech and subtitles, set it to speech
                            EngineState.variables[Vm.VAR_GLOBAL][90] = Register.Make(0, 2); // speech
                            break;
                    }
                }
            }
        }

        public void UpdateScummVmAudioOptions()
        {
            // Update ScummVM's speech/subtitles settings for SCI1.1 CD games,
            // depending on the in-game settings
            if (!IsCd || ResourceManager.GetSciVersion() != SciVersion.V1_1) return;
            ushort ingameSetting = (ushort)EngineState.variables[Vm.VAR_GLOBAL][90].Offset;

            switch (ingameSetting)
            {
                case 1:
                    // subtitles
                    ConfigManager.Instance.Set<bool>("subtitles", true);
                    ConfigManager.Instance.Set<bool>("speech_mute", true);
                    break;
                case 2:
                    // speech
                    ConfigManager.Instance.Set<bool>("subtitles", false);
                    ConfigManager.Instance.Set<bool>("speech_mute", false);
                    break;
                case 3:
                    // speech + subtitles
                    ConfigManager.Instance.Set<bool>("subtitles", true);
                    ConfigManager.Instance.Set<bool>("speech_mute", false);
                    break;
            }
        }

        public static int Selector(Func<SelectorCache, int> func)
        {
            return func(Instance.Kernel._selectorCache);
        }

        public static uint ReadSelectorValue(SegManager segMan, Register obj, Func<SelectorCache, int> func)
        {
            return ReadSelector(segMan, obj, Selector(func)).Offset;
        }

        public static Register ReadSelector(SegManager segMan, Register obj, Func<SelectorCache, int> func)
        {
            return ReadSelector(segMan, obj, Selector(func));
        }

        private static Register ReadSelector(SegManager segMan, Register obj, int selectorId)
        {
            ObjVarRef address = new ObjVarRef();
            Register fptr;
            if (LookupSelector(segMan, obj, selectorId, address, out fptr) != SelectorType.Variable)
                return Register.NULL_REG;
            return address.GetPointer(segMan)[0];
        }

        public static SelectorType LookupSelector(SegManager segMan, Register objLocation,
            Func<SelectorCache, int> func, ObjVarRef varp)
        {
            Register fptr;
            return LookupSelector(segMan, objLocation, Selector(func), varp, out fptr);
        }

        public static SelectorType LookupSelector(SegManager segMan, Register objLocation,
            Func<SelectorCache, int> func, ObjVarRef varp, out Register fptr)
        {
            return LookupSelector(segMan, objLocation, Selector(func), varp, out fptr);
        }

        public static SelectorType LookupSelector(SegManager segMan, Register objLocation, int selectorId,
            ObjVarRef varp, out Register fptr)
        {
            fptr = Register.NULL_REG;
            SciObject obj = segMan.GetObject(objLocation);
            int index;
            bool oldScriptHeader = ResourceManager.GetSciVersion() == SciVersion.V0_EARLY;

            // Early SCI versions used the LSB in the selector ID as a read/write
            // toggle, meaning that we must remove it for selector lookup.
            if (oldScriptHeader)
                selectorId &= ~1;

            if (obj == null)
            {
                throw new InvalidOperationException(
                    $"lookupSelector(): Attempt to send to non-object or invalid script. Address was {objLocation}");
            }

            index = obj.LocateVarSelector(segMan, selectorId);

            if (index >= 0)
            {
                // Found it as a variable
                if (varp != null)
                {
                    varp.obj = objLocation;
                    varp.varindex = index;
                }
                return SelectorType.Variable;
            }
            // Check if it's a method, with recursive lookup in superclasses
            while (obj != null)
            {
                index = obj.FuncSelectorPosition(selectorId);
                if (index >= 0)
                {
                    fptr = obj.GetFunction(index);
                    return SelectorType.Method;
                }
                obj = segMan.GetObject(obj.SuperClassSelector);
            }

            return SelectorType.None;


            //	return _lookupSelector_function(segMan, obj, selectorId, fptr);
        }

        public static void InvokeSelector(EngineState s, Register @object, Func<SelectorCache, int> func, int kArgc,
            StackPtr? kArgp)
        {
            InvokeSelector(s, @object, func, kArgc, kArgp, 0, StackPtr.Null);
        }

        public static void InvokeSelector(EngineState s, Register @object, Func<SelectorCache, int> func, int kArgc,
            StackPtr? kArgp, int argc, StackPtr argv)
        {
            InvokeSelector(s, @object, Selector(func), kArgc, kArgp, argc, argv);
        }

        public static void InvokeSelector(EngineState s, Register @object, int selectorId, int kArgc, StackPtr? kArgp,
            int argc, StackPtr argv)
        {
            int i;
            int framesize = 2 + 1 * argc;
            StackPtr stackframe = kArgp.Value + kArgc;

            stackframe[0] = Register.Make(0, (ushort)selectorId); // The selector we want to call
            stackframe[1] = Register.Make(0, (ushort)argc); // Argument count

            Register tmp;
            var slcType = LookupSelector(s._segMan, @object, selectorId, null, out tmp);

            switch (slcType)
            {
                case SelectorType.None:
                    throw new InvalidOperationException(
                        $"Selector '{Instance.Kernel.GetSelectorName(selectorId)}' of object at {@object} could not be invoked");
                case SelectorType.Variable:
                    throw new InvalidOperationException(
                        $"Attempting to invoke variable selector {Instance.Kernel.GetSelectorName(selectorId)} of object {@object}");
            }

            for (i = 0; i < argc; i++)
                stackframe[2 + i] = argv[i]; // Write each argument

            // Now commit the actual function:
            var xstack = Vm.SendSelector(s, @object, @object, stackframe, framesize, stackframe);

            xstack.sp += argc + 2;
            xstack.fp += argc + 2;


            Vm.Run(s); // Start a new vm
        }

        public static void WriteSelectorValue(SegManager segMan, Register obj, Func<SelectorCache, int> func,
            ushort value)
        {
            WriteSelectorValue(segMan, obj, Selector(func), value);
        }

        private static void WriteSelectorValue(SegManager segMan, Register obj, int selectorId, ushort value)
        {
            WriteSelector(segMan, obj, selectorId, Register.Make(0, value));
        }

        public static void WriteSelector(SegManager segMan, Register obj, Func<SelectorCache, int> func, Register value)
        {
            WriteSelector(segMan, obj, Selector(func), value);
        }

        private static void WriteSelector(SegManager segMan, Register obj, int selectorId, Register value)
        {
            ObjVarRef address = new ObjVarRef();

            if ((selectorId < 0) || (selectorId > Instance.Kernel.SelectorNamesSize))
            {
                throw new InvalidOperationException(
                    $"Attempt to write to invalid selector {selectorId} of object at {obj}.");
            }

            Register tmp;
            if (LookupSelector(segMan, obj, selectorId, address, out tmp) != SelectorType.Variable)
                throw new InvalidOperationException(
                    $"Selector '{Instance.Kernel.GetSelectorName(selectorId)}' of object at {obj} could not be written to");
            var ptr = address.GetPointer(segMan);
            ptr[0] = value;
        }

        public void CheckVocabularySwitch()
        {
            ushort parserLanguage = 1;
            if (Selector(o => o.parseLang) != -1)
                parserLanguage = (ushort)ReadSelectorValue(EngineState._segMan, GameObject, o => o.parseLang);

            if (parserLanguage == _vocabularyLanguage) return;

            Vocabulary = new Vocabulary(ResMan, parserLanguage > 1);
            Vocabulary.Reset();
            _vocabularyLanguage = parserLanguage;
        }

        // Base set of opcode formats. They're copied and adjusted slightly in
        // script_adjust_opcode_format depending on SCI version.
        private static readonly opcode_format[][] BaseOpcodeFormats =
        {
            // 00 - 03 / bnot, add, sub, mul
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            // 04 - 07 / div, mod, shr, shl
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            // 08 - 0B / xor, and, or, neg
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            // 0C - 0F / not, eq, ne, gt
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            // 10 - 13 / ge, lt, le, ugt
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            // 14 - 17 / uge, ult, ule, bt
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_SRelative},
            // 18 - 1B / bnt, jmp, ldi, push
            new[] {opcode_format.Script_SRelative}, new[] {opcode_format.Script_SRelative},
            new[] {opcode_format.Script_SVariable}, new[] {opcode_format.Script_None},
            // 1C - 1F / pushi, toss, dup, link
            new[] {opcode_format.Script_SVariable}, new[] {opcode_format.Script_None},
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_Variable},
            // 20 - 23 / call, callk, callb, calle
            new[] {opcode_format.Script_SRelative, opcode_format.Script_Byte},
            new[] {opcode_format.Script_Variable, opcode_format.Script_Byte},
            new[] {opcode_format.Script_Variable, opcode_format.Script_Byte},
            new[]
                {opcode_format.Script_Variable, opcode_format.Script_SVariable, opcode_format.Script_Byte},
            // 24 - 27 / ret, send, dummy, dummy
            new[] {opcode_format.Script_End}, new[] {opcode_format.Script_Byte},
            new[] {opcode_format.Script_Invalid}, new[] {opcode_format.Script_Invalid},
            // 28 - 2B / class, dummy, self, super
            new[] {opcode_format.Script_Variable}, new[] {opcode_format.Script_Invalid},
            new[] {opcode_format.Script_Byte},
            new[] {opcode_format.Script_Variable, opcode_format.Script_Byte},
            // 2C - 2F / rest, lea, selfID, dummy
            new[] {opcode_format.Script_SVariable},
            new[] {opcode_format.Script_SVariable, opcode_format.Script_Variable},
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_Invalid},
            // 30 - 33 / pprev, pToa, aTop, pTos
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_Property},
            new[] {opcode_format.Script_Property}, new[] {opcode_format.Script_Property},
            // 34 - 37 / sTop, ipToa, dpToa, ipTosî
            new[] {opcode_format.Script_Property}, new[] {opcode_format.Script_Property},
            new[] {opcode_format.Script_Property}, new[] {opcode_format.Script_Property},
            // 38 - 3B / dpTos, lofsa, lofss, push0
            new[] {opcode_format.Script_Property}, new[] {opcode_format.Script_SRelative},
            new[] {opcode_format.Script_SRelative}, new[] {opcode_format.Script_None},
            // 3C - 3F / push1, push2, pushSelf, line
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_None},
            new[] {opcode_format.Script_None}, new[] {opcode_format.Script_Word},
            // ------------------------------------------------------------------------
            // 40 - 43 / lag, lal, lat, lap
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 44 - 47 / lsg, lsl, lst, lsp
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 48 - 4B / lagi, lali, lati, lapi
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 4C - 4F / lsgi, lsli, lsti, lspi
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // ------------------------------------------------------------------------
            // 50 - 53 / sag, sal, sat, sap
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 54 - 57 / ssg, ssl, sst, ssp
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 58 - 5B / sagi, sali, sati, sapi
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 5C - 5F / ssgi, ssli, ssti, sspi
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // ------------------------------------------------------------------------
            // 60 - 63 / plusag, plusal, plusat, plusap
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 64 - 67 / plussg, plussl, plusst, plussp
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 68 - 6B / plusagi, plusali, plusati, plusapi
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 6C - 6F / plussgi, plussli, plussti, plusspi
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // ------------------------------------------------------------------------
            // 70 - 73 / minusag, minusal, minusat, minusap
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 74 - 77 / minussg, minussl, minusst, minussp
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 78 - 7B / minusagi, minusali, minusati, minusapi
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param},
            // 7C - 7F / minussgi, minussli, minussti, minusspi
            new[] {opcode_format.Script_Global}, new[] {opcode_format.Script_Local},
            new[] {opcode_format.Script_Temp}, new[] {opcode_format.Script_Param}
        };

        public AudioPlayer _audio;
        private ushort _vocabularyLanguage;
        public GfxPalette32 _gfxPalette32;
        public GfxRemap32 _gfxRemap32;
        public GfxPalette _gfxPalette16;
        public GfxRemap _gfxRemap16;
    }
}