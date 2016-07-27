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
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Sci.Engine
{
    struct SciScriptPatcherRuntimeEntry
    {
        public bool active;
        public uint magicDWord;
        public int magicOffset;
    }

    struct SciScriptPatcherEntry
    {
        public bool defaultActive;
        public ushort scriptNr;
        public string description;
        public short applyCount;
        public ushort[] signatureData;
        public ushort[] patchData;

        public SciScriptPatcherEntry(bool defaultActive, ushort scriptNr, string description, short applyCount, ushort[] signatureData, ushort[] patchData)
        {
            this.defaultActive = defaultActive;
            this.scriptNr = scriptNr;
            this.description = description;
            this.applyCount = applyCount;
            this.signatureData = signatureData;
            this.patchData = patchData;
        }
    }

    /// <summary>
    /// ScriptPatcher class, handles on-the-fly patching of script data.
    /// </summary>
    class ScriptPatcher
    {
        // defines maximum scratch area for getting original bytes from unpatched script data
        const int PATCH_VALUELIMIT = 4096;

        private int[] _selectorIdTable;
        private SciScriptPatcherRuntimeEntry[] _runtimeTable;
        private bool _isMacSci11;

        private static readonly string[] selectorNameTable = {
            "cycles",       // system selector
            "seconds",      // system selector
            "init",         // system selector
            "dispose",      // system selector
            "new",          // system selector
            "curEvent",     // system selector
            "disable",      // system selector
            "doit",         // system selector
            "show",         // system selector
            "x",            // system selector
            "cel",          // system selector
            "setMotion",    // system selector
            "overlay",      // system selector
            "deskSarg",     // Gabriel Knight
            "localize",     // Freddy Pharkas
            "put",          // Police Quest 1 VGA
            "say",          // Quest For Glory 1 VGA
            "contains",     // Quest For Glory 2
            "solvePuzzle",  // Quest For Glory 3
            "timesShownID", // Space Quest 1 VGA
            "startText",    // King's Quest 6 CD / Laura Bow 2 CD for audio+text support
            "startAudio",   // King's Quest 6 CD / Laura Bow 2 CD for audio+text support
            "modNum",       // King's Quest 6 CD / Laura Bow 2 CD for audio+text support
            "cycler",       // Space Quest 4 / system selector
            "setLoop",      // Laura Bow 1 Colonel's Bequest
            null
        };

        // ===========================================================================
        // Conquests of Camelot
        // At the bazaar in Jerusalem, it's possible to see a girl taking a shower.
        //  If you get too close, you get warned by the father - if you don't get away,
        //  he will kill you.
        // Instead of walking there manually, it's also possible to enter "look window"
        //  and ego will automatically walk to the window. It seems that this is something
        //  that wasn't properly implemented, because instead of getting killed, you will
        //  get an "Oops" message in Sierra SCI.
        //
        // This is caused by peepingTom in script 169 not getting properly initialized.
        // peepingTom calls the object behind global b9h. This global variable is
        //  properly initialized, when walking there manually (method fawaz::doit).
        // When you instead walk there automatically (method fawaz::handleEvent), that
        //  global isn't initialized, which then results in the Oops-message in Sierra SCI
        //  and an error message in ScummVM/SCI.
        //
        // We fix the script by patching in a jump to the proper code inside fawaz::doit.
        // Responsible method: fawaz::handleEvent
        // Fixes bug: #6402
        static readonly ushort[] camelotSignaturePeepingTom = {
            0x72, Workarounds.SIG_MAGICDWORD, Workarounds.SIG_UINT16_1(0x077e),Workarounds.SIG_UINT16_2(0x077e), // lofsa fawaz <-- start of proper initializion code
            0xa1, 0xb9,                      // sag b9h
            Workarounds.SIG_ADDTOOFFSET(+571),           // skip 571 bytes
            0x39, 0x7a,                      // pushi 7a <-- initialization code when walking automatically
            0x78,                            // push1
            0x7a,                            // push2
            0x38, Workarounds.SIG_UINT16_1(0x00a9), Workarounds.SIG_UINT16_2(0x00a9), // + 0xa9, 0x00,   // pushi 00a9 - script 169
            0x78,                            // push1
            0x43, 0x02, 0x04,                // call kScriptID
            0x36,                            // push
            0x81, 0x00,                      // lag 00
            0x4a, 0x06,                      // send 06
            0x32, Workarounds.SIG_UINT16_1(0x0520), Workarounds.SIG_UINT16_2(0x0520),        // jmp [end of fawaz::handleEvent]
            Workarounds.SIG_END
        };

        static readonly ushort[] camelotPatchPeepingTom = {
            Workarounds.PATCH_ADDTOOFFSET(+576),
            0x32, Workarounds.PATCH_UINT16_1(0xfdbd), Workarounds.PATCH_UINT16_2(0xfdbd),      // jmp to fawaz::doit / properly init peepingTom code
            Workarounds.PATCH_END
        };

        static readonly SciScriptPatcherEntry[] camelotSignatures = {
            new SciScriptPatcherEntry(true, 62, "fix peepingTom Sierra bug", 1, camelotSignaturePeepingTom, camelotPatchPeepingTom),
        };


        public ScriptPatcher()
        {
            // Allocate table for selector-IDs and initialize that table as well
            _selectorIdTable = new int[selectorNameTable.Length];
            for (var selectorNr = 0; selectorNr < _selectorIdTable.Length; selectorNr++)
                _selectorIdTable[selectorNr] = -1;
        }

        public void ProcessScript(ushort scriptNr, byte[] scriptData, int scriptSize)
        {
            SciScriptPatcherEntry[] signatureTable = null;
            SciScriptPatcherEntry curEntry;
            SciScriptPatcherRuntimeEntry curRuntimeEntry;
            SciGameId gameId = SciEngine.Instance.GameId;

            switch (gameId)
            {
                case SciGameId.CAMELOT:
                    signatureTable = camelotSignatures;
                    break;
                case SciGameId.ECOQUEST:
                    throw new NotImplementedException();
                    //signatureTable = ecoquest1Signatures;
                    break;
                case SciGameId.ECOQUEST2:
                    throw new NotImplementedException();
                    //signatureTable = ecoquest2Signatures;
                    break;
                case SciGameId.FANMADE:
                    throw new NotImplementedException();
                    //signatureTable = fanmadeSignatures;
                    break;
                case SciGameId.FREDDYPHARKAS:
                    throw new NotImplementedException();
                    //signatureTable = freddypharkasSignatures;
                    break;
                case SciGameId.GK1:
                    throw new NotImplementedException();
                    //signatureTable = gk1Signatures;
                    break;
                case SciGameId.KQ5:
                    //signatureTable = kq5Signatures;
                    throw new NotImplementedException();
                    break;
                case SciGameId.KQ6:
                    throw new NotImplementedException();
                    //signatureTable = kq6Signatures;
                    break;
                case SciGameId.KQ7:
                    throw new NotImplementedException();
                    //signatureTable = kq7Signatures;
                    break;
                case SciGameId.LAURABOW:
                    throw new NotImplementedException();
                    //signatureTable = laurabow1Signatures;
                    break;
                case SciGameId.LAURABOW2:
                    throw new NotImplementedException();
                    //signatureTable = laurabow2Signatures;
                    break;
                case SciGameId.LONGBOW:
                    throw new NotImplementedException();
                    //signatureTable = longbowSignatures;
                    break;
                case SciGameId.LSL2:
                    throw new NotImplementedException();
                    //signatureTable = larry2Signatures;
                    break;
                case SciGameId.LSL5:
                    throw new NotImplementedException();
                    //signatureTable = larry5Signatures;
                    break;
                case SciGameId.LSL6:
                    throw new NotImplementedException();
                    //signatureTable = larry6Signatures;
                    break;
                case SciGameId.MOTHERGOOSE256:
                    throw new NotImplementedException();
                    //signatureTable = mothergoose256Signatures;
                    break;
                case SciGameId.PQ1:
                    throw new NotImplementedException();
                    //signatureTable = pq1vgaSignatures;
                    break;
                case SciGameId.QFG1:
                    throw new NotImplementedException();
                    //signatureTable = qfg1egaSignatures;
                    break;
                case SciGameId.QFG1VGA:
                    throw new NotImplementedException();
                    //signatureTable = qfg1vgaSignatures;
                    break;
                case SciGameId.QFG2:
                    throw new NotImplementedException();
                    //signatureTable = qfg2Signatures;
                    break;
                case SciGameId.QFG3:
                    throw new NotImplementedException();
                    //signatureTable = qfg3Signatures;
                    break;
                case SciGameId.SQ1:
                    throw new NotImplementedException();
                    //signatureTable = sq1vgaSignatures;
                    break;
                case SciGameId.SQ4:
                    throw new NotImplementedException();
                    //signatureTable = sq4Signatures;
                    break;
                case SciGameId.SQ5:
                    throw new NotImplementedException();
                    //signatureTable = sq5Signatures;
                    break;
            }

            if (signatureTable != null)
            {
                _isMacSci11 = (SciEngine.Instance.Platform == Platform.Macintosh && ResourceManager.GetSciVersion() >= SciVersion.V1_1);

                if (_runtimeTable == null)
                {
                    // Abort, in case selectors are not yet initialized (happens for games w/o selector-dictionary)
                    if (!SciEngine.Instance.Kernel.SelectorNamesAvailable)
                        return;

                    // signature table needs to get initialized (Magic DWORD set, selector table set)
                    InitSignature(signatureTable);

                    // Do additional game-specific initialization
                    switch (gameId)
                    {
                        case SciGameId.KQ5:
                            if (SciEngine.Instance._features.UseAltWinGMSound)
                            {
                                // See the explanation in the kq5SignatureWinGMSignals comment
                                EnablePatch(signatureTable, "Win: GM Music signal checks");
                            }
                            break;
                        case SciGameId.KQ6:
                            if (SciEngine.Instance.IsCD)
                            {
                                // Enables Dual mode patches (audio + subtitles at the same time) for King's Quest 6
                                EnablePatch(signatureTable, "CD: audio + text support");
                            }
                            break;
                        case SciGameId.LAURABOW2:
                            if (SciEngine.Instance.IsCD)
                            {
                                // Enables Dual mode patches (audio + subtitles at the same time) for Laura Bow 2
                                EnablePatch(signatureTable, "CD: audio + text support");
                            }
                            break;
                    }
                }

                int i = 0;
                curEntry = signatureTable[i];
                curRuntimeEntry = _runtimeTable[i];

                while (curEntry.signatureData != null)
                {
                    if ((scriptNr == curEntry.scriptNr) && (curRuntimeEntry.active))
                    {
                        int foundOffset = 0;
                        short applyCount = curEntry.applyCount;
                        do
                        {
                            foundOffset = FindSignature(curEntry, curRuntimeEntry, scriptData, (uint)scriptSize);
                            if (foundOffset != -1)
                            {
                                // found, so apply the patch
                                // TODO: debugC(kDebugLevelScriptPatcher, "Script-Patcher: '%s' on script %d offset %d", curEntry.description, scriptNr, foundOffset);
                                ApplyPatch(curEntry, scriptData, (uint)scriptSize, foundOffset);
                            }
                            applyCount--;
                        } while ((foundOffset != -1) && (applyCount != 0));
                    }
                    i++;
                    curEntry = signatureTable[i];
                    curRuntimeEntry = _runtimeTable[i];
                }
            }
        }

        public bool VerifySignature(uint byteOffset, ushort[] signatureData, string signatureDescription, BytePtr scriptData, uint scriptSize)
        {
            ushort sigSelector = 0;
            var s = signatureData;
            ushort sigWord = 0;
            int i = 0;
            while (s[i] != Workarounds.SIG_END)
            {
                sigWord = s[i];
                ushort sigCommand = (ushort)(sigWord & Workarounds.SIG_COMMANDMASK);
                ushort sigValue = (ushort)(sigWord & Workarounds.SIG_VALUEMASK);
                switch (sigCommand)
                {
                    case Workarounds.SIG_CODE_ADDTOOFFSET:
                        {
                            // add value to offset
                            byteOffset += sigValue;
                            break;
                        }
                    case Workarounds.SIG_CODE_UINT16:
                    case Workarounds.SIG_CODE_SELECTOR16:
                        {
                            if ((byteOffset + 1) < scriptSize)
                            {
                                byte byte1;
                                byte byte2;

                                switch (sigCommand)
                                {
                                    case Workarounds.SIG_CODE_UINT16:
                                        {
                                            byte1 = (byte)(sigValue & Workarounds.SIG_BYTEMASK);
                                            i++; sigWord = s[i];
                                            if ((sigWord & Workarounds.SIG_COMMANDMASK) != 0)
                                                Error("Script-Patcher: signature inconsistent\nFaulty signature: '%s'", signatureDescription);
                                            byte2 = ((byte)(sigWord & Workarounds.SIG_BYTEMASK));
                                            break;
                                        }
                                    case Workarounds.SIG_CODE_SELECTOR16:
                                        {
                                            sigSelector = (ushort)_selectorIdTable[sigValue];
                                            byte1 = (byte)(sigSelector & 0xFF);
                                            byte2 = (byte)(sigSelector >> 8);
                                            break;
                                        }
                                    default:
                                        byte1 = 0; byte2 = 0;
                                        break;
                                }
                                if (!_isMacSci11)
                                {
                                    if ((scriptData[(int)byteOffset] != byte1) || (scriptData[(int)(byteOffset + 1)] != byte2))
                                        sigWord = Workarounds.SIG_MISMATCH;
                                }
                                else
                                {
                                    // SCI1.1+ on macintosh had uint16s in script in BE-order
                                    if ((scriptData[(int)byteOffset] != byte2) || (scriptData[(int)(byteOffset + 1)] != byte1))
                                        sigWord = Workarounds.SIG_MISMATCH;
                                }
                                byteOffset += 2;
                            }
                            else
                            {
                                sigWord = Workarounds.SIG_MISMATCH;
                            }
                            break;
                        }
                    case Workarounds.SIG_CODE_SELECTOR8:
                        {
                            if (byteOffset < scriptSize)
                            {
                                sigSelector = (ushort)_selectorIdTable[sigValue];
                                if ((sigSelector & 0xFF00) != 0)
                                    Error("Script-Patcher: 8 bit selector required, game uses 16 bit selector\nFaulty signature: '%s'", signatureDescription);
                                if (scriptData[(int)byteOffset] != (sigSelector & 0xFF))
                                    sigWord = Workarounds.SIG_MISMATCH;
                                byteOffset++;
                            }
                            else
                            {
                                sigWord = Workarounds.SIG_MISMATCH; // out of bounds
                            }
                        }
                        break;
                    case Workarounds.SIG_CODE_BYTE:
                        if (byteOffset < scriptSize)
                        {
                            if (scriptData[(int)byteOffset] != sigWord)
                                sigWord = Workarounds.SIG_MISMATCH;
                            byteOffset++;
                        }
                        else
                        {
                            sigWord = Workarounds.SIG_MISMATCH; // out of bounds
                        }
                        break;
                }

                if (sigWord == Workarounds.SIG_MISMATCH)
                    break;

                i++;
                sigWord = s[i];
            }

            if (sigWord == Workarounds.SIG_END) // signature fully matched?
                return true;
            return false;
        }

        private int FindSignature(SciScriptPatcherEntry patchEntry, SciScriptPatcherRuntimeEntry runtimeEntry, byte[] scriptData, uint scriptSize)
        {
            return FindSignature(runtimeEntry.magicDWord, runtimeEntry.magicOffset, patchEntry.signatureData, patchEntry.description, scriptData, scriptSize);
        }

        private int FindSignature(uint magicDWord, int magicOffset, ushort[] signatureData, string patchDescription, byte[] scriptData, uint scriptSize)
        {
            if (scriptSize < 4) // we need to find a DWORD, so less than 4 bytes is not okay
                return -1;

            // magicDWord is in platform-specific BE/LE form, so that the later match will work, this was done for performance
            uint searchLimit = (uint)(scriptSize - 3);
            int DWordOffset = 0;
            // first search for the magic DWORD
            while (DWordOffset < searchLimit)
            {
                if (magicDWord == scriptData.ToUInt32(DWordOffset))
                {
                    // magic DWORD found, check if actual signature matches
                    int offset = (DWordOffset + magicOffset);

                    if (VerifySignature((uint)offset, signatureData, patchDescription, scriptData, scriptSize))
                        return offset;
                }
                DWordOffset++;
            }
            // nothing found
            return -1;
        }

        private void EnablePatch(SciScriptPatcherEntry[] patchTable, string searchDescription)
        {
            int i = 0;
            SciScriptPatcherEntry curEntry = patchTable[i];
            SciScriptPatcherRuntimeEntry runtimeEntry = _runtimeTable[i];
            int matchCount = 0;

            while (curEntry.signatureData != null)
            {
                if (curEntry.description == searchDescription)
                {
                    // match found, enable patch
                    runtimeEntry.active = true;
                    matchCount++;
                }
                i++;
                curEntry = patchTable[i];
            }

            if (matchCount == 0)
                Error("Script-Patcher: no patch found to enable");
        }

        // will actually patch previously found signature area
        private void ApplyPatch(SciScriptPatcherEntry patchEntry, byte[] scriptData, uint scriptSize, int signatureOffset)
        {
            int i = 0;
            var patchData = patchEntry.patchData;
            byte[] orgData = new byte[PATCH_VALUELIMIT];
            int offset = signatureOffset;
            ushort patchWord = (ushort)patchEntry.patchData[0];
            ushort patchSelector = 0;

            // Copy over original bytes from script
            uint orgDataSize = (uint)(scriptSize - offset);
            if (orgDataSize > PATCH_VALUELIMIT)
                orgDataSize = PATCH_VALUELIMIT;
            Array.Copy(scriptData, offset, orgData, 0, (int)orgDataSize);

            while (patchWord != Workarounds.PATCH_END)
            {
                ushort patchCommand = (ushort)(patchWord & Workarounds.PATCH_COMMANDMASK);
                ushort patchValue = (ushort)(patchWord & Workarounds.PATCH_VALUEMASK);
                switch (patchCommand)
                {
                    case Workarounds.PATCH_CODE_ADDTOOFFSET:
                        {
                            // add value to offset
                            offset += patchValue;
                            break;
                        }
                    case Workarounds.PATCH_CODE_GETORIGINALBYTE:
                        {
                            // get original byte from script
                            if (patchValue >= orgDataSize)
                                Error("Script-Patcher: can not get requested original byte from script");
                            scriptData[offset] = orgData[patchValue];
                            offset++;
                            break;
                        }
                    case Workarounds.PATCH_CODE_GETORIGINALBYTEADJUST:
                        {
                            // get original byte from script and adjust it
                            if (patchValue >= orgDataSize)
                                Error("Script-Patcher: can not get requested original byte from script");
                            byte orgByte = orgData[patchValue];
                            short adjustValue;
                            i++; adjustValue = (short)patchData[i];
                            scriptData[offset] = (byte)(orgByte + adjustValue);
                            offset++;
                            break;
                        }
                    case Workarounds.PATCH_CODE_UINT16:
                    case Workarounds.PATCH_CODE_SELECTOR16:
                        {
                            byte byte1;
                            byte byte2;

                            switch (patchCommand)
                            {
                                case Workarounds.PATCH_CODE_UINT16:
                                    {
                                        byte1 = (byte)(patchValue & Workarounds.PATCH_BYTEMASK);
                                        i++; patchWord = patchData[i];
                                        if ((patchWord & Workarounds.PATCH_COMMANDMASK) != 0)
                                            Error("Script-Patcher: Patch inconsistent");
                                        byte2 = (byte)(patchWord & Workarounds.PATCH_BYTEMASK);
                                        break;
                                    }
                                case Workarounds.PATCH_CODE_SELECTOR16:
                                    {
                                        patchSelector = (ushort)_selectorIdTable[patchValue];
                                        byte1 = (byte)(patchSelector & 0xFF);
                                        byte2 = (byte)(patchSelector >> 8);
                                        break;
                                    }
                                default:
                                    byte1 = 0; byte2 = 0;
                                    break;
                            }
                            if (!_isMacSci11)
                            {
                                scriptData[offset++] = byte1;
                                scriptData[offset++] = byte2;
                            }
                            else
                            {
                                // SCI1.1+ on macintosh had uint16s in script in BE-order
                                scriptData[offset++] = byte2;
                                scriptData[offset++] = byte1;
                            }
                            break;
                        }
                    case Workarounds.PATCH_CODE_SELECTOR8:
                        {
                            patchSelector = (ushort)_selectorIdTable[patchValue];
                            if ((patchSelector & 0xFF00) != 0)
                                Error("Script-Patcher: 8 bit selector required, game uses 16 bit selector");
                            scriptData[offset] = (byte)(patchSelector & 0xFF);
                            offset++;
                            break;
                        }
                    case Workarounds.PATCH_CODE_BYTE:
                        scriptData[offset] = (byte)(patchValue & Workarounds.PATCH_BYTEMASK);
                        offset++;
                        break;
                }
                i++;
                patchWord = (ushort)patchData[i];
            }
        }

        // This method calculates the magic DWORD for each entry in the signature table
        //  and it also initializes the selector table for selectors used in the signatures/patches of the current game
        private void InitSignature(SciScriptPatcherEntry[] patchTable)
        {
            int i = 0;
            int patchEntryCount = 0;

            // Count entries and allocate runtime data
            while (patchTable[i].signatureData != null)
            {
                patchEntryCount++; i++;
            }
            _runtimeTable = new SciScriptPatcherRuntimeEntry[patchEntryCount];

            i = 0;
            var curEntry = patchTable[i];
            var curRuntimeEntry = _runtimeTable[i];
            while (curEntry.signatureData != null)
            {
                // process signature
                curRuntimeEntry.active = curEntry.defaultActive;
                curRuntimeEntry.magicDWord = 0;
                curRuntimeEntry.magicOffset = 0;

                // We verify the signature data and remember the calculated magic DWord from the signature data
                CalculateMagicDWordAndVerify(curEntry.description, curEntry.signatureData, true, curRuntimeEntry.magicDWord, curRuntimeEntry.magicOffset);
                // We verify the patch data
                CalculateMagicDWordAndVerify(curEntry.description, curEntry.patchData, false, curRuntimeEntry.magicDWord, curRuntimeEntry.magicOffset);

                i++;
                curEntry = patchTable[i];
                curRuntimeEntry = _runtimeTable[i];
            }
        }

        private void CalculateMagicDWordAndVerify(string description, ushort[] signatureData, bool v, uint magicDWord, int magicOffset)
        {
            throw new NotImplementedException();
        }
    }
}
