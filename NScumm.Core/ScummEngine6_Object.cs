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
    class BlastObject
    {
        public int Number { get; set; }

        public Rect Rect { get; set; }

        public int ScaleX { get; set; }

        public int ScaleY { get; set; }

        public int Image { get; set; }

        public int Mode { get; set; }
    }


    partial class ScummEngine6
    {
        int _blastObjectQueuePos;
        readonly BlastObject[] _blastObjectQueue = new BlastObject[200];

        [OpCode(0x61)]
        void DrawObject(int obj, int state)
        {
            // This is based on disassembly
            if (state == 0)
                state = 1;

            SetObjectState(obj, state, -1, -1);
        }

        [OpCode(0x62)]
        void DrawObjectAt(int obj, int x, int y)
        {
            SetObjectState(obj, 1, x, y);
        }

        [OpCode(0x63)]
        void DrawBlastObject(int a, int b, int c, int d, int e, int[] args)
        {
            EnqueueObject(a, b, c, d, e, 0xFF, 0xFF, 1, 0);
        }

        [OpCode(0x64)]
        void SetBlastObjectWindow(int a, int b, int c, int d)
        {
            // None of the scripts of The Dig and Full Throttle use this opcode.
            // Sam & Max only uses it at the beginning of the highway subgame. In
            // the original interpreter pop'ed arguments are just ignored and the
            // clipping blastObject window is defined with (0, 0, 320, 200)...
            // which matches the screen dimensions and thus, doesn't require
            // another clipping operation.
            // So, we just handle this as no-op opcode.
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
                if (((cls & 0x80) != 0 && !b) || ((cls & 0x80) == 0 && b))
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

        [OpCode(0x97)]
        void SetObjectName(int obj)
        {
            SetObjectNameCore(obj);
        }

        [OpCode(0xa0)]
        void FindObject(int x, int y)
        {
            Push(FindObjectCore(x, y));
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

        [OpCode(0xcb)]
        void PickOneOf(int i, int[] args)
        {
            if (i < 0 || i > args.Length)
                throw new ArgumentOutOfRangeException("i", i, string.Format("PickOneOf: {0} out of range (0, {1})", i, args.Length - 1));
            Push(args[i]);
        }

        [OpCode(0xcc)]
        void PickOneOfDefault(int i, int[] args, int def)
        {
            if (i < 0 || i >= args.Length)
                i = def;
            else
                i = args[i];
            Push(i);
        }

        [OpCode(0xcd)]
        void StampObject(int obj, short x, short y, byte state)
        {

            if (Game.Version >= 7 && obj < 30)
            {
                if (state == 0)
                    state = 255;

                var a = _actors[obj];
                a.ScaleX = state;
                a.ScaleY = state;
                a.PutActor(new Point(x, y), CurrentRoom);
                a.DrawToBackBuf = true;
                a.DrawCostume();
                a.DrawToBackBuf = false;
                a.DrawCostume();
                return;
            }

            if (state == 0)
                state = 1;

            var objnum = GetObjectIndex(obj);
            if (objnum == -1)
                return;

            if (x != -1)
            {
                _objs[objnum].Position = new Point((short)(x * 8), (short)(y * 8));
            }

            PutState(obj, state);
            DrawObject(objnum, 0);
        }

        [OpCode(0xdd)]
        void FindAllObjects(int room)
        {
            var i = 1;

            if (room != CurrentRoom)
                throw new NotSupportedException(string.Format("FindAllObjects: current room is not {0}", room));
            WriteVariable(0, 0);
            DefineArray(0, ArrayType.IntArray, 0, _resManager.NumLocalObjects + 1);
            WriteArray(0, 0, 0, _resManager.NumLocalObjects);

            while (i < _resManager.NumLocalObjects)
            {
                WriteArray(0, 0, i, _objs[i].Number);
                i++;
            }

            Push(ReadVariable(0));
        }

        [OpCode(0xed)]
        void GetObjectNewDir(int index)
        {
            Push(GetObjNewDir(index));
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

        void EnqueueObject(int objectNumber, int objectX, int objectY, int objectWidth,
                           int objectHeight, int scaleX, int scaleY, int image, int mode)
        {
            if (_blastObjectQueuePos >= _blastObjectQueue.Length)
            {
                throw new InvalidOperationException("enqueueObject: overflow");
            }

            var idx = GetObjectIndex(objectNumber);
            Debug.Assert(idx >= 0);

            var left = objectX;
            var top = objectY + ScreenTop;
            int right;
            int bottom;
            if (objectWidth == 0)
            {
                right = left + _objs[idx].Width;
            }
            else
            {
                right = left + objectWidth;
            }
            if (objectHeight == 0)
            {
                bottom = top + _objs[idx].Height;
            }
            else
            {
                bottom = top + objectHeight;
            }

            var eo = _blastObjectQueue[_blastObjectQueuePos++];
            eo.Number = objectNumber;
            eo.Rect = new Rect(left, top, right, bottom);
            eo.ScaleX = scaleX;
            eo.ScaleY = scaleY;
            eo.Image = image;
            eo.Mode = mode;
        }

        protected override void ClearDrawObjectQueue()
        {
            base.ClearDrawObjectQueue();
            _blastObjectQueuePos = 0;
        }

        void DrawBlastObjects()
        {
            foreach (var eo in _blastObjectQueue)
            {
                DrawBlastObject(eo);
            }
        }

        void DrawBlastObject(BlastObject eo)
        {
            // TODO: scumm6 with sam&max
//            var vs = MainVirtScreen;
//
//            //ScummHelper.AssertRange(30, eo.Number, _numGlobalObjects - 1, "blast object");
//
//            var objnum = GetObjectIndex(eo.Number);
//            if (objnum == -1)
//                throw new NotSupportedException(string.Format("DrawBlastObject: GetObjectIndex on BlastObject {0} failed", eo.Number));
//
//            var img = _objs[objnum].Images[eo.Image];
//
//            const byte *img = getObjectImage(ptr, eo->image);
//            if (_game.version == 8) {
//                    assert(img);
//                    bomp = img + 8;
//            } else {
//                    if (!img)
//                        img = getObjectImage(ptr, 1);   // Backward compatibility with samnmax blast objects
//                    assert(img);
//                    bomp = findResourceData(MKTAG('B','O','M','P'), img);
//                }
//
//            if (!bomp)
//                error("object %d is not a blast object", eo->number);
//
//            bdd.dst = *vs;
//            bdd.dst.setPixels(vs->getPixels(0, 0));
//            bdd.x = eo->rect.left;
//            bdd.y = eo->rect.top;
//
//            // Skip the bomp header
//            if (_game.version == 8) {
//                    bdd.src = bomp + 8;
//            } else {
//                    bdd.src = bomp + 10;
//                }
//            if (_game.version == 8) {
//                    bdd.srcwidth = READ_LE_UINT32(bomp);
//                    bdd.srcheight = READ_LE_UINT32(bomp+4);
//            } else {
//                    bdd.srcwidth = READ_LE_UINT16(bomp+2);
//                    bdd.srcheight = READ_LE_UINT16(bomp+4);
//                }
//
//            bdd.scale_x = (byte)eo->scaleX;
//            bdd.scale_y = (byte)eo->scaleY;
//
//            bdd.maskPtr = NULL;
//            bdd.numStrips = _gdi->_numStrips;
//
//            if ((bdd.scale_x != 255) || (bdd.scale_y != 255)) {
//                    bdd.shadowMode = 0;
//            } else {
//                    bdd.shadowMode = eo->mode;
//                }
//            bdd.shadowPalette = _shadowPalette;
//
//            bdd.actorPalette = 0;
//            bdd.mirror = false;
//
//            DrawBomp(bdd);
//
//            MarkRectAsDirty(vs, bdd.x, bdd.x + bdd.srcwidth, bdd.y, bdd.y + bdd.srcheight);
        }

    }
}

