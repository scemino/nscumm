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
using System.IO;
using NScumm.Core.Graphics;

namespace NScumm.Core.Video
{
    class SmackerVideoTrack : FixedRateVideoTrack
    {
        const int SmkBlockMono = 0;
        const int SmkBlockFull = 1;
        const int SmkBlockSkip = 2;
        const int SmkBlockFill = 3;

        private readonly Surface _surface;
        private readonly uint _frameCount;
        private readonly Rational _frameRate;
        private readonly uint _flags;
        private readonly uint _signature;
        private int _curFrame;
        private bool _dirtyPalette;
        private readonly byte[] _palette;
        private BigHuffmanTree _mMapTree;
        private BigHuffmanTree _mClrTree;
        private BigHuffmanTree _fullTree;
        private BigHuffmanTree _typeTree;

        public SmackerVideoTrack(uint width, uint height, uint frameCount, Rational frameRate, uint flags, uint signature)
        {
            _surface = new Surface((int)width, (int)(height * (flags != 0 ? 2 : 1)), PixelFormat.Indexed8, false);
            _frameCount = frameCount;
            _frameRate = frameRate;
            _flags = flags;
            _signature = signature;
            _curFrame = -1;
            _dirtyPalette = false;
            _palette = new byte[3 * 256];
        }

        public override ushort Width { get { return (ushort)_surface.Width; } }

        public override ushort Height { get { return (ushort)_surface.Height; } }

        public override PixelFormat PixelFormat
        {
            get { return _surface.PixelFormat; }
        }

        public override bool IsRewindable
        {
            get { return true; }
        }

        public override bool Rewind()
        {
            _curFrame = -1;
            return true;
        }

        public override int CurrentFrame
        {
            get { return _curFrame; }
        }

        public override int FrameCount { get { return (int)_frameCount; } }

        public override Surface DecodeNextFrame()
        {
            return _surface;
        }

        public override byte[] GetPalette()
        {
            _dirtyPalette = false;
            return _palette;
        }

        public override bool HasDirtyPalette()
        {
            return _dirtyPalette;
        }

        protected override Rational FrameRate { get { return _frameRate; } }

        public void ReadTrees(BitStream bs, int mMapSize, int mClrSize, int fullSize, int typeSize)
        {
            _mMapTree = new BigHuffmanTree(bs, mMapSize);
            _mClrTree = new BigHuffmanTree(bs, mClrSize);
            _fullTree = new BigHuffmanTree(bs, fullSize);
            _typeTree = new BigHuffmanTree(bs, typeSize);
        }

        public void IncreaseCurrentFrame()
        {
            _curFrame++;
        }

        public void UnpackPalette(Stream stream)
        {
            uint startPos = (uint)stream.Position;
            int len = 4 * stream.ReadByte();

            byte[] chunk = new byte[len];
            stream.Read(chunk, 0, len);
            var p = 0;

            byte[] oldPalette = new byte[3 * 256];
            Array.Copy(_palette, oldPalette, 3 * 256);

            var pal = 0;

            int sz = 0;
            byte b0;
            while (sz < 256)
            {
                b0 = chunk[p++];
                if ((b0 & 0x80) != 0)
                {               // if top bit is 1 (0x80 = 10000000)
                    sz += (b0 & 0x7f) + 1;     // get lower 7 bits + 1 (0x7f = 01111111)
                    pal += 3 * ((b0 & 0x7f) + 1);
                }
                else if ((b0 & 0x40) != 0)
                {        // if top 2 bits are 01 (0x40 = 01000000)
                    byte c = (byte)((b0 & 0x3f) + 1);  // get lower 6 bits + 1 (0x3f = 00111111)
                    uint s = (uint)(3 * chunk[p++]);
                    sz += c;

                    while (c-- != 0)
                    {
                        _palette[pal++] = oldPalette[s + 0];
                        _palette[pal++] = oldPalette[s + 1];
                        _palette[pal++] = oldPalette[s + 2];
                        s += 3;
                    }
                }
                else
                {                       // top 2 bits are 00
                    sz++;
                    // get the lower 6 bits for each component (0x3f = 00111111)
                    byte r = (byte)(b0 & 0x3f);
                    byte g = (byte)(chunk[p++] & 0x3f);
                    byte b = (byte)(chunk[p++] & 0x3f);

                    // upscale to full 8-bit color values. The Multimedia Wiki suggests
                    // a lookup table for this, but this should produce the same result.
                    _palette[pal++] = (byte)(r * 4 + r / 16);
                    _palette[pal++] = (byte)(g * 4 + g / 16);
                    _palette[pal++] = (byte)(b * 4 + b / 16);
                }
            }

            stream.Seek(startPos + len, SeekOrigin.Begin);

            _dirtyPalette = true;
        }

