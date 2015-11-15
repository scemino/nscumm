//
//  InstrumentProgram.cs
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

using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Audio.IMuse
{
    class InstrumentProgram: IInstrumentInternal
    {
        public InstrumentProgram(byte program, bool mt32)
        {
            _program = program;
            _mt32 = mt32;
            if (program > 127)
                _program = 255;
        }

        public InstrumentProgram(Serializer s)
        {
            _program = 255;
            SaveOrLoad(s);
        }

        public void SaveOrLoad(Serializer s)
        {
            if (!s.IsLoading)
            {
                s.Writer.WriteByte(_program);
                s.Writer.WriteByte(_mt32 ? 1 : 0);
            }
            else
            {
                _program = s.Reader.ReadByte();
                _mt32 = s.Reader.ReadByte() > 0;
            }
        }

        public void Send(MidiChannel mc)
        {
            if (_program > 127)
                return;

            byte program = _program;
            // TODO: _native_mt32
//            if (_native_mt32 != _mt32)
//                program = _native_mt32 ? MidiDriver::_gmToMt32[program] : MidiDriver::_mt32ToGm[program];
            if (program < 128)
                mc.ProgramChange(program);
        }

        public void CopyTo(Instrument dest)
        {
            dest.Program(_program, _mt32);
        }

        public bool IsValid { get { return true; } }

        byte _program;
        bool _mt32;
    }
}
