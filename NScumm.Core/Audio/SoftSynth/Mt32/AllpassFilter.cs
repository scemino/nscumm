//
//  AllpassFilter.cs
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
    class AllpassFilter : RingBuffer
    {
        public AllpassFilter(int size)
            : base(size)
        {
        }

        public Sample Process(Sample @in)
        {
            // This model corresponds to the allpass filter implementation of the real CM-32L device
            // found from sample analysis

            Sample bufferOut = Next();

#if MT32EMU_USE_FLOAT_SAMPLES
            // store input - feedback / 2
            buffer[index] = @in -0.5f * bufferOut;

            // return buffer output + feedforward / 2
            return bufferOut + 0.5f * buffer[index];
#else
            // store input - feedback / 2
            buffer[index] = (Sample)(@in - (bufferOut >> 1));

            // return buffer output + feedforward / 2
            return (Sample)(bufferOut + (buffer[index] >> 1));
#endif
        }
    }
}
