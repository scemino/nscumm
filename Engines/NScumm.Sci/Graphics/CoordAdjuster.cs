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
using NScumm.Core;

namespace NScumm.Sci.Graphics
{
    /// <summary>
    /// CoordAdjuster class, does coordinate adjustment as need by various functions
    ///  most of the time sci32 doesn't do any coordinate adjustment at all
    ///  sci16 does a lot of port adjustment on given coordinates
    /// </summary>
    internal class GfxCoordAdjuster
    {
        public virtual Rect OnControl(Rect rect)
        {
            return rect;
        }

        public virtual void KernelLocalToGlobal(ref short x, ref short y, Register? planeObject = null)
        {
        }

        public virtual void KernelGlobalToLocal(ref short x, ref short y, Register? planeObject = null)
        {
        }

        public virtual void SetCursorPos(ref Point pos)
        {
        }

        public virtual void MoveCursor(ref Point pos)
        {
        }

        public virtual Rect PictureGetDisplayArea() { return new Rect(0, 0); }
    }

    internal class GfxCoordAdjuster16 : GfxCoordAdjuster
    {
        private GfxPorts _ports;

        public GfxCoordAdjuster16(GfxPorts ports)
        {
            _ports = ports;
        }

        public override void KernelLocalToGlobal(ref short x, ref short y, Register? planeObject = null)
        {
            Port curPort = _ports.Port;
            x += curPort.left;
            y += curPort.top;
        }

        public override void KernelGlobalToLocal(ref short x, ref short y, Register? planeObject = null)
        {
            Port curPort = _ports.Port;
            x -= curPort.left;
            y -= curPort.top;
        }

        public override Rect OnControl(Rect rect)
        {
            Port oldPort = _ports.SetPort(_ports._picWind);
            Rect adjustedRect = new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);

            adjustedRect.Clip(_ports.Port.rect);
            _ports.OffsetRect(ref adjustedRect);
            _ports.SetPort(oldPort);
            return adjustedRect;
        }

        public override void SetCursorPos(ref Point pos)
        {
            pos.Y += _ports.Port.top;
            pos.X += _ports.Port.left;
        }

        public override void MoveCursor(ref Point pos)
        {
            pos.Y += _ports._picWind.rect.Top;
            pos.X += _ports._picWind.rect.Left;

            pos.Y = ScummHelper.Clip(pos.Y, _ports._picWind.rect.Top, _ports._picWind.rect.Bottom - 1);
            pos.X = ScummHelper.Clip(pos.X, _ports._picWind.rect.Left, _ports._picWind.rect.Right - 1);
        }

        public override Rect PictureGetDisplayArea()
        {
            var displayArea = new Rect(_ports.Port.rect.Right, _ports.Port.rect.Bottom);
            displayArea.MoveTo(_ports.Port.left, _ports.Port.top);
            return displayArea;
        }
    }
}
