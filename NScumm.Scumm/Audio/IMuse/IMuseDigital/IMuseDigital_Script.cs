//
//  IMuseDigital_Script.cs
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
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Audio.IMuse.IMuseDigital
{
    partial class IMuseDigital
    {
        public void ParseScriptCmds(int cmd, int b, int c, int d, int e, int f, int g, int h)
        {
            int soundId = b;
            int sub_cmd = c;

            if (cmd == 0)
                return;

            switch (cmd)
            {
                case 10: // ImuseStopAllSounds
                    StopAllSounds();
                    break;
                case 12: // ImuseSetParam
                    switch (sub_cmd)
                    {
                        case 0x400: // select group volume
                            SelectVolumeGroup(soundId, d);
                            break;
                        case 0x500: // set priority
                            SetPriority(soundId, d);
                            break;
                        case 0x600: // set volume
                            SetVolume(soundId, d);
                            break;
                        case 0x700: // set pan
                            SetPan(soundId, d);
                            break;
                        default:
//                            Console.Error.WriteLine("DoCommand SetParam DEFAULT command {0}", sub_cmd);
                            break;
                    }
                    break;
                case 14: // ImuseFadeParam
                    switch (sub_cmd)
                    {
                        case 0x600: // set volume fading
                            if ((d != 0) && (e == 0))
                                SetVolume(soundId, d);
                            else if ((d == 0) && (e == 0))
                                StopSound(soundId);
                            else
                                SetFade(soundId, d, e);
                            break;
                        default:
//                            Console.Error.WriteLine("DoCommand FadeParam DEFAULT sub command {0}", sub_cmd);
                            break;
                    }
                    break;
                case 25: // ImuseStartStream
                    this.Trace().Write(TraceSwitches.Music, "ImuseStartStream ({0}, {1}, {2})", soundId, c, d);
                    break;
                case 26: // ImuseSwitchStream
                    this.Trace().Write(TraceSwitches.Music, "ImuseSwitchStream ({0}, {1}, {2}, {3}, {4})", soundId, c, d, e, f);
                    break;
                case 0x1000: // ImuseSetState
                    //                    this.Trace().Write(TraceSwitches.Music,"ImuseSetState ({0})", b);
                    if ((_vm.Game.GameId == GameId.Dig) && (_vm.Game.Features.HasFlag(GameFeatures.Demo)))
                    {
                        if (b == 1)
                        {
                            FadeOutMusic(200);
                            StartMusic(1, 127);
                        }
                        else
                        {
                            if (GetSoundStatus(2) == 0)
                            {
                                FadeOutMusic(200);
                                StartMusic(2, 127);
                            }
                        }
                    }
                    else if ((_vm.Game.GameId == GameId.CurseOfMonkeyIsland) && (_vm.Game.Features.HasFlag(GameFeatures.Demo)))
                    {
                        switch (b)
                        {
                            case 2:
                                FadeOutMusic(108);
                                StartMusic("in1.imx", 1100, 0, 127);
                                break;
                            case 4:
                                FadeOutMusic(108);
                                StartMusic("in2.imx", 1120, 0, 127);
                                break;
                            case 8:
                                FadeOutMusic(108);
                                StartMusic("out1.imx", 1140, 0, 127);
                                break;
                            case 9:
                                FadeOutMusic(108);
                                StartMusic("out2.imx", 1150, 0, 127);
                                break;
                            case 16:
                                FadeOutMusic(108);
                                StartMusic("gun.imx", 1210, 0, 127);
                                break;
                            default:
                                FadeOutMusic(120);
                                break;
                        }
                    }
                    else if (_vm.Game.GameId == GameId.Dig)
                    {
                        SetDigMusicState(b);
                    }
                    else if (_vm.Game.GameId == GameId.CurseOfMonkeyIsland)
                    {
                        SetComiMusicState(b);
                    }
                    else if (_vm.Game.GameId == GameId.FullThrottle)
                    {
                        SetFtMusicState(b);
                    }
                    break;
                case 0x1001: // ImuseSetSequence
                    this.Trace().Write(TraceSwitches.Music, "ImuseSetSequence ({0})", b);
                    switch (_vm.Game.GameId)
                    {
                        case GameId.Dig:
                            SetDigMusicSequence(b);
                            break;
                        case GameId.CurseOfMonkeyIsland:
                            SetComiMusicSequence(b);
                            break;
                        case GameId.FullThrottle:
                            SetFtMusicSequence(b);
                            break;
                    }
                    break;
                case 0x1002: // ImuseSetCuePoint
                    this.Trace().Write(TraceSwitches.Music, "ImuseSetCuePoint ({0})", b);
                    if (_vm.Game.GameId == GameId.FullThrottle)
                    {
                        SetFtMusicCuePoint(b);
                    }
                    break;
                case 0x1003: // ImuseSetAttribute
                    this.Trace().Write(TraceSwitches.Music, "ImuseSetAttribute ({0}, {1})", b, c);
                    Debug.Assert((_vm.Game.GameId == GameId.Dig) || (_vm.Game.GameId == GameId.FullThrottle));
                    if (_vm.Game.GameId == GameId.Dig)
                    {
                        _attributes[b] = c;
                    }
                    break;
                case 0x2000: // ImuseSetGroupSfxVolume
                    break;
                case 0x2001: // ImuseSetGroupVoiceVolume
                    break;
                case 0x2002: // ImuseSetGroupMusicVolume
                    break;
                default:
                    throw new InvalidOperationException(string.Format("doCommand DEFAULT command {0}", cmd));
            }
        }

        public void FlushTracks()
        {
            lock (_mutex)
            {
                this.Trace().Write(TraceSwitches.Music, "flushTracks()");
                for (int l = 0; l < MaxDigitalTracks + MaxDigitalFadeTracks; l++)
                {
                    Track track = _track[l];
                    if (track.Used && track.ToBeRemoved && !_mixer.IsSoundHandleActive(track.MixChanHandle))
                    {
                        this.Trace().Write(TraceSwitches.Music, "flushTracks() - soundId:{0}", track.SoundId);
                        track.Clear();
                    }
                }
            }
        }

        public void StartVoice(int soundId, IAudioStream input)
        {
            this.Trace().Write(TraceSwitches.Music, "StartVoiceStream({0})", soundId);
            StartSound(soundId, "", 0, ImuseVolumeGroupVoice, input, 0, 127, 127, null);
        }

        public void StartVoice(int soundId, string soundName)
        {
            this.Trace().Write(TraceSwitches.Music, "startVoiceBundle({0}, {1})", soundName, soundId);
            StartSound(soundId, soundName, ImuseBundle, ImuseVolumeGroupVoice, null, 0, 127, 127, null);
        }

        public void StartSfx(int soundId, int priority)
        {
            this.Trace().Write(TraceSwitches.Music, "startSfx({0})", soundId);
            StartSound(soundId, "", ImuseResource, ImuseVolumeGroupSfx, null, 0, 127, priority, null);
        }

        public int GetSoundStatus(int soundId)
        {
            lock (_mutex)
            {
                this.Trace().Write(TraceSwitches.Music, "getSoundStatus({0})", soundId);
                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    // Note: We do not check track.toBeRemoved here on purpose (I *think*, at least).
                    // After all, tracks which are about to stop still are running (if only for a brief time).
                    if ((track.SoundId == soundId) && track.Used)
                    {
                        if (_mixer.IsSoundHandleActive(track.MixChanHandle))
                        {
                            return 1;
                        }
                    }
                }

                return 0;
            }
        }

        public void StopSound(int soundId)
        {
            lock (_mutex)
            {
                this.Trace().Write(TraceSwitches.Music, "stopSound({0})", soundId);
                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.SoundId == soundId))
                    {
                        this.Trace().Write(TraceSwitches.Music, "stopSound({0}) - stopping sound", soundId);
                        FlushTrack(track);
                    }
                }
            }
        }

        public int GetCurMusicPosInMs()
        {
            lock (_mutex)
            {
                int soundId = -1;

                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.VolGroupId == ImuseVolumeGroupMusic))
                    {
                        soundId = track.SoundId;
                    }
                }

                int msPos = GetPosInMs(soundId);
                this.Trace().Write(TraceSwitches.Music, "getCurMusicPosInMs({0}) = {1}", soundId, msPos);
                return msPos;
            }
        }

        public int GetCurVoiceLipSyncWidth()
        {
            lock (_mutex)
            {
                int msPos = GetPosInMs(Sound.TalkSoundID) + 50;
                int width, height;

                this.Trace().Write(TraceSwitches.Music, "getCurVoiceLipSyncWidth({0})", Sound.TalkSoundID);
                GetLipSync(Sound.TalkSoundID, 0, msPos, out width, out height);
                return width;
            }
        }

        public int GetCurVoiceLipSyncHeight()
        {
            lock (_mutex)
            {
                int msPos = GetPosInMs(Sound.TalkSoundID) + 50;
                int width, height;

                this.Trace().Write(TraceSwitches.Music, "getCurVoiceLipSyncHeight({0})", Sound.TalkSoundID);
                GetLipSync(Sound.TalkSoundID, 0, msPos, out width, out height);
                return height;
            }
        }

        public int GetCurMusicLipSyncWidth(int syncId)
        {
            lock (_mutex)
            {
                int soundId = -1;

                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.VolGroupId == ImuseVolumeGroupMusic))
                    {
                        soundId = track.SoundId;
                    }
                }

                int msPos = GetPosInMs(soundId) + 50;
                int width = 0, height = 0;

                this.Trace().Write(TraceSwitches.Music, "getCurVoiceLipSyncWidth({0}, {1})", soundId, msPos);
                GetLipSync(soundId, syncId, msPos, out width, out height);
                return width;
            }
        }

        public int GetCurMusicLipSyncHeight(int syncId)
        {
            lock (_mutex)
            {
                int soundId = -1;

                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.VolGroupId == ImuseVolumeGroupMusic))
                    {
                        soundId = track.SoundId;
                    }
                }

                int msPos = GetPosInMs(soundId) + 50;
                int width = 0, height = 0;

                this.Trace().Write(TraceSwitches.Music, "getCurVoiceLipSyncHeight({0}, {1})", soundId, msPos);
                GetLipSync(soundId, syncId, msPos, out width, out height);
                return height;
            }
        }

        public void StopAllSounds()
        {
            lock (_mutex)
            {
                this.Trace().Write(TraceSwitches.Music, "stopAllSounds");

                for (int l = 0; l < MaxDigitalTracks + MaxDigitalFadeTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used)
                    {
                        // Stop the sound output, *now*. No need to use toBeRemoved etc.
                        // as we are protected by a mutex, and this method is never called
                        // from callback either.
                        _mixer.StopHandle(track.MixChanHandle);
                        if (track.SoundDesc != null)
                        {
                            this.Trace().Write(TraceSwitches.Music, "stopAllSounds - stopping sound({0})", track.SoundId);
                            _sound.CloseSound(track.SoundDesc);
                        }

                        // Mark the track as unused
                        track.Clear();
                    }
                }
            }
        }

        public void Pause(bool p)
        {
            _pause = p;
        }

        public void RefreshScripts()
        {
            lock (_mutex)
            {
                this.Trace().Write(TraceSwitches.Music, "refreshScripts()");

                if (_stopingSequence != 0)
                {
                    // prevent start new music, only fade out old one
                    if (_vm.SmushActive)
                    {
                        FadeOutMusic(60);
                        return;
                    }
                    // small delay, it seems help for fix bug #1757010
                    if (_stopingSequence++ > 120)
                    {
                        this.Trace().Write(TraceSwitches.Music, "refreshScripts() Force restore music state");
                        ParseScriptCmds(0x1001, 0, 0, 0, 0, 0, 0, 0);
                        _stopingSequence = 0;
                    }
                }

                bool found = false;
                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.VolGroupId == ImuseVolumeGroupMusic))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found && _curMusicState != 0)
                {
                    this.Trace().Write(TraceSwitches.Music, "refreshScripts() Restore music state");
                    ParseScriptCmds(0x1001, 0, 0, 0, 0, 0, 0, 0);
                }
            }
        }

        void FlushTrack(Track track)
        {
            track.ToBeRemoved = true;

            if (track.SouStreamUsed)
            {
                _mixer.StopHandle(track.MixChanHandle);
            }
            else if (track.Stream != null)
            {
                this.Trace().Write(TraceSwitches.Music, "FlushTrack() - soundId:{0}", track.SoundId);
                // Finalize the appendable stream, then remove our reference to it.
                // Note that there might still be some data left in the buffers of the
                // appendable stream. We play it nice and wait till all of it
                // played. The audio mixer will take care of it afterwards (and dispose it).
                track.Stream.Finish();
                track.Stream = null;
                if (track.SoundDesc != null)
                {
                    _sound.CloseSound(track.SoundDesc);
                    track.SoundDesc = null;
                }
            }

            if (!_mixer.IsSoundHandleActive(track.MixChanHandle))
            {
                track.Clear();
            }
        }

        void StartMusic(int soundId, int volume)
        {
            this.Trace().Write(TraceSwitches.Music, "startMusicResource({0})", soundId);
            StartSound(soundId, "", ImuseResource, ImuseVolumeGroupMusic, null, 0, volume, 126, null);
        }

        void StartMusic(string soundName, int soundId, int hookId, int volume)
        {
            this.Trace().Write(TraceSwitches.Music, "startMusicBundle({0}, soundId:{1}, hookId:{2})", soundName, soundId, hookId);
            StartSound(soundId, soundName, ImuseBundle, ImuseVolumeGroupMusic, null, hookId, volume, 126, null);
        }

        void StartMusicWithOtherPos(string soundName, int soundId, int hookId, int volume, Track otherTrack)
        {
            this.Trace().Write(TraceSwitches.Music, "startMusicWithOtherPos({0}, soundId:{1}, hookId:{2}, oldSoundId:{3})", soundName, soundId, hookId, otherTrack.SoundId);
            StartSound(soundId, soundName, ImuseBundle, ImuseVolumeGroupMusic, null, hookId, volume, 126, otherTrack);
        }

        void GetLipSync(int soundId, int syncId, int msPos, out int width, out int height)
        {
            int sync_size;
            byte[] sync_ptr;

            width = 0;
            height = 0;

            msPos /= 16;
            if (msPos < 65536)
            {
                lock (_mutex)
                {
                    for (int l = 0; l < MaxDigitalTracks; l++)
                    {
                        var track = _track[l];
                        if (track.Used && !track.ToBeRemoved && (track.SoundId == soundId))
                        {
                            _sound.GetSyncSizeAndPtrById(track.SoundDesc, syncId, out sync_size, out sync_ptr);
                            var sync_pos = 0;
                            if ((sync_size != 0) && (sync_ptr != null))
                            {
                                sync_size /= 4;
                                while ((sync_size--) != 0)
                                {
                                    if (sync_ptr.ToUInt16BigEndian(sync_pos) >= msPos)
                                        break;
                                    sync_pos += 4;
                                }
                                if (sync_size < 0)
                                    sync_pos -= 4;
                                else if (sync_ptr.ToUInt16BigEndian(sync_pos) > msPos)
                                    sync_pos -= 4;

                                width = sync_ptr[2];
                                height = sync_ptr[3];
                                return;
                            }
                        }
                    }
                }
            }
        }

        int GetPosInMs(int soundId)
        {
            lock (_mutex)
            {
                for (int l = 0; l < MaxDigitalTracks; l++)
                {
                    var track = _track[l];
                    if (track.Used && !track.ToBeRemoved && (track.SoundId == soundId))
                    {
                        int pos = (5 * (track.DataOffset + track.RegionOffset)) / (track.FeedSize / 200);
                        return pos;
                    }
                }

                return 0;
            }
        }


    }
}

