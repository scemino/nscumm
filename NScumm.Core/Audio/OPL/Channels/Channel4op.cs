//
//  Channel4op.cs
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

using Operator = NScumm.Core.Audio.OPL.Operator;
using EnvelopeGenerator = NScumm.Core.Audio.OPL.OPL3.EnvelopeGenerator;
using ChannelData = NScumm.Core.Audio.OPL.OPL3.ChannelData;
using System.Text;

namespace NScumm.Core.Audio.OPL
{
    class Channel4op : Channel
    {
        Operator op1, op2, op3, op4;

        internal Channel4op(int baseAddress, Operator o1, Operator o2, Operator o3, Operator o4)
            : base(baseAddress)
        {
            op1 = o1;
            op2 = o2;
            op3 = o3;
            op4 = o4;
        }

        public override double[] getChannelOutput()
        {
            double channelOutput = 0,
            op1Output = 0, op2Output = 0, op3Output = 0, op4Output = 0;

            double[] output;

            int secondChannelBaseAddress = channelBaseAddress + 3;
            int secondCnt = OPL3.registers[secondChannelBaseAddress + ChannelData.CHD1_CHC1_CHB1_CHA1_FB3_CNT1_Offset] & 0x1;
            int cnt4op = (cnt << 1) | secondCnt;

            double feedbackOutput = (feedback[0] + feedback[1]) / 2;

            switch (cnt4op)
            {
                case 0:
                    if (op4.envelopeGenerator.stage == EnvelopeGenerator.Stage.OFF)
                        return getInFourChannels(0);

                    op1Output = op1.getOperatorOutput(feedbackOutput);
                    op2Output = op2.getOperatorOutput(op1Output * toPhase);
                    op3Output = op3.getOperatorOutput(op2Output * toPhase);
                    channelOutput = op4.getOperatorOutput(op3Output * toPhase);

                    break;
                case 1:
                    if (op2.envelopeGenerator.stage == EnvelopeGenerator.Stage.OFF &&
                        op4.envelopeGenerator.stage == EnvelopeGenerator.Stage.OFF)
                        return getInFourChannels(0);

                    op1Output = op1.getOperatorOutput(feedbackOutput);
                    op2Output = op2.getOperatorOutput(op1Output * toPhase);

                    op3Output = op3.getOperatorOutput(Operator.noModulator);
                    op4Output = op4.getOperatorOutput(op3Output * toPhase);

                    channelOutput = (op2Output + op4Output) / 2;
                    break;
                case 2:
                    if (op1.envelopeGenerator.stage == EnvelopeGenerator.Stage.OFF &&
                        op4.envelopeGenerator.stage == EnvelopeGenerator.Stage.OFF)
                        return getInFourChannels(0);

                    op1Output = op1.getOperatorOutput(feedbackOutput);

                    op2Output = op2.getOperatorOutput(Operator.noModulator);
                    op3Output = op3.getOperatorOutput(op2Output * toPhase);
                    op4Output = op4.getOperatorOutput(op3Output * toPhase);

                    channelOutput = (op1Output + op4Output) / 2;
                    break;
                case 3:
                    if (op1.envelopeGenerator.stage == EnvelopeGenerator.Stage.OFF &&
                        op3.envelopeGenerator.stage == EnvelopeGenerator.Stage.OFF &&
                        op4.envelopeGenerator.stage == EnvelopeGenerator.Stage.OFF)
                        return getInFourChannels(0);

                    op1Output = op1.getOperatorOutput(feedbackOutput);

                    op2Output = op2.getOperatorOutput(Operator.noModulator);
                    op3Output = op3.getOperatorOutput(op2Output * toPhase);

                    op4Output = op4.getOperatorOutput(Operator.noModulator);

                    channelOutput = (op1Output + op3Output + op4Output) / 3;
                    break;
            }

            feedback[0] = feedback[1];
            feedback[1] = (op1Output * ChannelData.feedback[fb]) % 1;

            output = getInFourChannels(channelOutput);
            return output;
        }

        protected override void keyOn()
        {
            op1.keyOn();
            op2.keyOn();
            op3.keyOn();
            op4.keyOn();
            feedback[0] = feedback[1] = 0;
        }

        protected override void keyOff()
        {
            op1.keyOff();
            op2.keyOff();
            op3.keyOff();
            op4.keyOff();
        }

        protected override void updateOperators()
        {
            // Key Scale Number, used in EnvelopeGenerator.setActualRates().
            int keyScaleNumber = block * 2 + ((fnumh >> OPL3.nts) & 0x01);
            int f_number = (fnumh << 8) | fnuml;
            op1.updateOperator(keyScaleNumber, f_number, block);
            op2.updateOperator(keyScaleNumber, f_number, block);
            op3.updateOperator(keyScaleNumber, f_number, block);
            op4.updateOperator(keyScaleNumber, f_number, block);
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();

            int f_number = (fnumh << 8) + fnuml;

            str.AppendFormat("channelBaseAddress: {0}\n", channelBaseAddress);
            str.AppendFormat("f_number: {0}, block: {1}\n", f_number, block);
            str.AppendFormat("cnt: {0}, feedback: {1}\n", cnt, fb);
            str.AppendFormat("op1:\n{0}", op1);
            str.AppendFormat("op2:\n{0}", op2);
            str.AppendFormat("op3:\n{0}", op3);
            str.AppendFormat("op4:\n{0}", op4);

            return str.ToString();
        }
    }
}

