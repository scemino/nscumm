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

        public void reset()
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

        public bool _needRedraw, _needBgReset, _visible;
        public byte _shadowMode;
        public bool _flip;
        public byte _frame;
        public byte _walkbox;
        public short _talkPosX, _talkPosY;
        public ushort _talkScript, _walkScript;
        public bool _ignoreTurns;
        public bool _drawToBackBuf;
        public ushort[] _sound = new ushort[32];
        public CostumeData _cost;

        /* HE specific */
        public int _heOffsX, _heOffsY;
        public bool _heSkipLimbs;
        public uint _heCondMask;
        public uint _hePaletteNum;
        public uint _heXmapNum;

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

        public Actor(ScummInterpreter scumm, byte id)
        {
            _scumm = scumm;
            _number = id;
        }

        //protected:
        public virtual void HideActor()
        {
            if (!_visible)
                return;

            if (_moving != 0)
            {
                StopActorMoving();
                StartAnimActor(_standFrame);
            }

            _visible = false;
            _cost.soundCounter = 0;
            _cost.soundPos = 0;
            _needRedraw = false;
            _needBgReset = true;
        }

        public void ShowActor()
        {
            if (_scumm.CurrentRoom == 0 || _visible)
                return;

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
            _needRedraw = true;
        }

        public virtual void InitActor(int mode)
        {
            if (mode == -1)
            {
                _top = _bottom = 0;
                _needRedraw = false;
                _needBgReset = false;
                _costumeNeedsInit = false;
                _visible = false;
                _flip = false;
                _speedx = 8;
                _speedy = 2;
                _frame = 0;
                _walkbox = 0;
                _animProgress = 0;
                _drawToBackBuf = false;
                _animVariable = new short[27];
                _palette = new ushort[256];
                _sound = new ushort[32];
                _cost = new CostumeData();
                _walkdata = new ActorWalkData();
                _walkdata.point3.x = 32000;
                _walkScript = 0;
            }

            if (mode == 1 || mode == -1)
            {
                _costume = 0;
                _room = 0;
                _pos.x = 0;
                _pos.y = 0;
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
            PutActor(_pos.x, _pos.y, _room);
        }

        public void PutActor(byte room)
        {
            PutActor(_pos.x, _pos.y, room);
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

            _pos.x = dstX;
            _pos.y = dstY;
            _room = newRoom;
            _needRedraw = true;

            if (_scumm.Variables[ScummInterpreter.VariableEgo] == _number)
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
                _cost.reset();
                _costume = c;
                ShowActor();
            }
            else
            {
                _costume = c;
                _cost.reset();
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

            diffX = next.x - _pos.x;
            diffY = next.y - _pos.y;
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

            if ((uint)Math.Abs((int)(deltaXFactor >> 16)) > _speedx)
            {
                deltaXFactor = (int)_speedx << 16;
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

            _needRedraw = true;

            nextFacing = UpdateActorDirection(true);
            if (!_moving.HasFlag(MoveFlags.InLeg) || _facing != nextFacing)
            {
                if (_walkFrame != _frame || _facing != nextFacing)
                {
                    StartWalkAnim(1, nextFacing);
                }
                _moving |= MoveFlags.InLeg;
            }

            if (_walkbox != _walkdata.curbox && _scumm.CheckXYInBoxBounds(_walkdata.curbox, _pos.x, _pos.y))
            {
                SetBox(_walkdata.curbox);
            }

            distX = Math.Abs(_walkdata.next.x - _walkdata.cur.x);
            distY = Math.Abs(_walkdata.next.y - _walkdata.cur.y);

            if (Math.Abs(_pos.x - _walkdata.cur.x) >= distX && Math.Abs(_pos.y - _walkdata.cur.y) >= distY)
            {
                _moving &= ~MoveFlags.InLeg;
                return 0;
            }

            tmpX = (_pos.x << 16) + _walkdata.xfrac + (_walkdata.deltaXFactor >> 8) * _scalex;
            _walkdata.xfrac = (ushort)tmpX;
            _pos.x = (short)(tmpX >> 16);

            tmpY = (_pos.y << 16) + _walkdata.yfrac + (_walkdata.deltaYFactor >> 8) * _scaley;
            _walkdata.yfrac = (ushort)tmpY;
            _pos.y = (short)(tmpY >> 16);

            if (Math.Abs(_pos.x - _walkdata.cur.x) > distX)
            {
                _pos.x = _walkdata.next.x;
            }

            if (Math.Abs(_pos.y - _walkdata.cur.y) > distY)
            {
                _pos.y = _walkdata.next.y;
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

            // TODO
            //_boxscale = (ushort)_scumm.GetBoxScale(_walkbox);

            //var scale = _scumm.GetScale(_walkbox, _pos.x, _pos.y);

            //_scalex = _scaley = (byte)scale;
        }

        protected void SetBox(byte box)
        {
            _walkbox = box;
            SetupActorScale();
        }

        protected int UpdateActorDirection(bool is_walking)
        {
            int from;
            bool dirType = false;
            int dir;
            bool shouldInterpolate;

            from = ToSimpleDir(dirType, _facing);
            dir = RemapDirection(_targetFacing, is_walking);

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

            abr = AdjustXYToBeInBox(_pos.x, _pos.y);

            _pos.x = abr.x;
            _pos.y = abr.y;
            _walkdata.destbox = abr.box;

            SetBox(abr.box);

            _walkdata.dest.x = -1;

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
                outX = tmp.x;
                outY = tmp.y;
            }

            tmp = ClosestPtOnLine(box.ur, box.lr, p);
            dist = p.SquareDistance(tmp);
            if (dist < bestdist)
            {
                bestdist = dist;
                outX = tmp.x;
                outY = tmp.y;
            }

            tmp = ClosestPtOnLine(box.lr, box.ll, p);
            dist = p.SquareDistance(tmp);
            if (dist < bestdist)
            {
                bestdist = dist;
                outX = tmp.x;
                outY = tmp.y;
            }

            tmp = ClosestPtOnLine(box.ll, box.ul, p);
            dist = p.SquareDistance(tmp);
            if (dist < bestdist)
            {
                bestdist = dist;
                outX = tmp.x;
                outY = tmp.y;
            }

            return bestdist;
        }

        private Point ClosestPtOnLine(Point lineStart, Point lineEnd, Point p)
        {
            Point result;

            int lxdiff = lineEnd.x - lineStart.x;
            int lydiff = lineEnd.y - lineStart.y;

            if (lineEnd.x == lineStart.x)
            {	// Vertical line?
                result.x = lineStart.x;
                result.y = p.y;
            }
            else if (lineEnd.y == lineStart.y)
            {	// Horizontal line?
                result.x = p.x;
                result.y = lineStart.y;
            }
            else
            {
                int dist = lxdiff * lxdiff + lydiff * lydiff;
                int a, b, c;
                if (Math.Abs(lxdiff) > Math.Abs(lydiff))
                {
                    a = lineStart.x * lydiff / lxdiff;
                    b = p.x * lxdiff / lydiff;

                    c = (a + b - lineStart.y + p.y) * lydiff * lxdiff / dist;

                    result.x = (short)c;
                    result.y = (short)(c * lydiff / lxdiff - a + lineStart.y);
                }
                else
                {
                    a = lineStart.y * lxdiff / lydiff;
                    b = p.y * lydiff / lxdiff;

                    c = (a + b - lineStart.x + p.x) * lydiff * lxdiff / dist;

                    result.x = (short)(c * lxdiff / lydiff - a + lineStart.x);
                    result.y = (short)c;
                }
            }

            if (Math.Abs(lydiff) < Math.Abs(lxdiff))
            {
                if (lxdiff > 0)
                {
                    if (result.x < lineStart.x)
                        result = lineStart;
                    else if (result.x > lineEnd.x)
                        result = lineEnd;
                }
                else
                {
                    if (result.x > lineStart.x)
                        result = lineStart;
                    else if (result.x < lineEnd.x)
                        result = lineEnd;
                }
            }
            else
            {
                if (lydiff > 0)
                {
                    if (result.y < lineStart.y)
                        result = lineStart;
                    else if (result.y > lineEnd.y)
                        result = lineEnd;
                }
                else
                {
                    if (result.y > lineStart.y)
                        result = lineStart;
                    else if (result.y < lineEnd.y)
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
                // TODO
                //_vm->_costumeLoader->costumeDecodeData(this, vald, (_vm->_game.version <= 2) ? 0xFFFF : aMask);
            }

            _needRedraw = true;
        }

        public void FaceToObject(int obj)
        {
            int x2, y2, dir;

            if (!IsInCurrentRoom())
                return;

            if (_scumm.GetObjectOrActorXY(obj, out x2, out y2) == false)
                return;

            dir = (x2 > _pos.x) ? 90 : 270;
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

        //public void DrawActorCostume(bool hitTestMode = false)
        //{
        //    if (_costume == 0)
        //        return;

        //    if (!hitTestMode)
        //    {
        //        if (!_needRedraw)
        //            return;

        //        _needRedraw = false;
        //    }

        //    SetupActorScale();

        //    //BaseCostumeRenderer* bcr = _vm->_costumeRenderer;
        //    //prepareDrawActorCostume(bcr);

        //    //// If the actor is partially hidden, redraw it next frame.
        //    //if (bcr->drawCostume(_vm->_virtscr[kMainVirtScreen], _vm->_gdi->_numStrips, this, _drawToBackBuf) & 1)
        //    //{
        //    //    _needRedraw = (_vm->_game.version <= 6);
        //    //}

        //    //if (!hitTestMode)
        //    //{
        //    //    // Record the vertical extent of the drawn actor
        //    //    _top = bcr->_draw_top;
        //    //    _bottom = bcr->_draw_bottom;
        //    //}
        //}
        //public virtual void prepareDrawActorCostume(BaseCostumeRenderer bcr);
        //public void animateCostume();
        //public virtual void setActorCostume(int c);

        //public void animateLimb(int limb, int f);

        //public bool actorHitTest(int x, int y);

        //public string getActorName();
        public void StartWalkActor(int destX, int destY, int dir)
        {
            AdjustBoxResult abr;

            abr.x = (short)destX;
            abr.y = (short)destY;

            if (!IsInCurrentRoom())
            {
                _pos.x = abr.x;
                _pos.y = abr.y;
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
                if (_moving != 0 && _walkdata.destdir == dir && _walkdata.dest.x == abr.x && _walkdata.dest.y == abr.y)
                    return;
            }

            if (_pos.x == abr.x && _pos.y == abr.y)
            {
                if (dir != _facing)
                    TurnToDirection(dir);
                return;
            }

            _walkdata.dest.x = abr.x;
            _walkdata.dest.y = abr.y;
            _walkdata.destbox = abr.box;
            _walkdata.destdir = (short)dir;
            _moving = (_moving & MoveFlags.InLeg) | MoveFlags.NewLeg;
            _walkdata.point3.x = 32000;

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
                _needRedraw = true;
                _cost.animCounter = 0;
                // V1 - V2 games don't seem to need a _cost.reset() at this point.
                // Causes Zak to lose his body in several scenes, see bug #771508
                if (frame == _initFrame)
                {
                    _cost.reset();
                }
                // TODO
                //_vm->_costumeLoader->costumeDecodeData(this, frame, (uint)-1);
                _frame = frame;
            }
        }

        private static bool InBoxQuickReject(BoxCoords box, int x, int y, int threshold)
        {
            int t;

            t = x - threshold;
            if (t > box.ul.x && t > box.ur.x && t > box.lr.x && t > box.ll.x)
                return true;

            t = x + threshold;
            if (t < box.ul.x && t < box.ur.x && t < box.lr.x && t < box.ll.x)
                return true;

            t = y - threshold;
            if (t > box.ul.y && t > box.ur.y && t > box.lr.y && t > box.ll.y)
                return true;

            t = y + threshold;
            if (t < box.ul.y && t < box.ur.y && t < box.lr.y && t < box.ll.y)
                return true;

            return false;
        }

        //public void remapActorPalette(int r_fact, int g_fact, int b_fact, int threshold);
        //public void remapActorPaletteColor(int slot, int color);

        //public void animateActor(int anim);

        private ScummInterpreter _scumm;
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

        //public int GetAnimVar(byte var);
        //public void SetAnimVar(byte var, int value);

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
                _needRedraw = true;
            }
        }

        public void SetPalette(int idx, ushort val)
        {
            _palette[idx] = val;
            _needRedraw = true;
        }

        public void SetScale(byte sx, byte sy)
        {
            if (sx != 255)
                _scalex = sx;
            if (sy != 255)
                _scaley = sy;
            _needRedraw = true;
        }

        //public void classChanged(int cls, bool value);

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
                    if (box1.ul.x == box1.ur.x && box1.ul.x == box2.ul.x && box1.ul.x == box2.ur.x)
                    {
                        flag = 0;
                        if (box1.ul.y > box1.ur.y)
                        {
                            SWAP(ref box1.ul.y, ref box1.ur.y);
                            flag |= 1;
                        }

                        if (box2.ul.y > box2.ur.y)
                        {
                            SWAP(ref box2.ul.y, ref box2.ur.y);
                            flag |= 2;
                        }

                        if (box1.ul.y > box2.ur.y || box2.ul.y > box1.ur.y ||
                                ((box1.ur.y == box2.ul.y || box2.ur.y == box1.ul.y) &&
                                box1.ul.y != box1.ur.y && box2.ul.y != box2.ur.y))
                        {
                            if ((flag & 1) != 0)
                                SWAP(ref box1.ul.y, ref box1.ur.y);
                            if ((flag & 2) != 0)
                                SWAP(ref box2.ul.y, ref box2.ur.y);
                        }
                        else
                        {
                            pos = _pos.y;
                            if (box2nr == box3nr)
                            {
                                int diffX = _walkdata.dest.x - _pos.x;
                                int diffY = _walkdata.dest.y - _pos.y;
                                int boxDiffX = box1.ul.x - _pos.x;

                                if (diffX != 0)
                                {
                                    int t;

                                    diffY *= boxDiffX;
                                    t = diffY / diffX;
                                    if (t == 0 && (diffY <= 0 || diffX <= 0)
                                            && (diffY >= 0 || diffX >= 0))
                                        t = -1;
                                    pos = _pos.y + t;
                                }
                            }

                            q = pos;
                            if (q < box2.ul.y)
                                q = box2.ul.y;
                            if (q > box2.ur.y)
                                q = box2.ur.y;
                            if (q < box1.ul.y)
                                q = box1.ul.y;
                            if (q > box1.ur.y)
                                q = box1.ur.y;
                            if (q == pos && box2nr == box3nr)
                                return true;
                            foundPath.y = (short)q;
                            foundPath.x = box1.ul.x;
                            return false;
                        }
                    }

                    if (box1.ul.y == box1.ur.y && box1.ul.y == box2.ul.y && box1.ul.y == box2.ur.y)
                    {
                        flag = 0;
                        if (box1.ul.x > box1.ur.x)
                        {
                            SWAP(ref box1.ul.x, ref box1.ur.x);
                            flag |= 1;
                        }

                        if (box2.ul.x > box2.ur.x)
                        {
                            SWAP(ref box2.ul.x, ref box2.ur.x);
                            flag |= 2;
                        }

                        if (box1.ul.x > box2.ur.x || box2.ul.x > box1.ur.x ||
                                ((box1.ur.x == box2.ul.x || box2.ur.x == box1.ul.x) &&
                                box1.ul.x != box1.ur.x && box2.ul.x != box2.ur.x))
                        {
                            if ((flag & 1) != 0)
                                SWAP(ref box1.ul.x, ref box1.ur.x);
                            if ((flag & 2) != 0)
                                SWAP(ref box2.ul.x, ref box2.ur.x);
                        }
                        else
                        {

                            if (box2nr == box3nr)
                            {
                                int diffX = _walkdata.dest.x - _pos.x;
                                int diffY = _walkdata.dest.y - _pos.y;
                                int boxDiffY = box1.ul.y - _pos.y;

                                pos = _pos.x;
                                if (diffY != 0)
                                {
                                    pos += diffX * boxDiffY / diffY;
                                }
                            }
                            else
                            {
                                pos = _pos.x;
                            }

                            q = pos;
                            if (q < box2.ul.x)
                                q = box2.ul.x;
                            if (q > box2.ur.x)
                                q = box2.ur.x;
                            if (q < box1.ul.x)
                                q = box1.ul.x;
                            if (q > box1.ur.x)
                                q = box1.ur.x;
                            if (q == pos && box2nr == box3nr)
                                return true;
                            foundPath.x = (short)q;
                            foundPath.y = box1.ul.y;
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
            dir = oldDirToNewDir(anim % 4);

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

        private static int oldDirToNewDir(int dir)
        {
            if (dir < 0 && dir > 3) throw new ArgumentOutOfRangeException("dir", dir, "Invalid direction");
            int[] new_dir_table = new int[4] { 270, 90, 180, 0 };
            return new_dir_table[dir];
        }

        public void ClassChanged(ObjectClass cls, bool value)
        {
            if (cls == ObjectClass.AlwaysClip)
                _forceClip = 0;
            if (cls == ObjectClass.IgnoreBoxes)
                _ignoreBoxes = value;
        }
    }
}
