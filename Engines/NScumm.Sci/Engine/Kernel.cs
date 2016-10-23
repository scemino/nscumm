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


        private readonly SegManager _segMan;
        private readonly ResourceManager _resMan;
        private readonly string _invalid;

        // Kernel-related lists
        private readonly List<string> _selectorNames = new List<string>();
        private readonly List<string> _kernelNames = new List<string>();

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
            "plane", "x", "y", "z", "scaleX", //  0 -  4
            "scaleY", "maxScale", "priority", "fixPriority", "inLeft", //  5 -  9
            "inTop", "inRight", "inBottom", "useInsetRect", "view", // 10 - 14
            "loop", "cel", "bitmap", "nsLeft", "nsTop", // 15 - 19
            "nsRight", "nsBottom", "lsLeft", "lsTop", "lsRight", // 20 - 25
            "lsBottom", "signal", "illegalBits", "brLeft", "brTop", // 25 - 29
            "brRight", "brBottom", "name", "key", "time", // 30 - 34
            "text", "elements", "fore", "back", "mode", // 35 - 39
            "style", "state", "font", "type", "window", // 40 - 44
            "cursor", "max", "mark", "who", "message", // 45 - 49
            "edit", "play", "number", "nodePtr", "client", // 50 - 54
            "dx", "dy", "b-moveCnt", "b-i1", "b-i2", // 55 - 59
            "b-di", "b-xAxis", "b-incr", "xStep", "yStep", // 60 - 64
            "moveSpeed", "cantBeHere", "heading", "mover", "doit", // 65 - 69
            "isBlocked", "looper", "modifiers", "replay", "setPri", // 70 - 74
            "at", "next", "done", "width", "pragmaFail", // 75 - 79
            "claimed", "value", "save", "restore", "title", // 80 - 84
            "button", "icon", "draw", "delete", "printLang", // 85 - 89
            "size", "points", "palette", "dataInc", "handle", // 90 - 94
            "min", "sec", "frame", "vol", "perform", // 95 - 99
            "moveDone", "topString", "flags", "quitGame", "restart", // 100 - 104
            "hide", "scaleSignal", "vanishingX", "vanishingY", "picture", // 105 - 109
            "resX", "resY", "coordType", "data", "skip", // 110 - 104
            "center", "all", "show", "textLeft", "textTop", // 115 - 119
            "textRight", "textBottom", "borderColor", "titleFore", "titleBack", // 120 - 124
            "titleFont", "dimmed", "frameOut", "lastKey", "magnifier", // 125 - 129
            "magPower", "mirrored", "pitch", "roll", "yaw", // 130 - 134
            "left", "right", "top", "bottom", "numLines" // 135 - 139
        };
