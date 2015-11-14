/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using NScumm.Core.Graphics;

namespace NScumm.Scumm.Graphics
{
    public class Palette
    {
        public static Palette Ega { get { return egaPalette ?? (egaPalette = CreatePalette(tableEGAPalette)); } }

        public static Palette Cga { get { return cgaPalette ?? (cgaPalette = CreatePalette(tableCGAPalette)); } }

        public static Palette V1 { get { return v1Palette ?? (v1Palette = CreatePalette(tableV1Palette)); } }

        public static Palette C64 { get { return c64Palette ?? (c64Palette = CreatePalette(tableC64Palette)); } }

        public static Palette Amiga { get { return amigaPalette ?? (amigaPalette = CreatePalette(tableAmigaPalette)); } }

        public static Palette AmigaMonkeyIsland { get { return amigaMonkeyIslandPalette ?? (amigaMonkeyIslandPalette = CreatePalette(tableAmigaMonkeyIslandPalette)); } }

        public static Palette Towns3 { get { return towns3Palette ?? (towns3Palette = CreatePalette(tableTowns3Palette)); } }

        public static Palette TownsLoom { get { return townsLoomPalette ?? (townsLoomPalette = CreatePalette(tableTownsLoomPalette)); } }

        public Color[] Colors { get; private set; }

        public Palette()
        {
            Colors = new Color[256];
        }

        public Palette(Color[] colors)
        {
            if (colors.Length != 256)
                throw new ArgumentException("Palette must have 256 colors.");
            Colors = colors;
        }

        static Palette CreatePalette(Color[] colors)
        {
            if (colors.Length != 16 && colors.Length != 17)
                throw new ArgumentException("16 or 17 colors was expected.");

            var palette = new Palette();
            Array.Copy(colors, palette.Colors, colors.Length);
            return palette;
        }

        readonly static Color[] tableEGAPalette =
            {
                Color.FromRgb(0x00, 0x00, 0x00), Color.FromRgb(0x00, 0x00, 0xAA), Color.FromRgb(0x00, 0xAA, 0x00), Color.FromRgb(0x00, 0xAA, 0xAA),
                Color.FromRgb(0xAA, 0x00, 0x00), Color.FromRgb(0xAA, 0x00, 0xAA), Color.FromRgb(0xAA, 0x55, 0x00), Color.FromRgb(0xAA, 0xAA, 0xAA),
                Color.FromRgb(0x55, 0x55, 0x55), Color.FromRgb(0x55, 0x55, 0xFF), Color.FromRgb(0x55, 0xFF, 0x55), Color.FromRgb(0x55, 0xFF, 0xFF),
                Color.FromRgb(0xFF, 0x55, 0x55), Color.FromRgb(0xFF, 0x55, 0xFF), Color.FromRgb(0xFF, 0xFF, 0x55), Color.FromRgb(0xFF, 0xFF, 0xFF)
            };

        readonly static Color[] tableCGAPalette =
            {
                Color.FromRgb(0x00, 0x00, 0x00), Color.FromRgb(0x00, 0xA8, 0xA8),   
                Color.FromRgb(0xA8, 0x00, 0xA8), Color.FromRgb(0xA8, 0xA8, 0xA8)
            };

        readonly static Color[] tableV1Palette =
            {
                Color.FromRgb(0x00, 0x00, 0x00),   Color.FromRgb(0xFF, 0xFF, 0xFF),   Color.FromRgb(0xAA, 0x00, 0x00),   Color.FromRgb(0x00, 0xAA, 0xAA),
                Color.FromRgb(0xAA, 0x00, 0xAA),   Color.FromRgb(0x00, 0xAA, 0x00),   Color.FromRgb(0x00, 0x00, 0xAA),   Color.FromRgb(0xFF, 0xFF, 0x55),
                Color.FromRgb(0xFF, 0x55, 0x55),   Color.FromRgb(0xAA, 0x55, 0x00),   Color.FromRgb(0xFF, 0x55, 0x55),   Color.FromRgb(0x55, 0x55, 0x55),
                Color.FromRgb(0xAA, 0xAA, 0xAA),   Color.FromRgb(0x55, 0xFF, 0x55),   Color.FromRgb(0x55, 0x55, 0xFF),   Color.FromRgb(0x55, 0x55, 0x55),
                Color.FromRgb(0xFF, 0x55, 0xFF)
            };

        readonly static Color[] tableC64Palette =
            {
                Color.FromRgb(0x00, 0x00, 0x00),   Color.FromRgb(0xFF, 0xFF, 0xFF),  Color.FromRgb(0x7E, 0x35, 0x2B),   Color.FromRgb(0x6E, 0xB7, 0xC1),
                Color.FromRgb(0x7F, 0x3B, 0xA6),   Color.FromRgb(0x5C, 0xA0, 0x35),   Color.FromRgb(0x33, 0x27, 0x99),   Color.FromRgb(0xCB, 0xD7, 0x65),
                Color.FromRgb(0x85, 0x53, 0x1C),   Color.FromRgb(0x50, 0x3C, 0x00),   Color.FromRgb(0xB4, 0x6B, 0x61),   Color.FromRgb(0x4A, 0x4A, 0x4A),
                Color.FromRgb(0x75, 0x75, 0x75),   Color.FromRgb(0xA3, 0xE7, 0x7C),   Color.FromRgb(0x70, 0x64, 0xD6),   Color.FromRgb(0xA3, 0xA3, 0xA3),
                // Use 17 color table for v1 games to allow correct color for inventory and
                // sentence line. Original games used some kind of dynamic color table
                // remapping between rooms.
                Color.FromRgb(0x7F, 0x3B, 0xA6)
            };

