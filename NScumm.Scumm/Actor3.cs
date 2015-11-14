//
//  Actor3.cs
//
//  Author:
//       Scemino <scemino74@gmail.com>
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

namespace NScumm.Scumm
{
    class Actor3: Actor
    {
        public Actor3(ScummEngine engine, byte id)
            : base(engine, id)
        {
        }

        protected override void SetupActorScale()
        {
            // TODO: The following could probably be removed
            ScaleX = 0xFF;
            ScaleY = 0xFF;
        }

        public override void Walk()
        {
            Point p2, p3;   // Gate locations
            int new_dir, next_box;

            if (Moving == 0)
                return;

            if (!(Moving.HasFlag(MoveFlags.NewLeg)))
            {
                if (Moving.HasFlag(MoveFlags.InLeg) && ActorWalkStep())
                    return;

                if (Moving.HasFlag(MoveFlags.LastLeg))
                {
                    Moving = 0;
                    StartAnimActor(StandFrame);
                    if (_targetFacing != _walkdata.DestDir)
                        TurnToDirection(_walkdata.DestDir);
                    return;
                }

                if (Moving.HasFlag(MoveFlags.Turn))
                {
                    new_dir = UpdateActorDirection(false);
                    if (Facing != new_dir)
                        SetDirection(new_dir);
                    else
                        Moving = 0;
                    return;
                }

                if (_walkdata.Point3.X != 32000)
                {
                    if (CalcMovementFactor(_walkdata.Point3))
                    {
                        _walkdata.Point3.X = 32000;
                        return;
                    }
                    _walkdata.Point3.X = 32000;
                }

                SetBox(_walkdata.CurBox);
                Moving &= MoveFlags.InLeg;
            }

            Moving &= ~MoveFlags.NewLeg;
            do
            {
                if (Walkbox == InvalidBox)
                {
                    SetBox(_walkdata.DestBox);
                    _walkdata.CurBox = _walkdata.DestBox;
                    break;
                }

                if (Walkbox == _walkdata.DestBox)
                    break;

                next_box = _scumm.GetNextBox(Walkbox, _walkdata.DestBox);
                if (next_box < 0)
                {
                    Moving |= MoveFlags.LastLeg;
                    return;
                }

                // Can't walk through locked boxes
                var flags = _scumm.GetBoxFlags((byte)next_box);
                if (flags.HasFlag(BoxFlags.Locked) && !(flags.HasFlag(BoxFlags.PlayerOnly) && !IsPlayer))
                {
                    Moving |= MoveFlags.LastLeg;
                    return;
                }

                _walkdata.CurBox = (byte)next_box;

                FindPathTowardsOld(Walkbox, (byte)next_box, _walkdata.DestBox, out p2, out p3);
                if (p2.X == 32000 && p3.X == 32000)
                {
                    break;
                }

                if (p2.X != 32000)
                {
                    if (CalcMovementFactor(p2))
                    {
                        _walkdata.Point3 = p3;
                        return;
                    }
                }
                if (CalcMovementFactor(p3))
                    return;

                SetBox(_walkdata.CurBox);
            } while (true);

            Moving |= MoveFlags.LastLeg;
            CalcMovementFactor(_walkdata.Dest);
        }

        void FindPathTowardsOld(byte box1, byte box2, byte finalBox, out Point p2, out Point p3)
        {
            var gateA = new Point[2];
            var gateB = new Point[2];
            p2 = new Point();
            p3 = new Point();

            GetGates(_scumm.GetBoxCoordinates(box1), _scumm.GetBoxCoordinates(box2), gateA, gateB);

            p2.X = 32000;
            p3.X = 32000;

            // TODO:
            // next box (box2) = final box?
//            if (box2 == finalBox)
//            {
//                // In Indy3, the masks (= z-level) have to match, too -- needed for the
//                // 'maze' in the zeppelin (see bug #1032964).
            //                if (_scumm.Game.Id != GID_INDY3 || _vm->getMaskFromBox(box1) == _vm->getMaskFromBox(box2))
//                {
//                    // Is the actor (x,y) between both gates?
//                    if (compareSlope(_pos, _walkdata.dest, gateA[0]) !=
//                        compareSlope(_pos, _walkdata.dest, gateB[0]) &&
//                        compareSlope(_pos, _walkdata.dest, gateA[1]) !=
//                        compareSlope(_pos, _walkdata.dest, gateB[1]))
//                    {
//                        return;
//                    }
//                }
//            }

            p3 = ScummMath.ClosestPtOnLine(gateA[1], gateB[1], Position);

            if (ScummMath.CompareSlope(Position, p3, gateA[0]) == ScummMath.CompareSlope(Position, p3, gateB[0]))
            {
                p2 = ScummMath.ClosestPtOnLine(gateA[0], gateB[0], Position);
            }
        }

