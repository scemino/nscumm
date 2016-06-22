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
using System;
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.IO;
using D = NScumm.Core.DebugHelper;
using NScumm.Core.Audio.Decoders;

namespace NScumm.Queen
{
    class AmigaSound : Sound
    {
        const int SB_HEADER_SIZE_V104 = 110;
        const int SB_HEADER_SIZE_V110 = 122;


        private short _fanfareRestore;
        private int _fanfareCount, _fluteCount;
        private SoundHandle _modHandle;
        private SoundHandle _patHandle;

        public AmigaSound(IMixer mixer, QueenEngine vm)
            : base(mixer, vm)
        {
        }

        public override void PlaySfx(ushort sfx)
        {
            if (_vm.Logic.CurrentRoom == 111)
            {
                // lightning sound
                PlaySound("88SSSSSS");
            }
        }

        public override void StopSfx()
        {
            _mixer.StopHandle(_sfxHandle);
        }

        public override void StopSong()
        {
            _mixer.StopHandle(_modHandle);
            _fanfareCount = _fluteCount = 0;
        }

        public override void UpdateMusic()
        {
            if (_fanfareCount > 0)
            {
                --_fanfareCount;
                if (_fanfareCount == 0)
                {
                    PlaySong(_fanfareRestore);
                }
            }
            if (_fluteCount > 0 && (_lastOverride == 40 || _lastOverride == 3))
            {
                --_fluteCount;
                if (_fluteCount == 0)
                {
                    PlayPattern("JUNG", 5 + _vm.Randomizer.Next(1 + 6));
                    _fluteCount = 100;
                }
            }
        }

        private void PlayPattern(string @base, int pattern)
        {
            _mixer.StopHandle(_patHandle);
            IAudioStream stream = LoadModule(@base, -pattern);
            if (stream != null)
            {
                _patHandle = _mixer.PlayStream(SoundType.SFX, stream);
            }
        }

        private void PlaySound(string @base)
        {
            D.Debug(7, $"AmigaSound::playSound({@base})");
            string soundName = $"{@base}.AMR";

            uint soundSize;
            var f = _vm.Resource.FindSound(soundName, out soundSize);
            if (f != null)
            {
                byte[] soundData = new byte[soundSize];
                f.Read(soundData, 0, (int)soundSize);

                IAudioStream stream = new RawStream(AudioFlags.None, 11025, true, new MemoryStream(soundData, 0, (int)soundSize));
                _sfxHandle = _mixer.PlayStream(SoundType.SFX, stream);
            }
        }

        private void PlaySoundData(Stream f, uint size, SoundHandle soundHandle)
        {
            // In order to simplify the code, we don't parse the .sb header but hard-code the
            // values. Refer to tracker item #1876741 for details on the format/fields.
            int headerSize;
            f.Seek(2, SeekOrigin.Current);
            var br = new BinaryReader(f);
            ushort version = br.ReadUInt16();
            switch (version)
            {
                case 104:
                    headerSize = SB_HEADER_SIZE_V104;
                    break;
                case 110:
                    headerSize = SB_HEADER_SIZE_V110;
                    break;
                default:
                    D.Warning("Unhandled SB file version %d, defaulting to 104", version);
                    headerSize = SB_HEADER_SIZE_V104;
                    break;
            }
            f.Seek(headerSize - 4, SeekOrigin.Current);
            size = (uint)(size - headerSize);
            byte[] sound = new byte[size];
            if (sound != null)
            {
                f.Read(sound, 0, (int)size);
                SoundType type = (soundHandle == _speechHandle) ? SoundType.Speech : SoundType.SFX;

                IAudioStream stream = new RawStream(AudioFlags.Unsigned, 11840, true, new MemoryStream(sound, 0, (int)size));
                soundHandle = _mixer.PlayStream(type, stream);
            }
        }

