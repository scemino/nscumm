using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using System.IO;

namespace NScumm.Core.Video
{
    public enum TrackType
    {
        None,
        Video,
        Audio
    }

    public abstract class VideoDecoder
    {
        private readonly List<ITrack> _tracks = new List<ITrack>();
        private readonly List<ITrack> _internalTracks = new List<ITrack>();
        private readonly List<ITrack> _externalTracks = new List<ITrack>();
        private AudioTrack _mainAudioTrack;
        private byte _audioVolume = Mixer.MaxChannelVolume;
        private sbyte _audioBalance;
        private VideoTrack _nextVideoTrack;
        private uint _pauseLevel;
        private Rational _playbackRate = new Rational();
        private bool _needsUpdate;
        private Timestamp _lastTimeChange = new Timestamp(0);
        private int _startTime;
        private uint _pauseStartTime;
        private bool _endTimeSet;
        private Timestamp _endTime;
        private bool _dirtyPalette;
        private byte[] _palette;

        public bool IsPaused { get { return _pauseLevel != 0; } }

        public bool IsPlaying
        {
            get
            {
                return _playbackRate != 0;
            }
        }

        public bool IsVideoLoaded
        {
            get { return _tracks.Count != 0; }
        }

        public virtual bool UseAudioSync
        {
            get { return true; }
        }

        public bool EndOfVideo
        {
            get
            {
                foreach (var track in _tracks)
                {
                    if (!track.EndOfTrack &&
                        (!IsPlaying || track.TrackType != TrackType.Video || !_endTimeSet ||
                         ((VideoTrack)track).GetNextFrameStartTime() < (uint)_endTime.Milliseconds))
                        return false;
                }
                return true;
            }
        }

        public bool NeedsUpdate
        {
            get { return HasFramesLeft() && GetTimeToNextFrame() == 0; }
        }

        public byte[] Palette { get; private set; }

        public bool HasDirtyPalette { get; private set; }

        public bool IsRewindable
        {
            get
            {
                if (!IsVideoLoaded)
                    return false;

                return _tracks.All(track => track.IsRewindable);
            }
        }

        public int CurrentFrame
        {
            get
            {
                int frame = -1;
                var tracks = _tracks.OfType<VideoTrack>();
                foreach (var track in tracks)
                {
                    frame += track.CurrentFrame + 1;
                }
                return frame;
            }
        }

        public void Start()
        {
            if (!IsPlaying)
                SetRate(new Rational(1));
        }

        public ushort GetWidth()
        {
            var track = _tracks.OfType<VideoTrack>().FirstOrDefault();
            return (ushort)(track == null ? 0 : track.Width);
        }

        public ushort GetHeight()
        {
            var track = _tracks.OfType<VideoTrack>().FirstOrDefault();
            return (ushort)(track == null ? 0 : track.Height);
        }

        public virtual void Close()
        {
            if (IsPlaying)
                Stop();

            for (int i = 0; i < _tracks.Count; i++)
            {
                // TODO: ? _tracks[i].Dispose();
                _tracks[i] = null;
            }

            _tracks.Clear();
            _internalTracks.Clear();
            _externalTracks.Clear();
            _dirtyPalette = false;
            _palette = null;
            _startTime = 0;
            _audioVolume = Mixer.MaxChannelVolume;
            _audioBalance = 0;
            _pauseLevel = 0;
            _needsUpdate = false;
            _lastTimeChange = new Timestamp(0);
            _endTime = new Timestamp(0);
            _endTimeSet = false;
            _nextVideoTrack = null;
            _mainAudioTrack = null;
        }

        public virtual bool Rewind()
        {
            if (!IsRewindable)
                return false;

            // Stop all tracks so they can be rewound
            if (IsPlaying)
                StopAudio();

            foreach (var track in _tracks)
            {
                if (!track.Rewind())
                    return false;
            }
            // Now that we've rewound, start all tracks again
            if (IsPlaying)
                StartAudio();

            _lastTimeChange = new Timestamp(0);
            _startTime = Environment.TickCount;
            ResetPauseStartTime();
            FindNextVideoTrack();
            return true;
        }

