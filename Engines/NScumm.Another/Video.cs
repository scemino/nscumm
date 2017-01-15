//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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
using NScumm.Core;
using NScumm.Core.Graphics;
using static NScumm.Core.DebugHelper;

namespace NScumm.Another
{
    internal class Video
    {
        public const int ScreenWidth = 320;
        public const int ScreenHeight = 200;
        public const int NumColors = 16;

        private const int VidPageSize = ScreenWidth * ScreenHeight / 2;

        // Special value when no palette change is necessary
        private const int NoPaletteChangeRequested = 0xFF;

        private static readonly byte[] Font =
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x10, 0x10, 0x10, 0x10, 0x00, 0x10, 0x00,
            0x28, 0x28, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x24, 0x7E, 0x24, 0x24, 0x7E, 0x24, 0x00,
            0x08, 0x3E, 0x48, 0x3C, 0x12, 0x7C, 0x10, 0x00, 0x42, 0xA4, 0x48, 0x10, 0x24, 0x4A, 0x84, 0x00,
            0x60, 0x90, 0x90, 0x70, 0x8A, 0x84, 0x7A, 0x00, 0x08, 0x08, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x06, 0x08, 0x10, 0x10, 0x10, 0x08, 0x06, 0x00, 0xC0, 0x20, 0x10, 0x10, 0x10, 0x20, 0xC0, 0x00,
            0x00, 0x44, 0x28, 0x10, 0x28, 0x44, 0x00, 0x00, 0x00, 0x10, 0x10, 0x7C, 0x10, 0x10, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x10, 0x20, 0x00, 0x00, 0x00, 0x7C, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x10, 0x28, 0x10, 0x00, 0x00, 0x04, 0x08, 0x10, 0x20, 0x40, 0x00, 0x00,
            0x78, 0x84, 0x8C, 0x94, 0xA4, 0xC4, 0x78, 0x00, 0x10, 0x30, 0x50, 0x10, 0x10, 0x10, 0x7C, 0x00,
            0x78, 0x84, 0x04, 0x08, 0x30, 0x40, 0xFC, 0x00, 0x78, 0x84, 0x04, 0x38, 0x04, 0x84, 0x78, 0x00,
            0x08, 0x18, 0x28, 0x48, 0xFC, 0x08, 0x08, 0x00, 0xFC, 0x80, 0xF8, 0x04, 0x04, 0x84, 0x78, 0x00,
            0x38, 0x40, 0x80, 0xF8, 0x84, 0x84, 0x78, 0x00, 0xFC, 0x04, 0x04, 0x08, 0x10, 0x20, 0x40, 0x00,
            0x78, 0x84, 0x84, 0x78, 0x84, 0x84, 0x78, 0x00, 0x78, 0x84, 0x84, 0x7C, 0x04, 0x08, 0x70, 0x00,
            0x00, 0x18, 0x18, 0x00, 0x00, 0x18, 0x18, 0x00, 0x00, 0x00, 0x18, 0x18, 0x00, 0x10, 0x10, 0x60,
            0x04, 0x08, 0x10, 0x20, 0x10, 0x08, 0x04, 0x00, 0x00, 0x00, 0xFE, 0x00, 0x00, 0xFE, 0x00, 0x00,
            0x20, 0x10, 0x08, 0x04, 0x08, 0x10, 0x20, 0x00, 0x7C, 0x82, 0x02, 0x0C, 0x10, 0x00, 0x10, 0x00,
            0x30, 0x18, 0x0C, 0x0C, 0x0C, 0x18, 0x30, 0x00, 0x78, 0x84, 0x84, 0xFC, 0x84, 0x84, 0x84, 0x00,
            0xF8, 0x84, 0x84, 0xF8, 0x84, 0x84, 0xF8, 0x00, 0x78, 0x84, 0x80, 0x80, 0x80, 0x84, 0x78, 0x00,
            0xF8, 0x84, 0x84, 0x84, 0x84, 0x84, 0xF8, 0x00, 0x7C, 0x40, 0x40, 0x78, 0x40, 0x40, 0x7C, 0x00,
            0xFC, 0x80, 0x80, 0xF0, 0x80, 0x80, 0x80, 0x00, 0x7C, 0x80, 0x80, 0x8C, 0x84, 0x84, 0x7C, 0x00,
            0x84, 0x84, 0x84, 0xFC, 0x84, 0x84, 0x84, 0x00, 0x7C, 0x10, 0x10, 0x10, 0x10, 0x10, 0x7C, 0x00,
            0x04, 0x04, 0x04, 0x04, 0x84, 0x84, 0x78, 0x00, 0x8C, 0x90, 0xA0, 0xE0, 0x90, 0x88, 0x84, 0x00,
            0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0xFC, 0x00, 0x82, 0xC6, 0xAA, 0x92, 0x82, 0x82, 0x82, 0x00,
            0x84, 0xC4, 0xA4, 0x94, 0x8C, 0x84, 0x84, 0x00, 0x78, 0x84, 0x84, 0x84, 0x84, 0x84, 0x78, 0x00,
            0xF8, 0x84, 0x84, 0xF8, 0x80, 0x80, 0x80, 0x00, 0x78, 0x84, 0x84, 0x84, 0x84, 0x8C, 0x7C, 0x03,
            0xF8, 0x84, 0x84, 0xF8, 0x90, 0x88, 0x84, 0x00, 0x78, 0x84, 0x80, 0x78, 0x04, 0x84, 0x78, 0x00,
            0x7C, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x00, 0x84, 0x84, 0x84, 0x84, 0x84, 0x84, 0x78, 0x00,
            0x84, 0x84, 0x84, 0x84, 0x84, 0x48, 0x30, 0x00, 0x82, 0x82, 0x82, 0x82, 0x92, 0xAA, 0xC6, 0x00,
            0x82, 0x44, 0x28, 0x10, 0x28, 0x44, 0x82, 0x00, 0x82, 0x44, 0x28, 0x10, 0x10, 0x10, 0x10, 0x00,
            0xFC, 0x04, 0x08, 0x10, 0x20, 0x40, 0xFC, 0x00, 0x3C, 0x30, 0x30, 0x30, 0x30, 0x30, 0x3C, 0x00,
            0x3C, 0x30, 0x30, 0x30, 0x30, 0x30, 0x3C, 0x00, 0x3C, 0x30, 0x30, 0x30, 0x30, 0x30, 0x3C, 0x00,
            0x3C, 0x30, 0x30, 0x30, 0x30, 0x30, 0x3C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFE,
            0x3C, 0x30, 0x30, 0x30, 0x30, 0x30, 0x3C, 0x00, 0x00, 0x00, 0x38, 0x04, 0x3C, 0x44, 0x3C, 0x00,
            0x40, 0x40, 0x78, 0x44, 0x44, 0x44, 0x78, 0x00, 0x00, 0x00, 0x3C, 0x40, 0x40, 0x40, 0x3C, 0x00,
            0x04, 0x04, 0x3C, 0x44, 0x44, 0x44, 0x3C, 0x00, 0x00, 0x00, 0x38, 0x44, 0x7C, 0x40, 0x3C, 0x00,
            0x38, 0x44, 0x40, 0x60, 0x40, 0x40, 0x40, 0x00, 0x00, 0x00, 0x3C, 0x44, 0x44, 0x3C, 0x04, 0x78,
            0x40, 0x40, 0x58, 0x64, 0x44, 0x44, 0x44, 0x00, 0x10, 0x00, 0x10, 0x10, 0x10, 0x10, 0x10, 0x00,
            0x02, 0x00, 0x02, 0x02, 0x02, 0x02, 0x42, 0x3C, 0x40, 0x40, 0x46, 0x48, 0x70, 0x48, 0x46, 0x00,
            0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x00, 0x00, 0x00, 0xEC, 0x92, 0x92, 0x92, 0x92, 0x00,
            0x00, 0x00, 0x78, 0x44, 0x44, 0x44, 0x44, 0x00, 0x00, 0x00, 0x38, 0x44, 0x44, 0x44, 0x38, 0x00,
            0x00, 0x00, 0x78, 0x44, 0x44, 0x78, 0x40, 0x40, 0x00, 0x00, 0x3C, 0x44, 0x44, 0x3C, 0x04, 0x04,
            0x00, 0x00, 0x4C, 0x70, 0x40, 0x40, 0x40, 0x00, 0x00, 0x00, 0x3C, 0x40, 0x38, 0x04, 0x78, 0x00,
            0x10, 0x10, 0x3C, 0x10, 0x10, 0x10, 0x0C, 0x00, 0x00, 0x00, 0x44, 0x44, 0x44, 0x44, 0x78, 0x00,
            0x00, 0x00, 0x44, 0x44, 0x44, 0x28, 0x10, 0x00, 0x00, 0x00, 0x82, 0x82, 0x92, 0xAA, 0xC6, 0x00,
            0x00, 0x00, 0x44, 0x28, 0x10, 0x28, 0x44, 0x00, 0x00, 0x00, 0x42, 0x22, 0x24, 0x18, 0x08, 0x30,
            0x00, 0x00, 0x7C, 0x08, 0x10, 0x20, 0x7C, 0x00, 0x60, 0x90, 0x20, 0x40, 0xF0, 0x00, 0x00, 0x00,
            0xFE, 0xFE, 0xFE, 0xFE, 0xFE, 0xFE, 0xFE, 0x00, 0x38, 0x44, 0xBA, 0xA2, 0xBA, 0x44, 0x38, 0x00,
            0x38, 0x44, 0x82, 0x82, 0x44, 0x28, 0xEE, 0x00, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA, 0x55, 0xAA
        };

