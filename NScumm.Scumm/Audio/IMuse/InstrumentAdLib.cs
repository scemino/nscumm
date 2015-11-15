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

using System.Diagnostics;
using System.IO;
using NScumm.Core;
using NScumm.Core.Audio;
using NScumm.Core.Audio.SoftSynth;
using NScumm.Scumm.IO;

namespace NScumm.Scumm.Audio.IMuse
{
    class InstrumentAdLib : IInstrumentInternal
    {
        byte[] _instrument;

        public InstrumentAdLib(byte[] data)
        {
            Debug.Assert(data.Length == 30);
            _instrument = (byte[])data.Clone();
        }

        public InstrumentAdLib(Stream input)
        {
            _instrument = new byte[30];
            input.Read(_instrument, 0, 30);
        }

        public void SaveOrLoad(Serializer s)
        {
            if (!s.IsLoading)
                s.Writer.WriteBytes(_instrument, 30);
            else
                _instrument = s.Reader.ReadBytes(30);
        }

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
