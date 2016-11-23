//
//  Player_MOD.cs
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
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.Decoders;
using NScumm.Scumm.Audio.Amiga;

namespace NScumm.Scumm.Audio.Players
{

    /// <summary>
    /// Generic Amiga MOD mixer - provides a 60Hz 'update' routine.
    /// </summary>
    class Player_MOD: IPlayerMod, IAudioStream
    {
        public int MusicVolume
        { 
            get{ return _maxvol; } 
            set { _maxvol = (byte)value; }
        }

        public int Rate { get { return _sampleRate; } }

        public bool IsStereo { get { return true; } }

        public bool IsEndOfData { get { return false; } }

        bool IAudioStream.IsEndOfStream { get { return false; } }

        public Player_MOD(IMixer mixer)
        {
            _mixer = mixer;
            _sampleRate = mixer.OutputRate;

            for (var i = 0; i < MOD_MAXCHANS; i++)
            {
                _channels[i].id = 0;
                _channels[i].vol = 0;
                _channels[i].freq = 0;
                _channels[i].input = null;
                _channels[i].ctr = 0;
                _channels[i].pos = 0;
            }

            _soundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false, true);
        }

        public void Dispose()
        {
            _mixer.StopHandle(_soundHandle);
            for (var i = 0; i < MOD_MAXCHANS; i++)
            {
                if (_channels[i].id == 0)
                    continue;
                _channels[i].input.Dispose();
            }
        }

        public int ReadBuffer(Ptr<short> buffer, int count)
        {
            do_mix(buffer, count / 2);
            return count;
        }

        public void SetMusicVolume(int vol)
        {
            _maxvol = (byte)vol;
        }

        public void SetUpdateProc(ModUpdateProc proc, int freq)
        {
            _playproc = proc;
            _mixamt = (uint)(_sampleRate / freq);
        }

        void ClearUpdateProc()
        {
            _playproc = null;
            _mixamt = 0;
        }

        public void StartChannel(int id, byte[] data, int size, int rate, int vol, int loopStart = 0, int loopEnd = 0, int pan = 0)
        {
            int i;
            if (id == 0)
            {
                Debug.WriteLine("player_mod - attempted to start channel id 0");
            }

            for (i = 0; i < MOD_MAXCHANS; i++)
            {
                if (_channels[i].id == 0)
                    break;
            }
            if (i == MOD_MAXCHANS)
            {
                Debug.WriteLine("player_mod - too many music channels playing ({0} max)", MOD_MAXCHANS);
                return;
            }
            _channels[i].id = id;
            _channels[i].vol = (byte)vol;
            _channels[i].pan = (sbyte)pan;
            _channels[i].freq = (ushort)rate;
            _channels[i].ctr = 0;

            var stream = new RawStream(AudioFlags.None, rate, true, new MemoryStream(data));
            if (loopStart != loopEnd)
            {
                _channels[i].input = new SubLoopingAudioStream(stream, 0, new Timestamp(0, loopStart, rate), new Timestamp(0, loopEnd, rate));
            }
            else
            {
                _channels[i].input = stream;
            }

            // read the first sample
            var sample = new short[1];
            _channels[i].input.ReadBuffer(sample,1);
            _channels[i].pos = sample[0];
        }

        public void StopChannel(int id)
        {
            if (id == 0)
            {
                Debug.WriteLine("player_mod - attempted to stop channel id 0");
            }
            for (int i = 0; i < MOD_MAXCHANS; i++)
            {
                if (_channels[i].id == id)
                {
//                    delete _channels[i].input;
                    _channels[i].input = null;
                    _channels[i].id = 0;
                    _channels[i].vol = 0;
                    _channels[i].freq = 0;
                    _channels[i].ctr = 0;
                    _channels[i].pos = 0;
                }
            }
        }

        public void SetChannelVol(int id, int vol)
        {
            if (id == 0)
            {
                Debug.WriteLine("player_mod - attempted to set volume for channel id 0");
            }

            for (int i = 0; i < MOD_MAXCHANS; i++)
            {
                if (_channels[i].id == id)
                {
                    _channels[i].vol = (byte)vol;
                    break;
                }
            }
        }