        private static readonly Dictionary<int,string> StringsTableEng = new Dictionary<int, string>
        {
            {0x001, "P E A N U T  3000"},
            {0x002, "Copyright  } 1990 Peanut Computer, Inc.\nAll rights reserved.\n\nCDOS Version 5.01"},
            {0x003, "2"},
            {0x004, "3"},
            {0x005, "."},
            {0x006, "A"},
            {0x007, "@"},
            {0x008, "PEANUT 3000"},
            {0x00A, "R"},
            {0x00B, "U"},
            {0x00C, "N"},
            {0x00D, "P"},
            {0x00E, "R"},
            {0x00F, "O"},
            {0x010, "J"},
            {0x011, "E"},
            {0x012, "C"},
            {0x013, "T"},
            {0x014, "Shield 9A.5f Ok"},
            {0x015, "Flux % 5.0177 Ok"},
            {0x016, "CDI Vector ok"},
            {0x017, " %%%ddd ok"},
            {0x018, "Race-Track ok"},
            {0x019, "SYNCHROTRON"},
            {0x01A, "E: 23%\ng: .005\n\nRK: 77.2L\n\nopt: g+\n\n Shield:\n1: OFF\n2: ON\n3: ON\n\nP~: 1\n"},
            {0x01B, "ON"},
            {0x01C, "-"},
            {0x021, "|"},
            {0x022, "--- Theoretical study ---"},
            {0x023, " THE EXPERIMENT WILL BEGIN IN    SECONDS"},
            {0x024, "  20"},
            {0x025, "  19"},
            {0x026, "  18"},
            {0x027, "  4"},
            {0x028, "  3"},
            {0x029, "  2"},
            {0x02A, "  1"},
            {0x02B, "  0"},
            {0x02C, "L E T ' S   G O"},
            {0x031, "- Phase 0:\nINJECTION of particles\ninto synchrotron"},
            {0x032, "- Phase 1:\nParticle ACCELERATION."},
            {0x033, "- Phase 2:\nEJECTION of particles\non the shield."},
            {0x034, "A  N  A  L  Y  S  I  S"},
            {0x035, "- RESULT:\nProbability of creating:\n ANTIMATTER: 91.V %\n NEUTRINO 27:  0.04 %\n NEUTRINO 424: 18 %\n"},
            {0x036, "   Practical verification Y/N ?"},
            {0x037, "SURE ?"},
            {0x038, "MODIFICATION OF PARAMETERS\nRELATING TO PARTICLE\nACCELERATOR (SYNCHROTRON)."},
            {0x039, "       RUN EXPERIMENT ?"},
            {0x03C, "t---t"},
            {0x03D, "000 ~"},
            {0x03E, ".20x14dd"},
            {0x03F, "gj5r5r"},
            {0x040, "tilgor 25%"},
            {0x041, "12% 33% checked"},
            {0x042, "D=4.2158005584"},
            {0x043, "d=10.00001"},
            {0x044, "+"},
            {0x045, "*"},
            {0x046, "% 304"},
            {0x047, "gurgle 21"},
            {0x048, "{{{{"},
            {0x049, "Delphine Software"},
            {0x04A, "By Eric Chahi"},
            {0x04B, "  5"},
            {0x04C, "  17"},
            {0x12C, "0"},
            {0x12D, "1"},
            {0x12E, "2"},
            {0x12F, "3"},
            {0x130, "4"},
            {0x131, "5"},
            {0x132, "6"},
            {0x133, "7"},
            {0x134, "8"},
            {0x135, "9"},
            {0x136, "A"},
            {0x137, "B"},
            {0x138, "C"},
            {0x139, "D"},
            {0x13A, "E"},
            {0x13B, "F"},
            {0x13C, "        ACCESS CODE:"},
            {0x13D, "PRESS BUTTON OR RETURN TO CONTINUE"},
            {0x13E, "   ENTER ACCESS CODE"},
            {0x13F, "   INVALID PASSWORD !"},
            {0x140, "ANNULER"},
            {0x141, "      INSERT DISK ?\n\n\n\n\n\n\n\n\nPRESS ANY KEY TO CONTINUE"},
            {0x142, " SELECT SYMBOLS CORRESPONDING TO\n THE POSITION\n ON THE CODE WHEEL"},
            {0x143, "    LOADING..."},
            {0x144, "              ERROR"},
            {0x15E, "LDKD"},
            {0x15F, "HTDC"},
            {0x160, "CLLD"},
            {0x161, "FXLC"},
            {0x162, "KRFK"},
            {0x163, "XDDJ"},
            {0x164, "LBKG"},
            {0x165, "KLFB"},
            {0x166, "TTCT"},
            {0x167, "DDRX"},
            {0x168, "TBHK"},
            {0x169, "BRTD"},
            {0x16A, "CKJL"},
            {0x16B, "LFCK"},
            {0x16C, "BFLX"},
            {0x16D, "XJRT"},
            {0x16E, "HRTB"},
            {0x16F, "HBHK"},
            {0x170, "JCGB"},
            {0x171, "HHFL"},
            {0x172, "TFBB"},
            {0x173, "TXHF"},
            {0x174, "JHJL"},
            {0x181, " BY"},
            {0x182, "ERIC CHAHI"},
            {0x183, "         MUSIC AND SOUND EFFECTS"},
            {0x184, " "},
            {0x185, "JEAN-FRANCOIS FREITAS"},
            {0x186, "IBM PC VERSION"},
            {0x187, "      BY"},
            {0x188, " DANIEL MORAIS"},
            {0x18B, "       THEN PRESS FIRE"},
            {0x18C, " PUT THE PADDLE ON THE UPPER LEFT CORNER"},
            {0x18D, "PUT THE PADDLE IN CENTRAL POSITION"},
            {0x18E, "PUT THE PADDLE ON THE LOWER RIGHT CORNER"},
            {0x258, "      Designed by ..... Eric Chahi"},
            {0x259, "    Programmed by...... Eric Chahi"},
            {0x25A, "      Artwork ......... Eric Chahi"},
            {0x25B, "Music by ........ Jean-francois Freitas"},
            {0x25C, "            Sound effects"},
            {0x25D, "        Jean-Francois Freitas\n             Eric Chahi"},
            {0x263, "              Thanks To"},
            {0x264, "           Jesus Martinez\n\n          Daniel Morais\n\n        Frederic Savoir\n\n      Cecile Chahi\n\n    Philippe Delamarre\n\n  Philippe Ulrich\n\nSebastien Berthet\n\nPierre Gousseau"},
            {0x265, "Now Go Out Of This World"},
            {0x190, "Good evening professor."},
            {0x191, "I see you have driven here in your\nFerrari."},
            {0x192, "IDENTIFICATION"},
            {0x193, "Monsieur est en parfaite sante."},
            {0x194, "Y\n"},
            {0x195, "AU BOULOT !!!\n"}
        };

