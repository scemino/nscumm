//
//  CostumeRendere0.cs
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
using NScumm.Core;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Graphics
{
    class CostumeRenderer0: BaseCostumeRenderer
    {
        CostumeLoader0 _loaded;

        public CostumeRenderer0(ScummEngine vm)
            : base(vm)
        {
            _loaded = new CostumeLoader0(vm);
        }

        public override void SetPalette(ushort[] palette)
        {
        }

        public override void SetFacing(Actor a)
        {
        }

        public override void SetCostume(int costume, int shadow)
        {
            _loaded.LoadCostume(costume);
        }

        protected override byte DrawLimb(Actor a, int limb)
        {
            var a0 = (Actor0)a;

            if (limb >= 8)
                return 0;

            if (limb == 0)
            {
                DrawTop = 200;
                DrawBottom = 0;
            }

            // Invalid current position?
            if (a.Cost.Curpos[limb] == 0xFFFF)
                return 0;

            _loaded.LoadCostume(a.Costume);
            byte frame = _loaded.Data[_loaded.FrameOffsets + a.Cost.Curpos[limb] + a.Cost.Active[limb]];

            // Get the frame ptr
            byte ptrLow = _loaded.Data[9 + frame];
            byte ptrHigh = (byte)(ptrLow + _loaded.Data[4]);
            int frameOffset = (_loaded.Data[9 + ptrHigh] << 8) + _loaded.Data[9 + ptrLow + 2];          // 0x23EF / 0x2400

            var data = 9 + frameOffset;

            // Set up the palette data
            var palette = new byte[4];
            if (_vm.GetCurrentLights().HasFlag(LightModes.ActorUseColors))
            {
                palette[1] = 10;
                palette[2] = CostumeLoader0.actorV0Colors[ActorID];
            }
            else
            {
                palette[2] = 11;
                palette[3] = 11;
            }

            int width = _loaded.Data[data];
            int height = _loaded.Data[data + 1];
            int offsetX = _xmove + _loaded.Data[data + 2];
            int offsetY = _ymove + _loaded.Data[data + 3];
            _xmove += (sbyte)_loaded.Data[data + 4];
            _ymove += (sbyte)_loaded.Data[data + 5];
            data += 6;

            if (width == 0 || height == 0)
                return 0;

            int xpos = (int)(ActorX + (a0.LimbFlipped[limb] ? -1 : +1) * (offsetX * 8 - a.Width / 2));
            // +1 as we appear to be 1 pixel away from the original interpreter
            int ypos = ActorY - offsetY + 1;

            var dst = new PixelNavigator(startNav);
            // This code is very similar to procC64()
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    byte color = _loaded.Data[data + y * width + x];

                    int destX = xpos + (a0.LimbFlipped[limb] ? -(x + 1) : x) * 8;
                    int destY = ypos + y;

                    if (destY >= 0 && destY < _h && destX >= 0 && destX < _w)
                    {
                        dst.GoTo(destX, destY);
                        var mask = _vm.GetMaskBuffer(0, destY, ZBuffer);
                        if (a0.LimbFlipped[limb])
                        {
                            LINE(0, 0, color, palette, dst, mask, destX);
                            LINE(2, 2, color, palette, dst, mask, destX);
                            LINE(4, 4, color, palette, dst, mask, destX);
                            LINE(6, 6, color, palette, dst, mask, destX);
                        }
                        else
                        {
                            LINE(6, 0, color, palette, dst, mask, destX);
                            LINE(4, 2, color, palette, dst, mask, destX);
                            LINE(2, 4, color, palette, dst, mask, destX);
                            LINE(0, 6, color, palette, dst, mask, destX);
                        }
                    }
                }
            }

            DrawTop = Math.Min(DrawTop, ypos);
            DrawBottom = Math.Max(DrawBottom, ypos + height);
            if (a0.LimbFlipped[limb])
                _vm.MarkRectAsDirty(_vm.MainVirtScreen, xpos - (width * 8), xpos, ypos, ypos + height, ActorID);
            else
                _vm.MarkRectAsDirty(_vm.MainVirtScreen, xpos, xpos + (width * 8), ypos, ypos + height, ActorID);
            return 0;
        }

        bool MASK_AT(int xoff, PixelNavigator mask, int destX)
        {
            mask.OffsetX((destX + xoff) / 8);
            return (mask.Read() & ScummHelper.RevBitMask((destX + xoff) & 7)) != 0;
        }

        void LINE(int c, int p, byte color, byte[] palette, PixelNavigator dst, PixelNavigator mask, int destX)
        {
            var pcolor = (color >> c) & 3;
            if (pcolor != 0)
            { 
                if (!MASK_AT(p, mask, destX))
                    dst.Write(p, palette[pcolor]); 
                if (!MASK_AT(p + 1, mask, destX))
                    dst.Write(p + 1, palette[pcolor]); 
            }
        }
    }
}

