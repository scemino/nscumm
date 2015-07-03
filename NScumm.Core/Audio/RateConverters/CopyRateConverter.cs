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

using System.Diagnostics;

namespace NScumm.Core.Audio
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

        public int Flow(IAudioStream input, short[] obuf, int volLeft, int volRight)
        {
            Debug.Assert(input.IsStereo == stereo);

            var osamp = obuf.Length / 2;

            if (stereo)
                osamp *= 2;

            // Reallocate temp buffer, if necessary
            if (osamp > _bufferSize)
            {
                _buffer = new short[osamp];
                _bufferSize = osamp;
            }

            // Read up to 'osamp' samples into our temporary buffer
            var len = input.ReadBuffer(_buffer);

            int iPos = 0;
            var oPos = 0;
            var inc = stereo ? 2 : 1;
            // Mix the data into the output buffer
            for (; iPos < len; iPos += inc)
            {
                var out0 = _buffer[iPos];
                var out1 = stereo ? _buffer[iPos + 1] : out0;

                // output left channel
                RateHelper.ClampedAdd(ref obuf[oPos + (reverseStereo ? 1 : 0)], (out0 * volLeft) / Mixer.MaxMixerVolume);

                // output right channel
                RateHelper.ClampedAdd(ref obuf[oPos + (reverseStereo ? 0 : 1)], (out1 * volRight) / Mixer.MaxMixerVolume);

                oPos += 2;
            }
            return oPos / 2;
        }

        public int Drain(short[] obuf, int vol)
        {
            return 0;
        }
    }
    
}
