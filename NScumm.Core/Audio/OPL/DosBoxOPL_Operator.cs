//
//  DosBoxOPL_Operator.cs
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

#define DBOPL_WAVE_EQUALS_WAVE_TABLEMUL
#define DBOPL_WAVE_GREATER_OR_EQUALS_WAVE_HANDLER

using System;

namespace NScumm.Core.Audio.OPL
{
    partial class DosBoxOPL
    {
        class Operator
        {
            //Masks for operator 20 values
            [Flags]
            public enum Mask
            {
                Ksr = 0x10,
                Sustain = 0x20,
                Vibrato = 0x40,
                Tremolo = 0x80
            }

            public enum State
            {
                Off,
                Release,
                Sustain,
                Decay,
                Attack
            }

            VolumeHandler volHandler;

#if (DBOPL_WAVE_EQUALS_WAVE_HANDLER)
            public WaveHandler waveHandler;
#else
            int waveBase;
            int waveMask;
            int waveStart;
#endif
            int waveIndex;
            //WAVE_BITS shifted counter of the frequency index
            int waveAdd;
            //The base frequency without vibrato
            int waveCurrent;
            //waveAdd + vibratao

            public uint chanData;
            //Frequency/octave and derived data coming from whatever channel controls this
            uint freqMul;
            //Scale channel frequency with this, TODO maybe remove?
            uint vibrato;
            //Scaled up vibrato strength
            int sustainLevel;
            //When stopping at sustain level stop here
            int totalLevel;
            //totalLevel is added to every generated volume
            uint currentLevel;
            //totalLevel + tremolo
            int volume;
            //The currently active volume

            uint attackAdd;
            //Timers for the different states of the envelope
            uint decayAdd;
            uint releaseAdd;
            uint rateIndex;
            //Current position of the evenlope

            byte rateZero;
            //int for the different states of the envelope having no changes
            byte keyOn;
            //Bitmask of different values that can generate keyon
            //Registers, also used to check for changes
            Mask reg20, reg40, reg60, reg80, regE0;
            //Active part of the envelope we're in
            State state;
            //0xff when tremolo is enabled
            byte tremoloMask;
            //Strength of the vibrato
            byte vibStrength;
            //Keep track of the calculated KSR so we can check for changes
            byte ksr;

            public void UpdateAttenuation()
            {
                byte kslBase = (byte)((chanData >> Channel.ShiftKslBase) & 0xff);
                int tl = (int)reg40 & 0x3f;
                byte kslShift = KslShiftTable[(int)reg40 >> 6];
                //Make sure the attenuation goes to the right bits
                totalLevel = tl << (EnvBits - 7);    //Total level goes 2 bits below max
                totalLevel += (kslBase << EnvExtra) >> kslShift;
            }

            public void UpdateRates(Chip chip)
            {
                //Mame seems to reverse this where enabling ksr actually lowers
                //the rate, but pdf manuals says otherwise?
                byte newKsr = (byte)((chanData >> Channel.ShiftKeyCode) & 0xff);
                if (!reg20.HasFlag(Mask.Ksr))
                {
                    newKsr >>= 2;
                }
                if (ksr == newKsr)
                    return;
                ksr = newKsr;
                UpdateAttack(chip);
                UpdateDecay(chip);
                UpdateRelease(chip);
            }

            public void UpdateFrequency()
            {
                var freq = chanData & ((1 << 10) - 1);
                byte block = (byte)((chanData >> 10) & 0xff);
                #if WAVE_PRECISION
                block = 7 - block;
                waveAdd = ( freq * freqMul ) >> block;
                #else
                waveAdd = (int)((freq << block) * freqMul);
                #endif
                if (reg20.HasFlag(Mask.Vibrato))
                {
                    vibStrength = (byte)(freq >> 7);

                    #if WAVE_PRECISION
                        vibrato = ( vibStrength * freqMul ) >> block;
                    #else
                    vibrato = (uint)((vibStrength << block) * freqMul);
                    #endif
                }
                else
                {
                    vibStrength = 0;
                    vibrato = 0;
                }
            }

