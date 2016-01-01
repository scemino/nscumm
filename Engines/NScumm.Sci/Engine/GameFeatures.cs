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
using NScumm.Core.Common;

namespace NScumm.Sci.Engine
{
    internal enum MoveCountType
    {
        Uninitialized,
        Ignore,
        Increment
    }

    internal class GameFeatures
    {
        private SegManager _segMan;
        private Kernel _kernel;

        private SciVersion _doSoundType, _setCursorType, _lofsType, _gfxFunctionsType, _messageFunctionType;

        private MoveCountType _moveCountType;
        private bool _usesCdTrack;
        private bool _forceDOSTracks;

        public GameFeatures(SegManager segMan, Kernel kernel)
        {
            _segMan = segMan;
            _kernel = kernel;

            _setCursorType = SciVersion.NONE;
            _doSoundType = SciVersion.NONE;
            _lofsType = SciVersion.NONE;
            _gfxFunctionsType = SciVersion.NONE;
            _messageFunctionType = SciVersion.NONE;
            _moveCountType = MoveCountType.Uninitialized;
#if ENABLE_SCI32
            _sci21KernelType = SciVersion.NONE;
            _sci2StringFunctionType = kSci2StringFunctionUninitialized;
#endif
            // TODO:
            //_usesCdTrack = Common::File::exists("cdaudio.map");
            //if (!ConfMan.getBool("use_cdaudio"))
            //    _usesCdTrack = false;
            _forceDOSTracks = false;
        }

        public SciVersion DetectLofsType()
        {
            if (_lofsType == SciVersion.NONE)
            {
                // This detection only works (and is only needed) for SCI 1
                if (ResourceManager.GetSciVersion() <= SciVersion.V01)
                {
                    _lofsType = SciVersion.V0_EARLY;
                    return _lofsType;
                }

                if (ResourceManager.GetSciVersion() >= SciVersion.V1_1 && ResourceManager.GetSciVersion() <= SciVersion.V2_1)
                {
                    // SCI1.1 type, i.e. we compensate for the fact that the heap is attached
                    // to the end of the script
                    _lofsType = SciVersion.V1_1;
                    return _lofsType;
                }

                if (ResourceManager.GetSciVersion() == SciVersion.V3)
                {
                    // SCI3 type, same as pre-SCI1.1, really, as there is no separate heap
                    // resource
                    _lofsType = SciVersion.V3;
                    return _lofsType;
                }

                // Find a function of the "Game" object (which is the game super class) which invokes lofsa/lofss
                SciObject gameObject = _segMan.GetObject(SciEngine.Instance.GameObject);
                SciObject gameSuperObject = _segMan.GetObject(gameObject.SuperClassSelector);
                bool found = false;
                if (gameSuperObject != null)
                {
                    string gameSuperClassName = _segMan.GetObjectName(gameObject.SuperClassSelector);

                    for (int m = 0; m < gameSuperObject.MethodCount; m++)
                    {
                        found = AutoDetectLofsType(gameSuperClassName, m);
                        if (found)
                            break;
                    }
                }
                else {
                    // TODO: warning("detectLofsType(): Could not find superclass of game object");
                }

                if (!found)
                {
                    // TODO: warning("detectLofsType(): failed, taking an educated guess");

                    if (ResourceManager.GetSciVersion() >= SciVersion.V1_MIDDLE)
                        _lofsType = SciVersion.V1_MIDDLE;
                    else
                        _lofsType = SciVersion.V0_EARLY;
                }

                // TODO: debugC(1, kDebugLevelVM, "Detected Lofs type: %s", getSciVersionDesc(_lofsType));
            }

            return _lofsType;
        }



        private bool AutoDetectLofsType(string gameSuperClassName, int methodNum)
        {
            // Look up the script address
            Register addr = GetDetectionAddr(gameSuperClassName, -1, methodNum);

            if (addr.Segment == 0)
                return false;

            int offset = (ushort)addr.Offset;
            Script script = _segMan.GetScript(addr.Segment);

            while (true)
            {
                short[] opparams = new short[4];
                byte extOpcode;
                byte opcode;
                offset += Vm.ReadPMachineInstruction(script.GetBuf(offset), out extOpcode, opparams);
                opcode = (byte)(extOpcode >> 1);

                // Check for end of script
                if (opcode == Vm.op_ret || offset >= script.BufSize)
                    break;

                if (opcode == Vm.op_lofsa || opcode == Vm.op_lofss)
                {
                    // Load lofs operand
                    ushort lofs = (ushort)opparams[0];

                    // Check for going out of bounds when interpreting as abs/rel
                    if (lofs >= script.BufSize)
                        _lofsType = SciVersion.V0_EARLY;

                    if ((short)offset + (short)lofs < 0)
                        _lofsType = SciVersion.V1_MIDDLE;

                    if ((short)offset + (short)lofs >= script.BufSize)
                        _lofsType = SciVersion.V1_MIDDLE;

                    if (_lofsType != SciVersion.NONE)
                        return true;

                    // If we reach here, we haven't been able to deduce the lofs
                    // parameter type so far.
                }
            }

            return false;   // not found
        }

        

