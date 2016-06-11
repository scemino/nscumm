//
//  AdLibMidiDriver.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.OPL;
using NScumm.Core.Audio.OPL.DosBox;
using NScumm.Core.Audio.SoftSynth;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    class AdLibMidiDriver : EmulatedMidiDriver
    {
        IOpl _opl;
        int _midiNumberOfChannels;
        int _adlibNoteMul;
        int _adlibWaveformSelect;
        int _adlibAMDepthEq48;
        int _adlibVibratoDepthEq14;
        int _adlibRhythmEnabled;
        int _adlibKeyboardSplitOn;
        int _adlibVibratoRhythm;
        byte[] _midiChannelsFreqTable = new byte[9];
        byte[] _adlibChannelsLevelKeyScalingTable = new byte[11];
        byte[] _adlibSetupChannelSequence1 = new byte[14 * 18];
        ushort[] _adlibSetupChannelSequence2 = new ushort[14];
        short[] _midiChannelsNote2Table = new short[9];
        byte[] _midiChannelsNote1Table = new byte[9];
        byte[] _midiChannelsOctTable = new byte[9];
        ushort[] _adlibChannelsVolume = new ushort[11];
        ushort[] _adlibMetaSequenceData = new ushort[28];

        static readonly byte[] _adlibChannelsMappingTable1 =
        {
      0, 1, 2, 3, 4, 5, 8, 9, 10, 11, 12, 13, 16, 17, 18, 19, 20, 21
    };

        static readonly byte[] _adlibChannelsNoFeedback =
        {
      0, 0, 0, 1, 1, 1, 0, 0, 0, 1, 1, 1, 0, 0, 0, 1, 1, 1
    };

        static readonly byte[] _adlibChannelsMappingTable2 =
        {
      0, 1, 2, 0, 1, 2, 3, 4, 5, 3, 4, 5, 6, 7, 8, 6, 7, 8
    };

        static readonly byte[] _adlibChannelsMappingTable3 =
        {
      0, 1, 2, 0, 1, 2, 3, 4, 5, 3, 4, 5, 6, 10, 8, 6, 7, 9
    };

        static readonly byte[] _adlibChannelsKeyScalingTable1 =
        {
      0, 3, 1, 4, 2, 5, 6, 9, 7, 10, 8, 11, 12, 15, 13, 16, 14, 17
    };

        static readonly byte[] _adlibChannelsKeyScalingTable2 =
        {
      0, 3, 1, 4, 2, 5, 6, 9, 7, 10, 8, 11, 12, 15, 16, 255, 14, 255, 17, 255, 13, 255
    };

        static readonly byte[] _adlibChannelsVolumeTable =
        {
      128, 128, 128, 128, 128, 128, 128, 128, 128, 128, 128
    };

        static readonly byte[] _adlibInitSequenceData1 =
        {
      1, 1, 3, 15, 5, 0, 1, 3, 15, 0, 0, 0, 1, 0
    };

        static readonly byte[] _adlibInitSequenceData2 =
        {
      0, 1, 1, 15, 7, 0, 2, 4, 0, 0, 0, 1, 0, 0
    };

        static readonly byte[] _adlibInitSequenceData3 =
        {
      0, 0, 0, 10, 4, 0, 8, 12, 11, 0, 0, 0, 1, 0
    };

        static readonly byte[] _adlibInitSequenceData4 =
        {
      0, 0, 0, 13, 4, 0, 6, 15, 0, 0, 0, 0, 1, 0
    };

        static readonly byte[] _adlibInitSequenceData5 =
        {
      0, 12, 0, 15, 11, 0, 8, 5, 0, 0, 0, 0, 0, 0
    };

        static readonly byte[] _adlibInitSequenceData6 =
        {
      0, 4, 0, 15, 11, 0, 7, 5, 0, 0, 0, 0, 0, 0
    };

        static readonly byte[] _adlibInitSequenceData7 =
        {
      0, 1, 0, 15, 11, 0, 5, 5, 0, 0, 0, 0, 0, 0
    };

        static readonly byte[] _adlibInitSequenceData8 =
        {
      0, 1, 0, 15, 11, 0, 7, 5, 0, 0, 0, 0, 0, 0
    };

        static readonly short[] _midiChannelsNoteTable =
        {
      256, 256, 256, 256, 256, 256, 256, 256, 256, 256, 256
    };

        static readonly short[] _midiNoteFreqTable =
        {
      690, 692, 695, 697, 700, 702, 705, 707, 710, 713, 715, 718,
      720, 723, 726, 728, 731, 733, 736, 739, 741, 744, 747, 749,
      752, 755, 758, 760, 763, 766, 769, 771, 774, 777, 780, 783,
      785, 788, 791, 794, 797, 800, 803, 806, 809, 811, 814, 817,
      820, 823, 826, 829, 832, 835, 838, 841, 844, 847, 850, 854,
      857, 860, 863, 866, 869, 872, 875, 879, 882, 885, 888, 891,
      895, 898, 901, 904, 908, 911, 914, 917, 921, 924, 927, 931,
      934, 937, 941, 944, 948, 951, 955, 958, 961, 965, 968, 972,
      975, 979, 983, 986, 990, 993, 997, 1000, 1004, 1008, 1011, 1015,
      1019, 1022, -511, -509, -507, -505, -504, -502, -500, -498, -496, -494,
      -492, -490, -488, -486, -484, -482, -480, -479, -477, -475, -473, -471,
      -469, -467, -465, -463, -460, -458, -456, -454, -452, -450, -448, -446,
      -444, -442, -440, -438, -436, -433, -431, -429, -427, -425, -423, -420,
      -418, -416, -414, -412, -409, -407, -405, -403, -401, -398, -396, -394,
      -391, -389, -387, -385, -382, -380, -378, -375, -373, -371, -368, -366,
      -363, -361, -359, -356, -354, -351, -349, -347, -344, -342, -339, -337
    };

        public AdLibMidiDriver(IMixer mixer)
          : base(mixer)
        {
        }

        public override void Dispose()
        {
            _mixer.StopHandle(_mixerSoundHandle);
            base.Dispose();
        }

        public override bool IsStereo
        {
            get { return false; }
        }

        public override int Rate
        {
            get { return _mixer.OutputRate; }
        }

        public override MidiDriverError Open()
        {
            base.Open();
            _opl = MakeAdLibOPL(Rate);
            AdlibSetupCard();
            for (int i = 0; i < 11; ++i)
            {
                _adlibChannelsVolume[i] = 0;
                AdlibSetNoteVolume(i, 0);
                AdlibTurnNoteOff(i);
            }
            _mixerSoundHandle = _mixer.PlayStream(SoundType.Music, this, -1, Mixer.MaxChannelVolume, 0, false, true);
            return 0;
        }

        public override void MetaEvent(byte type, byte[] data, ushort length)
        {
            int @event = 0;
            if (length > 4 && data.ToUInt32BigEndian() == 0x3F00)
            {
                @event = data[4];
                switch (@event)
                {
                    case 1:
                        if (length == 34)
                        {
                            HandleSequencerSpecificMetaEvent1(data[5], data, 6);
                            return;
                        }
                        break;
                    case 2:
                        if (length == 6)
                        {
                            HandleSequencerSpecificMetaEvent2(data[5]);
                            return;
                        }
                        break;
                    case 3:
                        if (length == 6)
                        {
                            HandleSequencerSpecificMetaEvent3(data[5]);
                            return;
                        }
                        break;
                }
            }
            D.Warning($"Unhandled meta event {@event} len {length}");
        }

        void HandleSequencerSpecificMetaEvent1(int channel, byte[] data, int o)
        {
            for (int i = 0; i < 28; ++i)
            {
                _adlibMetaSequenceData[i] = data[o + i];
            }
            if (_midiNumberOfChannels > channel)
            {
                ByteAccess p;
                if (_adlibRhythmEnabled != 0)
                {
                    p = new ByteAccess(_adlibChannelsKeyScalingTable2, channel * 2);
                }
                else
                {
                    p = new ByteAccess(_adlibChannelsKeyScalingTable1, channel * 2);
                }
                AdlibSetupChannel(p[0], _adlibMetaSequenceData, 0, _adlibMetaSequenceData[26]);
                if (p[1] != 255)
                {
                    AdlibSetupChannel(p[1], _adlibMetaSequenceData, 13, _adlibMetaSequenceData[27]);
                }
            }
        }

        void HandleSequencerSpecificMetaEvent2(byte value)
        {
            _adlibRhythmEnabled = value;
            _midiNumberOfChannels = _adlibRhythmEnabled != 0 ? 11 : 9;
            AdlibSetAmpVibratoRhythm();
        }

        void HandleSequencerSpecificMetaEvent3(byte value)
        {
            AdlibSetNoteMul(value);
        }

        void AdlibTurnNoteOff(int channel)
        {
            if ((_adlibRhythmEnabled != 0 && channel <= 6) || channel < 9)
            {
                _midiChannelsOctTable[channel] = 0;
                _midiChannelsFreqTable[channel] = (byte)(_midiChannelsFreqTable[channel] & ~0x20);
                AdlibWrite(0xB0 + channel, _midiChannelsFreqTable[channel]);
            }
            else if (_adlibRhythmEnabled != 0 && channel <= 10)
            {
                _adlibVibratoRhythm &= ~(1 << (4 - (channel - 6)));
                AdlibSetAmpVibratoRhythm();
            }
        }

        void AdlibSetAmpVibratoRhythm()
        {
            byte value = 0;
            if (_adlibAMDepthEq48 != 0)
            {
                value |= 0x80;
            }
            if (_adlibVibratoDepthEq14 != 0)
            {
                value |= 0x40;
            }
            if (_adlibRhythmEnabled != 0)
            {
                value |= 0x20;
            }
            AdlibWrite(0xBD, value | _adlibVibratoRhythm);
        }

        void AdlibSetNoteVolume(int channel, int volume)
        {
            if (_midiNumberOfChannels > channel)
            {
                if (volume > 127)
                {
                    volume = 127;
                }
                _adlibChannelsLevelKeyScalingTable[channel] = (byte)volume;
                ByteAccess p;
                if (_adlibRhythmEnabled != 0)
                {
                    p = new ByteAccess(_adlibChannelsKeyScalingTable2, channel * 2);
                }
                else
                {
                    p = new ByteAccess(_adlibChannelsKeyScalingTable1, channel * 2);
                }
                AdlibSetChannel0x40(p[0]);
                if (p[1] != 255)
                {
                    AdlibSetChannel0x40(p[1]);
                }
            }
        }

        void AdlibSetChannel0x40(int channel)
        {
            int index, value, fl;

            if (_adlibRhythmEnabled != 0)
            {
                index = _adlibChannelsMappingTable3[channel];
            }
            else
            {
                index = _adlibChannelsMappingTable2[channel];
            }
            value = 63 - (_adlibSetupChannelSequence1[channel * 14 + 8] & 63);
            fl = 0;
            if (_adlibRhythmEnabled != 0 && index > 6)
            {
                fl = -1;
            }
            if (_adlibChannelsNoFeedback[channel] != 0 || _adlibSetupChannelSequence1[channel * 14 + 12] == 0 || fl != 0)
            {
                value = ((_adlibChannelsLevelKeyScalingTable[index] * value) + 64) >> 7;
            }
            value = (_adlibChannelsVolumeTable[index] * value * 2) >> 8;
            if (value > 63)
            {
                value = 63;
            }
            value = 63 - value;
            value |= _adlibSetupChannelSequence1[channel * 14] << 6;
            AdlibWrite(0x40 + _adlibChannelsMappingTable1[channel], value);
        }

        void AdlibSetupCard()
        {
            for (int i = 1; i <= 0xF5; ++i)
            {
                AdlibWrite(i, 0);
            }
            AdlibWrite(4, 6);
            for (int i = 0; i < 9; ++i)
            {
                _midiChannelsNote2Table[i] = 8192;
                _midiChannelsOctTable[i] = 0;
                _midiChannelsNote1Table[i] = 0;
                _midiChannelsFreqTable[i] = 0;
            }
            _adlibChannelsLevelKeyScalingTable.Set(0, 127, 11);
            AdlibSetupChannels(0);
            AdlibResetAmpVibratoRhythm(0, 0, 0);
            AdlibSetNoteMul(1);
            AdlibSetWaveformSelect(1);
        }

        void AdlibSetWaveformSelect(int fl)
        {
            _adlibWaveformSelect = fl != 0 ? 0x20 : 0;
            for (int i = 0; i < 18; ++i)
            {
                AdlibWrite(0xE0 + _adlibChannelsMappingTable1[i], 0);
            }
            AdlibWrite(1, _adlibWaveformSelect);
        }

        void AdlibSetNoteMul(int mul)
        {
            if (mul > 12)
            {
                mul = 12;
            }
            else if (mul < 1)
            {
                mul = 1;
            }
            _adlibNoteMul = mul;
        }

        void AdlibResetAmpVibratoRhythm(int am, int vib, int kso)
        {
            _adlibAMDepthEq48 = am;
            _adlibVibratoDepthEq14 = vib;
            _adlibKeyboardSplitOn = kso;
            AdlibSetAmpVibratoRhythm();
            AdlibSetCSMKeyboardSplit();
        }

        void AdlibSetCSMKeyboardSplit()
        {
            byte value = (byte)(_adlibKeyboardSplitOn != 0 ? 0x40 : 0);
            AdlibWrite(8, value);
        }

        void AdlibSetupChannels(int fl)
        {
            if (fl != 0)
            {
                _midiChannelsNote1Table[8] = 24;
                _midiChannelsNote2Table[8] = 8192;
                AdlibPlayNote(8);
                _midiChannelsNote1Table[7] = 31;
                _midiChannelsNote2Table[7] = 8192;
                AdlibPlayNote(7);
            }
            _adlibRhythmEnabled = fl;
            _midiNumberOfChannels = fl != 0 ? 11 : 9;
            _adlibVibratoRhythm = 0;
            _adlibAMDepthEq48 = 0;
            _adlibVibratoDepthEq14 = 0;
            _adlibKeyboardSplitOn = 0;
            AdlibResetChannels();
            AdlibSetAmpVibratoRhythm();
        }

        void AdlibResetChannels()
        {
            for (int i = 0; i < 18; ++i)
            {
                AdlibSetupChannelFromSequence(i,
                  _adlibChannelsNoFeedback[i] != 0 ? _adlibInitSequenceData2 : _adlibInitSequenceData1, 0);
            }
            if (_adlibRhythmEnabled != 0)
            {
                AdlibSetupChannelFromSequence(12, _adlibInitSequenceData3, 0);
                AdlibSetupChannelFromSequence(15, _adlibInitSequenceData4, 0);
                AdlibSetupChannelFromSequence(16, _adlibInitSequenceData5, 0);
                AdlibSetupChannelFromSequence(14, _adlibInitSequenceData6, 0);
                AdlibSetupChannelFromSequence(17, _adlibInitSequenceData7, 0);
                AdlibSetupChannelFromSequence(13, _adlibInitSequenceData8, 0);
            }
        }

        void AdlibSetupChannelFromSequence(int channel, byte[] src, int fl)
        {
            for (int i = 0; i < 13; ++i)
            {
                _adlibSetupChannelSequence2[i] = src[i];
            }
            AdlibSetupChannel(channel, _adlibSetupChannelSequence2, 0, fl);
        }

        void AdlibSetupChannel(int channel, ushort[] src, int o, int fl)
        {
            for (int i = 0; i < 13; ++i)
            {
                _adlibSetupChannelSequence1[14 * channel + i] = (byte)src[o + i];
            }
            _adlibSetupChannelSequence1[14 * channel + 13] = (byte)(fl & 3);
            AdlibSetupChannelHelper(channel);
        }

        void AdlibSetupChannelHelper(int channel)
        {
            AdlibSetAmpVibratoRhythm();
            AdlibSetCSMKeyboardSplit();
            AdlibSetChannel0x40(channel);
            AdlibSetChannel0xC0(channel);
            AdlibSetChannel0x60(channel);
            AdlibSetChannel0x80(channel);
            AdlibSetChannel0x20(channel);
            AdlibSetChannel0xE0(channel);
        }

        void AdlibSetChannel0xC0(int channel)
        {
            if (_adlibChannelsNoFeedback[channel] == 0)
            {
                var p = new ByteAccess(_adlibSetupChannelSequence1, channel * 14);
                byte value = (byte)(p[2] << 1);
                if (p[12] == 0)
                {
                    value |= 1;
                }
                AdlibWrite(0xC0 + _adlibChannelsMappingTable2[channel], value);
            }
        }

        void AdlibSetChannel0x60(int channel)
        {
            var p = new ByteAccess(_adlibSetupChannelSequence1, channel * 14);
            byte value = (byte)((p[3] << 4) | (p[6] & 15));
            AdlibWrite(0x60 + _adlibChannelsMappingTable1[channel], value);
        }

        void AdlibSetChannel0x80(int channel)
        {
            var p = new ByteAccess(_adlibSetupChannelSequence1, channel * 14);
            byte value = (byte)((p[4] << 4) | (p[7] & 15));
            AdlibWrite(0x80 + _adlibChannelsMappingTable1[channel], value);
        }

        void AdlibSetChannel0x20(int channel)
        {
            var p = new ByteAccess(_adlibSetupChannelSequence1, channel * 14);
            byte value = (byte)(p[1] & 15);
            if (p[9] != 0)
            {
                value |= 0x80;
            }
            if (p[10] != 0)
            {
                value |= 0x40;
            }
            if (p[5] != 0)
            {
                value |= 0x20;
            }
            if (p[11] != 0)
            {
                value |= 0x10;
            }
            AdlibWrite(0x20 + _adlibChannelsMappingTable1[channel], value);
        }

        void AdlibSetChannel0xE0(int channel)
        {
            byte value = 0;
            if (_adlibWaveformSelect != 0)
            {
                var p = new ByteAccess(_adlibSetupChannelSequence1, channel * 14);
                value = (byte)(p[13] & 3);
            }
            AdlibWrite(0xE0 + _adlibChannelsMappingTable1[channel], value);
        }

        void AdlibPlayNote(int channel)
        {
            _midiChannelsFreqTable[channel] = AdlibPlayNoteHelper(channel, _midiChannelsNote1Table[channel],
              _midiChannelsNote2Table[channel], _midiChannelsOctTable[channel]);
        }

        byte AdlibPlayNoteHelper(int channel, int note1, int note2, int oct)
        {
            int n = ((note2 * _midiChannelsNoteTable[channel]) >> 8) - 8192;
            if (n != 0)
            {
                n >>= 5;
                n *= _adlibNoteMul;
            }
            n += (note1 << 8) + 8;
            n >>= 4;
            if (n < 0)
            {
                n = 0;
            }
            else if (n > 1535)
            {
                n = 1535;
            }
            int index = (((n >> 4) % 12) << 4) | (n & 0xF);
            int f = _midiNoteFreqTable[index];
            int o = (n >> 4) / 12 - 1;
            if (f < 0)
            {
                ++o;
            }
            if (o < 0)
            {
                ++o;
                f >>= 1;
            }
            AdlibWrite(0xA0 + channel, f & 0xFF);
            int value = ((f >> 8) & 3) | (o << 2) | oct;
            AdlibWrite(0xB0 + channel, value);
            return (byte)value;
        }

        void AdlibWrite(int port, int value)
        {
            _opl.WriteReg(port, value);
        }

        IOpl MakeAdLibOPL(int rate)
        {
            var opl = new DosBoxOPL(OplType.Opl2);
            opl.Init(rate);
            return opl;
        }

        public override MidiChannel AllocateChannel()
        {
            return null;
        }

        public override MidiChannel GetPercussionChannel()
        {
            return null;
        }

        public override int Property(int prop, int param)
        {
            throw new NotImplementedException();
        }

        public override void Send(int b)
        {
            int channel = b & 15;
            int cmd = (b >> 4) & 7;
            int param1 = (b >> 8) & 255;
            int param2 = (b >> 16) & 255;
            switch (cmd)
            {
                case 0:
                    AdlibTurnNoteOff(channel);
                    break;
                case 1:
                    HandleMidiEvent0x90_NoteOn(channel, param1, param2);
                    break;
                case 3:
                    break;
                case 5:
                    AdlibSetNoteVolume(channel, param1);
                    _adlibChannelsVolume[channel] = (ushort)param1;
                    break;
                case 6:
                    AdlibSetPitchBend(channel, param1 | (param2 << 7));
                    break;
                default:
                    D.Warning($"Unhandled cmd {cmd} channel {channel} (0x{b:X})");
                    break;
            }
        }

        void AdlibSetPitchBend(int channel, int range)
        {
            if ((_adlibRhythmEnabled != 0 && channel <= 6) || channel < 9)
            {
                if (range > 16383)
                {
                    range = 16383;
                }
                _midiChannelsNote2Table[channel] = (short)range;
                AdlibPlayNote(channel);
            }
        }

        void HandleMidiEvent0x90_NoteOn(int channel, int param1, int param2)
        {
            if (param2 == 0)
            {
                AdlibTurnNoteOff(channel);
                _adlibChannelsVolume[channel] = (ushort)param2;
            }
            else
            {
                AdlibSetNoteVolume(channel, param2);
                _adlibChannelsVolume[channel] = (ushort)param2;
                AdlibTurnNoteOff(channel);
                AdlibTurnNoteOn(channel, param1);
            }
        }

        void AdlibTurnNoteOn(int channel, int note)
        {
            note -= 12;
            if (note < 0)
            {
                note = 0;
            }
            if ((_adlibRhythmEnabled != 0 && channel <= 6) || channel < 9)
            {
                _midiChannelsNote1Table[channel] = (byte)note;
                _midiChannelsOctTable[channel] = 0x20;
                AdlibPlayNote(channel);
            }
            else if (_adlibRhythmEnabled != 0 && channel <= 10)
            {
                if (channel == 6)
                {
                    _midiChannelsNote1Table[6] = (byte)note;
                    AdlibPlayNote(channel);
                }
                else if (channel == 8 && _midiChannelsNote1Table[8] == note)
                {
                    _midiChannelsNote1Table[8] = (byte)note;
                    _midiChannelsNote1Table[7] = (byte)(note + 7);
                    AdlibPlayNote(8);
                    AdlibPlayNote(7);
                }
                _adlibVibratoRhythm = 1 << (4 - (channel - 6));
                AdlibSetAmpVibratoRhythm();
            }
        }

        protected override void GenerateSamples(short[] data, int pos, int len)
        {
            Array.Clear(data, pos, len);
            YM3812UpdateOne(_opl, data, pos, len);
        }

        private static void YM3812UpdateOne(IOpl OPL, short[] buffer, int pos, int length)
        {
            OPL.ReadBuffer(buffer, pos, length);
        }
    }
}