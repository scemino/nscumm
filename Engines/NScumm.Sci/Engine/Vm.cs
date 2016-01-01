//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 3 of the License; or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful;
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not; see <http://www.gnu.org/licenses/>.


using NScumm.Core.Common;
using System;
using System.Linq;

namespace NScumm.Sci.Engine
{
    /// <summary>
    /// Types of selectors as returned by lookupSelector() below.
    /// </summary>
    enum SelectorType
    {
        None = 0,
        Variable,
        Method
    }

    // A reference to an object's variable.
    // The object is stored as a reg_t; the variable as an index into _variables
    internal class ObjVarRef
    {
        public Register obj;
        public int varindex;

        public Register GetPointer(SegManager segMan)
        {
            SciObject o = segMan.GetObject(obj);
            return o != null ? o.GetVariableRef(varindex) : null;
        }
    }

    enum ExecStackType
    {
        CALL = 0,
        KERNEL = 1,
        VARSELECTOR = 2
    }

    internal class StackPtr : IComparable<StackPtr>, IComparable
    {
        private int _index;
        private Register[] _entries;

        public Register this[int index]
        {
            get { return _entries[_index + index]; }
            set { _entries[_index + index] = value; }
        }

        public StackPtr(Register[] entries, int index)
        {
            _entries = entries;
            _index = index;
        }

        public static StackPtr operator ++(StackPtr ptr)
        {
            ptr._index++;
            return ptr;
        }

        public static StackPtr operator --(StackPtr ptr)
        {
            ptr._index--;
            return ptr;
        }

        public static StackPtr operator +(StackPtr ptr, int offset)
        {
            ptr._index += offset;
            return ptr;
        }

        public static StackPtr operator -(StackPtr ptr, int offset)
        {
            ptr._index -= offset;
            return ptr;
        }

        public static int operator -(StackPtr ptr1, StackPtr ptr2)
        {
            if (ptr1._entries != ptr2._entries)
                throw new InvalidOperationException("StackPtr subtraction must be between to pointers of the same stack.");

            return ptr1._index - ptr2._index;
        }

        public static bool operator <(StackPtr ptr1, StackPtr ptr2)
        {
            if (ptr1._entries != ptr2._entries)
                throw new InvalidOperationException("StackPtr subtraction must be between to pointers of the same stack.");

            return ptr1._index < ptr2._index;
        }

        public static bool operator <=(StackPtr ptr1, StackPtr ptr2)
        {
            if (ptr1._entries != ptr2._entries)
                throw new InvalidOperationException("StackPtr subtraction must be between to pointers of the same stack.");

            return ptr1._index <= ptr2._index;
        }

        public static bool operator >(StackPtr ptr1, StackPtr ptr2)
        {
            if (ptr1._entries != ptr2._entries)
                throw new InvalidOperationException("StackPtr subtraction must be between to pointers of the same stack.");

            return ptr1._index > ptr2._index;
        }

        public static bool operator >=(StackPtr ptr1, StackPtr ptr2)
        {
            if (ptr1._entries != ptr2._entries)
                throw new InvalidOperationException("StackPtr subtraction must be between to pointers of the same stack.");

            return ptr1._index >= ptr2._index;
        }

        int IComparable.CompareTo(object obj)
        {
            var other = obj as StackPtr;
            if (other == null) return -1;
            return CompareTo(other);
        }

        public int CompareTo(StackPtr other)
        {
            return _index.CompareTo(other._index);
        }
    }

    internal class ExecStack
    {
        /// <summary>
        /// Pointer to the beginning of the current object
        /// </summary>
        public Register objp;
        /// <summary>
        /// Pointer to the object containing the invoked method
        /// </summary>
        public Register sendp;

        //  union {
        public ObjVarRef varp; // Variable pointer for r/w access
        public Register32 pc;       // Pointer to the initial program counter. Not accurate for the TOS element
        //  }
        //  addr;

        public StackPtr fp; // Frame pointer
        public StackPtr sp; // Stack pointer

        public int argc;
        public StackPtr variables_argp; // Argument pointer

        public ushort local_segment; // local variables etc

        public int debugSelector;   // The selector which was used to call or -1 if not applicable
        public int debugExportId;        // The exportId which was called or -1 if not applicable
        public int debugLocalCallOffset; // Local call offset or -1 if not applicable
        public int debugOrigin;          // The stack frame position the call was made from, or -1 if it was the initial call
        public ExecStackType type;

        public ExecStack(Register objp_, Register sendp_, StackPtr sp_, int argc_, StackPtr argp_,
                ushort localsSegment_, Register32 pc_, int debugSelector_,
                int debugExportId_, int debugLocalCallOffset_, int debugOrigin_,
                ExecStackType type_)
        {
            objp = objp_;
            sendp = sendp_;
            // varp is set separately for varselector calls
            pc = pc_;
            fp = sp = sp_;
            argc = argc_;
            variables_argp = argp_;
            variables_argp[0] = Register.Make(0, (ushort)argc);  // The first argument is argc
            if (localsSegment_ != 0xFFFF)
                local_segment = localsSegment_;
            else
                local_segment = (ushort)pc_.Segment;
            debugSelector = debugSelector_;
            debugExportId = debugExportId_;
            debugLocalCallOffset = debugLocalCallOffset_;
            debugOrigin = debugOrigin_;
            type = type_;
        }

        public Register GetVarPointer(SegManager segMan)
        {
            return varp.GetPointer(segMan);
        }
    }

    internal static class Vm
    {
        /// <summary>
        /// Number of bytes to be allocated for the stack
        /// </summary>
        public const int STACK_SIZE = 0x1000;

        /// <summary>
        /// Stack pointer value: Use predecessor's value
        /// </summary>
        public const StackPtr CALL_SP_CARRY = null;


        /// <summary>
        /// Number of kernel calls in between gcs; should be &lt; 50000
        /// </summary>
        public const int GC_INTERVAL = 0x8000;

        public const int VAR_GLOBAL = 0;
        public const int VAR_LOCAL = 1;
        public const int VAR_TEMP = 2;
        public const int VAR_PARAM = 3;

        public const int op_bnot = 0x00; // 000
        public const int op_add = 0x01;  // 001
        public const int op_sub = 0x02;  // 002
        public const int op_mul = 0x03;  // 003
        public const int op_div = 0x04;  // 004
        public const int op_mod = 0x05;  // 005
        public const int op_shr = 0x06;  // 006
        public const int op_shl = 0x07;  // 007
        public const int op_xor = 0x08;  // 008
        public const int op_and = 0x09;  // 009
        public const int op_or = 0x0a;   // 010
        public const int op_neg = 0x0b;  // 011
        public const int op_not = 0x0c;  // 012
        public const int op_eq_ = 0x0d;  // 013
        public const int op_ne_ = 0x0e;  // 014
        public const int op_gt_ = 0x0f;  // 015
        public const int op_ge_ = 0x10;  // 016
        public const int op_lt_ = 0x11;  // 017
        public const int op_le_ = 0x12;  // 018
        public const int op_ugt_ = 0x13; // 019
        public const int op_uge_ = 0x14; // 020
        public const int op_ult_ = 0x15; // 021
        public const int op_ule_ = 0x16; // 022
        public const int op_bt = 0x17;   // 023
        public const int op_bnt = 0x18;  // 024
        public const int op_jmp = 0x19;  // 025
        public const int op_ldi = 0x1a;  // 026
        public const int op_push = 0x1b; // 027
        public const int op_pushi = 0x1c;    // 028
        public const int op_toss = 0x1d; // 029
        public const int op_dup = 0x1e;  // 030
        public const int op_link = 0x1f; // 031
        public const int op_call = 0x20; // 032
        public const int op_callk = 0x21;    // 033
        public const int op_callb = 0x22;    // 034
        public const int op_calle = 0x23;    // 035
        public const int op_ret = 0x24;  // 036
        public const int op_send = 0x25; // 037
                                         // dummy      0x26;	// 038
                                         // dummy      0x27;	// 039
        public const int op_class = 0x28;    // 040
                                             // dummy      0x29;	// 041
        public const int op_self = 0x2a; // 042
        public const int op_super = 0x2b;    // 043
        public const int op_rest = 0x2c; // 044
        public const int op_lea = 0x2d;  // 045
        public const int op_selfID = 0x2e;   // 046
                                             // dummy      0x2f	// 047
        public const int op_pprev = 0x30;    // 048
        public const int op_pToa = 0x31; // 049
        public const int op_aTop = 0x32; // 050
        public const int op_pTos = 0x33; // 051
        public const int op_sTop = 0x34; // 052
        public const int op_ipToa = 0x35;    // 053
        public const int op_dpToa = 0x36;    // 054
        public const int op_ipTos = 0x37;    // 055
        public const int op_dpTos = 0x38;    // 056
        public const int op_lofsa = 0x39;    // 057
        public const int op_lofss = 0x3a;    // 058
        public const int op_push0 = 0x3b;    // 059
        public const int op_push1 = 0x3c;    // 060
        public const int op_push2 = 0x3d;    // 061
        public const int op_pushSelf = 0x3e; // 062
        public const int op_line = 0x3f; // 063