            public void Write20(Chip chip, byte val)
            {
                var change = ((int)reg20 ^ val);
                if (change == 0)
                    return;
                reg20 = (Mask)val;
                //Shift the tremolo bit over the entire register, saved a branch, YES!
                tremoloMask = (byte)(val >> 7);
                tremoloMask &= unchecked((byte)~((1 << EnvExtra) - 1));
                //Update specific features based on changes
                if ((change & (int)Mask.Ksr) != 0)
                {
                    UpdateRates(chip);
                }
                //With sustain enable the volume doesn't change
                if (reg20.HasFlag(Mask.Sustain) || (releaseAdd == 0))
                {
                    rateZero |= (1 << (int)State.Sustain);
                }
                else
                {
                    rateZero &= unchecked((byte)~(1 << (int)State.Sustain));
                }
                //Frequency multiplier or vibrato changed
                if ((change & (0xf | (int)Mask.Vibrato)) != 0)
                {
                    freqMul = chip.FreqMul[val & 0xf];
                    UpdateFrequency();
                }
            }

            public void Write40(Chip chip, byte val)
            {
                if (0 == ((int)reg40 ^ val))
                    return;
                reg40 = (Mask)val;
                UpdateAttenuation();
            }

            public void Write60(Chip chip, byte val)
            {
                var change = ((int)reg60 ^ val);
                reg60 = (Mask)val;
                if ((change & 0x0f) != 0)
                {
                    UpdateDecay(chip);
                }
                if ((change & 0xf0) != 0)
                {
                    UpdateAttack(chip);
                }
            }

            public void Write80(Chip chip, byte val)
            {
                var change = ((int)reg80 ^ val);
                if (change == 0)
                    return;
                reg80 = (Mask)val;
                byte sustain = (byte)(val >> 4);
                //Turn 0xf into 0x1f
                sustain |= (byte)((sustain + 1) & 0x10);
                sustainLevel = sustain << (EnvBits - 5);
                if ((change & 0x0f) != 0)
                {
                    UpdateRelease(chip);
                }
            }

            public void WriteE0(Chip chip, byte val)
            {
                if (((int)regE0 ^ val) == 0)
                    return;
                //in opl3 mode you can always selet 7 waveforms regardless of waveformselect
                byte waveForm = (byte)(val & ((0x3 & chip.WaveFormMask) | (0x7 & chip.Opl3Active)));
                regE0 = (Mask)val;
                #if ( DBOPL_WAVE_EQUALS_WAVE_HANDLER )
                waveHandler = WaveHandlerTable[waveForm];
                #else
                waveBase = WaveBaseTable[waveForm];
                waveStart = (WaveStartTable[waveForm] << WaveShift);
                waveMask = WaveMaskTable[waveForm];
                #endif
            }

            public bool Silent()
            {
                if (!EnvSilent(totalLevel + volume))
                    return false;
                if (0 == (rateZero & (1 << (int)state)))
                    return false;
                return true;
            }

            public void Prepare(Chip chip)
            {
                currentLevel = (uint)(totalLevel + (chip.TremoloValue & tremoloMask));
                waveCurrent = waveAdd;
                if ((vibStrength >> chip.VibratoShift) != 0)
                {
                    int add = (int)vibrato >> chip.VibratoShift;
                    //Sign extend over the shift value
                    int neg = chip.VibratoSign;
                    //Negate the add with -1 or 0
                    add = (add ^ neg) - neg;
                    waveCurrent = (waveCurrent+add);
                }

            }

            public void KeyOn(byte mask)
            {
                if (keyOn == 0)
                {
                    //Restart the frequency generator
                    #if DBOPL_WAVE_GREATER_OR_EQUALS_WAVE_HANDLER
                    waveIndex = waveStart;
                    #else
                    waveIndex = 0;
                    #endif
                    rateIndex = 0;
                    SetState(State.Attack);
                }
                keyOn |= mask;
            }

            public void KeyOff(byte mask)
            {
                keyOn &= (byte)~mask;
                if (keyOn == 0)
                {
                    if (state != State.Off)
                    {
                        SetState(State.Release);
                    }
                }
            }

