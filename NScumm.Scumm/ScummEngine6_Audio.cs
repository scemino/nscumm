//
//  ScummEngine6.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

using NScumm.Scumm.Audio.IMuse.IMuseDigital;

namespace NScumm.Scumm
{
    partial class ScummEngine6
    {
        [OpCode(0x69)]
        protected override void StopMusic()
        {
            Sound.StopAllSounds();
        }

        [OpCode(0x74)]
        protected virtual void StartSound(int sound)
        {
            if (Game.Version >= 7)
            {
                ((IMuseDigital)MusicEngine).StartSfx(sound, 64);
            }
            else
            {
                Sound.AddSoundToQueue(sound);
            }
        }

        [OpCode(0x75)]
        protected virtual void StopSound(int sound)
        {
            Sound.StopSound(sound);
        }

        [OpCode(0x76)]
        protected virtual void StartMusic(int sound)
        {
            Sound.AddSoundToQueue(sound);
        }

        [OpCode(0x98)]
        protected virtual void IsSoundRunning(int sound)
        {
            Push(sound != 0 && Sound.IsSoundRunning(sound));
        }

        [OpCode(0xac)]
        protected virtual void SoundKludge(int[] args)
        {
            Sound.SoundKludge(args);
        }
    }
}

