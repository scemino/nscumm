/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;

namespace NScumm.Core.Audio.OPL
{
    public enum OplType
    {
        Opl2,
        DualOpl2,
        Opl3
    }

    public static class Opl
    {
        /// <summary>
        /// The default callback frequency that start() uses.
        /// </summary>
        public const int DefaultCallbackFrequency = 250;
    }

    public interface IOpl
    {
        void Init();

        void Write(int a, int v);
        void WriteReg(int r, int v);

        void ReadBuffer(short[] buffer, int pos, int length);

        /// <summary>
        /// Start the OPL with callbacks.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="timerFrequency"></param>
        void Start(Action callback, int timerFrequency = Opl.DefaultCallbackFrequency);

        bool IsStereo { get; }
    }

    public abstract class EmulatedOPL : IOpl, IAudioStream
    {
        private const int FIXP_SHIFT = 16;

        private Action _callback;
        private int _baseFreq;

        private int _nextTick;
        private int _samplesPerTick;

        private SoundHandle _handle;

        public abstract void Init();
        public abstract void Write(int a, int v);
        public abstract void WriteReg(int r, int v);
        public abstract void ReadBuffer(short[] buffer, int pos, int length);

        public void Start(Action callback, int timerFrequency = Opl.DefaultCallbackFrequency)
        {
            _callback = callback;
            StartCallbacks(timerFrequency);
        }

        public int ReadBuffer(Ptr<short> buffer, int numSamples)
        {
            int stereoFactor = IsStereo ? 2 : 1;
            int len = numSamples / stereoFactor;

            do
            {
                var step = len;
                if (step > (_nextTick >> FIXP_SHIFT))
                    step = (_nextTick >> FIXP_SHIFT);

                GenerateSamples(buffer, step * stereoFactor);

                _nextTick -= step << FIXP_SHIFT;
                if (0 == (_nextTick >> FIXP_SHIFT))
                {
                    _callback?.Invoke();

                    _nextTick += _samplesPerTick;
                }

                buffer.Offset += step * stereoFactor;
                len -= step;
            } while (len != 0);

            return numSamples;
        }

        protected abstract void GenerateSamples(Ptr<short> buffer, int numSamples);

        public abstract bool IsStereo { get; }
        public int Rate => Engine.Instance.Mixer.OutputRate;
        public bool IsEndOfData => false;
        public bool IsEndOfStream => false;

        private void StartCallbacks(int timerFrequency)
        {
            SetCallbackFrequency(timerFrequency);
            _handle = Engine.Instance.Mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false, true);
        }

        private void SetCallbackFrequency(int timerFrequency)
        {
            _baseFreq = timerFrequency;
            System.Diagnostics.Debug.Assert(_baseFreq != 0);

            int d = Rate / _baseFreq;
            int r = Rate % _baseFreq;

            // This is equivalent to (getRate() << FIXP_SHIFT) / BASE_FREQ
            // but less prone to arithmetic overflow.

            _samplesPerTick = (d << FIXP_SHIFT) + (r << FIXP_SHIFT) / _baseFreq;
        }

        public abstract void Dispose();
    }
}