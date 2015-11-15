//
//  IMuseDigital_Track.cs
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

namespace NScumm.Scumm.Audio.IMuse.IMuseDigital
{
    partial class IMuseDigital
    {
        int AllocSlot(int priority)
        {
            int l, lowest_priority = 127;
            int trackId = -1;

            for (l = 0; l < MaxDigitalTracks; l++)
            {
                if (!_track[l].Used)
                {
                    trackId = l;
                    break;
                }
            }

            if (trackId == -1)
            {
//                Debug.WriteLine("IMuseDigital::allocSlot(): All slots are full");
                for (l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved &&
                        (lowest_priority > track.SoundPriority) && !track.SouStreamUsed)
                    {
                        lowest_priority = track.SoundPriority;
                        trackId = l;
                    }
                }
                if (lowest_priority <= priority)
                {
                    Debug.Assert(trackId != -1);
                    var track = _track[trackId];

                    // Stop the track immediately
                    _mixer.StopHandle(track.MixChanHandle);
                    if (track.SoundDesc != null)
                    {
                        _sound.CloseSound(track.SoundDesc);
                    }

                    // Mark it as unused
                    track.Clear();

//                    Debug.WriteLine("IMuseDigital::allocSlot(): Removed sound {0} from track {1}", _track[trackId].SoundId, trackId);
                }
                else
                {
//                    Debug.WriteLine("IMuseDigital::allocSlot(): Priority sound too low");
                    return -1;
                }
            }

            return trackId;
        }

        void StartSound(int soundId, string soundName, int soundType, int volGroupId, IAudioStream input, int hookId, int volume, int priority, Track otherTrack)
        {
            int bits, freq, channels;
            lock (_mutex)
            {
//                Debug.WriteLine("IMuseDigital::StartSound({0}) - begin func", soundId);

                int l = AllocSlot(priority);
                if (l == -1)
                {
//                    Console.Error.WriteLine("IMuseDigital::startSound() Can't start sound - no free slots");
                    return;
                }
//                Debug.WriteLine("IMuseDigital::startSound({0}, trackId:{1})", soundId, l);

                var track = _track[l];

                // Reset the track
                track.Clear();

                track.pan = 64;
                track.vol = volume * 1000;
                track.SoundId = soundId;
                track.VolGroupId = volGroupId;
                track.CurHookId = hookId;
                track.SoundPriority = priority;
                track.CurRegion = -1;
                track.SoundType = soundType;
                track.TrackId = l;
                track.SouStreamUsed = (input != null);

                if (track.SouStreamUsed)
                {
                    track.MixChanHandle = _mixer.PlayStream(track.GetSoundType(), input, -1, track.Volume, track.Pan,
                        true, false, track.MixerFlags.HasFlag(AudioFlags.Stereo));
                }
                else
                {
                    track.SoundName = soundName;
                    track.SoundDesc = _sound.OpenSound(soundId, soundName, soundType, volGroupId, -1);
                    if (track.SoundDesc == null)
                        track.SoundDesc = _sound.OpenSound(soundId, soundName, soundType, volGroupId, 1);
                    if (track.SoundDesc == null)
                        track.SoundDesc = _sound.OpenSound(soundId, soundName, soundType, volGroupId, 2);

                    if (track.SoundDesc == null)
                        return;

                    track.SndDataExtComp = _sound.IsSndDataExtComp(track.SoundDesc);

                    bits = _sound.GetBits(track.SoundDesc);
                    channels = _sound.GetChannels(track.SoundDesc);
                    freq = _sound.GetFreq(track.SoundDesc);

                    if ((soundId == Sound.TalkSoundID) && (soundType == ImuseBundle))
                    {
                        if (_vm._actorToPrintStrFor != 0xFF && _vm._actorToPrintStrFor != 0)
                        {
                            var a = _vm.Actors[_vm._actorToPrintStrFor];
                            freq = (freq * a._talkFrequency) / 256;
                            track.pan = (sbyte)a._talkPan;
                            track.vol = a._talkVolume * 1000;
                        }

                        // The volume is set to zero, when using subtitles only setting in COMI
                        // TODO: vs ?
//                        if (ConfMan.getBool("speech_mute") || _vm.VAR(_vm.VAR_VOICE_MODE) == 2)
//                        {
//                            track.vol = 0;
//                        }
                    }

                    Debug.Assert(bits == 8 || bits == 12 || bits == 16);
                    Debug.Assert(channels == 1 || channels == 2);
                    Debug.Assert(0 < freq && freq <= 65535);

                    track.FeedSize = freq * channels;
                    if (channels == 2)
                        track.MixerFlags = AudioFlags.Stereo;

                    if ((bits == 12) || (bits == 16))
                    {
                        track.MixerFlags |= AudioFlags.Is16Bits;
                        track.FeedSize *= 2;
                    }
                    else if (bits == 8)
                    {
                        track.MixerFlags |= AudioFlags.Unsigned;
                    }
//                    else
//                        Console.Error.WriteLine("IMuseDigital::startSound(): Can't handle {0} bit samples", bits);

                    if (otherTrack != null && otherTrack.Used && !otherTrack.ToBeRemoved)
                    {
                        track.CurRegion = otherTrack.CurRegion;
                        track.DataOffset = otherTrack.DataOffset;
                        track.RegionOffset = otherTrack.RegionOffset;
                        track.DataMod12Bit = otherTrack.DataMod12Bit;
                    }

                    track.Stream = new QueuingAudioStream(freq, track.MixerFlags.HasFlag(AudioFlags.Stereo));
                    track.MixChanHandle = _mixer.PlayStream(track.GetSoundType(), track.Stream, -1, track.Volume, track.Pan,
                        true, false, track.MixerFlags.HasFlag(AudioFlags.Stereo));
                }

                track.Used = true;
            }
        }