        /*private static readonly Dictionary<int,string> StringsTableDemo = new Dictionary<int, string>
        {
            {0x001, "P E A N U T  3000"},
            {0x002,
                "Copyright  } 1990 Peanut Computer, Inc.\nAll rights reserved.\n\nCDOS Version 5.01"},
            {0x003, "2"},
            {0x004, "3"},
            {0x005, "."},
            {0x006, "A"},
            {0x007, "@"},
            {0x008, "PEANUT 3000"},
            {0x00A, "R"},
            {0x00B, "U"},
            {0x00C, "N"},
            {0x00D, "P"},
            {0x00E, "R"},
            {0x00F, "O"},
            {0x010, "J"},
            {0x011, "E"},
            {0x012, "C"},
            {0x013, "T"},
            {0x014, "Shield 9A.5f Ok"},
            {0x015, "Flux % 5.0177 Ok"},
            {0x016, "CDI Vector ok"},
            {0x017, " %%%ddd ok"},
            {0x018, "Race-Track ok"},
            {0x019, "SYNCHROTRON"},
            {0x01A,
                "E: 23%\ng: .005\n\nRK: 77.2L\n\nopt: g+\n\n Shield:\n1: OFF\n2: ON\n3: ON\n\nP~: 1\n"},
            {0x01B, "ON"},
            {0x01C, "-"},
            {0x021, "|"},
            {0x022, "--- Theoretical study ---"},
            {0x023, " THE EXPERIMENT WILL BEGIN IN    SECONDS"},
            {0x024, "  20"},
            {0x025, "  19"},
            {0x026, "  18"},
            {0x027, "  4"},
            {0x028, "  3"},
            {0x029, "  2"},
            {0x02A, "  1"},
            {0x02B, "  0"},
            {0x02C, "L E T ' S   G O"},
            {0x031, "- Phase 0:\nINJECTION of particles\ninto synchrotron"},
            {0x032, "- Phase 1:\nParticle ACCELERATION."},
            {0x033, "- Phase 2:\nEJECTION of particles\non the shield."},
            {0x034, "A  N  A  L  Y  S  I  S"},
            {
                0x035,
                "- RESULT:\nProbability of creating:\n ANTIMATTER: 91.V %\n NEUTRINO 27:  0.04 %\n NEUTRINO 424: 18 %\n"
            },
            {0x036, "   Practical verification Y/N ?"},
            {0x037, "SURE ?"},
            {0x038, "MODIFICATION OF PARAMETERS\nRELATING TO PARTICLE\nACCELERATOR (SYNCHROTRON)."},
            {0x039, "       RUN EXPERIMENT ?"},
            {0x03C, "t---t"},
            {0x03D, "000 ~"},
            {0x03E, ".20x14dd"},
            {0x03F, "gj5r5r"},
            {0x040, "tilgor 25%"},
            {0x041, "12% 33% checked"},
            {0x042, "D=4.2158005584"},
            {0x043, "d=10.00001"},
            {0x044, "+"},
            {0x045, "*"},
            {0x046, "% 304"},
            {0x047, "gurgle 21"},
            {0x048, "{{{{"},
            {0x049, "Delphine Software"},
            {0x04A, "By Eric Chahi"},
            {0x04B, "  5"},
            {0x04C, "  17"},
            {0x12C, "0"},
            {0x12D, "1"},
            {0x12E, "2"},
            {0x12F, "3"},
            {0x130, "4"},
            {0x131, "5"},
            {0x132, "6"},
            {0x133, "7"},
            {0x134, "8"},
            {0x135, "9"},
            {0x136, "A"},
            {0x137, "B"},
            {0x138, "C"},
            {0x139, "D"},
            {0x13A, "E"},
            {0x13B, "F"},
            {0x13D, "PRESS BUTTON OR RETURN TO CONTINUE"},
            {0x13E, "   ENTER ACCESS CODE"},
            {0x13F, "   INVALID PASSWORD !"},
            {0x140, "ANNULER"},
            {0x141, "          INSERT DISK ?"},
            {0x142, " SELECT SYMBOLS CORRESPONDING TO\n THE POSITION\n ON THE CODE WHEEL"},
            {0x143, "    LOADING..."},
            {0x144, "              ERROR"},
            {0x181, " BY"},
            {0x182, "ERIC CHAHI"},
            {0x183, "         MUSIC AND SOUND EFFECTS"},
            {0x184, " "},
            {0x185, "JEAN-FRANCOIS FREITAS"},
            {0x186, "IBM PC VERSION"},
            {0x187, "      BY"},
            {0x188, " DANIEL MORAIS"},
            {0x18B, "       THEN PRESS FIRE"},
            {0x18C, " PUT THE PADDLE ON THE UPPER LEFT CORNER"},
            {0x18D, "PUT THE PADDLE IN CENTRAL POSITION"},
            {0x18E, "PUT THE PADDLE ON THE LOWER RIGHT CORNER"},
            {0x1F4, "Over Two Years in the Making"},
            {0x1F5, "   A New, State\nof the Art, Polygon\n  Graphics System"},
            {0x1F6, "   Comes to the\nComputer With Full\n Screen Graphics"},
            {0x1F7,
                "While conducting a nuclear fission\nexperiment at your local\nparticle accelerator ..."},
            {0x1F8, "Nature decides to put a little\n    extra spin on the ball"},
            {0x1F9, "And sends you ..."},
            {
                0x1FA,
                "     Out of this World\nA Cinematic Action Adventure\n Coming soon to a computer\n      screen near you\n from Interplay Productions\n   coming soon to the IBM"
            },
            {0x258, "      Designed by ..... Eric Chahi"},
            {0x259, "    Programmed by...... Eric Chahi"},
            {0x25A, "      Artwork ......... Eric Chahi"},
            {0x25B, "Music by ........ Jean-francois Freitas"},
            {0x25C, "            Sound effects"},
            {0x25D, "        Jean-Francois Freitas\n             Eric Chahi"},
            {0x263, "              Thanks To"},
            {
                0x264,
                "           Jesus Martinez\n\n          Daniel Morais\n\n        Frederic Savoir\n\n      Cecile Chahi\n\n    Philippe Delamarre\n\n  Philippe Ulrich\n\nSebastien Berthet\n\nPierre Gousseau"
            },
            {0x265, "Now Go Out Of This World"},
            {0x190, "Good evening professor."},
            {0x191, "I see you have driven here in your\nFerrari."},
            {0x192, "IDENTIFICATION"},
            {0x193, "Monsieur est en parfaite sante."},
            {0x194, "Y\n"},
            {0x195, "AU BOULOT !!!\n"},
        };*/

