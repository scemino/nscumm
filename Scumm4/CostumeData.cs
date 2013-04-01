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
    public class CostumeData
    {
        public byte[] active;
        public ushort animCounter;
        public byte soundCounter;
        public byte soundPos;
        public ushort stopped;
        public ushort[] curpos;
        public ushort[] start;
        public ushort[] end;
        public ushort[] frame;

        public ushort current;

        public CostumeData()
        {
            active = new byte[16];
            curpos = new ushort[16];
            start = new ushort[16];
            end = new ushort[16];
            frame = new ushort[16];
        }

        public void Reset()
        {
            current = 0;
            stopped = 0;
            for (int i = 0; i < 16; i++)
            {
                active[i] = 0;
                curpos[i] = start[i] = end[i] = frame[i] = 0xFFFF;
            }
        }
    }
}
