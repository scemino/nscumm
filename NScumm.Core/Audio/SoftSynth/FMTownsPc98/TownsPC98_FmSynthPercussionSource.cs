//
//  TownsPC98_FmSynthPercussionSource.cs
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

namespace NScumm.Core.Audio
{
    public class TownsPC98_FmSynthPercussionSource
    {
        public TownsPC98_FmSynthPercussionSource(uint timerbase, uint rtt)
        {
            _rtt = rtt;
            _tickLength = timerbase * 2;
            _volumeA = Mixer.MaxMixerVolume;
            _volumeB = Mixer.MaxMixerVolume;

            _reg = new Action<byte>[40];

            _reg[16] = value => _rhChan[0].startPosL = value;
            _reg[17] = value => _rhChan[1].startPosL = value;
            _reg[18] = value => _rhChan[2].startPosL = value;
            _reg[19] = value => _rhChan[3].startPosL = value;
            _reg[20] = value => _rhChan[4].startPosL = value;
            _reg[21] = value => _rhChan[5].startPosL = value;
            _reg[22] = value => _rhChan[0].startPosH = value;
            _reg[23] = value => _rhChan[1].startPosH = value;
            _reg[24] = value => _rhChan[2].startPosH = value;
            _reg[25] = value => _rhChan[3].startPosH = value;
            _reg[26] = value => _rhChan[4].startPosH = value;
            _reg[27] = value => _rhChan[5].startPosH = value;
            _reg[28] = value => _rhChan[0].endPosL = value;
            _reg[29] = value => _rhChan[1].endPosL = value;
            _reg[30] = value => _rhChan[2].endPosL = value;
            _reg[31] = value => _rhChan[3].endPosL = value;
            _reg[32] = value => _rhChan[4].endPosL = value;
            _reg[33] = value => _rhChan[5].endPosL = value;
            _reg[34] = value => _rhChan[0].endPosH = value;
            _reg[35] = value => _rhChan[1].endPosH = value;
            _reg[36] = value => _rhChan[2].endPosH = value;
            _reg[37] = value => _rhChan[3].endPosH = value;
            _reg[38] = value => _rhChan[4].endPosH = value;
            _reg[39] = value => _rhChan[5].endPosH = value;
        }

