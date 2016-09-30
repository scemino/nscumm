//
//  IFFDecoder.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NScumm.Core.Graphics;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Core.Image
{
    public class IFFDecoder
    {
        struct Header
        {
            public ushort width, height;
            public ushort x, y;
            public byte numPlanes;
            public byte masking;
            public byte compression;
            public byte flags;
            public ushort transparentColor;
            public byte xAspect, yAspect;
            public ushort pageWidth, pageHeight;
        }

        struct PaletteRange
        {
            public short timer, step, flags;
            public byte first, last;
        }

        enum Type
        {
            UNKNOWN = 0,
            ILBM,
            PBM
        }

        /// <summary>
        /// EA IFF 85 group identifier.
        /// </summary>
        private static readonly uint ID_FORM = ScummHelper.MakeTag('F', 'O', 'R', 'M');
        /// <summary>
        /// EA IFF 85 raster bitmap form.
        /// </summary>
        private static readonly uint ID_ILBM = ScummHelper.MakeTag('I', 'L', 'B', 'M');
        /// <summary>
        /// 256-color chunky format (DPaint 2 ?).
        /// </summary>
        private static readonly uint ID_PBM = ScummHelper.MakeTag('P', 'B', 'M', ' ');

        /// <summary>
        /// IFF BitmapHeader.
        /// </summary>
        private static readonly uint ID_BMHD = ScummHelper.MakeTag('B', 'M', 'H', 'D');
        /// <summary>
        /// IFF 8bit RGB colormap.
        /// </summary>
        private static readonly uint ID_CMAP = ScummHelper.MakeTag('C', 'M', 'A', 'P');
        /// <summary>
        /// IFF image data.
        /// </summary>
        private static readonly uint ID_BODY = ScummHelper.MakeTag('B', 'O', 'D', 'Y');

        /// <summary>
        /// color cycling.
        /// </summary>
        private static readonly uint ID_CRNG = ScummHelper.MakeTag('C', 'R', 'N', 'G');

        public Surface Surface { get; private set; }

        public Color[] Palette { get; private set; }

        private Header _header;
        private List<PaletteRange> _paletteRanges;
        private Type _type;
        private byte _numRelevantPlanes;
        private bool _pixelPacking;

        public IFFDecoder()
        {
            // these 2 properties are not reset by destroy(), so the default is set here.
            _numRelevantPlanes = 8;
            _paletteRanges = new List<PaletteRange>();
        }

        public bool LoadStream(Stream stream)
        {
            var br = new BinaryReader(stream);
            uint form = br.ReadUInt32BigEndian();

            if (form != ID_FORM)
            {
                D.Warning("Failed reading IFF-file");
                return false;
            }

            stream.Seek(4, SeekOrigin.Current);

            uint type = br.ReadUInt32BigEndian();

            if (type == ID_ILBM)
            {
                _type = Type.ILBM;
            }
            else if (type == ID_PBM)
            {
                _type = Type.PBM;
            }

            if (type == (uint)Type.UNKNOWN)
            {
                D.Warning("Failed reading IFF-file");
                return false;
            }

            while ((stream.Position + 8) < stream.Length)
            {
                uint chunkType = br.ReadUInt32BigEndian();
                uint chunkSize = br.ReadUInt32BigEndian();

                if (stream.Position == stream.Length)
                    break;

                if (chunkType == ID_BMHD)
                {
                    LoadHeader(stream);
                }
                else if (chunkType == ID_CMAP)
                {
                    LoadPalette(stream, chunkSize);
                }
                else if (chunkType == ID_CRNG)
                {
                    LoadPaletteRange(stream, chunkSize);
                }
                else if (chunkType == ID_BODY)
                {
                    LoadBitmap(stream);
                }
                else
                {
                    stream.Seek(chunkSize, SeekOrigin.Current);
                }
            }

            return true;
        }

        private void LoadHeader(Stream stream)
        {
            var br = new BinaryReader(stream);
            _header.width = br.ReadUInt16BigEndian();
            _header.height = br.ReadUInt16BigEndian();
            _header.x = br.ReadUInt16BigEndian();
            _header.y = br.ReadUInt16BigEndian();
            _header.numPlanes = br.ReadByte();
            _header.masking = br.ReadByte();
            _header.compression = br.ReadByte();
            _header.flags = br.ReadByte();
            _header.transparentColor = br.ReadUInt16BigEndian();
            _header.xAspect = br.ReadByte();
            _header.yAspect = br.ReadByte();
            _header.pageWidth = br.ReadUInt16BigEndian();
            _header.pageHeight = br.ReadUInt16BigEndian();

            Debug.Assert(_header.width >= 1);
            Debug.Assert(_header.height >= 1);
            Debug.Assert(_header.numPlanes >= 1 && _header.numPlanes <= 8 && _header.numPlanes != 7);
        }

        private void LoadPalette(Stream stream, uint size)
        {
            Palette = new Color[size / 3];
            for (int i = 0; i < Palette.Length; i++)
            {
                Palette[i] = Color.FromRgb(stream.ReadByte(), stream.ReadByte(), stream.ReadByte());
            }
        }

        private void LoadPaletteRange(Stream stream, uint size)
        {
            PaletteRange range;
            var br = new BinaryReader(stream);
            range.timer = br.ReadInt16BigEndian();
            range.step = br.ReadInt16BigEndian();
            range.flags = br.ReadInt16BigEndian();
            range.first = br.ReadByte();
            range.last = br.ReadByte();

            _paletteRanges.Add(range);
        }

        private void LoadBitmap(Stream stream)
        {
            _numRelevantPlanes = Math.Min(_numRelevantPlanes, _header.numPlanes);

            if (_numRelevantPlanes != 1 && _numRelevantPlanes != 2 && _numRelevantPlanes != 4)
                _pixelPacking = false;

            ushort outPitch = _header.width;

            if (_pixelPacking)
                outPitch = (ushort)(outPitch / (8 / _numRelevantPlanes));

            // FIXME: CLUT8 is not a proper format for packed bitmaps but there is no way to tell it to use 1, 2 or 4 bits per pixel
            Surface = new Surface(outPitch, _header.height, PixelFormat.Indexed8);

            if (_type == Type.ILBM)
            {
                uint scanlinePitch = (uint)(((_header.width + 15) >> 4) << 1);
                byte[] scanlines = new byte[scanlinePitch * _header.numPlanes];
                var data = Surface.Pixels;
                int d = 0;

                for (ushort i = 0; i < _header.height; ++i)
                {
                    int s = 0;

                    for (ushort j = 0; j < _header.numPlanes; ++j)
                    {
                        ushort outSize = (ushort)scanlinePitch;

                        if (_header.compression != 0)
                        {
                            var packStream = new PackBitsReadStream(stream);
                            packStream.Read(scanlines, s, outSize);
                        }
                        else
                        {
                            stream.Read(scanlines, s, outSize);
                        }

                        s += outSize;
                    }

                    PackPixels(scanlines, data.Data, data.Offset+ d, (ushort)scanlinePitch, outPitch);
                    d += outPitch;
                }
            }
            else if (_type == Type.PBM)
            {
                var data = Surface.Pixels;
                uint outSize = (uint)(_header.width * _header.height);

                if (_header.compression != 0)
                {
                    var packStream = new PackBitsReadStream(stream);
                    packStream.Read(data.Data, data.Offset, (int)outSize);
                }
                else
                {
                    stream.Read(data.Data, data.Offset, (int)outSize);
                }
            }
        }

        private void PackPixels(byte[] scanlines, byte[] data, int d, ushort scanlinePitch, ushort outPitch)
        {
            uint numPixels = _header.width;

            if (_pixelPacking)
                numPixels = (uint)(outPitch * (8 / _numRelevantPlanes));

            for (int x = 0; x < numPixels; ++x)
            {
                byte[] scanline = scanlines;
                int s = 0;
                byte pixel = 0;
                byte offset = (byte)(x >> 3);
                byte bit = (byte)(0x80 >> (x & 7));

                // first build a pixel by scanning all the usable planes in the input
                for (int plane = 0; plane < _numRelevantPlanes; ++plane)
                {
                    if ((scanline[s + offset] & bit) != 0)
                        pixel |= (byte)(1 << plane);

                    s += scanlinePitch;
                }

                // then output the pixel according to the requested packing
                if (!_pixelPacking)
                    data[d + x] = pixel;
                else if (_numRelevantPlanes == 1)
                    data[d + x / 8] |= (byte)(pixel << (x & 7));
                else if (_numRelevantPlanes == 2)
                    data[d + x / 4] |= (byte)(pixel << ((x & 3) << 1));
                else if (_numRelevantPlanes == 4)
                    data[d + x / 2] |= (byte)(pixel << ((x & 1) << 2));
            }
        }
    }
}

