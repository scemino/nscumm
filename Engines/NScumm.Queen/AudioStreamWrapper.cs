//
//  AudioStreamWrapper.cs
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

using System;
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;

namespace NScumm.Queen
{
    /// <summary>
    /// The sounds in the PC versions are all played at 11840 Hz. Unfortunately, we
    /// did not know that at the time, so there are plenty of compressed versions
    /// which claim that they should be played at 11025 Hz. This "wrapper" class
    /// works around that.
    /// </summary>
    class AudioStreamWrapper:IAudioStream
    {
        IAudioStream _stream;

        public int Rate { get; }

        public bool IsStereo
        {
            get
            {
                return _stream.IsStereo;
            }
        }

        public bool IsEndOfData
        {
            get
            {
                return _stream.IsEndOfData;
            }
        }

        public bool IsEndOfStream
        {
            get
            {
                return _stream.IsEndOfStream;
            }
        }

        public AudioStreamWrapper(IAudioStream stream)
        {
            _stream = stream;

            int rate = _stream.Rate;

            // A file where the sample rate claims to be 11025 Hz is
            // probably compressed with the old tool. We force the real
            // sample rate, which is 11840 Hz.
            //
            // However, a file compressed with the newer tool is not
            // guaranteed to have a sample rate of 11840 Hz. LAME will
            // automatically resample it to 12000 Hz. So in all other
            // cases, we use the rate from the file.

            if (rate == 11025)
                Rate = 11840;
            else
                Rate = rate;
        }

        public int ReadBuffer(short[] buffer, int numSamples)
        {
            return _stream.ReadBuffer(buffer, numSamples);
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
    
}
