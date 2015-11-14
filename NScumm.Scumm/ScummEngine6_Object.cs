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
using System.Diagnostics;
using NScumm.Core;
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm
{
    partial class ScummEngine6
    {
        protected int _blastObjectQueuePos;
        protected readonly BlastObject[] _blastObjectQueue = CreateBlastObjects();
        int _blastTextQueuePos;
        readonly BlastText[] _blastTextQueue = CreateBlastTexts();

        static BlastObject[] CreateBlastObjects()
        {
            var blastObjects = new BlastObject[200];
            for (int i = 0; i < blastObjects.Length; i++)
            {
                blastObjects[i] = new BlastObject();
            }
            return blastObjects;
        }

        static BlastText[] CreateBlastTexts()
        {
            var blastTextQueue = new BlastText[50];
            for (int i = 0; i < blastTextQueue.Length; i++)
            {
                blastTextQueue[i] = new BlastText();
            }
            return blastTextQueue;
        }

        [OpCode(0x61)]
        protected void DrawObject(int obj, int state)
        {
            // This is based on disassembly
            if (state == 0)
                state = 1;

            SetObjectState(obj, state, -1, -1);
        }

        [OpCode(0x62)]
        protected void DrawObjectAt(int obj, int x, int y)
        {
            SetObjectState(obj, 1, x, y);
        }

        [OpCode(0x63)]
        protected void DrawBlastObject(int a, int b, int c, int d, int e, int[] args)
        {
            EnqueueObject(a, b, c, d, e, 0xFF, 0xFF, 1, 0);
        }

        [OpCode(0x64)]
        protected void SetBlastObjectWindow(int a, int b, int c, int d)
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
        protected virtual void IfClassOfIs(int obj, int[] args)
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
        protected virtual void SetClass(int obj, int[] args)
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
        protected virtual void GetState(int obj)
        {
            Push(GetStateCore(obj));
        }

        [OpCode(0x70)]
        protected virtual void SetState(int obj, int state)
        {
            PutState(obj, state);
            MarkObjectRectAsDirty(obj);
            if (_bgNeedsRedraw)
                ClearDrawObjectQueue();
        }

        [OpCode(0x71)]
        protected virtual void SetOwner(int obj, int owner)
        {
            SetOwnerOf(obj, owner);
        }

        [OpCode(0x72)]
        protected virtual void GetOwner(int obj)
        {
            Push(GetOwnerCore(obj));
        }

        [OpCode(0x8d)]
        protected virtual void GetObjectX(int index)
        {
            Push(GetObjX(index));
        }

        [OpCode(0x8e)]
        protected virtual void GetObjectY(int index)
        {
            Push(GetObjY(index));
        }

        [OpCode(0x8f)]
        protected void GetObjectOldDir(int index)
        {
            Push(GetObjOldDir(index));
        }

        [OpCode(0x97)]
        protected virtual void SetObjectName(int obj)
        {
            SetObjectNameCore(obj);
        }

        [OpCode(0xa0)]
        protected virtual void FindObject(int x, int y)
        {
            Push(FindObjectCore(x, y));
        }

        [OpCode(0xc5)]
        protected virtual void DistObjectObject(int a, int b)
        {
            Push(GetDistanceBetween(true, a, 0, true, b, 0));
        }

        [OpCode(0xc6)]
        protected void DistObjectPt(int a, int b, int c)
        {
            Push(GetDistanceBetween(true, a, 0, false, b, c));
        }

        [OpCode(0xc7)]
        protected virtual void DistObjectPtPt(int a, int b, int c, int d)
        {
            Push(GetDistanceBetween(false, a, b, false, c, d));
        }

        [OpCode(0xcb)]
        protected virtual void PickOneOf(int i, int[] args)
        {
            if (i < 0 || i > args.Length)
                throw new ArgumentOutOfRangeException("i", string.Format("PickOneOf: {0} out of range (0, {1})", i, args.Length - 1));
            Push(args[i]);
        }

        [OpCode(0xcc)]
        protected virtual void PickOneOfDefault(int i, int[] args, int def)
        {
            if (i < 0 || i >= args.Length)
                i = def;
            else
                i = args[i];
            Push(i);
        }

        [OpCode(0xcd)]
        protected void StampObject(int obj, int x, int y, int state)
        {
            if (Game.Version >= 7 && obj < 30)
            {
                if (state == 0)
                    state = 255;

                var a = Actors[obj];
                a.ScaleX = (byte)state;
                a.ScaleY = (byte)state;
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
                _objs[objnum].Position = new Point(x * 8, y * 8);
            }

            PutState(obj, state);
            DrawObject(objnum, 0);
        }

        [OpCode(0xdd)]
        protected void FindAllObjects(int room)
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
        protected virtual void GetObjectNewDir(int index)
        {
            Push(GetObjNewDir(index));
        }

        protected void SetObjectState(int obj, int state, int x, int y)
        {
            var i = GetObjectIndex(obj);
            if (i == -1)
            {
                Debug.WriteLine("SetObjectState: no such object {0}", obj);
                return;
            }

            if (x != -1 && x != 0x7FFFFFFF)
            {
                _objs[i].Position = new Point((x * 8), (y * 8));
            }

            AddObjectToDrawQue((byte)i);
            if (Game.Version >= 7)
            {
                if (state == 0xFF)
                {
                    state = GetStateCore(obj);
                    var imagecount = _objs[i].Images.Count;
            
                    if (state < imagecount)
                        state++;
                    else
                        state = 1;
                }
            
                if (state == 0xFE)
                    state = new Random().Next(_objs[i].Images.Count + 1);
            }
            PutState(obj, state);
        }

        int GetObjOldDir(int index)
        {
            return ScummHelper.NewDirToOldDir(GetObjNewDir(index));
        }

        int GetObjNewDir(int index)
        {
            int dir;
            if (IsActor(index))
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
                pos1 = new Point(b, c);
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
                pos2 = new Point(e, f);
            }

            return ScummMath.GetDistance(pos1, pos2) * 0xFF / ((i + j) / 2);
        }

        protected void EnqueueObject(int objectNumber, int objectX, int objectY, int objectWidth,
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
            eo.Image = image - 1;
            eo.Mode = mode;
        }

        protected override void ClearDrawObjectQueue()
        {
            base.ClearDrawObjectQueue();
            _blastObjectQueuePos = 0;
        }

        protected override void DrawDirtyScreenParts()
        {
            // For the Full Throttle credits to work properly, the blast
            // texts have to be drawn before the blast objects. Unless
            // someone can think of a better way to achieve this effect.
            if (Game.Version >= 7 && Variables[VariableBlastAboveText.Value] == 1)
            {
                DrawBlastTexts();
                DrawBlastObjects();
                if (Game.Version == 8)
                {
                    // Does this case ever happen? We need to draw the
                    // actor over the blast object, so we're forced to
                    // also draw it over the subtitles.
                    ProcessUpperActors();
                }
            }
            else
            {
                DrawBlastObjects();
                if (Game.Version == 8)
                {
                    // Do this before drawing blast texts. Subtitles go on
                    // top of the CoMI verb coin, e.g. when Murray is
                    // talking to himself early in the game.
                    ProcessUpperActors();
                }
                DrawBlastTexts();
            }

            // Call the original method.
            base.DrawDirtyScreenParts();

            // Remove all blasted objects/text again.
            RemoveBlastTexts();
            RemoveBlastObjects();
        }

        // Used in Scumm v8, to allow the verb coin to be drawn over the inventory
        // chest. I'm assuming that draw order won't matter here.
        void ProcessUpperActors()
        {
            for (var i = 1; i < Actors.Length; i++)
            {
                if (Actors[i].IsInCurrentRoom && Actors[i].Costume != 0 && Actors[i].Layer < 0)
                {
                    Actors[i].DrawCostume();
                    Actors[i].AnimateCostume();
                }
            }
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

        void DrawBlastObject(BlastObject eo)
        {
            var objnum = GetObjectIndex(eo.Number);
            if (objnum == -1)
                throw new NotSupportedException(string.Format("DrawBlastObject: GetObjectIndex on BlastObject {0} failed", eo.Number));

            if (_objs[objnum].Images.Count == 0)
                return;

            var index = eo.Image >= _objs[objnum].Images.Count ? 0 : eo.Image < 0 ? 0 : eo.Image;
            var img = _objs[objnum].Images[index];

            if (!img.IsBomp)
                throw new NotSupportedException(string.Format("object {0} is not a blast object", eo.Number));

            var bdd = new BompDrawData();
            bdd.Src = img.Data;
            bdd.Dst = new PixelNavigator(MainVirtScreen.Surfaces[0]);
            bdd.Dst.GoTo(MainVirtScreen.XStart, 0);
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
            bdd.ShadowPalette = _shadowPalette;

            bdd.DrawBomp();

            MarkRectAsDirty(MainVirtScreen, new Rect(bdd.X, bdd.X + bdd.Width, bdd.Y, bdd.Y + bdd.Height));
        }

        protected void EnqueueText(byte[] text, int x, int y, byte color, byte charset, bool center)
        {
            var bt = _blastTextQueue[_blastTextQueuePos++];
            Debug.Assert(_blastTextQueuePos <= _blastTextQueue.Length);

            ConvertMessageToString(text, bt.Text, 0);
            bt.X = x;
            bt.Y = y;
            bt.Color = color;
            bt.Charset = charset;
            bt.Center = center;
        }

        protected void RemoveBlastTexts()
        {
            for (var i = 0; i < _blastTextQueuePos; i++)
            {
                RestoreBackground(_blastTextQueue[i].Rect);
            }
            _blastTextQueuePos = 0;
        }

        void DrawBlastTexts()
        {
            for (var i = 0; i < _blastTextQueuePos; i++)
            {
                var buf = _blastTextQueue[i].Text;
                var bufPos = 0;

                _charset.Top = _blastTextQueue[i].Y + ScreenTop;
                _charset.Right = ScreenWidth - 1;
                _charset.Center = _blastTextQueue[i].Center;
                _charset.SetColor(_blastTextQueue[i].Color);
                _charset.DisableOffsX = _charset.FirstChar = true;
                _charset.SetCurID(_blastTextQueue[i].Charset);

                int c;
                do
                {
                    _charset.Left = _blastTextQueue[i].X;

                    // Center text if necessary
                    if (_charset.Center)
                    {
                        _charset.Left -= _charset.GetStringWidth(0, buf, bufPos) / 2;
                        if (_charset.Left < 0)
                            _charset.Left = 0;
                    }

                    do
                    {
                        c = buf[bufPos++];

                        // FIXME: This is a workaround for bugs #864030 and #1399843:
                        // In COMI, some text contains ASCII character 11 = 0xB. It's
                        // not quite clear what it is good for; so for now we just ignore
                        // it, which seems to match the original engine (BTW, traditionally,
                        // this is a 'vertical tab').
                        if (c == 0x0B)
                            continue;

                        // Some localizations may override colors
                        // See credits in Chinese COMI
//                        if (Game.GameId == GameId.CurseOfMonkeyIsland && _language == Common::ZH_TWN &&
//                            c == '^' && (buf == _blastTextQueue[i].text + 1))
//                        {
//                            if (*buf == 'c')
//                            {
//                                int color = buf[3] - '0' + 10 * (buf[2] - '0');
//                                _charset.setColor(color);
//
//                                buf += 4;
//                                c = *buf++;
//                            }
//                        }

                        if (c != 0 && c != 0xFF && c != '\n')
                        {
//                            if (c & 0x80 && _useCJKMode)
//                            {
//                                if (_language == Common::JA_JPN && !checkSJISCode(c))
//                                {
//                                    c = 0x20; //not in S-JIS
//                                }
//                                else
//                                {
//                                    c += *buf++ * 256;
//                                }
//                            }
                            _charset.PrintChar(c, true);
                        }
                    } while (c != 0 && c != '\n');

                    _charset.Top += _charset.GetFontHeight();
                } while (c != 0);

                _blastTextQueue[i].Rect = _charset.Str;
            }
        }
    }
}