        void SetChannelPan(int id, sbyte pan)
        {
            if (id == 0)
            {
                Debug.WriteLine("player_mod - attempted to set pan for channel id 0");
            }
            for (int i = 0; i < MOD_MAXCHANS; i++)
            {
                if (_channels[i].id == id)
                {
                    _channels[i].pan = pan;
                    break;
                }
            }
        }

        public void SetChannelFreq(int id, int freq)
        {
            if (id == 0)
            {
                Debug.WriteLine("player_mod - attempted to set frequency for channel id 0");
            }
            for (int i = 0; i < MOD_MAXCHANS; i++)
            {
                if (_channels[i].id == id)
                {
                    if (freq > 31400)   // this is about as high as WinUAE goes
                        freq = 31400;   // can't easily verify on my own Amiga
                    _channels[i].freq = (ushort)freq;
                    break;
                }
            }
        }

        void do_mix(Ptr<short> data, int len)
        {
            Array.Clear(data.Data, data.Offset, data.Data.Length - data.Offset);
            while (len != 0)
            {
                uint dlen;
                if (_playproc != null)
                {
                    dlen = _mixamt - _mixpos;
                    if (_mixpos == 0)
                        _playproc();
                    if (dlen <= len)
                    {
                        _mixpos = 0;
                        len -= (int)dlen;
                    }
                    else
                    {
                        _mixpos = (uint)len;
                        dlen = (uint)len;
                        len = 0;
                    }
                }
                else
                {
                    dlen = (uint)len;
                    len = 0;
                }
                for (var i = 0; i < MOD_MAXCHANS; i++)
                {
                    if (_channels[i].id != 0)
                    {
                        ushort vol_l = (ushort)((127 - _channels[i].pan) * _channels[i].vol / 127);
                        ushort vol_r = (ushort)((127 + _channels[i].pan) * _channels[i].vol / 127);
                        for (var j = 0; j < dlen; j++)
                        {
                            // simple linear resample, unbuffered
                            int delta = _channels[i].freq * 0x10000 / _sampleRate;
                            ushort cfrac = (ushort)(~_channels[i].ctr & 0xFFFF);
                            if (_channels[i].ctr + delta < 0x10000)
                                cfrac = (ushort)delta;
                            _channels[i].ctr += (uint)delta;
                            int cpos = _channels[i].pos * cfrac / 0x10000;
                            while (_channels[i].ctr >= 0x10000)
                            {
                                var sample = new short[1];
                                if (_channels[i].input.ReadBuffer(sample, 1) != 1)
                                {    // out of data
                                    StopChannel(_channels[i].id);
                                    goto skipchan;  // exit 2 loops at once
                                }
                                _channels[i].pos = sample[0];
                                _channels[i].ctr -= 0x10000;
                                if (_channels[i].ctr > 0x10000)
                                    cpos += _channels[i].pos;
                                else
                                    cpos += (int)(_channels[i].pos * (_channels[i].ctr & 0xFFFF)) / 0x10000;
                            }
                            long pos = 0;
                            // if too many samples play in a row, the calculation below will overflow and clip
                            // so try and split it up into pieces it can manage comfortably
                            while (cpos < -0x8000)
                            {
                                pos -= 0x80000000 / delta;
                                cpos += 0x8000;
                            }
                            while (cpos > 0x7FFF)
                            {
                                pos += 0x7FFF0000 / delta;
                                cpos -= 0x7FFF;
                            }
                            pos += cpos * 0x10000 / delta;
                            RateHelper.ClampedAdd(ref data, (int)(pos * vol_l / Mixer.MaxMixerVolume));
                            data.Offset++;
                            RateHelper.ClampedAdd(ref data, (int)(pos * vol_r / Mixer.MaxMixerVolume));
                            data.Offset++;
                        }
                    }
                    skipchan:
                    ;   // channel ran out of data
                }
                data.Offset += (int)dlen;
            }
        }

        const int MOD_MAXCHANS = 24;

        struct soundChan
        {
            public int id;
            public byte vol;
            public sbyte pan;
            public ushort freq;

            public uint ctr;
            public short pos;
            public IAudioStream input;
        }

        IMixer _mixer;
        SoundHandle _soundHandle;

        uint _mixamt;
        uint _mixpos;
        readonly int _sampleRate;

        readonly soundChan[] _channels = new soundChan[MOD_MAXCHANS];

        byte _maxvol;

        ModUpdateProc _playproc;
    }
}