        public const int op_lag = 0x40;  // 064
        public const int op_lal = 0x41;  // 065
        public const int op_lat = 0x42;  // 066
        public const int op_lap = 0x43;  // 067
        public const int op_lsg = 0x44;  // 068
        public const int op_lsl = 0x45;  // 069
        public const int op_lst = 0x46;  // 070
        public const int op_lsp = 0x47;  // 071
        public const int op_lagi = 0x48; // 072
        public const int op_lali = 0x49; // 073
        public const int op_lati = 0x4a; // 074
        public const int op_lapi = 0x4b; // 075
        public const int op_lsgi = 0x4c; // 076
        public const int op_lsli = 0x4d; // 077
        public const int op_lsti = 0x4e; // 078
        public const int op_lspi = 0x4f; // 079

        public const int op_sag = 0x50;  // 080
        public const int op_sal = 0x51;  // 081
        public const int op_sat = 0x52;  // 082
        public const int op_sap = 0x53;  // 083
        public const int op_ssg = 0x54;  // 084
        public const int op_ssl = 0x55;  // 085
        public const int op_sst = 0x56;  // 086
        public const int op_ssp = 0x57;  // 087
        public const int op_sagi = 0x58; // 088
        public const int op_sali = 0x59; // 089
        public const int op_sati = 0x5a; // 090
        public const int op_sapi = 0x5b; // 091
        public const int op_ssgi = 0x5c; // 092
        public const int op_ssli = 0x5d; // 093
        public const int op_ssti = 0x5e; // 094
        public const int op_sspi = 0x5f; // 095

        public const int op_plusag = 0x60;   // 096
        public const int op_plusal = 0x61;   // 097
        public const int op_plusat = 0x62;   // 098
        public const int op_plusap = 0x63;   // 099
        public const int op_plussg = 0x64;   // 100
        public const int op_plussl = 0x65;   // 101
        public const int op_plusst = 0x66;   // 102
        public const int op_plussp = 0x67;   // 103
        public const int op_plusagi = 0x68;  // 104
        public const int op_plusali = 0x69;  // 105
        public const int op_plusati = 0x6a;  // 106
        public const int op_plusapi = 0x6b;  // 107
        public const int op_plussgi = 0x6c;  // 108
        public const int op_plussli = 0x6d;  // 109
        public const int op_plussti = 0x6e;  // 110
        public const int op_plusspi = 0x6f;  // 111

        public const int op_minusag = 0x70;  // 112
        public const int op_minusal = 0x71;  // 113
        public const int op_minusat = 0x72;  // 114
        public const int op_minusap = 0x73;  // 115
        public const int op_minussg = 0x74;  // 116
        public const int op_minussl = 0x75;  // 117
        public const int op_minusst = 0x76;  // 118
        public const int op_minussp = 0x77;  // 119
        public const int op_minusagi = 0x78; // 120
        public const int op_minusali = 0x79; // 121
        public const int op_minusati = 0x7a; // 122
        public const int op_minusapi = 0x7b; // 123
        public const int op_minussgi = 0x7c; // 124
        public const int op_minussli = 0x7d; // 125
        public const int op_minussti = 0x7e; // 126
        public const int op_minusspi = 0x7f;  // 127

        public static int SendSelector(EngineState s, Register send_obj, Register work_obj, StackPtr sp, int framesize, StackPtr argp)
        {
            // send_obj and work_obj are equal for anything but 'super'
            // Returns a pointer to the TOS exec_stack element

            Register funcp;
            int selector;
            int argc;
            int origin = s._executionStack.Count - 1; // Origin: Used for debugging
            int activeBreakpointTypes = SciEngine.Instance._debugState._activeBreakpointTypes;
            ObjVarRef varp = new ObjVarRef();

            var prevElementIterator = s._executionStack.Count;

            while (framesize > 0)
            {
                selector = argp[0].RequireUInt16();
                argp++;
                argc = argp[0].RequireUInt16();

                if (argc > 0x800)   // More arguments than the stack could possibly accomodate for
                    throw new InvalidOperationException("send_selector(): More than 0x800 arguments to function call");

                SelectorType selectorType = SciEngine.LookupSelector(s._segMan, send_obj, selector, varp, out funcp);
                if (selectorType == SelectorType.None)
                    throw new InvalidOperationException($"Send to invalid selector 0x{0xffff & selector:X} of object at {send_obj}");

                ExecStackType stackType = ExecStackType.VARSELECTOR;
                StackPtr curSP = null;
                Register32 curFP = Register32.Make(0, 0);
                if (selectorType == SelectorType.Method)
                {
                    stackType = ExecStackType.CALL;
                    curSP = sp;
                    // TODO: Will this offset suffice for large SCI3 scripts?
                    curFP = Register32.Make(funcp.Segment, (ushort)funcp.Offset);
                    sp = CALL_SP_CARRY; // Destroy sp, as it will be carried over
                }

                // TODO: debug
                //if (activeBreakpointTypes || DebugMan.isDebugChannelEnabled(kDebugLevelScripts))
                //    debugSelectorCall(send_obj, selector, argc, argp, varp, funcp, s._segMan, selectorType);

                ExecStack xstack = new ExecStack(work_obj, send_obj, curSP, argc, argp,
                                    0xFFFF, curFP, selector, -1, -1,
                                    origin, stackType);

                if (selectorType == SelectorType.Variable)
                    xstack.varp = varp;

                // The new stack entries should be put on the stack in reverse order
                // so that the first one is executed first
                s._executionStack.Insert(prevElementIterator, xstack);
                // Decrement the stack end pointer so that it points to our recently
                // added element, so that the next insert() places it before this one.
                --prevElementIterator;

                framesize -= (2 + argc);
                argp += argc + 1;
            }   // while (framesize > 0)


            _exec_varselectors(s);

            return s._executionStack.Count;
        }

