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

namespace NScumm.Scumm.Graphics
{
    interface ICostumeRenderer
    {
        int DrawTop { get; set; }

        int DrawBottom { get; set; }

        byte ActorID { get; set; }

        byte ShadowMode { get; set; }

        byte[] ShadowTable { get; set; }

        int ActorX { get; set; }

        int ActorY { get; set; }

        byte ZBuffer { get; set; }

        byte ScaleX { get; set; }

        byte ScaleY { get; set; }

        void SetPalette(ushort[] palette);

        void SetFacing(Actor a);

        void SetCostume(int costume, int shadow);

        int DrawCostume(VirtScreen vs, int numStrips, Actor actor, bool drawToBackBuf);
    }
}
