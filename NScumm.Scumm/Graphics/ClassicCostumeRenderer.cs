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
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Graphics
{
    class Codec1
    {
        // Parameters for the original ("V1") costume codec.
        // These ones are accessed from ARM code. Don't reorder.
        public int X;
        public int Y;
        public byte[] Scaletable;
        public int SkipWidth;
        public PixelNavigator DestPtr;
        public PixelNavigator MaskPtr;
        public int ScaleXStep;
        public byte Mask, Shr;
        public byte RepColor;
        public byte RepLen;
        // These ones aren't accessed from ARM code.
        public Rect BoundsRect;
        public int ScaleXIndex, ScaleYIndex;
    }

    class ClassicCostumeRenderer : ICostumeRenderer
    {
        static byte[] smallCostumeScaleTable = new byte[256]
        {
            0xFF, 0xFD, 0x7D, 0xBD, 0x3D, 0xDD, 0x5D, 0x9D,
            0x1D, 0xED, 0x6D, 0xAD, 0x2D, 0xCD, 0x4D, 0x8D,
            0x0D, 0xF5, 0x75, 0xB5, 0x35, 0xD5, 0x55, 0x95,
            0x15, 0xE5, 0x65, 0xA5, 0x25, 0xC5, 0x45, 0x85,
            0x05, 0xF9, 0x79, 0xB9, 0x39, 0xD9, 0x59, 0x99,
            0x19, 0xE9, 0x69, 0xA9, 0x29, 0xC9, 0x49, 0x89,
            0x09, 0xF1, 0x71, 0xB1, 0x31, 0xD1, 0x51, 0x91,
            0x11, 0xE1, 0x61, 0xA1, 0x21, 0xC1, 0x41, 0x81,
            0x01, 0xFB, 0x7B, 0xBB, 0x3B, 0xDB, 0x5B, 0x9B,
            0x1B, 0xEB, 0x6B, 0xAB, 0x2B, 0xCB, 0x4B, 0x8B,
            0x0B, 0xF3, 0x73, 0xB3, 0x33, 0xD3, 0x53, 0x93,
            0x13, 0xE3, 0x63, 0xA3, 0x23, 0xC3, 0x43, 0x83,
            0x03, 0xF7, 0x77, 0xB7, 0x37, 0xD7, 0x57, 0x97,
            0x17, 0xE7, 0x67, 0xA7, 0x27, 0xC7, 0x47, 0x87,
            0x07, 0xEF, 0x6F, 0xAF, 0x2F, 0xCF, 0x4F, 0x8F,
            0x0F, 0xDF, 0x5F, 0x9F, 0x1F, 0xBF, 0x3F, 0x7F,
            0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0,
            0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
            0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8,
            0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
            0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4,
            0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
            0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC,
            0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
            0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2,
            0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
            0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA,
            0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
            0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6,
            0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
            0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE,
            0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE
        };

        static readonly byte[] v1MMActorPalatte1 =
            {
                8, 8, 8, 8, 4, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8
            };
        static readonly byte[] v1MMActorPalatte2 =
            {
                0, 7, 2, 6, 9, 1, 3, 7, 7, 1, 1, 9, 1, 4, 5, 5, 4, 1, 0, 5, 4, 2, 2, 7, 7
            };

        public int DrawTop { get; set; }

        public int DrawBottom { get; set; }

        public byte ActorID { get; set; }

        public byte ShadowMode { get; set; }

        public byte[] ShadowTable { get; set; }

        public int ActorX { get; set; }

        public int ActorY { get; set; }

        public byte ZBuffer { get; set; }

        public byte ScaleX { get; set; }

        public byte ScaleY { get; set; }

        ushort[] _palette;
        ClassicCostumeLoader _loaded;
        ScummEngine _vm;

        /// <summary>
        /// Indicates whether to draw the actor mirrored.
        /// </summary>
        bool _mirror;

        byte _scaleIndexX;
        /* must wrap at 256 */
        byte _scaleIndexY;

        // current move offset
        int _xmove, _ymove;

        long _srcptr;

        // width and height of cel to decode
        int _width, _height;

        // _out
        PixelNavigator _pixelsNavigator;
        PixelNavigator startNav;
        int _w;
        int _h;

        #region Constructor

        public ClassicCostumeRenderer(ScummEngine vm)
        {
            _vm = vm;
            _loaded = new ClassicCostumeLoader(vm);
            _palette = new ushort[32];
        }

        #endregion

        #region Public Methods

        public void SetPalette(ushort[] palette)
        {
            if (_loaded.Format == 0x57)
            {
                for (int i = 0; i < 13; i++)
                    _palette[i] = palette[i];
            }
            else if (_vm.Game.IsOldBundle)
            {
                if (_vm.GetCurrentLights().HasFlag(LightModes.ActorUseColors))
                {
                    Array.Copy(palette, _palette, 16);
                }
                else
                {
                    for (int i = 0; i < 16; i++)
                    {
                        _palette[i] = 8;
                    }
                    _palette[12] = 0;
                }
                _palette[_loaded.Palette[0]] = _palette[0];
            }
            else
            {
                if (_vm.GetCurrentLights().HasFlag(LightModes.ActorUseColors))
                {
                    for (int i = 0; i < _loaded.NumColors; i++)
                    {
                        byte color = (byte)palette[i];
                        if (color == 255)
                            color = _loaded.Palette[i];
                        _palette[i] = color;
                    }
                }
                else
                {
                    for (int i = 0; i < _loaded.NumColors; i++)
                    {
                        _palette[i] = 8;
                    }
                    _palette[12] = 0;
                }
            }
        }

        public void SetFacing(Actor actor)
        {
            _mirror = ScummHelper.NewDirToOldDir(actor.Facing) != 0 || _loaded.Mirror;
        }

        public void SetCostume(int costume, int shadow)
        {
            _loaded.LoadCostume(costume);
        }

        public int DrawCostume(VirtScreen vs, int numStrips, Actor actor, bool drawToBackBuf)
        {
            var pixelsNavigator = new PixelNavigator(vs.Surfaces[drawToBackBuf ? 1 : 0]);
            pixelsNavigator.OffsetX(vs.XStart);

            ActorX += (vs.XStart & 7);
            _w = vs.Width;
            _h = vs.Height;
            pixelsNavigator.OffsetX(-(vs.XStart & 7));
            startNav = new PixelNavigator(pixelsNavigator);

            if (_vm.Game.Version <= 1)
            {
                _xmove = 0;
                _ymove = 0;
            }
            else if (_vm.Game.IsOldBundle)
            {
                _xmove = -72;
                _ymove = -100;
            }
            else
            {
                _xmove = _ymove = 0;
            }

            int result = 0;
            for (int i = 0; i < 16; i++)
                result |= DrawLimb(actor, i);
            return result;
        }

        #endregion

        #region Private Methods

        int DrawLimb(Actor a, int limb)
        {
            int i;
            int code;
            long baseptr, frameptr;
            CostumeData cost = a.Cost;

            // If the specified limb is stopped or not existing, do nothing.
            if ((cost.Curpos[limb] == 0xFFFF) || ((cost.Stopped & (1 << limb)) > 0))
                return 0;

            // Determine the position the limb is at
            i = cost.Curpos[limb] & 0x7FFF;

            baseptr = _loaded.BasePtr;

            // Get the frame pointer for that limb
            _loaded.CostumeReader.BaseStream.Seek(_loaded.FrameOffsets + limb * 2, System.IO.SeekOrigin.Begin);

            frameptr = baseptr + _loaded.CostumeReader.ReadUInt16();

            // Determine the offset to the costume data for the limb at position i
            _loaded.CostumeReader.BaseStream.Seek(_loaded.AnimCmds + i, System.IO.SeekOrigin.Begin);
            code = _loaded.CostumeReader.ReadByte() & 0x7F;

            // Code 0x7B indicates a limb for which there is nothing to draw
            if (code != 0x7B)
            {
                _loaded.CostumeReader.BaseStream.Seek(frameptr + code * 2, System.IO.SeekOrigin.Begin);
                _srcptr = baseptr + _loaded.CostumeReader.ReadUInt16();

                int xmoveCur, ymoveCur;

                _loaded.CostumeReader.BaseStream.Seek(_srcptr, System.IO.SeekOrigin.Begin);

                if (!_vm.Game.Features.HasFlag(GameFeatures.Old256) || code < 0x79)
                {
                    if (_loaded.Format == 0x57)
                    {
                        _width = _loaded.CostumeReader.ReadByte() * 8;
                        _height = _loaded.CostumeReader.ReadByte();
                        xmoveCur = _xmove + (sbyte)_loaded.CostumeReader.ReadByte() * 8;
                        ymoveCur = _ymove - (sbyte)_loaded.CostumeReader.ReadByte();
                        _xmove += (sbyte)_loaded.CostumeReader.ReadByte() * 8;
                        _ymove -= (sbyte)_loaded.CostumeReader.ReadByte();
                        _srcptr += 6;
                    }
                    else
                    {
                        _width = _loaded.CostumeReader.ReadUInt16();
                        _height = _loaded.CostumeReader.ReadUInt16();
                        xmoveCur = _xmove + _loaded.CostumeReader.ReadInt16();
                        ymoveCur = _ymove + _loaded.CostumeReader.ReadInt16();
                        _xmove += _loaded.CostumeReader.ReadInt16();
                        _ymove -= _loaded.CostumeReader.ReadInt16();
                        _srcptr += 12;
                    }

                    return MainRoutine(xmoveCur, ymoveCur);
                }
            }
            return 0;
        }

        int MainRoutine(int xmoveCur, int ymoveCur)
        {
            int i, skip = 0;
            byte drawFlag = 1;
            bool use_scaling;
            byte startScaleIndexX;
            int ex1, ex2;
            var rect = new Rect();
            int step;
            var v1 = new Codec1();

            const int ScaletableSize = 128;
            bool newAmiCost = (_vm.Game.Version == 5) && (_vm.Game.Platform == Platform.Amiga);
            v1.Scaletable = smallCostumeScaleTable;

            if (_loaded.NumColors == 32)
            {
                v1.Mask = 7;
                v1.Shr = 3;
            }
            else
            {
                v1.Mask = 15;
                v1.Shr = 4;
            }
            _loaded.CostumeReader.BaseStream.Seek(_srcptr, System.IO.SeekOrigin.Begin);

            switch (_loaded.Format)
            {
                case 0x60:
                case 0x61:
                    // This format is used e.g. in the Sam&Max intro
                    ex1 = _loaded.CostumeReader.ReadByte();
                    ex2 = _loaded.CostumeReader.ReadByte();
                    _srcptr += 2;
                    if (ex1 != 0xFF || ex2 != 0xFF)
                    {
                        _loaded.CostumeReader.BaseStream.Seek(_loaded.FrameOffsets + ex1 * 2, System.IO.SeekOrigin.Begin);
                        ex1 = _loaded.CostumeReader.ReadUInt16();
                        _loaded.CostumeReader.BaseStream.Seek(_loaded.BasePtr + ex1 + ex2 * 2, System.IO.SeekOrigin.Begin);
                        _srcptr = _loaded.BasePtr + _loaded.CostumeReader.ReadUInt16() + 14;
                    }
                    break;
            }

            use_scaling = (ScaleX != 0xFF) || (ScaleY != 0xFF);

            v1.X = ActorX;
            v1.Y = ActorY;

            if (use_scaling)
            {

                /* Scale direction */
                v1.ScaleXStep = -1;
                if (xmoveCur < 0)
                {
                    xmoveCur = -xmoveCur;
                    v1.ScaleXStep = 1;
                }

                // It's possible that the scale indexes will overflow and wrap
                // around to zero, so it's important that we use the same
                // method of accessing it both when calculating the size of the
                // scaled costume, and when drawing it. See bug #1519667.

                if (_mirror)
                {
                    /* Adjust X position */
                    startScaleIndexX = _scaleIndexX = (byte)(ScaletableSize - xmoveCur);
                    for (i = 0; i < xmoveCur; i++)
                    {
                        if (v1.Scaletable[_scaleIndexX++] < ScaleX)
                            v1.X -= v1.ScaleXStep;
                    }

                    rect.Left = rect.Right = v1.X;

                    _scaleIndexX = startScaleIndexX;
                    for (i = 0; i < _width; i++)
                    {
                        if (rect.Right < 0)
                        {
                            skip++;
                            startScaleIndexX = _scaleIndexX;
                        }
                        if (v1.Scaletable[_scaleIndexX++] < ScaleX)
                            rect.Right++;
                    }
                }
                else
                {
                    /* No mirror */
                    /* Adjust X position */
                    startScaleIndexX = _scaleIndexX = (byte)(xmoveCur + ScaletableSize);
                    for (i = 0; i < xmoveCur; i++)
                    {
                        if (v1.Scaletable[_scaleIndexX--] < ScaleX)
                            v1.X += v1.ScaleXStep;
                    }

                    rect.Left = rect.Right = v1.X;

                    _scaleIndexX = startScaleIndexX;
                    for (i = 0; i < _width; i++)
                    {
                        if (rect.Left >= _w)
                        {
                            startScaleIndexX = _scaleIndexX;
                            skip++;
                        }
                        if (v1.Scaletable[_scaleIndexX--] < ScaleX)
                            rect.Left--;
                    }
                }
                _scaleIndexX = startScaleIndexX;

                if (skip != 0)
                    skip--;

                step = -1;
                if (ymoveCur < 0)
                {
                    ymoveCur = -ymoveCur;
                    step = 1;
                }

                _scaleIndexY = (byte)(ScaletableSize - ymoveCur);
                for (i = 0; i < ymoveCur; i++)
                {
                    if (v1.Scaletable[_scaleIndexY++] < ScaleY)
                        v1.Y -= step;
                }

                rect.Top = rect.Bottom = v1.Y;
                _scaleIndexY = (byte)(ScaletableSize - ymoveCur);
                for (i = 0; i < _height; i++)
                {
                    if (v1.Scaletable[_scaleIndexY++] < ScaleY)
                        rect.Bottom++;
                }

                _scaleIndexY = (byte)(ScaletableSize - ymoveCur);
            }
            else
            {
                if (!_mirror)
                    xmoveCur = -xmoveCur;

                v1.X += xmoveCur;
                v1.Y += ymoveCur;

                if (_mirror)
                {
                    rect.Left = v1.X;
                    rect.Right = v1.X + _width;
                }
                else
                {
                    rect.Left = v1.X - _width;
                    rect.Right = v1.X;
                }

                rect.Top = v1.Y;
                rect.Bottom = rect.Top + _height;

            }

            v1.SkipWidth = _width;
            v1.ScaleXStep = _mirror ? 1 : -1;

            if (_vm.Game.Version == 1)
                // V1 games uses 8 x 8 pixels for actors
                _vm.MarkRectAsDirty(_vm.MainVirtScreen, rect.Left, rect.Right + 8, rect.Top, rect.Bottom, ActorID);
            else
                _vm.MarkRectAsDirty(_vm.MainVirtScreen, rect.Left, rect.Right + 1, rect.Top, rect.Bottom, ActorID);

            if (rect.Top >= _h || rect.Bottom <= 0)
                return 0;

            if (rect.Left >= _w || rect.Right <= 0)
                return 0;

            v1.RepLen = 0;

            if (_mirror)
            {
                if (!use_scaling)
                    skip = -v1.X;
                if (skip > 0)
                {
                    if (!newAmiCost && _loaded.Format != 0x57)
                    {
                        v1.SkipWidth -= skip;
                        Codec1IgnorePakCols(v1, skip);
                        v1.X = 0;
                    }
                }
                else
                {
                    skip = rect.Right - _w;
                    if (skip <= 0)
                    {
                        drawFlag = 2;
                    }
                    else
                    {
                        v1.SkipWidth -= skip;
                    }
                }
            }
            else
            {
                if (!use_scaling)
                    skip = rect.Right - _w;
                if (skip > 0)
                {
                    if (!newAmiCost && _loaded.Format != 0x57)
                    {
                        v1.SkipWidth -= skip;
                        Codec1IgnorePakCols(v1, skip);
                        v1.X = _w - 1;
                    }
                }
                else
                {
                    // V1 games uses 8 x 8 pixels for actors
                    if (_loaded.Format == 0x57)
                        skip = -8 - rect.Left;
                    else
                        skip = -1 - rect.Left;
                    if (skip <= 0)
                        drawFlag = 2;
                    else
                        v1.SkipWidth -= skip;
                }
            }

            if (v1.SkipWidth <= 0)
                return 0;

            if (rect.Left < 0)
                rect.Left = 0;

            if (rect.Top < 0)
                rect.Top = 0;

            if (rect.Top > _h)
                rect.Top = _h;

            if (rect.Bottom > _h)
                rect.Bottom = _h;

            if (DrawTop > rect.Top)
                DrawTop = rect.Top;
            if (DrawBottom < rect.Bottom)
                DrawBottom = rect.Bottom;

            if (_height + rect.Top >= 256)
            {
                return 2;
            }

            _pixelsNavigator = new PixelNavigator(startNav);
            _pixelsNavigator.Offset(v1.X, v1.Y);
            v1.DestPtr = _pixelsNavigator;

            v1.MaskPtr = _vm.GetMaskBuffer(0, v1.Y, ZBuffer);

            if (_loaded.Format == 0x57)
            {
                // The v1 costume renderer needs the actor number, which is
                // the same thing as the costume renderer's _actorID.
                ProcC64(v1, ActorID);
            }
            else if (newAmiCost)
            {
                Proc3Amiga(v1);
            }
            else
            {
                Proc3(v1);
            }

            return drawFlag;
        }

        void Codec1IgnorePakCols(Codec1 v1, int num)
        {
            _loaded.CostumeReader.BaseStream.Seek(_srcptr, System.IO.SeekOrigin.Begin);
            num *= _height;

            do
            {
                v1.RepLen = _loaded.CostumeReader.ReadByte();
                v1.RepColor = (byte)(v1.RepLen >> v1.Shr);
                v1.RepLen &= v1.Mask;

                if (v1.RepLen == 0)
                    v1.RepLen = _loaded.CostumeReader.ReadByte();

                do
                {
                    if ((--num) == 0)
                    {
                        _srcptr = _loaded.CostumeReader.BaseStream.Position;
                        return;
                    }
                } while ((--v1.RepLen) != 0);
            } while (true);
        }

        bool MaskAt(int xoff, Codec1 v1, PixelNavigator? mask)
        {
            return (mask.HasValue && ((mask.Value.Read(((v1.X + xoff) / 8)) & ScummHelper.RevBitMask((v1.X + xoff) & 7)) != 0));
        }

        void Line(int c, int p, Codec1 v1, PixelNavigator? mask, PixelNavigator dst, byte color, byte[] palette)
        {
            var pcolor = (color >> c) & 3;
            if (pcolor != 0)
            { 
                if (!MaskAt(p, v1, mask))
                    dst.Write(p, palette[pcolor]);
                if (!MaskAt(p + 1, v1, mask))
                    dst.Write(p + 1, palette[pcolor]);
            }
        }

        void ProcC64(Codec1 v1, int actor)
        {
//            const byte *mask, *src;
//            byte *dst;
            byte len;
            int y;
            uint height;
            byte color;
            bool rep;

            y = v1.Y;
            _loaded.CostumeReader.BaseStream.Seek(_srcptr, System.IO.SeekOrigin.Begin);
            var dst = v1.DestPtr;
            len = v1.RepLen;
            color = v1.RepColor;
            height = (uint)_height;

            v1.SkipWidth /= 8;

            // Set up the palette data
            byte[] palette = new byte[4];
            if (_vm.GetCurrentLights().HasFlag(LightModes.ActorUseColors))
            {
                if (_vm.Game.GameId == GameId.Maniac)
                {
                    palette[1] = v1MMActorPalatte1[actor];
                    palette[2] = v1MMActorPalatte2[actor];
                }
                else
                {
                    // Adjust for C64 version of Zak McKracken
                    palette[1] = (byte)(_vm.Game.Platform == Platform.C64 ? 10 : 8);
                    palette[2] = (byte)_palette[actor];
                }
            }
            else
            {
                palette[2] = 11;
                palette[3] = 11;
            }
            var mask = v1.MaskPtr;

            var skipInit = false;
            if (len != 0)
                skipInit = true;

            do
            {
                if (!skipInit)
                {
                    len = Init(ref color);
                }
                else
                {
                    skipInit = false;
                }

                rep = (len & 0x80) != 0;
                len &= 0x7f;
                while ((len--) != 0)
                {
                    if (!rep)
                        color = _loaded.CostumeReader.ReadByte();

                    if (0 <= y && y < _h && 0 <= v1.X && v1.X < _w)
                    {
                        if (!_mirror)
                        {
                            Line(0, 0, v1, mask, dst, color, palette);
                            Line(2, 2, v1, mask, dst, color, palette);
                            Line(4, 4, v1, mask, dst, color, palette);
                            Line(6, 6, v1, mask, dst, color, palette);
                        }
                        else
                        {
                            Line(6, 0, v1, mask, dst, color, palette);
                            Line(4, 2, v1, mask, dst, color, palette);
                            Line(2, 4, v1, mask, dst, color, palette);
                            Line(0, 6, v1, mask, dst, color, palette);
                        }
                    }
                    dst.OffsetY(1);
                    y++;
                    mask.OffsetY(1);
                    if ((--height) == 0)
                    {
                        if ((--v1.SkipWidth) == 0)
                            return;
                        height = (uint)_height;
                        y = v1.Y;
                        v1.X += 8 * v1.ScaleXStep;
                        if (v1.X < 0 || v1.X >= _w)
                            return;
                        mask = v1.MaskPtr;
                        v1.DestPtr.OffsetX(8 * v1.ScaleXStep);
                        dst = v1.DestPtr;
                    }
                }
            } while (true);
        }

        byte Init(ref byte color)
        {
            var len = _loaded.CostumeReader.ReadByte();
            if ((len & 0x80) != 0)
                color = _loaded.CostumeReader.ReadByte();
            return len;
        }

        void Proc3(Codec1 v1)
        {
            int y = v1.Y;
            _loaded.CostumeReader.BaseStream.Seek(_srcptr, System.IO.SeekOrigin.Begin);
            var dst = new PixelNavigator(v1.DestPtr);
            var len = v1.RepLen;
            uint color = v1.RepColor;
            var height = (uint)_height;

            var scaleIndexY = _scaleIndexY;
            var maskbit = (byte)ScummHelper.RevBitMask(v1.X & 7);
            var mask = new PixelNavigator(v1.MaskPtr);
            mask.OffsetX(v1.X / 8);

            bool ehmerde = (len != 0);

            do
            {
                if (!ehmerde)
                {
                    len = _loaded.CostumeReader.ReadByte();
                    color = (uint)(len >> v1.Shr);
                    len &= v1.Mask;
                    if (len == 0)
                        len = _loaded.CostumeReader.ReadByte();
                }

                do
                {
                    if (!ehmerde)
                    {
                        if (ScaleY == 255 || v1.Scaletable[scaleIndexY++] < ScaleY)
                        {
                            var masked = (y < 0 || y >= _h) || (v1.X < 0 || v1.X >= _w) || ((mask.Read() & maskbit) != 0);

                            if (color != 0 && !masked)
                            {
                                byte pcolor;
                                if ((ShadowMode & 0x20) != 0)
                                {
                                    pcolor = ShadowTable[dst.Read()];
                                }
                                else
                                {
                                    pcolor = (byte)_palette[color];
                                    if (pcolor == 13 && ShadowTable != null)
                                        pcolor = ShadowTable[dst.Read()];
                                }
                                dst.Write(pcolor);
                            }
                            dst.OffsetY(1);
                            mask.OffsetY(1);
                            y++;
                        }
                        if ((--height) == 0)
                        {
                            if ((--v1.SkipWidth) == 0)
                                return;
                            height = (uint)_height;
                            y = v1.Y;

                            scaleIndexY = _scaleIndexY;

                            if (ScaleX == 255 || v1.Scaletable[_scaleIndexX] < ScaleX)
                            {
                                v1.X += v1.ScaleXStep;
                                if (v1.X < 0 || v1.X >= _w)
                                    return;
                                maskbit = (byte)ScummHelper.RevBitMask(v1.X & 7);
                                v1.DestPtr.OffsetX(v1.ScaleXStep);
                            }
                            _scaleIndexX = (byte)(_scaleIndexX + v1.ScaleXStep);
                            dst = new PixelNavigator(v1.DestPtr);
                            mask = new PixelNavigator(v1.MaskPtr);
                            mask.OffsetX(v1.X / 8);
                        }
                    }
                    ehmerde = false;
                } while ((--len) != 0);
            } while (true);
        }

        void Proc3Amiga(Codec1 v1)
        {
            byte len;
            int color;
            bool masked;

            var mask = new PixelNavigator(v1.MaskPtr);
            mask.OffsetX(v1.X / 8);
            var dst = new PixelNavigator(v1.DestPtr);
            byte height = (byte)_height;
            byte width = (byte)_width;
            _loaded.CostumeReader.BaseStream.Seek(_srcptr, System.IO.SeekOrigin.Begin);
            var maskbit = (byte)ScummHelper.RevBitMask(v1.X & 7);
            var y = v1.Y;
            var oldXpos = v1.X;
            var oldScaleIndexX = _scaleIndexX;

            // Indy4 Amiga always uses the room map to match colors to the currently
            // setup palette in the actor code in the original, thus we need to do this
            // mapping over here too.
            var amigaMap = 
                (_vm.Game.Platform == Platform.Amiga && _vm.Game.GameId == GameId.Indy4) ? _vm.Gdi.RoomPalette : null;

            do
            {
                len = _loaded.CostumeReader.ReadByte();
                color = len >> v1.Shr;
                len &= v1.Mask;
                if (len == 0)
                    len = _loaded.CostumeReader.ReadByte();
                do
                {
                    if (ScaleY == 255 || v1.Scaletable[_scaleIndexY] < ScaleY)
                    {
                        masked = (y < 0 || y >= _h) || (v1.X < 0 || v1.X >= _w) || ((mask.Read() & maskbit) != 0);

                        if (color != 0 && !masked)
                        {
                            byte pcolor;
                            if (amigaMap != null)
                                pcolor = amigaMap[_palette[color]];
                            else
                                pcolor = (byte)_palette[color];
                            dst.Write(pcolor);
                        }

                        if (ScaleX == 255 || v1.Scaletable[_scaleIndexX] < ScaleX)
                        {
                            v1.X += v1.ScaleXStep;
                            dst.OffsetX(v1.ScaleXStep);
                            maskbit = (byte)ScummHelper.RevBitMask(v1.X & 7);
                        }
                        _scaleIndexX += (byte)v1.ScaleXStep;
                        mask = new PixelNavigator(v1.MaskPtr);
                        mask.OffsetX(v1.X / 8);
                    }
                    if (--width == 0)
                    {
                        if (--height == 0)
                            return;

                        if (y >= _h)
                            return;

                        if (v1.X != oldXpos)
                        {
                            dst.Offset(-(v1.X - oldXpos), 1);
                            mask = new PixelNavigator(v1.MaskPtr);
                            mask.OffsetY(1);
                            v1.MaskPtr = mask;
                            mask.OffsetX(oldXpos / 8);
                            maskbit = (byte)ScummHelper.RevBitMask(oldXpos & 7);
                            y++;
                        }
                        width = (byte)_width;
                        v1.X = oldXpos;
                        _scaleIndexX = oldScaleIndexX;
                        _scaleIndexY++;
                    }
                } while (--len != 0);
            } while (true);
        }

        #endregion
    }
}
