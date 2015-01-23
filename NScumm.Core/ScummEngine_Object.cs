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
using System.Linq;
using NScumm.Core.Graphics;
using System.Collections.Generic;

namespace NScumm.Core
{
    partial class ScummEngine
    {
        protected ObjectData[] _objs = new ObjectData[200];
        List<byte> _drawingObjects = new List<byte>();
        Dictionary<int, byte[]> _newNames = new Dictionary<int, byte[]>();

        internal uint[] ClassData
        {
            get { return _resManager.ClassData; }
        }

        byte[] GetObjectOrActorName(int num)
        {
            byte[] name;
            if (num < Actors.Length)
            {
                name = Actors[num].Name;
            }
            else if (_newNames.ContainsKey(num))
            {
                name = _newNames[num];
            }
            else
            {
                var obj = (from o in _invData
                                       where o != null && o.Number == num
                                       select o).FirstOrDefault();

                if (obj == null)
                {
                    obj = (from o in _objs
                                          where o.Number == num
                                          select o).FirstOrDefault();
                }
                if (obj != null && obj.Name != null)
                {
                    name = obj.Name;
                }
                else
                {
                    name = new byte[0];
                }
            }
            return name;
        }

        void UpdateObjectStates()
        {
            for (int i = 1; i < _objs.Length; i++)
            {
                if (_objs[i].Number > 0)
                    _objs[i].State = GetStateCore(_objs[i].Number);
            }
        }

        protected byte GetStateCore(int obj)
        {
            return _resManager.ObjectStateTable[obj];
        }

        void GetObjectOwner()
        {
            GetResult();
            SetResult(GetOwnerCore(GetVarOrDirectWord(OpCodeParameter.Param1)));
        }

        internal bool GetObjectOrActorXY(int obj, out Point p)
        {
            p = new Point();

            if (ObjIsActor(obj))
            {
                var act = Actors[ObjToActor(obj)];
                if (act != null && act.IsInCurrentRoom)
                {
                    p = act.Position;
                    return true;
                }
                return false;
            }

            switch (GetWhereIsObject(obj))
            {
                case WhereIsObject.NotFound:
                    return false;
                case WhereIsObject.Inventory:
                    if (ObjIsActor(_resManager.ObjectOwnerTable[obj]))
                    {
                        var act = Actors[_resManager.ObjectOwnerTable[obj]];
                        if (act != null && act.IsInCurrentRoom)
                        {
                            p = act.Position;
                            return true;
                        }
                    }
                    return false;
            }

            int dir;
            GetObjectXYPos(obj, out p, out dir);
            return true;
        }

        protected int ObjToActor(int obj)
        {
            return obj;
        }

        protected bool ObjIsActor(int obj)
        {
            return obj < Actors.Length;
        }

        internal bool GetClass(int obj, ObjectClass cls)
        {
            cls &= (ObjectClass)0x7F;

            if (Game.Version < 5)
            {
                // Translate the new (V5) object classes to the old classes
                // (for those which differ).
                switch (cls)
                {
                    case ObjectClass.Untouchable:
                        cls = (ObjectClass)24;
                        break;

                    case ObjectClass.Player:
                        cls = (ObjectClass)23;
                        break;

                    case ObjectClass.XFlip:
                        cls = (ObjectClass)19;
                        break;

                    case ObjectClass.YFlip:
                        cls = (ObjectClass)18;
                        break;
                }
            }

            return (_resManager.ClassData[obj] & (1 << ((int)cls - 1))) != 0;
        }

        protected void SetObjectNameCore(int obj)
        {
            if (obj < Actors.Length)
            {
                string msg = string.Format("Can't set actor {0} name with new name.", obj);
                throw new NotSupportedException(msg);
            }

            _newNames[obj] = ReadCharacters();
            RunInventoryScript(0);
        }

        protected int GetObjX(int obj)
        {
            if (obj < 1)
                return 0;                                   /* fix for indy4's map */

            if (obj < Actors.Length)
            {
                return Actors[obj].Position.X;
            }

            if (GetWhereIsObject(obj) == WhereIsObject.NotFound)
                return -1;

            Point p;
            GetObjectOrActorXY(obj, out p);
            return p.X;
        }

        protected int GetObjY(int obj)
        {
            if (obj < 1)
                return 0;                                   /* fix for indy4's map */

            if (obj < Actors.Length)
            {
                return Actors[obj].Position.Y;
            }
            if (GetWhereIsObject(obj) == WhereIsObject.NotFound)
                return -1;

            Point p;
            GetObjectOrActorXY(obj, out p);
            return p.Y;
        }

        protected WhereIsObject GetWhereIsObject(int obj)
        {
            if (obj >= _resManager.ObjectOwnerTable.Length)
                return WhereIsObject.NotFound;

            if (obj < 1)
                return WhereIsObject.NotFound;

            if (_resManager.ObjectOwnerTable[obj] != OwnerRoom)
            {
                for (int i = 0; i < _resManager.NumInventory; i++)
                    if (_inventory[i] == obj)
                        return WhereIsObject.Inventory;
                return WhereIsObject.NotFound;
            }

            for (int i = (_objs.Length - 1); i > 0; i--)
            {
                if (_objs[i].Number == obj)
                {
                    return WhereIsObject.Room;
                }
            }

            return WhereIsObject.NotFound;
        }

