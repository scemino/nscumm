//
//  QueenEngine.cs
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
using NScumm.Core.IO;
using NScumm.Core;
using System;
using System.Collections.Generic;
using D = NScumm.Core.DebugHelper;
using System.Diagnostics;

namespace NScumm.Queen
{
    public struct AnimFrame
    {
        public ushort frame;
        public ushort speed;
    }

    public class Person
    {
        //! actor settings to use
        public ActorData actor;
        //! actor name
        public string name;
        //! animation string
        public string anim;
        //! current frame
        public ushort bobFrame;
    }

    public class BobSlot
    {
        public bool active;
        //! current position
        public short x, y;
        //! bounding box
        public Box box;
        public bool xflip;
        //! shrinking percentage
        public ushort scale;
        //! associated BobFrame
        public ushort frameNum;
        //! 'direction' for the next frame (-1, 1)
        public int frameDir;

        //! animation stuff
        public bool animating;

        public struct Anim
        {
            public short speed, speedBak;

            public struct AnimFramePtr
            {
                public AnimFrame[] _anims;

                public int Pos;

                public AnimFrame CurrentFrame
                {
                    get
                    {
                        return _anims[Pos];
                    }
                }
            }

            //! string based animation
            public struct StringAnim
            {
                public AnimFramePtr buffer;
                public AnimFramePtr curPos;
            }

            public StringAnim @string;

            //! normal moving animation
            public struct NormalAnim
            {
                public bool rebound;
                public ushort firstFrame, lastFrame;
            }

            public NormalAnim normal;

        }

        public Anim anim = new Anim();

        public bool moving;
        //! moving speed
        public short speed;
        //! move along x axis instead of y
        public bool xmajor;
        //! moving direction
        public sbyte xdir, ydir;
        //! destination point
        public short endx, endy;
        public ushort dx, dy;
        public ushort total;

        public void MoveOneStep()
        {
            if (xmajor)
            {
                if (x == endx)
                {
                    y = endy;
                    moving = false;
                }
                else
                {
                    x += xdir;
                    total += dy;
                    if (total > dx)
                    {
                        y += ydir;
                        total -= dx;
                    }
                }
            }
            else
            {
                if (y == endy)
                {
                    x = endx;
                    moving = false;
                }
                else
                {
                    y += ydir;
                    total += dx;
                    if (total > dy)
                    {
                        x += xdir;
                        total -= dy;
                    }
                }
            }
        }

        public void AnimOneStep()
        {
            if (anim.@string.buffer._anims != null)
            {
                --anim.speed;
                if (anim.speed <= 0)
                {
                    // jump to next entry
                    ++anim.@string.curPos.Pos;
                    ushort nextFrame = anim.@string.curPos.CurrentFrame.frame;
                    if (nextFrame == 0)
                    {
                        anim.@string.curPos = anim.@string.buffer;
                        frameNum = anim.@string.curPos.CurrentFrame.frame;
                    }
                    else
                    {
                        frameNum = nextFrame;
                    }
                    anim.speed = (short)(anim.@string.curPos.CurrentFrame.speed / 4);
                }
            }
            else
            {
                // normal looping animation
                --anim.speed;
                if (anim.speed == 0)
                {
                    anim.speed = anim.speedBak;

                    short nextFrame = (short)(frameNum + frameDir);
                    if (nextFrame > anim.normal.lastFrame || nextFrame < anim.normal.firstFrame)
                    {
                        if (anim.normal.rebound)
                        {
                            frameDir *= -1;
                        }
                        else
                        {
                            frameNum = (ushort)(anim.normal.firstFrame - 1);
                        }
                    }
                    frameNum = (ushort)(frameNum + frameDir);
                }
            }
        }

        public void Clear(Box defaultBox)
        {
            active = false;
            xflip = false;
            animating = false;
            anim.@string.buffer = new Anim.AnimFramePtr();
            moving = false;
            scale = 100;
            box = new Box(defaultBox);
        }

        public void CurPos(short xx, short yy)
        {
            active = true;
            x = xx;
            y = yy;
        }

        public void AnimNormal(ushort firstFrame, ushort lastFrame, ushort spd, bool rebound, bool flip)
        {
            active = true;
            animating = true;
            frameNum = firstFrame;
            anim.speed = (short)spd;
            anim.speedBak = (short)spd;
            anim.@string.buffer = new Anim.AnimFramePtr();
            anim.normal.firstFrame = firstFrame;
            anim.normal.lastFrame = lastFrame;
            anim.normal.rebound = rebound;
            frameDir = 1;
            xflip = flip;
        }

        public void AnimString(AnimFrame[] animBuf)
        {
            active = true;
            animating = true;
            anim.@string.buffer = new Anim.AnimFramePtr { _anims = animBuf };
            anim.@string.curPos = new Anim.AnimFramePtr { _anims = animBuf };

            frameNum = animBuf[0].frame;
            anim.speed = (short)(animBuf[0].speed / 4);
        }

        public void Move(short dstx, short dsty, short spd)
        {
            active = true;
            moving = true;

            endx = dstx;
            endy = dsty;

            speed = (short)((spd < 1) ? 1 : spd);

            short deltax = (short)(endx - x);
            if (deltax < 0)
            {
                dx = (ushort)-deltax;
                xdir = -1;
            }
            else
            {
                dx = (ushort)deltax;
                xdir = 1;
            }
            short deltay = (short)(endy - y);
            if (deltay < 0)
            {
                dy = (ushort)-deltay;
                ydir = -1;
            }
            else
            {
                dy = (ushort)deltay;
                ydir = 1;
            }

            if (dx > dy)
            {
                total = (ushort)(dy / 2);
                xmajor = true;
            }
            else
            {
                total = (ushort)(dx / 2);
                xmajor = false;
            }

            // move one step along line to avoid glitching
            MoveOneStep();

        }

