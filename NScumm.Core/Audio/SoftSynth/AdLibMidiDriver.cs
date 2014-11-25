//
//  AdLibMidiDriver.cs
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
using System.Diagnostics;
using NScumm.Core.Audio.OPL;


namespace NScumm.Core.Audio.SoftSynth
{
    struct InstrumentExtra
    {
        public byte A, B, C, D, E, F, G, H;

        public InstrumentExtra(byte[] data)
        {
            A = data[0];
            B = data[1];
            C = data[2];
            D = data[3];
            E = data[4];
            F = data[5];
            G = data[6];
            H = data[7];
        }
    }

    class AdLibInstrument
    {
        public byte ModCharacteristic;
        public byte ModScalingOutputLevel;
        public byte ModAttackDecay;
        public byte ModSustainRelease;
        public byte ModWaveformSelect;
        public byte CarCharacteristic;
        public byte CarScalingOutputLevel;
        public byte CarAttackDecay;
        public byte CarSustainRelease;
        public byte CarWaveformSelect;
        public byte Feedback;
        public byte FlagsA;
        public InstrumentExtra ExtraA;
        public byte FlagsB;
        public InstrumentExtra ExtraB;
        public byte Duration;

        public AdLibInstrument()
        {
        }

        public AdLibInstrument(AdLibInstrument copy)
        {
            ModCharacteristic = copy.ModCharacteristic;
            ModScalingOutputLevel = copy.ModScalingOutputLevel;
            ModAttackDecay = copy.ModAttackDecay;
            ModSustainRelease = copy.ModSustainRelease;
            ModWaveformSelect = copy.ModWaveformSelect;
            CarCharacteristic = copy.CarCharacteristic;
            CarScalingOutputLevel = copy.CarScalingOutputLevel;
            CarAttackDecay = copy.CarAttackDecay;
            CarSustainRelease = copy.CarSustainRelease;
            CarWaveformSelect = copy.CarWaveformSelect;
            Feedback = copy.Feedback;
            FlagsA = copy.FlagsA;
            ExtraA = copy.ExtraA;
            FlagsB = copy.FlagsB;
            ExtraB = copy.ExtraB;
            Duration = copy.Duration;
        }

        public AdLibInstrument(byte[] data1, InstrumentExtra extraA, byte flagsB, InstrumentExtra extraB, byte duration)
        {
            ModCharacteristic = data1[0];
            ModScalingOutputLevel = data1[1];
            ModAttackDecay = data1[2];
            ModSustainRelease = data1[3];
            ModWaveformSelect = data1[4];
            CarCharacteristic = data1[5];
            CarScalingOutputLevel = data1[6];
            CarAttackDecay = data1[7];
            CarSustainRelease = data1[8];
            CarWaveformSelect = data1[9];
            Feedback = data1[10];
            FlagsA = data1[11];
            ExtraA = extraA;
            FlagsB = flagsB;
            ExtraB = extraB;
            Duration = duration;
        }
    }

    class Struct10
    {
        public byte Active;
        public short CurVal;
        public short Count;
        public ushort MaxValue;
        public short StartValue;
        public byte Loop;
        public byte[] TableA;
        public byte[] TableB;
        public sbyte Unk3;
        public sbyte ModWheel;
        public sbyte ModWheelLast;
        public ushort SpeedLoMax;
        public ushort NumSteps;
        public short SpeedHi;
        public sbyte Direction;
        public ushort SpeedLo;
        public ushort SpeedLoCounter;

        public Struct10()
        {
            TableA = new byte[4];
            TableB = new byte[4];
        }
    }

    class Struct11
    {
        public short ModifyVal;
        public byte Param, Flag0x40, Flag0x10;
        public Struct10 S10;
    }

