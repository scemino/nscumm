//
//  TVP.cs
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
using System;
using NScumm.Core.Audio.SoftSynth.Mt32;

namespace NScumm.Core
{
    class TVP
    {
        // FIXME: Add Explanation
        static ushort[] lowerDurationToDivisor = { 34078, 37162, 40526, 44194, 48194, 52556, 57312, 62499 };

        // These values represent unique options with no consistent pattern, so we have to use something like a table in any case.
        // The table matches exactly what the manual claims (when divided by 8192):
        // -1, -1/2, -1/4, 0, 1/8, 1/4, 3/8, 1/2, 5/8, 3/4, 7/8, 1, 5/4, 3/2, 2, s1, s2
        // ...except for the last two entries, which are supposed to be "1 cent above 1" and "2 cents above 1", respectively. They can only be roughly approximated with this integer math.
        static short[] pitchKeyfollowMult = { -8192, -4096, -2048, 0, 1024, 2048, 3072, 4096, 5120, 6144, 7168, 8192, 10240, 12288, 16384, 8198, 8226 };

        // Note: Keys < 60 use keyToPitchTable[60 - key], keys >= 60 use keyToPitchTable[key - 60].
        // FIXME: This table could really be shorter, since we never use e.g. key 127.
        static ushort[] keyToPitchTable = {
                0,   341,   683,  1024,  1365,  1707,  2048,  2389,
             2731,  3072,  3413,  3755,  4096,  4437,  4779,  5120,
             5461,  5803,  6144,  6485,  6827,  7168,  7509,  7851,
             8192,  8533,  8875,  9216,  9557,  9899, 10240, 10581,
            10923, 11264, 11605, 11947, 12288, 12629, 12971, 13312,
            13653, 13995, 14336, 14677, 15019, 15360, 15701, 16043,
            16384, 16725, 17067, 17408, 17749, 18091, 18432, 18773,
            19115, 19456, 19797, 20139, 20480, 20821, 21163, 21504,
            21845, 22187, 22528, 22869
        };

        Partial partial;
        readonly MemParams.System system_; // FIXME: Only necessary because masterTune calculation is done in the wrong place atm.

        Part part;
        TimbreParam.PartialParam partialParam;
        MemParams.PatchTemp patchTemp;

        int maxCounter;
        int processTimerIncrement;
        int counter;
        uint timeElapsed;

        int phase;
        int basePitch;
        int targetPitchOffsetWithoutLFO;
        int currentPitchOffset;

        short lfoPitchOffset;
        // In range -12 - 36
        sbyte timeKeyfollowSubtraction;

        short pitchOffsetChangePerBigTick;
        ushort targetPitchOffsetReachedBigTick;
        int shifts;

        ushort pitch;


        public TVP(Partial usePartial)
        {
            partial = usePartial;
            system_ = usePartial.Synth.mt32ram.system;
            // We want to do processing 4000 times per second. FIXME: This is pretty arbitrary.
            maxCounter = Mt32Emu.SAMPLE_RATE / 4000;
            // The timer runs at 500kHz. We only need to bother updating it every maxCounter samples, before we do processing.
            // This is how much to increment it by every maxCounter samples.
            processTimerIncrement = 500000 * maxCounter / Mt32Emu.SAMPLE_RATE;
        }

        public void StartDecay()
        {
            phase = 5;
            lfoPitchOffset = 0;
            targetPitchOffsetReachedBigTick = (ushort)(timeElapsed >> 8); // FIXME: Afaict there's no good reason for this - check
        }

        public ushort NextPitch()
        {
            // FIXME: Write explanation of counter and time increment
            if (counter == 0)
            {
                timeElapsed = (uint)(timeElapsed + processTimerIncrement);
                timeElapsed = timeElapsed & 0x00FFFFFF;
                Process();
            }
            counter = (counter + 1) % maxCounter;
            return pitch;
        }

        private void Process()
        {
            if (phase == 0)
            {
                TargetPitchOffsetReached();
                return;
            }
            if (phase == 5)
            {
                NextPhase();
                return;
            }
            if (phase > 7)
            {
                UpdatePitch();
                return;
            }

            short negativeBigTicksRemaining = (short)((timeElapsed >> 8) - targetPitchOffsetReachedBigTick);
            if (negativeBigTicksRemaining >= 0)
            {
                // We've reached the time for a phase change
                TargetPitchOffsetReached();
                return;
            }
            // FIXME: Write explanation for this stuff
            int rightShifts = shifts;
            if (rightShifts > 13)
            {
                rightShifts -= 13;
                negativeBigTicksRemaining = (short)(negativeBigTicksRemaining >> rightShifts); // PORTABILITY NOTE: Assumes arithmetic shift
                rightShifts = 13;
            }
            int newResult = (negativeBigTicksRemaining * pitchOffsetChangePerBigTick) >> rightShifts; // PORTABILITY NOTE: Assumes arithmetic shift
            newResult += targetPitchOffsetWithoutLFO + lfoPitchOffset;
            currentPitchOffset = newResult;
            UpdatePitch();
        }

