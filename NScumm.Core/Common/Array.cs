//
//  Array.cs
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

namespace NScumm.Core.Common
{
    /// <summary>
    /// This class implements a dynamically sized container, which
    /// can be accessed similar to a regular C++ array. Accessing
    /// elements is performed in constant time (like with plain arrays).
    /// In addition, one can append, insert and remove entries (this
    /// is the 'dynamic' part). Doing that in general takes time
    /// proportional to the number of elements in the array.
    /// 
    /// The container class closest to this in the C++ standard library is
    /// std::vector. However, there are some differences.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Array<T>: IEnumerable<T> where T : new()
    {
        protected int _capacity;
        protected int _size;
        protected T[] _storage;

        public bool Empty => _size == 0;
        public int Size => _size;
        public T[] Storage => _storage;

        public T this[int index]
        {
            get { return _storage[index]; }
            set { _storage[index] = value; }
        }

        public void Reserve(int newCapacity)
        {
            if (newCapacity <= _capacity)
                return;

            Array.Resize(ref _storage, newCapacity);
            _capacity = newCapacity;
        }

        public void Resize(int newSize)
        {
            Reserve(newSize);
            for (int i = _size; i < newSize; ++i)
                _storage[i] = new T();
            _size = newSize;
        }

        public void PushBack(T element)
        {
            if (_size + 1 <= _capacity)
                _storage[_size++] = element;
            else
                InsertAux(_size, new T[] { element });
        }

        private void InsertAux(int pos, T[] elements)
        {
            Reserve(RoundUpCapacity(_size + elements.Length));
            Array.Copy(elements, 0, _storage, pos, elements.Length);
        }

        private static int RoundUpCapacity(int capacity)
        {
            // Round up capacity to the next power of 2;
            // we use a minimal capacity of 8.
            int capa = 8;
            while (capa < capacity)
                capa <<= 1;
            return capa;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _size; i++)
            {
                yield return _storage[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T RemoveAt(int index)
        {
            T tmp = _storage[index];
            Array.Copy(_storage, index + 1, _storage, index, _size - index);
            _size--;
            return tmp;
        }

        public void Clear()
        {
            _storage = null;
            _capacity = 0;
            _size = 0;
        }
    }
}
