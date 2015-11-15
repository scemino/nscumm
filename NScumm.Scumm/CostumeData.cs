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

namespace NScumm.Scumm
{
    public class CostumeData
    {
        public byte[] Active;
        public ushort AnimCounter;
        public byte SoundCounter;
        public byte SoundPos;
        public ushort Stopped;
        public ushort[] Curpos;
        public ushort[] Start;
        public ushort[] End;
        public ushort[] Frame;

        public ushort Current;

        public CostumeData()
        {
            Active = new byte[16];
            Curpos = new ushort[16];
            Start = new ushort[16];
            End = new ushort[16];
            Frame = new ushort[16];
        }

        public void Reset()
        {
            Current = 0;
            Stopped = 0;
            for (int i = 0; i < 16; i++)
            {
                Active[i] = 0;
                Curpos[i] = Start[i] = End[i] = Frame[i] = 0xFFFF;
            }
        }
    }
}
