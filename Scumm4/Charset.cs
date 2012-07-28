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
    public class Charset
    {
        public byte Height { get; set; }

        public byte Bpp { get; set; }

        public byte[] ColorMap { get; private set; }

        public Dictionary<byte, CharInfo> Characters { get; private set; }

        public Charset()
        {
            this.ColorMap = new byte[16];
            this.Characters = new Dictionary<byte, CharInfo>();
        }

    }
}
