//
//  WavSound.cs
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
using NScumm.Core.Audio.Decoders;

namespace NScumm.Agos
{
    internal class WavSound : BaseSound
    {
        public WavSound(IMixer mixer, string filename, uint @base = 0)
            : base(mixer, filename, @base, false)
        {
        }

        public WavSound(IMixer mixer, string filename, uint[] offsets)
            : base(mixer, filename, offsets)
        {
        }

        public override IAudioStream MakeAudioStream(uint sound)
        {
            var tmp = GetSoundStream(sound);
            if (tmp == null) return null;
            return Wave.MakeWAVStream(tmp, true);
        }
    }
}