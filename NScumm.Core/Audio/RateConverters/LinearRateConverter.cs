//
//  LinearRateConverter.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

namespace NScumm.Core.Audio
{
    public class LinearRateConverter : IRateConverter
    {
        const int FRAC_BITS_LOW = 15;
        const int FRAC_ONE_LOW = (1 << FRAC_BITS_LOW);
        const int FRAC_HALF_LOW = (1 << (FRAC_BITS_LOW - 1));

        int opos;
        int oposInc;
        int ilast0;
        int ilast1;
        int icur0;
        int icur1;

        int inLen;
        bool stereo;
        bool reverseStereo;
        short[] inBuf;

        public LinearRateConverter(int inrate, int outrate, bool stereo, bool reverseStereo)
        {
            this.stereo = stereo;
            this.reverseStereo = reverseStereo;

            if (inrate >= 131072 || outrate >= 131072)
            {
                throw new ArgumentOutOfRangeException("rate effect can only handle rates < 131072");
            }
            

            opos = FRAC_ONE_LOW;
            inBuf = new short[RateHelper.IntermediateBufferSize];

            // Compute the linear interpolation increment.
            // This will overflow if inrate >= 2^17, and underflow if outrate >= 2^17.
            // Also, if the quotient of the two rate becomes too small / too big, that
            // would cause problems, but since we rarely scale from 1 to 65536 Hz or vice
            // versa, I think we can live with that limitation ;-).
            oposInc = (inrate << FRAC_BITS_LOW) / outrate;

            ilast0 = ilast1 = 0;
            icur0 = icur1 = 0;

            inLen = 0;
        }

        public int Flow(IAudioStream input, short[] obuf, int count, int volLeft, int volRight)
        {
            var obufPos = 0;
            var inPos = 0;
            var oend = count;

            while (obufPos < oend)
            {
                // read enough input samples so that opos < 0
                while (FRAC_ONE_LOW <= opos)
                {
                    // Check if we have to refill the buffer
                    if (inLen == 0)
                    {
                        inPos = 0;
                        inLen = input.ReadBuffer(inBuf, RateHelper.IntermediateBufferSize);
                        if (inLen <= 0)
                            return obufPos / 2;
                    }
                    inLen -= (stereo ? 2 : 1);
                    ilast0 = icur0;
                    icur0 = inBuf[inPos++];
                    if (stereo)
                    {
                        ilast1 = icur1;
                        icur1 = inBuf[inPos++];
                    }
                    opos -= FRAC_ONE_LOW;
                }

                // Loop as long as the outpos trails behind, and as long as there is
                // still space in the output buffer.
                while (opos < FRAC_ONE_LOW && obufPos < oend)
                {
                    // interpolate
                    int out0, out1;
                    out0 = (short)(ilast0 + (((icur0 - ilast0) * opos + FRAC_HALF_LOW) >> FRAC_BITS_LOW));
                    out1 = stereo ? (short)(ilast1 + (((icur1 - ilast1) * opos + FRAC_HALF_LOW) >> FRAC_BITS_LOW)) : out0;

                    // output left channel
                    RateHelper.ClampedAdd(ref obuf[obufPos + (reverseStereo ? 1 : 0)], (out0 * volLeft) / Mixer.MaxMixerVolume);

                    // output right channel
                    RateHelper.ClampedAdd(ref obuf[obufPos + (reverseStereo ? 0 : 1)], (out1 * volRight) / Mixer.MaxMixerVolume);

                    obufPos += 2;

                    // Increment output position
                    opos += oposInc;
                }
            }
            return obufPos / 2;

        }

        public int Drain(short[] obuf, int vol)
        {
            return 0;
        }
    }
}