        protected void PutState(int obj, int state)
        {
            ScummHelper.AssertRange(0, state, 0xFF, "state");
            _resManager.ObjectStateTable[obj] = (byte)state;
        }

        protected int GetObjectIndex(int obj)
        {
            if (obj < 1)
                return -1;

            for (var i = (_objs.Length - 1); i >= 0; i--)
            {
                if (_objs[i].Number == obj)
                    return i;
            }
            return -1;
        }

        protected void AddObjectToDrawQue(byte obj)
        {
            _drawingObjects.Add(obj);
        }

        protected virtual void ClearDrawObjectQueue()
        {
            _drawingObjects.Clear();
        }

        protected void PutOwner(int obj, byte owner)
        {
            _resManager.ObjectOwnerTable[obj] = owner;
        }

        protected void PutClass(int obj, int cls, bool set)
        {
            var cls2 = (ObjectClass)(cls & 0x7F);
            ScummHelper.AssertRange(1, (int)cls2, 32, "class");

            if (_game.Version < 5)
            {
                // Translate the new (V5) object classes to the old classes
                // (for those which differ).
                switch (cls2)
                {
                    case ObjectClass.Untouchable:
                        cls2 = (ObjectClass)24;
                        break;

                    case ObjectClass.Player:
                        cls2 = (ObjectClass)23;
                        break;

                    case ObjectClass.XFlip:
                        cls2 = (ObjectClass)19;
                        break;

                    case ObjectClass.YFlip:
                        cls2 = (ObjectClass)18;
                        break;
                }
            }

            if (set)
                ClassData[obj] |= (uint)(1 << ((int)cls2 - 1));
            else
                ClassData[obj] &= (uint)~(1 << ((int)cls2 - 1));

            if (_game.Version < 5 && obj >= 1 && obj < Actors.Length)
            {
                Actors[obj].ClassChanged(cls2, set);
            }
        }

        protected int GetOwnerCore(int obj)
        {
            return _resManager.ObjectOwnerTable[obj];
        }

        protected int FindObjectCore(int x, int y)
        {
            byte a;
            int mask = 0xF;

            for (int i = 1; i < _objs.Length; i++)
            {
                if ((_objs[i].Number < 1) || GetClass(_objs[i].Number, ObjectClass.Untouchable))
                    continue;

                var b = i;
                do
                {
                    a = _objs[b].ParentState;
                    b = _objs[b].Parent;
                    if (b == 0)
                    {
                        if (_objs[i].Position.X <= x && (_objs[i].Width + _objs[i].Position.X) > x &&
                            _objs[i].Position.Y <= y && (_objs[i].Height + _objs[i].Position.Y) > y)
                        {
                            return _objs[i].Number;
                        }
                        break;
                    }
                } while ((_objs[b].State & mask) == a);
            }

            return 0;
        }

        protected void GetObjectXYPos(int obj, out Point p, out int dir)
        {
            var idx = GetObjectIndex(obj);
            var od = _objs[idx];

            if (Game.Version >= 6)
            {
                var state = GetStateCore(obj) - 1;
                if (state < 0)
                    state = 0;

                var x = od.Position.X + od.Hotspots[state].X;
                var y = od.Position.Y + od.Hotspots[state].Y;
                p = new Point((short)x, (short)y);
            }
            else
            {
                p = od.Walk;
            }

            dir = ScummHelper.OldDirToNewDir(od.ActorDir & 3);
        }

        protected Point GetObjectXYPos(int obj)
        {
            int dir;
            Point p;
            GetObjectXYPos(obj, out p, out dir);
            return p;
        }

        void DrawRoomObjects(int argument)
        {
            const int mask = 0xF;
            if (Game.GameId == NScumm.Core.IO.GameId.SamNMax)
            {
                for (int i = 1; i < _objs.Length; i++)
                {
                    if (_objs[i].Number > 0 && ((_objs[i].State & mask) != 0))
                    {
                        DrawRoomObject(i, argument);
                    }
                }
            }
            else
            {
                for (int i = (_objs.Length - 1); i > 0; i--)
                {
                    if (_objs[i].Number > 0 && ((_objs[i].State & mask) != 0))
                    {
                        DrawRoomObject(i, argument);
                    }
                }
            }
        }

        void DrawRoomObject(int i, int argument)
        {
            byte a;
            const int mask = 0xF;

            var od = _objs[i];
            if ((i < 1) || (od.Number < 1) || od.State == 0)
            {
                return;
            }
            do
            {
                a = od.ParentState;
                if (od.Parent == 0)
                {
                    if (Game.Version <= 6 || od.FloatingObjectIndex == 0)
                        DrawObject(i, argument);
                    break;
                }
                od = _objs[od.Parent];
            } while ((od.State & mask) == a);
        }

