//
//  AkosRenderer.cs
//
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
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Graphics
{
    enum AkosOpcode: ushort
    {
        Return = 0xC001,
        SetVar = 0xC010,
        CmdQue3 = 0xC015,
        C016 = 0xC016,
        C017 = 0xC017,
        C018 = 0xC018,
        C019 = 0xC019,
        ComplexChan = 0xC020,
        C021 = 0xC021,
        C022 = 0xC022,
        ComplexChan2 = 0xC025,
        Jump = 0xC030,
        JumpIfSet = 0xC031,
        AddVar = 0xC040,
        C042 = 0xC042,
        C044 = 0xC044,
        C045 = 0xC045,
        C046 = 0xC046,
        C047 = 0xC047,
        C048 = 0xC048,
        Ignore = 0xC050,
        IncVar = 0xC060,
        CmdQue3Quick = 0xC061,
        JumpStart = 0xC070,
        JumpE = 0xC070,
        JumpNE = 0xC071,
        JumpL = 0xC072,
        JumpLE = 0xC073,
        JumpG = 0xC074,
        JumpGE = 0xC075,
        StartAnim = 0xC080,
        StartVarAnim = 0xC081,
        Random = 0xC082,
        SetActorClip = 0xC083,
        StartAnimInActor = 0xC084,
        SetVarInActor = 0xC085,
        HideActor = 0xC086,
        SetDrawOffs = 0xC087,
        JumpTable = 0xC088,
        SoundStuff = 0xC089,
        Flip = 0xC08A,
        Cmd3 = 0xC08B,
        Ignore3 = 0xC08C,
        Ignore2 = 0xC08D,
        C08E = 0xC08E,
        SkipStart = 0xC090,
        SkipE = 0xC090,
        SkipNE = 0xC091,
        SkipL = 0xC092,
        SkipLE = 0xC093,
        SkipG = 0xC094,
        SkipGE = 0xC095,
        ClearFlag = 0xC09F,
        C0A0 = 0xC0A0,
        C0A1 = 0xC0A1,
        C0A2 = 0xC0A2,
        C0A3 = 0xC0A3,
        C0A4 = 0xC0A4,
        C0A5 = 0xC0A5,
        C0A6 = 0xC0A6,
        C0A7 = 0xC0A7,
        EndSeq = 0xC0FF
    }

    struct CostumeInfo
    {
        public ushort width, height;
        public short rel_x, rel_y;
        public short move_x, move_y;
    }

    class AkosRenderer: ICostumeRenderer
    {
        public AkosRenderer(ScummEngine vm)
        {
            this._vm = vm;
        }

        public void SetPalette(ushort[] new_palette)
        {
            int size, i;

            size = akpl.Length;
            if (size == 0)
                return;

            if (size > 256)
                throw new InvalidOperationException(string.Format("akos_setPalette: {0} is too many colors", size));

//            if (vm.Game.Features.HasFlag(GameFeatures.16BITCOLOR) {
//                if (_paletteNum) {
//                    for (i = 0; i < size; i++)
//                        _palette[i] = READ_LE_ushort(_vm._hePalettes + _paletteNum * _vm._hePaletteSlot + 768 + akpl[i] * 2);
//                } else if (rgbs) {
//                    for (i = 0; i < size; i++) {
//                        if (new_palette[i] == 0xFF) {
//                            byte col = akpl[i];
//                            _palette[i] = _vm.get16BitColor(rgbs[col * 3 + 0], rgbs[col * 3 + 1], rgbs[col * 3 + 2]);
//                        } else {
//                            _palette[i] = new_palette[i];
//                        }
//                    }
//                }
//            } else if (_vm._game.heversion >= 99 && _paletteNum) {
//                for (i = 0; i < size; i++)
//                    _palette[i] = (byte)_vm._hePalettes[_paletteNum * _vm._hePaletteSlot + 768 + akpl[i]];
//            } else {
            for (i = 0; i < size; i++)
            {
                _palette[i] = new_palette[i] != 0xFF ? new_palette[i] : akpl[i];
            }
//            }

//            if (_vm._game.heversion == 70) {
//                for (i = 0; i < size; i++)
//                    _palette[i] = _vm._HEV7ActorPalette[_palette[i]];
//            }

            if (size == 256)
            {
                var color = new_palette[0];
                if (color == 255)
                {
                    _palette[0] = color;
                }
                else
                {
                    _useBompPalette = true;
                }
            }
        }

        public void SetFacing(Actor a)
        {
            _mirror = (ScummHelper.NewDirToOldDir(a.Facing) != 0) || ((akhd.flags & 1) != 0);
            if (a.Flip)
                _mirror = !_mirror;
        }

        public void SetCostume(int costume, int shadow)
        {
            akos = _vm.ResourceManager.GetCostumeData(costume);
            Debug.Assert(akos != null);

            akhd = ResourceFile7.ReadData<AkosHeader>(akos, "AKHD");
            akof = ResourceFile7.FindOffset(akos, "AKOF");
            akci = ResourceFile7.FindOffset(akos, "AKCI");
            aksq = ResourceFile7.ReadData(akos, "AKSQ");
            akcd = ResourceFile7.ReadData(akos, "AKCD");
            akpl = ResourceFile7.ReadData(akos, "AKPL");
            _codec = akhd.codec;
//            akct = ResourceFile7.ReadData(akos,"AKCT");
//            rgbs = ResourceFile7.ReadData(akos,"RGBS");

//            xmap = 0;
//            if (shadow) {
//                const byte *xmapPtr = _vm.getResourceAddress(rtImage, shadow);
//                Debug.Assert(xmapPtr);
//                xmap = ResourceFile7.ReadData(akos,'X','M','A','P'), xmapPtr);
//                Debug.Assert(xmap);
//            }
        }

        public int DrawCostume(VirtScreen vs, int numStrips, Actor actor, bool drawToBackBuf)
        {
            var pixelsNavigator = new PixelNavigator(vs.Surfaces[drawToBackBuf ? 1 : 0]);
            pixelsNavigator.OffsetX(vs.XStart);

            ActorX += (vs.XStart & 7);
            pixelsNavigator.OffsetX(-(vs.XStart & 7));
            startNav = new PixelNavigator(pixelsNavigator);

            if (_vm.Game.IsOldBundle)
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

        public bool SkipLimbs { get; set; }

        public bool ActorHitMode { get; set; }

        public short ActorHitX  { get; set; }

        public short ActorHitY { get; set; }

        public bool ActorHitResult { get; set; }

        byte DrawLimb(Actor a, int limb)
        {
            var cost = a.Cost;
            byte result = 0;

            var lastDx = 0;
            var lastDy = 0;

            if (SkipLimbs)
                return 0;

            if (cost.Active[limb] == 0 || ((cost.Stopped & (1 << limb)) != 0))
                return 0;

            var p = cost.Curpos[limb];

            AkosOpcode code = (AkosOpcode)aksq[p];
            if (((ushort)code & 0x80) != 0)
                code = (AkosOpcode)ScummHelper.SwapBytes(BitConverter.ToUInt16(aksq, p));


            if (code == AkosOpcode.C021 || code == AkosOpcode.C022)
            {
                ushort s = (ushort)(cost.Curpos[limb] + 4);
                var extra = aksq[p + 3];
                byte n = extra;
                p += (ushort)(extra + 2);
                code = (code == AkosOpcode.C021) ? AkosOpcode.ComplexChan : AkosOpcode.ComplexChan2;
            }

            if (code == AkosOpcode.Return || code == AkosOpcode.EndSeq)
                return 0;

            if (code != AkosOpcode.ComplexChan && code != AkosOpcode.ComplexChan2)
            {
                var off = ResourceFile7.ToStructure<AkosOffset>(akos, (int)(akof + 6 * ((ushort)code & 0xFFF)));

                Debug.Assert(((ushort)code & 0xFFF) * 6 < ScummHelper.SwapBytes(BitConverter.ToUInt32(akos, (int)(akof - 4))) - 8);
                Debug.Assert(((ushort)code & 0x7000) == 0);

                _srcptr = (int)off.akcd;
                Debug.Assert(_srcptr < akcd.Length);
                var costumeInfo = ResourceFile7.ToStructure<CostumeInfo>(akos, (int)(akci + off.akci));

                _width = costumeInfo.width;
                _height = costumeInfo.height;
                var xmoveCur = _xmove + costumeInfo.rel_x;
                var ymoveCur = _ymove + costumeInfo.rel_y;
                _xmove += costumeInfo.move_x;
                _ymove -= costumeInfo.move_y;

                switch (_codec)
                {
                    case 1:
                        result |= Codec1(xmoveCur, ymoveCur);
                        break;
                    case 5:
                        result |= Codec5(xmoveCur, ymoveCur);
                        break;
                    case 16:
                        result |= Codec16(xmoveCur, ymoveCur);
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("akos_drawLimb: invalid _codec {0}", _codec));
                }
            }
            else
            {
                if (code == AkosOpcode.ComplexChan2)
                {
                    lastDx = BitConverter.ToInt16(aksq, p + 2);
                    lastDy = BitConverter.ToInt16(aksq, p + 4);
                    p += 4;
                }

                var extra = aksq[p + 2];
                p += 3;

                for (var i = 0; i != extra; i++)
                {
                    code = (AkosOpcode)aksq[p + 4];
                    if (((ushort)code & 0x80) != 0)
                        code = (AkosOpcode)ScummHelper.SwapBytes(BitConverter.ToUInt16(aksq, p + 4));
                    var off = ResourceFile7.ToStructure<AkosOffset>(akos, (int)(akof + 6 * ((ushort)code & 0xFFF)));

                    _srcptr = (int)off.akcd;
                    var costumeInfo = ResourceFile7.ToStructure<CostumeInfo>(akos, (int)(akci + off.akci));

                    _width = costumeInfo.width;
                    _height = costumeInfo.height;

                    var xmoveCur = _xmove + BitConverter.ToInt16(aksq, p + 0);
                    var ymoveCur = _ymove + BitConverter.ToInt16(aksq, p + 2);

                    if (i == extra - 1)
                    {
                        _xmove += lastDx;
                        _ymove -= lastDy;
                    }

                    p += ((aksq[p + 4] & 0x80) != 0) ? (ushort)6 : (ushort)5;

                    switch (_codec)
                    {
                        case 1:
                            result |= Codec1(xmoveCur, ymoveCur);
                            break;
                        case 5:
                            result |= Codec5(xmoveCur, ymoveCur);
                            break;
                        case 16:
                            result |= Codec16(xmoveCur, ymoveCur);
                            break;
                        case 32:
                            result |= Codec32(xmoveCur, ymoveCur);
                            break;
                        default:
                            throw new InvalidOperationException(string.Format("akos_drawLimb: invalid _codec {0}", _codec));
                    }
                }
            }

            return result;
        }

        byte Codec1(int xmoveCur, int ymoveCur)
        {
            int num_colors;
            bool use_scaling;
            int i, j;
            int skip = 0, startScaleIndexX, startScaleIndexY;
            Rect rect;
            int step;
            byte drawFlag = 1;
            var v1 = new Codec1();

            int scaletableSize = 384;

            /* implement custom scale table */

            // FIXME. HACK
            // For some illogical reason gcc 3.4.x produces wrong code if
            // smallCostumeScaleTable from costume.cpp is used here
            // So I had to put copy of it back here as it was before 1.227 revision
            // of this file.
            v1.Scaletable = bigCostumeScaleTable;
            var table = ((ScummEngine7)_vm).GetStringAddressVar(((ScummEngine7)_vm).VariableCustomScaleTable);
            if (table != null)
            {
                v1.Scaletable = table;
            }

            // Setup color decoding variables
            num_colors = akpl.Length;
            if (num_colors == 32)
            {
                v1.Mask = 7;
                v1.Shr = 3;
            }
            else if (num_colors == 64)
            {
                v1.Mask = 3;
                v1.Shr = 2;
            }
            else
            {
                v1.Mask = 15;
                v1.Shr = 4;
            }

            use_scaling = (ScaleX != 0xFF) || (ScaleY != 0xFF);

            v1.X = ActorX;
            v1.Y = ActorY;

            v1.BoundsRect.Left = 0;
            v1.BoundsRect.Top = 0;
            v1.BoundsRect.Right = _vm.MainVirtScreen.Width;
            v1.BoundsRect.Bottom = _vm.MainVirtScreen.Height;

            if (use_scaling)
            {

                /* Scale direction */
                v1.ScaleXStep = -1;
                if (xmoveCur < 0)
                {
                    xmoveCur = -xmoveCur;
                    v1.ScaleXStep = 1;
                }

                if (_mirror)
                {
                    /* Adjust X position */
                    startScaleIndexX = j = scaletableSize - xmoveCur;
                    for (i = 0; i < xmoveCur; i++)
                    {
                        if (v1.Scaletable[j++] < ScaleX)
                            v1.X -= v1.ScaleXStep;
                    }

                    rect.Left = rect.Right = v1.X;

                    j = startScaleIndexX;
                    for (i = 0, skip = 0; i < _width; i++)
                    {
                        if (rect.Right < 0)
                        {
                            skip++;
                            startScaleIndexX = j;
                        }
                        if (v1.Scaletable[j++] < ScaleX)
                            rect.Right++;
                    }
                }
                else
                {
                    /* No mirror */
                    /* Adjust X position */
                    startScaleIndexX = j = scaletableSize + xmoveCur;
                    for (i = 0; i < xmoveCur; i++)
                    {
                        if (v1.Scaletable[j--] < ScaleX)
                            v1.X += v1.ScaleXStep;
                    }

                    rect.Left = rect.Right = v1.X;

                    j = startScaleIndexX;
                    for (i = 0; i < _width; i++)
                    {
                        if (rect.Left >= v1.BoundsRect.Right)
                        {
                            startScaleIndexX = j;
                            skip++;
                        }
                        if (v1.Scaletable[j--] < ScaleX)
                            rect.Left--;
                    }
                }

                if (skip != 0)
                    skip--;

                step = -1;
                if (ymoveCur < 0)
                {
                    ymoveCur = -ymoveCur;
                    step = -step;
                }

                startScaleIndexY = scaletableSize - ymoveCur;
                for (i = 0; i < ymoveCur; i++)
                {
                    if (v1.Scaletable[startScaleIndexY++] < ScaleY)
                        v1.Y -= step;
                }

                rect.Top = rect.Bottom = v1.Y;
                startScaleIndexY = scaletableSize - ymoveCur;
                for (i = 0; i < _height; i++)
                {
                    if (v1.Scaletable[startScaleIndexY++] < ScaleY)
                        rect.Bottom++;
                }

                startScaleIndexY = scaletableSize - ymoveCur;
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

                startScaleIndexX = scaletableSize;
                startScaleIndexY = scaletableSize;
            }

            v1.ScaleXIndex = startScaleIndexX;
            v1.ScaleYIndex = startScaleIndexY;
            v1.SkipWidth = _width;
            v1.ScaleXStep = _mirror ? 1 : -1;

            if (ActorHitMode)
            {
                if (ActorHitX < rect.Left || ActorHitX >= rect.Right || ActorHitY < rect.Top || ActorHitY >= rect.Bottom)
                    return 0;
            }
            else
                MarkRectAsDirty(rect);

            if (rect.Top >= v1.BoundsRect.Bottom || rect.Bottom <= v1.BoundsRect.Top)
                return 0;

            if (rect.Left >= v1.BoundsRect.Right || rect.Right <= v1.BoundsRect.Left)
                return 0;

            v1.RepLen = 0;

            if (_mirror)
            {
                if (!use_scaling)
                    skip = v1.BoundsRect.Left - v1.X;

                if (skip > 0)
                {
                    v1.SkipWidth -= skip;
                    Codec1IgnorePakCols(v1, skip);
                    v1.X = v1.BoundsRect.Left;
                }
                else
                {
                    skip = rect.Right - v1.BoundsRect.Right;
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
                    skip = rect.Right - v1.BoundsRect.Right + 1;
                if (skip > 0)
                {
                    v1.SkipWidth -= skip;
                    Codec1IgnorePakCols(v1, skip);
                    v1.X = v1.BoundsRect.Right - 1;
                }
                else
                {
                    skip = (v1.BoundsRect.Left - 1) - rect.Left;

                    if (skip <= 0)
                        drawFlag = 2;
                    else
                        v1.SkipWidth -= skip;
                }
            }

            if (v1.SkipWidth <= 0 || _height <= 0)
                return 0;

            if (rect.Left < v1.BoundsRect.Left)
                rect.Left = v1.BoundsRect.Left;

            if (rect.Top < v1.BoundsRect.Top)
                rect.Top = v1.BoundsRect.Top;

            if (rect.Top > v1.BoundsRect.Bottom)
                rect.Top = v1.BoundsRect.Bottom;

            if (rect.Bottom > v1.BoundsRect.Bottom)
                rect.Bottom = v1.BoundsRect.Bottom;

            if (DrawTop > rect.Top)
                DrawTop = rect.Top;
            if (DrawBottom < rect.Bottom)
                DrawBottom = rect.Bottom;

            _pixelsNavigator = new PixelNavigator(startNav);
            _pixelsNavigator.Offset(v1.X, v1.Y);
            v1.DestPtr = _pixelsNavigator;

            Codec1GenericDecode(v1);

            return drawFlag;
        }

        byte Codec5(int xmoveCur, int ymoveCur)
        {
            Rect clip;
            int maxw, maxh;

            if (ActorHitMode)
            {
                throw new NotImplementedException("codec5: _actorHitMode not yet implemented");
            }

            if (!_mirror)
            {
                clip.Left = (ActorX - xmoveCur - _width) + 1;
            }
            else
            {
                clip.Left = ActorX + xmoveCur - 1;
            }

            clip.Top = ActorY + ymoveCur;
            clip.Right = clip.Left + _width;
            clip.Bottom = clip.Top + _height;
            maxw = _vm.MainVirtScreen.Width;
            maxh = _vm.MainVirtScreen.Height;

            MarkRectAsDirty(clip);

            clip.Clip(maxw, maxh);

            if ((clip.Left >= clip.Right) || (clip.Top >= clip.Bottom))
                return 0;

            if (DrawTop > clip.Top)
                DrawTop = clip.Top;
            if (DrawBottom < clip.Bottom)
                DrawBottom = clip.Bottom;

            var bdd = new BompDrawData();

            bdd.Dst = startNav;
            if (!_mirror)
            {
                bdd.X = (ActorX - xmoveCur - _width) + 1;
            }
            else
            {
                bdd.X = ActorX + xmoveCur;
            }
            bdd.Y = ActorY + ymoveCur;

            var src = new byte[akcd.Length - _srcptr];
            Array.Copy(akcd, _srcptr, src, 0, src.Length);
            bdd.Src = src;
            bdd.Width = _width;
            bdd.Height = _height;

            bdd.ScaleX = 255;
            bdd.ScaleY = 255;

            bdd.MaskPtr = _vm.GetMaskBuffer(0, 0, ZBuffer);
            bdd.NumStrips = _vm.Gdi.NumStrips;

            bdd.ShadowMode = ShadowMode;
            bdd.ShadowPalette = _vm.ShadowPalette;

            bdd.ActorPalette = _useBompPalette ? _palette : null;

            bdd.Mirror = !_mirror;

            bdd.DrawBomp();

            _useBompPalette = false;

            return 0;
        }

        void Codec1GenericDecode(Codec1 v1)
        {
            bool skip_column = false;

            var y = v1.Y;
            var src = _srcptr;
            var dst = v1.DestPtr;
            var len = v1.RepLen;
            var color = v1.RepColor;
            var height = _height;

            var scaleytab = v1.ScaleYIndex;
            var maskbit = ScummHelper.RevBitMask(v1.X & 7);

            var mask = _vm.GetMaskBuffer(v1.X - (_vm.MainVirtScreen.XStart & 7), v1.Y, ZBuffer);

            bool ehmerde = (len != 0);

            do
            {
                if (!ehmerde)
                {
                    len = akcd[src++];
                    color = (byte)(len >> v1.Shr);
                    len &= v1.Mask;
                    if (len == 0)
                        len = akcd[src++];
                }

                do
                {
                    if (!ehmerde)
                    {
                        if (ScaleY == 255 || v1.Scaletable[scaleytab++] < ScaleY)
                        {
                            if (ActorHitMode)
                            {
                                if (color != 0 && y == ActorHitY && v1.X == ActorHitX)
                                {
                                    ActorHitResult = true;
                                    return;
                                }
                            }
                            else
                            {
                                bool masked = (y < v1.BoundsRect.Top || y >= v1.BoundsRect.Bottom) || (v1.X < 0 || v1.X >= v1.BoundsRect.Right) || ((mask.Read() & maskbit) != 0);

                                if (color != 0 && !masked && !skip_column)
                                {
                                    var pcolor = _palette[color];
                                    if (ShadowMode == 1)
                                    {
                                        if (pcolor == 13)
                                            pcolor = ShadowTable[dst.Read()];
                                    }
                                    else if (ShadowMode == 2)
                                    {
                                        throw new NotImplementedException("codec1_spec2"); // TODO
                                    }
                                    else if (ShadowMode == 3)
                                    {
                                        if (_vm.Game.Features.HasFlag(GameFeatures.Is16BitColor))
                                        {
                                            ushort srcColor = (ushort)((pcolor >> 1) & 0x7DEF);
                                            ushort dstColor = (ushort)((dst.ReadUInt16() >> 1) & 0x7DEF);
                                            pcolor = (ushort)(srcColor + dstColor);
                                        }
                                        else if (pcolor < 8)
                                        {
                                            pcolor = (ushort)((pcolor << 8) + dst.Read());
                                            pcolor = ShadowTable[pcolor];
                                        }
                                    }
                                    if (_vm.MainVirtScreen.BytesPerPixel == 2)
                                    {
                                        dst.WriteUInt16(pcolor);
                                    }
                                    else
                                    {
                                        dst.Write((byte)pcolor);
                                    }
                                }
                            }
                            dst.OffsetY(1);
                            mask.OffsetY(1);
                            y++;
                        }
                        if (--height == 0)
                        {
                            if (--v1.SkipWidth == 0)
                                return;
                            height = _height;
                            y = v1.Y;

                            scaleytab = v1.ScaleYIndex;

                            if (ScaleX == 255 || v1.Scaletable[v1.ScaleXIndex] < ScaleX)
                            {
                                v1.X += v1.ScaleXStep;
                                if (v1.X < 0 || v1.X >= v1.BoundsRect.Right)
                                    return;
                                maskbit = ScummHelper.RevBitMask(v1.X & 7);
                                v1.DestPtr.OffsetX(v1.ScaleXStep);
                                skip_column = false;
                            }
                            else
                                skip_column = true;
                            v1.ScaleXIndex += v1.ScaleXStep;
                            dst = v1.DestPtr;
                            mask = _vm.GetMaskBuffer(v1.X - (_vm.MainVirtScreen.XStart & 7), v1.Y, ZBuffer);
                        }
                    }
                    ehmerde = false;
                } while (--len != 0);
            } while (true);
        }

        byte Codec16(int xmoveCur, int ymoveCur)
        {
            Debug.Assert(_vm.MainVirtScreen.BytesPerPixel == 1);

            if (ActorHitMode)
            {
//                Console.Error.WriteLine("codec16: _actorHitMode not yet implemented");
                return 0;
            }

            Rect clip;
            if (!_mirror)
            {
                clip.Left = (ActorX - xmoveCur - _width) + 1;
            }
            else
            {
                clip.Left = ActorX + xmoveCur;
            }

            clip.Top = ActorY + ymoveCur;
            clip.Right = clip.Left + _width;
            clip.Bottom = clip.Top + _height;

            var minx = 0;
            var miny = 0;
            var maxw = _vm.MainVirtScreen.Width;
            var maxh = _vm.MainVirtScreen.Height;

            MarkRectAsDirty(clip);

            var skip_x = 0;
            var skip_y = 0;
            var cur_x = _width - 1;
            var cur_y = _height - 1;

            if (clip.Left < minx)
            {
                skip_x = -clip.Left;
                clip.Left = 0;
            }

            if (clip.Right > maxw)
            {
                cur_x -= clip.Right - maxw;
                clip.Right = maxw;
            }

            if (clip.Top < miny)
            {
                skip_y -= clip.Top;
                clip.Top = 0;
            }

            if (clip.Bottom > maxh)
            {
                cur_y -= clip.Bottom - maxh;
                clip.Bottom = maxh;
            }

            if ((clip.Left >= clip.Right) || (clip.Top >= clip.Bottom))
                return 0;

            if (DrawTop > clip.Top)
                DrawTop = clip.Top;
            if (DrawBottom < clip.Bottom)
                DrawBottom = clip.Bottom;

            int width_unk;

            var height_unk = clip.Top;
            int dir;

            if (!_mirror)
            {
                dir = -1;

                int tmp_skip_x = skip_x;
                skip_x = _width - 1 - cur_x;
                cur_x = _width - 1 - tmp_skip_x;
                width_unk = clip.Right - 1;
            }
            else
            {
                dir = 1;
                width_unk = clip.Left;
            }

            var out_height = cur_y - skip_y;
            if (out_height < 0)
            {
                out_height = -out_height;
            }
            out_height++;

            cur_x -= skip_x;
            if (cur_x < 0)
            {
                cur_x = -cur_x;
            }
            cur_x++;

            int numskip_before = skip_x + (skip_y * _width);
            int numskip_after = _width - cur_x;

            _pixelsNavigator = new PixelNavigator(_vm.MainVirtScreen.Surfaces[0]);
            _pixelsNavigator.GoTo(width_unk, height_unk);

            Akos16Decompress(_pixelsNavigator, _srcptr, cur_x, out_height, dir, numskip_before, numskip_after, 255, clip.Left, clip.Top, ZBuffer);
            return 0;
        }

        void Akos16Decompress(PixelNavigator dest, int srcPos, int t_width, int t_height, int dir,
                              int numskip_before, int numskip_after, byte transparency, int maskLeft, int maskTop, int zBuf)
        {
            var tmp_buf = _akos16.Buffer;
            var tmp_pos = 0;
            var maskbit = (byte)ScummHelper.RevBitMask(maskLeft & 7);

            if (dir < 0)
            {
                dest.OffsetX(-(t_width - 1));
                tmp_pos += (t_width - 1);
            }

            Akos16SetupBitReader(srcPos);

            if (numskip_before != 0)
            {
                Akos16SkipData(numskip_before);
            }

            var maskptr = _vm.GetMaskBuffer(maskLeft, maskTop, zBuf);

            Debug.Assert(t_height > 0);
            Debug.Assert(t_width > 0);
            while ((t_height--) != 0)
            {
                Akos16DecodeLine(tmp_buf, tmp_pos, t_width, dir);
                BompDrawData.BompApplyMask(_akos16.Buffer, 0, maskptr, maskbit, t_width, transparency);
                BompDrawData.BompApplyShadow(ShadowMode, ShadowTable, _akos16.Buffer, 0, dest, t_width, transparency);

                if (numskip_after != 0)
                {
                    Akos16SkipData(numskip_after);
                }
                dest.OffsetY(1);
                maskptr.OffsetY(1);
            }
        }

        void Akos16SetupBitReader(int srcPos)
        {
            _akos16.RepeatMode = false;
            _akos16.Numbits = 16;
            _akos16.Mask = (byte)((1 << akcd[srcPos]) - 1);
            _akos16.Shift = akcd[srcPos];
            _akos16.Color = akcd[srcPos + 1];
            _akos16.Bits = (ushort)((akcd[srcPos + 2] | akcd[srcPos + 3] << 8));
            _akos16.Dataptr = srcPos + 4;
        }

        void Akos16SkipData(int numbytes)
        {
            Akos16DecodeLine(null, 0, numbytes, 0);
        }

        void Akos16DecodeLine(byte[] buf, int bufPos, int numbytes, int dir)
        {
            ushort bits, tmp_bits;

            while (numbytes != 0)
            {
                if (buf != null)
                {
                    buf[bufPos] = _akos16.Color;
                    bufPos += dir;
                }

                if (!_akos16.RepeatMode)
                {
                    AKOS16_FILL_BITS();
                    bits = (ushort)(_akos16.Bits & 3);
                    if ((bits & 1) != 0)
                    {
                        AKOS16_EAT_BITS(2);
                        if ((bits & 2) != 0)
                        {
                            tmp_bits = (ushort)(_akos16.Bits & 7);
                            AKOS16_EAT_BITS(3);
                            if (tmp_bits != 4)
                            {
                                // A color change
                                _akos16.Color += (byte)(tmp_bits - 4);
                            }
                            else
                            {
                                // Color does not change, but rather identical pixels get repeated
                                _akos16.RepeatMode = true;
                                AKOS16_FILL_BITS();
                                _akos16.RepeatCount = (_akos16.Bits & 0xff) - 1;
                                AKOS16_EAT_BITS(8);
                                AKOS16_FILL_BITS();
                            }
                        }
                        else
                        {
                            AKOS16_FILL_BITS();
                            _akos16.Color = (byte)(_akos16.Bits & _akos16.Mask);
                            AKOS16_EAT_BITS(_akos16.Shift);
                            AKOS16_FILL_BITS();
                        }
                    }
                    else
                    {
                        AKOS16_EAT_BITS(1);
                    }
                }
                else
                {
                    if (--_akos16.RepeatCount == 0)
                    {
                        _akos16.RepeatMode = false;
                    }
                }
                numbytes--;
            }
        }

        void AKOS16_FILL_BITS()
        {
            if (_akos16.Numbits <= 8 && _akos16.Dataptr < akcd.Length)
            {
                _akos16.Bits |= (ushort)(akcd[_akos16.Dataptr++] << _akos16.Numbits);
                _akos16.Numbits += 8;
            }
        }

        void AKOS16_EAT_BITS(byte n)
        {
            _akos16.Numbits -= n;
            _akos16.Bits >>= n;
        }

        byte Codec32(int xmoveCur, int ymoveCur)
        {
            return 0;
        }

        void MarkRectAsDirty(Rect rect)
        {
            rect.Left -= _vm.MainVirtScreen.XStart & 7;
            rect.Right -= _vm.MainVirtScreen.XStart & 7;
            _vm.MarkRectAsDirty(_vm.MainVirtScreen, rect, ActorID);
        }

        void Codec1IgnorePakCols(Codec1 v1, int num)
        {
            num *= _height;

            do
            {
                v1.RepLen = akcd[_srcptr++];
                v1.RepColor = (byte)(v1.RepLen >> v1.Shr);
                v1.RepLen &= v1.Mask;

                if (v1.RepLen == 0)
                    v1.RepLen = akcd[_srcptr++];

                do
                {
                    if (--num == 0)
                        return;
                } while (--v1.RepLen != 0);
            } while (true);
        }

        ScummEngine _vm;
        // Destination
        PixelNavigator _pixelsNavigator;
        // Source pointer
        int _srcptr;
        PixelNavigator startNav;
        // current move offset
        int _xmove, _ymove;
        // whether to draw the actor mirrored
        bool _mirror;
        // width and height of cel to decode
        int _width, _height;
        bool _useBompPalette;
        ushort _codec;
        AkosHeader akhd;
        byte[] akos;
        // header
        byte[] akpl;
        // palette data
        long akci;
        // CostumeInfo table
        byte[] aksq;
        // command sequence
        long akof;
        // offsets into ci and cd table
        byte[] akcd;
        // costume data (contains the data for the codecs)
        // actor _palette
        ushort[] _palette = new ushort[256];

        static readonly byte[] bigCostumeScaleTable =
            {
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
                0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
                0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1,
                0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
                0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9,
                0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
                0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
                0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
                0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED,
                0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
                0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3,
                0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
                0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
                0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
                0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7,
                0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
                0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF,
                0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFE,

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
                0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
                0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1,
                0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
                0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9,
                0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
                0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
                0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
                0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED,
                0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
                0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3,
                0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
                0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
                0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
                0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7,
                0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
                0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF,
                0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFE,

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
                0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
                0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1,
                0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
                0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9,
                0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
                0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
                0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
                0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED,
                0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
                0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3,
                0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
                0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
                0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
                0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7,
                0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
                0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF,
                0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFF,
            };

        class Akos16
        {
            public bool RepeatMode;
            public int RepeatCount;
            public byte Mask;
            public byte Color;
            public byte Shift;
            public ushort Bits;
            public byte Numbits;
            public int Dataptr;
            public byte[] Buffer = new byte[336];
        }

        Akos16 _akos16 = new Akos16();
    }
}

