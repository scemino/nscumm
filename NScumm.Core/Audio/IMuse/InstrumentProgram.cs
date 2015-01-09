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

using NScumm.Core.Audio.IMuse;
using NScumm.Core.Audio.SoftSynth;

namespace NScumm.Core.Audio.IMuse
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

        //        InstrumentProgram(Serializer *s) {
        //            _program = 255;
        //            if (!s->isSaving())
        //                saveOrLoad(s);
        //        }

        //        void InstrumentProgram::saveOrLoad(Serializer *s) {
        //            if (s->isSaving()) {
        //                s->saveByte(_program);
        //                s->saveByte(_mt32 ? 1 : 0);
        //            } else {
        //                _program = s->loadByte();
        //                _mt32 = (s->loadByte() > 0);
        //            }
        //        }

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
