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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NScumm.Sword1
{
    enum StartPosOpcodes
    {
        opcSeqEnd = 0,
        opcCallFn,
        opcCallFnLong,
        opcSetVar8,
        opcSetVar16,
        opcSetVar32,
        opcGeorge,
        opcRunStart,
        opcRunHelper,
        opcPlaySequence,
        opcAddObject,
        opcRemoveObject,
        opcMegaSet,
        opcNoSprite
    }

    class StaticRes
    {
        public const int STAND = 0;
        public const int UP = 0;
        public const int UP_RIGHT = 1;
        public const int U_R = 1;
        public const int RIGHT = 2;
        public const int DOWN_RIGHT = 3;
        public const int D_R = 3;
        public const int DOWN = 4;
        public const int DOWN_LEFT = 5;
        public const int D_L = 5;
        public const int LEFT = 6;
        public const int UP_LEFT = 7;
        public const int U_L = 7;

        const int FLOOR_1 = 65536;
        const int FLOOR_2 = 131072;
        const int FLOOR_3 = 196608;
        const int FLOOR_4 = 262144;
        const int FLOOR_5 = 327680;
        const int FLOOR_6 = 393216;
        const int FLOOR_7 = 458752;
        const int FLOOR_8 = 524288;
        const int FLOOR_9 = 589824;
        const int FLOOR_10 = 655360;
        const int FLOOR_11 = 720896;
        const int FLOOR_12 = 786432;
        const int FLOOR_13 = 851968;
        const int FLOOR_14 = 917504;
        const int FLOOR_15 = 983040;
        const int FLOOR_16 = 1048576;
        const int FLOOR_17 = 1114112;
        const int FLOOR_18 = 1179648;
        const int FLOOR_19 = 1245184;
        const int FLOOR_20 = 1310720;
        const int FLOOR_21 = 1376256;
        const int FLOOR_22 = 1441792;
        const int FLOOR_23 = 1507328;
        const int FLOOR_24 = 1572864;
        const int FLOOR_25 = 1638400;
        const int FLOOR_26 = 1703936;
        const int FLOOR_27 = 1769472;
        const int FLOOR_28 = 1835008;
        const int FLOOR_29 = 1900544;
        const int FLOOR_31 = 2031616;
        const int FLOOR_32 = 2097152;
        const int FLOOR_33 = 2162688;
        const int FLOOR_34 = 2228224;
        const int FLOOR_35 = 2293760;
        const int FLOOR_36 = 2359296;
        const int FLOOR_37 = 2424832;
        const int FLOOR_38 = 2490368;
        const int FLOOR_39 = 2555904;
        const int FLOOR_40 = 2621440;
        const int FLOOR_41 = 2686976;
        const int FLOOR_42 = 2752512;
        const int FLOOR_43 = 2818048;
        const int FLOOR_45 = 2949120;
        const int FLOOR_46 = 3014656;
        const int FLOOR_47 = 3080192;
        const int FLOOR_48 = 3145728;
        const int FLOOR_49 = 3211264;
        const int FLOOR_50 = 3276800;
        const int FLOOR_53 = 3473408;
        const int FLOOR_54 = 3538944;
        const int FLOOR_55 = 3604480;
        const int FLOOR_56 = 3670016;
        const int FLOOR_57 = 3735552;
        const int FLOOR_58 = 3801088;
        const int FLOOR_59 = 3866624;
        const int FLOOR_60 = 3932160;
        const int LEFT_FLOOR_61 = 3997697;
        const int FLOOR_62 = 4063232;
        const int FLOOR_63 = 4128768;
        const int FLOOR_65 = 4259840;
        const int FLOOR_66 = 4325376;
        const int FLOOR_67 = 4390912;
        const int FLOOR_69 = 4521984;
        const int RIGHT_FLOOR_71 = 4653060;
        const int FLOOR_72 = 4718592;
        const int FLOOR_73 = 4784128;
        const int FLOOR_74 = 4849664;
        const int FLOOR_75 = 4915200;
        const int FLOOR_76 = 4980736;
        const int FLOOR_77 = 5046272;
        const int FLOOR_78 = 5111808;
        const int FLOOR_79 = 5177344;
        const int FLOOR_80 = 5242880;
        const int FLOOR_86 = 5636096;
        const int FLOOR_91 = 5963776;
        const int FLOOR_99 = 6488064;

        const int BEER_TOWEL = 3;
        const int HOTEL_KEY = 4;
        const int BALL = 5;
        const int RED_NOSE = 7;
        const int POLISHED_CHALICE = 8;
        const int PHOTOGRAPH = 10;
        const int GEM = 13;
        const int LAB_PASS = 17;
        const int LIFTING_KEYS = 18;
        const int MANUSCRIPT = 19;
        const int PLASTER = 23;
        const int ROSSO_CARD = 27;
        const int TISSUE = 32;
        const int LENS = 37;
        const int TRIPOD = 36;
        const int CHALICE = 31;
        const int MATCHBOOK = 20;
        const int PRESSURE_GAUGE = 24;
        const int BUZZER = 26;
        const int TOILET_KEY = 28;
        const int STONE_KEY = 30;
        const int TOILET_BRUSH = 33;
        const int MIRROR = 38;
        const int TOWEL_CUT = 39;


        // Intro with sequence
        public static readonly IEnumerable<byte> g_startPos0 =
            LOGIC_CALL_FN(StartPosOpcodes.opcPlaySequence, 4).Concat(
            GEORGE_POS(481, 413, DOWN, FLOOR_1)).Concat(
            INIT_SEQ_END());

        // Intro without sequence
        public static readonly IEnumerable<byte> g_startPos1 =
            GEORGE_POS(481, 413, DOWN, FLOOR_1).Concat(
            INIT_SEQ_END());

        public static readonly IEnumerable<byte> g_genIreland =
            LOGIC_SET_VAR8(ScriptVariableNames.PARIS_FLAG, 9).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, RED_NOSE)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, PHOTOGRAPH)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, LAB_PASS)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, LIFTING_KEYS)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, MATCHBOOK)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, BUZZER)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, TISSUE)).Concat(
            INIT_SEQ_END());

        public static readonly IEnumerable<byte> g_genSyria =
            LOGIC_SET_VAR8(ScriptVariableNames.PARIS_FLAG, 1).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, BALL)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, RED_NOSE)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, PHOTOGRAPH)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, LIFTING_KEYS)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, MATCHBOOK)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, BUZZER)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, TISSUE)).Concat(
            LOGIC_SET_VAR8(ScriptVariableNames.CHANGE_STANCE, STAND)).Concat(
            INIT_SEQ_END());

        public static readonly IEnumerable<byte> g_genSpain =
            LOGIC_SET_VAR8(ScriptVariableNames.PARIS_FLAG, 1).Concat(
            LOGIC_SET_VAR8(ScriptVariableNames.SPAIN_VISIT, 1)).Concat( // default to 1st spain visit, may get overwritten later
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, RED_NOSE)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, PHOTOGRAPH)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, LAB_PASS)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, LIFTING_KEYS)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, BUZZER)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, TISSUE)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, BALL)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, MATCHBOOK)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, PRESSURE_GAUGE)).Concat(
            INIT_SEQ_END());

        public static readonly IEnumerable<byte> g_genSpain2 = // 2nd spain visit
            LOGIC_SET_VAR8(ScriptVariableNames.SPAIN_VISIT, 2).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcRemoveObject, PRESSURE_GAUGE)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, POLISHED_CHALICE)).Concat(
            INIT_SEQ_END());

        public static readonly IEnumerable<byte> g_genNightTrain =
            LOGIC_SET_VAR8(ScriptVariableNames.PARIS_FLAG, 18).Concat(
            INIT_SEQ_END());

        public static readonly IEnumerable<byte> g_genScotland =
            LOGIC_SET_VAR8(ScriptVariableNames.PARIS_FLAG, 1).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, RED_NOSE)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, PHOTOGRAPH)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, LAB_PASS)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, LIFTING_KEYS)).Concat(
            LOGIC_CALL_FN(StartPosOpcodes.opcAddObject, BUZZER)).Concat(
            INIT_SEQ_END());

        public static readonly IEnumerable<byte> g_genWhiteCoat =
            LOGIC_SET_VAR8(ScriptVariableNames.PARIS_FLAG, 11).Concat(
            LOGIC_SET_VAR8(ScriptVariableNames.EVA_TEXT, 1)).Concat(
            LOGIC_SET_VAR8(ScriptVariableNames.EVA_MARQUET_FLAG, 2)).Concat(
            LOGIC_SET_VAR8(ScriptVariableNames.EVA_NURSE_FLAG, 4)).Concat(
            LOGIC_SET_VAR8(ScriptVariableNames.FOUND_WARD_FLAG, 1)).Concat(
            LOGIC_SET_VAR8(ScriptVariableNames.CONSULTANT_HERE, 1)).Concat(

            LOGIC_CALL_FN_LONG(StartPosOpcodes.opcMegaSet, Logic.PLAYER, SwordRes.GEORGE_WLK, SwordRes.MEGA_WHITE)).Concat(

            LOGIC_SET_VAR32(ScriptVariableNames.GEORGE_CDT_FLAG, SwordRes.WHT_TLK_TABLE)).Concat(
            LOGIC_SET_VAR8(ScriptVariableNames.GEORGE_TALK_FLAG, 0)).Concat(
            LOGIC_SET_VAR8(ScriptVariableNames.WHITE_COAT_FLAG, 1)).Concat(
            LOGIC_SET_VAR8(ScriptVariableNames.GEORGE_ALLOWED_REST_ANIMS, 0)).Concat(
            INIT_SEQ_END());

        static IEnumerable<byte> GEORGE_POS(ushort x, ushort y, byte dir, uint place)
        {
            return new[] { (byte)StartPosOpcodes.opcGeorge }.Concat(Encode16(x)).Concat(Encode16(y)).Concat(new[] { dir }).Concat(Encode24(place));
        }

        static IEnumerable<byte> LOGIC_CALL_FN(StartPosOpcodes opcode, byte param)
        {
            return new[] { (byte)StartPosOpcodes.opcCallFn, (byte)((int)opcode & 0xFF), param };
        }

        static IEnumerable<byte> LOGIC_CALL_FN_LONG(StartPosOpcodes opcode, int param1, int param2, int param3)
        {
            return
                new[] { (byte)StartPosOpcodes.opcCallFnLong, (byte)((int)opcode & 0xFF) }
                    .Concat(Encode32(param1))
                    .Concat(Encode32(param2))
                    .Concat(Encode32(param3));
        }

        static IEnumerable<byte> LOGIC_SET_VAR8(ScriptVariableNames id, byte value)
        {
            return new[] { (byte)StartPosOpcodes.opcSetVar8 }.Concat(Encode16((ushort)id)).Concat(Encode8(value));
        }

        static IEnumerable<byte> LOGIC_SET_VAR32(ScriptVariableNames id, int value)
        {
            return new[] { (byte)StartPosOpcodes.opcSetVar32 }.Concat(Encode16((ushort)id)).Concat(Encode32(value));
        }

        static IEnumerable<byte> INIT_SEQ_END()
        {
            return new[] { (byte)StartPosOpcodes.opcSeqEnd };
        }

        static IEnumerable<byte> Encode8(byte value)
        {
            return new[] { value };
        }

        static IEnumerable<byte> Encode16(ushort value)
        {
            return new[] { (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF) };
        }

        static IEnumerable<byte> Encode24(uint value)
        {
            return new[] { (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value >> 16) };
        }

        private static IEnumerable<byte> Encode32(int value)
        {
            return new[] { (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF), (byte)((value >> 16) & 0xFF), (byte)(value >> 24) };
        }
    }
}
