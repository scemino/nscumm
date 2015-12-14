using NScumm.Core.Audio;

namespace NScumm.Core.Video
{
    internal abstract class Track : ITrack
    {
        private bool _paused;

        public abstract TrackType TrackType { get; }

        public abstract bool EndOfTrack { get; }

        public virtual bool IsRewindable { get { return IsSeekable; } }

        public virtual bool Rewind()
        {
            return Seek(new Timestamp(0, 1000));
        }

        public virtual bool IsSeekable
        {
            get { return false; }
        }

        public virtual bool Seek(Timestamp time)
        {
            return false;
        }

        public void Pause(bool shouldPause)
        {
            _paused = shouldPause;
            PauseIntern(shouldPause);
        }

        public bool IsPaused { get { return _paused; } }

        public virtual Timestamp Duration
        {
            get
            {
                return new Timestamp(0, 1000);
            }
        }

        protected virtual void PauseIntern(bool shouldPause) { }
    }
}