        void DrawObject(int obj, int arg)
        {
            var od = _objs[obj];

            if (_bgNeedsRedraw)
                arg = 0;

            if (od.Number == 0)
                return;

            var xpos = (od.Position.X / 8);
            var ypos = (int)od.Position.Y;

            var width = od.Width / 8;
            var height = (od.Height &= 0xFFF8); // Mask out last 3 bits

            // Short circuit for objects which aren't visible at all.
            if (width == 0 || xpos > _screenEndStrip || xpos + width < _screenStartStrip)
                return;

            if (od == null || od.Images == null || od.Images.Count == 0)
                return;

            var x = 0xFFFF;

            int numstrip;
            for (var a = numstrip = 0; a < width; a++)
            {
                var tmp = xpos + a;
                if (tmp < _screenStartStrip || _screenEndStrip < tmp)
                    continue;
                if (arg > 0 && _screenStartStrip + arg <= tmp)
                    continue;
                if (arg < 0 && tmp <= _screenEndStrip + arg)
                    continue;
                Gdi.SetGfxUsageBit(tmp, Gdi.UsageBitDirty);
                if (tmp < x)
                    x = tmp;
                numstrip++;
            }

            if (numstrip != 0)
            {
                var flags = od.Flags;
                // Sam & Max needs this to fix object-layering problems with
                // the inventory and conversation icons.
                if ((_game.GameId == NScumm.Core.IO.GameId.SamNMax && GetClass(od.Number, ObjectClass.IgnoreBoxes)) ||
                    (_game.GameId == NScumm.Core.IO.GameId.FullThrottle && GetClass(od.Number, ObjectClass.Player)))
                    flags |= DrawBitmaps.DrawMaskOnAll;

                var state = GetStateCore(od.Number);
                if (state > 0 && (state - 1) < od.Images.Count)
                {
                    Gdi.DrawBitmap(od.Images[state - 1], _mainVirtScreen, x, ypos, width * 8, height, x - xpos, numstrip, roomData.Header.Width, flags);
                }
            }
        }

        void ProcessDrawQueue()
        {
            foreach (var obj in _drawingObjects)
            {
                if (obj != 0)
                {
                    DrawObject(obj, 0);
                }
            }
            _drawingObjects.Clear();
        }

        protected void SetOwnerOf(int obj, int owner)
        {
            // In Sam & Max this is necessary, or you won't get your stuff back
            // from the Lost and Found tent after riding the Cone of Tragedy. But
            // it probably applies to all V6+ games. See bugs #493153 and #907113.
            // FT disassembly is checked, behavior is correct. [sev]
            int arg = (Game.Version >= 6) ? obj : 0;

            // WORKAROUND for bug #1917981: Game crash when finishing Indy3 demo.
            // Script 94 tries to empty the inventory but does so in a bogus way.
            // This causes it to try to remove object 0 from the inventory.
            if (owner == 0)
            {
                ClearOwnerOf(obj);

                if (_currentScript != 0xFF)
                {
                    var ss = _slots[_currentScript];
                    if (ss.Where == WhereIsObject.Inventory)
                    {
                        if (ss.Number < _resManager.NumInventory && _inventory[ss.Number] == obj)
                        {
                            //throw new NotSupportedException("Odd setOwnerOf case #1: Please report to Fingolfin where you encountered this");
                            PutOwner(obj, 0);
                            RunInventoryScript(arg);
                            StopObjectCode();
                            return;
                        }
                        if (ss.Number == obj)
                            throw new NotSupportedException("Odd setOwnerOf case #2: Please report to Fingolfin where you encountered this");
                    }
                }
            }

            PutOwner(obj, (byte)owner);
            RunInventoryScript(arg);
        }

        void ClearOwnerOf(int obj)
        {
            // Stop the associated object script code (else crashes might occurs)
            StopObjectScriptCore((ushort)obj);

            if (GetOwnerCore(obj) != OwnerRoom)
            {
                // Alternatively, scan the inventory to see if the object is in there...
                for (int i = 0; i < _resManager.NumInventory; i++)
                {
                    if (_inventory[i] == obj)
                    {
                        // Found the object! Nuke it from the inventory.
                        _inventory[i] = 0;

                        // Now fill up the gap removing the object from the inventory created.
                        for (i = 0; i < _resManager.NumInventory - 1; i++)
                        {
                            if (_inventory[i] == 0 && _inventory[i + 1] != 0)
                            {
                                _inventory[i] = _inventory[i + 1];
                                _invData[i] = _invData[i + 1];
                                _inventory[i + 1] = 0;
                                // FIXME FIXME FIXME: This is incomplete, as we do not touch flags, status... BUG
                                // TODO:
                                //_res->_types[rtInventory][i]._address = _res->_types[rtInventory][i + 1]._address;
                                //_res->_types[rtInventory][i]._size = _res->_types[rtInventory][i + 1]._size;
                                //_res->_types[rtInventory][i + 1]._address = NULL;
                                //_res->_types[rtInventory][i + 1]._size = 0;
                            }
                        }
                        break;
                    }
                }
            }
        }


    }
}

