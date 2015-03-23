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
            if (colors.Length != 16)
                throw new ArgumentException("16 colors was expected.");

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

        static Palette egaPalette;
        static Palette cgaPalette;
    }
}
