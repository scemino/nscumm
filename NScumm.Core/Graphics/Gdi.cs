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
using System.Collections.Generic;
using System.IO;
using NScumm.Core.IO;

namespace NScumm.Core.Graphics
{
    [Flags]
    public enum DrawBitmaps
    {
        AllowMaskOr = 1 << 0,
        DrawMaskOnAll = 1 << 1,
        ObjectMode = 2 << 2
    }

    public class Gdi
    {
        #region Fields

        public int NumZBuffer = 2;
        public int NumStrips = 40;

        readonly ScummEngine _vm;
        GameInfo game;

        int paletteMod;
        byte decompShr;
        byte decompMask;
        byte transparentColor = 255;
        byte[][] maskBuffer = new byte[4][];
        byte[] roomPalette = new byte[256];

        #endregion

        #region Properties

        public byte[] RoomPalette
        {
            get{ return roomPalette; }
            set{ roomPalette = value; }
        }

        public byte TransparentColor
        {
            get { return transparentColor; }
            set { transparentColor = value; }
        }

        public bool IsZBufferEnabled { get; set; }

        #endregion

        #region Constructor

        public Gdi(ScummEngine vm, GameInfo game)
        {
            _vm = vm;
            this.game = game;
            for (int i = 0; i < maskBuffer.Length; i++)
            {
                maskBuffer[i] = new byte[40 * (200 + 4)];
            }
            IsZBufferEnabled = true;
            _gfxUsageBits = new uint[410 * 3];
        }

        #endregion

        #region Public Methods

        public void Init()
        {
            NumStrips = _vm.ScreenWidth / 8;
        }

        public void DrawBitmap(byte[] ptr, VirtScreen vs, int x, int y, int width, int height, int stripnr, int numstrip, DrawBitmaps flags)
        {
            // Check whether lights are turned on or not
            var lightsOn = _vm.IsLightOn();
            DrawBitmap(ptr, vs, x, y, width, height, stripnr, numstrip, flags, lightsOn, _vm.CurrentRoomData.Header.Width);
        }

        /// <summary>
        /// Draw a bitmap onto a virtual screen. This is main drawing method for room backgrounds
        /// and objects, used throughout all SCUMM versions.
        /// </summary>
        public void DrawBitmap(byte[] ptr, VirtScreen vs, int x, int y, int width, int height, int stripnr, int numstrip, DrawBitmaps flags, bool isLightOn, int roomWidth)
        {
            int sx = x - vs.XStart / 8;
            if (sx < 0)
            {
                numstrip -= -sx;
                x += -sx;
                stripnr += -sx;
                sx = 0;
            }

            // Compute the number of strips we have to iterate over.
            // TODO/FIXME: The computation of its initial value looks very fishy.
            // It was added as a kind of hack to fix some corner cases, but it compares
            // the room width to the virtual screen width; but the former should always
            // be bigger than the latter (except for MM NES, maybe)... strange
            int limit = Math.Max(roomWidth, vs.Width) / 8 - x;
            if (limit > numstrip)
                limit = numstrip;
            if (limit > NumStrips - sx)
                limit = NumStrips - sx;

            for (int k = 0; k < limit; ++k, ++stripnr, ++sx, ++x)
            {
                if (y < vs.TDirty[sx])
                    vs.TDirty[sx] = y;

                if (y + height > vs.BDirty[sx])
                    vs.BDirty[sx] = y + height;

                // In the case of a double buffered virtual screen, we draw to
                // the backbuffer, otherwise to the primary surface memory.
                PixelNavigator navDst;
                if (vs.HasTwoBuffers)
                {
                    navDst = new PixelNavigator(vs.Surfaces[1]);
                    navDst.GoTo(x * 8, y);
                }
                else
                {
                    navDst = new PixelNavigator(vs.Surfaces[0]);
                    navDst.GoTo(x * 8, y);
                }

                var smapReader = new BinaryReader(new MemoryStream(ptr));
                bool transpStrip = DrawStrip(navDst, height, stripnr, smapReader);

                if (vs.HasTwoBuffers)
                {
                    var navFrontBuf = new PixelNavigator(vs.Surfaces[0]);
                    navFrontBuf.GoTo(x * 8, y);
                    if (isLightOn)
                        Copy8Col(navFrontBuf, navDst, height);
                    else
                        Clear8Col(navFrontBuf, height);
                }

                var zplanes = GetZPlanes(ptr);
                DecodeMask(x, y, height, stripnr, zplanes, transpStrip, flags);

            }
        }