#endif

        /// <summary>
        /// Default kernel name table.
        /// </summary>
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
        /// <summary>
        /// NOTE: 0x72-0x79, 0x85-0x86, 0x88 are from the GK2 demo (which has debug support) and are
        /// just Dummy in other SCI2 games.
        /// </summary>
        private static readonly string[] sci2_default_knames =
        {
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
            /*0x10*/ "GetHighItemPri", // unused function
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
            /*0x1c*/ "RepaintPlane", // unused function
            /*0x1d*/ "SetShowStyle",
            /*0x1e*/ "ShowStylePercent", // unused function
            /*0x1f*/ "SetScroll",
            /*0x20*/ "AddMagnify",
            /*0x21*/ "DeleteMagnify",
            /*0x22*/ "IsHiRes",
            /*0x23*/ "Graph", // Robot in early SCI2.1 games with a SCI2 kernel table
            /*0x24*/ "InvertRect", // only in SCI2, not used in any SCI2 game
            /*0x25*/ "TextSize",
            /*0x26*/ "Message",
            /*0x27*/ "TextColors",
            /*0x28*/ "TextFonts",
            /*0x29*/ "Dummy",
            /*0x2a*/ "SetQuitStr",
            /*0x2b*/ "EditText",
            /*0x2c*/ "InputText", // unused function
            /*0x2d*/ "CreateTextBitmap",
            /*0x2e*/ "DisposeTextBitmap", // Priority in early SCI2.1 games with a SCI2 kernel table
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
            /*0x73*/ "InspectObject", // for debugging
            /*0x74*/ "MemoryInfo",
            /*0x75*/ "Profiler", // for debugging
            /*0x76*/ "Record", // for debugging
            /*0x77*/ "PlayBack", // for debugging
            /*0x78*/ "MonoOut", // for debugging
            /*0x79*/ "SetFatalStr", // for debugging
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
            /*0x86*/ "CheckIntegrity", // for debugging
            /*0x87*/ "ObjectIntersect",
            /*0x88*/ "MarkMemory", // for debugging
            /*0x89*/ "TextWidth", // for debugging(?), only in SCI2, not used in any SCI2 game
            /*0x8a*/ "PointSize", // for debugging(?), only in SCI2, not used in any SCI2 game

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

        private static readonly string[] sci21_default_knames =
        {
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
            /*0x18*/ "AddMagnify", // dummy in SCI3
            /*0x19*/ "DeleteMagnify", // dummy in SCI3
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
            /*0x2d*/ "GetHighItemPri", // unused function
            /*0x2e*/ "SetShowStyle",
            /*0x2f*/ "ShowStylePercent", // unused function
            /*0x30*/ "SetScroll", // dummy in SCI3
            /*0x31*/ "MovePlaneItems",
            /*0x32*/ "ShakeScreen",
            /*0x33*/ "Dummy",
            /*0x34*/ "Dummy",
            /*0x35*/ "Dummy",
            /*0x36*/ "Dummy",
            /*0x37*/ "IsHiRes",
            /*0x38*/ "SetVideoMode",
            /*0x39*/ "ShowMovie", // dummy in SCI3
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
            /*0x4b*/ "InputText", // unused function
            /*0x4c*/ "ScrollWindow", // Dummy in SCI3
            /*0x4d*/ "Dummy",
            /*0x4e*/ "Dummy",
            /*0x4f*/ "Dummy",
            /*0x50*/ "GetEvent",
            /*0x51*/ "GlobalToLocal",
            /*0x52*/ "LocalToGlobal",
            /*0x53*/ "MapKeyToDir",
            /*0x54*/ "HaveMouse",
            /*0x55*/ "SetCursor",
            /*0x56*/ "VibrateMouse", // Dummy in SCI3
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
            /*0x64*/ "AvoidPath", // dummy in SCI3
            /*0x65*/ "InPolygon",
            /*0x66*/ "MergePoly", // dummy in SCI3
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
            /*0x7f*/ "WinHelp", // Windows only
            /*0x80*/ "Dummy",
            /*0x81*/ "Dummy", // called when changing rooms in most SCI2.1 games (e.g. KQ7, GK2, MUMG deluxe, Phant1)
            /*0x82*/ "Dummy",
            /*0x83*/ "PrintDebug", // debug function, used by Shivers (demo and full)
            /*0x84*/ "Dummy",
            /*0x85*/ "Dummy",
            /*0x86*/ "Dummy",
            /*0x87*/ "Dummy",
            /*0x88*/ "Dummy",
            /*0x89*/ "Dummy",
            /*0x8a*/ "LoadChunk",
            /*0x8b*/ "SetPalStyleRange",
            /*0x8c*/ "AddPicAt",
            /*0x8d*/ "Dummy", // MessageBox in SCI3
            /*0x8e*/ "NewRoom", // debug function
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
            /*0x9b*/ "Dummy", // Minimize in SCI3
            /*0x9c*/ "DeletePic",
            // == SCI3 only ===============
            /*0x9d*/ "Dummy",
            /*0x9e*/ "WebConnect",
            /*0x9f*/ "Dummy",
            /*0xa0*/ "PlayDuck"
        };

#endif

#if ENABLE_SCI32
        // id of kString function, for quick usage in kArray
        // kArray calls kString in case parameters are strings
        ushort _kernelFunc_StringId;

        //    version,         subId, function-mapping,                    signature,              workarounds
        private static readonly SciKernelMapSubEntry[] kString_subops =
        {
            // every single copy of script 64918 in SCI2 through 2.1mid calls StringNew
            // with a second type argument which is unused (new strings are always type
            // 3)
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_THRU_SCI21MID(0), 0, kStringNew, "i(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_THRU_SCI21MID(0), 1, kArrayGetSize, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_THRU_SCI21MID(0), 2, kStringGetChar, "ri"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_THRU_SCI21MID(0), 3, kArraySetElements, "ri(i*)",
                Workarounds.kArraySetElements_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_THRU_SCI21MID(0), 4, kStringFree, "[0r]"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_THRU_SCI21MID(0), 5, kArrayFill, "rii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_THRU_SCI21MID(0), 6, kArrayCopy, "ririi"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 7, kStringCompare, "rr(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_UNTIL_SCI21MID(0), 8, kArrayDuplicate, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_UNTIL_SCI21MID(0), 9, kStringGetData, "[0or]"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_UNTIL_SCI21MID(0), 10, kStringLen, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_UNTIL_SCI21MID(0), 11, kStringFormat, "r(.*)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_UNTIL_SCI21MID(0), 12, kStringFormatAt, "r[ro](.*)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_UNTIL_SCI21MID(0), 13, kStringAtoi, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_UNTIL_SCI21MID(0), 14, kStringTrim, "ri(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_UNTIL_SCI21MID(0), 15, kStringUpper, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_UNTIL_SCI21MID(0), 16, kStringLower, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_UNTIL_SCI21MID(0), 17, kStringTrn, "rrrr"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_UNTIL_SCI21MID(0), 18, kStringTrnExclude, "rrrr"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21LATE(0), 8, kStringLen, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21LATE(0), 9, kStringFormat, "r(.*)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21LATE(0), 10, kStringFormatAt, "rr(.*)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21LATE(0), 11, kStringAtoi, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21LATE(0), 12, kStringTrim, "ri(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21LATE(0), 13, kStringUpper, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21LATE(0), 14, kStringLower, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21LATE(0), 15, kStringTrn, "rrrr"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21LATE(0), 16, kStringTrnExclude, "rrrr"),
        };

        private static readonly SciKernelMapSubEntry[] kRemapColors_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 0, kRemapColorsOff, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 1, kRemapColorsByRange, "iiii(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 2, kRemapColorsByPercent, "ii(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 3, kRemapColorsToGray, "ii(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 4, kRemapColorsToPercentGray, "iii(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 5, kRemapColorsBlockRange, "ii"),
        };

        private static readonly SciKernelMapSubEntry[] kBitmap_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 0, kBitmapCreate, "iiii(i)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 1, kBitmapDestroy, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 2, kBitmapDrawLine, "riiiii(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 3, kBitmapDrawView, "riii(i)(i)(0)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 4, kBitmapDrawText, "rriiiiiiiiiii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 5, kBitmapDrawColor, "riiiii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 6, kBitmapDrawBitmap, "rr(i)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 7, kBitmapInvert, "riiiiii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 8, kBitmapSetDisplace, "rii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 9, kBitmapCreateFromView,
                "iii(i)(i)(i)([r0])"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 10, kBitmapCopyPixels, "rr"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 11, kBitmapClone, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21LATE(0), 12, kBitmapGetInfo, "r(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21LATE(0), 13, kBitmapScale, "r...ii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI3(0), 14, kBitmapCreateFromUnknown, "......"),
            SciKernelMapSubEntry.MakeEmpty("Bitmap", SciVersionRange.SIG_SCI3(0), 15, "(.*)"),
            SciKernelMapSubEntry.MakeEmpty("Bitmap", SciVersionRange.SIG_SCI3(0), 16, "(.*)"),
        };

        private static readonly SciKernelMapSubEntry[] kFont_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 0, kSetFontHeight, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 1, kSetFontRes, "ii"),
        };

        private static readonly SciKernelMapSubEntry[] kScrollWindow_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 0, kScrollWindowCreate, "oi"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 1, kScrollWindowAdd, "iriii(i)",
                Workarounds.kScrollWindowAdd_workarounds),
            SciKernelMapSubEntry.MakeDummy("ScrollWindowClear", SciVersionRange.SIG_SCI32(0), 2, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 3, kScrollWindowPageUp, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 4, kScrollWindowPageDown, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 5, kScrollWindowUpArrow, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 6, kScrollWindowDownArrow, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 7, kScrollWindowHome, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 8, kScrollWindowEnd, "i"),
            SciKernelMapSubEntry.MakeDummy("ScrollWindowResize", SciVersionRange.SIG_SCI32(0), 9, "i."),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 10, kScrollWindowWhere, "ii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 11, kScrollWindowGo, "i.."),
            SciKernelMapSubEntry.MakeDummy("ScrollWindowInsert", SciVersionRange.SIG_SCI32(0), 12, "i....."),
            SciKernelMapSubEntry.MakeDummy("ScrollWindowDelete", SciVersionRange.SIG_SCI32(0), 13, "i."),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 14, kScrollWindowModify, "iiriii(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 15, kScrollWindowHide, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 16, kScrollWindowShow, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 17, kScrollWindowDestroy, "i"),
            // LSL6hires uses kScrollWindowText and kScrollWindowReconstruct to try to save
            // and restore the content of the game's subtitle window, but this feature did not
            // use the normal save/load functionality of the engine and was actually broken
            // (all text formatting was missing on restore). Since there is no real reason to
            // save the subtitle scrollback anyway, we just ignore calls to these two functions.
            SciKernelMapSubEntry.MakeEmpty("ScrollWindowText", SciVersionRange.SIG_SCI32(0), 18, "i"),
            SciKernelMapSubEntry.MakeEmpty("ScrollWindowReconstruct", SciVersionRange.SIG_SCI32(0), 19, "i."),
        };

        private static readonly SciKernelMapSubEntry[] kText_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 0, kTextSize32, "r[r0]i(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 1, kTextWidth, "ri"),
        };

        private static readonly SciKernelMapSubEntry[] kSave_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 0, kSaveGame, "[r0]i[r0](r0)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 1, kRestoreGame, "[r0]i[r0]"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 2, kGetSaveDir, "(r*)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 3, kCheckSaveGame, ".*"),
            // Subop 4 hasn't been encountered yet
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 5, kGetSaveFiles, "rrr"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 6, kMakeSaveCatName, "rr"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 7, kMakeSaveFileName, "rri"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 8, kAutoSave, "[o0]"),
        };

        // There are a lot of subops to PlayVMD, but only a few of them are ever
        // actually used by games
        //    version,         subId, function-mapping,                    signature,              workarounds
        private static readonly SciKernelMapSubEntry[] kPlayVMD_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 0, kPlayVMDOpen, "r(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 1, kPlayVMDInit, "ii(i)(i)(ii)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 6, kPlayVMDClose, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 14, kPlayVMDPlayUntilEvent, "i(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 16, kPlayVMDShowCursor, "i"),
            SciKernelMapSubEntry.MakeDummy("PlayVMDStartBlob", SciVersionRange.SIG_SINCE_SCI21(0), 17, ""),
            SciKernelMapSubEntry.MakeDummy("PlayVMDStopBlobs", SciVersionRange.SIG_SINCE_SCI21(0), 18, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 21, kPlayVMDSetBlackoutArea, "iiii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 23, kPlayVMDRestrictPalette, "ii")
        };

        //    version,         subId, function-mapping,                    signature,              workarounds
        private static readonly SciKernelMapSubEntry[] kList_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 0, kNewList, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 1, kDisposeList, "l"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 2, kNewNode, ".(.)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 3, kFirstNode, "[l0]"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 4, kLastNode, "l"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 5, kEmptyList, "l"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 6, kNextNode, "n"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 7, kPrevNode, "n"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 8, kNodeValue, "[n0]"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 9, kAddAfter, "lnn."),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 10, kAddToFront, "ln."),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 11, kAddToEnd, "ln(.)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 12, kAddBefore, "ln."),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 13, kMoveToFront, "ln"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 14, kMoveToEnd, "ln"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 15, kFindKey, "l."),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 16, kDeleteKey, "l."),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 17, kListAt, "li"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 18, kListIndexOf, "l[io]"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 19, kListEachElementDo, "li(.*)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 20, kListFirstTrue, "li(.*)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 21, kListAllTrue, "li(.*)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 22, kSort, "ooo"),
        };

        //    version,         subId, function-mapping,                    signature,              workarounds
        private static readonly SciKernelMapSubEntry[] kPalCycle_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 0, kPalCycleSetCycle, "iii(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 1, kPalCycleDoCycle, "i(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 2, kPalCyclePause, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 3, kPalCycleOn, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 4, kPalCycleOff, "(i)"),
        };

        // NOTE: In SSCI, some 'unused' kDoAudio subops are actually
        // called indirectly by kDoSound:
        //
        // kDoSoundGetAudioCapability -> kDoAudioGetCapability
        // kDoSoundPlay       -> kDoAudioPlay, kDoAudioStop
        // kDoSoundPause      -> kDoAudioPause, kDoAudioResume
        // kDoSoundFade       -> kDoAudioFade
        // kDoSoundSetVolume  -> kDoAudioVolume
        // kDoSoundSetLoop    -> kDoAudioSetLoop
        // kDoSoundUpdateCues -> kDoAudioPosition
        //
        // In ScummVM, logic inside these kernel functions has been
        // moved to methods of Audio32, and direct calls to Audio32
        // are made from kDoSound instead.
        //
        // Some kDoAudio methods are esoteric and appear to be used
        // only by one or two games:
        //
        // kDoAudioMixing: Phantasmagoria (other games call this
        // function, but only to disable the feature)
        // kDoAudioHasSignal: SQ6 TalkRandCycle
        // kDoAudioPan: Rama RegionSFX::pan method
        //
        // Finally, there is a split in SCI2.1mid audio code.
        // QFG4CD & SQ6 do not have opcodes 18 and 19, but they
        // exist in GK2, KQ7 2.00b, Phantasmagoria 1, PQ:SWAT, and
        // Torin. (It is unknown if they exist in MUMG Deluxe or
        // Shivers 1; they are not used in either of these games.)

        //    version,         subId, function-mapping,                    signature,              workarounds
        private static readonly SciKernelMapSubEntry[] kDoAudio_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 0, kDoAudioInit, ""),
            // SCI2 includes a Sync script that would call
            // kDoAudioWaitForPlay, but SSCI has no opcode 1 until
            // SCI2.1early
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21(0), 1, kDoAudioWaitForPlay,
                "(i)(i)(i)(i)(i)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 2, kDoAudioPlay, "(i)(i)(i)(i)(i)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 3, kDoAudioStop, "(i)(i)(i)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 4, kDoAudioPause, "(i)(i)(i)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 5, kDoAudioResume, "(i)(i)(i)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 6, kDoAudioPosition, "(i)(i)(i)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 7, kDoAudioRate, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 8, kDoAudioVolume, "(i)(i)(i)(i)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 9, kDoAudioGetCapability, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 10, kDoAudioBitDepth, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 11, kDoAudioDistort, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 12, kDoAudioMixing, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 13, kDoAudioChannels, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 14, kDoAudioPreload, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 15, kDoAudioFade, "(iiii)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 16, kDoAudioFade36, "iiiii(iii)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 17, kDoAudioHasSignal, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 18, kDoAudioCritical, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI21MID(0), 19, kDoAudioSetLoop, "iii(o)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI3(0), 20, kDoAudioPan, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI3(0), 21, kDoAudioPanOff, ""),
        };
