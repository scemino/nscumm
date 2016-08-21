//
//  RingBuffer.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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

#if MT32EMU_USE_FLOAT_SAMPLES
using Sample = System.Single;
using SampleEx = System.Single;
#else
using Sample = System.Int16;
using SampleEx = System.Int32;
#endif

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    class RingBuffer
    {
        protected Sample[] buffer;
        protected int index;

        public RingBuffer(int size)
        {
            buffer = new Sample[size];
        }

        public Sample Next()
        {
            if (++index >= buffer.Length)
            {
                index = 0;
            }
            return buffer[index];
        }

        public bool IsEmpty
        {
            get
            {
#if MT32EMU_USE_FLOAT_SAMPLES
    Sample max = 0.001f;
#else
    Sample max = 8;
#endif

                var buf = 0;
                for (var i = 0; i < buffer.Length; i++)
                {
                    if (buffer[buf] < -max || buffer[buf] > max) return false;
                    buf++;
                }
                return true;
            }
        }

        public void Mute()
        {
            Synth.MuteSampleBuffer(buffer, 0, buffer.Length);
        }
    }

    
}
