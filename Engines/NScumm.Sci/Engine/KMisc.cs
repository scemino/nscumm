//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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


using NScumm.Core;
using System;
using static NScumm.Core.DebugHelper;
using NScumm.Core.Graphics;
using System.Collections.Generic;
using System.Linq;

namespace NScumm.Sci.Engine
{
    enum kMemoryInfoFunc
    {
        LARGEST_HEAP_BLOCK = 0, // Largest heap block available
        FREE_HEAP = 1, // Total free heap memory
        LARGEST_HUNK_BLOCK = 2, // Largest available hunk memory block
        FREE_HUNK = 3, // Amount of free DOS paragraphs
        TOTAL_HUNK = 4 // Total amount of hunk memory (SCI01)
    }

    enum GetTimeMode
    {
        TICKS = 0,
        TIME_12HOUR = 1,
        TIME_24HOUR = 2,
        DATE = 3
    }

    enum MemoryFunction
    {
        ALLOCATE_CRITICAL = 1,
        ALLOCATE_NONCRITICAL = 2,
        FREE = 3,
        MEMCPY = 4,
        PEEK = 5,
        POKE = 6
    }

    enum MemorySegmentFunction
    {
        SAVE_DATA = 0,
        RESTORE_DATA = 1
    }

    enum PlatformOps
    {
        Unk0 = 0,
        CDSpeed = 1,
        Unk2 = 2,
        CDCheck = 3,
        GetPlatform = 4,
        Unk5 = 5,
        IsHiRes = 6,
        IsItWindows = 7
    }

    partial class Kernel
    {
        private const int SciPlatformDOS = 1;
        private const int SciPlatformWindows = 2;
        private const string AVOIDPATH_DYNMEM_STRING = "AvoidPath polyline";

        private static Register kEmpty(EngineState s, int argc, StackPtr argv)
        {
            // Placeholder for empty kernel functions which are still called from the
            // engine scripts (like the empty kSetSynonyms function in SCI1.1). This
            // differs from dummy functions because it does nothing and never throws a
            // warning when it is called.
            return s.r_acc;
        }

        /// <summary>
        /// kGameIsRestarting():
        /// Returns the restarting_flag in acc
        /// </summary>
        /// <param name="s"></param>
        /// <param name="argc"></param>
        /// <param name="argv"></param>
        /// <returns></returns>
        private static Register kGameIsRestarting(EngineState s, int argc, StackPtr argv)
        {
            s.r_acc = Register.Make(0, (ushort)s.gameIsRestarting);

            if (argc != 0)
            { // Only happens during replay
                if (argv[0].ToUInt16() == 0) // Set restarting flag
                    s.gameIsRestarting = GameIsRestarting.NONE;
            }

            int neededSleep = 30;

            // WORKAROUNDS for scripts that are polling too quickly in scenes that
            // are not animating much
            switch (SciEngine.Instance.GameId)
            {
                case SciGameId.CASTLEBRAIN:
                    // In Castle of Dr. Brain, memory color matching puzzle in the first
                    // room (room 100), the game scripts constantly poll the state of each
                    // stone when the user clicks on one. Since the scene is not animating
                    // much, this results in activating and deactivating each stone very
                    // quickly (together with its associated tone sound), depending on how
                    // low it is in the animate list. This worked somewhat in older PCs, but
                    // not in modern computers. We throttle the scene in order to allow the
                    // stones to display, otherwise the game scripts reset them too soon.
                    // Fixes bug #3127824.
                    if (s.CurrentRoomNumber == 100)
                    {
                        s._throttleTrigger = true;
                        neededSleep = 60;
                    }
                    break;
                case SciGameId.ICEMAN:
                    // In ICEMAN the submarine control room is not animating much, so it
                    // runs way too fast. We calm it down even more, otherwise fighting
                    // against other submarines is almost impossible.
                    if (s.CurrentRoomNumber == 27)
                    {
                        s._throttleTrigger = true;
                        neededSleep = 60;
                    }
                    break;
                case SciGameId.LSL3:
                    // LSL3 calculates a machinespeed variable during game startup
                    // (right after the filthy questions). This one would go through w/o
                    // throttling resulting in having to do 1000 pushups or something. Another
                    // way of handling this would be delaying incrementing of "machineSpeed"
                    // selector.
                    if (s.CurrentRoomNumber == 290)
                        s._throttleTrigger = true;
                    break;
                case SciGameId.SQ4:
                    // In SQ4 (floppy and CD) the sequel police appear way too quickly in
                    // the Skate-o-rama rooms, resulting in all sorts of timer issues, like
                    // #3109139 (which occurs because a police officer instantly teleports
                    // just before Roger exits and shoots him). We throttle these scenes a
                    // bit more, in order to prevent timer bugs related to the sequel police.
                    if (s.CurrentRoomNumber == 405 || s.CurrentRoomNumber == 406 ||
                        s.CurrentRoomNumber == 410 || s.CurrentRoomNumber == 411)
                    {
                        s._throttleTrigger = true;
                        neededSleep = 60;
                    }
                    break;
                default:
                    break;
            }

            s.SpeedThrottler(neededSleep);
            return s.r_acc;
        }

