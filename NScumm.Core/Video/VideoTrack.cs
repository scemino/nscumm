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
using NScumm.Core.Graphics;

namespace NScumm.Core.Video
{
    public abstract class VideoTrack : Track
    {
        public override TrackType TrackType => TrackType.Video;

        public abstract uint GetNextFrameStartTime();

        public override bool EndOfTrack => CurrentFrame >= FrameCount - 1;

        public abstract ushort Width { get; }

        public abstract ushort Height { get; }

        public abstract PixelFormat PixelFormat { get; }

        public abstract int CurrentFrame { get; }

        public abstract int FrameCount { get; }

        public virtual bool IsReversed { get; set; }

        public abstract Surface DecodeNextFrame();

        public virtual byte[] GetPalette() { return null; }

        public virtual bool HasDirtyPalette()
        {
            return false;
        }

        public virtual Timestamp GetFrameTime(uint frame)
        {
            // Default implementation: Return an invalid (negative) number
            return new Timestamp(0).AddFrames(-1);
        }

        public bool SetReverse(bool reverse)
        {
            return !reverse;
        }
    }
}