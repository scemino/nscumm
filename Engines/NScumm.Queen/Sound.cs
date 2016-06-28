//
//  Sound.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.IO;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    public abstract partial class Sound
    {
        protected IMixer _mixer;
        protected QueenEngine _vm;
        protected SoundHandle _sfxHandle;
        protected SoundHandle _speechHandle;
        bool _sfxToggle;
        bool _musicToggle;
        bool _speechToggle;
        protected bool _speechSfxExists;
        protected short _lastOverride;
        protected int _musicVolume;

        public bool SfxOn
        {
            get { return _sfxToggle; }
            set { _sfxToggle = value; }
        }
        public bool SpeechOn
        {
            get { return _speechToggle; }
            set { _speechToggle = value; }
        }
        public bool SpeechSfxExists { get { return _speechSfxExists; } }
        public virtual bool IsSpeechActive { get { return false; } }
        public bool MusicOn
        {
            get { return _musicToggle; }
            set { _musicToggle = value; }
        }
        public short LastOverride { get { return _lastOverride; } }
        public virtual int Volume
        {
            get { return _musicVolume; }
            set
            {
                if (ConfigManager.Instance.HasKey("mute") && ConfigManager.Instance.Get<bool>("mute"))
                    _musicVolume = 0;
                else
                    _musicVolume = value;

                _mixer.SetVolumeForSoundType(SoundType.Music, _musicVolume);
            }
        }

        protected Sound(IMixer mixer, QueenEngine vm)
        {
            _mixer = mixer;
            _vm = vm;
            _sfxToggle = true;
            SpeechOn = false;
            _musicToggle = true;
        }

        public void LoadState(uint version, byte[] data, ref int ptr)
        {
            _lastOverride = data.ToInt16BigEndian(ptr); ptr += 2;
        }

        public void PlayLastSong()
        {
            PlaySong(_lastOverride);
        }

        public void ToggleSfx()
        {
            _sfxToggle = !_sfxToggle;
        }

        public void ToggleMusic()
        {
            _musicToggle = !_musicToggle;
        }

        public void ToggleSpeech()
        {
            _speechToggle = !_speechToggle;
        }

        public void SaveState(byte[] data, ref int ptr)
        {
            data.WriteInt16BigEndian(ptr, _lastOverride); ptr += 2;
        }

        public static Sound MakeSoundInstance(IMixer mixer, QueenEngine vm, byte compression)
        {
            if (vm.Resource.Platform == Platform.Amiga)
                return new AmigaSound(mixer, vm);

            switch (compression)
            {
                case Defines.COMPRESSION_NONE:
                    return new SBSound(mixer, vm);
                case Defines.COMPRESSION_MP3:
                    return new Mp3Sound(mixer, vm);
                case Defines.COMPRESSION_OGG:
                    return new OggSound(mixer, vm);
                case Defines.COMPRESSION_FLAC:
                    return new FlacSound(mixer, vm);
                default:
                    D.Warning("Unknown compression type");
                    return new SilentSound(mixer, vm);
            }
        }

        public virtual void PlaySfx(ushort sfx) { }
        public virtual void PlaySong(short songNum) { }
        public virtual void PlaySpeech(string @base) { }

        public virtual void StopSfx() { }
        public virtual void StopSong() { }
        public virtual void StopSpeech() { }

        public virtual void UpdateMusic() { }
    }
}

