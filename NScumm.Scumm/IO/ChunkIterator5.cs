//
//  ChunkIterator5.cs
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

using System.Collections.Generic;
using System.IO;
using System.Text;
using NScumm.Core;

namespace NScumm.Scumm.IO
{
    #region Chunk Class
    sealed class Chunk
    {
        public long Size { get; set; }

        public string Tag { get; set; }

        public long Offset { get; set; }
    }
    #endregion

    class ChunkIterator5: IEnumerator<Chunk>
    {
        readonly BinaryReader _reader;
        readonly long _position;
        readonly long _size;

        public ChunkIterator5(BinaryReader reader, long size)
        {
            _reader = reader;
            _position = reader.BaseStream.Position;
            _size = size;
        }

        public Chunk Current
        {
            get;
            private set;
        }

        object System.Collections.IEnumerator.Current
        {
            get { return Current; }
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            if (Current != null)
            {
                var offset = Current.Offset + Current.Size - 8;
                _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
            }
            Current = null;
            if (_reader.BaseStream.Position < (_position + _size - 8) && _reader.BaseStream.Position < _reader.BaseStream.Length)
            {
                var tag = Encoding.UTF8.GetString(_reader.ReadBytes(4));
                var size = _reader.ReadUInt32BigEndian();
                Current = new Chunk { Offset = _reader.BaseStream.Position, Size = size, Tag = tag };
            }
            return Current != null;
        }

        public static Chunk ReadChunk(BinaryReader reader)
        {
            var tag = Encoding.UTF8.GetString(reader.ReadBytes(4));
            var size = reader.ReadUInt32BigEndian();
            return new Chunk { Offset = reader.BaseStream.Position, Size = size, Tag = tag };
        }

        public void Reset()
        {
            _reader.BaseStream.Seek(_position, SeekOrigin.Begin);
            Current = null;
        }
    }
 
}
