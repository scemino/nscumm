//
//  VocSound.cs
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
    internal class VocSound: BaseSound
    {
        private readonly bool _isUnsigned;

        public VocSound(IMixer mixer, string filename, bool isUnsigned, uint @base= 0, bool bigEndian=false)
            : base(mixer, filename, @base, bigEndian)
        {
            _isUnsigned = isUnsigned;
        }

        public override IAudioStream MakeAudioStream(uint sound)
        {
            var tmp = GetSoundStream(sound);
            if (tmp==null)
                return null;
            return new VocStream(tmp, _isUnsigned);
        }
    }
}