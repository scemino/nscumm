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
using System.Linq;
using System.Text;

namespace Scumm4.Graphics
{
    public class Gdi
    {
        public int _numZBuffer = 2;
        public int _numStrips = 40;

        private ScummInterpreter _vm;
        private bool _zbufferDisabled;
        private int _paletteMod;
        private int _decomp_shr;
        private int _decomp_mask;
        private byte _transparentColor;

        public byte TransparentColor
        {
            get { return _transparentColor; }
            set { _transparentColor = value; }
        }

        public bool IsZbufferEnabled
        {
            get { return !_zbufferDisabled; }
            set { _zbufferDisabled = !value; }
        }

        public Gdi(ScummInterpreter vm)
        {
            _vm = vm;
        }

        /// <summary>
        /// Draw a bitmap onto a virtual screen. This is main drawing method for room backgrounds
        /// and objects, used throughout all SCUMM versions.
        /// </summary>
        public void DrawBitmap(byte[] ptr, VirtScreen vs, int x, int y, int width, int height, int stripnr, int numstrip, DrawBitmapFlags flags)
        {
            // Check whether lights are turned on or not
            var lightsOn = _vm.IsLightOn();

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
            int limit = Math.Max(_vm.CurrentRoomData.Header.Width, vs.Width) / 8 - x;
            if (limit > numstrip)
                limit = numstrip;
            if (limit > _numStrips - sx)
                limit = _numStrips - sx;

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
                    if (lightsOn)
                        Copy8Col(navFrontBuf, navDst, height);
                    else
                        Clear8Col(navFrontBuf, height);
                }

