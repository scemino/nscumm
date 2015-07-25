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
    /// Various flags which can be bit-ORed and then passed to
    /// makeRawStream and some other AudioStream factories
    /// to control their behavior.
    ///
    /// Engine authors are advised not to rely on a certain value or
    /// order of these flags (in particular, do not store them verbatim
    /// in savestates).
    /// </summary>
    [Flags]
    public enum AudioFlags
    {
        None,

        /** unsigned samples (default: signed) */
        Unsigned = 1 << 0,

        /** sound is 16 bits wide (default: 8bit) */
        Is16Bits = 1 << 1,

        /** samples are little endian (default: big endian) */
        LittleEndian = 1 << 2,

        /** sound is in stereo (default: mono) */
        Stereo = 1 << 3
    }

    /// <summary>
    /// Generic audio input stream. Subclasses of this are used to feed arbitrary
    /// sampled audio data into ScummVM's audio mixer.
    /// </summary>
    public interface IAudioStream: IDisposable
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
        /// <param name="buffer">Buffer.</param>
        /// <param name="numSamples">The number of samples to read.</param>
        /// <returns>The number of samples read.</returns>
        int ReadBuffer(short[] buffer, int numSamples);

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

    /// <summary>
    /// A rewindable audio stream. This allows for reseting the AudioStream
    /// to its initial state. Note that rewinding itself is not required to
    /// be working when the stream is being played by Mixer!
    /// </summary>
    public interface IRewindableAudioStream: IAudioStream
    {
        /// <summary>
        /// Rewinds the stream to its start.
        /// </summary>
        /// <returns>true on success, false otherwise.</returns>
        bool Rewind();
    }

    /// <summary>
    /// A seekable audio stream. Subclasses of this class implement an
    /// interface for seeking. The seeking itself is not required to be
    /// working while the stream is being played by Mixer!
    /// </summary>
    public interface ISeekableAudioStream : IRewindableAudioStream
    {
        /// <summary>
        /// Seeks to a given offset in the stream.
        /// </summary>
        /// <param name="where">where offset as timestamp.</param>
        bool Seek(Timestamp where);

        /// <summary>
        /// Gets the length of the stream.
        /// </summary>
        /// <value>The length as Timestamp.</value>
        Timestamp Length { get; }
    }

    public static class SeekableAudioStreamExtensions
    {
        public static bool Seek(this ISeekableAudioStream stream, int where)
        {
            return stream.Seek(new Timestamp(where, stream.Rate));
        }
    }
}