        public void Init(byte[] instrData)
        {
            if (_ready)
            {
                Reset();
                return;
            }

            var pos = 0;

            if (instrData != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    _rhChan[i].data = instrData;
                    _rhChan[i].dataOffset = instrData.ToUInt16BigEndian(pos);
                    pos += 2;
                    _rhChan[i].size = instrData.ToUInt16BigEndian(pos);
                    pos += 2;
                }
                Reset();
                _ready = true;
            }
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    _rhChan[i] = new RhtChannel();
                }
                _ready = false;
            }
        }

        public void Reset()
        {
            _timer = 0;
            _totalLevel = 63;

            for (int i = 0; i < 6; i++)
            {
                var s = _rhChan[i];
                s.posOffset = s.startOffset = s.dataOffset;
                s.endOffset = (int)s.size + s.dataOffset;
                s.active = false;
                s.level = 0;
                s.@out = 0;
                s.decStep = 1;
                s.decState = 0;
                s.samples[0] = s.samples[1] = 0;
                s.startPosH = s.startPosL = s.endPosH = s.endPosL = 0;
            }
        }

        public void WriteReg(byte address, byte value)
        {
            if (!_ready)
                return;

            byte h = (byte)(address >> 4);
            byte l = (byte)(address & 15);

            if (address > 15)
                _reg[address](value);

            if (address == 0)
            {
                if ((value & 0x80) != 0)
                {
                    //key off
                    for (int i = 0; i < 6; i++)
                    {
                        if (((value >> i) & 1) != 0)
                            _rhChan[i].active = false;
                    }
                }
                else
                {
                    //key on
                    for (int i = 0; i < 6; i++)
                    {
                        if (((value >> i) & 1) != 0)
                        {
                            RhtChannel s = _rhChan[i];
                            s.posOffset = s.startOffset;
                            s.active = true;
                            s.@out = 0;
                            s.samples[0] = s.samples[1] = 0;
                            s.decStep = 1;
                            s.decState = 0;
                        }
                    }
                }
            }
            else if (address == 1)
            {
                // total level
                _totalLevel = (byte)((value & 63) ^ 63);
                for (int i = 0; i < 6; i++)
                    RecalcOuput(_rhChan[i]);
            }
            else if (h == 0 && ((l & 8) != 0))
            {
                // instrument level
                l &= 7;
                _rhChan[l].level = (byte)((value & 0x1f) ^ 0x1f);
                RecalcOuput(_rhChan[l]);
            }
            else if ((h & 3) != 0)
            {
                l &= 7;
                if (h == 1)
                {
                    // set start offset
                    _rhChan[l].startOffset = _rhChan[l].dataOffset + ((_rhChan[l].startPosH << 8 | _rhChan[l].startPosL) << 8);
                }
                else if (h == 2)
                {
                    // set end offset
                    _rhChan[l].endOffset = _rhChan[l].dataOffset + ((_rhChan[l].endPosH << 8 | _rhChan[l].endPosL) << 8) + 255;
                }
            }
        }

        public void NextTick(int[] buffer, int offset, int bufferSize)
        {
            if (!_ready)
                return;

            for (var i = 0; i < bufferSize; i++)
            {
                _timer += _tickLength;
                while (_timer > _rtt)
                {
                    _timer -= _rtt;

                    for (int ii = 0; ii < 6; ii++)
                    {
                        RhtChannel s = _rhChan[ii];
                        if (s.active)
                        {
                            RecalcOuput(s);
                            if (s.decStep != 0)
                            {
                                AdvanceInput(s);
                                if (s.posOffset == s.endOffset)
                                    s.active = false;
                            }
                            s.decStep ^= 1;
                        }
                    }
                }

                int finOut = 0;

                for (int ii = 0; ii < 6; ii++)
                {
                    if (_rhChan[ii].active)
                        finOut += _rhChan[ii].@out;
                }

                finOut <<= 1;

                if ((1 & _volMaskA) != 0)
                    finOut = (finOut * _volumeA) / Mixer.MaxMixerVolume;

                if ((1 & _volMaskB) != 0)
                    finOut = (finOut * _volumeB) / Mixer.MaxMixerVolume;

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

        void RecalcOuput(RhtChannel ins)
        {
            int s = _totalLevel + ins.level;
            int x = s > 62 ? 0 : (1 + (s >> 3));
            int y = s > 62 ? 0 : (15 - (s & 7));
            ins.@out = ((ins.samples[ins.decStep] * y) >> x) & ~3;
        }

        void AdvanceInput(RhtChannel ins)
        {
            sbyte cur = (sbyte)ins.data[ins.posOffset++];

            for (int i = 0; i < 2; i++)
            {
                int b = (2 * (cur & 7) + 1) * stepTable[ins.decState] / 8;
                ins.samples[i] = (short)ScummHelper.Clip(ins.samples[i ^ 1] + ((cur & 8) != 0 ? b : -b), -2048, 2047);
                ins.decState = (sbyte)ScummHelper.Clip(ins.decState + adjustIndex[cur & 7], 0, 48);
                cur >>= 4;
            }
        }

        static readonly sbyte[] adjustIndex = { -1, -1, -1, -1, 2, 5, 7, 9 };
        static readonly short[] stepTable =
            {
                16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45, 50, 55,
                60, 66, 73, 80, 88, 97, 107, 118, 130, 143, 157, 173, 190, 209, 230, 253, 279, 307, 337,
                371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552
            };

        class RhtChannel
        {
            public byte[] data;
            public int dataOffset;

            public int startOffset;
            public int endOffset;
            public int posOffset;
            public uint size;
            public bool active;
            public byte level;

            public sbyte decState;
            public byte decStep;

            public short[] samples = new short[2];
            public int @out;

            public byte startPosH;
            public byte startPosL;
            public byte endPosH;
            public byte endPosL;
        }

        RhtChannel[] _rhChan = new RhtChannel[6];

        byte _totalLevel;

        readonly uint _tickLength;
        uint _timer;
        readonly uint _rtt;

        Action<byte>[] _reg;

        ushort _volumeA;
        ushort _volumeB;
        int _volMaskA;
        int _volMaskB;

        bool _ready;
    }
}