            public int TemplateVolume(State state)
            {
                int vol = volume;
                int change;
                switch (state)
                {
                    case State.Off:
                        return EnvMax;
                    case State.Attack:
                        change = RateForward(attackAdd);
                        if (change == 0)
                            return vol;
                        vol += ((~vol) * change) >> 3;
                        if (vol < EnvMin)
                        {
                            volume = EnvMin;
                            rateIndex = 0;
                            SetState(State.Decay);
                            return EnvMin;
                        }
                        break;
                    case State.Decay:
                        vol += RateForward(decayAdd);
                        if (vol >= sustainLevel)
                        {
                            //Check if we didn't overshoot max attenuation, then just go off
                            if (vol >= EnvMax)
                            {
                                volume = EnvMax;
                                SetState(State.Off);
                                return EnvMax;
                            }
                            //Continue as sustain
                            rateIndex = 0;
                            SetState(State.Sustain);
                        }
                        break;
                    case State.Sustain:
                        if (reg20.HasFlag(Mask.Sustain))
                        {
                            return vol;
                        }
                        vol += RateForward(releaseAdd);
                        if (vol >= EnvMax)
                        {
                            volume = EnvMax;
                            SetState(State.Off);
                            return EnvMax;
                        }
                        break;
                //In sustain phase, but not sustaining, do regular release
                    case State.Release:
                        vol += RateForward(releaseAdd);
                        if (vol >= EnvMax)
                        {
                            volume = EnvMax;
                            SetState(State.Off);
                            return EnvMax;
                        }
                        break;
                }
                volume = vol;
                return vol;
            }

            public int RateForward(uint add)
            {
                rateIndex += add;
                int ret = (int)(rateIndex >> RateShift);
                rateIndex = rateIndex & RateMask;
                return ret;
            }

            public int ForwardWave()
            {
                waveIndex += waveCurrent;
                return waveIndex >> WaveShift;
            }

            public uint ForwardVolume()
            {
                return (uint)(currentLevel + volHandler());
            }

            public int GetSample(int modulation)
            {
                uint vol = ForwardVolume();
                if (EnvSilent((int)vol))
                {
                    //Simply forward the wave
                    waveIndex += waveCurrent;
                    return 0;
                }
                else
                {
                    int index = ForwardWave();
                    index += modulation;
                    return GetWave(index, vol);
                }
            }

            public int GetWave(int index, uint vol)
            {
                #if ( DBOPL_WAVE_EQUALS_WAVE_HANDLER )
                return waveHandler(index, vol << (3 - ENV_EXTRA));
                #elif ( DBOPL_WAVE_EQUALS_WAVE_TABLEMUL )
                return (WaveTable[waveBase + (index & waveMask)] * MulTable[vol >> EnvExtra]) >> MulShift;
                #elif ( DBOPL_WAVE_EQUALS_WAVE_TABLELOG )
                int wave = waveBase[ index & waveMask ];
                uint total = ( wave & 0x7fff ) + vol << ( 3 - ENV_EXTRA );
                int sig = ExpTable[ total & 0xff ];
                uint exp = total >> 8;
                int neg = wave >> 16;
                return ((sig ^ neg) - neg) >> exp;
                #else
                #error "No valid wave routine"
                #endif
            }

            public Operator()
            {
                SetState(State.Off);
                rateZero = (byte)(1 << (int)State.Off);
                sustainLevel = EnvMax;
                currentLevel = EnvMax;
                totalLevel = EnvMax;
                volume = EnvMax;
            }

            void SetState(State s)
            {
                state = s;
                volHandler = () => TemplateVolume(s);
            }

            /// <summary>
            /// We zero out when rate == 0
            /// </summary>
            /// <param name="chip">Chip.</param>
            void UpdateAttack(Chip chip)
            {
                byte rate = (byte)((int)reg60 >> 4);
                if (rate != 0)
                {
                    byte val = (byte)((rate << 2) + ksr);
                    attackAdd = chip.AttackRates[val];
                    rateZero &= (byte)unchecked((byte)~(1 << (int)State.Attack));
                }
                else
                {
                    attackAdd = 0;
                    rateZero |= (1 << (int)State.Attack);
                }
            }