        public void ScaleWalkSpeed(ushort ms)
        {
            if (!xmajor)
            {
                ms /= 2;
            }
            speed = (short)(scale * ms / 100);
            if (speed == 0)
            {
                speed = 1;
            }
        }
    }

    public class BobFrame
    {
        public ushort width, height;
        public ushort xhotspot, yhotspot;
        public byte[] data;

        public void Reset()
        {
            width = 0;
            height = 0;
            xhotspot = 0;
            yhotspot = 0;
            data = null;
        }
    }

    class BobSlotComparer : Comparer<BobSlot>
    {
        public static readonly BobSlotComparer Instance = new BobSlotComparer();

        public override int Compare(BobSlot x, BobSlot y)
        {
            //// As the qsort() function may reorder "equal" elements,
            //// we use the bob slot number when needed. This is required
            //// during the introduction, to hide a crate behind the clock.
            return x.y.CompareTo(y.y);
        }
    }

    public class Graphics
    {
        public const int BOB_OBJ1 = 5;
        public const int BOB_OBJ2 = 6;
        public const int BOB_FX = 7;

        public const int ARROW_BOB_UP = 62;
        public const int ARROW_BOB_DOWN = 63;
        private const int MAX_BOBS_NUMBER = 64;
        private const int MAX_STRING_LENGTH = 255;
        private const int MAX_STRING_SIZE = (MAX_STRING_LENGTH + 1);

        private const int BOB_SHRINK_BUF_SIZE = 60000;

        private QueenEngine _vm;
        private Box _defaultBox;
        private Box _gameScreenBox;
        private Box _fullScreenBox;
        /// <summary>
        /// used to scale a BobFrame
        /// </summary>
        private BobFrame _shrinkBuffer;
        private ushort[] _personFrames = new ushort[4];
        /// <summary>
        /// In-game objects/persons animations.
        /// </summary>
        private AnimFrame[][] _newAnim;
        /// <summary>
        /// Cutaway objects/persons animations.
        /// </summary>
        private AnimFrame[][] _cutAnim;

        /// <summary>
        /// Bob number followed by camera.
        /// </summary>
        private int _cameraBob;

        /// <summary>
        /// Number of static furniture in current room.
        /// </summary>
        private ushort _numFurnitureStatic;

        /// <summary>
        /// Number of animated furniture in current room
        /// </summary>
        private ushort _numFurnitureAnimated;

        /// <summary>
        /// Total number of frames for the animated furniture
        /// </summary>
        private ushort _numFurnitureAnimatedLen;

        /// <summary>
        /// Number of bobs to display.
        /// </summary>
        private ushort _sortedBobsCount;
        private BobSlot[] _sortedBobs = new BobSlot[MAX_BOBS_NUMBER];

        public BobSlot[] Bobs { get; private set; }
        public short[] PersonFrames { get; private set; }
        public ushort NumStaticFurniture { get { return _numFurnitureStatic; } }
        public ushort NumAnimatedFurniture { get { return _numFurnitureAnimated; } }

        public ushort NumFurnitureFrames { get { return (ushort)(_numFurnitureStatic + _numFurnitureAnimatedLen); } }

        /// <summary>
        /// Gets the current number of frames unpacked.
        /// </summary>
        /// <value>The current number of frames unpacked.</value>
        public ushort NumFrames
        {
            get;
            private set;
        }

        static readonly byte[] defaultAmigaCursor = {
            0x00, 0x00, 0xFF, 0xC0,
            0x7F, 0x80, 0x80, 0x40,
            0x7F, 0x00, 0x80, 0x80,
            0x7E, 0x00, 0x81, 0x00,
            0x7F, 0x00, 0x80, 0x80,
            0x7F, 0x80, 0x80, 0x40,
            0x7F, 0xC0, 0x80, 0x20,
            0x6F, 0xE0, 0x90, 0x10,
            0x47, 0xF0, 0xA8, 0x08,
            0x03, 0xF8, 0xC4, 0x04,
            0x01, 0xFC, 0x02, 0x02,
            0x00, 0xF8, 0x01, 0x04,
            0x00, 0x70, 0x00, 0x88,
            0x00, 0x20, 0x00, 0x50,
            0x00, 0x00, 0x00, 0x20
        };

        public Graphics(QueenEngine vm)
        {
            _vm = vm;
            _defaultBox = new Box(-1, -1, -1, -1);
            _gameScreenBox = new Box(0, 0, Defines.GAME_SCREEN_WIDTH - 1, Defines.ROOM_ZONE_HEIGHT - 1);
            _fullScreenBox = new Box(0, 0, Defines.GAME_SCREEN_WIDTH - 1, Defines.GAME_SCREEN_HEIGHT - 1);

            _shrinkBuffer = new BobFrame();
            _shrinkBuffer.data = new byte[BOB_SHRINK_BUF_SIZE];
            Bobs = new BobSlot[MAX_BOBS_NUMBER];
            for (var i = 0; i < Bobs.Length; ++i)
            {
                Bobs[i] = new BobSlot();
            }
            _newAnim = new AnimFrame[17][];
            for (int i = 0; i < 17; i++)
            {
                _newAnim[i] = new AnimFrame[30];
            }
            _cutAnim = new AnimFrame[21][];
            for (int i = 0; i < 21; i++)
            {
                _cutAnim[i] = new AnimFrame[30];
            }
        }

