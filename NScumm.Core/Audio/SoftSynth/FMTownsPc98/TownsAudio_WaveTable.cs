//
//  TownsAudioInterface.cs
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

namespace NScumm.Core.Audio.SoftSynth
{
    class TownsAudio_WaveTable
    {
        public void Clear()
        {
            name = string.Empty;
            id = -1;
            size = 0;
            loopStart = 0;
            loopLen = 0;
            rate = 0;
            rateOffs = 0;
            baseNote = 0;
            data = null;
        }

        public void ReadHeader(byte[] buffer, int offset)
        {
            name = System.Text.Encoding.UTF8.GetString(buffer, offset, 8);
            id = buffer.ToInt32(offset + 8);
            size = buffer.ToInt32(offset + 12);
            loopStart = buffer.ToUInt32(offset + 16);
            loopLen = buffer.ToUInt32(offset + 20);
            rate = buffer.ToUInt16(offset + 24);
            rateOffs = buffer.ToUInt16(offset + 26);
            baseNote = (ushort)buffer.ToUInt32(offset + 28);
        }

        public void ReadData(byte[] buffer, int offset)
        {
            if (size == 0)
                return;

            data = new sbyte[size];

            for (var i = 0; i < size; i++)
                data[i] = ((buffer[offset + i] & 0x80) != 0) ? (sbyte)(buffer[offset + i] & 0x7f) : (sbyte)-buffer[offset + i];
        }

        public string name;
        public int id;
        public int size;
        public uint loopStart;
        public uint loopLen;
        public ushort rate;
        public ushort rateOffs;
        public ushort baseNote;
        public sbyte[] data;
    }
}