        public byte PaletteIdRequested;
        private byte _currentPaletteId;
        private readonly BytePtr[] _pagePtrs = new BytePtr[4];

        // I am almost sure that:
        // _curPagePtr1 is the backbuffer
        // _curPagePtr2 is the frontbuffer
        // _curPagePtr3 is the background builder.
        private BytePtr _curPagePtr1, _curPagePtr2, _curPagePtr3;

        private readonly Polygon _polygon = new Polygon();
        private short _hliney;

        //Precomputer division lookup table
        private readonly ushort[] _interpTable = new ushort[0x400];

        private BytePtr _pData;
        private BytePtr _dataBuf;
        private readonly IAnotherSystem _sys;

        private readonly Color[] _pal = new Color[NumColors];
        private readonly Resource _res;
        private byte _mask;

        public Video(Resource res, IAnotherSystem system)
        {
            _res = res;
            _sys = system;
        }

        public void Init()
        {
            PaletteIdRequested = NoPaletteChangeRequested;

            var tmp = new byte[4 * VidPageSize];

            /*
            for (int i = 0; i < 4; ++i) {
                _pagePtrs[i] = allocPage();
            }
            */
            for (var i = 0; i < 4; ++i)
            {
                _pagePtrs[i] = new BytePtr(tmp, i * VidPageSize);
            }

            _curPagePtr3 = GetPagePtr(1);
            _curPagePtr2 = GetPagePtr(2);


            ChangePagePtr1(0xFE);

            _interpTable[0] = 0x4000;

            for (var i = 1; i < 0x400; ++i)
            {
                _interpTable[i] = (ushort) (0x4000 / i);
            }
        }

