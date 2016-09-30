//
//  PCXDecoder.cs
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

using System;
using System.IO;
using NScumm.Core.Graphics;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Core.Image
{
	public class PCXDecoder
	{
		public Surface Surface { get; private set;}

		public Color[] Palette { get; private set;}

		public bool LoadStream (Stream stream)
		{
			var br = new BinaryReader (stream);
			if (br.ReadByte () != 0x0a)	// ZSoft PCX
				return false;

			byte version = br.ReadByte ();	// 0 - 5
			if (version > 5)
				return false;

			bool compressed = br.ReadByte () != 0; // encoding, 1 = run length encoding
			byte bitsPerPixel = br.ReadByte ();	// 1, 2, 4 or 8

			// Window
			ushort xMin = br.ReadUInt16 ();
			ushort yMin = br.ReadUInt16 ();
			ushort xMax = br.ReadUInt16 ();
			ushort yMax = br.ReadUInt16 ();

			ushort width = (ushort)(xMax - xMin + 1);
			ushort height = (ushort)(yMax - yMin + 1);

			if (xMax < xMin || yMax < yMin) {
				D.Warning("Invalid PCX image dimensions");
				return false;
			}

			stream.Seek (4, SeekOrigin.Current);	// HDpi, VDpi

			// Read the EGA palette (colormap)
			Palette = new Color[16];
			for (var i = 0; i < 16; i++) {
				Palette [i] = Color.FromRgb(br.ReadByte(),br.ReadByte(), br.ReadByte ());
			}

			if (br.ReadByte () != 0)	// reserved, should be set to 0
				return false;

			byte nPlanes = br.ReadByte ();
			ushort bytesPerLine = br.ReadUInt16 ();
			ushort bytesPerscanLine = (ushort)(nPlanes * bytesPerLine);

			if (bytesPerscanLine < width * bitsPerPixel * nPlanes / 8) {
				D.Warning("PCX data is corrupted");
				return false;
			}

			stream.Seek (60, SeekOrigin.Current);	// PaletteInfo, HscreenSize, VscreenSize, Filler

			byte[] scanLine = new byte[bytesPerscanLine];
			BytePtr dst;
			int dstPos = 0;
			int x, y;

			if (nPlanes == 3 && bitsPerPixel == 8) {	// 24bpp
				Surface = new Surface (width, height, PixelFormat.Rgb24);
				dst = Surface.Pixels;

				for (y = 0; y < height; y++) {
					DecodeRle (br, scanLine, bytesPerscanLine, compressed);

					for (x = 0; x < width; x++) {
						byte b = scanLine [x];
						byte g = scanLine [x + bytesPerLine];
						byte r = scanLine [x + (bytesPerLine << 1)];
						uint color = ColorHelper.RGBToColor24 (r, g, b);

						dst.WriteUInt32 (dstPos, color);
						dstPos += Surface.BytesPerPixel;
					}
				}
			} else if (nPlanes == 1 && bitsPerPixel == 8) {	// 8bpp indexed
				Surface = new Surface (width, height, PixelFormat.Indexed8);
				dst = Surface.Pixels;

				for (y = 0; y < height; y++, dstPos += Surface.Pitch) {
					DecodeRle (br, scanLine, bytesPerscanLine, compressed);
					Array.Copy (scanLine, 0, dst.Data, dst.Offset + dstPos, width);
				}

				if (version == 5) {
					if (stream.ReadByte () != 12) {
						D.Warning("Expected a palette after the PCX image data");
						return false;
					}

					// Read the VGA palette
					Palette = new Color[256];
					for (var i = 0; i < 256; i++) {
						Palette [i] = Color.FromRgb(br.ReadByte(),br.ReadByte(), br.ReadByte ());
					}
				}
			} else if ((nPlanes == 2 || nPlanes == 3 || nPlanes == 4) && bitsPerPixel == 1) {	// planar, 4, 8 or 16 colors
				Surface = new Surface (width, height, PixelFormat.Indexed8);
				dst = Surface.Pixels;

				for (y = 0; y < height; y++, dstPos += Surface.Pitch) {
					DecodeRle (br, scanLine, bytesPerscanLine, compressed);

					for (x = 0; x < width; x++) {
						int m = 0x80 >> (x & 7), v = 0;
						for (int i = nPlanes - 1; i >= 0; i--) {
							v <<= 1;
							v += (scanLine [i * bytesPerLine + (x >> 3)] & m) == 0 ? 0 : 1;
						}
						dst [dstPos + x] = (byte)v;
					}
				}
			} else {
				// Known unsupported case: 1 plane and bpp < 8 (1, 2 or 4)
				D.Warning("Invalid PCX file (%d planes, %d bpp)", nPlanes, bitsPerPixel);
				return false;
			}

			return true;
		}

		private static void DecodeRle (BinaryReader stream, byte[] dst, int bytesPerscanLine, bool compressed)
		{
			var i = 0;

		    if (compressed) {
				while (i < bytesPerscanLine) {
					byte run = 1;
					var value = stream.ReadByte ();
					if (value >= 0xc0) {
						run = (byte)(value & 0x3f);
						value = stream.ReadByte ();
					}
					while (i < bytesPerscanLine && (run-- != 0))
						dst [i++] = value;
				}
			} else {
				stream.BaseStream.Read (dst, 0, bytesPerscanLine);
			}
		}
	}
}

