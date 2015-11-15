//
//  ImageDumper.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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
using System.Linq;
using NScumm.Scumm.IO;
using NScumm.Core.Graphics;
using NScumm.Core;
using System.Text;
using System.IO;
using System.Collections.Generic;
using NScumm.Scumm;
using NScumm.Scumm.Graphics;

namespace NScumm.Dump
{
    public class ImageDumper
    {
        GameInfo Game { get; set; }

        public ImageDumper(GameInfo game)
        {
            Game = game;
        }

        Gdi CreateGdi()
        {
            return Gdi.Create(null, Game);
        }

        public void DumpRoomImages(ResourceManager index, IList<int> roomIds)
        {
            var gdi = CreateGdi();
            gdi.IsZBufferEnabled = false;
            gdi.RoomPalette = CreatePalette();

            var rooms = roomIds == null ? index.Rooms : roomIds.Select(i => index.GetRoom((byte)i));
            foreach (var room in rooms)
            {
                if (room.Image == null)
                    continue;

                var name = room.Name ?? "room_" + room.Number;
                Console.WriteLine("room #{0}: {1}", room.Number, name);

                try
                {
                    DumpRoomImage(room, gdi);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ResetColor();
                }
            }
        }

        public void DumpObjectImages(ResourceManager index, IList<int> objIds = null)
        {
            var gdi = CreateGdi();
            gdi.IsZBufferEnabled = false;
            gdi.RoomPalette = CreatePalette();

            foreach (var room in index.Rooms)
            {
                foreach (var obj in room.Objects)
                {
                    if (objIds != null && !objIds.Contains(obj.Number))
                        continue;

                    try
                    {
                        DumpRoomObjectImages(room, obj, gdi);
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(e);
                        Console.ResetColor();
                    }
                }
            }
        }

        public void DumpImages(ResourceManager index)
        {
            foreach (var room in index.Rooms)
            {
                if (room.Image == null)
                    continue;

                var name = room.Name ?? "room_" + room.Number;
                Console.WriteLine("room #{0}: {1}", room.Number, name);

                try
                {
                    var gdi = CreateGdi();
                    gdi.IsZBufferEnabled = false;
                    gdi.RoomPalette = CreatePalette();

                    DumpRoomObjects(room, gdi);
                    DumpRoomImage(room, gdi);
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                    Console.ResetColor();
                }
            }
        }

        static byte[] CreatePalette()
        {
            var palette = new byte[256];
            for (int i = 0; i < palette.Length; i++)
            {
                palette[i] = (byte)i;
            }
            return palette;
        }

        System.Drawing.Color ToColor(Room room, VirtScreen screen, int x, int y)
        {
            var index = screen.Surfaces[0].Pixels[y * screen.Width + x];
            var c = Game.Features.HasFlag(GameFeatures.SixteenColors) ?
                Palette.Cga.Colors[index] : room.Palette.Colors[index];
            return System.Drawing.Color.FromArgb(c.R, c.G, c.B);
        }

        System.Drawing.Color ToColor(byte[] pixels, Palette palette, VirtScreen screen, int x, int y)
        {
            var index = pixels[y * screen.Width + x];
            var c = palette.Colors[index];
            return System.Drawing.Color.FromArgb(c.R, c.G, c.B);
        }

        static byte[,,] cgaDither = new byte[2, 2, 16]
        {
            {
                { 0, 1, 0, 1, 2, 2, 0, 0, 3, 1, 3, 1, 3, 2, 1, 3 },
                { 0, 0, 1, 1, 0, 2, 2, 3, 0, 3, 1, 1, 3, 3, 1, 3 }
            },
            {
                { 0, 0, 1, 1, 0, 2, 2, 3, 0, 3, 1, 1, 3, 3, 1, 3 },
                { 0, 1, 0, 1, 2, 2, 0, 0, 3, 1, 1, 1, 3, 2, 1, 3 }
            }
        };

        // CGA dithers 4x4 square with direct substitutes
        // Odd lines have colors swapped, so there will be checkered patterns.
        // But apparently there is a mistake for 10th color.
        static void DitherCGA(byte[] dst, int dstPitch, int x, int y, int width, int height)
        {
            int ptr;
            int idx1, idx2;

            for (var y1 = 0; y1 < height; y1++)
            {
                ptr = y1 * dstPitch;
                idx1 = (y + y1) % 2;

                for (var x1 = 0; x1 < width; x1++)
                {
                    idx2 = (x + x1) % 2;
                    dst[ptr] = cgaDither[idx1, idx2, dst[ptr] & 0xF];
                    ptr++;
                }
            }
        }

        System.Drawing.Bitmap ToBitmap(Room room, VirtScreen screen)
        {
            var bmp = new System.Drawing.Bitmap(screen.Width, screen.Height);
//            DitherCGA(screen.Surfaces[0].Pixels, screen.Surfaces[0].Pitch, 0, 0, screen.Width, screen.Height);
            var palette = Game.Features.HasFlag(GameFeatures.SixteenColors) ? Palette.Ega : room.Palette;
            for (int x = 0; x < screen.Width; x++)
            {
                for (int y = 0; y < screen.Height; y++)
                {
                    bmp.SetPixel(x, y, ToColor(screen.Surfaces[0].Pixels, palette, screen, x, y));
                }
            }
            return bmp;
        }

