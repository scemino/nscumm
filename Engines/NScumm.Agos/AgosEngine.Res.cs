//
//  AgosEngine.Res.cs
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
using System.Collections.Generic;
using System.IO;
using NScumm.Core;
using NScumm.Core.IO;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    internal partial class AgosEngine
    {
        private const int SD_TYPE_LITERAL = 0;
        private const int SD_TYPE_MATCH = 1;
        private const int kMaxColorDepth = 5;

        private class Decrunch
        {
            private readonly BytePtr src;
            private BytePtr s;
            private readonly BytePtr d;
            private readonly int destlen;
            private uint bb;
            private readonly uint x;
            private readonly uint y;
            private byte bc, bit, bits;
            private readonly byte type;

            public Decrunch(BytePtr src, BytePtr dst, int size)
            {
                this.src = src;
                s = src + size - 4;
                destlen = s.ToInt32BigEndian();
                d = dst + destlen;

                // Initialize bit buffer.
                s -= 4;
                bb = x = s.ToUInt32BigEndian();
                bits = 0;
                do
                {
                    x >>= 1;
                    bits++;
                } while (x != 0);
                bits--;

                while (d > dst)
                {
                    SD_GETBIT(ref x);
                    if (x != 0)
                    {
                        SD_GETBITS(ref x, 2);
                        switch (x)
                        {
                            case 0:
                                type = SD_TYPE_MATCH;
                                x = 9;
                                y = 2;
                                break;

                            case 1:
                                type = SD_TYPE_MATCH;
                                x = 10;
                                y = 3;
                                break;

                            case 2:
                                type = SD_TYPE_MATCH;
                                x = 12;
                                SD_GETBITS(ref y, 8);
                                break;

                            default:
                                type = SD_TYPE_LITERAL;
                                x = 8;
                                y = 8;
                                break;
                        }
                    }
                    else
                    {
                        SD_GETBIT(ref x);
                        if (x != 0)
                        {
                            type = SD_TYPE_MATCH;
                            x = 8;
                            y = 1;
                        }
                        else
                        {
                            type = SD_TYPE_LITERAL;
                            x = 3;
                            y = 0;
                        }
                    }

                    if (type == SD_TYPE_LITERAL)
                    {
                        SD_GETBITS(ref x, x);
                        y += x;
                        if (y + 1 > d.Offset - dst.Offset)
                        {
                            throw new OverflowException();
                        }
                        do
                        {
                            SD_GETBITS(ref x, 8);
                            d.Offset--;
                            d.Value = (byte) x;
                        } while (y-- > 0);
                    }
                    else
                    {
                        if (y + 1 > d.Offset - dst.Offset)
                        {
                            throw new OverflowException();
                        }
                        SD_GETBITS(ref x, x);
                        if (d.Offset + x > dst.Offset + destlen)
                        {
                            throw new OverflowException();
                        }
                        do
                        {
                            d.Offset--;
                            d.Value = d[(int) x];
                        } while (y-- > 0);
                    }
                }

                // Successful decrunch.
            }

            private bool SD_GETBIT(ref uint var)
            {
                if (bits-- == 0)
                {
                    s -= 4;
                    if (s < src)
                        return false;
                    bb = s.ToUInt32BigEndian();
                    bits = 31;
                }
                var = bb & 1;
                bb >>= 1;
                return true;
            }

            private bool SD_GETBIT(ref byte var)
            {
                if (bits-- == 0)
                {
                    s -= 4;
                    if (s < src)
                        return false;
                    bb = s.ToUInt32BigEndian();
                    bits = 31;
                }
                var = (byte) (bb & 1);
                bb >>= 1;
                return true;
            }

            private bool SD_GETBITS(ref uint var, uint nbits)
            {
                bc = (byte) nbits;
                var = 0;
                while (bc-- != 0)
                {
                    var <<= 1;
                    if (!SD_GETBIT(ref bit))
                        return false;
                    var |= bit;
                }
                return true;
            }
        }

        private ushort To16Wrapper(uint value)
        {
            return ScummHelper.SwapBytes((ushort) value);
        }

        protected ushort ReadUint16Wrapper(BytePtr src)
        {
            return src.ToUInt16BigEndian();
        }

        private uint ReadUint32Wrapper(BytePtr src)
        {
            return src.ToUInt32BigEndian();
        }

        private void DecompressData(string filename, BytePtr dst, int offs, int srcSize, int dstSize)
        {
            Error("Zlib support is required for Amiga and Macintosh versions");
        }

        private void LoadOffsets(string filename, int number, out int file,
            out int offset, out int srcSize, out int dstSize)
        {
            int offsSize = _gd.Platform == Platform.Amiga ? 16 : 12;

/* read offsets from index */
            var stream = OpenFileRead(filename);
            if (stream == null)
            {
                Error("loadOffsets: Can't load index file '{0}'", filename);
                file = 0;
                offset = 0;
                srcSize = 0;
                dstSize = 0;
                return;
            }

            using (var br = new BinaryReader(stream))
            {
                stream.Seek(number * offsSize, SeekOrigin.Begin);
                offset = br.ReadInt32();
                dstSize = br.ReadInt32();
                srcSize = br.ReadInt32();
                file = br.ReadInt32();
            }
        }

        private int AllocGamePcVars(Stream @in)
        {
            int itemArraySize, itemArrayInited, stringTableNum;
            uint version;
            int i;

            var br = new BinaryReader(@in);
            itemArraySize = br.ReadInt32BigEndian();
            version = br.ReadUInt32BigEndian();
            itemArrayInited = br.ReadInt32BigEndian();
            stringTableNum = br.ReadInt32BigEndian();

// First two items are predefined
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
            {
                itemArraySize += 2;
                itemArrayInited = itemArraySize;
            }
            else
            {
                itemArrayInited += 2;
                itemArraySize += 2;
            }

            if (version != 0x80)
                Error("allocGamePcVars: Not a runtime database");

            _itemArrayPtr = new Item[itemArraySize];
            _itemArraySize = itemArraySize;
            _itemArrayInited = itemArrayInited;
            for (i = 1; i < itemArrayInited; i++)
            {
                var item = AllocateItem<Item>();
                _itemArrayPtr[i] = item;
            }
// The rest is cleared automatically by calloc
            AllocateStringTable(stringTableNum + 10);
            _stringTabNum = stringTableNum;

            return itemArrayInited;
        }

        private void LoadGamePcFile()
        {
            int fileSize;

            if (GetFileName(GameFileTypes.GAME_BASEFILE) != null)
            {
/* Read main gamexx file */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_BASEFILE));
                if (@in == null)
                {
                    Error("loadGamePcFile: Can't load gamexx file '{0}'",
                        GetFileName(GameFileTypes.GAME_BASEFILE));
                }

                if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_CRUNCHED_GAMEPC))
                {
                    var srcSize = (int) @in.Length;
                    var srcBuf = new byte[srcSize];
                    @in.Read(srcBuf, 0, srcSize);

                    int dstSize = srcBuf.ToInt32BigEndian(srcSize - 4);
                    var dstBuf = new byte[dstSize];
                    DecrunchFile(srcBuf, dstBuf, srcSize);

                    using (var stream = new MemoryStream(dstBuf, 0, dstSize))
                    {
                        ReadGamePcFile(stream);
                    }
                }
                else
                {
                    ReadGamePcFile(@in);
                }
            }

            if (GetFileName(GameFileTypes.GAME_TBLFILE) != null)
            {
/* Read list of TABLE resources */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_TBLFILE));
                if (@in == null)
                {
                    Error("loadGamePcFile: Can't load table resources file '{0}'",
                        GetFileName(GameFileTypes.GAME_TBLFILE));
                }

                fileSize = (int) @in.Length;

                _tblList = new byte[fileSize];
                @in.Read(_tblList.Data, _tblList.Offset, fileSize);

