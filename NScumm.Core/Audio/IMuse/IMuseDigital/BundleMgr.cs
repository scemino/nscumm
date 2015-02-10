//
//  BundleMgr.cs
//
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
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using NScumm.Core.IO;

namespace NScumm.Core
{
    class IndexNodeComparer: Comparer<NScumm.Core.BundleDirCache.IndexNode>
    {
        public override int Compare(NScumm.Core.BundleDirCache.IndexNode x, NScumm.Core.BundleDirCache.IndexNode y)
        {
            return string.Compare(x.filename, y.filename, true);
        }
    }

    class BundleMgr
    {
        IndexNodeComparer _indexNodeComparer = new IndexNodeComparer();

        public BundleMgr(BundleDirCache cache)
        {
            _cache = cache;
            _curSampleId = -1;
        }

        public bool Open(string filename, ref bool compressed, bool errorFlag = false)
        {
            if (_file != null)
                return true;

            filename = ScummHelper.LocatePath(Path.GetDirectoryName(ScummEngine.Instance.Game.Path), filename);
            _file = new XorReader(File.OpenRead(filename), 0);

            int slot = _cache.MatchFile(filename);
            Debug.Assert(slot != -1);
            compressed = _cache.IsSndDataExtComp(slot);
            _numFiles = _cache.GetNumFiles(slot);
            Debug.Assert(_numFiles != 0);
            _bundleTable = _cache.GetTable(slot);
            _indexTable = _cache.GetIndexTable(slot);
            Debug.Assert(_bundleTable != null);
            _compTableLoaded = false;
            _outputSize = 0;
            _lastBlock = -1;

            return true;
        }

        public XorReader GetFile(string filename, ref int offset, ref int size)
        {
            var target = new BundleDirCache.IndexNode{ filename = filename };
            var index = Array.BinarySearch(_indexTable, target, _indexNodeComparer);

            if (index >= 0)
            {
                target = _indexTable[index];
                _file.BaseStream.Seek(_bundleTable[target.index].offset, SeekOrigin.Begin);
                offset = _bundleTable[target.index].offset;
                size = _bundleTable[target.index].size;
                return _file;
            }

            return null;
        }

        public int DecompressSampleByIndex(int index, int offset, int size, out byte[] compFinal, int headerSize, bool headerOutside)
        {
            int finalSize, outputSize;
            int skip, firstBlock, lastBlock;

            Debug.Assert(0 <= index && index < _numFiles);

            if (_curSampleId == -1)
                _curSampleId = index;

            Debug.Assert(_curSampleId == index);

            if (!_compTableLoaded)
            {
                _compTableLoaded = LoadCompTable(index);
                if (!_compTableLoaded)
                {
                    compFinal = null;
                    return 0;
                }
            }

            firstBlock = (offset + headerSize) / 0x2000;
            lastBlock = (offset + headerSize + size - 1) / 0x2000;

            // Clip last_block by the total number of blocks (= "comp items")
            if ((lastBlock >= _numCompItems) && (_numCompItems > 0))
                lastBlock = _numCompItems - 1;

            int blocksFinalSize = 0x2000 * (1 + lastBlock - firstBlock);
            compFinal = new byte[blocksFinalSize];
            finalSize = 0;

            skip = (offset + headerSize) % 0x2000;

            for (var i = firstBlock; i <= lastBlock; i++)
            {
                if (_lastBlock != i)
                {
                    // CMI hack: one more zero byte at the end of input buffer
                    _compInputBuff[_compTable[i].size] = 0;
                    _file.BaseStream.Seek(_bundleTable[index].offset + _compTable[i].offset, SeekOrigin.Begin);
                    _file.BaseStream.Read(_compInputBuff, 0, _compTable[i].size);
                    _outputSize = BundleCodecs.DecompressCodec(_compTable[i].codec, _compInputBuff, _compOutputBuff, _compTable[i].size);
                    if (_outputSize > 0x2000)
                    {
                        Console.Error.WriteLine("_outputSize: {0}", _outputSize);
                    }
                    _lastBlock = i;
                }

                outputSize = _outputSize;

                if (headerOutside)
                {
                    outputSize -= skip;
                }
                else
                {
                    if ((headerSize != 0) && (skip >= headerSize))
                        outputSize -= skip;
                }

                if ((outputSize + skip) > 0x2000) // workaround
                    outputSize -= (outputSize + skip) - 0x2000;

                if (outputSize > size)
                    outputSize = size;

                Debug.Assert(finalSize + outputSize <= blocksFinalSize);

                Array.Copy(_compOutputBuff, skip, compFinal, finalSize, outputSize);
                finalSize += outputSize;

                size -= outputSize;
                Debug.Assert(size >= 0);
                if (size == 0)
                    break;

                skip = 0;
            }

            return finalSize;
        }

        public int DecompressSampleByName(string name, int offset, int size, out byte[] comp_final, bool header_outside)
        {
            var final_size = 0;
            var index = Array.BinarySearch(_indexTable, new NScumm.Core.BundleDirCache.IndexNode{ filename = name }, _indexNodeComparer);
            if (index >= 0)
            {
                final_size = DecompressSampleByIndex(index, offset, size, out comp_final, 0, header_outside);
                return final_size;
            }

            Debug.WriteLine("BundleMgr::decompressSampleByName() Failed finding sound {0}", name);
            comp_final = null;
            return final_size;
        }

        public int DecompressSampleByCurIndex(int offset, int size, out byte[] compFinal, int headerSize, bool headerOutside)
        {
            return DecompressSampleByIndex(_curSampleId, offset, size, out compFinal, headerSize, headerOutside);
        }

        bool LoadCompTable(int index)
        {
            _file.BaseStream.Seek(_bundleTable[index].offset, SeekOrigin.Begin);
            var tag = _file.ReadTag();
            _numCompItems = (int)_file.ReadUInt32BigEndian();
            Debug.Assert(_numCompItems > 0);
            _file.BaseStream.Seek(8, SeekOrigin.Current);

            if (tag != "COMP")
            {
                Console.Error.WriteLine("BundleMgr::loadCompTable() Compressed sound {0} ({1}:{2}) invalid ({3})", index, ((FileStream)_file.BaseStream).Name, _bundleTable[index].offset, tag);
                return false;
            }

            _compTable = new CompTable[_numCompItems];
            int maxSize = 0;
            for (int i = 0; i < _numCompItems; i++)
            {
                _compTable[i] = new CompTable();
                _compTable[i].offset = (int)_file.ReadUInt32BigEndian();
                _compTable[i].size = (int)_file.ReadUInt32BigEndian();
                _compTable[i].codec = (int)_file.ReadUInt32BigEndian();
                _file.BaseStream.Seek(4, SeekOrigin.Current);
                if (_compTable[i].size > maxSize)
                    maxSize = _compTable[i].size;
            }
            // CMI hack: one more byte at the end of input buffer
            _compInputBuff = new byte[maxSize + 1];

            return true;
        }

        class CompTable
        {
            public int offset;
            public int size;
            public int codec;
        }

        BundleDirCache _cache;
        BundleDirCache.AudioTable[] _bundleTable;
        BundleDirCache.IndexNode[] _indexTable;
        CompTable[] _compTable;

        int _numFiles;
        int _numCompItems;
        int _curSampleId;
        XorReader _file;
        bool _compTableLoaded;
        byte[] _compOutputBuff = new byte[0x2000];
        byte[] _compInputBuff;
        int _outputSize;
        int _lastBlock;
    }
}

