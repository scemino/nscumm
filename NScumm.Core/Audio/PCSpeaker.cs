//
//  PCSpeaker.cs
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
using System.Collections.Generic;

namespace NScumm.Core.Audio
{
    public enum WaveForm
    {
        Square,
        Sine,
        Saw,
        Triangle
    }

    public class PCSpeaker: IAudioStream
    {
        public bool IsStereo
        {
            get{ return false; }
        }

        public int Rate
        {
            get;
            private set;
        }

        public bool IsEndOfData
        {
            get{ return false; }
        }

        public bool IsEndOfStream
        {
            get{ return false; }
        }

        ~PCSpeaker()
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

        public int Volume { get; set; }

        public bool IsPlaying { get { return _remainingSamples != 0; } }

        public PCSpeaker(int rate = 44100)
        {
            Rate = rate;
            _wave = WaveForm.Square;
            Volume = 255;
            generateWave = new Dictionary<WaveForm,Func<int,int,int>>()
            {
                { WaveForm.Square, GenerateSquare },
                { WaveForm.Sine, GenerateSine },
                { WaveForm.Saw, GenerateSaw },
                { WaveForm.Triangle, GenerateTriangle }
            };
        }

        /// <summary>
        /// Play a note for length ms.
        /// </summary>
        /// <param name="wave">Wave form to play.</param>
        /// <param name="freq">Frequency.</param>
        /// <param name="length">Length in ms.</param>
        /// <remarks>If length is negative, play until told to stop.</remarks>
        public void Play(WaveForm wave, int freq, int length)
        {
            lock (_mutex)
            {
                _wave = wave;
                _oscLength = Rate / freq;
                _oscSamples = 0;
                if (length == -1)
                {
                    _remainingSamples = 1;
                    _playForever = true;
                }
                else
                {
                    _remainingSamples = (Rate * length) / 1000;
                    _playForever = false;
                }
                _mixedSamples = 0;
            }
        }

        /// <summary>
        /// Stop the currently playing note after delay ms.
        /// </summary>
        /// <param name="delay">Delay.</param>
        public void Stop(int delay = 0)
        {
            lock (_mutex)
            {
                _remainingSamples = (Rate * delay) / 1000;
                _playForever = false;
            }
        }

        public int ReadBuffer(short[] buffer, int count)
        {
            lock (_mutex)
            {
                int i;
                for (i = 0; _remainingSamples != 0 && (i < count); i++)
                {
                    buffer[i] = (short)(generateWave[_wave](_oscSamples, _oscLength) * Volume);
                    if (_oscSamples++ >= _oscLength)
                        _oscSamples = 0;
                    if (!_playForever)
                        _remainingSamples--;
                    _mixedSamples++;
                }

                // Clear the rest of the buffer
                if (i < count)
                {
                    Array.Clear(buffer, i, (count - i));
                }

                return count;
            }
        }

        static int GenerateSquare(int x, int oscLength)
        {
            return (x < (oscLength / 2)) ? 127 : -128;
        }

        static int GenerateSine(int x, int oscLength)
        {
            if (oscLength == 0)
                return 0;

            // TODO: Maybe using a look-up-table would be better?
            return ScummHelper.Clip((int)(128 * Math.Sin(2.0 * Math.PI * x / oscLength)), -128, 127);
        }

        static int GenerateSaw(int x, int oscLength)
        {
            if (oscLength == 0)
                return 0;

            return ((x * (65536 / oscLength)) >> 8) - 128;
        }

        static int GenerateTriangle(int x, int oscLength)
        {
            if (oscLength == 0)
                return 0;

            int y = ((x * (65536 / (oscLength / 2))) >> 8) - 128;

            return (x <= (oscLength / 2)) ? y : (256 - y);
        }

        int _oscLength;
        int _oscSamples;
        int _remainingSamples;
        int _mixedSamples;
        bool _playForever;
        WaveForm _wave;
        object _mutex = new object();
        Dictionary<WaveForm, Func<int, int, int>> generateWave;
    }
}