        private void TargetPitchOffsetReached()
        {
            currentPitchOffset = targetPitchOffsetWithoutLFO + lfoPitchOffset;

            switch (phase)
            {
                case 3:
                case 4:
                    {
                        int newLFOPitchOffset = (part.GetModulation() * partialParam.pitchLFO.modSensitivity) >> 7;
                        newLFOPitchOffset = (newLFOPitchOffset + partialParam.pitchLFO.depth) << 1;
                        if (pitchOffsetChangePerBigTick > 0)
                        {
                            // Go in the opposite direction to last time
                            newLFOPitchOffset = -newLFOPitchOffset;
                        }
                        lfoPitchOffset = (short)newLFOPitchOffset;
                        int targetPitchOffset = targetPitchOffsetWithoutLFO + lfoPitchOffset;
                        SetupPitchChange(targetPitchOffset, (byte)(101 - partialParam.pitchLFO.rate));
                        UpdatePitch();
                        break;
                    }
                case 6:
                    UpdatePitch();
                    break;
                default:
                    NextPhase();
                    break;
            }
        }

        private void SetupPitchChange(int targetPitchOffset, byte changeDuration)
        {
            bool negativeDelta = targetPitchOffset < currentPitchOffset;
            int pitchOffsetDelta = targetPitchOffset - currentPitchOffset;
            if (pitchOffsetDelta > 32767 || pitchOffsetDelta < -32768)
            {
                pitchOffsetDelta = 32767;
            }
            if (negativeDelta)
            {
                pitchOffsetDelta = -pitchOffsetDelta;
            }
            // We want to maximise the number of bits of the Bit16s "pitchOffsetChangePerBigTick" we use in order to get the best possible precision later
            int absPitchOffsetDelta = pitchOffsetDelta << 16;
            byte normalisationShifts = Normalise(ref absPitchOffsetDelta); // FIXME: Double-check: normalisationShifts is usually between 0 and 15 here, unless the delta is 0, in which case it's 31
            absPitchOffsetDelta = absPitchOffsetDelta >> 1; // Make room for the sign bit

            changeDuration--; // changeDuration's now between 0 and 111
            int upperDuration = changeDuration >> 3; // upperDuration's now between 0 and 13
            shifts = normalisationShifts + upperDuration + 2;
            ushort divisor = lowerDurationToDivisor[changeDuration & 7];
            short newPitchOffsetChangePerBigTick = (short)(((absPitchOffsetDelta & 0xFFFF0000) / divisor) >> 1); // Result now fits within 15 bits. FIXME: Check nothing's getting sign-extended incorrectly
            if (negativeDelta)
            {
                newPitchOffsetChangePerBigTick = (short)-newPitchOffsetChangePerBigTick;
            }
            pitchOffsetChangePerBigTick = newPitchOffsetChangePerBigTick;

            int currentBigTick = (int)(timeElapsed >> 8);
            int durationInBigTicks = divisor >> (12 - upperDuration);
            if (durationInBigTicks > 32767)
            {
                durationInBigTicks = 32767;
            }
            // The result of the addition may exceed 16 bits, but wrapping is fine and intended here.
            targetPitchOffsetReachedBigTick = (ushort)(currentBigTick + durationInBigTicks);
        }

        private byte Normalise(ref int val)
        {
            byte leftShifts;
            for (leftShifts = 0; leftShifts < 31; leftShifts++)
            {
                if ((val & 0x80000000) != 0)
                {
                    break;
                }
                val = val << 1;
            }
            return leftShifts;
        }

        private void UpdatePitch()
        {
            int newPitch = basePitch + currentPitchOffset;
            if (!partial.IsPCM || (partial.GetControlROMPCMStruct().len & 0x01) == 0)
            { // FIXME: Use !partial.pcmWaveEntry.unaffectedByMasterTune instead
              // FIXME: masterTune recalculation doesn't really happen here, and there are various bugs not yet emulated
              // 171 is ~half a semitone.
                newPitch += ((system_.masterTune - 64) * 171) >> 6; // PORTABILITY NOTE: Assumes arithmetic shift.
            }
            if ((partialParam.wg.pitchBenderEnabled & 1) != 0)
            {
                newPitch += part.GetPitchBend();
            }
            if (newPitch < 0)
            {
                newPitch = 0;
            }

        }

        private void NextPhase()
        {
            phase++;
            int envIndex = phase == 6 ? 4 : phase;

            targetPitchOffsetWithoutLFO = CalcTargetPitchOffsetWithoutLFO(partialParam, envIndex, partial.Poly.GetVelocity()); // pitch we'll reach at the end

            int changeDuration = partialParam.pitchEnv.time[envIndex - 1];
            changeDuration -= timeKeyfollowSubtraction;
            if (changeDuration > 0)
            {
                SetupPitchChange(targetPitchOffsetWithoutLFO, (byte)changeDuration); // changeDuration between 0 and 112 now
                UpdatePitch();
            }
            else {
                TargetPitchOffsetReached();
            }
        }

