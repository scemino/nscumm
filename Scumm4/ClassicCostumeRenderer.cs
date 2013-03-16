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

using Scumm4.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class ClassicCostumeRenderer : ICostumeRenderer
    {
        byte[] SmallCostumeScaleTable = new byte[256] {
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

        class Codec1
        {
            // Parameters for the original ("V1") costume codec.
            // These ones are accessed from ARM code. Don't reorder.
            public int x;
            public int y;
            public byte[] scaletable;
            public int skip_width;
            public PixelNavigator destptr;
            public PixelNavigator mask_ptr;
            public int scaleXstep;
            public byte mask, shr;
            public byte repcolor;
            public byte replen;
        }

        public int DrawTop { get; set; }
        public int DrawBottom { get; set; }

        public byte ActorID { get; set; }

        public byte ShadowMode { get; set; }

        public int ActorX { get; set; }
        public int ActorY { get; set; }

        public byte ZBuffer { get; set; }

        public byte ScaleX { get; set; }
        public byte ScaleY { get; set; }

        private ushort[] _palette;
        private ClassicCostumeLoader _loaded;
        private ScummEngine _vm;

        /// <summary>
        /// Indicates whether to draw the actor mirrored.
        /// </summary>
        private bool _mirror;

        byte _scaleIndexX;						/* must wrap at 256 */
        byte _scaleIndexY;

        // current move offset
        int _xmove, _ymove;

        private long _srcptr;

        // width and height of cel to decode
        int _width, _height;

        // _out
        private PixelNavigator _pixelsNavigator;
        private PixelNavigator startNav;
        private int _w;
        private int _h;
        private int _numStrips;

        public ClassicCostumeRenderer(ScummEngine vm)
        {
            _vm = vm;
            _loaded = new ClassicCostumeLoader(vm.Index);
            _palette = new ushort[32];
        }

        public void SetPalette(ushort[] palette)
        {
            if (_loaded._format == 0x57)
            {
                for (int i = 0; i < 13; i++)
                    _palette[i] = palette[i];
            }
            else
            {
                if (_vm.GetCurrentLights().HasFlag(LightModes.ActorUseColors))
                {
                    for (int i = 0; i < _loaded._numColors; i++)
                    {
                        byte color = (byte)palette[i];
                        if (color == 255)
                            color = _loaded._palette[i];
                        _palette[i] = color;
                    }
                }
                else
                {
                    for (int i = 0; i < _loaded._numColors; i++)
                    {
                        _palette[i] = 8;
                    }
                    _palette[12] = 0;
                }
            }
        }

        public void SetFacing(Actor actor)
        {
            _mirror = ScummHelper.NewDirToOldDir(actor.GetFacing()) != 0 || _loaded._mirror;
        }

        public void SetCostume(int costume, int shadow)
        {
            _loaded.LoadCostume(costume);
        }

        public int DrawCostume(VirtScreen vs, int numStrips, Actor actor)
        {
            var pixelsNavigator = new PixelNavigator(vs.Surfaces[0]);
            pixelsNavigator.OffsetX(vs.XStart);

            ActorX += (vs.XStart & 7);
            _w = vs.Width;
            _h = vs.Height;
            pixelsNavigator.OffsetX(-(vs.XStart & 7));
            startNav = new PixelNavigator(pixelsNavigator);

            _numStrips = numStrips;

            _xmove = _ymove = 0;

            int result = 0;
            for (int i = 0; i < 16; i++)
                result |= DrawLimb(actor, i);
            return result;
        }

        private int DrawLimb(Actor a, int limb)
        {
            int i;
            int code;
            long baseptr, frameptr;
            CostumeData cost = a._cost;

            // If the specified limb is stopped or not existing, do nothing.
            if ((cost.curpos[limb] == 0xFFFF) || ((cost.stopped & (1 << limb)) > 0))
                return 0;

            // Determine the position the limb is at
            i = cost.curpos[limb] & 0x7FFF;

            baseptr = _loaded._baseptr;

            // Get the frame pointer for that limb
            _loaded._costumeReader.BaseStream.Seek(_loaded._frameOffsets + limb * 2, System.IO.SeekOrigin.Begin);

            frameptr = baseptr + _loaded._costumeReader.ReadUInt16();

            // Determine the offset to the costume data for the limb at position i
            _loaded._costumeReader.BaseStream.Seek(_loaded._animCmds + i, System.IO.SeekOrigin.Begin);
            code = _loaded._costumeReader.ReadByte() & 0x7F;

            // Code 0x7B indicates a limb for which there is nothing to draw
            if (code != 0x7B)
            {
                _loaded._costumeReader.BaseStream.Seek(frameptr + code * 2, System.IO.SeekOrigin.Begin);
                _srcptr = baseptr + _loaded._costumeReader.ReadUInt16();


                int xmoveCur, ymoveCur;

                _loaded._costumeReader.BaseStream.Seek(_srcptr, System.IO.SeekOrigin.Begin);

                if (_loaded._format == 0x57)
                {
                    _width = _loaded._costumeReader.ReadByte() * 8;
                    _height = _loaded._costumeReader.ReadByte();
                    xmoveCur = _xmove + (sbyte)_loaded._costumeReader.ReadByte() * 8;
                    ymoveCur = _ymove - (sbyte)_loaded._costumeReader.ReadByte();
                    _xmove += (sbyte)_loaded._costumeReader.ReadByte() * 8;
                    _ymove -= (sbyte)_loaded._costumeReader.ReadByte();
                    _srcptr += 6;
                }
                else
                {
                    _width = _loaded._costumeReader.ReadUInt16();
                    _height = _loaded._costumeReader.ReadUInt16();
                    xmoveCur = _xmove + _loaded._costumeReader.ReadInt16();
                    ymoveCur = _ymove + _loaded._costumeReader.ReadInt16();
                    _xmove += _loaded._costumeReader.ReadInt16();
                    _ymove -= _loaded._costumeReader.ReadInt16();
                    _srcptr += 12;
                }

                return MainRoutine(xmoveCur, ymoveCur);
            }
            return 0;
        }

        private int MainRoutine(int xmoveCur, int ymoveCur)
        {
            int i, skip = 0;
            byte drawFlag = 1;
            bool use_scaling;
            byte startScaleIndexX;
            int ex1, ex2;
            Rect rect = new Rect();
            int step;
            Codec1 v1 = new Codec1();

            const int ScaletableSize = 128;

            v1.scaletable = SmallCostumeScaleTable;

            if (_loaded._numColors == 32)
            {
                v1.mask = 7;
                v1.shr = 3;
            }
            else
            {
                v1.mask = 15;
                v1.shr = 4;
            }
            _loaded._costumeReader.BaseStream.Seek(_srcptr, System.IO.SeekOrigin.Begin);

            switch (_loaded._format)
            {
                case 0x60:
                case 0x61:
                    // This format is used e.g. in the Sam&Max intro
                    ex1 = _loaded._costumeReader.ReadByte();
                    ex2 = _loaded._costumeReader.ReadByte();
                    _srcptr += 2;
                    if (ex1 != 0xFF || ex2 != 0xFF)
                    {
                        _loaded._costumeReader.BaseStream.Seek(_loaded._frameOffsets + ex1 * 2, System.IO.SeekOrigin.Begin);
                        ex1 = _loaded._costumeReader.ReadUInt16();
                        _loaded._costumeReader.BaseStream.Seek(_loaded._baseptr + ex1 + ex2 * 2, System.IO.SeekOrigin.Begin);
                        _srcptr = _loaded._baseptr + _loaded._costumeReader.ReadUInt16() + 14;
                    }
                    break;
            }

            use_scaling = (ScaleX != 0xFF) || (ScaleY != 0xFF);

            v1.x = ActorX;
            v1.y = ActorY;

            if (use_scaling)
            {

                /* Scale direction */
                v1.scaleXstep = -1;
                if (xmoveCur < 0)
                {
                    xmoveCur = -xmoveCur;
                    v1.scaleXstep = 1;
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
                        if (v1.scaletable[_scaleIndexX++] < ScaleX)
                            v1.x -= v1.scaleXstep;
                    }

                    rect.left = rect.right = v1.x;

                    _scaleIndexX = startScaleIndexX;
                    for (i = 0; i < _width; i++)
                    {
                        if (rect.right < 0)
                        {
                            skip++;
                            startScaleIndexX = _scaleIndexX;
                        }
                        if (v1.scaletable[_scaleIndexX++] < ScaleX)
                            rect.right++;
                    }
                }
                else
                {
                    /* No mirror */
                    /* Adjust X position */
                    startScaleIndexX = _scaleIndexX = (byte)(xmoveCur + ScaletableSize);
                    for (i = 0; i < xmoveCur; i++)
                    {
                        if (v1.scaletable[_scaleIndexX--] < ScaleX)
                            v1.x += v1.scaleXstep;
                    }

                    rect.left = rect.right = v1.x;

                    _scaleIndexX = startScaleIndexX;
                    for (i = 0; i < _width; i++)
                    {
                        if (rect.left >= _w)
                        {
                            startScaleIndexX = _scaleIndexX;
                            skip++;
                        }
                        if (v1.scaletable[_scaleIndexX--] < ScaleX)
                            rect.left--;
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
                    if (v1.scaletable[_scaleIndexY++] < ScaleY)
                        v1.y -= step;
                }

                rect.top = rect.bottom = v1.y;
                _scaleIndexY = (byte)(ScaletableSize - ymoveCur);
                for (i = 0; i < _height; i++)
                {
                    if (v1.scaletable[_scaleIndexY++] < ScaleY)
                        rect.bottom++;
                }

                _scaleIndexY = (byte)(ScaletableSize - ymoveCur);
            }
            else
            {
                if (!_mirror)
                    xmoveCur = -xmoveCur;

                v1.x += xmoveCur;
                v1.y += ymoveCur;

                if (_mirror)
                {
                    rect.left = v1.x;
                    rect.right = v1.x + _width;
                }
                else
                {
                    rect.left = v1.x - _width;
                    rect.right = v1.x;
                }

                rect.top = v1.y;
                rect.bottom = rect.top + _height;

            }

            v1.skip_width = _width;
            v1.scaleXstep = _mirror ? 1 : -1;

            _vm.MarkRectAsDirty(_vm.MainVirtScreen, rect.left, rect.right + 1, rect.top, rect.bottom, ActorID);

            if (rect.top >= _h || rect.bottom <= 0)
                return 0;

            if (rect.left >= _w || rect.right <= 0)
                return 0;

            v1.replen = 0;

            if (_mirror)
            {
                if (!use_scaling)
                    skip = -v1.x;
                if (skip > 0)
                {
                    if (_loaded._format != 0x57)
                    {
                        v1.skip_width -= skip;
                        Codec1IgnorePakCols(v1, skip);
                        v1.x = 0;
                    }
                }
                else
                {
                    skip = rect.right - _w;
                    if (skip <= 0)
                    {
                        drawFlag = 2;
                    }
                    else
                    {
                        v1.skip_width -= skip;
                    }
                }
            }
            else
            {
                if (!use_scaling)
                    skip = rect.right - _w;
                if (skip > 0)
                {
                    if (_loaded._format != 0x57)
                    {
                        v1.skip_width -= skip;
                        Codec1IgnorePakCols(v1, skip);
                        v1.x = _w - 1;
                    }
                }
                else
                {
                    // V1 games uses 8 x 8 pixels for actors
                    if (_loaded._format == 0x57)
                        skip = -8 - rect.left;
                    else
                        skip = -1 - rect.left;
                    if (skip <= 0)
                        drawFlag = 2;
                    else
                        v1.skip_width -= skip;
                }
            }

            if (v1.skip_width <= 0)
                return 0;

            if (rect.left < 0)
                rect.left = 0;

            if (rect.top < 0)
                rect.top = 0;

            if (rect.top > _h)
                rect.top = _h;

            if (rect.bottom > _h)
                rect.bottom = _h;

            if (DrawTop > rect.top)
                DrawTop = rect.top;
            if (DrawBottom < rect.bottom)
                DrawBottom = rect.bottom;

            if (_height + rect.top >= 256)
            {
                return 2;
            }

            _pixelsNavigator = new PixelNavigator(startNav);
            _pixelsNavigator.Offset(v1.x, v1.y);
            v1.destptr = _pixelsNavigator;

            v1.mask_ptr = _vm.GetMaskBuffer(0, v1.y, ZBuffer);

            Proc3(v1);

            return drawFlag;
        }

        private void Codec1IgnorePakCols(Codec1 v1, int num)
        {
            _loaded._costumeReader.BaseStream.Seek(_srcptr, System.IO.SeekOrigin.Begin);
            num *= _height;

            do
            {
                v1.replen = _loaded._costumeReader.ReadByte();
                v1.repcolor = (byte)(v1.replen >> v1.shr);
                v1.replen &= v1.mask;

                if (v1.replen == 0)
                    v1.replen = _loaded._costumeReader.ReadByte();

                do
                {
                    if ((--num) == 0)
                    {
                        _srcptr = _loaded._costumeReader.BaseStream.Position;
                        return;
                    }
                } while ((--v1.replen) != 0);
            } while (true);
        }

        private void Proc3(Codec1 v1)
        {
            PixelNavigator dst;
            byte len, maskbit;
            int y;
            uint color, height, pcolor;
            byte scaleIndexY;
            bool masked;

            y = v1.y;
            _loaded._costumeReader.BaseStream.Seek(_srcptr, System.IO.SeekOrigin.Begin);
            dst = new PixelNavigator(v1.destptr);
            len = v1.replen;
            color = v1.repcolor;
            height = (uint)_height;

            scaleIndexY = _scaleIndexY;
            maskbit = (byte)ScummHelper.RevBitMask(v1.x & 7);
            var mask = new PixelNavigator(v1.mask_ptr);
            mask.OffsetX(v1.x / 8);

            bool ehmerde = false;
            if (len != 0)
            {
                ehmerde = true;
            }

            do
            {
                if (!ehmerde)
                {
                    len = _loaded._costumeReader.ReadByte();
                    color = (uint)(len >> v1.shr);
                    len &= v1.mask;
                    if (len == 0)
                        len = _loaded._costumeReader.ReadByte();
                }

                do
                {
                    if (!ehmerde)
                    {
                        if (ScaleY == 255 || v1.scaletable[scaleIndexY++] < ScaleY)
                        {
                            masked = (y < 0 || y >= _h) || (v1.x < 0 || v1.x >= _w) || ((mask.Read() & maskbit) != 0);

                            if (color != 0 && !masked)
                            {
                                pcolor = _palette[color];
                                dst.Write((byte)pcolor);
                            }
                            dst.OffsetY(1);
                            mask.OffsetY(1);
                            y++;
                        }
                        if ((--height) == 0)
                        {
                            if ((--v1.skip_width) == 0)
                                return;
                            height = (uint)_height;
                            y = v1.y;

                            scaleIndexY = _scaleIndexY;

                            if (ScaleX == 255 || v1.scaletable[_scaleIndexX] < ScaleX)
                            {
                                v1.x += v1.scaleXstep;
                                if (v1.x < 0 || v1.x >= _w)
                                    return;
                                maskbit = (byte)ScummHelper.RevBitMask(v1.x & 7);
                                v1.destptr.OffsetX(v1.scaleXstep);
                            }
                            _scaleIndexX = (byte)(_scaleIndexX + v1.scaleXstep);
                            dst = new PixelNavigator(v1.destptr);
                            mask = new PixelNavigator(v1.mask_ptr);
                            mask.OffsetX(v1.x / 8);
                        }
                    }
                    ehmerde = false;
                } while ((--len) != 0);
            } while (true);
        }
    }
}
