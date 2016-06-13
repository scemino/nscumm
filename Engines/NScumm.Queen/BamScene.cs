//
//  QueenEngine.cs
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
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Queen
{
    public enum BamFlags
    {
        F_STOP = 0,
        F_PLAY = 1,
        F_REQ_STOP = 2
    }

    class BamDataObj
    {
        public short x, y;
        public short frame;

        public BamDataObj(short x, short y, short frame)
        {
            this.x = x;
            this.y = y;
            this.frame = frame;
        }
    }

    class BamDataBlock
    {
        public BamDataObj obj1;
        // truck / Frank
        public BamDataObj obj2;
        // Rico  / robot
        public BamDataObj fx;
        public short sfx;

        public BamDataBlock(short o1x, short o1y, short o1f, short o2x, short o2y, short o2f, short fxx, short fxy, short fxf, short sfx)
        {
            obj1 = new BamDataObj(o1x, o1y, o1f);
            obj2 = new BamDataObj(o2x, o2y, o2f);
            fx = new BamDataObj(fxx, fxy, fxf);
            this.sfx = sfx;
        }

        public BamDataBlock(BamDataObj obj1, BamDataObj obj2, BamDataObj fx, short sfx)
        {
            this.obj1 = obj1;
            this.obj2 = obj2;
            this.fx = fx;
            this.sfx = sfx;
        }
    }

    public class BamScene
    {
        public BamFlags _flag;
        public ushort _index;
        private QueenEngine _vm;
        private BamDataBlock[] _fightData;
        private BobSlot _obj1;
        private BobSlot _obj2;
        private BobSlot _objfx;
        private bool _screenShaked;
        private ushort _lastSoundIndex;

        private static readonly BamDataBlock[] _carData = {
            new BamDataBlock (310, 105, 1 , 314, 106, 17 , 366, 101,  1,  0),
            new BamDataBlock (303, 105, 1 , 307, 106, 17 , 214,   0, 10 ,  0),
            new BamDataBlock (297, 104, 1 , 301, 105, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (294, 103, 1 , 298, 104, 17 , 214,   0, 10,  0 ),
            new BamDataBlock (291, 102, 1 , 295, 103, 18 , 214,   0, 10 ,  0 ),
            new BamDataBlock (287, 101, 1 , 291, 102, 18 , 266,  51, 10 ,  2 ),
            new BamDataBlock (283, 100, 1 , 287, 101, 19 , 279,  47, 11,  0 ),
            new BamDataBlock (279,  99, 1 , 283, 100, 20 , 294,  46, 12 ,  0 ),
            new BamDataBlock (274,  98, 1 , 278,  99, 20 , 305,  44, 13 ,  0 ),
            new BamDataBlock (269,  98, 1 , 273,  99, 20 , 320,  42, 14 ,  0 ),
            new BamDataBlock (264,  98, 1 , 268,  99, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (261,  98, 1 , 265,  99, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (259,  98, 1 , 263,  99, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (258,  98, 1 , 262,  99, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (257,  98, 2 , 260,  99, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (255,  99, 3 , 258, 100, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (258,  99, 4 , 257, 100, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (264, 102, 4 , 263, 103, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (272, 105, 5 , 274, 106, 17 , 214,   0, 10,  0 ),
            new BamDataBlock (276, 107, 5 , 277, 108, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (283, 108, 5 , 284, 109, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (288, 109, 5 , 288, 110, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (293, 110, 5 , 293, 111, 18 , 266,  59, 10 ,  2 ),
            new BamDataBlock (298, 110, 5 , 299, 111, 18 , 277,  56, 11 ,  0 ),
            new BamDataBlock (303, 110, 5 , 304, 111, 19 , 285,  55, 12 ,  0 ),
            new BamDataBlock (308, 110, 4 , 307, 111, 20 , 296,  54, 13 ,  0 ),
            new BamDataBlock (309, 110, 3 , 312, 111, 20 , 304,  53, 14 ,  0 ),
            new BamDataBlock (310, 110, 3 , 313, 111, 20 , 214,   0, 10,  0 ),
            new BamDataBlock (311, 110, 3 , 314, 111, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (309, 110, 2 , 312, 111, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (304, 111, 2 , 307, 112, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (300, 110, 2 , 303, 111, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (296, 109, 2 , 299, 110, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (292, 108, 1 , 296, 109, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (285, 107, 2 , 289, 108, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (282, 107, 3 , 285, 108, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (278, 107, 4 , 277, 108, 18 , 214,   0, 10 ,  0 ),
            new BamDataBlock (279, 108, 4 , 278, 109, 18 , 252,  57, 10 ,  2 ),
            new BamDataBlock (281, 108, 5 , 283, 109, 18 , 265,  55, 11 ,  0 ),
            new BamDataBlock (284, 109, 5 , 285, 110, 19 , 277,  55, 12 ,  0 ),
            new BamDataBlock (287, 110, 5 , 288, 111, 20 , 288,  54, 13 ,  0 ),
            new BamDataBlock (289, 111, 5 , 290, 112, 20 , 299,  54, 14 ,  0 ),
            new BamDataBlock (291, 112, 4 , 290, 113, 20 , 214,   0, 10 ,  0 ),
            new BamDataBlock (293, 113, 3 , 295, 114, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (296, 114, 2 , 299, 115, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (295, 115, 2 , 298, 116, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (293, 116, 1 , 297, 117, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (289, 116, 1 , 292, 117, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (285, 115, 1 , 289, 116, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (281, 114, 1 , 284, 115, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (277, 113, 1 , 280, 114, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (274, 112, 1 , 277, 113, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (271, 111, 1 , 274, 112, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (267, 110, 1 , 270, 111, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (263, 109, 1 , 266, 110, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (260, 108, 1 , 263, 109, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (254, 108, 2 , 256, 109, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (252, 107, 3 , 254, 108, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (253, 108, 3 , 255, 109, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (255, 109, 3 , 257, 110, 18 , 231,  59, 10 ,  2 ),
            new BamDataBlock (258, 111, 3 , 260, 112, 18 , 242,  57, 11 ,  0 ),
            new BamDataBlock (263, 112, 4 , 262, 113, 19 , 256,  57, 12 ,  0 ),
            new BamDataBlock (270, 111, 4 , 269, 112, 20 , 267,  57, 13 ,  0 ),
            new BamDataBlock (274, 112, 5 , 276, 113, 20 , 281,  56, 14 ,  0 ),
            new BamDataBlock (280, 111, 6 , 282, 112, 19 , 214,   0, 10 ,  0 ),
            new BamDataBlock (284, 109, 6 , 285, 110, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (289, 108, 6 , 291, 109, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (294, 107, 6 , 296, 108, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (294, 107, 5 , 296, 108, 18 , 272,  57, 10 ,  2 ),
            new BamDataBlock (295, 107, 5 , 297, 108, 18 , 282,  57, 11 ,  0 ),
            new BamDataBlock (296, 108, 5 , 298, 109, 19 , 295,  57, 12 ,  0 ),
            new BamDataBlock (300, 108, 4 , 299, 109, 20 , 303,  57, 13 ,  0 ),
            new BamDataBlock (303, 108, 3 , 306, 109, 20 , 313,  57, 14 ,  0 ),
            new BamDataBlock (307, 109, 2 , 311, 110, 17 , 214,   0, 10 ,  0 ),
            new BamDataBlock (310, 110, 1 , 314, 111, 17 , 214,   0, 10 , 99 )
        };

        private static readonly BamDataBlock[] _fight1Data = {
            new BamDataBlock (75, 96, 1, 187, 96, -23, 58, 37, 46, 0),
            new BamDataBlock (75, 96, 2, 187, 96, -23, 58, 37, 46, 0),
            new BamDataBlock (75, 96, 3, 187, 96, -23, 58, 37, 46, 0),
            new BamDataBlock (75, 96, 4, 187, 96, -23, 58, 37, 46, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 58, 37, 46, 0),
            new BamDataBlock (75, 96, 2, 187, 96, -23, 58, 37, 46, 0),
            new BamDataBlock (75, 96, 3, 187, 96, -23, 58, 37, 46, 0),
            new BamDataBlock (75, 96, 4, 187, 96, -24, 58, 37, 46, 0),
            new BamDataBlock (79, 96, 1, 187, 96, -24, 58, 37, 46, 0),
            new BamDataBlock (85, 96, 2, 187, 96, -24, 58, 37, 46, 0),
            new BamDataBlock (94, 96, 3, 187, 96, -24, 58, 37, 46, 0),
            new BamDataBlock (100, 96, 4, 187, 96, -24, 58, 37, 46, 0),
            new BamDataBlock (113, 96, 1, 187, 96, -25, 58, 37, 46, 0),
            new BamDataBlock (121, 96, 1, 187, 96, -25, 58, 37, 46, 0),
            new BamDataBlock (136, 96, 16, 187, 96, -26, 58, 37, 46, 0),
            new BamDataBlock (151, 93, 6, 187, 96, -27, 58, 37, 46, 0),
            new BamDataBlock (159, 83, 16, 187, 96, -28, 58, 37, 46, 0),
            new BamDataBlock (170, 73, 16, 187, 96, -29, 182, 96, 48, 3),
            new BamDataBlock (176, 69, 13, 187, 96, -31, 182, 94, 49, 1),
            new BamDataBlock (168, 66, 13, 187, 98, -32, 182, 92, 50, 0),
            new BamDataBlock (155, 75, 13, 187, 96, -32, 182, 88, 51, 3),
            new BamDataBlock (145, 86, 13, 187, 98, -32, 182, 85, 52, 0),
            new BamDataBlock (127, 104, 13, 187, 98, -32, 182, 25, 52, 1),
            new BamDataBlock (122, 108, 13, 187, 98, -32, 182, 25, 52, 1),
            new BamDataBlock (120, 104, 14, 187, 96, -34, 107, 145, 42, 2),
            new BamDataBlock (111, 103, 13, 187, 96, -23, 107, 144, 43, 0),
            new BamDataBlock (102, 105, 13, 187, 96, -23, 107, 142, 43, 0),
            new BamDataBlock (97, 107, 13, 187, 96, -23, 107, 139, 44, 0),
            new BamDataBlock (92, 101, 14, 187, 96, -23, 107, 34, 47, 3),
            new BamDataBlock (90, 105, 14, 187, 96, -23, 107, 34, 47, 0),
            new BamDataBlock (88, 104, 14, 187, 96, -23, 107, 34, 47, 0),
            new BamDataBlock (87, 105, 14, 187, 96, -23, 107, 34, 47, 0),
            new BamDataBlock (86, 105, 14, 187, 96, -23, 107, 34, 47, 0),
            new BamDataBlock (86, 105, 14, 187, 96, -23, 107, 34, 47, 0),
            new BamDataBlock (86, 105, 15, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (85, 98, 16, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (92, 96, 1, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (92, 96, 1, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (89, 96, 4, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (86, 96, 3, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (83, 96, 2, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (81, 96, 1, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (78, 96, 4, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 3, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 99)
        };

        private static readonly BamDataBlock[] _fight2Data = {
            new BamDataBlock (  75, 96,  1 , 187, 96, -23 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  78, 96,  2 , 187, 96, -23 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  81, 96,  3 , 189, 96, -18 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  84, 96,  4 , 183, 96, -19 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  87, 96,  1 , 181, 96, -20 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  90, 96,  2 , 177, 96, -21 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  93, 96,  3 , 171, 96, -22 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  96, 96,  4 , 169, 96, -17 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  99, 96,  1 , 165, 96, -18 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 102, 96,  2 , 159, 96, -19 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 105, 96,  3 , 157, 96, -20 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 108, 96,  4 , 153, 96, -21 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 111, 96,  1 , 147, 96, -22 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 114, 96,  2 , 147, 96, -23 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 117, 96,  3 , 147, 96, -23 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 120, 96,  4 , 147, 96, -24 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 123, 96,  1 , 147, 96, -25 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 125, 96,  2 , 147, 96, -25 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 127, 96, 12 , 147, 96, -69 , 122,  94, 36 ,  3 ),
            new BamDataBlock ( 127, 95, 11 , 147, 96, -70 , 122,  94, 41 ,  0 ),
            new BamDataBlock ( 127, 96, 12 , 147, 96, -71 , 122, 100, 36 ,  3 ),
            new BamDataBlock ( 127, 97, 11 , 147, 96, -69 , 122, 100, 41 ,  0 ),
            new BamDataBlock ( 127, 96, 12 , 147, 96, -70 , 127, 103, 36 ,  3 ),
            new BamDataBlock ( 127, 95, 11 , 147, 96, -71 , 127, 103, 41 ,  0 ),
            new BamDataBlock ( 127, 94, 12 , 147, 96, -69 , 123,  94, 36 ,  3 ),
            new BamDataBlock ( 127, 95, 11 , 147, 96, -70 , 123,  94, 41 ,  0 ),
            new BamDataBlock ( 127, 96, 12 , 147, 96, -71 , 120,  99, 36 ,  3 ),
            new BamDataBlock ( 127, 96, 12 , 147, 96, -71 , 115,  98, 41 ,  0 ),
            new BamDataBlock ( 117, 93, 11 , 147, 96, -25 , 115, 134, 42 ,  0 ),
            new BamDataBlock ( 110, 92, 11 , 147, 96, -25 , 114, 133, 42 ,  0 ),
            new BamDataBlock ( 102, 93, 11 , 147, 96, -25 , 114, 131, 43 ,  0 ),
            new BamDataBlock (  92, 93, 11 , 147, 96, -25 , 114, 130, 43 ,  0 ),
            new BamDataBlock (  82, 94, 11 , 147, 96, -25 , 114, 128, 44 ,  0 ),
            new BamDataBlock (  76, 95, 11 , 147, 96, -25 , 114, 127, 44 ,  0 ),
            new BamDataBlock (  70, 96, 11 , 147, 96, -25 , 114, 126, 45 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 147, 96, -25 , 114, 125, 46 ,  1 ),
            new BamDataBlock (  75, 96,  6 , 147, 96, -25 , 114,  43, 46 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 147, 96, -25 , 114,  43, 46 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 147, 96, -25 , 114,  43, 46 ,  0 ),
            new BamDataBlock (  75, 96,  7 , 147, 96, -25 , 114,  43, 46 ,  0 ),
            new BamDataBlock (  75, 96, 68 , 147, 96, -25 , 114,  43, 46 ,  0 ),
            new BamDataBlock (  75, 96, 68 , 147, 96, -25 ,  89, 104, 36 ,  2 ),
            new BamDataBlock (  75, 96, 68 , 147, 96, -25 ,  94, 103, 62 ,  0 ),
            new BamDataBlock (  75, 96, 68 , 147, 96, -25 , 122, 103, 63 ,  0 ),
            new BamDataBlock (  75, 96, 68 , 147, 96, -25 , 141, 103, 64 ,  0 ),
            new BamDataBlock (  75, 96, 68 , 147, 96, -30 , 150, 103, 65 ,  3 ),
            new BamDataBlock (  75, 96, 68 , 156, 96, -30 , 160, 103, 66 ,  0 ),
            new BamDataBlock (  75, 96,  7 , 164, 96, -30 , 169, 103, 67 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 169, 96, -30 , 177, 103, 48 ,  3 ),
            new BamDataBlock (  75, 96,  5 , 173, 96, -30 , 185, 103, 49 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 178, 96, -30 , 198, 103, 50 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 181, 96, -30 , 207, 103, 51 ,  1 ),
            new BamDataBlock (  75, 96,  5 , 184, 96, -30 , 221, 103, 52 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 186, 96, -30 , 224,  53, 53 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 187, 96, -23 , 224,  53, 53 , 99 )
        };

        private static readonly BamDataBlock[] _fight3Data = {
            new BamDataBlock (  75, 96,  1 , 187,  96, -23 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  77, 96,  2 , 187,  96, -22 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  80, 96,  3 , 185,  96, -17 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  83, 96,  4 , 181,  96, -18 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  86, 96,  1 , 175,  96, -19 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  88, 96,  2 , 173,  96, -20 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  91, 96,  3 , 169,  96, -21 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  94, 96,  4 , 163,  96, -22 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  97, 96,  1 , 161,  96, -17 , 150,  45, 35 ,  0 ),
            new BamDataBlock (  99, 96,  2 , 157,  96, -18 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 102, 96,  3 , 151,  96, -19 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 105, 96,  4 , 149,  96, -20 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 108, 96,  1 , 145,  96, -21 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 110, 96,  2 , 145,  96, -25 , 150,  45, 35 ,  0 ),
            new BamDataBlock ( 113, 96,  3 , 145,  96, -26 , 132,  96, 36 ,  2 ),
            new BamDataBlock ( 117, 96,  7 , 145,  96, -27 , 122,  97, 36 ,  0 ),
            new BamDataBlock ( 117, 96,  7 , 145,  96, -28 , 117,  97, 37 ,  0 ),
            new BamDataBlock ( 116, 96, 12 , 145,  96, -24 , 110,  96, 38 ,  3 ),
            new BamDataBlock ( 109, 96, 12 , 145,  96, -24 , 103,  95, 39 ,  0 ),
            new BamDataBlock ( 105, 96, 12 , 145,  96, -24 ,  95,  90, 40 ,  1 ),
            new BamDataBlock (  96, 96, 11 , 145,  96, -24 ,  86,  80, 41 ,  0 ),
            new BamDataBlock (  92, 96, 11 , 145,  96, -24 ,  86,  80, 41 ,  0 ),
            new BamDataBlock (  93, 96,  5 , 145,  96, -24 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  91, 96,  5 , 145,  96, -24 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  89, 96,  5 , 145,  96, -24 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  88, 96,  5 , 145,  96, -24 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  6 , 145,  96, -24 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  6 , 145,  96, -23 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  6 , 145,  96, -23 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  5 , 145,  96, -23 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  5 , 145,  96, -23 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  6 , 145,  96, -23 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  6 , 145,  96, -23 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  5 , 145,  96, -23 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  5 , 145,  96, -23 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  6 , 145,  96, -23 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  6 , 145,  96, -26 ,  86,  38, 41 ,  0 ),
            new BamDataBlock (  87, 96,  6 , 145,  96, -27 , 132,  97, 36 ,  2 ),
            new BamDataBlock (  87, 96,  5 , 145,  96, -28 , 118,  97, 36 ,  0 ),
            new BamDataBlock (  87, 96,  7 , 145,  96, -24 , 107,  97, 36 ,  0 ),
            new BamDataBlock (  87, 96,  8 , 145,  96, -24 , 101,  97, 36 ,  0 ),
            new BamDataBlock (  87, 96,  9 , 145,  96, -23 , 102,  97, 66 ,  3 ),
            new BamDataBlock (  87, 96, 10 , 145,  96, -23 , 120,  97, 67 ,  0 ),
            new BamDataBlock (  87, 96, 10 , 145,  96, -30 , 139,  97, 67 ,  1 ),
            new BamDataBlock (  87, 96,  7 , 146,  96, -30 , 144,  97, 62 ,  2 ),
            new BamDataBlock (  86, 96,  4 , 160,  96, -30 , 144,  97, 48 ,  1 ),
            new BamDataBlock (  83, 96,  3 , 170,  96, -31 , 154,  93, 49 ,  0 ),
            new BamDataBlock (  80, 96,  2 , 174,  96, -31 , 161,  89, 50 ,  0 ),
            new BamDataBlock (  78, 96,  1 , 178,  99, -31 , 169,  85, 51 ,  0 ),
            new BamDataBlock (  75, 96,  4 , 183, 104, -31 , 175,  79, 52 ,  0 ),
            new BamDataBlock (  75, 96,  1 , 185,  99, -32 , 180, 144, 42 ,  3 ),
            new BamDataBlock (  75, 96,  1 , 185, 106, -31 , 181, 141, 42 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 185, 104, -31 , 181, 138, 43 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 188, 106, -31 , 182, 135, 43 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 191,  99, -32 , 183, 131, 44 ,  3 ),
            new BamDataBlock (  75, 96,  6 , 191,  99, -32 , 183, 127, 45 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 191,  99, -32 , 184, 121, 46 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 191,  99, -32 , 183, 115, 46 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 191,  99, -32 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 191,  99, -32 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 191,  99, -32 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 191,  99, -32 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 191,  99, -32 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 191,  99, -32 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 191,  99, -32 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 191,  99, -32 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 191,  99, -32 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 191,  99, -32 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 195,  98, -33 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 191,  96, -34 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 193,  96, -25 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 193,  96, -24 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 193,  96, -24 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  5 , 193,  96, -24 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 191,  96, -18 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 190,  96, -19 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  6 , 187,  96, -20 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  1 , 187,  96, -23 , 183,  41, 47 ,  0 ),
            new BamDataBlock (  75, 96,  1 , 187,  96, -23 , 183,  41, 47 , 99 )
        };

        private static readonly BamDataBlock[] _fight4Data = {
            new BamDataBlock (75, 96, 1, 187, 96, -23, 150, 45, 35, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 150, 45, 35, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 150, 45, 35, 0),
            new BamDataBlock (75, 96, 5, 187, 96, -23, 150, 45, 35, 0),
            new BamDataBlock (75, 96, 5, 187, 96, -23, 150, 45, 35, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -24, 150, 45, 35, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -24, 150, 45, 35, 0),
            new BamDataBlock (75, 96, 7, 187, 96, -24, 150, 45, 35, 0),
            new BamDataBlock (75, 96, 8, 187, 96, -25, 79, 101, 59, 0),
            new BamDataBlock (75, 96, 9, 187, 96, -25, 95, 104, 66, 0),
            new BamDataBlock (75, 96, 10, 187, 96, -25, 129, 104, 65, 0),
            new BamDataBlock (75, 96, 10, 187, 96, -25, 160, 104, 64, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -25, 179, 104, 63, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -23, 188, 104, 62, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -29, 191, 104, 36, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -29, 195, 104, 37, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -31, 202, 104, 38, 0),
            new BamDataBlock (75, 96, 5, 187, 96, -32, 210, 104, 39, 0),
            new BamDataBlock (75, 96, 5, 187, 98, -32, 216, 104, 40, 0),
            new BamDataBlock (75, 96, 5, 187, 96, -32, 223, 104, 42, 0),
            new BamDataBlock (75, 96, 5, 187, 98, -32, 223, 104, 42, 0),
            new BamDataBlock (75, 96, 5, 187, 97, -33, 223, 104, 42, 0),
            new BamDataBlock (75, 96, 5, 187, 96, -34, 223, 104, 42, 0),
            new BamDataBlock (75, 96, 5, 187, 96, -23, 223, 104, 42, 0),
            new BamDataBlock (75, 96, 5, 187, 96, -23, 223, 104, 42, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -23, 223, 104, 42, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -24, 223, 104, 42, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -24, 223, 104, 42, 0),
            new BamDataBlock (75, 96, 5, 187, 96, -25, 223, 104, 42, 0),
            new BamDataBlock (75, 96, 5, 187, 96, -25, 223, 104, 42, 0),
            new BamDataBlock (75, 96, 5, 187, 96, -26, 175, 98, 36, 0),
            new BamDataBlock (75, 96, 5, 187, 96, -26, 152, 98, 36, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -27, 124, 98, 37, 0),
            new BamDataBlock (75, 96, 6, 187, 96, -28, 105, 98, 38, 0),
            new BamDataBlock (75, 96, 11, 187, 96, -23, 77, 98, 39, 0),
            new BamDataBlock (75, 96, 13, 187, 96, -23, 63, 98, 40, 0),
            new BamDataBlock (75, 96, 14, 187, 96, -23, 51, 98, 41, 0),
            new BamDataBlock (75, 98, 14, 187, 96, -23, 51, 98, 42, 0),
            new BamDataBlock (75, 94, 14, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 98, 14, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 15, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 0),
            new BamDataBlock (75, 96, 1, 187, 96, -23, 0, 0, 0, 99)
        };

        public BamScene(QueenEngine vm)
        {
            _flag = BamFlags.F_STOP;
            _vm = vm;
            if (_vm.Resource.Platform == Platform.Amiga)
            {
                _fightData = _fight4Data;
            }
            else
            {
                _fightData = _fight1Data;
            }
        }

        public void LoadState(uint version, byte[] data, ref int ptr)
        {
            _flag = (BamFlags)data.ToUInt16BigEndian(ptr); ptr += 2;
        }

        public void SaveState(byte[] data, ref int ptr)
        {
            data.WriteInt16BigEndian(ptr, (short)_flag); ptr += 2;
        }

        public void UpdateCarAnimation()
        {
            if (_flag != BamFlags.F_STOP)
            {
                BamDataBlock bdb = _carData[_index];

                // Truck
                _obj1.CurPos(bdb.obj1.x, bdb.obj1.y);
                _obj1.frameNum = (ushort)(40 + bdb.obj1.frame);

                // Rico
                _obj2.CurPos(bdb.obj2.x, bdb.obj2.y);
                _obj2.frameNum = (ushort)(30 + bdb.obj2.frame);

                // FX
                _objfx.CurPos(bdb.fx.x, bdb.fx.y);
                _objfx.frameNum = (ushort)(41 + bdb.fx.frame);

                if (bdb.sfx < 0)
                {
                    _vm.Sound.PlaySong((short)-bdb.sfx);
                }

                if (bdb.sfx == 99)
                {
                    _lastSoundIndex = _index = 0;
                }
                else
                {
                    ++_index;
                }

                if (bdb.sfx == 2)
                {
                    PlaySfx();
                }
            }
        }

        private static readonly BamDataBlock[][] fightDataBlocks = {
            _fight1Data,
            _fight2Data,
            _fight3Data
        };

        public void UpdateFightAnimation()
        {

            if (_flag != BamFlags.F_STOP)
            {
                BamDataBlock bdb = _fightData[_index];

                // Frank
                _obj1.CurPos(bdb.obj1.x, bdb.obj1.y);
                _obj1.frameNum = (ushort)(40 + Math.Abs(bdb.obj1.frame));
                _obj1.xflip = (bdb.obj1.frame < 0);

                // Robot
                _obj2.CurPos(bdb.obj2.x, bdb.obj2.y);
                _obj2.frameNum = (ushort)(40 + Math.Abs(bdb.obj2.frame));
                _obj2.xflip = (bdb.obj2.frame < 0);

                // FX
                _objfx.CurPos(bdb.fx.x, bdb.fx.y);
                _objfx.frameNum = (ushort)(40 + Math.Abs(bdb.fx.frame));
                _objfx.xflip = (bdb.fx.frame < 0);

                if (bdb.sfx < 0)
                {
                    _vm.Sound.PlaySong((short)-bdb.sfx);
                }

                ++_index;
                switch (bdb.sfx)
                {
                    case 0: // nothing, so reset shaked screen if necessary
                        if (_screenShaked)
                        {
                            _vm.Display.Shake = true;
                            _screenShaked = false;
                        }
                        break;
                    case 1: // shake screen
                        _vm.Display.Shake = false;
                        _screenShaked = true;
                        break;
                    case 2: // play background sfx
                        PlaySfx();
                        break;
                    case 3: // play background sfx and shake screen
                        PlaySfx();
                        _vm.Display.Shake = false;
                        _screenShaked = true;
                        break;
                    case 99: // end of BAM data
                        _lastSoundIndex = _index = 0;
                        if (_vm.Resource.Platform == Platform.DOS)
                        {
                            _fightData = fightDataBlocks[_vm.Randomizer.Next(1 + 2)];
                        }
                        if (_flag == BamFlags.F_REQ_STOP)
                        {
                            _flag = BamFlags.F_STOP;
                        }
                        break;
                }
            }
        }

        public void PrepareAnimation()
        {
            _vm.Graphics.ClearBob(Graphics.BOB_OBJ1);
            _obj1 = _vm.Graphics.Bobs[Graphics.BOB_OBJ1];
            _obj1.active = true;

            _vm.Graphics.ClearBob(Graphics.BOB_OBJ2);
            _obj2 = _vm.Graphics.Bobs[Graphics.BOB_OBJ2];
            _obj2.active = true;

            _vm.Graphics.ClearBob(Graphics.BOB_FX);
            _objfx = _vm.Graphics.Bobs[Graphics.BOB_FX];
            _objfx.active = true;

            _index = 0;
            _lastSoundIndex = 0;
        }

        private void PlaySfx()
        {
            _vm.Sound.PlaySfx(_vm.Logic.CurrentRoomSfx);
            _lastSoundIndex = _index;
        }
    }

}

