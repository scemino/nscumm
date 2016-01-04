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
using NScumm.Core.IO;
using System;
using System.Collections.Generic;

namespace NScumm.Sci.Engine
{
    class SciKernelMapSubEntry
    {
        public SciVersion fromVersion;
        public SciVersion toVersion;

        public ushort id;

        public string name;
        public KernelFunctionCall function;

        public string signature;
        public SciWorkaroundEntry[] workarounds;

        public static SciKernelMapSubEntry Make(SciVersionRange range, int id, KernelFunctionCall call, string signature, SciWorkaroundEntry[] workarounds)
        {
            return new SciKernelMapSubEntry { fromVersion = range.fromVersion, toVersion = range.toVersion, id = (ushort)id, function = call, signature = signature, workarounds = workarounds };
        }
    }

    class SciVersionRange
    {
        public SciVersion fromVersion;
        public SciVersion toVersion;
        public byte forPlatform;

        public static readonly SciVersionRange SIG_EVERYWHERE = new SciVersionRange { forPlatform = Kernel.SIGFOR_ALL };

        public static SciVersionRange SIG_SCI11(byte platform)
        {
            return new SciVersionRange { fromVersion = SciVersion.V1_1, toVersion = SciVersion.V1_1, forPlatform = platform };
        }

        public static SciVersionRange SIG_SCI21(byte platform)
        {
            return new SciVersionRange { fromVersion = SciVersion.V2_1, toVersion = SciVersion.V3, forPlatform = platform };
        }

        public static SciVersionRange SIG_SCI32(byte platform)
        {
            return new SciVersionRange { fromVersion = SciVersion.V2, toVersion = SciVersion.NONE, forPlatform = platform };
        }

        public static SciVersionRange SIG_SCIALL(byte platform)
        {
            return new SciVersionRange { fromVersion = SciVersion.NONE, toVersion = SciVersion.NONE, forPlatform = platform };
        }

        public static SciVersionRange SIG_SINCE_SCI11(byte platform)
        {
            return new SciVersionRange { fromVersion = SciVersion.V1_1, toVersion = SciVersion.NONE, forPlatform = platform };
        }

        public static SciVersionRange SIG_SOUNDSCI0(byte platform)
        {
            return new SciVersionRange { fromVersion = SciVersion.V0_EARLY, toVersion = SciVersion.V0_LATE, forPlatform = platform };
        }

        public static SciVersionRange SIG_SOUNDSCI1EARLY(byte platform)
        {
            return new SciVersionRange { fromVersion = SciVersion.V1_EARLY, toVersion = SciVersion.V1_EARLY, forPlatform = platform };
        }

        public static SciVersionRange SIG_SOUNDSCI1LATE(byte platform)
        {
            return new SciVersionRange { fromVersion = SciVersion.V1_LATE, toVersion = SciVersion.V1_LATE, forPlatform = platform };
        }

        public static SciVersionRange SIG_SOUNDSCI21(byte platform)
        {
            return new SciVersionRange { fromVersion = SciVersion.V2_1, toVersion = SciVersion.V3, forPlatform = platform };
        }
    }

    class SciKernelMapEntry
    {
        public string name;
        public KernelFunctionCall function;

        public SciVersion fromVersion;
        public SciVersion toVersion;
        public byte forPlatform;

        public string signature;
        public SciKernelMapSubEntry[] subFunctions;
        public SciWorkaroundEntry[] workarounds;

        public static SciKernelMapEntry Make(string name, KernelFunctionCall function, SciVersionRange range, string signature, SciKernelMapSubEntry[] subSignatures = null, SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapEntry { name = name, function = function, fromVersion = range.fromVersion, toVersion = range.toVersion, forPlatform = range.forPlatform, signature = signature, subFunctions = subSignatures, workarounds = workarounds };
        }

        public static SciKernelMapEntry Make(KernelFunctionCall function, SciVersionRange range, string signature, SciKernelMapSubEntry[] subSignatures = null, SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapEntry { name = function.Method.Name.Remove(0, 1), function = function, fromVersion = range.fromVersion, toVersion = range.toVersion, forPlatform = range.forPlatform, signature = signature, subFunctions = subSignatures, workarounds = workarounds };
        }
    }

    class KernelSubFunction
    {
        public KernelFunctionCall function;
        public string name;
        public ushort[] signature;
        public SciWorkaroundEntry[] workarounds;
        public bool debugLogging;
        public bool debugBreakpoint;
    }

    delegate Register KernelFunctionCall(EngineState s, int argc, StackPtr? argv);

    class KernelFunction
    {
        public KernelFunctionCall function;
        public string name;
        public ushort[] signature;
        public SciWorkaroundEntry[] workarounds;
        public KernelSubFunction[] subFunctions;
        public ushort subFunctionCount;
        public bool debugLogging;
        public bool debugBreakpoint;
    }

    partial class Kernel
    {
        private const int VOCAB_RESOURCE_SELECTORS = 997;

        private const int VOCAB_RESOURCE_SCI0_MAIN_VOCAB = 0;
        private const int VOCAB_RESOURCE_SCI0_PARSE_TREE_BRANCHES = 900;
        private const int VOCAB_RESOURCE_SCI0_SUFFIX_VOCAB = 901;

        private const int VOCAB_RESOURCE_SCI1_MAIN_VOCAB = 900;
        private const int VOCAB_RESOURCE_SCI1_PARSE_TREE_BRANCHES = 901;
        private const int VOCAB_RESOURCE_SCI1_SUFFIX_VOCAB = 902;

        private const int VOCAB_RESOURCE_ALT_INPUTS = 913;

        public const int SIGFOR_ALL = 0x3f;
        public const int SIGFOR_DOS = 1 << 0;
        public const int SIGFOR_PC98 = 1 << 1;
        public const int SIGFOR_WIN = 1 << 2;
        public const int SIGFOR_MAC = 1 << 3;
        public const int SIGFOR_AMIGA = 1 << 4;
        public const int SIGFOR_ATARI = 1 << 5;
        public const int SIGFOR_PC = SIGFOR_DOS | SIGFOR_WIN;

        // internal kernel signature data

        public const int SIG_TYPE_NULL = 0x01; // may be 0:0       [0]
        public const int SIG_TYPE_INTEGER = 0x02; // may be 0:*       [i], automatically also allows null
        public const int SIG_TYPE_UNINITIALIZED = 0x04; // may be FFFF:*    -> not allowable, only used for comparison
        public const int SIG_TYPE_OBJECT = 0x08; // may be object    [o]
        public const int SIG_TYPE_REFERENCE = 0x10; // may be reference [r]
        public const int SIG_TYPE_LIST = 0x20; // may be list      [l]
        public const int SIG_TYPE_NODE = 0x40; // may be node      [n]
        public const int SIG_TYPE_ERROR = 0x80; // happens, when there is a identification error - only used for comparison
        public const int SIG_IS_INVALID = 0x100; // ptr is invalid   [!] -> invalid offset
        public const int SIG_IS_OPTIONAL = 0x200; // is optional
        public const int SIG_NEEDS_MORE = 0x400; // needs at least one additional parameter following
        public const int SIG_MORE_MAY_FOLLOW = 0x800;  // may have more parameters of the same type following

        // this does not include SIG_TYPE_UNINITIALIZED, because we can not allow uninitialized values anywhere
        public const int SIG_MAYBE_ANY = (SIG_TYPE_NULL | SIG_TYPE_INTEGER | SIG_TYPE_OBJECT | SIG_TYPE_REFERENCE | SIG_TYPE_LIST | SIG_TYPE_NODE);


        private SegManager _segMan;
        private ResourceManager _resMan;
        private string _invalid;

        // Kernel-related lists
        private List<string> _selectorNames = new List<string>();
        private List<string> _kernelNames = new List<string>();
        /// <summary>
        /// Shortcut list for important selectors.
        /// </summary>
        public SelectorCache _selectorCache = new SelectorCache();

        private static readonly string[] sci0Selectors = {
                       "y",          "x",         "view",      "loop",        "cel", //  0 -  4
	           "underBits",      "nsTop",       "nsLeft",  "nsBottom",    "nsRight", //  5 -  9
	               "lsTop",     "lsLeft",     "lsBottom",   "lsRight",     "signal", // 10 - 14
	         "illegalBits",      "brTop",       "brLeft",  "brBottom",    "brRight", // 15 - 19
	                "name",        "key",         "time",      "text",   "elements", // 20 - 25
	               "color",       "back",         "mode",     "style",      "state", // 25 - 29
	                "font",       "type",       "window",    "cursor",        "max", // 30 - 34
	                "mark",        "who",      "message",      "edit",       "play", // 35 - 39
	              "number",     "handle",       "client",        "dx",         "dy", // 40 - 44
	           "b-moveCnt",       "b-i1",         "b-i2",      "b-di",    "b-xAxis", // 45 - 49
	              "b-incr",      "xStep",        "yStep", "moveSpeed",  "canBeHere", // 50 - 54
	             "heading",      "mover",         "doit", "isBlocked",     "looper", // 55 - 59
	            "priority",  "modifiers",       "replay",    "setPri",         "at", // 60 - 64
	                "next",       "done",        "width",  "wordFail", "syntaxFail", // 65 - 69
	        "semanticFail", "pragmaFail",         "said",   "claimed",      "value", // 70 - 74
	                "save",    "restore",        "title",    "button",       "icon", // 75 - 79
	                "draw",     "delete",            "z"                             // 80 - 82
        };

