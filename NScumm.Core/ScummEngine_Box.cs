//
//  ScummEngine_Box.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using NScumm.Core.Graphics;
using System.Diagnostics;

namespace NScumm.Core
{
    partial class ScummEngine
    {
        protected List<byte> _boxMatrix = new List<byte>();
        Box[] _boxes;
        readonly ScaleSlot[] _scaleSlots;
        internal ushort[] _extraBoxFlags = new ushort[65];

        internal BoxFlags GetBoxFlags(byte boxNum)
        {
            var box = GetBoxBase(boxNum);
            if (box == null)
                return 0;
            return box.Flags;
        }

        internal byte GetBoxMask(byte boxNum)
        {
            if (Game.Version <= 3 && boxNum == 255)
                return 1;

            // WORKAROUND for bug #847827: This is a bug in the data files, as it also
            // occurs with the original engine. We work around it here anyway.
            if (_game.Id == "atlantis" && _currentRoom == 225 && _roomResource == 94 && boxNum == 8)
                return 0;

            var box = GetBoxBase(boxNum);
            if (box == null)
                return 0;

            return box.Mask;
        }

        internal int GetNumBoxes()
        {
            return _boxes.Length;
        }

        internal BoxCoords GetBoxCoordinates(int boxnum)
        {
            var bp = GetBoxBase(boxnum);
            var box = new BoxCoords();

            box.UpperLeft.X = bp.Ulx;
            box.UpperLeft.Y = bp.Uly;
            box.UpperRight.X = bp.Urx;
            box.UpperRight.Y = bp.Ury;

            box.LowerLeft.X = bp.Llx;
            box.LowerLeft.Y = bp.Lly;
            box.LowerRight.X = bp.Lrx;
            box.LowerRight.Y = bp.Lry;

            return box;
        }

        protected Box GetBoxBase(int boxnum)
        {
            if (boxnum == 255)
                return null;

            // As a workaround, we simply use the last box if the last+1 box is requested.
            // Note that this may cause different behavior than the original game
            // engine exhibited! To faithfully reproduce the behavior of the original
            // engine, we would have to know the data coming *after* the walkbox table.
            if (_boxes.Length == boxnum)
                boxnum--;
            return _boxes[boxnum];
        }

        /// <summary>
        /// Compute if there is a way that connects box 'from' with box 'to'.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>
        /// The number of a box adjacent to 'from' that is the next on the
        /// way to 'to' (this can be 'to' itself or a third box).
        /// If there is no connection -1 is return.
        /// </returns>
        internal int GetNextBox(byte from, byte to)
        {
            byte i;
            int numOfBoxes = GetNumBoxes();
            int dest = -1;

            if (from == to)
                return to;

            if (to == InvalidBox)
                return -1;

            if (from == InvalidBox)
                return to;

            if (from >= numOfBoxes)
                throw new ArgumentOutOfRangeException("from");
            if (to >= numOfBoxes)
                throw new ArgumentOutOfRangeException("to");

            var boxm = _boxMatrix;

            // WORKAROUND #1: It seems that in some cases, the box matrix is corrupt
            // (more precisely, is too short) in the datafiles already. In
            // particular this seems to be the case in room 46 of Indy3 EGA (see
            // also bug #770690). This didn't cause problems in the original
            // engine, because there, the memory layout is different. After the
            // walkbox would follow the rest of the room file, thus the program
            // always behaved the same (and by chance, correct). Not so for us,
            // since random data may follow after the resource in ScummVM.
            //
            // As a workaround, we add a check for the end of the box matrix
            // resource, and abort the search once we reach the end.

            int boxmIndex = _boxMatrix[0] == 0xFF ? 1 : 0;
            // Skip up to the matrix data for box 'from'
            for (i = 0; i < from && boxmIndex < boxm.Count; i++)
            {
                while (boxmIndex < boxm.Count && boxm[boxmIndex] != 0xFF)
                    boxmIndex += 3;
                boxmIndex++;
            }

            // Now search for the entry for box 'to'
            while (boxmIndex < boxm.Count && boxm[boxmIndex] != 0xFF)
            {
                if (boxm[boxmIndex] <= to && to <= boxm[boxmIndex + 1])
                    dest = (sbyte)boxm[boxmIndex + 2];
                boxmIndex += 3;
            }

            //if (boxm >= boxm.Count)
            //    debug(0, "The box matrix apparently is truncated (room %d)", _roomResource);

            return dest;
        }

