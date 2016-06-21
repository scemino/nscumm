//
//  PackBitsReadStream.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NScumm.Core.Graphics;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Core
{
    class PackBitsReadStream : Stream
    {
        Stream _input;

        public override bool CanRead
        {
            get { return _input.CanRead; }
        }

        public override bool CanWrite
        {
            get { return _input.CanWrite; }
        }

        public override bool CanSeek
        {
            get { return _input.CanSeek; }
        }

        public override long Length
        {
            get { return _input.Length; }
        }

        public override long Position
        {
            get
            {
                return _input.Position;
            }

            set
            {
                _input.Position = value;
            }
        }

        public override void SetLength(long value)
        {
            _input.SetLength(value);
        }

        public PackBitsReadStream(Stream input)
        {
            _input = input;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _input.Seek(offset, origin);
        }

        public override void Flush()
        {
            _input.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var @out = buffer;
            int o = offset;
            int left = count;

            uint lenR = 0, lenW = 0;
            while (left > 0 && _input.Position != _input.Length)
            {
                lenR = (uint)_input.ReadByte();

                if (lenR == 128)
                {
                    // no-op
                    lenW = 0;
                }
                else if (lenR <= 127)
                {
                    // literal run
                    lenR++;
                    lenW = (uint)Math.Min(lenR, left);
                    for (int j = 0; j < lenW; j++)
                    {
                        @out[o++] = (byte)_input.ReadByte();
                    }
                    for (; lenR > lenW; lenR--)
                    {
                        _input.ReadByte();
                    }
                }
                else
                {  // len > 128
                   // expand run
                    lenW = (uint)Math.Min((256 - lenR) + 1, left);
                    byte val = (byte)_input.ReadByte();
                    @out.Set(o, val, (int)lenW);
                    o = (int)(o + lenW);
                }

                left = (int)(left - lenW);
            }

            return count - left;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
    
}
