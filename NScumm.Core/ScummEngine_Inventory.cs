//
//  ScummEngine_Inventory.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;

namespace NScumm.Core
{
    partial class ScummEngine
    {
        readonly ushort[] _inventory = new ushort[NumInventory];
        ObjectData[] _invData = new ObjectData[NumInventory];

        void GetInventoryCount()
        {
            GetResult();
            SetResult(GetInventoryCount(GetVarOrDirectByte(OpCodeParameter.Param1)));
        }

        void FindInventory()
        {
            GetResult();
            int x = GetVarOrDirectByte(OpCodeParameter.Param1);
            int y = GetVarOrDirectByte(OpCodeParameter.Param2);
            SetResult(FindInventory(x, y));
        }

        int GetInventorySlot()
        {
            for (int i = 0; i < NumInventory; i++)
            {
                if (_inventory[i] == 0)
                    return i;
            }
            return -1;
        }

        protected void AddObjectToInventory(int obj, byte room)
        {
            var slot = GetInventorySlot();
            if (GetWhereIsObject(obj) == WhereIsObject.FLObject)
            {
                GetObjectIndex(obj);
                throw new NotImplementedException();
            }
            else
            {
                var objs = _resManager.GetRoom(room).Objects;
                var objFound = (from o in objs
                                where o.Number == obj
                                select o).FirstOrDefault();
                _invData[slot] = objFound;
            }
            _inventory[slot] = (ushort)obj;
        }

        int GetInventoryCount(int owner)
        {
            int i, obj;
            int count = 0;
            for (i = 0; i < NumInventory; i++)
            {
                obj = _inventory[i];
                if (obj != 0 && GetOwner(obj) == owner)
                    count++;
            }
            return count;
        }

        int FindInventory(int owner, int idx)
        {
            int count = 1, i, obj;
            for (i = 0; i < NumInventory; i++)
            {
                obj = _inventory[i];
                if (obj != 0 && GetOwner(obj) == owner && count++ == idx)
                    return obj;
            }
            return 0;
        }
    }
}

