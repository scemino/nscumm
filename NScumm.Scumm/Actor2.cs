//
//  Actor2.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2015 
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
using NScumm.Core.Graphics;
using NScumm.Scumm.Graphics;

namespace NScumm.Scumm
{
    class Actor2: Actor3
    {
        public const int V12_X_MULTIPLIER = 8;
        public const int V12_Y_MULTIPLIER = 2;

        public override bool IsPlayer
        {
            get
            {
                // isPlayer() is not supported by v0
                Debug.Assert(_scumm.Game.Version != 0);
                return _scumm.Variables[42] <= Number && Number <= _scumm.Variables[43];
            }
        }

        public Actor2(ScummEngine engine, byte id)
            : base(engine, id)
        {
        }

        public override void Init(int mode)
        {
            base.Init(mode);

            _speedx = 1;
            _speedy = 1;

            InitFrame = 2;
            WalkFrame = 0;
            StandFrame = 1;
            TalkStartFrame = 5;
            TalkStopFrame = 4;
        }

        public override void Walk()
        {
            Point foundPath, tmp;
            int new_dir, next_box;

            if (Moving.HasFlag(MoveFlags.Turn))
            {
                new_dir = UpdateActorDirection(false);
                if (Facing != new_dir)
                {
                    SetDirection(new_dir);
                }
                else
                {
                    Moving = MoveFlags.None;
                }
                return;
            }

            if (Moving == MoveFlags.None)
                return;

            if (Moving.HasFlag(MoveFlags.InLeg))
            {
                ActorWalkStep();
            }
            else
            {
                if (Moving.HasFlag(MoveFlags.LastLeg))
                {
                    Moving = MoveFlags.None;
                    StartAnimActor(StandFrame);
                    if (_targetFacing != _walkdata.DestDir)
                        TurnToDirection(_walkdata.DestDir);
                }
                else
                {
                    SetBox(_walkdata.CurBox);
                    if (Walkbox == _walkdata.DestBox)
                    {
                        foundPath = _walkdata.Dest;
                        Moving |= MoveFlags.LastLeg;
                    }
                    else
                    {
                        next_box = _scumm.GetNextBox(Walkbox, _walkdata.DestBox);
                        if (next_box < 0)
                        {
                            Moving |= MoveFlags.LastLeg;
                            return;
                        }

                        // Can't walk through locked boxes
                        var flags = _scumm.GetBoxFlags((byte)next_box);
                        if ((flags.HasFlag(BoxFlags.Locked)) && !((flags.HasFlag(BoxFlags.PlayerOnly) && !IsPlayer)))
                        {
                            Moving |= MoveFlags.LastLeg;
                            //_walkdata.destdir = -1;
                        }

                        _walkdata.CurBox = (byte)next_box;

                        ScummMath.GetClosestPtOnBox(_scumm.GetBoxCoordinates(_walkdata.CurBox), RealPosition, out tmp);
                        ScummMath.GetClosestPtOnBox(_scumm.GetBoxCoordinates(Walkbox), tmp, out foundPath);
                    }
                    CalcMovementFactor(foundPath);
                }
            }
        }

        public override AdjustBoxResult AdjustXYToBeInBox(Point dst)
        {
            var abr = new AdjustBoxResult();
            abr.Position = dst;
            abr.Box = InvalidBox;

            var numBoxes = _scumm.GetNumBoxes() - 1;
            var bestDist = 0xFF;
            for (int i = 0; i <= numBoxes; i++)
            {
                // MM v0 prioritizes lower boxes, other engines higher boxes
                var box = (byte)(_scumm.Game.Version == 0 ? i : numBoxes - i);
                var flags = _scumm.GetBoxFlags(box);
                if ((flags.HasFlag(BoxFlags.Invisible) && !((flags.HasFlag(BoxFlags.PlayerOnly) && !IsPlayer))))
                    continue;
                Point found;
                var dist = CheckXYInBoxBounds(box, dst, out found); // also merged with getClosestPtOnBox
                if (dist == 0)
                {
                    abr.Position = found;
                    abr.Box = box;
                    break;
                }
                if (dist < bestDist)
                {
                    bestDist = dist;
                    abr.Position = found;
                    abr.Box = box;
                }
            }

            return abr;
        }