        private BytePtr GetPagePtr(byte page)
        {
            BytePtr p;
            if (page <= 3)
            {
                p = _pagePtrs[page];
            }
            else
            {
                switch (page)
                {
                    case 0xFF:
                        p = _curPagePtr3;
                        break;
                    case 0xFE:
                        p = _curPagePtr2;
                        break;
                    default:
                        p = _pagePtrs[0]; // XXX check
                        Warning("Video::getPagePtr() p != [0,1,2,3,0xFF,0xFE] == 0x{0:X}", page);
                        break;
                }
            }
            return p;
        }

        public void ChangePagePtr1(byte page)
        {
            Debug(DebugLevels.DbgVideo, "Video::changePagePtr1({0})", page);
            _curPagePtr1 = GetPagePtr(page);
        }

        public void CopyPagePtr(BytePtr src)
        {
            Debug(DebugLevels.DbgVideo, "Video::copyPagePtr()");
            var dst = _pagePtrs[0];
            var h = 200;
            while (h-- != 0)
            {
                var w = 40;
                while (w-- != 0)
                {
                    var p = new[]
                    {
                        src[8000 * 3],
                        src[8000 * 2],
                        src[8000 * 1],
                        src[8000 * 0]
                    };
                    for (var j = 0; j < 4; ++j)
                    {
                        byte acc = 0;
                        for (var i = 0; i < 8; ++i)
                        {
                            acc <<= 1;
                            acc = (byte) (acc | (((p[i & 3] & 0x80) != 0) ? 1 : 0));
                            p[i & 3] <<= 1;
                        }
                        dst.Value = acc;
                        dst.Offset++;
                    }
                    ++src.Offset;
                }
            }
        }

        public void SetDataBuffer(BytePtr dataBuf, ushort offset)
        {
            _dataBuf = dataBuf;
            _pData = dataBuf + offset;
        }

        /// <summary>
        /// A shape can be given in two different ways:
        /// - A list of screenspace vertices.
        /// - A list of objectspace vertices, based on a delta from the first vertex.
        /// This is a recursive function.
        /// </summary>
        /// <param name="color"></param>
        /// <param name="zoom"></param>
        /// <param name="pt"></param>
        public void ReadAndDrawPolygon(byte color, ushort zoom, Point pt)
        {
            var i = FetchByte();

            //This is
            if (i >= 0xC0)
            {
                // 0xc0 = 192

                // WTF ?
                if ((color & 0x80) != 0)
                {
                    //0x80 = 128 (1000 0000)
                    color = (byte) (i & 0x3F); //0x3F =  63 (0011 1111)
                }

                // pc is misleading here since we are not reading bytecode but only
                // vertices informations.
                _polygon.ReadVertices(_pData, zoom);

                FillPolygon(color, pt);
            }
            else
            {
                i &= 0x3F; //0x3F = 63
                if (i == 1)
                {
                    Warning("Video::ReadAndDrawPolygon() ec=0x{0:X} (i != 2)\n", 0xF80);
                }
                else if (i == 2)
                {
                    ReadAndDrawPolygonHierarchy(zoom, pt);
                }
                else
                {
                    Warning("Video::ReadAndDrawPolygon() ec=0x{0:X} (i != 2)\n", 0xFBB);
                }
            }
        }