        public void SetupArrows()
        {
            if (_vm.Resource.Platform == Platform.DOS)
            {
                int scrollX = _vm.Display.HorizontalScroll;
                BobSlot arrow;
                arrow = Bobs[ARROW_BOB_UP];
                arrow.CurPos((short)(303 + 8 + scrollX), 150 + 1 + 200);
                arrow.frameNum = 3;
                arrow = Bobs[ARROW_BOB_DOWN];
                arrow.CurPos((short)(303 + scrollX), 175 + 200);
                arrow.frameNum = 4;
            }
        }

        public void SetBobCutawayAnim(ushort bobNum, bool xflip, AnimFrame[] af, byte frameCount)
        {
            Debug.Assert(bobNum < 21 && frameCount < 30);
            Array.Copy(af, _cutAnim[bobNum], frameCount);
            Bobs[bobNum].xflip = xflip;
            Bobs[bobNum].AnimString(_cutAnim[bobNum]);
        }

        public void SetBobText(BobSlot pbs, string text, int textX, short textY, int color, int flags)
        {
            if (text.Length == 0)
                return;

            // Split text into lines

            var lines = new List<string>();
            int lineLength = 0;
            int i;

            // TODO: Hebrew strings are written from right to left and should be cut
            // to lines in reverse
            if (_vm.Resource.Language == Language.HE_ISR)
            {
                //    for (i = length - 1; i >= 0; i--)
                //    {
                //        lineLength++;

                //        if ((lineLength > 20 && textCopy[i] == ' ') || i == 0)
                //        {
                //            memcpy(lines[lineCount], textCopy + i, lineLength);
                //            lines[lineCount][lineLength] = '\0';
                //            lineCount++;
                //            lineLength = 0;
                //        }
                //    }
            }
            else
            {
                for (i = 0; i < text.Length; i++)
                {
                    lineLength++;

                    if ((lineLength > 20 && text[i] == ' ') || i == (text.Length - 1))
                    {
                        lines.Add(text.Substring(i + 1 - lineLength, lineLength));
                        lineLength = 0;
                    }
                }
            }

            // Find width of widest line

            int maxLineWidth = 0;

            for (i = 0; i < lines.Count; i++)
            {
                int tWidth = _vm.Display.TextWidth(lines[i]);
                if (maxLineWidth < tWidth)
                    maxLineWidth = tWidth;
            }

            // Calc text position

            short x, y, width;

            if (flags != 0)
            {
                if (flags == 2)
                    x = (short)(160 - maxLineWidth / 2);
                else
                    x = (short)textX;

                y = textY;

                width = 0;
            }
            else
            {
                x = pbs.x;
                y = pbs.y;

                BobFrame pbf = _vm.BankMan.FetchFrame(pbs.frameNum);

                width = (short)((pbf.width * pbs.scale) / 100);
                short height = (short)((pbf.height * pbs.scale) / 100);

                y = (short)(y - height - 16 - lines.Count * 9);
            }

            x -= _vm.Display.HorizontalScroll;

            if (y < 0)
            {
                y = 0;

                if (x < 160)
                    x = (short)(x + width / 2);
                else
                    x = (short)(x - width / 2 + maxLineWidth);
            }
            else if (flags == 0)
                x = (short)(x - maxLineWidth / 2);

            if (x < 0)
                x = 4;
            else if ((x + maxLineWidth) > 320)
                x = (short)(320 - maxLineWidth - 4);

            // remap some colors for the Amiga
            if (_vm.Resource.Platform == Platform.Amiga)
            {
                if (color == 5)
                {
                    color = (_vm.Logic.CurrentRoom == 9) ? 15 : 11;
                }
                else if (color == 10 && _vm.Logic.CurrentRoom == 100)
                {
                    color = 11;
                }
            }

            _vm.Display.TextCurrentColor((byte)color);

            for (i = 0; i < lines.Count; i++)
            {
                int lineX = x + (maxLineWidth - _vm.Display.TextWidth(lines[i])) / 2;

                D.Debug(7, $"Setting text '{lines[i]}' at ({lineX}, {y + 9 * i})");
                _vm.Display.SetText((ushort)lineX, (ushort)(y + 9 * i), lines[i]);
            }
        }

        public void ClearBob(int index)
        {
            Bobs[index].Clear(_defaultBox);
        }

        public void StopBobs()
        {
            for (int i = 0; i < Bobs.Length; ++i)
            {
                Bobs[i].moving = false;
            }
        }

        public void ResetPersonAnim(ushort bobNum)
        {
            if (_newAnim[bobNum][0].frame != 0)
            {
                Bobs[bobNum].AnimString(_newAnim[bobNum]);
            }
        }

