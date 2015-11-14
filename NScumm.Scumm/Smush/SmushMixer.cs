//
//  SmushMixer.cs
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

using System;
using NScumm.Core;
using NScumm.Core.Audio;

namespace NScumm.Scumm.Smush
{
    class SmushMixer: IEnableTrace
    {
        public SmushMixer(IMixer mixer)
        {
            _mixer = mixer;
            for (var i = 0; i < _channels.Length; i++)
            {
                _channels[i] = new Channel();
                _channels[i].Id = -1;
                _channels[i].Chan = null;
                _channels[i].Stream = null;
            }
        }

        public bool Stop()
        {
            this.Trace().Write(TraceSwitches.Music, "SmushMixer::stop()");
            for (int i = 0; i < NumChannels; i++)
            {
                if (_channels[i].Id != -1)
                {
                    _channels[i].Id = -1;
                    _channels[i].Chan = null;
                    if (_channels[i].Stream != null)
                    {
                        _channels[i].Stream.Finish();
                        _channels[i].Stream = null;
                    }
                }
            }

            return true;
        }

        public bool HandleFrame()
        {
            this.Trace().Write(TraceSwitches.Music, "SmushMixer::handleFrame()");
            for (int i = 0; i < _channels.Length; i++)
            {
                if (_channels[i].Id != -1)
                {
                    if (_channels[i].Chan.IsTerminated)
                    {
                        _channels[i].Id = -1;
                        _channels[i].Chan = null;
                        if (_channels[i].Stream != null)
                        {
                            _channels[i].Stream.Finish();
                            _channels[i].Stream = null;
                        }
                    }
                    else
                    {
                        int vol, pan;
                        bool stereo, is_16bit;

                        _channels[i].Chan.GetParameters(out stereo, out is_16bit, out vol, out pan);

                        // Grab the audio data from the channel
                        var size = _channels[i].Chan.AvailableSoundDataSize;
                        var data = _channels[i].Chan.GetSoundData();

                        var flags = stereo ? AudioFlags.Stereo : AudioFlags.None;
                        if (is_16bit)
                        {
                            flags |= AudioFlags.Is16Bits;
                        }
                        else
                        {
                            flags |= AudioFlags.Unsigned;
                        }

                        if (_mixer.IsReady)
                        {
                            // Stream the data
                            if (_channels[i].Stream == null)
                            {
                                _channels[i].Stream = new QueuingAudioStream(_channels[i].Chan.Rate, stereo);
                                _channels[i].Handle = _mixer.PlayStream(SoundType.SFX, _channels[i].Stream);
                            }
                            _mixer.SetChannelVolume(_channels[i].Handle, vol);
                            _mixer.SetChannelBalance(_channels[i].Handle, pan);
                            _channels[i].Stream.QueueBuffer(data, size, true, flags);  // The stream will free the buffer for us
                        }
                        else
                            data = null;
                    }
                }
            }
            return true;
        }

        public SmushChannel FindChannel(int track)
        {
            this.Trace().Write(TraceSwitches.Music, "SmushMixer.FindChannel({0})", track);
            for (var i = 0; i < _channels.Length; i++)
            {
                if (_channels[i].Id == track)
                    return _channels[i].Chan;
            }
            return null;
        }

        public void AddChannel(SmushChannel c)
        {
            var track = c.TrackIdentifier;

            this.Trace().Write(TraceSwitches.Music, "SmushMixer.AddChannel({0})", track);

            for (var i = 0; i < _channels.Length; i++)
            {
                if (_channels[i].Id == track)
                    this.Trace().Write(TraceSwitches.Music, "SmushMixer.AddChannel({0}): channel already exists", track);
            }

            for (var i = 0; i < _channels.Length; i++)
            {
                if ((_channels[i].Chan == null || _channels[i].Id == -1) && !_mixer.IsSoundHandleActive(_channels[i].Handle))
                {
                    _channels[i].Chan = c;
                    _channels[i].Id = track;
                    return;
                }
            }

            for (var i = 0; i < _channels.Length; i++)
            {
                this.Trace().Write(TraceSwitches.Music, "channel {0} : {1}({2}, {3})", i, _channels[i].Chan,
                    _channels[i].Chan != null ? _channels[i].Chan.TrackIdentifier : -1,
                    _channels[i].Chan == null || _channels[i].Chan.IsTerminated);
            }

            throw new InvalidOperationException(string.Format("SmushMixer::addChannel({0}): no channel available", track));
        }

        public bool Flush()
        {
            this.Trace().Write(TraceSwitches.Music, "SmushMixer::flush()");
            for (int i = 0; i < NumChannels; i++)
            {
                if (_channels[i].Id != -1)
                {
                    if (_channels[i].Stream != null && _channels[i].Stream.IsEndOfStream)
                    {
                        _mixer.StopHandle(_channels[i].Handle);
                        _channels[i].Id = -1;
                        _channels[i].Chan = null;
                        _channels[i].Stream = null;
                    }
                }
            }

            return true;
        }

        class Channel
        {
            public int Id;
            public SmushChannel Chan;
            public SoundHandle Handle = new SoundHandle();
            public QueuingAudioStream Stream;
        }

        const int NumChannels = 16;
        IMixer _mixer;
        Channel[] _channels = new Channel[NumChannels];
    }
}

