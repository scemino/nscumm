//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016
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

namespace NScumm.Core.Common
{
    /// <summary>
    /// Simple random number generator. Although it is definitely not suitable for
    /// cryptographic purposes, it serves our purposes just fine.
    /// </summary>
    public class RandomSource
    {
        private uint _randSeed;

        /// <summary>
        /// Construct a new randomness source with the specific name.
        /// The name used name must be globally unique, and is used to
        /// register the randomness source with the active event recorder,
        /// if any.
        /// </summary>
        /// <param name="name"></param>
        public RandomSource(string name)
        {
        }

        public uint Seed
        {
            get { return _randSeed; }
            set
            {
                _randSeed = value;
            }
        }

        /// <summary>
        /// Generates a random unsigned integer in the interval [0, max].
        /// </summary>
        /// <param name="max">the upper bound</param>
        /// <returns>a random number in the interval [0, max]</returns>
        public uint GetRandomNumber(uint max)
        {
            _randSeed = 0xDEADBF03 * (_randSeed + 1);
            _randSeed = (_randSeed >> 13) | (_randSeed << 19);
            return _randSeed % (max + 1);
        }

        /// <summary>
        /// Generates a random bit, i.e. either 0 or 1.
        /// Identical to getRandomNumber(1), but potentially faster.
        /// </summary>
        /// <returns>a random bit, either 0 or 1</returns>
        public uint GetRandomBit()
        {
            _randSeed = 0xDEADBF03 * (_randSeed + 1);
            _randSeed = (_randSeed >> 13) | (_randSeed << 19);
            return _randSeed & 1;
        }

        /// <summary>
        /// Generates a random unsigned integer in the interval [min, max].
        /// </summary>
        /// <param name="min">the lower bound</param>
        /// <param name="max">the upper bound</param>
        /// <returns>a random number in the interval [min, max]</returns>
        public uint GetRandomNumberRng(uint min, uint max)
        {
            return GetRandomNumber(max - min) + min;
        }
    }
}
