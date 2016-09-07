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

using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using System.Collections.Generic;

namespace NScumm.Sci
{
    internal class SciGameDescriptor : IGameDescriptor
    {
        public ADGameDescription GameDescription { get; private set; }

        public SciGameDescriptor(ADGameDescription desc)
        {
            GameDescription = desc;
            Width = 320;
            Height = 200;
        }

        public string Description { get; set; }

        public string Id { get; set; }

        public string Path { get; set; }

        public PixelFormat PixelFormat { get; }

        public Platform Platform { get; set; }

        public int Width { get; }

        public int Height { get; }

        public Core.Language Language { get; set; }
    }

    // our engine debug levels
    static class DebugLevels
    {
        public const int Error = 1 << 0;
        public const int Nodes = 1 << 1;
        public const int Graphics = 1 << 2;
        public const int Strings = 1 << 3;
        public const int Memory = 1 << 4;
        public const int FuncCheck = 1 << 5;
        public const int Bresen = 1 << 6;
        public const int Sound = 1 << 7;
        public const int BaseSetter = 1 << 8;
        public const int Parser = 1 << 9;
        public const int Said = 1 << 10;
        public const int File = 1 << 11;
        public const int Time = 1 << 12;
        public const int Room = 1 << 13;
        public const int AvoidPath = 1 << 14;
        public const int DclInflate = 1 << 15;
        public const int VM = 1 << 16;
        public const int Scripts = 1 << 17;
        public const int GC = 1 << 18;
        public const int ResMan = 1 << 19;
        public const int OnStartup = 1 << 20;
        public const int DebugMode = 1 << 21;
        public const int ScriptPatcher = 1 << 22;
        public const int Workarounds = 1 << 23;
    }

    internal enum SciGameId
    {
        ASTROCHICKEN,
        CAMELOT,
        CASTLEBRAIN,
        CHEST,
        CHRISTMAS1988,
        CHRISTMAS1990,
        CHRISTMAS1992,
        CNICK_KQ,
        CNICK_LAURABOW,
        CNICK_LONGBOW,
        CNICK_LSL,
        CNICK_SQ,
        ECOQUEST,
        ECOQUEST2,
        FAIRYTALES,
        FREDDYPHARKAS,
        FUNSEEKER,
        GK1,
        GK2,
        HOYLE1,
        HOYLE2,
        HOYLE3,
        HOYLE4,
        ICEMAN,
        ISLANDBRAIN,
        JONES,
        KQ1,
        KQ4,
        KQ5,
        KQ6,
        KQ7,
        KQUESTIONS,
        LAURABOW,
        LAURABOW2,
        LIGHTHOUSE,
        LONGBOW,
        LSL1,
        LSL2,
        LSL3,
        LSL5,
        LSL6,
        LSL6HIRES, // We have a separate ID for LSL6 SCI32, because it's actually a completely different game
        LSL7,
        MOTHERGOOSE, // this one is the SCI0 version
        MOTHERGOOSE256, // this one handles SCI1 and SCI1.1 variants, at least those 2 share a bit in common
        MOTHERGOOSEHIRES, // this one is the SCI2.1 hires version, completely different from the other ones
        MSASTROCHICKEN,
        PEPPER,
        PHANTASMAGORIA,
        PHANTASMAGORIA2,
        PQ1,
        PQ2,
        PQ3,
        PQ4,
        PQSWAT,
        QFG1,
        QFG1VGA,
        QFG2,
        QFG3,
        QFG4,
        RAMA,
        SHIVERS,
        //SHIVERS2,	// Not SCI
        SLATER,
        SQ1,
        SQ3,
        SQ4,
        SQ5,
        SQ6,
        TORIN,

        FANMADE // FIXME: Do we really need/want this?
    }

    public class SciMetaEngine : AdvancedMetaEngine
    {
        private const GuiOptions GAMEOPTION_PREFER_DIGITAL_SFX = GuiOptions.GAMEOPTIONS1;
        private const GuiOptions GAMEOPTION_ORIGINAL_SAVELOAD = GuiOptions.GAMEOPTIONS2;
        private const GuiOptions GAMEOPTION_FB01_MIDI = GuiOptions.GAMEOPTIONS3;
        private const GuiOptions GAMEOPTION_JONES_CDAUDIO = GuiOptions.GAMEOPTIONS4;
        private const GuiOptions GAMEOPTION_KQ6_WINDOWS_CURSORS = GuiOptions.GAMEOPTIONS5;
        private const GuiOptions GAMEOPTION_SQ4_SILVER_CURSORS = GuiOptions.GAMEOPTIONS6;
        private const GuiOptions GAMEOPTION_EGA_UNDITHER = GuiOptions.GAMEOPTIONS7;
        private const GuiOptions GAMEOPTION_HIGH_RESOLUTION_GRAPHICS = GuiOptions.GAMEOPTIONS8;
        private const GuiOptions GAMEOPTION_ENABLE_BLACK_LINED_VIDEO = GuiOptions.GAMEOPTIONS9;

        private const GuiOptions GUIO_GK2_DEMO =
            GuiOptions.NOASPECT | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI;

        private const GuiOptions GUIO_GK2 =
            GAMEOPTION_ENABLE_BLACK_LINED_VIDEO | GuiOptions.NOASPECT | GAMEOPTION_PREFER_DIGITAL_SFX |
            GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI;

        private const GuiOptions GUIO_GK2_MAC = GUIO_GK2;

#if ENABLE_SCI32
        const GuiOptions GUIO_GK1_FLOPPY =
            GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI;

        const GuiOptions GUIO_GK1_CD =
            GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI;

        const GuiOptions GUIO_GK1_MAC = GUIO_GK1_FLOPPY;
#endif

        public SciMetaEngine()
            : base(SciGameDescriptions, Options)
        {
        }

        public override IEngine Create(GameSettings settings, ISystem system)
        {
            return new SciEngine(system, settings, (SciGameDescriptor) settings.Game,
                GameIdStrToEnum[settings.Game.Id]);
        }

        protected override IGameDescriptor CreateGameDescriptor(string path, ADGameDescription desc)
        {
            return new SciGameDescriptor(desc)
            {
                Path = path,
                Description = "TODO",
                Language = desc.language,
                Platform = desc.platform,
                Id = desc.gameid
            };
        }

        private static readonly Dictionary<GuiOptions, ExtraGuiOption> Options = new Dictionary
            <GuiOptions, ExtraGuiOption>
            {
                {
                    GAMEOPTION_EGA_UNDITHER,
                    new ExtraGuiOption("Skip EGA dithering pass (full color backgrounds)",
                        "Skip dithering pass in EGA games, graphics are shown with full colors",
                        "disable_dithering",
                        false)
                },
                {
                    GAMEOPTION_HIGH_RESOLUTION_GRAPHICS,
                    new ExtraGuiOption("Enable high resolution graphics",
                        "Enable high resolution graphics/content",
                        "enable_high_resolution_graphics",
                        true)
                },
                {
                    GAMEOPTION_ENABLE_BLACK_LINED_VIDEO,
                    new ExtraGuiOption("Enable black-lined video",
                        "Draw black lines over videos to increase their apparent sharpness",
                        "enable_black_lined_video",
                        false)
                },
                {
                    GAMEOPTION_PREFER_DIGITAL_SFX,
                    new ExtraGuiOption("Prefer digital sound effects",
                        "Prefer digital sound effects instead of synthesized ones",
                        "prefer_digitalsfx",
                        true)
                },
                {
                    GAMEOPTION_ORIGINAL_SAVELOAD,
                    new ExtraGuiOption("Use original save/load screens",
                        "Use the original save/load screens, instead of the ScummVM ones",
                        "originalsaveload",
                        false)
                },
                {
                    GAMEOPTION_FB01_MIDI,
                    new ExtraGuiOption("Use IMF/Yamaha FB-01 for MIDI output",
                        "Use an IBM Music Feature card or a Yamaha FB-01 FM synth module for MIDI output",
                        "native_fb01",
                        false)
                },
                // Jones in the Fast Lane - CD audio tracks or resource.snd
                {
                    GAMEOPTION_JONES_CDAUDIO,
                    new ExtraGuiOption("Use CD audio",
                        "Use CD audio instead of in-game audio, if available",
                        "use_cdaudio",
                        true)
                },
                // KQ6 Windows - windows cursors
                {
                    GAMEOPTION_KQ6_WINDOWS_CURSORS,
                    new ExtraGuiOption("Use Windows cursors",
                        "Use the Windows cursors (smaller and monochrome) instead of the DOS ones",
                        "windows_cursors",
                        false)
                },
                // SQ4 CD - silver cursors
                {
                    GAMEOPTION_SQ4_SILVER_CURSORS,
                    new ExtraGuiOption("Use silver cursors",
                        "Use the alternate set of silver cursors, instead of the normal golden ones",
                        "silver_cursors",
                        false)
                },
            };

        // Game descriptions
        private static readonly Dictionary<string, SciGameId> GameIdStrToEnum = new Dictionary<string, SciGameId>
        {
            {"astrochicken", SciGameId.ASTROCHICKEN},
            {"camelot", SciGameId.CAMELOT},
            {"castlebrain", SciGameId.CASTLEBRAIN},
            {"chest", SciGameId.CHEST},
            {"christmas1988", SciGameId.CHRISTMAS1988},
            {"christmas1990", SciGameId.CHRISTMAS1990},
            {"christmas1992", SciGameId.CHRISTMAS1992},
            {"cnick-kq", SciGameId.CNICK_KQ},
            {"cnick-laurabow", SciGameId.CNICK_LAURABOW},
            {"cnick-longbow", SciGameId.CNICK_LONGBOW},
            {"cnick-lsl", SciGameId.CNICK_LSL},
            {"cnick-sq", SciGameId.CNICK_SQ},
            {"ecoquest", SciGameId.ECOQUEST},
            {"ecoquest2", SciGameId.ECOQUEST2},
            {"fairytales", SciGameId.FAIRYTALES},
            {"freddypharkas", SciGameId.FREDDYPHARKAS},
            {"funseeker", SciGameId.FUNSEEKER},
            {"gk1", SciGameId.GK1},
            {"gk2", SciGameId.GK2},
            {"hoyle1", SciGameId.HOYLE1},
            {"hoyle2", SciGameId.HOYLE2},
            {"hoyle3", SciGameId.HOYLE3},
            {"hoyle4", SciGameId.HOYLE4},
            {"iceman", SciGameId.ICEMAN},
            {"islandbrain", SciGameId.ISLANDBRAIN},
            {"jones", SciGameId.JONES},
            {"kq1sci", SciGameId.KQ1},
            {"kq4sci", SciGameId.KQ4},
            {"kq5", SciGameId.KQ5},
            {"kq6", SciGameId.KQ6},
            {"kq7", SciGameId.KQ7},
            {"kquestions", SciGameId.KQUESTIONS},
            {"laurabow", SciGameId.LAURABOW},
            {"laurabow2", SciGameId.LAURABOW2},
            {"lighthouse", SciGameId.LIGHTHOUSE},
            {"longbow", SciGameId.LONGBOW},
            {"lsl1sci", SciGameId.LSL1},
            {"lsl2", SciGameId.LSL2},
            {"lsl3", SciGameId.LSL3},
            {"lsl5", SciGameId.LSL5},
            {"lsl6", SciGameId.LSL6},
            {"lsl6hires", SciGameId.LSL6HIRES},
            {"lsl7", SciGameId.LSL7},
            {"mothergoose", SciGameId.MOTHERGOOSE},
            {"mothergoose256", SciGameId.MOTHERGOOSE256},
            {"mothergoosehires", SciGameId.MOTHERGOOSEHIRES},
            {"msastrochicken", SciGameId.MSASTROCHICKEN},
            {"pepper", SciGameId.PEPPER},
            {"phantasmagoria", SciGameId.PHANTASMAGORIA},
            {"phantasmagoria2", SciGameId.PHANTASMAGORIA2},
            {"pq1sci", SciGameId.PQ1},
            {"pq2", SciGameId.PQ2},
            {"pq3", SciGameId.PQ3},
            {"pq4", SciGameId.PQ4},
            {"pqswat", SciGameId.PQSWAT},
            {"qfg1", SciGameId.QFG1},
            {"qfg1vga", SciGameId.QFG1VGA},
            {"qfg2", SciGameId.QFG2},
            {"qfg3", SciGameId.QFG3},
            {"qfg4", SciGameId.QFG4},
            {"rama", SciGameId.RAMA},
            {"sci-fanmade", SciGameId.FANMADE}, // FIXME: Do we really need/want this?
            {"shivers", SciGameId.SHIVERS},
            //{ "shivers2",        GID_SHIVERS2 },	// Not SCI
            {"slater", SciGameId.SLATER},
            {"sq1sci", SciGameId.SQ1},
            {"sq3", SciGameId.SQ3},
            {"sq4", SciGameId.SQ4},
            {"sq5", SciGameId.SQ5},
            {"sq6", SciGameId.SQ6},
            {"torin", SciGameId.TORIN}
        };

