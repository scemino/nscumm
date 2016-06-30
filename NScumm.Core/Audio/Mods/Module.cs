//
//  Module.cs
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
using System.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Core.Audio
{
    public struct note_t
    {
        public byte sample;
        public byte note;
        public ushort period;
        public ushort effect;
    }

    public struct sample_t
    {
        public string name;
        public ushort len;
        public byte finetune;
        public byte vol;
        public ushort repeat;
        public ushort replen;
        public byte[] data;
    }

    public struct sample_offs
    {
        public string name;
        public ushort len;
        public uint offs;
    }

    public class Module
    {
        public string songname;

        public const int NUM_SAMPLES = 31;
        public sample_t[] sample = new sample_t[NUM_SAMPLES];
        public sample_offs[] commonSamples = new sample_offs[NUM_SAMPLES];

        public byte songlen;
        public byte undef;
        public byte[] songpos = new byte[128];
        public uint sig;
        public note_t[,,] pattern;

        private static readonly uint[] signatures =
        {
            ScummHelper.MakeTag('M','.','K','.'), ScummHelper.MakeTag('M','!','K','!'), ScummHelper.MakeTag('F','L','T','4')
        };

        private static readonly short[,] periods = {
{1712, 1616, 1524, 1440, 1356, 1280, 1208, 1140, 1076, 1016, 960 , 906,
     856 , 808 , 762 , 720 , 678 , 640 , 604 , 570 , 538 , 508 , 480 , 453,
     428 , 404 , 381 , 360 , 339 , 320 , 302 , 285 , 269 , 254 , 240 , 226,
     214 , 202 , 190 , 180 , 170 , 160 , 151 , 143 , 135 , 127 , 120 , 113,
     107 , 101 , 95  , 90  , 85  , 80  , 75  , 71  , 67  , 63  , 60  , 56 },
    {1700, 1604, 1514, 1430, 1348, 1274, 1202, 1134, 1070, 1010, 954 , 900,
     850 , 802 , 757 , 715 , 674 , 637 , 601 , 567 , 535 , 505 , 477 , 450,
     425 , 401 , 379 , 357 , 337 , 318 , 300 , 284 , 268 , 253 , 239 , 225,
     213 , 201 , 189 , 179 , 169 , 159 , 150 , 142 , 134 , 126 , 119 , 113,
     106 , 100 , 94  , 89  , 84  , 79  , 75  , 71  , 67  , 63  , 59  , 56 },
    {1688, 1592, 1504, 1418, 1340, 1264, 1194, 1126, 1064, 1004, 948 , 894,
     844 , 796 , 752 , 709 , 670 , 632 , 597 , 563 , 532 , 502 , 474 , 447,
     422 , 398 , 376 , 355 , 335 , 316 , 298 , 282 , 266 , 251 , 237 , 224,
     211 , 199 , 188 , 177 , 167 , 158 , 149 , 141 , 133 , 125 , 118 , 112,
     105 , 99  , 94  , 88  , 83  , 79  , 74  , 70  , 66  , 62  , 59  , 56 },
    {1676, 1582, 1492, 1408, 1330, 1256, 1184, 1118, 1056, 996 , 940 , 888,
     838 , 791 , 746 , 704 , 665 , 628 , 592 , 559 , 528 , 498 , 470 , 444,
     419 , 395 , 373 , 352 , 332 , 314 , 296 , 280 , 264 , 249 , 235 , 222,
     209 , 198 , 187 , 176 , 166 , 157 , 148 , 140 , 132 , 125 , 118 , 111,
     104 , 99  , 93  , 88  , 83  , 78  , 74  , 70  , 66  , 62  , 59  , 55 },
    {1664, 1570, 1482, 1398, 1320, 1246, 1176, 1110, 1048, 990 , 934 , 882,
     832 , 785 , 741 , 699 , 660 , 623 , 588 , 555 , 524 , 495 , 467 , 441,
     416 , 392 , 370 , 350 , 330 , 312 , 294 , 278 , 262 , 247 , 233 , 220,
     208 , 196 , 185 , 175 , 165 , 156 , 147 , 139 , 131 , 124 , 117 , 110,
     104 , 98  , 92  , 87  , 82  , 78  , 73  , 69  , 65  , 62  , 58  , 55 },
    {1652, 1558, 1472, 1388, 1310, 1238, 1168, 1102, 1040, 982 , 926 , 874,
     826 , 779 , 736 , 694 , 655 , 619 , 584 , 551 , 520 , 491 , 463 , 437,
     413 , 390 , 368 , 347 , 328 , 309 , 292 , 276 , 260 , 245 , 232 , 219,
     206 , 195 , 184 , 174 , 164 , 155 , 146 , 138 , 130 , 123 , 116 , 109,
     103 , 97  , 92  , 87  , 82  , 77  , 73  , 69  , 65  , 61  , 58  , 54 },
    {1640, 1548, 1460, 1378, 1302, 1228, 1160, 1094, 1032, 974 , 920 , 868,
     820 , 774 , 730 , 689 , 651 , 614 , 580 , 547 , 516 , 487 , 460 , 434,
     410 , 387 , 365 , 345 , 325 , 307 , 290 , 274 , 258 , 244 , 230 , 217,
     205 , 193 , 183 , 172 , 163 , 154 , 145 , 137 , 129 , 122 , 115 , 109,
     102 , 96  , 91  , 86  , 81  , 77  , 72  , 68  , 64  , 61  , 57  , 54 },
    {1628, 1536, 1450, 1368, 1292, 1220, 1150, 1086, 1026, 968 , 914 , 862,
     814 , 768 , 725 , 684 , 646 , 610 , 575 , 543 , 513 , 484 , 457 , 431,
     407 , 384 , 363 , 342 , 323 , 305 , 288 , 272 , 256 , 242 , 228 , 216,
     204 , 192 , 181 , 171 , 161 , 152 , 144 , 136 , 128 , 121 , 114 , 108,
     102 , 96  , 90  , 85  , 80  , 76  , 72  , 68  , 64  , 60  , 57  , 54 },
    {1814, 1712, 1616, 1524, 1440, 1356, 1280, 1208, 1140, 1076, 1016, 960,
     907 , 856 , 808 , 762 , 720 , 678 , 640 , 604 , 570 , 538 , 508 , 480,
     453 , 428 , 404 , 381 , 360 , 339 , 320 , 302 , 285 , 269 , 254 , 240,
     226 , 214 , 202 , 190 , 180 , 170 , 160 , 151 , 143 , 135 , 127 , 120,
     113 , 107 , 101 , 95  , 90  , 85  , 80  , 75  , 71  , 67  , 63  , 60 },
    {1800, 1700, 1604, 1514, 1430, 1350, 1272, 1202, 1134, 1070, 1010, 954,
     900 , 850 , 802 , 757 , 715 , 675 , 636 , 601 , 567 , 535 , 505 , 477,
     450 , 425 , 401 , 379 , 357 , 337 , 318 , 300 , 284 , 268 , 253 , 238,
     225 , 212 , 200 , 189 , 179 , 169 , 159 , 150 , 142 , 134 , 126 , 119,
     112 , 106 , 100 , 94  , 89  , 84  , 79  , 75  , 71  , 67  , 63  , 59 },
    {1788, 1688, 1592, 1504, 1418, 1340, 1264, 1194, 1126, 1064, 1004, 948,
     894 , 844 , 796 , 752 , 709 , 670 , 632 , 597 , 563 , 532 , 502 , 474,
     447 , 422 , 398 , 376 , 355 , 335 , 316 , 298 , 282 , 266 , 251 , 237,
     223 , 211 , 199 , 188 , 177 , 167 , 158 , 149 , 141 , 133 , 125 , 118,
     111 , 105 , 99  , 94  , 88  , 83  , 79  , 74  , 70  , 66  , 62  , 59 },
    {1774, 1676, 1582, 1492, 1408, 1330, 1256, 1184, 1118, 1056, 996 , 940,
     887 , 838 , 791 , 746 , 704 , 665 , 628 , 592 , 559 , 528 , 498 , 470,
     444 , 419 , 395 , 373 , 352 , 332 , 314 , 296 , 280 , 264 , 249 , 235,
     222 , 209 , 198 , 187 , 176 , 166 , 157 , 148 , 140 , 132 , 125 , 118,
     111 , 104 , 99  , 93  , 88  , 83  , 78  , 74  , 70  , 66  , 62  , 59 },
    {1762, 1664, 1570, 1482, 1398, 1320, 1246, 1176, 1110, 1048, 988 , 934,
     881 , 832 , 785 , 741 , 699 , 660 , 623 , 588 , 555 , 524 , 494 , 467,
     441 , 416 , 392 , 370 , 350 , 330 , 312 , 294 , 278 , 262 , 247 , 233,
     220 , 208 , 196 , 185 , 175 , 165 , 156 , 147 , 139 , 131 , 123 , 117,
     110 , 104 , 98  , 92  , 87  , 82  , 78  , 73  , 69  , 65  , 61  , 58 },
    {1750, 1652, 1558, 1472, 1388, 1310, 1238, 1168, 1102, 1040, 982 , 926,
     875 , 826 , 779 , 736 , 694 , 655 , 619 , 584 , 551 , 520 , 491 , 463,
     437 , 413 , 390 , 368 , 347 , 328 , 309 , 292 , 276 , 260 , 245 , 232,
     219 , 206 , 195 , 184 , 174 , 164 , 155 , 146 , 138 , 130 , 123 , 116,
     109 , 103 , 97  , 92  , 87  , 82  , 77  , 73  , 69  , 65  , 61  , 58 },
    {1736, 1640, 1548, 1460, 1378, 1302, 1228, 1160, 1094, 1032, 974 , 920,
     868 , 820 , 774 , 730 , 689 , 651 , 614 , 580 , 547 , 516 , 487 , 460,
     434 , 410 , 387 , 365 , 345 , 325 , 307 , 290 , 274 , 258 , 244 , 230,
     217 , 205 , 193 , 183 , 172 , 163 , 154 , 145 , 137 , 129 , 122 , 115,
     108 , 102 , 96  , 91  , 86  , 81  , 77  , 72  , 68  , 64  , 61  , 57 },
    {1724, 1628, 1536, 1450, 1368, 1292, 1220, 1150, 1086, 1026, 968 , 914,
     862 , 814 , 768 , 725 , 684 , 646 , 610 , 575 , 543 , 513 , 484 , 457,
     431 , 407 , 384 , 363 , 342 , 323 , 305 , 288 , 272 , 256 , 242 , 228,
     216 , 203 , 192 , 181 , 171 , 161 , 152 , 144 , 136 , 128 , 121 , 114,
     108 , 101 , 96  , 90  , 85  , 80  , 76  , 72  , 68  , 64  , 60  , 57 }};

        public Module()
        {
            for (int i = 0; i < NUM_SAMPLES; ++i)
            {
                sample[i].data = null;
            }
        }

        public bool Load(Stream st, int offs)
        {
            var br = new BinaryReader(st);
            if (offs != 0)
            {
                // Load the module with the common sample data
                Load(st, 0);
            }

            st.Seek(offs, SeekOrigin.Begin);
            songname = br.ReadBytes(20).GetRawText();

            for (int i = 0; i < NUM_SAMPLES; ++i)
            {
                sample[i].name = br.ReadBytes(22).GetRawText();
                sample[i].len = (ushort)(2 * br.ReadUInt16BigEndian());

                sample[i].finetune = br.ReadByte();
                System.Diagnostics.Debug.Assert(sample[i].finetune < 0x10);

                sample[i].vol = br.ReadByte();
                sample[i].repeat = (ushort)(2 * br.ReadUInt16BigEndian());
                sample[i].replen = (ushort)(2 * br.ReadUInt16BigEndian());
            }

            songlen = br.ReadByte();
            undef = br.ReadByte();

            songpos = br.ReadBytes(128);

            sig = br.ReadUInt32BigEndian();

            bool foundSig = false;
            for (int i = 0; i < signatures.Length; i++)
            {
                if (sig == signatures[i])
                {
                    foundSig = true;
                    break;
                }
            }

            if (!foundSig)
            {
                Warning("No known signature found in protracker module");
                return false;
            }

            int maxpattern = 0;
            for (int i = 0; i < 128; ++i)
                if (maxpattern < songpos[i])
                    maxpattern = songpos[i];

            pattern = new note_t[64, 4, maxpattern + 1];

            for (int i = 0; i <= maxpattern; ++i)
            {
                for (int j = 0; j < 64; ++j)
                {
                    for (int k = 0; k < 4; ++k)
                    {
                        uint note = br.ReadUInt32BigEndian();
                        pattern[i, j, k].sample = (byte)((note & 0xf0000000) >> 24 | (note & 0x0000f000) >> 12);
                        pattern[i, j, k].period = (ushort)((note >> 16) & 0xfff);
                        pattern[i, j, k].effect = (ushort)(note & 0xfff);
                        pattern[i, j, k].note = PeriodToNote((short)((note >> 16) & 0xfff));
                    }
                }
            }

            for (int i = 0; i < NUM_SAMPLES; ++i)
            {
                if (offs != 0)
                {
                    // Restore information for modules that use common sample data
                    for (int j = 0; j < NUM_SAMPLES; ++j)
                    {
                        if (StringComparer.OrdinalIgnoreCase.Equals(commonSamples[j].name, sample[i].name))
                        {
                            sample[i].len = commonSamples[j].len;
                            st.Seek(commonSamples[j].offs, SeekOrigin.Begin);
                            break;
                        }
                    }
                }
                else
                {
                    // Store information for modules that use common sample data
                    commonSamples[i].name = sample[i].name;
                    commonSamples[i].len = sample[i].len;
                    commonSamples[i].offs = (uint)st.Position;

                }

                if (sample[i].len == 0)
                {
                    sample[i].data = null;
                }
                else
                {
                    sample[i].data = br.ReadBytes(sample[i].len);
                }
            }

            return true;

        }

        public static byte PeriodToNote(short period, byte finetune = 0)
        {
            short diff1;
            short diff2;

            diff1 = (short)Math.Abs(periods[finetune, 0] - period);
            if (diff1 == 0)
                return 0;

            for (int i = 1; i < 60; i++)
            {
                diff2 = (short)Math.Abs(periods[finetune, i] - period);
                if (diff2 == 0)
                    return (byte)i;
                if (diff2 > diff1)
                    return (byte)(i - 1);
                diff1 = diff2;
            }
            return 59;
        }

        public static short NoteToPeriod(byte note, byte finetune = 0)
        {
            if (finetune > 15)
                finetune = 15;
            if (note > 59)
                note = 59;

            return periods[finetune, note];
        }
    }
    
}
