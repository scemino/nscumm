//
//  IMixerAudioStream.cs
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
    /// <summary>
    /// Generic audio input stream. Subclasses of this are used to feed arbitrary
    /// sampled audio data into ScummVM's audio mixer.
    /// </summary>
    public interface IMixerAudioStream: IDisposable
    {
        /// <summary>
        /// Fill the given buffer with up to numSamples samples. Returns the actual
        /// number of samples read, or -1 if a critical error occurred (note: you
        /// *must* check if this value is less than what you requested, this can
        /// happen when the stream is fully used up).
        /// 
        /// Data has to be in native endianess, 16 bit per sample, signed. For stereo
        /// stream, buffer will be filled with interleaved left and right channel
        /// samples, starting with a left sample. Furthermore, the samples in the
        /// left and right are summed up. So if you request 4 samples from a stereo
        /// stream, you will get a total of two left channel and two right channel
        /// samples.
        /// </summary>
        /// <returns>The buffer.</returns>
        /// <param name="buffer">Buffer.</param>
        int ReadBuffer(short[] buffer);

        /// <summary>
        /// Is this a stereo stream?
        /// </summary>
        /// <value><c>true</c> if this instance is stereo; otherwise, <c>false</c>.</value>
        bool IsStereo { get; }

        /// <summary>
        /// Sample rate of the stream.
        /// </summary>
        /// <value>The rate.</value>
        int Rate { get; }

        /// <summary>
        /// End of data reached? If this returns true, it means that at this
        /// time there is no data available in the stream. However there may be
        /// more data in the future.
        /// This is used by e.g. a rate converter to decide whether to keep on
        /// converting data or stop.
        /// </summary>
        /// <value><c>true</c> if this instance is end of data; otherwise, <c>false</c>.</value>
        bool IsEndOfData { get; }

        /// <summary>
        /// End of stream reached? If this returns true, it means that all data
        /// in this stream is used up and no additional data will appear in it
        /// in the future.
        /// This is used by the mixer to decide whether a given stream shall be
        /// removed from the list of active streams (and thus be destroyed).
        /// By default this maps to endOfData()
        /// </summary>
        /// <returns><c>true</c> if this instance is end of stream; otherwise, <c>false</c>.</returns>
        bool IsEndOfStream { get; }
    }
}

