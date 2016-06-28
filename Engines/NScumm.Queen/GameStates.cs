//
//  GameStates.cs
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
using System.Collections;
using System.Collections.Generic;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    internal class GameStates : IList<short>
    {
        short[] _items;

        public int Count
        {
            get
            {
                return _items.Length;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public short this[int index]
        {
            get
            {
                return _items[index];
            }

            set
            {
                D.Debug(8, $"Logic::gameState() [{index}] = {value}");
                _items[index] = value;
            }
        }

        public GameStates()
        {
            _items = new short[Logic.GAME_STATE_COUNT];
        }

        void ICollection<short>.Add(short item)
        {
            throw new NotSupportedException();
        }

        void ICollection<short>.Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(short item)
        {
            return Array.IndexOf(_items, item) != -1;
        }

        public void CopyTo(short[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _items.Length);
        }

        public IEnumerator<short> GetEnumerator()
        {
            return (IEnumerator<short>)_items.GetEnumerator();
        }

        bool ICollection<short>.Remove(short item)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public int IndexOf(short item)
        {
            return Array.IndexOf(_items, item);
        }

        void IList<short>.Insert(int index, short item)
        {
            throw new NotSupportedException();
        }

        void IList<short>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
    }

}
