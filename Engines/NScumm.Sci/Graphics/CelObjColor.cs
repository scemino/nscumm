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
//  MERCHANTABILITY or FITNESS FOßR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#if ENABLE_SCI32

using NScumm.Core;
using NScumm.Core.Graphics;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// A CelObjColor is the drawing primitive for fast,
    /// low-memory, flat color fills.
    /// </summary>
    internal class CelObjColor : CelObj
    {
        public CelObjColor(byte color, short width, short height)
        {
            _info.type = CelType.Color;
            _info.color = color;
            _origin.X = 0;
            _origin.Y = 0;
            _xResolution = SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptWidth;
            _yResolution = SciEngine.Instance._gfxFrameout.CurrentBuffer.ScriptHeight;
            _hunkPaletteOffset = 0;
            _mirrorX = false;
            _remap = false;
            _width = (ushort) width;
            _height = (ushort) height;
        }

        public void Draw(Buffer target, ScreenItem screenItem, Rect targetRect, bool mirrorX)
        {
            // TODO: The original engine sets this flag but why? One cannot
            // draw a solid color mirrored.
            _drawMirrored = mirrorX;
            Draw(target, targetRect);
        }

        public void Draw(Buffer target, Rect targetRect, Point scaledPosition, bool mirrorX)
        {
            Error("Unsupported method");
        }

        public void Draw(Buffer target, Rect targetRect)
        {
            target.FillRect(targetRect, _info.color);
        }

        public override CelObj Duplicate()
        {
            return new CelObjColor(_info.color, (short) _width, (short) _height);
        }

        public override BytePtr GetResPointer()
        {
            Error("Unsupported method");
            return BytePtr.Null;
        }
    }
}

#endif