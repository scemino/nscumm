//
//  TopCymbalOperator.cs
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
using System;
using OperatorData = NScumm.Core.Audio.OPL.OPL3.OperatorData;

namespace NScumm.Core.Audio.OPL
{
    class TopCymbalOperator : Operator
    {
        const int topCymbalOperatorBaseAddress = 0x15;

        internal TopCymbalOperator(int baseAddress)
            : base(baseAddress)
        {

        }

        internal TopCymbalOperator()
            : this(topCymbalOperatorBaseAddress)
        {

        }

        public override double getOperatorOutput(double modulator)
        {
            double highHatOperatorPhase =
                OPL3.highHatOperator.phase * OperatorData.multTable[OPL3.highHatOperator.mult];
            // The Top Cymbal operator uses his own phase together with the High Hat phase.
            return getOperatorOutput(modulator, highHatOperatorPhase);
        }
        // This method is used here with the HighHatOperator phase
        // as the externalPhase.
        // Conversely, this method is also used through inheritance by the HighHatOperator,
        // now with the TopCymbalOperator phase as the externalPhase.
        protected double getOperatorOutput(double modulator, double externalPhase)
        {
            double envelopeInDB = envelopeGenerator.getEnvelope(egt, am);
            envelope = Math.Pow(10, envelopeInDB / 10.0);

            phase = phaseGenerator.getPhase(vib);

            int waveIndex = ws & ((OPL3._new << 2) + 3);
            double[] waveform = OperatorData.waveforms[waveIndex];

            // Empirically tested multiplied phase for the Top Cymbal:
            double carrierPhase = (8 * phase) % 1;
            double modulatorPhase = externalPhase;
            double modulatorOutput = getOutput(Operator.noModulator, modulatorPhase, waveform);
            double carrierOutput = getOutput(modulatorOutput, carrierPhase, waveform);

            int cycles = 4;
            if ((carrierPhase * cycles) % cycles > 0.1)
                carrierOutput = 0;

            return carrierOutput * 2;
        }
    }
}