        Track CloneToFadeOutTrack(Track track, int fadeDelay)
        {
            Track fadeTrack;

//            Debug.WriteLine("cloneToFadeOutTrack(soundId:{0}, fade:{1}) - begin of func", track.TrackId, fadeDelay);

            if (track.ToBeRemoved)
            {
//                Console.Error.WriteLine("cloneToFadeOutTrack: Tried to clone a track to be removed, please bug report");
                return null;
            }

            Debug.Assert(track.TrackId < MaxDigitalTracks);
            fadeTrack = _track[track.TrackId + MaxDigitalTracks];

            if (fadeTrack.Used)
            {
//                Debug.WriteLine("cloneToFadeOutTrack: No free fade track, force flush fade soundId:{0}", fadeTrack.SoundId);
                FlushTrack(fadeTrack);
                _mixer.StopHandle(fadeTrack.MixChanHandle);
            }

            // Clone the settings of the given track
            fadeTrack = track.Clone();
            _track[track.TrackId + MaxDigitalTracks] = fadeTrack;
            fadeTrack.TrackId = track.TrackId + MaxDigitalTracks;

            // Clone the sound.
            // leaving bug number for now #1635361
            var soundDesc = _sound.CloneSound(track.SoundDesc);
            if (soundDesc == null)
            {
                // it fail load open old song after switch to diffrent CDs
                // so gave up
//                Console.Error.WriteLine("Game not supported while playing on 2 diffrent CDs");
            }
            track.SoundDesc = soundDesc;

            // Set the volume fading parameters to indicate a fade out
            fadeTrack.VolFadeDelay = fadeDelay;
            fadeTrack.VolFadeDest = 0;
            fadeTrack.VolFadeStep = (fadeTrack.VolFadeDest - fadeTrack.vol) * 60 * (1000 / _callbackFps) / (1000 * fadeDelay);
            fadeTrack.VolFadeUsed = true;

            // Create an appendable output buffer
            fadeTrack.Stream = new QueuingAudioStream(_sound.GetFreq(fadeTrack.SoundDesc), track.MixerFlags.HasFlag(AudioFlags.Stereo));
            fadeTrack.MixChanHandle = _mixer.PlayStream(track.GetSoundType(), fadeTrack.Stream, -1, fadeTrack.Volume, fadeTrack.Pan,
                true, false, track.MixerFlags.HasFlag(AudioFlags.Stereo));
            fadeTrack.Used = true;

//            Debug.WriteLine("CloneToFadeOutTrack() - end of func, soundId {0}, fade soundId {1}", track.SoundId, fadeTrack.SoundId);

            return fadeTrack;
        }

