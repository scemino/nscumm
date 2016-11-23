//
//  LoopingAudioStream.cs
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

using NScumm.Core;
using NScumm.Core.Audio;

namespace NScumm.Agos
{
    internal class LoopingAudioStream : IAudioStream
    {
        private BaseSound _parent;
        private IAudioStream _stream;
        private bool _loop;
        private uint _sound;
        private uint _loopSound;

        public LoopingAudioStream(BaseSound parent, uint sound, uint loopSound, bool loop)
        {
            _parent = parent;
            _sound = sound;
            _loop = loop;
            _loopSound = loopSound;

            _stream = _parent.MakeAudioStream(sound);
        }

        public int ReadBuffer(Ptr<short> buffer, int numSamples)
        {
            if (!_loop)
            {
                return _stream.ReadBuffer(buffer, numSamples);
            }

            var buf = new Ptr<short>(buffer);
            int samplesLeft = numSamples;

            while (samplesLeft > 0)
            {
                int len = _stream.ReadBuffer(buf, samplesLeft);
                if (len < samplesLeft)
                {
                    _stream.Dispose();
                    _stream = _parent.MakeAudioStream(_loopSound);
                }
                samplesLeft -= len;
                buf.Offset += len;
            }

            return numSamples;
        }

        public bool IsStereo => _stream?.IsStereo ?? false;

        public bool IsEndOfStream => IsEndOfData;

        public bool IsEndOfData
        {
            get
            {
                if (_stream==null)
                    return true;
                if (_loop)
                    return false;
                return _stream.IsEndOfData;
            }
        }

        public int Rate => _stream?.Rate ?? 22050;

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}