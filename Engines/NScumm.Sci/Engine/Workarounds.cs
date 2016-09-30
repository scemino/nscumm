
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
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    internal enum ScriptPatcherSelectors
    {
        SELECTOR_cycles = 0,
        SELECTOR_seconds,
        SELECTOR_init,
        SELECTOR_dispose,
        SELECTOR_new,
        SELECTOR_curEvent,
        SELECTOR_disable,
        SELECTOR_doit,
        SELECTOR_show,
        SELECTOR_x,
        SELECTOR_cel,
        SELECTOR_setMotion,
        SELECTOR_overlay,
        SELECTOR_deskSarg,
        SELECTOR_localize,
        SELECTOR_put,
        SELECTOR_say,
        SELECTOR_contains,
        SELECTOR_solvePuzzle,
        SELECTOR_timesShownID,
        SELECTOR_startText,
        SELECTOR_startAudio,
        SELECTOR_modNum,
        SELECTOR_cycler,
        SELECTOR_setLoop
    }

    internal enum SciWorkaroundType
    {
        NONE,      // only used by terminator or when no workaround was fomethodName = und
        IGNORE,    // ignore kernel call
        STILLCALL, // still do kernel call
        FAKE       // fake kernel call / replace temp value / fake opcode
    }

    internal class SciTrackOriginReply
    {
        public int scriptNr;
        public string objectName;
        public string methodName;
        public int localCallOffset;
    }

    internal struct SciWorkaroundSolution
    {
        public SciWorkaroundType type;
        public ushort value;
    }

    /// <summary>
    /// A structure describing a 'workaround' for a SCI script bug.
    ///
    /// Arrays of SciWorkaroundEntry instances are terminated by
    /// a fake entry in which "objectName" is null.
    // </summary>
    internal class SciWorkaroundEntry
    {
        public SciGameId gameId;
        public int roomNr;
        public int scriptNr;
        public short inheritanceLevel;
        public string objectName;
        public string methodName;
        public ushort[] localCallSignature;
        public int index;
        public SciWorkaroundSolution newValue;
    }

    internal static class Workarounds
    {
        public const int PATCH_END = SIG_END;
        public const int PATCH_COMMANDMASK = SIG_COMMANDMASK;
        public const int PATCH_VALUEMASK = SIG_VALUEMASK;
        public const int PATCH_BYTEMASK = SIG_BYTEMASK;
        public const int PATCH_CODE_ADDTOOFFSET = SIG_CODE_ADDTOOFFSET;
        public static ushort PATCH_ADDTOOFFSET(int offset)
        {
            return (ushort)(SIG_CODE_ADDTOOFFSET | offset);
        }
        public const ushort PATCH_CODE_GETORIGINALBYTE = 0xD000;

        public static ushort PATCH_GETORIGINALBYTE(int offset)
        {
            return (ushort)(PATCH_CODE_GETORIGINALBYTE | offset);
        }

        public const ushort PATCH_CODE_GETORIGINALBYTEADJUST = 0xC000;
        public static ushort PATCH_GETORIGINALBYTEADJUST_1(int offset)
        {
            return (ushort)(PATCH_CODE_GETORIGINALBYTEADJUST | offset);
        }
        public static ushort PATCH_GETORIGINALBYTEADJUST_2(short adjustValue)
        {
            return (ushort)adjustValue;
        }

        public const ushort PATCH_CODE_SELECTOR16 = SIG_CODE_SELECTOR16;

        public static ushort PATCH_SELECTOR16(ScriptPatcherSelectors selectorID)
        {
            return (ushort)(SIG_CODE_SELECTOR16 | (ushort)selectorID);
        }

        public const ushort PATCH_CODE_SELECTOR8 = SIG_CODE_SELECTOR8;

        public static ushort PATCH_SELECTOR8(ScriptPatcherSelectors selector)
        {
            return (ushort)(SIG_CODE_SELECTOR8 | (ushort)selector);
        }

        public const ushort PATCH_CODE_UINT16 = SIG_CODE_UINT16;

        public static ushort PATCH_UINT16_1(int value)
        {
            return (ushort)(SIG_CODE_UINT16 | (value & 0xFF));
        }

        public static ushort PATCH_UINT16_2(int value)
        {
            return ((ushort)(value >> 8));
        }

        public const int PATCH_CODE_BYTE = SIG_CODE_BYTE;

        public const int SAVEGAMEID_OFFICIALRANGE_START = 100;

        public static SciWorkaroundSolution TrackOriginAndFindWorkaround(int index, SciWorkaroundEntry[] workaroundList, out SciTrackOriginReply trackOrigin)
        {
            trackOrigin = null;
            // HACK for SCI3: Temporarily ignore this
            if (ResourceManager.GetSciVersion() == SciVersion.V3)
            {
                Warning("SCI3 HACK: trackOriginAndFindWorkaround() called, ignoring");
                var sci3IgnoreForNow = new SciWorkaroundSolution();
                sci3IgnoreForNow.type = SciWorkaroundType.FAKE;
                sci3IgnoreForNow.value = 0;
                return sci3IgnoreForNow;
            }

            EngineState state = SciEngine.Instance.EngineState;
            ExecStack lastCall = state.xs;
            Script localScript = state._segMan.GetScriptIfLoaded(lastCall.local_segment);
            int curScriptNr = localScript.ScriptNumber;
            int curLocalCallOffset = lastCall.debugLocalCallOffset;

            if (curLocalCallOffset != -1)
            {
                // if lastcall was actually a local call search back for a real call
                for (int i = state._executionStack.Count - 1; i >= 0; i--)
                {
                    var loopCall = state._executionStack[i];
                    if ((loopCall.debugSelector != -1) || (loopCall.debugExportId != -1))
                    {
                        lastCall.debugSelector = loopCall.debugSelector;
                        lastCall.debugExportId = loopCall.debugExportId;
                        break;
                    }
                }
            }

            var curObjectName = state._segMan.GetObjectName(lastCall.sendp);
            string curMethodName = null;
            SciGameId gameId = SciEngine.Instance.GameId;
            int curRoomNumber = state.CurrentRoomNumber;

            if (lastCall.type == ExecStackType.CALL)
            {
                if (lastCall.debugSelector != -1)
                {
                    curMethodName = SciEngine.Instance.Kernel.GetSelectorName(lastCall.debugSelector);
                }
                else if (lastCall.debugExportId != -1)
                {
                    curObjectName = "";
                    curMethodName = $"export {lastCall.debugExportId}";
                }
            }

            if (workaroundList != null)
            {
                // Search if there is a workaround for this one
                SciWorkaroundEntry workaround;
                short inheritanceLevel = 0;
                string searchObjectName = SciEngine.Instance.GetSciLanguageString(curObjectName, Language.English);
                Register searchObject = lastCall.sendp;
                ByteAccess curScriptPtr = null;
                uint curScriptSize = 0;
                bool matched = false;
                int i = 0;

                do
                {
                    workaround = workaroundList[i++];
                    while (workaround.methodName != null)
                    {
                        bool objectNameMatches = (workaround.objectName == null) ||
                                                 (workaround.objectName == searchObjectName);

                        if (workaround.gameId == gameId
                                && ((workaround.scriptNr == -1) || (workaround.scriptNr == curScriptNr))
                                && ((workaround.roomNr == -1) || (workaround.roomNr == curRoomNumber))
                                && ((workaround.inheritanceLevel == -1) || (workaround.inheritanceLevel == inheritanceLevel))
                                && objectNameMatches
                            && workaround.methodName == SciEngine.Instance.GetSciLanguageString(curMethodName, Language.English)
                                && ((workaround.index == -1) || (workaround.index == index)))
                        {
                            // Workaround found
                            if ((workaround.localCallSignature != null) || (curLocalCallOffset >= 0))
                            {
                                // local call signature found and/or subcall was made
                                if ((workaround.localCallSignature != null) && (curLocalCallOffset >= 0))
                                {
                                    // local call signature found and subcall was made . check signature accordingly
                                    if (curScriptPtr == null)
                                    {
                                        // get script data
                                        int segmentId = SciEngine.Instance.EngineState._segMan.GetScriptSegment(curScriptNr);
                                        SegmentObj segmentObj = null;
                                        if (segmentId != 0)
                                        {
                                            segmentObj = SciEngine.Instance.EngineState._segMan.GetScriptIfLoaded((ushort)segmentId);
                                        }
                                        if (segmentObj == null)
                                        {
                                            workaround = workaroundList[i++];
                                            continue;
                                        }
                                        Script scriptObj = (Script)segmentObj;
                                        curScriptPtr = scriptObj.GetBuf();
                                        curScriptSize = scriptObj.ScriptSize;
                                    }

                                    // now actually check for signature match
                                    if (SciEngine.Instance.ScriptPatcher.VerifySignature((uint)curLocalCallOffset, workaround.localCallSignature, "workaround signature", curScriptPtr, curScriptSize))
                                    {
                                        matched = true;
                                    }

                                }
                                else
                                {
                                    // mismatch, so workaround doesn't match
                                    workaround = workaroundList[i++];
                                    continue;
                                }
                            }
                            else
                            {
                                // no localcalls involved . workaround matches
                                matched = true;
                            }
                            if (matched)
                            {
                                DebugC(DebugLevels.Workarounds, "Workaround: '{0}:{1}' in script {2}, localcall {3:X}", workaround.objectName, workaround.methodName, curScriptNr, curLocalCallOffset);
                                trackOrigin = null;
                                return workaround.newValue;
                            }
                        }
                        workaround = workaroundList[i++];
                    }

                    // Go back to the parent
                    inheritanceLevel++;
                    searchObject = state._segMan.GetObject(searchObject).SuperClassSelector;
                    if (!searchObject.IsNull)
                        searchObjectName = state._segMan.GetObjectName(searchObject);
                } while (!searchObject.IsNull); // no parent left?
            }

            // give caller origin data
            trackOrigin = new SciTrackOriginReply();
            trackOrigin.objectName = curObjectName;
            trackOrigin.methodName = curMethodName;
            trackOrigin.scriptNr = curScriptNr;
            trackOrigin.localCallOffset = lastCall.debugLocalCallOffset;

            var noneFound = new SciWorkaroundSolution();
            noneFound.type = SciWorkaroundType.NONE;
            noneFound.value = 0;
            return noneFound;
        }
        //    // HACK for SCI3: Temporarily ignore this
        //    if (ResourceManager.GetSciVersion() == SciVersion.V3)
        //    {
        //        Warning("SCI3 HACK: trackOriginAndFindWorkaround() called, ignoring");
        //        SciWorkaroundSolution sci3IgnoreForNow = new SciWorkaroundSolution
        //        {
        //            type = SciWorkaroundType.FAKE,
        //            value = 0
        //        };
        //        return sci3IgnoreForNow;
        //    }

        //    EngineState state = SciEngine.Instance.EngineState;
        //    ExecStack lastCall = state.xs;
        //    Script localScript = state._segMan.GetScriptIfLoaded(lastCall.local_segment);
        //    int curScriptNr = localScript.ScriptNumber;

        //    if (lastCall.debugLocalCallOffset != -1)
        //    {
        //        // if lastcall was actually a local call search back for a real call
        //        for (int i = state._executionStack.Count - 1; i >= 0; i--)
        //        {
        //            ExecStack loopCall = state._executionStack[i];
        //            if ((loopCall.debugSelector != -1) || (loopCall.debugExportId != -1))
        //            {
        //                lastCall.debugSelector = loopCall.debugSelector;
        //                lastCall.debugExportId = loopCall.debugExportId;
        //                break;
        //            }
        //        }
        //    }

        //    string curObjectName = state._segMan.GetObjectName(lastCall.sendp);
        //    string curMethodName = null;
        //    SciGameId gameId = SciEngine.Instance.GameId;
        //    int curRoomNumber = state.CurrentRoomNumber;

        //    if (lastCall.type == ExecStackType.CALL)
        //    {
        //        if (lastCall.debugSelector != -1)
        //        {
        //            curMethodName = SciEngine.Instance.Kernel.GetSelectorName(lastCall.debugSelector);
        //        }
        //        else if (lastCall.debugExportId != -1)
        //        {
        //            curObjectName = "";
        //            curMethodName = $"export {lastCall.debugExportId}";
        //        }
        //    }

        //    if (workaroundList != null)
        //    {
        //        // Search if there is a workaround for this one
        //        short inheritanceLevel = 0;
        //        string searchObjectName = curObjectName;
        //        Register searchObject = lastCall.sendp;
        //        do
        //        {
        //            foreach (var workaround in workaroundList)
        //            {
        //                bool objectNameMatches = (workaround.objectName == null) ||
        //                                         (workaround.objectName == SciEngine.Instance.GetSciLanguageString(searchObjectName, Language.ENGLISH));

        //                // Special case: in the fanmade Russian translation of SQ4, all
        //                // of the object names have been deleted or renamed to Russian,
        //                // thus we disable checking of the object name. Fixes bug #5573.
        //                if (SciEngine.Instance.Language == Core.Language.RU_RUS && SciEngine.Instance.GameId == SciGameId.SQ4)
        //                    objectNameMatches = true;

        //                if (workaround.gameId == gameId
        //                        && ((workaround.scriptNr == -1) || (workaround.scriptNr == curScriptNr))
        //                        && ((workaround.roomNr == -1) || (workaround.roomNr == curRoomNumber))
        //                        && ((workaround.inheritanceLevel == -1) || (workaround.inheritanceLevel == inheritanceLevel))
        //                        && objectNameMatches
        //                        && workaround.methodName == SciEngine.Instance.GetSciLanguageString(curMethodName, Language.ENGLISH)
        //                        && ((workaround.index == -1) || (workaround.index == index)))
        //                {
        //                    // Workaround found
        //                    return workaround.newValue;
        //                }
        //            }

        //            // Go back to the parent
        //            inheritanceLevel++;
        //            searchObject = state._segMan.GetObject(searchObject).SuperClassSelector;
        //            if (!searchObject.IsNull)
        //                searchObjectName = state._segMan.GetObjectName(searchObject);
        //        } while (!searchObject.IsNull); // no parent left?
        //    }

        //    // give caller origin data
        //    trackOrigin = new SciTrackOriginReply
        //    {
        //        objectName = curObjectName,
        //        methodName = curMethodName,
        //        scriptNr = curScriptNr,
        //        localCallOffset = lastCall.debugLocalCallOffset
        //    };

        //    SciWorkaroundSolution noneFound = new SciWorkaroundSolution
        //    {
        //        type = SciWorkaroundType.NONE,
        //        value = 0
        //    };
        //    return noneFound;
        //}

        //    gameID,           room,script,lvl,          object-name, method-name,    call,index,             workaround
        public static readonly SciWorkaroundEntry[] ArithmeticWorkarounds = {
            new SciWorkaroundEntry { gameId = SciGameId.CAMELOT,       roomNr =  92, scriptNr =   92, inheritanceLevel =  0,objectName=   "endingCartoon2",methodName= "changeState",localCallSignature=sig_arithmetic_camelot_1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value = 0 } }, // op_lai: during the ending, sub gets called with no parameters, uses parameter 1 which is theGrail in this case - bug #5237
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST2,     roomNr = 100, scriptNr =   0,  inheritanceLevel =0,  objectName=             "Rain",methodName= "points",     localCallSignature=sig_arithmetic_ecoq2_1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_or: when giving the papers to the customs officer, gets called against a pointer instead of a number - bug #4939
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE,       roomNr = 516, scriptNr = 983,  inheritanceLevel =0,  objectName=           "Wander",methodName= "setTarget",  index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_mul: The Legend of the Lost Jewel Demo (fan made): called with object as second parameter when attacked by insects - bug #5124
            new SciWorkaroundEntry { gameId = SciGameId.GK1,           roomNr = 800,scriptNr =64992,  inheritanceLevel =0,  objectName=              "Fwd",methodName= "doit",       index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   1 } }, // op_gt: when Mosely finds Gabriel and Grace near the end of the game, compares the Grooper object with 7
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,        roomNr = 700,scriptNr =   -1,  inheritanceLevel =1,  objectName=             "Code",methodName= "doit",       index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   1 } }, // op_add: while bidding in Bridge, an object ("Bid") is added to an object in another segment ("hand3")
            new SciWorkaroundEntry { gameId = SciGameId.ICEMAN,        roomNr = 199,scriptNr =  977,  inheritanceLevel =0,  objectName=          "Grooper",methodName= "doit",       index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_add: While dancing with the girl
            new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSE256,roomNr =  -1,scriptNr =  999,  inheritanceLevel =0,  objectName=            "Event",methodName= "new",        index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_and: constantly during the game (SCI1 version)
            new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSE256,roomNr =  -1,scriptNr =    4,  inheritanceLevel =0,  objectName=            "rm004",methodName= "doit",       index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_or: when going north and reaching the castle (rooms 4 and 37) - bug #5101
            new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSEHIRES,roomNr =90,scriptNr =   90,  inheritanceLevel =0,  objectName=    "newGameButton",methodName= "select",     index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_ge: MUMG Deluxe, when selecting "New Game" in the main menu. It tries to compare an integer with a list. Needs to return false for the game to continue.
            new SciWorkaroundEntry { gameId = SciGameId.PHANTASMAGORIA,roomNr = 902,scriptNr =    0,  inheritanceLevel =0,  objectName=                 "",methodName= "export 7",   index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_shr: when starting a chapter in Phantasmagoria
            new SciWorkaroundEntry { gameId = SciGameId.QFG1VGA,       roomNr = 301,scriptNr =  928,  inheritanceLevel =0,  objectName=            "Blink",methodName= "init",       index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_div: when entering the inn, gets called with 1 parameter, but 2nd parameter is used for div which happens to be an object
            new SciWorkaroundEntry { gameId = SciGameId.QFG2,          roomNr = 200,scriptNr =  200,  inheritanceLevel =0,  objectName=            "astro",methodName= "messages",   index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_lsi: when getting asked for your name by the astrologer - bug #5152
            new SciWorkaroundEntry { gameId = SciGameId.QFG3,          roomNr = 780,scriptNr =  999,  inheritanceLevel =0,  objectName=                 "",methodName= "export 6",   index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_add: trying to talk to yourself at the top of the giant tree - bug #6692
            new SciWorkaroundEntry { gameId = SciGameId.QFG4,          roomNr = 710,scriptNr =64941,  inheritanceLevel =0,  objectName=        "RandCycle",methodName= "doit",       index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   1 } }, // op_gt: when the tentacle appears in the third room of the caves
        };

        // Attention:
        //  To identify local-call-subroutines code signatures are used.
        //   Offsets change a lot between different versions of games, especially between different language releases.
        //   That's why it isn't good to hardcode the offsets of those subroutines.
        //
        //  Those signatures are just like the script patcher signatures (for further study: engine\script_patches.cpp)
        //   However you may NOT use command SIG_SELECTOR8 nor SIG_SELECTOR16 atm. Proper support for those may be added later.

        //                Game: Conquests of Camelot
        //      Calling method: endingCartoon2::changeState
        //   Subroutine offset: English 0x020d (script 92)
        // Applies to at least: English PC floppy
        private static readonly ushort[] sig_arithmetic_camelot_1 = {
            0x83, 0x32,                      // lal local[32h]
            0x30, SIG_UINT16_1(0x001d), SIG_UINT16_2(0x001d),        // bnt [...]
            0x7a,                            // push2
            0x39, 0x08,                      // pushi 08
            0x36,                            // push
            0x43,                            // callk Graph
            SIG_END
        };

        //                Game: Eco Quest 2
        //      Calling method: Rain::points
        //   Subroutine offset: English 0x0cc6, French/Spanish 0x0ce0 (script 0)
        // Applies to at least: English/French/Spanish PC floppy
        private static readonly ushort[] sig_arithmetic_ecoq2_1 = {
            0x8f, 0x01,                      // lsp param[1]
            0x35, 0x10,                      // ldi 10h
            0x08,                            // div
            0x99, 0x6e,                      // lsgi global[6Eh]
            0x38, SIG_UINT16_1(0x8000), SIG_UINT16_2(0x8000),        // pushi 8000h
            0x8f, 0x01,                      // lsp param[1]
            0x35, 0x10,                      // ldi 10h
            0x0a,                            // mod
            0x0c,                            // shr
            0x14,                            // or
            0x36,                            // push
            SIG_END
        };

        //                Game: Island of Dr. Brain
        //      Calling method: upElevator::changeState, downElevator::changeState, correctElevator::changeState
        //   Subroutine offset: 0x201f (script 291)
        // Applies to at least: English PC floppy
        private static readonly ushort[] sig_kGraphSaveBox_ibrain_1 = {
            0x3f, 0x01,                      // link 01
            0x87, 0x01,                      // lap param[1]
            0x30, SIG_UINT16_1(0x0043), SIG_UINT16_2(0x0043),        // bnt [...]
            0x76,                            // push0
            SIG_END
        };

        //                Game: Space Quest 4
        //      Calling method: laserScript::changeState
        //   Subroutine offset: English/German/French/Russian PC floppy, Japanese PC-9801: 0x0016, English PC CD: 0x00b2 (script 150)
        // Applies to at least: English/German/French/Russian PC floppy, English PC CD, Japanese PC-9801
        private static readonly ushort[] sig_kGraphRedrawBox_sq4_1 = {
            0x3f, 0x07,                      // link 07
            0x39, SIG_ADDTOOFFSET(+1),       // pushi 2Ah for PC floppy, pushi 27h for PC CD
            0x76,                            // push0
            0x72,                            // lofsa laserSound
            SIG_END
        };

        //                Game: Police Quest 2
        //      Calling method: rm23Script::elements
        //   Subroutine offset: English 1.001.000: 0x04ae, English 1.002.011: 0x04ca, Japanese: 0x04eb (script 23)
        // Applies to at least: English PC floppy, Japanese PC-9801
        private static readonly ushort[] sig_kDisplay_pq2_1 = {
            0x35, 0x00,                      // ldi 00
            0xa3, 0x09,                      // sal local[9]
            0x35, 0x01,                      // ldi 01
            0xa3, 0x0a,                      // sal local[0Ah]
            0x38, SIG_ADDTOOFFSET(+2),       // pushi selector[drawPic] TODO: implement selectors
            0x7a,                            // push2
            0x39, 0x5a,                      // pushi 5Ah
            0x7a,                            // push2
            0x81, 0x02,                      // lag global[2]
            0x4a, 0x08,                      // send 08
            SIG_END
        };

        //                Game: Fan-Made games (SCI Studio)
        //      Calling method: Game::save, Game::restore
        //   Subroutine offset: Al Pond 2:          0x0e5c (ldi 0001)
        //                      Black Cauldron:     0x000a (ldi 01)
        //                      Cascade Quest:      0x0d1c (ldi 0001)
        //                      Demo Quest:         0x0e55 (ldi 0001)
        //                      I want my C64 back: 0x0e57 (ldi 0001)
        // Applies to at least: games listed above
        private static readonly ushort[] sig_kDeviceInfo_Fanmade_1 = {
            0x3f, 0x79,                      // link 79h
            0x34, SIG_UINT16_1(0x0001), SIG_UINT16_2(0x0001),        // ldi 0001
            0xa5, 0x00,                      // sat temp[0]
            SIG_END
        };

        private static readonly ushort[] sig_kDeviceInfo_Fanmade_2 = {
            0x3f, 0x79,                      // link 79h
            0x35, 0x01,                      // ldi 01
            0xa5, 0x00,                      // sat temp[0]
            SIG_END
        };

        //                Game: Island of Dr. Brain
        //      Calling method: childBreed::changeState
        //   Subroutine offset: 0x1c7c (script 310)
        // Applies to at least: English PC floppy
        private static readonly ushort[] sig_kStrAt_ibrain_1 = {
            0x3f, 0x16,                      // link 16
            0x78,                            // push1
            0x8f, 0x01,                      // lsp param[1]
            0x43,                            // callk StrLen
            SIG_END
        };

        //                Game: Quest for Glory 2
        //      Calling method: export 21 of script 2
        //   Subroutine offset: English 0x0deb (script 2)
        // Applies to at least: English PC floppy
        private static readonly ushort[] sig_kStrLen_qfg2_1 = {
            0x3f, 0x04,                      // link 04
            0x78,                            // push1
            0x8f, 0x02,                      // lsp param[2]
            0x43,                            // callk StrLen
            SIG_END
        };

        // Please do not use the #defines, that are called SIG_CODE_* / PATCH_CODE_* inside signature/patch-tables
        public const ushort SIG_END = 0xFFFF;
        public const ushort SIG_MISMATCH = 0xFFFE;
        public const ushort SIG_COMMANDMASK = 0xF000;
        public const ushort SIG_VALUEMASK = 0x0FFF;
        public const ushort SIG_BYTEMASK = 0x00FF;
        public const ushort SIG_MAGICDWORD = 0xF000;
        public const ushort SIG_CODE_ADDTOOFFSET = 0xE000;
        public static ushort SIG_ADDTOOFFSET(ushort offset) { return (ushort)(SIG_CODE_ADDTOOFFSET | offset); }
        public const ushort SIG_CODE_SELECTOR16 = 0x9000;
        public static ushort SIG_SELECTOR16(ScriptPatcherSelectors selector)
        {
            return (ushort)(SIG_CODE_SELECTOR16 | (ushort)selector);
        }
        public const ushort SIG_CODE_SELECTOR8 = 0x8000;
        public static ushort SIG_SELECTOR8(ScriptPatcherSelectors selector)
        {
            return (ushort)(SIG_CODE_SELECTOR8 | (ushort)selector);
        }
        public const ushort SIG_CODE_UINT16 = 0x1000;
        public const ushort SIG_CODE_BYTE = 0x0000;

        public static ushort SIG_UINT16_1(ushort value)
        {
            return (ushort)(SIG_CODE_UINT16 | (value & 0xFF));
        }

        public static ushort SIG_UINT16_2(ushort value)
        {
            return ((ushort)(value >> 8));
        }

        public static ushort PATCH_UINT16_1(ushort value)
        {
            return (ushort)(SIG_CODE_UINT16 | (value & 0xFF));
        }

        public static ushort PATCH_UINT16_2(ushort value)
        {
            return (ushort)(value >> 8);
        }

        internal static SciWorkaroundSolution TrackOriginAndFindWorkaround(int v, object kDisplay_workarounds, out SciTrackOriginReply originReply)
        {
            throw new NotImplementedException();
        }

        public static readonly SciWorkaroundEntry[] kGraphDrawLine_workarounds = {
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN,roomNr =   300, scriptNr =   300, inheritanceLevel = 0, objectName =   "dudeViewer", methodName = "show",        index =   0, newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.STILLCALL, value = 0 } }, // when looking at the gene explanation chart, gets called with 1 extra parameter
            new SciWorkaroundEntry { gameId = SciGameId.SQ1,        roomNr =    43, scriptNr =    43, inheritanceLevel = 0, objectName =  "someoneDied", methodName = "changeState", index =   0, newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.STILLCALL, value = 0 } }, // when ordering beer, gets called with 1 extra parameter
            new SciWorkaroundEntry { gameId = SciGameId.SQ1,        roomNr =    71, scriptNr =    71, inheritanceLevel = 0, objectName = "destroyXenon", methodName = "changeState", index =   0, newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.STILLCALL, value = 0 } }, // during the Xenon destruction cutscene (which results in death), gets called with 1 extra parameter - bug #5176
            new SciWorkaroundEntry { gameId = SciGameId.SQ1,        roomNr =    53, scriptNr =    53, inheritanceLevel = 0, objectName =     "blastEgo", methodName = "changeState", index =   0, newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.STILLCALL, value = 0 } }, // when Roger is found and zapped by the cleaning robot, gets called with 1 extra parameter - bug #5177
        };

        public static readonly SciWorkaroundEntry[] kSetCursor_workarounds = {
            new SciWorkaroundEntry { gameId = SciGameId.KQ5, roomNr =-1,scriptNr = 768, inheritanceLevel = 0, objectName = "KQCursor", methodName = "init", index = 0, newValue = new SciWorkaroundSolution { type=SciWorkaroundType.STILLCALL,value = 0 } }, // CD: gets called with 4 additional "900d" parameters
        };

        public static readonly SciWorkaroundEntry[] kAbs_workarounds = {
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE1,  roomNr =  1, scriptNr =  1, inheritanceLevel = 0, objectName = "room1", methodName = "doit",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0x3e9 } }, // crazy eights - called with objects instead of integers
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE1,  roomNr =  2, scriptNr =  2, inheritanceLevel = 0, objectName = "room2", methodName = "doit",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0x3e9 } }, // old maid - called with objects instead of integers
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE1,  roomNr =  3, scriptNr =  3, inheritanceLevel = 0, objectName = "room3", methodName = "doit",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0x3e9 } }, // hearts - called with objects instead of integers
            new SciWorkaroundEntry { gameId = SciGameId.QFG1VGA, roomNr = -1, scriptNr = -1, inheritanceLevel = 0, objectName =    null, methodName = "doit",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0x3e9 } }, // when the game is patched with the NRS patch
            new SciWorkaroundEntry { gameId = SciGameId.QFG3   , roomNr = -1, scriptNr = -1, inheritanceLevel = 0, objectName =    null, methodName = "doit",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0x3e9 } }, // when the game is patched with the NRS patch - bugs #6042, #6043
        };

        public static readonly SciWorkaroundEntry[] kGraphSaveBox_workarounds = {
            new SciWorkaroundEntry { gameId = SciGameId.CASTLEBRAIN, roomNr =    420, scriptNr =   427, inheritanceLevel =  0, objectName =         "alienIcon", methodName = "select",      index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when selecting a card during the alien card game, gets called with 1 extra parameter
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN, roomNr =    290, scriptNr =   291, inheritanceLevel =  0, objectName =        "upElevator", methodName = "changeState", localCallSignature = sig_kGraphSaveBox_ibrain_1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when testing in the elevator puzzle, gets called with 1 argument less - 15 is on stack - bug #4943
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN, roomNr =    290, scriptNr =   291, inheritanceLevel =  0, objectName =      "downElevator", methodName = "changeState", localCallSignature = sig_kGraphSaveBox_ibrain_1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // see above
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN, roomNr =    290, scriptNr =   291, inheritanceLevel =  0, objectName =   "correctElevator", methodName = "changeState", localCallSignature = sig_kGraphSaveBox_ibrain_1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // see above (when testing the correct solution)
            new SciWorkaroundEntry { gameId = SciGameId.PQ3,         roomNr =    202, scriptNr =   202, inheritanceLevel =  0, objectName =           "MapEdit", methodName = "movePt",      index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when plotting crimes, gets called with 2 extra parameters - bug #5099
        };

        public static SciWorkaroundEntry[] kGraphRestoreBox_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.LSL6, roomNr = -1, scriptNr = 86, inheritanceLevel = 0, objectName = "LL6Inv", methodName = "hide",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // happens during the game, gets called with 1 extra parameter
        };


        public static readonly SciWorkaroundEntry[] kGraphFillBoxForeground_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.LSL6, roomNr = -1, scriptNr = 0, inheritanceLevel = 0, objectName = "LSL6", methodName = "hideControls",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // happens when giving the bungee key to merrily (room 240) and at least in room 650 too - gets called with additional 5th parameter
        };


        public static readonly SciWorkaroundEntry[] kGraphFillBoxAny_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST2, roomNr =    100, scriptNr =   333, inheritanceLevel =  0, objectName =       "showEcorder", methodName = "changeState", index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // necessary workaround for our ecorder script patch, because there isn't enough space to patch the function
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =     -1, scriptNr =   818, inheritanceLevel =  0, objectName =    "iconTextSwitch", methodName = "show",        index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // CD: game menu "text/speech" display - parameter 5 is missing, but the right color number is on the stack
        };

        public static readonly SciWorkaroundEntry[] kGraphUpdateBox_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST2, roomNr =    100, scriptNr =  333, inheritanceLevel =  0, objectName =       "showEcorder", methodName = "changeState", index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // necessary workaround for our ecorder script patch, because there isn't enough space to patch the function
            new SciWorkaroundEntry { gameId = SciGameId.PQ3,       roomNr =    202, scriptNr =  202, inheritanceLevel =  0, objectName =           "MapEdit", methodName = "addPt",       index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when plotting crimes, gets called with 2 extra parameters - bug #5099
            new SciWorkaroundEntry { gameId = SciGameId.PQ3,       roomNr =    202, scriptNr =  202, inheritanceLevel =  0, objectName =           "MapEdit", methodName = "movePt",      index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when plotting crimes, gets called with 2 extra parameters - bug #5099
            new SciWorkaroundEntry { gameId = SciGameId.PQ3,       roomNr =    202, scriptNr =  202, inheritanceLevel =  0, objectName =           "MapEdit", methodName = "dispose",     index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when plotting crimes, gets called with 2 extra parameters
        };

        public static readonly SciWorkaroundEntry[] kGraphRedrawBox_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    405, scriptNr =  405, inheritanceLevel = 0, objectName =      "swimAfterEgo", methodName = "changeState",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // skateOrama when "swimming" in the air - accidental additional parameter specified
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    406, scriptNr =  406, inheritanceLevel = 0, objectName =       "", methodName = "changeState",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // FLOPPY: when getting shot by the police - accidental additional parameter specified
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    406, scriptNr =  406, inheritanceLevel = 0, objectName =      "swimAndShoot", methodName = "changeState",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // skateOrama when "swimming" in the air - accidental additional parameter specified
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    410, scriptNr =  410, inheritanceLevel = 0, objectName =      "swimAfterEgo", methodName = "changeState",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // skateOrama when "swimming" in the air - accidental additional parameter specified
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    411, scriptNr =  411, inheritanceLevel = 0, objectName =      "swimAndShoot", methodName = "changeState",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // skateOrama when "swimming" in the air - accidental additional parameter specified
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    150, scriptNr =  150, inheritanceLevel = 0, objectName =       "laserScript", methodName = "changeState",  localCallSignature = sig_kGraphRedrawBox_sq4_1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when visiting the pedestral where Roger Jr. is trapped, before trashing the brain icon in the programming chapter, accidental additional parameter specified - bug #5479
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    150, scriptNr =  150, inheritanceLevel = 0, objectName =       "laserScript", methodName = "changeState",  localCallSignature = sig_kGraphRedrawBox_sq4_1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // same as above, for the German version - bug #5527
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =     -1, scriptNr =  704, inheritanceLevel = 0, objectName =          "shootEgo", methodName = "changeState",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // When shot by Droid in Super Computer Maze (Rooms 500, 505, 510...) - accidental additional parameter specified
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,       roomNr =     -1, scriptNr =  981, inheritanceLevel = 0, objectName =          "myWindow", methodName =     "dispose",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // Happens in the floppy version, when closing any dialog box, accidental additional parameter specified - bug #5031
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,       roomNr =     -1, scriptNr =  995, inheritanceLevel = 0, objectName =              "invW", methodName =        "doit",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // Happens in the floppy version, when closing the inventory window, accidental additional parameter specified
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,       roomNr =     -1, scriptNr =  995, inheritanceLevel = 0, objectName =                  "", methodName =    "export 0",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // Happens in the floppy version, when opening the gem pouch, accidental additional parameter specified - bug #5138
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,       roomNr =     -1, scriptNr =  403, inheritanceLevel = 0, objectName =         "KQ5Window", methodName =     "dispose",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // Happens in the FM Towns version when closing any dialog box, accidental additional parameter specified
        };

        public static readonly SciWorkaroundEntry[] kDoSoundFade_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,       roomNr =        213, scriptNr =   989, inheritanceLevel =  0, objectName =      "globalSound3", methodName = "fade",  index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // english floppy: when bandits leave the secret temple, parameter 4 is an object - bug #5078
            new SciWorkaroundEntry { gameId = SciGameId.KQ6,       roomNr =        105, scriptNr =   989, inheritanceLevel =  0, objectName =       "globalSound", methodName = "fade",  index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // floppy: during intro, parameter 4 is an object
            new SciWorkaroundEntry { gameId = SciGameId.KQ6,       roomNr =        460, scriptNr =   989, inheritanceLevel =  0, objectName =      "globalSound2", methodName = "fade",  index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // after pulling the black widow's web on the isle of wonder, parameter 4 is an object - bug #4954
            new SciWorkaroundEntry { gameId = SciGameId.QFG4,      roomNr =         -1, scriptNr = 64989, inheritanceLevel =  0, objectName =          "longSong", methodName = "fade",  index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // CD version: many places, parameter 4 is an object (longSong)
            new SciWorkaroundEntry { gameId = SciGameId.SQ5,       roomNr =        800, scriptNr =   989, inheritanceLevel =  0, objectName =         "sq5Music1", methodName = "fade",  index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when cutting the wrong part of Goliath with the laser - bug #6341
        };

        public static readonly SciWorkaroundEntry[] kFindKey_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST2, roomNr =     100, scriptNr =  999, inheritanceLevel = 0, objectName =           "myList", methodName = "contains", index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0 } }, // When Noah Greene gives Adam the Ecorder, and just before the game gives a demonstration, a null reference to a list is passed - bug #4987
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,    roomNr =     300, scriptNr =  999, inheritanceLevel = 0, objectName =            "Piles", methodName = "contains", index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0 } }, // When passing the three cards in Hearts, a null reference to a list is passed - bug #5664
        };

        public static readonly SciWorkaroundEntry[] kDisposeScript_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.LAURABOW, roomNr =     777, scriptNr =  777, inheritanceLevel = 0, objectName =             "myStab", methodName = "changeState", index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // DEMO: after the will is signed, parameter 0 is an object - bug #4967
            new SciWorkaroundEntry { gameId = SciGameId.QFG1,     roomNr =      -1, scriptNr =   64, inheritanceLevel = 0, objectName =              "rm64", methodName = "dispose",      index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when leaving graveyard, parameter 0 is an object
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,      roomNr =     150, scriptNr =  151, inheritanceLevel = 0, objectName =       "fightScript", methodName = "dispose",      index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // during fight with Vohaul, parameter 0 is an object
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,      roomNr =     150, scriptNr =  152, inheritanceLevel = 0, objectName =      "driveCloseUp", methodName = "dispose",      index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when choosing "beam download", parameter 0 is an object
        };

        public static readonly SciWorkaroundEntry[] kIsObject_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.GK1,         roomNr =  50, scriptNr =  999, inheritanceLevel = 0, objectName =               "List", methodName = "eachElementDo",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0 } }, // GK1 demo, when asking Grace for messages it gets called with an invalid parameter (type "error") - bug #4950
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN, roomNr =  -1, scriptNr =  999, inheritanceLevel = 0, objectName =               "List", methodName = "eachElementDo",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0 } }, // when going to the game options, choosing "Info" and selecting anything from the list, gets called with an invalid parameter (type "error") - bug #4989
            new SciWorkaroundEntry { gameId = SciGameId.QFG3,        roomNr =  -1, scriptNr =  999, inheritanceLevel = 0, objectName =               "List", methodName = "eachElementDo",  index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0 } }, // when asking for something, gets called with type error parameter
        };

        public static readonly SciWorkaroundEntry[] kGetAngle_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE,  roomNr =   516, scriptNr =  992, inheritanceLevel = 0,   objectName =          "Motion", methodName = "init",         index =  0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE,      value = 0 } }, // The Legend of the Lost Jewel Demo (fan made): called with third/fourth parameters as objects
            new SciWorkaroundEntry { gameId = SciGameId.KQ6,      roomNr =    -1, scriptNr =  752, inheritanceLevel = 0,   objectName =     "throwDazzle", methodName = "changeState",  index =  0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // room 740/790 after the Genie is exposed in the Palace (short and long ending), it starts shooting lightning bolts around. An extra 5th parameter is passed - bug #4959 & #5203
            new SciWorkaroundEntry { gameId = SciGameId.SQ1,      roomNr =    -1, scriptNr =  927, inheritanceLevel = 0,   objectName =        "PAvoider", methodName = "doit",         index =  0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE,      value = 0 } }, // all rooms in Ulence Flats after getting the Pilot Droid: called with a single parameter when the droid is in Roger's path - bug #6016
        };

        public static readonly SciWorkaroundEntry[] kDirLoop_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.KQ4,      roomNr =    4, scriptNr =  992, inheritanceLevel = 0,   objectName =        "Avoid", methodName = "doit",         index =  0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE,      value = 0 } }, // when the ogre catches you in front of his house, second parameter points to the same object as the first parameter, instead of being an integer (the angle) - bug #5217
        };

        public static readonly SciWorkaroundEntry[] kDeleteKey_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,    roomNr = 300,  scriptNr = 999, inheritanceLevel =  0, objectName = "handleEventList", methodName = "delete",   index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // restarting hearts, while tray is shown - bug #6604
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,    roomNr = 500,  scriptNr = 999, inheritanceLevel =  0, objectName = "handleEventList", methodName = "delete",   index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // restarting cribbage, while tray is shown - bug #6604
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,    roomNr = 975,  scriptNr = 999, inheritanceLevel =  0, objectName = "handleEventList", methodName = "delete",   index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // going back to gamelist from hearts/cribbage, while tray is shown - bug #6604
        };

        public static readonly SciWorkaroundEntry[] kDisplay_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN,  roomNr =  300, scriptNr =  300, inheritanceLevel = 0, objectName =   "geneDude", methodName = "show",        index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when looking at the gene explanation chart - a parameter is an object
            new SciWorkaroundEntry { gameId = SciGameId.LONGBOW,      roomNr =  95,  scriptNr =    95, inheritanceLevel =  0, objectName =          "countDown", methodName = "changeState",                index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value =    0 } }, // DEMO: during title screen "Robin Hood! Your bow is needed"
            new SciWorkaroundEntry { gameId = SciGameId.LONGBOW,      roomNr = 220,  scriptNr =   220, inheritanceLevel =  0, objectName =             "moveOn", methodName = "changeState",                index =     0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value =    0 } }, // DEMO: during second room "Outwit and outfight..."
            new SciWorkaroundEntry { gameId = SciGameId.LONGBOW,      roomNr = 210,  scriptNr =   210, inheritanceLevel =  0, objectName =               "mama", methodName = "changeState",                index =     0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value =    0 } }, // DEMO: during third room "Fall under the spell..."
            new SciWorkaroundEntry { gameId = SciGameId.LONGBOW,      roomNr = 320,  scriptNr =   320, inheritanceLevel =  0,objectName =               "flyin", methodName = "changeState",                index =     0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value =    0 } }, // DEMO: during fourth room "Conspiracies, love..."
            new SciWorkaroundEntry { gameId = SciGameId.PQ2,          roomNr =   23, scriptNr =   23, inheritanceLevel = 0, objectName = "rm23Script", methodName = "elements",    localCallSignature  = sig_kDisplay_pq2_1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when looking at the 2nd page of pate's file - 0x75 as id
            new SciWorkaroundEntry { gameId = SciGameId.QFG1,         roomNr =   11, scriptNr =   11, inheritanceLevel = 0, objectName =     "battle", methodName = "<noname90>",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // DEMO: When entering battle, 0x75 as id
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,          roomNr =  397, scriptNr =    0, inheritanceLevel = 0, objectName =           "", methodName = "export 12",   index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // FLOPPY: when going into the computer store - bug #5227
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,          roomNr =  391, scriptNr =  391, inheritanceLevel = 0, objectName =  "doCatalog", methodName = "mode",        localCallSignature  =  sig_kDisplay_pq2_1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // CD: clicking on catalog in roboter sale - a parameter is an object
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,          roomNr =  391, scriptNr =  391, inheritanceLevel = 0, objectName = "choosePlug", methodName = "changeState", index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // CD: ordering connector in roboter sale - a parameter is an object
        };

        public static readonly SciWorkaroundEntry[] kCelWide_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,     roomNr = -1, scriptNr = 255, inheritanceLevel = 0, objectName = "deathIcon", methodName = "setSize",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // english floppy: when getting beaten up in the inn and probably more, called with 2nd parameter as object - bug #5049
            new SciWorkaroundEntry { gameId = SciGameId.PQ2,     roomNr = -1, scriptNr = 255, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when showing picture within windows, called with 2nd/3rd parameters as objects
            new SciWorkaroundEntry { gameId = SciGameId.SQ1,     roomNr =  1, scriptNr = 255, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // DEMO: Called with 2nd/3rd parameters as objects when clicking on the menu - bug #5012
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE, roomNr = -1, scriptNr = 979, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // In The Gem Scenario and perhaps other fanmade games, this is called with 2nd/3rd parameters as objects - bug #5144
        };

        public static readonly SciWorkaroundEntry[] kCelHigh_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,     roomNr = -1, scriptNr = 255, inheritanceLevel = 0, objectName = "deathIcon", methodName = "setSize",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // english floppy: when getting beaten up in the inn and probably more, called with 2nd parameter as object - bug #5049
            new SciWorkaroundEntry { gameId = SciGameId.PQ2,     roomNr = -1, scriptNr = 255, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when showing picture within windows, called with 2nd/3rd parameters as objects
            new SciWorkaroundEntry { gameId = SciGameId.SQ1,     roomNr =  1, scriptNr = 255, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // DEMO: Called with 2nd/3rd parameters as objects when clicking on the menu - bug #5012
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE, roomNr = -1, scriptNr = 979, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // In The Gem Scenario and perhaps other fanmade games, this is called with 2nd/3rd parameters as objects - bug #5144
        };

        public static readonly SciWorkaroundEntry[] kSetPort_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.LSL6, roomNr = 740, scriptNr =  740, inheritanceLevel = 0, objectName =              "rm740", methodName = "drawPic",    index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // ending scene, is called with additional 3 (!) parameters
            new SciWorkaroundEntry { gameId = SciGameId.QFG3, roomNr = 830, scriptNr =  830, inheritanceLevel = 0, objectName =        "portalOpens", methodName = "changeState",index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when the portal appears during the end, gets called with 4 parameters - bug #5174
        };

        public static readonly SciWorkaroundEntry[] kNewWindow_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST, roomNr = -1, scriptNr = 981, inheritanceLevel = 0, objectName = "SysWindow", methodName = "open",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // EcoQuest 1 demo uses an in-between interpreter from SCI1 to SCI1.1. It's SCI1.1, but uses the SCI1 semantics for this call - bug #4976
        };

        public static readonly SciWorkaroundEntry[] kPalVarySetPercent_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.GK1, roomNr = 370, scriptNr = 370, inheritanceLevel = 0, objectName = "graceComeOut", methodName = "changeState", index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // there's an extra parameter in GK1, when changing chapters. This extra parameter seems to be a bug or just unimplemented functionality, as there's no visible change from the original in the chapter change room
        };

        public static readonly SciWorkaroundEntry[] kMoveCursor_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.KQ5, roomNr =-1, scriptNr = 937, inheritanceLevel = 0, objectName = "IconBar", methodName = "handleEvent",  index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when pressing escape to open the menu, gets called with one parameter instead of 2 - bug #5575
        };

        public static readonly SciWorkaroundEntry[] kDeviceInfo_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE, roomNr =       -1, scriptNr =  994, inheritanceLevel = 1, objectName =              "Game", methodName = "save",    localCallSignature = sig_kDeviceInfo_Fanmade_1, index =   0, newValue = { type = SciWorkaroundType.STILLCALL, value = 0 } }, // In fanmade games, this is called with one parameter for CurDevice (Cascade Quest)
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE, roomNr =       -1, scriptNr =  994, inheritanceLevel = 1, objectName =              "Game", methodName = "save",    localCallSignature = sig_kDeviceInfo_Fanmade_2, index =   0, newValue = { type = SciWorkaroundType.STILLCALL, value = 0 } }, // In fanmade games, this is called with one parameter for CurDevice (Demo Quest)
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE, roomNr =       -1, scriptNr =  994, inheritanceLevel = 0, objectName =             "Black", methodName = "save",    localCallSignature = sig_kDeviceInfo_Fanmade_1, index =   0, newValue = { type = SciWorkaroundType.IGNORE,    value = 0 } }, // In fanmade games, this is called with one parameter for CurDevice (Black Cauldron Remake)
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE, roomNr =       -1, scriptNr =  994, inheritanceLevel = 1, objectName =              "Game", methodName = "restore", localCallSignature = sig_kDeviceInfo_Fanmade_1, index =   0, newValue = { type = SciWorkaroundType.STILLCALL, value = 0 } }, // In fanmade games, this is called with one parameter for CurDevice (Cascade Quest)
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE, roomNr =       -1, scriptNr =  994, inheritanceLevel = 1, objectName =              "Game", methodName = "restore", localCallSignature = sig_kDeviceInfo_Fanmade_2, index =   0, newValue = { type = SciWorkaroundType.STILLCALL, value = 0 } }, // In fanmade games, this is called with one parameter for CurDevice (Demo Quest)
        };

        public static SciWorkaroundEntry[] kStrCpy_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSE, roomNr=  23, scriptNr =   23, inheritanceLevel=   0, objectName =          "talkScript", methodName = "changeState", index =    0, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // when talking to the girl in scene 23, there's no destination parameter (script bug - wrong instruction order). The original source is used directly afterwards in kDisplay, to show the girl's text - bug #6485
        };

        public static readonly SciWorkaroundEntry[] kMemory_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.LAURABOW2, roomNr =     -1, scriptNr =  999, inheritanceLevel = 0, objectName =                  "", methodName = "export 6", index =   0, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // during the intro, when exiting the train (room 160), talking to Mr. Augustini, etc. - bug #4944
            new SciWorkaroundEntry { gameId = SciGameId.SQ1,       roomNr =     -1, scriptNr =  999, inheritanceLevel = 0, objectName =                  "", methodName = "export 6", index =   0, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // during walking Roger around Ulence Flats - bug #6017
        };

        public static readonly SciWorkaroundEntry[] kPaletteUnsetFlag_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.QFG4, roomNr =          100, scriptNr =   100, inheritanceLevel =  0, objectName =            "doMovie", methodName = "changeState", index =    0, newValue = { type = SciWorkaroundType.IGNORE,  value =   0 } }, // after the Sierra logo, no flags are passed, thus the call is meaningless - bug #4947
        };

        public static readonly SciWorkaroundEntry[] kStrAt_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.CASTLEBRAIN, roomNr =   220, scriptNr =  220, inheritanceLevel = 0, objectName =        "robotJokes", methodName = "animateOnce",index =   0, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // when trying to view the terminal at the end of the maze without having collected any robot jokes - bug #5127
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN, roomNr =   300, scriptNr =  310, inheritanceLevel = 0, objectName =        "childBreed", methodName = "changeState",localCallSignature = sig_kStrAt_ibrain_1, index =   0, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // when clicking Breed to get the second-generation cyborg hybrid (Standard difficulty), the two parameters are swapped - bug #5088
        };

        public static readonly SciWorkaroundEntry[] kStrLen_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.QFG2, roomNr = 210, scriptNr = 2, inheritanceLevel = 0, objectName = "", methodName = "export 21", localCallSignature = sig_kStrLen_qfg2_1, index = 0, newValue = { type =SciWorkaroundType.FAKE,  value = 0 } }, // When saying something incorrect at the WIT, an integer is passed instead of a reference - bug #5489
        };

        public static readonly SciWorkaroundEntry[] kUnLoad_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST,  roomNr =    380, scriptNr =   61, inheritanceLevel = 0, objectName =             "gotIt", methodName = "changeState", index =   0, newValue = { type = SciWorkaroundType.IGNORE, value = 0 } }, // CD version: after talking to the dolphin the first time, a 3rd parameter is passed by accident
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST,  roomNr =    380, scriptNr =   69, inheritanceLevel = 0, objectName =  "lookAtBlackBoard", methodName = "changeState", index =   0, newValue = { type = SciWorkaroundType.IGNORE, value = 0 } }, // German version, when closing the blackboard closeup in the dolphin room, a 3rd parameter is passed by accident - bug #5483
            new SciWorkaroundEntry { gameId = SciGameId.LAURABOW2, roomNr =     -1, scriptNr =   -1, inheritanceLevel = 0, objectName =          "sCartoon", methodName = "changeState", index =   0, newValue = { type = SciWorkaroundType.IGNORE, value = 0 } }, // DEMO: during the intro, a 3rd parameter is passed by accident - bug #4966
            new SciWorkaroundEntry { gameId = SciGameId.LSL6,      roomNr =    130, scriptNr =  130, inheritanceLevel = 0, objectName =   "recruitLarryScr", methodName = "changeState", index =   0, newValue = { type = SciWorkaroundType.IGNORE, value = 0 } }, // during intro, a 3rd parameter is passed by accident
            new SciWorkaroundEntry { gameId = SciGameId.LSL6,      roomNr =    740, scriptNr =  740, inheritanceLevel = 0, objectName =       "showCartoon", methodName = "changeState", index =   0, newValue = { type = SciWorkaroundType.IGNORE, value = 0 } }, // during ending, 4 additional parameters are passed by accident
            new SciWorkaroundEntry { gameId = SciGameId.LSL6HIRES, roomNr =    130, scriptNr =  130, inheritanceLevel = 0, objectName =   "recruitLarryScr", methodName = "changeState", index =   0, newValue = { type = SciWorkaroundType.IGNORE, value = 0 } }, // during intro, a 3rd parameter is passed by accident
            new SciWorkaroundEntry { gameId = SciGameId.SQ1,       roomNr =     43, scriptNr =  303, inheritanceLevel = 0, objectName =           "slotGuy", methodName = "dispose",     index =   0, newValue = { type = SciWorkaroundType.IGNORE, value = 0 } }, // when leaving ulence flats bar, parameter 1 is not passed - script error
            new SciWorkaroundEntry { gameId = SciGameId.QFG4,      roomNr =     -1, scriptNr =  110, inheritanceLevel = 0, objectName =           "dreamer", methodName = "dispose",     index =   0, newValue = { type = SciWorkaroundType.IGNORE, value = 0 } }, // during the dream sequence, a 3rd parameter is passed by accident
        };

        public static readonly SciWorkaroundEntry[] kReadNumber_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.CNICK_LAURABOW,roomNr = 100, scriptNr =  101, inheritanceLevel = 0, objectName =         "dominoes.opt", methodName = "doit", index =   0, newValue = { type = SciWorkaroundType.STILLCALL, value = 0 } }, // When dominoes.opt is present, the game scripts call kReadNumber with an extra integer parameter - bug #6425
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE3,        roomNr = 100, scriptNr =  101, inheritanceLevel = 0, objectName =         "dominoes.opt", methodName = "doit", index =   0, newValue = { type = SciWorkaroundType.STILLCALL, value = 0 } }, // When dominoes.opt is present, the game scripts call kReadNumber with an extra integer parameter - bug #6425
        };

        public static readonly SciWorkaroundEntry[] UninitializedReadWorkarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.CAMELOT, roomNr = 40, scriptNr =    40, inheritanceLevel =  0, objectName =              "Rm40", methodName = "handleEvent",                     index =     0, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // when looking at the ground at the pool of Siloam - bug #6401
            new SciWorkaroundEntry { gameId = SciGameId.CASTLEBRAIN,   roomNr = 280,  scriptNr = 280, inheritanceLevel =  0,  objectName =       "programmer",methodName =  "dispatchEvent",                   index =     0, newValue = { type = SciWorkaroundType.FAKE, value = 0xf } }, // pressing 'q' on the computer screen in the robot room, and closing the help dialog that pops up (bug #5143). Moves the cursor to the view with the ID returned (in this case, the robot hand)
            new SciWorkaroundEntry { gameId = SciGameId.CNICK_KQ,      roomNr =  -1,   scriptNr =  0, inheritanceLevel =  1,     objectName =     "Character", methodName = "say",                             index =     -1, newValue = { type = SciWorkaroundType.FAKE,  value = 0 } }, // checkers/backgammon, like in hoyle 3 - temps 504 and 505 - bug #6255
            new SciWorkaroundEntry { gameId = SciGameId.CNICK_KQ,     roomNr =   -1, scriptNr =  700, inheritanceLevel =  0,objectName ="gcWindow", methodName = "open",                            index =    -1, newValue = { type = SciWorkaroundType.FAKE,  value = 0 } }, // when entering the control menu, like in hoyle 3
            new SciWorkaroundEntry { gameId = SciGameId.CNICK_KQ,     roomNr =  300, scriptNr =  303, inheritanceLevel =  0,objectName ="theDoubleCube", methodName = "<noname520>",                     index =      5, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // while playing backgammon with doubling enabled - bug #6426 (same as the theDoubleCube::make workaround for Hoyle 3)
            new SciWorkaroundEntry { gameId = SciGameId.CNICK_KQ,     roomNr =  300, scriptNr =  303, inheritanceLevel =  0,objectName ="theDoubleCube",methodName =  "<noname519>",                     index =   9, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // when accepting a double, while playing backgammon with doubling enabled (same as the theDoubleCube::accept workaround for Hoyle 3)
            new SciWorkaroundEntry { gameId = SciGameId.CNICK_LAURABOW,roomNr =  -1,  scriptNr =   0, inheritanceLevel =  1,objectName ="Character",methodName =  "say",                             index = -1, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // Yatch, like in hoyle 3 - temps 504 and 505 - bug #6424
            new SciWorkaroundEntry { gameId = SciGameId.CNICK_LAURABOW,roomNr =  -1,  scriptNr = 700, inheritanceLevel =  0,objectName =null, methodName = "open",                            index = -1, newValue = { type = SciWorkaroundType.FAKE,  value = 0 } }, // when entering control menu - bug #6423 (same as the gcWindow workaround for Hoyle 3)
            new SciWorkaroundEntry { gameId = SciGameId.CNICK_LAURABOW,roomNr = 100,  scriptNr = 100, inheritanceLevel =  0,objectName =null,methodName =  "<noname144>",                     index =      1, newValue = { type = SciWorkaroundType.FAKE,  value = 0 } }, // while playing domino - bug #6429 (same as the dominoHand2 workaround for Hoyle 3)
            new SciWorkaroundEntry { gameId = SciGameId.CNICK_LAURABOW,roomNr = 100, scriptNr =  110, inheritanceLevel =  0,objectName =null,methodName =  "doit",                            index =     -1, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // when changing the "Dominoes per hand" setting - bug #6430
            new SciWorkaroundEntry { gameId = SciGameId.CNICK_LONGBOW, roomNr =   0, scriptNr =    0,  inheritanceLevel = 0,       objectName =   "RH Budget",methodName =  "init",                            index =      1, newValue = { type = SciWorkaroundType.FAKE,  value = 0 } }, // when starting the game
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST,      roomNr =  -1, scriptNr =   -1,  inheritanceLevel = 0,      objectName =           null, methodName = "doVerb",                          index =      0, newValue = { type = SciWorkaroundType.FAKE,  value = 0 } }, // almost clicking anywhere triggers this in almost all rooms
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE,       roomNr = 516, scriptNr =  979,  inheritanceLevel = 0,       objectName =            "",methodName =  "export 0",                        index =     20, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // Happens in Grotesteing after the logos
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE,       roomNr = 528, scriptNr =  990,  inheritanceLevel = 0,   objectName =         "GDialog",methodName =  "doit",                            index =      4, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // Happens in Cascade Quest when closing the glossary - bug #5116
            //new SciWorkaroundEntry { gameId = SciGameId.FANMADE,       roomNr = 488,  scriptNr =   1, inheritanceLevel =  0,  objectName =       "RoomScript",methodName =  "doit",        sig_uninitread_fanmade_1, index =     1, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // Happens in Ocean Battle while playing - bug #5335
            new SciWorkaroundEntry { gameId = SciGameId.FREDDYPHARKAS, roomNr =  -1,  scriptNr =  24, inheritanceLevel =  0,  objectName =            "gcWin", methodName = "open",                            index =     5, newValue = { type = SciWorkaroundType.FAKE,value = 0xf } }, // is used as priority for game menu
            new SciWorkaroundEntry { gameId = SciGameId.FREDDYPHARKAS, roomNr =  -1,  scriptNr =  31, inheritanceLevel =  0,  objectName =          "quitWin", methodName = "open",                            index =      5, newValue = { type = SciWorkaroundType.FAKE,value = 0xf } }, // is used as priority for game menu
            new SciWorkaroundEntry { gameId = SciGameId.FREDDYPHARKAS, roomNr = 540,  scriptNr = 540, inheritanceLevel =  0,  objectName =        "WaverCode", methodName = "init",                            index =     -1, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // Gun pratice mini-game - bug #5232
            new SciWorkaroundEntry { gameId = SciGameId.GK1,           roomNr =  -1, scriptNr =64950, inheritanceLevel = -1,  objectName =          "Feature",methodName =  "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // sometimes when walk-clicking
            new SciWorkaroundEntry { gameId = SciGameId.GK2,          roomNr =   -1,  scriptNr =  11, inheritanceLevel =  0,  objectName =                 "",methodName =  "export 10",                       index =      3, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // called when the game starts
            new SciWorkaroundEntry { gameId = SciGameId.GK2,          roomNr =   -1, scriptNr =   11, inheritanceLevel =  0,  objectName =                 "",methodName =  "export 10",                       index =      4, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // called during the game
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE1,       roomNr =   4,  scriptNr = 104,  inheritanceLevel = 0,   objectName ="GinRummyCardList", methodName = "calcRuns",                        index =      4, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // Gin Rummy / right when the game starts
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE1,       roomNr =   5, scriptNr =  204,  inheritanceLevel = 0,   objectName =         "tableau",methodName =  "checkRuns",                       index =    2, newValue = { type = SciWorkaroundType.FAKE,  value = 0 } }, // Cribbage / during the game
            //new SciWorkaroundEntry { gameId = SciGameId.HOYLE1,       roomNr =   3, scriptNr =   16,  inheritanceLevel = 0,   objectName =                "",methodName =  "export 0",     sig_uninitread_hoyle1_1,index =      3, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // Hearts / during the game - bug #5299
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE1,       roomNr =  -1, scriptNr =  997,  inheritanceLevel = 0,    objectName =        "MenuBar",methodName =  "doit",                            index =      0, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // When changing game speed settings - bug #5512
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE3,       roomNr =  -1,  scriptNr =   0,  inheritanceLevel = 1,    objectName =      "Character", methodName = "say",                             index =    -1, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // when starting checkers or dominoes, first time a character says something - temps 504 and 505
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE3,      roomNr =    -1, scriptNr =  700, inheritanceLevel =  0,   objectName =        "gcWindow", methodName = "open",                           index =    -1, newValue = { type = SciWorkaroundType.FAKE,  value = 0 } }, // when entering control menu
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE3,      roomNr =  100,  scriptNr = 100,  inheritanceLevel = 0,    objectName =    "dominoHand2",methodName =  "cue",                             index =      1, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // while playing domino - bug #5042
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE3,      roomNr =  100,  scriptNr = 110,  inheritanceLevel = 0,   objectName =        "OKButton",methodName =  "doit",                            index =     -1, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // when changing the "Dominoes per hand" setting - bug #6430
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE3,      roomNr =  300,  scriptNr = 303,  inheritanceLevel = 0,   objectName =   "theDoubleCube", methodName = "make",                            index =      5, newValue = { type = SciWorkaroundType.FAKE, value = 0 } }, // while playing backgammon with doubling enabled
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE3,      roomNr =  300,  scriptNr = 303,  inheritanceLevel = 0,   objectName =   "theDoubleCube",methodName =  "accept",                          index =      9, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when accepting a double, while playing backgammon with doubling enabled
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,      roomNr =   -1,  scriptNr =   0,  inheritanceLevel = 0,  objectName =               null,methodName =  "open",                            index =     -1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when selecting "Control" from the menu (temp vars 0-3) - bug #5132
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,      roomNr =  910,  scriptNr =  18,  inheritanceLevel = 0,  objectName =               null,methodName =  "init",                            index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // during tutorial - bug #5213
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,      roomNr =  910,  scriptNr = 910,  inheritanceLevel = 0,  objectName =               null,methodName =  "setup",                           index =      3, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when selecting "Tutorial" from the main menu - bug #5132
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,      roomNr =  700, scriptNr =  700,  inheritanceLevel = 1,  objectName =       "BridgeHand",methodName =  "calcQTS",                         index =      3, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when placing a bid in bridge (always)
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,      roomNr =  700, scriptNr =  710,  inheritanceLevel = 1, objectName ="BridgeStrategyPlay",methodName =  "checkSplitTops",                  index =     10, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // while playing bridge, objects LeadReturn_Trump, SecondSeat_Trump, ThirdSeat_Trump and others - bug #5794
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,       roomNr = 700,  scriptNr =  -1,  inheritanceLevel = 1,  objectName =    "BridgeDefense",methodName =  "think",                           index =     -1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // sometimes while playing bridge, temp var 3, 17 and others, objects LeadReturn_Trump, ThirdSeat_Trump and others
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,       roomNr = 700,  scriptNr = 730, inheritanceLevel =  1,  objectName =    "BridgeDefense",methodName =  "beatTheirBest",                   index =      3, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // rarely while playing bridge
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,       roomNr = 700,  scriptNr =  -1,  inheritanceLevel = 1,   objectName =            "Code",methodName =  "doit",                            index =     -1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when placing a bid in bridge (always), temp var 11, 24, 27, 46, 75, objects compete_tree, compwe_tree, other1_tree, b1 - bugs #5663 and #5794
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,       roomNr = 700,  scriptNr = 921,  inheritanceLevel = 0,   objectName =           "Print",methodName =  "addEdit",                         index =      0, newValue = { type = SciWorkaroundType.FAKE,value =  118 } }, // when saving the game (may also occur in other situations) - bug #6601, bug #6614
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,       roomNr = 700,  scriptNr = 921, inheritanceLevel =  0,    objectName =          "Print",methodName =  "addEdit",                         index =      1, newValue = { type = SciWorkaroundType.FAKE,value =    1 } }, // see above, Text-control saves its coordinates to temp[0] and temp[1], Edit-control adjusts to those uninitialized temps, who by accident were left over from the Text-control
            //new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,       roomNr = 300,  scriptNr = 300, inheritanceLevel =  0,   objectName =                "",methodName =  "export 2",     sig_uninitread_hoyle4_1, index =     0, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // after passing around cards in hearts
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,       roomNr = 400,  scriptNr = 400, inheritanceLevel =  1,   objectName =         "GinHand",methodName =  "calcRuns",                        index =      4, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // sometimes while playing Gin Rummy (e.g. when knocking and placing a card) - bug #5665
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,       roomNr = 500,  scriptNr =  17, inheritanceLevel =  1,   objectName =       "Character",methodName =  "say",                             index =    504, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // sometimes while playing Cribbage (e.g. when the opponent says "Last Card") - bug #5662
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,       roomNr = 800,  scriptNr = 870, inheritanceLevel =  0,   objectName =  "EuchreStrategy",methodName =  "thinkLead",                       index =      0, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // while playing Euchre, happens at least on 2nd or 3rd turn - bug #6602
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,       roomNr =  -1,  scriptNr = 937, inheritanceLevel =  0,  objectName =          "IconBar",methodName =  "dispatchEvent",                   index =    408, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // pressing ENTER on scoreboard while mouse is not on OK button, may not happen all the time - bug #6603
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN,  roomNr = 100,  scriptNr = 937, inheritanceLevel =  0,   objectName =         "IconBar",methodName =  "dispatchEvent",                   index =     58, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // when using ENTER at the startup menu - bug #5241
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN,  roomNr = 140,  scriptNr  = 140,  inheritanceLevel = 0,  objectName =            "piece",methodName =  "init",                            index =      3, newValue = { type = SciWorkaroundType.FAKE,value =    1 } }, // first puzzle right at the start, some initialization variable. bnt is done on it, and it should be non-0
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN,  roomNr = 200,   scriptNr =268,  inheritanceLevel = 0,   objectName =       "anElement",methodName =  "select",                          index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // elements puzzle, gets used before super TextIcon
            //new SciWorkaroundEntry { gameId = SciGameId.JONES,        roomNr =   1,  scriptNr = 232,  inheritanceLevel = 0,   objectName =     "weekendText",methodName =  "draw",          sig_uninitread_jones_1, index =     0, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // jones/cd only - gets called during the game
            new SciWorkaroundEntry { gameId = SciGameId.JONES,        roomNr =   1,  scriptNr = 255,  inheritanceLevel = 0,   objectName =                "",methodName =  "export 0",                        index =     -1, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // jones/cd only - called when a game ends, temps 13 and 14
            new SciWorkaroundEntry { gameId = SciGameId.JONES,        roomNr = 764,  scriptNr = 255,  inheritanceLevel = 0,   objectName =                "", methodName = "export 0",                        index =     -1, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // jones/ega&vga only - called when the game starts, temps 13 and 14
            //new SciWorkaroundEntry { gameId = SciGameId.KQ5,            -1,     0,  0,                   "", "export 29",                       null,     3, newValue = { type = SciWorkaroundType.FAKE,   0xf } }, // called when playing harp for the harpies or when aborting dialog in toy shop, is used for kDoAudio - bug #4961
            // ^^ shouldn't be needed anymore, we got a script patch instead (kq5PatchCdHarpyVolume)
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,  roomNr =          25,   scriptNr = 25,  inheritanceLevel = 0,  objectName =            "rm025", methodName = "doit",                            index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // inside witch forest, when going to the room where the walking rock is
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,  roomNr =          55,  scriptNr =  55,  inheritanceLevel = 0, objectName =        "helpScript", methodName = "doit",                            index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when giving the tambourine to the monster in the labyrinth (only happens at one of the locations) - bug #5198
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,  roomNr =          -1,  scriptNr = 755,  inheritanceLevel = 0,  objectName =            "gcWin", methodName = "open",                            index =     -1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when entering control menu in the FM-Towns version
            new SciWorkaroundEntry { gameId = SciGameId.KQ6,  roomNr =          -1,  scriptNr =  30,  inheritanceLevel = 0, objectName =              "rats", methodName = "changeState",                     index =     -1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // rats in the catacombs (temps 1 - 5) - bugs #4958, #4998, #5017
            new SciWorkaroundEntry { gameId = SciGameId.KQ6,  roomNr =         210,  scriptNr = 210,  inheritanceLevel = 0,  objectName =            "rm210", methodName = "scriptCheck",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    1 } }, // using inventory in that room - bug #4953
            new SciWorkaroundEntry { gameId = SciGameId.KQ6,  roomNr =         500, scriptNr =  500,  inheritanceLevel = 0, objectName =             "rm500", methodName = "init",                            index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // going to island of the beast
            new SciWorkaroundEntry { gameId = SciGameId.KQ6,   roomNr =        520, scriptNr =  520,  inheritanceLevel = 0,  objectName =            "rm520", methodName = "init",                            index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // going to boiling water trap on beast isle
            new SciWorkaroundEntry { gameId = SciGameId.KQ6,   roomNr =         -1,  scriptNr = 903,  inheritanceLevel = 0,  objectName =       "controlWin", methodName = "open",                            index =      4, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when opening the controls window (save, load etc)
            new SciWorkaroundEntry { gameId = SciGameId.KQ6,   roomNr =         -1,  scriptNr = 907,  inheritanceLevel = 0,  objectName =           "tomato", methodName = "doVerb",                          index =      2, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // when looking at the rotten tomato in the inventory - bug #5331
            new SciWorkaroundEntry { gameId = SciGameId.KQ6,   roomNr =         -1,  scriptNr = 928,  inheritanceLevel = 0,   objectName =              null, methodName = "startText",                       index =      0, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // gets caused by Text+Audio support (see script patcher)
            new SciWorkaroundEntry { gameId = SciGameId.KQ7,   roomNr =         -1, scriptNr =64996,  inheritanceLevel = 0,   objectName =            "User", methodName = "handleEvent",                     index =      1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // called when pushing a keyboard key
            new SciWorkaroundEntry { gameId = SciGameId.LAURABOW,roomNr =       37,  scriptNr =   0,  inheritanceLevel = 0,   objectName =             "CB1", methodName = "doit",                            index =      1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when going up the stairs - bug #5084
            new SciWorkaroundEntry { gameId = SciGameId.LAURABOW,  roomNr =     -1,  scriptNr = 967,  inheritanceLevel = 0,   objectName =          "myIcon", methodName = "cycle",                           index =      1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // having any portrait conversation coming up - initial bug #4971
            new SciWorkaroundEntry { gameId = SciGameId.LAURABOW2, roomNr =     -1,  scriptNr =  24,  inheritanceLevel = 0,  objectName =            "gcWin", methodName = "open",                            index =      5, newValue = { type = SciWorkaroundType.FAKE,value =  0xf } }, // is used as priority for game menu
            new SciWorkaroundEntry { gameId = SciGameId.LAURABOW2, roomNr =     -1,  scriptNr =  21,  inheritanceLevel = 0,  objectName =    "dropCluesCode", methodName = "doit",                            index =      1, newValue = { type = SciWorkaroundType.FAKE,value =  0x7fff } }, // when asking some questions (e.g. the reporter about the burglary, or the policeman about Ziggy). Must be big, as the game scripts perform lt on it and start deleting journal entries - bugs #4979, #5026
            new SciWorkaroundEntry { gameId = SciGameId.LAURABOW2,roomNr =      -1,  scriptNr =  90,  inheritanceLevel = 1,   objectName =     "MuseumActor", methodName = "init",                            index =      6, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Random actors in museum - bug #5197
            new SciWorkaroundEntry { gameId = SciGameId.LAURABOW2, roomNr =    240,  scriptNr = 240,  inheritanceLevel = 0,  objectName =   "sSteveAnimates", methodName = "changeState",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Steve Dorian's idle animation at the docks - bug #5028
            new SciWorkaroundEntry { gameId = SciGameId.LAURABOW2, roomNr =     -1,  scriptNr = 928, inheritanceLevel =  0,  objectName =               null, methodName = "startText",                       index =      0, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // gets caused by Text+Audio support (see script patcher)
            new SciWorkaroundEntry { gameId = SciGameId.LONGBOW,   roomNr =     -1,  scriptNr =   0, inheritanceLevel =  0,  objectName =          "Longbow", methodName = "restart",                         index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // When canceling a restart game - bug #5244
            new SciWorkaroundEntry { gameId = SciGameId.LONGBOW,   roomNr =     -1,  scriptNr = 213, inheritanceLevel =  0,  objectName =            "clear", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // When giving an answer using the druid hand sign code in any room
            //new SciWorkaroundEntry { gameId = SciGameId.LONGBOW,   roomNr =     -1,  scriptNr = 213, inheritanceLevel =  0,  objectName =           "letter", methodName = "handleEvent", sig_uninitread_longbow_1, index =   1, newValue = { type = SciWorkaroundType.FAKE,  value =  0 } }, // When using the druid hand sign code in any room - bug #5035
            new SciWorkaroundEntry { gameId = SciGameId.LSL1,      roomNr =    250,  scriptNr = 250, inheritanceLevel =  0,  objectName =         "increase", methodName = "handleEvent",                     index =      2, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // casino, playing game, increasing bet
            new SciWorkaroundEntry { gameId = SciGameId.LSL1,      roomNr =    720,  scriptNr = 720, inheritanceLevel =  0,  objectName =            "rm720", methodName = "init",                            index =      0, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // age check room
            new SciWorkaroundEntry { gameId = SciGameId.LSL2,      roomNr =     38,  scriptNr =  38, inheritanceLevel =  0,   objectName =     "cloudScript", methodName = "changeState",                     index =      1, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // entering the room in the middle deck of the ship - bug #5034
            new SciWorkaroundEntry { gameId = SciGameId.LSL3,      roomNr =    340,  scriptNr = 340, inheritanceLevel =  0,   objectName =     "ComicScript", methodName = "changeState",                     index =     -1, newValue = { type = SciWorkaroundType.FAKE,  value =  0 } }, // right after entering the 3 ethnic groups inside comedy club (temps 200, 201, 202, 203)
            new SciWorkaroundEntry { gameId = SciGameId.LSL3,      roomNr =     -1,  scriptNr = 997, inheritanceLevel =  0,   objectName =      "TheMenuBar", methodName = "handleEvent",                     index =      1, newValue = { type = SciWorkaroundType.FAKE, value = 0xf } }, // when setting volume the first time, this temp is used to set volume on entry (normally it would have been initialized to 's')
            new SciWorkaroundEntry { gameId = SciGameId.LSL6,      roomNr =    820,  scriptNr =  82, inheritanceLevel =  0,   objectName =                "", methodName = "export 0",                        index =     -1, newValue = { type = SciWorkaroundType.FAKE,  value =  0 } }, // when touching the electric fence - bug #5103
            new SciWorkaroundEntry { gameId = SciGameId.LSL6,      roomNr =     -1,  scriptNr =  85, inheritanceLevel =  0,    objectName =      "washcloth", methodName = "doVerb",                          index =      0, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // washcloth in inventory
            new SciWorkaroundEntry { gameId = SciGameId.LSL6,      roomNr =     -1,  scriptNr = 928, inheritanceLevel = -1,   objectName =        "Narrator", methodName = "startText",                       index =      0, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // used by various objects that are even translated in foreign versions, that's why we use the base-class
            new SciWorkaroundEntry { gameId = SciGameId.LSL6HIRES, roomNr =      0,  scriptNr =  85, inheritanceLevel =  0,   objectName =          "LL6Inv", methodName = "init",                            index =      0, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // on startup
            new SciWorkaroundEntry { gameId = SciGameId.LSL6HIRES, roomNr =     -1, scriptNr =64950, inheritanceLevel =  1,   objectName =         "Feature", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,  value =  0 } }, // at least when entering swimming pool area
            new SciWorkaroundEntry { gameId = SciGameId.LSL6HIRES,  roomNr =    -1,scriptNr = 64964,  inheritanceLevel = 0,   objectName =           "DPath", methodName = "init",                            index =      1, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // during the game
            new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSE256,roomNr = -1,  scriptNr =   0,  inheritanceLevel = 0,   objectName =              "MG", methodName = "doit",                            index =      5, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // SCI1.1: When moving the cursor all the way to the left during the game - bug #5224
            new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSE256,roomNr = -1, scriptNr =  992,  inheritanceLevel = 0,   objectName =          "AIPath", methodName = "init",                            index =      0, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // Happens in the demo and full version. In the demo, it happens when walking two screens from mother goose's house to the north. In the full version, it happens in rooms 7 and 23 - bug #5269
            new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSE256,roomNr = 90,  scriptNr =  90,  inheritanceLevel = 0,   objectName =     "introScript", methodName = "changeState",                     index =     65, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // SCI1(CD): At the very end, after the game is completed and restarted - bug #5626
            new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSE256, roomNr =94,  scriptNr =  94, inheritanceLevel =  0,    objectName =        "sunrise", methodName = "changeState",                     index =    367, newValue = { type = SciWorkaroundType.FAKE,  value =  0 } }, // At the very end, after the game is completed - bug #5294
            new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSEHIRES,roomNr =-1,scriptNr =64950,  inheritanceLevel = 1,    objectName =        "Feature", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,  value =  0 } }, // right when clicking on a child at the start and probably also later
            new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSEHIRES,roomNr =-1,scriptNr =64950,  inheritanceLevel = 1,     objectName  =          "View",methodName =  "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // see above
            new SciWorkaroundEntry { gameId = SciGameId.PEPPER,       roomNr =  -1,  scriptNr = 894,  inheritanceLevel = 0,    objectName =        "Package", methodName = "doVerb",                          index =      3, newValue = { type = SciWorkaroundType.FAKE,  value =  0 } }, // using the hand on the book in the inventory - bug #5154
            new SciWorkaroundEntry { gameId = SciGameId.PEPPER,       roomNr = 150, scriptNr =  928,  inheritanceLevel = 0,   objectName =        "Narrator", methodName = "startText",                       index =      0, newValue = { type = SciWorkaroundType.FAKE,  value =  0 } }, // happens during the non-interactive demo of Pepper
            new SciWorkaroundEntry { gameId = SciGameId.PQ4,          roomNr =  -1, scriptNr =   25,  inheritanceLevel = 0,   objectName =      "iconToggle", methodName = "select",                          index =      1, newValue = { type = SciWorkaroundType.FAKE,  value =  0 } }, // when toggling the icon bar to auto-hide or not
            new SciWorkaroundEntry { gameId = SciGameId.PQSWAT,       roomNr =  -1, scriptNr =64950,  inheritanceLevel = 0,    objectName =           "View", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,  value =  0 } }, // Using the menu in the beginning
            //new SciWorkaroundEntry { gameId = SciGameId.QFG1,         roomNr =  -1,  scriptNr = 210,  inheritanceLevel = 0,    objectName =      "Encounter", methodName = "init",           sig_uninitread_qfg1_1,index =      0, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // qfg1/hq1: going to the brigands hideout
            new SciWorkaroundEntry { gameId = SciGameId.QFG1VGA,      roomNr =  16,  scriptNr =  16,  inheritanceLevel = 0,    objectName =    "lassoFailed", methodName = "changeState",                     index =     -1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // qfg1vga: casting the "fetch" spell in the screen with the flowers, temps 0 and 1 - bug #5309
            //new SciWorkaroundEntry { gameId = SciGameId.QFG1VGA,      roomNr =  -1,  scriptNr = 210,  inheritanceLevel = 0,   objectName =       "Encounter", methodName = "init",        sig_uninitread_qfg1vga_1,index =      0, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // qfg1vga: going to the brigands hideout - bug #5515
            new SciWorkaroundEntry { gameId = SciGameId.QFG2,         roomNr =  -1,  scriptNr =  71,  inheritanceLevel = 0,    objectName =    "theInvSheet", methodName = "doit",                            index =      1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // accessing the inventory
            new SciWorkaroundEntry { gameId = SciGameId.QFG2,         roomNr =  -1,  scriptNr =  79, inheritanceLevel =  0,    objectName =    "TryToMoveTo", methodName = "onTarget",                        index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when throwing pot at air elemental, happens when client coordinates are the same as airElemental coordinates. happened to me right after room change - bug #6859
            new SciWorkaroundEntry { gameId = SciGameId.QFG2,         roomNr =  -1,  scriptNr = 701, inheritanceLevel = -1,    objectName =          "Alley", methodName = "at",                              index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when walking inside the alleys in the town - bug #5019 & #5106
            new SciWorkaroundEntry { gameId = SciGameId.QFG2,         roomNr =  -1, scriptNr =  990,  inheritanceLevel = 0,    objectName =        "Restore", methodName = "doit",                            index =    364, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when pressing enter in restore dialog w/o any saved games present
            //new SciWorkaroundEntry { gameId = SciGameId.QFG2,         roomNr = 260,  scriptNr = 260,  inheritanceLevel = 0,   objectName =          "abdulS", methodName = "changeState",    sig_uninitread_qfg2_1,index =     -1, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // During the thief's first mission (in the house), just before Abdul is about to enter the house (where you have to hide in the wardrobe), bug #5153, temps 1 and 2
            //new SciWorkaroundEntry { gameId = SciGameId.QFG2,         roomNr = 260,  scriptNr = 260, inheritanceLevel =  0,   objectName =         "jabbarS", methodName = "changeState",    sig_uninitread_qfg2_1, index =    -1, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // During the thief's first mission (in the house), just before Jabbar is about to enter the house (where you have to hide in the wardrobe), bug #5164, temps 1 and 2
            new SciWorkaroundEntry { gameId = SciGameId.QFG2,        roomNr =  500,  scriptNr = 500,  inheritanceLevel = 0,   objectName ="lightNextCandleS", methodName = "changeState",                     index =     -1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Inside the last room, while Ad Avis performs the ritual to summon the genie - bug #5566
            new SciWorkaroundEntry { gameId = SciGameId.QFG2,        roomNr =   -1,  scriptNr = 700,  inheritanceLevel = 0,  objectName =               null, methodName = "showSign",                        index =     10, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Occurs sometimes when reading a sign in Raseir, Shapeir et al - bugs #5627, #5635
            new SciWorkaroundEntry { gameId = SciGameId.QFG3,        roomNr =  510,  scriptNr = 510,  inheritanceLevel = 0,  objectName =       "awardPrize", methodName = "changeState",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    1 } }, // Simbani warrior challenge, after throwing the spears and retrieving the ring - bug #5277. Must be non-zero, otherwise the prize is awarded twice - bug #6160
            //new SciWorkaroundEntry { gameId = SciGameId.QFG3,        roomNr =  140,  scriptNr = 140,  inheritanceLevel = 0,   objectName =           "rm140", methodName = "init",           sig_uninitread_qfg3_1, index =     0, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // when importing a character and selecting the previous profession - bug #5163
            new SciWorkaroundEntry { gameId = SciGameId.QFG3,        roomNr =  330,  scriptNr = 330, inheritanceLevel = -1,   objectName =          "Teller", methodName = "doChild",                         index =     -1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when talking to King Rajah about "Rajah" (bug #5033, temp 1) or "Tarna" (temp 0), or when clicking on yourself and saying "Greet" (bug #5148, temp 1)
            new SciWorkaroundEntry { gameId = SciGameId.QFG3,        roomNr =  700,  scriptNr = 700, inheritanceLevel = -1,   objectName =   "monsterIsDead", methodName = "changeState",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // in the jungle, after winning any fight, bug #5169
            new SciWorkaroundEntry { gameId = SciGameId.QFG3,        roomNr =  470,  scriptNr = 470, inheritanceLevel = -1,  objectName =            "rm470", methodName = "notify",                          index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // closing the character screen in the Simbani village in the room with the bridge, bug #5165
            new SciWorkaroundEntry { gameId = SciGameId.QFG3,        roomNr =  490,  scriptNr = 490, inheritanceLevel = -1,  objectName =    "computersMove", methodName = "changeState",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // when finishing awari game, bug #5167
            //new SciWorkaroundEntry { gameId = SciGameId.QFG3,        roomNr =  490,  scriptNr = 490, inheritanceLevel = -1,  objectName =    "computersMove", methodName = "changeState",    sig_uninitread_qfg3_2,     4, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // also when finishing awari game
            new SciWorkaroundEntry { gameId = SciGameId.QFG3,        roomNr =  851,  scriptNr =  32, inheritanceLevel = -1,   objectName =         "ProjObj", methodName = "doit",                            index =      1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // near the end, when throwing the spear of death, bug #5282
            new SciWorkaroundEntry { gameId = SciGameId.QFG4,        roomNr =   -1,  scriptNr =  15, inheritanceLevel = -1,   objectName =  "charInitScreen", methodName = "dispatchEvent",                   index =      5, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // floppy version, when viewing the character screen
            new SciWorkaroundEntry { gameId = SciGameId.QFG4,        roomNr =   -1, scriptNr =64917, inheritanceLevel = -1,   objectName =    "controlPlane", methodName = "setBitmap",                       index =      3, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // floppy version, when entering the game menu
            new SciWorkaroundEntry { gameId = SciGameId.QFG4,        roomNr =   -1, scriptNr =64917, inheritanceLevel = -1,   objectName =           "Plane", methodName = "setBitmap",                       index =      3, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // floppy version, happens sometimes in fight scenes
            new SciWorkaroundEntry { gameId = SciGameId.QFG4,        roomNr =  520, scriptNr =64950, inheritanceLevel =  0,   objectName =          "fLake2", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // CD version, at the lake, when meeting the Rusalka and attempting to leave
            new SciWorkaroundEntry { gameId = SciGameId.QFG4,        roomNr =  800, scriptNr =64950, inheritanceLevel =  0,   objectName =            "View", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // CD version, in the room with the spider pillar, when climbing on the pillar
            new SciWorkaroundEntry { gameId = SciGameId.RAMA,        roomNr =   12,scriptNr = 64950, inheritanceLevel = -1,  objectName = "InterfaceFeature", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Demo, right when it starts
            new SciWorkaroundEntry { gameId = SciGameId.RAMA,        roomNr =   12,scriptNr = 64950, inheritanceLevel = -1,   objectName =   "hiliteOptText", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Demo, right when it starts
            new SciWorkaroundEntry { gameId = SciGameId.RAMA,         roomNr =  12,scriptNr = 64950, inheritanceLevel = -1,   objectName =            "View", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Demo, right when it starts
            new SciWorkaroundEntry { gameId = SciGameId.SHIVERS,      roomNr =  -1, scriptNr =  952, inheritanceLevel =  0,   objectName =    "SoundManager", methodName = "stop",                            index =      2, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Just after Sierra logo
            new SciWorkaroundEntry { gameId = SciGameId.SHIVERS,      roomNr =  -1, scriptNr =64950, inheritanceLevel =  0,    objectName =        "Feature", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // When clicking on the locked door at the beginning
            new SciWorkaroundEntry { gameId = SciGameId.SHIVERS,      roomNr =  -1, scriptNr =64950, inheritanceLevel =  0,    objectName =           "View", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // When clicking on the gargoyle eye at the beginning
            new SciWorkaroundEntry { gameId = SciGameId.SHIVERS,   roomNr =  20311, scriptNr =64964, inheritanceLevel =  0,    objectName =          "DPath", methodName = "init",                            index =      1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Just after door puzzle is solved and the metal balls start to roll
            new SciWorkaroundEntry { gameId = SciGameId.SHIVERS,   roomNr =  29260, scriptNr =29260, inheritanceLevel =  0,    objectName =         "spMars", methodName = "handleEvent",                     index =      4, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // When clicking mars after seeing fortune to align earth etc...
            new SciWorkaroundEntry { gameId = SciGameId.SHIVERS,   roomNr =  29260,scriptNr = 29260,  inheritanceLevel = 0,    objectName =        "spVenus", methodName = "handleEvent",                     index =      4, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // When clicking venus after seeing fortune to align earth etc...
            new SciWorkaroundEntry { gameId = SciGameId.SQ1,       roomNr =    103,scriptNr =   103, inheritanceLevel =  0,    objectName =           "hand", methodName = "internalEvent",                   index =     -1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Spanish (and maybe early versions?) only: when moving cursor over input pad, temps 1 and 2
            new SciWorkaroundEntry { gameId = SciGameId.SQ1,       roomNr =     -1, scriptNr =  703,  inheritanceLevel = 0,   objectName =                "", methodName = "export 1",                        index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // sub that's called from several objects while on sarien battle cruiser
            //new SciWorkaroundEntry { gameId = SciGameId.SQ1,       roomNr =     -1,  scriptNr = 703, inheritanceLevel =  0,   objectName =      "firePulsar", methodName = "changeState",     sig_uninitread_sq1_1,index =      0, newValue = { type = SciWorkaroundType.FAKE,   value = 0 } }, // export 1, but called locally (when shooting at aliens)
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =     -1,  scriptNr = 398, inheritanceLevel =  0,   objectName =         "showBox", methodName = "changeState",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // CD: called when rummaging in Software Excess bargain bin
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =     -1,  scriptNr = 928, inheritanceLevel = -1,   objectName =        "Narrator", methodName = "startText",                       index =   1000, newValue = { type = SciWorkaroundType.FAKE,value =    1 } }, // CD: happens in the options dialog and in-game when speech and subtitles are used simultaneously
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =     -1,  scriptNr = 708, inheritanceLevel = -1,   objectName =         "exitBut", methodName = "doVerb",                          index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Floppy: happens, when looking at the "close" button in the sq4 hintbook - bug #6447
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,        roomNr =    -1,  scriptNr = 708, inheritanceLevel = -1,   objectName =                "", methodName = "doVerb",                          index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Floppy: happens, when looking at the "close" button... in Russian version - bug #5573
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,        roomNr =    -1,  scriptNr = 708, inheritanceLevel = -1,   objectName =         "prevBut", methodName = "doVerb",                          index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Floppy: happens, when looking at the "previous" button in the sq4 hintbook - bug #6447
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,        roomNr =    -1,  scriptNr = 708, inheritanceLevel = -1, objectName ="\xA8\xE6\xE3 \xAD\xA0\xA7\xA0\xA4.", methodName = "doVerb",          index =      0, newValue = { type = SciWorkaroundType.FAKE, value =   0 } }, // Floppy: happens, when looking at the "previous" button... in Russian version - bug #5573
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,        roomNr =    -1,  scriptNr = 708, inheritanceLevel = -1,   objectName =         "nextBut", methodName = "doVerb",                          index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Floppy: happens, when looking at the "next" button in the sq4 hintbook - bug #6447
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,        roomNr =    -1,  scriptNr = 708, inheritanceLevel = -1,   objectName =               ".", methodName = "doVerb",                          index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Floppy: happens, when looking at the "next" button... in Russian version - bug #5573
            new SciWorkaroundEntry { gameId = SciGameId.SQ5,        roomNr =   201,  scriptNr = 201, inheritanceLevel =  0,   objectName =     "buttonPanel", methodName = "doVerb",                          index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    1 } }, // when looking at the orange or red button - bug #5112
            new SciWorkaroundEntry { gameId = SciGameId.SQ6,        roomNr =    -1,  scriptNr =   0, inheritanceLevel =  0,   objectName =             "SQ6", methodName = "init",                            index =      2, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // Demo and full version: called when the game starts (demo: room 0, full: room 100)
            new SciWorkaroundEntry { gameId = SciGameId.SQ6,        roomNr =    -1, scriptNr =64950, inheritanceLevel = -1,   objectName =         "Feature", methodName = "handleEvent",                     index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // called when pressing "Start game" in the main menu, when entering the Orion's Belt bar (room 300), and perhaps other places
            new SciWorkaroundEntry { gameId = SciGameId.SQ6,        roomNr =    -1, scriptNr =64964, inheritanceLevel =  0,   objectName =           "DPath", methodName = "init",                            index =      1, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // during the game
            new SciWorkaroundEntry { gameId = SciGameId.TORIN,      roomNr =    -1, scriptNr =64017, inheritanceLevel =  0,    objectName =         "oFlags", methodName = "clear",                           index =      0, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // entering Torin's home in the French version
            new SciWorkaroundEntry { gameId = SciGameId.TORIN,     roomNr =  10000, scriptNr =64029, inheritanceLevel =  0,   objectName =          "oMessager", methodName = "nextMsg",                      index =      3, newValue = { type = SciWorkaroundType.FAKE,value =    0 } }, // start of chapter one
        };
    }
}
