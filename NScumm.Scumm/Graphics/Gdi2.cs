//
//  Gdi2.cs
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

using System.Diagnostics;
using System.IO;
using NScumm.Core.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Graphics
{
    class StripTable
    {
        public int[] offsets = new int[160];
        public int[] run = new int[160];
        public int[] color = new int[160];
        public int[] zoffsets = new int[120];
        // FIXME: Why only 120 here?
        public int[] zrun = new int[120];
        // FIXME: Why only 120 here?
    }

    public class Gdi2: Gdi
    {
        /// <summary>
        /// For V2 games, we cache offsets into the room graphics, to speed up things.
        /// </summary>
        StripTable _roomStrips;

        public Gdi2(ScummEngine vm, GameInfo game)
            : base(vm, game)
        {
        }

        public override void RoomChanged(Room room)
        {
            var roomPtr = room.Image.Data;
            _roomStrips = GenerateStripTable(roomPtr,
                room.Header.Width, room.Header.Height);
        }

        /// <summary>
        /// Create and fill a table with offsets to the graphic and mask strips in the
        /// given V2 EGA bitmap.
        /// </summary>
        /// <returns>The filled strip table.</returns>
        /// <param name="src">The V2 EGA bitmap.</param>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        StripTable GenerateStripTable(byte[] src, int width, int height)
        {
            int srcPos = 0;
            var table = new StripTable();
            byte color = 0, data = 0;
            int x, y, length = 0;
            byte run = 1;

            // Decode the graphics strips, and memorize the run/color values
            // as well as the byte offset.
            for (x = 0; x < width; x++)
            {

                if ((x % 8) == 0)
                {
                    Debug.Assert(x / 8 < 160);
                    table.run[x / 8] = run;
                    table.color[x / 8] = color;
                    table.offsets[x / 8] = srcPos;
                }

                for (y = 0; y < height; y++)
                {
                    if (--run == 0)
                    {
                        data = src[srcPos++];
                        if ((data & 0x80) != 0)
                        {
                            run = (byte)(data & 0x7f);
                        }
                        else
                        {
                            run = (byte)(data >> 4);
                        }
                        if (run == 0)
                        {
                            run = src[srcPos++];
                        }
                        color = (byte)(data & 0x0f);
                    }
                }
            }

            // The mask data follows immediately after the graphics.
            x = 0;
            y = height;
            width /= 8;

            for (;;)
            {
                length = src[srcPos++];
                byte runFlag = (byte)(length & 0x80);
                if (runFlag != 0)
                {
                    length &= 0x7f;
                    data = src[srcPos++];
                }
                do
                {
                    if (runFlag == 0)
                        data = src[srcPos++];
                    if (y == height)
                    {
                        Debug.Assert(x < 120);
                        table.zoffsets[x] = srcPos - 1;
                        table.zrun[x] = length | runFlag;
                    }
                    if (--y == 0)
                    {
                        if (--width == 0)
                            return table;
                        x++;
                        y = height;
                    }
                } while ((--length) != 0);
            }
        }

        protected override void PrepareDrawBitmap(ImageData img, VirtScreen vs,
                               Point p, int width, int height,
                               int stripnr, int numstrip)
        {
            var ptr = img.Data;
            //
            // Since V3, all graphics data was encoded in strips, which is very efficient
            // for redrawing only parts of the screen. However, V2 is different: here
            // the whole graphics are encoded as one big chunk. That makes it rather
            // difficult to draw only parts of a room/object. We handle the V2 graphics
            // differently from all other (newer) graphic formats for this reason.
            //
            var table = (_objectMode ? null : _roomStrips);
            int left = (stripnr * 8);
            int right = left + (numstrip * 8);
            PixelNavigator navDst;
            var srcOffset = 0;
            byte color, data = 0;
            int run;
            bool dither = false;
            byte[] dither_table = new byte[128];
            var ditherOffset = 0;
            int theX, theY, maxX;

            var surface = vs.HasTwoBuffers ? vs.Surfaces[1] : vs.Surfaces[0];
            navDst = new PixelNavigator(surface);
            navDst.GoTo(p.X * 8, p.Y);

            var mask_ptr = GetMaskBuffer(p.X, p.Y, 1);

            if (table != null)
            {
                run = table.run[stripnr];
                color = (byte)table.color[stripnr];
                srcOffset = table.offsets[stripnr];
                theX = left;
                maxX = right;
            }
            else
            {
                run = 1;
                color = 0;
                srcOffset = 0;
                theX = 0;
                maxX = width;
            }

            // Decode and draw the image data.
            Debug.Assert(height <= 128);
            for (; theX < maxX; theX++)
            {
                ditherOffset = 0;
                for (theY = 0; theY < height; theY++)
                {
                    if (--run == 0)
                    {
                        data = ptr[srcOffset++];
                        if ((data & 0x80) != 0)
                        {
                            run = data & 0x7f;
                            dither = true;
                        }
                        else
                        {
                            run = data >> 4;
                            dither = false;
                        }
                        color = RoomPalette[data & 0x0f];
                        if (run == 0)
                        {
                            run = ptr[srcOffset++];
                        }
                    }
                    if (!dither)
                    {
                        dither_table[ditherOffset] = color;
                    }
                    if (left <= theX && theX < right)
                    {
                        navDst.Write(dither_table[ditherOffset++]);
                        navDst.OffsetY(1);
                    }
                }
                if (left <= theX && theX < right)
                {
                    navDst.Offset(1, -height);
                }
            }

            // Draw mask (zplane) data
            theY = 0;

            if (table != null)
            {
                srcOffset = table.zoffsets[stripnr];
                run = table.zrun[stripnr];
                theX = left;
            }
            else
            {
                run = ptr[srcOffset++];
                theX = 0;
            }
            while (theX < right)
            {
                byte runFlag = (byte)(run & 0x80);
                if (runFlag != 0)
                {
                    run &= 0x7f;
                    data = ptr[srcOffset++];
                }
                do
                {
                    if (runFlag == 0)
                        data = ptr[srcOffset++];

                    if (left <= theX)
                    {
                        mask_ptr.Write(data);
                        mask_ptr.OffsetY(1);
                    }
                    theY++;
                    if (theY >= height)
                    {
                        if (left <= theX)
                        {
                            mask_ptr.Offset(1, -height);
                        }
                        theY = 0;
                        theX += 8;
                        if (theX >= right)
                            break;
                    }
                } while ((--run) != 0);
                run = ptr[srcOffset++];
            }
        }

        protected override bool DrawStrip(PixelNavigator navDst, int width, int height, int stripnr, BinaryReader smapReader)
        {
            return false;
        }

        protected override void DecodeMask(int x, int y, int width, int height, int stripnr, System.Collections.Generic.IList<ZPlane> zPlanes, bool transpStrip, DrawBitmaps flags)
        {
            // Do nothing here for V2 games - zplane was already handled.
        }
    }
    
}
