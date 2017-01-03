//
//  DosBoxOPL_Chip.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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

namespace NScumm.Core.Audio.OPL.DosBox
{
    partial class DosBoxOPL
    {
        private class Chip
        {
            /// <summary>
            /// Gets the frequency scales for the different multiplications.
            /// </summary>
            /// <value>The freq mul.</value>
            public uint[] FreqMul => _freqMul;

            /// <summary>
            /// Gets the rates for decay and release for rate of this chip.
            /// </summary>
            /// <value>The linear rates.</value>
            public uint[] LinearRates => _linearRates;

            /// <summary>
            /// Gets the best match attack rates for the rate of this chip.
            /// </summary>
            /// <value>The attack rates.</value>
            public uint[] AttackRates => _attackRates;

            public Channel[] Channels => _chan;

            public byte Reg104 => _reg104;

            public byte Reg08 => _reg08;

            public byte RegBD => _regBd;

            public sbyte VibratoSign => _vibratoSign;

            public byte VibratoShift => _vibratoShift;

            public byte TremoloValue => _tremoloValue;

            public byte WaveFormMask => _waveFormMask;

            public sbyte Opl3Active => _opl3Active;

            public Chip()
            {
                _chan = new Channel[18];
                for (int i = 0; i < _chan.Length; i++)
                {
                    _chan[i] = new Channel(this, i);
                }
            }

            public uint ForwardNoise()
            {
                _noiseCounter += _noiseAdd;
                uint count = _noiseCounter >> LfoShift;
                _noiseCounter &= WaveMask;
                for (; count > 0; --count)
                {
                    //Noise calculation from mame
                    _noiseValue ^= (0x800302) & (0 - (_noiseValue & 1));
                    _noiseValue >>= 1;
                }
                return _noiseValue;
            }

            public void WriteReg(uint reg, byte val)
            {
                switch ((reg & 0xf0) >> 4)
                {
                    case 0x00 >> 4:
                        if (reg == 0x01)
                        {
                            _waveFormMask = (byte)(((val & 0x20) != 0) ? 0x7 : 0x0);
                        }
                        else if (reg == 0x104)
                        {
                            //Only detect changes in lowest 6 bits
                            if (0 == ((_reg104 ^ val) & 0x3f))
                                return;
                            //Always keep the highest bit enabled, for checking > 0x80
                            _reg104 = (byte)(0x80 | (val & 0x3f));
                        }
                        else if (reg == 0x105)
                        {
                            //MAME says the real opl3 doesn't reset anything on opl3 disable/enable till the next write in another register
                            if (0 == ((_opl3Active ^ val) & 1))
                                return;
                            _opl3Active = (sbyte)(((val & 1) != 0) ? 0xff : 0);
                            //Update the 0xc0 register for all channels to signal the switch to mono/stereo handlers
                            for (int i = 0; i < 18; i++)
                            {
                                _chan[i].ResetC0(this);
                            }
                        }
                        else if (reg == 0x08)
                        {
                            _reg08 = val;
                        }
                        break;
                    case 0x10 >> 4:
                        break;
                    case 0x20 >> 4:
                    case 0x30 >> 4:
                        RegOp(reg, op => op.Write20(this, val));
                        break;
                    case 0x40 >> 4:
                    case 0x50 >> 4:
                        RegOp(reg, op => op.Write40(this, val));
                        break;
                    case 0x60 >> 4:
                    case 0x70 >> 4:
                        RegOp(reg, op => op.Write60(this, val));
                        break;
                    case 0x80 >> 4:
                    case 0x90 >> 4:
                        RegOp(reg, op => op.Write80(this, val));
                        break;
                    case 0xa0 >> 4:
                        RegChan(reg, ch => ch.WriteA0(this, val));
                        break;
                    case 0xb0 >> 4:
                        if (reg == 0xbd)
                        {
                            WriteBd(val);
                        }
                        else
                        {
                            RegChan(reg, ch => ch.WriteB0(this, val));
                        }
                        break;
                    case 0xc0 >> 4:
                        RegChan(reg, ch => ch.WriteC0(this, val));
                        break;
                    case 0xd0 >> 4:
                        break;
                    case 0xe0 >> 4:
                    case 0xf0 >> 4:
                        RegOp(reg, op => op.WriteE0(this, val));
                        break;
                }
            }

