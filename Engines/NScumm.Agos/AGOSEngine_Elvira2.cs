//
//  AGOSEngine_Elvira2.cs
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
using NScumm.Core.IO;

namespace NScumm.Agos
{
    internal abstract class AGOSEngine_Elvira2 : AGOSEngine_Elvira1
    {
        public AGOSEngine_Elvira2(ISystem system, GameSettings settings, AGOSGameDescription gd)
            : base(system, settings, gd)
        {
        }

        protected override bool HasIcon(Item item)
        {
            var child = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            return child != null && child.objectFlags.HasFlag(SubObjectFlags.kOFIcon);
        }

        protected void oe2_ink()
        {
            // 160
            SetTextColor(GetVarOrByte());
        }

        protected void oe2_doTable()
        {
            // 143: start item sub
            Item i = GetNextItemPtr();

            var r = (SubRoom) FindChildOfType(i, ChildType.kRoomType);
            if (r != null)
            {
                var sub = GetSubroutineByID(r.subroutine_id);
                if (sub != null)
                {
                    StartSubroutine(sub);
                    return;
                }
            }

            if (GameType == SIMONGameType.GType_ELVIRA2)
            {
                var sr = (SubSuperRoom) FindChildOfType(i, ChildType.kSuperRoomType);
                if (sr != null)
                {
                    Subroutine sub = GetSubroutineByID(sr.subroutine_id);
                    if (sub != null)
                    {
                        StartSubroutine(sub);
                    }
                }
            }
        }

        protected void oe2_storeItem()
        {
            // 151: set array6 to item
            uint var = GetVarOrByte();
            Item item = GetNextItemPtr();
            _itemStore[var] = item;
        }

        protected void oe2_getItem()
        {
            // 152: set m1 to m3 to array 6
            Item item = _itemStore[GetVarOrByte()];
            uint var = GetVarOrByte();
            if (var == 1)
            {
                _subjectItem = item;
            }
            else
            {
                _objectItem = item;
            }
        }

        protected void oe2_bSet()
        {
            // 153: set bit
            SetBitFlag((int) GetVarWrapper(), true);
        }

        protected void oe2_bClear()
        {
            // 154: clear bit
            SetBitFlag((int) GetVarWrapper(), false);
        }

        protected void oe2_bZero()
        {
            // 155: is bit clear
            SetScriptCondition(!GetBitFlag((int) GetVarWrapper()));
        }

        protected void oe2_bNotZero()
        {
            // 156: is bit set
            int bit = (int) GetVarWrapper();

            // WORKAROUND: Enable copy protection again, in cracked version.
            if (GameType == SIMONGameType.GType_SIMON1 && _currentTable != null &&
                _currentTable.id == 2962 && bit == 63)
            {
                bit = 50;
            }

            SetScriptCondition(GetBitFlag(bit));
        }

        protected void oe2_getOValue()
        {
            // 157: get item int prop
            Item item = GetNextItemPtr();
            SubObject subObject = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            int prop = (int) GetVarOrByte();

            if (subObject != null && subObject.objectFlags.HasFlag((SubObjectFlags) (1 << prop)) && prop < 16)
            {
                int offs = GetOffsetOfChild2Param(subObject, 1 << prop);
                WriteNextVarContents((ushort) subObject.objectFlagValue[offs]);
            }
            else
            {
                WriteNextVarContents(0);
            }
        }

        protected void oe2_setOValue()
        {
            // 158: set item prop
            Item item = GetNextItemPtr();
            SubObject subObject = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            int prop = (int) GetVarOrByte();
            int value = (int) GetVarOrWord();

            if (subObject != null && subObject.objectFlags.HasFlag((SubObjectFlags) (1 << prop)) && prop < 16)
            {
                int offs = GetOffsetOfChild2Param(subObject, 1 << prop);
                subObject.objectFlagValue[offs] = (short) value;
            }
        }

        protected void oe2_getDollar2()
        {
            // 175
            _showPreposition = true;

            SetupCondCHelper();

            _objectItem = _hitAreaObjectItem;

            if (_objectItem == _dummyItem2)
                _objectItem = Me();

            if (_objectItem == _dummyItem3)
                _objectItem = DerefItem(Me().parent);

            if (_objectItem != null)
            {
                _scriptNoun2 = _objectItem.noun;
                _scriptAdj2 = _objectItem.adjective;
            }
            else
            {
                _scriptNoun2 = -1;
                _scriptAdj2 = -1;
            }

            _showPreposition = false;
        }

        protected void oe2_isAdjNoun()
        {
            // 179: item unk1 unk2 is
            Item item = GetNextItemPtr();
            short a = (short) GetNextWord();
            short n = (short) GetNextWord();

            if (GameType == SIMONGameType.GType_ELVIRA2 && item == null)
            {
                // WORKAROUND bug #1745996: A NULL item can occur when
                // interacting with items in the dinning room
                SetScriptCondition(false);
                return;
            }

            System.Diagnostics.Debug.Assert(item != null);
            SetScriptCondition(item.adjective == a && item.noun == n);
        }

        protected void oe2_b2Set()
        {
            // 180: set bit2
            int bit = (int) GetVarOrByte();
            _bitArrayTwo[bit / 16] = (ushort) (_bitArrayTwo[bit / 16] | (1 << (bit & 15)));
        }

