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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NScumm.Core.Audio;
using NScumm.Core.Graphics;
using System.IO;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Core.Video
{
    public enum TrackType
    {
        None,
        Video,
        Audio
    }

    /// <summary>
    /// Generic interface for video decoder classes.
    /// </summary>
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
        private Point _pos;

        public bool IsPaused => _pauseLevel != 0;

        public bool HasDirtyPalette { get; private set; }

        public bool IsPlaying => _playbackRate != 0;

        public bool IsVideoLoaded => _tracks.Count != 0;

        public virtual bool UseAudioSync => true;

        public bool EndOfVideo
        {
            get
            {
                foreach (var track in _tracks)
                {
                    if (!track.EndOfTrack &&
                        (!IsPlaying || track.TrackType != TrackType.Video || !_endTimeSet ||
                         ((VideoTrack) track).GetNextFrameStartTime() < (uint) _endTime.Milliseconds))
                        return false;
                }
                return true;
            }
        }

        public bool NeedsUpdate => HasFramesLeft() && GetTimeToNextFrame() == 0;

        public byte[] Palette { get; private set; }

        public Point Pos { get { return _pos; } set { _pos = value; } }

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

        public void PauseVideo(bool pause)
        {
            if (pause)
            {
                _pauseLevel++;

                // We can't go negative
            }
            else if (_pauseLevel != 0)
            {
                _pauseLevel--;

                // Do nothing
            }
            else
            {
                return;
            }

            if (_pauseLevel == 1 && pause)
            {
                _pauseStartTime = (uint) ServiceLocator.Platform.GetMilliseconds();
                    // Store the starting time from pausing to keep it for later
                foreach (var track in _tracks)
                {
                    track.Pause(true);
                }
            }
            else if (_pauseLevel == 0)
            {
                foreach (var track in _tracks)
                {
                    track.Pause(false);
                }

                _startTime = (int) (_startTime + (ServiceLocator.Platform.GetMilliseconds() - _pauseStartTime));
            }
        }

        public int GetFrameCount()
        {
            var count = 0;
            foreach (var videoTrack in _tracks.OfType<VideoTrack>())
            {
                count += videoTrack.FrameCount;
            }

            return count;
        }

        public void SetEndFrame(uint frame)
        {
            VideoTrack track = _tracks.OfType<VideoTrack>().FirstOrDefault();
            // If we didn't find a video track, we can't set the final frame (of course)
            if (track == null)
                return;

            Timestamp time = track.GetFrameTime(frame + 1);

            if (time < new Timestamp(0))
                return;

            SetEndTime(time);
        }

        public void SetEndTime(Timestamp endTime)
        {
            Timestamp startTime = new Timestamp(0);

            if (IsPlaying)
            {
                startTime = new Timestamp((int) GetTime());
                StopAudio();
            }

            _endTime = endTime;
            _endTimeSet = true;

            if (startTime > endTime)
                return;

            if (IsPlaying)
            {
                // We'll assume the audio track is going to start up at the same time it just was
                // and therefore not do any seeking.
                // Might want to set it anyway if we're seekable.
                StartAudioLimit(new Timestamp(_endTime.Milliseconds - startTime.Milliseconds));
                _lastTimeChange = startTime;
            }
        }

        public ushort GetWidth()
        {
            var track = _tracks.OfType<VideoTrack>().FirstOrDefault();
            return track?.Width ?? 0;
        }

        public ushort GetHeight()
        {
            var track = _tracks.OfType<VideoTrack>().FirstOrDefault();
            return track?.Height ?? 0;
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
            HasDirtyPalette = false;
            Palette = null;
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

        /**
         * Load a video from a file with the given name.
         *
         * A default implementation using Common::File and loadStream is provided.
         *
         * @param filename	the filename to load
         * @return whether loading the file succeeded
         */

        public virtual bool LoadFile(string filename)
        {
            var file = Engine.OpenFileRead(filename);
            if (file == null)
            {
                return false;
            }

            return LoadStream(file);
        }

        public abstract bool LoadStream(Stream stream);

        public void SetVolume(byte volume)
        {
            _audioVolume = volume;

            foreach (var it in _tracks)
                if (it.TrackType == TrackType.Audio)
                    ((AudioTrack) it).SetVolume(_audioVolume);
        }

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
                _pauseStartTime = (uint) Environment.TickCount;
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
                ((AudioTrack) track).Volume = _audioVolume;
                ((AudioTrack) track).Balance = _audioBalance;

                if (!isExternal && SupportsAudioTrackSwitching())
                {
                    if (_mainAudioTrack != null)
                    {
                        // The main audio track has already been found
                        ((AudioTrack) track).Mute = true;
                    }
                    else
                    {
                        // First audio track found . now the main one
                        _mainAudioTrack = (AudioTrack) track;
                        _mainAudioTrack.Mute = false;
                    }
                }
            }
            else if (track.TrackType == TrackType.Video)
            {
                // If this track has a better time, update _nextVideoTrack
                if (_nextVideoTrack == null ||
                    ((VideoTrack) track).GetNextFrameStartTime() < _nextVideoTrack.GetNextFrameStartTime())
                    _nextVideoTrack = (VideoTrack) track;
            }

            // Keep the track paused if we're paused
            if (IsPaused)
                track.Pause(true);

            // Start the track if we're playing
            if (IsPlaying && track.TrackType == TrackType.Audio)
                ((AudioTrack) track).Start();
        }

        protected virtual bool SupportsAudioTrackSwitching()
        {
            return false;
        }

        protected virtual void ReadNextPacket()
        {
        }

        public uint GetTimeToNextFrame()
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
                     ((VideoTrack) track).GetNextFrameStartTime() < (uint) _endTime.Milliseconds))
                    return true;
            }

            return false;
        }

        public bool SetReverse(bool reverse)
        {
            // Can only reverse video-only videos
            if (reverse && HasAudio())
                return false;

            // Attempt to make sure all the tracks are in the requested direction
            foreach (var t in _tracks)
            {
                if (t.TrackType == TrackType.Video && ((VideoTrack) t).IsReversed != reverse)
                {
                    if (!((VideoTrack) t).SetReverse(reverse))
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
                D.Warning("Cannot set custom rate to backwards");
                SetReverse(false);
                targetRate = new Rational(1);

                if (_playbackRate == targetRate)
                    return;
            }

            if (_playbackRate != 0)
                _lastTimeChange = new Timestamp((int) GetTime(), 1);

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
            _lastTimeChange = new Timestamp((int) GetTime());

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
                return (uint) _lastTimeChange.Milliseconds;

            if (IsPaused)
                return (uint) Math.Max((int) (_playbackRate * (_pauseStartTime - _startTime)), 0);

            if (UseAudioSync)
            {
                foreach (var t in _tracks.OfType<AudioTrack>())
                {
                    if (!t.EndOfTrack)
                    {
                        uint time = t.RunningTime;

                        if (time != 0)
                            return (uint) (time + _lastTimeChange.Milliseconds);
                    }
                }
            }

            return (uint) Math.Max(_playbackRate * (Environment.TickCount - _startTime), 0);
        }

        private VideoTrack FindNextVideoTrack()
        {
            _nextVideoTrack = null;
            uint bestTime = 0xFFFFFFFF;

            foreach (var t in _tracks)
            {
                if (t.TrackType == TrackType.Video && !t.EndOfTrack)
                {
                    VideoTrack track = (VideoTrack) t;
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