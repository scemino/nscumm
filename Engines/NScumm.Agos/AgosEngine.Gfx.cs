//
//  AgosEngine.Gfx.cs
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
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        private BytePtr _scrollImage;

        protected BytePtr vc10_depackColumn(Vc10State vs)
        {
            sbyte a = vs.depack_cont;
            var src = vs.srcPtr;
            var dst = new BytePtr(vs.depack_dest);
            ushort dh = vs.dh;

            if (a == -0x80)
            {
                a = (sbyte) src.Value;
                src.Offset++;
            }

            for (;;)
            {
                if (a >= 0)
                {
                    var color = src.Value;
                    src.Offset++;
                    do
                    {
                        dst.Value = color;
                        dst.Offset++;
                        if (--dh == 0)
                        {
                            if (--a < 0)
                                a = -0x80;
                            else
                                src.Offset--;
                            goto get_out;
                        }
                    } while (--a >= 0);
                }
                else
                {
                    do
                    {
                        dst.Value = src.Value;
                        src.Offset++;
                        dst.Offset++;
                        if (--dh == 0)
                        {
                            if (++a == 0)
                                a = -0x80;
                            goto get_out;
                        }
                    } while (++a != 0);
                }
                a = (sbyte) src.Value;
                src.Offset++;
            }

            get_out:
            vs.srcPtr = src;
            vs.depack_cont = a;
            return new BytePtr(vs.depack_dest, vs.y_skip);
        }

        protected void vc10_skip_cols(Vc10State vs)
        {
            while (vs.x_skip != 0)
            {
                vc10_depackColumn(vs);
                vs.x_skip--;
            }
        }

        protected bool DrawImageClip(Vc10State state)
        {
            var vlut = new Ptr<ushort>(_videoWindows, _windowNum * 4);

            if (_gd.ADGameDescription.gameType != SIMONGameType.GType_FF &&
                _gd.ADGameDescription.gameType != SIMONGameType.GType_PP)
            {
                state.draw_width = (ushort) (state.width * 2);
            }

            int cur = state.x;
            if (cur < 0)
            {
                do
                {
                    if (--state.draw_width == 0)
                        return false;
                    state.x_skip++;
                } while (++cur != 0);
            }
            state.x = (short) cur;

            var maxWidth = _gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                           _gd.ADGameDescription.gameType == SIMONGameType.GType_PP
                ? _screenWidth
                : vlut[2] * 2;
            cur += state.draw_width - maxWidth;
            if (cur > 0)
            {
                do
                {
                    if (--state.draw_width == 0)
                        return false;
                } while (--cur != 0);
            }

            cur = state.y;
            if (cur < 0)
            {
                do
                {
                    if (--state.draw_height == 0)
                        return false;
                    state.y_skip++;
                } while (++cur != 0);
            }
            state.y = (short) cur;

            var maxHeight = _gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                            _gd.ADGameDescription.gameType == SIMONGameType.GType_PP
                ? _screenHeight
                : vlut[3];
            cur += state.draw_height - maxHeight;
            if (cur > 0)
            {
                do
                {
                    if (--state.draw_height == 0)
                        return false;
                } while (--cur != 0);
            }

            if (_gd.ADGameDescription.gameType != SIMONGameType.GType_FF &&
                _gd.ADGameDescription.gameType != SIMONGameType.GType_PP)
            {
                state.draw_width *= 4;
            }

            return state.draw_width != 0 && state.draw_height != 0;
        }

        protected void DrawBackGroundImage(Vc10State state)
        {
            state.width = (ushort) _screenWidth;
            if (_window3Flag == 1)
            {
                state.width = 0;
                state.x_skip = 0;
                state.y_skip = 0;
            }

            var src = state.srcPtr + state.width * state.y_skip + state.x_skip * 8;
            var dst = state.surf_addr;

            state.draw_width *= 2;

            int h = state.draw_height;
            int w = state.draw_width;
            var paletteMod = state.paletteMod;
            do
            {
                for (var i = 0; i != w; i += 2)
                {
                    dst[i] = (byte) (src[i] + paletteMod);
                    dst[i + 1] = (byte) (src[i + 1] + paletteMod);
                }
                dst += (int) state.surf_pitch;
                src += state.width;
            } while (--h != 0);
        }

        protected void DrawVertImage(Vc10State state)
        {
            if (state.flags.HasFlag(DrawFlags.kDFCompressed))
            {
                DrawVertImageCompressed(state);
            }
            else
            {
                DrawVertImageUncompressed(state);
            }
        }

        private void DrawVertImageCompressed(Vc10State state)
        {
            System.Diagnostics.Debug.Assert(state.flags.HasFlag(DrawFlags.kDFCompressed));

            state.x_skip *= 4; /* reached */

            state.dl = state.width;
            state.dh = state.height;

            vc10_skip_cols(state);

            var dstPtr = state.surf_addr;
            if (!state.flags.HasFlag(DrawFlags.kDFNonTrans) && state.flags.HasFlag(DrawFlags.kDFScaled))
            {
                /* reached */
                dstPtr += (int) VcReadVar(252);
            }
            var w = 0;
            do
            {
                byte color;

                var src = vc10_depackColumn(state);
                var dst = dstPtr;

                var h = 0;
                if (state.flags.HasFlag(DrawFlags.kDFNonTrans))
                {
                    do
                    {
                        byte colors = src.Value;
                        color = (byte) (colors / 16);
                        dst[0] = (byte) (color | state.palette);
                        color = (byte) (colors & 15);
                        dst[1] = (byte) (color | state.palette);
                        dst += (int) state.surf_pitch;
                        src.Offset++;
                    } while (++h != state.draw_height);
                }
                else
                {
                    do
                    {
                        byte colors = src.Value;
                        color = (byte) (colors / 16);
                        if (color != 0)
                            dst[0] = (byte) (color | state.palette);
                        color = (byte) (colors & 15);
                        if (color != 0)
                            dst[1] = (byte) (color | state.palette);
                        dst += (int) state.surf_pitch;
                        src.Offset++;
                    } while (++h != state.draw_height);
                }
                dstPtr += 2;
            } while (++w != state.draw_width);
        }

        private void DrawVertImageUncompressed(Vc10State state)
        {
            System.Diagnostics.Debug.Assert(!state.flags.HasFlag(DrawFlags.kDFCompressed));

            var src = state.srcPtr + (state.width * state.y_skip) * 8;
            var dst = state.surf_addr;
            state.x_skip *= 4;

            do
            {
                for (var count = 0; count != state.draw_width; count++)
                {
                    byte color = (byte) (src[count + state.x_skip] / 16 + state.paletteMod);
                    if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                        dst[count * 2] = (byte) (color | state.palette);
                    color = (byte) ((src[count + state.x_skip] & 15) + state.paletteMod);
                    if (state.flags.HasFlag(DrawFlags.kDFNonTrans) || color != 0)
                        dst[count * 2 + 1] = (byte) (color | state.palette);
                }
                dst.Offset += (int) state.surf_pitch;
                src.Offset += state.width * 8;
            } while (--state.draw_height != 0);
        }

        protected virtual void DrawImage(Vc10State state)
        {
            Ptr<ushort> vlut = new Ptr<ushort>(_videoWindows, _windowNum * 4);

            if (!DrawImageClip(state))
                return;

            LocksScreen(screen =>
            {
                ushort xoffs = 0, yoffs = 0;
                if (GameType == SIMONGameType.GType_WW)
                {
                    if (_windowNum == 4 || (_windowNum >= 10 && _windowNum <= 27))
                    {
                        state.surf_addr = _window4BackScn.Pixels;
                        state.surf_pitch = (uint) (_videoWindows[18] * 16);

                        xoffs = (ushort) (((vlut[0] - _videoWindows[16]) * 2 + state.x) * 8);
                        yoffs = (ushort) (vlut[1] - _videoWindows[17] + state.y);

                        uint xmax = (uint) (xoffs + state.draw_width * 2);
                        uint ymax = (uint) (yoffs + state.draw_height);
                        SetMoveRect(xoffs, yoffs, (ushort) xmax, (ushort) ymax);

                        _window4Flag = 1;
                    }
                    else
                    {
                        state.surf_addr = screen.Pixels;
                        state.surf_pitch = (uint) screen.Pitch;

                        xoffs = (ushort) ((vlut[0] * 2 + state.x) * 8);
                        yoffs = (ushort) (vlut[1] + state.y);
                    }
                }
                else if (GameType == SIMONGameType.GType_ELVIRA2)
                {
                    if (_windowNum == 4 || _windowNum >= 10)
                    {
                        state.surf_addr = _window4BackScn.Pixels;
                        state.surf_pitch = (uint) (_videoWindows[18] * 16);

                        xoffs = (ushort) (((vlut[0] - _videoWindows[16]) * 2 + state.x) * 8);
                        yoffs = (ushort) (vlut[1] - _videoWindows[17] + state.y);

                        uint xmax = (uint) (xoffs + state.draw_width * 2);
                        uint ymax = (uint) (yoffs + state.draw_height);
                        SetMoveRect(xoffs, yoffs, (ushort) xmax, (ushort) ymax);

                        _window4Flag = 1;
                    }
                    else
                    {
                        state.surf_addr = screen.Pixels;
                        state.surf_pitch = (uint) screen.Pitch;

                        xoffs = (ushort) ((vlut[0] * 2 + state.x) * 8);
                        yoffs = (ushort) (vlut[1] + state.y);
                    }
                }
                else if (GameType == SIMONGameType.GType_ELVIRA1)
                {
                    if (_windowNum == 6)
                    {
                        state.surf_addr = _window6BackScn.Pixels;
                        state.surf_pitch = (uint) _window6BackScn.Pitch;

                        xoffs = (ushort) (state.x * 8);
                        yoffs = (ushort) state.y;
                    }
                    else if (_windowNum == 2 || _windowNum == 3)
                    {
                        state.surf_addr = screen.Pixels;
                        state.surf_pitch = (uint) screen.Pitch;

                        xoffs = (ushort) ((vlut[0] * 2 + state.x) * 8);
                        yoffs = (ushort) (vlut[1] + state.y);
                    }
                    else
                    {
                        state.surf_addr = _window4BackScn.Pixels;
                        state.surf_pitch = (uint) (_videoWindows[18] * 16);

                        xoffs = (ushort) (((vlut[0] - _videoWindows[16]) * 2 + state.x) * 8);
                        yoffs = (ushort) (vlut[1] - _videoWindows[17] + state.y);

                        uint xmax = (uint) (xoffs + state.draw_width * 2);
                        uint ymax = (uint) (yoffs + state.draw_height);
                        SetMoveRect(xoffs, yoffs, (ushort) xmax, (ushort) ymax);

                        _window4Flag = 1;
                    }
                }
                else
                {
                    state.surf_addr = screen.Pixels;
                    state.surf_pitch = (uint) screen.Pitch;

                    xoffs = (ushort) ((vlut[0] * 2 + state.x) * 8);
                    yoffs = (ushort) (vlut[1] + state.y);
                }

                state.surf_addr.Offset += (int) (xoffs + yoffs * state.surf_pitch);

                if (GameType == SIMONGameType.GType_ELVIRA1 && state.flags.HasFlag(DrawFlags.kDFNonTrans) && yoffs > 133)
                    state.paletteMod = 16;

                if (GameType == SIMONGameType.GType_ELVIRA2 || GameType == SIMONGameType.GType_WW)
                    state.palette = (byte) (state.surf_addr[0] & 0xF0);

                if (GameType == SIMONGameType.GType_ELVIRA2 && GamePlatform == Platform.AtariST && yoffs > 133)
                    state.palette = 208;

                if (_backFlag)
                {
                    DrawBackGroundImage(state);
                }
                else
                {
                    DrawVertImage(state);
                }
            });
        }

        private void HorizontalScroll(Vc10State state)
        {
            int dstPitch, w;

            if (GameType == SIMONGameType.GType_FF)
                _scrollXMax = (short) (state.width - 640);
            else
                _scrollXMax = (short) (state.width * 2 - 40);
            _scrollYMax = 0;
            _scrollImage = state.srcPtr;
            _scrollHeight = state.height;
            if (_variableArrayPtr[34] < 0)
                state.x = _variableArrayPtr[251];

            _scrollX = state.x;

            VcWriteVar(251, _scrollX);

            BytePtr dst, src;
            if (GameType == SIMONGameType.GType_SIMON2)
            {
                dst = _window4BackScn.Pixels;
                dstPitch = _window4BackScn.Pitch;
            }
            else
            {
                dst = BackBuf;
                dstPitch = _backBuf.Pitch;
            }

            if (GameType == SIMONGameType.GType_FF)
                src = state.srcPtr + _scrollX / 2;
            else
                src = state.srcPtr + _scrollX * 4;

            for (w = 0; w < _screenWidth; w += 8)
            {
                DecodeColumn(dst, src + (int) ReadUint32Wrapper(src), state.height, (ushort) dstPitch);
                dst += 8;
                src += 4;
            }

            SetMoveRect(0, 0, 320, _scrollHeight);

            _window4Flag = 1;
        }

        private static void DecodeColumn(BytePtr dst, BytePtr src, ushort height, ushort pitch)
        {
            var dstPtr = dst;
            uint h = height, w = 8;

            for (;;)
            {
                var reps = (sbyte) src.Value;
                src.Offset++;
                if (reps >= 0)
                {
                    byte color = src.Value;
                    src.Offset++;

                    do
                    {
                        dst.Value = color;
                        dst += pitch;

                        /* reached bottom? */
                        if (--h == 0)
                        {
                            /* reached right edge? */
                            if (--w == 0)
                                return;
                            dstPtr.Offset++;
                            dst = dstPtr;
                            h = height;
                        }
                    } while (--reps >= 0);
                }
                else
                {
                    do
                    {
                        dst.Value = src.Value;
                        src.Offset++;
                        dst += pitch;

                        /* reached bottom? */
                        if (--h == 0)
                        {
                            /* reached right edge? */
                            if (--w == 0)
                                return;
                            dstPtr.Offset++;
                            dst = dstPtr;
                            h = height;
                        }
                    } while (++reps != 0);
                }
            }
        }

        private void VerticalScroll(Vc10State state)
        {
            _scrollXMax = 0;
            _scrollYMax = (short) (state.height - 480);
            _scrollImage = state.srcPtr;
            _scrollWidth = state.width;
            if (_variableArrayPtr[34] < 0)
                state.y = _variableArrayPtr[250];

            _scrollY = state.y;

            VcWriteVar(250, _scrollY);

            var dst = BackBuf;
            var src = state.srcPtr + _scrollY / 2;

            for (var h = 0; h < _screenHeight; h += 8)
            {
                DecodeRow(dst, src + src.ToInt32(), state.width, (ushort) _backBuf.Pitch);
                dst += 8 * state.width;
                src.Offset += 4;
            }
        }

        private static void DecodeRow(BytePtr dst, BytePtr src, ushort width, ushort pitch)
        {
            var dstPtr = dst;
            uint w = width, h = 8;

            while (true)
            {
                var reps = (sbyte) src.Value;
                src.Offset++;
                if (reps >= 0)
                {
                    byte color = src.Value;
                    src.Offset++;

                    do
                    {
                        dst.Value = color;
                        dst.Offset++;

                        /* reached right edge? */
                        if (--w == 0)
                        {
                            /* reached bottom? */
                            if (--h == 0)
                                return;
                            dstPtr += pitch;
                            dst = dstPtr;
                            w = width;
                        }
                    } while (--reps >= 0);
                }
                else
                {
                    do
                    {
                        dst.Value = src.Value;
                        src.Offset++;
                        dst.Offset++;

                        /* reached right edge? */
                        if (--w == 0)
                        {
                            /* reached bottom? */
                            if (--h == 0)
                                return;
                            dstPtr += pitch;
                            dst = dstPtr;
                            w = width;
                        }
                    } while (++reps != 0);
                }
            }
        }

        protected void PaletteFadeOut(Ptr<Color> palPtr, int num, int size)
        {
            for (int i = 0; i < num; i++)
            {
                palPtr[i] = Color.FromRgb(
                    palPtr[i].R >= size ? palPtr[i].R - size : 0,
                    palPtr[i].G >= size ? palPtr[i].G - size : 0,
                    palPtr[i].B >= size ? palPtr[i].B - size : 0);
            }
        }

        protected void Animate(ushort windowNum, ushort zoneNum, ushort vgaSpriteId, short x, short y,
            ushort palette, bool vgaScript = false)
        {
            if (_gd.ADGameDescription.gameType != SIMONGameType.GType_PN &&
                _gd.ADGameDescription.gameType != SIMONGameType.GType_ELVIRA1)
            {
                if (IsSpriteLoaded(vgaSpriteId, zoneNum))
                    return;
            }

            Ptr<VgaSprite> vsp = _vgaSprites;
            while (vsp.Value.id != 0)
                vsp.Offset++;

            vsp.Value.windowNum = windowNum;
            vsp.Value.priority = 0;
            vsp.Value.flags = 0;

            vsp.Value.y = y;
            vsp.Value.x = x;
            vsp.Value.image = 0;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                vsp.Value.palette = 0;
            else
                vsp.Value.palette = palette;
            vsp.Value.id = vgaSpriteId;
            vsp.Value.zoneNum = zoneNum;

            for (;;)
            {
                var vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, zoneNum);
                _curVgaFile1 = vpe.Value.vgaFile1;
                if (vgaScript)
                {
                    if (vpe.Value.vgaFile1 != BytePtr.Null)
                        break;
                    if (_zoneNumber != zoneNum)
                        _noOverWrite = _zoneNumber;

                    LoadZone(zoneNum);
                    _noOverWrite = 0xFFFF;
                }
                else
                {
                    _zoneNumber = zoneNum;
                    if (vpe.Value.vgaFile1 != BytePtr.Null)
                        break;
                    LoadZone(zoneNum);
                }
            }

            var pp = _curVgaFile1;
            BytePtr p;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                p = pp + pp.ToUInt16(2);
                var header = new VgaFile1HeaderFeeble(p);
                var count = header.animationCount;
                p = pp + header.animationTable;

                var h = new AnimationHeaderFeeble(p);
                while (count-- != 0)
                {
                    h.Pointer = p;
                    if (h.id == vgaSpriteId)
                        break;
                    p += AnimationHeaderFeeble.Size;
                }
                System.Diagnostics.Debug.Assert(h.id == vgaSpriteId);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                p = pp + pp.ToUInt16BigEndian(4);
                var header = new VgaFile1HeaderCommon(p);
                int count = ScummHelper.SwapBytes(header.animationCount);
                p = pp + ScummHelper.SwapBytes(header.animationTable);

                var animHeader = new AnimationHeaderSimon(p);
                while (count-- != 0)
                {
                    animHeader.Pointer = p;
                    if (ScummHelper.SwapBytes(animHeader.id) == vgaSpriteId)
                        break;
                    p += AnimationHeaderSimon.Size;
                }

                animHeader.Pointer = p;
                System.Diagnostics.Debug.Assert(ScummHelper.SwapBytes(animHeader.id) == vgaSpriteId);
            }
            else
            {
                p = pp + pp.ToUInt16BigEndian(10);
                p += 20;

                var header = new VgaFile1HeaderCommon(p);
                var count = ScummHelper.SwapBytes(header.animationCount);
                p = pp + ScummHelper.SwapBytes(header.animationTable);

                var h = new AnimationHeaderWw(p);
                while (count-- != 0)
                {
                    h.Pointer = p;
                    if (ScummHelper.SwapBytes(h.id) == vgaSpriteId)
                        break;
                    p += AnimationHeaderWw.Size;
                }
                System.Diagnostics.Debug.Assert(ScummHelper.SwapBytes(h.id) == vgaSpriteId);
            }

