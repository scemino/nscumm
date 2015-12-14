//
//  ResampleAudioSampleProvider.cs
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

namespace NScumm.Core.Audio.SampleProviders
{
    public class ResampleAudioSampleProvider : AudioSampleProvider16
    {
        private IRateConverter _converter;
        private AudioFormat _outFormat;
        private AudioSampleProviderToAudioStream _stream;

        public override AudioFormat AudioFormat
        {
            get { return _outFormat; }
        }

        public ResampleAudioSampleProvider(IAudioSampleProvider source, int newSampleRate)
        {
            _outFormat = new AudioFormat(newSampleRate, 2, 16);
            _converter = RateHelper.MakeRateConverter(source.AudioFormat.SampleRate, newSampleRate, source.AudioFormat.Channels == 2, false);
            _stream = new AudioSampleProviderToAudioStream(source);
        }

        public override int Read(short[] samples, int count)
        {
            return _converter.Flow(_stream, samples, count, Mixer.MaxMixerVolume, Mixer.MaxMixerVolume) * 2;
        }
    }
}
