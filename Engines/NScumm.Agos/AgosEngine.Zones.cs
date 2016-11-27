using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Agos
{
    partial class AGOSEngine
    {
        private void FreezeBottom()
        {
            _vgaMemBase = _vgaMemPtr;
            _vgaFrozenBase = _vgaMemPtr;
        }

        protected void UnfreezeBottom()
        {
            _vgaMemPtr = _vgaRealBase;
            _vgaMemBase = _vgaRealBase;
            _vgaFrozenBase = _vgaRealBase;
        }

        private void LoadZone(ushort zoneNum, bool useError = true)
        {
            Ptr<VgaPointersEntry> vpe;

            CHECK_BOUNDS(zoneNum, _vgaBufferPointers);

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PN)
            {
// Only a single zone is used in Personal Nightmare
                vpe = _vgaBufferPointers;
                vc27_resetSprite();
                _vgaMemPtr = _vgaMemBase;
            }
            else
            {
                vpe = new Ptr<VgaPointersEntry>(_vgaBufferPointers, zoneNum);
                if (vpe.Value.vgaFile1 != BytePtr.Null)
                    return;
            }

// Loading order is important due to resource management

            if (_gd.Platform == Platform.Amiga &&
                _gd.ADGameDescription.gameType == SIMONGameType.GType_WW &&
                zoneTable[zoneNum] == 3)
            {
                byte num = (byte) ((zoneNum >= 85) ? 94 : 18);
                LoadVGAVideoFile(num, 2, useError);
            }
            else
            {
                LoadVGAVideoFile(zoneNum, 2, useError);
            }
            vpe.Value.vgaFile2 = _block;
            vpe.Value.vgaFile2End = _blockEnd;

            LoadVGAVideoFile(zoneNum, 1, useError);
            vpe.Value.vgaFile1 = _block;
            vpe.Value.vgaFile1End = _blockEnd;

            vpe.Value.sfxFile = BytePtr.Null;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
            {
// A singe sound file is used for Amiga and AtariST versions
                if (LoadVGASoundFile(1, 3))
                {
                    vpe.Value.sfxFile = _block;
                    vpe.Value.sfxFileEnd = _blockEnd;
                }
            }
            else if (!_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_ZLIBCOMP))
            {
                if (LoadVGASoundFile(zoneNum, 3))
                {
                    vpe.Value.sfxFile = _block;
                    vpe.Value.sfxFileEnd = _blockEnd;
                }
            }
        }

        private void SetZoneBuffers()
        {
            _zoneBuffers = new byte[_vgaMemSize];

            _vgaMemPtr = _zoneBuffers;
            _vgaMemBase = _zoneBuffers;
            _vgaFrozenBase = _zoneBuffers;
            _vgaRealBase = _zoneBuffers;
            _vgaMemEnd = _zoneBuffers + _vgaMemSize;
        }

        private BytePtr AllocBlock(int size)
        {
            while (true)
            {
                _block = _vgaMemPtr;
                _blockEnd = _block + size;

                if (_blockEnd >= _vgaMemEnd)
                {
                    _vgaMemPtr = _vgaMemBase;
                }
                else
                {
                    _rejectBlock = false;
                    CheckNoOverWrite();
                    if (_rejectBlock)
                        continue;
                    CheckRunningAnims();
                    if (_rejectBlock)
                        continue;
                    CheckZonePtrs();
                    _vgaMemPtr = _blockEnd;
                    return _block;
                }
            }
        }

        private void CheckRunningAnims()
        {
            if ((_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 ||
                 _gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2) &&
                (_videoLockOut & 0x20) != 0)
            {
                return;
            }

            for (var vsp = 0; _vgaSprites[vsp].id != 0; vsp++)
            {
                CheckAnims(_vgaSprites[vsp].zoneNum);
                if (_rejectBlock)
                    return;
            }
        }

        private void CheckNoOverWrite()
        {
            if (_noOverWrite == 0xFFFF)
                return;

            var vpe = _vgaBufferPointers[_noOverWrite];

            if (vpe.vgaFile1 < _blockEnd && vpe.vgaFile1End > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.vgaFile1End;
            }
            else if (vpe.vgaFile2 < _blockEnd && vpe.vgaFile2End > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.vgaFile2End;
            }
            else if (vpe.sfxFile != BytePtr.Null && vpe.sfxFile < _blockEnd && vpe.sfxFileEnd > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.sfxFileEnd;
            }
            else
            {
                _rejectBlock = false;
            }
        }

        private void CheckAnims(uint a)
        {
            var vpe = _vgaBufferPointers[a];

            if (vpe.vgaFile1 < _blockEnd && vpe.vgaFile1End > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.vgaFile1End;
            }
            else if (vpe.vgaFile2 < _blockEnd && vpe.vgaFile2End > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.vgaFile2End;
            }
            else if (vpe.sfxFile != BytePtr.Null && vpe.sfxFile < _blockEnd && vpe.sfxFileEnd > _block)
            {
                _rejectBlock = true;
                _vgaMemPtr = vpe.sfxFileEnd;
            }
            else
            {
                _rejectBlock = false;
            }
        }

        private void CheckZonePtrs()
        {
            foreach (var vpe in _vgaBufferPointers)
            {
                if (((vpe.vgaFile1 < _blockEnd) && (vpe.vgaFile1End > _block)) ||
                    ((vpe.vgaFile2 < _blockEnd) && (vpe.vgaFile2End > _block)) ||
                    ((vpe.sfxFile < _blockEnd) && (vpe.sfxFileEnd > _block)))
                {
                    vpe.vgaFile1 = BytePtr.Null;
                    vpe.vgaFile1End = BytePtr.Null;
                    vpe.vgaFile2 = BytePtr.Null;
                    vpe.vgaFile2End = BytePtr.Null;
                    vpe.sfxFile = BytePtr.Null;
                    vpe.sfxFileEnd = BytePtr.Null;
                }
            }
        }

        private static readonly byte[] zoneTable =
        {
            0, 0, 2, 2, 2, 2, 0, 2, 2, 2,
            3, 0, 0, 0, 0, 0, 0, 0, 1, 0,
            3, 3, 3, 1, 3, 0, 0, 0, 1, 0,
            2, 0, 3, 0, 3, 3, 0, 1, 1, 0,
            1, 2, 2, 2, 0, 2, 2, 2, 0, 2,
            1, 2, 2, 2, 0, 2, 2, 2, 2, 2,
            2, 2, 2, 1, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 0, 2, 0, 3, 2, 2, 2, 3,
            2, 3, 3, 3, 1, 3, 3, 1, 1, 0,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 0, 0, 2, 2, 0,
            0, 2, 0, 2, 2, 2, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 2, 2, 2, 2, 2,
            2, 0, 2, 0, 0, 2, 2, 0, 2, 2,
            2, 2, 2, 2, 2, 0, 0, 0, 0, 0,
        };
    }
}