        internal bool CheckXYInBoxBounds(int boxnum, Point p)
        {
            // Since this method is called by many other methods that take params
            // from e.g. script opcodes, but do not validate the boxnum, we
            // make a check here to filter out invalid boxes.
            // See also bug #1599113.
            if (boxnum < 0 || boxnum == InvalidBox)
                return false;

            var box = GetBoxCoordinates(boxnum);

            // Quick check: If the x (resp. y) coordinate of the point is
            // strictly smaller (bigger) than the x (y) coordinates of all
            // corners of the quadrangle, then it certainly is *not* contained
            // inside the quadrangle.
            if (p.X < box.UpperLeft.X && p.X < box.UpperRight.X && p.X < box.LowerRight.X && p.X < box.LowerLeft.X)
                return false;

            if (p.X > box.UpperLeft.X && p.X > box.UpperRight.X && p.X > box.LowerRight.X && p.X > box.LowerLeft.X)
                return false;

            if (p.Y < box.UpperLeft.Y && p.Y < box.UpperRight.Y && p.Y < box.LowerRight.Y && p.Y < box.LowerLeft.Y)
                return false;

            if (p.Y > box.UpperLeft.Y && p.Y > box.UpperRight.Y && p.Y > box.LowerRight.Y && p.Y > box.LowerLeft.Y)
                return false;

            // Corner case: If the box is a simple line segment, we consider the
            // point to be contained "in" (or rather, lying on) the line if it
            // is very close to its projection to the line segment.
            if ((box.UpperLeft == box.UpperRight && box.LowerRight == box.LowerLeft) ||
                (box.UpperLeft == box.LowerLeft && box.UpperRight == box.LowerRight))
            {
                Point tmp;
                tmp = ScummMath.ClosestPtOnLine(box.UpperLeft, box.LowerRight, p);
                if (p.SquareDistance(tmp) <= 4)
                    return true;
            }

            // Finally, fall back to the classic algorithm to compute containment
            // in a convex polygon: For each (oriented) side of the polygon
            // (quadrangle in this case), compute whether p is "left" or "right"
            // from it.

            if (!ScummMath.CompareSlope(box.UpperLeft, box.UpperRight, p))
                return false;

            if (!ScummMath.CompareSlope(box.UpperRight, box.LowerRight, p))
                return false;

            if (!ScummMath.CompareSlope(box.LowerRight, box.LowerLeft, p))
                return false;

            if (!ScummMath.CompareSlope(box.LowerLeft, box.UpperLeft, p))
                return false;

            return true;
        }

        protected void SetBoxFlags(int box, int val)
        {
            // SCUMM7+ stuff
            if ((val & 0xC000)!=0)
            {
                Debug.Assert(box >= 0 && box < 65);
                _extraBoxFlags[box] = (ushort)val;
            }
            else
            {
                var b = GetBoxBase(box);
                if (b == null)
                    return;
                b.Flags = (BoxFlags)val;
            }
        }

        public int GetBoxScale(byte boxNum)
        {
            var box = GetBoxBase(boxNum);
            if (box == null)
                return 255;
            return box.Scale;
        }

        public int GetScale(int boxNum, short x, short y)
        {
            var box = GetBoxBase(boxNum);
            if (box == null)
                return 255;

            int scale = (int)box.Scale;
            int slot = 0;
            if ((scale & 0x8000) != 0)
                slot = (scale & 0x7FFF) + 1;

            // Was a scale slot specified? If so, we compute the effective scale
            // from it, ignoring the box scale.
            if (slot != 0)
                scale = GetScaleFromSlot(slot, x, y);

            return scale;
        }

        public int GetScaleFromSlot(int slot, int x, int y)
        {
            int scale;
            int scaleX;
            int scaleY = 0;
            var s = _scaleSlots[slot - 1];

            //if (s.y1 == s.y2 && s.x1 == s.x2)
            //    throw new NotSupportedException(string.Format("Invalid scale slot {0}", slot));

            if (s.Y1 != s.Y2)
            {
                if (y < 0)
                    y = 0;

                scaleY = (s.Scale2 - s.Scale1) * (y - s.Y1) / (s.Y2 - s.Y1) + s.Scale1;
            }
            if (s.X1 == s.X2)
            {
                scale = scaleY;
            }
            else
            {
                scaleX = (s.Scale2 - s.Scale1) * (x - s.X1) / (s.X2 - s.X1) + s.Scale1;

                if (s.Y1 == s.Y2)
                {
                    scale = scaleX;
                }
                else
                {
                    scale = (scaleX + scaleY) / 2;
                }
            }

            // Clip the scale to range 1-255
            if (scale < 1)
                scale = 1;
            else if (scale > 255)
                scale = 255;

            return scale;
        }

        protected void SetScaleSlot(int slot, int x1, int y1, int scale1, int x2, int y2, int scale2)
        {
            if (slot < 1)
                throw new ArgumentOutOfRangeException("slot", slot, "Invalid scale slot");
            if (slot > _scaleSlots.Length)
                throw new ArgumentOutOfRangeException("slot", slot, "Invalid scale slot");
            _scaleSlots[slot - 1] = new ScaleSlot { X1 = x1, X2 = x2, Y1 = y1, Y2 = y2, Scale1 = scale1, Scale2 = scale2 };
        }
    }
}

