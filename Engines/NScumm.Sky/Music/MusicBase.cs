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
using NScumm.Core.Audio;
using NScumm.Core;

namespace NScumm.Sky.Music
{
    struct Actions
    {
        public byte MusicToProcess;
    }

    interface IChannelBase: IDisposable
    {
        byte Process(ushort aktTime);
        void UpdateVolume(ushort pVolume);
        bool IsActive { get; }
    }

    abstract class MusicBase: IDisposable
    {
        const int FILES_PER_SECTION = 4;

        protected MusicBase(IMixer mixer, Disk disk)
        {
            _mixer = mixer;
            _skyDisk = disk;
            _currentMusic = 0;
            _musicVolume = 127;
            _numberOfChannels = _currentMusic = 0;
        }

        public virtual void Dispose()
        {
            StopMusic();
        }

        public void LoadSection(byte pSection)
        {
            if (_currentMusic != 0)
                StopMusicInternal();
            lock (_mutex)
            {
                _musicData = null;
                _currentSection = pSection;
                _musicData = _skyDisk.LoadFile(_driverFileBase + FILES_PER_SECTION * pSection);

                _musicTempo0 = 0x78; // init constants taken from idb file, area ~0x1060
                _musicTempo1 = 0xC0;
                _onNextPoll.MusicToProcess = 0;
                _tempo = _aktTime = 0x10001;
                _numberOfChannels = _currentMusic = 0;
                SetupPointers();
                StartDriver();
            }
        }

        public void StartMusic(ushort param)
        {
            _onNextPoll.MusicToProcess = (byte)(param & 0xF);
        }

        public void StopMusic()
        {
            StopMusicInternal();
        }

        public bool IsPlaying
        {
            get
            {
                if (_mixer.IsSoundHandleActive(_musicHandle))
                    return true;
                for (byte cnt = 0; cnt < _numberOfChannels; cnt++)
                    if (_channels[cnt].IsActive)
                        return true;
                return false;
            }
        }

        public ushort Volume
        {
            get { return _musicVolume; }
            set { SetVolume(value); }
        }

        public byte CurrentMusic
        {
            get { return _currentMusic; }
        }


        protected abstract void SetupPointers();

        protected abstract void SetupChannels(byte[] channelData, int offset);

        protected abstract void StartDriver();

        protected abstract void SetVolume(ushort value);

        protected void UpdateTempo()
        {
            ushort tempoMul = (ushort)(_musicTempo0 * _musicTempo1);
            ushort divisor = 0x4446390 / 23864;
            _tempo = (uint)((tempoMul / divisor) << 16);
            _tempo |= (uint)((((tempoMul % divisor) << 16) | (tempoMul / divisor)) / divisor);
        }

        protected void LoadNewMusic()
        {
            ushort musicPos;
            if (_onNextPoll.MusicToProcess > _musicData[_musicDataLoc])
            {
                // TODO: error
                //error("Music %d requested but doesn't exist in file.", _onNextPoll.musicToProcess);
                return;
            }
            if (_currentMusic != 0)
                StopMusicInternal();

            _currentMusic = _onNextPoll.MusicToProcess;

            if (_currentMusic == 0)
                return;

            // Try playing digital audio first (from the Music Enhancement Project).
            // TODO: This always prefers digital music over the MIDI music types!
            byte section = _currentSection;
            byte song = _currentMusic;
            // handle duplicates
            if ((section == 2 && song == 1) || (section == 5 && song == 1))
            {
                section = 1;
                song = 1;
            }
            else if ((section == 2 && song == 4) || (section == 5 && song == 4))
            {
                section = 1;
                song = 4;
            }
            else if (section == 5 && song == 6)
            {
                section = 4;
                song = 4;
            }

            // TODO: SeekableAudioStream
            //var trackName = string.Format("music_{0}{1,02}", section, song);
            //var stream = SeekableAudioStream.OpenStreamFile(trackName);
            //if (stream)
            //{
            //    // not all tracks should loop
            //    bool loops = true;
            //    if ((section == 0 && song == 1)
            //     || (section == 1 && song == 1) || (section == 1 && song == 4)
            //     || (section == 2 && song == 1) || (section == 2 && song == 4)
            //     || (section == 4 && song == 2) || (section == 4 && song == 3)
            //     || (section == 4 && song == 5) || (section == 4 && song == 6)
            //     || (section == 4 && song == 11) || (section == 5 && song == 1)
            //     || (section == 5 && song == 3) || (section == 5 && song == 4))
            //        loops = false;
            //    _musicHandle = _mixer.PlayStream(SoundType.Music, new LoopingAudioStream(stream, loops ? 0 : 1));
            //    return;
            //}

            // no digital audio, resort to MIDI playback
            musicPos = _musicData.ToUInt16(_musicDataLoc + 1);
            musicPos += (ushort)(_musicDataLoc + ((_currentMusic - 1) << 1));
            musicPos = (ushort)(_musicData.ToUInt16(musicPos) + _musicDataLoc);

            _musicTempo0 = _musicData[musicPos];
            _musicTempo1 = _musicData[musicPos + 1];

            SetupChannels(_musicData, musicPos + 2);

            UpdateTempo();
        }

        protected void PollMusic()
        {
            lock (_mutex)
            {
                byte newTempo;
                if (_onNextPoll.MusicToProcess != _currentMusic)
                    LoadNewMusic();

                _aktTime += _tempo;

                for (byte cnt = 0; cnt < _numberOfChannels; cnt++)
                {
                    newTempo = _channels[cnt].Process((ushort)(_aktTime >> 16));
                    if (newTempo!=0)
                    {
                        _musicTempo1 = newTempo;
                        UpdateTempo();
                    }
                }
                _aktTime &= 0xFFFF;
            }
        }

        protected void StopMusicInternal()
        {
            _mixer.StopHandle(_musicHandle);

            lock (_mutex)
            {

                for (byte cnt = 0; cnt < _numberOfChannels; cnt++)
                {
                    _channels[cnt].Dispose();
                    _channels[cnt] = null;
                }
                _numberOfChannels = 0;
            }
        }

        protected IMixer _mixer;
        Disk _skyDisk;
        protected byte[] _musicData;

        protected ushort _musicDataLoc;
        protected ushort _driverFileBase;

        protected ushort _musicVolume, _numberOfChannels;
        protected byte _currentMusic, _currentSection;
        byte _musicTempo0; // can be changed by music stream
        byte _musicTempo1; // given once per music
        uint _tempo;      // calculated from musicTempo0 and musicTempo1
        uint _aktTime;
        Actions _onNextPoll;
        protected IChannelBase[] _channels = new IChannelBase[10];
        object _mutex = new object();
        SoundHandle _musicHandle;
    }
}