            public uint WriteAddr(uint port, byte val)
            {
                switch (port & 3)
                {
                    case 0:
                        return val;
                    case 2:
                        if (_opl3Active != 0 || (val == 0x05))
                            return (uint)(0x100 | val);
                        else
                            return val;
                }
                return 0;
            }

            public void GenerateBlock2(int total, int[] output)
            {
                int pos = 0;
                Array.Clear(output, 0, output.Length);
                while (total > 0)
                {
                    int samples = ForwardLfo(total);
                    for (var ch = _chan[0]; ch.ChannelNum < 9;)
                    {
                        ch = ch.SynthHandler(this, samples, output, pos);
                    }
                    total -= samples;
                    pos += samples;
                }
            }

            public void GenerateBlock3(int total, int[] output)
            {
                int pos = 0;
                while (total > 0)
                {
                    int samples = ForwardLfo(total);
                    Array.Clear(output, 0, samples * 2);

                    for (var i = 0; i < 18; i++)
                    {
                        var ch = _chan[i];
                        ch.SynthHandler(this, samples, output, pos);
                    }
                    total -= samples;
                    pos += samples * 2;
                }
            }

            public void Setup(int rate)
            {
                double scale = OPLRATE / rate;

                //Noise counter is run at the same precision as general waves
                _noiseAdd = (uint)(0.5 + scale * (1 << LfoShift));
                _noiseCounter = 0;
                _noiseValue = 1; //Make sure it triggers the noise xor the first time
                //The low frequency oscillation counter
                //Every time his overflows vibrato and tremoloindex are increased
                _lfoAdd = (int)(0.5 + scale * (1 << LfoShift));
                _lfoCounter = 0;
                _vibratoIndex = 0;
                _tremoloIndex = 0;

                //With higher octave this gets shifted up
                //-1 since the freqCreateTable = *2
                #if WAVE_PRECISION
                double freqScale = ( 1 << 7 ) * scale * ( 1 << ( WAVE_SH - 1 - 10));
                for ( int i = 0; i < 16; i++ ) {
                freqMul[i] = (uint)( 0.5 + freqScale * FreqCreateTable[ i ] );
                }
                #else
                uint freqScale = (uint)(0.5 + scale * (1 << (WaveShift - 1 - 10)));
                for (int i = 0; i < 16; i++)
                {
                    _freqMul[i] = freqScale * FreqCreateTable[i];
                }
                #endif

                //-3 since the real envelope takes 8 steps to reach the single value we supply
                for (byte i = 0; i < 76; i++)
                {
                    byte index, shift;
                    EnvelopeSelect(i, out index, out shift);
                    _linearRates[i] = (uint)(scale * (EnvelopeIncreaseTable[index] << (RateShift + EnvExtra - shift - 3)));
                }
                //Generate the best matching attack rate
                for (byte i = 0; i < 62; i++)
                {
                    byte index, shift;
                    EnvelopeSelect(i, out index, out shift);
                    //Original amount of samples the attack would take
                    int original = (int)((AttackSamplesTable[index] << shift) / scale);

                    int guessAdd = (int)(scale * (EnvelopeIncreaseTable[index] << (RateShift - shift - 3)));
                    int bestAdd = guessAdd;
                    uint bestDiff = 1 << 30;
                    for (uint passes = 0; passes < 16; passes++)
                    {
                        int volume = EnvMax;
                        int samples = 0;
                        uint count = 0;
                        while (volume > 0 && samples < original * 2)
                        {
                            count = (uint)(count + guessAdd);
                            int change = (int)(count >> RateShift);
                            count &= RateMask;
                            if (change != 0)
                            { // less than 1 %
                                volume += (~volume * change) >> 3;
                            }
                            samples++;

                        }
                        int diff = original - samples;
                        uint lDiff = (uint)Math.Abs(diff);
                        //Init last on first pass
                        if (lDiff < bestDiff)
                        {
                            bestDiff = lDiff;
                            bestAdd = guessAdd;
                            if (bestDiff == 0)
                                break;
                        }
                        //Below our target
                        if (diff < 0)
                        {
                            //Better than the last time
                            int mul = ((original - diff) << 12) / original;
                            guessAdd = ((guessAdd * mul) >> 12);
                            guessAdd++;
                        }
                        else if (diff > 0)
                        {
                            int mul = ((original - diff) << 12) / original;
                            guessAdd = (guessAdd * mul) >> 12;
                            guessAdd--;
                        }
                    }
                    _attackRates[i] = (uint)bestAdd;
                }
                for (byte i = 62; i < 76; i++)
                {
                    //This should provide instant volume maximizing
                    _attackRates[i] = 8 << RateShift;
                }
                //Setup the channels with the correct four op flags
                //Channels are accessed through a table so they appear linear here
                _chan[0].FourMask = 0x00 | (1 << 0);
                _chan[1].FourMask = 0x80 | (1 << 0);
                _chan[2].FourMask = 0x00 | (1 << 1);
                _chan[3].FourMask = 0x80 | (1 << 1);
                _chan[4].FourMask = 0x00 | (1 << 2);
                _chan[5].FourMask = 0x80 | (1 << 2);

                _chan[9].FourMask = 0x00 | (1 << 3);
                _chan[10].FourMask = 0x80 | (1 << 3);
                _chan[11].FourMask = 0x00 | (1 << 4);
                _chan[12].FourMask = 0x80 | (1 << 4);
                _chan[13].FourMask = 0x00 | (1 << 5);
                _chan[14].FourMask = 0x80 | (1 << 5);

                //mark the percussion channels
                _chan[6].FourMask = 0x40;
                _chan[7].FourMask = 0x40;
                _chan[8].FourMask = 0x40;

                //Clear Everything in opl3 mode
                WriteReg(0x105, 0x1);
                for (uint i = 0; i < 512; i++)
                {
                    if (i == 0x105)
                        continue;
                    WriteReg(i, 0xff);
                    WriteReg(i, 0x0);
                }
                WriteReg(0x105, 0x0);
                //Clear everything in opl2 mode
                for (uint i = 0; i < 255; i++)
                {
                    WriteReg(i, 0xff);
                    WriteReg(i, 0x0);
                }
            }