        private static Register kHaveMouse(EngineState s, int argc, StackPtr argv)
        {
            return Register.SIGNAL_REG;
        }

        private static Register kFlushResources(EngineState s, int argc, StackPtr argv)
        {
            Gc.Run(s);
            DebugC(DebugLevels.Room, "Entering room number {0}", argv[0].ToUInt16());
            return s.r_acc;
        }

        private static Register kMemory(EngineState s, int argc, StackPtr argv)
        {
            switch ((MemoryFunction)argv[0].ToUInt16())
            {
                case MemoryFunction.ALLOCATE_CRITICAL:
                    {
                        int byteCount = argv[1].ToUInt16();
                        // WORKAROUND:
                        //  - pq3 (multilingual) room 202
                        //     when plotting crimes, allocates the returned bytes from kStrLen
                        //     on "W" and "E" and wants to put a string in there, which doesn't
                        //     fit of course.
                        //  - lsl5 (multilingual) room 280
                        //     allocates memory according to a previous kStrLen for the name of
                        //     the airport ladies (bug #3093818), which isn't enough

                        // We always allocate 1 byte more, because of this
                        byteCount++;

                        if (s._segMan.AllocDynmem(byteCount, "kMemory() critical", out s.r_acc) == null)
                        {
                            throw new InvalidOperationException("Critical heap allocation failed");
                        }
                        break;
                    }
                case MemoryFunction.ALLOCATE_NONCRITICAL:
                    s._segMan.AllocDynmem(argv[1].ToUInt16(), "kMemory() non-critical", out s.r_acc);
                    break;
                case MemoryFunction.FREE:
                    if (!s._segMan.FreeDynmem(argv[1]))
                    {
                        if (SciEngine.Instance.GameId == SciGameId.QFG1VGA)
                        {
                            // Ignore script bug in QFG1VGA, when closing any conversation dialog with esc
                        }
                        else
                        {
                            // Usually, the result of a script bug. Non-critical
                            Warning($"Attempt to kMemory::free() non-dynmem pointer {argv[1]}");
                        }
                    }
                    break;
                case MemoryFunction.MEMCPY:
                    {
                        int size = argv[3].ToUInt16();
                        s._segMan.Memcpy(argv[1], argv[2], size);
                        break;
                    }
                case MemoryFunction.PEEK:
                    {
                        if (argv[1].Segment == 0)
                        {
                            // This occurs in KQ5CD when interacting with certain objects
                            Warning($"Attempt to peek invalid memory at {argv[1]}");
                            return s.r_acc;
                        }

                        SegmentRef @ref = s._segMan.Dereference(argv[1]);

                        if (!@ref.IsValid || @ref.maxSize < 2)
                        {
                            throw new InvalidOperationException($"Attempt to peek invalid memory at {argv[1]}");
                        }
                        if (@ref.isRaw)
                            return Register.Make(0, @ref.raw.Data.ReadSciEndianUInt16(@ref.raw.Offset));
                        else
                        {
                            if (@ref.skipByte)
                                throw new InvalidOperationException($"Attempt to peek memory at odd offset {argv[1]}");
                            return @ref.reg.Value[0];
                        }
                    }
                case MemoryFunction.POKE:
                    {
                        SegmentRef @ref = s._segMan.Dereference(argv[1]);

                        if (!@ref.IsValid || @ref.maxSize < 2)
                        {
                            throw new InvalidOperationException($"Attempt to poke invalid memory at {argv[1]}");
                        }

                        if (@ref.isRaw)
                        {
                            if (argv[2].Segment != 0)
                            {
                                throw new InvalidOperationException($"Attempt to poke memory reference {argv[2]} to {argv[1]}");
                            }
                            @ref.raw.Data.WriteSciEndianUInt16(@ref.raw.Offset, (ushort)argv[2].Offset);       // Amiga versions are BE
                        }
                        else
                        {
                            if (@ref.skipByte)
                                throw new InvalidOperationException($"Attempt to poke memory at odd offset {argv[1]}");
                            @ref.reg = new StackPtr(argv, 2);
                        }
                        break;
                    }
            }

            return s.r_acc;
        }

