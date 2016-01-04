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


using System;

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

    partial class Kernel
    {
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

        private static Register kGetTime(EngineState s, int argc, StackPtr? argv)
        {
            // TODO: g_engine->getTotalPlayTime();
            int elapsedTime = 0;
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
                    retval = elapsedTime * 60 / 1000;
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
                    retval = loc_time.Day | ((loc_time.Month + 1) << 5) | (((loc_time.Year - 80) & 0x7f) << 9);
                    // TODO: debugC(kDebugLevelTime, "GetTime(date) returns %d", retval);
                    break;
                default:
                    throw new InvalidOperationException($"Attempt to use unknown GetTime mode {mode}");
            }

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

        private static Register kDummy(EngineState s, int argc, StackPtr? argv)
        {
            kStub(s, argc, argv);
            throw new InvalidOperationException("Kernel function was called, which was considered to be unused - see log for details");
        }
    }
}