            /// <summary>
            /// Return the maximum amount of samples before and LFO change.
            /// </summary>
            /// <returns>The maximum amount of samples before and LFO change.</returns>
            /// <param name="samples">Samples.</param>
            private int ForwardLfo(int samples)
            {
                //Current vibrato value, runs 4x slower than tremolo
                _vibratoSign = (sbyte)((VibratoTable[_vibratoIndex >> 2]) >> 7);
                _vibratoShift = (byte)((VibratoTable[_vibratoIndex >> 2] & 7) + _vibratoStrength);
                _tremoloValue = (byte)(tremoloTable[_tremoloIndex] >> _tremoloStrength);

                //Check hom many samples there can be done before the value changes
                int todo = LfoMax - _lfoCounter;
                int count = (todo + _lfoAdd - 1) / _lfoAdd;
                if (count > samples)
                {
                    count = samples;
                    _lfoCounter += count * _lfoAdd;
                }
                else
                {
                    _lfoCounter += count * _lfoAdd;
                    _lfoCounter &= (LfoMax - 1);
                    //Maximum of 7 vibrato value * 4
                    _vibratoIndex = (byte)((_vibratoIndex + 1) & 31);
                    //Clip tremolo to the the table size
                    if (_tremoloIndex + 1 < tremoloTable.Length)
                        ++_tremoloIndex;
                    else
                        _tremoloIndex = 0;
                }
                return count;
            }

            private void RegOp(uint reg, Action<Operator> action)
            {
                var index = ((reg >> 3) & 0x20) | (reg & 0x1f);
                var op = opOffsetTable[index];
                if (op != null)
                {
                    action(op(this));
                }
            }

            private void RegChan(uint reg, Action<Channel> action)
            {
                var ch = chanOffsetTable[((reg >> 4) & 0x10) | (reg & 0xf)]; 
                if (ch != null)
                {
                    action(ch(this));
                }
            }

