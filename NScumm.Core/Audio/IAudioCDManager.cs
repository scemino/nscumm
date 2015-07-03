//
//  IAudioCDManager.cs
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

namespace NScumm.Core.Audio
{
    public struct AudioCdStatus
    {
        public bool playing;
        public int track;
        public int start;
        public int duration;
        public int numLoops;
        public int volume;
        public int balance;
    }

    public interface IAudioCDManager
    {
        bool IsPlaying { get; }

        int Volume { get; set; }

        int Balance { get; set; }

        /// <summary>
        /// Start audio CD playback.
        /// </summary>
        /// <param name="track">Track.</param>
        /// <param name="numLoops">Number loops.</param>
        /// <param name="startFrame">Start frame.</param>
        /// <param name="duration">Duration.</param>
        /// <param name="only_emulate">If set to <c>true</c> only emulate.</param>
        void Play(int track, int numLoops, int startFrame, int duration, bool only_emulate = false);

        /// <summary>
        /// Stop CD or emulated audio playback.
        /// </summary>
        void Stop();

        void Update();

        AudioCdStatus GetStatus();

        /// <summary>
        /// Start CD audio playback.
        /// </summary>
        /// <param name="track">the track to play.</param>
        /// <param name="num_loops">how often playback should be repeated (-1 = infinitely often).</param>
        /// <param name="start_frame">the frame at which playback should start (75 frames = 1 second).</param>
        /// <param name="duration">the number of frames to play.</param>
        void PlayCD(int track, int num_loops, int start_frame, int duration);

        void StopCD();

        void UpdateCD();
    }

}

