//
//  CMSEmulator.cs
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
using System.Diagnostics;

namespace NScumm.Core.Audio.SoftSynth
{
    class saa1099_channel
    {
        public int frequency;
        /* frequency (0x00..0xff) */
        public int freq_enable;
        /* frequency enable */
        public int noise_enable;
        /* noise enable */
        public int octave;
        /* octave (0x00..0x07) */
        public int[] amplitude = new int[2];
        /* amplitude (0x00..0x0f) */
        public int[] envelope = new int[2];
        /* envelope (0x00..0x0f or 0x10 == off) */

        /* vars to simulate the square wave */
        public double counter;
        public double freq;
        public int level;
    }

    /* this structure defines a noise channel */
    struct saa1099_noise
    {
        /* vars to simulate the noise generator output */
        public double counter;
        public double freq;
        public int level;
        /* noise polynomal shifter */
    }

    /* this structure defines a SAA1099 chip */
    class SAA1099
    {
        public int stream;
        /* our stream */
        public int[] noise_params = new int[2];
        /* noise generators parameters */
        public int[] env_enable = new int[2];
        /* envelope generators enable */
        public int[] env_reverse_right = new int[2];
        /* envelope reversed for right channel */
        public int[] env_mode = new int[2];
        /* envelope generators mode */
        public int[] env_bits = new int[2];
        /* non zero = 3 bits resolution */
        public int[] env_clock = new int[2];
        /* envelope clock mode (non-zero external) */
        public int[] env_step = new int[2];
        /* current envelope step */
        public int all_ch_enable;
        /* all channels enable */
        public int sync_state;
        /* sync all channels */
        public int selected_reg;
        /* selected register */
        public saa1099_channel[] channels;
        /* channels */
        public saa1099_noise[] noise;
        /* noise generators */

        public SAA1099()
        {
            channels = new saa1099_channel[6];
            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = new saa1099_channel();
            }

