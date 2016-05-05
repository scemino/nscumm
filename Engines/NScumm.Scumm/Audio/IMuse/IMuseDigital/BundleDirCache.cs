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
using System.Collections.Generic;
using System.IO;
using System.Text;
using NScumm.Core;

namespace NScumm.Scumm.Audio.IMuse.IMuseDigital
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
                if ((_budleDirCache[fileId].BundleTable == null) && (freeSlot == -1))
                {
                    freeSlot = fileId;
                }
                if (string.Equals(filename, _budleDirCache[fileId].FileName, StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var file = new BinaryReader(ServiceLocator.FileStorage.OpenFileRead(filename));

//                if (freeSlot == -1)
//                    Console.Error.WriteLine("BundleDirCache::matchFileFile() Can't find free slot for file bundle dir cache");

                var tag = file.ReadTag();
                if (tag == "LB23")
                    _budleDirCache[freeSlot].IsCompressed = true;
                offset = (int)file.ReadUInt32BigEndian();

                _budleDirCache[freeSlot].FileName = filename;
                _budleDirCache[freeSlot].NumFiles = (int)file.ReadUInt32BigEndian();
                _budleDirCache[freeSlot].BundleTable = CreateAudioTable(_budleDirCache[freeSlot].NumFiles);

                file.BaseStream.Seek(offset, SeekOrigin.Begin);

                _budleDirCache[freeSlot].IndexTable = CreateIndexTable(_budleDirCache[freeSlot].NumFiles);

                for (int i = 0; i < _budleDirCache[freeSlot].NumFiles; i++)
                {
                    var name = new List<byte>();
                    byte c;

                    if (tag == "LB23")
                    {
						_budleDirCache[freeSlot].BundleTable[i].Filename = file.ReadBytes(24).GetText();
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

						_budleDirCache[freeSlot].BundleTable[i].Filename = name.ToArray().GetText();
                    }
                    _budleDirCache[freeSlot].BundleTable[i].Offset = (int)file.ReadUInt32BigEndian();
                    _budleDirCache[freeSlot].BundleTable[i].Size = (int)file.ReadUInt32BigEndian();
                    _budleDirCache[freeSlot].IndexTable[i].Filename = _budleDirCache[freeSlot].BundleTable[i].Filename;
                    _budleDirCache[freeSlot].IndexTable[i].Index = i;
                }

                Array.Sort(_budleDirCache[freeSlot].IndexTable, new Comparison<IndexNode>((x, y) =>
                        string.Compare(x.Filename, y.Filename, StringComparison.OrdinalIgnoreCase)));

                return freeSlot;
            }
            return fileId;
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
            return _budleDirCache[slot].BundleTable;
        }

        public IndexNode[] GetIndexTable(int slot)
        {
            return _budleDirCache[slot].IndexTable;
        }

        public int GetNumFiles(int slot)
        {
            return _budleDirCache[slot].NumFiles;
        }

        public bool IsSndDataExtComp(int slot)
        {
            return _budleDirCache[slot].IsCompressed;
        }

        public class AudioTable
        {
            public string Filename;
            public int Offset;
            public int Size;
        }

        public class IndexNode
        {
            public string Filename;
            public int Index;
        }

        public class FileDirCache
        {
            public string FileName;
            public AudioTable[] BundleTable;
            public int NumFiles;
            public bool IsCompressed;
            public IndexNode[] IndexTable;
        }

        readonly FileDirCache[] _budleDirCache = new FileDirCache[4];
    }
}

