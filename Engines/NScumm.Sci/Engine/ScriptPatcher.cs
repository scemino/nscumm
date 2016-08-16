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
            new SciScriptPatcherEntry(true, 62, "fix peepingTom Sierra bug", 1, camelotSignaturePeepingTom, camelotPatchPeepingTom)        };

        // ===========================================================================
        // stayAndHelp::changeState (0) is called when ego swims to the left or right
        //  boundaries of room 660. Normally a textbox is supposed to get on screen
        //  but the call is wrong, so not only do we get an error message the script
        //  is also hanging because the cue won't get sent out
        //  This also happens in sierra sci
        // Applies to at least: PC-CD
        // Responsible method: stayAndHelp::changeState
        // Fixes bug: #5107
        static readonly ushort[] ecoquest1SignatureStayAndHelp = {
            0x3f, 0x01,                      // link 01
            0x87, 0x01,                      // lap param[1]
            0x65, 0x14,                      // aTop state
            0x36,                            // push
            0x3c,                            // dup
            0x35, 0x00,                      // ldi 00
            0x1a,                            // eq?
            0x31, 0x1c,                      // bnt [next state]
            0x76,                            // push0
            0x45, 0x01, 0x00,                // callb export1 from script 0 (switching control off)
            Workarounds.SIG_MAGICDWORD,
            0x38, Workarounds.SIG_UINT16_1(0x0122),Workarounds.SIG_UINT16_2(0x0122),        // pushi 0122
            0x78,                            // push1
            0x76,                            // push0
            0x81, 0x00,                      // lag global[0]
            0x4a, 0x06,                      // send 06 - call ego::setMotion(0)
            0x39, Workarounds.SIG_SELECTOR8(ScriptPatcherSelectors.SELECTOR_init),       // pushi "init"
            0x39, 0x04,                      // pushi 04
            0x76,                            // push0
            0x76,                            // push0
            0x39, 0x17,                      // pushi 17
            0x7c,                            // pushSelf
            0x51, 0x82,                      // class EcoNarrator
            0x4a, 0x0c,                      // send 0c - call EcoNarrator::init(0, 0, 23, self) (BADLY BROKEN!)
            0x33,                            // jmp [end]
            Workarounds.SIG_END
        };

        static readonly ushort[] ecoquest1PatchStayAndHelp = {
            0x87, 0x01,                      // lap param[1]
            0x65, 0x14,                      // aTop state
            0x36,                            // push
            0x2f, 0x22,                      // bt [next state] (this optimization saves 6 bytes)
            0x39, 0x00,                      // pushi 0 (wasting 1 byte here)
            0x45, 0x01, 0x00,                // callb export1 from script 0 (switching control off)
            0x38, Workarounds.PATCH_UINT16_1(0x0122), Workarounds.PATCH_UINT16_2(0x0122),      // pushi 0122
            0x78,                            // push1
            0x76,                            // push0
            0x81, 0x00,                      // lag global[0]
            0x4a, 0x06,                      // send 06 - call ego::setMotion(0)
            0x39, Workarounds.PATCH_SELECTOR8(ScriptPatcherSelectors.SELECTOR_init),     // pushi "init"
            0x39, 0x06,                      // pushi 06
            0x39, 0x02,                      // pushi 02 (additional 2 bytes)
            0x76,                            // push0
            0x76,                            // push0
            0x39, 0x17,                      // pushi 17
            0x7c,                            // pushSelf
            0x38, Workarounds.PATCH_UINT16_1(0x0280), Workarounds.PATCH_UINT16_2(0x0280),      // pushi 280 (additional 3 bytes)
            0x51, 0x82,                      // class EcoNarrator
            0x4a, 0x10,                      // send 10 - call EcoNarrator::init(2, 0, 0, 23, self, 640)
            Workarounds.PATCH_END
        };

        static readonly SciScriptPatcherEntry[] ecoquest1Signatures = {
            new SciScriptPatcherEntry(  true,   660, "CD: bad messagebox and freeze",               1, ecoquest1SignatureStayAndHelp, ecoquest1PatchStayAndHelp),
        };

        // ===========================================================================
        // doMyThing::changeState (2) is supposed to remove the initial text on the
        //  ecorder. This is done by reusing temp-space, that was filled on state 1.
        //  this worked in sierra sci just by accident. In our sci, the temp space
        //  is resetted every time, which means the previous text isn't available
        //  anymore. We have to patch the code because of that.
        // Fixes bug: #4993
        static readonly ushort[] ecoquest2SignatureEcorder = {
            0x31, 0x22,                      // bnt [next state]
            0x39, 0x0a,                      // pushi 0a
            0x5b, 0x04, 0x1e,                // lea temp[1e]
            0x36,                            // push
            Workarounds.SIG_MAGICDWORD,
            0x39, 0x64,                      // pushi 64
            0x39, 0x7d,                      // pushi 7d
            0x39, 0x32,                      // pushi 32
            0x39, 0x66,                      // pushi 66
            0x39, 0x17,                      // pushi 17
            0x39, 0x69,                      // pushi 69
            0x38, Workarounds.PATCH_UINT16_1(0x2631), Workarounds.PATCH_UINT16_2(0x2631),      // pushi 2631
            0x39, 0x6a,                      // pushi 6a
            0x39, 0x64,                      // pushi 64
            0x43, 0x1b, 0x14,                // call kDisplay
            0x35, 0x0a,                      // ldi 0a
            0x65, 0x20,                      // aTop ticks
            0x33,                            // jmp [end]
            Workarounds.SIG_ADDTOOFFSET(+1),             // [skip 1 byte]
            0x3c,                            // dup
            0x35, 0x03,                      // ldi 03
            0x1a,                            // eq?
            0x31,                            // bnt [end]
            Workarounds.SIG_END
        };

        static readonly ushort[] ecoquest2PatchEcorder = {
            0x2f, 0x02,                      // bt [to pushi 07]
            0x3a,                            // toss
            0x48,                            // ret
            0x38, Workarounds.PATCH_UINT16_1(0x0007), Workarounds.PATCH_UINT16_2(0x0007),      // pushi 07 (parameter count) (waste 1 byte)
            0x39, 0x0b,                      // push (FillBoxAny)
            0x39, 0x1d,                      // pushi 29d
            0x39, 0x73,                      // pushi 115d
            0x39, 0x5e,                      // pushi 94d
            0x38, Workarounds.PATCH_UINT16_1(0x00d7), Workarounds.PATCH_UINT16_2(0x00d7),      // pushi 215d
            0x78,                            // push1 (visual screen)
            0x38, Workarounds.PATCH_UINT16_1(0x0017), Workarounds.PATCH_UINT16_2(0x0017),      // pushi 17 (color) (waste 1 byte)
            0x43, 0x6c, 0x0e,                // call kGraph
            0x38, Workarounds.PATCH_UINT16_1(0x0005), Workarounds.PATCH_UINT16_2(0x0005),      // pushi 05 (parameter count) (waste 1 byte)
            0x39, 0x0c,                      // pushi 12d (UpdateBox)
            0x39, 0x1d,                      // pushi 29d
            0x39, 0x73,                      // pushi 115d
            0x39, 0x5e,                      // pushi 94d
            0x38, Workarounds.PATCH_UINT16_1(0x00d7),Workarounds.PATCH_UINT16_2(0x00d7),      // pushi 215d
            0x43, 0x6c, 0x0a,                // call kGraph
            Workarounds.PATCH_END
        };

        // ===========================================================================
        // Same patch as above for the ecorder introduction.
        // Two workarounds are needed for this patch in workarounds.cpp (when calling
        // kGraphFillBoxAny and kGraphUpdateBox), as there isn't enough space to patch
        // the function otherwise.
        // Fixes bug: #6467
        static readonly ushort[] ecoquest2SignatureEcorderTutorial = {
            0x30, Workarounds.SIG_UINT16_1(0x0023), Workarounds.SIG_UINT16_2(0x0023),        // bnt [next state]
            0x39, 0x0a,                      // pushi 0a
            0x5b, 0x04, 0x1f,                // lea temp[1f]
            0x36,                            // push
            Workarounds.SIG_MAGICDWORD,
            0x39, 0x64,                      // pushi 64
            0x39, 0x7d,                      // pushi 7d
            0x39, 0x32,                      // pushi 32
            0x39, 0x66,                      // pushi 66
            0x39, 0x17,                      // pushi 17
            0x39, 0x69,                      // pushi 69
            0x38, Workarounds.SIG_UINT16_1(0x2631), Workarounds.SIG_UINT16_2(0x2631),        // pushi 2631
            0x39, 0x6a,                      // pushi 6a
            0x39, 0x64,                      // pushi 64
            0x43, 0x1b, 0x14,                // call kDisplay
            0x35, 0x1e,                      // ldi 1e
            0x65, 0x20,                      // aTop ticks
            0x32,                            // jmp [end]
            // 2 extra bytes, jmp offset
            Workarounds.SIG_END
        };

        static readonly ushort[] ecoquest2PatchEcorderTutorial = {
            0x31, 0x23,                      // bnt [next state] (save 1 byte)
            // The parameter count below should be 7, but we're out of bytes
            // to patch! A workaround has been added because of this
            0x78,                            // push1 (parameter count)
            //0x39, 0x07,                    // pushi 07 (parameter count)
            0x39, 0x0b,                      // push (FillBoxAny)
            0x39, 0x1d,                      // pushi 29d
            0x39, 0x73,                      // pushi 115d
            0x39, 0x5e,                      // pushi 94d
            0x38, Workarounds.PATCH_UINT16_1(0x00d7), Workarounds.PATCH_UINT16_2(0x00d7),      // pushi 215d
            0x78,                            // push1 (visual screen)
            0x39, 0x17,                      // pushi 17 (color)
            0x43, 0x6c, 0x0e,                // call kGraph
            // The parameter count below should be 5, but we're out of bytes
            // to patch! A workaround has been added because of this
            0x78,                            // push1 (parameter count)
            //0x39, 0x05,                    // pushi 05 (parameter count)
            0x39, 0x0c,                      // pushi 12d (UpdateBox)
            0x39, 0x1d,                      // pushi 29d
            0x39, 0x73,                      // pushi 115d
            0x39, 0x5e,                      // pushi 94d
            0x38, Workarounds.PATCH_UINT16_1(0x00d7), Workarounds.PATCH_UINT16_2(0x00d7),      // pushi 215d
            0x43, 0x6c, 0x0a,                // call kGraph
            // We are out of bytes to patch at this point,
            // so we skip 494 (0x1EE) bytes to reuse this code:
            // ldi 1e
            // aTop 20
            // jmp 030e (jump to end)
            0x32, Workarounds.PATCH_UINT16_1(0x01ee), Workarounds.PATCH_UINT16_2(0x01ee),      // skip 494 (0x1EE) bytes
            Workarounds.PATCH_END
        };

        //          script, description,                                       signature                          patch
        static readonly SciScriptPatcherEntry[] ecoquest2Signatures = {
            new SciScriptPatcherEntry(  true,    50, "initial text not removed on ecorder",          1, ecoquest2SignatureEcorder,         ecoquest2PatchEcorder),
            new SciScriptPatcherEntry(  true,   333, "initial text not removed on ecorder tutorial", 1, ecoquest2SignatureEcorderTutorial, ecoquest2PatchEcorderTutorial),
        };

        // ===========================================================================
        // Fan-made games
        // Attention: Try to make script patches as specific as possible

        // CascadeQuest::autosave in script 994 is called various times to auto-save the game.
        // The script use a fixed slot "999" for this purpose. This doesn't work in ScummVM, because we do not let
        //  scripts save directly into specific slots, but instead use virtual slots / detect scripts wanting to
        //  create a new slot.
        //
        // For this game we patch the code to use slot 99 instead. kSaveGame also checks for Cascade Quest,
        //  will then check, if slot 99 is asked for and will then use the actual slot 0, which is the official
        //  ScummVM auto-save slot.
        //
        // Responsible method: CascadeQuest::autosave
        // Fixes bug: #7007
        static readonly ushort[] fanmadeSignatureCascadeQuestFixAutoSaving = {
            Workarounds.SIG_MAGICDWORD,
            0x38, Workarounds.SIG_UINT16_1(0x03e7), Workarounds.SIG_UINT16_2(0x03e7),        // pushi 3E7 (999d) -> save game slot 999
            0x74, Workarounds.SIG_UINT16_1(0x06f8), Workarounds.SIG_UINT16_2(0x06f8),        // lofss "AutoSave"
            0x89, 0x1e,                      // lsg global[1E]
            0x43, 0x2d, 0x08,                // callk SaveGame
            Workarounds.SIG_END
        };

        static readonly ushort[] fanmadePatchCascadeQuestFixAutoSaving = {
            0x38, Workarounds.PATCH_UINT16_1(Workarounds.SAVEGAMEID_OFFICIALRANGE_START - 1), Workarounds.PATCH_UINT16_2(Workarounds.SAVEGAMEID_OFFICIALRANGE_START - 1), // fix slot
            Workarounds.PATCH_END
        };

        // EventHandler::handleEvent in Demo Quest has a bug, and it jumps to the
        // wrong address when an incorrect word is typed, therefore leading to an
        // infinite loop. This script bug was not apparent in SSCI, probably because
        // event handling was slightly different there, so it was never discovered.
        // Fixes bug: #5120
        static readonly ushort[] fanmadeSignatureDemoQuestInfiniteLoop = {
            0x38, Workarounds.SIG_UINT16_1(0x004c), Workarounds.SIG_UINT16_2(0x004c),        // pushi 004c
            0x39, 0x00,                      // pushi 00
            0x87, 0x01,                      // lap 01
            0x4b, 0x04,                      // send 04
            Workarounds.SIG_MAGICDWORD,
            0x18,                            // not
            0x30, Workarounds.SIG_UINT16_1(0x002f), Workarounds.SIG_UINT16_2(0x002f),        // bnt 002f  [06a5]    --> jmp ffbc  [0664] --> BUG! infinite loop
            Workarounds.SIG_END
        };

        static readonly ushort[] fanmadePatchDemoQuestInfiniteLoop = {
            Workarounds.PATCH_ADDTOOFFSET(+10),
            0x30, Workarounds.PATCH_UINT16_1(0x0032), Workarounds.PATCH_UINT16_2(0x0032),      // bnt 0032  [06a8] --> pushi 004c
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                                  patch
        static readonly SciScriptPatcherEntry[] fanmadeSignatures = {
            new SciScriptPatcherEntry(  true,   994, "Cascade Quest: fix auto-saving",              1, fanmadeSignatureCascadeQuestFixAutoSaving, fanmadePatchCascadeQuestFixAutoSaving ),
            new SciScriptPatcherEntry(  true,   999, "Demo Quest: infinite loop on typo",           1, fanmadeSignatureDemoQuestInfiniteLoop,     fanmadePatchDemoQuestInfiniteLoop ),
        };


        // WORKAROUND
        // Freddy Pharkas intro screen
        // Sierra used inner loops for the scaling of the 2 title views.
        // Those inner loops don't call kGameIsRestarting, which is why
        // we do not update the screen and we also do not throttle.
        //
        // This patch fixes this and makes it work.
        // Applies to at least: English PC-CD
        // Responsible method: sTownScript::changeState(1), sTownScript::changeState(3) (script 110)
        static readonly ushort[] freddypharkasSignatureIntroScaling = {
            0x38, Workarounds.SIG_ADDTOOFFSET(+2),       // pushi (setLoop) (009b for PC CD)
            0x78,                            // push1
            Workarounds.PATCH_ADDTOOFFSET(1),            // push0 for first code, push1 for second code
            0x38, Workarounds.SIG_ADDTOOFFSET(+2),       // pushi (setStep) (0143 for PC CD)
            0x7a,                            // push2
            0x39, 0x05,                      // pushi 05
            0x3c,                            // dup
            0x72, Workarounds.SIG_ADDTOOFFSET(+2),       // lofsa (view)
            Workarounds.SIG_MAGICDWORD,
            0x4a, 0x1e,                      // send 1e
            0x35, 0x0a,                      // ldi 0a
            0xa3, 0x02,                      // sal local[2]
            // start of inner loop
            0x8b, 0x02,                      // lsl local[2]
            Workarounds.SIG_ADDTOOFFSET(+43),            // skip almost all of inner loop
            0xa3, 0x02,                      // sal local[2]
            0x33, 0xcf,                      // jmp [inner loop start]
            Workarounds.SIG_END
        };

        static readonly ushort[] freddypharkasPatchIntroScaling = {
            // remove setLoop(), objects in heap are already prepared, saves 5 bytes
            0x38,
            Workarounds.PATCH_GETORIGINALBYTE(+6),
            Workarounds.PATCH_GETORIGINALBYTE(+7),       // pushi (setStep)
            0x7a,                            // push2
            0x39, 0x05,                      // pushi 05
            0x3c,                            // dup
            0x72,
            Workarounds.PATCH_GETORIGINALBYTE(+13),
            Workarounds.PATCH_GETORIGINALBYTE(+14),      // lofsa (view)
            0x4a, 0x18,                      // send 18 - adjusted
            0x35, 0x0a,                      // ldi 0a
            0xa3, 0x02,                      // sal local[2]
            // start of new inner loop
            0x39, 0x00,                      // pushi 00
            0x43, 0x2c, 0x00,                // callk GameIsRestarting <-- add this so that our speed throttler is triggered
            Workarounds.SIG_ADDTOOFFSET(+47),            // skip almost all of inner loop
            0x33, 0xca,                      // jmp [inner loop start]
            Workarounds.PATCH_END
        };

        //  script 0 of freddy pharkas/CD PointsSound::check waits for a signal and if
        //   no signal received will call kDoSound(0xD) which is a dummy in sierra sci
        //   and ScummVM and will use acc (which is not set by the dummy) to trigger
        //   sound disposal. This somewhat worked in sierra sci, because the sample
        //   was already playing in the sound driver. In our case we would also stop
        //   the sample from playing, so we patch it out
        //   The "score" code is already buggy and sets volume to 0 when playing
        // Applies to at least: English PC-CD
        // Responsible method: unknown
        static readonly ushort[] freddypharkasSignatureScoreDisposal = {
            0x67, 0x32,                      // pTos 32 (selector theAudCount)
            0x78,                            // push1
            Workarounds.SIG_MAGICDWORD,
            0x39, 0x0d,                      // pushi 0d
            0x43, 0x75, 0x02,                // call kDoAudio
            0x1c,                            // ne?
            0x31,                            // bnt (-> to skip disposal)
            Workarounds.SIG_END
        };

        static readonly ushort[] freddypharkasPatchScoreDisposal = {
            0x34, Workarounds.PATCH_UINT16_1(0x0000), Workarounds.PATCH_UINT16_2(0x0000),      // ldi 0000
            0x34, Workarounds.PATCH_UINT16_1(0x0000), Workarounds.PATCH_UINT16_2(0x0000),     // ldi 0000
            0x34, Workarounds.PATCH_UINT16_1(0x0000), Workarounds.PATCH_UINT16_2(0x0000),     // ldi 0000
            Workarounds.PATCH_END
        };

        //  script 235 of freddy pharkas rm235::init and sEnterFrom500::changeState
        //   disable icon 7+8 of iconbar (CD only). When picking up the canister after
        //   placing it down, the scripts will disable all the other icons. This results
        //   in IconBar::disable doing endless loops even in sierra sci, because there
        //   is no enabled icon left. We remove disabling of icon 8 (which is help),
        //   this fixes the issue.
        // Applies to at least: English PC-CD
        // Responsible method: rm235::init and sEnterFrom500::changeState
        static readonly ushort[] freddypharkasSignatureCanisterHang = {
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_disable),   // pushi disable
            0x7a,                            // push2
            Workarounds.SIG_MAGICDWORD,
            0x39, 0x07,                      // pushi 07
            0x39, 0x08,                      // pushi 08
            0x81, 0x45,                      // lag 45
            0x4a, 0x08,                      // send 08 - call IconBar::disable(7, 8)
            Workarounds.SIG_END
        };

        static readonly ushort[] freddypharkasPatchCanisterHang = {
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0x78,                            // push1
            Workarounds.PATCH_ADDTOOFFSET(+2),
            0x33, 0x00,                      // ldi 00 (waste 2 bytes)
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0x06,                            // send 06 - call IconBar::disable(7)
            Workarounds.PATCH_END
        };

        //  script 215 of freddy pharkas lowerLadder::doit and highLadder::doit actually
        //   process keyboard-presses when the ladder is on the screen in that room.
        //   They strangely also call kGetEvent. Because the main User::doit also calls
        //   kGetEvent, it's pure luck, where the event will hit. It's the same issue
        //   as in QfG1VGA and if you turn dos-box to max cycles, and click around for
        //   ego, sometimes clicks also won't get registered. Strangely it's not nearly
        //   as bad as in our sci, but these differences may be caused by timing.
        //   We just reuse the active event, thus removing the duplicate kGetEvent call.
        // Applies to at least: English PC-CD, German Floppy, English Mac
        // Responsible method: lowerLadder::doit and highLadder::doit
        static readonly ushort[] freddypharkasSignatureLadderEvent = {
            0x39, Workarounds.SIG_MAGICDWORD,
            Workarounds.SIG_SELECTOR8(ScriptPatcherSelectors.SELECTOR_new),              // pushi new
            0x76,                            // push0
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_curEvent),  // pushi curEvent
            0x76,                            // push0
            0x81, 0x50,                      // lag global[50]
            0x4a, 0x04,                      // send 04 - read User::curEvent
            0x4a, 0x04,                      // send 04 - call curEvent::new
            0xa5, 0x00,                      // sat temp[0]
            0x38, Workarounds.SIG_SELECTOR16( ScriptPatcherSelectors.SELECTOR_localize),
            0x76,                            // push0
            0x4a, 0x04,                      // send 04 - call curEvent::localize
            Workarounds.SIG_END
        };

        static readonly ushort[] freddypharkasPatchLadderEvent = {
            0x34, 0x00, 0x00,                // ldi 0000 (waste 3 bytes, overwrites first 2 pushes)
            Workarounds.PATCH_ADDTOOFFSET(+8),
            0xa5, 0x00,                      // sat temp[0] (waste 2 bytes, overwrites 2nd send)
            Workarounds.PATCH_ADDTOOFFSET(+2),
            0x34, 0x00, 0x00,                // ldi 0000
            0x34, 0x00, 0x00,                // ldi 0000 (waste 6 bytes, overwrites last 3 opcodes)
            Workarounds.PATCH_END
        };

        // In the Macintosh version of Freddy Pharkas, kRespondsTo is broken for
        // property selectors. They hacked the script to work around the issue,
        // so we revert the script back to using the values of the DOS script.
        // Applies to at least: English Mac
        // Responsible method: unknown
        static readonly ushort[] freddypharkasSignatureMacInventory = {
            Workarounds.SIG_MAGICDWORD,
            0x39, 0x23,                      // pushi 23
            0x39, 0x74,                      // pushi 74
            0x78,                            // push1
            0x38, Workarounds.SIG_UINT16_1(0x0174), Workarounds.SIG_UINT16_2(0x0174),        // pushi 0174 (on mac it's actually 0x01, 0x74)
            0x85, 0x15,                      // lat 15
            Workarounds.SIG_END
        };

        static readonly ushort[] freddypharkasPatchMacInventory = {
            0x39, 0x02,                      // pushi 02 (now matches the DOS version)
            Workarounds.PATCH_ADDTOOFFSET(+23),
            0x39, 0x04,                      // pushi 04 (now matches the DOS version)
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                            patch
        static readonly SciScriptPatcherEntry[] freddypharkasSignatures = {
            new SciScriptPatcherEntry(  true,     0, "CD: score early disposal",                    1, freddypharkasSignatureScoreDisposal, freddypharkasPatchScoreDisposal ),
            new SciScriptPatcherEntry(  true,    15, "Mac: broken inventory",                       1, freddypharkasSignatureMacInventory,  freddypharkasPatchMacInventory ),
            new SciScriptPatcherEntry(  true,   110, "intro scaling workaround",                    2, freddypharkasSignatureIntroScaling,  freddypharkasPatchIntroScaling ),
            new SciScriptPatcherEntry(  true,   235, "CD: canister pickup hang",                    3, freddypharkasSignatureCanisterHang,  freddypharkasPatchCanisterHang ),
            new SciScriptPatcherEntry(  true,   320, "ladder event issue",                          2, freddypharkasSignatureLadderEvent,   freddypharkasPatchLadderEvent ),
        };

        // ===========================================================================
        // daySixBeignet::changeState (4) is called when the cop goes out and sets cycles to 220.
        //  this is not enough time to get to the door, so we patch that to 23 seconds
        // Applies to at least: English PC-CD, German PC-CD, English Mac
        // Responsible method: daySixBeignet::changeState
        static readonly ushort[] gk1SignatureDay6PoliceBeignet = {
            0x35, 0x04,                         // ldi 04
            0x1a,                               // eq?
            0x30, Workarounds.SIG_ADDTOOFFSET(+2),          // bnt [next state check]
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_dispose),      // pushi dispose
            0x76,                               // push0
            0x72, Workarounds.SIG_ADDTOOFFSET(+2),          // lofsa deskSarg
            0x4a, Workarounds.SIG_UINT16_1(0x0004), Workarounds.SIG_UINT16_2(0x0004),           // send 04
            Workarounds.SIG_MAGICDWORD,
            0x34, Workarounds.SIG_UINT16_1(0x00dc), Workarounds.SIG_UINT16_2(0x00dc),           // ldi 220
            0x65, Workarounds.SIG_ADDTOOFFSET(+1),          // aTop cycles (1a for PC, 1c for Mac)
            0x32,                               // jmp [end]
            Workarounds.SIG_END
        };

        static readonly ushort[] gk1PatchDay6PoliceBeignet = {
            Workarounds.PATCH_ADDTOOFFSET(+16),
            0x34, Workarounds.PATCH_UINT16_1(0x0017), Workarounds.PATCH_UINT16_2(0x0017),         // ldi 23
            0x65, Workarounds.PATCH_GETORIGINALBYTEADJUST_1(+20), Workarounds.PATCH_GETORIGINALBYTEADJUST_1(+2), // aTop seconds (1c for PC, 1e for Mac)
            Workarounds.PATCH_END
        };

        // sargSleeping::changeState (8) is called when the cop falls asleep and sets cycles to 220.
        //  this is not enough time to get to the door, so we patch it to 42 seconds
        // Applies to at least: English PC-CD, German PC-CD, English Mac
        // Responsible method: sargSleeping::changeState
        static readonly ushort[] gk1SignatureDay6PoliceSleep = {
            0x35, 0x08,                         // ldi 08
            0x1a,                               // eq?
            0x31, Workarounds.SIG_ADDTOOFFSET(+1),          // bnt [next state check]
            Workarounds.SIG_MAGICDWORD,
            0x34, Workarounds.SIG_UINT16_1(0x00dc), Workarounds.SIG_UINT16_2(0x00dc),           // ldi 220
            0x65, Workarounds.SIG_ADDTOOFFSET(+1),          // aTop cycles (1a for PC, 1c for Mac)
            0x32,                               // jmp [end]
            Workarounds.SIG_END
        };

        static readonly ushort[] gk1PatchDay6PoliceSleep = {
            Workarounds.PATCH_ADDTOOFFSET(+5),
            0x34, Workarounds.PATCH_UINT16_1(0x002a), Workarounds.PATCH_UINT16_2(0x002a),         // ldi 42
            0x65, Workarounds.PATCH_GETORIGINALBYTEADJUST_1(+9), Workarounds.PATCH_GETORIGINALBYTEADJUST_2(+2), // aTop seconds (1c for PC, 1e for Mac)
            Workarounds.PATCH_END
        };

        // startOfDay5::changeState (20h) - when gabriel goes to the phone the script will hang
        // Applies to at least: English PC-CD, German PC-CD, English Mac
        // Responsible method: startOfDay5::changeState
        static readonly ushort[] gk1SignatureDay5PhoneFreeze = {
            0x4a,
            Workarounds.SIG_MAGICDWORD, Workarounds.SIG_UINT16_1(0x000c), Workarounds.SIG_UINT16_2(0x000c), // send 0c
            0x35, 0x03,                         // ldi 03
            0x65, Workarounds.SIG_ADDTOOFFSET(+1),          // aTop cycles
            0x32, Workarounds.SIG_ADDTOOFFSET(+2),          // jmp [end]
            0x3c,                               // dup
            0x35, 0x21,                         // ldi 21
            Workarounds.SIG_END
        };

        static readonly ushort[] gk1PatchDay5PhoneFreeze = {
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0x35, 0x06,                         // ldi 01
            0x65, Workarounds.PATCH_GETORIGINALBYTEADJUST_1(+6), Workarounds.PATCH_GETORIGINALBYTEADJUST_2(+6), // aTop ticks
            Workarounds.PATCH_END
        };

        // Floppy version: Interrogation::dispose() compares an object reference
        // (stored in the view selector) with a number, leading to a crash (this kind
        // of comparison was not used in SCI32). The view selector is used to store
        // both a view number (in some cases), and a view reference (in other cases).
        // In the floppy version, the checks are in the wrong order, so there is a
        // comparison between a number an an object. In the CD version, the checks are
        // in the correct order, thus the comparison is correct, thus we use the code
        // from the CD version in the floppy one.
        // Applies to at least: English Floppy
        // Responsible method: Interrogation::dispose
        // TODO: Check, if English Mac is affected too and if this patch applies
        static readonly ushort[] gk1SignatureInterrogationBug = {
            Workarounds.SIG_MAGICDWORD,
            0x65, 0x4c,                      // aTop 4c
            0x67, 0x50,                      // pTos 50
            0x34, Workarounds.SIG_UINT16_1(0x2710), Workarounds.SIG_UINT16_2(0x2710),        // ldi 2710
            0x1e,                            // gt?
            0x31, 0x08,                      // bnt 08  [05a0]
            0x67, 0x50,                      // pTos 50
            0x34, Workarounds.SIG_UINT16_1(0x2710), Workarounds.SIG_UINT16_2(0x2710),        // ldi 2710
            0x04,                            // sub
            0x65, 0x50,                      // aTop 50
            0x63, 0x50,                      // pToa 50
            0x31, 0x15,                      // bnt 15  [05b9]
            0x39, 0x0e,                      // pushi 0e
            0x76,                            // push0
            0x4a, Workarounds.SIG_UINT16_1(0x0004), Workarounds.SIG_UINT16_2(0x0004),        // send 0004
            0xa5, 0x00,                      // sat 00
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_dispose),   // pushi dispose
            0x76,                            // push0
            0x63, 0x50,                      // pToa 50
            0x4a, Workarounds.SIG_UINT16_1(0x0004), Workarounds.SIG_UINT16_2(0x0004),        // send 0004
            0x85, 0x00,                      // lat 00
            0x65, 0x50,                      // aTop 50
            Workarounds.SIG_END
        };

        static readonly ushort[] gk1PatchInterrogationBug = {
            0x65, 0x4c,                      // aTop 4c
            0x63, 0x50,                      // pToa 50
            0x31, 0x15,                      // bnt 15  [05b9]
            0x39, 0x0e,                      // pushi 0e
            0x76,                            // push0
            0x4a, 0x04, 0x00,                // send 0004
            0xa5, 0x00,                      // sat 00
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_dispose),   // pushi dispose
            0x76,                            // push0
            0x63, 0x50,                      // pToa 50
            0x4a, 0x04, 0x00,                // send 0004
            0x85, 0x00,                      // lat 00
            0x65, 0x50,                      // aTop 50
            0x67, 0x50,                      // pTos 50
            0x34, Workarounds.PATCH_UINT16_1(0x2710), Workarounds.PATCH_UINT16_2(0x2710),      // ldi 2710
            0x1e,                            // gt?
            0x31, 0x08,                      // bnt 08  [05b9]
            0x67, 0x50,                      // pTos 50
            0x34, Workarounds.PATCH_UINT16_1(0x2710), Workarounds.PATCH_UINT16_2(0x2710),      // ldi 2710
            0x04,                            // sub
            0x65, 0x50,                      // aTop 50
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                     patch
        static readonly SciScriptPatcherEntry[] gk1Signatures = {
            new SciScriptPatcherEntry(  true,    51, "interrogation bug",                           1, gk1SignatureInterrogationBug, gk1PatchInterrogationBug ),
            new SciScriptPatcherEntry(  true,   212, "day 5 phone freeze",                          1, gk1SignatureDay5PhoneFreeze, gk1PatchDay5PhoneFreeze ),
            new SciScriptPatcherEntry(  true,   230, "day 6 police beignet timer issue",            1, gk1SignatureDay6PoliceBeignet, gk1PatchDay6PoliceBeignet ),
            new SciScriptPatcherEntry(  true,   230, "day 6 police sleep timer issue",              1, gk1SignatureDay6PoliceSleep, gk1PatchDay6PoliceSleep ),
        };

        // ===========================================================================
        // at least during harpy scene export 29 of script 0 is called in kq5cd and
        //  has an issue for those calls, where temp 3 won't get inititialized, but
        //  is later used to set master volume. This issue makes sierra sci set
        //  the volume to max. We fix the export, so volume won't get modified in
        //  those cases.
        static readonly ushort[] kq5SignatureCdHarpyVolume = {
            Workarounds.SIG_MAGICDWORD,
            0x80, Workarounds.SIG_UINT16_1(0x0191), Workarounds.SIG_UINT16_2(0x0191),        // lag global[191h]
            0x18,                            // not
            0x30, Workarounds.SIG_UINT16_1(0x002c), Workarounds.SIG_UINT16_2(0x002c),        // bnt [jump further] (jumping, if global 191h is 1)
            0x35, 0x01,                      // ldi 01
            0xa0, Workarounds.SIG_UINT16_1(0x0191), Workarounds.SIG_UINT16_2(0x0191),        // sag global[191h] (setting global 191h to 1)
            0x38, Workarounds.SIG_UINT16_1(0x017b), Workarounds.SIG_UINT16_2(0x017b),        // pushi 017b
            0x76,                            // push0
            0x81, 0x01,                      // lag global[1]
            0x4a, 0x04,                      // send 04 - read KQ5::masterVolume
            0xa5, 0x03,                      // sat temp[3] (store volume in temp 3)
            0x38, Workarounds.SIG_UINT16_1(0x017b), Workarounds.SIG_UINT16_2(0x017b),        // pushi 017b
            0x76,                            // push0
            0x81, 0x01,                      // lag global[1]
            0x4a, 0x04,                      // send 04 - read KQ5::masterVolume
            0x36,                            // push
            0x35, 0x04,                      // ldi 04
            0x20,                            // ge? (followed by bnt)
            Workarounds.SIG_END
        };

        static readonly ushort[] kq5PatchCdHarpyVolume = {
            0x38, Workarounds.PATCH_UINT16_1(0x022f), Workarounds.PATCH_UINT16_2(0x022f),      // pushi 022f (selector theVol) (3 new bytes)
            0x76,                            // push0 (1 new byte)
            0x51, 0x88,                      // class SpeakTimer (2 new bytes)
            0x4a, 0x04,                      // send 04 (2 new bytes) -> read SpeakTimer::theVol
            0xa5, 0x03,                      // sat temp[3] (2 new bytes) -> write to temp 3
            0x80, Workarounds.PATCH_UINT16_1(0x0191), Workarounds.PATCH_UINT16_2(0x0191),      // lag global[191h]
            // saving 1 byte due optimization
            0x2e, Workarounds.PATCH_UINT16_1(0x0023), Workarounds.PATCH_UINT16_2(0x0023),      // bt [jump further] (jumping, if global 191h is 1)
            0x35, 0x01,                      // ldi 01
            0xa0, Workarounds.PATCH_UINT16_1(0x0191), Workarounds.PATCH_UINT16_2(0x0191),      // sag global[191h] (setting global 191h to 1)
            0x38, Workarounds.PATCH_UINT16_1(0x017b), Workarounds.PATCH_UINT16_2(0x017b),      // pushi 017b
            0x76,                            // push0
            0x81, 0x01,                      // lag global[1]
            0x4a, 0x04,                      // send 04 - read KQ5::masterVolume
            0xa5, 0x03,                      // sat temp[3] (store volume in temp 3)
            // saving 8 bytes due removing of duplicate code
            0x39, 0x04,                      // pushi 04 (saving 1 byte due swapping)
            0x22,                            // lt? (because we switched values)
            Workarounds.PATCH_END
        };

        // This is a heap patch, and it modifies the properties of an object, instead
        // of patching script code.
        //
        // The witchCage object in script 200 is broken and claims to have 12
        // variables instead of the 8 it should have because it is a Cage.
        // Additionally its top,left,bottom,right properties are set to 0 rather
        // than the right values. We fix the object by setting the right values.
        // If they are all zero, this causes an impossible position check in
        // witch::cantBeHere and an infinite loop when entering room 22.
        //
        // This bug is accidentally not triggered in SSCI because the invalid number
        // of variables effectively hides witchCage::doit, causing this position check
        // to be bypassed entirely.
        // See also the warning+comment in Object::initBaseObject
        //
        // Fixes bug: #4964
        static readonly ushort[] kq5SignatureWitchCageInit = {
            Workarounds.SIG_UINT16_1(0x0000), Workarounds.SIG_UINT16_2(0x0000),         // top
            Workarounds.SIG_UINT16_1(0x0000), Workarounds.SIG_UINT16_2(0x0000),         // left
            Workarounds.SIG_UINT16_1(0x0000), Workarounds.SIG_UINT16_2(0x0000),         // bottom
            Workarounds.SIG_UINT16_1(0x0000), Workarounds.SIG_UINT16_2(0x0000),         // right
            Workarounds.SIG_UINT16_1(0x0000), Workarounds.SIG_UINT16_2(0x0000),         // extra property #1
            Workarounds.SIG_MAGICDWORD,
            Workarounds.SIG_UINT16_1(0x007a), Workarounds.SIG_UINT16_2(0x007a),         // extra property #2
            Workarounds.SIG_UINT16_1(0x00c8), Workarounds.SIG_UINT16_2(0x00c8),         // extra property #3
            Workarounds.SIG_UINT16_1(0x00a3), Workarounds.SIG_UINT16_2(0x00a3),         // extra property #4
            Workarounds.SIG_END
        };

        static readonly ushort[] kq5PatchWitchCageInit = {
            Workarounds.PATCH_UINT16_1(0x0000), Workarounds.PATCH_UINT16_2(0x0000),       // top
            Workarounds.PATCH_UINT16_1(0x007a), Workarounds.PATCH_UINT16_2(0x007a),       // left
            Workarounds.PATCH_UINT16_1(0x00c8), Workarounds.PATCH_UINT16_2(0x00c8),       // bottom
            Workarounds.PATCH_UINT16_1(0x00a3), Workarounds.PATCH_UINT16_2(0x00a3),       // right
            Workarounds.PATCH_END
        };

        // The multilingual releases of KQ5 hang right at the end during the magic battle with Mordack.
        // It seems additional code was added to wait for signals, but the signals are never set and thus
        // the game hangs. We disable that code, so that the battle works again.
        // This also happened in the original interpreter.
        // We must not change similar code, that happens before.

        // Applies to at least: French PC floppy, German PC floppy, Spanish PC floppy
        // Responsible method: stingScript::changeState, dragonScript::changeState, snakeScript::changeState
        static readonly ushort[] kq5SignatureMultilingualEndingGlitch = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x57,                      // lsg global[57h]
            0x35, 0x00,                      // ldi 0
            0x1a,                            // eq?
            0x18,                            // not
            0x30, Workarounds.SIG_UINT16_1(0x0011), Workarounds.SIG_UINT16_2(0x0011),        // bnt [skip signal check]
            Workarounds.SIG_ADDTOOFFSET(+8),             // skip globalSound::prevSignal get code
            0x36,                            // push
            0x35, 0x0a,                      // ldi 0Ah
            Workarounds.SIG_END
        };

        static readonly ushort[] kq5PatchMultilingualEndingGlitch = {
            Workarounds.PATCH_ADDTOOFFSET(+6),
            0x32,                            // change BNT into JMP
            Workarounds.PATCH_END
        };

        // In the final battle, the DOS version uses signals in the music to handle
        // timing, while in the Windows version another method is used and the GM
        // tracks do not contain these signals.
        // The original kq5 interpreter used global 400 to distinguish between
        // Windows (1) and DOS (0) versions.
        // We replace the 4 relevant checks for global 400 by a fixed true when
        // we use these GM tracks.
        //
        // Instead, we could have set global 400, but this has the possibly unwanted
        // side effects of switching to black&white cursors (which also needs complex
        // changes to GameFeatures::detectsetCursorType() ) and breaking savegame
        // compatibilty between the DOS and Windows CD versions of KQ5.
        // TODO: Investigate these side effects more closely.
        static readonly ushort[] kq5SignatureWinGMSignals = {
            Workarounds.SIG_MAGICDWORD,
            0x80, Workarounds.SIG_UINT16_1(0x0190), Workarounds.SIG_UINT16_2(0x0190),        // lag 0x190
            0x18,                            // not
            0x30, Workarounds.SIG_UINT16_1(0x001b), Workarounds.SIG_UINT16_2(0x001b),        // bnt +0x001B
            0x89, 0x57,                      // lsg 0x57
            Workarounds.SIG_END
        };

        static readonly ushort[] kq5PatchWinGMSignals = {
            0x34, Workarounds.PATCH_UINT16_1(0x0001), Workarounds.PATCH_UINT16_2(0x0001),      // ldi 0x0001
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                  patch
        static readonly SciScriptPatcherEntry[] kq5Signatures = {
            new SciScriptPatcherEntry(  true,     0, "CD: harpy volume change",                     1, kq5SignatureCdHarpyVolume,            kq5PatchCdHarpyVolume ),
            new SciScriptPatcherEntry(  true,   200, "CD: witch cage init",                         1, kq5SignatureWitchCageInit,            kq5PatchWitchCageInit ),
            new SciScriptPatcherEntry(  true,   124, "Multilingual: Ending glitching out",          3, kq5SignatureMultilingualEndingGlitch, kq5PatchMultilingualEndingGlitch ),
            new SciScriptPatcherEntry( false,   124, "Win: GM Music signal checks",                 4, kq5SignatureWinGMSignals,             kq5PatchWinGMSignals ),
        };

        // ===========================================================================
        // When giving the milk bottle to one of the babies in the garden in KQ6 (room
        // 480), script 481 starts a looping baby cry sound. However, that particular
        // script also has an overriden check method (cryMusic::check). This method
        // explicitly restarts the sound, even if it's set to be looped, thus the same
        // sound is played twice, squelching all other sounds. We just rip the
        // unnecessary cryMusic::check method out, thereby stopping the sound from
        // constantly restarting (since it's being looped anyway), thus the normal
        // game speech can work while the baby cry sound is heard.
        // Fixes bug: #4955
        static readonly ushort[] kq6SignatureDuplicateBabyCry = {
            Workarounds.SIG_MAGICDWORD,
            0x83, 0x00,                      // lal 00
            0x31, 0x1e,                      // bnt 1e  [07f4]
            0x78,                            // push1
            0x39, 0x04,                      // pushi 04
            0x43, 0x75, 0x02,                // callk DoAudio[75] 02
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6PatchDuplicateBabyCry = {
            0x48,                            // ret
            Workarounds.PATCH_END
        };

        // The inventory of King's Quest 6 is buggy. When it grows too large,
        //  it will get split into 2 pages. Switching between those pages will
        //  grow the stack, because it's calling itself per switch.
        // Which means after a while ScummVM will bomb out because the stack frame
        //  will be too large. This patch fixes the buggy script.
        // Applies to at least: PC-CD, English PC floppy, German PC floppy, English Mac
        // Responsible method: KqInv::showSelf
        // Fixes bug: #5681
        static readonly ushort[] kq6SignatureInventoryStackFix = {
            0x67, 0x30,                         // pTos state
            0x34, Workarounds.SIG_UINT16_1(0x2000), Workarounds.SIG_UINT16_2(0x2000),           // ldi 2000
            0x12,                               // and
            0x18,                               // not
            0x31, 0x04,                         // bnt [not first refresh]
            0x35, 0x00,                         // ldi 00
            Workarounds.SIG_MAGICDWORD,
            0x65, 0x1e,                         // aTop curIcon
            0x67, 0x30,                         // pTos state
            0x34, Workarounds.SIG_UINT16_1(0xdfff), Workarounds.SIG_UINT16_2(0xdfff),           // ldi dfff
            0x12,                               // and
            0x65, 0x30,                         // aTop state
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_show),         // pushi "show" ("show" is e1h for KQ6CD)
            0x78,                               // push1
            0x87, 0x00,                         // lap param[0]
            0x31, 0x04,                         // bnt [use global for show]
            0x87, 0x01,                         // lap param[1]
            0x33, 0x02,                         // jmp [use param for show]
            0x81, 0x00,                         // lag global[0]
            0x36,                               // push
            0x54, 0x06,                         // self 06 (KqInv::show)
            0x31, Workarounds.SIG_ADDTOOFFSET(+1),          // bnt [exit menu code] (0x08 for PC, 0x07 for mac)
            0x39, 0x39,                         // pushi 39
            0x76,                               // push0
            0x54, 0x04,                         // self 04 (KqInv::doit)
            Workarounds.SIG_END                             // followed by jmp (0x32 for PC, 0x33 for mac)
        };

        static readonly ushort[] kq6PatchInventoryStackFix = {
            0x67, 0x30,                         // pTos state
            0x3c,                               // dup (1 more byte, needed for patch)
            0x3c,                               // dup (1 more byte, saves 1 byte later)
            0x34, Workarounds.PATCH_UINT16_1(0x2000), Workarounds.PATCH_UINT16_2(0x2000),         // ldi 2000
            0x12,                               // and
            0x2f, 0x02,                         // bt [not first refresh] - saves 3 bytes in total
            0x65, 0x1e,                         // aTop curIcon
            0x00,                               // neg (either 2000 or 0000 in acc, this will create dfff or ffff) - saves 2 bytes
            0x12,                               // and
            0x65, 0x30,                         // aTop state
            0x38,                               // pushi "show"
            Workarounds.PATCH_GETORIGINALBYTE(+22),
            Workarounds.PATCH_GETORIGINALBYTE(+23),
            0x78,                               // push1
            0x87, 0x00,                         // lap param[0]
            0x31, 0x04,                         // bnt [call show using global 0]
            0x8f, 0x01,                         // lsp param[1], save 1 byte total with lsg global[0] combined
            0x33, 0x02,                         // jmp [call show using param 1]
            0x89, 0x00,                         // lsg global[0], save 1 byte total, see above
            0x54, 0x06,                         // self 06 (call x::show)
            0x31,                               // bnt [menu exit code]
            Workarounds.PATCH_GETORIGINALBYTEADJUST_1(+39), Workarounds.PATCH_GETORIGINALBYTEADJUST_2(+6),// dynamic offset must be 0x0E for PC and 0x0D for mac
            0x34, Workarounds.PATCH_UINT16_1(0x2000), Workarounds.PATCH_UINT16_2(0x2000),         // ldi 2000
            0x12,                               // and
            0x2f, 0x05,                         // bt [to return]
            0x39, 0x39,                         // pushi 39
            0x76,                               // push0
            0x54, 0x04,                         // self 04 (self::doit)
            0x48,                               // ret (saves 2 bytes for PC, 1 byte for mac)
            Workarounds.PATCH_END
        };

        // The "Drink Me" bottle code doesn't repaint the AddToPics elements to the screen,
        //  when Alexander returns back from the effect of the bottle.
        //  It's pretty strange that Sierra didn't find this bug, because it occurs when
        //  drinking the bottle right on the screen, where the bottle is found.
        // This bug also occurs in Sierra SCI.
        // Applies to at least: PC-CD, English PC floppy, German PC floppy, English Mac
        // Responsible method: drinkMeScript::changeState
        // Fixes bug: #5252
        static readonly ushort[] kq6SignatureDrinkMeFix = {
            Workarounds.SIG_MAGICDWORD,
            0x3c,                               // dup
            0x35, 0x0f,                         // ldi 0f
            0x1a,                               // eq?
            0x30, Workarounds.SIG_UINT16_1(0x00a4), Workarounds.SIG_UINT16_2(0x00a4),           // bnt [skip to next check]
            Workarounds.SIG_ADDTOOFFSET(+161),
            0x32, Workarounds.SIG_UINT16_1(0x007f), Workarounds.SIG_UINT16_2(0x007f),           // jmp [return]
            0x3c,                               // dup
            0x35, 0x10,                         // ldi 10
            0x1a,                               // eq?
            0x31, 0x07,                         // bnt [skip to next check]
            0x35, 0x03,                         // ldi 03
            0x65, 0x1a,                         // aTop (cycles)
            0x32, Workarounds.SIG_UINT16_1(0x0072), Workarounds.SIG_UINT16_2(0x0072),           // jmp [return]
            0x3c,                               // dup
            0x35, 0x11,                         // ldi 11
            0x1a,                               // eq?
            0x31, 0x13,                         // bnt [skip to next check]
            Workarounds.SIG_ADDTOOFFSET(+20),
            0x35, 0x12,                         // ldi 12
            Workarounds.SIG_ADDTOOFFSET(+23),
            0x35, 0x13,                         // ldi 13
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6PatchDrinkMeFix = {
            Workarounds.PATCH_ADDTOOFFSET(+5),              // skip to bnt 
            Workarounds.PATCH_GETORIGINALBYTEADJUST_1(+5), Workarounds.PATCH_GETORIGINALBYTEADJUST_2(+13), // adjust jump to [check for 11h code]
            Workarounds.PATCH_ADDTOOFFSET(+162),
            0x39, Workarounds.PATCH_SELECTOR8(ScriptPatcherSelectors.SELECTOR_doit),        // pushi (doit)
            0x76,                               // push0
            0x81, 0x0a,                         // lag 0a
            0x4a, 0x04,                         // send 04 (call addToPics::doit)
            0x3a,                               // toss
            0x48,                               // ret
            Workarounds.PATCH_ADDTOOFFSET(+8),              // skip to check 11h code
            0x35, 0x10,                         // ldi 10 instead of 11
            Workarounds.PATCH_ADDTOOFFSET(+23),             // skip to check 12h code
            0x35, 0x11,                         // ldi 11 instead of 12
            Workarounds.PATCH_ADDTOOFFSET(+23),             // skip to check 13h code
            0x35, 0x12,                         // ldi 12 instead of 13
            Workarounds.PATCH_END
        };

        // Audio + subtitles support - SHARED! - used for King's Quest 6 and Laura Bow 2
        //  this patch gets enabled, when the user selects "both" in the ScummVM "Speech + Subtitles" menu
        //  We currently use global 98d to hold a kMemory pointer.
        // Applies to at least: KQ6 PC-CD, LB2 PC-CD
        // Patched method: Messager::sayNext / lb2Messager::sayNext (always use text branch)
        static readonly ushort[] kq6laurabow2CDSignatureAudioTextSupport1 = {
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x02,                         // ldi 02
            0x12,                               // and
            Workarounds.SIG_MAGICDWORD,
            0x31, 0x13,                         // bnt [audio call]
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_modNum),       // pushi modNum
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6laurabow2CDPatchAudioTextSupport1 = {
            Workarounds.PATCH_ADDTOOFFSET(+5),
            0x33, 0x13,                         // jmp [audio call]
            Workarounds.PATCH_END
        };

        // Applies to at least: KQ6 PC-CD, LB2 PC-CD
        // Patched method: Messager::sayNext / lb2Messager::sayNext (allocate audio memory)
        static readonly ushort[] kq6laurabow2CDSignatureAudioTextSupport2 = {
            0x7a,                               // push2
            0x78,                               // push1
            0x39, 0x0c,                         // pushi 0c
            0x43, Workarounds.SIG_MAGICDWORD, 0x72, 0x04,   // kMemory
            0xa5, 0xc9,                         // sat global[c9]
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6laurabow2CDPatchAudioTextSupport2 = {
            Workarounds.PATCH_ADDTOOFFSET(+7),
            0xa1, 98,                           // sag global[98d]
            Workarounds.PATCH_END
        };

        // Applies to at least: KQ6 PC-CD, LB2 PC-CD
        // Patched method: Messager::sayNext / lb2Messager::sayNext (release audio memory)
        static readonly ushort[] kq6laurabow2CDSignatureAudioTextSupport3 = {
            0x7a,                               // push2
            0x39, 0x03,                         // pushi 03
            Workarounds.SIG_MAGICDWORD,
            0x8d, 0xc9,                         // lst temp[c9]
            0x43, 0x72, 0x04,                   // kMemory
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6laurabow2CDPatchAudioTextSupport3 = {
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0x89, 98,                           // lsg global[98d]
            Workarounds.PATCH_END
        };

        // startText call gets acc = 0 for text-only and acc = 2 for audio+text
        // Applies to at least: KQ6 PC-CD, LB2 PC-CD
        // Patched method: Narrator::say (use audio memory)
        static readonly ushort[] kq6laurabow2CDSignatureAudioTextSupport4 = {
            // set caller property code
            0x31, 0x08,                         // bnt [set acc to 0 for caller]
            0x87, 0x02,                         // lap param[2]
            0x31, 0x04,                         // bnt [set acc to 0 for caller]
            0x87, 0x02,                         // lap param[2]
            0x33, 0x02,                         // jmp [set caller]
            0x35, 0x00,                         // ldi 00
            0x65, 0x68,                         // aTop caller
            // call startText + startAudio code
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x01,                         // ldi 01
            0x12,                               // and
            0x31, 0x08,                         // bnt [skip code]
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_startText),    // pushi startText
            0x78,                               // push1
            0x8f, 0x01,                         // lsp param[1]
            0x54, 0x06,                         // self 06
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x02,                         // ldi 02
            0x12,                               // and
            0x31, 0x08,                         // bnt [skip code]
            Workarounds.SIG_MAGICDWORD,
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_startAudio),   // pushi startAudio
            0x78,                               // push1
            0x8f, 0x01,                         // lsp param[1]
            0x54, 0x06,                         // self 06
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6laurabow2CDPatchAudioTextSupport4 = {
            0x31, 0x02,                         // bnt [set caller]
            0x87, 0x02,                         // lap param[2]
            0x65, 0x68,                         // aTop caller
            0x81, 0x5a,                         // lag global[5a]
            0x78,                               // push1
            0x12,                               // and
            0x31, 0x11,                         // bnt [skip startText code]
            0x81, 0x5a,                         // lag global[5a]
            0x7a,                               // push2
            0x12,                               // and
            0x33, 0x03,                         // skip over 3 unused bytes
            Workarounds.PATCH_ADDTOOFFSET(+22),
            0x89, 98,                           // lsp global[98d]
            Workarounds.PATCH_END
        };

        // Applies to at least: KQ6 PC-CD, LB2 PC-CD
        // Patched method: Talker::display/Narrator::say (remove reset saved mouse cursor code)
        //  code would screw over mouse cursor
        static readonly ushort[] kq6laurabow2CDSignatureAudioTextSupport5 = {
            Workarounds.SIG_MAGICDWORD,
            0x35, 0x00,                         // ldi 00
            0x65, 0x82,                         // aTop saveCursor
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6laurabow2CDPatchAudioTextSupport5 = {
            0x18, 0x18, 0x18, 0x18,             // waste bytes, do nothing
            Workarounds.PATCH_END
        };

        // Additional patch specifically for King's Quest 6
        //  Fixes text window placement, when in "dual" mode
        // Applies to at least: PC-CD
        // Patched method: Kq6Talker::init
        static readonly ushort[] kq6CDSignatureAudioTextSupport1 = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x02,                         // ldi 02
            0x1a,                               // eq?
            0x31, Workarounds.SIG_ADDTOOFFSET(+1),          // bnt [jump-to-text-code]
            0x78,                               // push1
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6CDPatchAudioTextSupport1 = {
            Workarounds.PATCH_ADDTOOFFSET(+4),
            0x12,                               // and
            Workarounds.PATCH_END
        };

        // Additional patch specifically for King's Quest 6
        //  Fixes low-res portrait staying on screen for hi-res mode
        // Applies to at least: PC-CD
        // Patched method: Talker::startText
        //  this method is called by Narrator::say and acc is 0 for text-only and 2 for dual mode (audio+text)
        static readonly ushort[] kq6CDSignatureAudioTextSupport2 = {
            Workarounds.SIG_MAGICDWORD,
            0x3f, 0x01,                         // link 01
            0x63, 0x8a,                         // pToa viewInPrint
            0x18,                               // not
            0x31, 0x06,                         // bnt [skip following code]
            0x38, Workarounds.SIG_UINT16_1(0x00e1), Workarounds.SIG_UINT16_2(0x00e1),           // pushi 00e1
            0x76,                               // push0
            0x54, 0x04,                         // self 04
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6CDPatchAudioTextSupport2 = {
            Workarounds.PATCH_ADDTOOFFSET(+2),
            0x67, 0x8a,                         // pTos viewInPrint
            0x14,                               // or
            0x2f,                               // bt [skip following code]
            Workarounds.PATCH_END
        };

        // Additional patch specifically for King's Quest 6
        //  Fixes special windows, used for example in the Pawn shop (room 280),
        //   when the man in a robe complains about no more mints.
        //  We have to change even more code, because the game uses PODialog class for
        //   text windows and myDialog class for audio. Both are saved to KQ6Print::dialog
        //  Sadly PODialog is created during KQ6Print::addText, myDialog is set during
        //   KQ6Print::showSelf, which is called much later and KQ6Print::addText requires
        //   KQ6Print::dialog to be set, which means we have to set it before calling addText
        //   for audio mode, otherwise the user would have to click to get those windows disposed.
        // Applies to at least: PC-CD
        // Patched method: KQ6Print::say
        static readonly ushort[] kq6CDSignatureAudioTextSupport3 = {
            0x31, 0x6e,                         // bnt [to text code]
            Workarounds.SIG_ADDTOOFFSET(+85),
            Workarounds.SIG_MAGICDWORD,
            0x8f, 0x01,                         // lsp param[1]
            0x35, 0x01,                         // ldi 01
            0x1a,                               // eq?
            0x31, 0x0c,                         // bnt [code to set property repressText to 1]
            0x38,                               // pushi (selector addText)
            Workarounds.SIG_ADDTOOFFSET(+9),                // skip addText-calling code
            0x33, 0x10,                         // jmp [to ret]
            0x35, 0x01,                         // ldi 01
            0x65, 0x2e,                         // aTop repressText
            0x33, 0x0a,                         // jmp [to ret]
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6CDPatchAudioTextSupport3 = {
            0x31, 0x5c,                         // adjust jump to reuse audio mode addText-calling code
            Workarounds.PATCH_ADDTOOFFSET(102),
            0x48,                               // ret
            0x48,                               // ret (waste byte)
            0x72, 0x0e, 0x00,                   // lofsa myDialog
            0x65, 0x12,                         // aTop dialog
            0x33, 0xed,                         // jump back to audio mode addText-calling code
            Workarounds.PATCH_END
        };

        // Additional patch specifically for King's Quest 6
        //  Fixes text-window size for hires portraits mode
        //   Otherwise at least at the end some text-windows will be way too small
        // Applies to at least: PC-CD
        // Patched method: Talker::init
        static readonly ushort[] kq6CDSignatureAudioTextSupport4 = {
            Workarounds.SIG_MAGICDWORD,
            0x63, 0x94,                         // pToa raving
            0x31, 0x0a,                         // bnt [no rave code]
            0x35, 0x00,                         // ldi 00
            Workarounds.SIG_ADDTOOFFSET(6),                 // skip reset of bust, eyes and mouth
            0x33, 0x24,                         // jmp [to super class code]
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6CDPatchAudioTextSupport4 = {
            Workarounds.PATCH_ADDTOOFFSET(+12),
            0x33, Workarounds.PATCH_GETORIGINALBYTEADJUST_1(+13), Workarounds.PATCH_GETORIGINALBYTEADJUST_2(-6), // adjust jump to also include setSize call
            Workarounds.PATCH_END
        };

        //  Fixes text window placement, when dual mode is active (Guards in room 220)
        // Applies to at least: PC-CD
        // Patched method: tlkGateGuard1::init & tlkGateGuard2::init
        static readonly ushort[] kq6CDSignatureAudioTextSupportGuards = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x01,                         // ldi 01
            0x1a,                               // eq?
            Workarounds.SIG_END                             // followed by bnt for Guard1 and bt for Guard2
        };

        static readonly ushort[] kq6CDPatchAudioTextSupportGuards = {
            Workarounds.PATCH_ADDTOOFFSET(+2),
            0x35, 0x02,                         // ldi 02
            0x1c,                               // ne?
            Workarounds.PATCH_END
        };

        //  Fixes text window placement, when portrait+text is shown (Stepmother in room 250)
        // Applies to at least: PC-CD
        // Patched method: tlkStepmother::init
        static readonly ushort[] kq6CDSignatureAudioTextSupportStepmother = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x02,                         // ldi 02
            0x12,                               // and
            0x31,                               // bnt [jump-for-text-code]
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6CDPatchAudioTextSupportJumpAlways = {
            Workarounds.PATCH_ADDTOOFFSET(+4),
            0x1a,                               // eq?
            Workarounds.PATCH_END
        };

        //  Fixes "Girl In The Tower" to get played in dual mode as well
        //  Also changes credits to use CD audio for dual mode.
        //
        // Applies to at least: PC-CD
        // Patched method: rm740::cue (script 740), sCredits::init (script 52)
        static readonly ushort[] kq6CDSignatureAudioTextSupportGirlInTheTower = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x02,                         // ldi 02
            0x1a,                               // eq?
            0x31,                               // bnt [jump-for-text-code]
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6CDPatchAudioTextSupportGirlInTheTower = {
            Workarounds.PATCH_ADDTOOFFSET(+4),
            0x12,                               // and
            Workarounds.PATCH_END
        };

        //  Fixes dual mode for scenes with Azure and Ariel (room 370)
        //   Effectively same patch as the one for fixing "Girl In The Tower"
        // Applies to at least: PC-CD
        // Patched methods: rm370::init, caughtAtGateCD::changeState, caughtAtGateTXT::changeState, toLabyrinth::changeState
        // Fixes bug: #6750
        static readonly ushort[] kq6CDSignatureAudioTextSupportAzureAriel = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x02,                         // ldi 02
            0x1a,                               // eq?
            0x31,                               // bnt [jump-for-text-code]
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6CDPatchAudioTextSupportAzureAriel = {
            Workarounds.PATCH_ADDTOOFFSET(+4),
            0x12,                               // and
            Workarounds.PATCH_END
        };

        // Additional patch specifically for King's Quest 6
        //  Adds another button state for the text/audio button. We currently use the "speech" view for "dual" mode.
        // View 947, loop 9, cel 0+1 -> "text"
        // View 947, loop 8, cel 0+1 -> "speech"
        // View 947, loop 12, cel 0+1 -> "dual" (this view is injected by us into the game)
        // Applies to at least: PC-CD
        // Patched method: iconTextSwitch::show, iconTextSwitch::doit
        static readonly ushort[] kq6CDSignatureAudioTextMenuSupport = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x02,                         // ldi 02
            0x1a,                               // eq?
            0x31, 0x06,                         // bnt [set text view]
            0x35, 0x08,                         // ldi 08
            0x65, 0x14,                         // aTop loop
            0x33, 0x04,                         // jmp [skip over text view]
            0x35, 0x09,                         // ldi 09
            0x65, 0x14,                         // aTop loop
            Workarounds.SIG_ADDTOOFFSET(+102),              // skip to iconTextSwitch::doit code
            0x89, 0x5a,                         // lsg global[5a]
            0x3c,                               // dup
            0x35, 0x01,                         // ldi 01
            0x1a,                               // eq?
            0x31, 0x06,                         // bnt [set text mode]
            0x35, 0x02,                         // ldi 02
            0xa1, 0x5a,                         // sag global[5a]
            0x33, 0x0a,                         // jmp [skip over text mode code]
            0x3c,                               // dup
            0x35, 0x02,                         // ldi 02
            0x1a,                               // eq?
            0x31, 0x04,                         // bnt [skip over text ode code]
            0x35, 0x01,                         // ldi 01
            0xa1, 0x5a,                         // sag global[5a]
            0x3a,                               // toss
            0x67, 0x14,                         // pTos loop
            0x35, 0x09,                         // ldi 09
            0x1a,                               // eq?
            0x31, 0x04,                         // bnt [set text view]
            0x35, 0x08,                         // ldi 08
            0x33, 0x02,                         // jmp [skip text view]
            0x35, 0x09,                         // ldi 09
            0x65, 0x14,                         // aTop loop
            Workarounds.SIG_END
        };

        static readonly ushort[] kq6CDPatchAudioTextMenuSupport = {
            Workarounds.PATCH_ADDTOOFFSET(+13),
            0x33, 0x79,                         // jmp to new text+dual code
            Workarounds.PATCH_ADDTOOFFSET(+104),            // seek to iconTextSwitch::doit
            0x81, 0x5a,                         // lag global[5a]
            0x78,                               // push1
            0x02,                               // add
            0xa1, 0x5a,                         // sag global[5a]
            0x36,                               // push
            0x35, 0x03,                         // ldi 03
            0x1e,                               // gt?
            0x31, 0x03,                         // bnt [skip over]
            0x78,                               // push1
            0xa9, 0x5a,                         // ssg global[5a]
            0x33, 0x17,                         // jmp [iconTextSwitch::show call]
            // additional code for iconTextSwitch::show
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x01,                         // ldi 01
            0x1a,                               // eq?
            0x31, 0x04,                         // bnt [dual mode]
            0x35, 0x09,                         // ldi 09
            0x33, 0x02,                         // jmp [skip over dual mode]
            0x35, 0x0c,                         // ldi 0c (view 947, loop 12, cel 0+1 is our "dual" view, injected by view.cpp)
            0x65, 0x14,                         // aTop loop
            0x32, Workarounds.PATCH_UINT16_1(0xff75), Workarounds.PATCH_UINT16_2(0xff75),         // jmp [back to iconTextSwitch::show]
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                                 patch
        static readonly SciScriptPatcherEntry[] kq6Signatures = {
            new SciScriptPatcherEntry(  true,   481, "duplicate baby cry",                          1, kq6SignatureDuplicateBabyCry,             kq6PatchDuplicateBabyCry ),
            new SciScriptPatcherEntry(  true,   907, "inventory stack fix",                         1, kq6SignatureInventoryStackFix,            kq6PatchInventoryStackFix ),
            new SciScriptPatcherEntry(  true,    87, "Drink Me bottle fix",                         1, kq6SignatureDrinkMeFix,                   kq6PatchDrinkMeFix ),
            // King's Quest 6 and Laura Bow 2 share basic patches for audio + text support
            // *** King's Quest 6 audio + text support ***
            new SciScriptPatcherEntry( false,   924, "CD: audio + text support KQ6&LB2 1",             1, kq6laurabow2CDSignatureAudioTextSupport1,     kq6laurabow2CDPatchAudioTextSupport1 ),
            new SciScriptPatcherEntry( false,   924, "CD: audio + text support KQ6&LB2 2",             1, kq6laurabow2CDSignatureAudioTextSupport2,     kq6laurabow2CDPatchAudioTextSupport2 ),
            new SciScriptPatcherEntry( false,   924, "CD: audio + text support KQ6&LB2 3",             1, kq6laurabow2CDSignatureAudioTextSupport3,     kq6laurabow2CDPatchAudioTextSupport3 ),
            new SciScriptPatcherEntry( false,   928, "CD: audio + text support KQ6&LB2 4",             1, kq6laurabow2CDSignatureAudioTextSupport4,     kq6laurabow2CDPatchAudioTextSupport4 ),
            new SciScriptPatcherEntry( false,   928, "CD: audio + text support KQ6&LB2 5",             2, kq6laurabow2CDSignatureAudioTextSupport5,     kq6laurabow2CDPatchAudioTextSupport5 ),
            new SciScriptPatcherEntry( false,   909, "CD: audio + text support KQ6 1",                 2, kq6CDSignatureAudioTextSupport1,              kq6CDPatchAudioTextSupport1 ),
            new SciScriptPatcherEntry( false,   928, "CD: audio + text support KQ6 2",                 1, kq6CDSignatureAudioTextSupport2,              kq6CDPatchAudioTextSupport2 ),
            new SciScriptPatcherEntry( false,   104, "CD: audio + text support KQ6 3",                 1, kq6CDSignatureAudioTextSupport3,              kq6CDPatchAudioTextSupport3 ),
            new SciScriptPatcherEntry( false,   928, "CD: audio + text support KQ6 4",                 1, kq6CDSignatureAudioTextSupport4,              kq6CDPatchAudioTextSupport4 ),
            new SciScriptPatcherEntry( false,  1009, "CD: audio + text support KQ6 Guards",            2, kq6CDSignatureAudioTextSupportGuards,         kq6CDPatchAudioTextSupportGuards ),
            new SciScriptPatcherEntry( false,  1027, "CD: audio + text support KQ6 Stepmother",        1, kq6CDSignatureAudioTextSupportStepmother,     kq6CDPatchAudioTextSupportJumpAlways ),
            new SciScriptPatcherEntry( false,    52, "CD: audio + text support KQ6 Girl In The Tower", 1, kq6CDSignatureAudioTextSupportGirlInTheTower, kq6CDPatchAudioTextSupportGirlInTheTower ),
            new SciScriptPatcherEntry( false,   740, "CD: audio + text support KQ6 Girl In The Tower", 1, kq6CDSignatureAudioTextSupportGirlInTheTower, kq6CDPatchAudioTextSupportGirlInTheTower ),
            new SciScriptPatcherEntry( false,   370, "CD: audio + text support KQ6 Azure & Ariel",     6, kq6CDSignatureAudioTextSupportAzureAriel,     kq6CDPatchAudioTextSupportAzureAriel ),
            new SciScriptPatcherEntry( false,   903, "CD: audio + text support KQ6 menu",              1, kq6CDSignatureAudioTextMenuSupport,           kq6CDPatchAudioTextMenuSupport ),
        };

        // ===========================================================================

        // King's Quest 7 has really weird subtitles. It seems as if the subtitles were
        // not fully finished.
        //
        // Method kqMessager::findTalker in script 0 tries to figure out, which class to use for
        // displaying subtitles. It uses the "talker" data of the given message to do that.
        // Strangely this "talker" data seems to be quite broken.
        // For example chapter 2 starts with a cutscene.
        // Troll king: "Welcome, most beautiful of princesses!" - talker 6
        // Which is followed by the princess going
        // "Hmm?" - which is set to talker 99, normally the princess is talker 7.
        //
        // Talker 99 is seen as unknown and thus treated as "narrator", which makes
        // the scripts put the text at the top of the game screen and even use a
        // different font.
        //
        // In other cases, when the player character thinks to himself talker 99
        // is also used. In such situations it may make somewhat sense to do so,
        // but putting the text at the top of the screen is also irritating to the player.
        // It's really weird.
        //
        // The scripts also put the regular text in the middle of the screen, blocking
        // animations.
        //
        // And for certain rooms, the subtitle box may use another color
        // like for example pink/purple at the start of chapter 5.
        //
        // We fix all of that (hopefully - lots of testing is required).
        // We put the text at the bottom of the play screen.
        // We also make the scripts use the regular KQTalker instead of KQNarrator.
        // And we also make the subtitle box use color 255, which is fixed white.
        //
        // Applies to at least: PC CD 1.4 English, 1.51 English, 1.51 German, 2.00 English
        // Patched method: KQNarrator::init (script 31)
        static readonly ushort[] kq7SignatureSubtitleFix1 = {
            Workarounds.SIG_MAGICDWORD,
            0x39, 0x25,                         // pushi 25h (fore)
            0x78,                               // push1
            0x39, 0x06,                         // pushi 06 - sets back to 6
            0x39, 0x26,                         // pushi 26 (back)
            0x78,                               // push1
            0x78,                               // push1 - sets back to 1
            0x39, 0x2a,                         // pushi 2Ah (font)
            0x78,                               // push1
            0x89, 0x16,                         // lsg global[16h] - sets font to global[16h]
            0x7a,                               // push2 (y)
            0x78,                               // push1
            0x76,                               // push0 - sets y to 0
            0x54, Workarounds.SIG_UINT16_1(0x0018), Workarounds.SIG_UINT16_2(0x0018),           // self 18h
            Workarounds.SIG_END
        };

        static readonly ushort[] kq7PatchSubtitleFix1 = {
            0x33, 0x12,                         // jmp [skip special init code]
            Workarounds.PATCH_END
        };

        // Applies to at least: PC CD 1.51 English, 1.51 German, 2.00 English
        // Patched method: Narrator::init (script 64928)
        static readonly ushort[] kq7SignatureSubtitleFix2 = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x02,                         // ldi 02
            0x12,                               // and
            0x31, 0x1e,                         // bnt [skip audio volume code]
            0x38, Workarounds.SIG_ADDTOOFFSET(+2),          // pushi masterVolume (0212h for 2.00, 0219h for 1.51)
            0x76,                               // push0
            0x81, 0x01,                         // lag global[1]
            0x4a, 0x04, 0x00,                   // send 04
            0x65, 0x32,                         // aTop curVolume
            0x38, Workarounds.SIG_ADDTOOFFSET(+2),          // pushi masterVolume (0212h for 2.00, 0219h for 1.51)
            0x78,                               // push1
            0x67, 0x32,                         // pTos curVolume
            0x35, 0x02,                         // ldi 02
            0x06,                               // mul
            0x36,                               // push
            0x35, 0x03,                         // ldi 03
            0x08,                               // div
            0x36,                               // push
            0x81, 0x01,                         // lag global[1]
            0x4a, 0x06, 0x00,                   // send 06
            // end of volume code
            0x35, 0x01,                         // ldi 01
            0x65, 0x28,                         // aTop initialized
            Workarounds.SIG_END
        };

        static readonly ushort[] kq7PatchSubtitleFix2 = {
            Workarounds.PATCH_ADDTOOFFSET(+5),              // skip to bnt
            0x31, 0x1b,                         // bnt [skip audio volume code]
            Workarounds.PATCH_ADDTOOFFSET(+15),             // right after "aTop curVolume / pushi masterVolume / push1"
            0x7a,                               // push2
            0x06,                               // mul (saves 3 bytes in total)
            0x36,                               // push
            0x35, 0x03,                         // ldi 03
            0x08,                               // div
            0x36,                               // push
            0x81, 0x01,                         // lag global[1]
            0x4a, 0x06, 0x00,                   // send 06
            // end of volume code
            0x35, 118,                          // ldi 118d
            0x65, 0x16,                         // aTop y
            0x78,                               // push1 (saves 1 byte)
            0x69, 0x28,                         // sTop initialized
            Workarounds.PATCH_END
        };

        // Applies to at least: PC CD 1.51 English, 1.51 German, 2.00 English
        // Patched method: Narrator::say (script 64928)
        static readonly ushort[] kq7SignatureSubtitleFix3 = {
            Workarounds.SIG_MAGICDWORD,
            0x63, 0x28,                         // pToa initialized
            0x18,                               // not
            0x31, 0x07,                         // bnt [skip init code]
            0x38, Workarounds.SIG_ADDTOOFFSET(+2),          // pushi init (008Eh for 2.00, 0093h for 1.51)
            0x76,                               // push0
            0x54, Workarounds.SIG_UINT16_1(0x0004), Workarounds.SIG_UINT16_2(0x0004),           // self 04
            // end of init code
            0x8f, 0x00,                         // lsp param[0]
            0x35, 0x01,                         // ldi 01
            0x1e,                               // gt?
            0x31, 0x08,                         // bnt [set acc to 0]
            0x87, 0x02,                         // lap param[2]
            0x31, 0x04,                         // bnt [set acc to 0]
            0x87, 0x02,                         // lap param[2]
            0x33, 0x02,                         // jmp [over set acc to 0 code]
            0x35, 0x00,                         // ldi 00
            0x65, 0x18,                         // aTop caller
            Workarounds.SIG_END
        };

        static readonly ushort[] kq7PatchSubtitleFix3 = {
            Workarounds.PATCH_ADDTOOFFSET(+2),              // skip over "pToa initialized code"
            0x2f, 0x0c,                         // bt [skip init code] - saved 1 byte
            0x38,
            Workarounds.PATCH_GETORIGINALBYTE(+6),
            Workarounds.PATCH_GETORIGINALBYTE(+7),          // pushi (init)
            0x76,                               // push0
            0x54, Workarounds.PATCH_UINT16_1(0x0004), Workarounds.PATCH_UINT16_2(0x0004),           // self 04
            // additionally set background color here (5 bytes)
            0x34, Workarounds.PATCH_UINT16_1(255), Workarounds.PATCH_UINT16_2(255),            // pushi 255d
            0x65, 0x2e,                         // aTop back
            // end of init code
            0x8f, 0x00,                         // lsp param[0]
            0x35, 0x01,                         // ldi 01 - this may get optimized to get another byte
            0x1e,                               // gt?
            0x31, 0x04,                         // bnt [set acc to 0]
            0x87, 0x02,                         // lap param[2]
            0x2f, 0x02,                         // bt [over set acc to 0 code]
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                                 patch
        static readonly SciScriptPatcherEntry[] kq7Signatures = {
            new SciScriptPatcherEntry(  true,    31, "subtitle fix 1/3",                            1, kq7SignatureSubtitleFix1,                 kq7PatchSubtitleFix1 ),
            new SciScriptPatcherEntry(  true, 64928, "subtitle fix 2/3",                            1, kq7SignatureSubtitleFix2,                 kq7PatchSubtitleFix2 ),
            new SciScriptPatcherEntry(  true, 64928, "subtitle fix 3/3",                            1, kq7SignatureSubtitleFix3,                 kq7PatchSubtitleFix3 ),
        };


        // ===========================================================================
        // Laura Bow 1 - Colonel's Bequest
        //
        // This is basically just a broken easter egg in Colonel's Bequest.
        // A plane can show up in room 4, but that only happens really rarely.
        // Anyway the Sierra developer seems to have just entered the wrong loop,
        // which is why the statue view is used instead (loop 0).
        // We fix it to use the correct loop.
        //
        // This is only broken in the PC version. It was fixed for Amiga + Atari ST.
        //
        // Credits to OmerMor, for finding it.

        // Applies to at least: English PC Floppy
        // Responsible method: room4::init
        static readonly ushort[] laurabow1SignatureEasterEggViewFix = {
            0x78,                               // push1
            0x76,                               // push0
            Workarounds.SIG_MAGICDWORD,
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_setLoop),      // pushi "setLoop"
            0x78,                               // push1
            0x39, 0x03,                         // pushi 3 (loop 3, view only has 3 loops)
            Workarounds.SIG_END
        };

        static readonly ushort[] laurabow1PatchEasterEggViewFix = {
            Workarounds.PATCH_ADDTOOFFSET(+7),
            0x02,                            // change loop to 2
            Workarounds.PATCH_END
        };

        // When oiling the armor or opening the visor of the armor, the scripts
        //  first check if Laura/ego is near the armor and if she is not, they will move her
        //  to the armor. After that further code is executed.
        //
        // The current location is checked by a ego::inRect() call.
        //
        // The given rect for the inRect call inside openVisor::changeState was made larger for Atari ST/Amiga versions.
        //  We change the PC version to use the same rect.
        //
        // Additionally the coordinate, that Laura is moved to, is 152, 107 and may not be reachable depending on where
        //  Laura/ego was, when "use oil on helmet of armor" / "open visor of armor" got entered.
        //  Bad coordinates are for example 82, 110, which then cause collisions and effectively an endless loop.
        //  Game will effectively "freeze" and the user is only able to restore a previous game.
        //  This also happened, when using the original interpreter.
        //  We change the destination coordinate to 152, 110, which seems to be reachable all the time.
        //
        // The following patch fixes the rect for the PC version of the game.
        //
        // Applies to at least: English PC Floppy
        // Responsible method: openVisor::changeState (script 37)
        // Fixes bug: #7119
        static readonly ushort[] laurabow1SignatureArmorOpenVisorFix = {
            0x39, 0x04,                         // pushi 04
            Workarounds.SIG_MAGICDWORD,
            0x39, 0x6a,                         // pushi 6a (106d)
            0x38, Workarounds.SIG_UINT16_1(0x96), Workarounds.SIG_UINT16_2(0x96),             // pushi 0096 (150d)
            0x39, 0x6c,                         // pushi 6c (108d)
            0x38, Workarounds.SIG_UINT16_1(0x98), Workarounds.SIG_UINT16_2(0x98),             // pushi 0098 (152d)
            Workarounds.SIG_END
        };

        static readonly ushort[] laurabow1PatchArmorOpenVisorFix = {
            Workarounds.PATCH_ADDTOOFFSET(+2),
            0x39, 0x68,                         // pushi 68 (104d)   (-2)
            0x38, Workarounds.SIG_UINT16_1(0x94), Workarounds.SIG_UINT16_2(0x94),             // pushi 0094 (148d) (-2)
            0x39, 0x6f,                         // pushi 6f (111d)   (+3)
            0x38, Workarounds.SIG_UINT16_1(0x9a), Workarounds.SIG_UINT16_2(0x9a),             // pushi 009a (154d) (+2)
            Workarounds.PATCH_END
        };

        // This here fixes the destination coordinate (exact details are above).
        //
        // Applies to at least: English PC Floppy, English Atari ST Floppy, English Amiga Floppy
        // Responsible method: openVisor::changeState, oiling::changeState (script 37)
        // Fixes bug: #7119
        static readonly ushort[] laurabow1SignatureArmorMoveToFix = {
            Workarounds.SIG_MAGICDWORD,
            0x36,                               // push
            0x39, 0x6b,                         // pushi 6B (107d)
            0x38, Workarounds.SIG_UINT16_1(0x0098), Workarounds.SIG_UINT16_2(0x0098),           // pushi 98 (152d)
            0x7c,                               // pushSelf
            0x81, 0x00,                         // lag global[0]
            Workarounds.SIG_END
        };

        static readonly ushort[] laurabow1PatchArmorMoveToFix = {
            Workarounds.PATCH_ADDTOOFFSET(+1),
            0x39, 0x6e,                         // pushi 6E (110d) - adjust x, so that no collision can occur anymore
            Workarounds.PATCH_END
        };

        // In some cases like for example when the player oils the arm of the armor, command input stays
        // disabled, even when the player exits fast enough, so that Laura doesn't die.
        //
        // This is caused by the scripts only enabling control (directional movement), but do not enable command input as well.
        //
        // This bug also happens, when using the original interpreter.
        // And it was fixed for the Atari ST + Amiga versions of the game.
        //
        // Applies to at least: English PC Floppy
        // Responsible method: 2nd subroutine in script 37, called by oiling::changeState(7)
        // Fixes bug: #7154
        static readonly ushort[] laurabow1SignatureArmorOilingArmFix = {
            0x38, Workarounds.SIG_UINT16_1(0x0089), Workarounds.SIG_UINT16_2(0x0089),           // pushi 89h
            0x76,                               // push0
            Workarounds.SIG_MAGICDWORD,
            0x72, Workarounds.SIG_UINT16_1(0x1a5c), Workarounds.SIG_UINT16_2(0x1a5c),           // lofsa "Can" - offsets are not skipped to make sure only the PC version gets patched
            0x4a, 0x04,                         // send 04
            0x38, Workarounds.SIG_UINT16_1(0x0089), Workarounds.SIG_UINT16_2(0x0089),           // pushi 89h
            0x76,                               // push0
            0x72, Workarounds.SIG_UINT16_1(0x19a1), Workarounds.SIG_UINT16_2(0x19a1),           // lofsa "Visor"
            0x4a, 0x04,                         // send 04
            0x38, Workarounds.SIG_UINT16_1(0x0089), Workarounds.SIG_UINT16_2(0x0089),           // pushi 89h
            0x76,                               // push0
            0x72, Workarounds.SIG_UINT16_1(0x194a), Workarounds.SIG_UINT16_2(0x194a),           // lofsa "note"
            0x4a, 0x04,                         // send 04
            0x38, Workarounds.SIG_UINT16_1(0x0089), Workarounds.SIG_UINT16_2(0x0089),           // pushi 89h
            0x76,                               // push0
            0x72, Workarounds.SIG_UINT16_1(0x18f3), Workarounds.SIG_UINT16_2(0x18f3),           // lofsa "valve"
            0x4a, 0x04,                         // send 04
            0x8b, 0x34,                         // lsl local[34h]
            0x35, 0x02,                         // ldi 02
            0x1c,                               // ne?
            0x30, Workarounds.SIG_UINT16_1(0x0014), Workarounds.SIG_UINT16_2(0x0014),           // bnt [to ret]
            0x8b, 0x34,                         // lsl local[34h]
            0x35, 0x05,                         // ldi 05
            0x1c,                               // ne?
            0x30, Workarounds.SIG_UINT16_1(0x000c), Workarounds.SIG_UINT16_2(0x000c),           // bnt [to ret]
            0x8b, 0x34,                         // lsl local[34h]
            0x35, 0x06,                         // ldi 06
            0x1c,                               // ne?
            0x30, Workarounds.SIG_UINT16_1(0x0004), Workarounds.SIG_UINT16_2(0x0004),           // bnt [to ret]
            // followed by code to call script 0 export to re-enable controls and call setMotion
            Workarounds.SIG_END
        };

        static readonly ushort[] laurabow1PatchArmorOilingArmFix = {
            Workarounds.PATCH_ADDTOOFFSET(+3),              // skip over pushi 89h
            0x3c,                               // dup
            0x3c,                               // dup
            0x3c,                               // dup
            // saves a total of 6 bytes
            0x76,                               // push0
            0x72, Workarounds.SIG_UINT16_1(0x1a59), Workarounds.SIG_UINT16_2(0x1a59),           // lofsa "Can"
            0x4a, 0x04,                         // send 04
            0x76,                               // push0
            0x72, Workarounds.SIG_UINT16_1(0x19a1), Workarounds.SIG_UINT16_2(0x19a1),           // lofsa "Visor"
            0x4a, 0x04,                         // send 04
            0x76,                               // push0
            0x72, Workarounds.SIG_UINT16_1(0x194d), Workarounds.SIG_UINT16_2(0x194d),           // lofsa "note"
            0x4a, 0x04,                         // send 04
            0x76,                               // push0
            0x72, Workarounds.SIG_UINT16_1(0x18f9), Workarounds.SIG_UINT16_2(0x18f9),           // lofsa "valve" 18f3
            0x4a, 0x04,                         // send 04
            // new code to enable input as well, needs 9 spare bytes
            0x38, Workarounds.SIG_UINT16_1(0x00e2), Workarounds.SIG_UINT16_2(0x00e2),           // canInput
            0x78,                               // push1
            0x78,                               // push1
            0x51, 0x2b,                         // class User
            0x4a, 0x06,                         // send 06 -> call User::canInput(1)
            // original code, but changed a bit to save some more bytes
            0x8b, 0x34,                         // lsl local[34h]
            0x35, 0x02,                         // ldi 02
            0x04,                               // sub
            0x31, 0x12,                         // bnt [to ret]
            0x36,                               // push
            0x35, 0x03,                         // ldi 03
            0x04,                               // sub
            0x31, 0x0c,                         // bnt [to ret]
            0x78,                               // push1
            0x1a,                               // eq?
            0x2f, 0x08,                         // bt [to ret]
            // saves 7 bytes, we only need 3, so waste 4 bytes
            0x35, 0x00,                         // ldi 0
            0x35, 0x00,                         // ldi 0
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                           patch
        static readonly SciScriptPatcherEntry[] laurabow1Signatures = {
            new SciScriptPatcherEntry(  true,     4, "easter egg view fix",                         1, laurabow1SignatureEasterEggViewFix,  laurabow1PatchEasterEggViewFix ),
            new SciScriptPatcherEntry(  true,    37, "armor open visor fix",                        1, laurabow1SignatureArmorOpenVisorFix, laurabow1PatchArmorOpenVisorFix ),
            new SciScriptPatcherEntry(  true,    37, "armor move to fix",                           2, laurabow1SignatureArmorMoveToFix,    laurabow1PatchArmorMoveToFix ),
            new SciScriptPatcherEntry(  true,    37, "allowing input, after oiling arm",            1, laurabow1SignatureArmorOilingArmFix, laurabow1PatchArmorOilingArmFix ),
        };

        // ===========================================================================
        // Laura Bow 2
        //
        // Moving away the painting in the room with the hidden safe is problematic
        //  for the CD version of the game. safePic::doVerb gets triggered by the mouse-click.
        // This method sets local 0 as signal, which is only meant to get handled, when
        //  the player clicks again to move the painting back. This signal is processed by
        //  the room doit-script.
        // That doit-script checks safePic::cel to be not equal 0 and would then skip over
        //  the "close painting" trigger code. On very fast computers this script may
        //  get called too early (which is the case when running under ScummVM and when
        //  running the game using Sierra SCI in DOS-Box with cycles 15000) and thinks
        //  that it's supposed to move the painting back. Which then results in the painting
        //  getting moved to its original position immediately (which means it won't be possible
        //  to access the safe behind it).
        //
        // We patch the script, so that we check for cel to be not equal 4 (the final cel) and
        //  we also reset the safePic-signal immediately as well.
        //
        // In the floppy version Laura's coordinates are checked directly in rm560::doit
        //  and as soon as she moves, the painting will automatically move to its original position.
        //  This is not the case for the CD version of the game. The painting will only "move" back,
        //  when the player actually exits the room and re-enters.
        //
        // Applies to at least: English PC-CD
        // Responsible method: rm560::doit
        // Fixes bug: #6460
        static readonly ushort[] laurabow2CDSignaturePaintingClosing = {
            0x39, 0x04,                         // pushi 04 (cel)
            0x76,                               // push0
            Workarounds.SIG_MAGICDWORD,
            0x7a,                               // push2
            0x38, Workarounds.SIG_UINT16_1(0x0231), Workarounds.SIG_UINT16_2(0x0231),           // pushi 0231h (561)
            0x76,                               // push0
            0x43, 0x02, 0x04,                   // kScriptID (get export 0 of script 561)
            0x4a, 0x04,                         // send 04 (gets safePicture::cel)
            0x18,                               // not
            0x31, 0x21,                         // bnt [exit]
            0x38, Workarounds.SIG_UINT16_1(0x0283), Workarounds.SIG_UINT16_2(0x0283),           // pushi 0283h
            0x76,                               // push0
            0x7a,                               // push2
            0x39, 0x20,                         // pushi 20
            0x76,                               // push0
            0x43, 0x02, 0x04,                   // kScriptID (get export 0 of script 32)
            0x4a, 0x04,                         // send 04 (get sHeimlich::room)
            0x36,                               // push
            0x81, 0x0b,                         // lag global[b] (current room)
            0x1c,                               // ne?
            0x31, 0x0e,                         // bnt [exit]
            0x35, 0x00,                         // ldi 00
            0xa3, 0x00,                         // sal local[0] -> reset safePic signal
            Workarounds.SIG_END
        };

        static readonly ushort[] laurabow2CDPatchPaintingClosing = {
            Workarounds.PATCH_ADDTOOFFSET(+2),
            0x3c,                               // dup (1 additional byte)
            0x76,                               // push0
            0x3c,                               // dup (1 additional byte)
            0xab, 0x00,                         // ssl local[0] -> reset safePic signal
            0x7a,                               // push2
            0x38, Workarounds.PATCH_UINT16_1(0x0231), Workarounds.PATCH_UINT16_2(0x0231),         // pushi 0231h (561)
            0x76,                               // push0
            0x43, 0x02, 0x04,                   // kScriptID (get export 0 of script 561)
            0x4a, 0x04,                         // send 04 (gets safePicture::cel)
            0x1a,                               // eq?
            0x31, 0x1d,                         // bnt [exit]
            0x38, Workarounds.PATCH_UINT16_1(0x0283), Workarounds.PATCH_UINT16_2(0x0283),         // pushi 0283h
            0x76,                               // push0
            0x7a,                               // push2
            0x39, 0x20,                         // pushi 20
            0x76,                               // push0
            0x43, 0x02, 0x04,                   // kScriptID (get export 0 of script 32)
            0x4a, 0x04,                         // send 04 (get sHeimlich::room)
            0x36,                               // push
            0x81, 0x0b,                         // lag global[b] (current room)
            0x1a,                               // eq? (2 opcodes changed, to save 2 bytes)
            0x2f, 0x0a,                         // bt [exit]
            Workarounds.PATCH_END
        };

        // In the CD version the system menu is disabled for certain rooms. LB2::handsOff is called,
        //  when leaving the room (and in other cases as well). This method remembers the disabled
        //  icons of the icon bar. In the new room LB2::handsOn will get called, which then enables
        //  all icons, but also disabled the ones, that were disabled before.
        //
        // Because of this behaviour certain rooms, that should have the system menu enabled, have
        //  it disabled, when entering those rooms from rooms, where the menu is supposed to be
        //  disabled.
        //
        // We patch this by injecting code into LB2::newRoom (which is called right after a room change)
        //  and reset the global variable there, that normally holds the disabled buttons.
        //
        // This patch may cause side-effects and it's difficult to test, because it affects every room
        //  in the game. At least for the intro, the speakeasy and plenty of rooms in the beginning it
        //  seems to work correctly.
        //
        // Applies to at least: English PC-CD
        // Responsible method: LB2::newRoom, LB2::handsOff, LB2::handsOn
        // Fixes bug: #6440
        static readonly ushort[] laurabow2CDSignatureFixProblematicIconBar = {
            Workarounds.SIG_MAGICDWORD,
            0x38, Workarounds.SIG_UINT16_1(0x00f1), Workarounds.SIG_UINT16_2(0x00f1),           // pushi 00f1 (disable) - hardcoded, we only want to patch the CD version
            0x76,                               // push0
            0x81, 0x45,                         // lag global[45]
            0x4a, 0x04,                         // send 04
            Workarounds.SIG_END
        };

        static readonly ushort[] laurabow2CDPatchFixProblematicIconBar = {
            0x35, 0x00,                      // ldi 00
            0xa1, 0x74,                      // sag 74h
            0x35, 0x00,                      // ldi 00 (waste bytes)
            0x35, 0x00,                      // ldi 00
            Workarounds.PATCH_END
        };

        // Opening/Closing the east door in the pterodactyl room doesn't
        //  check, if it's locked and will open/close the door internally
        //  even when it is.
        //
        // It will get wired shut later in the game by Laura Bow and will be
        //  "locked" because of this. We patch in a check for the locked
        //  state. We also add code, that will set the "locked" state
        //  in case our eastDoor-wired-global is set. This makes the locked
        //  state effectively persistent.
        //
        // Applies to at least: English PC-CD, English PC-Floppy
        // Responsible method (CD): eastDoor::doVerb
        // Responsible method (Floppy): eastDoor::<noname300>
        // Fixes bug: #6458 (partly, see additional patch below)
        static readonly ushort[] laurabow2CDSignatureFixWiredEastDoor = {
            0x30, Workarounds.SIG_UINT16_1(0x0022), Workarounds.SIG_UINT16_2(0x0022),           // bnt [skip hand action]
            0x67, Workarounds.SIG_ADDTOOFFSET(+1),          // pTos CD: doorState, Floppy: state
            0x35, 0x00,                         // ldi 00
            0x1a,                               // eq?
            0x31, 0x08,                         // bnt [close door code]
            0x78,                               // push1
            Workarounds.SIG_MAGICDWORD,
            0x39, 0x63,                         // pushi 63h
            0x45, 0x04, 0x02,                   // callb export000_4, 02 (sets door-bitflag)
            0x33, 0x06,                         // jmp [super-code]
            0x78,                               // push1
            0x39, 0x63,                         // pushi 63h
            0x45, 0x03, 0x02,                   // callb export000_3, 02 (resets door-bitflag)
            0x38, Workarounds.SIG_ADDTOOFFSET(+2),          // pushi CD: 011dh, Floppy: 012ch
            0x78,                               // push1
            0x8f, 0x01,                         // lsp param[01]
            0x59, 0x02,                         // rest 02
            0x57, Workarounds.SIG_ADDTOOFFSET(+1), 0x06,    // super CD: LbDoor, Floppy: Door, 06
            0x33, 0x0b,                         // jmp [ret]
            Workarounds.SIG_END
        };

        static readonly ushort[] laurabow2CDPatchFixWiredEastDoor = {
            0x31, 0x23,                         // bnt [skip hand action] (saves 1 byte)
            0x81,   97,                         // lag 97d (get our eastDoor-wired-global)
            0x31, 0x04,                         // bnt [skip setting locked property]
            0x35, 0x01,                         // ldi 01
            0x65, 0x6a,                         // aTop locked (set eastDoor::locked to 1)
            0x63, 0x6a,                         // pToa locked (get eastDoor::locked)
            0x2f, 0x17,                         // bt [skip hand action]
            0x63, Workarounds.PATCH_GETORIGINALBYTE(+4),    // pToa CD: doorState, Floppy: state
            0x78,                               // push1
            0x39, 0x63,                         // pushi 63h
            0x2f, 0x05,                         // bt [close door code]
            0x45, 0x04, 0x02,                   // callb export000_4, 02 (sets door-bitflag)
            0x33, 0x0b,                         // jmp [super-code]
            0x45, 0x03, 0x02,                   // callb export000_3, 02 (resets door-bitflag)
            0x33, 0x06,                         // jmp [super-code]
            Workarounds.PATCH_END
        };

        // We patch in code, so that our eastDoor-wired-global will get set to 1.
        //  This way the wired-state won't get lost when exiting room 430.
        //
        // Applies to at least: English PC-CD, English PC-Floppy
        // Responsible method (CD): sWireItShut::changeState
        // Responsible method (Floppy): sWireItShut::<noname144>
        // Fixes bug: #6458 (partly, see additional patch above)
        static readonly ushort[] laurabow2SignatureRememberWiredEastDoor = {
            Workarounds.SIG_MAGICDWORD,
            0x33, 0x27,                         // jmp [ret]
            0x3c,                               // dup
            0x35, 0x06,                         // ldi 06
            0x1a,                               // eq?
            0x31, 0x21,                         // bnt [skip step]
            Workarounds.SIG_END
        };

        static readonly ushort[] laurabow2PatchRememberWiredEastDoor = {
            Workarounds.PATCH_ADDTOOFFSET(+2),              // skip jmp [ret]
            0x34, Workarounds.PATCH_UINT16_1(0x0001), Workarounds.PATCH_UINT16_2(0x0001),         // ldi 0001
            0xa1, Workarounds.PATCH_UINT16_1(97), Workarounds.PATCH_UINT16_2(97),             // sag 97d (set our eastDoor-wired-global)
            Workarounds.PATCH_END
        };

        // Laura Bow 2 CD resets the audio mode to speech on init/restart
        //  We already sync the settings from ScummVM (see SciEngine::syncIngameAudioOptions())
        //  and this script code would make it impossible to see the intro using "dual" mode w/o using debugger command
        //  That's why we remove the corresponding code
        // Patched method: LB2::init, rm100::init
        static readonly ushort[] laurabow2CDSignatureAudioTextSupportModeReset = {
            Workarounds.SIG_MAGICDWORD,
            0x35, 0x02,                         // ldi 02
            0xa1, 0x5a,                         // sag global[5a]
            Workarounds.SIG_END
        };

        static readonly ushort[] laurabow2CDPatchAudioTextSupportModeReset = {
            0x34, Workarounds.PATCH_UINT16_1(0x0001), Workarounds.PATCH_UINT16_2(0x0001),         // ldi 0001 (waste bytes)
            0x18,                               // not (waste bytes)
            Workarounds.PATCH_END
        };

        // Directly use global 5a for view-cel id
        //  That way it's possible to use a new "dual" mode view in the game menu
        // View 995, loop 13, cel 0 -> "text"
        // View 995, loop 13, cel 1 -> "speech"
        // View 995, loop 13, cel 2 -> "dual"  (this view is injected by us into the game)
        // Patched method: gcWin::open
        static readonly ushort[] laurabow2CDSignatureAudioTextMenuSupport1 = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x02,                         // ldi 02
            0x1a,                               // eq?
            0x36,                               // push
            Workarounds.SIG_END
        };

        static readonly ushort[] laurabow2CDPatchAudioTextMenuSupport1 = {
            Workarounds.PATCH_ADDTOOFFSET(+2),
            0x35, 0x01,                         // ldi 01
            0x04,                               // sub
            Workarounds.PATCH_END
        };

        //  Adds another button state for the text/audio button. We currently use the "speech" view for "dual" mode.
        // Patched method: iconMode::doit
        static readonly ushort[] laurabow2CDSignatureAudioTextMenuSupport2 = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x5a,                         // lsg global[5a]
            0x3c,                               // dup
            0x1a,                               // eq?
            0x31, 0x0a,                         // bnt [set text mode]
            0x35, 0x02,                         // ldi 02
            0xa1, 0x5a,                         // sag global[5a]
            0x35, 0x01,                         // ldi 01
            0xa5, 0x00,                         // sat temp[0]
            0x33, 0x0e,                         // jmp [draw cel code]
            0x3c,                               // dup
            0x35, 0x02,                         // ldi 02
            0x1a,                               // eq?
            0x31, 0x08,                         // bnt [draw cel code]
            0x35, 0x01,                         // ldi 01
            0xa1, 0x5a,                         // sag global[5a]
            0x35, 0x00,                         // ldi 00
            0xa5, 0x00,                         // sat temp[0]
            0x3a,                               // toss
            Workarounds.SIG_END
        };

        static readonly ushort[] laurabow2CDPatchAudioTextMenuSupport2 = {
            0x81, 0x5a,                         // lag global[5a]
            0x78,                               // push1
            0x02,                               // add
            0xa1, 0x5a,                         // sag global[5a]
            0x36,                               // push
            0x35, 0x03,                         // ldi 03
            0x1e,                               // gt?
            0x31, 0x03,                         // bnt [skip over]
            0x78,                               // push1
            0xa9, 0x5a,                         // ssg global[5a]
            0x89, 0x5a,                         // lsg global[5a]
            0x35, 0x01,                         // ldi 01
            0x04,                               // sub
            0xa5, 0x00,                         // sat temp[0] - calculate global[5a] - 1 to use as view cel id
            0x33, 0x07,                         // jmp [draw cel code, don't do toss]
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                                      patch
        static readonly SciScriptPatcherEntry[] laurabow2Signatures = {
            new SciScriptPatcherEntry(  true,   560, "CD: painting closing immediately",            1, laurabow2CDSignaturePaintingClosing,           laurabow2CDPatchPaintingClosing ),
            new SciScriptPatcherEntry(  true,     0, "CD: fix problematic icon bar",                1, laurabow2CDSignatureFixProblematicIconBar,     laurabow2CDPatchFixProblematicIconBar ),
            new SciScriptPatcherEntry(  true,   430, "CD/Floppy: make wired east door persistent",  1, laurabow2SignatureRememberWiredEastDoor,       laurabow2PatchRememberWiredEastDoor ),
            new SciScriptPatcherEntry(  true,   430, "CD/Floppy: fix wired east door",              1, laurabow2CDSignatureFixWiredEastDoor,          laurabow2CDPatchFixWiredEastDoor ),
            // King's Quest 6 and Laura Bow 2 share basic patches for audio + text support
            new SciScriptPatcherEntry( false,   924, "CD: audio + text support 1",                  1, kq6laurabow2CDSignatureAudioTextSupport1,      kq6laurabow2CDPatchAudioTextSupport1 ),
            new SciScriptPatcherEntry( false,   924, "CD: audio + text support 2",                  1, kq6laurabow2CDSignatureAudioTextSupport2,      kq6laurabow2CDPatchAudioTextSupport2 ),
            new SciScriptPatcherEntry( false,   924, "CD: audio + text support 3",                  1, kq6laurabow2CDSignatureAudioTextSupport3,      kq6laurabow2CDPatchAudioTextSupport3 ),
            new SciScriptPatcherEntry( false,   928, "CD: audio + text support 4",                  1, kq6laurabow2CDSignatureAudioTextSupport4,      kq6laurabow2CDPatchAudioTextSupport4 ),
            new SciScriptPatcherEntry( false,   928, "CD: audio + text support 5",                  2, kq6laurabow2CDSignatureAudioTextSupport5,      kq6laurabow2CDPatchAudioTextSupport5 ),
            new SciScriptPatcherEntry( false,     0, "CD: audio + text support disable mode reset", 1, laurabow2CDSignatureAudioTextSupportModeReset, laurabow2CDPatchAudioTextSupportModeReset ),
            new SciScriptPatcherEntry( false,   100, "CD: audio + text support disable mode reset", 1, laurabow2CDSignatureAudioTextSupportModeReset, laurabow2CDPatchAudioTextSupportModeReset ),
            new SciScriptPatcherEntry( false,    24, "CD: audio + text support LB2 menu 1",         1, laurabow2CDSignatureAudioTextMenuSupport1,     laurabow2CDPatchAudioTextMenuSupport1 ),
            new SciScriptPatcherEntry( false,    24, "CD: audio + text support LB2 menu 2",         1, laurabow2CDSignatureAudioTextMenuSupport2,     laurabow2CDPatchAudioTextMenuSupport2 ),
        };


        // ===========================================================================
        // Script 210 in the German version of Longbow handles the case where Robin
        // hands out the scroll to Marion and then types his name using the hand code.
        // The German version script contains a typo (probably a copy/paste error),
        // and the function that is used to show each letter is called twice. The
        // second time that the function is called, the second parameter passed to
        // the function is undefined, thus kStrCat() that is called inside the function
        // reads a random pointer and crashes. We patch all of the 5 function calls
        // (one for each letter typed from "R", "O", "B", "I", "N") so that they are
        // the same as the English version.
        // Applies to at least: German floppy
        // Responsible method: unknown
        // Fixes bug: #5264
        static readonly ushort[] longbowSignatureShowHandCode = {
            0x78,                            // push1
            0x78,                            // push1
            0x72, Workarounds.SIG_ADDTOOFFSET(+2),       // lofsa (letter, that was typed)
            0x36,                            // push
            0x40, Workarounds.SIG_ADDTOOFFSET(+2),       // call
            0x02,                            // perform the call above with 2 parameters
            0x36,                            // push
            0x40, Workarounds.SIG_ADDTOOFFSET(+2),       // call
            Workarounds.SIG_MAGICDWORD,
            0x02,                            // perform the call above with 2 parameters
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_setMotion), // pushi "setMotion" (0x11c in Longbow German)
            0x39, Workarounds.SIG_SELECTOR8(ScriptPatcherSelectors.SELECTOR_x),          // pushi "x" (0x04 in Longbow German)
            0x51, 0x1e,                      // class MoveTo
            Workarounds.SIG_END
        };

        static readonly ushort[] longbowPatchShowHandCode = {
            0x39, 0x01,                      // pushi 1 (combine the two push1's in one, like in the English version)
            Workarounds.PATCH_ADDTOOFFSET(+3),           // leave the lofsa call untouched
            // The following will remove the duplicate call
            0x32, Workarounds.PATCH_UINT16_1(0x0002), Workarounds.PATCH_UINT16_2(0x0002),      // jmp 02 - skip 2 bytes (the remainder of the first call)
            0x48,                            // ret (dummy, should never be reached)
            0x48,                            // ret (dummy, should never be reached)
            Workarounds.PATCH_END
        };

        // When walking through the forest, arithmetic errors may occur at "random".
        // The scripts try to add a value and a pointer to the object "berryBush".
        //
        // This is caused by a local variable overflow.
        //
        // The scripts create berry bush objects dynamically. The array storage for
        // those bushes may hold a total of 8 bushes. But sometimes 10 bushes
        // are created. This overwrites 2 additional locals in script 225 and
        // those locals are used normally for value lookups.
        //
        // Changing the total of bushes could cause all sorts of other issues,
        // that's why I rather patched the code, that uses the locals for a lookup.
        // Which means it doesn't matter anymore when those locals are overwritten.
        //
        // Applies to at least: English PC floppy, German PC floppy, English Amiga floppy
        // Responsible method: export 2 of script 225
        // Fixes bug: #6751
        static readonly ushort[] longbowSignatureBerryBushFix = {
            0x89, 0x70,                      // lsg global[70h]
            0x35, 0x03,                      // ldi 03h
            0x1a,                            // eq?
            0x2e, Workarounds.SIG_UINT16_1(0x002d), Workarounds.SIG_UINT16_2(0x002d),        // bt [process code]
            0x89, 0x70,                      // lsg global[70h]
            0x35, 0x04,                      // ldi 04h
            0x1a,                            // eq?
            0x2e, Workarounds.SIG_UINT16_1(0x0025), Workarounds.SIG_UINT16_2(0x0025),        // bt [process code]
            0x89, 0x70,                      // lsg global[70h]
            0x35, 0x05,                      // ldi 05h
            0x1a,                            // eq?
            0x2e, Workarounds.SIG_UINT16_1(0x001d), Workarounds.SIG_UINT16_2(0x001d),        // bt [process code]
            0x89, 0x70,                      // lsg global[70h]
            0x35, 0x06,                      // ldi 06h
            0x1a,                            // eq?
            0x2e, Workarounds.SIG_UINT16_1(0x0015), Workarounds.SIG_UINT16_2(0x0015),        // bt [process code]
            0x89, 0x70,                      // lsg global[70h]
            0x35, 0x18,                      // ldi 18h
            0x1a,                            // eq?
            0x2e, Workarounds.SIG_UINT16_1(0x000d), Workarounds.SIG_UINT16_2(0x000d),        // bt [process code]
            0x89, 0x70,                      // lsg global[70h]
            0x35, 0x19,                      // ldi 19h
            0x1a,                            // eq?
            0x2e, Workarounds.SIG_UINT16_1(0x0005), Workarounds.SIG_UINT16_2(0x0005),        // bt [process code]
            0x89, 0x70,                      // lsg global[70h]
            0x35, 0x1a,                      // ldi 1Ah
            0x1a,                            // eq?
            // jump location for the "bt" instructions
            0x30, Workarounds.SIG_UINT16_1(0x0011), Workarounds.SIG_UINT16_2(0x0011),        // bnt [skip over follow up code, to offset 0c35]
            // 55 bytes until here
            0x85, 00,                        // lat temp[0]
            Workarounds.SIG_MAGICDWORD,
            0x9a, Workarounds.SIG_UINT16_1(0x0110), Workarounds.SIG_UINT16_2(0x0110),        // lsli local[110h] -> 110h points normally to 110h / 2Bh
            // 5 bytes
            0x7a,                            // push2
            Workarounds.SIG_END
        };

        static readonly ushort[] longbowPatchBerryBushFix = {
            Workarounds.PATCH_ADDTOOFFSET(+4),           // keep: lsg global[70h], ldi 03h
            0x22,                            // lt? (global < 03h)
            0x2f, 0x42,                      // bt [skip over all the code directly]
            0x89, 0x70,                      // lsg global[70h]
            0x35, 0x06,                      // ldi 06h
            0x24,                            // le? (global <= 06h)
            0x2f, 0x0e,                      // bt [to kRandom code]
            0x89, 0x70,                      // lsg global[70h]
            0x35, 0x18,                      // ldi 18h
            0x22,                            // lt? (global < 18h)
            0x2f, 0x34,                      // bt [skip over all the code directly]
            0x89, 0x70,                      // lsg global[70h]
            0x35, 0x1a,                      // ldi 1Ah
            0x24,                            // le? (global <= 1Ah)
            0x31, 0x2d,                      // bnt [skip over all the code directly]
            // 28 bytes, 27 bytes saved
            // kRandom code
            0x85, 0x00,                      // lat temp[0]
            0x2f, 0x05,                      // bt [skip over case 0]
            // temp[0] == 0
            0x38, Workarounds.SIG_UINT16_1(0x0110), Workarounds.SIG_UINT16_2(0x0110),        // pushi 0110h - that's what's normally at local[110h]
            0x33, 0x18,                      // jmp [kRandom call]
            // check temp[0] further
            0x78,                            // push1
            0x1a,                            // eq?
            0x31, 0x05,                      // bt [skip over case 1]
            // temp[0] == 1
            0x38, Workarounds.SIG_UINT16_1(0x002b), Workarounds.SIG_UINT16_2(0x002b),        // pushi 002Bh - that's what's normally at local[111h]
            0x33, 0x0F,                      // jmp [kRandom call]
            // temp[0] >= 2
            0x8d, 00,                        // lst temp[0]
            0x35, 0x02,                      // ldi 02
            0x04,                            // sub
            0x9a, Workarounds.SIG_UINT16_1(0x0112), Workarounds.SIG_UINT16_2(0x0112),        // lsli local[112h] -> look up value in 2nd table
                                             // this may not be needed at all and was just added for safety reasons
            // waste 9 spare bytes
            0x35, 0x00,                      // ldi 00
            0x35, 0x00,                      // ldi 00
            0x34, Workarounds.SIG_UINT16_1(0x0000), Workarounds.SIG_UINT16_2(0x0000),      // ldi 0000
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                     patch
        static readonly SciScriptPatcherEntry[] longbowSignatures = {
            new SciScriptPatcherEntry(  true,   210, "hand code crash",                             5, longbowSignatureShowHandCode, longbowPatchShowHandCode),
            new SciScriptPatcherEntry(  true,   225, "arithmetic berry bush fix",                   1, longbowSignatureBerryBushFix, longbowPatchBerryBushFix),
        };

        // ===========================================================================
        // Leisure Suit Larry 2
        // On the plane, Larry is able to wear the parachute. This grants 4 points.
        // In early versions of LSL2, it was possible to get "unlimited" points by
        //  simply wearing it multiple times.
        // They fixed it in later versions by remembering, if the parachute was already
        //  used before.
        // But instead of adding it properly, it seems they hacked the script / forgot
        //  to replace script 0 as well, which holds information about how many global
        //  variables are allocated at the start of the game.
        // The script tries to read an out-of-bounds global variable, which somewhat
        //  "worked" in SSCI, but ScummVM/SCI doesn't allow that.
        // That's why those points weren't granted here at all.
        // We patch the script to use global 90, which seems to be unused in the whole game.
        // Applies to at least: English floppy
        // Responsible method: rm63Script::handleEvent
        // Fixes bug: #6346
        static readonly ushort[] larry2SignatureWearParachutePoints = {
            0x35, 0x01,                      // ldi 01
                    0xa1, Workarounds.SIG_MAGICDWORD, 0x8e,      // sag 8e
            0x80, Workarounds.SIG_UINT16_1(0x01e0), Workarounds.SIG_UINT16_2(0x01e0),        // lag 1e0
            0x18,                            // not
            0x30, Workarounds.SIG_UINT16_1(0x000f), Workarounds.SIG_UINT16_2(0x000f),        // bnt [don't give points]
            0x35, 0x01,                      // ldi 01
            0xa0, 0xe0, 0x01,                // sag 1e0
            Workarounds.SIG_END
        };

        static readonly ushort[] larry2PatchWearParachutePoints = {
            Workarounds.PATCH_ADDTOOFFSET(+4),
            0x80, Workarounds.PATCH_UINT16_1(0x005a), Workarounds.PATCH_UINT16_2(0x005a),      // lag 5a (global 90)
            Workarounds.PATCH_ADDTOOFFSET(+6),
            0xa0, Workarounds.PATCH_UINT16_1(0x005a), Workarounds.PATCH_UINT16_2(0x005a),      // sag 5a (global 90)
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                           patch
        static readonly SciScriptPatcherEntry[] larry2Signatures = {
            new SciScriptPatcherEntry(  true,    63, "plane: no points for wearing plane",          1, larry2SignatureWearParachutePoints, larry2PatchWearParachutePoints),
        };

        // ===========================================================================
        // Leisure Suit Larry 5
        // In Miami the player can call the green card telephone number and get
        //  green card including limo at the same time in the English 1.000 PC release.
        // This results later in a broken game in case the player doesn't read
        //  the second telephone number for the actual limousine service, because
        //  in that case it's impossible for the player to get back to the airport.
        //
        // We disable the code, that is responsible to make the limo arrive.
        //
        // This bug was fixed in the European (dual language) versions of the game.
        //
        // Applies to at least: English PC floppy (1.000)
        // Responsible method: sPhone::changeState(40)
        static readonly ushort[] larry5SignatureGreenCardLimoBug = {
            0x7a,                               // push2
            Workarounds.SIG_MAGICDWORD,
            0x39, 0x07,                         // pushi 07
            0x39, 0x0c,                         // pushi 0Ch
            0x45, 0x0a, 0x04,                   // call export 10 of script 0
            0x78,                               // push1
            0x39, 0x26,                         // pushi 26h (limo arrived flag)
            0x45, 0x07, 0x02,                   // call export 7 of script 0 (sets flag)
            Workarounds.SIG_END
        };

        static readonly ushort[] larry5PatchGreenCardLimoBug = {
            Workarounds.PATCH_ADDTOOFFSET(+8),
            0x34, Workarounds.PATCH_UINT16_1(0), Workarounds.PATCH_UINT16_2(0),              // ldi 0000 (dummy)
            0x34, Workarounds.PATCH_UINT16_1(0),  Workarounds.PATCH_UINT16_2(0),             // ldi 0000 (dummy)
            Workarounds.PATCH_END
        };

        // In one of the conversations near the end (to be exact - room 380 and the text
        //  about using champagne on Reverse Biaz - only used when you actually did that
        //  in the game), the German text is too large, causing the textbox to get too large.
        // Because of that the talking head of Patti is drawn over the textbox. A translation oversight.
        // Applies to at least: German floppy
        // Responsible method: none, position of talker object on screen needs to get modified
        static readonly ushort[] larry5SignatureGermanEndingPattiTalker = {
            Workarounds.SIG_MAGICDWORD,
            Workarounds.SIG_UINT16_1(0x006e), Workarounds.SIG_UINT16_2(0x006e),                 // object pattiTalker::x (110)
            Workarounds.SIG_UINT16_1(0x00b4), Workarounds.SIG_UINT16_2(0x00b4),                 // object pattiTalker::y (180)
            Workarounds.SIG_ADDTOOFFSET(+469),              // verify that it's really the German version
            0x59, 0x6f, 0x75,                   // (object name) "You"
            0x23, 0x47, 0x44, 0x75,             // "#GDu"
            Workarounds.SIG_END
        };

        static readonly ushort[] larry5PatchGermanEndingPattiTalker = {
            Workarounds.PATCH_UINT16_1(0x005a), Workarounds.PATCH_UINT16_2(0x005a),               // change pattiTalker::x to 90
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                               patch
        static readonly SciScriptPatcherEntry[] larry5Signatures = {
            new SciScriptPatcherEntry(  true,   280, "English-only: fix green card limo bug",       1, larry5SignatureGreenCardLimoBug,        larry5PatchGreenCardLimoBug),
            new SciScriptPatcherEntry(  true,   380, "German-only: Enlarge Patti Textbox",          1, larry5SignatureGermanEndingPattiTalker, larry5PatchGermanEndingPattiTalker),
        };

        // ===========================================================================
        // this is called on every death dialog. Problem is at least the german
        //  version of lsl6 gets title text that is far too long for the
        //  available temp space resulting in temp space corruption
        //  This patch moves the title text around, so this overflow
        //  doesn't happen anymore. We would otherwise get a crash
        //  calling for invalid views (this happens of course also
        //  in sierra sci)
        // Applies to at least: German PC-CD
        // Responsible method: unknown
        static readonly ushort[] larry6SignatureDeathDialog = {
            Workarounds.SIG_MAGICDWORD,
            0x3e, Workarounds.SIG_UINT16_1(0x0133), Workarounds.SIG_UINT16_2(0x0133),        // link 0133 (offset 0x20)
            0x35, 0xff,                      // ldi ff
            0xa3, 0x00,                      // sal 00
            Workarounds.SIG_ADDTOOFFSET(+680),           // [skip 680 bytes]
            0x8f, 0x01,                      // lsp 01 (offset 0x2cf)
            0x7a,                            // push2
            0x5a, Workarounds.SIG_UINT16_1(0x0004), Workarounds.SIG_UINT16_2(0x0004), Workarounds.SIG_UINT16_1(0x010e), Workarounds.SIG_UINT16_2(0x010e), // lea 0004 010e
            0x36,                            // push
            0x43, 0x7c, 0x0e,                // kMessage[7c] 0e
            Workarounds.SIG_ADDTOOFFSET(+90),            // [skip 90 bytes]
            0x38, Workarounds.SIG_UINT16_1(0x00d6), Workarounds.SIG_UINT16_2(0x00d6),        // pushi 00d6 (offset 0x335)
            0x78,                            // push1
            0x5a, Workarounds.SIG_UINT16_1(0x0004), Workarounds.SIG_UINT16_2(0x0004), Workarounds.SIG_UINT16_1(0x010e), Workarounds.SIG_UINT16_2(0x010e), // lea 0004 010e
            0x36,                            // push
            Workarounds.SIG_ADDTOOFFSET(+76),            // [skip 76 bytes]
            0x38, Workarounds.SIG_UINT16_1(0x00cd), Workarounds.SIG_UINT16_2(0x00cd),        // pushi 00cd (offset 0x38b)
            0x39, 0x03,                      // pushi 03
            0x5a, Workarounds.SIG_UINT16_1(0x0004), Workarounds.SIG_UINT16_2(0x0004), Workarounds.SIG_UINT16_1(0x010e), Workarounds.SIG_UINT16_2(0x010e), // lea 0004 010e
            0x36,
            Workarounds.SIG_END
        };

        static readonly ushort[] larry6PatchDeathDialog = {
            0x3e, 0x00, 0x02,                // link 0200
            Workarounds.PATCH_ADDTOOFFSET(+687),
            0x5a, Workarounds.PATCH_UINT16_1(0x0004), Workarounds.PATCH_UINT16_2(0x0004), Workarounds.PATCH_UINT16_1(0x0140), Workarounds.PATCH_UINT16_2(0x0140), // lea 0004 0140
            Workarounds.PATCH_ADDTOOFFSET(+98),
            0x5a, Workarounds.PATCH_UINT16_1(0x0004), Workarounds.PATCH_UINT16_2(0x0004), Workarounds.PATCH_UINT16_1(0x0140), Workarounds.PATCH_UINT16_2(0x0140), // lea 0004 0140
            Workarounds.PATCH_ADDTOOFFSET(+82),
            0x5a, Workarounds.PATCH_UINT16_1(0x0004), Workarounds.PATCH_UINT16_2(0x0004), Workarounds.PATCH_UINT16_1(0x0140), Workarounds.PATCH_UINT16_2(0x0140), // lea 0004 0140
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                   patch
        static readonly SciScriptPatcherEntry[] larry6Signatures = {
            new SciScriptPatcherEntry(  true,    82, "death dialog memory corruption",              1, larry6SignatureDeathDialog, larry6PatchDeathDialog),
        };

        // ===========================================================================
        // Mother Goose SCI1/SCI1.1
        // MG::replay somewhat calculates the savedgame-id used when saving again
        //  this doesn't work right and we remove the code completely.
        //  We set the savedgame-id directly right after restoring in kRestoreGame.
        //  We also draw the background picture in here instead.
        //  This Mixed Up Mother Goose draws the background picture before restoring,
        //  instead of doing it properly in MG::replay. This fixes graphic issues,
        //  when restoring from GMM.
        //
        // Applies to at least: English SCI1 CD, English SCI1.1 floppy, Japanese FM-Towns
        // Responsible method: MG::replay (script 0)
        static readonly ushort[] mothergoose256SignatureReplay = {
            0x7a,                            // push2
            0x78,                            // push1
            0x5b, 0x00, 0xbe,                // lea global[BEh]
            0x36,                            // push
            0x43, 0x70, 0x04,                // callk MemorySegment
            0x7a,                            // push2
            0x5b, 0x00, 0xbe,                // lea global[BEh]
            0x36,                            // push
            0x76,                            // push0
            0x43, 0x62, 0x04,                // callk StrAt
            0xa1, 0xaa,                      // sag global[AAh]
            0x7a,                            // push2
            0x5b, 0x00, 0xbe,                // lea global[BEh]
            0x36,                            // push
            0x78,                            // push1
            0x43, 0x62, 0x04,                // callk StrAt
            0x36,                            // push
            0x35, 0x20,                      // ldi 20
            0x04,                            // sub
            0xa1, Workarounds.SIG_ADDTOOFFSET(+1),       // sag global[57h] -> FM-Towns [9Dh]
            // 35 bytes
            0x39, 0x03,                      // pushi 03
            0x89, Workarounds.SIG_ADDTOOFFSET(+1),       // lsg global[1Dh] -> FM-Towns [1Eh]
            0x76,                            // push0
            0x7a,                            // push2
            0x5b, 0x00, 0xbe,                // lea global[BEh]
            0x36,                            // push
            0x7a,                            // push2
            0x43, 0x62, 0x04,                // callk StrAt
            0x36,                            // push
            0x35, 0x01,                      // ldi 01
            0x04,                            // sub
            0x36,                            // push
            0x43, 0x62, 0x06,                // callk StrAt
            // 22 bytes
            0x7a,                            // push2
            0x5b, 0x00, 0xbe,                // lea global[BE]
            0x36,                            // push
            0x39, 0x03,                      // pushi 03
            0x43, 0x62, 0x04,                // callk StrAt
            // 10 bytes
            0x36,                            // push
            0x35, Workarounds.SIG_MAGICDWORD, 0x20,      // ldi 20
            0x04,                            // sub
            0xa1, 0xb3,                      // sag global[b3]
            // 6 bytes
            Workarounds.SIG_END
        };

        static readonly ushort[] mothergoose256PatchReplay = {
            0x39, 0x06,                      // pushi 06
            0x76,                            // push0
            0x76,                            // push0
            0x38, Workarounds.PATCH_UINT16_1(200), Workarounds.PATCH_UINT16_2(200),         // pushi 200d
            0x38, Workarounds.PATCH_UINT16_1(320), Workarounds.PATCH_UINT16_2(320),         // pushi 320d
            0x76,                            // push0
            0x76,                            // push0
            0x43, 0x15, 0x0c,                // callk SetPort -> set picture port to full screen
            // 15 bytes
            0x39, 0x04,                      // pushi 04
            0x3c,                            // dup
            0x76,                            // push0
            0x38, Workarounds.PATCH_UINT16_1(255), Workarounds.PATCH_UINT16_2(255),         // pushi 255d
            0x76,                            // push0
            0x43, 0x6f, 0x08,                // callk Palette -> set intensity to 0 for all colors
            // 11 bytes
            0x7a,                            // push2
            0x38, Workarounds.PATCH_UINT16_1(800), Workarounds.PATCH_UINT16_2(800),         // pushi 800
            0x76,                            // push0
            0x43, 0x08, 0x04,                // callk DrawPic -> draw picture 800
            // 8 bytes
            0x39, 0x06,                      // pushi 06
            0x39, 0x0c,                      // pushi 0Ch
            0x76,                            // push0
            0x76,                            // push0
            0x38, Workarounds.PATCH_UINT16_1(200), Workarounds.PATCH_UINT16_2(200),         // push 200
            0x38, Workarounds.PATCH_UINT16_1(320), Workarounds.PATCH_UINT16_2(320),         // push 320
            0x78,                            // push1
            0x43, 0x6c, 0x0c,                // callk Graph -> send everything to screen
            // 16 bytes
            0x39, 0x06,                      // pushi 06
            0x76,                            // push0
            0x76,                            // push0
            0x38, Workarounds.PATCH_UINT16_1(156), Workarounds.PATCH_UINT16_2(156),         // pushi 156d
            0x38, Workarounds.PATCH_UINT16_1(258), Workarounds.PATCH_UINT16_2(258),         // pushi 258d
            0x39, 0x03,                      // pushi 03
            0x39, 0x04,                      // pushi 04
            0x43, 0x15, 0x0c,                // callk SetPort -> set picture port back
            // 17 bytes
            0x34, Workarounds.PATCH_UINT16_1(0x0000), Workarounds.PATCH_UINT16_2(0x0000),      // ldi 0000 (dummy)
            0x34, Workarounds.PATCH_UINT16_1(0x0000), Workarounds.PATCH_UINT16_2(0x0000),      // ldi 0000 (dummy)
            Workarounds.PATCH_END
        };

        // when saving, it also checks if the savegame ID is below 13.
        //  we change this to check if below 113 instead
        //
        // Applies to at least: English SCI1 CD, English SCI1.1 floppy, Japanese FM-Towns
        // Responsible method: Game::save (script 994 for SCI1), MG::save (script 0 for SCI1.1)
        static readonly ushort[] mothergoose256SignatureSaveLimit = {
            0x89, Workarounds.SIG_MAGICDWORD, 0xb3,      // lsg global[b3]
            0x35, 0x0d,                      // ldi 0d
            0x20,                            // ge?
            Workarounds.SIG_END
        };

        static readonly ushort[] mothergoose256PatchSaveLimit = {
            Workarounds.PATCH_ADDTOOFFSET(+2),
            0x35, 0x0d + Workarounds.SAVEGAMEID_OFFICIALRANGE_START, // ldi 113d
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                         patch
        static readonly SciScriptPatcherEntry[] mothergoose256Signatures = {
            new SciScriptPatcherEntry(  true,     0, "replay save issue",                           1, mothergoose256SignatureReplay,    mothergoose256PatchReplay ),
            new SciScriptPatcherEntry(  true,     0, "save limit dialog (SCI1.1)",                  1, mothergoose256SignatureSaveLimit, mothergoose256PatchSaveLimit ),
            new SciScriptPatcherEntry(  true,   994, "save limit dialog (SCI1)",                    1, mothergoose256SignatureSaveLimit, mothergoose256PatchSaveLimit ),
        };

        // ===========================================================================
        // Police Quest 1 VGA

        // When briefing is about to start in room 15, other officers will get into the room too.
        // When one of those officers gets into the way of ego, they will tell the player to sit down.
        // But control will be disabled right at that point. Ego may then go to his seat by himself,
        // or more often than not will just stand there. The player is unable to do anything.
        //
        // Sergeant Dooley will then enter the room. Tell the player to sit down 3 times and after
        // that it's game over.
        //
        // Because the Sergeant is telling the player to sit down, one has to assume that the player
        // is meant to still be in control. Which is why this script patch removes disabling of player control.
        //
        // The script also tries to make ego walk to the chair, but it fails because it gets stuck with other
        // actors. So I guess the safest way is to remove all of that and let the player do it manually.
        //
        // The responsible method seems to use a few hardcoded texts, which is why I have to assume that it's
        // not used anywhere else. I also checked all scripts and couldn't find any other calls to it.
        //
        // This of course also happens when using the original interpreter.
        //
        // Scripts work like this: manX::doit (script 134) triggers gab::changeState, which then triggers rm015::notify
        //
        // Applies to at least: English floppy
        // Responsible method: gab::changeState (script 152)
        // Fixes bug: #5865
        static readonly ushort[] pq1vgaSignatureBriefingGettingStuck = {
            0x76,                                // push0
            0x45, 0x02, 0x00,                    // call export 2 of script 0 (disable control)
            0x38, Workarounds.SIG_ADDTOOFFSET(+2),           // pushi notify
            0x76,                                // push0
            0x81, 0x02,                          // lag global[2] (get current room)
            0x4a, 0x04,                          // send 04
            Workarounds.SIG_MAGICDWORD,
            0x8b, 0x02,                          // lsl local[2]
            0x35, 0x01,                          // ldi 01
            0x02,                                // add
            Workarounds.SIG_END
        };

        static readonly ushort[] pq1vgaPatchBriefingGettingStuck = {
            0x33, 0x0a,                      // jmp to lsl local[2], skip over export 2 and ::notify
            Workarounds.PATCH_END                        // rm015::notify would try to make ego walk to the chair
        };

        // When at the police station, you can put or get your gun from your locker.
        // The script, that handles this, is buggy. It disposes the gun as soon as
        //  you click, but then waits 2 seconds before it also closes the locker.
        // Problem is that it's possible to click again, which then results in a
        //  disposed object getting accessed. This happened to work by pure luck in
        //  SSCI.
        // This patch changes the code, so that the gun is actually given away
        //  when the 2 seconds have passed and the locker got closed.
        // Applies to at least: English floppy
        // Responsible method: putGun::changeState (script 341)
        // Fixes bug: #5705 / #6400
        static readonly ushort[] pq1vgaSignaturePutGunInLockerBug = {
            0x35, 0x00,                      // ldi 00
            0x1a,                            // eq?
            0x31, 0x25,                      // bnt [next state check]
            Workarounds.SIG_ADDTOOFFSET(+22),            // [skip 22 bytes]
            Workarounds.SIG_MAGICDWORD,
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_put),       // pushi "put"
            0x78,                            // push1
            0x76,                            // push0
            0x81, 0x00,                      // lag 00
            0x4a, 0x06,                      // send 06 - ego::put(0)
            0x35, 0x02,                      // ldi 02
            0x65, 0x1c,                      // aTop 1c (set timer to 2 seconds)
            0x33, 0x0e,                      // jmp [end of method]
            0x3c,                            // dup --- next state check target
            0x35, 0x01,                      // ldi 01
            0x1a,                            // eq?
            0x31, 0x08,                      // bnt [end of method]
            0x39, Workarounds.SIG_SELECTOR8(ScriptPatcherSelectors.SELECTOR_dispose),    // pushi "dispose"
            0x76,                            // push0
            0x72, Workarounds.SIG_UINT16_1(0x0088), Workarounds.SIG_UINT16_2(0x0088),        // lofsa 0088
            0x4a, 0x04,                      // send 04 - locker::dispose
            Workarounds.SIG_END
        };

        static readonly ushort[] pq1vgaPatchPutGunInLockerBug = {
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0x31, 0x1c,                      // bnt [next state check]
            Workarounds.PATCH_ADDTOOFFSET(+22),
            0x35, 0x02,                      // ldi 02
            0x65, 0x1c,                      // aTop 1c (set timer to 2 seconds)
            0x33, 0x17,                      // jmp [end of method]
            0x3c,                            // dup --- next state check target
            0x35, 0x01,                      // ldi 01
            0x1a,                            // eq?
            0x31, 0x11,                      // bnt [end of method]
            0x38, Workarounds.PATCH_SELECTOR16(ScriptPatcherSelectors.SELECTOR_put),     // pushi "put"
            0x78,                            // push1
            0x76,                            // push0
            0x81, 0x00,                      // lag 00
            0x4a, 0x06,                      // send 06 - ego::put(0)
            Workarounds.PATCH_END
        };

        // When restoring a saved game, which was made while driving around,
        //  the game didn't redraw the map. This also happened in Sierra SCI.
        //
        // The map is a picture resource and drawn over the main picture.
        //  This is called an "overlay" in SCI. This wasn't implemented properly.
        //  We fix it by actually implementing it properly.
        //
        // Applies to at least: English floppy
        // Responsible method: rm500::init, changeOverlay::changeState (script 500)
        // Fixes bug: #5016
        static readonly ushort[] pq1vgaSignatureMapSaveRestoreBug = {
            0x39, 0x04,                          // pushi 04
            Workarounds.SIG_ADDTOOFFSET(+2),                 // skip either lsg global[f9] or pTos register
            Workarounds.SIG_MAGICDWORD,
            0x38, 0x64, 0x80,                    // pushi 8064
            0x76,                                // push0
            0x89, 0x28,                          // lsg global[28]
            0x43, 0x08, 0x08,                    // kDrawPic (8)
            Workarounds.SIG_END
        };

        static readonly ushort[] pq1vgaPatchMapSaveRestoreBug = {
            0x38, Workarounds.PATCH_SELECTOR16(ScriptPatcherSelectors.SELECTOR_overlay), // pushi "overlay"
            0x7a,                            // push2
            0x89, 0xf9,                      // lsg global[f9]
            0x39, 0x64,                      // pushi 64 (no transition)
            0x81, 0x02,                      // lag global[02] (current room object)
            0x4a, 0x08,                      // send 08
            0x18,                            // not (waste byte)
            Workarounds.PATCH_END
        };

        //          script, description,                                         signature                            patch
        static readonly SciScriptPatcherEntry[] pq1vgaSignatures = {
            new SciScriptPatcherEntry(  true,   152, "getting stuck while briefing is about to start", 1, pq1vgaSignatureBriefingGettingStuck, pq1vgaPatchBriefingGettingStuck ),
            new SciScriptPatcherEntry(  true,   341, "put gun in locker bug",                          1, pq1vgaSignaturePutGunInLockerBug,    pq1vgaPatchPutGunInLockerBug ),
            new SciScriptPatcherEntry(  true,   500, "map save/restore bug",                           2, pq1vgaSignatureMapSaveRestoreBug,    pq1vgaPatchMapSaveRestoreBug ),
        };

        // ===========================================================================
        //  At the healer's house there is a bird's nest up on the tree.
        //   The player can throw rocks at it until it falls to the ground.
        //   The hero will then grab the item, that is in the nest.
        //
        //  When running is active, the hero will not reach the actual destination
        //   and because of that, the game will get stuck.
        //
        //  We just change the coordinate of the destination slightly, so that walking,
        //   sneaking and running work.
        //
        //  This bug was fixed by Sierra at least in the Japanese PC-9801 version.
        // Applies to at least: English floppy (1.000, 1.012)
        // Responsible method: pickItUp::changeState (script 54)
        // Fixes bug: #6407
        static readonly ushort[] qfg1egaSignatureThrowRockAtNest = {
            0x4a, 0x04,                         // send 04 (nest::x)
            0x36,                               // push
            Workarounds.SIG_MAGICDWORD,
            0x35, 0x0f,                         // ldi 0f (15d)
            0x02,                               // add
            0x36,                               // push
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg1egaPatchThrowRockAtNest = {
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0x35, 0x12,                         // ldi 12 (18d)
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                            patch
        static readonly SciScriptPatcherEntry[] qfg1egaSignatures = {
            new SciScriptPatcherEntry(  true,    54, "throw rock at nest while running",            1, qfg1egaSignatureThrowRockAtNest,     qfg1egaPatchThrowRockAtNest ),
        };

        // ===========================================================================
        //  script 215 of qfg1vga pointBox::doit actually processes button-presses
        //   during fighting with monsters. It strangely also calls kGetEvent. Because
        //   the main User::doit also calls kGetEvent it's pure luck, where the event
        //   will hit. It's the same issue as in freddy pharkas and if you turn dos-box
        //   to max cycles, sometimes clicks also won't get registered. Strangely it's
        //   not nearly as bad as in our sci, but these differences may be caused by
        //   timing.
        //   We just reuse the active event, thus removing the duplicate kGetEvent call.
        // Applies to at least: English floppy
        // Responsible method: pointBox::doit
        static readonly ushort[] qfg1vgaSignatureFightEvents = {
            0x39, Workarounds.SIG_MAGICDWORD,
            Workarounds.SIG_SELECTOR8(ScriptPatcherSelectors.SELECTOR_new),                 // pushi "new"
            0x76,                               // push0
            0x51, 0x07,                         // class Event
            0x4a, 0x04,                         // send 04 - call Event::new
            0xa5, 0x00,                         // sat temp[0]
            0x78,                               // push1
            0x76,                               // push0
            0x4a, 0x04,                         // send 04 - read Event::x
            0xa5, 0x03,                         // sat temp[3]
            0x76,                               // push0 (selector y)
            0x76,                               // push0
            0x85, 0x00,                         // lat temp[0]
            0x4a, 0x04,                         // send 04 - read Event::y
            0x36,                               // push
            0x35, 0x0a,                         // ldi 0a
            0x04,                               // sub (poor mans localization) ;-)
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg1vgaPatchFightEvents = {
            0x38, Workarounds.PATCH_SELECTOR16(ScriptPatcherSelectors.SELECTOR_curEvent), // pushi 15a (selector curEvent)
            0x76,                            // push0
            0x81, 0x50,                      // lag global[50]
            0x4a, 0x04,                      // send 04 - read User::curEvent -> needs one byte more than previous code
            0xa5, 0x00,                      // sat temp[0]
            0x78,                            // push1
            0x76,                            // push0
            0x4a, 0x04,                      // send 04 - read Event::x
            0xa5, 0x03,                      // sat temp[3]
            0x76,                            // push0 (selector y)
            0x76,                            // push0
            0x85, 0x00,                      // lat temp[0]
            0x4a, 0x04,                      // send 04 - read Event::y
            0x39, 0x00,                      // pushi 00
            0x02,                            // add (waste 3 bytes) - we don't need localization, User::doit has already done it
            Workarounds.PATCH_END
        };

        // Script 814 of QFG1VGA is responsible for showing dialogs. However, the death
        // screen message shown when the hero dies in room 64 (ghost room) is too large
        // (254 chars long). Since the window header and main text are both stored in
        // temp space, this is an issue, as the scripts read the window header, then the
        // window text, which erases the window header text because of its length. To
        // fix that, we allocate more temp space and move the pointer used for the
        // window header a little bit, wherever it's used in script 814.
        // Fixes bug: #6139.

        // Patch 1: Increase temp space
        static readonly ushort[] qfg1vgaSignatureTempSpace = {
            Workarounds.SIG_MAGICDWORD,
            0x3f, 0xba,                         // link 0xba
            0x87, 0x00,                         // lap 0
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg1vgaPatchTempSpace = {
            0x3f, 0xca,                         // link 0xca
            Workarounds.PATCH_END
        };

        // Patch 2: Move the pointer used for the window header a little bit
        static readonly ushort[] qfg1vgaSignatureDialogHeader = {
            Workarounds.SIG_MAGICDWORD,
            0x5b, 0x04, 0x80,                   // lea temp[0x80]
            0x36,                               // push
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg1vgaPatchDialogHeader = {
            0x5b, 0x04, 0x90,                   // lea temp[0x90]
            Workarounds.PATCH_END
        };

        // When clicking on the crusher in room 331, Ego approaches him to talk to him,
        // an action that is handled by moveToCrusher::changeState in script 331. The
        // scripts set Ego to move close to the crusher, but when Ego is sneaking instead
        // of walking, the target coordinates specified by script 331 are never reached,
        // as Ego is making larger steps, and never reaches the required spot. This is an
        // edge case that can occur when Ego is set to sneak. Normally, when clicking on
        // the crusher, ego is supposed to move close to position 79, 165. We change it
        // to 85, 165, which is not an edge case thus the freeze is avoided.
        // Fixes bug: #6180
        static readonly ushort[] qfg1vgaSignatureMoveToCrusher = {
            Workarounds.SIG_MAGICDWORD,
            0x51, 0x1f,                         // class Motion
            0x36,                               // push
            0x39, 0x4f,                         // pushi 4f (79 - x)
            0x38, Workarounds.SIG_UINT16_1(0x00a5), Workarounds.SIG_UINT16_2(0x00a5),           // pushi 00a5 (165 - y)
            0x7c,                               // pushSelf
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg1vgaPatchMoveToCrusher = {
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0x39, 0x55,                         // pushi 55 (85 - x)
            Workarounds.PATCH_END
        };

        // Same pathfinding bug as above, where Ego is set to move to an impossible
        // spot when sneaking. In GuardsTrumpet::changeState, we change the final
        // location where Ego is moved from 111, 111 to 116, 116.
        // target coordinate is really problematic here.
        //
        // 114, 114 works when the speed slider is all the way up, but doesn't work
        // when the speed slider is not.
        //
        // It seems that this bug was fixed by Sierra for the Macintosh version.
        //
        // Applies to at least: English PC floppy
        // Responsible method: GuardsTrumpet::changeState(8)
        // Fixes bug: #6248
        static readonly ushort[] qfg1vgaSignatureMoveToCastleGate = {
            0x51, Workarounds.SIG_ADDTOOFFSET(+1),          // class MoveTo
            Workarounds.SIG_MAGICDWORD,
            0x36,                               // push
            0x39, 0x6f,                         // pushi 6f (111d)
            0x3c,                               // dup (111d) - coordinates 111, 111
            0x7c,                               // pushSelf
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg1vgaPatchMoveToCastleGate = {
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0x39, 0x74,                         // pushi 74 (116d), changes coordinates to 116, 116
            Workarounds.PATCH_END
        };

        // Typo in the original Sierra scripts
        // Looking at a cheetaur resulted in a text about a Saurus Rex
        // The code treats both monster types the same.
        // Applies to at least: English floppy
        // Responsible method: smallMonster::doVerb
        // Fixes bug #6249
        static readonly ushort[] qfg1vgaSignatureCheetaurDescription = {
            Workarounds.SIG_MAGICDWORD,
            0x34, Workarounds.SIG_UINT16_1(0x01b8), Workarounds.SIG_UINT16_2(0x01b8),           // ldi 01b8
            0x1a,                               // eq?
            0x31, 0x16,                         // bnt 16
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_say),          // pushi 0127h (selector "say")
            0x39, 0x06,                         // pushi 06
            0x39, 0x03,                         // pushi 03
            0x78,                               // push1
            0x39, 0x12,                         // pushi 12 -> monster type Saurus Rex
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg1vgaPatchCheetaurDescription = {
            Workarounds.PATCH_ADDTOOFFSET(+14),
            0x39, 0x11,                         // pushi 11 -> monster type cheetaur
            Workarounds.PATCH_END
        };

        // In the "funny" room (Yorick's room) in QfG1 VGA, pulling the chain and
        //  then pressing the button on the right side of the room results in
        //  a broken game. This also happens in SSCI.
        // Problem is that the Sierra programmers forgot to disable the door, that
        //  gets opened by pulling the chain. So when ego falls down and then
        //  rolls through the door, one method thinks that the player walks through
        //  it and acts that way and the other method is still doing the roll animation.
        // Local 5 of that room is a timer, that closes the door (object door11).
        // Setting it to 1 during happyFace::changeState(0) stops door11::doit from
        //  calling goTo6::init, so the whole issue is stopped from happening.
        //
        // Applies to at least: English floppy
        // Responsible method: happyFace::changeState, door11::doit
        // Fixes bug #6181
        static readonly ushort[] qfg1vgaSignatureFunnyRoomFix = {
            0x65, 0x14,                         // aTop 14 (state)
            0x36,                               // push
            0x3c,                               // dup
            0x35, 0x00,                         // ldi 00
            0x1a,                               // eq?
            0x30, Workarounds.SIG_UINT16_1(0x0025), Workarounds.SIG_UINT16_2(0x0025),           // bnt 0025 [-> next state]
            Workarounds.SIG_MAGICDWORD,
            0x35, 0x01,                         // ldi 01
            0xa3, 0x4e,                         // sal 4e
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg1vgaPatchFunnyRoomFix = {
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0x2e, Workarounds.PATCH_UINT16_1(0x0029), Workarounds.PATCH_UINT16_2(0x0029),         // bt 0029 [-> next state] - saves 4 bytes
            0x35, 0x01,                         // ldi 01
            0xa3, 0x4e,                         // sal 4e
            0xa3, 0x05,                         // sal 05 (sets local 5 to 1)
            0xa3, 0x05,                         // and again to make absolutely sure (actually to waste 2 bytes)
            Workarounds.PATCH_END
        };

        // The player is able to buy (and also steal) potions in the healer's hut
        //  Strangely Sierra delays the actual buy/get potion code for 60 ticks
        //  Why they did that is unknown. The code is triggered anyway only after
        //  the relevant dialog boxes are closed.
        //
        // This delay causes problems in case the user quickly enters the inventory.
        // That's why we change the amount of ticks to 1, so that the remaining states
        //  are executed right after the dialog boxes are closed.
        //
        // Applies to at least: English floppy
        // Responsible method: cueItScript::changeState
        // Fixes bug #6706
        static readonly ushort[] qfg1vgaSignatureHealerHutNoDelay = {
            0x65, 0x14,                         // aTop 14 (state)
            0x36,                               // push
            0x3c,                               // dup
            0x35, 0x00,                         // ldi 00
            0x1a,                               // eq?
            0x31, 0x07,                         // bnt 07 [-> next state]
            Workarounds.SIG_MAGICDWORD,
            0x35, 0x3c,                         // ldi 3c (60 ticks)
            0x65, 0x20,                         // aTop ticks
            0x32,                               // jmp [-> end of method]
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg1vgaPatchHealerHutNoDelay = {
            Workarounds.PATCH_ADDTOOFFSET(+9),
            0x35, 0x01,                         // ldi 01 (1 tick only, so that execution will resume as soon as dialog box is closed)
            Workarounds.PATCH_END
        };

        // When following the white stag, you can actually enter the 2nd room from the mushroom/fairy location,
        //  which results in ego entering from the top. When you then throw a dagger at the stag, one animation
        //  frame will stay on screen, because of a script bug.
        //
        // Applies to at least: English floppy, Mac floppy
        // Responsible method: stagHurt::changeState
        // Fixes bug #6135
        static readonly ushort[] qfg1vgaSignatureWhiteStagDagger = {
            0x87, 0x01,                         // lap param[1]
            0x65, 0x14,                         // aTop state
            0x36,                               // push
            0x3c,                               // dup
            0x35, 0x00,                         // ldi 0
            0x1a,                               // eq?
            0x31, 0x16,                         // bnt [next parameter check]
            0x76,                               // push0
            0x45, 0x02, 0x00,                   // callb export 2 from script 0, 0
            Workarounds.SIG_MAGICDWORD,
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_say),          // pushi 0127h (selector "say")
            0x39, 0x05,                         // pushi 05
            0x39, 0x03,                         // pushi 03
            0x39, 0x51,                         // pushi 51h
            0x76,                               // push0
            0x76,                               // push0
            0x7c,                               // pushSelf
            0x81, 0x5b,                         // lag global[5Bh] -> qg1Messager
            0x4a, 0x0e,                         // send 0Eh -> qg1Messager::say(3, 51h, 0, 0, stagHurt)
            0x33, 0x12,                         // jmp -> [ret]
            0x3c,                               // dup
            0x35, 0x01,                         // ldi 1
            0x1a,                               // eq?
            0x31, 0x0c,                         // bnt [ret]
            0x38,                               // pushi...
            Workarounds.SIG_ADDTOOFFSET(+11),
            0x3a,                               // toss
            0x48,                               // ret
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg1vgaPatchWhiteStagDagger = {
            Workarounds.PATCH_ADDTOOFFSET(+4),
            0x2f, 0x05,                         // bt [next check] (state != 0)
            // state = 0 code
            0x35, 0x01,                         // ldi 1
            0x65, 0x1a,                         // aTop cycles
            0x48,                               // ret
            0x36,                               // push
            0x35, 0x01,                         // ldi 1
            0x1a,                               // eq?
            0x31, 0x16,                         // bnt [state = 2 code]
            // state = 1 code
            0x76,                               // push0
            0x45, 0x02, 0x00,                   // callb export 2 from script 0, 0
            0x38, Workarounds.PATCH_SELECTOR16(ScriptPatcherSelectors.SELECTOR_say),        // pushi 0127h (selector "say")
            0x39, 0x05,                         // pushi 05
            0x39, 0x03,                         // pushi 03
            0x39, 0x51,                         // pushi 51h
            0x76,                               // push0
            0x76,                               // push0
            0x7c,                               // pushSelf
            0x81, 0x5b,                         // lag global[5Bh] -> qg1Messager
            0x4a, 0x0e,                         // send 0Eh -> qg1Messager::say(3, 51h, 0, 0, stagHurt)
            0x48,                               // ret
            // state = 2 code
            Workarounds.PATCH_ADDTOOFFSET(+13),
            0x48,                               // ret (remove toss)
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                            patch
        static readonly SciScriptPatcherEntry[] qfg1vgaSignatures = {
            new SciScriptPatcherEntry(  true,    41, "moving to castle gate",                       1, qfg1vgaSignatureMoveToCastleGate,    qfg1vgaPatchMoveToCastleGate ),
            new SciScriptPatcherEntry(  true,    55, "healer's hut, no delay for buy/steal",        1, qfg1vgaSignatureHealerHutNoDelay,    qfg1vgaPatchHealerHutNoDelay ),
            new SciScriptPatcherEntry(  true,    77, "white stag dagger throw animation glitch",    1, qfg1vgaSignatureWhiteStagDagger,     qfg1vgaPatchWhiteStagDagger ),
            new SciScriptPatcherEntry(  true,    96, "funny room script bug fixed",                 1, qfg1vgaSignatureFunnyRoomFix,        qfg1vgaPatchFunnyRoomFix ),
            new SciScriptPatcherEntry(  true,   210, "cheetaur description fixed",                  1, qfg1vgaSignatureCheetaurDescription, qfg1vgaPatchCheetaurDescription ),
            new SciScriptPatcherEntry(  true,   215, "fight event issue",                           1, qfg1vgaSignatureFightEvents,         qfg1vgaPatchFightEvents ),
            new SciScriptPatcherEntry(  true,   216, "weapon master event issue",                   1, qfg1vgaSignatureFightEvents,         qfg1vgaPatchFightEvents ),
            new SciScriptPatcherEntry(  true,   331, "moving to crusher",                           1, qfg1vgaSignatureMoveToCrusher,       qfg1vgaPatchMoveToCrusher ),
            new SciScriptPatcherEntry(  true,   814, "window text temp space",                      1, qfg1vgaSignatureTempSpace,           qfg1vgaPatchTempSpace ),
            new SciScriptPatcherEntry(  true,   814, "dialog header offset",                        3, qfg1vgaSignatureDialogHeader,        qfg1vgaPatchDialogHeader ),
        };


        // ===========================================================================
        // This is a very complicated bug.
        // When the player encounters an enemy in the desert while riding a saurus and later
        //  tries to get back on it by entering "ride", the game will not give control back
        //  to the player.
        //
        // This is caused by script mountSaurus getting triggered twice.
        //  Once by entering the command "ride" and then a second time by a proximity check.
        //
        // Both are calling mountSaurus::init() in script 20, this one disables controls
        //  then mountSaurus::changeState() from script 660 is triggered
        //  mountSaurus::changeState(5) finally calls mountSaurus::dispose(), which is also in script 20
        //  which finally re-enables controls
        //
        // A fix is difficult to implement. The code in script 20 is generic and used by multiple objects
        //
        // Originally I decided to change the responsible globals (66h and A1h) during mountSaurus::changeState(5).
        //  This worked as far as for controls, but mountSaurus::init changes a few selectors of ego as well, which
        //  won't get restored in that situation, which then messes up room changes and other things.
        //
        // I have now decided to change sheepScript::changeState(2) in script 665 instead.
        //
        // This fix could cause issues in case there is a cutscene, where ego is supposed to get onto the saurus using
        //  sheepScript.
        //
        // Applies to at least: English PC Floppy, English Amiga Floppy
        // Responsible method: mountSaurus::changeState(), mountSaurus::init(), mountSaurus::dispose()
        // Fixes bug: #5156
        static readonly ushort[] qfg2SignatureSaurusFreeze = {
            0x3c,                               // dup
            0x35, 0x02,                         // ldi 5
            Workarounds.SIG_MAGICDWORD,
            0x1a,                               // eq?
            0x30, Workarounds.SIG_UINT16_1(0x0043), Workarounds.SIG_UINT16_2(0x0043),           // bnt [ret]
            0x76,                               // push0
            Workarounds.SIG_ADDTOOFFSET(+61),               // skip to dispose code
            0x39, Workarounds.SIG_SELECTOR8(ScriptPatcherSelectors.SELECTOR_dispose),       // pushi "dispose"
            0x76,                               // push0
            0x54, 0x04,                         // self 04
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg2PatchSaurusFreeze = {
            0x81, 0x66,                         // lag 66h
            0x2e, Workarounds.SIG_UINT16_1(0x0040), Workarounds.SIG_UINT16_2(0x0040),           // bt [to dispose code]
            0x35, 0x00,                         // ldi 0 (waste 2 bytes)
            Workarounds.PATCH_END
        };

        // Script 944 in QFG2 contains the FileSelector system class, used in the
        // character import screen. This gets incorrectly called constantly, whenever
        // the user clicks on a button in order to refresh the file list. This was
        // probably done because it would be easier to refresh the list whenever the
        // user inserted a new floppy disk, or changed directory. The problem is that
        // the script has a bug, and it invalidates the text of the entries in the
        // list. This has a high probability of breaking, as the user could change the
        // list very quickly, or the garbage collector could kick in and remove the
        // deleted entries. We don't allow the user to change the directory, thus the
        // contents of the file list are constant, so we can avoid the constant file
        // and text entry refreshes whenever a button is pressed, and prevent possible
        // crashes because of these constant quick object reallocations.
        // Fixes bug: #5096
        static readonly ushort[] qfg2SignatureImportDialog = {
            0x63, Workarounds.SIG_MAGICDWORD, 0x20,         // pToa text
            0x30, Workarounds.SIG_UINT16_1(0x000b), Workarounds.SIG_UINT16_2(0x000b),           // bnt [next state]
            0x7a,                               // push2
            0x39, 0x03,                         // pushi 03
            0x36,                               // push
            0x43, 0x72, 0x04,                   // callk Memory 4
            0x35, 0x00,                         // ldi 00
            0x65, 0x20,                         // aTop text
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg2PatchImportDialog = {
            Workarounds.PATCH_ADDTOOFFSET(+5),
            0x48,                               // ret
            Workarounds.PATCH_END
        };

        // Quest For Glory 2 character import doesn't properly set the character type
        //  in versions 1.102 and below, which makes all importerted characters a fighter.
        //
        // Sierra released an official patch. However the fix is really easy to
        //  implement on our side, so we also patch the flaw in here in case we find it.
        //
        // The version released on GOG is 1.102 without this patch applied, so us
        //  patching it is quite useful.
        //
        // Applies to at least: English Floppy
        // Responsible method: importHero::changeState
        // Fixes bug: inside versions 1.102 and below
        static readonly ushort[] qfg2SignatureImportCharType = {
            0x35, 0x04,                         // ldi 04
            0x90, Workarounds.SIG_UINT16_1(0x023b), Workarounds.SIG_UINT16_2(0x023b),           // lagi global[23Bh]
            0x02,                               // add
            0x36,                               // push
            0x35, 0x04,                         // ldi 04
            0x08,                               // div
            0x36,                               // push
            0x35, 0x0d,                         // ldi 0D
            0xb0, Workarounds.SIG_UINT16_1(0x023b), Workarounds.SIG_UINT16_2(0x023b),           // sagi global[023Bh]
            0x8b, 0x1f,                         // lsl local[1Fh]
            0x35, 0x05,                         // ldi 05
            Workarounds.SIG_MAGICDWORD,
            0xb0, Workarounds.SIG_UINT16_1(0x0150), Workarounds.SIG_UINT16_2(0x0150),           // sagi global[0150h]
            0x8b, 0x02,                         // lsl local[02h]
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg2PatchImportCharType = {
            0x80, Workarounds.PATCH_UINT16_1(0x023f), Workarounds.PATCH_UINT16_2(0x023f),         // lag global[23Fh] <-- patched to save 2 bytes
            0x02,                               // add
            0x36,                               // push
            0x35, 0x04,                         // ldi 04
            0x08,                               // div
            0x36,                               // push
            0xa8, Workarounds.SIG_UINT16_1(0x0248), Workarounds.SIG_UINT16_2(0x0248),           // ssg global[0248h] <-- patched to save 2 bytes
            0x8b, 0x1f,                         // lsl local[1Fh]
            0xa8, Workarounds.SIG_UINT16_1(0x0155), Workarounds.SIG_UINT16_2(0x0155),           // ssg global[0155h] <-- patched to save 2 bytes
            // new code, directly from the official sierra patch file
            0x83, 0x01,                         // lal local[01h]
            0xa1, 0xbb,                         // sag global[BBh]
            0xa1, 0x73,                         // sag global[73h]
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                    patch
        static readonly SciScriptPatcherEntry[] qfg2Signatures = {
            new SciScriptPatcherEntry(  true,   665, "getting back on saurus freeze fix",           1, qfg2SignatureSaurusFreeze,   qfg2PatchSaurusFreeze ),
            new SciScriptPatcherEntry(  true,   805, "import character type fix",                   1, qfg2SignatureImportCharType, qfg2PatchImportCharType ),
            new SciScriptPatcherEntry(  true,   944, "import dialog continuous calls",              1, qfg2SignatureImportDialog,   qfg2PatchImportDialog ),
        };

        // ===========================================================================
        // Patch for the import screen in QFG3, same as the one for QFG2 above
        static readonly ushort[] qfg3SignatureImportDialog = {
            0x63, Workarounds.SIG_MAGICDWORD, 0x2a,         // pToa text
            0x31, 0x0b,                         // bnt [next state]
            0x7a,                               // push2
            0x39, 0x03,                         // pushi 03
            0x36,                               // push
            0x43, 0x72, 0x04,                   // callk Memory 4
            0x35, 0x00,                         // ldi 00
            0x65, 0x2a,                         // aTop text
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchImportDialog = {
            Workarounds.PATCH_ADDTOOFFSET(+4),
            0x48,                               // ret
            Workarounds.PATCH_END
        };


        // ===========================================================================
        // Patch for the Woo dialog option in Uhura's conversation.
        // Problem: The Woo dialog option (0xffb5) is negative, and therefore
        // treated as an option opening a submenu. This leads to uhuraTell::doChild
        // being called, which calls hero::solvePuzzle and then proceeds with
        // Teller::doChild to open the submenu. However, there is no actual submenu
        // defined for option -75 since -75 does not show up in uhuraTell::keys.
        // This will cause Teller::doChild to run out of bounds while scanning through
        // uhuraTell::keys.
        // Strategy: there is another conversation option in uhuraTell::doChild calling
        // hero::solvePuzzle (0xfffc) which does a ret afterwards without going to
        // Teller::doChild. We jump to this call of hero::solvePuzzle to get that same
        // behaviour.
        // Applies to at least: English, German, Italian, French, Spanish Floppy
        // Responsible method: uhuraTell::doChild
        // Fixes bug: #5172
        static readonly ushort[] qfg3SignatureWooDialog = {
            Workarounds.SIG_MAGICDWORD,
            0x67, 0x12,                         // pTos 12 (query)
            0x35, 0xb6,                         // ldi b6
            0x1a,                               // eq?
            0x2f, 0x05,                         // bt 05
            0x67, 0x12,                         // pTos 12 (query)
            0x35, 0x9b,                         // ldi 9b
            0x1a,                               // eq?
            0x31, 0x0c,                         // bnt 0c
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_solvePuzzle),  // pushi 0297
            0x7a,                               // push2
            0x38, Workarounds.SIG_UINT16_1(0x010c), Workarounds.SIG_UINT16_2(0x010c),           // pushi 010c
            0x7a,                               // push2
            0x81, 0x00,                         // lag 00
            0x4a, 0x08,                         // send 08
            0x67, 0x12,                         // pTos 12 (query)
            0x35, 0xb5,                         // ldi b5
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchWooDialog = {
            Workarounds.PATCH_ADDTOOFFSET(+0x29),
            0x33, 0x11,                         // jmp to 0x6a2, the call to hero::solvePuzzle for 0xFFFC
            Workarounds.PATCH_END
        };

        // Alternative version, with uint16 offsets, for GOG release of QfG3.
        static readonly ushort[] qfg3SignatureWooDialogAlt = {
            Workarounds.SIG_MAGICDWORD,
            0x67, 0x12,                         // pTos 12 (query)
            0x35, 0xb6,                         // ldi b6
            0x1a,                               // eq?
            0x2e, Workarounds.SIG_UINT16_1(0x0005), Workarounds.SIG_UINT16_2(0x0005),           // bt 05
            0x67, 0x12,                         // pTos 12 (query)
            0x35, 0x9b,                         // ldi 9b
            0x1a,                               // eq?
            0x30, Workarounds.SIG_UINT16_1(0x000c), Workarounds.SIG_UINT16_2(0x000c),           // bnt 0c
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_solvePuzzle),  // pushi 0297
            0x7a,                               // push2
            0x38, Workarounds.SIG_UINT16_1(0x010c), Workarounds.SIG_UINT16_2(0x010c),           // pushi 010c
            0x7a,                               // push2
            0x81, 0x00,                         // lag 00
            0x4a, 0x08,                         // send 08
            0x67, 0x12,                         // pTos 12 (query)
            0x35, 0xb5,                         // ldi b5
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchWooDialogAlt = {
            Workarounds.PATCH_ADDTOOFFSET(+0x2C),
            0x33, 0x12,                         // jmp to 0x708, the call to hero::solvePuzzle for 0xFFFC
            Workarounds.PATCH_END
        };

        // When exporting characters at the end of Quest for Glory 3, the underlying
        //  code has issues with values, that are above 9999.
        //  For further study: https://github.com/Blazingstix/QFGImporter/blob/master/QFGImporter/QFGImporter/QFG3.txt
        //
        // If a value is above 9999, parts or even the whole character file will get corrupted.
        //
        // We are fixing the code because of that. We are patching code, that is calculating the checksum
        //  and add extra code to lower such values to 9999.
        //
        // Applies to at least: English, French, German, Italian, Spanish floppy
        // Responsible method: saveHero::changeState
        // Fixes bug #6807
        static readonly ushort[] qfg3SignatureExportChar = {
            0x35, Workarounds.SIG_ADDTOOFFSET(+1),          // ldi  00 / ldi 01 (2 loops, we patch both)
            0xa5, 0x00,                         // sat  temp[0] [contains index to data]
            0x8d, 0x00,                         // lst  temp[0]
            Workarounds.SIG_MAGICDWORD,
            0x35, 0x2c,                         // ldi  2c
            0x22,                               // lt?  [index above or equal 2Ch (44d)?
            0x31, 0x23,                         // bnt  [exit loop]
            // from this point it's actually useless code, maybe a sci compiler bug
            0x8d, 0x00,                         // lst  temp[0]
            0x35, 0x01,                         // ldi  01
            0x02,                               // add
            0x9b, 0x00,                         // lsli local[0] ---------- load local[0 + ACC] onto stack
            0x8d, 0x00,                         // lst  temp[0]
            0x35, 0x01,                         // ldi  01
            0x02,                               // add
            0xb3, 0x00,                         // sali local[0] ---------- save stack to local[0 + ACC]
            // end of useless code
            0x8b, Workarounds.SIG_ADDTOOFFSET(+1),          // lsl  local[36h/37h] ---- load local[36h/37h] onto stack
            0x8d, 0x00,                         // lst  temp[0]
            0x35, 0x01,                         // ldi  01
            0x02,                               // add
            0x93, 0x00,                         // lali local[0] ---------- load local[0 + ACC] into ACC
            0x02,                               // add -------------------- add ACC + stack and put into ACC
            0xa3, Workarounds.SIG_ADDTOOFFSET(+1),          // sal  local[36h/37h] ---- save ACC to local[36h/37h]
            0x8d, 0x00,                         // lst temp[0] ------------ temp[0] to stack
            0x35, 0x02,                         // ldi 02
            0x02,                               // add -------------------- add 2 to stack
            0xa5, 0x00,                         // sat temp[0] ------------ save ACC to temp[0]
            0x33, 0xd6,                         // jmp [loop]
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchExportChar = {
            Workarounds.PATCH_ADDTOOFFSET(+11),
            0x85, 0x00,                         // lat  temp[0]
            0x9b, 0x01,                         // lsli local[0] + 1 ------ load local[ ACC + 1] onto stack
            0x3c,                               // dup
            0x34, Workarounds.PATCH_UINT16_1(0x2710), Workarounds.PATCH_UINT16_2(0x2710),         // ldi  2710h (10000d)
            0x2c,                               // ult? ------------------- is value smaller than 10000?
            0x2f, 0x0a,                         // bt   [jump over]
            0x3a,                               // toss
            0x38, Workarounds.PATCH_UINT16_1(0x270f), Workarounds.PATCH_UINT16_2(0x270f),         // pushi 270fh (9999d)
            0x3c,                               // dup
            0x85, 0x00,                         // lat  temp[0]
            0xba, Workarounds.PATCH_UINT16_1(0x0001), Workarounds.PATCH_UINT16_2(0x0001),         // ssli local[0] + 1 ------ save stack to local[ ACC + 1] (UINT16 to waste 1 byte)
            // jump offset
            0x83, Workarounds.PATCH_GETORIGINALBYTE(+26),   // lal  local[37h/36h] ---- load local[37h/36h] into ACC
            0x02,                               // add -------------------- add local[37h/36h] + data value
            Workarounds.PATCH_END
        };

        // Quest for Glory 3 doesn't properly import the character type of Quest for Glory 1 character files.
        //  This issue was never addressed. It's caused by Sierra reading data directly from the local
        //  area, which is only set by Quest For Glory 2 import data, instead of reading the properly set global variable.
        //
        // We fix it, by also directly setting the local variable.
        //
        // Applies to at least: English, French, German, Italian, Spanish floppy
        // Responsible method: importHero::changeState(4)
        static readonly ushort[] qfg3SignatureImportQfG1Char = {
            Workarounds.SIG_MAGICDWORD,
            0x82, Workarounds.SIG_UINT16_1(0x0238), Workarounds.SIG_UINT16_2(0x0238),           // lal local[0x0238]
            0xa0, Workarounds.SIG_UINT16_1(0x016a), Workarounds.SIG_UINT16_2(0x016a),           // sag global[0x016a]
            0xa1, 0x7d,                         // sag global[0x7d]
            0x35, 0x01,                         // ldi 01
            0x99, 0xfb,                         // lsgi global[0xfb]
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchImportQfG1Char = {
            Workarounds.PATCH_ADDTOOFFSET(+8),
            0xa3, 0x01,                         // sal 01           -> also set local[01]
            0x89, 0xfc,                         // lsg global[0xFD] -> save 2 bytes
            Workarounds.PATCH_END
        };

        // The chief in his hut (room 640) is not drawn using the correct priority,
        //  which results in a graphical glitch. This is a game bug and also happens
        //  in Sierra's SCI. We adjust priority accordingly to fix it.
        //
        // Applies to at least: English, French, German, Italian, Spanish floppy
        // Responsible method: heap in script 640
        // Fixes bug #5173
        static readonly ushort[] qfg3SignatureChiefPriority = {
            Workarounds.SIG_MAGICDWORD,
            Workarounds.SIG_UINT16_1(0x0002), Workarounds.SIG_UINT16_2(0x0002),                 // yStep     0x0002
            Workarounds.SIG_UINT16_1(0x0281), Workarounds.SIG_UINT16_2(0x0281),                 // view      0x0281
            Workarounds.SIG_UINT16_1(0x0000), Workarounds.SIG_UINT16_2(0x0000),                 // loop      0x0000
            Workarounds.SIG_UINT16_1(0x0000), Workarounds.SIG_UINT16_2(0x0000),                 // cel       0x0000
            Workarounds.SIG_UINT16_1(0x0000), Workarounds.SIG_UINT16_2(0x0000),                 // priority  0x0000
            Workarounds.SIG_UINT16_1(0x0000), Workarounds.SIG_UINT16_2(0x0000),                 // underbits 0x0000
            Workarounds.SIG_UINT16_1(0x1000), Workarounds.SIG_UINT16_2(0x1000),                 // signal    0x1000
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchChiefPriority = {
            Workarounds.PATCH_ADDTOOFFSET(+8),
            Workarounds.PATCH_UINT16_1(0x000A), Workarounds.PATCH_UINT16_2(0x000A),               // new priority 0x000A (10d)
            Workarounds.PATCH_ADDTOOFFSET(+2),
            Workarounds.PATCH_UINT16_1(0x1010), Workarounds.PATCH_UINT16_2(0x1010),               // signal       0x1010 (set fixed priority flag)
            Workarounds.PATCH_END
        };

        // There are 3 points that can't be achieved in the game. They should've been
        // awarded for telling Rakeesh and Kreesha (room 285) about the Simabni
        // initiation.
        // However the array of posibble messages the hero can tell in that room
        // (local 156) is missing the "Tell about Initiation" message (#31) which
        // awards these points.
        // This patch adds the message to that array, thus allowing the hero to tell
        // that message (after completing the initiation) and gain the 3 points.
        // A side effect of increasing the local156 array is that the next local
        // array is shifted and shrinks in size from 4 words to 3. The patch changes
        // the 2 locations in the script that reference that array, to point to the new
        // location ($aa --> $ab). It is safe to shrink the 2nd array to 3 words
        // because only the first element in it is ever used.
        //
        // Note: You have to re-enter the room in case a saved game was loaded from a
        // previous version of ScummVM and that saved game was made inside that room.
        //
        // Applies to: English, French, German, Italian, Spanish and the GOG release.
        // Responsible method: heap in script 285
        // Fixes bug #7086
        static readonly ushort[] qfg3SignatureMissingPoints1 = {
            // local[$9c] = [0 -41 -76 1 -30 -77 -33 -34 -35 -36 -37 -42 -80 999]
            // local[$aa] = [0 0 0 0]
            Workarounds.SIG_UINT16_1(0x0000), Workarounds.SIG_UINT16_2(0x0000),                 //   0 START MARKER
            Workarounds.SIG_MAGICDWORD,
            Workarounds.SIG_UINT16_1(0xFFD7), Workarounds.SIG_UINT16_2(0xFFD7),                 // -41 "Greet"
            Workarounds.SIG_UINT16_1(0xFFB4), Workarounds.SIG_UINT16_2(0xFFB4),                 // -76 "Say Good-bye"
            Workarounds.SIG_UINT16_1(0x0001), Workarounds.SIG_UINT16_2(0x0001),                 //   1 "Tell about Tarna"
            Workarounds.SIG_UINT16_1(0xFFE2), Workarounds.SIG_UINT16_2(0xFFE2),                 // -30 "Tell about Simani"
            Workarounds.SIG_UINT16_1(0xFFB3), Workarounds.SIG_UINT16_2(0xFFB3),                 // -77 "Tell about Prisoner"
            Workarounds.SIG_UINT16_1(0xFFDF), Workarounds.SIG_UINT16_2(0xFFDF),                 // -33 "Dispelled Leopard Lady"
            Workarounds.SIG_UINT16_1(0xFFDE), Workarounds.SIG_UINT16_2(0xFFDE),                 // -34 "Tell about Leopard Lady"
            Workarounds.SIG_UINT16_1(0xFFDD), Workarounds.SIG_UINT16_2(0xFFDD),                 // -35 "Tell about Leopard Lady"
            Workarounds.SIG_UINT16_1(0xFFDC), Workarounds.SIG_UINT16_2(0xFFDC),                 // -36 "Tell about Leopard Lady"
            Workarounds.SIG_UINT16_1(0xFFDB), Workarounds.SIG_UINT16_2(0xFFDB),                 // -37 "Tell about Village"
            Workarounds.SIG_UINT16_1(0xFFD6), Workarounds.SIG_UINT16_2(0xFFD6),                 // -42 "Greet"
            Workarounds.SIG_UINT16_1(0xFFB0), Workarounds.SIG_UINT16_2(0xFFB0),                 // -80 "Say Good-bye"
            Workarounds.SIG_UINT16_1(0x03E7), Workarounds.SIG_UINT16_2(0x03E7),                 // 999 END MARKER
            Workarounds.SIG_ADDTOOFFSET(+2),                // local[$aa][0]
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchMissingPoints1 = {
            Workarounds.PATCH_ADDTOOFFSET(+14),
            Workarounds.PATCH_UINT16_1(0xFFE1), Workarounds.PATCH_UINT16_2(0xFFE1),               // -31 "Tell about Initiation"
            Workarounds.PATCH_UINT16_1(0xFFDE), Workarounds.PATCH_UINT16_2(0xFFDE),               // -34 "Tell about Leopard Lady"
            Workarounds.PATCH_UINT16_1(0xFFDD), Workarounds.PATCH_UINT16_2(0xFFDD),               // -35 "Tell about Leopard Lady"
            Workarounds.PATCH_UINT16_1(0xFFDC), Workarounds.PATCH_UINT16_2(0xFFDC),               // -36 "Tell about Leopard Lady"
            Workarounds.PATCH_UINT16_1(0xFFDB), Workarounds.PATCH_UINT16_2(0xFFDB),               // -37 "Tell about Village"
            Workarounds.PATCH_UINT16_1(0xFFD6), Workarounds.PATCH_UINT16_2(0xFFD6),               // -42 "Greet"
            Workarounds.PATCH_UINT16_1(0xFFB0), Workarounds.PATCH_UINT16_2(0xFFB0),               // -80 "Say Good-bye"
            Workarounds.PATCH_UINT16_1(0x03E7), Workarounds.PATCH_UINT16_2(0x03E7),               // 999 END MARKER
            Workarounds.PATCH_GETORIGINALBYTE(+28),         // local[$aa][0].low
            Workarounds.PATCH_GETORIGINALBYTE(+29),         // local[$aa][0].high
            Workarounds.PATCH_END
        };

        static readonly ushort[] qfg3SignatureMissingPoints2a = {
            Workarounds.SIG_MAGICDWORD,
            0x35, 0x00,                         // ldi 0
            0xb3, 0xaa,                         // sali local[$aa]
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3SignatureMissingPoints2b = {
            Workarounds.SIG_MAGICDWORD,
            0x36,                               // push
            0x5b, 0x02, 0xaa,                   // lea local[$aa]
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchMissingPoints2 = {
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0xab,                               // local[$aa] ==> local[$ab]
            Workarounds.PATCH_END
        };


        // Partly WORKAROUND:
        // During combat, the game is not properly throttled. That's because the game uses
        // an inner loop for combat and does not iterate through the main loop.
        // It also doesn't call kGameIsRestarting. This may get fixed properly at some point
        // by rewriting the speed throttler.
        //
        // Additionally Sierra set the cycle speed of the hero to 0. Which explains
        // why the actions of the hero are so incredibly fast. This issue also happened
        // in the original interpreter, when the computer was too powerful.
        //
        // Applies to at least: English, French, German, Italian, Spanish PC floppy
        // Responsible method: combatControls::dispatchEvent (script 550) + WarriorObj in heap
        // Fixes bug #6247
        static readonly ushort[] qfg3SignatureCombatSpeedThrottling1 = {
            0x31, 0x0d,                         // bnt [skip code]
            Workarounds.SIG_MAGICDWORD,
            0x89, 0xd2,                         // lsg global[D2h]
            0x35, 0x00,                         // ldi 0
            0x1e,                               // gt?
            0x31, 0x06,                         // bnt [skip code]
            0xe1, 0xd2,                         // -ag global[D2h] (jump skips over this)
            0x81, 0x58,                         // lag global[58h]
            0xa3, 0x01,                         // sal local[01]
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchCombatSpeedThrottling1 = {
            0x80, 0xd2,                         // lsg global[D2h]
            0x14,                               // or
            0x31, 0x06,                         // bnt [skip code] - saves 4 bytes
            0xe1, 0xd2,                         // -ag global[D2h]
            0x81, 0x58,                         // lag global[58h]
            0xa3, 0x01,                         // sal local[01] (jump skips over this)
            // our code
            0x76,                               // push0
            0x43, 0x2c, 0x00,                   // callk GameIsRestarting <-- add this so that our speed throttler is triggered
            Workarounds.PATCH_END
        };

        static readonly ushort[] qfg3SignatureCombatSpeedThrottling2 = {
            Workarounds.SIG_MAGICDWORD,
            Workarounds.SIG_UINT16_1(12), Workarounds.SIG_UINT16_2(12),                     // priority 12
            Workarounds.SIG_UINT16_1(0), Workarounds.SIG_UINT16_2(0),                      // underbits 0
            Workarounds.SIG_UINT16_1(0x4010), Workarounds.SIG_UINT16_2(0x4010),                 // signal 4010h
            Workarounds.SIG_ADDTOOFFSET(+18),
            Workarounds.SIG_UINT16_1(0), Workarounds.SIG_UINT16_2(0),                      // scaleSignal 0
            Workarounds.SIG_UINT16_1(128),Workarounds.SIG_UINT16_2(128),                    // scaleX
            Workarounds.SIG_UINT16_1(128), Workarounds.SIG_UINT16_2(128),                    // scaleY
            Workarounds.SIG_UINT16_1(128), Workarounds.SIG_UINT16_2(128),                    // maxScale
            Workarounds.SIG_UINT16_1(0), Workarounds.SIG_UINT16_2(0),                     // cycleSpeed
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchCombatSpeedThrottling2 = {
            Workarounds.PATCH_ADDTOOFFSET(+32),
            Workarounds.PATCH_UINT16_1(5), Workarounds.PATCH_UINT16_2(5),                    // set cycleSpeed to 5
            Workarounds.PATCH_END
        };

        // In room #750, when the hero enters from the top east path (room #755), it
        // could go out of the contained-access polygon bounds, and be able to travel
        // freely in the room.
        // The reason is that the cutoff y value (42) that determines whether the hero
        // enters from the top or bottom path is inaccurate: it's possible to enter the
        // top path from as low as y=45.
        // This patch changes the cutoff to be 50 which should be low enough.
        // It also changes the position in which the hero enters from the top east path
        // as the current location is hidden behind the tree.
        //
        // Applies to: English, French, German, Italian, Spanish and the GOG release.
        // Responsible method: enterEast::changeState (script 750)
        // Fixes bug #6693
        static readonly ushort[] qfg3SignatureRoom750Bounds1 = {
            // (if (< (ego y?) 42)
            0x76,                               // push0 ("y")
            0x76,                               // push0
            0x81, 0x00,                         // lag global[0] (ego)
            0x4a, 0x04,                         // send 4
            Workarounds.SIG_MAGICDWORD,
            0x36,                               // push
            0x35,   42,                         // ldi 42 <-- comparing ego.y with 42
            0x22,                               // lt?
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchRoom750Bounds1 = {
            // (if (< (ego y?) 50)
            Workarounds.PATCH_ADDTOOFFSET(+8),
            50,                                 // 42 --> 50
            Workarounds.PATCH_END
        };

        static readonly ushort[] qfg3SignatureRoom750Bounds2 = {
            // (ego x: 294 y: 39)
            0x78,                               // push1 ("x")
            0x78,                               // push1 
            0x38, Workarounds.SIG_UINT16_1(294), Workarounds.SIG_UINT16_2(294),              // pushi 294
            0x76,                               // push0 ("y")
            0x78,                               // push1
            Workarounds.SIG_MAGICDWORD,
            0x39,   29,                         // pushi 29
            0x81, 0x00,                         // lag global[0] (ego)
            0x4a, 0x0c,                         // send 12
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchRoom750Bounds2 = {
            // (ego x: 320 y: 39)
            Workarounds.PATCH_ADDTOOFFSET(+3),
            Workarounds.PATCH_UINT16_1(320), Workarounds.PATCH_UINT16_2(320),                  // 294 --> 320
            Workarounds.PATCH_ADDTOOFFSET(+3),
            39,                                 //  29 -->  39
            Workarounds.PATCH_END
        };

        static readonly ushort[] qfg3SignatureRoom750Bounds3 = {
            // (ego setMotion: MoveTo 282 29 self)
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_setMotion),    // pushi "setMotion" 0x133 in QfG3
            0x39, 0x04,                         // pushi 4
            0x51, Workarounds.SIG_ADDTOOFFSET(+1),          // class MoveTo
            0x36,                               // push
            0x38, Workarounds.SIG_UINT16_1(282), Workarounds.SIG_UINT16_2(282),              // pushi 282
            Workarounds.SIG_MAGICDWORD,
            0x39,   29,                         // pushi 29
            0x7c,                               // pushSelf
            0x81, 0x00,                         // lag global[0] (ego)
            0x4a, 0x0c,                         // send 12
            Workarounds.SIG_END
        };

        static readonly ushort[] qfg3PatchRoom750Bounds3 = {
            // (ego setMotion: MoveTo 309 35 self)
            Workarounds.PATCH_ADDTOOFFSET(+9),
            Workarounds.PATCH_UINT16_1(309), Workarounds.PATCH_UINT16_2(309),                 // 282 --> 309
            Workarounds.PATCH_ADDTOOFFSET(+1),
            35,                                 //  29 -->  35
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                    patch
        static readonly SciScriptPatcherEntry[] qfg3Signatures = {
            new SciScriptPatcherEntry(  true,   944, "import dialog continuous calls",                     1, qfg3SignatureImportDialog,           qfg3PatchImportDialog ),
            new SciScriptPatcherEntry(  true,   440, "dialog crash when asking about Woo",                 1, qfg3SignatureWooDialog,              qfg3PatchWooDialog ),
            new SciScriptPatcherEntry(  true,   440, "dialog crash when asking about Woo",                 1, qfg3SignatureWooDialogAlt,           qfg3PatchWooDialogAlt ),
            new SciScriptPatcherEntry(  true,    52, "export character save bug",                          2, qfg3SignatureExportChar,             qfg3PatchExportChar ),
            new SciScriptPatcherEntry(  true,    54, "import character from QfG1 bug",                     1, qfg3SignatureImportQfG1Char,         qfg3PatchImportQfG1Char ),
            new SciScriptPatcherEntry(  true,   640, "chief in hut priority fix",                          1, qfg3SignatureChiefPriority,          qfg3PatchChiefPriority ),
            new SciScriptPatcherEntry(  true,   285, "missing points for telling about initiation heap",   1, qfg3SignatureMissingPoints1,         qfg3PatchMissingPoints1 ),
            new SciScriptPatcherEntry(  true,   285, "missing points for telling about initiation script", 1, qfg3SignatureMissingPoints2a,        qfg3PatchMissingPoints2 ),
            new SciScriptPatcherEntry(  true,   285, "missing points for telling about initiation script", 1, qfg3SignatureMissingPoints2b,        qfg3PatchMissingPoints2 ),
            new SciScriptPatcherEntry(  true,   550, "combat speed throttling script",                     1, qfg3SignatureCombatSpeedThrottling1, qfg3PatchCombatSpeedThrottling1 ),
            new SciScriptPatcherEntry(  true,   550, "combat speed throttling heap",                       1, qfg3SignatureCombatSpeedThrottling2, qfg3PatchCombatSpeedThrottling2 ),
            new SciScriptPatcherEntry(  true,   750, "hero goes out of bounds in room 750",                2, qfg3SignatureRoom750Bounds1,         qfg3PatchRoom750Bounds1 ),
            new SciScriptPatcherEntry(  true,   750, "hero goes out of bounds in room 750",                2, qfg3SignatureRoom750Bounds2,         qfg3PatchRoom750Bounds2 ),
            new SciScriptPatcherEntry(  true,   750, "hero goes out of bounds in room 750",                2, qfg3SignatureRoom750Bounds3,         qfg3PatchRoom750Bounds3 ),
        };

        // ===========================================================================
        // When you leave Ulence Flats, another timepod is supposed to appear.
        // On fast machines, that timepod appears fully immediately and then
        //  starts to appear like it should be. That first appearance is caused
        //  by the scripts setting an invalid cel number and the machine being
        //  so fast that there is no time for another script to actually fix
        //  the cel number. On slower machines, the cel number gets fixed
        //  by the cycler and that's why only fast machines are affected.
        //  The same issue happens in Sierra SCI.
        // We simply set the correct starting cel number to fix the bug.
        // Responsible method: robotIntoShip::changeState(9)
        static readonly ushort[] sq1vgaSignatureUlenceFlatsTimepodGfxGlitch = {
            0x39,
            Workarounds.SIG_MAGICDWORD, Workarounds.SIG_SELECTOR8(ScriptPatcherSelectors.SELECTOR_cel), // pushi "cel"
            0x78,                               // push1
            0x39, 0x0a,                         // pushi 0x0a (set ship::cel to 10)
            0x38, Workarounds.SIG_UINT16_1(0x00a0), Workarounds.SIG_UINT16_2(0x00a0),           // pushi 0x00a0 (ship::setLoop)
            Workarounds.SIG_END
        };

        static readonly ushort[] sq1vgaPatchUlenceFlatsTimepodGfxGlitch = {
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0x39, 0x09,                         // pushi 0x09 (set ship::cel to 9)
            Workarounds.PATCH_END
        };

        // In Ulence Flats, there is a space ship, that you will use at some point.
        //  Near that space ship are 2 force field generators.
        //  When you look at the top of those generators, the game will crash.
        //  This happens also in Sierra SCI. It's caused by a jump, that goes out of bounds.
        //  We currently do not know if this was caused by a compiler glitch or if it was a developer error.
        //  Anyway we patch this glitchy code, so that the game won't crash anymore.
        //
        // Applies to at least: English Floppy
        // Responsible method: radar1::doVerb
        // Fixes bug: #6816
        static readonly ushort[] sq1vgaSignatureUlenceFlatsGeneratorGlitch = {
            Workarounds.SIG_MAGICDWORD, 0x1a,               // eq?
            0x30, Workarounds.SIG_UINT16_1(0xcdf4), Workarounds.SIG_UINT16_2(0xcdf4),           // bnt absolute 0xf000
            Workarounds.SIG_END
        };

        static readonly ushort[] sq1vgaPatchUlenceFlatsGeneratorGlitch = {
            Workarounds.PATCH_ADDTOOFFSET(+1),
            0x32, Workarounds.PATCH_UINT16_1(0x0000), Workarounds.PATCH_UINT16_2(0x0000),         // jmp 0x0000 (waste bytes)
            Workarounds.PATCH_END
        };

        // No documentation for this patch (TODO)
        static readonly ushort[] sq1vgaSignatureEgoShowsCard = {
            Workarounds.SIG_MAGICDWORD,
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_timesShownID), // push "timesShownID"
            0x78,                               // push1
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_timesShownID), // push "timesShownID"
            0x76,                               // push0
            0x51, 0x7c,                         // class DeltaurRegion
            0x4a, 0x04,                         // send 0x04 (get timesShownID)
            0x36,                               // push
            0x35, 0x01,                         // ldi 1
            0x02,                               // add
            0x36,                               // push
            0x51, 0x7c,                         // class DeltaurRegion
            0x4a, 0x06,                         // send 0x06 (set timesShownID)
            0x36,                               // push      (wrong, acc clobbered by class, above)
            0x35, 0x03,                         // ldi 0x03
            0x22,                               // lt?
            Workarounds.SIG_END
        };

        // Note that this script patch is merely a reordering of the
        // instructions in the original script.
        static readonly ushort[] sq1vgaPatchEgoShowsCard = {
            0x38, Workarounds.PATCH_SELECTOR16(ScriptPatcherSelectors.SELECTOR_timesShownID), // push "timesShownID"
            0x76,                               // push0
            0x51, 0x7c,                         // class DeltaurRegion
            0x4a, 0x04,                         // send 0x04 (get timesShownID)
            0x36,                               // push
            0x35, 0x01,                         // ldi 1
            0x02,                               // add
            0x36,                               // push (this push corresponds to the wrong one above)
            0x38, Workarounds.PATCH_SELECTOR16(ScriptPatcherSelectors.SELECTOR_timesShownID), // push "timesShownID"
            0x78,                               // push1
            0x36,                               // push
            0x51, 0x7c,                         // class DeltaurRegion
            0x4a, 0x06,                         // send 0x06 (set timesShownID)
            0x35, 0x03,                         // ldi 0x03
            0x22,                               // lt?
            Workarounds.PATCH_END
        };

        // The spider droid on planet Korona has a fixed movement speed,
        //  which is way faster than the default movement speed of ego.
        // This means that the player would have to turn up movement speed,
        //  otherwise it will be impossible to escape it.
        // We fix this issue by making the droid move a bit slower than ego
        //  does (relative to movement speed setting).
        //
        // Applies to at least: English PC floppy
        // Responsible method: spider::doit
        static readonly ushort[] sq1vgaSignatureSpiderDroidTiming = {
            Workarounds.SIG_MAGICDWORD,
            0x63, 0x4e,                         // pToa script
            0x30, Workarounds.SIG_UINT16_1(0x0005), Workarounds.SIG_UINT16_2(0x0005),           // bnt [further method code]
            0x35, 0x00,                         // ldi 00
            0x32, Workarounds.SIG_UINT16_1(0x0062), Workarounds.SIG_UINT16_2(0x0062),           // jmp [super-call]
            0x38, Workarounds.SIG_UINT16_1(0x0088), Workarounds.SIG_UINT16_2(0x0088),           // pushi 0088h (script)
            0x76,                               // push0
            0x81, 0x02,                         // lag global[2] (current room)
            0x4a, 0x04,                         // send 04 (get [current room].script)
            0x30, Workarounds.SIG_UINT16_1(0x0005), Workarounds.SIG_UINT16_2(0x0005),           // bnt [further method code]
            0x35, 0x00,                         // ldi 00
            0x32, Workarounds.SIG_UINT16_1(0x0052), Workarounds.SIG_UINT16_2(0x0052),           // jmp [super-call]
            0x89, 0xa6,                         // lsg global[a6] <-- flag gets set to 1 when ego went up the skeleton tail, when going down it's set to 2
            0x35, 0x01,                         // ldi 01
            0x1a,                               // eq?
            0x30, Workarounds.SIG_UINT16_1(0x0012), Workarounds.SIG_UINT16_2(0x0012),           // bnt [PChase set code], in case global A6 <> 1
            0x81, 0xb5,                         // lag global[b5]
            0x30, Workarounds.SIG_UINT16_1(0x000d), Workarounds.SIG_UINT16_2(0x000d),           // bnt [PChase set code], in case global B5 == 0
            0x38, Workarounds.SIG_UINT16_1(0x008c), Workarounds.SIG_UINT16_2(0x008c),           // pushi 008c
            0x78,                               // push1
            0x72, Workarounds.SIG_UINT16_1(0x1cb6), Workarounds.SIG_UINT16_2(0x1cb6),           // lofsa 1CB6 (moveToPath)
            0x36,                               // push
            0x54, 0x06,                         // self 06
            0x32, Workarounds.SIG_UINT16_1(0x0038), Workarounds.SIG_UINT16_2(0x0038),          // jmp [super-call]
            // PChase set call
            0x81, 0xb5,                         // lag global[B5]
            0x18,                               // not
            0x30, Workarounds.SIG_UINT16_1(0x0032), Workarounds.SIG_UINT16_2(0x0032),           // bnt [super-call], in case global B5 <> 0
            // followed by:
            // is spider in current room
            // is global A6h == 2? -> set PChase
            Workarounds.SIG_END
        }; // 58 bytes)

        // Global A6h <> 1 (did NOT went up the skeleton)
        //  Global B5h = 0 -> set PChase
        //  Global B5h <> 0 -> do not do anything
        // Global A6h = 1 (did went up the skeleton)
        //  Global B5h = 0 -> set PChase
        //  Global B5h <> 0 -> set moveToPath

        static readonly ushort[] sq1vgaPatchSpiderDroidTiming = {
            0x63, 0x4e,                         // pToa script
            0x2f, 0x68,                         // bt [super-call]
            0x38, Workarounds.PATCH_UINT16_1(0x0088), Workarounds.PATCH_UINT16_2(0x0088),         // pushi 0088 (script)
            0x76,                               // push0
            0x81, 0x02,                         // lag global[2] (current room)
            0x4a, 0x04,                         // send 04
            0x2f, 0x5e,                         // bt [super-call]
            // --> 12 bytes saved
            // new code
            0x38, Workarounds.PATCH_UINT16_1(0x0176), Workarounds.PATCH_UINT16_2(0x0176),         // pushi 0176 (egoMoveSpeed)
            0x76,                               // push0
            0x81, 0x01,                         // lag global[1]
            0x4a, 0x04,                         // send 04 - sq1::egoMoveSpeed
            0x36,                               // push
            0x36,                               // push
            0x35, 0x03,                         // ldi 03
            0x0c,                               // shr
            0x02,                               // add --> egoMoveSpeed + (egoMoveSpeed >> 3)
            0x39, 0x01,                         // push 01 (waste 1 byte)
            0x02,                               // add --> egoMoveSpeed++
            0x65, 0x4c,                         // aTop cycleSpeed
            0x65, 0x5e,                         // aTop moveSpeed
            // new code end
            0x81, 0xb5,                         // lag global[B5]
            0x31, 0x13,                         // bnt [PChase code chunk]
            0x89, 0xa6,                         // lsg global[A6]
            0x35, 0x01,                         // ldi 01
            0x1a,                               // eq?
            0x31, 0x3e,                         // bnt [super-call]
            0x38, Workarounds.PATCH_UINT16_1(0x008c), Workarounds.PATCH_UINT16_2(0x008c),         // pushi 008c
            0x78,                               // push1
            0x72, Workarounds.PATCH_UINT16_1(0x1cb6), Workarounds.PATCH_UINT16_2(0x1cb6),         // lofsa moveToPath
            0x36,                               // push
            0x54, 0x06,                         // self 06 - spider::setScript(movePath)
            0x33, 0x32,                         // jmp [super-call]
            // --> 9 bytes saved
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                                   patch
        static readonly SciScriptPatcherEntry[] sq1vgaSignatures = {
            new SciScriptPatcherEntry(  true,    45, "Ulence Flats: timepod graphic glitch",        1, sq1vgaSignatureUlenceFlatsTimepodGfxGlitch, sq1vgaPatchUlenceFlatsTimepodGfxGlitch ),
            new SciScriptPatcherEntry(  true,    45, "Ulence Flats: force field generator glitch",  1, sq1vgaSignatureUlenceFlatsGeneratorGlitch,  sq1vgaPatchUlenceFlatsGeneratorGlitch ),
            new SciScriptPatcherEntry(  true,    58, "Sarien armory droid zapping ego first time",  1, sq1vgaSignatureEgoShowsCard,                sq1vgaPatchEgoShowsCard ),
            new SciScriptPatcherEntry(  true,   704, "spider droid timing issue",                   1, sq1vgaSignatureSpiderDroidTiming,           sq1vgaPatchSpiderDroidTiming ),
        };

        // ===========================================================================
        //  script 298 of sq4/floppy has an issue. object "nest" uses another property
        //   which isn't included in property count. We return 0 in that case, the game
        //   adds it to nest::x. The problem is that the script also checks if x exceeds
        //   we never reach that of course, so the pterodactyl-flight will go endlessly
        //   we could either calculate property count differently somehow fixing this
        //   but I think just patching it out is cleaner.
        // Fixes bug: #5093
        static readonly ushort[] sq4FloppySignatureEndlessFlight = {
            0x39, 0x04,                         // pushi 04 (selector x)
            Workarounds.SIG_MAGICDWORD,
            0x78,                               // push1
            0x67, 0x08,                         // pTos 08 (property x)
            0x63, Workarounds.SIG_ADDTOOFFSET(+1),          // pToa (invalid property) - 44h for English floppy, 4ch for German floppy
            0x02,                               // add
            Workarounds.SIG_END
        };

        static readonly ushort[] sq4FloppyPatchEndlessFlight = {
            Workarounds.PATCH_ADDTOOFFSET(+5),
            0x35, 0x03,                         // ldi 03 (which would be the content of the property)
            Workarounds.PATCH_END
        };

        // Floppy-only: When the player tries to throw something at the sequel police in Space Quest X (zero g zone),
        //   the game will first show a textbox and then cause a signature mismatch in ScummVM/
        //   crash the whole game in Sierra SCI/display garbage (the latter when the Sierra "patch" got applied).
        //
        // All of this is caused by a typo in the script. Right after the code for showing the textbox,
        //  there is more similar code for showing another textbox, but without a pointer to the text.
        //  This has to be a typo, because there is no unused text to be found within that script.
        //
        // Sierra's "patch" didn't include a proper fix (as in a modified script). Instead they shipped a dummy
        //  text resource, which somewhat "solved" the issue in Sierra SCI, but it still showed another textbox
        //  with garbage in it. Funnily Sierra must have known that, because that new text resource contains:
        //  "Hi! This is a kludge!"
        //
        // We properly fix it by removing the faulty code.
        // Applies to at least: English Floppy
        // Responsible method: sp1::doVerb
        // Fixes bug: found by SCI developer
        static readonly ushort[] sq4FloppySignatureThrowStuffAtSequelPoliceBug = {
            0x47, 0xff, 0x00, 0x02,             // call export 255_0, 2
            0x3a,                               // toss
            Workarounds.SIG_MAGICDWORD,
            0x36,                               // push
            0x47, 0xff, 0x00, 0x02,             // call export 255_0, 2
            Workarounds.SIG_END
        };

        static readonly ushort[] sq4FloppyPatchThrowStuffAtSequelPoliceBug = {
            Workarounds.PATCH_ADDTOOFFSET(+5),
            0x48,                            // ret
            Workarounds.PATCH_END
        };

        // Right at the start of Space Quest 4 CD, when walking up in the first room, ego will
        //  immediately walk down just after entering the upper room.
        //
        // This is caused by the scripts setting ego's vertical coordinate to 189 (BDh), which is the
        //  trigger in rooms to walk to the room below it. Sometimes this isn't triggered, because
        //  the scripts also initiate a motion to vertical coordinate 188 (BCh). When you lower the game's speed,
        //  this bug normally always triggers. And it triggers of course also in the original interpreter.
        //
        // It doesn't happen in PC floppy, because nsRect is not the same as in CD.
        //
        // We fix it by setting ego's vertical coordinate to 188 and we also initiate a motion to 187.
        //
        // Applies to at least: English PC CD
        // Responsible method: rm045::doit
        // Fixes bug: #5468
        static readonly ushort[] sq4CdSignatureWalkInFromBelowRoom45 = {
            0x76,                               // push0
            Workarounds.SIG_MAGICDWORD,
            0x78,                               // push1
            0x38, Workarounds.SIG_UINT16_1(0x00bd), Workarounds.SIG_UINT16_2(0x00bd),           // pushi 00BDh
            0x38, Workarounds.SIG_ADDTOOFFSET(+2),          // pushi [setMotion selector]
            0x39, 0x03,                         // pushi 3
            0x51, Workarounds.SIG_ADDTOOFFSET(+1),          // class [MoveTo]
            0x36,                               // push
            0x78,                               // push1
            0x76,                               // push0
            0x81, 0x00,                         // lag global[0]
            0x4a, 0x04,                         // send 04 -> get ego::x
            0x36,                               // push
            0x38, Workarounds.SIG_UINT16_1(0x00bc), Workarounds.SIG_UINT16_2(0x00bc),           // pushi 00BCh
            Workarounds.SIG_END
        };

        static readonly ushort[] sq4CdPatchWalkInFromBelowRoom45 = {
            Workarounds.PATCH_ADDTOOFFSET(+2),
            0x38, Workarounds.PATCH_UINT16_1(0x00bc), Workarounds.PATCH_UINT16_2(0x00bc),         // pushi 00BCh
            Workarounds.PATCH_ADDTOOFFSET(+15),
            0x38, Workarounds.PATCH_UINT16_1(0x00bb), Workarounds.PATCH_UINT16_2(0x00bb),         // pushi 00BBh
            Workarounds.PATCH_END
        };

        // It seems that Sierra forgot to set a script flag, when cleaning out the bank account
        // in Space Quest 4 CD. This was probably caused by the whole bank account interaction
        // getting a rewrite and polish in the CD version.
        //
        // Because of this bug, points for changing back clothes will not get awarded, which
        // makes it impossible to get a perfect point score in the CD version of the game.
        // The points are awarded by rm371::doit in script 371.
        //
        // We fix this. Bug also happened, when using the original interpreter.
        // Bug does not happen for PC floppy.
        //
        // Attention: Some Let's Plays on youtube show that points are in fact awarded. Which is true.
        //            But those Let's Plays were actually created by playing a hacked Space Quest 4 version
        //            (which is part Floppy, part CD version - we consider it to be effectively pirated)
        //            and not the actual CD version of Space Quest 4.
        //            It's easy to identify - talkie + store called "Radio Shack" -> is hacked version.
        //
        // Applies to at least: English PC CD
        // Responsible method: but2Script::changeState(2)
        // Fixes bug: #6866
        static readonly ushort[] sq4CdSignatureGetPointsForChangingBackClothes = {
            0x35, 0x02,                         // ldi 02
            Workarounds.SIG_MAGICDWORD,
            0x1a,                               // eq?
            0x30, Workarounds.SIG_UINT16_1(0x006a), Workarounds.SIG_UINT16_2(0x006a),           // bnt [state 3]
            0x76,
            Workarounds.SIG_ADDTOOFFSET(+46),               // jump over "withdraw funds" code
            0x33, 0x33,                         // jmp [end of state 2, set cycles code]
            Workarounds.SIG_ADDTOOFFSET(+51),               // jump over "clean bank account" code
            0x35, 0x02,                         // ldi 02
            0x65, 0x1a,                         // aTop cycles
            0x33, 0x0b,                         // jmp [toss/ret]
            0x3c,                               // dup
            0x35, 0x03,                         // ldi 03
            0x1a,                               // eq?
            0x31, 0x05,                         // bnt [toss/ret]
            Workarounds.SIG_END
        };

        static readonly ushort[] sq4CdPatchGetPointsForChangingBackClothes = {
            Workarounds.PATCH_ADDTOOFFSET(+3),
            0x30, Workarounds.PATCH_UINT16_1(0x0070), Workarounds.PATCH_UINT16_2(0x0070),         // bnt [state 3]
            Workarounds.PATCH_ADDTOOFFSET(+47),             // "withdraw funds" code
            0x33, 0x39,                         // jmp [end of state 2, set cycles code]
            Workarounds.PATCH_ADDTOOFFSET(+51),
            0x78,                               // push1
            0x39, 0x1d,                         // ldi 1Dh
            0x45, 0x07, 0x02,                   // call export 7 of script 0 (set flag) -> effectively sets global 73h, bit 2
            0x35, 0x02,                         // ldi 02
            0x65, 0x1c,                         // aTop cycles
            0x33, 0x05,                         // jmp [toss/ret]
            // check for state 3 code removed to save 6 bytes
            Workarounds.PATCH_END
        };


        // For Space Quest 4 CD, Sierra added a pick up animation for Roger, when he picks up the rope.
        //
        // When the player is detected by the zombie right at the start of the game, while picking up the rope,
        // scripts bomb out. This also happens, when using the original interpreter.
        //
        // This is caused by code, that's supposed to make Roger face the arriving drone.
        // We fix it, by checking if ego::cycler is actually set before calling that code.
        //
        // Applies to at least: English PC CD
        // Responsible method: droidShoots::changeState(3)
        // Fixes bug: #6076
        static readonly ushort[] sq4CdSignatureGettingShotWhileGettingRope = {
            0x35, 0x02,                         // ldi 02
            0x65, 0x1a,                         // aTop cycles
            0x32, Workarounds.SIG_UINT16_1(0x02fa), Workarounds.SIG_UINT16_2(0x02fa),           // jmp [end]
            Workarounds.SIG_MAGICDWORD,
            0x3c,                               // dup
            0x35, 0x02,                         // ldi 02
            0x1a,                               // eq?
            0x31, 0x0b,                         // bnt [state 3 check]
            0x76,                               // push0
            0x45, 0x02, 0x00,                   // call export 2 of script 0 -> disable controls
            0x35, 0x02,                         // ldi 02
            0x65, 0x1a,                         // aTop cycles
            0x32, Workarounds.SIG_UINT16_1(0x02e9), Workarounds.SIG_UINT16_2(0x02e9),           // jmp [end]
            0x3c,                               // dup
            0x35, 0x03,                         // ldi 03
            0x1a,                               // eq?
            0x31, 0x1e,                         // bnt [state 4 check]
            0x76,                               // push0
            0x45, 0x02, 0x00,                   // call export 2 of script 0 -> disable controls again??
            0x7a,                               // push2
            0x89, 0x00,                         // lsg global[0]
            0x72, Workarounds.SIG_UINT16_1(0x0242), Workarounds.SIG_UINT16_2(0x0242),           // lofsa deathDroid
            0x36,                               // push
            0x45, 0x0d, 0x04,                   // call export 13 of script 0 -> set heading of ego to face droid
            Workarounds.SIG_END
        };

        static readonly ushort[] sq4CdPatchGettingShotWhileGettingRope = {
            Workarounds.PATCH_ADDTOOFFSET(+11),
            // this makes state 2 only do the 2 cycles wait, controls should always be disabled already at this point
            0x2f, 0xf3,                         // bt [previous state aTop cycles code]
            // Now we check for state 3, this change saves us 11 bytes
            0x3c,                               // dup
            0x35, 0x03,                         // ldi 03
            0x1a,                               // eq?
            0x31, 0x29,                         // bnt [state 4 check]
            // new state 3 code
            0x76,                               // push0
            0x45, 0x02, 0x00,                   // call export 2 of script 0 (disable controls, actually not needed)
            0x38, Workarounds.PATCH_SELECTOR16(ScriptPatcherSelectors.SELECTOR_cycler),     // pushi cycler
            0x76,                               // push0
            0x81, 0x00,                         // lag global[0]
            0x4a, 0x04,                         // send 04 (get ego::cycler)
            0x30, Workarounds.PATCH_UINT16_1(10), Workarounds.PATCH_UINT16_2(10),             // bnt [jump over heading call]
            Workarounds.PATCH_END
        };

        // The scripts in SQ4CD support simultaneous playing of speech and subtitles,
        // but this was not available as an option. The following two patches enable
        // this functionality in the game's GUI options dialog.
        // Patch 1: iconTextSwitch::show, called when the text options button is shown.
        // This is patched to add the "Both" text resource (i.e. we end up with
        // "Speech", "Text" and "Both")
        static readonly ushort[] sq4CdSignatureTextOptionsButton = {
            Workarounds.SIG_MAGICDWORD,
            0x35, 0x01,                         // ldi 0x01
            0xa1, 0x53,                         // sag 0x53
            0x39, 0x03,                         // pushi 0x03
            0x78,                               // push1
            0x39, 0x09,                         // pushi 0x09
            0x54, 0x06,                         // self 0x06
            Workarounds.SIG_END
        };

        static readonly ushort[] sq4CdPatchTextOptionsButton = {
            Workarounds.PATCH_ADDTOOFFSET(+7),
            0x39, 0x0b,                         // pushi 0x0b
            Workarounds.PATCH_END
        };

        // Patch 2: Adjust a check in babbleIcon::init, which handles the babble icon
        // (e.g. the two guys from Andromeda) shown when dying/quitting.
        // Fixes bug: #6068
        static readonly ushort[] sq4CdSignatureBabbleIcon = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x5a,                         // lsg 5a
            0x35, 0x02,                         // ldi 02
            0x1a,                               // eq?
            0x31, 0x26,                         // bnt 26  [02a7]
            Workarounds.SIG_END
        };

        static readonly ushort[] sq4CdPatchBabbleIcon = {
            0x89, 0x5a,                         // lsg 5a
            0x35, 0x01,                         // ldi 01
            0x1a,                               // eq?
            0x2f, 0x26,                         // bt 26  [02a7]
            Workarounds.PATCH_END
        };

        // Patch 3: Add the ability to toggle among the three available options,
        // when the text options button is clicked: "Speech", "Text" and "Both".
        // Refer to the patch above for additional details.
        // iconTextSwitch::doit (called when the text options button is clicked)
        static readonly ushort[] sq4CdSignatureTextOptions = {
            Workarounds.SIG_MAGICDWORD,
            0x89, 0x5a,                         // lsg 0x5a (load global 90 to stack)
            0x3c,                               // dup
            0x35, 0x01,                         // ldi 0x01
            0x1a,                               // eq? (global 90 == 1)
            0x31, 0x06,                         // bnt 0x06 (0x0691)
            0x35, 0x02,                         // ldi 0x02
            0xa1, 0x5a,                         // sag 0x5a (save acc to global 90)
            0x33, 0x0a,                         // jmp 0x0a (0x69b)
            0x3c,                               // dup
            0x35, 0x02,                         // ldi 0x02
            0x1a,                               // eq? (global 90 == 2)
            0x31, 0x04,                         // bnt 0x04 (0x069b)
            0x35, 0x01,                         // ldi 0x01
            0xa1, 0x5a,                         // sag 0x5a (save acc to global 90)
            0x3a,                               // toss
            0x38, Workarounds.SIG_SELECTOR16(ScriptPatcherSelectors.SELECTOR_show),         // pushi 0x00d9
            0x76,                               // push0
            0x54, 0x04,                         // self 0x04
            0x48,                               // ret
            Workarounds.SIG_END
        };

        static readonly ushort[] sq4CdPatchTextOptions = {
            0x89, 0x5a,                         // lsg 0x5a (load global 90 to stack)
            0x3c,                               // dup
            0x35, 0x03,                         // ldi 0x03 (acc = 3)
            0x1a,                               // eq? (global 90 == 3)
            0x2f, 0x07,                         // bt 0x07
            0x89, 0x5a,                         // lsg 0x5a (load global 90 to stack again)
            0x35, 0x01,                         // ldi 0x01 (acc = 1)
            0x02,                               // add: acc = global 90 (on stack) + 1 (previous acc value)
            0x33, 0x02,                         // jmp 0x02
            0x35, 0x01,                         // ldi 0x01 (reset acc to 1)
            0xa1, 0x5a,                         // sag 0x5a (save acc to global 90)
            0x33, 0x03,                         // jmp 0x03 (jump over the wasted bytes below)
            0x34, Workarounds.PATCH_UINT16_1(0x0000), Workarounds.PATCH_UINT16_2(0x0000),         // ldi 0x0000 (waste 3 bytes)
            0x3a,                               // toss
            // (the rest of the code is the same)
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                                      patch
        static readonly SciScriptPatcherEntry[] sq4Signatures = {
            new SciScriptPatcherEntry(  true,   298, "Floppy: endless flight",                      1, sq4FloppySignatureEndlessFlight,               sq4FloppyPatchEndlessFlight ),
            new SciScriptPatcherEntry(  true,   700, "Floppy: throw stuff at sequel police bug",    1, sq4FloppySignatureThrowStuffAtSequelPoliceBug, sq4FloppyPatchThrowStuffAtSequelPoliceBug ),
            new SciScriptPatcherEntry(  true,    45, "CD: walk in from below for room 45 fix",      1, sq4CdSignatureWalkInFromBelowRoom45,           sq4CdPatchWalkInFromBelowRoom45 ),
            new SciScriptPatcherEntry(  true,   396, "CD: get points for changing back clothes fix",1, sq4CdSignatureGetPointsForChangingBackClothes, sq4CdPatchGetPointsForChangingBackClothes ),
            new SciScriptPatcherEntry(  true,   701, "CD: getting shot, while getting rope",        1, sq4CdSignatureGettingShotWhileGettingRope,     sq4CdPatchGettingShotWhileGettingRope ),
            new SciScriptPatcherEntry(  true,     0, "CD: Babble icon speech and subtitles fix",    1, sq4CdSignatureBabbleIcon,                      sq4CdPatchBabbleIcon ),
            new SciScriptPatcherEntry(  true,   818, "CD: Speech and subtitles option",             1, sq4CdSignatureTextOptions,                     sq4CdPatchTextOptions ),
            new SciScriptPatcherEntry(  true,   818, "CD: Speech and subtitles option button",      1, sq4CdSignatureTextOptionsButton,               sq4CdPatchTextOptionsButton ),
        };

        // ===========================================================================
        // The toolbox in sq5 is buggy. When you click on the upper part of the "put
        //  in inventory"-button (some items only - for example the hole puncher - at the
        //  upper left), points will get awarded correctly and the item will get put into
        //  the player's inventory, but you will then get a "not here" message and the
        //  item will also remain to be the current mouse cursor.
        // The bug report also says that items may get lost. I wasn't able to reproduce
        //  that part.
        // This is caused by the mouse-click event getting reprocessed (which wouldn't
        //  be a problem by itself) and during this reprocessing coordinates are not
        //  processed the same as during the first click (script 226 includes a local
        //  subroutine, which checks coordinates in a hardcoded way w/o port-adjustment).
        // Because of this, the hotspot for the button is lower than it should be, which
        //  then results in the game thinking that the user didn't click on the button
        //  and also results in the previously mentioned message.
        // This happened in Sierra SCI as well (of course).
        // We fix it by combining state 0 + 1 of takeTool::changeState and so stopping
        //  the event to get reprocessed. This was the only way possible, because everything
        //  else is done in SCI system scripts and I don't want to touch those.
        // Applies to at least: English/German/French PC floppy
        // Responsible method: takeTool::changeState
        // Fixes bug: #6457
        static readonly ushort[] sq5SignatureToolboxFix = {
            0x31, 0x13,                    // bnt [check for state 1]
            Workarounds.SIG_MAGICDWORD,
            0x38, Workarounds.SIG_UINT16_1(0x00aa), Workarounds.SIG_UINT16_2(0x00aa),      // pushi 00aa
            0x39, 0x05,                    // pushi 05
            0x39, 0x16,                    // pushi 16
            0x76,                          // push0
            0x39, 0x03,                    // pushi 03
            0x76,                          // push0
            0x7c,                          // pushSelf
            0x81, 0x5b,                    // lag 5b
            0x4a, 0x0e,                    // send 0e
            0x32, Workarounds.SIG_UINT16_1(0x0088), Workarounds.SIG_UINT16_2(0x0088),      // jmp [end-of-method]
            0x3c,                          // dup
            0x35, 0x01,                    // ldi 01
            0x1a,                          // eq?
            0x31, 0x28,                    // bnt [check for state 2]
            Workarounds.SIG_END
        };

        static readonly ushort[] sq5PatchToolboxFix = {
            0x31, 0x41,                    // bnt [check for state 2]
            Workarounds.PATCH_ADDTOOFFSET(+16),        // skip to jmp offset
            0x35, 0x01,                    // ldi 01
            0x65, 0x14,                    // aTop [state]
            0x36, 0x00, 0x00,              // ldi 0000 (waste 3 bytes)
            0x35, 0x00,                    // ldi 00 (waste 2 bytes)
            Workarounds.PATCH_END
        };

        //          script, description,                                      signature                        patch
        static readonly SciScriptPatcherEntry[] sq5Signatures = {
            new SciScriptPatcherEntry(  true,   226, "toolbox fix",                                 1, sq5SignatureToolboxFix,          sq5PatchToolboxFix ),
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
            SciGameId gameId = SciEngine.Instance.GameId;

            switch (gameId)
            {
                case SciGameId.CAMELOT:
                    signatureTable = camelotSignatures;
                    break;
                case SciGameId.ECOQUEST:
                    signatureTable = ecoquest1Signatures;
                    break;
                case SciGameId.ECOQUEST2:
                    signatureTable = ecoquest2Signatures;
                    break;
                case SciGameId.FANMADE:
                    signatureTable = fanmadeSignatures;
                    break;
                case SciGameId.FREDDYPHARKAS:
                    signatureTable = freddypharkasSignatures;
                    break;
                case SciGameId.GK1:
                    signatureTable = gk1Signatures;
                    break;
                case SciGameId.KQ5:
                    signatureTable = kq5Signatures;
                    break;
                case SciGameId.KQ6:
                    signatureTable = kq6Signatures;
                    break;
                case SciGameId.KQ7:
                    signatureTable = kq7Signatures;
                    break;
                case SciGameId.LAURABOW:
                    signatureTable = laurabow1Signatures;
                    break;
                case SciGameId.LAURABOW2:
                    signatureTable = laurabow2Signatures;
                    break;
                case SciGameId.LONGBOW:
                    signatureTable = longbowSignatures;
                    break;
                case SciGameId.LSL2:
                    signatureTable = larry2Signatures;
                    break;
                case SciGameId.LSL5:
                    signatureTable = larry5Signatures;
                    break;
                case SciGameId.LSL6:
                    signatureTable = larry6Signatures;
                    break;
                case SciGameId.MOTHERGOOSE256:
                    signatureTable = mothergoose256Signatures;
                    break;
                case SciGameId.PQ1:
                    signatureTable = pq1vgaSignatures;
                    break;
                case SciGameId.QFG1:
                    signatureTable = qfg1egaSignatures;
                    break;
                case SciGameId.QFG1VGA:
                    signatureTable = qfg1vgaSignatures;
                    break;
                case SciGameId.QFG2:
                    signatureTable = qfg2Signatures;
                    break;
                case SciGameId.QFG3:
                    signatureTable = qfg3Signatures;
                    break;
                case SciGameId.SQ1:
                    signatureTable = sq1vgaSignatures;
                    break;
                case SciGameId.SQ4:
                    signatureTable = sq4Signatures;
                    break;
                case SciGameId.SQ5:
                    signatureTable = sq5Signatures;
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
                foreach (var curEntry2 in signatureTable)
                {
                    var curRuntimeEntry = _runtimeTable[i];
                    if ((scriptNr == curEntry2.scriptNr) && (curRuntimeEntry.active))
                    {
                        int foundOffset = 0;
                        short applyCount = curEntry2.applyCount;
                        do
                        {
                            foundOffset = FindSignature(curEntry2, curRuntimeEntry, scriptData, (uint)scriptSize);
                            if (foundOffset != -1)
                            {
                                // found, so apply the patch
                                // TODO: debugC(kDebugLevelScriptPatcher, "Script-Patcher: '%s' on script %d offset %d", curEntry.description, scriptNr, foundOffset);
                                ApplyPatch(curEntry2, scriptData, (uint)scriptSize, foundOffset);
                            }
                            applyCount--;
                        } while ((foundOffset != -1) && (applyCount != 0));
                    }
                    i++;
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
            uint searchLimit = scriptSize - 3;
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
            ushort patchWord = patchEntry.patchData[0];
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
                patchWord = patchData[i];
            }
        }

        // This method calculates the magic DWORD for each entry in the signature table
        //  and it also initializes the selector table for selectors used in the signatures/patches of the current game
        private void InitSignature(SciScriptPatcherEntry[] patchTable)
        {
            int patchEntryCount = patchTable.Length;

            // Count entries and allocate runtime data
            _runtimeTable = new SciScriptPatcherRuntimeEntry[patchEntryCount];

            var i = 0;
            foreach (var curEntry in patchTable)
            {
                var curRuntimeEntry = _runtimeTable[i];
                // process signature
                curRuntimeEntry.active = curEntry.defaultActive;
                curRuntimeEntry.magicDWord = 0;
                curRuntimeEntry.magicOffset = 0;

                // We verify the signature data and remember the calculated magic DWord from the signature data
                CalculateMagicDWordAndVerify(curEntry.description, curEntry.signatureData, true, ref curRuntimeEntry.magicDWord, ref curRuntimeEntry.magicOffset);
                // We verify the patch data
                CalculateMagicDWordAndVerify(curEntry.description, curEntry.patchData, false, ref curRuntimeEntry.magicDWord, ref curRuntimeEntry.magicOffset);

                i++;
            }
        }

        // Attention: Magic DWord is returned using platform specific byte order. This is done on purpose for performance.
        private void CalculateMagicDWordAndVerify(string signatureDescription, ushort[] signatureData, bool magicDWordIncluded, ref uint calculatedMagicDWord, ref int calculatedMagicDWordOffset)
        {
            int curSelector = -1;
            int magicOffset;
            byte[] magicDWord = new byte[4];
            int magicDWordLeft = 0;
            ushort curWord;
            ushort curCommand;
            uint curValue;
            byte byte1 = 0;
            byte byte2 = 0;

            int s = 0;
            curWord = signatureData[0];
            magicOffset = 0;
            while (curWord != Workarounds.SIG_END)
            {
                curCommand = (ushort)(curWord & Workarounds.SIG_COMMANDMASK);
                curValue = (uint)(curWord & Workarounds.SIG_VALUEMASK);
                switch (curCommand)
                {
                    case Workarounds.SIG_MAGICDWORD:
                        {
                            if (magicDWordIncluded)
                            {
                                if ((calculatedMagicDWord != 0) || (magicDWordLeft != 0))
                                    Error($"Script-Patcher: Magic-DWORD specified multiple times in signature\nFaulty patch: '{signatureDescription}'");
                                magicDWordLeft = 4;
                                calculatedMagicDWordOffset = magicOffset;
                            }
                            else
                            {
                                Error($"Script-Patcher: Magic-DWORD sequence found in patch data\nFaulty patch: '{signatureDescription}'");
                            }
                            break;
                        }
                    case Workarounds.SIG_CODE_ADDTOOFFSET:
                        {
                            magicOffset = (int)(magicOffset - curValue);
                            if (magicDWordLeft != 0)
                                Error($"Script-Patcher: Magic-DWORD contains AddToOffset command\nFaulty patch: '{signatureDescription}'");
                            break;
                        }
                    case Workarounds.SIG_CODE_UINT16:
                    case Workarounds.SIG_CODE_SELECTOR16:
                        {
                            // UINT16 or 1
                            switch (curCommand)
                            {
                                case Workarounds.SIG_CODE_UINT16:
                                    {
                                        s++; curWord = signatureData[s];
                                        if ((curWord & Workarounds.SIG_COMMANDMASK) != 0)
                                            Error("Script-Patcher: signature entry inconsistent\nFaulty patch: '%s'", signatureDescription);
                                        if (!_isMacSci11)
                                        {
                                            byte1 = (byte)curValue;
                                            byte2 = (byte)(curWord & Workarounds.SIG_BYTEMASK);
                                        }
                                        else
                                        {
                                            byte1 = (byte)(curWord & Workarounds.SIG_BYTEMASK);
                                            byte2 = (byte)curValue;
                                        }
                                        break;
                                    }
                                case Workarounds.SIG_CODE_SELECTOR16:
                                    {
                                        curSelector = _selectorIdTable[curValue];
                                        if (curSelector == -1)
                                        {
                                            curSelector = SciEngine.Instance.Kernel.FindSelector(selectorNameTable[curValue]);
                                            _selectorIdTable[curValue] = curSelector;
                                        }
                                        if (!_isMacSci11)
                                        {
                                            byte1 = (byte)(curSelector & 0x00FF);
                                            byte2 = (byte)(curSelector >> 8);
                                        }
                                        else
                                        {
                                            byte1 = (byte)(curSelector >> 8);
                                            byte2 = (byte)(curSelector & 0x00FF);
                                        }
                                        break;
                                    }
                            }
                            magicOffset -= 2;
                            if (magicDWordLeft != 0)
                            {
                                // Remember current word for Magic DWORD
                                magicDWord[4 - magicDWordLeft] = byte1;
                                magicDWordLeft--;
                                if (magicDWordLeft != 0)
                                {
                                    magicDWord[4 - magicDWordLeft] = byte2;
                                    magicDWordLeft--;
                                }
                                if (magicDWordLeft == 0)
                                {
                                    // Magic DWORD is now known, convert to platform specific byte order
                                    calculatedMagicDWord = magicDWord.ToUInt32();
                                }
                            }
                            break;
                        }
                    case Workarounds.SIG_CODE_BYTE:
                    case Workarounds.SIG_CODE_SELECTOR8:
                        {
                            if (curCommand == Workarounds.SIG_CODE_SELECTOR8)
                            {
                                curSelector = _selectorIdTable[curValue];
                                if (curSelector == -1)
                                {
                                    curSelector = SciEngine.Instance.Kernel.FindSelector(selectorNameTable[curValue]);
                                    _selectorIdTable[curValue] = curSelector;
                                    if (curSelector != -1)
                                    {
                                        if ((curSelector & 0xFF00) != 0)
                                            Error($"Script-Patcher: 8 bit selector required, game uses 16 bit selector\nFaulty patch: '{signatureDescription}'");
                                    }
                                }
                                curValue = (uint)curSelector;
                            }
                            magicOffset--;
                            if (magicDWordLeft != 0)
                            {
                                // Remember current byte for Magic DWORD
                                magicDWord[4 - magicDWordLeft] = (byte)curValue;
                                magicDWordLeft--;
                                if (magicDWordLeft == 0)
                                {
                                    // Magic DWORD is now known, convert to platform specific byte order
                                    calculatedMagicDWord = magicDWord.ToUInt32();
                                }
                            }
                            break;
                        }
                    case Workarounds.PATCH_CODE_GETORIGINALBYTEADJUST:
                        {
                            s++; // skip over extra uint16
                            break;
                        }
                }
                s++;
                curWord = signatureData[s];
            }

            if (magicDWordLeft != 0)
                Error($"Script-Patcher: Magic-DWORD beyond End-Of-Signature\nFaulty patch: '{signatureDescription}'");
            if (magicDWordIncluded)
            {
                if (calculatedMagicDWord == 0)
                {
                    Error($"Script-Patcher: Magic-DWORD not specified in signature\nFaulty patch: '{signatureDescription}'");
                }
            }
        }
    }
}