        void SelectVolumeGroup(int soundId, int volGroupId)
        {
            lock (_mutex)
            {
//                Debug.WriteLine("IMuseDigital::SetGroupVolume({0}, {1})", soundId, volGroupId);
                Debug.Assert((volGroupId >= 1) && (volGroupId <= 4));

                if (volGroupId == 4)
                    volGroupId = 3;

                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.SoundId == soundId))
                    {
//                        Debug.WriteLine("IMuseDigital::setVolumeGroup({0}) - setting", soundId);
                        track.VolGroupId = volGroupId;
                    }
                }
            }
        }

        public void SetPriority(int soundId, int priority)
        {
            lock (_mutex)
            {
//                Debug.WriteLine("IMuseDigital::setPriority({0}, {1})", soundId, priority);
                Debug.Assert((priority >= 0) && (priority <= 127));

                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.SoundId == soundId))
                    {
//                        Debug.WriteLine("IMuseDigital::setPriority({0}) - setting", soundId);
                        track.SoundPriority = priority;
                    }
                }
            }
        }

        public void SetVolume(int soundId, int volume)
        {
            lock (_mutex)
            {
//                Debug.WriteLine("IMuseDigital::setVolume({0}, {1})", soundId, volume);

                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.SoundId == soundId))
                    {
//                        Debug.WriteLine("IMuseDigital::setVolume({0}) - setting", soundId);
                        track.vol = volume * 1000;
                    }
                }
            }
        }

        void SetHookId(int soundId, int hookId)
        {
            lock (_mutex)
            {
                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.SoundId == soundId))
                    {
                        track.CurHookId = hookId;
                    }
                }
            }
        }

        int GetCurMusicSoundId()
        {
            int soundId = -1;

            lock (_mutex)
            {
                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.VolGroupId == ImuseVolumeGroupMusic))
                    {
                        soundId = track.SoundId;
                        break;
                    }
                }
            }

            return soundId;
        }

        public void SetPan(int soundId, int pan)
        {
            lock (_mutex)
            {
//                Debug.WriteLine("IMuseDigital::setPan({0}, {1})", soundId, pan);

                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.SoundId == soundId))
                    {
//                        Debug.WriteLine("IMuseDigital::setPan({0}) - setting", soundId);
                        track.pan = (sbyte)pan;
                    }
                }
            }
        }

        void SetFade(int soundId, int destVolume, int delay60HzTicks)
        {
            lock (_mutex)
            {
//                Debug.WriteLine("IMuseDigital::setFade({0}, {1}, {2})", soundId, destVolume, delay60HzTicks);

                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.SoundId == soundId))
                    {
//                        Debug.WriteLine("IMuseDigital::setFade({0}) - setting", soundId);
                        track.VolFadeDelay = delay60HzTicks;
                        track.VolFadeDest = destVolume * 1000;
                        track.VolFadeStep = (track.VolFadeDest - track.vol) * 60 * (1000 / _callbackFps) / (1000 * delay60HzTicks);
                        track.VolFadeUsed = true;
                    }
                }
            }
        }

        void FadeOutMusic(int fadeDelay)
        {
            lock (_mutex)
            {
//                Debug.WriteLine("IMuseDigital::fadeOutMusic(fade:{0})", fadeDelay);

                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.VolGroupId == IMuseDigital.ImuseVolumeGroupMusic))
                    {
//                        Debug.WriteLine("IMuseDigital::fadeOutMusic(fade:{0}, sound:{1})", fadeDelay, track.SoundId);
                        CloneToFadeOutTrack(track, fadeDelay);
                        FlushTrack(track);
                        break;
                    }
                }
            }
        }

        void FadeOutMusicAndStartNew(int fadeDelay, string filename, int soundId)
        {
            lock (_mutex)
            {
//                Debug.WriteLine("IMuseDigital::fadeOutMusicAndStartNew(fade:{0}, file:{1}, sound:{2})", fadeDelay, filename, soundId);

                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.VolGroupId == ImuseVolumeGroupMusic))
                    {
//                        Debug.WriteLine("IMuseDigital::fadeOutMusicAndStartNew(sound:{0}) - starting", soundId);
                        StartMusicWithOtherPos(filename, soundId, 0, 127, track);
                        CloneToFadeOutTrack(track, fadeDelay);
                        FlushTrack(track);
                        break;
                    }
                }
            }
        }

        void SetHookIdForMusic(int hookId)
        {
            lock (_mutex)
            {
//                Debug.WriteLine("IMuseDigital::SetHookIdForMusic(hookId:{0})", hookId);

                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.VolGroupId == ImuseVolumeGroupMusic))
                    {
//                        Debug.WriteLine("IMuseDigital::setHookIdForMusic - setting for sound:{0}", track.SoundId);
                        track.CurHookId = hookId;
                        break;
                    }
                }
            }
        }

        void SetTrigger(TriggerParams trigger)
        {
            lock (_mutex)
            {
//                Debug.WriteLine("IMuseDigital::setTrigger({0})", trigger.Filename);

                _triggerParams = trigger;
                _triggerUsed = true;
            }
        }
    }
}

