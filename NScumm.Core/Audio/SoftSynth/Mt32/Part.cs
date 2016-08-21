//
//  Part.cs
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
using System.Collections.Generic;
using System.Linq;

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    // This is basically a per-partial, pre-processed combination of timbre and patch/rhythm settings
    class PatchCache
    {
        public bool playPartial;
        public bool PCMPartial;
        public int pcm;
        public char waveform;

        public int structureMix;
        public int structurePosition;
        public int structurePair;

        // The following fields are actually common to all partials in the timbre
        public bool dirty;
        public int partialCount;
        public bool sustain;
        public bool reverb;

        public TimbreParam.PartialParam srcPartial;

        // The following directly points into live sysex-addressable memory
        public TimbreParam.PartialParam partialParam;
    }

    class Part
    {
        // Direct pointer to sysex-addressable memory dedicated to this part (valid for parts 1-8, NULL for rhythm)
        private TimbreParam timbreTemp;

        // 0=Part 1, .. 7=Part 8, 8=Rhythm
        private int partNum;

        private bool holdpedal;

        private int activePartialCount;
        private PatchCache[] patchCache = new PatchCache[4];

        private List<Poly> activePolys = new List<Poly>();

        protected Synth synth;
        // Direct pointer into sysex-addressable memory
        protected MemParams.PatchTemp patchTemp;
        protected string name; // "Part 1".."Part 8", "Rhythm"
        protected byte[] currentInstr = new byte[11];
        protected byte modulation;
        protected byte expression;
        protected int pitchBend;
        protected bool nrpn;
        protected ushort rpn;
        protected ushort pitchBenderRange; // (patchTemp.patch.benderRange * 683) at the time of the last MIDI program change or MIDI data entry.

        static readonly byte[] PartialStruct = {
            0, 0, 2, 2, 1, 3,
            3, 0, 3, 0, 2, 1, 3
        };

        static readonly byte[] PartialMixStruct = {
            0, 1, 0, 1, 1, 0,
            1, 3, 3, 2, 2, 2, 2
        };

        public virtual int AbsTimbreNum
        {
            get { return (patchTemp.Patch.TimbreGroup * 64) + patchTemp.Patch.TimbreNum; }
        }

        public Synth Synth
        {
            get { return synth; }
        }

        public int ActivePartialCount
        {
            get { return activePartialCount; }
        }

        public Part(Synth synth, int usePartNum)
        {
            this.synth = synth;
            partNum = usePartNum;

            for (int i = 0; i < patchCache.Length; i++)
            {
                patchCache[i] = new PatchCache();
            }
            patchCache[0].dirty = true;
            holdpedal = false;
            patchTemp = synth.mt32ram.patchTemp[partNum];
            if (usePartNum == 8)
            {
                // Nasty hack for rhythm
                timbreTemp = null;
            }
            else {
                name = $"Part {partNum + 1}";
                timbreTemp = synth.mt32ram.timbreTemp[partNum];
            }
            currentInstr[0] = 0;
            currentInstr[10] = 0;
            modulation = 0;
            expression = 100;
            pitchBend = 0;
            activePartialCount = 0;
        }

        public virtual void RefreshTimbre(int absTimbreNum)
        {
            if (AbsTimbreNum == absTimbreNum)
            {
                Array.Copy(timbreTemp.common.name.Data, timbreTemp.common.name.Offset, currentInstr, 0, 10);
                patchCache[0].dirty = true;
            }
        }

        public virtual void SetProgram(int patchNum)
        {
            SetPatch(synth.mt32ram.patches[patchNum]);
            holdpedal = false;
            AllSoundOff();
            SetTimbre(synth.mt32ram.timbres[AbsTimbreNum].timbre);
            Refresh();
        }

        public void SetPatch(PatchParam patch)
        {
            patchTemp.Patch = patch;
        }

        public virtual void SetTimbre(TimbreParam timbre)
        {
            timbreTemp = timbre;
        }

        public byte GetExpression()
        {
            return expression;
        }

        public void AllSoundOff()
        {
            // MIDI "All sound off" (0x78) should release notes immediately regardless of the hold pedal.
            // This controller is not actually implemented by the synths, though (according to the docs and Mok) -
            // we're only using this method internally.
            foreach (Poly poly in activePolys)
            {
                poly.StartDecay();
            }
        }

        public virtual void Refresh()
        {
            BackupCacheToPartials(patchCache);
            for (int t = 0; t < 4; t++)
            {
                // Common parameters, stored redundantly
                patchCache[t].dirty = true;
                patchCache[t].reverb = patchTemp.Patch.ReverbSwitch > 0;
            }
            Array.Copy(timbreTemp.common.name.Data, timbreTemp.common.name.Offset, currentInstr, 0, 10);
            synth.NewTimbreSet(partNum, patchTemp.Patch.TimbreGroup, currentInstr);
            UpdatePitchBenderRange();
        }

        public void PartialDeactivated(Poly poly)
        {
            activePartialCount--;
            if (!poly.IsActive)
            {
                activePolys.Remove(poly);
                synth.partialManager.PolyFreed(poly);
                synth.PolyStateChanged(partNum);
            }
        }

        public Poly GetFirstActivePoly()
        {
            return activePolys.First();
        }

        public int GetActiveNonReleasingPartialCount()
        {
            int activeNonReleasingPartialCount = 0;
            foreach (var poly in activePolys)
            {
                if (poly.State != PolyState.Releasing)
                {
                    activeNonReleasingPartialCount += poly.ActivePartialCount;
                }
            }
            return activeNonReleasingPartialCount;
        }

        public MemParams.PatchTemp GetPatchTemp()
        {
            return patchTemp;
        }

        protected void UpdatePitchBenderRange()
        {
            pitchBenderRange = (ushort)(patchTemp.Patch.BenderRange * 683);
        }

        protected void BackupCacheToPartials(PatchCache[] cache)
        {
            // check if any partials are still playing with the old patch cache
            // if so then duplicate the cached data from the part to the partial so that
            // we can change the part's cache without affecting the partial.
            // We delay this until now to avoid a copy operation with every note played
            foreach (Poly poly in activePolys)
            {
                poly.BackupCacheToPartials(cache);
            }
        }

        public void ResetAllControllers()
        {
            modulation = 0;
            expression = 100;
            pitchBend = 0;
            SetHoldPedal(false);
        }

        public void SetHoldPedal(bool pressed)
        {
            if (holdpedal && !pressed)
            {
                holdpedal = false;
                StopPedalHold();
            }
            else {
                holdpedal = pressed;
            }
        }

        private void StopPedalHold()
        {
            foreach (var poly in activePolys)
            {
                poly.StopPedalHold();
            }
        }

        public void Reset()
        {
            ResetAllControllers();
            AllSoundOff();
            rpn = 0xFFFF;
        }

        public virtual void NoteOff(int midiKey)
        {
            StopNote(MidiKeyToKey(midiKey));
        }

        protected void StopNote(int key)
        {
#if MT32EMU_MONITOR_INSTRUMENTS
    synth.printDebug("%s (%s): stopping key %d", name, currentInstr, key);
#endif

            foreach (Poly poly in activePolys)
            {
                // Generally, non-sustaining instruments ignore note off. They die away eventually anyway.
                // Key 0 (only used by special cases on rhythm part) reacts to note off even if non-sustaining or pedal held.
                if (poly.Key == key && (poly.CanSustain || key == 0))
                {
                    if (poly.NoteOff(holdpedal && key != 0))
                    {
                        break;
                    }
                }
            }
        }

        public bool AbortFirstPolyPreferHeld()
        {
            if (AbortFirstPoly(PolyState.Held))
            {
                return true;
            }
            return AbortFirstPoly();
        }

        public byte GetModulation()
        {
            return modulation;
        }

        private bool AbortFirstPoly()
        {
            if (activePolys.Count == 0)
            {
                return false;
            }
            return activePolys.First().StartAbort();
        }

        public int GetPitchBend()
        {
            return pitchBend;
        }

        /// <summary>
        /// Applies key shift to a MIDI key and converts it into an internal key value in the range 12-108.
        /// </summary>
        /// <returns>The key to key.</returns>
        /// <param name="midiKey">Midi key.</param>
        private int MidiKeyToKey(int midiKey)
        {
            int key = midiKey + patchTemp.Patch.KeyShift;
            if (key < 36)
            {
                // After keyShift is applied, key < 36, so move up by octaves
                while (key < 36)
                {
                    key += 12;
                }
            }
            else if (key > 132)
            {
                // After keyShift is applied, key > 132, so move down by octaves
                while (key > 132)
                {
                    key -= 12;
                }
            }
            key -= 24;
            return key;
        }

        public virtual void NoteOn(int midiKey, int velocity)
        {
            int key = MidiKeyToKey(midiKey);
            if (patchCache[0].dirty)
            {
                CacheTimbre(patchCache, timbreTemp);
            }
            PlayPoly(patchCache, null, midiKey, key, velocity);
        }

        protected void CacheTimbre(PatchCache[] cache, TimbreParam timbre)
        {
            BackupCacheToPartials(cache);
            int partialCount = 0;
            for (int t = 0; t < 4; t++)
            {
                if (((timbre.common.partialMute >> t) & 0x1) == 1)
                {
                    cache[t].playPartial = true;
                    partialCount++;
                }
                else {
                    cache[t].playPartial = false;
                    continue;
                }

                // Calculate and cache common parameters
                cache[t].srcPartial = new TimbreParam.PartialParam(timbre.partial[t]);

                cache[t].pcm = timbre.partial[t].wg.pcmWave;

                switch (t)
                {
                    case 0:
                        cache[t].PCMPartial = (PartialStruct[timbre.common.partialStructure12] & 0x2) != 0;
                        cache[t].structureMix = PartialMixStruct[timbre.common.partialStructure12];
                        cache[t].structurePosition = 0;
                        cache[t].structurePair = 1;
                        break;
                    case 1:
                        cache[t].PCMPartial = (PartialStruct[timbre.common.partialStructure12] & 0x1) != 0;
                        cache[t].structureMix = PartialMixStruct[timbre.common.partialStructure12];
                        cache[t].structurePosition = 1;
                        cache[t].structurePair = 0;
                        break;
                    case 2:
                        cache[t].PCMPartial = (PartialStruct[timbre.common.partialStructure34] & 0x2) != 0;
                        cache[t].structureMix = PartialMixStruct[timbre.common.partialStructure34];
                        cache[t].structurePosition = 0;
                        cache[t].structurePair = 3;
                        break;
                    case 3:
                        cache[t].PCMPartial = (PartialStruct[timbre.common.partialStructure34] & 0x1) != 0;
                        cache[t].structureMix = PartialMixStruct[timbre.common.partialStructure34];
                        cache[t].structurePosition = 1;
                        cache[t].structurePair = 2;
                        break;
                }

                cache[t].partialParam = timbre.partial[t];

                cache[t].waveform = (char)timbre.partial[t].wg.waveform;
            }
            for (int t = 0; t < 4; t++)
            {
                // Common parameters, stored redundantly
                cache[t].dirty = false;
                cache[t].partialCount = partialCount;
                cache[t].sustain = (timbre.common.noSustain == 0);
            }
            //synth.printDebug("Res 1: %d 2: %d 3: %d 4: %d", cache[0].waveform, cache[1].waveform, cache[2].waveform, cache[3].waveform);

#if MT32EMU_MONITOR_INSTRUMENTS
    synth.printDebug("%s (%s): Recached timbre", name, currentInstr);
    for (int i = 0; i < 4; i++) {
        synth.printDebug(" %d: play=%s, pcm=%s (%d), wave=%d", i, cache[i].playPartial ? "YES" : "NO", cache[i].PCMPartial ? "YES" : "NO", timbre.partial[i].wg.pcmWave, timbre.partial[i].wg.waveform);
    }
#endif
        }

        public void SetModulation(int midiModulation)
        {
            modulation = (byte)midiModulation;
        }

        public void SetDataEntryMSB(byte midiDataEntryMSB)
        {
            if (nrpn)
            {
                // The last RPN-related control change was for an NRPN,
                // which the real synths don't support.
                return;
            }
            if (rpn != 0)
            {
                // The RPN has been set to something other than 0,
                // which is the only RPN that these synths support
                return;
            }
            patchTemp.Patch.BenderRange = (byte)(midiDataEntryMSB > 24 ? 24 : midiDataEntryMSB);
            UpdatePitchBenderRange();
        }

        public void SetVolume(int midiVolume)
        {
            // CONFIRMED: This calculation matches the table used in the control ROM
            patchTemp.OutputLevel = (byte)(midiVolume * 100 / 127);
            //synth.printDebug("%s (%s): Set volume to %d", name, currentInstr, midiVolume);
        }

        public void SetExpression(int midiExpression)
        {
            // CONFIRMED: This calculation matches the table used in the control ROM
            expression = (byte)(midiExpression * 100 / 127);
        }

        public void SetNRPN()
        {
            nrpn = true;
        }

        public virtual void SetPan(int midiPan)
        {
            // NOTE: Panning is inverted compared to GM.

            // CM-32L: Divide by 8.5
            patchTemp.Panpot = (byte)((midiPan << 3) / 68);
            // FIXME: MT-32: Divide by 9
            //patchTemp.panpot = (Bit8u)(midiPan / 9);

            //synth.printDebug("%s (%s): Set pan to %d", name, currentInstr, panpot);
        }

        public void SetRPNLSB(byte midiRPNLSB)
        {
            nrpn = false;
            rpn = (ushort)((rpn & 0xFF00) | midiRPNLSB);
        }

        public void SetRPNMSB(byte midiRPNMSB)
        {
            nrpn = false;
            rpn = (ushort)((rpn & 0x00FF) | (midiRPNMSB << 8));
        }

        public void AllNotesOff()
        {
            // The MIDI specification states - and Mok confirms - that all notes off (0x7B)
            // should treat the hold pedal as usual.
            foreach (Poly poly in activePolys)
            {
                // FIXME: This has special handling of key 0 in NoteOff that Mok has not yet confirmed applies to AllNotesOff.
                // if (poly.canSustain() || poly.getKey() == 0) {
                // FIXME: The real devices are found to be ignoring non-sustaining polys while processing AllNotesOff. Need to be confirmed.
                if (poly.CanSustain)
                {
                    poly.NoteOff(holdpedal);
                }
            }
        }

        public void SetBend(int midiBend)
        {
            // CONFIRMED:
            pitchBend = ((midiBend - 8192) * pitchBenderRange) >> 14; // PORTABILITY NOTE: Assumes arithmetic shift
        }

        protected void PlayPoly(PatchCache[] cache, MemParams.RhythmTemp rhythmTemp, int midiKey, int key, int velocity)
        {
            // CONFIRMED: Even in single-assign mode, we don't abort playing polys if the timbre to play is completely muted.
            int needPartials = cache[0].partialCount;
            if (needPartials == 0)
            {
                synth.PrintDebug("{0} ({1}): Completely muted instrument", name, currentInstr.GetRawText());
                return;
            }

            if ((patchTemp.Patch.AssignMode & 2) == 0)
            {
                // Single-assign mode
                AbortFirstPoly(key);
                if (synth.IsAbortingPoly) return;
            }

            if (!synth.partialManager.FreePartials(needPartials, partNum))
            {
#if MT32EMU_MONITOR_PARTIALS
                synth.PrintDebug("%s (%s): Insufficient free partials to play key %d (velocity %d); needed=%d, free=%d, assignMode=%d", name, currentInstr, midiKey, velocity, needPartials, synth.partialManager.getFreePartialCount(), patchTemp.patch.assignMode);
                synth.printPartialUsage();
#endif
                return;
            }
            if (synth.IsAbortingPoly) return;

            Poly poly = synth.partialManager.AssignPolyToPart(this);
            if (poly == null)
            {
                synth.PrintDebug("{0} ({1}): No free poly to play key {2} (velocity {3})", name, currentInstr.GetRawText(), midiKey, velocity);
                return;
            }
            if ((patchTemp.Patch.AssignMode & 1) != 0)
            {
                // Priority to data first received
                activePolys.Insert(0, poly);
            }
            else {
                activePolys.Add(poly);
            }

            Partial[] partials = new Partial[4];
            for (int x = 0; x < 4; x++)
            {
                if (cache[x].playPartial)
                {
                    partials[x] = synth.partialManager.AllocPartial(partNum);
                    activePartialCount++;
                }
                else {
                    partials[x] = null;
                }
            }
            poly.Reset(key, velocity, cache[0].sustain, partials);

            for (int x = 0; x < 4; x++)
            {
                if (partials[x] != null)
                {
                    partials[x].StartPartial(this, poly, cache[x], rhythmTemp, partials[cache[x].structurePair]);
                }
            }
            synth.PolyStateChanged(partNum);
        }

        private bool AbortFirstPoly(int key)
        {
            foreach (Poly poly in activePolys)
            {
                if (poly.Key == key)
                {
                    return poly.StartAbort();
                }
            }
            return false;
        }

        public bool AbortFirstPoly(PolyState state)
        {
            foreach (Poly poly in activePolys)
            {
                if (poly.State == state)
                {
                    return poly.StartAbort();
                }
            }
            return false;
        }
    }
}