        public override void PlaySong(short song)
        {
            D.Debug(2, $"Sound::playSong {song} override {_lastOverride}");

            if (song < 0)
            {
                StopSong();
                return;
            }

            // remap song numbers for the Amiga
            switch (song)
            {
                case 1:
                case 2:
                    song = 39;
                    break;
                case 37:
                case 52:
                case 196:
                    song = 90;
                    break;
                case 38:
                case 89:
                    song = 3;
                    break;
                case 24:
                case 158:
                    song = 117;
                    break;
                case 71:
                case 72:
                case 73:
                case 75:
                    song = 133;
                    break;
                case 203:
                    song = 67;
                    break;
                case 145:
                    song = 140;
                    break;
                case 53:
                case 204:
                    song = 44;
                    break;
                case 136:
                case 142:
                case 179:
                    song = 86;
                    break;
                case 101:
                case 102:
                case 143:
                    song = 188;
                    break;
                case 65:
                case 62:
                    song = 69;
                    break;
                case 118:
                case 119:
                    song = 137;
                    break;
                case 130:
                case 131:
                    song = 59;
                    break;
                case 174:
                case 175:
                    song = 57;
                    break;
                case 171:
                case 121:
                    song = 137;
                    break;
                case 138:
                case 170:
                case 149:
                    song = 28;
                    break;
                case 122:
                case 180:
                case 83:
                case 98:
                    song = 83;
                    break;
                case 20:
                case 33:
                    song = 34;
                    break;
                case 29:
                case 35:
                    song = 36;
                    break;
                case 7:
                case 9:
                case 10:
                    song = 11;
                    break;
                case 110:
                    song = 94;
                    break;
                case 111:
                    song = 95;
                    break;
                case 30:
                    song = 43;
                    break;
                case 76:
                    song = 27;
                    break;
                case 194:
                case 195:
                    song = 32;
                    break;
            }

            if (_lastOverride != 32 && _lastOverride != 44)
            {
                if (PlaySpecialSfx(song))
                {
                    return;
                }
            }

            if (_lastOverride == song && _mixer.IsSoundHandleActive(_modHandle))
            {
                return;
            }
            switch (song)
            {
                // hotel
                case 39:
                    PlayModule("HOTEL", 1);
                    break;
                case 19:
                    PlayModule("HOTEL", 3);
                    break;
                case 34:
                    PlayModule("HOTEL", 2);
                    break;
                case 36:
                    PlayModule("HOTEL", 4);
                    _fanfareRestore = _lastOverride;
                    _fanfareCount = 60;
                    break;
                // jungle
                case 40:
                    PlayModule("JUNG", 1);
                    _fanfareRestore = _lastOverride;
                    _fanfareCount = 80;
                    _fluteCount = 100;
                    break;
                case 3:
                    PlayModule("JUNG", 2);
                    _fluteCount = 100;
                    break;
                // temple
                case 54:
                    PlayModule("TEMPLE", 1);
                    break;
                case 12:
                    PlayModule("TEMPLE", 2);
                    break;
                case 11:
                    PlayModule("TEMPLE", 3);
                    break;
                case 31:
                    PlayModule("TEMPLE", 4);
                    _fanfareRestore = _lastOverride;
                    _fanfareCount = 80;
                    break;
                // floda
                case 41:
                    PlayModule("FLODA", 4);
                    _fanfareRestore = _lastOverride;
                    _fanfareCount = 60;
                    break;
                case 13:
                    PlayModule("FLODA", 3);
                    break;
                case 16:
                    PlayModule("FLODA", 1);
                    break;
                case 17:
                    PlayModule("FLODA", 2);
                    break;
                case 43:
                    PlayModule("FLODA", 5);
                    break;
                // end credits
                case 67:
                    PlayModule("TITLE", 1);
                    break;
                // intro credits
                case 88:
                    PlayModule("TITLE", 1);
                    break;
                // valley
                case 90:
                    PlayModule("AWESTRUK", 1);
                    break;
                // confrontation
                case 91:
                    PlayModule("'JUNGLE'", 1);
                    break;
                // Frank
                case 46:
                    PlayModule("FRANK", 1);
                    break;
                // trader bob
                case 6:
                    PlayModule("BOB", 1);
                    break;
                // azura
                case 44:
                    PlayModule("AZURA", 1);
                    break;
                // amazon fortress
                case 21:
                    PlayModule("FORT", 1);
                    break;
                // rocket
                case 32:
                    PlayModule("ROCKET", 1);
                    break;
                // robot
                case 92:
                    PlayModule("ROBOT", 1);
                    break;
                default:
                    // song not available in the amiga version
                    return;
            }
            _lastOverride = song;
        }