        private static Register kMemoryInfo(EngineState s, int argc, StackPtr argv)
        {
            // The free heap size returned must not be 0xffff, or some memory
            // calculations will overflow. Crazy Nick's games handle up to 32746
            // bytes (0x7fea), otherwise they throw a warning that the memory is
            // fragmented
            const ushort size = 0x7fea;

            switch ((kMemoryInfoFunc)argv[0].Offset)
            {
                case kMemoryInfoFunc.LARGEST_HEAP_BLOCK:
                    // In order to prevent "Memory fragmented" dialogs from
                    // popping up in some games, we must return FREE_HEAP - 2 here.
                    return Register.Make(0, size - 2);
                case kMemoryInfoFunc.FREE_HEAP:
                case kMemoryInfoFunc.LARGEST_HUNK_BLOCK:
                case kMemoryInfoFunc.FREE_HUNK:
                case kMemoryInfoFunc.TOTAL_HUNK:
                    return Register.Make(0, size);

                default:
                    throw new InvalidOperationException($"Unknown MemoryInfo operation: {argv[0].Offset:X4}");
            }
        }

        private static Register kMemorySegment(EngineState s, int argc, StackPtr argv)
        {
            // MemorySegment provides access to a 256-byte block of memory that remains
            // intact across restarts and restores

            switch ((MemorySegmentFunction)argv[0].ToUInt16())
            {
                case MemorySegmentFunction.SAVE_DATA:
                    {
                        if (argc < 3)
                            throw new InvalidOperationException("Insufficient number of arguments passed to MemorySegment");
                        ushort size = argv[2].ToUInt16();

                        if (size == 0)
                            size = (ushort)(s._segMan.Strlen(argv[1]) + 1);

                        if (size > EngineState.MemorySegmentMax)
                        {
                            // This was set to cut the block to 256 bytes. This should be an
                            // error, as we won't restore the full block that the game scripts
                            // request, thus error out instead.
                            //size = EngineState::kMemorySegmentMax;
                            throw new InvalidOperationException($"kMemorySegment: Requested to save more than 256 bytes ({size})");
                        }

                        s._memorySegmentSize = size;

                        // We assume that this won't be called on pointers
                        s._segMan.Memcpy(new ByteAccess(s._memorySegment), argv[1], size);
                        break;
                    }
                case MemorySegmentFunction.RESTORE_DATA:
                    s._segMan.Memcpy(argv[1], new ByteAccess(s._memorySegment), s._memorySegmentSize);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown MemorySegment operation {argv[0].ToUInt16():X4}");
            }

            return argv[1];
        }

        private static Register kPlatform(EngineState s, int argc, StackPtr argv)
        {
            bool isWindows = SciEngine.Instance.Platform == Core.IO.Platform.Windows;

            if (argc == 0 && ResourceManager.GetSciVersion() < SciVersion.V2)
            {
                // This is called in KQ5CD with no parameters, where it seems to do some
                // graphics driver check. This kernel function didn't have subfunctions
                // then. If 0 is returned, the game functions normally, otherwise all
                // the animations show up like a slideshow (e.g. in the intro). So we
                // return 0. However, the behavior changed for kPlatform with no
                // parameters in SCI32.
                return Register.NULL_REG;
            }

            PlatformOps operation = (PlatformOps)((argc == 0) ? 0 : argv[0].ToUInt16());

            switch (operation)
            {
                case PlatformOps.CDSpeed:
                    // TODO: Returns CD Speed?
                    Warning("STUB: kPlatform(CDSpeed)");
                    break;
                case PlatformOps.Unk2:
                    // Always returns 2
                    return Register.Make(0, 2);
                case PlatformOps.CDCheck:
                    // TODO: Some sort of CD check?
                    Warning("STUB: PlatformOps.(CDCheck)");
                    break;
                case PlatformOps.Unk0:
                case PlatformOps.GetPlatform:
                    // For Mac versions, PlatformOps.(0) with other args has more functionality
                    if (operation == PlatformOps.Unk0 && SciEngine.Instance.Platform == Core.IO.Platform.Macintosh && argc > 1)
                        return kMacPlatform(s, argc - 1, argv + 1);
                    return Register.Make(0, (ushort)(isWindows ? SciPlatformWindows : SciPlatformDOS));
                case PlatformOps.Unk5:
                    // This case needs to return the opposite of case 6 to get hires graphics
                    return Register.Make(0, !isWindows);
                case PlatformOps.IsHiRes:
                    return Register.Make(0, isWindows);
                case PlatformOps.IsItWindows:
                    return Register.Make(0, isWindows);
                default:
                    throw new InvalidOperationException($"Unsupported kPlatform operation {operation}");
            }

            return Register.NULL_REG;
        }