        // Gabriel Knight 2 - English DOS (GOG version) - ressci.* merged in ressci.000
        private static readonly ADGameDescription[] SciGameDescriptions =
        {
            // Astro Chicken - English DOS
            // SCI interpreter version 0.000.453
            new ADGameDescription("astrochicken", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "f3d1be7752d30ba60614533d531e2e98", 474),
                new ADGameFileDescription("resource.001", 0, "6fd05926c2199af0af6f72f90d0d7260", 126895),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH |
                                                                         GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain - English Amiga (from www.back2roots.org)
            // Executable scanning reports "1.005.000"
            // SCI interpreter version 1.000.510
            new ADGameDescription("castlebrain", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "9f9fb826aa7e944b95eadbf568244a68", 2766),
                    new ADGameFileDescription("resource.000", 0, "0efa8409c43d42b32642f96652d3230d", 314773),
                    new ADGameFileDescription("resource.001", 0, "3fb02ce493f6eacdcc3713851024f80e", 559540),
                    new ADGameFileDescription("resource.002", 0, "d226d7d3b4f77c4a566913fc310487fc", 792380),
                    new ADGameFileDescription("resource.003", 0, "d226d7d3b4f77c4a566913fc310487fc", 464348),
                }, Core.Language.EN_ANY, Platform.Amiga, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain - German Amiga (from www.back2roots.org, also includes English language)
            // Executable scanning reports "1.005.001"
            // SCI interpreter version 1.000.510
            new ADGameDescription("castlebrain", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "8e60424682db52a982bcc3535a7e86f3", 2796),
                    new ADGameFileDescription("resource.000", 0, "0efa8409c43d42b32642f96652d3230d", 332468),
                    new ADGameFileDescription("resource.001", 0, "4e0836fadc324316c1a418125709ba45", 569057),
                    new ADGameFileDescription("resource.002", 0, "85e51acb5f9c539d66e3c8fe40e17da5", 826309),
                    new ADGameFileDescription("resource.003", 0, "85e51acb5f9c539d66e3c8fe40e17da5", 493638),
                }, Core.Language.DE_DEU, Platform.Amiga, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain Macintosh (from omer_mor, bug report #3328251)
            new ADGameDescription("castlebrain", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "75cb06a94d2e0641295edd043f26f3a8", 2763),
                    new ADGameFileDescription("resource.000", 0, "27ec5fa09cd12a7fd16e86d96a2ed245", 476566),
                    new ADGameFileDescription("resource.001", 0, "7f7da982f5cd868e1e608cd4f6515656", 400521),
                    new ADGameFileDescription("resource.002", 0, "e1a6b6f1060f60be9dcb6d28ad7a2a20", 1168310),
                    new ADGameFileDescription("resource.003", 0, "6c3d1bb26ad532c94046bc9ac49b5ff4", 891295),
                }, Core.Language.EN_ANY, Platform.Macintosh, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain - English DOS Non-Interactive Demo
            // SCI interpreter version 1.000.005
            new ADGameDescription("castlebrain", "Demo", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "467bb5e3224bb54640c3280032aebff5", 633),
                    new ADGameFileDescription("resource.000", 0, "9780f040d58182994e22d2e34fab85b0", 67367),
                    new ADGameFileDescription("resource.001", 0, "2af49dbd8f2e1db4ab09f9310dc91259", 570553),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain - English DOS 5.25" Floppy EGA (from omer_mor, bug report #3035349)
            new ADGameDescription("castlebrain", "EGA", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "88d106f945f7fd9d1aeda961cfec38a9", 2646),
                    new ADGameFileDescription("resource.000", 0, "6e125f4ce3f4f5c35f2617c7b66c6e21", 25325),
                    new ADGameFileDescription("resource.001", 0, "1d806162f6d3cfbe3c0135414efe6f88", 99931),
                    new ADGameFileDescription("resource.002", 0, "6a41a0eb5237778427dddf92ae07cf9b", 294772),
                    new ADGameFileDescription("resource.003", 0, "0c6ab4efb3be4d991ae9762e19f17c92", 306378),
                    new ADGameFileDescription("resource.004", 0, "5e7b90949422de005f80285979972e43", 292423),
                    new ADGameFileDescription("resource.005", 0, "8a5ed3ba96e2eaf18e36fedfaab89419", 297838),
                    new ADGameFileDescription("resource.006", 0, "dceed92e709cad1bd9582809a235b0a0", 266682),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain - English DOS 3.5" Floppy EGA (from nozomi77, bug report #3405307)
            new ADGameDescription("castlebrain", "EGA", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "dfcf23e36cb81223bdf11166aaf90754", 2730),
                    new ADGameFileDescription("resource.000", 0, "27ec5fa09cd12a7fd16e86d96a2ed245", 300857),
                    new ADGameFileDescription("resource.001", 0, "6e0020a9f9bef9a9d65943dc013f14b5", 222108),
                    new ADGameFileDescription("resource.002", 0, "de2f182529efaad2c4b510b452ab77ac", 633662),
                    new ADGameFileDescription("resource.003", 0, "38b4b37febc6b4f5061c461a283df148", 430388),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain - English DOS Floppy (from jvprat)
            // Executable scanning reports "1.000.044", Floppy label reports "1.0, 10.30.91", VERSION file reports "1.000"
            // SCI interpreter version 1.000.510
            new ADGameDescription("castlebrain", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "1302ceb141d44b05a42723791b2d84c6", 2739),
                    new ADGameFileDescription("resource.000", 0, "27ec5fa09cd12a7fd16e86d96a2ed245", 346731),
                    new ADGameFileDescription("resource.001", 0, "d2f5a1be74ed963fa849a76892be5290", 794832),
                    new ADGameFileDescription("resource.002", 0, "c0c29c51af66d65cb53f49e785a2d978", 1280907),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain - English DOS 5.25" Floppy VGA 1.1 (from rnjacobs, bug report #3578286)
            new ADGameDescription("castlebrain", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "a1deac2647ad09472c63656bfb950a4d", 2739),
                    new ADGameFileDescription("resource.000", 0, "27ec5fa09cd12a7fd16e86d96a2ed245", 347071),
                    new ADGameFileDescription("resource.001", 0, "13e81e1839cd7b216d2bb5615c1ca160", 356812),
                    new ADGameFileDescription("resource.002", 0, "583d348c908f89f94f8551d7fe0a2eca", 991752),
                    new ADGameFileDescription("resource.003", 0, "6c3d1bb26ad532c94046bc9ac49b5ff4", 728315),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain - English DOS Floppy 1.1
            new ADGameDescription("castlebrain", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "f77728304c70017c54793eb6ca648174", 2745),
                    new ADGameFileDescription("resource.000", 0, "27ec5fa09cd12a7fd16e86d96a2ed245", 347071),
                    new ADGameFileDescription("resource.001", 0, "13e81e1839cd7b216d2bb5615c1ca160", 796776),
                    new ADGameFileDescription("resource.002", 0, "930e416bec196b9703a331d81b3d66f2", 1283812),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain - English DOS Floppy 1.000
            // Reported by graxer in bug report #3037942
            new ADGameDescription("castlebrain", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "453daa935535cef68d19704c2b1b78a2", 2649),
                    new ADGameFileDescription("resource.000", 0, "6e125f4ce3f4f5c35f2617c7b66c6e21", 25929),
                    new ADGameFileDescription("resource.001", 0, "4891faa2f6594c622e482f0ddce24fb4", 99404),
                    new ADGameFileDescription("resource.002", 0, "aebb56d5d005557ca0d122a03aa85386", 322459),
                    new ADGameFileDescription("resource.003", 0, "278ec1e6132c7be844d433dd23beb318", 335156),
                    new ADGameFileDescription("resource.004", 0, "fca1c3f2be660185206f004bda09f4fb", 333549),
                    new ADGameFileDescription("resource.005", 0, "9294e55da1e83708ad3104b2a3963e18", 327537),
                    new ADGameFileDescription("resource.006", 0, "1d778a0c65cac9ddbab65495e50a94ee", 335281),
                    new ADGameFileDescription("resource.007", 0, "063bb8ce4157c778cf30d1c912c006f1", 335631),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain - Spanish DOS (also includes english language)
            // SCI interpreter version 1.000.510
            new ADGameDescription("castlebrain", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "5738c163e014bbe046474de009020b82", 2727),
                    new ADGameFileDescription("resource.000", 0, "27ec5fa09cd12a7fd16e86d96a2ed245", 1197694),
                    new ADGameFileDescription("resource.001", 0, "735be4e58957180cfc807d5e18fdffcd", 1433302),
                }, Core.Language.ES_ESP, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // Castle of Dr. Brain aka Dr. Brain Puzzle no Shiro - Japanese PC-98 Floppy (from m_kiewitz)
            // includes both Japanese and English text
            // Executable scanning reports "x.yyy.zzz", VERSION file reports "1.000"
            new ADGameDescription("castlebrain", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "ff9674d5d0215a7ebae25ee38d5a72af", 2631),
                    new ADGameFileDescription("resource.000", 0, "27ec5fa09cd12a7fd16e86d96a2ed245", 548272),
                    new ADGameFileDescription("resource.001", 0, "7c3e82c390e934de9b7afcab6de9cec4", 1117317),
                }, Core.Language.JA_JPN, Platform.PC98, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GuiOptions.NOASPECT | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),
