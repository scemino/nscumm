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

using System;

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    class NullLowPassFilter : AbstractLowPassFilter
    {
        public override float Process(float sample)
        {
            return sample;
        }
    }

    class CoarseLowPassFilter : AbstractLowPassFilter
    {
        const int COARSE_LPF_DELAY_LINE_LENGTH = 8; // Must be a power of 2
        const int DELAY_LINE_MASK = COARSE_LPF_DELAY_LINE_LENGTH - 1;

        private float[] LPF_TAPS;
        private float[] ringBuffer = new float[COARSE_LPF_DELAY_LINE_LENGTH];
        private uint ringBufferPosition;


        public CoarseLowPassFilter(bool oldMT32AnalogLPF)
        {
            LPF_TAPS = oldMT32AnalogLPF ? COARSE_LPF_TAPS_MT32 : COARSE_LPF_TAPS_CM32L;
            MuteRingBuffer(ringBuffer, COARSE_LPF_DELAY_LINE_LENGTH);
        }

        public override float Process(float inSample)
        {
            float sample = LPF_TAPS[COARSE_LPF_DELAY_LINE_LENGTH] * ringBuffer[ringBufferPosition];
            ringBuffer[ringBufferPosition] = Synth.ClipSampleEx(inSample);

            for (int i = 0; i < COARSE_LPF_DELAY_LINE_LENGTH; i++)
            {
                sample += LPF_TAPS[i] * ringBuffer[(i + ringBufferPosition) & DELAY_LINE_MASK];
            }

            ringBufferPosition = (ringBufferPosition - 1) & DELAY_LINE_MASK;

            return sample;
        }
    }

    class AccurateLowPassFilter : AbstractLowPassFilter
    {
        const int DELAY_LINE_MASK = ACCURATE_LPF_DELAY_LINE_LENGTH - 1;

        const int COARSE_LPF_DELAY_LINE_LENGTH = 8; // Must be a power of 2
        const int ACCURATE_LPF_DELAY_LINE_LENGTH = 16; // Must be a power of 2

        const int ACCURATE_LPF_NUMBER_OF_PHASES = 3; // Upsampling factor
        const int ACCURATE_LPF_PHASE_INCREMENT_REGULAR = 2; // Downsampling factor
        const int ACCURATE_LPF_PHASE_INCREMENT_OVERSAMPLED = 1; // No downsampling
        static readonly uint[,] ACCURATE_LPF_DELTAS_REGULAR = { { 0, 0, 0 }, { 1, 1, 0 }, { 1, 2, 1 } };
        static readonly uint[,] ACCURATE_LPF_DELTAS_OVERSAMPLED = { { 0, 0, 0 }, { 1, 0, 0 }, { 1, 0, 1 } };

        private float[] LPF_TAPS;
        private uint[,] deltas;
        private int phaseIncrement;
        private int outputSampleRate;

        private float[] ringBuffer = new float[ACCURATE_LPF_DELAY_LINE_LENGTH];
        private int ringBufferPosition;
        private int phase;

        public AccurateLowPassFilter(bool oldMT32AnalogLPF, bool oversample)
        {
            LPF_TAPS = oldMT32AnalogLPF ? ACCURATE_LPF_TAPS_MT32 : ACCURATE_LPF_TAPS_CM32L;
            deltas = oversample ? ACCURATE_LPF_DELTAS_OVERSAMPLED : ACCURATE_LPF_DELTAS_REGULAR;
            phaseIncrement = oversample ? ACCURATE_LPF_PHASE_INCREMENT_OVERSAMPLED : ACCURATE_LPF_PHASE_INCREMENT_REGULAR;
            outputSampleRate = Mt32Emu.SAMPLE_RATE * ACCURATE_LPF_NUMBER_OF_PHASES / phaseIncrement;
        }

        public override float Process(float inSample)
        {
            float sample = (phase == 0) ? LPF_TAPS[ACCURATE_LPF_DELAY_LINE_LENGTH * ACCURATE_LPF_NUMBER_OF_PHASES] * ringBuffer[ringBufferPosition] : 0.0f;
            if (!HasNextSample())
            {
                ringBuffer[ringBufferPosition] = inSample;
            }

            for (int tapIx = phase, delaySampleIx = 0; delaySampleIx < ACCURATE_LPF_DELAY_LINE_LENGTH; delaySampleIx++, tapIx += ACCURATE_LPF_NUMBER_OF_PHASES)
            {
                sample += LPF_TAPS[tapIx] * ringBuffer[(delaySampleIx + ringBufferPosition) & DELAY_LINE_MASK];
            }

            phase += phaseIncrement;
            if (ACCURATE_LPF_NUMBER_OF_PHASES <= phase)
            {
                phase -= ACCURATE_LPF_NUMBER_OF_PHASES;
                ringBufferPosition = (ringBufferPosition - 1) & DELAY_LINE_MASK;
            }

            return ACCURATE_LPF_NUMBER_OF_PHASES * sample;
        }

        public override bool HasNextSample()
        {
            return phaseIncrement <= phase;
        }
    }

    abstract class AbstractLowPassFilter
    {
        // Integer versions of the FIRs above multiplied by (1 << 14) and rounded.
        protected static readonly float[] COARSE_LPF_TAPS_MT32 = {
            20848, -3609, -2589, 2943, -1827, 887, -385, 180, -114
        };

        protected static readonly float[] COARSE_LPF_TAPS_CM32L = {
            21965, -6608, 590, 1084, -1142, 812, -510, 314, -204
        };

        /* Combined FIR that both approximates the impulse response of the analogue circuits of sample & hold and the low pass filter
         * in the audible frequency range (below 20 kHz) and attenuates unwanted mirror spectra above 28 kHz as well. It is a polyphase
         * filter intended for resampling the signal to 48 kHz yet for applying high frequency boost.
         * As with the filter above, the analogue LPF frequency response is obtained for 1536 pin grid for range up to 96 kHz and multiplied
         * by the corresponding sinc. The result is further squared, windowed and passed to generalised Parks-McClellan routine as a desired response.
         * Finally, the minimum phase factor is found that's essentially the coefficients below.
         * Relative error in the audible frequency range doesn't exceed 0.0006%, attenuation in the stopband is better than 100 dB.
         * This level of performance makes it nearly bit-accurate for standard 16-bit sample resolution.
         */

        // FIR version for MT-32 first generation.
        protected static readonly float[] ACCURATE_LPF_TAPS_MT32 = {
            0.003429281f, 0.025929869f, 0.096587777f, 0.228884848f, 0.372413431f, 0.412386503f, 0.263980018f,
            -0.014504962f, -0.237394528f, -0.257043496f, -0.103436603f, 0.063996095f, 0.124562333f, 0.083703206f,
            0.013921662f, -0.033475018f, -0.046239712f, -0.029310921f, 0.00126585f, 0.021060961f, 0.017925605f,
            0.003559874f, -0.005105248f, -0.005647917f, -0.004157918f, -0.002065664f, 0.00158747f, 0.003762585f,
            0.001867137f, -0.001090028f, -0.001433979f, -0.00022367f, 4.34308E-05f, -0.000247827f, 0.000157087f,
            0.000605823f, 0.000197317f, -0.000370511f, -0.000261202f, 9.96069E-05f, 9.85073E-05f, -5.28754E-05f,
            -1.00912E-05f, 7.69943E-05f, 2.03162E-05f, -5.67967E-05f, -3.30637E-05f, 1.61958E-05f, 1.73041E-05f
        };

        // FIR version for new MT-32 and CM-32L/LAPC-I.
        protected static readonly float[] ACCURATE_LPF_TAPS_CM32L = {
            0.003917452f, 0.030693861f, 0.116424199f, 0.275101674f, 0.43217361f, 0.431247894f, 0.183255659f,
            -0.174955671f, -0.354240244f, -0.212401714f, 0.072259178f, 0.204655344f, 0.108336211f, -0.039099027f,
            -0.075138174f, -0.026261906f, 0.00582663f, 0.003052193f, 0.00613657f, 0.017017951f, 0.008732535f,
            -0.011027427f, -0.012933664f, 0.001158097f, 0.006765958f, 0.00046778f, -0.002191106f, 0.001561017f,
            0.001842871f, -0.001996876f, -0.002315836f, 0.000980965f, 0.001817454f, -0.000243272f, -0.000972848f,
            0.000149941f, 0.000498886f, -0.000204436f, -0.000347415f, 0.000142386f, 0.000249137f, -4.32946E-05f,
            -0.000131231f, 3.88575E-07f, 4.48813E-05f, -1.31906E-06f, -1.03499E-05f, 7.71971E-06f, 2.86721E-06f
        };

        public static AbstractLowPassFilter CreateLowPassFilter(AnalogOutputMode mode, bool oldMT32AnalogLPF)
        {
            switch (mode)
            {
                case AnalogOutputMode.COARSE:
                    return new CoarseLowPassFilter(oldMT32AnalogLPF);
                case AnalogOutputMode.ACCURATE:
                    return new AccurateLowPassFilter(oldMT32AnalogLPF, false);
                case AnalogOutputMode.OVERSAMPLED:
                    return new AccurateLowPassFilter(oldMT32AnalogLPF, true);
                default:
                    return new NullLowPassFilter();
            }
        }

        public static void MuteRingBuffer(float[] ringBuffer, uint length)
        {
            Array.Clear(ringBuffer, 0, ringBuffer.Length);
        }

        public abstract float Process(float sample);
        public virtual bool HasNextSample() { return false; }
        //public virtual uint OutputSampleRate { get; }
        //public virtual uint EstimateInSampleCount(uint outSamples) const;
        //public virtual void AddPositionIncrement(uint) { }
    }

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

        public float SynthOutputGain
        {
            get { return synthGain; }
            set { synthGain = value; }
        }

        public Analog(AnalogOutputMode mode, ControlROMFeatureSet controlROMFeatures)
        {
            leftChannelLPF = AbstractLowPassFilter.CreateLowPassFilter(mode, controlROMFeatures.IsOldMT32AnalogLPF);
            rightChannelLPF = AbstractLowPassFilter.CreateLowPassFilter(mode, controlROMFeatures.IsOldMT32AnalogLPF);
        }

        public void SetReverbOutputGain(float useReverbGain, bool mt32ReverbCompatibilityMode)
        {
            if (!mt32ReverbCompatibilityMode) useReverbGain *= CM32L_REVERB_TO_LA32_ANALOG_OUTPUT_GAIN_FACTOR;
            reverbGain = useReverbGain;
        }
    }
}
