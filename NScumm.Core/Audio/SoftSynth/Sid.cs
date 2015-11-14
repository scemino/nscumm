//
//  Sid.cs
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

/*
 *  This file is based on reSID, a MOS6581 SID emulator engine.
 *  Copyright (C) 2004  Dag Lem <resid@nimrod.no>
 */

namespace NScumm.Core.Audio.SoftSynth
{
    public class SID : ISid
    {
        // Fixpoint constants (16.16 bits).
        const int FIXP_SHIFT = 16;
        const int FIXP_MASK = 0xffff;

        public SID()
        {
            for (int i = 0; i < voice.Length; i++)
            {
                voice[i] = new Voice();
            }
            voice[0].SetSyncSource(voice[2]);
            voice[1].SetSyncSource(voice[0]);
            voice[2].SetSyncSource(voice[1]);

            SetSamplingParameters(985248, 44100);

            bus_value = 0;
            bus_value_ttl = 0;
        }

        public bool EnableFilter
        { 
            get { return filter.IsEnabled; } 
            set { filter.IsEnabled = value; } 
        }

        bool EnableExternalFilter
        { 
            get { return extfilt.IsEnabled; } 
            set { extfilt.IsEnabled = value; } 
        }

        /// <summary>
        /// Setting of SID sampling parameters.
        /// </summary>
        /// <returns><c>true</c>, if sampling parameters was set, <c>false</c> otherwise.</returns>
        /// <param name="clock_freq">Clock freq.</param>
        /// <param name="sample_freq">Sample freq.</param>
        /// <param name="pass_freq">Pass freq.</param>
        /// <param name="filter_scale">Filter scale.</param>
        /// <remarks>
        /// Use a clock freqency of 985248Hz for PAL C64, 1022730Hz for NTSC C64.
        /// The default end of passband frequency is pass_freq = 0.9*sample_freq/2
        /// for sample frequencies up to ~ 44.1kHz, and 20kHz for higher sample
        /// frequencies.
        /// 
        ///  For resampling, the ratio between the clock frequency and the sample
        ///  frequency is limited as follows:
        ///  125*clock_freq/sample_freq < 16384
        ///  E.g. provided a clock frequency of ~ 1MHz, the sample frequency can not
        ///  be set lower than ~ 8kHz. A lower sample frequency would make the
        ///  resampling code overfill its 16k sample ring buffer.
        ///  The end of passband frequency is also limited:
        ///  pass_freq <= 0.9*sample_freq/2
        ///  E.g. for a 44.1kHz sampling rate the end of passband frequency is limited
        ///  to slightly below 20kHz. This constraint ensures that the FIR table is
        ///  not overfilled.
        /// </remarks>
        public bool SetSamplingParameters(double clock_freq, double sample_freq, double pass_freq = -1, double filter_scale = 0.97)
        {
            // The default passband limit is 0.9*sample_freq/2 for sample
            // frequencies below ~ 44.1kHz, and 20kHz for higher sample frequencies.
            if (pass_freq < 0)
            {
                pass_freq = 20000;
                if (2 * pass_freq / sample_freq >= 0.9)
                {
                    pass_freq = 0.9 * sample_freq / 2;
                }
            }
            // Check whether the FIR table would overfill.
            else if (pass_freq > 0.9 * sample_freq / 2)
            {
                return false;
            }

            // The filter scaling is only included to avoid clipping, so keep
            // it sane.
            if (filter_scale < 0.9 || filter_scale > 1.0)
            {
                return false;
            }

            // Set the external filter to the pass freq
            extfilt.SetSamplingParameter(pass_freq);
            clock_frequency = clock_freq;

            cycles_per_sample =
                (int)(clock_freq / sample_freq * (1 << FIXP_SHIFT) + 0.5);

            sample_offset = 0;
            sample_prev = 0;

            return true;
        }

        /// <summary>
        /// SID clocking with audio sampling.
        /// Fixpoint arithmetics is used.
        /// </summary>
        /// <returns>The clock.</returns>
        /// <param name="delta_t">Delta t.</param>
        /// <param name="buf">Buffer.</param>
        /// <param name="n">N.</param>
        /// <param name="offset">Offset.</param>
        /// <param name="interleave">Interleave.</param>
        public int UpdateClock(ref int delta_t, short[] buf, int n, int offset, int interleave = 1)
        {
            int s = 0;

            for (;;)
            {
                var next_sample_offset = sample_offset + cycles_per_sample + (1 << (FIXP_SHIFT - 1));
                var delta_t_sample = next_sample_offset >> FIXP_SHIFT;
                if (delta_t_sample > delta_t)
                {
                    break;
                }
                if (s >= n)
                {
                    return s;
                }
                UpdateClock(delta_t_sample);
                delta_t -= delta_t_sample;
                sample_offset = (next_sample_offset & FIXP_MASK) - (1 << (FIXP_SHIFT - 1));
                buf[offset + s++ * interleave] = (short)Output();
            }

            UpdateClock(delta_t);
            sample_offset -= delta_t << FIXP_SHIFT;
            delta_t = 0;
            return s;
        }

        public void Reset()
        {
            for (int i = 0; i < 3; i++)
            {
                voice[i].Reset();
            }
            filter.Reset();
            extfilt.Reset();

            bus_value = 0;
            bus_value_ttl = 0;
        }

        // Read/write registers.
        int Read(int offset)
        {
            switch (offset)
            {
                case 0x19:
                case 0x1a:
                    return 0; //readPOT();
                case 0x1b:
                    return voice[2].Wave.ReadOSC();
                case 0x1c:
                    return voice[2].Envelope.ReadENV();
                default:
                    return bus_value;
            }
        }

