//
//  Mp3AudioSampleProvider.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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

using NScumm.Core.Audio;
using System.IO;
using NScumm.Core.Audio.SampleProviders;

namespace NScumm
{
    // Doesn't work: System.ArgumentOutOfRangeException: Channels must be 1 or greater
    // Parameter name: channels
    //class Mp3AudioSampleProvider : IAudioSampleProvider
    //{
    //    readonly ToyMp3.Mp3Stream _stream;

    //    public Mp3AudioSampleProvider(Stream stream)
    //    {
    //        _stream = new ToyMp3.Mp3Stream(stream);
    //        AudioFormat = new AudioFormat(_stream.Samplerate, _stream.Channels);
    //    }

    //    public AudioFormat AudioFormat
    //    {
    //        get;
    //    }

    //    public int Read(byte[] samples, int count)
    //    {
    //        return _stream.Read(samples, count);
    //    }
    //}
}