        void GetGates(BoxCoords box1, BoxCoords box2, Point[] gateA, Point[] gateB)
        {
            int i, j;
            var dist = new int[8];
            var minDist = new int[3];
            var closest = new int[3];
            var box = new bool[3];
            var closestPoint = new Point[8];
            var boxCorner = new Point[8];
            int line1, line2;

            // For all corner coordinates of the first box, compute the point closest
            // to them on the second box (and also compute the distance of these points).
            boxCorner[0] = box1.UpperLeft;
            boxCorner[1] = box1.UpperRight;
            boxCorner[2] = box1.LowerRight;
            boxCorner[3] = box1.LowerLeft;
            for (i = 0; i < 4; i++)
            {
                dist[i] = (int)ScummMath.GetClosestPtOnBox(box2, boxCorner[i], out closestPoint[i]);
            }

            // Now do the same but with the roles of the first and second box swapped.
            boxCorner[4] = box2.UpperLeft;
            boxCorner[5] = box2.UpperRight;
            boxCorner[6] = box2.LowerRight;
            boxCorner[7] = box2.LowerLeft;
            for (i = 4; i < 8; i++)
            {
                dist[i] = (int)ScummMath.GetClosestPtOnBox(box1, boxCorner[i], out closestPoint[i]);
            }

            // Find the three closest "close" points between the two boxes.
            for (j = 0; j < 3; j++)
            {
                minDist[j] = 0xFFFF;
                for (i = 0; i < 8; i++)
                {
                    if (dist[i] < minDist[j])
                    {
                        minDist[j] = dist[i];
                        closest[j] = i;
                    }
                }
                dist[closest[j]] = 0xFFFF;
                minDist[j] = (int)Math.Sqrt(minDist[j]);
                box[j] = (closest[j] > 3);  // Is the point on the first or on the second box?
            }


            // Finally, compute the actual "gate".

            if (box[0] == box[1] && Math.Abs(minDist[0] - minDist[1]) < 4)
            {
                line1 = closest[0];
                line2 = closest[1];

            }
            else if (box[0] == box[1] && minDist[0] == minDist[1])
            {  // parallel
                line1 = closest[0];
                line2 = closest[1];
            }
            else if (box[0] == box[2] && minDist[0] == minDist[2])
            {  // parallel
                line1 = closest[0];
                line2 = closest[2];
            }
            else if (box[1] == box[2] && minDist[1] == minDist[2])
            {  // parallel
                line1 = closest[1];
                line2 = closest[2];

            }
            else if (box[0] == box[2] && Math.Abs(minDist[0] - minDist[2]) < 4)
            {
                line1 = closest[0];
                line2 = closest[2];
            }
            else if (Math.Abs(minDist[0] - minDist[2]) < 4)
            {
                line1 = closest[1];
                line2 = closest[2];
            }
            else if (Math.Abs(minDist[0] - minDist[1]) < 4)
            {
                line1 = closest[0];
                line2 = closest[1];
            }
            else
            {
                line1 = closest[0];
                line2 = closest[0];
            }

            // Set the gate
            if (line1 < 4)
            {
                gateA[0] = boxCorner[line1];
                gateA[1] = closestPoint[line1];
            }
            else
            {
                gateA[1] = boxCorner[line1];
                gateA[0] = closestPoint[line1];
            }

            if (line2 < 4)
            {
                gateB[0] = boxCorner[line2];
                gateB[1] = closestPoint[line2];
            }
            else
            {
                gateB[1] = boxCorner[line2];
                gateB[0] = closestPoint[line2];
            }
        }

    }
}