        readonly static Color[] tableAmigaPalette =
            {
                Color.FromRgb(0x00, 0x00, 0x00),   Color.FromRgb(0x00, 0x00, 0xBB),   Color.FromRgb(0x00, 0xBB, 0x00),   Color.FromRgb(0x00, 0xBB, 0xBB),
                Color.FromRgb(0xBB, 0x00, 0x00),   Color.FromRgb(0xBB, 0x00, 0xBB),   Color.FromRgb(0xBB, 0x77, 0x00),   Color.FromRgb(0xBB, 0xBB, 0xBB),
                Color.FromRgb(0x77, 0x77, 0x77),   Color.FromRgb(0x77, 0x77, 0xFF),   Color.FromRgb(0x00, 0xFF, 0x00),   Color.FromRgb(0x00, 0xFF, 0xFF),
                Color.FromRgb(0xFF, 0x88, 0x88),   Color.FromRgb(0xFF, 0x00, 0xFF),   Color.FromRgb(0xFF, 0xFF, 0x00),   Color.FromRgb(0xFF, 0xFF, 0xFF)
            };

        readonly static Color[] tableAmigaMonkeyIslandPalette =
            {
                Color.FromRgb(0x00, 0x00, 0x00),   Color.FromRgb(0x00, 0x00, 0xAA),   Color.FromRgb(0x00, 0x88, 0x22),   Color.FromRgb(0x00, 0x66, 0x77),
                Color.FromRgb(0xBB, 0x66, 0x66),   Color.FromRgb(0xAA, 0x22, 0xAA),   Color.FromRgb(0x88, 0x55, 0x22),   Color.FromRgb(0x77, 0x77, 0x77),
                Color.FromRgb(0x33, 0x33, 0x33),   Color.FromRgb(0x22, 0x55, 0xDD),   Color.FromRgb(0x22, 0xDD, 0x44),   Color.FromRgb(0x00, 0xCC, 0xFF),
                Color.FromRgb(0xFF, 0x99, 0x99),   Color.FromRgb(0xFF, 0x55, 0xFF),   Color.FromRgb(0xFF, 0xFF, 0x77),   Color.FromRgb(0xFF, 0xFF, 0xFF)
            };

        static readonly Color[] tableTowns3Palette =
            {
                Color.FromRgb(0x00, 0x00, 0x00),   Color.FromRgb(0x00, 0x00, 0xA0), Color.FromRgb(0x00, 0xA0, 0x00), Color.FromRgb(0x00, 0xA0, 0xA0),
                Color.FromRgb(0xA0, 0x00, 0x00),   Color.FromRgb(0xA0, 0x00, 0xA0), Color.FromRgb(0xA0, 0x60, 0x00), Color.FromRgb(0xA0, 0xA0, 0xA0),
                Color.FromRgb(0x60, 0x60, 0x60),   Color.FromRgb(0x60, 0x60, 0xE0), Color.FromRgb(0x00, 0xE0, 0x00), Color.FromRgb(0x00, 0xE0, 0xE0),
                Color.FromRgb(0xE0, 0x80, 0x80),   Color.FromRgb(0xE0, 0x00, 0xE0), Color.FromRgb(0xE0, 0xE0, 0x00), Color.FromRgb(0xE0, 0xE0, 0xE0)
            };

        static readonly Color[] tableTownsLoomPalette =
            {
                Color.FromRgb(0x00, 0x00, 0x00),   Color.FromRgb(0x00, 0x00, 0xAB),   Color.FromRgb(0x00, 0xAB, 0x00),   Color.FromRgb(0x00, 0xAB, 0xAB),
                Color.FromRgb(0xAB, 0x00, 0x00),   Color.FromRgb(0x69, 0x29, 0x45),   Color.FromRgb(0x8C, 0x4D, 0x14),   Color.FromRgb(0xAB, 0xAB, 0xAB),
                Color.FromRgb(0x57, 0x3F, 0x57),   Color.FromRgb(0x57, 0x57, 0xFF),   Color.FromRgb(0x57, 0xFF, 0x57),   Color.FromRgb(0x57, 0xFF, 0xFF),
                Color.FromRgb(0xFF, 0x57, 0x57),   Color.FromRgb(0xD6, 0x94, 0x40),   Color.FromRgb(0xFF, 0xFF, 0x57),   Color.FromRgb(0xFF, 0xFF, 0xFF)
            };

        static Palette egaPalette;
        static Palette cgaPalette;
        static Palette v1Palette;
        static Palette c64Palette;
        static Palette amigaPalette;
        static Palette amigaMonkeyIslandPalette;
        static Palette towns3Palette;
        static Palette townsLoomPalette;
    }
}
