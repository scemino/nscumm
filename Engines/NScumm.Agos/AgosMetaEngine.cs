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
            // Simon the Sorcerer 1 - English DOS Floppy
            new AGOSGameDescription(new ADGameDescription("simon1","Floppy",
                new[]
                {
                    new ADGameFileDescription("gamepc", (ushort)GameFileTypes.GAME_BASEFILE,"c392e494dcabed797b98cbcfc687b33a",-1),
                    new ADGameFileDescription("icon.dat", (ushort)GameFileTypes.GAME_ICONFILE,"22107c24dfb31b66ac503c28a6e20b19",-1),
                    new ADGameFileDescription("stripped.txt", (ushort)GameFileTypes.GAME_STRFILE,"c95a0a1ee973e19c2a1c5d12026c139f",-1),
                    new ADGameFileDescription("tbllist", (ushort)GameFileTypes.GAME_TBLFILE,"d198a80de2c59e4a0cd24b98814849e8",-1),
                }, Language.EN_ANY, Platform.DOS, ADGameFlags.NO_FLAGS, GuiOptions.NOSPEECH),
                SIMONGameType.GType_SIMON1, GameIds.GID_SIMON1DOS, GameFeatures.GF_OLD_BUNDLE)
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
                //case AGOS::GType_PN:
                //    *engine = new AGOS::AGOSEngine_PN(syst, gd);
                //    break;
                //case AGOS::GType_ELVIRA1:
                //    *engine = new AGOS::AGOSEngine_Elvira1(syst, gd);
                //    break;
                //case AGOS::GType_ELVIRA2:
                //    *engine = new AGOS::AGOSEngine_Elvira2(syst, gd);
                //    break;
                //case AGOS::GType_WW:
                //    *engine = new AGOS::AGOSEngine_Waxworks(syst, gd);
                //    break;
                case SIMONGameType.GType_SIMON1:
                    return new AgosEngineSimon1(system, settings, gd);
                //case AGOS::GType_SIMON2:
                //    *engine = new AGOS::AGOSEngine_Simon2(syst, gd);
                //    break;
# if ENABLE_AGOS2
//                case AGOS::GType_FF:
//                    if (gd->features & GF_DEMO)
//                        *engine = new AGOS::AGOSEngine_FeebleDemo(syst, gd);
//                    else
//                        *engine = new AGOS::AGOSEngine_Feeble(syst, gd);
//                    break;
//                case AGOS::GType_PP:
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
            return new AgosGameDescriptor(path, (AGOSGameDescription)desc);
        }
    }
}


