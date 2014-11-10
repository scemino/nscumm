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
using System;

namespace NScumm.Core
{
    partial class ScummEngine
    {
        protected Sound _sound;

        public Sound Sound{ get { return _sound; } }

        public void UpdateSound()
        {
            _sound.Update();
        }

        void PlayActorSounds()
        {
            for (var i = 1; i < _actors.Length; i++)
            {
                if (_actors[i].Cost.SoundCounter != 0 && _actors[i].IsInCurrentRoom)
                {
                    _currentScript = 0xFF;

                    var sound = _actors[i].Sound;
                    // fast mode will flood the queue with walk sounds
//                    if (!_fastMode) {
                    _sound.AddSoundToQueue(sound);
//                    }
                    for (var j = 1; j < _actors.Length; j++)
                    {
                        _actors[j].Cost.SoundCounter = 0;
                    }
                    return;
                }
            }
        }

    }
}

