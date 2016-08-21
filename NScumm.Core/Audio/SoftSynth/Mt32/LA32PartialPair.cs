//
//  LA32PartialPair.cs
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

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    public enum LA32PartialPairType
    {
        MASTER,
        SLAVE
    }

    /// <summary>
    /// LA32PartialPair contains a structure of two partials being mixed / ring modulated.
    /// </summary>
    class LA32PartialPair
    {
        LA32WaveGenerator master = new LA32WaveGenerator();
        LA32WaveGenerator slave = new LA32WaveGenerator();
        bool ringModulated;
        bool mixed;

        public void Deactivate(LA32PartialPairType useMaster)
        {
            if (useMaster == LA32PartialPairType.MASTER)
            {
                master.Deactivate();
            }
            else {
                slave.Deactivate();
            }
        }

        public bool IsActive(LA32PartialPairType useMaster)
        {
            return useMaster == LA32PartialPairType.MASTER ? master.IsActive : slave.IsActive;
        }

        public void GenerateNextSample(LA32PartialPairType useMaster, int amp, ushort pitch, int cutoff)
        {
            if (useMaster == LA32PartialPairType.MASTER)
            {
                master.GenerateNextSample(amp, pitch, cutoff);
            }
            else {
                slave.GenerateNextSample(amp, pitch, cutoff);
            }
        }

        public short NextOutSample()
        {
            if (!ringModulated)
            {
                return (short)(UnlogAndMixWGOutput(master) + UnlogAndMixWGOutput(slave));
            }

            /*
             * SEMI-CONFIRMED: Ring modulation model derived from sample analysis of specially constructed patches which exploit distortion.
             * LA32 ring modulator found to produce distorted output in case if the absolute value of maximal amplitude of one of the input partials exceeds 8191.
             * This is easy to reproduce using synth partials with resonance values close to the maximum. It looks like an integer overflow happens in this case.
             * As the distortion is strictly bound to the amplitude of the complete mixed square + resonance wave in the linear space,
             * it is reasonable to assume the ring modulation is performed also in the linear space by sample multiplication.
             * Most probably the overflow is caused by limited precision of the multiplication circuit as the very similar distortion occurs with panning.
             */
            short nonOverdrivenMasterSample = UnlogAndMixWGOutput(master); // Store master partial sample for further mixing
            short masterSample = (short)(nonOverdrivenMasterSample << 2);
            masterSample >>= 2;

            /* SEMI-CONFIRMED from sample analysis:
             * We observe that for partial structures with ring modulation the interpolation is not applied to the slave PCM partial.
             * It's assumed that the multiplication circuitry intended to perform the interpolation on the slave PCM partial
             * is borrowed by the ring modulation circuit (or the LA32 chip has a similar lack of resources assigned to each partial pair).
             */
            short slaveSample = slave.IsPCMWave ? LA32Utilites.Unlog(slave.GetOutputLogSample(true)) : UnlogAndMixWGOutput(slave);
            slaveSample <<= 2;
            slaveSample >>= 2;
            short ringModulatedSample = (short)((masterSample * slaveSample) >> 13);
            return (short)(mixed ? nonOverdrivenMasterSample + ringModulatedSample : ringModulatedSample);
        }

        public void InitPCM(LA32PartialPairType useMaster, Ptr<short> pcmWaveAddress, int pcmWaveLength, bool pcmWaveLooped)
        {
            if (useMaster == LA32PartialPairType.MASTER)
            {
                master.InitPCM(pcmWaveAddress, pcmWaveLength, pcmWaveLooped, true);
            }
            else {
                slave.InitPCM(pcmWaveAddress, pcmWaveLength, pcmWaveLooped, !ringModulated);
            }
        }

        public void Init(bool useRingModulated, bool useMixed)
        {
            ringModulated = useRingModulated;
            mixed = useMixed;
        }

        public void InitSynth(LA32PartialPairType useMaster, bool sawtoothWaveform, byte pulseWidth, byte resonance)
        {
            if (useMaster == LA32PartialPairType.MASTER)
            {
                master.InitSynth(sawtoothWaveform, pulseWidth, resonance);
            }
            else {
                slave.InitSynth(sawtoothWaveform, pulseWidth, resonance);
            }
        }

        private short UnlogAndMixWGOutput(LA32WaveGenerator wg)
        {
            if (!wg.IsActive)
            {
                return 0;
            }
            short firstSample = LA32Utilites.Unlog(wg.GetOutputLogSample(true));
            short secondSample = LA32Utilites.Unlog(wg.GetOutputLogSample(false));
            if (wg.IsPCMWave)
            {
                return ((short)(firstSample + (((secondSample - firstSample) * wg.PCMInterpolationFactor) >> 7)));
            }
            return (short)(firstSample + secondSample);
        }

    }
}