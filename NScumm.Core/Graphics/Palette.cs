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


namespace NScumm.Core.Graphics
{
    public class Palette
    {
        public static Palette Ega { get { return egaPalette ?? (egaPalette = CreatePalette(tableEGAPalette)); } }

        public static Palette Cga { get { return cgaPalette ?? (cgaPalette = CreatePalette(tableCGAPalette)); } }

        public static Palette V1 { get { return v1Palette ?? (v1Palette = CreatePalette(tableV1Palette)); } }

        public static Palette C64 { get { return c64Palette ?? (c64Palette = CreatePalette(tableC64Palette)); } }

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
                Color.FromRgb(0x75, 0x75, 0x75),   Color.FromRgb(0xA3, 0xE7, 0x7C),   Color.FromRgb(0x70, 0x64, 0xD6),   Color.FromRgb(0xA3, 0xA3, 0xA3)
            };

        static Palette egaPalette;
        static Palette cgaPalette;
        static Palette v1Palette;
        static Palette c64Palette;
    }
}
