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
using System.Linq;
using static NScumm.Core.DebugHelper;
using System.Linq.Expressions;
using System.Reflection;

namespace NScumm.Sci.Engine
{
    internal class SciKernelMapSubEntry
    {
        public SciVersion fromVersion;
        public SciVersion toVersion;

        public ushort id;

        public string name;
        public KernelFunctionCall function;

        public string signature;
        public SciWorkaroundEntry[] workarounds;

        public static SciKernelMapSubEntry Make(SciVersionRange range, int id, KernelFunctionCall call, string signature,
            SciWorkaroundEntry[] workarounds)
        {
            return new SciKernelMapSubEntry
            {
                name = call.Method.Name.Remove(0, 1),
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                id = (ushort) id,
                function = call,
                signature = signature,
                workarounds = workarounds
            };
        }
    }

    internal class SciVersionRange
    {
        public SciVersion fromVersion;
        public SciVersion toVersion;
        public byte forPlatform;

        public static readonly SciVersionRange SIG_EVERYWHERE = new SciVersionRange {forPlatform = Kernel.SIGFOR_ALL};

        public static SciVersionRange SIG_SCI0(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.NONE,
                toVersion = SciVersion.V01,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI1(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V1_EGA_ONLY,
                toVersion = SciVersion.V1_LATE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI11(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V1_1,
                toVersion = SciVersion.V1_1,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SINCE_SCI11(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V1_1,
                toVersion = SciVersion.NONE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI21EARLY(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2_1_EARLY,
                toVersion = SciVersion.V2_1_EARLY,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_UNTIL_SCI21EARLY(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2,
                toVersion = SciVersion.V2_1_EARLY,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_UNTIL_SCI21MID(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2,
                toVersion = SciVersion.V2_1_MIDDLE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SINCE_SCI21(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2_1_EARLY,
                toVersion = SciVersion.V3,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SINCE_SCI21MID(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2_1_MIDDLE,
                toVersion = SciVersion.V3,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SINCE_SCI21LATE(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2_1_LATE,
                toVersion = SciVersion.V3,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI16(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.NONE,
                toVersion = SciVersion.V1_1,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCI32(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2,
                toVersion = SciVersion.NONE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SCIALL(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.NONE,
                toVersion = SciVersion.NONE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SOUNDSCI0(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V0_EARLY,
                toVersion = SciVersion.V0_LATE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SOUNDSCI1EARLY(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V1_EARLY,
                toVersion = SciVersion.V1_EARLY,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SOUNDSCI1LATE(byte platform)
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V1_LATE,
                toVersion = SciVersion.V1_LATE,
                forPlatform = platform
            };
        }

        public static SciVersionRange SIG_SOUNDSCI21()
        {
            return new SciVersionRange
            {
                fromVersion = SciVersion.V2_1_EARLY,
                toVersion = SciVersion.V3
            };
        }
    }

    internal struct ClassReference
    {
        public int script;
        public string className;
        public string selectorName;
        public SelectorType selectorType;
        public uint selectorOffset;

        public ClassReference(int script, string className, string selectorName, SelectorType selectorType,
            uint selectorOffset)
        {
            this.script = script;
            this.className = className;
            this.selectorName = selectorName;
            this.selectorType = selectorType;
            this.selectorOffset = selectorOffset;
        }
    }

    internal class SciKernelMapEntry
    {
        public string name;
        public KernelFunctionCall function;

        public SciVersion fromVersion;
        public SciVersion toVersion;
        public byte forPlatform;

        public string signature;
        public SciKernelMapSubEntry[] subFunctions;
        public SciWorkaroundEntry[] workarounds;

        public static SciKernelMapEntry Make(string name, KernelFunctionCall function, SciVersionRange range,
            string signature, SciKernelMapSubEntry[] subSignatures = null, SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapEntry
            {
                name = name,
                function = function,
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                forPlatform = range.forPlatform,
                signature = signature,
                subFunctions = subSignatures,
                workarounds = workarounds
            };
        }

        public static SciKernelMapEntry Make(KernelFunctionCall function, SciVersionRange range, string signature,
            SciKernelMapSubEntry[] subSignatures = null, SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapEntry
            {
                name = function.Method.Name.Remove(0, 1),
                function = function,
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                forPlatform = range.forPlatform,
                signature = signature,
                subFunctions = subSignatures,
                workarounds = workarounds
            };
        }

        public static SciKernelMapEntry MakeDummy(string name, SciVersionRange range, string signature,
            SciKernelMapSubEntry[] subSignatures = null, SciWorkaroundEntry[] workarounds = null)
        {
            return new SciKernelMapEntry
            {
                name = name,
                function = Kernel.kDummy,
                fromVersion = range.fromVersion,
                toVersion = range.toVersion,
                forPlatform = range.forPlatform,
                signature = signature,
                subFunctions = subSignatures,
                workarounds = workarounds
            };
        }
    }

    internal class KernelSubFunction
    {
        public KernelFunctionCall function;
        public string name;
        public ushort[] signature;
        public SciWorkaroundEntry[] workarounds;
        public bool debugLogging;
        public bool debugBreakpoint;
    }

    internal delegate Register KernelFunctionCall(EngineState s, int argc, StackPtr argv);

    internal class KernelFunction
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

    internal partial class Kernel
    {
#if ENABLE_SCI32
        private const int kKernelEntriesSci2 = 0x8b;
        private const int kKernelEntriesGk2Demo = 0xa0;
        private const int kKernelEntriesSci21 = 0x9d;
        private const int kKernelEntriesSci3 = 0xa1;
#endif
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
        public const int SIG_TYPE_UNINITIALIZED = 0x04; // may be FFFF:*    . not allowable, only used for comparison
        public const int SIG_TYPE_OBJECT = 0x08; // may be object    [o]
        public const int SIG_TYPE_REFERENCE = 0x10; // may be reference [r]
        public const int SIG_TYPE_LIST = 0x20; // may be list      [l]
        public const int SIG_TYPE_NODE = 0x40; // may be node      [n]

        public const int SIG_TYPE_ERROR = 0x80;
            // happens, when there is a identification error - only used for comparison

        public const int SIG_IS_INVALID = 0x100; // ptr is invalid   [!] . invalid offset
        public const int SIG_IS_OPTIONAL = 0x200; // is optional
        public const int SIG_NEEDS_MORE = 0x400; // needs at least one additional parameter following
        public const int SIG_MORE_MAY_FOLLOW = 0x800; // may have more parameters of the same type following

        // this does not include SIG_TYPE_UNINITIALIZED, because we can not allow uninitialized values anywhere
        public const int SIG_MAYBE_ANY =
            (SIG_TYPE_NULL | SIG_TYPE_INTEGER | SIG_TYPE_OBJECT | SIG_TYPE_REFERENCE | SIG_TYPE_LIST | SIG_TYPE_NODE);


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

        private static readonly string[] sci0Selectors =
        {
            "y", "x", "view", "loop", "cel", //  0 -  4
            "underBits", "nsTop", "nsLeft", "nsBottom", "nsRight", //  5 -  9
            "lsTop", "lsLeft", "lsBottom", "lsRight", "signal", // 10 - 14
            "illegalBits", "brTop", "brLeft", "brBottom", "brRight", // 15 - 19
            "name", "key", "time", "text", "elements", // 20 - 25
            "color", "back", "mode", "style", "state", // 25 - 29
            "font", "type", "window", "cursor", "max", // 30 - 34
            "mark", "who", "message", "edit", "play", // 35 - 39
            "number", "handle", "client", "dx", "dy", // 40 - 44
            "b-moveCnt", "b-i1", "b-i2", "b-di", "b-xAxis", // 45 - 49
            "b-incr", "xStep", "yStep", "moveSpeed", "canBeHere", // 50 - 54
            "heading", "mover", "doit", "isBlocked", "looper", // 55 - 59
            "priority", "modifiers", "replay", "setPri", "at", // 60 - 64
            "next", "done", "width", "wordFail", "syntaxFail", // 65 - 69
            "semanticFail", "pragmaFail", "said", "claimed", "value", // 70 - 74
            "save", "restore", "title", "button", "icon", // 75 - 79
            "draw", "delete", "z" // 80 - 82
        };

        private static readonly string[] sci1Selectors =
        {
            "parseLang", "printLang", "subtitleLang", "size", "points", // 83 - 87
            "palette", "dataInc", "handle", "min", "sec", // 88 - 92
            "frame", "vol", "pri", "perform", "moveDone" // 93 - 97
        };

        private static readonly string[] sci11Selectors =
        {
            "topString", "flags", "quitGame", "restart", "hide", // 98 - 102
            "scaleSignal", "scaleX", "scaleY", "maxScale", "vanishingX", // 103 - 107
            "vanishingY" // 108
        };

#if ENABLE_SCI32
        private static readonly string[] sci2Selectors =
        {
                "plane",           "x",           "y",            "z",     "scaleX", //  0 -  4
               "scaleY",    "maxScale",    "priority",  "fixPriority",     "inLeft", //  5 -  9
                "inTop",     "inRight",    "inBottom", "useInsetRect",       "view", // 10 - 14
                 "loop",         "cel",      "bitmap",       "nsLeft",      "nsTop", // 15 - 19
              "nsRight",    "nsBottom",      "lsLeft",        "lsTop",    "lsRight", // 20 - 25
             "lsBottom",      "signal", "illegalBits",       "brLeft",      "brTop", // 25 - 29
              "brRight",    "brBottom",        "name",          "key",       "time", // 30 - 34
                 "text",    "elements",        "fore",         "back",       "mode", // 35 - 39
                "style",       "state",        "font",         "type",     "window", // 40 - 44
               "cursor",         "max",        "mark",          "who",    "message", // 45 - 49
                 "edit",        "play",      "number",      "nodePtr",     "client", // 50 - 54
                   "dx",          "dy",   "b-moveCnt",         "b-i1",       "b-i2", // 55 - 59
                 "b-di",     "b-xAxis",      "b-incr",        "xStep",      "yStep", // 60 - 64
            "moveSpeed",  "cantBeHere",     "heading",        "mover",       "doit", // 65 - 69
            "isBlocked",      "looper",   "modifiers",       "replay",     "setPri", // 70 - 74
                   "at",        "next",        "done",        "width", "pragmaFail", // 75 - 79
              "claimed",       "value",        "save",      "restore",      "title", // 80 - 84
               "button",        "icon",        "draw",       "delete",  "printLang", // 85 - 89
                 "size",      "points",     "palette",      "dataInc",     "handle", // 90 - 94
                  "min",         "sec",       "frame",          "vol",    "perform", // 95 - 99
             "moveDone",   "topString",       "flags",     "quitGame",    "restart", // 100 - 104
                 "hide", "scaleSignal",  "vanishingX",   "vanishingY",    "picture", // 105 - 109
                 "resX",        "resY",   "coordType",         "data",       "skip", // 110 - 104
               "center",         "all",        "show",     "textLeft",    "textTop", // 115 - 119
            "textRight",  "textBottom", "borderColor",    "titleFore",  "titleBack", // 120 - 124
            "titleFont",      "dimmed",    "frameOut",      "lastKey",  "magnifier", // 125 - 129
             "magPower",    "mirrored",       "pitch",         "roll",        "yaw", // 130 - 134
                 "left",       "right",         "top",       "bottom",   "numLines"  // 135 - 139
        };
#endif

        /** Default kernel name table. */

        private static readonly string[] s_defaultKernelNames =
        {
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
            /*0x26*/ "SetSynonyms", // Portrait (KQ6 hires)
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
            /*0x4d*/ "CanBeHere", // CantBeHere in newer SCI versions
            /*0x4e*/ "OnControl",
            /*0x4f*/ "InitBresen",
            /*0x50*/ "DoBresen",
            /*0x51*/ "Platform", // DoAvoider (SCI0)
            /*0x52*/ "SetJump",
            /*0x53*/ "SetDebug", // for debugging
            /*0x54*/ "InspectObj", // for debugging
            /*0x55*/ "ShowSends", // for debugging
            /*0x56*/ "ShowObjs", // for debugging
            /*0x57*/ "ShowFree", // for debugging
            /*0x58*/ "MemoryInfo",
            /*0x59*/ "StackUsage", // for debugging
            /*0x5a*/ "Profiler", // for debugging
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
            /*0x6e*/ "ShiftScreen", // never called?
            /*0x6f*/ "Palette",
            /*0x70*/ "MemorySegment",
            /*0x71*/ "Intersections", // MoveCursor (SCI1 late), PalVary (SCI1.1)
            /*0x72*/ "Memory",
            /*0x73*/ "ListOps", // never called?
            /*0x74*/ "FileIO",
            /*0x75*/ "DoAudio",
            /*0x76*/ "DoSync",
            /*0x77*/ "AvoidPath",
            /*0x78*/ "Sort", // StrSplit (SCI01)
            /*0x79*/ "ATan", // never called?
            /*0x7a*/ "Lock",
            /*0x7b*/ "StrSplit",
            /*0x7c*/ "GetMessage", // Message (SCI1.1)
            /*0x7d*/ "IsItSkip",
            /*0x7e*/ "MergePoly",
            /*0x7f*/ "ResCheck",
            /*0x80*/ "AssertPalette",
            /*0x81*/ "TextColors",
            /*0x82*/ "TextFonts",
            /*0x83*/ "Record", // for debugging
            /*0x84*/ "PlayBack", // for debugging
            /*0x85*/ "ShowMovie",
            /*0x86*/ "SetVideoMode",
            /*0x87*/ "SetQuitStr",
            /*0x88*/ "DbugStr" // for debugging
        };

        #if ENABLE_SCI32

// NOTE: 0x72-0x79, 0x85-0x86, 0x88 are from the GK2 demo (which has debug support) and are
// just Dummy in other SCI2 games.
        private static readonly string[] sci2_default_knames = {
	/*0x00*/ "Load",
	/*0x01*/ "UnLoad",
	/*0x02*/ "ScriptID",
	/*0x03*/ "DisposeScript",
	/*0x04*/ "Lock",
	/*0x05*/ "ResCheck",
	/*0x06*/ "Purge",
	/*0x07*/ "Clone",
	/*0x08*/ "DisposeClone",
	/*0x09*/ "RespondsTo",
	/*0x0a*/ "SetNowSeen",
	/*0x0b*/ "NumLoops",
	/*0x0c*/ "NumCels",
	/*0x0d*/ "CelWide",
	/*0x0e*/ "CelHigh",
	/*0x0f*/ "GetHighPlanePri",
	/*0x10*/ "GetHighItemPri",		// unused function
	/*0x11*/ "ShakeScreen",
	/*0x12*/ "OnMe",
	/*0x13*/ "ShowMovie",
	/*0x14*/ "SetVideoMode",
	/*0x15*/ "AddScreenItem",
	/*0x16*/ "DeleteScreenItem",
	/*0x17*/ "UpdateScreenItem",
	/*0x18*/ "FrameOut",
	/*0x19*/ "AddPlane",
	/*0x1a*/ "DeletePlane",
	/*0x1b*/ "UpdatePlane",
	/*0x1c*/ "RepaintPlane",		// unused function
	/*0x1d*/ "SetShowStyle",
	/*0x1e*/ "ShowStylePercent",	// unused function
	/*0x1f*/ "SetScroll",
	/*0x20*/ "AddMagnify",
	/*0x21*/ "DeleteMagnify",
	/*0x22*/ "IsHiRes",
	/*0x23*/ "Graph",		// Robot in early SCI2.1 games with a SCI2 kernel table
	/*0x24*/ "InvertRect",	// only in SCI2, not used in any SCI2 game
	/*0x25*/ "TextSize",
	/*0x26*/ "Message",
	/*0x27*/ "TextColors",
	/*0x28*/ "TextFonts",
	/*0x29*/ "Dummy",
	/*0x2a*/ "SetQuitStr",
	/*0x2b*/ "EditText",
	/*0x2c*/ "InputText",			// unused function
	/*0x2d*/ "CreateTextBitmap",
	/*0x2e*/ "DisposeTextBitmap",	// Priority in early SCI2.1 games with a SCI2 kernel table
	/*0x2f*/ "GetEvent",
	/*0x30*/ "GlobalToLocal",
	/*0x31*/ "LocalToGlobal",
	/*0x32*/ "MapKeyToDir",
	/*0x33*/ "HaveMouse",
	/*0x34*/ "SetCursor",
	/*0x35*/ "VibrateMouse",
	/*0x36*/ "SaveGame",
	/*0x37*/ "RestoreGame",
	/*0x38*/ "RestartGame",
	/*0x39*/ "GameIsRestarting",
	/*0x3a*/ "MakeSaveCatName",
	/*0x3b*/ "MakeSaveFileName",
	/*0x3c*/ "GetSaveFiles",
	/*0x3d*/ "GetSaveDir",
	/*0x3e*/ "CheckSaveGame",
	/*0x3f*/ "CheckFreeSpace",
	/*0x40*/ "DoSound",
	/*0x41*/ "DoAudio",
	/*0x42*/ "DoSync",
	/*0x43*/ "NewList",
	/*0x44*/ "DisposeList",
	/*0x45*/ "NewNode",
	/*0x46*/ "FirstNode",
	/*0x47*/ "LastNode",
	/*0x48*/ "EmptyList",
	/*0x49*/ "NextNode",
	/*0x4a*/ "PrevNode",
	/*0x4b*/ "NodeValue",
	/*0x4c*/ "AddAfter",
	/*0x4d*/ "AddToFront",
	/*0x4e*/ "AddToEnd",
	/*0x4f*/ "Dummy",
	/*0x50*/ "Dummy",
	/*0x51*/ "FindKey",
	/*0x52*/ "Dummy",
	/*0x53*/ "Dummy",
	/*0x54*/ "Dummy",
	/*0x55*/ "DeleteKey",
	/*0x56*/ "Dummy",
	/*0x57*/ "Dummy",
	/*0x58*/ "ListAt",
	/*0x59*/ "ListIndexOf",
	/*0x5a*/ "ListEachElementDo",
	/*0x5b*/ "ListFirstTrue",
	/*0x5c*/ "ListAllTrue",
	/*0x5d*/ "Random",
	/*0x5e*/ "Abs",
	/*0x5f*/ "Sqrt",
	/*0x60*/ "GetAngle",
	/*0x61*/ "GetDistance",
	/*0x62*/ "ATan",
	/*0x63*/ "SinMult",
	/*0x64*/ "CosMult",
	/*0x65*/ "SinDiv",
	/*0x66*/ "CosDiv",
	/*0x67*/ "GetTime",
	/*0x68*/ "Platform",
	/*0x69*/ "BaseSetter",
	/*0x6a*/ "DirLoop",
	/*0x6b*/ "CantBeHere",
	/*0x6c*/ "InitBresen",
	/*0x6d*/ "DoBresen",
	/*0x6e*/ "SetJump",
	/*0x6f*/ "AvoidPath",
	/*0x70*/ "InPolygon",
	/*0x71*/ "MergePoly",
	/*0x72*/ "SetDebug",
	/*0x73*/ "InspectObject",     // for debugging
	/*0x74*/ "MemoryInfo",
	/*0x75*/ "Profiler",          // for debugging
	/*0x76*/ "Record",            // for debugging
	/*0x77*/ "PlayBack",          // for debugging
	/*0x78*/ "MonoOut",           // for debugging
	/*0x79*/ "SetFatalStr",       // for debugging
	/*0x7a*/ "GetCWD",
	/*0x7b*/ "ValidPath",
	/*0x7c*/ "FileIO",
	/*0x7d*/ "Dummy",
	/*0x7e*/ "DeviceInfo",
	/*0x7f*/ "Palette",
	/*0x80*/ "PalVary",
	/*0x81*/ "PalCycle",
	/*0x82*/ "Array",
	/*0x83*/ "String",
	/*0x84*/ "RemapColors",
	/*0x85*/ "IntegrityChecking", // for debugging
	/*0x86*/ "CheckIntegrity",	  // for debugging
	/*0x87*/ "ObjectIntersect",
	/*0x88*/ "MarkMemory",	      // for debugging
	/*0x89*/ "TextWidth",		  // for debugging(?), only in SCI2, not used in any SCI2 game
	/*0x8a*/ "PointSize",	      // for debugging(?), only in SCI2, not used in any SCI2 game

	/*0x8b*/ "AddLine",
	/*0x8c*/ "DeleteLine",
	/*0x8d*/ "UpdateLine",
	/*0x8e*/ "AddPolygon",
	/*0x8f*/ "DeletePolygon",
	/*0x90*/ "UpdatePolygon",
	/*0x91*/ "Bitmap",
	/*0x92*/ "ScrollWindow",
	/*0x93*/ "SetFontRes",
	/*0x94*/ "MovePlaneItems",
	/*0x95*/ "PreloadResource",
	/*0x96*/ "Dummy",
	/*0x97*/ "ResourceTrack",
	/*0x98*/ "CheckCDisc",
	/*0x99*/ "GetSaveCDisc",
	/*0x9a*/ "TestPoly",
	/*0x9b*/ "WinHelp",
	/*0x9c*/ "LoadChunk",
	/*0x9d*/ "SetPalStyleRange",
	/*0x9e*/ "AddPicAt",
	/*0x9f*/ "MessageBox"
};

        private static readonly string[] sci21_default_knames = {
	/*0x00*/ "Load",
	/*0x01*/ "UnLoad",
	/*0x02*/ "ScriptID",
	/*0x03*/ "DisposeScript",
	/*0x04*/ "Lock",
	/*0x05*/ "ResCheck",
	/*0x06*/ "Purge",
	/*0x07*/ "SetLanguage",
	/*0x08*/ "Dummy",
	/*0x09*/ "Dummy",
	/*0x0a*/ "Clone",
	/*0x0b*/ "DisposeClone",
	/*0x0c*/ "RespondsTo",
	/*0x0d*/ "FindSelector",
	/*0x0e*/ "FindClass",
	/*0x0f*/ "Dummy",
	/*0x10*/ "Dummy",
	/*0x11*/ "Dummy",
	/*0x12*/ "Dummy",
	/*0x13*/ "Dummy",
	/*0x14*/ "SetNowSeen",
	/*0x15*/ "NumLoops",
	/*0x16*/ "NumCels",
	/*0x17*/ "IsOnMe",
	/*0x18*/ "AddMagnify",		// dummy in SCI3
	/*0x19*/ "DeleteMagnify",	// dummy in SCI3
	/*0x1a*/ "CelRect",
	/*0x1b*/ "BaseLineSpan",
	/*0x1c*/ "CelWide",
	/*0x1d*/ "CelHigh",
	/*0x1e*/ "AddScreenItem",
	/*0x1f*/ "DeleteScreenItem",
	/*0x20*/ "UpdateScreenItem",
	/*0x21*/ "FrameOut",
	/*0x22*/ "CelInfo",
	/*0x23*/ "Bitmap",
	/*0x24*/ "CelLink",
	/*0x25*/ "Dummy",
	/*0x26*/ "Dummy",
	/*0x27*/ "Dummy",
	/*0x28*/ "AddPlane",
	/*0x29*/ "DeletePlane",
	/*0x2a*/ "UpdatePlane",
	/*0x2b*/ "RepaintPlane",
	/*0x2c*/ "GetHighPlanePri",
	/*0x2d*/ "GetHighItemPri",		// unused function
	/*0x2e*/ "SetShowStyle",
	/*0x2f*/ "ShowStylePercent",	// unused function
	/*0x30*/ "SetScroll",			// dummy in SCI3
	/*0x31*/ "MovePlaneItems",
	/*0x32*/ "ShakeScreen",
	/*0x33*/ "Dummy",
	/*0x34*/ "Dummy",
	/*0x35*/ "Dummy",
	/*0x36*/ "Dummy",
	/*0x37*/ "IsHiRes",
	/*0x38*/ "SetVideoMode",
	/*0x39*/ "ShowMovie",			// dummy in SCI3
	/*0x3a*/ "Robot",
	/*0x3b*/ "CreateTextBitmap",
	/*0x3c*/ "Random",
	/*0x3d*/ "Abs",
	/*0x3e*/ "Sqrt",
	/*0x3f*/ "GetAngle",
	/*0x40*/ "GetDistance",
	/*0x41*/ "ATan",
	/*0x42*/ "SinMult",
	/*0x43*/ "CosMult",
	/*0x44*/ "SinDiv",
	/*0x45*/ "CosDiv",
	/*0x46*/ "Text",
	/*0x47*/ "Dummy",
	/*0x48*/ "Message",
	/*0x49*/ "Font",
	/*0x4a*/ "EditText",
	/*0x4b*/ "InputText",		// unused function
	/*0x4c*/ "ScrollWindow",	// Dummy in SCI3
	/*0x4d*/ "Dummy",
	/*0x4e*/ "Dummy",
	/*0x4f*/ "Dummy",
	/*0x50*/ "GetEvent",
	/*0x51*/ "GlobalToLocal",
	/*0x52*/ "LocalToGlobal",
	/*0x53*/ "MapKeyToDir",
	/*0x54*/ "HaveMouse",
	/*0x55*/ "SetCursor",
	/*0x56*/ "VibrateMouse",	// Dummy in SCI3
	/*0x57*/ "Dummy",
	/*0x58*/ "Dummy",
	/*0x59*/ "Dummy",
	/*0x5a*/ "List",
	/*0x5b*/ "Array",
	/*0x5c*/ "String",
	/*0x5d*/ "FileIO",
	/*0x5e*/ "BaseSetter",
	/*0x5f*/ "DirLoop",
	/*0x60*/ "CantBeHere",
	/*0x61*/ "InitBresen",
	/*0x62*/ "DoBresen",
	/*0x63*/ "SetJump",
	/*0x64*/ "AvoidPath",		// dummy in SCI3
	/*0x65*/ "InPolygon",
	/*0x66*/ "MergePoly",		// dummy in SCI3
	/*0x67*/ "ObjectIntersect",
	/*0x68*/ "Dummy",
	/*0x69*/ "MemoryInfo",
	/*0x6a*/ "DeviceInfo",
	/*0x6b*/ "Palette",
	/*0x6c*/ "PalVary",
	/*0x6d*/ "PalCycle",
	/*0x6e*/ "RemapColors",
	/*0x6f*/ "AddLine",
	/*0x70*/ "DeleteLine",
	/*0x71*/ "UpdateLine",
	/*0x72*/ "AddPolygon",
	/*0x73*/ "DeletePolygon",
	/*0x74*/ "UpdatePolygon",
	/*0x75*/ "DoSound",
	/*0x76*/ "DoAudio",
	/*0x77*/ "DoSync",
	/*0x78*/ "Save",
	/*0x79*/ "GetTime",
	/*0x7a*/ "Platform",
	/*0x7b*/ "CD",
	/*0x7c*/ "SetQuitStr",
	/*0x7d*/ "GetConfig",
	/*0x7e*/ "Table",
	/*0x7f*/ "WinHelp",		// Windows only
	/*0x80*/ "Dummy",
	/*0x81*/ "Dummy",		// called when changing rooms in most SCI2.1 games (e.g. KQ7, GK2, MUMG deluxe, Phant1)
	/*0x82*/ "Dummy",
	/*0x83*/ "PrintDebug",	// debug function, used by Shivers (demo and full)
	/*0x84*/ "Dummy",
	/*0x85*/ "Dummy",
	/*0x86*/ "Dummy",
	/*0x87*/ "Dummy",
	/*0x88*/ "Dummy",
	/*0x89*/ "Dummy",
	/*0x8a*/ "LoadChunk",
	/*0x8b*/ "SetPalStyleRange",
	/*0x8c*/ "AddPicAt",
	/*0x8d*/ "Dummy",	// MessageBox in SCI3
	/*0x8e*/ "NewRoom",		// debug function
	/*0x8f*/ "Dummy",
	/*0x90*/ "Priority",
	/*0x91*/ "MorphOn",
	/*0x92*/ "PlayVMD",
	/*0x93*/ "SetHotRectangles",
	/*0x94*/ "MulDiv",
	/*0x95*/ "GetSierraProfileInt", // , Windows only
	/*0x96*/ "GetSierraProfileString", // , Windows only
	/*0x97*/ "SetWindowsOption", // Windows only
	/*0x98*/ "GetWindowsOption", // Windows only
	/*0x99*/ "WinDLL", // Windows only
	/*0x9a*/ "Dummy",
	/*0x9b*/ "Dummy",	// Minimize in SCI3
	/*0x9c*/ "DeletePic",
	// == SCI3 only ===============
	/*0x9d*/ "Dummy",
	/*0x9e*/ "WebConnect",
	/*0x9f*/ "Dummy",
	/*0xa0*/ "PlayDuck"
};

#endif


        private static readonly SciKernelMapSubEntry[] kDoSound_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 0, kDoSoundInit, "o", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 1, kDoSoundPlay, "o", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 2, kDoSoundRestore, "(o)", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 3, kDoSoundDispose, "o", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 4, kDoSoundMute, "(i)", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 5, kDoSoundStop, "o", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 6, kDoSoundPause, "i", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 7, kDoSoundResumeAfterRestore, "", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 8, kDoSoundMasterVolume, "(i)", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 9, kDoSoundUpdate, "o", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 10, kDoSoundFade, "[o0]",
                Workarounds.kDoSoundFade_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 11, kDoSoundGetPolyphony, "", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 12, kDoSoundStopAll, "", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 0, kDoSoundMasterVolume, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 1, kDoSoundMute, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 2, kDoSoundRestore, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 3, kDoSoundGetPolyphony, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 4, kDoSoundUpdate, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 5, kDoSoundInit, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 6, kDoSoundDispose, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 7, kDoSoundPlay, "oi", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 8, kDoSoundStop, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 9, kDoSoundPause, "[o0]i", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 10, kDoSoundFade, "oiiii",
                Workarounds.kDoSoundFade_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 11, kDoSoundUpdateCues, "o", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 12, kDoSoundSendMidi, "oiii", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 13, kDoSoundGlobalReverb, "(i)", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 14, kDoSoundSetHold, "oi", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 15, kDoSoundDummy, "", null),
            //  ^^ Longbow demo
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 0, kDoSoundMasterVolume, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 1, kDoSoundMute, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 2, kDoSoundRestore, "", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 3, kDoSoundGetPolyphony, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 4, kDoSoundGetAudioCapability, "", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 5, kDoSoundSuspend, "i", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 6, kDoSoundInit, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 7, kDoSoundDispose, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 8, kDoSoundPlay, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 9, kDoSoundStop, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 10, kDoSoundPause, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 11, kDoSoundFade, "oiiii(i)",
                Workarounds.kDoSoundFade_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 12, kDoSoundSetHold, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 13, kDoSoundDummy, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 14, kDoSoundSetVolume, "oi", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 15, kDoSoundSetPriority, "oi", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 16, kDoSoundSetLoop, "oi", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 17, kDoSoundUpdateCues, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 18, kDoSoundSendMidi, "oiii(i)", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 19, kDoSoundGlobalReverb, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 20, kDoSoundUpdate, null, null),
#if ENABLE_SCI32
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 0, kDoSoundMasterVolume, null, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),      1, kDoSoundMute,               null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),      2, kDoSoundRestore,            null,                   null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),      3, kDoSoundGetPolyphony,       null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),      4, kDoSoundGetAudioCapability, null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),      5, kDoSoundSuspend,            null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),      6, kDoSoundInit,               null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),      7, kDoSoundDispose,            null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),      8, kDoSoundPlay,               "o(i)",                 null ),
	        // ^^ TODO: if this is really the only change between SCI1LATE AND SCI21, we could rename the
	        //     SIG_SOUNDSCI1LATE #define to SIG_SINCE_SOUNDSCI1LATE and make it being SCI1LATE+. Although
	        //     I guess there are many more changes somewhere
	        // TODO: Quest for Glory 4 (SCI2.1) uses the old scheme, we need to detect it accordingly
	        //        signature for SCI21 should be "o"
	        SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),      9, kDoSoundStop,               null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),     10, kDoSoundPause,              null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),     11, kDoSoundFade,               null,                   Workarounds.kDoSoundFade_workarounds ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),     12, kDoSoundSetHold,            null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),     13, kDoSoundDummy,              null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),     14, kDoSoundSetVolume,          null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),     15, kDoSoundSetPriority,        null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),     16, kDoSoundSetLoop,            null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),     17, kDoSoundUpdateCues,         null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),     18, kDoSoundSendMidi,           null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),     19, kDoSoundGlobalReverb,       null,                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(),     20, kDoSoundUpdate,             null,                   null ),
