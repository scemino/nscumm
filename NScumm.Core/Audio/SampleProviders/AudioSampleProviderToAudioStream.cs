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
        readonly AudioFormat _format;
        private IAudioSampleProvider _audioSampleProvider;

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
        }

        public int ReadBuffer(short[] samples, int numSamples)
        {
            var buffer = new Buffer(numSamples * 2);
            var numBytes = _audioSampleProvider.Read(buffer.Bytes, numSamples * 2);
            Array.Copy(buffer.Shorts, samples, numBytes);
            return numBytes / 2;
        }

        public void Dispose()
        {
        }
    }
}
