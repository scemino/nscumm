//
//  DefaultAudioCDManager.cs
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

using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.Audio;

namespace NScumm.Scumm.Audio
{
    class DefaultAudioCDManager: IAudioCDManager
    {
        public bool IsPlaying
        {
            get
            {
                if (_emulating)
                {
                    // Audio CD emulation
                    return _mixer.IsSoundHandleActive(_handle);
                }
                else
                {
                    // Real Audio CD
                    return PollCD();
                }
            }
        }

        public int Volume
        {
            set
            {
                _cd.volume = value;
                if (_emulating)
                {
                    // Audio CD emulation
                    if (_mixer.IsSoundHandleActive(_handle))
                        _mixer.SetChannelVolume(_handle, _cd.volume);
                }
                else
                {
                    // Real Audio CD

                    // Unfortunately I can't implement this atm
                    // since SDL doesn't seem to offer an interface method for this.

                    // g_system->setVolumeCD(_cd.volume);
                }
            }
            get{ return _cd.volume; }
        }

        public int Balance
        {
            set
            {
                _cd.balance = value;
                if (_emulating)
                {
                    // Audio CD emulation
                    if (IsPlaying)
                        _mixer.SetChannelBalance(_handle, _cd.balance);
                }
                else
                {
                    // Real Audio CD

                    // Unfortunately I can't implement this atm
                    // since SDL doesn't seem to offer an interface method for this.

                    // g_system->setBalanceCD(_cd.balance);
                }
            }
            get{ return _cd.volume; }
        }

        public DefaultAudioCDManager(ScummEngine vm, IMixer mixer)
        {
            _vm = vm;
            _cd.playing = false;
            _cd.track = 0;
            _cd.start = 0;
            _cd.duration = 0;
            _cd.numLoops = 0;
            _cd.volume = Mixer.MaxChannelVolume;
            _cd.balance = 0;
            _mixer = mixer;
            _emulating = false;
            Debug.Assert(_mixer != null);
        }

        public void Play(int track, int numLoops, int startFrame, int duration, bool only_emulate)
        {
            if (numLoops != 0 || startFrame != 0)
            {
                _cd.track = track;
                _cd.numLoops = numLoops;
                _cd.start = startFrame;
                _cd.duration = duration;

                // Try to load the track from a compressed data file, and if found, use
                // that. If not found, attempt to start regular Audio CD playback of
                // the requested track.
                string[] trackName = new string[2];
                trackName[0] = string.Format("track{0}", track);
                trackName[1] = string.Format("track{0:00}", track);
                ISeekableAudioStream stream = null;
                var directory = ServiceLocator.FileStorage.GetDirectoryName(_vm.Game.Path);
                for (int i = 0; stream == null && i < 2; ++i)
                {
                    var path = ScummHelper.LocatePath(directory, trackName[i]);
                    if (path != null)
                    {
                        // TODO: open stream
                    }
                }

                // Stop any currently playing emulated track
                _mixer.StopHandle(_handle);

                if (stream != null)
                {
                    var start = new Timestamp(0, startFrame, 75);
                    var end = duration != 0 ? new Timestamp(0, startFrame + duration, 75) : stream.Length;

                    /*
            FIXME: Seems numLoops == 0 and numLoops == 1 both indicate a single repetition,
            while all other positive numbers indicate precisely the number of desired
            repetitions. Finally, -1 means infinitely many
            */
                    _emulating = true;
                    _handle = _mixer.PlayStream(SoundType.Music, LoopingAudioStream.Create(stream, start, end, (numLoops < 1) ? numLoops + 1 : numLoops), -1, _cd.volume, _cd.balance);
                }
                else
                {
                    _emulating = false;
                    if (!only_emulate)
                        PlayCD(track, numLoops, startFrame, duration);
                }
            }
        }

        public void Stop() {
            if (_emulating)
            {
                // Audio CD emulation
                _mixer.StopHandle(_handle);
                _emulating = false;
            }
            else
            {
                // Real Audio CD
                StopCD();
            }
        }

        public void PlayCD(int track, int num_loops, int start_frame, int duration)
        {
        }

        public bool PollCD()
        {
            return false;
        }

        public void StopCD()
        {
        }

        public void Update()
        {
            if (_emulating)
            {
                // Check whether the audio track stopped playback
                if (!_mixer.IsSoundHandleActive(_handle))
                {
                    // FIXME: We do not update the numLoops parameter here (and in fact,
                    // currently can't do that). Luckily, only one engine ever checks
                    // this part of the AudioCD status, namely the SCUMM engine; and it
                    // only checks whether the track is currently set to infinite looping
                    // or not.
                    _emulating = false;
                }
            }
            else
            {
                UpdateCD();
            }
        }

        public void UpdateCD()
        {
        }

        public AudioCdStatus GetStatus()
        {
            AudioCdStatus info = _cd;
            info.playing = IsPlaying;
            return info;
        }

        ScummEngine _vm;
        SoundHandle _handle=new SoundHandle();
        bool _emulating;

        AudioCdStatus _cd;
        IMixer _mixer;
    }
}
