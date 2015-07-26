//
//  Player_AppleII.cs
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
using NScumm.Core.IO;

namespace NScumm.Core.Audio
{
    class Player_AppleII: IMusicEngine, IAudioStream
    {
        public int Rate { get { return _sampleRate; } }

        public bool IsStereo { get { return false; } }

        public bool IsEndOfData { get { return false; } }

        bool IAudioStream.IsEndOfStream { get { return IsEndOfData; } }

        public Player_AppleII(ScummEngine scumm, IMixer mixer)
        {
            _mixer = mixer;
            _vm = scumm;
            _sampleConverter = new SampleConverter();
            ResetState();
            SetSampleRate(_mixer.OutputRate);
            _soundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false, true);
        }

        public void Dispose()
        {
            _mixer.StopHandle(_soundHandle);
        }

        public void SetMusicVolume(int vol)
        { 
            _sampleConverter.SetMusicVolume(vol);
        }

        public void StartSound(int nr)
        {
            lock (_mutex)
            {
                var data = _vm.ResourceManager.GetSound(_vm.Sound.MusicType, nr);
                Debug.Assert(data != null);
                const int ptr1 = 4;

                ResetState();
                _soundNr = nr;
                _type = data[ptr1];
                _loop = (data.Length > 5) ? data[ptr1 + 1] : 0;
                _params = (data.Length > 6) ? new byte[data.Length - ptr1 - 2] : new byte[0];
                if (_params.Length > 0)
                {
                    Array.Copy(data, ptr1 + 2, _params, 0, _params.Length);
                }

                switch (_type)
                {
                    case 0: // empty (nothing to play)
                        ResetState();
                        return;
                    case 1:
                        _soundFunc = new AppleII_SoundFunction1_FreqUpDown();
                        break;
                    case 2:
                        _soundFunc = new AppleII_SoundFunction2_SymmetricWave();
                        break;
                    case 3:
                        _soundFunc = new AppleII_SoundFunction3_AsymmetricWave();
                        break;
                    case 4:
                        _soundFunc = new AppleII_SoundFunction4_Polyphone();
                        break;
                    case 5:
                        _soundFunc = new AppleII_SoundFunction5_Noise();
                        break;
                }
                _soundFunc.Init(this, _params);

                Debug.Assert(_loop > 0);

                Debug.WriteLine("startSound {0}: type {1}, loop {2}", nr, _type, _loop);
            }
        }

        public void StopAllSounds()
        {
            lock (_mutex)
            {
                ResetState();
            }
        }

        public void StopSound(int nr)
        {
            lock (_mutex)
            {
                if (_soundNr == nr)
                {
                    ResetState();
                }
            }
        }

        public int GetSoundStatus(int nr)
        {
            lock (_mutex)
            {
                return (_soundNr == nr) ? 1 : 0;
            }
        }

        public int GetMusicTimer()
        {
            /* Apple-II sounds are synchronous -> no music timer */
            return 0;
        }

        public int ReadBuffer(short[] buffer, int count)
        {
            var numSamples = count;
            lock (_mutex)
            {
                if (_soundNr == 0)
                    return 0;

                var samplesLeft = numSamples;
                var offset = 0;
                do
                {
                    int nSamplesRead = _sampleConverter.ReadSamples(buffer, offset, samplesLeft);
                    samplesLeft -= nSamplesRead;
                    offset += nSamplesRead;
                } while ((samplesLeft > 0) && UpdateSound());

                // reset state if sound is played completely
                if (_soundFunc == null && (_sampleConverter.AvailableSize == 0))
                    ResetState();

                return numSamples - samplesLeft;
            }
        }

        // Apple-II sound-resource helpers
        // toggle speaker on/off
        public void SpeakerToggle()
        {
            _speakerState ^= 0x1;
        }

        public void GenerateSamples(int cycles)
        {
            _sampleConverter.AddCycles(_speakerState, cycles);
        }

        public void Wait(int interval, int count /*y*/)
        {
            Debug.Assert(count > 0); // 0 == 256?
            Debug.Assert(interval > 0); // 0 == 256?
            GenerateSamples(11 + count * (8 + 5 * interval));
        }

        void IMusicEngine.SaveOrLoad(Serializer serializer)
        {
        }

        bool UpdateSound()
        {
            if (_soundFunc == null)
                return false;

            if (_soundFunc.Update())
            {
                --_loop;
                if (_loop <= 0)
                {
                    _soundFunc = null;
                }
                else
                {
                    // reset function state on each loop
                    _soundFunc.Init(this, _params);
                }
            }

            return true;
        }

        void SetSampleRate(int rate)
        {
            _sampleRate = rate;
            _sampleConverter.SetSampleRate(rate);
        }

        void ResetState()
        {
            _soundNr = 0;
            _type = 0;
            _loop = 0;
            _params = null;
            _speakerState = 0;
            _soundFunc = null;
            _sampleConverter.Reset();
        }

        // sound number
        int _soundNr;
        // type of sound
        int _type;
        // number of loops left
        int _loop;
        // global sound param list
        byte[] _params;
        // speaker toggle state (0 / 1)
        byte _speakerState;
        // sound function
        IAppleII_SoundFunction _soundFunc;
        // cycle to sample converter
        readonly SampleConverter _sampleConverter;

        ScummEngine _vm;
        IMixer _mixer;
        SoundHandle _soundHandle;
        int _sampleRate;
        object _mutex = new object();
    }
}

