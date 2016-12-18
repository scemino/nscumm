//
//  AgosMetaEngine.cs
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

using NScumm.Core;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    enum GameFileTypes
    {
        GAME_BASEFILE = 1 << 0,
        GAME_ICONFILE = 1 << 1,
        GAME_GMEFILE = 1 << 2,
        GAME_MENUFILE = 1 << 3,
        GAME_STRFILE = 1 << 4,
        GAME_RMSLFILE = 1 << 5,
        GAME_STATFILE = 1 << 6,
        GAME_TBLFILE = 1 << 7,
        GAME_XTBLFILE = 1 << 8,
        GAME_RESTFILE = 1 << 9,
        GAME_TEXTFILE = 1 << 10,
        GAME_VGAFILE = 1 << 11,

        GAME_GFXIDXFILE = 1 << 12
    }

    enum SIMONGameType
    {
        GType_PN = 0,
        GType_ELVIRA1 = 1,
        GType_ELVIRA2 = 2,
        GType_WW = 3,
        GType_SIMON1 = 4,
        GType_SIMON2 = 5,
        GType_FF = 6,
        GType_PP = 7
    }

    enum GameIds
    {
        GID_PN,
        GID_ELVIRA1,
        GID_ELVIRA2,
        GID_WAXWORKS,

        GID_SIMON1,
        GID_SIMON1DOS,
        GID_SIMON1CD32,

        GID_SIMON2,

        GID_FEEBLEFILES,

        GID_DIMP,
        GID_JUMBLE,
        GID_PUZZLE,
        GID_SWAMPY
    }

    enum GameFeatures
    {
        GF_TALKIE = 1 << 0,
        GF_OLD_BUNDLE = 1 << 1,
        GF_CRUNCHED = 1 << 2,
        GF_CRUNCHED_GAMEPC = 1 << 3,
        GF_ZLIBCOMP = 1 << 4,
        GF_32COLOR = 1 << 5,
        GF_EGA = 1 << 6,
        GF_PLANAR = 1 << 7,
        GF_DEMO = 1 << 8,
        GF_PACKED = 1 << 9,
        GF_BROKEN_FF_RATING = 1 << 10
    }

    public class AgosMetaEngine : AdvancedMetaEngine
    {
        private static readonly ADGameDescription[] gameDescriptions =
        {
            // Waxworks - English Amiga Floppy
            new AgosGameDescription(new ADGameDescription("waxworks","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"eca24fe7c3e005caca47cecac56f7245",43392),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"4822a91c18b1b2005ac17fc617f7dcbe",18940),
                        new ADGameFileDescription("menus.dat", (ushort)GameFileTypes.GAME_MENUFILE,"3409eeb8ca8b46fc04da99de67573f5e",320),
                        new ADGameFileDescription("start", (ushort)GameFileTypes.GAME_RESTFILE,"b575b336e741dde1725edd4079d5ab67",20902),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"6faaebff2786216900061eeb978f10af",225),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"95c44bfc380770a6b6dd0dfcc69e80a0",309),
                        new ADGameFileDescription("xtbllist", (ushort)GameFileTypes.GAME_XTBLFILE,"6c7b3db345d46349a5226f695c03e20f",88),
                    }, Language.EN_ANY, Platform.Amiga, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GuiOptions.NOMIDI),
                SIMONGameType.GType_WW, GameIds.GID_WAXWORKS, GameFeatures.GF_OLD_BUNDLE|GameFeatures.GF_CRUNCHED|
                                                              GameFeatures.GF_CRUNCHED_GAMEPC|GameFeatures.GF_PLANAR),

            // Waxworks - German Amiga Floppy
            new AgosGameDescription(new ADGameDescription("waxworks","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"2938a17103de603c4c6f05e6a433b365",44640),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"4822a91c18b1b2005ac17fc617f7dcbe",18940),
                        new ADGameFileDescription("menus.dat", (ushort)GameFileTypes.GAME_MENUFILE,"3409eeb8ca8b46fc04da99de67573f5e",320),
                        new ADGameFileDescription("start", (ushort)GameFileTypes.GAME_RESTFILE,"b575b336e741dde1725edd4079d5ab67",20902),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"6faaebff2786216900061eeb978f10af",225),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"95c44bfc380770a6b6dd0dfcc69e80a0",309),
                        new ADGameFileDescription("xtbllist", (ushort)GameFileTypes.GAME_XTBLFILE,"6c7b3db345d46349a5226f695c03e20f",88),
                    }, Language.DE_DEU, Platform.Amiga, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GuiOptions.NOMIDI),
                SIMONGameType.GType_WW, GameIds.GID_WAXWORKS, GameFeatures.GF_OLD_BUNDLE|GameFeatures.GF_CRUNCHED|
                                                              GameFeatures.GF_CRUNCHED_GAMEPC|GameFeatures.GF_PLANAR),

            // Waxworks - English DOS Floppy Demo
            new AgosGameDescription(new ADGameDescription("waxworks","Non-Interactive Demo",
                    new[]
                    {
                        new ADGameFileDescription("demo", (ushort)GameFileTypes.GAME_BASEFILE,"50704abde6c68a226001400461620129",7238),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"ef1b8ad3494cf103dc10a99fe152ef9a",20901),
                        new ADGameFileDescription("menus.dat", (ushort)GameFileTypes.GAME_MENUFILE,"c4103f122d27677c9db144cae1394a66",2),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO, GuiOptions.NOSPEECH),
                SIMONGameType.GType_WW, GameIds.GID_WAXWORKS, GameFeatures.GF_OLD_BUNDLE|GameFeatures.GF_DEMO),

            // Waxworks - English DOS Floppy
            new AgosGameDescription(new ADGameDescription("waxworks","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"7751e9358e894e32ef40ef3b3bae0f2a",51327),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"ef1b8ad3494cf103dc10a99fe152ef9a",20901),
                        new ADGameFileDescription("menus.dat", (ushort)GameFileTypes.GAME_MENUFILE,"3409eeb8ca8b46fc04da99de67573f5e",320),
                        new ADGameFileDescription("roomslst", (ushort)GameFileTypes.GAME_RMSLFILE,"e3758c46ab8f3c23a1ac012bd607108d",128),
                        new ADGameFileDescription("start", (ushort)GameFileTypes.GAME_RESTFILE,"036b647973d6884cdfc2042a3d12df83",15354),
                        new ADGameFileDescription("statelst", (ushort)GameFileTypes.GAME_STATFILE,"469e98c69f00928a8366ba415d91902d",11104),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"f259e3e07a1cde8d0404a767d815e12c",225),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"95c44bfc380770a6b6dd0dfcc69e80a0",309),
                        new ADGameFileDescription("xtbllist", (ushort)GameFileTypes.GAME_XTBLFILE,"6c7b3db345d46349a5226f695c03e20f",88),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_WW, GameIds.GID_WAXWORKS, GameFeatures.GF_OLD_BUNDLE),

            // Waxworks - French DOS Floppy
            new AgosGameDescription(new ADGameDescription("waxworks","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"7edfdcccbf7627532882192c1a356150",53681),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"ef1b8ad3494cf103dc10a99fe152ef9a",20901),
                        new ADGameFileDescription("menus.dat", (ushort)GameFileTypes.GAME_MENUFILE,"3409eeb8ca8b46fc04da99de67573f5e",320),
                        new ADGameFileDescription("roomslst", (ushort)GameFileTypes.GAME_RMSLFILE,"e3758c46ab8f3c23a1ac012bd607108d",128),
                        new ADGameFileDescription("start", (ushort)GameFileTypes.GAME_RESTFILE,"036b647973d6884cdfc2042a3d12df83",15354),
                        new ADGameFileDescription("statelst", (ushort)GameFileTypes.GAME_STATFILE,"469e98c69f00928a8366ba415d91902d",11104),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"f259e3e07a1cde8d0404a767d815e12c",225),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"95c44bfc380770a6b6dd0dfcc69e80a0",309),
                        new ADGameFileDescription("xtbllist", (ushort)GameFileTypes.GAME_XTBLFILE,"6c7b3db345d46349a5226f695c03e20f",88),
                    }, Language.FR_FRA, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_WW, GameIds.GID_WAXWORKS, GameFeatures.GF_OLD_BUNDLE),

            // Waxworks - German DOS Floppy
            new AgosGameDescription(new ADGameDescription("waxworks","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"32ee34134422e286525c73e71bd0ea2d",53523),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"ef1b8ad3494cf103dc10a99fe152ef9a",20901),
                        new ADGameFileDescription("menus.dat", (ushort)GameFileTypes.GAME_MENUFILE,"3409eeb8ca8b46fc04da99de67573f5e",320),
                        new ADGameFileDescription("roomslst", (ushort)GameFileTypes.GAME_RMSLFILE,"e3758c46ab8f3c23a1ac012bd607108d",128),
                        new ADGameFileDescription("start", (ushort)GameFileTypes.GAME_RESTFILE,"036b647973d6884cdfc2042a3d12df83",15354),
                        new ADGameFileDescription("statelst", (ushort)GameFileTypes.GAME_STATFILE,"469e98c69f00928a8366ba415d91902d",11104),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"f259e3e07a1cde8d0404a767d815e12c",225),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"95c44bfc380770a6b6dd0dfcc69e80a0",309),
                        new ADGameFileDescription("xtbllist", (ushort)GameFileTypes.GAME_XTBLFILE,"6c7b3db345d46349a5226f695c03e20f",88),
                    }, Language.DE_DEU, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_WW, GameIds.GID_WAXWORKS, GameFeatures.GF_OLD_BUNDLE),

            // Waxworks - Spanish DOS Floppy
            new AgosGameDescription(new ADGameDescription("waxworks","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"b0d513069920a5e2eac2ea5d290692f1",53307),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"ef1b8ad3494cf103dc10a99fe152ef9a",20901),
                        new ADGameFileDescription("menus.dat", (ushort)GameFileTypes.GAME_MENUFILE,"3409eeb8ca8b46fc04da99de67573f5e",320),
                        new ADGameFileDescription("roomslst", (ushort)GameFileTypes.GAME_RMSLFILE,"e3758c46ab8f3c23a1ac012bd607108d",128),
                        new ADGameFileDescription("start", (ushort)GameFileTypes.GAME_RESTFILE,"036b647973d6884cdfc2042a3d12df83",15354),
                        new ADGameFileDescription("statelst", (ushort)GameFileTypes.GAME_STATFILE,"469e98c69f00928a8366ba415d91902d",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"f259e3e07a1cde8d0404a767d815e12c",225),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"95c44bfc380770a6b6dd0dfcc69e80a0",309),
                        new ADGameFileDescription("xtbllist", (ushort)GameFileTypes.GAME_XTBLFILE,"6c7b3db345d46349a5226f695c03e20f",88),
                    }, Language.ES_ESP, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_WW, GameIds.GID_WAXWORKS, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - English Acorn Floppy
            new AgosGameDescription(new ADGameDescription("simon1","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamebase.dat", (ushort)GameFileTypes.GAME_BASEFILE,"c392e494dcabed797b98cbcfc687b33a",36980),
                        new ADGameFileDescription("icondata.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",14361),
                        new ADGameFileDescription("stripped.dat", (ushort)GameFileTypes.GAME_STRFILE,"c95a0a1ee973e19c2a1c5d12026c139f",252),
                        new ADGameFileDescription("tbllist.dat", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",711),
                    }, Language.EN_ANY, Platform.Acorn, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GuiOptions.NOMIDI),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - English Acorn CD Demo
            new AgosGameDescription(new ADGameDescription("simon1","CD Demo",
                    new[]
                    {
                        new ADGameFileDescription("data", (ushort)GameFileTypes.GAME_GMEFILE,"b4a7526ced425ba8ad0d548d0ec69900",1237886),
                        new ADGameFileDescription("gamebase", (ushort)GameFileTypes.GAME_BASEFILE,"425c7d1957699d35abca7e12a08c7422",30879),
                        new ADGameFileDescription("icondata", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",14361),
                        new ADGameFileDescription("stripped", (ushort)GameFileTypes.GAME_STRFILE,"d9de7542612d9f4e0819ad0df5eac56b",252),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",711),
                    }, Language.EN_ANY, Platform.Acorn, ADGameFlags.DEMO, GuiOptions.NOSUBTITLES),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - English Acorn CD
            new AgosGameDescription(new ADGameDescription("simon1","CD",
                    new[]
                    {
                        new ADGameFileDescription("data", (ushort)GameFileTypes.GAME_GMEFILE,"64958b3a38afdcb85da1eeed85169806",6943110),
                        new ADGameFileDescription("gamebase", (ushort)GameFileTypes.GAME_BASEFILE,"28261b99cd9da1242189b4f6f2841bd6",29176),
                        new ADGameFileDescription("icondata", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",14361),
                        new ADGameFileDescription("stripped", (ushort)GameFileTypes.GAME_STRFILE,"f3b27a3fbb45dcd323a48159496e45e8",252),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",711),
                    }, Language.EN_ANY, Platform.Acorn, ADGameFlags.CD, GuiOptions.NOSUBTITLES),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - English Amiga OCS Floppy
            new AgosGameDescription(new ADGameDescription("simon1","OCS Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"bb94a688e247695d912cce9d0173d73a",37991),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"565ef7a98dcc21ef526a2bb10b6f42ed",18979),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"f5fc67db3b8c5283cda51c43b98a74f8",243),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"f9d5bf2ce09f82289c791c3ca26e1e4b",696),
                    }, Language.EN_ANY, Platform.Amiga, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH| GuiOptions.NOMIDI),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_32COLOR | GameFeatures.GF_CRUNCHED | GameFeatures.GF_OLD_BUNDLE | GameFeatures.GF_PLANAR),

            // Simon the Sorcerer 1 - English Amiga OCS Demo
            new AgosGameDescription(new ADGameDescription("simon1","OCS Demo",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"4696309eed9d7335c62ebb87a0f006ad",12764),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"ebc96af15bfaf75ba8210326b9260d2f",9124),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"8edde5b9498dc9f31da1093028da467c",27),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"1247e024e1f13ca54c1e354120c7519c",105),
                    }, Language.EN_ANY, Platform.Amiga, ADGameFlags.DEMO, GuiOptions.NOSPEECH| GuiOptions.NOMIDI),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_32COLOR | GameFeatures.GF_CRUNCHED |
                                                                GameFeatures.GF_CRUNCHED_GAMEPC |GameFeatures.GF_OLD_BUNDLE | GameFeatures.GF_PLANAR),

            // Simon the Sorcerer 1 - German Amiga OCS Floppy
            new AgosGameDescription(new ADGameDescription("simon1","OCS Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"634c82b7a0b760214fd71add328c7a00",39493),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"565ef7a98dcc21ef526a2bb10b6f42ed",18979),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"f5fc67db3b8c5283cda51c43b98a74f8",243),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"f9d5bf2ce09f82289c791c3ca26e1e4b",696),
                    }, Language.DE_DEU, Platform.Amiga, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH| GuiOptions.NOMIDI),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_32COLOR | GameFeatures.GF_CRUNCHED |
                                                                GameFeatures.GF_OLD_BUNDLE | GameFeatures.GF_PLANAR),

            // Simon the Sorcerer 1 - English Amiga AGA Floppy
            new AgosGameDescription(new ADGameDescription("simon1","AGA Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"6c9ad2ff571d34a4cf0c696cf4e13500",38057),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"565ef7a98dcc21ef526a2bb10b6f42ed",18979),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"c649fcc0439766810e5097ee7e81d4c8",243),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"f9d5bf2ce09f82289c791c3ca26e1e4b",696),
                    }, Language.EN_ANY, Platform.Amiga, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GuiOptions.NOMIDI),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1,
                GameFeatures.GF_CRUNCHED|GameFeatures.GF_OLD_BUNDLE|GameFeatures.GF_PLANAR),

            // Simon the Sorcerer 1 - French Amiga AGA Floppy
            new AgosGameDescription(new ADGameDescription("simon1","AGA Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"bd9828b9d4e5d89b50fe8c47a8e6bc07",-1),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"565ef7a98dcc21ef526a2bb10b6f42ed",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"2297baec985617d0d5612a0124bac359",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"f9d5bf2ce09f82289c791c3ca26e1e4b",-1),
                    }, Language.FR_FRA, Platform.Amiga, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GuiOptions.NOMIDI),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1,
                GameFeatures.GF_CRUNCHED|GameFeatures.GF_OLD_BUNDLE|GameFeatures.GF_PLANAR),

            // Simon the Sorcerer 1 - German Amiga AGA Floppy
            new AgosGameDescription(new ADGameDescription("simon1","AGA Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"a2de9553f3b73064369948b5af38bb30",-1),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"565ef7a98dcc21ef526a2bb10b6f42ed",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"c649fcc0439766810e5097ee7e81d4c8",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"f9d5bf2ce09f82289c791c3ca26e1e4b",-1),
                    }, Language.DE_DEU, Platform.Amiga, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GuiOptions.NOMIDI),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1,
                GameFeatures.GF_CRUNCHED|GameFeatures.GF_OLD_BUNDLE|GameFeatures.GF_PLANAR),

            // Simon the Sorcerer 1 - Italian Amiga AGA Floppy
            new AgosGameDescription(new ADGameDescription("simon1","AGA Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"55dc304e7d3f8ad518af3b7f69da02b6",-1),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"565ef7a98dcc21ef526a2bb10b6f42ed",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"c649fcc0439766810e5097ee7e81d4c8",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"f9d5bf2ce09f82289c791c3ca26e1e4b",-1),
                    }, Language.IT_ITA, Platform.Amiga, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH | GuiOptions.NOMIDI),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1,
                GameFeatures.GF_CRUNCHED|GameFeatures.GF_OLD_BUNDLE|GameFeatures.GF_PLANAR),

            // Simon the Sorcerer 1 - English Amiga CD32
            new AgosGameDescription(new ADGameDescription("simon1","CD32",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"bab7f19237cf7d7619b6c73631da1854",-1),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"565ef7a98dcc21ef526a2bb10b6f42ed",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"59be788020441e21861e284236fd08c1",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"f9d5bf2ce09f82289c791c3ca26e1e4b",-1),
                    }, Language.EN_ANY, Platform.Amiga, ADGameFlags.CD, GuiOptions.NOSUBTITLES | GuiOptions.NOMIDI),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1CD32,
                GameFeatures.GF_TALKIE|GameFeatures.GF_OLD_BUNDLE|GameFeatures.GF_PLANAR),

            // Simon the Sorcerer 1 - English Amiga CD32 alternative?
            new AgosGameDescription(new ADGameDescription("simon1","CD32",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"ec5358680c117f29b128cbbb322111a4",-1),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"8ce5a46466a4f8f6d0f780b0ef00d5f5",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"59be788020441e21861e284236fd08c1",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"f9d5bf2ce09f82289c791c3ca26e1e4b",-1),
                    }, Language.EN_ANY, Platform.Amiga, ADGameFlags.CD, GuiOptions.NOSUBTITLES | GuiOptions.NOMIDI),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1CD32,
                GameFeatures.GF_TALKIE|GameFeatures.GF_OLD_BUNDLE|GameFeatures.GF_PLANAR),

            // Simon the Sorcerer 1 - English Amiga CD32 demo, from the cover disc of
            // issue 5 (October 1994) of Amiga CD32 Gamer
            new AgosGameDescription(new ADGameDescription("simon1","CD32 Demo",
                    new[]
                    {
                        new ADGameFileDescription("gameamiga", (ushort)GameFileTypes.GAME_BASEFILE,"e243f9229f9728b3476e54d2cf5f18a1",27998),
                        new ADGameFileDescription("icon.pkd", (ushort)GameFileTypes.GAME_ICONFILE,"565ef7a98dcc21ef526a2bb10b6f42ed",18979),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"94413c71c86c32ed9baaa1c74a151cb3",243),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"f9d5bf2ce09f82289c791c3ca26e1e4b",696),
                    }, Language.EN_ANY, Platform.Amiga, ADGameFlags.CD|ADGameFlags.DEMO, GuiOptions.NOSUBTITLES | GuiOptions.NOMIDI),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1CD32,
                GameFeatures.GF_TALKIE|GameFeatures.GF_OLD_BUNDLE|GameFeatures.GF_PLANAR),

            // Simon the Sorcerer 1 - English DOS Floppy Demo
            new AgosGameDescription(new ADGameDescription("simon1","Floppy Demo",
                    new[]
                    {
                        new ADGameFileDescription("gdemo", (ushort)GameFileTypes.GAME_BASEFILE,"2be4a21bc76e2fdc071867c130651439",25288),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"55af3b4d93972bc58bfee38a86b76c3f",11495),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"33a2e329b97b2a349858d6a093159eb7",27),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"1247e024e1f13ca54c1e354120c7519c",105),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE|GameFeatures.GF_DEMO),

            // Simon the Sorcerer 1 - English DOS Floppy
            new AgosGameDescription(new ADGameDescription("simon1","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"c392e494dcabed797b98cbcfc687b33a",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"c95a0a1ee973e19c2a1c5d12026c139f",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - English DOS Floppy with Czech patch
            new AgosGameDescription(new ADGameDescription("simon1","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"62de24fc579b94fac7d3d23201b65b14",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"c95a0a1ee973e19c2a1c5d12026c139f",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.CZ_CZE, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - English DOS Floppy with Russian patch
            new AgosGameDescription(new ADGameDescription("simon1","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"605fb866e03ec1c41b10c6a518ddfa49",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"c95a0a1ee973e19c2a1c5d12026c139f",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.RU_RUS, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - English DOS Floppy (Infocom)
            new AgosGameDescription(new ADGameDescription("simon1","Infocom Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"9f93d27432ce44a787eef10adb640870",37070),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",14361),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"2af9affc5981eec44b90d4c556145cb8",252),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",711),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - English DOS Floppy (Infocom) with Czech patch
            new AgosGameDescription(new ADGameDescription("simon1","Infocom Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"62de24fc579b94fac7d3d23201b65b14",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"2af9affc5981eec44b90d4c556145cb8",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.CZ_CZE, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - English DOS Floppy (Infocom) with Russian patch
            new AgosGameDescription(new ADGameDescription("simon1","Infocom Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"605fb866e03ec1c41b10c6a518ddfa49",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"2af9affc5981eec44b90d4c556145cb8",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.RU_RUS, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - French DOS Floppy
            new AgosGameDescription(new ADGameDescription("simon1","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"34759d0d4285a2f4b21b8e03b8fcefb3",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"aa01e7386057abc0c3e27dbaa9c4ba5b",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.FR_FRA, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - German DOS Floppy
            new AgosGameDescription(new ADGameDescription("simon1","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"063015e6ce7d90b570dbc21fe0c667b1",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"c95a0a1ee973e19c2a1c5d12026c139f",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.DE_DEU, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - Italian DOS Floppy
            new AgosGameDescription(new ADGameDescription("simon1","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"65c9b2dea57df84ef55d1eaf384ebd30",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"2af9affc5981eec44b90d4c556145cb8",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.IT_ITA, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - Spanish DOS Floppy
            new AgosGameDescription(new ADGameDescription("simon1","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"5374fafdea2068134f33deab225feed3",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"2af9affc5981eec44b90d4c556145cb8",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.ES_ESP, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE),

            // Simon the Sorcerer 1 - English DOS CD Demo
            new AgosGameDescription(new ADGameDescription("simon1","CD Demo",
                    new[]
                    {
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"b4a7526ced425ba8ad0d548d0ec69900",1237886),
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"425c7d1957699d35abca7e12a08c7422",30879),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",14361),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"d9de7542612d9f4e0819ad0df5eac56b",252),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",711),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO, GuiOptions.NOSUBTITLES),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - English DOS CD
            new AgosGameDescription(new ADGameDescription("simon1","CD",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"28261b99cd9da1242189b4f6f2841bd6",29176),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",14361),
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"64958b3a38afdcb85da1eeed85169806",6943110),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"f3b27a3fbb45dcd323a48159496e45e8",252),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",711),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.CD, GuiOptions.NOSUBTITLES),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - English DOS CD  (Infocom)
            new AgosGameDescription(new ADGameDescription("simon1","Infocom CD",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"c0b948b6821d2140f8b977144f21027a",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"64f73e94639b63af846ac4a8a94a23d8",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"f3b27a3fbb45dcd323a48159496e45e8",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.CD, GuiOptions.NOSUBTITLES),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - English DOS CD with Russian patch
            new AgosGameDescription(new ADGameDescription("simon1","CD",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"3fac46064f69e5298f4f027f204c5aab",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"91321f0d806f8d9fef71a00e58581427",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"ef51ac74c946881ae4d7ca66cc7a0d1e",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.RU_RUS, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - French DOS CD
            new AgosGameDescription(new ADGameDescription("simon1","CD",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"3cfb9d1ff4ec725af9924140126cf69f",39310),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",14361),
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"638049fa5d41b81fb6fb11671721b871",7041803),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"ef51ac74c946881ae4d7ca66cc7a0d1e",252),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",711),
                    }, Language.FR_FRA, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - German DOS CD
            new AgosGameDescription(new ADGameDescription("simon1","CD",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"48b1f3499e2e0d731047f4d481ff7817",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"7db9912acac4f1d965a64bdcfc370ba1",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"40d68bec54042ef930f084ad9a4342a1",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.DE_DEU, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSUBTITLES),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - Hebrew DOS CD
            new AgosGameDescription(new ADGameDescription("simon1","CD",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"bc66e9c0b296e1b155a246917133f71a",34348),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",14361),
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"a34b2c8642f2e3676d7088b5c8b3e884",6976948),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"9d31bef42db1a8abe4e9f368014df1d5",252),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",711),
                    }, Language.HE_ISR, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - Italian DOS CD
            new AgosGameDescription(new ADGameDescription("simon1","CD",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"8d3ca654e158c91b860c7eae31d65312",37807),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",14361),
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"52e315e0e02feca86d15cc82e3306b6c",7035767),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"9d31bef42db1a8abe4e9f368014df1d5",252),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",711),
                    }, Language.IT_ITA, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - Italian DOS CD alternate
            new AgosGameDescription(new ADGameDescription("simon1","CD",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"8d3ca654e158c91b860c7eae31d65312",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"104efd83c8f3edf545982e07d87f66ac",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"9d31bef42db1a8abe4e9f368014df1d5",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.IT_ITA, Platform.Windows, // FIXME: DOS version which uses WAV format
                    ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - Spanish DOS CD
            new AgosGameDescription(new ADGameDescription("simon1","CD",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"439f801ba52c02c9d1844600d1ce0f5e",37847),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",14361),
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"eff2774a73890b9eac533db90cd1afa1",7030485),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"9d31bef42db1a8abe4e9f368014df1d5",252),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",711),
                    }, Language.ES_ESP, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - English Windows CD
            new AgosGameDescription(new ADGameDescription("simon1","CD",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"c7c12fea7f6d0bfd22af5cdbc8166862",36152),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",14361),
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"b1b18d0731b64c0738c5cc4a2ee792fc",7030377),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"a27e87a9ba21212d769804b3df47bfb2",252),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",711),
                    }, Language.EN_ANY, Platform.Windows, ADGameFlags.CD, GuiOptions.NOSUBTITLES),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 1 - German Windows CD
            new AgosGameDescription(new ADGameDescription("simon1","CD",
                    new[]
                    {
                        new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"48b1f3499e2e0d731047f4d481ff7817",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                        new ADGameFileDescription("simon.gme", (ushort)GameFileTypes.GAME_GMEFILE,"acd9cc438525b142d93b15c77a6f551b",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"40d68bec54042ef930f084ad9a4342a1",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                    }, Language.DE_DEU, Platform.Windows, ADGameFlags.CD, GuiOptions.NOSUBTITLES),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - English DOS Floppy
            new AgosGameDescription(new ADGameDescription("simon2","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("game32", (ushort)GameFileTypes.GAME_BASEFILE,"604d04315935e77624bd356ac926e068",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"aa6840420899a31874204f90bb214108",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"e229f84d46fa83f99b4a7115679f3fb6",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, 0),

            // Simon the Sorcerer 2 - English DOS Floppy with Russian patch
            new AgosGameDescription(new ADGameDescription("simon2","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("game32", (ushort)GameFileTypes.GAME_BASEFILE,"7edfc633dd50f8caa719c478443db70b",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"aa6840420899a31874204f90bb214108",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"e229f84d46fa83f99b4a7115679f3fb6",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.RU_RUS, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, 0),

            // Simon the Sorcerer 2 - German DOS Floppy
            new AgosGameDescription(new ADGameDescription("simon2","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("game32", (ushort)GameFileTypes.GAME_BASEFILE,"eb6e3e37fe52993f948d7e2d6b869828",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"5fa9d080b04c610f526bd685be1bf747",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"fd30df01cc248ecbaef302af855e0212",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.DE_DEU, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, 0),

            // Simon the Sorcerer 2 - German DOS Floppy alternate?
            new AgosGameDescription(new ADGameDescription("simon2","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("game32", (ushort)GameFileTypes.GAME_BASEFILE,"604d04315935e77624bd356ac926e068",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"ec9f0f24fd895e7ea72e3c8e448c0240",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"6de6292c9ac11bfb2e70fdb0f773ba85",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.DE_DEU, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, 0),

            // Simon the Sorcerer 2 - Italian DOS Floppy
            new AgosGameDescription(new ADGameDescription("simon2","Floppy",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"3e11d400bea0638f360a724687005cd1",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"f306a397565d7f13bec7ecf14c723de7",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"bea6843fb9f3b2144fcb146d62db0b9a",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.IT_ITA, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, 0),

            // Simon the Sorcerer 2 - English DOS CD Demo
            new AgosGameDescription(new ADGameDescription("simon2","CD Demo",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"3794c15887539b8578bacab694ccf08a",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"f8c9e6df1e55923a749e115ba74210c4",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"e229f84d46fa83f99b4a7115679f3fb6",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"a0d5a494b5d3d209d1a1d76cc8d76601",-1),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.DEMO, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - German DOS CD Demo
            new AgosGameDescription(new ADGameDescription("simon2","CD Demo",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"7596ef2644fde56ee5fad0dcd483a01e",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"3f308f201f6b1ddf7c2704c1fc43a3e1",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"4c68cf64e581a9bd638a56c900b08bfe",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"a0d5a494b5d3d209d1a1d76cc8d76601",-1),
                    }, Language.DE_DEU, Platform.DOS, ADGameFlags.DEMO, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - German DOS CD Non-Interactive Demo
            new AgosGameDescription(new ADGameDescription("simon2","CD Non-Interactive Demo",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"c45facd0605860684d464b6a62323567",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"30ea02d374327cab6c78444f3c198c1c",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"4c68cf64e581a9bd638a56c900b08bfe",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"a0d5a494b5d3d209d1a1d76cc8d76601",-1),
                    }, Language.DE_DEU, Platform.DOS, ADGameFlags.DEMO, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - English DOS CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"8c301fb9c4fcf119d2730ccd2a565eb3",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"9c535d403966750ae98bdaf698375a38",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"e229f84d46fa83f99b4a7115679f3fb6",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - English DOS CD alternate?
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"608e277904d87dd28725fa08eacc2c0d",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"8d6dcc65577e285dbca03ff6d7d9323c",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"e229f84d46fa83f99b4a7115679f3fb6",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"a0d5a494b5d3d209d1a1d76cc8d76601",-1),
                    }, Language.EN_ANY, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - French DOS CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"43b3a04d2f0a0cbd1b024c814856561a",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"8af0e02c0c3344db64dffc12196eb59d",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"5ea27977b4d7dcfd50eb5074e162ebbf",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.FR_FRA, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - German DOS CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"0d05c3f4c06c9a4ceb3d2f5bc0b18e11",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"6c5fdfdd0eab9038767c2d22858406b2",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"6de6292c9ac11bfb2e70fdb0f773ba85",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.DE_DEU, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - German DOS CD alternate?
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"a76ea940076b5d9316796dea225a9b69",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"ec9f0f24fd895e7ea72e3c8e448c0240",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"6de6292c9ac11bfb2e70fdb0f773ba85",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.DE_DEU, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - Hebrew DOS CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"952a2b1be23c3c609ba8d988a9a1627d",53366),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",18089),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"a2b249a82ea182af09789eb95fb6c5be",19650639),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"de9dbc24158660e153483fa0cf6c3172",171),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",513),
                    }, Language.HE_ISR, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - Italian DOS CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"3e11d400bea0638f360a724687005cd1",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"344aca58e5ad5e25c517d5eb1d85c435",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"bea6843fb9f3b2144fcb146d62db0b9a",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.IT_ITA, Platform.Windows,// FIXME: DOS version which uses WAV format
                    ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - Spanish DOS CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"268dc322aa73bcf27bb016b8e8ceb889",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"4f43bd06b6cc78dbd25a7475ca964eb1",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"d13753796bd81bf313a2449f34d8b112",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.ES_ESP, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - Russian DOS CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"e26d162e573587f4601b88701292212c",58851),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",18089),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"9c535d403966750ae98bdaf698375a38",19687892),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"e229f84d46fa83f99b4a7115679f3fb6",171),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",513),
                    }, Language.RU_RUS, Platform.DOS, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - Czech Windows CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"eb8217f9ec4628d12ca606033146c48c",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"2d1074788501c55dcd9e59269ea0aaed",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"e229f84d46fa83f99b4a7115679f3fb6",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.CZ_CZE, Platform.Windows, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - English Windows CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"608e277904d87dd28725fa08eacc2c0d",58652),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",18089),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"e749c4c103d7e7d51b34620ed76c5a04",20046789),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"e229f84d46fa83f99b4a7115679f3fb6",171),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",513),
                    }, Language.EN_ANY, Platform.Windows, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - French Windows CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"6e217d75f4089b92270fad879e091e29",58652),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",18089),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"1d2f867a7eba818c85c1e4849821e812",20046789),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"5ea27977b4d7dcfd50eb5074e162ebbf",171),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",513),
                    }, Language.FR_FRA, Platform.Windows, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - German Windows CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"a76ea940076b5d9316796dea225a9b69",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"9609a933c541fed2e00c6c3479d7c181",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"6de6292c9ac11bfb2e70fdb0f773ba85",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.DE_DEU, Platform.Windows, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE),

            // Simon the Sorcerer 2 - Polish Windows CD
            new AgosGameDescription(new ADGameDescription("simon2","CD",
                    new[]
                    {
                        new ADGameFileDescription("gsptr30", (ushort)GameFileTypes.GAME_BASEFILE,"657fd873f5d0637097ee02315b447e6f",-1),
                        new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"72096a62d36e6034ea9fecc13b2dbdab",-1),
                        new ADGameFileDescription("simon2.gme", (ushort)GameFileTypes.GAME_GMEFILE,"7b9afcf82a94722707e0d025c0192be8",-1),
                        new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"e229f84d46fa83f99b4a7115679f3fb6",-1),
                        new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"2082f8d02075e590300478853a91ffd9",-1),
                    }, Language.PL_POL, Platform.Windows, ADGameFlags.CD, GuiOptions.NONE),
                SIMONGameType.GType_SIMON2, GameIds.GID_SIMON2, GameFeatures.GF_TALKIE)
        };

        public AgosMetaEngine()
            : base(gameDescriptions)
        {
        }

        public override string OriginalCopyright => "AGOS (C) Adventure Soft";

        public override IEngine Create(GameSettings settings, ISystem system)
        {
            var gd = ((AgosGameDescriptor)settings.Game).ADGameDescription;
            switch (gd.gameType)
            {
                //case SIMONGameType.GType_PN:
                //    *engine = new AGOSEngine_PN(syst, gd);
                //    break;
                case SIMONGameType.GType_ELVIRA1:
                    return new AgosEngineElvira1(system, settings, gd);
                case SIMONGameType.GType_ELVIRA2:
                    return new AgosEngineElvira2(system, settings, gd);
                case SIMONGameType.GType_WW:
                    return new AgosEngineWaxworks(system, settings, gd);
                case SIMONGameType.GType_SIMON1:
                    return new AgosEngineSimon1(system, settings, gd);
                case SIMONGameType.GType_SIMON2:
                    return new AgosEngineSimon2(system, settings, gd);
# if ENABLE_AGOS2
//                case SIMONGameType.GType_FF:
//                    if (gd->features & GF_DEMO)
//                        *engine = new AGOS::AGOSEngine_FeebleDemo(syst, gd);
//                    else
//                        *engine = new AGOS::AGOSEngine_Feeble(syst, gd);
//                    break;
//                case SIMONGameType.GType_PP:
//                    if (gd->gameId == GID_DIMP)
//                        *engine = new AGOS::AGOSEngine_DIMP(syst, gd);
//                    else
//                        *engine = new AGOS::AGOSEngine_PuzzlePack(syst, gd);
//                    break;
#endif
                default:
                    Error("AGOS engine: unknown gameType");
                    return null;
            }
        }

        protected override IGameDescriptor CreateGameDescriptor(string path, ADGameDescription desc)
        {
            return new AgosGameDescriptor(path, (AgosGameDescription)desc);
        }
    }
}


