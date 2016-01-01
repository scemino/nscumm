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

using NScumm.Sci.Engine;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// Controls class, handles drawing of controls in SCI16 (SCI0-SCI1.1) games
    /// </summary>
    internal class GfxControls16
    {
        private GfxPaint16 _paint16;
        private GfxPorts _ports;
        private GfxScreen _screen;
        private GfxText16 _text16;
        private SegManager _segMan;

        // Textedit-Control related
        private Core.Graphics.Rect _texteditCursorRect;
        private bool _texteditCursorVisible;
        private uint _texteditBlinkTime;

        public GfxControls16(SegManager segMan, GfxPorts ports, GfxPaint16 paint16, GfxText16 text16, GfxScreen screen)
        {
            _segMan = segMan;
            _ports = ports;
            _paint16 = paint16;
            _text16 = text16;
            _screen = screen;

            _texteditBlinkTime = 0;
            _texteditCursorVisible = false;
        }
    }
}
