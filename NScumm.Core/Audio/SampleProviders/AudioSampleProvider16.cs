//
//  AudioSampleProvider16.cs
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


using System;

namespace NScumm.Core.Audio.SampleProviders
{
    public abstract class AudioSampleProvider16 : IAudioSampleProvider
    {
        private short[] _buffer;

        public abstract AudioFormat AudioFormat
        {
            get;
        }

        protected AudioSampleProvider16()
        {
            _buffer = new short[4096];
        }

        public abstract int Read(short[] samples, int count);

        public int Read(byte[] samples, int count)
        {
            if (_buffer.Length < count/2)
            {
                _buffer = new short[count / 2];
            }
            Array.Clear(_buffer, 0, _buffer.Length);
            var numRead = Read(_buffer, count / 2);
            var offs = 0;
            for (int i = 0; i < numRead; i++)
            {
                samples[offs++] = (byte)(_buffer[i] & 0xFF);
                samples[offs++] = (byte)((_buffer[i] >> 8) & 0xFF);
            }
            return numRead * 2;
        }
    }
}
