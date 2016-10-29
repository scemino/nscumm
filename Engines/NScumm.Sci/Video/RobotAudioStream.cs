//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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

using NScumm.Core;
using NScumm.Core.Audio;
using System;

namespace NScumm.Sci.Video
{
    internal class RobotAudioStream : IAudioStream
    {
        /// <summary>
        /// The sample rate used for all robot audio.
        /// </summary>
        public const int kRobotSampleRate = 22050;

        /// <summary>
        /// Multiplier for the size of a packet that
        /// is being expanded by writing to every other
        /// byte of the target buffer.
        /// </summary>
        public const int kEOSExpansion = 2;

        object _mutex = new object();

        /**
         * Loop buffer for playback. Contains decompressed
         * 16-bit PCM samples.
         */
        BytePtr _loopBuffer;

        /**
         * The size of the loop buffer, in bytes.
         */
        int _loopBufferSize;

        /**
         * The position of the read head within the loop
         * buffer, in bytes.
         */
        int _readHead;

        /**
         * The lowest file position that can be buffered,
         * in uncompressed bytes.
         */
        int _readHeadAbs;

        /**
         * The highest file position that can be buffered,
         * in uncompressed bytes.
         */
        int _maxWriteAbs;

        /**
         * The highest file position, in uncompressed bytes,
         * that has been written to the stream.
         * Different from `_maxWriteAbs`, which is the highest
         * uncompressed position which *can* be written right
         * now.
         */
        int _writeHeadAbs;

        /**
         * The highest file position, in uncompressed bytes,
         * that has been written to the even & odd sides of
         * the stream.
         *
         * Index 0 corresponds to the 'even' side; index
         * 1 correspond to the 'odd' side.
         */
        int[] _jointMin = new int[2];

        /**
         * When `true`, the stream is waiting for all primer
         * blocks to be received before allowing playback to
         * begin.
         */
        bool _waiting;

        /**
         * When `true`, the stream will accept no more audio
         * blocks.
         */
        bool _finished;

        /**
         * The uncompressed position of the first packet of
         * robot data. Used to decide whether all primer
         * blocks have been received and the stream should
         * be started.
         */
        int _firstPacketPosition;

        /**
         * Decompression buffer, used to temporarily store
         * an uncompressed block of audio data.
         */
        BytePtr _decompressionBuffer;

        /**
         * The size of the decompression buffer, in bytes.
         */
        int _decompressionBufferSize;

        /**
         * The position of the packet currently in the
         * decompression buffer. Used to avoid
         * re-decompressing audio data that has already
         * been decompressed during a partial packet read.
         */
        int _decompressionBufferPosition;

        public bool IsStereo => false;

        public int Rate => 22050;

        public bool IsEndOfData
        {
            get
            {
                lock (_mutex)
                {
                    return _readHeadAbs >= _writeHeadAbs;
                }
            }
        }

        public bool IsEndOfStream
        {
            get
            {
                lock (_mutex)
                {
                    return _finished && IsEndOfData;
                }
            }
        }

        /// <summary>
        /// Playback state information. Used for framerate
        /// calculation.
        /// </summary>
        internal class StreamState
        {
            /// <summary>
            ///  The current position of the read head of
            /// the audio stream.
            /// </summary>
            public int bytesPlaying;

            /// <summary>
            /// The sample rate of the audio stream.
            /// Always 22050.
            /// </summary>
            public ushort rate;

            /// <summary>
            /// The bit depth of the audio stream.
            /// Always 16.
            /// </summary>
            public byte bits;
        }

        /// <summary>
        /// A single packet of compressed audio from a
        /// Robot data stream.
        /// </summary>
        internal class RobotAudioPacket
        {
            /// <summary>
            /// Raw DPCM-compressed audio data.
            /// </summary>
            public BytePtr data;

            /// <summary>
            /// The size of the compressed audio data,
            /// in bytes.
            /// </summary>
            public int dataSize;

            /// <summary>
            /// The uncompressed, file-relative position
            /// of this audio packet.
            /// </summary>
            public int position;

            public RobotAudioPacket(BytePtr data, int dataSize, int position)
            {

                this.data = data;
                this.dataSize = dataSize;
                this.position = position;
            }
        }

        public RobotAudioStream(int bufferSize)
        {
            _loopBuffer = new byte[bufferSize];
            _loopBufferSize = bufferSize;
            _decompressionBuffer = BytePtr.Null;
            _decompressionBufferPosition = -1;
            _waiting = true;
            _firstPacketPosition = -1;
        }

        public int ReadBuffer(short[] buffer, int numSamples)
        {
            lock (_mutex)
            {

                if (_waiting)
                {
                    return 0;
                }

                throw new NotImplementedException();

                //System.Diagnostics.Debug.Assert(((_writeHeadAbs - _readHeadAbs) & 1) == 0);
                //int maxNumSamples = (_writeHeadAbs - _readHeadAbs) / sizeof(short);
                //numSamples = Math.Min(numSamples, maxNumSamples);

                //if (numSamples == 0)
                //{
                //    return 0;
                //}

                //InterpolateMissingSamples(numSamples);

                //var inBuffer = new UShortAccess(_loopBuffer, _readHead);

                //System.Diagnostics.Debug.Assert(0 == ((_loopBufferSize - _readHead) & 1));
                //int numSamplesToEnd = (_loopBufferSize - _readHead) / sizeof(short);

                //int numSamplesToRead = Math.Min(numSamples, numSamplesToEnd);
                //Common::copy(inBuffer, inBuffer + numSamplesToRead, outBuffer);

                //if (numSamplesToRead < numSamples)
                //{
                //    inBuffer = (Audio::st_sample_t*)_loopBuffer;
                //    outBuffer += numSamplesToRead;
                //    numSamplesToRead = numSamples - numSamplesToRead;
                //    Common::copy(inBuffer, inBuffer + numSamplesToRead, outBuffer);
                //}

                //int numBytes = numSamples * sizeof(short);

                //_readHead += numBytes;
                //if (_readHead > _loopBufferSize)
                //{
                //    _readHead -= _loopBufferSize;
                //}
                //_readHeadAbs += numBytes;
                //_maxWriteAbs += numBytes;
                //System.Diagnostics.Debug.Assert(0==(_readHead & 1));
                //System.Diagnostics.Debug.Assert(0 == (_readHeadAbs & 1));

                //return numSamples;
            }
        }

        public void Dispose()
        {
        }

        public bool AddPacket(RobotAudioPacket packet)
        {
            throw new NotImplementedException();
        }

        public StreamState GetStatus()
        {
            lock (_mutex)
            {
                StreamState status = new StreamState();
                status.bytesPlaying = _readHeadAbs;
                status.rate = (ushort)Rate;
                status.bits = 8 * sizeof(short);
                return status;
            }
        }

        public void Finish()
        {
            lock (_mutex)
            {
                _finished = true;
            }
        }
    }
}
