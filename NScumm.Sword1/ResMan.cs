using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NScumm.Core;

namespace NScumm.Sword1
{
    internal class MemHandle
    {
        public byte[] data;
        public uint size;
        public uint refCount;
        public ushort cond;
        public MemHandle next, prev;
    }

    internal class Grp
    {
        public uint noRes;
        public MemHandle[] resHandle;
        public uint[] offset;
        public uint[] length;
    }

    internal class Clu
    {
        public uint refCount;
        public Stream file;
        public string label;
        public uint noGrp;
        public Grp[] grp;
        public Clu nextOpen;
    }

    internal class Prj
    {
        public uint noClu;
        public Clu[] clu;
    }

    internal class ResMan
    {
        private const int MAX_LABEL_SIZE = 31 + 1;

#if __PSP__
const int MAX_OPEN_CLUS =4; // the PSP can't have more than 8 files open simultaneously
                        // since we also need filehandles for music and sometimes savegames
                        // set the maximum number of open clusters to 4.
#else
        private const int MAX_OPEN_CLUS = 8; // don't open more than 8 files at once
#endif

        private bool _isBigEndian;
        private Prj _prj;

        private uint[] _srIdList = { // the file numbers differ for the control panel file IDs, so we need this array
            SwordRes.OTHER_SR_FONT,      // SR_FONT
	        0x04050000,         // SR_BUTTON
	        SwordRes.OTHER_SR_REDFONT,   // SR_REDFONT
	        0x04050001,         // SR_PALETTE
	        0x04050002,         // SR_PANEL_ENGLISH
	        0x04050003,         // SR_PANEL_FRENCH
	        0x04050004,         // SR_PANEL_GERMAN
	        0x04050005,         // SR_PANEL_ITALIAN
	        0x04050006,         // SR_PANEL_SPANISH
	        0x04050007,         // SR_PANEL_AMERICAN
	        0x04050008,         // SR_TEXT_BUTTON
	        0x04050009,         // SR_SPEED
	        0x0405000A,         // SR_SCROLL1
	        0x0405000B,         // SR_SCROLL2
	        0x0405000C,         // SR_CONFIRM
	        0x0405000D,         // SR_VOLUME
	        0x0405000E,         // SR_VLIGHT
	        0x0405000F,         // SR_VKNOB
	        0x04050010,         // SR_WINDOW
	        0x04050011,         // SR_SLAB1
	        0x04050012,         // SR_SLAB2
	        0x04050013,         // SR_SLAB3
	        0x04050014,         // SR_SLAB4
	        0x04050015,         // SR_BUTUF
	        0x04050016,         // SR_BUTUS
	        0x04050017,         // SR_BUTDS
	        0x04050018,         // SR_BUTDF
	        0x04050019,         // SR_DEATHPANEL
	        0,
        };

        private readonly MemMan _memMan;
        private int _openClus;
        private Clu _openCluStart;
        private Clu _openCluEnd;
        private string _directory;

        public ResMan(string directory, string fileName, bool isMacFile)
        {
            _directory = directory;
            _isBigEndian = isMacFile;
            _memMan = new MemMan();
            LoadCluDescript(fileName);
        }

        public byte[] OpenFetchRes(uint id)
        {
            ResOpen(id);
            return FetchRes(id);
        }

        public void ResOpen(uint id)
        {  // load resource ID into memory
            MemHandle memHandle = ResHandle(id);
            if (memHandle == null)
                return;
            if (memHandle.cond == MemMan.MEM_FREED)
            { // memory has been freed
                uint size = ResLength(id);
                _memMan.Alloc(memHandle, size);
                var clusFile = ResFile(id);
                Debug.Assert(clusFile != null);
                clusFile.Seek(ResOffset(id), SeekOrigin.Begin);
                clusFile.Read(memHandle.data, 0, (int)size);
            }
            else
                _memMan.SetCondition(memHandle, MemMan.MEM_DONT_FREE);

            memHandle.refCount++;
            if (memHandle.refCount > 20)
            {
                // TODO: debug(1, "%d references to id %d. Guess there's something wrong.", memHandle.refCount, id);
            }
        }

        public byte[] CptResOpen(uint id)
        {
#if SCUMM_BIG_ENDIAN
            openCptResourceBigEndian(id);
#else
            OpenCptResourceLittleEndian(id);
#endif
            MemHandle handle = ResHandle(id);
            return handle?.data;
        }

