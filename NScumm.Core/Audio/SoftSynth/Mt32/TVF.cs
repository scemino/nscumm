//
//  TVF.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    class TVF
    {
        // When this is the target phase, level[0] is targeted within time[0]
        // Note that this phase is always set up in reset(), not nextPhase()
        const int PHASE_ATTACK = 1;

        // When this is the target phase, level[1] is targeted within time[1]
        const int PHASE_2 = 2;

        // When this is the target phase, level[2] is targeted within time[2]
        const int PHASE_3 = 3;

        // When this is the target phase, level[3] is targeted within time[3]
        const int PHASE_4 = 4;

        // When this is the target phase, immediately goes to PHASE_RELEASE unless the poly is set to sustain.
        // Otherwise level[3] is continued with increment 0 - no phase change will occur until some external influence (like pedal release)
        const int PHASE_SUSTAIN = 5;

        // 0 is targeted within time[4] (the time calculation is quite different from the other phases)
        const int PHASE_RELEASE = 6;

        // 0 is targeted with increment 0 (thus theoretically staying that way forever)
        const int PHASE_DONE = 7;

        // This table matches the values used by a real LAPC-I.
        static readonly sbyte[] biasLevelToBiasMult = { 85, 42, 21, 16, 10, 5, 2, 0, -2, -5, -10, -16, -21, -74, -85 };
        // These values represent unique options with no consistent pattern, so we have to use something like a table in any case.
        // The table entries, when divided by 21, match approximately what the manual claims:
        // -1, -1/2, -1/4, 0, 1/8, 1/4, 3/8, 1/2, 5/8, 3/4, 7/8, 1, 5/4, 3/2, 2, s1, s2
        // Note that the entry for 1/8 is rounded to 2 (from 1/8 * 21 = 2.625), which seems strangely inaccurate compared to the others.
        static readonly sbyte[] keyfollowMult21 = { -21, -10, -5, 0, 2, 5, 8, 10, 13, 16, 18, 21, 26, 32, 42, 21, 21 };


        readonly Partial partial;
        LA32Ramp cutoffModifierRamp;
        TimbreParam.PartialParam partialParam;

        byte baseCutoff;
        int keyTimeSubtraction;
        int levelMult;

        byte target;
        int phase;

        public TVF(Partial partial, LA32Ramp cutoffModifierRamp)
        {
            this.partial = partial;
            this.cutoffModifierRamp = cutoffModifierRamp;
        }

        public void StartDecay()
        {
            if (phase >= PHASE_RELEASE)
            {
                return;
            }
            if (partialParam.tvf.envTime[4] == 0)
            {
                StartRamp(0, 1, PHASE_DONE - 1);
            }
            else {
                StartRamp(0, (byte)-partialParam.tvf.envTime[4], PHASE_DONE - 1);
            }
        }

        private void StartRamp(byte newTarget, byte newIncrement, int newPhase)
        {
            target = newTarget;
            phase = newPhase;
            cutoffModifierRamp.StartRamp(newTarget, newIncrement);
#if MT32EMU_MONITOR_TVF
    partial.getSynth().printDebug("[+%lu] [Partial %d] TVF,ramp,%d,%d,%d,%d", partial.debugGetSampleNum(), partial.debugGetPartialNum(), newTarget, (newIncrement & 0x80) ? -1 : 1, (newIncrement & 0x7F), newPhase);
#endif
        }

        public void Reset(TimbreParam.PartialParam newPartialParam, int basePitch)
        {
            partialParam = newPartialParam;

            int key = partial.Poly.Key;
            int velocity = partial.Poly.GetVelocity();

            Tables tables = Tables.Instance;

            baseCutoff = (byte)CalcBaseCutoff(newPartialParam, basePitch, key);
#if MT32EMU_MONITOR_TVF
    partial.getSynth().printDebug("[+%lu] [Partial %d] TVF,base,%d", partial.debugGetSampleNum(), partial.debugGetPartialNum(), baseCutoff);
#endif

            int newLevelMult = velocity * newPartialParam.tvf.envVeloSensitivity;
            newLevelMult >>= 6;
            newLevelMult += 109 - newPartialParam.tvf.envVeloSensitivity;
            newLevelMult += (key - 60) >> (4 - newPartialParam.tvf.envDepthKeyfollow);
            if (newLevelMult < 0)
            {
                newLevelMult = 0;
            }
            newLevelMult *= newPartialParam.tvf.envDepth;
            newLevelMult >>= 6;
            if (newLevelMult > 255)
            {
                newLevelMult = 255;
            }
            levelMult = newLevelMult;

            if (newPartialParam.tvf.envTimeKeyfollow != 0)
            {
                keyTimeSubtraction = (key - 60) >> (5 - newPartialParam.tvf.envTimeKeyfollow);
            }
            else {
                keyTimeSubtraction = 0;
            }

            int newTarget = (newLevelMult * newPartialParam.tvf.envLevel[0]) >> 8;
            int envTimeSetting = newPartialParam.tvf.envTime[0] - keyTimeSubtraction;
            int newIncrement;
            if (envTimeSetting <= 0)
            {
                newIncrement = (0x80 | 127);
            }
            else {
                newIncrement = tables.envLogarithmicTime[newTarget] - envTimeSetting;
                if (newIncrement <= 0)
                {
                    newIncrement = 1;
                }
            }
            cutoffModifierRamp.Reset();
            StartRamp((byte)newTarget, (byte)newIncrement, PHASE_2 - 1);
        }

        private static int CalcBaseCutoff(TimbreParam.PartialParam partialParam, int basePitch, int key)
        {
            int baseCutoff = keyfollowMult21[partialParam.tvf.keyfollow] - keyfollowMult21[partialParam.wg.pitchKeyfollow];
            // baseCutoff range now: -63 to 63
            baseCutoff *= key - 60;
            // baseCutoff range now: -3024 to 3024
            int biasPoint = partialParam.tvf.biasPoint;
            if ((biasPoint & 0x40) == 0)
            {
                // biasPoint range here: 0 to 63
                int bias = biasPoint + 33 - key; // bias range here: -75 to 84
                if (bias > 0)
                {
                    bias = -bias; // bias range here: -1 to -84
                    baseCutoff += bias * biasLevelToBiasMult[partialParam.tvf.biasLevel]; // Calculation range: -7140 to 7140
                                                                                           // baseCutoff range now: -10164 to 10164
                }
            }
            else {
                // biasPoint range here: 64 to 127
                int bias = biasPoint - 31 - key; // bias range here: -75 to 84
                if (bias < 0)
                {
                    baseCutoff += bias * biasLevelToBiasMult[partialParam.tvf.biasLevel]; // Calculation range: −6375 to 6375
                                                                                           // baseCutoff range now: -9399 to 9399
                }
            }
            // baseCutoff range now: -10164 to 10164
            baseCutoff += ((partialParam.tvf.cutoff << 4) - 800);
            // baseCutoff range now: -10964 to 10964
            if (baseCutoff >= 0)
            {
                // FIXME: Potentially bad if baseCutoff ends up below -2056?
                int pitchDeltaThing = (basePitch >> 4) + baseCutoff - 3584;
                if (pitchDeltaThing > 0)
                {
                    baseCutoff -= pitchDeltaThing;
                }
            }
            else if (baseCutoff < -2048)
            {
                baseCutoff = -2048;
            }
            baseCutoff += 2056;
            baseCutoff >>= 4; // PORTABILITY NOTE: Hmm... Depends whether it could've been below -2056, but maybe arithmetic shift assumed?
            if (baseCutoff > 255)
            {
                baseCutoff = 255;
            }
            return baseCutoff;
        }

        public void HandleInterrupt()
        {
            NextPhase();
        }

        private void NextPhase()
        {
            Tables tables = Tables.Instance;
            int newPhase = phase + 1;

            switch (newPhase)
            {
                case PHASE_DONE:
                    StartRamp(0, 0, newPhase);
                    return;
                case PHASE_SUSTAIN:
                case PHASE_RELEASE:
                    // FIXME: Afaict newPhase should never be PHASE_RELEASE here. And if it were, this is an odd way to handle it.
                    if (!partial.Poly.CanSustain)
                    {
                        phase = newPhase; // FIXME: Correct?
                        StartDecay(); // FIXME: This should actually start decay even if phase is already 6. Does that matter?
                        return;
                    }
                    StartRamp((byte)((levelMult * partialParam.tvf.envLevel[3]) >> 8), 0, newPhase);
                    return;
            }

            int envPointIndex = phase;
            int envTimeSetting = partialParam.tvf.envTime[envPointIndex] - keyTimeSubtraction;

            int newTarget = (levelMult * partialParam.tvf.envLevel[envPointIndex]) >> 8;
            int newIncrement;
            if (envTimeSetting > 0)
            {
                int targetDelta = newTarget - target;
                if (targetDelta == 0)
                {
                    if (newTarget == 0)
                    {
                        targetDelta = 1;
                        newTarget = 1;
                    }
                    else {
                        targetDelta = -1;
                        newTarget--;
                    }
                }
                newIncrement = tables.envLogarithmicTime[targetDelta < 0 ? -targetDelta : targetDelta] - envTimeSetting;
                if (newIncrement <= 0)
                {
                    newIncrement = 1;
                }
                if (targetDelta < 0)
                {
                    newIncrement |= 0x80;
                }
            }
            else {
                newIncrement = newTarget >= target ? (0x80 | 127) : 127;
            }
            StartRamp((byte)newTarget, (byte)newIncrement, newPhase);
        }

        public byte GetBaseCutoff()
        {
            return baseCutoff;
        }
    }
}
