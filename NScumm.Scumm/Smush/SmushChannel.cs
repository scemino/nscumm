//
//  SmushChannel.cs
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
using System.Diagnostics;
using System.IO;

namespace NScumm.Scumm.Smush
{
    abstract class SmushChannel
    {
        public int TrackIdentifier{ get { return _track; } }

        public int AvailableSoundDataSize { get { return _sbufferSize; } }

        public abstract bool IsTerminated { get; }

        public abstract int Rate { get; }

        public abstract bool AppendData(BinaryReader b, int size);

        public abstract bool SetParameters(int nb, int flags, int volume, int pan, int index);

        public abstract bool CheckParameters(int index, int nb, int flags, int volume, int pan);

        public abstract byte[] GetSoundData();

        public abstract bool GetParameters(out bool stereo, out bool is_16bit, out int vol, out int pan);

        protected SmushChannel(int track)
        {
            _track = track;
            _dataSize = -1;
        }

        protected abstract bool HandleSubTags(ref int offset);

        protected void ProcessBuffer()
        {
            Debug.Assert(_tbuffer != null);
            Debug.Assert(_tbufferSize != 0);
            Debug.Assert(_sbuffer == null || _sbuffer.Length == 0);
            Debug.Assert(_sbufferSize == 0);

            if (_inData)
            {
                if (_dataSize < _tbufferSize)
                {
                    int offset = _dataSize;
                    while (HandleSubTags(ref offset))
                        ;
                    _sbufferSize = _dataSize;
                    _sbuffer = _tbuffer;
                    if (offset < _tbufferSize)
                    {
                        int new_size = _tbufferSize - offset;
                        _tbuffer = new byte[new_size];

                        Array.Copy(_sbuffer, offset, _tbuffer, 0, new_size);
                        _tbufferSize = new_size;
                    }
                    else
                    {
                        _tbuffer = new byte[0];
                        _tbufferSize = 0;
                    }
                    if (_sbufferSize == 0)
                    {
                        _sbuffer = new byte[0];
                    }
                }
                else
                {
                    _sbufferSize = _tbufferSize;
                    _sbuffer = _tbuffer;
                    _tbufferSize = 0;
                    _tbuffer = new byte[0];
                }
            }
            else
            {
                int offset = 0;
                while (HandleSubTags(ref offset))
                    ;
                if (_inData)
                {
                    _sbufferSize = _tbufferSize - offset;
                    Debug.Assert(_sbufferSize != 0);
                    _sbuffer = new byte[_sbufferSize];

                    Array.Copy(_tbuffer, offset, _sbuffer, 0, _sbufferSize);
                    _tbuffer = new byte[0];
                    _tbufferSize = 0;
                }
                else
                {
                    if (offset != 0)
                    {
                        var old = _tbuffer;
                        int new_size = _tbufferSize - offset;
                        _tbuffer = new byte[new_size];

                        Array.Copy(old, offset, _tbuffer, 0, new_size);

                        _tbufferSize = new_size;
                        old = null;
                    }
                }
            }
        }

        /// <summary>
        /// The track number.
        /// </summary>
        protected int _track;

        /// <summary>
        /// Data temporary buffer.
        /// </summary>
        protected byte[] _tbuffer = new byte[0];
        /// <summary>
        /// Temporary buffer size.
        /// </summary>
        protected int _tbufferSize;
        /// <summary>
        /// Sound buffer.
        /// </summary>
        protected byte[] _sbuffer = new byte[0];
        /// <summary>
        /// Sound buffer size.
        /// </summary>
        protected int _sbufferSize;

        /// <summary>
        /// Remaining size of sound data in the iMUS buffer.
        /// </summary>
        protected int _dataSize;

        protected bool _inData;

        protected int _volume;
        protected int _pan;
    }
}
