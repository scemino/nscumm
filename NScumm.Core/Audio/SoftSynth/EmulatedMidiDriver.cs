//
//  EmulatedMidiDriver.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

namespace NScumm.Core.Audio.SoftSynth
{
    abstract class EmulatedMidiDriver: MidiDriver, IAudioStream
    {
        public bool IsOpen { get { return _isOpen; } }

        public override uint BaseTempo { get { return 1000000 / (uint)_baseFreq; } }

        protected EmulatedMidiDriver(IMixer mixer)
        {
            _mixer = mixer;
            _baseFreq = 250;
        }

        public override MidiDriverError Open()
        {
            _isOpen = true;

            var d = Rate / _baseFreq;
            var r = Rate % _baseFreq;

            // This is equivalent to (getRate() << FIXP_SHIFT) / BASE_FREQ
            // but less prone to arithmetic overflow.
            _samplesPerTick = (d << FixpShift) + (r << FixpShift) / _baseFreq;

            return MidiDriverError.None;
        }

        public override void SetTimerCallback(object timerParam, TimerProc timerProc)
        {
            _timerProc = timerProc;
            _timerParam = timerParam;
        }

        protected abstract void GenerateSamples(short[] buf, int pos, int len);

        protected virtual void OnTimer()
        {
        }

        #region IMixerAudioStream implementation

        public int ReadBuffer(short[] data)
        {
            int stereoFactor = IsStereo ? 2 : 1;
            int len = data.Length / stereoFactor;
            int step;
            int pos = 0;

            do
            {
                step = len;
                if (step > (_nextTick >> FixpShift))
                    step = (_nextTick >> FixpShift);

                GenerateSamples(data, pos, step);

                _nextTick -= step << FixpShift;
                if (0 == (_nextTick >> FixpShift))
                {
                    if (_timerProc != null)
                        _timerProc(_timerParam);

                    OnTimer();

                    _nextTick += _samplesPerTick;
                }

                pos += step * stereoFactor;
                len -= step;
            } while (len != 0);

            return data.Length;
        }

        public abstract bool IsStereo
        {
            get;
        }

        public abstract int Rate
        {
            get;
        }

        public bool IsEndOfData
        {
            get{ return false; }
        }

        public bool IsEndOfStream
        {
            get{ return false; }
        }

        public virtual void Dispose()
        {
        }

        #endregion

        const int FixpShift = 16;

        protected bool _isOpen;
        protected IMixer _mixer;
        protected SoundHandle _mixerSoundHandle;
        protected int _baseFreq;

        object _timerParam;
        TimerProc _timerProc;

        int _nextTick;
        int _samplesPerTick;
    }
}

