//
//  SaudChannel.cs
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
using NScumm.Core;

namespace NScumm.Scumm.Smush
{
    class SaudChannel : SmushChannel
    {
        public SaudChannel(int trackId)
            : base(trackId)
        {
        }

        public override bool IsTerminated
        {
            get{ return (_markReached && _dataSize == 0 && _sbuffer.Length == 0); }
        }

        public override int Rate
        {
            get{ return 22050; }
        }

        public override bool AppendData(BinaryReader b, int size)
        {
            if (_dataSize == -1)
            {
                Debug.Assert(size > 8);
                var saud_type = System.Text.Encoding.UTF8.GetString(b.ReadBytes(4));
                /*uint32 saud_size =*/
                b.ReadUInt32BigEndian();
                if (saud_type != "SAUD")
                    throw new NotSupportedException(string.Format("Invalid Chunk for SaudChannel : {0}", saud_type));
                size -= 8;
                _dataSize = -2;
            }
            if (_tbuffer.Length != 0)
            {
                var old = _tbuffer;
                _tbuffer = new byte[_tbufferSize + size];
                Array.Copy(old, _tbuffer, _tbufferSize);
                old = null;
                Array.Copy(b.ReadBytes(size), 0, _tbuffer, _tbufferSize, size);

                _tbufferSize += size;
            }
            else
            {
                _tbufferSize = size;
                _tbuffer = b.ReadBytes(_tbufferSize);
            }

            if (_keepSize)
            {
                _sbufferSize = _tbufferSize;
                _sbuffer = _tbuffer;
                _tbufferSize = 0;
                _tbuffer = new byte[0];
            }
            else
            {
                ProcessBuffer();
            }

            return true;
        }

        public override bool SetParameters(int nb, int flags, int volume, int pan, int index)
        {
            _nbframes = nb;
            _flags = flags; // bit 7 == IS_VOICE, bit 6 == IS_BACKGROUND_MUSIC, other ??
            _volume = volume;
            _pan = pan;
            _index = index;
            if (index != 0)
            {
                _dataSize = -2;
                _keepSize = true;
                _inData = true;
            }
            return true;
        }

        public override bool CheckParameters(int index, int nb, int flags, int volume, int pan)
        {
            if (++_index != index)
                throw new NotSupportedException("invalid index in SaudChannel::CheckParameters()");
            if (_nbframes != nb)
                throw new NotSupportedException("invalid duration in SaudChannel::CheckParameters()");
            if (_flags != flags)
                throw new NotSupportedException("invalid flags in SaudChannel::CheckParameters()");
            if (_volume != volume || _pan != pan)
            {
                _volume = volume;
                _pan = pan;
            }
            return true;
        }

        public override byte[] GetSoundData()
        {
            var tmp = _sbuffer;

            if (!_keepSize)
            {
                Debug.Assert(_dataSize > 0);
                _dataSize -= _sbufferSize;
            }

            _sbuffer = new byte[0];
            _sbufferSize = 0;

            return tmp;
        }

        public override bool GetParameters(out bool stereo, out bool is_16bit, out int vol, out int pan)
        {
            stereo = false;
            is_16bit = false;
            vol = _volume;
            pan = _pan;
            return true;
        }

        protected override bool HandleSubTags(ref int offset)
        {
            if (_tbufferSize - offset >= 8)
            {
                var type = System.Text.Encoding.UTF8.GetString(_tbuffer, offset, 4);
                var size = ScummHelper.SwapBytes(BitConverter.ToUInt32(_tbuffer, offset + 4));
                var available_size = _tbufferSize - offset;

                switch (type)
                {
                    case "STRK":
                        _inData = false;
                        if (available_size >= (size + 8))
                        {
                            var subSize = ScummHelper.SwapBytes(BitConverter.ToUInt32(_tbuffer, offset + 4));
                            if (subSize != 14 && subSize != 10)
                            {
                                throw new NotSupportedException(string.Format("STRK has an invalid size : {0}", subSize));
                            }
                        }
                        else
                            return false;
                        break;
                    case "SMRK":
                        _inData = false;
                        if (available_size >= (size + 8))
                            _markReached = true;
                        else
                            return false;
                        break;
                    case "SHDR":
                        _inData = false;
                        if (available_size >= (size + 8))
                        {
                            var subSize = ScummHelper.SwapBytes(BitConverter.ToUInt32(_tbuffer, offset + 4));
                            if (subSize != 4)
                                throw new NotSupportedException(string.Format("SHDR has an invalid size : {0}", subSize));
                        }
                        else
                            return false;
                        break;
                    case "SDAT":
                        _inData = true;
                        _dataSize = (int)size;
                        offset += 8;
                        return false;
                    default:
                        throw new NotSupportedException(string.Format("unknown Chunk in SAUD track : {0}", type));
                }
                offset += (int)(size + 8);
                return true;
            }
            return false;
        }

        int _nbframes;
        bool _markReached;
        int _flags;
        int _index;
        bool _keepSize;
    }


}

