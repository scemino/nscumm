//
//  HighHatOperator.cs
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
    class HighHatOperator : TopCymbalOperator
    {
        const int highHatOperatorBaseAddress = 0x11;

        internal HighHatOperator()
            : base(highHatOperatorBaseAddress)
        {

        }

        public override double getOperatorOutput(double modulator)
        {
            double topCymbalOperatorPhase =
                OPL3.topCymbalOperator.phase * OperatorData.multTable[OPL3.topCymbalOperator.mult];
            // The sound output from the High Hat resembles the one from
            // Top Cymbal, so we use the parent method and modifies his output
            // accordingly afterwards.
            double operatorOutput = base.getOperatorOutput(modulator, topCymbalOperatorPhase);
            if (operatorOutput == 0)
                operatorOutput = new Random().NextDouble() * envelope;
            return operatorOutput;
        }
    }
}