#if ENABLE_SCI32
// Inside the Chest / Behind the Developer's Shield
// SCI interpreter version 2.000.000
            new ADGameDescription("chest", "", new [] {
                new ADGameFileDescription("resource.map", 0, "9dd015e79cac4f91e7de805448f39775", 1912),
                new ADGameFileDescription("resource.000", 0, "e4efcd042f86679dd4e1834bb3a38edb", 3770943),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.UNSATBLE),
#endif
            // Christmas Card 1988 - English DOS
            // SCI interpreter version 0.000.294
            new ADGameDescription("christmas1988", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "39485580d34a72997f3d5b3aba4d24f1", 426),
                new ADGameFileDescription("resource.001", 0, "11391434f41c834090d7a1e9488ce936", 129739),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Christmas Card 1990: The Seasoned Professional - English DOS (16 Colors)
            // SCI interpreter version 1.000.172
            new ADGameDescription("christmas1990", "16 Colors", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "8f656714a05b94423ac6eb10ee8797d0", 600),
                    new ADGameFileDescription("resource.001", 0, "acde93e58fca4f7a2a5a220558a94aa8", 272629),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Christmas Card 1990: The Seasoned Professional - English DOS (256 Colors)
            // SCI interpreter version 1.000.174
            new ADGameDescription("christmas1990", "256 Colors", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "44b8f45b841b9b5e17e939a35e443988", 600),
                    new ADGameFileDescription("resource.001", 0, "acde93e58fca4f7a2a5a220558a94aa8", 335362),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Christmas Card 1992 - English DOS
            // SCI interpreter version 1.001.055
            new ADGameDescription("christmas1992", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "f1f8c8a8443f523422af70b4ec85b71c", 318),
                    new ADGameFileDescription("resource.000", 0, "62fb9256f8e7e6e65a6875efdb7939ac", 203396),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Codename: Iceman - English Amiga (from www.back2roots.org)
            // Executable scanning reports "1.002.031"
            // SCI interpreter version 0.000.685
            new ADGameDescription("iceman", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "035829b391709a4e542d7c7b224625f6", 6000),
                    new ADGameFileDescription("resource.000", 0, "b1bccd827453d4cb834bfd5b45bef63c", 73682),
                    new ADGameFileDescription("resource.001", 0, "ede5a0e1e2a80fb629dae72c72f33d37", 293145),
                    new ADGameFileDescription("resource.002", 0, "36670a917550757d57df84c96cf9e6d9", 469387),
                    new ADGameFileDescription("resource.003", 0, "d97a96f1ab91b41cf46a02cc89b0a04e", 619219),
                    new ADGameFileDescription("resource.004", 0, "8613c45fc771d658e5a505b9a4a54f31", 713382),
                    new ADGameFileDescription("resource.005", 0, "605b67a9ef199a9bb015745e7c004cf4", 478384),
                }, Core.Language.EN_ANY, Platform.Amiga, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Codename: Iceman - English Atari ST
            // Game version 1.041
            // Executable reports "1.002.041"
            new ADGameDescription("iceman", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "066e89b685ad788e06bae0b76d0d37d3", 5718),
                    new ADGameFileDescription("resource.000", 0, "053278385ce910a3f630f2e45e3c10be", 26987),
                    new ADGameFileDescription("resource.001", 0, "32b351072fccf76fc82234d73d28c08b", 438880),
                    new ADGameFileDescription("resource.002", 0, "36670a917550757d57df84c96cf9e6d9", 566667),
                    new ADGameFileDescription("resource.003", 0, "d97a96f1ab91b41cf46a02cc89b0a04e", 624304),
                    new ADGameFileDescription("resource.004", 0, "8613c45fc771d658e5a505b9a4a54f31", 670884),
                }, Core.Language.EN_ANY, Platform.AtariST, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Codename: Iceman - English DOS Non-Interactive Demo
            // Executable scanning reports "0.000.685"
            new ADGameDescription("iceman", "Demo", new[]
            {
                new ADGameFileDescription("resource.map", 0, "782974f29d8a824782d2d4aea39964e3", 1056),
                new ADGameFileDescription("resource.001", 0, "d4b75e280d1c3a97cfef1b0bebff387c", 573647),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                     GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                     GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Codename: Iceman - English DOS (from jvprat)
            // Executable scanning reports "0.000.685", Floppy label reports "1.033, 6.8.90", VERSION file reports "1.033"
            // SCI interpreter version 0.000.685
            new ADGameDescription("iceman", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "a18f3cef4481a81d3415fb87a754343e", 5700),
                new ADGameFileDescription("resource.000", 0, "b1bccd827453d4cb834bfd5b45bef63c", 26989),
                new ADGameFileDescription("resource.001", 0, "32b351072fccf76fc82234d73d28c08b", 438872),
                new ADGameFileDescription("resource.002", 0, "36670a917550757d57df84c96cf9e6d9", 566549),
                new ADGameFileDescription("resource.003", 0, "d97a96f1ab91b41cf46a02cc89b0a04e", 624303),
                new ADGameFileDescription("resource.004", 0, "8613c45fc771d658e5a505b9a4a54f31", 670883),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Codename: Iceman - English DOS (from FRG)
            // SCI interpreter version 0.000.668
            new ADGameDescription("iceman", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "554b44b79b0e9a7fc59f66dda0daac02", 5670),
                new ADGameFileDescription("resource.000", 0, "b1bccd827453d4cb834bfd5b45bef63c", 26974),
                new ADGameFileDescription("resource.001", 0, "005bd332d4b0f9d8e99d3b905223a332", 438501),
                new ADGameFileDescription("resource.002", 0, "250b859381ebf2bf8922bd99683b0cc1", 566464),
                new ADGameFileDescription("resource.003", 0, "dc7c5280e7acfaffe6ef2a6c963c5f94", 622118),
                new ADGameFileDescription("resource.004", 0, "64f342463f6f35ba71b3509ef696ae3f", 669188),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Codename: Iceman - English DOS (supplied by ssburnout in bug report #3049193)
            // 1.022 9x5.25" (label: Int#0.000.668)
            new ADGameDescription("iceman", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "2948e06dab4930e4c8098c24ac874db8", 6252),
                new ADGameFileDescription("resource.000", 0, "b1bccd827453d4cb834bfd5b45bef63c", 26974),
                new ADGameFileDescription("resource.001", 0, "005bd332d4b0f9d8e99d3b905223a332", 126839),
                new ADGameFileDescription("resource.002", 0, "250b859381ebf2bf8922bd99683b0cc1", 307001),
                new ADGameFileDescription("resource.003", 0, "7d7a840701d2f6eff57679bf7dced747", 318060),
                new ADGameFileDescription("resource.004", 0, "e0e72970bad9a956db13dcb63d898437", 322457),
                new ADGameFileDescription("resource.005", 0, "1f2f79e399098859c73e49ac6a3545d8", 330657),
                new ADGameFileDescription("resource.006", 0, "08050329aa113a9f14ed99cbfe3536ec", 232942),
                new ADGameFileDescription("resource.007", 0, "64f342463f6f35ba71b3509ef696ae3f", 267811),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Codename: Iceman - English DOS 1.023 (from abevi, bug report #2612718)
            new ADGameDescription("iceman", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "da131654de1d6f640222c092313c6ca5", 6252),
                new ADGameFileDescription("resource.000", 0, "b1bccd827453d4cb834bfd5b45bef63c", 26974),
                new ADGameFileDescription("resource.001", 0, "005bd332d4b0f9d8e99d3b905223a332", 126833),
                new ADGameFileDescription("resource.002", 0, "250b859381ebf2bf8922bd99683b0cc1", 306891),
                new ADGameFileDescription("resource.003", 0, "7d7a840701d2f6eff57679bf7dced747", 317954),
                new ADGameFileDescription("resource.004", 0, "e0e72970bad9a956db13dcb63d898437", 322483),
                new ADGameFileDescription("resource.005", 0, "dc7c5280e7acfaffe6ef2a6c963c5f94", 330653),
                new ADGameFileDescription("resource.006", 0, "08050329aa113a9f14ed99cbfe3536ec", 232942),
                new ADGameFileDescription("resource.007", 0, "64f342463f6f35ba71b3509ef696ae3f", 267702),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Conquests of Camelot - English Amiga (from www.back2roots.org)
            // Executable scanning reports "1.002.030"
            // SCI interpreter version 0.000.685
            new ADGameDescription("camelot", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "51aba42f8e63b219755d4372ea424509", 6654),
                    new ADGameFileDescription("resource.000", 0, "dfadf0b4c9fb44ce55570149856c302d", 128100),
                    new ADGameFileDescription("resource.001", 0, "67391de361b9347f123ac0899b4b91f7", 300376),
                    new ADGameFileDescription("resource.002", 0, "8c7f12b2c38d225d4c7121b30ea1b4d2", 605334),
                    new ADGameFileDescription("resource.003", 0, "82a73e7572e7ee627429bb5111ff82ca", 672392),
                    new ADGameFileDescription("resource.004", 0, "6821dc97cf643ba521a4e840dda3c58b", 647410),
                    new ADGameFileDescription("resource.005", 0, "c6e551bdc24f0acc193159038d4ca767", 605882),
                    new ADGameFileDescription("resource.006", 0, "8f880a536908ab496bbc552f7f5c3738", 585255),
                }, Core.Language.EN_ANY, Platform.Amiga, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Conquests of Camelot - English DOS Non-Interactive Demo
            // SCI interpreter version 0.000.668
            new ADGameDescription("camelot", "Demo", new[]
            {
                new ADGameFileDescription("resource.map", 0, "f4cd75c15be75e04cdca3acda2c0b0ea", 468),
                new ADGameFileDescription("resource.001", 0, "4930708722f34bfbaa4945fb08f55f61", 232523),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                     GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                     GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Conquests of Camelot - English DOS (from jvprat)
            // Executable scanning reports "0.000.685", Floppy label reports "1.001, 0.000.685", VERSION file reports "1.001.000"
            // SCI interpreter version 0.000.685
            new ADGameDescription("camelot", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "95eca3991906dfd7ed26d193df07596f", 7278),
                new ADGameFileDescription("resource.001", 0, "8e1a3a8c588007404b532b8dfacc1460", 596774),
                new ADGameFileDescription("resource.002", 0, "8e1a3a8c588007404b532b8dfacc1460", 722250),
                new ADGameFileDescription("resource.003", 0, "8e1a3a8c588007404b532b8dfacc1460", 723712),
                new ADGameFileDescription("resource.004", 0, "8e1a3a8c588007404b532b8dfacc1460", 729143),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Conquests of Camelot - English Atari ST
            // Game version 1.019.000
            // Floppy: INT#10.12.90
            // Executable reports "1.002.038"
            new ADGameDescription("camelot", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "0f80a11867be91a158823887a49cf443", 7290),
                    new ADGameFileDescription("resource.001", 0, "162f66c42e4146ee63f78fba6f1a6757", 596773),
                    new ADGameFileDescription("resource.002", 0, "162f66c42e4146ee63f78fba6f1a6757", 724615),
                    new ADGameFileDescription("resource.003", 0, "162f66c42e4146ee63f78fba6f1a6757", 713351),
                    new ADGameFileDescription("resource.004", 0, "162f66c42e4146ee63f78fba6f1a6757", 718766),
                }, Core.Language.EN_ANY, Platform.AtariST, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Conquests of Camelot - English DOS
            // SCI interpreter version 0.000.685
            new ADGameDescription("camelot", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "86bffb2a393b7a5d8de45e735091f037", 9504),
                new ADGameFileDescription("resource.001", 0, "8e1a3a8c588007404b532b8dfacc1460", 212461),
                new ADGameFileDescription("resource.002", 0, "8e1a3a8c588007404b532b8dfacc1460", 317865),
                new ADGameFileDescription("resource.003", 0, "8e1a3a8c588007404b532b8dfacc1460", 359145),
                new ADGameFileDescription("resource.004", 0, "8e1a3a8c588007404b532b8dfacc1460", 345180),
                new ADGameFileDescription("resource.005", 0, "8e1a3a8c588007404b532b8dfacc1460", 345734),
                new ADGameFileDescription("resource.006", 0, "8e1a3a8c588007404b532b8dfacc1460", 332446),
                new ADGameFileDescription("resource.007", 0, "8e1a3a8c588007404b532b8dfacc1460", 358182),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Conquests of the Longbow - English Amiga (from www.back2roots.org)
            // Executable scanning reports "1.005.001"
            // SCI interpreter version 1.000.510
            new ADGameDescription("longbow", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "6204f3d00c0f6c0f5f95a29a4190f2f9", 6048),
                    new ADGameFileDescription("resource.000", 0, "8d11c744b4a51e7a8ceac687a46f08ca", 830333),
                    new ADGameFileDescription("resource.001", 0, "76caf8593e065a98c8ab4a6e2c7dbafc", 839008),
                    new ADGameFileDescription("resource.002", 0, "eb312373045906b54a3181bebaf6651a", 733145),
                    new ADGameFileDescription("resource.003", 0, "7fe3b3372d7fdda60045807e9c8e4867", 824554),
                    new ADGameFileDescription("resource.004", 0, "d1038c75d85a6650d48e07d174a6a913", 838175),
                    new ADGameFileDescription("resource.005", 0, "1c3804e56b114028c5873a35c2f06d13", 653002),
                    new ADGameFileDescription("resource.006", 0, "f9487732289a4f4966b4e34eea413325", 842817),
                }, Core.Language.EN_ANY, Platform.Amiga, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Conquests of the Longbow - English DOS
            // SCI interpreter version 1.000.510
            new ADGameDescription("longbow", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "36d3b81ff75b67dd4d27b7f5d3166503", 6261),
                    new ADGameFileDescription("resource.000", 0, "36e8fda5d0b8c49e587c8a9617959f72", 1096767),
                    new ADGameFileDescription("resource.001", 0, "d4c299213f8d799da1492680d12d0fb3", 1133226),
                    new ADGameFileDescription("resource.002", 0, "7f6ce331219d58d5087731e4475ab4f1", 1128555),
                    new ADGameFileDescription("resource.003", 0, "21ebe6b39b57a73fc449f67f013765aa", 972635),
                    new ADGameFileDescription("resource.004", 0, "9cfce07e204a329e94fda8b5657621da", 1064637),
                    new ADGameFileDescription("resource.005", 0, "d036df0872f2db19bca34601276be2d7", 1154950),
                    new ADGameFileDescription("resource.006", 0, "b367a6a59f29ee30dde1d88a5a41152d", 1042966),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Conquests of the Longbow - English DOS Floppy (from jvprat)
            // Executable scanning reports "1.000.168", Floppy label reports "1.1, 1.13.92", VERSION file reports "1.1"
            // SCI interpreter version 1.000.510
            new ADGameDescription("longbow", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "247f955865572569342751de47e861ab", 6027),
                    new ADGameFileDescription("resource.000", 0, "36e8fda5d0b8c49e587c8a9617959f72", 1297120),
                    new ADGameFileDescription("resource.001", 0, "1e6084a19f7a6c50af88d3a9b32c411e", 1366155),
                    new ADGameFileDescription("resource.002", 0, "7f6ce331219d58d5087731e4475ab4f1", 1234743),
                    new ADGameFileDescription("resource.003", 0, "1867136d01ece57b531032d466910522", 823686),
                    new ADGameFileDescription("resource.004", 0, "9cfce07e204a329e94fda8b5657621da", 1261462),
                    new ADGameFileDescription("resource.005", 0, "21ebe6b39b57a73fc449f67f013765aa", 1284720)
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Conquests of the Longbow - English DOS
            // SCI interpreter version 1.000.510
            new ADGameDescription("longbow", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "737c6f83a1ee601727ff026898f19fa1", 6045),
                    new ADGameFileDescription("resource.000", 0, "36e8fda5d0b8c49e587c8a9617959f72", 1296607),
                    new ADGameFileDescription("resource.001", 0, "1e6084a19f7a6c50af88d3a9b32c411e", 1379267),
                    new ADGameFileDescription("resource.002", 0, "7f6ce331219d58d5087731e4475ab4f1", 1234140),
                    new ADGameFileDescription("resource.003", 0, "1867136d01ece57b531032d466910522", 823610),
                    new ADGameFileDescription("resource.004", 0, "9cfce07e204a329e94fda8b5657621da", 1260237),
                    new ADGameFileDescription("resource.005", 0, "21ebe6b39b57a73fc449f67f013765aa", 1284609),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Conquests of the Longbow EGA - English DOS
            // SCI interpreter version 1.000.510
            new ADGameDescription("longbow", "EGA", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "7676ec9f08967d7a9a7724f5170456e0", 6261),
                    new ADGameFileDescription("resource.000", 0, "36e8fda5d0b8c49e587c8a9617959f72", 718161),
                    new ADGameFileDescription("resource.001", 0, "3c3735caa34fa3f261a9552831bb43ed", 705680),
                    new ADGameFileDescription("resource.002", 0, "7025b87e735b1df3f0e9488a621f4333", 700633),
                    new ADGameFileDescription("resource.003", 0, "eaca7933e8e56bea22b42f7fd5d7a8a7", 686510),
                    new ADGameFileDescription("resource.004", 0, "b7bb35c027bb424ecefcd122768e5e60", 705631),
                    new ADGameFileDescription("resource.005", 0, "58942b1aa6d6ffeb66e9f8897fd4435f", 469243),
                    new ADGameFileDescription("resource.006", 0, "8c767b3939add63d11274065e46aad04", 713158),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Conquests of the Longbow DOS 1.0 EGA (4 x 5.25" disks)
            // Provided by ssburnout in bug report #3046802
            new ADGameDescription("longbow", "EGA", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "0517ca368ec844df0cb21a05020fae01", 6021),
                    new ADGameFileDescription("resource.000", 0, "36e8fda5d0b8c49e587c8a9617959f72", 934643),
                    new ADGameFileDescription("resource.001", 0, "76c729e563809170e6cc8b2f3f6cf0a4", 1196133),
                    new ADGameFileDescription("resource.002", 0, "8c767b3939add63d11274065e46aad04", 1152478),
                    new ADGameFileDescription("resource.003", 0, "7025b87e735b1df3f0e9488a621f4333", 1171439),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Conquests of the Longbow - English DOS Non-Interactive Demo
            // SCI interpreter version 1.000.510
            new ADGameDescription("longbow", "Demo", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "cbc5cb73341de1bff1b1e20a640af220", 588),
                    new ADGameFileDescription("resource.001", 0, "f05a20cc07eee85da8e999d0ac0f596b", 869916),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Conquests of the Longbow - German DOS (suplied by markcoolio in bug report #2727681, also includes english language)
            // SCI interpreter version 1.000.510
            new ADGameDescription("longbow", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "7376b7a07f8bd3a8ab8d67595d3f5b51", 6285),
                    new ADGameFileDescription("resource.000", 0, "ee39f92e006142424cf9209329e727c6", 977281),
                    new ADGameFileDescription("resource.001", 0, "d4c299213f8d799da1492680d12d0fb3", 1167657),
                    new ADGameFileDescription("resource.002", 0, "7f6ce331219d58d5087731e4475ab4f1", 1172521),
                    new ADGameFileDescription("resource.003", 0, "a204de2a083a7770ff455a838210a678", 1165249),
                    new ADGameFileDescription("resource.004", 0, "9cfce07e204a329e94fda8b5657621da", 1101869),
                    new ADGameFileDescription("resource.005", 0, "d036df0872f2db19bca34601276be2d7", 1176914),
                    new ADGameFileDescription("resource.006", 0, "b367a6a59f29ee30dde1d88a5a41152d", 1123585),
                }, Core.Language.DE_DEU, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest - English DOS Non-Interactive Demo (from FRG)
            // Executable scanning reports "x.yyy.zzz"
            // SCI interpreter version 1.001.069 (just a guess)
            new ADGameDescription("ecoquest", "Demo", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "c819e171359b7c95f4c13b846d5c034e", 873),
                    new ADGameFileDescription("resource.001", 0, "baf9393a9bfa73098adb501e5bc5487b", 657518),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest - English DOS CD 1.1
            // SCI interpreter version 1.001.064
            new ADGameDescription("ecoquest", "CD", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "a4b73d5d2b55bdb6e44345e99c8fbdd0", 4804),
                    new ADGameFileDescription("resource.000", 0, "d908dbef56816ac6c60dd145fdeafb2b", 3536046),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.CD,
                GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // Eco Quest - English DOS CD 1.1
            // SCI interpreter version 1.001.064
            // Same entry as the DOS version above. This one is used for the alternate
            // General MIDI music tracks in the Windows version
            new ADGameDescription("ecoquest", "CD", new[]
            {
                new ADGameFileDescription("resource.map", 0, "a4b73d5d2b55bdb6e44345e99c8fbdd0", 4804),
                new ADGameFileDescription("resource.000", 0, "d908dbef56816ac6c60dd145fdeafb2b", 3536046),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.CD, GuiOptions.MIDIGM | GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                   GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest - English DOS Floppy (reported by misterhands in bug #6599)
            // Game v1.10, interpreter 2.000.286, INT #6.12.92
            new ADGameDescription("ecoquest", "Floppy", new[]
            {
                new ADGameFileDescription("resource.map", 0, "acb10c12bf15ffa7d0fac36124b20c8e", 4890),
                new ADGameFileDescription("resource.000", 0, "89cf7c8eed99afd0a9f4188170b81ebe", 3428654),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.CD, GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                   GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest - English DOS Floppy
            // SCI interpreter version 1.000.510
            new ADGameDescription("ecoquest", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "704367225929a88aad281ac72844ddac", 4053),
                    new ADGameFileDescription("resource.000", 0, "241b98d3903f6a5b872baa19b80aef3b", 1099239),
                    new ADGameFileDescription("resource.001", 0, "96d4435d24c01f1c1675e46457604c5f", 1413719),
                    new ADGameFileDescription("resource.002", 0, "28fe9b4f0567e71feb198bc9f3a2c605", 1241816),
                    new ADGameFileDescription("resource.003", 0, "f3146df0ad4297f5ce35aa8c4753bf6c", 586832),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest - English DOS Floppy
            // SCI interpreter version 1.000.510
            new ADGameDescription("ecoquest", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "f77baec05fae76707205f5be6534a7f3", 4059),
                    new ADGameFileDescription("resource.000", 0, "241b98d3903f6a5b872baa19b80aef3b", 858490),
                    new ADGameFileDescription("resource.001", 0, "2fed7451bca81b0c891eed1a956f2263", 1212161),
                    new ADGameFileDescription("resource.002", 0, "323b3b12f43d53f27d259beb225f0aa7", 1129316),
                    new ADGameFileDescription("resource.003", 0, "83ac03e4bddb2c1ac2d36d2a587d0536", 1145616),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest - German DOS Floppy (supplied by markcoolio in bug report #2723744, also includes english language)
            // SCI interpreter version 1.000.510
            new ADGameDescription("ecoquest", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "7a9b43bf27dc000ac8559ecbe824b659", 4395),
                    new ADGameFileDescription("resource.000", 0, "99b73d40403a51c7e60d01df0d6cd34a", 998227),
                    new ADGameFileDescription("resource.001", 0, "2fed7451bca81b0c891eed1a956f2263", 1212060),
                    new ADGameFileDescription("resource.002", 0, "02d7d0411f7903aacb3bc8b0f8ca8a9a", 1202581),
                    new ADGameFileDescription("resource.003", 0, "84dd11b6825255671c703aee5ceff620", 1175835),
                }, Core.Language.DE_DEU, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest - Spanish DOS Floppy (from jvprat, also includes english language)
            // Executable scanning reports "1.ECO.013", VERSION file reports "1.000, 11.12.92"
            // SCI interpreter version 1.000.510
            new ADGameDescription("ecoquest", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "82e6b1e3bdb2f064b18380009df7b345", 4395),
                    new ADGameFileDescription("resource.000", 0, "0b12a91c935e385308af8d17811deded", 1004085),
                    new ADGameFileDescription("resource.001", 0, "2fed7451bca81b0c891eed1a956f2263", 1212060),
                    new ADGameFileDescription("resource.002", 0, "2d21a1d2dcbffa551552e3e0725d2284", 1186033),
                    new ADGameFileDescription("resource.003", 0, "84dd11b6825255671c703aee5ceff620", 1174993),
                }, Core.Language.ES_ESP, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest - French DOS Floppy (from Strangerke, also includes english language)
            // SCI interpreter version 1.ECO.013
            new ADGameDescription("ecoquest", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "67742945cd59b896d9f22a549f605217", 4407),
                    new ADGameFileDescription("resource.000", 0, "0b12a91c935e385308af8d17811deded", 973723),
                    new ADGameFileDescription("resource.001", 0, "fc7fba54b6bb88fd7e9c229636599aa9", 1205841),
                    new ADGameFileDescription("resource.002", 0, "b836c6ee9de67d814ac5d1b05f5b9858", 1173872),
                    new ADGameFileDescription("resource.003", 0, "f8f767f9d6351432621c6e54c1b2ba8c", 1141520),
                }, Core.Language.FR_FRA, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest 2 - English DOS Non-Interactive Demo
            // SCI interpreter version 1.001.055
            new ADGameDescription("ecoquest2", "Demo", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "607cfa0d8a03b7d348c06ee727e3d939", 1321),
                    new ADGameFileDescription("resource.000", 0, "dd6f614c43c029f063e93cd243af90a4", 525992),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest 2 - English DOS Floppy (supplied by markcoolio in bug report #2723761)
            // SCI interpreter version 1.001.065
            new ADGameDescription("ecoquest2", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "28fb7b6abb9fc1cb8882d7c2e701b63f", 5658),
                    new ADGameFileDescription("resource.000", 0, "cc1d17e5637528dbe4a812699e1cbfc6", 4208192),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest 2 - French DOS Floppy (from Strangerke)
            // SCI interpreter version 1.001.081
            new ADGameDescription("ecoquest2", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "c22ab8b33c339c138b6b1697b77b9e79", 5588),
                    new ADGameFileDescription("resource.000", 0, "1c4093f7248240329121fdf8c0d59152", 4231946),
                }, Core.Language.FR_FRA, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest 2 - Spanish DOS Floppy (supplied by umbrio in bug report #3313962)
            new ADGameDescription("ecoquest2", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "a6b271b934afa7e84d03816a4fefa67b", 5593),
                    new ADGameFileDescription("resource.000", 0, "1c4093f7248240329121fdf8c0d59152", 4209150),
                    new ADGameFileDescription("resource.msg", 0, "eff8be1925d42288de55e405983e9314", 117810),
                }, Core.Language.ES_ESP, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Eco Quest 2 - German DOS Floppy (supplied by frankenbuam in bug report #3615072)
            new ADGameDescription("ecoquest2", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "d8b20073e64f41f6437f73143a186753", 5643),
                    new ADGameFileDescription("resource.000", 0, "cc1d17e5637528dbe4a812699e1cbfc6", 4210876),
                    new ADGameFileDescription("resource.msg", 0, "2f231d31af172ea72ed533fd112f971b", 133458),
                }, Core.Language.DE_DEU, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Freddy Pharkas - English DOS demo (from FRG)
            // SCI interpreter version 1.001.069
            new ADGameDescription("freddypharkas", "Demo", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "97aa9fcfe84c9993a64debd28c32393a", 1909),
                    new ADGameFileDescription("resource.000", 0, "5ea8e7a3ea10cce6efd5c106dc62fd8c", 867724),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Freddy Pharkas - English CD DOS (from FRG)
            // SCI interpreter version 1.001.132
            new ADGameDescription("freddypharkas", "CD", new[]
            {
                new ADGameFileDescription("resource.map", 0, "d46b282f228a67ba13bd4b4009e95f8f", 6058),
                new ADGameFileDescription("resource.000", 0, "ee3c64ffff0ba9fb08bea2624631c598", 5490246),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.CD, GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                   GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Freddy Pharkas - English DOS Floppy (updated information from markcoolio in bug reports #2723773 and #2724720)
            // Executable scanning reports "1.cfs.081"
            // SCI interpreter version 1.001.132 (just a guess)
            new ADGameDescription("freddypharkas", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "a32674e7fbf7b213b4a066c8037f16b6", 5816),
                    new ADGameFileDescription("resource.0 00", 0, "96b07e9b914dba1c8dc6c78a176326df", 5233230),
                    new ADGameFileDescription("resource.msg", 0, "554f65315d851184f6e38211489fdd8f", -1)
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Freddy Pharkas - French DOS Floppy (supplied by misterhands in bug report #3589449)
            // Executable scanning reports "1.cfs.081"
            new ADGameDescription("freddypharkas", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "a32674e7fbf7b213b4a066c8037f16b6", 5816),
                    new ADGameFileDescription("resource.000", 0, "fed4808fdb72486908ac7ad0044b14d8", 5233230),
                    new ADGameFileDescription("resource.msg", 0, "4dc478f5c73b57e5d690bdfffdcf1c44", 816518),
                }, Core.Language.FR_FRA, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Freddy Pharkas - Windows (supplied by abevi in bug report #2612718)
            // Executable scanning reports "1.cfs.081"
            // SCI interpreter version 1.001.132 (just a guess)
            new ADGameDescription("freddypharkas", "Floppy", new[]
            {
                new ADGameFileDescription("resource.map", 0, "a32674e7fbf7b213b4a066c8037f16b6", 5816),
                new ADGameFileDescription("resource.000", 0, "fed4808fdb72486908ac7ad0044b14d8", 5233230),
            }, Core.Language.EN_ANY, Platform.Windows, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH |
                                                                             GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                             GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                             GAMEOPTION_FB01_MIDI),

            // Freddy Pharkas - German DOS Floppy (from Tobis87, updated information from markcoolio in bug reports #2723772 and #2724720)
            // Executable scanning reports "1.cfs.081"
            // SCI interpreter version 1.001.132 (just a guess)
            new ADGameDescription("freddypharkas", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "a32674e7fbf7b213b4a066c8037f16b6", 5816),
                    new ADGameFileDescription("resource.000", 0, "96b07e9b914dba1c8dc6c78a176326df", 5233230),
                    new ADGameFileDescription("resource.msg", 0, "304b5a5781800affd2235152a5794fa8", -1),
                }, Core.Language.DE_DEU, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Freddy Pharkas - Spanish DOS (from jvprat)
            // Executable scanning reports "1.cfs.081", VERSION file reports "1.000, March 30, 1995"
            // SCI interpreter version 1.001.132 (just a guess)
            new ADGameDescription("freddypharkas", "CD", new[]
            {
                new ADGameFileDescription("resource.map", 0, "a32674e7fbf7b213b4a066c8037f16b6", 5816),
                new ADGameFileDescription("resource.000", 0, "fed4808fdb72486908ac7ad0044b14d8", 1456640),
                new ADGameFileDescription("resource.001", 0, "15298fac241b5360763bfb68add1db07", 1456640),
                new ADGameFileDescription("resource.002", 0, "419dbd5366f702b4123dedbbb0cffaae", 1456640),
                new ADGameFileDescription("resource.003", 0, "05acdc256c742e79c50b9fe7ec2cc898", 863310),
                new ADGameFileDescription("resource.msg", 0, "45b5bf74933ac3727e4cc844446dc052", 796156),
            }, Core.Language.ES_ESP, Platform.DOS, ADGameFlags.CD, GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                   GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Freddy Pharkas - Spanish DOS (from jvprat)
            // Executable scanning reports "1.cfs.081", VERSION file reports "1.000, March 30, 1995"
            // SCI interpreter version 1.001.132 (just a guess)
            new ADGameDescription("freddypharkas", "Floppy", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "a32674e7fbf7b213b4a066c8037f16b6", 5816),
                    new ADGameFileDescription("resource.000", 0, "96b07e9b914dba1c8dc6c78a176326df", 5233230),
                    new ADGameFileDescription("resource.msg", 0, "45b5bf74933ac3727e4cc844446dc052", 796156),
                }, Core.Language.ES_ESP, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Freddy Pharkas - English DOS CD Demo
            // SCI interpreter version 1.001.095
            new ADGameDescription("freddypharkas", "CD Demo", new[]
            {
                new ADGameFileDescription("resource.map", 0, "a62a7eae85dd1e6b07f39662b278437e", 1918),
                new ADGameFileDescription("resource.000", 0, "4962a3c4dd44e36e78ea4a7a374c2220", 957382),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO, GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                     GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Freddy Pharkas - English Macintosh
            new ADGameDescription("freddypharkas", "", new[]
            {
                new ADGameFileDescription("Data1", 0, "ef7cbd62727989818f1cfae69c9fd61d", 3038492),
                new ADGameFileDescription("Data2", 0, "2424b418f7d52c385cea4701f529c69a", 4721732),
            }, Core.Language.EN_ANY, Platform.Macintosh, ADGameFlags.MACRESFORK, GuiOptions.NOSPEECH |
                                                                                 GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                                 GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                                 GAMEOPTION_FB01_MIDI),

            // Fun Seeker's Guide - English DOS
            // SCI interpreter version 0.000.506
            new ADGameDescription("funseeker", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "7ee6859ef74314f6d91938c3595348a9", 282),
                new ADGameFileDescription("resource.001", 0, "f1e680095424e31f7fae1255d36bacba", 40692),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Gabriel Knight - English DOS Demo
            // SCI interpreter version 1.001.092
            // Note: we are not using ADGF_DEMO here, to avoid a game ID like gk1demo-demo
            new ADGameDescription("gk1demo", "Demo", new[]
            {
                new ADGameFileDescription("resource.map", 0, "39645952ae0ed8072c7e838f31b75464", 2490),
                new ADGameFileDescription("resource.000", 0, "eb3ed7477ca4110813fe1fcf35928561", 1718450),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Gabriel Knight - English DOS Demo (from DrMcCoy)
            // SCI interpreter version 1.001.092
            // Note: we are not using ADGF_DEMO here, to avoid a game ID like gk1demo-demo
            new ADGameDescription("gk1demo", "Demo", new[]
            {
                new ADGameFileDescription("resource.map", 0, "8cad2a256f41463030cbb7ea1bfb2857", 2490),
                new ADGameFileDescription("resource.000", 0, "eb3ed7477ca4110813fe1fcf35928561", 1718450),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),
#if ENABLE_SCI32
// Gabriel Knight - English DOS Floppy
// SCI interpreter version 2.000.000
            new ADGameDescription("gk1", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "372d059f75856afa6d73dd84cbb8913d", 10783),
                new ADGameFileDescription("resource.000", 0, "69b7516962510f780d38519cc15fcc7c", 13022630),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.UNSTABLE, GUIO_GK1_FLOPPY),

            // Gabriel Knight - English DOS Floppy (supplied my markcoolio in bug report #2723777)
            // SCI interpreter version 2.000.000
            new ADGameDescription("gk1", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "65e8c14092e4c9b3b3538b7602c8c5ec", 10783),
                new ADGameFileDescription("resource.000", 0, "69b7516962510f780d38519cc15fcc7c", 13022630),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.UNSTABLE, GUIO_GK1_FLOPPY),

            // Gabriel Knight - English DOS Floppy
            // SCI interpreter version 2.000.000, VERSION file reports "1.0\nGabriel Knight\n11/22/10:33 pm\n\x1A"
            new ADGameDescription("gk1", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "ef41df08cf2c1f680216cdbeed0f8311", 10783),
                new ADGameFileDescription("resource.000", 0, "69b7516962510f780d38519cc15fcc7c", 13022630),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.UNSTABLE, GUIO_GK1_FLOPPY),

            // Gabriel Knight - German DOS Floppy (supplied my markcoolio in bug report #2723775)
            // SCI interpreter version 2.000.000
            new ADGameDescription("gk1", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "ad6508b0296b25c07b1f58828dc33696", 10789),
                new ADGameFileDescription("resource.000", 0, "091cf08910780feabc56f8551b09cb36", 13077029),
            }, Core.Language.DE_DEU, Platform.DOS, ADGameFlags.UNSTABLE, GUIO_GK1_FLOPPY),

            // Gabriel Knight - French DOS Floppy (supplied my kervala in bug report #3611487)
            // SCI interpreter version 2.000.000
            new ADGameDescription("gk1", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "236e36cc847cdeafdd5e5fa8cba916ed", 10801),
                new ADGameFileDescription("resource.000", 0, "091cf08910780feabc56f8551b09cb36", 13033072),
            }, Core.Language.FR_FRA, Platform.DOS, ADGameFlags.UNSTABLE, GUIO_GK1_FLOPPY),

            // Gabriel Knight - English DOS CD (from jvprat)
            // Executable scanning reports "2.000.000", VERSION file reports "01.100.000"
            new ADGameDescription("gk1", "CD", new[]
            {
                new ADGameFileDescription("resource.map", 0, "372d059f75856afa6d73dd84cbb8913d", 10996),
                new ADGameFileDescription("resource.000", 0, "69b7516962510f780d38519cc15fcc7c", 12581736),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.CD | ADGameFlags.UNSTABLE, GUIO_GK1_CD),

            // Gabriel Knight - English Windows CD (from jvprat)
            // Executable scanning reports "2.000.000", VERSION file reports "01.100.000"
            new ADGameDescription("gk1", "CD", new[]
            {
                new ADGameFileDescription("resource.map", 0, "372d059f75856afa6d73dd84cbb8913d", 10996),
                new ADGameFileDescription("resource.000", 0, "69b7516962510f780d38519cc15fcc7c", 12581736),
            }, Core.Language.EN_ANY, Platform.Windows, ADGameFlags.CD | ADGameFlags.UNSTABLE, GUIO_GK1_CD),

            // Gabriel Knight - German DOS CD (from Tobis87)
            // SCI interpreter version 2.000.000
            new ADGameDescription("gk1", "CD", new[]
            {
                new ADGameFileDescription("resource.map", 0, "a7d3e55114c65647310373cb390815ba", 11392),
                new ADGameFileDescription("resource.000", 0, "091cf08910780feabc56f8551b09cb36", 13400497),
            }, Core.Language.DE_DEU, Platform.DOS, ADGameFlags.CD | ADGameFlags.UNSTABLE, GUIO_GK1_CD),

            // Gabriel Knight - Spanish DOS CD (from jvprat)
            // Executable scanning reports "2.000.000", VERSION file reports "1.000.000, April 13, 1995"
            new ADGameDescription("gk1", "CD", new[]
            {
                new ADGameFileDescription("resource.map", 0, "7cb6e9bba15b544ec7a635c45bde9953", 11404),
                new ADGameFileDescription("resource.000", 0, "091cf08910780feabc56f8551b09cb36", 13381599),
            }, Core.Language.ES_ESP, Platform.DOS, ADGameFlags.CD | ADGameFlags.UNSTABLE, GUIO_GK1_CD),

            // Gabriel Knight - French DOS CD (from Hkz)
            // VERSION file reports "1.000.000, May 3, 1994"
            new ADGameDescription("gk1", "CD", new[]
            {
                new ADGameFileDescription("resource.map", 0, "55f909ba93a2515042a08d8a2da8414e", 11392),
                new ADGameFileDescription("resource.000", 0, "091cf08910780feabc56f8551b09cb36", 13325145),
            }, Core.Language.FR_FRA, Platform.DOS, ADGameFlags.CD | ADGameFlags.UNSTABLE, GUIO_GK1_CD),

            // Gabriel Knight - German Windows CD (from Tobis87)
            // SCI interpreter version 2.000.000
            new ADGameDescription("gk1", "CD", new[]
            {
                new ADGameFileDescription("resource.map", 0, "a7d3e55114c65647310373cb390815ba", 11392),
                new ADGameFileDescription("resource.000", 0, "091cf08910780feabc56f8551b09cb36", 13400497),
            }, Core.Language.DE_DEU, Platform.Windows, ADGameFlags.CD | ADGameFlags.UNSTABLE, GUIO_GK1_CD),

            // Gabriel Knight - Spanish Windows CD (from jvprat)
            // Executable scanning reports "2.000.000", VERSION file reports "1.000.000, April 13, 1995"
            new ADGameDescription("gk1", "CD", new[]
            {
                new ADGameFileDescription("resource.map", 0, "7cb6e9bba15b544ec7a635c45bde9953", 11404),
                new ADGameFileDescription("resource.000", 0, "091cf08910780feabc56f8551b09cb36", 13381599),
            }, Core.Language.ES_ESP, Platform.Windows, ADGameFlags.CD | ADGameFlags.UNSTABLE, GUIO_GK1_CD),

            // Gabriel Knight - English Macintosh
            new ADGameDescription("gk1", "", new[]
            {
                new ADGameFileDescription("Data1", 0, "044d3bcd7e5b5bb0393d954ade8053fe", 5814918),
                new ADGameFileDescription("Data2", 0, "99a0c63febf9e44e12a00f99c00eae0f", 6685352),
                new ADGameFileDescription("Data3", 0, "f25068b408b09275d8b698866462f578", 3677599),
                new ADGameFileDescription("Data4", 0, "1cceebbe411b26c860a74f91c337fdf3", 3230086),
            }, Core.Language.EN_ANY, Platform.Macintosh, ADGameFlags.MACRESFORK | ADGameFlags.UNSTABLE, GUIO_GK1_CD),

            // Gabriel Knight 2 - English Windows Non-Interactive Demo
            // Executable scanning reports "2.100.002"
            new ADGameDescription("gk2", "Demo", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "e0effce11c4908f4b91838741716c83d", 1351),
                    new ADGameFileDescription("resource.000", 0, "d04cfc7f04b6f74d13025378be49ec2b", 4640330),
                }, Core.Language.EN_ANY, Platform.Windows, ADGameFlags.DEMO | ADGameFlags.UNSTABLE, GUIO_GK2_DEMO),

        // using Enrico Rolfi's HD/DVD installer: http://gkpatches.vogons.zetafleet.com/
            new ADGameDescription("gk2", "", new[]
                {
                    new ADGameFileDescription("resmap.000", 0, "b996fa1e57389a1e179a00a0049de1f4", 8110),
                    new ADGameFileDescription("ressci.000", 0, "a19fc3604c6e5407abcf03d59ee87217", 168522221),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.UNSTABLE, GUIO_GK2),

            // Gabriel Knight 2 - English DOS (from jvprat)
            // Executable scanning reports "2.100.002", VERSION file reports "1.1"
            new ADGameDescription("gk2", "", new[]
                {
                    new ADGameFileDescription("resmap.001", 0, "1b8bf6a23b37ed67358eb825fc687260", 2776),
                    new ADGameFileDescription("ressci.001", 0, "24463ae235b1afbbc4ff5e2ed1b8e3b2", 50496082),
                    new ADGameFileDescription("resmap.002", 0, "2028230674bb54cd24370e0745e7a9f4", 1975),
                    new ADGameFileDescription("ressci.002", 0, "f0edc1dcd704bd99e598c5a742dc7150", 42015676),
                    new ADGameFileDescription("resmap.003", 0, "51f3372a2133c406719dafad86369be3", 1687),
                    new ADGameFileDescription("ressci.003", 0, "86cb3f3d176994e7f8a9ad663a4b907e", 35313750),
                    new ADGameFileDescription("resmap.004", 0, "0f6e48f3e84e867f7d4a5215fcff8d5c", 2719),
                    new ADGameFileDescription("ressci.004", 0, "4f30aa6e6f895132402c8652f9e1d741", 58317316),
                    new ADGameFileDescription("resmap.005", 0, "2dac0e232262b4a51271fd28559b3e70", 2065),
                    new ADGameFileDescription("ressci.005", 0, "14b62d4a3bddee57a03cb1495a798a0f", 38075705),
                    new ADGameFileDescription("resmap.006", 0, "ce9359037277b7d7976da185c2fa0aad", 2977),
                    new ADGameFileDescription("ressci.006", 0, "8e44e03890205a7be12f45aaba9644b4", 60659424),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.UNSTABLE, GUIO_GK2),

            // Gabriel Knight 2 - French DOS (6-CDs Sierra Originals reedition)
            // Executable scanning reports "2.100.002", VERSION file reports "1.0"
            new ADGameDescription("gk2", "", new[]
                {
                    new ADGameFileDescription("resmap.001", 0, "5752eb78e0dffd6ad1d6ada75fe1222e", 2800),
                    new ADGameFileDescription("ressci.001", 0, "37d2df0e1ec0603b605d0c87f1c09ce5", 50810410),
                    new ADGameFileDescription("resmap.002", 0, "1ca433e4bc26383ff134a817386b723e", 1987),
                    new ADGameFileDescription("ressci.002", 0, "5d07e6b51afaa3a5850b17a3dbd800a0", 41367424),
                    new ADGameFileDescription("resmap.003", 0, "27b15dea1f9c73e1f5b57467c2d98b80", 1699),
                    new ADGameFileDescription("ressci.003", 0, "93c561e5d49a804deed4ea4c2eda7386", 35200452),
                    new ADGameFileDescription("resmap.004", 0, "9e5aaa053785d1ea61b1448df930db1a", 2743),
                    new ADGameFileDescription("ressci.004", 0, "5d07e6b51afaa3a5850b17a3dbd800a0", 58988750),
                    new ADGameFileDescription("resmap.005", 0, "6b1f4b59a7af58e1aff21259cc457851", 2077),
                    new ADGameFileDescription("ressci.005", 0, "1eb5a72744799f5a5518543f5b4c3c79", 37882126),
                    new ADGameFileDescription("resmap.006", 0, "11b2e722170b8c93fdaa5428e2c7676f", 3001),
                    new ADGameFileDescription("ressci.006", 0, "4037d941aec39d2e654e20960429aefc", 60568486),
                }, Core.Language.FR_FRA, Platform.DOS, ADGameFlags.UNSTABLE, GUIO_GK2),

            // Gabriel Knight 2 - English Macintosh
            // NOTE: This only contains disc 1 files (as well as the persistent file:
            // Data1. Other discs have conflicting names :(
            new ADGameDescription("gk2", "", new[]
                {
                    new ADGameFileDescription("Data1", 0, "81cb3b4461af845efc59450a74b49fe6", 693041),
                    new ADGameFileDescription("Data2", 0, "69a05445a7c8c2da06d8f5a70200974d", 16774575),
                    new ADGameFileDescription("Data3", 0, "256309284f6447aaa5028103753e7e78", 15451830),
                    new ADGameFileDescription("Data4", 0, "8b843c62eb53136a855d6e0087e3cb0d", 5889553),
                    new ADGameFileDescription("Data5", 0, "f9fcf9ab2eb13b2125c33a1cda03a093", 14349984),
                }, Core.Language.EN_ANY, Platform.Macintosh, ADGameFlags.MACRESFORK|ADGameFlags.UNSTABLE, GUIO_GK2_MAC),
#endif // ENABLE_SCI32

            // Hoyle 1 - English DOS (supplied by ssburnout in bug report #3049193)
            // 1.000.104 3x5.25" (label:INT.0.000.519)
            new ADGameDescription("hoyle1", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "d6c37503a8f282636e1b08f7a6cf4afd", 7818),
                new ADGameFileDescription("resource.001", 0, "e0dd44069a62a463fd124974b915f10d", 162805),
                new ADGameFileDescription("resource.002", 0, "e0dd44069a62a463fd124974b915f10d", 342149),
                new ADGameFileDescription("resource.003", 0, "e0dd44069a62a463fd124974b915f10d", 328925),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Hoyle 1 - English DOS (supplied by wibble92 in bug report #2644547)
            // SCI interpreter version 0.000.530
            new ADGameDescription("hoyle1", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "9de9aa6d23569b3c8bf798503cf1216a", 7818),
                    new ADGameFileDescription("resource.001", 0, "e0dd44069a62a463fd124974b915f10d", 162783),
                    new ADGameFileDescription("resource.002", 0, "e0dd44069a62a463fd124974b915f10d", 342309),
                    new ADGameFileDescription("resource.003", 0, "e0dd44069a62a463fd124974b915f10d", 328912),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 1 - English DOS (supplied by merkur in bug report #2719227)
            // SCI interpreter version 0.000.530
            new ADGameDescription("hoyle1", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "1034a218943d12f1f36e753fa10c95b8", 4386),
                    new ADGameFileDescription("resource.001", 0, "e0dd44069a62a463fd124974b915f10d", 518308),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 1 3.5' - English DOS (supplied by eddydrama in bug report #3052366 and dinnerx in bug report #3090841)
            new ADGameDescription("hoyle1", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "0af9a3dcd72a091960de070432e1f524", 4386),
                    new ADGameFileDescription("resource.001", 0, "e0dd44069a62a463fd124974b915f10d", 518127),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 1 - English DOS v1.000.115 (supplied by misterhands in bug report #6597)
            // Executable scanning reports "0.000.668"
            new ADGameDescription("hoyle1", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "3ddf55fdbe14eb0e89a27a2cfc1338bd", 4386),
                    new ADGameFileDescription("resource.001", 0, "e0dd44069a62a463fd124974b915f10d", 519525),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 1 - English Amiga (from www.back2roots.org - verified by waltervn in bug report #6870)
            // Game version 1.000.139, SCI interpreter version x.yyy.zzz
            new ADGameDescription("hoyle1", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "2a72b1aba65fa6e339370eb86d8601d1", 5166),
                    new ADGameFileDescription("resource.001", 0, "e0dd44069a62a463fd124974b915f10d", 218755),
                    new ADGameFileDescription("resource.002", 0, "e0dd44069a62a463fd124974b915f10d", 439502),
                }, Core.Language.EN_ANY, Platform.Amiga, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 1 - English Atari ST
            // Game version 1.000.104, SCI interpreter version 1.002.024
            new ADGameDescription("hoyle1", "", new[]
                {
                    new ADGameFileDescription("resource.001", 0, "e0dd44069a62a463fd124974b915f10d", 518127),
                    new ADGameFileDescription("resource.map", 0, "0af9a3dcd72a091960de070432e1f524", 4386),
                }, Core.Language.EN_ANY, Platform.AtariST, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 1 - English Atari ST
            // Game version 1.000.108, SCI interpreter version 1.002.026
            new ADGameDescription("hoyle1", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "ed8355f84752e49ffa1f0cf9eca4b28e", 4140),
                    new ADGameFileDescription("resource.001", 0, "e0dd44069a62a463fd124974b915f10d", 517454),
                }, Core.Language.EN_ANY, Platform.AtariST, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 2 - English DOS
            // SCI interpreter version 0.000.572
            new ADGameDescription("hoyle2", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "4f894d203f64aa23d9ff64d30ae36926", 2100),
                    new ADGameFileDescription("resource.001", 0, "8f2dd70abe01112eca464cda818b5eb6", 98138),
                    new ADGameFileDescription("resource.002", 0, "8f2dd70abe01112eca464cda818b5eb6", 196631),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 2 - English DOS (supplied by ssburnout in bug report #3049193)
            // 1.000.011 1x3.5" (label:Int#6.21.90)
            new ADGameDescription("hoyle2", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "db0ba08b953e9904a4960ad99cd29c20", 1356),
                    new ADGameFileDescription("resource.001", 0, "8f2dd70abe01112eca464cda818b5eb6", 216315),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 2 - English DOS (supplied by m_kiewitz)
            // SCI interpreter version 0.000.668, Ver 1.000.014, 2x5.25"
            new ADGameDescription("hoyle2", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "8cef06c93d17d96f44aacd5902d84b30", 2100),
                    new ADGameFileDescription("resource.001", 0, "8f2dd70abe01112eca464cda818b5eb6", 98289),
                    new ADGameFileDescription("resource.002", 0, "8f2dd70abe01112eca464cda818b5eb6", 197326),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 2 - English DOS (supplied by misterhands in bug report #6598)
            // Game v1.000.016, interpreter 0.000.668, INT #12.5.90
            new ADGameDescription("hoyle2", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "d8758a4eb6f34f6b3130bf25a496d123", 1356),
                    new ADGameFileDescription("resource.001", 0, "8f2dd70abe01112eca464cda818b5eb6", 217880),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 2 - English Amiga (from www.back2roots.org)
            // Executable scanning reports "1.002.032"
            // SCI interpreter version 0.000.685
            new ADGameDescription("hoyle2", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "62ed48d20c580e5a98f102f7cd93706a", 1356),
                    new ADGameFileDescription("resource.001", 0, "8f2dd70abe01112eca464cda818b5eb6", 222704),
                }, Core.Language.EN_ANY, Platform.Amiga, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 2 - English Atari ST
            // Game version 1.001.017
            // Executable scanning reports "1.002.034"
            new ADGameDescription("hoyle2", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "13c8cc977598b6ad61d24c6296a090fd", 1356),
                    new ADGameFileDescription("resource.001", 0, "8f2dd70abe01112eca464cda818b5eb6", 216280),
                }, Core.Language.EN_ANY, Platform.AtariST, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Hoyle 2 - English Macintosh
            // Executable scanning reports "x.yyy.zzz"
            new ADGameDescription("hoyle2", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "1af1d3aa3cf564f93477c9f87e53f495", 1728),
                    new ADGameFileDescription("resource.001", 0, "b73b8131669d69d41a326415e4519138", 482882),
                }, Core.Language.EN_ANY, Platform.Macintosh, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // King's Quest 1 SCI Remake - English DOS (from the King's Quest Collection)
            // Executable scanning reports "S.old.010", VERSION file reports "1.000.051"
            // SCI interpreter version 0.000.999
            new ADGameDescription("kq1sci", "SCI", new[]
            {
                new ADGameFileDescription("resource.map", 0, "7fe9399a0bec84ca5727309778d27f07", 5790),
                new ADGameFileDescription("resource.001", 0, "fed9e0072ffd511d248674e60dee2099", 555439),
                new ADGameFileDescription("resource.002", 0, "fed9e0072ffd511d248674e60dee2099", 714062),
                new ADGameFileDescription("resource.003", 0, "fed9e0072ffd511d248674e60dee2099", 717478),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // King's Quest 5 - English DOS Floppy
            // SCI interpreter version 1.000.060
            new ADGameDescription("kq5", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "d6172c27b453350e158815fbae23f41e", 8004),
                new ADGameFileDescription("resource.000", 0, "a591bd4b879fc832b8095c0b3befe9e2", 276351),
                new ADGameFileDescription("resource.001", 0, "3f28c72dc7531aaccf8e972c7ee50d14", 1022087),
                new ADGameFileDescription("resource.002", 0, "3e56ba5bf5e8637c619b57f6b6cacbb4", 1307211),
                new ADGameFileDescription("resource.003", 0, "5d5d498f33ca7cde0d5b058630b36ad3", 1347875),
                new ADGameFileDescription("resource.004", 0, "944a996f9cc90dabde9f51ed7dd52366", 1239689),
                new ADGameFileDescription("resource.005", 0, "b6c43441cb78a9b484efc8e614aac092", 1287999),
                new ADGameFileDescription("resource.006", 0, "672ede1136e9e401658538e51bd5dc22", 1172619),
                new ADGameFileDescription("resource.007", 0, "2f48faf27666b58c276dda20f91f4a93", 1240456),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // King's Quest 5 - English DOS Floppy
            // VERSION file reports "0.000.051"
            // Supplied by misterhands in bug report #3536863.
            // This is the original English version, which has been externally patched to
            // Polish in the Polish release below.
            new ADGameDescription("kq5", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "70010c20138541f89013bb5e1b30f16a", 7998),
                new ADGameFileDescription("resource.000", 0, "a591bd4b879fc832b8095c0b3befe9e2", 276398),
                new ADGameFileDescription("resource.001", 0, "c0f48d4a7ebeaa6aa074fc98d77423e9", 1018560),
                new ADGameFileDescription("resource.002", 0, "7f188a95acdb60bbe32a8379ba299393", 1307048),
                new ADGameFileDescription("resource.003", 0, "0860785af59518b94d54718dddcd6907", 1348500),
                new ADGameFileDescription("resource.004", 0, "c4745dd1e261c22daa6477961d08bf6c", 1239887),
                new ADGameFileDescription("resource.005", 0, "6556ff8e7c4d1acf6a78aea154daa76c", 1287869),
                new ADGameFileDescription("resource.006", 0, "da82e4beb744731d0a151f1d4922fafa", 1170456),
                new ADGameFileDescription("resource.007", 0, "431def14ca29cdb5e6a5e84d3f38f679", 1240176),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // King's Quest 5 - English DOS Floppy (supplied by omer_mor in bug report #3036996)
            // VERSION file reports "0.000.051"
            new ADGameDescription("kq5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "8b2158083302568b73b16fa3655360fe", 8184),
                    new ADGameFileDescription("resource.000", 0, "a591bd4b879fc832b8095c0b3befe9e2", 276398),
                    new ADGameFileDescription("resource.001", 0, "c0f48d4a7ebeaa6aa074fc98d77423e9", 1099506),
                    new ADGameFileDescription("resource.002", 0, "e0c40d0e85340357d2404f9b5ae1921c", 1061243),
                    new ADGameFileDescription("resource.003", 0, "89c00d788d022c13a9b250fa96290ab0", 1110169),
                    new ADGameFileDescription("resource.004", 0, "d68f0d8a52ac990aa5641b7087476253", 1153751),
                    new ADGameFileDescription("resource.005", 0, "ef4f1166bc37b6cfab70234ea60ddc3d", 1032675),
                    new ADGameFileDescription("resource.006", 0, "06cb3f689836086ebe08b1efc0126592", 921113),
                    new ADGameFileDescription("resource.007", 0, "252249753c6e850eacceb8af634986d3", 1133608),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // King's Quest 5 EGA (supplied by markcoolio in bug report #2829470)
            // SCI interpreter version 1.000.060
            // VERSION file reports "0.000.055"
            new ADGameDescription("kq5", "EGA", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "baf888a4e4797ce0de0b19d4e183583c", 7662),
                    new ADGameFileDescription("resource.000", 0, "a591bd4b879fc832b8095c0b3befe9e2", 394242),
                    new ADGameFileDescription("resource.001", 0, "c1eef048fa9fe76298c2d4705ef9549f", 558362),
                    new ADGameFileDescription("resource.002", 0, "076aa0bf1d8d2c147d64aeffbe2928e5", 597593),
                    new ADGameFileDescription("resource.003", 0, "ecb47cd04d06b2ab2f9f883667db6e81", 487608),
                    new ADGameFileDescription("resource.004", 0, "4d74e8094ff57cea6ee92faf63dbd0af", 621513),
                    new ADGameFileDescription("resource.005", 0, "3cca5b2dae8afe94532edfdc98d7edbe", 669919),
                    new ADGameFileDescription("resource.006", 0, "698c698570cde9015e4d51eb8d2e9db1", 666527),
                    new ADGameFileDescription("resource.007", 0, "703d8df30e89541af337d7706540d5c4", 541743),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // King's Quest 5 EGA 1.2M disk version (from LordHoto)
            // VERSION file reports "0.000.055"
            new ADGameDescription("kq5", "EGA", new[]
                {
                    new ADGameFileDescription("resource.002", 0, "4d74e8094ff57cea6ee92faf63dbd0af", 1195538),
                    new ADGameFileDescription("resource.003", 0, "3cca5b2dae8afe94532edfdc98d7edbe", 1092132),
                    new ADGameFileDescription("resource.000", 0, "a591bd4b879fc832b8095c0b3befe9e2", 413818),
                    new ADGameFileDescription("resource.001", 0, "c1eef048fa9fe76298c2d4705ef9549f", 1162752),
                    new ADGameFileDescription("resource.map", 0, "53206afb4fd73871a484e83acab80f31", 7608),
                    new ADGameFileDescription("resource.004", 0, "83568edf7fde18b3eed988bc5d22ceb1", 1188053),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI)
            ,

            // King's Quest 5 EGA (supplied by omer_mor in bug report #3035421)
            // VERSION file reports "0.000.062"
            new ADGameDescription("kq5", "EGA", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "e17cfb38175382b9188da75c53bbab64", 7656),
                    new ADGameFileDescription("resource.000", 0, "a591bd4b879fc832b8095c0b3befe9e2", 394072),
                    new ADGameFileDescription("resource.001", 0, "c1eef048fa9fe76298c2d4705ef9549f", 561444),
                    new ADGameFileDescription("resource.002", 0, "076aa0bf1d8d2c147d64aeffbe2928e5", 597580),
                    new ADGameFileDescription("resource.003", 0, "ecb47cd04d06b2ab2f9f883667db6e81", 487633),
                    new ADGameFileDescription("resource.004", 0, "4d74e8094ff57cea6ee92faf63dbd0af", 620749),
                    new ADGameFileDescription("resource.005", 0, "3cca5b2dae8afe94532edfdc98d7edbe", 669961),
                    new ADGameFileDescription("resource.006", 0, "698c698570cde9015e4d51eb8d2e9db1", 666541),
                    new ADGameFileDescription("resource.007", 0, "703d8df30e89541af337d7706540d5c4", 541762),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // King's Quest V DOS 0.000.062 EGA (5 x 5.25" disks)
            // Supplied by ssburnout in bug report #3046780
            new ADGameDescription("kq5", "EGA", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "ef4fdc72ca7aef62054e8b075d7960d8", 7596),
                    new ADGameFileDescription("resource.000", 0, "a591bd4b879fc832b8095c0b3befe9e2", 413648),
                    new ADGameFileDescription("resource.001", 0, "c1eef048fa9fe76298c2d4705ef9549f", 1162806),
                    new ADGameFileDescription("resource.002", 0, "4d74e8094ff57cea6ee92faf63dbd0af", 1194799),
                    new ADGameFileDescription("resource.003", 0, "3cca5b2dae8afe94532edfdc98d7edbe", 1092325),
                    new ADGameFileDescription("resource.004", 0, "8e5c1bc4d738cf7316ff506f59d265e2", 1187803),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // King's Quest 5 DOS Spanish Floppy 0.000.062 VGA (5 x 3.5" disks)
            // Supplied by dianiu in bug report #3555646
            new ADGameDescription("kq5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "c09896a2a30c9b002c5cbbc62f5a5c3a", 8169),
                    new ADGameFileDescription("resource.000", 0, "1f1d03aead44da46362ff40c0074a3ec", 335871),
                    new ADGameFileDescription("resource.001", 0, "d1803ad904127ae091edb274ee8c047f", 1180637),
                    new ADGameFileDescription("resource.002", 0, "d9cd5972016f650cc31fb7c2a2b0953a", 1102207),
                    new ADGameFileDescription("resource.003", 0, "829c8caeff793f3cfcea2cb01aaa4150", 965586),
                    new ADGameFileDescription("resource.004", 0, "0bd9e570ee04b025e43d3075998fae5b", 1117965),
                    new ADGameFileDescription("resource.005", 0, "4aaa2e9a69089b9afbaaccbbf2c4e647", 1202936),
                    new ADGameFileDescription("resource.006", 0, "65b520e60c4217e6a6572d9edf77193b", 1141985),
                    new ADGameFileDescription("resource.007", 0, "f42b0100f0a1c30806814f8648b6bc28", 1145583),
                }, Core.Language.ES_ESP, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // King's Quest 5 - German DOS Floppy (supplied by markcoolio in bug report #2727101, also includes english language)
            // SCI interpreter version 1.000.060
            new ADGameDescription("kq5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "bff44f0c326a71b1757c793a02b502d6", 8283),
                    new ADGameFileDescription("resource.000", 0, "d7ed18ec4a5de02a9a57830aa65a600d", 336826),
                    new ADGameFileDescription("resource.001", 0, "b1e5ec6a17be7e75ddb955f6f73191e4", 1136919),
                    new ADGameFileDescription("resource.002", 0, "04a88122db44610a4af019a579ec5ff6", 1340813),
                    new ADGameFileDescription("resource.003", 0, "215bb35acefae75fc80757c717166d7e", 1323916),
                    new ADGameFileDescription("resource.004", 0, "fecdec847e3bd8e3b0f9827900aa95fd", 1331811),
                    new ADGameFileDescription("resource.005", 0, "9c429782d102739f6bbb81e8b953b0cb", 1267525),
                    new ADGameFileDescription("resource.006", 0, "d1a75fdc01840664d00366cff6919366", 1208972),
                    new ADGameFileDescription("resource.007", 0, "c07494f0cce7c05210893938786a955b", 1337361),
                }, Core.Language.DE_DEU, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // King's Quest 5 - French DOS Floppy (from the King's Quest Collector's Edition 1994, also includes english language)
            // Supplied by aroenai in bug report #2812611
            // VERSION file reports "1.000", SCI interpreter version 1.000.784
            new ADGameDescription("kq5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "eb7853832f3bb10900b13b421a0bbe7f", 8283),
                    new ADGameFileDescription("resource.000", 0, "f063775b279208c14a83eda47073be90", 332806),
                    new ADGameFileDescription("resource.001", 0, "3e6add38564250fd1a5bb10593007530", 1136827),
                    new ADGameFileDescription("resource.002", 0, "d9a97a9cf6c79bbe8f19378f6dea45d5", 1343738),
                    new ADGameFileDescription("resource.003", 0, "bef90d755076c110e67ee3e635503f82", 1324811),
                    new ADGameFileDescription("resource.004", 0, "c14dbafcfbe00855ac6b2f2701058047", 1332216),
                    new ADGameFileDescription("resource.005", 0, "f4b31cafc5defac75125c5f7b7f9a31a", 1268334),
                    new ADGameFileDescription("resource.006", 0, "f7dc85307632ef657ceb1651204f6f51", 1210081),
                    new ADGameFileDescription("resource.007", 0, "7db4d0a1d8d547c0019cb7d2a6acbdd4", 1338473),
                }, Core.Language.FR_FRA, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // King's Quest 5 - Italian DOS Floppy (from glorifindel, includes english language)
            // SCI interpreter version 1.000.060
            new ADGameDescription("kq5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "d55c9e83894a0885e37cd79bacf86384", 8283),
                    new ADGameFileDescription("resource.000", 0, "c99bbb11ace4aaacdc98b588a2ecea06", 332246),
                    new ADGameFileDescription("resource.001", 0, "42b98457b1a7282daa27afd89eef53f4", 1136389),
                    new ADGameFileDescription("resource.002", 0, "8cdc160f9dfc84aed7caa6c66fa31000", 1340730),
                    new ADGameFileDescription("resource.003", 0, "d0cb52dc41488c018359aa79a6527f51", 1323676),
                    new ADGameFileDescription("resource.004", 0, "e5c57060adf2b5c6fc24142acba023da", 1331097),
                    new ADGameFileDescription("resource.005", 0, "f4e441f284560eaa8022102315656a7d", 1267757),
                    new ADGameFileDescription("resource.006", 0, "8eeabd92af71e766e323db2100879102", 1209325),
                    new ADGameFileDescription("resource.007", 0, "dc10c107e0923b902326a040b9c166b9", 1337859),
                }, Core.Language.IT_ITA, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // King's Quest 5 - Polish DOS Floppy (supplied by jacek909 in bug report #2725722)
            // SCI interpreter version 1.000.060
            // VERSION file reports "0.000.051".
            // This is actually an English version with external text resource patches (bug #3536863).
            new ADGameDescription("kq5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "70010c20138541f89013bb5e1b30f16a", 7998),
                    new ADGameFileDescription("resource.000", 0, "a591bd4b879fc832b8095c0b3befe9e2", 276398),
                    new ADGameFileDescription("resource.001", 0, "c0f48d4a7ebeaa6aa074fc98d77423e9", 1018560),
                    new ADGameFileDescription("resource.002", 0, "7f188a95acdb60bbe32a8379ba299393", 1307048),
                    new ADGameFileDescription("resource.003", 0, "0860785af59518b94d54718dddcd6907", 1348500),
                    new ADGameFileDescription("resource.004", 0, "c4745dd1e261c22daa6477961d08bf6c", 1239887),
                    new ADGameFileDescription("resource.005", 0, "6556ff8e7c4d1acf6a78aea154daa76c", 1287869),
                    new ADGameFileDescription("resource.006", 0, "da82e4beb744731d0a151f1d4922fafa", 1170456),
                    new ADGameFileDescription("resource.007", 0, "431def14ca29cdb5e6a5e84d3f38f679", 1240176),
                    new ADGameFileDescription("text.000", 0, "601aa35a3ddeb558e1280e0963e955a2", 1517),
                }, Core.Language.PL_POL, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // King's Quest 5 - English Macintosh
            // VERSION file reports "1.000.055"
            new ADGameDescription("kq5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "4d4bd26ac9f3014f5dae6b21cdcde747", 8598),
                    new ADGameFileDescription("resource.000", 0, "e8965601526ce840887b8af3a8593156", 328291),
                    new ADGameFileDescription("resource.001", 0, "aa2fae60f67edf2aacd43b92b59c2b3d", 1071492),
                    new ADGameFileDescription("resource.002", 0, "14311ed6d0f4ae0af7561470953cc466", 1373044),
                    new ADGameFileDescription("resource.003", 0, "aa606e541901b1dd150b49014ace6d11", 1401126),
                    new ADGameFileDescription("resource.004", 0, "bb81f49927cdb0ac4d902e64f2bc40ec", 1377139),
                    new ADGameFileDescription("resource.005", 0, "432e2a58e4d496d730697db072437337", 1366732),
                    new ADGameFileDescription("resource.006", 0, "3d22904a374c192f51e5665b74364133", 1264079),
                    new ADGameFileDescription("resource.007", 0, "ffe17e23d5833a79f3695addfc149a56", 1361965),
                }, Core.Language.EN_ANY, Platform.Macintosh, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD |
                GAMEOPTION_FB01_MIDI),

            // King's Quest 5 - FM-Towns (supplied by abevi in bug report #3038720)
            new ADGameDescription("kq5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "20c7cd248ff1a349ed354568eebd972b", 12733),
                    new ADGameFileDescription("resource.000", 0, "71afd220d46bde1109c58e6acc0f3a01", 469094),
                    new ADGameFileDescription("resource.001", 0, "72a569f46f1abf2d9d2b1526ad3799c3", 12808839),
                }, Core.Language.JA_JPN, Platform.FMTowns, ADGameFlags.ADDENGLISH,
                GuiOptions.NOASPECT | GAMEOPTION_ORIGINAL_SAVELOAD | GuiOptions.MIDITOWNS),

            // King's Quest 5 - Japanese PC-98 Floppy 0.000.015 (supplied by omer_mor in bug report #3073583)
            new ADGameDescription("kq5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "3bca188108ec5b6ad91612483a6cbc27", 7875),
                    new ADGameFileDescription("resource.000", 0, "70d6a2ec17fd49a63217992fc4347cd9", 493681),
                    new ADGameFileDescription("resource.001", 0, "a504e91327a4d51ee4818eb72026dbe9", 950364),
                    new ADGameFileDescription("resource.002", 0, "0750a84ece1d89d3a952e2a2b90b525c", 911833),
                    new ADGameFileDescription("resource.003", 0, "6f8d552b60ec82a165619a99e19c509d", 1078032),
                    new ADGameFileDescription("resource.004", 0, "e114ce8f884601c43308fb5cbbea4874", 1174129),
                    new ADGameFileDescription("resource.005", 0, "349ad9438172265d00680075c5a988d0", 1019669),
                }, Core.Language.JA_JPN, Platform.PC98, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GuiOptions.NOASPECT | GAMEOPTION_PREFER_DIGITAL_SFX | GAMEOPTION_ORIGINAL_SAVELOAD
                | GAMEOPTION_FB01_MIDI),

            // Laura Bow - English DOS (from FRG)
            // SCI interpreter version 0.000.631
            new ADGameDescription("laurabow", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "b1905f6aa68ff65a057b080b1eae954c", 12030),
                new ADGameFileDescription("resource.001", 0, "e45c888d9c7c04aec0a20e9f820b79ff", 108032),
                new ADGameFileDescription("resource.002", 0, "e45c888d9c7c04aec0a20e9f820b79ff", 354680),
                new ADGameFileDescription("resource.003", 0, "e45c888d9c7c04aec0a20e9f820b79ff", 361815),
                new ADGameFileDescription("resource.004", 0, "e45c888d9c7c04aec0a20e9f820b79ff", 339714),
                new ADGameFileDescription("resource.005", 0, "e45c888d9c7c04aec0a20e9f820b79ff", 327465),
                new ADGameFileDescription("resource.006", 0, "e45c888d9c7c04aec0a20e9f820b79ff", 328390),
                new ADGameFileDescription("resource.007", 0, "e45c888d9c7c04aec0a20e9f820b79ff", 317687),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Larry 1 EGA Remake - English DOS (from spookypeanut)
            // SCI interpreter version 0.000.510 (or 0.000.577?)
            new ADGameDescription("lsl1sci", "SCI/EGA", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "abc0dc50c55de5b9723bb6de193f8756", 3282),
                    new ADGameFileDescription("resource.000", 0, "d3bceaebef3f7be941c2038b3565161e", 451366),
                    new ADGameFileDescription("resource.001", 0, "38936d3c68b6f79d3ffb13955713fed7", 591352),
                    new ADGameFileDescription("resource.002", 0, "24c958bc922b07f91e25e8c93aa01fcf", 491230),
                    new ADGameFileDescription("resource.003", 0, "685cd6c1e05a695ab1e0db826337ee2a", 553279),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 1 Remake - English Amiga
            // Executable scanning reports "1.004.024"
            // SCI interpreter version 1.000.784
            // NOTE: The resource.002 file, contained in disk 3, is broken in the
            // www.back2roots.org version (it contains a large chunk of zeroes and
            // several broken resources, e.g. pic 250 and views 250 and 251).
            new ADGameDescription("lsl1sci", "SCI", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "7d115a9e27dc8ac71e8d5ef33d589bd5", 3366),
                    new ADGameFileDescription("resource.000", 0, "e67fd129d5810fc7ad8ea509d891cc00", 363073),
                    new ADGameFileDescription("resource.001", 0, "24ed6dc01b1e7fbc66c3d63a5994549a", 750465),
                    new ADGameFileDescription("resource.002", 0, "5790ac0505f7ca98d4567132b875eb1e", 681041),
                    new ADGameFileDescription("resource.003", 0, "4a34c3367c2fe7eb380d741374da1989", 572251),
                }, Core.Language.EN_ANY, Platform.Amiga, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 1 VGA Remake - English DOS (from spookypeanut)
            // Executable scanning reports "1.000.577", VERSION file reports "2.1"
            new ADGameDescription("lsl1sci", "SCI", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "6d04d26466337a1a64b8c6c0eb65c9a9", 3222),
                    new ADGameFileDescription("resource.000", 0, "d3bceaebef3f7be941c2038b3565161e", 922406),
                    new ADGameFileDescription("resource.001", 0, "ec20246209d7b19f38989261e5c8f5b8", 1111226),
                    new ADGameFileDescription("resource.002", 0, "85d6935ef77e6b0e16bc307640a0d913", 1088312),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 1 VGA Remake - English DOS (from FRG)
            // SCI interpreter version 1.000.510
            new ADGameDescription("lsl1sci", "SCI", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "8606b083b011a0cc4a1fbfc2198a0a77", 3198),
                    new ADGameFileDescription("resource.000", 0, "d3bceaebef3f7be941c2038b3565161e", 918242),
                    new ADGameFileDescription("resource.001", 0, "d34cadb11e1aefbb497cf91bc1d3baa7", 1114688),
                    new ADGameFileDescription("resource.002", 0, "85b030bb66d5342b0a068f1208c431a8", 1078443),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 1 VGA Remake - English Macintosh (from omer_mor, bug report #3328262)
            new ADGameDescription("lsl1sci", "SCI", new[]
            {
                new ADGameFileDescription("resource.map", 0, "6395e7f7881e37e39d81ff5175a35f6f", 3237),
                new ADGameFileDescription("resource.000", 0, "5933df4ea688584d6f59fdea5a9404f8", 989066),
                new ADGameFileDescription("resource.001", 0, "aa6f153f70f1e32d1bde465fff08eecf", 1137418),
                new ADGameFileDescription("resource.002", 0, "b22c616aa789ebef990290c7ffd86548", 1097477),
            }, Core.Language.EN_ANY, Platform.Macintosh, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH |
                                                                               GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                               GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                               GAMEOPTION_FB01_MIDI),

            // Larry 1 VGA Remake - English DOS Non-Interactive Demo
            // SCI interpreter version 1.000.084
            new ADGameDescription("lsl1sci", "SCI/Demo", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "434e1f6c39d71647b34f0ee57b2bbd68", 444),
                    new ADGameFileDescription("resource.001", 0, "0c0768215c562d9dace4a5ca53696cf3", 359913),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 1 VGA Remake - Spanish DOS (from the Leisure Suit Larry Collection, also includes english language)
            // Executable scanning reports "1.SQ4.057", VERSION file reports "1.000"
            // This version is known to be corrupted
            // SCI interpreter version 1.000.510
            new ADGameDescription("lsl1sci", "SCI", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "4fbe5c25878d51d7b2a68b710de4491b", 3327),
                    new ADGameFileDescription("resource.000", 0, "5e501a9bf8c753bf4c96158042422f00", 839172),
                    new ADGameFileDescription("resource.001", 0, "112648995dbc194037f1e4ed2e195910", 1063341),
                    new ADGameFileDescription("resource.002", 0, "3fe2a3aec0ed53c7d6db1845a67e3aa2", 1095908),
                    new ADGameFileDescription("resource.003", 0, "ac175df0ea9a2cba57f0248651856d27", 376556),
                }, Core.Language.ES_ESP, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 1 VGA Remake - Russian DOS (also includes english language?!)
            // Executable scanning reports "1.000.510", VERSION file reports "2.0"
            // SCI interpreter version 1.000.510
            new ADGameDescription("lsl1sci", "SCI", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "b54413d35e206d21ae2b2bdb092bd13a", 3198),
                    new ADGameFileDescription("resource.000", 0, "0d7b2afa666bd36d9535a15d3a837a66", 928566),
                    new ADGameFileDescription("resource.001", 0, "bc8ca10c807515d959cbd91f9ba47735", 1123759),
                    new ADGameFileDescription("resource.002", 0, "b7409ab32bc3bee2d6cce887cd33f2b6", 1092160),
                }, Core.Language.RU_RUS, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 1 VGA Remake - Polish DOS (from Polish Leisure Suit Larry Collection, official release)
            // SCI interpreter version 1.000.577, VERSION file reports "2.1" (this release does NOT include english text)
            new ADGameDescription("lsl1sci", "SCI", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "58330a85767e42a2487129913283ab5b", 3228),
                    new ADGameFileDescription("resource.000", 0, "b6097ff35cdc8469f02150fe2f824198", 4781210),
                }, Core.Language.PL_POL, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 2 - English DOS
            // SCI interpreter version 0.000.409
            new ADGameDescription("lsl2", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "42258cf767a8ebaa9e66b6151a80e601", 5628),
                new ADGameFileDescription("resource.001", 0, "4a24443a25e2b1492462a52809605dc2", 143847),
                new ADGameFileDescription("resource.002", 0, "4a24443a25e2b1492462a52809605dc2", 348331),
                new ADGameFileDescription("resource.003", 0, "4a24443a25e2b1492462a52809605dc2", 236550),
                new ADGameFileDescription("resource.004", 0, "4a24443a25e2b1492462a52809605dc2", 204861),
                new ADGameFileDescription("resource.005", 0, "4a24443a25e2b1492462a52809605dc2", 277732),
                new ADGameFileDescription("resource.006", 0, "4a24443a25e2b1492462a52809605dc2", 345683),
            }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GAMEOPTION_EGA_UNDITHER |
                                                                         GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                         GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                         GAMEOPTION_FB01_MIDI),

            // Larry 5 - English Amiga
            // Executable scanning reports "1.004.023"
            // SCI interpreter version 1.000.784
            new ADGameDescription("lsl5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "e36052ae0c8b14d6f074bcb0aee50a38", 6096),
                    new ADGameFileDescription("resource.000", 0, "d8b58ce10de52aa16f8b2006838c4fcc", 310510),
                    new ADGameFileDescription("resource.001", 0, "8caa8fbb50ea43f3efdfb66f1e68998b", 800646),
                    new ADGameFileDescription("resource.002", 0, "abdaa299e00c908052d33cd82eb60e9b", 784576),
                    new ADGameFileDescription("resource.003", 0, "810ad1d61638c27a780576cb09f18ed7", 805941),
                    new ADGameFileDescription("resource.004", 0, "3ce5901f1bc171ac0274d99a4eeb9e57", 623022),
                    new ADGameFileDescription("resource.005", 0, "f8b2d1137bb767e5d232056b99dd69eb", 623621),
                    new ADGameFileDescription("resource.006", 0, "bafc64e3144f115dc58c6aee02de98fb", 715598),
                }, Core.Language.EN_ANY, Platform.Amiga, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 5 - German Amiga (also includes english language)
            // Executable scanning reports "1.004.024"
            // SCI interpreter version 1.000.784
            new ADGameDescription("lsl5", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "863326c2eb5160f0b0960e159e8bf954", 6372),
                new ADGameFileDescription("resource.000", 0, "5113d03db08e3da77a5b61294001331b", 357525),
                new ADGameFileDescription("resource.001", 0, "59eba83ad465b08d763b44f86afa86f6", 837566),
                new ADGameFileDescription("resource.002", 0, "59eba83ad465b08d763b44f86afa86f6", 622229),
                new ADGameFileDescription("resource.003", 0, "59eba83ad465b08d763b44f86afa86f6", 383690),
                new ADGameFileDescription("resource.004", 0, "59eba83ad465b08d763b44f86afa86f6", 654296),
                new ADGameFileDescription("resource.005", 0, "59eba83ad465b08d763b44f86afa86f6", 664717),
                new ADGameFileDescription("resource.006", 0, "bafc64e3144f115dc58c6aee02de98fb", 754966),
                new ADGameFileDescription("resource.007", 0, "59eba83ad465b08d763b44f86afa86f6", 683135),
            }, Core.Language.DE_DEU, Platform.Amiga, ADGameFlags.ADDENGLISH, GuiOptions.NOSPEECH |
                                                                             GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                             GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                             GAMEOPTION_FB01_MIDI),

            // Larry 5 - English DOS Non-Interactive Demo (from FRG)
            // SCI interpreter version 1.000.181
            new ADGameDescription("lsl5", "Demo", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "efe8d3f45ce4f6bd9a6643e0ac8d2a97", 504),
                    new ADGameFileDescription("resource.001", 0, "8bd8d9c0b5f455ee1269d63ce86c50dd", 531380),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 5 - English DOS (from spookypeanut)
            // SCI interpreter version 1.000.510
            new ADGameDescription("lsl5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "be00ef895197754ae4eab021ca44cbcd", 6417),
                    new ADGameFileDescription("resource.000", 0, "f671ab479df0c661b19cd16237692846", 726823),
                    new ADGameFileDescription("resource.001", 0, "db4a1381d88028876a99303bfaaba893", 751296),
                    new ADGameFileDescription("resource.002", 0, "d39d8db1a1e7806e7ccbfea3ef22df44", 1137646),
                    new ADGameFileDescription("resource.003", 0, "13fd4942bb818f9acd2970d66fca6509", 768599),
                    new ADGameFileDescription("resource.004", 0, "999f407c9f38f937d4b8c4230ff5bb38", 1024516),
                    new ADGameFileDescription("resource.005", 0, "0cc8d35a744031c772ca7cd21ae95273", 1011944),
                    new ADGameFileDescription("resource.006", 0, "dda27ce00682aa76198dac124bbbe334", 1024810),
                    new ADGameFileDescription("resource.007", 0, "ac443fae1285fb359bf2b2bc6a7301ae", 1030656),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 5 - English Macintosh (from omer_mor, bug report #3328257)
            new ADGameDescription("lsl5", "", new[]
            {
                new ADGameFileDescription("resource.map", 0, "f12439da78b9878e16436661deb83f84", 6525),
                new ADGameFileDescription("resource.000", 0, "f2537473213d70e7f4fc82e988ab90ca", 702403),
                new ADGameFileDescription("resource.001", 0, "db4a1381d88028876a99303bfaaba893", 704679),
                new ADGameFileDescription("resource.002", 0, "e86aeb27711f4a673e06ec32cfc84125", 1125854),
                new ADGameFileDescription("resource.003", 0, "13fd4942bb818f9acd2970d66fca6509", 854733),
                new ADGameFileDescription("resource.004", 0, "999f407c9f38f937d4b8c4230ff5bb38", 1046644),
                new ADGameFileDescription("resource.005", 0, "0cc8d35a744031c772ca7cd21ae95273", 1008293),
                new ADGameFileDescription("resource.006", 0, "dda27ce00682aa76198dac124bbbe334", 1110043),
                new ADGameFileDescription("resource.007", 0, "ac443fae1285fb359bf2b2bc6a7301ae", 989801),
            }, Core.Language.EN_ANY, Platform.Macintosh, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH |
                                                                               GAMEOPTION_PREFER_DIGITAL_SFX |
                                                                               GAMEOPTION_ORIGINAL_SAVELOAD |
                                                                               GAMEOPTION_FB01_MIDI),

            // Larry 5 - German DOS (from Tobis87)
            // SCI interpreter version T.A00.196
            new ADGameDescription("lsl5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "c97297aa76d4dd2ed144c7b7769e2caf", 6867),
                    new ADGameFileDescription("resource.000", 0, "4c00c14b8181ad47076a51d86097d97e", 759095),
                    new ADGameFileDescription("resource.001", 0, "245c44f8ccd796732e61857e67b30079", 918742),
                    new ADGameFileDescription("resource.002", 0, "e86aeb27711f4a673e06ec32cfc84125", 947382),
                    new ADGameFileDescription("resource.003", 0, "74edc89d8c1cb346ca346081b927e4c6", 1006884),
                    new ADGameFileDescription("resource.004", 0, "999f407c9f38f937d4b8c4230ff5bb38", 1023776),
                    new ADGameFileDescription("resource.005", 0, "0cc8d35a744031c772ca7cd21ae95273", 959342),
                    new ADGameFileDescription("resource.006", 0, "dda27ce00682aa76198dac124bbbe334", 1021774),
                    new ADGameFileDescription("resource.007", 0, "ac443fae1285fb359bf2b2bc6a7301ae", 993408),
                }, Core.Language.DE_DEU, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 5 - French DOS (provided by richiefs in bug report #2670691)
            // Executable scanning reports "1.lsl5.019"
            // SCI interpreter version 1.000.510 (just a guess)
            new ADGameDescription("lsl5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "499898e652dc41b51e368ae41acce41f", 7023),
                    new ADGameFileDescription("resource.000", 0, "4c00c14b8181ad47076a51d86097d97e", 958096),
                    new ADGameFileDescription("resource.001", 0, "245c44f8ccd796732e61857e67b30079", 1196765),
                    new ADGameFileDescription("resource.002", 0, "e86aeb27711f4a673e06ec32cfc84125", 948898),
                    new ADGameFileDescription("resource.003", 0, "74edc89d8c1cb346ca346081b927e4c6", 1006608),
                    new ADGameFileDescription("resource.004", 0, "999f407c9f38f937d4b8c4230ff5bb38", 971293),
                    new ADGameFileDescription("resource.005", 0, "0cc8d35a744031c772ca7cd21ae95273", 920524),
                    new ADGameFileDescription("resource.006", 0, "dda27ce00682aa76198dac124bbbe334", 946540),
                    new ADGameFileDescription("resource.007", 0, "ac443fae1285fb359bf2b2bc6a7301ae", 958842),
                }, Core.Language.FR_FRA, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 5 - Spanish DOS (from the Leisure Suit Larry Collection)
            // Executable scanning reports "1.ls5.006", VERSION file reports "1.000, 4/21/92"
            // SCI interpreter version 1.000.510 (just a guess)
            new ADGameDescription("lsl5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "b6f7da7bf24e5a6b2946032cec3ea59c", 6861),
                    new ADGameFileDescription("resource.000", 0, "4c00c14b8181ad47076a51d86097d97e", 765418),
                    new ADGameFileDescription("resource.001", 0, "245c44f8ccd796732e61857e67b30079", 916028),
                    new ADGameFileDescription("resource.002", 0, "e86aeb27711f4a673e06ec32cfc84125", 929645),
                    new ADGameFileDescription("resource.003", 0, "74edc89d8c1cb346ca346081b927e4c6", 1005496),
                    new ADGameFileDescription("resource.004", 0, "999f407c9f38f937d4b8c4230ff5bb38", 1021996),
                    new ADGameFileDescription("resource.005", 0, "0cc8d35a744031c772ca7cd21ae95273", 958079),
                    new ADGameFileDescription("resource.006", 0, "dda27ce00682aa76198dac124bbbe334", 1015136),
                    new ADGameFileDescription("resource.007", 0, "ac443fae1285fb359bf2b2bc6a7301ae", 987222),
                }, Core.Language.ES_ESP, Platform.DOS, ADGameFlags.ADDENGLISH,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 5 - Italian DOS Floppy (from glorifindel)
            // SCI interpreter version 1.000.510 (just a guess)
            new ADGameDescription("lsl5", "", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "a99776df795127f387cb35dae872d4e4", 5919),
                    new ADGameFileDescription("resource.000", 0, "a8989a5a89e7d4f702b26b378c7a357a", 7001981),
                }, Core.Language.IT_ITA, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 5 1.0 EGA DOS (8 x 3.5" disks)
            // Provided by ssburnout in bug report #3046806
            new ADGameDescription("lsl5", "EGA", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "1370ae356fdda2e7f9ea56dda3ff9a57", 6597),
                    new ADGameFileDescription("resource.000", 0, "f2537473213d70e7f4fc82e988ab90ca", 248416),
                    new ADGameFileDescription("resource.001", 0, "bb642b0b0f879aca98addd62d901387e", 445841),
                    new ADGameFileDescription("resource.002", 0, "c2cb2dec12e26f6243bc1b78e4e84940", 617030),
                    new ADGameFileDescription("resource.003", 0, "f8e876302a3aba5bcaab5c51db6b6532", 682911),
                    new ADGameFileDescription("resource.004", 0, "16f4d8fb1b526125edaca4fc6cbb7530", 530230),
                    new ADGameFileDescription("resource.005", 0, "6043b2cc23d663e6a01b25bd0e4de55e", 576442),
                    new ADGameFileDescription("resource.006", 0, "f6046a8445422f17d40b1b10ab21ebf3", 568551),
                    new ADGameFileDescription("resource.007", 0, "640ee65595d40372ef95462f2c1ae28a", 593429),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),

            // Larry 5 EGA
            // Supplied by omer_mor in bug report #3049771
            new ADGameDescription("lsl5", "EGA", new[]
                {
                    new ADGameFileDescription("resource.map", 0, "89dbf8006985ec0c547ffe125c25ebf9", 6255),
                    new ADGameFileDescription("resource.000", 0, "f2537473213d70e7f4fc82e988ab90ca", 765747),
                    new ADGameFileDescription("resource.001", 0, "bb642b0b0f879aca98addd62d901387e", 1196260),
                    new ADGameFileDescription("resource.002", 0, "5a55af4e40728b1a8103dc47ad2afa8d", 1100539),
                    new ADGameFileDescription("resource.003", 0, "16f4d8fb1b526125edaca4fc6cbb7530", 1064563),
                }, Core.Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS,
                GuiOptions.NOSPEECH | GAMEOPTION_PREFER_DIGITAL_SFX |
                GAMEOPTION_ORIGINAL_SAVELOAD | GAMEOPTION_FB01_MIDI),
        };

        public override string OriginalCopyright => "Sierra's Creative Interpreter (C) Sierra Online";
    }
}