        public static void Run(EngineState s)
        {
            int temp;
            Register r_temp; // Temporary register
            StackPtr s_temp; // Temporary stack pointer
            short[] opparams = new short[4]; // opcode parameters

            s.r_rest = 0;  // &rest adjusts the parameter count by this value
                           // Current execution data:
            s.xs = s._executionStack.Last();
            ExecStack xs_new = null;
            SciObject obj = s._segMan.GetObject(s.xs.objp);
            Script scr = null;
            Script local_script = s._segMan.GetScriptIfLoaded(s.xs.local_segment);
            int old_executionStackBase = s.executionStackBase;
            // Used to detect the stack bottom, for "physical" returns

            if (local_script == null)
                throw new InvalidOperationException("run_vm(): program counter gone astray (local_script pointer is null)");

            s.executionStackBase = s._executionStack.Count - 1;

            s.variablesSegment[VAR_TEMP] = s.variablesSegment[VAR_PARAM] = s._segMan.FindSegmentByType(SegmentType.STACK);
            s.variablesBase[VAR_TEMP] = s.variablesBase[VAR_PARAM] = s.stack_base;

            s._executionStackPosChanged = true; // Force initialization

# if ABORT_ON_INFINITE_LOOP
            byte prevOpcode = 0xFF;
#endif

            while (true)
            {
                int var_type; // See description below
                int var_number;

                SciEngine.Instance._debugState.old_pc_offset = s.xs.pc.Offset;
                SciEngine.Instance._debugState.old_sp = s.xs.sp;

                if (s.abortScriptProcessing != AbortGameState.None)
                    return; // Stop processing

                if (s._executionStackPosChanged)
                {
                    scr = s._segMan.GetScriptIfLoaded((ushort)s.xs.pc.Segment);
                    if (scr == null)
                        throw new InvalidOperationException($"No script in segment {s.xs.pc.Segment}");
                    s.xs = s._executionStack.Last();
                    s._executionStackPosChanged = false;

                    obj = s._segMan.GetObject(s.xs.objp);
                    local_script = s._segMan.GetScriptIfLoaded(s.xs.local_segment);
                    if (local_script == null)
                    {
                        throw new InvalidOperationException($"Could not find local script from segment {s.xs.local_segment:X}");
                    }
                    else {
                        s.variablesSegment[VAR_LOCAL] = local_script.LocalsSegment;
                        s.variablesBase[VAR_LOCAL] = s.variables[VAR_LOCAL] = local_script.LocalsBegin;
                        s.variablesMax[VAR_LOCAL] = local_script.LocalsCount;
                        s.variablesMax[VAR_TEMP] = s.xs.sp - s.xs.fp;
                        s.variablesMax[VAR_PARAM] = s.xs.argc + 1;
                    }
                    s.variables[VAR_TEMP] = s.xs.fp;
                    s.variables[VAR_PARAM] = s.xs.variables_argp;
                }

                if (s.abortScriptProcessing != AbortGameState.None)
                    return; // Stop processing

                // Debug if this has been requested:
                // TODO: re-implement sci_debug_flags
                if (SciEngine.Instance._debugState.debugging /* sci_debug_flags*/)
                {
                    // TODO: SciEngine.Instance.scriptDebug();
                    SciEngine.Instance._debugState.breakpointWasHit = false;
                }
                // TODO: Console* con = SciEngine.Instance.getSciDebugger();
                //con.onFrame();

                if (s.xs.sp < s.xs.fp)
                    throw new InvalidOperationException($"run_vm(): stack underflow, sp: {s.xs.sp[0]}, fp: {s.xs.fp[0]}");

                s.variablesMax[VAR_TEMP] = s.xs.sp - s.xs.fp;

                if (s.xs.pc.Offset >= scr.BufSize)
                    throw new InvalidOperationException($"run_vm(): program counter gone astray, addr: {s.xs.pc.Offset}, code buffer size: {scr.BufSize}");

                // Get opcode
                byte extOpcode;
                s.xs.pc.IncOffset(ReadPMachineInstruction(scr.GetBuf(s.xs.pc.Offset), out extOpcode, opparams));
                byte opcode = (byte)(extOpcode >> 1);
                //debug("%s: %d, %d, %d, %d, acc = %04x:%04x, script %d, local script %d", opcodeNames[opcode], opparams[0], opparams[1], opparams[2], opparams[3], PRINT_REG(s.r_acc), scr.getScriptNumber(), local_script.getScriptNumber());

# if ABORT_ON_INFINITE_LOOP
                if (prevOpcode != 0xFF)
                {
                    if (prevOpcode == op_eq_ || prevOpcode == op_ne_ ||
                        prevOpcode == op_gt_ || prevOpcode == op_ge_ ||
                        prevOpcode == op_lt_ || prevOpcode == op_le_ ||
                        prevOpcode == op_ugt_ || prevOpcode == op_uge_ ||
                        prevOpcode == op_ult_ || prevOpcode == op_ule_)
                    {
                        if (opcode == op_jmp)
                            throw new InvalidOperationException("Infinite loop detected in script %d", scr.getScriptNumber());
                    }
                }

                prevOpcode = opcode;
#endif

                switch (opcode)
                {

                    case op_bnot: // 0x00 (00)
                                  // Binary not
                        s.r_acc = Register.Make(0, (ushort)(0xffff ^ s.r_acc.RequireUInt16()));
                        break;

                    case op_add: // 0x01 (01)
                        s.r_acc = POP32() + s.r_acc;
                        break;

                    case op_sub: // 0x02 (02)
                        s.r_acc = POP32() - s.r_acc;
                        break;

                    //case op_mul: // 0x03 (03)
                    //    s.r_acc = POP32() * s.r_acc;
                    //    break;

                    //case op_div: // 0x04 (04)
                    //             // we check for division by 0 inside the custom reg_t division operator
                    //    s.r_acc = POP32() / s.r_acc;
                    //    break;

                    //case op_mod: // 0x05 (05)
                    //             // we check for division by 0 inside the custom reg_t modulo operator
                    //    s.r_acc = POP32() % s.r_acc;
                    //    break;

                    //case op_shr: // 0x06 (06)
                    //             // Shift right logical
                    //    s.r_acc = POP32() >> s.r_acc;
                    //    break;

                    //case op_shl: // 0x07 (07)
                    //             // Shift left logical
                    //    s.r_acc = POP32() << s.r_acc;
                    //    break;

                    //case op_xor: // 0x08 (08)
                    //    s.r_acc = POP32() ^ s.r_acc;
                    //    break;

                    //case op_and: // 0x09 (09)
                    //    s.r_acc = POP32() & s.r_acc;
                    //    break;

                    //case op_or: // 0x0a (10)
                    //    s.r_acc = POP32() | s.r_acc;
                    //    break;

                    //case op_neg:    // 0x0b (11)
                    //    s.r_acc = Register.Make(0, -s.r_acc.requireSint16());
                    //    break;

                    //case op_not: // 0x0c (12)
                    //    s.r_acc = Register.Make(0, !(s.r_acc.getOffset() || s.r_acc.getSegment()));
                    //    // Must allow pointers to be negated, as this is used for checking whether objects exist
                    //    break;

                    //case op_eq_: // 0x0d (13)
                    //    s.r_prev = s.r_acc;
                    //    s.r_acc = Register.Make(0, POP32() == s.r_acc);
                    //    break;

                    //case op_ne_: // 0x0e (14)
                    //    s.r_prev = s.r_acc;
                    //    s.r_acc = Register.Make(0, POP32() != s.r_acc);
                    //    break;

                    //case op_gt_: // 0x0f (15)
                    //    s.r_prev = s.r_acc;
                    //    s.r_acc = Register.Make(0, POP32() > s.r_acc);
                    //    break;

                    //case op_ge_: // 0x10 (16)
                    //    s.r_prev = s.r_acc;
                    //    s.r_acc = Register.Make(0, POP32() >= s.r_acc);
                    //    break;

                    //case op_lt_: // 0x11 (17)
                    //    s.r_prev = s.r_acc;
                    //    s.r_acc = Register.Make(0, POP32() < s.r_acc);
                    //    break;

                    //case op_le_: // 0x12 (18)
                    //    s.r_prev = s.r_acc;
                    //    s.r_acc = Register.Make(0, POP32() <= s.r_acc);
                    //    break;

                    //case op_ugt_: // 0x13 (19)
                    //              // > (unsigned)
                    //    s.r_prev = s.r_acc;
                    //    s.r_acc = Register.Make(0, POP32().gtU(s.r_acc));
                    //    break;

                    //case op_uge_: // 0x14 (20)
                    //              // >= (unsigned)
                    //    s.r_prev = s.r_acc;
                    //    s.r_acc = Register.Make(0, POP32().geU(s.r_acc));
                    //    break;

                    //case op_ult_: // 0x15 (21)
                    //              // < (unsigned)
                    //    s.r_prev = s.r_acc;
                    //    s.r_acc = Register.Make(0, POP32().ltU(s.r_acc));
                    //    break;

                    //case op_ule_: // 0x16 (22)
                    //              // <= (unsigned)
                    //    s.r_prev = s.r_acc;
                    //    s.r_acc = Register.Make(0, POP32().leU(s.r_acc));
                    //    break;

                    //case op_bt: // 0x17 (23)
                    //            // Branch relative if true
                    //    if (s.r_acc.Offset != 0 || s.r_acc.Segment != 0)
                    //        s.xs.pc.IncOffset(opparams[0]);

                    //    if (s.xs.pc.Offset >= local_script.ScriptSize)
                    //        throw new InvalidOperationException($"[VM] op_bt: request to jump past the end of script {local_script.ScriptNumber} (offset {s.xs.pc.Offset}, script is {local_script.ScriptSize} bytes)");
                    //    break;

                    //case op_bnt: // 0x18 (24)
                    //             // Branch relative if not true
                    //    if (!(s.r_acc.Offset != 0 || s.r_acc.Segment != 0))
                    //        s.xs.pc.IncOffset(opparams[0]);

                    //    if (s.xs.pc.Offset >= local_script.ScriptSize)
                    //        throw new InvalidOperationException("[VM] op_bnt: request to jump past the end of script %d (offset %d, script is %d bytes)",
                    //            local_script.ScriptNumber, s.xs.pc.Offset, local_script.ScriptSize);
                    //    break;

                    //case op_jmp: // 0x19 (25)
                    //    s.xs.pc.IncOffset(opparams[0]);

                    //    if (s.xs.pc.Offset >= local_script.ScriptSize)
                    //        throw new InvalidOperationException("[VM] op_jmp: request to jump past the end of script %d (offset %d, script is %d bytes)",
                    //            local_script.ScriptNumber, s.xs.pc.Offset, local_script.ScriptSize);
                    //    break;

                    //case op_ldi: // 0x1a (26)
                    //             // Load data immediate
                    //    s.r_acc = Register.Make(0, opparams[0]);
                    //    break;

                    //case op_push: // 0x1b (27)
                    //              // Push to stack
                    //    PUSH32(s.r_acc);
                    //    break;

                    //case op_pushi: // 0x1c (28)
                    //               // Push immediate
                    //    PUSH(opparams[0]);
                    //    break;

                    //case op_toss: // 0x1d (29)
                    //              // TOS (Top Of Stack) subtract
                    //    s.xs.sp--;
                    //    break;

                    //case op_dup: // 0x1e (30)
                    //             // Duplicate TOD (Top Of Stack) element
                    //    r_temp = s.xs.sp[-1];
                    //    PUSH32(r_temp);
                    //    break;

                    //case op_link: // 0x1f (31)
                    //              // We shouldn't initialize temp variables at all
                    //              //  We put special segment 0xFFFF in there, so that uninitialized reads can get detected
                    //    for (int i = 0; i < opparams[0]; i++)
                    //        s.xs.sp[i] = Register.Make(0xffff, 0);

                    //    s.xs.sp += opparams[0];
                    //    break;

                    //case op_call:
                    //    { // 0x20 (32)
                    //      // Call a script subroutine
                    //        int argc = (opparams[1] >> 1) // Given as offset, but we need count
                    //                   + 1 + s.r_rest;
                    //        StackPtr call_base = s.xs.sp - argc;
                    //        s.xs.sp[1].incOffset(s.r_rest);

                    //        uint localCallOffset = s.xs.addr.pc.getOffset() + opparams[0];

                    //        ExecStack xstack(s.xs.objp, s.xs.objp, s.xs.sp,
                    //                        (call_base.requireUint16()) + s.r_rest, call_base,
                    //                        s.xs.local_segment, make_reg32(s.xs.addr.pc.getSegment(), localCallOffset),
                    //                        NULL_SELECTOR, -1, localCallOffset, s._executionStack.size() - 1,
                    //                        EXEC_STACK_TYPE_CALL);

                    //        s._executionStack.push_back(xstack);
                    //        xs_new = &(s._executionStack.back());

                    //        s.r_rest = 0; // Used up the &rest adjustment
                    //        s.xs.sp = call_base;

                    //        s._executionStackPosChanged = true;
                    //        break;
                    //    }

                    case op_callk:
                        { // 0x21 (33)
                          // Run the garbage collector, if needed
                            if (s.gcCountDown-- <= 0)
                            {
                                s.gcCountDown = s.scriptGCInterval;
                                Gc.Run(s);
                            }

                            // Call kernel function
                            s.xs.sp -= (opparams[1] >> 1) + 1;

                            bool oldScriptHeader = (ResourceManager.GetSciVersion() == SciVersion.V0_EARLY);
                            if (!oldScriptHeader)
                                s.xs.sp -= s.r_rest;

                            int argc = s.xs.sp[0].RequireUInt16();

                            if (!oldScriptHeader)
                                argc += s.r_rest;

                            CallKernelFunc(s, opparams[0], argc);

                            if (!oldScriptHeader)
                                s.r_rest = 0;

                            // Calculate xs again: The kernel function might
                            // have spawned a new VM

                            xs_new = s._executionStack.Last();
                            s._executionStackPosChanged = true;

                            // If a game is being loaded, stop processing
                            if (s.abortScriptProcessing != AbortGameState.None)
                                return; // Stop processing

                            break;
                        }

                    //case op_callb: // 0x22 (34)
                    //               // Call base script
                    //    temp = ((opparams[1] >> 1) + s.r_rest + 1);
                    //    s_temp = s.xs.sp;
                    //    s.xs.sp -= temp;

                    //    s.xs.sp[0].incOffset(s.r_rest);
                    //    xs_new = execute_method(s, 0, opparams[0], s_temp, s.xs.objp,
                    //                            s.xs.sp[0].getOffset(), s.xs.sp);
                    //    s.r_rest = 0; // Used up the &rest adjustment
                    //    if (xs_new)    // in case of error, keep old stack
                    //        s._executionStackPosChanged = true;
                    //    break;

                    //case op_calle: // 0x23 (35)
                    //               // Call external script
                    //    temp = ((opparams[2] >> 1) + s.r_rest + 1);
                    //    s_temp = s.xs.sp;
                    //    s.xs.sp -= temp;

                    //    s.xs.sp[0].incOffset(s.r_rest);
                    //    xs_new = execute_method(s, opparams[0], opparams[1], s_temp, s.xs.objp,
                    //                            s.xs.sp[0].getOffset(), s.xs.sp);
                    //    s.r_rest = 0; // Used up the &rest adjustment
                    //    if (xs_new)  // in case of error, keep old stack
                    //        s._executionStackPosChanged = true;
                    //    break;

                    //case op_ret: // 0x24 (36)
                    //             // Return from an execution loop started by call, calle, callb, send, self or super
                    //    do
                    //    {
                    //        StackPtr old_sp2 = s.xs.sp;
                    //        StackPtr old_fp = s.xs.fp;
                    //        ExecStack* old_xs = &(s._executionStack.back());

                    //        if ((int)s._executionStack.size() - 1 == s.executionStackBase)
                    //        { // Have we reached the base?
                    //            s.executionStackBase = old_executionStackBase; // Restore stack base

                    //            s._executionStack.pop_back();

                    //            s._executionStackPosChanged = true;
                    //            return; // "Hard" return
                    //        }

                    //        if (old_xs.type == EXEC_STACK_TYPE_VARSELECTOR)
                    //        {
                    //            // varselector access?
                    //            reg_t* var = old_xs.getVarPointer(s._segMan);
                    //            if (old_xs.argc) // write?
                    //                *var = old_xs.variables_argp[1];
                    //            else // No, read
                    //                s.r_acc = *var;
                    //        }

                    //        // Not reached the base, so let's do a soft return
                    //        s._executionStack.pop_back();
                    //        s._executionStackPosChanged = true;
                    //        s.xs = &(s._executionStack.back());

                    //        if (s.xs.sp == CALL_SP_CARRY // Used in sends to 'carry' the stack pointer
                    //                || s.xs.type != EXEC_STACK_TYPE_CALL)
                    //        {
                    //            s.xs.sp = old_sp2;
                    //            s.xs.fp = old_fp;
                    //        }

                    //    } while (s.xs.type == EXEC_STACK_TYPE_VARSELECTOR);
                    //    // Iterate over all varselector accesses
                    //    s._executionStackPosChanged = true;
                    //    xs_new = s.xs;

                    //    break;

                    //case op_send: // 0x25 (37)
                    //              // Send for one or more selectors
                    //    s_temp = s.xs.sp;
                    //    s.xs.sp -= ((opparams[0] >> 1) + s.r_rest); // Adjust stack

                    //    s.xs.sp[1].incOffset(s.r_rest);
                    //    xs_new = send_selector(s, s.r_acc, s.r_acc, s_temp,
                    //                            (int)(opparams[0] >> 1) + (uint16)s.r_rest, s.xs.sp);

                    //    if (xs_new && xs_new != s.xs)
                    //        s._executionStackPosChanged = true;

                    //    s.r_rest = 0;

                    //    break;

                    //case 0x26: // (38)
                    //case 0x27: // (39)
                    //    if (getSciVersion() == SCI_VERSION_3)
                    //    {
                    //        if (extOpcode == 0x4c)
                    //            s.r_acc = obj.getInfoSelector();
                    //        else if (extOpcode == 0x4d)
                    //            PUSH32(obj.getInfoSelector());
                    //        else if (extOpcode == 0x4e)
                    //            s.r_acc = obj.getSuperClassSelector();    // TODO: is this correct?
                    //                                                      // TODO: There are also opcodes in
                    //                                                      // here to get the superclass, and possibly the species too.
                    //        else
                    //            throw new InvalidOperationException("Dummy opcode 0x%x called", opcode);  // should never happen
                    //    }
                    //    else
                    //        throw new InvalidOperationException("Dummy opcode 0x%x called", opcode);  // should never happen
                    //    break;

                    //case op_class: // 0x28 (40)
                    //               // Get class address
                    //    s.r_acc = s._segMan.getClassAddress((unsigned)opparams[0], SCRIPT_GET_LOCK,
                    //                                    s.xs.addr.pc.getSegment());
                    //    break;

                    //case 0x29: // (41)
                    //    throw new InvalidOperationException("Dummy opcode 0x%x called", opcode);  // should never happen
                    //    break;

                    //case op_self: // 0x2a (42)
                    //              // Send to self
                    //    s_temp = s.xs.sp;
                    //    s.xs.sp -= ((opparams[0] >> 1) + s.r_rest); // Adjust stack

                    //    s.xs.sp[1].incOffset(s.r_rest);
                    //    xs_new = send_selector(s, s.xs.objp, s.xs.objp,
                    //                            s_temp, (int)(opparams[0] >> 1) + (uint16)s.r_rest,
                    //                            s.xs.sp);

                    //    if (xs_new && xs_new != s.xs)
                    //        s._executionStackPosChanged = true;

                    //    s.r_rest = 0;
                    //    break;

                    //case op_super: // 0x2b (43)
                    //               // Send to any class
                    //    r_temp = s._segMan.getClassAddress(opparams[0], SCRIPT_GET_LOAD, s.xs.addr.pc.getSegment());

                    //    if (!r_temp.isPointer())
                    //        throw new InvalidOperationException("[VM]: Invalid superclass in object");
                    //    else {
                    //        s_temp = s.xs.sp;
                    //        s.xs.sp -= ((opparams[1] >> 1) + s.r_rest); // Adjust stack

                    //        s.xs.sp[1].incOffset(s.r_rest);
                    //        xs_new = send_selector(s, r_temp, s.xs.objp, s_temp,
                    //                                (int)(opparams[1] >> 1) + (uint16)s.r_rest,
                    //                                s.xs.sp);

                    //        if (xs_new && xs_new != s.xs)
                    //            s._executionStackPosChanged = true;

                    //        s.r_rest = 0;
                    //    }

                    //    break;

                    //case op_rest: // 0x2c (44)
                    //              // Pushes all or part of the parameter variable list on the stack
                    //    temp = (uint16)opparams[0]; // First argument
                    //    s.r_rest = MAX<int16>(s.xs.argc - temp + 1, 0); // +1 because temp counts the paramcount while argc doesn't

                    //    for (; temp <= s.xs.argc; temp++)
                    //        PUSH32(s.xs.variables_argp[temp]);

                    //    break;

                    //case op_lea: // 0x2d (45)
                    //             // Load Effective Address
                    //    temp = (uint16)opparams[0] >> 1;
                    //    var_number = temp & 0x03; // Get variable type

                    //    // Get variable block offset
                    //    r_temp.setSegment(s.variablesSegment[var_number]);
                    //    r_temp.setOffset(s.variables[var_number] - s.variablesBase[var_number]);

                    //    if (temp & 0x08)  // Add accumulator offset if requested
                    //        r_temp.incOffset(s.r_acc.requireSint16());

                    //    r_temp.incOffset(opparams[1]);  // Add index
                    //    r_temp.setOffset(r_temp.getOffset() * 2); // variables are 16 bit
                    //                                              // That's the immediate address now
                    //    s.r_acc = r_temp;
                    //    break;


                    case op_selfID: // 0x2e (46)
                                    // Get 'self' identity
                        s.r_acc = s.xs.objp;
                        break;

                    case 0x2f: // (47)
                        throw new InvalidOperationException($"Dummy opcode 0x{opcode:x} called");  // should never happen

                    //case op_pprev: // 0x30 (48)
                    //               // Pushes the value of the prev register, set by the last comparison
                    //               // bytecode (eq?, lt?, etc.), on the stack
                    //    PUSH32(s.r_prev);
                    //    break;

                    //case op_pToa: // 0x31 (49)
                    //              // Property To Accumulator
                    //    s.r_acc = validate_property(s, obj, opparams[0]);
                    //    break;

                    //case op_aTop: // 0x32 (50)
                    //              // Accumulator To Property
                    //    validate_property(s, obj, opparams[0]) = s.r_acc;
                    //    break;

                    //case op_pTos: // 0x33 (51)
                    //              // Property To Stack
                    //    PUSH32(validate_property(s, obj, opparams[0]));
                    //    break;

                    //case op_sTop: // 0x34 (52)
                    //              // Stack To Property
                    //    validate_property(s, obj, opparams[0]) = POP32();
                    //    break;

                    //case op_ipToa: // 0x35 (53)
                    //case op_dpToa: // 0x36 (54)
                    //case op_ipTos: // 0x37 (55)
                    //case op_dpTos: // 0x38 (56)
                    //    {
                    //        // Increment/decrement a property and copy to accumulator,
                    //        // or push to stack
                    //        reg_t & opProperty = validate_property(s, obj, opparams[0]);
                    //        if (opcode & 1)
                    //            opProperty += 1;
                    //        else
                    //            opProperty -= 1;

                    //        if (opcode == op_ipToa || opcode == op_dpToa)
                    //            s.r_acc = opProperty;
                    //        else
                    //            PUSH32(opProperty);
                    //        break;
                    //    }

                    //case op_lofsa: // 0x39 (57)
                    //case op_lofss: // 0x3a (58)
                    //               // Load offset to accumulator or push to stack
                    //    r_temp.setSegment(s.xs.addr.pc.getSegment());

                    //    switch (SciEngine.Instance._features.detectLofsType())
                    //    {
                    //        case SCI_VERSION_0_EARLY:
                    //            r_temp.setOffset((uint16)s.xs.addr.pc.getOffset() + opparams[0]);
                    //            break;
                    //        case SCI_VERSION_1_MIDDLE:
                    //            r_temp.setOffset(opparams[0]);
                    //            break;
                    //        case SCI_VERSION_1_1:
                    //            r_temp.setOffset(opparams[0] + local_script.getScriptSize());
                    //            break;
                    //        case SCI_VERSION_3:
                    //            // In theory this can break if the variant with a one-byte argument is
                    //            // used. For now, assume it doesn't happen.
                    //            r_temp.setOffset(local_script.relocateOffsetSci3(s.xs.addr.pc.getOffset() - 2));
                    //            break;
                    //        default:
                    //            throw new InvalidOperationException("Unknown lofs type");
                    //    }

                    //    if (r_temp.getOffset() >= scr.getBufSize())
                    //        throw new InvalidOperationException("VM: lofsa/lofss operation overflowed: %04x:%04x beyond end"


                    //                  " of script (at %04x)", PRINT_REG(r_temp), scr.getBufSize());

                    //    if (opcode == op_lofsa)
                    //        s.r_acc = r_temp;
                    //    else
                    //        PUSH32(r_temp);
                    //    break;

                    case op_push0: // 0x3b (59)
                        PUSH(0);
                        break;

                    case op_push1: // 0x3c (60)
                        PUSH(1);
                        break;

                    case op_push2: // 0x3d (61)
                        PUSH(2);
                        break;

                    //case op_pushSelf: // 0x3e (62)
                    //                  // Compensate for a bug in non-Sierra compilers, which seem to generate
                    //                  // pushSelf instructions with the low bit set. This makes the following
                    //                  // heuristic fail and leads to endless loops and crashes. Our
                    //                  // interpretation of this seems correct, as other SCI tools, like for
                    //                  // example SCI Viewer, have issues with these scripts (e.g. script 999
                    //                  // in Circus Quest). Fixes bug #3038686.
                    //    if (!(extOpcode & 1) || SciEngine.Instance.GameId == SciGameId.FANMADE)
                    //    {
                    //        PUSH32(s.xs.objp);
                    //    }
                    //    else {
                    //        // Debug opcode op_file
                    //    }
                    //    break;

                    //case op_line: // 0x3f (63)
                    //              // Debug opcode (line number)
                    //              //debug("Script %d, line %d", scr.getScriptNumber(), opparams[0]);
                    //    break;

                    //case op_lag: // 0x40 (64)
                    //case op_lal: // 0x41 (65)
                    //case op_lat: // 0x42 (66)
                    //case op_lap: // 0x43 (67)
                    //             // Load global, local, temp or param variable into the accumulator
                    //case op_lagi: // 0x48 (72)
                    //case op_lali: // 0x49 (73)
                    //case op_lati: // 0x4a (74)
                    //case op_lapi: // 0x4b (75)
                    //              // Same as the 4 ones above, except that the accumulator is used as
                    //              // an additional index
                    //    var_type = opcode & 0x3; // Gets the variable type: g, l, t or p
                    //    var_number = opparams[0] + (opcode >= op_lagi ? s.r_acc.requireSint16() : 0);
                    //    s.r_acc = read_var(s, var_type, var_number);
                    //    break;

                    //case op_lsg: // 0x44 (68)
                    //case op_lsl: // 0x45 (69)
                    //case op_lst: // 0x46 (70)
                    //case op_lsp: // 0x47 (71)
                    //             // Load global, local, temp or param variable into the stack
                    //case op_lsgi: // 0x4c (76)
                    //case op_lsli: // 0x4d (77)
                    //case op_lsti: // 0x4e (78)
                    //case op_lspi: // 0x4f (79)
                    //              // Same as the 4 ones above, except that the accumulator is used as
                    //              // an additional index
                    //    var_type = opcode & 0x3; // Gets the variable type: g, l, t or p
                    //    var_number = opparams[0] + (opcode >= op_lsgi ? s.r_acc.requireSint16() : 0);
                    //    PUSH32(read_var(s, var_type, var_number));
                    //    break;

                    case op_sag: // 0x50 (80)
                    case op_sal: // 0x51 (81)
                    case op_sat: // 0x52 (82)
                    case op_sap: // 0x53 (83)
                                 // Save the accumulator into the global, local, temp or param variable
                    case op_sagi: // 0x58 (88)
                    case op_sali: // 0x59 (89)
                    case op_sati: // 0x5a (90)
                    case op_sapi: // 0x5b (91)
                                  // Save the accumulator into the global, local, temp or param variable,
                                  // using the accumulator as an additional index
                        var_type = opcode & 0x3; // Gets the variable type: g, l, t or p
                        var_number = opparams[0] + (opcode >= op_sagi ? s.r_acc.RequireInt16() : 0);
                        if (opcode >= op_sagi)  // load the actual value to store in the accumulator
                            s.r_acc = POP32();
                        write_var(s, var_type, var_number, s.r_acc);
                        break;

                    //case op_ssg: // 0x54 (84)
                    //case op_ssl: // 0x55 (85)
                    //case op_sst: // 0x56 (86)
                    //case op_ssp: // 0x57 (87)
                    //             // Save the stack into the global, local, temp or param variable
                    //case op_ssgi: // 0x5c (92)
                    //case op_ssli: // 0x5d (93)
                    //case op_ssti: // 0x5e (94)
                    //case op_sspi: // 0x5f (95)
                    //              // Same as the 4 ones above, except that the accumulator is used as
                    //              // an additional index
                    //    var_type = opcode & 0x3; // Gets the variable type: g, l, t or p
                    //    var_number = opparams[0] + (opcode >= op_ssgi ? s.r_acc.requireSint16() : 0);
                    //    write_var(s, var_type, var_number, POP32());
                    //    break;

                    //case op_plusag: // 0x60 (96)
                    //case op_plusal: // 0x61 (97)
                    //case op_plusat: // 0x62 (98)
                    //case op_plusap: // 0x63 (99)
                    //                // Increment the global, local, temp or param variable and save it
                    //                // to the accumulator
                    //case op_plusagi: // 0x68 (104)
                    //case op_plusali: // 0x69 (105)
                    //case op_plusati: // 0x6a (106)
                    //case op_plusapi: // 0x6b (107)
                    //                 // Same as the 4 ones above, except that the accumulator is used as
                    //                 // an additional index
                    //    var_type = opcode & 0x3; // Gets the variable type: g, l, t or p
                    //    var_number = opparams[0] + (opcode >= op_plusagi ? s.r_acc.requireSint16() : 0);
                    //    s.r_acc = read_var(s, var_type, var_number) + 1;
                    //    write_var(s, var_type, var_number, s.r_acc);
                    //    break;

                    //case op_plussg: // 0x64 (100)
                    //case op_plussl: // 0x65 (101)
                    //case op_plusst: // 0x66 (102)
                    //case op_plussp: // 0x67 (103)
                    //                // Increment the global, local, temp or param variable and save it
                    //                // to the stack
                    //case op_plussgi: // 0x6c (108)
                    //case op_plussli: // 0x6d (109)
                    //case op_plussti: // 0x6e (110)
                    //case op_plusspi: // 0x6f (111)
                    //                 // Same as the 4 ones above, except that the accumulator is used as
                    //                 // an additional index
                    //    var_type = opcode & 0x3; // Gets the variable type: g, l, t or p
                    //    var_number = opparams[0] + (opcode >= op_plussgi ? s.r_acc.requireSint16() : 0);
                    //    r_temp = read_var(s, var_type, var_number) + 1;
                    //    PUSH32(r_temp);
                    //    write_var(s, var_type, var_number, r_temp);
                    //    break;

                    //case op_minusag: // 0x70 (112)
                    //case op_minusal: // 0x71 (113)
                    //case op_minusat: // 0x72 (114)
                    //case op_minusap: // 0x73 (115)
                    //                 // Decrement the global, local, temp or param variable and save it
                    //                 // to the accumulator
                    //case op_minusagi: // 0x78 (120)
                    //case op_minusali: // 0x79 (121)
                    //case op_minusati: // 0x7a (122)
                    //case op_minusapi: // 0x7b (123)
                    //                  // Same as the 4 ones above, except that the accumulator is used as
                    //                  // an additional index
                    //    var_type = opcode & 0x3; // Gets the variable type: g, l, t or p
                    //    var_number = opparams[0] + (opcode >= op_minusagi ? s.r_acc.requireSint16() : 0);
                    //    s.r_acc = read_var(s, var_type, var_number) - 1;
                    //    write_var(s, var_type, var_number, s.r_acc);
                    //    break;

                    //case op_minussg: // 0x74 (116)
                    //case op_minussl: // 0x75 (117)
                    //case op_minusst: // 0x76 (118)
                    //case op_minussp: // 0x77 (119)
                    //                 // Decrement the global, local, temp or param variable and save it
                    //                 // to the stack
                    //case op_minussgi: // 0x7c (124)
                    //case op_minussli: // 0x7d (125)
                    //case op_minussti: // 0x7e (126)
                    //case op_minusspi: // 0x7f (127)
                    //                  // Same as the 4 ones above, except that the accumulator is used as
                    //                  // an additional index
                    //    var_type = opcode & 0x3; // Gets the variable type: g, l, t or p
                    //    var_number = opparams[0] + (opcode >= op_minussgi ? s.r_acc.requireSint16() : 0);
                    //    r_temp = read_var(s, var_type, var_number) - 1;
                    //    PUSH32(r_temp);
                    //    write_var(s, var_type, var_number, r_temp);
                    //    break;

                    default:
                        throw new InvalidOperationException($"run_vm(): illegal opcode 0x{opcode:X}");

                } // switch (opcode)

                if (s._executionStackPosChanged) // Force initialization
                    s.xs = xs_new;

                if (s.xs != s._executionStack.Last())
                {
                    throw new InvalidOperationException($"xs is stale ({s.xs} vs {s._executionStack.Last()}); last command was {opcode:X2}");
                }
                ++s.scriptStepCounter;
            }
        }

