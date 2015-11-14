//
//  ScummEngine_Object.cs
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
using NScumm.Core.Graphics;

namespace NScumm.Scumm
{
    partial class ScummEngine3
    {
        void DrawObject()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);

            int xpos, ypos, state;
            DrawObjectCore(out xpos, out ypos, out state);

            var idx = GetObjectIndex(obj);
            if (idx == -1)
                return;

            var od = _objs[idx];
            if (xpos != 0xFF)
            {
                var wx = od.Walk.X + (xpos * 8) - od.Position.X;
                var wy = od.Walk.Y + (ypos * 8) - od.Position.Y;
                od.Walk = new Point((short)wx, (short)wy);
                od.Position = new Point((short)(xpos * 8), (short)(ypos * 8));
            }

            AddObjectToDrawQue((byte)idx);

            var pos = od.Position;
            var w = od.Width;
            var h = od.Height;

            var i = _objs.Length - 1;
            do
            {
                if (_objs[i].Number != 0 &&
                    _objs[i].Position == pos &&
                    _objs[i].Width == w && _objs[i].Height == h)
                    PutState(_objs[i].Number, 0);
            } while ((--i) != 0);

            PutState(obj, state);
        }

        void FindObject()
        {
            GetResult();
            var x = GetVarOrDirectByte(OpCodeParameter.Param1);
            var y = GetVarOrDirectByte(OpCodeParameter.Param2);
            SetResult(FindObjectCore(x, y));
        }

        void SetState()
        {
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            var state = (byte)GetVarOrDirectByte(OpCodeParameter.Param2);
            PutState(obj, state);
            MarkObjectRectAsDirty(obj);
            if (_bgNeedsRedraw)
                ClearDrawObjectQueue();
        }

        void SetClass()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);
            int cls;

            while ((_opCode = ReadByte()) != 0xFF)
            {
                cls = GetVarOrDirectWord(OpCodeParameter.Param1);

                // WORKAROUND bug #1668393: Due to a script bug, the wrong opcode is
                // used to test and set the state of various objects (e.g. the inside
                // door (object 465) of the of the Hostel on Mars), when opening the
                // Hostel door from the outside.
                if (cls == 0)
                {
                    // Class '0' means: clean all class data
                    ClassData[obj] = 0;
                    if (Game.Version < 5 && IsActor(obj))
                    {
                        var a = Actors[obj];
                        a.IgnoreBoxes = false;
                        a.ForceClip = 0;
                    }
                }
                else
                {
                    PutClass(obj, cls, (cls & 0x80) != 0);
                }
            }
        }

        void IfClassOfIs()
        {
            var cond = true;
            var obj = GetVarOrDirectWord(OpCodeParameter.Param1);

            while ((_opCode = ReadByte()) != 0xFF)
            {
                var cls = GetVarOrDirectWord(OpCodeParameter.Param1);
                var b = GetClass(obj, (ObjectClass)cls);
                if ((((cls & 0x80) != 0) && !b) || ((0 == (cls & 0x80)) && b))
                    cond = false;
            }
            JumpRelative(cond);
        }

        protected virtual void PickupObject()
        {
            int obj = GetVarOrDirectWord(OpCodeParameter.Param1);

            if (obj < 1)
            {
                string msg = string.Format("pickupObjectOld received invalid index {0} (script {1})", obj, Slots[CurrentScript].Number);
                throw new NotSupportedException(msg);
            }

            if (GetObjectIndex(obj) == -1)
                return;

            // Don't take an object twice
            if (GetWhereIsObject(obj) == WhereIsObject.Inventory)
                return;

            // debug(0, "adding %d from %d to inventoryOld", obj, _currentRoom);
            AddObjectToInventory(obj, _roomResource);
            MarkObjectRectAsDirty(obj);
            PutOwner(obj, (byte)Variables[VariableEgo.Value]);
            PutClass(obj, (int)ObjectClass.Untouchable, true);
            PutState(obj, 1);
            ClearDrawObjectQueue();
            RunInventoryScript(1);
        }

        void IfState()
        {
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            int b = GetVarOrDirectByte(OpCodeParameter.Param2);

            JumpRelative(GetStateCore(a) == b);
        }

        void IfNotState()
        {
            int a = GetVarOrDirectWord(OpCodeParameter.Param1);
            int b = GetVarOrDirectByte(OpCodeParameter.Param2);

            JumpRelative(GetStateCore(a) != b);
        }
    }
}

