using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.OPL;
using NScumm.Core.Audio.OPL.DosBox;
using System;

namespace NScumm.Sky.Music
{
    class AdLibMusic : MusicBase, IAudioStream
    {
        public AdLibMusic(Mixer mixer, Disk disk)
            : base(mixer, disk)
        {
            _driverFileBase = 60202;
            _sampleRate = mixer.OutputRate;

            _opl = new DosBoxOPL(OplType.Opl2);
            _opl.Init(_sampleRate);

            _soundHandle = _mixer.PlayStream(SoundType.Music, this, -1, Mixer.MaxChannelVolume, 0, false, true);
        }

        public bool IsEndOfData
        {
            get
            {
                return false;
            }
        }

        public bool IsEndOfStream
        {
            get
            {
                return false;
            }
        }

        public bool IsStereo
        {
            get
            {
                return false;
            }
        }

        public int Rate
        {
            get
            {
                return _sampleRate;
            }
        }

        public override void Dispose()
        {
            _mixer.StopHandle(_soundHandle);
            base.Dispose();
        }

        public int ReadBuffer(short[] data, int numSamples)
        {
            if (_musicData == null)
            {
                // no music loaded
                Array.Clear(data, 0, numSamples);
            }
            else if ((_currentMusic == 0) || (_numberOfChannels == 0))
            {
                // music loaded but not played as of yet
                Array.Clear(data, 0, numSamples);
                // poll anyways as pollMusic() can activate the music
                PollMusic();
                _nextMusicPoll = _sampleRate / 50;
            }
            else
            {
                int render;
                int remaining = numSamples;
                int offset = 0;
                while (remaining != 0)
                {
                    render = (remaining > _nextMusicPoll) ? _nextMusicPoll : remaining;
                    remaining -= render;
                    _nextMusicPoll -= render;
                    _opl.ReadBuffer(data, offset, render);
                    offset += render;
                    if (_nextMusicPoll == 0)
                    {
                        PollMusic();
                        _nextMusicPoll = _sampleRate / 50;
                    }
                }
            }
            return numSamples;
        }

        protected override void SetupChannels(byte[] channelData, int offset)
        {
            _numberOfChannels = channelData[offset];
            offset++;
            for (byte cnt = 0; cnt < _numberOfChannels; cnt++)
            {
                ushort chDataStart = (ushort)(channelData.ToUInt16(offset + cnt * 2) + _musicDataLoc);
                _channels[cnt] = new AdLibChannel(_opl, _musicData, chDataStart);
            }
        }

        protected override void SetupPointers()
        {
            if (SystemVars.Instance.GameVersion.Version.Minor == 109)
            {
                // disk demo uses a different AdLib driver version, some offsets have changed
                //_musicDataLoc = (_musicData[0x11CC] << 8) | _musicData[0x11CB];
                //_initSequence = _musicData + 0xEC8;

                _musicDataLoc = _musicData.ToUInt16(0x1200);
                _initSequence = new ByteAccess(_musicData, 0xEFB);
            }
            else if (SystemVars.Instance.GameVersion.Version.Minor == 267)
            {
                _musicDataLoc = _musicData.ToUInt16(0x11F7);
                _initSequence = new ByteAccess(_musicData, 0xE87);
            }
            else
            {
                _musicDataLoc = _musicData.ToUInt16(0x1201);
                _initSequence = new ByteAccess(_musicData, 0xE91);
            }
            _nextMusicPoll = 0;
        }

        protected override void SetVolume(ushort param)
        {
            _musicVolume = param;
            // TODO:
            //_mixer.SetVolumeForSoundType(SoundType.Music, 2 * param);
        }

        protected override void StartDriver()
        {
            ushort cnt = 0;
            while (_initSequence[cnt] != 0 || _initSequence[cnt + 1] != 0)
            {
                _opl.WriteReg(_initSequence[cnt], _initSequence[cnt + 1]);
                cnt += 2;
            }
        }

        IOpl _opl;
        SoundHandle _soundHandle;
        private int _sampleRate, _nextMusicPoll;
        private ByteAccess _initSequence;
    }
}