        public static int ReadPMachineInstruction(ByteAccess src, out byte extOpcode, short[] opparams)
        {
            int offset = 0;
            extOpcode = src[offset++]; // Get "extended" opcode (lower bit has special meaning)
            byte opcode = (byte)(extOpcode >> 1); // get the actual opcode

            Array.Clear(opparams, 0, 4);

            for (int i = 0; SciEngine.Instance._opcode_formats[opcode][i] != 0; ++i)
            {
                //debugN("Opcode: 0x%x, Opnumber: 0x%x, temp: %d\n", opcode, opcode, temp);
                switch (SciEngine.Instance._opcode_formats[opcode][i])
                {
                    case opcode_format.Script_Byte:
                        opparams[i] = src[offset++];
                        break;
                    case opcode_format.Script_SByte:
                        opparams[i] = (sbyte)src[offset++];
                        break;
                    case opcode_format.Script_Word:
                        opparams[i] = (short)src.Data.ReadSci11EndianUInt16(src.Offset + offset);
                        offset += 2;
                        break;
                    case opcode_format.Script_SWord:
                        opparams[i] = (short)src.Data.ReadSci11EndianUInt16(src.Offset + offset);
                        offset += 2;
                        break;

                    case opcode_format.Script_Variable:
                    case opcode_format.Script_Property:

                    case opcode_format.Script_Local:
                    case opcode_format.Script_Temp:
                    case opcode_format.Script_Global:
                    case opcode_format.Script_Param:

                    case opcode_format.Script_Offset:
                        if ((extOpcode & 1) != 0)
                        {
                            opparams[i] = src[offset++];
                        }
                        else {
                            opparams[i] = (short)src.Data.ReadSci11EndianUInt16(src.Offset + offset);
                            offset += 2;
                        }
                        break;

                    case opcode_format.Script_SVariable:
                    case opcode_format.Script_SRelative:
                        if ((extOpcode & 1) != 0)
                        {
                            opparams[i] = (sbyte)src[offset++];
                        }
                        else {
                            opparams[i] = (short)src.Data.ReadSci11EndianUInt16(src.Offset + offset);
                            offset += 2;
                        }
                        break;

                    case opcode_format.Script_None:
                    case opcode_format.Script_End:
                        break;

                    case opcode_format.Script_Invalid:
                    default:
                        throw new InvalidOperationException($"opcode {extOpcode:X2}: Invalid");
                }
            }

            // Special handling of the op_line opcode
            if (opcode == Vm.op_pushSelf)
            {
                // Compensate for a bug in non-Sierra compilers, which seem to generate
                // pushSelf instructions with the low bit set. This makes the following
                // heuristic fail and leads to endless loops and crashes. Our
                // interpretation of this seems correct, as other SCI tools, like for
                // example SCI Viewer, have issues with these scripts (e.g. script 999
                // in Circus Quest). Fixes bug #3038686.
                if (((extOpcode & 1) == 0) || SciEngine.Instance.GameId == SciGameId.FANMADE)
                {
                    // op_pushSelf: no adjustment necessary
                }
                else {
                    // Debug opcode op_file, skip null-terminated string (file name)
                    while (src[offset++] != 0) { }
                }
            }

            return offset;
        }