        private static readonly string[] sci1Selectors = {
              "parseLang",  "printLang", "subtitleLang",       "size",    "points", // 83 - 87
	            "palette",    "dataInc",       "handle",        "min",       "sec", // 88 - 92
	              "frame",        "vol",          "pri",    "perform",  "moveDone"  // 93 - 97
        };

        private static readonly string[] sci11Selectors = {
              "topString",      "flags",    "quitGame",     "restart",      "hide", // 98 - 102
	        "scaleSignal",     "scaleX",      "scaleY",    "maxScale","vanishingX", // 103 - 107
	         "vanishingY"                                                           // 108
        };

        /** Default kernel name table. */
        static readonly string[] s_defaultKernelNames = {
	        /*0x00*/ "Load",
	        /*0x01*/ "UnLoad",
	        /*0x02*/ "ScriptID",
	        /*0x03*/ "DisposeScript",
	        /*0x04*/ "Clone",
	        /*0x05*/ "DisposeClone",
	        /*0x06*/ "IsObject",
	        /*0x07*/ "RespondsTo",
	        /*0x08*/ "DrawPic",
	        /*0x09*/ "Show",
	        /*0x0a*/ "PicNotValid",
	        /*0x0b*/ "Animate",
	        /*0x0c*/ "SetNowSeen",
	        /*0x0d*/ "NumLoops",
	        /*0x0e*/ "NumCels",
	        /*0x0f*/ "CelWide",
	        /*0x10*/ "CelHigh",
	        /*0x11*/ "DrawCel",
	        /*0x12*/ "AddToPic",
	        /*0x13*/ "NewWindow",
	        /*0x14*/ "GetPort",
	        /*0x15*/ "SetPort",
	        /*0x16*/ "DisposeWindow",
	        /*0x17*/ "DrawControl",
	        /*0x18*/ "HiliteControl",
	        /*0x19*/ "EditControl",
	        /*0x1a*/ "TextSize",
	        /*0x1b*/ "Display",
	        /*0x1c*/ "GetEvent",
	        /*0x1d*/ "GlobalToLocal",
	        /*0x1e*/ "LocalToGlobal",
	        /*0x1f*/ "MapKeyToDir",
	        /*0x20*/ "DrawMenuBar",
	        /*0x21*/ "MenuSelect",
	        /*0x22*/ "AddMenu",
	        /*0x23*/ "DrawStatus",
	        /*0x24*/ "Parse",
	        /*0x25*/ "Said",
	        /*0x26*/ "SetSynonyms",	// Portrait (KQ6 hires)
	        /*0x27*/ "HaveMouse",
	        /*0x28*/ "SetCursor",
	        // FOpen (SCI0)
	        // FPuts (SCI0)
	        // FGets (SCI0)
	        // FClose (SCI0)
	        /*0x29*/ "SaveGame",
	        /*0x2a*/ "RestoreGame",
	        /*0x2b*/ "RestartGame",
	        /*0x2c*/ "GameIsRestarting",
	        /*0x2d*/ "DoSound",
	        /*0x2e*/ "NewList",
	        /*0x2f*/ "DisposeList",
	        /*0x30*/ "NewNode",
	        /*0x31*/ "FirstNode",
	        /*0x32*/ "LastNode",
	        /*0x33*/ "EmptyList",
	        /*0x34*/ "NextNode",
	        /*0x35*/ "PrevNode",
	        /*0x36*/ "NodeValue",
	        /*0x37*/ "AddAfter",
	        /*0x38*/ "AddToFront",
	        /*0x39*/ "AddToEnd",
	        /*0x3a*/ "FindKey",
	        /*0x3b*/ "DeleteKey",
	        /*0x3c*/ "Random",
	        /*0x3d*/ "Abs",
	        /*0x3e*/ "Sqrt",
	        /*0x3f*/ "GetAngle",
	        /*0x40*/ "GetDistance",
	        /*0x41*/ "Wait",
	        /*0x42*/ "GetTime",
	        /*0x43*/ "StrEnd",
	        /*0x44*/ "StrCat",
	        /*0x45*/ "StrCmp",
	        /*0x46*/ "StrLen",
	        /*0x47*/ "StrCpy",
	        /*0x48*/ "Format",
	        /*0x49*/ "GetFarText",
	        /*0x4a*/ "ReadNumber",
	        /*0x4b*/ "BaseSetter",
	        /*0x4c*/ "DirLoop",
	        /*0x4d*/ "CanBeHere",       // CantBeHere in newer SCI versions
	        /*0x4e*/ "OnControl",
	        /*0x4f*/ "InitBresen",
	        /*0x50*/ "DoBresen",
	        /*0x51*/ "Platform",        // DoAvoider (SCI0)
	        /*0x52*/ "SetJump",
	        /*0x53*/ "SetDebug",        // for debugging
	        /*0x54*/ "InspectObj",      // for debugging
	        /*0x55*/ "ShowSends",       // for debugging
	        /*0x56*/ "ShowObjs",        // for debugging
	        /*0x57*/ "ShowFree",        // for debugging
	        /*0x58*/ "MemoryInfo",
	        /*0x59*/ "StackUsage",      // for debugging
	        /*0x5a*/ "Profiler",        // for debugging
	        /*0x5b*/ "GetMenu",
	        /*0x5c*/ "SetMenu",
	        /*0x5d*/ "GetSaveFiles",
	        /*0x5e*/ "GetCWD",
	        /*0x5f*/ "CheckFreeSpace",
	        /*0x60*/ "ValidPath",
	        /*0x61*/ "CoordPri",
	        /*0x62*/ "StrAt",
	        /*0x63*/ "DeviceInfo",
	        /*0x64*/ "GetSaveDir",
	        /*0x65*/ "CheckSaveGame",
	        /*0x66*/ "ShakeScreen",
	        /*0x67*/ "FlushResources",
	        /*0x68*/ "SinMult",
	        /*0x69*/ "CosMult",
	        /*0x6a*/ "SinDiv",
	        /*0x6b*/ "CosDiv",
	        /*0x6c*/ "Graph",
	        /*0x6d*/ "Joystick",
	        // End of kernel function table for SCI0
	        /*0x6e*/ "ShiftScreen",     // never called?
	        /*0x6f*/ "Palette",
	        /*0x70*/ "MemorySegment",
	        /*0x71*/ "Intersections",	// MoveCursor (SCI1 late), PalVary (SCI1.1)
	        /*0x72*/ "Memory",
	        /*0x73*/ "ListOps",         // never called?
	        /*0x74*/ "FileIO",
	        /*0x75*/ "DoAudio",
	        /*0x76*/ "DoSync",
	        /*0x77*/ "AvoidPath",
	        /*0x78*/ "Sort",            // StrSplit (SCI01)
	        /*0x79*/ "ATan",            // never called?
	        /*0x7a*/ "Lock",
	        /*0x7b*/ "StrSplit",
	        /*0x7c*/ "GetMessage",      // Message (SCI1.1)
	        /*0x7d*/ "IsItSkip",
	        /*0x7e*/ "MergePoly",
	        /*0x7f*/ "ResCheck",
	        /*0x80*/ "AssertPalette",
	        /*0x81*/ "TextColors",
	        /*0x82*/ "TextFonts",
	        /*0x83*/ "Record",          // for debugging
	        /*0x84*/ "PlayBack",        // for debugging
	        /*0x85*/ "ShowMovie",
	        /*0x86*/ "SetVideoMode",
	        /*0x87*/ "SetQuitStr",
	        /*0x88*/ "DbugStr"          // for debugging
        };