        public ushort RefreshObject(ushort obj)
        {
            D.Debug(6, $"Graphics::refreshObject({obj:X})");
            ushort curImage = NumFrames;

            ObjectData pod = _vm.Logic.ObjectData[obj];
            if (pod.image == 0)
            {
                return curImage;
            }

            // check the object is in the current room
            if (pod.room != _vm.Logic.CurrentRoom)
            {
                return curImage;
            }

            // find bob for the object
            ushort curBob = _vm.Logic.FindBob((short)obj);
            BobSlot pbs = Bobs[curBob];

            if (pod.image == -3 || pod.image == -4)
            {
                // a person object
                if (pod.name <= 0)
                {
                    pbs.Clear(_defaultBox);
                }
                else
                {
                    // find person number
                    ushort pNum = _vm.Logic.FindPersonNumber(obj, _vm.Logic.CurrentRoom);
                    curImage = (ushort)(_personFrames[pNum] - 1);
                    if (_personFrames[pNum] == 0)
                    {
                        _personFrames[pNum] = curImage = NumFrames;
                    }
                    curImage = SetupPerson((ushort)(obj - _vm.Logic.CurrentRoomData), curImage);
                }
                return curImage;
            }

            // find frame used for object
            curImage = _vm.Logic.FindFrame(obj);

            if (pod.name < 0 || pod.image < 0)
            {
                // object is hidden or disabled
                pbs.Clear(_defaultBox);
                return curImage;
            }

            int image = pod.image;
            if (image > 5000)
            {
                image -= 5000;
            }

            GraphicData pgd = _vm.Logic.GraphicData[image];
            bool rebound = false;
            short lastFrame = pgd.lastFrame;
            if (lastFrame < 0)
            {
                lastFrame = (short)-lastFrame;
                rebound = true;
            }
            if (pgd.firstFrame < 0)
            {
                SetupObjectAnim(pgd, curImage, curBob, pod.name != 0);
                curImage = (ushort)(curImage + (pgd.lastFrame - 1));
            }
            else if (lastFrame != 0)
            {
                // turn on an animated bob
                pbs.animating = false;
                ushort firstImage = curImage;
                --curImage;
                ushort j;
                for (j = (ushort)pgd.firstFrame; j <= lastFrame; ++j)
                {
                    ++curImage;
                    _vm.BankMan.Unpack(j, curImage, 15);
                }
                pbs.CurPos((short)pgd.x, (short)pgd.y);
                pbs.frameNum = firstImage;
                if (pgd.speed > 0)
                {
                    pbs.AnimNormal(firstImage, curImage, (ushort)(pgd.speed / 4), rebound, false);
                }
            }
            else
            {
                _vm.BankMan.Unpack((uint)pgd.firstFrame, curImage, 15);
                pbs.CurPos((short)pgd.x, (short)pgd.y);
                pbs.frameNum = curImage;
            }

            return curImage;
        }

        public void Update(ushort room)
        {
            SortBobs();
            if (_cameraBob >= 0)
            {
                _vm.Display.HorizontalScrollUpdate(Bobs[_cameraBob].x);
            }
            HandleParallax(room);
            _vm.Display.PrepareUpdate();
            DrawBobs();
        }

        public void DrawInventoryItem(uint frameNum, ushort x, ushort y)
        {
            if (frameNum != 0)
            {
                var bf = _vm.BankMan.FetchFrame(frameNum);
                _vm.Display.DrawInventoryItem(bf.data, x, y, bf.width, bf.height);
            }
            else
            {
                _vm.Display.DrawInventoryItem(null, x, y, 32, 32);
            }
        }

        public void EraseAllAnims()
        {
            for (int i = 1; i <= 16; ++i)
            {
                _newAnim[i][0].frame = 0;
            }
        }

        public void ClearPersonFrames()
        {
            Array.Clear(_personFrames, 0, _personFrames.Length);
        }

        public void SetupNewRoom(string room, ushort roomNum, short[] furniture, ushort furnitureCount)
        {
            // reset sprites table
            ClearBobs();

            // load/setup objects associated to this room
            string filename = $"{room}.BBK";
            _vm.BankMan.Load(filename, 15);

            NumFrames = Defines.FRAMES_JOE + 1;
            SetupRoomFurniture(furniture, furnitureCount);
            SetupRoomObjects();

            if (roomNum >= 90)
            {
                PutCameraOnBob(0);
            }
        }

        public void PutCameraOnBob(int bobNum)
        {
            _cameraBob = bobNum;
        }

        public void UnpackControlBank()
        {
            if (_vm.Resource.Platform == Platform.DOS)
            {
                _vm.BankMan.Load("CONTROL.BBK", 17);

                // unpack mouse pointer frame
                _vm.BankMan.Unpack(1, 1, 17);

                // unpack arrows frames and change hotspot to be always on top
                _vm.BankMan.Unpack(3, 3, 17);
                _vm.BankMan.FetchFrame(3).yhotspot += 200;
                _vm.BankMan.Unpack(4, 4, 17);
                _vm.BankMan.FetchFrame(4).yhotspot += 200;

                _vm.BankMan.Close(17);
            }
        }

        public void SetupMouseCursor()
        {
            if (_vm.Resource.Platform == Platform.Amiga)
            {

                byte[] cursorData = new byte[16 * 15];
                var src = defaultAmigaCursor;
                var srcPos = 0;
                int i = 0;
                for (int h = 0; h < 15; ++h)
                {
                    for (int b = 0; b < 16; ++b)
                    {
                        ushort mask = (ushort)(1 << (15 - b));
                        byte color = 0;
                        if ((src.ToUInt16BigEndian(srcPos) & mask) != 0)
                        {
                            color |= 2;
                        }
                        if ((src.ToUInt16BigEndian(srcPos + 2) & mask) != 0)
                        {
                            color |= 1;
                        }
                        if (color != 0)
                        {
                            cursorData[i] = (byte)(0x90 + color - 1);
                        }
                        ++i;
                    }
                    srcPos += 4;
                }
                _vm.Display.SetMouseCursor(cursorData, 16, 15);
            }
            else
            {
                BobFrame bf = _vm.BankMan.FetchFrame(1);
                _vm.Display.SetMouseCursor(bf.data, bf.width, bf.height);
            }
        }

