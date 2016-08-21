//
//  Poly.cs
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
    enum PolyState
    {
        Playing,
        Held, // This marks keys that have been released on the keyboard, but are being held by the pedal
        Releasing,
        Inactive
    }

    class Poly
    {
        Part part;
        int key;
        int velocity;
        int activePartialCount;
        bool sustain;

        PolyState state;

        Partial[] partials = new Partial[4];

        Poly next;

        /**
         * Returns the internal key identifier.
         * For non-rhythm, this is within the range 12 to 108.
         * For rhythm on MT-32, this is 0 or 1 (special cases) or within the range 24 to 87.
         * For rhythm on devices with extended PCM sounds (e.g. CM-32L), this is 0, 1 or 24 to 108
         */
        public int Key
        {
            get
            {
                return key;
            }
        }

        public int ActivePartialCount
        {
            get
            {
                return activePartialCount;
            }
        }

        public PolyState State
        {
            get
            {
                return state;
            }
        }

        public bool CanSustain
        {
            get
            {
                return sustain;
            }
        }

        public bool IsActive
        {
            get
            {
                return state != PolyState.Inactive;
            }
        }

        public int GetVelocity()
        {
            return velocity;
        }

        public Poly()
        {
            key = 255;
            velocity = 255;
            state = PolyState.Inactive;
        }

        public void SetPart(Part usePart)
        {
            part = usePart;
        }

        public bool NoteOff(bool pedalHeld)
        {
            // Generally, non-sustaining instruments ignore note off. They die away eventually anyway.
            // Key 0 (only used by special cases on rhythm part) reacts to note off even if non-sustaining or pedal held.
            if (state == PolyState.Inactive || state == PolyState.Releasing)
            {
                return false;
            }
            if (pedalHeld)
            {
                if (state == PolyState.Held)
                {
                    return false;
                }
                state = PolyState.Held;
            }
            else {
                StartDecay();
            }
            return true;
        }

        public void Reset(int newKey, int newVelocity, bool newSustain, Partial[] newPartials)
        {
            if (IsActive)
            {
                // This should never happen
                //part.Synth.PrintDebug("Resetting active poly. Active partial count: %i\n", activePartialCount);
                for (int i = 0; i < 4; i++)
                {
                    if (partials[i] != null && partials[i].IsActive)
                    {
                        partials[i].Deactivate();
                        activePartialCount--;
                    }
                }
                state = PolyState.Inactive;
            }

            key = newKey;
            velocity = newVelocity;
            sustain = newSustain;

            activePartialCount = 0;
            for (int i = 0; i < 4; i++)
            {
                partials[i] = newPartials[i];
                if (newPartials[i] != null)
                {
                    activePartialCount++;
                    state = PolyState.Playing;
                }
            }
        }

        public bool StartDecay()
        {
            if (state == PolyState.Inactive || state == PolyState.Releasing)
            {
                return false;
            }
            state = PolyState.Releasing;

            for (int t = 0; t < 4; t++)
            {
                Partial partial = partials[t];
                if (partial != null)
                {
                    partial.StartDecayAll();
                }
            }
            return true;
        }

        public void BackupCacheToPartials(PatchCache[] cache)
        {
            for (int partialNum = 0; partialNum < 4; partialNum++)
            {
                Partial partial = partials[partialNum];
                if (partial != null)
                {
                    partial.BackupCache(cache[partialNum]);
                }
            }
        }

        public bool StopPedalHold()
        {
            if (state != PolyState.Held)
            {
                return false;
            }
            return StartDecay();
        }

        public Poly GetNext()
        {
            return next;
        }

        // This is called by Partial to inform the poly that the Partial has deactivated
        public void PartialDeactivated(Partial partial)
        {
            for (int i = 0; i < 4; i++)
            {
                if (partials[i] == partial)
                {
                    partials[i] = null;
                    activePartialCount--;
                }
            }
            if (activePartialCount == 0)
            {
                state = PolyState.Inactive;
                if (part.Synth.abortingPoly == this)
                {
                    part.Synth.abortingPoly = null;
                }
            }
            part.PartialDeactivated(this);
        }

        public bool StartAbort()
        {
            if (state == PolyState.Inactive || part.Synth.IsAbortingPoly)
            {
                return false;
            }
            for (int t = 0; t < 4; t++)
            {
                Partial partial = partials[t];
                if (partial != null)
                {
                    partial.StartAbort();
                    part.Synth.abortingPoly = this;
                }
            }
            return true;
        }

   }
}