            void UpdateRelease(Chip chip)
            {
                byte rate = (byte)((int)reg80 & 0xf);
                if (rate != 0)
                {
                    byte val = (byte)((rate << 2) + ksr);
                    releaseAdd = chip.LinearRates[val];
                    rateZero &= (byte)unchecked((byte)~(1 << (int)State.Release));
                    if (!(reg20.HasFlag(Mask.Sustain)))
                    {
                        rateZero &= (byte)unchecked((byte)~(1 << (int)State.Sustain));
                    }
                }
                else
                {
                    rateZero |= (1 << (int)State.Release);
                    releaseAdd = 0;
                    if (!(reg20.HasFlag(Mask.Sustain)))
                    {
                        rateZero |= (1 << (int)State.Sustain);
                    }
                }
            }

            void UpdateDecay(Chip chip)
            {
                var rate = (int)reg60 & 0xf;
                if (rate != 0)
                {
                    byte val = (byte)((rate << 2) + ksr);
                    decayAdd = chip.LinearRates[val];
                    rateZero &= unchecked((byte)~(1 << (int)State.Decay));
                }
                else
                {
                    decayAdd = 0;
                    rateZero |= (1 << (int)State.Decay);
                }
            }

            //Shift strength for the ksl value determined by ksl strength
            static readonly byte[] KslShiftTable = { 31, 1, 2, 0 };

            #if DBOPL_WAVE_EQUALS_WAVE_HANDLER
            static int WaveForm0(uint i, uint volume)
            {
                int neg = (int)(0 - ((i >> 9) & 1));//Create ~0 or 0
                uint wave = SinTable[i & 511];
                return (MakeVolume(wave, volume) ^ neg) - neg;
            }

            static int WaveForm1(uint i, uint volume)
            {
                uint wave = SinTable[i & 511];
                wave |= ((((i ^ 512) & 512) - 1) >> (32 - 12));
                return MakeVolume(wave, volume);
            }

            static int WaveForm2(uint i, uint volume)
            {
                uint wave = SinTable[i & 511];
                return MakeVolume(wave, volume);
            }

            static int WaveForm3(uint i, uint volume)
            {
                uint wave = SinTable[i & 255];
                wave |= (((i ^ 256) & 256) - 1) >> (32 - 12);
                return MakeVolume(wave, volume);
            }

            static int WaveForm4(uint i, uint volume)
            {
                //Twice as fast
                i <<= 1;
                int neg = (int)(0 - ((i >> 9) & 1));//Create ~0 or 0
                uint wave = SinTable[i & 511];
                wave |= ((((i ^ 512) & 512) - 1) >> (32 - 12));
                return (MakeVolume(wave, volume) ^ neg) - neg;
            }

            static int WaveForm5(uint i, uint volume)
            {
                //Twice as fast
                i <<= 1;
                uint wave = SinTable[i & 511];
                wave |= (((i ^ 512) & 512) - 1) >> (32 - 12);
                return MakeVolume(wave, volume);
            }

            static int WaveForm6(uint i, uint volume)
            {
                int neg = (int)(0 - ((i >> 9) & 1));//Create ~0 or 0
                return (MakeVolume(0, volume) ^ neg) - neg;
            }

            static int WaveForm7(uint i, uint volume)
            {
                //Negative is reversed here
                int neg = (int)(((i >> 9) & 1) - 1);
                uint wave = (i << 3);
                //When negative the volume also runs backwards
                wave = (uint)(((wave ^ neg) - neg) & 4095);
                return (MakeVolume(wave, volume) ^ neg) - neg;
            }

            static int MakeVolume(uint wave, uint volume)
            {
                uint total = (wave + volume);
                uint index = (total & 0xff);
                uint sig = ExpTable[index];
                int exp = (int)(total >> 8);
#if false
                //Check if we overflow the 31 shift limit
                if ( exp >= 32 ) {
                LOG_MSG( "WTF %d %d", total, exp );
                }
#endif
                return (int)(sig >> exp);
            }

            static readonly WaveHandler[] WaveHandlerTable =
                {
                    WaveForm0, WaveForm1, WaveForm2, WaveForm3,
                    WaveForm4, WaveForm5, WaveForm6, WaveForm7
                };
            #endif
        }
    }
}

