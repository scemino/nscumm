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

namespace Scumm4
{
    public class AnimPict
    {
        public AnimPict(ushort width, ushort height)
        {
            this.Width = width;
            this.Height = height;
            this.Data = new byte[width * height];
        }

        public byte[] Data { get; private set; }

        public ushort Width { get; private set; }

        public ushort Height { get; private set; }

        public short RelX { get; set; }

        public short RelY { get; set; }

        public short MoveX { get; set; }

        public short MoveY { get; set; }

        public ushort Limb { get; set; }

        public bool Mirror { get; set; }
    }
}
