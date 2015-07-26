//
//  AppleII_SoundFunction1_FreqUpDown.cs
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

namespace NScumm.Core.Audio
{
    /// <summary>
    /// SoundFunction1: frequency up/down
    /// </summary>
    class AppleII_SoundFunction1_FreqUpDown : IAppleII_SoundFunction
    {
        public void Init(Player_AppleII player, byte[] args)
        {
            _player = player;
            _delta = args[0];
            _count = args[1];
            _interval = args[2];
            _limit = args[3];
            _decInterval = (args[4] >= 0x40);
        }

        public bool Update()
        { // D085
            if (_decInterval)
            {
                do
                {
                    _update(_interval, _count);
                    _interval -= (byte)_delta;
                } while (_interval >= _limit);
            }
            else
            {
                do
                {
                    _update(_interval, _count);
                    _interval += (byte)_delta;
                } while (_interval < _limit);
            }
            return true;
        }

        void _update(int interval /*a*/, int count /*y*/)
        { // D076
            Debug.Assert(interval > 0); // 0 == 256?
            Debug.Assert(count > 0); // 0 == 256?

            for (; count >= 0; --count)
            {
                _player.SpeakerToggle();
                _player.GenerateSamples(17 + 5 * interval);
            }
        }

        Player_AppleII _player;
        int _delta;
        int _count;
        byte _interval;
        // must be unsigned byte ("interval < delta" possible)
        int _limit;
        bool _decInterval;
    }
    
}
