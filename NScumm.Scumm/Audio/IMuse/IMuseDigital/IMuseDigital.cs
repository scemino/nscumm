//
//  IMuseDigital.cs
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
using System.Threading;
using NScumm.Core.Audio;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Audio.IMuse.IMuseDigital
{
    partial class IMuseDigital: IMusicEngine
    {
        public IMuseDigital(ScummEngine7 scumm, IMixer mixer, int fps)
        {
            _vm = scumm;
            _mixer = mixer;

            _pause = false;
            _sound = new ImuseDigiSndMgr(_vm);
            Debug.Assert(_sound != null);
            _callbackFps = fps;
            ResetState();
            for (int l = 0; l < MaxDigitalTracks + MaxDigitalFadeTracks; l++)
            {
                _track[l] = new Track();
                _track[l].TrackId = l;
            }

            _timer = new Timer(new TimerCallback(o => Callback()), this, TimeSpan.Zero, TimeSpan.FromMilliseconds(1000 / _callbackFps));

            _audioNames = null;
            _numAudioNames = 0;
        }

        void IMusicEngine.SetMusicVolume(int vol)
        {
        }

        void IMusicEngine.StartSound(int sound)
        {
//            Console.Error.WriteLine("IMuseDigital::startSound(int) should be never called"); 
        }

        int IMusicEngine.GetMusicTimer()
        {
            return 0;
        }

        public void SaveOrLoad(Serializer serializer)
        {
            // TODO: vs
        }

        static AudioFlags MakeMixerFlags(Track track)
        {
            var flags = track.MixerFlags;
            var mixerFlags = AudioFlags.None;
            if (flags.HasFlag(AudioFlags.Unsigned))
                mixerFlags |= AudioFlags.Unsigned;
            if (flags.HasFlag(AudioFlags.Is16Bits))
                mixerFlags |= AudioFlags.Is16Bits;


            if (track.SndDataExtComp)
                mixerFlags |= AudioFlags.LittleEndian;

            if (flags.HasFlag(AudioFlags.Stereo))
                mixerFlags |= AudioFlags.Stereo;
            return mixerFlags;
        }

        void ResetState()
        {
            _curMusicState = 0;
            _curMusicSeq = 0;
            _curMusicCue = 0;
            Array.Clear(_attributes, 0, _attributes.Length);
            _nextSeqToPlay = 0;
            _stopingSequence = 0;
            RadioChatterSFX = false;
            _triggerUsed = false;
        }

        void Callback()
        {
            lock (_mutex)
            {
                for (int l = 0; l < MaxDigitalTracks + MaxDigitalFadeTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used)
                    {
                        // Ignore tracks which are about to finish. Also, if it did finish in the meantime,
                        // mark it as unused.
                        if (track.Stream == null)
                        {
                            if (!_mixer.IsSoundHandleActive(track.MixChanHandle))
                                _track[l].Clear();
                            continue;
                        }

                        if (_pause)
                            return;

                        if (track.VolFadeUsed)
                        {
                            if (track.VolFadeStep < 0)
                            {
                                if (track.vol > track.VolFadeDest)
                                {
                                    track.vol += track.VolFadeStep;
                                    if (track.vol < track.VolFadeDest)
                                    {
                                        track.vol = track.VolFadeDest;
                                        track.VolFadeUsed = false;
                                    }
                                    if (track.vol == 0)
                                    {
                                        // Fade out complete . remove this track
                                        FlushTrack(track);
                                        continue;
                                    }
                                }
                            }
                            else if (track.VolFadeStep > 0)
                            {
                                if (track.vol < track.VolFadeDest)
                                {
                                    track.vol += track.VolFadeStep;
                                    if (track.vol > track.VolFadeDest)
                                    {
                                        track.vol = track.VolFadeDest;
                                        track.VolFadeUsed = false;
                                    }
                                }
                            }
//                            Debug.WriteLine("Fade: sound({0}), Vol({1})", track.SoundId, track.vol / 1000);
                        }

                        if (!track.SouStreamUsed)
                        {
                            Debug.Assert(track.Stream != null);
                            byte[] tmpSndBufferPtr = null;
                            int curFeedSize = 0;

                            if (track.CurRegion == -1)
                            {
                                SwitchToNextRegion(track);
                                if (track.Stream == null) // Seems we reached the end of the stream
                                continue;
                            }

                            int bits = _sound.GetBits(track.SoundDesc);
                            int channels = _sound.GetChannels(track.SoundDesc);

                            int feedSize = track.FeedSize / _callbackFps;

                            if (track.Stream.IsEndOfData)
                            {
                                feedSize *= 2;
                            }

                            if ((bits == 12) || (bits == 16))
                            {
                                if (channels == 1)
                                    feedSize &= ~1;
                                if (channels == 2)
                                    feedSize &= ~3;
                            }
                            else if (bits == 8)
                            {
                                if (channels == 2)
                                    feedSize &= ~1;
                            }
                            else
                            {
//                                Console.Error.WriteLine("IMuseDigita::callback: Unexpected sample width, {0} bits", bits);
                                continue;
                            }

                            if (feedSize == 0)
                                continue;

                            do
                            {
                                switch (bits)
                                {
                                    case 12:
                                        byte[] tmpPtr;
                                        feedSize += track.DataMod12Bit;
                                        int tmpFeedSize12Bits = (feedSize * 3) / 4;
                                        int tmpLength12Bits = (tmpFeedSize12Bits / 3) * 4;
                                        track.DataMod12Bit = feedSize - tmpLength12Bits;
                                        int tmpOffset = (track.RegionOffset * 3) / 4;
                                        int tmpFeedSize = _sound.GetDataFromRegion(track.SoundDesc, track.CurRegion, out tmpPtr, tmpOffset, tmpFeedSize12Bits);
                                        curFeedSize = BundleCodecs.Decode12BitsSample(tmpPtr, out tmpSndBufferPtr, tmpFeedSize);
                                        break;
                                    case 16:
                                        curFeedSize = _sound.GetDataFromRegion(track.SoundDesc, track.CurRegion, out tmpSndBufferPtr, track.RegionOffset, feedSize);
                                        if (channels == 1)
                                        {
                                            curFeedSize &= ~1;
                                        }
                                        if (channels == 2)
                                        {
                                            curFeedSize &= ~3;
                                        }
                                        break;
                                    case 8:
                                        curFeedSize = _sound.GetDataFromRegion(track.SoundDesc, track.CurRegion, out tmpSndBufferPtr, track.RegionOffset, feedSize);
                                        if (RadioChatterSFX && track.SoundId == 10000)
                                        {
                                            if (curFeedSize > feedSize)
                                                curFeedSize = feedSize;
                                            var buf = new byte[curFeedSize];
                                            int index = 0;
                                            int count = curFeedSize - 4;
                                            var ptr_1 = 0;
                                            var ptr_2 = 4;
                                            int value = tmpSndBufferPtr[ptr_1 + 0] - 0x80;
                                            value += tmpSndBufferPtr[ptr_1 + 1] - 0x80;
                                            value += tmpSndBufferPtr[ptr_1 + 2] - 0x80;
                                            value += tmpSndBufferPtr[ptr_1 + 3] - 0x80;
                                            do
                                            {
                                                int t = tmpSndBufferPtr[ptr_1++];
                                                int v = t - (value / 4);
                                                value = tmpSndBufferPtr[ptr_2++] - 0x80 + (value - t + 0x80);
                                                buf[index++] = (byte)(v * 2 + 0x80);
                                            }
                                            while ((--count) != 0);
                                            buf[curFeedSize - 1] = 0x80;
                                            buf[curFeedSize - 2] = 0x80;
                                            buf[curFeedSize - 3] = 0x80;
                                            buf[curFeedSize - 4] = 0x80;
                                            tmpSndBufferPtr = buf;
                                        }
                                        if (channels == 2)
                                        {
                                            curFeedSize &= ~1;
                                        }
                                        break;
                                }

                                if (curFeedSize > feedSize)
                                    curFeedSize = feedSize;

                                if (_mixer.IsReady)
                                {
                                    track.Stream.QueueBuffer(tmpSndBufferPtr, curFeedSize, true, MakeMixerFlags(track));
                                    track.RegionOffset += curFeedSize;
                                }
                                else
                                    tmpSndBufferPtr = null;

                                if (_sound.IsEndOfRegion(track.SoundDesc, track.CurRegion))
                                {
                                    SwitchToNextRegion(track);
                                    if (track.Stream == null) // Seems we reached the end of the stream
                                    break;
                                }
                                feedSize -= curFeedSize;
                                Debug.Assert(feedSize >= 0);
                            } while (feedSize != 0);
                        }
                        if (_mixer.IsReady)
                        {
                            _mixer.SetChannelVolume(track.MixChanHandle, track.Volume);
                            _mixer.SetChannelBalance(track.MixChanHandle, track.Pan);
                        }
                    }
                }
            }
        }

        void SwitchToNextRegion(Track track)
        {
            Debug.Assert(track != null);

            if (track.TrackId >= MaxDigitalTracks)
            {
                FlushTrack(track);
//                Debug.WriteLine("SwToNeReg(trackId:{0}) - fadetrack can't go next region, exiting SwToNeReg", track.TrackId);
                return;
            }

            int num_regions = _sound.GetNumRegions(track.SoundDesc);

            if (++track.CurRegion == num_regions)
            {
                FlushTrack(track);
//                Debug.WriteLine("SwToNeReg(trackId:{0}) - end of region, exiting SwToNeReg", track.TrackId);
                return;
            }

            SoundDesc soundDesc = track.SoundDesc;
            if (_triggerUsed && track.SoundDesc.NumMarkers != 0)
            {
                if (_sound.CheckForTriggerByRegionAndMarker(soundDesc, track.CurRegion, _triggerParams.Marker))
                {
//                    Debug.WriteLine("SwToNeReg(trackId:{0}) - trigger {1} reached", track.TrackId, _triggerParams.Marker);
//                    Debug.WriteLine("SwToNeReg(trackId:{0}) - exit current region {1}", track.TrackId, track.CurRegion);
//                    Debug.WriteLine("SwToNeReg(trackId:{0}) - call cloneToFadeOutTrack(delay:{1})", track.TrackId, _triggerParams.FadeOutDelay);
                    Track fadeTrack = CloneToFadeOutTrack(track, _triggerParams.FadeOutDelay);
                    if (fadeTrack != null)
                    {
                        fadeTrack.DataOffset = _sound.GetRegionOffset(fadeTrack.SoundDesc, fadeTrack.CurRegion);
                        fadeTrack.RegionOffset = 0;
//                        Debug.WriteLine("SwToNeReg(trackId:{0})-sound({1}) select region {2}, curHookId: {3}", fadeTrack.TrackId, fadeTrack.SoundId, fadeTrack.CurRegion, fadeTrack.CurHookId);
                        fadeTrack.CurHookId = 0;
                    }
                    FlushTrack(track);
                    StartMusic(_triggerParams.Filename, _triggerParams.SoundId, _triggerParams.HookId, _triggerParams.Volume);
                    _triggerUsed = false;
                    return;
                }
            }

            int jumpId = _sound.GetJumpIdByRegionAndHookId(soundDesc, track.CurRegion, track.CurHookId);
            if (jumpId != -1)
            {
                int region = _sound.GetRegionIdByJumpId(soundDesc, jumpId);
                Debug.Assert(region != -1);
                int sampleHookId = _sound.GetJumpHookId(soundDesc, jumpId);
                Debug.Assert(sampleHookId != -1);
//                Debug.WriteLine("SwToNeReg(trackId:{0}) - JUMP found - sound:{1}, track hookId:{2}, data hookId:{3}", track.TrackId, track.SoundId, track.CurHookId, sampleHookId);
                if (track.CurHookId == sampleHookId)
                {
                    int fadeDelay = (60 * _sound.GetJumpFade(soundDesc, jumpId)) / 1000;
//                    Debug.WriteLine("SwToNeReg(trackId:{0}) - sound({1}) match hookId", track.TrackId, track.SoundId);
                    if (fadeDelay != 0)
                    {
//                        Debug.WriteLine("SwToNeReg(trackId:{0}) - call cloneToFadeOutTrack(delay:{1})", track.TrackId, fadeDelay);
                        var fadeTrack = CloneToFadeOutTrack(track, fadeDelay);
                        if (fadeTrack != null)
                        {
                            fadeTrack.DataOffset = _sound.GetRegionOffset(fadeTrack.SoundDesc, fadeTrack.CurRegion);
                            fadeTrack.RegionOffset = 0;
//                            Debug.WriteLine("SwToNeReg(trackId:{0}) - sound({1}) faded track, select region {2}, curHookId: {3}", fadeTrack.TrackId, fadeTrack.SoundId, fadeTrack.CurRegion, fadeTrack.CurHookId);
                            fadeTrack.CurHookId = 0;
                        }
                    }
                    track.CurRegion = region;
//                    Debug.WriteLine("SwToNeReg(trackId:{0}) - sound({1}) jump to region {2}, curHookId: {3}", track.TrackId, track.SoundId, track.CurRegion, track.CurHookId);
                    track.CurHookId = 0;
                }
                else
                {
//                    Debug.WriteLine("SwToNeReg(trackId:{0}) - Normal switch region, sound({1}), hookId({2})", track.TrackId, track.SoundId, track.CurHookId);
                }
            }
            else
            {
//                Debug.WriteLine("SwToNeReg(trackId:{0}) - Normal switch region, sound({1}), hookId({2})", track.TrackId, track.SoundId, track.CurHookId);
            }

//            Debug.WriteLine("SwToNeReg(trackId:{0}) - sound({1}), select region {2}", track.TrackId, track.SoundId, track.CurRegion);
            track.DataOffset = _sound.GetRegionOffset(soundDesc, track.CurRegion);
            track.RegionOffset = 0;
//            Debug.WriteLine("SwToNeReg(trackId:{0}) - end of func", track.TrackId);
        }

        public bool RadioChatterSFX { get; set; }

        const int MaxDigitalTracks = 8;
        const int MaxDigitalFadeTracks = 8;

        public const int MaxImuseSounds = 16;
        public const int ImuseResource = 1;
        public const int ImuseBundle = 2;

        public const int ImuseVolumeGroupVoice = 1;
        public const int ImuseVolumeGroupSfx = 2;
        public const int ImuseVolumeGroupMusic = 3;

        int _callbackFps;
        // value how many times callback needs to be called per second

        struct TriggerParams
        {
            public string Marker;
            public int FadeOutDelay;
            public string Filename;
            public int SoundId;
            public int HookId;
            public int Volume;
        }

        Timer _timer;
        TriggerParams _triggerParams;
        bool _triggerUsed;

        Track[] _track = new Track[MaxDigitalTracks + MaxDigitalFadeTracks];

        object _mutex = new object();
        ScummEngine7 _vm;
        IMixer _mixer;
        readonly ImuseDigiSndMgr _sound;

        string[] _audioNames;
        // filenames of sound SFX used in FT
        int _numAudioNames;
        // number of above filenames

        bool _pause;
        // flag mean that iMuse callback should be idle

        int[] _attributes = new int[188];
        // internal attributes for each music file to store and check later
        int _nextSeqToPlay;
        // id of sequence type of music needed played
        int _curMusicState;
        // current or previous id of music
        int _curMusicSeq;
        // current or previous id of sequence music
        int _curMusicCue;
        // current cue for current music. used in FT
        int _stopingSequence;
    }
}

