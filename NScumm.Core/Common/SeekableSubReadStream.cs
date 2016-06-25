//
//  SeekableSubReadStream.cs
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
using System.IO;

namespace NScumm.Core
{
    public class SeekableSubReadStream : Stream
    {
        long _begin;
        long _end;
        readonly Stream _parentStream;

        public override bool CanRead
        {
            get
            {
                return _parentStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _parentStream.CanSeek;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return _parentStream.CanTimeout;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _parentStream.CanWrite;
            }
        }

        public override long Position
        {
            get
            {
                return _parentStream.Position - _begin;
            }
            set
            {
                _parentStream.Position = value + _begin;
            }
        }

        public override long Length
        {
            get
            {
                return _end - _begin;
            }
        }

        public override int ReadTimeout
        {
            get
            {
                return _parentStream.ReadTimeout;
            }
            set
            {
                _parentStream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return _parentStream.WriteTimeout;
            }
            set
            {
                _parentStream.WriteTimeout = value;
            }
        }

        public SeekableSubReadStream(Stream parentStream, long begin, long end)
        {
            _parentStream = parentStream;
            _begin = begin;
            _end = end;
            _parentStream.Seek(_begin, SeekOrigin.Begin);
        }

        protected override void Dispose(bool disposing)
        {
            _parentStream.Dispose();
        }

        public override void Flush()
        {
            _parentStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (_begin + offset > _end)
                    {
                        return _parentStream.Seek(_end, SeekOrigin.Begin);
                    }
                    else
                    {
                        return _parentStream.Seek(_begin + offset, SeekOrigin.Begin);
                    }
                case SeekOrigin.Current:
                    if (_begin + offset > _end)
                    {
                        return _parentStream.Seek(_end, SeekOrigin.Begin);
                    }
                    else
                    {
                        return _parentStream.Seek(offset, SeekOrigin.Current);
                    }
                case SeekOrigin.End:
                    return _parentStream.Seek(_end, SeekOrigin.Begin);
            }
            return 0;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _parentStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _parentStream.EndRead(asyncResult);
        }

        public override int ReadByte()
        {
            return _parentStream.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if ((Position + offset + count) > Length)
            {
                count = (int)(Length - (Position + offset));
            }
            return _parentStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if ((Position + offset + count) > Length)
            {
                count = (int)(Length - (Position + offset));
            }
            _parentStream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            if (Position < _end)
            {
                _parentStream.WriteByte(value);
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _parentStream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _parentStream.EndWrite(asyncResult);
        }

        public override void SetLength(long value)
        {
            _parentStream.SetLength(value);
        }
    }
}

