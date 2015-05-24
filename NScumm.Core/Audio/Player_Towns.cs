//
//  Player_Towns.cs
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
using NScumm.Core.Audio.SoftSynth;
using NScumm.Core.IO;
using System.Collections.Generic;

namespace NScumm.Core.Audio
{
    internal abstract class Player_Towns: IMusicEngine
    {
        protected Player_Towns(ScummEngine vm, bool isVersion2)
        {
            _vm = vm;
            _v2 = isVersion2;
            _numSoundMax = isVersion2 ? 256 : 200;
            _unkFlags = 0x33;
        }

        public virtual int GetMusicTimer()
        {
            return 0;
        }

        public abstract void SetMusicVolume(int vol);

        public abstract void StartSound(int sound);

        public abstract void StopSound(int sound);

        public abstract void StopAllSounds();

        public abstract bool Init();

        public void SetSfxVolume(int volume)
        {
            if (_intf == null)
                return;
            _intf.SetSoundEffectVolume(volume);
        }

        public virtual int GetSoundStatus(int sound)
        {
            if (_intf == null)
                return 0;
            for (int i = 1; i < 9; i++)
            {
                if (_pcmCurrentSound[i].index == sound)
                    return _intf.Callback(40, 0x3f + i) ? 1 : 0;
            }
            return 0;
        }

        public abstract int DoCommand(int numargs, int[] args);

        public virtual void SaveOrLoad(Serializer ser)
        {
            for (int i = 1; i < 9; i++)
            {
                if (_pcmCurrentSound[i].index == 0)
                    continue;

                if (_intf.Callback(40, i + 0x3f))
                    continue;

                _intf.Callback(39, i + 0x3f);

                _pcmCurrentSound[i].index = 0;
            }

            _pcmCurrentSound.ForEach(s => s.SaveOrLoad(ser));
        }

        // version 1 specific
        public virtual int GetCurrentCdaSound()
        {
            return 0;
        }

        public virtual int GetCurrentCdaVolume()
        {
            return 0;
        }

        public virtual void SetVolumeCD(int left, int right)
        {
        }

        public virtual void SetSoundVolume(int sound, int left, int right)
        {
        }

        public virtual void SetSoundNote(int sound, int note)
        {
        }

        void RestoreAfterLoad()
        {
            var restoredSounds = new HashSet<ushort>();

            for (int i = 1; i < 9; i++)
            {
                if (_pcmCurrentSound[i].index == 0 || _pcmCurrentSound[i].index == 0xffff)
                    continue;

                // Don't restart multichannel sounds more than once
                if (restoredSounds.Contains(_pcmCurrentSound[i].index))
                    continue;

                if (!_v2)
                    restoredSounds.Add(_pcmCurrentSound[i].index);

                var ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, _pcmCurrentSound[i].index);
                if (ptr == null)
                    continue;

                var offset = 0;
                if (_vm.Game.Version != 3)
                    offset += 2;

                if (ptr[offset + 13] != 0)
                    continue;

                PlayPcmTrack(_pcmCurrentSound[i].index, ptr, offset + 6, _pcmCurrentSound[i].velo, _pcmCurrentSound[i].pan, _pcmCurrentSound[i].note, (int)_pcmCurrentSound[i].priority);
            }
        }

        protected void PlayPcmTrack(int sound, byte[] data, int offset, int velo, int pan, int note, int priority)
        {
            if (_intf == null)
                return;

            var sfxData = data;
            var sfxOffset = offset + 16;

            int numChan = _v2 ? 1 : data[offset + 14];
            for (int i = 0; i < numChan; i++)
            {
                int chan = AllocatePcmChannel(sound, i, (uint)priority);
                if (chan == 0)
                    return;

                _intf.Callback(70, _unkFlags);
                _intf.Callback(3, chan + 0x3f, pan);
                _intf.Callback(37, chan + 0x3f, note, velo, sfxData, sfxOffset);

                _pcmCurrentSound[chan].note = (byte)note;
                _pcmCurrentSound[chan].velo = (byte)velo;
                _pcmCurrentSound[chan].pan = (byte)pan;
                _pcmCurrentSound[chan].paused = 0;
                _pcmCurrentSound[chan].looping = sfxData.ToUInt32(sfxOffset + 20) != 0 ? (byte)1 : (byte)0;

                sfxOffset += (sfxData.ToInt32(sfxOffset + 12) + 32);
            }
        }

