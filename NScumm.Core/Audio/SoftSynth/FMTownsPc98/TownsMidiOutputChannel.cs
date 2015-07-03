//
//  TownsMidiOutputChannel.cs
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


namespace NScumm.Core.Audio.SoftSynth
{
    enum CheckPriorityStatus
    {
        Disconnected = -2,
        HighPriority = -1
    }

    class EffectEnvelope
    {
        public EnvelopeState state;
        public int currentLevel;
        public int duration;
        public int maxLevel;
        public int startLevel;
        public byte loop;
        public byte[] stateTargetLevels = new byte[4];
        public byte[] stateModWheelLevels = new byte[4];
        public sbyte modWheelSensitivity;
        public sbyte modWheelState;
        public sbyte modWheelLast;
        public ushort numSteps;
        public uint stepCounter;
        public int incrPerStep;
        public sbyte dir;
        public uint incrPerStepRem;
        public uint incrCountRem;
    }

    class TownsMidiOutputChannel
    {
        public TownsMidiOutputChannel(MidiDriver_TOWNS driver, int chanIndex)
        {
            _driver = driver;
            _chan = (byte)chanIndex;

            _effectEnvelopes = new EffectEnvelope[2];
            for (int i = 0; i < _effectEnvelopes.Length; i++)
            {
                _effectEnvelopes[i] = new EffectEnvelope();
            }
            _effectDefs = new EffectDef[2];
            for (int i = 0; i < _effectEnvelopes.Length; i++)
            {
                _effectDefs[i] = new EffectDef();
            }

            _effectDefs[0].s = _effectEnvelopes[1];
            _effectDefs[1].s = _effectEnvelopes[0];
        }

        public void NoteOn(byte msb, ushort lsb)
        {
            _freq = (ushort)((msb << 7) + lsb);
            _freqAdjust = 0;
            KeyOnSetFreq(_freq);
        }

        public void NoteOnPitchBend(byte msb, ushort lsb)
        {
            _freq = (ushort)((msb << 7) + lsb);
            KeyOnSetFreq((ushort)(_freq + _freqAdjust));
        }

        public void SetupProgram(byte[] data, byte mLevelPara, byte tLevelPara)
        {
            // This driver uses only 2 operators and 2 algorithms (algorithm 5 and 7),
            // since it is just a modified AdLib driver. It also uses AdLib programs.
            // There are no FM-TOWNS specific programs. This is the reason for the low quality of the FM-TOWNS
            // music (unsuitable data is just forced into the wrong audio device).

            byte[] mul = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 10, 12, 12, 15, 15 };
            byte chan = _chanMap[_chan];

            byte mulAmsFms1 = _driver._chanState[chan].mulAmsFms = data[0];
            byte tl1 = _driver._chanState[chan].tl = (byte)((data[1] | 0x3f) - mLevelPara);
            byte attDec1 = _driver._chanState[chan].attDec = (byte)~data[2];
            byte sus1 = _driver._chanState[chan].sus = (byte)~data[3];
            _driver._chanState[chan].unk2 = data[4];
            chan += 3;

            @out(0x30, mul[mulAmsFms1 & 0x0f]);
            @out(0x40, (byte)((tl1 & 0x3f) + 15));
            @out(0x50, (byte)(((attDec1 >> 4) << 1) | ((attDec1 >> 4) & 1)));
            @out(0x60, (byte)(((attDec1 << 1) | (attDec1 & 1)) & 0x1f));
            @out(0x70, (byte)((((mulAmsFms1 & 0x20) ^ 0x20) != 0) ? (((sus1 & 0x0f) << 1) | 1) : 0));
            @out(0x80, sus1);

            byte mulAmsFms2 = _driver._chanState[chan].mulAmsFms = data[5];
            byte tl2 = _driver._chanState[chan].tl = (byte)((data[6] | 0x3f) - tLevelPara);
            byte attDec2 = _driver._chanState[chan].attDec = (byte)~data[7];
            byte sus2 = _driver._chanState[chan].sus = (byte)~data[8];
            _driver._chanState[chan].unk2 = data[9];

