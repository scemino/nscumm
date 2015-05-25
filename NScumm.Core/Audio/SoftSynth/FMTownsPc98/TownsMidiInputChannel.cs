//
//  TownsMidiInputChannel.cs
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

namespace NScumm.Core.Audio.SoftSynth
{
    class TownsMidiInputChannel : MidiChannel
    {
        public override byte Number
        {
            get{ return _chanIndex; }
        }

        public override MidiDriver Device
        {
            get{ return _driver; }
        }

        public TownsMidiInputChannel(MidiDriver_TOWNS driver, int chanIndex)
        {
            _driver = driver;
            _chanIndex = (byte)chanIndex;
            _instrument = new byte[30];
        }

        public bool Allocate()
        {
            if (_allocated)
                return false;
            _allocated = true;
            return true;
        }

        public override void Release()
        {
            _allocated = false;
        }

        public override void Send(uint b)
        {
            _driver.Send((int)(b | _chanIndex));
        }

        public override void NoteOff(byte note)
        {
            if (_out == null)
                return;

            for (TownsMidiOutputChannel oc = _out; oc != null; oc = oc._next)
            {
                if (oc._note != note)
                    continue;

                if (_sustain != 0)
                    oc._sustainNoteOff = 1;
                else
                    oc.Disconnect();
            }
        }

        public override void NoteOn(byte note, byte velocity)
        {
            TownsMidiOutputChannel oc = _driver.AllocateOutputChannel(_priority);

            if (oc == null)
                return;

            oc.Connect(this);

            oc._adjustModTl = (byte)(_instrument[10] & 1);
            oc._note = note;
            oc._sustainNoteOff = 0;
            oc._duration = (short)(_instrument[29] * 63);

            oc._operator1Tl = (byte)((_instrument[1] & 0x3f) + _driver._operatorLevelTable[((velocity >> 1) << 5) + (_instrument[4] >> 2)]);
            if (oc._operator1Tl > 63)
                oc._operator1Tl = 63;

            oc._operator2Tl = (byte)((_instrument[6] & 0x3f) + _driver._operatorLevelTable[((velocity >> 1) << 5) + (_instrument[9] >> 2)]);
            if (oc._operator2Tl > 63)
                oc._operator2Tl = 63;

            oc.SetupProgram(_instrument, oc._adjustModTl == 1 ? _programAdjustLevel[_driver._operatorLevelTable[(_tl >> 2) + (oc._operator1Tl << 5)]] : oc._operator1Tl, _programAdjustLevel[_driver._operatorLevelTable[(_tl >> 2) + (oc._operator2Tl << 5)]]);
            oc.NoteOn((byte)(note + _transpose), _freqLSB);

            if ((_instrument[11] & 0x80) != 0)
                oc.SetupEffects(0, _instrument[11], _instrument, 12);
            else
                oc._effectEnvelopes[0].state = EnvelopeState.Ready;

            if ((_instrument[20] & 0x80) != 0)
                oc.SetupEffects(1, _instrument[20], _instrument, 21);
            else
                oc._effectEnvelopes[1].state = EnvelopeState.Ready;
        }

        public override void ProgramChange(byte program)
        {
            // Not implemented (The loading and assignment of programs
            // is handled externally by the SCUMM engine. The programs
            // get sent via sysEx_customInstrument.)
        }

        public override void PitchBend(short bend)
        {
            _pitchBend = bend;
            _freqLSB = (ushort)(((_pitchBend * _pitchBendFactor) >> 6) + _detune);
            for (TownsMidiOutputChannel oc = _out; oc != null; oc = oc._next)
                oc.NoteOnPitchBend((byte)(oc._note + oc._in._transpose), _freqLSB);
        }

        public override void ControlChange(byte control, byte value)
        {
            switch (control)
            {
                case 1:
                    ControlModulationWheel(value);
                    break;
                case 7:
                    ControlVolume(value);
                    break;
                case 10:
                    ControlPanPos(value);
                    break;
                case 64:
                    ControlSustain(value);
                    break;
                case 123:
                    while (_out != null)
                        _out.Disconnect();
                    break;
            }
        }

        public override void PitchBendFactor(byte value)
        {
            _pitchBendFactor = value;
            _freqLSB = (ushort)(((_pitchBend * _pitchBendFactor) >> 6) + _detune);
            for (TownsMidiOutputChannel oc = _out; oc != null; oc = oc._next)
                oc.NoteOnPitchBend((byte)(oc._note + oc._in._transpose), _freqLSB);
        }

        public override void Priority(byte value)
        {
            _priority = value;
        }

        public override void SysExCustomInstrument(uint type, byte[] instr)
        {
            Array.Copy(instr, _instrument, 30);
        }

        void ControlModulationWheel(byte value)
        {
            _modWheel = (sbyte)value;
            for (TownsMidiOutputChannel oc = _out; oc != null; oc = oc._next)
                oc.SetModWheel(value);
        }

        void ControlVolume(byte value)
        {
            /* This is all done inside the imuse code
    uint16 v1 = _ctrlVolume + 1;
    uint16 v2 = value;
    if (_chanIndex != 16) {
        _ctrlVolume = value;
        v2 = _player.getEffectiveVolume();
    }
    _tl = (v1 * v2) >> 7;*/

            _tl = value;
        }

        void ControlPanPos(byte value)
        {
            // not implemented
        }

        void ControlSustain(byte value)
        {
            _sustain = value;
            if (value == 0)
                ReleasePedal();
        }

        void ReleasePedal()
        {
            for (TownsMidiOutputChannel oc = _out; oc != null; oc = oc._next)
            {
                if (oc._sustainNoteOff != 0)
                    oc.Disconnect();
            }
        }

        internal TownsMidiOutputChannel _out;

        byte[] _instrument;
        byte _chanIndex;
        internal byte _priority;
        byte _tl;
        sbyte _transpose;
        sbyte _detune;
        internal sbyte _modWheel;
        byte _sustain;
        byte _pitchBendFactor;
        short _pitchBend;
        ushort _freqLSB;

        bool _allocated;

        MidiDriver_TOWNS _driver;

        static readonly byte[] _programAdjustLevel =
            {
                0x00, 0x04, 0x07, 0x0B, 0x0D, 0x10, 0x12, 0x14,
                0x16, 0x18, 0x1A, 0x1B, 0x1D, 0x1E, 0x1F, 0x21,
                0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29,
                0x2A, 0x2B, 0x2C, 0x2C, 0x2D, 0x2E, 0x2F, 0x2F,
                0x30, 0x31, 0x31, 0x32, 0x33, 0x33, 0x34, 0x35,
                0x35, 0x36, 0x36, 0x37, 0x37, 0x38, 0x38, 0x39,
                0x39, 0x3A, 0x3A, 0x3B, 0x3B, 0x3C, 0x3C, 0x3C,
                0x3D, 0x3D, 0x3E, 0x3E, 0x3E, 0x3F, 0x3F, 0x3F
            };

    }
    
}
