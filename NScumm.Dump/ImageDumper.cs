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

namespace NScumm.Tmp
{
    public class ImageDumper
    {
        GameInfo Game
        {
            get;
            set;
        }

        public ImageDumper(GameInfo game)
        {
            Game = game;
        }

        static byte[] tableEGAPalette = new byte []
        {
            0x00, 0x00, 0x00,   0x00, 0x00, 0xAA,   0x00, 0xAA, 0x00,   0x00, 0xAA, 0xAA,
            0xAA, 0x00, 0x00,   0xAA, 0x00, 0xAA,   0xAA, 0x55, 0x00,   0xAA, 0xAA, 0xAA,
            0x55, 0x55, 0x55,   0x55, 0x55, 0xFF,   0x55, 0xFF, 0x55,   0x55, 0xFF, 0xFF,
            0xFF, 0x55, 0x55,   0xFF, 0x55, 0xFF,   0xFF, 0xFF, 0x55,   0xFF, 0xFF, 0xFF
        };

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
                Color.FromRgb(tableEGAPalette[index * 3], tableEGAPalette[index * 3 + 1], tableEGAPalette[index * 3 + 2]) :
                room.Palette.Colors[index];
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

        public void DumpImages(ResourceManager index)
        {
            foreach (var room in index.Rooms)
            {
                if (room.Data == null)
                    continue;

                var name = room.Name ?? "room_" + room.Number;
                Console.WriteLine(name);

                try
                {
                    var gdi = new Gdi(null, Game);
                    gdi.RoomPalette = CreatePalette();

                    //              foreach (var obj in room.Objects) {
                    //                  if (obj.Name.Length == 0)
                    //                      continue;
                    //
                    //                  var text = new ScummText (obj.Name);
                    //                  var sb = new StringBuilder ();
                    //                  sb.AppendLine ("Object #" + obj.Number);
                    //                  sb.Append ("  ");
                    //                  var decoder = new TextDecoder (sb);
                    //                  text.Decode (decoder);
                    //                  Console.WriteLine (sb);
                    //
                    //                  if (obj.Image.Length == 0)
                    //                      continue;
                    //                  var screen = new VirtScreen (0, obj.Width, obj.Height, PixelFormat.Indexed8, 2);
                    //                  //byte[] ptr, VirtScreen vs, int x, int y, int width, int height, int stripnr, int numstrip, DrawBitmaps flags)
                    //                  gdi.DrawBitmap (obj.Image, screen, 0, 0, obj.Width, obj.Height, 0, obj.Width / 8, 0);
                    //                  var bmp = ToBitmap (room, screen);
                    //                  bmp.Save ("obj_" + obj.Number + ".png");
                    //              }

                    var screen2 = new VirtScreen(0, room.Header.Width, room.Header.Height, 
                                      PixelFormat.Indexed8, 2);

                    var numStrips = room.Header.Width / 8;
                    gdi.DrawBitmap(room.Data, screen2, 0, 0, room.Header.Width, room.Header.Height, 0, numStrips, 0, true, room.Header.Width);

                    var bmpRoom = ToBitmap(room, screen2);
                    bmpRoom.Save("bg_" + name + ".png");
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
}

