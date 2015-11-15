//
//  SampleBuffer.cs
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

using System;

namespace NScumm.Scumm.Audio
{

    /// <summary>
    /// Optimized for use with periodical read/write phases when the buffer
    /// is filled in a write phase and completely read in a read phase.
    /// The growing strategy is optimized for repeated small (e.g. 2 bytes)
    /// single writes resulting in large buffers
    ///  (avg.: 4KB, max: 18KB @ 16bit/22.050kHz (MM sound21)).
    /// </summary>
    class SampleBuffer
    {
        public int AvailableSize
        {
            get
            {
                if (_readPos >= _writePos)
                    return 0;
                return _writePos - _readPos;
            }
        }

        public SampleBuffer()
        {
            Clear();
        }

        public void Clear()
        {
            _data = null;
            _capacity = 0;
            _writePos = 0;
            _readPos = 0;
        }

        public int Read(short[] dataPtr, int offset, int dataSize)
        {
            var avail = AvailableSize;
            if (avail == 0)
                return 0;
            if (dataSize > avail)
                dataSize = avail;
            Array.Copy(_data, _readPos, dataPtr, offset, dataSize);
            _readPos += dataSize;
            return dataSize;
        }

        public int Write(short value)
        {
            EnsureFree(1);
            _data[_writePos] = value;
            _writePos ++;
            return 1;
        }

        void EnsureFree(int needed)
        {
            // if data was read completely, reset read/write pos to front
            if ((_writePos != 0) && (_writePos == _readPos))
            {
                _writePos = 0;
                _readPos = 0;
            }

            // check for enough space at end of buffer
            var freeEndCnt = _capacity - _writePos;
            if (needed <= freeEndCnt)
                return;

            var avail = AvailableSize;

            // check for enough space at beginning and end of buffer
            if (needed <= _readPos + freeEndCnt)
            {
                // move unread data to front of buffer
                Array.Copy(_data, _readPos, _data, 0, avail);
                _writePos = avail;
                _readPos = 0;
            }
            else
            { // needs a grow
                var old_data = _data;
                int new_len = avail + needed;

                _capacity = new_len + 2048;
                _data = new short[_capacity];

                if (old_data != null)
                {
                    // copy old unread data to front of new buffer
                    Array.Copy(old_data, _readPos, _data, 0, avail);
                    _writePos = avail;
                    _readPos = 0;
                }
            }
        }

        int _writePos;
        int _readPos;
        int _capacity;
        short[] _data;
    }
    
}