#endif
        };

        private static readonly SciKernelMapSubEntry[] kFileIO_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 0, kFileIOOpen, "r(i)", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 0, kFileIOOpen, "ri", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 1, kFileIOClose, "i", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 2, kFileIOReadRaw, "iri", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 3, kFileIOWriteRaw, "iri", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 4, kFileIOUnlink, "r", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 5, kFileIOReadString, "rii", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 6, kFileIOWriteString, "ir", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 7, kFileIOSeek, "iii", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 8, kFileIOFindFirst, "rri", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 9, kFileIOFindNext, "r", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 10, kFileIOExists, "r", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI11(0), 11, kFileIORename, "rr", null),
#if ENABLE_SCI32
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),          13, kFileIOReadByte,            "i",                    null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),          14, kFileIOWriteByte,           "ii",                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),          15, kFileIOReadWord,            "i",                    null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),          16, kFileIOWriteWord,           "ii",                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),          17, kFileIOCreateSaveSlot,      "ir",                   null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),          18, kFileIOChangeDirectory,     "r",                    null ), // for SQ6, when changing the savegame directory in the save/load dialog
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),          19, kFileIOIsValidDirectory,    "r",                    null ), // for Torin / Torin demo
        #endif
        };


        //    version,         subId, function-mapping,                    signature,              workarounds
        private static readonly SciKernelMapSubEntry[] kGraph_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 1, kStubNull, "", null),
            // called by gk1 sci32 right at the start
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 2, kGraphGetColorCount, "", null),
            // 3 - set palette via resource
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 4, kGraphDrawLine, "iiiii(i)(i)",
                Workarounds.kGraphDrawLine_workarounds),
            // 5 - nop
            // 6 - draw pattern
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 7, kGraphSaveBox, "iiiii",
                Workarounds.kGraphSaveBox_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 8, kGraphRestoreBox, "[r0!]",
                Workarounds.kGraphRestoreBox_workarounds),
            // ^ this may get called with invalid references, we check them within restoreBits() and sierra sci behaves the same
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 9, kGraphFillBoxBackground, "iiii", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 10, kGraphFillBoxForeground, "iiii",
                Workarounds.kGraphFillBoxForeground_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 11, kGraphFillBoxAny, "iiiiii(i)(i)",
                Workarounds.kGraphFillBoxAny_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI11(0), 12, kGraphUpdateBox, "iiii(i)(r0)",
                Workarounds.kGraphUpdateBox_workarounds), // kq6 hires
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 12, kGraphUpdateBox, "iiii(i)",
                Workarounds.kGraphUpdateBox_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 13, kGraphRedrawBox, "iiii",
                Workarounds.kGraphRedrawBox_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 14, kGraphAdjustPriority, "ii", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI11(0), 15, kGraphSaveUpscaledHiresBox, "iiii", null),
            // kq6 hires
        };

        private static readonly SciKernelMapSubEntry[] kPalVary_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 0, kPalVaryInit, "ii(i)(i)(i)", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 0, kPalVaryInit, "ii(i)(i)", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 1, kPalVaryReverse, "(i)(i)(i)", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 2, kPalVaryGetCurrentStep, "", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 3, kPalVaryDeinit, "", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 4, kPalVaryChangeTarget, "i", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 5, kPalVaryChangeTicks, "i", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 6, kPalVaryPauseResume, "i", null),
