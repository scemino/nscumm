//
//  State.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using System;

namespace NScumm.Queen
{
    public enum Direction
    {
        LEFT = 1,
        RIGHT = 2,
        FRONT = 3,
        BACK = 4
    }

    public enum StateTalk
    {
        TALK,
        MUTE
    }

    public static class State
    {
        private static readonly Direction[] sd = {
            Direction.BACK,
            Direction.RIGHT,
            Direction.LEFT,
            Direction.FRONT
        };

        public static Direction FindDirection(ushort state)
        {
            return sd[(state >> 2) & 3];
        }

        public static StateTalk FindTalk(ushort state)
        {
            return (state & (1 << 9)) != 0 ? StateTalk.TALK : StateTalk.MUTE;
        }
    }
}

