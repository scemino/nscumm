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

namespace NScumm.Core
{
    class BundleMgr
    {
        public BundleMgr(BundleDirCache cache)
        {
            _cache = cache;
            _curSampleId = -1;
            _fileBundleId = -1;
        }

        public bool Open(string filename, ref bool compressed, bool errorFlag = false)
        {
            // TODO: vs
            throw new NotImplementedException();
//            if (_file != null)
//                return true;
//
//            if ((_file = File.OpenRead(filename)) == null)
//            {
//                if (errorFlag)
//                {
//                    Console.Error.WriteLine("BundleMgr::open() Can't open bundle file: {0}", filename);
//                }
//                else
//                {
//                    Console.Error.WriteLine("BundleMgr::open() Can't open bundle file: {0}", filename);
//                }
//                return false;
//            }
//
//            int slot = _cache.MatchFile(filename);
//            Debug.Assert(slot != -1);
//            compressed = _cache.IsSndDataExtComp(slot);
//            _numFiles = _cache.GetNumFiles(slot);
//            Debug.Assert(_numFiles != 0);
//            _bundleTable = _cache.GetTable(slot);
//            _indexTable = _cache.GetIndexTable(slot);
//            Debug.Assert(_bundleTable != null);
//            _compTableLoaded = false;
//            _outputSize = 0;
//            _lastBlock = -1;
//
//            return true;
        }

        public Stream GetFile(string filename, ref int offset, ref int size)
        {
            // TODO: vs
//            BundleDirCache::IndexNode target;
//            strcpy(target.filename, filename);
//            BundleDirCache::IndexNode *found = (BundleDirCache::IndexNode *)bsearch(&target, _indexTable, _numFiles,
//                sizeof(BundleDirCache::IndexNode), (int (*)(const void*, const void*))scumm_stricmp);
//            if (found) {
//                _file->seek(_bundleTable[found->index].offset, SEEK_SET);
//                offset = _bundleTable[found->index].offset;
//                size = _bundleTable[found->index].size;
//                return _file;
//            }

            return null;
        }

        public int DecompressSampleByIndex(int index, int offset, int size, out byte[] compFinal, int headerSize, bool headerOutside)
        {
            throw new NotImplementedException();
        }

        public int DecompressSampleByName(string name, int offset, int size, out byte[] comp_final, bool header_outside)
        {
            throw new NotImplementedException();
        }

        public int DecompressSampleByCurIndex(int offset, int size, out byte[] compFinal, int headerSize, bool headerOutside)
        {
            return DecompressSampleByIndex(_curSampleId, offset, size, out compFinal, headerSize, headerOutside);
        }


        BundleDirCache _cache;
        //        BundleDirCache::AudioTable *_bundleTable;
        //        BundleDirCache::IndexNode *_indexTable;
        //        CompTable _compTable;

        int _numFiles;
        int _numCompItems;
        int _curSampleId;
        FileStream _file;
        bool _compTableLoaded;
        int _fileBundleId;
        byte[] _compOutputBuff = new byte[0x2000];
        byte[] _compInputBuff;
        int _outputSize;
        int _lastBlock;
    }
}

