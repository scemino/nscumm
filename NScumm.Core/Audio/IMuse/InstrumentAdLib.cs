//
//  InstrumentAdLib.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
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

using NScumm.Core.Audio.IMuse;
using NScumm.Core.Audio.SoftSynth;

namespace NScumm.Core.Audio.IMuse
{
    class InstrumentAdLib : IInstrumentInternal
    {
        byte[] _instrument;

        public InstrumentAdLib(byte[] data)
        {
            _instrument = data;
        }

        //        Instrument_AdLib(Serializer *s);
        //        void saveOrLoad(Serializer *s);

        public void Send(MidiChannel mc)
        {
            mc.SysExCustomInstrument(AdlibMidiDriver.ToType("ADL "), _instrument);
        }

        public void CopyTo(Instrument dest)
        {
            dest.Adlib(_instrument);
        }

        public bool IsValid { get { return true; } }
    }
    
}