        static readonly SciKernelMapSubEntry[] kDoSound_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),       0, kDoSoundInit,               "o",                    null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),       1, kDoSoundPlay,               "o",                    null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),       2, kDoSoundRestore,            "(o)",                  null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),       3, kDoSoundDispose,            "o",                    null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),       4, kDoSoundMute,               "(i)",                  null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),       5, kDoSoundStop,               "o",                    null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),       6, kDoSoundPause,              "i",                    null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),       7, kDoSoundResumeAfterRestore, "",                     null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),       8, kDoSoundMasterVolume,       "(i)",                  null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),       9, kDoSoundUpdate,             "o",                    null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),      10, kDoSoundFade,               "[o0]",                 Workarounds.kDoSoundFade_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),      11, kDoSoundGetPolyphony,       "",                     null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0),      12, kDoSoundStopAll,            "",                     null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0),  0, kDoSoundMasterVolume,       null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0),  1, kDoSoundMute,               null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0),  2, kDoSoundRestore,            null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0),  3, kDoSoundGetPolyphony,       null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0),  4, kDoSoundUpdate,             null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0),  5, kDoSoundInit,               null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0),  6, kDoSoundDispose,            null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0),  7, kDoSoundPlay,               "oi",                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0),  8, kDoSoundStop,               null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0),  9, kDoSoundPause,              "[o0]i",                null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 10, kDoSoundFade,               "oiiii",                Workarounds.kDoSoundFade_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 11, kDoSoundUpdateCues,         "o",                    null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 12, kDoSoundSendMidi,           "oiii",                 null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 13, kDoSoundGlobalReverb,       "(i)",                  null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 14, kDoSoundSetHold,            "oi",                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 15, kDoSoundDummy,              "",                     null),
	        //  ^^ Longbow demo
	        SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),   0, kDoSoundMasterVolume,       null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),   1, kDoSoundMute,               null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),   2, kDoSoundRestore,            "",                     null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),   3, kDoSoundGetPolyphony,       null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),   4, kDoSoundGetAudioCapability, "",                     null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),   5, kDoSoundSuspend,            "i",                    null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),   6, kDoSoundInit,               null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),   7, kDoSoundDispose,            null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),   8, kDoSoundPlay,               null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),   9, kDoSoundStop,               null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),  10, kDoSoundPause,              null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),  11, kDoSoundFade,               "oiiii(i)",             Workarounds.kDoSoundFade_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),  12, kDoSoundSetHold,            null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),  13, kDoSoundDummy,              null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),  14, kDoSoundSetVolume,          "oi",                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),  15, kDoSoundSetPriority,        "oi",                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),  16, kDoSoundSetLoop,            "oi",                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),  17, kDoSoundUpdateCues,         null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),  18, kDoSoundSendMidi,           "oiii(i)",              null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),  19, kDoSoundGlobalReverb,       null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0),  20, kDoSoundUpdate,             null,                   null),
#if ENABLE_SCI32
	        { SIG_SOUNDSCI21,      0, MAP_CALL(DoSoundMasterVolume),       NULL,                   NULL },
            { SIG_SOUNDSCI21,      1, MAP_CALL(DoSoundMute),               NULL,                   NULL },
            { SIG_SOUNDSCI21,      2, MAP_CALL(DoSoundRestore),            NULL,                   NULL },
            { SIG_SOUNDSCI21,      3, MAP_CALL(DoSoundGetPolyphony),       NULL,                   NULL },
            { SIG_SOUNDSCI21,      4, MAP_CALL(DoSoundGetAudioCapability), NULL,                   NULL },
            { SIG_SOUNDSCI21,      5, MAP_CALL(DoSoundSuspend),            NULL,                   NULL },
            { SIG_SOUNDSCI21,      6, MAP_CALL(DoSoundInit),               NULL,                   NULL },
            { SIG_SOUNDSCI21,      7, MAP_CALL(DoSoundDispose),            NULL,                   NULL },
            { SIG_SOUNDSCI21,      8, MAP_CALL(DoSoundPlay),               "o(i)",                 NULL },
	        // ^^ TODO: if this is really the only change between SCI1LATE AND SCI21, we could rename the
	        //     SIG_SOUNDSCI1LATE #define to SIG_SINCE_SOUNDSCI1LATE and make it being SCI1LATE+. Although
	        //     I guess there are many more changes somewhere
	        // TODO: Quest for Glory 4 (SCI2.1) uses the old scheme, we need to detect it accordingly
	        //        signature for SCI21 should be "o"
	        { SIG_SOUNDSCI21,      9, MAP_CALL(DoSoundStop),               NULL,                   NULL },
            { SIG_SOUNDSCI21,     10, MAP_CALL(DoSoundPause),              NULL,                   NULL },
            { SIG_SOUNDSCI21,     11, MAP_CALL(DoSoundFade),               NULL,                   kDoSoundFade_workarounds },
            { SIG_SOUNDSCI21,     12, MAP_CALL(DoSoundSetHold),            NULL,                   NULL },
            { SIG_SOUNDSCI21,     13, MAP_CALL(DoSoundDummy),              NULL,                   NULL },
            { SIG_SOUNDSCI21,     14, MAP_CALL(DoSoundSetVolume),          NULL,                   NULL },
            { SIG_SOUNDSCI21,     15, MAP_CALL(DoSoundSetPriority),        NULL,                   NULL },
            { SIG_SOUNDSCI21,     16, MAP_CALL(DoSoundSetLoop),            NULL,                   NULL },
            { SIG_SOUNDSCI21,     17, MAP_CALL(DoSoundUpdateCues),         NULL,                   NULL },
            { SIG_SOUNDSCI21,     18, MAP_CALL(DoSoundSendMidi),           NULL,                   NULL },
            { SIG_SOUNDSCI21,     19, MAP_CALL(DoSoundGlobalReverb),       NULL,                   NULL },
            { SIG_SOUNDSCI21,     20, MAP_CALL(DoSoundUpdate),             NULL,                   NULL },
