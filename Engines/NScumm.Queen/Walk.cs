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

using System;
using D = NScumm.Core.DebugHelper;

namespace NScumm.Queen
{
    struct WalkData
    {
        public short dx, dy;
        public Area area;
        public ushort areaNum;
        public MovePersonAnim anim;
    }

    struct MovePersonData
    {
        public string name;
        public short walkLeft1, walkLeft2;
        public short walkRight1, walkRight2;
        public short walkBack1, walkBack2;
        public short walkFront1, walkFront2;
        public ushort frontStandingFrame;
        public ushort backStandingFrame;
        public ushort animSpeed;
        public ushort moveSpeed;

        public MovePersonData(string name,
          short walkLeft1, short walkLeft2,
          short walkRight1, short walkRight2,
          short walkBack1, short walkBack2,
          short walkFront1, short walkFront2,
          ushort frontStandingFrame, ushort backStandingFrame,
          ushort animSpeed, ushort moveSpeed)
        {
            this.name = name;
            this.walkLeft1 = walkLeft1;
            this.walkLeft2 = walkLeft2;
            this.walkRight1 = walkRight1;
            this.walkRight2 = walkRight2;
            this.walkBack1 = walkBack1;
            this.walkBack2 = walkBack2;
            this.walkFront1 = walkFront1;
            this.walkFront2 = walkFront2;
            this.frontStandingFrame = frontStandingFrame;
            this.backStandingFrame = backStandingFrame;
            this.animSpeed = animSpeed;
            this.moveSpeed = moveSpeed;
        }
    }


    struct MovePersonAnim
    {
        public short firstFrame;
        public short lastFrame;
        public Direction facing;

        public void Set(short ff, short lf, Direction dir)
        {
            firstFrame = ff;
            lastFrame = lf;
            facing = dir;
        }
    }

    public class Walk
    {
        const int MAX_WALK_DATA = 16;

        QueenEngine _vm;

        /// <summary>
        /// Set if stopJoe() is called.
        /// </summary>
        bool _joeInterrupted;

        /// <summary>
        /// Set if handleSpecialArea() is called.
        /// </summary>
        bool _joeMoveBlock;

        Area[] _roomArea;

        /// <summary>
        /// Number of areas for current room.
        /// </summary>
        ushort _roomAreaCount;

        ushort _walkDataCount;
        ushort[] _areaStrike = new ushort[MAX_WALK_DATA];
        ushort _areaStrikeCount;

        ushort[] _areaList = new ushort[MAX_WALK_DATA];
        ushort _areaListCount;

        WalkData[] _walkData = new WalkData[MAX_WALK_DATA];