            noise = new saa1099_noise[2];
            for (int i = 0; i < noise.Length; i++)
            {
                noise[i] = new saa1099_noise();
            }
        }
    }

    public class CMSEmulator
    {
        public int SampleRate { get; private set; }

        public CMSEmulator(int sampleRate)
        {
            SampleRate = sampleRate;
        }

        public void ReadBuffer(short[] buffer)
        {
            Update(0, buffer);
            Update(1, buffer);
        }

        public void PortWrite(int port, int val)
        {
            switch (port)
            {
                case 0x220:
                    PortWriteIntern(0, val);
                    break;

                case 0x221:
                    _saa1099[0].selected_reg = val & 0x1f;
                    if (_saa1099[0].selected_reg == 0x18 || _saa1099[0].selected_reg == 0x19)
                    {
                        /* clock the envelope channels */
                        if (_saa1099[0].env_clock[0] != 0)
                            Envelope(0, 0);
                        if (_saa1099[0].env_clock[1] != 0)
                            Envelope(0, 1);
                    }
                    break;

                case 0x222:
                    PortWriteIntern(1, val);
                    break;

                case 0x223:
                    _saa1099[1].selected_reg = val & 0x1f;
                    if (_saa1099[1].selected_reg == 0x18 || _saa1099[1].selected_reg == 0x19)
                    {
                        /* clock the envelope channels */
                        if (_saa1099[1].env_clock[0] != 0)
                            Envelope(1, 0);
                        if (_saa1099[1].env_clock[1] != 0)
                            Envelope(1, 1);
                    }
                    break;

                default:
                    Debug.WriteLine("CMSEmulator got port: 0x{0:X}", port);
                    break;
            }
        }

        void PortWriteIntern(int chip, int data)
        {
            var saa = _saa1099[chip];
            int reg = saa.selected_reg;
            int ch;

            switch (reg)
            {
            /* channel i amplitude */
                case 0x00:
                case 0x01:
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                    ch = reg & 7;
                    saa.channels[ch].amplitude[Left] = amplitude_lookup[data & 0x0f];
                    saa.channels[ch].amplitude[Right] = amplitude_lookup[(data >> 4) & 0x0f];
                    break;

            /* channel i frequency */
                case 0x08:
                case 0x09:
                case 0x0a:
                case 0x0b:
                case 0x0c:
                case 0x0d:
                    ch = reg & 7;
                    saa.channels[ch].frequency = data & 0xff;
                    break;

            /* channel i octave */
                case 0x10:
                case 0x11:
                case 0x12:
                    ch = (reg - 0x10) << 1;
                    saa.channels[ch + 0].octave = data & 0x07;
                    saa.channels[ch + 1].octave = (data >> 4) & 0x07;
                    break;

            /* channel i frequency enable */
                case 0x14:
                    saa.channels[0].freq_enable = data & 0x01;
                    saa.channels[1].freq_enable = data & 0x02;
                    saa.channels[2].freq_enable = data & 0x04;
                    saa.channels[3].freq_enable = data & 0x08;
                    saa.channels[4].freq_enable = data & 0x10;
                    saa.channels[5].freq_enable = data & 0x20;
                    break;

            /* channel i noise enable */
                case 0x15:
                    saa.channels[0].noise_enable = data & 0x01;
                    saa.channels[1].noise_enable = data & 0x02;
                    saa.channels[2].noise_enable = data & 0x04;
                    saa.channels[3].noise_enable = data & 0x08;
                    saa.channels[4].noise_enable = data & 0x10;
                    saa.channels[5].noise_enable = data & 0x20;
                    break;

            /* noise generators parameters */
                case 0x16:
                    saa.noise_params[0] = data & 0x03;
                    saa.noise_params[1] = (data >> 4) & 0x03;
                    break;

            /* envelope generators parameters */
                case 0x18:
                case 0x19:
                    ch = reg - 0x18;
                    saa.env_reverse_right[ch] = data & 0x01;
                    saa.env_mode[ch] = (data >> 1) & 0x07;
                    saa.env_bits[ch] = data & 0x10;
                    saa.env_clock[ch] = data & 0x20;
                    saa.env_enable[ch] = data & 0x80;
                    /* reset the envelope */
                    saa.env_step[ch] = 0;
                    break;

            /* channels enable & reset generators */
                case 0x1c:
                    saa.all_ch_enable = data & 0x01;
                    saa.sync_state = data & 0x02;
                    if ((data & 0x02) != 0)
                    {
                        int i;
                        /* Synch & Reset generators */
                        for (i = 0; i < 6; i++)
                        {
                            saa.channels[i].level = 0;
                            saa.channels[i].counter = 0.0;
                        }
                    }
                    break;

                default:
                    // The CMS allows all registers to be written, so we just output some debug
                    // message here
                    Debug.WriteLine("CMS Unknown write to reg {0:X} with {1:X}", reg, data);
                    break;
            }
        }

        void Envelope(int chip, int ch)
        {
            var saa = _saa1099[chip];
            if (saa.env_enable[ch] != 0)
            {
                int step, mode, mask;
                mode = saa.env_mode[ch];
                /* step from 0..63 and then loop in steps 32..63 */
                step = saa.env_step[ch] = ((saa.env_step[ch] + 1) & 0x3f) | (saa.env_step[ch] & 0x20);

                mask = 15;
                if (saa.env_bits[ch] != 0)
                    mask &= ~1;     /* 3 bit resolution, mask LSB */

                saa.channels[ch * 3 + 0].envelope[Left] =
                    saa.channels[ch * 3 + 1].envelope[Left] =
                        saa.channels[ch * 3 + 2].envelope[Left] = envelope[mode, step] & mask;
                if ((saa.env_reverse_right[ch] & 0x01) != 0)
                {
                    saa.channels[ch * 3 + 0].envelope[Right] =
                        saa.channels[ch * 3 + 1].envelope[Right] =
                            saa.channels[ch * 3 + 2].envelope[Right] = (15 - envelope[mode, step]) & mask;
                }
                else
                {
                    saa.channels[ch * 3 + 0].envelope[Right] =
                        saa.channels[ch * 3 + 1].envelope[Right] =
                            saa.channels[ch * 3 + 2].envelope[Right] = envelope[mode, step] & mask;
                }
            }
            else
            {
                /* envelope mode off, set all envelope factors to 16 */
                saa.channels[ch * 3 + 0].envelope[Left] =
                    saa.channels[ch * 3 + 1].envelope[Left] =
                        saa.channels[ch * 3 + 2].envelope[Left] =
                            saa.channels[ch * 3 + 0].envelope[Right] =
                                saa.channels[ch * 3 + 1].envelope[Right] =
                                    saa.channels[ch * 3 + 2].envelope[Right] = 16;
            }
        }

        void Update(int chip, short[] buffer)
        {
            var saa = _saa1099[chip];
            int j, ch;

            if (chip == 0)
            {
                Array.Clear(buffer, 0, buffer.Length);
            }

            /* if the channels are disabled we're done */
            if (saa.all_ch_enable == 0)
            {
                return;
            }

            for (ch = 0; ch < 2; ch++)
            {
                switch (saa.noise_params[ch])
                {
                    case 0:
                        saa.noise[ch].freq = 31250.0 * 2;
                        break;
                    case 1:
                        saa.noise[ch].freq = 15625.0 * 2;
                        break;
                    case 2:
                        saa.noise[ch].freq = 7812.5 * 2;
                        break;
                    case 3:
                        saa.noise[ch].freq = saa.channels[ch * 3].freq;
                        break;
                }
            }

            /* fill all data needed */
            for (j = 0; j < buffer.Length / 2; ++j)
            {
                int output_l = 0, output_r = 0;

                /* for each channel */
                for (ch = 0; ch < 6; ch++)
                {
                    if (saa.channels[ch].freq == 0.0)
                        saa.channels[ch].freq = (double)((2 * 15625) << saa.channels[ch].octave) /
                        (511.0 - (double)saa.channels[ch].frequency);

                    /* check the actual position in the square wave */
                    saa.channels[ch].counter -= saa.channels[ch].freq;
                    while (saa.channels[ch].counter < 0)
                    {
                        /* calculate new frequency now after the half wave is updated */
                        saa.channels[ch].freq = (double)((2 * 15625) << saa.channels[ch].octave) /
                        (511.0 - (double)saa.channels[ch].frequency);

                        saa.channels[ch].counter += SampleRate;
                        saa.channels[ch].level ^= 1;

                        /* eventually clock the envelope counters */
                        if (ch == 1 && saa.env_clock[0] == 0)
                            Envelope(chip, 0);
                        if (ch == 4 && saa.env_clock[1] == 0)
                            Envelope(chip, 1);
                    }

                    /* if the noise is enabled */
                    if (saa.channels[ch].noise_enable != 0)
                    {
                        /* if the noise level is high (noise 0: chan 0-2, noise 1: chan 3-5) */
                        if ((saa.noise[ch / 3].level & 1) != 0)
                        {
                            /* subtract to avoid overflows, also use only half amplitude */
                            output_l -= saa.channels[ch].amplitude[Left] * saa.channels[ch].envelope[Left] / 16 / 2;
                            output_r -= saa.channels[ch].amplitude[Right] * saa.channels[ch].envelope[Right] / 16 / 2;
                        }
                    }

                    /* if the square wave is enabled */
                    if (saa.channels[ch].freq_enable != 0)
                    {
                        /* if the channel level is high */
                        if ((saa.channels[ch].level & 1) != 0)
                        {
                            output_l += saa.channels[ch].amplitude[Left] * saa.channels[ch].envelope[Left] / 16;
                            output_r += saa.channels[ch].amplitude[Right] * saa.channels[ch].envelope[Right] / 16;
                        }
                    }
                }

                for (ch = 0; ch < 2; ch++)
                {
                    /* check the actual position in noise generator */
                    saa.noise[ch].counter -= saa.noise[ch].freq;
                    while (saa.noise[ch].counter < 0)
                    {
                        saa.noise[ch].counter += SampleRate;
                        if (((saa.noise[ch].level & 0x4000) == 0) == ((saa.noise[ch].level & 0x0040) == 0))
                            saa.noise[ch].level = (saa.noise[ch].level << 1) | 1;
                        else
                            saa.noise[ch].level <<= 1;
                    }
                }
                /* write sound data to the buffer */
                buffer[j * 2 + 0] = (short)ScummHelper.Clip(buffer[j * 2 + 0] + output_l / 6, -32768, 32767);
                buffer[j * 2 + 1] = (short)ScummHelper.Clip(buffer[j * 2 + 1] + output_r / 6, -32768, 32767);
            }
        }

        readonly SAA1099[] _saa1099 = CreateSAA1099();

        static SAA1099[] CreateSAA1099()
        {
            var saa1099 = new SAA1099[2];
            for (int i = 0; i < saa1099.Length; i++)
            {
                saa1099[i] = new SAA1099();
            }
            return saa1099;
        }

        const int Left = 0x00;
        const int Right = 0x01;

        static readonly byte[,] envelope = new byte[8, 64]
        {
            /* zero amplitude */
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            },
            /* maximum amplitude */
            {15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
                15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
                15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
                15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15
            },
            /* single decay */
            {15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            },
            /* repetitive decay */
            {15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
                15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
                15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
                15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0
            },
            /* single triangular */
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            },
            /* repetitive triangular */
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0
            },
            /* single attack */
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            },
            /* repetitive attack */
            { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15
            }
        };

        static readonly int[] amplitude_lookup =
            {
                0 * 32767 / 16,  1 * 32767 / 16,  2 * 32767 / 16,   3 * 32767 / 16,
                4 * 32767 / 16,  5 * 32767 / 16,  6 * 32767 / 16,   7 * 32767 / 16,
                8 * 32767 / 16,  9 * 32767 / 16, 10 * 32767 / 16, 11 * 32767 / 16,
                12 * 32767 / 16, 13 * 32767 / 16, 14 * 32767 / 16, 15 * 32767 / 16
            };
    }
}

