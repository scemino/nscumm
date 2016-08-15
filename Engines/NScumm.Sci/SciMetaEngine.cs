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
    class SciGameDescriptor : IGameDescriptor
    {
        public ADGameDescription GameDescription { get; private set; }

        public SciGameDescriptor(ADGameDescription desc)
        {
            GameDescription = desc;
            Width = 320;
            Height = 200;
        }

        public string Description
        {
            get; set;
        }

        public string Id
        {
            get; set;
        }

        public string Path
        {
            get; set;
        }

        public PixelFormat PixelFormat
        {
            get; set;
        }

        public Platform Platform
        {
            get; set;
        }

        public int Width
        {
            get; set;
        }

        public int Height
        {
            get; set;
        }

        public Core.Language Language
        {
            get; set;
        }
    }

    enum SciGameId
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
        public SciMetaEngine()
            : base(SciGameDescriptions)
        {
        }

        public override IEngine Create(GameSettings settings, ISystem system)
        {
            return new SciEngine(system, settings, (SciGameDescriptor)settings.Game, s_gameIdStrToEnum[settings.Game.Id]);
        }

        protected override IGameDescriptor CreateGameDescriptor(string path, ADGameDescription desc)
        {
            return new SciGameDescriptor(desc) { Path = path, Description = "TODO", Language = desc.language, Platform = desc.platform, Id = desc.gameid };
        }

        // Game descriptions
        private static readonly ADGameDescription[] SciGameDescriptions = {
	        // Astro Chicken - English DOS
	        // SCI interpreter version 0.000.453
	        new ADGameDescription("astrochicken", "",
                new [] {
                    new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "f3d1be7752d30ba60614533d531e2e98", fileSize = 474 },
                    new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "6fd05926c2199af0af6f72f90d0d7260", fileSize = 126895 },
                }, Core.Language.EN_ANY, Platform.DOS),

			// Castle of Dr. Brain - English Amiga (from www.back2roots.org)
			// Executable scanning reports "1.005.000"
			// SCI interpreter version 1.000.510
			new ADGameDescription("castlebrain", "",
                new [] {
                    new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "9f9fb826aa7e944b95eadbf568244a68", fileSize = 2766},
                    new ADGameFileDescription { fileName = "resource.000", fileType = 0, md5 = "0efa8409c43d42b32642f96652d3230d", fileSize = 314773},
                    new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "3fb02ce493f6eacdcc3713851024f80e", fileSize = 559540},
                    new ADGameFileDescription { fileName = "resource.002", fileType = 0, md5 = "d226d7d3b4f77c4a566913fc310487fc", fileSize = 792380},
                    new ADGameFileDescription { fileName = "resource.003", fileType = 0, md5 = "d226d7d3b4f77c4a566913fc310487fc", fileSize = 464348},
                }, Core.Language.EN_ANY, Platform.Amiga),

            // Castle of Dr. Brain - German Amiga (from www.back2roots.org, also includes English language)
            // Executable scanning reports "1.005.001"
            // SCI interpreter version 1.000.510
            new ADGameDescription("castlebrain", "",
                new [] {
                    new ADGameFileDescription("resource.map", 0, "8e60424682db52a982bcc3535a7e86f3", 2796),
                    new ADGameFileDescription("resource.000", 0, "0efa8409c43d42b32642f96652d3230d", 332468),
                    new ADGameFileDescription("resource.001", 0, "4e0836fadc324316c1a418125709ba45", 569057),
                    new ADGameFileDescription("resource.002", 0, "85e51acb5f9c539d66e3c8fe40e17da5", 826309),
                    new ADGameFileDescription("resource.003", 0, "85e51acb5f9c539d66e3c8fe40e17da5", 493638),
                }, Core.Language.DE_DEU, Platform.Amiga),

            // Castle of Dr. Brain Macintosh (from omer_mor, bug report #3328251)
            new ADGameDescription("castlebrain", "",
                new [] {
                    new ADGameFileDescription("resource.map", 0, "75cb06a94d2e0641295edd043f26f3a8", 2763),
                    new ADGameFileDescription("resource.000", 0, "27ec5fa09cd12a7fd16e86d96a2ed245", 476566),
                    new ADGameFileDescription("resource.001", 0, "7f7da982f5cd868e1e608cd4f6515656", 400521),
                    new ADGameFileDescription("resource.002", 0, "e1a6b6f1060f60be9dcb6d28ad7a2a20", 1168310),
                    new ADGameFileDescription("resource.003", 0, "6c3d1bb26ad532c94046bc9ac49b5ff4", 891295),
                }, Core.Language.EN_ANY, Platform.Macintosh),

            // Castle of Dr. Brain - English DOS Non-Interactive Demo
            // SCI interpreter version 1.000.005
            new ADGameDescription("castlebrain", "Demo", new [] {
                new ADGameFileDescription("resource.map", 0, "467bb5e3224bb54640c3280032aebff5", 633),
                new ADGameFileDescription("resource.000", 0, "9780f040d58182994e22d2e34fab85b0", 67367),
                new ADGameFileDescription("resource.001", 0, "2af49dbd8f2e1db4ab09f9310dc91259", 570553),
            }, Core.Language.EN_ANY, Platform.DOS),


            // King's Quest 1 SCI Remake - English DOS (from the King's Quest Collection)
	        // Executable scanning reports "S.old.010", VERSION file reports "1.000.051"
	        // SCI interpreter version 0.000.999	
	        new ADGameDescription("kq1sci", "SCI",
                new [] {
                    new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "7fe9399a0bec84ca5727309778d27f07", fileSize = 5790},
                    new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "fed9e0072ffd511d248674e60dee2099", fileSize = 555439},
                    new ADGameFileDescription { fileName = "resource.002", fileType = 0, md5 = "fed9e0072ffd511d248674e60dee2099", fileSize = 714062},
                    new ADGameFileDescription { fileName = "resource.003", fileType = 0, md5 = "fed9e0072ffd511d248674e60dee2099", fileSize = 717478},
            }, Core.Language.EN_ANY, Platform.DOS),

            // Laura Bow - English DOS (from FRG)
            // SCI interpreter version 0.000.631
            new ADGameDescription("laurabow", "",
                new [] {
                    new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "b1905f6aa68ff65a057b080b1eae954c", fileSize = 12030},
                    new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "e45c888d9c7c04aec0a20e9f820b79ff", fileSize = 108032},
                    new ADGameFileDescription { fileName = "resource.002", fileType = 0, md5 = "e45c888d9c7c04aec0a20e9f820b79ff", fileSize = 354680},
                    new ADGameFileDescription { fileName = "resource.003", fileType = 0, md5 = "e45c888d9c7c04aec0a20e9f820b79ff", fileSize = 361815},
                    new ADGameFileDescription { fileName = "resource.004", fileType = 0, md5 = "e45c888d9c7c04aec0a20e9f820b79ff", fileSize = 339714},
                    new ADGameFileDescription { fileName = "resource.005", fileType = 0, md5 = "e45c888d9c7c04aec0a20e9f820b79ff", fileSize = 327465},
                    new ADGameFileDescription { fileName = "resource.006", fileType = 0, md5 = "e45c888d9c7c04aec0a20e9f820b79ff", fileSize = 328390},
                    new ADGameFileDescription { fileName = "resource.007", fileType = 0, md5 = "e45c888d9c7c04aec0a20e9f820b79ff", fileSize = 317687},
            }, Core.Language.EN_ANY, Platform.DOS),

            // Conquests of the Longbow - English DOS Floppy (from jvprat)
            // Executable scanning reports "1.000.168", Floppy label reports "1.1, 1.13.92", VERSION file reports "1.1"
            // SCI interpreter version 1.000.510
            new ADGameDescription("longbow", "",
                new [] {
                    new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "247f955865572569342751de47e861ab", fileSize = 6027},
                    new ADGameFileDescription { fileName = "resource.000", fileType = 0, md5 = "36e8fda5d0b8c49e587c8a9617959f72", fileSize = 1297120},
                    new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "1e6084a19f7a6c50af88d3a9b32c411e", fileSize = 1366155},
                    new ADGameFileDescription { fileName = "resource.002", fileType = 0, md5 = "7f6ce331219d58d5087731e4475ab4f1", fileSize = 1234743},
                    new ADGameFileDescription { fileName = "resource.003", fileType = 0, md5 = "1867136d01ece57b531032d466910522", fileSize = 823686},
                    new ADGameFileDescription { fileName = "resource.004", fileType = 0, md5 = "9cfce07e204a329e94fda8b5657621da", fileSize = 1261462},
                    new ADGameFileDescription { fileName = "resource.005", fileType = 0, md5 = "21ebe6b39b57a73fc449f67f013765aa", fileSize = 1284720},
            }, Core.Language.EN_ANY, Platform.DOS),

            // Freddy Pharkas - English DOS Floppy (updated information from markcoolio in bug reports #2723773 and #2724720)
            // Executable scanning reports "1.cfs.081"
            // SCI interpreter version 1.001.132 (just a guess)
            new ADGameDescription("freddypharkas", "Floppy",
                new [] {
                    new ADGameFileDescription { fileName = "resource.map",  fileType = 0, md5 = "a32674e7fbf7b213b4a066c8037f16b6", fileSize = 5816},
                    new ADGameFileDescription { fileName = "resource.0 00", fileType = 0, md5 = "96b07e9b914dba1c8dc6c78a176326df", fileSize = 5233230},
                    new ADGameFileDescription { fileName = "resource.msg",  fileType = 0, md5 = "554f65315d851184f6e38211489fdd8f", fileSize = -1},
            },Core.Language.EN_ANY, Platform.DOS),

            // Larry 1 VGA Remake - English DOS (from spookypeanut)
            // Executable scanning reports "1.000.577", VERSION file reports "2.1"
            new ADGameDescription("lsl1sci", "SCI",
                 new [] {
                    new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "6d04d26466337a1a64b8c6c0eb65c9a9", fileSize = 3222},
                    new ADGameFileDescription { fileName = "resource.000", fileType = 0, md5 = "d3bceaebef3f7be941c2038b3565161e", fileSize = 922406},
                    new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "ec20246209d7b19f38989261e5c8f5b8", fileSize = 1111226},
                    new ADGameFileDescription { fileName = "resource.002", fileType = 0, md5 = "85d6935ef77e6b0e16bc307640a0d913", fileSize = 1088312},
            }, Core.Language.EN_ANY, Platform.DOS),

            // Larry 1 VGA Remake - English DOS (from FRG)
            // SCI interpreter version 1.000.510
            new ADGameDescription("lsl1sci", "SCI",
                new [] {
                    new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "8606b083b011a0cc4a1fbfc2198a0a77", fileSize = 3198},
                    new ADGameFileDescription { fileName = "resource.000", fileType = 0, md5 = "d3bceaebef3f7be941c2038b3565161e", fileSize = 918242},
                    new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "d34cadb11e1aefbb497cf91bc1d3baa7", fileSize = 1114688},
                    new ADGameFileDescription { fileName = "resource.002", fileType = 0, md5 = "85b030bb66d5342b0a068f1208c431a8", fileSize = 1078443},
                }, Core.Language.EN_ANY, Platform.DOS),

            // Larry 2 - English DOS
            // SCI interpreter version 0.000.409
            new ADGameDescription("lsl2", "",
                new[] {
                new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "42258cf767a8ebaa9e66b6151a80e601", fileSize = 5628},
                new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "4a24443a25e2b1492462a52809605dc2", fileSize = 143847},
                new ADGameFileDescription { fileName = "resource.002", fileType = 0, md5 = "4a24443a25e2b1492462a52809605dc2", fileSize = 348331},
                new ADGameFileDescription { fileName = "resource.003", fileType = 0, md5 = "4a24443a25e2b1492462a52809605dc2", fileSize = 236550},
                new ADGameFileDescription { fileName = "resource.004", fileType = 0, md5 = "4a24443a25e2b1492462a52809605dc2", fileSize = 204861},
                new ADGameFileDescription { fileName = "resource.005", fileType = 0, md5 = "4a24443a25e2b1492462a52809605dc2", fileSize = 277732},
                new ADGameFileDescription { fileName = "resource.006", fileType = 0, md5 = "4a24443a25e2b1492462a52809605dc2", fileSize = 345683},
            }, Core.Language.EN_ANY, Platform.DOS),

            // Larry 5 - English DOS (from spookypeanut)
            // SCI interpreter version 1.000.510
            new ADGameDescription("lsl5", "",
                new[] {
                    new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "be00ef895197754ae4eab021ca44cbcd", fileSize = 6417},
                    new ADGameFileDescription { fileName = "resource.000", fileType = 0, md5 = "f671ab479df0c661b19cd16237692846", fileSize = 726823},
                    new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "db4a1381d88028876a99303bfaaba893", fileSize = 751296},
                    new ADGameFileDescription { fileName = "resource.002", fileType = 0, md5 = "d39d8db1a1e7806e7ccbfea3ef22df44", fileSize = 1137646},
                    new ADGameFileDescription { fileName = "resource.003",fileType =  0, md5 = "13fd4942bb818f9acd2970d66fca6509", fileSize = 768599},
                    new ADGameFileDescription { fileName = "resource.004", fileType = 0, md5 = "999f407c9f38f937d4b8c4230ff5bb38", fileSize = 1024516},
                    new ADGameFileDescription { fileName = "resource.005", fileType = 0, md5 = "0cc8d35a744031c772ca7cd21ae95273", fileSize = 1011944},
                    new ADGameFileDescription { fileName = "resource.006",fileType =  0, md5 = "dda27ce00682aa76198dac124bbbe334", fileSize = 1024810},
                    new ADGameFileDescription { fileName = "resource.007", fileType = 0, md5 = "ac443fae1285fb359bf2b2bc6a7301ae", fileSize = 1030656},
                }, Core.Language.EN_ANY, Platform.DOS),
        };

        static readonly Dictionary<string, SciGameId> s_gameIdStrToEnum = new Dictionary<string, SciGameId>
        {
            { "astrochicken",    SciGameId.ASTROCHICKEN },
            { "camelot",         SciGameId.CAMELOT },
            { "castlebrain",     SciGameId.CASTLEBRAIN },
            { "chest",           SciGameId.CHEST },
            { "christmas1988",   SciGameId.CHRISTMAS1988 },
            { "christmas1990",   SciGameId.CHRISTMAS1990 },
            { "christmas1992",   SciGameId.CHRISTMAS1992 },
            { "cnick-kq",        SciGameId.CNICK_KQ },
            { "cnick-laurabow",  SciGameId.CNICK_LAURABOW },
            { "cnick-longbow",   SciGameId.CNICK_LONGBOW },
            { "cnick-lsl",       SciGameId.CNICK_LSL },
            { "cnick-sq",        SciGameId.CNICK_SQ },
            { "ecoquest",        SciGameId.ECOQUEST },
            { "ecoquest2",       SciGameId.ECOQUEST2 },
            { "fairytales",      SciGameId.FAIRYTALES },
            { "freddypharkas",   SciGameId.FREDDYPHARKAS },
            { "funseeker",       SciGameId.FUNSEEKER },
            { "gk1",             SciGameId.GK1 },
            { "gk2",             SciGameId.GK2 },
            { "hoyle1",          SciGameId.HOYLE1 },
            { "hoyle2",          SciGameId.HOYLE2 },
            { "hoyle3",          SciGameId.HOYLE3 },
            { "hoyle4",          SciGameId.HOYLE4 },
            { "iceman",          SciGameId.ICEMAN },
            { "islandbrain",     SciGameId.ISLANDBRAIN },
            { "jones",           SciGameId.JONES },
            { "kq1sci",          SciGameId.KQ1 },
            { "kq4sci",          SciGameId.KQ4 },
            { "kq5",             SciGameId.KQ5 },
            { "kq6",             SciGameId.KQ6 },
            { "kq7",             SciGameId.KQ7 },
            { "kquestions",      SciGameId.KQUESTIONS },
            { "laurabow",        SciGameId.LAURABOW },
            { "laurabow2",       SciGameId.LAURABOW2 },
            { "lighthouse",      SciGameId.LIGHTHOUSE },
            { "longbow",         SciGameId.LONGBOW },
            { "lsl1sci",         SciGameId.LSL1 },
            { "lsl2",            SciGameId.LSL2 },
            { "lsl3",            SciGameId.LSL3 },
            { "lsl5",            SciGameId.LSL5 },
            { "lsl6",            SciGameId.LSL6 },
            { "lsl6hires",       SciGameId.LSL6HIRES },
            { "lsl7",            SciGameId.LSL7 },
            { "mothergoose",     SciGameId.MOTHERGOOSE },
            { "mothergoose256",  SciGameId.MOTHERGOOSE256 },
            { "mothergoosehires",SciGameId.MOTHERGOOSEHIRES },
            { "msastrochicken",  SciGameId.MSASTROCHICKEN },
            { "pepper",          SciGameId.PEPPER },
            { "phantasmagoria",  SciGameId.PHANTASMAGORIA },
            { "phantasmagoria2", SciGameId.PHANTASMAGORIA2 },
            { "pq1sci",          SciGameId.PQ1 },
            { "pq2",             SciGameId.PQ2 },
            { "pq3",             SciGameId.PQ3 },
            { "pq4",             SciGameId.PQ4 },
            { "pqswat",          SciGameId.PQSWAT },
            { "qfg1",            SciGameId.QFG1 },
            { "qfg1vga",         SciGameId.QFG1VGA },
            { "qfg2",            SciGameId.QFG2 },
            { "qfg3",            SciGameId.QFG3 },
            { "qfg4",            SciGameId.QFG4 },
            { "rama",            SciGameId.RAMA },
            { "sci-fanmade",     SciGameId.FANMADE },	// FIXME: Do we really need/want this?
	        { "shivers",         SciGameId.SHIVERS },
	        //{ "shivers2",        GID_SHIVERS2 },	// Not SCI
	        { "slater",          SciGameId.SLATER },
            { "sq1sci",          SciGameId.SQ1 },
            { "sq3",             SciGameId.SQ3 },
            { "sq4",             SciGameId.SQ4 },
            { "sq5",             SciGameId.SQ5 },
            { "sq6",             SciGameId.SQ6 },
            { "torin",           SciGameId.TORIN }
        };

        public override string OriginalCopyright
        {
            get
            {
                return "Sierra's Creative Interpreter (C) Sierra Online";
            }
        }
    }
}
