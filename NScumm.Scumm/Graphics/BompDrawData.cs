//
//  BompDrawData.cs
//
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
using System.Diagnostics;
using System.IO;
using NScumm.Core;
using NScumm.Core.Graphics;

namespace NScumm.Scumm.Graphics
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class BompDrawData
    {
        public PixelNavigator Dst;
        public int X, Y;

        public byte[] Src;
        public int Width, Height;

        public int ScaleX, ScaleY;

        public int ShadowMode;

        public byte[] ShadowPalette;

        public ushort[] ActorPalette;

        public bool Mirror;

        public int NumStrips;

        public PixelNavigator? MaskPtr;

        internal string DebuggerDisplay
        {
            get
            { 
                return string.Format("Rect={0}]", new Rect(X, Y, X + Width, Y + Height));
            }
        }

        public void DrawBomp()
        {
            Rect clip;
            byte skip_y_bits = 0x80;
            byte skip_y_new = 0;
            byte[] bomp_scaling_x = new byte[64];
            byte[] bomp_scaling_y = new byte[64];

            if (X < 0)
            {
                clip.Left = -X;
            }
            else
            {
                clip.Left = 0;
            }

            if (Y < 0)
            {
                clip.Top = -Y;
            }
            else
            {
                clip.Top = 0;
            }

            clip.Right = Width;
            if (clip.Right > Dst.Width - X)
            {
                clip.Right = Dst.Width - X;
            }

            clip.Bottom = Height;
            if (clip.Bottom > Dst.Height - Y)
            {
                clip.Bottom = Dst.Height - Y;
            }

            var src = Src;
            var pn = new PixelNavigator(Dst);
            pn.GoTo(X + clip.Left, Y);

            var maskbit = ScummHelper.RevBitMask((X + clip.Left) & 7);
            PixelNavigator maskPtr = new PixelNavigator();
            // Mask against any additionally imposed mask
            if (MaskPtr.HasValue)
            {
                maskPtr = MaskPtr.Value;
                maskPtr.GoTo((X + clip.Left) / 8, Y);
            }

            var scalingYPtr = 0;

            // Setup vertical scaling
            if (ScaleY != 255)
            {
                var scaleBottom = SetupBompScale(bomp_scaling_y, Height, ScaleY);

                skip_y_new = bomp_scaling_y[scalingYPtr++];
                skip_y_bits = 0x80;

                if (clip.Bottom > scaleBottom)
                {
                    clip.Bottom = scaleBottom;
                }
            }

            // Setup horizontal scaling
            if (ScaleX != 255)
            {
                var scaleRight = SetupBompScale(bomp_scaling_x, Width, ScaleX);

                if (clip.Right > scaleRight)
                {
                    clip.Right = scaleRight;
                }
            }

            var width = clip.Right - clip.Left;

            if (width <= 0)
                return;

            int pos_y = 0;
            var line_buffer = new byte[1024];

            byte tmp;
            using (var br = new BinaryReader(new MemoryStream(src)))
            {
                // Loop over all lines
                while (pos_y < clip.Bottom)
                {
                    br.ReadUInt16();
                    // Decode a single (bomp encoded) line, reversed if we are in mirror mode
                    if (Mirror)
                        BompDecodeLineReverse(br, line_buffer, 0, Width);
                    else
                        // Decode a single (bomp encoded) line
                        BompDecodeLine(br, line_buffer, 0, Width);

                    // If vertical scaling is enabled, do it
                    if (ScaleY != 255)
                    {
                        // A bit set means we should skip this line...
                        tmp = (byte)(skip_y_new & skip_y_bits);

                        // Advance the scale-skip bit mask, if it's 0, get the next scale-skip byte
                        skip_y_bits /= 2;
                        if (skip_y_bits == 0)
                        {
                            skip_y_bits = 0x80;
                            skip_y_new = bomp_scaling_y[scalingYPtr++];
                        }

                        // Skip the current line if the above check tells us to
                        if (tmp != 0)
                            continue;
                    }

                    // Perform horizontal scaling
                    if (ScaleX != 255)
                    {
                        BompScaleFuncX(line_buffer, bomp_scaling_x, 0, 0x80, Width);
                    }

                    // The first clip.top lines are to be clipped, i.e. not drawn
                    if (clip.Top > 0)
                    {
                        clip.Top--;
                    }
                    else
                    {
                        // Replace the parts of the line which are masked with the transparency color
                        if (MaskPtr.HasValue)
                            BompApplyMask(line_buffer, clip.Left, maskPtr, (byte)maskbit, width, 255);

                        // Apply custom color map, if available
                        if (ActorPalette != null)
                            BompApplyActorPalette(ActorPalette, line_buffer, clip.Left, width);

                        // Finally, draw the decoded, scaled, masked and recolored line onto
                        // the target surface, using the specified shadow mode
                        BompApplyShadow(ShadowMode, ShadowPalette, line_buffer, clip.Left, pn, width, 255);
                    }

                    // Advance to the next line
                    pos_y++;
                    if (MaskPtr.HasValue)
                    {
                        maskPtr.OffsetY(1);
                    }
                    pn.OffsetY(1);
                }
            }
        }

        void BompApplyActorPalette(ushort[] actorPalette, byte[] line_buffer, int pos, int size)
        {
            actorPalette[255] = 255;
            while (size-- > 0)
            {
                line_buffer[pos] = (byte)actorPalette[line_buffer[pos]];
                pos++;
            }
        }

        public static void BompApplyMask(byte[] line_buffer, int linePos, PixelNavigator mask, byte maskbit, int size, byte transparency)
        {
            while (true)
            {
                do
                {
                    if (size-- == 0)
                        return;
                    if ((mask.Read() & maskbit) != 0)
                    {
                        line_buffer[linePos] = transparency;
                    }
                    linePos++;
                    maskbit >>= 1;
                } while (maskbit != 0);
                mask.OffsetX(1);
                maskbit = 128;
            }
        }

        public static void BompApplyShadow(int shadowMode, byte[] shadowPalette, byte[] lineBuffer, int linePos, PixelNavigator dst, int size, byte transparency)
        {
            Debug.Assert(size > 0);
            switch (shadowMode)
            {
                case 0:
                    BompApplyShadow0(lineBuffer, linePos, dst, size);
                    break;
                case 1:
                    BompApplyShadow1(shadowPalette, lineBuffer, linePos, dst, size, transparency);
                    break;
                case 3:
                    BompApplyShadow3(shadowPalette, lineBuffer, linePos, dst, size, transparency);
                    break;
                default:
                    throw new ArgumentException(string.Format("Unknown shadow mode {0}", shadowMode));
            }
        }

        public static void BompApplyShadow0(byte[] lineBuffer, int linePos, PixelNavigator dst, int size)
        {
            while (size-- > 0)
            {
                byte tmp = lineBuffer[linePos++];
                if (tmp != 255)
                {
                    dst.Write(tmp);
                }
                dst.OffsetX(1);
            }
        }

        static void BompApplyShadow1(byte[] shadowPalette, byte[] lineBuffer, int linePos, PixelNavigator dst, int size, byte transparency)
        {
            while (size-- > 0)
            {
                byte tmp = lineBuffer[linePos++];
                if (tmp != transparency)
                {
                    if (tmp == 13)
                    {
                        tmp = shadowPalette[dst.Read()];
                    }
                    dst.Write(tmp);
                }
                dst.OffsetX(1);
            }
        }

        static void BompApplyShadow3(byte[] shadowPalette, byte[] lineBuffer, int linePos, PixelNavigator dst, int size, byte transparency)
        {
            while (size-- > 0)
            {
                byte tmp = lineBuffer[linePos++];
                if (tmp != transparency)
                {
                    if (tmp < 8)
                    {
                        tmp = shadowPalette[dst.Read() + (tmp << 8)];
                    }
                    dst.Write(tmp);
                }
                dst.OffsetX(1);
            }
        }

        int SetupBompScale(byte[] scaling, int size, int scale)
        {
            int[] offsets = { 3, 2, 1, 0, 7, 6, 5, 4 };
            var bitsCount = 0;
            var pos = 0;

            var count = (256 - size / 2);
            Debug.Assert(0 <= count && count < 768);
            var scalePos = count;

            count = (size + 7) / 8;
            while ((count--) != 0)
            {
                byte scaleMask = 0;
                for (var i = 0; i < 8; i++)
                {
                    var scaleTest = bigCostumeScaleTable[scalePos + offsets[i]];
                    scaleMask <<= 1;
                    if (scale < scaleTest)
                    {
                        scaleMask |= 1;
                    }
                    else
                    {
                        bitsCount++;
                    }
                }
                scalePos += 8;

                scaling[pos++] = scaleMask;
            }
            size &= 7;
            if (size != 0)
            {
                --pos;
                if ((scaling[pos] & ScummHelper.RevBitMask(size)) == 0)
                {
                    scaling[pos] |= (byte)ScummHelper.RevBitMask(size);
                    bitsCount--;
                }
            }

            return bitsCount;
        }

        void BompScaleFuncX(byte[] lineBuffer, byte[] scaling, int scalingPos, byte skip, int size)
        {
            var line_ptr1 = 0;
            var line_ptr2 = 0;

            byte tmp = scaling[scalingPos++];

            while ((size--) != 0)
            {
                if ((skip & tmp) == 0)
                {
                    lineBuffer[line_ptr1++] = lineBuffer[line_ptr2];
                }
                line_ptr2++;
                skip >>= 1;
                if (skip == 0)
                {
                    skip = 128;
                    tmp = scaling[scalingPos++];
                }
            }
        }

        static void BompDecodeLineReverse(BinaryReader br, byte[] dst, int dstPos, int len)
        {
            Debug.Assert(len > 0);

            dstPos += len;

            int num;
            byte code, color;

            while (len > 0)
            {
                code = br.ReadByte();
                num = (code >> 1) + 1;
                if (num > len)
                    num = len;
                len -= num;
                dstPos -= num;
                if ((code & 1) != 0)
                {
                    color = br.ReadByte();
                    for (int i = 0; i < num; i++)
                    {
                        dst[dstPos + i] = color;
                    }
                }
                else
                {
                    for (int i = 0; i < num; i++)
                    {
                        dst[dstPos + i] = br.ReadByte();
                    }
                }
            }
        }

        static void BompDecodeLine(BinaryReader br, byte[] dst, int dstPos, int len)
        {
            while (len > 0)
            {
                var code = br.ReadByte();
                var num = (code >> 1) + 1;
                if (num > len)
                    num = len;
                len -= num;
                if ((code & 1) != 0)
                {
                    var color = br.ReadByte();
                    for (int i = 0; i < num; i++)
                    {
                        dst[dstPos + i] = color;
                    }
                }
                else
                {
                    for (int i = 0; i < num; i++)
                    {
                        dst[dstPos + i] = br.ReadByte();
                    }
                }
                dstPos += num;
            }
        }

        public static byte[] DecompressBomp(byte[] data, int width, int height)
        {
            var pixels = new byte[width * height];
            using (var br = new BinaryReader(new MemoryStream(data)))
            {
                for (int h = 0; h < height; h++)
                {
                    br.ReadUInt16();
                    BompDecodeLine(br, pixels, h * width, width);
                }
            }
            return pixels;
        }

        static readonly byte[] bigCostumeScaleTable =
            {
                0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0,
                0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
                0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8,
                0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
                0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4,
                0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
                0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC,
                0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
                0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2,
                0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
                0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA,
                0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
                0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6,
                0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
                0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE,
                0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
                0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1,
                0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
                0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9,
                0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
                0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
                0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
                0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED,
                0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
                0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3,
                0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
                0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
                0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
                0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7,
                0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
                0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF,
                0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFE,

                0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0,
                0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
                0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8,
                0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
                0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4,
                0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
                0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC,
                0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
                0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2,
                0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
                0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA,
                0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
                0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6,
                0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
                0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE,
                0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
                0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1,
                0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
                0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9,
                0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
                0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
                0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
                0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED,
                0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
                0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3,
                0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
                0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
                0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
                0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7,
                0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
                0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF,
                0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFE,

                0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0,
                0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
                0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8,
                0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
                0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4,
                0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
                0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC,
                0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
                0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2,
                0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
                0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA,
                0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
                0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6,
                0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
                0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE,
                0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
                0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1,
                0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
                0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9,
                0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
                0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
                0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
                0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED,
                0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
                0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3,
                0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
                0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
                0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
                0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7,
                0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
                0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF,
                0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFF,
            };
    }
    
}
