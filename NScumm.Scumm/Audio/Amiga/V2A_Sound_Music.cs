//
//  V2A_Sound_Music.cs
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

namespace NScumm.Core.Audio
{
    // plays a music track
    class V2A_Sound_Music : IV2A_Sound
    {
        int Id { get; set; }

        IPlayerMod Player { get; set; }

        public V2A_Sound_Music(ushort instoff, ushort voloff, ushort chan1off, ushort chan2off, ushort chan3off, ushort chan4off, ushort sampoff, bool looped)
        {
            _instoff = instoff;
            _voloff = voloff;
            _chan1off = chan1off;
            _chan2off = chan2off;
            _chan3off = chan3off;
            _chan4off = chan4off;
            _sampoff = sampoff;
            _looped = looped;
        }

        public void Start(IPlayerMod mod, int id, byte[] data)
        {
            Player = mod;
            Id = id;

            _data = new byte[BitConverter.ToUInt16(data, 0)];
            Array.Copy(data, _data, _data.Length);

            _chan[0].dataptr_i = _chan1off;
            _chan[1].dataptr_i = _chan2off;
            _chan[2].dataptr_i = _chan3off;
            _chan[3].dataptr_i = _chan4off;
            for (int i = 0; i < 4; i++)
            {
                _chan[i].dataptr = _chan[i].dataptr_i;
                _chan[i].volbase = 0;
                _chan[i].volptr = 0;
                _chan[i].chan = 0;
                _chan[i].dur = 0;
                _chan[i].ticks = 0;
            }
            Update();
        }

        public bool Update()
        {
            Debug.Assert(Id != 0);
            int i, j = 0;
            for (i = 0; i < 4; i++)
            {
                if (_chan[i].dur != 0)
                {
                    if (--_chan[i].dur == 0)
                    {
                        Player.StopChannel(Id | (_chan[i].chan << 8));
                    }
                    else
                    {
                        Player.SetChannelVol(Id | (_chan[i].chan << 8), _data.ToUInt16BigEndian(_chan[i].volbase + (_chan[i].volptr++ << 1)));
                        if (_chan[i].volptr == 0)
                        {
                            Player.StopChannel(Id | (_chan[i].chan << 8));
                            _chan[i].dur = 0;
                        }
                    }
                }
                if (_chan[i].dataptr == 0)
                {
                    j++;
                    continue;
                }
                if (_data.ToUInt16BigEndian(_chan[i].dataptr) <= _chan[i].ticks)
                {
                    if (_data.ToUInt16BigEndian(_chan[i].dataptr + 2) == 0xFFFF)
                    {
                        if (_looped)
                        {
                            _chan[i].dataptr = _chan[i].dataptr_i;
                            _chan[i].ticks = 0;
                            if (_data.ToUInt16BigEndian(_chan[i].dataptr) > 0)
                            {
                                _chan[i].ticks++;
                                continue;
                            }
                        }
                        else
                        {
                            _chan[i].dataptr = 0;
                            j++;
                            continue;
                        }
                    }
                    int freq = V2A_Sound_Base.BASE_FREQUENCY / _data.ToUInt16BigEndian(_chan[i].dataptr + 2);
                    int inst = _data.ToUInt16BigEndian(_chan[i].dataptr + 8);
                    _chan[i].volbase = (ushort)(_voloff + (_data.ToUInt16BigEndian(_instoff + (inst << 5)) << 9));
                    _chan[i].volptr = 0;
                    _chan[i].chan = (ushort)(_data.ToUInt16BigEndian(_chan[i].dataptr + 6) & 0x3);

                    if (_chan[i].dur != 0) // if there's something playing, stop it
                        Player.StopChannel(Id | (_chan[i].chan << 8));

                    _chan[i].dur = _data.ToUInt16BigEndian(_chan[i].dataptr + 4);

                    int vol = _data.ToUInt16BigEndian(_chan[i].volbase + (_chan[i].volptr++ << 1));

                    int pan;
                    if ((_chan[i].chan == 0) || (_chan[i].chan == 3))
                        pan = -127;
                    else
                        pan = 127;
                    int offset = _data.ToUInt16BigEndian(_instoff + (inst << 5) + 0x14);
                    int len = _data.ToUInt16BigEndian(_instoff + (inst << 5) + 0x18);
                    int loopoffset = _data.ToUInt16BigEndian(_instoff + (inst << 5) + 0x16);
                    int looplen = _data.ToUInt16BigEndian(_instoff + (inst << 5) + 0x10);

                    int size = len + looplen;
                    var data = new byte[size];
                    Array.Copy(_data, _sampoff + offset, data, 0, len);
                    Array.Copy(_data, _sampoff + loopoffset, data, len, looplen);

                    Player.StartChannel(Id | (_chan[i].chan << 8), data, size, freq, vol, len, looplen + len, pan);
                    _chan[i].dataptr += 16;
                }
                _chan[i].ticks++;
            }
            if (j == 4)
                return false;
            return true;
        }

        public void Stop()
        {
            Debug.Assert(Id != 0);
            for (int i = 0; i < 4; i++)
            {
                if (_chan[i].dur != 0)
                    Player.StopChannel(Id | (_chan[i].chan << 8));
            }
            _data = null;
            Id = 0;
        }

        readonly ushort _instoff;
        readonly ushort _voloff;
        readonly ushort _chan1off;
        readonly ushort _chan2off;
        readonly ushort _chan3off;
        readonly ushort _chan4off;
        readonly ushort _sampoff;
        readonly bool _looped;

        byte[] _data;

        struct tchan
        {
            public ushort dataptr_i;
            public ushort dataptr;
            public ushort volbase;
            public byte volptr;
            public ushort chan;
            public ushort dur;
            public ushort ticks;
        }

        tchan[] _chan = new tchan[4];
    }
    
}
