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
using System;
using System.Diagnostics;

namespace NScumm.Core.Audio.IMuse
{
    partial class IMuseDigital
    {
        int AllocSlot(int priority)
        {
            int l, lowest_priority = 127;
            int trackId = -1;

            for (l = 0; l < MAX_DIGITAL_TRACKS; l++)
            {
                if (!_track[l].used)
                {
                    trackId = l;
                    break;
                }
            }

            if (trackId == -1)
            {
                Debug.WriteLine("IMuseDigital::allocSlot(): All slots are full");
                for (l = 0; l < MAX_DIGITAL_TRACKS; l++)
                {
                    var track = _track[l];
                    if (track.used && !track.toBeRemoved &&
                        (lowest_priority > track.soundPriority) && !track.souStreamUsed)
                    {
                        lowest_priority = track.soundPriority;
                        trackId = l;
                    }
                }
                if (lowest_priority <= priority)
                {
                    Debug.Assert(trackId != -1);
                    var track = _track[trackId];

                    // Stop the track immediately
                    _mixer.StopHandle(track.mixChanHandle);
                    if (track.soundDesc != null)
                    {
                        _sound.CloseSound(track.soundDesc);
                    }

                    // Mark it as unused
                    track.Clear();

                    Debug.WriteLine("IMuseDigital::allocSlot(): Removed sound {0} from track {1}", _track[trackId].soundId, trackId);
                }
                else
                {
                    Debug.WriteLine("IMuseDigital::allocSlot(): Priority sound too low");
                    return -1;
                }
            }

            return trackId;
        }

        void StartSound(int soundId, string soundName, int soundType, int volGroupId, IAudioStream input, int hookId, int volume, int priority, Track otherTrack)
        {
            lock (_mutex)
            {
                Debug.WriteLine("IMuseDigital::StartSound({0}) - begin func", soundId);

                int l = AllocSlot(priority);
                if (l == -1)
                {
                    Console.Error.WriteLine("IMuseDigital::startSound() Can't start sound - no free slots");
                    return;
                }
                Debug.WriteLine("IMuseDigital::startSound({0}, trackId:{1})", soundId, l);

                var track = _track[l];

                // Reset the track
                track.Clear();

                track.pan = 64;
                track.vol = volume * 1000;
                track.soundId = soundId;
                track.volGroupId = volGroupId;
                track.curHookId = hookId;
                track.soundPriority = priority;
                track.curRegion = -1;
                track.soundType = soundType;
                track.trackId = l;

                int bits = 0, freq = 0, channels = 0;

                track.souStreamUsed = (input != null);

                if (track.souStreamUsed)
                {
                    track.mixChanHandle = _mixer.PlayStream(track.GetSoundType(), input, -1, track.Volume, track.Pan,
                        true, false, track.mixerFlags.HasFlag(AudioFlags.Stereo));
                }
                else
                {
                    track.soundName = soundName;
                    track.soundDesc = _sound.OpenSound(soundId, soundName, soundType, volGroupId, -1);
                    if (track.soundDesc == null)
                        track.soundDesc = _sound.OpenSound(soundId, soundName, soundType, volGroupId, 1);
                    if (track.soundDesc == null)
                        track.soundDesc = _sound.OpenSound(soundId, soundName, soundType, volGroupId, 2);

                    if (track.soundDesc == null)
                        return;

                    track.sndDataExtComp = _sound.IsSndDataExtComp(track.soundDesc);

                    bits = _sound.GetBits(track.soundDesc);
                    channels = _sound.GetChannels(track.soundDesc);
                    freq = _sound.GetFreq(track.soundDesc);

                    if ((soundId == Sound.TalkSoundID) && (soundType == IMUSE_BUNDLE))
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

                    track.feedSize = freq * channels;
                    if (channels == 2)
                        track.mixerFlags = AudioFlags.Stereo;

                    if ((bits == 12) || (bits == 16))
                    {
                        track.mixerFlags |= AudioFlags.Is16Bits;
                        track.feedSize *= 2;
                    }
                    else if (bits == 8)
                    {
                        track.mixerFlags |= AudioFlags.Unsigned;
                    }
                    else
                        Console.Error.WriteLine("IMuseDigital::startSound(): Can't handle {0} bit samples", bits);

                    if (otherTrack != null && otherTrack.used && !otherTrack.toBeRemoved)
                    {
                        track.curRegion = otherTrack.curRegion;
                        track.dataOffset = otherTrack.dataOffset;
                        track.regionOffset = otherTrack.regionOffset;
                        track.dataMod12Bit = otherTrack.dataMod12Bit;
                    }

                    track.stream = new QueuingAudioStream(freq, track.mixerFlags.HasFlag(AudioFlags.Stereo));
                    track.mixChanHandle = _mixer.PlayStream(track.GetSoundType(), track.stream, -1, track.Volume, track.Pan,
                        true, false, track.mixerFlags.HasFlag(AudioFlags.Stereo));
                }

                track.used = true;
            }
        }

