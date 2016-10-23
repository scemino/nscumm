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

using NScumm.Core.Audio;

namespace NScumm.Core.Video
{
    /// <summary>
    /// A VideoTrack that is played at a constant rate.
    /// If the frame count is unknown, you must override endOfTrack().
    /// </summary>
    public abstract class FixedRateVideoTrack : VideoTrack
    {
        protected abstract Rational FrameRate { get; }

        public override uint GetNextFrameStartTime()
        {
            if (EndOfTrack || CurrentFrame < 0)
                return 0;

            return (uint)GetFrameTime((uint)(CurrentFrame + 1)).Milliseconds;
        }

        public override Timestamp GetFrameTime(uint frame)
        {
            // Try to get as accurate as possible, considering we have a fractional frame rate
            // (which Audio::Timestamp doesn't support).
            Rational frameRate = FrameRate;

            // Try to keep it in terms of the frame rate, if the frame rate is a whole
            // number.
            if (frameRate.Denominator == 1)
                return new Timestamp(0, (int)frame, frameRate);

            // Convert as best as possible
            Rational time = frameRate.Inverse() * new Rational((int)frame);
            return new Timestamp(0, time.Numerator, time.Denominator);
        }

        public uint GetFrameAtTime(Timestamp time)
        {
            Rational frameRate = FrameRate;

            // Easy conversion
            if (frameRate == time.Framerate)
                return (uint)time.TotalNumberOfFrames;

            // Create the rational based on the time first to hopefully cancel out
            // *something* when multiplying by the frameRate (which can be large in
            // some AVI videos).
            return (uint)(int)(new Rational(time.TotalNumberOfFrames, time.Framerate) * frameRate);
        }

    }
}