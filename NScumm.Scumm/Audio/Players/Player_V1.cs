//
//  Player_V1.cs
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

namespace NScumm.Core.Audio
{
    class Player_V1: Player_V2
    {
        /// <summary>
        /// Feedback for white noise.
        /// </summary>
        const int FB_WNOISE = 0x12000;
        /// <summary>
        /// Feedback for periodic noise.
        /// </summary>
        const int FB_PNOISE = 0x08000;

        int _next_chunk;
        int _repeat_chunk;
        uint _chunk_type;
        uint _mplex_step;
        uint _mplex;
        uint _repeat_ctr;
        uint _freq_current;
        int _forced_level;
        ushort _random_lsr;
        Action<uint> _value_ptr;
        Action<uint> _value_ptr_2;
        uint _time_left;
        uint _start;
        uint _end;
        int _delta;
        uint _time_left_2;
        uint _start_2;
        int _delta_2;

        public Player_V1(ScummEngine scumm, IMixer mixer, bool pcjr)
            : base(scumm, mixer, pcjr)
        {
            // Initialize channel code
            for (int i = 0; i < 4; ++i)
                ClearChannel(i);

            _mplex_step = (uint)(_sampleRate << FIXP_SHIFT) / 1193000;
            _next_chunk = _repeat_chunk = 0;
            _forced_level = 0;
            _random_lsr = 0;
        }

        public override void StartSound(int nr)
        {
            lock (_mutex)
            {
                var data = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, nr);

                var offset = _pcjr ? BitConverter.ToUInt16(data, 4) : 6;
                int cprio = _current_data != null ? _current_data[0] & 0x7f : 0;
                int prio = data[offset] & 0x7f;
                var restartable = (data[offset] & 0x80) != 0;

                Debug.WriteLine("startSound {0}: prio {1}{2}, cprio {3}",
                    nr, prio, restartable ? " restartable" : "", cprio);

                if (_current_nr == 0 || cprio <= prio)
                {
                    if (_current_data != null && ((_current_data[0] & 0x80) != 0))
                    {
                        _next_nr = _current_nr;
                        _next_data = _current_data;
                    }

                    var d = new byte[data.Length - offset];
                    Array.Copy(data, offset, d, 0, d.Length);
                    ChainSound(nr, d);
                }
            }
        }

        public override void StopAllSounds()
        {
            lock (_mutex)
            {
                for (int i = 0; i < 4; i++)
                    ClearChannel(i);
                _repeat_chunk = _next_chunk = 0;
                _next_nr = _current_nr = 0;
                _next_data = _current_data = null;
            }
        }