        public void ResClose(uint id)
        {
            MemHandle handle = ResHandle(id);
            if (handle == null)
                return;
            if (handle.refCount == 0)
            {
                // TODO: warning("Resource Manager fail: unlocking object with refCount 0. Id: %d", id);
            }
            else
            {
                handle.refCount--;
                if (handle.refCount == 0)
                {
                    _memMan.SetCondition(handle, MemMan.MEM_CAN_FREE);
                }
            }
        }

        public ByteAccess FetchFrame(byte[] resourceData, uint frameNo)
        {
            uint frameFile = 0;
            var idxData = Screen.Header.Size;
            if (_isBigEndian)
            {
                if (frameNo >= resourceData.ToUInt32BigEndian(idxData))
                    throw new InvalidOperationException($"fetchFrame:: frame {frameNo} doesn't exist in resource.");
                frameFile += resourceData.ToUInt32BigEndian((int)(idxData + (frameNo + 1) * 4));
            }
            else
            {
                if (frameNo >= resourceData.ToUInt32(idxData))
                    throw new InvalidOperationException($"fetchFrame:: frame {frameNo} doesn't exist in resource.");
                frameFile += resourceData.ToUInt32((int)(idxData + (frameNo + 1) * 4));
            }
            return new ByteAccess(resourceData, (int)frameFile);
        }

        public ushort ReadUInt16(ushort value)
        {
            return _isBigEndian ? ScummHelper.SwapBytes(value) : value;
        }

        public uint ReadUInt32(uint value)
        {
            return _isBigEndian ? ScummHelper.SwapBytes(value) : value;
        }

        public Screen.Header LockScript(uint scrId)
        {
            if (_scriptList[scrId / ObjectMan.ITM_PER_SEC] == 0)
                throw new InvalidOperationException($"Script id {scrId} not found");
            scrId = _scriptList[scrId / ObjectMan.ITM_PER_SEC];
# if SCUMM_BIG_ENDIAN
            OpenScriptResourceBigEndian(scrId);
#else
            OpenScriptResourceLittleEndian(scrId);
#endif
            MemHandle handle = ResHandle(scrId);
            if (handle == null)
                throw new InvalidOperationException($"Script resource handle {scrId} not found");
            return new Screen.Header(handle.data);
        }

        public void UnlockScript(uint scrId)
        {
            ResClose(_scriptList[scrId / ObjectMan.ITM_PER_SEC]);
        }


        private void LoadCluDescript(string fileName)
        {
            // The cluster description file is always little endian (even on the mac version, whose cluster files are big endian)
            using (var stream = ServiceLocator.FileStorage.OpenFileRead(fileName))
            using (var file = new BinaryReader(stream))
            {
                _prj = new Prj();
                _prj.noClu = file.ReadUInt32();
                _prj.clu = new Clu[_prj.noClu];
                var cluIndex = file.ReadUInt32s((int)_prj.noClu);

                for (uint clusCnt = 0; clusCnt < _prj.noClu; clusCnt++)
                    if (cluIndex[clusCnt] != 0)
                    {
                        var cluster = _prj.clu[clusCnt] = new Clu();
                        cluster.label = new string(file.ReadChars(MAX_LABEL_SIZE));

                        cluster.file = null;
                        cluster.noGrp = file.ReadUInt32();
                        cluster.grp = new Grp[cluster.noGrp];
                        cluster.nextOpen = null;
                        cluster.refCount = 0;

                        var grpIndex = file.ReadUInt32s((int)cluster.noGrp);

                        for (uint grpCnt = 0; grpCnt < cluster.noGrp; grpCnt++)
                            if (grpIndex[grpCnt] != 0)
                            {
                                Grp group = cluster.grp[grpCnt] = new Grp();
                                group.noRes = file.ReadUInt32();
                                group.resHandle = new MemHandle[group.noRes];
                                group.offset = new uint[group.noRes];
                                group.length = new uint[group.noRes];
                                var resIdIdx = file.ReadUInt32s((int)@group.noRes);

                                for (uint resCnt = 0; resCnt < group.noRes; resCnt++)
                                {
                                    if (resIdIdx[resCnt] != 0)
                                    {
                                        group.offset[resCnt] = file.ReadUInt32();
                                        group.length[resCnt] = file.ReadUInt32();
                                        group.resHandle[resCnt] = new MemHandle();
                                        _memMan.InitHandle(group.resHandle[resCnt]);
                                    }
                                    else
                                    {
                                        group.offset[resCnt] = 0xFFFFFFFF;
                                        group.length[resCnt] = 0;
                                        group.resHandle[resCnt] = new MemHandle();
                                        _memMan.InitHandle(group.resHandle[resCnt]);
                                    }
                                }
                            }
                    }

                if (_prj.clu[3].grp[5].noRes == 29)
                    for (byte cnt = 0; cnt < 29; cnt++)
                        _srIdList[cnt] = (uint)(0x04050000 | cnt);
            }
        }