        private bool PlaySpecialSfx(short sfx)
        {
            switch (sfx)
            {
                case 5: // normal volume
                    break;
                case 15: // soft volume
                    break;
                case 14: // medium volume
                    break;
                case 25: // open door
                    PlaySound("116BSSSS");
                    break;
                case 26: // close door
                    PlaySound("105ASSSS");
                    break;
                case 56: // light switch
                    PlaySound("27SSSSSS");
                    break;
                case 57: // hydraulic doors open
                    PlaySound("96SSSSSS");
                    break;
                case 58: // hydraulic doors close
                    PlaySound("97SSSSSS");
                    break;
                case 59: // metallic door slams
                    PlaySound("105SSSSS");
                    break;
                case 63: // oracle rezzes in
                    PlaySound("132SSSSS");
                    break;
                case 27: // cloth slide 1
                    PlaySound("135SSSSS");
                    break;
                case 83: // splash
                    PlaySound("18SSSSSS");
                    break;
                case 85: // agression enhancer
                    PlaySound("138BSSSS");
                    break;
                case 68: // dino ray
                    PlaySound("138SSSSS");
                    break;
                case 140: // dino transformation
                    PlaySound("55BSSSSS");
                    break;
                case 141: // experimental laser
                    PlaySound("55SSSSSS");
                    break;
                case 94: // plane hatch open
                    PlaySound("3SSSSSSS");
                    break;
                case 95: // plane hatch close
                    PlaySound("4SSSSSSS");
                    break;
                case 117: // oracle rezzes out
                    PlaySound("70SSSSSS");
                    break;
                case 124: // dino horn
                    PlaySound("103SSSSS");
                    break;
                case 127: // punch
                    PlaySound("128SSSSS");
                    break;
                case 128: // body hits ground
                    PlaySound("129SSSSS");
                    break;
                case 137: // explosion
                    PlaySound("88SSSSSS");
                    break;
                case 86: // stone door grind 1
                    PlaySound("1001SSSS");
                    break;
                case 188: // stone door grind 2
                    PlaySound("1002SSSS");
                    break;
                case 28: // cloth slide 2
                    PlaySound("1005SSSS");
                    break;
                case 151: // rattle bars
                    PlaySound("115SSSSS");
                    break;
                case 152: // door dissolves
                    PlaySound("56SSSSSS");
                    break;
                case 153: // altar slides
                    PlaySound("85SSSSSS");
                    break;
                case 166: // pull lever
                    PlaySound("1008SSSS");
                    break;
                case 182: // zap Frank
                    PlaySound("1023SSSS");
                    break;
                case 69: // splorch
                    PlaySound("137ASSSS");
                    break;
                case 70: // robot laser
                    PlaySound("61SSSSSS");
                    break;
                case 133: // pick hits stone
                    PlaySound("71SSSSSS");
                    break;
                case 165: // press button
                    PlaySound("1007SSSS");
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void PlayModule(string @base, int song)
        {
            _mixer.StopHandle(_modHandle);
            IAudioStream stream = LoadModule(@base, song);
            if (stream != null)
            {
                _modHandle = _mixer.PlayStream(SoundType.Music, stream);
            }
            _fanfareCount = 0;
        }

        private IAudioStream LoadModule(string @base, int num)
        {
            D.Debug(7, "AmigaSound::loadModule(%s, %d)", @base, num);
            string name;

            // load song/pattern data
            uint sngDataSize;
            name = $"{@base}.SNG";
            byte[] sngData = _vm.Resource.LoadFile(name, 0, out sngDataSize);
            var sngStr = new MemoryStream(sngData, 0, (int)sngDataSize);

            // load instruments/wave data
            uint insDataSize;
            name = $"{@base}.INS";
            byte[] insData = _vm.Resource.LoadFile(name, 0, out insDataSize);
            MemoryStream insStr = new MemoryStream(insData, 0, (int)insDataSize);

            // TODO: MakeRjp1Stream
            //IAudioStream stream = Audio.MakeRjp1Stream(&sngStr, &insStr, num, _mixer.OutputRate);
            IAudioStream stream = null;

            return stream;
        }

    }

    class Mp3Sound : PCSound
    {
        public Mp3Sound(IMixer mixer, QueenEngine vm)
            : base(mixer, vm)
        {
        }

        protected override void PlaySoundData(Stream f, uint size, ref SoundHandle soundHandle)
        {
            var mp3Stream = ServiceLocator.AudioManager.MakeMp3Stream(f);
            soundHandle = _mixer.PlayStream(SoundType.SFX, mp3Stream);
        }
    }

    class SilentSound : PCSound
    {
        public SilentSound(IMixer mixer, QueenEngine vm)
            : base(mixer, vm)
        {
        }

        protected override void PlaySoundData(Stream f, uint size, ref SoundHandle soundHandle)
        {
        }
    }

    class SBSound : PCSound
    {
        const int SB_HEADER_SIZE_V104 = 110;
        const int SB_HEADER_SIZE_V110 = 122;

        public SBSound(IMixer mixer, QueenEngine vm) : base(mixer, vm) { }

        protected override void PlaySoundData(Stream f, uint size, ref SoundHandle soundHandle)
        {
            // In order to simplify the code, we don't parse the .sb header but hard-code the
            // values. Refer to tracker item #1876741 for details on the format/fields.
            int headerSize;
            f.Seek(2, SeekOrigin.Current);
            var br = new BinaryReader(f);
            ushort version = br.ReadUInt16();
            switch (version)
            {
                case 104:
                    headerSize = SB_HEADER_SIZE_V104;
                    break;
                case 110:
                    headerSize = SB_HEADER_SIZE_V110;
                    break;
                default:
                    D.Warning("Unhandled SB file version %d, defaulting to 104", version);
                    headerSize = SB_HEADER_SIZE_V104;
                    break;
            }
            f.Seek(headerSize - 4, SeekOrigin.Current);
            size = (uint)(size - headerSize);
            byte[] sound = new byte[size];
            f.Read(sound, 0, (int)size);
            var type = (soundHandle == _speechHandle) ? SoundType.Speech : SoundType.SFX;

            var stream = new RawStream(AudioFlags.Unsigned, 11840, true, new MemoryStream(sound, 0, (int)size));
            soundHandle = _mixer.PlayStream(type, stream);
        }
    }

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
            set { _musicVolume = value; }
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
#if !USE_VORBIS
                    D.Warning("Using OGG compressed datafile, but OGG support not compiled in");
                    return new SilentSound(mixer, vm);
#else
                    return new OGGSound(mixer, vm);
#endif
                case Defines.COMPRESSION_FLAC:
#if !USE_FLAC
                    D.Warning("Using FLAC compressed datafile, but FLAC support not compiled in");
                    return new SilentSound(mixer, vm);
#else
                    return new FLACSound(mixer, vm);
#endif
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