            private void WriteBd(byte val)
            {
                byte change = (byte)(_regBd ^ val);
                if (change == 0)
                    return;
                _regBd = val;
                //TODO could do this with shift and xor?
                _vibratoStrength = (byte)(((val & 0x40) != 0) ? 0x00 : 0x01);
                _tremoloStrength = (byte)(((val & 0x80) != 0) ? 0x00 : 0x02);
                if ((val & 0x20) != 0)
                {
                    //Drum was just enabled, make sure channel 6 has the right synth
                    if ((change & 0x20) != 0)
                    {
                        var mode = (_opl3Active != 0) ? SynthMode.Sm3Percussion : SynthMode.Sm2Percussion;
                        _chan[6].SynthMode = mode;
                    }
                    //Bass Drum
                    if ((val & 0x10) != 0)
                    {
                        _chan[6].Ops[0].KeyOn(0x2);
                        _chan[6].Ops[1].KeyOn(0x2);
                    }
                    else
                    {
                        _chan[6].Ops[0].KeyOff(0x2);
                        _chan[6].Ops[1].KeyOff(0x2);
                    }
                    //Hi-Hat
                    if ((val & 0x1) != 0)
                    {
                        _chan[7].Ops[0].KeyOn(0x2);
                    }
                    else
                    {
                        _chan[7].Ops[0].KeyOff(0x2);
                    }
                    //Snare
                    if ((val & 0x8) != 0)
                    {
                        _chan[7].Ops[1].KeyOn(0x2);
                    }
                    else
                    {
                        _chan[7].Ops[1].KeyOff(0x2);
                    }
                    //Tom-Tom
                    if ((val & 0x4) != 0)
                    {
                        _chan[8].Ops[0].KeyOn(0x2);
                    }
                    else
                    {
                        _chan[8].Ops[0].KeyOff(0x2);
                    }
                    //Top Cymbal
                    if ((val & 0x2) != 0)
                    {
                        _chan[8].Ops[1].KeyOn(0x2);
                    }
                    else
                    {
                        _chan[8].Ops[1].KeyOff(0x2);
                    }
                    //Toggle keyoffs when we turn off the percussion
                }
                else if ((change & 0x20) != 0)
                {
                    //Trigger a reset to setup the original synth handler
                    _chan[6].ResetC0(this);
                    _chan[6].Ops[0].KeyOff(0x2);
                    _chan[6].Ops[1].KeyOff(0x2);
                    _chan[7].Ops[0].KeyOff(0x2);
                    _chan[7].Ops[1].KeyOff(0x2);
                    _chan[8].Ops[0].KeyOff(0x2);
                    _chan[8].Ops[1].KeyOff(0x2);
                }
            }

            //This is used as the base counter for vibrato and tremolo
            private int _lfoCounter;

            private int _lfoAdd;

            private uint _noiseCounter;
            private uint _noiseAdd;
            private uint _noiseValue;

            private readonly uint[] _freqMul = new uint[16];
            private readonly uint[] _linearRates = new uint[76];
            private readonly uint[] _attackRates = new uint[76];

            //18 channels with 2 operators each
            private readonly Channel[] _chan;

            private byte _reg104;
            private byte _reg08;
            private byte _regBd;
            private byte _vibratoIndex;
            private byte _tremoloIndex;
            private sbyte _vibratoSign;
            private byte _vibratoShift;
            private byte _tremoloValue;
            private byte _vibratoStrength;
            private byte _tremoloStrength;
            /// <summary>
            /// Mask for allowed wave forms.
            /// </summary>
            private byte _waveFormMask;
            //0 or -1 when enabled
            private sbyte _opl3Active;

            //The lower bits are the shift of the operator vibrato value
            //The highest bit is right shifted to generate -1 or 0 for negation
            //So taking the highest input value of 7 this gives 3, 7, 3, 0, -3, -7, -3, 0
            private static readonly sbyte[] VibratoTable =
                {
                    1 - 0x00, 0 - 0x00, 1 - 0x00, 30 - 0x00,
                    1 - 0x80, 0 - 0x80, 1 - 0x80, 30 - 0x80
                };
        }
    }
}