        public void DrawBobs()
        {
            Box bobBox = _vm.Display.Fullscreen ? _fullScreenBox : _gameScreenBox;
            for (int i = 0; i < _sortedBobsCount; ++i)
            {
                BobSlot pbs = _sortedBobs[i];
                if (pbs.active)
                {
                    BobFrame pbf = _vm.BankMan.FetchFrame(pbs.frameNum);

                    ushort xh = pbf.xhotspot;
                    ushort yh = pbf.yhotspot;

                    if (pbs.xflip)
                    {
                        xh = (ushort)(pbf.width - xh);
                    }

                    // adjusts hot spots when object is scaled
                    if (pbs.scale != 100)
                    {
                        xh = (ushort)((xh * pbs.scale) / 100);
                        yh = (ushort)((yh * pbs.scale) / 100);
                    }

                    // adjusts position to hot-spot and screen scroll
                    ushort x = (ushort)(pbs.x - xh - _vm.Display.HorizontalScroll);
                    ushort y = (ushort)(pbs.y - yh);

                    DrawBob(pbs, pbf, bobBox, (short)x, (short)y);
                }
            }
        }

        private void DrawBob(BobSlot bs, BobFrame bf, Box bbox, short x, short y)
        {
            D.Debug(9, $"Graphics::drawBob({bs.frameNum}, {x}, {y})");

            ushort w, h;
            if (bs.scale < 100)
            {
                ShrinkFrame(bf, bs.scale);
                bf = _shrinkBuffer;
            }
            w = bf.width;
            h = bf.height;

            Box box = (bs.box == _defaultBox) ? bbox : bs.box;

            if (w != 0 && h != 0 && box.Intersects(x, y, w, h))
            {
                var src = bf.data;
                var s = 0;
                ushort x_skip = 0;
                ushort y_skip = 0;
                ushort w_new = w;
                ushort h_new = h;

                // compute bounding box intersection with frame
                if (x < box.x1)
                {
                    x_skip = (ushort)(box.x1 - x);
                    w_new -= x_skip;
                    x = box.x1;
                }

                if (y < box.y1)
                {
                    y_skip = (ushort)(box.y1 - y);
                    h_new -= y_skip;
                    y = box.y1;
                }

                if (x + w_new > box.x2 + 1)
                {
                    w_new = (ushort)(box.x2 - x + 1);
                }

                if (y + h_new > box.y2 + 1)
                {
                    h_new = (ushort)(box.y2 - y + 1);
                }

                s += w * y_skip;
                if (!bs.xflip)
                {
                    s += x_skip;
                }
                else
                {
                    s += w - w_new - x_skip;
                    x = (short)(x + w_new - 1);
                }
                _vm.Display.DrawBobSprite(src, s, (ushort)x, (ushort)y, w_new, h_new, w, bs.xflip);
            }

        }

        private void ShrinkFrame(BobFrame bf, ushort percentage)
        {
            // computing new size, rounding to upper value
            ushort new_w = (ushort)((bf.width * percentage + 50) / 100);
            ushort new_h = (ushort)((bf.height * percentage + 50) / 100);
            Debug.Assert(new_w * new_h < BOB_SHRINK_BUF_SIZE);

            if (new_w != 0 && new_h != 0)
            {
                _shrinkBuffer.width = new_w;
                _shrinkBuffer.height = new_h;

                ushort x, y;
                ushort[] sh = new ushort[Defines.GAME_SCREEN_WIDTH];
                for (x = 0; x < Math.Max(new_h, new_w); ++x)
                {
                    sh[x] = (ushort)(x * 100 / percentage);
                }
                var dst = _shrinkBuffer.data;
                var d = 0;
                for (y = 0; y < new_h; ++y)
                {
                    var p = sh[y] * bf.width;
                    for (x = 0; x < new_w; ++x)
                    {
                        dst[d++] = bf.data[p + sh[x]];
                    }
                }
            }
        }

        private void HandleParallax(ushort roomNum)
        {
            var screenScroll = _vm.Display.HorizontalScroll;
            switch (roomNum)
            {
                case Defines.ROOM_AMAZON_HIDEOUT:
                    Bobs[8].x = (short)(250 - screenScroll / 2);
                    break;
                case Defines.ROOM_TEMPLE_MAZE_5:
                    Bobs[5].x = (short)(410 - screenScroll / 2);
                    Bobs[6].x = (short)(790 - screenScroll / 2);
                    break;
                case Defines.ROOM_TEMPLE_OUTSIDE:
                    Bobs[5].x = (short)(320 - screenScroll / 2);
                    break;
                case Defines.ROOM_TEMPLE_TREE:
                    Bobs[5].x = (short)(280 - screenScroll / 2);
                    break;
                case Defines.ROOM_VALLEY_CARCASS:
                    Bobs[5].x = (short)(600 - screenScroll / 2);
                    break;
                case Defines.ROOM_UNUSED_INTRO_1:
                    Bobs[5].x = (short)(340 - screenScroll / 2);
                    Bobs[6].x = (short)(50 - screenScroll / 2);
                    Bobs[7].x = (short)(79 - screenScroll / 2);
                    break;
                case Defines.ROOM_CAR_CHASE:
                    _vm.Bam.UpdateCarAnimation();
                    break;
                case Defines.ROOM_FINAL_FIGHT:
                    _vm.Bam.UpdateFightAnimation();
                    break;
                case Defines.ROOM_INTRO_RITA_JOE_HEADS:
                    _cameraBob = -1;
                    if (screenScroll < 80)
                    {
                        _vm.Display.HorizontalScroll = (short)(screenScroll + 4);
                        // Joe's body and head
                        Bobs[1].x += 4;
                        Bobs[20].x += 4;
                        // Rita's body and head
                        Bobs[2].x -= 2;
                        Bobs[21].x -= 2;
                    }
                    break;
                case Defines.ROOM_INTRO_EXPLOSION:
                    Bobs[21].x += 2;
                    Bobs[21].y += 2;
                    break;
            }
        }

