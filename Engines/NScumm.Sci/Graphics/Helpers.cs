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
using NScumm.Sci.Engine;
using System;

namespace NScumm.Sci.Graphics
{
    public struct Color : IEquatable<Color>
    {
        public byte used;
        public byte r, g, b;

        public override int GetHashCode()
        {
            return r ^ g ^ b;
        }

        public override bool Equals(object obj)
        {
            return obj is Color && Equals((Color)obj);
        }

        public bool Equals(Color other)
        {
            return other.r == r && other.g == g && other.b == b;
        }

        public static bool operator ==(Color c1, Color c2)
        {
            return c1.Equals(c2);
        }

        public static bool operator !=(Color c1, Color c2)
        {
            return !(c1 == c2);
        }
    }

    internal class Palette
    {
        public byte[] mapping = new byte[256];
        public int timestamp;
        public Color[] colors = new Color[256];
        public byte[] intensity = new byte[256];

        public Palette()
        {
        }

        public Palette(Palette copy)
        {
            Array.Copy(copy.mapping, mapping, mapping.Length);
            timestamp = copy.timestamp;
            Array.Copy(copy.colors, colors, colors.Length);
            Array.Copy(copy.intensity, intensity, intensity.Length);
        }
    }

    internal class Port
    {
        private const int PORTS_FIRSTWINDOWID = 2;

        public ushort id;
        public short top, left;
        public Rect rect;
        public short curTop, curLeft;
        public short fontHeight;
        public int fontId;
        public bool greyedOutput;
        public short penClr, backClr;
        public short penMode;
        public ushort counterTillFree;

        public Port(ushort theId)
        {
            id = theId;
        }

        public bool IsWindow { get { return id >= PORTS_FIRSTWINDOWID && id != 0xFFFF; } }
    }

    internal class Window : Port
    {
        public Rect dims; // client area of window
        public Rect restoreRect; // total area of window including borders
        public WindowManagerStyle wndStyle;
        public GfxScreenMasks saveScreenMask;
        public Register hSaved1;
        public Register hSaved2;
        public string title;
        public bool bDrawn;

        public Window(ushort theId) : base(theId)
        {
        }
    }
}