        private void OpenScriptResourceLittleEndian(uint id)
        {
            bool needByteSwap = false;
            if (_isBigEndian)
            {
                // Cluster files are in big endian fomat.
                // If the resource are not in memory anymore, and therefore will be read
                // from disk, they will need to be byte swaped.
                MemHandle memHandle = ResHandle(id);
                if (memHandle != null)
                    needByteSwap = (memHandle.cond == MemMan.MEM_FREED);
            }
            ResOpen(id);
            if (needByteSwap)
            {
                MemHandle handle = ResHandle(id);
                if (handle == null)
                    return;
                // uint32 totSize = handle.size;
                Screen.Header head = new Screen.Header(handle.data);
                head.comp_length = ScummHelper.SwapBytes(head.comp_length);
                head.decomp_length = ScummHelper.SwapBytes(head.decomp_length);
                head.version = ScummHelper.SwapBytes(head.version);
                UIntAccess data = new UIntAccess(handle.data, Screen.Header.Size);
                uint size = handle.size - Screen.Header.Size;
                if ((size & 3) != 0)
                    throw new InvalidOperationException($"Odd size during script endian conversion. Resource ID ={id}, size = {size}");
                size >>= 2;
                for (uint cnt = 0; cnt < size; cnt++)
                {
                    data[0] = ScummHelper.SwapBytes(data[0]);
                    data.Offset += 4;
                }
            }
        }

        private void OpenCptResourceLittleEndian(uint id)
        {
            bool needByteSwap = false;
            if (_isBigEndian)
            {
                // Cluster files are in big endian fomat.
                // If the resource are not in memory anymore, and therefore will be read
                // from disk, they will need to be byte swaped.
                MemHandle memHandle = ResHandle(id);
                if (memHandle != null)
                    needByteSwap = (memHandle.cond == MemMan.MEM_FREED);
            }
            ResOpen(id);
            if (needByteSwap)
            {
                MemHandle handle = ResHandle(id);
                if (handle == null)
                    return;
                uint totSize = handle.size;
                var data = Screen.Header.Size;
                totSize -= Screen.Header.Size;
                if ((totSize & 3) != 0)
                    throw new InvalidOperationException($"Illegal compact size for id {id}: {totSize}");
                totSize /= 4;
                for (uint cnt = 0; cnt < totSize; cnt++)
                {
                    handle.data.WriteUInt32(data, handle.data.ToUInt32BigEndian(data));
                    data++;
                }
            }
        }

        public byte[] FetchRes(uint id)
        {
            MemHandle memHandle = ResHandle(id);
            if (memHandle == null)
            {
                // TODO:warning("fetchRes:: resource %d out of bounds", id);
                return null;
            }
            if (memHandle.data == null)
                throw new InvalidOperationException($"fetchRes:: resource {id} is not open");
            return memHandle.data;
        }

        private MemHandle ResHandle(uint id)
        {
            if ((id >> 16) == 0x0405)
                id = _srIdList[id & 0xFFFF];
            byte cluster = (byte)((id >> 24) - 1);
            byte group = (byte)(id >> 16);

            // There is a known case of reading beyond array boundaries when trying to use
            // portuguese subtitles (cluster file 2, group 6) with a version that does not
            // contain subtitles for this languages (i.e. has only 6 languages and not 7).
            if (cluster >= _prj.noClu || group >= _prj.clu[cluster].noGrp)
                return null;

            return _prj.clu[cluster].grp[group].resHandle[id & 0xFFFF];
        }

        private uint ResLength(uint id)
        {
            if ((id >> 16) == 0x0405)
                id = _srIdList[id & 0xFFFF];
            byte cluster = (byte)((id >> 24) - 1);
            byte group = (byte)(id >> 16);

            if (cluster >= _prj.noClu || group >= _prj.clu[cluster].noGrp)
                return 0;

            return _prj.clu[cluster].grp[group].length[id & 0xFFFF];
        }