        /// <summary>
        /// What is read from the bytecode is not a pure screnspace polygon but a polygonspace polygon.
        /// </summary>
        /// <param name="zoom"></param>
        /// <param name="pgc"></param>
        private void ReadAndDrawPolygonHierarchy(ushort zoom, Point pgc)
        {
            var pt = pgc;
            pt.X = (short) (pt.X - FetchByte() * zoom / 64);
            pt.Y = (short) (pt.Y - FetchByte() * zoom / 64);

            short childs = FetchByte();
            Debug(DebugLevels.DbgVideo, "Video::readAndDrawPolygonHierarchy childs={0}", childs);

            for (; childs >= 0; --childs)
            {
                var off = FetchWord();

                var po = pt;
                po.X = (short) (po.X + FetchByte() * zoom / 64);
                po.Y = (short) (po.Y + FetchByte() * zoom / 64);

                ushort color = 0xFF;
                var bp = off;
                off &= 0x7FFF;

                if ((bp & 0x8000) != 0)
                {
                    color = (ushort) (_pData.Value & 0x7F);
                    _pData += 2;
                }

                var bak = _pData;
                _pData = _dataBuf + off * 2;


                ReadAndDrawPolygon((byte) color, zoom, po);

                _pData = bak;
            }
        }

        private ushort FetchWord()
        {
            var value = _pData.ToUInt16BigEndian();
            _pData += 2;
            return value;
        }

        private void FillPolygon(byte color, Point pt)
        {
            if (_polygon.Bbw == 0 && _polygon.Bbh == 1 && _polygon.NumPoints == 4)
            {
                DrawPoint(color, pt.X, pt.Y);
                return;
            }

            var x1 = (short) (pt.X - _polygon.Bbw / 2);
            var x2 = (short) (pt.X + _polygon.Bbw / 2);
            var y1 = (short) (pt.Y - _polygon.Bbh / 2);
            var y2 = (short) (pt.Y + _polygon.Bbh / 2);

            if (x1 > 319 || x2 < 0 || y1 > 199 || y2 < 0)
                return;

            _hliney = y1;

            ushort i, j;
            i = 0;
            j = (ushort) (_polygon.NumPoints - 1);

            x2 = (short) (_polygon.Points[i].X + x1);
            x1 = (short) (_polygon.Points[j].X + x1);

            ++i;
            --j;

            Action<short, short, byte> drawFct;
            if (color < 0x10)
            {
                drawFct = DrawLineN;
            }
            else if (color > 0x10)
            {
                drawFct = DrawLineP;
            }
            else
            {
                drawFct = DrawLineBlend;
            }

            var cpt1 = (uint) (x1 << 16);
            var cpt2 = (uint) (x2 << 16);

            while (true)
            {
                _polygon.NumPoints -= 2;
                if (_polygon.NumPoints == 0)
                {
#if TRACE_FRAMEBUFFER
            				dumpFrameBuffers("fillPolygonEnd");
            		#endif
#if TRACE_BG_BUFFER
            			dumpBackGroundBuffer();
            #endif
                    break;
                }
                ushort h;
                var step1 = CalcStep(_polygon.Points[j + 1], _polygon.Points[j], out h);
                var step2 = CalcStep(_polygon.Points[i - 1], _polygon.Points[i], out h);

                ++i;
                --j;

                cpt1 = (cpt1 & 0xFFFF0000) | 0x7FFF;
                cpt2 = (cpt2 & 0xFFFF0000) | 0x8000;

                if (h == 0)
                {
                    cpt1 = (uint) (cpt1 + step1);
                    cpt2 = (uint) (cpt2 + step2);
                }
                else
                {
                    for (; h != 0; --h)
                    {
                        if (_hliney >= 0)
                        {
                            x1 = (short) (cpt1 >> 16);
                            x2 = (short) (cpt2 >> 16);
                            if (x1 <= 319 && x2 >= 0)
                            {
                                if (x1 < 0) x1 = 0;
                                if (x2 > 319) x2 = 319;
                                drawFct(x1, x2, color);
                            }
                        }
                        cpt1 = (uint) (cpt1 + step1);
                        cpt2 = (uint) (cpt2 + step2);
                        ++_hliney;
                        if (_hliney > 199) return;
                    }
                }

#if TRACE_FRAMEBUFFER
            				DumpFrameBuffers("fillPolygonChild");
#endif
#if TRACE_BG_BUFFER

            			DumpBackGroundBuffer();
#endif
            }
        }

        private int CalcStep(Point p1, Point p2, out ushort dy)
        {
            dy = (ushort) (p2.Y - p1.Y);
            return (p2.X - p1.X) * _interpTable[dy] * 4;
        }

        private void DrawPoint(byte color, short x, short y)
        {
            Debug(DebugLevels.DbgVideo, "DrawPoint({0}, {1}, {2})", color, x, y);
            if (x >= 0 && x <= 319 && y >= 0 && y <= 199)
            {
                var off = (ushort) (y * 160 + x / 2);

                byte cmasko, cmaskn;
                if ((x & 1) != 0)
                {
                    cmaskn = 0x0F;
                    cmasko = 0xF0;
                }
                else
                {
                    cmaskn = 0xF0;
                    cmasko = 0x0F;
                }

                var colb = (byte) ((color << 4) | color);
                if (color == 0x10)
                {
                    cmaskn &= 0x88;
                    cmasko = (byte) ~cmaskn;
                    colb = 0x88;
                }
                else if (color == 0x11)
                {
                    colb = (_pagePtrs[0] + off).Value;
                }
                var b = (_curPagePtr1 + off).Value;
                _curPagePtr1[off] = (byte) ((b & cmasko) | (colb & cmaskn));
            }
        }