        public static void Fill(byte[] dst, int dstPitch, byte color, int w, int h)
        {
            if (w == dstPitch)
            {
                for (int i = 0; i < dst.Length; i++)
                {
                    dst[i] = color;
                }
            }
            else
            {
                int offset = 0;
                do
                {
                    for (int i = 0; i < w; i++)
                    {
                        dst[offset + i] = color;
                    }
                    offset += dstPitch;
                } while ((--h) != 0);
            }
        }

        public static void Blit(PixelNavigator dst, PixelNavigator src, int width, int height)
        {
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    dst.Write(src.Read());
                    src.OffsetX(1);
                    dst.OffsetX(1);
                }
                src.Offset(-width, 1);
                dst.Offset(-width, 1);
            }
        }

        public static void Fill(PixelNavigator dst, byte color, int width, int height)
        {
            for (int h = 0; h < height; h++)
            {
                for (int w = 0; w < width; w++)
                {
                    dst.Write(color);
                    dst.OffsetX(1);
                }
                dst.Offset(-width, 1);
            }
        }

        public PixelNavigator GetMaskBuffer(int x, int y, int i)
        {
            var nav = new PixelNavigator(maskBuffer[i], 40, 1);
            nav.GoTo(x, y);
            return nav;
        }

        public void ResetBackground(int top, int bottom, int strip)
        {
            var vs = _vm.MainVirtScreen;

            if (top < 0)
                top = 0;

            if (bottom > vs.Height)
                bottom = vs.Height;

            if (top >= bottom)
                return;

            if (strip < 0 || strip >= NumStrips)
                throw new ArgumentOutOfRangeException("strip");

            if (top < vs.TDirty[strip])
                vs.TDirty[strip] = top;

            if (bottom > vs.BDirty[strip])
                vs.BDirty[strip] = bottom;

            int numLinesToProcess = bottom - top;
            if (numLinesToProcess > 0)
            {
                var navDest = new PixelNavigator(vs.Surfaces[0]);
                navDest.GoTo(strip * 8 + vs.XStart, top);
                if (_vm.IsLightOn())
                {
                    var bgBakNav = new PixelNavigator(vs.Surfaces[1]);
                    bgBakNav.GoTo(strip * 8 + vs.XStart, top);
                    Copy8Col(navDest, bgBakNav, numLinesToProcess);
                }
                else
                {
                    Clear8Col(navDest, numLinesToProcess);
                }
            }
        }

        #endregion

        #region GfxUsageBit Members

        public const int UsageBitDirty = 96;
        public const int UsageBitRestored = 95;

        /// <summary>
        /// For each of the 410 screen strips, gfxUsageBits contains a
        /// bitmask. The lower 80 bits each correspond to one actor and
        /// signify if any part of that actor is currently contained in
        /// that strip.
        ///
        /// If the leftmost bit is set, the strip (background) is dirty
        /// needs to be redrawn.
        ///
        /// The second leftmost bit is set by removeBlastObject() and
        /// restoreBackground(), but I'm not yet sure why.
        /// </summary>
        uint[] _gfxUsageBits;

        public void SetGfxUsageBit(int strip, int bit)
        {
            if (strip < 0 || strip >= (_gfxUsageBits.Length / 3))
                throw new ArgumentOutOfRangeException("strip");
            if (bit < 1 || bit > 96)
                throw new ArgumentOutOfRangeException("bit");
            bit--;
            _gfxUsageBits[3 * strip + bit / 32] |= (uint)((1 << bit % 32));
        }

        public void ClearGfxUsageBits()
        {
            Array.Clear(_gfxUsageBits, 0, _gfxUsageBits.Length);
        }

        public void ClearGfxUsageBit(int strip, int bit)
        {
            if (strip < 0 || strip >= (_gfxUsageBits.Length / 3))
                throw new ArgumentOutOfRangeException("strip");
            if (bit < 1 || bit > 96)
                throw new ArgumentOutOfRangeException("bit");
            bit--;
            _gfxUsageBits[3 * strip + bit / 32] &= (uint)~(1 << (bit % 32));
        }

        public bool TestGfxUsageBit(int strip, int bit)
        {
            if (strip < 0 || strip >= (_gfxUsageBits.Length / 3))
                throw new ArgumentOutOfRangeException("strip");
            if (bit < 0 || bit > 96)
                throw new ArgumentOutOfRangeException("bit");
            bit--;
            return (_gfxUsageBits[3 * strip + bit / 32] & (1 << (bit % 32))) != 0;
        }

        public bool TestGfxOtherUsageBits(int strip, int bit)
        {
            // Don't exclude the DIRTY and RESTORED bits from the test
            var bitmask = new [] { 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF };

            ScummHelper.AssertRange(1, bit, 96, "TestGfxOtherUsageBits");
            bit--;
            bitmask[bit / 32] &= (uint)(~(1 << (bit % 32)));

            for (int i = 0; i < 3; i++)
                if ((_gfxUsageBits[3 * strip + i] & bitmask[i]) != 0)
                    return true;

            return false;
        }

        public bool TestGfxAnyUsageBits(int strip)
        {
            // Exclude the DIRTY and RESTORED bits from the test
            var bitmask = new uint[] { 0xFFFFFFFF, 0xFFFFFFFF, 0x3FFFFFFF };

            ScummHelper.AssertRange(0, strip, _gfxUsageBits.Length / 3, "TestGfxOtherUsageBits");
            for (var i = 0; i < 3; i++)
                if ((_gfxUsageBits[3 * strip + i] & bitmask[i]) != 0)
                    return true;

            return false;
        }

        public void SaveOrLoad(Serializer serializer)
        {
            var entries = new[]
            {
                LoadAndSaveEntry.Create(reader => _gfxUsageBits = reader.ReadUInt32s(200), writer => writer.WriteUInt32s(_gfxUsageBits, 200), 8, 9),
                LoadAndSaveEntry.Create(reader => _gfxUsageBits = reader.ReadUInt32s(410), writer => writer.WriteUInt32s(_gfxUsageBits, 410), 10, 13),
                LoadAndSaveEntry.Create(reader => _gfxUsageBits = reader.ReadUInt32s(3 * 410), writer => writer.WriteUInt32s(_gfxUsageBits, 3 * 410), 14)
            };
            Array.ForEach(entries, entry => entry.Execute(serializer));
        }

        #endregion GfxUsageBit Members

        #region Private Methods

        static void DecodeMask(BinaryReader reader, IList<byte> mask, int offset, int width, int height)
        {
            int dstIndex = offset;
            byte c, b;
            while (height != 0)
            {
                b = reader.ReadByte();

                if ((b & 0x80) != 0)
                {
                    b &= 0x7F;
                    c = reader.ReadByte();

                    do
                    {
                        mask[dstIndex] = c;
                        dstIndex += width;
                        --height;
                    } while ((--b != 0) && (height != 0));
                }
                else
                {
                    do
                    {
                        mask[dstIndex] = reader.ReadByte();
                        dstIndex += width;
                        --height;
                    } while ((--b != 0) && (height != 0));
                }
            }
        }

        static void Clear8Col(PixelNavigator nav, int height)
        {
            do
            {
                for (int i = 0; i < 8; i++)
                {
                    nav.Write(0);
                    nav.OffsetX(1);
                }
                nav.Offset(-8, 1);
            } while ((--height) != 0);
        }

        static void Copy8Col(PixelNavigator navDst, PixelNavigator navSource, int height)
        {
            do
            {
                for (int i = 0; i < 8; i++)
                {
                    navDst.Write(navSource.Read());
                    navDst.OffsetX(1);
                    navSource.OffsetX(1);
                }
                navDst.Offset(-8, 1);
                navSource.Offset(-8, 1);
            } while ((--height) != 0);
        }

        void DecodeMask(int x, int y, int height, int stripnr, IList<byte[]> zplanes, bool transpStrip, DrawBitmaps flags)
        {
            int i;
            PixelNavigator mask_ptr;

            if (flags.HasFlag(DrawBitmaps.DrawMaskOnAll))
            {
                // Sam & Max uses dbDrawMaskOnAll for things like the inventory
                // box and the speech icons. While these objects only have one
                // mask, it should be applied to all the Z-planes in the room,
                // i.e. they should mask every actor.
                //
                // This flag used to be called dbDrawMaskOnBoth, and all it
                // would do was to mask Z-plane 0. (Z-plane 1 would also be
                // masked, because what is now the else-clause used to be run
                // always.) While this seems to be the only way there is to
                // mask Z-plane 0, this wasn't good enough since actors in
                // Z-planes >= 2 would not be masked.
                //
                // The flag is also used by The Dig and Full Throttle, but I
                // don't know what for. At the time of writing, these games
                // are still too unstable for me to investigate.

                //z_plane_ptr = (byte*)zplanes[1] + *(ushort*)(zplanes[1] + stripnr * 2 + 8);
                var zplaneStream = new MemoryStream(zplanes[1]);
                var binZplane = new BinaryReader(zplaneStream);
                binZplane.BaseStream.Seek(stripnr * 2 + 8, SeekOrigin.Begin);
                zplaneStream.Seek(binZplane.ReadUInt16(), SeekOrigin.Begin);
                for (i = 0; i < zplanes.Count; i++)
                {
                    mask_ptr = GetMaskBuffer(x, y, i);
                    if (transpStrip && flags.HasFlag(DrawBitmaps.AllowMaskOr))
                        DecompressMaskImgOr(mask_ptr, zplaneStream, height);
                    else
                        DecompressMaskImg(mask_ptr, zplaneStream, height);
                }
            }
            else
            {
                for (i = 1; i < zplanes.Count; i++)
                {
                    var zplanePtr = new MemoryStream(zplanes[i]);
                    if (game.IsOldBundle)
                    {
                        zplanePtr.Seek(stripnr * 2, SeekOrigin.Begin);
                    }
                    else if (game.Features.HasFlag(GameFeatures.Old256))
                    {
                        zplanePtr.Seek(stripnr * 2 + 4, SeekOrigin.Begin);
                    }
                    else
                    {
                        zplanePtr.Seek(stripnr * 2 + 2, SeekOrigin.Begin);
                    }
                    var br = new BinaryReader(zplanePtr);
                    uint offs = br.ReadUInt16();

                    mask_ptr = GetMaskBuffer(x, y, i);

                    if (offs != 0)
                    {
                        zplanePtr.Seek(offs, SeekOrigin.Begin);
                        if (transpStrip && flags.HasFlag(DrawBitmaps.AllowMaskOr))
                        {
                            DecompressMaskImgOr(mask_ptr, zplanePtr, height);
                        }
                        else
                        {
                            DecompressMaskImg(mask_ptr, zplanePtr, height);
                        }
                    }
                    else
                    {
                        if (!(transpStrip && flags.HasFlag(DrawBitmaps.AllowMaskOr)))
                            for (int h = 0; h < height; h++)
                            {
                                mask_ptr.OffsetY(1);
                                mask_ptr.Write(0);
                            }
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

        static void DecompressMaskImgOr(PixelNavigator dst, Stream src, int height)
        {
            byte b, c;

            while (height != 0)
            {
                b = (byte)src.ReadByte();

                if ((b & 0x80) != 0)
                {
                    b &= 0x7F;
                    c = (byte)src.ReadByte();

                    do
                    {
                        dst.Write((byte)(dst.Read() | c));
                        dst.OffsetY(1);
                        --height;
                    } while ((--b != 0) && (height != 0));
                }
                else
                {
                    do
                    {
                        dst.Write((byte)(dst.Read() | src.ReadByte()));
                        dst.OffsetY(1);
                        --height;
                    } while ((--b != 0) && (height != 0));
                }
            }
        }

        bool DrawStrip(PixelNavigator navDst, int height, int stripnr, BinaryReader smapReader)
        {
            // Do some input verification and make sure the strip/strip offset
            // are actually valid. Normally, this should never be a problem,
            // but if e.g. a savegame gets corrupted, we can easily get into
            // trouble here. See also bug #795214.
            int offset = -1;
            int smapLen;
            if (game.Features.HasFlag(GameFeatures.SixteenColors))
            {
                smapLen = smapReader.ReadInt16();
                if (stripnr * 2 + 2 < smapLen)
                {
                    smapReader.BaseStream.Seek(stripnr * 2, SeekOrigin.Current);
                    offset = smapReader.ReadInt16();
                }
            }
            else
            {
                smapLen = smapReader.ReadInt32();
                if (stripnr * 4 + 4 < smapLen)
                {
                    smapReader.BaseStream.Seek(stripnr * 4, SeekOrigin.Current);
                    offset = smapReader.ReadInt32();
                }
            }

            ScummHelper.AssertRange(0, offset, smapLen - 1, "screen strip");
            smapReader.BaseStream.Seek(offset, SeekOrigin.Begin);

            return DecompressBitmap(navDst, smapReader, height);
        }

        bool DecompressBitmap(PixelNavigator navDst, BinaryReader src, int numLinesToProcess)
        {
            if (game.Features.HasFlag(GameFeatures.SixteenColors))
            {
                DrawStripEGA(navDst, src, numLinesToProcess);
                return false;
            }

            paletteMod = 0;

            byte code = src.ReadByte();
            bool transpStrip = false;
            decompShr = (byte)(code % 10);
            decompMask = (byte)(0xFF >> (8 - decompShr));

            switch (code)
            {
                case 1:
                    DrawStripRaw(navDst, src, numLinesToProcess, false);
                    break;
                case 2:
				// Indy256
                    UnkDecode8(navDst, src, numLinesToProcess);
                    break;
                case 3:
				// Indy256
                    UnkDecode9(navDst, src, numLinesToProcess);
                    break;
                case 4:
				// Indy256
                    UnkDecode10(navDst, src, numLinesToProcess);
                    break;
                case 7:
				// Indy256
                    UnkDecode11(navDst, src, numLinesToProcess);
                    break;

                case 14:
                case 15:
                case 16:
                case 17:
                case 18:
                    DrawStripBasicV(navDst, src, numLinesToProcess, false);
                    break;

                case 24:
                case 25:
                case 26:
                case 27:
                case 28:
                    DrawStripBasicH(navDst, src, numLinesToProcess, false);
                    break;

                case 34:
                case 35:
                case 36:
                case 37:
                case 38:
                    transpStrip = true;
                    DrawStripBasicV(navDst, src, numLinesToProcess, true);
                    break;

                case 44:
                case 45:
                case 46:
                case 47:
                case 48:
                    transpStrip = true;
                    DrawStripBasicH(navDst, src, numLinesToProcess, true);
                    break;

                default:
                    throw new NotImplementedException(string.Format("Gdi.DecompressBitmap: default case {0}", code));
            }

            return transpStrip;
        }

        void DrawStripRaw(PixelNavigator navDst, BinaryReader src, int height, bool transpCheck)
        {
            int x;

            if (game.Features.HasFlag(GameFeatures.Old256))
            {
                int h = height;
                x = 8;
                while (true)
                {
                    navDst.Write(RoomPalette[src.ReadByte()]);
                    if (!NextRow(ref navDst, ref x, ref h, height))
                        return;
                }
            }
            do
            {
                for (x = 0; x < 8; x++)
                {
                    int color = src.ReadByte();
                    if (!transpCheck || color != TransparentColor)
                        WriteRoomColor(navDst, color);
                }
                navDst.OffsetY(1);
            } while ((--height) != 0);
        }

        void UnkDecode8(PixelNavigator navDst, BinaryReader src, int height)
        {
            int h = height;
            int x = 8;
            while (true)
            {
                int run = src.ReadByte() + 1;
                int color = src.ReadByte();
		
                for (var i = 0; i < run; i++)
                {
                    navDst.Write(RoomPalette[color]);
                    if (!NextRow(ref navDst, ref x, ref h, height))
                        return;
                }
            }
        }

        void UnkDecode9(PixelNavigator navDst, BinaryReader src, int height)
        {
            int c, color;
            int i;
            int buffer = 0;
            int mask = 128;
            int h = height;
            byte run = 0;

            int x = 8;
            while (true)
            {
                c = ReadNBits(src, 4, ref mask, ref buffer);
		
                switch (c >> 2)
                {
                    case 0:
                        color = ReadNBits(src, 4, ref mask, ref buffer);
                        for (i = 0; i < ((c & 3) + 2); i++)
                        {
                            navDst.Write(RoomPalette[run * 16 + color]);
                            if (!NextRow(ref navDst, ref x, ref h, height))
                                return;
                        }
                        break;
		
                    case 1:
                        for (i = 0; i < ((c & 3) + 1); i++)
                        {
                            color = ReadNBits(src, 4, ref mask, ref buffer);
                            navDst.Write(RoomPalette[run * 16 + color]);
                            if (!NextRow(ref navDst, ref x, ref h, height))
                                return;
                        }
                        break;
		
                    case 2:
                        run = (byte)ReadNBits(src, 4, ref mask, ref buffer);
                        break;
                }
            }
        }

        void UnkDecode10(PixelNavigator navDst, BinaryReader src, int height)
        {
            int h = height;
            int numcolors = src.ReadByte();
            var local_palette = src.ReadBytes(numcolors);
        
            int x = 8;
        
            while (true)
            {
                int color = src.ReadByte();
                if (color < numcolors)
                {
                    navDst.Write(RoomPalette[local_palette[color]]);
                    if (!NextRow(ref navDst, ref x, ref h, height))
                        return;
                }
                else
                {
                    int run = color - numcolors + 1;
                    color = src.ReadByte();
                    for (var i = 0; i < run; i++)
                    {
                        navDst.Write(RoomPalette[color]);
                        if (!NextRow(ref navDst, ref x, ref h, height))
                            return;
                    }
                }
            }
        }

        void UnkDecode11(PixelNavigator navDst, BinaryReader src, int height)
        {
            int i;
            int buffer = 0, mask = 128;
            int inc = 1;
            int color = src.ReadByte();
		
            int x = 8;
            do
            {
                int h = height;
                do
                {
                    navDst.Write(RoomPalette[color]);
                    navDst.OffsetY(1);
                    for (i = 0; i < 3; i++)
                    {
                        if (!ReadBit256(src, ref mask, ref buffer))
                            break;
                    }
                    switch (i)
                    {
                        case 1:
                            inc = -inc;
                            color -= inc;
                            break;
                        case 2:
                            color -= inc;
                            break;
                        case 3:
                            inc = 1;
                            color = ReadNBits(src, 8, ref mask, ref buffer);
                            break;
                    }
                } while ((--h) != 0);
                navDst.Offset(1, -height);
            } while ((--x) != 0);
        }

        static bool ReadBit256(BinaryReader src, ref int mask, ref int buffer)
        {
            if ((mask <<= 1) == 256)
            {     
                buffer = src.ReadByte();           
                mask = 1;                  
            }                              
            return ((buffer & mask) != 0);
        }

        static int ReadNBits(BinaryReader src, int n, ref int mask, ref int buffer)
        {
            var color = 0;
            for (int b = 0; b < n; b++)
            {  
                var bits = ReadBit256(src, ref mask, ref buffer) ? 1 : 0;
                color += (bits << b);          
            }
            return color;
        }

        static bool NextRow(ref PixelNavigator navDst, ref int x, ref int y, int height)
        {
            navDst.OffsetY(1);
            if (--y == 0)
            { 
                if ((--x) == 0)
                    return false; 
                navDst.Offset(1, -height);
                y = height;              
            }               
            return true;
        }

        void DrawStripEGA(PixelNavigator navDst, BinaryReader src, int height)
        {
            byte color;
            int run;
            int x = 0, y = 0;
            int z;

            navDst = new PixelNavigator(navDst);

            while (x < 8)
            {
                color = src.ReadByte();

                if ((color & 0x80) != 0)
                {
                    run = color & 0x3f;

                    if ((color & 0x40) != 0)
                    {
                        color = src.ReadByte();

                        if (run == 0)
                        {
                            run = src.ReadByte();
                        }
                        for (z = 0; z < run; z++)
                        {
                            navDst.GoTo(x, y);
                            navDst.Write((z & 1) != 0 ? RoomPalette[(color & 0xf) + paletteMod] : RoomPalette[(color >> 4) + paletteMod]);

                            y++;
                            if (y >= height)
                            {
                                y = 0;
                                x++;
                            }
                        }
                    }
                    else
                    {
                        if (run == 0)
                        {
                            run = src.ReadByte();
                        }

                        for (z = 0; z < run; z++)
                        {
                            navDst.GoTo(x - 1, y);
                            var col = navDst.Read();
                            navDst.GoTo(x, y);
                            navDst.Write(col);

                            y++;
                            if (y >= height)
                            {
                                y = 0;
                                x++;
                            }
                        }
                    }
                }
                else
                {
                    run = color >> 4;
                    if (run == 0)
                    {
                        run = src.ReadByte();
                    }

                    for (z = 0; z < run; z++)
                    {
                        navDst.GoTo(x, y);
                        navDst.Write(RoomPalette[(color & 0xf) + paletteMod]);

                        y++;
                        if (y >= height)
                        {
                            y = 0;
                            x++;
                        }
                    }
                }
            }
        }

        void DrawStripBasicH(PixelNavigator navDst, BinaryReader src, int height, bool transpCheck)
        {
            byte color = src.ReadByte();
            uint bits = src.ReadByte();
            byte cl = 8;
            int inc = -1;

            do
            {
                int x = 8;
                do
                {
                    FillBits(ref cl, ref bits, src);
                    if (!transpCheck || color != transparentColor)
                        WriteRoomColor(navDst, color);
                    navDst.OffsetX(1);
                    if (!ReadBit(ref cl, ref bits))
                    {
                    }
                    else if (!ReadBit(ref cl, ref bits))
                    {
                        FillBits(ref cl, ref bits, src);
                        color = (byte)(bits & decompMask);
                        bits >>= decompShr;
                        cl -= decompShr;
                        inc = -1;
                    }
                    else if (!ReadBit(ref cl, ref bits))
                    {
                        color = (byte)(color + inc);
                    }
                    else
                    {
                        inc = -inc;
                        color = (byte)(color + inc);
                    }
                } while (--x != 0);
                navDst.Offset(-8, 1);
            } while (--height != 0);
        }

        void DrawStripBasicV(PixelNavigator navDst, BinaryReader src, int height, bool transpCheck)
        {
            byte color = src.ReadByte();
            uint bits = src.ReadByte();
            byte cl = 8;
            int inc = -1;

            int x = 8;
            do
            {
                int h = height;
                do
                {
                    FillBits(ref cl, ref bits, src);
                    if (!transpCheck || color != transparentColor)
                    {
                        WriteRoomColor(navDst, color);
                    }
                    navDst.OffsetY(1);
                    if (!ReadBit(ref cl, ref bits))
                    {
                    }
                    else if (!ReadBit(ref cl, ref bits))
                    {
                        FillBits(ref cl, ref bits, src);
                        color = (byte)(bits & decompMask);
                        bits >>= decompShr;
                        cl -= decompShr;
                        inc = -1;
                    }
                    else if (!ReadBit(ref cl, ref bits))
                    {
                        color = (byte)(color + inc);
                    }
                    else
                    {
                        inc = -inc;
                        color = (byte)(color + inc);
                    }
                } while ((--h) != 0);
                navDst.Offset(1, -height);
            } while ((--x) != 0);
        }

        static void FillBits(ref byte cl, ref uint bits, BinaryReader src)
        {
            if (cl <= 8)
            {
                var srcBits = (uint)src.ReadByte();
                bits |= (srcBits << cl);
                cl += 8;
            }
        }

        static bool ReadBit(ref byte cl, ref uint bits)
        {
            cl--;
            var bit = bits & 1;
            bits >>= 1;
            return bit != 0;
        }

        void WriteRoomColor(PixelNavigator navDst, int color)
        {
            // As described in bug #1294513 "FOA/Amiga: Palette problem (Regression)"
            // the original AMIGA version of Indy4: The Fate of Atlantis allowed
            // overflowing of the palette index. To have the same result in our code,
            // we need to do an logical AND 0xFF here to keep the result in [0, 255].
            navDst.Write(RoomPalette[(color + paletteMod) & 0xFF]);
        }

        List<byte[]> GetZPlanes(byte[] ptr)
        {
            var zplanes = new List<byte[]>();

            if (IsZBufferEnabled)
            {
                var zplane = new MemoryStream(ptr);
                var zplaneReader = new BinaryReader(zplane);
                int zOffset;
                if (game.Features.HasFlag(GameFeatures.SixteenColors))
                {
                    zOffset = zplaneReader.ReadUInt16();
                    zplaneReader.BaseStream.Seek(-2, SeekOrigin.Current);
                }
                else
                {
                    zOffset = zplaneReader.ReadInt32();
                    zplaneReader.BaseStream.Seek(-4, SeekOrigin.Current);
                }
                while (zOffset != 0 && zplanes.Count < 4)
                {
                    zplanes.Add(zplaneReader.ReadBytes(zOffset));
                    if (zplaneReader.BaseStream.Position < (zplaneReader.BaseStream.Length - 2))
                    {
                        zOffset = zplaneReader.ReadUInt16();
                        zplaneReader.BaseStream.Seek(-2, SeekOrigin.Current);
                    }
                    else
                    {
                        zOffset = 0;
                    }
                }
            }
            return zplanes;
        }

        #endregion
    }
}