/* Remember the current state */
                _subroutineListOrg = _subroutineList;
                _tablesHeapPtrOrg = _tablesHeapPtr;
                _tablesHeapCurPosOrg = _tablesHeapCurPos;
            }

            if (GetFileName(GameFileTypes.GAME_STRFILE) != null)
            {
/* Read list of TEXT resources */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_STRFILE));
                if (@in == null)
                    Error("loadGamePcFile: Can't load text resources file '{0}'",
                        GetFileName(GameFileTypes.GAME_STRFILE));

                fileSize = (int) @in.Length;
                _strippedTxtMem = new byte[fileSize];
                if (_strippedTxtMem == null)
                    Error("loadGamePcFile: Out of memory for strip text list");
                @in.Read(_strippedTxtMem, 0, fileSize);
            }

            if (GetFileName(GameFileTypes.GAME_STATFILE) != null)
            {
/* Read list of ROOM STATE resources */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_STATFILE));
                if (@in == null)
                {
                    Error("loadGamePcFile: Can't load state resources file '{0}'",
                        GetFileName(GameFileTypes.GAME_STATFILE));
                }

                _numRoomStates = (int) (@in.Length / 8);

                _roomStates = new RoomState[_numRoomStates];

                var br = new BinaryReader(@in);
                for (uint s = 0; s < _numRoomStates; s++)
                {
                    ushort num = (ushort) (br.ReadUInt16BigEndian() - (_itemArrayInited - 2));

                    _roomStates[num].state = br.ReadUInt16BigEndian();
                    _roomStates[num].classFlags = br.ReadUInt16BigEndian();
                    _roomStates[num].roomExitStates = br.ReadUInt16BigEndian();
                }
            }

            if (GetFileName(GameFileTypes.GAME_RMSLFILE) != null)
            {
/* Read list of ROOM ITEMS resources */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_RMSLFILE));
                if (@in == null)
                {
                    Error("loadGamePcFile: Can't load room resources file '0'",
                        GetFileName(GameFileTypes.GAME_RMSLFILE));
                }

                fileSize = (int) @in.Length;

                _roomsList = new byte[fileSize];
                if (_roomsList == null)
                    Error("loadGamePcFile: Out of memory for room items list");
                @in.Read(_roomsList, 0, fileSize);
            }

            if (GetFileName(GameFileTypes.GAME_XTBLFILE) != null)
            {
/* Read list of XTABLE resources */
                var @in = OpenFileRead(GetFileName(GameFileTypes.GAME_XTBLFILE));
                if (@in == null)
                {
                    Error("loadGamePcFile: Can't load xtable resources file '{0}'",
                        GetFileName(GameFileTypes.GAME_XTBLFILE));
                }

                fileSize = (int) @in.Length;

                _xtblList = new byte[fileSize];
                if (_xtblList == null)
                    Error("loadGamePcFile: Out of memory for strip xtable list");
                @in.Read(_xtblList, 0, fileSize);

/* Remember the current state */
                _xsubroutineListOrg = _subroutineList;
                _xtablesHeapPtrOrg = _tablesHeapPtr;
                _xtablesHeapCurPosOrg = _tablesHeapCurPos;
            }
        }

        private void ReadGamePcFile(Stream @in)
        {
            var numInitedObjects = AllocGamePcVars(@in);

            CreatePlayer();
            ReadGamePcText(@in);

            for (var i = 2; i < numInitedObjects; i++)
            {
                ReadItemFromGamePc(@in, _itemArrayPtr[i]);
            }

            ReadSubroutineBlock(@in);
        }

        private void ReadGamePcText(Stream @in)
        {
            var br = new BinaryReader(@in);
            _textSize = br.ReadInt32BigEndian();
            _textMem = new byte[_textSize];
            if (_textMem == null)
                Error("readGamePcText: Out of text memory");

            @in.Read(_textMem, 0, _textSize);

            SetupStringTable(_textMem, _stringTabNum);
        }

        private void ReadItemFromGamePc(Stream @in, Item item)
        {
            uint type;

            var br = new BinaryReader(@in);
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
            {
                item.itemName = (ushort) br.ReadUInt32BigEndian();
                item.adjective = br.ReadInt16BigEndian();
                item.noun = br.ReadInt16BigEndian();
                item.state = br.ReadInt16BigEndian();
                br.ReadUInt16BigEndian();
                item.next = (ushort) FileReadItemID(br);
                item.child = (ushort) FileReadItemID(br);
                item.parent = (ushort) FileReadItemID(br);
                br.ReadUInt16BigEndian();
                br.ReadUInt16BigEndian();
                br.ReadUInt16BigEndian();
                item.classFlags = br.ReadUInt16BigEndian();
                item.children = null;
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
            {
                item.itemName = (ushort) br.ReadUInt32BigEndian();
                item.adjective = br.ReadInt16BigEndian();
                item.noun = br.ReadInt16BigEndian();
                item.state = br.ReadInt16BigEndian();
                item.next = (ushort) FileReadItemID(br);
                item.child = (ushort) FileReadItemID(br);
                item.parent = (ushort) FileReadItemID(br);
                br.ReadUInt16BigEndian();
                item.classFlags = br.ReadUInt16BigEndian();
                item.children = null;
            }
            else
            {
                item.adjective = br.ReadInt16BigEndian();
                item.noun = br.ReadInt16BigEndian();
                item.state = br.ReadInt16BigEndian();
                item.next = (ushort) FileReadItemID(br);
                item.child = (ushort) FileReadItemID(br);
                item.parent = (ushort) FileReadItemID(br);
                br.ReadUInt16BigEndian();
                item.classFlags = br.ReadUInt16BigEndian();
                item.children = null;
            }


            type = br.ReadUInt32BigEndian();
            while (type != 0)
            {
                type = br.ReadUInt16BigEndian();
                if (type != 0)
                    ReadItemChildren(br, item, (ChildType) type);
            }
        }

        protected virtual void ReadItemChildren(BinaryReader br, Item item, ChildType type)
        {
            if (type == ChildType.kRoomType)
            {
                var subRoom = AllocateChildBlock<SubRoom>(item, ChildType.kRoomType);
                subRoom.roomShort = (ushort) br.ReadUInt32BigEndian();
                subRoom.roomLong = (ushort) br.ReadUInt32BigEndian();
                subRoom.flags = br.ReadUInt16BigEndian();
            }
            else if (type == ChildType.kObjectType)
            {
                var subObject =
                    AllocateChildBlock<SubObject>(item, ChildType.kObjectType);
                br.ReadUInt32BigEndian();
                br.ReadUInt32BigEndian();
                br.ReadUInt32BigEndian();
                subObject.objectName = (ushort) br.ReadUInt32BigEndian();
                subObject.objectSize = br.ReadUInt16BigEndian();
                subObject.objectWeight = br.ReadUInt16BigEndian();
                subObject.objectFlags = (SubObjectFlags) br.ReadUInt16BigEndian();
            }
            else if (type == ChildType.kGenExitType)
            {
                var genExit = AllocateChildBlock<SubGenExit>(item, ChildType.kGenExitType);
                genExit.dest[0] = (ushort) FileReadItemID(br);
                genExit.dest[1] = (ushort) FileReadItemID(br);
                genExit.dest[2] = (ushort) FileReadItemID(br);
                genExit.dest[3] = (ushort) FileReadItemID(br);
                genExit.dest[4] = (ushort) FileReadItemID(br);
                genExit.dest[5] = (ushort) FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
            }
            else if (type == ChildType.kContainerType)
            {
                var container = AllocateChildBlock<SubContainer>(item, ChildType.kContainerType);
                container.volume = br.ReadUInt16BigEndian();
                container.flags = br.ReadUInt16BigEndian();
            }
            else if (type == ChildType.kChainType)
            {
                var chain = AllocateChildBlock<SubChain>(item, ChildType.kChainType);
                chain.chChained = (ushort) FileReadItemID(br);
            }
            else if (type == ChildType.kUserFlagType)
            {
                SetUserFlag(item, 0, br.ReadUInt16BigEndian());
                SetUserFlag(item, 1, br.ReadUInt16BigEndian());
                SetUserFlag(item, 2, br.ReadUInt16BigEndian());
                SetUserFlag(item, 3, br.ReadUInt16BigEndian());
                SetUserFlag(item, 4, br.ReadUInt16BigEndian());
                SetUserFlag(item, 5, br.ReadUInt16BigEndian());
                SetUserFlag(item, 6, br.ReadUInt16BigEndian());
                SetUserFlag(item, 7, br.ReadUInt16BigEndian());
                var subUserFlag = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
                subUserFlag.userItems[0] = (ushort) FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
                FileReadItemID(br);
            }
            else if (type == ChildType.kInheritType)
            {
                var inherit = AllocateChildBlock<SubInherit>(item, ChildType.kInheritType);
                inherit.inMaster = (ushort) FileReadItemID(br);
            }
            else
            {
                Error("readItemChildren: invalid type {0}", type);
            }
        }

        protected static uint FileReadItemID(BinaryReader br)
        {
            uint val = br.ReadUInt32BigEndian();
            if (val == 0xFFFFFFFF)
                return 0;
            return val + 2;
        }

        private void OpenGameFile()
        {
            _gameFile = OpenFileRead(GetFileName(GameFileTypes.GAME_GMEFILE));

            if (_gameFile == null)
                Error("openGameFile: Can't load game file '{0}'",
                    GetFileName(GameFileTypes.GAME_GMEFILE));

            var br = new BinaryReader(_gameFile);
            int size = br.ReadInt32();

            _gameOffsetsPtr = new Ptr<uint>(new uint[size / 4]);
            _gameFile.Seek(0, SeekOrigin.Begin);

            for (int r = 0; r < size / 4; r++)
                _gameOffsetsPtr[r] = br.ReadUInt32();
        }

        private void ReadGameFile(BytePtr dst, int offs, int size)
        {
            _gameFile.Seek(offs, SeekOrigin.Begin);
            if (_gameFile.Read(dst.Data, dst.Offset, size) != size)
                Error("readGameFile: Read failed ({0},{1})", offs, size);
        }

        private static bool DecrunchFile(BytePtr src, BytePtr dst, int size)
        {
            var decrunch = new Decrunch(src, dst, size);
            // Successful decrunch.
            return true;
        }

        private void DecompressPN(Stack<uint> dataList, out BytePtr dataOut, ref int dataOutSize)
        {
            // Set up the output data area
            dataOutSize = (int) dataList.Pop();
            dataOut = new byte[dataOutSize];
            int outIndex = dataOutSize;

            // Decompression routine
            uint srcVal = dataList.Pop();

            while (outIndex > 0)
            {
                int numBits = 0;
                int count = 0;

                int destVal;
                if (GetBit(dataList, ref srcVal))
                {
                    destVal = CopyBits(dataList, ref srcVal, 2);

                    if (destVal < 2)
                    {
                        count = (int) (destVal + 2);
                        destVal = CopyBits(dataList, ref srcVal, destVal + 9);
                        TransferLoop(dataOut, ref outIndex, (uint) destVal, count);
                        continue;
                    }
                    else if (destVal != 3)
                    {
                        count = CopyBits(dataList, ref srcVal, 8);
                        destVal = CopyBits(dataList, ref srcVal, 8);
                        TransferLoop(dataOut, ref outIndex, (uint) destVal, count);
                        continue;
                    }
                    else
                    {
                        numBits = 8;
                        count = 8;
                    }
                }
                else if (GetBit(dataList, ref srcVal))
                {
                    destVal = CopyBits(dataList, ref srcVal, 8);
                    TransferLoop(dataOut, ref outIndex, (uint) destVal, 1);
                    continue;
                }
                else
                {
                    numBits = 3;
                    count = 0;
                }

                destVal = CopyBits(dataList, ref srcVal, numBits);
                count += destVal;

                // Loop through extracting specified number of bytes
                for (int i = 0; i <= count; ++i)
                {
                    // Shift 8 bits from the source to the destination
                    for (int bitCtr = 0; bitCtr < 8; ++bitCtr)
                    {
                        bool flag = GetBit(dataList, ref srcVal);
                        destVal = (destVal << 1) | (flag ? 1 : 0);
                    }

                    dataOut[--outIndex] = (byte) (destVal & 0xff);
                }
            }
        }

        private static bool GetBit(Stack<uint> dataList, ref uint srcVal)
        {
            bool result = (srcVal & 1) != 0;
            srcVal >>= 1;
            if (srcVal == 0)
            {
                srcVal = dataList.Pop();

                result = (srcVal & 1) != 0;
                srcVal = (uint) ((srcVal >> 1) | 0x80000000L);
            }

            return result;
        }

        private static int CopyBits(Stack<uint> dataList, ref uint srcVal, int numBits)
        {
            int destVal = 0;

            for (int i = 0; i < numBits; ++i)
            {
                bool f = GetBit(dataList, ref srcVal);
                destVal = (destVal << 1) | (f ? 1 : 0);
            }

            return destVal;
        }

        private static void TransferLoop(BytePtr dataOut, ref int outIndex, uint destVal, int max)
        {
            System.Diagnostics.Debug.Assert(outIndex > max - 1);
            BytePtr pDest = dataOut + outIndex;

            for (int i = 0; (i <= max) && (outIndex > 0); ++i)
            {
                pDest = dataOut + --outIndex;
                pDest.Value = pDest[(int) destVal];
            }
        }

        protected void LoadVGABeardFile(ushort id)
        {
            int size;

            if (Features.HasFlag(GameFeatures.GF_OLD_BUNDLE))
            {
                string filename;
                if (id == 23)
                    id = 112;
                else if (id == 328)
                    id = 119;

                if (GamePlatform == Platform.Amiga)
                {
                    if (Features.HasFlag(GameFeatures.GF_TALKIE))
                        filename = $"0{id}.out";
                    else
                        filename = $"0{id}.pkd";
                }
                else
                {
                    filename = $"0{id}.VGA";
                }

                var @in = OpenFileRead(filename);
                if (@in == null)
                    Error("loadSimonVGAFile: Can't load {0}", filename);

                size = (int) @in.Length;
                if (Features.HasFlag(GameFeatures.GF_CRUNCHED))
                {
                    var srcBuffer = new byte[size];
                    if (@in.Read(srcBuffer, 0, size) != size)
                        Error("loadSimonVGAFile: Read failed");
                    DecrunchFile(srcBuffer, _vgaBufferPointers[11].vgaFile2, size);
                }
                else
                {
                    var ptr = _vgaBufferPointers[11].vgaFile2;
                    if (@in.Read(ptr.Data, ptr.Offset, size) != size)
                        Error("loadSimonVGAFile: Read failed");
                }
            }
            else
            {
                int offs = (int) _gameOffsetsPtr[id];
                size = (int) (_gameOffsetsPtr[id + 1] - offs);
                ReadGameFile(_vgaBufferPointers[11].vgaFile2, offs, size);
            }
        }

        private void LoadVGAVideoFile(ushort id, byte type, bool useError)
        {
            BytePtr dst;
            string filename;
            int offs, srcSize, dstSize;
            int extraBuffer = 0;

            if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                 _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) &&
                id == 2 && type == 2)
            {
// WORKAROUND: For the extra long strings in foreign languages
// Allocate more space for text to cope with foreign languages that use
// up more space than English. I hope 6400 bytes are enough. This number
// is base on: 2 (lines) * 320 (screen width) * 10 (textheight) -- olki
                extraBuffer += 6400;
            }

            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_ZLIBCOMP))
            {
                int file;
                LoadOffsets(GetFileName(GameFileTypes.GAME_GFXIDXFILE), id * 3 + type, out file, out offs,
                    out srcSize, out dstSize);

                if (_gd.Platform == Platform.Amiga)
                    filename = $"GFX{file}.VGA";
                else
                    filename = "graphics.vga";

                dst = AllocBlock(dstSize + extraBuffer);
                DecompressData(filename, dst, offs, srcSize, dstSize);
            }
            else if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_OLD_BUNDLE))
            {
                if (_gd.Platform == Platform.Acorn)
                {
                    filename = $"{id:D3}{type}.DAT";
                }
                else if (_gd.Platform == Platform.Amiga || _gd.Platform == Platform.AtariST)
                {
                    if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
                    {
                        filename = $"{id:D3}{type}.out";
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 &&
                             _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_DEMO))
                    {
                        if (_gd.Platform == Platform.AtariST)
                            filename = $"{id:D2}{type}.out";
                        else
                            filename = $"{(char) (48 + id)}{type}.out";
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                             _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                    {
                        filename = $"{id:D2}{type}.pkd";
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                    {
                        filename = $"{(char) (48 + id)}{type}.in";
                    }
                    else
                    {
                        filename = $"{id:D3}{type}.pkd";
                    }
                }
                else
                {
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ||
                        _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2 ||
                        _gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                    {
                        filename = $"{id:D2}{type}.VGA";
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
                    {
                        filename = $"{(char) (48 + id)}{type}.out";
                    }
                    else
                    {
                        filename = $"{id:D3}{type}.VGA";
                    }
                }

                var @in = OpenFileRead(filename);
                if (@in == null)
                {
                    if (useError)
                        Error("loadVGAVideoFile: Can't load {0}", filename);

                    _block = _blockEnd = BytePtr.Null;
                    return;
                }

                dstSize = srcSize = (int) @in.Length;
                if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN && _gd.Platform == Platform.DOS && id == 17 &&
                    type == 2)
                {
// The A2.out file isn't compressed in PC version of Personal Nightmare
                    dst = AllocBlock(dstSize + extraBuffer);
                    if (@in.Read(dst.Data, dst.Offset, dstSize) != dstSize)
                        Error("loadVGAVideoFile: Read failed");
                }
                else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN &&
                         _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_CRUNCHED))
                {
                    var data = new Stack<uint>();
                    BytePtr dataOut;
                    int dataOutSize = 0;

                    var br = new BinaryReader(@in);
                    for (int i = 0; i < srcSize / 4; ++i)
                    {
                        uint dataVal = br.ReadUInt32BigEndian();
                        // Correct incorrect byte, in corrupt 72.out file, included in some PC versions.
                        if (dataVal == 168042714)
                            data.Push(168050906);
                        else
                            data.Push(dataVal);
                    }

                    DecompressPN(data, out dataOut, ref dataOutSize);
                    dst = AllocBlock(dataOutSize + extraBuffer);
                    Array.Copy(dataOut.Data, dataOut.Offset, dst.Data, dst.Offset, dataOutSize);
                }
                else if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_CRUNCHED))
                {
                    var srcBuffer = new byte[srcSize];
                    if (@in.Read(srcBuffer, 0, srcSize) != srcSize)
                        Error("loadVGAVideoFile: Read failed");

                    dstSize = srcBuffer.ToInt32BigEndian(srcSize - 4);
                    dst = AllocBlock(dstSize + extraBuffer);
                    DecrunchFile(srcBuffer, dst, srcSize);
                }
                else
                {
                    dst = AllocBlock(dstSize + extraBuffer);
                    if (@in.Read(dst.Data, dst.Offset, dstSize) != dstSize)
                        Error("loadVGAVideoFile: Read failed");
                }
            }
            else
            {
                id = (ushort) (id * 2 + (type - 1));
                offs = (int) _gameOffsetsPtr[id];
                dstSize = (int) (_gameOffsetsPtr[id + 1] - offs);

                if (dstSize == 0)
                {
                    if (useError)
                        Error("loadVGAVideoFile: Can't load id {0} type {1}", id, type);

                    _block = _blockEnd = BytePtr.Null;
                    return;
                }

                dst = AllocBlock(dstSize + extraBuffer);
                ReadGameFile(dst, offs, dstSize);
            }
        }

        private BytePtr ConvertImage(Vc10State state, bool compressed)
        {
            byte colorDepth = 4;
            if (GameType == SIMONGameType.GType_SIMON1)
            {
                if ((((_videoLockOut & 0x20) != 0) && state.palette == null) ||
                    ((Features.HasFlag(GameFeatures.GF_32COLOR)) &&
                     state.palette != 0xC0))
                {
                    colorDepth = 5;
                }
            }

            var src = state.srcPtr;
            int width = state.width * 16;
            int height = state.height;

            _planarBuf = new byte[width * height];
            BytePtr dst = _planarBuf;

            if (compressed)
            {
                ConvertCompressedImage(src, dst, colorDepth, height, width, GameType == SIMONGameType.GType_PN);
            }
            else
            {
                var length = (width + 15) / 16 * height;
                int i;
                for (i = 0; i < length; i++)
                {
                    ushort[] w = new ushort[kMaxColorDepth];
                    int j;
                    if (GameType == SIMONGameType.GType_SIMON1 && colorDepth == 4)
                    {
                        for (j = 0; j < colorDepth; ++j)
                        {
                            w[j] = src.ToUInt16BigEndian(j * length * 2);
                        }
                        if (state.palette == 0xC0)
                        {
                            BitplaneToChunkyText(w, colorDepth, ref dst);
                        }
                        else
                        {
                            BitplaneToChunky(w, colorDepth, ref dst);
                        }
                        src += 2;
                    }
                    else
                    {
                        for (j = 0; j < colorDepth; ++j)
                        {
                            w[j] = src.ToUInt16BigEndian();
                            src += 2;
                        }
                        BitplaneToChunky(w, colorDepth, ref dst);
                    }
                }
            }

            return _planarBuf;
        }

        private static void ConvertCompressedImage(BytePtr src, BytePtr dst, byte colorDepth, int height, int width,
            bool horizontal = true)
        {
            BytePtr[] plane = new BytePtr[kMaxColorDepth];
            BytePtr[] uncptr = new BytePtr[kMaxColorDepth];
            int length, i, j;

            var uncbfrout = new byte[width * height];

            length = (width + 15) / 16 * height;

            for (i = 0; i < colorDepth; ++i)
            {
                plane[i] = src + src.ToUInt16BigEndian(i * 4) + src.ToUInt16BigEndian(i * 4 + 2);
                uncptr[i] = new byte[length * 2];
                UncompressPlane(plane[i], uncptr[i], length);
                plane[i] = uncptr[i];
            }

            BytePtr uncbfroutptr = uncbfrout;
            for (i = 0; i < length; ++i)
            {
                ushort[] w = new ushort[kMaxColorDepth];
                for (j = 0; j < colorDepth; ++j)
                {
                    w[j] = plane[j].ToUInt16BigEndian();
                    plane[j] += 2;
                }
                BitplaneToChunky(w, colorDepth, ref uncbfroutptr);
            }

            uncbfroutptr = uncbfrout;
            int chunkSize = colorDepth > 4 ? 16 : 8;
            if (horizontal)
            {
                for (j = 0; j < height; ++j)
                {
                    for (i = 0; i < width / 16; ++i)
                    {
                        uncbfroutptr.Copy(dst + width * chunkSize / 16 * j + chunkSize * i, chunkSize);
                        uncbfroutptr += chunkSize;
                    }
                }
            }
            else
            {
                for (i = 0; i < width / 16; ++i)
                {
                    for (j = 0; j < height; ++j)
                    {
                        uncbfroutptr.Copy(dst + width * chunkSize / 16 * j + chunkSize * i, chunkSize);
                        uncbfroutptr += chunkSize;
                    }
                }
            }
        }

        private static void UncompressPlane(BytePtr plane, BytePtr outptr, int length)
        {
            while (length != 0)
            {
                int wordlen;
                sbyte x = (sbyte) plane.Value;
                plane.Offset++;
                if (x >= 0)
                {
                    wordlen = Math.Min(x + 1, length);
                    ushort w = plane.ToUInt16();
                    plane += 2;
                    for (int i = 0; i < wordlen; ++i)
                    {
                        outptr.WriteUInt16(0, w);
                        outptr += 2;
                    }
                }
                else
                {
                    wordlen = Math.Min(-x, length);
                    plane.Copy(outptr, wordlen * 2);
                    outptr += wordlen * 2;
                    plane += wordlen * 2;
                }
                length -= wordlen;
            }
        }

        private static void BitplaneToChunky(Ptr<ushort> w, byte colorDepth, ref BytePtr dst)
        {
            for (int j = 0; j < 8; j++)
            {
                byte color1 = 0;
                byte color2 = 0;
                for (int p = 0; p < colorDepth; ++p)
                {
                    if ((w[p] & 0x8000) != 0)
                    {
                        color1 = (byte) (color1 | 1 << p);
                    }
                    if ((w[p] & 0x4000) != 0)
                    {
                        color2 = (byte) (color2 | 1 << p);
                    }
                    w[p] <<= 2;
                }
                if (colorDepth > 4)
                {
                    dst.Value = color1;
                    dst.Offset++;
                    dst.Value = color2;
                    dst.Offset++;
                }
                else
                {
                    dst.Value = (byte) ((color1 << 4) | color2);
                    dst.Offset++;
                }
            }
        }

        private static void BitplaneToChunkyText(Ptr<ushort> w, byte colorDepth, ref BytePtr dst)
        {
            for (int j = 0; j < 16; j++)
            {
                byte color = 0;
                for (int p = 0; p < colorDepth; ++p)
                {
                    if ((w[p] & 0x8000) != 0)
                    {
                        color = (byte) (color | 1 << p);
                    }
                    w[p] <<= 1;
                }
                if (color != 0)
                    color |= 0xC0;
                dst.Value = color;
                dst.Offset++;
            }
        }
    }
}