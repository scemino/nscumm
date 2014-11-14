//
//  CopyRateConverter.cs
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
using System.Diagnostics;

namespace NScumm.Core
{
    class CopyRateConverter: IRateConverter
    {
        bool stereo;
        bool reverseStereo;
        short[] _buffer;
        int _bufferSize;

        public CopyRateConverter(bool stereo, bool reverseStereo)
        { 
            this.stereo = stereo;
            this.reverseStereo = reverseStereo;
            _bufferSize = 0; 
        }

        public int Flow(IMixerAudioStream input, short[] obuf, int volLeft, int volRight)
        {
            Debug.Assert(input.IsStereo == stereo);

            int pos = 0;
            var osamp = obuf.Length;

            if (stereo)
                osamp *= 2;

            // Reallocate temp buffer, if necessary
            if (osamp > _bufferSize)
            {
                _buffer = new short[osamp * 2];
                _bufferSize = osamp;
            }

            // Read up to 'osamp' samples into our temporary buffer
            var len = input.ReadBuffer(_buffer);

            // Mix the data into the output buffer
            for (; len > 0; len -= (stereo ? 2 : 1))
            {
                short out0, out1;
                out0 = _buffer[pos++];
                out1 = (stereo ? _buffer[pos++] : out0);

                // output left channel
                RateHelper.ClampedAdd(ref obuf[reverseStereo ? 1 : 0], (out0 * (int)volLeft) / Mixer.MaxMixerVolume);

                // output right channel
                RateHelper.ClampedAdd(ref obuf[(reverseStereo ? 1 : 0) ^ 1], (out1 * (int)volRight) / Mixer.MaxMixerVolume);
            }
            return pos / 2;
        }

        public int Drain(short[] obuf, int vol)
        {
            return 0;
        }
    }
    
}