        private static Register kRestartGame(EngineState s, int argc, StackPtr argv)
        {
            s.ShrinkStackToBase();

            s.abortScriptProcessing = AbortGameState.RestartGame; // Force vm to abort ASAP
            return Register.NULL_REG;
        }

        // kMacPlatform is really a subop of kPlatform for SCI1.1+ Mac
        private static Register kMacPlatform(EngineState s, int argc, StackPtr argv)
        {
            // Mac versions use their own secondary platform functions
            // to do various things. Why didn't they just declare a new
            // kernel function?

            switch (argv[0].ToUInt16())
            {
                case 0:
                    // Subop 0 has changed a few times
                    // In SCI1, its usage is still unknown
                    // In SCI1.1, it's NOP
                    // In SCI32, it's used for remapping cursor ID's
                    if (ResourceManager.GetSciVersion() >= SciVersion.V2_1_EARLY) // Set Mac cursor remap
                        SciEngine.Instance._gfxCursor.SetMacCursorRemapList(argc - 1, argv + 1);
                    else if (ResourceManager.GetSciVersion() != SciVersion.V1_1)
                    {
                        Warning("Unknown SCI1 kMacPlatform(0) call");
                    }
                    break;
                case 4: // Handle icon bar code
                    return kIconBar(s, argc - 1, argv + 1);
                case 7: // Unknown, but always return -1
                    return Register.SIGNAL_REG;
                case 1: // Unknown, calls QuickDraw region functions (KQ5, QFG1VGA, Dr. Brain 1)
                    break;  // removed warning, as it produces a lot of spam in the console
                case 2: // Unknown, "UseNextWaitEvent" (Various)
                case 3: // Unknown, "ProcessOpenDocuments" (Various)
                case 5: // Unknown, plays a sound (KQ7)
                case 6: // Unknown, menu-related (Unused?)
                    Warning($"Unhandled kMacPlatform({argv[0].ToUInt16()})");
                    break;
                default:
                    throw new InvalidOperationException($"Unknown kMacPlatform({argv[0].ToUInt16()})");
            }

            return s.r_acc;
        }

        // kIconBar is really a subop of kMacPlatform for SCI1.1 Mac
        private static Register kIconBar(EngineState s, int argc, StackPtr argv)
        {
            // Mac versions use their own tertiary platform functions
            // to handle the outside-of-the-screen icon bar.

            // QFG1 Mac calls this function to load the Mac icon bar (of which
            // the resources do exist), but the game completely ignores it and
            // uses the standard icon bar for the game. We do the same.
            if (!SciEngine.Instance.HasMacIconBar)
                return Register.NULL_REG;

            switch (argv[0].ToUInt16())
            {
                case 0: // InitIconBar
                    for (int i = 0; i < argv[1].ToUInt16(); i++)
                        SciEngine.Instance._gfxMacIconBar.AddIcon(argv[i + 2]);
                    break;
                case 1: // DisposeIconBar
                    Warning("kIconBar(Dispose)");
                    break;
                case 2: // EnableIconBar (-1 = all)
                    Debug(0, $"kIconBar(Enable, {argv[1].ToInt16()})");
                    SciEngine.Instance._gfxMacIconBar.SetIconEnabled(argv[1].ToInt16(), true);
                    break;
                case 3: // DisableIconBar (-1 = all)
                    Debug(0, $"kIconBar(Disable, {argv[1].ToInt16()})");
                    SciEngine.Instance._gfxMacIconBar.SetIconEnabled(argv[1].ToInt16(), false);
                    break;
                case 4: // SetIconBarIcon
                    Debug(0, $"kIconBar(SetIcon, {argv[1].ToUInt16()}, {argv[2].ToUInt16()})");
                    if (argv[2].ToInt16() == -1)
                        SciEngine.Instance._gfxMacIconBar.SetInventoryIcon(argv[2].ToInt16());
                    break;
                default:
                    throw new InvalidOperationException($"Unknown kIconBar({argv[0].ToUInt16()})");
            }

            SciEngine.Instance._gfxMacIconBar.DrawIcons();

            return Register.NULL_REG;
        }

