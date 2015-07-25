//
//  AudioFormat.cs
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

namespace NScumm.Core.Audio
{
    public struct AudioFormat
    {
        readonly int _channels;
        readonly int _sampleRate;
        readonly int _bitsPerSample;
        readonly int _blockAlign;
        readonly int _averageBytesPerSecond;

        /// <summary>
        /// Gets the number of channels.
        /// </summary>
        public int Channels
        {
            get
            {
                return _channels;
            }
        }

        /// <summary>
        /// Gets the sample rate.
        /// </summary>
        /// <description>>Number of samples per second.</description>
        public int SampleRate
        {
            get
            {
                return _sampleRate;
            }
        }

        /// <summary>
        /// Gets the average number of bytes used per second.
        /// </summary>
        public int AverageBytesPerSecond
        {
            get
            {
                return _averageBytesPerSecond;
            }
        }

        /// <summary>
        /// Gets the block alignment.
        /// </summary>
        public int BlockAlign
        {
            get
            {
                return _blockAlign;
            }
        }

        /// <summary>
        /// Gets the number of bits per sample.
        /// </summary>
        public int BitsPerSample
        {
            get
            {
                return _bitsPerSample;
            }
        }

        public AudioFormat(int rate = 44100, int channels = 2, int bits = 16)
        {
            if (channels < 1)
            {
                throw new ArgumentOutOfRangeException("channels", "Channels must be 1 or greater");
            }
            _channels = channels;
            _sampleRate = rate;
            _bitsPerSample = bits;
        
            _blockAlign = channels * (bits / 8);
            _averageBytesPerSecond = _sampleRate * _blockAlign;
        }
    }
    
}
