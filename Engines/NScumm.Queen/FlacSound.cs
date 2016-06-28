//
//  FlacSound.cs
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

using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;

namespace NScumm.Queen
{

    class FlacSound : PCSound
    {
        public FlacSound(IMixer mixer, QueenEngine vm)
            : base(mixer, vm)
        {
        }

        protected override void PlaySoundData(Stream f, ref SoundHandle soundHandle)
        {
            var tmp = ServiceLocator.AudioManager.MakeFlacStream(f);
            soundHandle = _mixer.PlayStream(SoundType.SFX, new AudioStreamWrapper(tmp));
        }
    }
}
