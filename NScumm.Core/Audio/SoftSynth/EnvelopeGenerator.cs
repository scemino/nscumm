//
//  EnvelopeGenerator.cs
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
    class EnvelopeGenerator
    {
        public EnvelopeGenerator()
        {
            Reset();
        }

        enum State
        {
            ATTACK,
            DECAY_SUSTAIN,
            RELEASE
        }

        public void UpdateClock(int  delta_t)
        {
            // Check for ADSR delay bug.
            // If the rate counter comparison value is set below the current value of the
            // rate counter, the counter will continue counting up until it wraps around
            // to zero at 2^15 = 0x8000, and then count rate_period - 1 before the
            // envelope can finally be stepped.
            // This has been verified by sampling ENV3.
            //

            // NB! This requires two's complement integer.
            int rate_step = rate_period - rate_counter;
            if (rate_step <= 0)
            {
                rate_step += 0x7fff;
            }

            while (delta_t!=0)
            {
                if (delta_t < rate_step)
                {
                    rate_counter += delta_t;
                    if ((rate_counter & 0x8000)!=0)
                    {
                        rate_counter++;
                        rate_counter &= 0x7fff;
                    }
                    return;
                }

                rate_counter = 0;
                delta_t -= rate_step;

                // The first envelope step in the attack state also resets the exponential
                // counter. This has been verified by sampling ENV3.
                //
                if (state == State.ATTACK || ++exponential_counter == exponential_counter_period)
                {
                    exponential_counter = 0;

                    // Check whether the envelope counter is frozen at zero.
                    if (hold_zero)
                    {
                        rate_step = rate_period;
                        continue;
                    }

                    switch (state)
                    {
                        case State.ATTACK:
                            // The envelope counter can flip from 0xff to 0x00 by changing state to
                            // release, then to attack. The envelope counter is then frozen at
                            // zero; to unlock this situation the state must be changed to release,
                            // then to attack. This has been verified by sampling ENV3.
                            //
                            envelope_counter++;
                            envelope_counter &= 0xff;
                            if (envelope_counter == 0xff)
                            {
                                state = State.DECAY_SUSTAIN;
                                rate_period = rate_counter_period[decay];
                            }
                            break;
                        case State.DECAY_SUSTAIN:
                            if (envelope_counter != sustain_level[sustain])
                            {
                                --envelope_counter;
                            }
                            break;
                        case State.RELEASE:
                            // The envelope counter can flip from 0x00 to 0xff by changing state to
                            // attack, then to release. The envelope counter will then continue
                            // counting down in the release state.
                            // This has been verified by sampling ENV3.
                            // NB! The operation below requires two's complement integer.
                            //
                            envelope_counter--;
                            envelope_counter &= 0xff;
                            break;
                    }

                    // Check for change of exponential counter period.
                    switch (envelope_counter)
                    {
                        case 0xff:
                            exponential_counter_period = 1;
                            break;
                        case 0x5d:
                            exponential_counter_period = 2;
                            break;
                        case 0x36:
                            exponential_counter_period = 4;
                            break;
                        case 0x1a:
                            exponential_counter_period = 8;
                            break;
                        case 0x0e:
                            exponential_counter_period = 16;
                            break;
                        case 0x06:
                            exponential_counter_period = 30;
                            break;
                        case 0x00:
                            exponential_counter_period = 1;

                            // When the envelope counter is changed to zero, it is frozen at zero.
                            // This has been verified by sampling ENV3.
                            hold_zero = true;
                            break;
                    }
                }

                rate_step = rate_period;
            }
        }

        public void Reset()
        {
            envelope_counter = 0;

            attack = 0;
            decay = 0;
            sustain = 0;
            release = 0;

            gate = 0;

            rate_counter = 0;
            exponential_counter = 0;
            exponential_counter_period = 1;

            state = State.RELEASE;
            rate_period = rate_counter_period[release];
            hold_zero = true;
        }

        public void WriteCONTROL_REG(int control)
        {
            var gate_next = control & 0x01;

            // The rate counter is never reset, thus there will be a delay before the
            // envelope counter starts counting up (attack) or down (release).

            // Gate bit on: Start attack, decay, sustain.
            if (gate==0 && gate_next!=0)
            {
                state = State.ATTACK;
                rate_period = rate_counter_period[attack];

                // Switching to attack state unlocks the zero freeze.
                hold_zero = false;
            }
            // Gate bit off: Start release.
            else if (gate!=0 && gate_next==0)
            {
                state = State.RELEASE;
                rate_period = rate_counter_period[release];
            }

            gate = gate_next;
        }

        public void WriteATTACK_DECAY(int attack_decay)
        {
            attack = (attack_decay >> 4) & 0x0f;
            decay = attack_decay & 0x0f;
            if (state == State.ATTACK)
            {
                rate_period = rate_counter_period[attack];
            }
            else if (state == State.DECAY_SUSTAIN)
            {
                rate_period = rate_counter_period[decay];
            }
        }

        public void WriteSUSTAIN_RELEASE(int sustain_release)
        {
            sustain = (sustain_release >> 4) & 0x0f;
            release = sustain_release & 0x0f;
            if (state == State.RELEASE)
            {
                rate_period = rate_counter_period[release];
            }
        }

        public int ReadENV()
        {
            return Output();
        }

        // 8-bit envelope output.
        public int Output()
        {
            return envelope_counter;
        }

        int rate_counter;
        int rate_period;
        int exponential_counter;
        int exponential_counter_period;
        int envelope_counter;
        bool hold_zero;

        int attack;
        int decay;
        int sustain;
        int release;

        int gate;

        State state;

        // Lookup table to convert from attack, decay, or release value to rate
        // counter period.
        static readonly int[] rate_counter_period =
            {
            9,  //   2ms*1.0MHz/256 =     7.81
            32,  //   8ms*1.0MHz/256 =    31.25
            63,  //  16ms*1.0MHz/256 =    62.50
            95,  //  24ms*1.0MHz/256 =    93.75
            149,  //  38ms*1.0MHz/256 =   148.44
            220,  //  56ms*1.0MHz/256 =   218.75
            267,  //  68ms*1.0MHz/256 =   265.63
            313,  //  80ms*1.0MHz/256 =   312.50
            392,  // 100ms*1.0MHz/256 =   390.63
            977,  // 250ms*1.0MHz/256 =   976.56
            1954,  // 500ms*1.0MHz/256 =  1953.13
            3126,  // 800ms*1.0MHz/256 =  3125.00
            3907,  //   1 s*1.0MHz/256 =  3906.25
            11720,  //   3 s*1.0MHz/256 = 11718.75
            19532,  //   5 s*1.0MHz/256 = 19531.25
            31251   //   8 s*1.0MHz/256 = 31250.00
        };

        // The 16 selectable sustain levels.
        static readonly int[] sustain_level =
        {
            0x00,
            0x11,
            0x22,
            0x33,
            0x44,
            0x55,
            0x66,
            0x77,
            0x88,
            0x99,
            0xaa,
            0xbb,
            0xcc,
            0xdd,
            0xee,
            0xff,
        };
    }
    
}