        private static void CallKernelFunc(EngineState s, short kernelCallNr, int argc)
        {
            Kernel kernel = SciEngine.Instance.Kernel;

            if (kernelCallNr >= kernel._kernelFuncs.Length)
                throw new InvalidOperationException($"Invalid kernel function 0x{kernelCallNr:X} requested");

            KernelFunction kernelCall = kernel._kernelFuncs[kernelCallNr];
            var argv = s.xs.sp + 1;

            if (kernelCall.signature != null
                    && !kernel.SignatureMatch(kernelCall.signature, argc, argv))
            {
                // signature mismatch, check if a workaround is available
                SciTrackOriginReply originReply;
                SciWorkaroundSolution solution = Workarounds.TrackOriginAndFindWorkaround(0, kernelCall.workarounds, out originReply);
                switch (solution.type)
                {
                    case SciWorkaroundType.NONE:
                        kernel.SignatureDebug(kernelCall.signature, argc, argv);
                        throw new InvalidOperationException($"[VM] k{kernelCall.name}[{kernelCallNr:X}]: signature mismatch via method {originReply.objectName}::{originReply.methodName} (room {s.CurrentRoomNumber}, script {originReply.scriptNr}, localCall 0x{originReply.localCallOffset:X})");
                    case SciWorkaroundType.IGNORE: // don't do kernel call, leave acc alone
                        return;
                    case SciWorkaroundType.STILLCALL: // call kernel anyway
                        break;
                    case SciWorkaroundType.FAKE: // don't do kernel call, fake acc
                        s.r_acc = Register.Make(0, solution.value);
                        return;
                    default:
                        throw new InvalidOperationException("unknown workaround type");
                }
            }


            // Call kernel function
            if (kernelCall.subFunctionCount == 0)
            {
                AddKernelCallToExecStack(s, kernelCallNr, argc, argv);
                s.r_acc = kernelCall.function(s, argc, argv);

                if (kernelCall.debugLogging)
                    LogKernelCall(kernelCall, null, s, argc, argv, s.r_acc);
                if (kernelCall.debugBreakpoint)
                {
                    // TODO: debugN("Break on k%s\n", kernelCall.name);
                    SciEngine.Instance._debugState.debugging = true;
                    SciEngine.Instance._debugState.breakpointWasHit = true;
                }
            }
            else {
                // Sub-functions available, check signature and call that one directly
                if (argc < 1)
                    throw new InvalidOperationException($"[VM] k{kernelCall.name}[{kernelCallNr:X}]: no subfunction ID parameter given");
                if (argv[0].IsPointer)
                    throw new InvalidOperationException($"[VM] k{kernelCall.name}[{kernelCallNr:X}]: given subfunction ID is actually a pointer");
                var subId = argv[0].ToUInt16();
                // Skip over subfunction-id
                argc--;
                argv++;
                if (subId >= kernelCall.subFunctionCount)
                    throw new InvalidOperationException($"[VM] k%{kernelCall.name}: subfunction ID {subId} requested, but not available");
                KernelSubFunction kernelSubCall = kernelCall.subFunctions[subId];
                if (kernelSubCall.signature != null && !kernel.SignatureMatch(kernelSubCall.signature, argc, argv))
                {
                    // Signature mismatch
                    SciTrackOriginReply originReply;
                    SciWorkaroundSolution solution = Workarounds.TrackOriginAndFindWorkaround(0, kernelSubCall.workarounds, out originReply);
                    switch (solution.type)
                    {
                        case SciWorkaroundType.NONE:
                            {
                                kernel.SignatureDebug(kernelSubCall.signature, argc, argv);
                                int callNameLen = kernelCall.name.Length;
                                if (string.CompareOrdinal(kernelCall.name, 0, kernelSubCall.name, 0, callNameLen) == 0)
                                {
                                    var subCallName = kernelSubCall.name.Substring(callNameLen);
                                    throw new InvalidOperationException($"[VM] k{kernelCall.name}({subCallName}): signature mismatch via method {originReply.objectName}::{originReply.methodName} (room {s.CurrentRoomNumber}, script {originReply.scriptNr}, localCall {originReply.localCallOffset:X})");
                                }
                                throw new InvalidOperationException($"[VM] k{kernelSubCall.name}: signature mismatch via method {originReply.objectName}::{originReply.methodName} (room {s.CurrentRoomNumber}, script {originReply.scriptNr}, localCall {originReply.localCallOffset:X})");
                            }
                        case SciWorkaroundType.IGNORE: // don't do kernel call, leave acc alone
                            return;
                        case SciWorkaroundType.STILLCALL: // call kernel anyway
                            break;
                        case SciWorkaroundType.FAKE: // don't do kernel call, fake acc
                            s.r_acc = Register.Make(0, solution.value);
                            return;
                        default:
                            throw new InvalidOperationException("unknown workaround type");
                    }
                }
                if (kernelSubCall.function == null)
                    throw new InvalidOperationException("[VM] k{kernelCall.name}: subfunction ID {subId} requested, but not available");
                AddKernelCallToExecStack(s, kernelCallNr, argc, argv);
                s.r_acc = kernelSubCall.function(s, argc, argv);

                if (kernelSubCall.debugLogging)
                    LogKernelCall(kernelCall, kernelSubCall, s, argc, argv, s.r_acc);
                if (kernelSubCall.debugBreakpoint)
                {
                    // TODO: debugN("Break on k%s\n", kernelSubCall.name);
                    SciEngine.Instance._debugState.debugging = true;
                    SciEngine.Instance._debugState.breakpointWasHit = true;
                }
            }

            // Remove callk stack frame again, if there's still an execution stack
            var es = s._executionStack.LastOrDefault();
            if (es != null)
                s._executionStack.Remove(es);
        }

