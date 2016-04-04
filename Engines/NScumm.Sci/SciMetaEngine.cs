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
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using NScumm.Core.Input;
using NScumm.Core.IO;
using NScumm.Core.Engines;
using NScumm.Core.Common;
using System.Globalization;
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

        public CultureInfo Culture
        {
            get; set;
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

    class SciSystem : ISystem
    {
        public IGraphicsManager GraphicsManager { get; }
        public IInputManager InputManager { get; }
        public ISaveFileManager SaveFileManager { get; }

        public SciSystem(IGraphicsManager graphicsManager, IInputManager inputManager, ISaveFileManager saveFileManager)
        {
            GraphicsManager = graphicsManager;
            InputManager = inputManager;
            SaveFileManager = saveFileManager;
        }
    }

    public class SciMetaEngine : AdvancedMetaEngine
    {
        public SciMetaEngine()
            : base(SciGameDescriptions)
        {
        }

        public override IEngine Create(GameSettings settings, IGraphicsManager gfxManager, IInputManager inputManager, IAudioOutput output, ISaveFileManager saveFileManager, bool debugMode = false)
        {
            return new SciEngine(new SciSystem(gfxManager, inputManager, saveFileManager), output, (SciGameDescriptor)settings.Game, s_gameIdStrToEnum[settings.Game.Id]);
        }

        protected override GameDetected CreateGameDetected(string path, ADGameDescription desc)
        {
            var gd = new SciGameDescriptor(desc) { Path = path, Description = "TODO", Culture = ToCulture(desc.language), Platform = desc.platform, Id = desc.gameid };
            return new GameDetected(gd, this);
        }

        // Game descriptions
        private static readonly ADGameDescription[] SciGameDescriptions = new[] {
	        // Astro Chicken - English DOS
	        // SCI interpreter version 0.000.453
	        new ADGameDescription {
                gameid = "astrochicken", extra= "",
                filesDescriptions =new []{
                    new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "f3d1be7752d30ba60614533d531e2e98", fileSize = 474 },
                    new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "6fd05926c2199af0af6f72f90d0d7260", fileSize = 126895 },
                }, language = Core.Common.Language.EN_ANY, platform = Platform.DOS
            },

			// Castle of Dr. Brain - English Amiga (from www.back2roots.org)
			// Executable scanning reports "1.005.000"
			// SCI interpreter version 1.000.510
			new ADGameDescription { gameid= "castlebrain", extra= "",
				filesDescriptions =new [] {
					new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "9f9fb826aa7e944b95eadbf568244a68", fileSize = 2766},
					new ADGameFileDescription { fileName = "resource.000", fileType = 0, md5 = "0efa8409c43d42b32642f96652d3230d", fileSize = 314773},
					new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "3fb02ce493f6eacdcc3713851024f80e", fileSize = 559540},
					new ADGameFileDescription { fileName = "resource.002", fileType = 0, md5 = "d226d7d3b4f77c4a566913fc310487fc", fileSize = 792380},
					new ADGameFileDescription { fileName = "resource.003", fileType = 0, md5 = "d226d7d3b4f77c4a566913fc310487fc", fileSize = 464348},
				},
				language = NScumm.Core.Common.Language.EN_ANY, platform =  Platform.Amiga
			},

            // King's Quest 1 SCI Remake - English DOS (from the King's Quest Collection)
	        // Executable scanning reports "S.old.010", VERSION file reports "1.000.051"
	        // SCI interpreter version 0.000.999	
	        new ADGameDescription {
                gameid = "kq1sci", extra = "SCI",
                filesDescriptions = new [] {
                    new ADGameFileDescription { fileName = "resource.map", fileType = 0, md5 = "7fe9399a0bec84ca5727309778d27f07", fileSize = 5790},
                    new ADGameFileDescription { fileName = "resource.001", fileType = 0, md5 = "fed9e0072ffd511d248674e60dee2099", fileSize = 555439},
                    new ADGameFileDescription { fileName = "resource.002", fileType = 0, md5 = "fed9e0072ffd511d248674e60dee2099", fileSize = 714062},
                    new ADGameFileDescription { fileName = "resource.003", fileType = 0, md5 = "fed9e0072ffd511d248674e60dee2099", fileSize = 717478},
                }, language = Core.Common.Language.EN_ANY, platform = Platform.DOS
            }
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
    }
}
