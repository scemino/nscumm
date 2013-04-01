/*
 * This file is part of NScumm.
 * 
 * NScumm is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * NScumm is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with NScumm.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scumm4
{
    public class CostumeData
    {
        public byte[] active;
        public ushort animCounter;
        public byte soundCounter;
        public byte soundPos;
        public ushort stopped;
        public ushort[] curpos;
        public ushort[] start;
        public ushort[] end;
        public ushort[] frame;

        public ushort current;

        public CostumeData()
        {
            active = new byte[16];
            curpos = new ushort[16];
            start = new ushort[16];
            end = new ushort[16];
            frame = new ushort[16];
        }

        public void Reset()
        {
            current = 0;
            stopped = 0;
            for (int i = 0; i < 16; i++)
            {
                active[i] = 0;
                curpos[i] = start[i] = end[i] = frame[i] = 0xFFFF;
            }
        }
    }

    [Flags]
    public enum MoveFlags
    {
        NewLeg = 1,
        InLeg = 2,
        Turn = 4,
        LastLeg = 8,
        Frozen = 0x80
    }

    [Flags]
    public enum ObjectClass
    {
        NeverClip = 20,
        AlwaysClip = 21,
        IgnoreBoxes = 22,
        YFlip = 29,
        XFlip = 30,
        Player = 31,	// Actor is controlled by the player
        Untouchable = 32
    }

    [Flags]
    public enum BoxFlags
    {
        XFlip = 0x08,
        YFlip = 0x10,
        IgnoreScale = 0x20,
        PlayerOnly = 0x20,
        Locked = 0x40,
        Invisible = 0x80
    }

    public class Actor
    {
        public const int InvalidBox = 0xFF;

        /** The position of the actor inside the virtual screen. */
        protected Point _pos;

        public int _top, _bottom;
        public uint _width;
        public byte _number;
        public ushort _costume;
        public byte _room;

        public byte _talkColor;
        public int _talkFrequency;
        public byte _talkPan;
        public byte _talkVolume;
        public ushort _boxscale;
        public byte _scalex, _scaley;
        public byte _charset;
        public MoveFlags _moving;
        public bool _ignoreBoxes;
        public int _forceClip;

        public byte _initFrame;
        public byte _walkFrame;
        public byte _standFrame;
        public byte _talkStartFrame;
        public byte _talkStopFrame;

        private bool _needRedraw;

        public bool NeedRedraw
        {
            get { return _needRedraw; }
            set { _needRedraw = value; }
        }

        public bool _needBgReset, _visible;
        public byte _shadowMode;
        public bool _flip;
        public byte _frame;
        public byte _walkbox;
        public short _talkPosX, _talkPosY;
        public ushort _talkScript, _walkScript;
        public bool _ignoreTurns;
        public ushort[] _sound = new ushort[32];
        public CostumeData _cost;

        public byte[] Name { get; set; }

        protected struct ActorWalkData
        {
            public Point dest;           // Final destination point
            public byte destbox;         // Final destination box
            public short destdir;        // Final destination, direction to face at

            public Point cur;            // Last position
            public byte curbox;                  // Last box

            public Point next;           // Next position on our way to the destination, i.e. our intermediate destination

            public Point point3;
            public int deltaXFactor, deltaYFactor;
            public ushort xfrac, yfrac;
        }

        protected ushort[] _palette = new ushort[256];
        protected int _elevation;
        protected ushort _facing;
        protected ushort _targetFacing;
        protected uint _speedx, _speedy;
        protected byte _animProgress, _animSpeed;
        protected bool _costumeNeedsInit;
        protected ActorWalkData _walkdata;
        protected short[] _animVariable = new short[27];

        public Actor(ScummEngine scumm, byte id)
        {
            _scumm = scumm;
            _number = id;
        }

        //protected:
        public virtual void HideActor()
        {
            if (!_visible)
                return;

            Console.WriteLine("HideActor: {0}", _costume);

            if (_moving != 0)
            {
                StopActorMoving();
                StartAnimActor(_standFrame);
            }

            _visible = false;
            _cost.soundCounter = 0;
            _cost.soundPos = 0;
            NeedRedraw = false;
            _needBgReset = true;
        }

        public void ShowActor()
        {
            if (_scumm.CurrentRoom == 0 || _visible)
                return;

            Console.WriteLine("ShowActor: {0}", _costume);

            AdjustActorPos();

            // TODO:
            //_vm->ensureResourceLoaded(rtCostume, _costume);

            if (_costumeNeedsInit)
            {
                StartAnimActor(_initFrame);
                _costumeNeedsInit = false;
            }

            StopActorMoving();
            _visible = true;
            NeedRedraw = true;
        }

        public virtual void InitActor(int mode)
        {
            this.Name = null;
            if (mode == -1)
            {
                _top = _bottom = 0;
                NeedRedraw = false;
                _needBgReset = false;
                _costumeNeedsInit = false;
                _visible = false;
                _flip = false;
                _speedx = 8;
                _speedy = 2;
                _frame = 0;
                _walkbox = 0;
                _animProgress = 0;
                _animVariable = new short[27];
                _palette = new ushort[256];
                _sound = new ushort[32];
                _cost = new CostumeData();
                _walkdata = new ActorWalkData();
                _walkdata.point3.X = 32000;
                _walkScript = 0;
            }

            if (mode == 1 || mode == -1)
            {
                _costume = 0;
                _room = 0;
                _pos.X = 0;
                _pos.Y = 0;
                _facing = 180;
            }
            else if (mode == 2)
            {
                _facing = 180;
            }
            _elevation = 0;
            _width = 24;
            _talkColor = 15;
            _talkPosX = 0;
            _talkPosY = -80;
            _boxscale = _scaley = _scalex = 0xFF;
            _charset = 0;
            _sound = new ushort[32];
            _targetFacing = _facing;

            _shadowMode = 0;

            StopActorMoving();

            SetActorWalkSpeed(8, 2);

            _animSpeed = 0;

            _ignoreBoxes = false;
            _forceClip = 0;
            _ignoreTurns = false;

            _talkFrequency = 256;
            _talkPan = 64;
            _talkVolume = 127;

            _initFrame = 1;
            _walkFrame = 2;
            _standFrame = 3;
            _talkStartFrame = 4;
            _talkStopFrame = 5;

            _walkScript = 0;
            _talkScript = 0;

            _scumm.ClassData[_number] = 0;
        }

        public void PutActor()
        {
            PutActor(_pos.X, _pos.Y, _room);
        }

        public void PutActor(byte room)
        {
            PutActor(_pos.X, _pos.Y, room);
        }

        public void PutActor(short x, short y)
        {
            PutActor(x, y, _room);
        }

        public void PutActor(short dstX, short dstY, byte newRoom)
        {
            if (_visible && _scumm.CurrentRoom != newRoom && _scumm.GetTalkingActor() == _number)
            {
                _scumm.StopTalk();
            }

            _pos.X = dstX;
            _pos.Y = dstY;
            _room = newRoom;
            NeedRedraw = true;

            if (_scumm.Variables[ScummEngine.VariableEgo] == _number)
            {
                _scumm.EgoPositioned = true;
            }

            if (_visible)
            {
                if (IsInCurrentRoom())
                {
                    if (_moving != 0)
                    {
                        StopActorMoving();
                        StartAnimActor(_standFrame);
                    }
                    AdjustActorPos();
                }
                else
                {
                    HideActor();
                }
            }
            else
            {
                if (IsInCurrentRoom())
                    ShowActor();
            }
        }

        public void SetActorCostume(ushort c)
        {
            _costumeNeedsInit = true;

            if (_visible)
            {
                HideActor();
                _cost.Reset();
                _costume = c;
                ShowActor();
            }
            else
            {
                _costume = c;
                _cost.Reset();
            }

            for (int i = 0; i < 32; i++)
                _palette[i] = 0xFF;
        }

        public void SetActorWalkSpeed(uint newSpeedX, uint newSpeedY)
        {
            if (newSpeedX == _speedx && newSpeedY == _speedy)
                return;

            _speedx = newSpeedX;
            _speedy = newSpeedY;

            if (_moving != 0)
            {
                CalcMovementFactor(_walkdata.next);
            }
        }

        protected int CalcMovementFactor(Point next)
        {
            int diffX, diffY;
            int deltaXFactor, deltaYFactor;

            if (_pos == next)
                return 0;

            diffX = next.X - _pos.X;
            diffY = next.Y - _pos.Y;
            deltaYFactor = (int)_speedy << 16;

            if (diffY < 0)
                deltaYFactor = -deltaYFactor;

            deltaXFactor = deltaYFactor * diffX;
            if (diffY != 0)
            {
                deltaXFactor /= diffY;
            }
            else
            {
                deltaYFactor = 0;
            }

            if ((uint)Math.Abs(deltaXFactor) > (_speedx << 16))
            {
                deltaXFactor = (int)(_speedx << 16);
                if (diffX < 0)
                    deltaXFactor = -deltaXFactor;

                deltaYFactor = deltaXFactor * diffY;
                if (diffX != 0)
                {
                    deltaYFactor /= diffX;
                }
                else
                {
                    deltaXFactor = 0;
                }
            }

            _walkdata.cur = _pos;
            _walkdata.next = next;
            _walkdata.deltaXFactor = deltaXFactor;
            _walkdata.deltaYFactor = deltaYFactor;
            _walkdata.xfrac = 0;
            _walkdata.yfrac = 0;

            _targetFacing = (ushort)GetAngleFromPos(deltaXFactor, deltaYFactor, false);

            return ActorWalkStep();
        }

        private static int GetAngleFromPos(int x, int y, bool useATAN)
        {
            if (useATAN)
            {
                double temp = Math.Atan2((double)x, (double)-y);
                return NormalizeAngle((int)(temp * 180 / Math.PI));
            }
            else
            {
                if (Math.Abs(y) * 2 < Math.Abs(x))
                {
                    if (x > 0)
                        return 90;
                    return 270;
                }
                else
                {
                    if (y > 0)
                        return 180;
                    return 0;
                }
            }
        }

        private static ushort FetAngleFromPos(int x, int y, bool useATAN)
        {
            {
                if (useATAN)
                {
                    double temp = Math.Atan2((double)x, (double)-y);
                    return (ushort)NormalizeAngle((int)(temp * 180 / Math.PI));
                }
                else
                {
                    if (Math.Abs(y) * 2 < Math.Abs(x))
                    {
                        if (x > 0)
                            return 90;
                        return 270;
                    }
                    else
                    {
                        if (y > 0)
                            return 180;
                        return 0;
                    }
                }
            }
        }

        private static int NormalizeAngle(int angle)
        {
            int temp;
            temp = (angle + 360) % 360;
            return ToSimpleDir(true, temp) * 45;
        }

        private static int ToSimpleDir(bool dirType, int dir)
        {
            if (dirType)
            {
                short[] directions = new short[] { 22, 72, 107, 157, 202, 252, 287, 337 };
                for (int i = 0; i < 7; i++)
                    if (dir >= directions[i] && dir <= directions[i + 1])
                        return i + 1;
            }
            else
            {
                short[] directions = new short[] { 71, 109, 251, 289 };
                for (int i = 0; i < 3; i++)
                    if (dir >= directions[i] && dir <= directions[i + 1])
                        return i + 1;
            }
            return 0;
        }

        protected int ActorWalkStep()
        {
            int tmpX, tmpY;
            int distX, distY;
            int nextFacing;

            NeedRedraw = true;

            nextFacing = UpdateActorDirection(true);
            if (!_moving.HasFlag(MoveFlags.InLeg) || _facing != nextFacing)
            {
                if (_walkFrame != _frame || _facing != nextFacing)
                {
                    StartWalkAnim(1, nextFacing);
                }
                _moving |= MoveFlags.InLeg;
            }

            if (_walkbox != _walkdata.curbox && _scumm.CheckXYInBoxBounds(_walkdata.curbox, _pos.X, _pos.Y))
            {
                SetBox(_walkdata.curbox);
            }

            distX = Math.Abs(_walkdata.next.X - _walkdata.cur.X);
            distY = Math.Abs(_walkdata.next.Y - _walkdata.cur.Y);

            if (Math.Abs(_pos.X - _walkdata.cur.X) >= distX && Math.Abs(_pos.Y - _walkdata.cur.Y) >= distY)
            {
                _moving &= ~MoveFlags.InLeg;
                return 0;
            }

            tmpX = (_pos.X << 16) + _walkdata.xfrac + (_walkdata.deltaXFactor >> 8) * _scalex;
            _walkdata.xfrac = (ushort)tmpX;
            _pos.X = (short)(tmpX >> 16);

            tmpY = (_pos.Y << 16) + _walkdata.yfrac + (_walkdata.deltaYFactor >> 8) * _scaley;
            _walkdata.yfrac = (ushort)tmpY;
            _pos.Y = (short)(tmpY >> 16);

            if (Math.Abs(_pos.X - _walkdata.cur.X) > distX)
            {
                _pos.X = _walkdata.next.X;
            }

            if (Math.Abs(_pos.Y - _walkdata.cur.Y) > distY)
            {
                _pos.Y = _walkdata.next.Y;
            }

            if (_pos == _walkdata.next)
            {
                _moving &= ~MoveFlags.InLeg;
                return 0;
            }
            return 1;
        }

        protected int RemapDirection(int dir, bool is_walking)
        {
            BoxFlags flags;
            bool flipX;
            bool flipY;

            // FIXME: It seems that at least in The Dig the original code does
            // check _ignoreBoxes here. However, it breaks some animations in Loom,
            // causing Bobbin to face towards the camera instead of away from it
            // in some places: After the tree has been destroyed by lightning, and
            // when entering the dark tunnels beyond the dragon's lair at the very
            // least. Possibly other places as well.
            //
            // The Dig also checks if the actor is in the current room, but that's
            // not necessary here because we never call the function unless the
            // actor is in the current room anyway.

            if (!_ignoreBoxes)
            {
                flags = _scumm.GetBoxFlags(_walkbox);

                flipX = (_walkdata.deltaXFactor > 0);
                flipY = (_walkdata.deltaYFactor > 0);

                // Check for X-Flip
                if (flags.HasFlag(BoxFlags.XFlip) || IsInClass(ObjectClass.XFlip))
                {
                    dir = 360 - dir;
                    flipX = !flipX;
                }
                // Check for Y-Flip
                if (flags.HasFlag(BoxFlags.YFlip) || IsInClass(ObjectClass.YFlip))
                {
                    dir = 180 - dir;
                    flipY = !flipY;
                }

                switch ((byte)flags & 7)
                {
                    case 1:
                        {
                            if (is_walking)	                       // Actor is walking
                                return flipX ? 90 : 270;
                            else	                               // Actor is standing/turning
                                return (dir == 90) ? 90 : 270;
                        }
                    case 2:
                        {
                            if (is_walking)	                       // Actor is walking
                                return flipY ? 180 : 0;
                            else	                               // Actor is standing/turning
                                return (dir == 0) ? 0 : 180;
                        }
                    case 3:
                        return 270;
                    case 4:
                        return 90;
                    case 5:
                        return 0;
                    case 6:
                        return 180;
                }
            }
            // OR 1024 in to signal direction interpolation should be done
            return NormalizeAngle(dir) | 1024;
        }

        protected virtual void SetupActorScale()
        {
            if (_ignoreBoxes)
                return;

            _boxscale = (ushort)_scumm.GetBoxScale(_walkbox);

            var scale = _scumm.GetScale(_walkbox, _pos.X, _pos.Y);

            _scalex = _scaley = (byte)scale;
        }

        protected void SetBox(byte box)
        {
            _walkbox = box;
            SetupActorScale();
        }

        protected int UpdateActorDirection(bool isWalking)
        {
            int from;
            bool dirType = false;
            int dir;
            bool shouldInterpolate;

            from = ToSimpleDir(dirType, _facing);
            dir = RemapDirection(_targetFacing, isWalking);

            shouldInterpolate = (dir & 1024) != 0 ? true : false;
            dir &= 1023;

            if (shouldInterpolate)
            {
                int to = ToSimpleDir(dirType, dir);
                int num = dirType ? 8 : 4;

                // Turn left or right, depending on which is shorter.
                int diff = to - from;
                if (Math.Abs(diff) > (num >> 1))
                    diff = -diff;

                if (diff > 0)
                {
                    to = from + 1;
                }
                else if (diff < 0)
                {
                    to = from - 1;
                }

                dir = FromSimpleDir(dirType, (to + num) % num);
            }

            return dir;
        }

        /// <summary>
        /// Convert a simple direction to an angle.
        /// </summary>
        /// <param name="dirType"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static int FromSimpleDir(bool dirType, int dir)
        {
            if (dirType)
                return dir * 45;
            else
                return dir * 90;
        }

        public void AdjustActorPos()
        {
            AdjustBoxResult abr;

            abr = AdjustXYToBeInBox(_pos.X, _pos.Y);

            _pos.X = abr.x;
            _pos.Y = abr.y;
            _walkdata.destbox = abr.box;

            SetBox(abr.box);

            _walkdata.dest.X = -1;

            StopActorMoving();
            _cost.soundCounter = 0;
            _cost.soundPos = 0;

            if (_walkbox != InvalidBox)
            {
                int flags = (int)_scumm.GetBoxFlags(_walkbox);
                if ((flags & 7) != 0)
                {
                    TurnToDirection(_facing);
                }
            }
        }

        public virtual AdjustBoxResult AdjustXYToBeInBox(short dstX, short dstY)
        {
            int[] thresholdTable = new int[] { 30, 80, 0 };
            AdjustBoxResult abr = new AdjustBoxResult();
            short tmpX = 0;
            short tmpY = 0;
            uint tmpDist, bestDist;
            int threshold, numBoxes;
            BoxFlags flags;
            byte bestBox;
            int box;
            int firstValidBox = 0;

            abr.x = dstX;
            abr.y = dstY;
            abr.box = InvalidBox;

            if (_ignoreBoxes)
                return abr;

            for (int tIdx = 0; tIdx < thresholdTable.Length; tIdx++)
            {
                threshold = thresholdTable[tIdx];

                numBoxes = _scumm.GetNumBoxes() - 1;
                if (numBoxes < firstValidBox)
                    return abr;

                bestDist = 0xFFFF;
                bestBox = InvalidBox;

                // We iterate (backwards) over all boxes, searching the one closest
                // to the desired coordinates.
                for (box = numBoxes; box >= firstValidBox; box--)
                {
                    flags = _scumm.GetBoxFlags((byte)box);

                    // Skip over invisible boxes
                    if (flags.HasFlag(BoxFlags.Invisible) && !(flags.HasFlag(BoxFlags.PlayerOnly) && !IsPlayer()))
                        continue;

                    // For increased performance, we perform a quick test if
                    // the coordinates can even be within a distance of 'threshold'
                    // pixels of the box.
                    if (threshold > 0 && InBoxQuickReject(_scumm.GetBoxCoordinates(box), dstX, dstY, threshold))
                        continue;

                    // Check if the point is contained in the box. If it is,
                    // we don't have to search anymore.
                    if (_scumm.CheckXYInBoxBounds(box, dstX, dstY))
                    {
                        abr.x = dstX;
                        abr.y = dstY;
                        abr.box = (byte)box;
                        return abr;
                    }

                    // Find the point in the box which is closest to our point.
                    tmpDist = GetClosestPtOnBox(_scumm.GetBoxCoordinates(box), dstX, dstY, ref tmpX, ref tmpY);

                    // Check if the box is closer than the previous boxes.
                    if (tmpDist < bestDist)
                    {
                        abr.x = tmpX;
                        abr.y = tmpY;

                        if (tmpDist == 0)
                        {
                            abr.box = (byte)box;
                            return abr;
                        }
                        bestDist = tmpDist;
                        bestBox = (byte)box;
                    }
                }

                // If the closest ('best') box we found is within the threshold, or if
                // we are on the last run (i.e. threshold == 0), return that box.
                if (threshold == 0 || threshold * threshold >= bestDist)
                {
                    abr.box = bestBox;
                    return abr;
                }
            }

            return abr;
        }

        private uint GetClosestPtOnBox(BoxCoords box, short x, short y, ref short outX, ref short outY)
        {
            Point p = new Point(x, y);
            Point tmp;
            uint dist;
            uint bestdist = 0xFFFFFF;

            tmp = ClosestPtOnLine(box.ul, box.ur, p);
            dist = p.SquareDistance(tmp);
            if (dist < bestdist)
            {
                bestdist = dist;
                outX = tmp.X;
                outY = tmp.Y;
            }

            tmp = ClosestPtOnLine(box.ur, box.lr, p);
            dist = p.SquareDistance(tmp);
            if (dist < bestdist)
            {
                bestdist = dist;
                outX = tmp.X;
                outY = tmp.Y;
            }

            tmp = ClosestPtOnLine(box.lr, box.ll, p);
            dist = p.SquareDistance(tmp);
            if (dist < bestdist)
            {
                bestdist = dist;
                outX = tmp.X;
                outY = tmp.Y;
            }

            tmp = ClosestPtOnLine(box.ll, box.ul, p);
            dist = p.SquareDistance(tmp);
            if (dist < bestdist)
            {
                bestdist = dist;
                outX = tmp.X;
                outY = tmp.Y;
            }

            return bestdist;
        }

        private Point ClosestPtOnLine(Point lineStart, Point lineEnd, Point p)
        {
            Point result;

            int lxdiff = lineEnd.X - lineStart.X;
            int lydiff = lineEnd.Y - lineStart.Y;

            if (lineEnd.X == lineStart.X)
            {	// Vertical line?
                result.X = lineStart.X;
                result.Y = p.Y;
            }
            else if (lineEnd.Y == lineStart.Y)
            {	// Horizontal line?
                result.X = p.X;
                result.Y = lineStart.Y;
            }
            else
            {
                int dist = lxdiff * lxdiff + lydiff * lydiff;
                int a, b, c;
                if (Math.Abs(lxdiff) > Math.Abs(lydiff))
                {
                    a = lineStart.X * lydiff / lxdiff;
                    b = p.X * lxdiff / lydiff;

                    c = (a + b - lineStart.Y + p.Y) * lydiff * lxdiff / dist;

                    result.X = (short)c;
                    result.Y = (short)(c * lydiff / lxdiff - a + lineStart.Y);
                }
                else
                {
                    a = lineStart.Y * lxdiff / lydiff;
                    b = p.Y * lydiff / lxdiff;

                    c = (a + b - lineStart.X + p.X) * lydiff * lxdiff / dist;

                    result.X = (short)(c * lxdiff / lydiff - a + lineStart.X);
                    result.Y = (short)c;
                }
            }

            if (Math.Abs(lydiff) < Math.Abs(lxdiff))
            {
                if (lxdiff > 0)
                {
                    if (result.X < lineStart.X)
                        result = lineStart;
                    else if (result.X > lineEnd.X)
                        result = lineEnd;
                }
                else
                {
                    if (result.X > lineStart.X)
                        result = lineStart;
                    else if (result.X < lineEnd.X)
                        result = lineEnd;
                }
            }
            else
            {
                if (lydiff > 0)
                {
                    if (result.Y < lineStart.Y)
                        result = lineStart;
                    else if (result.Y > lineEnd.Y)
                        result = lineEnd;
                }
                else
                {
                    if (result.Y > lineStart.Y)
                        result = lineStart;
                    else if (result.Y < lineEnd.Y)
                        result = lineEnd;
                }
            }

            return result;
        }

        public void SetDirection(int direction)
        {
            uint aMask;
            int i;
            ushort vald;

            // Do nothing if actor is already facing in the given direction
            if (_facing == direction)
                return;

            // Normalize the angle
            _facing = (ushort)NormalizeAngle(direction);

            // If there is no costume set for this actor, we are finished
            if (_costume == 0)
                return;

            // Update the costume for the new direction (and mark the actor for redraw)
            aMask = 0x8000;
            for (i = 0; i < 16; i++, aMask >>= 1)
            {
                vald = _cost.frame[i];
                if (vald == 0xFFFF)
                    continue;
                _scumm.CostumeLoader.CostumeDecodeData(this, vald, aMask);
            }

            NeedRedraw = true;
        }

        public void FaceToObject(int obj)
        {
            int x2, y2, dir;

            if (!IsInCurrentRoom())
                return;

            if (_scumm.GetObjectOrActorXY(obj, out x2, out y2) == false)
                return;

            dir = (x2 > _pos.X) ? 90 : 270;
            TurnToDirection(dir);
        }

        public void TurnToDirection(int newdir)
        {
            if (newdir == -1 || _ignoreTurns)
                return;

            _moving = MoveFlags.Turn;
            _targetFacing = (ushort)newdir;
        }

        public virtual void WalkActor()
        {
            int new_dir, next_box;
            Point foundPath;

            if (_moving == 0)
                return;

            if ((_moving & MoveFlags.NewLeg) == 0)
            {
                if (((_moving & MoveFlags.InLeg) != 0) && ActorWalkStep() != 0)
                    return;

                if ((_moving & MoveFlags.LastLeg) != 0)
                {
                    _moving = 0;
                    SetBox(_walkdata.destbox);
                    StartAnimActor(_standFrame);
                    if (_targetFacing != _walkdata.destdir)
                        TurnToDirection(_walkdata.destdir);
                    return;
                }

                if ((_moving & MoveFlags.Turn) != 0)
                {
                    new_dir = UpdateActorDirection(false);
                    if (_facing != new_dir)
                        SetDirection(new_dir);
                    else
                        _moving = 0;
                    return;
                }

                SetBox(_walkdata.curbox);
                _moving &= MoveFlags.InLeg;
            }

            _moving &= ~MoveFlags.NewLeg;
            do
            {
                if (_walkbox == InvalidBox)
                {
                    SetBox(_walkdata.destbox);
                    _walkdata.curbox = _walkdata.destbox;
                    break;
                }

                if (_walkbox == _walkdata.destbox)
                    break;

                next_box = _scumm.GetNextBox(_walkbox, _walkdata.destbox);
                if (next_box < 0)
                {
                    _walkdata.destbox = _walkbox;
                    _moving |= MoveFlags.LastLeg;
                    return;
                }

                _walkdata.curbox = (byte)next_box;

                if (FindPathTowards(_walkbox, (byte)next_box, _walkdata.destbox, out foundPath))
                    break;

                if (CalcMovementFactor(foundPath) != 0)
                    return;

                SetBox(_walkdata.curbox);
            } while (true);

            _moving |= MoveFlags.LastLeg;
            CalcMovementFactor(_walkdata.dest);
        }

        public void DrawActorCostume(bool hitTestMode = false)
        {
            if (_costume == 0)
                return;

            if (!hitTestMode)
            {
                if (!NeedRedraw)
                    return;

                NeedRedraw = false;
            }

            SetupActorScale();

            ICostumeRenderer bcr = _scumm.CostumeRenderer;
            PrepareDrawActorCostume(bcr);

            // If the actor is partially hidden, redraw it next frame.
            if ((bcr.DrawCostume(_scumm.MainVirtScreen, this._scumm._gdi._numStrips, this) & 1) != 0)
            {
                NeedRedraw = true;
            }

            if (!hitTestMode)
            {
                // Record the vertical extent of the drawn actor
                _top = bcr.DrawTop;
                _bottom = bcr.DrawBottom;
            }
        }

        public virtual void PrepareDrawActorCostume(ICostumeRenderer bcr)
        {
            bcr.ActorID = _number;
            bcr.ActorX = _pos.X - _scumm.MainVirtScreen.XStart;
            bcr.ActorY = _pos.Y - _elevation;

            if ((_boxscale & 0x8000) != 0)
            {
                bcr.ScaleX = bcr.ScaleY = (byte)_scumm.GetScaleFromSlot((_boxscale & 0x7fff) + 1, _pos.X, _pos.Y);
            }
            else
            {
                bcr.ScaleX = _scalex;
                bcr.ScaleY = _scaley;
            }

            bcr.ShadowMode = _shadowMode;

            bcr.SetCostume(_costume, 0);
            bcr.SetPalette(_palette);
            bcr.SetFacing(this);


            if (_forceClip > 0)
                bcr.ZBuffer = (byte)_forceClip;
            else if (IsInClass(ObjectClass.NeverClip))
                bcr.ZBuffer = 0;
            else
            {
                bcr.ZBuffer = _scumm.GetBoxMask(_walkbox);
                if (bcr.ZBuffer > _scumm._gdi._numZBuffer - 1)
                    bcr.ZBuffer = (byte)(_scumm._gdi._numZBuffer - 1);
            }

            bcr.DrawTop = 0x7fffffff;
            bcr.DrawBottom = 0;
        }

        public void StartWalkActor(int destX, int destY, int dir)
        {
            AdjustBoxResult abr;

            abr.x = (short)destX;
            abr.y = (short)destY;

            if (!IsInCurrentRoom())
            {
                _pos.X = abr.x;
                _pos.Y = abr.y;
                if (!_ignoreTurns && dir != -1)
                    _facing = (ushort)dir;
                return;
            }

            if (_ignoreBoxes)
            {
                abr.box = InvalidBox;
                _walkbox = InvalidBox;
            }
            else
            {
                if (_scumm.CheckXYInBoxBounds(_walkdata.destbox, abr.x, abr.y))
                {
                    abr.box = _walkdata.destbox;
                }
                else
                {
                    abr = AdjustXYToBeInBox(abr.x, abr.y);
                }
                if (_moving != 0 && _walkdata.destdir == dir && _walkdata.dest.X == abr.x && _walkdata.dest.Y == abr.y)
                    return;
            }

            if (_pos.X == abr.x && _pos.Y == abr.y)
            {
                if (dir != _facing)
                    TurnToDirection(dir);
                return;
            }

            _walkdata.dest.X = abr.x;
            _walkdata.dest.Y = abr.y;
            _walkdata.destbox = abr.box;
            _walkdata.destdir = (short)dir;
            _moving = (_moving & MoveFlags.InLeg) | MoveFlags.NewLeg;
            _walkdata.point3.X = 32000;

            _walkdata.curbox = _walkbox;
        }

        public void StopActorMoving()
        {
            if (_walkScript != 0)
                _scumm.StopScript(_walkScript);

            _moving = 0;
        }

        protected void StartWalkAnim(int cmd, int angle)
        {
            if (angle == -1)
                angle = _facing;

            /* Note: walk scripts aren't required to make the Dig
             * work as usual
             */
            if (_walkScript != 0)
            {
                int[] args = new int[16];

                args[0] = _number;
                args[1] = cmd;
                args[2] = angle;
                _scumm.RunScript((byte)_walkScript, true, false, args);
            }
            else
            {
                switch (cmd)
                {
                    case 1:										/* start walk */
                        SetDirection(angle);
                        StartAnimActor(_walkFrame);
                        break;
                    case 2:										/* change dir only */
                        SetDirection(angle);
                        break;
                    case 3:										/* stop walk */
                        TurnToDirection(angle);
                        StartAnimActor(_standFrame);
                        break;
                }
            }
        }

        public void RunActorTalkScript(int f)
        {
            if (_scumm.GetTalkingActor() == 0 || _room != _scumm.CurrentRoom || _frame == f)
                return;

            if (_talkScript != 0)
            {
                int script = _talkScript;
                _scumm.RunScript((byte)script, true, false, new int[] { f, _number });
            }
            else
            {
                StartAnimActor((byte)f);
            }
        }

        public void StartAnimActor(byte frame)
        {
            switch (frame)
            {
                case 0x38:
                    frame = _initFrame;
                    break;
                case 0x39:
                    frame = _walkFrame;
                    break;
                case 0x3A:
                    frame = _standFrame;
                    break;
                case 0x3B:
                    frame = _talkStartFrame;
                    break;
                case 0x3C:
                    frame = _talkStopFrame;
                    break;
            }

            if (IsInCurrentRoom() && _costume != 0)
            {
                _animProgress = 0;
                NeedRedraw = true;
                _cost.animCounter = 0;
                // V1 - V2 games don't seem to need a _cost.reset() at this point.
                // Causes Zak to lose his body in several scenes, see bug #771508
                if (frame == _initFrame)
                {
                    _cost.Reset();
                }
                _scumm.CostumeLoader.CostumeDecodeData(this, frame, uint.MaxValue);
                _frame = frame;
            }
        }

        private static bool InBoxQuickReject(BoxCoords box, int x, int y, int threshold)
        {
            int t;

            t = x - threshold;
            if (t > box.ul.X && t > box.ur.X && t > box.lr.X && t > box.ll.X)
                return true;

            t = x + threshold;
            if (t < box.ul.X && t < box.ur.X && t < box.lr.X && t < box.ll.X)
                return true;

            t = y - threshold;
            if (t > box.ul.Y && t > box.ur.Y && t > box.lr.Y && t > box.ll.Y)
                return true;

            t = y + threshold;
            if (t < box.ul.Y && t < box.ur.Y && t < box.lr.Y && t < box.ll.Y)
                return true;

            return false;
        }

        private ScummEngine _scumm;

        public bool IsInCurrentRoom()
        {
            return _room == _scumm.CurrentRoom;
        }

        public Point GetPos()
        {
            Point p = new Point(_pos);
            return p;
        }

        public Point GetRealPos()
        {
            return _pos;
        }

        public int GetRoom()
        {
            return _room;
        }

        public int GetFacing()
        {
            return _facing;
        }

        public void SetFacing(ushort newFacing)
        {
            _facing = newFacing;
        }

        public void SetAnimSpeed(byte newAnimSpeed)
        {
            _animSpeed = newAnimSpeed;
            _animProgress = 0;
        }

        public int GetAnimSpeed()
        {
            return _animSpeed;
        }

        public int GetAnimProgress()
        {
            return _animProgress;
        }

        public int GetElevation()
        {
            return _elevation;
        }

        public void SetElevation(int newElevation)
        {
            if (_elevation != newElevation)
            {
                _elevation = newElevation;
                NeedRedraw = true;
            }
        }

        public void SetPalette(int idx, ushort val)
        {
            _palette[idx] = val;
            NeedRedraw = true;
        }

        public void SetScale(int sx, int sy)
        {
            if (sx != -1)
                _scalex = (byte)sx;
            if (sy != -1)
                _scaley = (byte)sy;
            NeedRedraw = true;
        }

        public bool IsInClass(ObjectClass cls)
        {
            return _scumm.GetClass(_number, cls);
        }

        protected virtual bool IsPlayer()
        {
            return IsInClass(ObjectClass.Player);
        }

        private static void SWAP<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }

        protected bool FindPathTowards(byte box1nr, byte box2nr, byte box3nr, out Point foundPath)
        {
            foundPath = new Point();
            BoxCoords box1 = _scumm.GetBoxCoordinates(box1nr);
            BoxCoords box2 = _scumm.GetBoxCoordinates(box2nr);
            Point tmp;
            int i, j;
            int flag;
            int q, pos;

            for (i = 0; i < 4; i++)
            {
                for (j = 0; j < 4; j++)
                {
                    if (box1.ul.X == box1.ur.X && box1.ul.X == box2.ul.X && box1.ul.X == box2.ur.X)
                    {
                        flag = 0;
                        if (box1.ul.Y > box1.ur.Y)
                        {
                            SWAP(ref box1.ul.Y, ref box1.ur.Y);
                            flag |= 1;
                        }

                        if (box2.ul.Y > box2.ur.Y)
                        {
                            SWAP(ref box2.ul.Y, ref box2.ur.Y);
                            flag |= 2;
                        }

                        if (box1.ul.Y > box2.ur.Y || box2.ul.Y > box1.ur.Y ||
                                ((box1.ur.Y == box2.ul.Y || box2.ur.Y == box1.ul.Y) &&
                                box1.ul.Y != box1.ur.Y && box2.ul.Y != box2.ur.Y))
                        {
                            if ((flag & 1) != 0)
                                SWAP(ref box1.ul.Y, ref box1.ur.Y);
                            if ((flag & 2) != 0)
                                SWAP(ref box2.ul.Y, ref box2.ur.Y);
                        }
                        else
                        {
                            pos = _pos.Y;
                            if (box2nr == box3nr)
                            {
                                int diffX = _walkdata.dest.X - _pos.X;
                                int diffY = _walkdata.dest.Y - _pos.Y;
                                int boxDiffX = box1.ul.X - _pos.X;

                                if (diffX != 0)
                                {
                                    int t;

                                    diffY *= boxDiffX;
                                    t = diffY / diffX;
                                    if (t == 0 && (diffY <= 0 || diffX <= 0)
                                            && (diffY >= 0 || diffX >= 0))
                                        t = -1;
                                    pos = _pos.Y + t;
                                }
                            }

                            q = pos;
                            if (q < box2.ul.Y)
                                q = box2.ul.Y;
                            if (q > box2.ur.Y)
                                q = box2.ur.Y;
                            if (q < box1.ul.Y)
                                q = box1.ul.Y;
                            if (q > box1.ur.Y)
                                q = box1.ur.Y;
                            if (q == pos && box2nr == box3nr)
                                return true;
                            foundPath.Y = (short)q;
                            foundPath.X = box1.ul.X;
                            return false;
                        }
                    }

                    if (box1.ul.Y == box1.ur.Y && box1.ul.Y == box2.ul.Y && box1.ul.Y == box2.ur.Y)
                    {
                        flag = 0;
                        if (box1.ul.X > box1.ur.X)
                        {
                            SWAP(ref box1.ul.X, ref box1.ur.X);
                            flag |= 1;
                        }

                        if (box2.ul.X > box2.ur.X)
                        {
                            SWAP(ref box2.ul.X, ref box2.ur.X);
                            flag |= 2;
                        }

                        if (box1.ul.X > box2.ur.X || box2.ul.X > box1.ur.X ||
                                ((box1.ur.X == box2.ul.X || box2.ur.X == box1.ul.X) &&
                                box1.ul.X != box1.ur.X && box2.ul.X != box2.ur.X))
                        {
                            if ((flag & 1) != 0)
                                SWAP(ref box1.ul.X, ref box1.ur.X);
                            if ((flag & 2) != 0)
                                SWAP(ref box2.ul.X, ref box2.ur.X);
                        }
                        else
                        {

                            if (box2nr == box3nr)
                            {
                                int diffX = _walkdata.dest.X - _pos.X;
                                int diffY = _walkdata.dest.Y - _pos.Y;
                                int boxDiffY = box1.ul.Y - _pos.Y;

                                pos = _pos.X;
                                if (diffY != 0)
                                {
                                    pos += diffX * boxDiffY / diffY;
                                }
                            }
                            else
                            {
                                pos = _pos.X;
                            }

                            q = pos;
                            if (q < box2.ul.X)
                                q = box2.ul.X;
                            if (q > box2.ur.X)
                                q = box2.ur.X;
                            if (q < box1.ul.X)
                                q = box1.ul.X;
                            if (q > box1.ur.X)
                                q = box1.ur.X;
                            if (q == pos && box2nr == box3nr)
                                return true;
                            foundPath.X = (short)q;
                            foundPath.Y = box1.ul.Y;
                            return false;
                        }
                    }
                    tmp = box1.ul;
                    box1.ul = box1.ur;
                    box1.ur = box1.lr;
                    box1.lr = box1.ll;
                    box1.ll = tmp;
                }
                tmp = box2.ul;
                box2.ul = box2.ur;
                box2.ur = box2.lr;
                box2.lr = box2.ll;
                box2.ll = tmp;
            }
            return false;
        }

        public void AnimateActor(int anim)
        {
            int cmd, dir;
            cmd = anim / 4;
            dir = OldDirToNewDir(anim % 4);

            // Convert into old cmd code
            cmd = 0x3F - cmd + 2;

            switch (cmd)
            {
                case 2:				// stop walking
                    StartAnimActor(_standFrame);
                    StopActorMoving();
                    break;
                case 3:				// change direction immediatly
                    _moving &= ~MoveFlags.Turn;
                    SetDirection(dir);
                    break;
                case 4:				// turn to new direction
                    TurnToDirection(dir);
                    break;
                case 64:
                default:
                    StartAnimActor((byte)anim);
                    break;
            }
        }

        public void AnimateCostume()
        {
            if (_costume == 0)
                return;

            _animProgress++;
            if (_animProgress >= _animSpeed)
            {
                _animProgress = 0;

                _scumm.CostumeLoader.LoadCostume(_costume);
                if (_scumm.CostumeLoader.IncreaseAnims(this) != 0)
                {
                    NeedRedraw = true;
                }
            }
        }

        private static int OldDirToNewDir(int dir)
        {
            if (dir < 0 && dir > 3) throw new ArgumentOutOfRangeException("dir", dir, "Invalid direction");
            int[] new_dir_table = new int[4] { 270, 90, 180, 0 };
            return new_dir_table[dir];
        }

        public void ClassChanged(ObjectClass cls, bool value)
        {
            if (cls == ObjectClass.AlwaysClip)
                _forceClip = value ? 1 : 0;
            if (cls == ObjectClass.IgnoreBoxes)
                _ignoreBoxes = value;
        }

        public void Load(System.IO.BinaryReader reader, uint version)
        {
            short heOffsX, heOffsY;
            ushort[] sound;
            byte drawToBackBuf;
            byte heSkipLimbs;
            byte mask;
            uint heCondMask, hePaletteNum, heXmapNum;
            int layer;
            ushort[] heJumpOffsetTable, heJumpCountTable;
            uint[] heCondMaskTable;

            var actorEntries = new[]{
                    LoadAndSaveEntry.Create(()=> _pos.X = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _pos.Y = reader.ReadInt16(),8),

                    LoadAndSaveEntry.Create(()=> heOffsX = reader.ReadInt16(),32),
                    LoadAndSaveEntry.Create(()=> heOffsY = reader.ReadInt16(),32),
                    LoadAndSaveEntry.Create(()=> _top = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _bottom = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _elevation = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _width = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> _facing = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> _costume = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> _room = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _talkColor = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _talkFrequency = reader.ReadInt16(),16),
                    LoadAndSaveEntry.Create(()=> _talkPan = (byte)reader.ReadInt16(),24),
                    LoadAndSaveEntry.Create(()=> _talkVolume = (byte)reader.ReadInt16(),29),
                    LoadAndSaveEntry.Create(()=> _boxscale = reader.ReadUInt16(),34),
                    LoadAndSaveEntry.Create(()=> _scalex = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _scaley = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _charset = reader.ReadByte(),8),
		            
                    // Actor sound grew from 8 to 32 bytes and switched to uint16 in HE games
                    LoadAndSaveEntry.Create(()=> sound = reader.ReadBytes(8).Cast<ushort>().ToArray(),8,36),
                    LoadAndSaveEntry.Create(()=> sound = reader.ReadBytes(32).Cast<ushort>().ToArray(),37,61),
                    LoadAndSaveEntry.Create(()=> sound = reader.ReadUInt16s(32),62),
                    
                    // Actor animVariable grew from 8 to 27
                    LoadAndSaveEntry.Create(()=> _animVariable = reader.ReadInt16s(8),8,40),
                    LoadAndSaveEntry.Create(()=> _animVariable = reader.ReadInt16s(27),41),

                    LoadAndSaveEntry.Create(()=> _targetFacing = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> _moving = (MoveFlags)reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _ignoreBoxes = reader.ReadByte()!=0,8),
                    LoadAndSaveEntry.Create(()=> _forceClip = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _initFrame = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _walkFrame = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _standFrame = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _talkStartFrame = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _talkStopFrame = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _speedx = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> _speedy = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> _cost.animCounter = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> _cost.soundCounter = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> drawToBackBuf = reader.ReadByte(),32),
                    LoadAndSaveEntry.Create(()=> _flip = reader.ReadByte()!=0,32),
                    LoadAndSaveEntry.Create(()=> heSkipLimbs = reader.ReadByte(),32),

		            // Actor palette grew from 64 to 256 bytes and switched to uint16 in HE games
                    LoadAndSaveEntry.Create(()=> _palette = reader.ReadBytes(64).Cast<ushort>().ToArray(),8,9),
                    LoadAndSaveEntry.Create(()=> _palette = reader.ReadBytes(256).Cast<ushort>().ToArray(),10,79),
                    LoadAndSaveEntry.Create(()=> _palette = reader.ReadUInt16s(256),80),

                    LoadAndSaveEntry.Create(()=> mask = reader.ReadByte(),8,9),
                    LoadAndSaveEntry.Create(()=> _shadowMode = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _visible = reader.ReadByte()!=0,8),
                    LoadAndSaveEntry.Create(()=> _frame = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _animSpeed = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _animProgress = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _walkbox = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _needRedraw = reader.ReadByte()!=0,8),
                    LoadAndSaveEntry.Create(()=> _needBgReset = reader.ReadByte()!=0,8),
                    LoadAndSaveEntry.Create(()=> _costumeNeedsInit = reader.ReadByte()!=0,8),
                    LoadAndSaveEntry.Create(()=> heCondMask = reader.ReadUInt32(),38),
                    LoadAndSaveEntry.Create(()=> hePaletteNum = reader.ReadUInt32(),59),
                    LoadAndSaveEntry.Create(()=> heXmapNum = reader.ReadUInt32(),59),

                    LoadAndSaveEntry.Create(()=> _talkPosX = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _talkPosY = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _ignoreTurns = reader.ReadByte()!=0,8),

                    // Actor layer switched to int32 in HE games
                    LoadAndSaveEntry.Create(()=> layer = reader.ReadByte(),8,57),
                    LoadAndSaveEntry.Create(()=> layer = reader.ReadInt32(),58),

                    LoadAndSaveEntry.Create(()=> _talkScript = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> _walkScript = reader.ReadUInt16(),8),

                    LoadAndSaveEntry.Create(()=> _walkdata.dest.X = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.dest.Y = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.destbox = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.destdir = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.curbox = reader.ReadByte(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.cur.X = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.cur.Y = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.next.X = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.next.Y = reader.ReadInt16(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.deltaXFactor = reader.ReadInt32(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.deltaYFactor = reader.ReadInt32(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.xfrac = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> _walkdata.yfrac = reader.ReadUInt16(),8),

                    LoadAndSaveEntry.Create(()=> _walkdata.point3.X = reader.ReadInt16(),42),
                    LoadAndSaveEntry.Create(()=> _walkdata.point3.Y = reader.ReadInt16(),42),

                    LoadAndSaveEntry.Create(()=> _cost.active = reader.ReadBytes(16),8),
                    LoadAndSaveEntry.Create(()=> _cost.stopped = reader.ReadUInt16(),8),
                    LoadAndSaveEntry.Create(()=> _cost.curpos = reader.ReadUInt16s(16),8),
                    LoadAndSaveEntry.Create(()=> _cost.start = reader.ReadUInt16s(16),8),
                    LoadAndSaveEntry.Create(()=> _cost.end = reader.ReadUInt16s(16),8),
                    LoadAndSaveEntry.Create(()=> _cost.frame = reader.ReadUInt16s(16),8),

                    LoadAndSaveEntry.Create(()=> heJumpOffsetTable = reader.ReadUInt16s(16),65),
                    LoadAndSaveEntry.Create(()=> heJumpCountTable = reader.ReadUInt16s(16),65),
                    LoadAndSaveEntry.Create(()=> heCondMaskTable = reader.ReadUInt32s(16),65),
            };

            // Not all actor data is saved; so when loading, we first reset
            // the actor, to ensure completely reproducible behavior (else,
            // some not saved value in the actor class can cause odd things)
            InitActor(-1);

            Array.ForEach(actorEntries, e => e.Execute(version));
        }
    }
}
