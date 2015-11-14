//
//  Player_Towns_v1.cs
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
using NScumm.Core.Audio.SoftSynth;

namespace NScumm.Scumm.Audio.Players
{
    class Player_Towns_v1: Player_Towns
    {
        public Player_Towns_v1(ScummEngine vm, IMixer mixer)
            : base(vm, false)
        {
            if (_vm.Game.Version == 3)
            {
                _soundOverride = new SoundOvrParameters[_numSoundMax];
            }

            _driver = new TownsEuphonyDriver(mixer);
        }

        public override bool Init()
        {
            if (_driver == null)
                return false;

            if (!_driver.Init())
                return false;

            _driver.ReserveSoundEffectChannels(8);
            _intf = _driver.Interface;

            // Treat all 6 fm channels and all 8 pcm channels as sound effect channels
            // since music seems to exist as CD audio only in the games which use this
            // MusicEngine implementation.
            _intf.SetSoundEffectChanMask(-1);

            SetVolumeCD(255, 255);

            return true;
        }

        public override void StartSound(int sound)
        {
            var ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, sound);
            var offset = 0;
            if (_vm.Game.Version != 3)
                offset += 2;

            int type = ptr[offset + 13];

            if (type == 0)
            {
                byte velocity = 0;
                byte note = 0;

                if (_vm.Game.Version == 3)
                {
                    velocity = (byte)(_soundOverride[sound].vLeft + _soundOverride[sound].vRight);
                    note = _soundOverride[sound].note;
                }

                velocity = velocity != 0 ? (byte)(velocity >> 2) : (byte)(ptr[offset + 14] >> 1);
                ushort len = (ushort)(ptr.ToUInt16(offset) + 2);
                PlayPcmTrack(sound, ptr, offset + 6, velocity, 64, note != 0 ? note : (len > 50 ? ptr[offset + 50] : 60), ptr.ToUInt16(offset + 10));

            }
            else if (type == 1)
            {
                PlayEuphonyTrack(sound, ptr, 6);

            }
            else if (type == 2)
            {
                PlayCdaTrack(sound, ptr, 6);
            }

            if (_vm.Game.Version == 3)
                _soundOverride[sound].vLeft = _soundOverride[sound].vRight = _soundOverride[sound].note = 0;
        }

        public override void SetMusicVolume(int vol)
        {
            _driver.SetMusicVolume(vol);
        }

        public override void StopSound(int sound)
        {
            if (sound == 0 || sound == _cdaCurrentSound)
            {
                _cdaCurrentSound = 0;
                _vm.Sound.StopCD();
                _vm.Sound.StopCDTimer();
            }

            if (sound != 0 && sound == _eupCurrentSound)
            {
                _eupCurrentSound = 0;
                _eupLooping = false;
                _driver.StopParser();
            }

            StopPcmTrack(sound);
        }

        public override void StopAllSounds()
        {
            _cdaCurrentSound = 0;
            _vm.Sound.StopCD();
            _vm.Sound.StopCDTimer();

            _eupCurrentSound = 0;
            _eupLooping = false;
            _driver.StopParser();

            StopPcmTrack(0);
        }

        public override int DoCommand(int numargs, int[] args)
        {
            int res = 0;

            switch (args[0])
            {
                case 2:
                    _driver.Interface.Callback(73, 0);
                    break;

                case 3:
                    RestartLoopingSounds();
                    break;

                case 8:
                    StartSound(args[1]);
                    break;

                case 9:
                    _vm.Sound.StopSound(args[1]);
                    break;

                case 11:
                    StopPcmTrack(0);
                    break;

                case 14:
                    StartSoundEx(args[1], args[2], args[3], args[4]);
                    break;

                case 15:
                    StopSoundSuspendLooping(args[1]);
                    break;

                default:
                    Debug.WriteLine("Player_Towns_v1::doCommand: Unknown command {0}", args[0]);
                    break;
            }

            return res;
        }

        void PlayCdaTrack(int sound, byte[] data, int offset, bool skipTrackVelo = false)
        {
            var ptr = data;

            if (sound == 0)
                return;

            if (!skipTrackVelo)
            {
                if (_vm.Game.Version == 3)
                {
                    if ((_soundOverride[sound].vLeft + _soundOverride[sound].vRight) != 0)
                        SetVolumeCD(_soundOverride[sound].vLeft, _soundOverride[sound].vRight);
                    else
                        SetVolumeCD(ptr[offset + 8], ptr[offset + 9]);
                }
                else
                {
                    SetVolumeCD(ptr[offset + 8], ptr[offset + 9]);
                }
            }

            if (sound == _cdaCurrentSound && _vm.Sound.PollCD() == 1)
                return;

            offset += 16;

            int track = ptr[offset + 0];
            _cdaNumLoops = ptr[offset + 1];
            int start = (ptr[offset + 2] * 60 + ptr[offset + 3]) * 75 + ptr[offset + 4];
            int end = (ptr[offset + 5] * 60 + ptr[offset + 6]) * 75 + ptr[offset + 7];

            _vm.Sound.PlayCDTrack(track, _cdaNumLoops == 0xff ? -1 : _cdaNumLoops, start, end <= start ? 0 : end - start);
            _cdaForceRestart = 0;
            _cdaCurrentSound = (byte)sound;
        }

