//
//  MidiChannel_MPU401.cs
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

namespace NScumm.Core.Audio
{
    /// <summary>
    /// Common MPU401 implementation methods.
    /// </summary>
    public class MidiChannel_MPU401: MidiChannel
    {
        private MidiDriver _owner;
        private bool _allocated;
        private byte _channel;

        public override MidiDriver Device
        {
            get
            {
                return _owner;
            }
        }

        public override byte Number
        {
            get
            {
                return _channel;
            }
        }

        public void Init(MidiDriver owner, byte channel)
        {
            _owner = owner;
            _channel = channel;
            _allocated = false;
        }

        public bool Allocate()
        {
            if (_allocated)
                return false;

            _allocated = true;

            return true;
        }

        public override void ControlChange(byte control, byte value)
        {
            _owner.Send(value << 16 | control << 8 | 0xB0 | _channel);
        }

        public override void NoteOff(byte note)
        {
            _owner.Send(note << 8 | 0x80 | _channel);
        }

        public override void NoteOn(byte note, byte velocity)
        {
            _owner.Send(velocity << 16 | note << 8 | 0x90 | _channel);
        }

        public override void PitchBend(short bend)
        {
            _owner.Send((((bend + 0x2000) >> 7) & 0x7F) << 16 | ((bend + 0x2000) & 0x7F) << 8 | 0xE0 | _channel);
        }

        public override void PitchBendFactor(byte value)
        {
            _owner.SetPitchBendRange(_channel, value);
        }

        public override void ProgramChange(byte program)
        {
            _owner.Send(program << 8 | 0xC0 | _channel);
        }

        public override void Release()
        {
            _allocated = false;
        }

        public override void Send(uint b)
        {
            _owner.Send((int)((b & 0xFFFFFFF0) | (_channel & 0xF)));
        }

        public override void SysExCustomInstrument(uint type, byte[] instr)
        {
            _owner.SysExCustomInstrument(_channel, type, instr);
        }
    }
}