        void DumpRoomImage(Room room, Gdi gdi)
        {
            var name = room.Name ?? "room_" + room.Number;

            var screen = new VirtScreen(0, room.Header.Width, room.Header.Height, PixelFormat.Indexed8, 2);
            var numStrips = room.Header.Width / 8;
            gdi.NumStrips = numStrips;
            gdi.IsZBufferEnabled = false;
            if (room.Header.Height > 0)
            {
                gdi.RoomChanged(room);
                gdi.DrawBitmap(room.Image, screen, new Point(), room.Header.Width, room.Header.Height, 0, numStrips, room.Header.Width, 0, true);

                using (var bmpRoom = ToBitmap(room, screen))
                {
                    bmpRoom.Save(name + ".png");
                }
            }

            DumpZPlanes(room, name);
        }

        static void DumpZPlanes(Room room, string name)
        {
            if (room.Image != null)
            {
                for (int i = 0; i < room.Image.ZPlanes.Count; i++)
                {
                    var zplane = room.Image.ZPlanes[i];
                    var nStrips = zplane.StripOffsets.Count;
                    var pixels = new byte[nStrips * room.Header.Height];
                    var pn = new PixelNavigator(pixels, nStrips, 1);
                    using (var ms = new MemoryStream(zplane.Data))
                    {
                        for (int nStrip = 0; nStrip < nStrips; nStrip++)
                        {
                            var offset = zplane.StripOffsets[nStrip];
                            if (offset.HasValue)
                            {
                                ms.Seek(offset.Value, SeekOrigin.Begin);
                                pn.GoTo(nStrip, 0);
                                DecompressMaskImg(pn, ms, room.Header.Height);
                            }
                        }
                    }
                    using (var bmpZ = new System.Drawing.Bitmap(nStrips * 8, room.Header.Height))
                    {
                        for (int j = 0; j < room.Header.Height; j++)
                        {
                            for (int x = 0; x < nStrips; x++)
                            {
                                for (int b = 0; b < 8; b++)
                                {
                                    bmpZ.SetPixel(x * 8 + b, j, (pixels[x + j * nStrips] & (0x80 >> b)) != 0 ? System.Drawing.Color.White : System.Drawing.Color.Black);
                                }
                            }
                        }
                        bmpZ.Save(name + "_z" + i + ".png");
                    }
                }
            }
        }

        static void DecompressMaskImg(PixelNavigator dst, Stream src, int height)
        {
            while (height != 0)
            {
                var b = (byte)src.ReadByte();

                if ((b & 0x80) != 0)
                {
                    b &= 0x7F;
                    var c = (byte)src.ReadByte();

                    do
                    {
                        dst.Write(c);
                        dst.OffsetY(1);
                        --height;
                    } while (--b != 0 && height != 0);
                }
                else
                {
                    do
                    {
                        dst.Write((byte)src.ReadByte());
                        dst.OffsetY(1);
                        --height;
                    } while (--b != 0 && height != 0);
                }
            }
        }

        void DumpRoomObjects(Room room, Gdi gdi)
        {
            foreach (var obj in room.Objects)
            {
                DumpRoomObjectImages(room, obj, gdi);
            }
        }

        void DumpRoomObjectImages(Room room, ObjectData obj, Gdi gdi)
        {
            var text = new ScummText(obj.Name);
            var sb = new StringBuilder();
            sb.Append("Object #" + obj.Number).Append(" ");

            var decoder = new TextDecoder(sb);
            text.Decode(decoder);

            sb.AppendFormat(" size: {0}x{1}", obj.Width, obj.Height);
            Console.WriteLine(sb);

            var j = 0;
            foreach (var img in obj.Images)
            {
//                try
//                {
                var screen = new VirtScreen(0, obj.Width, obj.Height, PixelFormat.Indexed8, 2);
                if (img.IsBomp)
                {
                    var bdd = new BompDrawData();
                    bdd.Src = img.Data;
                    bdd.Dst = new PixelNavigator(screen.Surfaces[0]);
                    bdd.X = 0;
                    bdd.Y = 0;

                    bdd.Width = obj.Width;
                    bdd.Height = obj.Height;

                    bdd.ScaleX = 255;
                    bdd.ScaleY = 255;
                    bdd.DrawBomp();
                }
                else
                {
                    gdi.DrawBitmap(img, screen, new Point(0, 0), obj.Width, obj.Height & 0xFFF8, 0, obj.Width / 8, room.Header.Width, DrawBitmaps.None, true);
                }

                using (var bmp = ToBitmap(room, screen))
                {
                    bmp.Save("obj_" + obj.Number + "_" + (++j) + ".png");
                }
//                }
//                catch (Exception e)
//                {
//                    Console.ForegroundColor = ConsoleColor.Red;
//                    Console.WriteLine(e);
//                    Console.ResetColor();
//                    Console.ReadLine();
//                }
            }
        }
    }
}