        private static readonly MovePersonData[] _moveData =
        {
      new MovePersonData("COMPY", -1, -6, 1, 6, 0, 0, 0, 0, 12, 12, 1, 14),
      new MovePersonData("DEINO", -1, -8, 1, 8, 0, 0, 0, 0, 11, 11, 1, 10),
      new MovePersonData("FAYE", -1, -6, 1, 6, 13, 18, 7, 12, 19, 22, 2, 5),
      new MovePersonData("GUARDS", -1, -6, 1, 6, 0, 0, 0, 0, 7, 7, 2, 5),
      new MovePersonData("PRINCESS1", -1, -6, 1, 6, 13, 18, 7, 12, 19, 21, 2, 5),
      new MovePersonData("PRINCESS2", -1, -6, 1, 6, 13, 18, 7, 12, 19, 21, 2, 5),
      new MovePersonData("AMGUARD", -1, -6, 1, 6, 13, 18, 7, 12, 19, 21, 2, 5),
      new MovePersonData("SPARKY", -1, -6, 1, 6, 13, 18, 7, 12, 21, 20, 2, 5),
      new MovePersonData("LOLA_SHOWER", -1, -6, 55, 60, 0, 0, 0, 0, 7, 7, 2, 5),
      new MovePersonData("LOLA", -24, -29, 24, 29, 0, 0, 0, 0, 30, 30, 2, 5),
      new MovePersonData("BOB", -15, -20, 15, 20, 21, 26, 0, 0, 27, 29, 2, 5),
      new MovePersonData("CHEF", -1, -4, 1, 4, 0, 0, 0, 0, 1, 5, 2, 4),
      new MovePersonData("HENRY", -1, -6, 1, 6, 0, 0, 0, 0, 7, 7, 2, 6),
      new MovePersonData("ANDERSON", -1, -6, 1, 6, 0, 0, 0, 0, 7, 7, 2, 5),
      new MovePersonData("JASPAR", -4, -9, 4, 9, 16, 21, 10, 15, 1, 3, 1, 10),
      new MovePersonData("PYGMY", -7, -12, 7, 12, 0, 0, 0, 0, 27, 27, 2, 5),
      new MovePersonData("FRANK", 7, 12, 1, 6, 0, 0, 0, 0, 13, 13, 2, 4),
      new MovePersonData("WEDGEWOOD", -20, -25, 20, 25, 0, 0, 0, 0, 1, 1, 1, 5),
      new MovePersonData("TMPD", -1, -6, 1, 6, 13, 18, 7, 12, 19, 21, 2, 5),
      new MovePersonData("IAN", -1, -6, 1, 6, 0, 0, 0, 0, 7, 7, 2, 6),
      new MovePersonData("*", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
    };

        public Walk(QueenEngine vm)
        {
            _vm = vm;
        }

        public short MovePerson(Person pp, short endx, short endy, ushort curImage, int direction)
        {
            if (endx == 0 && endy == 0)
            {
                // TODO: warning("Walk::movePerson() - endx == 0 && endy == 0");
                return 0;
            }

            short can = 0;
            InitWalkData();

            ushort bobNum = (ushort)pp.actor.bobNum;
            ushort bankNum = pp.actor.bankNum;

            ushort oldx = (ushort)_vm.Graphics.Bobs[bobNum].x;
            ushort oldy = (ushort)_vm.Graphics.Bobs[bobNum].y;

            ushort oldPos = _vm.Grid.FindAreaForPos(GridScreen.ROOM, oldx, oldy);
            ushort newPos = _vm.Grid.FindAreaForPos(GridScreen.ROOM, (ushort)endx, (ushort)endy);

            D.Debug(9, $"Walk::movePerson({direction}, {oldx}, {oldy}, {endx}, {endy}) - old = {oldPos}, new = {newPos}");

            // find MovePersonData associated to Person
            int mpd = 0;
            while (_moveData[mpd].name[0] != '*')
            {
                if (string.Equals(_moveData[mpd].name, pp.name, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
                ++mpd;
            }

            if (Calc(oldPos, newPos, (short)oldx, (short)oldy, endx, endy))
            {
                if (_walkDataCount > 0)
                {
                    AnimatePersonPrepare(_moveData[mpd], direction);
                    AnimatePerson(_moveData[mpd], curImage, bobNum, bankNum, direction);
                }
            }
            else
            {
                can = -1;
            }

            ushort standingFrame = (ushort)(31 + bobNum);

            // make other person face the right direction
            BobSlot pbs = _vm.Graphics.Bobs[bobNum];
            pbs.endx = endx;
            pbs.endy = endy;
            pbs.animating = false;
            pbs.scale = _walkData[_walkDataCount].area.CalcScale(endy);
            if (_walkData[_walkDataCount].anim.facing == Direction.BACK)
            {
                _vm.BankMan.Unpack(_moveData[mpd].backStandingFrame, standingFrame, bankNum);
            }
            else
            {
                _vm.BankMan.Unpack(_moveData[mpd].frontStandingFrame, standingFrame, bankNum);
            }
            ushort obj = _vm.Logic.ObjectForPerson(bobNum);
            if (_walkData[_walkDataCount].dx < 0)
            {
                _vm.Logic.ObjectData[obj].image = -3;
                pbs.xflip = true;
            }
            else
            {
                _vm.Logic.ObjectData[obj].image = -4;
                pbs.xflip = false;
            }
            pbs.frameNum = standingFrame;
            return can;
        }

        void AnimatePerson(MovePersonData mpd, ushort image, ushort bobNum, ushort bankNum, int direction)
        {
            // queen.c l.2572-2651
            BobSlot pbs = _vm.Graphics.Bobs[bobNum];

            // check to see which way person should be facing
            if (mpd.walkLeft1 == mpd.walkRight1)
            {
                pbs.xflip = (direction == -3);
            }
            else
            {
                // they have special walk for left and right, so don't flip
                pbs.xflip = false;
            }

            ushort i;
            for (i = 1; i <= _walkDataCount; ++i)
            {
                WalkData pwd = _walkData[i];

                // unpack necessary frames for bob animation
                ushort dstFrame = image;
                ushort srcFrame = (ushort)Math.Abs(pwd.anim.firstFrame);
                while (srcFrame <= Math.Abs(pwd.anim.lastFrame))
                {
                    _vm.BankMan.Unpack(srcFrame, dstFrame, bankNum);
                    ++dstFrame;
                    ++srcFrame;
                }
                // pass across bobs direction ONLY if walk is a mirror flip!
                if (Math.Abs(mpd.walkLeft1) == Math.Abs(mpd.walkRight1))
                {
                    pbs.AnimNormal(image, (ushort)(dstFrame - 1), mpd.animSpeed, false, pbs.xflip);
                }
                else
                {
                    pbs.AnimNormal(image, (ushort)(dstFrame - 1), mpd.animSpeed, false, false);
                }

                // move other actors at correct speed relative to scale
                ushort moveSpeed = (ushort)(_vm.Grid.FindScale((ushort)pbs.x, (ushort)pbs.y) * mpd.moveSpeed / 100);
                pbs.Move((short)(pbs.x + pwd.dx), (short)(pbs.y + pwd.dy), (short)moveSpeed);

                // flip if one set of frames for actor
                if (mpd.walkLeft1 < 0 || Math.Abs(mpd.walkLeft1) == Math.Abs(mpd.walkRight1))
                {
                    pbs.xflip = pwd.dx < 0;
                }

                while (pbs.moving)
                {
                    _vm.Update();
                    pbs.scale = pwd.area.CalcScale(pbs.y);
                    pbs.ScaleWalkSpeed(mpd.moveSpeed);
                    if (_vm.Input.CutawayQuit)
                    {
                        StopPerson(bobNum);
                        break;
                    }
                }
            }
        }

        void StopPerson(ushort bobNum)
        {
            BobSlot pbs = _vm.Graphics.Bobs[bobNum];
            pbs.x = pbs.endx;
            pbs.y = pbs.endy;
            pbs.moving = false;
        }

        void AnimatePersonPrepare(MovePersonData mpd, int direction)
        {
            // queen.c l.2469-2572
            int i;
            for (i = 1; i <= _walkDataCount; ++i)
            {

                WalkData pwd = _walkData[i];

                if (pwd.dx < 0)
                {
                    pwd.anim.Set(mpd.walkLeft1, mpd.walkLeft2, Direction.LEFT);
                }
                else if (pwd.dx > 0)
                {
                    pwd.anim.Set(mpd.walkRight1, mpd.walkRight2, Direction.RIGHT);
                }
                else
                {
                    if (Math.Abs(mpd.walkLeft1) == Math.Abs(mpd.walkRight1))
                    {
                        pwd.anim.Set(mpd.walkRight1, mpd.walkRight2, Direction.RIGHT);
                    }
                    else
                    {
                        // we have specific moves for this actor, see what direction they were last facing
                        if (direction == -3)
                        {
                            // previously facing right
                            pwd.anim.Set(mpd.walkLeft1, mpd.walkLeft2, Direction.LEFT);
                        }
                        else
                        {
                            // previously facing left
                            pwd.anim.Set(mpd.walkRight1, mpd.walkRight2, Direction.RIGHT);
                        }
                    }
                }

                short k = Math.Abs(pwd.dy);
                short ds = pwd.area.ScaleDiff;
                if (ds > 0)
                {
                    k = (short)(k * ((k * ds) / pwd.area.box.yDiff) / 2);
                }

                if (Math.Abs(pwd.dx) < k)
                {
                    if (pwd.dy < 0)
                    {
                        if (mpd.walkBack1 > 0)
                        {
                            pwd.anim.Set(mpd.walkBack1, mpd.walkBack2, Direction.BACK);
                        }
                        else if (pwd.dx < 0)
                        {
                            pwd.anim.Set(mpd.walkLeft1, mpd.walkLeft2, Direction.BACK);
                        }
                        else
                        {
                            pwd.anim.Set(mpd.walkRight1, mpd.walkRight2, Direction.BACK);
                        }
                    }
                    else if (pwd.dy > 0)
                    {
                        if (mpd.walkFront1 > 0)
                        {
                            pwd.anim.Set(mpd.walkFront1, mpd.walkFront2, Direction.FRONT);
                        }
                        else if (Math.Abs(mpd.walkLeft1) == Math.Abs(mpd.walkRight1))
                        {
                            if (pwd.dx < 0)
                            {
                                pwd.anim.Set(mpd.walkLeft1, mpd.walkLeft2, Direction.FRONT);
                            }
                            else
                            {
                                pwd.anim.Set(mpd.walkRight1, mpd.walkRight2, Direction.FRONT);
                            }
                        }
                        else
                        {
                            // we have a special move for left/right, so select that instead!
                            if (direction == -3)
                            {
                                // previously facing right
                                pwd.anim.Set(mpd.walkLeft1, mpd.walkLeft2, Direction.FRONT);
                            }
                            else
                            {
                                // previously facing left
                                pwd.anim.Set(mpd.walkRight1, mpd.walkRight2, Direction.FRONT);
                            }
                        }
                    }
                }
            }
        }

        public short MoveJoe(Direction direction, short endx, short endy, bool inCutaway)
        {
            _joeInterrupted = false;
            _joeMoveBlock = false;
            short can = 0;
            InitWalkData();

            ushort oldx = (ushort)_vm.Graphics.Bobs[0].x;
            ushort oldy = (ushort)_vm.Graphics.Bobs[0].y;

            _vm.Logic.JoeWalk = JoeWalkMode.MOVE;

            ushort oldPos = _vm.Grid.FindAreaForPos(GridScreen.ROOM, oldx, oldy);
            ushort newPos = _vm.Grid.FindAreaForPos(GridScreen.ROOM, (ushort)endx, (ushort)endy);

            D.Debug(9, $"Walk::moveJoe({direction}, {oldx}, {oldy}, {endx}, {endy}) - old = {oldPos}, new = {newPos}");

            // if in cutaway, allow Joe to walk anywhere
            if (newPos == 0 && inCutaway)
            {
                IncWalkData((short)(short)oldx, (short)oldy, endx, endy, oldPos);
            }
            else
            {
                if (Calc(oldPos, newPos, (short)oldx, (short)oldy, endx, endy))
                {
                    if (_walkDataCount > 0)
                    {
                        AnimateJoePrepare();
                        AnimateJoe();
                        if (_joeInterrupted)
                        {
                            can = -1;
                        }
                    }
                }
                else
                {
                    // path has been blocked, make Joe say so
                    _vm.Logic.MakeJoeSpeak(4);
                    can = -1;
                }
            }

            _vm.Graphics.Bobs[0].animating = false;
            if (_joeMoveBlock)
            {
                can = -2;
                _joeMoveBlock = false;
            }
            else if (direction > 0)
            {
                _vm.Logic.JoeFacing = direction;
            }
            _vm.Logic.JoePrevFacing = _vm.Logic.JoeFacing;
            _vm.Logic.JoeFace();
            return can;
        }

        bool Calc(ushort oldPos, ushort newPos, short oldx, short oldy, short x, short y)
        {
            // if newPos is outside of an AREA then traverse Y axis until an AREA is found
            if (newPos == 0)
            {
                newPos = (ushort)FindAreaPosition(ref x, ref y, true);
            }

            // do the same for oldPos in case Joe somehow sits on the border of an AREA
            // and does not register
            if (oldPos == 0)
            {
                oldPos = (ushort)FindAreaPosition(ref oldx, ref oldy, false);
            }

            if (oldPos == newPos)
            {
                IncWalkData(oldx, oldy, x, y, newPos);
                return true;
            }
            else if (CalcPath(oldPos, newPos))
            {
                ushort i;
                short px = oldx;
                short py = oldy;
                for (i = 2; i <= _areaListCount; ++i)
                {
                    ushort a1 = _areaList[i - 1];
                    ushort a2 = _areaList[i];
                    Area pa1 = _roomArea[a1];
                    Area pa2 = _roomArea[a2];
                    ushort x1 = (ushort)CalcC(pa1.box.x1, pa1.box.x2, pa2.box.x1, pa2.box.x2, px);
                    ushort y1 = (ushort)CalcC(pa1.box.y1, pa1.box.y2, pa2.box.y1, pa2.box.y2, py);
                    IncWalkData(px, py, (short)x1, (short)y1, a1);
                    px = (short)x1;
                    py = (short)y1;
                }
                IncWalkData(px, py, x, y, newPos);
                return true;
            }
            return false;
        }

        private bool CalcPath(ushort oldArea, ushort newArea)
        {
            D.Debug(9, $"Walk::calcPath({oldArea}, {newArea})");
            _areaList[1] = _areaStrike[1] = oldArea;
            _areaListCount = _areaStrikeCount = 1;
            ushort area = oldArea;
            while (_areaListCount > 0 && area != newArea)
            {
                area = FindFreeArea(area);
                if (area == 0)
                {
                    // wrong path, rolling back
                    _areaList[_areaListCount] = 0;
                    --_areaListCount;
                    area = _areaList[_areaListCount];
                }
                else
                {
                    ++_areaListCount;
                    _areaList[_areaListCount] = area;
                    if (!IsAreaStruck(area))
                    {
                        ++_areaStrikeCount;
                        _areaStrike[_areaStrikeCount] = area;
                    }
                }
            }
            return _areaList[1] != 0;
        }

        private ushort FindFreeArea(ushort area)
        {
            ushort testArea;
            ushort freeArea = 0;
            ushort map = (ushort)Math.Abs(_roomArea[area].mapNeighbors);
            for (testArea = 1; testArea <= _roomAreaCount; ++testArea)
            {
                int b = _roomAreaCount - testArea;
                if ((map & (1 << b)) != 0)
                {
                    // connecting area, check if it's been struck off
                    if (!IsAreaStruck(testArea))
                    {
                        // legitimate connecting area, keep it
                        freeArea = testArea;
                        break;
                    }
                }
            }
            return freeArea;
        }

        private bool IsAreaStruck(ushort area)
        {
            int i;
            bool found = false;
            for (i = 1; i <= _areaStrikeCount; ++i)
            {
                if (_areaStrike[i] == area)
                {
                    found = true;
                    break;
                }
            }
            return found;
        }

        private short CalcC(short c1, short c2, short c3, short c4, short lastc)
        {
            short s1 = Math.Max(c1, c3);
            short s2 = Math.Min(c2, c4);
            short c;
            if ((lastc >= s1 && lastc <= s2) || (lastc >= s2 && lastc <= s1))
            {
                c = lastc;
            }
            else
            {
                c = (short)((s1 + s2) / 2);
            }
            return c;
        }

        private short FindAreaPosition(ref short x, ref short y, bool recalibrate)
        {
            // In order to locate the nearest available area, the original algorithm
            // computes the horizontal and vertical distances for each available area.
            // Unlike the original, we also compute the diagonal distance.
            // To get an example of this in action, in the room D1, make Joe walking
            // to the wall at the right of the window (just above the radiator). On the
            // original game, Joe will go to the left door...
            ushort i;
            ushort pos = 1;
            uint minDist = uint.MaxValue;
            Box b = _roomArea[1].box;
            for (i = 1; i <= _roomAreaCount; ++i)
            {
                b = _roomArea[i].box;

                ushort dx1 = (ushort)Math.Abs(b.x1 - x);
                ushort dx2 = (ushort)Math.Abs(b.x2 - x);
                ushort dy1 = (ushort)Math.Abs(b.y1 - y);
                ushort dy2 = (ushort)Math.Abs(b.y2 - y);
                ushort csx = Math.Min(dx1, dx2);
                ushort csy = Math.Min(dy1, dy2);

                bool inX = (x >= b.x1) && (x <= b.x2);
                bool inY = (y >= b.y1) && (y <= b.y2);

                uint dist = minDist;
                if (
                  !inX && !inY)
                {
                    dist = (uint)(csx * csx + csy * csy);
                }
                else if (inX)
                {
                    dist = (uint)(csy * csy);
                }
                else if (inY)
                {
                    dist = (uint)(csx * csx);
                }

                if (dist < minDist)
                {
                    minDist = dist;
                    pos = i;
                }
            }
            // we now have the closest area near X,Y, so we can recalibrate
            // the X,Y coord to be in this area
            if (recalibrate)
            {
                b = _roomArea[pos].box;
                if (x < b.x1) x = b.x1;
                if (x > b.x2) x = b.x2;
                if (y < b.y1) y = b.y1;
                if (y > b.y2) y = b.y2;
            }
            return (short)pos;
        }

        private void AnimateJoe()
        {
            // queen.c l.2789-2835
            Direction lastDirection = 0;
            ushort i;
            BobSlot pbs = _vm.Graphics.Bobs[0];
            _vm.Logic.JoeFacing = _walkData[1].anim.facing;
            _vm.Logic.JoeScale = _walkData[1].area.CalcScale(pbs.y);
            _vm.Logic.JoeFace();
            for (i = 1; i <= _walkDataCount && !_joeInterrupted; ++i)
            {
                WalkData pwd = _walkData[i];

                // area has been turned off, see if we should execute a cutaway
                if (pwd.area.mapNeighbors < 0)
                {
                    // queen.c l.2838-2911
                    _vm.Logic.HandleSpecialArea(pwd.anim.facing, pwd.areaNum, i);
                    _joeMoveBlock = true;
                    return;
                }
                if (lastDirection != pwd.anim.facing)
                {
                    pbs.AnimNormal((ushort)pwd.anim.firstFrame, (ushort)pwd.anim.lastFrame, 1, false, false);
                }

                ushort moveSpeed = (ushort)(_vm.Grid.FindScale((ushort)pbs.x, (ushort)pbs.y) * 6 / 100);
                pbs.Move((short)(pbs.x + pwd.dx), (short)(pbs.y + pwd.dy), (short)moveSpeed);
                pbs.xflip = (pbs.xdir < 0);
                while (pbs.moving)
                {
                    // adjust Joe's movespeed according to scale
                    pbs.scale = pwd.area.CalcScale(pbs.y);
                    _vm.Logic.JoeScale = pbs.scale;
                    pbs.ScaleWalkSpeed(6);
                    _vm.Update(true);
                    if (_vm.Input.CutawayQuit || _vm.Logic.JoeWalk == JoeWalkMode.EXECUTE)
                    {
                        StopJoe();
                        break;
                    }
                }
                lastDirection = pwd.anim.facing;
            }
            _vm.Logic.JoeFacing = lastDirection;
        }

        private void StopJoe()
        {
            BobSlot pbs = _vm.Graphics.Bobs[0];
            pbs.moving = false;
            _joeInterrupted = true;
        }

        private void AnimateJoePrepare()
        {
            // queen.c l.2748-2788
            ushort i;
            for (i = 1; i <= _walkDataCount; ++i)
            {
                WalkData pwd = _walkData[i];

                if (pwd.dx < 0)
                {
                    pwd.anim.Set(11, 18, Direction.LEFT);
                }
                else
                {
                    pwd.anim.Set(11, 18, Direction.RIGHT);
                }

                short k = Math.Abs(pwd.dy);
                short ds = pwd.area.ScaleDiff;
                if (ds > 0)
                {
                    k = (short)(k * (((k * ds) / pwd.area.box.yDiff) / 2));
                }

                if (Math.Abs(pwd.dx) < k)
                {
                    if (pwd.dy < 0)
                    {
                        if (ds < 0)
                        {
                            pwd.anim.Set(19, 24, Direction.FRONT);
                        }
                        else
                        {
                            pwd.anim.Set(25, 30, Direction.BACK);
                        }
                    }
                    else if (pwd.dy > 0)
                    {
                        if (ds < 0)
                        {
                            pwd.anim.Set(25, 30, Direction.BACK);
                        }
                        else
                        {
                            pwd.anim.Set(19, 24, Direction.FRONT);
                        }
                    }
                }
            }
        }

        private void IncWalkData(short px, short py, short x, short y, ushort areaNum)
        {
            D.Debug(9, $"Walk::incWalkData({x - px}, {y - py}, {areaNum})");
            if (px != x || py != y)
            {
                ++_walkDataCount;
                WalkData pwd = _walkData[_walkDataCount];
                pwd.dx = (short)(x - px);
                pwd.dy = (short)(y - py);
                pwd.area = _roomArea[areaNum];
                pwd.areaNum = areaNum;
            }
        }

        private void InitWalkData()
        {
            ushort curRoom = _vm.Logic.CurrentRoom;
            _roomArea = _vm.Grid.Areas[curRoom];
            _roomAreaCount = (ushort)_vm.Grid.AreaMax[curRoom];

            _walkDataCount = 0;
            Array.Clear(_walkData, 0, _walkData.Length);
            _areaStrikeCount = 0;
            Array.Clear(_areaStrike, 0, _areaStrike.Length);
            _areaListCount = 0;
            Array.Clear(_areaList, 0, _areaList.Length);
        }
    }
}