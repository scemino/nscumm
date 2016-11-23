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
using static NScumm.Core.DebugHelper;
using NScumm.Sci.Sound.Decoders;
using System.IO;

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

        public int ReadBuffer(Ptr<short> buffer, int numSamples)
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
            lock (_mutex)
            {

                if (_finished)
                {
                    Warning("Packet {0} sent to finished robot audio stream", packet.position);
                    return false;
                }

                // `packet.position` is the decompressed (doubled) position of the packet,
                // so values of `position` will always be divisible either by 2 (even) or by
                // 4 (odd).
                sbyte bufferIndex = (sbyte)((packet.position % 4) != 0 ? 1 : 0);

                // Packet 0 is the first primer, packet 2 is the second primer,
                // packet 4+ are regular audio data
                if (packet.position <= 2 && _firstPacketPosition == -1)
                {
                    _readHead = 0;
                    _readHeadAbs = 0;
                    _maxWriteAbs = _loopBufferSize;
                    _writeHeadAbs = 2;
                    _jointMin[0] = 0;
                    _jointMin[1] = 2;
                    _waiting = true;
                    _finished = false;
                    _firstPacketPosition = packet.position;
                    FillRobotBuffer(packet, bufferIndex);
                    return true;
                }

                int packetEndByte = packet.position + (packet.dataSize * sizeof(short) * kEOSExpansion);

                // Already read all the way past this packet (or already wrote valid samples
                // to this channel all the way past this packet), so discard it
                if (packetEndByte <= Math.Max(_readHeadAbs, _jointMin[bufferIndex]))
                {
                    DebugC(DebugLevels.Video, "Rejecting packet {0}, read past {1} / {2}", packet.position, _readHeadAbs, _jointMin[bufferIndex]);
                    return true;
                }

                // The loop buffer is full, so tell the caller to send the packet again
                // later
                if (_maxWriteAbs <= _jointMin[bufferIndex])
                {
                    DebugC(DebugLevels.Video, "Rejecting packet {0}, full buffer", packet.position);
                    return false;
                }

                FillRobotBuffer(packet, bufferIndex);

                // This packet is the second primer, so allow playback to begin
                if (_firstPacketPosition != -1 && _firstPacketPosition != packet.position)
                {
                    DebugC(DebugLevels.Video, "Done waiting. Robot audio begins");
                    _waiting = false;
                    _firstPacketPosition = -1;
                }

                // Only part of the packet could be read into the loop buffer before it was
                // full, so tell the caller to send the packet again later
                if (packetEndByte > _maxWriteAbs)
                {
                    DebugC(DebugLevels.Video, "Partial read of packet {0} ({1} / {2})", packet.position, packetEndByte - _maxWriteAbs, packetEndByte - packet.position);
                    return false;
                }

                // The entire packet was successfully read into the loop buffer
                return true;
            }
        }

        private void FillRobotBuffer(RobotAudioPacket packet, sbyte bufferIndex)
        {
            int sourceByte = 0;

            int decompressedSize = packet.dataSize * sizeof(short);
            if (_decompressionBufferPosition != packet.position)
            {
                if (decompressedSize != _decompressionBufferSize)
                {
                    _decompressionBuffer.Realloc(decompressedSize);
                    _decompressionBufferSize = decompressedSize;
                }

                short carry = 0;
                using (var ms = new MemoryStream(packet.data.Data, packet.data.Offset, packet.dataSize))
                {
                    SolStream.DeDpcm16(new UShortAccess(_decompressionBuffer), ms, packet.dataSize, ref carry);
                }
                _decompressionBufferPosition = packet.position;
            }

            int numBytes = decompressedSize;
            int packetPosition = packet.position;
            int endByte = packet.position + decompressedSize * kEOSExpansion;
            int startByte = Math.Max(_readHeadAbs + bufferIndex * 2, _jointMin[bufferIndex]);
            int maxWriteByte = _maxWriteAbs + bufferIndex * 2;
            if (packetPosition < startByte)
            {
                sourceByte = (startByte - packetPosition) / kEOSExpansion;
                numBytes -= sourceByte;
                packetPosition = startByte;
            }
            if (packetPosition > maxWriteByte)
            {
                numBytes += (packetPosition - maxWriteByte) / kEOSExpansion;
                packetPosition = maxWriteByte;
            }
            if (endByte > maxWriteByte)
            {
                numBytes -= (endByte - maxWriteByte) / kEOSExpansion;
                endByte = maxWriteByte;
            }

            int maxJointMin = Math.Max(_jointMin[0], _jointMin[1]);
            if (endByte > maxJointMin)
            {
                _writeHeadAbs += endByte - maxJointMin;
            }

            if (packetPosition > _jointMin[bufferIndex])
            {
                int packetEndByte = packetPosition % _loopBufferSize;
                int targetBytePosition;
                int numBytesToEnd;
                if ((packetPosition & ~3) > (_jointMin[1 - bufferIndex] & ~3))
                {
                    targetBytePosition = _jointMin[1 - bufferIndex] % _loopBufferSize;
                    if (targetBytePosition >= packetEndByte)
                    {
                        numBytesToEnd = _loopBufferSize - targetBytePosition;
                        Array.Clear(_loopBuffer.Data, _loopBuffer.Offset + targetBytePosition, numBytesToEnd);
                        targetBytePosition = (1 - bufferIndex) != 0 ? 2 : 0;
                    }
                    numBytesToEnd = packetEndByte - targetBytePosition;
                    if (numBytesToEnd > 0)
                    {
                        Array.Clear(_loopBuffer.Data, _loopBuffer.Offset + targetBytePosition, numBytesToEnd);
                    }
                }
                targetBytePosition = _jointMin[bufferIndex] % _loopBufferSize;
                if (targetBytePosition >= packetEndByte)
                {
                    numBytesToEnd = _loopBufferSize - targetBytePosition;
                    InterpolateChannel(new UShortAccess(_loopBuffer, targetBytePosition), numBytesToEnd / sizeof(short) / kEOSExpansion, 0);
                    targetBytePosition = bufferIndex != 0 ? 2 : 0;
                }
                numBytesToEnd = packetEndByte - targetBytePosition;
                if (numBytesToEnd > 0)
                {
                    InterpolateChannel(new UShortAccess(_loopBuffer + targetBytePosition), numBytesToEnd / sizeof(short) / kEOSExpansion, 0);
                }
            }

            if (numBytes > 0)
            {
                int targetBytePosition = packetPosition % _loopBufferSize;
                int packetEndByte = endByte % _loopBufferSize;
                int numBytesToEnd = 0;
                if (targetBytePosition >= packetEndByte)
                {
                    numBytesToEnd = (_loopBufferSize - (targetBytePosition & ~3)) / kEOSExpansion;
                    CopyEveryOtherSample(new UShortAccess(_loopBuffer + targetBytePosition), new UShortAccess(_decompressionBuffer + sourceByte), numBytesToEnd / kEOSExpansion);
                    targetBytePosition = bufferIndex != 0 ? 2 : 0;
                }
                CopyEveryOtherSample(new UShortAccess(_loopBuffer + targetBytePosition), new UShortAccess(_decompressionBuffer + sourceByte + numBytesToEnd), (packetEndByte - targetBytePosition) / sizeof(short) / kEOSExpansion);
            }
            _jointMin[bufferIndex] = endByte;
        }

        private static void InterpolateChannel(UShortAccess buffer, int numSamples, sbyte bufferIndex)
        {
            if (numSamples <= 0)
            {
                return;
            }

            if (bufferIndex != 0)
            {
                short lastSample = (short)buffer.Value;
                int sample = lastSample;
                UShortAccess target = new UShortAccess(buffer, 2);
                UShortAccess source = new UShortAccess(buffer, 4);
                --numSamples;

                while ((numSamples--) != 0)
                {
                    sample = source.Value + lastSample;
                    lastSample = (short)source.Value;
                    sample /= 2;
                    target.Value = (ushort)sample;
                    source.Offset += 4;
                    target.Offset += 4;
                }

                target.Value = (ushort)sample;
            }
            else {
                var target = new UShortAccess(buffer);
                var source = new UShortAccess(buffer, 2);
                var lastSample = (short)source.Value;

                while ((numSamples--) != 0)
                {
                    int sample = (short)source.Value + lastSample;
                    lastSample = (short)source.Value;
                    sample /= 2;
                    target.Value = (ushort)sample;
                    source.Offset += 4;
                    target.Offset += 4;
                }
            }
        }

        private static void CopyEveryOtherSample(UShortAccess @out, UShortAccess @in, int numSamples)
        {
            while ((numSamples--) != 0)
            {
                @out.Value = @in.Value;
                @in.Offset += 2;
                @out.Offset += 4;
            }
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
