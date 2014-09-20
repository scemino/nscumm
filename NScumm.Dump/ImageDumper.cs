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
using NScumm.Core.IO;
using NScumm.Core.Graphics;
using NScumm.Core;
using System.Text;
using System.IO;

namespace NScumm.Tmp
{
    public class ImageDumper
    {
        GameInfo Game { get; set; }

        static Color[] tableEGAPalette =
            {
                Color.FromRgb(0x00, 0x00, 0x00),   Color.FromRgb(0x00, 0x00, 0xAA),   Color.FromRgb(0x00, 0xAA, 0x00),   Color.FromRgb(0x00, 0xAA, 0xAA),
                Color.FromRgb(0xAA, 0x00, 0x00),   Color.FromRgb(0xAA, 0x00, 0xAA),   Color.FromRgb(0xAA, 0x55, 0x00),   Color.FromRgb(0xAA, 0xAA, 0xAA),
                Color.FromRgb(0x55, 0x55, 0x55),   Color.FromRgb(0x55, 0x55, 0xFF),   Color.FromRgb(0x55, 0xFF, 0x55),   Color.FromRgb(0x55, 0xFF, 0xFF),
                Color.FromRgb(0xFF, 0x55, 0x55),   Color.FromRgb(0xFF, 0x55, 0xFF),   Color.FromRgb(0xFF, 0xFF, 0x55),   Color.FromRgb(0xFF, 0xFF, 0xFF)
            };

        public ImageDumper(GameInfo game)
        {
            Game = game;
        }

        public void DumpImages(ResourceManager index)
        {
            foreach (var room in index.Rooms)
            {
                if (room.Image == null && room.Data == null)
                    continue;

                var name = room.Name ?? "room_" + room.Number;
                Console.WriteLine(name);

                try
                {
                    var gdi = new Gdi(null, Game);
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
                tableEGAPalette[index] : room.Palette.Colors[index];
            return System.Drawing.Color.FromArgb(c.R, c.G, c.B);
        }

        System.Drawing.Bitmap ToBitmap(Room room, VirtScreen screen)
        {
            var bmp = new System.Drawing.Bitmap(screen.Width, screen.Height);
            for (int x = 0; x < screen.Width; x++)
            {
                for (int y = 0; y < screen.Height; y++)
                {
                    bmp.SetPixel(x, y, ToColor(room, screen, x, y));
                }
            }
            return bmp;
        }

        void DumpRoomImage(Room room, Gdi gdi)
        {
            var name = room.Name ?? "room_" + room.Number;

            var screen2 = new VirtScreen(0, room.Header.Width, room.Header.Height, PixelFormat.Indexed8, 2);
            var numStrips = room.Header.Width / 8;
            if (room.Data != null)
            {
                gdi.DrawBitmap(room.Data, screen2, 0, 0, room.Header.Width, room.Header.Height, 0, numStrips, 0, true, room.Header.Width);
            }
            else
            {
                gdi.DrawBitmap(room.Image, screen2, 0, 0, room.Header.Width, room.Header.Height, 0, numStrips, room.Header.Width, 0, true);
            }
            var bmpRoom = ToBitmap(room, screen2);
            bmpRoom.Save("bg_" + name + ".png");

            DumpZPlanes(room, name);
        }

        static void DumpZPlanes(Room room, string name)
        {
            if (room.Image != null)
            {
                for (int i = 1; i < room.Image.ZPlanes.Count; i++)
                {
                    var zplane = room.Image.ZPlanes[i];
                    var nStrips = room.Header.Width / 8;
                    var pixels = new byte[nStrips * room.Header.Height];
                    var pn = new PixelNavigator(pixels, nStrips, 1);
                    using (var ms = new MemoryStream(zplane.Data))
                    {
                        var binZplane = new BinaryReader(ms);
                        var offsets = binZplane.ReadUInt16s(nStrips);
                        for (int nStrip = 0; nStrip < nStrips; nStrip++)
                        {
                            var offset = offsets[nStrip] - 8;
                            ms.Seek(offset, SeekOrigin.Begin);
                            pn.GoTo(nStrip, 0);
                            DecompressMaskImg(pn, ms, room.Header.Height);
                        }
                    }
                    var bmpZ = new System.Drawing.Bitmap(nStrips * 8, room.Header.Height);
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
                    bmpZ.Save("bg_" + name + "_z" + i + ".png");
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
                var text = new ScummText(obj.Name);
                var sb = new StringBuilder();
                sb.AppendLine("Object #" + obj.Number);
                sb.Append("  ");
                var decoder = new TextDecoder(sb);
                text.Decode(decoder);
                Console.WriteLine(sb);
                if (obj.Images.Count == 0 && (obj.Image == null || obj.Image.Length == 0))
                    continue;
                if (obj.Images.Count == 0)
                {
                    var screen = new VirtScreen(0, obj.Width, obj.Height, PixelFormat.Indexed8, 2);
                    gdi.DrawBitmap(obj.Image, screen, 0, 0, obj.Width, obj.Height, 0, obj.Width / 8, 0, true, room.Header.Width);
                    var bmp = ToBitmap(room, screen);
                    bmp.Save("obj_" + obj.Number + ".png");
                }
                else
                {
                    var j = 0;
                    foreach (var img in obj.Images)
                    {
                        var screen = new VirtScreen(0, obj.Width, obj.Height, PixelFormat.Indexed8, 2);
                        gdi.DrawBitmap(img, screen, 0, 0, obj.Width, obj.Height, 0, obj.Width / 8, room.Header.Width, 0, true);
                        var bmp = ToBitmap(room, screen);
                        bmp.Save("obj_" + obj.Number + "_" + (++j) + ".png");
                    }
                }
            }
        }
    }
}

