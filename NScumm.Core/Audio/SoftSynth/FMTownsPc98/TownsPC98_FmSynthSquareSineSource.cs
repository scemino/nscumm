//
//  TownsPC98_FmSynthSquareSineSource.cs
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

namespace NScumm.Core.Audio.SoftSynth
{

    class TownsPC98_FmSynthSquareSineSource
    {
        public TownsPC98_FmSynthSquareSineSource(uint timerbase, uint rtt)
        {
            _rtt = rtt;
            _updateRequest = -1;
            _tickLength = timerbase * 27;
            _rand = 1;
            _outN = 1;
            _evpTimer = 0x1f;
            _pReslt = 0x1f;
            _evpUpdate = true;
            _volumeA = Mixer.MaxMixerVolume;
            _volumeB = Mixer.MaxMixerVolume;

            _regOut = new Action<byte>[11];
            _regIn = new Func<byte>[11];

            _regIn[0] = () => _channels[0].frqL;
            _regIn[1] = () => _channels[0].frqH;
            _regIn[2] = () => _channels[1].frqL;
            _regIn[3] = () => _channels[1].frqH;
            _regIn[4] = () => _channels[2].frqL;
            _regIn[5] = () => _channels[2].frqH;
            _regIn[6] = () => _noiseGenerator;
            _regIn[7] = () => _chanEnable;
            _regIn[8] = () => _channels[0].vol;
            _regIn[9] = () => _channels[1].vol;
            _regIn[10] = () => _channels[2].vol;

            _regOut[0] = value => _channels[0].frqL = value;
            _regOut[1] = value => _channels[0].frqH = value;
            _regOut[2] = value => _channels[1].frqL = value;
            _regOut[3] = value => _channels[1].frqH = value;
            _regOut[4] = value => _channels[2].frqL = value;
            _regOut[5] = value => _channels[2].frqH = value;
            _regOut[6] = value => _noiseGenerator = value;
            _regOut[7] = value => _chanEnable = value;
            _regOut[8] = value => _channels[0].vol = value;
            _regOut[9] = value => _channels[1].vol = value;
            _regOut[10] = value => _channels[2].vol = value;

            Reset();
        }

        public void Init(int[] rsTable, int[] rseTable)
        {
            if (_ready)
            {
                Reset();
                return;
            }

            _tlTable = new int[16];
            _tleTable = new int[32];
            float a, b, d;
            d = 801.0f;

            for (int i = 0; i < 16; i++)
            {
                b = 1.0f / rsTable[i];
                a = 1.0f / d + b + 1.0f / 1000.0f;
                float v = (b / a) * 32767.0f;
                _tlTable[i] = (int)v;

                b = 1.0f / rseTable[i];
                a = 1.0f / d + b + 1.0f / 1000.0f;
                v = (b / a) * 32767.0f;
                _tleTable[i] = (int)v;
            }

            for (int i = 16; i < 32; i++)
            {
                b = 1.0f / rseTable[i];
                a = 1.0f / d + b + 1.0f / 1000.0f;
                float v = (b / a) * 32767.0f;
                _tleTable[i] = (int)v;
            }

            _ready = true;
        }

        public void Reset()
        {
            _rand = 1;
            _outN = 1;
            _updateRequest = -1;
            _nTick = _evpUpdateCnt = 0;
            _evpTimer = 0x1f;
            _pReslt = 0x1f;
            _attack = 0;
            _cont = false;
            _evpUpdate = true;
            _timer = 0;

            for (int i = 0; i < 3; i++)
            {
                _channels[i].tick = 0;
                _channels[i].smp = _channels[i].@out = 0;
            }

            for (int i = 0; i < 14; i++)
                WriteReg(i, 0, true);

            WriteReg(7, 0xbf, true);
        }

        public void WriteReg(int address, int value, bool force = false)
        {
            if (!_ready)
                return;

            if (address > 10 || _regIn[address]() == value)
            {
                if ((address == 11 || address == 12 || address == 13) && value != 0)
                    Debug.WriteLine("TownsPC98_FmSynthSquareSineSource: unsupported reg address: {0}", address);
                return;
            }

            if (!force)
            {
                if (_updateRequest >= 63)
                {
                    Debug.WriteLine("TownsPC98_FmSynthSquareSineSource: event buffer overflow");
                    _updateRequest = -1;
                }
                _updateRequestBuf[++_updateRequest] = (byte)value;
                _updateRequestBuf[++_updateRequest] = (byte)address;
                return;
            }

            _regOut[address]((byte)value);
        }

