//
//  ImuseChannel.cs
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
using NScumm.Scumm.Audio.IMuse.IMuseDigital;

namespace NScumm.Scumm.Smush
{
    class ImuseChannel: SmushChannel
    {
        public ImuseChannel(int track)
            : base(track)
        {
        }

        #region implemented abstract members of SmushChannel

        public override bool AppendData(BinaryReader b, int size)
        {
            if (_dataSize == -1)
            {
                Debug.Assert(size > 8);
                var imus_type = System.Text.Encoding.UTF8.GetString(b.ReadBytes(4));
                /*uint32 imus_size =*/
                b.ReadUInt32BigEndian();
//                if (imus_type != "iMUS")
//                    Console.Error.WriteLine("Invalid Chunk for imuse_channel");
                size -= 8;
                _tbufferSize = size;
                Debug.Assert(_tbufferSize != 0);
                _tbuffer = b.ReadBytes(size);
                _dataSize = -2;
            }
            else
            {
                if (_tbuffer.Length != 0)
                {
                    var old = _tbuffer;
                    int new_size = size + _tbufferSize;
                    _tbuffer = new byte[new_size];
                    Array.Copy(old, _tbuffer, _tbufferSize);
                    b.BaseStream.Read(_tbuffer, _tbufferSize, size);
                    _tbufferSize += size;
                }
                else
                {
                    _tbufferSize = size;
                    _tbuffer = new byte[_tbufferSize];
                    b.BaseStream.Read(_tbuffer, 0, size);
                }
            }

            ProcessBuffer();

            _srbufferSize = _sbufferSize;
            if (_sbuffer.Length != 0 && _bitsize == 12)
                Decode();

            return true;
        }

        public override bool SetParameters(int nb, int size, int flags, int unk1, int tmp)
        {
            if ((flags == 1) || (flags == 2) || (flags == 3))
            {
                _volume = 127;
            }
            else if ((flags >= 100) && (flags <= 163))
            {
                _volume = flags * 2 - 200;
            }
            else if ((flags >= 200) && (flags <= 263))
            {
                _volume = flags * 2 - 400;
            }
            else if ((flags >= 300) && (flags <= 363))
            {
                _volume = flags * 2 - 600;
            }
//            else
//            {
//                Console.Error.WriteLine("ImuseChannel::setParameters(): bad flags: {0}", flags);
//            }
            _pan = 0;
            return true;
        }

        public override bool CheckParameters(int index, int nb, int flags, int volume, int pan)
        {
            return true;
        }

        public override byte[] GetSoundData()
        {
            var tmp = _sbuffer;

            Debug.Assert(_dataSize > 0);
            _dataSize -= _srbufferSize;

            _sbuffer = new byte[0];
            _sbufferSize = 0;

            return tmp;
        }

