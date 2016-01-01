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
            trackOrigin.objectName = curObjectName;
            trackOrigin.methodName = curMethodName;
            trackOrigin.scriptNr = curScriptNr;
            trackOrigin.localCallOffset = lastCall.debugLocalCallOffset;

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

    }
}