//            if (DebugMan.isDebugChannelEnabled(kDebugVGAScript)) {
//                if (_gd.ADGameDescription.gameType == GType_FF || _gd.ADGameDescription.gameType == GType_PP) {
//                    DumpVgaScript(_curVgaFile1 + READ_LE_UINT16(&((AnimationHeader_Feeble*)p).scriptOffs), zoneNum, vgaSpriteId);
//                } else if (_gd.ADGameDescription.gameType == GType_SIMON1 || _gd.ADGameDescription.gameType == GType_SIMON2) {
//                    DumpVgaScript(_curVgaFile1 + READ_BE_UINT16(&((AnimationHeader_Simon*)p).scriptOffs), zoneNum, vgaSpriteId);
//                } else {
//                    DumpVgaScript(_curVgaFile1 + READ_BE_UINT16(&((AnimationHeader_WW*)p).scriptOffs), zoneNum, vgaSpriteId);
//                }
//            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                AddVgaEvent(_vgaBaseDelay, EventType.ANIMATE_EVENT,
                    _curVgaFile1 + new AnimationHeaderFeeble(p).scriptOffs, vgaSpriteId, zoneNum);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                AddVgaEvent(_vgaBaseDelay, EventType.ANIMATE_EVENT,
                    _curVgaFile1 + ScummHelper.SwapBytes(new AnimationHeaderSimon(p).scriptOffs), vgaSpriteId, zoneNum);
            }
            else
            {
                AddVgaEvent(_vgaBaseDelay, EventType.ANIMATE_EVENT,
                    _curVgaFile1 + ScummHelper.SwapBytes(new AnimationHeaderWw(p).scriptOffs), vgaSpriteId, zoneNum);
            }
        }

        private void SetImage(ushort vgaSpriteId, bool vgaScript)
        {
            BytePtr b;

            uint zoneNum = (uint) (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN ? 0 : vgaSpriteId / 100);

            for (;;)
            {
                var vpe = _vgaBufferPointers[zoneNum];
                _curVgaFile1 = vpe.vgaFile1;
                _curVgaFile2 = vpe.vgaFile2;

                if (vgaScript)
                {
                    if (vpe.vgaFile1 != BytePtr.Null)
                        break;
                    if (_zoneNumber != zoneNum)
                        _noOverWrite = _zoneNumber;

                    LoadZone((ushort) zoneNum);
                    _noOverWrite = 0xFFFF;
                }
                else
                {
                    _curSfxFile = vpe.sfxFile;
                    _curSfxFileSize = vpe.sfxFileEnd.Offset - vpe.sfxFile.Offset;
                    _zoneNumber = (ushort) zoneNum;

                    if (vpe.vgaFile1 != BytePtr.Null)
                        break;

                    LoadZone((ushort) zoneNum);
                }
            }

            var bb = _curVgaFile1;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                b = bb + bb.ToUInt16(2);
                var header = new VgaFile1HeaderCommon(b);
                var count = header.imageCount;
                b = bb + header.imageTable;

                var h = new ImageHeaderFeeble(b);
                while (count-- != 0)
                {
                    h.Pointer = b;
                    if (h.id == vgaSpriteId)
                        break;
                    b += ImageHeaderFeeble.Size;
                }
                System.Diagnostics.Debug.Assert(h.id == vgaSpriteId);
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                b = bb + bb.ToUInt16BigEndian(4);
                var header = new VgaFile1HeaderCommon(b);
                var count = ScummHelper.SwapBytes(header.imageCount);
                b = bb + ScummHelper.SwapBytes(header.imageTable);

                while (count-- != 0)
                {
                    if (ScummHelper.SwapBytes(new ImageHeader_Simon(b).id) == vgaSpriteId)
                        break;
                    b += ImageHeader_Simon.Size;
                }
                System.Diagnostics.Debug.Assert(ScummHelper.SwapBytes(new ImageHeader_Simon(b).id) == vgaSpriteId);

                if (!vgaScript)
                    ClearVideoWindow(_windowNum, ScummHelper.SwapBytes(new ImageHeader_Simon(b).color));
            }
            else
            {
                b = bb + bb.ToUInt16BigEndian(10);
                b += 20;

                var header = new VgaFile1HeaderCommon(b);
                var count = ScummHelper.SwapBytes(header.imageCount);
                b = bb + ScummHelper.SwapBytes(header.imageTable);

                var h = new ImageHeaderWw(b);
                while (count-- != 0)
                {
                    h.Pointer = b;
                    if (ScummHelper.SwapBytes(h.id) == vgaSpriteId)
                        break;
                    b += ImageHeaderWw.Size;
                }
                System.Diagnostics.Debug.Assert(ScummHelper.SwapBytes(h.id) == vgaSpriteId);

                if (!vgaScript)
                {
                    ushort color = ScummHelper.SwapBytes(h.color);
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                    {
                        if ((color & 0x80) != 0)
                            _wiped = true;
                        else if (_wiped == true)
                            RestoreMenu();
                        color &= 0xFF7F;
                    }
                    ClearVideoWindow(_windowNum, color);
                }
            }

