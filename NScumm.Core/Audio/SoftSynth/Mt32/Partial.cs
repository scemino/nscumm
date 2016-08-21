//
//  Partial.cs
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

#if MT32EMU_USE_FLOAT_SAMPLES
using Sample = System.Single;
using SampleEx = System.Single;
#else
using Sample = System.Int16;
using SampleEx = System.Int32;
#endif

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    class Partial
    {
        static readonly byte[] PAN_NUMERATOR_MASTER = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 5, 6, 7 };
        static readonly byte[] PAN_NUMERATOR_SLAVE = { 0, 1, 2, 3, 4, 5, 6, 7, 7, 7, 7, 7, 7, 7, 7 };

        static readonly int[] PAN_FACTORS = { 0, 18, 37, 55, 73, 91, 110, 128, 146, 165, 183, 201, 219, 238, 256 };


        private Synth synth;
        private readonly int debugPartialNum; // Only used for debugging
                                              // Number of the sample currently being rendered by produceOutput(), or 0 if no run is in progress
                                              // This is only kept available for debugging purposes.
        private long sampleNum;

        // Actually, this is a 4-bit register but we abuse this to emulate inverted mixing.
        // Also we double the value to enable INACCURATE_SMOOTH_PAN, with respect to MoK.
        private int leftPanValue, rightPanValue;

        private int ownerPart; // -1 if unassigned
        private int mixType;
        private int structurePosition; // 0 or 1 of a structure pair

        // Only used for PCM partials
        private int pcmNum;
        // FIXME: Give this a better name (e.g. pcmWaveInfo)
        private PCMWaveEntry pcmWave;

        // Final pulse width value, with velfollow applied, matching what is sent to the LA32.
        // Range: 0-255
        private int pulseWidthVal;

        private Poly poly;
        private Partial pair;

        private TVA tva;
        private TVP tvp;
        private TVF tvf;

        private LA32Ramp ampRamp = new LA32Ramp();
        private LA32Ramp cutoffModifierRamp = new LA32Ramp();

        // TODO: This should be owned by PartialPair
        private LA32PartialPair la32Pair = new LA32PartialPair();

        private PatchCache patchCache;
        private PatchCache cachebackup;

        public bool alreadyOutputed;

        public bool IsActive
        {
            get
            {
                return ownerPart > -1;
            }
        }

        public bool IsRingModulatingSlave
        {
            get
            {
                return pair != null && structurePosition == 1 && (mixType == 1 || mixType == 2);
            }
        }

        public bool IsPCM { get { return pcmWave != null; } }

        public bool HasRingModulatingSlave
        {
            get
            {
                return pair != null && structurePosition == 0 && (mixType == 1 || mixType == 2);
            }
        }

        public Synth Synth
        {
            get { return synth; }
        }

        public Poly Poly
        {
            get { return poly; }
        }

        public Partial(Synth useSynth, int useDebugPartialNum)
        {
            synth = useSynth;
            debugPartialNum = useDebugPartialNum;
            // Initialisation of tva, tvp and tvf uses 'this' pointer
            // and thus should not be in the initializer list to avoid a compiler warning
            tva = new TVA(this, ampRamp);
            tvp = new TVP(this);
            tvf = new TVF(this, cutoffModifierRamp);
            ownerPart = -1;
        }

        public void Activate(int partNum)
        {
            // This just marks the partial as being assigned to a part
            ownerPart = partNum;
        }

        public bool ShouldReverb()
        {
            if (!IsActive)
            {
                return false;
            }
            return patchCache.reverb;
        }

        public void StartDecayAll()
        {
            tva.StartDecay();
            tvp.StartDecay();
            tvf.StartDecay();
        }

        public bool ProduceOutput(Ptr<Sample> leftBuf, Ptr<Sample> rightBuf, int length)
        {
            if (!IsActive || alreadyOutputed || IsRingModulatingSlave)
            {
                return false;
            }
            if (poly == null)
            {
                //synth.printDebug("[Partial %d] *** ERROR: poly is NULL at Partial::produceOutput()!", debugPartialNum);
                return false;
            }
            alreadyOutputed = true;

            var l = 0;
            var r = 0;
            for (sampleNum = 0; sampleNum < length; sampleNum++)
            {
                if (!tva.IsPlaying || !la32Pair.IsActive(LA32PartialPairType.MASTER))
                {
                    Deactivate();
                    break;
                }
                la32Pair.GenerateNextSample(LA32PartialPairType.MASTER, GetAmpValue(), tvp.NextPitch(), GetCutoffValue());
                if (HasRingModulatingSlave)
                {
                    la32Pair.GenerateNextSample(LA32PartialPairType.SLAVE, pair.GetAmpValue(), pair.tvp.NextPitch(), pair.GetCutoffValue());
                    if (!pair.tva.IsPlaying || !la32Pair.IsActive(LA32PartialPairType.SLAVE))
                    {
                        pair.Deactivate();
                        if (mixType == 2)
                        {
                            Deactivate();
                            break;
                        }
                    }
                }

                // Although, LA32 applies panning itself, we assume here it is applied in the mixer, not within a pair.
                // Applying the pan value in the log-space looks like a waste of unlog resources. Though, it needs clarification.
                Sample sample = (Sample)la32Pair.NextOutSample();

                // FIXME: Sample analysis suggests that the use of panVal is linear, but there are some quirks that still need to be resolved.
#if MT32EMU_USE_FLOAT_SAMPLES
                Sample leftOut = (sample * (float)leftPanValue) / 14.0f;
                Sample rightOut = (sample * (float)rightPanValue) / 14.0f;
                *(leftBuf++) += leftOut;
                *(rightBuf++) += rightOut;
#else
                // FIXME: Dividing by 7 (or by 14 in a Mok-friendly way) looks of course pointless. Need clarification.
                // FIXME2: LA32 may produce distorted sound in case if the absolute value of maximal amplitude of the input exceeds 8191
                // when the panning value is non-zero. Most probably the distortion occurs in the same way it does with ring modulation,
                // and it seems to be caused by limited precision of the common multiplication circuit.
                // From analysis of this overflow, it is obvious that the right channel output is actually found
                // by subtraction of the left channel output from the input.
                // Though, it is unknown whether this overflow is exploited somewhere.
                Sample leftOut = (Sample)((sample * leftPanValue) >> 8);
                Sample rightOut = (Sample)((sample * rightPanValue) >> 8);
                leftBuf[l] = Synth.ClipSampleEx(leftBuf[l] + (SampleEx)leftOut);
                rightBuf[r] = Synth.ClipSampleEx(rightBuf[r] + (SampleEx)rightOut);
                l++;
                r++;
#endif
            }

            sampleNum = 0;
            return true;
        }

        public ControlROMPCMStruct GetControlROMPCMStruct()
        {
            if (pcmWave != null)
            {
                return pcmWave.controlROMPCMStruct;
            }
            return null;
        }

        private int GetAmpValue()
        {
            // SEMI-CONFIRMED: From sample analysis:
            // (1) Tested with a single partial playing PCM wave 77 with pitchCoarse 36 and no keyfollow, velocity follow, etc.
            // This gives results within +/- 2 at the output (before any DAC bitshifting)
            // when sustaining at levels 156 - 255 with no modifiers.
            // (2) Tested with a special square wave partial (internal capture ID tva5) at TVA envelope levels 155-255.
            // This gives deltas between -1 and 0 compared to the real output. Note that this special partial only produces
            // positive amps, so negative still needs to be explored, as well as lower levels.
            //
            // Also still partially unconfirmed is the behaviour when ramping between levels, as well as the timing.
            // TODO: The tests above were performed using the float model, to be refined
            int ampRampVal = 67117056 - ampRamp.NextValue();
            if (ampRamp.CheckInterrupt())
            {
                tva.HandleInterrupt();
            }
            return ampRampVal;
        }


        public void StartAbort()
        {
            // This is called when the partial manager needs to terminate partials for re-use by a new Poly.
            tva.StartAbort();
        }

        private int GetCutoffValue()
        {
            if (IsPCM)
            {
                return 0;
            }
            int cutoffModifierRampVal = cutoffModifierRamp.NextValue();
            if (cutoffModifierRamp.CheckInterrupt())
            {
                tvf.HandleInterrupt();
            }
            return (tvf.GetBaseCutoff() << 18) + cutoffModifierRampVal;
        }

        public void Deactivate()
        {
            if (!IsActive)
            {
                return;
            }
            ownerPart = -1;
            if (poly != null)
            {
                poly.PartialDeactivated(this);
            }
#if MT32EMU_MONITOR_PARTIALS
    synth.printDebug("[+%lu] [Partial %d] Deactivated", sampleNum, debugPartialNum);
    synth.printPartialUsage(sampleNum);
#endif
            if (IsRingModulatingSlave)
            {
                pair.la32Pair.Deactivate(LA32PartialPairType.SLAVE);
            }
            else
            {
                la32Pair.Deactivate(LA32PartialPairType.MASTER);
                if (HasRingModulatingSlave)
                {
                    pair.Deactivate();
                    pair = null;
                }
            }
            if (pair != null)
            {
                pair.pair = null;
            }
        }

        public void StartPartial(Part part, Poly usePoly, PatchCache usePatchCache, MemParams.RhythmTemp rhythmTemp, Partial pairPartial)
        {
            if (usePoly == null || usePatchCache == null)
            {
                //synth.printDebug("[Partial %d] *** Error: Starting partial for owner %d, usePoly=%s, usePatchCache=%s", debugPartialNum, ownerPart, usePoly == NULL ? "*** NULL ***" : "OK", usePatchCache == NULL ? "*** NULL ***" : "OK");
                return;
            }
            patchCache = usePatchCache;
            poly = usePoly;
            mixType = patchCache.structureMix;
            structurePosition = patchCache.structurePosition;

            byte panSetting = rhythmTemp != null ? rhythmTemp.Panpot : part.GetPatchTemp().Panpot;
            if (mixType == 3)
            {
                if (structurePosition == 0)
                {
                    panSetting = (byte)(PAN_NUMERATOR_MASTER[panSetting] << 1);
                }
                else {
                    panSetting = (byte)(PAN_NUMERATOR_SLAVE[panSetting] << 1);
                }
                // Do a normal mix independent of any pair partial.
                mixType = 0;
                pairPartial = null;
            }
            else {
                // Mok wanted an option for smoother panning, and we love Mok.
# if !INACCURATE_SMOOTH_PAN
                // CONFIRMED by Mok: exactly bytes like this (right shifted?) are sent to the LA32.
                panSetting &= 0x0E;
#endif
            }

            leftPanValue = synth.IsReversedStereoEnabled ? 14 - panSetting : panSetting;
            rightPanValue = 14 - leftPanValue;

#if !MT32EMU_USE_FLOAT_SAMPLES
            leftPanValue = PAN_FACTORS[leftPanValue];
            rightPanValue = PAN_FACTORS[rightPanValue];
#endif

            // SEMI-CONFIRMED: From sample analysis:
            // Found that timbres with 3 or 4 partials (i.e. one using two partial pairs) are mixed in two different ways.
            // Either partial pairs are added or subtracted, it depends on how the partial pairs are allocated.
            // It seems that partials are grouped into quarters and if the partial pairs are allocated in different quarters the subtraction happens.
            // Though, this matters little for the majority of timbres, it becomes crucial for timbres which contain several partials that sound very close.
            // In this case that timbre can sound totally different depending of the way it is mixed up.
            // Most easily this effect can be displayed with the help of a special timbre consisting of several identical square wave partials (3 or 4).
            // Say, it is 3-partial timbre. Just play any two notes simultaneously and the polys very probably are mixed differently.
            // Moreover, the partial allocator retains the last partial assignment it did and all the subsequent notes will sound the same as the last released one.
            // The situation is better with 4-partial timbres since then a whole quarter is assigned for each poly. However, if a 3-partial timbre broke the normal
            // whole-quarter assignment or after some partials got aborted, even 4-partial timbres can be found sounding differently.
            // This behaviour is also confirmed with two more special timbres: one with identical sawtooth partials, and one with PCM wave 02.
            // For my personal taste, this behaviour rather enriches the sounding and should be emulated.
            // Also, the current partial allocator model probably needs to be refined.
            if ((debugPartialNum & 8) != 0)
            {
                leftPanValue = -leftPanValue;
                rightPanValue = -rightPanValue;
            }

            if (patchCache.PCMPartial)
            {
                pcmNum = patchCache.pcm;
                if (synth.controlROMMap.pcmCount > 128)
                {
                    // CM-32L, etc. support two "banks" of PCMs, selectable by waveform type parameter.
                    if (patchCache.waveform > 1)
                    {
                        pcmNum += 128;
                    }
                }
                pcmWave = synth.pcmWaves[pcmNum];
            }
            else {
                pcmWave = null;
            }

            // CONFIRMED: pulseWidthVal calculation is based on information from Mok
            pulseWidthVal = (poly.GetVelocity() - 64) * (patchCache.srcPartial.wg.pulseWidthVeloSensitivity - 7) + Tables.Instance.pulseWidth100To255[patchCache.srcPartial.wg.pulseWidth];
            if (pulseWidthVal < 0)
            {
                pulseWidthVal = 0;
            }
            else if (pulseWidthVal > 255)
            {
                pulseWidthVal = 255;
            }

            pair = pairPartial;
            alreadyOutputed = false;
            tva.Reset(part, patchCache.partialParam, rhythmTemp);
            tvp.Reset(part, patchCache.partialParam);
            tvf.Reset(patchCache.partialParam, tvp.GetBasePitch());

            LA32PartialPairType pairType;
            LA32PartialPair useLA32Pair;
            if (IsRingModulatingSlave)
            {
                pairType = LA32PartialPairType.SLAVE;
                useLA32Pair = pair.la32Pair;
            }
            else {
                pairType = LA32PartialPairType.MASTER;
                la32Pair.Init(HasRingModulatingSlave, mixType == 1);
                useLA32Pair = la32Pair;
            }
            if (IsPCM)
            {
                useLA32Pair.InitPCM(pairType, new Ptr<short>(synth.pcmROMData,pcmWave.addr), pcmWave.len, pcmWave.loop);
            }
            else {
                useLA32Pair.InitSynth(pairType, (patchCache.waveform & 1) != 0, (byte)pulseWidthVal, (byte)(patchCache.srcPartial.tvf.resonance + 1));
            }
            if (!HasRingModulatingSlave)
            {
                la32Pair.Deactivate(LA32PartialPairType.SLAVE);
            }
        }

        public void BackupCache(PatchCache cache)
        {
            if (patchCache == cache)
            {
                cachebackup = cache;
                patchCache = cachebackup;
            }
        }
    }
}
