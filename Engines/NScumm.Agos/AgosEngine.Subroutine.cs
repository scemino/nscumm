//
//  AgosEngine.Subroutine.cs
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
using System.IO;
using NScumm.Core;
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        private static readonly string[] opcodeArgTable_elvira1 =
        {
            "I ", "I ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "F ", "F ", "FN ",
            "FN ", "FN ", "FN ", "FF ", "FF ", "FF ", "FF ", "II ", "II ", "a ", "a ", "n ", "n ", "p ",
            "N ", "I ", "I ", "I ", "I ", "IN ", "IB ", "IB ", "II ", "IB ", "N ", " ", " ", " ", "I ",
            "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "IBF ", "FIB ", "FF ", "N ", "NI ",
            "IF ", "F ", "F ", "IB ", "IB ", "FN ", "FN ", "FN ", "FF ", "FF ", "FN ", "FN ", "FF ", "FF ",
            "FN ", "FF ", "FN ", "F ", "I ", "IN ", "IN ", "IB ", "IB ", "IB ", "IB ", "II ", "I ", "I ",
            "IN ", "T ", "F ", " ", "T ", "T ", "I ", "I ", " ", " ", "T ", " ", " ", " ", " ", " ", "T ",
            " ", "N ", "INN ", "II ", "II ", "ITN ", "ITIN ", "ITIN ", "I3 ", "IN ", "I ", "I ", "Ivnn ",
            "vnn ", "Ivnn ", "NN ", "IT ", "INN ", " ", "N ", "N ", "N ", "T ", "v ", " ", " ", " ", " ",
            "FN ", "I ", "TN ", "IT ", "II ", "I ", " ", "N ", "I ", " ", "I ", "NI ", "I ", "I ", "T ",
            "I ", "I ", "N ", "N ", " ", "N ", "IF ", "IF ", "IF ", "IF ", "IF ", "IF ", "T ", "IB ",
            "IB ", "IB ", "I ", " ", "vnnN ", "Ivnn ", "T ", "T ", "T ", "IF ", " ", " ", " ", "Ivnn ",
            "IF ", "INI ", "INN ", "IN ", "II ", "IFF ", "IIF ", "I ", "II ", "I ", "I ", "IN ", "IN ",
            "II ", "II ", "II ", "II ", "IIN ", "IIN ", "IN ", "II ", "IN ", "IN ", "T ", "vanpan ",
            "vIpI ", "T ", "T ", " ", " ", "IN ", "IN ", "IN ", "IN ", "N ", "INTTT ", "ITTT ",
            "ITTT ", "I ", "I ", "IN ", "I ", " ", "F ", "NN ", "INN ", "INN ", "INNN ", "TF ", "NN ",
            "N ", "NNNNN ", "N ", " ", "NNNNNNN ", "N ", " ", "N ", "NN ", "N ", "NNNNNIN ", "N ", "N ",
            "N ", "NNN ", "NNNN ", "INNN ", "IN ", "IN ", "TT ", "I ", "I ", "I ", "TTT ", "IN ", "IN ",
            "FN ", "FN ", "FN ", "N ", "N ", "N ", "NI ", " ", " ", "N ", "I ", "INN ", "NN ", "N ",
            "N ", "Nan ", "NN ", " ", " ", " ", " ", " ", " ", " ", "IF ", "N ", " ", " ", " ", "II ",
            " ", "NI ", "N ",
        };

        private static readonly string[] opcodeArgTable_elvira2 =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "I  ", "I ", " ", "T ", " ", " ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", "T ", "T ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NBNNN ", "N ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "N ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NN ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ", "N ",
            "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ", "B ",
            "IBB ", "IBN ", "IB ", "B ", " ", "TB ", "TB ", "I ", "N ", "B ", "INB ", "INB ", "INB ", "INB ",
            "INB ", "INB ", "INB ", "N ", " ", "INBB ", "B ", "B ", "Ian ", "B ", "B ", "B ", "B ", "T ",
            "T ", "B ", " ", "I ", " ", " "
        };

        public static readonly string[] opcodeArgTable_waxworks =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BT ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", "T ", "T ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NBNNN ", "N ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NN ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ", "N ",
            "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ", "B ",
            "IBB ", "IBN ", "IB ", "B ", " ", "TB ", "TB ", "I ", "N ", "B ", "INB ", "INB ", "INB ", "INB ",
            "INB ", "INB ", "INB ", "N ", " ", "INBB ", "B ", "B ", "Ian ", "B ", "B ", "B ", "B ", "T ",
            "T ", "B ", " ", "I ", " ", " "
        };

        private static readonly string[] opcodeArgTable_simon1talkie =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BTS ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NBNNN ", "N ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NN ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ", "N ",
            "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ", "B ",
            "IBB ", "IBN ", "IB ", "B ", "BNBN ", "BBTS ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ", "T ",
            "T ", "B ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", " ", " ", "N ", "N ", " ",
            " ",
        };

        private static readonly string[] opcodeArgTable_simon1dos =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BT ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NBNNN ", "N ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NN ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ", "N ",
            "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ", "B ",
            "IBB ", "IBN ", "IB ", "B ", "BNBN ", "BBT ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ", "T ",
            "T ", "B ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", " ", " ", "N ", "N ", " ",
            " ",
        };

        private static readonly string[] opcodeArgTable_simon2talkie =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BTS ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NNBNNN ", "NN ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NNB ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ",
            "N ", "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ",
            "B ", "IBB ", "IBN ", "IB ", "B ", "BNBN ", "BBTS ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ",
            "T ", "T ", "B ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", " ", " ", "N ", "N ",
            " ", " ", "BT ", " ", "B "
        };

        private static readonly string[] opcodeArgTable_simon2dos =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BT ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NNBNNN ", "NN ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NNB ", "N ", "N ", "Ban ", "BB ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ",
            "N ", "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ",
            "B ", "IBB ", "IBN ", "IB ", "B ", "BNBN ", "BBT ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ",
            "T ", "T ", "B ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", " ", " ", "N ", "N ",
            " ", " ", "BT ", " ", "B "
        };

        private static readonly string[] opcodeArgTable_feeblefiles =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "B ", "B ", "BN ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BB ", "BB ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBB ", "BIB ", "BB ", "B ", "BI ", "IB ", "B ", "B ", "BN ",
            "BN ", "BN ", "BB ", "BB ", "BN ", "BN ", "BB ", "BB ", "BN ", "BB ", "BN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "B ", "T ", "T ", "NNNNNB ", "BT ", "BTS ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NNBNNN ", "NN ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NNB ", "N ", "N ", "Ban ", " ", " ", " ", " ", " ", "IB ", "B ", " ", "II ", " ", "BI ",
            "N ", "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "B ", "B ", "B ",
            "B ", "IBB ", "IBN ", "IB ", "B ", "BNNN ", "BBTS ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ",
            "T ", "N ", " ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", "T ", " ", "N ", "N ",
            " ", " ", "BT ", " ", "B ", " ", "BBBB ", " ", " ", "BBBB ", "B ", "B ", "B ", "B "
        };

        private static readonly string[] opcodeArgTable_puzzlepack =
        {
            " ", "I ", "I ", "I ", "I ", "I ", "I ", "II ", "II ", "II ", "II ", "N ", "N ", "NN ", "NN ",
            "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "II ", "II ", "N ", "I ", "I ", "I ", "IN ", "IB ",
            "II ", "I ", "I ", "II ", "II ", "IBN ", "NIB ", "NN ", "B ", "BI ", "IN ", "N ", "N ", "NN ",
            "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "NN ", "B ", "I ", "IB ",
            "IB ", "II ", "I ", "I ", "IN ", "N ", "T ", "T ", "NNNNNB ", "BTNN ", "BTS ", "T ", " ", "B ",
            "N ", "IBN ", "I ", "I ", "I ", "NN ", " ", " ", "IT ", "II ", "I ", "B ", " ", "IB ", "IBB ",
            "IIB ", "T ", " ", " ", "IB ", "IB ", "IB ", "B ", "BB ", "IBB ", "NB ", "N ", "NNBNNN ", "NN ",
            " ", "BNNNNNN ", "B ", " ", "B ", "B ", "BB ", "NNNNNIN ", "N ", "N ", "N ", "NNN ", "NBNN ",
            "IBNN ", "IB ", "IB ", "IB ", "IB ", "N ", "N ", "N ", "BI ", " ", " ", "N ", "I ", "IBB ",
            "NNB ", "N ", "N ", "Ban ", " ", " ", " ", " ", " ", "IN ", "B ", " ", "II ", " ", "BI ",
            "N ", "I ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "IB ", "BI ", "BB ", "N ", "N ", "N ",
            "N ", "IBN ", "IBN ", "IN ", "B ", "BNNN ", "BBTS ", "N ", " ", "Ian ", "B ", "B ", "B ", "B ",
            "T ", "N ", " ", " ", "I ", " ", " ", "BBI ", "NNBB ", "BBB ", " ", " ", "T ", " ", "N ", "N ",
            " ", " ", "BT ", " ", "B ", " ", "BBBB ", " ", " ", "BBBB ", "B ", "B ", "B ", "B "
        };

        protected Subroutine GetSubroutineByID(uint subroutineId)
        {
            Subroutine cur;

            for (cur = _subroutineList; cur != null; cur = cur.next)
            {
                if (cur.id == subroutineId)
                    return cur;
            }

            if (LoadXTablesIntoMem((ushort) subroutineId))
            {
                for (cur = _subroutineList; cur != null; cur = cur.next)
                {
                    if (cur.id == subroutineId)
                        return cur;
                }
            }

            if (LoadTablesIntoMem((ushort) subroutineId))
            {
                for (cur = _subroutineList; cur != null; cur = cur.next)
                {
                    System.Diagnostics.Debug.WriteLine($"cur.id: {cur.id}");
                    if (cur.id == subroutineId)
                        return cur;
                }
            }

            Debug(0, "getSubroutineByID: subroutine {0} not found", subroutineId);
            return null;
        }

        protected void AlignTableMem()
        {
            if (!IS_ALIGNED(_tablesHeapPtr, 4))
            {
                _tablesHeapCurPos += 2;
                _tablesHeapCurPos += 2;
            }
        }

        private BytePtr AllocateTable(int size)
        {
            var org = _tablesHeapPtr;
            size = (size + 1) & ~1;

            _tablesHeapPtr += size;
            _tablesHeapCurPos += size;

            if (_tablesHeapCurPos > _tablesHeapSize)
                Error("Tablesheap overflow");

            return org;
        }

        private void AllocTablesHeap()
        {
            _tablesHeapSize = _tableMemSize;
            _tablesHeapCurPos = 0;
            _tablesHeapPtr = new byte[_tableMemSize];
        }

        private void EndCutscene()
        {
            _sound.StopVoice();

            var sub = GetSubroutineByID(170);
            if (sub != null)
                StartSubroutineEx(sub);

            _runScriptReturn1 = true;
        }

        protected Stream OpenTablesFile(string filename)
        {
            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_OLD_BUNDLE))
                return OpenTablesFileSimon1(filename);
            return OpenTablesFileGme(filename);
        }

        private Stream OpenTablesFileSimon1(string filename)
        {
            var @in = OpenFileRead(filename);
            if (@in == null)
                Error("openTablesFile: Can't open '{0}'", filename);
            return @in;
        }

        private Stream OpenTablesFileGme(string filename)
        {
            var res = int.Parse(filename.Substring(6)) + _tableIndexBase - 1;
            var offs = _gameOffsetsPtr[res];

            _gameFile.Seek(offs, SeekOrigin.Begin);
            return _gameFile;
        }

        protected virtual bool LoadTablesIntoMem(ushort subrId)
        {
            if (_tblList == BytePtr.Null)
                return false;

            var p = 32;

            var minNum = _tblList.ToUInt16BigEndian(p);
            var maxNum = _tblList.ToUInt16BigEndian(p + 2);
            ushort fileNum = _tblList[p + 4];
            p += 6;

            while (minNum != 0)
            {
                if ((subrId >= minNum) && (subrId <= maxNum))
                {
                    _subroutineList = _subroutineListOrg;
                    _tablesHeapPtr = _tablesHeapPtrOrg;
                    _tablesHeapCurPos = _tablesHeapCurPosOrg;
                    _stringIdLocalMin = 1;
                    _stringIdLocalMax = 0;

                    var filename = $"TABLES%.{fileNum:D2}";
                    var @in = OpenTablesFile(filename);
                    ReadSubroutineBlock(@in);
                    CloseTablesFile(@in);

                    AlignTableMem();

                    _tablesheapPtrNew = _tablesHeapPtr;
                    _tablesHeapCurPosNew = _tablesHeapCurPos;

                    if (_tablesHeapCurPos > _tablesHeapSize)
                        Error("loadTablesIntoMem: Out of table memory");
                    return true;
                }

                minNum = _tblList.ToUInt16BigEndian(p);
                maxNum = _tblList.ToUInt16BigEndian(p + 2);
                fileNum = _tblList[p + 4];
                p += 6;
            }

            Debug(1, "loadTablesIntoMem: didn't find {0}", subrId);
            return false;
        }

        private bool LoadXTablesIntoMem(ushort subrId)
        {
            int p = 0;
            char[] filename = new char[30];

            if (_xtblList == null)
                return false;

            while (_xtblList[p] != 0)
            {
                int i;
                for (i = 0; _xtblList[p] != 0; p++, i++)
                    filename[i] = (char) _xtblList[p];
                filename[i] = '\0';
                p++;

                for (;;)
                {
                    uint min_num = _xtblList.ToUInt16BigEndian(p);
                    p += 2;

                    if (min_num == 0)
                        break;

                    uint max_num = _xtblList.ToUInt16BigEndian(p);
                    p += 2;

                    if (subrId >= min_num && subrId <= max_num)
                    {
                        _subroutineList = _xsubroutineListOrg;
                        _tablesHeapPtr = _xtablesHeapPtrOrg;
                        _tablesHeapCurPos = _xtablesHeapCurPosOrg;
                        _stringIdLocalMin = 1;
                        _stringIdLocalMax = 0;

                        var @in = OpenTablesFile(new string(filename));
                        ReadSubroutineBlock(@in);
                        CloseTablesFile(@in);

                        AlignTableMem();

                        _subroutineListOrg = _subroutineList;
                        _tablesHeapPtrOrg = _tablesHeapPtr;
                        _tablesHeapCurPosOrg = _tablesHeapCurPos;
                        _tablesheapPtrNew = _tablesHeapPtr;
                        _tablesHeapCurPosNew = _tablesHeapCurPos;

                        return true;
                    }
                }
            }

            Debug(1, "loadXTablesIntoMem: didn't find {0}", subrId);
            return false;
        }

        protected void CloseTablesFile(Stream @in)
        {
            if (_gd.ADGameDescription.features.HasFlag(GameFeatures.GF_OLD_BUNDLE))
            {
                @in.Dispose();
            }
        }

        private Subroutine CreateSubroutine(ushort id)
        {
            System.Diagnostics.Debug.WriteLine($"CreateSubroutine {id}");
            AlignTableMem();

            var sub = new Subroutine(AllocateTable(Subroutine.Size));
            sub.id = id;
            sub.first = 0;
            sub.next = _subroutineList;
            _subroutineList = sub;
            return sub;
        }

        private SubroutineLine CreateSubroutineLine(Subroutine sub, int where)
        {
            SubroutineLine curSl = null, lastSl = null;

            var size = sub.id == 0 ? SUBROUTINE_LINE_BIG_SIZE : SUBROUTINE_LINE_SMALL_SIZE;
            var sl = new SubroutineLine(AllocateTable(size));

// where is what offset to insert the line at, locate the proper beginning line
            if (sub.first != 0)
            {
                curSl = new SubroutineLine(sub.Pointer + sub.first);
                while (where != 0)
                {
                    lastSl = curSl;
                    curSl = new SubroutineLine(sub.Pointer + curSl.next);
                    if (curSl.Pointer == sub.Pointer)
                        break;
                    where--;
                }
            }

            if (lastSl != null)
            {
// Insert the subroutine line in the middle of the link
                lastSl.next = (ushort) (sl.Pointer.Offset - sub.Pointer.Offset);
                sl.next = (ushort) (curSl.Pointer.Offset - sub.Pointer.Offset);
            }
            else
            {
// Insert the subroutine line at the head of the link
                sl.next = sub.first;
                sub.first = (ushort) (sl.Pointer.Offset - sub.Pointer.Offset);
            }

            return sl;
        }

        private void RunSubroutine101()
        {
            var sub = GetSubroutineByID(101);
            if (sub != null)
                StartSubroutineEx(sub);

            PermitInput();
        }

        protected int StartSubroutine(Subroutine sub)
        {
            int result = -1;
            var sl = new SubroutineLine(sub.Pointer + sub.first);

            var old_code_ptr = _codePtr;
            Subroutine old_currentTable = _currentTable;
            SubroutineLine old_currentLine = _currentLine;
            SubroutineLine old_classLine = _classLine;
            short old_classMask = _classMask;
            short old_classMode1 = _classMode1;
            short old_classMode2 = _classMode2;

            _classLine = null;
            _classMask = 0;
            _classMode1 = 0;
            _classMode2 = 0;

            if (DebugManager.Instance.IsDebugChannelEnabled(DebugLevels.kDebugSubroutine))
                DumpSubroutine(sub);

            if (++_recursionDepth > 40)
                Error("Recursion error");

            // WORKAROUND: If the game is saved, right after Simon is thrown in the dungeon of Sordid's Fortress of Doom,
            // the saved game fails to load correctly. When loading the saved game, the sequence of Simon waking is started,
            // before the scene is actually reloaded, due to a script bug. We manually add the extra script code from the
            // updated DOS CD release, which fixed this particular script bug.
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 &&
                sub.id == 12101)
            {
                const byte bit = 228;
                if ((_bitArrayTwo[bit / 16] & (1 << (bit & 15))) != 0 && (int) ReadVariable(34) == -1)
                {
                    _bitArrayTwo[228 / 16] = (ushort) (_bitArrayTwo[228 / 16] & ~(1 << (bit & 15)));
                    WriteVariable(34, 1);
                }
            }

            _currentTable = sub;
            restart:

            if (HasToQuit)
                return result;

            while (sl.Pointer != sub.Pointer)
            {
                _currentLine = sl;
                if (CheckIfToRunSubroutineLine(sl, sub))
                {
                    _codePtr = sl.Pointer;
                    if (sub.id != 0)
                        _codePtr += 2;
                    else
                        _codePtr += 8;

                    DebugC(DebugLevels.kDebugOpcode, "; {0}", sub.id);
                    result = RunScript();
                    if (result != 0)
                    {
                        break;
                    }
                }
                sl = new SubroutineLine(sub.Pointer + sl.next);
            }

            // WORKAROUND: Feeble walks in the incorrect direction, when looking at the Vent in the Research and Testing area of
            // the Company Central Command Compound. We manually add the extra script code from the updated English 2CD release,
            // which fixed this particular script bug.
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF &&
                _language == Language.EN_ANY)
            {
                if (sub.id == 39125 && ReadVariable(84) == 2)
                {
                    WriteVariable(1, 1136);
                    WriteVariable(2, 346);
                }
                if (sub.id == 39126 && ReadVariable(84) == 2)
                {
                    Subroutine tmpSub = GetSubroutineByID(80);
                    if (tmpSub != null)
                    {
                        StartSubroutine(tmpSub);
                    }
                }
            }

            if (_classMode1 != 0)
            {
                _subjectItem = NextInByClass(_subjectItem, _classMask);
                if (_subjectItem == null)
                {
                    _classMode1 = 0;
                }
                else
                {
                    Delay(0);
                    sl = _classLine; /* Rescanner */
                    goto restart;
                }
            }
            if (_classMode2 != 0)
            {
                _objectItem = NextInByClass(_objectItem, _classMask);
                if (_objectItem == null)
                {
                    _classMode2 = 0;
                }
                else
                {
                    Delay(0);
                    sl = _classLine; /* Rescanner */
                    goto restart;
                }
            }

            /* result -10 means restart subroutine */
            if (result == -10)
            {
                Delay(0);
                sl = new SubroutineLine(sub.Pointer + sub.first);
                goto restart;
            }

            _codePtr = old_code_ptr;
            _currentLine = old_currentLine;
            _currentTable = old_currentTable;
            _classLine = old_classLine;
            _classMask = old_classMask;
            _classMode1 = old_classMode2;
            _classMode2 = old_classMode1;
            _findNextPtr = null;

            _recursionDepth--;
            return result;
        }

        private int StartSubroutineEx(Subroutine sub)
        {
            return StartSubroutine(sub);
        }

        private bool CheckIfToRunSubroutineLine(SubroutineLine sl, Subroutine sub)
        {
            if (sub.id != 0)
                return true;

            if (sl.verb != -1 && sl.verb != _scriptVerb &&
                (sl.verb != -2 || _scriptVerb != -1))
                return false;

            if (sl.noun1 != -1 && sl.noun1 != _scriptNoun1 &&
                (sl.noun1 != -2 || _scriptNoun1 != -1))
                return false;

            if (sl.noun2 != -1 && sl.noun2 != _scriptNoun2 &&
                (sl.noun2 != -2 || _scriptNoun2 != -1))
                return false;

            return true;
        }

        protected void ReadSubroutineBlock(Stream @in)
        {
            var br = new BinaryReader(@in);
            while (br.ReadUInt16BigEndian() == 0)
            {
                ReadSubroutine(br, CreateSubroutine(br.ReadUInt16BigEndian()));
            }
        }

        private void ReadSubroutine(BinaryReader br, Subroutine sub)
        {
            while (br.ReadUInt16BigEndian() == 0)
            {
                ReadSubroutineLine(br, CreateSubroutineLine(sub, 0xFFFF), sub);
            }
        }

        private void ReadSubroutineLine(BinaryReader br, SubroutineLine sl, Subroutine sub)
        {
            var lineBuffer = new byte[2048];
            BytePtr q = lineBuffer;
            int size;

            if (sub.id == 0)
            {
                sl.verb = br.ReadInt16BigEndian();
                sl.noun1 = br.ReadInt16BigEndian();
                sl.noun2 = br.ReadInt16BigEndian();
            }
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
            {
                br.ReadUInt16BigEndian();
                br.ReadUInt16BigEndian();
                br.ReadUInt16BigEndian();
            }

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
            {
                short tmp = br.ReadInt16BigEndian();
                q.WriteInt16BigEndian(0, tmp);
                while (tmp != 10000)
                {
                    if (q.ToUInt16BigEndian() == 198)
                    {
                        br.ReadUInt16BigEndian();
                    }
                    else
                    {
                        q = ReadSingleOpcode(br, q);
                    }

                    tmp = br.ReadInt16BigEndian();
                    q.WriteInt16BigEndian(0, tmp);
                }
            }
            else
            {
                while ((q.Value = br.ReadByte()) != 0xFF)
                {
                    if (q.Value == 87)
                    {
                        br.ReadUInt16BigEndian();
                    }
                    else
                    {
                        q = ReadSingleOpcode(br, q);
                    }
                }
            }

            size = q.Offset + 2;
            var data = AllocateTable(size);
            Array.Copy(lineBuffer, 0, data.Data, data.Offset, size);
        }

        private BytePtr ReadSingleOpcode(BinaryReader br, BytePtr ptr)
        {
            int i;
            ushort opcode;
            string[] table;

            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_PP)
                table = opcodeArgTable_puzzlepack;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_FF)
                table = opcodeArgTable_feeblefiles;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2 &&
                     _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
                table = opcodeArgTable_simon2talkie;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                table = opcodeArgTable_simon2dos;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                     _gd.ADGameDescription.features.HasFlag(GameFeatures.GF_TALKIE))
                table = opcodeArgTable_simon1talkie;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1)
                table = opcodeArgTable_simon1dos;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_WW)
                table = opcodeArgTable_waxworks;
            else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2)
                table = opcodeArgTable_elvira2;
            else
                table = opcodeArgTable_elvira1;

            i = 0;
            if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
            {
                opcode = ptr.ToUInt16BigEndian();
                ptr += 2;
            }
            else
            {
                opcode = ptr.Value;
                ptr.Offset++;
            }

            var stringPtr = table[opcode];
            if (stringPtr == null)
                Error("Unable to locate opcode table. Perhaps you are using the wrong game target?");

            for (;;)
            {
                if (stringPtr[i] == ' ')
                    return ptr;

                int l = stringPtr[i++];

                ushort val;
                switch (l)
                {
                    case 'F':
                    case 'N':
                    case 'S':
                    case 'a':
                    case 'n':
                    case 'p':
                    case 'v':
                    case '3':
                        val = br.ReadUInt16BigEndian();
                        ptr.WriteUInt16BigEndian(0, val);
                        ptr.Offset += 2;
                        break;

                    case 'B':
                        if (_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1)
                        {
                            val = br.ReadUInt16BigEndian();
                            ptr.WriteUInt16BigEndian(0, val);
                            ptr.Offset += 2;
                        }
                        else
                        {
                            ptr.Value = br.ReadByte();
                            ptr.Offset++;
                            if (ptr[-1] == 0xFF)
                            {
                                ptr.Value = br.ReadByte();
                                ptr.Offset++;
                            }
                        }
                        break;

                    case 'I':
                        val = br.ReadUInt16BigEndian();
                        switch (val)
                        {
                            case 1:
                                val = 0xFFFF;
                                break;
                            case 3:
                                val = 0xFFFD;
                                break;
                            case 5:
                                val = 0xFFFB;
                                break;
                            case 7:
                                val = 0xFFF9;
                                break;
                            case 9:
                                val = 0xFFF7;
                                break;
                            default:
                                val = (ushort) FileReadItemID(br);
                                break;
                        }
                        ptr.WriteUInt16BigEndian(0, val);
                        ptr.Offset += 2;
                        break;

                    case 'T':
                        val = br.ReadUInt16BigEndian();
                        switch (val)
                        {
                            case 0:
                                val = 0xFFFF;
                                break;
                            case 3:
                                val = 0xFFFD;
                                break;
                            default:
                                val = (ushort) br.ReadUInt32BigEndian();
                                break;
                        }
                        ptr.WriteUInt16BigEndian(0, val);
                        ptr.Offset += 2;
                        break;
                    default:
                        Error("readSingleOpcode: Bad cmd table entry {0}", l);
                        break;
                }
            }
        }
    }
}