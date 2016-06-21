//
//  MyClass.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core.IO;
using NScumm.Core;

namespace NScumm.Queen
{
    class QueenGameDescriptor : IGameDescriptor
    {
        public string Id
        {
            get
            {
                return "queen";
            }
        }

        public string Description
        {
            get
            {
                return "Flight of the Amazon Queen";
            }
        }

        public Language Language { get; set; }

        public Platform Platform { get; set; }

        public int Width
        {
            get
            {
                return 320;
            }
        }

        public int Height
        {
            get
            {
                return 200;
            }
        }

        public Core.Graphics.PixelFormat PixelFormat
        {
            get
            {
                return Core.Graphics.PixelFormat.Indexed8;
            }
        }

        public string Path { get; set; }
    }

    public class QueenMetaEngine : AdvancedMetaEngine
    {
        public string OriginalCopyright { get { return "Flight of the Amazon Queen (C) John Passfield and Steve Stamatiadis"; } }

        public QueenMetaEngine()
            : base(gameDescriptions)
        {
        }

        public override GameDetected DetectGame(string path)
        {
            var gd = base.DetectGame(path);
            return gd;
        }

        public override IEngine Create(GameSettings settings, ISystem system)
        {
            return new QueenEngine(settings, system);
        }

        protected override IGameDescriptor CreateGameDescriptor(string path, ADGameDescription desc)
        {
            return new QueenGameDescriptor
            {
                Platform = desc.platform,
                Language = desc.language,
                Path = path
            };
        }

