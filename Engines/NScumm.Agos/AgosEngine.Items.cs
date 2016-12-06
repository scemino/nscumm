//
//  AgosEngine.Items.cs
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
using static NScumm.Core.DebugHelper;

namespace NScumm.Agos
{
    partial class AgosEngine
    {
        protected T AllocateChildBlock<T>(Item i, ChildType type) where T : Child, new()
        {
            var child = AllocateItem<T>();
            child.next = i.children;
            i.children = child;
            child.type = type;
            return child;
        }

        private T AllocateItem<T>() where T : new()
        {
            var ptr = new T();
            _itemHeap.PushBack(ptr);
            return ptr;
        }

        private void AllocItemHeap()
        {
            _itemHeap.Clear();
        }

        protected virtual bool HasIcon(Item item)
        {
            return GetUserFlag(item, 7) != 0;
        }

        protected virtual int ItemGetIconNumber(Item item)
        {
            return GetUserFlag(item, 7);
        }

        private void SetItemState(Item item, int value)
        {
            item.state = (short) value;
        }

        private void CreatePlayer()
        {
            _currentPlayer = _itemArrayPtr[1];
            _currentPlayer.adjective = -1;
            _currentPlayer.noun = 10000;

            var p = AllocateChildBlock<SubPlayer>(_currentPlayer, ChildType.kPlayerType);
            if (p == null)
                Error("createPlayer: player create failure");

            p.size = 0;
            p.weight = 0;
            p.strength = 6000;
            p.flags = 1; // Male
            p.level = 1;
            p.score = 0;

            SetUserFlag(_currentPlayer, 0, 0);
        }

        protected Child FindChildOfType(Item i, ChildType type)
        {
            Item b = null;
            var child = i.children;

            while (child != null)
            {
                if (child.type == type)
                    return child;
                if (child.type == (ChildType) 255)
                    b = DerefItem(((SubInherit) child).inMaster);
                child = child.next;
            }
            if (b == null) return null;

            child = b.children;
            while (child != null)
            {
                if (child.type == type)
                    return child;
                child = child.next;
            }

            return null;
        }

        private int GetUserFlag(Item item, int a)
        {
            var subUserFlag = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
            if (subUserFlag == null)
                return 0;

            int max = _gd.ADGameDescription.gameType == SIMONGameType.GType_ELVIRA1 ? 7 : 3;
            if (a < 0 || a > max)
                return 0;

            return subUserFlag.userFlags[a];
        }

        protected int GetUserFlag1(Item item, int a)
        {
            if (item == null || item == _dummyItem2 || item == _dummyItem3)
                return -1;

            var subUserFlag = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
            if (subUserFlag == null)
                return 0;

            if (a < 0 || a > 7)
                return 0;

            return subUserFlag.userFlags[a];
        }

        protected void SetUserFlag(Item item, int a, int b)
        {
            var subUserFlag = (SubUserFlag) FindChildOfType(item, ChildType.kUserFlagType);
            if (subUserFlag == null)
            {
                subUserFlag =
                    AllocateChildBlock<SubUserFlag>(item, ChildType.kUserFlagType);
            }

            if (a < 0 || a > 7)
                return;

            subUserFlag.userFlags[a] = (ushort) b;
        }

        private bool IsRoom(Item item)
        {
            return FindChildOfType(item, ChildType.kRoomType) != null;
        }

        private bool IsObject(Item item)
        {
            return FindChildOfType(item, ChildType.kObjectType) != null;
        }

        protected int GetOffsetOfChild2Param(SubObject child, int prop)
        {
            int m = 1;
            int offset = 0;
            while (m != prop)
            {
                if ((child.objectFlags & (SubObjectFlags) m) != 0)
                    offset++;
                m *= 2;
            }
            return offset;
        }

        protected Item Me()
        {
            if (_currentPlayer != null)
                return _currentPlayer;
            return _dummyItem1;
        }

        private Item Actor()
        {
            Error("actor: is this code ever used?");
            //if (_actorPlayer)
            //	return _actorPlayer;
            return _dummyItem1; // for compilers that don't support NORETURN
        }