        public void SortBobs()
        {
            _sortedBobsCount = 0;

            // animate/move the bobs
            for (var i = 0; i < Bobs.Length; ++i)
            {
                BobSlot pbs = Bobs[i];
                if (pbs.active)
                {
                    _sortedBobs[_sortedBobsCount] = pbs;
                    ++_sortedBobsCount;

                    if (pbs.animating)
                    {
                        pbs.AnimOneStep();
                        if (pbs.frameNum > 500)
                        { // SFX frame
                            _vm.Sound.PlaySfx(_vm.Logic.CurrentRoomSfx);
                            pbs.frameNum -= 500;
                        }
                    }
                    if (pbs.moving)
                    {
                        int j;
                        for (j = 0; pbs.moving && j < pbs.speed; ++j)
                        {
                            pbs.MoveOneStep();
                        }
                    }
                }
            }
            Array.Sort(_sortedBobs, 0, _sortedBobsCount, BobSlotComparer.Instance);
        }

        private void SetupRoomObjects()
        {
            ushort i;
            // furniture frames are reserved in ::setupRoomFurniture(), we append objects
            // frames after the furniture ones.
            ushort curImage = (ushort)(Defines.FRAMES_JOE + _numFurnitureStatic + _numFurnitureAnimatedLen);
            ushort firstRoomObj = (ushort)(_vm.Logic.CurrentRoomData + 1);
            ushort lastRoomObj = _vm.Logic.RoomData[_vm.Logic.CurrentRoom + 1];
            ushort numObjectStatic = 0;
            ushort numObjectAnimated = 0;
            ushort curBob;

            // invalidates all Bobs for persons (except Joe's one)
            for (i = 1; i <= 3; ++i)
            {
                Bobs[i].active = false;
            }

            // static/animated Bobs
            for (i = firstRoomObj; i <= lastRoomObj; ++i)
            {
                ObjectData pod = _vm.Logic.ObjectData[i];
                // setup blanks bobs for turned off objects (in case
                // you turn them on again)
                if (pod.image == -1)
                {
                    // static OFF Bob
                    curBob = (ushort)(20 + _numFurnitureStatic + numObjectStatic);
                    ++numObjectStatic;
                    // create a blank frame for the OFF object
                    ++NumFrames;
                    ++curImage;
                }
                else if (pod.image == -2)
                {
                    // animated OFF Bob
                    curBob = (ushort)(5 + _numFurnitureAnimated + numObjectAnimated);
                    ++numObjectAnimated;
                }
                else if (pod.image > 0 && pod.image < 5000)
                {
                    GraphicData pgd = _vm.Logic.GraphicData[pod.image];
                    short lastFrame = pgd.lastFrame;
                    bool rebound = false;
                    if (lastFrame < 0)
                    {
                        lastFrame = (short)-lastFrame;
                        rebound = true;
                    }
                    if (pgd.firstFrame < 0)
                    {
                        curBob = (ushort)(5 + _numFurnitureAnimated);
                        SetupObjectAnim(pgd, (ushort)(curImage + 1), (ushort)(curBob + numObjectAnimated), pod.name > 0);
                        curImage = (ushort)(curImage + pgd.lastFrame);
                        ++numObjectAnimated;
                    }
                    else if (lastFrame != 0)
                    {
                        // animated objects
                        ushort j;
                        ushort firstFrame = (ushort)(curImage + 1);
                        for (j = (ushort)pgd.firstFrame; j <= lastFrame; ++j)
                        {
                            ++curImage;
                            _vm.BankMan.Unpack(j, curImage, 15);
                            ++NumFrames;
                        }
                        curBob = (ushort)(5 + _numFurnitureAnimated + numObjectAnimated);
                        if (pod.name > 0)
                        {
                            BobSlot pbs = Bobs[curBob];
                            pbs.CurPos((short)pgd.x, (short)pgd.y);
                            pbs.frameNum = firstFrame;
                            if (pgd.speed > 0)
                            {
                                pbs.AnimNormal(firstFrame, curImage, (ushort)(pgd.speed / 4), rebound, false);
                            }
                        }
                        ++numObjectAnimated;
                    }
                    else
                    {
                        // static objects
                        curBob = (ushort)(20 + _numFurnitureStatic + numObjectStatic);
                        ++curImage;
                        Bobs[curBob].Clear(_defaultBox);
                        _vm.BankMan.Unpack((uint)pgd.firstFrame, curImage, 15);
                        ++NumFrames;
                        if (pod.name > 0)
                        {
                            BobSlot pbs = Bobs[curBob];
                            pbs.CurPos((short)pgd.x, (short)pgd.y);
                            pbs.frameNum = curImage;
                        }
                        ++numObjectStatic;
                    }
                }
            }

            // persons Bobs
            for (i = firstRoomObj; i <= lastRoomObj; ++i)
            {
                ObjectData pod = _vm.Logic.ObjectData[i];
                if (pod.image == -3 || pod.image == -4)
                {
                    D.Debug(6, $"Graphics::setupRoomObjects() - Setting up person {i:X}, name={pod.name:X}");
                    ushort noun = (ushort)(i - _vm.Logic.CurrentRoomData);
                    if (pod.name > 0)
                    {
                        curImage = SetupPerson(noun, curImage);
                    }
                    else
                    {
                        curImage = AllocPerson(noun, curImage);
                    }
                }
            }

            // paste downs list
            ++curImage;
            NumFrames = curImage;
            for (i = firstRoomObj; i <= lastRoomObj; ++i)
            {
                ObjectData pod = _vm.Logic.ObjectData[i];
                if (pod.name > 0 && pod.image > 5000)
                {
                    PasteBob((ushort)(pod.image - 5000), curImage);
                }
            }

        }

