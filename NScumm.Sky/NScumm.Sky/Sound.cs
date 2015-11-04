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
        const int MaxFxNumber = 393;

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

        public void RestoreSfx()
        {
            // queue sfx, so they will be started when the player exits the control panel
            Array.Clear(_sfxQueue, 0, _sfxQueue.Length);
            byte queueSlot = 0;
            if (_saveSounds[0] != 0xFFFF)
            {
                _sfxQueue[queueSlot].fxNo = (byte)_saveSounds[0];
                _sfxQueue[queueSlot].vol = (byte)(_saveSounds[0] >> 8);
                _sfxQueue[queueSlot].chan = 0;
                _sfxQueue[queueSlot].count = 1;
                queueSlot++;
            }
            if (_saveSounds[1] != 0xFFFF)
            {
                _sfxQueue[queueSlot].fxNo = (byte)_saveSounds[1];
                _sfxQueue[queueSlot].vol = (byte)(_saveSounds[1] >> 8);
                _sfxQueue[queueSlot].chan = 1;
                _sfxQueue[queueSlot].count = 1;
            }
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

        public void FnStopFx()
        {
            _mixer.StopID(SoundChannel0);
            _mixer.StopID(SoundChannel1);
            _saveSounds[0] = _saveSounds[1] = 0xFFFF;
        }

        public void FnPauseFx()
        {
            if (!_isPaused)
            {
                _isPaused = true;
                _mixer.PauseId(SoundChannel0, true);
                _mixer.PauseId(SoundChannel1, true);
            }
        }

        public void FnUnPauseFx()
        {
            if (_isPaused)
            {
                _isPaused = false;
                _mixer.PauseId(SoundChannel0, false);
                _mixer.PauseId(SoundChannel1, false);
            }
        }

        public void CheckFxQueue()
        {
            for (byte cnt = 0; cnt < MaxQueuedFx; cnt++)
            {
                if (_sfxQueue[cnt].count!=0)
                {
                    _sfxQueue[cnt].count--;
                    if (_sfxQueue[cnt].count == 0)
                        PlaySound(_sfxQueue[cnt].fxNo, _sfxQueue[cnt].vol, _sfxQueue[cnt].chan);
                }
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

        public ushort[] _saveSounds = new ushort[2];
        private byte _soundsTotal;

        Disk _skyDisk;
        ushort _sfxBaseOfs;
        byte[] _soundData;
        UShortAccess _sampleRates;
        UShortAccess _sfxInfo;
        byte _mainSfxVolume;

        bool _isPaused;

        static SfxQueue[] _sfxQueue = new SfxQueue[MaxQueuedFx];

        public void FnStartFx(uint sound, byte channel)
        {
            // TODO: FnStartFx
            //    _saveSounds[channel] = 0xFFFF;
            //    if (sound < 256 || sound > MaxFxNumber || (SystemVars.Instance.SystemFlags.HasFlag(SystemFlags.FX_OFF))
            //        return;

            //    byte screen = (byte)(_skyLogic.ScriptVariables[SCREEN] & 0xff);
            //    if (sound == 278 && screen == 25) // is this weld in room 25
            //        sound = 394;

            //    unchecked
            //    {
            //        sound &= (uint)(~(1 << 8));
            //    }

            //    Sfx sfx = musicList[sound];
            //    RoomList roomList = sfx->roomList;

            //    int i = 0;
            //    if (roomList[i].room != 0xff) // if room list empty then do all rooms
            //        while (roomList[i].room != screen)
            //        { // check rooms
            //            i++;
            //            if (roomList[i].room == 0xff)
            //                return;
            //        }

            //    // get fx volume

            //    byte volume = _mainSfxVolume; // start with standard vol

            //    if (SystemVars.Instance.SystemFlags & SF_SBLASTER)
            //        volume = roomList[i].adlibVolume;
            //    else if (SkyEngine::_systemVars.systemFlags & SF_ROLAND)
            //        volume = roomList[i].rolandVolume;
            //    volume = (volume * _mainSfxVolume) >> 8;

            //    // Check the flags, the sound may come on after a delay.
            //    if (sfx->flags & SFXF_START_DELAY)
            //    {
            //        for (uint8 cnt = 0; cnt < MAX_QUEUED_FX; cnt++)
            //        {
            //            if (_sfxQueue[cnt].count == 0)
            //            {
            //                _sfxQueue[cnt].chan = channel;
            //                _sfxQueue[cnt].fxNo = sfx->soundNo;
            //                _sfxQueue[cnt].vol = volume;
            //                _sfxQueue[cnt].count = sfx->flags & 0x7F;
            //                return;
            //            }
            //        }
            //        return; // ignore sound if it can't be queued
            //    }

            //    if (sfx->flags & SFXF_SAVE)
            //        _saveSounds[channel] = sfx->soundNo | (volume << 8);

            //    playSound(sfx->soundNo, volume, channel);
        }
    }
}
