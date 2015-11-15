//
//  BaseCostumeRenderer.cs
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

namespace NScumm.Scumm.Graphics
{
    abstract class BaseCostumeRenderer : ICostumeRenderer
    {
        public byte ActorID { get; set; }

        public byte ShadowMode { get; set; }

        public byte[] ShadowTable { get; set; }

        public int ActorX { get; set; }

        public int ActorY { get; set; }

        public byte ZBuffer { get; set; }

        public byte ScaleX { get; set; }

        public byte ScaleY { get; set; }

        public int DrawTop { get; set; }

        public int DrawBottom { get; set; }

        protected BaseCostumeRenderer(ScummEngine scumm)
        {
            _vm = scumm;
        }

        public abstract void SetPalette(ushort[] palette);

        public abstract void SetFacing(Actor a);

        public abstract void SetCostume(int costume, int shadow);

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

        protected abstract byte DrawLimb(Actor a, int limb);

        protected ScummEngine _vm;

        // current move offset
        protected int _xmove, _ymove;

        // width and height of cel to decode
        protected PixelNavigator startNav;
        protected int _w;
        protected int _h;
    }
    
}
