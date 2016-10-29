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
using NScumm.Sci.Sound;
using System;

namespace NScumm.Sci.Video
{
    /// <summary>
    /// This class manages packetized audio playback
    /// for robots.
    /// </summary>
    class AudioList
    {
        /// <summary>
        ///  AudioBlock represents a block of audio
        /// from the Robot's audio track.
        /// </summary>
        class AudioBlock
        {
            public AudioBlock(int position, int size, BytePtr data)
            {
                _position = position;
                _size = size;
                _data = new byte[size];
                Array.Copy(data.Data, data.Offset, _data.Data, 0, size);
            }

            /// <summary>
            /// Submits the block of audio to the
            /// audio manager.
            /// </summary>
            /// <param name="startOffset"></param>
            /// <returns>true if the block was fully
            /// read, or false if the block was not
            /// read or only partially read.
            /// </returns>
            public bool Submit(int startOffset)
            {
                System.Diagnostics.Debug.Assert(_data != BytePtr.Null);
                var packet = new RobotAudioStream.RobotAudioPacket(_data, _size, (_position - startOffset) * 2);
                return SciEngine.Instance._audio32.PlayRobotAudio(packet);
            }


            /// <summary>
            /// The absolute position, in compressed
            /// bytes, of this audio block's audio
            /// data in the audio stream.
            /// </summary>
            private int _position;

            /// <summary>
            /// The compressed size, in bytes, of
            /// this audio block's audio data.
            /// </summary>
            private int _size;

            /// <summary>
            /// A buffer containing raw
            /// SOL-compressed audio data.
            /// </summary>
            private BytePtr _data;
        }

        /**
		 * Maximum number of on-screen screen items.
		 */
        private const int kScreenItemListSize = 10;

        /**
		 * Maximum number of queued audio blocks.
		 */
        private const int kAudioListSize = 10;

        /**
		 * Maximum number of samples used for frame timing.
		 */
        private const int kDelayListSize = 10;

        /**
		 * Maximum number of cues.
		 */
        private const int kCueListSize = 256;

        /**
		 * Maximum number of 'fixed' cels that never
		 * change for the duration of a robot.
		 */
        private const int kFixedCelListSize = 4;

        /**
		 * The size of a hunk palette in the Robot stream.
		 */
        private const int kRawPaletteSize = 1200;

        /**
		 * The size of a frame of Robot data. This
		 * value was used to align the first block of
		 * data after the main Robot header to the next
		 * CD sector.
		 */
        private const int kRobotFrameSize = 2048;

        /**
		 * The size of a block of zero-compressed
		 * audio. Used to fill audio when the size of
		 * an audio packet does not match the expected
		 * packet size.
		 */
        private const int kRobotZeroCompressSize = 2048;

        /**
		 * The size of the audio block header, in bytes.
		 * The audio block header consists of the
		 * compressed size of the audio in the record,
		 * plus the position of the audio in the
		 * compressed data stream.
		 */
        private const int kAudioBlockHeaderSize = 8;

        /**
		 * The size of a Robot cel header, in bytes.
		 */
        private const int kCelHeaderSize = 22;

        /**
		 * The maximum amount that the frame rate is
		 * allowed to drift from the nominal frame rate
		 * in order to correct for AV drift or slow
		 * playback.
		 */
        private const int kMaxFrameRateDrift = 1;

        /// <summary>
        /// The status of the audio track of a Robot
        /// animation.
        /// </summary>
        enum RobotAudioStatus
        {
            kRobotAudioReady = 1,
            kRobotAudioStopped = 2,
            kRobotAudioPlaying = 3,
            kRobotAudioPaused = 4,
            kRobotAudioStopping = 5
        }

        /// <summary>
        /// The list of compressed audio blocks
        /// submitted for playback.
        /// </summary>
        AudioBlock[] _blocks = new AudioBlock[kAudioListSize];

        /// <summary>
        /// The number of blocks in `_blocks` that are
        /// ready to be submitted.
        /// </summary>
        byte _blocksSize;

        /// <summary>
        /// The index of the oldest submitted audio block.
        /// </summary>
        byte _oldestBlockIndex;

        /// <summary>
        /// The index of the newest submitted audio block.
        /// </summary>
        byte _newestBlockIndex;

