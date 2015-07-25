//
//  SimpleRateConverter.cs
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
    class SimpleRateConverter : IRateConverter
    {
        /// <summary>
        /// The size of the intermediate input cache. Bigger values may increase
        /// performance, but only until some point (depends largely on cache size,
        /// target processor and various other factors), at which it will decrease
        /// again.
        /// </summary>
        short[] inBuf = new short[RateHelper.IntermediateBufferSize];
        int inPtr;
        int inLen;

        /** position of how far output is ahead of input */
        /** Holds what would have been opos-ipos */
        long opos;

        /** fractional position increment in the output stream */
        long oposInc;
        bool stereo;
        bool reverseStereo;

        public SimpleRateConverter(int inrate, int outrate, bool stereo, bool reverseStereo)
        {
            this.stereo = stereo;
            this.reverseStereo = reverseStereo;

            if ((inrate % outrate) != 0)
            {
                throw new ArgumentException("Input rate must be a multiple of output rate to use rate effect", "inrate");
            }

            if (inrate >= 65536)
            {
                throw new ArgumentOutOfRangeException("inrate", "rate effect can only handle rates < 65536");
            }
            if (outrate >= 65536)
            {
                throw new ArgumentOutOfRangeException("outrate", "rate effect can only handle rates < 65536");
            }

            opos = 1;

            /* increment */
            oposInc = inrate / outrate;

            inLen = 0;
        }

        public int Flow(IAudioStream input, short[] obuf, int count, int volLeft, int volRight)
        {
            int pos = 0;
            int oend = count * 2;

            while (pos < oend)
            {
                // read enough input samples so that opos >= 0
                do
                {
                    // Check if we have to refill the buffer
                    if (inLen == 0)
                    {
                        inPtr = 0;
                        inLen = input.ReadBuffer(inBuf, RateHelper.IntermediateBufferSize);
                        if (inLen <= 0)
                            return pos / 2;
                    }
                    inLen -= (stereo ? 2 : 1);
                    opos--;
                    if (opos >= 0)
                    {
                        inPtr += (stereo ? 2 : 1);
                    }
                } while (opos >= 0);

                short out0, out1;
                out0 = inBuf[inPtr++];
                out1 = (stereo ? inBuf[inPtr++] : out0);

                // Increment output position
                opos += oposInc;

                // output left channel
                RateHelper.ClampedAdd(ref obuf[reverseStereo ? 1 : 0], (out0 * (int)volLeft) / Mixer.MaxMixerVolume);

                // output right channel
                RateHelper.ClampedAdd(ref obuf[(reverseStereo ? 1 : 0) ^ 1], (out1 * (int)volRight) / Mixer.MaxMixerVolume);

                pos += 2;
            }
            return pos / 2;
        }

        public int Drain(short[] obuf, int vol)
        {
            return 0;
        }
    }
}
