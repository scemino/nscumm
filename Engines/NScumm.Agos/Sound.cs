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
using NScumm.Core.Audio.Decoders;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    [Flags]
    internal enum SoundTypeFlags
    {
        AMBIENT = 1 << 0,
        SFX = 1 << 1,
        SFX5 = 1 << 2
    }

    internal class Sound
    {
        public const bool SOUND_BIG_ENDIAN = true;
        private readonly AgosEngine _vm;

        private readonly IMixer _mixer;

        private BaseSound _voice;
        private BaseSound _effects;

        private bool _effectsPaused;
        private bool _ambientPaused;
        private bool _sfx5Paused;

        private short[] _filenums;
        private uint[] _offsets;
        private ushort _lastVoiceFile;

        private SoundHandle _voiceHandle;
        private SoundHandle _effectsHandle;
        private SoundHandle _ambientHandle;
        private SoundHandle _sfx5Handle;

        private bool _hasEffectsFile;
        private bool _hasVoiceFile;
        private ushort _ambientPlaying;

        // Personal Nightmare specfic
        private BytePtr _soundQueuePtr;
        private ushort _soundQueueNum;
        private uint _soundQueueSize;
        private ushort _soundQueueFreq;

        public bool IsSfxActive => _mixer.IsSoundHandleActive(_effectsHandle);

        public bool IsVoiceActive => _mixer.IsSoundHandleActive(_voiceHandle);

        public Sound(AgosEngine vm, GameSpecificSettings gss, IMixer mixer)
        {
            _vm = vm;
            _mixer = mixer;

            if (_vm._gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
            {
                LoadVoiceFile(gss);

                if (_vm._gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
                    LoadSfxFile(gss);
            }
        }

        public void StopVoice()
        {
            _mixer.StopHandle(_voiceHandle);
        }

        public void AmbientPause(bool b)
        {
            _ambientPaused = b;

            if (_ambientPaused && _ambientPlaying != 0)
            {
                _mixer.StopHandle(_ambientHandle);
            }
            else if (_ambientPlaying != 0)
            {
                uint tmp = _ambientPlaying;
                _ambientPlaying = 0;
                PlayAmbient((ushort) tmp);
            }
        }

        private void LoadSfxFile(GameSpecificSettings gss)
        {
            if (_hasEffectsFile)
                return;

            _effects = MakeSound(_mixer, gss.effects_filename);
            _hasEffectsFile = _effects != null;

            if (_hasEffectsFile)
                return;

            const bool dataIsUnsigned = true;

            if (Engine.FileExists(gss.effects_filename))
            {
                _hasEffectsFile = true;
                _effects = new VocSound(_mixer, gss.effects_filename, dataIsUnsigned);
            }
        }

        private static BaseSound MakeSound(IMixer mixer, string basename)
        {
#if USE_FLAC
            if (Engine.FileExists(basename + ".fla"))
                return new FLACSound(mixer, basename + ".fla");
#endif
#if USE_VORBIS
            if (Engine.FileExists(basename + ".ogg"))
                return new VorbisSound(mixer, basename + ".ogg");
#endif
#if USE_MAD
            if (Common::File::exists(basename + ".mp3"))
                return new MP3Sound(mixer, basename + ".mp3");
#endif
            if (Engine.FileExists(basename + ".wav"))
                return new WavSound(mixer, basename + ".wav");
            if (Engine.FileExists(basename + ".voc"))
                return new VocSound(mixer, basename + ".voc", true);
            return null;
        }

        private void LoadVoiceFile(GameSpecificSettings gss)
        {
            // Game versions which use separate voice files
            if (_hasVoiceFile || _vm.GameType == SIMONGameType.GType_FF || _vm.GameId == GameIds.GID_SIMON1CD32)
                return;

            _voice = MakeSound(_mixer, gss.speech_filename);
            _hasVoiceFile = _voice != null;

            if (_hasVoiceFile)
                return;

            if (_vm.GameType == SIMONGameType.GType_SIMON2)
            {
                // for simon2 mac/amiga, only read index file
                var file = Engine.OpenFileRead("voices.idx");
                if (file != null)
                {
                    var br = new BinaryReader(file);
                    int end = (int) file.Length;
                    _filenums = new short[end / 6 + 1];
                    _offsets = new uint[end / 6 + 1 + 1];

                    for (int i = 1; i <= end / 6; i++)
                    {
                        _filenums[i] = br.ReadInt16BigEndian();
                        _offsets[i] = br.ReadUInt32BigEndian();
                    }
                    // We need to add a terminator entry otherwise we get an out of
                    // bounds read when the offset table is accessed in
                    // BaseSound::getSoundStream.
                    _offsets[end / 6 + 1] = 0;

                    _hasVoiceFile = true;
                    return;
                }
            }

            const bool dataIsUnsigned = true;

            if (Engine.FileExists(gss.speech_filename))
            {
                _hasVoiceFile = true;
                if (_vm.GameType == SIMONGameType.GType_PP)
                    _voice = new WavSound(_mixer, gss.speech_filename);
                else
                    _voice = new VocSound(_mixer, gss.speech_filename, dataIsUnsigned);
            }
        }

        // This method is only used by Simon1 Amiga CD32 & Windows
        public void ReadSfxFile(string filename)
        {
            if (_hasEffectsFile)
                return;

            _mixer.StopHandle(_effectsHandle);

            if (!Engine.FileExists(filename))
            {
                Error("readSfxFile: Can't load sfx file {0}", filename);
            }

            bool dataIsUnsigned = _vm._gd.ADGameDescription.gameId != GameIds.GID_SIMON1CD32;

            if (_vm._gd.ADGameDescription.gameId == GameIds.GID_SIMON1CD32)
            {
                _effects = new VocSound(_mixer, filename, dataIsUnsigned, 0, SOUND_BIG_ENDIAN);
            }
            else
            {
                _effects = new WavSound(_mixer, filename);
            }
        }

        // Feeble Files specific
        public void PlayAmbientData(BytePtr soundData, ushort sound, short pan, short vol)
        {
            if (sound == _ambientPlaying)
                return;

            _ambientPlaying = sound;

            if (_ambientPaused)
                return;

            _mixer.StopHandle(_ambientHandle);
            _ambientHandle = PlaySoundData(soundData, sound, pan, vol, true);
        }

        public void PlaySfxData(BytePtr soundData, ushort sound, short pan, uint vol)
        {
            if (_effectsPaused)
                return;

            _effectsHandle = PlaySoundData(soundData, sound, pan, (int) vol, false);
        }

        private SoundHandle PlaySoundData(BytePtr soundData, uint sound, int pan = 0, int vol = 0, bool loop = false)
        {
            int size = soundData.ToInt32(4) + 8;
            var stream = new MemoryStream(soundData.Data, soundData.Offset, size);
            var sndStream = Wave.MakeWAVStream(stream, true);

            ConvertVolume(ref vol);
            ConvertPan(ref pan);

            return _mixer.PlayStream(SoundType.SFX,
                new Core.Audio.LoopingAudioStream(sndStream, loop ? 0 : 1),
                -1, vol, pan);
        }


        public void PlaySfx5Data(BytePtr soundData, ushort sound, short pan, short vol)
        {
            if (_sfx5Paused)
                return;

            _mixer.StopHandle(_sfx5Handle);
            _sfx5Handle = PlaySoundData(soundData, sound, pan, vol, true);
        }

        public void PlayAmbient(ushort sound)
        {
            if (_effects == null)
                return;

            if (sound == _ambientPlaying)
                return;

            _ambientPlaying = sound;

            if (_ambientPaused)
                return;

            _mixer.StopHandle(_ambientHandle);
            _ambientHandle = _effects.PlaySound(sound, SoundType.SFX, true);
        }

        public void PlayEffects(ushort sound)
        {
            if (_effects == null)
                return;

            if (_effectsPaused)
                return;

            if (_vm.GameType == SIMONGameType.GType_SIMON1)
                _mixer.StopHandle(_effectsHandle);
            _effectsHandle = _effects.PlaySound(sound, SoundType.SFX, false);
        }

        public void StopAllSfx()
        {
            _mixer.StopHandle(_ambientHandle);
            _mixer.StopHandle(_effectsHandle);
            _mixer.StopHandle(_sfx5Handle);
            _ambientPlaying = 0;
        }

        // This method is only used by Simon1 Amiga CD32
        public void ReadVoiceFile(string filename)
        {
            _mixer.StopHandle(_voiceHandle);

            if (!Engine.FileExists(filename))
                Error("readVoiceFile: Can't load voice file {0}", filename);

            const bool dataIsUnsigned = false;

            _voice = new RawSound(_mixer, filename, dataIsUnsigned);
        }

        private static void ConvertVolume(ref int vol)
        {
            // DirectSound was originally used, which specifies volume
            // and panning differently than ScummVM does, using a logarithmic scale
            // rather than a linear one.
            //
            // Volume is a value between -10,000 and 0.
            //
            // In both cases, the -10,000 represents -100 dB. When panning, only
            // one speaker's volume is affected - just like in ScummVM - with
            // negative values affecting the left speaker, and positive values
            // affecting the right speaker. Thus -10,000 means the left speaker is
            // silent.

            int v = ScummHelper.Clip(vol, -10000, 0);
            if (v != 0)
            {
                vol = (int) (Mixer.MaxChannelVolume * Math.Pow(10.0, v / 2000.0) + 0.5);
            }
            else
            {
                vol = Mixer.MaxChannelVolume;
            }
        }

        private static void ConvertPan(ref int pan)
        {
            // DirectSound was originally used, which specifies volume
            // and panning differently than ScummVM does, using a logarithmic scale
            // rather than a linear one.
            //
            // Panning is a value between -10,000 and 10,000.
            //
            // In both cases, the -10,000 represents -100 dB. When panning, only
            // one speaker's volume is affected - just like in ScummVM - with
            // negative values affecting the left speaker, and positive values
            // affecting the right speaker. Thus -10,000 means the left speaker is
            // silent.

            int p = ScummHelper.Clip(pan, -10000, 10000);
            if (p < 0)
            {
                pan = (int) (255.0 * Math.Pow(10.0, p / 2000.0) + 127.5);
            }
            else if (p > 0)
            {
                pan = (int) (255.0 * Math.Pow(10.0, p / -2000.0) - 127.5);
            }
            else
            {
                pan = 0;
            }
        }

        public void PlayVoiceData(byte[] soundData, uint sound)
        {
            _mixer.StopHandle(_voiceHandle);
            _voiceHandle = PlaySoundData(soundData, sound);
        }

        public void PlayVoice(uint sound)
        {
            if (_filenums != null)
            {
                if (_lastVoiceFile != _filenums[sound])
                {
                    _mixer.StopHandle(_voiceHandle);

                    _lastVoiceFile = (ushort) _filenums[sound];
                    var filename = "voices{_filenums[sound]}.dat";
                    if (!Engine.FileExists(filename))
                        Error("playVoice: Can't load voice file %s", filename);

                    _voice = new WavSound(_mixer, filename, _offsets);
                }
            }

            if (_voice == null)
                return;

            _mixer.StopHandle(_voiceHandle);
            if (_vm.GameType == SIMONGameType.GType_PP)
            {
                if (sound < 11)
                    _voiceHandle = _voice.PlaySound(sound, sound + 1, SoundType.Music, true, -1500);
                else
                    _voiceHandle = _voice.PlaySound(sound, sound, SoundType.Music, true);
            }
            else
            {
                _voiceHandle = _voice.PlaySound(sound, SoundType.Speech, false);
            }
        }

        public void QueueSound(BytePtr ptr, ushort sound, int size, ushort freq)
        {
            if (_effectsPaused)
                return;

            // Only a single sound can be queued
            _soundQueuePtr = ptr;
            _soundQueueNum = sound;
            _soundQueueSize = (uint) size;
            _soundQueueFreq = freq;
        }

        public void StopSfx()
        {
            _mixer.StopHandle(_effectsHandle);
        }

        // Elvira 1/2 and Waxworks specific
        public void PlayRawData(BytePtr soundData, ushort sound, int size, int freq)
        {
            if (_effectsPaused)
                return;

            var buffer = new byte[size];
            Array.Copy(soundData.Data, soundData.Offset, buffer, 0, size);

            AudioFlags flags = 0;
            if (_vm.GamePlatform == Platform.DOS && _vm.GameId != GameIds.GID_ELVIRA2)
                flags = AudioFlags.Unsigned;

            var stream = new RawStream(flags, freq, true, new MemoryStream(buffer, 0, size));
            _effectsHandle = _mixer.PlayStream(SoundType.SFX, stream);
        }

        public void EffectsPause(bool b)
        {
            _effectsPaused = b;
            _sfx5Paused = b;
        }
    }
}