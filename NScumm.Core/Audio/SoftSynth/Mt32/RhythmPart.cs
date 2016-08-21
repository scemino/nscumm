//
//  RhythmPart.cs
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

namespace NScumm.Core.Audio.SoftSynth.Mt32
{
    class RhythmPart : Part
    {
        // Pointer to the area of the MT-32's memory dedicated to rhythm
        readonly MemParams.RhythmTemp[] rhythmTemp;

        // This caches the timbres/settings in use by the rhythm part
        readonly PatchCache[][] drumCache = new PatchCache[85][];

        public RhythmPart(Synth synth, int usePartNum) : base(synth, usePartNum)
        {
            for (int i = 0; i < 85; i++)
            {
                drumCache[i] = new PatchCache[4];
                for (int j = 0; j < 4; j++)
                {
                    drumCache[i][j] = new PatchCache();
                }
            }
            name = "Rhythm";
            rhythmTemp = synth.mt32ram.rhythmTemp;
            Refresh();
        }

        public override void RefreshTimbre(int absTimbreNum)
        {
            for (int m = 0; m < 85; m++)
            {
                if (rhythmTemp[m].timbre == absTimbreNum - 128)
                {
                    drumCache[m][0].dirty = true;
                }
            }
        }

        public override void SetProgram(int patchNum)
        {
        }

        public override void Refresh()
        {
            // (Re-)cache all the mapped timbres ahead of time
            for (int drumNum = 0; drumNum < synth.controlROMMap.rhythmSettingsCount; drumNum++)
            {
                int drumTimbreNum = rhythmTemp[drumNum].timbre;
                if (drumTimbreNum >= 127)
                { // 94 on MT-32
                    continue;
                }
                PatchCache[] cache = drumCache[drumNum];
                BackupCacheToPartials(cache);
                for (int t = 0; t < 4; t++)
                {
                    // Common parameters, stored redundantly
                    cache[t].dirty = true;
                    cache[t].reverb = rhythmTemp[drumNum].ReverbSwitch > 0;
                }
            }
            UpdatePitchBenderRange();
        }

        public override void SetTimbre(TimbreParam timbre)
        {
            //synth->printDebug("%s: Attempted to call setTimbre() - doesn't make sense for rhythm", name);
        }

        public override int AbsTimbreNum
        {
            get
            {
                //synth->printDebug("%s: Attempted to call getAbsTimbreNum() - doesn't make sense for rhythm", name);
                return 0;
            }
        }

        public override void SetPan(int midiPan)
        {
            // CONFIRMED: This does change patchTemp, but has no actual effect on playback.
#if MT32EMU_MONITOR_MIDI
    synth->printDebug("%s: Pointlessly setting pan (%d) on rhythm part", name, midiPan);
#endif
            base.SetPan(midiPan);
        }

        public override void NoteOn(int midiKey, int velocity)
        {
            if (midiKey < 24 || midiKey > 108)
            { /*> 87 on MT-32)*/
                //synth->printDebug("%s: Attempted to play invalid key %d (velocity %d)", name, midiKey, velocity);
                return;
            }
            int key = midiKey;
            int drumNum = key - 24;
            int drumTimbreNum = rhythmTemp[drumNum].timbre;
            if (drumTimbreNum >= 127)
            { // 94 on MT-32
                //synth->printDebug("%s: Attempted to play unmapped key %d (velocity %d)", name, midiKey, velocity);
                return;
            }
            // CONFIRMED: Two special cases described by Mok
            if (drumTimbreNum == 64 + 6)
            {
                NoteOff(0);
                key = 1;
            }
            else if (drumTimbreNum == 64 + 7)
            {
                // This noteOff(0) is not performed on MT-32, only LAPC-I
                NoteOff(0);
                key = 0;
            }
            int absTimbreNum = drumTimbreNum + 128;
            TimbreParam timbre = synth.mt32ram.timbres[absTimbreNum].timbre;
            Array.Copy(timbre.common.name.Data, timbre.common.name.Offset, currentInstr, 0, 10);
            if (drumCache[drumNum][0].dirty)
            {
                CacheTimbre(drumCache[drumNum], timbre);
            }
            PlayPoly(drumCache[drumNum], rhythmTemp[drumNum], midiKey, key, velocity);
        }

        public override void NoteOff(int midiKey)
        {
            StopNote(midiKey);
        }
    }
}