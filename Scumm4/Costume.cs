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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class Costume
    {
        private CostumeAnimation[] cAnims;
        private byte room;
        private byte[] palette;

        public byte[] Palette
        {
            get { return palette; }
        }

        public byte Room
        {
            get { return room; }
        }

        public CostumeAnimation[] Animations
        {
            get { return cAnims; }
        }

        public Costume(byte room, byte[] palette, CostumeAnimation[] cAnims)
        {
            this.room = room;
            this.palette = palette;
            this.cAnims = cAnims;
        }
    }
}