        private static void LogKernelCall(KernelFunction kernelCall, object p, EngineState s, int argc, StackPtr argv, Register r_acc)
        {
            throw new NotImplementedException();
        }

        private static void AddKernelCallToExecStack(EngineState s, short kernelCallNr, int argc, StackPtr argv)
        {
            // Add stack frame to indicate we're executing a callk.
            // This is useful in debugger backtraces if this
            // kernel function calls a script itself.
            ExecStack xstack = new ExecStack(Register.NULL_REG, Register.NULL_REG, null, argc, argv - 1, 0xFFFF, Register32.Make(0, 0),
                                kernelCallNr, -1, -1, s._executionStack.Count - 1, ExecStackType.KERNEL);
            s._executionStack.Add(xstack);
        }

        private static void write_var(EngineState s, int type, int index, Register value)
        {
            if (validate_variable(s.variables[type], s.stack_base, type, s.variablesMax[type], index))
            {

                // WORKAROUND: This code is needed to work around a probable script bug, or a
                // limitation of the original SCI engine, which can be observed in LSL5.
                //
                // In some games, ego walks via the "Grooper" object, in particular its "stopGroop"
                // child. In LSL5, during the game, ego is swapped from Larry to Patti. When this
                // happens in the original interpreter, the new actor is loaded in the same memory
                // location as the old one, therefore the client variable in the stopGroop object
                // points to the new actor. This is probably why the reference of the stopGroop
                // object is never updated (which is why I mentioned that this is either a script
                // bug or some kind of limitation).
                //
                // In our implementation, each new object is loaded in a different memory location,
                // and we can't overwrite the old one. This means that in our implementation,
                // whenever ego is changed, we need to update the "client" variable of the
                // stopGroop object, which points to ego, to the new ego object. If this is not
                // done, ego's movement will not be updated properly, so the result is
                // unpredictable (for example in LSL5, Patti spins around instead of walking).
                if (index == 0 && type == VAR_GLOBAL && ResourceManager.GetSciVersion() > SciVersion.V0_EARLY)
                {   // global 0 is ego
                    Register stopGroopPos = s._segMan.FindObjectByName("stopGroop");
                    if (!stopGroopPos.IsNull)
                    {   // does the game have a stopGroop object?
                        // Find the "client" member variable of the stopGroop object, and update it
                        ObjVarRef varp = new ObjVarRef();
                        Register tmp;
                        if (SciEngine.LookupSelector(s._segMan, stopGroopPos, SciEngine.Selector(o => o.client), varp, out tmp) == SelectorType.Variable)
                        {
                            Register clientVar = varp.GetPointer(s._segMan);
                            clientVar.Set(value);
                        }
                    }
                }

                // If we are writing an uninitialized value into a temp, we remove the uninitialized segment
                //  this happens at least in sq1/room 44 (slot-machine), because a send is missing parameters, then
                //  those parameters are taken from uninitialized stack and afterwards they are copied back into temps
                //  if we don't remove the segment, we would get false-positive uninitialized reads later
                if (type == VAR_TEMP && value.Segment == 0xffff)
                    value.SetSegment(0);

                s.variables[type][index] = value;

                if (type == VAR_GLOBAL && index == 90)
                {
                    // The game is trying to change its speech/subtitle settings
                    if (!SciEngine.Instance.EngineState._syncedAudioOptions || s.variables[VAR_GLOBAL][4] == Register.TRUE_REG)
                    {
                        // ScummVM audio options haven't been applied yet, so apply them.
                        // We also force the ScummVM audio options when loading a game from
                        // the launcher.
                        // TODO: SciEngine.Instance.SyncIngameAudioOptions();
                        SciEngine.Instance.EngineState._syncedAudioOptions = true;
                    }
                    else {
                        // Update ScummVM's audio options
                        // TODO: SciEngine.Instance.UpdateScummVMAudioOptions();
                    }
                }
            }
        }

