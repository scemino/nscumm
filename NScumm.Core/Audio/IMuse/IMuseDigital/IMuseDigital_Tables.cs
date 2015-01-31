//
//  IMuseDigital_Tables.cs
//
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

namespace NScumm.Core.Audio.IMuse
{
    partial class IMuseDigital
    {
        struct ImuseFtStateTable
        {
            public string audioName;
            public byte transitionType;
            public byte volume;
            public string name;
        }

        struct ImuseFtSeqTable
        {
            public string audioName;
            public byte transitionType;
            public byte volume;
        }

        static readonly ImuseFtStateTable[] _ftStateMusicTable =
            {
                new ImuseFtStateTable{ audioName = "",         transitionType = 0,  volume = 0,    name = "STATE_NULL"          },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 127,  name = "stateKstandOutside"  },
                new ImuseFtStateTable{ audioName = "kinside",  transitionType = 2,  volume = 127,  name = "stateKstandInside"   },
                new ImuseFtStateTable{ audioName = "moshop",   transitionType = 3,  volume = 64,   name = "stateMoesInside"     },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateMoesOutside"    },
                new ImuseFtStateTable{ audioName = "mellover", transitionType = 2,  volume = 127,  name = "stateMellonAbove"    },
                new ImuseFtStateTable{ audioName = "radloop",  transitionType = 3,  volume = 28,   name = "stateTrailerOutside" },
                new ImuseFtStateTable{ audioName = "radloop",  transitionType = 3,  volume = 58,   name = "stateTrailerInside"  },
                new ImuseFtStateTable{ audioName = "radloop",  transitionType = 3,  volume = 127,  name = "stateTodShop"        },
                new ImuseFtStateTable{ audioName = "junkgate", transitionType = 2,  volume = 127,  name = "stateJunkGate"       },
                new ImuseFtStateTable{ audioName = "junkover", transitionType = 3,  volume = 127,  name = "stateJunkAbove"      },
                new ImuseFtStateTable{ audioName = "gastower", transitionType = 2,  volume = 127,  name = "stateGasTower"       },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateTowerAlarm"     },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateCopsOnGround"   },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateCopsAround"     },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateMoesRuins"      },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateKstandNight"    },
                new ImuseFtStateTable{ audioName = "trukblu2", transitionType = 2,  volume = 127,  name = "stateTruckerTalk"    },
                new ImuseFtStateTable{ audioName = "stretch",  transitionType = 2,  volume = 127,  name = "stateMumblyPeg"      },
                new ImuseFtStateTable{ audioName = "kstand",   transitionType = 2,  volume = 100,  name = "stateRanchOutside"   },
                new ImuseFtStateTable{ audioName = "kinside",  transitionType = 2,  volume = 127,  name = "stateRanchInside"    },
                new ImuseFtStateTable{ audioName = "desert",   transitionType = 2,  volume = 127,  name = "stateWreckedTruck"   },
                new ImuseFtStateTable{ audioName = "opening",  transitionType = 2,  volume = 100,  name = "stateGorgeVista"     },
                new ImuseFtStateTable{ audioName = "caveopen", transitionType = 2,  volume = 127,  name = "stateCaveOpen"       },
                new ImuseFtStateTable{ audioName = "cavecut1", transitionType = 2,  volume = 127,  name = "stateCaveOuter"      },
                new ImuseFtStateTable{ audioName = "cavecut1", transitionType = 1,  volume = 127,  name = "stateCaveMiddle"     },
                new ImuseFtStateTable{ audioName = "cave",     transitionType = 2,  volume = 127,  name = "stateCaveInner"      },
                new ImuseFtStateTable{ audioName = "corville", transitionType = 2,  volume = 127,  name = "stateCorvilleFront"  },
                new ImuseFtStateTable{ audioName = "mines",    transitionType = 2,  volume = 127,  name = "stateMineField"      },
                new ImuseFtStateTable{ audioName = "bunyman3", transitionType = 2,  volume = 127,  name = "stateBunnyStore"     },
                new ImuseFtStateTable{ audioName = "stretch",  transitionType = 2,  volume = 127,  name = "stateStretchBen"     },
                new ImuseFtStateTable{ audioName = "saveme",   transitionType = 2,  volume = 127,  name = "stateBenPleas"       },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateBenConvinces"   },
                new ImuseFtStateTable{ audioName = "derby",    transitionType = 3,  volume = 127,  name = "stateDemoDerby"      },
                new ImuseFtStateTable{ audioName = "fire",     transitionType = 3,  volume = 127,  name = "stateLightMyFire"    },
                new ImuseFtStateTable{ audioName = "derby",    transitionType = 3,  volume = 127,  name = "stateDerbyChase"     },
                new ImuseFtStateTable{ audioName = "carparts", transitionType = 2,  volume = 127,  name = "stateVultureCarParts" },
                new ImuseFtStateTable{ audioName = "cavecut1", transitionType = 2,  volume = 127,  name = "stateVulturesInside" },
                new ImuseFtStateTable{ audioName = "mines",    transitionType = 2,  volume = 127,  name = "stateFactoryRear"    },
                new ImuseFtStateTable{ audioName = "croffice", transitionType = 2,  volume = 127,  name = "stateCorleyOffice"   },
                new ImuseFtStateTable{ audioName = "melcut",   transitionType = 2,  volume = 127,  name = "stateCorleyHall"     },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateProjRoom"       },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateMMRoom"         },
                new ImuseFtStateTable{ audioName = "bumper",   transitionType = 2,  volume = 127,  name = "stateBenOnBumper"    },
                new ImuseFtStateTable{ audioName = "benump",   transitionType = 2,  volume = 127,  name = "stateBenOnBack"      },
                new ImuseFtStateTable{ audioName = "plane",    transitionType = 2,  volume = 127,  name = "stateInCargoPlane"   },
                new ImuseFtStateTable{ audioName = "saveme",   transitionType = 2,  volume = 127,  name = "statePlaneControls"  },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateCliffHanger1"   },
                new ImuseFtStateTable{ audioName = "",         transitionType = 4,  volume = 0,    name = "stateCliffHanger2"   },
            };

        static readonly string[] _ftSeqNames =
        {

            "SEQ_NULL",
            "seqLogo",
            "seqOpenFlick",
            "seqBartender",
            "seqBenWakes",
            "seqPhotoScram",
            "seqClimbChain",
            "seqDogChase",
            "seqDogSquish",
            "seqDogHoist",
            "seqCopsArrive",
            "seqCopsLand",
            "seqCopsLeave",
            "seqCopterFlyby",
            "seqCopterCrash",
            "seqMoGetsParts",
            "seqMoFixesBike",
            "seqFirstGoodbye",
            "seqCopRoadblock",
            "seqDivertCops",
            "seqMurder",
            "seqCorleyDies",
            "seqTooLateAtMoes",
            "seqPicture",
            "seqNewsReel",
            "seqCopsInspect",
            "seqHijack",
            "seqNestolusAtRanch",
            "seqRipLimo",
            "seqGorgeTurn",
            "seqCavefishTalk",
            "seqArriveCorville",
            "seqSingleBunny",
            "seqBunnyArmy",
            "seqArriveAtMines",
            "seqArriveAtVultures",
            "seqMakePlan",
            "seqShowPlan",
            "seqDerbyStart",
            "seqLightBales",
            "seqNestolusBBQ",
            "seqCallSecurity",
            "seqFilmFail",
            "seqFilmBurn",
            "seqRipSpeech",
            "seqExposeRip",
            "seqRipEscape",
            "seqRareMoment",
            "seqFanBunnies",
            "seqRipDead",
            "seqFuneral",
            "seqCredits"         
        };

        static readonly ImuseFtSeqTable[] _ftSeqMusicTable =
            {
                new ImuseFtSeqTable { audioName = "",         transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "opening",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "barbeat",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "barwarn",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0, },

                new ImuseFtSeqTable { audioName = "benwakes", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "barwarn",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "swatben",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "dogattak", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 4,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 4,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "cops2",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "cops2",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "cops2",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "bunymrch", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 4,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "melcut",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "tada",     transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 4,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "trucker",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "cops2",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "barwarn",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "murder",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "murder2",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "corldie",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "barwarn",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "picture",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "ripintro", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "trucker",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "hosed",    transitionType = 2,  volume = 127 },

                new ImuseFtSeqTable { audioName = "ripdead",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "nesranch", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "scolding", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "desert",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "cavecut1", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "caveamb",  transitionType = 2,  volume = 80 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "castle",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "bunymrch", transitionType = 2,  volume = 105 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "valkyrs",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "melcut",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "veltures", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "sorry",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "makeplan", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "castle",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "derby",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "fire",     transitionType = 3,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "saveme",   transitionType = 3,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "scolding", transitionType = 2,  volume = 127 },

                new ImuseFtSeqTable { audioName = "cops2",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "sorry",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "sorry",    transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "caveamb",  transitionType = 2,  volume = 85 },
                new ImuseFtSeqTable { audioName = "tada",     transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "expose",   transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 4,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "mocoup",   transitionType = 2,  volume = 127 },

                new ImuseFtSeqTable { audioName = "ripscram", transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "",         transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "valkyrs",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "ripdead",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "funeral",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "moshop",   transitionType = 3,  volume = 64 },
                new ImuseFtSeqTable { audioName = "",         transitionType = 0,  volume = 0  },

                new ImuseFtSeqTable { audioName = "bornbad",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "hammvox",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "legavox",  transitionType = 2,  volume = 127 },
                new ImuseFtSeqTable { audioName = "chances",  transitionType = 2,  volume = 90 },
            };

    }
}

