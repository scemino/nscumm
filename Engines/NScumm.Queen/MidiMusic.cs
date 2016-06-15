//
//  MidiMusic.cs
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
using System.Diagnostics;
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    public class TuneData
    {
        public short[] tuneNum = new short[9];
        public short[] sfx = new short[2];
        public short mode;
        public short delay;

        public TuneData(short[] tuneNum, short[] sfx, short mode, short delay)
        {
            this.tuneNum = tuneNum;
            this.sfx = sfx;
            this.mode = mode;
            this.delay = delay;
        }
    }

    class MidiMusic : MidiDriverBase
    {
        const int MUSIC_QUEUE_SIZE = 14;

        MidiDriver _driver;
        MidiParser _parser;
        MidiChannel[] _channelsTable = new MidiChannel[16];
        byte[] _channelsVolume = new byte[16];
        bool _adlib;
        bool _nativeMT32;
        object _mutex = new object();
        Random _rnd;

        bool _isPlaying;
        bool _isLooping;
        bool _randomLoop;
        byte _masterVolume;
        byte _queuePos;
        short _currentSong;
        short _lastSong;    //first song from previous queue
        short[] _songQueue = new short[MUSIC_QUEUE_SIZE];

        ushort _numSongs;
        byte[] _buf;
        uint _musicDataSize;
        bool _vToggle;
        byte[] _musicData;
        TuneData[] _tune;

        public MidiMusic(QueenEngine vm)
        {
            _masterVolume = 192;
            var dev = MidiDriver.DetectDevice(MusicDriverTypes.Midi | MusicDriverTypes.AdLib, vm.Settings.AudioDevice);
            _adlib = (MidiDriver.GetMusicType(dev) == MusicType.AdLib);
            // TODO: _nativeMT32 = ((MidiDriver::getMusicType(dev) == MT_MT32) || ConfMan.getBool("native_mt32"));

            string musicDataFile;
            if (vm.Resource.IsDemo)
            {
                musicDataFile = "AQ8.RL";
            }
            else
            {
                musicDataFile = "AQ.RL";
            }

            if (_adlib)
            {
                musicDataFile = "AQBANK.MUS";
            }
            _musicData = vm.Resource.LoadFile(musicDataFile, 0, out _musicDataSize);
            _numSongs = _musicData.ToUInt16();
            _tune = vm.Resource.IsDemo ? Sound._tuneDemo : Sound._tune;

            if (_adlib)
            {
                //      int infoOffset = _numSongs * 4 + 2;
                //      if (READ_LE_UINT16(_musicData + 2) != infoOffset) {
                //          defaultAdLibVolume = _musicData[infoOffset];
                //      }
                _driver = C_Player_CreateAdLibMidiDriver(vm.Mixer);
            }
            else
            {
                throw new NotImplementedException();
                //_driver = MidiDriver.CreateMidi(dev);
                //if (_nativeMT32)
                //{
                //    _driver.Property(MidiDriver.PROP_CHANNEL_MASK, 0x03FE);
                //}
            }

            MidiDriverError ret = _driver.Open();
            Debug.Assert(ret == 0);
            _driver.SetTimerCallback(this, (object param) => OnTimer());

            if (_nativeMT32)
                _driver.SendMt32Reset();
            else
                _driver.SendGmReset();

            _parser = MidiParser.CreateSmfParser();
            _parser.MidiDriver = this;
            _parser.TimerRate = _driver.BaseTempo;
        }

        public void PlaySong(ushort songNum)
        {
            QueueClear();
            QueueSong(songNum);
            PlayMusic();
        }

        public void StopSong() { StopMusic(); }

        public void PlayMusic()
        {
            if (_songQueue[0] == 0)
            {
                D.Debug(5, "MidiMusic::playMusic - Music queue is empty");
                return;
            }

            ushort songNum = (ushort)_songQueue[_queuePos];

            //Special type
            // > 1000 && < 2000 . queue different tunelist
            // 2000 . repeat music from previous queue
            if (songNum > 999)
            {
                if ((songNum + 1) == 2000)
                {
                    songNum = (ushort)_lastSong;
                    QueueClear();
                    QueueSong(songNum);
                }
                else
                {
                    QueueTuneList((short)(songNum - 1000));
                    _queuePos = (byte)(_randomLoop ? RandomQueuePos() : 0);
                    songNum = (ushort)_songQueue[_queuePos];
                }
            }

            int prevSong = (int)SongOffset((ushort)_currentSong);
            if (_musicData[prevSong] == 'C' || _musicData[prevSong] == 'c')
            {
                _buf = null;
            }

            _currentSong = (short)songNum;
            if (songNum == 0)
            {
                StopMusic();
                return;
            }

            var musicPtr = new ByteAccess(_musicData, (int)SongOffset(songNum));
            uint size = SongLength(songNum);
            if (musicPtr.Value == 'C' || musicPtr.Value == 'c')
            {
                uint packedSize = SongLength(songNum) - 0x200;
                _buf = new byte[packedSize * 2];
                var buf = new UShortAccess(_buf);

                var data = new UShortAccess(musicPtr.Data, musicPtr.Offset + 1);
                var idx = new ByteAccess(data.Data, data.Offset + 0x200);

                for (int i = 0; i < packedSize; i++)
#if SCUMM_NEED_ALIGNMENT
            memcpy(&_buf[i], (byte *)((byte *)data + *(idx + i) * sizeof(uint16)), sizeof(uint16));
#else
                    buf[i] = data[idx[i]];
#endif

                var offs = (musicPtr.Value == 'c') ? 1 : 0;
                musicPtr = new ByteAccess(_buf, offs);
                size = (uint)(packedSize * 2 - offs);
            }

            StopMusic();

            lock (_mutex)
            {
                _parser.LoadMusic(musicPtr.Data, musicPtr.Offset, (int)size);
                _parser.ActiveTrack = 0;
                _isPlaying = true;

                D.Debug(8, $"Playing song {songNum} [queue position: {_queuePos}]");
                QueueUpdatePos();
            }
        }

        public void StopMusic()
        {
            lock (_mutex)
            {
                _isPlaying = false;
                _parser.UnloadMusic();
            }
        }

        public void ToggleVChange()
        {
            SetVolume(_vToggle ? (GetVolume() * 2) : (GetVolume() / 2));
            _vToggle = !_vToggle;
        }

        public int GetVolume() { return _masterVolume; }

        public void SetVolume(int volume)
        {
            if (volume < 0)
                volume = 0;
            else if (volume > 255)
                volume = 255;

            if (_masterVolume == volume)
                return;

            _masterVolume = (byte)volume;

            for (int i = 0; i < 16; ++i)
            {
                if (_channelsTable[i]!=null)
                    _channelsTable[i].Volume((byte)(_channelsVolume[i] * _masterVolume / 255));
            }
        }

        public override void Send(int b)
        {
            if (_adlib)
            {
                _driver.Send(b);
                return;
            }

            byte channel = (byte)(b & 0x0F);
            if ((b & 0xFFF0) == 0x07B0)
            {
                // Adjust volume changes by master volume
                byte volume = (byte)((b >> 16) & 0x7F);
                _channelsVolume[channel] = volume;
                volume = (byte)(volume * _masterVolume / 255);
                b = (int)((b & 0xFF00FFFF) | (volume << 16));
            }
            else if ((b & 0xF0) == 0xC0 && !_nativeMT32)
            {
                b = (int)((b & 0xFFFF00FF) | MidiDriver.Mt32ToGm[(b >> 8) & 0xFF] << 8);
            }
            else if ((b & 0xFFF0) == 0x007BB0)
            {
                //Only respond to All Notes Off if this channel
                //has currently been allocated
                if (_channelsTable[channel] == null)
                    return;
            }

            //Work around annoying loud notes in certain Roland Floda tunes
            if (channel == 3 && _currentSong == 90)
                return;
            if (channel == 4 && _currentSong == 27)
                return;
            if (channel == 5 && _currentSong == 38)
                return;

            if (_channelsTable[channel] == null)
                _channelsTable[channel] = (channel == 9) ? _driver.GetPercussionChannel() : _driver.AllocateChannel();

            if (_channelsTable[channel] != null)
                _channelsTable[channel].Send((uint)b);
        }

        private void QueueUpdatePos()
        {
            if (_randomLoop)
            {
                _queuePos = RandomQueuePos();
            }
            else
            {
                if (_queuePos < (MUSIC_QUEUE_SIZE - 1) && _songQueue[_queuePos + 1] != 0)
                    _queuePos++;
                else if (_isLooping)
                    _queuePos = 0;
            }
        }

        private uint SongLength(ushort songNum)
        {
            if (songNum < _numSongs)
                return (SongOffset((ushort)(songNum + 1)) - SongOffset(songNum));
            return (_musicDataSize - SongOffset(songNum));
        }

        private uint SongOffset(ushort songNum)
        {
            ushort offsLo = _musicData.ToUInt16((songNum * 4) + 2);
            ushort offsHi = _musicData.ToUInt16((songNum * 4) + 4);
            return (uint)((offsHi << 4) | offsLo);
        }

        private byte RandomQueuePos()
        {
            int queueSize = 0;
            for (int i = 0; i < MUSIC_QUEUE_SIZE; i++)
                if (_songQueue[i] != 0)
                    queueSize++;

            if (queueSize == 0)
                return 0;

            return (byte)_rnd.Next(1 + (queueSize - 1) & 0xFF);
        }

        public void QueueTuneList(short tuneList)
        {
            QueueClear();

            //Jungle is the only part of the game that uses multiple tunelists.
            //For the sake of code simplification we just hardcode the extended list ourselves
            if ((tuneList + 1) == 3)
            {
                _randomLoop = true;
                int i = 0;
                while (Sound._jungleList[i] != 0)
                    QueueSong((ushort)(Sound._jungleList[i++] - 1));
                return;
            }

            int mode = _tune[tuneList].mode;
            switch (mode)
            {
                case 0: // random loop
                    _randomLoop = true;
                    _isLooping = false;
                    break;
                case 1: // sequential loop
                    _isLooping = (_songQueue[1] == 0);
                    break;
                case 2: // play once
                default:
                    _isLooping = false;
                    break;
            }

            int j = 0;
            while (_tune[tuneList].tuneNum[j] != 0)
                QueueSong((ushort)(_tune[tuneList].tuneNum[j++] - 1));

            if (_randomLoop)
                _queuePos = RandomQueuePos();
        }

        private void QueueClear()
        {
            _lastSong = _songQueue[0];
            _queuePos = 0;
            _isLooping = _randomLoop = false;
            Array.Clear(_songQueue, 0, _songQueue.Length);
        }

        private bool QueueSong(ushort songNum)
        {
            if (songNum >= _numSongs && songNum < 1000)
            {
                // this happens at the end of the car chase, where we try to play song 176,
                // see Sound::_tune[], entry 39
                D.Debug(3, $"Trying to queue an invalid song number {songNum}, max {_numSongs}");
                return false;
            }
            byte emptySlots = 0;
            for (int i = 0; i < MUSIC_QUEUE_SIZE; i++)
                if (_songQueue[i] == 0)
                    emptySlots++;

            if (emptySlots == 0)
                return false;

            // Work around bug in Roland music, note that these numbers are 'one-off'
            // from the original code
            if (!_adlib && (songNum == 88 || songNum == 89))
                songNum = 62;

            _songQueue[MUSIC_QUEUE_SIZE - emptySlots] = (short)songNum;
            return true;
        }

        private void OnTimer()
        {
            lock (_mutex)
            {
                if (_isPlaying)
                    _parser.OnTimer();
            }
        }

        public override void MetaEvent(byte type, byte[] data, ushort length)
        {
            switch (type)
            {
                case 0x2F: // End of Track
                    if (_isLooping || _songQueue[1] != 0)
                    {
                        PlayMusic();
                    }
                    else
                    {
                        StopMusic();
                    }
                    break;
                case 0x7F: // Specific
                    if (_adlib)
                    {
                        _driver.MetaEvent(type, data, length);
                    }
                    break;
                default:
                    //D.Warning($"Unhandled meta event: {type:X2}");
                    break;
            }
        }

        private MidiDriver C_Player_CreateAdLibMidiDriver(IMixer mixer)
        {
            return new AdLibMidiDriver(mixer);
        }
    }
}