        protected override void PrepareDrawActorCostume(ICostumeRenderer bcr) 
        {
            base.PrepareDrawActorCostume(bcr);

            bcr.ActorX = RealPosition.X;
            bcr.ActorY = RealPosition.Y - Elevation;

            if (_scumm.Game.Version <= 2) {
                bcr.ActorX *= V12_X_MULTIPLIER;
                bcr.ActorY *= V12_Y_MULTIPLIER;
            }
            bcr.ActorX -= _scumm.MainVirtScreen.XStart;

//            if (_scumm.Game.Platform == Common::kPlatformNES) {
//                // In the NES version, when the actor is facing right,
//                // we need to shift it 8 pixels to the left
//                if (Facing == 90)
//                    bcr.ActorX -= 8;
//            } else 
                if (_scumm.Game.Version == 0) {
                bcr.ActorX += 12;
            } else if (_scumm.Game.Version <= 2) {
                // HACK: We have to adjust the x position by one strip (8 pixels) in
                // V2 games. However, it is not quite clear to me why. And to fully
                // match the original, it seems we have to offset by 2 strips if the
                // actor is facing left (270 degree).
                // V1 games are once again slightly different, here we only have
                // to adjust the 270 degree case...
                if (Facing == 270)
                    bcr.ActorX += 16;
                else if (_scumm.Game.Version == 2)
                    bcr.ActorX += 8;
            }
        }

        static int CheckXYInBoxBounds(int boxnum, Point p, out Point dest)
        {
            BoxCoords box = ScummEngine.Instance.GetBoxCoordinates(boxnum);
            int xmin, xmax;

            // We are supposed to determine the point (destX,destY) contained in
            // the given box which is closest to the point (x,y), and then return
            // some kind of "distance" between the two points.

            // First, we determine destY and a range (xmin to xmax) in which destX
            // is contained.
            if (p.Y < box.UpperLeft.Y)
            {
                // Point is above the box
                dest.Y = box.UpperLeft.Y;
                xmin = box.UpperLeft.X;
                xmax = box.UpperRight.X;
            }
            else if (p.Y >= box.LowerLeft.Y)
            {
                // Point is below the box
                dest.Y = box.LowerLeft.Y;
                xmin = box.LowerLeft.X;
                xmax = box.LowerRight.X;
            }
            else if ((p.X >= box.UpperLeft.X) && (p.X >= box.LowerLeft.X) && (p.X < box.UpperRight.X) && (p.X < box.LowerRight.X))
            {
                // Point is strictly inside the box
                dest.X = p.X;
                dest.Y = p.Y;
                xmin = xmax = p.X;
            }
            else
            {
                // Point is to the left or right of the box,
                // so the y coordinate remains unchanged
                dest.Y = p.Y;
                int ul = box.UpperLeft.X;
                int ll = box.LowerLeft.X;
                int ur = box.UpperRight.X;
                int lr = box.LowerRight.X;
                int top = box.UpperLeft.Y;
                int bottom = box.LowerLeft.Y;
                int cury;

                // Perform a binary search to determine the x coordinate.
                // Note: It would be possible to compute this value in a
                // single step simply by calculating the slope of the left
                // resp. right side and using that to find the correct
                // result. However, the original engine did use the search
                // approach, so we do that, too.
                do
                {
                    xmin = (ul + ll) / 2;
                    xmax = (ur + lr) / 2;
                    cury = (top + bottom) / 2;

                    if (cury < p.Y)
                    {
                        top = cury;
                        ul = xmin;
                        ur = xmax;
                    }
                    else if (cury > p.Y)
                    {
                        bottom = cury;
                        ll = xmin;
                        lr = xmax;
                    }
                } while (cury != p.Y);
            }

            // Now that we have limited the value of destX to a fixed
            // interval, it's a trivial matter to finally determine it.
            if (p.X < xmin)
            {
                dest.X = xmin;
            }
            else if (p.X > xmax)
            {
                dest.X = xmax;
            }
            else
            {
                dest.X = p.X;
            }

            // Compute the distance of the points. We measure the
            // distance with a granularity of 8x8 blocks only (hence
            // yDist must be divided by 4, as we are using 8x2 pixels
            // blocks for actor coordinates).
            int xDist = Math.Abs(p.X - dest.X);
            int yDist = Math.Abs(p.Y - dest.Y) / 4;
            int dist;

            if (ScummEngine.Instance.Game.Version == 0)
                xDist *= 2;

            if (xDist < yDist)
                dist = (xDist >> 1) + yDist;
            else
                dist = (yDist >> 1) + xDist;

            return dist;
        }
    }
}