#endif
        };

        static readonly SciKernelMapSubEntry[] kFileIO_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),           0, kFileIOOpen,         "r(i)",null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),          0, kFileIOOpen,         "ri",  null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),          1, kFileIOClose,        "i",   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),          2, kFileIOReadRaw,      "iri", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),          3, kFileIOWriteRaw,     "iri", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),          4, kFileIOUnlink,       "r",   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),          5, kFileIOReadString,   "rii", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),          6, kFileIOWriteString,  "ir",  null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),          7, kFileIOSeek,         "iii", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),          8, kFileIOFindFirst,    "rri", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),          9, kFileIOFindNext,     "r",   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),         10, kFileIOExists,       "r",   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI11(0),    11, kFileIORename,       "rr",  null),
        #if ENABLE_SCI32
	        { SIG_SCI32,          13, MAP_CALL(FileIOReadByte),            "i",                    NULL },
            { SIG_SCI32,          14, MAP_CALL(FileIOWriteByte),           "ii",                   NULL },
            { SIG_SCI32,          15, MAP_CALL(FileIOReadWord),            "i",                    NULL },
            { SIG_SCI32,          16, MAP_CALL(FileIOWriteWord),           "ii",                   NULL },
            { SIG_SCI32,          17, MAP_CALL(FileIOCreateSaveSlot),      "ir",                   NULL },
            { SIG_SCI32,          18, MAP_EMPTY(FileIOChangeDirectory),    "r",                    NULL }, // for SQ6, when changing the savegame directory in the save/load dialog
	        { SIG_SCI32,          19, MAP_CALL(FileIOIsValidDirectory),    "r",                    NULL }, // for Torin / Torin demo
        #endif
        };


        //    version,         subId, function-mapping,                    signature,              workarounds
        static readonly SciKernelMapSubEntry[] kGraph_subops = {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),1,kStubNull,"",null), // called by gk1 sci32 right at the start
	        SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),2,kGraphGetColorCount,"",null),
            // 3 - set palette via resource
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),4, kGraphDrawLine,"iiiii(i)(i)",Workarounds.kGraphDrawLine_workarounds),
	        // 5 - nop
	        // 6 - draw pattern
	        SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),7, kGraphSaveBox,              "iiiii",                Workarounds.kGraphSaveBox_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),8, kGraphRestoreBox,           "[r0!]",                Workarounds.kGraphRestoreBox_workarounds),
	        // ^ this may get called with invalid references, we check them within restoreBits() and sierra sci behaves the same
	        SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 9,kGraphFillBoxBackground,    "iiii",                 null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),10,kGraphFillBoxForeground,    "iiii",                 Workarounds.kGraphFillBoxForeground_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),11,kGraphFillBoxAny,           "iiiiii(i)(i)",         Workarounds.kGraphFillBoxAny_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI11(0), 12,kGraphUpdateBox,            "iiii(i)(r0)",          Workarounds.kGraphUpdateBox_workarounds ), // kq6 hires
	        SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),12,kGraphUpdateBox,            "iiii(i)",              Workarounds.kGraphUpdateBox_workarounds ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),13,kGraphRedrawBox,            "iiii",                 Workarounds.kGraphRedrawBox_workarounds ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0),14,kGraphAdjustPriority,       "ii",                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI11(0), 15,kGraphSaveUpscaledHiresBox, "iiii",                 null), // kq6 hires
        };

        static readonly SciKernelMapEntry[] s_kernelMap =
        {
            SciKernelMapEntry.Make(kAbs,SciVersionRange.SIG_EVERYWHERE,"i",null,Workarounds.kAbs_workarounds),
            SciKernelMapEntry.Make(kAddMenu,SciVersionRange.SIG_EVERYWHERE,"rr",null,null),
            SciKernelMapEntry.Make(kAddToEnd,SciVersionRange.SIG_EVERYWHERE,"ln",null,null),
            SciKernelMapEntry.Make(kAddToFront,SciVersionRange.SIG_EVERYWHERE,"ln",null,null),
            SciKernelMapEntry.Make(kAnimate,SciVersionRange.SIG_EVERYWHERE,"(l0)(i)",null,null),
            SciKernelMapEntry.Make(kClone,SciVersionRange.SIG_EVERYWHERE,"o",null,null),
            SciKernelMapEntry.Make(kDisposeClone,SciVersionRange.SIG_EVERYWHERE,"o",null,null),
            SciKernelMapEntry.Make(kDisposeList,SciVersionRange.SIG_EVERYWHERE,"l",null,null),
            SciKernelMapEntry.Make(kDisposeScript,SciVersionRange.SIG_EVERYWHERE,"i(i*)",null,Workarounds.kDisposeScript_workarounds),
            SciKernelMapEntry.Make(kDoSound,SciVersionRange.SIG_EVERYWHERE,"i(.*)",kDoSound_subops,null),
            SciKernelMapEntry.Make(kDrawMenuBar,SciVersionRange.SIG_EVERYWHERE,"i",null,null),
            SciKernelMapEntry.Make(kDrawStatus,SciVersionRange.SIG_EVERYWHERE,"[r0](i)(i)",null,null),
            SciKernelMapEntry.Make("FClose",kFileIOClose,SciVersionRange.SIG_EVERYWHERE,"i",null,null),
            SciKernelMapEntry.Make("FGets",kFileIOReadString,SciVersionRange.SIG_EVERYWHERE,"rii",null,null),
            SciKernelMapEntry.Make("FOpen",kFileIOOpen,SciVersionRange.SIG_EVERYWHERE,"ri",null,null),
            SciKernelMapEntry.Make("FPuts",kFileIOWriteString,SciVersionRange.SIG_EVERYWHERE,"ir",null,null),
            SciKernelMapEntry.Make(kFileIO,SciVersionRange.SIG_EVERYWHERE,"i(.*)",kFileIO_subops,null),
            SciKernelMapEntry.Make(kFindKey,SciVersionRange.SIG_EVERYWHERE,"l.",null,Workarounds.kFindKey_workarounds),
            SciKernelMapEntry.Make(kFirstNode,SciVersionRange.SIG_EVERYWHERE,"[l0]",null,null),
            SciKernelMapEntry.Make(kFlushResources,SciVersionRange.SIG_EVERYWHERE,"i",null,null),
            SciKernelMapEntry.Make(kFormat,SciVersionRange.SIG_EVERYWHERE,"r[ri](.*)",null,null),
            SciKernelMapEntry.Make(kGetCWD,SciVersionRange.SIG_EVERYWHERE,"r",null,null),
            SciKernelMapEntry.Make(kGetSaveDir,SciVersionRange.SIG_SCI32(SIGFOR_ALL),"(r*)",null,null),
            SciKernelMapEntry.Make(kGetSaveDir,SciVersionRange.SIG_EVERYWHERE,"",null,null),
            SciKernelMapEntry.Make(kGameIsRestarting,SciVersionRange.SIG_EVERYWHERE,"(i)",null,null),
            SciKernelMapEntry.Make(kGraph,SciVersionRange.SIG_EVERYWHERE,null,kGraph_subops,null),
            SciKernelMapEntry.Make(kHaveMouse,SciVersionRange.SIG_EVERYWHERE,"",null,null),
            SciKernelMapEntry.Make(kIsObject,SciVersionRange.SIG_EVERYWHERE,".",null,Workarounds.kIsObject_workarounds),
            SciKernelMapEntry.Make(kLastNode,SciVersionRange.SIG_EVERYWHERE,"l",null,null),
            SciKernelMapEntry.Make(kLoad,SciVersionRange.SIG_EVERYWHERE,"ii(i*)",null,null),
            SciKernelMapEntry.Make(kMemoryInfo,SciVersionRange.SIG_EVERYWHERE,"i",null,null),
            SciKernelMapEntry.Make(kNewList,SciVersionRange.SIG_EVERYWHERE,"",null,null),
            SciKernelMapEntry.Make(kNewNode,SciVersionRange.SIG_EVERYWHERE,"..",null,null),
            SciKernelMapEntry.Make(kScriptID,SciVersionRange.SIG_EVERYWHERE,"[io](i)",null,null),
            SciKernelMapEntry.Make(kSetCursor,SciVersionRange.SIG_SCI21(SIGFOR_ALL),"i(i)([io])(i*)",null,null),
	        // TODO: SCI2.1 may supply an object optionally (mother goose sci21 right on startup) - find out why
	        SciKernelMapEntry.Make(kSetCursor,SciVersionRange.SIG_SCI11(SIGFOR_ALL),"i(i)(i)(i)(iiiiii)",null,null),
            SciKernelMapEntry.Make(kSetCursor,SciVersionRange.SIG_EVERYWHERE,"i(i)(i)(i)(i)",null,Workarounds.kSetCursor_workarounds),
            SciKernelMapEntry.Make(kSetMenu,SciVersionRange.SIG_EVERYWHERE,"i(.*)",null,null),
            new SciKernelMapEntry()
        };

        public KernelFunction[] _kernelFuncs;

        public int SelectorNamesSize { get { return _selectorNames.Count; } }

        public Kernel(ResourceManager resMan, SegManager segMan)
        {
            _resMan = resMan;
            _segMan = segMan;
            _invalid = "<invalid>";
        }

        public void Init()
        {
            LoadSelectorNames();
            MapSelectors();      // Map a few special selectors for later use
        }

        private void MapSelectors()
        {
            // species
            // superClass
            _selectorCache._info_ = FindSelector("-info-");
            _selectorCache.y = FindSelector("y");
            _selectorCache.x = FindSelector("x");
            _selectorCache.view = FindSelector("view");
            _selectorCache.loop = FindSelector("loop");
            _selectorCache.cel = FindSelector("cel");
            _selectorCache.underBits = FindSelector("underBits");
            _selectorCache.nsTop = FindSelector("nsTop");
            _selectorCache.nsLeft = FindSelector("nsLeft");
            _selectorCache.nsBottom = FindSelector("nsBottom");
            _selectorCache.lsTop = FindSelector("lsTop");
            _selectorCache.lsLeft = FindSelector("lsLeft");
            _selectorCache.lsBottom = FindSelector("lsBottom");
            _selectorCache.lsRight = FindSelector("lsRight");
            _selectorCache.nsRight = FindSelector("nsRight");
            _selectorCache.signal = FindSelector("signal");
            _selectorCache.illegalBits = FindSelector("illegalBits");
            _selectorCache.brTop = FindSelector("brTop");
            _selectorCache.brLeft = FindSelector("brLeft");
            _selectorCache.brBottom = FindSelector("brBottom");
            _selectorCache.brRight = FindSelector("brRight");

            // name
            // key
            // time
            _selectorCache.text = FindSelector("text");
            _selectorCache.elements = FindSelector("elements");
            // color
            // back
            _selectorCache.mode = FindSelector("mode");
            // style
            _selectorCache.state = FindSelector("state");
            _selectorCache.font = FindSelector("font");
            _selectorCache.type = FindSelector("type");
            // window
            _selectorCache.cursor = FindSelector("cursor");
            _selectorCache.max = FindSelector("max");
            _selectorCache.mark = FindSelector("mark");
            _selectorCache.sort = FindSelector("sort");
            // who
            _selectorCache.message = FindSelector("message");
            // edit
            _selectorCache.play = FindSelector("play");
            _selectorCache.number = FindSelector("number");
            _selectorCache.handle = FindSelector("handle");  // nodePtr
            _selectorCache.client = FindSelector("client");
            _selectorCache.dx = FindSelector("dx");
            _selectorCache.dy = FindSelector("dy");
            _selectorCache.b_movCnt = FindSelector("b -moveCnt");
            _selectorCache.b_i1 = FindSelector("b-i1");
            _selectorCache.b_i2 = FindSelector("b-i2");
            _selectorCache.b_di = FindSelector("b-di");
            _selectorCache.b_xAxis = FindSelector("b-xAxis");
            _selectorCache.b_incr = FindSelector("b-incr");
            _selectorCache.xStep = FindSelector("xStep");
            _selectorCache.yStep = FindSelector("yStep");
            _selectorCache.xLast = FindSelector("xLast");
            _selectorCache.yLast = FindSelector("yLast");
            _selectorCache.moveSpeed = FindSelector("moveSpeed");
            _selectorCache.canBeHere = FindSelector("canBeHere");   // cantBeHere
            _selectorCache.heading = FindSelector("heading");
            _selectorCache.mover = FindSelector("mover");
            _selectorCache.doit = FindSelector("doit");
            _selectorCache.isBlocked = FindSelector("isBlocked");
            _selectorCache.looper = FindSelector("looper");
            _selectorCache.priority = FindSelector("priority");
            _selectorCache.modifiers = FindSelector("modifiers");
            _selectorCache.replay = FindSelector("replay");
            // setPri
            // at
            // next
            // done
            // width
            _selectorCache.wordFail = FindSelector("wordFail");
            _selectorCache.syntaxFail = FindSelector("syntaxFail");
            // semanticFail
            // pragmaFail
            // said
            _selectorCache.claimed = FindSelector("claimed");
            // value
            // save
            // restore
            // title
            // button
            // icon
            // draw
            _selectorCache.delete_ = FindSelector("delete");
            _selectorCache.z = FindSelector("z");
            // -----------------------------
            _selectorCache.size = FindSelector("size");
            _selectorCache.moveDone = FindSelector("moveDone");
            _selectorCache.vol = FindSelector("vol");
            _selectorCache.pri = FindSelector("pri");
            _selectorCache.min = FindSelector("min");
            _selectorCache.sec = FindSelector("sec");
            _selectorCache.frame = FindSelector("frame");
            _selectorCache.dataInc = FindSelector("dataInc");
            _selectorCache.palette = FindSelector("palette");
            _selectorCache.cantBeHere = FindSelector("cantBeHere");
            _selectorCache.nodePtr = FindSelector("nodePtr");
            _selectorCache.flags = FindSelector("flags");
            _selectorCache.points = FindSelector("points");
            _selectorCache.syncCue = FindSelector("syncCue");
            _selectorCache.syncTime = FindSelector("syncTime");
            _selectorCache.printLang = FindSelector("printLang");
            _selectorCache.subtitleLang = FindSelector("subtitleLang");
            _selectorCache.parseLang = FindSelector("parseLang");
            _selectorCache.overlay = FindSelector("overlay");
            _selectorCache.topString = FindSelector("topString");
            _selectorCache.scaleSignal = FindSelector("scaleSignal");
            _selectorCache.scaleX = FindSelector("scaleX");
            _selectorCache.scaleY = FindSelector("scaleY");
            _selectorCache.maxScale = FindSelector("maxScale");
            _selectorCache.vanishingX = FindSelector("vanishingX");
            _selectorCache.vanishingY = FindSelector("vanishingY");
            _selectorCache.iconIndex = FindSelector("iconIndex");
            _selectorCache.select = FindSelector("select");

#if ENABLE_SCI32
            FIND_SELECTOR(data);
            FIND_SELECTOR(picture);
            FIND_SELECTOR(bitmap);
            FIND_SELECTOR(plane);
            FIND_SELECTOR(top);
            FIND_SELECTOR(left);
            FIND_SELECTOR(bottom);
            FIND_SELECTOR(right);
            FIND_SELECTOR(resY);
            FIND_SELECTOR(resX);
            FIND_SELECTOR(dimmed);
            FIND_SELECTOR(fore);
            FIND_SELECTOR(back);
            FIND_SELECTOR(skip);
            FIND_SELECTOR(fixPriority);
            FIND_SELECTOR(mirrored);
            FIND_SELECTOR(visible);
            FIND_SELECTOR(useInsetRect);
            FIND_SELECTOR(inTop);
            FIND_SELECTOR(inLeft);
            FIND_SELECTOR(inBottom);
            FIND_SELECTOR(inRight);
#endif
        }

        public void LoadKernelNames(GameFeatures features)
        {
            _kernelNames.Clear();

            if (ResourceManager.GetSciVersion() <= SciVersion.V1_1)
            {
                _kernelNames.AddRange(s_defaultKernelNames);

                // Some (later) SCI versions replaced CanBeHere by CantBeHere
                // If vocab.999 exists, the kernel function is still named CanBeHere
                if (_selectorCache.cantBeHere != -1)
                    _kernelNames[0x4d] = "CantBeHere";
            }

            switch (ResourceManager.GetSciVersion())
            {
                case SciVersion.V0_EARLY:
                case SciVersion.V0_LATE:
                    // Insert SCI0 file functions after SetCursor (0x28)
                    _kernelNames.Insert(0x29, "FOpen");
                    _kernelNames.Insert(0x2A, "FPuts");
                    _kernelNames.Insert(0x2B, "FGets");
                    _kernelNames.Insert(0x2C, "FClose");

                    // Function 0x55 is DoAvoider
                    _kernelNames[0x55] = "DoAvoider";

                    // Cut off unused functions
                    _kernelNames.RemoveRange(0x72, _kernelNames.Count - 0x72);
                    break;

                case SciVersion.V01:
                    // Multilingual SCI01 games have StrSplit as function 0x78
                    _kernelNames[0x78] = "StrSplit";

                    // Cut off unused functions
                    _kernelNames.RemoveRange(0x79, _kernelNames.Count - 0x79);
                    break;

                case SciVersion.V1_LATE:
                    _kernelNames[0x71] = "MoveCursor";
                    break;

                case SciVersion.V1_1:
                    // In SCI1.1, kSetSynonyms is an empty function
                    _kernelNames[0x26] = "Empty";

                    if (SciEngine.Instance.GameId == SciGameId.KQ6)
                    {
                        // In the Windows version of KQ6 CD, the empty kSetSynonyms
                        // function has been replaced with kPortrait. In KQ6 Mac,
                        // kPlayBack has been replaced by kShowMovie.
                        if (SciEngine.Instance.Platform == Platform.Windows)
                            _kernelNames[0x26] = "Portrait";
                        else if (SciEngine.Instance.Platform == Platform.Macintosh)
                            _kernelNames[0x84] = "ShowMovie";
                    }
                    else if (SciEngine.Instance.GameId == SciGameId.QFG4 && SciEngine.Instance.IsDemo)
                    {
                        _kernelNames[0x7b] = "RemapColors"; // QFG4 Demo has this SCI2 function instead of StrSplit
                    }

                    _kernelNames[0x71] = "PalVary";

                    // At least EcoQuest 1 demo uses kGetMessage instead of kMessage.
                    // Detect which function to use.
                    if (features.DetectMessageFunctionType() == SciVersion.V1_1)
                        _kernelNames[0x7c] = "Message";
                    break;

#if ENABLE_SCI32
                case SciVersion.V2:
                    _kernelNames = Common::StringArray(sci2_default_knames, kKernelEntriesSci2);
                    break;

                case SciVersion.V2_1:
                    if (features.detectSci21KernelType() == SciVersion.V2)
                    {
                        // Some early SCI2.1 games use a modified SCI2 kernel table instead of
                        // the SCI2.1 kernel table. We detect which version to use based on
                        // how kDoSound is called from Sound::play().
                        // Known games that use this:
                        // GK2 demo
                        // KQ7 1.4
                        // PQ4 SWAT demo
                        // LSL6
                        // PQ4CD
                        // QFG4CD

                        // This is interesting because they all have the same interpreter
                        // version (2.100.002), yet they would not be compatible with other
                        // games of the same interpreter.

                        _kernelNames = Common::StringArray(sci2_default_knames, kKernelEntriesGk2Demo);
                        // OnMe is IsOnMe here, but they should be compatible
                        _kernelNames[0x23] = "Robot"; // Graph in SCI2
                        _kernelNames[0x2e] = "Priority"; // DisposeTextBitmap in SCI2
                    }
                    else {
                        // Normal SCI2.1 kernel table
                        _kernelNames = Common::StringArray(sci21_default_knames, kKernelEntriesSci21);
                    }
                    break;

                case SciVersion.V3:
                    _kernelNames = Common::StringArray(sci21_default_knames, kKernelEntriesSci3);

                    // In SCI3, some kernel functions have been removed, and others have been added
                    _kernelNames[0x18] = "Dummy";   // AddMagnify in SCI2.1
                    _kernelNames[0x19] = "Dummy";   // DeleteMagnify in SCI2.1
                    _kernelNames[0x30] = "Dummy";   // SetScroll in SCI2.1
                    _kernelNames[0x39] = "Dummy";   // ShowMovie in SCI2.1
                    _kernelNames[0x4c] = "Dummy";   // ScrollWindow in SCI2.1
                    _kernelNames[0x56] = "Dummy";   // VibrateMouse in SCI2.1 (only used in QFG4 floppy)
                    _kernelNames[0x64] = "Dummy";   // AvoidPath in SCI2.1
                    _kernelNames[0x66] = "Dummy";   // MergePoly in SCI2.1
                    _kernelNames[0x8d] = "MessageBox";  // Dummy in SCI2.1
                    _kernelNames[0x9b] = "Minimize";    // Dummy in SCI2.1

                    break;
#endif

                default:
                    // Use default table for the other versions
                    break;
            }

            MapFunctions();
        }

        private void MapFunctions()
        {
            int mapped = 0;
            int ignored = 0;
            int functionCount = _kernelNames.Count;
            byte platformMask = 0;
            SciVersion myVersion = ResourceManager.GetSciVersion();

            switch (SciEngine.Instance.Platform)
            {
                case Platform.DOS:
                case Platform.FMTowns:
                    platformMask = SIGFOR_DOS;
                    break;
                case Platform.PC98:
                    platformMask = SIGFOR_PC98;
                    break;
                case Platform.Windows:
                    platformMask = SIGFOR_WIN;
                    break;
                case Platform.Macintosh:
                    platformMask = SIGFOR_MAC;
                    break;
                case Platform.Amiga:
                    platformMask = SIGFOR_AMIGA;
                    break;
                case Platform.AtariST:
                    platformMask = SIGFOR_ATARI;
                    break;
                default:
                    break;
            }

            Array.Resize(ref _kernelFuncs, functionCount);

            for (int id = 0; id < functionCount; id++)
            {
                // First, get the name, if known, of the kernel function with number functnr
                string kernelName = _kernelNames[id];

                // Reset the table entry
                _kernelFuncs[id] = new KernelFunction();
                if (string.IsNullOrEmpty(kernelName))
                {
                    // No name was given . must be an unknown opcode
                    // TODO: warning("Kernel function %x unknown", id);
                    continue;
                }

                // Don't map dummy functions - they will never be called
                if (kernelName == "Dummy")
                {
                    _kernelFuncs[id].function = kDummy;
                    continue;
                }

#if ENABLE_SCI32
                // HACK: Phantasmagoria Mac uses a modified kDoSound (which *nothing*
                // else seems to use)!
                if (g_sci.getPlatform() == Common::kPlatformMacintosh && g_sci.getGameId() == GID_PHANTASMAGORIA && kernelName == "DoSound")
                {
                    _kernelFuncs[id].function = kDoSoundPhantasmagoriaMac;
                    _kernelFuncs[id].signature = parseKernelSignature("DoSoundPhantasmagoriaMac", "i.*");
                    _kernelFuncs[id].name = "DoSoundPhantasmagoriaMac";
                    continue;
                }
#endif

                // If the name is known, look it up in s_kernelMap. This table
                // maps kernel func names to actual function (pointers).
                bool nameMatch = false;
                SciKernelMapEntry kernelMap = null;
                for (int i = 0; i < s_kernelMap.Length; i++)
                {
                    kernelMap = s_kernelMap[i];
                    if (kernelName == kernelMap.name)
                    {
                        if ((kernelMap.fromVersion == SciVersion.NONE) || (kernelMap.fromVersion <= myVersion))
                            if ((kernelMap.toVersion == SciVersion.NONE) || (kernelMap.toVersion >= myVersion))
                                if ((platformMask & kernelMap.forPlatform) != 0)
                                    break;
                        nameMatch = true;
                    }
                }

                if (!string.IsNullOrEmpty(kernelMap.name))
                {
                    // A match was found
                    _kernelFuncs[id].function = kernelMap.function;
                    _kernelFuncs[id].name = kernelMap.name;
                    _kernelFuncs[id].signature = ParseKernelSignature(kernelMap.name, kernelMap.signature);
                    _kernelFuncs[id].workarounds = kernelMap.workarounds;
                    if (kernelMap.subFunctions != null)
                    {
                        // Get version for subfunction identification
                        SciVersion mySubVersion = (SciVersion)kernelMap.function(null, 0, null).Offset;
                        // Now check whats the highest subfunction-id for this version
                        SciKernelMapSubEntry kernelSubMap;
                        ushort subFunctionCount = 0;
                        for (int i = 0; i < kernelMap.subFunctions.Length; i++)
                        {
                            kernelSubMap = kernelMap.subFunctions[i];
                            if ((kernelSubMap.fromVersion == SciVersion.NONE) || (kernelSubMap.fromVersion <= mySubVersion))
                                if ((kernelSubMap.toVersion == SciVersion.NONE) || (kernelSubMap.toVersion >= mySubVersion))
                                    if (subFunctionCount <= kernelSubMap.id)
                                        subFunctionCount = (ushort)(kernelSubMap.id + 1);
                        }
                        if (subFunctionCount == 0)
                            throw new InvalidOperationException($"k{kernelName}[{id:X}]: no subfunctions found for requested version");
                        // Now allocate required memory and go through it again
                        _kernelFuncs[id].subFunctionCount = subFunctionCount;
                        var subFunctions = new KernelSubFunction[subFunctionCount];
                        for (int i = 0; i < subFunctionCount; i++)
                        {
                            subFunctions[i] = new KernelSubFunction();
                        }
                        _kernelFuncs[id].subFunctions = subFunctions;
                        // And fill this info out
                        uint kernelSubNr = 0;
                        for (int i = 0; i < kernelMap.subFunctions.Length; i++)
                        {
                            kernelSubMap = kernelMap.subFunctions[i];
                            if ((kernelSubMap.fromVersion == SciVersion.NONE) || (kernelSubMap.fromVersion <= mySubVersion))
                                if ((kernelSubMap.toVersion == SciVersion.NONE) || (kernelSubMap.toVersion >= mySubVersion))
                                {
                                    uint subId = kernelSubMap.id;
                                    if (subFunctions[subId].function == null)
                                    {
                                        subFunctions[subId].function = kernelSubMap.function;
                                        subFunctions[subId].name = kernelSubMap.name;
                                        subFunctions[subId].workarounds = kernelSubMap.workarounds;
                                        if (kernelSubMap.signature != null)
                                        {
                                            subFunctions[subId].signature = ParseKernelSignature(kernelSubMap.name, kernelSubMap.signature);
                                        }
                                        else {
                                            // we go back the submap to find the previous signature for that kernel call
                                            var kernelSubMapBackI = i;
                                            uint kernelSubLeft = kernelSubNr;
                                            while (kernelSubLeft != 0)
                                            {
                                                kernelSubLeft--;
                                                kernelSubMapBackI--;
                                                var kernelSubMapBack = kernelMap.subFunctions[kernelSubMapBackI];
                                                if (kernelSubMapBack.name == kernelSubMap.name)
                                                {
                                                    if (kernelSubMapBack.signature != null)
                                                    {
                                                        subFunctions[subId].signature = ParseKernelSignature(kernelSubMap.name, kernelSubMapBack.signature);
                                                        break;
                                                    }
                                                }
                                            }
                                            if (subFunctions[subId].signature == null)
                                                throw new InvalidOperationException($"k{kernelSubMap.name}: no previous signatures");
                                        }
                                    }
                                }
                            kernelSubNr++;
                        }
                    }
                    ++mapped;
                }
                else {
                    if (nameMatch)
                        throw new InvalidOperationException($"k{kernelName}[{id:X}]: not found for this version/platform");
                    // No match but a name was given . stub
                    // TODO: warning("k%s[%x]: unmapped", kernelName.c_str(), id);
                    _kernelFuncs[id].function = kStub;
                }
            } // for all functions requesting to be mapped

            // TODO: debugC(kDebugLevelVM, "Handled %d/%d kernel functions, mapping %d and ignoring %d.",
            //mapped + ignored, _kernelNames.size(), mapped, ignored);

            return;
        }

        /// <summary>
        /// this parses a written kernel signature into an internal memory format
        /// [io] -> either integer or object
        /// (io) -> optionally integer AND an object
        /// (i) -> optional integer
        /// . -> any type
        /// i* -> optional multiple integers
        /// .* -> any parameters afterwards (or none)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        private ushort[] ParseKernelSignature(string kernelName, string writtenSig)
        {
            var curPos = 0;
            char curChar;
            int size = 0;
            bool validType = false;
            bool optionalType = false;
            bool eitherOr = false;
            bool optional = false;
            bool hadOptional = false;

            // No signature given? no signature out
            if (string.IsNullOrEmpty(writtenSig))
                return null;

            // First, we check how many bytes the result will be
            //  we also check, if the written signature makes any sense
            curPos = 0;
            while (curPos < writtenSig.Length)
            {
                curChar = writtenSig[curPos];
                switch (curChar)
                {
                    case '[': // either or
                        if (eitherOr)
                            throw new InvalidOperationException($"signature for k{kernelName}: '[' used within '[]'");
                        eitherOr = true;
                        validType = false;
                        break;
                    case ']': // either or end
                        if (!eitherOr)
                            throw new InvalidOperationException($"signature for k{kernelName}: ']' used without leading '['");
                        if (!validType)
                            throw new InvalidOperationException($"signature for k{kernelName}: '[]' does not surround valid type(s)");
                        eitherOr = false;
                        validType = false;
                        size++;
                        break;
                    case '(': // optional
                        if (optional)
                            throw new InvalidOperationException($"signature for k{kernelName}: '(' used within '()' brackets");
                        if (eitherOr)
                            throw new InvalidOperationException($"signature for k{kernelName}: '(' used within '[]' brackets");
                        optional = true;
                        validType = false;
                        optionalType = false;
                        break;
                    case ')': // optional end
                        if (!optional)
                            throw new InvalidOperationException($"signature for k{kernelName}: ')' used without leading '('");
                        if (!optionalType)
                            throw new InvalidOperationException($"signature for k{kernelName}: '()' does not to surround valid type(s)");
                        optional = false;
                        validType = false;
                        hadOptional = true;
                        break;
                    case '0': // allowed types
                    case 'i':
                    case 'o':
                    case 'r':
                    case 'l':
                    case 'n':
                    case '.':
                    case '!':
                        if ((hadOptional) & (!optional))
                            throw new InvalidOperationException($"signature for k{kernelName}: non-optional type may not follow optional type");
                        validType = true;
                        if (optional)
                            optionalType = true;
                        if (!eitherOr)
                            size++;
                        break;
                    case '*': // accepts more of the same parameter (must be last char)
                        if (!validType)
                        {
                            if ((0 == curPos) || (writtenSig[curPos - 1] != ']'))
                                throw new InvalidOperationException($"signature for k{kernelName}: a valid type must be in front of '*'");
                        }
                        if (eitherOr)
                            throw new InvalidOperationException($"signature for k{kernelName}: '*' may not be inside '[]'");
                        if (optional)
                        {
                            if ((writtenSig[curPos + 1] != ')') || ((curPos + 2) != writtenSig.Length))
                                throw new InvalidOperationException($"signature for k{kernelName}: '*' may only be used for last type");
                        }
                        else {
                            if ((curPos + 1) != writtenSig.Length)
                                throw new InvalidOperationException($"signature for k{kernelName}: '*' may only be used for last type");
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"signature for k{kernelName}: '{writtenSig[curPos]}' unknown");
                }
                curPos++;
            }

            ushort signature = 0;

            // Now we allocate buffer with required size and fill it
            var result = new ushort[size + 1];
            var writePos = 0;
            curPos = 0;
            while (curPos < writtenSig.Length)
            {
                curChar = writtenSig[curPos];
                if (!eitherOr)
                {
                    // not within either-or, check if next character forces output
                    switch (curChar)
                    {
                        case '\0':
                        case '[':
                        case '(':
                        case ')':
                        case 'i':
                        case 'o':
                        case 'r':
                        case 'l':
                        case 'n':
                        case '.':
                        case '!':
                            // and we also got some signature pending?
                            if (signature != 0)
                            {
                                if ((signature & SIG_MAYBE_ANY) == 0)
                                    throw new InvalidOperationException($"signature for k{kernelName}: invalid ('!') may only get used in combination with a real type");
                                if (((signature & SIG_IS_INVALID) != 0) && ((signature & SIG_MAYBE_ANY) == (SIG_TYPE_NULL | SIG_TYPE_INTEGER)))
                                    throw new InvalidOperationException($"signature for k{kernelName}: invalid ('!') should not be used on exclusive null/integer type");
                                if (optional)
                                {
                                    signature |= SIG_IS_OPTIONAL;
                                    if (curChar != ')')
                                        signature |= SIG_NEEDS_MORE;
                                }
                                result[writePos] = signature;
                                writePos++;
                                signature = 0;
                            }
                            break;
                    }
                }
                switch (curChar)
                {
                    case '[': // either or
                        eitherOr = true;
                        break;
                    case ']': // either or end
                        eitherOr = false;
                        break;
                    case '(': // optional
                        optional = true;
                        break;
                    case ')': // optional end
                        optional = false;
                        break;
                    case '0':
                        if ((signature & SIG_TYPE_NULL) != 0)
                            throw new InvalidOperationException($"signature for k{kernelName}: NULL ('0') specified more than once");
                        signature |= SIG_TYPE_NULL;
                        break;
                    case 'i':
                        if ((signature & SIG_TYPE_INTEGER) != 0)
                            throw new InvalidOperationException($"signature for k{kernelName}: integer ('i') specified more than once");
                        signature |= SIG_TYPE_INTEGER | SIG_TYPE_NULL;
                        break;
                    case 'o':
                        if ((signature & SIG_TYPE_OBJECT) != 0)
                            throw new InvalidOperationException($"signature for k{kernelName}: object ('o') specified more than once");
                        signature |= SIG_TYPE_OBJECT;
                        break;
                    case 'r':
                        if ((signature & SIG_TYPE_REFERENCE) != 0)
                            throw new InvalidOperationException($"signature for k{kernelName}: reference ('r') specified more than once");
                        signature |= SIG_TYPE_REFERENCE;
                        break;
                    case 'l':
                        if ((signature & SIG_TYPE_LIST) != 0)
                            throw new InvalidOperationException($"signature for k{kernelName}: list ('l') specified more than once");
                        signature |= SIG_TYPE_LIST;
                        break;
                    case 'n':
                        if ((signature & SIG_TYPE_NODE) != 0)
                            throw new InvalidOperationException($"signature for k{kernelName}: node ('n') specified more than once");
                        signature |= SIG_TYPE_NODE;
                        break;
                    case '.':
                        if ((signature & SIG_MAYBE_ANY) != 0)
                            throw new InvalidOperationException($"signature for k{kernelName}: maybe-any ('.') shouldn't get specified with other types in front of it");
                        signature |= SIG_MAYBE_ANY;
                        break;
                    case '!':
                        if ((signature & SIG_IS_INVALID) != 0)
                            throw new InvalidOperationException($"signature for k{kernelName}: invalid ('!') specified more than once");
                        signature |= SIG_IS_INVALID;
                        break;
                    case '*': // accepts more of the same parameter
                        signature |= SIG_MORE_MAY_FOLLOW;
                        break;
                    default:
                        break;
                }
                curPos++;
            }

            // and we also got some signature pending?
            if (signature != 0)
            {
                if ((signature & SIG_MAYBE_ANY) == 0)
                    throw new InvalidOperationException($"signature for k{kernelName}: invalid ('!') may only get used in combination with a real type");
                if (((signature & SIG_IS_INVALID) != 0) && ((signature & SIG_MAYBE_ANY) == (SIG_TYPE_NULL | SIG_TYPE_INTEGER)))
                    throw new InvalidOperationException($"signature for k{kernelName}: invalid ('!') should not be used on exclusive null/integer type");
                if (optional)
                {
                    signature |= SIG_IS_OPTIONAL;
                    signature |= SIG_NEEDS_MORE;
                }
                result[writePos] = signature;
                writePos++;
                signature = 0;
            }

            // Write terminator
            result[writePos] = 0;

            return result;
        }

        public int FindSelector(string selectorName)
        {
            for (int pos = 0; pos < _selectorNames.Count; ++pos)
            {
                if (_selectorNames[pos] == selectorName)
                    return pos;
            }

            // TODO/ debugC(kDebugLevelVM, "Could not map '%s' to any selector", selectorName);

            return -1;
        }

        private void LoadSelectorNames()
        {
            var r = _resMan.FindResource(new ResourceId(ResourceType.Vocab, VOCAB_RESOURCE_SELECTORS), false);
            bool oldScriptHeader = (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY);

            // Starting with KQ7, Mac versions have a BE name table. GK1 Mac and earlier (and all
            // other platforms) always use LE.
            bool isBE = (SciEngine.Instance.Platform == Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V2_1
                    && SciEngine.Instance.GameId != SciGameId.GK1);

            if (r == null)
            { // No such resource?
              // Check if we have a table for this game
              // Some demos do not have a selector table
                var staticSelectorTable = CheckStaticSelectorNames();

                if (staticSelectorTable.Length == 0)
                    throw new InvalidOperationException("Kernel: Could not retrieve selector names");
                else {
                    // TODO: warning("No selector vocabulary found, using a static one");
                }

                for (var i = 0; i < staticSelectorTable.Length; i++)
                {
                    _selectorNames.Add(staticSelectorTable[i]);
                    if (oldScriptHeader)
                        _selectorNames.Add(staticSelectorTable[i]);
                }

                return;
            }

            int count = (isBE ? r.data.ToUInt16BigEndian() : r.data.ToUInt16()) + 1; // Counter is slightly off

            for (int i = 0; i < count; i++)
            {
                int offset = isBE ? r.data.ToUInt16BigEndian(2 + i * 2) : r.data.ToUInt16(2 + i * 2);
                int len = isBE ? r.data.ToUInt16BigEndian(offset) : r.data.ToUInt16(offset);

                byte[] tmp1 = new byte[len];
                Array.Copy(r.data, offset + 2, tmp1, 0, len);
                string tmp = System.Text.Encoding.UTF8.GetString(tmp1);
                _selectorNames.Add(tmp);
                //debug("%s", tmp.c_str());

                // Early SCI versions used the LSB in the selector ID as a read/write
                // toggle. To compensate for that, we add every selector name twice.
                if (oldScriptHeader)
                    _selectorNames.Add(tmp);
            }
        }

        private string[] CheckStaticSelectorNames()
        {
            string[] names;
            int offset = (ResourceManager.GetSciVersion() < SciVersion.V1_1) ? 3 : 0;

#if ENABLE_SCI32
            const int count = (getSciVersion() <= SCI_VERSION_1_1) ? ARRAYSIZE(sci0Selectors) + offset : ARRAYSIZE(sci2Selectors);
#else
            int count = sci0Selectors.Length + offset;
#endif
            int countSci1 = sci1Selectors.Length;
            int countSci11 = sci11Selectors.Length;

            // Resize the list of selector names and fill in the SCI 0 names.
            names = new string[count];
            if (ResourceManager.GetSciVersion() <= SciVersion.V1_LATE)
            {
                // Fill selectors 0 - 2 for SCI0 - SCI1 late
                names[0] = "species";
                names[1] = "superClass";
                names[2] = "-info-";
            }

            if (ResourceManager.GetSciVersion() <= SciVersion.V1_1)
            {
                // SCI0 - SCI11
                for (int i = offset; i < count; i++)
                    names[i] = sci0Selectors[i - offset];

                if (ResourceManager.GetSciVersion() > SciVersion.V01)
                {
                    // Several new selectors were added in SCI 1 and later.
                    names = new string[count + countSci1];
                    for (int i = count; i < count + countSci1; i++)
                        names[i] = sci1Selectors[i - count];
                }

                if (ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                {
                    // Several new selectors were added in SCI 1.1
                    names = new string[count + countSci1 + countSci11];
                    for (int i = count + countSci1; i < count + countSci1 + countSci11; i++)
                        names[i] = sci11Selectors[i - count - countSci1];
                }
#if ENABLE_SCI32
            }
            else {
                // SCI2+
                for (int i = 0; i < count; i++)
                    names[i] = sci2Selectors[i];
#endif
            }

            FindSpecificSelectors(names);

            throw new NotImplementedException();
            //foreach (var selectorRemap in Selectors.sciSelectorRemap)
            //{
            //    if (ResourceManager.GetSciVersion() >= selectorRemap.minVersion && ResourceManager.GetSciVersion() <= selectorRemap.maxVersion)
            //    {
            //        var slot = selectorRemap.slot;
            //        if (slot >= names.Length)
            //            names.resize(slot + 1);
            //        names[slot] = selectorRemap.name;
            //    }
            //}

            return names;
        }

        public string GetSelectorName(int selector)
        {
            if (selector >= _selectorNames.Count)
            {
                // This should only occur in games w/o a selector-table
                //  We need this for proper workaround tables
                // TODO: maybe check, if there is a fixed selector-table and error() out in that case
                for (var loopSelector = _selectorNames.Count; loopSelector <= selector; ++loopSelector)
                    _selectorNames.Add($"<noname{loopSelector}>");
            }

            // Ensure that the selector has a name
            if (string.IsNullOrEmpty(_selectorNames[selector]))
                _selectorNames[selector] = $"<noname{selector}>";

            return _selectorNames[selector];
        }

        private void FindSpecificSelectors(string[] selectorNames)
        {
            throw new NotImplementedException();

            //// Now, we need to find out selectors which keep changing place...
            //// We do that by dissecting game objects, and looking for selectors at
            //// specified locations.

            //// We need to initialize script 0 here, to make sure that it's always
            //// located at segment 1.
            //_segMan.InstantiateScript(0);
            //ushort sci2Offset = (ushort)((ResourceManager.GetSciVersion() >= SciVersion.V2) ? 64000 : 0);

            //// The Actor class contains the init, xLast and yLast selectors, which
            //// we reference directly. It's always in script 998, so we need to
            //// explicitly load it here.
            //if ((ResourceManager.GetSciVersion() >= SciVersion.V1_EGA_ONLY))
            //{
            //    ushort actorScript = 998;

            //    if (_resMan.TestResource(new ResourceId(ResourceType.Script, (ushort)(actorScript + sci2Offset)))!=null)
            //    {
            //        _segMan.InstantiateScript(actorScript + sci2Offset);

            //        var actorClass = _segMan.GetObject(_segMan.FindObjectByName("Actor"));

            //        if (actorClass != null)
            //        {
            //            // Find the xLast and yLast selectors, used in kDoBresen

            //            int offset = (ResourceManager.GetSciVersion() < SciVersion.V1_1) ? 3 : 0;
            //            int offset2 = (ResourceManager.GetSciVersion() >= SciVersion.V2) ? 12 : 0;
            //            // xLast and yLast always come between illegalBits and xStep
            //            int illegalBitsSelectorPos = actorClass.LocateVarSelector(_segMan, 15 + offset + offset2); // illegalBits
            //            int xStepSelectorPos = actorClass.LocateVarSelector(_segMan, 51 + offset + offset2);   // xStep
            //            if (xStepSelectorPos - illegalBitsSelectorPos != 3)
            //            {
            //                throw new InvalidOperationException($"illegalBits and xStep selectors aren't found in known locations. illegalBits = {illegalBitsSelectorPos}, xStep = {xStepSelectorPos}");
            //            }

            //            int xLastSelectorPos = actorClass.GetVarSelector(illegalBitsSelectorPos + 1);
            //            int yLastSelectorPos = actorClass.GetVarSelector(illegalBitsSelectorPos + 2);

            //            if (selectorNames.Length < (uint)yLastSelectorPos + 1)
            //                selectorNames.resize((uint)yLastSelectorPos + 1);

            //            selectorNames[xLastSelectorPos] = "xLast";
            //            selectorNames[yLastSelectorPos] = "yLast";
            //        }   // if (actorClass)

            //        _segMan.UninstantiateScript(998);
            //    }   // if (_resMan.testResource(ResourceId(kResourceTypeScript, 998)))
            //}   // if ((ResourceManager.GetSciVersion() >= SCI_VERSION_1_EGA_ONLY))

            //// Find selectors from specific classes

            //for (int i = 0; i < classReferences.Length; i++)
            //{
            //    if (!_resMan.TestResource(new ResourceId(ResourceType.Script, classReferences[i].script + sci2Offset)))
            //        continue;

            //    _segMan.InstantiateScript(classReferences[i].script + sci2Offset);

            //    var targetClass = _segMan.GetObject(_segMan.FindObjectByName(classReferences[i].className));
            //    int targetSelectorPos = 0;
            //    uint selectorOffset = classReferences[i].selectorOffset;

            //    if (targetClass)
            //    {
            //        if (classReferences[i].selectorType == kSelectorMethod)
            //        {
            //            if (targetClass.getMethodCount() < selectorOffset + 1)
            //                error("The %s class has less than %d methods (%d)",
            //                        classReferences[i].className, selectorOffset + 1,
            //                        targetClass.getMethodCount());

            //            targetSelectorPos = targetClass.getFuncSelector(selectorOffset);
            //        }
            //        else {
            //            // Add the global selectors to the selector ID
            //            selectorOffset += (ResourceManager.GetSciVersion() <= SCI_VERSION_1_LATE) ? 3 : 8;

            //            if (targetClass.getVarCount() < selectorOffset + 1)
            //                error("The %s class has less than %d variables (%d)",
            //                        classReferences[i].className, selectorOffset + 1,
            //                        targetClass.getVarCount());

            //            targetSelectorPos = targetClass.getVarSelector(selectorOffset);
            //        }

            //        if (selectorNames.size() < (uint32)targetSelectorPos + 1)
            //            selectorNames.resize((uint32)targetSelectorPos + 1);


            //        selectorNames[targetSelectorPos] = classReferences[i].selectorName;
            //    }
            //}

            //// Reset the segment manager
            //_segMan.ResetSegMan();
        }

        private string LookupText(Register address, int index)
        {
            if (address.Segment != 0)
                return _segMan.GetString(address);

            int _index = index;
            var textres = _resMan.FindResource(new ResourceId(ResourceType.Text, (ushort)address.Offset), false);

            if (textres == null)
            {
                throw new InvalidOperationException($"text.{address.Offset} not found");
            }

            var textlen = textres.size;
            var seeker = textres.data;
            var i = 0;

            while ((index--) != 0)
                while (((textlen--) != 0) && (seeker[i++] != 0))
                    ;

            if (textlen != 0)
                return ScummHelper.GetText(seeker, i);

            throw new InvalidOperationException("Index {_index} out of bounds in text.{address.Offset}");
        }
    }
}