        public void DecodeFrame(BitStream bs)
        {
            _mMapTree.Reset();
            _mClrTree.Reset();
            _fullTree.Reset();
            _typeTree.Reset();

            // Height needs to be doubled if we have flags (Y-interlaced or Y-doubled)
            uint doubleY = (uint)(_flags != 0 ? 2 : 1);

            uint bw = (uint)(Width / 4);
            uint bh = Height / doubleY / 4;
            int stride = Width;
            uint block = 0, blocks = bw * bh;

            uint type, run, j, mode;
            uint p1, p2, clr, map;
            byte hi, lo;
            uint i;
            int bOut;
            byte[] pixels = _surface.Pixels;
            while (block < blocks)
            {
                type = _typeTree.GetCode(bs);
                run = GetBlockRun((int) ((type >> 2) & 0x3f));

                switch (type & 3)
                {
                    case SmkBlockMono:
                        while (run-- != 0 && block < blocks)
                        {
                            clr = _mClrTree.GetCode(bs);
                            map = _mMapTree.GetCode(bs);

                            bOut = (int)((block / bw) * (stride * 4 * doubleY) + (block % bw) * 4);
                            hi = (byte)(clr >> 8);
                            lo = (byte)(clr & 0xff);
                            for (i = 0; i < 4; i++)
                            {
                                for (j = 0; j < doubleY; j++)
                                {
                                    pixels[bOut] = (map & 1) != 0 ? hi : lo;
                                    pixels[bOut + 1] = (map & 2) != 0 ? hi : lo;
                                    pixels[bOut + 2] = (map & 4) != 0 ? hi : lo;
                                    pixels[bOut + 3] = (map & 8) != 0 ? hi : lo;
                                    bOut += stride;
                                }
                                map >>= 4;
                            }
                            ++block;
                        }
                        break;
                    case SmkBlockFull:
                        // Smacker v2 has one mode, Smacker v4 has three
                        if (_signature == ScummHelper.MakeTag('S', 'M', 'K', '2'))
                        {
                            mode = 0;
                        }
                        else
                        {
                            // 00 - mode 0
                            // 10 - mode 1
                            // 01 - mode 2
                            mode = 0;
                            if (bs.GetBit() != 0)
                            {
                                mode = 1;
                            }
                            else if (bs.GetBit() != 0)
                            {
                                mode = 2;
                            }
                        }

                        while (run-- != 0 && block < blocks)
                        {
                            bOut = (int)((block / bw) * (stride * 4 * doubleY) + (block % bw) * 4);
                            switch (mode)
                            {
                                case 0:
                                    for (i = 0; i < 4; ++i)
                                    {
                                        p1 = _fullTree.GetCode(bs);
                                        p2 = _fullTree.GetCode(bs);
                                        for (j = 0; j < doubleY; ++j)
                                        {
                                            pixels[bOut + 2] = (byte)(p1 & 0xff);
                                            pixels[bOut + 3] = (byte)(p1 >> 8);
                                            pixels[bOut + 0] = (byte)(p2 & 0xff);
                                            pixels[bOut + 1] = (byte)(p2 >> 8);
                                            bOut += stride;
                                        }
                                    }
                                    break;
                                case 1:
                                    p1 = _fullTree.GetCode(bs);
                                    pixels[bOut + 0] = pixels[bOut + 1] = (byte)(p1 & 0xFF);
                                    pixels[bOut + 2] = pixels[bOut + 3] = (byte)(p1 >> 8);
                                    bOut += stride;
                                    pixels[bOut + 0] = pixels[bOut + 1] = (byte)(p1 & 0xFF);
                                    pixels[bOut + 2] = pixels[bOut + 3] = (byte)(p1 >> 8);
                                    bOut += stride;
                                    p2 = _fullTree.GetCode(bs);
                                    pixels[bOut + 0] = pixels[bOut + 1] = (byte)(p2 & 0xFF);
                                    pixels[bOut + 2] = pixels[bOut + 3] = (byte)(p2 >> 8);
                                    bOut += stride;
                                    pixels[bOut + 0] = pixels[bOut + 1] = (byte)(p2 & 0xFF);
                                    pixels[bOut + 2] = pixels[bOut + 3] = (byte)(p2 >> 8);
                                    bOut += stride;
                                    break;
                                case 2:
                                    for (i = 0; i < 2; i++)
                                    {
                                        // We first get p2 and then p1
                                        // Check ffmpeg thread "[PATCH] Smacker video decoder bug fix"
                                        // http://article.gmane.org/gmane.comp.video.ffmpeg.devel/78768
                                        p2 = _fullTree.GetCode(bs);
                                        p1 = _fullTree.GetCode(bs);
                                        for (j = 0; j < doubleY; ++j)
                                        {
                                            pixels[bOut + 0] = (byte)(p1 & 0xff);
                                            pixels[bOut + 1] = (byte)(p1 >> 8);
                                            pixels[bOut + 2] = (byte)(p2 & 0xff);
                                            pixels[bOut + 3] = (byte)(p2 >> 8);
                                            bOut += stride;
                                        }
                                        for (j = 0; j < doubleY; ++j)
                                        {
                                            pixels[bOut + 0] = (byte)(p1 & 0xff);
                                            pixels[bOut + 1] = (byte)(p1 >> 8);
                                            pixels[bOut + 2] = (byte)(p2 & 0xff);
                                            pixels[bOut + 3] = (byte)(p2 >> 8);
                                            bOut += stride;
                                        }
                                    }
                                    break;
                            }
                            ++block;
                        }
                        break;
                    case SmkBlockSkip:
                        while (run-- != 0 && block < blocks)
                            block++;
                        break;
                    case SmkBlockFill:
                        uint col;
                        mode = type >> 8;
                        while (run-- != 0 && block < blocks)
                        {
                            bOut = (int)((block / bw) * (stride * 4 * doubleY) + (block % bw) * 4);
                            col = mode * 0x01010101;
                            for (i = 0; i < 4 * doubleY; ++i)
                            {
                                pixels[bOut + 0] = pixels[bOut + 1] = pixels[bOut + 2] = pixels[bOut + 3] = (byte)col;
                                bOut += stride;
                            }
                            ++block;
                        }
                        break;
                }
            }
        }

        private static uint GetBlockRun(int index)
        {
            return index <= 58 ? (uint)index + 1 : (uint)(128 << index - 59);
        }
    }
}
