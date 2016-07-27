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

        private static Register kEmpty(EngineState s, int argc, StackPtr? argv)
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
        private static Register kGameIsRestarting(EngineState s, int argc, StackPtr? argv)
        {
            s.r_acc = Register.Make(0, (ushort)s.gameIsRestarting);

            if (argc != 0)
            { // Only happens during replay
                if (argv.Value[0].ToUInt16() == 0) // Set restarting flag
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

        private static Register kHaveMouse(EngineState s, int argc, StackPtr? argv)
        {
            return Register.SIGNAL_REG;
        }

        private static Register kFlushResources(EngineState s, int argc, StackPtr? argv)
        {
            Gc.Run(s);
            // TODO: debugC(kDebugLevelRoom, "Entering room number %d", argv[0].toUint16());
            return s.r_acc;
        }

        private static Register kMemory(EngineState s, int argc, StackPtr? argv)
        {
            switch ((MemoryFunction)argv.Value[0].ToUInt16())
            {
                case MemoryFunction.ALLOCATE_CRITICAL:
                    {
                        int byteCount = argv.Value[1].ToUInt16();
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
                    s._segMan.AllocDynmem(argv.Value[1].ToUInt16(), "kMemory() non-critical", out s.r_acc);
                    break;
                case MemoryFunction.FREE:
                    if (!s._segMan.FreeDynmem(argv.Value[1]))
                    {
                        if (SciEngine.Instance.GameId == SciGameId.QFG1VGA)
                        {
                            // Ignore script bug in QFG1VGA, when closing any conversation dialog with esc
                        }
                        else
                        {
                            // Usually, the result of a script bug. Non-critical
                            Warning($"Attempt to kMemory::free() non-dynmem pointer {argv.Value[1]}");
                        }
                    }
                    break;
                case MemoryFunction.MEMCPY:
                    {
                        int size = argv.Value[3].ToUInt16();
                        s._segMan.Memcpy(argv.Value[1], argv.Value[2], size);
                        break;
                    }
                case MemoryFunction.PEEK:
                    {
                        if (argv.Value[1].Segment == 0)
                        {
                            // This occurs in KQ5CD when interacting with certain objects
                            Warning($"Attempt to peek invalid memory at {argv.Value[1]}");
                            return s.r_acc;
                        }

                        SegmentRef @ref = s._segMan.Dereference(argv.Value[1]);

                        if (!@ref.IsValid || @ref.maxSize < 2)
                        {
                            throw new InvalidOperationException($"Attempt to peek invalid memory at {argv.Value[1]}");
                        }
                        if (@ref.isRaw)
                            return Register.Make(0, @ref.raw.Data.ReadSciEndianUInt16(@ref.raw.Offset));
                        else
                        {
                            if (@ref.skipByte)
                                throw new InvalidOperationException($"Attempt to peek memory at odd offset {argv.Value[1]}");
                            return @ref.reg.Value[0];
                        }
                    }
                case MemoryFunction.POKE:
                    {
                        SegmentRef @ref = s._segMan.Dereference(argv.Value[1]);

                        if (!@ref.IsValid || @ref.maxSize < 2)
                        {
                            throw new InvalidOperationException($"Attempt to poke invalid memory at {argv.Value[1]}");
                        }

                        if (@ref.isRaw)
                        {
                            if (argv.Value[2].Segment != 0)
                            {
                                throw new InvalidOperationException($"Attempt to poke memory reference {argv.Value[2]} to {argv.Value[1]}");
                            }
                            @ref.raw.Data.WriteSciEndianUInt16(@ref.raw.Offset, (ushort)argv.Value[2].Offset);       // Amiga versions are BE
                        }
                        else
                        {
                            if (@ref.skipByte)
                                throw new InvalidOperationException($"Attempt to poke memory at odd offset {argv.Value[1]}");
                            @ref.reg = new StackPtr(argv.Value, 2);
                        }
                        break;
                    }
            }

            return s.r_acc;
        }

        private static Register kMemoryInfo(EngineState s, int argc, StackPtr? argv)
        {
            // The free heap size returned must not be 0xffff, or some memory
            // calculations will overflow. Crazy Nick's games handle up to 32746
            // bytes (0x7fea), otherwise they throw a warning that the memory is
            // fragmented
            const ushort size = 0x7fea;

            switch ((kMemoryInfoFunc)argv.Value[0].Offset)
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
                    throw new InvalidOperationException($"Unknown MemoryInfo operation: {argv.Value[0].Offset:X4}");
            }
        }

        private static Register kMemorySegment(EngineState s, int argc, StackPtr? argv)
        {
            // MemorySegment provides access to a 256-byte block of memory that remains
            // intact across restarts and restores

            switch ((MemorySegmentFunction)argv.Value[0].ToUInt16())
            {
                case MemorySegmentFunction.SAVE_DATA:
                    {
                        if (argc < 3)
                            throw new InvalidOperationException("Insufficient number of arguments passed to MemorySegment");
                        ushort size = argv.Value[2].ToUInt16();

                        if (size == 0)
                            size = (ushort)(s._segMan.Strlen(argv.Value[1]) + 1);

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
                        s._segMan.Memcpy(new ByteAccess(s._memorySegment), argv.Value[1], size);
                        break;
                    }
                case MemorySegmentFunction.RESTORE_DATA:
                    s._segMan.Memcpy(argv.Value[1], new ByteAccess(s._memorySegment), s._memorySegmentSize);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown MemorySegment operation {argv.Value[0].ToUInt16():X4}");
            }

            return argv.Value[1];
        }

        private static Register kPlatform(EngineState s, int argc, StackPtr? argv)
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

            PlatformOps operation = (PlatformOps)((argc == 0) ? 0 : argv.Value[0].ToUInt16());

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

        private static Register kRestartGame(EngineState s, int argc, StackPtr? argv)
        {
            s.ShrinkStackToBase();

            s.abortScriptProcessing = AbortGameState.RestartGame; // Force vm to abort ASAP
            return Register.NULL_REG;
        }

        // kMacPlatform is really a subop of kPlatform for SCI1.1+ Mac
        private static Register kMacPlatform(EngineState s, int argc, StackPtr? argv)
        {
            // Mac versions use their own secondary platform functions
            // to do various things. Why didn't they just declare a new
            // kernel function?

            switch (argv.Value[0].ToUInt16())
            {
                case 0:
                    // Subop 0 has changed a few times
                    // In SCI1, its usage is still unknown
                    // In SCI1.1, it's NOP
                    // In SCI32, it's used for remapping cursor ID's
                    if (ResourceManager.GetSciVersion() >= SciVersion.V2_1) // Set Mac cursor remap
                        SciEngine.Instance._gfxCursor.SetMacCursorRemapList(argc - 1, argv.Value + 1);
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
                    Warning($"Unhandled kMacPlatform({argv.Value[0].ToUInt16()})");
                    break;
                default:
                    throw new InvalidOperationException($"Unknown kMacPlatform({argv.Value[0].ToUInt16()})");
            }

            return s.r_acc;
        }

        // kIconBar is really a subop of kMacPlatform for SCI1.1 Mac
        private static Register kIconBar(EngineState s, int argc, StackPtr? argv)
        {
            // Mac versions use their own tertiary platform functions
            // to handle the outside-of-the-screen icon bar.

            // QFG1 Mac calls this function to load the Mac icon bar (of which
            // the resources do exist), but the game completely ignores it and
            // uses the standard icon bar for the game. We do the same.
            if (!SciEngine.Instance.HasMacIconBar)
                return Register.NULL_REG;

            switch (argv.Value[0].ToUInt16())
            {
                case 0: // InitIconBar
                    for (int i = 0; i < argv.Value[1].ToUInt16(); i++)
                        SciEngine.Instance._gfxMacIconBar.AddIcon(argv.Value[i + 2]);
                    break;
                case 1: // DisposeIconBar
                    Warning("kIconBar(Dispose)");
                    break;
                case 2: // EnableIconBar (-1 = all)
                    Debug(0, $"kIconBar(Enable, {argv.Value[1].ToInt16()})");
                    SciEngine.Instance._gfxMacIconBar.SetIconEnabled(argv.Value[1].ToInt16(), true);
                    break;
                case 3: // DisableIconBar (-1 = all)
                    Debug(0, $"kIconBar(Disable, {argv.Value[1].ToInt16()})");
                    SciEngine.Instance._gfxMacIconBar.SetIconEnabled(argv.Value[1].ToInt16(), false);
                    break;
                case 4: // SetIconBarIcon
                    Debug(0, $"kIconBar(SetIcon, {argv.Value[1].ToUInt16()}, {argv.Value[2].ToUInt16()})");
                    if (argv.Value[2].ToInt16() == -1)
                        SciEngine.Instance._gfxMacIconBar.SetInventoryIcon(argv.Value[2].ToInt16());
                    break;
                default:
                    throw new InvalidOperationException($"Unknown kIconBar({argv.Value[0].ToUInt16()})");
            }

            SciEngine.Instance._gfxMacIconBar.DrawIcons();

            return Register.NULL_REG;
        }

        private static Register kGetTime(EngineState s, int argc, StackPtr? argv)
        {
            long elapsedTime = SciEngine.Instance.TotalPlaytime;
            int retval = 0; // Avoid spurious warning

            var loc_time = DateTime.Now;

            GetTimeMode mode = (argc > 0) ? (GetTimeMode)argv.Value[0].ToUInt16() : 0;

            // Modes 2 and 3 are supported since 0.629.
            // This condition doesn't check that exactly, but close enough.
            if (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY && mode > GetTimeMode.TIME_12HOUR)
                throw new InvalidOperationException($"kGetTime called in SCI0 with mode {mode} (expected 0 or 1)");

            switch (mode)
            {
                case GetTimeMode.TICKS:
                    retval = (int)(elapsedTime * 60 / 1000);
                    // TODO: debugC(kDebugLevelTime, "GetTime(elapsed) returns %d", retval);
                    break;
                case GetTimeMode.TIME_12HOUR:
                    retval = ((loc_time.Hour % 12) << 12) | (loc_time.Minute << 6) | (loc_time.Second);
                    // TODO: debugC(kDebugLevelTime, "GetTime(12h) returns %d", retval);
                    break;
                case GetTimeMode.TIME_24HOUR:
                    retval = (loc_time.Hour << 11) | (loc_time.Minute << 5) | (loc_time.Second >> 1);
                    // TODO: debugC(kDebugLevelTime, "GetTime(24h) returns %d", retval);
                    break;
                case GetTimeMode.DATE:
                    // Year since 1980 (0 = 1980, 1 = 1981, etc.)
                    retval = loc_time.Day | ((loc_time.Month + 1) << 5) | (((loc_time.Year - 1980) & 0x7f) << 9);
                    // TODO: debugC(kDebugLevelTime, "GetTime(date) returns %d", retval);
                    break;
                default:
                    throw new InvalidOperationException($"Attempt to use unknown GetTime mode {mode}");
            }

            //Debug($"GetTime=>{retval}");
            return Register.Make(0, (ushort)retval);
        }

        private static Register kStub(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }

        private static Register kStubNull(EngineState s, int argc, StackPtr? argv)
        {
            kStub(s, argc, argv);
            return Register.NULL_REG;
        }

        internal static Register kDummy(EngineState s, int argc, StackPtr? argv)
        {
            kStub(s, argc, argv);
            throw new InvalidOperationException("Kernel function was called, which was considered to be unused - see log for details");
        }

        private static Register kSetDebug(EngineState s, int argc, StackPtr? argv)
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

        private static Register kAvoidPath(EngineState s, int argc, StackPtr? argv)
        {
            throw new NotImplementedException();
        }
    }
}