            byte mul2 = mul[mulAmsFms2 & 0x0f];
            tl2 = (byte)((tl2 & 0x3f) + 15);
            byte ar2 = (byte)(((attDec2 >> 4) << 1) | ((attDec2 >> 4) & 1));
            byte dec2 = (byte)(((attDec2 << 1) | (attDec2 & 1)) & 0x1f);
            byte sus2r = (byte)((((mulAmsFms2 & 0x20) ^ 0x20) != 0) ? (((sus2 & 0x0f) << 1) | 1) : 0);

            for (int i = 4; i < 16; i += 4)
            {
                @out((byte)(0x30 + i), mul2);
                @out((byte)(0x40 + i), tl2);
                @out((byte)(0x50 + i), ar2);
                @out((byte)(0x60 + i), dec2);
                @out((byte)(0x70 + i), sus2r);
                @out((byte)(0x80 + i), sus2);
            }

            _driver._chanState[chan].fgAlg = data[10];

            byte alg = (byte)(5 + 2 * (data[10] & 1));
            byte fb = (byte)(4 * (data[10] & 0x0e));
            @out(0xb0, (byte)(fb | alg));
            byte t = (byte)(mulAmsFms1 | mulAmsFms2);
            @out(0xb4, (byte)(0xc0 | ((t & 0x80) >> 3) | ((t & 0x40) >> 5)));
        }

        public void SetupEffects(int index, byte flags, byte[] effectData, int offset)
        {
            ushort[] effectMaxLevel = { 0x2FF, 0x1F, 0x07, 0x3F, 0x0F, 0x0F, 0x0F, 0x03, 0x3F, 0x0F, 0x0F, 0x0F, 0x03, 0x3E, 0x1F };
            byte[] effectType = { 0x1D, 0x1C, 0x1B, 0x00, 0x03, 0x04, 0x07, 0x08, 0x0D, 0x10, 0x11, 0x14, 0x15, 0x1e, 0x1f, 0x00 };

            EffectEnvelope s = _effectEnvelopes[index];
            EffectDef d = _effectDefs[index];

            d.phase = 0;
            d.useModWheel = (byte)(flags & 0x40);
            s.loop = (byte)(flags & 0x20);
            d.loopRefresh = (byte)(flags & 0x10);
            d.type = effectType[flags & 0x0f];
            s.maxLevel = effectMaxLevel[flags & 0x0f];
            s.modWheelSensitivity = 31;
            s.modWheelState = (sbyte)((d.useModWheel != 0) ? _in._modWheel >> 2 : 31);

            switch (d.type)
            {
                case 0:
                    s.startLevel = _operator2Tl;
                    break;
                case 13:
                    s.startLevel = _operator1Tl;
                    break;
                case 30:
                    s.startLevel = 31;
                    d.s.modWheelState = 0;
                    break;
                case 31:
                    s.startLevel = 0;
                    d.s.modWheelSensitivity = 0;
                    break;
                default:
                    s.startLevel = GetEffectStartLevel(d.type);
                    break;
            }

            StartEffect(s, effectData, offset);
        }

        public void SetModWheel(byte value)
        {
            if (_effectEnvelopes[0].state != EnvelopeState.Ready && _effectDefs[0].type != 0)
                _effectEnvelopes[0].modWheelState = (sbyte)(value >> 2);

            if (_effectEnvelopes[1].state != EnvelopeState.Ready && _effectDefs[1].type != 0)
                _effectEnvelopes[1].modWheelState = (sbyte)(value >> 2);
        }

        public void Connect(TownsMidiInputChannel chan)
        {
            if (chan == null)
                return;

            _in = chan;
            _next = chan._out;
            _prev = null;
            chan._out = this;
            if (_next != null)
                _next._prev = this;
        }

        public void Disconnect()
        {
            KeyOff();

            TownsMidiOutputChannel p = _prev;
            TownsMidiOutputChannel n = _next;

            if (n != null)
                n._prev = p;
            if (p != null)
                p._next = n;
            else
                _in._out = n;
            _in = null;
        }