                // TODO: mask
                var zplanes = GetZPlanes(ptr);
                //DecodeMask(x, y, width, height, stripnr, zplanes, transpStrip, flags);

            }
        }

        private void Clear8Col(PixelNavigator nav, int height)
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

        private void Copy8Col(PixelNavigator navDst, PixelNavigator navSource, int height)
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

        private void DecodeMask(int x, int y, int width, int height, int stripnr, List<Stream> zplanes, bool transpStrip, DrawBitmapFlags flags)
        {
            int i;
            PixelNavigator mask_ptr;

            if (flags.HasFlag(DrawBitmapFlags.DrawMaskOnAll))
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
                //for (i = 0; i < zplanes.Count; i++)
                //{
                //    mask_ptr = GetMaskBuffer(x, y, i);
                //    if (transpStrip && flags.HasFlag(DrawBitmapFlags.AllowMaskOr))
                //        DecompressMaskImgOr(mask_ptr, z_plane_ptr, height);
                //    else
                //        DecompressMaskImg(mask_ptr, z_plane_ptr, height);
                //}
            }
            else
            {
                for (i = 1; i < zplanes.Count; i++)
                {
                    uint offs;

                    if (zplanes[i] == null)
                        continue;

                    var beginPtr = zplanes[i].Position;
                    zplanes[i].Seek(stripnr * 2 + 2, SeekOrigin.Current);
                    var br = new BinaryReader(zplanes[i]);
                    offs = br.ReadUInt16();

                    mask_ptr = GetMaskBuffer(x, y, i);

                    if (offs != 0)
                    {
                        zplanes[i].Seek(beginPtr + offs, SeekOrigin.Current);

                        if (transpStrip && flags.HasFlag(DrawBitmapFlags.AllowMaskOr))
                        {
                            DecompressMaskImgOr(mask_ptr, zplanes[i], height);
                        }
                        else
                        {
                            DecompressMaskImg(mask_ptr, zplanes[i], height);
                        }

                    }
                    else
                    {
                        if (!(transpStrip && flags.HasFlag(DrawBitmapFlags.AllowMaskOr)))
                            for (int h = 0; h < height; h++)
                            {
                                //mask_ptr[h * _numStrips] = 0;
                            }
                    }
                }
            }
        }

        private void DecompressMaskImg(PixelNavigator dst, Stream src, int height)
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

        private void DecompressMaskImgOr(PixelNavigator dst, Stream src, int height)
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

        public PixelNavigator GetMaskBuffer(int x, int y, int i)
        {
            PixelNavigator nav;
            nav = new PixelNavigator(_maskBuffer, 320, 1);
            nav.GoTo(320 * i, 200 * i);
            nav.Offset(x, y);
            return nav;
        }

        private byte[] _maskBuffer = new byte[320 * 200 * 2];

        private bool DrawStrip(PixelNavigator navDst, int height, int stripnr, BinaryReader smapReader)
        {
            // Do some input verification and make sure the strip/strip offset
            // are actually valid. Normally, this should never be a problem,
            // but if e.g. a savegame gets corrupted, we can easily get into
            // trouble here. See also bug #795214.
            int offset = -1;
            int smapLen = smapReader.ReadInt32();
            if (stripnr * 4 + 4 < smapLen)
            {
                smapReader.BaseStream.Seek(stripnr * 4, SeekOrigin.Current);
                offset = smapReader.ReadInt32();
            }
            smapReader.BaseStream.Seek(offset, SeekOrigin.Begin);

            return DecompressBitmap(navDst, smapReader, height);
        }

        private bool DecompressBitmap(PixelNavigator navDst, BinaryReader src, int numLinesToProcess)
        {
            _paletteMod = 0;

            byte code = src.ReadByte();
            bool transpStrip = false;
            _decomp_shr = code % 10;
            _decomp_mask = 0xFF >> (8 - _decomp_shr);

            switch (code)
            {
                case 1:
                    throw new NotImplementedException();
                    //DrawStripRaw(dst, dstPitch, src, numLinesToProcess, false);
                    break;

                case 2:
                    throw new NotImplementedException();
                    //unkDecode8(dst, dstPitch, src, numLinesToProcess);       /* Ender - Zak256/Indy256 */
                    break;

                case 3:
                    throw new NotImplementedException();
                    //unkDecode9(dst, dstPitch, src, numLinesToProcess);       /* Ender - Zak256/Indy256 */
                    break;

                case 4:
                    throw new NotImplementedException();
                    //unkDecode10(dst, dstPitch, src, numLinesToProcess);      /* Ender - Zak256/Indy256 */
                    break;

                case 7:
                    throw new NotImplementedException();
                    //unkDecode11(dst, dstPitch, src, numLinesToProcess);      /* Ender - Zak256/Indy256 */
                    break;

                case 8:
                    // Used in 3DO versions of HE games
                    transpStrip = true;
                    throw new NotImplementedException();
                    //drawStrip3DO(dst, dstPitch, src, numLinesToProcess, true);
                    break;

                case 9:
                    //drawStrip3DO(dst, dstPitch, src, numLinesToProcess, false);
                    throw new NotImplementedException();
                    break;

                case 10:
                    // Used in Amiga version of Monkey Island 1
                    //drawStripEGA(dst, dstPitch, src, numLinesToProcess);
                    throw new NotImplementedException();
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

                case 64:
                case 65:
                case 66:
                case 67:
                case 68:
                case 104:
                case 105:
                case 106:
                case 107:
                case 108:
                    throw new NotImplementedException();
                    //DrawStripComplex(dst, dstPitch, src, numLinesToProcess, false);
                    break;

                case 84:
                case 85:
                case 86:
                case 87:
                case 88:
                case 124:
                case 125:
                case 126:
                case 127:
                case 128:
                    transpStrip = true;
                    throw new NotImplementedException();
                    //DrawStripComplex(dst, dstPitch, src, numLinesToProcess, true);
                    break;

                case 134:
                case 135:
                case 136:
                case 137:
                case 138:
                    //drawStripHE(dst, dstPitch, src, 8, numLinesToProcess, false);
                    throw new NotImplementedException();
                    break;

                case 143: // Triggered by Russian water
                case 144:
                case 145:
                case 146:
                case 147:
                case 148:
                    transpStrip = true;
                    //drawStripHE(dst, dstPitch, src, 8, numLinesToProcess, true);
                    throw new NotImplementedException();
                    break;

                case 149:
                    //drawStripRaw(dst, dstPitch, src, numLinesToProcess, true);
                    throw new NotImplementedException();
                    break;

                default:
                    //error("Gdi::decompressBitmap: default case %d", code);
                    throw new NotImplementedException();
            }

            return transpStrip;
        }

        private void DrawStripBasicH(PixelNavigator navDst, BinaryReader src, int height, bool transpCheck)
        {
            int color = src.ReadByte();
            int bits = src.ReadByte();
            int cl = 8;
            int inc = -1;

            do
            {
                int x = 8;
                do
                {
                    FILL_BITS(ref cl, ref bits, src);
                    if (!transpCheck || color != _transparentColor)
                        WriteRoomColor(navDst, color);
                    navDst.OffsetX(1);
                    if (!READ_BIT(ref cl, ref bits))
                    {
                    }
                    else if (!READ_BIT(ref cl, ref bits))
                    {
                        FILL_BITS(ref cl, ref bits, src);
                        color = bits & _decomp_mask;
                        bits >>= _decomp_shr;
                        cl -= _decomp_shr;
                        inc = -1;
                    }
                    else if (!READ_BIT(ref cl, ref bits))
                    {
                        color += inc;
                    }
                    else
                    {
                        inc = -inc;
                        color += inc;
                    }
                } while (--x != 0);
                navDst.Offset(-8, 1);
            } while (--height != 0);
        }

        private void DrawStripBasicV(PixelNavigator navDst, BinaryReader src, int height, bool transpCheck)
        {
            int color = src.ReadByte();
            int bits = src.ReadByte();
            int cl = 8;
            int inc = -1;

            int x = 8;
            do
            {
                int h = height;
                do
                {
                    FILL_BITS(ref cl, ref bits, src);
                    if (!transpCheck || color != _transparentColor)
                    {
                        WriteRoomColor(navDst, color);
                    }
                    navDst.OffsetY(1);
                    if (!READ_BIT(ref cl, ref bits))
                    {
                    }
                    else if (!READ_BIT(ref cl, ref bits))
                    {
                        FILL_BITS(ref cl, ref bits, src);
                        color = bits & _decomp_mask;
                        bits >>= _decomp_shr;
                        cl -= _decomp_shr;
                        inc = -1;
                    }
                    else if (!READ_BIT(ref cl, ref bits))
                    {
                        color += inc;
                    }
                    else
                    {
                        inc = -inc;
                        color += inc;
                    }
                } while ((--h) != 0);
                navDst.Offset(1, -height);
            } while ((--x) != 0);
        }

        private void FILL_BITS(ref int cl, ref int bits, BinaryReader src)
        {
            if (cl <= 8)
            {
                bits |= (src.ReadByte() << cl);
                cl += 8;
            }
        }

        private bool READ_BIT(ref int cl, ref int bits)
        {
            cl--;
            var bit = bits & 1;
            bits >>= 1;
            return bit != 0;
        }

        private void WriteRoomColor(PixelNavigator navDst, int color)
        {
            // As described in bug #1294513 "FOA/Amiga: Palette problem (Regression)"
            // the original AMIGA version of Indy4: The Fate of Atlantis allowed
            // overflowing of the palette index. To have the same result in our code,
            // we need to do an logical AND 0xFF here to keep the result in [0, 255].
            navDst.Write(_vm._roomPalette[(color + _paletteMod) & 0xFF]);
        }

        private List<Stream> GetZPlanes(byte[] ptr)
        {
            List<Stream> zplanes = new List<Stream>();
            int numzbuf;

            zplanes.Add(new MemoryStream(ptr));

            if (_zbufferDisabled)
                numzbuf = 0;
            else if (_numZBuffer <= 1)
                numzbuf = _numZBuffer;
            else
            {
                numzbuf = _numZBuffer;
                //assert(numzbuf <= 9);

                //uint* uPtr = (uint*)ptr;
                //zplanes.Add((IntPtr)(ptr + (*uPtr)));
                var zplane = new MemoryStream(ptr);
                var zplaneReader = new BinaryReader(zplane);
                var uPtr = zplaneReader.ReadInt32();
                zplaneReader.BaseStream.Seek(uPtr, SeekOrigin.Begin);
                zplanes.Add(new MemoryStream(ptr));

                //for (i = 2; i < numzbuf; i++)
                //{
                //    zplane_list[i] = zplane_list[i - 1] + READ_LE_UINT16(zplane_list[i - 1]);
                //}
            }

            return zplanes;
        }

        public void ResetBackground(int top, int bottom, int strip)
        {
            VirtScreen vs = _vm.MainVirtScreen;
            int numLinesToProcess;

            if (top < 0)
                top = 0;

            if (bottom > vs.Height)
                bottom = vs.Height;

            if (top >= bottom)
                return;

            System.Diagnostics.Debug.Assert(0 <= strip && strip < _numStrips);

            if (top < vs.TDirty[strip])
                vs.TDirty[strip] = top;

            if (bottom > vs.BDirty[strip])
                vs.BDirty[strip] = bottom;

            numLinesToProcess = bottom - top;
            if (numLinesToProcess > 0)
            {
                PixelNavigator navDest = new PixelNavigator(vs.Surfaces[0]);
                navDest.GoTo(strip * 8 + vs.XStart, top);
                if (_vm.IsLightOn())
                {
                    PixelNavigator bgBakNav = new PixelNavigator(vs.Surfaces[1]);
                    bgBakNav.GoTo(strip * 8 + vs.XStart, top); 
                    Copy8Col(navDest, bgBakNav, numLinesToProcess);
                }
                else
                {
                    Clear8Col(navDest, numLinesToProcess);
                }
            }
        }

        public void Init()
        {
            _numStrips = _vm._screenWidth / 8;
        }
    }
}