        public void Reset(Part usePart, TimbreParam.PartialParam usePartialParam)
        {
            part = usePart;
            partialParam = usePartialParam;
            patchTemp = part.GetPatchTemp();

            int key = partial.Poly.Key;
            int velocity = partial.Poly.GetVelocity();

            // FIXME: We're using a per-TVP timer instead of a system-wide one for convenience.
            timeElapsed = 0;

            basePitch = CalcBasePitch(partial, partialParam, patchTemp, key);
            currentPitchOffset = CalcTargetPitchOffsetWithoutLFO(partialParam, 0, velocity);
            targetPitchOffsetWithoutLFO = currentPitchOffset;
            phase = 0;

            if (partialParam.pitchEnv.timeKeyfollow != 0)
            {
                timeKeyfollowSubtraction = (sbyte)((key - 60) >> (5 - partialParam.pitchEnv.timeKeyfollow)); // PORTABILITY NOTE: Assumes arithmetic shift
            }
            else {
                timeKeyfollowSubtraction = 0;
            }
            lfoPitchOffset = 0;
            counter = 0;
            pitch = (ushort)basePitch;

            // These don't really need to be initialised, but it aids debugging.
            pitchOffsetChangePerBigTick = 0;
            targetPitchOffsetReachedBigTick = 0;
            shifts = 0;
        }

        private static int CalcTargetPitchOffsetWithoutLFO(TimbreParam.PartialParam partialParam, int levelIndex, int velocity)
        {
            int veloMult = CalcVeloMult(partialParam.pitchEnv.veloSensitivity, velocity);
            int targetPitchOffsetWithoutLFO = partialParam.pitchEnv.level[levelIndex] - 50;
            targetPitchOffsetWithoutLFO = (targetPitchOffsetWithoutLFO * veloMult) >> (16 - partialParam.pitchEnv.depth); // PORTABILITY NOTE: Assumes arithmetic shift
            return targetPitchOffsetWithoutLFO;
        }

        private static int CalcVeloMult(byte veloSensitivity, int velocity)
        {
            if (veloSensitivity == 0 || veloSensitivity > 3)
            {
                // Note that on CM-32L/LAPC-I veloSensitivity is never > 3, since it's clipped to 3 by the max tables.
                return 21845; // aka floor(4096 / 12 * 64), aka ~64 semitones
            }
            // When velocity is 127, the multiplier is 21845, aka ~64 semitones (regardless of veloSensitivity).
            // The lower the velocity, the lower the multiplier. The veloSensitivity determines the amount decreased per velocity value.
            // The minimum multiplier (with velocity 0, veloSensitivity 3) is 170 (~half a semitone).
            int veloMult = 32768;
            veloMult -= (127 - velocity) << (5 + veloSensitivity);
            veloMult *= 21845;
            veloMult >>= 15;
            return veloMult;
        }

        private static int CalcBasePitch(Partial partial, TimbreParam.PartialParam partialParam, MemParams.PatchTemp patchTemp, int key)
        {
            int basePitch = KeyToPitch(key);
            basePitch = (basePitch * pitchKeyfollowMult[partialParam.wg.pitchKeyfollow]) >> 13; // PORTABILITY NOTE: Assumes arithmetic shift
            basePitch += CoarseToPitch(partialParam.wg.pitchCoarse);
            basePitch += FineToPitch(partialParam.wg.pitchFine);
            // NOTE:Mok: This is done on MT-32, but not LAPC-I:
            //pitch += coarseToPitch(patchTemp.patch.keyShift + 12);
            basePitch += FineToPitch(patchTemp.Patch.FineTune);

            ControlROMPCMStruct controlROMPCMStruct = partial.GetControlROMPCMStruct();
            if (controlROMPCMStruct != null)
            {
                basePitch += (controlROMPCMStruct.pitchMSB << 8) | controlROMPCMStruct.pitchLSB;
            }
            else {
                if ((partialParam.wg.waveform & 1) == 0)
                {
                    basePitch += 37133; // This puts Middle C at around 261.64Hz (assuming no other modifications, masterTune of 64, etc.)
                }
                else {
                    // Sawtooth waves are effectively double the frequency of square waves.
                    // Thus we add 4096 less than for square waves here, which results in halving the frequency.
                    basePitch += 33037;
                }
            }
            if (basePitch < 0)
            {
                basePitch = 0;
            }
            if (basePitch > 59392)
            {
                basePitch = 59392;
            }
            return basePitch;
        }

        private static int FineToPitch(byte fine)
        {
            return (fine - 50) * 4096 / 1200; // One cent per fine offset
        }

        private static int CoarseToPitch(byte coarse)
        {
            return (coarse - 36) * 4096 / 12; // One semitone per coarse offset
        }

        private static short KeyToPitch(int key)
        {
            // We're using a table to do: return round_to_nearest_or_even((key - 60) * (4096.0 / 12.0))
            // Banker's rounding is just slightly annoying to do in C++
            int k = key;
            short pitch = (short)keyToPitchTable[Math.Abs(k - 60)];
            return (short)(key < 60 ? -pitch : pitch);
        }

        public int GetBasePitch()
        {
            return basePitch;
        }
    }
}
