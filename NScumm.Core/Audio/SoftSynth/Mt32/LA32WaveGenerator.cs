//
//  LA32WaveGenerator.cs
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
    /**
     * LA32 performs wave generation in the log-space that allows replacing multiplications by cheap additions
     * It's assumed that only low-bit multiplications occur in a few places which are unavoidable like these:
     * - interpolation of exponent table (obvious, a delta value has 4 bits)
     * - computation of resonance amp decay envelope (the table contains values with 1-2 "1" bits except the very first value 31 but this case can be found using inversion)
     * - interpolation of PCM samples (obvious, the wave position counter is in the linear space, there is no log() table in the chip)
     * and it seems to be implemented in the same way as in the Boss chip, i.e. right shifted additions which involved noticeable precision loss
     * Subtraction is supposed to be replaced by simple inversion
     * As the logarithmic sine is always negative, all the logarithmic values are treated as decrements
     */
    class LogSample
    {
        // 16-bit fixed point value, includes 12-bit fractional part
        // 4-bit integer part allows to present any 16-bit sample in the log-space
        // Obviously, the log value doesn't contain the sign of the resulting sample
        public ushort logValue;
        public enum Sign
        {
            POSITIVE,
            NEGATIVE
        }
        public Sign sign;
    }

    class LA32Utilites
    {
        public static ushort InterpolateExp(ushort fract)
        {
            ushort expTabIndex = (ushort)(fract >> 3);
            ushort extraBits = (ushort)(~fract & 7);
            ushort expTabEntry2 = (ushort)(8191 - Tables.Instance.exp9[expTabIndex]);
            ushort expTabEntry1 = (ushort)(expTabIndex == 0 ? 8191 : (8191 - Tables.Instance.exp9[expTabIndex - 1]));
            return (ushort)(expTabEntry2 + (((expTabEntry1 - expTabEntry2) * extraBits) >> 3));
        }
        public static short Unlog(LogSample logSample)
        {
            //Bit16s sample = (Bit16s)EXP2F(13.0f - logSample.logValue / 1024.0f);
            int intLogValue = (logSample.logValue >> 12);
            ushort fracLogValue = (ushort)(logSample.logValue & 4095);
            short sample = (short)(InterpolateExp(fracLogValue) >> intLogValue);
            return (short)(logSample.sign == LogSample.Sign.POSITIVE ? sample : -sample);
        }

        public static void AddLogSamples(LogSample logSample1, LogSample logSample2)
        {
            uint logSampleValue = (uint)(logSample1.logValue + logSample2.logValue);
            logSample1.logValue = (ushort)(logSampleValue < 65536 ? (ushort)logSampleValue : 65535);
            logSample1.sign = logSample1.sign == logSample2.sign ? LogSample.Sign.POSITIVE : LogSample.Sign.NEGATIVE;
        }
    }

    /// <summary>
    /// LA32WaveGenerator is aimed to represent the exact model of LA32 wave generator.
    /// The output square wave is created by adding high / low linear segments in-between
    /// the rising and falling cosine segments.Basically, it�s very similar to the phase distortion synthesis.
    /// Behaviour of a true resonance filter is emulated by adding decaying sine wave.
    /// The beginning and the ending of the resonant sine is multiplied by a cosine window.
    /// To synthesise sawtooth waves, the resulting square wave is multiplied by synchronous cosine wave.
    /// </summary>
    class LA32WaveGenerator
    {
        const int SINE_SEGMENT_RELATIVE_LENGTH = 1 << 18;
        const int MIDDLE_CUTOFF_VALUE = 128 << 18;
        const int RESONANCE_DECAY_THRESHOLD_CUTOFF_VALUE = 144 << 18;
        const int MAX_CUTOFF_VALUE = 240 << 18;
        static readonly LogSample SILENCE = new LogSample { logValue = 65535, sign = LogSample.Sign.POSITIVE };

        //***************************************************************************
        //  The local copy of partial parameters below
        //***************************************************************************

        bool active;

        // True means the resulting square wave is to be multiplied by the synchronous cosine
        bool sawtoothWaveform;

        // Logarithmic amp of the wave generator
        int amp;

        // Logarithmic frequency of the resulting wave
        ushort pitch;

        // Values in range [1..31]
        // Value 1 correspong to the minimum resonance
        byte resonance;

        // Processed value in range [0..255]
        // Values in range [0..128] have no effect and the resulting wave remains symmetrical
        // Value 255 corresponds to the maximum possible asymmetric of the resulting wave
        byte pulseWidth;

        // Composed of the base cutoff in range [78..178] left-shifted by 18 bits and the TVF modifier
        int cutoffVal;

        // Logarithmic PCM sample start address
        Ptr<short> pcmWaveAddress;

        // Logarithmic PCM sample length
        int pcmWaveLength;

        // true for looped logarithmic PCM samples
        bool pcmWaveLooped;

        // false for slave PCM partials in the structures with the ring modulation
        bool pcmWaveInterpolated;

        //***************************************************************************
        // Internal variables below
        //***************************************************************************

        // Relative position within either the synth wave or the PCM sampled wave
        // 0 - start of the positive rising sine segment of the square wave or start of the PCM sample
        // 1048576 (2^20) - end of the negative rising sine segment of the square wave
        // For PCM waves, the address of the currently playing sample equals (wavePosition / 256)
        int wavePosition;

        // Relative position within a square wave phase:
        // 0             - start of the phase
        // 262144 (2^18) - end of a sine phase in the square wave
        int squareWavePosition;

        // Relative position within the positive or negative wave segment:
        // 0 - start of the corresponding positive or negative segment of the square wave
        // 262144 (2^18) - corresponds to end of the first sine phase in the square wave
        // The same increment sampleStep is used to indicate the current position
        // since the length of the resonance wave is always equal to four square wave sine segments.
        int resonanceSinePosition;

        // The amp of the resonance sine wave grows with the resonance value
        // As the resonance value cannot change while the partial is active, it is initialised once
        int resonanceAmpSubtraction;

        // The decay speed of resonance sine wave, depends on the resonance value
        int resAmpDecayFactor;

        // Fractional part of the pcmPosition
        int pcmInterpolationFactor;

        // Current phase of the square wave
        enum WavePhase
        {
            POSITIVE_RISING_SINE_SEGMENT,
            POSITIVE_LINEAR_SEGMENT,
            POSITIVE_FALLING_SINE_SEGMENT,
            NEGATIVE_FALLING_SINE_SEGMENT,
            NEGATIVE_LINEAR_SEGMENT,
            NEGATIVE_RISING_SINE_SEGMENT
        }
        WavePhase phase;

        // Current phase of the resonance wave
        enum ResonancePhase
        {
            POSITIVE_RISING_RESONANCE_SINE_SEGMENT,
            POSITIVE_FALLING_RESONANCE_SINE_SEGMENT,
            NEGATIVE_FALLING_RESONANCE_SINE_SEGMENT,
            NEGATIVE_RISING_RESONANCE_SINE_SEGMENT
        }
        ResonancePhase resonancePhase;

        // Resulting log-space samples of the square and resonance waves
        LogSample squareLogSample = new LogSample();
        LogSample resonanceLogSample = new LogSample();

        // Processed neighbour log-space samples of the PCM wave
        LogSample firstPCMLogSample = new LogSample();
        LogSample secondPCMLogSample = new LogSample();

        public bool IsActive { get { return active; } }

        public bool IsPCMWave { get { return pcmWaveAddress != null; } }

        public int PCMInterpolationFactor { get { return pcmInterpolationFactor; } }

        public LogSample GetOutputLogSample(bool first)
        {
            if (!IsActive)
            {
                return SILENCE;
            }
            if (IsPCMWave)
            {
                return first ? firstPCMLogSample : secondPCMLogSample;
            }
            return first ? squareLogSample : resonanceLogSample;
        }

        public void InitSynth(bool useSawtoothWaveform, byte usePulseWidth, byte useResonance)
        {
            sawtoothWaveform = useSawtoothWaveform;
            pulseWidth = usePulseWidth;
            resonance = useResonance;

            wavePosition = 0;

            squareWavePosition = 0;
            phase = WavePhase.POSITIVE_RISING_SINE_SEGMENT;

            resonanceSinePosition = 0;
            resonancePhase = ResonancePhase.POSITIVE_RISING_RESONANCE_SINE_SEGMENT;
            resonanceAmpSubtraction = (32 - resonance) << 10;
            resAmpDecayFactor = Tables.Instance.resAmpDecayFactor[resonance >> 2] << 2;

            pcmWaveAddress = null;
            active = true;
        }

        public void InitPCM(Ptr<short> usePCMWaveAddress, int usePCMWaveLength, bool usePCMWaveLooped, bool usePCMWaveInterpolated)
        {
            pcmWaveAddress = usePCMWaveAddress;
            pcmWaveLength = usePCMWaveLength;
            pcmWaveLooped = usePCMWaveLooped;
            pcmWaveInterpolated = usePCMWaveInterpolated;

            wavePosition = 0;
            active = true;
        }

        public void Deactivate()
        {
            active = false;
        }

        public void GenerateNextSample(int useAmp, ushort usePitch, int useCutoffVal)
        {
            if (!active)
            {
                return;
            }

            amp = useAmp;
            pitch = usePitch;

            if (IsPCMWave)
            {
                GenerateNextPCMWaveLogSamples();
                return;
            }

            // The 240 cutoffVal limit was determined via sample analysis (internal Munt capture IDs: glop3, glop4).
            // More research is needed to be sure that this is correct, however.
            cutoffVal = (useCutoffVal > MAX_CUTOFF_VALUE) ? MAX_CUTOFF_VALUE : useCutoffVal;

            GenerateNextSquareWaveLogSample();
            GenerateNextResonanceWaveLogSample();
            if (sawtoothWaveform)
            {
                var cosineLogSample = new LogSample();
                GenerateNextSawtoothCosineLogSample(cosineLogSample);
                LA32Utilites.AddLogSamples(squareLogSample, cosineLogSample);
                LA32Utilites.AddLogSamples(resonanceLogSample, cosineLogSample);
            }
            AdvancePosition();
        }

        private void AdvancePosition()
        {
            wavePosition = wavePosition + GetSampleStep();
            wavePosition %= 4 * SINE_SEGMENT_RELATIVE_LENGTH;

            int effectiveCutoffValue = (cutoffVal > MIDDLE_CUTOFF_VALUE) ? (cutoffVal - MIDDLE_CUTOFF_VALUE) >> 10 : 0;
            int resonanceWaveLengthFactor = GetResonanceWaveLengthFactor(effectiveCutoffValue);
            int highLinearLength = GetHighLinearLength(effectiveCutoffValue);
            int lowLinearLength = (resonanceWaveLengthFactor << 8) - 4 * SINE_SEGMENT_RELATIVE_LENGTH - highLinearLength;
            ComputePositions(highLinearLength, lowLinearLength, resonanceWaveLengthFactor);

            // resonancePhase computation hack
            resonancePhase = (ResonancePhase)(((resonanceSinePosition >> 18) + (phase > WavePhase.POSITIVE_FALLING_SINE_SEGMENT ? 2 : 0)) & 3);
        }

        private void ComputePositions(int highLinearLength, int lowLinearLength, int resonanceWaveLengthFactor)
        {
            // Assuming 12-bit multiplication used here
            squareWavePosition = resonanceSinePosition = (wavePosition >> 8) * (resonanceWaveLengthFactor >> 4);
            if (squareWavePosition < SINE_SEGMENT_RELATIVE_LENGTH)
            {
                phase = WavePhase.POSITIVE_RISING_SINE_SEGMENT;
                return;
            }
            squareWavePosition -= SINE_SEGMENT_RELATIVE_LENGTH;
            if (squareWavePosition < highLinearLength)
            {
                phase = WavePhase.POSITIVE_LINEAR_SEGMENT;
                return;
            }
            squareWavePosition -= highLinearLength;
            if (squareWavePosition < SINE_SEGMENT_RELATIVE_LENGTH)
            {
                phase = WavePhase.POSITIVE_FALLING_SINE_SEGMENT;
                return;
            }
            squareWavePosition -= SINE_SEGMENT_RELATIVE_LENGTH;
            resonanceSinePosition = squareWavePosition;
            if (squareWavePosition < SINE_SEGMENT_RELATIVE_LENGTH)
            {
                phase = WavePhase.NEGATIVE_FALLING_SINE_SEGMENT;
                return;
            }
            squareWavePosition -= SINE_SEGMENT_RELATIVE_LENGTH;
            if (squareWavePosition < lowLinearLength)
            {
                phase = WavePhase.NEGATIVE_LINEAR_SEGMENT;
                return;
            }
            squareWavePosition -= lowLinearLength;
            phase = WavePhase.NEGATIVE_RISING_SINE_SEGMENT;
        }

        private int GetHighLinearLength(int effectiveCutoffValue)
        {
            // Ratio of positive segment to wave length
            int effectivePulseWidthValue = 0;
            if (pulseWidth > 128)
            {
                effectivePulseWidthValue = (pulseWidth - 128) << 6;
            }

            int highLinearLength = 0;
            // highLinearLength = EXP2F(19.0f - effectivePulseWidthValue / 4096.0f + effectiveCutoffValue / 4096.0f) - 2 * SINE_SEGMENT_RELATIVE_LENGTH;
            if (effectivePulseWidthValue < effectiveCutoffValue)
            {
                int expArg = effectiveCutoffValue - effectivePulseWidthValue;
                highLinearLength = LA32Utilites.InterpolateExp((ushort)(~expArg & 4095));
                highLinearLength <<= 7 + (expArg >> 12);
                highLinearLength -= 2 * SINE_SEGMENT_RELATIVE_LENGTH;
            }
            return highLinearLength;
        }

        private int GetResonanceWaveLengthFactor(int effectiveCutoffValue)
        {
            // resonanceWaveLengthFactor = (Bit32u)EXP2F(12.0f + effectiveCutoffValue / 4096.0f);
            int resonanceWaveLengthFactor = LA32Utilites.InterpolateExp((ushort)(~effectiveCutoffValue & 4095));
            resonanceWaveLengthFactor <<= effectiveCutoffValue >> 12;
            return resonanceWaveLengthFactor;
        }

        private int GetSampleStep()
        {
            // sampleStep = EXP2F(pitch / 4096.0f + 4.0f)
            int sampleStep = LA32Utilites.InterpolateExp((ushort)(~pitch & 4095));
            sampleStep <<= pitch >> 12;
            sampleStep >>= 8;
            sampleStep &= ~1;
            return sampleStep;
        }

        private void GenerateNextSawtoothCosineLogSample(LogSample logSample)
        {
            int sawtoothCosinePosition = wavePosition + (1 << 18);
            if ((sawtoothCosinePosition & (1 << 18)) > 0)
            {
                logSample.logValue = Tables.Instance.logsin9[~(sawtoothCosinePosition >> 9) & 511];
            }
            else {
                logSample.logValue = Tables.Instance.logsin9[(sawtoothCosinePosition >> 9) & 511];
            }
            logSample.logValue <<= 2;
            logSample.sign = ((sawtoothCosinePosition & (1 << 19)) == 0) ? LogSample.Sign.POSITIVE : LogSample.Sign.NEGATIVE;
        }

        private void GenerateNextResonanceWaveLogSample()
        {
            int logSampleValue;
            if (resonancePhase == ResonancePhase.POSITIVE_FALLING_RESONANCE_SINE_SEGMENT || resonancePhase == ResonancePhase.NEGATIVE_RISING_RESONANCE_SINE_SEGMENT)
            {
                logSampleValue = Tables.Instance.logsin9[~(resonanceSinePosition >> 9) & 511];
            }
            else {
                logSampleValue = Tables.Instance.logsin9[(resonanceSinePosition >> 9) & 511];
            }
            logSampleValue <<= 2;
            logSampleValue += amp >> 10;

            // From the digital captures, the decaying speed of the resonance sine is found a bit different for the positive and the negative segments
            int decayFactor = phase < WavePhase.NEGATIVE_FALLING_SINE_SEGMENT ? resAmpDecayFactor : resAmpDecayFactor + 1;
            // Unsure about resonanceSinePosition here. It's possible that dedicated counter & decrement are used. Although, cutoff is finely ramped, so maybe not.
            logSampleValue += resonanceAmpSubtraction + (((resonanceSinePosition >> 4) * decayFactor) >> 8);

            // To ensure the output wave has no breaks, two different windows are appied to the beginning and the ending of the resonance sine segment
            if (phase == WavePhase.POSITIVE_RISING_SINE_SEGMENT || phase == WavePhase.NEGATIVE_FALLING_SINE_SEGMENT)
            {
                // The window is synchronous sine here
                logSampleValue += Tables.Instance.logsin9[(squareWavePosition >> 9) & 511] << 2;
            }
            else if (phase == WavePhase.POSITIVE_FALLING_SINE_SEGMENT || phase == WavePhase.NEGATIVE_RISING_SINE_SEGMENT)
            {
                // The window is synchronous square sine here
                logSampleValue += Tables.Instance.logsin9[~(squareWavePosition >> 9) & 511] << 3;
            }

            if (cutoffVal < MIDDLE_CUTOFF_VALUE)
            {
                // For the cutoff values below the cutoff middle point, it seems the amp of the resonance wave is expotentially decayed
                logSampleValue = logSampleValue + 31743 + ((MIDDLE_CUTOFF_VALUE - cutoffVal) >> 9);
            }
            else if (cutoffVal < RESONANCE_DECAY_THRESHOLD_CUTOFF_VALUE)
            {
                // For the cutoff values below this point, the amp of the resonance wave is sinusoidally decayed
                int sineIx = (cutoffVal - MIDDLE_CUTOFF_VALUE) >> 13;
                logSampleValue += Tables.Instance.logsin9[sineIx] << 2;
            }

            // After all the amp decrements are added, it should be safe now to adjust the amp of the resonance wave to what we see on captures
            logSampleValue -= 1 << 12;

            resonanceLogSample.logValue = (ushort)(logSampleValue < 65536 ? logSampleValue : 65535);
            resonanceLogSample.sign = resonancePhase < ResonancePhase.NEGATIVE_FALLING_RESONANCE_SINE_SEGMENT ? LogSample.Sign.POSITIVE : LogSample.Sign.NEGATIVE;
        }

        private void GenerateNextSquareWaveLogSample()
        {
            int logSampleValue;
            switch (phase)
            {
                case WavePhase.POSITIVE_RISING_SINE_SEGMENT:
                case WavePhase.NEGATIVE_FALLING_SINE_SEGMENT:
                    logSampleValue = Tables.Instance.logsin9[(squareWavePosition >> 9) & 511];
                    break;
                case WavePhase.POSITIVE_FALLING_SINE_SEGMENT:
                case WavePhase.NEGATIVE_RISING_SINE_SEGMENT:
                    logSampleValue = Tables.Instance.logsin9[~(squareWavePosition >> 9) & 511];
                    break;
                case WavePhase.POSITIVE_LINEAR_SEGMENT:
                case WavePhase.NEGATIVE_LINEAR_SEGMENT:
                default:
                    logSampleValue = 0;
                    break;
            }
            logSampleValue <<= 2;
            logSampleValue += amp >> 10;
            if (cutoffVal < MIDDLE_CUTOFF_VALUE)
            {
                logSampleValue += (MIDDLE_CUTOFF_VALUE - cutoffVal) >> 9;
            }

            squareLogSample.logValue = (ushort)(logSampleValue < 65536 ? logSampleValue : 65535);
            squareLogSample.sign = phase < WavePhase.NEGATIVE_FALLING_SINE_SEGMENT ? LogSample.Sign.POSITIVE : LogSample.Sign.NEGATIVE;
        }

        private void GenerateNextPCMWaveLogSamples()
        {
            // This should emulate the ladder we see in the PCM captures for pitches 01, 02, 07, etc.
            // The most probable cause is the factor in the interpolation formula is one bit less
            // accurate than the sample position counter
            pcmInterpolationFactor = (wavePosition & 255) >> 1;
            int pcmWaveTableIx = wavePosition >> 8;
            PcmSampleToLogSample(firstPCMLogSample, pcmWaveAddress[pcmWaveTableIx]);
            if (pcmWaveInterpolated)
            {
                pcmWaveTableIx++;
                if (pcmWaveTableIx < pcmWaveLength)
                {
                    PcmSampleToLogSample(secondPCMLogSample, pcmWaveAddress[pcmWaveTableIx]);
                }
                else {
                    if (pcmWaveLooped)
                    {
                        pcmWaveTableIx = pcmWaveTableIx - pcmWaveLength;
                        PcmSampleToLogSample(secondPCMLogSample, pcmWaveAddress[pcmWaveTableIx]);
                    }
                    else {
                        secondPCMLogSample = SILENCE;
                    }
                }
            }
            else {
                secondPCMLogSample = SILENCE;
            }
            // pcmSampleStep = (Bit32u)EXP2F(pitch / 4096.0f + 3.0f);
            int pcmSampleStep = LA32Utilites.InterpolateExp((ushort)(~pitch & 4095));
            pcmSampleStep <<= pitch >> 12;
            // Seeing the actual lengths of the PCM wave for pitches 00..12,
            // the pcmPosition counter can be assumed to have 8-bit fractions
            pcmSampleStep >>= 9;
            wavePosition += pcmSampleStep;
            if (wavePosition >= (pcmWaveLength << 8))
            {
                if (pcmWaveLooped)
                {
                    wavePosition = wavePosition - (pcmWaveLength << 8);
                }
                else {
                    Deactivate();
                }
            }
        }

        private void PcmSampleToLogSample(LogSample logSample, short pcmSample)
        {
            int logSampleValue = (32787 - (pcmSample & 32767)) << 1;
            logSampleValue += amp >> 10;
            logSample.logValue = (ushort)(logSampleValue < 65536 ? logSampleValue : 65535);
            logSample.sign = pcmSample < 0 ? LogSample.Sign.NEGATIVE : LogSample.Sign.POSITIVE;
        }
    }
}
