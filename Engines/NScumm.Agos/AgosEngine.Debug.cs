//
//  AGOSEngine.Debug.cs
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
using NScumm.Core;
using NScumm.Core.Graphics;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        private static readonly string[] simon1dos_opcodeNameTable =
        {
            /* 0 */
            "|NOT",
            "IJ|AT",
            "IJ|NOT_AT",
            null,
            /* 4 */
            null,
            "IJ|CARRIED",
            "IJ|NOT_CARRIED",
            "IIJ|IS_AT",
            /* 8 */
            null,
            null,
            null,
            "VJ|IS_ZERO",
            /* 12 */
            "VJ|ISNOT_ZERO",
            "VWJ|IS_EQ",
            "VWJ|IS_NEQ",
            "VWJ|IS_LE",
            /* 16 */
            "VWJ|IS_GE",
            "VVJ|IS_EQF",
            "VVJ|IS_NEQF",
            "VVJ|IS_LEF",
            /* 20 */
            "VVJ|IS_GEF",
            null,
            null,
            "WJ|CHANCE",
            /* 24 */
            null,
            "IJ|IS_ROOM",
            "IJ|IS_OBJECT",
            "IWJ|ITEM_STATE_IS",
            /* 28 */
            "IBJ|OBJECT_HAS_FLAG",
            null,
            null,
            "I|SET_NO_PARENT",
            /* 32 */
            null,
            "II|SET_PARENT",
            null,
            null,
            /* 36 */
            "VV|MOVE",
            null,
            null,
            null,
            /* 40 */
            null,
            "V|ZERO",
            "VW|SET",
            "VW|ADD",
            /* 44 */
            "VW|SUB",
            "VV|ADDF",
            "VV|SUBF",
            "VW|MUL",
            /* 48 */
            "VW|DIV",
            "VV|MULF",
            "VV|DIVF",
            "VW|MOD",
            /* 52 */
            "VV|MODF",
            "VW|RANDOM",
            null,
            "I|SET_A_PARENT",
            /* 56 */
            "IB|SET_CHILD2_BIT",
            "IB|CLEAR_CHILD2_BIT",
            "II|MAKE_SIBLING",
            "I|INC_STATE",
            /* 60 */
            "I|DEC_STATE",
            "IW|SET_STATE",
            "V|SHOW_INT",
            "T|SHOW_STRING_NL",
            /* 64 */
            "T|SHOW_STRING",
            "WWWWWB|ADD_TEXT_BOX",
            "BT|SET_SHORT_TEXT",
            "BT|SET_LONG_TEXT",
            /* 68 */
            "x|END",
            "x|DONE",
            "V|SHOW_STRING_AR3",
            "W|START_SUB",
            /* 72 */
            null,
            null,
            null,
            null,
            /* 76 */
            "WW|ADD_TIMEOUT",
            "J|IS_SUBJECT_ITEM_EMPTY",
            "J|IS_OBJECT_ITEM_EMPTY",
            "ITJ|CHILD_FR2_IS",
            /* 80 */
            "IIJ|IS_ITEM_EQ",
            null,
            "B|DEBUG",
            "|RESCAN",
            /* 84 */
            null,
            null,
            null,
            "W|COMMENT",
            /* 88 */
            "|STOP_ANIMATION",
            "|RESTART_ANIMATION",
            "IB|GET_PARENT",
            "IB|GET_NEXT",
            /* 92 */
            "IB|GET_CHILDREN",
            null,
            null,
            null,
            /* 96 */
            "WB|PICTURE",
            "W|LOAD_ZONE",
            "WBWWW|ANIMATE",
            "W|STOP_ANIMATE",
            /* 100 */
            "|KILL_ANIMATE",
            "BWWWWWW|DEFINE_WINDOW",
            "B|CHANGE_WINDOW",
            "|CLS",
            /* 104 */
            "B|CLOSE_WINDOW",
            null,
            null,
            "WWWWWIW|ADD_BOX",
            /* 108 */
            "W|DEL_BOX",
            "W|ENABLE_BOX",
            "W|DISABLE_BOX",
            "WWW|MOVE_BOX",
            /* 112 */
            null,
            null,
            "IB|DO_ICONS",
            "IBJ|IS_CLASS",
            /* 116 */
            "IB|SET_CLASS",
            "IB|UNSET_CLASS",
            null,
            "W|WAIT_SYNC",
            /* 120 */
            "W|SYNC",
            "BI|DEF_OBJ",
            null,
            null,
            /* 124 */
            null,
            "IJ|IS_SIBLING_WITH_A",
            "IBB|DO_CLASS_ICONS",
            "WW|PLAY_TUNE",
            /* 128 */
            null,
            null,
            "Bww|SET_ADJ_NOUN",
            null,
            /* 132 */
            "|SAVE_USER_GAME",
            "|LOAD_USER_GAME",
            "|STOP_TUNE",
            "|PAUSE",
            /* 136 */
            "IV|COPY_SF",
            "B|RESTORE_ICONS",
            "|FREEZE_ZONES",
            "II|SET_PARENT_SPECIAL",
            /* 140 */
            "|CLEAR_TIMERS",
            "BI|SET_M1_OR_M3",
            "WJ|IS_BOX",
            "I|START_ITEM_SUB",
            /* 144 */
            null,
            null,
            null,
            null,
            /* 148 */
            null,
            null,
            null,
            "BI|STORE_ITEM",
            /* 152 */
            "BB|GET_ITEM",
            "B|SET_BIT",
            "B|CLEAR_BIT",
            "BJ|IS_BIT_CLEAR",
            /* 156 */
            "BJ|IS_BIT_SET",
            "IBB|GET_ITEM_PROP",
            "IBW|SET_ITEM_PROP",
            null,
            /* 160 */
            "B|SET_INK",
            "BWBW|SETUP_TEXT",
            "BBT|PRINT_STR",
            "W|PLAY_EFFECT",
            /* 164 */
            "|getDollar2",
            "IWWJ|IS_ADJ_NOUN",
            "B|SET_BIT2",
            "B|CLEAR_BIT2",
            /* 168 */
            "BJ|IS_BIT2_CLEAR",
            "BJ|IS_BIT2_SET",
            null,
            null,
            /* 172 */
            null,
            null,
            null,
            "|LOCK_ZONES",
            /* 176 */
            "|UNLOCK_ZONES",
            "BBI|SCREEN_TEXT_POBJ",
            "WWBB|GETPATHPOSN",
            "BBB|SCREEN_TEXT_LONG_TEXT",
            /* 180 */
            "|MOUSE_ON",
            "|MOUSE_OFF",
            "|LOAD_BEARD",
            "|UNLOAD_BEARD",
            /* 184 */
            "W|UNLOAD_ZONE",
            "W|LOAD_SOUND_FILES",
            "|UNFREEZE_ZONES",
            "|FADE_TO_BLACK",
        };

        private static readonly string[] simon1talkie_opcodeNameTable =
        {
            /* 0 */
            "|NOT",
            "IJ|AT",
            "IJ|NOT_AT",
            null,
            /* 4 */
            null,
            "IJ|CARRIED",
            "IJ|NOT_CARRIED",
            "IIJ|IS_AT",
            /* 8 */
            null,
            null,
            null,
            "VJ|IS_ZERO",
            /* 12 */
            "VJ|ISNOT_ZERO",
            "VWJ|IS_EQ",
            "VWJ|IS_NEQ",
            "VWJ|IS_LE",
            /* 16 */
            "VWJ|IS_GE",
            "VVJ|IS_EQF",
            "VVJ|IS_NEQF",
            "VVJ|IS_LEF",
            /* 20 */
            "VVJ|IS_GEF",
            null,
            null,
            "WJ|CHANCE",
            /* 24 */
            null,
            "IJ|IS_ROOM",
            "IJ|IS_OBJECT",
            "IWJ|ITEM_STATE_IS",
            /* 28 */
            "IBJ|OBJECT_HAS_FLAG",
            null,
            null,
            "I|SET_NO_PARENT",
            /* 32 */
            null,
            "II|SET_PARENT",
            null,
            null,
            /* 36 */
            "VV|MOVE",
            null,
            null,
            null,
            /* 40 */
            null,
            "V|ZERO",
            "VW|SET",
            "VW|ADD",
            /* 44 */
            "VW|SUB",
            "VV|ADDF",
            "VV|SUBF",
            "VW|MUL",
            /* 48 */
            "VW|DIV",
            "VV|MULF",
            "VV|DIVF",
            "VW|MOD",
            /* 52 */
            "VV|MODF",
            "VW|RANDOM",
            null,
            "I|SET_A_PARENT",
            /* 56 */
            "IB|SET_CHILD2_BIT",
            "IB|CLEAR_CHILD2_BIT",
            "II|MAKE_SIBLING",
            "I|INC_STATE",
            /* 60 */
            "I|DEC_STATE",
            "IW|SET_STATE",
            "V|SHOW_INT",
            "T|SHOW_STRING_NL",
            /* 64 */
            "T|SHOW_STRING",
            "WWWWWB|ADD_TEXT_BOX",
            "BT|SET_SHORT_TEXT",
            "BTw|SET_LONG_TEXT",
            /* 68 */
            "x|END",
            "x|DONE",
            "V|SHOW_STRING_AR3",
            "W|START_SUB",
            /* 72 */
            null,
            null,
            null,
            null,
            /* 76 */
            "WW|ADD_TIMEOUT",
            "J|IS_SUBJECT_ITEM_EMPTY",
            "J|IS_OBJECT_ITEM_EMPTY",
            "ITJ|CHILD_FR2_IS",
            /* 80 */
            "IIJ|IS_ITEM_EQ",
            null,
            "B|DEBUG",
            "|RESCAN",
            /* 84 */
            null,
            null,
            null,
            "W|COMMENT",
            /* 88 */
            "|STOP_ANIMATION",
            "|RESTART_ANIMATION",
            "IB|GET_PARENT",
            "IB|GET_NEXT",
            /* 92 */
            "IB|GET_CHILDREN",
            null,
            null,
            null,
            /* 96 */
            "WB|PICTURE",
            "W|LOAD_ZONE",
            "WBWWW|ANIMATE",
            "W|STOP_ANIMATE",
            /* 100 */
            "|KILL_ANIMATE",
            "BWWWWWW|DEFINE_WINDOW",
            "B|CHANGE_WINDOW",
            "|CLS",
            /* 104 */
            "B|CLOSE_WINDOW",
            null,
            null,
            "WWWWWIW|ADD_BOX",
            /* 108 */
            "W|DEL_BOX",
            "W|ENABLE_BOX",
            "W|DISABLE_BOX",
            "WWW|MOVE_BOX",
            /* 112 */
            null,
            null,
            "IB|DO_ICONS",
            "IBJ|IS_CLASS",
            /* 116 */
            "IB|SET_CLASS",
            "IB|UNSET_CLASS",
            null,
            "W|WAIT_SYNC",
            /* 120 */
            "W|SYNC",
            "BI|DEF_OBJ",
            null,
            null,
            /* 124 */
            null,
            "IJ|IS_SIBLING_WITH_A",
            "IBB|DO_CLASS_ICONS",
            "WW|PLAY_TUNE",
            /* 128 */
            null,
            null,
            "Bww|SET_ADJ_NOUN",
            null,
            /* 132 */
            "|SAVE_USER_GAME",
            "|LOAD_USER_GAME",
            "|STOP_TUNE",
            "|PAUSE",
            /* 136 */
            "IV|COPY_SF",
            "B|RESTORE_ICONS",
            "|FREEZE_ZONES",
            "II|SET_PARENT_SPECIAL",
            /* 140 */
            "|CLEAR_TIMERS",
            "BI|SET_M1_OR_M3",
            "WJ|IS_BOX",
            "I|START_ITEM_SUB",
            /* 144 */
            null,
            null,
            null,
            null,
            /* 148 */
            null,
            null,
            null,
            "BI|STORE_ITEM",
            /* 152 */
            "BB|GET_ITEM",
            "B|SET_BIT",
            "B|CLEAR_BIT",
            "BJ|IS_BIT_CLEAR",
            /* 156 */
            "BJ|IS_BIT_SET",
            "IBB|GET_ITEM_PROP",
            "IBW|SET_ITEM_PROP",
            null,
            /* 160 */
            "B|SET_INK",
            "BWBW|SETUP_TEXT",
            "BBTW|PRINT_STR",
            "W|PLAY_EFFECT",
            /* 164 */
            "|getDollar2",
            "IWWJ|IS_ADJ_NOUN",
            "B|SET_BIT2",
            "B|CLEAR_BIT2",
            /* 168 */
            "BJ|IS_BIT2_CLEAR",
            "BJ|IS_BIT2_SET",
            null,
            null,
            /* 172 */
            null,
            null,
            null,
            "|LOCK_ZONES",
            /* 176 */
            "|UNLOCK_ZONES",
            "BBI|SCREEN_TEXT_POBJ",
            "WWBB|GETPATHPOSN",
            "BBB|SCREEN_TEXT_LONG_TEXT",
            /* 180 */
            "|MOUSE_ON",
            "|MOUSE_OFF",
            "|LOAD_BEARD",
            "|UNLOAD_BEARD",
            /* 184 */
            "W|UNLOAD_ZONE",
            "W|LOAD_SOUND_FILES",
            "|UNFREEZE_ZONES",
            "|FADE_TO_BLACK",
        };

        private static readonly string[] simon1_videoOpcodeNameTable =
        {
            /* 0 */
            "x|RET",
            "ddd|FADEOUT",
            "w|CALL",
            "ddddd|NEW_SPRITE",
            /* 4 */
            "ddd|FADEIN",
            "vdj|IF_EQUAL",
            "dj|IF_OBJECT_HERE",
            "dj|IF_OBJECT_NOT_HERE",
            /* 8 */
            "ddj|IF_OBJECT_IS_AT",
            "ddj|IF_OBJECT_STATE_IS",
            "ddddd|DRAW",
            "|CLEAR_PATHFIND_ARRAY",
            /* 12 */
            "w|DELAY",
            "d|SET_SPRITE_OFFSET_X",
            "d|SET_SPRITE_OFFSET_Y",
            "d|SYNC",
            /* 16 */
            "d|WAIT_SYNC",
            "dq|SET_PATHFIND_ITEM",
            "i|JUMP_REL",
            "|CHAIN_TO",
            /* 20 */
            "dd|SET_REPEAT",
            "i|END_REPEAT",
            "dd|SET_PALETTE",
            "d|SET_PRIORITY",
            /* 24 */
            "wiid|SET_SPRITE_XY",
            "x|HALT_SPRITE",
            "ddddd|SET_WINDOW",
            "|RESET",
            /* 28 */
            "dddd|PLAY_SOUND",
            "|STOP_ALL_SOUNDS",
            "d|SET_FRAME_RATE",
            "d|SET_WINDOW",
            /* 32 */
            "vv|COPY_VAR",
            "|MOUSE_ON",
            "|MOUSE_OFF",
            "dd|CLEAR_WINDOW",
            /* 36 */
            "dd|SET_WINDOW_IMAGE",
            "v|SET_SPRITE_OFFSET_Y",
            "vj|IF_VAR_NOT_ZERO",
            "vd|SET_VAR",
            /* 40 */
            "vd|ADD_VAR",
            "vd|SUB_VAR",
            "vd|DELAY_IF_NOT_EQ",
            "dj|IF_BIT_SET",
            /* 44 */
            "dj|IF_BIT_CLEAR",
            "v|SET_SPRITE_X",
            "v|SET_SPRITE_Y",
            "vv|ADD_VAR_F",
            /* 48 */
            "|COMPUTE_YOFS",
            "d|SET_BIT",
            "d|CLEAR_BIT",
            "d|ENABLE_BOX",
            /* 52 */
            "d|PLAY_EFFECT",
            "dd|DUMMY_53",
            "ddd|DUMMY_54",
            "ddd|MOVE_BOX",
            /* 56 */
            "|DUMMY_56",
            "|BLACK_PALETTE",
            "|DUMMY_58",
            "j|IF_SPEECH",
            /* 60 */
            "d|STOP_ANIMATE",
            "wdd|MASK",
            "|FASTFADEOUT",
            "|FASTFADEIN",
        };

        private static readonly byte[] bmp_hdr =
        {
            0x42, 0x4D,
            0x9E, 0x14, 0x00, 0x00, /* offset 2, file size */
            0x00, 0x00, 0x00, 0x00,
            0x36, 0x04, 0x00, 0x00,
            0x28, 0x00, 0x00, 0x00,
            0x3C, 0x00, 0x00, 0x00, /* image width */
            0x46, 0x00, 0x00, 0x00, /* image height */
            0x01, 0x00, 0x08, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x00, 0x00,
            0x00, 0x01, 0x00, 0x00,
        };

        private BytePtr DumpOpcode(BytePtr p)
        {
            ushort opcode;
            string s = null;

            if (GameType == SIMONGameType.GType_ELVIRA1)
            {
                opcode = p.ToUInt16BigEndian();
                p += 2;
                if (opcode == 10000)
                    return BytePtr.Null;
            }
            else
            {
                opcode = p.Value;
                p.Offset++;
                if (opcode == 255)
                    return BytePtr.Null;
            }

            switch (GameType)
            {
//                case SIMONGameType.GType_PP:
//                    st = s = puzzlepack_opcodeNameTable[opcode];
//                    break;
//                case SIMONGameType.GType_FF:
//                    st = s = feeblefiles_opcodeNameTable[opcode];
//                    break;
                default:
//                    if (GameType == SIMONGameType.GType_SIMON2 && Features.HasFlag(GameFeatures.GF_TALKIE)) {
//                        st = s = simon2talkie_opcodeNameTable[opcode];
//                    } else if (GameType == SIMONGameType.GType_SIMON2) {
//                        st = s = simon2dos_opcodeNameTable[opcode];
//                    } else
                    if (Features.HasFlag(GameFeatures.GF_TALKIE))
                    {
                        s = simon1talkie_opcodeNameTable[opcode];
                    }
                    else if (GameType == SIMONGameType.GType_SIMON1)
                    {
                        s = simon1dos_opcodeNameTable[opcode];
                    }
//                    else if (GameType == SIMONGameType.GType_WW) {
//                        st = s = waxworks_opcodeNameTable[opcode];
//                    } else if (GameType == SIMONGameType.GType_ELVIRA2) {
//                        st = s = elvira2_opcodeNameTable[opcode];
//                    } else {
//                        st = s = elvira1_opcodeNameTable[opcode];
//                    }
                    break;
            }

            if (s == null)
            {
                Error("dumpOpcode: INVALID OPCODE {0}", opcode);
            }

            int st = 0;
            while (s[st] != '|')
                st++;
            DebugN("{0} ", s.Substring(st + 1));

            for (;;)
            {
                switch (s[st++])
                {
                    case 'x':
                        DebugN("\n");
                        return BytePtr.Null;
                    case '|':
                        DebugN("\n");
                        return p;
                    case 'B':
                    {
                        byte b = p.Value;
                        p.Offset++;
                        if (b == 255)
                        {
                            DebugN("[{0}] ", p.Value);
                            p.Offset++;
                        }
                        else
                            DebugN("{0} ", b);
                        break;
                    }
                    case 'V':
                    {
                        byte b = p.Value;
                        p.Offset++;
                        if (b == 255)
                        {
                            DebugN("[[{0}]] ", p.Value);
                            p.Offset++;
                        }
                        else
                            DebugN("[{0}] ", b);
                        break;
                    }

                    case 'W':
                    {
                        ushort n = p.ToUInt16BigEndian();
                        p += 2;
                        if (GameType == SIMONGameType.GType_PP)
                        {
                            if (n >= 60000 && n < 62048)
                                DebugN("[{0}] ", n - 60000);
                            else
                                DebugN("{0} ", n);
                        }
                        else
                        {
                            if (n >= 30000 && n < 30512)
                                DebugN("[{0}] ", n - 30000);
                            else
                                DebugN("{0} ", n);
                        }
                        break;
                    }

                    case 'w':
                    {
                        int n = (short) p.ToUInt16BigEndian();
                        p += 2;
                        DebugN("{0} ", n);
                        break;
                    }

                    case 'I':
                    {
                        int n = (short) p.ToUInt16BigEndian();
                        p += 2;
                        if (n == -1)
                            DebugN("SUBJECT_ITEM ");
                        else if (n == -3)
                            DebugN("OBJECT_ITEM ");
                        else if (n == -5)
                            DebugN("ME_ITEM ");
                        else if (n == -7)
                            DebugN("ACTOR_ITEM ");
                        else if (n == -9)
                            DebugN("ITEM_A_PARENT ");
                        else
                            DebugN("<{0}> ", n);
                        break;
                    }

                    case 'J':
                    {
                        DebugN(". ");
                    }
                        break;

                    case 'T':
                    {
                        uint n = p.ToUInt16BigEndian();
                        p += 2;
                        if (n != 0xFFFF)
                            DebugN("\"{0}\"({1}) ", GetStringPtrById((ushort) n), n);
                        else
                            DebugN("NULL_STRING ");
                    }
                        break;
                }
            }
        }

        private void DumpSubroutineLine(SubroutineLine sl, Subroutine sub)
        {
            DebugN("; ****\n");

            var p = sl.Pointer + SUBROUTINE_LINE_SMALL_SIZE;
            if (sub.id == 0)
            {
                DebugN("; verb={0}, noun1={1}, noun2={2}\n", sl.verb, sl.noun1, sl.noun2);
                p = sl.Pointer + SUBROUTINE_LINE_BIG_SIZE;
            }

            for (;;)
            {
                p = DumpOpcode(p);
                if (p == BytePtr.Null)
                    break;
            }
        }

        private void DumpSubroutine(Subroutine sub)
        {
            DebugN("\n******************************************\n;Subroutine, ID={0}:\nSUB_{1}:\n", sub.id, sub.id);
            var sl = new SubroutineLine(sub.Pointer + sub.first);
            for (; sl.Pointer != sub.Pointer; sl.Pointer = sub.Pointer + sl.next)
            {
                DumpSubroutineLine(sl, sub);
            }
            DebugN("\nEND ******************************************\n");
        }

        private void DumpVgaScript(BytePtr ptr, ushort res, ushort id)
        {
            DumpVgaScriptAlways(ptr, res, id);
        }

        protected void DumpVgaScriptAlways(BytePtr ptr, ushort res, ushort id)
        {
            DebugN("; address={0:X}, vgafile={1}  vgasprite={2}\n",
                (uint) (ptr.Offset - _vgaBufferPointers[res].vgaFile1.Offset), res, id);
            DumpVideoScript(ptr, false);
            DebugN("; end\n");
        }

        private void DumpVideoScript(BytePtr src, bool singeOpcode)
        {
            string str = null;

            do
            {
                ushort opcode;
                if (GameType == SIMONGameType.GType_SIMON2 || GameType == SIMONGameType.GType_FF ||
                    GameType == SIMONGameType.GType_PP)
                {
                    opcode = src.Value;
                    src.Offset++;
                }
                else
                {
                    opcode = src.ToUInt16BigEndian();
                    src += 2;
                }

                if (opcode >= _numVideoOpcodes)
                {
                    Error("dumpVideoScript: Opcode {0} out of range ({0})", opcode, _numVideoOpcodes);
                }

                switch (GameType)
                {
//                    case SIMONGameType.GType_PP:
//                        str = puzzlepack_videoOpcodeNameTable[opcode];
//                        break;
//                    case SIMONGameType.GType_FF:
//                        str = feeblefiles_videoOpcodeNameTable[opcode];
//                        break;
//                    case SIMONGameType.GType_SIMON2:
//                        str = simon2_videoOpcodeNameTable[opcode];
//                        break;
                    case SIMONGameType.GType_SIMON1:
                        str = simon1_videoOpcodeNameTable[opcode];
                        break;
//                    case SIMONGameType.GType_WW:
//                        str = ww_videoOpcodeNameTable[opcode];
//                        break;
//                    case SIMONGameType.GType_ELVIRA2:
//                        str = elvira2_videoOpcodeNameTable[opcode];
//                        break;
//                    case SIMONGameType.GType_ELVIRA1:
//                        str = elvira1_videoOpcodeNameTable[opcode];
//                        break;
//                    default:
//                        str = pn_videoOpcodeNameTable[opcode];
//                        break;
                }

                if (str == null)
                {
                    Error("dumpVideoScript: Invalid Opcode {0}", opcode);
                }

                int strn = 0;
                while (str[strn] != '|')
                    strn++;
                DebugN("{0:D2}: {1} ", opcode, str.Substring(strn + 1));

                int end = (GameType == SIMONGameType.GType_FF || GameType == SIMONGameType.GType_PP) ? 9999 : 999;
                for (; str[strn] != '|'; strn++)
                {
                    switch (str[strn])
                    {
                        case 'x':
                            DebugN("\n");
                            return;
                        case 'b':
                        {
                            DebugN("{0} ", str[strn++]);
                            break;
                        }
                        case 'w':
                        {
                            short v = (short) ReadUint16Wrapper(src);
                            src += 2;
                            if (v < 0)
                                DebugN("[{0}] ", -v);
                            else
                                DebugN("{0} ", v);
                            break;
                        }
                        case 'd':
                        {
                            DebugN("{0} ", (short) ReadUint16Wrapper(src));
                            src += 2;
                            break;
                        }
                        case 'v':
                        {
                            DebugN("[{0}] ", ReadUint16Wrapper(src));
                            src += 2;
                            break;
                        }
                        case 'i':
                        {
                            DebugN("{0} ", (short) ReadUint16Wrapper(src));
                            src += 2;
                            break;
                        }
                        case 'j':
                        {
                            DebugN(". ");
                            break;
                        }
                        case 'q':
                        {
                            while (ReadUint16Wrapper(src) != end)
                            {
                                DebugN("({0},{0}) ", ReadUint16Wrapper(src),
                                    ReadUint16Wrapper(src + 2));
                                src += 4;
                            }
                            src += 2;
                            break;
                        }
                        default:
                            Error("dumpVideoScript: Invalid fmt string '{0}' in decompile VGA", str[strn]);
                            break;
                    }
                }

                DebugN("\n");
            } while (!singeOpcode);
        }

        private void DumpBitmap(string filename, BytePtr offs, ushort w, ushort h, int flags, Color[] palette,
            byte @base)
        {
            byte[] imageBuffer = new byte[w * h];

            Vc10State state = new Vc10State();
            state.depack_cont = -0x80;
            state.srcPtr = offs;
            state.dh = h;
            state.height = h;
            state.width = (ushort) (w / 16);

            if (Features.HasFlag(GameFeatures.GF_PLANAR))
            {
                state.srcPtr = ConvertImage(state, GameType == SIMONGameType.GType_PN || (flags & 0x80) != 0);
                flags &= ~0x80;
            }

            var src = state.srcPtr;
            BytePtr dst = imageBuffer;
            int i, j;

            if (w > _screenWidth)
            {
                for (i = 0; i < w; i += 8)
                {
                    DecodeColumn(dst, src + (int) ReadUint32Wrapper(src), h, w);
                    dst.Offset += 8;
                    src += 4;
                }
            }
            else if (h > _screenHeight)
            {
                for (i = 0; i < h; i += 8)
                {
                    DecodeRow(dst, src + (int) ReadUint32Wrapper(src), w, w);
                    dst += 8 * w;
                    src += 4;
                }
            }
            else if (GameType == SIMONGameType.GType_FF || GameType == SIMONGameType.GType_PP)
            {
                if ((flags & 0x80) != 0)
                {
                    for (i = 0; i != w; i++)
                    {
                        var c = vc10_depackColumn(state);
                        for (j = 0; j != h; j++)
                        {
                            dst[j * w + i] = c[j];
                        }
                    }
                }
                else
                {
                    for (j = 0; j != h; j++)
                    {
                        for (i = 0; i != w; i++)
                        {
                            dst[i] = src[i];
                        }
                        dst += w;
                        src += w;
                    }
                }
            }
            else if ((GameType == SIMONGameType.GType_SIMON1 || GameType == SIMONGameType.GType_SIMON2) && w == 320 &&
                     (h == 134 || h == 135 || h == 200))
            {
                for (j = 0; j != h; j++)
                {
                    ushort count = (ushort) (w / 8);

                    var dstPtr = dst;
                    do
                    {
                        uint bits = (uint) ((src[0] << 24) | (src[1] << 16) | (src[2] << 8) | (src[3]));

                        dstPtr[0] = (byte) ((bits >> (32 - 5)) & 31);
                        dstPtr[1] = (byte) ((bits >> (32 - 10)) & 31);
                        dstPtr[2] = (byte) ((bits >> (32 - 15)) & 31);
                        dstPtr[3] = (byte) ((bits >> (32 - 20)) & 31);
                        dstPtr[4] = (byte) ((bits >> (32 - 25)) & 31);
                        dstPtr[5] = (byte) ((bits >> (32 - 30)) & 31);

                        bits = (bits << 8) | src[4];

                        dstPtr[6] = (byte) ((bits >> (40 - 35)) & 31);
                        dstPtr[7] = (byte) ((bits) & 31);

                        dstPtr += 8;
                        src += 5;
                    } while (--count != 0);
                    dst += w;
                }
            }
            else if ((flags & 0x80) != 0)
            {
                for (i = 0; i != w; i += 2)
                {
                    var c = vc10_depackColumn(state);
                    for (j = 0; j != h; j++)
                    {
                        byte col = c[j];
                        dst[j * w + i] = (byte) ((col >> 4) | @base);
                        dst[j * w + i + 1] = (byte) ((col & 0xF) | @base);
                    }
                }
            }
            else
            {
                for (j = 0; j != h; j++)
                {
                    for (i = 0; i != w / 2; i++)
                    {
                        byte col = src[i];
                        dst[i * 2] = (byte) ((col >> 4) | @base);
                        dst[i * 2 + 1] = (byte) ((col & 0xF) | @base);
                    }
                    dst += w;
                    src += w / 2;
                }
            }

            DumpBMP(filename, (short) w, (short) h, imageBuffer, palette);
        }

        private static void DumpBMP(string filename, short w, short h, BytePtr bytes, Color[] palette)
        {
            byte[] my_hdr = new byte[bmp_hdr.Length];
            var @out = OpenFileWrite(filename);
            if (@out == null)
                return;

            Array.Copy(bmp_hdr, my_hdr, bmp_hdr.Length);

            my_hdr.WriteUInt32(2, (uint) (w * h + 1024 + bmp_hdr.Length));
            my_hdr.WriteUInt32(18, (uint) w);
            my_hdr.WriteUInt32(22, (uint) h);

            @out.Write(my_hdr, 0, my_hdr.Length);

            byte[] color = new byte[4];
            for (var i = 0; i != 256; i++)
            {
                var c = palette[i];
                color[0] = (byte) c.B;
                color[1] = (byte) c.G;
                color[2] = (byte) c.R;
                color[3] = 0;
                @out.Write(color, 0, 4);
            }

            while (--h >= 0)
            {
                @out.Write(bytes.Data, bytes.Offset + h * ((w + 3) & ~3), (w + 3) & ~3);
            }
            @out.Dispose();
        }

        private void DumpSingleBitmap(int file, int image, BytePtr offs, int w, int h, byte @base)
        {
            var buf = $"dumps/File{file}_Image{image}.bmp";

            if (FileExists(buf))
                return;

            DumpBitmap(buf, offs, (ushort) w, (ushort) h, 0, _displayPalette, @base);
        }

        private void DumpAllSubroutines()
        {
            for (int i = 0; i < 65536; i++)
            {
                var sub = GetSubroutineByID((uint) i);
                if (sub != null)
                {
                    DumpSubroutine(sub);
                }
            }
        }

        private void DumpAllVgaImageFiles()
        {
            byte start = (byte) ((GameType == SIMONGameType.GType_PN) ? 0 : 2);

            for (int z = start; z < _numZone; z++)
            {
                LoadZone((ushort) z, false);
                DumpVgaBitmaps((ushort) z);
            }
        }

        private void DumpAllVgaScriptFiles()
        {
            byte start = (byte) ((GameType == SIMONGameType.GType_PN) ? 0 : 2);

            for (int z = start; z < _numZone; z++)
            {
                ushort zoneNum = (ushort) ((GameType == SIMONGameType.GType_PN) ? 0 : z);
                LoadZone((ushort) z, false);

                VgaPointersEntry vpe = _vgaBufferPointers[zoneNum];
                if (vpe.vgaFile1 != BytePtr.Null)
                {
                    _curVgaFile1 = vpe.vgaFile1;
                    DumpVgaFile(_curVgaFile1);
                }
            }
        }

        protected virtual void DumpVgaFile(BytePtr vga)
        {
            throw new NotImplementedException();
        }

        private void DumpVgaBitmaps(ushort zoneNum)
        {
            ushort zone = (ushort) (GameType == SIMONGameType.GType_PN ? 0 : zoneNum);
            VgaPointersEntry vpe = _vgaBufferPointers[zone];
            if (vpe.vgaFile1 == BytePtr.Null || vpe.vgaFile2 == BytePtr.Null)
                return;

            var vga1 = vpe.vgaFile1;
            var vga2 = vpe.vgaFile2;
            int imageBlockSize = vpe.vgaFile2End.Offset - vpe.vgaFile2.Offset;

            var pal = PalLoad(vga1, 0, 0);

            var offsEnd = (int) ReadUint32Wrapper(vga2 + 8);
            for (var i = 1;; i++)
            {
                if (i * 8 >= offsEnd)
                    break;

                BytePtr p2 = vga2 + i * 8;
                var offs = (int) ReadUint32Wrapper(p2);

                var width = ReadUint16Wrapper(p2 + 6);
                ushort height;
                ushort flags;
                if (GameType == SIMONGameType.GType_FF || GameType == SIMONGameType.GType_PP)
                {
                    height = (ushort) (p2.ToUInt16(4) & 0x7FFF);
                    flags = p2[5];
                }
                else
                {
                    height = p2[5];
                    flags = p2[4];
                }

                Debug(1, "Zone {0}: Image {1}. Offs= {2} Width={3}, Height={4}, Flags=0x{5:X}", zoneNum, i, offs, width, height,
                    flags);
                if (offs >= imageBlockSize || width == 0 || height == 0)
                    break;

                /* dump bitmap */
                var buf = $"dumps/Res{zoneNum}_Image{i}.bmp";

                DumpBitmap(buf, vga2 + offs, width, height, flags, pal, 0);
            }
        }

        private Color[] PalLoad(BytePtr vga1, int a, int b)
        {
            Color[] pal = new Color[256];
            BytePtr src;
            ushort num, palSize;

            if (GameType == SIMONGameType.GType_FF || GameType == SIMONGameType.GType_PP)
            {
                num = 256;
                palSize = 768;
            }
            else
            {
                num = 32;
                palSize = 96;
            }

            if (GameType == SIMONGameType.GType_PN && (Features.HasFlag(GameFeatures.GF_EGA)))
            {
                Array.Copy(_displayPalette, pal, 16);
            }
            else if (GameType == SIMONGameType.GType_PN || GameType == SIMONGameType.GType_ELVIRA1 ||
                     GameType == SIMONGameType.GType_ELVIRA2 || GameType == SIMONGameType.GType_WW)
            {
                src = vga1 + vga1.ToUInt16BigEndian(6) + b * 32;

                for (int i = 0; i < num; i++)
                {
                    ushort color = src.ToUInt16BigEndian();
                    pal[i] = Color.FromRgb(
                        ((color & 0xf00) >> 8) * 32,
                        ((color & 0x0f0) >> 4) * 32,
                        ((color & 0x00f) >> 0) * 32);
                    src += 2;
                }
            }
            else
            {
                src = vga1 + 6 + b * palSize;

                for (int i = 0; i < num; i++)
                {
                    pal[i] = Color.FromRgb(
                        src[0] << 2,
                        src[1] << 2,
                        src[2] << 2);

                    src += 3;
                }
            }
            return pal;
        }
    }
}