        static readonly ADGameDescription[] gameDescriptions = {
            // Amiga Demo - English
            new ADGameDescription(
                "queen",
                "Demo",
                new []{new ADGameFileDescription("queen.1", "f7a1a37ac93bf763b1569231237cb4d8", 563335)},
                Language.EN_ANY,
                Platform.Amiga),

            // Amiga Interview Demo - English
            new ADGameDescription(
                "queen",
                "Interview",
                new []{new ADGameFileDescription("queen.1", "f5d42a18d8f5689480413871410663d7", 597032)},
                Language.EN_ANY,
                Platform.Amiga),

            // DOS Demo - English
            new ADGameDescription(
                "queen",
                "Demo",
                    new []{new ADGameFileDescription("queen.1", "f39334d8133840aa3bcbd733c12937cf", 3732177)},
                Language.EN_ANY,
                Platform.DOS),

            // DOS Interview Demo - English
            new ADGameDescription(
                "queen",
                "Interview",
                new []{new ADGameFileDescription("queen.1", "30b3291f37665bf24d9482b183cb2f67", 1915913)},
                Language.EN_ANY,
                Platform.DOS),

            // PCGAMES DOS Demo - English
            new ADGameDescription(
                "queen",
                "Demo",
                new []{new ADGameFileDescription("queen.1", "f39334d8133840aa3bcbd733c12937cf", 3724538)},
                Language.EN_ANY,
                Platform.DOS),

            // Amiga Floppy - English
            new ADGameDescription(
                "queen",
                "Floppy",
                new []{new ADGameFileDescription("queen.1", "9c209c2cbc1730e3138663c4fd29c2e8", 351775)}, // TODO: Fill in correct MD5
                Language.EN_ANY,
                Platform.Amiga),

            // DOS Floppy - English
            new ADGameDescription(
                "queen",
                "Floppy",
                new []{new ADGameFileDescription("queen.1", "f5e827645d3c887be3bdf4729d847756", 22677657)},
                Language.EN_ANY,
                Platform.DOS),

            // DOS CD - English
            new ADGameDescription(
                "queen",
                "Talkie",
                new []{new ADGameFileDescription("queen.1", "b6302bccf70463de3d5faf0f0628f742", 190787021)},
                Language.EN_ANY,
                Platform.DOS),

            // DOS Floppy - French
            new ADGameDescription(
                "queen",
                "Floppy",
                new []{new ADGameFileDescription("queen.1", "f5e827645d3c887be3bdf4729d847756", 22157304)},
                Language.FR_FRA,
                Platform.DOS),

            // DOS CD - French
            new ADGameDescription(
                "queen",
                "Talkie",
                new []{new ADGameFileDescription("queen.1", "6fd5486a0db75bae2e023b575c3d6a5d", 186689095)},
                Language.FR_FRA,
                Platform.DOS),
    
#if Undefined
            // DOS Floppy - German
            new ADGameDescription(
                "queen",
                "Floppy",
                AD_ENTRY1s("queen.1", NULL, 22240013), // TODO: Fill in correct MD5
                Common::DE_DEU,
                Platform.DOS),
#endif

#if Undefined
            // DOS CD - German
            new ADGameDescription(
                "queen",
                "Talkie",
                AD_ENTRY1s("queen.1", NULL, 217648975), // TODO: Fill in correct MD5
                Common::DE_DEU,
                Platform.DOS),
#endif

#if Undefined
            // DOS CD - Hebrew
            new ADGameDescription(
                "queen",
                "Talkie",
                AD_ENTRY1s("queen.1", NULL, 190705558), // TODO: Fill in correct MD5
                Common::HE_ISR,
                Platform.DOS),
#endif

#if Undefined
            // DOS Floppy - Italian
            new ADGameDescription(
                "queen",
                "Floppy",
                AD_ENTRY1s("queen.1", NULL, 22461366), // TODO: Fill in correct MD5
                Common::IT_ITA,
                Platform.DOS),
#endif

            // DOS CD - Italian
            new ADGameDescription(
                "queen",
                "Talkie",
                new []{new ADGameFileDescription("queen.1", "b6302bccf70463de3d5faf0f0628f742", 190795582)},
                Language.IT_ITA,
                Platform.DOS),

#if Undefined
            // DOS CD - Spanish
            new ADGameDescription(
                "queen",
                "Talkie",
                AD_ENTRY1s("queen.1", NULL, 190730602), // TODO: Fill in correct MD5
                Common::ES_ESP,
                Platform.DOS),
#endif

            // DOS CD - English (Compressed Freeware Release v1.0)
            new ADGameDescription(
                "queen",
                "Talkie",
                new []{new ADGameFileDescription("queen.1c", "a0749bb8b72e537ead1a63a3dde1443d", 54108887)},
                Language.EN_ANY,
                Platform.DOS),

            // DOS CD - English (Compressed Freeware Release v1.1)
            new ADGameDescription(
                "queen",
                "Talkie",
                new []{new ADGameFileDescription("queen.1c", "21fd690b372f8a6289f6f33bc986276c", 51222412)},
                Language.EN_ANY,
                Platform.DOS),

            // DOS CD - French (Compressed Freeware Release v1.0)
            new ADGameDescription(
                "queen",
                "Talkie",
                new []{new ADGameFileDescription("queen.1c", "67e3020f8a35e1df7b1c753b5aaa71e1", 97382620)},
                Language.FR_FRA,
                Platform.DOS),

            // DOS CD - German (Compressed Freeware Release v1.0)
            new ADGameDescription(
                "queen",
                "Talkie",
                new []{new ADGameFileDescription("queen.1c", "28f78dbec7e20f603a10c2f8ea889a5c", 108738717)},
                Language.DE_DEU,
                Platform.DOS),

            // DOS CD - Hebrew (Compressed Freeware Release v1.0)
            new ADGameDescription(
                "queen",
                "Talkie",
                new []{new ADGameFileDescription("queen.1c", "4d52d8780613ef27a2b779caecb20a21", 99391805)},
                Language.HE_ISR,
                Platform.DOS),

            // DOS CD - Italian (Compressed Freeware Release v1.0)
            new ADGameDescription(
                "queen",
                "Talkie",
                new []{new ADGameFileDescription("queen.1c", "2f72b715ed753cf905a37cdcc7ea611e", 98327801)},
                Language.IT_ITA,
                Platform.DOS),

    // TODO: Freeware Release for Spanish DOS CD is missing.
#if Undefined
            // DOS CD - Spanish (Compressed Freeware Release v1.0)
            new ADGameDescription(
                "queen",
                "Talkie",
                new []{new ADGameFileDescription("queen.1c", NULL, ?)},
                Common::ES_ESP,
                Platform.DOS),
#endif
    };

    }
}

