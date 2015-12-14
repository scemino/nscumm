using NScumm.Core.Audio;

namespace NScumm.Core.Video
{
    public interface ITrack
    {
        TrackType TrackType { get; }
        bool EndOfTrack { get; }
        bool IsRewindable { get; }
        bool Rewind();
        bool IsSeekable { get; }
        bool Seek(Timestamp time);
        void Pause(bool shouldPause);
        bool IsPaused { get; }
        Timestamp Duration { get; }
    }
}