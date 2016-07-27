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
                },
                Core.Language.EN_ANY, Platform.Amiga),

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