        public void Write(int offset, int value)
        {
            bus_value = value;
            bus_value_ttl = 0x2000;

            switch (offset)
            {
                case 0x00:
                    voice[0].Wave.WriteFREQ_LO(value);
                    break;
                case 0x01:
                    voice[0].Wave.WriteFREQ_HI(value);
                    break;
                case 0x02:
                    voice[0].Wave.WritePW_LO(value);
                    break;
                case 0x03:
                    voice[0].Wave.WritePW_HI(value);
                    break;
                case 0x04:
                    voice[0].WriteCONTROL_REG(value);
                    break;
                case 0x05:
                    voice[0].Envelope.WriteATTACK_DECAY(value);
                    break;
                case 0x06:
                    voice[0].Envelope.WriteSUSTAIN_RELEASE(value);
                    break;
                case 0x07:
                    voice[1].Wave.WriteFREQ_LO(value);
                    break;
                case 0x08:
                    voice[1].Wave.WriteFREQ_HI(value);
                    break;
                case 0x09:
                    voice[1].Wave.WritePW_LO(value);
                    break;
                case 0x0a:
                    voice[1].Wave.WritePW_HI(value);
                    break;
                case 0x0b:
                    voice[1].WriteCONTROL_REG(value);
                    break;
                case 0x0c:
                    voice[1].Envelope.WriteATTACK_DECAY(value);
                    break;
                case 0x0d:
                    voice[1].Envelope.WriteSUSTAIN_RELEASE(value);
                    break;
                case 0x0e:
                    voice[2].Wave.WriteFREQ_LO(value);
                    break;
                case 0x0f:
                    voice[2].Wave.WriteFREQ_HI(value);
                    break;
                case 0x10:
                    voice[2].Wave.WritePW_LO(value);
                    break;
                case 0x11:
                    voice[2].Wave.WritePW_HI(value);
                    break;
                case 0x12:
                    voice[2].WriteCONTROL_REG(value);
                    break;
                case 0x13:
                    voice[2].Envelope.WriteATTACK_DECAY(value);
                    break;
                case 0x14:
                    voice[2].Envelope.WriteSUSTAIN_RELEASE(value);
                    break;
                case 0x15:
                    filter.WriteFC_LO(value);
                    break;
                case 0x16:
                    filter.WriteFC_HI(value);
                    break;
                case 0x17:
                    filter.WriteRES_FILT(value);
                    break;
                case 0x18:
                    filter.WriteMODE_VOL(value);
                    break;
            }
        }

        // 16-bit output (AUDIO OUT).
        int Output()
        {
            const int range = 1 << 16;
            const int half = range >> 1;
            int sample = extfilt.Output() / ((4095 * 255 >> 7) * 3 * 15 * 2 / range);
            if (sample >= half)
            {
                return half - 1;
            }
            if (sample < -half)
            {
                return -half;
            }
            return sample;
        }

        void UpdateClock(int delta_t)
        {
            int i;

            if (delta_t <= 0)
            {
                return;
            }

            // Age bus value.
            bus_value_ttl -= delta_t;
            if (bus_value_ttl <= 0)
            {
                bus_value = 0;
                bus_value_ttl = 0;
            }

            // Clock amplitude modulators.
            for (i = 0; i < 3; i++)
            {
                voice[i].Envelope.UpdateClock(delta_t);
            }

            // Clock and synchronize oscillators.
            // Loop until we reach the current cycle.
            int delta_t_osc = delta_t;
            while (delta_t_osc != 0)
            {
                int delta_t_min = delta_t_osc;

                // Find minimum number of cycles to an oscillator accumulator MSB toggle.
                // We have to clock on each MSB on / MSB off for hard sync to operate
                // correctly.
                for (i = 0; i < 3; i++)
                {
                    var wave = voice[i].Wave;

                    // It is only necessary to clock on the MSB of an oscillator that is
                    // a sync source and has freq != 0.
                    if (!(wave.sync_dest.sync != 0 && wave.freq != 0))
                    {
                        continue;
                    }

                    int freq = wave.freq;
                    uint accumulator = wave.accumulator;

                    // Clock on MSB off if MSB is on, clock on MSB on if MSB is off.
                    uint delta_accumulator =
                        (uint)(((accumulator & 0x800000) != 0 ? 0x1000000 : 0x800000) - accumulator);

                    int delta_t_next = (int)(delta_accumulator / freq);
                    if ((delta_accumulator % freq) != 0)
                    {
                        ++delta_t_next;
                    }

                    if (delta_t_next < delta_t_min)
                    {
                        delta_t_min = delta_t_next;
                    }
                }

                // Clock oscillators.
                for (i = 0; i < 3; i++)
                {
                    voice[i].Wave.UpdateClock(delta_t_min);
                }

                // Synchronize oscillators.
                for (i = 0; i < 3; i++)
                {
                    voice[i].Wave.Synchronize();
                }

                delta_t_osc -= delta_t_min;
            }

            // Clock filter.
            filter.UpdateClock(delta_t,
                voice[0].Output(), voice[1].Output(), voice[2].Output());

            // Clock external filter.
            extfilt.UpdateClock(delta_t, filter.Output());
        }

        readonly Voice[] voice = new Voice[3];
        readonly Filter filter = new Filter();
        readonly ExternalFilter extfilt = new ExternalFilter();

        int bus_value;
        int bus_value_ttl;

        double clock_frequency;

        // Sampling variables.
        int cycles_per_sample;
        int sample_offset;
        short sample_prev;
    }
}