        /// <summary>
        /// The offset used when sending packets to the
        /// audio stream.
        /// </summary>
        int _startOffset;

        /// <summary>
        /// The status of robot audio playback.
        /// </summary>
        RobotAudioStatus _status;

        /// <summary>
        /// Stops playback of robot audio, allowing
        /// any queued audio to finish playing back.
        /// </summary>
        public void StopAudio()
        {
            SciEngine.Instance._audio32.FinishRobotAudio();
            FreeAudioBlocks();
            _status = RobotAudioStatus.kRobotAudioStopping;
        }

        /// <summary>
        /// Submits as many blocks of audio as possible
        /// to the audio engine.
        /// </summary>
        public void SubmitDriverMax()
        {
            while (_blocksSize != 0)
            {
                if (!_blocks[_oldestBlockIndex].Submit(_startOffset))
                {
                    return;
                }

                _blocks[_oldestBlockIndex] = null;
                ++_oldestBlockIndex;
                if (_oldestBlockIndex == kAudioListSize)
                {
                    _oldestBlockIndex = 0;
                }

                --_blocksSize;
            }
        }

        /// <summary>
        /// Adds a new AudioBlock to the queue.
        /// </summary>
        /// <param name="position">The absolute position of the audio for the block, in compressed bytes.</param>
        /// <param name="size">The size of the buffer.</param>
        /// <param name="data">A pointer to compressed audio
        /// data that will be copied into the new AudioBlock.</param>
        public void AddBlock(int position, int size, BytePtr data)
        {
            System.Diagnostics.Debug.Assert(data != BytePtr.Null);
            System.Diagnostics.Debug.Assert(size >= 0);
            System.Diagnostics.Debug.Assert(position >= -1);

            if (_blocksSize == kAudioListSize)
            {
                _blocks[_oldestBlockIndex] = null;
                ++_oldestBlockIndex;
                if (_oldestBlockIndex == kAudioListSize)
                {
                    _oldestBlockIndex = 0;
                }
                --_blocksSize;
            }

            if (_blocksSize == 0)
            {
                _oldestBlockIndex = _newestBlockIndex = 0;
            }
            else
            {
                ++_newestBlockIndex;
                if (_newestBlockIndex == kAudioListSize)
                {
                    _newestBlockIndex = 0;
                }
            }

            _blocks[_newestBlockIndex] = new AudioBlock(position, size, data);
            ++_blocksSize;
        }

        /// <summary>
        /// Immediately stops any active playback and
        /// purges all audio data in the audio list.
        /// </summary>
        public void Reset()
        {
            StopAudioNow();
            _startOffset = 0;
            _status = RobotAudioStatus.kRobotAudioReady;
        }

        /// <summary>
        /// Pauses the robot audio channel in
        /// preparation for the first block of audio
        /// data to be read.
        /// </summary>
        public void PrepareForPrimer()
        {
            SciEngine.Instance._audio32.Pause(AudioChannelIndex.RobotChannel);
            _status = RobotAudioStatus.kRobotAudioPaused;
        }

        /// <summary>
        /// Sets the audio offset which is used to
        /// offset the position of audio packets
        /// sent to the audio stream.
        /// </summary>
        /// <param name="offset"></param>
        public void SetAudioOffset(int offset)
        {
            _startOffset = offset;
        }

        public void StopAudioNow()
        {
            if (_status == RobotAudioStatus.kRobotAudioPlaying || _status == RobotAudioStatus.kRobotAudioStopping || _status == RobotAudioStatus.kRobotAudioPaused)
            {
                SciEngine.Instance._audio32.StopRobotAudio();
                _status = RobotAudioStatus.kRobotAudioStopped;
            }

            FreeAudioBlocks();
        }

        private void FreeAudioBlocks()
        {
            while (_blocksSize != 0)
            {
                _blocks[_oldestBlockIndex] = null;
                ++_oldestBlockIndex;
                if (_oldestBlockIndex == kAudioListSize)
                {
                    _oldestBlockIndex = 0;
                }

                --_blocksSize;
            }
        }

        public void StartAudioNow()
        {
            SubmitDriverMax();
            SciEngine.Instance._audio32.Resume((short)AudioChannelIndex.RobotChannel);
            _status = RobotAudioStatus.kRobotAudioPlaying;
        }
    }
}