        public override bool GetParameters(out bool stereo, out bool is_16bit, out int vol, out int pan)
        {
            stereo = (_channels == 2);
            is_16bit = (_bitsize > 8);
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
                    case "MAP ":
                        _inData = false;
                        if (available_size >= (size + 8))
                        {
                            var tmp = new byte[_tbuffer.Length - offset];
                            Array.Copy(_tbuffer, offset, tmp, 0, tmp.Length);
                            HandleMap(tmp);
                        }
                        break;
                    case "DATA":
                        _inData = true;
                        _dataSize = (int)size;
                        offset += 8;
                        {
                            int reqsize = 1;
                            if (_channels == 2)
                                reqsize *= 2;
                            if (_bitsize == 16)
                                reqsize *= 2;
                            else if (_bitsize == 12)
                            {
                                if (reqsize > 1)
                                    reqsize = reqsize * 3 / 2;
                                else
                                    reqsize = 3;
                            }
                            if ((size % reqsize) != 0)
                            {
                                Debug.WriteLine("Invalid iMUS sound data size : ({0} %% {1}) != 0, correcting...", size, reqsize);
                                size += (uint)(3 - (size % reqsize));
                            }
                        }
                        return false;
//                    default:
//                        Console.Error.WriteLine("unknown Chunk in iMUS track : {0} ", type);
//                        break;
                }
                offset += (int)(size + 8);
                return true;
            }
            return false;
        }

        public override bool IsTerminated
        {
            get
            {
                return (_dataSize <= 0 && _sbuffer.Length == 0);
            }
        }

        public override int Rate
        {
            get
            {
                return _rate;
            }
        }

        #endregion

        bool HandleMap(byte[] data)
        {
            // Read the chunk size & skip over the chunk header
            var size = ScummHelper.SwapBytes(BitConverter.ToUInt32(data, 4));
            var i = 8;

            while (size > 0)
            {
                var subType = System.Text.Encoding.UTF8.GetString(data, i, 4);
                var subSize = ScummHelper.SwapBytes(BitConverter.ToUInt32(data, i + 4));
                i += 8;
                size -= 8;

                switch (subType)
                {
                    case "FRMT":
//                        if (subSize != 20)
//                            Console.Error.WriteLine("invalid size for FRMT Chunk");
                        //uint32 imuse_start = READ_BE_UINT32(data);
                        //uint32 unk = READ_BE_UINT32(data+4);
                        _bitsize = (int)ScummHelper.SwapBytes(BitConverter.ToUInt32(data, i + 8));
                        _rate = (int)ScummHelper.SwapBytes(BitConverter.ToUInt32(data, i + 12));
                        _channels = (int)ScummHelper.SwapBytes(BitConverter.ToUInt32(data, i + 16));
                        Debug.Assert(_channels == 1 || _channels == 2);
                        break;
                    case "TEXT":
                        // Ignore this
                        break;
                    case "REGN":
//                        if (subSize != 8)
//                            Console.Error.WriteLine("invalid size for REGN Chunk");
                        break;
                    case "STOP":
//                        if (subSize != 4)
//                            Console.Error.WriteLine("invalid size for STOP Chunk");
                        break;
//                    default:
//                        Console.Error.WriteLine("Unknown iMUS subChunk found : {0}, {1}", subType, subSize);
//                        break;
                }

                i += (int)subSize;
                size -= subSize;
            }
            return true;
        }

        void Decode()
        {
            int remaining_size = _sbufferSize % 3;
            if (remaining_size != 0)
            {
                _srbufferSize -= remaining_size;
                Debug.Assert(_inData);
                if (_tbuffer.Length == 0)
                {
                    _tbuffer = new byte[remaining_size];
                    Array.Copy(_sbuffer, _sbufferSize - remaining_size, _tbuffer, 0, remaining_size);
                    _tbufferSize = remaining_size;
                    _sbufferSize -= remaining_size;
                }
                else
                {
                    Debug.WriteLine("impossible ! : {0}, {1}, {2}, {3}({4}), {5}({6}, {7})",
                        this, _dataSize, _inData, _tbuffer, _tbufferSize, _sbuffer, _sbufferSize, _srbufferSize);
                    var old = _tbuffer;
                    int new_size = remaining_size + _tbufferSize;
                    _tbuffer = new byte[new_size];
                    Array.Copy(old, _tbuffer, _tbufferSize);
                    Array.Copy(_sbuffer, _sbufferSize - remaining_size, _tbuffer, _tbufferSize, remaining_size);
                    _tbufferSize += remaining_size;
                }
            }

            byte[] keep;
            _sbufferSize = BundleCodecs.Decode12BitsSample(_sbuffer, out keep, _sbufferSize);
            _sbuffer = keep;
        }

        int _srbufferSize;
        /// <summary>
        /// the bitsize of the original data
        /// </summary>
        int _bitsize;
        /// <summary>
        /// the sampling rate of the original data.
        /// </summary>
        int _rate;
        /// <summary>
        /// the number of channels of the original data
        /// </summary>
        int _channels;
    }
}

