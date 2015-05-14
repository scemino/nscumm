//
//  Voice.cs
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

/*
 *  This file is based on reSID, a MOS6581 SID emulator engine.
 *  Copyright (C) 2004  Dag Lem <resid@nimrod.no>
 */

namespace NScumm.Core.Audio.SoftSynth
{
    class Voice
    {
        public EnvelopeGenerator Envelope { get; private set;}
        public WaveformGenerator Wave { get; private set;}

        public Voice()
        {
            wave_zero = 0x380;
            voice_DC = 0x800 * 0xff;
            Envelope = new EnvelopeGenerator();
            Wave = new WaveformGenerator();
        }

        public void SetSyncSource(Voice source)
        {
            Wave.SetSyncSource(source.Wave);
        }

        public void Reset()
        {
            Wave.Reset();
            Envelope.Reset();
        }

        public void WriteCONTROL_REG(int control)
        {
            Wave.WriteCONTROL_REG(control);
            Envelope.WriteCONTROL_REG(control);
        }

        // Amplitude modulated waveform output.
        // Range [-2048*255, 2047*255].
        public int Output()
        {
            // Multiply oscillator output with envelope output.
            return (Wave.Output() - wave_zero) * Envelope.Output() + voice_DC;
        }


        // Waveform D/A zero level.
        int wave_zero;

        // Multiplying D/A DC offset.
        int voice_DC;
    }
    
}