        public abstract bool LoadStream(Stream stream);

        public bool HasAudio()
        {
            return _tracks.OfType<AudioTrack>().Any();
        }

        public virtual Surface DecodeNextFrame()
        {
            _needsUpdate = false;

            ReadNextPacket();

            // If we have no next video track at this point, there shouldn't be
            // any frame available for us to display.
            if (_nextVideoTrack == null)
                return null;

            Surface frame = _nextVideoTrack.DecodeNextFrame();

            if (_nextVideoTrack.HasDirtyPalette())
            {
                Palette = _nextVideoTrack.GetPalette();
                HasDirtyPalette = true;
            }

            // Look for the next video track here for the next decode.
            FindNextVideoTrack();

            return frame;
        }


        protected void ResetPauseStartTime()
        {
            if (IsPaused)
                _pauseStartTime = (uint)Environment.TickCount;
        }

        protected ITrack GetTrack(int track)
        {
            if (track > _internalTracks.Count)
                return null;

            return _internalTracks[track];
        }

        protected void AddTrack(ITrack track, bool isExternal = false)
        {
            _tracks.Add(track);

            if (isExternal)
                _externalTracks.Add(track);
            else
                _internalTracks.Add(track);

            if (track.TrackType == TrackType.Audio)
            {
                // Update volume settings if it's an audio track
                ((AudioTrack)track).Volume = _audioVolume;
                ((AudioTrack)track).Balance = _audioBalance;

                if (!isExternal && SupportsAudioTrackSwitching())
                {
                    if (_mainAudioTrack != null)
                    {
                        // The main audio track has already been found
                        ((AudioTrack)track).Mute = true;
                    }
                    else
                    {
                        // First audio track found . now the main one
                        _mainAudioTrack = (AudioTrack)track;
                        _mainAudioTrack.Mute = false;
                    }
                }
            }
            else if (track.TrackType == TrackType.Video)
            {
                // If this track has a better time, update _nextVideoTrack
                if (_nextVideoTrack == null || ((VideoTrack)track).GetNextFrameStartTime() < _nextVideoTrack.GetNextFrameStartTime())
                    _nextVideoTrack = (VideoTrack)track;
            }

            // Keep the track paused if we're paused
            if (IsPaused)
                track.Pause(true);

            // Start the track if we're playing
            if (IsPlaying && track.TrackType == TrackType.Audio)
                ((AudioTrack)track).Start();
        }

        protected virtual bool SupportsAudioTrackSwitching()
        {
            return false;
        }

        protected virtual void ReadNextPacket() { }

        private uint GetTimeToNextFrame()
        {
            if (EndOfVideo || _needsUpdate || _nextVideoTrack == null)
                return 0;

            uint currentTime = GetTime();
            uint nextFrameStartTime = _nextVideoTrack.GetNextFrameStartTime();

            if (_nextVideoTrack.IsReversed)
            {
                // For reversed videos, we need to handle the time difference the opposite way.
                if (nextFrameStartTime >= currentTime)
                    return 0;

                return currentTime - nextFrameStartTime;
            }

            // Otherwise, handle it normally.
            if (nextFrameStartTime <= currentTime)
                return 0;

            return nextFrameStartTime - currentTime;
        }

        private bool HasFramesLeft()
        {
            // This is similar to endOfVideo(), except it doesn't take Audio into account (and returns true if not the end of the video)
            // This is only used for needsUpdate() atm so that setEndTime() works properly
            // And unlike endOfVideoTracks(), this takes into account _endTime
            foreach (var track in _tracks)
            {
                if ((track).TrackType == TrackType.Video && !(track).EndOfTrack &&
                    (!IsPlaying || !_endTimeSet ||
                     ((VideoTrack)track).GetNextFrameStartTime() < (uint)_endTime.Milliseconds))
                    return true;
            }

            return false;
        }

