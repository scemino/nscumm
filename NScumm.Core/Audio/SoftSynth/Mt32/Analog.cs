//
//  Analog.cs
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
    // Methods for emulating the effects of analogue circuits of real hardware units on the output signal.
    enum AnalogOutputMode
    {
        // Only digital path is emulated. The output samples correspond to the digital signal at the DAC entrance.
        DIGITAL_ONLY,
        // Coarse emulation of LPF circuit. High frequencies are boosted, sample rate remains unchanged.
        COARSE,
        // Finer emulation of LPF circuit. Output signal is upsampled to 48 kHz to allow emulation of audible mirror spectra above 16 kHz,
        // which is passed through the LPF circuit without significant attenuation.
        ACCURATE,
        // Same as AnalogOutputMode_ACCURATE mode but the output signal is 2x oversampled, i.e. the output sample rate is 96 kHz.
        // This makes subsequent resampling easier. Besides, due to nonlinear passband of the LPF emulated, it takes fewer number of MACs
        // compared to a regular LPF FIR implementations.
        OVERSAMPLED
    }

    /// <summary>
    /// Analog class is dedicated to perform fair emulation of analogue circuitry of hardware units that is responsible
    /// for processing output signal after the DAC. It appears that the analogue circuit labeled "LPF" on the schematic
    /// also applies audible changes to the signal spectra. There is a significant boost of higher frequencies observed
    /// aside from quite poor attenuation of the mirror spectra above 16 kHz which is due to a relatively low filter order.
    /// 
    /// As the final mixing of multiplexed output signal is performed after the DAC, this function is migrated here from Synth.
    /// Saying precisely, mixing is performed within the LPF as the entrance resistors are actually components of a LPF
    /// designed using the multiple feedback topology. Nevertheless, the schematic separates them.
    /// </summary>
    class Analog
    {
        private const int OUTPUT_GAIN_FRACTION_BITS = 8;
        private const float OUTPUT_GAIN_MULTIPLIER = (1 << OUTPUT_GAIN_FRACTION_BITS);

        // According to the CM-64 PCB schematic, there is a difference in the values of the LPF entrance resistors for the reverb and non-reverb channels.
        // This effectively results in non-unity LPF DC gain for the reverb channel of 0.68 while the LPF has unity DC gain for the LA32 output channels.
        // In emulation, the reverb output gain is multiplied by this factor to compensate for the LPF gain difference.
        const float CM32L_REVERB_TO_LA32_ANALOG_OUTPUT_GAIN_FACTOR = 0.68f;

        private readonly AbstractLowPassFilter leftChannelLPF;
        private readonly AbstractLowPassFilter rightChannelLPF;
        private float synthGain, reverbGain;

        public int OutputSampleRate
        {
            get { return leftChannelLPF.OutputSampleRate; }
        }

        public float SynthOutputGain
        {
            get { return synthGain; }
            set
            {
#if MT32EMU_USE_FLOAT_SAMPLES
                synthGain = value;
#else
                if (OUTPUT_GAIN_MULTIPLIER < value) value = OUTPUT_GAIN_MULTIPLIER;
                synthGain = (SampleEx)(value * OUTPUT_GAIN_MULTIPLIER);
#endif
            }
        }

        public Analog(AnalogOutputMode mode, ControlROMFeatureSet controlROMFeatures)
        {
            leftChannelLPF = AbstractLowPassFilter.CreateLowPassFilter(mode, controlROMFeatures.IsOldMT32AnalogLPF);
            rightChannelLPF = AbstractLowPassFilter.CreateLowPassFilter(mode, controlROMFeatures.IsOldMT32AnalogLPF);
        }

        public void SetReverbOutputGain(float useReverbGain, bool mt32ReverbCompatibilityMode)
        {
            if (!mt32ReverbCompatibilityMode) useReverbGain *= CM32L_REVERB_TO_LA32_ANALOG_OUTPUT_GAIN_FACTOR;
#if MT32EMU_USE_FLOAT_SAMPLES
    reverbGain = useReverbGain;
#else
            if (OUTPUT_GAIN_MULTIPLIER < useReverbGain) useReverbGain = OUTPUT_GAIN_MULTIPLIER;
            reverbGain = (SampleEx)(useReverbGain * OUTPUT_GAIN_MULTIPLIER);
#endif
        }

        public int GetDACStreamsLength(int outputLength)
        {
            return leftChannelLPF.EstimateInSampleCount(outputLength);
        }

        public void Process(Sample[] outStream, int offset, Sample[] nonReverbLeft, Sample[] nonReverbRight, Sample[] reverbDryLeft, Sample[] reverbDryRight, Sample[] reverbWetLeft, Sample[] reverbWetRight, int outLength)
        {
            var o = offset;
            var nrl = 0;
            var nrr = 0;
            var rdl = 0;
            var rdr = 0;
            var rwl = 0;
            var rwr = 0;
            if (outStream == null)
            {
                leftChannelLPF.AddPositionIncrement(outLength);
                rightChannelLPF.AddPositionIncrement(outLength);
                return;
            }

            while (0 < (outLength--))
            {
                SampleEx outSampleL;
                SampleEx outSampleR;

                if (leftChannelLPF.HasNextSample())
                {
                    outSampleL = leftChannelLPF.Process(0);
                    outSampleR = rightChannelLPF.Process(0);
                }
                else {
                    SampleEx inSampleL = (SampleEx)((nonReverbLeft[nrl++] + reverbDryLeft[rdl++]) * synthGain + reverbWetLeft[rwl++] * reverbGain);
                    SampleEx inSampleR = (SampleEx)((nonReverbRight[nrr++] + reverbDryRight[rdr++]) * synthGain + reverbWetRight[rwr++] * reverbGain);

#if !MT32EMU_USE_FLOAT_SAMPLES
                    inSampleL >>= OUTPUT_GAIN_FRACTION_BITS;
                    inSampleR >>= OUTPUT_GAIN_FRACTION_BITS;
#endif

                    outSampleL = leftChannelLPF.Process(inSampleL);
                    outSampleR = rightChannelLPF.Process(inSampleR);
                }

                outStream[o++] = Synth.ClipSampleEx(outSampleL);
                outStream[o++] = Synth.ClipSampleEx(outSampleR);
            }
        }
    }
}
