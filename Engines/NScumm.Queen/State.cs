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

    public enum StateUse
    {
        STATE_USE,
        STATE_USE_ON
    }

    public enum StateOn
    {
        STATE_ON_ON,
        STATE_ON_OFF
    }

    public static class State
    {
        private static readonly Direction[] sd = {
            Direction.BACK,
            Direction.RIGHT,
            Direction.LEFT,
            Direction.FRONT
        };

        private static readonly Verb[] sdv = {
            Verb.NONE,
            Verb.OPEN,
            Verb.NONE,
            Verb.CLOSE,

            Verb.NONE,
            Verb.NONE,
            Verb.LOOK_AT,
            Verb.MOVE,

            Verb.GIVE,
            Verb.TALK_TO,
            Verb.NONE,
            Verb.NONE,

            Verb.USE,
            Verb.NONE,
            Verb.PICK_UP,
            Verb.NONE
        };

        static readonly StateGrab[] sg = {
            StateGrab.NONE,
            StateGrab.DOWN,
            StateGrab.UP,
            StateGrab.MID
        };

        public static Direction FindDirection(ushort state)
        {
            return sd[(state >> 2) & 3];
        }

        public static StateTalk FindTalk(ushort state)
        {
            return (state & (1 << 9)) != 0 ? StateTalk.TALK : StateTalk.MUTE;
        }

        public static Verb FindDefaultVerb(ushort state)
        {
            return sdv[(state >> 4) & 0xF];
        }

        public static StateUse FindUse(ushort state)
        {
            return ((state & (1 << 10))!=0) ? StateUse.STATE_USE : StateUse.STATE_USE_ON;
        }

        public static StateGrab FindGrab(ushort state)
        {
            return sg[state & 3];
        }

        public static void AlterDefaultVerb(ref ushort objState, Verb v)
        {
            ushort val;
            switch (v)
            {
                case Verb.OPEN:
                    val = 1;
                    break;
                case Verb.CLOSE:
                    val = 3;
                    break;
                case Verb.MOVE:
                    val = 7;
                    break;
                case Verb.GIVE:
                    val = 8;
                    break;
                case Verb.USE:
                    val = 12;
                    break;
                case Verb.PICK_UP:
                    val = 14;
                    break;
                case Verb.TALK_TO:
                    val = 9;
                    break;
                case Verb.LOOK_AT:
                    val = 6;
                    break;
                default:
                    val = 0;
                    break;
            }
            objState = (ushort)((objState & ~0xF0) | (val << 4));
        }

        public static StateOn FindOn(ushort state)
        {
            return ((state & (1 << 8))!=0) ? StateOn.STATE_ON_ON : StateOn.STATE_ON_OFF;
        }

        public static void AlterOn(ref ushort objState, StateOn state)
        {
            switch (state)
            {
                case StateOn.STATE_ON_ON:
                    objState |= (1 << 8);
                    break;
                case StateOn.STATE_ON_OFF:
                    objState = ((ushort)(objState & ~(1 << 8)));
                    break;
            }
        }

   }
}