#if ENABLE_SCI32
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),           0, kPalVarySetVary,            "i(i)(i)(ii)",          null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),           1, kPalVarySetPercent,         "(i)(i)",               Workarounds.kPalVarySetPercent_workarounds ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),           2, kPalVaryGetPercent,         "",                     null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),           3, kPalVaryOff,                "",                     null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),           4, kPalVaryMergeTarget,        "i",                    null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),           5, kPalVarySetTime,            "i",                    null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),           6, kPalVaryPauseResume,        "i",                    null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),           7, kPalVarySetTarget,          "i",                    null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),           8, kPalVarySetStart,           "i",                    null ),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0),           9, kPalVaryMergeStart,         "i",                    null ),
#endif
        };

        private static readonly SciKernelMapSubEntry[] kPalette_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 1, kPaletteSetFromResource, "i(i)", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 2, kPaletteSetFlag, "iii", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 3, kPaletteUnsetFlag, "iii",
                Workarounds.kPaletteUnsetFlag_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 4, kPaletteSetIntensity, "iii(i)", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 5, kPaletteFindColor, "iii", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 6, kPaletteAnimate, "i*", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 7, kPaletteSave, "", null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 8, kPaletteRestore, "[r0]", null),
        };

        private static readonly SciKernelMapEntry[] s_kernelMap =
        {
            SciKernelMapEntry.Make(kAbs, SciVersionRange.SIG_EVERYWHERE, "i", null, Workarounds.kAbs_workarounds),
            SciKernelMapEntry.Make(kAddAfter, SciVersionRange.SIG_EVERYWHERE, "lnn", null, null),
            SciKernelMapEntry.Make(kAddMenu, SciVersionRange.SIG_EVERYWHERE, "rr", null, null),
            SciKernelMapEntry.Make(kAddToEnd, SciVersionRange.SIG_EVERYWHERE, "ln", null, null),
            SciKernelMapEntry.Make(kAddToFront, SciVersionRange.SIG_EVERYWHERE, "ln", null, null),
            SciKernelMapEntry.Make(kAddToPic, SciVersionRange.SIG_EVERYWHERE, "[il](iiiiii)", null, null),
            SciKernelMapEntry.Make(kAnimate, SciVersionRange.SIG_EVERYWHERE, "(l0)(i)", null, null),
            SciKernelMapEntry.Make(kAssertPalette, SciVersionRange.SIG_EVERYWHERE, "i", null, null),
            SciKernelMapEntry.Make(kAvoidPath, SciVersionRange.SIG_EVERYWHERE, "ii(.*)", null, null),
            SciKernelMapEntry.Make(kBaseSetter, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kCanBeHere, SciVersionRange.SIG_EVERYWHERE, "o(l)", null, null),
#if ENABLE_SCI32
//{ "CantBeHere", kCantBeHere32, SIG_SCI32, SIGFOR_ALL,    "ol",                    null,            null },
#endif
            SciKernelMapEntry.Make(kCantBeHere, SciVersionRange.SIG_EVERYWHERE, "o(l)", null, null),
            SciKernelMapEntry.Make(kCelHigh, SciVersionRange.SIG_EVERYWHERE, "ii(i)", null,
                Workarounds.kCelHigh_workarounds),
            SciKernelMapEntry.Make(kCelWide, SciVersionRange.SIG_EVERYWHERE, "ii(i)", null,
                Workarounds.kCelWide_workarounds),
            SciKernelMapEntry.Make(kCheckFreeSpace, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "r.*", null, null),
            SciKernelMapEntry.Make(kCheckFreeSpace, SciVersionRange.SIG_SCI11(SIGFOR_ALL), "r(i)", null, null),
            SciKernelMapEntry.Make(kCheckFreeSpace, SciVersionRange.SIG_EVERYWHERE, "r", null, null),
            SciKernelMapEntry.Make(kCheckSaveGame, SciVersionRange.SIG_EVERYWHERE, ".*", null, null),
            SciKernelMapEntry.Make(kClone, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kCoordPri, SciVersionRange.SIG_EVERYWHERE, "i(i)", null, null),
            SciKernelMapEntry.Make(kCosDiv, SciVersionRange.SIG_EVERYWHERE, "ii", null, null),
            SciKernelMapEntry.Make(kDeleteKey, SciVersionRange.SIG_EVERYWHERE, "l.", null,
                Workarounds.kDeleteKey_workarounds),
            SciKernelMapEntry.Make(kDeviceInfo, SciVersionRange.SIG_EVERYWHERE, "i(r)(r)(i)", null,
                Workarounds.kDeviceInfo_workarounds), // subop
            SciKernelMapEntry.Make(kDirLoop, SciVersionRange.SIG_EVERYWHERE, "oi", null,
                Workarounds.kDirLoop_workarounds),
            SciKernelMapEntry.Make(kDisposeClone, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kDisposeList, SciVersionRange.SIG_EVERYWHERE, "l", null, null),
            SciKernelMapEntry.Make(kDisposeScript, SciVersionRange.SIG_EVERYWHERE, "i(i*)", null,
                Workarounds.kDisposeScript_workarounds),
            SciKernelMapEntry.Make(kDisposeWindow, SciVersionRange.SIG_EVERYWHERE, "i(i)", null, null),
            SciKernelMapEntry.Make(kDisplay, SciVersionRange.SIG_EVERYWHERE, "[ir]([ir!]*)", null,
                Workarounds.kDisplay_workarounds),
            // ^ we allow invalid references here, because kDisplay gets called with those in e.g. pq3 during intro
            //    restoreBits() checks and skips invalid handles, so that's fine. Sierra SCI behaved the same
            SciKernelMapEntry.Make(kDoAudio, SciVersionRange.SIG_EVERYWHERE, "i(.*)", null, null), // subop
            //SciKernelMapEntry.Make(kDoAvoider,SciVersionRange.    SIG_EVERYWHERE,           "o(i)",                  null,            null },
            SciKernelMapEntry.Make(kDoBresen, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kDoSound, SciVersionRange.SIG_EVERYWHERE, "i(.*)", kDoSound_subops, null),
            SciKernelMapEntry.Make(kDoSync, SciVersionRange.SIG_EVERYWHERE, "i(.*)", null, null), // subop
            SciKernelMapEntry.Make(kDrawCel, SciVersionRange.SIG_SCI11(SIGFOR_PC), "iiiii(i)(i)([ri])", null, null),
            // reference for kq6 hires
            SciKernelMapEntry.Make(kDrawCel, SciVersionRange.SIG_EVERYWHERE, "iiiii(i)(i)", null, null),
            SciKernelMapEntry.Make(kDrawControl, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kDrawMenuBar, SciVersionRange.SIG_EVERYWHERE, "i", null, null),
            SciKernelMapEntry.Make(kDrawPic, SciVersionRange.SIG_EVERYWHERE, "i(i)(i)(i)", null, null),
            SciKernelMapEntry.Make(kDrawStatus, SciVersionRange.SIG_EVERYWHERE, "[r0](i)(i)", null, null),
            SciKernelMapEntry.Make(kEditControl, SciVersionRange.SIG_EVERYWHERE, "[o0][o0]", null, null),
            SciKernelMapEntry.Make(kEmpty, SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.Make(kEmptyList, SciVersionRange.SIG_EVERYWHERE, "l", null, null),
            SciKernelMapEntry.Make("FClose", kFileIOClose, SciVersionRange.SIG_EVERYWHERE, "i", null, null),
            SciKernelMapEntry.Make("FGets", kFileIOReadString, SciVersionRange.SIG_EVERYWHERE, "rii", null, null),
            SciKernelMapEntry.Make("FOpen", kFileIOOpen, SciVersionRange.SIG_EVERYWHERE, "ri", null, null),
            SciKernelMapEntry.Make("FPuts", kFileIOWriteString, SciVersionRange.SIG_EVERYWHERE, "ir", null, null),
            SciKernelMapEntry.Make(kFileIO, SciVersionRange.SIG_EVERYWHERE, "i(.*)", kFileIO_subops, null),
            SciKernelMapEntry.Make(kFindKey, SciVersionRange.SIG_EVERYWHERE, "l.", null,
                Workarounds.kFindKey_workarounds),
            SciKernelMapEntry.Make(kFirstNode, SciVersionRange.SIG_EVERYWHERE, "[l0]", null, null),
            SciKernelMapEntry.Make(kFlushResources, SciVersionRange.SIG_EVERYWHERE, "i", null, null),
            SciKernelMapEntry.Make(kFormat, SciVersionRange.SIG_EVERYWHERE, "r[ri](.*)", null, null),
            SciKernelMapEntry.Make(kGameIsRestarting, SciVersionRange.SIG_EVERYWHERE, "(i)", null, null),
            SciKernelMapEntry.Make(kGetAngle, SciVersionRange.SIG_EVERYWHERE, "iiii", null,
                Workarounds.kGetAngle_workarounds),
            SciKernelMapEntry.Make(kGetCWD, SciVersionRange.SIG_EVERYWHERE, "r", null, null),
            SciKernelMapEntry.Make(kGetDistance, SciVersionRange.SIG_EVERYWHERE, "ii(i)(i)(i)(i)", null, null),
            SciKernelMapEntry.Make(kGetEvent, SciVersionRange.SIG_SCIALL(SIGFOR_MAC), "io(i*)", null, null),
            SciKernelMapEntry.Make(kGetEvent, SciVersionRange.SIG_EVERYWHERE, "io", null, null),
            SciKernelMapEntry.Make(kGetFarText, SciVersionRange.SIG_EVERYWHERE, "ii[r0]", null, null),
            SciKernelMapEntry.Make(kGetMenu, SciVersionRange.SIG_EVERYWHERE, "i.", null, null),
            SciKernelMapEntry.Make(kGetMessage, SciVersionRange.SIG_EVERYWHERE, "iiir", null, null),
            SciKernelMapEntry.Make(kGetPort, SciVersionRange.SIG_EVERYWHERE, "", null, null),
            SciKernelMapEntry.Make(kGetSaveDir, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "(r*)", null, null),
            SciKernelMapEntry.Make(kGetSaveDir, SciVersionRange.SIG_EVERYWHERE, "", null, null),
            SciKernelMapEntry.Make(kGetSaveFiles, SciVersionRange.SIG_EVERYWHERE, "rrr", null, null),
            SciKernelMapEntry.Make(kGetTime, SciVersionRange.SIG_EVERYWHERE, "(i)", null, null),
            SciKernelMapEntry.Make(kGlobalToLocal, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "oo", null, null),
            SciKernelMapEntry.Make(kGlobalToLocal, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kGraph, SciVersionRange.SIG_EVERYWHERE, null, kGraph_subops, null),
            SciKernelMapEntry.Make(kHaveMouse, SciVersionRange.SIG_EVERYWHERE, "", null, null),
            SciKernelMapEntry.Make(kHiliteControl, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kInitBresen, SciVersionRange.SIG_EVERYWHERE, "o(i)", null, null),
            SciKernelMapEntry.Make(kIntersections, SciVersionRange.SIG_EVERYWHERE, "iiiiriiiri", null, null),
            SciKernelMapEntry.Make(kIsItSkip, SciVersionRange.SIG_EVERYWHERE, "iiiii", null, null),
            SciKernelMapEntry.Make(kIsObject, SciVersionRange.SIG_EVERYWHERE, ".", null,
                Workarounds.kIsObject_workarounds),
            SciKernelMapEntry.Make(kJoystick, SciVersionRange.SIG_EVERYWHERE, "i(.*)", null, null), // subop
            SciKernelMapEntry.Make(kLastNode, SciVersionRange.SIG_EVERYWHERE, "l", null, null),
            SciKernelMapEntry.Make(kLoad, SciVersionRange.SIG_EVERYWHERE, "ii(i*)", null, null),
            SciKernelMapEntry.Make(kLocalToGlobal, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "oo", null, null),
            SciKernelMapEntry.Make(kLocalToGlobal, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kLock, SciVersionRange.SIG_EVERYWHERE, "ii(i)", null, null),
            SciKernelMapEntry.Make(kMapKeyToDir, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kMemory, SciVersionRange.SIG_EVERYWHERE, "i(.*)", null,
                Workarounds.kMemory_workarounds), // subop
            SciKernelMapEntry.Make(kMemoryInfo, SciVersionRange.SIG_EVERYWHERE, "i", null, null),
            SciKernelMapEntry.Make(kMemorySegment, SciVersionRange.SIG_EVERYWHERE, "ir(i)", null, null), // subop
            SciKernelMapEntry.Make(kMenuSelect, SciVersionRange.SIG_EVERYWHERE, "o(i)", null, null),
            SciKernelMapEntry.Make(kMergePoly, SciVersionRange.SIG_EVERYWHERE, "rli", null, null),
            SciKernelMapEntry.Make(kMessage, SciVersionRange.SIG_EVERYWHERE, "i(.*)", null, null), // subop
            SciKernelMapEntry.Make(kMoveCursor, SciVersionRange.SIG_EVERYWHERE, "ii", null,
                Workarounds.kMoveCursor_workarounds),
            SciKernelMapEntry.Make(kNewList, SciVersionRange.SIG_EVERYWHERE, "", null, null),
            SciKernelMapEntry.Make(kNewNode, SciVersionRange.SIG_EVERYWHERE, "..", null, null),
            SciKernelMapEntry.Make(kNewWindow, SciVersionRange.SIG_SCIALL(SIGFOR_MAC), ".*", null, null),
            SciKernelMapEntry.Make(kNewWindow, SciVersionRange.SIG_SCI0(SIGFOR_ALL), "iiii[r0]i(i)(i)(i)", null, null),
            SciKernelMapEntry.Make(kNewWindow, SciVersionRange.SIG_SCI1(SIGFOR_ALL), "iiii[ir]i(i)(i)([ir])(i)(i)(i)(i)",
                null, null),
            SciKernelMapEntry.Make(kNewWindow, SciVersionRange.SIG_SCI11(SIGFOR_ALL), "iiiiiiii[r0]i(i)(i)(i)", null,
                Workarounds.kNewWindow_workarounds),
            SciKernelMapEntry.Make(kNextNode, SciVersionRange.SIG_EVERYWHERE, "n", null, null),
            SciKernelMapEntry.Make(kNodeValue, SciVersionRange.SIG_EVERYWHERE, "[n0]", null, null),
            SciKernelMapEntry.Make(kNumCels, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kNumLoops, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kOnControl, SciVersionRange.SIG_EVERYWHERE, "ii(i)(i)(i)", null, null),
            SciKernelMapEntry.Make(kPalVary, SciVersionRange.SIG_EVERYWHERE, "i(i*)", kPalVary_subops, null),
            SciKernelMapEntry.Make(kPalette, SciVersionRange.SIG_EVERYWHERE, "i(.*)", kPalette_subops, null),
            SciKernelMapEntry.Make(kParse, SciVersionRange.SIG_EVERYWHERE, "ro", null, null),
            SciKernelMapEntry.Make(kPicNotValid, SciVersionRange.SIG_EVERYWHERE, "(i)", null, null),
            SciKernelMapEntry.Make(kPlatform, SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            //SciKernelMapEntry.Make(kPortrait,SciVersionRange.SIG_EVERYWHERE,"i(.*)",null,null), // subop
            SciKernelMapEntry.Make(kPrevNode, SciVersionRange.SIG_EVERYWHERE, "n", null, null),
            SciKernelMapEntry.Make(kPriCoord, SciVersionRange.SIG_EVERYWHERE, "i", null, null),
            SciKernelMapEntry.Make(kRandom, SciVersionRange.SIG_EVERYWHERE, "i(i)(i)", null, null),
            SciKernelMapEntry.Make(kReadNumber, SciVersionRange.SIG_EVERYWHERE, "r", null,
                Workarounds.kReadNumber_workarounds),
            SciKernelMapEntry.Make(kRemapColors, SciVersionRange.SIG_SCI11(SIGFOR_ALL), "i(i)(i)(i)(i)", null, null),
#if ENABLE_SCI32
//{ "RemapColors", kRemapColors32, SIG_SCI32, SIGFOR_ALL,  "i(i)(i)(i)(i)(i)",      null,            null },
#endif
            SciKernelMapEntry.Make(kResCheck, SciVersionRange.SIG_EVERYWHERE, "ii(iiii)", null, null),
            SciKernelMapEntry.Make(kRespondsTo, SciVersionRange.SIG_EVERYWHERE, ".i", null, null),
            SciKernelMapEntry.Make(kRestartGame, SciVersionRange.SIG_EVERYWHERE, "", null, null),
            SciKernelMapEntry.Make(kRestoreGame, SciVersionRange.SIG_EVERYWHERE, "[r0]i[r0]", null, null),
            SciKernelMapEntry.Make(kSaid, SciVersionRange.SIG_EVERYWHERE, "[r0]", null, null),
            SciKernelMapEntry.Make(kSaveGame, SciVersionRange.SIG_EVERYWHERE, "[r0]i[r0](r0)", null, null),
            SciKernelMapEntry.Make(kScriptID, SciVersionRange.SIG_EVERYWHERE, "[io](i)", null, null),
            SciKernelMapEntry.Make(kSetCursor, SciVersionRange.SIG_SINCE_SCI21(SIGFOR_ALL), "i(i)([io])(i*)", null, null),
            // TODO: SCI2.1 may supply an object optionally (mother goose sci21 right on startup) - find out why
            SciKernelMapEntry.Make(kSetCursor, SciVersionRange.SIG_SCI11(SIGFOR_ALL), "i(i)(i)(i)(iiiiii)", null, null),
            SciKernelMapEntry.Make(kSetCursor, SciVersionRange.SIG_EVERYWHERE, "i(i)(i)(i)(i)", null,
                Workarounds.kSetCursor_workarounds),
            SciKernelMapEntry.Make(kSetDebug, SciVersionRange.SIG_EVERYWHERE, "(i*)", null, null),
            SciKernelMapEntry.Make(kSetJump, SciVersionRange.SIG_EVERYWHERE, "oiii", null, null),
            SciKernelMapEntry.Make(kSetMenu, SciVersionRange.SIG_EVERYWHERE, "i(.*)", null, null),
            SciKernelMapEntry.Make(kSetNowSeen, SciVersionRange.SIG_EVERYWHERE, "o(i)", null, null),
            SciKernelMapEntry.Make(kSetPort, SciVersionRange.SIG_EVERYWHERE, "i(iiiii)(i)", null,
                Workarounds.kSetPort_workarounds),
            SciKernelMapEntry.Make(kSetQuitStr, SciVersionRange.SIG_EVERYWHERE, "r", null, null),
            SciKernelMapEntry.Make(kSetSynonyms, SciVersionRange.SIG_EVERYWHERE, "o", null, null),
            SciKernelMapEntry.Make(kSetVideoMode, SciVersionRange.SIG_EVERYWHERE, "i", null, null),
            SciKernelMapEntry.Make(kShakeScreen, SciVersionRange.SIG_EVERYWHERE, "(i)(i)", null, null),
            SciKernelMapEntry.Make(kShowMovie, SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.Make(kShow, SciVersionRange.SIG_EVERYWHERE, "i", null, null),
            SciKernelMapEntry.Make(kSinDiv, SciVersionRange.SIG_EVERYWHERE, "ii", null, null),
            SciKernelMapEntry.Make(kSort, SciVersionRange.SIG_EVERYWHERE, "ooo", null, null),
            SciKernelMapEntry.Make(kSqrt, SciVersionRange.SIG_EVERYWHERE, "i", null, null),
            SciKernelMapEntry.Make(kStrAt, SciVersionRange.SIG_EVERYWHERE, "ri(i)", null, Workarounds.kStrAt_workarounds),
            SciKernelMapEntry.Make(kStrCat, SciVersionRange.SIG_EVERYWHERE, "rr", null, null),
            SciKernelMapEntry.Make(kStrCmp, SciVersionRange.SIG_EVERYWHERE, "rr(i)", null, null),
            SciKernelMapEntry.Make(kStrCpy, SciVersionRange.SIG_EVERYWHERE, "r[r0](i)", null,
                Workarounds.kStrCpy_workarounds),
            SciKernelMapEntry.Make(kStrEnd, SciVersionRange.SIG_EVERYWHERE, "r", null, null),
            SciKernelMapEntry.Make(kStrLen, SciVersionRange.SIG_EVERYWHERE, "[r0]", null,
                Workarounds.kStrLen_workarounds),
            SciKernelMapEntry.Make(kStrSplit, SciVersionRange.SIG_EVERYWHERE, "rr[r0]", null, null),
            SciKernelMapEntry.Make(kTextColors, SciVersionRange.SIG_EVERYWHERE, "(i*)", null, null),
            SciKernelMapEntry.Make(kTextFonts, SciVersionRange.SIG_EVERYWHERE, "(i*)", null, null),
            SciKernelMapEntry.Make(kTextSize, SciVersionRange.SIG_SCIALL(SIGFOR_MAC), "r[r0]i(i)(r0)(i)", null, null),
            SciKernelMapEntry.Make(kTextSize, SciVersionRange.SIG_EVERYWHERE, "r[r0]i(i)(r0)", null, null),
            SciKernelMapEntry.Make(kTimesCos, SciVersionRange.SIG_EVERYWHERE, "ii", null, null),
            SciKernelMapEntry.Make("CosMult", kTimesCos, SciVersionRange.SIG_EVERYWHERE, "ii", null, null),
            SciKernelMapEntry.Make(kTimesCot, SciVersionRange.SIG_EVERYWHERE, "ii", null, null),
            SciKernelMapEntry.Make(kTimesSin, SciVersionRange.SIG_EVERYWHERE, "ii", null, null),
            SciKernelMapEntry.Make("SinMult", kTimesSin, SciVersionRange.SIG_EVERYWHERE, "ii", null, null),
            SciKernelMapEntry.Make(kTimesTan, SciVersionRange.SIG_EVERYWHERE, "ii", null, null),
            SciKernelMapEntry.Make(kUnLoad, SciVersionRange.SIG_EVERYWHERE, "i[ir!]", null,
                Workarounds.kUnLoad_workarounds),
            // ^ We allow invalid references here (e.g. bug #6600), since they will be invalidated anyway by the call itself
            SciKernelMapEntry.Make(kValidPath, SciVersionRange.SIG_EVERYWHERE, "r", null, null),
            SciKernelMapEntry.Make(kWait, SciVersionRange.SIG_EVERYWHERE, "i", null, null),
            // Unimplemented SCI0-SCI1.1 unused functions, always mapped to kDummy
            SciKernelMapEntry.MakeDummy("InspectObj", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.MakeDummy("ShowSends", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.MakeDummy("ShowObjs", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.MakeDummy("ShowFree", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.MakeDummy("StackUsage", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.MakeDummy("Profiler", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.MakeDummy("ShiftScreen", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.MakeDummy("ListOps", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            // Used by the sysLogger class (e.g. script 952 in GK1CD), a class used to report bugs by Sierra's testers
            SciKernelMapEntry.MakeDummy("ATan", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.MakeDummy("Record", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.MakeDummy("PlayBack", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            SciKernelMapEntry.MakeDummy("DbugStr", SciVersionRange.SIG_EVERYWHERE, "(.*)", null, null),
            new SciKernelMapEntry()
        };

        private static readonly ClassReference[] classReferences =
        {
            new ClassReference(0, "Character", "say", SelectorType.Method, 5), // Crazy Nick's Soft Picks
            new ClassReference(928, "Narrator", "say", SelectorType.Method, 4),
            new ClassReference(928, "Narrator", "startText", SelectorType.Method, 5),
            new ClassReference(929, "Sync", "syncTime", SelectorType.Variable, 1),
            new ClassReference(929, "Sync", "syncCue", SelectorType.Variable, 2),
            new ClassReference(981, "SysWindow", "open", SelectorType.Method, 1),
            new ClassReference(999, "Script", "init", SelectorType.Method, 0),
            new ClassReference(999, "Script", "dispose", SelectorType.Method, 2),
            new ClassReference(999, "Script", "changeState", SelectorType.Method, 3)
        };

        public KernelFunction[] _kernelFuncs;

        public int SelectorNamesSize
        {
            get { return _selectorNames.Count; }
        }

        public int KernelNamesSize
        {
            get { return _kernelNames.Count; }
        }

        public bool SelectorNamesAvailable
        {
            get { return _selectorNames.Count > 0; }
        }

        public Kernel(ResourceManager resMan, SegManager segMan)
        {
            _resMan = resMan;
            _segMan = segMan;
            _invalid = "<invalid>";
        }

        public void Init()
        {
            LoadSelectorNames();
            MapSelectors(); // Map a few special selectors for later use
        }

        private void MapSelectors()
        {
            // species
            // superClass
            FindSelector(o => o._info_);
            FindSelector(o => o.y);
            FindSelector(o => o.x);
            FindSelector(o => o.view);
            FindSelector(o => o.loop);
            FindSelector(o => o.cel);
            FindSelector(o => o.underBits);
            FindSelector(o => o.nsTop);
            FindSelector(o => o.nsLeft);
            FindSelector(o => o.nsBottom);
            FindSelector(o => o.lsTop);
            FindSelector(o => o.lsLeft);
            FindSelector(o => o.lsBottom);
            FindSelector(o => o.lsRight);
            FindSelector(o => o.nsRight);
            FindSelector(o => o.signal);
            FindSelector(o => o.illegalBits);
            FindSelector(o => o.brTop);
            FindSelector(o => o.brLeft);
            FindSelector(o => o.brBottom);
            FindSelector(o => o.brRight);

            // name
            // key
            // time
            FindSelector(o => o.text);
            FindSelector(o => o.elements);
            // color
            // back
            FindSelector(o => o.mode);
            // style
            FindSelector(o => o.state);
            FindSelector(o => o.font);
            FindSelector(o => o.type);
            // window
            FindSelector(o => o.cursor);
            FindSelector(o => o.max);
            FindSelector(o => o.mark);
            FindSelector(o => o.sort);
            // who
            FindSelector(o => o.message);
            // edit
            FindSelector(o => o.play);
            FindSelector(o => o.number);
            FindSelector(o => o.handle); // nodePtr
            FindSelector(o => o.client);
            FindSelector(o => o.dx);
            FindSelector(o => o.dy);
            FindSelector(o => o.b_movCnt);
            FindSelector(o => o.b_i1);
            FindSelector(o => o.b_i2);
            FindSelector(o => o.b_di);
            FindSelector(o => o.b_xAxis);
            FindSelector(o => o.b_incr);
            FindSelector(o => o.xStep);
            FindSelector(o => o.yStep);
            FindSelector(o => o.xLast);
            FindSelector(o => o.yLast);
            FindSelector(o => o.moveSpeed);
            FindSelector(o => o.canBeHere); // cantBeHere
            FindSelector(o => o.heading);
            FindSelector(o => o.mover);
            FindSelector(o => o.doit);
            FindSelector(o => o.isBlocked);
            FindSelector(o => o.looper);
            FindSelector(o => o.priority);
            FindSelector(o => o.modifiers);
            FindSelector(o => o.replay);
            // setPri
            // at
            // next
            // done
            // width
            FindSelector(o => o.wordFail);
            FindSelector(o => o.syntaxFail);
            // semanticFail
            // pragmaFail
            // said
            FindSelector(o => o.claimed);
            // value
            // save
            // restore
            // title
            // button
            // icon
            // draw
            FindSelector(o => o.delete);
            FindSelector(o => o.z);
            // -----------------------------
            FindSelector(o => o.size);
            FindSelector(o => o.moveDone);
            FindSelector(o => o.vol);
            FindSelector(o => o.pri);
            FindSelector(o => o.min);
            FindSelector(o => o.sec);
            FindSelector(o => o.frame);
            FindSelector(o => o.dataInc);
            FindSelector(o => o.palette);
            FindSelector(o => o.cantBeHere);
            FindSelector(o => o.nodePtr);
            FindSelector(o => o.flags);
            FindSelector(o => o.points);
            FindSelector(o => o.syncCue);
            FindSelector(o => o.syncTime);
            FindSelector(o => o.printLang);
            FindSelector(o => o.subtitleLang);
            FindSelector(o => o.parseLang);
            FindSelector(o => o.overlay);
            FindSelector(o => o.topString);
            FindSelector(o => o.scaleSignal);
            FindSelector(o => o.scaleX);
            FindSelector(o => o.scaleY);
            FindSelector(o => o.maxScale);
            FindSelector(o => o.vanishingX);
            FindSelector(o => o.vanishingY);
            FindSelector(o => o.iconIndex);
            FindSelector(o => o.select);

#if ENABLE_SCI32
            FindSelector(o => o.data);
            FindSelector(o => o.picture);
            FindSelector(o => o.bitmap);
            FindSelector(o => o.plane);
            FindSelector(o => o.top);
            FindSelector(o => o.left);
            FindSelector(o => o.bottom);
            FindSelector(o => o.right);
            FindSelector(o => o.resY);
            FindSelector(o => o.resX);
            FindSelector(o => o.dimmed);
            FindSelector(o => o.fore);
            FindSelector(o => o.back);
            FindSelector(o => o.skip);
            FindSelector(o => o.fixPriority);
            FindSelector(o => o.mirrored);
            FindSelector(o => o.visible);
            FindSelector(o => o.useInsetRect);
            FindSelector(o => o.inTop);
            FindSelector(o => o.inLeft);
            FindSelector(o => o.inBottom);
            FindSelector(o => o.inRight);
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
                    _kernelNames.AddRange(sci2_default_knames.Take(kKernelEntriesSci2));
                    break;

                case SciVersion.V2_1_EARLY:
                case SciVersion.V2_1_MIDDLE:
                case SciVersion.V2_1_LATE:
                    if (features.DetectSci21KernelType() == SciVersion.V2)
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

                        _kernelNames.AddRange(sci2_default_knames.Take(kKernelEntriesGk2Demo));
                        // OnMe is IsOnMe here, but they should be compatible
                        _kernelNames[0x23] = "Robot"; // Graph in SCI2
                        _kernelNames[0x2e] = "Priority"; // DisposeTextBitmap in SCI2
                    }
                    else {
                        // Normal SCI2.1 kernel table
                        _kernelNames.AddRange(sci21_default_knames.Take(kKernelEntriesSci21));
                    }
                    break;

                case SciVersion.V3:
                    _kernelNames.AddRange(sci21_default_knames.Take(kKernelEntriesSci3));

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
                    Warning($"Kernel function {id:X} unknown");
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
                if (SciEngine.Instance.Platform == Platform.Macintosh &&
                    SciEngine.Instance.GameId == SciGameId.PHANTASMAGORIA &&
                    kernelName == "DoSound")
                {
                    _kernelFuncs[id].function = kDoSoundPhantasmagoriaMac;
                    _kernelFuncs[id].signature = ParseKernelSignature("DoSoundPhantasmagoriaMac", "i.*");
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
                        SciVersion mySubVersion = (SciVersion) kernelMap.function(null, 0, StackPtr.Null).Offset;
                        // Now check whats the highest subfunction-id for this version
                        SciKernelMapSubEntry kernelSubMap;
                        ushort subFunctionCount = 0;
                        for (int i = 0; i < kernelMap.subFunctions.Length; i++)
                        {
                            kernelSubMap = kernelMap.subFunctions[i];
                            if ((kernelSubMap.fromVersion == SciVersion.NONE) ||
                                (kernelSubMap.fromVersion <= mySubVersion))
                                if ((kernelSubMap.toVersion == SciVersion.NONE) ||
                                    (kernelSubMap.toVersion >= mySubVersion))
                                    if (subFunctionCount <= kernelSubMap.id)
                                        subFunctionCount = (ushort) (kernelSubMap.id + 1);
                        }
                        if (subFunctionCount == 0)
                            throw new InvalidOperationException(
                                $"k{kernelName}[{id:X}]: no subfunctions found for requested version");
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
                            if ((kernelSubMap.fromVersion == SciVersion.NONE) ||
                                (kernelSubMap.fromVersion <= mySubVersion))
                                if ((kernelSubMap.toVersion == SciVersion.NONE) ||
                                    (kernelSubMap.toVersion >= mySubVersion))
                                {
                                    uint subId = kernelSubMap.id;
                                    if (subFunctions[subId].function == null)
                                    {
                                        subFunctions[subId].function = kernelSubMap.function;
                                        subFunctions[subId].name = kernelSubMap.name;
                                        subFunctions[subId].workarounds = kernelSubMap.workarounds;
                                        if (kernelSubMap.signature != null)
                                        {
                                            subFunctions[subId].signature = ParseKernelSignature(kernelSubMap.name,
                                                kernelSubMap.signature);
                                        }
                                        else
                                        {
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
                                                        subFunctions[subId].signature =
                                                            ParseKernelSignature(kernelSubMap.name,
                                                                kernelSubMapBack.signature);
                                                        break;
                                                    }
                                                }
                                            }
                                            if (subFunctions[subId].signature == null)
                                                throw new InvalidOperationException(
                                                    $"k{kernelSubMap.name}: no previous signatures");
                                        }
                                    }
                                }
                            kernelSubNr++;
                        }
                    }
                    ++mapped;
                }
                else
                {
                    if (nameMatch)
                        throw new InvalidOperationException(
                            $"k{kernelName}[{id:X}]: not found for this version/platform");
                    // No match but a name was given . stub
                    Warning($"k{kernelName}[{id:X}]: unmapped");
                    _kernelFuncs[id].function = kStub;
                }
            } // for all functions requesting to be mapped

            DebugC(DebugLevels.VM, "Handled {0}/{1} kernel functions, mapping {2} and ignoring {3}.",
                mapped + ignored, _kernelNames.Count, mapped, ignored);

            return;
        }

        /// <summary>
        /// this parses a written kernel signature into an internal memory format
        /// [io] . either integer or object
        /// (io) . optionally integer AND an object
        /// (i) . optional integer
        /// . . any type
        /// i* . optional multiple integers
        /// .* . any parameters afterwards (or none)
        /// </summary>
        /// <param name="kernelName"></param>
        /// <param name="writtenSig"></param>
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
            if (writtenSig == null)
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
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: ']' used without leading '['");
                        if (!validType)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: '[]' does not surround valid type(s)");
                        eitherOr = false;
                        validType = false;
                        size++;
                        break;
                    case '(': // optional
                        if (optional)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: '(' used within '()' brackets");
                        if (eitherOr)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: '(' used within '[]' brackets");
                        optional = true;
                        validType = false;
                        optionalType = false;
                        break;
                    case ')': // optional end
                        if (!optional)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: ')' used without leading '('");
                        if (!optionalType)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: '()' does not to surround valid type(s)");
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
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: non-optional type may not follow optional type");
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
                                throw new InvalidOperationException(
                                    $"signature for k{kernelName}: a valid type must be in front of '*'");
                        }
                        if (eitherOr)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: '*' may not be inside '[]'");
                        if (optional)
                        {
                            if ((writtenSig[curPos + 1] != ')') || ((curPos + 2) != writtenSig.Length))
                                throw new InvalidOperationException(
                                    $"signature for k{kernelName}: '*' may only be used for last type");
                        }
                        else
                        {
                            if ((curPos + 1) != writtenSig.Length)
                                throw new InvalidOperationException(
                                    $"signature for k{kernelName}: '*' may only be used for last type");
                        }
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"signature for k{kernelName}: '{writtenSig[curPos]}' unknown");
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
                                    throw new InvalidOperationException(
                                        $"signature for k{kernelName}: invalid ('!') may only get used in combination with a real type");
                                if (((signature & SIG_IS_INVALID) != 0) &&
                                    ((signature & SIG_MAYBE_ANY) == (SIG_TYPE_NULL | SIG_TYPE_INTEGER)))
                                    throw new InvalidOperationException(
                                        $"signature for k{kernelName}: invalid ('!') should not be used on exclusive null/integer type");
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
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: null ('0') specified more than once");
                        signature |= SIG_TYPE_NULL;
                        break;
                    case 'i':
                        if ((signature & SIG_TYPE_INTEGER) != 0)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: integer ('i') specified more than once");
                        signature |= SIG_TYPE_INTEGER | SIG_TYPE_NULL;
                        break;
                    case 'o':
                        if ((signature & SIG_TYPE_OBJECT) != 0)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: object ('o') specified more than once");
                        signature |= SIG_TYPE_OBJECT;
                        break;
                    case 'r':
                        if ((signature & SIG_TYPE_REFERENCE) != 0)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: reference ('r') specified more than once");
                        signature |= SIG_TYPE_REFERENCE;
                        break;
                    case 'l':
                        if ((signature & SIG_TYPE_LIST) != 0)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: list ('l') specified more than once");
                        signature |= SIG_TYPE_LIST;
                        break;
                    case 'n':
                        if ((signature & SIG_TYPE_NODE) != 0)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: node ('n') specified more than once");
                        signature |= SIG_TYPE_NODE;
                        break;
                    case '.':
                        if ((signature & SIG_MAYBE_ANY) != 0)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: maybe-any ('.') shouldn't get specified with other types in front of it");
                        signature |= SIG_MAYBE_ANY;
                        break;
                    case '!':
                        if ((signature & SIG_IS_INVALID) != 0)
                            throw new InvalidOperationException(
                                $"signature for k{kernelName}: invalid ('!') specified more than once");
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
                    throw new InvalidOperationException(
                        $"signature for k{kernelName}: invalid ('!') may only get used in combination with a real type");
                if (((signature & SIG_IS_INVALID) != 0) &&
                    ((signature & SIG_MAYBE_ANY) == (SIG_TYPE_NULL | SIG_TYPE_INTEGER)))
                    throw new InvalidOperationException(
                        $"signature for k{kernelName}: invalid ('!') should not be used on exclusive null/integer type");
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

        private void FindSelector(Expression<Func<SelectorCache, int>> selector)
        {
            var exp = (MemberExpression) selector.Body;
            var selectorName = exp.Member.Name.Replace('_', '-');
            var field = (FieldInfo) exp.Member;
            var param = Expression.Parameter(typeof(SelectorCache));
            var lambdaExp = Expression.Lambda<Action<SelectorCache>>(
                Expression.Assign(
                    Expression.Field(param, field),
                    Expression.Constant(FindSelector(selectorName))),
                param);

            var action = lambdaExp.Compile();
            action(_selectorCache);
        }

        public int FindSelector(string selectorName)
        {
            for (int pos = 0; pos < _selectorNames.Count; ++pos)
            {
                if (_selectorNames[pos] == selectorName)
                    return pos;
            }

            DebugC(DebugLevels.VM, "Could not map '{0}' to any selector", selectorName);

            return -1;
        }

        private void LoadSelectorNames()
        {
            var r = _resMan.FindResource(new ResourceId(ResourceType.Vocab, VOCAB_RESOURCE_SELECTORS), false);
            bool oldScriptHeader = (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY);

            // Starting with KQ7, Mac versions have a BE name table. GK1 Mac and earlier (and all
            // other platforms) always use LE.
            bool isBE = (SciEngine.Instance.Platform == Platform.Macintosh &&
                         ResourceManager.GetSciVersion() >= SciVersion.V2_1_EARLY
                         && SciEngine.Instance.GameId != SciGameId.GK1);

            if (r == null)
            {
                // No such resource?
                // Check if we have a table for this game
                // Some demos do not have a selector table
                var staticSelectorTable = CheckStaticSelectorNames();

                if (staticSelectorTable.Length == 0)
                    throw new InvalidOperationException("Kernel: Could not retrieve selector names");
                else
                {
                    Warning("No selector vocabulary found, using a static one");
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
            int count = (ResourceManager.GetSciVersion() <= SciVersion.V1_1) ? sci0Selectors.Length + offset : sci2Selectors.Length;
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

            foreach (var selectorRemap in Selectors.sciSelectorRemap)
            {
                if (ResourceManager.GetSciVersion() >= selectorRemap.minVersion &&
                    ResourceManager.GetSciVersion() <= selectorRemap.maxVersion)
                {
                    var slot = selectorRemap.slot;
                    if (slot >= names.Length)
                        Array.Resize(ref names, (int) slot + 1);
                    names[slot] = selectorRemap.name;
                }
            }

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
            // Now, we need to find out selectors which keep changing place...
            // We do that by dissecting game objects, and looking for selectors at
            // specified locations.

            // We need to initialize script 0 here, to make sure that it's always
            // located at segment 1.
            _segMan.InstantiateScript(0);
            ushort sci2Offset = (ushort) ((ResourceManager.GetSciVersion() >= SciVersion.V2) ? 64000 : 0);

            // The Actor class contains the init, xLast and yLast selectors, which
            // we reference directly. It's always in script 998, so we need to
            // explicitly load it here.
            if ((ResourceManager.GetSciVersion() >= SciVersion.V1_EGA_ONLY))
            {
                ushort actorScript = 998;

                if (_resMan.TestResource(new ResourceId(ResourceType.Script, (ushort) (actorScript + sci2Offset))) !=
                    null)
                {
                    _segMan.InstantiateScript(actorScript + sci2Offset);

                    var actorClass = _segMan.GetObject(_segMan.FindObjectByName("Actor"));

                    if (actorClass != null)
                    {
                        // Find the xLast and yLast selectors, used in kDoBresen

                        int offset = (ResourceManager.GetSciVersion() < SciVersion.V1_1) ? 3 : 0;
                        int offset2 = (ResourceManager.GetSciVersion() >= SciVersion.V2) ? 12 : 0;
                        // xLast and yLast always come between illegalBits and xStep
                        int illegalBitsSelectorPos = actorClass.LocateVarSelector(_segMan, 15 + offset + offset2);
                            // illegalBits
                        int xStepSelectorPos = actorClass.LocateVarSelector(_segMan, 51 + offset + offset2); // xStep
                        if (xStepSelectorPos - illegalBitsSelectorPos != 3)
                        {
                            throw new InvalidOperationException(
                                $"illegalBits and xStep selectors aren't found in known locations. illegalBits = {illegalBitsSelectorPos}, xStep = {xStepSelectorPos}");
                        }

                        int xLastSelectorPos = actorClass.GetVarSelector((ushort) (illegalBitsSelectorPos + 1));
                        int yLastSelectorPos = actorClass.GetVarSelector((ushort) (illegalBitsSelectorPos + 2));

                        if (selectorNames.Length < (uint) yLastSelectorPos + 1)
                            selectorNames = new string[(int) yLastSelectorPos + 1];

                        selectorNames[xLastSelectorPos] = "xLast";
                        selectorNames[yLastSelectorPos] = "yLast";
                    } // if (actorClass)

                    _segMan.UninstantiateScript(998);
                } // if (_resMan.testResource(ResourceId(kResourceTypeScript, 998)))
            } // if ((ResourceManager.GetSciVersion() >= SCI_VERSION_1_EGA_ONLY))

            // Find selectors from specific classes

            for (int i = 0; i < classReferences.Length; i++)
            {
                if (
                    _resMan.TestResource(new ResourceId(ResourceType.Script,
                        (ushort) (classReferences[i].script + sci2Offset))) == null)
                    continue;

                _segMan.InstantiateScript(classReferences[i].script + sci2Offset);

                var targetClass = _segMan.GetObject(_segMan.FindObjectByName(classReferences[i].className));
                int targetSelectorPos = 0;
                uint selectorOffset = classReferences[i].selectorOffset;

                if (targetClass != null)
                {
                    if (classReferences[i].selectorType == SelectorType.Method)
                    {
                        if (targetClass.MethodCount < selectorOffset + 1)
                            Error("The {0} class has less than {1} methods ({2})",
                                classReferences[i].className, selectorOffset + 1,
                                targetClass.MethodCount);

                        targetSelectorPos = targetClass.GetFuncSelector((int) selectorOffset);
                    }
                    else
                    {
                        // Add the global selectors to the selector ID
                        selectorOffset =
                            (uint) (selectorOffset + ((ResourceManager.GetSciVersion() <= SciVersion.V1_LATE) ? 3 : 8));

                        if (targetClass.VarCount < selectorOffset + 1)
                            Error("The {0} class has less than {1} variables ({2})",
                                classReferences[i].className, selectorOffset + 1,
                                targetClass.VarCount);

                        targetSelectorPos = targetClass.GetVarSelector((ushort) selectorOffset);
                    }

                    if (selectorNames.Length < (uint) targetSelectorPos + 1)
                        selectorNames = new string[((int) targetSelectorPos + 1)];


                    selectorNames[targetSelectorPos] = classReferences[i].selectorName;
                }
            }

            // Reset the segment manager
            _segMan.ResetSegMan();
        }

        private string LookupText(Register address, int index)
        {
            if (address.Segment != 0)
                return _segMan.GetString(address);

            int _index = index;
            var textres = _resMan.FindResource(new ResourceId(ResourceType.Text, (ushort) address.Offset), false);

            if (textres == null)
            {
                throw new InvalidOperationException($"text.{address.Offset} not found");
            }

            var textlen = textres.size;
            var seeker = textres.data;
            var i = 0;

            while ((index--) != 0)
                while (((textlen--) != 0) && (seeker[i++] != 0))
                {
                }

            if (textlen != 0)
                return seeker.GetText(i);

            throw new InvalidOperationException($"Index {_index} out of bounds in text.{address.Offset}");
        }
    }
}