//
//  AppleII_SoundFunction4_Polyphone.cs
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

namespace NScumm.Core.Audio
{
    /// <summary>
    ///  SoundFunction4: polyphone (2 voices)
    /// </summary>
    class AppleII_SoundFunction4_Polyphone : IAppleII_SoundFunction
    {
        public void Init(Player_AppleII player, byte[] args)
        {
            _player = player;
            _params = args;
            _updateRemain1 = 80;
            _updateRemain2 = 10;
            _count = 0;
        }

        public bool Update()
        { // D170
            // while (_params[0] != 0x01)
            if (_params[_paramsOffset] != 0x01)
            {
                if (_count == 0) // prepare next loop
                    NextLoop(_params[_paramsOffset], _params[_paramsOffset + 1], _params[_paramsOffset + 2]);
                if (LoopIteration()) // loop finished -> fetch next parameter set
                    _paramsOffset += 3;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Prepare for next parameter set loop
        /// </summary>
        /// <param name="param0">Param0.</param>
        /// <param name="param1">Param1.</param>
        /// <param name="param2">Param2.</param>
        void NextLoop(byte param0, byte param1, byte param2)
        { // LD182
            _count = (ushort)((-param2 << 8) | 0x3);

            _bitmask1 = 0x3;
            _bitmask2 = 0x3;

            _updateInterval2 = param0;
            if (_updateInterval2 == 0)
                _bitmask2 = 0x0;

            _updateInterval1 = param1;
            if (_updateInterval1 == 0)
            {
                _bitmask1 = 0x0;
                if (_bitmask2 != 0)
                {
                    _bitmask1 = _bitmask2;
                    _bitmask2 = 0;
                    _updateInterval1 = _updateInterval2;
                }
            }

            _speakerShiftReg = 0;
        }

        /// <summary>
        /// perform one loop iteration
        /// Returns true if loop finished.
        /// </summary>
        /// <returns><c>true</c>, if iteration was looped, <c>false</c> otherwise.</returns>
        bool LoopIteration()
        { // D1A2
            --_updateRemain1;
            --_updateRemain2;

            if (_updateRemain2 == 0)
            {
                _updateRemain2 = _updateInterval2;
                // use only first voice's data (bitmask1) if both voices are triggered
                if (_updateRemain1 != 0)
                {
                    _speakerShiftReg ^= _bitmask2;
                }
            }

            if (_updateRemain1 == 0)
            {
                _updateRemain1 = _updateInterval1;
                _speakerShiftReg ^= _bitmask1;
            }

            if ((_speakerShiftReg & 0x1) != 0)
                _player.SpeakerToggle();
            _speakerShiftReg >>= 1;
            _player.GenerateSamples(42); /* actually 42.5 */

            ++_count;
            return (_count == 0);
        }

        Player_AppleII _player;
        byte[] _params;
        int _paramsOffset;

        byte _updateRemain1;
        byte _updateRemain2;

        ushort _count;
        byte _bitmask1;
        byte _bitmask2;
        byte _updateInterval1;
        byte _updateInterval2;
        byte _speakerShiftReg;
    }
    
}
