//
//  CombFilter.cs
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
using System;
using Sample = System.Int16;
using SampleEx = System.Int32;
#endif

namespace NScumm.Core.Audio.SoftSynth.Mt32
{

    class CombFilter : RingBuffer
    {
        protected readonly uint filterFactor;
        protected uint feedbackFactor;

        public CombFilter(int size, uint useFilterFactor)
            : base(size)
        {
            filterFactor = useFilterFactor;
        }

        public virtual void Process(Sample @in)
        {
            // This model corresponds to the comb filter implementation of the real CM-32L device

            // the previously stored value
            Sample last = buffer[index];

            // prepare input + feedback
            Sample filterIn = (Sample)(@in + WeirdMul(Next(), (byte)feedbackFactor, 0xF0));

            // store input + feedback processed by a low-pass filter
            buffer[index] = (Sample)(WeirdMul(last, (byte)filterFactor, 0xC0) - filterIn);
        }

        public Sample GetOutputAt(int outIndex)
        {
            return buffer[(buffer.Length + index - outIndex) % buffer.Length];
        }

        // This algorithm tries to emulate exactly Boss multiplication operation (at least this is what we see on reverb RAM data lines).
        // Also LA32 is suspected to use the similar one to perform PCM interpolation and ring modulation.
        internal static Sample WeirdMul(Sample a, byte addMask, byte carryMask)
        {
#if MT32EMU_USE_FLOAT_SAMPLES
            return a * addMask / 256.0f;
#elif MT32EMU_BOSS_REVERB_PRECISE_MODE
    byte mask = 0x80;
    int res = 0;
    for (int i = 0; i < 8; i++) {
        int carry = (a < 0) && (mask & carryMask) > 0 ? a & 1 : 0;
        a >>= 1;
        res += (mask & addMask) > 0 ? a + carry : 0;
        mask >>= 1;
    }
    return res;
#else
            return (Sample)((a * addMask) >> 8);
#endif

        }

        public void SetFeedbackFactor(uint useFeedbackFactor)
        {
            feedbackFactor = useFeedbackFactor;
        }
    }

    class DelayWithLowPassFilter : CombFilter
    {
        uint amp;

        public DelayWithLowPassFilter(int useSize, uint useFilterFactor, uint useAmp)
            : base(useSize, useFilterFactor)
        {
            amp = useAmp;
        }

        public override void Process(Sample @in)
        {
            // the previously stored value
            Sample last = buffer[index];

            // move to the next index
            Next();

            // low-pass filter process
            Sample lpfOut = (Sample)(WeirdMul(last, (byte)filterFactor, 0xFF) + @in);

            // store lpfOut multiplied by LPF amp factor
            buffer[index] = WeirdMul(lpfOut, (byte)amp, 0xFF);
        }

    }

    class TapDelayCombFilter : CombFilter
    {
        int outL;
        int outR;

        public TapDelayCombFilter(int useSize, uint useFilterFactor)
            : base(useSize, useFilterFactor)
        {
        }

        public override void Process(Sample @in)
        {
            // the previously stored value
            Sample last = buffer[index];

            // move to the next index
            Next();

            // prepare input + feedback
            // Actually, the size of the filter varies with the TIME parameter, the feedback sample is taken from the position just below the right output
            Sample filterIn = (Sample)(@in + WeirdMul(GetOutputAt(outR + Mt32Emu.MODE_3_FEEDBACK_DELAY), (byte)feedbackFactor, 0xF0));

            // store input + feedback processed by a low-pass filter
            buffer[index] = (Sample)(WeirdMul(last, (byte)filterFactor, 0xF0) - filterIn);
        }

        public void SetOutputPositions(int useOutL, int useOutR)
        {
            outL = useOutL;
            outR = useOutR;
        }

        public Sample GetLeftOutput()
        {
            return GetOutputAt(outL + Mt32Emu.PROCESS_DELAY + Mt32Emu.MODE_3_ADDITIONAL_DELAY);
        }

        public Sample GetRightOutput()
        {
            return GetOutputAt(outR + Mt32Emu.PROCESS_DELAY + Mt32Emu.MODE_3_ADDITIONAL_DELAY);
        }

    }
}
