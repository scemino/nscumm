//
//  Player_V2.cs
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
    class Player_V2: Player_V2Base
    {
        public Player_V2(ScummEngine scumm, IMixer mixer, bool pcjr)
            : base(scumm, mixer, pcjr)
        {
            // Initialize square generator
            _level = 0;

            _RNG = NG_PRESET;

            if (_pcjr)
            {
                _decay = PCJR_DECAY;
                _update_step = (((uint)_sampleRate << FIXP_SHIFT) / (111860 * 2));
            }
            else
            {
                _decay = SPK_DECAY;
                _update_step = (((uint)_sampleRate << FIXP_SHIFT) / (1193000 * 2));
            }

            // Adapt _decay to sample rate.  It must be squared when
            // sample rate doubles.
            for (var i = 0; (_sampleRate << i) < 30000; i++)
                _decay = _decay * _decay / 65536;

            _timer_output = 0;
            for (var i = 0; i < 4; i++)
                _timer_count[i] = 0;

            SetMusicVolume(255);

            _soundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false, true);
        }

        public override void Dispose()
        {
            lock (_mutex)
            {
                _mixer.StopHandle(_soundHandle);
            }
        }

        public override void SetMusicVolume(int vol)
        {
            if (vol > 255)
                vol = 255;

            /* scale to int16, FIXME: find best value */
            double output = vol * 128.0 / 3;

            /* build volume table (2dB per step) */
            for (int i = 0; i < 15; i++)
            {
                /* limit volume to avoid clipping */
                if (output > 0xffff)
                    _volumetable[i] = 0xffff;
                else
                    _volumetable[i] = (uint)output;

                output /= 1.258925412;         /* = 10 ^ (2/20) = 2dB */
            }
            _volumetable[15] = 0;
        }

        public override void StopAllSounds()
        {
            lock (_mutex)
            {
                for (int i = 0; i < 4; i++)
                {
                    ClearChannel(i);
                }
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
                    _current_nr = 0;
                    _current_data = null;
                    ChainNextSound();
                }
            }
        }

        public override void StartSound(int nr)
        {
            lock (_mutex)
            {
                var data = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, nr);
                Debug.Assert(data != null);

                int cprio = _current_data != null ? _current_data[_header_len] : 0;
                int prio = data[_header_len];
                int nprio = _next_data != null ? _next_data[_header_len] : 0;

                int restartable = data[_header_len + 1];

                if (_current_nr == 0 || cprio <= prio)
                {
                    int tnr = _current_nr;
                    int tprio = cprio;
                    var tdata = _current_data;

                    ChainSound(nr, data);
                    nr = tnr;
                    prio = tprio;
                    data = tdata;
                    restartable = data != null ? data[_header_len + 1] : 0;
                }

                if (_current_nr == 0)
                {
                    nr = 0;
                    _next_nr = 0;
                    _next_data = null;
                }

                if (nr != _current_nr
                    && restartable != 0
                    && (_next_nr == 0
                    || nprio <= prio))
                {
                    _next_nr = nr;
                    _next_data = data;
                }
            }
        }

        public override int GetSoundStatus(int nr)
        {
            return _current_nr == nr || _next_nr == nr ? 1 : 0;
        }

        public override int ReadBuffer(short[] data)
        {
            lock (_mutex)
            {

                int step;
                int len = data.Length / 2;
                int offset = 0;

                do
                {
                    if (0 == (_next_tick >> FIXP_SHIFT))
                    {
                        _next_tick += _tick_len;
                        NextTick();
                    }

                    step = len;
                    if (step > (_next_tick >> FIXP_SHIFT))
                        step = (int)(_next_tick >> FIXP_SHIFT);
                    if (_pcjr)
                        GeneratePCjrSamples(data, offset, step);
                    else
                        GenerateSpkSamples(data, offset, step);
                    offset += 2 * step;
                    _next_tick -= (uint)(step << FIXP_SHIFT);
                } while ((len -= step) != 0);

                return data.Length;
            }
        }

        void GenerateSpkSamples(short[] data, int offset, int len)
        {
            int winning_channel = -1;
            for (int i = 0; i < 4; i++)
            {
                if (winning_channel == -1 && _channels[i].d.volume != 0 && _channels[i].d.time_left != 0)
                {
                    winning_channel = i;
                }
            }

            Array.Clear(data, offset, len * 2);
            if (winning_channel != -1)
            {
                SquareGenerator(0, _channels[winning_channel].d.freq, 0, 0, data, offset, len);
            }
            else if (_level == 0)
                /* shortcut: no sound is being played. */
                return;

            LowPassFilter(data, offset, len);
        }

        void GeneratePCjrSamples(short[] data, int offset, int len)
        {
            int i, j;
            int freq, vol;

            Array.Clear(data, offset, len);
            bool hasdata = false;

            for (i = 1; i < 3; i++)
            {
                freq = _channels[i].d.freq >> 6;
                if (_channels[i].d.volume != 0 && _channels[i].d.time_left != 0)
                {
                    for (j = 0; j < i; j++)
                    {
                        if (_channels[j].d.volume != 0
                            && _channels[j].d.time_left != 0
                            && freq == (_channels[j].d.freq >> 6))
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

            for (i = 0; i < 4; i++)
            {
                freq = _channels[i].d.freq >> 6;
                vol = (65535 - _channels[i].d.volume) >> 12;
                if (0 == _channels[i].d.volume || 0 == _channels[i].d.time_left)
                {
                    _timer_count[i] -= len << FIXP_SHIFT;
                    if (_timer_count[i] < 0)
                        _timer_count[i] = 0;
                }
                else if (i < 3)
                {
                    hasdata = true;
                    SquareGenerator(i, freq, vol, 0, data, offset, len);
                }
                else
                {
                    int noiseFB = (freq & 4) != 0 ? FB_WNOISE : FB_PNOISE;
                    int n = (freq & 3);

                    freq = (n == 3) ? 2 * (_channels[2].d.freq >> 6) : 1 << (5 + n);
                    hasdata = true;
                    SquareGenerator(i, freq, vol, noiseFB, data, offset, len);
                }
//                #if 0
//                debug(9, "channel[%d]: freq %d %.1f ; volume %d",
//                i, freq, 111860.0 / freq,  vol);
//                #endif
            }

            if (_level != 0 || hasdata)
                LowPassFilter(data, offset, len);
        }

        void SquareGenerator(int channel, int freq, int vol, int noiseFeedback, short[] sample, int offset, int len)
        {
            int period = (int)(_update_step * freq);
            int nsample;
            if (period == 0)
                period = (int)_update_step;

            for (var i = 0; i < len; i++)
            {
                uint duration = 0;

                if ((_timer_output & (1 << channel)) != 0)
                    duration += (uint)_timer_count[channel];

                _timer_count[channel] -= (1 << FIXP_SHIFT);
                while (_timer_count[channel] <= 0)
                {
                    if (noiseFeedback != 0)
                    {
                        if ((_RNG & 1) != 0)
                        {
                            _RNG ^= (uint)noiseFeedback;
                            _timer_output ^= (1 << channel);
                        }
                        _RNG >>= 1;
                    }
                    else
                    {
                        _timer_output ^= (1 << channel);
                    }

                    if ((_timer_output & (1 << channel)) != 0)
                        duration += (uint)period;

                    _timer_count[channel] += period;
                }

                if ((_timer_output & (1 << channel)) != 0)
                    duration -= (uint)_timer_count[channel];

                nsample = (int)(sample[offset] +
                (((duration - (1 << (FIXP_SHIFT - 1)))
                * (int)_volumetable[vol]) >> FIXP_SHIFT));
                /* overflow: clip value */
                if (nsample > 0x7fff)
                    nsample = 0x7fff;
                if (nsample < -0x8000)
                    nsample = -0x8000;
                sample[offset] = (short)nsample;
                // The following write isn't necessary, because the lowPassFilter does it for us
                //sample[1] = sample[0];
                offset += 2;
            }
        }

        void LowPassFilter(short[] sample, int offset, int len)
        {
            for (var i = 0; i < len * 2; i += 2)
            {
                _level = (int)(_level * _decay + sample[offset + i] * (0x10000 - _decay)) >> 16;
                sample[offset + i] = sample[offset + i + 1] = (short)_level;
            }
        }

        /// <summary>
        /// Depends on sample rate.
        /// </summary>
        const int SPK_DECAY = 0xa000;
        /// <summary>
        /// Depends on sample rate.
        /// </summary>
        const int PCJR_DECAY = 0xa000;

        /// <summary>
        /// noise generator preset.
        /// </summary>
        const int NG_PRESET = 0x0f35;
        /// <summary>
        /// feedback for white noise.
        /// </summary>
        const int FB_WNOISE = 0x12000;
        /// <summary>
        /// feedback for periodic noise.
        /// </summary>
        const int FB_PNOISE = 0x08000;

        uint _update_step;
        uint _decay;
        int _level;
        uint _RNG;
        uint[] _volumetable = new uint[16];

        int[] _timer_count = new int[4];
        int _timer_output;
    }
}

