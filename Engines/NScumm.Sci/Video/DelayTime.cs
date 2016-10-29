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

namespace NScumm.Sci.Video
{
    /// <summary>
    /// This class tracks the amount of time it takes for
    /// a frame of robot animation to be rendered.This
    /// information is used by the player to speculatively
    /// skip rendering of future frames to keep the
    /// animation in sync with the robot audio.
    /// </summary>
    class DelayTime
    {
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

        private RobotDecoder _decoder;

        /// <summary>
        /// The start time, in ticks, of the current timing
        /// loop. If no loop is in progress, the value is 0.
        /// 
        /// @note This is slightly different than SSCI where
        /// the not-timing value was -1.
        /// </summary>
        private uint _startTime;

        /// <summary>
        /// A sorted list containing the timing data for
        /// the last `kDelayListSize` frames, in ticks.
        /// </summary>
        private int[] _delays = new int[kDelayListSize];

        /// <summary>
        /// A list of monotonically increasing identifiers
        /// used to identify and replace the oldest sample
        /// in the `_delays` array when finishing the
        /// next timing operation.
        /// </summary>
        private uint[] _timestamps = new uint[kDelayListSize];

        /// <summary>
        /// The identifier of the oldest timing.
        /// </summary>
        private uint _oldestTimestamp;

        /// <summary>
        /// The identifier of the newest timing.
        /// </summary>
        private uint _newestTimestamp;

        public bool TimingInProgress => _startTime != 0;

        public DelayTime(RobotDecoder robotDecoder)
        {
            _decoder = robotDecoder;
            for (int i = 0; i < kDelayListSize; ++i)
            {
                _timestamps[i] = (uint)i;
                _delays[i] = 0;
            }

            _oldestTimestamp = 0;
            _newestTimestamp = kDelayListSize - 1;
            _startTime = 0;
        }

        public int PredictedTicks()
        {
            return _delays[kDelayListSize / 2];
        }

        /// <summary>
        /// Starts performance timing.
        /// </summary>
        public void StartTiming()
        {
            _startTime = _decoder.TickCount;
        }

        public void EndTiming()
        {
            int timeDelta = (int)(_decoder.TickCount - _startTime);
            for (var i = 0; i < kDelayListSize; ++i)
            {
                if (_timestamps[i] == _oldestTimestamp)
                {
                    _timestamps[i] = ++_newestTimestamp;
                    _delays[i] = timeDelta;
                    break;
                }
            }
            ++_newestTimestamp;
            _startTime = 0;
            SortList();
        }

        private void SortList()
        {
            for (var i = 0; i < kDelayListSize - 1; ++i)
            {
                int smallestDelay = _delays[i];
                int smallestIndex = i;

                for (var j = i + 1; j < kDelayListSize - 1; ++j)
                {
                    if (_delays[j] < smallestDelay)
                    {
                        smallestDelay = _delays[j];
                        smallestIndex = j;
                    }
                }

                if (smallestIndex != i)
                {
                    ScummHelper.Swap(ref _delays[i], ref _delays[smallestIndex]);
                    ScummHelper.Swap(ref _timestamps[i], ref _timestamps[smallestIndex]);
                }
            }
        }
    }
}
