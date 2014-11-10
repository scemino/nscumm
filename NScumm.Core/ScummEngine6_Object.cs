//
//  ScummEngine6_Object.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
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
using NScumm.Core.Graphics;
using System.Diagnostics;

namespace NScumm.Core
{
    partial class ScummEngine6
    {
        [OpCode(0x61)]
        void DrawObject(int obj, int state)
        {
            // This is based on disassembly
            if (state == 0)
                state = 1;

            SetObjectState(obj, state, -1, -1);
        }

        void SetObjectState(int obj, int state, int x, int y)
        {

            var i = GetObjectIndex(obj);
            if (i == -1)
            {
                Debug.WriteLine("SetObjectState: no such object {0}", obj);
                return;
            }

            if (x != -1 && x != 0x7FFFFFFF)
            {
                _objs[i].Position = new Point((short)(x * 8), (short)(y * 8));
            }

            AddObjectToDrawQue((byte)i);
            //TODO: scumm 7
//            if (Game.Version >= 7)
//            {
//                if (state == 0xFF)
//                {
//                    state = GetState(obj);
//                    var imagecount = GetObjectImageCount(obj);
//
//                    if (state < imagecount)
//                        state++;
//                    else
//                        state = 1;
//                }
//
//                if (state == 0xFE)
//                    state = _rnd.getRandomNumber(getObjectImageCount(obj));
//            }
            PutState(obj, state);
        }

        [OpCode(0x6d)]
        void IfClassOfIs(int obj, int[] args)
        {
            var num = args.Length;
            var cond = true;

            while (--num >= 0)
            {
                var cls = args[num];
                var b = GetClass(obj, (ObjectClass)cls);
                if (((cls & 0x80) != 0 && !b) || (!((cls & 0x80) != 0 && b)))
                    cond = false;
            }
            Push(cond);
        }

        [OpCode(0x6e)]
        void SetClass(int obj, int[] args)
        {
            var num = args.Length;
            while (--num >= 0)
            {
                var cls = args[num];
                if (cls == 0)
                    ClassData[num] = 0;
                else if ((cls & 0x80) != 0)
                    PutClass(obj, cls, true);
                else
                    PutClass(obj, cls, false);
            }
        }

        [OpCode(0x6f)]
        void GetState(int obj)
        {
            Push(GetStateCore(obj));
        }

        [OpCode(0x70)]
        void SetState(int obj, int state)
        {
            PutState(obj, state);
            MarkObjectRectAsDirty(obj);
            if (_bgNeedsRedraw)
                ClearDrawObjectQueue();
        }

        [OpCode(0x71)]
        void SetOwner(int obj, int owner)
        {
            SetOwnerOf(obj, owner);
        }

        [OpCode(0x72)]
        void GetOwner(int obj)
        {
            Push(GetOwnerCore(obj));
        }

        [OpCode(0x8d)]
        void GetObjectX(int index)
        {
            Push(GetObjX(index));
        }

        [OpCode(0x8e)]
        void GetObjectY(int index)
        {
            Push(GetObjY(index));
        }

        [OpCode(0x8f)]
        void GetObjectOldDir(int index)
        {
            Push(GetObjOldDir(index));
        }

        [OpCode(0xa0)]
        void FindObject(int x, int y)
        {
            Push(FindObjectCore(x, y));
        }

        [OpCode(0xed)]
        void GetObjectNewDir(int index)
        {
            Push(GetObjNewDir(index));
        }

        int GetObjOldDir(int index)
        {
            return ScummHelper.NewDirToOldDir(GetObjNewDir(index));
        }

        int GetObjNewDir(int index)
        {
            int dir;
            if (ObjIsActor(index))
            {
                dir = _actors[ObjToActor(index)].Facing;
            }
            else
            {
                Point pos;
                GetObjectXYPos(index, out pos, out dir);
            }
            return dir;
        }


        [OpCode(0xc5)]
        void DistObjectObject(int a, int b)
        {
            Push(GetDistanceBetween(true, a, 0, true, b, 0));
        }

        [OpCode(0xc6)]
        void DistObjectPt(int a, int b, int c)
        {
            Push(GetDistanceBetween(true, a, 0, false, b, c));
        }

        [OpCode(0xc7)]
        void DistObjectPtPt(int a, int b, int c, int d)
        {
            Push(GetDistanceBetween(false, a, b, false, c, d));
        }

        int GetDistanceBetween(bool isObj1, int b, int c, bool isObj2, int e, int f)
        {
            int i, j;
            Point pos1;
            Point pos2;

            j = i = 0xFF;

            if (isObj1)
            {
                if (!GetObjectOrActorXY(b, out pos1))
                    return -1;
                if (b < _actors.Length)
                    i = _actors[b].ScaleX;
            }
            else
            {
                pos1 = new Point((short)b, (short)c);
            }

            if (isObj2)
            {
                if (!GetObjectOrActorXY(e, out pos2))
                    return -1;
                if (e < _actors.Length)
                    j = _actors[e].ScaleX;
            }
            else
            {
                pos2 = new Point((short)e, (short)f);
            }

            return ScummMath.GetDistance(pos1, pos2) * 0xFF / ((i + j) / 2);
        }

    }
}

