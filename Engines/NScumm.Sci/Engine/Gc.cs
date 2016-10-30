﻿//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 3 of the License; or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful;
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not; see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;
using System.Linq;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    internal class AddrSet : HashSet<Register>
    {
    }

    internal class WorklistManager
    {
        public List<Register> _worklist;
        public AddrSet _map; // used for 2 contains() calls, inside push() and run_gc()

        public WorklistManager()
        {
            _worklist = new List<Register>();
            _map = new AddrSet();
        }

        public void Push(Register reg)
        {
            if (reg.Segment == 0) // No numbers
                return;

            DebugC(DebugLevels.GC, "[GC] Adding {0}", reg);

            if (_map.Contains(reg))
                return; // already dealt with it

            _map.Add(reg);
            _worklist.Add(reg);
        }

        public void Push(IEnumerable<Register> tmp)
        {
            foreach (var item in tmp)
            {
                Push(item);
            }
        }
    }

    internal static class Gc
    {
        /// <summary>
        /// Runs garbage collection on the current system state
        /// </summary>
        /// <param name="s">The state in which we should gc</param>
        public static void Run(EngineState s)
        {
            SegManager segMan = s._segMan;

            // Some debug stuff
            DebugC(DebugLevels.GC, "[GC] Running...");
# if GC_DEBUG_CODE
            const char* segnames[SEG_TYPE_MAX + 1];
            int segcount[SEG_TYPE_MAX + 1];
            memset(segnames, 0, sizeof(segnames));
            memset(segcount, 0, sizeof(segcount));
#endif

            // Compute the set of all segments references currently in use.
            AddrSet activeRefs = FindAllActiveReferences(s);

            // Iterate over all segments, and check for each whether it
            // contains stuff that can be collected.
            var heap = segMan.Segments;
            for (var seg = 1; seg < heap.Count; seg++)
            {
                SegmentObj mobj = heap[seg];

                if (mobj != null)
                {
# if GC_DEBUG_CODE
                    const SegmentType type = mobj.getType();
                    segnames[type] = segmentTypeNames[type];
#endif

                    // Get a list of all deallocatable objects in this segment,
                    // then free any which are not referenced from somewhere.
                    var tmp = mobj.ListAllDeallocatable((ushort) seg);
                    foreach (var addr in tmp)
                    {
                        if (!activeRefs.Contains(addr))
                        {
                            // Not found . we can free it
                            mobj.FreeAtAddress(segMan, addr);
                            DebugC(DebugLevels.GC, "[GC] Deallocating {0}", addr);
# if GC_DEBUG_CODE
                            segcount[type]++;
#endif
                        }
                    }
                }
            }

# if GC_DEBUG_CODE
// Output debug summary of garbage collection
            DebugC(DebugLevels.GC, "[GC] Summary:");
            for (int i = 0; i <= SEG_TYPE_MAX; i++)
                if (segcount[i])
            DebugC(DebugLevels.GC, "\t{0}\t* {1}", segcount[i], segnames[i]);
#endif
        }

        private static AddrSet FindAllActiveReferences(EngineState s)
        {
            //assert(!s._executionStack.empty());

            var wm = new WorklistManager();

            // Initialize registers
            wm.Push(s.r_acc);
            wm.Push(s.r_prev);

            // Initialize value stack
            // We do this one by hand since the stack doesn't know the current execution stack
            // Skip fake kernel stack frame if it's on top

            var iter = s._executionStack.Last(o => o.type != ExecStackType.KERNEL);

            // TODO: assert((iter != s._executionStack.end()) && ((*iter).type != EXEC_STACK_TYPE_KERNEL));

            StackPtr sp = iter.sp;

            for (var pos = s.stack_base; pos < sp; pos++)
                wm.Push(pos[0]);

            DebugC(DebugLevels.GC, "[GC] -- Finished adding value stack");

            // Init: Execution Stack
            foreach (ExecStack es in s._executionStack)
            {
                if (es.type != ExecStackType.KERNEL)
                {
                    wm.Push(es.objp); // this is important to make a copy of the register
                    wm.Push(es.sendp);
                    if (es.type == ExecStackType.VARSELECTOR)
                        wm.Push(es.GetVarPointer(s._segMan)[0]);
                }
            }

            DebugC(DebugLevels.GC, "[GC] -- Finished adding execution stack");

            var heap = s._segMan.Segments;
            int heapSize = heap.Count;

            for (var i = 1; i < heapSize; i++)
            {
                // Init: Explicitly loaded scripts
                if (heap[i] != null)
                {
                    if (heap[i].Type == SegmentType.SCRIPT)
                    {
                        Script script = (Script) heap[i];

                        if (script.Lockers != 0)
                        {
                            // Explicitly loaded?
                            wm.Push(script.ListObjectReferences());
                        }
                    }
#if ENABLE_SCI32
                    // Init: Explicitly opted-out bitmaps
                    else if (heap[i].Type == SegmentType.BITMAP)
                    {
                        var bt = (BitmapTable) heap[i];
                        for (var j = 0; j < bt._table.Length; j++)
                        {
                            if (bt._table[j].Item != null && !bt._table[j].Item.ShouldGc)
                            {
                                wm.Push(Register.Make((ushort) i, (ushort) j));
                            }
                        }
                    }
#endif
                }
            }

            DebugC(DebugLevels.GC, "[GC] -- Finished explicitly loaded scripts, done with root set");

            ProcessWorkList(s._segMan, wm, heap);

            if (SciEngine.Instance._gfxPorts != null)
                SciEngine.Instance._gfxPorts.ProcessEngineHunkList(wm);

            return NormalizeAddresses(s._segMan, wm._map);
        }

        private static AddrSet NormalizeAddresses(SegManager segMan, AddrSet nonnormalMap)
        {
            AddrSet normalMap = new AddrSet();

            foreach (var reg in nonnormalMap)
            {
                SegmentObj mobj = segMan.GetSegmentObj(reg.Segment);
                if (mobj == null) continue;

                var reg2 = mobj.FindCanonicAddress(segMan, reg);
                normalMap.Add(reg2);
            }

            return normalMap;
        }

        private static void ProcessWorkList(SegManager segMan, WorklistManager wm, List<SegmentObj> heap)
        {
            var stackSegment = segMan.FindSegmentByType(SegmentType.STACK);
            while (wm._worklist.Count != 0)
            {
                var reg = wm._worklist.Last();
                wm._worklist.Remove(reg);
                if (reg.Segment != stackSegment)
                {
                    // No need to repeat this one
                    DebugC(DebugLevels.GC, "[GC] Checking {0}", reg);
                    if (reg.Segment < heap.Count && heap[reg.Segment] != null)
                    {
                        // Valid heap object? Find its outgoing references!
                        wm.Push(heap[reg.Segment].ListAllOutgoingReferences(reg));
                    }
                }
            }
        }
    }
}