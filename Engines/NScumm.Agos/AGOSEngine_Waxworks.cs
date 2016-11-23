//
//  AGOSEngine_Waxworks.cs
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

using System.Text;
using NScumm.Core;
using NScumm.Core.IO;

namespace NScumm.Agos
{
    internal abstract class AGOSEngine_Waxworks : AGOSEngine_Elvira2
    {
        public AGOSEngine_Waxworks(ISystem system, GameSettings settings, AGOSGameDescription gd)
            : base(system, settings, gd)
        {
        }

        protected void oww_addTextBox()
        {
            // 65: add hit area
            int id = (int) GetVarOrWord();
            int x = (int) GetVarOrWord();
            int y = (int) GetVarOrWord();
            int w = (int) GetVarOrWord();
            int h = (int) GetVarOrWord();
            int number = (int) GetVarOrByte();
            if (number < _numTextBoxes)
                DefineBox(id, x, y, w, h, (number << 8) + 129, 208, _dummyItem2);
        }

        protected void oww_setShortText()
        {
            // 66: set item name
            uint var = GetVarOrByte();
            uint stringId = GetNextStringID();
            if (var < _numTextBoxes)
            {
                _shortText[var] = (ushort) stringId;
            }
        }

        protected void oww_setLongText()
        {
            // 67: set item description
            int var = (int) GetVarOrByte();
            uint stringId = GetNextStringID();
            if (Features.HasFlag(GameFeatures.GF_TALKIE))
            {
                uint speechId = (uint) GetNextWord();
                if (var < _numTextBoxes)
                {
                    _longText[var] = (ushort) stringId;
                    _longSound[var] = (ushort) speechId;
                }
            }
            else
            {
                if (var < _numTextBoxes)
                {
                    _longText[var] = (ushort) stringId;
                }
            }
        }

        protected void oww_printLongText()
        {
            // 70: show string from array
            var str = GetStringPtrById(_longText[GetVarOrByte()]);
            ShowMessageFormat("%s\n", str);
        }

        protected void oww_lockZones()
        {
            // 189: lock zone
            _vgaMemBase = _vgaMemPtr;
        }

        protected void oww_unlockZones()
        {
            // 190: unlock zone
            _vgaMemPtr = _vgaFrozenBase;
            _vgaMemBase = _vgaFrozenBase;
        }

        protected override bool LoadTablesIntoMem(ushort subrId)
        {
            var p = _tblList;
            if (p == BytePtr.Null)
                return false;

            while (p.Value != 0)
            {
                var filename = new StringBuilder();
                while (p.Value != 0)
                {
                    filename.Append((char) p.Value);
                    p.Offset++;
                }
                p.Offset++;

                if (_gd.Platform == Platform.Acorn)
                {
                    filename.Append(".DAT");
                }

                for (;;)
                {
                    uint minNum = p.ToUInt16BigEndian();
                    p.Offset += 2;
                    if (minNum == 0)
                        break;

                    uint maxNum = p.ToUInt16BigEndian();
                    p.Offset += 2;

                    if (subrId < minNum || subrId > maxNum) continue;

                    _subroutineList = _subroutineListOrg;
                    _tablesHeapPtr = _tablesHeapPtrOrg;
                    _tablesHeapCurPos = _tablesHeapCurPosOrg;
                    _stringIdLocalMin = 1;
                    _stringIdLocalMax = 0;

                    var @in = OpenTablesFile(filename.ToString());
                    ReadSubroutineBlock(@in);
                    CloseTablesFile(@in);
                    if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON2)
                    {
                        // TODO: vs
//                        _sound.LoadSfxTable(GetFileName(GameFileTypes.GAME_GMEFILE),
//                            _gameOffsetsPtr[atoi(filename + 6) - 1 + _soundIndexBase]);
                    }
                    else if (_gd.ADGameDescription.gameType == SIMONGameType.GType_SIMON1 &&
                             _gd.Platform == Platform.Windows)
                    {
                        filename[0] = 'S';
                        filename[1] = 'F';
                        filename[2] = 'X';
                        filename[3] = 'X';
                        filename[4] = 'X';
                        filename[5] = 'X';
                        var tmp = filename.ToString().Substring(6);
                        if (int.Parse(tmp) != 1 && int.Parse(tmp) != 30)
                            _sound.ReadSfxFile(filename.ToString());
                    }

                    AlignTableMem();

                    _tablesheapPtrNew = _tablesHeapPtr;
                    _tablesHeapCurPosNew = _tablesHeapCurPos;

                    if (_tablesHeapCurPos > _tablesHeapSize)
                        DebugHelper.Error("loadTablesIntoMem: Out of table memory");
                    return true;
                }
            }

            DebugHelper.Debug(1, "loadTablesIntoMem: didn't find {0}", subrId);
            return false;
        }
    }
}