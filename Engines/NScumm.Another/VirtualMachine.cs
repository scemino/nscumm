//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2017 scemino
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

#define BYPASS_PROTECTION

using System;
using System.Collections.Generic;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Another
{
    internal enum ScriptVars
    {
        VmVariableRandomSeed = 0x3C,

        VmVariableLastKeychar = 0xDA,

        VmVariableHeroPosUpDown = 0xE5,

        VmVariableMusMark = 0xF4,

        VmVariableScrollY = 0xF9, // = 239
        VmVariableHeroAction = 0xFA,
        VmVariableHeroPosJumpDown = 0xFB,
        VmVariableHeroPosLeftRight = 0xFC,
        VmVariableHeroPosMask = 0xFD,
        VmVariableHeroActionPosMask = 0xFE,
        VmVariablePauseSlices = 0xFF
    }

    internal class VirtualMachine
    {
        private const int VmNumThreads = 64;
        private const int VmNumVariables = 256;
        private const int VmNoSetvecRequested = 0xFFFF;
        private const int VmInactiveThread = 0xFFFF;

        //For threadsData navigation
        private const int PcOffset = 0;

        private const int RequestedPcOffset = 1;
        private const int NumDataFields = 2;

        //For vmIsChannelActive navigation
        private const int CurrState = 0;

        private const int RequestedState = 1;
        private const int NumThreadFields = 2;

        private const int ColorBlack = 0xFF;
        private const int DefaultZoom = 0x40;

        private readonly short[] _vmVariables = new short[VmNumVariables];
        private readonly ushort[] _scriptStackCalls = new ushort[VmNumThreads];

        private readonly ushort[][] _threadsData = CreateThreadsData();

        // This array is used:
        //     0 to save the channel's instruction pointer
        //     when the channel release control (this happens on a break).

        //     1 When a setVec is requested for the next vm frame.
        private readonly byte[][] _vmIsChannelActive = CreateVmIsChannelActive();

        private static readonly ushort[] FrequenceTable =
        {
            0x0CFF, 0x0DC3, 0x0E91, 0x0F6F, 0x1056, 0x114E, 0x1259, 0x136C,
            0x149F, 0x15D9, 0x1726, 0x1888, 0x19FD, 0x1B86, 0x1D21, 0x1EDE,
            0x20AB, 0x229C, 0x24B3, 0x26D7, 0x293F, 0x2BB2, 0x2E4C, 0x3110,
            0x33FB, 0x370D, 0x3A43, 0x3DDF, 0x4157, 0x4538, 0x4998, 0x4DAE,
            0x5240, 0x5764, 0x5C9A, 0x61C8, 0x6793, 0x6E19, 0x7485, 0x7BBD
        };

        public bool FastMode;

        private readonly Resource _res;
        private readonly Video _video;
        private readonly IAnotherSystem _sys;
        private readonly AnotherMixer _mixer;
        private readonly SfxPlayer _player;
        private readonly Dictionary<int, Action> _opcodeTable;

        private byte _stackPtr;
        private bool _gotoNextThread;
        private BytePtr _scriptPtr;
        private uint _lastTimeStamp;

        public VirtualMachine(AnotherMixer mixer, Resource resParameter, SfxPlayer player, Video vid,
            IAnotherSystem sys)
        {
            _sys = sys;
            _mixer = mixer;
            _res = resParameter;
            _player = player;
            _video = vid;

            _opcodeTable = new Dictionary<int, Action>
            {
                /* 0x00 */
                {0x00, op_movConst},
                {0x01, op_mov},
                {0x02, op_add},
                {0x03, op_addConst},
                /* 0x04 */
                {0x04, op_call},
                {0x05, op_ret},
                {0x06, op_pauseThread},
                {0x07, op_jmp},
                /* 0x08 */
                {0x08, op_setSetVect},
                {0x09, op_jnz},
                {0x0A, op_condJmp},
                {0x0B, op_setPalette},
                /* 0x0C */
                {0x0C, op_resetThread},
                {0x0D, op_selectVideoPage},
                {0x0E, op_fillVideoPage},
                {0x0F, op_copyVideoPage},
                /* 0x10 */
                {0x10, op_blitFramebuffer},
                {0x11, op_killThread},
                {0x12, op_drawString},
                {0x13, op_sub},
                /* 0x14 */
                {0x14, op_and},
                {0x15, op_or},
                {0x16, op_shl},
                {0x17, op_shr},
                /* 0x18 */
                {0x18, op_playSound},
                {0x19, op_updateMemList},
                {0x1A, op_playMusic}
            };
        }

        public void Init()
        {
            FastMode = false;
            _player.MarkVar = new Ptr<short>(_vmVariables, (int) ScriptVars.VmVariableMusMark);

            _vmVariables[0x54] = 0x81;
            _vmVariables[(int) ScriptVars.VmVariableRandomSeed] = 0;
            // bypass the protection
            _vmVariables[0xBC] = 0x10;
            _vmVariables[0xC6] = 0x80;
            _vmVariables[0xF2] = (short) (Engine.Instance.Settings.Game.Platform == Platform.Amiga ? 6000 : 4000);
            _vmVariables[0xDC] = 33;
        }

        public void InitForPart(ushort partId)
        {
            _player.Stop();
            _mixer.StopAll();

            //WTF is that ?
            _vmVariables[0xE4] = 0x14;

            _res.SetupPart(partId);

            //Set all thread to inactive (pc at 0xFFFF or 0xFFFE )
            for (int i = 0; i < NumDataFields; i++)
            {
                _threadsData[i].Set(0, (ushort) 0xFFFF, _threadsData[i].Length);
            }

            for (int i = 0; i < NumThreadFields; i++)
            {
                Array.Clear(_vmIsChannelActive[i], 0, _vmIsChannelActive[i].Length);
            }

            int firstThreadId = 0;
            _threadsData[PcOffset][firstThreadId] = 0;
        }

        public void CheckThreadRequests()
        {
            //Check if a part switch has been requested.
            if (_res.RequestedNextPart != 0)
            {
                InitForPart(_res.RequestedNextPart);
                _res.RequestedNextPart = 0;
            }


            // Check if a state update has been requested for any thread during the previous VM execution:
            //      - Pause
            //      - Jump

            // JUMP:
            // Note: If a jump has been requested, the jump destination is stored
            // in threadsData[REQUESTED_PC_OFFSET]. Otherwise threadsData[REQUESTED_PC_OFFSET] == 0xFFFF

            // PAUSE:
            // Note: If a pause has been requested it is stored in  vmIsChannelActive[REQUESTED_STATE][i]

            for (int threadId = 0; threadId < VmNumThreads; threadId++)
            {
                _vmIsChannelActive[CurrState][threadId] = _vmIsChannelActive[RequestedState][threadId];

                ushort n = _threadsData[RequestedPcOffset][threadId];

                if (n != VmNoSetvecRequested)
                {
                    _threadsData[PcOffset][threadId] = (ushort) (n == 0xFFFE ? VmInactiveThread : n);
                    _threadsData[RequestedPcOffset][threadId] = VmNoSetvecRequested;
                }
            }
        }

        public void Inp_updatePlayer()
        {
            _sys.ProcessEvents();

            if (_res.CurrentPartId == 0x3E89)
            {
                char c = _sys.Input.LastChar;
                if (c == 8 || /*c == 0xD |*/ c == 0 || c >= 'a' && c <= 'z')
                {
                    _vmVariables[(int) ScriptVars.VmVariableLastKeychar] = (short) (c & ~0x20);
                    _sys.Input.LastChar = (char) 0;
                }
            }

            short lr = 0;
            short m = 0;
            short ud = 0;

            if (_sys.Input.DirMask.HasFlag(Direction.Right))
            {
                lr = 1;
                m |= 1;
            }
            if (_sys.Input.DirMask.HasFlag(Direction.Left))
            {
                lr = -1;
                m |= 2;
            }
            if (_sys.Input.DirMask.HasFlag(Direction.Down))
            {
                ud = 1;
                m |= 4;
            }

            _vmVariables[(int) ScriptVars.VmVariableHeroPosUpDown] = ud;

            if (_sys.Input.DirMask.HasFlag(Direction.Up))
            {
                _vmVariables[(int) ScriptVars.VmVariableHeroPosUpDown] = -1;
            }

            if (_sys.Input.DirMask.HasFlag(Direction.Up))
            {
                // inpJump
                ud = -1;
                m |= 8;
            }

            _vmVariables[(int) ScriptVars.VmVariableHeroPosJumpDown] = ud;
            _vmVariables[(int) ScriptVars.VmVariableHeroPosLeftRight] = lr;
            _vmVariables[(int) ScriptVars.VmVariableHeroPosMask] = m;
            short button = 0;

            if (_sys.Input.Button)
            {
                // inpButton
                button = 1;
                m |= 0x80;
            }

            _vmVariables[(int) ScriptVars.VmVariableHeroAction] = button;
            _vmVariables[(int) ScriptVars.VmVariableHeroActionPosMask] = m;
        }

        public void HostFrame()
        {
            // Run the Virtual Machine for every active threads (one vm frame).
            // Inactive threads are marked with a thread instruction pointer set to 0xFFFF (VM_INACTIVE_THREAD).
            // A thread must feature a break opcode so the interpreter can move to the next thread.

            for (int threadId = 0; threadId < VmNumThreads; threadId++)
            {
                if (_vmIsChannelActive[CurrState][threadId] != 0)
                    continue;

                ushort n = _threadsData[PcOffset][threadId];

                if (n != VmInactiveThread)
                {
                    // Set the script pointer to the right location.
                    // script pc is used in executeThread in order
                    // to get the next opcode.
                    _scriptPtr = _res.SegBytecode + n;
                    _stackPtr = 0;

                    _gotoNextThread = false;
                    Debug(DebugLevels.DbgVm, "VirtualMachine::hostFrame() i=0x{0:X2} n=0x{1:X2} *p=0x{2:X2}", threadId,
                        n,
                        _scriptPtr.Value);
                    ExecuteThread();

                    //Since .pc is going to be modified by this next loop iteration, we need to save it.
                    _threadsData[PcOffset][threadId] = (ushort) (_scriptPtr.Offset - _res.SegBytecode.Offset);


                    Debug(DebugLevels.DbgVm, "VirtualMachine::hostFrame() i=0x{0:X2} pos=0x{1:X}", threadId,
                        _threadsData[PcOffset][threadId]);
                    if (_sys.Input.Quit)
                    {
                        break;
                    }
                }
            }
        }

        private static byte[][] CreateVmIsChannelActive()
        {
            var data = new byte[NumThreadFields][];
            for (int i = 0; i < NumThreadFields; i++)
            {
                data[i] = new byte[VmNumThreads];
            }
            return data;
        }

        private static ushort[][] CreateThreadsData()
        {
            var data = new ushort[NumDataFields][];
            for (int i = 0; i < NumDataFields; i++)
            {
                data[i] = new ushort[VmNumThreads];
            }
            return data;
        }

        private void ExecuteThread()
        {
            while (!_gotoNextThread)
            {
                byte opcode = FetchByte();

                // 1000 0000 is set
                if ((opcode & 0x80) != 0)
                {
                    ushort off = (ushort) (((opcode << 8) | FetchByte()) * 2);
                    _res.UseSegVideo2 = false;
                    short x = FetchByte();
                    short y = FetchByte();
                    short h = (short) (y - 199);
                    if (h > 0)
                    {
                        y = 199;
                        x += h;
                    }
                    Debug(DebugLevels.DbgVideo, "vid_opcd_0x80 : opcode=0x{0:X} off=0x{1:X} x={2} y={3}", opcode, off,
                        x, y);

                    // This switch the polygon database to "cinematic" and probably draws a black polygon
                    // over all the screen.
                    _video.SetDataBuffer(_res.SegCinematic, off);
                    _video.ReadAndDrawPolygon(ColorBlack, DefaultZoom, new Point(x, y));

                    continue;
                }

                // 0100 0000 is set
                if ((opcode & 0x40) != 0)
                {
                    short x, y;
                    ushort off = (ushort) (FetchWord() * 2);
                    x = FetchByte();

                    _res.UseSegVideo2 = false;

                    if (0 == (opcode & 0x20))
                    {
                        if (0 == (opcode & 0x10)) // 0001 0000 is set
                        {
                            x = (short) ((x << 8) | FetchByte());
                        }
                        else
                        {
                            x = _vmVariables[x];
                        }
                    }
                    else
                    {
                        if ((opcode & 0x10) != 0)
                        {
                            // 0001 0000 is set
                            x += 0x100;
                        }
                    }

                    y = FetchByte();

                    if (0 == (opcode & 8)) // 0000 1000 is set
                    {
                        if (0 == (opcode & 4))
                        {
                            // 0000 0100 is set
                            y = (short) ((y << 8) | FetchByte());
                        }
                        else
                        {
                            y = _vmVariables[y];
                        }
                    }

                    ushort zoom = FetchByte();

                    if (0 == (opcode & 2)) // 0000 0010 is set
                    {
                        if (0 == (opcode & 1)) // 0000 0001 is set
                        {
                            --_scriptPtr.Offset;
                            zoom = 0x40;
                        }
                        else
                        {
                            zoom = (ushort) _vmVariables[zoom];
                        }
                    }
                    else
                    {
                        if ((opcode & 1) != 0)
                        {
                            // 0000 0001 is set
                            _res.UseSegVideo2 = true;
                            --_scriptPtr.Offset;
                            zoom = 0x40;
                        }
                    }
                    Debug(DebugLevels.DbgVideo, "vid_opcd_0x40 : off=0x{0:X} x={1} y={2}", off, x, y);
                    _video.SetDataBuffer(_res.UseSegVideo2 ? _res.SegVideo2 : _res.SegCinematic, off);
                    _video.ReadAndDrawPolygon(0xFF, zoom, new Point(x, y));

                    continue;
                }


                if (opcode > 0x1A)
                {
                    Error("VirtualMachine::executeThread() ec=0x{0:X} invalid opcode=0x{1:X}", 0xFFF, opcode);
                }
                else
                {
                    if (_opcodeTable.ContainsKey(opcode))
                    {
                        _opcodeTable[opcode]();
                    }
                    else
                    {
                        Error("opcode {0:X2} not implemented", opcode);
                    }
                }
            }
        }

        private ushort FetchWord()
        {
            var value = _scriptPtr.ToUInt16BigEndian();
            _scriptPtr += 2;
            return value;
        }

        private byte FetchByte()
        {
            var value = _scriptPtr.Value;
            _scriptPtr.Offset++;
            return value;
        }

        private void op_movConst()
        {
            byte variableId = FetchByte();
            short value = (short) FetchWord();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_movConst(0x{0:X2}, {1})", variableId, value);
            _vmVariables[variableId] = value;
        }

        private void op_mov()
        {
            byte dstVariableId = FetchByte();
            byte srcVariableId = FetchByte();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_mov(0x{0:X2}, 0x{1:X2})", dstVariableId, srcVariableId);
            _vmVariables[dstVariableId] = _vmVariables[srcVariableId];
        }

        private void op_add()
        {
            byte dstVariableId = FetchByte();
            byte srcVariableId = FetchByte();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_add(0x{0:X2}, 0x{1:X2})", dstVariableId, srcVariableId);
            _vmVariables[dstVariableId] += _vmVariables[srcVariableId];
        }

        private void op_addConst()
        {
            if (_res.CurrentPartId == 0x3E86 && _scriptPtr == _res.SegBytecode + 0x6D48)
            {
                Warning("VirtualMachine::op_addConst() hack for non-stop looping gun sound bug");
                // the script 0x27 slot 0x17 doesn't stop the gun sound from looping, I 
                // don't really know why ; for now, let's play the 'stopping sound' like 
                // the other scripts do
                //  (0x6D43) jmp(0x6CE5)
                //  (0x6D46) break
                //  (0x6D47) VAR(6) += -50
                snd_playSound(0x5B, 1, 64, 1);
            }
            byte variableId = FetchByte();
            short value = (short) FetchWord();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_addConst(0x{0:X2}, {1})", variableId, value);
            _vmVariables[variableId] += value;
        }

        private void op_call()
        {
            ushort offset = FetchWord();
            byte sp = _stackPtr;

            Debug(DebugLevels.DbgVm, "VirtualMachine::op_call(0x{0:X})", offset);
            _scriptStackCalls[sp] = (ushort) (_scriptPtr.Offset - _res.SegBytecode.Offset);
            if (_stackPtr == 0xFF)
            {
                Error("VirtualMachine::op_call() ec=0x{0:X} stack overflow", 0x8F);
            }
            ++_stackPtr;
            _scriptPtr = _res.SegBytecode + offset;
        }

        private void op_ret()
        {
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_ret()");
            if (_stackPtr == 0)
            {
                Error("VirtualMachine::op_ret() ec=0x{0:X} stack underflow", 0x8F);
            }
            --_stackPtr;
            byte sp = _stackPtr;
            _scriptPtr = _res.SegBytecode + _scriptStackCalls[sp];
        }

        private void op_pauseThread()
        {
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_pauseThread()");
            _gotoNextThread = true;
        }

        private void op_jmp()
        {
            ushort pcOffset = FetchWord();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_jmp(0x{0:X2})", pcOffset);
            _scriptPtr = _res.SegBytecode + pcOffset;
        }

        private void op_setSetVect()
        {
            byte threadId = FetchByte();
            ushort pcOffsetRequested = FetchWord();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_setSetVect(0x{0:X}, 0x{1:X})", threadId, pcOffsetRequested);
            _threadsData[RequestedPcOffset][threadId] = pcOffsetRequested;
        }

        private void op_jnz()
        {
            byte i = FetchByte();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_jnz(0x{0:X2})", i);
            --_vmVariables[i];
            if (_vmVariables[i] != 0)
            {
                op_jmp();
            }
            else
            {
                FetchWord();
            }
        }

        private void op_condJmp()
        {
            byte opcode = FetchByte();
            var var = FetchByte();
            short b = _vmVariables[var];
            byte c = FetchByte();
            short a;

            if ((opcode & 0x80) != 0)
            {
                a = _vmVariables[c];
            }
            else if ((opcode & 0x40) != 0)
            {
                a = (short) (c * 256 + FetchByte());
            }
            else
            {
                a = c;
            }
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_condJmp({0}, 0x{1:X2}, 0x{2:X2})", opcode, b, a);

            // Check if the conditional value is met.
            bool expr = false;
            switch (opcode & 7)
            {
                case 0: // jz
                    expr = (b == a);
#if BYPASS_PROTECTION
                    if (_res.CurrentPartId == 16000)
                    {
                        //
                        // 0CB8: jmpIf(VAR(0x29) == VAR(0x1E) @0CD3)
                        // ...
                        //
                        if (var == 0x29 && (opcode & 0x80) != 0)
                        {
                            // 4 symbols
                            _vmVariables[0x29] = _vmVariables[0x1E];
                            _vmVariables[0x2A] = _vmVariables[0x1F];
                            _vmVariables[0x2B] = _vmVariables[0x20];
                            _vmVariables[0x2C] = _vmVariables[0x21];
                            // counters
                            _vmVariables[0x32] = 6;
                            _vmVariables[0x64] = 20;
                            Warning("Script::op_condJmp() bypassing protection");
                            expr = true;
                        }
                    }
#endif
                    break;
                case 1: // jnz
                    expr = (b != a);
                    break;
                case 2: // jg
                    expr = (b > a);
                    break;
                case 3: // jge
                    expr = (b >= a);
                    break;
                case 4: // jl
                    expr = (b < a);
                    break;
                case 5: // jle
                    expr = (b <= a);
                    break;
                default:
                    Warning("VirtualMachine::op_condJmp() invalid condition {0}", (opcode & 7));
                    break;
            }

            if (expr)
            {
                op_jmp();
            }
            else
            {
                FetchWord();
            }
        }

        private void op_setPalette()
        {
            ushort paletteId = FetchWord();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_changePalette({0})", paletteId);
            _video.PaletteIdRequested = (byte) (paletteId >> 8);
        }

        private void op_resetThread()
        {
            byte threadId = FetchByte();
            byte i = FetchByte();

            // FCS: WTF, this is cryptic as hell !!
            //int8_t n = (i & 0x3F) - threadId;  //0x3F = 0011 1111
            // The following is so much clearer

            //Make sure i within [0-VM_NUM_THREADS-1]
            i = (byte) (i & (VmNumThreads - 1));
            sbyte n = (sbyte) (i - threadId);

            if (n < 0)
            {
                Warning("VirtualMachine::op_resetThread() ec=0x{0:X} (n < 0)", 0x880);
                return;
            }
            ++n;
            byte a = FetchByte();

            Debug(DebugLevels.DbgVm, "VirtualMachine::op_resetThread({0}, {1}, {2})", threadId, i, a);

            if (a == 2)
            {
                var p = new Ptr<ushort>(_threadsData[RequestedPcOffset], threadId);
                while (n-- != 0)
                {
                    p.Value = 0xFFFE;
                    p.Offset++;
                }
            }
            else if (a < 2)
            {
                var p = new BytePtr(_vmIsChannelActive[RequestedState], threadId);
                while (n-- != 0)
                {
                    p.Value = a;
                    p.Offset++;
                }
            }
        }

        private void op_selectVideoPage()
        {
            byte frameBufferId = FetchByte();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_selectVideoPage({0})", frameBufferId);
            _video.ChangePagePtr1(frameBufferId);
        }

        private void op_fillVideoPage()
        {
            byte pageId = FetchByte();
            byte color = FetchByte();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_fillVideoPage({0}, {1})", pageId, color);
            _video.FillPage(pageId, color);
        }

        private void op_copyVideoPage()
        {
            byte srcPageId = FetchByte();
            byte dstPageId = FetchByte();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_copyVideoPage({0}, {1})", srcPageId, dstPageId);
            _video.CopyPage(srcPageId, dstPageId, _vmVariables[(int) ScriptVars.VmVariableScrollY]);
        }

        private void op_blitFramebuffer()
        {
            byte pageId = FetchByte();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_blitFramebuffer({0})", pageId);
            inp_handleSpecialKeys();

            //Nasty hack....was this present in the original assembly  ??!!
            if (_res.CurrentPartId == Resource.GamePartFirst && _vmVariables[0x67] == 1)
                _vmVariables[0xDC] = 0x21;

            if (!FastMode)
            {
                int delay = (int) (_sys.GetTimeStamp() - _lastTimeStamp);
                int timeToSleep = _vmVariables[(int) ScriptVars.VmVariablePauseSlices] * 20 - delay;

                // The bytecode will set vmVariables[VM_VARIABLE_PAUSE_SLICES] from 1 to 5
                // The virtual machine hence indicate how long the image should be displayed.

                //printf("vmVariables[VM_VARIABLE_PAUSE_SLICES]={0}\n",vmVariables[VM_VARIABLE_PAUSE_SLICES]);


                if (timeToSleep > 0)
                {
                    //	printf("Sleeping for={0}\n",timeToSleep);
                    _sys.Sleep(timeToSleep);
                }

                _lastTimeStamp = _sys.GetTimeStamp();
            }

            //WTF ?
            _vmVariables[0xF7] = 0;

            _video.UpdateDisplay(pageId);
        }

        private void inp_handleSpecialKeys()
        {
            if (_sys.Input.Pause)
            {
                if (_res.CurrentPartId != 16000 && _res.CurrentPartId != 16001)
                {
                    _sys.Input.Pause = false;
                    while (!_sys.Input.Pause && !_sys.Input.Quit)
                    {
                        _sys.ProcessEvents();
                        _sys.Sleep(50);
                    }
                }
                _sys.Input.Pause = false;
            }
            if (_sys.Input.Code)
            {
                _sys.Input.Code = false;
                if (_res.CurrentPartId != 16009 && _res.CurrentPartId != 16000)
                {
                    _res.RequestedNextPart = 16009;
                }
            }
            if (_vmVariables[0xC9] == 1)
            {
                // this happens on french/europeans versions when the user does not select
                // any symbols for the protection, the disassembly shows a simple 'hlt' here.
            }
        }

        private void op_killThread()
        {
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_killThread()");
            _scriptPtr = _res.SegBytecode + 0xFFFF;
            _gotoNextThread = true;
        }

        private void op_drawString()
        {
            ushort stringId = FetchWord();
            ushort x = FetchByte();
            ushort y = FetchByte();
            ushort color = FetchByte();

            Debug(DebugLevels.DbgVm, "VirtualMachine::op_drawString(0x{0:X}, {1}, {2}, {3})", stringId, x, y, color);

            _video.DrawString(color, x, y, stringId);
        }

        private void op_sub()
        {
            byte i = FetchByte();
            byte j = FetchByte();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_sub(0x{0:X2}, 0x{1:X2})", i, j);
            _vmVariables[i] -= _vmVariables[j];
        }

        private void op_and()
        {
            byte variableId = FetchByte();
            ushort n = FetchWord();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_and(0x{0:X2}, {1})", variableId, n);
            _vmVariables[variableId] = (short) (_vmVariables[variableId] & n);
        }

        private void op_or()
        {
            byte variableId = FetchByte();
            ushort value = FetchWord();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_or(0x{0:X2}, {1})", variableId, value);
            _vmVariables[variableId] = (short) (_vmVariables[variableId] | value);
        }

        private void op_shl()
        {
            byte variableId = FetchByte();
            ushort leftShiftValue = FetchWord();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_shl(0x{0:X2}, {1})", variableId, leftShiftValue);
            _vmVariables[variableId] = (short) (_vmVariables[variableId] << leftShiftValue);
        }

        private void op_shr()
        {
            byte variableId = FetchByte();
            ushort rightShiftValue = FetchWord();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_shr(0x{0:X2}, {1})", variableId, rightShiftValue);
            _vmVariables[variableId] = (short) (_vmVariables[variableId] >> rightShiftValue);
        }

        private void op_playSound()
        {
            ushort resourceId = FetchWord();
            byte freq = FetchByte();
            byte vol = FetchByte();
            byte channel = FetchByte();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_playSound(0x{0:X}, {1}, {2}, {3})", resourceId, freq, vol,
                channel);
            snd_playSound(resourceId, freq, vol, channel);
        }

        private void op_updateMemList()
        {
            ushort resourceId = FetchWord();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_updateMemList({0})", resourceId);

            if (resourceId == 0)
            {
                _player.Stop();
                _mixer.StopAll();
                _res.InvalidateRes();
            }
            else
            {
                _res.LoadPartsOrMemoryEntry(resourceId);
            }
        }

        private void op_playMusic()
        {
            ushort resNum = FetchWord();
            ushort delay = FetchWord();
            byte pos = FetchByte();
            Debug(DebugLevels.DbgVm, "VirtualMachine::op_playMusic(0x{0:X}, {1}, {2})", resNum, delay, pos);
            snd_playMusic(resNum, delay, pos);
        }

        private void snd_playMusic(ushort resNum, ushort delay, byte pos)
        {
            Debug(DebugLevels.DbgSnd, "snd_playMusic(0x{0:X}, {1}, {2})", resNum, delay, pos);

            if (resNum != 0)
            {
                _player.LoadSfxModule(resNum, delay, pos);
                _player.Start();
            }
            else if (delay != 0)
            {
                _player.SetEventsDelay(delay);
            }
            else
            {
                _player.Stop();
            }
        }

        private void snd_playSound(ushort resNum, byte freq, byte vol, byte channel)
        {
            Debug(DebugLevels.DbgSnd, "snd_playSound(0x{0:X}, {1}, {2}, {3})", resNum, freq, vol, channel);

            var me = _res.MemList[resNum];

            if (me.State != MemEntry.Loaded)
                return;


            if (vol == 0)
            {
                _mixer.StopChannel(channel);
            }
            else
            {
                var mc = new MixerChunk
                {
                    Data = me.BufPtr + 8,
                    Len = (ushort) (me.BufPtr.ToUInt16BigEndian() * 2),
                    LoopLen = (ushort) (me.BufPtr.ToUInt16BigEndian(2) * 2)
                };
                // skip header
                if (mc.LoopLen != 0)
                {
                    mc.LoopPos = mc.Len;
                }
                //assert(freq < 40);
                _mixer.PlayChannel((byte) (channel & 3), mc, FrequenceTable[freq], (byte) Math.Min((int) vol, 0x3F));
            }
        }

        public void SaveOrLoad(Serializer ser)
        {
            Entry[] entries =
            {
                Entry.Create(_vmVariables, 1),
                Entry.Create(_scriptStackCalls, 0x40, 1),
                Entry.Create(_threadsData, 2, 0x40, 1),
                Entry.Create(_vmIsChannelActive, 2, 0x40, 1),
            };
            ser.SaveOrLoadEntries(entries);
        }
    }
}