        private bool SetReverse(bool reverse)
        {
            // Can only reverse video-only videos
            if (reverse && HasAudio())
                return false;

            // Attempt to make sure all the tracks are in the requested direction
            foreach (var t in _tracks)
            {
                if (t.TrackType == TrackType.Video && ((VideoTrack)t).IsReversed != reverse)
                {
                    if (!((VideoTrack)t).SetReverse(reverse))
                        return false;

                    _needsUpdate = true; // force an update
                }
            }

            FindNextVideoTrack();
            return true;
        }

        private void SetRate(Rational rate)
        {
            if (!IsVideoLoaded || _playbackRate == rate)
                return;

            if (rate == 0)
            {
                Stop();
                return;
            }
            else if (rate != 1 && HasAudio())
            {
                throw new InvalidOperationException("Cannot set custom rate in videos with audio");
            }

            Rational targetRate = rate;

            // Attempt to set the reverse
            if (!SetReverse(rate < 0))
            {
                Debug.Assert(rate < 0); // We shouldn't fail for forward.
                                        //TODO: warning("Cannot set custom rate to backwards");
                SetReverse(false);
                targetRate = new Rational(1);

                if (_playbackRate == targetRate)
                    return;
            }

            if (_playbackRate != 0)
                _lastTimeChange = new Timestamp((int)GetTime(), 1);

            _playbackRate = targetRate;
            _startTime = Environment.TickCount;

            // Adjust start time if we've seeked to something besides zero time
            if (_lastTimeChange != new Timestamp(0))
                _startTime -= _lastTimeChange.Milliseconds / _playbackRate;

            StartAudio();
        }

        private void StartAudio()
        {
            if (_endTimeSet)
            {
                // HACK: Timestamp's subtraction asserts out when subtracting two times
                // with different rates.
                StartAudioLimit(_endTime - _lastTimeChange.ConvertToFramerate(_endTime.Framerate));
                return;
            }

            foreach (var track in _tracks.OfType<AudioTrack>())
            {
                track.Start();
            }
        }

        private void StartAudioLimit(Timestamp limit)
        {
            foreach (var track in _tracks.OfType<AudioTrack>())
            {
                track.Start(limit);
            }
        }

        private void Stop()
        {
            if (!IsPlaying)
                return;

            // Stop audio here so we don't have it affect getTime()
            StopAudio();

            // Keep the time marked down in case we start up again
            // We do this before _playbackRate is set so we don't get
            // _lastTimeChange returned, but before _pauseLevel is
            // reset.
            _lastTimeChange = new Timestamp((int)GetTime());

            _playbackRate = new Rational(0);
            _startTime = 0;
            Palette = null;
            HasDirtyPalette = false;
            _needsUpdate = false;

            // Also reset the pause state.
            _pauseLevel = 0;

            // Reset the pause state of the tracks too
            foreach (var track in _tracks)
            {
                track.Pause(false);
            }
        }

        private void StopAudio()
        {
            foreach (var track in _tracks.OfType<AudioTrack>())
            {
                track.Stop();
            }
        }

        private uint GetTime()
        {
            if (!IsPlaying)
                return (uint)_lastTimeChange.Milliseconds;

            if (IsPaused)
                return (uint)Math.Max((int)(_playbackRate * (_pauseStartTime - _startTime)), 0);

            if (UseAudioSync)
            {
                foreach (var t in _tracks.OfType<AudioTrack>())
                {
                    if (!t.EndOfTrack)
                    {
                        uint time = t.RunningTime;

                        if (time != 0)
                            return (uint)(time + _lastTimeChange.Milliseconds);
                    }
                }
            }

            return (uint)Math.Max(_playbackRate * (Environment.TickCount - _startTime), 0);
        }

        private VideoTrack FindNextVideoTrack()
        {
            _nextVideoTrack = null;
            uint bestTime = 0xFFFFFFFF;

            foreach (var t in _tracks)
            {
                if (t.TrackType == TrackType.Video && !t.EndOfTrack)
                {
                    VideoTrack track = (VideoTrack)t;
                    uint time = track.GetNextFrameStartTime();

                    if (time < bestTime)
                    {
                        bestTime = time;
                        _nextVideoTrack = track;
                    }
                }
            }

            return _nextVideoTrack;
        }
    }
}
