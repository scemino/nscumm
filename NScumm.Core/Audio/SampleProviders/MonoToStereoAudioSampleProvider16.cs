//
//  MonoToStereoAudioSampleProvider16.cs
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
    public class MonoToStereoAudioSampleProvider16 : IAudioSampleProvider
    {
        private AudioFormat _audioFormat;
        private IAudioSampleProvider _audioSampleProvider;
        private byte[] _buffer;

        public MonoToStereoAudioSampleProvider16(IAudioSampleProvider audioSampleProvider)
        {
            if (audioSampleProvider.AudioFormat.Channels != 1)
            {
                throw new ArgumentException("audioSampleProvider expected to be Mono");
            }
            if (audioSampleProvider.AudioFormat.BitsPerSample != 16)
            {
                throw new ArgumentException("audioSampleProvider expected to be 16 bit");
            }

            _audioSampleProvider = audioSampleProvider;
            _audioFormat = new AudioFormat(_audioSampleProvider.AudioFormat.SampleRate, 2, _audioSampleProvider.AudioFormat.BitsPerSample);
            _buffer = new byte[4096];
        }

        public AudioFormat AudioFormat
        {
            get { return _audioFormat; }
        }

        public int Read(byte[] samples, int count)
        {
            var numSamples = count / 2;
            if (_buffer.Length < numSamples)
            {
                _buffer = new byte[numSamples];
            }
            var numBytes = _audioSampleProvider.Read(_buffer, numSamples);
            int offs = 0;
            for (int i = 0; i < numBytes; i += 2)
            {
                var value = _buffer.ToInt16(i);
                samples.WriteInt16(offs += 2, value);
                samples.WriteInt16(offs += 2, value);
            }
            return numBytes * 2;
        }
    }
}
