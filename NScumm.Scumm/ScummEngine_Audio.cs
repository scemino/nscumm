//
//  ScummEngine_Audio.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NScumm.Core.Audio;

namespace NScumm.Scumm
{
    partial class ScummEngine
    {
        protected bool _saveSound;

        internal Sound Sound { get; private set; }

        internal IMixer Mixer { get; private set; }

        void PlayActorSounds()
        {
            int sound;
            for (var i = 1; i < Actors.Length; i++)
            {
                if (Actors[i].Cost.SoundCounter != 0 && Actors[i].IsInCurrentRoom)
                {
                    CurrentScript = 0xFF;

                    if (Game.Version == 0)
                    {
                        sound = Actors[i].Sound & 0x3F;
                    }
                    else
                    {
                        sound = Actors[i].Sound;
                    }
                    // fast mode will flood the queue with walk sounds
//                    if (!_fastMode) {
                    Sound.AddSoundToQueue(sound);
//                    }
                    for (var j = 1; j < Actors.Length; j++)
                    {
                        Actors[j].Cost.SoundCounter = 0;
                    }
                    return;
                }
            }
        }
    }
}