        private void DrawLineBlend(short x1, short x2, byte color)
        {
            Debug(DebugLevels.DbgVideo, "drawLineBlend({0}, {1}, {2})", x1, x2, color);
            var xmax = Math.Max(x1, x2);
            var xmin = Math.Min(x1, x2);
            var p = _curPagePtr1 + _hliney * 160 + xmin / 2;

            var w = (ushort) (xmax / 2 - xmin / 2 + 1);
            byte cmaske = 0;
            byte cmasks = 0;
            if ((xmin & 1) != 0)
            {
                --w;
                cmasks = 0xF7;
            }
            if ((xmax & 1) == 0)
            {
                --w;
                cmaske = 0x7F;
            }

            if (cmasks != 0)
            {
                p.Value = (byte) ((p.Value & cmasks) | 0x08);
                ++p.Offset;
            }
            while (w-- != 0)
            {
                p.Value = (byte) ((p.Value & 0x77) | 0x88);
                ++p.Offset;
            }
            if (cmaske != 0)
            {
                p.Value = (byte) ((p.Value & cmaske) | 0x80);
                ++p.Offset;
            }
        }

        private void DrawLineP(short x1, short x2, byte color)
        {
            Debug(DebugLevels.DbgVideo, "drawLineP({0}, {1}, {2})", x1, x2, color);
            var xmax = Math.Max(x1, x2);
            var xmin = Math.Min(x1, x2);
            var off = (ushort) (_hliney * 160 + xmin / 2);
            var p = _curPagePtr1 + off;
            var q = _pagePtrs[0] + off;

            var w = (byte) (xmax / 2 - xmin / 2 + 1);
            byte cmaske = 0;
            byte cmasks = 0;
            if ((xmin & 1) != 0)
            {
                --w;
                cmasks = 0xF0;
            }
            if ((xmax & 1) == 0)
            {
                --w;
                cmaske = 0x0F;
            }

            if (cmasks != 0)
            {
                p.Value = (byte) ((p.Value & cmasks) | (q.Value & 0x0F));
                ++p.Offset;
                ++q.Offset;
            }
            while (w-- != 0)
            {
                p.Value = q.Value;
                p.Offset++;
                q.Offset++;
            }
            if (cmaske != 0)
            {
                p.Value = (byte) ((p.Value & cmaske) | (q.Value & 0xF0));
                ++p.Offset;
                ++q.Offset;
            }
        }

        private void DrawLineN(short x1, short x2, byte color)
        {
            Debug(DebugLevels.DbgVideo, "drawLineN({0}, {1}, {2})", x1, x2, color);
            var xmax = Math.Max(x1, x2);
            var xmin = Math.Min(x1, x2);
            var p = _curPagePtr1 + _hliney * 160 + xmin / 2;

            var w = (ushort) (xmax / 2 - xmin / 2 + 1);
            byte cmaske = 0;
            byte cmasks = 0;
            if ((xmin & 1) != 0)
            {
                --w;
                cmasks = 0xF0;
            }
            if ((xmax & 1) == 0)
            {
                --w;
                cmaske = 0x0F;
            }

            var colb = (byte) (((color & 0xF) << 4) | (color & 0xF));
            if (cmasks != 0)
            {
                p.Value = (byte) ((p.Value & cmasks) | (colb & 0x0F));
                ++p.Offset;
            }
            while (w-- != 0)
            {
                p.Value = colb;
                p.Offset++;
            }
            if (cmaske != 0)
            {
                p.Value = (byte) ((p.Value & cmaske) | (colb & 0xF0));
                ++p.Offset;
            }
        }

        private byte FetchByte()
        {
            var value = _pData.Value;
            _pData.Offset++;
            return value;
        }

        public void FillPage(byte pageId, byte color)
        {
            Debug(DebugLevels.DbgVideo, "Video::fillPage({0}, {1})", pageId, color);
            var p = GetPagePtr(pageId);

            // Since a palette indice is coded on 4 bits, we need to duplicate the
            // clearing color to the upper part of the byte.
            var c = (byte) ((color << 4) | color);

            p.Data.Set(p.Offset, c, VidPageSize);

#if TRACE_FRAMEBUFFER
			DumpFrameBuffers("-fillPage");
#endif
#if TRACE_BG_BUFFER

			DumpBackGroundBuffer();
#endif
        }

        public void CopyPage(byte srcPageId, byte dstPageId, short vscroll)
        {
            Debug(DebugLevels.DbgVideo, "Video::copyPage({0}, {1})", srcPageId, dstPageId);

            if (srcPageId == dstPageId)
                return;

            BytePtr p;
            BytePtr q;

            if (srcPageId >= 0xFE || ((srcPageId &= 0xBF) & 0x80) == 0)
            {
                p = GetPagePtr(srcPageId);
                q = GetPagePtr(dstPageId);
                p.Copy(q, VidPageSize);
            }
            else
            {
                p = GetPagePtr((byte) (srcPageId & 3));
                q = GetPagePtr(dstPageId);
                if (vscroll >= -199 && vscroll <= 199)
                {
                    ushort h = 200;
                    if (vscroll < 0)
                    {
                        h = (ushort) (h + vscroll);
                        p = p - vscroll * 160;
                    }
                    else
                    {
                        h = (ushort) (h - vscroll);
                        q += vscroll * 160;
                    }
                    p.Copy(q, h * 160);
                }
            }


#if TRACE_FRAMEBUFFER
	char name[256];
	memset(name,0,sizeof(name));
	sprintf(name,"copyPage_0x{0:X}_to_0x{1:X}",(p-_pagePtrs[0])/VID_PAGE_SIZE,(q-_pagePtrs[0])/VID_PAGE_SIZE);
	dumpFrameBuffers(name);
	#endif
        }