        Track CloneToFadeOutTrack(Track track, int fadeDelay)
        {
            Track fadeTrack;

            Debug.WriteLine("cloneToFadeOutTrack(soundId:{0}, fade:{1}) - begin of func", track.trackId, fadeDelay);

            if (track.toBeRemoved)
            {
                Console.Error.WriteLine("cloneToFadeOutTrack: Tried to clone a track to be removed, please bug report");
                return null;
            }

            Debug.Assert(track.trackId < MAX_DIGITAL_TRACKS);
            fadeTrack = _track[track.trackId + MAX_DIGITAL_TRACKS];

            if (fadeTrack.used)
            {
                Debug.WriteLine("cloneToFadeOutTrack: No free fade track, force flush fade soundId:{0}", fadeTrack.soundId);
                FlushTrack(fadeTrack);
                _mixer.StopHandle(fadeTrack.mixChanHandle);
            }

            // Clone the settings of the given track
            fadeTrack = track.Clone();
            fadeTrack.trackId = track.trackId + MAX_DIGITAL_TRACKS;

            // Clone the sound.
            // leaving bug number for now #1635361
            var soundDesc = _sound.CloneSound(track.soundDesc);
            if (soundDesc == null)
            {
                // it fail load open old song after switch to diffrent CDs
                // so gave up
                Console.Error.WriteLine("Game not supported while playing on 2 diffrent CDs");
            }
            track.soundDesc = soundDesc;

            // Set the volume fading parameters to indicate a fade out
            fadeTrack.volFadeDelay = fadeDelay;
            fadeTrack.volFadeDest = 0;
            fadeTrack.volFadeStep = (fadeTrack.volFadeDest - fadeTrack.vol) * 60 * (1000 / _callbackFps) / (1000 * fadeDelay);
            fadeTrack.volFadeUsed = true;

            // Create an appendable output buffer
            fadeTrack.stream = new QueuingAudioStream(_sound.GetFreq(fadeTrack.soundDesc), track.mixerFlags.HasFlag(AudioFlags.Stereo));
            fadeTrack.mixChanHandle = _mixer.PlayStream(track.GetSoundType(), fadeTrack.stream, -1, fadeTrack.Volume, fadeTrack.Pan,
                true, false, track.mixerFlags.HasFlag(AudioFlags.Stereo));
            fadeTrack.used = true;

            Debug.WriteLine("CloneToFadeOutTrack() - end of func, soundId {0}, fade soundId {1}", track.soundId, fadeTrack.soundId);

            return fadeTrack;
        }

        void SelectVolumeGroup(int soundId, int volGroupId)
        {
            lock (_mutex)
            {
                Debug.WriteLine("IMuseDigital::SetGroupVolume({0}, {1})", soundId, volGroupId);
                Debug.Assert((volGroupId >= 1) && (volGroupId <= 4));

                if (volGroupId == 4)
                    volGroupId = 3;

                for (int l = 0; l < MAX_DIGITAL_TRACKS; l++)
                {
                    var track = _track[l];
                    if (track.used && !track.toBeRemoved && (track.soundId == soundId))
                    {
                        Debug.WriteLine("IMuseDigital::setVolumeGroup({0}) - setting", soundId);
                        track.volGroupId = volGroupId;
                    }
                }
            }
        }

        public void SetPriority(int soundId, int priority)
        {
            lock (_mutex)
            {
                Debug.WriteLine("IMuseDigital::setPriority({0}, {1})", soundId, priority);
                Debug.Assert((priority >= 0) && (priority <= 127));

                for (int l = 0; l < MAX_DIGITAL_TRACKS; l++)
                {
                    var track = _track[l];
                    if (track.used && !track.toBeRemoved && (track.soundId == soundId))
                    {
                        Debug.WriteLine("IMuseDigital::setPriority({0}) - setting", soundId);
                        track.soundPriority = priority;
                    }
                }
            }
        }

