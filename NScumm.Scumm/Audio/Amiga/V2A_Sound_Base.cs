//
//  V2A_Sound_Base.cs
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

namespace NScumm.Scumm.Audio.Amiga
{
    abstract class V2A_Sound_Base : IV2A_Sound
    {
        public const int BASE_FREQUENCY = 3579545;

        protected IPlayerMod Player { get; set; }

        protected int Id { get; set; }

        protected V2A_Sound_Base(int numChan)
            : this(numChan, 0, 0)
        {
        }

        protected V2A_Sound_Base(int numChan, ushort offset, ushort size)
        { 
            _numChan = numChan;
            _offset = offset;
            _size = size;
        }

        public abstract void Start(IPlayerMod mod, int id, byte[] data);

        public abstract bool Update();

        public virtual void Stop()
        {
            Debug.Assert(Id != 0);
            for (int i = 0; i < _numChan; i++)
                Player.StopChannel(Id | (i << 8));
            Id = 0;
            _data = null;
        }

        readonly int _numChan;
        protected readonly ushort _offset;
        protected readonly ushort _size;
        protected byte[] _data;
    }

    // plays two looped waveforms
    
}