//            if (DebugMan.isDebugChannelEnabled(kDebugVGAScript)) {
//                if (_gd.ADGameDescription.gameType == GType_FF || _gd.ADGameDescription.gameType == GType_PP) {
//                    DumpVgaScript(_curVgaFile1 + READ_LE_UINT16(&((ImageHeader_Feeble*)b).scriptOffs), zoneNum, vgaSpriteId);
//                } else if (_gd.ADGameDescription.gameType == GType_SIMON1 || _gd.ADGameDescription.gameType == GType_SIMON2) {
//                    DumpVgaScript(_curVgaFile1 + READ_BE_UINT16(&((ImageHeader_Simon*)b).scriptOffs), zoneNum, vgaSpriteId);
//                } else {
//                    DumpVgaScript(_curVgaFile1 + READ_BE_UINT16(&((ImageHeader_WW*)b).scriptOffs), zoneNum, vgaSpriteId);
//                }
//            }

            var vc_ptr_org = _vcPtr;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                _vcPtr = _curVgaFile1 + new ImageHeaderFeeble(b).scriptOffs;
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
            {
                _vcPtr = _curVgaFile1 + ScummHelper.SwapBytes(new ImageHeader_Simon(b).scriptOffs);
            }
            else
            {
                _vcPtr = _curVgaFile1 + ScummHelper.SwapBytes(new ImageHeaderWw(b).scriptOffs);
            }

            RunVgaScript();
            _vcPtr = vc_ptr_org;
        }

        // Personal Nightmare specific
        private void RestoreMenu()
        {
            _wiped = false;

            _videoLockOut |= 0x80;

            ClearVideoWindow(3, 0);

            ushort oldWindowNum = _windowNum;

            SetWindowImage(1, 1);
            SetWindowImage(2, 2);

            DrawEdging();

            _windowNum = oldWindowNum;

            _videoLockOut |= 0x20;
            _videoLockOut = (ushort) (_videoLockOut & ~0x80);
        }

        // Personal Nightmare specific
        private void DrawEdging()
        {
            byte color = (byte) (GamePlatform == Platform.DOS ? 7 : 15);

            LocksScreen(screen =>
            {
                var dst = screen.GetBasePtr(0, 136);
                byte len = 52;

                while (len-- != 0)
                {
                    dst[0] = color;
                    dst[319] = color;
                    dst += screen.Pitch;
                }

                dst = screen.GetBasePtr(0, 187);
                dst.Data.Set(dst.Offset, color, _screenWidth);
            });
        }

        private void SetWindowImageEx(ushort mode, ushort vgaSpriteId)
        {
            _window3Flag = 0;

            if (mode == 4)
            {
                vc29_stopAllSounds();

                if (GameType == SIMONGameType.GType_ELVIRA1)
                {
                    if (_variableArray[299] == 0)
                    {
                        _variableArray[293] = 0;
                        _wallOn = 0;
                    }
                }
                else if (GameType == SIMONGameType.GType_ELVIRA2)
                {
                    if (_variableArray[70] == 0)
                    {
                        _variableArray[71] = 0;
                        _wallOn = 0;
                    }
                }
            }

            if ((_videoLockOut & 0x10) != 0)
                Error("setWindowImageEx: _videoLockOut & 0x10");

            if (GameType != SIMONGameType.GType_PP && GameType != SIMONGameType.GType_FF)
            {
                if (GameType == SIMONGameType.GType_WW && (mode == 6 || mode == 8 || mode == 9))
                {
                    SetWindowImage(mode, vgaSpriteId);
                }
                else
                {
                    while (_copyScnFlag != 0 && !HasToQuit)
                        Delay(1);

                    SetWindowImage(mode, vgaSpriteId);
                }
            }
            else
            {
                SetWindowImage(mode, vgaSpriteId);
            }

            // Amiga versions wait for verb area to be displayed.
            if (GameType == SIMONGameType.GType_SIMON1 && GamePlatform == Platform.Amiga && vgaSpriteId == 1)
            {
                _copyScnFlag = 5;
                while (_copyScnFlag != 0 && !HasToQuit)
                    Delay(1);
            }
        }

        private void SetWindowImage(ushort mode, ushort vgaSpriteId, bool specialCase = false)
        {
            ushort updateWindow;

            _windowNum = updateWindow = mode;
            _videoLockOut |= 0x20;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                vc27_resetSprite();
            }
            else if (!specialCase)
            {
                Ptr<VgaTimerEntry> vte = _vgaTimerList;
                while (vte.Value.type != EventType.ANIMATE_INT)
                    vte.Offset++;

                vte.Value.delay = 2;
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
            {
                Ptr<AnimTable> animTable = _screenAnim1;
                while (animTable.Value.srcPtr != BytePtr.Null)
                {
                    animTable.Value.srcPtr = BytePtr.Null;
                    animTable.Offset++;
                }
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
            {
                _scrollX = 0;
                _scrollY = 0;
                _scrollXMax = 0;
                _scrollYMax = 0;
                _scrollCount = 0;
                _scrollFlag = 0;
                _scrollHeight = 134;
                _variableArrayPtr = _variableArray;
                if (_variableArray[34] >= 0)
                {
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                        _variableArray[250] = 0;
                    _variableArray[251] = 0;
                }
            }

            SetImage(vgaSpriteId, specialCase);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                FillBackGroundFromBack();
                _syncFlag2 = true;
            }
            else
            {
                _copyScnFlag = 2;
                _vgaSpriteChanged++;

                if (_window3Flag == 1)
                {
                    ClearVideoBackGround(3, 0);
                    _videoLockOut = (ushort) (_videoLockOut & ~0x20);
                    return;
                }

                int xoffs = _videoWindows[updateWindow * 4 + 0] * 16;
                int yoffs = _videoWindows[updateWindow * 4 + 1];
                uint width = (uint) (_videoWindows[updateWindow * 4 + 2] * 16);
                uint height = _videoWindows[updateWindow * 4 + 3];

                var screen = OSystem.GraphicsManager.Capture();
                var dst = _backGroundBuf.GetBasePtr(xoffs, yoffs);
                BytePtr src = BytePtr.Null;
                int srcWidth;

                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                {
                    src = _window4BackScn.GetBasePtr(xoffs, yoffs);
                    srcWidth = 320;
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1
                         && _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
                {
                    // The DOS Floppy demo was based off Waxworks engine
                    if (updateWindow == 4 || updateWindow >= 10)
                    {
                        src = _window4BackScn.Pixels;
                        srcWidth = _videoWindows[18] * 16;
                    }
                    else if (updateWindow == 3 || updateWindow == 9)
                    {
                        src = screen.GetBasePtr(xoffs, yoffs);
                        srcWidth = screen.Pitch;
                    }
                    else
                    {
                        UnlockScreen(screen);
                        _videoLockOut = (ushort) (_videoLockOut & ~0x20);
                        return;
                    }
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
                {
                    if (updateWindow == 4)
                    {
                        src = _window4BackScn.Pixels;
                        srcWidth = _videoWindows[18] * 16;
                    }
                    else if (updateWindow >= 10)
                    {
                        src = _window4BackScn.GetBasePtr(xoffs, yoffs);
                        srcWidth = _videoWindows[18] * 16;
                    }
                    else if (updateWindow == 0)
                    {
                        src = screen.GetBasePtr(xoffs, yoffs);
                        srcWidth = screen.Pitch;
                    }
                    else
                    {
                        UnlockScreen(screen);
                        _videoLockOut = (ushort) (_videoLockOut & ~0x20);
                        return;
                    }
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                {
                    if (updateWindow == 4 || updateWindow >= 10)
                    {
                        src = _window4BackScn.Pixels;
                        srcWidth = _videoWindows[18] * 16;
                    }
                    else if (updateWindow == 3 || updateWindow == 9)
                    {
                        src = screen.GetBasePtr(xoffs, yoffs);
                        srcWidth = screen.Pitch;
                    }
                    else
                    {
                        UnlockScreen(screen);
                        _videoLockOut = (ushort) (_videoLockOut & ~0x20);
                        return;
                    }
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                {
                    if (updateWindow == 4 || updateWindow >= 10)
                    {
                        src = _window4BackScn.Pixels;
                        srcWidth = _videoWindows[18] * 16;
                    }
                    else if (updateWindow == 3)
                    {
                        src = screen.GetBasePtr(xoffs, yoffs);
                        srcWidth = screen.Pitch;
                    }
                    else
                    {
                        UnlockScreen(screen);
                        _videoLockOut = (ushort) (_videoLockOut & ~0x20);
                        return;
                    }
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                {
                    if (updateWindow == 6)
                    {
                        _window6Flag = 1;
                        src = _window6BackScn.Pixels;
                        srcWidth = 48;
                    }
                    else if (updateWindow == 2 || updateWindow == 3)
                    {
                        src = screen.GetBasePtr(xoffs, yoffs);
                        srcWidth = screen.Pitch;
                    }
                    else
                    {
                        src = _window4BackScn.Pixels;
                        srcWidth = _videoWindows[18] * 16;
                    }
                }
                else
                {
                    src = screen.GetBasePtr(xoffs, yoffs);
                    srcWidth = screen.Pitch;
                }

                _boxStarHeight = (byte) height;

                for (; height > 0; height--)
                {
                    Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, (int) width);
                    dst.Offset += _backGroundBuf.Pitch;
                    src.Offset += srcWidth;
                }

                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN &&
                    !_wiped && !specialCase)
                {
                    byte color = (byte) ((_gd.Platform == Platform.DOS) ? 7 : 15);
                    dst = screen.GetBasePtr(48, 0);
                    dst.Data.Set(dst.Offset, color, 224);

                    dst = screen.GetBasePtr(48, 132);
                    dst.Data.Set(dst.Offset, color, 224);
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                         updateWindow == 3 && _bottomPalette)
                {
                    dst = screen.GetBasePtr(0, 133);

                    for (int h = 0; h < 67; h++)
                    {
                        for (int w = 0; w < _screenWidth; w++)
                            dst[w] += 0x10;
                        dst += screen.Pitch;
                    }
                }

                UnlockScreen(screen);
            }

            _videoLockOut = (ushort) (_videoLockOut & ~0x20);
        }
    }
}