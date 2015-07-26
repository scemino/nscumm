//
//  Paula.cs
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
    /// <summary>
    /// Emulation of the "Paula" Amiga music chip
    /// The interrupt frequency specifies the number of mixed wavesamples between
    /// calls of the interrupt method
    /// </summary>
    public abstract class Paula: IAudioStream
    {
        public int Rate { get; private set; }

        public bool IsStereo { get { return _stereo; } }

        public bool IsEndOfData { get { return _end; } }

        bool IAudioStream.IsEndOfStream { get { return IsEndOfData; } }

        public bool IsPlaying { get; private set; }

        public uint TimerBase { get; set; }

        public uint SingleInterrupt { get; set; }

        public uint SingleInterruptUnscaled
        {
            get
            { 
                return (uint)((((double)SingleInterrupt) * TimerBase) / Rate);
            }
            set
            {
                SingleInterrupt = (uint)(((double)value * Rate) / TimerBase);
            }
        }

        public uint InterruptFreq
        {
            get{ return _intFreq; }
            set
            {
                _intFreq = value;
                SingleInterrupt = 0;
            }
        }

        public uint InterruptFreqUnscaled
        {
            get
            { 
                return (uint)((((double)InterruptFreq) * TimerBase) / Rate);
            }
            set
            {
                InterruptFreq = (uint)(((double)value * Rate) / TimerBase);
            }
        }

        protected Paula(bool stereo = false, int rate = 44100, uint interruptFreq = 0)
        {
            _stereo = stereo;
            Rate = rate;
            _periodScale = ((double)PalPaulaClock / rate);
            _intFreq = interruptFreq;

            ClearVoices();
            _voice[0].panning = 191;
            _voice[1].panning = 63;
            _voice[2].panning = 63;
            _voice[3].panning = 191;

            if (_intFreq == 0)
                _intFreq = (uint)Rate;

            SingleInterrupt = 0;
            TimerBase = 1;
            IsPlaying = false;
            _end = true;
        }

        ~Paula()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public int ReadBuffer(short[] buffer, int count)
        {
            lock (_mutex)
            {
                Array.Clear(buffer, 0, count);
                if (!IsPlaying)
                {
                    return count;
                }

                if (_stereo)
                    return ReadBufferIntern(true, buffer, count);
                else
                    return ReadBufferIntern(false, buffer, count);
            }
        }

        protected abstract void Interrupt();

        public void StartPlay()
        {
            IsPlaying = true;
        }

        public void StopPlay()
        {
            IsPlaying = false;
        }

        public void PausePlay(bool pause)
        {
            IsPlaying = !pause;
        }

        protected void StartPaula()
        {
            IsPlaying = true;
            _end = false;
        }

        protected void StopPaula()
        {
            IsPlaying = false;
            _end = true;
        }

        protected void DisableChannel(int channel)
        {
            Debug.Assert(channel < NUM_VOICES);
            _voice[channel].data = null;
        }

        protected void EnableChannel(int channel)
        {
            Debug.Assert(channel < NUM_VOICES);
            var ch = _voice[channel];
            ch.data = ch.dataRepeat;
            ch.length = ch.lengthRepeat;
            // actually first 2 bytes are dropped?
            ch.offset = new Offset(0);
            // ch.period = ch.periodRepeat;
        }

        protected void SetChannelPeriod(int channel, short period)
        {
            Debug.Assert(channel < NUM_VOICES);
            _voice[channel].period = period;
        }

        protected void SetChannelVolume(int channel, byte volume)
        {
            Debug.Assert(channel < NUM_VOICES);
            _voice[channel].volume = volume;
        }

        protected void SetChannelSampleStart(int channel, byte[] data)
        {
            Debug.Assert(channel < NUM_VOICES);
            _voice[channel].dataRepeat = data;
        }

        protected void SetChannelSampleLen(int channel, int length)
        {
            Debug.Assert(channel < NUM_VOICES);
            Debug.Assert(length < 32768 / 2);
            _voice[channel].lengthRepeat = 2 * length;
        }

        protected int GetChannelDmaCount(int channel)
        {
            Debug.Assert(channel < NUM_VOICES);
            return _voice[channel].dmaCount;
        }

        protected void SetChannelDmaCount(int channel, int dmaVal = 0)
        {
            Debug.Assert(channel < NUM_VOICES);
            _voice[channel].dmaCount = dmaVal;
        }

        protected void SetAudioFilter(bool enable)
        {
            // TODO: implement
        }

        int ReadBufferIntern(bool stereo, short[] buffer, int count)
        {
            var bufOffset = 0;
            var numSamples = count;
            int samples = _stereo ? numSamples / 2 : numSamples;
            while (samples > 0)
            {
                // Handle 'interrupts'. This gives subclasses the chance to adjust the channel data
                // (e.g. insert new samples, do pitch bending, whatever).
                if (SingleInterrupt == 0)
                {
                    SingleInterrupt = _intFreq;
                    Interrupt();
                }

                // Compute how many samples to generate: at most the requested number of samples,
                // of course, but we may stop earlier when an 'interrupt' is expected.
                uint nSamples = Math.Min((uint)samples, SingleInterrupt);

                // Loop over the four channels of the emulated Paula chip
                for (int voice = 0; voice < NUM_VOICES; voice++)
                {
                    // No data, or paused -> skip channel
                    if (_voice[voice].data == null || (_voice[voice].period <= 0))
                        continue;

                    // The Paula chip apparently run at 7.0937892 MHz in the PAL
                    // version and at 7.1590905 MHz in the NTSC version. We divide this
                    // by the requested the requested output sampling rate _rate
                    // (typically 44.1 kHz or 22.05 kHz) obtaining the value _periodScale.
                    // This is then divided by the "period" of the channel we are
                    // processing, to obtain the correct output 'rate'.
                    var rate = FixedPointFractionHelper.DoubleToFrac(_periodScale / _voice[voice].period);
                    // Cap the volume
                    _voice[voice].volume = Math.Min((byte)0x40, _voice[voice].volume);


                    var ch = _voice[voice];
                    var p = buffer;
                    var pOff = bufOffset;
                    int neededSamples = (int)nSamples;

                    // NOTE: A Protracker (or other module format) player might actually
                    // push the offset past the sample length in its interrupt(), in which
                    // case the first mixBuffer() call should not mix anything, and the loop
                    // should be triggered.
                    // Thus, doing an assert(ch.offset.int_off < ch.length) here is wrong.
                    // An example where this happens is a certain Protracker module played
                    // by the OS/2 version of Hopkins FBI.

                    // Mix the generated samples into the output buffer
                    neededSamples -= MixBuffer(stereo, p, ref pOff, ch.data, ch.offset, rate, neededSamples, ch.length, ch.volume, ch.panning);

                    // Wrap around if necessary
                    if (ch.offset.int_off >= ch.length)
                    {
                        // Important: Wrap around the offset *before* updating the voice length.
                        // Otherwise, if length != lengthRepeat we would wrap incorrectly.
                        // Note: If offset >= 2*len ever occurs, the following would be wrong;
                        // instead of subtracting, we then should compute the modulus using "%=".
                        // Since that requires a division and is slow, and shouldn't be necessary
                        // in practice anyway, we only use subtraction.
                        ch.offset.int_off -= ch.length;
                        ch.dmaCount++;

                        ch.data = ch.dataRepeat;
                        ch.length = ch.lengthRepeat;
                    }

                    // If we have not yet generated enough samples, and looping is active: loop!
                    if (neededSamples > 0 && ch.length > 2)
                    {
                        // Repeat as long as necessary.
                        while (neededSamples > 0)
                        {
                            // Mix the generated samples into the output buffer
                            neededSamples -= MixBuffer(stereo, p, ref pOff, ch.data, ch.offset, rate, neededSamples, ch.length, ch.volume, ch.panning);

                            if (ch.offset.int_off >= ch.length)
                            {
                                // Wrap around. See also the note above.
                                ch.offset.int_off -= ch.length;
                                ch.dmaCount++;
                            }
                        }
                    }

                }
                bufOffset += _stereo ? (int)nSamples * 2 : (int)nSamples;
                SingleInterrupt -= nSamples;
                samples -= (int)nSamples;
            }
            return numSamples;
        }

        int MixBuffer(bool stereo, short[] buf, ref int bufOffset, byte[] data, Offset offset, int rate, int neededSamples, int bufSize, byte volume, byte panning)
        {
            int samples;
            for (samples = 0; samples < neededSamples && offset.int_off < bufSize; ++samples)
            {
                var d = (sbyte)data[offset.int_off];
                var tmp = d * volume;
                if (stereo)
                {
                    buf[bufOffset++] += (short)((tmp * (255 - panning)) >> 7);
                    buf[bufOffset++] += (short)((tmp * panning) >> 7);
                }
                else
                    buf[bufOffset++] += (short)tmp;

                // Step to next source sample
                offset.rem_off += rate;
                if (offset.rem_off >= FixedPointFractionHelper.FRAC_ONE)
                {
                    offset.int_off += FixedPointFractionHelper.FracToInt(offset.rem_off);
                    offset.rem_off = (int)(offset.rem_off & FixedPointFractionHelper.FRAC_LO_MASK);
                }
            }

            return samples;
        }

        void ClearVoices()
        { 
            for (int i = 0; i < NUM_VOICES; ++i)
            {
                ClearVoice(i);
            }
        }

        void ClearVoice(int voice)
        {
            Debug.Assert(voice < NUM_VOICES);
            _voice[voice].data = null;
            _voice[voice].dataRepeat = null;
            _voice[voice].length = 0;
            _voice[voice].lengthRepeat = 0;
            _voice[voice].period = 0;
            _voice[voice].volume = 0;
            _voice[voice].offset = new Offset(0);
            _voice[voice].dmaCount = 0;
        }

        void SetChannelPanning(int channel, byte panning)
        {
            Debug.Assert(channel < NUM_VOICES);
            _voice[channel].panning = panning;
        }

        void SetChannelData(int channel, byte[] data, byte[] dataRepeat, int length, int lengthRepeat, int offset = 0)
        {
            Debug.Assert(channel < NUM_VOICES);

            Channel ch = _voice[channel];

            ch.dataRepeat = data;
            ch.lengthRepeat = length;
            EnableChannel(channel);
            ch.offset = new Offset(offset);

            ch.dataRepeat = dataRepeat;
            ch.lengthRepeat = lengthRepeat;
        }

        void SetChannelOffset(int channel, Offset offset)
        {
            Debug.Assert(channel < NUM_VOICES);
            _voice[channel].offset = offset;
        }

        Offset GetChannelOffset(int channel)
        {
            Debug.Assert(channel < NUM_VOICES);
            return _voice[channel].offset;
        }

        public const int NUM_VOICES = 4;

        public const int PalSystemClock = 7093790;
        public const int NtscSystemClock = 7159090;
        public const int PalCiaClock = PalSystemClock / 10;
        public const int NtscCiaClock = NtscSystemClock / 10;
        public const int PalPaulaClock = PalSystemClock / 2;
        public const int NtscPauleClock = NtscSystemClock / 2;

        bool _end;
        protected readonly object _mutex = new object();
        Channel[] _voice = CreateVoices();

        static Channel[] CreateVoices()
        {
            var voices = new Channel[NUM_VOICES];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = new Channel();
            }
            return voices;
        }

        readonly bool _stereo;
        readonly double _periodScale;
        uint _intFreq;

        class Channel
        {
            public byte[] data;
            public byte[] dataRepeat;
            public int length;
            public int lengthRepeat;
            public short period;
            public byte volume;
            public Offset offset;
            public byte panning;
            // For stereo mixing: 0 = far left, 255 = far right
            public int dmaCount;
        }

        /* TODO: Document this */
        class Offset
        {
            public int int_off;
            // integral part of the offset
            public int rem_off;
            // fractional part of the offset, at least 0 and less than 1

            public Offset(int off = 0)
            {
                int_off = off;
                rem_off = 0;
            }
        }
    }
}

