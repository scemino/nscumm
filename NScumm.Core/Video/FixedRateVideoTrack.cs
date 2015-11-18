using NScumm.Core.Audio;

namespace NScumm.Core.Video
{
    internal abstract class FixedRateVideoTrack : VideoTrack
    {
        protected abstract Rational FrameRate { get; }

        public override uint GetNextFrameStartTime()
        {
            if (EndOfTrack || CurrentFrame < 0)
                return 0;

            return (uint)GetFrameTime((uint)(CurrentFrame + 1)).Milliseconds;
        }

        public Timestamp GetFrameTime(uint frame)
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