//
//  V2A_Sound_MultiLoopedDuration.cs
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
    // plays two looped waveforms for a fixed number of frames
    class V2A_Sound_MultiLoopedDuration : V2A_Sound_MultiLooped
    {
        public V2A_Sound_MultiLoopedDuration(ushort offset, ushort size, ushort freq1, byte vol1, ushort freq2, byte vol2, ushort numframes)
            : base(offset, size, freq1, vol1, freq2, vol2)
        {
            _duration = numframes;
        }

        public override void Start(IPlayerMod mod, int id, byte[] data)
        {
            base.Start(mod, id, data);
            _ticks = 0;
        }

        public override bool Update()
        {
            Debug.Assert(Id != 0);
            _ticks++;
            if (_ticks >= _duration)
                return false;
            return true;
        }

        readonly ushort _duration;
        int _ticks;
    }
    
}
