//
//  Gdi1.cs
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

using NScumm.Core.Graphics;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Graphics
{
    public class Gdi1: Gdi
    {
        // Render settings which are specific to the v0/v1 graphic decoders.
        ImageData1 _v1;
        byte[] objectMap;

        public Gdi1(ScummEngine vm, GameInfo game)
            : base(vm, game)
        {
        }

        protected override void PrepareDrawBitmap(ImageData img, VirtScreen vs,
                                                  Point p, int width, int height,
                                                  int stripnr, int numstrip)
        {
            if (_objectMode)
            {
                var imgData = (ImageData1)img;
                objectMap = imgData.ObjectMap;
            }
        }

        protected override bool DrawStrip(PixelNavigator navDst, int width, int height, int stripnr, System.IO.BinaryReader smapReader)
        {
            if (_objectMode)
            {
                DrawStripV1Object(navDst, stripnr, width, height);
            }
            else
            {
                DrawStripV1Background(navDst, stripnr, height);
            }

            return false;
        }

        public override void RoomChanged(Room room)
        {
            _v1 = room.Image as ImageData1;
            _objectMode = true;
        }

        protected override void DecodeMask(int x, int y, int width, int height, int stripnr, System.Collections.Generic.IList<ZPlane> zPlanes, bool transpStrip, DrawBitmaps flags)
        {
            var mask_ptr = GetMaskBuffer(x, y, 1);
            DrawStripV1Mask(mask_ptr, stripnr, width, height);
        }

        void DrawStripV1Background(PixelNavigator navDst, int stripnr, int height)
        {
            int charIdx;
            height /= 8;
            for (int y = 0; y < height; y++)
            {
                _v1.Colors[3] = (byte)(_v1.ColorMap[y + stripnr * height] & 7);
                // Check for room color change in V1 zak
                if (RoomPalette[0] == 255)
                {
                    _v1.Colors[2] = RoomPalette[2];
                    _v1.Colors[1] = RoomPalette[1];
                }

                charIdx = _v1.PicMap[y + stripnr * height] * 8;
                for (int i = 0; i < 8; i++)
                {
                    byte c = _v1.CharMap[charIdx + i];
                    var color = _v1.Colors[(c >> 6) & 3];
                    navDst.Write(0, color);
                    navDst.Write(1, color);
                    color = _v1.Colors[(c >> 4) & 3];
                    navDst.Write(2, color);
                    navDst.Write(3, color);
                    color = _v1.Colors[(c >> 2) & 3];
                    navDst.Write(4, color);
                    navDst.Write(5, color);
                    color = _v1.Colors[(c >> 0) & 3];
                    navDst.Write(6, color);
                    navDst.Write(7, color);
                    navDst.OffsetY(1);
                }
            }
        }

        void DrawStripV1Object(PixelNavigator navDst, int stripnr, int width, int height)
        {
            int charIdx;
            height /= 8;
            width /= 8;
            for (var y = 0; y < height; y++)
            {
                _v1.Colors[3] = (byte)(objectMap[(y + height) * width + stripnr] & 7);
                charIdx = objectMap[y * width + stripnr] * 8;
                for (var i = 0; i < 8; i++)
                {
                    byte c = _v1.CharMap[charIdx + i];
                    var color = _v1.Colors[(c >> 6) & 3];
                    navDst.Write(0, color);
                    navDst.Write(1, color);
                    color = _v1.Colors[(c >> 4) & 3];
                    navDst.Write(2, color);
                    navDst.Write(3, color);
                    color = _v1.Colors[(c >> 2) & 3];
                    navDst.Write(4, color);
                    navDst.Write(5, color);
                    color = _v1.Colors[(c >> 0) & 3];
                    navDst.Write(6, color);
                    navDst.Write(7, color);
                    navDst.OffsetY(1);
                }
            }
        }

        void DrawStripV1Mask(PixelNavigator navDst, int stripnr, int width, int height)
        {
            int maskIdx;
            height /= 8;
            width /= 8;
            for (var y = 0; y < height; y++)
            {
                if (_objectMode)
                    maskIdx = objectMap[(y + 2 * height) * width + stripnr] * 8;
                else
                    maskIdx = _v1.MaskMap[y + stripnr * height] * 8;
                for (var i = 0; i < 8; i++)
                {
                    byte c = _v1.MaskChar[maskIdx + i];

                    // V1/V0 masks are inverted compared to what ScummVM expects
                    navDst.Write((byte)(c ^ 0xFF));
                    navDst.OffsetY(1);
                }
            }
        }
    }
}

