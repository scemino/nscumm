//
//  SampleConverter.cs
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

using System.Diagnostics;

namespace NScumm.Scumm.Audio
{
    class SampleConverter
    {
        public SampleConverter()
        {
            _volume = MaxVolume;
            _buffer = new SampleBuffer();
        }

        public void Reset()
        {
            _missingCyclesFP = 0;
            _sampleCyclesSumFP = 0;
            _buffer.Clear();
        }

        public int AvailableSize
        {
            get
            {
                return _buffer.AvailableSize;
            }
        }

        public void SetMusicVolume(int vol)
        {
            Debug.Assert(vol >= 0 && vol <= MaxVolume);
            _volume = vol;
        }

        public void SetSampleRate(int rate)
        {
            /* ~46 CPU cycles per sample @ 22.05kHz */
            _cyclesPerSampleFP = (int)(AppleIICpuClock * (1 << PrecShift) / rate);
            Reset();
        }

        public void AddCycles(byte level, int cycles)
        {
            /* convert to fixed precision floats */
            int cyclesFP = cycles << PrecShift;

            // step 1: if cycles are left from the last call, process them first
            if (_missingCyclesFP > 0)
            {
                int n = (_missingCyclesFP < cyclesFP) ? _missingCyclesFP : cyclesFP;
                if (level != 0)
                    _sampleCyclesSumFP += n;
                cyclesFP -= n;
                _missingCyclesFP -= n;
                if (_missingCyclesFP == 0)
                {
                    AddSampleToBuffer(2 * 32767 * _sampleCyclesSumFP / _cyclesPerSampleFP - 32767);
                }
                else
                {
                    return;
                }
            }

            _sampleCyclesSumFP = 0;

            // step 2: process blocks of cycles fitting into a whole sample
            while (cyclesFP >= _cyclesPerSampleFP)
            {
                AddSampleToBuffer(level != 0 ? 32767 : -32767);
                cyclesFP -= _cyclesPerSampleFP;
            }

            // step 3: remember cycles left for next call
            if (cyclesFP > 0)
            {
                _missingCyclesFP = _cyclesPerSampleFP - cyclesFP;
                if (level != 0)
                    _sampleCyclesSumFP = cyclesFP;
            }
        }

        public int ReadSamples(short[] buffer, int offset, int numSamples)
        {
            return _buffer.Read(buffer, offset, numSamples);
        }

        void AddSampleToBuffer(int sample)
        {
            short value = (short)(sample * _volume / MaxVolume);
            _buffer.Write(value);
        }

        const int PrecShift = 7;
        const int MaxVolume = 256;
        // CPU_CLOCK according to AppleWin
        const double AppleIICpuClock = 1020484.5;
        // ~ 1.02 MHz

        int _cyclesPerSampleFP;
        /* (fixed precision) */
        int _missingCyclesFP;
        /* (fixed precision) */
        int _sampleCyclesSumFP;
        /* (fixed precision) */
        int _volume;
        /* 0 - 256 */
        SampleBuffer _buffer;
    }
    
}
