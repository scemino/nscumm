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

#if ENABLE_SCI32
using System;
using System.Collections;
using System.Collections.Generic;

namespace NScumm.Sci.Graphics
{
    internal class StablePointerArray<T>: IList<T>
    {
        private readonly T[] _items;
        private int _size;

        public StablePointerArray(int n)
        {
            _items = new T[n];
        }

        public StablePointerArray(StablePointerArray<T> other, Func<T, T> clone)
        {
            _size = other._size;
            for (var i = 0; i < _size; ++i)
            {
                if (other._items[i] == null)
                {
                    _items[i] = default(T);
                }
                else
                {
                    _items[i] = clone(other._items[i]);
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _size; i++)
            {
                yield return _items[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(T item)
        {
            _items[_size++] = item;
        }

        public void Clear()
        {
            Array.Clear(_items,0,_size);
            _size = 0;
        }

        public bool Contains(T item)
        {
            return Array.IndexOf(_items, item, 0, _size) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        public bool Remove(T item)
        {
            var index = Array.IndexOf(_items, item, 0, _size);
            if (index == -1) return false;

            _items[index] = default(T);
            return true;
        }

        public int Count => _size;
        public bool IsReadOnly => false;

        public int IndexOf(T item)
        {
            return Array.IndexOf(_items, item, 0, _size);
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            _items[index] = default(T);
        }

        public T this[int index]
        {
            get { return _items[index]; }
            set { _items[index] = value; }
        }

        /// <summary>
        /// Removes freed pointers from the pointer list.
        /// </summary>
        /// <returns>The new size of the list.</returns>
        public int Pack()
        {
            var newSize = 0;
            for (var i = 0; i < _size; i++)
            {
                if (_items[i] == null) continue;

                _items[newSize++] = _items[i];
            }
            _size = newSize;
            return newSize;
        }

        public virtual void Sort()
        {
            Array.Sort(_items, 0, _size);
        }
    }
}

#endif