        protected void oe2_b2Clear()
        {
            // 181: clear bit2
            int bit = (int) GetVarOrByte();
            _bitArrayTwo[bit / 16] = (ushort) (_bitArrayTwo[bit / 16] & ~(1 << (bit & 15)));
        }

        protected void oe2_b2Zero()
        {
            // 182: is bit2 clear
            int bit = (int) GetVarOrByte();
            SetScriptCondition((_bitArrayTwo[bit / 16] & (1 << (bit & 15))) == 0);
        }

        protected void oe2_b2NotZero()
        {
            // 183: is bit2 set
            int bit = (int) GetVarOrByte();
            SetScriptCondition((_bitArrayTwo[bit / 16] & (1 << (bit & 15))) != 0);
        }

        protected override int SetupIconHitArea(WindowBlock window, uint num, int x, int y, Item itemPtr)
        {
            var ha = FindEmptyHitArea();
            ha.Value.x = (ushort) ((x + window.x) * 8);
            ha.Value.y = (ushort) (y * 25 + window.y);
            ha.Value.itemPtr = itemPtr;
            ha.Value.width = 24;
            ha.Value.height = 24;
            ha.Value.flags = (ushort) (BoxFlags.kBFDragBox | BoxFlags.kBFBoxInUse | BoxFlags.kBFBoxItem);
            ha.Value.id = 0x7FFD;
            ha.Value.priority = 100;
            ha.Value.verb = 208;

            return Array.IndexOf(_hitAreas, ha);
        }

        protected override int ItemGetIconNumber(Item item)
        {
            var child = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            int offs;

            if (child == null || !child.objectFlags.HasFlag(SubObjectFlags.kOFIcon))
                return 0;

            offs = GetOffsetOfChild2Param(child, 0x10);
            return child.objectFlagValue[offs];
        }

        protected override void ReadItemChildren(BinaryReader br, Item item, ChildType type)
        {
            if (type == ChildType.kRoomType)
            {
                int fr1 = br.ReadUInt16BigEndian();
                int fr2 = br.ReadUInt16BigEndian();
                int i;
                int j, k;

                var subRoom = AllocateChildBlock<SubRoom>(item, ChildType.kRoomType);
                subRoom.subroutine_id = (ushort) fr1;
                subRoom.roomExitStates = (ushort) fr2;

                for (i = k = 0, j = fr2; i != 6; i++, j >>= 2)
                    if ((j & 3) != 0)
                        subRoom.roomExit[k++] = (ushort) FileReadItemID(br);
            }
            else if (type == ChildType.kObjectType)
            {
                int fr = br.ReadInt32BigEndian();
                int i;

                var subObject = AllocateChildBlock<SubObject>(item, ChildType.kObjectType);
                subObject.objectFlags = (SubObjectFlags) fr;

                if ((fr & 1) != 0)
                {
                    subObject.objectFlagValue.Add((short) br.ReadUInt32BigEndian());
                }
                for (i = 1; i != 16; i++)
                    if ((fr & (1 << i)) != 0)
                        subObject.objectFlagValue.Add(br.ReadInt16BigEndian());

                if (_gd.ADGameDescription.gameType != SIMONGameType.GType_ELVIRA2)
                    subObject.objectName = (ushort) br.ReadUInt32BigEndian();
            }
            else if (type == ChildType.kSuperRoomType)
            {
                System.Diagnostics.Debug.Assert(_gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA2);

                int i, j, k;
                int id, x, y, z;

                id = br.ReadUInt16BigEndian();
                x = br.ReadUInt16BigEndian();
                y = br.ReadUInt16BigEndian();
                z = br.ReadUInt16BigEndian();

                j = x * y * z;

                var subSuperRoom = AllocateChildBlock<SubSuperRoom>(item, ChildType.kSuperRoomType);
                subSuperRoom.subroutine_id = (ushort) id;
                subSuperRoom.roomX = (ushort) x;
                subSuperRoom.roomY = (ushort) y;
                subSuperRoom.roomZ = (ushort) z;

                for (i = k = 0; i != j; i++)
                    subSuperRoom.roomExitStates[k++] = br.ReadUInt16BigEndian();
            }
            else if (type == ChildType.kContainerType)
            {
                var container = AllocateChildBlock<SubContainer>(item, ChildType.kContainerType);
                container.volume = br.ReadUInt16BigEndian();
                container.flags = br.ReadUInt16BigEndian();
            }
            else if (type == ChildType.kChainType)
            {
                var chain = AllocateChildBlock<SubChain>(item, ChildType.kChainType);
                chain.chChained = (ushort) FileReadItemID(br);
            }
            else if (type == ChildType.kUserFlagType)
            {
                SetUserFlag(item, 0, br.ReadUInt16BigEndian());
                SetUserFlag(item, 1, br.ReadUInt16BigEndian());
                SetUserFlag(item, 2, br.ReadUInt16BigEndian());
                SetUserFlag(item, 3, br.ReadUInt16BigEndian());
            }
            else if (type == ChildType.kInheritType)
            {
                var inherit = AllocateChildBlock<SubInherit>(item, ChildType.kInheritType);
                inherit.inMaster = (ushort) FileReadItemID(br);
            }
            else
            {
                DebugHelper.Error("readItemChildren: invalid type {0}", type);
            }
        }
    }
}