        private static Register kGetTime(EngineState s, int argc, StackPtr argv)
        {
            long elapsedTime = SciEngine.Instance.TotalPlayTime;
            int retval = 0; // Avoid spurious warning

            var loc_time = DateTime.Now;

            GetTimeMode mode = (argc > 0) ? (GetTimeMode)argv[0].ToUInt16() : 0;

            // Modes 2 and 3 are supported since 0.629.
            // This condition doesn't check that exactly, but close enough.
            if (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY && mode > GetTimeMode.TIME_12HOUR)
                throw new InvalidOperationException($"kGetTime called in SCI0 with mode {mode} (expected 0 or 1)");

            switch (mode)
            {
                case GetTimeMode.TICKS:
                    retval = (int)(elapsedTime * 60 / 1000);
                    DebugC(DebugLevels.Time, "GetTime(elapsed) returns {0}", retval);
                    break;
                case GetTimeMode.TIME_12HOUR:
                    retval = ((loc_time.Hour % 12) << 12) | (loc_time.Minute << 6) | (loc_time.Second);
                    DebugC(DebugLevels.Time, "GetTime(12h) returns {0}", retval);
                    break;
                case GetTimeMode.TIME_24HOUR:
                    retval = (loc_time.Hour << 11) | (loc_time.Minute << 5) | (loc_time.Second >> 1);
                    DebugC(DebugLevels.Time, "GetTime(24h) returns {0}", retval);
                    break;
                case GetTimeMode.DATE:
                    // Year since 1980 (0 = 1980, 1 = 1981, etc.)
                    retval = loc_time.Day | ((loc_time.Month + 1) << 5) | (((loc_time.Year - 1980) & 0x7f) << 9);
                    DebugC(DebugLevels.Time, "GetTime(date) returns {0}", retval);
                    break;
                default:
                    throw new InvalidOperationException($"Attempt to use unknown GetTime mode {mode}");
            }

            //Debug($"GetTime=>{retval}");
            return Register.Make(0, (ushort)retval);
        }

        private static Register kStub(EngineState s, int argc, StackPtr argv)
        {
            throw new NotSupportedException("kStub");
        }

        private static Register kStubNull(EngineState s, int argc, StackPtr argv)
        {
            kStub(s, argc, argv);
            return Register.NULL_REG;
        }

        internal static Register kDummy(EngineState s, int argc, StackPtr argv)
        {
            kStub(s, argc, argv);
            throw new InvalidOperationException("Kernel function was called, which was considered to be unused - see log for details");
        }

        private static Register kSetDebug(EngineState s, int argc, StackPtr argv)
        {
            // WORKAROUND: For some reason, GK1 calls this unconditionally when
            // watching the intro. Older (SCI0) games call it on room change if
            // a flag is set, in which case the debugger SHOULD get activated.
            // Therefore, don't break into the debugger in GK1, but do so elsewhere.

            if (SciEngine.Instance.GameId != SciGameId.GK1)
            {
                Debug("Debug mode activated");

                //SciEngine.Instance.Debugger.Attach();
            }

            return s.r_acc;
        }

