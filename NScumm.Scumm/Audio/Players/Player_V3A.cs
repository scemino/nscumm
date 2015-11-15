//
//  Player_V3A.cs
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
using System.Diagnostics;
using NScumm.Core;
using NScumm.Scumm.Audio.Amiga;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Audio.Players
{
    /// <summary>
    /// Scumm V3 Amiga sound/music driver.
    /// </summary>
    class Player_V3A : IMusicEngine
    {
        public Player_V3A(ScummEngine scumm, IPlayerMod mod)
        {
            _vm = scumm;
            for (var i = 0; i < V3A_MAXMUS; i++)
            {
                _mus[i].id = 0;
                _mus[i].dur = 0;
            }
            for (var i = 0; i < V3A_MAXSFX; i++)
            {
                _sfx[i].id = 0;
                _sfx[i].dur = 0;
            }

            _curSong = 0;
            _songData = null;
            _songPtr = 0;
            _songDelay = 0;

            _music_timer = 0;

            _isinit = false;

            _mod = mod;
            _mod.SetUpdateProc(playMusic, 60);
        }

        public void Dispose()
        {
        }

        public void SetMusicVolume(int vol)
        {
            _mod.MusicVolume = vol;
        }

        public void StartSound(int nr)
        {
            Debug.Assert(_vm != null);
            var data = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, nr);
            Debug.Assert(data != null);

            if ((_vm.Game.GameId != GameId.Indy3) && (_vm.Game.GameId != GameId.Loom))
                Debug.WriteLine("player_v3a - unknown game");

            if (!_isinit)
            {
                int i;

                int offset = 4;
                int numInstruments;
                byte[] ptr;
                if (_vm.Game.GameId == GameId.Indy3)
                {
                    ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, 83);
                    numInstruments = 12;
                }
                else
                {
                    ptr = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, 79);
                    numInstruments = 9;
                }
                Debug.Assert(ptr != null);
                _wavetable = new instData[numInstruments + 1];
                for (i = 0; i < numInstruments; i++)
                {
                    _wavetable[i] = new instData();
                    for (int j = 0; j < 6; j++)
                    {
                        int off, len;
                        off = ptr.ToUInt16BigEndian(offset);
                        len = ptr.ToUInt16BigEndian(offset + 2);
                        _wavetable[i]._ilen[j] = (ushort)len;
                        if (len != 0)
                        {
                            _wavetable[i]._idat[j] = new byte[len];
                            Array.Copy(ptr, off, _wavetable[i]._idat[j], 0, len);
                        }
                        else
                            _wavetable[i]._idat[j] = null;
                        off = ptr.ToUInt16BigEndian(offset + 4);
                        len = ptr.ToUInt16BigEndian(offset + 6);
                        _wavetable[i]._llen[j] = (ushort)len;
                        if (len != 0)
                        {
                            _wavetable[i]._ldat[j] = new byte[len];
                            Array.Copy(ptr, off, _wavetable[i]._ldat[j], 0, len);
                        }
                        else
                            _wavetable[i]._ldat[j] = null;
                        _wavetable[i]._oct[j] = ptr.ToUInt16BigEndian(offset + 8);
                        offset += 10;
                    }
                    if (_vm.Game.GameId == GameId.Indy3)
                    {
                        _wavetable[i]._pitadjust = 0;
                        offset += 2;
                    }
                    else
                    {
                        _wavetable[i]._pitadjust = ptr.ToInt16BigEndian(offset + 2);
                        offset += 4;
                    }
                }
                _wavetable[i] = null;
                _isinit = true;
            }

            if (GetSoundStatus(nr) != 0)
                StopSound(nr);  // if a sound is playing, restart it

            if (data[26] != 0)
            {
                if (_curSong != 0)
                    StopSound(_curSong);
                _curSong = nr;
                _songData = data;
                _songPtr = 0x1C;
                _songDelay = 1;
                _music_timer = 0;
            }
            else
            {
                int size = data.ToUInt16BigEndian(12);
                int rate = 3579545 / data.ToUInt16BigEndian(20);
                var sound = new byte[size];
                int vol = (data[24] << 1) | (data[24] >> 5);    // if I boost this to 0-255, it gets too loud and starts to clip
                Array.Copy(data, data.ToUInt16BigEndian(8), sound, 0, size);
                int loopStart = 0, loopEnd = 0;
                int loopcount = data[27];
                if (loopcount > 1)
                {
                    loopStart = data.ToUInt16BigEndian(10) - data.ToUInt16BigEndian(8);
                    loopEnd = data.ToUInt16BigEndian(14);
                }
                int i = getSfxChan();
                if (i == -1)
                {
                    return;
                }
                _sfx[i].id = nr;
                _sfx[i].dur = 1 + loopcount * 60 * size / rate;
                if (data.ToUInt16BigEndian(16) != 0)
                {
                    _sfx[i].rate = (uint)(data.ToUInt16BigEndian(20) << 16);
                    _sfx[i].delta = data.ToInt32BigEndian(32);
                    _sfx[i].dur = data.ToInt32BigEndian(40);
                }
                else
                {
                    _sfx[i].delta = 0;
                }
                _mod.StartChannel(nr | 0x100, sound, size, rate, vol, loopStart, loopEnd);
            }
        }

        public void StopSound(int nr)
        {
            int i;
            if (nr == 0)
            {  // Amiga Loom does this near the end, when Chaos casts SILENCE on Hetchel
                StopAllSounds();
                return;
            }
            if (nr == _curSong)
            {
                for (i = 0; i < V3A_MAXMUS; i++)
                {
                    if (_mus[i].id != 0)
                        _mod.StopChannel(_mus[i].id);
                    _mus[i].id = 0;
                    _mus[i].dur = 0;
                }
                _curSong = 0;
                _songPtr = 0;
                _songDelay = 0;
                _songData = null;
            }
            else
            {
                i = getSfxChan(nr);
                if (i != -1)
                {
                    _mod.StopChannel(nr | 0x100);
                    _sfx[i].id = 0;
                    _sfx[i].dur = 0;
                }
            }
        }

        public void StopAllSounds()
        {
            int i;
            for (i = 0; i < V3A_MAXMUS; i++)
            {
                if (_mus[i].id != 0)
                    _mod.StopChannel(_mus[i].id);
                _mus[i].id = 0;
                _mus[i].dur = 0;
            }
            _curSong = 0;
            _songPtr = 0;
            _songDelay = 0;
            _songData = null;
            for (i = 0; i < V3A_MAXSFX; i++)
            {
                if (_sfx[i].id != 0)
                    _mod.StopChannel(_sfx[i].id | 0x100);
                _sfx[i].id = 0;
                _sfx[i].dur = 0;
            }
        }

        public int  GetMusicTimer()
        {
            return _music_timer / 30;
        }

        public int GetSoundStatus(int nr)
        {
            if (nr == _curSong)
                return 1;
            if (getSfxChan(nr) != -1)
                return 1;
            return 0;
        }

        void IMusicEngine.SaveOrLoad(Serializer serializer)
        {
        }

        int getMusChan(int id = 0)
        {
            int i;
            for (i = 0; i < V3A_MAXMUS; i++)
            {
                if (_mus[i].id == id)
                    break;
            }
            if (i == V3A_MAXMUS)
            {
                if (id == 0)
                    Debug.WriteLine("player_v3a - out of music channels");
                return -1;
            }
            return i;
        }

        int getSfxChan(int id = 0)
        {
            int i;
            for (i = 0; i < V3A_MAXSFX; i++)
            {
                if (_sfx[i].id == id)
                    break;
            }
            if (i == V3A_MAXSFX)
            {
                if (id == 0)
                    Debug.WriteLine("player_v3a - out of sfx channels");
                return -1;
            }
            return i;
        }

        void playMusic()
        {
            int i;
            for (i = 0; i < V3A_MAXMUS; i++)
            {
                if (_mus[i].id != 0)
                {
                    _mus[i].dur--;
                    if (_mus[i].dur != 0)
                        continue;
                    _mod.StopChannel(_mus[i].id);
                    _mus[i].id = 0;
                }
            }
            for (i = 0; i < V3A_MAXSFX; i++)
            {
                if (_sfx[i].id != 0)
                {
                    if (_sfx[i].delta != 0)
                    {
                        ushort oldrate = (ushort)(_sfx[i].rate >> 16);
                        _sfx[i].rate += (uint)(_sfx[i].delta);
                        if (_sfx[i].rate < (55 << 16))
                            _sfx[i].rate = 55 << 16;    // at rates below 55, frequency
                        ushort newrate = (ushort)(_sfx[i].rate >> 16);    // exceeds 65536, which is bad
                        if (oldrate != newrate)
                            _mod.SetChannelFreq(_sfx[i].id | 0x100, 3579545 / newrate);
                    }
                    _sfx[i].dur--;
                    if (_sfx[i].dur != 0)
                        continue;
                    _mod.StopChannel(_sfx[i].id | 0x100);
                    _sfx[i].id = 0;
                }
            }

            _music_timer++;
            if (_curSong == 0)
                return;
            if (_songDelay != 0 && --_songDelay != 0)
                return;
            if (_songPtr == 0)
            {
                // at the end of the song, and it wasn't looped - kill it
                _curSong = 0;
                return;
            }
            while (true)
            {
                int inst, pit, vol, dur, oct;
                inst = _songData[_songPtr++];
                if ((inst & 0xF0) != 0x80)
                {
                    // tune is at the end - figure out what's still playing
                    // and see how long we have to wait until we stop/restart
                    for (i = 0; i < V3A_MAXMUS; i++)
                    {
                        if (_songDelay < _mus[i].dur)
                            _songDelay = (ushort)_mus[i].dur;
                    }
                    if (inst == 0xFB)   // it's a looped song, restart it afterwards
                        _songPtr = 0x1C;
                    else
                        _songPtr = 0;   // otherwise, terminate it
                    break;
                }
                inst &= 0xF;
                pit = _songData[_songPtr++];
                vol = _songData[_songPtr++] & 0x7F; // if I boost this to 0-255, it gets too loud and starts to clip
                dur = _songData[_songPtr++];
                if (pit == 0)
                {
                    _songDelay = (ushort)dur;
                    break;
                }
                pit += _wavetable[inst]._pitadjust;
                oct = (pit / 12) - 2;
                pit = pit % 12;
                if (oct < 0)
                    oct = 0;
                if (oct > 5)
                    oct = 5;
                int rate = 3579545 / note_freqs[_wavetable[inst]._oct[oct], pit];
                if (_wavetable[inst]._llen[oct] == 0)
                    dur = _wavetable[inst]._ilen[oct] * 60 / rate;
                var data = new byte[_wavetable[inst]._ilen[oct] + _wavetable[inst]._llen[oct]];
                if (_wavetable[inst]._idat[oct] != null)
                    Array.Copy(_wavetable[inst]._idat[oct], data, _wavetable[inst]._ilen[oct]);
                if (_wavetable[inst]._ldat[oct] != null)
                    Array.Copy(_wavetable[inst]._ldat[oct], 0, data, _wavetable[inst]._ilen[oct], _wavetable[inst]._llen[oct]);

                i = getMusChan();
                if (i == -1)
                {
                    return;
                }
                _mus[i].id = i + 1;
                _mus[i].dur = dur + 1;
                _mod.StartChannel(_mus[i].id, data, _wavetable[inst]._ilen[oct] + _wavetable[inst]._llen[oct], rate, vol,
                    _wavetable[inst]._ilen[oct], _wavetable[inst]._ilen[oct] + _wavetable[inst]._llen[oct]);
            }
        }

        const int V3A_MAXMUS = 24;
        const int V3A_MAXSFX = 16;

        struct musChan
        {
            public int id;
            public int dur;
        }

        struct sfxChan
        {
            public int id;
            public int dur;
            public uint rate;
            public int delta;
        }

        class instData
        {
            public byte[][] _idat = new byte[6][];
            public ushort[] _ilen = new ushort[6];
            public byte[][] _ldat = new byte[6][];
            public ushort[] _llen = new ushort[6];
            public ushort[] _oct = new ushort[6];
            public short _pitadjust;
        }

        readonly ScummEngine _vm;
        readonly IPlayerMod _mod;

        readonly musChan[] _mus = new musChan[V3A_MAXMUS];
        readonly sfxChan[] _sfx = new sfxChan[V3A_MAXSFX];

        int _curSong;
        byte[] _songData;
        ushort _songPtr;
        ushort _songDelay;
        int _music_timer;
        bool _isinit;

        instData[] _wavetable;

        static readonly ushort[,] note_freqs = new ushort[4, 12]
        {
            { 0x06B0, 0x0650, 0x05F4, 0x05A0, 0x054C, 0x0500, 0x04B8, 0x0474, 0x0434, 0x03F8, 0x03C0, 0x0388 },
            { 0x0358, 0x0328, 0x02FA, 0x02D0, 0x02A6, 0x0280, 0x025C, 0x023A, 0x021A, 0x01FC, 0x01E0, 0x01C4 },
            { 0x01AC, 0x0194, 0x017D, 0x0168, 0x0153, 0x0140, 0x012E, 0x011D, 0x010D, 0x00FE, 0x00F0, 0x00E2 },
            { 0x00D6, 0x00CA, 0x00BE, 0x00B4, 0x00A9, 0x00A0, 0x0097, 0x008E, 0x0086, 0x007F, 0x00F0, 0x00E2 }
        };

    }
}

