//
//  AudioSampleProviderToAudioStream.cs
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

namespace NScumm.Core.Audio.SampleProviders
{
    public class AudioSampleProviderToAudioStream : IAudioStream
    {
        private readonly AudioFormat _format;
        private IAudioSampleProvider _audioSampleProvider;
        private byte[] _buffer;

        public AudioFormat AudioFormat
        {
            get { return _format; }
        }

        public bool IsStereo
        {
            get
            {
                return _format.Channels == 2;
            }
        }

        public int Rate
        {
            get
            {
                return _format.SampleRate;
            }
        }

        public bool IsEndOfData
        {
            get
            {
                return false;
            }
        }

        public bool IsEndOfStream
        {
            get
            {
                return false;
            }
        }

        public AudioSampleProviderToAudioStream(IAudioSampleProvider audioSampleProvider)
        {
            _audioSampleProvider = audioSampleProvider;
            _format = _audioSampleProvider.AudioFormat;
            _buffer = new byte[4096];
        }

        public int ReadBuffer(Ptr<short> samples, int numSamples)
        {
            if (_buffer.Length < numSamples)
            {
                _buffer = new byte[numSamples * 2];
            }
            var numBytes = _audioSampleProvider.Read(_buffer, numSamples * 2);
            var numSamplesRead = numBytes / 2;
            for (int i = 0; i < numSamplesRead; i++)
            {
                samples[i] = _buffer.ToInt16(i * 2);
            }
            return numSamplesRead;
        }

        public void Dispose()
        {
        }
    }
}
