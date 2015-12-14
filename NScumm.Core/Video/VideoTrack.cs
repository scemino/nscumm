using NScumm.Core.Graphics;

namespace NScumm.Core.Video
{
    internal abstract class VideoTrack : Track
    {
        public override TrackType TrackType { get { return TrackType.Video; } }

        public abstract uint GetNextFrameStartTime();

        public override bool EndOfTrack
        {
            get
            {
                return CurrentFrame >= FrameCount - 1;
            }
        }

        public abstract ushort Width { get; }

        public abstract ushort Height { get; }

        public abstract PixelFormat PixelFormat { get; }

        public abstract int CurrentFrame { get; }

        public abstract int FrameCount { get; }

        public virtual bool IsReversed { get; set; }

        public abstract Surface DecodeNextFrame();

        public abstract byte[] GetPalette();

        public virtual bool HasDirtyPalette()
        {
            return false;
        }

        //public Timestamp GetFrameTime(uint frame)
        //{
        //    // Default implementation: Return an invalid (negative) number
        //    return new Timestamp().AddFrames(-1);
        //}
        public bool SetReverse(bool reverse)
        {
            return !reverse;
        }
    }
}