        public void NextTick(int[] buffer, int offset, uint bufferSize)
        {
            if (!_ready)
                return;

            for (uint i = 0; i < bufferSize; i++)
            {
                _timer += _tickLength;
                while (_timer > _rtt)
                {
                    _timer -= _rtt;

                    if (++_nTick >= (_noiseGenerator & 0x1f))
                    {
                        if (((_rand + 1) & 2) != 0)
                            _outN ^= 1;

                        _rand = (((_rand & 1) ^ ((_rand >> 3) & 1)) << 16) | (_rand >> 1);
                        _nTick = 0;
                    }

                    for (int ii = 0; ii < 3; ii++)
                    {
                        if (++_channels[ii].tick >= (((_channels[ii].frqH & 0x0f) << 8) | _channels[ii].frqL))
                        {
                            _channels[ii].tick = 0;
                            _channels[ii].smp ^= 1;
                        }
                        _channels[ii].@out = (byte)((_channels[ii].smp | ((_chanEnable >> ii) & 1)) & (_outN | ((_chanEnable >> (ii + 3)) & 1)));
                    }

                    if (_evpUpdate)
                    {
                        if (++_evpUpdateCnt >= 0)
                        {
                            _evpUpdateCnt = 0;

                            if (--_evpTimer < 0)
                            {
                                if (_cont)
                                {
                                    _evpTimer &= 0x1f;
                                }
                                else
                                {
                                    _evpUpdate = false;
                                    _evpTimer = 0;
                                }
                            }
                        }
                    }
                    _pReslt = (uint)(_evpTimer ^ _attack);
                    UpdateRegs();
                }

                int finOut = 0;
                for (int ii = 0; ii < 3; ii++)
                {
                    int finOutTemp = (((_channels[ii].vol >> 4) & 1) != 0) ? _tleTable[_channels[ii].@out != 0 ? _pReslt : 0] : _tlTable[_channels[ii].@out != 0 ? (_channels[ii].vol & 0x0f) : 0];

                    if (((1 << ii) & _volMaskA) != 0)
                        finOutTemp = (finOutTemp * _volumeA) / Mixer.MaxMixerVolume;

                    if (((1 << ii) & _volMaskB) != 0)
                        finOutTemp = (finOutTemp * _volumeB) / Mixer.MaxMixerVolume;

                    finOut += finOutTemp;
                }

                finOut /= 3;

                buffer[offset + (i << 1)] += finOut;
                buffer[offset + (i << 1) + 1] += finOut;
            }
        }

        public void SetVolumeIntern(int volA, int volB)
        {
            _volumeA = (ushort)volA;
            _volumeB = (ushort)volB;
        }

        public void SetVolumeChannelMasks(int channelMaskA, int channelMaskB)
        {
            _volMaskA = channelMaskA;
            _volMaskB = channelMaskB;
        }

        byte ChanEnable()
        {
            return _chanEnable;
        }

        void UpdateRegs()
        {
            for (int i = 0; i < _updateRequest;)
            {
                byte b = _updateRequestBuf[i++];
                byte a = _updateRequestBuf[i++];
                WriteReg(a, b, true);
            }
            _updateRequest = -1;
        }

        byte[] _updateRequestBuf = new byte[64];
        int _updateRequest;
        int _rand;

        sbyte _evpTimer;
        uint _pReslt;
        byte _attack;

        bool _evpUpdate, _cont;

        int _evpUpdateCnt;
        byte _outN;
        int _nTick;

        int[] _tlTable;
        int[] _tleTable;

        readonly uint _tickLength;
        uint _timer;
        readonly uint _rtt;

        class Channel
        {
            public int tick;
            public byte smp;
            public byte @out;

            public byte frqL;
            public byte frqH;
            public byte vol;
        }

        Channel[] _channels = CreateChannels();

        static Channel[] CreateChannels()
        {
            var channels = new Channel[3];
            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = new Channel();
            }
            return channels;
        }

        byte _noiseGenerator;
        byte _chanEnable;

        Action<byte>[] _regOut;
        Func<byte>[] _regIn;

        ushort _volumeA;
        ushort _volumeB;
        int _volMaskA;
        int _volMaskB;

        bool _ready;
    }
    
}
