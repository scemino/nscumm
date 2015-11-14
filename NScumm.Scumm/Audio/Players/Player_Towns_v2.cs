//
//  Player_Towns_v2.cs
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
using NScumm.Core.Audio.SoftSynth;
using NScumm.Scumm.Audio.IMuse;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Audio.Players
{
    class Player_Towns_v2: Player_Towns
    {
        public Player_Towns_v2(ScummEngine vm, IMixer mixer, IIMuse imuse, bool disposeIMuse)
            : base(vm, true)
        {
            _imuse = imuse;
            _imuseDispose = disposeIMuse;
            _soundOverride = new SoundOvrParameters[_numSoundMax];
            _intf = new TownsAudioInterface(mixer, null);
        }

        public override bool Init()
        {
            if (_intf == null)
                return false;

            if (!_intf.Init())
                return false;

            _intf.Callback(33, 8);
            _intf.SetSoundEffectChanMask(~0x3f);

            return true;
        }

        public override void SetMusicVolume(int vol)
        {
            _imuse.SetMusicVolume(vol);
        }

        public override int GetSoundStatus(int sound)
        {
            if (_soundOverride[sound].type == 7)
                return base.GetSoundStatus(sound);
            return _imuse.GetSoundStatus(sound);
        }

        public override void StartSound(int sound)
        {
            var ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, sound);

            if (ptr.ToText() == "TOWS")
            {
                _soundOverride[sound].type = 7;
                byte velo = (byte)(_soundOverride[sound].velo != 0 ? _soundOverride[sound].velo - 1 : (ptr[10] + ptr[11] + 1) >> 1);
                byte pan = (byte)(_soundOverride[sound].pan != 0 ? _soundOverride[sound].pan - 1 : 64);
                byte pri = ptr[9];
                _soundOverride[sound].velo = _soundOverride[sound].pan = 0;
                PlayPcmTrack(sound, ptr, 8, velo, pan, ptr[52], pri);

            }
            else if (ptr.ToText() == "SBL ")
            {
                _soundOverride[sound].type = 5;
                PlayVocTrack(ptr, 27);
            }
            else
            {
                _soundOverride[sound].type = 3;
                _imuse.StartSound(sound);
            }
        }

        public override void StopSound(int sound)
        {
            if (_soundOverride[sound].type == 7)
            {
                StopPcmTrack(sound);
            }
            else
            {
                _imuse.StopSound(sound);
            }
        }

        public override void StopAllSounds()
        {
            StopPcmTrack(0);
            _imuse.StopAllSounds();
        }

        public override int DoCommand(int numargs, int[] args)
        {
            int res = -1;
            byte[] ptr = null;

            switch (args[0])
            {
                case 8:
                    StartSound(args[1]);
                    res = 0;
                    break;

                case 9:
                case 15:
                    StopSound(args[1]);
                    res = 0;
                    break;

                case 11:
                    StopPcmTrack(0);
                    break;

                case 13:
                    res = GetSoundStatus(args[1]);
                    break;

                case 258:
                    if (_soundOverride[args[1]].type == 0)
                    {
                        ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, args[1]);
                        if (ptr.ToText() == "TOWS")
                            _soundOverride[args[1]].type = 7;
                    }
                    if (_soundOverride[args[1]].type == 7)
                    {
                        _soundOverride[args[1]].velo = (byte)(args[2] + 1);
                        res = 0;
                    }
                    break;

                case 259:
                    if (_soundOverride[args[1]].type == 0)
                    {
                        ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, args[1]);
                        if (ptr.ToText() == "TOWS")
                            _soundOverride[args[1]].type = 7;
                    }
                    if (_soundOverride[args[1]].type == 7)
                    {
                        _soundOverride[args[1]].pan = (byte)(64 - ScummHelper.Clip(args[2], -63, 63));
                        res = 0;
                    }
                    break;
            }

            if (res == -1)
                return _imuse.DoCommand(numargs, args);

            return res;
        }

        public override void SaveOrLoad(Serializer ser)
        {
            if (ser.Version >= 83)
                base.SaveOrLoad(ser);
        }

        void PlayVocTrack(byte[] data, int offset)
        {
            uint len = (data.ToUInt32(offset) >> 8) - 2;

            int chan = AllocatePcmChannel(0xffff, 0, 0x1000);
            if (chan == 0)
                return;

            _sblData = new byte[len + 32];
            Array.Copy(header, _sblData, 32);
            _sblData.WriteUInt32(12, len);

            var src = offset + 6;
            var dst = 32;
            for (var i = 0; i < len; i++)
                _sblData[dst++] = (byte)(((data[src] & 0x80) != 0) ? (data[src++] & 0x7f) : -data[src++]);

            _intf.Callback(37, 0x3f + chan, 60, 127, _sblData);
            _pcmCurrentSound[chan].paused = 0;
        }

        static readonly byte[] header =
            {
                0x54, 0x61, 0x6C, 0x6B, 0x69, 0x65, 0x20, 0x20,
                0x78, 0x56, 0x34, 0x12, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x36, 0x04, 0x00, 0x00, 0x3C, 0x00, 0x00, 0x00
            };

        struct SoundOvrParameters
        {
            public byte velo;
            public byte pan;
            public byte type;
        }

        SoundOvrParameters[] _soundOverride;

        byte[] _sblData;

        IIMuse _imuse;
        readonly bool _imuseDispose;
    }
}

