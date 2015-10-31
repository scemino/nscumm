using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.Decoders;
using System;
using System.IO;

namespace NScumm.Sky
{
    struct SfxQueue
    {
        public byte count, fxNo, chan, vol;
    }

    class Sound : IDisposable
    {
        const int SoundFileBase = 60203;
        const int MaxQueuedFx = 4;

        const int SoundChannel0 = 0;
        const int SoundChannel1 = 1;
        public const int SOUND_BG = 2;

        public const int SoundVoice = 3;
        const int SoundSpeech = 4;

        public Sound(Mixer mixer, Disk disk, byte volume)
        {
            _skyDisk = disk;
            _mixer = mixer;
            _saveSounds[0] = _saveSounds[1] = 0xFFFF;
            _mainSfxVolume = volume;
        }

        ~Sound()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            _mixer.StopAll();
        }

        public SoundHandle PlaySound(int id, byte[] sound, int size)
        {
            var flags = AudioFlags.Unsigned;
            var sizeOfDataFileHeader = ServiceLocator.Platform.SizeOf<DataFileHeader>();
            size -= sizeOfDataFileHeader;
            var buffer = new byte[size];
            Array.Copy(sound, sizeOfDataFileHeader, buffer, 0, size);

            _mixer.StopID(id);

            var stream = new RawStream(flags, 11025, true, new MemoryStream(buffer, 0, size));
            return _mixer.PlayStream(SoundType.SFX, stream, id);
        }

        public void PlaySound(ushort sound, ushort volume, byte channel)
        {
            if (channel == 0)
                _mixer.StopID(SoundChannel0);
            else
                _mixer.StopID(SoundChannel1);

            if (_soundData == null)
            {
                // TODO: warning
                //warning("Sound::playSound(%04X, %04X) called with a section having been loaded", sound, volume);
                return;
            }

            if (sound > _soundsTotal)
            {
                // TODO: debug
                //debug(5, "Sound::playSound %d ignored, only %d sfx in file", sound, _soundsTotal);
                return;
            }

            volume = (ushort)((volume & 0x7F) << 1);
            sound &= 0xFF;

            // Note: All those tables are big endian. Don't ask me why. *sigh*

            // Use the sample rate from game data, see bug #1507757.
            ushort sampleRate = ScummHelper.SwapBytes(_sampleRates[sound << 2]);
            if (sampleRate > 11025)
                sampleRate = 11025;
            int dataOfs = ScummHelper.SwapBytes(_sfxInfo[((sound << 3) + 0) / 2]) << 4;
            int dataSize = ScummHelper.SwapBytes(_sfxInfo[((sound << 3) + 2) / 2]);
            int dataLoop = ScummHelper.SwapBytes(_sfxInfo[((sound << 3) + 6) / 2]);
            dataOfs += _sfxBaseOfs;

            var stream = new RawStream(AudioFlags.Unsigned, sampleRate, false, new MemoryStream(_soundData, dataOfs, dataSize));

            IAudioStream output = null;
            if (dataLoop != 0)
            {
                int loopSta = dataSize - dataLoop;
                int loopEnd = dataSize;

                output = new SubLoopingAudioStream(stream, 0, new Timestamp(0, loopSta, sampleRate), new Timestamp(0, loopEnd, sampleRate), true);
            }
            else
            {
                output = stream;
            }

            if (channel == 0)
                _ingameSound0 = _mixer.PlayStream(SoundType.SFX, output, SoundChannel0, volume, 0);
            else
                _ingameSound1 = _mixer.PlayStream(SoundType.SFX, output, SoundChannel1, volume, 0);
        }

        public void LoadSection(byte section)
        {
            FnStopFx();
            _mixer.StopAll();

            _soundData = _skyDisk.LoadFile(section * 4 + SoundFileBase);
            ushort asmOfs;
            if (SystemVars.Instance.GameVersion.Version.Minor == 109)
            {
                if (section == 0)
                    asmOfs = 0x78;
                else
                    asmOfs = 0x7C;
            }
            else
                asmOfs = 0x7E;

            if ((_soundData[asmOfs] != 0x3C) || (_soundData[asmOfs + 0x27] != 0x8D) ||
                (_soundData[asmOfs + 0x28] != 0x1E) || (_soundData[asmOfs + 0x2F] != 0x8D) ||
                (_soundData[asmOfs + 0x30] != 0x36))
                throw new NotSupportedException("Unknown sounddriver version");

            _soundsTotal = _soundData[asmOfs + 1];
            ushort sRateTabOfs = _soundData.ToUInt16(asmOfs + 0x29);
            _sfxBaseOfs = _soundData.ToUInt16(asmOfs + 0x31);
            _sampleRates = new UShortAccess(_soundData, sRateTabOfs);

            _sfxInfo = new UShortAccess(_soundData, _sfxBaseOfs);
            // if we just restored a savegame, the sfxqueue holds the sound we need to restart
            if (!SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.GAME_RESTORED))
                for (var cnt = 0; cnt < MaxQueuedFx; cnt++)
                    _sfxQueue[cnt].count = 0;
        }

        private void FnStopFx()
        {
            _mixer.StopID(SoundChannel0);
            _mixer.StopID(SoundChannel1);
            _saveSounds[0] = _saveSounds[1] = 0xFFFF;
        }

        private void FnPauseFx()
        {
            if (!_isPaused)
            {
                _isPaused = true;
                // TODO: PauseId
                //_mixer.PauseId(SOUND_CH0, true);
                //_mixer.PauseId(SOUND_CH1, true);
            }
        }

        private void FnUnPauseFx()
        {
            if (_isPaused)
            {
                _isPaused = false;
                // TODO: PauseId
                //_mixer.PauseId(SOUND_CH0, false);
                //_mixer.PauseId(SOUND_CH1, false);
            }
        }

        void StopSpeech()
        {
            _mixer.StopID(SoundSpeech);
        }

        Mixer _mixer;
        SoundHandle _voiceHandle;
        SoundHandle _effectHandle;
        SoundHandle _bgSoundHandle;
        SoundHandle _ingameSound0, _ingameSound1, _ingameSpeech;

        ushort[] _saveSounds = new ushort[2];
        private byte _soundsTotal;

        Disk _skyDisk;
        ushort _sfxBaseOfs;
        byte[] _soundData;
        UShortAccess _sampleRates;
        UShortAccess _sfxInfo;
        byte _mainSfxVolume;

        bool _isPaused;

        static SfxQueue[] _sfxQueue = new SfxQueue[MaxQueuedFx];
    }
}
