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
    internal abstract class AgosEngineElvira2 : AgosEngineElvira1
    {
        public AgosEngineElvira2(ISystem system, GameSettings settings, AgosGameDescription gd)
            : base(system, settings, gd)
        {
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

        protected bool SaveGame(int slot, string caption)
        {
            int item_index, num_item, i;
            TimeEvent te;
            uint curTime = GetTime();
            uint gsc = _gameStoppedClock;

            _videoLockOut |= 0x100;

            var stream = OSystem.SaveFileManager.OpenForSaving(GenSaveName(slot));
            if (stream == null)
            {
                _videoLockOut = (ushort) (_videoLockOut & ~0x100);
                return false;
            }
            var f = new BinaryWriter(stream);
            if (GameType == SIMONGameType.GType_PP)
            {
                // No caption
            }
            else if (GameType == SIMONGameType.GType_FF)
            {
                f.WriteBytes(caption.GetBytes(), 100);
            }
            else if (GameType == SIMONGameType.GType_SIMON1 || GameType == SIMONGameType.GType_SIMON2)
            {
                f.WriteString(caption, 18);
            }
            else
            {
                f.WriteString(caption, 8);
            }

            f.WriteUInt32BigEndian((uint) (_itemArrayInited - 1));
            f.WriteUInt32BigEndian(0xFFFFFFFF);
            f.WriteUInt32BigEndian(curTime);
            f.WriteUInt32BigEndian(0);

            i = 0;
            for (te = _firstTimeStruct; te != null; te = te.next)
                i++;
            f.WriteUInt32BigEndian((uint) i);

            if (GameType == SIMONGameType.GType_FF && _clockStopped != 0)
                gsc += (GetTime() - _clockStopped);
            for (te = _firstTimeStruct; te != null; te = te.next)
            {
                f.WriteUInt32BigEndian(te.time - curTime + gsc);
                f.WriteUInt16BigEndian(te.subroutine_id);
            }

            if (GameType == SIMONGameType.GType_WW && GamePlatform == Platform.DOS)
            {
                if (_roomsListPtr != BytePtr.Null)
                {
                    BytePtr p = _roomsListPtr;
                    for (;;)
                    {
                        ushort minNum = p.ToUInt16BigEndian();
                        p += 2;
                        if (minNum == 0)
                            break;

                        ushort maxNum = p.ToUInt16BigEndian();
                        p += 2;

                        for (ushort z = minNum; z <= maxNum; z++)
                        {
                            ushort itemNum = (ushort) (z + 2);
                            Item item = DerefItem(itemNum);

                            ushort num = (ushort) (itemNum - _itemArrayInited);
                            _roomStates[num].state = (ushort) item.state;
                            _roomStates[num].classFlags = item.classFlags;
                            var subRoom = (SubRoom) FindChildOfType(item, ChildType.kRoomType);
                            _roomStates[num].roomExitStates = subRoom.roomExitStates;
                        }
                    }
                }

                for (var s = 0; s < _numRoomStates; s++)
                {
                    f.WriteUInt16BigEndian(_roomStates[s].state);
                    f.WriteUInt16BigEndian(_roomStates[s].classFlags);
                    f.WriteUInt16BigEndian(_roomStates[s].roomExitStates);
                }
                f.WriteUInt16BigEndian(0);
                f.WriteUInt16BigEndian(_currentRoom);
            }

            item_index = 1;
            for (num_item = _itemArrayInited - 1; num_item != 0; num_item--)
            {
                Item item = _itemArrayPtr[item_index++];

                if ((GameType == SIMONGameType.GType_WW && GamePlatform == Platform.Amiga) ||
                    GameType == SIMONGameType.GType_ELVIRA2)
                {
                    WriteItemID(f, item.parent);
                }
                else
                {
                    f.WriteUInt16BigEndian(item.parent);
                    f.WriteUInt16BigEndian(item.next);
                }

                f.WriteUInt16BigEndian((ushort) item.state);
                f.WriteUInt16BigEndian(item.classFlags);

                SubRoom r = (SubRoom) FindChildOfType(item, ChildType.kRoomType);
                if (r != null)
                {
                    f.WriteUInt16BigEndian(r.roomExitStates);
                }

                var sr = (SubSuperRoom) FindChildOfType(item, ChildType.kSuperRoomType);
                int j;
                if (sr != null)
                {
                    ushort n = (ushort) (sr.roomX * sr.roomY * sr.roomZ);
                    for (i = j = 0; i != n; i++)
                        f.WriteUInt16BigEndian(sr.roomExitStates[j++]);
                }

                var o = (SubObject) FindChildOfType(item, ChildType.kObjectType);
                if (o != null)
                {
                    f.WriteUInt32BigEndian((uint) o.objectFlags);
                    i = (int) (o.objectFlags & (SubObjectFlags) 1);

                    for (j = 1; j < 16; j++)
                    {
                        if (((int) o.objectFlags & (1 << j)) != 0)
                        {
                            f.WriteUInt16BigEndian((ushort) o.objectFlagValue[i++]);
                        }
                    }
                }

                var u = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
                if (u != null)
                {
                    for (i = 0; i != 4; i++)
                    {
                        f.WriteUInt16BigEndian(u.userFlags[i]);
                    }
                }
            }

            // write the variables
            for (i = 0; i != _numVars; i++)
            {
                f.WriteUInt16BigEndian((ushort) ReadVariable((ushort) i));
            }

            // write the items in item store
            for (i = 0; i != _numItemStore; i++)
            {
                if (GameType == SIMONGameType.GType_WW && GamePlatform == Platform.Amiga)
                {
                    f.WriteUInt16BigEndian((ushort) (ItemPtrToID(_itemStore[i]) * 16));
                }
                else if (GameType == SIMONGameType.GType_ELVIRA2)
                {
                    if (GamePlatform == Platform.DOS)
                    {
                        WriteItemID(f, (ushort) ItemPtrToID(_itemStore[i]));
                    }
                    else
                    {
                        f.WriteUInt16BigEndian((ushort) (ItemPtrToID(_itemStore[i]) * 18));
                    }
                }
                else
                {
                    f.WriteUInt16BigEndian((ushort) ItemPtrToID(_itemStore[i]));
                }
            }

            // Write the bits in array 1
            for (i = 0; i != _numBitArray1; i++)
                f.WriteUInt16BigEndian(_bitArray[i]);

            // Write the bits in array 2
            for (i = 0; i != _numBitArray2; i++)
                f.WriteUInt16BigEndian(_bitArrayTwo[i]);

            // Write the bits in array 3
            for (i = 0; i != _numBitArray3; i++)
                f.WriteUInt16BigEndian(_bitArrayThree[i]);

            if (GameType == SIMONGameType.GType_ELVIRA2 || GameType == SIMONGameType.GType_WW)
            {
                f.WriteUInt16BigEndian(_superRoomNumber);
            }

            f.Dispose();
            _videoLockOut = (ushort) (_videoLockOut & ~0x100);

            return true;
        }

        protected override bool LoadGame(string filename, bool restartMode = false)
        {
            byte[] ident;
            int num, item_index, i, j;

            _videoLockOut |= 0x100;

            BinaryReader f;

            if (restartMode)
            {
                // Load restart state
                var file = OpenFileRead(filename);
                f = file == null ? null : new BinaryReader(file);
            }
            else
            {
                f = new BinaryReader(OSystem.SaveFileManager.OpenForLoading(filename));
            }

            if (f == null)
            {
                _videoLockOut = (ushort) (_videoLockOut & ~0x100);
                return false;
            }

            if (GameType == SIMONGameType.GType_PP)
            {
                // No caption
            }
            else if (GameType == SIMONGameType.GType_FF)
            {
                ident = f.ReadBytes(100);
            }
            else if (GameType == SIMONGameType.GType_SIMON1 || GameType == SIMONGameType.GType_SIMON2)
            {
                ident = f.ReadBytes(18);
            }
            else if (!restartMode)
            {
                ident = f.ReadBytes(8);
            }

            num = (int) f.ReadUInt32BigEndian();

            if (f.ReadUInt32BigEndian() != 0xFFFFFFFF || num != _itemArrayInited - 1)
            {
                f.Dispose();
                _videoLockOut = (ushort) (_videoLockOut & ~0x100);
                return false;
            }

            f.ReadUInt32BigEndian();
            f.ReadUInt32BigEndian();
            _noParentNotify = true;

            // add all timers
            KillAllTimers();
            for (num = (int) f.ReadUInt32BigEndian(); num != 0; num--)
            {
                uint timeout = f.ReadUInt32BigEndian();
                ushort subroutine_id = f.ReadUInt16BigEndian();
                AddTimeEvent((ushort) timeout, subroutine_id);
            }

            if (GameType == SIMONGameType.GType_WW && GamePlatform == Platform.DOS)
            {
                for (uint s = 0; s < _numRoomStates; s++)
                {
                    _roomStates[s].state = f.ReadUInt16BigEndian();
                    _roomStates[s].classFlags = f.ReadUInt16BigEndian();
                    _roomStates[s].roomExitStates = f.ReadUInt16BigEndian();
                }
                f.ReadUInt16BigEndian();

                ushort room = _currentRoom;
                _currentRoom = f.ReadUInt16BigEndian();
                if (_roomsListPtr != BytePtr.Null)
                {
                    var p = _roomsListPtr;
                    if (room == _currentRoom)
                    {
                        for (;;)
                        {
                            ushort minNum = p.ToUInt16BigEndian();
                            p += 2;
                            if (minNum == 0)
                                break;

                            ushort maxNum = p.ToUInt16BigEndian();
                            p += 2;

                            for (ushort z = minNum; z <= maxNum; z++)
                            {
                                ushort itemNum = (ushort) (z + 2);
                                Item item = DerefItem(itemNum);

                                num = (itemNum - _itemArrayInited);
                                item.state = (short) _roomStates[num].state;
                                item.classFlags = _roomStates[num].classFlags;
                                var subRoom = (SubRoom) FindChildOfType(item, ChildType.kRoomType);
                                subRoom.roomExitStates = _roomStates[num].roomExitStates;
                            }
                        }
                    }
                    else
                    {
                        for (;;)
                        {
                            ushort minNum = p.ToUInt16BigEndian();
                            p += 2;
                            if (minNum == 0)
                                break;

                            ushort maxNum = p.ToUInt16BigEndian();
                            p += 2;

                            for (ushort z = minNum; z <= maxNum; z++)
                            {
                                ushort itemNum = (ushort) (z + 2);
                                _itemArrayPtr[itemNum] = null;
                            }
                        }
                    }
                }

                if (room != _currentRoom)
                {
                    _roomsListPtr = BytePtr.Null;
                    LoadRoomItems(_currentRoom);
                }
            }

            item_index = 1;
            for (num = _itemArrayInited - 1; num != 0; num--)
            {
                Item item = _itemArrayPtr[item_index++], parent_item;

                if ((GameType == SIMONGameType.GType_WW && GamePlatform == Platform.Amiga) ||
                    GameType == SIMONGameType.GType_ELVIRA2)
                {
                    parent_item = DerefItem(ReadItemID(f));
                    SetItemParent(item, parent_item);
                }
                else
                {
                    uint parent = f.ReadUInt16BigEndian();
                    uint next = f.ReadUInt16BigEndian();

                    if (GameType == SIMONGameType.GType_WW && GamePlatform == Platform.DOS &&
                        DerefItem(item.parent) == null)
                        item.parent = 0;

                    parent_item = DerefItem(parent);
                    SetItemParent(item, parent_item);

                    if (parent_item == null)
                    {
                        item.parent = (ushort) parent;
                        item.next = (ushort) next;
                    }
                }

                item.state = (short) f.ReadUInt16BigEndian();
                item.classFlags = f.ReadUInt16BigEndian();

                var r = (SubRoom) FindChildOfType(item, ChildType.kRoomType);
                if (r != null)
                {
                    r.roomExitStates = f.ReadUInt16BigEndian();
                }

                var sr = (SubSuperRoom) FindChildOfType(item, ChildType.kSuperRoomType);
                if (sr != null)
                {
                    ushort n = (ushort) (sr.roomX * sr.roomY * sr.roomZ);
                    for (i = j = 0; i != n; i++)
                        sr.roomExitStates[j++] = f.ReadUInt16BigEndian();
                }

                var o = (SubObject) FindChildOfType(item, ChildType.kObjectType);
                if (o != null)
                {
                    o.objectFlags = (SubObjectFlags) f.ReadUInt32BigEndian();
                    i = (int) (o.objectFlags & (SubObjectFlags) 1);

                    for (j = 1; j < 16; j++)
                    {
                        if ((o.objectFlags & (SubObjectFlags) (1 << j)) != (SubObjectFlags) 0)
                        {
                            o.objectFlagValue[i++] = (short) f.ReadUInt16BigEndian();
                        }
                    }
                }

                var u = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
                if (u != null)
                {
                    for (i = 0; i != 4; i++)
                    {
                        u.userFlags[i] = f.ReadUInt16BigEndian();
                    }
                }
            }

            // read the variables
            for (i = 0; i != _numVars; i++)
            {
                WriteVariable((ushort) i, f.ReadUInt16BigEndian());
            }

            // read the items in item store
            for (i = 0; i != _numItemStore; i++)
            {
                if (GameType == SIMONGameType.GType_WW && GamePlatform == Platform.Amiga)
                {
                    _itemStore[i] = DerefItem((uint) (f.ReadUInt16BigEndian() / 16));
                }
                else if (GameType == SIMONGameType.GType_ELVIRA2)
                {
                    if (GamePlatform == Platform.DOS)
                    {
                        _itemStore[i] = DerefItem(ReadItemID(f));
                    }
                    else
                    {
                        _itemStore[i] = DerefItem((uint) (f.ReadUInt16BigEndian() / 18));
                    }
                }
                else
                {
                    _itemStore[i] = DerefItem(f.ReadUInt16BigEndian());
                }
            }

            // Read the bits in array 1
            for (i = 0; i != _numBitArray1; i++)
                _bitArray[i] = f.ReadUInt16BigEndian();

            // Read the bits in array 2
            for (i = 0; i != _numBitArray2; i++)
                _bitArrayTwo[i] = f.ReadUInt16BigEndian();

            // Read the bits in array 3
            for (i = 0; i != _numBitArray3; i++)
                _bitArrayThree[i] = f.ReadUInt16BigEndian();

            if (GameType == SIMONGameType.GType_ELVIRA2 || GameType == SIMONGameType.GType_WW)
            {
                _superRoomNumber = f.ReadUInt16BigEndian();
            }

            f.Dispose();

            _noParentNotify = false;

            _videoLockOut = (ushort) (_videoLockOut & ~0x100);

            // The floppy disk versions of Simon the Sorcerer 2 block changing
            // to scrolling rooms, if the copy protection fails. But the copy
            // protection flags are never set in the CD version.
            // Setting this copy protection flag, allows saved games to be shared
            // between all versions of Simon the Sorcerer 2.
            if (GameType == SIMONGameType.GType_SIMON2)
            {
                SetBitFlag(135, true);
            }

            return true;
        }

        protected override bool HasIcon(Item item)
        {
            var child = (SubObject) FindChildOfType(item, ChildType.kObjectType);
            return child != null && child.objectFlags.HasFlag(SubObjectFlags.kOFIcon);
        }

        protected override int SetupIconHitArea(WindowBlock window, uint num, int x, int y, Item itemPtr)
        {
            var ha = FindEmptyHitArea();
            ha.Value.x = (ushort) ((x + window.x) * 8);
            ha.Value.y = (ushort) (y * 25 + window.y);
            ha.Value.itemPtr = itemPtr;
            ha.Value.width = 24;
            ha.Value.height = 24;
            ha.Value.flags = BoxFlags.kBFDragBox | BoxFlags.kBFBoxInUse | BoxFlags.kBFBoxItem;
            ha.Value.id = 0x7FFD;
            ha.Value.priority = 100;
            ha.Value.verb = 208;

            return Array.IndexOf(_hitAreas, ha.Value);
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