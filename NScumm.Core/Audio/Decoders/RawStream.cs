//
//  RawStream.cs
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
using System.IO;

namespace NScumm.Core.Audio.Decoders
{
    /// <summary>
    /// This is a stream, which allows for playing raw PCM data from a stream.
    /// </summary>
    public class RawStream: ISeekableAudioStream
    {
        public RawStream(AudioFlags flags, int rate, bool disposeStream, Stream stream)
        {
            _is16Bit = flags.HasFlag(AudioFlags.Is16Bits);
            _isLE = flags.HasFlag(AudioFlags.LittleEndian);
            _isUnsigned = flags.HasFlag(AudioFlags.Unsigned);
            _rate = rate;
            _isStereo = flags.HasFlag(AudioFlags.Stereo);
            _playtime = new Timestamp(0, rate);
            _stream = stream;
            _disposeStream = disposeStream;

            // Setup our buffer for readBuffer
            _buffer = new byte[SampleBufferLength * (_is16Bit ? 2 : 1)];

            // Calculate the total playtime of the stream
            _playtime = new Timestamp(0, (int)_stream.Length / (_isStereo ? 2 : 1) / (_is16Bit ? 2 : 1), rate);
        }

        public bool IsStereo
        {
            get { return _isStereo; }
        }

        public int Rate
        {
            get { return _rate; }
        }

        public bool IsEndOfData
        {
            get { return _endOfData; }
        }

        public bool IsEndOfStream
        {
            get { return IsEndOfData; }
        }

        public Timestamp Length { get { return _playtime; } }

        public bool Seek(Timestamp wh)
        {
            _endOfData = true;

            if (wh > _playtime)
                return false;

            var seekSample = AudioStreamHelper.ConvertTimeToStreamPos(wh, Rate, IsStereo).TotalNumberOfFrames;
            _stream.Seek(seekSample * (_is16Bit ? 2 : 1), SeekOrigin.Begin);

            // In case of an error we will not continue stream playback.
            if (_stream.Position != _stream.Length)
                _endOfData = false;

            return true;
        }

        public int ReadBuffer(short[] buffer, int count)
        {
            int samplesLeft = count;

            while (samplesLeft > 0)
            {
                // Try to read up to "samplesLeft" samples.
                int len = FillBuffer(samplesLeft);

                // In case we were not able to read any samples
                // we will stop reading here.
                if (len == 0)
                    break;

                // Adjust the samples left to read.
                samplesLeft -= len;

                // Copy the data to the caller's buffer.
                int srcPos = 0;
                int dstPos = 0;
                while (len-- > 0)
                {
                    buffer[dstPos++] = ReadEndianSample(_buffer, srcPos);
                    srcPos += (_is16Bit ? 2 : 1);
                }
            }

            return count - samplesLeft;
        }

        ~RawStream()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _buffer = null;
                if (_disposeStream && _stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }
            }
        }

        public bool Rewind()
        {
            return Seek(new Timestamp(0, Rate));
        }

        short ReadEndianSample(byte[] ptr, int offset)
        {
            ushort value;
            if (_is16Bit)
            {
                value = _isLE ? BitConverter.ToUInt16(ptr, offset) : ScummHelper.SwapBytes(BitConverter.ToUInt16(ptr, offset));
            }
            else
            {
                value = (ushort)(ptr[offset] << 8);
            }

            if (_isUnsigned)
            {
                value ^= 0x8000;
            }

            return (short)value;
        }

        int FillBuffer(int maxSamples)
        {
            int bufferedSamples = 0;
            var dstPos = 0;

            // We can only read up to "kSampleBufferLength" samples
            // so we take this into consideration, when trying to
            // read up to maxSamples.
            maxSamples = Math.Min(SampleBufferLength, maxSamples);

            // We will only read up to maxSamples
            while (maxSamples > 0 && !IsEndOfData)
            {
                // Try to read all the sample data and update the
                // destination pointer.
                var bytesRead = _stream.Read(_buffer, dstPos, maxSamples * (_is16Bit ? 2 : 1));
                dstPos += bytesRead;

                // Calculate how many samples we actually read.
                var samplesRead = bytesRead / (_is16Bit ? 2 : 1);

                // Update all status variables
                bufferedSamples += samplesRead;
                maxSamples -= samplesRead;

                // We stop stream playback, when we reached the end of the data stream.
                // We also stop playback when an error occures.
                if (_stream.Position == _stream.Length)
                    _endOfData = true;
            }

            return bufferedSamples;
        }

        /// <summary>
        /// How many samples we can buffer at once.
        /// </summary>
        /// <remarks>
        /// TODO: Check whether this size suffices
        /// for systems with slow disk I/O.
        /// </remarks>
        const int SampleBufferLength = 2048;

        readonly bool _is16Bit;
        bool _isLE;
        bool _isUnsigned;

        /// <summary>
        /// Sample rate of stream
        /// </summary>
        int _rate;
        /// <summary>
        /// Whether this is an stereo stream.
        /// </summary>
        bool _isStereo;
        /// <summary>
        /// Calculated total play time
        /// </summary>
        Timestamp _playtime;
        /// <summary>
        /// Stream to read data from.
        /// </summary>
        Stream _stream;
        bool _disposeStream;
        /// <summary>
        /// Whether the stream end has been reached.
        /// </summary>
        bool _endOfData;
        /// <summary>
        /// Buffer used in readBuffer.
        /// </summary>
        byte[] _buffer;
    }
}

