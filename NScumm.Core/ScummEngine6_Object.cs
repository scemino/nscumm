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
using System.IO;

namespace NScumm.Core
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
    class BlastObject
    {
        public int Number { get; set; }

        public Rect Rect { get; set; }

        public int ScaleX { get; set; }

        public int ScaleY { get; set; }

        public int Image { get; set; }

        public int Mode { get; set; }

        internal string DebuggerDisplay
        {
            get
            { 
                return string.Format("Id={0}, Rec={1}]", Number, Rect.DebuggerDisplay);
            }
        }
    }


    partial class ScummEngine6
    {
        int _blastObjectQueuePos;
        readonly BlastObject[] _blastObjectQueue = CreateBlastObjects();

        static BlastObject[] CreateBlastObjects()
        {
            var blastObjects = new BlastObject[200];
            for (int i = 0; i < blastObjects.Length; i++)
            {
                blastObjects[i] = new BlastObject();
            }
            return blastObjects;
        }

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

                var a = Actors[obj];
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
                dir = Actors[ObjToActor(index)].Facing;
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
                if (b < Actors.Length)
                    i = Actors[b].ScaleX;
            }
            else
            {
                pos1 = new Point((short)b, (short)c);
            }

            if (isObj2)
            {
                if (!GetObjectOrActorXY(e, out pos2))
                    return -1;
                if (e < Actors.Length)
                    j = Actors[e].ScaleX;
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
            Debug.Assert(idx >= 0, "Object index should be positive");

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

        void BompApplyShadow(int shadowMode, byte[] lineBuffer, int linePos, PixelNavigator dst, int size)
        {
            Debug.Assert(size > 0);
            switch (shadowMode)
            {
                case 0:
                    BompApplyShadow0(lineBuffer, linePos, dst, size);
                    break;
                default:
                    throw new ArgumentException(string.Format("Unknown shadow mode {0}", shadowMode));
            }
        }

        void BompApplyShadow0(byte[] lineBuffer, int linePos, PixelNavigator dst, int size)
        {
            while (size-- > 0)
            {
                byte tmp = lineBuffer[linePos++];
                if (tmp != 255)
                {
                    dst.Write(tmp);
                }
                dst.OffsetX(1);
            }
        }

        protected override void ClearDrawObjectQueue()
        {
            base.ClearDrawObjectQueue();
            _blastObjectQueuePos = 0;
        }

        protected override void DrawDirtyScreenParts()
        {
            DrawBlastObjects();

            // Call the original method.
            base.DrawDirtyScreenParts();

            // Remove all blasted objects/text again.
            RemoveBlastObjects();
        }

        void RemoveBlastObjects()
        {
            for (var i = 0; i < _blastObjectQueuePos; i++)
            {
                var eo = _blastObjectQueue[i];
                RemoveBlastObject(eo);
            }
            _blastObjectQueuePos = 0;
        }

        void RemoveBlastObject(BlastObject eo)
        {
            var vs = MainVirtScreen;

            int left_strip, right_strip;

            var r = eo.Rect;

            r.Clip(vs.Width, vs.Height);

            if (r.Width <= 0 || r.Height <= 0)
                return;

            left_strip = r.Left / 8;
            right_strip = (r.Right + (vs.XStart % 8)) / 8;

            if (left_strip < 0)
                left_strip = 0;
            if (right_strip > Gdi.NumStrips - 1)
                right_strip = Gdi.NumStrips - 1;
            for (var i = left_strip; i <= right_strip; i++)
                Gdi.ResetBackground(r.Top, r.Bottom, i);

            MarkRectAsDirty(MainVirtScreen, r, Gdi.UsageBitRestored);
        }

        void DrawBlastObjects()
        {
            for (int i = 0; i < _blastObjectQueuePos; i++)
            {
                var eo = _blastObjectQueue[i];
                DrawBlastObject(eo);
            }
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay,nq}")]
        class BompDrawData
        {
            public Surface Dst;
            public int X, Y;

            public byte[] Src;
            public int Width, Height;

            public int ScaleX, ScaleY;

            public int ShadowMode;

            internal string DebuggerDisplay
            {
                get
                { 
                    return string.Format("Rect={0}]", new Rect(X, Y, X + Width, Y + Height).DebuggerDisplay);
                }
            }
        }

        void DrawBlastObject(BlastObject eo)
        {
            var objnum = GetObjectIndex(eo.Number);
            if (objnum == -1)
                throw new NotSupportedException(string.Format("DrawBlastObject: GetObjectIndex on BlastObject {0} failed", eo.Number));

            var index = eo.Image >= _objs[objnum].Images.Count ? 0 : eo.Image;
            var img = _objs[objnum].Images[index];

            if (!img.IsBomp)
                throw new NotSupportedException(string.Format("object {0} is not a blast object", eo.Number));

            var bdd = new BompDrawData();
            bdd.Src = img.Data;
            bdd.Dst = MainVirtScreen.Surfaces[0];
            bdd.X = eo.Rect.Left;
            bdd.Y = eo.Rect.Top;

            bdd.Width = _objs[objnum].Width;
            bdd.Height = _objs[objnum].Height;

            bdd.ScaleX = eo.ScaleX;
            bdd.ScaleY = eo.ScaleY;

            if ((bdd.ScaleX != 255) || (bdd.ScaleY != 255))
            {
                bdd.ShadowMode = 0;
            }
            else
            {
                bdd.ShadowMode = eo.Mode;
            }

            DrawBomp(bdd);

            MarkRectAsDirty(MainVirtScreen, new Rect(bdd.X, bdd.X + bdd.Width, bdd.Y, bdd.Y + bdd.Height));
        }

        int SetupBompScale(byte[] scaling, int size, int scale)
        {
            int[] offsets = { 3, 2, 1, 0, 7, 6, 5, 4 };
            var bitsCount = 0;
            var pos = 0;

            var count = (256 - size / 2);
            Debug.Assert(0 <= count && count < 768);
            var scalePos = count;

            count = (size + 7) / 8;
            while ((count--) != 0)
            {
                byte scaleMask = 0;
                for (var i = 0; i < 8; i++)
                {
                    var scaleTest = bigCostumeScaleTable[scalePos + offsets[i]];
                    scaleMask <<= 1;
                    if (scale < scaleTest)
                    {
                        scaleMask |= 1;
                    }
                    else
                    {
                        bitsCount++;
                    }
                }
                scalePos += 8;

                scaling[pos++] = scaleMask;
            }
            size &= 7;
            if (size != 0)
            {
                --pos;
                if ((scaling[pos] & ScummHelper.RevBitMask(size)) == 0)
                {
                    scaling[pos] |= (byte)ScummHelper.RevBitMask(size);
                    bitsCount--;
                }
            }

            return bitsCount;
        }

        void BompScaleFuncX(byte[] lineBuffer, byte[] scaling, int scalingPos, byte skip, int size)
        {
            var line_ptr1 = 0;
            var line_ptr2 = 0;

            byte tmp = scaling[scalingPos++];

            while ((size--) != 0)
            {
                if ((skip & tmp) == 0)
                {
                    lineBuffer[line_ptr1++] = lineBuffer[line_ptr2];
                }
                line_ptr2++;
                skip >>= 1;
                if (skip == 0)
                {
                    skip = 128;
                    tmp = scaling[scalingPos++];
                }
            }
        }

        void DrawBomp(BompDrawData bd)
        {
            Rect clip;
            byte skip_y_bits = 0x80;
            byte skip_y_new = 0;
            byte[] bomp_scaling_x = new byte[64];
            byte[] bomp_scaling_y = new byte[64];

            if (bd.X < 0)
            {
                clip.Left = -bd.X;
            }
            else
            {
                clip.Left = 0;
            }

            if (bd.Y < 0)
            {
                clip.Top = -bd.Y;
            }
            else
            {
                clip.Top = 0;
            }

            clip.Right = bd.Width;
            if (clip.Right > bd.Dst.Width - bd.X)
            {
                clip.Right = bd.Dst.Width - bd.X;
            }

            clip.Bottom = bd.Height;
            if (clip.Bottom > bd.Dst.Height - bd.Y)
            {
                clip.Bottom = bd.Dst.Height - bd.Y;
            }

            var src = bd.Src;
            var pn = new PixelNavigator(bd.Dst);
            pn.GoTo(bd.X + clip.Left, bd.Y);

            var scalingYPtr = 0;

            // Setup vertical scaling
            if (bd.ScaleY != 255)
            {
                var scaleBottom = SetupBompScale(bomp_scaling_y, bd.Height, bd.ScaleY);

                skip_y_new = bomp_scaling_y[scalingYPtr++];
                skip_y_bits = 0x80;

                if (clip.Bottom > scaleBottom)
                {
                    clip.Bottom = scaleBottom;
                }
            }

            // Setup horizontal scaling
            if (bd.ScaleX != 255)
            {
                var scaleRight = SetupBompScale(bomp_scaling_x, bd.Width, bd.ScaleX);

                if (clip.Right > scaleRight)
                {
                    clip.Right = scaleRight;
                }
            }

            var width = clip.Right - clip.Left;

            if (width <= 0)
                return;

            int pos_y = 0;
            var line_buffer = new byte[1024];

            byte tmp;
            using (var br = new BinaryReader(new MemoryStream(src)))
            {
                // Loop over all lines
                while (pos_y < clip.Bottom)
                {
                    // Decode a single (bomp encoded) line
                    BompDecodeLine(br, line_buffer, 0, bd.Width);

                    // If vertical scaling is enabled, do it
                    if (bd.ScaleY != 255)
                    {
                        // A bit set means we should skip this line...
                        tmp = (byte)(skip_y_new & skip_y_bits);

                        // Advance the scale-skip bit mask, if it's 0, get the next scale-skip byte
                        skip_y_bits /= 2;
                        if (skip_y_bits == 0)
                        {
                            skip_y_bits = 0x80;
                            skip_y_new = bomp_scaling_y[scalingYPtr++];
                        }

                        // Skip the current line if the above check tells us to
                        if (tmp != 0)
                            continue;
                    }

                    // Perform horizontal scaling
                    if (bd.ScaleX != 255)
                    {
                        BompScaleFuncX(line_buffer, bomp_scaling_x, 0, 0x80, (byte)bd.Width);
                    }

                    // The first clip.top lines are to be clipped, i.e. not drawn
                    if (clip.Top > 0)
                    {
                        clip.Top--;
                    }
                    else
                    {
                        // Finally, draw the decoded, scaled, masked and recolored line onto
                        // the target surface, using the specified shadow mode
                        BompApplyShadow(bd.ShadowMode, line_buffer, clip.Left, pn, width);
                    }

                    // Advance to the next line
                    pos_y++;
                    pn.OffsetY(1);
                }
            }
        }

        static readonly byte[] bigCostumeScaleTable =
            {
                0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0,
                0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
                0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8,
                0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
                0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4,
                0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
                0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC,
                0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
                0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2,
                0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
                0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA,
                0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
                0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6,
                0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
                0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE,
                0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
                0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1,
                0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
                0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9,
                0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
                0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
                0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
                0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED,
                0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
                0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3,
                0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
                0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
                0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
                0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7,
                0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
                0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF,
                0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFE,

                0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0,
                0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
                0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8,
                0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
                0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4,
                0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
                0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC,
                0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
                0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2,
                0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
                0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA,
                0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
                0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6,
                0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
                0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE,
                0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
                0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1,
                0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
                0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9,
                0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
                0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
                0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
                0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED,
                0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
                0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3,
                0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
                0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
                0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
                0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7,
                0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
                0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF,
                0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFE,

                0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0,
                0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
                0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8,
                0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
                0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4,
                0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
                0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC,
                0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
                0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2,
                0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
                0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA,
                0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
                0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6,
                0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
                0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE,
                0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
                0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1,
                0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
                0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9,
                0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
                0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5,
                0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
                0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED,
                0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
                0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3,
                0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
                0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB,
                0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
                0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7,
                0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
                0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF,
                0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFF,
            };

    }
}

