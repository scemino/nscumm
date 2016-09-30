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

namespace NScumm.Core.Common
{
    public static class StreamExtension
    {
        public static Stream ReadStream(this Stream stream, long dataSize)
        {
            return new SeekableSubReadStream(stream, stream.Position, stream.Position + dataSize, true);
        }
    }

    public class SeekableSubReadStream : Stream
    {
        private readonly long _begin;
        private readonly long _end;
        private DisposablePtr<Stream> _parentStream;

        public override bool CanRead => _parentStream.Value.CanRead;

        public override bool CanSeek => _parentStream.Value.CanSeek;

        public override bool CanTimeout => _parentStream.Value.CanTimeout;

        public override bool CanWrite => _parentStream.Value.CanWrite;

        public override long Position
        {
            get
            {
                return _parentStream.Value.Position - _begin;
            }
            set
            {
                _parentStream.Value.Position = value + _begin;
            }
        }

        public override long Length => _end - _begin;

        public override int ReadTimeout
        {
            get
            {
                return _parentStream.Value.ReadTimeout;
            }
            set
            {
                _parentStream.Value.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return _parentStream.Value.WriteTimeout;
            }
            set
            {
                _parentStream.Value.WriteTimeout = value;
            }
        }

        public SeekableSubReadStream(Stream parentStream, long begin, long end, bool disposeParentStream)
        {
            _parentStream = new DisposablePtr<Stream>(parentStream,disposeParentStream);
            _begin = begin;
            _end = end;
            _parentStream.Value.Seek(_begin, SeekOrigin.Begin);
        }

        protected override void Dispose(bool disposing)
        {
            _parentStream.Dispose();
        }

        public override void Flush()
        {
            _parentStream.Value.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    if (_begin + offset > _end)
                    {
                        return _parentStream.Value.Seek(_end, SeekOrigin.Begin);
                    }
                    return _parentStream.Value.Seek(_begin + offset, SeekOrigin.Begin);
                case SeekOrigin.Current:
                    if (_begin + offset > _end)
                    {
                        return _parentStream.Value.Seek(_end, SeekOrigin.Begin);
                    }
                    return _parentStream.Value.Seek(offset, SeekOrigin.Current);
                case SeekOrigin.End:
                    return _parentStream.Value.Seek(_end, SeekOrigin.Begin);
            }
            return 0;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _parentStream.Value.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _parentStream.Value.EndRead(asyncResult);
        }

        public override int ReadByte()
        {
            return _parentStream.Value.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position + offset + count > Length)
            {
                count = (int)(Length - (Position + offset));
            }
            return _parentStream.Value.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (Position + offset + count > Length)
            {
                count = (int)(Length - (Position + offset));
            }
            _parentStream.Value.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            if (Position < _end)
            {
                _parentStream.Value.WriteByte(value);
            }
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return _parentStream.Value.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _parentStream.Value.EndWrite(asyncResult);
        }

        public override void SetLength(long value)
        {
            _parentStream.Value.SetLength(value);
        }
    }
}