        private uint ResOffset(uint id)
        {
            if ((id >> 16) == 0x0405)
                id = _srIdList[id & 0xFFFF];
            byte cluster = (byte)((id >> 24) - 1);
            byte group = (byte)(id >> 16);

            if (cluster >= _prj.noClu || group >= _prj.clu[cluster].noGrp)
                return 0;

            return _prj.clu[cluster].grp[group].offset[id & 0xFFFF];
        }

        private Stream ResFile(uint id)
        {
            Clu cluster = _prj.clu[((id >> 24) - 1)];
            if (cluster.file == null)
            {
                _openClus++;
                if (_openCluEnd == null)
                {
                    _openCluStart = _openCluEnd = cluster;
                }
                else
                {
                    _openCluEnd.nextOpen = cluster;
                    _openCluEnd = cluster;
                }
                string fileName;
                // Supposes that big endian means mac cluster file and little endian means PC cluster file.
                // This works, but we may want to separate the file name from the endianess or try .CLM extension if opening.clu file fail.
                if (_isBigEndian)
                    fileName = $"{_prj.clu[(id >> 24) - 1].label.Trim('\0')}.CLM";
                else
                    fileName = $"{_prj.clu[(id >> 24) - 1].label.Trim('\0')}.CLU";
                var path = ScummHelper.LocatePath(_directory, fileName);
                cluster.file = ServiceLocator.FileStorage.OpenFileRead(path);

                while (_openClus > MAX_OPEN_CLUS)
                {
                    Debug.Assert(_openCluStart != null);
                    Clu closeClu = _openCluStart;
                    _openCluStart = _openCluStart.nextOpen;

                    if (closeClu != null)
                    {
                        closeClu.file?.Dispose();
                        closeClu.file = null;
                        closeClu.nextOpen = null;
                    }
                    _openClus--;
                }
            }
            return cluster.file;
        }