        private ushort AllocPerson(ushort noun, ushort curImage)
        {
            Person p;
            if (_vm.Logic.InitPerson(noun, "", false, out p) && p.anim != null)
            {
                curImage += CountAnimFrames(p.anim);
                _personFrames[p.actor.bobNum] = (ushort)(curImage + 1);
            }
            return curImage;
        }

        private ushort CountAnimFrames(string anim)
        {
            var afbuf = new AnimFrame[30];
            FillAnimBuffer(anim, afbuf);

            var frames = new bool[256];
            ushort count = 0;
            var af = 0;
            for (; afbuf[af].frame != 0; ++af)
            {
                ushort frameNum = afbuf[af].frame;
                if (frameNum > 500)
                {
                    frameNum -= 500;
                }
                if (!frames[frameNum])
                {
                    frames[frameNum] = true;
                    ++count;
                }
            }
            return count;
        }

        private ushort SetupPerson(ushort noun, ushort curImage)
        {
            if (noun == 0)
            {
                D.Warning("Trying to setup person 0");
                return curImage;
            }

            Person p;
            if (!_vm.Logic.InitPerson(noun, "", true, out p))
            {
                return curImage;
            }

            ActorData pad = p.actor;
            ushort scale = 100;
            ushort a = _vm.Grid.FindAreaForPos(GridScreen.ROOM, pad.x, pad.y);
            if (a != 0)
            {
                // person is not standing in the area box, scale it accordingly
                scale = _vm.Grid.Areas[_vm.Logic.CurrentRoom][a].CalcScale((short)pad.y);
            }

            _vm.BankMan.Unpack(pad.bobFrameStanding, p.bobFrame, p.actor.bankNum);
            ushort obj = (ushort)(_vm.Logic.CurrentRoomData + noun);
            BobSlot pbs = Bobs[pad.bobNum];
            pbs.CurPos((short)pad.x, (short)pad.y);
            pbs.scale = scale;
            pbs.frameNum = p.bobFrame;
            pbs.xflip = (_vm.Logic.ObjectData[obj].image == -3); // person is facing left

            D.Debug(6, $"Graphics::setupPerson({noun}, {curImage}) - bob = {pad.bobNum} name = {p.name}");

            if (p.anim != null)
            {
                curImage = SetupPersonAnim(pad, p.anim, curImage);
            }
            else
            {
                ErasePersonAnim((ushort)pad.bobNum);
            }
            return curImage;
        }

        private void ErasePersonAnim(ushort bobNum)
        {
            _newAnim[bobNum][0].frame = 0;
            BobSlot pbs = Bobs[bobNum];
            pbs.animating = false;
            pbs.anim.@string.buffer = new BobSlot.Anim.AnimFramePtr();
        }

        private ushort SetupPersonAnim(ActorData ad, string anim, ushort curImage)
        {
            D.Debug(9, $"Graphics::setupPersonAnim({anim}, {curImage})");
            _personFrames[ad.bobNum] = (ushort)(curImage + 1);

            var animFrames = _newAnim[ad.bobNum];
            FillAnimBuffer(anim, animFrames);
            ushort[] frameCount = new ushort[256];
            AnimFrame[] af = animFrames;
            var a = 0;
            for (; af[a].frame != 0; ++a)
            {
                ushort frameNum = af[a].frame;
                if (frameNum > 500)
                {
                    frameNum -= 500;
                }
                if (frameCount[frameNum] == 0)
                {
                    frameCount[frameNum] = 1;
                }
            }
            ushort i, n = 1;
            for (i = 1; i < 256; ++i)
            {
                if (frameCount[i] != 0)
                {
                    frameCount[i] = n;
                    ++n;
                }
            }
            a = 0;
            for (; af[a].frame != 0; ++a)
            {
                if (af[a].frame > 500)
                {
                    af[a].frame = (ushort)(curImage + frameCount[af[a].frame - 500] + 500);
                }
                else
                {
                    af[a].frame = (ushort)(curImage + frameCount[af[a].frame]);
                }
            }

            // unpack necessary frames
            for (i = 1; i < 256; ++i)
            {
                if (frameCount[i] != 0)
                {
                    ++curImage;
                    _vm.BankMan.Unpack(i, curImage, ad.bankNum);
                }
            }

            // start animation
            Bobs[ad.bobNum].AnimString(af);
            return curImage;
        }

        private void FillAnimBuffer(string anim, AnimFrame[] af)
        {
            var i = 0;
            var o = 0;
            string tmp;
            for (;;)
            {
                // anim frame format is "%3hu,%3hu," (frame number, frame speed)
                tmp = anim.Substring(o, 3);
                af[i].frame = ushort.Parse(tmp);
                o += 4;
                tmp = anim.Substring(o, 3);
                af[i].speed = (ushort)int.Parse(tmp);
                o += 4;
                if (af[i].frame == 0)
                    break;
                ++i;
            }
        }

