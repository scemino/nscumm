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
    public class CharInfo
    {
        public CharInfo(byte widht, byte height)
        {
            this.Width = widht;
            this.Height = height;
            this.Stride = ((Width + 7) / 8) * 8;
            this.Pixels = new byte[this.Stride * this.Height];
        }

        public byte[] Pixels { get; private set; }

        public byte Width { get; private set; }

        public byte Height { get; private set; }

        public sbyte X { get; set; }

        public sbyte Y { get; set; }

        public int Stride { get; private set; }
    }
}
