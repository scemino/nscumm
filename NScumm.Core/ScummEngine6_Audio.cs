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


namespace NScumm.Core
{
    partial class ScummEngine6
    {
        [OpCode(0x69)]
        void StopMusic()
        {
            _sound.StopAllSounds();
        }

        [OpCode(0x74)]
        void StartSound(int sound)
        {
            _sound.AddSoundToQueue(sound);
        }

        [OpCode(0x75)]
        void StopSound(int sound)
        {
            // TODO: scumm6
//            _sound.StopSound(sound);
        }

        [OpCode(0x76)]
        void StartMusic(int sound)
        {
            _sound.AddSoundToQueue(sound);
        }

        [OpCode(0x98)]
        void IsSoundRunning(int sound)
        {
            Push(sound != 0 && _sound.IsSoundRunning(sound));
        }

        [OpCode(0xac)]
        void SoundKludge(int[] args)
        {
            _sound.SoundKludge(args);
        }
    }
}