        private static readonly uint[] _scriptList = { //a table of resource tags
            SwordRes.SCRIPT0,		// 0		STANDARD SCRIPTS

	        SwordRes.SCRIPT1,		// 1		PARIS 1
	        SwordRes.SCRIPT2,		// 2
	        SwordRes.SCRIPT3,		// 3
	        SwordRes.SCRIPT4,		// 4
	        SwordRes.SCRIPT5,		// 5
	        SwordRes.SCRIPT6,		// 6
	        SwordRes.SCRIPT7,		// 7
	        SwordRes.SCRIPT8,		// 8

	        SwordRes.SCRIPT9,		// 9		PARIS 2
	        SwordRes.SCRIPT10,		// 10
	        SwordRes.SCRIPT11,		// 11
	        SwordRes.SCRIPT12,		// 12
	        SwordRes.SCRIPT13,		// 13
	        SwordRes.SCRIPT14,		// 14
	        SwordRes.SCRIPT15,		// 15
	        SwordRes.SCRIPT16,		// 16
	        SwordRes.SCRIPT17,		// 17
	        SwordRes.SCRIPT18,		// 18

	        SwordRes.SCRIPT19,		// 19		IRELAND
	        SwordRes.SCRIPT20,		// 20
	        SwordRes.SCRIPT21,		// 21
	        SwordRes.SCRIPT22,		// 22
	        SwordRes.SCRIPT23,		// 23
	        SwordRes.SCRIPT24,		// 24
	        SwordRes.SCRIPT25,		// 25
	        SwordRes.SCRIPT26,		// 26

	        SwordRes.SCRIPT27,		// 27		PARIS 3
	        SwordRes.SCRIPT28,		// 28
	        SwordRes.SCRIPT29,		// 29
	        0,					// 30
	        SwordRes.SCRIPT31,		// 31
	        SwordRes.SCRIPT32,		// 32
	        SwordRes.SCRIPT33,		// 33
	        SwordRes.SCRIPT34,		// 34
	        SwordRes.SCRIPT35,		// 35

	        SwordRes.SCRIPT36,		// 36		PARIS 4
	        SwordRes.SCRIPT37,		// 37
	        SwordRes.SCRIPT38,		// 38
	        SwordRes.SCRIPT39,		// 39
	        SwordRes.SCRIPT40,		// 40
	        SwordRes.SCRIPT41,		// 41
	        SwordRes.SCRIPT42,		// 42
	        SwordRes.SCRIPT43,		// 43
	        0,					// 44

	        SwordRes.SCRIPT45,		// 45		SYRIA
	        SwordRes.SCRIPT46,		// 46		PARIS 4
	        SwordRes.SCRIPT47,		// 47
	        SwordRes.SCRIPT48,		// 48		PARIS 4
	        SwordRes.SCRIPT49,		// 49
	        SwordRes.SCRIPT50,		// 50
	        0,					// 51
	        0,					// 52
	        0,					// 53
	        SwordRes.SCRIPT54,		// 54
	        SwordRes.SCRIPT55,		// 55

	        SwordRes.SCRIPT56,		// 56		SPAIN
	        SwordRes.SCRIPT57,		// 57
	        SwordRes.SCRIPT58,		// 58
	        SwordRes.SCRIPT59,		// 59
	        SwordRes.SCRIPT60,		// 60
	        SwordRes.SCRIPT61,		// 61
	        SwordRes.SCRIPT62,		// 62

	        SwordRes.SCRIPT63,		// 63		NIGHT TRAIN
	        0,					// 64
	        SwordRes.SCRIPT65,		// 65
	        SwordRes.SCRIPT66,		// 66
	        SwordRes.SCRIPT67,		// 67
	        0,					// 68
	        SwordRes.SCRIPT69,		// 69
	        0,					// 70

	        SwordRes.SCRIPT71,		// 71		SCOTLAND
	        SwordRes.SCRIPT72,		// 72
	        SwordRes.SCRIPT73,		// 73
	        SwordRes.SCRIPT74,		// 74

	        0,					// 75
	        0,					// 76
	        0,					// 77
	        0,					// 78
	        0,					// 79
	        SwordRes.SCRIPT80,		// 80
	        0,					// 81
	        0,					// 82
	        0,					// 83
	        0,					// 84
	        0,					// 85
	        SwordRes.SCRIPT86,		// 86
	        0,					// 87
	        0,					// 88
	        0,					// 89
	        SwordRes.SCRIPT90,		// 90
	        0,					// 91
	        0,					// 92
	        0,					// 93
	        0,					// 94
	        0,					// 95
	        0,					// 96
	        0,					// 97
	        0,					// 98
	        0,					// 99
	        0,					// 100
	        0,					// 101
	        0,					// 102
	        0,					// 103
	        0,					// 104
	        0,					// 105
	        0,					// 106
	        0,					// 107
	        0,					// 108
	        0,					// 109
	        0,					// 110
	        0,					// 111
	        0,					// 112
	        0,					// 113
	        0,					// 114
	        0,					// 115
	        0,					// 116
	        0,					// 117
	        0,					// 118
	        0,					// 119
	        0,					// 120
	        0,					// 121
	        0,					// 122
	        0,					// 123
	        0,					// 124
	        0,					// 125
	        0,					// 126
	        0,					// 127
	        SwordRes.SCRIPT128,	// 128

	        SwordRes.SCRIPT129,	// 129
	        SwordRes.SCRIPT130,	// 130
	        SwordRes.SCRIPT131,	// 131
	        0,					// 132
	        SwordRes.SCRIPT133,	// 133
	        SwordRes.SCRIPT134,	// 134
	        0,					// 135
	        0,					// 136
	        0,					// 137
	        0,					// 138
	        0,					// 139
	        0,					// 140
	        0,					// 141
	        0,					// 142
	        0,					// 143
	        0,					// 144
	        SwordRes.SCRIPT145,	// 145
	        SwordRes.SCRIPT146,	// 146
	        0,					// 147
	        0,					// 148
	        0					// 149
        };

        public void Flush()
        {
            for (uint clusCnt = 0; clusCnt < _prj.noClu; clusCnt++)
            {
                Clu cluster = _prj.clu[clusCnt];
                for (uint grpCnt = 0; grpCnt < cluster.noGrp; grpCnt++)
                {
                    Grp group = cluster.grp[grpCnt];
                    for (uint resCnt = 0; resCnt < group.noRes; resCnt++)
                        if (group.resHandle[resCnt].cond != MemMan.MEM_FREED)
                        {
                            _memMan.SetCondition(group.resHandle[resCnt], MemMan.MEM_CAN_FREE);
                            group.resHandle[resCnt].refCount = 0;
                        }
                }
                if (cluster.file != null)
                {
                    cluster.file.Dispose();
                    cluster.file = null;
                    cluster.refCount = 0;
                }
            }
            _openClus = 0;
            _openCluStart = _openCluEnd = null;
            // the memory manager cached the blocks we asked it to free, so explicitly make it free them
            _memMan.Flush();
        }
    }
}