        public bool Update()
        {
            if (_in == null)
                return false;

            if (_duration != 0)
            {
                _duration -= 17;
                if (_duration <= 0)
                {
                    Disconnect();
                    return true;
                }
            }

            for (int i = 0; i < 2; i++)
            {
                if (_effectEnvelopes[i].state != EnvelopeState.Ready)
                    UpdateEffectGenerator(_effectEnvelopes[i], _effectDefs[i]);
            }

            return false;
        }

        public int CheckPriority(int pri)
        {
            if (_in == null)
                return (int)CheckPriorityStatus.Disconnected;

            if (_next == null && pri >= _in._priority)
                return _in._priority;

            return (int)CheckPriorityStatus.HighPriority;
        }

        void StartEffect(EffectEnvelope s, byte[] effectData, int offset)
        {
            s.state = EnvelopeState.Attacking;
            s.currentLevel = 0;
            s.modWheelLast = 31;
            s.duration = effectData[offset] * 63;
            s.stateTargetLevels[0] = effectData[offset + 1];
            s.stateTargetLevels[1] = effectData[offset + 3];
            s.stateTargetLevels[2] = effectData[offset + 5];
            s.stateTargetLevels[3] = effectData[offset + 6];
            s.stateModWheelLevels[0] = effectData[offset + 2];
            s.stateModWheelLevels[1] = effectData[offset + 4];
            s.stateModWheelLevels[2] = 0;
            s.stateModWheelLevels[3] = effectData[offset + 7];
            InitNextEnvelopeState(s);
        }

        void UpdateEffectGenerator(EffectEnvelope s, EffectDef d)
        {
            byte f = (byte)AdvanceEffectEnvelope(s, d);

            if ((f & 1) != 0)
            {
                switch (d.type)
                {
                    case 0:
                        _operator2Tl = (byte)(s.startLevel + d.phase);
                        break;
                    case 13:
                        _operator1Tl = (byte)(s.startLevel + d.phase);
                        break;
                    case 30:
                        d.s.modWheelState = (sbyte)d.phase;
                        break;
                    case 31:
                        d.s.modWheelSensitivity = (sbyte)d.phase;
                        break;
                }
            }

            if ((f & 2) != 0)
            {
                if (d.loopRefresh != 0)
                    KeyOn();
            }
        }

        int AdvanceEffectEnvelope(EffectEnvelope s, EffectDef d)
        {
            if (s.duration != 0)
            {
                s.duration -= 17;
                if (s.duration <= 0)
                {
                    s.state = EnvelopeState.Ready;
                    return 0;
                }
            }

            int t = s.currentLevel + s.incrPerStep;

            s.incrCountRem += s.incrPerStepRem;
            if (s.incrCountRem >= s.numSteps)
            {
                s.incrCountRem -= s.numSteps;
                t += s.dir;
            }

            int retFlags = 0;

            if (t != s.currentLevel || (s.modWheelState != s.modWheelLast))
            {
                s.currentLevel = t;
                s.modWheelLast = s.modWheelState;
                t = GetEffectModLevel(t, s.modWheelState);
                if (t != d.phase)
                {
                    d.phase = t;
                    retFlags |= 1;
                }
            }

            if ((--s.stepCounter) != 0)
                return retFlags;

            if (++s.state > EnvelopeState.Releasing)
            {
                if (s.loop == 0)
                {
                    s.state = EnvelopeState.Ready;
                    return retFlags;
                }
                s.state = EnvelopeState.Attacking;
                retFlags |= 2;
            }

            InitNextEnvelopeState(s);

            return retFlags;
        }

        void InitNextEnvelopeState(EffectEnvelope s)
        {
            byte v = s.stateTargetLevels[(int)s.state - 1];
            int e = _effectEnvStepTable[_driver._operatorLevelTable[((v & 0x7f) << 5) + s.modWheelSensitivity]];

            if ((v & 0x80) != 0)
                e = _driver.RandomValue(e);

            if (e == 0)
                e = 1;

            s.numSteps = (ushort)(s.stepCounter = (uint)e);
            int d = 0;

            if (s.state != EnvelopeState.Sustaining)
            {
                v = s.stateModWheelLevels[(int)s.state - 1];
                e = GetEffectModLevel(s.maxLevel, (v & 0x7f) - 31);

                if ((v & 0x80) != 0)
                    e = _driver.RandomValue(e);

                if (e + s.startLevel > s.maxLevel)
                {
                    e = s.maxLevel - s.startLevel;
                }
                else
                {
                    if (e + s.startLevel < 0)
                        e = -s.startLevel;
                }

                d = e - s.currentLevel;
            }

            s.incrPerStep = d / s.numSteps;
            s.dir = (sbyte)((d < 0) ? -1 : 1);
            d *= s.dir;
            s.incrPerStepRem = (uint)(d % s.numSteps);
            s.incrCountRem = 0;
        }

