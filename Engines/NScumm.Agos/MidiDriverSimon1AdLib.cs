//
//  MidiDriverSimon1AdLib.cs
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

namespace NScumm.Agos
{
    struct RhythmMap
    {
        public int channel;
        public int program;
        public int note;

        public RhythmMap(int channel, int program, int note)
        {
            this.channel = channel;
            this.program = program;
            this.note = note;
        }
    }

    class Voice
    {
        public uint channel;
        public uint note;
        public uint instrTotalLevel;
        public uint instrScalingLevel;
        public uint frequency;
    }

    internal class MidiDriverSimon1AdLib : MidiDriver
    {
        private const int OPLVoicesCount = 9;
        private const int NumberOfVoices = 11;
        private const int NumberOfMidiChannels = 16;
        private const int ChannelOrphanedFlag = 0x80;
        private const int ChannelUnused = 0xFF;

        private byte[] _instruments;
        private IOpl _opl;
        private bool _isOpen;
        private object _timerParam;
        private TimerProc _timerProc;

        private Voice[] _voices = AgosEngine.CreateArray<Voice>(NumberOfVoices);
        private uint[] _midiPrograms = new uint[NumberOfMidiChannels];

        private int _melodyVoices;
        private byte _amvdrBits;
        private bool _rhythmEnabled;

        private static readonly RhythmMap[] _rhythmMap =
        {
            new RhythmMap(11, 123, 40),
            new RhythmMap(12, 127, 50),
            new RhythmMap(12, 124, 1),
            new RhythmMap(12, 124, 90),
            new RhythmMap(13, 125, 50),
            new RhythmMap(13, 125, 25),
            new RhythmMap(15, 127, 80),
            new RhythmMap(13, 125, 25),
            new RhythmMap(15, 127, 40),
            new RhythmMap(13, 125, 35),
            new RhythmMap(15, 127, 90),
            new RhythmMap(13, 125, 35),
            new RhythmMap(13, 125, 45),
            new RhythmMap(14, 126, 90),
            new RhythmMap(13, 125, 45),
            new RhythmMap(15, 127, 90),
            new RhythmMap(0, 0, 0),
            new RhythmMap(15, 127, 60),
            new RhythmMap(0, 0, 0),
            new RhythmMap(13, 125, 60),
            new RhythmMap(0, 0, 0),
            new RhythmMap(0, 0, 0),
            new RhythmMap(0, 0, 0),
            new RhythmMap(13, 125, 45),
            new RhythmMap(13, 125, 40),
            new RhythmMap(13, 125, 35),
            new RhythmMap(13, 125, 30),
            new RhythmMap(13, 125, 25),
            new RhythmMap(13, 125, 80),
            new RhythmMap(13, 125, 40),
            new RhythmMap(13, 125, 80),
            new RhythmMap(13, 125, 40),
            new RhythmMap(14, 126, 40),
            new RhythmMap(15, 127, 60),
            new RhythmMap(0, 0, 0),
            new RhythmMap(0, 0, 0),
            new RhythmMap(14, 126, 80),
            new RhythmMap(0, 0, 0),
            new RhythmMap(13, 125, 100)
        };

        private static readonly uint[] _rhythmInstrumentMask =
        {
            0x10, 0x08, 0x04, 0x02, 0x01
        };

        private static readonly int[] _operatorMap =
        {
            0x00, 0x01, 0x02, 0x08, 0x09, 0x0A, 0x10, 0x11,
            0x12
        };

        private static readonly int[] _frequencyIndexAndOctaveTable =
        {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B, 0x00, 0x01, 0x02, 0x03,
            0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B,
            0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
            0x18, 0x19, 0x1A, 0x1B, 0x20, 0x21, 0x22, 0x23,
            0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B,
            0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
            0x38, 0x39, 0x3A, 0x3B, 0x40, 0x41, 0x42, 0x43,
            0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B,
            0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57,
            0x58, 0x59, 0x5A, 0x5B, 0x60, 0x61, 0x62, 0x63,
            0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B,
            0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77,
            0x78, 0x79, 0x7A, 0x7B, 0x70, 0x71, 0x72, 0x73,
            0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B,
            0x7B, 0x7B, 0x7B, 0x7B, 0x7B, 0x7B, 0x7B, 0x7B
        };

