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

using NScumm.Core.Graphics;
using System.Collections.Generic;
using System;
using NScumm.Core;

namespace NScumm.Sci.Engine
{
    internal enum AbortGameState
    {
        None = 0,
        LoadGame = 1,
        RestartGame = 2,
        QuitGame = 3
    }

    internal class VideoState
    {
        public string fileName;
        public ushort x;
        public ushort y;
        public ushort flags;

        public void Reset()
        {
            fileName = "";
            x = y = flags = 0;
        }
    }

    internal enum GameIsRestarting
    {
        NONE = 0,
        RESTART = 1,
        RESTORE = 2
    }

    internal class EngineState
    {
        /// <summary>
        /// MemorySegment provides access to a 256-byte block of memory that remains
        /// intact across restarts and restores
        /// </summary>
        public const int MemorySegmentMax = 256;

        // We assume that scripts give us savegameId 0.99 for creating a new save slot
        //  and savegameId 100.199 for existing save slots. Refer to kfile.cpp
        private const int SAVEGAMEID_OFFICIALRANGE_START = 100;
        private const int SAVEGAMEID_OFFICIALRANGE_END = 199;

        /// <summary>
        /// The segment manager
        /// </summary>
        public SegManager _segMan;

        /* Non-VM information */

        /// <summary>
        /// The last time the game invoked Wait() 
        /// </summary>
        public int lastWaitTime;
        /// <summary>
        /// The last time the game updated the screen
        /// </summary>
        public int _screenUpdateTime;

        /// <summary>
        /// total times kAnimate was invoked
        /// </summary>
        public uint _throttleCounter;
        /// <summary>
        /// last time kAnimate was invoked
        /// </summary>
        public int _throttleLastTime;
        public bool _throttleTrigger;
        public bool _gameIsBenchmarking;

        /* Kernel File IO stuff */

        /// <summary>
        /// Array of file handles. Dynamically increased if required.
        /// </summary>
        public FileHandle[] _fileHandles;

        public DirSeeker _dirseeker;

        /// <summary>
        /// last virtual id fed to kSaveGame, if no kGetSaveFiles was called inbetween
        /// </summary>
        public short _lastSaveVirtualId;
        /// <summary>
        /// last newly created filename-id by kSaveGame
        /// </summary>
        public short _lastSaveNewId;

# if ENABLE_SCI32
        public VirtualIndexFile _virtualIndexFile;
#endif
        /// <summary>
        /// Remembers the item selected in QfG import rooms
        /// </summary>
        public int _chosenQfGImportItem;

        /// <summary>
        /// Refer to GfxCursor::setPosition()
        /// </summary>
        public bool _cursorWorkaroundActive;
        public Point _cursorWorkaroundPoint;
        public Rect _cursorWorkaroundRect;

        /* VM Information */

        /// <summary>
        /// The execution stack
        /// </summary>
        public List<ExecStack> _executionStack;
        /// <summary>
        /// When called from kernel functions, the vm is re-started recursively on
        /// the same stack. This variable contains the stack base for the current vm.
        /// </summary>
        public int executionStackBase;
        /// <summary>
        /// Set to true if the execution stack position should be re-evaluated by the vm 
        /// </summary>
        public bool _executionStackPosChanged;

        /* Registers */

        /// <summary>
        /// Accumulator
        /// </summary>
        public Register r_acc;
        /// <summary>
        /// previous comparison result
        /// </summary>
        public Register r_prev;
        /// <summary>
        /// current &rest register
        /// </summary>
        public short r_rest;
        private int g_debug_sleeptime_factor = 1;

        /// <summary>
        /// Pointer to the least stack element
        /// </summary>
        public StackPtr stack_base;

        /// <summary>
        /// First invalid stack element
        /// </summary>
        public StackPtr stack_top;

        // Script state
        public ExecStack xs;
        /// <summary>
        /// global, local, temp, param, as immediate pointers
        /// </summary>
        public StackPtr[] variables = new StackPtr[4];
        /// <summary>
        /// Used for referencing VM ops
        /// </summary>
        public StackPtr[] variablesBase = new StackPtr[4];
        /// <summary>
        /// Same as above, contains segment IDs
        /// </summary>
        public int[] variablesSegment = new int[4];
        /// <summary>
        /// Max. values for all variables
        /// </summary>
        public int[] variablesMax = new int[4];

