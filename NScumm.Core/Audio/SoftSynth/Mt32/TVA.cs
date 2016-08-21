//
//  TVA.cs
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
    // Note that when entering nextPhase(), newPhase is set to phase + 1, and the descriptions/names below refer to
    // newPhase's value.
    enum TvaPhase
    {
        // In this phase, the base amp (as calculated in calcBasicAmp()) is targeted with an instant time.
        // This phase is entered by reset() only if time[0] != 0.
        BASIC = 0,

        // In this phase, level[0] is targeted within time[0], and velocity potentially affects time
        ATTACK = 1,

        // In this phase, level[1] is targeted within time[1]
        PHASE2 = 2,

        // In this phase, level[2] is targeted within time[2]
        PHASE3 = 3,

        // In this phase, level[3] is targeted within time[3]
        PHASE4 = 4,

        // In this phase, immediately goes to PHASE_RELEASE unless the poly is set to sustain.
        // Aborts the partial if level[3] is 0.
        // Otherwise level[3] is continued, no phase change will occur until some external influence (like pedal release)
        SUSTAIN = 5,

        // In this phase, 0 is targeted within time[4] (the time calculation is quite different from the other phases)
        RELEASE = 6,

        // It's PHASE_DEAD, Jim.
        DEAD = 7
    }

    class TVA
    {
        // CONFIRMED: Matches a table in ROM - haven't got around to coming up with a formula for it yet.
        static byte[] biasLevelToAmpSubtractionCoeff = { 255, 187, 137, 100, 74, 54, 40, 29, 21, 15, 10, 5, 0 };

        private Partial partial;
        private LA32Ramp ampRamp;
        private MemParams.System system_;

        private Part part;
        private TimbreParam.PartialParam partialParam;
        private MemParams.PatchTemp patchTemp;
        private MemParams.RhythmTemp rhythmTemp;

        private bool playing;

        private int biasAmpSubtraction;
        private int veloAmpSubtraction;
        private int keyTimeSubtraction;

        private byte target;
        private TvaPhase phase;

        public bool IsPlaying { get { return playing; } }

        public TVA(Partial usePartial, LA32Ramp useAmpRamp)
        {
            partial = usePartial;
            ampRamp = useAmpRamp;
            system_ = usePartial.Synth.mt32ram.system;
            phase = TvaPhase.DEAD;
        }

        public void StartDecay()
        {
            if (phase >= TvaPhase.RELEASE)
            {
                return;
            }
            byte newIncrement;
            if (partialParam.tva.envTime[4] == 0)
            {
                newIncrement = 1;
            }
            else {
                newIncrement = (byte)-partialParam.tva.envTime[4];
            }
            // The next time nextPhase() is called, it will think TVA_PHASE_RELEASE has finished and the partial will be aborted
            StartRamp(0, newIncrement, TvaPhase.RELEASE);
        }

        private void StartRamp(byte newTarget, byte newIncrement, TvaPhase newPhase)
        {
            target = newTarget;
            phase = newPhase;
            ampRamp.StartRamp(newTarget, newIncrement);
#if MT32EMU_MONITOR_TVA
            partial.getSynth().printDebug("[+%lu] [Partial %d] TVA,ramp,%d,%d,%d,%d", partial.debugGetSampleNum(), partial.debugGetPartialNum(), (newIncrement & 0x80) ? -1 : 1, (newIncrement & 0x7F), newPhase);
#endif
        }

        public void Reset(Part newPart, TimbreParam.PartialParam newPartialParam, MemParams.RhythmTemp newRhythmTemp)
        {
            part = newPart;
            partialParam = newPartialParam;
            patchTemp = newPart.GetPatchTemp();
            rhythmTemp = newRhythmTemp;

            playing = true;

            var tables = Tables.Instance;

            int key = partial.Poly.Key;
            int velocity = partial.Poly.GetVelocity();

            keyTimeSubtraction = CalcKeyTimeSubtraction(partialParam.tva.envTimeKeyfollow, key);

            biasAmpSubtraction = CalcBiasAmpSubtractions(partialParam, key);
            veloAmpSubtraction = CalcVeloAmpSubtraction(partialParam.tva.veloSensitivity, velocity);

            int newTarget = CalcBasicAmp(tables, partial, system_, partialParam, patchTemp, newRhythmTemp, biasAmpSubtraction, veloAmpSubtraction, part.GetExpression());
            TvaPhase newPhase;
            if (partialParam.tva.envTime[0] == 0)
            {
                // Initially go to the TVA_PHASE_ATTACK target amp, and spend the next phase going from there to the TVA_PHASE_2 target amp
                // Note that this means that velocity never affects time for this partial.
                newTarget += partialParam.tva.envLevel[0];
                newPhase = TvaPhase.ATTACK; // The first target used in nextPhase() will be TVA_PHASE_2
            }
            else {
                // Initially go to the base amp determined by TVA level, part volume, etc., and spend the next phase going from there to the full TVA_PHASE_ATTACK target amp.
                newPhase = TvaPhase.BASIC; // The first target used in nextPhase() will be TVA_PHASE_ATTACK
            }

            ampRamp.Reset();//currentAmp = 0;

            // "Go downward as quickly as possible".
            // Since the current value is 0, the LA32Ramp will notice that we're already at or below the target and trying to go downward,
            // and therefore jump to the target immediately and raise an interrupt.
            StartRamp((byte)newTarget, 0x80 | 127, newPhase);
        }

        public void HandleInterrupt()
        {
            NextPhase();
        }

        private static int CalcBasicAmp(Tables tables, Partial partial, MemParams.System system_, TimbreParam.PartialParam partialParam, MemParams.PatchTemp patchTemp, MemParams.RhythmTemp rhythmTemp, int biasAmpSubtraction, int veloAmpSubtraction, byte expression)
        {
            int amp = 155;

            if (!partial.IsRingModulatingSlave)
            {
                amp -= tables.masterVolToAmpSubtraction[system_.masterVol];
                if (amp < 0)
                {
                    return 0;
                }
                amp -= tables.levelToAmpSubtraction[patchTemp.OutputLevel];
                if (amp < 0)
                {
                    return 0;
                }
                amp -= tables.levelToAmpSubtraction[expression];
                if (amp < 0)
                {
                    return 0;
                }
                if (rhythmTemp != null)
                {
                    amp -= tables.levelToAmpSubtraction[rhythmTemp.OutputLevel];
                    if (amp < 0)
                    {
                        return 0;
                    }
                }
            }
            amp -= biasAmpSubtraction;
            if (amp < 0)
            {
                return 0;
            }
            amp -= tables.levelToAmpSubtraction[partialParam.tva.level];
            if (amp < 0)
            {
                return 0;
            }
            amp -= veloAmpSubtraction;
            if (amp < 0)
            {
                return 0;
            }
            if (amp > 155)
            {
                amp = 155;
            }
            amp -= partialParam.tvf.resonance >> 1;
            if (amp < 0)
            {
                return 0;
            }
            return amp;
        }

        private static int CalcVeloAmpSubtraction(byte veloSensitivity, int velocity)
        {
            // FIXME:KG: Better variable names
            int velocityMult = veloSensitivity - 50;
            int absVelocityMult = velocityMult < 0 ? -velocityMult : velocityMult;
            velocityMult = ((int)((uint)(velocityMult * (velocity - 64)) << 2));
            return absVelocityMult - (velocityMult >> 8); // PORTABILITY NOTE: Assumes arithmetic shift
        }

        private static int CalcBiasAmpSubtractions(TimbreParam.PartialParam partialParam, int key)
        {
            int biasAmpSubtraction1 = CalcBiasAmpSubtraction(partialParam.tva.biasPoint1, partialParam.tva.biasLevel1, key);
            if (biasAmpSubtraction1 > 255)
            {
                return 255;
            }
            int biasAmpSubtraction2 = CalcBiasAmpSubtraction(partialParam.tva.biasPoint2, partialParam.tva.biasLevel2, key);
            if (biasAmpSubtraction2 > 255)
            {
                return 255;
            }
            int biasAmpSubtraction = biasAmpSubtraction1 + biasAmpSubtraction2;
            if (biasAmpSubtraction > 255)
            {
                return 255;
            }
            return biasAmpSubtraction;
        }

        public void StartAbort()
        {
            StartRamp(64, 0x80 | 127, TvaPhase.RELEASE);
        }

        private static int MultBias(byte biasLevel, int bias)
        {
            return (bias * biasLevelToAmpSubtractionCoeff[biasLevel]) >> 5;
        }

        private static int CalcBiasAmpSubtraction(byte biasPoint, byte biasLevel, int key)
        {
            if ((biasPoint & 0x40) == 0)
            {
                int bias = biasPoint + 33 - key;
                if (bias > 0)
                {
                    return MultBias(biasLevel, bias);
                }
            }
            else {
                int bias = biasPoint - 31 - key;
                if (bias < 0)
                {
                    bias = -bias;
                    return MultBias(biasLevel, bias);
                }
            }
            return 0;
        }

        private int CalcKeyTimeSubtraction(byte envTimeKeyfollow, int key)
        {
            if (envTimeKeyfollow == 0)
            {
                return 0;
            }
            return (key - 60) >> (5 - envTimeKeyfollow); // PORTABILITY NOTE: Assumes arithmetic shift
        }

        private void NextPhase()
        {
            Tables tables = Tables.Instance;

            if (phase >= TvaPhase.DEAD || !playing)
            {
                //partial.Synth.PrintDebug("TVA::nextPhase(): Shouldn't have got here with phase %d, playing=%s", phase, playing ? "true" : "false");
                return;
            }
            TvaPhase newPhase = (TvaPhase)(phase + 1);

            if (newPhase == TvaPhase.DEAD)
            {
                End(newPhase);
                return;
            }

            bool allLevelsZeroFromNowOn = false;
            if (partialParam.tva.envLevel[3] == 0)
            {
                if (newPhase == TvaPhase.PHASE4)
                {
                    allLevelsZeroFromNowOn = true;
                }
                else if (partialParam.tva.envLevel[2] == 0)
                {
                    if (newPhase == TvaPhase.PHASE3)
                    {
                        allLevelsZeroFromNowOn = true;
                    }
                    else if (partialParam.tva.envLevel[1] == 0)
                    {
                        if (newPhase == TvaPhase.PHASE2)
                        {
                            allLevelsZeroFromNowOn = true;
                        }
                        else if (partialParam.tva.envLevel[0] == 0)
                        {
                            if (newPhase == TvaPhase.ATTACK)
                            { // this line added, missing in ROM - FIXME: Add description of repercussions
                                allLevelsZeroFromNowOn = true;
                            }
                        }
                    }
                }
            }

            int newTarget;
            int newIncrement = 0; // Initialised to please compilers
            int envPointIndex = (int)phase;

            if (!allLevelsZeroFromNowOn)
            {
                newTarget = CalcBasicAmp(tables, partial, system_, partialParam, patchTemp, rhythmTemp, biasAmpSubtraction, veloAmpSubtraction, part.GetExpression());

                if (newPhase == TvaPhase.SUSTAIN || newPhase == TvaPhase.RELEASE)
                {
                    if (partialParam.tva.envLevel[3] == 0)
                    {
                        End(newPhase);
                        return;
                    }
                    if (!partial.Poly.CanSustain)
                    {
                        newPhase = TvaPhase.RELEASE;
                        newTarget = 0;
                        newIncrement = -partialParam.tva.envTime[4];
                        if (newIncrement == 0)
                        {
                            // We can't let the increment be 0, or there would be no emulated interrupt.
                            // So we do an "upward" increment, which should set the amp to 0 extremely quickly
                            // and cause an "interrupt" to bring us back to nextPhase().
                            newIncrement = 1;
                        }
                    }
                    else {
                        newTarget += partialParam.tva.envLevel[3];
                        newIncrement = 0;
                    }
                }
                else {
                    newTarget += partialParam.tva.envLevel[envPointIndex];
                }
            }
            else {
                newTarget = 0;
            }

            if ((newPhase != TvaPhase.SUSTAIN && newPhase != TvaPhase.RELEASE) || allLevelsZeroFromNowOn)
            {
                int envTimeSetting = partialParam.tva.envTime[envPointIndex];

                if (newPhase == TvaPhase.ATTACK)
                {
                    envTimeSetting -= (partial.Poly.GetVelocity() - 64) >> (6 - partialParam.tva.envTimeVeloSensitivity); // PORTABILITY NOTE: Assumes arithmetic shift

                    if (envTimeSetting <= 0 && partialParam.tva.envTime[envPointIndex] != 0)
                    {
                        envTimeSetting = 1;
                    }
                }
                else {
                    envTimeSetting -= keyTimeSubtraction;
                }
                if (envTimeSetting > 0)
                {
                    int targetDelta = newTarget - target;
                    if (targetDelta <= 0)
                    {
                        if (targetDelta == 0)
                        {
                            // target and newTarget are the same.
                            // We can't have an increment of 0 or we wouldn't get an emulated interrupt.
                            // So instead make the target one less than it really should be and set targetDelta accordingly.
                            targetDelta = -1;
                            newTarget--;
                            if (newTarget < 0)
                            {
                                // Oops, newTarget is less than zero now, so let's do it the other way:
                                // Make newTarget one more than it really should've been and set targetDelta accordingly.
                                // FIXME (apparent bug in real firmware):
                                // This means targetDelta will be positive just below here where it's inverted, and we'll end up using envLogarithmicTime[-1], and we'll be setting newIncrement to be descending later on, etc..
                                targetDelta = 1;
                                newTarget = -newTarget;
                            }
                        }
                        targetDelta = -targetDelta;
                        newIncrement = tables.envLogarithmicTime[(byte)targetDelta] - envTimeSetting;
                        if (newIncrement <= 0)
                        {
                            newIncrement = 1;
                        }
                        newIncrement = newIncrement | 0x80;
                    }
                    else {
                        // FIXME: The last 22 or so entries in this table are 128 - surely that fucks things up, since that ends up being -128 signed?
                        newIncrement = tables.envLogarithmicTime[(byte)targetDelta] - envTimeSetting;
                        if (newIncrement <= 0)
                        {
                            newIncrement = 1;
                        }
                    }
                }
                else {
                    newIncrement = newTarget >= target ? (0x80 | 127) : 127;
                }

                // FIXME: What's the point of this? It's checked or set to non-zero everywhere above
                if (newIncrement == 0)
                {
                    newIncrement = 1;
                }
            }

            StartRamp((byte)newTarget, (byte)newIncrement, newPhase);
        }

        private void End(TvaPhase newPhase)
        {
            phase = newPhase;
            playing = false;
#if MT32EMU_MONITOR_TVA
    partial->getSynth()->printDebug("[+%lu] [Partial %d] TVA,end,%d", partial->debugGetSampleNum(), partial->debugGetPartialNum(), newPhase);
#endif
        }
   }
}