        public void DrawString(ushort color, ushort x, ushort y, ushort stringId)
        {

            //Not found
            if (!StringsTableEng.ContainsKey(stringId))
                return;

            //Search for the location where the string is located.
            var se = StringsTableEng[stringId];
            Debug(DebugLevels.DbgVideo, "DrawString({0}, {1}, {2}, '{3}')", color, x, y, se);

            //Used if the string contains a return carriage.
            var xOrigin = x;
            var len = se.Length;
            for (var i = 0; i < len; ++i)
            {
                if (se[i] == '\n')
                {
                    y += 8;
                    x = xOrigin;
                    continue;
                }

                DrawChar(se[i], x, y, (byte) color, _curPagePtr1);
                x++;
            }
        }

        private static void DrawChar(char character, ushort x, ushort y, byte color, BytePtr buf)
        {
            if (x > 39 || y > 192) return;

            var ft = new BytePtr(Font, (character - ' ') * 8);
            var p = buf + x * 4 + y * 160;

            for (var j = 0; j < 8; ++j)
            {
                var ch = (ft + j).Value;
                for (var i = 0; i < 4; ++i)
                {
                    var b = (p + i).Value;
                    byte cmask = 0xFF;
                    byte colb = 0;
                    if ((ch & 0x80) != 0)
                    {
                        colb = (byte) (colb | color << 4);
                        cmask &= 0x0F;
                    }
                    ch <<= 1;
                    if ((ch & 0x80) != 0)
                    {
                        colb |= color;
                        cmask &= 0xF0;
                    }
                    ch <<= 1;
                    p[i] = (byte) ((b & cmask) | colb);
                }
                p += 160;
            }
        }

        public void UpdateDisplay(byte pageId)
        {
            Debug(DebugLevels.DbgVideo, "Video::updateDisplay({0})", pageId);

            if (pageId != 0xFE)
            {
                if (pageId == 0xFF)
                {
                    ScummHelper.Swap(ref _curPagePtr2, ref _curPagePtr3);
                }
                else
                {
                    _curPagePtr2 = GetPagePtr(pageId);
                }
            }

            //Check if we need to change the palette
            if (PaletteIdRequested != NoPaletteChangeRequested)
            {
                ChangePal(PaletteIdRequested);
                PaletteIdRequested = NoPaletteChangeRequested;
            }

            //Q: Why 160 ?
            //A: Because one byte gives two palette indices so
            //   we only need to move 320/2 per line.
            _sys.CopyRect(0, 0, ScreenWidth, ScreenHeight, _curPagePtr2);

#if TRACE_FRAMEBUFFER
	      dumpFrameBuffer(_curPagePtr2,allFrameBuffers,320,200);
#endif
        }

        private void ChangePal(byte palNum)
        {
            if (palNum >= 32)
                return;

            //colors are coded on 2bytes (565) for 16 colors = 32
            var p = _res.SegPalettes + palNum * 32;

            // Moved to the heap, legacy code used to allocate the palette
            // on the stack.
            //byte pal[NUM_COLORS * 3]; //3 = BYTES_PER_PIXEL

            for (var i = 0; i < NumColors; ++i)
            {
                var c1 = p[0];
                var c2 = p[1];
                p += 2;
                _pal[i] = Color.FromRgb(
                    (((c1 & 0x0F) << 2) | ((c1 & 0x0F) >> 2)) << 2, // r
                    (((c2 & 0xF0) >> 2) | ((c2 & 0xF0) >> 6)) << 2, // g
                    (((c2 & 0x0F) >> 2) | ((c2 & 0x0F) << 2)) << 2); // b
            }

            _sys.SetPalette(0, NumColors, _pal);
            _currentPaletteId = palNum;

#if TRACE_PALETTE
            	DebugN("byte[] dumpPalette=new [] = {{\n");
            	for (int i = 0; i < NumColors; ++i)
            	{
            		DebugN("0x{0:X},0x{1:X},0x{2:X},",_pal[i].R,_pal[i].G,_pal[i].B);
            	}
            	DebugN("\n}};\n");
#endif


#if TRACE_FRAMEBUFFER
            	dumpPaletteCursor++;
#endif
        }

        public void SaveOrLoad(Serializer ser)
        {
            _mask = 0;
            if (ser.Mode == Mode.SmSave)
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (_pagePtrs[i] == _curPagePtr1)
                        _mask = (byte) (_mask | i << 4);
                    if (_pagePtrs[i] == _curPagePtr2)
                        _mask = (byte) (_mask | i << 2);
                    if (_pagePtrs[i] == _curPagePtr3)
                        _mask = (byte) (_mask | i << 0);
                }
            }
            Entry[] entries =
            {
                Entry.Create(this, o => o._currentPaletteId, 1),
                Entry.Create(this, o => o.PaletteIdRequested, 1),
                Entry.Create(this, o => o._mask, 1),
                Entry.Create(_pagePtrs[0], VidPageSize, 1),
                Entry.Create(_pagePtrs[1], VidPageSize, 1),
                Entry.Create(_pagePtrs[2], VidPageSize, 1),
                Entry.Create(_pagePtrs[3], VidPageSize, 1),
            };
            ser.SaveOrLoadEntries(entries);

            if (ser.Mode == Mode.SmLoad)
            {
                _curPagePtr1 = _pagePtrs[(_mask >> 4) & 0x3];
                _curPagePtr2 = _pagePtrs[(_mask >> 2) & 0x3];
                _curPagePtr3 = _pagePtrs[(_mask >> 0) & 0x3];
                ChangePal(_currentPaletteId);
            }
        }
    }
}