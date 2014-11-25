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
    public class LinearRateConverter:IRateConverter
    {
        const int FracBits = 16;
        const long FracOne = (1L << FracBits);
        const long FracHalf = (1L << (FracBits - 1));

        long opos;
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

            if (inrate >= 65536)
            {
                throw new ArgumentOutOfRangeException("inrate", inrate, "rate effect can only handle rates < 65536");
            }
            if (outrate >= 65536)
            {
                throw new ArgumentOutOfRangeException("outrate", outrate, "rate effect can only handle rates < 65536");
            }

            opos = FracOne;
            inBuf = new short[RateHelper.IntermediateBufferSize];
            // Compute the linear interpolation increment.
            // This will overflow if inrate >= 2^16, and underflow if outrate >= 2^16.
            // Also, if the quotient of the two rate becomes too small / too big, that
            // would cause problems, but since we rarely scale from 1 to 65536 Hz or vice
            // versa, I think we can live with that limitation ;-).
            oposInc = (inrate << FracBits) / outrate;

            ilast0 = ilast1 = 0;
            icur0 = icur1 = 0;

            inLen = 0;
        }

        public int Flow(IMixerAudioStream input, short[] obuf, int volLeft, int volRight)
        {
            var obufPos = 0;
            var inPos = 0;
            var oend = obuf.Length;

            while (obufPos < oend)
            {
                // read enough input samples so that opos < 0
                while (FracOne <= opos)
                {
                    // Check if we have to refill the buffer
                    if (inLen == 0)
                    {
                        inPos = 0;
                        inLen = input.ReadBuffer(inBuf);
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
                    opos -= FracOne;
                }

                // Loop as long as the outpos trails behind, and as long as there is
                // still space in the output buffer.
                while (opos < FracOne && obufPos < oend)
                {
                    // interpolate
                    short out0, out1;
                    out0 = (short)(ilast0 + (((icur0 - ilast0) * opos + FracHalf) >> FracBits));
                    out1 = stereo ? (short)(ilast1 + (((icur1 - ilast1) * opos + FracHalf) >> FracBits)) : out0;

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