        protected Item GetNextItemPtr()
        {
            int a = GetNextWord();
            switch (a)
            {
                case -1:
                    return _subjectItem;
                case -3:
                    return _objectItem;
                case -5:
                    return Me();
                case -7:
                    return Actor();
                case -9:
                    return DerefItem(Me().parent);
                default:
                    return DerefItem((uint) a);
            }
        }

        private Item GetNextItemPtrStrange()
        {
            int a = GetNextWord();
            switch (a)
            {
                case -1:
                    return _subjectItem;
                case -3:
                    return _objectItem;
                case -5:
                    return _dummyItem2;
                case -7:
                    return null;
                case -9:
                    return _dummyItem3;
                default:
                    return DerefItem((uint) a);
            }
        }

        private int GetNextItemID()
        {
            int a = GetNextWord();
            switch (a)
            {
                case -1:
                    return ItemPtrToID(_subjectItem);
                case -3:
                    return ItemPtrToID(_objectItem);
                case -5:
                    return GetItem1ID();
                case -7:
                    return 0;
                case -9:
                    return Me().parent;
                default:
                    return a;
            }
        }

        protected void SetItemParent(Item item, Item parent)
        {
            Item old_parent = DerefItem(item.parent);

            if (item == parent)
                Error("setItemParent: Trying to set item as its own parent");

            // unlink it if it has a parent
            if (old_parent != null)
                UnlinkItem(item);
            ItemChildrenChanged(old_parent);
            LinkItem(item, parent);
            ItemChildrenChanged(parent);
        }

        private void ItemChildrenChanged(Item item)
        {
            int i;

            if (_noParentNotify)
                return;

            MouseOff();

            for (i = 0; i != 8; i++)
            {
                var window = _windowArray[i];
                if (window?.iconPtr != null && window.iconPtr.itemRef == item)
                {
                    if (_fcsData1[i] != 0)
                    {
                        _fcsData2[i] = true;
                    }
                    else
                    {
                        _fcsData2[i] = false;
                        DrawIconArray(i, item, window.iconPtr.line, window.iconPtr.classMask);
                    }
                }
            }

            MouseOn();
        }

        private void UnlinkItem(Item item)
        {
            Item first, parent, next;

            // can't unlink item without parent
            if (item.parent == 0)
                return;

            // get parent and first child of parent
            parent = DerefItem(item.parent);
            first = DerefItem(parent.child);

            // the node to remove is first in the parent's children?
            if (first == item)
            {
                parent.child = item.next;
                item.parent = 0;
                item.next = 0;
                return;
            }

            for (;;)
            {
                if (first == null)
                    Error("unlinkItem: parent empty");
                if (first.next == 0)
                    Error("unlinkItem: parent does not contain child");

                next = DerefItem(first.next);
                if (next == item)
                {
                    first.next = next.next;
                    item.parent = 0;
                    item.next = 0;
                    return;
                }
                first = next;
            }
        }

        private void LinkItem(Item item, Item parent)
        {
            uint id;
            // Don't allow that an item that is already linked is relinked
            if (item.parent != 0)
                return;

            id = (uint) ItemPtrToID(parent);
            item.parent = (ushort) id;

            if (parent != null)
            {
                item.next = parent.child;
                parent.child = (ushort) ItemPtrToID(item);
            }
            else
            {
                item.next = 0;
            }
        }

        protected Item DerefItem(uint item)
        {
            if (item >= _itemArraySize)
                Error("derefItem: invalid item {0}", item);
            return _itemArrayPtr[item];
        }

        private Item NextInByClass(Item i, short m)
        {
            i = _findNextPtr;
            while (i != null)
            {
                if ((i.classFlags & m) != 0)
                {
                    _findNextPtr = DerefItem(i.next);
                    return i;
                }
                if (m == 0)
                {
                    _findNextPtr = DerefItem(i.next);
                    return i;
                }
                i = DerefItem(i.next);
            }
            return null;
        }

        protected int ItemPtrToID(Item id)
        {
            int i;
            for (i = 0; i != _itemArraySize; i++)
                if (_itemArrayPtr[i] == id)
                    return i;
            Error("itemPtrToID: not found");
            return 0; // for compilers that don't support NORETURN
        }
    }
}