        private static bool validate_variable(StackPtr r, StackPtr stack_base, int type, int max, int index)
        {
            string[] names = { "global", "local", "temp", "param" };

            if (index < 0 || index >= max)
            {
                string txt = $"[VM] Attempt to use invalid {names[type]} variable {index:X4}";
                if (max == 0)
                    txt += "(variable type invalid)";
                else
                    txt += $"(out of range [0..{max - 1}])";

                if (type == VAR_PARAM || type == VAR_TEMP)
                {
                    int total_offset = r - stack_base;
                    if (total_offset < 0 || total_offset >= STACK_SIZE)
                    {
                        // Fatal, as the game is trying to do an OOB access
                        throw new InvalidOperationException($"{txt}. [VM] Access would be outside even of the stack ({total_offset}); access denied");
                    }
                    else {
                        // TODO: debugC(kDebugLevelVM, "%s", txt.c_str());
                        // TODO: debugC(kDebugLevelVM, "[VM] Access within stack boundaries; access granted.");
                        return true;
                    }
                }
                return false;
            }

            return true;
        }

        private static void _exec_varselectors(EngineState s)
        {
            // Executes all varselector read/write ops on the TOS
            while (s._executionStack.Count != 0 && s._executionStack.Last().type == ExecStackType.VARSELECTOR)
            {
                ExecStack xs = s._executionStack.Last();
                var var = xs.GetVarPointer(s._segMan);
                if (var == null)
                {
                    throw new InvalidOperationException("Invalid varselector exec stack entry");
                }
                else {
                    // varselector access?
                    if (xs.argc != 0)
                    { // write?
                        var.Set(xs.variables_argp[1]);

                    }
                    else // No, read
                        s.r_acc = var;
                }
                s._executionStack.Remove(xs);
            }
        }

        // Operating on the stack
        // 16 bit:
        private static void PUSH(int v)
        {
            PUSH32(Register.Make(0, (ushort)v));
        }

        // 32 bit:
        private static void PUSH32(Register a)
        {
            var s = SciEngine.Instance.EngineState;
            validate_stack_addr(s, (s.xs.sp)++)[0].Set(a);
        }

        public static Register POP32()
        {
            var s = SciEngine.Instance.EngineState;
            return validate_stack_addr(s, --(s.xs.sp))[0];
        }

        static StackPtr validate_stack_addr(EngineState s, StackPtr sp)
        {
            if (sp >= s.stack_base && sp < s.stack_top)
                return sp;
            else
                throw new InvalidOperationException("[VM] Stack index {sp - s.stack_base} out of valid range [0..{s.stack_top - s.stack_base - 1}]");
        }
    }
}
