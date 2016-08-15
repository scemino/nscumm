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
                    throw new NotImplementedException();
                    //signatureTable = fanmadeSignatures;
                    break;
                case SciGameId.FREDDYPHARKAS:
                    signatureTable = freddypharkasSignatures;
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
                    signatureTable = laurabow1Signatures;
                    break;
                case SciGameId.LAURABOW2:
                    throw new NotImplementedException();
                    //signatureTable = laurabow2Signatures;
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