        private void SetupObjectAnim(GraphicData gd, ushort firstImage, ushort bobNum, bool visible)
        {
            short[] tempFrames = new short[20];
            ushort numTempFrames = 0;
            ushort i, j;
            for (i = 1; i <= _vm.Logic.GraphicAnimCount; ++i)
            {
                GraphicAnim pga = _vm.Logic.GraphicAnim[i];
                if (pga.keyFrame == gd.firstFrame)
                {
                    short frame = pga.frame;
                    if (frame > 500)
                    { // SFX
                        frame -= 500;
                    }
                    bool foundMatchingFrame = false;
                    for (j = 0; j < numTempFrames; ++j)
                    {
                        if (tempFrames[j] == frame)
                        {
                            foundMatchingFrame = true;
                            break;
                        }
                    }
                    if (!foundMatchingFrame)
                    {
                        Debug.Assert(numTempFrames < 20);
                        tempFrames[numTempFrames] = frame;
                        ++numTempFrames;
                    }
                }
            }

            // sort found frames ascending
            bool swap = true;
            while (swap)
            {
                swap = false;
                for (i = 0; i < numTempFrames - 1; ++i)
                {
                    if (tempFrames[i] > tempFrames[i + 1])
                    {
                        ScummHelper.Swap(ref tempFrames[i], ref tempFrames[i + 1]);
                        swap = true;
                    }
                }
            }

            // queen.c l.962-980 / l.1269-1294
            for (i = 0; i < gd.lastFrame; ++i)
            {
                _vm.BankMan.Unpack((uint)Math.Abs(tempFrames[i]), (uint)(firstImage + i), 15);
            }
            BobSlot pbs = Bobs[bobNum];
            pbs.animating = false;
            if (visible)
            {
                pbs.CurPos((short)gd.x, (short)gd.y);
                if (tempFrames[0] < 0)
                {
                    pbs.xflip = true;
                }
                var paf = _newAnim[bobNum];
                var p = 0;
                for (i = 1; i <= _vm.Logic.GraphicAnimCount; ++i)
                {
                    GraphicAnim pga = _vm.Logic.GraphicAnim[i];
                    if (pga.keyFrame == gd.firstFrame)
                    {
                        ushort frameNr = 0;
                        for (j = 1; j <= gd.lastFrame; ++j)
                        {
                            if (pga.frame > 500)
                            {
                                if (pga.frame - 500 == tempFrames[j - 1])
                                {
                                    frameNr = (ushort)(j + firstImage - 1 + 500);
                                }
                            }
                            else if (pga.frame == tempFrames[j - 1])
                            {
                                frameNr = (ushort)(j + firstImage - 1);
                            }
                        }
                        paf[p].frame = frameNr;
                        paf[p].speed = pga.speed;
                        ++p;
                    }
                }
                paf[p].frame = 0;
                paf[p].speed = 0;
                pbs.AnimString(_newAnim[bobNum]);
            }
        }

        private void SetupRoomFurniture(short[] furniture, ushort furnitureCount)
        {
            ushort i;
            ushort curImage = Defines.FRAMES_JOE;

            // unpack the static bobs
            _numFurnitureStatic = 0;
            for (i = 1; i <= furnitureCount; ++i)
            {
                var obj = furniture[i];
                if (obj > 0 && obj <= 5000)
                {
                    GraphicData pgd = _vm.Logic.GraphicData[obj];
                    if (pgd.lastFrame == 0)
                    {
                        ++_numFurnitureStatic;
                        ++curImage;
                        _vm.BankMan.Unpack((uint)pgd.firstFrame, curImage, 15);
                        ++NumFrames;
                        BobSlot pbs = Bobs[19 + _numFurnitureStatic];
                        pbs.CurPos((short)pgd.x, (short)pgd.y);
                        pbs.frameNum = curImage;
                    }
                }
            }

            // unpack the animated bobs
            _numFurnitureAnimated = 0;
            _numFurnitureAnimatedLen = 0;
            ushort curBob = 0;
            for (i = 1; i <= furnitureCount; ++i)
            {
                ushort obj = (ushort)furniture[i];
                if (obj > 0 && obj <= 5000)
                {
                    GraphicData pgd = _vm.Logic.GraphicData[obj];

                    bool rebound = false;
                    short lastFrame = pgd.lastFrame;
                    if (lastFrame < 0)
                    {
                        rebound = true;
                        lastFrame = (short)-lastFrame;
                    }

                    if (lastFrame > 0)
                    {
                        _numFurnitureAnimatedLen = (ushort)(_numFurnitureAnimatedLen + lastFrame - pgd.firstFrame + 1);
                        ++_numFurnitureAnimated;
                        ushort image = (ushort)(curImage + 1);
                        int k;
                        for (k = pgd.firstFrame; k <= lastFrame; ++k)
                        {
                            ++curImage;
                            _vm.BankMan.Unpack((uint)k, curImage, 15);
                            ++NumFrames;
                        }
                        BobSlot pbs = Bobs[5 + curBob];
                        pbs.AnimNormal(image, curImage, (ushort)(pgd.speed / 4), rebound, false);
                        pbs.CurPos((short)pgd.x, (short)pgd.y);
                        ++curBob;
                    }
                }
            }

            // unpack the paste downs
            for (i = 1; i <= furnitureCount; ++i)
            {
                if (furniture[i] > 5000)
                {
                    PasteBob((ushort)(furniture[i] - 5000), (ushort)(curImage + 1));
                }
            }

        }

        private void PasteBob(ushort objNum, ushort image)
        {
            GraphicData pgd = _vm.Logic.GraphicData[objNum];
            _vm.BankMan.Unpack((uint)pgd.firstFrame, image, 15);
            BobFrame bf = _vm.BankMan.FetchFrame(image);
            _vm.Display.DrawBobPasteDown(bf.data, pgd.x, pgd.y, bf.width, bf.height);
            _vm.BankMan.EraseFrame(image);
        }

        public void ClearBobs()
        {
            for (var i = 0; i < Bobs.Length; ++i)
            {
                Bobs[i].Clear(_defaultBox);
            }
        }
    }
}