        short GetEffectStartLevel(byte type)
        {
            byte chan = (type < 13) ? _chanMap2[_chan] : ((type < 26) ? _chanMap[_chan] : _chan);

            if (type == 28)
                return 15;
            else if (type == 29)
                return 383;
            else if (type > 29)
                return 0;
            else if (type > 12)
                type -= 13;

            var defOff = type << 2;
            byte res = (byte)((_driver._chanState[chan].get(_effectDefaults[defOff + 0] >> 5) & _effectDefaults[defOff + 2]) >> _effectDefaults[defOff + 1]);
            if (_effectDefaults[defOff + 3] != 0)
                res = (byte)(_effectDefaults[defOff + 3] - res);

            return res;
        }

        int GetEffectModLevel(int lvl, int mod)
        {
            if (mod == 0)
                return 0;

            if (mod == 31)
                return lvl;

            if (lvl > 63 || lvl < -63)
                return ((lvl + 1) * mod) >> 5;

            if (mod < 0)
            {
                if (lvl < 0)
                    return _driver._operatorLevelTable[((-lvl) << 5) - mod];
                else
                    return -_driver._operatorLevelTable[(lvl << 5) - mod];
            }
            else
            {
                if (lvl < 0)
                    return -_driver._operatorLevelTable[((-lvl) << 5) + mod];
                else
                    return _driver._operatorLevelTable[(lvl << 5) + mod];
            }
        }

        void KeyOn()
        {
            @out(0x28, 0x30);
        }

        void KeyOff()
        {
            @out(0x28, 0);
        }

        void KeyOnSetFreq(ushort frq)
        {
            ushort note = (ushort)((frq << 1) >> 8);
            frq = (ushort)((_freqMSB[note] << 11) | _freqLSB[note]);
            @out(0xa4, (byte)(frq >> 8));
            @out(0xa0, (byte)(frq & 0xff));
            //out(0x28, 0x00);
            @out(0x28, 0x30);
        }

        void @out(byte reg, byte val)
        {
            byte[] chanRegOffs = { 0, 1, 2, 0, 1, 2 };
            byte[] keyValOffs = { 0, 1, 2, 4, 5, 6 };

            if (reg == 0x28)
                val = (byte)((val & 0xf0) | keyValOffs[_chan]);
            if (reg < 0x30)
                _driver._intf.Callback(17, 0, (int)reg, (int)val);
            else
                _driver._intf.Callback(17, _chan / 3, (reg & ~3) | chanRegOffs[_chan], (int)val);
        }

        internal EffectEnvelope[] _effectEnvelopes;

        class EffectDef
        {
            public int phase;
            public byte type;
            public byte useModWheel;
            public byte loopRefresh;
            public EffectEnvelope s;
        }

        EffectDef[] _effectDefs;

        internal TownsMidiInputChannel _in;
        internal TownsMidiOutputChannel _prev;
        internal TownsMidiOutputChannel _next;
        internal byte _adjustModTl;
        byte _chan;
        internal byte _note;
        internal byte _operator2Tl;
        internal byte _operator1Tl;
        internal byte _sustainNoteOff;
        internal short _duration;

        ushort _freq;
        short _freqAdjust;

        MidiDriver_TOWNS _driver;

        static readonly byte[] _chanMap =
            {
                0, 1, 2, 8, 9, 10
            };

        static readonly byte[] _chanMap2 =
            {
                3, 4, 5, 11, 12, 13
            };


