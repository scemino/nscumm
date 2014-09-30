//
//  Operator.cs
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
using OPL3Data = NScumm.Core.Audio.OPL.OPL3.OPL3Data;
using EnvelopeGenerator = NScumm.Core.Audio.OPL.OPL3.EnvelopeGenerator;
using System.Text;

namespace NScumm.Core.Audio.OPL
{
    class Operator
    {
        internal OPL3.PhaseGenerator phaseGenerator;
        internal EnvelopeGenerator envelopeGenerator;
        public double envelope, phase;
        public int operatorBaseAddress;
        public int am, vib, ksr, egt, mult, ksl, tl, ar, dr, sl, rr, ws;
        public int keyScaleNumber, f_number, block;
        public const double noModulator = 0;

        internal Operator(int baseAddress)
        {
            operatorBaseAddress = baseAddress;
            phaseGenerator = new OPL3.PhaseGenerator();
            envelopeGenerator = new OPL3.EnvelopeGenerator();

            envelope = 0;
            am = vib = ksr = egt = mult = ksl = tl = ar = dr = sl = rr = ws = 0;
            keyScaleNumber = f_number = block = 0;
        }

        internal void update_AM1_VIB1_EGT1_KSR1_MULT4()
        {

            int am1_vib1_egt1_ksr1_mult4 = OPL3.registers[operatorBaseAddress + OperatorData.AM1_VIB1_EGT1_KSR1_MULT4_Offset];

            // Amplitude Modulation. This register is used int EnvelopeGenerator.getEnvelope();
            am = (am1_vib1_egt1_ksr1_mult4 & 0x80) >> 7;
            // Vibrato. This register is used in PhaseGenerator.getPhase();
            vib = (am1_vib1_egt1_ksr1_mult4 & 0x40) >> 6;
            // Envelope Generator Type. This register is used in EnvelopeGenerator.getEnvelope();
            egt = (am1_vib1_egt1_ksr1_mult4 & 0x20) >> 5;
            // Key Scale Rate. Sets the actual envelope rate together with rate and keyScaleNumber.
            // This register os used in EnvelopeGenerator.setActualAttackRate().
            ksr = (am1_vib1_egt1_ksr1_mult4 & 0x10) >> 4;
            // Multiple. Multiplies the Channel.baseFrequency to get the Operator.operatorFrequency.
            // This register is used in PhaseGenerator.setFrequency().
            mult = am1_vib1_egt1_ksr1_mult4 & 0x0F;

            phaseGenerator.setFrequency(f_number, block, mult);
            envelopeGenerator.setActualAttackRate(ar, ksr, keyScaleNumber);
            envelopeGenerator.setActualDecayRate(dr, ksr, keyScaleNumber);
            envelopeGenerator.setActualReleaseRate(rr, ksr, keyScaleNumber);
        }

        internal void update_KSL2_TL6()
        {

            int ksl2_tl6 = OPL3.registers[operatorBaseAddress + OperatorData.KSL2_TL6_Offset];

            // Key Scale Level. Sets the attenuation in accordance with the octave.
            ksl = (ksl2_tl6 & 0xC0) >> 6;
            // Total Level. Sets the overall damping for the envelope.
            tl = ksl2_tl6 & 0x3F;

            envelopeGenerator.setAtennuation(f_number, block, ksl);
            envelopeGenerator.setTotalLevel(tl);
        }

        internal void update_AR4_DR4()
        {

            int ar4_dr4 = OPL3.registers[operatorBaseAddress + OperatorData.AR4_DR4_Offset];

            // Attack Rate.
            ar = (ar4_dr4 & 0xF0) >> 4;
            // Decay Rate.
            dr = ar4_dr4 & 0x0F;

            envelopeGenerator.setActualAttackRate(ar, ksr, keyScaleNumber);
            envelopeGenerator.setActualDecayRate(dr, ksr, keyScaleNumber);
        }

        internal void update_SL4_RR4()
        {

            int sl4_rr4 = OPL3.registers[operatorBaseAddress + OperatorData.SL4_RR4_Offset];

            // Sustain Level.
            sl = (sl4_rr4 & 0xF0) >> 4;
            // Release Rate.
            rr = sl4_rr4 & 0x0F;

            envelopeGenerator.setActualSustainLevel(sl);
            envelopeGenerator.setActualReleaseRate(rr, ksr, keyScaleNumber);
        }

        internal void update_5_WS3()
        {
            int _5_ws3 = OPL3.registers[operatorBaseAddress + OperatorData._5_WS3_Offset];
            ws = _5_ws3 & 0x07;
        }

        public virtual double getOperatorOutput(double modulator)
        {
            if (envelopeGenerator.stage == EnvelopeGenerator.Stage.OFF)
                return 0;

            double envelopeInDB = envelopeGenerator.getEnvelope(egt, am);
            envelope = Math.Pow(10, envelopeInDB / 10.0);

            // If it is in OPL2 mode, use first four waveforms only:
            ws &= ((OPL3._new << 2) + 3);
            double[] waveform = OperatorData.waveforms[ws];

            phase = phaseGenerator.getPhase(vib);

            double operatorOutput = getOutput(modulator, phase, waveform);
            return operatorOutput;
        }

        protected double getOutput(double modulator, double outputPhase, double[] waveform)
        {
            outputPhase = (outputPhase + modulator) % 1;
            if (outputPhase < 0)
            {
                outputPhase++;
                // If the double could not afford to be less than 1:
                outputPhase %= 1;
            }
            int sampleIndex = (int)(outputPhase * OperatorData.waveLength);
            return waveform[sampleIndex] * envelope;
        }

        internal void keyOn()
        {
            if (ar > 0)
            {
                envelopeGenerator.keyOn();
                phaseGenerator.keyOn();
            }
            else
                envelopeGenerator.stage = EnvelopeGenerator.Stage.OFF;
        }

        internal void keyOff()
        {
            envelopeGenerator.keyOff();
        }

        protected internal void updateOperator(int ksn, int f_num, int blk)
        {
            keyScaleNumber = ksn;
            f_number = f_num;
            block = blk;
            update_AM1_VIB1_EGT1_KSR1_MULT4();
            update_KSL2_TL6();
            update_AR4_DR4();
            update_SL4_RR4();
            update_5_WS3();
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            double operatorFrequency = f_number * Math.Pow(2, block - 1) * OPL3Data.sampleRate / Math.Pow(2, 19) * OperatorData.multTable[mult];

            str.AppendFormat("operatorBaseAddress: %d\n", operatorBaseAddress);
            str.AppendFormat("operatorFrequency: %f\n", operatorFrequency);
            str.AppendFormat("mult: %d, ar: %d, dr: %d, sl: %d, rr: %d, ws: %d\n", mult, ar, dr, sl, rr, ws);
            str.AppendFormat("am: %d, vib: %d, ksr: %d, egt: %d, ksl: %d, tl: %d\n", am, vib, ksr, egt, ksl, tl);

            return str.ToString();
        }
    }
}

