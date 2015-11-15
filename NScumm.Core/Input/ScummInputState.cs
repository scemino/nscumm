//
//  ScummInputState.cs
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

/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */
using System.Collections.Generic;
using System;

namespace NScumm.Core.Input
{
    public struct ScummInputState: IEquatable<ScummInputState>
    {
        public bool IsLeftButtonDown { get; private set; }

        public bool IsRightButtonDown { get; private set; }

        public bool IsKeyDown(KeyCode code)
        {
            return _keys != null && _keys.Contains(code);
        }

        public bool IsKeyUp(KeyCode code)
        {
            return !IsKeyDown(code);
        }

        public HashSet<KeyCode> GetKeys()
        {
            return GetKeys(this);
        }

        public ScummInputState(IList<KeyCode> keys, bool isMouseLeftDown, bool isMouseRightDown)
            : this()
        {
            _keys = new HashSet<KeyCode>(keys);   
            IsLeftButtonDown = isMouseLeftDown;
            IsRightButtonDown = isMouseRightDown;
        }

        public override int GetHashCode()
        {
            return IsLeftButtonDown.GetHashCode() ^ IsRightButtonDown.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is ScummInputState && Equals((ScummInputState)obj);
        }

        public bool Equals(ScummInputState state)
        {
            bool equals = IsLeftButtonDown == state.IsLeftButtonDown && IsRightButtonDown == state.IsRightButtonDown;
            if (equals)
            {
                var keys = GetKeys(this);
                var otherKeys = GetKeys(state);
                equals = keys.SetEquals(otherKeys);
            }
            return equals;
        }

        private static HashSet<KeyCode> GetKeys(ScummInputState state)
        {
            return state._keys ?? new HashSet<KeyCode>();
        }

        public static bool operator ==(ScummInputState a, ScummInputState b)
        {
            return Equals(a, b);
        }

        public static bool operator !=(ScummInputState a, ScummInputState b)
        {
            return !(a == b);
        }

        readonly HashSet<KeyCode> _keys;
    }
    
}
