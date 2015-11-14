//
//  AppleII_SoundFunction2_SymmetricWave.cs
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

using System.Diagnostics;
using NScumm.Scumm.Audio.Players;

namespace NScumm.Scumm.Audio.AppleII
{
    /// <summary>
    /// SoundFunction2: symmetric wave (~)
    /// </summary>
    class AppleII_SoundFunction2_SymmetricWave : IAppleII_SoundFunction
    {
        public void Init(Player_AppleII player, byte[] args)
        {
            _player = player;
            _params = args;
            _pos = 1;
        }

        public bool Update()
        { // D0D6
            // while (pos = 1; pos < 256; ++pos)
            if (_pos < 256)
            {
                byte interval = _params[_pos];
                if (interval == 0xFF)
                    return true;
                _update(interval, _params[0] /*, LD12F=interval*/);

                ++_pos;
                return false;
            }
            return true;
        }

        void _update(int interval /*a*/, int count)
        { // D0EF
            if (interval == 0xFE)
            {
                _player.Wait(interval, 10);
            }
            else
            {
                Debug.Assert(count > 0); // 0 == 256?
                Debug.Assert(interval > 0); // 0 == 256?

                int a = (interval >> 3) + count;
                for (int y = a; y > 0; --y)
                {
                    _player.GenerateSamples(1292 - 5 * interval);
                    _player.SpeakerToggle();

                    _player.GenerateSamples(1287 - 5 * interval);
                    _player.SpeakerToggle();
                }
            }
        }

        Player_AppleII _player;
        byte[] _params;
        int _pos;
    }    
}