    class AdLibMidiDriver: EmulatedMidiDriver
    {
        public AdLibMidiDriver(IMixer mixer)
            : base(mixer)
        {
            _voiceIndex = -1;

            _parts = new AdLibPart[32];
            for (var i = 0; i < _parts.Length; ++i)
            {
                _parts[i] = new AdLibPart();
                _parts[i].Init(this, (byte)(i + ((i >= 9) ? 1 : 0)));
            }
            _percussion = new AdLibPercussionChannel();
            _percussion.Init(this, 9);
            _timerIncrease = 0xD69;
            _timerThreshold = 0x411B;

            _voices = new AdLibVoice[9];
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i] = new AdLibVoice();
            }
        }

        public override MidiDriverError Open()
        {
            if (IsOpen)
                return MidiDriverError.AlreadyOpen;

            base.Open();

            byte i = 0;
            foreach (var voice in _voices)
            {
                voice.Channel = i++;
                voice.S11a.S10 = voice.S10b;
                voice.S11b.S10 = voice.S10a;
            }

            // Try to use OPL3 when requested.
#if ENABLE_OPL3
    if (_opl3Mode) {
        _opl = OPL::Config::create(OPL::Config::kOpl3);
    }

    // Initialize plain OPL2 when no OPL3 is intiailized already.
    if (!_opl) {
#endif
            // TODO: vs OPL
//        _opl = OPL::Config::create();
            _opl = new DosBoxOPL(OplType.Opl2);
            #if ENABLE_OPL3
        _opl3Mode = false;
    }
#endif
            _opl.Init((uint)Rate);

            _regCache = new byte[256];

            AdlibWrite(8, 0x40);
            AdlibWrite(0xBD, 0x00);
#if ENABLE_OPL3
    if (!_opl3Mode) {
#endif
            AdlibWrite(1, 0x20);
            CreateLookupTable();
#if ENABLE_OPL3
    } else {
        _regCacheSecondary = (byte *)calloc(256, 1);
        adlibWriteSecondary(5, 1);
    }
#endif

            _mixerSoundHandle = _mixer.PlayStream(SoundType.Plain, this, -1, Mixer.MaxChannelVolume, 0, false, true);

            return 0;
        }


        public override void Send(int b)
        {
            Send((byte)(b & 0xF), (byte)b & 0xFFFFFFF0);
        }

        public void Send(byte chan, uint b)
        {
            //byte param3 = (byte) ((b >> 24) & 0xFF);
            byte param2 = (byte)((b >> 16) & 0xFF);
            byte param1 = (byte)((b >> 8) & 0xFF);
            byte cmd = (byte)(b & 0xF0);

            AdLibPart part;
            if (chan == 9)
                part = _percussion;
            else
                part = _parts[chan];

            switch (cmd)
            {
                case 0x80:// Note Off
                    part.NoteOff(param1);
                    break;
                case 0x90: // Note On
                    part.NoteOn(param1, param2);
                    break;
                case 0xA0: // Aftertouch
                    break; // Not supported.
                case 0xB0: // Control Change
                    part.ControlChange(param1, param2);
                    break;
                case 0xC0: // Program Change
                    part.ProgramChange(param1);
                    break;
                case 0xD0: // Channel Pressure
                    break; // Not supported.
                case 0xE0: // Pitch Bend
                    part.PitchBend((short)((param1 | (param2 << 7)) - 0x2000));
                    break;
                case 0xF0: // SysEx
                    // We should never get here! SysEx information has to be
                    // sent via high-level semantic methods.
                    Console.Error.WriteLine("MidiDriver_ADLIB: Receiving SysEx command on a send() call");
                    break;

                default:
                    Console.Error.WriteLine("MidiDriver_ADLIB: Unknown send() command 0x%02X", cmd);
                    break;
            }
        }

        public override bool IsStereo { get { return _opl.IsStereo; } }

        public override int  Rate { get { return _mixer.OutputRate; } }

        public static uint ToType(string type)
        {
            int val = type[0];
            val |= type[1] << 8;
            val |= type[2] << 16;
            val |= type[3] << 24;
            return (uint)val;
        }

        void PartKeyOff(AdLibPart part, byte note)
        {
            for (var voice = part._voice; voice != null; voice = voice.Next)
            {
                if (voice.Note == note)
                {
                    if (part._pedal)
                        voice.WaitForPedal = true;
                    else
                        McOff(voice);
                }
            }
        }

        void PartKeyOn(AdLibPart part, AdLibInstrument instr, byte note, byte velocity, AdLibInstrument second, byte pan)
        {
            var voice = AllocateVoice(part._priEff);
            if (voice == null)
                return;

            LinkMc(part, voice);
            McKeyOn(voice, instr, note, velocity, second, pan);
        }

        void McKeyOn(AdLibVoice voice, AdLibInstrument instr, byte note, byte velocity, AdLibInstrument second, byte pan)
        {
            AdLibPart part = voice.Part;
            byte vol1, vol2;
#if ENABLE_OPL3
    byte secVol1 = 0, secVol2 = 0;
#endif

            voice.TwoChan = (byte)(instr.Feedback & 1);
            voice.Note = note;
            voice.WaitForPedal = false;
            voice.Duration = instr.Duration;
            if (voice.Duration != 0)
                voice.Duration *= 63;

            if (!_scummSmallHeader)
            {
#if ENABLE_OPL3
        if (_opl3Mode)
            vol1 = (instr->modScalingOutputLevel & 0x3F) + (velocity * ((instr->modWaveformSelect >> 3) + 1)) / 64;
        else
#endif
                vol1 = (byte)((instr.ModScalingOutputLevel & 0x3F) + volumeLookupTable[velocity >> 1, instr.ModWaveformSelect >> 2]);
            }
            else
            {
                vol1 = (byte)(0x3f - (instr.ModScalingOutputLevel & 0x3F));
            }
            if (vol1 > 0x3F)
                vol1 = 0x3F;
            voice.Vol1 = vol1;

            if (!_scummSmallHeader)
            {
#if ENABLE_OPL3
        if (_opl3Mode)
            vol2 = (instr->carScalingOutputLevel & 0x3F) + (velocity * ((instr->carWaveformSelect >> 3) + 1)) / 64;
        else
#endif
                vol2 = (byte)((instr.CarScalingOutputLevel & 0x3F) + volumeLookupTable[velocity >> 1, instr.CarWaveformSelect >> 2]);
            }
            else
            {
                vol2 = (byte)(0x3f - (instr.CarScalingOutputLevel & 0x3F));
            }
            if (vol2 > 0x3F)
                vol2 = 0x3F;
            voice.Vol2 = vol2;

#if ENABLE_OPL3
    if (_opl3Mode) {
        voice->_secTwoChan = second->feedback & 1;
        secVol1 = (second->modScalingOutputLevel & 0x3F) + (velocity * ((second->modWaveformSelect >> 3) + 1)) / 64;
        if (secVol1 > 0x3F) {
            secVol1 = 0x3F;
        }
        voice->_secVol1 = secVol1;
        secVol2 = (second->carScalingOutputLevel & 0x3F) + (velocity * ((second->carWaveformSelect >> 3) + 1)) / 64;
        if (secVol2 > 0x3F) {
            secVol2 = 0x3F;
        }
        voice->_secVol2 = secVol2;
    }
#endif

            if (!_scummSmallHeader)
            {
#if ENABLE_OPL3
        if (!_opl3Mode) {
#endif
                int c = part._volEff >> 2;
                vol2 = volumeTable[volumeLookupTable[vol2, c]];
                if (voice.TwoChan != 0)
                    vol1 = volumeTable[volumeLookupTable[vol1, c]];
#if ENABLE_OPL3
        } else {
            vol2    = g_volumeTable[((vol2    + 1) * part->_volEff) >> 7];
            secVol2 = g_volumeTable[((secVol2 + 1) * part->_volEff) >> 7];
            if (voice->_twoChan)
                vol1    = g_volumeTable[((vol1    + 1) * part->_volEff) >> 7];
            if (voice->_secTwoChan)
                secVol1 = g_volumeTable[((secVol1 + 1) * part->_volEff) >> 7];
        }
#endif
            }

            AdlibSetupChannel(voice.Channel, instr, vol1, vol2);
#if ENABLE_OPL3
    if (!_opl3Mode) {
#endif
            AdlibNoteOnEx(voice.Channel, /*part->_transposeEff + */note, part._detuneEff + (part._pitchBend * part._pitchBendFactor >> 6));

            if ((instr.FlagsA & 0x80) != 0)
            {
                McInitStuff(voice, voice.S10a, voice.S11a, instr.FlagsA, instr.ExtraA);
            }
            else
            {
                voice.S10a.Active = 0;
            }

            if ((instr.FlagsB & 0x80) != 0)
            {
                McInitStuff(voice, voice.S10b, voice.S11b, instr.FlagsB, instr.ExtraB);
            }
            else
            {
                voice.S10b.Active = 0;
            }
#if ENABLE_OPL3
    } else {
        adlibSetupChannelSecondary(voice->_channel, second, secVol1, secVol2, pan);
        adlibNoteOnEx(voice->_channel, note, part->_pitchBend >> 1);
    }
#endif
        }

        void McInitStuff(AdLibVoice voice, Struct10 s10, Struct11 s11, byte flags, InstrumentExtra ie)
        {
            var part = voice.Part;
            s11.ModifyVal = 0;
            s11.Flag0x40 = (byte)(flags & 0x40);
            s10.Loop = (byte)(flags & 0x20);
            s11.Flag0x10 = (byte)(flags & 0x10);
            s11.Param = paramTable1[flags & 0xF];
            s10.MaxValue = maxValTable[flags & 0xF];
            s10.Unk3 = 31;
            if (s11.Flag0x40 != 0)
            {
                s10.ModWheel = (sbyte)(part._modWheel >> 2);
            }
            else
            {
                s10.ModWheel = 31;
            }

            switch (s11.Param)
            {
                case 0:
                    s10.StartValue = voice.Vol2;
                    break;
                case 13:
                    s10.StartValue = voice.Vol1;
                    break;
                case 30:
                    s10.StartValue = 31;
                    s11.S10.ModWheel = 0;
                    break;
                case 31:
                    s10.StartValue = 0;
                    s11.S10.Unk3 = 0;
                    break;
                default:
                    s10.StartValue = (short)AdlibGetRegValueParam(voice.Channel, s11.Param);
                    break;
            }

            Struct10Init(s10, ie);
        }

        void Struct10Init(Struct10 s10, InstrumentExtra ie)
        {
            s10.Active = 1;
            if (!_scummSmallHeader)
            {
                s10.CurVal = 0;
            }
            else
            {
                s10.CurVal = s10.StartValue;
                s10.StartValue = 0;
            }
            s10.ModWheelLast = 31;
            s10.Count = ie.A;
            if (s10.Count != 0)
                s10.Count *= 63;
            s10.TableA[0] = ie.B;
            s10.TableA[1] = ie.D;
            s10.TableA[2] = ie.F;
            s10.TableA[3] = ie.G;

            s10.TableB[0] = ie.C;
            s10.TableB[1] = ie.E;
            s10.TableB[2] = 0;
            s10.TableB[3] = ie.H;

            Struct10Setup(s10);
        }

        void Struct10Setup(Struct10 s10)
        {
            int b, c, d, e, f, g, h;
            byte t;

            b = s10.Unk3;
            f = s10.Active - 1;

            t = s10.TableA[f];
            e = numStepsTable[volumeLookupTable[t & 0x7F, b]];
            if ((t & 0x80) != 0)
            {
                e = RandomNr(e);
            }
            if (e == 0)
                e++;

            s10.NumSteps = s10.SpeedLoMax = (ushort)e;

            if (f != 2)
            {
                c = s10.MaxValue;
                g = s10.StartValue;
                t = s10.TableB[f];
                d = LookupVolume(c, (t & 0x7F) - 31);
                if ((t & 0x80) != 0)
                {
                    d = RandomNr(d);
                }
                if (d + g > c)
                {
                    h = c - g;
                }
                else
                {
                    h = d;
                    if (d + g < 0)
                        h = -g;
                }
                h -= s10.CurVal;
            }
            else
            {
                h = 0;
            }

            s10.SpeedHi = (short)(h / e);
            if (h < 0)
            {
                h = -h;
                s10.Direction = -1;
            }
            else
            {
                s10.Direction = 1;
            }

            s10.SpeedLo = (ushort)(h % e);
            s10.SpeedLoCounter = 0;
        }

        void AdlibSetParam(int channel, byte param, int value, bool primary = true)
        {
            byte reg;

            Debug.Assert(channel >= 0 && channel < 9);
#if ENABLE_OPL3
    assert(!_opl3Mode || (param == 0 || param == 13));
#endif

            if (param <= 12)
            {
                reg = operator2Offsets[channel];
            }
            else if (param <= 25)
            {
                param -= 13;
                reg = operator1Offsets[channel];
            }
            else if (param <= 27)
            {
                param -= 13;
                reg = (byte)channel;
            }
            else if (param == 28 || param == 29)
            {
                if (param == 28)
                    value -= 15;
                else
                    value -= 383;
                value <<= 4;
                _channelTable2[channel] = (ushort)value;
                AdlibPlayNote(channel, _curNotTable[channel] + value);
                return;
            }
            else
            {
                return;
            }

            var asp = setParamTable[param];
            if (asp.Inversion != 0)
                value = asp.Inversion - value;
            reg += asp.RegisterBase;
#if ENABLE_OPL3
    if (primary) {
#endif
            AdlibWrite(reg, (byte)((AdlibGetRegValue(reg) & ~asp.Mask) | (((byte)value) << asp.Shift)));
#if ENABLE_OPL3
    } else {
        adlibWriteSecondary(reg, (adlibGetRegValueSecondary(reg) & ~as->mask) | (((byte)value) << as->shift));
    }
#endif
        }

        static int LookupVolume(int a, int b)
        {
            if (b == 0)
                return 0;

            if (b == 31)
                return a;

            if (a < -63 || a > 63)
            {
                return b * (a + 1) >> 5;
            }

            if (b < 0)
            {
                if (a < 0)
                {
                    return volumeLookupTable[-a, -b];
                }
                else
                {
                    return -volumeLookupTable[a, -b];
                }
            }
            else
            {
                if (a < 0)
                {
                    return -volumeLookupTable[-a, b];
                }
                else
                {
                    return volumeLookupTable[a, b];
                }
            }
        }

        static byte _randSeed = 1;

        static int RandomNr(int a)
        {
            if ((_randSeed & 1) != 0)
            {
                _randSeed >>= 1;
                _randSeed ^= 0xB8;
            }
            else
            {
                _randSeed >>= 1;
            }
            return _randSeed * a >> 8;
        }

        void AdlibNoteOn(int chan, byte note, int mod)
        {
#if ENABLE_OPL3
    if (_opl3Mode) {
        adlibNoteOnEx(chan, note, mod);
        return;
    }
#endif

            Debug.Assert(chan >= 0 && chan < 9);
            int code = (note << 7) + mod;
            _curNotTable[chan] = (ushort)code;
            AdlibPlayNote(chan, (short)_channelTable2[chan] + code);
        }

        void AdlibNoteOnEx(int chan, byte note, int mod)
        {
            Debug.Assert(chan >= 0 && chan < 9);

#if ENABLE_OPL3
    if (_opl3Mode) {
        const int noteAdjusted = note + (mod >> 8) - 7;
        const int pitchAdjust = (mod >> 5) & 7;

        adlibWrite(0xA0 + chan, g_noteFrequencies[(noteAdjusted % 12) * 8 + pitchAdjust + 6 * 8]);
        adlibWriteSecondary(0xA0 + chan, g_noteFrequencies[(noteAdjusted % 12) * 8 + pitchAdjust + 6 * 8]);
        adlibWrite(0xB0 + chan, (CLIP(noteAdjusted / 12, 0, 7) << 2) | 0x20);
        adlibWriteSecondary(0xB0 + chan, (CLIP(noteAdjusted / 12, 0, 7) << 2) | 0x20);
    } else {
#endif
            int code = (note << 7) + mod;
            _curNotTable[chan] = (ushort)code;
            _channelTable2[chan] = 0;
            AdlibPlayNote(chan, code);
#if ENABLE_OPL3
    }
#endif
        }

        void AdlibPlayNote(int channel, int note)
        {
            byte old, oct, notex;
            int note2;
            int i;

            note2 = (note >> 7) - 4;
            note2 = (note2 < 128) ? note2 : 0;

            oct = (byte)(note2 / 12);
            if (oct > 7)
                oct = 7 << 2;
            else
                oct <<= 2;
            notex = (byte)(note2 % 12 + 3);

            old = AdlibGetRegValue((byte)(channel + 0xB0));
            if ((old & 0x20) != 0)
            {
                old &= 0xdf;
                if (oct > old)
                {
                    if (notex < 6)
                    {
                        notex += 12;
                        oct -= 4;
                    }
                }
                else if (oct < old)
                {
                    if (notex > 11)
                    {
                        notex -= 12;
                        oct += 4;
                    }
                }
            }

            i = (notex << 3) + ((note >> 4) & 0x7);
            AdlibWrite((byte)(channel + 0xA0), noteFrequencies[i]);
            AdlibWrite((byte)(channel + 0xB0), (byte)(oct | 0x20));
        }

        void AdlibSetupChannel(int chan, AdLibInstrument instr, byte vol1, byte vol2)
        {
            Debug.Assert(chan >= 0 && chan < 9);

            byte channel = operator1Offsets[chan];
            AdlibWrite((byte)(channel + 0x20), instr.ModCharacteristic);
            AdlibWrite((byte)(channel + 0x40), (byte)((instr.ModScalingOutputLevel | 0x3F) - vol1));
            AdlibWrite((byte)(channel + 0x60), (byte)(0xff & (~instr.ModAttackDecay)));
            AdlibWrite((byte)(channel + 0x80), (byte)(0xff & (~instr.ModSustainRelease)));
            AdlibWrite((byte)(channel + 0xE0), instr.ModWaveformSelect);

            channel = operator2Offsets[chan];
            AdlibWrite((byte)(channel + 0x20), instr.CarCharacteristic);
            AdlibWrite((byte)(channel + 0x40), (byte)((instr.CarScalingOutputLevel | 0x3F) - vol2));
            AdlibWrite((byte)(channel + 0x60), (byte)(0xff & (~instr.CarAttackDecay)));
            AdlibWrite((byte)(channel + 0x80), (byte)(0xff & (~instr.CarSustainRelease)));
            AdlibWrite((byte)(channel + 0xE0), instr.CarWaveformSelect);

            AdlibWrite((byte)(chan + 0xC0), instr.Feedback
#if ENABLE_OPL3
            | (_opl3Mode ? 0x30 : 0)
#endif
            );
        }

        void LinkMc(AdLibPart part, AdLibVoice voice)
        {
            voice.Part = part;
            voice.Next = part._voice;
            part._voice = voice;
            voice.Prev = null;

            if (voice.Next != null)
                voice.Next.Prev = voice;
        }

        AdLibVoice AllocateVoice(byte pri)
        {
            AdLibVoice ac, best = null;

            for (var i = 0; i < 9; i++)
            {
                if (++_voiceIndex >= 9)
                    _voiceIndex = 0;
                ac = _voices[_voiceIndex];
                if (ac.Part == null)
                    return ac;
                if (ac.Next == null)
                {
                    if (ac.Part._priEff <= pri)
                    {
                        pri = ac.Part._priEff;
                        best = ac;
                    }
                }
            }

            /* SCUMM V3 games don't have note priorities, first comes wins. */
            if (_scummSmallHeader)
                return null;

            if (best != null)
                McOff(best);
            return best;
        }

        void McOff(AdLibVoice voice)
        {
            AdlibKeyOff(voice.Channel);

            var tmp = voice.Prev;

            if (voice.Next != null)
                voice.Next.Prev = tmp;
            if (tmp != null)
                tmp.Next = voice.Next;
            else
                voice.Part._voice = voice.Next;
            voice.Part = null;
        }

        void AdlibKeyOff(int chan)
        {
            byte reg = (byte)(chan + 0xB0);
            AdlibWrite(reg, (byte)(AdlibGetRegValue(reg) & ~0x20));
#if ENABLE_OPL3
    if (_opl3Mode) {
        adlibWriteSecondary(reg, adlibGetRegValueSecondary(reg) & ~0x20);
    }
#endif
        }

        int AdlibGetRegValueParam(int chan, byte param)
        {
            byte val;
            byte channel;

            Debug.Assert(chan >= 0 && chan < 9);

            if (param <= 12)
            {
                channel = operator2Offsets[chan];
            }
            else if (param <= 25)
            {
                param -= 13;
                channel = operator1Offsets[chan];
            }
            else if (param <= 27)
            {
                param -= 13;
                channel = (byte)chan;
            }
            else if (param == 28)
            {
                return 0xF;
            }
            else if (param == 29)
            {
                return 0x17F;
            }
            else
            {
                return 0;
            }

            var asp = setParamTable[param];
            val = AdlibGetRegValue((byte)(channel + asp.RegisterBase));
            val &= asp.Mask;
            val >>= asp.Shift;
            if (asp.Inversion != 0)
                val = (byte)(asp.Inversion - val);

            return val;
        }

        byte AdlibGetRegValue(byte reg)
        {
            return _regCache[reg];
        }

        public override MidiChannel AllocateChannel()
        {
            AdLibPart part;

            for (var i = 0; i < _parts.Length; ++i)
            {
                part = _parts[i];
                if (!part._allocated)
                {
                    part.Allocate();
                    return part;
                }
            }
            return null;
        }

        public override MidiChannel GetPercussionChannel()
        {
            return _percussion;
        }

        void AdlibWrite(byte reg, byte value)
        {
            if (_regCache[reg] == value)
            {
                return;
            }
#if DEBUG_ADLIB
    debug(6, "%10d: adlibWrite[%x] = %x", g_tick, reg, value);
#endif
            _regCache[reg] = value;

            _opl.WriteReg(reg, value);
        }

        protected override void GenerateSamples(short[] data, int pos, int len)
        {
            if (_opl.IsStereo)
            {
                len *= 2;
            }
            _opl.ReadBuffer(data, pos, len);
        }

        class AdLibPart : MidiChannel
        {
            public AdLibPart()
            {
                _pitchBendFactor = 2;
                _pan = 64;
            }

            internal protected virtual void Init(AdLibMidiDriver owner, byte channel)
            {
                _owner = owner;
                _channel = channel;
                _priEff = 127;
                ProgramChange(0);
            }

            internal protected void Allocate()
            {
                _allocated = true;
            }

            public override  MidiDriver Device{ get { return _owner; } }

            public override byte Number { get { return _channel; } }

            public override void Release()
            {
                _allocated = false;
            }

            public override void Send(uint b)
            {
                _owner.Send(_channel, b);
            }

            // Regular messages
            public override void NoteOff(byte note)
            {
                _owner.PartKeyOff(this, note);
            }

            public override void NoteOn(byte note, byte velocity)
            {
                _owner.PartKeyOn(this, _partInstr, note, velocity,
                    #if ENABLE_OPL3
                    _partInstrSecondary,
                    #else
                    null,
                    #endif
                    _pan);
            }

            public override void ProgramChange(byte program)
            {
                if (program > 127)
                    return;

                _program = program;
                #if ENABLE_OPL3
                if (!_owner->_opl3Mode) {
                #endif
                _partInstr = new AdLibInstrument(gmInstruments[program]);
                #if ENABLE_OPL3
                } else {
                memcpy(&_partInstr,          &g_gmInstrumentsOPL3[program][0], sizeof(AdLibInstrument));
                memcpy(&_partInstrSecondary, &g_gmInstrumentsOPL3[program][1], sizeof(AdLibInstrument));
                }
                #endif
            }

            public override void PitchBend(short bend)
            {
                _pitchBend = bend;
                for (var voice = _voice; voice != null; voice = voice.Next)
                {
                    #if ENABLE_OPL3
                        if (!_owner->_opl3Mode) {
                    #endif
                    _owner.AdlibNoteOn(voice.Channel, voice.Note/* + _transposeEff*/,
                        (_pitchBend * _pitchBendFactor >> 6) + _detuneEff);
                    #if ENABLE_OPL3
                        } else {
                        _owner->adlibNoteOn(voice->_channel, voice->_note, _pitchBend >> 1);
                        }
                    #endif
                }
            }

            // Control Change messages
            public override void ControlChange(byte control, byte value)
            {
                switch (control)
                {
                    case 0:
                    case 32:
                        // Bank select. Not supported
                        break;
                    case 1:
                        ModulationWheel(value);
                        break;
                    case 7:
                        Volume(value);
                        break;
                    case 10:
                        PanPosition(value);
                        break;
                    case 16:
                        PitchBendFactor(value);
                        break;
                    case 17:
                        Detune(value);
                        break;
                    case 18:
                        Priority(value);
                        break;
                    case 64:
                        Sustain(value > 0);
                        break;
                    case 91:
                        // Effects level. Not supported.
                        break;
                    case 93:
                        // Chorus level. Not supported.
                        break;
                    case 119:
                        // Unknown, used in Simon the Sorcerer 2
                        break;
                    case 121:
                        // reset all controllers
                        ModulationWheel(0);
                        PitchBendFactor(0);
                        Detune(0);
                        Sustain(false);
                        break;
                    case 123:
                        AllNotesOff();
                        break;
                    default:
                        Console.Error.WriteLine("AdLib: Unknown control change message {0} ({1})", (int)control, (int)value);
                        break;
                }
            }

            public override void ModulationWheel(byte value)
            {
                _modWheel = value;
                for (var voice = _voice; voice != null; voice = voice.Next)
                {
                    if (voice.S10a.Active != 0 && voice.S11a.Flag0x40 != 0)
                        voice.S10a.ModWheel = (sbyte)(_modWheel >> 2);
                    if (voice.S10b.Active != 0 && voice.S11b.Flag0x40 != 0)
                        voice.S10b.ModWheel = (sbyte)(_modWheel >> 2);
                }
            }

            public override void Volume(byte value)
            {
                _volEff = value;
                for (var voice = _voice; voice != null; voice = voice.Next)
                {
                    #if ENABLE_OPL3
                        if (!_owner->_opl3Mode) {
                    #endif
                    _owner.AdlibSetParam(voice.Channel, 0, volumeTable[volumeLookupTable[voice.Vol2, _volEff >> 2]]);
                    if (voice.TwoChan != 0)
                    {
                        _owner.AdlibSetParam(voice.Channel, 13, volumeTable[volumeLookupTable[voice.Vol1, _volEff >> 2]]);
                    }
                    #if ENABLE_OPL3
                        } else {
                        _owner->adlibSetParam(voice->_channel, 0, g_volumeTable[((voice->_vol2    + 1) * _volEff) >> 7], true);
                        _owner->adlibSetParam(voice->_channel, 0, g_volumeTable[((voice->_secVol2 + 1) * _volEff) >> 7], false);
                        if (voice->_twoChan) {
                        _owner->adlibSetParam(voice->_channel, 13, g_volumeTable[((voice->_vol1    + 1) * _volEff) >> 7], true);
                        }
                        if (voice->_secTwoChan) {
                        _owner->adlibSetParam(voice->_channel, 13, g_volumeTable[((voice->_secVol1 + 1) * _volEff) >> 7], false);
                        }
                        }
                    #endif
                }
            }

            public override void PanPosition(byte value)
            {
                _pan = value;
            }

            public override void PitchBendFactor(byte value)
            {
                #if ENABLE_OPL3
                // Not supported in OPL3 mode.
                if (_owner->_opl3Mode) {
                return;
                }
                #endif
                _pitchBendFactor = value;
                for (var voice = _voice; voice != null; voice = voice.Next)
                {
                    _owner.AdlibNoteOn(voice.Channel, voice.Note/* + _transposeEff*/,
                        (_pitchBend * _pitchBendFactor >> 6) + _detuneEff);
                }
            }

            public override void Detune(byte value)
            {
                // Sam&Max's OPL3 driver uses this for a completly different purpose. It
                // is related to voice allocation. We ignore this for now.
                // TODO: We probably need to look how the interpreter side of Sam&Max's
                // iMuse version handles all this too. Implementing the driver side here
                // would be not that hard.
                #if ENABLE_OPL3
                if (_owner.Opl3Mode) {
                //_maxNotes = value;
                return;
                }
                #endif

                _detuneEff = (sbyte)value;
                for (var voice = _voice; voice != null; voice = voice.Next)
                {
                    _owner.AdlibNoteOn(voice.Channel, voice.Note/* + _transposeEff*/,
                        (_pitchBend * _pitchBendFactor >> 6) + _detuneEff);
                }
            }

            public override void Priority(byte value)
            {
                _priEff = value;
            }

            public override void Sustain(bool value)
            {

                _pedal = value;
                if (!value)
                {
                    for (var voice = _voice; voice != null; voice = voice.Next)
                    {
                        if (voice.WaitForPedal)
                            _owner.McOff(voice);
                    }
                }
            }

            public override void EffectLevel(byte value)
            {
                return;
            }
            // Not supported
            public override void ChorusLevel(byte value)
            {
                return;
            }
            // Not supported
            public override void AllNotesOff()
            {
                while (_voice != null)
                    _owner.McOff(_voice);
            }

            // SysEx messages
            public override void SysExCustomInstrument(uint type, byte[] instr)
            {
                // Sam&Max allows for instrument overwrites, but we will not support it
                // until we can find any track actually using it.
                #if ENABLE_OPL3
                if (_owner->_opl3Mode) {
                warning("AdLibPart::sysEx_customInstrument: Used in OPL3 mode");
                return;
                }
                #endif

                if (type == ToType("ADL "))
                {
                    _partInstr.ModCharacteristic = instr[0];
                    _partInstr.ModScalingOutputLevel = instr[1];
                    _partInstr.ModAttackDecay = instr[2];
                    _partInstr.ModSustainRelease = instr[3];
                    _partInstr.ModWaveformSelect = instr[4];
                    _partInstr.CarCharacteristic = instr[5];
                    _partInstr.CarScalingOutputLevel = instr[6];
                    _partInstr.CarAttackDecay = instr[7];
                    _partInstr.CarSustainRelease = instr[8];
                    _partInstr.CarWaveformSelect = instr[9];
                    _partInstr.Feedback = instr[10];
                    _partInstr.FlagsA = instr[11];
                    _partInstr.ExtraA.A = instr[12];
                    _partInstr.ExtraA.B = instr[13];
                    _partInstr.ExtraA.C = instr[14];
                    _partInstr.ExtraA.D = instr[15];
                    _partInstr.ExtraA.E = instr[16];
                    _partInstr.ExtraA.F = instr[17];
                    _partInstr.ExtraA.G = instr[18];
                    _partInstr.FlagsB = instr[19];
                    _partInstr.ExtraB.A = instr[20];
                    _partInstr.ExtraB.B = instr[21];
                    _partInstr.ExtraB.C = instr[22];
                    _partInstr.ExtraB.D = instr[23];
                    _partInstr.ExtraB.E = instr[24];
                    _partInstr.ExtraB.F = instr[25];
                    _partInstr.ExtraB.G = instr[26];
                    _partInstr.Duration = instr[27];
                }
            }

            //  AdLibPart *_prev, *_next;
            internal protected AdLibVoice _voice;
            internal protected short _pitchBend;
            internal protected byte _pitchBendFactor;
            //int8 _transposeEff;
            internal protected byte _volEff;
            internal protected sbyte _detuneEff;
            internal protected byte _modWheel;
            internal protected bool _pedal;
            protected byte _program;
            internal protected byte _priEff;
            protected byte _pan;
            protected AdLibInstrument _partInstr;
            #if ENABLE_OPL3
            AdLibInstrument _partInstrSecondary;
            #endif

            protected AdLibMidiDriver _owner;
            internal protected bool _allocated;
            protected byte _channel;

            static readonly AdLibInstrument[] gmInstruments = new AdLibInstrument[]
            {
                // 0x00
                new AdLibInstrument(new byte []{ 0xC2, 0xC5, 0x2B, 0x99, 0x58, 0xC2, 0x1F, 0x1E, 0xC8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x23),
                new AdLibInstrument(new byte []{ 0x22, 0x53, 0x0E, 0x8A, 0x30, 0x14, 0x06, 0x1D, 0x7A, 0x5C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x06, 0x00, 0x1C, 0x79, 0x40, 0x02, 0x00, 0x4B, 0x79, 0x58, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC2, 0x89, 0x2A, 0x89, 0x49, 0xC2, 0x16, 0x1C, 0xB8, 0x7C, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x23),
                new AdLibInstrument(new byte []{ 0xC2, 0x17, 0x3D, 0x6A, 0x00, 0xC4, 0x2E, 0x2D, 0xC9, 0x20, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x06, 0x1E, 0x1C, 0x99, 0x00, 0x02, 0x3A, 0x4C, 0x79, 0x00, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x84, 0x40, 0x3B, 0x5A, 0x6F, 0x81, 0x0E, 0x3B, 0x5A, 0x7F, 0x0B, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x84, 0x40, 0x3B, 0x5A, 0x63, 0x81, 0x00, 0x3B, 0x5A, 0x7F, 0x01, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x8C, 0x80, 0x05, 0xEA, 0x59, 0x82, 0x0A, 0x3C, 0xAA, 0x64, 0x07, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x85, 0x40, 0x0D, 0xEC, 0x71, 0x84, 0x58, 0x3E, 0xCB, 0x7C, 0x01, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x8A, 0xC0, 0x0C, 0xDC, 0x50, 0x88, 0x58, 0x3D, 0xDA, 0x7C, 0x01, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC9, 0x40, 0x2B, 0x78, 0x42, 0xC2, 0x04, 0x4C, 0x8A, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x1A),
                new AdLibInstrument(new byte []{ 0x2A, 0x0E, 0x17, 0x89, 0x28, 0x22, 0x0C, 0x1B, 0x09, 0x70, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE7, 0x9B, 0x08, 0x08, 0x26, 0xE2, 0x06, 0x0A, 0x08, 0x70, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC5, 0x05, 0x00, 0xFC, 0x40, 0x84, 0x00, 0x00, 0xDC, 0x50, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x86, 0x40, 0x5D, 0x5A, 0x41, 0x81, 0x00, 0x0B, 0x5A, 0x7F, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                // 0x10
                new AdLibInstrument(new byte []{ 0xED, 0x00, 0x7B, 0xC8, 0x40, 0xE1, 0x99, 0x4A, 0xE9, 0x7E, 0x07, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE8, 0x4F, 0x3A, 0xD7, 0x7C, 0xE2, 0x97, 0x49, 0xF9, 0x7D, 0x05, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x10, 0x2F, 0xF7, 0x7D, 0xF3, 0x45, 0x8F, 0xC7, 0x62, 0x07, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x01, 0x8C, 0x9F, 0xDA, 0x70, 0xE4, 0x50, 0x9F, 0xDA, 0x6A, 0x09, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x08, 0xD5, 0x9D, 0xA5, 0x45, 0xE2, 0x3F, 0x9F, 0xD6, 0x49, 0x07, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE5, 0x0F, 0x7D, 0xB8, 0x2E, 0xA2, 0x0F, 0x7C, 0xC7, 0x61, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x2A, 0x9F, 0xDB, 0x01, 0xE1, 0x04, 0x8F, 0xD7, 0x62, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x88, 0x9C, 0x50, 0x64, 0xE2, 0x18, 0x70, 0xC4, 0x7C, 0x0B, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x02, 0xA3, 0x0D, 0xDA, 0x01, 0xC2, 0x35, 0x5D, 0x58, 0x00, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x18),
                new AdLibInstrument(new byte []{ 0x42, 0x55, 0x3E, 0xEB, 0x24, 0xD4, 0x08, 0x0D, 0xA9, 0x71, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x18),
                new AdLibInstrument(new byte []{ 0xC2, 0x00, 0x2B, 0x17, 0x51, 0xC2, 0x1E, 0x4D, 0x97, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x19),
                new AdLibInstrument(new byte []{ 0xC6, 0x01, 0x2D, 0xA7, 0x44, 0xC2, 0x06, 0x0E, 0xA7, 0x79, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC2, 0x0C, 0x06, 0x06, 0x55, 0xC2, 0x3F, 0x09, 0x86, 0x7D, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0A),
                new AdLibInstrument(new byte []{ 0xC2, 0x2E, 0x4F, 0x77, 0x00, 0xC4, 0x08, 0x0E, 0x98, 0x59, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC2, 0x30, 0x4F, 0xCA, 0x01, 0xC4, 0x0D, 0x0E, 0xB8, 0x7F, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC4, 0x29, 0x4F, 0xCA, 0x03, 0xC8, 0x0D, 0x0C, 0xB7, 0x7D, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0B),
                // 0x20
                new AdLibInstrument(new byte []{ 0xC2, 0x40, 0x3C, 0x96, 0x58, 0xC4, 0xDE, 0x0E, 0xC7, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x20),
                new AdLibInstrument(new byte []{ 0x31, 0x13, 0x2D, 0xD7, 0x3C, 0xE2, 0x18, 0x2E, 0xB8, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x22, 0x86, 0x0D, 0xD7, 0x50, 0xE4, 0x18, 0x5E, 0xB8, 0x7C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x28),
                new AdLibInstrument(new byte []{ 0xF2, 0x0A, 0x0D, 0xD7, 0x40, 0xE4, 0x1F, 0x5E, 0xB8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x09, 0x4B, 0xD6, 0x48, 0xE4, 0x1F, 0x1C, 0xB8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x28),
                new AdLibInstrument(new byte []{ 0x62, 0x11, 0x0C, 0xE6, 0x3C, 0xE4, 0x1F, 0x0C, 0xC8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x12, 0x3D, 0xE6, 0x34, 0xE4, 0x1F, 0x7D, 0xB8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x13, 0x3D, 0xE6, 0x34, 0xE4, 0x1F, 0x5D, 0xB8, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xA2, 0x40, 0x5D, 0xBA, 0x3F, 0xE2, 0x00, 0x8F, 0xD8, 0x79, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x40, 0x3D, 0xDA, 0x3B, 0xE1, 0x00, 0x7E, 0xD8, 0x7A, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x62, 0x00, 0x6D, 0xFA, 0x5D, 0xE2, 0x00, 0x8F, 0xC8, 0x79, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x00, 0x4E, 0xDB, 0x4A, 0xE3, 0x18, 0x6F, 0xE9, 0x7E, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x00, 0x4E, 0xDB, 0x66, 0xE2, 0x00, 0x7F, 0xE9, 0x7E, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x02, 0x0F, 0x66, 0xAA, 0x51, 0x02, 0x64, 0x29, 0xF9, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
                new AdLibInstrument(new byte []{ 0x16, 0x4A, 0x04, 0xBA, 0x39, 0xC2, 0x58, 0x2D, 0xCA, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
                new AdLibInstrument(new byte []{ 0x02, 0x00, 0x01, 0x7A, 0x79, 0x02, 0x3F, 0x28, 0xEA, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                // 0x30
                new AdLibInstrument(new byte []{ 0x62, 0x53, 0x9C, 0xBA, 0x31, 0x62, 0x5B, 0xAD, 0xC9, 0x55, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x40, 0x6E, 0xDA, 0x49, 0xE2, 0x13, 0x8F, 0xF9, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x40, 0x8F, 0xFA, 0x50, 0xF2, 0x04, 0x7F, 0xFA, 0x7D, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0xA0, 0xCE, 0x5B, 0x02, 0xE2, 0x32, 0x7F, 0xFB, 0x3D, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE6, 0x80, 0x9C, 0x99, 0x42, 0xE2, 0x04, 0x7D, 0x78, 0x60, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xEA, 0xA0, 0xAC, 0x67, 0x02, 0xE2, 0x00, 0x7C, 0x7A, 0x7C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE7, 0x94, 0xAD, 0xB7, 0x03, 0xE2, 0x00, 0x7C, 0xBA, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC3, 0x3F, 0x4B, 0xE9, 0x7E, 0xC1, 0x3F, 0x9B, 0xF9, 0x7F, 0x0B, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x06),
                new AdLibInstrument(new byte []{ 0xB2, 0x20, 0xAD, 0xE9, 0x00, 0x62, 0x05, 0x8F, 0xC8, 0x68, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x00, 0x8F, 0xFB, 0x50, 0xF6, 0x47, 0x8F, 0xE9, 0x68, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x00, 0xAF, 0x88, 0x58, 0xF2, 0x54, 0x6E, 0xC9, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xF2, 0x2A, 0x9F, 0x98, 0x01, 0xE2, 0x84, 0x4E, 0x78, 0x6C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x02, 0x9F, 0xB8, 0x48, 0x22, 0x89, 0x9F, 0xE8, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x2A, 0x7F, 0xB8, 0x01, 0xE4, 0x00, 0x0D, 0xC5, 0x7C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x28, 0x8E, 0xE8, 0x01, 0xF2, 0x00, 0x4D, 0xD6, 0x7D, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x62, 0x23, 0x8F, 0xEA, 0x00, 0xF2, 0x00, 0x5E, 0xD9, 0x7C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                // 0x40
                new AdLibInstrument(new byte []{ 0xB4, 0x26, 0x6E, 0x98, 0x01, 0x62, 0x00, 0x7D, 0xC8, 0x7D, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x2E, 0x20, 0xD9, 0x01, 0xF2, 0x0F, 0x90, 0xF8, 0x78, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x28, 0x7E, 0xF8, 0x01, 0xE2, 0x23, 0x8E, 0xE8, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xB8, 0x28, 0x9E, 0x98, 0x01, 0x62, 0x00, 0x3D, 0xC8, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x62, 0x00, 0x8E, 0xC9, 0x3D, 0xE6, 0x00, 0x7E, 0xD8, 0x68, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x00, 0x5F, 0xF9, 0x48, 0xE6, 0x98, 0x8F, 0xF8, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x62, 0x0C, 0x6E, 0xD8, 0x3D, 0x2A, 0x06, 0x7D, 0xD8, 0x58, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x00, 0x7E, 0x89, 0x38, 0xE6, 0x84, 0x80, 0xF8, 0x68, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x80, 0x6C, 0xD9, 0x30, 0xE2, 0x00, 0x8D, 0xC8, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x80, 0x88, 0x48, 0x40, 0xE2, 0x0A, 0x7D, 0xA8, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x00, 0x77, 0xC5, 0x54, 0xE2, 0x00, 0x9E, 0xD7, 0x70, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x80, 0x86, 0xB9, 0x64, 0xE2, 0x05, 0x9F, 0xD7, 0x78, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x00, 0x68, 0x68, 0x56, 0xE2, 0x08, 0x9B, 0xB3, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x00, 0xA6, 0x87, 0x41, 0xE2, 0x0A, 0x7E, 0xC9, 0x7C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x80, 0x9A, 0xB8, 0x48, 0xE2, 0x00, 0x9E, 0xF9, 0x60, 0x09, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x80, 0x8E, 0x64, 0x68, 0xE2, 0x28, 0x6F, 0x73, 0x7C, 0x01, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                // 0x50
                new AdLibInstrument(new byte []{ 0xE8, 0x00, 0x7D, 0x99, 0x54, 0xE6, 0x80, 0x80, 0xF8, 0x7C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE6, 0x00, 0x9F, 0xB9, 0x6D, 0xE1, 0x00, 0x8F, 0xC8, 0x7D, 0x02, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x00, 0x09, 0x68, 0x4A, 0xE2, 0x2B, 0x9E, 0xF3, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC4, 0x00, 0x99, 0xE8, 0x3B, 0xE2, 0x25, 0x6F, 0x93, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE6, 0x00, 0x6F, 0xDA, 0x69, 0xE2, 0x05, 0x2F, 0xD8, 0x6A, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xEC, 0x60, 0x9D, 0xC7, 0x00, 0xE2, 0x21, 0x7F, 0xC9, 0x7C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE3, 0x00, 0x0F, 0xF7, 0x7D, 0xE1, 0x3F, 0x0F, 0xA7, 0x01, 0x0D, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0xA9, 0x0F, 0xA8, 0x02, 0xE2, 0x3C, 0x5F, 0xDA, 0x3C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE8, 0x40, 0x0D, 0x89, 0x7D, 0xE2, 0x17, 0x7E, 0xD9, 0x7C, 0x07, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x00, 0xDF, 0x8A, 0x56, 0xE2, 0x5E, 0xCF, 0xBA, 0x7E, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE2, 0x00, 0x0B, 0x68, 0x60, 0xE2, 0x01, 0x9E, 0xB8, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xEA, 0x00, 0xAE, 0xAB, 0x49, 0xE2, 0x00, 0xAE, 0xBA, 0x6C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xEB, 0x80, 0x8C, 0xCB, 0x3A, 0xE2, 0x86, 0xAF, 0xCA, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE5, 0x40, 0xDB, 0x3B, 0x3C, 0xE2, 0x80, 0xBE, 0xCA, 0x71, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE4, 0x00, 0x9E, 0xAA, 0x3D, 0xE1, 0x43, 0x0F, 0xBA, 0x7E, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE7, 0x40, 0xEC, 0xCA, 0x44, 0xE2, 0x03, 0xBF, 0xBA, 0x66, 0x02, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                // 0x60
                new AdLibInstrument(new byte []{ 0xEA, 0x00, 0x68, 0xB8, 0x48, 0xE2, 0x0A, 0x8E, 0xB8, 0x7C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x61, 0x00, 0xBE, 0x99, 0x7E, 0xE3, 0x40, 0xCF, 0xCA, 0x7D, 0x09, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xCD, 0x00, 0x0B, 0x00, 0x48, 0xC2, 0x58, 0x0C, 0x00, 0x7C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x1C),
                new AdLibInstrument(new byte []{ 0xE2, 0x00, 0x0E, 0x00, 0x52, 0xE2, 0x58, 0x5F, 0xD0, 0x7D, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xCC, 0x00, 0x7D, 0xDA, 0x40, 0xC2, 0x00, 0x5E, 0x9B, 0x58, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE9, 0xC0, 0xEE, 0xD8, 0x43, 0xE2, 0x05, 0xDD, 0xAA, 0x70, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xDA, 0x00, 0x8F, 0xAC, 0x4A, 0x22, 0x05, 0x8D, 0x8A, 0x75, 0x02, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0x62, 0x8A, 0xCB, 0x7A, 0x74, 0xE6, 0x56, 0xAF, 0xDB, 0x70, 0x02, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xC2, 0x41, 0xAC, 0x5B, 0x5B, 0xC2, 0x80, 0x0D, 0xCB, 0x7D, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x12),
                new AdLibInstrument(new byte []{ 0x75, 0x00, 0x0E, 0xCB, 0x5A, 0xE2, 0x1E, 0x0A, 0xC9, 0x7D, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x10),
                new AdLibInstrument(new byte []{ 0x41, 0x00, 0x0E, 0xEA, 0x53, 0xC2, 0x00, 0x08, 0xCA, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x07),
                new AdLibInstrument(new byte []{ 0xC1, 0x40, 0x0C, 0x59, 0x6A, 0xC2, 0x80, 0x3C, 0xAB, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0D),
                new AdLibInstrument(new byte []{ 0x4B, 0x00, 0x0A, 0xF5, 0x61, 0xC2, 0x19, 0x0C, 0xE9, 0x7C, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x07),
                new AdLibInstrument(new byte []{ 0x62, 0x00, 0x7F, 0xD8, 0x54, 0xEA, 0x00, 0x8F, 0xD8, 0x7D, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x00, 0x7F, 0xD9, 0x56, 0xE1, 0x00, 0x8F, 0xD8, 0x7E, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte []{ 0xE1, 0x00, 0x7F, 0xD9, 0x56, 0xE1, 0x00, 0x8F, 0xD8, 0x7E, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                // 0x70
                new AdLibInstrument(new byte []{ 0xCF, 0x40, 0x09, 0xEA, 0x54, 0xC4, 0x00, 0x0C, 0xDB, 0x64, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte []{ 0xCF, 0x40, 0x0C, 0xAA, 0x54, 0xC4, 0x00, 0x18, 0xF9, 0x64, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte []{ 0xC9, 0x0E, 0x88, 0xD9, 0x3E, 0xC2, 0x08, 0x1A, 0xEA, 0x6C, 0x0C, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
                new AdLibInstrument(new byte []{ 0x03, 0x00, 0x15, 0x00, 0x64, 0x02, 0x00, 0x08, 0x00, 0x7C, 0x09, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte []{ 0x01, 0x00, 0x47, 0xD7, 0x6C, 0x01, 0x3F, 0x0C, 0xFB, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
                new AdLibInstrument(new byte []{ 0x00, 0x00, 0x36, 0x67, 0x7C, 0x01, 0x3F, 0x0E, 0xFA, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
                new AdLibInstrument(new byte []{ 0x02, 0x00, 0x36, 0x68, 0x7C, 0x01, 0x3F, 0x0E, 0xFA, 0x7C, 0x00, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
                new AdLibInstrument(new byte []{ 0xCB, 0x00, 0xAF, 0x00, 0x7E, 0xC0, 0x00, 0xC0, 0x06, 0x7F, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0F),
                new AdLibInstrument(new byte []{ 0x05, 0x0D, 0x80, 0xA6, 0x7F, 0x0B, 0x38, 0xA9, 0xD8, 0x00, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
                new AdLibInstrument(new byte []{ 0x0F, 0x00, 0x90, 0xFA, 0x68, 0x06, 0x00, 0xA7, 0x39, 0x54, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x06),
                new AdLibInstrument(new byte []{ 0xC9, 0x15, 0xDD, 0xFF, 0x7C, 0x00, 0x00, 0xE7, 0xFC, 0x6C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x38),
                new AdLibInstrument(new byte []{ 0x48, 0x3C, 0x30, 0xF6, 0x03, 0x0A, 0x38, 0x97, 0xE8, 0x00, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
                new AdLibInstrument(new byte []{ 0x07, 0x80, 0x0B, 0xC8, 0x65, 0x02, 0x3F, 0x0C, 0xEA, 0x7C, 0x0F, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
                new AdLibInstrument(new byte []{ 0x00, 0x21, 0x66, 0x40, 0x03, 0x00, 0x3F, 0x47, 0x00, 0x00, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte []{ 0x08, 0x00, 0x0B, 0x3C, 0x7C, 0x08, 0x3F, 0x06, 0xF3, 0x00, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02)
            };
        }

        class AdLibPercussionChannel : AdLibPart
        {
            public AdLibPercussionChannel()
            {
                _notes = new byte[256];
                _customInstruments = new AdLibInstrument[256];
            }

            internal protected override void Init(AdLibMidiDriver owner, byte channel)
            {
                base.Init(owner, channel);
                _priEff = 0;
                _volEff = 127;

                // Initialize the custom instruments data
                Array.Clear(_notes, 0, _notes.Length);
                Array.Clear(_customInstruments, 0, _customInstruments.Length);
            }

            public override void NoteOff(byte note)
            {
                if (_customInstruments[note] != null)
                {
                    note = _notes[note];
                }

                // This used to ignore note off events, since the builtin percussion
                // instrument data has a duration value, which causes the percussion notes
                // to stop automatically. This is not the case for (Groovie's) custom
                // percussion instruments though. Also the OPL3 driver of Sam&Max actually
                // does not handle the duration value, so we need it there too.
                _owner.PartKeyOff(this, note);
            }

            public override void NoteOn(byte note, byte velocity)
            {
                AdLibInstrument inst = null;
                AdLibInstrument sec = null;

                // The custom instruments have priority over the default mapping
                // We do not support custom instruments in OPL3 mode though.
                #if ENABLE_OPL3
                if (!_owner->_opl3Mode) {
                #endif
                inst = _customInstruments[note];
                if (inst != null)
                    note = _notes[note];
                #if ENABLE_OPL3
                }
                #endif

                if (inst == null)
                {
                    // Use the default GM to FM mapping as a fallback
                    byte key = gmPercussionInstrumentMap[note];
                    if (key != 0xFF)
                    {
                        #if ENABLE_OPL3
                                if (!_owner->_opl3Mode) {
                        #endif
                        inst = gmPercussionInstruments[key];
                        #if ENABLE_OPL3
                                } else {
                                inst = &g_gmPercussionInstrumentsOPL3[key][0];
                                sec  = &g_gmPercussionInstrumentsOPL3[key][1];
                                }
                        #endif
                    }
                }

                if (inst == null)
                {
                    Debug.WriteLine("No instrument FM definition for GM percussion key {0}", (int)note);
                    return;
                }

                _owner.PartKeyOn(this, inst, note, velocity, sec, _pan);
            }

            public override void ProgramChange(byte program)
            {
            }

            // Control Change messages
            public override void ModulationWheel(byte value)
            {
            }

            public override void PitchBendFactor(byte value)
            {
            }

            public override void Detune(byte value)
            {
            }

            public override void Priority(byte value)
            {
            }

            public override void Sustain(bool value)
            {
            }

            // SysEx messages
            public override void SysExCustomInstrument(uint type, byte[] instr)
            {
                // We do not allow custom instruments in OPL3 mode right now.
                #if ENABLE_OPL3
                if (_owner->_opl3Mode) {
                warning("AdLibPercussionChannel::sysEx_customInstrument: Used in OPL3 mode");
                return;
                }
                #endif

                if (type == ToType("ADLP"))
                {
                    byte note = instr[0];
                    _notes[note] = instr[1];

                    // Allocate memory for the new instruments
                    if (_customInstruments[note] == null)
                    {
                        _customInstruments[note] = new AdLibInstrument();
                    }

                    // Save the new instrument data
                    _customInstruments[note].ModCharacteristic = instr[2];
                    _customInstruments[note].ModScalingOutputLevel = instr[3];
                    _customInstruments[note].ModAttackDecay = instr[4];
                    _customInstruments[note].ModSustainRelease = instr[5];
                    _customInstruments[note].ModWaveformSelect = instr[6];
                    _customInstruments[note].CarCharacteristic = instr[7];
                    _customInstruments[note].CarScalingOutputLevel = instr[8];
                    _customInstruments[note].CarAttackDecay = instr[9];
                    _customInstruments[note].CarSustainRelease = instr[10];
                    _customInstruments[note].CarWaveformSelect = instr[11];
                    _customInstruments[note].Feedback = instr[12];
                }
            }

            byte[] _notes;
            AdLibInstrument[] _customInstruments;

            static readonly byte[] gmPercussionInstrumentMap = new byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C,
                0x0D, 0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0xFF, 0xFF, 0x17, 0x18, 0x19, 0x1A,
                0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x21, 0x22, 0x23, 0xFF, 0xFF,
                0x24, 0x25, 0xFF, 0xFF, 0xFF, 0x26, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
            };

            static readonly AdLibInstrument[] gmPercussionInstruments = new AdLibInstrument[]
            {
                new AdLibInstrument(new byte[]{ 0x1A, 0x3F, 0x15, 0x05, 0x7C, 0x02, 0x21, 0x2B, 0xE4, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x06),
                new AdLibInstrument(new byte[]{ 0x11, 0x12, 0x04, 0x07, 0x7C, 0x02, 0x23, 0x0B, 0xE5, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
                new AdLibInstrument(new byte[]{ 0x0A, 0x3F, 0x0B, 0x01, 0x7C, 0x1F, 0x1C, 0x46, 0xD0, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x01),
                new AdLibInstrument(new byte[]{ 0x00, 0x3F, 0x0F, 0x00, 0x7C, 0x10, 0x12, 0x07, 0x00, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte[]{ 0x0F, 0x3F, 0x0B, 0x00, 0x7C, 0x1F, 0x0F, 0x19, 0xD0, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte[]{ 0x00, 0x3F, 0x1F, 0x00, 0x7E, 0x1F, 0x16, 0x07, 0x00, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
                new AdLibInstrument(new byte[]{ 0x12, 0x3F, 0x05, 0x06, 0x7C, 0x03, 0x1F, 0x4A, 0xD9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
                new AdLibInstrument(new byte[]{ 0xCF, 0x7F, 0x08, 0xFF, 0x7E, 0x00, 0xC7, 0x2D, 0xF7, 0x73, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte[]{ 0x12, 0x3F, 0x05, 0x06, 0x7C, 0x43, 0x21, 0x0C, 0xE9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
                new AdLibInstrument(new byte[]{ 0xCF, 0x7F, 0x08, 0xCF, 0x7E, 0x00, 0x45, 0x2A, 0xF8, 0x4B, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0C),
                new AdLibInstrument(new byte[]{ 0x12, 0x3F, 0x06, 0x17, 0x7C, 0x03, 0x27, 0x0B, 0xE9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
                new AdLibInstrument(new byte[]{ 0xCF, 0x7F, 0x08, 0xCD, 0x7E, 0x00, 0x40, 0x1A, 0x69, 0x63, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0C),
                new AdLibInstrument(new byte[]{ 0x13, 0x3F, 0x05, 0x06, 0x7C, 0x03, 0x17, 0x0A, 0xD9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
                new AdLibInstrument(new byte[]{ 0x15, 0x3F, 0x05, 0x06, 0x7C, 0x03, 0x21, 0x0C, 0xE9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
                new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x2B, 0xFB, 0x7E, 0xC0, 0x1E, 0x1A, 0xCA, 0x7F, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x10),
                new AdLibInstrument(new byte[]{ 0x17, 0x3F, 0x04, 0x09, 0x7C, 0x03, 0x22, 0x0D, 0xE9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
                new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x0F, 0x5E, 0x7C, 0xC6, 0x13, 0x00, 0xCA, 0x7F, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
                new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x7E, 0x9D, 0x7C, 0xC8, 0xC0, 0x0A, 0xBA, 0x74, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x06),
                new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x4D, 0x9F, 0x7C, 0xC6, 0x00, 0x08, 0xDA, 0x5B, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
                new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x5D, 0xAA, 0x7A, 0xC0, 0xA4, 0x67, 0x99, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x4A, 0xFD, 0x7C, 0xCF, 0x00, 0x59, 0xEA, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte[]{ 0x0F, 0x18, 0x0A, 0xFA, 0x57, 0x06, 0x07, 0x06, 0x39, 0x7C, 0x0A, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte[]{ 0xCF, 0x3F, 0x2B, 0xFC, 0x7C, 0xCC, 0xC6, 0x0B, 0xEA, 0x7F, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x10),
                new AdLibInstrument(new byte[]{ 0x05, 0x1A, 0x04, 0x00, 0x7C, 0x12, 0x10, 0x0C, 0xEA, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x07),
                new AdLibInstrument(new byte[]{ 0x04, 0x19, 0x04, 0x00, 0x7C, 0x12, 0x10, 0x2C, 0xEA, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x04),
                new AdLibInstrument(new byte[]{ 0x04, 0x0A, 0x04, 0x00, 0x6C, 0x01, 0x07, 0x0D, 0xFA, 0x74, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x07),
                new AdLibInstrument(new byte[]{ 0x15, 0x14, 0x05, 0x00, 0x7D, 0x01, 0x07, 0x5C, 0xE9, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
                new AdLibInstrument(new byte[]{ 0x10, 0x10, 0x05, 0x08, 0x7C, 0x01, 0x08, 0x0D, 0xEA, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x05),
                new AdLibInstrument(new byte[]{ 0x11, 0x00, 0x06, 0x87, 0x7F, 0x02, 0x40, 0x09, 0x59, 0x68, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x08),
                new AdLibInstrument(new byte[]{ 0x13, 0x26, 0x04, 0x6A, 0x7F, 0x01, 0x00, 0x08, 0x5A, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x08),
                new AdLibInstrument(new byte[]{ 0xCF, 0x4E, 0x0C, 0xAA, 0x50, 0xC4, 0x00, 0x18, 0xF9, 0x54, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte[]{ 0xCF, 0x4E, 0x0C, 0xAA, 0x50, 0xC3, 0x00, 0x18, 0xF8, 0x54, 0x04, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte[]{ 0xCB, 0x3F, 0x8F, 0x00, 0x7E, 0xC5, 0x00, 0x98, 0xD6, 0x5F, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x0D),
                new AdLibInstrument(new byte[]{ 0x0C, 0x18, 0x87, 0xB3, 0x7F, 0x19, 0x10, 0x55, 0x75, 0x7C, 0x0E, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte[]{ 0x05, 0x11, 0x15, 0x00, 0x64, 0x02, 0x08, 0x08, 0x00, 0x5C, 0x09, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte[]{ 0x04, 0x08, 0x15, 0x00, 0x48, 0x01, 0x08, 0x08, 0x00, 0x60, 0x08, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x02),
                new AdLibInstrument(new byte[]{ 0xDA, 0x00, 0x53, 0x30, 0x68, 0x07, 0x1E, 0x49, 0xC4, 0x7E, 0x03, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00),
                new AdLibInstrument(new byte[]{ 0x1C, 0x00, 0x07, 0xBC, 0x6C, 0x0C, 0x14, 0x0B, 0x6A, 0x7E, 0x0B, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x03),
                new AdLibInstrument(new byte[]{ 0x0A, 0x0E, 0x7F, 0x00, 0x7D, 0x13, 0x20, 0x28, 0x03, 0x7C, 0x06, 0 }, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0, new InstrumentExtra(new byte[]{ 0, 0, 0, 0, 0, 0, 0, 0 }), 0x00)
            };
        }

        class AdLibVoice
        {
            public AdLibPart Part;
            public AdLibVoice Next, Prev;
            public bool WaitForPedal;
            public byte Note;
            public byte Channel;
            public byte TwoChan;
            public byte Vol1, Vol2;
            public short Duration;

            public Struct10 S10a;
            public Struct11 S11a;
            public Struct10 S10b;
            public Struct11 S11b;

            #if ENABLE_OPL3
            byte _secTwoChan;
            byte _secVol1, _secVol2;
            #endif

            public AdLibVoice()
            {
                S10a = new Struct10();
                S11a = new Struct11();
                S10b = new Struct10();
                S11b = new Struct11();
            }
        }

        struct AdLibSetParams
        {
            public byte RegisterBase;
            public byte Shift;
            public byte Mask;
            public byte Inversion;

            public AdLibSetParams(byte[] data)
            {
                RegisterBase = data[0];
                Shift = data[1];
                Mask = data[2];
                Inversion = data[3];
            }
        }

        static readonly byte[] noteFrequencies = new byte[]
        {
            90, 91, 92, 92, 93, 94, 94, 95,
            96, 96, 97, 98, 98, 99, 100, 101,
            101, 102, 103, 104, 104, 105, 106, 107,
            107, 108, 109, 110, 111, 111, 112, 113,
            114, 115, 115, 116, 117, 118, 119, 120,
            121, 121, 122, 123, 124, 125, 126, 127,
            128, 129, 130, 131, 132, 132, 133, 134,
            135, 136, 137, 138, 139, 140, 141, 142,
            143, 145, 146, 147, 148, 149, 150, 151,
            152, 153, 154, 155, 157, 158, 159, 160,
            161, 162, 163, 165, 166, 167, 168, 169,
            171, 172, 173, 174, 176, 177, 178, 180,
            181, 182, 184, 185, 186, 188, 189, 190,
            192, 193, 194, 196, 197, 199, 200, 202,
            203, 205, 206, 208, 209, 211, 212, 214,
            215, 217, 218, 220, 222, 223, 225, 226,
            228, 230, 231, 233, 235, 236, 238, 240,
            242, 243, 245, 247, 249, 251, 252, 254
        };

        static readonly ushort[] numStepsTable = new ushort[]
        {
            1, 2, 4, 5,
            6, 7, 8, 9,
            10, 12, 14, 16,
            18, 21, 24, 30,
            36, 50, 64, 82,
            100, 136, 160, 192,
            240, 276, 340, 460,
            600, 860, 1200, 1600
        };
        static readonly byte[] paramTable1 = new byte[]
        {
            29, 28, 27, 0,
            3, 4, 7, 8,
            13, 16, 17, 20,
            21, 30, 31, 0
        };

        static readonly ushort[] maxValTable = new ushort[]
        {
            0x2FF, 0x1F, 0x7, 0x3F,
            0x0F, 0x0F, 0x0F, 0x3,
            0x3F, 0x0F, 0x0F, 0x0F,
            0x3, 0x3E, 0x1F, 0
        };

        static readonly byte[] volumeTable = new byte[]
        {
            0, 4, 7, 11,
            13, 16, 18, 20,
            22, 24, 26, 27,
            29, 30, 31, 33,
            34, 35, 36, 37,
            38, 39, 40, 41,
            42, 43, 44, 44,
            45, 46, 47, 47,
            48, 49, 49, 50,
            51, 51, 52, 53,
            53, 54, 54, 55,
            55, 56, 56, 57,
            57, 58, 58, 59,
            59, 60, 60, 60,
            61, 61, 62, 62,
            62, 63, 63, 63
        };

        static readonly byte[,] volumeLookupTable = CreateLookupTable();

        static byte[,] CreateLookupTable()
        {
            int sum;
            var table = new byte[64, 32];
            for (var i = 0; i < 64; i++)
            {
                sum = i;
                for (var j = 0; j < 32; j++)
                {
                    table[i, j] = (byte)(sum >> 5);
                    sum += i;
                }
            }
            for (var i = 0; i < 64; i++)
                table[i, 0] = 0;

            return table;
        }

        static readonly byte[] operator1Offsets = new byte[9]
        {
            0, 1, 2, 8,
            9, 10, 16, 17,
            18
        };

        static readonly byte[] operator2Offsets = new byte[9]
        {
            3, 4, 5, 11,
            12, 13, 19, 20,
            21
        };

        static readonly AdLibSetParams[] setParamTable = new AdLibSetParams[]
        {
            new AdLibSetParams(new byte[]{ 0x40, 0, 63, 63 }),  // level
            new AdLibSetParams(new byte[]{ 0xE0, 2, 0, 0 }),    // unused
            new AdLibSetParams(new byte[]{ 0x40, 6, 192, 0 }),  // level key scaling
            new AdLibSetParams(new byte[]{ 0x20, 0, 15, 0 }),   // modulator frequency multiple
            new AdLibSetParams(new byte[]{ 0x60, 4, 240, 15 }), // attack rate
            new AdLibSetParams(new byte[]{ 0x60, 0, 15, 15 }),  // decay rate
            new AdLibSetParams(new byte[]{ 0x80, 4, 240, 15 }), // sustain level
            new AdLibSetParams(new byte[]{ 0x80, 0, 15, 15 }),  // release rate
            new AdLibSetParams(new byte[]{ 0xE0, 0, 3, 0 }),    // waveformSelect select
            new AdLibSetParams(new byte[]{ 0x20, 7, 128, 0 }),  // amp mod
            new AdLibSetParams(new byte[]{ 0x20, 6, 64, 0 }),   // vib
            new AdLibSetParams(new byte[]{ 0x20, 5, 32, 0 }),   // eg typ
            new AdLibSetParams(new byte[]{ 0x20, 4, 16, 0 }),   // ksr
            new AdLibSetParams(new byte[]{ 0xC0, 0, 1, 0 }),    // decay alg
            new AdLibSetParams(new byte[]{ 0xC0, 1, 14, 0 })    // feedback
        };

        /// <summary>
        /// FIXME: This flag controls a special mode for SCUMM V3 games
        /// </summary>
        bool _scummSmallHeader;
        #if ENABLE_OPL3
    bool _opl3Mode;
#endif

        IOpl _opl;
        byte[] _regCache;
        #if ENABLE_OPL3
    byte *_regCacheSecondary;
#endif

        int _timerCounter;

        ushort[] _channelTable2 = new ushort[9];
        int _voiceIndex;
        int _timerIncrease;
        int _timerThreshold;
        ushort[] _curNotTable = new ushort[9];
        AdLibVoice[] _voices;
        //[9];
        AdLibPart[] _parts;
        AdLibPercussionChannel _percussion;
    }
}