        static readonly ushort[] _effectEnvStepTable =
            {
                0x0001, 0x0002, 0x0004, 0x0005, 0x0006, 0x0007, 0x0008, 0x0009,
                0x000A, 0x000C, 0x000E, 0x0010, 0x0012, 0x0015, 0x0018, 0x001E,
                0x0024, 0x0032, 0x0040, 0x0052, 0x0064, 0x0088, 0x00A0, 0x00C0,
                0x00F0, 0x0114, 0x0154, 0x01CC, 0x0258, 0x035C, 0x04B0, 0x0640
            };

        static readonly byte[] _effectDefaults =
            {
                0x40, 0x00, 0x3F, 0x3F, 0xE0, 0x02, 0x00, 0x00, 0x40, 0x06, 0xC0, 0x00,
                0x20, 0x00, 0x0F, 0x00, 0x60, 0x04, 0xF0, 0x0F, 0x60, 0x00, 0x0F, 0x0F,
                0x80, 0x04, 0xF0, 0x0F, 0x80, 0x00, 0x0F, 0x0F, 0xE0, 0x00, 0x03, 0x00,
                0x20, 0x07, 0x80, 0x00, 0x20, 0x06, 0x40, 0x00, 0x20, 0x05, 0x20, 0x00,
                0x20, 0x04, 0x10, 0x00, 0xC0, 0x00, 0x01, 0x00, 0xC0, 0x01, 0x0E, 0x00
            };

        static readonly byte[] _freqMSB =
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,
            0x02, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x03, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x80, 0x81, 0x83, 0x85,
            0x87, 0x88, 0x8A, 0x8C, 0x8E, 0x8F, 0x91, 0x93, 0x95, 0x96, 0x98, 0x9A,
            0x9C, 0x9E, 0x9F, 0xA1, 0xA3, 0xA5, 0xA6, 0xA8, 0xAA, 0xAC, 0xAD, 0xAF,
            0xB1, 0xB3, 0xB4, 0xB6, 0xB8, 0xBA, 0xBC, 0xBD, 0xBF, 0xC1, 0xC3, 0xC4,
            0xC6, 0xC8, 0xCA, 0xCB, 0xCD, 0xCF, 0xD1, 0xD2, 0xD4, 0xD6, 0xD8, 0xDA,
            0xDB, 0xDD, 0xDF, 0xE1, 0xE2, 0xE4, 0xE6, 0xE8, 0xE9, 0xEB, 0xED, 0xEF
        };

        static readonly ushort[] _freqLSB =
        {
            0x02D6, 0x02D6, 0x02D6, 0x02D6, 0x02D6, 0x02D6, 0x02D6, 0x02D6,
            0x02D6, 0x02D6, 0x02D6, 0x02D6, 0x02D6, 0x02D6, 0x0301, 0x032F,
            0x0360, 0x0393, 0x03C9, 0x0403, 0x0440, 0x0481, 0x04C6, 0x050E,
            0x055B, 0x02D6, 0x0301, 0x032F, 0x0360, 0x0393, 0x03C9, 0x0403,
            0x0440, 0x0481, 0x04C6, 0x050E, 0x055B, 0x02D6, 0x0301, 0x032F,
            0x0360, 0x0393, 0x03C9, 0x0403, 0x0440, 0x0481, 0x04C6, 0x050E,
            0x055B, 0x02D6, 0x0301, 0x032F, 0x0360, 0x0393, 0x03C9, 0x0403,
            0x0440, 0x0481, 0x04C6, 0x050E, 0x055B, 0x02D6, 0x0301, 0x032F,
            0x0360, 0x0393, 0x03C9, 0x0403, 0x0440, 0x0481, 0x04C6, 0x050E,
            0x055B, 0x02D6, 0x0301, 0x032F, 0x0360, 0x0393, 0x03C9, 0x0403,
            0x0440, 0x0481, 0x04C6, 0x050E, 0x055B, 0x02D6, 0x0301, 0x032F,
            0x0360, 0x0393, 0x03C9, 0x0403, 0x0440, 0x0481, 0x04C6, 0x050E,
            0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B,
            0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B,
            0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B,
            0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B, 0x055B
        };
    }
}