        private static readonly int[] _frequencyTable =
        {
            0x0157, 0x016B, 0x0181, 0x0198, 0x01B0, 0x01CA, 0x01E5, 0x0202,
            0x0220, 0x0241, 0x0263, 0x0287, 0x2100, 0xD121, 0xA307, 0x46A4
        };

        private static readonly int[] _operatorDefaults =
        {
            0x01, 0x11, 0x4F, 0x00, 0xF1, 0xF2, 0x53, 0x74
        };

        private static readonly int[] _rhythmOperatorMap =
        {
            0x10, 0x14, 0x12, 0x15, 0x11
        };

        private static readonly int[] _rhythmVoiceMap =
        {
            6, 7, 8, 8, 7
        };

        public MidiDriverSimon1AdLib(byte[] instrumentData)
        {
            _instruments = instrumentData;
        }

        public override void Send(int b)
        {
            int channel = b & 0x0F;
            int command = b & 0xF0;
            int param1 = (b >> 8) & 0xFF;
            int param2 = (b >> 16) & 0xFF;

            // The percussion channel is handled specially. The AdLib output uses
            // channels 11 to 15 for percussions. For this, the original converted
            // note on on the percussion channel to note on channels 11 to 15 before
            // giving it to the AdLib output. We do this in here for simplicity.
            if (command == 0x90 && channel == 9)
            {
                param1 -= 36;
                if (param1 < 0 || param1 >= _rhythmMap.Length)
                {
                    return;
                }

                channel = _rhythmMap[param1].channel;
                base.Send((byte) (0xC0 | channel), (byte) _rhythmMap[param1].program, 0);

                param1 = _rhythmMap[param1].note;
                base.Send((byte) (0x80 | channel), (byte) param1, (byte) param2);

                param2 >>= 1;
            }

            switch (command)
            {
                case 0x80: // note OFF
                    NoteOff((uint) channel, (uint) param1);
                    break;

                case 0x90: // note ON
                    if (param2 == 0)
                    {
                        NoteOff((uint) channel, (uint) param1);
                    }
                    else
                    {
                        NoteOn((uint) channel, (uint) param1, (uint) param2);
                    }
                    break;

                case 0xB0: // control change
                    ControlChange((uint) channel, (uint) param1, (uint) param2);
                    break;

                case 0xC0: // program change
                    ProgramChange((uint) channel, (uint) param1);
                    break;
            }
        }

        public override MidiDriverError Open()
        {
            if (_isOpen)
            {
                return MidiDriverError.AlreadyOpen;
            }

            _opl = new DosBoxOPL(OplType.Opl2);
            if (_opl == null)
            {
                return MidiDriverError.DeviceNotAvailable;
            }

            _opl.Init();

            _opl.Start(OnTimer);

            _opl.WriteReg(0x01, 0x20);
            _opl.WriteReg(0x08, 0x40);
            _opl.WriteReg(0xBD, 0xC0);
            Reset();

            _isOpen = true;
            return 0;
        }

        public override void SetTimerCallback(object timerParam, TimerProc timerProc)
        {
            _timerParam = timerParam;
            _timerProc = timerProc;
        }

        public override uint BaseTempo => 1000000 / Opl.DefaultCallbackFrequency;

        public override MidiChannel AllocateChannel()
        {
            return null;
        }

        public override MidiChannel GetPercussionChannel()
        {
            return null;
        }

        private void Reset()
        {
            ResetOPLVoices();
            ResetRhythm();
            for (int i = 0; i < NumberOfVoices; ++i)
            {
                _voices[i].channel = ChannelUnused;
            }
            ResetVoices();
        }

        private void ResetOPLVoices()
        {
            _amvdrBits &= 0xE0;
            _opl.WriteReg(0xBD, _amvdrBits);
            for (int i = 8; i >= 0; --i)
            {
                _opl.WriteReg(0xB0 + i, 0);
            }
        }

        private void ProgramChange(uint channel, uint program)
        {
            _midiPrograms[channel] = program;

            if (_rhythmEnabled && channel >= 11)
            {
                SetupInstrument(channel - 5, program);
            }
            else
            {
                // Fully unallocate all previously allocated but now unused voices for
                // this MIDI channel.
                for (uint i = 0; i < OPLVoicesCount; ++i)
                {
                    if (_voices[i].channel == (channel | ChannelOrphanedFlag))
                    {
                        _voices[i].channel = ChannelUnused;
                    }
                }

                // Set the program for all voices allocted for this MIDI channel.
                for (uint i = 0; i < OPLVoicesCount; ++i)
                {
                    if (_voices[i].channel == channel)
                    {
                        SetupInstrument(i, program);
                    }
                }
            }
        }