        private Register GetDetectionAddr(string objName, int slc, int methodNum)
        {
            // Get address of target object
            Register objAddr = _segMan.FindObjectByName(objName, 0);
            Register addr;

            if (objAddr.IsNull)
            {
                throw new InvalidOperationException($"getDetectionAddr: {objName} object couldn't be found");
            }

            if (methodNum == -1)
            {
                if (SciEngine.LookupSelector(_segMan, objAddr, slc, null, out addr) != SelectorType.Method)
                {
                    throw new InvalidOperationException($"getDetectionAddr: target selector is not a method of object {objName}");
                }
            }
            else {
                addr = _segMan.GetObject(objAddr).GetFunction(methodNum);
            }

            return addr;
        }

        public SciVersion DetectMessageFunctionType()
        {
            throw new NotImplementedException();
        }

        public bool UsesOldGfxFunctions()
        {
            return DetectGfxFunctionsType() == SciVersion.V0_EARLY;
        }

        private SciVersion DetectGfxFunctionsType()
        {
            if (_gfxFunctionsType == SciVersion.NONE)
            {
                if (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY)
                {
                    // Old SCI0 games always used old graphics functions
                    _gfxFunctionsType = SciVersion.V0_EARLY;
                }
                else if (ResourceManager.GetSciVersion() >= SciVersion.V01)
                {
                    // SCI01 and newer games always used new graphics functions
                    _gfxFunctionsType = SciVersion.V0_LATE;
                }
                else {  // SCI0 late
                        // Check if the game is using an overlay
                    bool searchRoomObj = false;
                    Register rmObjAddr = _segMan.FindObjectByName("Rm");

                    if (SciEngine.Selector(s => s.overlay) != -1)
                    {
                        // The game has an overlay selector, check how it calls kDrawPic
                        // to determine the graphics functions type used
                        Register tmp;
                        if (SciEngine.LookupSelector(_segMan, rmObjAddr, SciEngine.Selector(s => s.overlay), null, out tmp) == SelectorType.Method)
                        {
                            if (!AutoDetectGfxFunctionsType())
                            {
                                // TODO: warning("Graphics functions detection failed, taking an educated guess");

                                // Try detecting the graphics function types from the
                                // existence of the motionCue selector (which is a bit
                                // of a hack)
                                if (_kernel.FindSelector("motionCue") != -1)
                                    _gfxFunctionsType = SciVersion.V0_LATE;
                                else
                                    _gfxFunctionsType = SciVersion.V0_EARLY;
                            }
                        }
                        else {
                            // The game has an overlay selector, but it's not a method
                            // of the Rm object (like in Hoyle 1 and 2), so search for
                            // other methods
                            searchRoomObj = true;
                        }
                    }
                    else {
                        // The game doesn't have an overlay selector, so search for it
                        // manually
                        searchRoomObj = true;
                    }

                    if (searchRoomObj)
                    {
                        // If requested, check if any method of the Rm object is calling
                        // kDrawPic, as the overlay selector might be missing in demos
                        bool found = false;

                        SciObject obj = _segMan.GetObject(rmObjAddr);
                        for (var m = 0; m < obj.MethodCount; m++)
                        {
                            found = AutoDetectGfxFunctionsType(m);
                            if (found)
                                break;
                        }

                        if (!found)
                        {
                            // No method of the Rm object is calling kDrawPic, thus the
                            // game doesn't have overlays and is using older graphics
                            // functions
                            _gfxFunctionsType = SciVersion.V0_EARLY;
                        }
                    }
                }

                // TODO: debugC(1, kDebugLevelVM, "Detected graphics functions type: %s", getSciVersionDesc(_gfxFunctionsType));
            }

            return _gfxFunctionsType;
        }

        private bool AutoDetectGfxFunctionsType(int methodNum = -1)
        {
            // Look up the script address
            Register addr = GetDetectionAddr("Rm", SciEngine.Selector(s => s.overlay), methodNum);

            if (addr.Segment == 0)
                return false;

            var offset = (int)addr.Offset;
            Script script = _segMan.GetScript(addr.Segment);

            while (true)
            {
                short[] opparams = new short[4];
                byte extOpcode;
                byte opcode;
                offset += Vm.ReadPMachineInstruction(script.GetBuf((ushort)offset), out extOpcode, opparams);
                opcode = (byte)(extOpcode >> 1);

                // Check for end of script
                if (opcode == Vm.op_ret || offset >= script.BufSize)
                    break;

                if (opcode == Vm.op_callk)
                {
                    ushort kFuncNum = (ushort)opparams[0];
                    ushort argc = (ushort)opparams[1];

                    if (kFuncNum == 8)
                    {   // kDrawPic	(SCI0 - SCI11)
                        // If kDrawPic is called with 6 parameters from the overlay
                        // selector, the game is using old graphics functions.
                        // Otherwise, if it's called with 8 parameters (e.g. SQ3) or 4 parameters
                        // (e.g. Hoyle 1/2), it's using new graphics functions.
                        _gfxFunctionsType = (argc == 6) ? SciVersion.V0_EARLY : SciVersion.V0_LATE;
                        return true;
                    }
                }
            }

            return false;   // not found
        }
    }
}
