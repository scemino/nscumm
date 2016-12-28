//
//  AGOSEngine.Vga.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2016 scemino
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
using System.Linq;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        private readonly Item[] _objectArray = new Item[50];
        protected readonly Item[] _itemStore = new Item[50];
        protected readonly BytePtr[] _pathFindArray = new BytePtr[2 * 100];

        protected readonly ushort[] _shortText = new ushort[40];
        protected readonly ushort[] _longText = new ushort[40];
        protected readonly ushort[] _longSound = new ushort[40];

        public SIMONGameType GameType => _gd.ADGameDescription.gameType;
        public GameIds GameId => _gd.ADGameDescription.gameId;
        public Platform GamePlatform => _gd.Platform;
        public Language Language => _gd.Language;
        public GameFeatures Features => _gd.ADGameDescription.features;

        protected virtual void SetupVideoOpcodes(Action[] op)
        {
            SetupVideoOpcodesCore(op);
        }

        protected void SetupVideoOpcodesCore(Action[] op)
        {
            op[1] = vc1_fadeOut;
            op[2] = vc2_call;
            op[3] = vc3_loadSprite;
            op[4] = vc4_fadeIn;
            op[5] = vc5_ifEqual;
            op[6] = vc6_ifObjectHere;
            op[7] = vc7_ifObjectNotHere;
            op[8] = vc8_ifObjectIsAt;
            op[9] = vc9_ifObjectStateIs;
            op[10] = vc10_draw;
            op[12] = vc12_delay;
            op[13] = vc13_addToSpriteX;
            op[14] = vc14_addToSpriteY;
            op[15] = vc15_sync;
            op[16] = vc16_waitSync;
            op[18] = vc18_jump;
            op[20] = vc20_setRepeat;
            op[21] = vc21_endRepeat;
            op[23] = vc23_setPriority;
            op[24] = vc24_setSpriteXY;
            op[25] = vc25_halt_sprite;
            op[26] = vc26_setSubWindow;
            op[27] = vc27_resetSprite;
            op[29] = vc29_stopAllSounds;
            op[30] = vc30_setFrameRate;
            op[31] = vc31_setWindow;
            op[33] = vc33_setMouseOn;
            op[34] = vc34_setMouseOff;
            op[35] = vc35_clearWindow;
            op[36] = vc36_setWindowImage;
            op[38] = vc38_ifVarNotZero;
            op[39] = vc39_setVar;
            op[40] = vc40_scrollRight;
            op[41] = vc41_scrollLeft;
            op[42] = vc42_delayIfNotEQ;
            op[43] = vc43_ifBitSet;
            op[44] = vc44_ifBitClear;
            op[45] = vc45_setSpriteX;
            op[46] = vc46_setSpriteY;
            op[47] = vc47_addToVar;
            op[49] = vc49_setBit;
            op[50] = vc50_clearBit;
            op[51] = vc51_enableBox;
            op[52] = vc52_playSound;
            op[55] = vc55_moveBox;
        }

        private void SetupVgaOpcodes()
        {
            Array.Clear(_vga_opcode_table, 0, _vga_opcode_table.Length);

            switch (_gd.ADGameDescription.gameType)
            {
                case SIMONGameType.GType_PN:
                case SIMONGameType.GType_ELVIRA1:
                case SIMONGameType.GType_ELVIRA2:
                case SIMONGameType.GType_WW:
                case SIMONGameType.GType_SIMON1:
                case SIMONGameType.GType_SIMON2:
                case SIMONGameType.GType_FF:
                case SIMONGameType.GType_PP:
                    SetupVideoOpcodes(_vga_opcode_table);
                    break;
                default:
                    Error("setupVgaOpcodes: Unknown game");
                    break;
            }
        }

        // VGA Script parser
        private void RunVgaScript()
        {
            while (true)
            {
                uint opcode;

                if (DebugManager.Instance.IsDebugChannelEnabled(DebugLevels.kDebugVGAOpcode))
                {
                    if (_vcPtr != _vcGetOutOfCode)
                    {
                        DebugN("{0:D5} {1:D5}: {2:D5} {3:D4} ", _vgaTickCounter, _vcPtr.Offset - _curVgaFile1.Offset,
                            _vgaCurSpriteId,
                            _vgaCurZoneNum);
                        DumpVideoScript(_vcPtr, true);
                    }
                }

                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                    _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                {
                    opcode = _vcPtr.Value;
                    _vcPtr.Offset++;
                }
                else
                {
                    opcode = _vcPtr.ToUInt16BigEndian();
                    _vcPtr += 2;
                }

                if (opcode == 0)
                    return;

                if (opcode >= _numVideoOpcodes || _vga_opcode_table[opcode] == null)
                    Error("runVgaScript: Invalid VGA opcode '{0}' encountered", opcode);

                Debug($"runVgaScript {opcode} {_vga_opcode_table[opcode].Method.Name}");
                _vga_opcode_table[opcode]();
            }
        }

        private void DirtyBackGround()
        {
            Ptr<AnimTable> animTable = _screenAnim1;
            while (animTable.Value.srcPtr != BytePtr.Null)
            {
                if (animTable.Value.id == _vgaCurSpriteId && animTable.Value.zoneNum == _vgaCurZoneNum)
                {
                    animTable.Value.windowNum |= 0x8000;
                    break;
                }
                animTable.Offset++;
            }
        }

        protected Ptr<VgaSprite> FindCurSprite()
        {
            Ptr<VgaSprite> vsp = _vgaSprites;
            while (vsp.Value.id != 0)
            {
                if (vsp.Value.id == _vgaCurSpriteId && vsp.Value.zoneNum == _vgaCurZoneNum)
                    break;
                vsp.Offset++;
            }
            return vsp;
        }

        private bool IsSpriteLoaded(ushort id, ushort zoneNum)
        {
            Ptr<VgaSprite> vsp = _vgaSprites;
            while (vsp.Value.id != 0)
            {
                if (vsp.Value.id == id && vsp.Value.zoneNum == zoneNum)
                    return true;
                vsp.Offset++;
            }
            return false;
        }

        public bool GetBitFlag(int bit)
        {
            ushort bits = _bitArray[bit / 16];
            return (bits & (1 << (bit & 15))) != 0;
        }

        public void SetBitFlag(int bit, bool value)
        {
            ushort bits = _bitArray[bit / 16];
            _bitArray[bit / 16] = (ushort) ((bits & ~(1 << (bit & 15))) | ((value ? 1 : 0) << (bit & 15)));
        }

        protected int VcReadVarOrWord()
        {
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
            {
                return (int) VcReadNextWord();
            }
            short var = (short) VcReadNextWord();
            if (var < 0)
                var = (short) VcReadVar(-var);
            return var;
        }

        protected uint VcReadNextWord()
        {
            uint a = ReadUint16Wrapper(_vcPtr);
            _vcPtr += 2;
            return a;
        }

        private uint VcReadNextByte()
        {
            _vcPtr.Offset++;
            return _vcPtr[-1];
        }

        protected uint VcReadVar(int var)
        {
            System.Diagnostics.Debug.Assert(var < _numVars);
            return (ushort) _variableArrayPtr[var];
        }

        protected void VcWriteVar(int var, short value)
        {
            System.Diagnostics.Debug.Assert(var < _numVars);
            _variableArrayPtr[var] = value;
        }

        protected void vc1_fadeOut()
        {
            /* dummy opcode */
            _vcPtr += 6;
        }

        protected void vc2_call()
        {
            ushort num;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
            {
                num = (ushort) VcReadNextWord();
            }
            else
            {
                num = (ushort) VcReadVarOrWord();
            }

            var oldFile1 = _curVgaFile1;
            var oldFile2 = _curVgaFile2;

            SetImage(num, true);

            _curVgaFile1 = oldFile1;
            _curVgaFile2 = oldFile2;
        }

        protected void vc3_loadSprite()
        {
            ushort zoneNum, vgaSpriteId;

            ushort windowNum = (ushort) VcReadNextWord();
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 && windowNum == 3)
            {
                _window3Flag = 1;
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_FF ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                zoneNum = (ushort) VcReadNextWord();
                vgaSpriteId = (ushort) VcReadNextWord();
            }
            else
            {
                vgaSpriteId = (ushort) VcReadNextWord();
                zoneNum = (ushort) (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN ? 0 : vgaSpriteId / 100);
            }

            short x = (short) VcReadNextWord();
            short y = (short) VcReadNextWord();
            ushort palette = (ushort) VcReadNextWord();

            var oldFile1 = _curVgaFile1;
            Animate(windowNum, zoneNum, vgaSpriteId, x, y, palette, true);
            _curVgaFile1 = oldFile1;
        }

        protected void vc4_fadeIn()
        {
            /* dummy opcode */
            _vcPtr += 6;
        }

        protected void vc5_ifEqual()
        {
            ushort var;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                var = (ushort) VcReadVarOrWord();
            else
                var = (ushort) VcReadNextWord();

            ushort value = (ushort) VcReadNextWord();
            if (VcReadVar(var) != value)
                VcSkipNextInstruction();
        }

        protected void VcSkipNextInstruction()
        {
            ushort opcode;
            if (GameType == SIMONGameType.GType_FF ||
                GameType == SIMONGameType.GType_PP)
            {
                opcode = (ushort) VcReadNextByte();
                _vcPtr += opcodeParamLenFeebleFiles[opcode];
            }
            else if (GameType == SIMONGameType.GType_SIMON2)
            {
                opcode = (ushort) VcReadNextByte();
                _vcPtr += opcodeParamLenSimon2[opcode];
            }
            else if (GameType == SIMONGameType.GType_SIMON1)
            {
                opcode = (ushort) VcReadNextWord();
                _vcPtr += opcodeParamLenSimon1[opcode];
            }
            else if (GameType == SIMONGameType.GType_ELVIRA2 ||
                     GameType == SIMONGameType.GType_WW)
            {
                opcode = (ushort) VcReadNextWord();
                _vcPtr += opcodeParamLenWW[opcode];
            }
            else if (GameType == SIMONGameType.GType_ELVIRA1)
            {
                opcode = (ushort) VcReadNextWord();
                _vcPtr += opcodeParamLenElvira1[opcode];
            }
            else
            {
                opcode = (ushort) VcReadNextWord();
                _vcPtr += opcodeParamLenPN[opcode];
            }

            DebugC(DebugLevels.kDebugVGAOpcode, "; skipped\n");
        }

        private bool IfObjectHere(ushort a)
        {
            CHECK_BOUNDS(a, _objectArray);

            var item = _objectArray[a];
            if (item == null)
                return true;

            return Me().parent == item.parent;
        }

        protected void vc6_ifObjectHere()
        {
            if (!IfObjectHere((ushort) VcReadNextWord()))
            {
                VcSkipNextInstruction();
            }
        }

        protected void vc7_ifObjectNotHere()
        {
            if (IfObjectHere((ushort) VcReadNextWord()))
                VcSkipNextInstruction();
        }

        protected void vc8_ifObjectIsAt()
        {
            ushort a = (ushort) VcReadNextWord();
            ushort b = (ushort) VcReadNextWord();
            if (!IfObjectAt(a, b))
                VcSkipNextInstruction();
        }

        private bool IfObjectAt(ushort a, ushort b)
        {
            CHECK_BOUNDS(a, _objectArray);
            CHECK_BOUNDS(b, _objectArray);

            var item_a = _objectArray[a];
            var item_b = _objectArray[b];

            if (item_a == null || item_b == null)
                return true;

            return DerefItem(item_a.parent) == item_b;
        }

        protected void vc9_ifObjectStateIs()
        {
            ushort a = (ushort) VcReadNextWord();
            ushort b = (ushort) VcReadNextWord();
            if (!IfObjectState(a, b))
                VcSkipNextInstruction();
        }

        private bool IfObjectState(ushort a, ushort b)
        {
            CHECK_BOUNDS(a, _objectArray);

            var item = _objectArray[a];
            if (item == null)
                return true;
            return item.state == b;
        }

        protected BytePtr vc10_uncompressFlip(BytePtr src, ushort w, ushort h)
        {
            w *= 8;

            BytePtr dst;
            sbyte cur = -0x80;
            int wCur = w;

            BytePtr dstPtr = new BytePtr(_videoBuf1, w);

            do
            {
                dst = dstPtr;
                uint h_cur = h;

                if (cur == -0x80)
                {
                    cur = (sbyte) src.Value;
                    src.Offset++;
                }

                for (;;)
                {
                    if (cur >= 0)
                    {
                        /* rle_same */
                        var color = src.Value;
                        src.Offset++;
                        do
                        {
                            dst.Value = color;
                            dst += w;
                            if (--h_cur == 0)
                            {
                                if (--cur < 0)
                                    cur = -0x80;
                                else
                                    src.Offset--;
                                goto next_line;
                            }
                        } while (--cur >= 0);
                    }
                    else
                    {
                        /* rle_diff */
                        do
                        {
                            dst.Value = src.Value;
                            src.Offset++;
                            dst += w;
                            if (--h_cur == 0)
                            {
                                if (++cur == 0)
                                    cur = -0x80;
                                goto next_line;
                            }
                        } while (++cur != 0);
                    }
                    cur = (sbyte) src.Value;
                    src.Offset++;
                }
                next_line:
                dstPtr.Offset++;
            } while (--wCur != 0);

            var srcPtr = dstPtr = new BytePtr(_videoBuf1, w);

            do
            {
                dst = dstPtr;
                int i;
                for (i = 0; i != w; ++i)
                {
                    byte b = srcPtr[i];
                    b = (byte) ((b >> 4) | (b << 4));
                    dst.Offset--;
                    dst.Value = b;
                }

                srcPtr += w;
                dstPtr += w;
            } while (--h != 0);

            return _videoBuf1;
        }

        private BytePtr vc10_flip(BytePtr src, ushort w, ushort h)
        {
            if (Features.HasFlag(GameFeatures.GF_32COLOR))
            {
                w *= 16;
                var dstPtr = new BytePtr(_videoBuf1, w);

                do
                {
                    BytePtr dst = dstPtr;
                    for (var i = 0; i != w; ++i)
                    {
                        dst.Offset--;
                        dst.Value = src[i];
                    }

                    src += w;
                    dstPtr += w;
                } while (--h != 0);
            }
            else
            {
                w *= 8;
                var dstPtr = new BytePtr(_videoBuf1, w);

                do
                {
                    BytePtr dst = dstPtr;
                    for (var i = 0; i != w; ++i)
                    {
                        byte b = src[i];
                        b = (byte) ((b >> 4) | (b << 4));
                        dst.Offset--;
                        dst.Value = b;
                    }

                    src += w;
                    dstPtr += w;
                } while (--h != 0);
            }

            return _videoBuf1;
        }

        protected void vc10_draw()
        {
            var image = (short) VcReadNextWord();

            ushort palette = 0;
            if (GameType == SIMONGameType.GType_FF ||
                GameType == SIMONGameType.GType_PP)
            {
                palette = _vcPtr[0];
                _vcPtr += 2;
            }
            else if (GameType == SIMONGameType.GType_SIMON1 ||
                     GameType == SIMONGameType.GType_SIMON2)
            {
                palette = _vcPtr[1];
                _vcPtr += 2;
            }

            var x = (short) VcReadNextWord();
            var y = (short) VcReadNextWord();

            DrawFlags flags;
            if (GameType == SIMONGameType.GType_SIMON2 || GameType == SIMONGameType.GType_FF ||
                GameType == SIMONGameType.GType_PP)
            {
                flags = (DrawFlags) VcReadNextByte();
            }
            else
            {
                flags = (DrawFlags) VcReadNextWord();
            }

            DrawImageInit(image, palette, x, y, flags);
        }

        protected void vc11_clearPathFinder()
        {
            Array.Clear(_pathFindArray, 0, _pathFindArray.Length);
        }

        protected void DrawImageInit(short image, ushort palette, short x, short y, DrawFlags flags)
        {
            if (image == 0)
                return;

            Debug("drawImage_init({0},{1},{2},{3},{4})", image, palette, x, y, flags);

            int width, height;
            Vc10State state = new Vc10State();

            state.image = image;
            if (state.image < 0)
                state.image = (short) VcReadVar(-state.image);

            state.palette = (byte) (GameType == SIMONGameType.GType_PN ? 0 : palette * 16);
            state.paletteMod = 0;

            state.x = (short) (x - _scrollX);
            state.y = (short) (y - _scrollY);

            state.flags = flags;

            var src = _curVgaFile2 + state.image * 8;
            state.srcPtr = _curVgaFile2 + (int) ReadUint32Wrapper(src);
            if (GameType == SIMONGameType.GType_FF ||
                GameType == SIMONGameType.GType_PP)
            {
                width = src.ToUInt16(6);
                height = src.ToUInt16(4) & 0x7FFF;
                flags = (DrawFlags) src[5];
            }
            else
            {
                width = src.ToUInt16BigEndian(6) / 16;
                height = src[5];
                flags = (DrawFlags) src[4];
            }

            if (height == 0 || width == 0)
                return;

            if (DebugManager.Instance.IsDebugChannelEnabled(DebugLevels.kDebugImageDump))
                DumpSingleBitmap(_vgaCurZoneNum, state.image, state.srcPtr, width, height, state.palette);
            state.width = state.draw_width = (ushort) width; /* cl */
            state.height = state.draw_height = (ushort) height; /* ch */

            state.depack_cont = -0x80;

            state.x_skip = 0; /* colums to skip = bh */
            state.y_skip = 0; /* rows to skip   = bl */

            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_PLANAR))
            {
                if (GameType == SIMONGameType.GType_PN)
                {
                    state.srcPtr = ConvertImage(state,
                        state.flags.HasFlag(DrawFlags.kDFCompressed | DrawFlags.kDFCompressedFlip));
                }
                else
                    state.srcPtr = ConvertImage(state, flags.HasFlag(DrawFlags.kDFShaded));

                // converted planar clip is already uncompressed
                if (state.flags.HasFlag(DrawFlags.kDFCompressedFlip))
                {
                    state.flags &= ~DrawFlags.kDFCompressedFlip;
                    state.flags |= DrawFlags.kDFFlip;
                }
                if (state.flags.HasFlag(DrawFlags.kDFCompressed))
                {
                    state.flags &= ~DrawFlags.kDFCompressed;
                }
            }
            else if (GameType == SIMONGameType.GType_FF ||
                     GameType == SIMONGameType.GType_PP)
            {
                if (flags.HasFlag(DrawFlags.kDFShaded))
                {
                    state.flags |= DrawFlags.kDFCompressed;
                }
            }
            else
            {
                if (flags.HasFlag(DrawFlags.kDFShaded) && !state.flags.HasFlag(DrawFlags.kDFCompressedFlip))
                {
                    if (state.flags.HasFlag(DrawFlags.kDFFlip))
                    {
                        state.flags &= ~DrawFlags.kDFFlip;
                        state.flags |= DrawFlags.kDFCompressedFlip;
                    }
                    else
                    {
                        state.flags |= DrawFlags.kDFCompressed;
                    }
                }
            }

            uint maxWidth = (uint) (GameType == SIMONGameType.GType_FF ||
                                    GameType == SIMONGameType.GType_PP
                ? 640
                : 20);
            if ((GameType == SIMONGameType.GType_SIMON2 ||
                 GameType == SIMONGameType.GType_FF) && width > maxWidth)
            {
                HorizontalScroll(state);
                return;
            }
            if (GameType == SIMONGameType.GType_FF && height > 480)
            {
                VerticalScroll(state);
                return;
            }

            if (GameType != SIMONGameType.GType_FF &&
                GameType != SIMONGameType.GType_PP)
            {
                if (state.flags.HasFlag(DrawFlags.kDFCompressedFlip))
                {
                    state.srcPtr = vc10_uncompressFlip(state.srcPtr, (ushort) width, (ushort) height);
                }
                else if (state.flags.HasFlag(DrawFlags.kDFFlip))
                {
                    state.srcPtr = vc10_flip(state.srcPtr, (ushort) width, (ushort) height);
                }
            }

            DrawImage(state);
        }

        private void CheckOnStopTable()
        {
            Ptr<VgaSleepStruct> vfs = _onStopTable;
            while (vfs.Value.ident != 0)
            {
                if (vfs.Value.ident == _vgaCurSpriteId)
                {
                    var vsp = FindCurSprite();
                    Animate(vsp.Value.windowNum, vsp.Value.zoneNum, vfs.Value.id, vsp.Value.x, vsp.Value.y,
                        vsp.Value.palette, true);
                    var vfs_tmp = vfs;
                    do
                    {
                        vfs_tmp[0] = new VgaSleepStruct(vfs_tmp[1]);
                        vfs_tmp.Offset++;
                    } while (vfs_tmp.Value.ident != 0);
                }
                else
                {
                    vfs.Offset++;
                }
            }
        }

        private void vc11_onStop()
        {
            ushort id = (ushort) VcReadNextWord();

            Ptr<VgaSleepStruct> vfs = _onStopTable;
            while (vfs.Value.ident != 0)
                vfs.Offset++;

            vfs.Value.ident = _vgaCurSpriteId;
            vfs.Value.codePtr = _vcPtr;
            vfs.Value.id = id;
            vfs.Value.zoneNum = _vgaCurZoneNum;
        }

        protected void vc12_delay()
        {
            ushort num;

            if (GameType == SIMONGameType.GType_FF ||
                GameType == SIMONGameType.GType_PP)
            {
                num = (ushort) VcReadNextByte();
            }
            else if (GameType == SIMONGameType.GType_SIMON2)
            {
                num = (ushort) (VcReadNextByte() * _frameCount);
            }
            else
            {
                num = (ushort) (VcReadVarOrWord() * _frameCount);
            }

            num += _vgaBaseDelay;

            AddVgaEvent(num, EventType.ANIMATE_EVENT, _vcPtr, _vgaCurSpriteId, _vgaCurZoneNum);
            _vcPtr = _vcGetOutOfCode;
        }

        protected void vc13_addToSpriteX()
        {
            var vsp = FindCurSprite();
            vsp.Value.x += (short) VcReadNextWord();

            vsp.Value.windowNum |= 0x8000;
            DirtyBackGround();
            _vgaSpriteChanged++;
        }

        protected void vc14_addToSpriteY()
        {
            var vsp = FindCurSprite();
            vsp.Value.y += (short) VcReadNextWord();

            vsp.Value.windowNum |= 0x8000;
            DirtyBackGround();
            _vgaSpriteChanged++;
        }

        protected void vc15_sync()
        {
            Ptr<VgaSleepStruct> vfs = _waitSyncTable;
            ushort id;

            if (GameType == SIMONGameType.GType_PN)
                id = _vgaCurSpriteId;
            else
                id = (ushort) VcReadNextWord();

            while (vfs.Value.ident != 0)
            {
                if (vfs.Value.ident == id)
                {
                    AddVgaEvent(_vgaBaseDelay, EventType.ANIMATE_EVENT, vfs.Value.codePtr, vfs.Value.id,
                        vfs.Value.zoneNum);
                    var vfsTmp = vfs;
                    do
                    {
                        vfsTmp[0] = new VgaSleepStruct(vfsTmp[1]);
                        vfsTmp.Offset++;
                    } while (vfsTmp.Value.ident != 0);
                }
                else
                {
                    vfs.Offset++;
                }
            }

            _lastVgaWaitFor = id;
            // Clear a wait event
            if (id == VgaWaitFor)
                VgaWaitFor = 0;
        }

        protected void vc16_waitSync()
        {
            var vfs = _waitSyncTable.FirstOrDefault(o => o.ident == 0);
            vfs.ident = (ushort) VcReadNextWord();
            vfs.codePtr = _vcPtr;
            vfs.id = _vgaCurSpriteId;
            vfs.zoneNum = _vgaCurZoneNum;

            _vcPtr = _vcGetOutOfCode;
        }

        private void CheckWaitEndTable()
        {
            var vfs = new Ptr<VgaSleepStruct>(_waitEndTable);
            while (vfs.Value.ident != 0)
            {
                if (vfs.Value.ident == _vgaCurSpriteId)
                {
                    AddVgaEvent(_vgaBaseDelay, EventType.ANIMATE_EVENT, vfs.Value.codePtr, vfs.Value.id,
                        vfs.Value.zoneNum);
                    var vfs_tmp = vfs;
                    do
                    {
                        vfs_tmp[0] = new VgaSleepStruct(vfs_tmp[1]);
                        vfs_tmp.Offset++;
                    } while (vfs_tmp.Value.ident != 0);
                }
                else
                {
                    vfs.Offset++;
                }
            }
        }

        protected void vc17_setPathfinderItem()
        {
            ushort a = (ushort) VcReadNextWord();
            _pathFindArray[a - 1] = _vcPtr;

            int end = (GameType == SIMONGameType.GType_FF ||
                       GameType == SIMONGameType.GType_PP)
                ? 9999
                : 999;
            while (ReadUint16Wrapper(_vcPtr) != end)
                _vcPtr += 4;
            _vcPtr += 2;
        }

        protected void vc17_waitEnd()
        {
            ushort id = (ushort) VcReadNextWord();
            ushort zoneNum = (ushort) (GameType == SIMONGameType.GType_PN ? 0 : id / 100);

            var vfs = new Ptr<VgaSleepStruct>(_waitEndTable);
            while (vfs.Value.ident != 0)
                vfs.Offset++;

            if (IsSpriteLoaded(id, zoneNum))
            {
                vfs.Value.ident = id;
                vfs.Value.codePtr = _vcPtr;
                vfs.Value.id = _vgaCurSpriteId;
                vfs.Value.zoneNum = _vgaCurZoneNum;
                _vcPtr = _vcGetOutOfCode;
            }
        }

        protected void vc18_jump()
        {
            var offs = (short) VcReadNextWord();
            _vcPtr += offs;
        }

        protected void vc19_loop()
        {
            var bb = _curVgaFile1;
            var b = _curVgaFile1 + bb.ToUInt16BigEndian(10);
            b.Offset += 20;

            var header = new VgaFile1HeaderCommon(b);
            var count = ScummHelper.SwapBytes(header.animationCount);
            b = bb + ScummHelper.SwapBytes(header.animationTable);

            var header2 = new AnimationHeaderWw(b);
            while (count-- != 0)
            {
                if (ScummHelper.SwapBytes(header2.id) == _vgaCurSpriteId)
                    break;
                header2.Pointer += AnimationHeaderWw.Size;
            }
            System.Diagnostics.Debug.Assert(ScummHelper.SwapBytes(header2.id) == _vgaCurSpriteId);

            _vcPtr = _curVgaFile1 + ScummHelper.SwapBytes(header2.scriptOffs);
        }

        protected void vc20_setRepeat()
        {
            // Sets counter used by the endRepeat opcode below.
            ushort a = (ushort) VcReadNextWord();
            _vcPtr.WriteUInt16(0, a);
            _vcPtr += 2;
        }

        protected void vc21_endRepeat()
        {
            short a = (short) VcReadNextWord();
            var tmp = _vcPtr + a;
            if (GameType == SIMONGameType.GType_SIMON2 ||
                GameType == SIMONGameType.GType_FF ||
                GameType == SIMONGameType.GType_PP)
                tmp += 3;
            else
                tmp += 4;

            var val = tmp.ToUInt16();
            if (val != 0)
            {
                // Decrement counter
                tmp.WriteUInt16(0, (ushort) (val - 1));
                _vcPtr = tmp + 2;
            }
        }

        protected virtual void vc22_setPalette()
        {
            ushort num;

            ushort b = (ushort) VcReadNextWord();

            // PC EGA version of Personal Nightmare uses standard EGA palette
            if (GameType == SIMONGameType.GType_PN && (Features.HasFlag(GameFeatures.GF_EGA)))
                return;

            num = 16;

            Ptr<Color> palptr = DisplayPalette;
            _bottomPalette = true;

            if (GameType == SIMONGameType.GType_PN)
            {
                if (b > 128)
                {
                    b -= 128;
                    palptr.Offset += 16;
                }
            }
            else if (GameType == SIMONGameType.GType_ELVIRA1)
            {
                if (b >= 1000)
                {
                    b -= 1000;
                    _bottomPalette = false;
                }
                else
                {
                    Color[] extraColors =
                    {
                        Color.FromRgb(40, 0, 0), Color.FromRgb(24, 24, 16), Color.FromRgb(48, 48, 40),
                        Color.FromRgb(0, 0, 0), Color.FromRgb(16, 0, 0), Color.FromRgb(8, 8, 0),
                        Color.FromRgb(48, 24, 0), Color.FromRgb(56, 40, 0), Color.FromRgb(0, 0, 24),
                        Color.FromRgb(8, 16, 24), Color.FromRgb(24, 32, 40), Color.FromRgb(16, 24, 0),
                        Color.FromRgb(24, 8, 0), Color.FromRgb(16, 16, 0), Color.FromRgb(40, 40, 32),
                        Color.FromRgb(32, 32, 24), Color.FromRgb(40, 0, 0), Color.FromRgb(24, 24, 16),
                        Color.FromRgb(48, 48, 40)
                    };

                    num = 13;

                    for (int i = 0; i < 19; i++)
                    {
                        var c = extraColors[i];
                        palptr[13 + i] = Color.FromRgb(c.R * 4, c.G * 4, c.B * 4);
                    }
                }
            }

            if (GameType == SIMONGameType.GType_ELVIRA2 && GamePlatform == Platform.AtariST)
            {
                // Custom palette used for icon area
                palptr.Offset += (13 * 16);
                for (var c = 0; c < 16; c++)
                {
                    palptr[c] = Color.FromRgb(
                        iconPalette[c * 3 + 0] * 2,
                        iconPalette[c * 3 + 1] * 2,
                        iconPalette[c * 3 + 2] * 2);
                }
                palptr = DisplayPalette;
            }

            var offs = _curVgaFile1 + _curVgaFile1.ToUInt16BigEndian(6);
            var src = offs + b * 32;

            do
            {
                ushort color = src.ToUInt16BigEndian();
                palptr.Value = Color.FromRgb(((color & 0xf00) >> 8) * 32,
                    ((color & 0x0f0) >> 4) * 32,
                    ((color & 0x00f) >> 0) * 32);

                palptr.Offset++;
                src += 2;
            } while (--num != 0);

            _paletteFlag = 2;
            _vgaSpriteChanged++;
        }

        protected void vc23_setPriority()
        {
            var vsp = FindCurSprite();
            var pri = (ushort) VcReadNextWord();

            if (vsp.Value.id == 0)
                return;

            var bak = new VgaSprite(vsp.Value);
            bak.priority = pri;
            bak.windowNum |= 0x8000;

            var vus2 = vsp;

            if (vsp != _vgaSprites && pri < vsp[-1].priority)
            {
                do
                {
                    vsp.Offset--;
                } while (vsp != _vgaSprites && pri < vsp[-1].priority);
                do
                {
                    vus2[0] = new VgaSprite(vus2[-1]);
                    vus2.Offset--;
                } while (vus2.Offset != vsp.Offset);
                vus2[0] = new VgaSprite(bak);
            }
            else if (vsp[1].id != 0 && pri >= vsp[1].priority)
            {
                do
                {
                    vsp.Offset++;
                } while (vsp[1].id != 0 && pri >= vsp[1].priority);
                do
                {
                    vus2[0] = new VgaSprite(vus2[1]);
                    vus2.Offset++;
                } while (vus2.Offset != vsp.Offset);
                vus2[0] = new VgaSprite(bak);
            }
            else
            {
                vsp.Value.priority = pri;
            }
            _vgaSpriteChanged++;
        }

        protected void vc24_setSpriteXY()
        {
            var vsp = FindCurSprite();

            if (GameType == SIMONGameType.GType_ELVIRA2)
            {
                vsp.Value.image = (short) VcReadNextWord();
            }
            else
            {
                vsp.Value.image = (short) VcReadVarOrWord();
            }

            vsp.Value.x += (short) VcReadNextWord();
            vsp.Value.y += (short) VcReadNextWord();
            if (GameType == SIMONGameType.GType_SIMON2 ||
                GameType == SIMONGameType.GType_FF ||
                GameType == SIMONGameType.GType_PP)
            {
                vsp.Value.flags = (DrawFlags) VcReadNextByte();
            }
            else
            {
                vsp.Value.flags = (DrawFlags) VcReadNextWord();
            }

            vsp.Value.windowNum |= 0x8000;
            DirtyBackGround();
            _vgaSpriteChanged++;
        }

        protected void vc25_halt_sprite()
        {
            CheckWaitEndTable();
            CheckOnStopTable();

            var vsp = FindCurSprite();
            while (vsp.Value.id != 0)
            {
                vsp[0] = new VgaSprite(vsp[1]);
                vsp.Offset++;
            }
            _vcPtr = _vcGetOutOfCode;

            DirtyBackGround();
            _vgaSpriteChanged++;
        }

        protected void vc26_setSubWindow()
        {
            var @as = new Ptr<ushort>(VideoWindows, (int) (VcReadNextWord() * 4)); // number
            @as[0] = (ushort) VcReadNextWord(); // x
            @as[1] = (ushort) VcReadNextWord(); // y
            @as[2] = (ushort) VcReadNextWord(); // width
            @as[3] = (ushort) VcReadNextWord(); // height
        }

        protected void vc27_resetSprite()
        {
            _videoLockOut |= 8;

            _lastVgaWaitFor = 0;

            VgaSprite bak = new VgaSprite();
            Ptr<VgaSprite> vsp = _vgaSprites;
            while (vsp.Value.id != 0)
            {
                // For animated heart in Elvira 2
                if (GameType == SIMONGameType.GType_ELVIRA2 && vsp.Value.id == 100)
                {
                    bak = new VgaSprite(vsp.Value);
                }
                vsp.Value.id = 0;
                vsp.Offset++;
            }

            if (bak.id != 0)
            {
                _vgaSprites[0] = new VgaSprite(bak);
            }

            Ptr<VgaSleepStruct> vfs = _waitEndTable;
            while (vfs.Value.ident != 0)
            {
                vfs.Value.ident = 0;
                vfs.Offset++;
            }

            vfs = _waitSyncTable;
            while (vfs.Value.ident != 0)
            {
                vfs.Value.ident = 0;
                vfs.Offset++;
            }

            vfs = _onStopTable;
            while (vfs.Value.ident != 0)
            {
                vfs.Value.ident = 0;
                vfs.Offset++;
            }

            Ptr<VgaTimerEntry> vte = _vgaTimerList;
            while (vte.Value.delay != 0)
            {
                // Skip the animateSprites event in earlier games
                if (vte.Value.type == EventType.ANIMATE_INT)
                {
                    vte.Offset++;
                    // For animated heart in Elvira 2
                }
                else if (GameType == SIMONGameType.GType_ELVIRA2 && vte.Value.id == 100)
                {
                    vte.Offset++;
                }
                else
                {
                    var vte2 = vte;
                    while (vte2.Value.delay != 0)
                    {
                        vte2.Value = new VgaTimerEntry(vte2[1]);
                        vte2.Offset++;
                    }
                }
            }

            if ((_videoLockOut & 0x20) != 0)
            {
                Ptr<AnimTable> animTable = _screenAnim1;
                while (animTable.Value.srcPtr != BytePtr.Null)
                {
                    animTable.Value.srcPtr = BytePtr.Null;
                    animTable.Offset++;
                }
            }

            if (GameType == SIMONGameType.GType_SIMON2 || GameType == SIMONGameType.GType_FF ||
                GameType == SIMONGameType.GType_PP)
                VcWriteVar(254, 0);

            // Stop any OmniTV video that is currently been played
            if (GameType == SIMONGameType.GType_FF || GameType == SIMONGameType.GType_PP)
                SetBitFlag(42, true);

            _videoLockOut = (ushort) (_videoLockOut & ~8);
        }

        protected void vc28_playSFX()
        {
            ushort sound = (ushort) VcReadNextWord();
            ushort chans = (ushort) VcReadNextWord();
            ushort freq = (ushort) VcReadNextWord();
            ushort flags = (ushort) VcReadNextWord();
            Debug(0, "vc28_playSFX: (sound {0}, channels {1}, frequency {2}, flags {3})", sound, chans, freq, flags);

            LoadSound(sound, freq, (SoundTypeFlags) flags);
        }

        protected void vc29_stopAllSounds()
        {
            if (GameType != SIMONGameType.GType_PP)
                _sound.StopVoice();

            _sound.StopAllSfx();
        }

        protected void vc30_setFrameRate()
        {
            _frameCount = (ushort) VcReadNextWord();
        }

        protected void vc31_setWindow()
        {
            _windowNum = (ushort) VcReadNextWord();
        }

        protected void vc32_copyVar()
        {
            ushort a = (ushort) VcReadVar((int) VcReadNextWord());
            VcWriteVar((int) VcReadNextWord(), (short) a);
        }

        protected void vc32_saveScreen()
        {
            if (GameType == SIMONGameType.GType_PN)
            {
                LockScreen(screen =>
                {
                    var dst = BackGround;
                    var src = screen.Pixels;
                    for (int i = 0; i < _screenHeight; i++)
                    {
                        Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, _screenWidth);
                        dst += _backGroundBuf.Pitch;
                        src += screen.Pitch;
                    }
                });
            }
            else
            {
                ushort xoffs = (ushort) (VideoWindows[4 * 4 + 0] * 16);
                ushort yoffs = VideoWindows[4 * 4 + 1];
                ushort width = (ushort) (VideoWindows[4 * 4 + 2] * 16);
                ushort height = VideoWindows[4 * 4 + 3];

                var dst = _backGroundBuf.GetBasePtr(xoffs, yoffs);
                var src = _window4BackScn.Pixels;
                ushort srcWidth = (ushort) (VideoWindows[4 * 4 + 2] * 16);
                for (; height > 0; height--)
                {
                    Array.Copy(src.Data, src.Offset, dst.Data, dst.Offset, width);
                    dst += _backGroundBuf.Pitch;
                    src += srcWidth;
                }
            }
        }

        protected void vc33_setMouseOn()
        {
            if (_mouseHideCount == 0) return;

            _mouseHideCount = 1;
            if (GameType == SIMONGameType.GType_ELVIRA2 || GameType == SIMONGameType.GType_WW)
            {
                // Set mouse palette
                DisplayPalette[65] = Color.FromRgb(48 * 4, 48 * 4, 48 * 4);
                _paletteFlag = 1;
            }
            MouseOn();
        }

        protected void vc34_setMouseOff()
        {
            MouseOff();
            _mouseHideCount = 200;
            _leftButtonDown = false;
        }

        private void ClearVideoBackGround(ushort num, ushort color)
        {
            var vlut = new Ptr<ushort>(VideoWindows, num * 4);
            var dst = _backGroundBuf.GetBasePtr(vlut[0] * 16, vlut[1]);

            for (int h = 0; h < vlut[3]; h++)
            {
                dst.Data.Set(dst.Offset, (byte) color, vlut[2] * 16);
                dst.Offset += _backGroundBuf.Pitch;
            }
        }

        protected virtual void ClearVideoWindow(ushort num, ushort color)
        {
            if (GameType == SIMONGameType.GType_ELVIRA1)
            {
                if (num == 2 || num == 6)
                    return;
            }
            else if (GameType == SIMONGameType.GType_ELVIRA2 ||
                     GameType == SIMONGameType.GType_WW)
            {
                if (num != 4 && num < 10)
                    return;
            }
            else if (GameType == SIMONGameType.GType_SIMON1)
            {
                if (num != 4)
                    return;
            }

            if (GameType == SIMONGameType.GType_ELVIRA1 && num == 3)
            {
                LockScreen(screen =>
                {
                    var dst = screen.Pixels;
                    for (int i = 0; i < _screenHeight; i++)
                    {
                        dst.Data.Set(dst.Offset, (byte) color, _screenWidth);
                        dst += screen.Pitch;
                    }
                });
            }
            else
            {
                var vlut = new Ptr<ushort>(VideoWindows, num * 4);
                ushort xoffs = (ushort) ((vlut[0] - VideoWindows[16]) * 16);
                ushort yoffs = (ushort) (vlut[1] - VideoWindows[17]);
                ushort dstWidth = (ushort) (VideoWindows[18] * 16);
// TODO: Is there any known connection between dstWidth and the pitch
// of the _window4BackScn Surface? If so, we might be able to pass
// yoffs as proper y parameter to getBasePtr.
                var dst = _window4BackScn.GetBasePtr(xoffs, 0) + yoffs * dstWidth;

                SetMoveRect(0, 0, (ushort) (vlut[2] * 16), vlut[3]);

                for (uint h = 0; h < vlut[3]; h++)
                {
                    dst.Data.Set(dst.Offset, (byte) color, vlut[2] * 16);
                    dst.Offset += dstWidth;
                }

                _window4Flag = 1;
            }
        }

        protected void vc35_clearWindow()
        {
            ushort num = (ushort) VcReadNextWord();
            ushort color = (ushort) VcReadNextWord();

            // Clear video background
            if (GameType == SIMONGameType.GType_ELVIRA1)
            {
                if (num == 2 || num == 6)
                    return;
            }
            else if (GameType == SIMONGameType.GType_ELVIRA2 || GameType == SIMONGameType.GType_WW)
            {
                if (num != 4 && num < 10)
                    return;
            }
            else if (GameType == SIMONGameType.GType_SIMON1)
            {
                if (num != 4)
                    return;
            }

            // Clear video window
            ClearVideoWindow(num, color);
            ClearVideoBackGround(num, color);
            _vgaSpriteChanged++;
        }

        protected virtual void vc36_setWindowImage()
        {
            _displayFlag = 0;
            ushort vga_res = (ushort) VcReadNextWord();
            ushort windowNum = (ushort) VcReadNextWord();
            SetWindowImage(windowNum, vga_res);
        }

        protected void vc37_pokePalette()
        {
            ushort offs = (ushort) VcReadNextWord();
            ushort color = (ushort) VcReadNextWord();

            // PC EGA version of Personal Nightmare uses standard EGA palette
            if (GameType == SIMONGameType.GType_PN && Features.HasFlag(GameFeatures.GF_EGA))
                return;

            var palptr = new Ptr<Color>(DisplayPalette, offs);
            palptr[0] = Color.FromRgb(
                ((color & 0xf00) >> 8) * 32,
                ((color & 0x0f0) >> 4) * 32,
                ((color & 0x00f) >> 0) * 32);

            if ((_videoLockOut & 0x20) == 0)
            {
                _paletteFlag = 1;
                _displayFlag++;
            }
        }

        protected void vc37_addToSpriteY()
        {
            Ptr<VgaSprite> vsp = FindCurSprite();
            vsp.Value.y = (short) (vsp.Value.y + VcReadVar((int) VcReadNextWord()));

            vsp.Value.windowNum |= 0x8000;
            DirtyBackGround();
            _vgaSpriteChanged++;
        }

        protected void vc38_ifVarNotZero()
        {
            ushort var;
            if (GameType == SIMONGameType.GType_PP)
                var = (ushort) VcReadVarOrWord();
            else
                var = (ushort) VcReadNextWord();

            if (VcReadVar(var) == 0)
                VcSkipNextInstruction();
        }

        protected void vc39_setVar()
        {
            ushort var;
            if (GameType == SIMONGameType.GType_PP)
                var = (ushort) VcReadVarOrWord();
            else
                var = (ushort) VcReadNextWord();

            short value = (short) VcReadNextWord();
            VcWriteVar(var, value);
        }

        protected void vc40_scrollRight()
        {
            ushort var = (ushort) VcReadNextWord();
            short value = (short) (VcReadVar(var) + VcReadNextWord());

            if (GameType == SIMONGameType.GType_SIMON2 && var == 15 && !GetBitFlag(80))
            {
                if ((_scrollCount < 0) || (_scrollCount == 0 && _scrollFlag == 0))
                {
                    _scrollCount = 0;
                    if (value - _scrollX >= 30)
                    {
                        _scrollCount = (short) Math.Min(20, _scrollXMax - _scrollX);
                        AddVgaEvent(6, EventType.SCROLL_EVENT, BytePtr.Null, 0, 0);
                    }
                }
            }

            VcWriteVar(var, value);
        }

        protected void vc41_scrollLeft()
        {
            ushort var = (ushort) VcReadNextWord();
            short value = (short) (VcReadVar(var) - VcReadNextWord());

            if (GameType == SIMONGameType.GType_SIMON2 && var == 15 && !GetBitFlag(80))
            {
                if ((_scrollCount > 0) || (_scrollCount == 0 && _scrollFlag == 0))
                {
                    _scrollCount = 0;
                    if ((ushort) (value - _scrollX) < 11)
                    {
                        _scrollCount = (short) -Math.Min(20, (int) _scrollX);
                        AddVgaEvent(6, EventType.SCROLL_EVENT, BytePtr.Null, 0, 0);
                    }
                }
            }

            VcWriteVar(var, value);
        }

        protected void vc42_delayIfNotEQ()
        {
            ushort val = (ushort) VcReadVar((int) VcReadNextWord());
            if (val != VcReadNextWord())
            {
                AddVgaEvent((ushort) (_frameCount + 1), EventType.ANIMATE_EVENT, _vcPtr - 4, _vgaCurSpriteId,
                    _vgaCurZoneNum);
                _vcPtr = _vcGetOutOfCode;
            }
        }

        private void vc43_ifBitSet()
        {
            if (!GetBitFlag((int) VcReadNextWord()))
            {
                VcSkipNextInstruction();
            }
        }

        private void vc44_ifBitClear()
        {
            if (GetBitFlag((int) VcReadNextWord()))
            {
                VcSkipNextInstruction();
            }
        }

        private void vc45_setSpriteX()
        {
            var vsp = FindCurSprite();
            vsp.Value.x = (short) VcReadVar((int) VcReadNextWord());

            vsp.Value.windowNum |= 0x8000;
            DirtyBackGround();
            _vgaSpriteChanged++;
        }

        private void vc46_setSpriteY()
        {
            var vsp = FindCurSprite();
            vsp.Value.y = (short) VcReadVar((int) VcReadNextWord());

            vsp.Value.windowNum |= 0x8000;
            DirtyBackGround();
            _vgaSpriteChanged++;
        }

        private void vc47_addToVar()
        {
            ushort var = (ushort) VcReadNextWord();
            VcWriteVar(var, (short) (VcReadVar(var) + VcReadVar((int) VcReadNextWord())));
        }

        protected virtual void vc48_setPathFinder()
        {
            ushort a = (ushort) _variableArrayPtr[12];
            var p = _pathFindArray[a - 1];

            int b = (ushort) _variableArray[13];
            p.Offset += (b * 2 + 1) * 2;
            int c = _variableArray[14];

            int step = 2;
            if (c < 0)
            {
                c = -c;
                step = -2;
            }

            var vp = new Ptr<short>(_variableArray, 20);

            do
            {
                int y2 = ReadUint16Wrapper(p);
                p.Offset += step * 2;
                var y1 = ReadUint16Wrapper(p) - y2;

                vp[0] = (short) (y1 / 2);
                vp[1] = (short) (y1 - y1 / 2);

                vp.Offset += 2;
            } while (--c != 0);
        }

        private void vc49_setBit()
        {
            ushort bit = (ushort) VcReadNextWord();
            if (GameType == SIMONGameType.GType_FF && bit == 82)
            {
                _variableArrayPtr = _variableArray2;
            }
            SetBitFlag(bit, true);
        }

        private void vc50_clearBit()
        {
            ushort bit = (ushort) VcReadNextWord();
            if (GameType == SIMONGameType.GType_FF && bit == 82)
            {
                _variableArrayPtr = _variableArray;
            }
            SetBitFlag(bit, false);
        }

        private void vc51_enableBox()
        {
            EnableBox((int) VcReadNextWord());
        }

        private void vc55_moveBox()
        {
            ushort id = (ushort) VcReadNextWord();
            short x = (short) VcReadNextWord();
            short y = (short) VcReadNextWord();

            foreach (var ha in _hitAreas)
            {
                if (ha.id == id)
                {
                    ha.x = (ushort) (ha.x + x);
                    ha.y = (ushort) (ha.y + y);
                    break;
                }
            }

            _needHitAreaRecalc++;
        }

        protected void vc59_ifSpeech()
        {
            if (_sound.IsVoiceActive)
                VcSkipNextInstruction();
        }

        protected void vc61_setMaskImage()
        {
            var vsp = FindCurSprite();

            vsp.Value.image = (short) VcReadVarOrWord();
            vsp.Value.x = (short) (vsp.Value.x + VcReadNextWord());
            vsp.Value.y = (short) (vsp.Value.y + VcReadNextWord());
            vsp.Value.flags = DrawFlags.kDFMasked | DrawFlags.kDFSkipStoreBG;

            vsp.Value.windowNum |= 0x8000;
            DirtyBackGround();
            _vgaSpriteChanged++;
        }

        protected void vc62_fastFadeOut()
        {
            vc29_stopAllSounds();

            if (!_fastFadeOutFlag)
            {
                int fadeSize, fadeCount;

                _fastFadeCount = 256;
                if (GameType == SIMONGameType.GType_SIMON1 ||
                    GameType == SIMONGameType.GType_SIMON2)
                {
                    if (_windowNum == 4)
                        _fastFadeCount = 208;
                }

                if (GameType == SIMONGameType.GType_FF ||
                    GameType == SIMONGameType.GType_PP)
                {
                    if (GameType == SIMONGameType.GType_FF && GetBitFlag(75))
                    {
                        fadeCount = 4;
                        fadeSize = 64;
                    }
                    else
                    {
                        fadeCount = 32;
                        fadeSize = 8;
                    }
                }
                else
                {
                    fadeCount = 64;
                    fadeSize = 4;
                }

                for (var i = fadeCount; i != 0; --i)
                {
                    PaletteFadeOut(_currentPalette, _fastFadeCount, fadeSize);
                    OSystem.GraphicsManager.SetPalette(_currentPalette, 0, _fastFadeCount);
                    Delay(5);
                }

                if (GameType == SIMONGameType.GType_WW ||
                    GameType == SIMONGameType.GType_FF ||
                    GameType == SIMONGameType.GType_PP)
                {
                    ClearSurfaces();
                }
                else
                {
                    if (_windowNum != 4)
                    {
                        ClearSurfaces();
                    }
                }
            }
            if (GameType == SIMONGameType.GType_SIMON2)
            {
                if (_nextMusicToPlay != -1)
                    LoadMusic((ushort) _nextMusicToPlay);
            }
        }

        protected void vc63_fastFadeIn()
        {
            if (GameType == SIMONGameType.GType_FF)
            {
                _fastFadeInFlag = 256;
            }
            else if (GameType == SIMONGameType.GType_SIMON1 ||
                     GameType == SIMONGameType.GType_SIMON2)
            {
                _fastFadeInFlag = 208;
                if (_windowNum != 4)
                {
                    _fastFadeInFlag = 256;
                }
            }
            _fastFadeOutFlag = false;
        }

        protected void vc60_stopAnimation()
        {
            ushort sprite, zoneNum;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
            {
                zoneNum = (ushort) VcReadNextWord();
                sprite = (ushort) VcReadVarOrWord();
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 ||
                     _gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
            {
                zoneNum = (ushort) VcReadNextWord();
                sprite = (ushort) VcReadNextWord();
            }
            else
            {
                sprite = (ushort) VcReadNextWord();
                zoneNum = (ushort) (sprite / 100);
            }

            VcStopAnimation(zoneNum, sprite);
        }

        protected virtual void VcStopAnimation(ushort zone, ushort sprite)
        {
            ushort oldCurSpriteId, oldCurZoneNum;

            oldCurSpriteId = _vgaCurSpriteId;
            oldCurZoneNum = _vgaCurZoneNum;
            var vcPtrOrg = _vcPtr;

            _vgaCurZoneNum = zone;
            _vgaCurSpriteId = sprite;

            var vsp = FindCurSprite().Value;
            if (vsp.id != 0)
            {
                vc25_halt_sprite();

                Ptr<VgaTimerEntry> vte = _vgaTimerList;
                while (vte.Value.delay != 0)
                {
                    if (vte.Value.id == _vgaCurSpriteId && vte.Value.zoneNum == _vgaCurZoneNum)
                    {
                        DeleteVgaEvent(vte);
                        break;
                    }
                    vte.Offset++;
                }
            }

            _vgaCurZoneNum = oldCurZoneNum;
            _vgaCurSpriteId = oldCurSpriteId;
            _vcPtr = vcPtrOrg;
        }

        protected uint GetVarOrWord()
        {
            uint a = _codePtr.ToUInt16BigEndian();
            _codePtr += 2;
            if (GameType == SIMONGameType.GType_PP)
            {
                if (a >= 60000 && a < 62048)
                {
                    return ReadVariable((ushort) (a - 60000));
                }
            }
            else
            {
                if (a >= 30000 && a < 30512)
                {
                    return ReadVariable((ushort) (a - 30000));
                }
            }
            return a;
        }

        private static readonly byte[] opcodeParamLenPN =
        {
            0, 6, 2, 10, 6, 4, 2, 2,
            4, 4, 8, 2, 0, 2, 2, 2,
            0, 2, 2, 2, 0, 4, 2, 2,
            2, 8, 0, 10, 0, 8, 0, 2,
            2, 0, 0, 0, 0, 2, 4, 2,
            4, 4, 0, 0, 2, 2, 2, 4,
            4, 0, 18, 2, 4, 4, 4, 0,
            4
        };

        private static readonly byte[] opcodeParamLenElvira1 =
        {
            0, 6, 2, 10, 6, 4, 2, 2,
            4, 4, 8, 2, 0, 2, 2, 2,
            2, 2, 2, 2, 0, 4, 2, 2,
            2, 8, 0, 10, 0, 8, 0, 2,
            2, 0, 0, 0, 0, 2, 4, 2,
            4, 4, 0, 0, 2, 2, 2, 4,
            4, 0, 18, 2, 4, 4, 4, 0,
            4
        };

        private static readonly byte[] opcodeParamLenWW =
        {
            0, 6, 2, 10, 6, 4, 2, 2,
            4, 4, 8, 2, 2, 2, 2, 2,
            2, 2, 2, 0, 4, 2, 2, 2,
            8, 0, 10, 0, 8, 0, 2, 2,
            0, 0, 0, 4, 4, 4, 2, 4,
            4, 4, 4, 2, 2, 4, 2, 2,
            2, 2, 2, 2, 2, 4, 6, 6,
            0, 0, 0, 0, 2, 2, 0, 0,
        };

        private static readonly byte[] opcodeParamLenSimon1 =
        {
            0, 6, 2, 10, 6, 4, 2, 2,
            4, 4, 10, 0, 2, 2, 2, 2,
            2, 0, 2, 0, 4, 2, 4, 2,
            8, 0, 10, 0, 8, 0, 2, 2,
            4, 0, 0, 4, 4, 2, 2, 4,
            4, 4, 4, 2, 2, 2, 2, 4,
            0, 2, 2, 2, 2, 4, 6, 6,
            0, 0, 0, 0, 2, 6, 0, 0,
        };

        private static readonly byte[] opcodeParamLenSimon2 =
        {
            0, 6, 2, 12, 6, 4, 2, 2,
            4, 4, 9, 0, 1, 2, 2, 2,
            2, 0, 2, 0, 4, 2, 4, 2,
            7, 0, 10, 0, 8, 0, 2, 2,
            4, 0, 0, 4, 4, 2, 2, 4,
            4, 4, 4, 2, 2, 2, 2, 4,
            0, 2, 2, 2, 2, 4, 6, 6,
            2, 0, 6, 6, 4, 6, 0, 0,
            0, 0, 4, 4, 4, 4, 4, 0,
            4, 2, 2
        };

        private static readonly byte[] opcodeParamLenFeebleFiles =
        {
            0, 6, 2, 12, 6, 4, 2, 2,
            4, 4, 9, 0, 1, 2, 2, 2,
            2, 0, 2, 0, 4, 2, 4, 2,
            7, 0, 10, 0, 8, 0, 2, 2,
            4, 0, 0, 4, 4, 2, 2, 4,
            4, 4, 4, 2, 2, 2, 2, 4,
            0, 2, 2, 2, 6, 6, 6, 6,
            2, 0, 6, 6, 4, 6, 0, 0,
            0, 0, 4, 4, 4, 4, 4, 0,
            4, 2, 2, 4, 6, 6, 0, 0,
            6, 4, 2, 6, 0
        };

        private static readonly byte[] iconPalette =
        {
            0x00, 0x00, 0x00,
            0x77, 0x77, 0x55,
            0x55, 0x00, 0x00,
            0x77, 0x00, 0x00,
            0x22, 0x00, 0x00,
            0x00, 0x11, 0x00,
            0x11, 0x22, 0x11,
            0x22, 0x33, 0x22,
            0x44, 0x55, 0x44,
            0x33, 0x44, 0x00,
            0x11, 0x33, 0x00,
            0x00, 0x11, 0x44,
            0x77, 0x44, 0x00,
            0x66, 0x22, 0x00,
            0x00, 0x22, 0x66,
            0x77, 0x55, 0x00,
        };
    }
}