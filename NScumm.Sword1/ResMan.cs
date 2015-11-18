using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NScumm.Core;

namespace NScumm.Sword1
{
    class MemHandle
    {
        public byte[] data;
        public uint size;
        public uint refCount;
        public ushort cond;
        public MemHandle next, prev;
    }

    class Grp
    {
        public uint noRes;
        public MemHandle[] resHandle;
        public uint[] offset;
        public uint[] length;
    }

    class Clu
    {
        public uint refCount;
        public Stream file;
        public string label;
        public uint noGrp;
        public Grp[] grp;
        public Clu nextOpen;
    }

    class Prj
    {
        public uint noClu;
        public Clu[] clu;
    }

    class ResMan
    {
        const int MAX_LABEL_SIZE = 31 + 1;

#if __PSP__
const int MAX_OPEN_CLUS =4; // the PSP can't have more than 8 files open simultaneously
                        // since we also need filehandles for music and sometimes savegames
                        // set the maximum number of open clusters to 4.
#else
        const int MAX_OPEN_CLUS = 8; // don't open more than 8 files at once
#endif

        private bool _isBigEndian;
        private Prj _prj;

        uint[] _srIdList = { // the file numbers differ for the control panel file IDs, so we need this array
            Sword1Res.OTHER_SR_FONT,      // SR_FONT
	        0x04050000,         // SR_BUTTON
	        Sword1Res.OTHER_SR_REDFONT,   // SR_REDFONT
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
        Clu _openCluStart, _openCluEnd;
        private string _directory;

        public ResMan(string directory, string fileName, bool isMacFile)
        {
            _directory = directory;
            _isBigEndian = isMacFile;
            _memMan = new MemMan();
            LoadCluDescript(fileName);
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

        public byte[] OpenFetchRes(uint id)
        {
            ResOpen(id);
            return FetchRes(id);
        }

        void ResOpen(uint id)
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

        byte[] FetchRes(uint id)
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

        MemHandle ResHandle(uint id)
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

        uint ResLength(uint id)
        {
            if ((id >> 16) == 0x0405)
                id = _srIdList[id & 0xFFFF];
            byte cluster = (byte)((id >> 24) - 1);
            byte group = (byte)(id >> 16);

            if (cluster >= _prj.noClu || group >= _prj.clu[cluster].noGrp)
                return 0;

            return _prj.clu[cluster].grp[group].length[id & 0xFFFF];
        }

        uint ResOffset(uint id)
        {
            if ((id >> 16) == 0x0405)
                id = _srIdList[id & 0xFFFF];
            byte cluster = (byte)((id >> 24) - 1);
            byte group = (byte)(id >> 16);

            if (cluster >= _prj.noClu || group >= _prj.clu[cluster].noGrp)
                return 0;

            return _prj.clu[cluster].grp[group].offset[id & 0xFFFF];
        }

        Stream ResFile(uint id)
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
    }
}