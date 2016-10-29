//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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

#if ENABLE_SCI32
using System;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Sci.Engine;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    internal enum LineStyle
    {
        Solid,
        Dashed,
        Pattern
    }

    /// <summary>
    /// Paint32 class, handles painting/drawing for SCI32 (SCI2+) games
    /// </summary>
    internal class GfxPaint32
    {
        private const int DefaultSkipColor = 250;

        class LineProperties
        {
            public SciBitmap bitmap;
            public bool[] pattern = new bool[16];
            public byte patternIndex;
            public bool solid;
            public bool horizontal;
            public int lastAddress;
        }

        private SegManager _segMan;

        public GfxPaint32(SegManager segMan)
        {
            _segMan = segMan;
        }

        public Register KernelAddLine(Register planeObject, Point startPoint, Point endPoint, short priority,
            byte color, LineStyle style, ushort pattern, byte thickness)
        {
            Plane plane = SciEngine.Instance._gfxFrameout.GetPlanes().FindByObject(planeObject);
            if (plane == null)
            {
                Error("kAddLine: Plane {0} not found", planeObject);
            }

            Rect gameRect = new Rect();
            Register bitmapId = MakeLineBitmap(startPoint, endPoint, priority, color, style, pattern, thickness,
                gameRect);

            CelInfo32 celInfo = new CelInfo32();
            celInfo.type = CelType.Mem;
            celInfo.bitmap = bitmapId;
            // SSCI stores the line color on `celInfo`, even though
            // this is not a `kCelTypeColor`, as a hack so that
            // `kUpdateLine` can get the originally used color
            celInfo.color = color;

            ScreenItem screenItem = new ScreenItem(planeObject, celInfo, gameRect);
            screenItem._priority = priority;
            screenItem._fixedPriority = true;

            plane._screenItemList.Add(screenItem);

            return screenItem._object;
        }

        public void KernelUpdateLine(ScreenItem screenItem, Plane plane, Point startPoint, Point endPoint,
            short priority, byte color, LineStyle style, ushort pattern, byte thickness)
        {
            throw new System.NotImplementedException();
        }

        public void KernelDeleteLine(Register screenItemObject, Register planeObject)
        {
            throw new System.NotImplementedException();
        }

        private void Plotter(int x, int y, int color, object data)
        {
            LineProperties properties = (LineProperties) data;
            BytePtr pixels = properties.bitmap.Pixels;

            ushort bitmapWidth = properties.bitmap.Width;
            ushort bitmapHeight = properties.bitmap.Height;
            int index = bitmapWidth * y + x;

            // Only draw the points in the bitmap, and ignore the rest. SSCI scripts
            // can draw lines ending outside the visible area (e.g. negative coordinates)
            if (x >= 0 && x < bitmapWidth && y >= 0 && y < bitmapHeight)
            {
                if (properties.solid)
                {
                    pixels[index] = (byte) color;
                    return;
                }

                if (properties.horizontal && x != properties.lastAddress)
                {
                    properties.lastAddress = x;
                    ++properties.patternIndex;
                }
                else if (!properties.horizontal && y != properties.lastAddress)
                {
                    properties.lastAddress = y;
                    ++properties.patternIndex;
                }

                if (properties.pattern[properties.patternIndex])
                {
                    pixels[index] = (byte) color;
                }

                if (properties.patternIndex == properties.pattern.Length)
                {
                    properties.patternIndex = 0;
                }
            }
        }


        private Register MakeLineBitmap(Point startPoint, Point endPoint, short priority, byte color, LineStyle style,
            ushort pattern, byte thickness, Rect outRect)
        {
            byte skipColor = (byte) (color != DefaultSkipColor ? DefaultSkipColor : 0);

            // Line thickness is expected to be 2 * thickness + 1
            thickness = (byte) ((Math.Max((byte) 1, thickness) - 1) | 1);
            byte halfThickness = (byte) (thickness >> 1);

            ushort scriptWidth = SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            ushort scriptHeight = SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;

            outRect.Left = Math.Min(startPoint.X, endPoint.X);
            outRect.Top = Math.Min(startPoint.Y, endPoint.Y);
            outRect.Right = (short) (Math.Max(startPoint.X, endPoint.X) + 1 + 1); // rect lower edge + thickness offset
            outRect.Bottom = (short) (Math.Max(startPoint.Y, endPoint.Y) + 1 + 1); // rect lower edge + thickness offset

            outRect.Grow(halfThickness);
            outRect.Clip(new Rect((short) scriptWidth, (short) scriptHeight));

            Register bitmapId = new Register();
            SciBitmap bitmap = _segMan.AllocateBitmap(out bitmapId, outRect.Width, outRect.Height, skipColor,
                0, 0, (short) scriptWidth, (short) scriptHeight, 0, false, true);

            BytePtr pixels = bitmap.Pixels;
            pixels.Data.Set(pixels.Offset, skipColor, bitmap.Width * bitmap.Height);

            LineProperties properties = new LineProperties {bitmap = bitmap};

            switch (style)
            {
                case LineStyle.Solid:
                    pattern = 0xFFFF;
                    properties.solid = true;
                    break;
                case LineStyle.Dashed:
                    pattern = 0xFF00;
                    properties.solid = false;
                    break;
                case LineStyle.Pattern:
                    properties.solid = pattern == 0xFFFF;
                    break;
            }

            // Change coordinates to be relative to the bitmap
            short x1 = (short) (startPoint.X - outRect.Left);
            short y1 = (short) (startPoint.Y - outRect.Top);
            short x2 = (short) (endPoint.X - outRect.Left);
            short y2 = (short) (endPoint.Y - outRect.Top);

            if (!properties.solid)
            {
                for (int i = 0; i < properties.pattern.Length; ++i)
                {
                    properties.pattern[i] = (pattern & 0x8000) != 0;
                    pattern <<= 1;
                }

                properties.patternIndex = 0;
                properties.horizontal = Math.Abs(x2 - x1) > Math.Abs(y2 - y1);
                properties.lastAddress = properties.horizontal ? x1 : y1;
            }

            if (thickness <= 1)
            {
                Core.Graphics.Graphics.DrawLine(x1, y1, x2, y2, color, Plotter, properties);
            }
            else
            {
                Core.Graphics.Graphics.DrawThickLine2(x1, y1, x2, y2, thickness, color, Plotter, properties);
            }

            return bitmapId;
        }
    }
}

#endif