#endif

        private static readonly SciKernelMapSubEntry[] kDoSound_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 0, kDoSoundInit, "o"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 1, kDoSoundPlay, "o"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 2, kDoSoundRestore, "(o)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 3, kDoSoundDispose, "o"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 4, kDoSoundMute, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 5, kDoSoundStop, "o"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 6, kDoSoundPause, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 7, kDoSoundResumeAfterRestore, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 8, kDoSoundMasterVolume, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 9, kDoSoundUpdate, "o"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 10, kDoSoundFade, "[o0]",
                Workarounds.kDoSoundFade_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 11, kDoSoundGetPolyphony, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI0(0), 12, kDoSoundStopAll, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 0, kDoSoundMasterVolume, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 1, kDoSoundMute, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 2, kDoSoundRestore, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 3, kDoSoundGetPolyphony, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 4, kDoSoundUpdate, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 5, kDoSoundInit, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 6, kDoSoundDispose, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 7, kDoSoundPlay, "oi"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 8, kDoSoundStop, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 9, kDoSoundPause, "[o0]i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 10, kDoSoundFade, "oiiii",
                Workarounds.kDoSoundFade_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 11, kDoSoundUpdateCues, "o"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 12, kDoSoundSendMidi, "oiii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 13, kDoSoundGlobalReverb, "(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 14, kDoSoundSetHold, "oi"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1EARLY(0), 15, kDoSoundDummy, ""),
            //  ^^ Longbow demo
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 0, kDoSoundMasterVolume, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 1, kDoSoundMute, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 2, kDoSoundRestore, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 3, kDoSoundGetPolyphony, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 4, kDoSoundGetAudioCapability, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 5, kDoSoundSuspend, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 6, kDoSoundInit, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 7, kDoSoundDispose, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 8, kDoSoundPlay, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 9, kDoSoundStop, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 10, kDoSoundPause, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 11, kDoSoundFade, "oiiii(i)",
                Workarounds.kDoSoundFade_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 12, kDoSoundSetHold, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 13, kDoSoundDummy, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 14, kDoSoundSetVolume, "oi"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 15, kDoSoundSetPriority, "oi"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 16, kDoSoundSetLoop, "oi"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 17, kDoSoundUpdateCues, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 18, kDoSoundSendMidi, "oiii(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 19, kDoSoundGlobalReverb, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI1LATE(0), 20, kDoSoundUpdate, null),
#if ENABLE_SCI32
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 0, kDoSoundMasterVolume, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 1, kDoSoundMute, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 2, kDoSoundRestore, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 3, kDoSoundGetPolyphony, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 4, kDoSoundGetAudioCapability, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 5, kDoSoundSuspend, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 6, kDoSoundInit, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 7, kDoSoundDispose, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 8, kDoSoundPlay, "o(i)"),
            // ^^ TODO: if this is really the only change between SCI1LATE AND SCI21, we could rename the
            //     SIG_SOUNDSCI1LATE #define to SIG_SINCE_SOUNDSCI1LATE and make it being SCI1LATE+. Although
            //     I guess there are many more changes somewhere
            // TODO: Quest for Glory 4 (SCI2.1) uses the old scheme, we need to detect it accordingly
            //        signature for SCI21 should be "o"
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 9, kDoSoundStop, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 10, kDoSoundPause, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 11, kDoSoundFade, null,
                Workarounds.kDoSoundFade_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 12, kDoSoundSetHold, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 13, kDoSoundDummy, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 14, kDoSoundSetVolume, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 15, kDoSoundSetPriority, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 16, kDoSoundSetLoop, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 17, kDoSoundUpdateCues, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 18, kDoSoundSendMidi, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 19, kDoSoundGlobalReverb, null),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SOUNDSCI21(), 20, kDoSoundUpdate, null),
#endif
        };

        private static readonly SciKernelMapSubEntry[] kFileIO_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 0, kFileIOOpen, "r(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 0, kFileIOOpen, "ri"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 1, kFileIOClose, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 2, kFileIOReadRaw, "iri"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 3, kFileIOWriteRaw, "iri"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 4, kFileIOUnlink, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 5, kFileIOReadString, "rii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 6, kFileIOWriteString, "ir"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 7, kFileIOSeek, "iii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 8, kFileIOFindFirst, "rri"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 9, kFileIOFindNext, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 10, kFileIOExists, "r"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SINCE_SCI11(0), 11, kFileIORename, "rr"),
#if ENABLE_SCI32
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 13, kFileIOReadByte, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 14, kFileIOWriteByte, "ii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 15, kFileIOReadWord, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 16, kFileIOWriteWord, "ii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 17, "FileIOCheckFreeSpace", kCheckFreeSpace, "i(r)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 18, kFileIOChangeDirectory, "r"),
            // for SQ6, when changing the savegame directory in the save/load dialog
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 19, kFileIOIsValidDirectory, "r"),
            // for Torin / Torin demo
#endif
        };


        //    version,         subId, function-mapping,                    signature,              workarounds
        private static readonly SciKernelMapSubEntry[] kGraph_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 1, kStubNull, ""),
            // called by gk1 sci32 right at the start
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 2, kGraphGetColorCount, ""),
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
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 9, kGraphFillBoxBackground, "iiii"),
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
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 14, kGraphAdjustPriority, "ii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI11(0), 15, kGraphSaveUpscaledHiresBox, "iiii"),
            // kq6 hires
        };

        private static readonly SciKernelMapSubEntry[] kPalVary_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 0, kPalVaryInit, "ii(i)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 0, kPalVaryInit, "ii(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 1, kPalVaryReverse, "(i)(i)(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 2, kPalVaryGetCurrentStep, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 3, kPalVaryDeinit, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 4, kPalVaryChangeTarget, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 5, kPalVaryChangeTicks, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI16(0), 6, kPalVaryPauseResume, "i"),
#if ENABLE_SCI32
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 0, kPalVarySetVary, "i(i)(i)(ii)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 1, kPalVarySetPercent, "(i)(i)",
                Workarounds.kPalVarySetPercent_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 2, kPalVaryGetPercent, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 3, kPalVaryOff, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 4, kPalVaryMergeTarget, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 5, kPalVarySetTime, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 6, kPalVaryPauseResume, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 7, kPalVarySetTarget, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 8, kPalVarySetStart, "i"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCI32(0), 9, kPalVaryMergeStart, "i"),