        private static Register kAvoidPath(EngineState s, int argc, StackPtr argv)
        {
            var start = new Point(argv[0].ToInt16(), argv[1].ToInt16());

            switch (argc)
            {

                case 3:
                    {
                        Register retval;
                        Polygon polygon = ConvertPolygon(s, argv[2]);

                        if (polygon == null)
                            return Register.NULL_REG;

                        // Override polygon type to prevent inverted result for contained access polygons
                        polygon.Type = PolygonType.BARRED_ACCESS;

                        retval = Register.Make(0, Contained(start, polygon) != PolygonContainmentType.OUTSIDE);
                        return retval;
                    }
                case 6:
                case 7:
                case 8:
                    {
                        var end = new Point(argv[2].ToInt16(), argv[3].ToInt16());
                        Register poly_list, output;
                        int width, height, opt = 1;

                        if (ResourceManager.GetSciVersion() >= SciVersion.V2)
                        {
                            if (argc < 7)
                                Error("[avoidpath] Not enough arguments");

                            poly_list = (!argv[4].IsNull ? SciEngine.ReadSelector(s._segMan, argv[4], o => o.elements) : Register.NULL_REG);
                            width = argv[5].ToUInt16();
                            height = argv[6].ToUInt16();
                            if (argc > 7)
                                opt = argv[7].ToUInt16();
                        }
                        else
                        {
                            // SCI1.1 and older games always ran with an internal resolution of 320x200
                            poly_list = argv[4];
                            width = 320;
                            height = 190;
                            if (argc > 6)
                                opt = argv[6].ToUInt16();
                        }

                        // TODO: if (DebugManager.Instance.IsDebugChannelEnabled(DebugLevels.AvoidPath))
                        //{
                        //    Debug("[avoidpath] Pathfinding input:");
                        //    DrawPoint(s, start, 1, width, height);
                        //    DrawPoint(s, end, 0, width, height);

                        //    if (poly_list.Segment)
                        //    {
                        //        PrintInput(s, poly_list, start, end, opt);
                        //        DrawInput(s, poly_list, start, end, opt, width, height);
                        //    }

                        //    // Update the whole screen
                        //    SciEngine.Instance._gfxScreen.CopyToScreen();
                        //    SciEngine.Instance.System.GraphicsManager.UpdateScreen();
                        //    if (SciEngine.Instance._gfxPaint16 == null)
                        //        ServiceLocator.Platform.Sleep(2500);
                        //}

                        PathfindingState p = ConvertPolygonSet(s, poly_list, start, end, width, height, opt);

                        if (p == null)
                        {
                            Warning("[avoidpath] Error: pathfinding failed for following input:\n");
                            PrintInput(s, poly_list, start, end, opt);
                            Warning("[avoidpath] Returning direct path from start point to end point\n");
                            output = AllocateOutputArray(s._segMan, 3);
                            SegmentRef arrayRef = s._segMan.Dereference(output);
                            System.Diagnostics.Debug.Assert(arrayRef.IsValid && !arrayRef.skipByte);

                            WritePoint(arrayRef, 0, start);
                            WritePoint(arrayRef, 1, end);
                            WritePoint(arrayRef, 2, new Point(POLY_LAST_POINT, POLY_LAST_POINT));

                            return output;
                        }

                        // Apply Dijkstra
                        AStar(p);

                        output = OutputPath(p, s);

                        // Memory is freed by explicit calls to Memory
                        return output;
                    }

                default:
                    Warning($"Unknown AvoidPath subfunction {argc}");
                    return Register.NULL_REG;
            }
        }

