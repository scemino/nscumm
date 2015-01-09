//
//  Instrument.cs
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

using System;
using NScumm.Core.Audio.IMuse;

namespace NScumm.Core.Audio.IMuse
{
    enum InstrumentType
    {
        None = 0,
        Program = 1,
        AdLib = 2,
        Roland = 3,
        PcSpk = 4,
        MacSfx = 5
    }

    class Instrument
    {
        InstrumentType _type;
        IInstrumentInternal _instrument;

        static bool _native_mt32;

        public static void NativeMT32(bool native)
        {
            _native_mt32 = native;
        }

        /// <summary>
        /// This emulates the percussion bank setup LEC used with the MT-32,
        /// where notes 24 - 34 were assigned instruments without reverb.
        /// It also fixes problems on GS devices that map sounds to these
        /// notes by default.
        /// </summary>
        public static readonly byte[] _gmRhythmMap =
            {
            0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,
            0,  0,  0,  0,  0,  0,  0,  0, 36, 37, 38, 39, 40, 41, 66, 47,
            65, 48, 56
        };

        public void Clear()
        {
            _instrument = null;
            _type = InstrumentType.None;
        }

        public void CopyTo(Instrument dest)
        {
            if (_instrument != null)
                _instrument.CopyTo(dest);
            else
                dest.Clear();
        }

        public void Program(byte program, bool mt32)
        {
            Clear();
            if (program > 127)
                return;
            _type = InstrumentType.Program;
            _instrument = new InstrumentProgram(program, mt32);
        }

        public void Adlib(byte[] instrument)
        {
            Clear();
            if (instrument == null)
                return;
            _type = InstrumentType.AdLib;
            _instrument = new InstrumentAdLib(instrument);
        }

        public void Roland(byte[] instrument)
        {
            throw new NotImplementedException();
            //            Clear();
            //            if (instrument == null)
            //                return;
            //            _type = InstrumentType.Roland;
            //            _instrument = new Instrument_Roland(instrument);
        }

        public void PcSpk(byte[] instrument)
        {
            throw new NotImplementedException();
            //            Clear();
            //            if (!instrument)
            //                return;
            //            _type = InstrumentType.PcSpk;
            //            _instrument = new Instrument_PcSpk(instrument);
        }

        public void MacSfx(byte program)
        {
            throw new NotImplementedException();
            //            Clear();
            //            if (program > 127)
            //                return;
            //            _type = InstrumentType.MacSfx;
            //            _instrument = new Instrument_MacSfx(program);
        }

        public InstrumentType Type { get { return _type; } }

        public bool IsValid { get { return (_instrument != null && _instrument.IsValid); } }
        //        public void saveOrLoad(Serializer *s);
        public void Send(MidiChannel mc)
        {
            if (_instrument != null)
                _instrument.Send(mc);
        }
    }
    
}
