//
//  BundleDirCache.cs
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
using System.Text;
using System.Collections.Generic;

namespace NScumm.Core
{
    class BundleDirCache
    {
        public BundleDirCache()
        {
            for (int i = 0; i < _budleDirCache.Length; i++)
            {
                _budleDirCache[i] = new FileDirCache();
            }
        }

        public int MatchFile(string filename)
        {
            int offset;
            bool found = false;
            int freeSlot = -1;
            int fileId;

            for (fileId = 0; fileId < _budleDirCache.Length; fileId++)
            {
                if ((_budleDirCache[fileId].bundleTable == null) && (freeSlot == -1))
                {
                    freeSlot = fileId;
                }
                if (string.Equals(filename, _budleDirCache[fileId].fileName, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var file = new NScumm.Core.IO.XorReader(File.OpenRead(filename), 0);

                if (freeSlot == -1)
                    Console.Error.WriteLine("BundleDirCache::matchFileFile() Can't find free slot for file bundle dir cache");

                var tag = file.ReadTag();
                if (tag == "LB23")
                    _budleDirCache[freeSlot].isCompressed = true;
                offset = (int)file.ReadUInt32BigEndian();

                _budleDirCache[freeSlot].fileName = filename;
                _budleDirCache[freeSlot].numFiles = (int)file.ReadUInt32BigEndian();
                _budleDirCache[freeSlot].bundleTable = CreateAudioTable(_budleDirCache[freeSlot].numFiles);

                file.BaseStream.Seek(offset, SeekOrigin.Begin);

                _budleDirCache[freeSlot].indexTable = CreateIndexTable(_budleDirCache[freeSlot].numFiles);

                for (int i = 0; i < _budleDirCache[freeSlot].numFiles; i++)
                {
                    var name = new List<byte>();
                    byte c;

                    if (tag == "LB23")
                    {
                        _budleDirCache[freeSlot].bundleTable[i].filename = Encoding.ASCII.GetString(file.ReadBytes(24));
                    }
                    else
                    {
                        for (var z2 = 0; z2 < 8; z2++)
                            if ((c = file.ReadByte()) != 0)
                                name.Add(c);
                        name.Add((byte)'.');
                        for (var z2 = 0; z2 < 4; z2++)
                            if ((c = file.ReadByte()) != 0)
                                name.Add(c);

                        _budleDirCache[freeSlot].bundleTable[i].filename = Encoding.ASCII.GetString(name.ToArray());
                    }
                    _budleDirCache[freeSlot].bundleTable[i].offset = (int)file.ReadUInt32BigEndian();
                    _budleDirCache[freeSlot].bundleTable[i].size = (int)file.ReadUInt32BigEndian();
                    _budleDirCache[freeSlot].indexTable[i].filename = _budleDirCache[freeSlot].bundleTable[i].filename;
                    _budleDirCache[freeSlot].indexTable[i].index = i;
                }

                Array.Sort(_budleDirCache[freeSlot].indexTable, new Comparison<IndexNode>((x, y) =>
                        {
                            return string.Compare(x.filename, y.filename, true);
                        }));

                return freeSlot;
            }
            else
            {
                return fileId;
            }
        }

        static AudioTable[] CreateAudioTable(int length)
        {
            var table = new AudioTable[length];
            for (int i = 0; i < length; i++)
            {
                table[i] = new AudioTable();
            }
            return table;
        }

        static IndexNode[] CreateIndexTable(int length)
        {
            var table = new IndexNode[length];
            for (int i = 0; i < length; i++)
            {
                table[i] = new IndexNode();
            }
            return table;
        }

        public AudioTable[] GetTable(int slot)
        {
            return _budleDirCache[slot].bundleTable;
        }

        public IndexNode[] GetIndexTable(int slot)
        {
            return _budleDirCache[slot].indexTable;
        }

        public int GetNumFiles(int slot)
        {
            return _budleDirCache[slot].numFiles;
        }

        public bool IsSndDataExtComp(int slot)
        {
            return _budleDirCache[slot].isCompressed;
        }

        public class AudioTable
        {
            public string filename;
            public int offset;
            public int size;
        }

        public class IndexNode
        {
            public string filename;
            public int index;
        }

        public class FileDirCache
        {
            public string fileName;
            public AudioTable[] bundleTable;
            public int numFiles;
            public bool isCompressed;
            public IndexNode[] indexTable;
        }

        FileDirCache[] _budleDirCache = new FileDirCache[4];
    }
}