        private static void AStar(PathfindingState s)
        {
            // Vertices of which the shortest path is known
            var closedSet = new List<Vertex>();

            // The remaining vertices
            var openSet = new List<Vertex>();
            openSet.Add(s.vertex_start);
            s.vertex_start.costG = 0;
            s.vertex_start.costF = (uint)Math.Sqrt((float)s.vertex_start.v.SquareDistance(s.vertex_end.v));

            while (openSet.Count != 0)
            {
                // Find vertex in open set with lowest F cost
                var vertex_min_it = openSet.Last();
                Vertex vertex_min = null;
                uint min = Vertex.HUGE_DISTANCE;

                foreach (var vertex in openSet)
                {
                    if (vertex.costF < min)
                    {
                        vertex_min_it = vertex;
                        vertex_min = vertex_min_it;
                        min = vertex.costF;
                    }
                }

                System.Diagnostics.Debug.Assert(vertex_min != null);    // the vertex cost should never be bigger than HUGE_DISTANCE

                // Check if we are done
                if (vertex_min == s.vertex_end)
                    break;

                // Move vertex from set open to set closed
                closedSet.Add(vertex_min);
                openSet.Remove(vertex_min_it);

                var visVerts = VisibleVertices(s, vertex_min);

                foreach (var vertex in visVerts)
                {
                    uint new_dist;

                    if (closedSet.Contains(vertex))
                        continue;

                    if (!openSet.Contains(vertex))
                        openSet.Insert(0, vertex);

                    new_dist = vertex_min.costG + (uint)Math.Sqrt((float)vertex_min.v.SquareDistance(vertex.v));

                    // When travelling to a vertex on the screen edge, we
                    // add a penalty score to make this path less appealing.
                    // NOTE: If an obstacle has only one vertex on a screen edge,
                    // later SSCI pathfinders will treat that vertex like any
                    // other, while we apply a penalty to paths traversing it.
                    // This difference might lead to problems, but none are
                    // known at the time of writing.

                    // WORKAROUND: This check fails in QFG1VGA, room 81 (bug report #3568452).
                    // However, it is needed in other SCI1.1 games, such as LB2. Therefore, we
                    // add this workaround for that scene in QFG1VGA, until our algorithm matches
                    // better what SSCI is doing. With this workaround, QFG1VGA no longer freezes
                    // in that scene.
                    bool qfg1VgaWorkaround = (SciEngine.Instance.GameId == SciGameId.QFG1VGA &&
                                              SciEngine.Instance.EngineState.CurrentRoomNumber == 81);

                    if (s.PointOnScreenBorder(vertex.v) && !qfg1VgaWorkaround)
                        new_dist += 10000;

                    if (new_dist < vertex.costG)
                    {
                        vertex.costG = new_dist;
                        vertex.costF = vertex.costG + (uint)Math.Sqrt((float)vertex.v.SquareDistance(s.vertex_end.v));
                        vertex.path_prev = vertex_min;
                    }
                }
            }

            if (openSet.Count==0)
                DebugC(DebugLevels.AvoidPath, "AvoidPath: End point ({0}, {1}) is unreachable", s.vertex_end.v.X, s.vertex_end.v.Y);
        }

        private static List<Vertex> VisibleVertices(PathfindingState s, Vertex vertex_cur)
        {
            List<Vertex> visVerts = new List<Vertex>();

            for (int i = 0; i < s.vertices; i++)
            {
                Vertex vertex = s.vertex_index[i];

                // Make sure we don't intersect a polygon locally at the vertices
                if ((vertex == vertex_cur) || (Inside(vertex.v, vertex_cur)) || (Inside(vertex_cur.v, vertex)))
                    continue;

                // Check for intersecting edges
                int j;
                for (j = 0; j < s.vertices; j++)
                {
                    Vertex edge = s.vertex_index[j];
                    if (VertexHasEdges(edge))
                    {
                        if (Between(vertex_cur.v, vertex.v, edge.v))
                        {
                            // If we hit a vertex, make sure we can pass through it without intersecting its polygon
                            if ((Inside(vertex_cur.v, edge)) || (Inside(vertex.v, edge)))
                                break;

                            // This edge won't properly intersect, so we continue
                            continue;
                        }

                        if (IntersectProper(vertex_cur.v, vertex.v, edge.v, edge._next.v))
                            break;
                    }
                }

                if (j == s.vertices)
                    visVerts.Insert(0, vertex);
            }

            return visVerts;
        }

        private static bool IntersectProper(Point a, Point b, Point c, Point d)
        {
            bool ab = (Left(a, b, c) && Left(b, a, d)) || (Left(a, b, d) && Left(b, a, c));
            bool cd = (Left(c, d, a) && Left(d, c, b)) || (Left(c, d, b) && Left(d, c, a));

            return ab && cd;
        }