        protected void StopPcmTrack(int sound)
        {
            if (_intf == null)
                return;

            for (int i = 1; i < 9; i++)
            {
                if (sound == _pcmCurrentSound[i].index || sound == 0)
                {
                    _intf.Callback(39, i + 0x3f);
                    _pcmCurrentSound[i].index = 0;
                }
            }
        }

        protected int AllocatePcmChannel(int sound, int sfxChanRelIndex, uint priority)
        {
            if (_intf == null)
                return 0;

            int chan = 0;

            if (_v2 && priority > 255)
            {
                chan = 8;
                if (_intf.Callback(40, 0x47))
                    _intf.Callback(39, 0x47);
            }
            else
            {
                for (int i = 8; i != 0; i--)
                {
                    if (_pcmCurrentSound[i].index == 0)
                    {
                        chan = i;
                        continue;
                    }

                    if (_intf.Callback(40, i + 0x3f))
                        continue;

                    chan = i;
                    if (_pcmCurrentSound[chan].index == 0xffff)
                        _intf.Callback(39, chan + 0x3f);
                    else
                        _vm.Sound.StopSound(_pcmCurrentSound[chan].index);
                }

                if (chan == 0)
                {
                    for (int i = 1; i < 9; i++)
                    {
                        if (priority >= _pcmCurrentSound[i].priority)
                            chan = i;
                    }
                    if (_pcmCurrentSound[chan].index == 0xffff)
                        _intf.Callback(39, chan + 0x3f);
                    else
                        _vm.Sound.StopSound(_pcmCurrentSound[chan].index);
                }
            }

            if (chan != 0)
            {
                _pcmCurrentSound[chan].index = (ushort)sound;
                _pcmCurrentSound[chan].chan = (ushort)sfxChanRelIndex;
                _pcmCurrentSound[chan].priority = priority;
            }

            return chan;
        }

        protected class PcmCurrentSound
        {
            public ushort index;
            public ushort chan;
            public byte note;
            public byte velo;
            public byte pan;
            public byte paused;
            public byte looping;
            public uint priority;

            public void SaveOrLoad(Serializer ser)
            {
                LoadAndSaveEntry[] pcmEntries =
                    {
                        LoadAndSaveEntry.Create(r => index = r.ReadUInt16(), w => w.WriteUInt16(index), 81),
                        LoadAndSaveEntry.Create(r => chan = r.ReadUInt16(), w => w.WriteUInt16(chan), 81),
                        LoadAndSaveEntry.Create(r => note = r.ReadByte(), w => w.WriteByte(note), 81),
                        LoadAndSaveEntry.Create(r => velo = r.ReadByte(), w => w.WriteByte(velo), 81),
                        LoadAndSaveEntry.Create(r => pan = r.ReadByte(), w => w.WriteByte(pan), 81),
                        LoadAndSaveEntry.Create(r => paused = r.ReadByte(), w => w.WriteByte(paused), 81),
                        LoadAndSaveEntry.Create(r => looping = r.ReadByte(), w => w.WriteByte(looping), 81),
                        LoadAndSaveEntry.Create(r => priority = r.ReadUInt32(), w => w.WriteUInt32(priority), 81),
                    };
                pcmEntries.ForEach(e => e.Execute(ser));
            }
        }

        protected PcmCurrentSound[] _pcmCurrentSound = CreatePcmCurrentSounds();

        static PcmCurrentSound[] CreatePcmCurrentSounds()
        {
            var sounds = new PcmCurrentSound[9];
            for (int i = 0; i < sounds.Length; i++)
            {
                sounds[i] = new PcmCurrentSound();
            }
            return sounds;
        }

        int _unkFlags;

        protected TownsAudioInterface _intf;
        protected ScummEngine _vm;

        protected readonly int _numSoundMax;
        readonly bool _v2;
    }
}