        void PlayEuphonyTrack(int sound, byte[] data, int offset)
        {
            int pos = offset + 16;
            int src = pos + data[offset + 14] * 48;
            int trackData = src + 150;

            for (int i = 0; i < 32; i++)
                _driver.ConfigChanEnable(i, data[src++]);
            for (int i = 0; i < 32; i++)
                _driver.ConfigChanSetMode(i, 0xff);
            for (int i = 0; i < 32; i++)
                _driver.ConfigChanRemap(i, data[src++]);
            for (int i = 0; i < 32; i++)
                _driver.ConfigChanAdjustVolume(i, data[src++]);
            for (int i = 0; i < 32; i++)
                _driver.ConfigChanSetTranspose(i, data[src++]);

            src += 8;
            for (int i = 0; i < 6; i++)
                _driver.AssignChannel(i, data[src++]);

            for (int i = 0; i < data[offset + 14]; i++)
            {
                _driver.LoadInstrument(i, i, data, pos + i * 48);
                _driver.Interface.Callback(4, i, i);
            }

            _eupVolLeft = _soundOverride[sound].vLeft;
            _eupVolRight = _soundOverride[sound].vRight;
            int lvl = _soundOverride[sound].vLeft + _soundOverride[sound].vRight;
            if (lvl == 0)
                lvl = data[offset + 8] + data[offset + 9];
            lvl >>= 2;

            for (int i = 0; i < 6; i++)
                _driver.ChanVolume(i, lvl);

            int trackSize = data.ToInt32(src);
            src += 4;
            byte startTick = data[src++];

            _driver.SetMusicTempo(data[src++]);
            _driver.StartMusicTrack(data, trackData, trackSize, startTick);

            _eupLooping = (data[src] != 1);
            _driver.SetMusicLoop(_eupLooping);
            _driver.ContinueParsing();
            _eupCurrentSound = (byte)sound;
        }

        void StartSoundEx(int sound, int velo, int pan, int note)
        {
            var ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, sound);
            var offset = 2;

            if (pan > 99)
                pan = 99;

            velo = velo != 0 ? (velo * ptr[offset + 14] + 50) / 100 : ptr[offset + 14];
            velo = ScummHelper.Clip(velo, 1, 255);
            ushort pri = ptr.ToUInt16(offset + 10);

            if (ptr[offset + 13] == 0)
            {
                velo >>= 1;

                if (velo == 0)
                    velo = 1;

                pan = pan != 0 ? (((pan << 7) - pan) + 50) / 100 : 64;

                PlayPcmTrack(sound, ptr, offset + 6, velo != 0 ? velo : ptr[offset + 14] >> 1, pan, note != 0 ? note : ptr[offset + 50], pri);

            }
            else if (ptr[offset + 13] == 2)
            {
                int volLeft = velo;
                int volRight = velo;

                if (pan < 50)
                    volRight = ((pan * 2 + 1) * velo + 50) / 100;
                else if (pan > 50)
                    volLeft = (((99 - pan) * 2 + 1) * velo + 50) / 100;

                SetVolumeCD(volLeft, volRight);

                if (_cdaForceRestart == 0 && sound == _cdaCurrentSound)
                    return;

                PlayCdaTrack(sound, ptr, offset + 6, true);
            }
        }

        void StopSoundSuspendLooping(int sound)
        {
            if (sound == 0)
            {
                return;
            }
            else if (sound == _cdaCurrentSound)
            {
                if (_cdaNumLoops != 0 && _cdaForceRestart != 0)
                    _cdaForceRestart = 1;
            }
            else
            {
                for (int i = 1; i < 9; i++)
                {
                    if (sound == _pcmCurrentSound[i].index)
                    {
                        if (!_driver.SoundEffectIsPlaying(i + 0x3f))
                            continue;
                        _driver.StopSoundEffect(i + 0x3f);
                        if (_pcmCurrentSound[i].looping != 0)
                            _pcmCurrentSound[i].paused = 1;
                        else
                            _pcmCurrentSound[i].index = 0;
                    }
                }
            }
        }

        void RestartLoopingSounds()
        {
            if (_cdaNumLoops != 0 && _cdaForceRestart == 0)
                _cdaForceRestart = 1;

            for (int i = 1; i < 9; i++)
            {
                if (_pcmCurrentSound[i].paused == 0)
                    continue;

                _pcmCurrentSound[i].paused = 0;

                var ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, _pcmCurrentSound[i].index);
                if (ptr == null)
                    continue;

                var offset = 0;
                offset += 24;

                int c = 1;
                while (_pcmCurrentSound[i].chan != c)
                {
                    offset += ptr.ToInt32(offset + 12) + 32;
                    c++;
                }

                _driver.PlaySoundEffect(i + 0x3f, _pcmCurrentSound[i].note, _pcmCurrentSound[i].velo, ptr);
            }

            _driver.Interface.Callback(73, 1);
        }

        struct SoundOvrParameters
        {
            public byte vLeft;
            public byte vRight;
            public byte note;
        }

        byte _eupCurrentSound;
        bool _eupLooping;
        byte _eupVolLeft;
        byte _eupVolRight;

        byte _cdaCurrentSound;
        byte _cdaNumLoops;
        byte _cdaForceRestart;

        byte _cdaCurrentSoundTemp;
        byte _cdaNumLoopsTemp;

        readonly SoundOvrParameters[] _soundOverride;
        TownsEuphonyDriver _driver;
    }
}

