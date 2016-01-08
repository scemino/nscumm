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
    enum SciWorkaroundType
    {
        NONE,      // only used by terminator or when no workaround was found
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

    struct SciWorkaroundSolution
    {
        public SciWorkaroundType type;
        public ushort value;
    }

    /// <summary>
    /// A structure describing a 'workaround' for a SCI script bug.
    /// 
    /// Arrays of SciWorkaroundEntry instances are terminated by
    /// a fake entry in which "objectName" is NULL.
    /// </summary>
    class SciWorkaroundEntry
    {
        public SciGameId gameId;
        public int roomNr;
        public int scriptNr;
        public short inheritanceLevel;
        public string objectName;
        public string methodName;
        public int localCallOffset;
        public int index;
        public SciWorkaroundSolution newValue;
    }

    internal static class Workarounds
    {
        public static SciWorkaroundSolution TrackOriginAndFindWorkaround(int index, SciWorkaroundEntry[] workaroundList, out SciTrackOriginReply trackOrigin)
        {
            trackOrigin = null;

            // HACK for SCI3: Temporarily ignore this
            if (ResourceManager.GetSciVersion() == SciVersion.V3)
            {
                // TODO: warning("SCI3 HACK: trackOriginAndFindWorkaround() called, ignoring");
                SciWorkaroundSolution sci3IgnoreForNow = new SciWorkaroundSolution
                {
                    type = SciWorkaroundType.FAKE,
                    value = 0
                };
                return sci3IgnoreForNow;
            }

            EngineState state = SciEngine.Instance.EngineState;
            ExecStack lastCall = state.xs;
            Script localScript = state._segMan.GetScriptIfLoaded(lastCall.local_segment);
            int curScriptNr = localScript.ScriptNumber;

            if (lastCall.debugLocalCallOffset != -1)
            {
                // if lastcall was actually a local call search back for a real call
                for (int i = state._executionStack.Count - 1; i > 0; i--)
                {
                    ExecStack loopCall = state._executionStack[i];
                    if ((loopCall.debugSelector != -1) || (loopCall.debugExportId != -1))
                    {
                        lastCall.debugSelector = loopCall.debugSelector;
                        lastCall.debugExportId = loopCall.debugExportId;
                        break;
                    }
                }
            }

            string curObjectName = state._segMan.GetObjectName(lastCall.sendp);
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
                short inheritanceLevel = 0;
                string searchObjectName = curObjectName;
                Register searchObject = lastCall.sendp;
                do
                {
                    foreach (var workaround in workaroundList)
                    {
                        bool objectNameMatches = (workaround.objectName == null) ||
                                                 (workaround.objectName == SciEngine.Instance.GetSciLanguageString(searchObjectName, Language.ENGLISH));

                        // Special case: in the fanmade Russian translation of SQ4, all
                        // of the object names have been deleted or renamed to Russian,
                        // thus we disable checking of the object name. Fixes bug #5573.
                        if (SciEngine.Instance.Language == Core.Common.Language.RU_RUS && SciEngine.Instance.GameId == SciGameId.SQ4)
                            objectNameMatches = true;

                        if (workaround.gameId == gameId
                                && ((workaround.scriptNr == -1) || (workaround.scriptNr == curScriptNr))
                                && ((workaround.roomNr == -1) || (workaround.roomNr == curRoomNumber))
                                && ((workaround.inheritanceLevel == -1) || (workaround.inheritanceLevel == inheritanceLevel))
                                && objectNameMatches
                                && workaround.methodName == SciEngine.Instance.GetSciLanguageString(curMethodName, Language.ENGLISH)
                                && workaround.localCallOffset == lastCall.debugLocalCallOffset
                                && ((workaround.index == -1) || (workaround.index == index)))
                        {
                            // Workaround found
                            return workaround.newValue;
                        }
                    }

                    // Go back to the parent
                    inheritanceLevel++;
                    searchObject = state._segMan.GetObject(searchObject).SuperClassSelector;
                    if (!searchObject.IsNull)
                        searchObjectName = state._segMan.GetObjectName(searchObject);
                } while (!searchObject.IsNull); // no parent left?
            }

            // give caller origin data
            trackOrigin = new SciTrackOriginReply
            {
                objectName = curObjectName,
                methodName = curMethodName,
                scriptNr = curScriptNr,
                localCallOffset = lastCall.debugLocalCallOffset
            };

            SciWorkaroundSolution noneFound = new SciWorkaroundSolution
            {
                type = SciWorkaroundType.NONE,
                value = 0
            };
            return noneFound;
        }

        //    gameID,           room,script,lvl,          object-name, method-name,    call,index,             workaround
        public static readonly SciWorkaroundEntry[] ArithmeticWorkarounds = {
            new SciWorkaroundEntry { gameId = SciGameId.CAMELOT,       roomNr =  92, scriptNr =   92, inheritanceLevel =  0,objectName=   "endingCartoon2",methodName= "changeState",localCallOffset= 0x20d,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value = 0 } }, // op_lai: during the ending, sub gets called with no parameters, uses parameter 1 which is theGrail in this case - bug #5237
	        new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST2,     roomNr = 100, scriptNr =   0,  inheritanceLevel =0,  objectName=             "Rain",methodName= "points",     localCallOffset= 0xcc6,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_or: when giving the papers to the customs officer, gets called against a pointer instead of a number - bug #4939
	        new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST2,     roomNr = 100, scriptNr =   0,  inheritanceLevel =0,  objectName=             "Rain",methodName= "points",     localCallOffset= 0xce0,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // Same as above, for the Spanish version - bug #5750
	        new SciWorkaroundEntry { gameId = SciGameId.FANMADE,       roomNr = 516, scriptNr = 983,  inheritanceLevel =0,  objectName=           "Wander",methodName= "setTarget",  localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_mul: The Legend of the Lost Jewel Demo (fan made): called with object as second parameter when attacked by insects - bug #5124
	        new SciWorkaroundEntry { gameId = SciGameId.GK1,           roomNr = 800,scriptNr =64992,  inheritanceLevel =0,  objectName=              "Fwd",methodName= "doit",       localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   1 } }, // op_gt: when Mosely finds Gabriel and Grace near the end of the game, compares the Grooper object with 7
	        new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,        roomNr = 700,scriptNr =   -1,  inheritanceLevel =1,  objectName=             "Code",methodName= "doit",       localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   1 } }, // op_add: while bidding in Bridge, an object ("Bid") is added to an object in another segment ("hand3")
	        new SciWorkaroundEntry { gameId = SciGameId.ICEMAN,        roomNr = 199,scriptNr =  977,  inheritanceLevel =0,  objectName=          "Grooper",methodName= "doit",       localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_add: While dancing with the girl
	        new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSE256,roomNr =  -1,scriptNr =  999,  inheritanceLevel =0,  objectName=            "Event",methodName= "new",        localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_and: constantly during the game (SCI1 version)
	        new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSE256,roomNr =  -1,scriptNr =    4,  inheritanceLevel =0,  objectName=            "rm004",methodName= "doit",       localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_or: when going north and reaching the castle (rooms 4 and 37) - bug #5101
	        new SciWorkaroundEntry { gameId = SciGameId.MOTHERGOOSEHIRES,roomNr =90,scriptNr =   90,  inheritanceLevel =0,  objectName=    "newGameButton",methodName= "select",     localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_ge: MUMG Deluxe, when selecting "New Game" in the main menu. It tries to compare an integer with a list. Needs to return false for the game to continue.
	        new SciWorkaroundEntry { gameId = SciGameId.PHANTASMAGORIA,roomNr = 902,scriptNr =    0,  inheritanceLevel =0,  objectName=                 "",methodName= "export 7",   localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_shr: when starting a chapter in Phantasmagoria
	        new SciWorkaroundEntry { gameId = SciGameId.QFG1VGA,       roomNr = 301,scriptNr =  928,  inheritanceLevel =0,  objectName=            "Blink",methodName= "init",       localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_div: when entering the inn, gets called with 1 parameter, but 2nd parameter is used for div which happens to be an object
	        new SciWorkaroundEntry { gameId = SciGameId.QFG2,          roomNr = 200,scriptNr =  200,  inheritanceLevel =0,  objectName=            "astro",methodName= "messages",   localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_lsi: when getting asked for your name by the astrologer - bug #5152
	        new SciWorkaroundEntry { gameId = SciGameId.QFG3,          roomNr = 780,scriptNr =  999,  inheritanceLevel =0,  objectName=                 "",methodName= "export 6",   localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   0 } }, // op_add: trying to talk to yourself at the top of the giant tree - bug #6692
	        new SciWorkaroundEntry { gameId = SciGameId.QFG4,          roomNr = 710,scriptNr =64941,  inheritanceLevel =0,  objectName=        "RandCycle",methodName= "doit",       localCallOffset=    -1,index=    0,newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.FAKE, value =   1 } }, // op_gt: when the tentacle appears in the third room of the caves
        };

        internal static SciWorkaroundSolution TrackOriginAndFindWorkaround(int v, object kDisplay_workarounds, out SciTrackOriginReply originReply)
        {
            throw new NotImplementedException();
        }

        public static readonly SciWorkaroundEntry[] kGraphDrawLine_workarounds = {
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN,roomNr =   300, scriptNr =   300, inheritanceLevel = 0, objectName =   "dudeViewer", methodName = "show",        localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.STILLCALL, value = 0 } }, // when looking at the gene explanation chart, gets called with 1 extra parameter
	        new SciWorkaroundEntry { gameId = SciGameId.SQ1,        roomNr =    43, scriptNr =    43, inheritanceLevel = 0, objectName =  "someoneDied", methodName = "changeState", localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.STILLCALL, value = 0 } }, // when ordering beer, gets called with 1 extra parameter
	        new SciWorkaroundEntry { gameId = SciGameId.SQ1,        roomNr =    71, scriptNr =    71, inheritanceLevel = 0, objectName = "destroyXenon", methodName = "changeState", localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.STILLCALL, value = 0 } }, // during the Xenon destruction cutscene (which results in death), gets called with 1 extra parameter - bug #5176
	        new SciWorkaroundEntry { gameId = SciGameId.SQ1,        roomNr =    53, scriptNr =    53, inheritanceLevel = 0, objectName =     "blastEgo", methodName = "changeState", localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution{ type = SciWorkaroundType.STILLCALL, value = 0 } }, // when Roger is found and zapped by the cleaning robot, gets called with 1 extra parameter - bug #5177
        };

        public static readonly SciWorkaroundEntry[] kSetCursor_workarounds = {
            new SciWorkaroundEntry { gameId = SciGameId.KQ5, roomNr =-1,scriptNr = 768, inheritanceLevel = 0, objectName = "KQCursor", methodName = "init", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type=SciWorkaroundType.STILLCALL,value = 0 } }, // CD: gets called with 4 additional "900d" parameters
        };

        public static readonly SciWorkaroundEntry[] kAbs_workarounds = {
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE1,  roomNr =  1, scriptNr =  1, inheritanceLevel = 0, objectName = "room1", methodName = "doit", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0x3e9 } }, // crazy eights - called with objects instead of integers
	        new SciWorkaroundEntry { gameId = SciGameId.HOYLE1,  roomNr =  2, scriptNr =  2, inheritanceLevel = 0, objectName = "room2", methodName = "doit", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0x3e9 } }, // old maid - called with objects instead of integers
	        new SciWorkaroundEntry { gameId = SciGameId.HOYLE1,  roomNr =  3, scriptNr =  3, inheritanceLevel = 0, objectName = "room3", methodName = "doit", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0x3e9 } }, // hearts - called with objects instead of integers
	        new SciWorkaroundEntry { gameId = SciGameId.QFG1VGA, roomNr = -1, scriptNr = -1, inheritanceLevel = 0, objectName =    null, methodName = "doit", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0x3e9 } }, // when the game is patched with the NRS patch
	        new SciWorkaroundEntry { gameId = SciGameId.QFG3   , roomNr = -1, scriptNr = -1, inheritanceLevel = 0, objectName =    null, methodName = "doit", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0x3e9 } }, // when the game is patched with the NRS patch - bugs #6042, #6043
        };

        public static readonly SciWorkaroundEntry[] kGraphSaveBox_workarounds = {
            new SciWorkaroundEntry { gameId = SciGameId.CASTLEBRAIN, roomNr =    420, scriptNr =   427, inheritanceLevel =  0, objectName =         "alienIcon", methodName = "select",      localCallOffset =     -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when selecting a card during the alien card game, gets called with 1 extra parameter
	        new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN, roomNr =    290, scriptNr =   291, inheritanceLevel =  0, objectName =        "upElevator", methodName = "changeState", localCallOffset = 0x201f, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when testing in the elevator puzzle, gets called with 1 argument less - 15 is on stack - bug #4943
	        new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN, roomNr =    290, scriptNr =   291, inheritanceLevel =  0, objectName =      "downElevator", methodName = "changeState", localCallOffset = 0x201f, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // see above
	        new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN, roomNr =    290, scriptNr =   291, inheritanceLevel =  0, objectName =   "correctElevator", methodName = "changeState", localCallOffset = 0x201f, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // see above (when testing the correct solution)
	        new SciWorkaroundEntry { gameId = SciGameId.PQ3,         roomNr =    202, scriptNr =   202, inheritanceLevel =  0, objectName =           "MapEdit", methodName = "movePt",      localCallOffset =     -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when plotting crimes, gets called with 2 extra parameters - bug #5099
        };

        public static SciWorkaroundEntry[] kGraphRestoreBox_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.LSL6, roomNr = -1, scriptNr = 86, inheritanceLevel = 0, objectName = "LL6Inv", methodName = "hide", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // happens during the game, gets called with 1 extra parameter
        };

        public static readonly SciWorkaroundEntry[] kGraphFillBoxForeground_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.LSL6, roomNr = -1, scriptNr = 0, inheritanceLevel = 0, objectName = "LSL6", methodName = "hideControls", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // happens when giving the bungee key to merrily (room 240) and at least in room 650 too - gets called with additional 5th parameter
        };

        public static readonly SciWorkaroundEntry[] kGraphFillBoxAny_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST2, roomNr =    100, scriptNr =   333, inheritanceLevel =  0, objectName =       "showEcorder", methodName = "changeState", localCallOffset =    -1, index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // necessary workaround for our ecorder script patch, because there isn't enough space to patch the function
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =     -1, scriptNr =   818, inheritanceLevel =  0, objectName =    "iconTextSwitch", methodName = "show",        localCallOffset =    -1, index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // CD: game menu "text/speech" display - parameter 5 is missing, but the right color number is on the stack
        };
        public static readonly SciWorkaroundEntry[] kGraphUpdateBox_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST2, roomNr =    100, scriptNr =  333, inheritanceLevel =  0, objectName =       "showEcorder", methodName = "changeState", localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // necessary workaround for our ecorder script patch, because there isn't enough space to patch the function
	        new SciWorkaroundEntry { gameId = SciGameId.PQ3,       roomNr =    202, scriptNr =  202, inheritanceLevel =  0, objectName =           "MapEdit", methodName = "addPt",       localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when plotting crimes, gets called with 2 extra parameters - bug #5099
	        new SciWorkaroundEntry { gameId = SciGameId.PQ3,       roomNr =    202, scriptNr =  202, inheritanceLevel =  0, objectName =           "MapEdit", methodName = "movePt",      localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when plotting crimes, gets called with 2 extra parameters - bug #5099
	        new SciWorkaroundEntry { gameId = SciGameId.PQ3,       roomNr =    202, scriptNr =  202, inheritanceLevel =  0, objectName =           "MapEdit", methodName = "dispose",     localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when plotting crimes, gets called with 2 extra parameters
        };
        public static readonly SciWorkaroundEntry[] kGraphRedrawBox_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    405, scriptNr =  405, inheritanceLevel = 0, objectName =      "swimAfterEgo", methodName = "changeState",  localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // skateOrama when "swimming" in the air - accidental additional parameter specified
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    406, scriptNr =  406, inheritanceLevel = 0, objectName =       "egoFollowed", methodName = "changeState",  localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // FLOPPY: when getting shot by the police - accidental additional parameter specified
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    406, scriptNr =  406, inheritanceLevel = 0, objectName =      "swimAndShoot", methodName = "changeState",  localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // skateOrama when "swimming" in the air - accidental additional parameter specified
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    410, scriptNr =  410, inheritanceLevel = 0, objectName =      "swimAfterEgo", methodName = "changeState",  localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // skateOrama when "swimming" in the air - accidental additional parameter specified
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    411, scriptNr =  411, inheritanceLevel = 0, objectName =      "swimAndShoot", methodName = "changeState",  localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // skateOrama when "swimming" in the air - accidental additional parameter specified
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    150, scriptNr =  150, inheritanceLevel = 0, objectName =       "laserScript", methodName = "changeState",  localCallOffset = 0xb2, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when visiting the pedestral where Roger Jr. is trapped, before trashing the brain icon in the programming chapter, accidental additional parameter specified - bug #5479
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =    150, scriptNr =  150, inheritanceLevel = 0, objectName =       "laserScript", methodName = "changeState",  localCallOffset = 0x16, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // same as above, for the German version - bug #5527
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,       roomNr =     -1, scriptNr =  704, inheritanceLevel = 0, objectName =          "shootEgo", methodName = "changeState",  localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // When shot by Droid in Super Computer Maze (Rooms 500, 505, 510...) - accidental additional parameter specified
	        new SciWorkaroundEntry { gameId = SciGameId.KQ5,       roomNr =     -1, scriptNr =  981, inheritanceLevel = 0, objectName =          "myWindow", methodName =     "dispose",  localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // Happens in the floppy version, when closing any dialog box, accidental additional parameter specified - bug #5031
	        new SciWorkaroundEntry { gameId = SciGameId.KQ5,       roomNr =     -1, scriptNr =  995, inheritanceLevel = 0, objectName =              "invW", methodName =        "doit",  localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // Happens in the floppy version, when closing the inventory window, accidental additional parameter specified
	        new SciWorkaroundEntry { gameId = SciGameId.KQ5,       roomNr =     -1, scriptNr =  995, inheritanceLevel = 0, objectName =                  "", methodName =    "export 0",  localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // Happens in the floppy version, when opening the gem pouch, accidental additional parameter specified - bug #5138
	        new SciWorkaroundEntry { gameId = SciGameId.KQ5,       roomNr =     -1, scriptNr =  403, inheritanceLevel = 0, objectName =         "KQ5Window", methodName =     "dispose",  localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // Happens in the FM Towns version when closing any dialog box, accidental additional parameter specified
        };
        public static readonly SciWorkaroundEntry[] kDoSoundFade_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,       roomNr =        213, scriptNr =   989, inheritanceLevel =  0, objectName =      "globalSound3", methodName = "fade",  localCallOffset =        -1, index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // english floppy: when bandits leave the secret temple, parameter 4 is an object - bug #5078
	        new SciWorkaroundEntry { gameId = SciGameId.KQ6,       roomNr =        105, scriptNr =   989, inheritanceLevel =  0, objectName =       "globalSound", methodName = "fade",  localCallOffset =        -1, index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // floppy: during intro, parameter 4 is an object
	        new SciWorkaroundEntry { gameId = SciGameId.KQ6,       roomNr =        460, scriptNr =   989, inheritanceLevel =  0, objectName =      "globalSound2", methodName = "fade",  localCallOffset =        -1, index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // after pulling the black widow's web on the isle of wonder, parameter 4 is an object - bug #4954
	        new SciWorkaroundEntry { gameId = SciGameId.QFG4,      roomNr =         -1, scriptNr = 64989, inheritanceLevel =  0, objectName =          "longSong", methodName = "fade",  localCallOffset =        -1, index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // CD version: many places, parameter 4 is an object (longSong)
	        new SciWorkaroundEntry { gameId = SciGameId.SQ5,       roomNr =        800, scriptNr =   989, inheritanceLevel =  0, objectName =         "sq5Music1", methodName = "fade",  localCallOffset =        -1, index =    0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when cutting the wrong part of Goliath with the laser - bug #6341
        };
        public static readonly SciWorkaroundEntry[] kFindKey_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST2, roomNr =     100, scriptNr =  999, inheritanceLevel = 0, objectName =           "myList", methodName = "contains", localCallOffset =       -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0 } }, // When Noah Greene gives Adam the Ecorder, and just before the game gives a demonstration, a null reference to a list is passed - bug #4987
	        new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,    roomNr =     300, scriptNr =  999, inheritanceLevel = 0, objectName =            "Piles", methodName = "contains", localCallOffset =       -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0 } }, // When passing the three cards in Hearts, a null reference to a list is passed - bug #5664
        };
        public static readonly SciWorkaroundEntry[] kDisposeScript_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.LAURABOW, roomNr =     777, scriptNr =  777, inheritanceLevel = 0, objectName =             "myStab", methodName = "changeState", localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // DEMO: after the will is signed, parameter 0 is an object - bug #4967
	        new SciWorkaroundEntry { gameId = SciGameId.QFG1,     roomNr =      -1, scriptNr =   64, inheritanceLevel = 0, objectName =              "rm64", methodName = "dispose",      localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when leaving graveyard, parameter 0 is an object
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,      roomNr =     150, scriptNr =  151, inheritanceLevel = 0, objectName =       "fightScript", methodName = "dispose",      localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // during fight with Vohaul, parameter 0 is an object
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,      roomNr =     150, scriptNr =  152, inheritanceLevel = 0, objectName =      "driveCloseUp", methodName = "dispose",      localCallOffset =   -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when choosing "beam download", parameter 0 is an object
        };
        public static readonly SciWorkaroundEntry[] kIsObject_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.GK1,         roomNr =  50, scriptNr =  999, inheritanceLevel = 0, objectName =               "List", methodName = "eachElementDo", localCallOffset = -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0 } }, // GK1 demo, when asking Grace for messages it gets called with an invalid parameter (type "error") - bug #4950
	        new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN, roomNr =  -1, scriptNr =  999, inheritanceLevel = 0, objectName =               "List", methodName = "eachElementDo", localCallOffset = -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0 } }, // when going to the game options, choosing "Info" and selecting anything from the list, gets called with an invalid parameter (type "error") - bug #4989
	        new SciWorkaroundEntry { gameId = SciGameId.QFG3,        roomNr =  -1, scriptNr =  999, inheritanceLevel = 0, objectName =               "List", methodName = "eachElementDo", localCallOffset = -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE, value = 0 } }, // when asking for something, gets called with type error parameter
        };
        public static readonly SciWorkaroundEntry[] kGetAngle_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.FANMADE,  roomNr =   516, scriptNr =  992, inheritanceLevel = 0,   objectName =          "Motion", methodName = "init",         localCallOffset  = -1,  index =  0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE,      value = 0 } }, // The Legend of the Lost Jewel Demo (fan made): called with third/fourth parameters as objects
	        new SciWorkaroundEntry { gameId = SciGameId.KQ6,      roomNr =    -1, scriptNr =  752, inheritanceLevel = 0,   objectName =     "throwDazzle", methodName = "changeState",  localCallOffset  = -1,  index =  0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // room 740/790 after the Genie is exposed in the Palace (short and long ending), it starts shooting lightning bolts around. An extra 5th parameter is passed - bug #4959 & #5203
	        new SciWorkaroundEntry { gameId = SciGameId.SQ1,      roomNr =    -1, scriptNr =  927, inheritanceLevel = 0,   objectName =        "PAvoider", methodName = "doit",         localCallOffset  = -1,  index =  0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.FAKE,      value = 0 } }, // all rooms in Ulence Flats after getting the Pilot Droid: called with a single parameter when the droid is in Roger's path - bug #6016
        };
        public static readonly SciWorkaroundEntry[] kDirLoop_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.KQ4,      roomNr =    4, scriptNr =  992, inheritanceLevel = 0,   objectName =        "Avoid", methodName = "doit",         localCallOffset  = -1,  index =  0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE,      value = 0 } }, // when the ogre catches you in front of his house, second parameter points to the same object as the first parameter, instead of being an integer (the angle) - bug #5217
        };
        public static readonly SciWorkaroundEntry[] kDeleteKey_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,    roomNr = 300,  scriptNr = 999, inheritanceLevel =  0, objectName = "handleEventList", methodName = "delete",   localCallOffset  =     -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // restarting hearts, while tray is shown - bug #6604
	        new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,    roomNr = 500,  scriptNr = 999, inheritanceLevel =  0, objectName = "handleEventList", methodName = "delete",   localCallOffset  =     -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // restarting cribbage, while tray is shown - bug #6604
	        new SciWorkaroundEntry { gameId = SciGameId.HOYLE4,    roomNr = 975,  scriptNr = 999, inheritanceLevel =  0, objectName = "handleEventList", methodName = "delete",   localCallOffset  =     -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // going back to gamelist from hearts/cribbage, while tray is shown - bug #6604
        };
        public static readonly SciWorkaroundEntry[] kDisplay_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.ISLANDBRAIN,  roomNr =  300, scriptNr =  300, inheritanceLevel = 0, objectName =   "geneDude", methodName = "show",        localCallOffset  =    -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when looking at the gene explanation chart - a parameter is an object
	        new SciWorkaroundEntry { gameId = SciGameId.PQ2,          roomNr =   23, scriptNr =   23, inheritanceLevel = 0, objectName = "rm23Script", methodName = "elements",    localCallOffset  = 0x4ae, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when looking at the 2nd page of pate's file - 0x75 as id
	        new SciWorkaroundEntry { gameId = SciGameId.PQ2,          roomNr =   23, scriptNr =   23, inheritanceLevel = 0, objectName = "rm23Script", methodName = "elements",    localCallOffset  = 0x4c1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when looking at the 2nd page of pate's file - 0x75 as id (another pq2 version, bug #5223)
	        new SciWorkaroundEntry { gameId = SciGameId.QFG1,         roomNr =   11, scriptNr =   11, inheritanceLevel = 0, objectName =     "battle", methodName = "<noname90>",  localCallOffset  =    -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // DEMO: When entering battle, 0x75 as id
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,          roomNr =  397, scriptNr =    0, inheritanceLevel = 0, objectName =           "", methodName = "export 12",   localCallOffset  =    -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // FLOPPY: when going into the computer store - bug #5227
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,          roomNr =  391, scriptNr =  391, inheritanceLevel = 0, objectName =  "doCatalog", methodName = "mode",        localCallOffset  =  0x84, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // CD: clicking on catalog in roboter sale - a parameter is an object
	        new SciWorkaroundEntry { gameId = SciGameId.SQ4,          roomNr =  391, scriptNr =  391, inheritanceLevel = 0, objectName = "choosePlug", methodName = "changeState", localCallOffset  =    -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // CD: ordering connector in roboter sale - a parameter is an object
        };
        public static readonly SciWorkaroundEntry[] kCelWide_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,     roomNr = -1, scriptNr = 255, inheritanceLevel = 0, objectName = "deathIcon", methodName = "setSize", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // english floppy: when getting beaten up in the inn and probably more, called with 2nd parameter as object - bug #5049
	        new SciWorkaroundEntry { gameId = SciGameId.PQ2,     roomNr = -1, scriptNr = 255, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when showing picture within windows, called with 2nd/3rd parameters as objects
	        new SciWorkaroundEntry { gameId = SciGameId.SQ1,     roomNr =  1, scriptNr = 255, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // DEMO: Called with 2nd/3rd parameters as objects when clicking on the menu - bug #5012
	        new SciWorkaroundEntry { gameId = SciGameId.FANMADE, roomNr = -1, scriptNr = 979, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // In The Gem Scenario and perhaps other fanmade games, this is called with 2nd/3rd parameters as objects - bug #5144
        };
        public static readonly SciWorkaroundEntry[] kCelHigh_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.KQ5,     roomNr = -1, scriptNr = 255, inheritanceLevel = 0, objectName = "deathIcon", methodName = "setSize", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // english floppy: when getting beaten up in the inn and probably more, called with 2nd parameter as object - bug #5049
	        new SciWorkaroundEntry { gameId = SciGameId.PQ2,     roomNr = -1, scriptNr = 255, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // when showing picture within windows, called with 2nd/3rd parameters as objects
	        new SciWorkaroundEntry { gameId = SciGameId.SQ1,     roomNr =  1, scriptNr = 255, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // DEMO: Called with 2nd/3rd parameters as objects when clicking on the menu - bug #5012
	        new SciWorkaroundEntry { gameId = SciGameId.FANMADE, roomNr = -1, scriptNr = 979, inheritanceLevel = 0, objectName =     "DIcon", methodName = "setSize", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // In The Gem Scenario and perhaps other fanmade games, this is called with 2nd/3rd parameters as objects - bug #5144
        };
        public static readonly SciWorkaroundEntry[] kSetPort_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.LSL6, roomNr = 740, scriptNr =  740, inheritanceLevel = 0, objectName =              "rm740", methodName = "drawPic",    localCallOffset =    -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // ending scene, is called with additional 3 (!) parameters
	        new SciWorkaroundEntry { gameId = SciGameId.QFG3, roomNr = 830, scriptNr =  830, inheritanceLevel = 0, objectName =        "portalOpens", methodName = "changeState",localCallOffset =    -1, index =   0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when the portal appears during the end, gets called with 4 parameters - bug #5174
        };
        public static readonly SciWorkaroundEntry[] kNewWindow_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.ECOQUEST, roomNr = -1, scriptNr = 981, inheritanceLevel = 0, objectName = "SysWindow", methodName = "open", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.STILLCALL, value = 0 } }, // EcoQuest 1 demo uses an in-between interpreter from SCI1 to SCI1.1. It's SCI1.1, but uses the SCI1 semantics for this call - bug #4976
        };
        public static readonly SciWorkaroundEntry[] kMoveCursor_workarounds =
        {
            new SciWorkaroundEntry { gameId = SciGameId.KQ5, roomNr =-1, scriptNr = 937, inheritanceLevel = 0, objectName = "IconBar", methodName = "handleEvent", localCallOffset = -1, index = 0, newValue = new SciWorkaroundSolution { type = SciWorkaroundType.IGNORE, value = 0 } }, // when pressing escape to open the menu, gets called with one parameter instead of 2 - bug #5575
        };
    }
}
