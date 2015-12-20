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
using NScumm.Core.Audio;

namespace NScumm.Core.Video
{
    internal abstract class AudioTrack : Track
    {
        private byte _volume;
        private SoundHandle _handle;
        private bool _muted;
        private sbyte _balance;

        public AudioTrack()
        {
            _volume = Audio.Mixer.MaxChannelVolume;
        }

        public override TrackType TrackType
        {
            get { return TrackType.Audio; }
        }
        public override bool EndOfTrack
        {
            get
            {
                IAudioStream stream = AudioStream;
                return stream == null || !Mixer.IsSoundHandleActive(_handle) || stream.IsEndOfData;
            }
        }

        public abstract IAudioStream AudioStream { get; }

        public abstract IMixer Mixer { get; }

        public byte Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;

                if (Mixer.IsSoundHandleActive(_handle))
                    Mixer.SetChannelVolume(_handle, _muted ? 0 : _volume);
            }
        }

        public sbyte Balance
        {
            get { return _balance; }
            set
            {
                _balance = value;

                if (Mixer.IsSoundHandleActive(_handle))
                    Mixer.SetChannelBalance(_handle, _balance);
            }
        }

        public virtual SoundType SoundType
        {
            get { return SoundType.Plain; }
        }

        public uint RunningTime
        {
            get
            {
                if (Mixer.IsSoundHandleActive(_handle))
                    return (uint)Mixer.GetSoundElapsedTime(_handle);
                return 0;
            }
        }

        public bool Mute
        {
            get { return _muted; }
            set
            {
                // Update the mute settings, if required
                if (_muted != value)
                {
                    _muted = value;

                    if (Mixer.IsSoundHandleActive(_handle))
                        Mixer.SetChannelVolume(_handle, value ? 0 : _volume);
                }
            }
        }

        public void Start()
        {
            Stop();

            var stream = AudioStream;
            if (stream == null) throw new InvalidOperationException("stream should not be null");

            _handle = Mixer.PlayStream(SoundType, stream, -1, _muted ? 0 : Volume, Balance, false);

            // Pause the audio again if we're still paused
            if (IsPaused)
                Mixer.PauseHandle(_handle, true);
        }

        // TODO:
        public void Start(Timestamp limit)
        {
            Stop();
            throw new NotImplementedException();
            var stream = AudioStream;
            if (stream == null) throw new InvalidOperationException("stream should not be null");

            //    stream = new LimitingAudioStream(stream, limit, false);

            _handle = Mixer.PlayStream(SoundType, stream, -1, _muted ? 0 : Volume, Balance, true);

            // Pause the audio again if we're still paused
            if (IsPaused)
                Mixer.PauseHandle(_handle, true);
        }

        public void Stop()
        {
            Mixer.StopHandle(_handle);
        }
    }
}