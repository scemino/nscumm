//
//  SnareDrumOperator.cs
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
using EnvelopeGenerator = NScumm.Core.Audio.OPL.OPL3.EnvelopeGenerator;
using OperatorData = NScumm.Core.Audio.OPL.OPL3.OperatorData;

namespace NScumm.Core.Audio.OPL
{
    class SnareDrumOperator : Operator
    {
        const int snareDrumOperatorBaseAddress = 0x14;

        internal SnareDrumOperator()
            : base(snareDrumOperatorBaseAddress)
        {
        }

        public override double getOperatorOutput(double modulator)
        {
            if (envelopeGenerator.stage == EnvelopeGenerator.Stage.OFF)
                return 0;

            double envelopeInDB = envelopeGenerator.getEnvelope(egt, am);
            envelope = Math.Pow(10, envelopeInDB / 10.0);

            // If it is in OPL2 mode, use first four waveforms only:
            int waveIndex = ws & ((OPL3._new << 2) + 3);
            double[] waveform = OperatorData.waveforms[waveIndex];

            phase = OPL3.highHatOperator.phase * 2;

            double operatorOutput = getOutput(modulator, phase, waveform);

            double noise = new Random().NextDouble() * envelope;

            if (operatorOutput / envelope != 1 && operatorOutput / envelope != -1)
            {
                if (operatorOutput > 0)
                    operatorOutput = noise;
                else if (operatorOutput < 0)
                    operatorOutput = -noise;
                else
                    operatorOutput = 0;
            }

            return operatorOutput * 2;
        }
    }
}

