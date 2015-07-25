//
//  AudioStreamToAudioSampleProvider.cs
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
    /// <summary>
    /// Converts an IAudioStream to an IAudioSampleProvider.
    /// </summary>
    /// <description>>An IAudioStream is always a stream of 16 bits samples,
    /// but the rate and the number of channels can be variable.
    /// </description>
    public class AudioStreamToAudioSampleProvider: AudioSampleProvider16
    {
        readonly IAudioStream _stream;
        readonly AudioFormat _format;

        public override AudioFormat AudioFormat
        {
            get{ return _format; }
        }

        public AudioStreamToAudioSampleProvider(IAudioStream stream)
        {
            _stream = stream;
            _format = new AudioFormat(stream.Rate, stream.IsStereo ? 2 : 1);
        }

        public override int Read(short[] samples, int count)
        {
            return _stream.ReadBuffer(samples, count) / 2;
        }
    }    
}
