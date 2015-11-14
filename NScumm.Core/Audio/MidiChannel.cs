//
//  MidiChannel.cs
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

namespace NScumm.Core.Audio
{
    public abstract class MidiChannel
    {
        public abstract MidiDriver Device { get; }

        public abstract byte Number { get; }

        public abstract void Release();

        public abstract void Send(uint b);
        // 4-bit channel portion is ignored

        // Regular messages
        public abstract void NoteOff(byte note);

        public abstract void NoteOn(byte note, byte velocity);

        public abstract void ProgramChange(byte program);

        public abstract void PitchBend(short bend);
        // -0x2000 to +0x1FFF

        // Control Change messages
        public abstract void ControlChange(byte control, byte value);

        public virtual void ModulationWheel(byte value)
        {
            ControlChange(1, value);
        }

        public virtual void Volume(byte value)
        {
            ControlChange(7, value);
        }

        public virtual void PanPosition(byte value)
        {
            ControlChange(10, value);
        }

        public abstract void PitchBendFactor(byte value);

        public virtual void Detune(byte value)
        {
            ControlChange(17, value);
        }

        public virtual void Priority(byte value)
        {
        }

        public virtual void Sustain(bool value)
        {
            ControlChange(64, value ? (byte)1 : (byte)0);
        }

        public virtual void EffectLevel(byte value)
        {
            ControlChange(91, value);
        }

        public virtual void ChorusLevel(byte value)
        {
            ControlChange(93, value);
        }

        public virtual void AllNotesOff()
        {
            ControlChange(123, 0);
        }

        // SysEx messages
        public abstract void SysExCustomInstrument(uint type, byte[] instr);
    }
    
}