        private void ControlChange(uint channel, uint controller, uint value)
        {
            // Enable/Disable Rhythm Section
            if (controller == 0x67)
            {
                ResetVoices();
                _rhythmEnabled = (value != 0);

                if (_rhythmEnabled)
                {
                    _melodyVoices = 6;
                    _amvdrBits = 0xE0;
                }
                else
                {
                    _melodyVoices = 9;
                    _amvdrBits = 0xC0;
                }

                _voices[6].channel = ChannelUnused;
                _voices[7].channel = ChannelUnused;
                _voices[8].channel = ChannelUnused;

                _opl.WriteReg(0xBD, _amvdrBits);
            }
        }

        private void ResetVoices()
        {
            Array.Clear(_midiPrograms, 0, _midiPrograms.Length);
            for (int i = 0; i < NumberOfVoices; ++i)
            {
                _voices[i].channel = ChannelUnused;
            }

            for (int i = 0; i < OPLVoicesCount; ++i)
            {
                ResetRhythm();
                _opl.WriteReg(0x08, 0x00);

                int oplRegister = _operatorMap[i];
                for (int j = 0; j < 4; ++j)
                {
                    oplRegister += 0x20;

                    _opl.WriteReg(oplRegister + 0, _operatorDefaults[2 * j + 0]);
                    _opl.WriteReg(oplRegister + 3, _operatorDefaults[2 * j + 1]);
                }

                _opl.WriteReg(oplRegister + 0x60, 0x00);
                _opl.WriteReg(oplRegister + 0x63, 0x00);

                // This seems to be serious bug but the original does it the same way.
                _opl.WriteReg(_operatorMap[i] + i, 0x08);
            }
        }

        private void ResetRhythm()
        {
            _melodyVoices = 9;
            _amvdrBits = 0xC0;
            _opl.WriteReg(0xBD, _amvdrBits);
        }

        private void NoteOn(uint channel, uint note, uint velocity)
        {
            if (_rhythmEnabled && channel >= 11)
            {
                NoteOnRhythm(channel, note, velocity);
                return;
            }

            int voiceNum = AllocateVoice(channel);
            Voice voice = _voices[voiceNum];

            if ((voice.channel & 0x7F) != channel)
            {
                SetupInstrument((uint) voiceNum, _midiPrograms[channel]);
            }
            voice.channel = channel;

            _opl.WriteReg(0x43 + _operatorMap[voiceNum],
                (int) ((0x3F - (((velocity | 0x80) * voice.instrTotalLevel) >> 8)) | voice.instrScalingLevel));

            voice.note = note;
            if (note >= 0x80)
            {
                note = 0;
            }

            int frequencyAndOctave = _frequencyIndexAndOctaveTable[note];
            uint frequency = (uint) _frequencyTable[frequencyAndOctave & 0x0F];

            uint highByte = (uint) (((frequency & 0xFF00) >> 8) | ((frequencyAndOctave & 0x70) >> 2));
            uint lowByte = frequency & 0x00FF;
            voice.frequency = (highByte << 8) | lowByte;

            _opl.WriteReg(0xA0 + voiceNum, (int) lowByte);
            _opl.WriteReg(0xB0 + voiceNum, (int) (highByte | 0x20));
        }

        private int AllocateVoice(uint channel)
        {
            for (int i = 0; i < _melodyVoices; ++i)
            {
                if (_voices[i].channel == (channel | ChannelOrphanedFlag))
                {
                    return i;
                }
            }

            for (int i = 0; i < _melodyVoices; ++i)
            {
                if (_voices[i].channel == ChannelUnused)
                {
                    return i;
                }
            }

            for (int i = 0; i < _melodyVoices; ++i)
            {
                if (_voices[i].channel > 0x7F)
                {
                    return i;
                }
            }

            // The original had some logic for a priority based reuse of channels.
            // However, the priority value is always 0, which causes the first channel
            // to be picked all the time.
            const int voice = 0;
            _opl.WriteReg(0xA0 + voice, (int) ((_voices[voice].frequency) & 0xFF));
            _opl.WriteReg(0xB0 + voice, (int) ((_voices[voice].frequency >> 8) & 0xFF));
            return voice;
        }