        public override void StopSound(int nr)
        {
            lock (_mutex)
            {
                if (_next_nr == nr)
                {
                    _next_nr = 0;
                    _next_data = null;
                }
                if (_current_nr == nr)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        ClearChannel(i);
                    }
                    _repeat_chunk = _next_chunk = 0;
                    _current_nr = 0;
                    _current_data = null;
                    ChainNextSound();
                }
            }
        }

        public override int GetMusicTimer()
        {
            /* Do V1 games have a music timer? */
            return 0;
        }

        protected override void NextTick()
        {
            if (_next_chunk != 0)
            {
                if (_pcjr)
                    NextPCjrCmd();
                else
                    NextSpeakerCmd();
            }
        }

        protected override void ClearChannel(int i)
        {
            _channels[i].freq = 0;
            _channels[i].volume = 15;
        }

        protected override void ChainSound(int nr, byte[] data)
        {
            for (var i = 0; i < 4; ++i)
                ClearChannel(i);

            _current_nr = nr;
            _current_data = data;
            _repeat_chunk = _next_chunk = (_pcjr ? 2 : 4);

            Debug.WriteLine("Chaining new sound {0}", nr);
            if (_pcjr)
                ParsePCjrChunk();
            else
                ParseSpeakerChunk();
        }

        protected override void GenerateSpkSamples(short[] data, int offset, int len)
        {
            Array.Clear(data, offset, 2 * len);
            if (_channels[0].freq == 0)
            {
                if (_forced_level != 0)
                {
                    int sample = (int)(_forced_level * _volumetable[0]);
                    for (var i = 0; i < len; i++)
                        data[offset + 2 * i] = data[offset + 2 * i + 1] = (short)sample;
                    Debug.WriteLine("speaker: {0}: forced one", _tick_len);
                }
                else if (_level == 0)
                {
                    return;
                }
            }
            else
            {
                SquareGenerator(0, _channels[0].freq, 0, 0, data, offset, len);
                Debug.WriteLine("speaker: {0:X}: freq {1} {2:F1}", _tick_len,
                    _channels[0].freq, 1193000.0 / _channels[0].freq);
            }
            LowPassFilter(data, offset, len);
        }

        protected override void GeneratePCjrSamples(short[] data, int offset, int len)
        {
            bool hasData = false;

            Array.Clear(data, offset, 2 * len);

            if (_forced_level != 0)
            {
                int sample = (int)(_forced_level * _volumetable[0]);
                for (var i = 0; i < len; i++)
                    data[offset + 2 * i] = data[offset + 2 * i + 1] = (short)sample;
                hasData = true;
                Debug.WriteLine("channel[4]: {0:X}: forced one", _tick_len);
            }

            for (var i = 1; i < 3; i++)
            {
                var freq = _channels[i].freq;
                if (freq != 0)
                {
                    for (var j = 0; j < i; j++)
                    {
                        if (freq == _channels[j].freq)
                        {
                            /* HACK: this channel is playing at
                     * the same frequency as another.
                     * Synchronize it to the same phase to
                     * prevent interference.
                     */
                            _timer_count[i] = _timer_count[j];
                            _timer_output ^= (1 << i) &
                            (_timer_output ^ _timer_output << (i - j));
                        }
                    }
                }
            }

            for (var i = 0; i < 4; i++)
            {
                var freq = _channels[i].freq;
                var vol = _channels[i].volume;
                if (_volumetable[_channels[i].volume] == 0)
                {
                    _timer_count[i] -= len << FIXP_SHIFT;
                    if (_timer_count[i] < 0)
                        _timer_count[i] = 0;
                }
                else if (i < 3)
                {
                    hasData = true;
                    SquareGenerator(i, freq, (int)vol, 0, data, offset, len);
                    Debug.WriteLine("channel[{0}]: {1:X}: freq {2} {3:F1} ; volume {4}",
                        i, _tick_len, freq, 111860.0 / freq, vol);
                }
                else
                {
                    int noiseFB = ((freq & 4) != 0) ? FB_WNOISE : FB_PNOISE;
                    int n = (freq & 3);

                    freq = (n == 3) ? 2 * (_channels[2].freq) : 1 << (5 + n);
                    hasData = true;
                    SquareGenerator(i, freq, (int)vol, noiseFB, data, offset, len);
                    Debug.WriteLine("channel[{0}]: {1:X}: noise freq {2} {3:F1} ; volume {4}",
                        i, _tick_len, freq, 111860.0 / freq, vol);
                }
            }

            if (_level != 0 || hasData)
                LowPassFilter(data, offset, len);
        }

        void SetMplex(uint mplex)
        {
            if (mplex == 0)
                mplex = 65536;
            _mplex = mplex;
            _tick_len = (uint)_mplex_step * mplex;
        }

        void ParseSpeakerChunk()
        {
            SetMplex(3000);
            _forced_level = 0;

            parse_again:
            _chunk_type = BitConverter.ToUInt16(_current_data, _next_chunk);
            Debug.WriteLine("ParseSpeakerChunk: sound {0}, offset {1:X}, chunk {2:X}",
                _current_nr, _next_chunk, _chunk_type);

            _next_chunk += 2;
            switch (_chunk_type)
            {
                case 0xffff:
                    _current_nr = 0;
                    _current_data = null;
                    _channels[0].freq = 0;
                    _next_chunk = 0;
                    ChainNextSound();
                    break;
                case 0xfffe:
                    _repeat_chunk = _next_chunk;
                    goto parse_again;

                case 0xfffd:
                    _next_chunk = _repeat_chunk;
                    goto parse_again;

                case 0xfffc:
                    /* handle reset. We don't need this do we? */
                    goto parse_again;

                case 0:
                    _time_left = 1;
                    SetMplex(BitConverter.ToUInt16(_current_data, _next_chunk));
                    _next_chunk += 2;
                    break;
                case 1:
                    SetMplex(BitConverter.ToUInt16(_current_data, _next_chunk));
                    _start = BitConverter.ToUInt16(_current_data, _next_chunk + 2);
                    _end = BitConverter.ToUInt16(_current_data, _next_chunk + 4);
                    _delta = BitConverter.ToInt16(_current_data, _next_chunk + 6);
                    _repeat_ctr = BitConverter.ToUInt16(_current_data, _next_chunk + 8);
                    _channels[0].freq = (ushort)_start;
                    _next_chunk += 10;
                    Debug.WriteLine("chunk 1: mplex {0}, freq {1} -> {2} step {3}  x {4}",
                        _mplex, _start, _end, _delta, _repeat_ctr);
                    break;
                case 2:
                    _start = BitConverter.ToUInt16(_current_data, _next_chunk);
                    _end = BitConverter.ToUInt16(_current_data, _next_chunk + 2);
                    _delta = BitConverter.ToInt16(_current_data, _next_chunk + 4);
                    _channels[0].freq = 0;
                    _next_chunk += 6;
                    _forced_level = -1;
                    Debug.WriteLine("chunk 2: {0} -> {1} step {2}",
                        _start, _end, _delta);
                    break;
                case 3:
                    _start = BitConverter.ToUInt16(_current_data, _next_chunk);
                    _end = BitConverter.ToUInt16(_current_data, _next_chunk + 2);
                    _delta = BitConverter.ToInt16(_current_data, _next_chunk + 4);
                    _channels[0].freq = 0;
                    _next_chunk += 6;
                    _forced_level = -1;
                    Debug.WriteLine("chunk 3: {0} -> {1} step {2}",
                        _start, _end, _delta);
                    break;
            }
        }

        void ParsePCjrChunk()
        {
            SetMplex(3000);
            _forced_level = 0;

            parse_again:

            _chunk_type = BitConverter.ToUInt16(_current_data, _next_chunk);
            Debug.WriteLine("parsePCjrChunk: sound {0}, offset {1:X}, chunk {2:X}",
                _current_nr, _next_chunk, _chunk_type);

            _next_chunk += 2;
            switch (_chunk_type)
            {
                case 0xffff:
                    for (var i = 0; i < 4; ++i)
                        ClearChannel(i);
                    _current_nr = 0;
                    _current_data = null;
                    _repeat_chunk = _next_chunk = 0;
                    ChainNextSound();
                    break;

                case 0xfffe:
                    _repeat_chunk = _next_chunk;
                    goto parse_again;

                case 0xfffd:
                    _next_chunk = _repeat_chunk;
                    goto parse_again;

                case 0xfffc:
                    /* handle reset. We don't need this do we? */
                    goto parse_again;

                case 0:
                    {
                        SetMplex(BitConverter.ToUInt16(_current_data, _next_chunk));
                        _next_chunk += 2;
                        for (var i = 0; i < 4; i++)
                        {
                            var tmp = (int)BitConverter.ToUInt16(_current_data, _next_chunk);
                            _next_chunk += 2;
                            if (tmp == 0xffff)
                            {
                                _channels[i].cmd_ptr = null;
                                continue;
                            }
                            _channels[i].attack = BitConverter.ToUInt16(_current_data, tmp);
                            _channels[i].decay = BitConverter.ToUInt16(_current_data, tmp + 2);
                            _channels[i].level = BitConverter.ToUInt16(_current_data, tmp + 4);
                            _channels[i].sustain_1 = BitConverter.ToUInt16(_current_data, tmp + 6);
                            _channels[i].sustain_2 = BitConverter.ToUInt16(_current_data, tmp + 8);
                            _channels[i].notelen = 1;
                            _channels[i].volume = 15;
                            _channels[i].cmd_ptr = tmp + 10;
                        }
                    }
                    break;

                case 1:
                    {
                        SetMplex(BitConverter.ToUInt16(_current_data, _next_chunk));
                        var tmp = BitConverter.ToUInt16(_current_data, _next_chunk + 2);
                        _channels[0].cmd_ptr = tmp != 0xffff ? (int?)tmp : null;
                        tmp = BitConverter.ToUInt16(_current_data, _next_chunk + 4);
                        _start = BitConverter.ToUInt16(_current_data, _next_chunk + 6);
                        _delta = BitConverter.ToInt16(_current_data, _next_chunk + 8);
                        _time_left = BitConverter.ToUInt16(_current_data, _next_chunk + 10);
                        _next_chunk += 12;
                        bool volume = false;
                        if (tmp >= 0xe0)
                        {
                            _channels[3].freq = tmp & 0xf;
                            _value_ptr = value => _channels[3].volume = value;
                            volume = true;
                        }
                        else
                        {
                            Debug.Assert((tmp & 0x10) == 0);
                            tmp = (ushort)((tmp & 0x60) >> 5);
                            _value_ptr = value => _channels[tmp].freq = (int)value;
                            _channels[tmp].volume = 0;
                        }
                        _value_ptr(_start);
                        if (_channels[0].cmd_ptr.HasValue)
                        {
                            tmp = BitConverter.ToUInt16(_current_data, _channels[0].cmd_ptr.Value);
                            _start_2 = BitConverter.ToUInt16(_current_data, _channels[0].cmd_ptr.Value + 2);
                            _delta_2 = BitConverter.ToInt16(_current_data, _channels[0].cmd_ptr.Value + 4);
                            _time_left_2 = BitConverter.ToUInt16(_current_data, _channels[0].cmd_ptr.Value + 6);
                            _channels[0].cmd_ptr += 8;
                            if (volume)
                            {
                                tmp = (ushort)((tmp & 0x70) >> 4);
                                if ((tmp & 1) != 0)
                                {
                                    _value_ptr_2 = value => _channels[tmp >> 1].volume = value;
                                }
                                else
                                {
                                    _value_ptr_2 = value => _channels[tmp >> 1].freq = (int)value;
                                }
                            }
                            else
                            {
                                Debug.Assert((tmp & 0x10) == 0);
                                tmp = (ushort)((tmp & 0x60) >> 5);
                                _value_ptr_2 = value => _channels[tmp].freq = (int)value;
                                _channels[tmp].volume = 0;
                            }
                            _value_ptr_2(_start_2);
                        }
//                        Debug.WriteLine("chunk 1: {0}: {1} step {2} for {3}, {4}: {5} step {6} for {7}",
//                            (long)(_value_ptr - (uint*)_channels), _start, _delta, _time_left,
//                            (long)(_value_ptr_2 - (uint*)_channels), _start_2, _delta_2, _time_left_2);
                    }
                    break;

                case 2:
                    _start = BitConverter.ToUInt16(_current_data, _next_chunk);
                    _end = BitConverter.ToUInt16(_current_data, _next_chunk + 2);
                    _delta = BitConverter.ToInt16(_current_data, _next_chunk + 4);
                    _channels[0].freq = 0;
                    _next_chunk += 6;
                    _forced_level = -1;
                    Debug.WriteLine("chunk 2: {0} -> {1} step {2}",
                        _start, _end, _delta);
                    break;
                case 3:
                    {
                        SetMplex(BitConverter.ToUInt16(_current_data, _next_chunk));
                        var tmp = BitConverter.ToUInt16(_current_data, _next_chunk + 2);
                        Debug.Assert((tmp & 0xf0) == 0xe0);
                        _channels[3].freq = (int)(tmp & 0xf);
                        if ((tmp & 3) == 3)
                        {
                            _next_chunk += 2;
                            _channels[2].freq = BitConverter.ToUInt16(_current_data, _next_chunk + 2);
                        }
                        _channels[3].volume = BitConverter.ToUInt16(_current_data, _next_chunk + 4);
                        _repeat_ctr = BitConverter.ToUInt16(_current_data, _next_chunk + 6);
                        _delta = BitConverter.ToInt16(_current_data, _next_chunk + 8);
                        _next_chunk += 10;
                    }
                    break;
            }
        }

        void NextSpeakerCmd()
        {
            ushort lsr;
            switch (_chunk_type)
            {
                case 0:
                    if ((--_time_left) != 0)
                        return;
                    _time_left = BitConverter.ToUInt16(_current_data, _next_chunk);
                    _next_chunk += 2;
                    if (_time_left == 0xfffb)
                    {
                        /* handle 0xfffb?? */
                        _time_left = BitConverter.ToUInt16(_current_data, _next_chunk);
                        _next_chunk += 2;
                    }
                    Debug.WriteLine("nextSpeakerCmd: chunk {0}, offset {1:X}: notelen {2}",
                        _chunk_type, _next_chunk - 2, _time_left);
                    if (_time_left == 0)
                    {
                        ParseSpeakerChunk();
                    }
                    else
                    {
                        _channels[0].freq = BitConverter.ToUInt16(_current_data, _next_chunk);
                        _next_chunk += 2;
                        Debug.WriteLine("freq_current: {0}", _channels[0].freq);
                    }
                    break;

                case 1:
                    _channels[0].freq = (ushort)((_channels[0].freq + _delta) & 0xffff);
                    if (_channels[0].freq == _end)
                    {
                        if ((--_repeat_ctr) == 0)
                        {
                            ParseSpeakerChunk();
                            return;
                        }
                        _channels[0].freq = (ushort)_start;
                    }
                    break;

                case 2:
                    _start = (uint)(_start + _delta) & 0xffff;
                    if (_start == _end)
                    {
                        ParseSpeakerChunk();
                        return;
                    }
                    SetMplex(_start);
                    _forced_level = -_forced_level;
                    break;
                case 3:
                    _start = (uint)(_start + _delta) & 0xffff;
                    if (_start == _end)
                    {
                        ParseSpeakerChunk();
                        return;
                    }
                    lsr = (ushort)(_random_lsr + 0x9248);
                    lsr = (ushort)((lsr >> 3) | (lsr << 13));
                    _random_lsr = lsr;
                    SetMplex((_start & lsr) | 0x180);
                    _forced_level = -_forced_level;
                    break;
            }
        }

        void NextPCjrCmd()
        {
            uint i;
            int dummy;
            switch (_chunk_type)
            {
                case 0:
                    for (i = 0; i < 4; i++)
                    {
                        if (!_channels[i].cmd_ptr.HasValue)
                            continue;
                        if (--_channels[i].notelen == 0)
                        {
                            dummy = BitConverter.ToUInt16(_current_data, _channels[i].cmd_ptr.Value);
                            if (dummy >= 0xfffe)
                            {
                                if (dummy == 0xfffe)
                                    _next_chunk = 2;
                                ParsePCjrChunk();
                                return;
                            }
                            _channels[i].notelen = (uint)(4 * dummy);
                            dummy = BitConverter.ToUInt16(_current_data, _channels[i].cmd_ptr.Value + 2);
                            if (dummy == 0)
                            {
                                _channels[i].hull_counter = 4;
                                _channels[i].sustctr = (int)(_channels[i].sustain_2);
                            }
                            else
                            {
                                _channels[i].hull_counter = 1;
                                _channels[i].freq = dummy;
                            }
                            Debug.WriteLine("chunk 0: channel {0} play {1} for {2}",
                                i, dummy, _channels[i].notelen);
                            _channels[i].cmd_ptr += 4;
                        }


                        switch (_channels[i].hull_counter)
                        {
                            case 1:
                                _channels[i].volume -= _channels[i].attack;
                                if ((int)_channels[i].volume <= 0)
                                {
                                    _channels[i].volume = 0;
                                    _channels[i].hull_counter++;
                                }
                                break;
                            case 2:
                                _channels[i].volume += _channels[i].decay;
                                if (_channels[i].volume >= _channels[i].level)
                                {
                                    _channels[i].volume = _channels[i].level;
                                    _channels[i].hull_counter++;
                                }
                                break;
                            case 4:
                                if (--_channels[i].sustctr < 0)
                                {
                                    _channels[i].sustctr = (int)(_channels[i].sustain_2);
                                    _channels[i].volume += _channels[i].sustain_1;
                                    if ((int)_channels[i].volume >= 15)
                                    {
                                        _channels[i].volume = 15;
                                        _channels[i].hull_counter++;
                                    }
                                }
                                break;
                        }
                    }
                    break;

                case 1:
                    _start += (uint)_delta;
                    _value_ptr(_start);
                    if (--_time_left == 0)
                    {
                        _start = BitConverter.ToUInt16(_current_data, _next_chunk);
                        _next_chunk += 2;
                        if (_start == 0xffff)
                        {
                            ParsePCjrChunk();
                            return;
                        }
                        _delta = BitConverter.ToUInt16(_current_data, _next_chunk);
                        _time_left = BitConverter.ToUInt16(_current_data, _next_chunk + 2);
                        _next_chunk += 4;
                        _value_ptr(_start);
                    }

                    if (_channels[0].cmd_ptr.HasValue)
                    {
                        _start_2 += (uint)_delta_2;
                        _value_ptr_2(_start_2);
                        if (--_time_left_2 == 0)
                        {
                            _start_2 = BitConverter.ToUInt16(_current_data, _channels[0].cmd_ptr.Value);
                            if (_start_2 == 0xffff)
                            {
                                _next_chunk = _channels[0].cmd_ptr.Value + 2;
                                ParsePCjrChunk();
                                return;
                            }
                            _delta_2 = BitConverter.ToInt16(_current_data, _channels[0].cmd_ptr.Value + 2);
                            _time_left_2 = BitConverter.ToUInt16(_current_data, _channels[0].cmd_ptr.Value + 4);
                            _channels[0].cmd_ptr += 6;
                        }
                    }
                    break;

                case 2:
                    _start += (uint)_delta;
                    if (_start == _end)
                    {
                        ParsePCjrChunk();
                        return;
                    }
                    SetMplex(_start);
                    Debug.WriteLine("chunk 2: mplex {0} curve {1}", _start, _forced_level);
                    _forced_level = -_forced_level;
                    break;
                case 3:
                    dummy = (int)(_channels[3].volume + _delta);
                    if (dummy >= 15)
                    {
                        _channels[3].volume = 15;
                    }
                    else if (dummy <= 0)
                    {
                        _channels[3].volume = 0;
                    }
                    else
                    {
                        _channels[3].volume = (uint)dummy;
                        break;
                    }

                    if (--_repeat_ctr == 0)
                    {
                        ParsePCjrChunk();
                        return;
                    }
                    _delta = BitConverter.ToUInt16(_current_data, _next_chunk);
                    _next_chunk += 2;
                    break;
            }
        }

        class channel_data_v1
        {
            public int freq;
            public uint volume;
            public int? cmd_ptr;
            public uint notelen;
            public uint hull_counter;
            public uint attack;
            public uint decay;
            public uint level;
            public uint sustain_1;
            public uint sustain_2;
            public int sustctr;
        }

        readonly channel_data_v1[] _channels = CreateChannels();

        static channel_data_v1[] CreateChannels()
        {
            var channels = new channel_data_v1[4];
            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = new channel_data_v1();
            }
            return channels;
        }
    }
}

