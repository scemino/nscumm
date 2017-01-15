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

using System;
using System.IO;
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Another
{
    internal enum ResType : byte
    {
        RtSound = 0,
        RtMusic = 1,
        RtPolyAnim = 2, // full screen Video buffer, size=0x7D00

        // FCS: 0x7D00=32000...but 320x200 = 64000 ??
        // Since the game is 16 colors, two pixels palette indices can be stored in one byte
        // that's why we can store two pixels palette indice in one byte and we only need 320*200/2 bytes for
        // an entire screen.

        RtPalette = 3, // palette (1024=vga + 1024=ega), size=2048
        RtBytecode = 4,
        RtPolyCinematic = 5
    }

    internal class Resource
    {
        /// <summary>
        /// 600kb total memory consumed (not taking into account stack and static heap)
        /// </summary>
        private const int MemBlockSize = 600 * 1024;

        private const int MemEntryStateEndOfMemlist = 0xFF;
        private const int MemEntryStateNotNeeded = 0;
        private const int MemEntryStateLoaded = 1;
        private const int MemEntryStateLoadMe = 2;

        private const int ResSize = 0;
        private const int ResCompressed = 1;
        private const int StatsTotalSize = 6;

        //The game is divided in 10 parts.
        private const int GameNumParts = 10;

        public const int GamePartFirst = 0x3E80;
        public const int GamePart1 = 0x3E80;
        public const int GamePart2 = 0x3E81; //Introductino
        public const int GamePart3 = 0x3E82;
        public const int GamePart4 = 0x3E83; //Wake up in the suspended jail
        public const int GamePart5 = 0x3E84;
        public const int GamePart6 = 0x3E85; //BattleChar sequence
        public const int GamePart7 = 0x3E86;
        public const int GamePart8 = 0x3E87;
        public const int GamePart9 = 0x3E88;
        public const int GamePart10 = 0x3E89;
        private const int GamePartLast = 0x3E89;

        //For each part of the game, four resources are referenced.
        private const int MemlistPartPalette = 0;

        private const int MemlistPartCode = 1;
        private const int MemlistPartPolyCinematic = 2;
        private const int MemlistPartVideo2 = 3;

        private const int MemlistPartNone = 0x00;

        private readonly int[,] _resourceSizeStats = new int[7, 2];
        private readonly int[,] _resourceUnitStats = new int[7, 2];
        public readonly MemEntry[] MemList = ScummHelper.CreateArray<MemEntry>(150);

        private static readonly string[] ResTypes =
        {
            "RT_SOUND",
            "RT_MUSIC",
            "RT_POLY_ANIM",
            "RT_PALETTE",
            "RT_BYTECODE",
            "RT_POLY_CINEMATIC"
        };

        /*
	MEMLIST_PART_VIDEO1 and MEMLIST_PART_VIDEO2 are used to store polygons.

	It seems that:
	- MEMLIST_PART_VIDEO1 contains the cinematic polygons.
	- MEMLIST_PART_VIDEO2 contains the polygons for player and enemies animations.

	That would make sense since protection screen and cinematic game parts do not load MEMLIST_PART_VIDEO2.

*/
        private static readonly ushort[,] MemListParts = new ushort[,]
        {
            //MEMLIST_PART_PALETTE   MEMLIST_PART_CODE   MEMLIST_PART_VIDEO1   MEMLIST_PART_VIDEO2
            {0x14, 0x15, 0x16, 0x00}, // protection screens
            {0x17, 0x18, 0x19, 0x00}, // introduction cinematic
            {0x1A, 0x1B, 0x1C, 0x11},
            {0x1D, 0x1E, 0x1F, 0x11},
            {0x20, 0x21, 0x22, 0x11},
            {0x23, 0x24, 0x25, 0x00}, // battlechar cinematic
            {0x26, 0x27, 0x28, 0x11},
            {0x29, 0x2A, 0x2B, 0x11},
            {0x7D, 0x7E, 0x7F, 0x00},
            {0x7D, 0x7E, 0x7F, 0x00} // password screen
        };

        public Video Video { get; set; }

        public ushort CurrentPartId;
        public ushort RequestedNextPart;
        public bool UseSegVideo2;
        public BytePtr SegPalettes;
        public BytePtr SegBytecode;
        public BytePtr SegCinematic;
        public BytePtr SegVideo2;
        public BytePtr MemPtrStart;

        private ushort _numMemList;
        private BytePtr _scriptBakPtr, _scriptCurPtr, _vidBakPtr, _vidCurPtr;

        /// <summary>
        /// Read all entries from memlist.bin. Do not load anything in memory,
        /// this is just a fast way to access the data later based on their id.
        /// </summary>
        public void ReadEntries()
        {
            var resourceCounter = 0;

            var stream = Engine.OpenFileRead("memlist.bin");
            if (stream == null)
            {
                Error("Resource::readEntries() unable to open 'memlist.bin' file");
                //Error will exit() no need to return or do anything else.
            }
            using (var f = new BinaryReader(stream))
            {
                //Prepare stats array
                //memset(resourceSizeStats,0,sizeof(resourceSizeStats));
                //memset(resourceUnitStats,0,sizeof(resourceUnitStats));

                _numMemList = 0;
                var memEntry = new Ptr<MemEntry>(MemList);
                while (true)
                {
                    //System.Diagnostics.Debug.Assert(_numMemList < _memList.Length);
                    memEntry.Value.State = f.ReadByte();
                    memEntry.Value.Type = (ResType) f.ReadByte();
                    memEntry.Value.BufPtr = BytePtr.Null;
                    f.ReadUInt16BigEndian();
                    memEntry.Value.Unk4 = f.ReadUInt16BigEndian();
                    memEntry.Value.RankNum = f.ReadByte();
                    memEntry.Value.BankId = f.ReadByte();
                    memEntry.Value.BankOffset = f.ReadUInt32BigEndian();
                    memEntry.Value.UnkC = f.ReadUInt16BigEndian();
                    memEntry.Value.PackedSize = f.ReadUInt16BigEndian();
                    memEntry.Value.Unk10 = f.ReadUInt16BigEndian();
                    memEntry.Value.Size = f.ReadUInt16BigEndian();

                    if (memEntry.Value.State == MemEntryStateEndOfMemlist)
                    {
                        break;
                    }

                    //Memory tracking
                    if (memEntry.Value.PackedSize == memEntry.Value.Size)
                    {
                        _resourceUnitStats[(int) memEntry.Value.Type, ResSize]++;
                        _resourceUnitStats[StatsTotalSize, ResSize]++;
                    }
                    else
                    {
                        _resourceUnitStats[(int) memEntry.Value.Type, ResCompressed]++;
                        _resourceUnitStats[StatsTotalSize, ResCompressed]++;
                    }

                    _resourceSizeStats[(int) memEntry.Value.Type, ResSize] += memEntry.Value.Size;
                    _resourceSizeStats[StatsTotalSize, ResSize] += memEntry.Value.Size;
                    _resourceSizeStats[(int) memEntry.Value.Type, ResCompressed] += memEntry.Value.PackedSize;
                    _resourceSizeStats[StatsTotalSize, ResCompressed] += memEntry.Value.PackedSize;


                    if (memEntry.Value.State == MemEntryStateEndOfMemlist)
                    {
                        break;
                    }

                    Debug(DebugLevels.DbgRes, "R:0x{0:X2}, {1,-17} size={2:D5} (compacted gain={3:F0}%)",
                        resourceCounter,
                        ResTypeToString(memEntry.Value.Type),
                        memEntry.Value.Size,
                        memEntry.Value.Size != 0
                            ? (memEntry.Value.Size - memEntry.Value.PackedSize) / (float) memEntry.Value.Size * 100.0f
                            : 0.0f);

                    resourceCounter++;

                    _numMemList++;
                    memEntry.Offset++;
                }

                Debug(DebugLevels.DbgRes, "\n");
                Debug(DebugLevels.DbgRes, "Total # resources: {0}", resourceCounter);
                Debug(DebugLevels.DbgRes, "Compressed       : {0}",
                    _resourceUnitStats[StatsTotalSize, ResCompressed]);
                Debug(DebugLevels.DbgRes, "Uncompressed     : {0}", _resourceUnitStats[StatsTotalSize, ResSize]);
                Debug(DebugLevels.DbgRes, "Note: {0:F0} % of resources are compressed.",
                    100 * _resourceUnitStats[StatsTotalSize, ResCompressed] / (float) resourceCounter);

                Debug(DebugLevels.DbgRes, "\n");
                Debug(DebugLevels.DbgRes, "Total size (uncompressed) : {0:D7} bytes.",
                    _resourceSizeStats[StatsTotalSize, ResSize]);
                Debug(DebugLevels.DbgRes, "Total size (compressed)   : {0:D7} bytes.",
                    _resourceSizeStats[StatsTotalSize, ResCompressed]);
                Debug(DebugLevels.DbgRes, "Note: Overall compression gain is : {0:F0} %.",
                    (_resourceSizeStats[StatsTotalSize, ResSize] -
                     _resourceSizeStats[StatsTotalSize, ResCompressed]) /
                    (float) _resourceSizeStats[StatsTotalSize, ResSize] * 100);

                Debug(DebugLevels.DbgRes, "\n");
                for (var i = 0; i < 6; i++)
                    Debug(DebugLevels.DbgRes,
                        "Total {0,-17} unpacked size: {1:D7} ({2:F0} % of total unpacked size) packedSize {3:D7} ({4:F0} % of floppy space) gain:({5:F0} %)",
                        ResTypeToString((ResType) i),
                        _resourceSizeStats[i, ResSize],
                        _resourceSizeStats[i, ResSize] / (float) _resourceSizeStats[StatsTotalSize, ResSize] * 100.0f,
                        _resourceSizeStats[i, ResCompressed],
                        _resourceSizeStats[i, ResCompressed] /
                        (float) _resourceSizeStats[StatsTotalSize, ResCompressed] * 100.0f,
                        (_resourceSizeStats[i, ResSize] - _resourceSizeStats[i, ResCompressed]) /
                        (float) _resourceSizeStats[i, ResSize] * 100.0f);

                Debug(DebugLevels.DbgRes, "Note: Damn you sound compression rate!");

                Debug(DebugLevels.DbgRes, "\nTotal bank files:              {0}",
                    _resourceUnitStats[StatsTotalSize, ResSize] +
                    _resourceUnitStats[StatsTotalSize, ResCompressed]);
                for (var i = 0; i < 6; i++)
                    Debug(DebugLevels.DbgRes, "Total {0,-17} files: {1:D3}", ResTypeToString((ResType) i),
                        _resourceUnitStats[i, ResSize] + _resourceUnitStats[i, ResCompressed]);
            }
        }

        public void AllocMemBlock()
        {
            MemPtrStart = new byte[MemBlockSize];
            _scriptBakPtr = _scriptCurPtr = MemPtrStart;
            _vidBakPtr =
                _vidCurPtr =
                    MemPtrStart + MemBlockSize -
                    0x800 * 16; //0x800 = 2048, so we have 32KB free for vidBack and vidCur
            UseSegVideo2 = false;
        }

        // Protection screen and cinematic don't need the player and enemies polygon data
        // so _memList[video2Index] is never loaded for those parts of the game. When
        // needed (for action phrases) _memList[video2Index] is always loaded with 0x11
        // (as seen in memListParts).
        public void SetupPart(ushort partId)
        {
            if (partId == CurrentPartId)
                return;

            if (partId < GamePartFirst || partId > GamePartLast)
                Error("Resource::setupPart() ec=0x{0:X} invalid partId", partId);

            var memListPartIndex = (ushort) (partId - GamePartFirst);

            var paletteIndex = (byte) MemListParts[memListPartIndex, MemlistPartPalette];
            var codeIndex = (byte) MemListParts[memListPartIndex, MemlistPartCode];
            var videoCinematicIndex = (byte) MemListParts[memListPartIndex, MemlistPartPolyCinematic];
            var video2Index = (byte) MemListParts[memListPartIndex, MemlistPartVideo2];

            // Mark all resources as located on harddrive.
            InvalidateAll();

            MemList[paletteIndex].State = MemEntryStateLoadMe;
            MemList[codeIndex].State = MemEntryStateLoadMe;
            MemList[videoCinematicIndex].State = MemEntryStateLoadMe;

            // This is probably a cinematic or a non interactive part of the game.
            // Player and enemy polygons are not needed.
            if (video2Index != MemlistPartNone)
                MemList[video2Index].State = MemEntryStateLoadMe;


            LoadMarkedAsNeeded();

            SegPalettes = MemList[paletteIndex].BufPtr;
            SegBytecode = MemList[codeIndex].BufPtr;
            SegCinematic = MemList[videoCinematicIndex].BufPtr;

            Debug($"data: {SegBytecode[0]}, {SegBytecode.ToUInt16BigEndian(1):X2}");

            // This is probably a cinematic or a non interactive part of the game.
            // Player and enemy polygons are not needed.
            if (video2Index != MemlistPartNone)
                SegVideo2 = MemList[video2Index].BufPtr;

            Debug(DebugLevels.DbgRes, "");
            Debug(DebugLevels.DbgRes, "setupPart({0})", partId - GamePartFirst);
            Debug(DebugLevels.DbgRes, "Loaded resource {0} ({1}) in segPalettes.", paletteIndex,
                ResTypeToString(MemList[paletteIndex].Type));
            Debug(DebugLevels.DbgRes, "Loaded resource {0} ({1}) in segBytecode.", codeIndex,
                ResTypeToString(MemList[codeIndex].Type));
            Debug(DebugLevels.DbgRes, "Loaded resource {0} ({1}) in segCinematic.", videoCinematicIndex,
                ResTypeToString(MemList[videoCinematicIndex].Type));

            if (video2Index != MemlistPartNone)
                Debug(DebugLevels.DbgRes, "Loaded resource {0} ({1}) in _segVideo2.", video2Index,
                    ResTypeToString(MemList[video2Index].Type));

            CurrentPartId = partId;

            // _scriptCurPtr is changed in this.load();
            _scriptBakPtr = _scriptCurPtr;
        }

        public void InvalidateRes()
        {
            Ptr<MemEntry> me = MemList;
            var i = _numMemList;
            while (i-- != 0)
            {
                if (me.Value.Type <= ResType.RtPolyAnim || me.Value.Type > (ResType) 6)
                {
                    // 6 WTF ?!?! ResType goes up to 5 !!
                    me.Value.State = MemEntryStateNotNeeded;
                }
                ++me.Offset;
            }
            _scriptCurPtr = _scriptBakPtr;
        }

        /// <summary>
        /// This method serves two purpose:
        /// - Load parts in memory segments (palette,code,video1,video2)
        ///          or
        /// - Load a resource in memory
        /// This is decided based on the resourceId. If it does not match a mementry id it is supposed to
        /// be a part id.
        /// </summary>
        /// <param name="resourceId"></param>
        public void LoadPartsOrMemoryEntry(ushort resourceId)
        {
            if (resourceId > _numMemList)
            {
                RequestedNextPart = resourceId;
            }
            else
            {
                var me = MemList[resourceId];
                if (me.State == MemEntryStateNotNeeded)
                {
                    me.State = MemEntryStateLoadMe;
                    LoadMarkedAsNeeded();
                }
            }
        }

        public void SaveOrLoad(Serializer ser)
        {
            var loadedList = new byte[64];
            if (ser.Mode == Mode.SmSave)
            {
                BytePtr p = loadedList;
                var q = MemPtrStart;
                while (true)
                {
                    Ptr<MemEntry> it = MemList;
                    var me = Ptr<MemEntry>.Null;
                    var num = _numMemList;
                    while (num-- != 0)
                    {
                        if (it.Value.State == MemEntry.Loaded && it.Value.BufPtr == q)
                        {
                            me = it;
                        }
                        ++it.Offset;
                    }
                    if (me == Ptr<MemEntry>.Null)
                    {
                        break;
                    }
                    //assert(p < loadedList + 64);
                    p.Value = (byte) me.Offset;
                    p.Offset++;
                    q += me.Value.Size;
                }
            }

            Entry[] entries =
            {
                Entry.Create(loadedList, 64, 1),
                Entry.Create(this, o => o.CurrentPartId, 1),
                Entry.Create(this, o => o._scriptBakPtr, 1),
                Entry.Create(this, o => o._scriptCurPtr, 1),
                Entry.Create(this, o => o._vidBakPtr, 1),
                Entry.Create(this, o => o._vidCurPtr, 1),
                Entry.Create(this, o => o.UseSegVideo2, 1),
                Entry.Create(this, o => o.SegPalettes, 1),
                Entry.Create(this, o => o.SegBytecode, 1),
                Entry.Create(this, o => o.SegCinematic, 1),
                Entry.Create(this, o => o.SegVideo2, 1),
            };

            ser.SaveOrLoadEntries(entries);
            if (ser.Mode == Mode.SmLoad)
            {
                BytePtr p = loadedList;
                var q = MemPtrStart;
                while (p.Value != 0)
                {
                    var me = MemList[p.Value];
                    p.Offset++;
                    ReadBank(me, q);
                    me.BufPtr = q;
                    me.State = MemEntry.Loaded;
                    q += me.Size;
                }
            }
        }
        private static string ResTypeToString(ResType type)
        {
            if ((int) type >= ResTypes.Length)
                return "RT_UNKNOWN";
            return ResTypes[(int) type];
        }

        private void InvalidateAll()
        {
            for (var i = 0; i < _numMemList; i++)
            {
                MemList[i].State = MemEntryStateNotNeeded;
            }

            _scriptCurPtr = MemPtrStart;
        }

        /// <summary>
        /// Go over every resource and check if they are marked at "MEMENTRY_STATE_LOAD_ME".
        /// Load them in memory and mark them are MEMENTRY_STATE_LOADED
        /// </summary>
        private void LoadMarkedAsNeeded()
        {
            while (true)
            {
                MemEntry me = null;

                // get resource with max rankNum
                byte maxNum = 0;
                var i = _numMemList;
                Ptr<MemEntry> it = MemList;
                while (i-- != 0)
                {
                    if (it.Value.State == MemEntryStateLoadMe && maxNum <= it.Value.RankNum)
                    {
                        maxNum = it.Value.RankNum;
                        me = it.Value;
                    }
                    it.Offset++;
                }

                if (me == null)
                {
                    break; // no entry found
                }


                // At this point the resource descriptor should be pointed to "me"
                // "That's what she said"

                BytePtr loadDestination;
                if (me.Type == ResType.RtPolyAnim)
                {
                    loadDestination = _vidCurPtr;
                }
                else
                {
                    loadDestination = _scriptCurPtr;
                    if (me.Size > _vidBakPtr.Offset - _scriptCurPtr.Offset)
                    {
                        Warning("Resource::load() not enough memory");
                        me.State = MemEntryStateNotNeeded;
                        continue;
                    }
                }

                if (me.BankId == 0)
                {
                    Warning("Resource::load() ec=0x{0:X} (me.bankId == 0)", 0xF00);
                    me.State = MemEntryStateNotNeeded;
                }
                else
                {
                    Debug(DebugLevels.DbgBank,
                        "Resource::load() bufPos={0:X} size={1:X} type={2:X} pos={3:X} bankId={4:X}",
                        loadDestination.Offset - MemPtrStart.Offset, me.PackedSize, me.Type, me.BankOffset, me.BankId);
                    ReadBank(me, loadDestination);
                    if (me.Type == ResType.RtPolyAnim)
                    {
                        Video.CopyPagePtr(_vidCurPtr);
                        me.State = MemEntryStateNotNeeded;
                    }
                    else
                    {
                        me.BufPtr = loadDestination;
                        me.State = MemEntryStateLoaded;
                        _scriptCurPtr += me.Size;
                    }
                }
            }
        }

        private void ReadBank(MemEntry me, BytePtr dstBuf)
        {
            var n = Array.IndexOf(MemList, me);
            Debug(DebugLevels.DbgBank, "Resource::readBank({0})", n);

            var bk = new Bank();
            if (!bk.Read(me, dstBuf))
            {
                Error("Resource::readBank() unable to unpack entry {0}\n", n);
            }
        }
    }
}