        private void SetupInstrument(uint voice, uint instrument)
        {
            var instrumentData = new BytePtr(_instruments, (int) (instrument * 16));

            int scaling = instrumentData[3];
            if (_rhythmEnabled && voice >= 7)
            {
                scaling = instrumentData[2];
            }

            int scalingLevel = scaling & 0xC0;
            int totalLevel = scaling & 0x3F;

            _voices[voice].instrScalingLevel = (uint) scalingLevel;
            _voices[voice].instrTotalLevel = (uint) ((-(totalLevel - 0x3F)) & 0xFF);

            if (!_rhythmEnabled || voice <= 6)
            {
                int oplRegister = _operatorMap[voice];
                for (int j = 0; j < 4; ++j)
                {
                    oplRegister += 0x20;
                    _opl.WriteReg(oplRegister + 0, instrumentData.Value);
                    instrumentData.Offset++;
                    _opl.WriteReg(oplRegister + 3, instrumentData.Value);
                    instrumentData.Offset++;
                }
                oplRegister += 0x60;
                _opl.WriteReg(oplRegister + 0, instrumentData.Value);
                instrumentData.Offset++;
                _opl.WriteReg(oplRegister + 3, instrumentData.Value);
                instrumentData.Offset++;

                _opl.WriteReg((int) (0xC0 + voice), instrumentData.Value);
                instrumentData.Offset++;
            }
            else
            {
                voice -= 7;

                int oplRegister = _rhythmOperatorMap[voice + 1];
                for (int j = 0; j < 4; ++j)
                {
                    oplRegister += 0x20;
                    _opl.WriteReg(oplRegister + 0, instrumentData.Value);
                    instrumentData.Offset += 2;
                }
                oplRegister += 0x60;
                _opl.WriteReg(oplRegister + 0, instrumentData.Value);
                instrumentData.Offset += 2;

                _opl.WriteReg(0xC0 + _rhythmVoiceMap[voice + 1], instrumentData.Value);
                instrumentData.Offset++;
            }
        }


        private void NoteOnRhythm(uint channel, uint note, uint velocity)
        {
            uint voiceNum = channel - 5;
            Voice voice = _voices[voiceNum];

            _amvdrBits = (byte) (_amvdrBits | _rhythmInstrumentMask[voiceNum - 6]);

            uint level = (0x3F - (((velocity | 0x80) * voice.instrTotalLevel) >> 8)) | voice.instrScalingLevel;
            if (voiceNum == 6)
            {
                _opl.WriteReg(0x43 + _rhythmOperatorMap[voiceNum - 6], (int) level);
            }
            else
            {
                _opl.WriteReg(0x40 + _rhythmOperatorMap[voiceNum - 6], (int) level);
            }

            voice.note = note;
            if (note >= 0x80)
            {
                note = 0;
            }

            int frequencyAndOctave = _frequencyIndexAndOctaveTable[note];
            uint frequency = (uint) _frequencyTable[frequencyAndOctave & 0x0F];

            uint highByte = (uint) (((frequency & 0xFF00) >> 8) | ((frequencyAndOctave & 0x70) >> 2));
            uint lowByte = frequency & 0x00FF;
            voice.frequency = (highByte << 8) | lowByte;

            uint oplOperator = (uint) _rhythmVoiceMap[voiceNum - 6];
            _opl.WriteReg((int) (0xA0 + oplOperator), (int) lowByte);
            _opl.WriteReg((int) (0xB0 + oplOperator), (int) highByte);

            _opl.WriteReg(0xBD, _amvdrBits);
        }

        private void NoteOff(uint channel, uint note)
        {
            if (_melodyVoices <= 6 && channel >= 11)
            {
                _amvdrBits = (byte) (_amvdrBits & ~(_rhythmInstrumentMask[channel - 11]));
                _opl.WriteReg(0xBD, _amvdrBits);
            }
            else
            {
                for (int i = 0; i < _melodyVoices; ++i)
                {
                    if (_voices[i].note == note && _voices[i].channel == channel)
                    {
                        _voices[i].channel |= ChannelOrphanedFlag;
                        _opl.WriteReg(0xA0 + i, (int) ((_voices[i].frequency) & 0xFF));
                        _opl.WriteReg(0xB0 + i, (int) ((_voices[i].frequency >> 8) & 0xFF));
                        return;
                    }
                }
            }
        }

        private void OnTimer()
        {
            _timerProc?.Invoke(_timerParam);
        }
    }
}