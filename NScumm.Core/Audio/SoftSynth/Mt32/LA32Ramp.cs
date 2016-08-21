//
//  LA32Ramp.cs
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
    class LA32Ramp
    {
        // SEMI-CONFIRMED from sample analysis.
        const int TARGET_MULT = 0x40000;
        const int MAX_CURRENT = 0xFF * TARGET_MULT;

        // We simulate the delay in handling "target was reached" interrupts by waiting
        // this many samples before setting interruptRaised.
        // FIXME: This should vary with the sample rate, but doesn't.
        // SEMI-CONFIRMED: Since this involves asynchronous activity between the LA32
        // and the 8095, a good value is hard to pin down.
        // This one matches observed behaviour on a few digital captures I had handy,
        // and should be double-checked. We may also need a more sophisticated delay
        // scheme eventually.
        const int INTERRUPT_TIME = 7;

        private int current;
        private int largeTarget;
        private int largeIncrement;
        private bool descending;

        private int interruptCountdown;
        private bool interruptRaised;

        public void StartRamp(byte target, byte increment)
        {
            // CONFIRMED: From sample analysis, this appears to be very accurate.
            if (increment == 0)
            {
                largeIncrement = 0;
            }
            else {
                // Three bits in the fractional part, no need to interpolate
                // (unsigned int)(EXP2F(((increment & 0x7F) + 24) / 8.0f) + 0.125f)
                int expArg = increment & 0x7F;
                largeIncrement = 8191 - Tables.Instance.exp9[~(expArg << 6) & 511];
                largeIncrement <<= expArg >> 3;
                largeIncrement += 64;
                largeIncrement >>= 9;
            }
            descending = (increment & 0x80) != 0;
            if (descending)
            {
                // CONFIRMED: From sample analysis, descending increments are slightly faster
                largeIncrement++;
            }

            largeTarget = target * TARGET_MULT;
            interruptCountdown = 0;
            interruptRaised = false;
        }

        public void Reset()
        {
            current = 0;
            largeTarget = 0;
            largeIncrement = 0;
            descending = false;
            interruptCountdown = 0;
            interruptRaised = false;
        }

        public int NextValue()
        {
            if (interruptCountdown > 0)
            {
                if (--interruptCountdown == 0)
                {
                    interruptRaised = true;
                }
            }
            else if (largeIncrement != 0)
            {
                // CONFIRMED from sample analysis: When increment is 0, the LA32 does *not* change the current value at all (and of course doesn't fire an interrupt).
                if (descending)
                {
                    // Lowering current value
                    if (largeIncrement > current)
                    {
                        current = largeTarget;
                        interruptCountdown = INTERRUPT_TIME;
                    }
                    else {
                        current -= largeIncrement;
                        if (current <= largeTarget)
                        {
                            current = largeTarget;
                            interruptCountdown = INTERRUPT_TIME;
                        }
                    }
                }
                else {
                    // Raising current value
                    if (MAX_CURRENT - current < largeIncrement)
                    {
                        current = largeTarget;
                        interruptCountdown = INTERRUPT_TIME;
                    }
                    else {
                        current += largeIncrement;
                        if (current >= largeTarget)
                        {
                            current = largeTarget;
                            interruptCountdown = INTERRUPT_TIME;
                        }
                    }
                }
            }
            return current;
        }

        public bool CheckInterrupt()
        {
            bool wasRaised = interruptRaised;
            interruptRaised = false;
            return wasRaised;
        }
   }
}
