//
//  Codec.cs
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

using System.Collections.Generic;
using System.IO;
using NScumm.Core.Graphics;

namespace NScumm.Core.Image.Codecs
{
    public abstract class Codec
    {
        /**
    	 * A type of dithering.
	     */

        public enum DitherType
        {
            /** Unknown */
            Unknown,
            /** Video for Windows dithering */
            VFW,
            /** QuickTime dithering */
            QT
        }

        public abstract PixelFormat PixelFormat { get; }

        /**
         * Decode the frame for the given data and return a pointer to a surface
         * containing the decoded frame.
         *
         * @return a pointer to the decoded frame
         */
        public abstract Surface DecodeFrame(Stream stream);

        /**
         * Can this codec's frames contain a palette?
         */

        public virtual bool ContainsPalette()
        {
            return false;
        }

        /**
         * Get the palette last decoded from decodeImage
         */

        public virtual byte[] GetPalette()
        {
            return null;
        }

        /**
         * Does the codec have a dirty palette?
         */

        public virtual bool HasDirtyPalette()
        {
            return false;
        }

        /**
         * Can the codec dither down to 8bpp?
         */

        public virtual bool CanDither(DitherType type)
        {
            return false;
        }

        /**
         * Activate dithering mode with a palette
         */

        public virtual void SetDither(DitherType type, byte[] palette)
        {
        }

        /**
         * Create a dither table, as used by QuickTime codecs.
         */

        public static byte[] CreateQuickTimeDitherTable(byte[] palette, int colorCount)
        {
            var buf = new byte[0x10000];

            var checkQueue = new List<ushort>();

            var foundBlack = false;
            var foundWhite = false;

            var palPtr = new BytePtr(palette);

            // See what colors we have, and add them to the queue to check
            for (var i = 0; i < colorCount; i++)
            {
                var r = palPtr.Value; palPtr.Offset++;
                var g = palPtr.Value; palPtr.Offset++;
                var b = palPtr.Value; palPtr.Offset++;
                var n = (ushort) ((i << 8) | 1);
                var col = MakeQuickTimeDitherColor(r, g, b);

                if (col == 0)
                {
                    // Special case for close-to-black
                    // The original did more here, but it effectively discarded the value
                    // due to a poor if-check (whole 16-bit value instead of lower 8-bits).
                    buf.WriteUInt16(0, n);
                    foundBlack = true;
                }
                else if (col == 0x3FFF)
                {
                    // Special case for close-to-white
                    // The original did more here, but it effectively discarded the value
                    // due to a poor if-check (whole 16-bit value instead of lower 8-bits).
                    buf.WriteUInt16(0x7FFE, n);
                    foundWhite = true;
                }
                else
                {
                    // Previously unfound color
                    AddColorToQueue(col, n, buf, checkQueue);
                }
            }

            // More special handling for white
            if (foundWhite)
                checkQueue.Insert(0, 0x3FFF);

            // More special handling for black
            if (foundBlack)
                checkQueue.Insert(0, 0);

            // Go through the list of colors we have and match up similar colors
            // to fill in the table as best as we can.
            while (checkQueue.Count > 0)
            {
                var col = checkQueue[0];
                checkQueue.RemoveAt(0);
                var index = buf.ToUInt16(col * 2);

                var x = (uint) (col << 4);
                if ((x & 0xFF) < 0xF0)
                    AddColorToQueue((ushort) ((x + 0x10) >> 4), index, buf, checkQueue);
                if ((x & 0xFF) >= 0x10)
                    AddColorToQueue((ushort) ((x - 0x10) >> 4), index, buf, checkQueue);

                var y = (uint) (col << 7);
                if ((y & 0xFF00) < 0xF800)
                    AddColorToQueue((ushort) ((y + 0x800) >> 7), index, buf, checkQueue);
                if ((y & 0xFF00) >= 0x800)
                    AddColorToQueue((ushort) ((y - 0x800) >> 7), index, buf, checkQueue);

                var z = (uint) (col << 2);
                if ((z & 0xFF00) < 0xF800)
                    AddColorToQueue((ushort) ((z + 0x800) >> 2), index, buf, checkQueue);
                if ((z & 0xFF00) >= 0x800)
                    AddColorToQueue((ushort) ((z - 0x800) >> 2), index, buf, checkQueue);
            }

            // Contract the table back to just palette entries
            for (var i = 0; i < 0x4000; i++)
                buf[i] = (byte) (buf.ToUInt16(i * 2) >> 8);

            // Now go through and distribute the error to three more pixels
            var bufPtr = new BytePtr(buf);
            for (uint realR = 0; realR < 0x100; realR += 8)
            {
                for (uint realG = 0; realG < 0x100; realG += 8)
                {
                    for (uint realB = 0; realB < 0x100; realB += 16)
                    {
                        var palIndex = bufPtr.Value;
                        var r = (byte) realR;
                        var g = (byte) realG;
                        var b = (byte) realB;

                        var palR = (byte) (palette[palIndex * 3] & 0xF8);
                        var palG = (byte) (palette[palIndex * 3 + 1] & 0xF8);
                        var palB = (byte) (palette[palIndex * 3 + 2] & 0xF0);

                        r = AdjustColorRange(r, (byte) realR, palR);
                        g = AdjustColorRange(g, (byte) realG, palG);
                        b = AdjustColorRange(b, (byte) realB, palB);
                        palIndex = buf[MakeQuickTimeDitherColor(r, g, b)];
                        bufPtr[0x4000] = palIndex;

                        palR = (byte) (palette[palIndex * 3] & 0xF8);
                        palG = (byte) (palette[palIndex * 3 + 1] & 0xF8);
                        palB = (byte) (palette[palIndex * 3 + 2] & 0xF0);

                        r = AdjustColorRange(r, (byte) realR, palR);
                        g = AdjustColorRange(g, (byte) realG, palG);
                        b = AdjustColorRange(b, (byte) realB, palB);
                        palIndex = buf[MakeQuickTimeDitherColor(r, g, b)];
                        bufPtr[0x8000] = palIndex;

                        palR = (byte) (palette[palIndex * 3] & 0xF8);
                        palG = (byte) (palette[palIndex * 3 + 1] & 0xF8);
                        palB = (byte) (palette[palIndex * 3 + 2] & 0xF0);

                        r = AdjustColorRange(r, (byte) realR, palR);
                        g = AdjustColorRange(g, (byte) realG, palG);
                        b = AdjustColorRange(b, (byte) realB, palB);
                        palIndex = buf[MakeQuickTimeDitherColor(r, g, b)];
                        bufPtr[0xC000] = palIndex;

                        bufPtr.Offset++;
                    }
                }
            }

            return buf;
        }

        /**
         * Add a color to the QuickTime dither table check queue if it hasn't already been found.
         */

        private static void AddColorToQueue(ushort color, ushort index, byte[] checkBuffer, List<ushort> checkQueue)
        {
            if ((checkBuffer.ToUInt16(color * 2) & 0xFF) != 0) return;

            // Previously unfound color
            checkBuffer.WriteUInt16(color * 2, index);
            checkQueue.Add(color);
        }

        private static byte AdjustColorRange(byte currentColor, byte correctColor, byte palColor)
        {
            return (byte) ScummHelper.Clip(currentColor - palColor + correctColor, 0, 255);
        }

        private static ushort MakeQuickTimeDitherColor(byte r, byte g, byte b)
        {
            // RGB554
            return (ushort) (((r & 0xF8) << 6) | ((g & 0xF8) << 1) | (b >> 4));
        }
    }
}