        private static Register OutputPath(PathfindingState p, EngineState s)
        {
            int path_len = 0;
            Register output;
            Vertex vertex = p.vertex_end;
            bool unreachable = vertex.path_prev == null;

            if (!unreachable)
            {
                while (vertex != null)
                {
                    // Compute path length
                    path_len++;
                    vertex = vertex.path_prev;
                }
            }

            // Allocate memory for path, plus 3 extra for appended point, prepended point and sentinel
            output = AllocateOutputArray(s._segMan, path_len + 3);
            SegmentRef arrayRef = s._segMan.Dereference(output);
            System.Diagnostics.Debug.Assert(arrayRef.IsValid && !arrayRef.skipByte);

            if (unreachable)
            {
                // If pathfinding failed we only return the path up to vertex_start

                if (p._prependPoint != null)
                    WritePoint(arrayRef, 0, p._prependPoint.Value);
                else
                    WritePoint(arrayRef, 0, p.vertex_start.v);

                WritePoint(arrayRef, 1, p.vertex_start.v);
                WritePoint(arrayRef, 2, new Point(POLY_LAST_POINT, POLY_LAST_POINT));

                return output;
            }

            int offset = 0;

            if (p._prependPoint != null)
                WritePoint(arrayRef, offset++, p._prependPoint.Value);

            vertex = p.vertex_end;
            for (int i = path_len - 1; i >= 0; i--)
            {
                WritePoint(arrayRef, offset + i, vertex.v);
                vertex = vertex.path_prev;
            }
            offset += path_len;

            if (p._appendPoint != null)
                WritePoint(arrayRef, offset++, p._appendPoint.Value);

            // Sentinel
            WritePoint(arrayRef, offset, new Point(POLY_LAST_POINT, POLY_LAST_POINT));

            if (DebugManager.Instance.IsDebugChannelEnabled(DebugLevels.AvoidPath))
            {
                Debug("\nReturning path:");

                SegmentRef outputList = s._segMan.Dereference(output);
                if (!outputList.IsValid || outputList.skipByte)
                {
                    Warning("output_path: Polygon data pointer is invalid, skipping polygon");
                    return output;
                }

                for (int i = 0; i < offset; i++)
                {
                    Point pt = ReadPoint(outputList, i);
                    // debugN(-1, " (%i, %i)", pt.x, pt.y);
                }
                Debug(";\n");
            }

            return output;
        }


        private static void WritePoint(SegmentRef @ref, int offset, Point point)
        {
            if (@ref.isRaw)
            {    // dynmem blocks are raw
                @ref.raw.Data.WriteSciEndianUInt16(@ref.raw.Offset + offset * POLY_POINT_SIZE, (ushort)point.X);
                @ref.raw.Data.WriteSciEndianUInt16(@ref.raw.Offset + offset * POLY_POINT_SIZE + 2, (ushort)point.Y);
            }
            else
            {
                var reg = @ref.reg.Value;
                reg[offset * 2] = Register.Make(0, (ushort)point.X);
                reg[offset * 2 + 1] = Register.Make(0, (ushort)point.Y);
            }
        }

        private static Register AllocateOutputArray(SegManager segMan, int size)
        {
            Register addr;

# if ENABLE_SCI32
            if (getSciVersion() >= SCI_VERSION_2)
            {
                SciArray<reg_t>* array = segMan.allocateArray(&addr);
                assert(array);
                array.setType(0);
                array.setSize(size * 2);
                return addr;
            }
#endif

            segMan.AllocDynmem(POLY_POINT_SIZE * size, AVOIDPATH_DYNMEM_STRING, out addr);
            return addr;
        }

        private static void PrintPolygon(SegManager segMan, Register polygon)
        {
            Register points = SciEngine.ReadSelector(segMan, polygon, o => o.points);

# if ENABLE_SCI32
            if (segMan.isHeapObject(points))
                points = readSelector(segMan, points, SELECTOR(data));
#endif

            int size = (int)SciEngine.ReadSelectorValue(segMan, polygon, o => o.size);
            int type = (int)SciEngine.ReadSelectorValue(segMan, polygon, o => o.type);
            int i;
            Point point;

            // TODO: debugN(-1, "%i:", type);

            SegmentRef pointList = segMan.Dereference(points);
            if (!pointList.IsValid || pointList.skipByte)
            {
                Warning("print_polygon: Polygon data pointer is invalid, skipping polygon");
                return;
            }

            for (i = 0; i < size; i++)
            {
                point = ReadPoint(pointList, i);
                // TODO: debugN(-1, " (%i, %i)", point.x, point.y);
            }

            point = ReadPoint(pointList, 0);
            Debug($" ({point.X}, {point.Y})");
        }

        private static void PrintInput(EngineState s, Register poly_list, Point start, Point end, int opt)
        {
            List list;
            Node node;

            Debug($"Start point: ({start.X}, {start.Y})");
            Debug($"End point: ({end.X}, {end.Y})");
            Debug($"Optimization level: {opt}");

            if (poly_list.Segment == 0)
                return;

            list = s._segMan.LookupList(poly_list);

            if (list == null)
            {
                Warning("[avoidpath] Could not obtain polygon list");
                return;
            }

            Debug("Polygons:");
            node = s._segMan.LookupNode(list.first);

            while (node != null)
            {
                PrintPolygon(s._segMan, node.value);
                node = s._segMan.LookupNode(node.succ);
            }

        }
    }
}