        public void SetVolume(int soundId, int volume)
        {
            lock (_mutex)
            {
                Debug.WriteLine("IMuseDigital::setVolume({0}, {1})", soundId, volume);

                for (int l = 0; l < MAX_DIGITAL_TRACKS; l++)
                {
                    var track = _track[l];
                    if (track.used && !track.toBeRemoved && (track.soundId == soundId))
                    {
                        Debug.WriteLine("IMuseDigital::setVolume({0}) - setting", soundId);
                        track.vol = volume * 1000;
                    }
                }
            }
        }

        void SetHookId(int soundId, int hookId)
        {
            lock (_mutex)
            {
                for (int l = 0; l < MAX_DIGITAL_TRACKS; l++)
                {
                    var track = _track[l];
                    if (track.used && !track.toBeRemoved && (track.soundId == soundId))
                    {
                        track.curHookId = hookId;
                    }
                }
            }
        }

        int GetCurMusicSoundId()
        {
            int soundId = -1;

            lock (_mutex)
            {
                for (int l = 0; l < MAX_DIGITAL_TRACKS; l++)
                {
                    var track = _track[l];
                    if (track.used && !track.toBeRemoved && (track.volGroupId == IMUSE_VOLGRP_MUSIC))
                    {
                        soundId = track.soundId;
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
                Debug.WriteLine("IMuseDigital::setPan({0}, {1})", soundId, pan);

                for (int l = 0; l < MAX_DIGITAL_TRACKS; l++)
                {
                    var track = _track[l];
                    if (track.used && !track.toBeRemoved && (track.soundId == soundId))
                    {
                        Debug.WriteLine("IMuseDigital::setPan({0}) - setting", soundId);
                        track.pan = (sbyte)pan;
                    }
                }
            }
        }

        void SetFade(int soundId, int destVolume, int delay60HzTicks)
        {
            lock (_mutex)
            {
                Debug.WriteLine("IMuseDigital::setFade({0}, {1}, {2})", soundId, destVolume, delay60HzTicks);

                for (int l = 0; l < MAX_DIGITAL_TRACKS; l++)
                {
                    var track = _track[l];
                    if (track.used && !track.toBeRemoved && (track.soundId == soundId))
                    {
                        Debug.WriteLine("IMuseDigital::setFade({0}) - setting", soundId);
                        track.volFadeDelay = delay60HzTicks;
                        track.volFadeDest = destVolume * 1000;
                        track.volFadeStep = (track.volFadeDest - track.vol) * 60 * (1000 / _callbackFps) / (1000 * delay60HzTicks);
                        track.volFadeUsed = true;
                    }
                }
            }
        }

        void FadeOutMusic(int fadeDelay)
        {
            lock (_mutex)
            {
                Debug.WriteLine("IMuseDigital::fadeOutMusic(fade:{0})", fadeDelay);

                for (int l = 0; l < MAX_DIGITAL_TRACKS; l++)
                {
                    var track = _track[l];
                    if (track.used && !track.toBeRemoved && (track.volGroupId == IMuseDigital.IMUSE_VOLGRP_MUSIC))
                    {
                        Debug.WriteLine("IMuseDigital::fadeOutMusic(fade:{0}, sound:{1})", fadeDelay, track.soundId);
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
                Debug.WriteLine("IMuseDigital::fadeOutMusicAndStartNew(fade:{0}, file:{1}, sound:{2})", fadeDelay, filename, soundId);

                for (int l = 0; l < MAX_DIGITAL_TRACKS; l++)
                {
                    var track = _track[l];
                    if (track.used && !track.toBeRemoved && (track.volGroupId == IMUSE_VOLGRP_MUSIC))
                    {
                        Debug.WriteLine("IMuseDigital::fadeOutMusicAndStartNew(sound:{0}) - starting", soundId);
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
                Debug.WriteLine("IMuseDigital::SetHookIdForMusic(hookId:{0})", hookId);

                for (int l = 0; l < MAX_DIGITAL_TRACKS; l++)
                {
                    var track = _track[l];
                    if (track.used && !track.toBeRemoved && (track.volGroupId == IMUSE_VOLGRP_MUSIC))
                    {
                        Debug.WriteLine("IMuseDigital::setHookIdForMusic - setting for sound:{0}", track.soundId);
                        track.curHookId = hookId;
                        break;
                    }
                }
            }
        }

        void SetTrigger(TriggerParams trigger)
        {
            lock (_mutex)
            {
                Debug.WriteLine("IMuseDigital::setTrigger({0})", trigger.filename);

                _triggerParams = trigger;
                _triggerUsed = true;
            }
        }
    }
}