        public AbortGameState abortScriptProcessing;
        public GameIsRestarting gameIsRestarting; // is set when restarting (=1) or restoring the game (=2)

        public int scriptStepCounter; // Counts the number of steps executed
        public int scriptGCInterval; // Number of steps in between gcs

        /// <summary>
        /// Number of kernel calls until next gc
        /// </summary>
        public int gcCountDown;

        public MessageState _msgState;

        public ushort _memorySegmentSize;
        public byte[] _memorySegment = new byte[MemorySegmentMax];

        public VideoState _videoState;
        public ushort _vmdPalStart, _vmdPalEnd;
        public bool _syncedAudioOptions;

        public ushort _palCycleToColor;

        public ushort CurrentRoomNumber
        {
            get
            {
                return variables[Vm.VAR_GLOBAL][13].ToUInt16();
            }
        }

        public EngineState(SegManager segMan)
        {
            _segMan = segMan;
            _videoState = new VideoState();
            _fileHandles = new FileHandle[0];
            _executionStack = new List<ExecStack>();

            Reset(false);
        }

		public void SaveLoadWithSerializer (Serializer ser)
		{
			throw new NotImplementedException ();
		}

        public void SpeedThrottler(int neededSleep)
        {
            if (_throttleTrigger)
            {
                var curTime = Environment.TickCount;
                var duration = curTime - _throttleLastTime;

                if (duration < neededSleep)
                {
                    ServiceLocator.Platform.Sleep(neededSleep - duration);
                    _throttleLastTime = Environment.TickCount;
                }
                else {
                    _throttleLastTime = curTime;
                }
                _throttleTrigger = false;
            }
        }

        /// <summary>
        /// Resets the engine state.
        /// </summary>
        /// <param name="isRestoring"></param>
        private void Reset(bool isRestoring)
        {
            if (!isRestoring)
            {
                _memorySegmentSize = 0;
                _fileHandles = new FileHandle[5];
                for (int i = 0; i < 5; i++)
                {
                    _fileHandles[i] = new FileHandle();
                }
                abortScriptProcessing = AbortGameState.None;
            }

            executionStackBase = 0;
            _executionStackPosChanged = false;
            stack_base = StackPtr.Null;
            stack_top = StackPtr.Null;

            r_acc = Register.NULL_REG;
            r_prev = Register.NULL_REG;
            r_rest = 0;

            lastWaitTime = 0;

            gcCountDown = 0;

            _throttleCounter = 0;
            _throttleLastTime = 0;
            _throttleTrigger = false;
            _gameIsBenchmarking = false;

            _lastSaveVirtualId = SAVEGAMEID_OFFICIALRANGE_START;
            _lastSaveNewId = 0;

            _chosenQfGImportItem = 0;

            _cursorWorkaroundActive = false;

            scriptStepCounter = 0;
            scriptGCInterval = Vm.GC_INTERVAL;

            _videoState.Reset();
            _syncedAudioOptions = false;

            _vmdPalStart = 0;
            _vmdPalEnd = 256;

            _palCycleToColor = 255;
        }

        public void InitGlobals()
        {
            Script script_000 = _segMan.GetScript(1);

            if (script_000.LocalsCount == 0)
                throw new InvalidOperationException("Script 0 has no locals block");

            variablesSegment[Vm.VAR_GLOBAL] = script_000.LocalsSegment;
            variablesBase[Vm.VAR_GLOBAL] = script_000.LocalsBegin;
            variables[Vm.VAR_GLOBAL] = script_000.LocalsBegin;
            variablesMax[Vm.VAR_GLOBAL] = script_000.LocalsCount;
        }

        public void Wait(int ticks)
        {
            var time = Environment.TickCount;
            r_acc = Register.Make(0, (ushort)((((long)time) - ((long)lastWaitTime)) * 60 / 1000));
            lastWaitTime = time;

            ticks *= g_debug_sleeptime_factor;
            SciEngine.Instance.Sleep(ticks * 1000 / 60);
        }

        /// <summary>
        /// Shrink execution stack to size.
        /// Contains an assert if it is not already smaller.
        /// </summary>
        public void ShrinkStackToBase()
        {
            if (_executionStack.Count > 0)
            {
                int index = executionStackBase + 1;
                var count = _executionStack.Count - index;
                _executionStack.RemoveRange(index, count);
            }
        }
    }
}