#endif
        };

        private static readonly SciKernelMapSubEntry[] kPalette_subops =
        {
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 1, kPaletteSetFromResource, "i(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 2, kPaletteSetFlag, "iii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 3, kPaletteUnsetFlag, "iii",
                Workarounds.kPaletteUnsetFlag_workarounds),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 4, kPaletteSetIntensity, "iii(i)"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 5, kPaletteFindColor, "iii"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 6, kPaletteAnimate, "i*"),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 7, kPaletteSave, ""),
            SciKernelMapSubEntry.Make(SciVersionRange.SIG_SCIALL(0), 8, kPaletteRestore, "[r0]"),
        };

        private static readonly SciKernelMapEntry[] s_kernelMap =
        {
            SciKernelMapEntry.Make(kAbs, SciVersionRange.SIG_EVERYWHERE, "i", null, Workarounds.kAbs_workarounds),
            SciKernelMapEntry.Make(kAddAfter, SciVersionRange.SIG_EVERYWHERE, "lnn"),
            SciKernelMapEntry.Make(kAddMenu, SciVersionRange.SIG_EVERYWHERE, "rr"),
            SciKernelMapEntry.Make(kAddToEnd, SciVersionRange.SIG_EVERYWHERE, "ln"),
            SciKernelMapEntry.Make(kAddToFront, SciVersionRange.SIG_EVERYWHERE, "ln"),
            SciKernelMapEntry.Make(kAddToPic, SciVersionRange.SIG_EVERYWHERE, "[il](iiiiii)"),
            SciKernelMapEntry.Make(kAnimate, SciVersionRange.SIG_EVERYWHERE, "(l0)(i)"),
            SciKernelMapEntry.Make(kAssertPalette, SciVersionRange.SIG_EVERYWHERE, "i"),
            SciKernelMapEntry.Make(kAvoidPath, SciVersionRange.SIG_EVERYWHERE, "ii(.*)"),
            SciKernelMapEntry.Make(kBaseSetter, SciVersionRange.SIG_SCI16(SIGFOR_ALL), "o"),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("BaseSetter", kBaseSetter32, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "o"),
#endif

            SciKernelMapEntry.Make(kCanBeHere, SciVersionRange.SIG_EVERYWHERE, "o(l)"),
            SciKernelMapEntry.Make(kCantBeHere, SciVersionRange.SIG_SCI16(SIGFOR_ALL), "o(l)"),
#if ENABLE_SCI32
            SciKernelMapEntry.Make(kCantBeHere, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "ol"),
#endif
            SciKernelMapEntry.Make(kCelHigh, SciVersionRange.SIG_SCI16(SIGFOR_ALL), "ii(i)", null,
                Workarounds.kCelHigh_workarounds),
            SciKernelMapEntry.Make(kCelWide, SciVersionRange.SIG_SCI16(SIGFOR_ALL), "ii(i)", null,
                Workarounds.kCelWide_workarounds),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("CelHigh", kCelHigh32, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "iii"),
            SciKernelMapEntry.Make("CelWide", kCelWide32, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "iii", null,
                Workarounds.kCelWide_workarounds),
#endif
            SciKernelMapEntry.Make(kCheckFreeSpace, SciVersionRange.SIG_THRU_SCI21EARLY(SIGFOR_ALL), "r(i)"),
            SciKernelMapEntry.Make(kCheckFreeSpace, SciVersionRange.SIG_SCI11(SIGFOR_ALL), "r(i)"),
            SciKernelMapEntry.Make(kCheckFreeSpace, SciVersionRange.SIG_SCI16(SIGFOR_ALL), "r"),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("CheckSaveGame", kCheckSaveGame32, SciVersionRange.SIG_THRU_SCI21EARLY(SIGFOR_ALL),
                "ri[r0]"),
#endif
            SciKernelMapEntry.Make(kCheckSaveGame, SciVersionRange.SIG_SCI16(SIGFOR_ALL), ".*"),
            SciKernelMapEntry.Make(kClone, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kCoordPri, SciVersionRange.SIG_EVERYWHERE, "i(i)"),
            SciKernelMapEntry.Make(kCosDiv, SciVersionRange.SIG_EVERYWHERE, "ii"),
            SciKernelMapEntry.Make(kDeleteKey, SciVersionRange.SIG_EVERYWHERE, "l.", null,
                Workarounds.kDeleteKey_workarounds),
            SciKernelMapEntry.Make(kDeviceInfo, SciVersionRange.SIG_EVERYWHERE, "i(r)(r)(i)", null,
                Workarounds.kDeviceInfo_workarounds), // subop
            SciKernelMapEntry.Make(kDirLoop, SciVersionRange.SIG_EVERYWHERE, "oi", null,
                Workarounds.kDirLoop_workarounds),
            SciKernelMapEntry.Make(kDisposeClone, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kDisposeList, SciVersionRange.SIG_EVERYWHERE, "l"),
            SciKernelMapEntry.Make(kDisposeScript, SciVersionRange.SIG_EVERYWHERE, "i(i*)", null,
                Workarounds.kDisposeScript_workarounds),
            SciKernelMapEntry.Make(kDisposeWindow, SciVersionRange.SIG_EVERYWHERE, "i(i)"),
            SciKernelMapEntry.Make(kDisplay, SciVersionRange.SIG_EVERYWHERE, "[ir]([ir!]*)", null,
                Workarounds.kDisplay_workarounds),
            // ^ we allow invalid references here, because kDisplay gets called with those in e.g. pq3 during intro
            //    restoreBits() checks and skips invalid handles, so that's fine. Sierra SCI behaved the same
            SciKernelMapEntry.Make(kDoAudio, new SciVersionRange(SciVersion.NONE,SciVersion.V2,SIGFOR_ALL), "i(.*)"), // subop
#if ENABLE_SCI32
            SciKernelMapEntry.Make("DoAudio", kDoAudio32, SciVersionRange.SIG_SINCE_SCI21(SIGFOR_ALL), "(.*)",
                kDoAudio_subops),
#endif
            SciKernelMapEntry.Make(kDoAvoider, SciVersionRange.SIG_EVERYWHERE, "o(i)"),
            SciKernelMapEntry.Make(kDoBresen, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kDoSound, SciVersionRange.SIG_EVERYWHERE, "i(.*)", kDoSound_subops),
            SciKernelMapEntry.Make(kDoSync, SciVersionRange.SIG_EVERYWHERE, "i(.*)"), // subop
            SciKernelMapEntry.Make(kDrawCel, SciVersionRange.SIG_SCI11(SIGFOR_PC), "iiiii(i)(i)([ri])"),
            // reference for kq6 hires
            SciKernelMapEntry.Make(kDrawCel, SciVersionRange.SIG_EVERYWHERE, "iiiii(i)(i)"),
            SciKernelMapEntry.Make(kDrawControl, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kDrawMenuBar, SciVersionRange.SIG_EVERYWHERE, "i"),
            SciKernelMapEntry.Make(kDrawPic, SciVersionRange.SIG_EVERYWHERE, "i(i)(i)(i)"),
            SciKernelMapEntry.Make(kDrawStatus, SciVersionRange.SIG_EVERYWHERE, "[r0](i)(i)"),
            SciKernelMapEntry.Make(kEditControl, SciVersionRange.SIG_EVERYWHERE, "[o0][o0]"),
            SciKernelMapEntry.Make(kEmpty, SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.Make(kEmptyList, SciVersionRange.SIG_EVERYWHERE, "l"),
            SciKernelMapEntry.Make("FClose", kFileIOClose, SciVersionRange.SIG_EVERYWHERE, "i"),
            SciKernelMapEntry.Make("FGets", kFileIOReadString, SciVersionRange.SIG_EVERYWHERE, "rii"),
            SciKernelMapEntry.Make("FOpen", kFileIOOpen, SciVersionRange.SIG_EVERYWHERE, "ri"),
            SciKernelMapEntry.Make("FPuts", kFileIOWriteString, SciVersionRange.SIG_EVERYWHERE, "ir"),
            SciKernelMapEntry.Make(kFileIO, SciVersionRange.SIG_EVERYWHERE, "i(.*)", kFileIO_subops),
            SciKernelMapEntry.Make(kFindKey, SciVersionRange.SIG_EVERYWHERE, "l.", null,
                Workarounds.kFindKey_workarounds),
            SciKernelMapEntry.Make(kFirstNode, SciVersionRange.SIG_EVERYWHERE, "[l0]"),
            SciKernelMapEntry.Make(kFlushResources, SciVersionRange.SIG_EVERYWHERE, "i"),
            SciKernelMapEntry.Make(kFormat, SciVersionRange.SIG_EVERYWHERE, "r[ri](.*)"),
            SciKernelMapEntry.Make(kGameIsRestarting, SciVersionRange.SIG_EVERYWHERE, "(i)"),
            SciKernelMapEntry.Make(kGetAngle, SciVersionRange.SIG_EVERYWHERE, "iiii", null,
                Workarounds.kGetAngle_workarounds),
            SciKernelMapEntry.Make(kGetCWD, SciVersionRange.SIG_EVERYWHERE, "r"),
            SciKernelMapEntry.Make(kGetDistance, SciVersionRange.SIG_EVERYWHERE, "ii(i)(i)(i)(i)"),
            SciKernelMapEntry.Make(kGetEvent, SciVersionRange.SIG_SCIALL(SIGFOR_MAC), "io(i*)"),
            SciKernelMapEntry.Make(kGetEvent, SciVersionRange.SIG_EVERYWHERE, "io"),
            SciKernelMapEntry.Make(kGetFarText, SciVersionRange.SIG_EVERYWHERE, "ii[r0]"),
            SciKernelMapEntry.Make(kGetMenu, SciVersionRange.SIG_EVERYWHERE, "i."),
            SciKernelMapEntry.Make(kGetMessage, SciVersionRange.SIG_EVERYWHERE, "iiir"),
            SciKernelMapEntry.Make(kGetPort, SciVersionRange.SIG_EVERYWHERE, ""),
#if ENABLE_SCI32
            SciKernelMapEntry.Make(kGetSaveDir, SciVersionRange.SIG_THRU_SCI21EARLY(SIGFOR_ALL), "(r)"),
#endif
            SciKernelMapEntry.Make(kGetSaveDir, SciVersionRange.SIG_SCI16(SIGFOR_ALL), ""),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("GetSaveFiles", kGetSaveFiles32, SciVersionRange.SIG_THRU_SCI21EARLY(SIGFOR_ALL),
                "rrr"),
#endif
            SciKernelMapEntry.Make(kGetSaveFiles, SciVersionRange.SIG_EVERYWHERE, "rrr"),
            SciKernelMapEntry.Make(kGetTime, SciVersionRange.SIG_EVERYWHERE, "(i)"),
            SciKernelMapEntry.Make(kGlobalToLocal, SciVersionRange.SIG_EVERYWHERE, "o"),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("GlobalToLocal", kGlobalToLocal32, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "oo"),
#endif
            SciKernelMapEntry.Make(kGraph, SciVersionRange.SIG_SCI16(SIGFOR_ALL), null, kGraph_subops),
            SciKernelMapEntry.MakeEmpty("Graph", SciVersionRange.SIG_SCI32(SIGFOR_ALL), "(.*)"),
            SciKernelMapEntry.Make(kHaveMouse, SciVersionRange.SIG_EVERYWHERE, ""),
            SciKernelMapEntry.Make(kHiliteControl, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kInitBresen, SciVersionRange.SIG_EVERYWHERE, "o(i)"),
            SciKernelMapEntry.Make(kIntersections, SciVersionRange.SIG_EVERYWHERE, "iiiiriiiri"),
            SciKernelMapEntry.Make(kIsItSkip, SciVersionRange.SIG_EVERYWHERE, "iiiii"),
            SciKernelMapEntry.Make(kIsObject, SciVersionRange.SIG_EVERYWHERE, ".", null,
                Workarounds.kIsObject_workarounds),
            SciKernelMapEntry.Make(kJoystick, SciVersionRange.SIG_EVERYWHERE, "i(.*)"), // subop
            SciKernelMapEntry.Make(kLastNode, SciVersionRange.SIG_EVERYWHERE, "l"),
            SciKernelMapEntry.Make(kLoad, SciVersionRange.SIG_EVERYWHERE, "ii(i*)"),
            SciKernelMapEntry.Make(kLocalToGlobal, SciVersionRange.SIG_EVERYWHERE, "o"),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("LocalToGlobal", kLocalToGlobal32, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "oo"),
#endif
            SciKernelMapEntry.Make(kLock, SciVersionRange.SIG_EVERYWHERE, "ii(i)"),
            SciKernelMapEntry.Make(kMapKeyToDir, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kMemory, SciVersionRange.SIG_EVERYWHERE, "i(.*)", null,
                Workarounds.kMemory_workarounds), // subop
            SciKernelMapEntry.Make(kMemoryInfo, SciVersionRange.SIG_EVERYWHERE, "i"),
            SciKernelMapEntry.Make(kMemorySegment, SciVersionRange.SIG_EVERYWHERE, "ir(i)"), // subop
            SciKernelMapEntry.Make(kMenuSelect, SciVersionRange.SIG_EVERYWHERE, "o(i)"),
            SciKernelMapEntry.Make(kMergePoly, SciVersionRange.SIG_EVERYWHERE, "rli"),
            SciKernelMapEntry.Make(kMessage, SciVersionRange.SIG_EVERYWHERE, "i(.*)"), // subop
            SciKernelMapEntry.Make(kMoveCursor, SciVersionRange.SIG_EVERYWHERE, "ii", null,
                Workarounds.kMoveCursor_workarounds),
            SciKernelMapEntry.Make(kNewList, SciVersionRange.SIG_EVERYWHERE, ""),
            SciKernelMapEntry.Make(kNewNode, SciVersionRange.SIG_EVERYWHERE, ".."),
            SciKernelMapEntry.Make(kNewWindow, SciVersionRange.SIG_SCIALL(SIGFOR_MAC), ".*"),
            SciKernelMapEntry.Make(kNewWindow, SciVersionRange.SIG_SCI0(SIGFOR_ALL), "iiii[r0]i(i)(i)(i)"),
            SciKernelMapEntry.Make(kNewWindow, SciVersionRange.SIG_SCI1(SIGFOR_ALL), "iiii[ir]i(i)(i)([ir])(i)(i)(i)(i)"),
            SciKernelMapEntry.Make(kNewWindow, SciVersionRange.SIG_SCI11(SIGFOR_ALL), "iiiiiiii[r0]i(i)(i)(i)", null,
                Workarounds.kNewWindow_workarounds),
            SciKernelMapEntry.Make(kNextNode, SciVersionRange.SIG_EVERYWHERE, "n"),
            SciKernelMapEntry.Make(kNodeValue, SciVersionRange.SIG_EVERYWHERE, "[n0]"),
            SciKernelMapEntry.Make(kNumCels, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kNumLoops, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kOnControl, SciVersionRange.SIG_EVERYWHERE, "ii(i)(i)(i)"),
            SciKernelMapEntry.Make(kPalVary, SciVersionRange.SIG_EVERYWHERE, "i(i*)", kPalVary_subops),
            SciKernelMapEntry.Make(kPalette, SciVersionRange.SIG_EVERYWHERE, "i(.*)", kPalette_subops),
            SciKernelMapEntry.Make(kParse, SciVersionRange.SIG_EVERYWHERE, "ro"),
            SciKernelMapEntry.Make(kPicNotValid, SciVersionRange.SIG_EVERYWHERE, "(i)"),
            SciKernelMapEntry.Make(kPlatform, SciVersionRange.SIG_SCI16(SIGFOR_ALL), "(.*)"),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("Platform", kPlatform32, SciVersionRange.SIG_SCI32(SIGFOR_MAC), "(.*)"),
            SciKernelMapEntry.Make("Platform", kPlatform32, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "(i)"),
#endif
            //SciKernelMapEntry.Make(kPortrait,SciVersionRange.SIG_EVERYWHERE,"i(.*)",null,null), // subop
            SciKernelMapEntry.Make(kPrevNode, SciVersionRange.SIG_EVERYWHERE, "n"),
            SciKernelMapEntry.Make(kPriCoord, SciVersionRange.SIG_EVERYWHERE, "i"),
            SciKernelMapEntry.Make(kRandom, SciVersionRange.SIG_EVERYWHERE, "i(i)(i)"),
            SciKernelMapEntry.Make(kReadNumber, SciVersionRange.SIG_EVERYWHERE, "r", null,
                Workarounds.kReadNumber_workarounds),
            SciKernelMapEntry.Make(kRemapColors, SciVersionRange.SIG_SCI11(SIGFOR_ALL), "i(i)(i)(i)(i)"),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("RemapColors", kRemapColors32, SciVersionRange.SIG_SCI32(SIGFOR_ALL),
                "i(i)(i)(i)(i)(i)", kRemapColors_subops),
#endif
            SciKernelMapEntry.Make(kResCheck, SciVersionRange.SIG_EVERYWHERE, "ii(iiii)"),
            SciKernelMapEntry.Make(kRespondsTo, SciVersionRange.SIG_EVERYWHERE, ".i"),
            SciKernelMapEntry.Make(kRestartGame, SciVersionRange.SIG_EVERYWHERE, ""),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("RestoreGame", kRestoreGame32, SciVersionRange.SIG_THRU_SCI21EARLY(SIGFOR_ALL),
                "[r0]i[r0]"),
#endif
            SciKernelMapEntry.Make(kRestoreGame, SciVersionRange.SIG_EVERYWHERE, "[r0]i[r0]"),
            SciKernelMapEntry.Make(kSaid, SciVersionRange.SIG_EVERYWHERE, "[r0]"),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("SaveGame", kSaveGame32, SciVersionRange.SIG_THRU_SCI21EARLY(SIGFOR_ALL),
                "[r0]i[r0](r0)"),
#endif
            SciKernelMapEntry.Make(kSaveGame, SciVersionRange.SIG_SCI16(SIGFOR_ALL), "[r0]i[r0](r0)"),
            SciKernelMapEntry.Make(kScriptID, SciVersionRange.SIG_EVERYWHERE, "[io](i)"),
            SciKernelMapEntry.Make(kSetCursor, SciVersionRange.SIG_SCI11(SIGFOR_ALL), "i(i)(i)(i)(iiiiii)"),
            SciKernelMapEntry.Make(kSetCursor, SciVersionRange.SIG_SCI16(SIGFOR_ALL), "i(i)(i)(i)(i)", null,
                Workarounds.kSetCursor_workarounds),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("SetCursor", kSetCursor32, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "i(i)(i)(i)", null,
                Workarounds.kSetCursor_workarounds),
#endif
            SciKernelMapEntry.Make(kSetDebug, SciVersionRange.SIG_EVERYWHERE, "(i*)"),
            SciKernelMapEntry.Make(kSetJump, SciVersionRange.SIG_EVERYWHERE, "oiii"),
            SciKernelMapEntry.Make(kSetMenu, SciVersionRange.SIG_EVERYWHERE, "i(.*)"),
            SciKernelMapEntry.Make(kSetNowSeen, SciVersionRange.SIG_SCI16(SIGFOR_ALL), "o(i)"),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("SetNowSeen", kSetNowSeen32, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "o"),
#endif

            SciKernelMapEntry.Make(kSetPort, SciVersionRange.SIG_EVERYWHERE, "i(iiiii)(i)", null,
                Workarounds.kSetPort_workarounds),
            SciKernelMapEntry.Make(kSetQuitStr, SciVersionRange.SIG_EVERYWHERE, "r"),
            SciKernelMapEntry.Make(kSetSynonyms, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kSetVideoMode, SciVersionRange.SIG_EVERYWHERE, "i"),
            SciKernelMapEntry.Make(kShakeScreen, SciVersionRange.SIG_EVERYWHERE, "(i)(i)"),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("ShakeScreen", kShakeScreen32, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "i(i)"),
#endif
            SciKernelMapEntry.Make(kShowMovie, SciVersionRange.SIG_SCI16(SIGFOR_ALL), "(.*)"),
#if ENABLE_SCI32
            SciKernelMapEntry.Make("ShowMovie", kShowMovie32, SciVersionRange.SIG_SCI32(SIGFOR_DOS), "ri(i)(i)"),
            SciKernelMapEntry.Make("ShowMovie", kShowMovie32, SciVersionRange.SIG_SCI32(SIGFOR_MAC), "ri(i)(i)"),
            SciKernelMapEntry.Make("ShowMovie", kShowMovieWin, SciVersionRange.SIG_SCI32(SIGFOR_WIN), "(.*)"),
#endif
            SciKernelMapEntry.Make(kShow, SciVersionRange.SIG_EVERYWHERE, "i"),
            SciKernelMapEntry.Make(kSinDiv, SciVersionRange.SIG_EVERYWHERE, "ii"),
            SciKernelMapEntry.Make(kSort, SciVersionRange.SIG_EVERYWHERE, "ooo"),
            SciKernelMapEntry.Make(kSqrt, SciVersionRange.SIG_EVERYWHERE, "i"),
            SciKernelMapEntry.Make(kStrAt, SciVersionRange.SIG_EVERYWHERE, "ri(i)", null, Workarounds.kStrAt_workarounds),
            SciKernelMapEntry.Make(kStrCat, SciVersionRange.SIG_EVERYWHERE, "rr"),
            SciKernelMapEntry.Make(kStrCmp, SciVersionRange.SIG_EVERYWHERE, "rr(i)"),
            SciKernelMapEntry.Make(kStrCpy, SciVersionRange.SIG_EVERYWHERE, "r[r0](i)", null,
                Workarounds.kStrCpy_workarounds),
            SciKernelMapEntry.Make(kStrEnd, SciVersionRange.SIG_EVERYWHERE, "r"),
            SciKernelMapEntry.Make(kStrLen, SciVersionRange.SIG_EVERYWHERE, "[r0]", null,
                Workarounds.kStrLen_workarounds),
            SciKernelMapEntry.Make(kStrSplit, SciVersionRange.SIG_EVERYWHERE, "rr[r0]"),
            SciKernelMapEntry.Make(kTextColors, SciVersionRange.SIG_EVERYWHERE, "(i*)"),
            SciKernelMapEntry.Make(kTextFonts, SciVersionRange.SIG_EVERYWHERE, "(i*)"),
            SciKernelMapEntry.Make(kTextSize, SciVersionRange.SIG_SCIALL(SIGFOR_MAC), "r[r0]i(i)(r0)(i)"),
            SciKernelMapEntry.Make(kTextSize, SciVersionRange.SIG_EVERYWHERE, "r[r0]i(i)(r0)"),
            SciKernelMapEntry.Make(kTimesCos, SciVersionRange.SIG_EVERYWHERE, "ii"),
            SciKernelMapEntry.Make("CosMult", kTimesCos, SciVersionRange.SIG_EVERYWHERE, "ii"),
            SciKernelMapEntry.Make(kTimesCot, SciVersionRange.SIG_EVERYWHERE, "ii"),
            SciKernelMapEntry.Make(kTimesSin, SciVersionRange.SIG_EVERYWHERE, "ii"),
            SciKernelMapEntry.Make("SinMult", kTimesSin, SciVersionRange.SIG_EVERYWHERE, "ii"),
            SciKernelMapEntry.Make(kTimesTan, SciVersionRange.SIG_EVERYWHERE, "ii"),
            SciKernelMapEntry.Make(kUnLoad, SciVersionRange.SIG_EVERYWHERE, "i[ir!]", null,
                Workarounds.kUnLoad_workarounds),
            // ^ We allow invalid references here (e.g. bug #6600), since they will be invalidated anyway by the call itself
            SciKernelMapEntry.Make(kValidPath, SciVersionRange.SIG_EVERYWHERE, "r"),
            SciKernelMapEntry.Make(kWait, SciVersionRange.SIG_EVERYWHERE, "i"),
            // Unimplemented SCI0-SCI1.1 unused functions, always mapped to kDummy
            SciKernelMapEntry.MakeDummy("InspectObj", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("ShowSends", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("ShowObjs", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("ShowFree", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("StackUsage", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("Profiler", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("ShiftScreen", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("ListOps", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            // Used by the sysLogger class (e.g. script 952 in GK1CD), a class used to report bugs by Sierra's testers
            SciKernelMapEntry.MakeDummy("ATan", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("Record", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("PlayBack", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("DbugStr", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
#if ENABLE_SCI32
            // SCI2 Kernel Functions
            // TODO: whoever knows his way through those calls, fix the signatures.
            SciKernelMapEntry.Make("TextSize", kTextSize32, SciVersionRange.SIG_UNTIL_SCI21EARLY(SIGFOR_ALL),
                "r[r0]i(i)"),
            SciKernelMapEntry.Make("TextColors", kDummy, SciVersionRange.SIG_UNTIL_SCI21EARLY(SIGFOR_ALL), "(.*)"),
            SciKernelMapEntry.Make("TextFonts", kDummy, SciVersionRange.SIG_UNTIL_SCI21EARLY(SIGFOR_ALL), "(.*)"),
            SciKernelMapEntry.Make(kAddPlane, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kAddScreenItem, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kArray, SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.Make(kCreateTextBitmap, SciVersionRange.SIG_EVERYWHERE, "i(.*)"),
            SciKernelMapEntry.Make(kDeletePlane, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kDeleteScreenItem, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make("DisposeTextBitmap", kBitmapDestroy, SciVersionRange.SIG_SCI2(SIGFOR_ALL), "r"),
            SciKernelMapEntry.Make(kFrameOut, SciVersionRange.SIG_EVERYWHERE, "(i)"),
            SciKernelMapEntry.Make(kGetHighPlanePri, SciVersionRange.SIG_EVERYWHERE, ""),
            SciKernelMapEntry.Make(kInPolygon, SciVersionRange.SIG_EVERYWHERE, "iio"),
            SciKernelMapEntry.Make(kIsHiRes, SciVersionRange.SIG_EVERYWHERE, ""),
            SciKernelMapEntry.Make(kListAllTrue, SciVersionRange.SIG_EVERYWHERE, "li(.*)"),
            SciKernelMapEntry.Make(kListAt, SciVersionRange.SIG_EVERYWHERE, "li"),
            SciKernelMapEntry.Make(kListEachElementDo, SciVersionRange.SIG_EVERYWHERE, "li(.*)"),
            SciKernelMapEntry.Make(kListFirstTrue, SciVersionRange.SIG_EVERYWHERE, "li(.*)"),
            SciKernelMapEntry.Make(kListIndexOf, SciVersionRange.SIG_EVERYWHERE, "l[o0]"),
            // kMessageBox is used only by KQ7 1.51
            SciKernelMapEntry.Make(kMessageBox, SciVersionRange.SIG_SCI32(SIGFOR_ALL), "rri"),
            SciKernelMapEntry.Make("OnMe", kIsOnMe, SciVersionRange.SIG_EVERYWHERE, "iioi"),
            // Purge is used by the memory manager in SSCI to ensure that X number of bytes (the so called "unmovable
            // memory") are available when the current room changes. This is similar to the SCI0-SCI1.1 FlushResources
            // call, with the added functionality of ensuring that a specific amount of memory is available. We have
            // our own memory manager and garbage collector, thus we simply call FlushResources, which in turn invokes
            // our garbage collector (i.e. the SCI0-SCI1.1 semantics).
            SciKernelMapEntry.Make("Purge", kFlushResources, SciVersionRange.SIG_EVERYWHERE, "i"),
            SciKernelMapEntry.Make(kSetShowStyle, SciVersionRange.SIG_EVERYWHERE, "ioiiiii([ri])(i)"),
            SciKernelMapEntry.Make(kString, SciVersionRange.SIG_EVERYWHERE, "(.*)", kString_subops),
            SciKernelMapEntry.Make(kUpdatePlane, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kUpdateScreenItem, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kObjectIntersect, SciVersionRange.SIG_EVERYWHERE, "oo"),
            SciKernelMapEntry.Make(kEditText, SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.Make(kMakeSaveCatName, SciVersionRange.SIG_EVERYWHERE, "rr"),
            SciKernelMapEntry.Make(kMakeSaveFileName, SciVersionRange.SIG_EVERYWHERE, "rri"),
            SciKernelMapEntry.Make(kSetScroll, SciVersionRange.SIG_EVERYWHERE, "oiiii(i)(i)"),
            SciKernelMapEntry.Make(kPalCycle, SciVersionRange.SIG_EVERYWHERE, "(.*)", kPalCycle_subops),
            // SCI2 Empty functions

            // Debug function used to track resources
            SciKernelMapEntry.MakeEmpty("ResourceTrack", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            // Future TODO: This call is used in the floppy version of QFG4 to add
            // vibration to exotic mice with force feedback, such as the Logitech
            // Cyberman and Wingman mice. Since this is only used for very exotic
            // hardware and we have no direct and cross-platform way of communicating
            // with them via SDL, plus we would probably need to make changes to common
            // code, this call is mapped to an empty function for now as it's a rare
            // feature not worth the effort.
            SciKernelMapEntry.MakeEmpty("VibrateMouse", SciVersionRange.SIG_EVERYWHERE, "(.*)"),

            // Unused / debug SCI2 unused functions, always mapped to kDummy

            // AddMagnify/DeleteMagnify are both called by script 64979 (the Magnifier
            // object) in GK1 only. There is also an associated empty magnifier view
            // (view 1), however, it doesn't seem to be used anywhere, as all the
            // magnifier closeups (e.g. in scene 470) are normal views. Thus, these
            // are marked as dummy, so if they're ever used the engine will error out.
            SciKernelMapEntry.MakeDummy("AddMagnify", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("DeleteMagnify", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("RepaintPlane", SciVersionRange.SIG_EVERYWHERE, "o"),
            SciKernelMapEntry.MakeDummy("InspectObject", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            // Profiler (same as SCI0-SCI1.1)
            // Record (same as SCI0-SCI1.1)
            // PlayBack (same as SCI0-SCI1.1)
            SciKernelMapEntry.MakeDummy("MonoOut", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("SetFatalStr", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("IntegrityChecking", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("CheckIntegrity", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("MarkMemory", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("GetHighItemPri", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("ShowStylePercent", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("InvertRect", SciVersionRange.SIG_UNTIL_SCI21EARLY(SIGFOR_ALL), "(.*)"),
            SciKernelMapEntry.MakeDummy("InputText", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.Make(kTextWidth, SciVersionRange.SIG_UNTIL_SCI21EARLY(SIGFOR_ALL), "ri"),
            SciKernelMapEntry.MakeDummy("PointSize", SciVersionRange.SIG_EVERYWHERE, "(.*)"),

            // SCI2.1 Kernel Functions
            SciKernelMapEntry.Make(kCD, SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.Make(kIsOnMe, SciVersionRange.SIG_EVERYWHERE, "iioi"),
            SciKernelMapEntry.Make(kList, SciVersionRange.SIG_SINCE_SCI21(SIGFOR_ALL), "(.*)", kList_subops),
            SciKernelMapEntry.Make(kMulDiv, SciVersionRange.SIG_EVERYWHERE, "iii"),
            SciKernelMapEntry.Make(kPlayVMD, SciVersionRange.SIG_EVERYWHERE, "(.*)", kPlayVMD_subops),
            SciKernelMapEntry.Make(kRobot, SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.Make(kSave, SciVersionRange.SIG_EVERYWHERE, "i(.*)", kSave_subops),
            SciKernelMapEntry.Make(kText, SciVersionRange.SIG_SINCE_SCI21MID(SIGFOR_ALL), "i(.*)", kText_subops),
            SciKernelMapEntry.Make(kAddPicAt, SciVersionRange.SIG_EVERYWHERE, "oiii"),
            SciKernelMapEntry.Make(kGetWindowsOption, SciVersionRange.SIG_EVERYWHERE, "i"),
            SciKernelMapEntry.Make(kWinHelp, SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.Make(kGetConfig, SciVersionRange.SIG_EVERYWHERE, "ro"),
            SciKernelMapEntry.Make(kGetSierraProfileInt, SciVersionRange.SIG_EVERYWHERE, "rri"),
            SciKernelMapEntry.Make(kCelInfo, SciVersionRange.SIG_SINCE_SCI21MID(SIGFOR_ALL), "iiiiii"),
            SciKernelMapEntry.Make(kSetLanguage, SciVersionRange.SIG_SINCE_SCI21MID(SIGFOR_ALL), "r"),
            SciKernelMapEntry.Make(kScrollWindow, SciVersionRange.SIG_EVERYWHERE, "i(.*)", kScrollWindow_subops),
            SciKernelMapEntry.Make(kSetFontRes, SciVersionRange.SIG_SCI21EARLY(SIGFOR_ALL), "ii"),
            SciKernelMapEntry.Make(kFont, SciVersionRange.SIG_SINCE_SCI21MID(SIGFOR_ALL), "i(.*)", kFont_subops),
            SciKernelMapEntry.Make(kBitmap, SciVersionRange.SIG_EVERYWHERE, "(.*)", kBitmap_subops),
            SciKernelMapEntry.Make(kAddLine, SciVersionRange.SIG_EVERYWHERE, "oiiii(iiiii)"),
            // The first argument is a ScreenItem instance ID that is created by the
            // engine, not the VM; as a result, in ScummVM, this argument looks like
            // an integer and not an object, although it is an object reference.
            SciKernelMapEntry.Make(kUpdateLine, SciVersionRange.SIG_EVERYWHERE, "ioiiii(iiiii)"),
            SciKernelMapEntry.Make(kDeleteLine, SciVersionRange.SIG_EVERYWHERE, "io"),

            // SCI2.1 Empty Functions

            // Debug function, used in of Shivers (demo and full). It's marked as a
            // stub in the original interpreters, but it gets called by the game scripts.
            // Usually, it gets called with a string (which is the output format) and a
            // variable number of parameters
            SciKernelMapEntry.MakeEmpty("PrintDebug", SciVersionRange.SIG_EVERYWHERE, "(.*)"),

            // SetWindowsOption is used to set Windows specific options, like for example the title bar visibility of
            // the game window in Phantasmagoria 2. We ignore these settings completely.
            SciKernelMapEntry.MakeEmpty("SetWindowsOption", SciVersionRange.SIG_EVERYWHERE, "ii"),
            // Debug function called whenever the current room changes
            SciKernelMapEntry.MakeEmpty("NewRoom", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            // Unused / debug SCI2.1 unused functions, always mapped to kDummy

            // The debug functions are called from the inbuilt debugger or polygon
            // editor in SCI2.1 games. Related objects are: PEditor, EditablePolygon,
            // aeDisplayClass and scalerCode
            SciKernelMapEntry.MakeDummy("FindSelector", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("FindClass", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("CelRect", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("BaseLineSpan", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("CelLink", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("AddPolygon", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("DeletePolygon", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("UpdatePolygon", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("Table", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("LoadChunk", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("Priority", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("WinDLL", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("DeletePic", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("GetSierraProfileString", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            // Unused / debug functions in the in-between SCI2.1 interpreters
            SciKernelMapEntry.MakeDummy("PreloadResource", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("CheckCDisc", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("GetSaveCDisc", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            SciKernelMapEntry.MakeDummy("TestPoly", SciVersionRange.SIG_EVERYWHERE, "(.*)"),
            // SCI2.1 unmapped functions - TODO!

            // SetHotRectangles - used by Phantasmagoria 1, script 64981 (used in the chase scene)
            //     <lskovlun> The idea, if I understand correctly, is that the engine generates events
            //     of a special HotRect type continuously when the mouse is on that rectangle
            SciKernelMapEntry.Make(kSetHotRectangles, SciVersionRange.SIG_SINCE_SCI21MID(SIGFOR_ALL), "i(r)"),

            // Used by SQ6 to scroll through the inventory via the up/down buttons
            SciKernelMapEntry.Make(kMovePlaneItems, SciVersionRange.SIG_SINCE_SCI21(SIGFOR_ALL), "oii(i)"),
            SciKernelMapEntry.Make(kSetPalStyleRange, SciVersionRange.SIG_EVERYWHERE, "ii"),
            SciKernelMapEntry.Make(kMorphOn, SciVersionRange.SIG_EVERYWHERE, ""),
            // SCI3 Kernel Functions
            SciKernelMapEntry.Make(kPlayDuck, SciVersionRange.SIG_EVERYWHERE, "(.*)"),
#endif
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
            FindSelector(o => o.borderColor);
            FindSelector(o => o.width);
            FindSelector(o => o.fixPriority);
            FindSelector(o => o.mirrored);
            FindSelector(o => o.visible);
            FindSelector(o => o.useInsetRect);
            FindSelector(o => o.inTop);
            FindSelector(o => o.inLeft);
            FindSelector(o => o.inBottom);
            FindSelector(o => o.inRight);
            FindSelector(o => o.textTop);
            FindSelector(o => o.textLeft);
            FindSelector(o => o.textBottom);
            FindSelector(o => o.textRight);
            FindSelector(o => o.title);
            FindSelector(o => o.titleFont);
            FindSelector(o => o.titleFore);
            FindSelector(o => o.titleBack);
            FindSelector(o => o.magnifier);
            FindSelector(o => o.frameOut);
            FindSelector(o => o.casts);
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
                    else
                    {
                        // Normal SCI2.1 kernel table
                        _kernelNames.AddRange(sci21_default_knames.Take(kKernelEntriesSci21));
                    }
                    break;

                case SciVersion.V3:
                    _kernelNames.AddRange(sci21_default_knames.Take(kKernelEntriesSci3));

                    // In SCI3, some kernel functions have been removed, and others have been added
                    _kernelNames[0x18] = "Dummy"; // AddMagnify in SCI2.1
                    _kernelNames[0x19] = "Dummy"; // DeleteMagnify in SCI2.1
                    _kernelNames[0x30] = "Dummy"; // SetScroll in SCI2.1
                    _kernelNames[0x39] = "Dummy"; // ShowMovie in SCI2.1
                    _kernelNames[0x4c] = "Dummy"; // ScrollWindow in SCI2.1
                    _kernelNames[0x56] = "Dummy"; // VibrateMouse in SCI2.1 (only used in QFG4 floppy)
                    _kernelNames[0x64] = "Dummy"; // AvoidPath in SCI2.1
                    _kernelNames[0x66] = "Dummy"; // MergePoly in SCI2.1
                    _kernelNames[0x8d] = "MessageBox"; // Dummy in SCI2.1
                    _kernelNames[0x9b] = "Minimize"; // Dummy in SCI2.1

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
            var mapped = 0;
            var ignored = 0;
            var functionCount = _kernelNames.Count;
            byte platformMask = 0;
            var myVersion = ResourceManager.GetSciVersion();

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

            for (var id = 0; id < functionCount; id++)
            {
                // First, get the name, if known, of the kernel function with number functnr
                var kernelName = _kernelNames[id];

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
                if (kernelName == "String")
                {
                    _kernelFunc_StringId = (ushort) id;
                }

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
                var nameMatch = false;
                SciKernelMapEntry kernelMap = null;
                for (var i = 0; i < s_kernelMap.Length; i++)
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
                        var mySubVersion = (SciVersion) kernelMap.function(null, 0, StackPtr.Null).Offset;
                        // Now check whats the highest subfunction-id for this version
                        SciKernelMapSubEntry kernelSubMap;
                        ushort subFunctionCount = 0;
                        for (var i = 0; i < kernelMap.subFunctions.Length; i++)
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
                        for (var i = 0; i < subFunctionCount; i++)
                        {
                            subFunctions[i] = new KernelSubFunction();
                        }
                        _kernelFuncs[id].subFunctions = subFunctions;
                        // And fill this info out
                        uint kernelSubNr = 0;
                        for (var i = 0; i < kernelMap.subFunctions.Length; i++)
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
                                            var kernelSubLeft = kernelSubNr;
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
                        Error($"k{kernelName}[{id:X}]: not found for this version/platform");
                    // No match but a name was given . stub
                    Warning($"k{kernelName}[{id:X}]: unmapped");
                    _kernelFuncs[id].function = kStub;
                }
            } // for all functions requesting to be mapped

            DebugC(DebugLevels.VM, "Handled {0}/{1} kernel functions, mapping {2} and ignoring {3}.",
                mapped + ignored, _kernelNames.Count, mapped, ignored);
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
            var size = 0;
            var validType = false;
            var optionalType = false;
            var eitherOr = false;
            var optional = false;
            var hadOptional = false;

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
            for (var pos = 0; pos < _selectorNames.Count; ++pos)
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
            var oldScriptHeader = (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY);

            // Starting with KQ7, Mac versions have a BE name table. GK1 Mac and earlier (and all
            // other platforms) always use LE.
            var isBE = SciEngine.Instance.Platform == Platform.Macintosh &&
                       ResourceManager.GetSciVersion() >= SciVersion.V2_1_EARLY
                       && SciEngine.Instance.GameId != SciGameId.GK1;

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

            var count = (isBE ? r.data.ToUInt16BigEndian() : r.data.ToUInt16()) + 1; // Counter is slightly off

            for (var i = 0; i < count; i++)
            {
                int offset = isBE ? r.data.ToUInt16BigEndian(2 + i * 2) : r.data.ToUInt16(2 + i * 2);
                int len = isBE ? r.data.ToUInt16BigEndian(offset) : r.data.ToUInt16(offset);

                var tmp1 = new byte[len];
                Array.Copy(r.data, offset + 2, tmp1, 0, len);
                var tmp = System.Text.Encoding.UTF8.GetString(tmp1);
                _selectorNames.Add(tmp);
                //debug("%s", tmp.c_str());

                // Early SCI versions used the LSB in the selector ID as a read/write
                // toggle. To compensate for that, we add every selector name twice.
                if (oldScriptHeader)
                    _selectorNames.Add(tmp);
            }
        }

        public bool SignatureMatch(ushort[] signature, int argc, StackPtr argv)
        {
            var sig = 0;
            var nextSig = 0;
            var curSig = nextSig;
            while (nextSig < signature.Length && argc != 0)
            {
                curSig = nextSig;
                var type = FindRegType(argv[0]);

                if ((type & SIG_IS_INVALID) != 0 && (0 == (signature[curSig] & SIG_IS_INVALID)))
                    return false; // pointer is invalid and signature doesn't allow that?

                if (0 == (type & ~SIG_IS_INVALID & signature[curSig]))
                {
                    if ((type & ~SIG_IS_INVALID) == SIG_TYPE_ERROR && (signature[curSig] & SIG_IS_INVALID) != 0)
                    {
                        // Type is unknown (error - usually because of a deallocated object or
                        // stale pointer) and the signature allows invalid pointers. In this case,
                        // ignore the invalid pointer.
                    }
                    else
                    {
                        return false; // type mismatch
                    }
                }

                if (0 == (signature[curSig] & SIG_MORE_MAY_FOLLOW))
                {
                    sig++;
                    nextSig = sig;
                }
                else
                {
                    signature[nextSig] |= SIG_IS_OPTIONAL; // more may follow . assumes followers are optional
                }
                argv++;
                argc--;
            }

            // Too many arguments?
            if (argc != 0)
                return false;
            // Signature end reached?
            if (signature[nextSig] == 0)
                return true;
            // current parameter is optional?
            if ((signature[curSig] & SIG_IS_OPTIONAL) != 0)
            {
                // yes, check if nothing more is required
                if (0 == (signature[curSig] & SIG_NEEDS_MORE))
                    return true;
            }
            else
            {
                // no, check if next parameter is optional
                if ((signature[nextSig] & SIG_IS_OPTIONAL) != 0)
                    return true;
            }
            // Too few arguments or more optional arguments required
            return false;
        }

        private int FindRegType(Register reg)
        {
            // No segment? Must be integer
            if (reg.Segment == 0)
                return SIG_TYPE_INTEGER | (reg.Offset != 0 ? 0 : SIG_TYPE_NULL);

            if (reg.Segment == 0xFFFF)
                return SIG_TYPE_UNINITIALIZED;

            // Otherwise it's an object
            var mobj = _segMan.GetSegmentObj(reg.Segment);
            if (mobj == null)
                return SIG_TYPE_ERROR;

            var result = 0;
            if (!mobj.IsValidOffset((ushort) reg.Offset))
                result |= SIG_IS_INVALID;

            switch (mobj.Type)
            {
                case SegmentType.SCRIPT:
                    if (reg.Offset <= ((Script) mobj).BufSize &&
                        reg.Offset >= (uint) -Script.SCRIPT_OBJECT_MAGIC_OFFSET &&
                        ((Script) mobj).OffsetIsObject((int) reg.Offset))
                    {
                        result |= ((Script) mobj).GetObject((ushort) reg.Offset) != null
                            ? SIG_TYPE_OBJECT
                            : SIG_TYPE_REFERENCE;
                    }
                    else
                        result |= SIG_TYPE_REFERENCE;
                    break;
                case SegmentType.CLONES:
                    result |= SIG_TYPE_OBJECT;
                    break;
                case SegmentType.LOCALS:
                case SegmentType.STACK:
                case SegmentType.DYNMEM:
                case SegmentType.HUNK:
# if ENABLE_SCI32
                case SegmentType.ARRAY:
                case SegmentType.BITMAP:
#endif
                    result |= SIG_TYPE_REFERENCE;
                    break;
                case SegmentType.LISTS:
                    result |= SIG_TYPE_LIST;
                    break;
                case SegmentType.NODES:
                    result |= SIG_TYPE_NODE;
                    break;
                default:
                    return SIG_TYPE_ERROR;
            }
            return result;
        }


        private string[] CheckStaticSelectorNames()
        {
            string[] names;
            var offset = (ResourceManager.GetSciVersion() < SciVersion.V1_1) ? 3 : 0;

#if ENABLE_SCI32
            var count = ResourceManager.GetSciVersion() <= SciVersion.V1_1
                ? sci0Selectors.Length + offset
                : sci2Selectors.Length;
#else
            int count = sci0Selectors.Length + offset;
#endif
            var countSci1 = sci1Selectors.Length;
            var countSci11 = sci11Selectors.Length;

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
                for (var i = offset; i < count; i++)
                    names[i] = sci0Selectors[i - offset];

                if (ResourceManager.GetSciVersion() > SciVersion.V01)
                {
                    // Several new selectors were added in SCI 1 and later.
                    names = new string[count + countSci1];
                    for (var i = count; i < count + countSci1; i++)
                        names[i] = sci1Selectors[i - count];
                }

                if (ResourceManager.GetSciVersion() >= SciVersion.V1_1)
                {
                    // Several new selectors were added in SCI 1.1
                    names = new string[count + countSci1 + countSci11];
                    for (var i = count + countSci1; i < count + countSci1 + countSci11; i++)
                        names[i] = sci11Selectors[i - count - countSci1];
                }
#if ENABLE_SCI32
            }
            else
            {
                // SCI2+
                for (var i = 0; i < count; i++)
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
            var sci2Offset = (ushort) (ResourceManager.GetSciVersion() >= SciVersion.V2 ? 64000 : 0);

            // The Actor class contains the init, xLast and yLast selectors, which
            // we reference directly. It's always in script 998, so we need to
            // explicitly load it here.
            if (ResourceManager.GetSciVersion() >= SciVersion.V1_EGA_ONLY)
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

                        var offset = (ResourceManager.GetSciVersion() < SciVersion.V1_1) ? 3 : 0;
                        var offset2 = (ResourceManager.GetSciVersion() >= SciVersion.V2) ? 12 : 0;
                        // xLast and yLast always come between illegalBits and xStep
                        var illegalBitsSelectorPos = actorClass.LocateVarSelector(_segMan, 15 + offset + offset2);
                        // illegalBits
                        var xStepSelectorPos = actorClass.LocateVarSelector(_segMan, 51 + offset + offset2); // xStep
                        if (xStepSelectorPos - illegalBitsSelectorPos != 3)
                        {
                            throw new InvalidOperationException(
                                $"illegalBits and xStep selectors aren't found in known locations. illegalBits = {illegalBitsSelectorPos}, xStep = {xStepSelectorPos}");
                        }

                        var xLastSelectorPos = actorClass.GetVarSelector((ushort) (illegalBitsSelectorPos + 1));
                        var yLastSelectorPos = actorClass.GetVarSelector((ushort) (illegalBitsSelectorPos + 2));

                        if (selectorNames.Length < (uint) yLastSelectorPos + 1)
                            selectorNames = new string[(int) yLastSelectorPos + 1];

                        selectorNames[xLastSelectorPos] = "xLast";
                        selectorNames[yLastSelectorPos] = "yLast";
                    } // if (actorClass)

                    _segMan.UninstantiateScript(998);
                } // if (_resMan.testResource(ResourceId(kResourceTypeScript, 998)))
            } // if ((ResourceManager.GetSciVersion() >= SCI_VERSION_1_EGA_ONLY))

            // Find selectors from specific classes

            for (var i = 0; i < classReferences.Length; i++)
            {
                if (
                    _resMan.TestResource(new ResourceId(ResourceType.Script,
                        (ushort) (classReferences[i].script + sci2Offset))) == null)
                    continue;

                _segMan.InstantiateScript(classReferences[i].script + sci2Offset);

                var targetClass = _segMan.GetObject(_segMan.FindObjectByName(classReferences[i].className));
                var targetSelectorPos = 0;
                var selectorOffset = classReferences[i].selectorOffset;

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
                        selectorNames = new string[targetSelectorPos + 1];


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

            var _index = index;
            var textres = _resMan.FindResource(new ResourceId(ResourceType.Text, (ushort) address.Offset), false);

            if (textres == null)
            {
                throw new InvalidOperationException($"text.{address.Offset} not found");
            }

            var textlen = textres.size;
            var seeker = textres.data;
            var i = 0;

            while (index-- != 0)
                while ((textlen-- != 0) && (seeker[i++] != 0))
                {
                }

            if (textlen != 0)
                return seeker.GetText(i);

            throw new InvalidOperationException($"Index {_index} out of bounds in text.{address.Offset}");
        }
    }
}